using System;
using System.Collections.Generic;
using System.IO;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Tests
{
    /// <summary>Loads the real ModuleData files and builds small synthetic worlds.</summary>
    internal static class TestData
    {
        /// <summary>Unit prices used by <see cref="World"/>. Game base values (v1.4.8).</summary>
        public static readonly Dictionary<string, decimal> BasePrices = new Dictionary<string, decimal>
        {
            ["grain"] = 10, ["fish"] = 12, ["olives"] = 30, ["date_fruit"] = 50, ["grape"] = 20, ["oil"] = 290,
            ["salt"] = 40, ["iron"] = 50, ["tools"] = 250, ["flax"] = 15, ["linen"] = 245, ["wool"] = 22,
            ["felt"] = 230, ["velvet"] = 575, ["cotton"] = 80, ["silver"] = 100, ["jewelry"] = 675, ["wine"] = 90,
            ["meat"] = 30, ["cheese"] = 40, ["butter"] = 25, ["beer"] = 50, ["sheep"] = 80, ["hog"] = 60, ["clay"] = 18,
            ["pottery"] = 210, ["hardwood"] = 25, ["planks"] = 180, ["hides"] = 50, ["leather"] = 230, ["fur"] = 400,
        };

        private static string DataDir => Path.Combine(AppContext.BaseDirectory, "ModuleData");

        public static string InstrumentsJson => File.ReadAllText(Path.Combine(DataDir, "instruments.json"));
        public static string SettingsJson => File.ReadAllText(Path.Combine(DataDir, "settings.json"));

        public static InstrumentCatalog Catalog() => CatalogLoader.Parse(InstrumentsJson);

        /// <summary>A fresh copy each call, so tests may mutate it.</summary>
        public static MarketSettings Settings() => SettingsLoader.Parse(SettingsJson);

        /// <summary>Village share of world production that makes a default raid shock +0.15, as before ADR-040 (2.5 x 0.06).</summary>
        public const double RaidShare = 0.06;

        /// <summary>
        /// Settings with sigma = 0 for every class, no shock baseline and the economy model off (F = world value): prices move only through kappa and
        /// raw shocks, so expected values can be computed by hand.
        /// </summary>
        public static MarketSettings NoNoiseSettings()
        {
            MarketSettings s = Settings();
            foreach (ClassParameters p in s.Classes.Values)
            {
                p.Sigma = 0;
                p.JumpChance = 0;
            }
            s.ShockBaselineHalfLifeDays = 0;
            s.MarketNews.Enabled = false;
            s.EconomyWeight = 0;
            s.Npc.Enabled = false;
            return s;
        }

        /// <summary>Three towns quoting every item at base price x multiplier (per item override possible).</summary>
        public static WorldSnapshot World(int day, Season season = Season.Spring, double multiplier = 1.0,
            Dictionary<string, double> itemMultipliers = null, double categoryFactor = 1.0)
        {
            var world = new WorldSnapshot { Day = day, Season = season, KingdomCount = 2, ActiveWarPairs = 0 };
            foreach (string id in new[] { "town_A", "town_B", "town_C" })
            {
                var town = new TownSnapshot { Id = id, KingdomId = "k1", Prosperity = 1000, FoodStocks = 50 };
                foreach (KeyValuePair<string, decimal> p in BasePrices)
                {
                    double m = multiplier;
                    if (itemMultipliers != null && itemMultipliers.TryGetValue(p.Key, out double im)) m *= im;
                    town.ItemPrices[p.Key] = p.Value * (decimal)m;
                }
                foreach (string cat in Catalog().RequiredCategoryIds) town.CategoryFactors[cat] = categoryFactor;
                if (id == "town_A") town.Workshops["smithy"] = new WorkshopGroupSnapshot { Count = 2, DailyProfit = 40 };
                world.Towns.Add(town);
            }
            return world;
        }
    }
}
