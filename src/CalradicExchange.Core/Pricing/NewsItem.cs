using System.Collections.Generic;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>An event that reached the exchange, for the News tab. Text is built by the game layer.</summary>
    public sealed class NewsItem
    {
        public int Day { get; set; }
        public MarketEvent Event { get; set; }
        public List<string> AffectedInstruments { get; set; } = new List<string>();
    }
}
