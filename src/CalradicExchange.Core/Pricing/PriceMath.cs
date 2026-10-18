using System;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>The daily price formula (ECONOMY_MODEL 3) and sentiment decay (4). Pure functions.</summary>
    public static class PriceMath
    {
        public const decimal MinPrice = 0.0001m;

        /// <summary>Per-day decay multiplier for a half-life in days: 0.5^(1/halfLife).</summary>
        public static double DecayFactor(double halfLifeDays)
        {
            return Math.Pow(0.5, 1.0 / halfLifeDays);
        }

        /// <summary>
        /// r = kappa x ln(T / P) + sigma x eps, clamped to +-maxReturn, with T = F x e^S.
        /// </summary>
        public static double LogReturn(decimal price, decimal fundamental, double shock, double kappa, double sigma, double eps, double maxReturn)
        {
            double target = (double)fundamental * Math.Exp(shock);
            double r = kappa * Math.Log(target / (double)price) + sigma * eps;
            if (r > maxReturn) return maxReturn;
            if (r < -maxReturn) return -maxReturn;
            return r;
        }

        /// <summary>P x e^r, rounded to 4 decimals and kept above zero.</summary>
        public static decimal Apply(decimal price, double logReturn)
        {
            decimal next = Round((double)price * Math.Exp(logReturn));
            return next < MinPrice ? MinPrice : next;
        }

        public static decimal Round(double value)
        {
            return Math.Round((decimal)value, 4, MidpointRounding.AwayFromZero);
        }
    }
}
