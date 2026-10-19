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
    /// Buy and sell flow (PLAN 1.19, 1.20): pick an instrument (multi-selection inquiry) -> quantity (slider popup,
    /// text inquiry as fallback) -> confirmation with the full cost breakdown -> execute in Core and move the hero's gold.
    /// </summary>
    public static class TradeFlow
    {
        public static void StartBuy()
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            TraderProfile trader = b.CurrentTrader();

            var elements = new List<InquiryElement>();
            foreach (InstrumentDefinition def in b.Catalog.Instruments)
            {
                PriceState ps = b.Market.Get(def.Id);
                if (ps == null || !ps.HasPrice || ps.Closed) continue;
                TextObject title = Strings.BuyElement;
                title.SetTextVariable("NAME", Names.Instrument(def));
                title.SetTextVariable("PRICE", Fmt.Price(ps.Price));
                elements.Add(new InquiryElement(def.Id, title.ToString(), Icon(def), true, Fmt.Pct(ps.ChangeOver(7))));
            }

            decimal held = b.Portfolio.MarketValue(id => MarketReport.PriceOf(b, id));
            TextObject desc = Strings.BuyDescription;
            desc.SetTextVariable("GOLD", trader.Gold);
            desc.SetTextVariable("LEFT", Fmt.Denars(Math.Max(0, b.Orders.TotalLimit(trader) - held)));

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                Strings.BuyTitle.ToString(), desc.ToString(), elements, true, 1, 1,
                Strings.Continue.ToString(), Strings.Cancel.ToString(),
                chosen => Guard("buy pick", () => AskQuantity((string)chosen.First().Identifier, OrderSide.Buy)),
                null, "", true));
        }

        public static void StartSell()
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;

            var elements = new List<InquiryElement>();
            foreach (Position pos in b.Portfolio.Positions)
            {
                InstrumentDefinition def = b.Catalog.Find(pos.InstrumentId);
                PriceState ps = b.Market.Get(pos.InstrumentId);
                if (def == null || ps == null) continue;
                TextObject title = Strings.SellElement;
                title.SetTextVariable("NAME", Names.Instrument(def));
                title.SetTextVariable("QTY", pos.Quantity);
                title.SetTextVariable("PRICE", Fmt.Price(ps.Price));
                string hint = Fmt.SignedDenars(pos.Quantity * ps.Price - pos.CostBasis);
                elements.Add(new InquiryElement(def.Id, title.ToString(), Icon(def), !ps.Closed, hint));
            }
            if (elements.Count == 0) return;

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                Strings.SellTitle.ToString(), Strings.SellDescription.ToString(), elements, true, 1, 1,
                Strings.Continue.ToString(), Strings.Cancel.ToString(),
                chosen => Guard("sell pick", () => AskQuantity((string)chosen.First().Identifier, OrderSide.Sell)),
                null));
        }

        private static void AskQuantity(string id, OrderSide side)
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
                    // Explain the first blocking reason using a one-share quote.
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
                qty => Guard("quantity", () => { close(); ShowConfirmation(id, side, qty); }),
                close));
            if (shown) return;

            InformationManager.ShowTextInquiry(new TextInquiryData(
                side == OrderSide.Buy ? Strings.BuyTitle.ToString() : Strings.SellTitle.ToString(),
                prompt.ToString(), true, true, Strings.Continue.ToString(), Strings.Cancel.ToString(),
                text => Guard("quantity", () => ShowConfirmation(id, side, int.Parse(text.Trim()))),
                null, false,
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

        private static void ShowConfirmation(string id, OrderSide side, int quantity)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            TradeQuote q = b.Orders.Quote(id, side, quantity, b.CurrentTrader(), b.Portfolio);
            if (!q.IsValid)
            {
                ShowError(q.Error);
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
                () => Guard("execute", () => Execute(id, side, quantity)), null));
        }

        private static void Execute(string id, OrderSide side, int quantity)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            TraderProfile trader = b.CurrentTrader();
            TradeResult r = b.Orders.Execute(id, side, quantity, trader, b.Portfolio);
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
