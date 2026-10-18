using System.Linq;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class VolumeAndRebaseTests
    {
        [Fact]
        public void AveragePrice_IsVolumeWeighted_AndSkipsTownsWithoutVolume()
        {
            WorldSnapshot w = TestData.World(1);
            w.Towns[0].ItemPrices["velvet"] = 1000; w.Towns[0].ItemVolumes["velvet"] = 30;
            w.Towns[1].ItemPrices["velvet"] = 2000; w.Towns[1].ItemVolumes["velvet"] = 10;
            w.Towns[2].ItemPrices["velvet"] = 5750; // no stock, no supply: theoretical price only

            // (30 x 1000 + 10 x 2000) / 40
            Assert.Equal(1250m, FundamentalCalculator.AverageItemPrice(w, "velvet"));
        }

        [Fact]
        public void AveragePrice_FallsBackToProsperity_WhenNoTownHasVolume()
        {
            WorldSnapshot w = TestData.World(1);
            w.Towns[0].ItemPrices["grain"] = 10; w.Towns[0].Prosperity = 3000;
            w.Towns[1].ItemPrices["grain"] = 20; w.Towns[1].Prosperity = 1000;
            w.Towns.RemoveAt(2);

            Assert.Equal(12.5m, FundamentalCalculator.AverageItemPrice(w, "grain"));
        }

        [Fact]
        public void CategoryFactor_IsVolumeWeighted()
        {
            WorldSnapshot w = TestData.World(1);
            w.Towns[0].CategoryFactors["horse"] = 1.2; w.Towns[0].CategoryVolumes["horse"] = 3;
            w.Towns[1].CategoryFactors["horse"] = 0.8; w.Towns[1].CategoryVolumes["horse"] = 1;

            Assert.Equal(1.1, FundamentalCalculator.AverageCategoryFactor(w, "horse").Value, 9);
        }

        [Fact]
        public void NewMarket_RecordsFundamentalMethod()
        {
            var e = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), new MarketState(), new XorShift128Plus(1));
            Assert.False(e.NeedsRebase);
            e.Tick(TestData.World(1));

            Assert.Equal(MarketEngine.FundamentalMethodId, e.State.FundamentalMethod);
            Assert.False(e.NeedsRebase);
        }

        [Fact]
        public void OldMethodMarket_IsRebuiltOnNextTick_KeepingNews()
        {
            var state = new MarketState();
            var e = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), state, new XorShift128Plus(1));
            e.Tick(TestData.World(1));
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.WarDeclared, Day = 1 });
            e.Tick(TestData.World(2));
            state.FundamentalMethod = null; // what a schema 2 save looks like
            state.BaseItemPrices["grain"] = 999m;

            var reloaded = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), state, new XorShift128Plus(2));
            Assert.True(reloaded.NeedsRebase);
            TickSummary s = reloaded.Tick(TestData.World(3, multiplier: 2.0));

            Assert.True(s.Initialized);
            Assert.False(reloaded.NeedsRebase);
            Assert.Equal(200m, reloaded.Get("cx_grain").Price);
            Assert.Equal(20m, state.BaseItemPrices["grain"]);
            Assert.Single(reloaded.Get("cx_grain").History);
            Assert.Single(state.News);
            Assert.Equal(3, state.LastTickDay);
        }

        [Fact]
        public void CloseAllAt_PaysMarketValue_AndRealizes()
        {
            var pf = new Portfolio();
            pf.ApplyBuy("a", 10, 1000, 10);
            pf.ApplyBuy("b", 4, 400, 5);

            int paid = pf.CloseAllAt(id => id == "a" ? 120m : 90m);

            Assert.Equal(1200 + 360, paid);
            Assert.Empty(pf.Positions);
            Assert.Equal(200m - 40m, pf.RealizedPnl);
        }

        [Fact]
        public void Schema2Save_MigratesAndAsksForRebase()
        {
            ExchangeState s2 = ExchangeState.CreateNew(1);
            var e = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), s2.Market, new XorShift128Plus(1));
            e.Tick(TestData.World(1));
            string json = StateSerializer.Serialize(s2)
                .Replace("\"schemaVersion\":" + ExchangeState.CurrentSchemaVersion, "\"schemaVersion\":2")
                .Replace("\"fundamentalMethod\":\"" + MarketEngine.FundamentalMethodId + "\",", "");

            ExchangeState loaded = StateSerializer.Deserialize(json);
            var reloaded = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), loaded.Market, new XorShift128Plus(1));

            Assert.Equal(ExchangeState.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.NotNull(loaded.Market.FamineStartDays);
            Assert.True(reloaded.NeedsRebase);
        }
    }
}
