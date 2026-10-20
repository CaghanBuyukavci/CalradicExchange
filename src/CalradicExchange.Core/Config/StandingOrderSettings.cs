using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>
    /// Take-profit / stop-loss orders on held shares (ADR-039). The fee is paid when the order is placed and is not
    /// refunded; the sale itself pays no commission, only the profit tax.
    /// </summary>
    public sealed class StandingOrderSettings
    {
        /// <summary>Fee = max(minFee, order value x (feeRate + feePerDay x days)), order value at today's price.</summary>
        [JsonProperty("feeRate")] public double FeeRate { get; set; } = 0.002;
        [JsonProperty("feePerDay")] public double FeePerDay { get; set; } = 0.0002;
        [JsonProperty("minFee")] public int MinFee { get; set; } = 10;

        /// <summary>Longest validity the slider offers, in days.</summary>
        [JsonProperty("maxDays")] public int MaxDays { get; set; } = 60;

        /// <summary>Slider ends of the targets, in percent of the average cost. 0 turns a target off.</summary>
        [JsonProperty("maxTakeProfitPercent")] public int MaxTakeProfitPercent { get; set; } = 100;
        [JsonProperty("maxStopLossPercent")] public int MaxStopLossPercent { get; set; } = 50;

        /// <summary>Orders listed and saved; the oldest finished ones are dropped first, pending ones never.</summary>
        [JsonProperty("ordersKept")] public int OrdersKept { get; set; } = 20;
    }
}
