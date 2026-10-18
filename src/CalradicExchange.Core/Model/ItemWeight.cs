namespace CalradicExchange.Core.Model
{
    /// <summary>A trade good (game item StringId) and its weight inside a basket.</summary>
    public sealed class ItemWeight
    {
        public ItemWeight(string itemId, double weight)
        {
            ItemId = itemId;
            Weight = weight;
        }

        public string ItemId { get; }
        public double Weight { get; }
    }
}
