namespace CalradicExchange.Core.Model
{
    /// <summary>Plain copy of one living kingdom's state for a single day (PLAN 2.4, ADR-032). Filled by the game layer.</summary>
    public sealed class KingdomSnapshot
    {
        public string Id { get; set; }

        /// <summary>Sum of the prosperity of the kingdom's towns and castles.</summary>
        public double Prosperity { get; set; }

        /// <summary>Towns and castles the kingdom holds.</summary>
        public int FiefCount { get; set; }

        /// <summary>Towns among the fiefs. With <see cref="Castles"/> 0 too, the index weighs <see cref="FiefCount"/> instead.</summary>
        public int Towns { get; set; }

        public int Castles { get; set; }

        /// <summary>Towns and castles of the kingdom under siege today.</summary>
        public int FiefsUnderSiege { get; set; }

        /// <summary>Living kingdoms this kingdom is at war with.</summary>
        public int EnemyCount { get; set; }

        /// <summary>Villages bound to the kingdom's fiefs.</summary>
        public int Villages { get; set; }

        /// <summary>Sum of the hearths of those villages (village wealth and population).</summary>
        public double VillageHearth { get; set; }

        /// <summary>Villages looted or being raided today.</summary>
        public int VillagesRaided { get; set; }

        /// <summary>Military strength of all the kingdom's clans (Kingdom.CurrentTotalStrength).</summary>
        public double Strength { get; set; }

        /// <summary>Clans sworn to the kingdom, mercenaries not counted.</summary>
        public int Clans { get; set; }
    }
}
