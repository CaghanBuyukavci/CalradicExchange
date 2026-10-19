namespace CalradicExchange.Core.Trading
{
    /// <summary>What the order engine needs to know about the trader (read from the main hero by the game layer).</summary>
    public sealed class TraderProfile
    {
        public TraderProfile(int gold, int clanTier, int tradeSkill, string townId = null)
        {
            Gold = gold;
            ClanTier = clanTier;
            TradeSkill = tradeSkill;
            TownId = townId;
        }

        public int Gold { get; }
        public int ClanTier { get; }
        public int TradeSkill { get; }

        /// <summary>Settlement id of the town the trader is in; guild shares can only be bought there (ADR-026).</summary>
        public string TownId { get; }
    }
}
