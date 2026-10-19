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

        /// <summary>
        /// Market volume per item StringId (daily supply + units in store). Used as the averaging weight (ADR-017).
        /// Towns with zero volume quote theoretical prices nobody can trade at and are left out.
        /// </summary>
        public Dictionary<string, double> ItemVolumes { get; set; } = new Dictionary<string, double>();

        /// <summary>Market volume per item category StringId.</summary>
        public Dictionary<string, double> CategoryVolumes { get; set; } = new Dictionary<string, double>();

        /// <summary>Workshops grouped by workshop type StringId (PLAN 2.1). Hidden types are left out by the reader.</summary>
        public Dictionary<string, WorkshopGroupSnapshot> Workshops { get; set; } = new Dictionary<string, WorkshopGroupSnapshot>();
    }
}
