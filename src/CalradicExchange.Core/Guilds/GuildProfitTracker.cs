using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Guilds
{
    /// <summary>Appends each day's workshop profits to the per-guild series (PLAN 2.1). Pure; no randomness.</summary>
    public static class GuildProfitTracker
    {
        /// <summary>
        /// Records <paramref name="world"/>'s workshops. Pairs seen today get today's net profit appended (may be
        /// negative, trimmed to <paramref name="windowDays"/>); pairs not seen today keep their history but drop to
        /// 0 workshops. Groups without a measurement today only update their workshop count.
        /// </summary>
        public static void Record(List<GuildProfitSeries> guilds, WorldSnapshot world, int windowDays)
        {
            if (guilds == null) throw new ArgumentNullException(nameof(guilds));
            if (world == null) throw new ArgumentNullException(nameof(world));
            int window = Math.Max(1, windowDays);

            Dictionary<string, GuildProfitSeries> byKey = guilds.ToDictionary(g => g.Key);
            var seen = new HashSet<string>();
            foreach (TownSnapshot town in world.Towns)
            {
                if (town?.Workshops == null) continue;
                foreach (KeyValuePair<string, WorkshopGroupSnapshot> pair in town.Workshops)
                {
                    if (pair.Value == null || pair.Value.Count <= 0) continue;
                    string key = GuildProfitSeries.KeyFor(town.Id, pair.Key);
                    if (!byKey.TryGetValue(key, out GuildProfitSeries series))
                    {
                        series = new GuildProfitSeries { TownId = town.Id, WorkshopTypeId = pair.Key };
                        guilds.Add(series);
                        byKey[key] = series;
                    }
                    seen.Add(key);
                    if (series.LastDay == world.Day && series.WorkshopCount > 0) continue; // same day twice
                    series.WorkshopCount = pair.Value.Count;
                    series.LastDay = world.Day;
                    if (!pair.Value.HasProfit) continue;
                    series.DailyProfit.Add(pair.Value.DailyProfit);
                    int excess = series.DailyProfit.Count - window;
                    if (excess > 0) series.DailyProfit.RemoveRange(0, excess);
                }
            }

            foreach (GuildProfitSeries series in guilds)
            {
                if (!seen.Contains(series.Key) && series.LastDay != world.Day) series.WorkshopCount = 0;
            }
        }
    }
}
