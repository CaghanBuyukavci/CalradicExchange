using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>One line of the market table: either a section header or an instrument row.</summary>
    public sealed class MarketRowVM : ViewModel
    {
        public static MarketRowVM Section(string text) => new MarketRowVM { IsHeader = true, SectionText = text };

        public static MarketRowVM Instrument(string name, string price, string day, Color dayColor, string week, Color weekColor, bool alternate)
        {
            return new MarketRowVM
            {
                Name = name,
                Price = price,
                Day = day,
                DayColor = dayColor,
                Week = week,
                WeekColor = weekColor,
                IsAlternate = alternate,
            };
        }

        [DataSourceProperty] public bool IsHeader { get; private set; }
        [DataSourceProperty] public bool IsItem => !IsHeader;
        [DataSourceProperty] public bool IsAlternate { get; private set; }
        [DataSourceProperty] public string SectionText { get; private set; } = "";
        [DataSourceProperty] public string Name { get; private set; } = "";
        [DataSourceProperty] public string Price { get; private set; } = "";
        [DataSourceProperty] public string Day { get; private set; } = "";
        [DataSourceProperty] public Color DayColor { get; private set; } = TableColors.Neutral;
        [DataSourceProperty] public string Week { get; private set; } = "";
        [DataSourceProperty] public Color WeekColor { get; private set; } = TableColors.Neutral;
    }
}
