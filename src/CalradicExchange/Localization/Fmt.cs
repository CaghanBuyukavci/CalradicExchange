using System;
using System.Globalization;
using CalradicExchange.Core.Model;
using TaleWorlds.Localization;

namespace CalradicExchange.Localization
{
    /// <summary>Number formatting that follows the game language (decimal separator, percent sign position).</summary>
    public static class Fmt
    {
        /// <summary>Whole denars, e.g. "1250".</summary>
        public static string Denars(decimal value)
        {
            return Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        }

        public static string Denars(double value) => Denars((decimal)value);

        public static string Denars(long value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>A price: whole denars above 100, one decimal below.</summary>
        public static string Price(decimal value)
        {
            string s = value >= 100 ? Denars(value) : Math.Round(value, 1).ToString("0.0", CultureInfo.InvariantCulture);
            return s.Replace(".", Strings.DecimalSeparator.ToString());
        }

        /// <summary>Signed percentage of a fraction, e.g. 0.062 -> "+6.2%" (EN) or "+%6,2" (TR).</summary>
        public static string Pct(double fraction)
        {
            string sign = fraction > 0.00049 ? "+" : fraction < -0.00049 ? "-" : "";
            string abs = Math.Abs(fraction * 100).ToString("0.0", CultureInfo.InvariantCulture).Replace(".", Strings.DecimalSeparator.ToString());
            TextObject t = Strings.PercentFormat;
            t.SetTextVariable("SIGN", sign);
            t.SetTextVariable("ABS", abs);
            return t.ToString();
        }

        /// <summary>Signed whole denars, e.g. "+120" or "-35".</summary>
        public static string SignedDenars(decimal value)
        {
            string s = Denars(value);
            return value > 0 && s != "0" ? "+" + s : s;
        }

        public static string Season(Season season) => Strings.SeasonName(season).ToString();
    }
}
