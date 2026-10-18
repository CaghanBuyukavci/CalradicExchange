using Newtonsoft.Json;

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
    }
}
