using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>One kind of market news (ADR-040). The texts are localized by id: cx_mnews_{id} and cx_mrumor_{id}.</summary>
    public sealed class MarketNewsTemplate
    {
        [JsonProperty("id")] public string Id { get; set; }

        /// <summary>Game item StringIds of the commodities it can hit. Empty: any commodity.</summary>
        [JsonProperty("items")] public List<string> Items { get; set; } = new List<string>();

        /// <summary>Log impulse on S, drawn evenly between the two (both negative for bad news for the price).</summary>
        [JsonProperty("minImpulse")] public double MinImpulse { get; set; }
        [JsonProperty("maxImpulse")] public double MaxImpulse { get; set; }

        [JsonProperty("halfLifeDays")] public double HalfLifeDays { get; set; } = 20;
    }
}
