using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    /// <summary>Limited supply and the town's investors (ADR-037).</summary>
    public class LiquidityTests
    {
        private static MarketEngine NewEngine(bool investors, ulong seed = 1, MarketState state = null)
        {
            MarketSettings s = TestData.NoNoiseSettings();
            s.Npc.Enabled = investors;
            return new MarketEngine(TestData.Catalog(), s, state ?? new MarketState(), new XorShift128Plus(seed));
        }

        [Fact]
        public void Supply_IsIssuedAtListing_FromDepthAndPrice()
        {
            MarketEngine e = NewEngine(false);
            e.Tick(TestData.World(1));

            PriceState grain = e.Get("cx_grain"); // 100 a share; 40,000 x 6 / 100
            Assert.Equal(2400, grain.TotalShares);
            Assert.Equal(720, grain.NpcShares);
            Assert.All(e.State.Prices.Where(p => p.HasPrice), p => Assert.True(p.TotalShares > 0));
        }

        [Fact]
        public void ImpactMultiplier_GrowsAsFreeFloatShrinks_AndIsClamped()
        {
            var npc = new NpcSettings();
            Assert.Equal(1.0, Liquidity.ImpactMultiplier(2400, 720, 0, 0, 0.7, npc), 9);
            // Player holds 1000; buying 100 more: mid free float (2400 - 720 - 1000 - 50) / 2400.
            Assert.Equal(0.7 / (630.0 / 2400), Liquidity.ImpactMultiplier(2400, 720, 1000, 100, 0.7, npc), 9);
            Assert.Equal(4.0, Liquidity.ImpactMultiplier(2400, 720, 1680, 10, 0.7, npc), 9);
            // Selling back into a deep float makes the order cheaper, down to the floor.
            Assert.Equal(0.5, Liquidity.ImpactMultiplier(2400, 0, 0, -4800, 1, npc), 9);
            Assert.Equal(1.0, Liquidity.ImpactMultiplier(0, 0, 0, 10, 0.7, npc), 9);
        }

        [Fact]
        public void PlayerOrder_SlipsMore_WhenThePlayerAlreadyHoldsMuchOfTheFloat()
        {
            MarketEngine e = NewEngine(false);
            e.Tick(TestData.World(1));
            var orders = new OrderEngine(e);
            var trader = new TraderProfile(10_000_000, 6, 0);

            var empty = new Portfolio();
            var heavy = new Portfolio();
            heavy.ApplyBuy("cx_grain", 1000, 100_000, 0);

            double light = orders.Quote("cx_grain", OrderSide.Buy, 100, trader, empty).Impact;
            double thin = orders.Quote("cx_grain", OrderSide.Buy, 100, trader, heavy).Impact;
            Assert.True(thin > light * 2.5, $"{thin} vs {light}");
        }

        [Fact]
        public void Whale_MovesThePrice_HoldsTheShares_AndMakesTheNews()
        {
            MarketEngine e = NewEngine(false);
            e.Tick(TestData.World(1));
            PriceState grain = e.Get("cx_grain");

            MarketEvent ev = e.WhaleTrade("cx_grain", true, 2, "town_A", 1);

            Assert.NotNull(ev);
            Assert.Equal(MarketEventType.WhaleTrade, ev.Type);
            Assert.Equal("buy", ev.Detail);
            Assert.Equal(800, ev.Amount); // 2 x 40,000 / 100
            Assert.Equal(1520, grain.NpcShares);
            Assert.True(grain.Price > 110m && ev.Magnitude > 0.1, $"{grain.Price} {ev.Magnitude}");
            Assert.Equal(grain.Price, grain.History.Last().P);
            Assert.Same(ev, e.State.News.Last().Event);

            MarketEvent sell = e.WhaleTrade("cx_grain", false, 2, "town_B", 1);
            Assert.Equal("sell", sell.Detail);
            Assert.Equal(1520 - sell.Amount, grain.NpcShares);
            Assert.True(sell.Magnitude < 0);
        }

        [Fact]
        public void Investors_CannotBuyMoreThanTheFreeFloat_NorSellMoreThanTheyHold()
        {
            MarketEngine e = NewEngine(false);
            e.Tick(TestData.World(1));
            e.PlayerShares = id => id == "cx_grain" ? 500 : 0;
            PriceState grain = e.Get("cx_grain");

            e.WhaleTrade("cx_grain", true, 100, null, 1);
            Assert.Equal(2400 - 500, grain.NpcShares);

            e.WhaleTrade("cx_grain", false, 100, null, 1);
            Assert.Equal(0, grain.NpcShares);
            Assert.Null(e.WhaleTrade("cx_grain", false, 1, null, 1));
            Assert.Null(e.WhaleTrade("cx_guild_town_A_smithy", true, 1, null, 1));
        }

        [Fact]
        public void AfterAWhaleBuys_InvestorsSellOffTheirPile_AndThePriceComesBack()
        {
            MarketEngine e = NewEngine(true);
            e.Settings.Npc.FlowNoise = 0;
            e.Settings.Npc.FlowMomentum = 0;
            e.Settings.Npc.WhaleDailyChance = 0;
            e.Tick(TestData.World(1));
            PriceState grain = e.Get("cx_grain");

            e.WhaleTrade("cx_grain", true, 2, null, 1);
            decimal peak = grain.Price;
            int pile = grain.NpcShares;
            for (int d = 2; d <= 30; d++) e.Tick(TestData.World(d));

            Assert.True(grain.NpcShares < 720 + (pile - 720) / 4, $"{grain.NpcShares}");
            Assert.True(grain.Price < peak && grain.Price < 103m, $"{grain.Price}");
        }

        [Fact]
        public void AfterAWhaleBuys_TheSellOffComesOnRandomDays_NotEveryDay()
        {
            MarketEngine e = NewEngine(true);
            e.Settings.Npc.FlowNoise = 0;
            e.Settings.Npc.WhaleDailyChance = 0;
            e.Tick(TestData.World(1));
            PriceState grain = e.Get("cx_grain");
            e.WhaleTrade("cx_grain", true, 2, null, 1);

            int sellDays = 0, quietDays = 0, held = grain.NpcShares;
            for (int d = 2; d <= 21; d++)
            {
                e.Tick(TestData.World(d));
                if (grain.NpcShares < held) sellDays++;
                else quietDays++;
                held = grain.NpcShares;
            }
            Assert.True(sellDays >= 3 && quietDays >= 3, $"sell {sellDays}, quiet {quietDays}");
        }

        [Fact]
        public void InvestorFlow_IsDeterministic_AndProducesWhalesOverAYear()
        {
            MarketEngine a = NewEngine(true, 7), b = NewEngine(true, 7);
            int whales = 0;
            for (int d = 1; d <= 336; d++)
            {
                whales += a.Tick(TestData.World(d)).GeneratedEvents.Count(ev => ev.Type == MarketEventType.WhaleTrade);
                b.Tick(TestData.World(d));
            }
            Assert.Equal(a.State.Prices.Select(p => p.Price), b.State.Prices.Select(p => p.Price));
            Assert.Equal(a.State.Prices.Select(p => p.NpcShares), b.State.Prices.Select(p => p.NpcShares));
            Assert.InRange(whales, 15, 60); // 10 % a day
            Assert.All(a.State.Prices.Where(p => p.HasPrice), p => Assert.InRange(p.NpcShares, 0, p.TotalShares));
        }

        [Fact]
        public void Supply_SurvivesSaveAndLoad_AndOldSavesGetItOnTheNextTick()
        {
            MarketEngine e = NewEngine(false);
            e.Tick(TestData.World(1));
            e.WhaleTrade("cx_grain", true, 1, "town_A", 1);

            ExchangeState state = ExchangeState.CreateNew(1);
            state.Market = e.State;
            string json = StateSerializer.Serialize(state);
            ExchangeState back = StateSerializer.Deserialize(json);
            Assert.Equal(json, StateSerializer.Serialize(back));
            PriceState grain = back.Market.Prices.Single(p => p.InstrumentId == "cx_grain");
            Assert.Equal(1120, grain.NpcShares);
            Assert.Equal("cx_grain", back.Market.News.Last().Event.InstrumentId);

            // A save from before schema 13 has no supply: it is issued from the current price on the next tick.
            foreach (PriceState ps in back.Market.Prices) ps.TotalShares = ps.NpcShares = 0;
            MarketEngine loaded = NewEngine(false, 1, back.Market);
            loaded.Tick(TestData.World(2));
            PriceState g = loaded.Get("cx_grain");
            Assert.Equal(Liquidity.SupplyFor(loaded.Settings.For(InstrumentClass.Commodity), loaded.Settings.Npc, g.Price), g.TotalShares);
            Assert.Equal((int)System.Math.Round(g.TotalShares * 0.3), g.NpcShares);
        }
    }
}
