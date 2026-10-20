namespace CalradicExchange.Core.Trading
{
    /// <summary>Which target sold the shares of a filled order; None for an ordinary trade.</summary>
    public enum StandingOrderTrigger
    {
        None,
        TakeProfit,
        StopLoss,
    }
}
