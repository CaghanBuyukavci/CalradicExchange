using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CalradicExchange.Adapters;
using CalradicExchange.Core.Guilds;

namespace CalradicExchange.Diagnostics
{
    /// <summary>Text views of the workshop profit series (PLAN 2.1): daily log line, console list and CSV.</summary>
    public static class GuildReport
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>One line for the daily tick log.</summary>
        public static string Summary(List<GuildProfitSeries> guilds)
        {
            List<GuildProfitSeries> active = guilds.Where(g => g.WorkshopCount > 0).ToList();
            if (active.Count == 0) return $"{guilds.Count} tracked, none active";
            List<double> avg = active.Select(g => g.AverageDailyProfit).OrderBy(x => x).ToList();
            GuildProfitSeries best = active.OrderByDescending(g => g.AverageDailyProfit).First();
            int zero = active.Count(g => g.AverageDailyProfit < 0.5);
            return string.Format(Inv,
                "{0} tracked, {1} active ({2} workshops), avg daily profit median {3:0.0}, mean {4:0.0}, max {5:0.0} ({6}), {7} at zero, days recorded {8}",
                guilds.Count, active.Count, active.Sum(g => g.WorkshopCount), avg[avg.Count / 2], avg.Average(),
                best.AverageDailyProfit, best.Key, zero, active.Max(g => g.DailyProfit.Count));
        }

        /// <summary>The <paramref name="count"/> most profitable active guilds, for the console.</summary>
        public static string Top(List<GuildProfitSeries> guilds, int count)
        {
            var sb = new StringBuilder(Summary(guilds) + "\n");
            sb.AppendLine("town                 type                 n   avg/day   last  days");
            foreach (GuildProfitSeries g in guilds.Where(x => x.WorkshopCount > 0).OrderByDescending(x => x.AverageDailyProfit).Take(count))
            {
                double last = g.DailyProfit.Count > 0 ? g.DailyProfit[g.DailyProfit.Count - 1] : 0;
                sb.AppendLine(string.Format(Inv, "{0,-20} {1,-20} {2,2} {3,9:0.0} {4,6:0} {5,5}",
                    Trim(Names.Settlement(g.TownId), 20), Trim(g.WorkshopTypeId, 20), g.WorkshopCount, g.AverageDailyProfit, last, g.DailyProfit.Count));
            }
            return sb.ToString();
        }

        /// <summary>Every guild with its full series, for calibration.</summary>
        public static string Csv(List<GuildProfitSeries> guilds)
        {
            var sb = new StringBuilder("town_id,town,workshop_type,count,last_day,days,avg_daily_profit,daily_profits\n");
            foreach (GuildProfitSeries g in guilds.OrderBy(x => x.TownId).ThenBy(x => x.WorkshopTypeId))
            {
                sb.Append(string.Format(Inv, "{0},\"{1}\",{2},{3},{4},{5},{6:0.00},\"{7}\"\n",
                    g.TownId, Names.Settlement(g.TownId), g.WorkshopTypeId, g.WorkshopCount, g.LastDay, g.DailyProfit.Count,
                    g.AverageDailyProfit, string.Join(" ", g.DailyProfit.Select(p => p.ToString("0", Inv)))));
            }
            return sb.ToString();
        }

        private static string Trim(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
    }
}
