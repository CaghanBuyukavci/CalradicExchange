using System.Collections.Generic;
using CalradicExchange.Core.Guilds;
using CalradicExchange.Core.Kingdoms;
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

        /// <summary>Exchange day the current market was started (first tick or rebuild), -1 before. Added in schema 4.</summary>
        public int StartDay { get; set; } = -1;

        /// <summary>Exchange day of the last processed tick, -1 before the first.</summary>
        public int LastTickDay { get; set; } = -1;

        [JsonConverter(typeof(StringEnumConverter))]
        public Season? LastSeason { get; set; }

        /// <summary>Towns currently out of food (used to detect famine starts).</summary>
        public List<string> FamineTowns { get; set; } = new List<string>();

        /// <summary>Exchange day of each town's last famine start (cooldown). Added in schema 3.</summary>
        public Dictionary<string, int> FamineStartDays { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// How fundamentals were computed when prices were set. When it differs from
        /// <see cref="MarketEngine.FundamentalMethodId"/> the market is rebuilt (ADR-017). Added in schema 3.
        /// </summary>
        public string FundamentalMethod { get; set; }

        /// <summary>Average item price on the day it was first observed (sector normalization F_i,0).</summary>
        public Dictionary<string, decimal> BaseItemPrices { get; set; } = new Dictionary<string, decimal>();

        /// <summary>Average category price factor on the day it was first observed.</summary>
        public Dictionary<string, double> BaseCategoryFactors { get; set; } = new Dictionary<string, double>();

        public List<PriceState> Prices { get; set; } = new List<PriceState>();
        public List<NewsItem> News { get; set; } = new List<NewsItem>();
        public RandomState Rng { get; set; }

        /// <summary>Daily workshop profit per (town, workshop type), the base of guild shares. Added in schema 7.</summary>
        public List<GuildProfitSeries> Guilds { get; set; } = new List<GuildProfitSeries>();

        /// <summary>Kingdoms behind the kingdom indices (PLAN 2.4). Added in schema 10.</summary>
        public List<KingdomRecord> Kingdoms { get; set; } = new List<KingdomRecord>();

        /// <summary>Market news announced by a rumor, waiting for its day (ADR-040). Added in schema 15.</summary>
        public List<ScheduledMarketNews> ScheduledNews { get; set; } = new List<ScheduledMarketNews>();
    }
}
