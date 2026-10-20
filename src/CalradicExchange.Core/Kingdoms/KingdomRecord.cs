namespace CalradicExchange.Core.Kingdoms
{
    /// <summary>
    /// What the exchange remembers about a kingdom (PLAN 2.4, schema 10): when it was first seen, its values on that
    /// day (the index base) and whether it has fallen. Persisted in <see cref="Pricing.MarketState.Kingdoms"/>.
    /// </summary>
    public sealed class KingdomRecord
    {
        public string KingdomId { get; set; }
        public int FirstSeenDay { get; set; }

        /// <summary>Day the index is listed from: the first day for kingdoms present when tracking began, later ones wait minListingDays.</summary>
        public int ListDay { get; set; }

        public bool Listed { get; set; }
        public double BaseProsperity { get; set; }
        public int BaseFiefs { get; set; }

        /// <summary>
        /// Bases of the parts added in schema 12 (ADR-032): weighted fiefs (towns count double), village hearths,
        /// military strength and clans. 0 = not set yet; the next reading sets it.
        /// </summary>
        public double BaseFiefScore { get; set; }
        public double BaseHearth { get; set; }
        public double BaseStrength { get; set; }
        public int BaseClans { get; set; }
        public int LastSeenDay { get; set; }

        /// <summary>The kingdom was destroyed. Its index is closed at zero and never reopens.</summary>
        public bool Eliminated { get; set; }
    }
}
