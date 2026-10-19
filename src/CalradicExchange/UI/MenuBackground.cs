using System;
using System.IO;
using CalradicExchange.Diagnostics;
using TaleWorlds.Engine.GauntletUI;
using GameTexture = TaleWorlds.Engine.Texture;
using UiTexture = TaleWorlds.TwoDimension.Texture;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Registers GUI\Textures\cx_exchange_bg.png as the Gauntlet sprite used as the exchange menu background.
    /// The game menu binds its background by sprite name (GameMenuWidget.Sprite="@Background"), so a sprite added to
    /// UIResourceManager.SpriteData under our name is picked up by MenuContext.SetBackgroundMeshName.
    /// The engine loads PNGs with red and blue swapped; deploy.ps1 writes a pre-swapped copy from Assets\Textures.
    /// </summary>
    public static class MenuBackground
    {
        public const string SpriteName = "cx_exchange_bg";
        private const string FileName = "cx_exchange_bg.png";

        /// <summary>Size of the vanilla menu backgrounds (e.g. wait_guards_stop); our image is the same ratio at 2x.</summary>
        private const int Width = 445;
        private const int Height = 805;

        private static bool _failed;

        /// <summary>Returns true when the sprite is available; loads it on first use. Never throws.</summary>
        public static bool EnsureRegistered()
        {
            if (_failed) return false;
            try
            {
                var sprites = UIResourceManager.SpriteData?.Sprites;
                if (sprites == null) return false;
                if (sprites.ContainsKey(SpriteName)) return true;

                string folder = Path.Combine(ModulePaths.ModuleRoot, "GUI", "Textures");
                string path = Path.Combine(folder, FileName);
                if (!File.Exists(path))
                {
                    _failed = true;
                    ExchangeLog.Warn($"Menu background not found: {path}");
                    return false;
                }

                GameTexture texture = GameTexture.LoadTextureFromPath(FileName, folder + Path.DirectorySeparatorChar);
                string source = "path";
                if (texture == null || !texture.IsValid)
                {
                    texture = GameTexture.CreateFromMemory(File.ReadAllBytes(path));
                    source = "memory";
                }
                if (texture == null || !texture.IsValid)
                {
                    _failed = true;
                    ExchangeLog.Warn($"Menu background could not be loaded as a texture: {path}");
                    return false;
                }

                var uiTexture = new UiTexture(new EngineTexture(texture));
                sprites[SpriteName] = new TextureSprite(SpriteName, uiTexture, Width, Height);
                ExchangeLog.Info($"Menu background registered ({texture.Width}x{texture.Height}, loaded from {source}).");
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                ExchangeLog.Error("Menu background registration failed", ex);
                return false;
            }
        }
    }
}
