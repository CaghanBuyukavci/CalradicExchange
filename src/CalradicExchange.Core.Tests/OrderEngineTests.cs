using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class OrderEngineTests
    {
        private readonly MarketEngine _market;
        private readonly OrderEngine _orders;
        private readonly Portfolio _portfolio = new Portfolio();
        private readonly TraderProfile _rich = new TraderProfile(1_000_000, 6, 0);

        public OrderEngineTests()
        {
            _market = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), new MarketState(), new XorShift128Plus(1));
            _market.Tick(TestData.World(1)); // grain share = 100
            _orders = new OrderEngine(_market);
        }

        [Fact]
        public void BuyQuote_HasExactCostBreakdown()
        {
            TradeQuote q = _orders.Quote("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);

            // impact = 0.5 x 1000 / 40000 = 0.0125; exec = 100 x 1.01 x 1.0125 = 102.2625
            Assert.True(q.IsValid);
            Assert.Equal(0.0125, q.Impact, 9);
            Assert.Equal(102.2625m, q.ExecutionPrice);
            Assert.Equal(1023, q.Gross);
            Assert.Equal(11, q.Commission); // ceil(max(10, 10.23))
            Assert.Equal(1034, q.Total);
        }

        [Fact]
        public void Commission_HasMinimum_AndTradeSkillDiscount()
        {
            Assert.Equal(10, _orders.Commission(100, 0));
            Assert.Equal(20, _orders.Commission(2000, 0));
            Assert.Equal(10, _orders.Commission(2000, 300));
            Assert.Equal(15, _orders.Commission(2000, 150));
            Assert.Equal(10, _orders.Commission(2000, 1000)); // capped at 300
        }

        [Fact]
        public void Buy_UpdatesPortfolio_AndMovesPrice()
        {
            TradeResult r = _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);

            Assert.True(r.Success);
            Assert.Equal(-1034, r.GoldDelta);
            Assert.Equal(10, _portfolio.QuantityOf("cx_grain"));
            Assert.Equal(1034m, _portfolio.Find("cx_grain").CostBasis);
            Assert.Equal(100.625m, _market.Get("cx_grain").Price); // 100 x (1 + 0.5 x 0.0125)
        }

        [Fact]
        public void ImmediateRoundTrip_LosesMoney()
        {
            _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);
            TradeResult sell = _orders.Execute("cx_grain", OrderSide.Sell, 10, _rich, _portfolio);

            Assert.True(sell.Success);
            Assert.True(sell.GoldDelta < 1034);
            Assert.Equal(sell.GoldDelta - 1034, sell.RealizedPnl);
            Assert.Equal(0, _portfolio.QuantityOf("cx_grain"));
            Assert.Empty(_portfolio.Positions);
            Assert.Equal(sell.RealizedPnl, _portfolio.RealizedPnl);
        }

        [Fact]
        public void PartialSell_UsesAverageCost()
        {
            _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);
            TradeResult sell = _orders.Execute("cx_grain", OrderSide.Sell, 4, _rich, _portfolio);

            Assert.Equal(sell.GoldDelta - 1034m * 4 / 10, sell.RealizedPnl);
            Assert.Equal(1034m * 6 / 10, _portfolio.Find("cx_grain").CostBasis);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void ZeroOrNegativeQuantity_IsRejected(int qty)
        {
            Assert.Equal(TradeError.InvalidQuantity, _orders.Quote("cx_grain", OrderSide.Buy, qty, _rich, _portfolio).Error);
        }

        [Fact]
        public void NotEnoughGold_IsRejected_AndChangesNothing()
        {
            TradeResult r = _orders.Execute("cx_grain", OrderSide.Buy, 10, new TraderProfile(1033, 6, 0), _portfolio);

            Assert.Equal(TradeError.InsufficientGold, r.Quote.Error);
            Assert.Equal(0, r.GoldDelta);
            Assert.Empty(_portfolio.Positions);
            Assert.Equal(100m, _market.Get("cx_grain").Price);
        }

        [Fact]
        public void SellingMoreThanHeld_IsRejected()
        {
            _orders.Execute("cx_grain", OrderSide.Buy, 5, _rich, _portfolio);
            Assert.Equal(TradeError.InsufficientShares, _orders.Quote("cx_grain", OrderSide.Sell, 6, _rich, _portfolio).Error);
            Assert.Equal(TradeError.InsufficientShares, _orders.Quote("cx_salt", OrderSide.Sell, 1, _rich, _portfolio).Error);
        }

        [Fact]
        public void UnknownInstrument_IsRejected()
        {
            Assert.Equal(TradeError.UnknownInstrument, _orders.Quote("nope", OrderSide.Buy, 1, _rich, _portfolio).Error);
        }

        [Fact]
        public void TotalLimit_DependsOnTierAndSkill()
        {
            Assert.Equal(20000, _orders.TotalLimit(new TraderProfile(0, 0, 0)));
            Assert.Equal(20000 + 3 * 20000 + 50 * 100, _orders.TotalLimit(new TraderProfile(0, 3, 100)));
        }

        [Fact]
        public void TotalLimit_IsEnforced()
        {
            var poor = new TraderProfile(1_000_000, 0, 0); // limit 20,000
            Assert.True(_orders.Execute("cx_grain", OrderSide.Buy, 150, poor, _portfolio).Success); // ~15,000
            Assert.Equal(TradeError.TotalLimitExceeded, _orders.Quote("cx_salt", OrderSide.Buy, 20, poor, _portfolio).Error); // +8,000
        }

        [Fact]
        public void InstrumentLimit_IsEnforced()
        {
            // commodity limit = 40,000 x 1.5 = 60,000 -> 600 grain shares at 100
            Assert.Equal(TradeError.InstrumentLimitExceeded, _orders.Quote("cx_grain", OrderSide.Buy, 601, _rich, _portfolio).Error);
            Assert.True(_orders.Quote("cx_grain", OrderSide.Buy, 600, _rich, _portfolio).IsValid);
        }

        [Fact]
        public void MaxBuyQuantity_IsTheLargestValidOrder()
        {
            var trader = new TraderProfile(5000, 6, 0);
            int max = _orders.MaxBuyQuantity("cx_grain", trader, _portfolio);

            Assert.True(_orders.Quote("cx_grain", OrderSide.Buy, max, trader, _portfolio).IsValid);
            Assert.False(_orders.Quote("cx_grain", OrderSide.Buy, max + 1, trader, _portfolio).IsValid);
        }

        [Fact]
        public void Portfolio_MarksToMarket()
        {
            _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);
            decimal price = _market.Get("cx_grain").Price;

            Assert.Equal(10 * price, _portfolio.MarketValue(id => _market.Get(id).Price));
            Assert.Equal(10 * price - 1034, _portfolio.UnrealizedPnl(id => _market.Get(id).Price));
        }

    }
}
