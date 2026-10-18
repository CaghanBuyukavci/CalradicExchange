using System.Collections.Generic;

namespace CalradicExchange.Core.Model
{
    /// <summary>Plain copy of one town's state for a single day. Filled by the game layer (WorldReader).</summary>
    public sealed class TownSnapshot
    {
        public string Id { get; set; }
        public string KingdomId { get; set; }
        public double Prosperity { get; set; }
        public bool IsUnderSiege { get; set; }
        public double FoodStocks { get; set; }

        /// <summary>Mid market price per unit, keyed by item StringId (ADR-009).</summary>
        public Dictionary<string, decimal> ItemPrices { get; set; } = new Dictionary<string, decimal>();

        /// <summary>Market price factor per item category StringId.</summary>
        public Dictionary<string, double> CategoryFactors { get; set; } = new Dictionary<string, double>();
    }
}
