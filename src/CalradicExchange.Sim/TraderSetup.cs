namespace CalradicExchange.Sim
{
    /// <summary>A player profile for the bots: clan tier, Trade skill and starting gold.</summary>
    public sealed class TraderSetup
    {
        /// <summary>Early game: clan tier 1, Trade 30 (limit 21,500). Used for the early-game income cap.</summary>
        public static readonly TraderSetup Early = new TraderSetup("early (tier 1, trade 30)", 1, 30, 25000);

        /// <summary>Mid game: clan tier 3, Trade 100 (limit 55,000).</summary>
        public static readonly TraderSetup Mid = new TraderSetup("mid (tier 3, trade 100)", 3, 100, 60000);

        /// <summary>Late game: clan tier 5, Trade 250 (limit 92,500).</summary>
        public static readonly TraderSetup Late = new TraderSetup("late (tier 5, trade 250)", 5, 250, 100000);

        public TraderSetup(string name, int clanTier, int tradeSkill, int gold)
        {
            Name = name;
            ClanTier = clanTier;
            TradeSkill = tradeSkill;
            Gold = gold;
        }

        public string Name { get; }
        public int ClanTier { get; }
        public int TradeSkill { get; }
        public int Gold { get; }
    }
}
