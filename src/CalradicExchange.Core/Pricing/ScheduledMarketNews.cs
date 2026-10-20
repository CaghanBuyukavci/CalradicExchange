namespace CalradicExchange.Core.Pricing
{
    /// <summary>Market news announced by a rumor, waiting for its day (ADR-040). Schema 15.</summary>
    public sealed class ScheduledMarketNews
    {
        public int Day { get; set; }
        public string TemplateId { get; set; }
        public string InstrumentId { get; set; }
        public double Impulse { get; set; }
    }
}
