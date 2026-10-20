using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Behaviors;
using CalradicExchange.Core.Charts;
using CalradicExchange.Core.Trading;
using CalradicExchange.Localization;
using CalradicExchange.UI;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradicExchange.Menus
{
    /// <summary>
    /// Portfolio performance window (ADR-031): total profit over the last 7, 14 or 30 days as a line chart, from the
    /// daily points in Portfolio.Performance plus a live point for today.
    /// </summary>
    public static class PerformanceReport
    {
        private static readonly int[] Periods = { 7, 14, 30 };

        /// <summary>Chart size in the prefab (CxPerformance.xml). Slices are SliceWidth pixels wide; day marks are DotSize squares.</summary>
        public const float ChartWidth = 700, ChartHeight = 260, SliceWidth = 4, LineThickness = 3, DotSize = 6;

        /// <summary>The selected period, kept while the game runs.</summary>
        private static int _period = 7;

        private static readonly Color Gain = Color.ConvertStringToColor("#7CD67CFF");
        private static readonly Color Loss = Color.ConvertStringToColor("#E87070FF");

        /// <summary>Opens the window; closing it runs <paramref name="back"/> (the portfolio).</summary>
        public static void Show(Action back)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            TablePopup.Show("CxPerformance", close => Build(b, () => { close(); back?.Invoke(); }));
        }

        private static PerformanceVM Build(ExchangeCampaignBehavior b, Action close)
        {
            var vm = new PerformanceVM(Strings.PerformanceTitle.ToString(), Strings.Close.ToString(), close);
            foreach (int period in Periods)
            {
                int days = period;
                vm.Tabs.Add(new TabVM(Strings.PerformancePeriod.SetTextVariable("DAYS", days).ToString(), days == _period, () =>
                {
                    _period = days;
                    Fill(b, vm);
                }));
            }
            Fill(b, vm);
            return vm;
        }

        private static void Fill(ExchangeCampaignBehavior b, PerformanceVM vm)
        {
            for (int i = 0; i < vm.Tabs.Count; i++) vm.Tabs[i].IsSelected = Periods[i] == _period;

            Portfolio pf = b.Portfolio;
            int today = b.State.Market.LastTickDay;
            PerformancePoint now = pf.PerformanceAt(today, id => MarketReport.PriceOf(b, id));
            List<PerformancePoint> points = pf.Performance.Where(p => p.Day >= today - _period && p.Day < today).ToList();
            points.Add(now);

            TextObject totals = Strings.PerformanceTotals;
            totals.SetTextVariable("PNL", Fmt.SignedDenars(now.Pnl));
            totals.SetTextVariable("VALUE", Fmt.Denars(now.Value));

            FillSummary(b, vm, now);
            vm.Columns.Clear();
            vm.DayTicks.Clear();
            if (points.Count < 2)
            {
                vm.Set("", TableColors.Neutral, totals.ToString(), Strings.PerformanceNotEnough.ToString(), ChartHeight, ChartHeight, "", "", "", "", "");
                return;
            }

            decimal change = now.Pnl - points[0].Pnl;
            int covered = today - points[0].Day;
            TextObject head = covered < _period ? Strings.PerformanceChangePartial.SetTextVariable("COVERED", covered) : Strings.PerformanceChange;
            head.SetTextVariable("DAYS", _period);
            head.SetTextVariable("CHANGE", Fmt.SignedDenars(change));

            ChartLayout layout = LineChart.Layout(points.Select(p => (double)p.Day).ToList(), points.Select(p => (double)p.Pnl).ToList(),
                (int)(ChartWidth / SliceWidth), ChartHeight, LineThickness);
            foreach (ChartColumn c in layout.Columns)
            {
                vm.Columns.Add(new ChartColumnVM(c.LineTop, c.LineHeight, c.FillTop, c.FillHeight, c.Positive ? Gain : Loss));
            }

            // One dot per day; each list slot ends at its dot's right edge, kept inside the chart.
            float previous = 0;
            foreach (float x in LineChart.Ticks(points[0].Day, today, ChartWidth))
            {
                float right = Math.Min(ChartWidth, Math.Max(DotSize, x + DotSize / 2));
                vm.DayTicks.Add(new ChartTickVM(right - previous));
                previous = right;
            }
            vm.Set(head.ToString(), TableColors.ForAmount(change), totals.ToString(), "", layout.ZeroY, ChartHeight,
                layout.Max > 0 ? Fmt.SignedDenars((decimal)layout.Max) : "",
                "0",
                layout.Min < 0 ? Fmt.SignedDenars((decimal)layout.Min) : "",
                Strings.NewsDaysAgo.SetTextVariable("DAYS", covered).ToString(), Strings.NewsToday.ToString());
        }

        /// <summary>The portfolio totals that used to be under the portfolio and history tables (ADR-035).</summary>
        private static void FillSummary(ExchangeCampaignBehavior b, PerformanceVM vm, PerformancePoint now)
        {
            Portfolio pf = b.Portfolio;
            decimal open = pf.UnrealizedPnl(id => MarketReport.PriceOf(b, id));
            decimal pending = pf.GuildAccruals.Values.Sum();
            vm.Summary.Clear();
            Add(vm, Strings.SumValue, Denars(Fmt.Denars(now.Value)), null);
            Add(vm, Strings.SumOpen, Denars(Fmt.SignedDenars(open)), TableColors.ForAmount(open));
            Add(vm, Strings.SumRealized, Denars(Fmt.SignedDenars(pf.RealizedPnl)), TableColors.ForAmount(pf.RealizedPnl));
            Add(vm, Strings.SumGuildIncome, Denars(Fmt.SignedDenars(pf.GuildIncomeTotal)), TableColors.ForAmount(pf.GuildIncomeTotal));
            Add(vm, Strings.SumGuildPending, Denars(Fmt.SignedDenars(pending)), TableColors.ForAmount(pending));
            Add(vm, Strings.SumGuildDebt, Denars(Fmt.Denars(pf.TotalGuildDebt)), pf.TotalGuildDebt > 0 ? TableColors.Down : (Color?)null);
            Add(vm, Strings.SumFees, Denars(Fmt.Denars(pf.TotalCommissionPaid)), null);
            Add(vm, Strings.SumTaxes, Denars(Fmt.Denars(pf.TotalProfitTaxPaid)), null);
            Add(vm, Strings.SumTrades, pf.TradeCount.ToString(), null);
            Add(vm, Strings.SumTotal, Denars(Fmt.SignedDenars(now.Pnl)), TableColors.ForAmount(now.Pnl), true);
        }

        private static void Add(PerformanceVM vm, TextObject label, string value, Color? color, bool total = false)
        {
            var row = new GridRowVM(new[] { label.ToString(), value }, total);
            if (color != null) row.WithColor(2, color.Value);
            vm.Summary.Add(row);
        }

        private static string Denars(string amount) => Strings.DenarsAmount.SetTextVariable("AMOUNT", amount).ToString();
    }
}
