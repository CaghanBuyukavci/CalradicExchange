using System.Linq;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class FullStatePersistenceTests
    {
        [Fact]
        public void LiveState_RoundTrips_Exactly()
        {
            ExchangeState state = BuildBusyState(out _);

            string first = StateSerializer.Serialize(state);
            string second = StateSerializer.Serialize(StateSerializer.Deserialize(first));

            Assert.Equal(first, second);
        }

        [Fact]
        public void LoadedState_ContinuesExactlyLikeTheOriginal()
        {
            ExchangeState original = BuildBusyState(out MarketEngine originalEngine);
            ExchangeState loaded = StateSerializer.Deserialize(StateSerializer.Serialize(original));
            var loadedEngine = new MarketEngine(TestData.Catalog(), TestData.Settings(), loaded.Market, new XorShift128Plus(loaded.Market.Rng));

            for (int d = 31; d <= 60; d++)
            {
                originalEngine.Tick(TestData.World(d));
                loadedEngine.Tick(TestData.World(d));
            }

            Assert.Equal(originalEngine.State.Prices.Select(p => p.Price), loadedEngine.State.Prices.Select(p => p.Price));
        }

        [Fact]
        public void LoadedState_KeepsShocksHoldsNewsAndPortfolio()
        {
            ExchangeState loaded = StateSerializer.Deserialize(StateSerializer.Serialize(BuildBusyState(out _)));

            Assert.True(loaded.Market.Initialized);
            Assert.Equal(30, loaded.Market.LastTickDay);
            Assert.Equal(Season.Summer, loaded.Market.LastSeason);
            Assert.Contains("town_A", loaded.Market.FamineTowns);
            Assert.True(loaded.Market.BaseItemPrices.ContainsKey("date_fruit"));
            Assert.NotEmpty(loaded.Market.News);
            Assert.Equal(MarketEventType.VillageLooted, loaded.Market.News.First(n => n.Event.Type == MarketEventType.VillageLooted).Event.Type);
            Assert.Contains(loaded.Market.Prices.Single(p => p.InstrumentId == "cx_grain").Shocks, s => s.Value > 0);
            Assert.Equal(10, loaded.Portfolio.QuantityOf("cx_grain"));
            Assert.Equal(1, loaded.Portfolio.TradeCount);
        }

        [Fact]
        public void Phase0Save_MigratesToCurrentSchema()
        {
            // Exactly what Phase 0 wrote into the user's save (see exchange.log, 2026-10-18).
            const string phase0 = "{\"schemaVersion\":1,\"createdOnDay\":91077,\"lastTickDay\":91078,\"writtenByVersion\":\"0.0.1\"}";

            ExchangeState s = StateSerializer.Deserialize(phase0);

            Assert.Equal(ExchangeState.CurrentSchemaVersion, s.SchemaVersion);
            Assert.Equal(91077, s.CreatedOnDay);
            Assert.NotNull(s.Market);
            Assert.False(s.Market.Initialized);
            Assert.NotNull(s.Portfolio);
            Assert.Empty(s.Portfolio.Positions);
        }

        [Fact]
        public void DictionaryKeys_KeepGameSpelling()
        {
            var state = ExchangeState.CreateNew(1);
            state.Market.BaseItemPrices["Date_Fruit"] = 1m;
            Assert.Contains("\"Date_Fruit\"", StateSerializer.Serialize(state));
        }

        private static ExchangeState BuildBusyState(out MarketEngine engine)
        {
            var state = ExchangeState.CreateNew(1);
            engine = new MarketEngine(TestData.Catalog(), TestData.Settings(), state.Market, new XorShift128Plus(99));
            var orders = new OrderEngine(engine);
            for (int d = 1; d <= 30; d++)
            {
                if (d == 5) engine.ApplyEvent(new MarketEvent { Type = MarketEventType.SiegeStarted, Day = d, TownId = "town_B", KingdomId = "k1" });
                if (d == 28) engine.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, Day = d, ItemId = "grain", TownId = "town_A", SettlementId = "village_A1" });
                if (d == 29) orders.Execute("cx_grain", OrderSide.Buy, 10, new TraderProfile(100000, 3, 50), state.Portfolio);

                WorldSnapshot w = TestData.World(d, d < 20 ? Season.Spring : Season.Summer);
                if (d >= 25) w.Towns[0].FoodStocks = 0;
                engine.Tick(w);
            }
            state.LastTickDay = 30;
            return state;
        }
    }
}
