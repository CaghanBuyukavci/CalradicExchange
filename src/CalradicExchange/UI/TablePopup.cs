using System;
using CalradicExchange.Diagnostics;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Shows one of our Gauntlet table prefabs on top of the current screen (PLAN 2.6 spike, pulled forward).
    /// Returns false when the layer cannot be created so the caller can fall back to a text inquiry (ADR-006).
    /// </summary>
    public static class TablePopup
    {
        private static GauntletLayer _layer;
        private static ScreenBase _screen;

        public static bool Show(string movieName, Func<Action, ExchangeTableVM> buildViewModel)
        {
            Close();
            try
            {
                ScreenBase screen = ScreenManager.TopScreen;
                if (screen == null) return false;

                var layer = new GauntletLayer("CxExchangeTable", 2000);
                layer.LoadMovie(movieName, buildViewModel(Close));
                layer.InputRestrictions.SetInputRestrictions();
                layer.IsFocusLayer = true;
                screen.AddLayer(layer);
                ScreenManager.TrySetFocus(layer);

                _layer = layer;
                _screen = screen;
                return true;
            }
            catch (Exception ex)
            {
                ExchangeLog.Error($"Table popup '{movieName}' failed; falling back to text.", ex);
                Close();
                return false;
            }
        }

        public static void Close()
        {
            if (_layer == null) return;
            try
            {
                _layer.InputRestrictions.ResetInputRestrictions();
                ScreenManager.TryLoseFocus(_layer);
                _screen?.RemoveLayer(_layer);
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Closing table popup failed", ex);
            }
            finally
            {
                _layer = null;
                _screen = null;
            }
        }
    }
}
