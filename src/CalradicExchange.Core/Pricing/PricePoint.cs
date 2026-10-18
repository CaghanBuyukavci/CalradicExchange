namespace CalradicExchange.Core.Pricing
{
    /// <summary>One day of history: price, fundamental and sentiment.</summary>
    public sealed class PricePoint
    {
        public int Day { get; set; }
        public decimal P { get; set; }
        public decimal F { get; set; }
        public double S { get; set; }
    }
}
