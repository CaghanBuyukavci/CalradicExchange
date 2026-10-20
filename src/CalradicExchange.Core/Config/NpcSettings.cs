using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>
    /// Limited share supply and the trading of the town's other investors (ECONOMY_MODEL 5.1, ADR-037). The investors
    /// have no portfolios; only their net holding per instrument is kept (PriceState.NpcShares).
    /// </summary>
    public sealed class NpcSettings
    {
        /// <summary>False: no investor flow and no whales. Supply still scales the price impact.</summary>
        [JsonProperty("enabled")] public bool Enabled { get; set; }

        /// <summary>Shares issued per instrument, in denars at the listing price: depth x this. Guilds use sharesPerGuild.</summary>
        [JsonProperty("supplyDepthMultiple")] public double SupplyDepthMultiple { get; set; } = 6;

        /// <summary>Share of the supply the investors hold at listing; the rest is free float.</summary>
        [JsonProperty("initialHoldingFraction")] public double InitialHoldingFraction { get; set; } = 0.3;

        /// <summary>Bounds of the impact multiplier (free float at listing / free float during the order).</summary>
        [JsonProperty("impactMultiplierMin")] public double ImpactMultiplierMin { get; set; } = 0.5;
        [JsonProperty("impactMultiplierMax")] public double ImpactMultiplierMax { get; set; } = 4;

        /// <summary>Standard deviation of the investors' daily net order, as a fraction of the class depth.</summary>
        [JsonProperty("flowNoise")] public double FlowNoise { get; set; } = 0.15;

        /// <summary>
        /// Daily net order per unit of the last 5 days' price change, as a fraction of depth (trend followers). Off by
        /// default: in the simulator it amplified price pushes from raiding one's own villages (ADR-037).
        /// </summary>
        [JsonProperty("flowMomentum")] public double FlowMomentum { get; set; }

        /// <summary>
        /// Pull of the investors' holding back to its listing level, as a fraction of depth per unit of the gap
        /// (gap in supply fractions). A whale's pile is sold off over the following days this way.
        /// </summary>
        [JsonProperty("holdingReversion")] public double HoldingReversion { get; set; } = 0.5;

        /// <summary>Chance per day that one whale trades somewhere in the market.</summary>
        [JsonProperty("whaleDailyChance")] public double WhaleDailyChance { get; set; } = 0.1;

        /// <summary>Whale order size, in multiples of the class depth (uniform between the two).</summary>
        [JsonProperty("whaleMinDepths")] public double WhaleMinDepths { get; set; } = 1;
        [JsonProperty("whaleMaxDepths")] public double WhaleMaxDepths { get; set; } = 3;

        /// <summary>Chance that a whale buys; it sells more often when the investors already hold a lot.</summary>
        [JsonProperty("whaleBuyChance")] public double WhaleBuyChance { get; set; } = 0.6;
    }
}
