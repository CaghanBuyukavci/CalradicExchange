using System.Collections.Generic;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Random;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>All persisted market data. Part of <see cref="Persistence.ExchangeState"/>.</summary>
    public sealed class MarketState
    {
        public bool Initialized { get; set; }

        /// <summary>Exchange day of the last processed tick, -1 before the first.</summary>
        public int LastTickDay { get; set; } = -1;

        [JsonConverter(typeof(StringEnumConverter))]
        public Season? LastSeason { get; set; }

        /// <summary>Towns currently out of food (used to detect famine starts).</summary>
        public List<string> FamineTowns { get; set; } = new List<string>();

        /// <summary>Average item price on the day it was first observed (sector normalization F_i,0).</summary>
        public Dictionary<string, decimal> BaseItemPrices { get; set; } = new Dictionary<string, decimal>();

        /// <summary>Average category price factor on the day it was first observed.</summary>
        public Dictionary<string, double> BaseCategoryFactors { get; set; } = new Dictionary<string, double>();

        public List<PriceState> Prices { get; set; } = new List<PriceState>();
        public List<NewsItem> News { get; set; } = new List<NewsItem>();
        public RandomState Rng { get; set; }
    }
}
