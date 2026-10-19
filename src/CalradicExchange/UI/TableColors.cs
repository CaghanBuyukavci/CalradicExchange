using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>Text colors of the exchange tables, readable on the dark popup canvas.</summary>
    public static class TableColors
    {
        public static readonly Color Up = Color.ConvertStringToColor("#7CD67CFF");
        public static readonly Color Down = Color.ConvertStringToColor("#E87070FF");
        public static readonly Color Neutral = Color.ConvertStringToColor("#F1E9DAFF");
        public static readonly Color Muted = Color.ConvertStringToColor("#B8AE9EFF");

        /// <summary>Green above +0.05%, red below -0.05%, otherwise the normal text color.</summary>
        public static Color ForChange(double fraction)
        {
            if (fraction > 0.0005) return Up;
            if (fraction < -0.0005) return Down;
            return Neutral;
        }

        public static Color ForAmount(decimal amount)
        {
            if (amount >= 1) return Up;
            if (amount <= -1) return Down;
            return Neutral;
        }
    }
}
