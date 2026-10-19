namespace CalradicExchange.Core.Model
{
    /// <summary>All workshops of one type in one town on one day. Filled by the game layer (WorldReader).</summary>
    public sealed class WorkshopGroupSnapshot
    {
        public int Count { get; set; }

        /// <summary>Sum of the owners' daily workshop income (ADR-010), in denars.</summary>
        public double DailyProfit { get; set; }
    }
}
