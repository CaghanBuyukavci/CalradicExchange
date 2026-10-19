using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>One tab button of a table popup (portfolio: shares / baskets / guilds).</summary>
    public sealed class TabVM : ViewModel
    {
        private static readonly Color SelectedColor = Color.ConvertStringToColor("#E4C59BFF");
        private static readonly Color IdleColor = Color.ConvertStringToColor("#A89F91FF");

        private readonly Action _onSelect;
        private bool _isSelected;
        private string _text;

        public TabVM(string text, bool selected, Action onSelect)
        {
            _text = text;
            _isSelected = selected;
            _onSelect = onSelect;
        }

        [DataSourceProperty]
        public string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                OnPropertyChanged();
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotSelected));
                OnPropertyChanged(nameof(TextColor));
            }
        }

        [DataSourceProperty] public bool IsNotSelected => !_isSelected;
        [DataSourceProperty] public Color TextColor => _isSelected ? SelectedColor : IdleColor;

        /// <summary>Bound to the tab button (Command.Click="ExecuteSelect").</summary>
        public void ExecuteSelect()
        {
            _onSelect?.Invoke();
        }
    }
}
