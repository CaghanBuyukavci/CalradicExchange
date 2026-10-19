using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalradicExchange.Core.Guilds
{
    /// <summary>
    /// Daily profit history of one (town, workshop type) pair: the base data of a guild share (PLAN 2.1, schema 7).
    /// </summary>
    public sealed class GuildProfitSeries
    {
        public string TownId { get; set; }
        public string WorkshopTypeId { get; set; }

        /// <summary>Workshops of this type in the town on <see cref="LastDay"/>; 0 when none are left.</summary>
        public int WorkshopCount { get; set; }

        /// <summary>Last exchange day this pair was seen with at least one workshop.</summary>
        public int LastDay { get; set; }

        /// <summary>Exchange day of the newest <see cref="DailyProfit"/> entry; -1 before the first. Added in schema 9.</summary>
        public int LastProfitDay { get; set; } = -1;

        /// <summary>Daily profit in denars, oldest first, at most the configured window.</summary>
        public List<double> DailyProfit { get; set; } = new List<double>();

        [JsonIgnore] public string Key => KeyFor(TownId, WorkshopTypeId);

        /// <summary>Mean of the recorded days; 0 when nothing is recorded yet.</summary>
        [JsonIgnore] public double AverageDailyProfit => DailyProfit.Count > 0 ? DailyProfit.Average() : 0;

        public static string KeyFor(string townId, string workshopTypeId) => townId + "|" + workshopTypeId;
    }
}
