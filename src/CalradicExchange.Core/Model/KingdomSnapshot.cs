namespace CalradicExchange.Core.Model
{
    /// <summary>Plain copy of one living kingdom's state for a single day (PLAN 2.4). Filled by the game layer.</summary>
    public sealed class KingdomSnapshot
    {
        public string Id { get; set; }

        /// <summary>Sum of the prosperity of the kingdom's towns and castles.</summary>
        public double Prosperity { get; set; }

        /// <summary>Towns and castles the kingdom holds.</summary>
        public int FiefCount { get; set; }

        /// <summary>Towns and castles of the kingdom under siege today.</summary>
        public int FiefsUnderSiege { get; set; }

        /// <summary>Living kingdoms this kingdom is at war with.</summary>
        public int EnemyCount { get; set; }
    }
}
