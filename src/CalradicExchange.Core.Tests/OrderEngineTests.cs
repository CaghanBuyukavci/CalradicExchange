using System;
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

            // impact = 0.1 x 1000 / 40000 = 0.0025; exec = 100 x 1.005 x 1.0025 = 100.75125
            Assert.True(q.IsValid);
            Assert.Equal(0.0025, q.Impact, 9);
            Assert.Equal(100.7513m, q.ExecutionPrice);
            Assert.Equal(1008, q.Gross);
            Assert.Equal(26, q.Commission); // ceil(2.5% x 1008)
            Assert.Equal(0, q.ProfitTax);
            Assert.Equal(1034, q.Total);
        }

        [Theory]
        [InlineData(0, 0.025)]
        [InlineData(60, 0.021)]
        [InlineData(150, 0.015)]
        [InlineData(300, 0.005)]
        [InlineData(1000, 0.005)]
        [InlineData(-10, 0.025)]
        public void CommissionRate_FallsWithTradeSkill(int skill, double expected)
        {
            Assert.Equal(expected, _orders.CommissionRate(skill), 9);
        }

        [Fact]
        public void Commission_HasMinimum()
        {
            Assert.Equal(2, _orders.Commission(50, 0));
            Assert.Equal(50, _orders.Commission(2000, 0));
            Assert.Equal(10, _orders.Commission(2000, 300));
        }

        [Fact]
        public void Buy_UpdatesPortfolio_AndMovesPrice()
        {
            TradeResult r = _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);

            Assert.True(r.Success);
            Assert.Equal(-1034, r.GoldDelta);
            Assert.Equal(10, _portfolio.QuantityOf("cx_grain"));
            Assert.Equal(1034m, _portfolio.Find("cx_grain").CostBasis);
            Assert.Equal(100.125m, _market.Get("cx_grain").Price); // 100 x (1 + 0.5 x 0.0025)
        }

        [Fact]
        public void Trades_AreRecordedInHistory_WithTheirCosts()
        {
            TradeResult buy = _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio, 7);
            TradeResult sell = _orders.Execute("cx_grain", OrderSide.Sell, 4, _rich, _portfolio, 9);

            Assert.Equal(2, _portfolio.History.Count);
            TradeRecord b = _portfolio.History[0], s = _portfolio.History[1];
            Assert.Equal(7, b.Day);
            Assert.Equal(OrderSide.Buy, b.Side);
            Assert.Equal(10, b.Quantity);
            Assert.Equal(buy.Quote.ExecutionPrice, b.ExecutionPrice);
            Assert.Equal(1034, b.Total);
            Assert.Equal(26, b.Commission);
            Assert.Equal(0m, b.RealizedPnl);
            Assert.Equal(9, s.Day);
            Assert.Equal(OrderSide.Sell, s.Side);
            Assert.Equal(sell.GoldDelta, s.Total);
            Assert.Equal(sell.RealizedPnl, s.RealizedPnl);
        }

        [Fact]
        public void RejectedTrade_IsNotRecorded()
        {
            _orders.Execute("cx_grain", OrderSide.Sell, 1, _rich, _portfolio, 3);
            Assert.Empty(_portfolio.History);
        }

        [Fact]
        public void History_KeepsOnlyTheNewestTrades()
        {
            var p = new Portfolio();
            for (int i = 1; i <= 5; i++) p.Record(new TradeRecord { Day = i }, 3);

            Assert.Equal(new[] { 3, 4, 5 }, p.History.ConvertAll(t => t.Day));
        }

        [Fact]
        public void ImmediateRoundTrip_LosesMoney_AndIsNotTaxed()
        {
            _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);
            TradeResult sell = _orders.Execute("cx_grain", OrderSide.Sell, 10, _rich, _portfolio);

            Assert.True(sell.Success);
            Assert.Equal(0, sell.Quote.ProfitTax);
            Assert.True(sell.GoldDelta < 1034);
            Assert.Equal(sell.GoldDelta - 1034, sell.RealizedPnl);
            Assert.Empty(_portfolio.Positions);
            Assert.Equal(sell.RealizedPnl, _portfolio.RealizedPnl);
        }

        [Fact]
        public void ProfitableSell_PaysProfitTax()
        {
            _orders.Execute("cx_grain", OrderSide.Buy, 10, _rich, _portfolio);
            _market.SetPrice("cx_grain", 150m);

            TradeQuote q = _orders.Quote("cx_grain", OrderSide.Sell, 10, _rich, _portfolio);
            int afterCommission = q.Gross - q.Commission;
            int expectedTax = (int)Math.Ceiling((afterCommission - 1034) * 0.10);

            Assert.Equal(expectedTax, q.ProfitTax);
            Assert.Equal(afterCommission - expectedTax, q.Total);

            TradeResult r = _orders.Execute("cx_grain", OrderSide.Sell, 10, _rich, _portfolio);
            Assert.Equal(q.Total - 1034, r.RealizedPnl);
            Assert.Equal(expectedTax, _portfolio.TotalProfitTaxPaid);
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
            Assert.Equal(5000, _orders.TotalLimit(new TraderProfile(0, 0, 0)));
            Assert.Equal(20000, _orders.TotalLimit(new TraderProfile(0, 1, 0)));
            Assert.Equal(5000 + 3 * 15000 + 50 * 100, _orders.TotalLimit(new TraderProfile(0, 3, 100)));
        }

        [Fact]
        public void TotalLimit_IsEnforced()
        {
            var beginner = new TraderProfile(1_000_000, 0, 0); // limit 5,000
            Assert.True(_orders.Execute("cx_grain", OrderSide.Buy, 40, beginner, _portfolio).Success); // ~4,000
            Assert.Equal(TradeError.TotalLimitExceeded, _orders.Quote("cx_salt", OrderSide.Buy, 3, beginner, _portfolio).Error); // +1,200
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
