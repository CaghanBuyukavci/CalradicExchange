using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Per-class pricing and liquidity parameters (ECONOMY_MODEL 3 and 5).</summary>
    public sealed class ClassParameters
    {
        /// <summary>Daily pull towards the target value.</summary>
        [JsonProperty("kappa")] public double Kappa { get; set; }

        /// <summary>Daily volatility of the noise term.</summary>
        [JsonProperty("sigma")] public double Sigma { get; set; }

        /// <summary>Full bid/ask spread as a fraction (0.02 = 2%).</summary>
        [JsonProperty("spread")] public double Spread { get; set; }

        /// <summary>Market depth in denars used for slippage.</summary>
        [JsonProperty("depth")] public double Depth { get; set; }

        /// <summary>Slippage coefficient.</summary>
        [JsonProperty("lambda")] public double Lambda { get; set; }

        /// <summary>Daily chance of an unannounced jump in the noise (fat tails, ADR-040). 0 = none.</summary>
        [JsonProperty("jumpChance")] public double JumpChance { get; set; }

        /// <summary>Standard deviation of a jump, in log units.</summary>
        [JsonProperty("jumpSigma")] public double JumpSigma { get; set; }

        /// <summary>
        /// Subtract the slow baseline of expected shocks from S (ADR-019). Off for commodities (ADR-040): their raid
        /// shocks are scaled by the village's share of world production, so frequent small raids stay small by themselves.
        /// </summary>
        [JsonProperty("shockBaseline")] public bool ShockBaseline { get; set; } = true;
    }
}
