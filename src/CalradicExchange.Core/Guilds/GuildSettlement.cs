using System.Collections.Generic;

namespace CalradicExchange.Core.Guilds
{
    /// <summary>Outcome of one weekly guild payout (ADR-027). The game layer applies <see cref="GoldDelta"/>.</summary>
    public sealed class GuildSettlement
    {
        public int Day { get; set; }

        /// <summary>Gold to give (positive) or take (negative). Never takes more than the player has.</summary>
        public int GoldDelta { get; set; }

        /// <summary>Part of this week's profit that went to older debts.</summary>
        public int DebtRepaid { get; set; }

        public List<GuildSettlementLine> Lines { get; } = new List<GuildSettlementLine>();

        /// <summary>Debts still open after this payout, per guild instrument.</summary>
        public Dictionary<string, decimal> DebtsAfter { get; set; } = new Dictionary<string, decimal>();
    }
}
