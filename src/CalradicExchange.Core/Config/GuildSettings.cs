using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Guild partnership share parameters (ECONOMY_MODEL 2.3, PLAN 2.1-2.3, ADR-027).</summary>
    public sealed class GuildSettings
    {
        /// <summary>Days of workshop profit averaged into a guild's daily profit.</summary>
        [JsonProperty("profitWindowDays")] public int ProfitWindowDays { get; set; } = 28;

        /// <summary>Partnership shares per guild. A player can hold at most this many of one guild.</summary>
        [JsonProperty("sharesPerGuild")] public int SharesPerGuild { get; set; } = 100;

        /// <summary>Yearly payout / price the share is valued at (0.25 = 25% a year).</summary>
        [JsonProperty("annualYield")] public double AnnualYield { get; set; } = 0.25;

        /// <summary>Days of profit data needed before a guild is listed.</summary>
        [JsonProperty("minListingDays")] public int MinListingDays { get; set; } = 7;

        /// <summary>Floor of the daily profit used for the price, so loss-making guilds keep a small positive price.</summary>
        [JsonProperty("minValuedDailyProfit")] public double MinValuedDailyProfit { get; set; } = 10;

        /// <summary>Relation lost each payout week with the ruler of a guild's town while the player owes it (ADR-027).</summary>
        [JsonProperty("debtRelationPenalty")] public int DebtRelationPenalty { get; set; } = 2;
    }
}
