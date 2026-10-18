namespace CalradicExchange.Core.Model
{
    /// <summary>Calendar lengths read from the game (ADR-008). Normal game: 7 / 21 / 84.</summary>
    public sealed class CalendarInfo
    {
        public static readonly CalendarInfo Default = new CalendarInfo(7, 21, 84);

        public CalendarInfo(int daysInWeek, int daysInSeason, int daysInYear)
        {
            DaysInWeek = daysInWeek;
            DaysInSeason = daysInSeason;
            DaysInYear = daysInYear;
        }

        public int DaysInWeek { get; }
        public int DaysInSeason { get; }
        public int DaysInYear { get; }
    }
}
