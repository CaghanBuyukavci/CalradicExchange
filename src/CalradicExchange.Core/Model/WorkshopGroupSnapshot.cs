namespace CalradicExchange.Core.Model
{
    /// <summary>All workshops of one type in one town on one day. Filled by the game layer (WorldReader).</summary>
    public sealed class WorkshopGroupSnapshot
    {
        public int Count { get; set; }

        /// <summary>
        /// Net profit of the group's workshops since the previous reading, in denars; negative for a loss (ADR-027).
        /// Only meaningful when <see cref="HasProfit"/> is true.
        /// </summary>
        public double DailyProfit { get; set; }

        /// <summary>
        /// False when no workshop of the group could be measured today (first reading after a load, or every
        /// workshop changed owner or type). The count is still recorded; no profit is appended.
        /// </summary>
        public bool HasProfit { get; set; } = true;
    }
}
