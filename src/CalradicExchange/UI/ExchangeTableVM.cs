using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Data source of the table popups (GUI/Prefabs/CxPortfolioTable.xml, CxTradeTable.xml,
    /// CxHistoryTable.xml, CxNewsTable.xml). Column headers are generic slots H1..H8 so all tables share one view
    /// model. Optional: a status line at the bottom left, a second button right of Close, and a search box.
    /// </summary>
    public sealed class ExchangeTableVM : ViewModel
    {
        private readonly Action _onClose;
        private string _h1;
        private string _emptyText = "";
        private string _searchText = "";
        private string _statusText = "";
        private string _secondaryText = "";
        private Action _onSecondary;
        private Action<string> _onSearch;

        public ExchangeTableVM(string title, string[] headers, string footer, string closeText, Action onClose, string description = null)
        {
            Title = title;
            H1 = At(headers, 0); H2 = At(headers, 1); H3 = At(headers, 2); H4 = At(headers, 3);
            H5 = At(headers, 4); H6 = At(headers, 5); H7 = At(headers, 6); H8 = At(headers, 7);
            Footer = footer ?? "";
            Description = description ?? "";
            CloseText = closeText;
            _onClose = onClose;
        }

        [DataSourceProperty] public string Title { get; }
        [DataSourceProperty] public string Description { get; }
        [DataSourceProperty] public bool HasDescription => Description.Length > 0;
        [DataSourceProperty]
        public string H1
        {
            get => _h1;
            set
            {
                if (_h1 == value) return;
                _h1 = value;
                OnPropertyChanged();
            }
        }

        [DataSourceProperty] public string H2 { get; }
        [DataSourceProperty] public string H3 { get; }
        [DataSourceProperty] public string H4 { get; }
        [DataSourceProperty] public string H5 { get; }
        [DataSourceProperty] public string H6 { get; }
        [DataSourceProperty] public string H7 { get; }
        [DataSourceProperty] public string H8 { get; }
        [DataSourceProperty] public string Footer { get; }
        [DataSourceProperty] public bool HasFooter => Footer.Length > 0;
        [DataSourceProperty] public string CloseText { get; }

        /// <summary>Shown in place of the rows when the current list is empty.</summary>
        [DataSourceProperty]
        public string EmptyText
        {
            get => _emptyText;
            set
            {
                value = value ?? "";
                if (_emptyText == value) return;
                _emptyText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        [DataSourceProperty] public bool IsEmpty => _emptyText.Length > 0;
        [DataSourceProperty] public MBBindingList<TabVM> Tabs { get; } = new MBBindingList<TabVM>();
        [DataSourceProperty] public MBBindingList<PortfolioRowVM> PortfolioRows { get; } = new MBBindingList<PortfolioRowVM>();
        [DataSourceProperty] public MBBindingList<GridRowVM> Rows { get; } = new MBBindingList<GridRowVM>();
        [DataSourceProperty] public MBBindingList<TradeRowVM> TradeRows { get; } = new MBBindingList<TradeRowVM>();

        /// <summary>Bottom-left line, e.g. gold and limit left.</summary>
        [DataSourceProperty]
        public string StatusText
        {
            get => _statusText;
            set
            {
                value = value ?? "";
                if (_statusText == value) return;
                _statusText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStatus));
            }
        }

        [DataSourceProperty] public bool HasStatus => _statusText.Length > 0;
        [DataSourceProperty] public string SecondaryText => _secondaryText;
        [DataSourceProperty] public bool HasSecondary => _onSecondary != null;

        /// <summary>Adds the button right of Close (Portfolio, Performance).</summary>
        public ExchangeTableVM WithSecondary(string text, Action onClick)
        {
            _secondaryText = text ?? "";
            _onSecondary = onClick;
            return this;
        }

        /// <summary>Bound to the second button (Command.Click="ExecuteSecondary").</summary>
        public void ExecuteSecondary()
        {
            _onSecondary?.Invoke();
        }

        [DataSourceProperty] public bool HasSearch => _onSearch != null;
        [DataSourceProperty] public string SearchPlaceholder { get; private set; } = "";

        /// <summary>Bound to the search box; every change calls the filter.</summary>
        [DataSourceProperty]
        public string SearchText
        {
            get => _searchText;
            set
            {
                value = value ?? "";
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                _onSearch?.Invoke(value);
            }
        }

        /// <summary>Shows the search box. <paramref name="onSearch"/> refills the rows; it runs once at once with <paramref name="initial"/>.</summary>
        public ExchangeTableVM WithSearch(string placeholder, string initial, Action<string> onSearch)
        {
            SearchPlaceholder = placeholder ?? "";
            _onSearch = onSearch;
            _searchText = initial ?? "";
            onSearch?.Invoke(_searchText);
            return this;
        }

        /// <summary>Bound to the close button (Command.Click="ExecuteClose").</summary>
        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }

        private static string At(string[] a, int i) => a != null && i < a.Length ? a[i] : "";
    }
}
