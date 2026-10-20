using System;
using CalradicExchange.Behaviors;
using CalradicExchange.Core.Model;
using CalradicExchange.Diagnostics;
using CalradicExchange.Localization;
using CalradicExchange.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace CalradicExchange.Menus
{
    /// <summary>
    /// Town menu entry and the exchange submenu (PLAN 1.18, ADR-006: menus and inquiries until the Gauntlet screen).
    /// </summary>
    public static class ExchangeMenus
    {
        /// <summary>Vanilla town menu id (PlayerTownVisitCampaignBehavior, v1.4.8).</summary>
        public const string TownMenuId = "town";
        public const string ExchangeMenuId = "cx_exchange";
        public const string VisitOptionId = "cx_visit_exchange";

        /// <summary>Vanilla "Trade" option of the town menu; the exchange goes right above it.</summary>
        private const string TradeOptionId = "trade";

        /// <summary>Position among the town options when "Trade" is not found.</summary>
        private const int VisitOptionIndex = 5;

        private const string MenuTextVariable = "CX_EXCHANGE_TEXT";

        /// <summary>
        /// The town option. Added on OnAfterSessionLaunchedEvent: some vanilla town options are added on
        /// OnSessionLaunchedEvent after ours, and an index beyond the list end puts the option first.
        /// </summary>
        public static void AddVisitOption(CampaignGameStarter starter)
        {
            int visitIndex = VisitIndex(starter);
            starter.AddGameMenuOption(TownMenuId, VisitOptionId, Strings.MenuVisitExchange,
                OnVisitCondition, args => Guard("visit", () => GameMenu.SwitchToMenu(ExchangeMenuId)), isLeave: false, index: visitIndex);
            ExchangeLog.Info($"Town option added at {visitIndex}.");
        }

        public static void AddTo(CampaignGameStarter starter)
        {
            starter.AddGameMenu(ExchangeMenuId, "{=!}{" + MenuTextVariable + "}", OnExchangeInit, GameMenu.MenuOverlayType.SettlementWithBoth);
            starter.AddGameMenuOption(ExchangeMenuId, "cx_markets", Strings.OptMarkets,
                args => Option(args, GameMenuOption.LeaveType.Trade), args => Guard("markets", TradeFlow.ShowMarkets));
            starter.AddGameMenuOption(ExchangeMenuId, "cx_portfolio", Strings.OptPortfolio,
                args => Option(args, GameMenuOption.LeaveType.Manage), args => Guard("portfolio", MarketReport.ShowPortfolio));
            starter.AddGameMenuOption(ExchangeMenuId, "cx_news", Strings.OptNews,
                args => Option(args, GameMenuOption.LeaveType.Submenu), args => Guard("news", MarketReport.ShowNews));
            starter.AddGameMenuOption(ExchangeMenuId, "cx_history", Strings.OptHistory,
                args => Option(args, GameMenuOption.LeaveType.Manage), args => Guard("history", TradeHistoryReport.Show));
            starter.AddGameMenuOption(ExchangeMenuId, "cx_leave", Strings.OptLeave,
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => Guard("leave", () => GameMenu.SwitchToMenu(TownMenuId)), isLeave: true);

            ExchangeLog.Info("Exchange menus added.");
        }

        /// <summary>
        /// Index of vanilla "Trade" in the town menu, so the exchange is listed right above it. The vanilla behaviors
        /// add their town options before our session launch; other mods may add theirs later.
        /// </summary>
        private static int VisitIndex(CampaignGameStarter starter)
        {
            int i = 0;
            foreach (GameMenuOption option in starter.GetPresumedGameMenu(TownMenuId).MenuOptions)
            {
                if (option.IdString == TradeOptionId) return i;
                i++;
            }
            ExchangeLog.Warn($"Town menu option '{TradeOptionId}' not found; the exchange goes to {VisitOptionIndex}.");
            return VisitOptionIndex;
        }

        /// <summary>Re-enters the exchange menu so its text shows fresh numbers.</summary>
        public static void Refresh()
        {
            try
            {
                if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == ExchangeMenuId) GameMenu.SwitchToMenu(ExchangeMenuId);
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Menu refresh failed", ex);
            }
        }

        private static bool OnVisitCondition(MenuCallbackArgs args)
        {
            try
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                Settlement settlement = Settlement.CurrentSettlement;
                return settlement != null && settlement.IsTown && ExchangeCampaignBehavior.Current != null;
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Visit condition failed", ex);
                return false;
            }
        }

        private static void OnExchangeInit(MenuCallbackArgs args)
        {
            try
            {
                if (MenuBackground.EnsureRegistered()) args.MenuContext.SetBackgroundMeshName(MenuBackground.SpriteName);
                MBTextManager.SetTextVariable(MenuTextVariable, MarketReport.MenuText());
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Exchange menu init failed", ex);
                MBTextManager.SetTextVariable(MenuTextVariable, "Calradic Exchange");
            }
        }

        private static bool Option(MenuCallbackArgs args, GameMenuOption.LeaveType icon)
        {
            args.optionLeaveType = icon;
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady)
            {
                args.IsEnabled = false;
                args.Tooltip = Strings.ExchangeClosedText.SetTextVariable("REASON", b?.DisabledReason ?? Strings.ClosedReasonPrices.ToString());
            }
            return true;
        }

        private static void Guard(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ExchangeLog.Error($"Menu action '{what}' failed", ex);
            }
        }
    }
}
