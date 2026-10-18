namespace CalradicExchange.Core.Pricing
{
    /// <summary>Temporary sigma multiplier from an event (e.g. x2 for 14 days after a town changes hands).</summary>
    public sealed class VolBoost
    {
        public double Multiplier { get; set; }

        /// <summary>Last day (inclusive) the boost applies.</summary>
        public int UntilDay { get; set; }
    }
}
