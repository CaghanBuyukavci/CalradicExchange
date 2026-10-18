using System.Collections.Generic;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class FundamentalTests
    {
        [Fact]
        public void AveragePrice_IsProsperityWeighted()
        {
            WorldSnapshot w = TestData.World(1);
            w.Towns[0].ItemPrices["grain"] = 10; w.Towns[0].Prosperity = 3000;
            w.Towns[1].ItemPrices["grain"] = 20; w.Towns[1].Prosperity = 1000;
            w.Towns.RemoveAt(2);

            Assert.Equal(12.5m, FundamentalCalculator.AverageItemPrice(w, "grain"));
        }

        [Fact]
        public void AveragePrice_ExcludesBesiegedTowns()
        {
            WorldSnapshot w = TestData.World(1);
            w.Towns[0].ItemPrices["grain"] = 1000;
            w.Towns[0].IsUnderSiege = true;

            Assert.Equal(10m, FundamentalCalculator.AverageItemPrice(w, "grain"));
        }

        [Fact]
        public void AveragePrice_NullWithoutData()
        {
            WorldSnapshot w = TestData.World(1);
            Assert.Null(FundamentalCalculator.AverageItemPrice(w, "no_such_item"));
            foreach (TownSnapshot t in w.Towns) t.IsUnderSiege = true;
            Assert.Null(FundamentalCalculator.AverageItemPrice(w, "grain"));
        }

        [Fact]
        public void Commodity_IsUnitsTimesAverage()
        {
            Assert.Equal(100m, FundamentalCalculator.Commodity(TestData.World(1), "grain", 10));
        }

        [Fact]
        public void Sector_Is100AtBaseline_AndTracksRatio()
        {
            InstrumentCatalog cat = TestData.Catalog();
            InstrumentDefinition agri = cat.Find("cx_sector_agriculture");
            MarketState state = Baselines(TestData.World(0));

            Assert.Equal(100.0, (double)FundamentalCalculator.Sector(TestData.World(1), agri, state), 6);
            Assert.Equal(110.0, (double)FundamentalCalculator.Sector(TestData.World(1, multiplier: 1.1), agri, state), 6);
        }

        [Fact]
        public void Sector_RenormalizesMissingComponents()
        {
            InstrumentDefinition agri = TestData.Catalog().Find("cx_sector_agriculture");
            MarketState state = Baselines(TestData.World(0));
            WorldSnapshot w = TestData.World(1, itemMultipliers: new Dictionary<string, double> { ["grain"] = 2.0 });
            foreach (TownSnapshot t in w.Towns) t.ItemPrices.Remove("fish");

            // Remaining weights: grain .35 (x2), olives/date/grape .45 (x1) -> (0.7 + 0.45) / 0.8 = 1.4375
            Assert.Equal(143.75, (double)FundamentalCalculator.Sector(w, agri, state), 6);
        }

        [Fact]
        public void MilitarySector_AppliesWarMultiplier()
        {
            InstrumentDefinition mil = TestData.Catalog().Find("cx_sector_military");
            MarketState state = Baselines(TestData.World(0));
            WorldSnapshot w = TestData.World(1, categoryFactor: 1.2);
            w.KingdomCount = 4;
            w.ActiveWarPairs = 2;

            // 100 x 1.2 x (1 + 0.15 x 2/4)
            Assert.Equal(129.0, (double)FundamentalCalculator.Sector(w, mil, state), 6);
        }

        private static MarketState Baselines(WorldSnapshot w)
        {
            var state = new MarketState();
            foreach (string item in TestData.Catalog().RequiredItemIds) state.BaseItemPrices[item] = FundamentalCalculator.AverageItemPrice(w, item).Value;
            foreach (string cat in TestData.Catalog().RequiredCategoryIds) state.BaseCategoryFactors[cat] = FundamentalCalculator.AverageCategoryFactor(w, cat).Value;
            return state;
        }
    }
}
