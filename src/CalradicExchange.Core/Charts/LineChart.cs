using System;
using System.Collections.Generic;

namespace CalradicExchange.Core.Charts
{
    /// <summary>
    /// Lays out a line chart as thin vertical slices (PLAN 2.7, ADR-031), so Gauntlet can draw it with plain
    /// widgets: no rotation, no runtime textures. Points are joined by straight lines; x need not be evenly spaced.
    /// </summary>
    public static class LineChart
    {
        public static ChartLayout Layout(IReadOnlyList<double> xs, IReadOnlyList<double> ys, int columns, float height, float thickness)
        {
            if (xs == null || ys == null || xs.Count != ys.Count) throw new ArgumentException("xs and ys must have the same length.");
            var layout = new ChartLayout();
            if (xs.Count == 0 || columns <= 0 || height <= 0) return layout;

            double min = 0, max = 0;
            foreach (double y in ys)
            {
                min = Math.Min(min, y);
                max = Math.Max(max, y);
            }
            // The axis always includes zero; a margin is added only on the sides that have values.
            double pad = (max - min) * 0.08;
            if (min < 0) min -= pad;
            if (max > 0) max += pad;
            if (max - min < 1e-9) max = 1; // all zero: flat line on the bottom edge
            layout.Min = min;
            layout.Max = max;

            float half = thickness / 2;
            Func<double, float> toY = v => (float)((max - v) / (max - min) * (height - thickness) + half);
            layout.ZeroY = toY(0);

            double x0 = xs[0], x1 = xs[xs.Count - 1];
            double width = x1 - x0;
            for (int c = 0; c < columns; c++)
            {
                double left = width <= 0 ? x0 : x0 + width * c / columns;
                double right = width <= 0 ? x0 : x0 + width * (c + 1) / columns;
                double yl = ValueAt(xs, ys, left), yr = ValueAt(xs, ys, right);
                double lo = Math.Min(yl, yr), hi = Math.Max(yl, yr);
                for (int i = 0; i < xs.Count; i++)
                {
                    if (xs[i] > left && xs[i] < right)
                    {
                        lo = Math.Min(lo, ys[i]);
                        hi = Math.Max(hi, ys[i]);
                    }
                }
                float top = toY(hi) - half, bottom = toY(lo) + half;
                double mid = ValueAt(xs, ys, (left + right) / 2);
                float yMid = toY(mid);
                layout.Columns.Add(new ChartColumn
                {
                    LineTop = Math.Max(0, top),
                    LineHeight = Math.Min(height, bottom) - Math.Max(0, top),
                    FillTop = Math.Min(yMid, layout.ZeroY),
                    FillHeight = Math.Abs(layout.ZeroY - yMid),
                    Positive = mid >= 0,
                });
            }
            return layout;
        }

        /// <summary>
        /// Pixel position (0..<paramref name="width"/>) of every whole x from <paramref name="x0"/> to
        /// <paramref name="x1"/>, on the same scale as <see cref="Layout"/>: x0 at the left edge, x1 at the right edge.
        /// Used for one axis mark per day.
        /// </summary>
        public static List<float> Ticks(double x0, double x1, float width)
        {
            var ticks = new List<float>();
            if (width <= 0 || x1 < x0) return ticks;
            double span = x1 - x0;
            for (double x = Math.Ceiling(x0); x <= x1 + 1e-9; x++)
            {
                ticks.Add(span <= 0 ? 0 : (float)((x - x0) / span * width));
            }
            return ticks;
        }

        /// <summary>Straight-line value between the two points around <paramref name="x"/>; the end values outside.</summary>
        public static double ValueAt(IReadOnlyList<double> xs, IReadOnlyList<double> ys, double x)
        {
            if (x <= xs[0]) return ys[0];
            for (int i = 1; i < xs.Count; i++)
            {
                if (x <= xs[i])
                {
                    double dx = xs[i] - xs[i - 1];
                    return dx <= 0 ? ys[i] : ys[i - 1] + (ys[i] - ys[i - 1]) * (x - xs[i - 1]) / dx;
                }
            }
            return ys[ys.Count - 1];
        }
    }
}
