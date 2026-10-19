using TaleWorlds.CampaignSystem;

namespace CalradicExchange.Adapters
{
    /// <summary>One workshop at one reading: what the next reading needs to compute its net profit (ADR-027).</summary>
    public sealed class WorkshopReading
    {
        public WorkshopReading(string typeId, Hero owner, int capital, int ownerIncome)
        {
            TypeId = typeId;
            Owner = owner;
            Capital = capital;
            OwnerIncome = ownerIncome;
        }

        public string TypeId { get; }
        public Hero Owner { get; }
        public int Capital { get; }

        /// <summary>What the owner withdraws per day at this capital (ClanFinanceModel).</summary>
        public int OwnerIncome { get; }
    }
}
