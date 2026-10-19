using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Data source of the buy / sell confirmation (GUI/Prefabs/CxConfirmPopup.xml): a heading and a label | value list
    /// of the cost breakdown. Rows use GridRowVM cells C1 (label) and C2 (value); IsAlternate marks the total rows.
    /// </summary>
    public sealed class ConfirmVM : ViewModel
    {
        private readonly Action _onConfirm;
        private readonly Action _onCancel;

        public ConfirmVM(string title, string heading, string confirmText, string cancelText, Action onConfirm, Action onCancel)
        {
            Title = title;
            Heading = heading;
            ConfirmText = confirmText;
            CancelText = cancelText;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        [DataSourceProperty] public string Title { get; }
        [DataSourceProperty] public string Heading { get; }
        [DataSourceProperty] public string ConfirmText { get; }
        [DataSourceProperty] public string CancelText { get; }
        [DataSourceProperty] public MBBindingList<GridRowVM> Rows { get; } = new MBBindingList<GridRowVM>();

        /// <summary>Adds one line; <paramref name="total"/> highlights it.</summary>
        public ConfirmVM Add(string label, string value, Color? color = null, bool total = false)
        {
            var row = new GridRowVM(new[] { label, value }, total);
            if (color != null) row.WithColor(2, color.Value);
            Rows.Add(row);
            return this;
        }

        public void ExecuteConfirm() => _onConfirm?.Invoke();
        public void ExecuteCancel() => _onCancel?.Invoke();
    }
}
