namespace CalradicExchange.Core.Trading
{
    /// <summary>State of a take-profit / stop-loss order (ADR-039).</summary>
    public enum StandingOrderStatus
    {
        Pending,
        Filled,
        Canceled,
    }
}
