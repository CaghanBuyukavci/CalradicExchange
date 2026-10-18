using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void ShippedCatalog_Loads()
        {
            InstrumentCatalog c = TestData.Catalog();

            Assert.Equal(10, c.UnitsPerShare);
            Assert.Equal(12, c.Instruments.Count(i => i.Class == InstrumentClass.Commodity));
            Assert.Equal(5, c.Instruments.Count(i => i.Class == InstrumentClass.Sector));
            Assert.Contains("felt", c.RequiredItemIds);
            Assert.Contains("fish", c.RequiredItemIds);
            Assert.Contains("war_horse", c.RequiredCategoryIds);
            Assert.DoesNotContain("cloth", c.RequiredItemIds);
            Assert.True(c.Find("cx_grain").HasTag("agriculture"));
            Assert.Equal(11, c.GuildWorkshopTypes.Count);
        }

        [Fact]
        public void ShippedSettings_Load()
        {
            MarketSettings s = TestData.Settings();

            Assert.Equal(0.20, s.For(InstrumentClass.Commodity).Kappa);
            Assert.Equal(3, s.RulesFor(MarketEventType.VillageLooted).Count);
            Assert.Equal(0.21, s.SeasonImpulses[Season.Winter]);
            Assert.Equal(5, s.Trading.MinCommission);
            Assert.Equal(0.10, s.Trading.ProfitTaxRate);
        }

        [Fact]
        public void Catalog_ReportsAllProblemsAtOnce()
        {
            string json = @"{
              ""unitsPerShare"": 10,
              ""commodities"": [ { ""id"": ""a"", ""itemId"": ""grain"" }, { ""id"": ""a"" } ],
              ""sectors"": [ { ""id"": ""s"", ""components"": [ { ""itemId"": ""grain"", ""weight"": 0.5 } ] } ]
            }";

            var ex = Assert.Throws<ConfigException>(() => CatalogLoader.Parse(json));

            Assert.Contains(ex.Errors, e => e.Contains("duplicate id 'a'"));
            Assert.Contains(ex.Errors, e => e.Contains("missing 'itemId'"));
            Assert.Contains(ex.Errors, e => e.Contains("weights must sum to 1"));
        }

        [Fact]
        public void Catalog_SectorNeedsExactlyOneComponentKind()
        {
            string json = @"{ ""unitsPerShare"": 10, ""commodities"": [ { ""id"": ""a"", ""itemId"": ""grain"" } ],
              ""sectors"": [ { ""id"": ""s"" } ] }";

            var ex = Assert.Throws<ConfigException>(() => CatalogLoader.Parse(json));
            Assert.Contains(ex.Errors, e => e.Contains("exactly one"));
        }

        [Fact]
        public void Catalog_InvalidJson_Throws()
        {
            var ex = Assert.Throws<ConfigException>(() => CatalogLoader.Parse("{ not json"));
            Assert.Equal(CatalogLoader.FileName, ex.File);
        }

        [Fact]
        public void Settings_InvalidValues_AreReported()
        {
            string json = TestData.SettingsJson
                .Replace("\"kappa\": 0.2,", "\"kappa\": 1.5,")
                .Replace("\"target\": \"eventItem\"", "\"target\": \"nonsense\"");

            var ex = Assert.Throws<ConfigException>(() => SettingsLoader.Parse(json));

            Assert.Contains(ex.Errors, e => e.Contains("classes.Commodity.kappa"));
            Assert.Contains(ex.Errors, e => e.Contains("'nonsense'"));
        }

        [Fact]
        public void Settings_UnknownEventType_Throws()
        {
            string json = TestData.SettingsJson.Replace("\"VillageLooted\":", "\"VillageLoted\":");
            Assert.Throws<ConfigException>(() => SettingsLoader.Parse(json));
        }

        [Fact]
        public void Settings_MissingClass_IsReported()
        {
            string json = TestData.SettingsJson.Replace("\"Bond\":", "\"_Bond\":");
            // "_Bond" is not an InstrumentClass, so this fails while reading.
            Assert.Throws<ConfigException>(() => SettingsLoader.Parse(json));
        }
    }
}
