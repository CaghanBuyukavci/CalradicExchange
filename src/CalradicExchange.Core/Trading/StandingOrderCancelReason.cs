namespace CalradicExchange.Core.Trading
{
    /// <summary>Why an order ended without a sale. The locked shares go back to the portfolio; the fee is not refunded.</summary>
    public enum StandingOrderCancelReason
    {
        None,

        /// <summary>The player canceled it in the orders window.</summary>
        Player,

        /// <summary>Its validity ran out.</summary>
        Expired,

        /// <summary>The instrument closed, or the shares left the portfolio another way (forced sale, rebuild).</summary>
        Closed,
    }
}
