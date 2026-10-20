using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Order cost parameters (ECONOMY_MODEL 5, ADR-014).</summary>
    public sealed class TradingSettings
    {
        /// <summary>Commission rate on the gross of every trade, the same for every trader (ADR-033).</summary>
        [JsonProperty("commissionRate")] public double CommissionRate { get; set; }

        [JsonProperty("minCommission")] public int MinCommission { get; set; }

        /// <summary>Share of realized profit taken on each profitable sell. Losses are not taxed.</summary>
        [JsonProperty("profitTaxRate")] public double ProfitTaxRate { get; set; }

        /// <summary>Share of the slippage that stays in the market price after a trade.</summary>
        [JsonProperty("permanentImpactFactor")] public double PermanentImpactFactor { get; set; }

        /// <summary>Number of trades kept in the player's trade history; older ones are dropped.</summary>
        [JsonProperty("tradeHistoryLimit")] public int TradeHistoryLimit { get; set; } = 500;
    }
}
