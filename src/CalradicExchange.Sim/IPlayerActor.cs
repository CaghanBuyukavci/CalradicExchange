using System.Collections.Generic;

namespace CalradicExchange.Sim
{
    /// <summary>A bot that also changes the world, the way a player can (GDD 8: indirect manipulation).</summary>
    public interface IPlayerActor
    {
        /// <summary>Products whose villages the player raids today (before the market tick).</summary>
        IEnumerable<string> RaidsToday(SimContext ctx);
    }
}
