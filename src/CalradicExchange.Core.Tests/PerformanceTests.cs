using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Charts;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class PerformanceTests
    {
        private static Portfolio Holding(decimal cost, int qty)
        {
            var pf = new Portfolio();
            pf.ApplyBuy("a", qty, (int)cost, 0);
            return pf;
        }

        [Fact]
        public void TotalPnl_AddsOpenRealizedAndGuildProfit_AndIgnoresBuying()
        {
            Portfolio pf = Holding(1000, 10);
            pf.RealizedPnl = 50;
            pf.GuildIncomeTotal = 20;
            pf.GuildAccruals["g"] = 5;
            Assert.Equal(200m + 50 + 20 + 5, pf.TotalPnl(id => 120m));

            // Buying at the market price moves the value, not the profit.
            decimal before = pf.TotalPnl(id => 120m);
            pf.ApplyBuy("a", 5, 600, 0);
            Assert.Equal(before, pf.TotalPnl(id => 120m));
        }

        [Fact]
        public void RecordPerformance_KeepsOnePointPerDay_AndTheNewestDays()
        {
            Portfolio pf = Holding(1000, 10);
            pf.RecordPerformance(1, id => 100m, 3);
            pf.RecordPerformance(1, id => 110m, 3);
            Assert.Single(pf.Performance);
            Assert.Equal(100m, pf.Performance[0].Pnl);

            for (int d = 2; d <= 5; d++) pf.RecordPerformance(d, id => 100m + d, 3);
            Assert.Equal(new[] { 3, 4, 5 }, pf.Performance.Select(p => p.Day));
            Assert.Equal(1050m, pf.Performance.Last().Value);
        }

        [Fact]
        public void Performance_SurvivesSaveAndLoad()
        {
            ExchangeState state = ExchangeState.CreateNew(1);
            state.Portfolio.RecordPerformance(4, id => 1m, 60);
            string json = StateSerializer.Serialize(state);
            ExchangeState back = StateSerializer.Deserialize(json);
            Assert.Equal(json, StateSerializer.Serialize(back));
            Assert.Equal(4, back.Portfolio.Performance.Single().Day);
        }

        [Fact]
        public void LineChart_PutsZeroAtTheBottom_WhenAllValuesArePositive()
        {
            ChartLayout l = LineChart.Layout(new double[] { 0, 1, 2 }, new double[] { 0, 50, 100 }, 10, 200, 2);
            Assert.Equal(10, l.Columns.Count);
            Assert.Equal(0, l.Min);
            Assert.Equal(199f, l.ZeroY, 3);
            // Rising line: each slice sits higher than the previous one, and stays inside the chart.
            for (int i = 1; i < l.Columns.Count; i++) Assert.True(l.Columns[i].LineTop < l.Columns[i - 1].LineTop);
            Assert.All(l.Columns, c => Assert.True(c.LineTop >= 0 && c.LineTop + c.LineHeight <= 200.001f && c.LineHeight >= 2));
            Assert.All(l.Columns, c => Assert.True(c.Positive));
        }

        [Fact]
        public void LineChart_FillsBelowZeroForLosses_AndCatchesPeaksInsideASlice()
        {
            ChartLayout l = LineChart.Layout(new double[] { 0, 1, 2 }, new double[] { -100, 100, -100 }, 3, 100, 2);
            Assert.True(l.Min < -100 && l.Max > 100);
            Assert.True(l.ZeroY > 40 && l.ZeroY < 60);
            Assert.False(l.Columns[0].Positive);
            Assert.True(l.Columns[1].Positive);
            // The peak at x = 1 is inside the middle slice, so that slice's line reaches the top value.
            Assert.True(l.Columns[1].LineTop < 10);
        }

        [Fact]
        public void LineChart_HandlesOnePointAndFlatZero()
        {
            ChartLayout one = LineChart.Layout(new double[] { 5 }, new double[] { 42 }, 4, 100, 2);
            Assert.Equal(4, one.Columns.Count);
            Assert.Equal(one.Columns[0].LineTop, one.Columns[3].LineTop);

            ChartLayout flat = LineChart.Layout(new double[] { 0, 1 }, new double[] { 0, 0 }, 2, 100, 2);
            Assert.Equal(99f, flat.ZeroY, 3);
            Assert.Equal(0f, flat.Columns[0].FillHeight, 3);
            Assert.Equal(0d, LineChart.ValueAt(new List<double> { 0, 2 }, new List<double> { 0, 10 }, -1));
            Assert.Equal(5d, LineChart.ValueAt(new List<double> { 0, 2 }, new List<double> { 0, 10 }, 1));
        }
    }
}
