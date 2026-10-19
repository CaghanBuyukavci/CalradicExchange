using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Trading;
using Newtonsoft.Json;

namespace CalradicExchange.Core.Persistence
{
    /// <summary>
    /// Root of all persisted mod state. Serialized as one JSON string (ADR-002).
    /// Any change to the persisted shape must bump <see cref="CurrentSchemaVersion"/> and add a migration.
    /// </summary>
    public sealed class ExchangeState
    {
        public const int CurrentSchemaVersion = 10;

        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>Game day (whole days since campaign start) on which the exchange was created.</summary>
        [JsonProperty("createdOnDay")]
        public int CreatedOnDay { get; set; }

        /// <summary>Last game day the daily tick ran. -1 when it has never run.</summary>
        [JsonProperty("lastTickDay")]
        public int LastTickDay { get; set; } = -1;

        /// <summary>Mod version that last wrote this state. Informational only.</summary>
        [JsonProperty("writtenByVersion")]
        public string WrittenByVersion { get; set; } = CoreInfo.Version;

        /// <summary>Prices, sentiment, history and news. Added in schema 2.</summary>
        [JsonProperty("market")]
        public MarketState Market { get; set; } = new MarketState();

        /// <summary>The player's holdings. Added in schema 2.</summary>
        [JsonProperty("portfolio")]
        public Portfolio Portfolio { get; set; } = new Portfolio();

        public static ExchangeState CreateNew(int currentDay)
        {
            return new ExchangeState { CreatedOnDay = currentDay };
        }
    }
}
