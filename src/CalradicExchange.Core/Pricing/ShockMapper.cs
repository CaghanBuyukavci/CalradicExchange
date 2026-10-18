using System.Collections.Generic;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>
    /// Resolves the "target" of a settings.json shock rule to instruments.
    /// instrument:&lt;id&gt; | tag:&lt;tag&gt; | eventItem | guilds:eventTown | townBond:eventTown |
    /// kingdomIndex:eventKingdom | kingdomIndex:otherKingdom | kingdomIndex:both
    /// </summary>
    public static class ShockMapper
    {
        private static readonly HashSet<string> FixedTargets = new HashSet<string>
        {
            "eventItem",
            "guilds:eventTown",
            "townBond:eventTown",
            "kingdomIndex:eventKingdom",
            "kingdomIndex:otherKingdom",
            "kingdomIndex:both",
        };

        public static bool IsValidTarget(string target)
        {
            if (string.IsNullOrEmpty(target)) return false;
            if (FixedTargets.Contains(target)) return true;
            return (target.StartsWith("instrument:") && target.Length > "instrument:".Length)
                || (target.StartsWith("tag:") && target.Length > "tag:".Length);
        }

        public static IEnumerable<InstrumentDefinition> Resolve(string target, MarketEvent ev, InstrumentCatalog catalog)
        {
            if (target.StartsWith("instrument:"))
            {
                InstrumentDefinition def = catalog.Find(target.Substring("instrument:".Length));
                if (def != null) yield return def;
                yield break;
            }

            string tag = target.StartsWith("tag:") ? target.Substring("tag:".Length) : null;
            foreach (InstrumentDefinition def in catalog.Instruments)
            {
                if (Matches(target, tag, def, ev)) yield return def;
            }
        }

        /// <summary>Key that ties a held shock to the event that releases it.</summary>
        public static string HoldKeyFor(MarketEvent ev)
        {
            switch (ev.Type)
            {
                case MarketEventType.SiegeStarted:
                case MarketEventType.SiegeEnded:
                    return "siege:" + ev.TownId;
                default:
                    return null;
            }
        }

        private static bool Matches(string target, string tag, InstrumentDefinition def, MarketEvent ev)
        {
            if (tag != null) return def.HasTag(tag);
            switch (target)
            {
                case "eventItem":
                    return def.Class == InstrumentClass.Commodity && ev.ItemId != null && def.ItemId == ev.ItemId;
                case "guilds:eventTown":
                    return def.Class == InstrumentClass.Guild && ev.TownId != null && def.TownId == ev.TownId;
                case "townBond:eventTown":
                    return def.Class == InstrumentClass.Bond && ev.TownId != null && def.TownId == ev.TownId;
                case "kingdomIndex:eventKingdom":
                    return def.Class == InstrumentClass.Kingdom && ev.KingdomId != null && def.KingdomId == ev.KingdomId;
                case "kingdomIndex:otherKingdom":
                    return def.Class == InstrumentClass.Kingdom && ev.OtherKingdomId != null && def.KingdomId == ev.OtherKingdomId;
                case "kingdomIndex:both":
                    return def.Class == InstrumentClass.Kingdom && def.KingdomId != null
                        && (def.KingdomId == ev.KingdomId || def.KingdomId == ev.OtherKingdomId);
                default:
                    return false;
            }
        }
    }
}
