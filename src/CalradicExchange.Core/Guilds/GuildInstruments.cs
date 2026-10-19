using System;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Guilds
{
    /// <summary>Guild partnership shares as instruments (PLAN 2.2, ADR-027): ids, definitions and the observed value.</summary>
    public static class GuildInstruments
    {
        public const string IdPrefix = "cx_guild_";

        public static string IdFor(string townId, string workshopTypeId) => IdPrefix + townId + "_" + workshopTypeId;

        public static bool IsGuildId(string instrumentId) => instrumentId != null && instrumentId.StartsWith(IdPrefix, StringComparison.Ordinal);

        public static InstrumentDefinition Define(GuildProfitSeries series, GuildTypeDefinition type)
        {
            return new InstrumentDefinition
            {
                Id = IdFor(series.TownId, series.WorkshopTypeId),
                Class = InstrumentClass.Guild,
                NameId = type?.NameId,
                Name = type?.Name ?? series.WorkshopTypeId,
                TownId = series.TownId,
                Tags = { "guild", series.WorkshopTypeId },
            };
        }

        /// <summary>
        /// Value of one share from the profit average: max(average, floor) x days per year / annual yield / shares.
        /// A share priced like this pays about annualYield of its price per year when profit stays at the average.
        /// </summary>
        public static decimal ObservedValue(GuildProfitSeries series, GuildSettings settings, int daysPerYear)
        {
            double daily = Math.Max(series.AverageDailyProfit, settings.MinValuedDailyProfit);
            double value = daily * Math.Max(1, daysPerYear) / Math.Max(0.01, settings.AnnualYield) / Math.Max(1, settings.SharesPerGuild);
            return (decimal)Math.Round(value, 4);
        }
    }
}
