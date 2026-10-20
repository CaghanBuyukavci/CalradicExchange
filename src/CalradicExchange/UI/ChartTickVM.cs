using TaleWorlds.Library;

namespace CalradicExchange.UI
{
    /// <summary>
    /// One day mark on the x axis of the performance chart. The marks sit in a horizontal list, so each slot is as
    /// wide as the gap to the previous mark and the dot is drawn at its right end. Float for Gauntlet's layout binding.
    /// </summary>
    public sealed class ChartTickVM : ViewModel
    {
        public ChartTickVM(float width)
        {
            Width = width;
        }

        [DataSourceProperty] public float Width { get; }
    }
}
