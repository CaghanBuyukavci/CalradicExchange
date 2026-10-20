using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;

namespace CalradicExchange.Core.Trading
{
    /// <summary>
    /// Prices and executes market orders against the house (ECONOMY_MODEL 5, 6; ADR-007).
    /// Long positions only in Phase 1; short selling arrives in Phase 3.
    /// </summary>
    public sealed class OrderEngine
    {
        private readonly MarketEngine _market;

        public OrderEngine(MarketEngine market)
        {
            _market = market ?? throw new ArgumentNullException(nameof(market));
        }

        private MarketSettings Settings => _market.Settings;

        private int GuildShares => Settings.Guilds?.SharesPerGuild ?? 100;

        /// <summary>Total investment limit: base + per clan tier + per trade skill point.</summary>
        public int TotalLimit(TraderProfile trader)
        {
            LimitSettings l = Settings.Limits;
            return l.BaseTotal + l.PerClanTier * Math.Max(0, trader.ClanTier) + l.PerTradeSkill * Math.Max(0, trader.TradeSkill);
        }

        /// <summary>Per-instrument limit: class depth x multiple.</summary>
        public int InstrumentLimit(InstrumentClass cls)
        {
            return (int)(Settings.For(cls).Depth * Settings.Limits.PerInstrumentDepthMultiple);
        }

        /// <summary>Flat commission rate (ADR-033). A broker level of our own may lower it later; the Trade skill does not.</summary>
        public double CommissionRate => Settings.Trading.CommissionRate;

        public int Commission(int gross)
        {
            double fee = Math.Max(Settings.Trading.MinCommission, CommissionRate * gross);
            return (int)Math.Ceiling(fee - 1e-9);
        }

        /// <summary>Tax on the profit of a sell: rate x max(0, proceeds after commission - cost basis).</summary>
        public int ProfitTax(int proceedsAfterCommission, decimal removedBasis)
        {
            decimal profit = proceedsAfterCommission - removedBasis;
            if (profit <= 0) return 0;
            return (int)Math.Ceiling((double)profit * Settings.Trading.ProfitTaxRate - 1e-9);
        }

        public TradeQuote Quote(string instrumentId, OrderSide side, int quantity, TraderProfile trader, Portfolio portfolio)
        {
            return Quote(instrumentId, side, quantity, trader, portfolio, false);
        }

        /// <summary>
        /// <paramref name="standingOrder"/>: the sale of a take-profit / stop-loss order (ADR-039). No commission, and
        /// the order's own locked shares may be sold.
        /// </summary>
        private TradeQuote Quote(string instrumentId, OrderSide side, int quantity, TraderProfile trader, Portfolio portfolio, bool standingOrder)
        {
            var q = new TradeQuote { InstrumentId = instrumentId, Side = side, Quantity = quantity };
            InstrumentDefinition def = _market.Find(instrumentId);
            PriceState ps = _market.Get(instrumentId);

            if (quantity <= 0) return Fail(q, TradeError.InvalidQuantity);
            if (def == null || ps == null) return Fail(q, TradeError.UnknownInstrument);
            if (ps.Closed) return Fail(q, TradeError.InstrumentClosed);
            if (!ps.HasPrice || ps.Price <= 0) return Fail(q, TradeError.NoPrice);

            ClassParameters cp = Settings.For(def.Class);
            double p = (double)ps.Price;
            double orderValue = quantity * p;
            // Limited supply (ADR-037): the thinner the free float, the larger the slippage.
            int held = portfolio.QuantityOf(instrumentId);
            double impact = cp.Lambda * orderValue / cp.Depth * _market.ImpactMultiplier(instrumentId, side == OrderSide.Buy ? quantity : -quantity, held);
            double halfSpread = cp.Spread / 2.0;

            double exec;
            int gross;
            if (side == OrderSide.Buy)
            {
                exec = p * (1 + halfSpread) * (1 + impact);
                gross = SafeInt(Math.Ceiling(quantity * exec));
            }
            else
            {
                exec = Math.Max(0, p * (1 - halfSpread) * (1 - impact));
                gross = SafeInt(Math.Floor(quantity * exec));
            }

            q.MarketPrice = ps.Price;
            q.ExecutionPrice = PriceMath.Round(exec);
            q.Impact = impact;
            q.Gross = gross;
            q.Commission = standingOrder ? 0 : Commission(gross);
            if (side == OrderSide.Buy)
            {
                q.Total = gross + q.Commission;
            }
            else
            {
                int afterCommission = Math.Max(0, gross - q.Commission);
                q.CostBasis = portfolio.BasisFor(instrumentId, quantity);
                q.ProfitTax = ProfitTax(afterCommission, q.CostBasis);
                q.Total = afterCommission - q.ProfitTax;
                q.RealizedPnl = q.Total - q.CostBasis;
            }

            if (side == OrderSide.Buy)
            {
                if (def.Class == InstrumentClass.Guild && trader.OwnGuilds.Contains(instrumentId)) return Fail(q, TradeError.OwnWorkshop);
                if (q.Total > trader.Gold) return Fail(q, TradeError.InsufficientGold);
                decimal invested = portfolio.MarketValue(PriceOf);
                if ((double)invested + orderValue > TotalLimit(trader)) return Fail(q, TradeError.TotalLimitExceeded);
                double heldHere = portfolio.QuantityOf(instrumentId) * p;
                if (heldHere + orderValue > InstrumentLimit(def.Class)) return Fail(q, TradeError.InstrumentLimitExceeded);
                if (def.Class == InstrumentClass.Guild)
                {
                    if (def.TownId != trader.TownId) return Fail(q, TradeError.NotInGuildTown);
                    if (portfolio.QuantityOf(instrumentId) + quantity > GuildShares) return Fail(q, TradeError.GuildSharesExceeded);
                }
            }
            else if (quantity > (standingOrder ? portfolio.QuantityOf(instrumentId) : portfolio.FreeQuantity(instrumentId)))
            {
                return Fail(q, TradeError.InsufficientShares);
            }

            return q;
        }

        /// <summary>
        /// Quotes and, if valid, executes: updates the portfolio, records the trade under <paramref name="day"/> and
        /// moves the market price. Gold is applied by the caller.
        /// </summary>
        public TradeResult Execute(string instrumentId, OrderSide side, int quantity, TraderProfile trader, Portfolio portfolio, int day = 0)
        {
            TradeQuote q = Quote(instrumentId, side, quantity, trader, portfolio);
            var result = new TradeResult { Quote = q };
            if (!q.IsValid) return result;

            if (side == OrderSide.Buy)
            {
                portfolio.ApplyBuy(instrumentId, quantity, q.Total, q.Commission);
                result.GoldDelta = -q.Total;
            }
            else
            {
                result.RealizedPnl = portfolio.ApplySell(instrumentId, quantity, q.Total, q.Commission, q.ProfitTax);
                result.GoldDelta = q.Total;
            }
            portfolio.Record(new TradeRecord
            {
                Day = day,
                InstrumentId = instrumentId,
                Side = side,
                Quantity = quantity,
                ExecutionPrice = q.ExecutionPrice,
                Total = q.Total,
                Commission = q.Commission,
                ProfitTax = q.ProfitTax,
                RealizedPnl = result.RealizedPnl,
            }, _market.Settings.Trading.TradeHistoryLimit);
            _market.ApplyTradeImpact(instrumentId, side == OrderSide.Buy, q.Impact);
            return result;
        }

        /// <summary>Fee of a take-profit / stop-loss order: max(minFee, value x (feeRate + feePerDay x days)).</summary>
        public int StandingOrderFee(decimal value, int days)
        {
            StandingOrderSettings o = OrderSettings;
            double fee = Math.Max(o.MinFee, (double)value * (o.FeeRate + o.FeePerDay * Math.Max(0, days)));
            return (int)Math.Ceiling(fee - 1e-9);
        }

        private StandingOrderSettings OrderSettings => Settings.Orders ?? new StandingOrderSettings();

        /// <summary>
        /// What a take-profit / stop-loss order on <paramref name="quantity"/> held shares would cost (ADR-039).
        /// Targets are percents of the average cost; 0 turns one off.
        /// </summary>
        public StandingOrderQuote QuoteStandingOrder(string instrumentId, int quantity, int takeProfitPercent, int stopLossPercent, int days,
            TraderProfile trader, Portfolio portfolio)
        {
            StandingOrderSettings o = OrderSettings;
            var q = new StandingOrderQuote { InstrumentId = instrumentId, Quantity = quantity, Days = days };
            InstrumentDefinition def = _market.Find(instrumentId);
            PriceState ps = _market.Get(instrumentId);
            Position pos = portfolio.Find(instrumentId);

            if (def == null || ps == null) return Fail(q, TradeError.UnknownInstrument);
            if (def.Class == InstrumentClass.Guild) return Fail(q, TradeError.OrdersNotForGuilds);
            if (ps.Closed) return Fail(q, TradeError.InstrumentClosed);
            if (!ps.HasPrice || ps.Price <= 0) return Fail(q, TradeError.NoPrice);

            q.MarketPrice = ps.Price;
            q.ReferenceCost = pos != null ? Math.Round(pos.AverageCost, 4) : 0;
            if (takeProfitPercent > 0) q.TakeProfitPrice = Math.Round(q.ReferenceCost * (1 + takeProfitPercent / 100m), 4);
            if (stopLossPercent > 0) q.StopLossPrice = Math.Round(q.ReferenceCost * (1 - stopLossPercent / 100m), 4);
            q.Fee = StandingOrderFee(Math.Max(0, quantity) * ps.Price, days);

            if (quantity <= 0) return Fail(q, TradeError.InvalidQuantity);
            if (quantity > portfolio.FreeQuantity(instrumentId)) return Fail(q, TradeError.InsufficientShares);
            if (days < 1 || days > o.MaxDays || takeProfitPercent < 0 || takeProfitPercent > o.MaxTakeProfitPercent
                || stopLossPercent < 0 || stopLossPercent > o.MaxStopLossPercent) return Fail(q, TradeError.InvalidOrder);
            if (takeProfitPercent == 0 && stopLossPercent == 0) return Fail(q, TradeError.NoOrderTarget);
            if (q.Fee > trader.Gold) return Fail(q, TradeError.InsufficientGold);
            return q;
        }

        /// <summary>
        /// Places the order if the quote is valid: locks the shares and books the fee as a cost. The caller takes the
        /// fee in gold. Returns the order, or null with the reason in <paramref name="quote"/>.
        /// </summary>
        public StandingOrder PlaceStandingOrder(string instrumentId, int quantity, int takeProfitPercent, int stopLossPercent, int days,
            TraderProfile trader, Portfolio portfolio, int day, out StandingOrderQuote quote)
        {
            quote = QuoteStandingOrder(instrumentId, quantity, takeProfitPercent, stopLossPercent, days, trader, portfolio);
            if (!quote.IsValid) return null;
            var order = new StandingOrder
            {
                Id = portfolio.NextOrderId++,
                InstrumentId = instrumentId,
                Quantity = quantity,
                ReferenceCost = quote.ReferenceCost,
                TakeProfitPercent = takeProfitPercent,
                StopLossPercent = stopLossPercent,
                TakeProfitPrice = quote.TakeProfitPrice,
                StopLossPrice = quote.StopLossPrice,
                PlacedDay = day,
                ExpiryDay = day + days,
                Fee = quote.Fee,
            };
            portfolio.Orders.Add(order);
            portfolio.TotalCommissionPaid += quote.Fee;
            portfolio.RealizedPnl -= quote.Fee;
            portfolio.TrimOrders(OrderSettings.OrdersKept);
            return order;
        }

        /// <summary>
        /// Checks the pending orders after the day's prices (ADR-039). A target reached sells at the market without
        /// commission (profit tax only); an order past its last day expires; one on a closed instrument is canceled.
        /// Returns the orders that ended today; the gold of the filled ones is their Proceeds, paid by the caller.
        /// </summary>
        public List<StandingOrder> ProcessStandingOrders(Portfolio portfolio, int day)
        {
            var ended = new List<StandingOrder>();
            foreach (StandingOrder o in portfolio.Orders.Where(x => x.IsPending).ToList())
            {
                PriceState ps = _market.Get(o.InstrumentId);
                if (ps == null || ps.Closed || !ps.HasPrice || portfolio.QuantityOf(o.InstrumentId) < o.Quantity)
                {
                    portfolio.CancelOrder(o, StandingOrderCancelReason.Closed, day);
                    ended.Add(o);
                    continue;
                }

                StandingOrderTrigger trigger = o.TakeProfitPrice != null && ps.Price >= o.TakeProfitPrice ? StandingOrderTrigger.TakeProfit
                    : o.StopLossPrice != null && ps.Price <= o.StopLossPrice ? StandingOrderTrigger.StopLoss
                    : StandingOrderTrigger.None;
                if (trigger != StandingOrderTrigger.None)
                {
                    Fill(portfolio, o, trigger, day);
                    ended.Add(o);
                    continue;
                }
                if (day >= o.ExpiryDay)
                {
                    portfolio.CancelOrder(o, StandingOrderCancelReason.Expired, day);
                    ended.Add(o);
                }
            }
            if (ended.Count > 0) portfolio.TrimOrders(OrderSettings.OrdersKept);
            return ended;
        }

        /// <summary>Sells the order's shares at the market, without commission; canceled if the sale is not possible.</summary>
        private void Fill(Portfolio portfolio, StandingOrder o, StandingOrderTrigger trigger, int day)
        {
            TradeQuote q = Quote(o.InstrumentId, OrderSide.Sell, o.Quantity, null, portfolio, true);
            if (!q.IsValid)
            {
                portfolio.CancelOrder(o, StandingOrderCancelReason.Closed, day);
                return;
            }
            o.Status = StandingOrderStatus.Filled;
            o.Trigger = trigger;
            o.ClosedDay = day;
            o.FillPrice = q.ExecutionPrice;
            o.Proceeds = q.Total;
            o.RealizedPnl = portfolio.ApplySell(o.InstrumentId, o.Quantity, q.Total, 0, q.ProfitTax);
            portfolio.Record(new TradeRecord
            {
                Day = day,
                InstrumentId = o.InstrumentId,
                Side = OrderSide.Sell,
                Quantity = o.Quantity,
                ExecutionPrice = q.ExecutionPrice,
                Total = q.Total,
                ProfitTax = q.ProfitTax,
                RealizedPnl = o.RealizedPnl,
                Order = trigger,
            }, Settings.Trading.TradeHistoryLimit);
            _market.ApplyTradeImpact(o.InstrumentId, false, q.Impact);
        }

        /// <summary>Largest quantity that passes every check for a buy (0 if none). Binary search over quotes.</summary>
        public int MaxBuyQuantity(string instrumentId, TraderProfile trader, Portfolio portfolio)
        {
            PriceState ps = _market.Get(instrumentId);
            if (ps == null || !ps.HasPrice || ps.Price <= 0) return 0;
            int hi = (int)Math.Min(int.MaxValue / 2, Math.Max(1, trader.Gold / (double)ps.Price) + 1);
            int lo = 0;
            while (lo < hi)
            {
                int mid = lo + (hi - lo + 1) / 2;
                if (Quote(instrumentId, OrderSide.Buy, mid, trader, portfolio).IsValid) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        private decimal PriceOf(string id)
        {
            PriceState ps = _market.Get(id);
            return ps != null && ps.HasPrice ? ps.Price : 0;
        }

        private static TradeQuote Fail(TradeQuote q, TradeError error)
        {
            q.Error = error;
            return q;
        }

        private static StandingOrderQuote Fail(StandingOrderQuote q, TradeError error)
        {
            q.Error = error;
            return q;
        }

        private static int SafeInt(double v)
        {
            return v >= int.MaxValue ? int.MaxValue : (int)v;
        }
    }
}
