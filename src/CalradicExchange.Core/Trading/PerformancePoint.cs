namespace CalradicExchange.Core.Trading
{
    /// <summary>One day of the portfolio performance chart (schema 11, ADR-031).</summary>
    public sealed class PerformancePoint
    {
        public int Day { get; set; }

        /// <summary>
        /// Total profit so far: open profit + realized profit + guild payouts + guild profit accrued but not yet paid.
        /// Buying and selling do not move it; only gains and losses do.
        /// </summary>
        public decimal Pnl { get; set; }

        /// <summary>Market value of the holdings that day.</summary>
        public decimal Value { get; set; }
    }
}
