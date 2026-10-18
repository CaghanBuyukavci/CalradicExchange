using System;
using CalradicExchange.Core.Persistence.Migrations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                root = JObject.Parse(json);
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
            return root.ToObject<ExchangeState>(JsonSerializer.Create(Settings));
        }
    }
}
