using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalradicExchange.Core.Trading
{
    /// <summary>The player's holdings and running profit figures (PLAN 1.8). Average-cost accounting.</summary>
    public sealed class Portfolio
    {
        public List<Position> Positions { get; set; } = new List<Position>();
        public decimal RealizedPnl { get; set; }
        public long TotalCommissionPaid { get; set; }
        public long TotalProfitTaxPaid { get; set; }
        public int TradeCount { get; set; }

        /// <summary>
        /// Guild profit (or loss, negative) earned since the last payout, per guild instrument (ADR-027). Accrued daily
        /// by share-days held, settled every week. Added in schema 9.
        /// </summary>
        public Dictionary<string, decimal> GuildAccruals { get; set; } = new Dictionary<string, decimal>();

        /// <summary>Unpaid guild losses per guild instrument, repaid from later guild profits (ADR-027). Schema 9.</summary>
        public Dictionary<string, decimal> GuildDebts { get; set; } = new Dictionary<string, decimal>();

        /// <summary>Net guild profit paid out so far (negative when losses dominated). Schema 9.</summary>
        public decimal GuildIncomeTotal { get; set; }

        [JsonIgnore] public decimal TotalGuildDebt => GuildDebts.Values.Sum();

        /// <summary>Executed trades, oldest first, trimmed to the configured limit. Added in schema 6.</summary>
        public List<TradeRecord> History { get; set; } = new List<TradeRecord>();

        /// <summary>Daily total profit for the performance chart, oldest first, one point per day. Added in schema 11.</summary>
        public List<PerformancePoint> Performance { get; set; } = new List<PerformancePoint>();

        /// <summary>Total profit at the given prices: open + realized + guild payouts + guild profit accrued but not paid.</summary>
        public decimal TotalPnl(Func<string, decimal> priceOf)
        {
            return UnrealizedPnl(priceOf) + RealizedPnl + GuildIncomeTotal + GuildAccruals.Values.Sum();
        }

        /// <summary>The performance point for <paramref name="day"/> at the given prices (not stored).</summary>
        public PerformancePoint PerformanceAt(int day, Func<string, decimal> priceOf)
        {
            return new PerformancePoint { Day = day, Pnl = Math.Round(TotalPnl(priceOf), 2), Value = Math.Round(MarketValue(priceOf), 2) };
        }

        /// <summary>Stores today's point (replacing an earlier one of the same day) and keeps the newest <paramref name="keep"/> days.</summary>
        public void RecordPerformance(int day, Func<string, decimal> priceOf, int keep)
        {
            PerformancePoint point = PerformanceAt(day, priceOf);
            Performance.RemoveAll(p => p.Day >= day);
            Performance.Add(point);
            int excess = Performance.Count - Math.Max(1, keep);
            if (excess > 0) Performance.RemoveRange(0, excess);
        }

        /// <summary>Appends a trade and drops the oldest ones beyond <paramref name="limit"/> (0 keeps none).</summary>
        public void Record(TradeRecord record, int limit)
        {
            History.Add(record);
            int excess = History.Count - System.Math.Max(0, limit);
            if (excess > 0) History.RemoveRange(0, excess);
        }

        public Position Find(string instrumentId)
        {
            return Positions.FirstOrDefault(p => p.InstrumentId == instrumentId);
        }

        public int QuantityOf(string instrumentId)
        {
            return Find(instrumentId)?.Quantity ?? 0;
        }

        public void ApplyBuy(string instrumentId, int quantity, int totalPaid, int commission)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            Position pos = Find(instrumentId);
            if (pos == null)
            {
                pos = new Position { InstrumentId = instrumentId };
                Positions.Add(pos);
            }
            pos.Quantity += quantity;
            pos.CostBasis += totalPaid;
            TotalCommissionPaid += commission;
            TradeCount++;
        }

        /// <summary>Cost basis that selling <paramref name="quantity"/> shares would remove (average cost).</summary>
        public decimal BasisFor(string instrumentId, int quantity)
        {
            Position pos = Find(instrumentId);
            if (pos == null || quantity <= 0) return 0;
            if (quantity >= pos.Quantity) return pos.CostBasis;
            return Math.Round(pos.CostBasis * quantity / pos.Quantity, 4);
        }

        /// <summary>Removes shares at average cost and returns the realized profit.</summary>
        public decimal ApplySell(string instrumentId, int quantity, int netProceeds, int commission, int profitTax = 0)
        {
            Position pos = Find(instrumentId);
            if (pos == null || quantity <= 0 || quantity > pos.Quantity) throw new InvalidOperationException($"Cannot sell {quantity} of {instrumentId}.");

            decimal removedBasis = BasisFor(instrumentId, quantity);
            pos.Quantity -= quantity;
            pos.CostBasis -= removedBasis;
            if (pos.Quantity == 0) Positions.Remove(pos);

            decimal realized = netProceeds - removedBasis;
            RealizedPnl += realized;
            TotalCommissionPaid += commission;
            TotalProfitTaxPaid += profitTax;
            TradeCount++;
            return realized;
        }

        /// <summary>
        /// Sells <paramref name="quantity"/> shares at <paramref name="price"/> without commission or tax, outside the
        /// market (guild closure, forced sale for a guild loss). Records the trade as forced. Returns the proceeds.
        /// </summary>
        public int ForceSell(string instrumentId, int quantity, decimal price, int day, int historyLimit)
        {
            Position pos = Find(instrumentId);
            if (pos == null || quantity <= 0) return 0;
            quantity = Math.Min(quantity, pos.Quantity);
            int proceeds = (int)Math.Round(quantity * price, MidpointRounding.AwayFromZero);
            decimal realized = ApplySell(instrumentId, quantity, proceeds, 0);
            Record(new TradeRecord
            {
                Day = day,
                InstrumentId = instrumentId,
                Side = OrderSide.Sell,
                Quantity = quantity,
                ExecutionPrice = price,
                Total = proceeds,
                RealizedPnl = realized,
                Forced = true,
            }, historyLimit);
            return proceeds;
        }

        /// <summary>
        /// Closes every position at the given prices without fees or tax (used when the market is rebuilt).
        /// Returns the gold to pay out.
        /// </summary>
        public int CloseAllAt(Func<string, decimal> priceOf)
        {
            int total = 0;
            foreach (Position pos in Positions.ToList())
            {
                int proceeds = (int)Math.Round(pos.Quantity * priceOf(pos.InstrumentId), MidpointRounding.AwayFromZero);
                RealizedPnl += proceeds - pos.CostBasis;
                total += proceeds;
            }
            Positions.Clear();
            return total;
        }

        /// <summary>Mark-to-market value at the given prices (missing prices count as 0).</summary>
        public decimal MarketValue(Func<string, decimal> priceOf)
        {
            return Positions.Sum(p => p.Quantity * priceOf(p.InstrumentId));
        }

        public decimal UnrealizedPnl(Func<string, decimal> priceOf)
        {
            return Positions.Sum(p => p.Quantity * priceOf(p.InstrumentId) - p.CostBasis);
        }
    }
}
