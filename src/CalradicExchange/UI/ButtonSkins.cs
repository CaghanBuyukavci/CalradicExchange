using System;
using CalradicExchange.Diagnostics;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Puts the runtime button images (GUI\Textures\cx_btn_*.png) on the Buy / Sell brushes of CxBrushes.xml.
    /// Brushes resolve sprites by name when the game starts, before our runtime sprites exist, so the layers are set
    /// here instead. Widgets copy their brush when created, so this must run before a trade table is shown.
    /// If an image is missing the brush keeps its plain colored square.
    /// </summary>
    public static class ButtonSkins
    {
        /// <summary>Pixel size of the button images.</summary>
        private const int Width = 53;
        private const int Height = 25;

        private static bool _applied;

        public static void EnsureApplied()
        {
            if (_applied) return;
            _applied = true;
            Apply("Cx.RowButton.Buy", "cx_btn_buy", "cx_btn_buy_hover");
            Apply("Cx.RowButton.Sell", "cx_btn_sell", "cx_btn_sell_hover");
        }

        private static void Apply(string brushName, string normalName, string hoverName)
        {
            try
            {
                Brush brush = UIResourceManager.BrushFactory?.GetBrush(brushName);
                Sprite normal = RuntimeSprites.Get(normalName, Width, Height);
                Sprite hover = RuntimeSprites.Get(hoverName, Width, Height) ?? normal;
                if (brush == null || normal == null)
                {
                    ExchangeLog.Warn($"Button skin {brushName} not applied (brush {(brush != null ? "ok" : "missing")}, image {(normal != null ? "ok" : "missing")}).");
                    return;
                }

                // The XML colors tint the plain square; the images carry their own colors.
                brush.DefaultLayer.Sprite = normal;
                brush.DefaultLayer.Color = Color.White;
                SetStyle(brush, "Default", normal, 1f);
                SetStyle(brush, "Hovered", hover, 1f);
                SetStyle(brush, "Pressed", hover, 0.85f);
                SetStyle(brush, "Disabled", normal, 0.5f);
                ExchangeLog.Info($"Button skin {brushName} applied.");
            }
            catch (Exception ex)
            {
                ExchangeLog.Error($"Button skin {brushName} failed", ex);
            }
        }

        private static void SetStyle(Brush brush, string styleName, Sprite sprite, float colorFactor)
        {
            StyleLayer layer = brush.GetStyle(styleName)?.GetLayer("Default");
            if (layer == null) return;
            layer.Sprite = sprite;
            layer.Color = Color.White;
            layer.ColorFactor = colorFactor;
        }
    }
}
