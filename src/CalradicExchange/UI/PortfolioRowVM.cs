using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// One position in the portfolio table, with Buy / Sell buttons like the market tables (ADR-025) and an Order
    /// button for a take-profit / stop-loss order (ADR-039).
    /// </summary>
    public sealed class PortfolioRowVM : ViewModel
    {
        private readonly Action _onBuy;
        private readonly Action _onSell;
        private Action _onOrder;

        public PortfolioRowVM(string name, string quantity, string average, string now, string pnl, string pnlPercent, Color pnlColor,
            bool alternate, bool canBuy, bool canSell, string buyText, string sellText, Action onBuy, Action onSell)
        {
            Name = name;
            Quantity = quantity;
            Average = average;
            Now = now;
            Pnl = pnl;
            PnlPercent = pnlPercent;
            PnlColor = pnlColor;
            IsAlternate = alternate;
            CanBuy = canBuy;
            CanSell = canSell;
            BuyText = buyText;
            SellText = sellText;
            _onBuy = onBuy;
            _onSell = onSell;
        }

        [DataSourceProperty] public string Name { get; }
        [DataSourceProperty] public string Quantity { get; }
        [DataSourceProperty] public string Average { get; }
        [DataSourceProperty] public string Now { get; }
        [DataSourceProperty] public string Pnl { get; }
        [DataSourceProperty] public string PnlPercent { get; }
        [DataSourceProperty] public Color PnlColor { get; }
        [DataSourceProperty] public bool IsAlternate { get; }
        [DataSourceProperty] public bool CanBuy { get; }
        [DataSourceProperty] public bool CanSell { get; }
        [DataSourceProperty] public string BuyText { get; }
        [DataSourceProperty] public string SellText { get; }
        [DataSourceProperty] public bool CanOrder { get; private set; }
        [DataSourceProperty] public string OrderText { get; private set; } = "";

        /// <summary>Shows the Order button right of Sell (not on guild partnerships).</summary>
        public PortfolioRowVM WithOrder(bool canOrder, string text, Action onOrder)
        {
            CanOrder = canOrder;
            OrderText = text ?? "";
            _onOrder = onOrder;
            return this;
        }

        /// <summary>Bound to the row's Buy button (Command.Click="ExecuteBuy").</summary>
        public void ExecuteBuy()
        {
            if (CanBuy) _onBuy?.Invoke();
        }

        /// <summary>Bound to the row's Sell button (Command.Click="ExecuteSell").</summary>
        public void ExecuteSell()
        {
            if (CanSell) _onSell?.Invoke();
        }

        /// <summary>Bound to the row's Order button (Command.Click="ExecuteOrder").</summary>
        public void ExecuteOrder()
        {
            if (CanOrder) _onOrder?.Invoke();
        }
    }
}
