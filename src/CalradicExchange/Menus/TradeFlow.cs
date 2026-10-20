using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Adapters;
using CalradicExchange.Behaviors;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Trading;
using CalradicExchange.Diagnostics;
using CalradicExchange.Localization;
using CalradicExchange.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradicExchange.Menus
{
    /// <summary>
    /// Buy and sell flow (PLAN 1.19, 1.20, ADR-025): a market table per instrument class with Buy / Sell buttons on
    /// every row -> quantity (slider popup, text inquiry as fallback) -> confirmation with the full cost breakdown ->
    /// execute in Core and move the hero's gold -> back to the same market table.
    /// </summary>
    public static class TradeFlow
    {
        /// <summary>Market tabs of the Markets window, left to right.</summary>
        private static readonly InstrumentClass[] MarketTabs = { InstrumentClass.Commodity, InstrumentClass.Sector, InstrumentClass.Guild, InstrumentClass.Kingdom };

        /// <summary>The selected market tab, kept while the game runs so the menu and a finished trade come back to it.</summary>
        private static InstrumentClass _marketTab = InstrumentClass.Commodity;

        /// <summary>Opens the Markets window on the last selected tab.</summary>
        public static void ShowMarkets() => ShowMarket(_marketTab);

        /// <summary>
        /// Markets window on the tab of <paramref name="cls"/>: commodity shares, baskets (InstrumentClass.Sector),
        /// guild partnerships or kingdom indices. <paramref name="search"/> refills the commodity search box.
        /// </summary>
        public static void ShowMarket(InstrumentClass cls, string search = "")
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            if (Array.IndexOf(MarketTabs, cls) >= 0) _marketTab = cls;
            ButtonSkins.EnsureApplied();
            if (TablePopup.Show("CxTradeTable", close => MarketTable(b, search, close))) return;
            if (_marketTab != InstrumentClass.Guild) ShowMarketFallback(b, _marketTab);
            else ExchangeLog.Warn("Guild market table could not be shown.");
        }

        public static void ShowGuildMarket() => ShowMarket(InstrumentClass.Guild);

        /// <summary>"Gold: ... / Limit left: ..." for the bottom-left corner of the market tables.</summary>
        public static string GoldAndLimit(ExchangeCampaignBehavior b)
        {
            TraderProfile trader = b.CurrentTrader();
            decimal held = b.Portfolio.MarketValue(id => MarketReport.PriceOf(b, id));
            TextObject t = Strings.TradeDescription;
            t.SetTextVariable("GOLD", trader.Gold);
            t.SetTextVariable("LEFT", Fmt.Denars(Math.Max(0, b.Orders.TotalLimit(trader) - held)));
            return t.ToString();
        }

        private static ExchangeTableVM MarketTable(ExchangeCampaignBehavior b, string search, Action close)
        {
            var vm = new ExchangeTableVM("", null, null, Strings.Close.ToString(), close);
            vm.StatusText = GoldAndLimit(b);
            vm.WithSecondary(Strings.BtnPortfolio.ToString(), () => MarketReport.ShowPortfolio(() => ShowMarket(_marketTab, vm.SearchText)));

            // Rows of the selected tab before filtering; the search box filters the commodity tab.
            var rows = new List<KeyValuePair<string, TradeRowVM>>();
            foreach (InstrumentClass tab in MarketTabs)
            {
                InstrumentClass cls = tab;
                vm.Tabs.Add(new TabVM(MarketTabLabel(cls).ToString(), cls == _marketTab, () =>
                {
                    _marketTab = cls;
                    FillMarket(b, vm, rows);
                }));
            }
            vm.WithSearch(Strings.SearchPlaceholder.ToString(), search, text =>
            {
                if (_marketTab == InstrumentClass.Commodity) FillRows(vm, rows, text);
            });
            FillMarket(b, vm, rows);
            return vm;
        }

        /// <summary>Refills title, description, headers and rows for the selected tab.</summary>
        private static void FillMarket(ExchangeCampaignBehavior b, ExchangeTableVM vm, List<KeyValuePair<string, TradeRowVM>> rows)
        {
            InstrumentClass cls = _marketTab;
            for (int i = 0; i < vm.Tabs.Count; i++) vm.Tabs[i].IsSelected = MarketTabs[i] == cls;
            vm.Title = MarketTitle(cls);
            vm.ShowSearch(cls == InstrumentClass.Commodity);
            vm.EmptyText = "";
            rows.Clear();
            vm.TradeRows.Clear();
            vm.OtherRows.Clear();
            vm.OtherTitle = "";
            Action back = () => ShowMarket(cls, vm.SearchText);
            if (cls == InstrumentClass.Guild)
            {
                FillGuilds(b, vm, back);
                return;
            }

            if (cls == InstrumentClass.Commodity) vm.Description = Strings.HeaderCommodities.SetTextVariable("UNITS", b.Catalog.UnitsPerShare).ToString();
            else if (cls == InstrumentClass.Kingdom) vm.Description = Strings.KingdomMarketDescription.ToString();
            else vm.Description = "";

            TextObject nameHeader = cls == InstrumentClass.Kingdom ? Strings.ColKingdom : cls == InstrumentClass.Sector ? Strings.ColBasket : Strings.ColInstrument;
            vm.SetHeaders(new[]
            {
                nameHeader.ToString(), Strings.ColPrice.ToString(),
                Strings.ColToday.ToString(), Strings.ColWeek.ToString(), Strings.ColHeld.ToString(),
            });

            string buyText = Strings.BtnBuy.ToString(), sellText = Strings.BtnSell.ToString(), none = Strings.NoPrice.ToString();
            foreach (InstrumentDefinition def in b.Market.Instruments.Where(i => i.Class == cls))
            {
                PriceState ps = b.Market.Get(def.Id);
                if (cls == InstrumentClass.Kingdom && ps != null && ps.Closed) continue; // fallen kingdoms
                bool priced = ps != null && ps.HasPrice;
                bool open = priced && !ps.Closed;
                int qty = b.Portfolio.FreeQuantity(def.Id); // shares in pending orders are not for sale (ADR-039)
                double day = priced ? ps.ChangeOver(1) : 0, week = priced ? ps.ChangeOver(7) : 0;
                string id = def.Id;
                string name = Names.ShortInstrument(def);
                rows.Add(new KeyValuePair<string, TradeRowVM>(name, new TradeRowVM(name,
                    priced ? Fmt.Price(ps.Price) : none,
                    priced ? Fmt.Pct(day) : none, TableColors.ForChange(day),
                    priced ? Fmt.Pct(week) : none, TableColors.ForChange(week),
                    qty > 0 ? qty.ToString() : none,
                    open, open && qty > 0, false, buyText, sellText,
                    () => Guard("buy pick", () => AskQuantity(id, OrderSide.Buy, back)),
                    () => Guard("sell pick", () => AskQuantity(id, OrderSide.Sell, back)))));
            }
            FillRows(vm, rows, cls == InstrumentClass.Commodity ? vm.SearchText : "");
        }

        /// <summary>Shows the rows whose name contains <paramref name="search"/> (case-insensitive), shading every other one.</summary>
        private static void FillRows(ExchangeTableVM vm, List<KeyValuePair<string, TradeRowVM>> rows, string search)
        {
            string s = (search ?? "").Trim();
            vm.TradeRows.Clear();
            bool alternate = false;
            foreach (KeyValuePair<string, TradeRowVM> row in rows)
            {
                if (s.Length > 0 && row.Key.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                row.Value.IsAlternate = alternate;
                vm.TradeRows.Add(row.Value);
                alternate = !alternate;
            }
            vm.EmptyText = vm.TradeRows.Count == 0 && s.Length > 0 ? Strings.SearchNoMatch.ToString() : "";
        }

        /// <summary>
        /// Guild tab (ADR-026): partnership shares of this town's guilds (Buy and Sell); the player's shares in other
        /// towns (Sell only) go to the second section, which has its own header and scroll and sits right below.
        /// </summary>
        private static void FillGuilds(ExchangeCampaignBehavior b, ExchangeTableVM vm, Action back)
        {
            TraderProfile trader = b.CurrentTrader();
            vm.Description = Strings.GuildMarketDescription.ToString();
            vm.SetHeaders(new[]
            {
                Strings.ColGuild.ToString(), Strings.ColPrice.ToString(), Strings.ColWeek.ToString(),
                Strings.ColWeekProfitPerShare.ToString(), Strings.ColHeld.ToString(),
            });

            vm.TradeRows.Add(new TradeRowVM(Strings.GuildSectionLocal.SetTextVariable("TOWN", Names.Settlement(trader.TownId)).ToString()));
            List<InstrumentDefinition> local = b.Market.Guilds.Where(g => g.TownId == trader.TownId && !(b.Market.Get(g.Id)?.Closed ?? true)).ToList();
            if (local.Count == 0) vm.TradeRows.Add(new TradeRowVM(Strings.GuildNoneHere.ToString()));
            bool alternate = false;
            foreach (InstrumentDefinition def in local)
            {
                vm.TradeRows.Add(GuildRow(b, trader, def, true, alternate, back));
                alternate = !alternate;
            }

            List<InstrumentDefinition> elsewhere = b.Portfolio.Positions
                .Select(p => b.Market.Find(p.InstrumentId))
                .Where(d => d != null && d.Class == InstrumentClass.Guild && d.TownId != trader.TownId)
                .ToList();
            alternate = false;
            foreach (InstrumentDefinition def in elsewhere)
            {
                vm.OtherRows.Add(GuildRow(b, trader, def, false, alternate, back));
                alternate = !alternate;
            }
            vm.OtherTitle = elsewhere.Count > 0 ? Strings.GuildSectionOther.ToString() : "";
            // The main list and the second section share about 475 px; the second one starts right below this town's rows.
            vm.OtherHeight = Math.Max(236, Math.Min(400, 475 - (vm.TradeRows.Count * 36 + 12)));
        }

        private static TradeRowVM GuildRow(ExchangeCampaignBehavior b, TraderProfile trader, InstrumentDefinition def, bool local, bool alternate, Action back)
        {
            PriceState ps = b.Market.Get(def.Id);
            bool priced = ps != null && ps.HasPrice;
            bool open = priced && !ps.Closed;
            int qty = b.Portfolio.QuantityOf(def.Id);
            double week = priced ? ps.ChangeOver(7) : 0;
            double weekProfit = WeekProfitPerShare(b, def.Id);
            string none = Strings.NoPrice.ToString();
            string id = def.Id;
            bool own = trader.OwnGuilds.Contains(id);
            string name = own ? Strings.OwnWorkshopMark.SetTextVariable("NAME", Names.Instrument(def)).ToString() : Names.Instrument(def);
            return new TradeRowVM(name,
                priced ? Fmt.Price(ps.Price) : none,
                priced ? Fmt.Pct(week) : none, TableColors.ForChange(week),
                Fmt.SignedDenars((decimal)weekProfit), TableColors.ForAmount((decimal)weekProfit),
                qty > 0 ? qty.ToString() : none,
                open && local && !own, open && qty > 0, alternate, Strings.BtnBuy.ToString(), Strings.BtnSell.ToString(),
                () => Guard("buy pick", () => AskQuantity(id, OrderSide.Buy, back)),
                () => Guard("sell pick", () => AskQuantity(id, OrderSide.Sell, back)));
        }

        /// <summary>Profit of the guild's last 7 recorded days for one partnership share.</summary>
        private static double WeekProfitPerShare(ExchangeCampaignBehavior b, string id)
        {
            var series = b.Market.GuildSeries(id);
            if (series == null || series.DailyProfit.Count == 0) return 0;
            double week = series.DailyProfit.Skip(Math.Max(0, series.DailyProfit.Count - 7)).Sum();
            return week / Math.Max(1, b.Market.Settings.Guilds?.SharesPerGuild ?? 100);
        }

        private static string MarketTitle(InstrumentClass cls)
        {
            switch (cls)
            {
                case InstrumentClass.Commodity: return Strings.CommodityMarketTitle.ToString();
                case InstrumentClass.Guild: return Strings.GuildMarketTitle.ToString();
                case InstrumentClass.Kingdom: return Strings.KingdomMarketTitle.ToString();
                default: return Strings.BasketMarketTitle.ToString();
            }
        }

        private static TextObject MarketTabLabel(InstrumentClass cls)
        {
            switch (cls)
            {
                case InstrumentClass.Sector: return Strings.MarketTabBaskets;
                case InstrumentClass.Guild: return Strings.MarketTabGuilds;
                case InstrumentClass.Kingdom: return Strings.MarketTabIndices;
                default: return Strings.MarketTabShares;
            }
        }

        /// <summary>Vanilla fallback when the Gauntlet table cannot be shown (ADR-006): pick an instrument, then buy or sell.</summary>
        private static void ShowMarketFallback(ExchangeCampaignBehavior b, InstrumentClass cls)
        {
            var elements = new List<InquiryElement>();
            foreach (InstrumentDefinition def in b.Market.Instruments.Where(i => i.Class == cls))
            {
                PriceState ps = b.Market.Get(def.Id);
                if (ps == null || !ps.HasPrice || ps.Closed) continue;
                TextObject title = Strings.BuyElement;
                title.SetTextVariable("NAME", Names.Instrument(def));
                title.SetTextVariable("PRICE", Fmt.Price(ps.Price));
                elements.Add(new InquiryElement(def.Id, title.ToString(), Icon(def), true, Fmt.Pct(ps.ChangeOver(7))));
            }
            if (elements.Count == 0) return;

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                MarketTitle(cls), "", elements, true, 1, 1, Strings.Continue.ToString(), Strings.Cancel.ToString(),
                chosen => Guard("fallback pick", () => ChooseSide(b, (string)chosen.First().Identifier, () => ShowMarket(cls))),
                null));
        }

        private static void ChooseSide(ExchangeCampaignBehavior b, string id, Action back)
        {
            if (b.Portfolio.FreeQuantity(id) <= 0)
            {
                AskQuantity(id, OrderSide.Buy, back);
                return;
            }
            InformationManager.ShowInquiry(new InquiryData(Names.Instrument(b.Market.Find(id)), "", true, true,
                Strings.BtnBuy.ToString(), Strings.BtnSell.ToString(),
                () => Guard("fallback buy", () => AskQuantity(id, OrderSide.Buy, back)),
                () => Guard("fallback sell", () => AskQuantity(id, OrderSide.Sell, back))));
        }

        /// <summary>
        /// Starts a buy or sell from another table (portfolio). <paramref name="back"/> reopens that table after the
        /// trade or when the player backs out.
        /// </summary>
        public static void StartTrade(string id, OrderSide side, Action back)
        {
            Guard(side == OrderSide.Buy ? "buy" : "sell", () => AskQuantity(id, side, back));
        }

        /// <summary>Quantity step. <paramref name="back"/> runs after the trade or when the player backs out.</summary>
        private static void AskQuantity(string id, OrderSide side, Action back)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            InstrumentDefinition def = b.Market.Find(id);
            TraderProfile trader = b.CurrentTrader();
            string name = Names.Instrument(def);
            bool guild = def?.Class == InstrumentClass.Guild;

            int max;
            TextObject prompt;
            if (side == OrderSide.Buy)
            {
                max = b.Orders.MaxBuyQuantity(id, trader, b.Portfolio);
                if (max <= 0)
                {
                    // Explain the first blocking reason using a one-share quote; the market table stays open.
                    ShowError(b.Orders.Quote(id, OrderSide.Buy, 1, trader, b.Portfolio).Error);
                    return;
                }
                prompt = guild ? Strings.QuantityBuyGuild : Strings.QuantityBuy;
                prompt.SetTextVariable("PRICE", Fmt.Price(MarketReport.PriceOf(b, id)));
            }
            else
            {
                max = b.Portfolio.FreeQuantity(id);
                if (max <= 0) return;
                prompt = guild ? Strings.QuantitySellGuild : Strings.QuantitySell;
                prompt.SetTextVariable("QTY", max);
            }
            prompt.SetTextVariable("NAME", name);
            prompt.SetTextVariable("MAX", max);
            string title = (side == OrderSide.Buy ? guild ? Strings.BuyGuildTitle : Strings.BuyTitle : guild ? Strings.SellGuildTitle : Strings.SellTitle).ToString();
            int initial = side == OrderSide.Sell ? max : 1;

            bool shown = TablePopup.Show("CxQuantityPopup", close => new QuantityVM(title, prompt.ToString(), max, initial,
                Strings.Continue.ToString(), Strings.Cancel.ToString(),
                qty => Estimate(id, side, qty),
                qty => Guard("quantity", () => { close(); ShowConfirmation(id, side, qty, back); }),
                () => Guard("quantity cancel", () => { close(); back(); })));
            if (shown) return;

            InformationManager.ShowTextInquiry(new TextInquiryData(title,
                prompt.ToString(), true, true, Strings.Continue.ToString(), Strings.Cancel.ToString(),
                text => Guard("quantity", () => ShowConfirmation(id, side, int.Parse(text.Trim()), back)),
                () => Guard("quantity cancel", back), false,
                text => ValidateQuantity(text, max),
                "", side == OrderSide.Sell ? max.ToString() : ""));
        }

        /// <summary>One-line cost preview for the quantity popup.</summary>
        private static string Estimate(string id, OrderSide side, int quantity)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return "";
            TradeQuote q = b.Orders.Quote(id, side, quantity, b.CurrentTrader(), b.Portfolio);
            if (!q.IsValid) return Strings.TradeErrorText(q.Error).ToString();
            TextObject t = side == OrderSide.Buy ? Strings.QuantityEstimateBuy : Strings.QuantityEstimateSell;
            t.SetTextVariable("TOTAL", q.Total);
            t.SetTextVariable("FEE", q.Commission);
            t.SetTextVariable("TAX", q.ProfitTax);
            return t.ToString();
        }

        private static Tuple<bool, string> ValidateQuantity(string text, int max)
        {
            bool ok = int.TryParse(text?.Trim(), out int qty) && qty >= 1 && qty <= max;
            return new Tuple<bool, string>(ok, ok ? "" : Strings.QuantityInvalid.SetTextVariable("MAX", max).ToString());
        }

        private static void ShowConfirmation(string id, OrderSide side, int quantity, Action back)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            TradeQuote q = b.Orders.Quote(id, side, quantity, b.CurrentTrader(), b.Portfolio);
            if (!q.IsValid)
            {
                ShowError(q.Error);
                back();
                return;
            }

            string title = side == OrderSide.Buy ? Strings.BuyTitle.ToString() : Strings.SellTitle.ToString();
            string name = Names.Instrument(b.Market.Find(id));
            bool shown = TablePopup.Show("CxConfirmPopup", close => ConfirmTable(q, title, name,
                () => Guard("execute", () => { close(); Execute(id, side, quantity); back(); }),
                () => Guard("confirm cancel", () => { close(); back(); })));
            if (shown) return;

            TextObject text = side == OrderSide.Buy ? Strings.ConfirmBuy : Strings.ConfirmSell;
            text.SetTextVariable("QTY", q.Quantity);
            text.SetTextVariable("NAME", Names.Instrument(b.Market.Find(id)));
            text.SetTextVariable("PRICE", Fmt.Price(q.MarketPrice));
            text.SetTextVariable("EXEC", Fmt.Price(q.ExecutionPrice));
            text.SetTextVariable("GROSS", q.Gross);
            text.SetTextVariable("FEE", q.Commission);
            text.SetTextVariable("TAX", q.ProfitTax);
            text.SetTextVariable("TOTAL", q.Total);

            InformationManager.ShowInquiry(new InquiryData(
                side == OrderSide.Buy ? Strings.BuyTitle.ToString() : Strings.SellTitle.ToString(),
                text.ToString(), true, true, Strings.Confirm.ToString(), Strings.Cancel.ToString(),
                () => Guard("execute", () => { Execute(id, side, quantity); back(); }),
                () => Guard("confirm cancel", back)));
        }

        /// <summary>The cost breakdown as a label | value list; a sell also shows the average cost and the profit or loss.</summary>
        private static ConfirmVM ConfirmTable(TradeQuote q, string title, string name, Action onConfirm, Action onCancel)
        {
            bool buy = q.Side == OrderSide.Buy;
            TextObject heading = buy ? Strings.ConfirmBuyHeading : Strings.ConfirmSellHeading;
            heading.SetTextVariable("QTY", q.Quantity);
            heading.SetTextVariable("NAME", name);

            var vm = new ConfirmVM(title, heading.ToString(), Strings.Confirm.ToString(), Strings.Cancel.ToString(), onConfirm, onCancel);
            vm.Add(Strings.CfMarketPrice.ToString(), Fmt.Price(q.MarketPrice));
            vm.Add(Strings.CfExecPrice.ToString(), Fmt.Price(q.ExecutionPrice));
            if (!buy) vm.Add(Strings.CfAverageCost.ToString(), Fmt.Price(q.Quantity > 0 ? q.CostBasis / q.Quantity : 0));
            vm.Add(Strings.CfGross.ToString(), DenarsText(q.Gross));
            vm.Add(Strings.CfFee.ToString(), DenarsText(q.Commission));
            if (!buy) vm.Add(Strings.CfTax.ToString(), DenarsText(q.ProfitTax), q.ProfitTax > 0 ? TableColors.Down : (Color?)null);
            vm.Add((buy ? Strings.CfTotalPay : Strings.CfTotalReceive).ToString(), DenarsText(q.Total), null, true);
            if (!buy)
            {
                vm.Add(Strings.CfProfit.ToString(), Strings.DenarsAmount.SetTextVariable("AMOUNT", Fmt.SignedDenars(q.RealizedPnl)).ToString(),
                    TableColors.ForAmount(q.RealizedPnl), true);
            }
            return vm;
        }

        private static string DenarsText(long amount) => Strings.DenarsAmount.SetTextVariable("AMOUNT", Fmt.Denars(amount)).ToString();

        private static void Execute(string id, OrderSide side, int quantity)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            TraderProfile trader = b.CurrentTrader();
            TradeResult r = b.Orders.Execute(id, side, quantity, trader, b.Portfolio, (int)CampaignTime.Now.ToDays);
            if (!r.Success)
            {
                ExchangeLog.Info($"Trade rejected: {side} {quantity} {id}: {r.Quote.Error}");
                ShowError(r.Quote.Error);
                return;
            }

            // Core already updated the portfolio; now move the gold. Null counterpart = the house.
            if (r.GoldDelta < 0) GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, -r.GoldDelta, true);
            else if (r.GoldDelta > 0) GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, r.GoldDelta, true);

            TradeQuote q = r.Quote;
            ExchangeLog.Info($"Trade: {side} {quantity} {id} @ {q.ExecutionPrice} (market {q.MarketPrice}), gross {q.Gross}, fee {q.Commission}, tax {q.ProfitTax}, gold {r.GoldDelta:+0;-0}, realized {r.RealizedPnl:0.##}. Gold now {Hero.MainHero.Gold}. commission {b.Orders.CommissionRate:P2}, clan tier {trader.ClanTier}.");

            TextObject msg = side == OrderSide.Buy ? Strings.Bought : Strings.Sold;
            msg.SetTextVariable("QTY", quantity);
            msg.SetTextVariable("NAME", Names.Instrument(b.Market.Find(id)));
            msg.SetTextVariable("TOTAL", q.Total);
            msg.SetTextVariable("PNL", Fmt.SignedDenars(r.RealizedPnl));
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Yellow));
            ExchangeMenus.Refresh();
        }

        private static ImageIdentifier Icon(InstrumentDefinition def)
        {
            ItemObject item = Names.ItemOf(def);
            return item != null ? new ItemImageIdentifier(item) : null;
        }

        private static void ShowError(TradeError error)
        {
            InformationManager.DisplayMessage(new InformationMessage(Strings.TradeErrorText(error).ToString(), Colors.Red));
        }

        private static void Guard(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ExchangeLog.Error($"Trade flow '{what}' failed", ex);
            }
        }
    }
}
