using System.Collections.Generic;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>What one daily tick did, for the log and for "big move" notifications.</summary>
    public sealed class TickSummary
    {
        public int Day { get; set; }

        /// <summary>False when the tick was ignored (same day ticked twice).</summary>
        public bool Processed { get; set; }

        /// <summary>True on the very first tick, which only sets initial prices.</summary>
        public bool Initialized { get; set; }

        public int InstrumentCount { get; set; }
        public string MinMoveId { get; set; }
        public double MinMove { get; set; }
        public string MaxMoveId { get; set; }
        public double MaxMove { get; set; }

        /// <summary>Events Core generated itself (season change, famine).</summary>
        public List<MarketEvent> GeneratedEvents { get; set; } = new List<MarketEvent>();

        public override string ToString()
        {
            if (!Processed) return $"day {Day}: skipped";
            if (Initialized) return $"day {Day}: initialized {InstrumentCount} instruments";
            return $"day {Day}: {InstrumentCount} instruments, min {MinMoveId} {MinMove:P2}, max {MaxMoveId} {MaxMove:P2}, generated events {GeneratedEvents.Count}";
        }
    }
}
