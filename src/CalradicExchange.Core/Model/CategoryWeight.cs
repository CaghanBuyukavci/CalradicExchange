namespace CalradicExchange.Core.Model
{
    /// <summary>An item category (game ItemCategory StringId) and its weight inside a basket.</summary>
    public sealed class CategoryWeight
    {
        public CategoryWeight(string categoryId, double weight)
        {
            CategoryId = categoryId;
            Weight = weight;
        }

        public string CategoryId { get; }
        public double Weight { get; }
    }
}
