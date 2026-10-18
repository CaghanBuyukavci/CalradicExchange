namespace CalradicExchange.Core.Trading
{
    /// <summary>Why an order cannot be executed. The game layer maps each value to a localized message.</summary>
    public enum TradeError
    {
        None,
        InvalidQuantity,
        UnknownInstrument,
        InstrumentClosed,
        NoPrice,
        InsufficientGold,
        InsufficientShares,
        TotalLimitExceeded,
        InstrumentLimitExceeded,
    }
}
