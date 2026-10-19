using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Pricing;

namespace CalradicExchange.Sim
{
    /// <summary>Statistics used by the ECONOMY_MODEL 8 acceptance targets.</summary>
    public static class Metrics
    {
        /// <summary>Annualized volatility of daily log returns.</summary>
        public static double AnnualVolatility(IReadOnlyList<PricePoint> s, int daysPerYear)
        {
            var r = new List<double>();
            for (int i = 1; i < s.Count; i++) r.Add(Math.Log((double)(s[i].P / s[i - 1].P)));
            return StdDev(r) * Math.Sqrt(daysPerYear);
        }

        /// <summary>Standard deviation of daily log changes of the fundamental F (not annualized).</summary>
        public static double FundamentalDailyVolatility(IReadOnlyList<PricePoint> s)
        {
            var r = new List<double>();
            for (int i = 1; i < s.Count; i++)
            {
                if (s[i].F > 0 && s[i - 1].F > 0) r.Add(Math.Log((double)(s[i].F / s[i - 1].F)));
            }
            return StdDev(r);
        }

        public static double MinRatio(IReadOnlyList<PricePoint> s) => (double)(s.Min(p => p.P) / s[0].P);

        public static double MaxRatio(IReadOnlyList<PricePoint> s) => (double)(s.Max(p => p.P) / s[0].P);

        /// <summary>Pearson correlation of the 30-day moving averages of P and its target F x e^S (ADR-022: news, not F alone, sets the direction).</summary>
        public static double FundamentalCorrelation(IReadOnlyList<PricePoint> s, int window = 30)
        {
            var p = new List<double>();
            var f = new List<double>();
            for (int i = window - 1; i < s.Count; i++)
            {
                double sp = 0, sf = 0;
                for (int j = i - window + 1; j <= i; j++)
                {
                    sp += (double)s[j].P;
                    sf += (double)s[j].F * Math.Exp(s[j].S);
                }
                p.Add(sp / window);
                f.Add(sf / window);
            }
            return Correlation(p, f);
        }

        public static double Correlation(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            double ma = a.Average(), mb = b.Average();
            double cov = 0, va = 0, vb = 0;
            for (int i = 0; i < a.Count; i++)
            {
                cov += (a[i] - ma) * (b[i] - mb);
                va += (a[i] - ma) * (a[i] - ma);
                vb += (b[i] - mb) * (b[i] - mb);
            }
            return va > 0 && vb > 0 ? cov / Math.Sqrt(va * vb) : 0;
        }

        public static double StdDev(IReadOnlyList<double> x)
        {
            if (x.Count < 2) return 0;
            double m = x.Average();
            return Math.Sqrt(x.Sum(v => (v - m) * (v - m)) / (x.Count - 1));
        }

        public static double Median(IEnumerable<double> values)
        {
            double[] v = values.OrderBy(x => x).ToArray();
            if (v.Length == 0) return double.NaN;
            return v.Length % 2 == 1 ? v[v.Length / 2] : (v[v.Length / 2 - 1] + v[v.Length / 2]) / 2;
        }
    }
}
