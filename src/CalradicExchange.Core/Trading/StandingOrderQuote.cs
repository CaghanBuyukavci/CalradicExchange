namespace CalradicExchange.Core.Trading
{
    /// <summary>What placing a take-profit / stop-loss order would mean, for the order window (ADR-039).</summary>
    public sealed class StandingOrderQuote
    {
        public string InstrumentId { get; set; }
        public int Quantity { get; set; }
        public int Days { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal ReferenceCost { get; set; }
        public decimal? TakeProfitPrice { get; set; }
        public decimal? StopLossPrice { get; set; }
        public int Fee { get; set; }
        public TradeError Error { get; set; }
        public bool IsValid => Error == TradeError.None;
    }
}
