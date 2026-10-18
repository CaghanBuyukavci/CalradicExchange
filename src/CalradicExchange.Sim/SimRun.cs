using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;

namespace CalradicExchange.Sim
{
    /// <summary>One full simulation: synthetic world + market engine (+ optional trading bot).</summary>
    public sealed class SimRun
    {
        private SimRun() { }

        /// <summary>Per-instrument daily series (unbounded, unlike PriceState.History).</summary>
        public Dictionary<string, List<PricePoint>> Series { get; } = new Dictionary<string, List<PricePoint>>();
        public Dictionary<MarketEventType, int> EventCounts { get; } = new Dictionary<MarketEventType, int>();
        public SimContext Context { get; private set; }
        public IStrategy Strategy { get; private set; }

        public static SimRun Execute(InstrumentCatalog catalog, MarketSettings settings, ulong seed, int days, IStrategy strategy = null, TraderSetup trader = null)
        {
            var run = new SimRun { Strategy = strategy };
            var world = new SyntheticWorld(seed * 7919 + 1, catalog.RequiredCategoryIds);
            var market = new MarketEngine(catalog, settings, new MarketState(), new XorShift128Plus(seed));
            if (strategy != null) run.Context = new SimContext(market, trader ?? TraderSetup.Mid, seed * 31 + 7);
            foreach (InstrumentDefinition def in catalog.Instruments) run.Series[def.Id] = new List<PricePoint>();

            for (int day = 0; day <= days; day++)
            {
                List<MarketEvent> events = world.Advance(day);
                foreach (MarketEvent ev in events) market.ApplyEvent(ev);
                TickSummary tick = market.Tick(world.Snapshot(day));
                events.AddRange(tick.GeneratedEvents);
                foreach (MarketEvent ev in events)
                {
                    run.EventCounts.TryGetValue(ev.Type, out int n);
                    run.EventCounts[ev.Type] = n + 1;
                }

                foreach (PriceState ps in market.State.Prices.Where(p => p.HasPrice))
                {
                    run.Series[ps.InstrumentId].Add(new PricePoint { Day = day, P = ps.Price, F = ps.Fundamental, S = ps.Shock });
                }

                if (run.Context != null && day > 0)
                {
                    run.Context.Day = day;
                    if (day == days) run.Context.SellAll();
                    else strategy.OnDay(run.Context, events);
                }
            }
            return run;
        }
    }
}
