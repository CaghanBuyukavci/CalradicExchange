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
                }
            }

            root["schemaVersion"] = toVersion;
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
