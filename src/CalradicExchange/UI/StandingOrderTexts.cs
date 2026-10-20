namespace CalradicExchange.UI
{
    /// <summary>Texts of the order window for one quote (StandingOrderVM); Error is "" when the order can be placed.</summary>
    public sealed class StandingOrderTexts
    {
        public string Quantity { get; set; } = "";
        public string TakeProfit { get; set; } = "";
        public string StopLoss { get; set; } = "";
        public string Days { get; set; } = "";
        public string Fee { get; set; } = "";
        public string Error { get; set; } = "";
    }
}
