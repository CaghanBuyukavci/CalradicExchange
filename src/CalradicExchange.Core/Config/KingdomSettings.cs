using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Kingdom index parameters (ECONOMY_MODEL 2.4, PLAN 2.4, ADR-030).</summary>
    public sealed class KingdomSettings
    {
        /// <summary>Days a kingdom founded during tracking (e.g. by a rebellion) waits before its index is listed.</summary>
        [JsonProperty("minListingDays")] public int MinListingDays { get; set; } = 7;

        /// <summary>Exponent of the weighted fief ratio in the index (towns x townWeight + castles x castleWeight).</summary>
        [JsonProperty("fiefExponent")] public double FiefExponent { get; set; } = 0.5;
        [JsonProperty("townWeight")] public double TownWeight { get; set; } = 2;
        [JsonProperty("castleWeight")] public double CastleWeight { get; set; } = 1;

        /// <summary>Exponent of the prosperity ratio (towns and castles).</summary>
        [JsonProperty("prosperityExponent")] public double ProsperityExponent { get; set; } = 0.6;

        /// <summary>Exponent of the village hearth ratio.</summary>
        [JsonProperty("villageExponent")] public double VillageExponent { get; set; } = 0.3;

        /// <summary>Exponent of the military strength ratio; the ratio is clamped to [strengthRatioMin, strengthRatioMax].</summary>
        [JsonProperty("strengthExponent")] public double StrengthExponent { get; set; } = 0.2;
        [JsonProperty("strengthRatioMin")] public double StrengthRatioMin { get; set; } = 0.25;
        [JsonProperty("strengthRatioMax")] public double StrengthRatioMax { get; set; } = 3;

        /// <summary>Exponent of the sworn clan count ratio.</summary>
        [JsonProperty("clanExponent")] public double ClanExponent { get; set; } = 0.3;

        /// <summary>Index lost per village looted or being raided, and the lowest the raid multiplier goes.</summary>
        [JsonProperty("raidStep")] public double RaidStep { get; set; } = 0.015;
        [JsonProperty("raidFloor")] public double RaidFloor { get; set; } = 0.85;

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
