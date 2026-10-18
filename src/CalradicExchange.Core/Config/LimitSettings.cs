using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Position limits (ECONOMY_MODEL 6).</summary>
    public sealed class LimitSettings
    {
        [JsonProperty("baseTotal")] public int BaseTotal { get; set; }
        [JsonProperty("perClanTier")] public int PerClanTier { get; set; }
        [JsonProperty("perTradeSkill")] public int PerTradeSkill { get; set; }

        /// <summary>Per-instrument limit as a multiple of the class depth.</summary>
        [JsonProperty("perInstrumentDepthMultiple")] public double PerInstrumentDepthMultiple { get; set; }
    }
}
