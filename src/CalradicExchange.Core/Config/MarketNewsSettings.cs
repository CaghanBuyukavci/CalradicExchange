using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>
    /// Market news made up by the exchange (ADR-040): a mine collapses, a plague hits the herds, an army places a big
    /// order. Each one pushes one commodity hard; a part of the move happens on the day, the rest over the next days.
    /// Many are announced by a rumor a few days before, and some rumors are false.
    /// </summary>
    public sealed class MarketNewsSettings
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; } = true;

        /// <summary>Chance per day that a piece of market news is set off (one commodity, one template).</summary>
        [JsonProperty("dailyChance")] public double DailyChance { get; set; } = 0.6;

        /// <summary>Chance that a piece of news is first heard as a rumor.</summary>
        [JsonProperty("rumorChance")] public double RumorChance { get; set; } = 0.6;

        /// <summary>Share of all rumors that are false (no news follows). Lowered by the broker level later.</summary>
        [JsonProperty("falseRumorShare")] public double FalseRumorShare { get; set; } = 0.33;

        /// <summary>Days between a rumor and its news, drawn evenly from this range.</summary>
        [JsonProperty("rumorLeadMinDays")] public int RumorLeadMinDays { get; set; } = 1;
        [JsonProperty("rumorLeadMaxDays")] public int RumorLeadMaxDays { get; set; } = 3;

        /// <summary>A rumor pushes S by this fraction of its news' impulse, true or false.</summary>
        [JsonProperty("rumorImpulseFraction")] public double RumorImpulseFraction { get; set; } = 0.25;
        [JsonProperty("rumorHalfLifeDays")] public double RumorHalfLifeDays { get; set; } = 4;

        /// <summary>Part of the impulse the price jumps by on the news day; kappa pulls in the rest over the next days.</summary>
        [JsonProperty("immediateFraction")] public double ImmediateFraction { get; set; } = 0.35;

        [JsonProperty("templates")] public List<MarketNewsTemplate> Templates { get; set; } = new List<MarketNewsTemplate>();
    }
}
