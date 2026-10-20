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
        private static readonly Color Death = Color.ConvertStringToColor("#DCDCDCFF");
        private static readonly Color Battle = Color.ConvertStringToColor("#FF8A65FF");
        private static readonly Color WarEnd = Color.ConvertStringToColor("#9FB8F0FF");
        private static readonly Color Clan = Color.ConvertStringToColor("#C8C864FF");
        private static readonly Color Whale = Color.ConvertStringToColor("#5FA8FFFF");

        public static Color For(MarketEventType type)
        {
            switch (type)
            {
                case MarketEventType.VillageLooted: return Raid;
                case MarketEventType.SiegeStarted:
                case MarketEventType.SiegeEnded: return Siege;
                case MarketEventType.SettlementCaptured:
                case MarketEventType.CastleCaptured: return Conquest;
                case MarketEventType.HeroDied: return Death;
                case MarketEventType.BattleEnded: return Battle;
                case MarketEventType.WarEnded: return WarEnd;
                case MarketEventType.ClanChangedKingdom: return Clan;
                case MarketEventType.WhaleTrade: return Whale;
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
