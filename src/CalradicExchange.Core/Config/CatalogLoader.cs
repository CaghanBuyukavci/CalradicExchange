using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CalradicExchange.Core.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CalradicExchange.Core.Config
{
    /// <summary>Parses and validates instruments.json.</summary>
    public static class CatalogLoader
    {
        public const string FileName = "instruments.json";
        private const double WeightTolerance = 0.001;

        public static InstrumentCatalog Parse(string json)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json ?? string.Empty);
            }
            catch (JsonException ex)
            {
                throw new ConfigException(FileName, "not valid JSON: " + ex.Message, ex);
            }

            var errors = new List<string>();
            var defs = new List<InstrumentDefinition>();
            var ids = new HashSet<string>();

            int unitsPerShare = root.Value<int?>("unitsPerShare") ?? 0;
            if (unitsPerShare <= 0) errors.Add("unitsPerShare must be a positive integer.");

            JArray commodities = root["commodities"] as JArray;
            if (commodities == null || commodities.Count == 0) errors.Add("commodities must be a non-empty array.");
            else
            {
                for (int i = 0; i < commodities.Count; i++)
                {
                    string where = $"commodities[{i}]";
                    var def = ReadCommon(commodities[i], where, InstrumentClass.Commodity, errors, ids);
                    def.ItemId = RequireString(commodities[i], "itemId", where, errors);
                    defs.Add(def);
                }
            }

            JArray sectors = root["sectors"] as JArray;
            if (sectors != null)
            {
                for (int i = 0; i < sectors.Count; i++)
                {
                    JToken s = sectors[i];
                    string where = $"sectors[{i}] ({s.Value<string>("id")})";
                    var def = ReadCommon(s, where, InstrumentClass.Sector, errors, ids);
                    def.WarFactor = s.Value<double?>("warFactor") ?? 0.0;

                    foreach (JToken c in (s["components"] as JArray) ?? new JArray())
                    {
                        string item = c.Value<string>("itemId");
                        double w = c.Value<double?>("weight") ?? double.NaN;
                        if (string.IsNullOrEmpty(item)) errors.Add($"{where}: component without itemId.");
                        else if (!(w > 0)) errors.Add($"{where}: component {item} needs a positive weight.");
                        else def.ItemComponents.Add(new ItemWeight(item, w));
                    }
                    foreach (JToken c in (s["categoryComponents"] as JArray) ?? new JArray())
                    {
                        string cat = c.Value<string>("categoryId");
                        double w = c.Value<double?>("weight") ?? double.NaN;
                        if (string.IsNullOrEmpty(cat)) errors.Add($"{where}: category component without categoryId.");
                        else if (!(w > 0)) errors.Add($"{where}: category {cat} needs a positive weight.");
                        else def.CategoryComponents.Add(new CategoryWeight(cat, w));
                    }

                    bool hasItems = def.ItemComponents.Count > 0;
                    bool hasCats = def.CategoryComponents.Count > 0;
                    if (hasItems == hasCats)
                    {
                        errors.Add($"{where}: needs either 'components' or 'categoryComponents' (exactly one).");
                    }
                    else
                    {
                        double sum = hasItems ? def.ItemComponents.Sum(c => c.Weight) : def.CategoryComponents.Sum(c => c.Weight);
                        if (Math.Abs(sum - 1.0) > WeightTolerance)
                        {
                            errors.Add($"{where}: weights must sum to 1, got {sum.ToString("0.###", CultureInfo.InvariantCulture)}.");
                        }
                    }
                    if (def.WarFactor < 0) errors.Add($"{where}: warFactor must not be negative.");
                    defs.Add(def);
                }
            }

            var guildTypes = new List<string>();
            foreach (JToken g in (root["guildTypes"] as JArray) ?? new JArray())
            {
                string type = g.Value<string>("workshopTypeId");
                if (string.IsNullOrEmpty(type)) errors.Add("guildTypes: entry without workshopTypeId.");
                else guildTypes.Add(type);
            }

            if (errors.Count > 0) throw new ConfigException(FileName, errors);
            return new InstrumentCatalog(unitsPerShare, defs, guildTypes);
        }

        private static InstrumentDefinition ReadCommon(JToken token, string where, InstrumentClass cls, List<string> errors, HashSet<string> ids)
        {
            var def = new InstrumentDefinition
            {
                Class = cls,
                Id = RequireString(token, "id", where, errors),
                NameId = token.Value<string>("nameId"),
                Name = token.Value<string>("name") ?? token.Value<string>("id"),
            };
            if (def.Id != null && !ids.Add(def.Id)) errors.Add($"{where}: duplicate id '{def.Id}'.");
            if (token["tags"] is JArray tags) def.Tags = tags.Values<string>().ToList();
            return def;
        }

        private static string RequireString(JToken token, string name, string where, List<string> errors)
        {
            string value = token.Value<string>(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{where}: missing '{name}'.");
                return null;
            }
            return value;
        }
    }
}
