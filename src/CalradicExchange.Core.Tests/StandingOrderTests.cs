using System.Linq;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    /// <summary>Take-profit / stop-loss orders on held shares (ADR-039).</summary>
    public class StandingOrderTests
    {
        private readonly MarketEngine _market;
        private readonly OrderEngine _orders;
        private readonly Portfolio _pf = new Portfolio();
        private readonly TraderProfile _rich = new TraderProfile(1_000_000, 6, 0);

        public StandingOrderTests()
        {
            var settings = TestData.NoNoiseSettings();
            settings.Npc.SupplyDepthMultiple = 1e6;
            settings.Npc.InitialHoldingFraction = 0;
            _market = new MarketEngine(TestData.Catalog(), settings, new MarketState(), new XorShift128Plus(1));
            _market.Tick(TestData.World(1)); // grain share = 100
            _orders = new OrderEngine(_market);
            _orders.Execute("cx_grain", OrderSide.Buy, 20, _rich, _pf, 1);
        }

        private StandingOrder Place(int qty, int tp, int sl, int days = 10, int day = 1)
        {
            StandingOrder o = _orders.PlaceStandingOrder("cx_grain", qty, tp, sl, days, _rich, _pf, day, out StandingOrderQuote q);
            Assert.True(q.IsValid, q.Error.ToString());
            return o;
        }

        [Fact]
        public void Fee_GrowsWithValueAndDays_WithAMinimum()
        {
            Assert.Equal(10, _orders.StandingOrderFee(1000m, 30)); // 1000 x 0.8 % = 8 -> minimum
            Assert.Equal(80, _orders.StandingOrderFee(10000m, 30)); // 10000 x (0.2 % + 30 x 0.02 %)
            Assert.Equal(32, _orders.StandingOrderFee(10000m, 6));
        }

        [Fact]
        public void Placing_LocksTheShares_AndBooksTheFee()
        {
            decimal realizedBefore = _pf.RealizedPnl;
            long feesBefore = _pf.TotalCommissionPaid;
            Position pos = _pf.Find("cx_grain");

            StandingOrder o = Place(12, 20, 10);

            Assert.Equal(20, _pf.QuantityOf("cx_grain"));
            Assert.Equal(12, _pf.LockedQuantity("cx_grain"));
            Assert.Equal(8, _pf.FreeQuantity("cx_grain"));
            Assert.Equal(System.Math.Round(pos.AverageCost * 1.2m, 4), o.TakeProfitPrice);
            Assert.Equal(System.Math.Round(pos.AverageCost * 0.9m, 4), o.StopLossPrice);
            Assert.Equal(11, o.ExpiryDay);
            Assert.Equal(realizedBefore - o.Fee, _pf.RealizedPnl);
            Assert.Equal(feesBefore + o.Fee, _pf.TotalCommissionPaid);

            // Locked shares cannot be sold or put in another order.
            Assert.Equal(TradeError.InsufficientShares, _orders.Quote("cx_grain", OrderSide.Sell, 9, _rich, _pf).Error);
            Assert.True(_orders.Quote("cx_grain", OrderSide.Sell, 8, _rich, _pf).IsValid);
            Assert.Equal(TradeError.InsufficientShares, _orders.QuoteStandingOrder("cx_grain", 9, 10, 0, 5, _rich, _pf).Error);
        }

        [Fact]
        public void Quote_RejectsBadOrders()
        {
            Assert.Equal(TradeError.NoOrderTarget, _orders.QuoteStandingOrder("cx_grain", 5, 0, 0, 5, _rich, _pf).Error);
            Assert.Equal(TradeError.InvalidOrder, _orders.QuoteStandingOrder("cx_grain", 5, 10, 0, 0, _rich, _pf).Error);
            Assert.Equal(TradeError.InvalidOrder, _orders.QuoteStandingOrder("cx_grain", 5, 10, 60, 5, _rich, _pf).Error);
            Assert.Equal(TradeError.InsufficientShares, _orders.QuoteStandingOrder("cx_grain", 21, 10, 0, 5, _rich, _pf).Error);
            Assert.Equal(TradeError.InsufficientGold, _orders.QuoteStandingOrder("cx_grain", 5, 10, 0, 5, new TraderProfile(5, 0, 0), _pf).Error);

            // Guilds are listed after a few days of workshop profits.
            var guilds = new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), new MarketState(), new XorShift128Plus(3));
            for (int d = 1; d <= 9; d++) guilds.Tick(TestData.World(d));
            Assert.Equal(TradeError.OrdersNotForGuilds,
                new OrderEngine(guilds).QuoteStandingOrder("cx_guild_town_A_smithy", 1, 10, 0, 5, _rich, new Portfolio()).Error);
        }

        [Fact]
        public void TakeProfit_SellsAtTheMarket_WithoutCommission_ButWithProfitTax()
        {
            StandingOrder o = Place(10, 10, 0);
            Assert.Empty(_orders.ProcessStandingOrders(_pf, 2)); // price 100, target ~110.8

            _market.SetPrice("cx_grain", 120m);
            StandingOrder filled = Assert.Single(_orders.ProcessStandingOrders(_pf, 3));

            Assert.Same(o, filled);
            Assert.Equal(StandingOrderStatus.Filled, o.Status);
            Assert.Equal(StandingOrderTrigger.TakeProfit, o.Trigger);
            Assert.Equal(10, _pf.QuantityOf("cx_grain"));
            Assert.Equal(0, _pf.LockedQuantity("cx_grain"));
            TradeRecord t = _pf.History.Last();
            Assert.Equal(StandingOrderTrigger.TakeProfit, t.Order);
            Assert.Equal(0, t.Commission);
            Assert.True(t.ProfitTax > 0);
            Assert.Equal(o.Proceeds, t.Total);
            Assert.True(o.RealizedPnl > 0 && o.FillPrice < 120m && o.FillPrice > 119m, $"{o.FillPrice} {o.RealizedPnl}");
        }

        [Fact]
        public void StopLoss_SellsAtTheDaysPrice_EvenBelowTheTarget()
        {
            StandingOrder o = Place(10, 0, 10);
            _market.SetPrice("cx_grain", 80m);

            Assert.Single(_orders.ProcessStandingOrders(_pf, 2));
            Assert.Equal(StandingOrderTrigger.StopLoss, o.Trigger);
            Assert.True(o.FillPrice < o.StopLossPrice && o.FillPrice < 80m, $"{o.FillPrice}");
            Assert.True(o.RealizedPnl < 0);
            Assert.Equal(0, _pf.History.Last().ProfitTax);
        }

        [Fact]
        public void AnOrderPastItsLastDay_Expires_AndTheSharesComeBack()
        {
            StandingOrder o = Place(10, 50, 0, days: 3);
            Assert.Empty(_orders.ProcessStandingOrders(_pf, 2));
            Assert.Empty(_orders.ProcessStandingOrders(_pf, 3));
            Assert.Single(_orders.ProcessStandingOrders(_pf, 4));

            Assert.Equal(StandingOrderStatus.Canceled, o.Status);
            Assert.Equal(StandingOrderCancelReason.Expired, o.CancelReason);
            Assert.Equal(20, _pf.FreeQuantity("cx_grain"));
            Assert.DoesNotContain(_pf.History, t => t.Order != StandingOrderTrigger.None);
        }

        [Fact]
        public void TheList_KeepsTheLast20_ButNeverDropsAPendingOrder()
        {
            StandingOrder first = Place(1, 10, 0);
            for (int i = 0; i < 24; i++) _pf.CancelOrder(Place(1, 10, 0), StandingOrderCancelReason.Player, 1);
            Place(1, 10, 0);

            Assert.Equal(20, _pf.Orders.Count);
            Assert.Contains(first, _pf.Orders);
            Assert.Equal(26, _pf.Orders.Last().Id);
        }

        [Fact]
        public void AForcedSale_ReleasesOrdersBeyondTheHolding()
        {
            StandingOrder a = Place(8, 10, 0);
            StandingOrder b = Place(8, 10, 0);
            _pf.ForceSell("cx_grain", 10, 100m, 2, 500);

            Assert.True(a.IsPending);
            Assert.Equal(StandingOrderCancelReason.Closed, b.CancelReason);
            Assert.Equal(8, _pf.LockedQuantity("cx_grain"));
        }

        [Fact]
        public void Orders_SurviveSaveAndLoad()
        {
            Place(5, 20, 10);
            _market.SetPrice("cx_grain", 130m);
            _orders.ProcessStandingOrders(_pf, 2);
            Place(5, 15, 0);

            var state = new ExchangeState { Portfolio = _pf };
            string json = StateSerializer.Serialize(state);
            Portfolio back = StateSerializer.Deserialize(json).Portfolio;

            Assert.Equal(json, StateSerializer.Serialize(new ExchangeState { Portfolio = back, Market = state.Market }));
            Assert.Equal(2, back.Orders.Count);
            Assert.Equal(StandingOrderTrigger.TakeProfit, back.Orders[0].Trigger);
            Assert.Equal(5, back.LockedQuantity("cx_grain"));
            Assert.Equal(3, back.NextOrderId);
        }
    }
}
