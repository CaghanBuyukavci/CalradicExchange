using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Sim
{
    /// <summary>Spreads the whole limit evenly over every instrument on the first day and holds to the end.</summary>
    public sealed class BuyAndHoldStrategy : IStrategy
    {
        private bool _bought;

        public string Name => "buy-and-hold";

        public void OnDay(SimContext ctx, IReadOnlyList<MarketEvent> events)
        {
            if (_bought) return;
            List<string> ids = ctx.Market.Catalog.Instruments.Select(i => i.Id).ToList();
            double budget = ctx.Orders.TotalLimit(ctx.Trader) * 0.95 / ids.Count;
            foreach (string id in ids) ctx.BuyFor(id, budget);
            _bought = true;
        }
    }
}
