using Newtonsoft.Json;

namespace CalradicExchange.Core.Random
{
    /// <summary>Serializable xorshift128+ state.</summary>
    public sealed class RandomState
    {
        [JsonProperty("s0")] public ulong S0 { get; set; }
        [JsonProperty("s1")] public ulong S1 { get; set; }
    }
}
