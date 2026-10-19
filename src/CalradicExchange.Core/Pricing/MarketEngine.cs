using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Guilds;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Random;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>
    /// Owns the market for a campaign: takes world snapshots and events, updates every instrument once per day.
    /// Deterministic for a given state, RNG state, snapshot sequence and event sequence (CLAUDE.md rule 4).
    /// </summary>
    public sealed class MarketEngine
    {
        private const double NegligibleShock = 1e-5;

        /// <summary>Version of the fundamental calculation. Changing it rebuilds existing markets on load (ADR-017).</summary>
        public const string FundamentalMethodId = "volume_v1";

        private readonly InstrumentCatalog _catalog;
        private readonly MarketSettings _settings;
        private readonly IRandom _rng;
        private readonly Dictionary<string, PriceState> _byId = new Dictionary<string, PriceState>();

        /// <summary>Guild instruments built from <see cref="MarketState.Guilds"/> (PLAN 2.2), ordered by id.</summary>
        private readonly List<InstrumentDefinition> _guildDefs = new List<InstrumentDefinition>();
        private readonly Dictionary<string, InstrumentDefinition> _guildById = new Dictionary<string, InstrumentDefinition>();
        private readonly Dictionary<string, GuildProfitSeries> _seriesById = new Dictionary<string, GuildProfitSeries>();

        public MarketEngine(InstrumentCatalog catalog, MarketSettings settings, MarketState state, IRandom rng)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            State = state ?? new MarketState();
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            RefreshGuilds();
            SyncPriceStates();
        }

        public MarketState State { get; }
        public InstrumentCatalog Catalog => _catalog;
        public MarketSettings Settings => _settings;

        /// <summary>Every listed instrument: the catalog in file order, then guilds ordered by id. Order matters for determinism.</summary>
        public IEnumerable<InstrumentDefinition> Instruments => _catalog.Instruments.Concat(_guildDefs);

        /// <summary>Listed guild instruments (open and closed).</summary>
        public IReadOnlyList<InstrumentDefinition> Guilds => _guildDefs;

        public InstrumentDefinition Find(string instrumentId)
        {
            if (instrumentId == null) return null;
            return _catalog.Find(instrumentId) ?? (_guildById.TryGetValue(instrumentId, out InstrumentDefinition def) ? def : null);
        }

        /// <summary>Profit series behind a guild instrument; null for other instruments.</summary>
        public GuildProfitSeries GuildSeries(string instrumentId)
        {
            return instrumentId != null && _seriesById.TryGetValue(instrumentId, out GuildProfitSeries s) ? s : null;
        }

        /// <summary>
        /// True when the saved prices were set with an older fundamental method. The next tick rebuilds the market;
        /// the caller should first settle open positions at current prices.
        /// </summary>
        public bool NeedsRebase => State.Initialized && State.FundamentalMethod != FundamentalMethodId;

        public PriceState Get(string instrumentId)
        {
            return instrumentId != null && _byId.TryGetValue(instrumentId, out PriceState ps) ? ps : null;
        }

        /// <summary>
        /// Registers an event. Impulses are felt on the next tick. Returns the affected instrument ids.
        /// </summary>
        public IReadOnlyList<string> ApplyEvent(MarketEvent ev)
        {
            if (ev == null) throw new ArgumentNullException(nameof(ev));
            var affected = new List<string>();

            if (ev.Type == MarketEventType.SiegeEnded) ReleaseHolds(ShockMapper.HoldKeyFor(ev));
            if (ev.Type == MarketEventType.KingdomDestroyed) CloseKingdom(ev.KingdomId, affected);

            string holdKey = ShockMapper.HoldKeyFor(ev);
            foreach (ShockRule rule in _settings.RulesFor(ev.Type))
            {
                double impulse = rule.Impulse;
                if (rule.Seasonal)
                {
                    if (ev.Season == null || !_settings.SeasonImpulses.TryGetValue(ev.Season.Value, out impulse)) continue;
                }
                if (rule.ScaleByTownShare) impulse *= (ev.TownShare ?? 0) * rule.TownShareMultiplier;

                foreach (InstrumentDefinition def in ShockMapper.Resolve(rule.Target, ev, Instruments, Find))
                {
                    PriceState ps = Get(def.Id);
                    if (ps == null || ps.Closed) continue;

                    string source = ev.Type + ":" + rule.Target;
                    double added = StackLimited(ps, source, impulse, rule.MaxStack);
                    if (added != 0)
                    {
                        ps.Shocks.Add(new ShockComponent
                        {
                            Value = added,
                            Source = source,
                            HalfLifeDays = rule.HalfLifeDays,
                            HoldKey = rule.HoldUntilEnd ? holdKey : null,
                            Pending = true,
                        });
                    }
                    if (rule.VolMultiplier > 1 && rule.VolDays > 0)
                    {
                        ps.VolBoosts.Add(new VolBoost { Multiplier = rule.VolMultiplier, UntilDay = ev.Day + rule.VolDays });
                    }
                    if (!affected.Contains(def.Id)) affected.Add(def.Id);
                }
            }

            AddNews(ev, affected);
            return affected;
        }

        /// <summary>Runs the daily update. A second call for the same (or an older) day does nothing.</summary>
        public TickSummary Tick(WorldSnapshot world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var summary = new TickSummary { Day = world.Day };
            if (world.Day <= State.LastTickDay) return summary;
            summary.Processed = true;

            if (NeedsRebase) Rebuild();
            GuildProfitTracker.Record(State.Guilds, world, _settings.Guilds?.ProfitWindowDays ?? 28);
            RefreshGuilds();
            SyncPriceStates();
            UpdateBaselines(world);

            if (!State.Initialized)
            {
                foreach (InstrumentDefinition def in Instruments)
                {
                    PriceState ps = _byId[def.Id];
                    decimal? f = ComputeFundamental(def, world);
                    if (f != null && f > 0) StartPrice(ps, f.Value, world.Day);
                }
                State.Initialized = true;
                State.StartDay = world.Day;
                State.FundamentalMethod = FundamentalMethodId;
                State.LastSeason = world.Season;
                State.LastTickDay = world.Day;
                summary.Initialized = true;
                summary.InstrumentCount = _byId.Values.Count(p => p.HasPrice);
                SaveRng();
                return summary;
            }

            DetectGeneratedEvents(world, summary);

            Dictionary<string, decimal> observed = ObserveFundamentals(world);
            Dictionary<InstrumentClass, double> common = CommonMoves(observed);

            summary.MinMove = double.MaxValue;
            summary.MaxMove = double.MinValue;
            foreach (InstrumentDefinition def in Instruments)
            {
                PriceState ps = _byId[def.Id];
                // Always draw, so one instrument's data gap does not shift the noise of all the others.
                double eps = _rng.NextGaussian();
                if (def.Class == InstrumentClass.Guild) UpdateGuildLifecycle(def, ps, summary);
                if (ps.Closed) continue;

                bool seen = observed.TryGetValue(def.Id, out decimal f);
                if (!ps.HasPrice)
                {
                    if (seen) StartPrice(ps, f, world.Day);
                    continue;
                }
                if (seen) UpdateFundamental(ps, def.Class, f, common);

                ps.Shock = ClampShock(EffectiveShock(ps, DecayAndSumShocks(ps)));
                double volMult = ActiveVolMultiplier(ps, world.Day);
                ClassParameters cp = _settings.For(def.Class);
                double sigma = cp.Sigma * _settings.VolatilityMultiplier * volMult;

                double r = PriceMath.LogReturn(ps.Price, ps.Fundamental, ps.Shock, cp.Kappa, sigma, eps, _settings.MaxDailyLogReturn);
                ps.Price = PriceMath.Apply(ps.Price, r);
                ps.LastReturn = r;
                AddHistory(ps, world.Day);

                summary.InstrumentCount++;
                if (r < summary.MinMove) { summary.MinMove = r; summary.MinMoveId = def.Id; }
                if (r > summary.MaxMove) { summary.MaxMove = r; summary.MaxMoveId = def.Id; }
            }
            if (summary.InstrumentCount == 0) summary.MinMove = summary.MaxMove = 0;

            State.LastTickDay = world.Day;
            SaveRng();
            return summary;
        }

        /// <summary>
        /// Moves the market price after a player trade: P x (1 +- factor x impact) (ECONOMY_MODEL 5, permanent impact).
        /// </summary>
        public void ApplyTradeImpact(string instrumentId, bool isBuy, double impact)
        {
            PriceState ps = Get(instrumentId);
            if (ps == null || !ps.HasPrice || ps.Closed) return;
            double move = _settings.Trading.PermanentImpactFactor * impact;
            ps.Price = PriceMath.Apply(ps.Price, Math.Log(isBuy ? 1 + move : Math.Max(0.01, 1 - move)));
        }

        /// <summary>Debug: force a price (console exchange.set_price).</summary>
        public bool SetPrice(string instrumentId, decimal price)
        {
            PriceState ps = Get(instrumentId);
            if (ps == null || ps.Closed || price <= 0) return false;
            if (!ps.HasPrice)
            {
                ps.HasPrice = true;
                ps.InitialPrice = price;
                ps.Fundamental = price;
                ps.RawFundamental = price;
                ps.Anchor = price;
            }
            ps.Price = PriceMath.Round((double)price);
            return true;
        }

        private decimal? ComputeFundamental(InstrumentDefinition def, WorldSnapshot world)
        {
            switch (def.Class)
            {
                case InstrumentClass.Commodity:
                    return FundamentalCalculator.Commodity(world, def.ItemId, _catalog.UnitsPerShare);
                case InstrumentClass.Sector:
                    return FundamentalCalculator.Sector(world, def, State);
                case InstrumentClass.Guild:
                    GuildProfitSeries series = GuildSeries(def.Id);
                    if (series == null || series.WorkshopCount <= 0 || series.DailyProfit.Count == 0) return null;
                    return GuildInstruments.ObservedValue(series, _settings.Guilds ?? new GuildSettings(), world.Calendar.DaysInYear);
                default:
                    return null; // Kingdom and bond fundamentals arrive later.
            }
        }

        private void StartPrice(PriceState ps, decimal fundamental, int day)
        {
            decimal p = PriceMath.Round((double)fundamental);
            ps.HasPrice = true;
            ps.Price = p;
            ps.Fundamental = p;
            ps.RawFundamental = p;
            ps.Anchor = p;
            ps.EconomySignal = 0;
            ps.InitialPrice = p;
            AddHistory(ps, day);
        }

        /// <summary>World values of every open instrument that has data today.</summary>
        private Dictionary<string, decimal> ObserveFundamentals(WorldSnapshot world)
        {
            var observed = new Dictionary<string, decimal>();
            foreach (InstrumentDefinition def in Instruments)
            {
                PriceState ps = _byId[def.Id];
                if (ps.Closed) continue;
                decimal? f = ComputeFundamental(def, world);
                if (f != null && f > 0) observed[def.Id] = f.Value;
            }
            return observed;
        }

        /// <summary>
        /// Median daily move per instrument class: the part of a move that is the whole economy getting dearer or
        /// cheaper (e.g. a new campaign's stock boom and bust). It is taken out of E (ADR-022).
        /// </summary>
        private Dictionary<InstrumentClass, double> CommonMoves(Dictionary<string, decimal> observed)
        {
            var moves = new Dictionary<InstrumentClass, List<double>>();
            foreach (InstrumentDefinition def in Instruments)
            {
                PriceState ps = _byId[def.Id];
                if (!ps.HasPrice || ps.RawFundamental <= 0 || !observed.TryGetValue(def.Id, out decimal f)) continue;
                if (!moves.TryGetValue(def.Class, out List<double> list)) moves[def.Class] = list = new List<double>();
                list.Add(DailyMove(ps.RawFundamental, f));
            }
            var common = new Dictionary<InstrumentClass, double>();
            foreach (KeyValuePair<InstrumentClass, List<double>> kv in moves) common[kv.Key] = Median(kv.Value);
            return common;
        }

        /// <summary>
        /// F = anchor x e^E (ADR-022). E carries the instrument's own move relative to its class, decays with
        /// economyHalfLifeDays and stays within economyMaxAbs; the anchor drifts slowly towards the town price.
        /// With economyWeight 0 the model is off and F is the observed value.
        /// </summary>
        private void UpdateFundamental(PriceState ps, InstrumentClass cls, decimal observed, Dictionary<InstrumentClass, double> common)
        {
            double weight = _settings.EconomyWeight;
            if (weight <= 0)
            {
                ps.RawFundamental = observed;
                ps.Fundamental = observed;
                return;
            }

            if (ps.Anchor <= 0) ps.Anchor = ps.Fundamental > 0 ? ps.Fundamental : observed;
            if (ps.RawFundamental > 0)
            {
                double relative = DailyMove(ps.RawFundamental, observed) - (common.TryGetValue(cls, out double c) ? c : 0);
                double decay = _settings.EconomyHalfLifeDays > 0 ? PriceMath.DecayFactor(_settings.EconomyHalfLifeDays) : 0;
                double max = _settings.EconomyMaxAbs;
                ps.EconomySignal = Math.Max(-max, Math.Min(max, decay * ps.EconomySignal + weight * relative));
            }
            if (_settings.AnchorHalfLifeDays > 0)
            {
                double step = 1 - PriceMath.DecayFactor(_settings.AnchorHalfLifeDays);
                double log = Math.Log((double)ps.Anchor);
                ps.Anchor = PriceMath.Round(Math.Exp(log + step * (Math.Log((double)observed) - log)));
            }
            ps.RawFundamental = observed;
            ps.Fundamental = PriceMath.Round((double)ps.Anchor * Math.Exp(ps.EconomySignal));
        }

        private double DailyMove(decimal previous, decimal now)
        {
            double move = Math.Log((double)now / (double)previous);
            double max = _settings.EconomyMaxDailyMove;
            return max > 0 ? Math.Max(-max, Math.Min(max, move)) : move;
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0) return 0;
            values.Sort();
            int n = values.Count;
            return n % 2 == 1 ? values[n / 2] : (values[n / 2 - 1] + values[n / 2]) / 2;
        }

        private void UpdateBaselines(WorldSnapshot world)
        {
            foreach (string item in _catalog.RequiredItemIds)
            {
                if (State.BaseItemPrices.ContainsKey(item)) continue;
                decimal? avg = FundamentalCalculator.AverageItemPrice(world, item);
                if (avg != null && avg > 0) State.BaseItemPrices[item] = avg.Value;
            }
            foreach (string cat in _catalog.RequiredCategoryIds)
            {
                if (State.BaseCategoryFactors.ContainsKey(cat)) continue;
                double? avg = FundamentalCalculator.AverageCategoryFactor(world, cat);
                if (avg != null && avg > 0) State.BaseCategoryFactors[cat] = avg.Value;
            }
        }

        private void DetectGeneratedEvents(WorldSnapshot world, TickSummary summary)
        {
            if (State.LastSeason != null && State.LastSeason != world.Season)
            {
                var ev = new MarketEvent { Type = MarketEventType.SeasonChanged, Day = world.Day, Season = world.Season };
                ApplyEvent(ev);
                summary.GeneratedEvents.Add(ev);
            }
            State.LastSeason = world.Season;

            double totalProsperity = 0;
            foreach (TownSnapshot town in world.Towns) totalProsperity += Math.Max(0, town.Prosperity);

            foreach (TownSnapshot town in world.Towns)
            {
                bool starving = town.FoodStocks <= 0;
                bool known = State.FamineTowns.Contains(town.Id);
                if (starving && !known)
                {
                    State.FamineTowns.Add(town.Id);
                    bool coolingDown = State.FamineStartDays.TryGetValue(town.Id, out int last) && world.Day - last < _settings.FamineCooldownDays;
                    if (coolingDown) continue;

                    State.FamineStartDays[town.Id] = world.Day;
                    var ev = new MarketEvent
                    {
                        Type = MarketEventType.FamineStarted,
                        Day = world.Day,
                        TownId = town.Id,
                        KingdomId = town.KingdomId,
                        TownShare = totalProsperity > 0 ? Math.Max(0, town.Prosperity) / totalProsperity : 0,
                    };
                    ApplyEvent(ev);
                    summary.GeneratedEvents.Add(ev);
                }
                else if (!starving && known)
                {
                    State.FamineTowns.Remove(town.Id);
                }
            }
        }

        /// <summary>The part of an impulse that still fits under the rule's maxStack on this instrument.</summary>
        private static double StackLimited(PriceState ps, string source, double impulse, double maxStack)
        {
            if (maxStack <= 0 || impulse == 0) return impulse;
            double live = 0;
            foreach (ShockComponent c in ps.Shocks)
            {
                if (c.Source == source && Math.Sign(c.Value) == Math.Sign(impulse)) live += c.Value;
            }
            double room = impulse * maxStack - live;
            if (Math.Sign(room) != Math.Sign(impulse)) return 0;
            double added = Math.Abs(room) < Math.Abs(impulse) ? room : impulse;
            return Math.Abs(added) < NegligibleShock ? 0 : added;
        }

        private static double DecayAndSumShocks(PriceState ps)
        {
            double sum = 0;
            for (int i = ps.Shocks.Count - 1; i >= 0; i--)
            {
                ShockComponent c = ps.Shocks[i];
                if (c.Pending) c.Pending = false;
                else if (c.HoldKey == null) c.Value *= PriceMath.DecayFactor(c.HalfLifeDays);

                if (c.HoldKey == null && Math.Abs(c.Value) < NegligibleShock) ps.Shocks.RemoveAt(i);
                else sum += c.Value;
            }
            return sum;
        }

        /// <summary>Raw shock sum minus its slow baseline (ADR-019). The baseline moves after it is applied.</summary>
        private double EffectiveShock(PriceState ps, double raw)
        {
            double halfLife = _settings.ShockBaselineHalfLifeDays;
            if (halfLife <= 0) return raw;
            double effective = raw - ps.ShockBaseline;
            ps.ShockBaseline += (raw - ps.ShockBaseline) * (1 - PriceMath.DecayFactor(halfLife));
            return effective;
        }

        private double ActiveVolMultiplier(PriceState ps, int day)
        {
            ps.VolBoosts.RemoveAll(b => b.UntilDay < day);
            double mult = 1.0;
            foreach (VolBoost b in ps.VolBoosts) mult = Math.Max(mult, b.Multiplier);
            return mult;
        }

        private double ClampShock(double s)
        {
            return Math.Max(-_settings.MaxAbsShock, Math.Min(_settings.MaxAbsShock, s));
        }

        private void AddHistory(PriceState ps, int day)
        {
            ps.History.Add(new PricePoint { Day = day, P = ps.Price, F = ps.Fundamental, S = Math.Round(ps.Shock, 5) });
            int excess = ps.History.Count - _settings.HistoryDays;
            if (excess > 0) ps.History.RemoveRange(0, excess);
        }

        private void ReleaseHolds(string holdKey)
        {
            if (holdKey == null) return;
            foreach (PriceState ps in State.Prices)
            {
                foreach (ShockComponent c in ps.Shocks)
                {
                    if (c.HoldKey == holdKey) c.HoldKey = null;
                }
            }
        }

        private void CloseKingdom(string kingdomId, List<string> affected)
        {
            if (kingdomId == null) return;
            foreach (InstrumentDefinition def in Instruments)
            {
                if (def.Class != InstrumentClass.Kingdom || def.KingdomId != kingdomId) continue;
                PriceState ps = Get(def.Id);
                if (ps == null || ps.Closed) continue;
                ps.Closed = true;
                ps.Price = 0;
                ps.Fundamental = 0;
                affected.Add(def.Id);
            }
        }

        private void AddNews(MarketEvent ev, List<string> affected)
        {
            if (_settings.NewsKeep <= 0) return;
            State.News.Add(new NewsItem { Day = ev.Day, Event = ev, AffectedInstruments = new List<string>(affected) });
            int excess = State.News.Count - _settings.NewsKeep;
            if (excess > 0) State.News.RemoveRange(0, excess);
        }

        /// <summary>Drops prices and baselines so the next tick starts the market again from the current world. News is kept.</summary>
        private void Rebuild()
        {
            State.Prices.Clear();
            State.BaseItemPrices.Clear();
            State.BaseCategoryFactors.Clear();
            State.Initialized = false;
        }

        /// <summary>Adds price states for new catalog entries; keeps states of removed ones (harmless, preserves saves).</summary>
        private void SyncPriceStates()
        {
            _byId.Clear();
            foreach (PriceState ps in State.Prices)
            {
                if (ps?.InstrumentId != null) _byId[ps.InstrumentId] = ps;
            }
            foreach (InstrumentDefinition def in Instruments)
            {
                if (_byId.ContainsKey(def.Id)) continue;
                var ps = new PriceState { InstrumentId = def.Id };
                State.Prices.Add(ps);
                _byId[def.Id] = ps;
            }
        }

        /// <summary>
        /// Rebuilds the guild instrument list: every series of an eligible workshop type that has a price state already
        /// or at least minListingDays of profit data (PLAN 2.2). Ordered by id for determinism.
        /// </summary>
        private void RefreshGuilds()
        {
            _guildDefs.Clear();
            _guildById.Clear();
            _seriesById.Clear();
            int minDays = _settings.Guilds?.MinListingDays ?? 7;
            var known = new HashSet<string>(State.Prices.Where(p => p?.InstrumentId != null).Select(p => p.InstrumentId));
            foreach (GuildProfitSeries series in State.Guilds.OrderBy(g => GuildInstruments.IdFor(g.TownId, g.WorkshopTypeId), StringComparer.Ordinal))
            {
                GuildTypeDefinition type = _catalog.FindGuildType(series.WorkshopTypeId);
                if (type == null || string.IsNullOrEmpty(series.TownId)) continue;
                string id = GuildInstruments.IdFor(series.TownId, series.WorkshopTypeId);
                if (!known.Contains(id) && series.DailyProfit.Count < minDays) continue;
                InstrumentDefinition def = GuildInstruments.Define(series, type);
                _guildDefs.Add(def);
                _guildById[id] = def;
                _seriesById[id] = series;
            }
        }

        /// <summary>
        /// Closes a guild whose last workshop is gone (positions are paid out at the last price by the caller, PLAN 2.3)
        /// and reopens it when workshops of the type come back: it then starts from a fresh price.
        /// </summary>
        private void UpdateGuildLifecycle(InstrumentDefinition def, PriceState ps, TickSummary summary)
        {
            GuildProfitSeries series = GuildSeries(def.Id);
            if (series == null) return;
            if (series.WorkshopCount <= 0 && !ps.Closed && ps.HasPrice)
            {
                ps.Closed = true;
                ps.Shocks.Clear();
                ps.VolBoosts.Clear();
                summary.ClosedInstruments.Add(def.Id);
            }
            else if (series.WorkshopCount > 0 && ps.Closed)
            {
                ps.Closed = false;
                ps.HasPrice = false;
                summary.ReopenedInstruments.Add(def.Id);
            }
        }

        private void SaveRng()
        {
            State.Rng = _rng.GetState();
        }
    }
}
