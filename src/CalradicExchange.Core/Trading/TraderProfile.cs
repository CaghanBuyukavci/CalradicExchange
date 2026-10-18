namespace CalradicExchange.Core.Trading
{
    /// <summary>What the order engine needs to know about the trader (read from the main hero by the game layer).</summary>
    public sealed class TraderProfile
    {
        public TraderProfile(int gold, int clanTier, int tradeSkill)
        {
            Gold = gold;
            ClanTier = clanTier;
            TradeSkill = tradeSkill;
        }

        public int Gold { get; }
        public int ClanTier { get; }
        public int TradeSkill { get; }
    }
}
