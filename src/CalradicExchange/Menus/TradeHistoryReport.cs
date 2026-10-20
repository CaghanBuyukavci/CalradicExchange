using System;
using System.Linq;
using System.Text;
using CalradicExchange.Adapters;
using CalradicExchange.Behaviors;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Trading;
using CalradicExchange.Localization;
using CalradicExchange.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradicExchange.Menus
{
    /// <summary>The player's trade history table (Portfolio.History, schema 6), newest first.</summary>
    public static class TradeHistoryReport
    {
        /// <summary>Trades listed in the window, newest first. The save keeps more (trading.tradeHistoryLimit).</summary>
        private const int MaxRows = 100;

        public static void Show()
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            if (TablePopup.Show("CxHistoryTable", close => Table(b, close))) return;

            var sb = new StringBuilder();
            if (b.Portfolio.History.Count == 0) sb.AppendLine(Strings.HistoryEmpty.ToString());
            foreach (TradeRecord t in Enumerable.Reverse(b.Portfolio.History).Take(30))
            {
                string[] c = Cells(b, t);
                sb.AppendLine($"{c[0]}  {c[1]} {c[3]} {c[2]} @ {c[4]}  {c[5]}  ({c[7]})");
            }
            sb.AppendLine(" ");
            sb.AppendLine(Footer(b.Portfolio));
            InformationManager.ShowInquiry(new InquiryData(Strings.HistoryTitle.ToString(), sb.ToString(), true, false, Strings.Close.ToString(), "", null, null));
        }

        private static ExchangeTableVM Table(ExchangeCampaignBehavior b, Action close)
        {
            // Totals are in the performance window (ADR-035).
            Portfolio pf = b.Portfolio;

            var vm = new ExchangeTableVM(Strings.HistoryTitle.ToString(),
                new[]
                {
                    Strings.ColDate.ToString(), Strings.ColSide.ToString(), Strings.ColInstrument.ToString(), Strings.ColQuantity.ToString(),
                    Strings.ColPrice.ToString(), Strings.ColTotal.ToString(), Strings.ColCosts.ToString(), Strings.ColProfit.ToString(),
                },
                null, Strings.Close.ToString(), close);
            vm.WithSecondary(Strings.BtnPortfolio.ToString(), () => MarketReport.ShowPortfolio(Show));
            if (pf.History.Count == 0) vm.EmptyText = Strings.HistoryEmpty.ToString();
            vm.StatusText = Strings.HistorySessionTrades.SetTextVariable("COUNT", pf.RecordedSinceLoad).ToString();

            bool alternate = false;
            foreach (TradeRecord t in Enumerable.Reverse(pf.History).Take(MaxRows))
            {
                var row = new GridRowVM(Cells(b, t), alternate)
                    .WithColor(6, t.Side == OrderSide.Buy ? TableColors.Down : TableColors.Up);
                if (t.Side == OrderSide.Sell) row.WithColor(8, TableColors.ForAmount(t.RealizedPnl));
                vm.Rows.Add(row);
                alternate = !alternate;
            }
            return vm;
        }

        /// <summary>date | side | name | qty | price | total (signed gold flow) | fees + tax | realized profit.</summary>
        private static string[] Cells(ExchangeCampaignBehavior b, TradeRecord t)
        {
            InstrumentDefinition def = b.Market.Find(t.InstrumentId);
            bool buy = t.Side == OrderSide.Buy;
            return new[]
            {
                CampaignTime.Days(t.Day).ToString(),
                (buy ? Strings.SideBuy : t.Forced ? Strings.SideForced : Strings.SideSell).ToString(),
                def != null ? Names.Instrument(def) : t.InstrumentId,
                t.Quantity.ToString(),
                Fmt.Price(t.ExecutionPrice),
                Fmt.SignedDenars(buy ? -t.Total : t.Total),
                Fmt.Denars((long)t.Commission + t.ProfitTax),
                buy ? Strings.NoPrice.ToString() : Fmt.SignedDenars(t.RealizedPnl),
            };
        }

        private static string Footer(Portfolio pf)
        {
            TextObject totals = Strings.HistoryTotals;
            totals.SetTextVariable("COUNT", pf.TradeCount);
            totals.SetTextVariable("REALIZED", Fmt.SignedDenars(pf.RealizedPnl));
            totals.SetTextVariable("FEES", Fmt.Denars(pf.TotalCommissionPaid));
            totals.SetTextVariable("TAXES", Fmt.Denars(pf.TotalProfitTaxPaid));
            return totals.ToString();
        }
    }
}
