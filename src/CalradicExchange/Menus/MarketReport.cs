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
            decimal debt = b.Portfolio.TotalGuildDebt;
            return debt > 0 ? t + "\n" + Strings.MenuGuildDebt.SetTextVariable("DEBT", Fmt.Denars(debt)) : t.ToString();
        }

        public static void ShowPortfolio() => ShowPortfolio(null);

        /// <summary>
        /// Portfolio table. <paramref name="back"/> reopens the window it was opened from (a market, the news) when
        /// the portfolio is closed; null when it was opened from the menu.
        /// </summary>
        public static void ShowPortfolio(Action back)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            ButtonSkins.EnsureApplied();
            if (TablePopup.Show("CxPortfolioTable", close => PortfolioTable(b, () => { close(); back?.Invoke(); }, back))) return;
            Portfolio pf = b.Portfolio;

            var sb = new StringBuilder();
            if (pf.Positions.Count == 0) sb.AppendLine(Strings.PortfolioEmpty.ToString());
            foreach (Position pos in pf.Positions)
            {
                decimal price = PriceOf(b, pos.InstrumentId);
                TextObject line = Strings.PortfolioLine;
                line.SetTextVariable("NAME", Names.Instrument(b.Market.Find(pos.InstrumentId)));
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

        private static ExchangeTableVM PortfolioTable(ExchangeCampaignBehavior b, Action close, Action back)
        {
            Portfolio pf = b.Portfolio;
            TextObject totals = Strings.PortfolioTotals;
            totals.SetTextVariable("VALUE", Fmt.Denars(pf.MarketValue(id => PriceOf(b, id))));
            totals.SetTextVariable("OPEN", Fmt.SignedDenars(pf.UnrealizedPnl(id => PriceOf(b, id))));
            totals.SetTextVariable("REALIZED", Fmt.SignedDenars(pf.RealizedPnl));
            totals.SetTextVariable("FEES", Fmt.Denars(pf.TotalCommissionPaid));
            totals.SetTextVariable("TAXES", Fmt.Denars(pf.TotalProfitTaxPaid));
            string footer = pf.Positions.Count == 0 ? Strings.PortfolioEmpty + "\n" + totals : totals.ToString();
            if (pf.GuildIncomeTotal != 0 || pf.GuildAccruals.Count > 0 || pf.GuildDebts.Count > 0)
            {
                TextObject guilds = Strings.PortfolioGuildTotals;
                guilds.SetTextVariable("INCOME", Fmt.SignedDenars(pf.GuildIncomeTotal));
                guilds.SetTextVariable("PENDING", Fmt.SignedDenars(pf.GuildAccruals.Values.Sum()));
                guilds.SetTextVariable("DEBT", Fmt.Denars(pf.TotalGuildDebt));
                footer += "\n" + guilds;
            }

            var vm = new ExchangeTableVM(Strings.PortfolioTitle.ToString(),
                new[]
                {
                    Strings.ColInstrument.ToString(), Strings.ColQuantity.ToString(), Strings.ColBoughtAt.ToString(),
                    Strings.ColNow.ToString(), Strings.ColProfit.ToString(), Strings.ColProfitPct.ToString(),
                },
                footer, Strings.Close.ToString(), close);

            foreach (InstrumentClass tab in PortfolioTabs)
            {
                InstrumentClass cls = tab;
                int count = pf.Positions.Count(p => TabOf(b, p.InstrumentId) == cls);
                vm.Tabs.Add(new TabVM(TabLabel(cls).SetTextVariable("COUNT", count).ToString(), cls == _portfolioTab, () =>
                {
                    _portfolioTab = cls;
                    FillPortfolio(b, vm, back);
                }));
            }
            FillPortfolio(b, vm, back);
            return vm;
        }

        /// <summary>Portfolio tabs; instruments of other classes (none yet) are listed under the first one.</summary>
        private static readonly InstrumentClass[] PortfolioTabs = { InstrumentClass.Commodity, InstrumentClass.Sector, InstrumentClass.Guild, InstrumentClass.Kingdom };

        /// <summary>The selected portfolio tab, kept while the game runs so a trade from the portfolio comes back to it.</summary>
        private static InstrumentClass _portfolioTab = InstrumentClass.Commodity;

        private static InstrumentClass TabOf(ExchangeCampaignBehavior b, string id)
        {
            InstrumentClass cls = b.Market.Find(id)?.Class ?? InstrumentClass.Commodity;
            return Array.IndexOf(PortfolioTabs, cls) >= 0 ? cls : PortfolioTabs[0];
        }

        private static TextObject TabLabel(InstrumentClass cls)
        {
            switch (cls)
            {
                case InstrumentClass.Sector: return Strings.TabBaskets;
                case InstrumentClass.Guild: return Strings.TabGuilds;
                case InstrumentClass.Kingdom: return Strings.TabKingdoms;
                default: return Strings.TabShares;
            }
        }

        private static string NameHeader(InstrumentClass cls)
        {
            switch (cls)
            {
                case InstrumentClass.Sector: return Strings.ColBasket.ToString();
                case InstrumentClass.Guild: return Strings.ColGuild.ToString();
                case InstrumentClass.Kingdom: return Strings.ColKingdom.ToString();
                default: return Strings.ColInstrument.ToString();
            }
        }

        /// <summary>Fills the rows of the selected tab. A trade from a row comes back to the portfolio, which keeps <paramref name="outerBack"/>.</summary>
        private static void FillPortfolio(ExchangeCampaignBehavior b, ExchangeTableVM vm, Action outerBack)
        {
            Portfolio pf = b.Portfolio;
            for (int i = 0; i < vm.Tabs.Count; i++) vm.Tabs[i].IsSelected = PortfolioTabs[i] == _portfolioTab;
            vm.H1 = NameHeader(_portfolioTab);
            vm.PortfolioRows.Clear();

            string buyText = Strings.BtnBuy.ToString(), sellText = Strings.BtnSell.ToString();
            string here = b.CurrentTrader().TownId;
            Action back = () => ShowPortfolio(outerBack);
            bool alternate = false;
            foreach (Position pos in pf.Positions.Where(p => TabOf(b, p.InstrumentId) == _portfolioTab))
            {
                decimal price = PriceOf(b, pos.InstrumentId);
                decimal pnl = pos.Quantity * price - pos.CostBasis;
                double pct = pos.CostBasis > 0 ? (double)(pnl / pos.CostBasis) : 0;
                PriceState ps = b.Market.Get(pos.InstrumentId);
                bool open = ps != null && ps.HasPrice && !ps.Closed;
                string id = pos.InstrumentId;
                InstrumentDefinition def = b.Market.Find(id);
                bool canBuy = open && (def?.Class != InstrumentClass.Guild || def.TownId == here);
                vm.PortfolioRows.Add(new PortfolioRowVM(Names.Instrument(b.Market.Find(id)), pos.Quantity.ToString(),
                    Fmt.Price(pos.AverageCost), Fmt.Price(price), Fmt.SignedDenars(pnl), Fmt.Pct(pct), TableColors.ForAmount(pnl), alternate,
                    canBuy, open, buyText, sellText,
                    () => TradeFlow.StartTrade(id, OrderSide.Buy, back),
                    () => TradeFlow.StartTrade(id, OrderSide.Sell, back)));
                alternate = !alternate;
            }
            vm.EmptyText = vm.PortfolioRows.Count == 0 ? Strings.PortfolioTabEmpty.ToString() : "";
        }

        public static void ShowNews()
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            if (TablePopup.Show("CxNewsTable", close => NewsTable(b, close))) return;

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

        /// <summary>All kept news, newest first; kind and text in the color of the event's category.</summary>
        private static ExchangeTableVM NewsTable(ExchangeCampaignBehavior b, Action close)
        {
            var vm = new ExchangeTableVM(Strings.NewsTitle.ToString(),
                new[] { Strings.ColWhen.ToString(), Strings.ColNewsKind.ToString(), Strings.ColNews.ToString() },
                null, Strings.Close.ToString(), close);
            vm.WithSecondary(Strings.BtnPortfolio.ToString(), () => ShowPortfolio(ShowNews));

            int today = b.NextDay;
            bool alternate = false;
            foreach (NewsItem item in Enumerable.Reverse(b.Market.State.News))
            {
                int ago = Math.Max(0, today - item.Day);
                TextObject when = ago == 0 ? Strings.NewsToday : Strings.NewsDaysAgo.SetTextVariable("DAYS", ago);
                Color color = NewsColors.For(item.Event.Type);
                vm.Rows.Add(new GridRowVM(new[] { when.ToString(), Strings.NewsCategory(item.Event.Type).ToString(), Describe(item.Event) }, alternate)
                    .WithColor(1, TableColors.Muted)
                    .WithColor(2, color)
                    .WithColor(3, color));
                alternate = !alternate;
            }
            vm.EmptyText = vm.Rows.Count == 0 ? Strings.NewsEmpty.ToString() : "";
            return vm;
        }

        /// <summary>
        /// One-line description of an event, used by the news list. Places carry their current owner in parentheses,
        /// except in a capture, whose text already names both kingdoms.
        /// </summary>
        public static string Describe(MarketEvent ev)
        {
            if (ev.Type == MarketEventType.HeroCaptured && string.IsNullOrEmpty(ev.OtherHeroId))
            {
                return Strings.HeroCapturedByFaction.SetTextVariable("HERO", Names.Hero(ev.HeroId))
                    .SetTextVariable("K1", Names.Faction(ev.KingdomId)).SetTextVariable("K2", Names.Faction(ev.OtherKingdomId)).ToString();
            }
            bool owners = ev.Type != MarketEventType.SettlementCaptured;
            TextObject t = Strings.EventText(ev.Type);
            t.SetTextVariable("VILLAGE", owners ? Names.SettlementWithOwner(ev.SettlementId) : Names.Settlement(ev.SettlementId));
            t.SetTextVariable("ITEM", Names.Item(ev.ItemId));
            t.SetTextVariable("TOWN", owners ? Names.SettlementWithOwner(ev.TownId) : Names.Settlement(ev.TownId));
            t.SetTextVariable("K1", ev.Type == MarketEventType.HeroCaptured ? Names.Faction(ev.KingdomId) : Names.Kingdom(ev.KingdomId));
            t.SetTextVariable("K2", ev.Type == MarketEventType.HeroCaptured ? Names.Faction(ev.OtherKingdomId) : Names.Kingdom(ev.OtherKingdomId));
            t.SetTextVariable("HERO", Names.Hero(ev.HeroId));
            t.SetTextVariable("CAPTOR", Names.Hero(ev.OtherHeroId));
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
