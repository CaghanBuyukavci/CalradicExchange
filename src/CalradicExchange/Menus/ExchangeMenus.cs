using System;
using CalradicExchange.Diagnostics;
using CalradicExchange.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace CalradicExchange.Menus
{
    /// <summary>Adds the exchange entry to the town menu. Phase 0: placeholder message only.</summary>
    public static class ExchangeMenus
    {
        /// <summary>Vanilla town menu id (PlayerTownVisitCampaignBehavior, v1.4.8).</summary>
        public const string TownMenuId = "town";
        public const string VisitOptionId = "cx_visit_exchange";

        /// <summary>Position among the town options; vanilla puts "Go to alley" at 4.</summary>
        private const int VisitOptionIndex = 5;

        public static void AddTo(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(TownMenuId, VisitOptionId, Strings.MenuVisitExchange,
                OnVisitCondition, OnVisitConsequence, isLeave: false, index: VisitOptionIndex);
            ExchangeLog.Info("Town menu option added.");
        }

        private static bool OnVisitCondition(MenuCallbackArgs args)
        {
            try
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                Settlement settlement = Settlement.CurrentSettlement;
                return settlement != null && settlement.IsTown;
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Visit condition failed", ex);
                return false;
            }
        }

        private static void OnVisitConsequence(MenuCallbackArgs args)
        {
            try
            {
                ExchangeLog.Info($"Player opened the exchange in {Settlement.CurrentSettlement?.StringId}.");
                InformationManager.DisplayMessage(new InformationMessage(Strings.ComingSoon.ToString()));
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Visit consequence failed", ex);
            }
        }
    }
}
