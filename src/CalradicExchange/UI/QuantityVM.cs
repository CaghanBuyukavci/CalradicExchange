using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Data source of the quantity popup (GUI/Prefabs/CxQuantityPopup.xml). The slider and the number box are both
    /// bound to <see cref="Quantity"/>, so moving one updates the other. The estimate line is rebuilt on each change.
    /// </summary>
    public sealed class QuantityVM : ViewModel
    {
        private readonly Func<int, string> _estimate;
        private readonly Action<int> _onConfirm;
        private readonly Action _onCancel;
        private int _quantity;
        private string _estimateText = "";

        public QuantityVM(string title, string prompt, int max, int initial, string confirmText, string cancelText,
            Func<int, string> estimate, Action<int> onConfirm, Action onCancel)
        {
            Title = title;
            Prompt = prompt;
            Max = Math.Max(1, max);
            MaxText = Max.ToString();
            ConfirmText = confirmText;
            CancelText = cancelText;
            _estimate = estimate;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _quantity = Clamp(initial);
            _estimateText = _estimate?.Invoke(_quantity) ?? "";
        }

        [DataSourceProperty] public string Title { get; }
        [DataSourceProperty] public string Prompt { get; }
        [DataSourceProperty] public int Min => 1;
        [DataSourceProperty] public int Max { get; }
        [DataSourceProperty] public string MinText => "1";
        [DataSourceProperty] public string MaxText { get; }
        [DataSourceProperty] public bool HasSlider => Max > 1;
        [DataSourceProperty] public string ConfirmText { get; }
        [DataSourceProperty] public string CancelText { get; }

        [DataSourceProperty]
        public int Quantity
        {
            get => _quantity;
            set
            {
                int v = Clamp(value);
                if (v == _quantity) return;
                _quantity = v;
                OnPropertyChangedWithValue(v, nameof(Quantity));
                EstimateText = _estimate?.Invoke(v) ?? "";
            }
        }

        [DataSourceProperty]
        public string EstimateText
        {
            get => _estimateText;
            private set
            {
                if (value == _estimateText) return;
                _estimateText = value;
                OnPropertyChangedWithValue(value, nameof(EstimateText));
            }
        }

        public void ExecuteDecrease() => Quantity = _quantity - 1;
        public void ExecuteIncrease() => Quantity = _quantity + 1;
        public void ExecuteSetMax() => Quantity = Max;

        public void ExecuteConfirm() => _onConfirm?.Invoke(_quantity);
        public void ExecuteCancel() => _onCancel?.Invoke();

        private int Clamp(int v) => v < 1 ? 1 : v > Max ? Max : v;
    }
}
