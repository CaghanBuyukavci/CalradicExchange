using System.Collections.Generic;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Sim
{
    /// <summary>A trading bot (PLAN 1.12). Called once per day after the market tick.</summary>
    public interface IStrategy
    {
        string Name { get; }

        /// <param name="events">Events of this day, including those Core generated (season, famine).</param>
        void OnDay(SimContext ctx, IReadOnlyList<MarketEvent> events);
    }
}
