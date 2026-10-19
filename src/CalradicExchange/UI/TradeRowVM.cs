using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>One instrument row of the trade tables (GUI/Prefabs/CxTradeTable.xml) with its own Buy and Sell buttons.</summary>
    public sealed class TradeRowVM : ViewModel
    {
        private readonly Action _onBuy;
        private readonly Action _onSell;
        private bool _isAlternate;

        /// <summary>A section header row (e.g. "Your shares in other towns").</summary>
        public TradeRowVM(string sectionText)
        {
            IsHeader = true;
            SectionText = sectionText ?? "";
            Name = Price = Day = Week = Held = BuyText = SellText = "";
            DayColor = WeekColor = TableColors.Neutral;
        }

        public TradeRowVM(string name, string price, string day, Color dayColor, string week, Color weekColor, string held,
            bool canBuy, bool canSell, bool alternate, string buyText, string sellText, Action onBuy, Action onSell)
        {
            BuyText = buyText;
            SellText = sellText;
            Name = name;
            Price = price;
            Day = day;
            DayColor = dayColor;
            Week = week;
            WeekColor = weekColor;
            Held = held;
            CanBuy = canBuy;
            CanSell = canSell;
            _isAlternate = alternate;
            _onBuy = onBuy;
            _onSell = onSell;
        }

        [DataSourceProperty] public bool IsHeader { get; }
        [DataSourceProperty] public bool IsItem => !IsHeader;
        [DataSourceProperty] public string SectionText { get; } = "";
        [DataSourceProperty] public string Name { get; }
        [DataSourceProperty] public string Price { get; }
        [DataSourceProperty] public string Day { get; }
        [DataSourceProperty] public Color DayColor { get; }
        [DataSourceProperty] public string Week { get; }
        [DataSourceProperty] public Color WeekColor { get; }
        [DataSourceProperty] public string Held { get; }
        [DataSourceProperty] public bool CanBuy { get; }

        /// <summary>The Sell button is shown only for instruments the player holds.</summary>
        [DataSourceProperty] public bool CanSell { get; }

        /// <summary>Settable so the shading can follow the rows left after a search.</summary>
        [DataSourceProperty]
        public bool IsAlternate
        {
            get => _isAlternate;
            set
            {
                if (_isAlternate == value) return;
                _isAlternate = value;
                OnPropertyChanged();
            }
        }
        [DataSourceProperty] public string BuyText { get; }
        [DataSourceProperty] public string SellText { get; }

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
    }
}
