using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Kingdom index parameters (ECONOMY_MODEL 2.4, PLAN 2.4, ADR-030).</summary>
    public sealed class KingdomSettings
    {
        /// <summary>Days a kingdom founded during tracking (e.g. by a rebellion) waits before its index is listed.</summary>
        [JsonProperty("minListingDays")] public int MinListingDays { get; set; } = 7;

        /// <summary>Exponent of the fief count ratio in the index.</summary>
        [JsonProperty("fiefExponent")] public double FiefExponent { get; set; } = 0.5;

        /// <summary>Prosperity sums below this count as this much, so a kingdom without fiefs keeps a small positive index.</summary>
        [JsonProperty("prosperityFloor")] public double ProsperityFloor { get; set; } = 100;

        /// <summary>Index lost per enemy kingdom, and the lowest the war multiplier goes.</summary>
        [JsonProperty("warStep")] public double WarStep { get; set; } = 0.05;
        [JsonProperty("warFloor")] public double WarFloor { get; set; } = 0.7;

        /// <summary>Index lost per fief under siege, and the lowest the siege multiplier goes.</summary>
        [JsonProperty("siegeStep")] public double SiegeStep { get; set; } = 0.08;
        [JsonProperty("siegeFloor")] public double SiegeFloor { get; set; } = 0.6;
    }
}
