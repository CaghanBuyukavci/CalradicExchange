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

        /// <summary>Half-life of the expected-shock baseline subtracted from S (ADR-019). 0 disables it.</summary>
        [JsonProperty("shockBaselineHalfLifeDays")] public double ShockBaselineHalfLifeDays { get; set; }

        /// <summary>
        /// Weight of the real economy's relative daily moves in the economy signal E (ADR-022). 0 switches the
        /// model off: F is the observed world value each day (direct mode, used by tests).
        /// </summary>
        [JsonProperty("economyWeight")] public double EconomyWeight { get; set; }

        /// <summary>Half-life of E: a relative price move in the towns fades out of the share price over this time.</summary>
        [JsonProperty("economyHalfLifeDays")] public double EconomyHalfLifeDays { get; set; }

        /// <summary>|E| never exceeds this (log units, 0.3 = about +35% / -26%).</summary>
        [JsonProperty("economyMaxAbs")] public double EconomyMaxAbs { get; set; }

        /// <summary>A single day's observed move is clamped to this before it enters E (data glitches, empty markets).</summary>
        [JsonProperty("economyMaxDailyMove")] public double EconomyMaxDailyMove { get; set; }

        /// <summary>Half-life of the anchor's drift towards the observed world value (the slow real link). 0 = fixed anchor.</summary>
        [JsonProperty("anchorHalfLifeDays")] public double AnchorHalfLifeDays { get; set; }

        /// <summary>A town that ran out of food cannot start another famine for this many days.</summary>
        [JsonProperty("famineCooldownDays")] public int FamineCooldownDays { get; set; }

        [JsonProperty("classes")] public Dictionary<InstrumentClass, ClassParameters> Classes { get; set; } = new Dictionary<InstrumentClass, ClassParameters>();
        [JsonProperty("trading")] public TradingSettings Trading { get; set; }
        [JsonProperty("limits")] public LimitSettings Limits { get; set; }
        [JsonProperty("guilds")] public GuildSettings Guilds { get; set; } = new GuildSettings();
        [JsonProperty("kingdoms")] public KingdomSettings Kingdoms { get; set; } = new KingdomSettings();
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
