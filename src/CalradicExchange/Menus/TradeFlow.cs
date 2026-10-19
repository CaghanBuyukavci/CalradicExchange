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
        /// <summary>Commodity market (InstrumentClass.Commodity) or basket market (InstrumentClass.Sector).</summary>
        public static void ShowMarket(InstrumentClass cls)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            ButtonSkins.EnsureApplied();
            if (TablePopup.Show("CxTradeTable", close => MarketTable(b, cls, close))) return;
            ShowMarketFallback(b, cls);
        }

        private static ExchangeTableVM MarketTable(ExchangeCampaignBehavior b, InstrumentClass cls, Action close)
        {
            TraderProfile trader = b.CurrentTrader();
            decimal held = b.Portfolio.MarketValue(id => MarketReport.PriceOf(b, id));
            TextObject desc = Strings.TradeDescription;
            desc.SetTextVariable("GOLD", trader.Gold);
            desc.SetTextVariable("LEFT", Fmt.Denars(Math.Max(0, b.Orders.TotalLimit(trader) - held)));
            string description = desc.ToString();
            if (cls == InstrumentClass.Commodity)
            {
                description = Strings.HeaderCommodities.SetTextVariable("UNITS", b.Catalog.UnitsPerShare) + "\n" + description;
            }

            var vm = new ExchangeTableVM(MarketTitle(cls),
                new[]
                {
                    Strings.ColInstrument.ToString(), Strings.ColPrice.ToString(), Strings.ColToday.ToString(),
                    Strings.ColWeek.ToString(), Strings.ColHeld.ToString(),
                },
                null, Strings.Close.ToString(), close, description);

            string buyText = Strings.BtnBuy.ToString(), sellText = Strings.BtnSell.ToString(), none = Strings.NoPrice.ToString();
            Action back = () => ShowMarket(cls);
            bool alternate = false;
            foreach (InstrumentDefinition def in b.Catalog.Instruments.Where(i => i.Class == cls))
            {
                PriceState ps = b.Market.Get(def.Id);
                bool priced = ps != null && ps.HasPrice;
                bool open = priced && !ps.Closed;
                int qty = b.Portfolio.QuantityOf(def.Id);
                double day = priced ? ps.ChangeOver(1) : 0, week = priced ? ps.ChangeOver(7) : 0;
                string id = def.Id;
                vm.TradeRows.Add(new TradeRowVM(Names.Instrument(def),
                    priced ? Fmt.Price(ps.Price) : none,
                    priced ? Fmt.Pct(day) : none, TableColors.ForChange(day),
                    priced ? Fmt.Pct(week) : none, TableColors.ForChange(week),
                    qty > 0 ? qty.ToString() : none,
                    open, open && qty > 0, alternate, buyText, sellText,
                    () => Guard("buy pick", () => AskQuantity(id, OrderSide.Buy, back)),
                    () => Guard("sell pick", () => AskQuantity(id, OrderSide.Sell, back))));
                alternate = !alternate;
            }
            return vm;
        }

        private static string MarketTitle(InstrumentClass cls)
        {
            return (cls == InstrumentClass.Commodity ? Strings.CommodityMarketTitle : Strings.BasketMarketTitle).ToString();
        }

        /// <summary>Vanilla fallback when the Gauntlet table cannot be shown (ADR-006): pick an instrument, then buy or sell.</summary>
        private static void ShowMarketFallback(ExchangeCampaignBehavior b, InstrumentClass cls)
        {
            var elements = new List<InquiryElement>();
            foreach (InstrumentDefinition def in b.Catalog.Instruments.Where(i => i.Class == cls))
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
            if (b.Portfolio.QuantityOf(id) <= 0)
            {
                AskQuantity(id, OrderSide.Buy, back);
                return;
            }
            InformationManager.ShowInquiry(new InquiryData(Names.Instrument(b.Catalog.Find(id)), "", true, true,
                Strings.BtnBuy.ToString(), Strings.BtnSell.ToString(),
                () => Guard("fallback buy", () => AskQuantity(id, OrderSide.Buy, back)),
                () => Guard("fallback sell", () => AskQuantity(id, OrderSide.Sell, back))));
        }

        /// <summary>Quantity step. <paramref name="back"/> runs after the trade or when the player backs out.</summary>
        private static void AskQuantity(string id, OrderSide side, Action back)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            InstrumentDefinition def = b.Catalog.Find(id);
            TraderProfile trader = b.CurrentTrader();
            string name = Names.Instrument(def);

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
                prompt = Strings.QuantityBuy;
                prompt.SetTextVariable("PRICE", Fmt.Price(MarketReport.PriceOf(b, id)));
            }
            else
            {
                max = b.Portfolio.QuantityOf(id);
                if (max <= 0) return;
                prompt = Strings.QuantitySell;
                prompt.SetTextVariable("QTY", max);
            }
            prompt.SetTextVariable("NAME", name);
            prompt.SetTextVariable("MAX", max);
            string title = side == OrderSide.Buy ? Strings.BuyTitle.ToString() : Strings.SellTitle.ToString();
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

            TextObject text = side == OrderSide.Buy ? Strings.ConfirmBuy : Strings.ConfirmSell;
            text.SetTextVariable("QTY", q.Quantity);
            text.SetTextVariable("NAME", Names.Instrument(b.Catalog.Find(id)));
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
            ExchangeLog.Info($"Trade: {side} {quantity} {id} @ {q.ExecutionPrice} (market {q.MarketPrice}), gross {q.Gross}, fee {q.Commission}, tax {q.ProfitTax}, gold {r.GoldDelta:+0;-0}, realized {r.RealizedPnl:0.##}. Gold now {Hero.MainHero.Gold}. Trade skill {trader.TradeSkill} -> commission {b.Orders.CommissionRate(trader.TradeSkill):P2}, clan tier {trader.ClanTier}.");

            TextObject msg = side == OrderSide.Buy ? Strings.Bought : Strings.Sold;
            msg.SetTextVariable("QTY", quantity);
            msg.SetTextVariable("NAME", Names.Instrument(b.Catalog.Find(id)));
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
