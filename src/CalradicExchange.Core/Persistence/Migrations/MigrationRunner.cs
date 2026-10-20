using Newtonsoft.Json.Linq;

namespace CalradicExchange.Core.Persistence.Migrations
{
    /// <summary>
    /// Upgrades persisted JSON from an older schema version to the current one, one step at a time.
    /// Add a case per version bump: "case N: root = MigrateNToNPlus1(root); break;".
    /// </summary>
    public static class MigrationRunner
    {
        public static JObject Migrate(JObject root, int fromVersion, int toVersion)
        {
            for (int v = fromVersion; v < toVersion; v++)
            {
                switch (v)
                {
                    case 0:
                        // Schema 0 never shipped; treat a missing version as schema 1.
                        break;
                    case 1:
                        root = Migrate1To2(root);
                        break;
                    case 2:
                        // Schema 3 adds market.famineStartDays and market.fundamentalMethod. Both default when
                        // missing; a missing method makes the engine rebuild prices with the new averaging (ADR-017).
                        break;
                    case 3:
                        root = Migrate3To4(root);
                        break;
                    case 4:
                        root = Migrate4To5(root);
                        break;
                    case 5:
                        // Schema 6 adds portfolio.history (trade history). A missing list deserializes as empty;
                        // trades made before schema 6 were never recorded.
                        break;
                    case 6:
                        // Schema 7 adds market.guilds (workshop profit series, PLAN 2.1). Starts empty and fills daily.
                        break;
                    case 7:
                        root = Migrate7To8(root);
                        break;
                    case 8:
                        // Schema 9 adds guild instruments (PLAN 2.2): portfolio.guildAccruals, portfolio.guildDebts,
                        // portfolio.guildIncomeTotal, history[].forced and guilds[].lastProfitDay. All default when missing.
                        break;
                    case 9:
                        // Schema 10 adds market.kingdoms (kingdom indices, PLAN 2.4). Starts empty; the kingdoms alive
                        // on the next tick are listed at once.
                        break;
                    case 10:
                        // Schema 11 adds news[].event.heroId / otherHeroId (HeroCaptured news) and portfolio.performance
                        // (daily total profit for the chart, ADR-031). Both default when missing; the chart starts empty.
                        break;
                    case 11:
                        // Schema 12 (ADR-032) adds kingdoms[].baseFiefScore / baseHearth / baseStrength / baseClans, set from
                        // the next reading (KingdomInstruments.SetMissingBases), and new news event fields. Nothing to do here.
                        break;
                }
            }

            root["schemaVersion"] = toVersion;
            return root;
        }

        /// <summary>
        /// Schema 8 measures guild profit as net capital change (can be negative, ADR-027) instead of the owner's
        /// income. Schema 7 series used the old measure and are dropped; they refill within the profit window.
        /// </summary>
        private static JObject Migrate7To8(JObject root)
        {
            if (root["market"] is JObject market) market.Remove("guilds");
            return root;
        }

        /// <summary>
        /// Schema 5 adds prices[].anchor and prices[].economySignal (ADR-022). The anchor starts at the current
        /// fundamental so prices do not jump; the signal starts at 0.
        /// </summary>
        private static JObject Migrate4To5(JObject root)
        {
            if (!(root["market"] is JObject market) || !(market["prices"] is JArray prices)) return root;
            foreach (JToken price in prices)
            {
                if (!(price is JObject ps)) continue;
                if (ps["anchor"] == null && ps["fundamental"] != null) ps["anchor"] = ps["fundamental"];
                if (ps["economySignal"] == null) ps["economySignal"] = 0.0;
            }
            return root;
        }

        /// <summary>
        /// Schema 4 adds market.startDay and prices[].rawFundamental (ADR-021). The start day is taken from the
        /// oldest history point; the raw fundamental starts as the last fundamental.
        /// </summary>
        private static JObject Migrate3To4(JObject root)
        {
            if (!(root["market"] is JObject market)) return root;
            int start = int.MaxValue;
            if (market["prices"] is JArray prices)
            {
                foreach (JToken price in prices)
                {
                    if (!(price is JObject ps)) continue;
                    if (ps["rawFundamental"] == null && ps["fundamental"] != null) ps["rawFundamental"] = ps["fundamental"];
                    if (ps["history"] is JArray history)
                    {
                        foreach (JToken point in history)
                        {
                            JToken day = point?["day"];
                            if (day != null && day.Type == JTokenType.Integer) start = System.Math.Min(start, (int)day);
                        }
                    }
                }
            }
            bool initialized = market["initialized"]?.Type == JTokenType.Boolean && (bool)market["initialized"];
            if (market["startDay"] == null) market["startDay"] = initialized && start != int.MaxValue ? start : -1;
            return root;
        }

        /// <summary>
        /// Schema 2 adds "market" and "portfolio". A schema 1 save (Phase 0) has neither; an empty market
        /// initializes itself on the next daily tick, as when the mod is added mid-campaign.
        /// </summary>
        private static JObject Migrate1To2(JObject root)
        {
            if (root["market"] == null) root["market"] = new JObject();
            if (root["portfolio"] == null) root["portfolio"] = new JObject();
            return root;
        }
    }
}
