using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Sim
{
    /// <summary>
    /// Reads the news and buys what the event table says should rise, on the day of the event,
    /// then sells after a fixed holding period. Models a player who follows the world closely.
    /// With <c>includeRaids</c> it also chases every single village raid (the "raid chaser").
    /// </summary>
    public sealed class EventFollowerStrategy : IStrategy
    {
        public const int HoldDays = 7;
        /// <summary>Share of the total limit put into one event trade.</summary>
        private const double BudgetShare = 0.15;

        /// <summary>A careful trader keeps slippage per order at or below this (order value = impact x depth / lambda).</summary>
        private const double MaxImpact = 0.005;

        private readonly List<(string Id, int SellDay, MarketEventType Cause, int Paid)> _open = new List<(string, int, MarketEventType, int)>();

        /// <summary>Per event type: number of round trips and their total profit in denars.</summary>
        public Dictionary<MarketEventType, (int Trades, long Pnl)> Stats { get; } = new Dictionary<MarketEventType, (int, long)>();

        private readonly bool _includeRaids;

        public EventFollowerStrategy(bool includeRaids)
        {
            _includeRaids = includeRaids;
        }

        public string Name => _includeRaids ? "raid-chaser" : "event-follower";

        public void OnDay(SimContext ctx, IReadOnlyList<MarketEvent> events)
        {
            foreach (var o in _open.Where(o => o.SellDay <= ctx.Day || ctx.Day == FinalDay).ToList())
            {
                int before = ctx.Gold;
                ctx.Sell(o.Id, ctx.Portfolio.QuantityOf(o.Id));
                Record(o.Cause, ctx.Gold - before - o.Paid);
                _open.RemoveAll(x => x.Id == o.Id);
            }

            foreach (MarketEvent ev in events)
            {
                if (ev.Type == MarketEventType.VillageLooted && !_includeRaids) continue;
                foreach (string id in Targets(ctx, ev))
                {
                    if (_open.Any(o => o.Id == id)) continue;
                    int before = ctx.Gold;
                    if (ctx.BuyFor(id, Budget(ctx, id)) > 0) _open.Add((id, ctx.Day + HoldDays, ev.Type, before - ctx.Gold));
                }
            }
        }

        /// <summary>Set by the runner so open trades are closed (and counted) on the last day.</summary>
        public int FinalDay { get; set; } = int.MaxValue;

        private static double Budget(SimContext ctx, string id)
        {
            var cp = ctx.Market.Settings.For(ctx.Market.Catalog.Find(id).Class);
            double lowImpact = cp.Lambda > 0 ? MaxImpact * cp.Depth / cp.Lambda : double.MaxValue;
            return Math.Min(ctx.Orders.TotalLimit(ctx.Trader) * BudgetShare, lowImpact);
        }

        private void Record(MarketEventType type, long pnl)
        {
            Stats.TryGetValue(type, out (int Trades, long Pnl) s);
            Stats[type] = (s.Trades + 1, s.Pnl + pnl);
        }

        private static IEnumerable<string> Targets(SimContext ctx, MarketEvent ev)
        {
            switch (ev.Type)
            {
                case MarketEventType.VillageLooted:
                    return ctx.Market.Catalog.Instruments.Where(i => i.Class == InstrumentClass.Commodity && i.ItemId == ev.ItemId).Select(i => i.Id);
                case MarketEventType.WarDeclared:
                case MarketEventType.SiegeStarted:
                    return new[] { "cx_sector_military" };
                case MarketEventType.PeaceMade:
                    return new[] { "cx_sector_luxury", "cx_sector_textile" };
                case MarketEventType.FamineStarted:
                    return ctx.Market.Catalog.Instruments.Where(i => i.HasTag("agriculture")).Select(i => i.Id);
                case MarketEventType.SeasonChanged when ev.Season == Season.Winter || ev.Season == Season.Spring:
                    return ctx.Market.Catalog.Instruments.Where(i => i.HasTag("agriculture")).Select(i => i.Id);
                default:
                    return Enumerable.Empty<string>();
            }
        }
    }
}
