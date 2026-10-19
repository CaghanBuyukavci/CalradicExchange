using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    /// <summary>ADR-019: only the unexpected part of the shock moves the price.</summary>
    public class ShockBaselineTests
    {
        private static MarketEngine Engine(double halfLife)
        {
            MarketSettings s = TestData.NoNoiseSettings();
            s.ShockBaselineHalfLifeDays = halfLife;
            var e = new MarketEngine(TestData.Catalog(), s, new MarketState(), new XorShift128Plus(1));
            e.Tick(TestData.World(1));
            return e;
        }

        private static void Raid(MarketEngine e, int day) =>
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, Day = day, ItemId = "grain" });

        [Fact]
        public void FirstSurprise_IsFeltInFull()
        {
            MarketEngine e = Engine(42);
            Raid(e, 1);
            e.Tick(TestData.World(2));

            Assert.Equal(0.15, e.Get("cx_grain").Shock, 9);
        }

        [Fact]
        public void SteadyStreamOfEvents_StopsMovingThePrice()
        {
            MarketEngine e = Engine(42);
            for (int d = 2; d <= 500; d++)
            {
                Raid(e, d);
                e.Tick(TestData.World(d));
            }

            // One raid a day forever is no news: effective S goes back near zero and the price to F.
            Assert.InRange(e.Get("cx_grain").Shock, -0.02, 0.02);
            Assert.InRange((double)e.Get("cx_grain").Price, 97.0, 103.0);
        }

        [Fact]
        public void WithoutBaseline_SteadyEventsKeepAPermanentPremium()
        {
            MarketEngine e = Engine(0);
            for (int d = 2; d <= 500; d++)
            {
                Raid(e, d);
                e.Tick(TestData.World(d));
            }

            Assert.True(e.Get("cx_grain").Shock > 0.5);
        }

        [Fact]
        public void BaselineDecaysAfterQuietPeriod_SoNextEventIsNewsAgain()
        {
            MarketEngine e = Engine(42);
            for (int d = 2; d <= 200; d++)
            {
                Raid(e, d);
                e.Tick(TestData.World(d));
            }
            for (int d = 201; d <= 600; d++) e.Tick(TestData.World(d));
            Raid(e, 600);
            e.Tick(TestData.World(601));

            Assert.InRange(e.Get("cx_grain").Shock, 0.12, 0.15);
        }
    }
}
