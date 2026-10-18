using System;
using System.Linq;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;

namespace CalradicExchange.Sim
{
    /// <summary>What a strategy bot sees and can do on one simulated day.</summary>
    public sealed class SimContext
    {
        public SimContext(MarketEngine market, TraderSetup trader, ulong seed)
        {
            Market = market;
            Orders = new OrderEngine(market);
            Setup = trader;
            Gold = trader.Gold;
            StartGold = trader.Gold;
            Rng = new XorShift128Plus(seed);
        }

        public TraderSetup Setup { get; }

        public MarketEngine Market { get; }
        public OrderEngine Orders { get; }
        public Portfolio Portfolio { get; } = new Portfolio();
        public XorShift128Plus Rng { get; }
        public int Day { get; set; }
        public int Gold { get; private set; }
        public int StartGold { get; }

        /// <summary>Largest amount of gold ever tied up in positions (cost basis).</summary>
        public decimal PeakInvested { get; private set; }

        public TraderProfile Trader => new TraderProfile(Gold, Setup.ClanTier, Setup.TradeSkill);

        /// <summary>Buys as many shares as fit in <paramref name="budget"/> (and the limits). Returns shares bought.</summary>
        public int BuyFor(string instrumentId, double budget)
        {
            PriceState ps = Market.Get(instrumentId);
            if (ps == null || !ps.HasPrice) return 0;
            int wanted = (int)Math.Floor(budget / ((double)ps.Price * 1.05));
            int qty = Math.Min(wanted, Orders.MaxBuyQuantity(instrumentId, Trader, Portfolio));
            if (qty <= 0) return 0;
            TradeResult r = Orders.Execute(instrumentId, OrderSide.Buy, qty, Trader, Portfolio);
            if (!r.Success) return 0;
            Gold += r.GoldDelta;
            PeakInvested = Math.Max(PeakInvested, Portfolio.Positions.Sum(p => p.CostBasis));
            return qty;
        }

        public void Sell(string instrumentId, int quantity)
        {
            quantity = Math.Min(quantity, Portfolio.QuantityOf(instrumentId));
            if (quantity <= 0) return;
            TradeResult r = Orders.Execute(instrumentId, OrderSide.Sell, quantity, Trader, Portfolio);
            if (r.Success) Gold += r.GoldDelta;
        }

        public void SellAll()
        {
            foreach (Position p in Portfolio.Positions.ToList()) Sell(p.InstrumentId, p.Quantity);
        }
    }
}
