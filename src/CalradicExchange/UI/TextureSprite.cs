using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace CalradicExchange.UI
{
    /// <summary>A Gauntlet sprite that shows one whole texture (UV 0..1), for images loaded at runtime instead of a sprite sheet.</summary>
    public sealed class TextureSprite : Sprite
    {
        private readonly Texture _texture;

        public TextureSprite(string name, Texture texture, int width, int height)
            : base(name, width, height, SpriteNinePatchParameters.Empty)
        {
            _texture = texture;
        }

        public override Texture Texture => _texture;

        public override Vec2 GetMinUvs() => Vec2.Zero;

        public override Vec2 GetMaxUvs() => new Vec2(1f, 1f);
    }
}
