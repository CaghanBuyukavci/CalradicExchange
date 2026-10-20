using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Guilds;
using CalradicExchange.Core.Kingdoms;
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

        /// <summary>Kingdom indices built from <see cref="MarketState.Kingdoms"/> (PLAN 2.4), ordered by id.</summary>
        private readonly List<InstrumentDefinition> _kingdomDefs = new List<InstrumentDefinition>();
        private readonly Dictionary<string, InstrumentDefinition> _kingdomById = new Dictionary<string, InstrumentDefinition>();
        private readonly Dictionary<string, KingdomRecord> _recordById = new Dictionary<string, KingdomRecord>();

        public MarketEngine(InstrumentCatalog catalog, MarketSettings settings, MarketState state, IRandom rng)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            State = state ?? new MarketState();
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            RefreshGuilds();
            RefreshKingdoms();
            SyncPriceStates();
        }

        public MarketState State { get; }

        /// <summary>
        /// The player's holding of an instrument, for the free float (ADR-037). Set by the game layer and the
        /// simulator; null counts as no holding. Not saved.
        /// </summary>
        public Func<string, int> PlayerShares { get; set; }
        public InstrumentCatalog Catalog => _catalog;
        public MarketSettings Settings => _settings;

        /// <summary>
        /// Every listed instrument: the catalog in file order, then guilds, then kingdom indices, both ordered by id.
        /// Order matters for determinism.
        /// </summary>
        public IEnumerable<InstrumentDefinition> Instruments => _catalog.Instruments.Concat(_guildDefs).Concat(_kingdomDefs);

        /// <summary>Listed guild instruments (open and closed).</summary>
        public IReadOnlyList<InstrumentDefinition> Guilds => _guildDefs;

        /// <summary>Listed kingdom indices (open and fallen).</summary>
        public IReadOnlyList<InstrumentDefinition> Kingdoms => _kingdomDefs;

        public InstrumentDefinition Find(string instrumentId)
        {
            if (instrumentId == null) return null;
            if (_guildById.TryGetValue(instrumentId, out InstrumentDefinition guild)) return guild;
            if (_kingdomById.TryGetValue(instrumentId, out InstrumentDefinition kingdom)) return kingdom;
            return _catalog.Find(instrumentId);
        }

        /// <summary>Record behind a kingdom index; null for other instruments.</summary>
        public KingdomRecord KingdomRecord(string instrumentId)
        {
            return instrumentId != null && _recordById.TryGetValue(instrumentId, out KingdomRecord r) ? r : null;
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
                if (rule.HeroRank != null && ev.HeroRank != rule.HeroRank) continue;
                double stackUnit = impulse; // maxStack counts in unscaled impulses
                if (rule.ScaleByTownShare) impulse *= (ev.TownShare ?? 0) * rule.TownShareMultiplier;
                if (rule.ScaleByMagnitude) impulse *= ev.Magnitude ?? 1;

                foreach (InstrumentDefinition def in ShockMapper.Resolve(rule.Target, ev, Instruments, Find))
                {
                    PriceState ps = Get(def.Id);
                    if (ps == null || ps.Closed) continue;

                    string source = ev.Type + ":" + rule.Target + (rule.HeroRank != null ? ":" + rule.HeroRank : "");
                    double added = StackLimited(ps, source, impulse, stackUnit, rule.MaxStack);
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
            KingdomInstruments.Track(State.Kingdoms, world, KingdomParams);
            RefreshGuilds();
            RefreshKingdoms();
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
                EnsureSupply();
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
                if (def.Class == InstrumentClass.Kingdom && !ps.Closed && KingdomRecord(def.Id)?.Eliminated == true)
                {
                    CloseAtZero(ps);
                    summary.ClosedInstruments.Add(def.Id);
                }
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

            EnsureSupply();
            RunInvestors(world, summary);

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
                case InstrumentClass.Kingdom:
                    KingdomRecord record = KingdomRecord(def.Id);
                    KingdomSnapshot k = record == null || record.Eliminated ? null : world.Kingdoms?.Find(x => x.Id == record.KingdomId);
                    return k == null ? (decimal?)null : KingdomInstruments.ObservedValue(record, k, KingdomParams);
                default:
                    return null; // Bond fundamentals arrive in Phase 3.
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
            ps.TotalShares = 0; // issued again at the new price (EnsureSupply)
            ps.NpcShares = 0;
            AddHistory(ps, day);
        }

        /// <summary>
        /// Impact multiplier of an order taking <paramref name="taken"/> shares out of free float (negative for a
        /// sell), with the player holding <paramref name="playerShares"/> before the order (ADR-037).
        /// </summary>
        public double ImpactMultiplier(string instrumentId, int taken, int playerShares)
        {
            InstrumentDefinition def = Find(instrumentId);
            PriceState ps = Get(instrumentId);
            if (def == null || ps == null || _settings.Npc == null) return 1;
            return Liquidity.ImpactMultiplier(ps.TotalShares, ps.NpcShares, playerShares, taken,
                Liquidity.ReferenceFreeFraction(def.Class, _settings.Npc), _settings.Npc);
        }

        /// <summary>Issues shares for every priced instrument that has none yet (new listing, or a save from before schema 13).</summary>
        private void EnsureSupply()
        {
            NpcSettings npc = _settings.Npc;
            if (npc == null) return;
            foreach (InstrumentDefinition def in Instruments)
            {
                PriceState ps = _byId[def.Id];
                if (!ps.HasPrice || ps.TotalShares > 0 || ps.Price <= 0) continue;
                if (def.Class == InstrumentClass.Guild)
                {
                    ps.TotalShares = _settings.Guilds?.SharesPerGuild ?? 100;
                    ps.NpcShares = 0;
                    continue;
                }
                ps.TotalShares = Liquidity.SupplyFor(_settings.For(def.Class), npc, ps.Price);
                ps.NpcShares = Liquidity.TradedByInvestors(def.Class) ? (int)Math.Round(ps.TotalShares * npc.InitialHoldingFraction) : 0;
            }
        }

        /// <summary>
        /// The town's investors (ADR-037): every tradable instrument gets one net order a day (noise, trend following,
        /// and a pull of their holding back to its listing level), and now and then one whale trades a big block,
        /// which makes the news. Draws the same random numbers whatever the prices, for determinism.
        /// </summary>
        private void RunInvestors(WorldSnapshot world, TickSummary summary)
        {
            NpcSettings npc = _settings.Npc;
            if (npc == null || !npc.Enabled) return;

            var open = new List<InstrumentDefinition>();
            foreach (InstrumentDefinition def in Instruments)
            {
                if (!Liquidity.TradedByInvestors(def.Class)) continue;
                PriceState ps = _byId[def.Id];
                double eps = _rng.NextGaussian();
                if (ps.Closed || !ps.HasPrice || ps.TotalShares <= 0) continue;
                open.Add(def);

                double gap = (npc.InitialHoldingFraction * ps.TotalShares - ps.NpcShares) / ps.TotalShares;
                double fraction = npc.FlowNoise * eps + npc.FlowMomentum * ps.ChangeOver(5) + npc.HoldingReversion * gap;
                InvestorTrade(def, ps, fraction * _settings.For(def.Class).Depth, world.Day);
            }

            double roll = _rng.NextDouble();
            if (roll >= npc.WhaleDailyChance || open.Count == 0) return;
            double pick = _rng.NextDouble(), side = _rng.NextDouble(), size = _rng.NextDouble(), home = _rng.NextDouble();
            InstrumentDefinition target = open[Math.Min(open.Count - 1, (int)(pick * open.Count))];
            PriceState tps = _byId[target.Id];
            bool heavy = tps.NpcShares > npc.InitialHoldingFraction * 1.5 * tps.TotalShares;
            bool buy = side < (heavy ? 1 - npc.WhaleBuyChance : npc.WhaleBuyChance);
            double depths = npc.WhaleMinDepths + (npc.WhaleMaxDepths - npc.WhaleMinDepths) * size;
            string town = world.Towns.Count > 0 ? world.Towns[Math.Min(world.Towns.Count - 1, (int)(home * world.Towns.Count))].Id : null;
            MarketEvent whale = WhaleTrade(target.Id, buy, depths, town, world.Day);
            if (whale != null) summary.GeneratedEvents.Add(whale);
        }

        /// <summary>
        /// A whale buys or sells <paramref name="depths"/> x the class depth of <paramref name="instrumentId"/> and
        /// the news is recorded. Also used by the console (exchange.whale). Null when nothing could be traded.
        /// </summary>
        public MarketEvent WhaleTrade(string instrumentId, bool buy, double depths, string townId, int day)
        {
            InstrumentDefinition def = Find(instrumentId);
            PriceState ps = Get(instrumentId);
            if (def == null || ps == null || ps.Closed || !ps.HasPrice || ps.TotalShares <= 0 || !Liquidity.TradedByInvestors(def.Class)) return null;
            decimal before = ps.Price;
            int shares = InvestorTrade(def, ps, (buy ? 1 : -1) * depths * _settings.For(def.Class).Depth, day);
            if (shares == 0 || before <= 0) return null;
            var ev = new MarketEvent
            {
                Type = MarketEventType.WhaleTrade,
                Day = day,
                InstrumentId = def.Id,
                TownId = townId,
                Detail = shares > 0 ? "buy" : "sell",
                Amount = Math.Abs(shares),
                Magnitude = Math.Round((double)(ps.Price / before) - 1, 4),
            };
            AddNews(ev, new List<string> { def.Id });
            return ev;
        }

        /// <summary>
        /// Investors trade <paramref name="value"/> denars (negative: sell) at the market: buys stop at the free float,
        /// sells at their holding. Moves the price like a player order of the same size. Returns signed shares.
        /// </summary>
        private int InvestorTrade(InstrumentDefinition def, PriceState ps, double value, int day)
        {
            int shares = (int)Math.Round(value / (double)ps.Price);
            int player = PlayerShares?.Invoke(def.Id) ?? 0;
            if (shares > 0) shares = Math.Min(shares, Math.Max(0, ps.TotalShares - ps.NpcShares - player));
            else shares = Math.Max(shares, -ps.NpcShares);
            if (shares == 0) return 0;

            ClassParameters cp = _settings.For(def.Class);
            double multiplier = Liquidity.ImpactMultiplier(ps.TotalShares, ps.NpcShares, player, shares,
                Liquidity.ReferenceFreeFraction(def.Class, _settings.Npc), _settings.Npc);
            double impact = cp.Lambda * Math.Abs(shares) * (double)ps.Price / cp.Depth * multiplier;
            ps.NpcShares += shares;
            ApplyTradeImpact(def.Id, shares > 0, impact);
            if (ps.History.Count > 0 && ps.History[ps.History.Count - 1].Day == day) ps.History[ps.History.Count - 1].P = ps.Price;
            return shares;
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
        private static double StackLimited(PriceState ps, string source, double impulse, double unit, double maxStack)
        {
            if (maxStack <= 0 || impulse == 0 || unit == 0) return impulse;
            double live = 0;
            foreach (ShockComponent c in ps.Shocks)
            {
                if (c.Source == source && Math.Sign(c.Value) == Math.Sign(impulse)) live += c.Value;
            }
            double room = Math.Sign(impulse) * Math.Abs(unit) * maxStack - live;
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

        private KingdomSettings KingdomParams => _settings.Kingdoms ?? new KingdomSettings();

        /// <summary>A destroyed kingdom's index goes to zero at once and stays closed (GDD 6.4, ADR-030).</summary>
        private void CloseKingdom(string kingdomId, List<string> affected)
        {
            if (kingdomId == null) return;
            KingdomRecord record = State.Kingdoms.Find(r => r.KingdomId == kingdomId);
            if (record != null) record.Eliminated = true;
            foreach (InstrumentDefinition def in Instruments)
            {
                if (def.Class != InstrumentClass.Kingdom || def.KingdomId != kingdomId) continue;
                PriceState ps = Get(def.Id);
                if (ps == null || ps.Closed) continue;
                CloseAtZero(ps);
                affected.Add(def.Id);
            }
        }

        private static void CloseAtZero(PriceState ps)
        {
            ps.Closed = true;
            ps.Price = 0;
            ps.Fundamental = 0;
            ps.Shocks.Clear();
            ps.VolBoosts.Clear();
        }

        /// <summary>Rebuilds the kingdom index list from the listed records, ordered by id for determinism.</summary>
        private void RefreshKingdoms()
        {
            _kingdomDefs.Clear();
            _kingdomById.Clear();
            _recordById.Clear();
            foreach (KingdomRecord record in State.Kingdoms.Where(r => r.Listed && !string.IsNullOrEmpty(r.KingdomId))
                .OrderBy(r => KingdomInstruments.IdFor(r.KingdomId), StringComparer.Ordinal))
            {
                InstrumentDefinition def = KingdomInstruments.Define(record);
                _kingdomDefs.Add(def);
                _kingdomById[def.Id] = def;
                _recordById[def.Id] = record;
            }
        }

        private void AddNews(MarketEvent ev, List<string> affected)
        {
            if (_settings.NewsKeep <= 0 || ev.Quiet) return;
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
