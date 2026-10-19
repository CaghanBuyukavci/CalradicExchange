using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Executed trades, oldest first, trimmed to the configured limit. Added in schema 6.</summary>
        public List<TradeRecord> History { get; set; } = new List<TradeRecord>();

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
