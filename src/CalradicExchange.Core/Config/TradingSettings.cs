using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Order cost parameters (ECONOMY_MODEL 5, ADR-014).</summary>
    public sealed class TradingSettings
    {
        /// <summary>Commission rate at Trade skill 0.</summary>
        [JsonProperty("commissionRateMax")] public double CommissionRateMax { get; set; }

        /// <summary>Commission rate at <see cref="TradeSkillCap"/> and above.</summary>
        [JsonProperty("commissionRateMin")] public double CommissionRateMin { get; set; }

        [JsonProperty("minCommission")] public int MinCommission { get; set; }

        /// <summary>Trade skill at which the commission reaches its minimum rate.</summary>
        [JsonProperty("tradeSkillCap")] public int TradeSkillCap { get; set; }

        /// <summary>Share of realized profit taken on each profitable sell. Losses are not taxed.</summary>
        [JsonProperty("profitTaxRate")] public double ProfitTaxRate { get; set; }

        /// <summary>Share of the slippage that stays in the market price after a trade.</summary>
        [JsonProperty("permanentImpactFactor")] public double PermanentImpactFactor { get; set; }
    }
}
