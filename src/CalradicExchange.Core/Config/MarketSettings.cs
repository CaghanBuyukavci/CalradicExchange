using System.Collections.Generic;
using CalradicExchange.Core.Model;
using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>All tunable market parameters, loaded from settings.json.</summary>
    public sealed class MarketSettings
    {
        /// <summary>Global sigma multiplier (player setting, 0.5 - 2).</summary>
        [JsonProperty("volatilityMultiplier")] public double VolatilityMultiplier { get; set; } = 1.0;

        [JsonProperty("maxDailyLogReturn")] public double MaxDailyLogReturn { get; set; }
        [JsonProperty("maxAbsShock")] public double MaxAbsShock { get; set; }
        [JsonProperty("historyDays")] public int HistoryDays { get; set; }
        [JsonProperty("newsKeep")] public int NewsKeep { get; set; }

        [JsonProperty("classes")] public Dictionary<InstrumentClass, ClassParameters> Classes { get; set; } = new Dictionary<InstrumentClass, ClassParameters>();
        [JsonProperty("trading")] public TradingSettings Trading { get; set; }
        [JsonProperty("limits")] public LimitSettings Limits { get; set; }
        [JsonProperty("seasonImpulses")] public Dictionary<Season, double> SeasonImpulses { get; set; } = new Dictionary<Season, double>();
        [JsonProperty("eventShocks")] public Dictionary<MarketEventType, List<ShockRule>> EventShocks { get; set; } = new Dictionary<MarketEventType, List<ShockRule>>();

        public ClassParameters For(InstrumentClass cls)
        {
            return Classes[cls];
        }

        public IReadOnlyList<ShockRule> RulesFor(MarketEventType type)
        {
            return EventShocks.TryGetValue(type, out List<ShockRule> rules) ? rules : (IReadOnlyList<ShockRule>)new ShockRule[0];
        }
    }
}
