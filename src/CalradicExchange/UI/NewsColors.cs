using CalradicExchange.Core.Model;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>Color of each news category in the news table, readable on the dark popup canvas.</summary>
    public static class NewsColors
    {
        private static readonly Color Raid = Color.ConvertStringToColor("#F0A050FF");
        private static readonly Color Siege = Color.ConvertStringToColor("#E86060FF");
        private static readonly Color Conquest = Color.ConvertStringToColor("#F08CC8FF");
        private static readonly Color War = Color.ConvertStringToColor("#B48CF0FF");
        private static readonly Color Peace = Color.ConvertStringToColor("#7CD67CFF");
        private static readonly Color Season = Color.ConvertStringToColor("#7CC4E8FF");
        private static readonly Color Famine = Color.ConvertStringToColor("#E8D060FF");
        private static readonly Color Fall = Color.ConvertStringToColor("#B0B0B0FF");
        private static readonly Color Captive = Color.ConvertStringToColor("#4FD1C5FF");

        public static Color For(MarketEventType type)
        {
            switch (type)
            {
                case MarketEventType.VillageLooted: return Raid;
                case MarketEventType.SiegeStarted:
                case MarketEventType.SiegeEnded: return Siege;
                case MarketEventType.SettlementCaptured: return Conquest;
                case MarketEventType.WarDeclared: return War;
                case MarketEventType.PeaceMade: return Peace;
                case MarketEventType.SeasonChanged: return Season;
                case MarketEventType.FamineStarted: return Famine;
                case MarketEventType.KingdomDestroyed: return Fall;
                case MarketEventType.HeroCaptured: return Captive;
                default: return TableColors.Neutral;
            }
        }
    }
}
