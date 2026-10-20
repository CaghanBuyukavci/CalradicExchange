namespace CalradicExchange.Core.Trading
{
    /// <summary>One executed trade, kept for the player's trade history (schema 6).</summary>
    public sealed class TradeRecord
    {
        /// <summary>Game day number passed in by the caller (the game layer uses whole campaign days).</summary>
        public int Day { get; set; }

        public string InstrumentId { get; set; }
        public OrderSide Side { get; set; }
        public int Quantity { get; set; }

        /// <summary>Per-share price after spread and slippage.</summary>
        public decimal ExecutionPrice { get; set; }

        /// <summary>Gold paid (buy) or received (sell), fees and tax included.</summary>
        public int Total { get; set; }

        public int Commission { get; set; }
        public int ProfitTax { get; set; }

        /// <summary>Realized profit of a sell; 0 for a buy.</summary>
        public decimal RealizedPnl { get; set; }

        /// <summary>Not a player order: guild closure or a sale to cover a guild loss (ADR-027). Schema 9.</summary>
        public bool Forced { get; set; }

        /// <summary>Sold by a take-profit / stop-loss order (ADR-039); None for an ordinary trade. Schema 14.</summary>
        public StandingOrderTrigger Order { get; set; }
    }
}
