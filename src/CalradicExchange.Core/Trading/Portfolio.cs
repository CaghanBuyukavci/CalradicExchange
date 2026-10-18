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
        public int TradeCount { get; set; }

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

        /// <summary>Removes shares at average cost and returns the realized profit.</summary>
        public decimal ApplySell(string instrumentId, int quantity, int netProceeds, int commission)
        {
            Position pos = Find(instrumentId);
            if (pos == null || quantity <= 0 || quantity > pos.Quantity) throw new InvalidOperationException($"Cannot sell {quantity} of {instrumentId}.");

            decimal removedBasis = pos.Quantity == quantity ? pos.CostBasis : Math.Round(pos.CostBasis * quantity / pos.Quantity, 4);
            pos.Quantity -= quantity;
            pos.CostBasis -= removedBasis;
            if (pos.Quantity == 0) Positions.Remove(pos);

            decimal realized = netProceeds - removedBasis;
            RealizedPnl += realized;
            TotalCommissionPaid += commission;
            TradeCount++;
            return realized;
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
