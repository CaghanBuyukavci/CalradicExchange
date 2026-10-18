using System;
using System.Text.RegularExpressions;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    /// <summary>ADR-021: the price follows a smoothed fundamental, with a long half-life while a new campaign settles.</summary>
    public class FundamentalSmoothingTests
    {
        private static MarketEngine Engine(double normal, double start = 0, int ramp = 0, MarketState state = null)
        {
            MarketSettings s = TestData.NoNoiseSettings();
            s.FundamentalHalfLifeDays = normal;
            s.FundamentalStartHalfLifeDays = start;
            s.FundamentalRampDays = ramp;
            return new MarketEngine(TestData.Catalog(), s, state ?? new MarketState(), new XorShift128Plus(1));
        }

        [Fact]
        public void WithoutSmoothing_FundamentalFollowsTheWorld()
        {
            MarketEngine e = Engine(0);
            e.Tick(TestData.World(1));
            decimal f0 = e.Get("cx_grain").Fundamental;

            e.Tick(TestData.World(2, multiplier: 2.0));

            Assert.Equal(2 * f0, e.Get("cx_grain").Fundamental);
        }

        [Fact]
        public void WithSmoothing_FundamentalMovesOneEmaStep_AndRawIsKept()
        {
            MarketEngine e = Engine(14);
            e.Tick(TestData.World(1));
            decimal f0 = e.Get("cx_grain").Fundamental;

            e.Tick(TestData.World(2, multiplier: 2.0));

            double alpha = 1 - Math.Pow(0.5, 1.0 / 14);
            PriceState ps = e.Get("cx_grain");
            Assert.Equal((double)f0 * Math.Pow(2, alpha), (double)ps.Fundamental, 3);
            Assert.Equal(2 * f0, ps.RawFundamental);
        }

        [Fact]
        public void WithSmoothing_OneHalfLifeClosesHalfTheLogGap()
        {
            MarketEngine e = Engine(10);
            e.Tick(TestData.World(1));
            decimal f0 = e.Get("cx_grain").Fundamental;

            for (int d = 2; d <= 11; d++) e.Tick(TestData.World(d, multiplier: 4.0));

            // Ten days at half-life 10: ln F has covered half of ln 4, so F = 2 x F0.
            Assert.Equal(2.0 * (double)f0, (double)e.Get("cx_grain").Fundamental, 2);
        }

        [Fact]
        public void HalfLife_EasesFromStartToNormalOverTheRamp()
        {
            MarketEngine e = Engine(14, start: 45, ramp: 90);
            e.Tick(TestData.World(10));

            Assert.Equal(10, e.State.StartDay);
            Assert.Equal(45, e.SmoothingHalfLife(10), 9);
            Assert.Equal(29.5, e.SmoothingHalfLife(55), 9);
            Assert.Equal(14, e.SmoothingHalfLife(100), 9);
            Assert.Equal(14, e.SmoothingHalfLife(500), 9);
        }

        [Fact]
        public void DefaultRamp_EndsWithoutSmoothing_SoFundamentalFollowsTheWorldAgain()
        {
            MarketEngine e = Engine(0, start: 45, ramp: 90);
            e.Tick(TestData.World(1));
            decimal f0 = e.Get("cx_grain").Fundamental;

            e.Tick(TestData.World(2, multiplier: 2.0));
            Assert.True(e.Get("cx_grain").Fundamental < 1.05m * f0);

            Assert.Equal(22.5, e.SmoothingHalfLife(46), 9);
            Assert.Equal(0, e.SmoothingHalfLife(91));
            for (int d = 91; d <= 92; d++) e.Tick(TestData.World(d, multiplier: 2.0));
            Assert.Equal(2 * f0, e.Get("cx_grain").Fundamental);
        }

        [Fact]
        public void Schema3Save_GetsStartDayFromHistory_AndRawFundamental()
        {
            ExchangeState s3 = ExchangeState.CreateNew(1);
            MarketEngine e = Engine(14, 45, 90, s3.Market);
            for (int d = 5; d <= 8; d++) e.Tick(TestData.World(d));

            string json = StateSerializer.Serialize(s3)
                .Replace("\"schemaVersion\":" + ExchangeState.CurrentSchemaVersion, "\"schemaVersion\":3");
            json = Regex.Replace(json, "\"startDay\":-?\\d+,", "");
            json = Regex.Replace(json, "\"rawFundamental\":[0-9.]+,", "");
            Assert.DoesNotContain("startDay", json);
            Assert.DoesNotContain("rawFundamental", json);

            ExchangeState loaded = StateSerializer.Deserialize(json);

            Assert.Equal(ExchangeState.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(5, loaded.Market.StartDay);
            foreach (PriceState ps in loaded.Market.Prices)
            {
                if (ps.HasPrice) Assert.Equal(ps.Fundamental, ps.RawFundamental);
            }
        }

        [Fact]
        public void StartDayAndRawFundamental_RoundTrip()
        {
            ExchangeState state = ExchangeState.CreateNew(1);
            MarketEngine e = Engine(14, 45, 90, state.Market);
            e.Tick(TestData.World(3));
            e.Tick(TestData.World(4, multiplier: 1.5));

            ExchangeState loaded = StateSerializer.Deserialize(StateSerializer.Serialize(state));

            Assert.Equal(3, loaded.Market.StartDay);
            PriceState a = state.Market.Prices.Find(p => p.InstrumentId == "cx_grain");
            PriceState b = loaded.Market.Prices.Find(p => p.InstrumentId == "cx_grain");
            Assert.Equal(a.RawFundamental, b.RawFundamental);
            Assert.Equal(a.Fundamental, b.Fundamental);
        }
    }
}
