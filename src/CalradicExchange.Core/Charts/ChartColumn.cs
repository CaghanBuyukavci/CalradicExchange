namespace CalradicExchange.Core.Charts
{
    /// <summary>One thin vertical slice of a line chart: the bit of line crossing it and the fill down (or up) to zero.</summary>
    public sealed class ChartColumn
    {
        /// <summary>Distance from the top of the chart to the top of the line piece, in pixels.</summary>
        public float LineTop { get; set; }
        public float LineHeight { get; set; }
        public float FillTop { get; set; }
        public float FillHeight { get; set; }

        /// <summary>Value at the middle of the slice is zero or more.</summary>
        public bool Positive { get; set; }
    }
}
