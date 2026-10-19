using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Data source of the portfolio performance window (GUI/Prefabs/CxPerformance.xml): period tabs, a summary, the
    /// chart slices and the axis labels. The owner refills it through <see cref="Set"/> when a period is picked.
    /// </summary>
    public sealed class PerformanceVM : ViewModel
    {
        private readonly Action _onClose;
        private string _headline = "", _totals = "";
        private Color _headlineColor = TableColors.Neutral;
        private string _emptyText = "";
        private string _maxLabel = "", _zeroLabel = "", _minLabel = "", _startLabel = "", _endLabel = "";
        private float _zeroTop, _minLabelTop, _chartHeight;

        public PerformanceVM(string title, string closeText, Action onClose)
        {
            Title = title;
            CloseText = closeText;
            _onClose = onClose;
        }

        [DataSourceProperty] public string Title { get; }
        [DataSourceProperty] public string CloseText { get; }
        [DataSourceProperty] public MBBindingList<TabVM> Tabs { get; } = new MBBindingList<TabVM>();
        [DataSourceProperty] public MBBindingList<ChartColumnVM> Columns { get; } = new MBBindingList<ChartColumnVM>();

        /// <summary>"Last 7 days: +1,234 denars", in green or red.</summary>
        [DataSourceProperty] public string Headline { get => _headline; private set => SetText(ref _headline, value, nameof(Headline)); }
        [DataSourceProperty] public bool HasHeadline => _headline.Length > 0;

        [DataSourceProperty]
        public Color HeadlineColor
        {
            get => _headlineColor;
            private set
            {
                if (_headlineColor == value) return;
                _headlineColor = value;
                OnPropertyChangedWithValue(value, nameof(HeadlineColor));
            }
        }

        /// <summary>Total profit and portfolio value now.</summary>
        [DataSourceProperty] public string Totals { get => _totals; private set => SetText(ref _totals, value, nameof(Totals)); }
        [DataSourceProperty] public string EmptyText { get => _emptyText; private set { SetText(ref _emptyText, value, nameof(EmptyText)); OnPropertyChanged(nameof(IsEmpty)); OnPropertyChanged(nameof(HasChart)); } }
        [DataSourceProperty] public bool IsEmpty => _emptyText.Length > 0;
        [DataSourceProperty] public bool HasChart => !IsEmpty;
        [DataSourceProperty] public string MaxLabel { get => _maxLabel; private set => SetText(ref _maxLabel, value, nameof(MaxLabel)); }
        [DataSourceProperty] public string ZeroLabel { get => _zeroLabel; private set => SetText(ref _zeroLabel, value, nameof(ZeroLabel)); }
        [DataSourceProperty] public string MinLabel { get => _minLabel; private set => SetText(ref _minLabel, value, nameof(MinLabel)); }
        [DataSourceProperty] public string StartLabel { get => _startLabel; private set => SetText(ref _startLabel, value, nameof(StartLabel)); }
        [DataSourceProperty] public string EndLabel { get => _endLabel; private set => SetText(ref _endLabel, value, nameof(EndLabel)); }

        /// <summary>Distance of the zero line from the top of the chart.</summary>
        [DataSourceProperty]
        public float ZeroTop
        {
            get => _zeroTop;
            private set
            {
                if (_zeroTop == value) return;
                _zeroTop = value;
                OnPropertyChangedWithValue(value, nameof(ZeroTop));
                OnPropertyChangedWithValue(ZeroLabelTop, nameof(ZeroLabelTop));
            }
        }

        /// <summary>The zero label centered on the zero line, kept inside the chart (labels are 24 px high).</summary>
        [DataSourceProperty] public float ZeroLabelTop => Math.Max(0, Math.Min(_chartHeight - 24, _zeroTop - 12));

        [DataSourceProperty]
        public float MinLabelTop
        {
            get => _minLabelTop;
            private set
            {
                if (_minLabelTop == value) return;
                _minLabelTop = value;
                OnPropertyChangedWithValue(value, nameof(MinLabelTop));
            }
        }

        /// <summary>Replaces everything below the tabs. Empty <paramref name="emptyText"/> shows the chart.</summary>
        public void Set(string headline, Color headlineColor, string totals, string emptyText, float zeroTop, float chartHeight, string maxLabel, string zeroLabel, string minLabel,
            string startLabel, string endLabel)
        {
            Headline = headline ?? "";
            OnPropertyChanged(nameof(HasHeadline));
            HeadlineColor = headlineColor;
            Totals = totals ?? "";
            EmptyText = emptyText ?? "";
            _chartHeight = chartHeight;
            ZeroTop = zeroTop;
            OnPropertyChangedWithValue(ZeroLabelTop, nameof(ZeroLabelTop));
            MinLabelTop = chartHeight - 24;
            MaxLabel = maxLabel ?? "";
            ZeroLabel = zeroLabel ?? "";
            MinLabel = minLabel ?? "";
            StartLabel = startLabel ?? "";
            EndLabel = endLabel ?? "";
        }

        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }

        private void SetText(ref string field, string value, string name)
        {
            value = value ?? "";
            if (field == value) return;
            field = value;
            OnPropertyChangedWithValue(value, name);
        }
    }
}
