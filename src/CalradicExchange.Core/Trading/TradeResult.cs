namespace CalradicExchange.Core.Trading
{
    /// <summary>Outcome of an executed (or rejected) order. The game layer applies <see cref="GoldDelta"/> to the hero.</summary>
    public sealed class TradeResult
    {
        public TradeQuote Quote { get; set; }

        /// <summary>Negative for a buy, positive for a sell, 0 when rejected.</summary>
        public int GoldDelta { get; set; }

        /// <summary>Realized profit of a sell (net proceeds - cost basis of the sold shares).</summary>
        public decimal RealizedPnl { get; set; }

        public bool Success => Quote != null && Quote.IsValid;
    }
}
