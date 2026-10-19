using System;
using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// One row of the generic tables (CxPickTable.xml, CxHistoryTable.xml): up to eight text cells C1..C8, each with
    /// its own color. Clickable rows call <see cref="ExecuteSelect"/>.
    /// </summary>
    public sealed class GridRowVM : ViewModel
    {
        private readonly string[] _cells;
        private readonly Color[] _colors = new Color[8];
        private readonly Action _onSelect;

        public GridRowVM(string[] cells, bool alternate, Action onSelect = null, bool enabled = true)
        {
            _cells = cells ?? new string[0];
            for (int i = 0; i < _colors.Length; i++) _colors[i] = TableColors.Neutral;
            IsAlternate = alternate;
            IsEnabled = enabled;
            _onSelect = onSelect;
        }

        /// <summary>Sets the text color of cell <paramref name="index"/> (1-based, like C1..C8).</summary>
        public GridRowVM WithColor(int index, Color color)
        {
            _colors[index - 1] = color;
            return this;
        }

        [DataSourceProperty] public bool IsAlternate { get; }
        [DataSourceProperty] public bool IsEnabled { get; }
        [DataSourceProperty] public string C1 => Cell(0);
        [DataSourceProperty] public string C2 => Cell(1);
        [DataSourceProperty] public string C3 => Cell(2);
        [DataSourceProperty] public string C4 => Cell(3);
        [DataSourceProperty] public string C5 => Cell(4);
        [DataSourceProperty] public string C6 => Cell(5);
        [DataSourceProperty] public string C7 => Cell(6);
        [DataSourceProperty] public string C8 => Cell(7);
        [DataSourceProperty] public Color Color1 => _colors[0];
        [DataSourceProperty] public Color Color2 => _colors[1];
        [DataSourceProperty] public Color Color3 => _colors[2];
        [DataSourceProperty] public Color Color4 => _colors[3];
        [DataSourceProperty] public Color Color5 => _colors[4];
        [DataSourceProperty] public Color Color6 => _colors[5];
        [DataSourceProperty] public Color Color7 => _colors[6];
        [DataSourceProperty] public Color Color8 => _colors[7];

        /// <summary>Bound to the row button (Command.Click="ExecuteSelect").</summary>
        public void ExecuteSelect()
        {
            if (IsEnabled) _onSelect?.Invoke();
        }

        private string Cell(int i) => i < _cells.Length ? _cells[i] ?? "" : "";
    }
}
