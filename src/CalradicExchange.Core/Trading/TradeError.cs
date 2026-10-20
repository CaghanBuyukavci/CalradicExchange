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

        /// <summary>Guild shares are bought only in the guild's own town (ADR-026).</summary>
        NotInGuildTown,

        /// <summary>A guild has a fixed number of partnership shares (guilds.sharesPerGuild).</summary>
        GuildSharesExceeded,

        /// <summary>The trader owns a workshop of that guild: no partnership shares in one's own business (ADR-034).</summary>
        OwnWorkshop,

        /// <summary>Take-profit / stop-loss orders are not given on guild partnerships (ADR-039).</summary>
        OrdersNotForGuilds,

        /// <summary>A take-profit / stop-loss order needs at least one of the two targets.</summary>
        NoOrderTarget,

        /// <summary>Validity outside 1..orders.maxDays, or a target outside its slider range.</summary>
        InvalidOrder,
    }
}
