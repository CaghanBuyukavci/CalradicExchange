using System;
using CalradicExchange.Core.Trading;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Data source of the take-profit / stop-loss window (GUI/Prefabs/CxOrderPopup.xml, ADR-039): four sliders
    /// (quantity, profit %, loss %, days), each with its value on the right, the fee below. Every change quotes the
    /// order again; the Place button is enabled only for a valid quote.
    /// </summary>
    public sealed class StandingOrderVM : ViewModel
    {
        private readonly Func<int, int, int, int, StandingOrderQuote> _quote;
        private readonly Func<StandingOrderQuote, StandingOrderTexts> _describe;
        private readonly Action<int, int, int, int> _onConfirm;
        private readonly Action _onCancel;
        private int _quantity, _takeProfit, _stopLoss, _days;
        private string _quantityText = "", _takeProfitText = "", _stopLossText = "", _daysText = "", _feeText = "", _errorText = "";
        private bool _canConfirm;

        public StandingOrderVM(string title, string heading, string[] labels, int maxQuantity, int maxTakeProfit, int maxStopLoss, int maxDays,
            int quantity, int takeProfit, int stopLoss, int days, string confirmText, string cancelText,
            Func<int, int, int, int, StandingOrderQuote> quote, Func<StandingOrderQuote, StandingOrderTexts> describe,
            Action<int, int, int, int> onConfirm, Action onCancel)
        {
            Title = title;
            Heading = heading;
            QuantityLabel = labels[0];
            TakeProfitLabel = labels[1];
            StopLossLabel = labels[2];
            DaysLabel = labels[3];
            MaxQuantity = Math.Max(1, maxQuantity);
            MaxTakeProfit = Math.Max(1, maxTakeProfit);
            MaxStopLoss = Math.Max(1, maxStopLoss);
            MaxDays = Math.Max(1, maxDays);
            ConfirmText = confirmText;
            CancelText = cancelText;
            _quote = quote;
            _describe = describe;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _quantity = Clamp(quantity, 1, MaxQuantity);
            _takeProfit = Clamp(takeProfit, 0, MaxTakeProfit);
            _stopLoss = Clamp(stopLoss, 0, MaxStopLoss);
            _days = Clamp(days, 1, MaxDays);
            Refresh();
        }

        [DataSourceProperty] public string Title { get; }
        [DataSourceProperty] public string Heading { get; }
        [DataSourceProperty] public string QuantityLabel { get; }
        [DataSourceProperty] public string TakeProfitLabel { get; }
        [DataSourceProperty] public string StopLossLabel { get; }
        [DataSourceProperty] public string DaysLabel { get; }
        [DataSourceProperty] public int MaxQuantity { get; }
        [DataSourceProperty] public int MaxTakeProfit { get; }
        [DataSourceProperty] public int MaxStopLoss { get; }
        [DataSourceProperty] public int MaxDays { get; }
        [DataSourceProperty] public string ConfirmText { get; }
        [DataSourceProperty] public string CancelText { get; }

        [DataSourceProperty]
        public int Quantity
        {
            get => _quantity;
            set => SetSlider(ref _quantity, Clamp(value, 1, MaxQuantity), nameof(Quantity));
        }

        [DataSourceProperty]
        public int TakeProfit
        {
            get => _takeProfit;
            set => SetSlider(ref _takeProfit, Clamp(value, 0, MaxTakeProfit), nameof(TakeProfit));
        }

        [DataSourceProperty]
        public int StopLoss
        {
            get => _stopLoss;
            set => SetSlider(ref _stopLoss, Clamp(value, 0, MaxStopLoss), nameof(StopLoss));
        }

        [DataSourceProperty]
        public int Days
        {
            get => _days;
            set => SetSlider(ref _days, Clamp(value, 1, MaxDays), nameof(Days));
        }

        [DataSourceProperty] public string QuantityText { get => _quantityText; private set => SetText(ref _quantityText, value, nameof(QuantityText)); }
        [DataSourceProperty] public string TakeProfitText { get => _takeProfitText; private set => SetText(ref _takeProfitText, value, nameof(TakeProfitText)); }
        [DataSourceProperty] public string StopLossText { get => _stopLossText; private set => SetText(ref _stopLossText, value, nameof(StopLossText)); }
        [DataSourceProperty] public string DaysText { get => _daysText; private set => SetText(ref _daysText, value, nameof(DaysText)); }
        [DataSourceProperty] public string FeeText { get => _feeText; private set => SetText(ref _feeText, value, nameof(FeeText)); }

        [DataSourceProperty]
        public string ErrorText
        {
            get => _errorText;
            private set
            {
                if (SetText(ref _errorText, value, nameof(ErrorText))) OnPropertyChanged(nameof(HasError));
            }
        }

        [DataSourceProperty] public bool HasError => _errorText.Length > 0;

        [DataSourceProperty]
        public bool CanConfirm
        {
            get => _canConfirm;
            private set
            {
                if (_canConfirm == value) return;
                _canConfirm = value;
                OnPropertyChangedWithValue(value, nameof(CanConfirm));
            }
        }

        public void ExecuteConfirm()
        {
            if (CanConfirm) _onConfirm?.Invoke(_quantity, _takeProfit, _stopLoss, _days);
        }

        public void ExecuteCancel() => _onCancel?.Invoke();

        private void SetSlider(ref int field, int value, string name)
        {
            if (field == value) return;
            field = value;
            OnPropertyChangedWithValue(value, name);
            Refresh();
        }

        private bool SetText(ref string field, string value, string name)
        {
            value = value ?? "";
            if (field == value) return false;
            field = value;
            OnPropertyChangedWithValue(value, name);
            return true;
        }

        private void Refresh()
        {
            StandingOrderQuote q = _quote?.Invoke(_quantity, _takeProfit, _stopLoss, _days);
            StandingOrderTexts t = q != null ? _describe?.Invoke(q) : null;
            if (t == null) return;
            QuantityText = t.Quantity;
            TakeProfitText = t.TakeProfit;
            StopLossText = t.StopLoss;
            DaysText = t.Days;
            FeeText = t.Fee;
            ErrorText = t.Error;
            CanConfirm = q.IsValid;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
