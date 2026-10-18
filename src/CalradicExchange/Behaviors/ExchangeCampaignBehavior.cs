using System;
using CalradicExchange.Core;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Diagnostics;
using CalradicExchange.Menus;
using TaleWorlds.CampaignSystem;

namespace CalradicExchange.Behaviors
{
    /// <summary>
    /// The single campaign behavior of the mod. Owns the persisted state (one JSON string, ADR-002),
    /// registers menus and receives campaign events. Every handler is wrapped in try/catch.
    /// </summary>
    public sealed class ExchangeCampaignBehavior : CampaignBehaviorBase
    {
        private ExchangeState _state;

        public ExchangeState State => _state;

        public static ExchangeCampaignBehavior Current => Campaign.Current?.GetCampaignBehavior<ExchangeCampaignBehavior>();

        public static int CurrentDay => (int)CampaignTime.Now.ToDays;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
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
                    ExchangeLog.Info($"State saved ({json.Length} chars, schema {_state.SchemaVersion}, lastTickDay {_state.LastTickDay}).");
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
                ExchangeLog.Info($"State loaded: schema {_state.SchemaVersion}, createdOnDay {_state.CreatedOnDay}, lastTickDay {_state.LastTickDay}, writtenBy {_state.WrittenByVersion}.");
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
                ExchangeMenus.AddTo(starter);
                ExchangeLog.Info($"Session launched on day {CurrentDay}.");
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("OnSessionLaunched failed", ex);
            }
        }

        private void OnDailyTick()
        {
            try
            {
                EnsureState();
                _state.LastTickDay = CurrentDay;
                ExchangeLog.Info($"Daily tick day {_state.LastTickDay}: 0 instruments (Phase 0).");
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("OnDailyTick failed", ex);
            }
        }

        private void EnsureState()
        {
            if (_state != null) return;
            _state = ExchangeState.CreateNew(CurrentDay);
            ExchangeLog.Info($"New exchange state created on day {_state.CreatedOnDay}.");
        }
    }
}
