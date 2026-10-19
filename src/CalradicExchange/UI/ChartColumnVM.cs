using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// One thin slice of the performance chart (GUI/Prefabs/CxPerformance.xml). Positions are floats because Gauntlet
    /// binds layout properties (MarginTop, SuggestedHeight) without converting other number types.
    /// </summary>
    public sealed class ChartColumnVM : ViewModel
    {
        public ChartColumnVM(float lineTop, float lineHeight, float fillTop, float fillHeight, Color fillColor)
        {
            LineTop = lineTop;
            LineHeight = lineHeight;
            FillTop = fillTop;
            FillHeight = fillHeight;
            FillColor = fillColor;
        }

        [DataSourceProperty] public float LineTop { get; }
        [DataSourceProperty] public float LineHeight { get; }
        [DataSourceProperty] public float FillTop { get; }
        [DataSourceProperty] public float FillHeight { get; }
        [DataSourceProperty] public Color FillColor { get; }
    }
}
