using CalradicExchange.Core.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalradicExchange.Core.Config
{
    /// <summary>One row of the event table (ECONOMY_MODEL 4.1): which instruments an event pushes, and how.</summary>
    public sealed class ShockRule
    {
        /// <summary>Target selector, see <see cref="CalradicExchange.Core.Pricing.ShockMapper"/>.</summary>
        [JsonProperty("target")] public string Target { get; set; }

        /// <summary>Log impulse added to the sentiment term S.</summary>
        [JsonProperty("impulse")] public double Impulse { get; set; }

        [JsonProperty("halfLifeDays")] public double HalfLifeDays { get; set; }

        /// <summary>Do not decay until the matching end event (siege end).</summary>
        [JsonProperty("holdUntilEnd")] public bool HoldUntilEnd { get; set; }

        /// <summary>Use settings "seasonImpulses" for the event's season instead of <see cref="Impulse"/>.</summary>
        [JsonProperty("seasonal")] public bool Seasonal { get; set; }

        /// <summary>Optional temporary sigma multiplier.</summary>
        [JsonProperty("volMultiplier")] public double VolMultiplier { get; set; }

        [JsonProperty("volDays")] public int VolDays { get; set; }

        /// <summary>Multiply the impulse by the event's town share x <see cref="TownShareMultiplier"/> (famine, ADR-018).</summary>
        [JsonProperty("scaleByTownShare")] public bool ScaleByTownShare { get; set; }

        [JsonProperty("townShareMultiplier")] public double TownShareMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Multiply the impulse by the event's item share (the raided village's share of world production of its
        /// product) x <see cref="ItemShareMultiplier"/> (ADR-040). Events without a share get no impulse.
        /// </summary>
        [JsonProperty("scaleByItemShare")] public bool ScaleByItemShare { get; set; }

        [JsonProperty("itemShareMultiplier")] public double ItemShareMultiplier { get; set; } = 1.0;

        /// <summary>
        /// When above 0, live components of this rule on one instrument never add up to more than
        /// impulse x maxStack: repeated news of the same kind is worth less (ADR-022, self-raid manipulation cap).
        /// </summary>
        [JsonProperty("maxStack")] public double MaxStack { get; set; }

        /// <summary>When set, the rule applies only to events whose hero has this rank (HeroDied, HeroCaptured; ADR-032).</summary>
        [JsonProperty("heroRank"), JsonConverter(typeof(StringEnumConverter))] public HeroRank? HeroRank { get; set; }

        /// <summary>Multiply the impulse by the event's Magnitude (battle size, rebellion). maxStack still counts in unscaled impulses.</summary>
        [JsonProperty("scaleByMagnitude")] public bool ScaleByMagnitude { get; set; }
    }
}
