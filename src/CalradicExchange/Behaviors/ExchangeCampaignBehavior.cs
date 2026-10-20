using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalradicExchange.Adapters;
using CalradicExchange.Core;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Guilds;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using CalradicExchange.Diagnostics;
using CalradicExchange.Localization;
using CalradicExchange.Menus;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradicExchange.Behaviors
{
    /// <summary>
    /// The single campaign behavior of the mod. Owns the persisted state (one JSON string, ADR-002),
    /// runs the daily market tick, receives world events and serves the menus.
    /// Every handler is wrapped in try/catch (CLAUDE.md rule 6).
    /// </summary>
    public sealed class ExchangeCampaignBehavior : CampaignBehaviorBase
    {
        /// <summary>Notify the player about a held instrument moving this much in a day.</summary>
        private const double HeldMoveNotice = 0.03;

        /// <summary>Notify about any instrument moving this much in a day.</summary>
        private const double AnyMoveNotice = 0.08;

        private const int MaxMoveNotices = 4;

        private ExchangeState _state;
        private InstrumentCatalog _catalog;
        private MarketSettings _settings;
        private MarketEngine _market;
        private OrderEngine _orders;
        private GuildPayouts _guildPayouts;

        public GuildPayouts GuildPayouts => _guildPayouts;
        private WorldReader _reader;

        public static ExchangeCampaignBehavior Current => Campaign.Current?.GetCampaignBehavior<ExchangeCampaignBehavior>();

        public ExchangeState State => _state;
        public InstrumentCatalog Catalog => _catalog;
        public MarketEngine Market => _market;
        public OrderEngine Orders => _orders;
        public Portfolio Portfolio => _state?.Portfolio;

        /// <summary>True when data files loaded and the market has been initialized.</summary>
        public bool IsReady => _market != null && _state?.Market?.Initialized == true;

        /// <summary>Set when the exchange cannot run (e.g. bad data files). Shown in the menu.</summary>
        public string DisabledReason { get; private set; }

        /// <summary>Exchange day the next tick will process; events received now belong to it.</summary>
        public int NextDay => (_state?.Market?.LastTickDay ?? -1) + 1;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, starter =>
            {
                try { ExchangeMenus.AddVisitOption(starter); }
                catch (Exception ex) { ExchangeLog.Error("Town option failed", ex); }
            });
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            EventBridge.Register(this, OnWorldEvent);
            CampaignEvents.WorkshopOwnerChangedEvent.AddNonSerializedListener(this, (workshop, oldOwner) =>
            {
                if (workshop?.Owner == Hero.MainHero) SellOwnWorkshopShares("bought");
            });
            CampaignEvents.WorkshopTypeChangedEvent.AddNonSerializedListener(this, workshop =>
            {
                if (workshop?.Owner == Hero.MainHero) SellOwnWorkshopShares("type changed");
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                // The state is saved as several short strings (ADR-028): one longer than 32767 bytes corrupts the save.
                if (dataStore.IsSaving)
                {
                    EnsureState();
                    string json = StateSerializer.Serialize(_state);
                    List<string> chunks = SaveChunks.Pack(json);
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        string chunk = chunks[i];
                        dataStore.SyncData(ChunkKey(i), ref chunk);
                    }
                    ExchangeLog.Info($"State saved ({json.Length} chars in {chunks.Count} chunk(s) of {chunks.Max(c => c.Length)} chars max, schema {_state.SchemaVersion}, exchange day {_state.Market.LastTickDay}, positions {_state.Portfolio.Positions.Count}).");
                }
                else if (dataStore.IsLoading)
                {
                    string first = null;
                    dataStore.SyncData(ChunkKey(0), ref first);
                    var chunks = new List<string> { first };
                    for (int i = 1; i < SaveChunks.PartCount(first); i++)
                    {
                        string chunk = null;
                        dataStore.SyncData(ChunkKey(i), ref chunk);
                        chunks.Add(chunk);
                    }
                    string json = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(first)) json = SaveChunks.Unpack(chunks);
                    }
                    catch (StateFormatException ex)
                    {
                        ExchangeLog.Error("Saved exchange state chunks could not be joined; starting fresh.", ex);
                    }
                    LoadState(json);
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("SyncData failed", ex);
            }
        }

        /// <summary>Chunk 0 keeps the original key, so saves from before ADR-028 (one plain JSON string) still load.</summary>
        private static string ChunkKey(int index) => index == 0 ? CoreInfo.SaveKey : CoreInfo.SaveKey + "_" + index;

        public TraderProfile CurrentTrader()
        {
            Hero hero = Hero.MainHero;
            Settlement here = Settlement.CurrentSettlement;
            var trader = new TraderProfile(hero.Gold, Clan.PlayerClan?.Tier ?? 0, hero.GetSkillValue(DefaultSkills.Trade),
                here != null && here.IsTown ? here.StringId : null);
            foreach (string id in OwnGuildIds()) trader.OwnGuilds.Add(id);
            return trader;
        }

        /// <summary>Guild ids of the main hero's workshops (town + workshop type).</summary>
        private static IEnumerable<string> OwnGuildIds()
        {
            var workshops = Hero.MainHero?.OwnedWorkshops;
            if (workshops == null) yield break;
            foreach (Workshop w in workshops)
            {
                if (w?.Settlement == null || w.WorkshopType == null) continue;
                yield return GuildInstruments.IdFor(w.Settlement.StringId, w.WorkshopType.StringId);
            }
        }

        /// <summary>
        /// The player may not hold partnership shares of a guild they own a workshop of (ADR-034). After buying a
        /// workshop or changing its type, those shares are sold at the current price, without commission or tax.
        /// </summary>
        private void SellOwnWorkshopShares(string reason)
        {
            try
            {
                if (_market == null || _state?.Portfolio == null) return;
                int day = NextDay;
                foreach (string id in OwnGuildIds().Distinct().ToList())
                {
                    int qty = _state.Portfolio.QuantityOf(id);
                    if (qty <= 0) continue;
                    PriceState ps = _market.Get(id);
                    decimal price = ps != null && ps.HasPrice ? ps.Price : 0;
                    decimal cost = _state.Portfolio.BasisFor(id, qty);
                    int proceeds = _state.Portfolio.ForceSell(id, qty, price, day, _market.Settings.Trading.TradeHistoryLimit);
                    if (proceeds > 0) GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, proceeds, true);
                    TextObject msg = Strings.OwnWorkshopSold;
                    msg.SetTextVariable("NAME", Names.Instrument(_market.Find(id)));
                    msg.SetTextVariable("QTY", qty);
                    msg.SetTextVariable("TOTAL", proceeds);
                    msg.SetTextVariable("PNL", Fmt.SignedDenars(proceeds - cost));
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Yellow));
                    ExchangeLog.Info($"Own workshop ({reason}): sold {qty} {id} @ {price:0.####} for {proceeds} (cost {cost:0}).");
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Selling own workshop shares failed", ex);
            }
        }

        /// <summary>Applies an event now (also used by the console). Returns affected instrument ids.</summary>
        public IReadOnlyList<string> ApplyEvent(MarketEvent ev)
        {
            if (_market == null || ev == null) return new string[0];
            ev.Day = NextDay;
            IReadOnlyList<string> affected = _market.ApplyEvent(ev);
            ExchangeLog.Info($"Event {ev} -> {(affected.Count == 0 ? "no instruments" : string.Join(", ", affected))}");
            if (ev.Type == MarketEventType.KingdomDestroyed) WriteOffFallenKingdoms(ev.Day);
            return affected;
        }

        /// <summary>Runs one market tick now (daily tick, first start, console exchange.tick).</summary>
        public TickSummary RunTick()
        {
            if (_market == null) return null;
            if (_market.NeedsRebase) SettleBeforeRebase();
            WorldSnapshot world = _reader.Read(NextDay);
            TickSummary summary = _market.Tick(world);
            _state.LastTickDay = _state.Market.LastTickDay;
            ExchangeLog.Info($"Daily tick {summary} (towns {world.Towns.Count}, kingdoms {world.KingdomCount}, wars {world.ActiveWarPairs}, season {world.Season})");
            foreach (MarketEvent ev in summary.GeneratedEvents) ExchangeLog.Info($"Generated event {ev}");
            if (summary.Processed && !summary.Initialized) NotifyWhales(summary);
            if (summary.Processed)
            {
                ExchangeLog.Info("Guilds: " + GuildReport.Summary(_state.Market.Guilds));
                ExchangeLog.Info("Kingdoms: " + KingdomReport(world));
                ExchangeLog.Info($"Workshops: {_reader.LastMeasured} measured, {_reader.LastUnmeasured} not measured, net profit {_reader.LastNetTotal:0} vs owner income {_reader.LastIncomeTotal:0} (all workshops).");
            }
            if (summary.Processed && !summary.Initialized)
            {
                try
                {
                    _guildPayouts?.AfterTick(summary, world);
                }
                catch (Exception ex)
                {
                    ExchangeLog.Error("Guild payouts failed", ex);
                }
            }
            if (summary.Processed) WriteOffFallenKingdoms(summary.Day);
            if (summary.Processed) SellOwnWorkshopShares("daily check");
            if (summary.Processed) RecordPerformance(summary.Day);
            if (summary.Processed && !summary.Initialized) NotifyBigMoves();
            if (summary.Processed && _state.Market.LastTickDay <= MarketDump.AutoDumpDays) MarketDump.Append(this);
            return summary;
        }

        /// <summary>
        /// The saved prices were built with an older fundamental method (ADR-017). Open positions are bought back
        /// at the current market price, without fees or tax, before the market is rebuilt.
        /// </summary>
        private void SettleBeforeRebase()
        {
            int positions = _state.Portfolio.Positions.Count;
            int gold = _state.Portfolio.CloseAllAt(id => _market.Get(id)?.Price ?? 0);
            ExchangeLog.Info($"Rebase: fundamental method '{_state.Market.FundamentalMethod}' -> '{MarketEngine.FundamentalMethodId}'. Closed {positions} positions for {gold} denars.");
            if (gold > 0) GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, true);
            if (positions > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(Strings.Rebased.SetTextVariable("GOLD", gold).ToString(), Colors.Yellow));
            }
        }

        private void LoadState(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                ExchangeLog.Info("No exchange state in this save (new game or mod added mid-campaign).");
                return;
            }

            try
            {
                _state = StateSerializer.Deserialize(json);
                ExchangeLog.Info($"State loaded: schema {_state.SchemaVersion}, exchange day {_state.Market.LastTickDay}, initialized {_state.Market.Initialized}, positions {_state.Portfolio.Positions.Count}, writtenBy {_state.WrittenByVersion}.");
            }
            catch (StateFormatException ex)
            {
                // Keep the campaign alive: start a fresh exchange instead of failing the load.
                ExchangeLog.Error("Saved exchange state could not be read; starting fresh.", ex);
                _state = null;
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                EnsureState();
                StartMarket();
                ExchangeMenus.AddTo(starter);
                ExchangeLog.Info($"Session launched. Exchange day {NextDay}, ready {IsReady}.");
                // In a new campaign the character is created after this point; fees use the skill at trade time.
                if (IsReady && Hero.MainHero != null)
                {
                    TraderProfile t = CurrentTrader();
                    ExchangeLog.Info($"Trader now: trade skill {t.TradeSkill} -> commission {Orders.CommissionRate:P2}, clan tier {t.ClanTier}, limit {Orders.TotalLimit(t)}.");
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("OnSessionLaunched failed", ex);
            }
        }

        private void StartMarket()
        {
            try
            {
                string dir = ModulePaths.ModuleDataFolder;
                _catalog = CatalogLoader.Parse(File.ReadAllText(Path.Combine(dir, CatalogLoader.FileName)));
                _settings = SettingsLoader.Parse(File.ReadAllText(Path.Combine(dir, SettingsLoader.FileName)));
            }
            catch (Exception ex) when (ex is ConfigException || ex is IOException || ex is UnauthorizedAccessException)
            {
                DisabledReason = Strings.ClosedReasonData.ToString();
                ExchangeLog.Error("Exchange disabled: data files could not be loaded.", ex);
                return;
            }

            // ADR-004: reseed from entropy on every load so reloading does not replay tomorrow's noise.
            var rng = new XorShift128Plus((ulong)DateTime.UtcNow.Ticks ^ ((ulong)Environment.TickCount << 32));
            _market = new MarketEngine(_catalog, _settings, _state.Market, rng);
            _market.PlayerShares = id => _state?.Portfolio?.QuantityOf(id) ?? 0; // free float (ADR-037)
            _orders = new OrderEngine(_market);
            _guildPayouts = new GuildPayouts(this);
            _reader = new WorldReader(_catalog);
            ExchangeLog.Info($"Market ready: {_catalog.Instruments.Count} instruments, history {_settings.HistoryDays} days.");

            // Mid-campaign install or new game: set opening prices from today's world right away (PLAN 1.21).
            // A save priced with an older fundamental method is rebuilt right away too (ADR-017).
            if (!_state.Market.Initialized || _market.NeedsRebase) RunTick();
        }

        private void OnDailyTick()
        {
            try
            {
                RunTick();
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("OnDailyTick failed", ex);
            }
        }

        private void OnWorldEvent(MarketEvent ev)
        {
            if (ev == null) return;
            if (_market == null)
            {
                ExchangeLog.Info($"Event ignored (market not running): {ev}");
                return;
            }
            ApplyEvent(ev);
        }

        /// <summary>
        /// Shares of a fallen kingdom's index are worth nothing (GDD 6.4, ADR-030): the position is closed at zero and
        /// recorded as a forced sale so the loss shows in the history and the realized profit.
        /// </summary>
        private void WriteOffFallenKingdoms(int day)
        {
            try
            {
                foreach (Position pos in _state.Portfolio.Positions.ToList())
                {
                    InstrumentDefinition def = _market.Find(pos.InstrumentId);
                    PriceState ps = _market.Get(pos.InstrumentId);
                    if (def?.Class != InstrumentClass.Kingdom || ps == null || !ps.Closed) continue;
                    int qty = pos.Quantity;
                    decimal cost = pos.CostBasis;
                    _state.Portfolio.ForceSell(def.Id, qty, 0, day, _market.Settings.Trading.TradeHistoryLimit);
                    TextObject msg = Strings.KingdomFallen;
                    msg.SetTextVariable("KINGDOM", Names.Kingdom(def.KingdomId));
                    msg.SetTextVariable("QTY", qty);
                    msg.SetTextVariable("LOSS", Fmt.Denars(cost));
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));
                    ExchangeLog.Info($"Kingdom fallen: {def.Id}, wrote off {qty} shares (cost {cost:0}).");
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Kingdom write-off failed", ex);
            }
        }

        /// <summary>One line per listed kingdom index for the daily log: price, fundamental, fiefs, prosperity, wars, sieges.</summary>
        /// <summary>Daily point of the portfolio performance chart (ADR-031), after payouts and write-offs.</summary>
        private void RecordPerformance(int day)
        {
            try
            {
                Portfolio pf = _state.Portfolio;
                pf.RecordPerformance(day, id => _market.Get(id) is PriceState ps && ps.HasPrice ? ps.Price : 0, PerformanceDaysKept);
                PerformancePoint p = pf.Performance[pf.Performance.Count - 1];
                ExchangeLog.Info($"Performance day {day}: total profit {p.Pnl:0}, value {p.Value:0}, points {pf.Performance.Count}.");
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Recording performance failed", ex);
            }
        }

        /// <summary>Days of performance kept in the save; the chart shows up to 30.</summary>
        private const int PerformanceDaysKept = 60;

        private string KingdomReport(WorldSnapshot world)
        {
            var parts = new List<string>();
            foreach (InstrumentDefinition def in _market.Kingdoms)
            {
                PriceState ps = _market.Get(def.Id);
                KingdomSnapshot k = world.Kingdoms.Find(x => x.Id == def.KingdomId);
                string state = k == null ? "fallen" : $"towns {k.Towns}, castles {k.Castles}, prosperity {k.Prosperity:0}, villages {k.Villages} hearth {k.VillageHearth:0} raided {k.VillagesRaided}, strength {k.Strength:0}, clans {k.Clans}, enemies {k.EnemyCount}, sieges {k.FiefsUnderSiege}";
                parts.Add($"{def.KingdomId} P {ps?.Price:0.0} F {ps?.Fundamental:0.0} ({state})");
            }
            return parts.Count == 0 ? "none listed" : string.Join("; ", parts);
        }

        /// <summary>A whale's trade is news (ADR-037); it is also shown as a message, like a big move.</summary>
        private static void NotifyWhales(TickSummary summary)
        {
            foreach (MarketEvent ev in summary.GeneratedEvents.Where(e => e.Type == MarketEventType.WhaleTrade))
            {
                InformationManager.DisplayMessage(new InformationMessage(MarketReport.Describe(ev), ev.Detail == "buy" ? Colors.Green : Colors.Red));
            }
        }

        private void NotifyBigMoves()
        {
            var notices = new List<(PriceState Price, InstrumentDefinition Def)>();
            // Guilds only when held: there are about 150 of them.
            foreach (InstrumentDefinition def in _catalog.Instruments.Concat(_market.Kingdoms).Concat(_market.Guilds.Where(g => _state.Portfolio.QuantityOf(g.Id) > 0)))
            {
                PriceState ps = _market.Get(def.Id);
                if (ps == null || !ps.HasPrice || ps.Closed) continue;
                double move = Math.Abs(ps.LastReturn);
                bool held = _state.Portfolio.QuantityOf(def.Id) > 0;
                if ((held && move >= HeldMoveNotice) || move >= AnyMoveNotice) notices.Add((ps, def));
            }

            foreach ((PriceState ps, InstrumentDefinition def) in notices.OrderByDescending(n => Math.Abs(n.Price.LastReturn)).Take(MaxMoveNotices))
            {
                TextObject t = Strings.BigMove;
                t.SetTextVariable("NAME", Names.Instrument(def));
                t.SetTextVariable("CHANGE", Fmt.Pct(Math.Exp(ps.LastReturn) - 1));
                t.SetTextVariable("PRICE", Fmt.Price(ps.Price));
                Color color = ps.LastReturn >= 0 ? Colors.Green : Colors.Red;
                InformationManager.DisplayMessage(new InformationMessage(t.ToString(), color));
            }
        }

        private void EnsureState()
        {
            if (_state != null) return;
            _state = ExchangeState.CreateNew((int)CampaignTime.Now.ToDays);
            ExchangeLog.Info($"New exchange state created (campaign day {_state.CreatedOnDay}).");
        }
    }
}
