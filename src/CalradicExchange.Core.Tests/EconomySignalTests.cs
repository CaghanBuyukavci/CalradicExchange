using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    /// <summary>ADR-022: F = anchor x e^E; E is the bounded, fading relative move of an item in the towns.</summary>
    public class EconomySignalTests
    {
        private static MarketEngine Engine(double weight = 0.6, double halfLife = 14, double maxAbs = 0.3,
            double anchorHalfLife = 0, MarketState state = null)
        {
            MarketSettings s = TestData.NoNoiseSettings();
            s.EconomyWeight = weight;
            s.EconomyHalfLifeDays = halfLife;
            s.EconomyMaxAbs = maxAbs;
            s.EconomyMaxDailyMove = 0.15;
            s.AnchorHalfLifeDays = anchorHalfLife;
            return new MarketEngine(TestData.Catalog(), s, state ?? new MarketState(), new XorShift128Plus(1));
        }

        private static Dictionary<string, double> Grain(double m) => new Dictionary<string, double> { ["grain"] = m };

        [Fact]
        public void WholeEconomyMovingTogether_DoesNotMoveF()
        {
            MarketEngine e = Engine();
            e.Tick(TestData.World(1));
            decimal f0 = e.Get("cx_grain").Fundamental;

            e.Tick(TestData.World(2, multiplier: 1.1));

            PriceState ps = e.Get("cx_grain");
            Assert.Equal(f0, ps.Fundamental);
            Assert.Equal(0, ps.EconomySignal, 9);
            Assert.True(ps.RawFundamental > f0);
        }

        [Fact]
        public void OneItemGettingDearer_RaisesE_ByWeightTimesRelativeMove()
        {
            MarketEngine e = Engine();
            e.Tick(TestData.World(1));
            decimal f0 = e.Get("cx_grain").Fundamental;

            e.Tick(TestData.World(2, itemMultipliers: Grain(1.1)));

            PriceState ps = e.Get("cx_grain");
            double expected = 0.6 * Math.Log(1.1);
            Assert.Equal(expected, ps.EconomySignal, 6);
            Assert.Equal((double)f0 * Math.Exp(expected), (double)ps.Fundamental, 2);
            Assert.Equal(0, e.Get("cx_salt").EconomySignal, 9);
        }

        [Fact]
        public void RelativeMove_FadesWithTheHalfLife_WhileTownPricesStayHigh()
        {
            MarketEngine e = Engine(halfLife: 10);
            e.Tick(TestData.World(1));
            e.Tick(TestData.World(2, itemMultipliers: Grain(1.1)));
            double start = e.Get("cx_grain").EconomySignal;

            for (int d = 3; d <= 12; d++) e.Tick(TestData.World(d, itemMultipliers: Grain(1.1)));

            Assert.Equal(start / 2, e.Get("cx_grain").EconomySignal, 6);
        }

        [Fact]
        public void E_StaysWithinItsBound()
        {
            MarketEngine e = Engine(maxAbs: 0.3);
            e.Tick(TestData.World(1));
            double m = 1.0;
            for (int d = 2; d <= 30; d++)
            {
                m *= 1.15;
                e.Tick(TestData.World(d, itemMultipliers: Grain(m)));
            }

            Assert.Equal(0.3, e.Get("cx_grain").EconomySignal, 9);
        }

        [Fact]
        public void Anchor_DriftsTowardsTownPrice_WithItsHalfLife()
        {
            MarketEngine e = Engine(anchorHalfLife: 10);
            e.Tick(TestData.World(1));
            decimal a0 = e.Get("cx_grain").Anchor;

            for (int d = 2; d <= 11; d++) e.Tick(TestData.World(d, multiplier: 4.0));

            // The first day moves everything (no relative move), then ten half-life steps close half of ln 4.
            Assert.Equal(2.0 * (double)a0, (double)e.Get("cx_grain").Anchor, 1);
            Assert.Equal(0, e.Get("cx_grain").EconomySignal, 9);
        }

        [Fact]
        public void WeightZero_IsDirectMode()
        {
            MarketEngine e = Engine(weight: 0);
            e.Tick(TestData.World(1));
            decimal f0 = e.Get("cx_grain").Fundamental;

            e.Tick(TestData.World(2, multiplier: 2.0));

            Assert.Equal(2 * f0, e.Get("cx_grain").Fundamental);
        }

        [Fact]
        public void Schema3Save_MigratesThroughToCurrent()
        {
            ExchangeState s3 = ExchangeState.CreateNew(1);
            MarketEngine e = Engine(state: s3.Market);
            for (int d = 5; d <= 8; d++) e.Tick(TestData.World(d));

            string json = StateSerializer.Serialize(s3)
                .Replace("\"schemaVersion\":" + ExchangeState.CurrentSchemaVersion, "\"schemaVersion\":3");
            json = Regex.Replace(json, "\"startDay\":-?\\d+,", "");
            json = Regex.Replace(json, "\"rawFundamental\":[0-9.]+,", "");
            json = Regex.Replace(json, "\"anchor\":[0-9.]+,", "");
            json = Regex.Replace(json, "\"economySignal\":[-0-9.Ee]+,", "");
            Assert.DoesNotContain("anchor", json);
            Assert.DoesNotContain("economySignal", json);

            ExchangeState loaded = StateSerializer.Deserialize(json);

            Assert.Equal(ExchangeState.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(5, loaded.Market.StartDay);
            foreach (PriceState ps in loaded.Market.Prices)
            {
                if (!ps.HasPrice) continue;
                Assert.Equal(ps.Fundamental, ps.RawFundamental);
                Assert.Equal(ps.Fundamental, ps.Anchor);
                Assert.Equal(0, ps.EconomySignal);
            }
        }

        [Fact]
        public void AnchorAndSignal_RoundTrip()
        {
            ExchangeState state = ExchangeState.CreateNew(1);
            MarketEngine e = Engine(anchorHalfLife: 30, state: state.Market);
            e.Tick(TestData.World(3));
            e.Tick(TestData.World(4, itemMultipliers: Grain(1.2)));

            ExchangeState loaded = StateSerializer.Deserialize(StateSerializer.Serialize(state));

            PriceState a = state.Market.Prices.Find(p => p.InstrumentId == "cx_grain");
            PriceState b = loaded.Market.Prices.Find(p => p.InstrumentId == "cx_grain");
            Assert.Equal(a.Anchor, b.Anchor);
            Assert.Equal(a.EconomySignal, b.EconomySignal, 12);
            Assert.Equal(a.RawFundamental, b.RawFundamental);
            Assert.Equal(a.Fundamental, b.Fundamental);
        }
    }
}
