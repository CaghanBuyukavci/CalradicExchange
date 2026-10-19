using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Guild share parameters (ECONOMY_MODEL 2.3, PLAN 2.1-2.3).</summary>
    public sealed class GuildSettings
    {
        /// <summary>Days of workshop profit averaged into a guild's daily profit.</summary>
        [JsonProperty("profitWindowDays")] public int ProfitWindowDays { get; set; } = 28;
    }
}
