using System;
using System.Runtime.CompilerServices;
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
        private string _title = "", _description = "";
        private string _h1 = "", _h2 = "", _h3 = "", _h4 = "", _h5 = "", _h6 = "", _h7 = "", _h8 = "";
        private string _emptyText = "";
        private string _searchText = "";
        private string _statusText = "";
        private string _secondaryText = "";
        private Action _onSecondary;
        private Action<string> _onSearch;
        private bool _searchVisible;

        public ExchangeTableVM(string title, string[] headers, string footer, string closeText, Action onClose, string description = null)
        {
            _title = title ?? "";
            SetHeaders(headers);
            Footer = footer ?? "";
            _description = description ?? "";
            CloseText = closeText;
            _onClose = onClose;
        }

        [DataSourceProperty] public string Title { get => _title; set => Set(ref _title, value); }

        [DataSourceProperty]
        public string Description
        {
            get => _description;
            set
            {
                if (Set(ref _description, value)) OnPropertyChanged(nameof(HasDescription));
            }
        }

        [DataSourceProperty] public bool HasDescription => _description.Length > 0;
        [DataSourceProperty] public string H1 { get => _h1; set => Set(ref _h1, value); }
        [DataSourceProperty] public string H2 { get => _h2; set => Set(ref _h2, value); }
        [DataSourceProperty] public string H3 { get => _h3; set => Set(ref _h3, value); }
        [DataSourceProperty] public string H4 { get => _h4; set => Set(ref _h4, value); }
        [DataSourceProperty] public string H5 { get => _h5; set => Set(ref _h5, value); }
        [DataSourceProperty] public string H6 { get => _h6; set => Set(ref _h6, value); }
        [DataSourceProperty] public string H7 { get => _h7; set => Set(ref _h7, value); }
        [DataSourceProperty] public string H8 { get => _h8; set => Set(ref _h8, value); }

        /// <summary>Replaces all column headers (a tab switch); missing ones become empty.</summary>
        public void SetHeaders(string[] headers)
        {
            H1 = At(headers, 0); H2 = At(headers, 1); H3 = At(headers, 2); H4 = At(headers, 3);
            H5 = At(headers, 4); H6 = At(headers, 5); H7 = At(headers, 6); H8 = At(headers, 7);
        }

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

        [DataSourceProperty] public bool HasSearch => _onSearch != null && _searchVisible;

        /// <summary>Shows or hides the search box set up by <see cref="WithSearch"/> (only some tabs filter).</summary>
        public void ShowSearch(bool visible)
        {
            if (_searchVisible == visible) return;
            _searchVisible = visible;
            OnPropertyChanged(nameof(HasSearch));
        }
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
            _searchVisible = true;
            _searchText = initial ?? "";
            onSearch?.Invoke(_searchText);
            return this;
        }

        /// <summary>Bound to the close button (Command.Click="ExecuteClose").</summary>
        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }

        private static string At(string[] a, int i) => a != null && i < a.Length ? a[i] ?? "" : "";

        private bool Set(ref string field, string value, [CallerMemberName] string name = null)
        {
            value = value ?? "";
            if (field == value) return false;
            field = value;
            OnPropertyChangedWithValue(value, name);
            return true;
        }
    }
}
