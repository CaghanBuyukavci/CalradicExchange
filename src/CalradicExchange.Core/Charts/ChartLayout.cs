using System.Collections.Generic;

namespace CalradicExchange.Core.Charts
{
    /// <summary>Result of <see cref="LineChart.Layout"/>.</summary>
    public sealed class ChartLayout
    {
        public List<ChartColumn> Columns { get; } = new List<ChartColumn>();

        /// <summary>Distance from the top of the chart to the zero line.</summary>
        public float ZeroY { get; set; }

        /// <summary>Value range of the vertical axis (always includes zero).</summary>
        public double Min { get; set; }
        public double Max { get; set; }
    }
}
