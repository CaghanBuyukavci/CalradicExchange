namespace CalradicExchange.Core.Guilds
{
    /// <summary>One guild's part of a weekly payout.</summary>
    public sealed class GuildSettlementLine
    {
        public string InstrumentId { get; set; }

        /// <summary>The player's share of the week's profit (negative: loss).</summary>
        public int Amount { get; set; }

        /// <summary>Shares sold without fees to cover the loss, and what they brought.</summary>
        public int ForcedQuantity { get; set; }
        public int ForcedProceeds { get; set; }

        /// <summary>Loss that could not be covered and was added to the guild's debt.</summary>
        public int NewDebt { get; set; }
    }
}
