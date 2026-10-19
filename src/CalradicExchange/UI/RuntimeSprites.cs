using System;
using System.Collections.Generic;
using System.IO;
using CalradicExchange.Diagnostics;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.TwoDimension;
using GameTexture = TaleWorlds.Engine.Texture;
using UiTexture = TaleWorlds.TwoDimension.Texture;

namespace CalradicExchange.UI
{
    /// <summary>
    /// Loads PNGs from Modules\CalradicExchange\GUI\Textures at runtime and adds them to UIResourceManager.SpriteData
    /// under their file name without extension (ADR-023). deploy.ps1 writes them as 32-bit RGBA; a 24-bit PNG came out
    /// with red and blue swapped.
    /// </summary>
    public static class RuntimeSprites
    {
        private static readonly HashSet<string> Failed = new HashSet<string>();

        /// <summary>Returns the sprite, loading it on first use; null when it cannot be loaded. Never throws.</summary>
        public static Sprite Get(string name, int width, int height)
        {
            if (Failed.Contains(name)) return null;
            try
            {
                var sprites = UIResourceManager.SpriteData?.Sprites;
                if (sprites == null) return null;
                if (sprites.TryGetValue(name, out Sprite existing)) return existing;

                string fileName = name + ".png";
                string folder = Path.Combine(ModulePaths.ModuleRoot, "GUI", "Textures");
                string path = Path.Combine(folder, fileName);
                if (!File.Exists(path))
                {
                    Failed.Add(name);
                    ExchangeLog.Warn($"Texture not found: {path}");
                    return null;
                }

                GameTexture texture = GameTexture.LoadTextureFromPath(fileName, folder + Path.DirectorySeparatorChar);
                string source = "path";
                if (texture == null || !texture.IsValid)
                {
                    texture = GameTexture.CreateFromMemory(File.ReadAllBytes(path));
                    source = "memory";
                }
                if (texture == null || !texture.IsValid)
                {
                    Failed.Add(name);
                    ExchangeLog.Warn($"Could not load texture: {path}");
                    return null;
                }

                var sprite = new TextureSprite(name, new UiTexture(new EngineTexture(texture)), width, height);
                sprites[name] = sprite;
                ExchangeLog.Info($"Sprite {name} registered ({texture.Width}x{texture.Height}, loaded from {source}).");
                return sprite;
            }
            catch (Exception ex)
            {
                Failed.Add(name);
                ExchangeLog.Error($"Sprite {name} registration failed", ex);
                return null;
            }
        }
    }
}
