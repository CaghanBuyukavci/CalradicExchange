namespace CalradicExchange.Core.Trading
{
    /// <summary>A long holding. Cost basis includes everything paid (price, spread, slippage, commission).</summary>
    public sealed class Position
    {
        public string InstrumentId { get; set; }
        public int Quantity { get; set; }
        public decimal CostBasis { get; set; }

        /// <summary>Accumulated dividends / coupons received (Phase 2+).</summary>
        public decimal IncomeReceived { get; set; }

        public decimal AverageCost => Quantity > 0 ? CostBasis / Quantity : 0;
    }
}
