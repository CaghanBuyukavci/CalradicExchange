using System;
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
            double impact = cp.Lambda * orderValue / cp.Depth;
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
            q.Commission = Commission(gross);
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
                decimal held = portfolio.MarketValue(PriceOf);
                if ((double)held + orderValue > TotalLimit(trader)) return Fail(q, TradeError.TotalLimitExceeded);
                double heldHere = portfolio.QuantityOf(instrumentId) * p;
                if (heldHere + orderValue > InstrumentLimit(def.Class)) return Fail(q, TradeError.InstrumentLimitExceeded);
                if (def.Class == InstrumentClass.Guild)
                {
                    if (def.TownId != trader.TownId) return Fail(q, TradeError.NotInGuildTown);
                    if (portfolio.QuantityOf(instrumentId) + quantity > GuildShares) return Fail(q, TradeError.GuildSharesExceeded);
                }
            }
            else if (quantity > portfolio.QuantityOf(instrumentId))
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

        private static int SafeInt(double v)
        {
            return v >= int.MaxValue ? int.MaxValue : (int)v;
        }
    }
}
