namespace CalradicExchange.Core.Trading
{
    /// <summary>
    /// A take-profit / stop-loss sell order on held shares (ADR-039, schema 14). While pending, its shares stay in the
    /// position but are locked: they cannot be sold or put in another order. Checked after each daily price update.
    /// </summary>
    public sealed class StandingOrder
    {
        public int Id { get; set; }
        public string InstrumentId { get; set; }
        public int Quantity { get; set; }

        /// <summary>Average cost per share when the order was placed; the target percents are taken from it.</summary>
        public decimal ReferenceCost { get; set; }

        /// <summary>Target in percent of the reference cost; 0 = no take-profit.</summary>
        public int TakeProfitPercent { get; set; }

        /// <summary>Target in percent below the reference cost; 0 = no stop-loss.</summary>
        public int StopLossPercent { get; set; }

        /// <summary>Sell when the market price reaches this or more. Null: off.</summary>
        public decimal? TakeProfitPrice { get; set; }

        /// <summary>Sell when the market price falls to this or less. Null: off.</summary>
        public decimal? StopLossPrice { get; set; }

        public int PlacedDay { get; set; }

        /// <summary>Last day the order is checked; still pending after that day's check, it expires.</summary>
        public int ExpiryDay { get; set; }

        public int Fee { get; set; }

        public StandingOrderStatus Status { get; set; }
        public StandingOrderTrigger Trigger { get; set; }
        public StandingOrderCancelReason CancelReason { get; set; }

        /// <summary>Day it was filled or canceled.</summary>
        public int ClosedDay { get; set; }

        /// <summary>Filled only: per-share sale price, gold received (after profit tax) and realized profit.</summary>
        public decimal FillPrice { get; set; }
        public int Proceeds { get; set; }
        public decimal RealizedPnl { get; set; }

        public bool IsPending => Status == StandingOrderStatus.Pending;
    }
}
