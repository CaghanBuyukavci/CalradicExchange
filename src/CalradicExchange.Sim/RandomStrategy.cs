using System.Collections.Generic;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Trading;

namespace CalradicExchange.Sim
{
    /// <summary>Trades without information: on ~30% of days buys or sells a random instrument.</summary>
    public sealed class RandomStrategy : IStrategy
    {
        public string Name => "random";

        public void OnDay(SimContext ctx, IReadOnlyList<MarketEvent> events)
        {
            if (ctx.Rng.NextDouble() >= 0.3) return;
            var instruments = ctx.Market.Catalog.Instruments;
            string id = instruments[ctx.Rng.NextInt(instruments.Count)].Id;

            if (ctx.Rng.NextDouble() < 0.5)
            {
                ctx.BuyFor(id, ctx.Orders.TotalLimit(ctx.Trader) * (0.05 + ctx.Rng.NextDouble() * 0.1));
            }
            else
            {
                Position pos = ctx.Portfolio.Find(id);
                if (pos != null) ctx.Sell(id, 1 + ctx.Rng.NextInt(pos.Quantity));
            }
        }
    }
}
