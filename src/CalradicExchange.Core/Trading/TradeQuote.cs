namespace CalradicExchange.Core.Trading
{
    /// <summary>Full cost breakdown of an order, shown on the confirmation window before execution.</summary>
    public sealed class TradeQuote
    {
        public string InstrumentId { get; set; }
        public OrderSide Side { get; set; }
        public int Quantity { get; set; }

        /// <summary>Market (mid) price per share.</summary>
        public decimal MarketPrice { get; set; }

        /// <summary>Price per share after spread and slippage.</summary>
        public decimal ExecutionPrice { get; set; }

        /// <summary>Slippage fraction: lambda x order value / depth.</summary>
        public double Impact { get; set; }

        /// <summary>Quantity x execution price, rounded against the trader.</summary>
        public int Gross { get; set; }

        public int Commission { get; set; }

        /// <summary>Buy: gold to pay (gross + commission). Sell: gold received (gross - commission).</summary>
        public int Total { get; set; }

        public TradeError Error { get; set; }

        public bool IsValid => Error == TradeError.None;
    }
}
