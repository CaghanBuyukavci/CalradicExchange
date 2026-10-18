using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>One position in the portfolio table.</summary>
    public sealed class PortfolioRowVM : ViewModel
    {
        public PortfolioRowVM(string name, string quantity, string average, string now, string pnl, string pnlPercent, Color pnlColor, bool alternate)
        {
            Name = name;
            Quantity = quantity;
            Average = average;
            Now = now;
            Pnl = pnl;
            PnlPercent = pnlPercent;
            PnlColor = pnlColor;
            IsAlternate = alternate;
        }

        [DataSourceProperty] public string Name { get; }
        [DataSourceProperty] public string Quantity { get; }
        [DataSourceProperty] public string Average { get; }
        [DataSourceProperty] public string Now { get; }
        [DataSourceProperty] public string Pnl { get; }
        [DataSourceProperty] public string PnlPercent { get; }
        [DataSourceProperty] public Color PnlColor { get; }
        [DataSourceProperty] public bool IsAlternate { get; }
    }
}
