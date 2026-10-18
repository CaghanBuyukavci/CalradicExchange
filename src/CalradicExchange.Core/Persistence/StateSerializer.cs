using System;
using CalradicExchange.Core.Persistence.Migrations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CalradicExchange.Core.Persistence
{
    /// <summary>Converts <see cref="ExchangeState"/> to and from the single JSON save string.</summary>
    public static class StateSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Decimal,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            ContractResolver = new DefaultContractResolver
            {
                // camelCase property names, but dictionary keys (item ids) stay exactly as the game spells them.
                NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false, OverrideSpecifiedNames = false },
            },
        };

        public static string Serialize(ExchangeState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.SchemaVersion = ExchangeState.CurrentSchemaVersion;
            state.WrittenByVersion = CoreInfo.Version;
            return JsonConvert.SerializeObject(state, Settings);
        }

        /// <summary>
        /// Parses a save string, running migrations for older schema versions.
        /// Throws <see cref="StateFormatException"/> when the string cannot be understood.
        /// </summary>
        public static ExchangeState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new StateFormatException("Save string is empty.");

            JObject root;
            try
            {
                // Decimal parsing: prices must not pass through double on the way back in.
                using (var reader = new JsonTextReader(new System.IO.StringReader(json)) { FloatParseHandling = FloatParseHandling.Decimal })
                {
                    root = JObject.Load(reader);
                }
            }
            catch (JsonException ex)
            {
                throw new StateFormatException("Save string is not valid JSON.", ex);
            }

            int version = root.Value<int?>("schemaVersion") ?? 0;
            if (version > ExchangeState.CurrentSchemaVersion)
            {
                throw new StateFormatException(
                    $"Save was written by a newer mod version (schema {version}, this build supports {ExchangeState.CurrentSchemaVersion}).");
            }

            root = MigrationRunner.Migrate(root, version, ExchangeState.CurrentSchemaVersion);
            // Typed read from text: each number is parsed straight into its field type (double or decimal), bit-exact.
            ExchangeState state = JsonConvert.DeserializeObject<ExchangeState>(root.ToString(Formatting.None), Settings);
            if (state.Market == null) state.Market = new Pricing.MarketState();
            if (state.Portfolio == null) state.Portfolio = new Trading.Portfolio();
            return state;
        }
    }
}
