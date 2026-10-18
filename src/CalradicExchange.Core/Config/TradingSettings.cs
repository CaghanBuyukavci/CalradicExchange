using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Order cost parameters (ECONOMY_MODEL 5).</summary>
    public sealed class TradingSettings
    {
        [JsonProperty("commissionRate")] public double CommissionRate { get; set; }
        [JsonProperty("minCommission")] public int MinCommission { get; set; }

        /// <summary>Trade skill at which the full discount applies.</summary>
        [JsonProperty("tradeSkillCap")] public int TradeSkillCap { get; set; }

        /// <summary>Commission discount at the skill cap (0.5 = half price).</summary>
        [JsonProperty("tradeSkillMaxDiscount")] public double TradeSkillMaxDiscount { get; set; }

        /// <summary>Share of the slippage that stays in the market price after a trade.</summary>
        [JsonProperty("permanentImpactFactor")] public double PermanentImpactFactor { get; set; }
    }
}
