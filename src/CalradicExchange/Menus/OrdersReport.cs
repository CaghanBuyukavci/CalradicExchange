using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalradicExchange.Adapters;
using CalradicExchange.Behaviors;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Trading;
using CalradicExchange.Diagnostics;
using CalradicExchange.Localization;
using CalradicExchange.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradicExchange.Menus
{
    /// <summary>
    /// Take-profit / stop-loss orders (ADR-039): the order window opened from a portfolio row, the My Orders table,
    /// and the messages when an order fills or ends. Order days are campaign days, like the trade history.
    /// </summary>
    public static class OrdersReport
    {
        /// <summary>Slider positions the order window opens with.</summary>
        private const int DefaultTakeProfit = 20, DefaultStopLoss = 10, DefaultDays = 14;

        public static int Today => (int)CampaignTime.Now.ToDays;

        /// <summary>Opens the order window for a held instrument; <paramref name="back"/> reopens the portfolio.</summary>
        public static void StartOrder(string id, Action back)
        {
            Guard("order", () =>
            {
                ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
                if (b == null || !b.IsReady) return;
                InstrumentDefinition def = b.Market.Find(id);
                int free = b.Portfolio.FreeQuantity(id);
                if (def == null || free <= 0) return;
                StandingOrderQuote probe = b.Orders.QuoteStandingOrder(id, free, DefaultTakeProfit, DefaultStopLoss, DefaultDays, b.CurrentTrader(), b.Portfolio);
                if (probe.Error == TradeError.OrdersNotForGuilds || probe.Error == TradeError.InstrumentClosed || probe.Error == TradeError.NoPrice)
                {
                    ShowError(probe.Error);
                    return;
                }

                var s = b.Market.Settings.Orders;
                string name = Names.ShortInstrument(def);
                TextObject heading = Strings.OrderHeading;
                heading.SetTextVariable("NAME", name);
                heading.SetTextVariable("FREE", free);
                heading.SetTextVariable("COST", Fmt.Price(probe.ReferenceCost));
                heading.SetTextVariable("PRICE", Fmt.Price(probe.MarketPrice));
                string[] labels =
                {
                    Strings.OrderQuantityLabel.ToString(), Strings.OrderTakeProfitLabel.ToString(),
                    Strings.OrderStopLossLabel.ToString(), Strings.OrderDaysLabel.ToString(),
                };

                bool shown = TablePopup.Show("CxOrderPopup", close => new StandingOrderVM(
                    Strings.OrderTitle.ToString() + " - " + name, heading.ToString(), labels,
                    free, s.MaxTakeProfitPercent, s.MaxStopLossPercent, s.MaxDays,
                    free, DefaultTakeProfit, DefaultStopLoss, DefaultDays,
                    Strings.BtnPlaceOrder.ToString(), Strings.Cancel.ToString(),
                    (qty, tp, sl, days) => b.Orders.QuoteStandingOrder(id, qty, tp, sl, days, b.CurrentTrader(), b.Portfolio),
                    q => Describe(q, free),
                    (qty, tp, sl, days) => Guard("place order", () => { close(); Place(id, qty, tp, sl, days); back?.Invoke(); }),
                    () => Guard("order cancel", () => { close(); back?.Invoke(); })));
                if (!shown) ShowError(TradeError.None);
            });
        }

        private static StandingOrderTexts Describe(StandingOrderQuote q, int free)
        {
            return new StandingOrderTexts
            {
                Quantity = Strings.OrderQuantityValue.SetTextVariable("QTY", q.Quantity).SetTextVariable("MAX", free).ToString(),
                TakeProfit = Target(q.TakeProfitPrice, q.ReferenceCost),
                StopLoss = Target(q.StopLossPrice, q.ReferenceCost),
                Days = Strings.OrderDaysValue.SetTextVariable("DAYS", q.Days).ToString(),
                Fee = Strings.OrderFee.SetTextVariable("FEE", q.Fee).ToString(),
                Error = q.IsValid ? "" : Strings.TradeErrorText(q.Error).ToString(),
            };
        }

        /// <summary>"%20 = 120.50", or "Off" for a target turned off.</summary>
        private static string Target(decimal? price, decimal cost)
        {
            if (price == null) return Strings.OrderTargetOff.ToString();
            int pct = cost > 0 ? (int)Math.Round(Math.Abs(price.Value / cost - 1) * 100) : 0;
            return Strings.OrderTargetValue.SetTextVariable("PCT", pct).SetTextVariable("PRICE", Fmt.Price(price.Value)).ToString();
        }

        private static void Place(string id, int qty, int takeProfit, int stopLoss, int days)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            StandingOrder o = b.Orders.PlaceStandingOrder(id, qty, takeProfit, stopLoss, days, b.CurrentTrader(), b.Portfolio, Today, out StandingOrderQuote q);
            if (o == null)
            {
                ExchangeLog.Info($"Order rejected: {qty} {id} tp {takeProfit}% sl {stopLoss}% {days}d: {q.Error}");
                ShowError(q.Error);
                return;
            }
            if (o.Fee > 0) GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, o.Fee, true);
            ExchangeLog.Info($"Order placed: #{o.Id} {qty} {id}, take profit {o.TakeProfitPrice?.ToString("0.####") ?? "off"}, stop loss {o.StopLossPrice?.ToString("0.####") ?? "off"} (cost {o.ReferenceCost:0.####}), days {o.PlacedDay}..{o.ExpiryDay}, fee {o.Fee}. Gold now {Hero.MainHero.Gold}.");
            TextObject msg = Strings.OrderPlaced;
            msg.SetTextVariable("QTY", qty);
            msg.SetTextVariable("NAME", Names.ShortInstrument(b.Market.Find(id)));
            msg.SetTextVariable("FEE", o.Fee);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Yellow));
            ExchangeMenus.Refresh();
        }

        /// <summary>My Orders, newest first. <paramref name="back"/> reopens the portfolio.</summary>
        public static void Show(Action back)
        {
            ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
            if (b == null || !b.IsReady) return;
            ButtonSkins.EnsureApplied();
            if (TablePopup.Show("CxOrdersTable", close => Table(b, () => { close(); back?.Invoke(); }, back))) return;

            var sb = new StringBuilder();
            if (b.Portfolio.Orders.Count == 0) sb.AppendLine(Strings.OrdersEmpty.ToString());
            foreach (StandingOrder o in Enumerable.Reverse(b.Portfolio.Orders))
            {
                string[] c = Cells(b, o);
                sb.AppendLine($"{c[0]}  {c[1]} x{c[2]}  {c[3]} / {c[4]}  {c[6]} {c[7]}");
            }
            InformationManager.ShowInquiry(new InquiryData(Strings.OrdersTitle.ToString(), sb.ToString(), true, false, Strings.Close.ToString(), "", back, null));
        }

        private static ExchangeTableVM Table(ExchangeCampaignBehavior b, Action close, Action back)
        {
            var vm = new ExchangeTableVM(Strings.OrdersTitle.ToString(),
                new[]
                {
                    Strings.ColPlaced.ToString(), Strings.ColInstrument.ToString(), Strings.ColQuantity.ToString(), Strings.ColTakeProfit.ToString(),
                    Strings.ColStopLoss.ToString(), Strings.ColDaysLeft.ToString(), Strings.ColStatus.ToString(), Strings.ColResult.ToString(),
                },
                null, Strings.Close.ToString(), close);
            vm.WithSecondary(Strings.BtnPortfolio.ToString(), () => MarketReport.ShowPortfolio(back));
            Fill(b, vm, back);
            return vm;
        }

        private static void Fill(ExchangeCampaignBehavior b, ExchangeTableVM vm, Action back)
        {
            vm.OrderRows.Clear();
            string cancelText = Strings.BtnCancelOrder.ToString();
            bool alternate = false;
            foreach (StandingOrder o in Enumerable.Reverse(b.Portfolio.Orders))
            {
                StandingOrder order = o;
                Color status = o.Status == StandingOrderStatus.Pending ? TableColors.Muted
                    : o.Status == StandingOrderStatus.Filled ? TableColors.Up : TableColors.Down;
                Color result = o.Status == StandingOrderStatus.Filled ? TableColors.ForAmount(o.RealizedPnl) : TableColors.Muted;
                vm.OrderRows.Add(new OrderRowVM(Cells(b, o), status, result, alternate, o.IsPending, cancelText,
                    () => Guard("cancel order", () => { Cancel(b, order); Fill(b, vm, back); })));
                alternate = !alternate;
            }
            vm.EmptyText = vm.OrderRows.Count == 0 ? Strings.OrdersEmpty.ToString() : "";
        }

        /// <summary>placed | name | qty | take profit | stop loss | days left | status | result.</summary>
        private static string[] Cells(ExchangeCampaignBehavior b, StandingOrder o)
        {
            InstrumentDefinition def = b.Market.Find(o.InstrumentId);
            string none = Strings.NoPrice.ToString();
            return new[]
            {
                CampaignTime.Days(o.PlacedDay).ToString(),
                def != null ? Names.ShortInstrument(def) : o.InstrumentId,
                o.Quantity.ToString(),
                o.TakeProfitPrice != null ? $"{Fmt.Price(o.TakeProfitPrice.Value)} ({Percent(o.TakeProfitPercent)})" : none,
                o.StopLossPrice != null ? $"{Fmt.Price(o.StopLossPrice.Value)} ({Percent(-o.StopLossPercent)})" : none,
                o.IsPending ? Strings.OrderDaysLeft.SetTextVariable("DAYS", Math.Max(0, o.ExpiryDay - Today)).ToString() : none,
                StatusText(o),
                ResultText(o),
            };
        }

        /// <summary>Whole signed percent in the language's format, e.g. "+%20" in Turkish.</summary>
        private static string Percent(int percent)
        {
            return Strings.PercentFormat.SetTextVariable("SIGN", percent > 0 ? "+" : percent < 0 ? "-" : "")
                .SetTextVariable("ABS", Math.Abs(percent)).ToString();
        }

        private static string StatusText(StandingOrder o)
        {
            switch (o.Status)
            {
                case StandingOrderStatus.Filled: return Strings.OrderFilled.ToString();
                case StandingOrderStatus.Canceled: return Strings.OrderCanceled.ToString();
                default: return Strings.OrderPending.ToString();
            }
        }

        private static string ResultText(StandingOrder o)
        {
            switch (o.Status)
            {
                case StandingOrderStatus.Filled:
                    return Strings.OrderResultFilled.SetTextVariable("KIND", TriggerName(o.Trigger)).SetTextVariable("PNL", Fmt.SignedDenars(o.RealizedPnl)).ToString();
                case StandingOrderStatus.Canceled:
                    switch (o.CancelReason)
                    {
                        case StandingOrderCancelReason.Expired: return Strings.OrderResultExpired.ToString();
                        case StandingOrderCancelReason.Player: return Strings.OrderResultPlayer.ToString();
                        default: return Strings.OrderResultClosed.ToString();
                    }
                default: return "";
            }
        }

        public static string TriggerName(StandingOrderTrigger trigger)
        {
            return (trigger == StandingOrderTrigger.StopLoss ? Strings.SideStopLoss : Strings.SideTakeProfit).ToString();
        }

        private static void Cancel(ExchangeCampaignBehavior b, StandingOrder o)
        {
            if (!o.IsPending) return;
            b.Portfolio.CancelOrder(o, StandingOrderCancelReason.Player, Today);
            ExchangeLog.Info($"Order canceled by the player: #{o.Id} {o.Quantity} {o.InstrumentId}.");
            Announce(b, o);
            ExchangeMenus.Refresh();
        }

        /// <summary>
        /// After the daily check: pays the filled orders and writes one message per order that ended (bottom-left log).
        /// </summary>
        public static void Settle(ExchangeCampaignBehavior b, List<StandingOrder> ended)
        {
            foreach (StandingOrder o in ended)
            {
                if (o.Status == StandingOrderStatus.Filled && o.Proceeds > 0) GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, o.Proceeds, true);
                ExchangeLog.Info($"Order #{o.Id} {o.Status} ({(o.Status == StandingOrderStatus.Filled ? o.Trigger.ToString() : o.CancelReason.ToString())}): {o.Quantity} {o.InstrumentId}" +
                    (o.Status == StandingOrderStatus.Filled ? $" @ {o.FillPrice:0.####}, gold +{o.Proceeds}, realized {o.RealizedPnl:0.##}" : "") + ".");
                Announce(b, o);
            }
        }

        private static void Announce(ExchangeCampaignBehavior b, StandingOrder o)
        {
            TextObject msg;
            Color color = Colors.Yellow;
            if (o.Status == StandingOrderStatus.Filled)
            {
                msg = Strings.OrderFilledMessage;
                msg.SetTextVariable("KIND", TriggerName(o.Trigger));
                msg.SetTextVariable("TOTAL", o.Proceeds);
                msg.SetTextVariable("PNL", Fmt.SignedDenars(o.RealizedPnl));
                color = o.RealizedPnl >= 0 ? Colors.Green : Colors.Red;
            }
            else if (o.CancelReason == StandingOrderCancelReason.Expired) msg = Strings.OrderExpiredMessage;
            else if (o.CancelReason == StandingOrderCancelReason.Player) msg = Strings.OrderCanceledMessage;
            else msg = Strings.OrderClosedMessage;
            msg.SetTextVariable("QTY", o.Quantity);
            msg.SetTextVariable("NAME", Names.ShortInstrument(b.Market.Find(o.InstrumentId)));
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), color));
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
                ExchangeLog.Error($"Order flow '{what}' failed", ex);
            }
        }
    }
}
