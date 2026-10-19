using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Data source of the table popups (GUI/Prefabs/CxMarketTable.xml, CxPortfolioTable.xml, CxTradeTable.xml,
    /// CxHistoryTable.xml). Column headers are generic slots H1..H8 so all tables share one view model.
    /// </summary>
    public sealed class ExchangeTableVM : ViewModel
    {
        private readonly Action _onClose;

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
        [DataSourceProperty] public string H1 { get; }
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
        [DataSourceProperty] public MBBindingList<MarketRowVM> MarketRows { get; } = new MBBindingList<MarketRowVM>();
        [DataSourceProperty] public MBBindingList<PortfolioRowVM> PortfolioRows { get; } = new MBBindingList<PortfolioRowVM>();
        [DataSourceProperty] public MBBindingList<GridRowVM> Rows { get; } = new MBBindingList<GridRowVM>();
        [DataSourceProperty] public MBBindingList<TradeRowVM> TradeRows { get; } = new MBBindingList<TradeRowVM>();

        /// <summary>Bound to the close button (Command.Click="ExecuteClose").</summary>
        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }

        private static string At(string[] a, int i) => a != null && i < a.Length ? a[i] : "";
    }
}
