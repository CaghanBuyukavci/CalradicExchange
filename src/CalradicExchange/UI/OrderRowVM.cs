using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>One take-profit / stop-loss order in the My Orders table (ADR-039); pending ones have a Cancel button.</summary>
    public sealed class OrderRowVM : ViewModel
    {
        private readonly Action _onCancel;

        public OrderRowVM(string[] cells, Color statusColor, Color resultColor, bool alternate, bool canCancel, string cancelText, Action onCancel)
        {
            Date = Cell(cells, 0);
            Name = Cell(cells, 1);
            Quantity = Cell(cells, 2);
            TakeProfit = Cell(cells, 3);
            StopLoss = Cell(cells, 4);
            Remaining = Cell(cells, 5);
            Status = Cell(cells, 6);
            Result = Cell(cells, 7);
            StatusColor = statusColor;
            ResultColor = resultColor;
            IsAlternate = alternate;
            CanCancel = canCancel;
            CancelText = cancelText;
            _onCancel = onCancel;
        }

        [DataSourceProperty] public string Date { get; }
        [DataSourceProperty] public string Name { get; }
        [DataSourceProperty] public string Quantity { get; }
        [DataSourceProperty] public string TakeProfit { get; }
        [DataSourceProperty] public string StopLoss { get; }
        [DataSourceProperty] public string Remaining { get; }
        [DataSourceProperty] public string Status { get; }
        [DataSourceProperty] public string Result { get; }
        [DataSourceProperty] public Color StatusColor { get; }
        [DataSourceProperty] public Color ResultColor { get; }
        [DataSourceProperty] public bool IsAlternate { get; }
        [DataSourceProperty] public bool CanCancel { get; }
        [DataSourceProperty] public string CancelText { get; }

        /// <summary>Bound to the row's Cancel button (Command.Click="ExecuteCancel").</summary>
        public void ExecuteCancel()
        {
            if (CanCancel) _onCancel?.Invoke();
        }

        private static string Cell(string[] cells, int i) => cells != null && i < cells.Length ? cells[i] ?? "" : "";
    }
}
