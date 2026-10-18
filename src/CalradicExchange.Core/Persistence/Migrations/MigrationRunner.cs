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
                }
            }

            root["schemaVersion"] = toVersion;
            return root;
        }
    }
}
