using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalradicExchange.Adapters;
using CalradicExchange.Core;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using CalradicExchange.Diagnostics;
using CalradicExchange.Localization;
using CalradicExchange.Menus;
using TaleWorlds.CampaignSystem;
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
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            EventBridge.Register(this, OnWorldEvent);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                string json = null;
                if (dataStore.IsSaving)
                {
                    EnsureState();
                    json = StateSerializer.Serialize(_state);
                }

                dataStore.SyncData(CoreInfo.SaveKey, ref json);

                if (dataStore.IsSaving)
                {
                    ExchangeLog.Info($"State saved ({json.Length} chars, schema {_state.SchemaVersion}, exchange day {_state.Market.LastTickDay}, positions {_state.Portfolio.Positions.Count}).");
                }
                else if (dataStore.IsLoading)
                {
                    LoadState(json);
                }
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("SyncData failed", ex);
            }
        }

        public TraderProfile CurrentTrader()
        {
            Hero hero = Hero.MainHero;
            return new TraderProfile(hero.Gold, Clan.PlayerClan?.Tier ?? 0, hero.GetSkillValue(DefaultSkills.Trade));
        }

        /// <summary>Applies an event now (also used by the console). Returns affected instrument ids.</summary>
        public IReadOnlyList<string> ApplyEvent(MarketEvent ev)
        {
            if (_market == null || ev == null) return new string[0];
            ev.Day = NextDay;
            IReadOnlyList<string> affected = _market.ApplyEvent(ev);
            ExchangeLog.Info($"Event {ev} -> {(affected.Count == 0 ? "no instruments" : string.Join(", ", affected))}");
            return affected;
        }

        /// <summary>Runs one market tick now (daily tick, first start, console exchange.tick).</summary>
        public TickSummary RunTick()
        {
            if (_market == null) return null;
            WorldSnapshot world = _reader.Read(NextDay);
            TickSummary summary = _market.Tick(world);
            _state.LastTickDay = _state.Market.LastTickDay;
            ExchangeLog.Info($"Daily tick {summary} (towns {world.Towns.Count}, kingdoms {world.KingdomCount}, wars {world.ActiveWarPairs}, season {world.Season})");
            foreach (MarketEvent ev in summary.GeneratedEvents) ExchangeLog.Info($"Generated event {ev}");
            if (summary.Processed && !summary.Initialized) NotifyBigMoves();
            return summary;
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
            _orders = new OrderEngine(_market);
            _reader = new WorldReader(_catalog);
            ExchangeLog.Info($"Market ready: {_catalog.Instruments.Count} instruments, history {_settings.HistoryDays} days.");

            // Mid-campaign install or new game: set opening prices from today's world right away (PLAN 1.21).
            if (!_state.Market.Initialized) RunTick();
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

        private void NotifyBigMoves()
        {
            var notices = new List<(PriceState Price, InstrumentDefinition Def)>();
            foreach (InstrumentDefinition def in _catalog.Instruments)
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
