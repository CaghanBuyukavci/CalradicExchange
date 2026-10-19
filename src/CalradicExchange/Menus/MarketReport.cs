using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalradicExchange.Adapters;
using CalradicExchange.Behaviors;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Trading;
using CalradicExchange.Localization;
using CalradicExchange.UI;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradicExchange.Menus
{
    /// <summary>Builds the read-only texts: menu summary, portfolio and news (PLAN 1.18). Prices are in the market tables (TradeFlow).</summary>
    public static class MarketReport
    {
        public static string MenuText()
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady)
            {
                return Strings.ExchangeClosedText.SetTextVariable("REASON", b?.DisabledReason ?? Strings.ClosedReasonPrices.ToString()).ToString();
            }

            TraderProfile trader = b.CurrentTrader();
            decimal value = b.Portfolio.MarketValue(id => PriceOf(b, id));
            decimal open = b.Portfolio.UnrealizedPnl(id => PriceOf(b, id));
            TextObject t = Strings.ExchangeMenuText;
            t.SetTextVariable("VALUE", Fmt.Denars(value));
            t.SetTextVariable("PNL", Fmt.SignedDenars(open));
            t.SetTextVariable("USED", Fmt.Denars(value));
            t.SetTextVariable("LIMIT", Fmt.Denars(b.Orders.TotalLimit(trader)));
            t.SetTextVariable("FEE", Fmt.Pct(b.Orders.CommissionRate(trader.TradeSkill)).TrimStart('+'));
            t.SetTextVariable("TAX", Fmt.Pct(b.Market.Settings.Trading.ProfitTaxRate).TrimStart('+'));
            return t.ToString();
        }

        public static void ShowPortfolio()
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            ButtonSkins.EnsureApplied();
            if (TablePopup.Show("CxPortfolioTable", close => PortfolioTable(b, close))) return;
            Portfolio pf = b.Portfolio;

            var sb = new StringBuilder();
            if (pf.Positions.Count == 0) sb.AppendLine(Strings.PortfolioEmpty.ToString());
            foreach (Position pos in pf.Positions)
            {
                decimal price = PriceOf(b, pos.InstrumentId);
                TextObject line = Strings.PortfolioLine;
                line.SetTextVariable("NAME", Names.Instrument(b.Catalog.Find(pos.InstrumentId)));
                line.SetTextVariable("QTY", pos.Quantity);
                line.SetTextVariable("AVG", Fmt.Price(pos.AverageCost));
                line.SetTextVariable("PRICE", Fmt.Price(price));
                line.SetTextVariable("PNL", Fmt.SignedDenars(pos.Quantity * price - pos.CostBasis));
                sb.AppendLine(line.ToString());
            }
            sb.AppendLine(" ");
            TextObject totals = Strings.PortfolioTotals;
            totals.SetTextVariable("VALUE", Fmt.Denars(pf.MarketValue(id => PriceOf(b, id))));
            totals.SetTextVariable("OPEN", Fmt.SignedDenars(pf.UnrealizedPnl(id => PriceOf(b, id))));
            totals.SetTextVariable("REALIZED", Fmt.SignedDenars(pf.RealizedPnl));
            totals.SetTextVariable("FEES", Fmt.Denars(pf.TotalCommissionPaid));
            totals.SetTextVariable("TAXES", Fmt.Denars(pf.TotalProfitTaxPaid));
            sb.AppendLine(totals.ToString());
            Show(Strings.PortfolioTitle.ToString(), sb.ToString());
        }

        private static ExchangeTableVM PortfolioTable(ExchangeCampaignBehavior b, Action close)
        {
            Portfolio pf = b.Portfolio;
            TextObject totals = Strings.PortfolioTotals;
            totals.SetTextVariable("VALUE", Fmt.Denars(pf.MarketValue(id => PriceOf(b, id))));
            totals.SetTextVariable("OPEN", Fmt.SignedDenars(pf.UnrealizedPnl(id => PriceOf(b, id))));
            totals.SetTextVariable("REALIZED", Fmt.SignedDenars(pf.RealizedPnl));
            totals.SetTextVariable("FEES", Fmt.Denars(pf.TotalCommissionPaid));
            totals.SetTextVariable("TAXES", Fmt.Denars(pf.TotalProfitTaxPaid));
            string footer = pf.Positions.Count == 0 ? Strings.PortfolioEmpty + "\n" + totals : totals.ToString();

            var vm = new ExchangeTableVM(Strings.PortfolioTitle.ToString(),
                new[]
                {
                    Strings.ColInstrument.ToString(), Strings.ColQuantity.ToString(), Strings.ColBoughtAt.ToString(),
                    Strings.ColNow.ToString(), Strings.ColProfit.ToString(), Strings.ColProfitPct.ToString(),
                },
                footer, Strings.Close.ToString(), close);

            string buyText = Strings.BtnBuy.ToString(), sellText = Strings.BtnSell.ToString();
            Action back = ShowPortfolio;
            bool alternate = false;
            foreach (Position pos in pf.Positions)
            {
                decimal price = PriceOf(b, pos.InstrumentId);
                decimal pnl = pos.Quantity * price - pos.CostBasis;
                double pct = pos.CostBasis > 0 ? (double)(pnl / pos.CostBasis) : 0;
                PriceState ps = b.Market.Get(pos.InstrumentId);
                bool open = ps != null && ps.HasPrice && !ps.Closed;
                string id = pos.InstrumentId;
                vm.PortfolioRows.Add(new PortfolioRowVM(Names.Instrument(b.Catalog.Find(id)), pos.Quantity.ToString(),
                    Fmt.Price(pos.AverageCost), Fmt.Price(price), Fmt.SignedDenars(pnl), Fmt.Pct(pct), TableColors.ForAmount(pnl), alternate,
                    open, open, buyText, sellText,
                    () => TradeFlow.StartTrade(id, OrderSide.Buy, back),
                    () => TradeFlow.StartTrade(id, OrderSide.Sell, back)));
                alternate = !alternate;
            }
            return vm;
        }

        public static void ShowNews()
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;

            List<NewsItem> news = b.Market.State.News;
            var sb = new StringBuilder();
            if (news.Count == 0) sb.AppendLine(Strings.NewsEmpty.ToString());
            int today = b.NextDay;
            foreach (NewsItem item in Enumerable.Reverse(news).Take(15))
            {
                int ago = System.Math.Max(0, today - item.Day);
                TextObject when = ago == 0 ? Strings.NewsToday : Strings.NewsDaysAgo.SetTextVariable("DAYS", ago);
                TextObject line = Strings.NewsLine;
                line.SetTextVariable("WHEN", when);
                line.SetTextVariable("TEXT", Describe(item.Event));
                sb.AppendLine(line.ToString());
            }
            Show(Strings.NewsTitle.ToString(), sb.ToString());
        }

        /// <summary>One-line description of an event, used by the news list.</summary>
        public static string Describe(MarketEvent ev)
        {
            TextObject t = Strings.EventText(ev.Type);
            t.SetTextVariable("VILLAGE", Names.Settlement(ev.SettlementId));
            t.SetTextVariable("ITEM", Names.Item(ev.ItemId));
            t.SetTextVariable("TOWN", Names.Settlement(ev.TownId));
            t.SetTextVariable("K1", Names.Kingdom(ev.KingdomId));
            t.SetTextVariable("K2", Names.Kingdom(ev.OtherKingdomId));
            if (ev.Season != null) t.SetTextVariable("SEASON", Fmt.Season(ev.Season.Value));
            return t.ToString();
        }

        public static decimal PriceOf(ExchangeCampaignBehavior b, string id)
        {
            PriceState ps = b.Market.Get(id);
            return ps != null && ps.HasPrice ? ps.Price : 0;
        }

        private static void Show(string title, string text)
        {
            InformationManager.ShowInquiry(new InquiryData(title, text, true, false, Strings.Close.ToString(), "", null, null));
        }
    }
}
