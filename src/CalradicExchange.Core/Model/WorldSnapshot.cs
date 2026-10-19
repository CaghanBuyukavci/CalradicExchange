using System.Collections.Generic;

namespace CalradicExchange.Core.Model
{
    /// <summary>Everything Core needs from the world for one daily tick.</summary>
    public sealed class WorldSnapshot
    {
        /// <summary>Exchange day number (monotonic, one per daily tick).</summary>
        public int Day { get; set; }

        public CalendarInfo Calendar { get; set; } = CalendarInfo.Default;
        public Season Season { get; set; }
        public List<TownSnapshot> Towns { get; set; } = new List<TownSnapshot>();

        /// <summary>Kingdoms that are not eliminated.</summary>
        public int KingdomCount { get; set; }

        /// <summary>Number of kingdom pairs currently at war with each other.</summary>
        public int ActiveWarPairs { get; set; }

        /// <summary>Living kingdoms (PLAN 2.4). Empty in the simulator and when the reader fails; nothing closes then.</summary>
        public List<KingdomSnapshot> Kingdoms { get; set; } = new List<KingdomSnapshot>();
    }
}
