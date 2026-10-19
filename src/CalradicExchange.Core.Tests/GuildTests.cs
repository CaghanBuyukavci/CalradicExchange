using System.Linq;
using CalradicExchange.Core.Guilds;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class GuildTests
    {
        // TestData.World: town_A has 2 smithies making 40 denars a day together.
        private const string SmithyA = "cx_guild_town_A_smithy";

        private static MarketEngine Run(int days, MarketState state = null, System.Func<int, WorldSnapshot> world = null)
        {
            var engine = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), state ?? new MarketState(), new XorShift128Plus(3));
            for (int d = 1; d <= days; d++) engine.Tick(world != null ? world(d) : TestData.World(d));
            return engine;
        }

        private static WorldSnapshot WithSmithyProfit(int day, double profit, int count = 2)
        {
            WorldSnapshot w = TestData.World(day);
            WorkshopGroupSnapshot g = w.Towns.Single(t => t.Id == "town_A").Workshops["smithy"];
            g.DailyProfit = profit;
            g.Count = count;
            return w;
        }

        [Fact]
        public void Guild_IsListedAfterMinListingDays()
        {
            Assert.Null(Run(5).Find(SmithyA));

            MarketEngine engine = Run(9);
            InstrumentDefinition def = engine.Find(SmithyA);
            Assert.NotNull(def);
            Assert.Equal(InstrumentClass.Guild, def.Class);
            Assert.Equal("town_A", def.TownId);
            Assert.Equal("cx_guild_smithy", def.NameId);
            Assert.True(engine.Get(SmithyA).HasPrice);
        }

        [Fact]
        public void GuildPrice_StartsAtTheYieldValue()
        {
            MarketEngine engine = Run(8);
            // 40 a day x 84 days / 25% / 100 shares
            Assert.Equal(134.4m, engine.Get(SmithyA).InitialPrice);
        }

        [Fact]
        public void LossMakingGuild_KeepsAPositiveFloorPrice()
        {
            MarketEngine engine = Run(8, world: d => WithSmithyProfit(d, -30));
            // floor 10 a day x 84 / 0.25 / 100
            Assert.Equal(33.6m, engine.Get(SmithyA).InitialPrice);
        }

        [Fact]
        public void SiegeOfTheTown_PushesItsGuildDown()
        {
            MarketEngine engine = Run(10);
            engine.ApplyEvent(new MarketEvent { Type = MarketEventType.SiegeStarted, Day = 10, TownId = "town_A", KingdomId = "k1" });
            Assert.Contains(engine.Get(SmithyA).Shocks, s => s.Value < 0);
        }

        [Fact]
        public void Guild_ClosesWhenItsWorkshopsAreGone_AndReopensWhenTheyReturn()
        {
            var state = new MarketState();
            MarketEngine engine = Run(10, state);
            Assert.False(engine.Get(SmithyA).Closed);

            TickSummary gone = engine.Tick(WithSmithyProfit(11, 0, count: 0));
            Assert.Contains(SmithyA, gone.ClosedInstruments);
            Assert.True(engine.Get(SmithyA).Closed);
            Assert.True(engine.Get(SmithyA).Price > 0); // last price kept for the payout

            TickSummary back = engine.Tick(TestData.World(12));
            Assert.Contains(SmithyA, back.ReopenedInstruments);
            engine.Tick(TestData.World(13));
            Assert.False(engine.Get(SmithyA).Closed);
            Assert.True(engine.Get(SmithyA).HasPrice);
        }

        [Fact]
        public void GuildShares_AreBoughtOnlyInTheirTown_AndUpToTheShareCount()
        {
            MarketEngine engine = Run(9);
            var orders = new OrderEngine(engine);
            var pf = new Portfolio();

            Assert.Equal(TradeError.NotInGuildTown, orders.Quote(SmithyA, OrderSide.Buy, 5, new TraderProfile(100000, 6, 0, "town_B"), pf).Error);
            Assert.Equal(TradeError.NotInGuildTown, orders.Quote(SmithyA, OrderSide.Buy, 5, new TraderProfile(100000, 6, 0), pf).Error);

            var inTown = new TraderProfile(100000, 6, 0, "town_A");
            Assert.True(orders.Execute(SmithyA, OrderSide.Buy, 60, inTown, pf).Success);
            Assert.Equal(TradeError.GuildSharesExceeded, orders.Quote(SmithyA, OrderSide.Buy, 41, inTown, pf).Error);

            // Selling works anywhere.
            Assert.True(orders.Quote(SmithyA, OrderSide.Sell, 10, new TraderProfile(0, 0, 0, "town_C"), pf).IsValid);
        }

        [Fact]
        public void Accrual_FollowsTodaysProfitAndSharesHeld()
        {
            var pf = new Portfolio();
            pf.Positions.Add(new Position { InstrumentId = SmithyA, Quantity = 25, CostBasis = 3000 });
            var series = new GuildProfitSeries { TownId = "town_A", WorkshopTypeId = "smithy", WorkshopCount = 2, LastProfitDay = 4 };
            series.DailyProfit.AddRange(new[] { 10.0, 80.0 });

            GuildLedger.Accrue(pf, id => id == SmithyA ? series : null, 4, 100);
            GuildLedger.Accrue(pf, id => id == SmithyA ? series : null, 5, 100); // not measured on day 5: nothing

            Assert.Equal(20m, pf.GuildAccruals[SmithyA]); // 80 x 25 / 100
        }

        [Fact]
        public void Settle_PaysProfit()
        {
            var pf = new Portfolio();
            pf.GuildAccruals[SmithyA] = 120.4m;

            GuildSettlement s = GuildLedger.Settle(pf, 50, _ => 100m, 7, 500);

            Assert.Equal(120, s.GoldDelta);
            Assert.Empty(pf.GuildAccruals);
            Assert.Equal(120m, pf.GuildIncomeTotal);
        }

        [Fact]
        public void Settle_TakesALossFromGold()
        {
            var pf = new Portfolio();
            pf.GuildAccruals[SmithyA] = -80m;

            GuildSettlement s = GuildLedger.Settle(pf, 500, _ => 100m, 7, 500);

            Assert.Equal(-80, s.GoldDelta);
            Assert.Empty(pf.GuildDebts);
        }

        [Fact]
        public void Settle_ForceSellsThatGuildsShares_WhenGoldIsShort()
        {
            var pf = new Portfolio();
            pf.Positions.Add(new Position { InstrumentId = SmithyA, Quantity = 10, CostBasis = 1000 });
            pf.GuildAccruals[SmithyA] = -250m;

            GuildSettlement s = GuildLedger.Settle(pf, 30, _ => 100m, 14, 500);

            // 30 from gold, 220 missing -> 3 shares sold for 300, 80 of it is left over.
            GuildSettlementLine line = s.Lines.Single();
            Assert.Equal(3, line.ForcedQuantity);
            Assert.Equal(300, line.ForcedProceeds);
            Assert.Equal(0, line.NewDebt);
            Assert.Equal(7, pf.QuantityOf(SmithyA));
            Assert.Equal(50, s.GoldDelta); // 30 + 300 - 250 = 80 left, the player had 30
            TradeRecord forced = Assert.Single(pf.History);
            Assert.True(forced.Forced);
            Assert.Equal(0, forced.Commission);
        }

        [Fact]
        public void Settle_LeavesADebt_WhenGoldAndSharesAreNotEnough_AndLaterProfitRepaysIt()
        {
            var pf = new Portfolio();
            pf.Positions.Add(new Position { InstrumentId = SmithyA, Quantity = 1, CostBasis = 100 });
            pf.GuildAccruals[SmithyA] = -300m;

            GuildSettlement first = GuildLedger.Settle(pf, 50, _ => 100m, 7, 500);

            // 50 gold + 100 from the one share; 150 owed.
            Assert.Equal(-50, first.GoldDelta);
            Assert.Equal(150m, pf.GuildDebts[SmithyA]);
            Assert.Equal(150m, first.DebtsAfter[SmithyA]);
            Assert.Equal(0, pf.QuantityOf(SmithyA));

            const string other = "cx_guild_town_B_brewery";
            pf.GuildAccruals[other] = 200m;
            GuildSettlement second = GuildLedger.Settle(pf, 0, _ => 100m, 14, 500);

            Assert.Equal(150, second.DebtRepaid);
            Assert.Equal(50, second.GoldDelta);
            Assert.Empty(pf.GuildDebts);
        }

        [Fact]
        public void GuildFields_SurviveSaveAndLoad()
        {
            var state = ExchangeState.CreateNew(1);
            MarketEngine engine = Run(9, state.Market);
            state.Portfolio.GuildAccruals[SmithyA] = 12.5m;
            state.Portfolio.GuildDebts[SmithyA] = 40m;
            state.Portfolio.GuildIncomeTotal = -7m;
            state.Portfolio.History.Add(new TradeRecord { InstrumentId = SmithyA, Forced = true });

            string json = StateSerializer.Serialize(state);
            ExchangeState back = StateSerializer.Deserialize(json);

            Assert.Equal(json, StateSerializer.Serialize(back));
            Assert.Equal(12.5m, back.Portfolio.GuildAccruals[SmithyA]);
            Assert.Equal(40m, back.Portfolio.TotalGuildDebt);
            Assert.True(back.Portfolio.History.Single().Forced);
            Assert.Equal(9, back.Market.Guilds.Single().LastProfitDay);
            Assert.NotNull(new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), back.Market, new XorShift128Plus(1)).Find(SmithyA));
        }
    }
}
