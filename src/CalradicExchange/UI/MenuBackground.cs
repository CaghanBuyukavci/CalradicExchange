namespace CalradicExchange.UI
{
    /// <summary>
    /// The exchange menu background (GUI\Textures\cx_exchange_bg.png). The game menu binds its background by sprite name
    /// (GameMenuWidget.Sprite="@Background"), so a runtime sprite under our name is picked up by
    /// MenuContext.SetBackgroundMeshName.
    /// </summary>
    public static class MenuBackground
    {
        public const string SpriteName = "cx_exchange_bg";

        /// <summary>Size of the vanilla menu backgrounds (e.g. wait_guards_stop); our image is the same ratio at 2x.</summary>
        private const int Width = 445;
        private const int Height = 805;

        /// <summary>Returns true when the sprite is available; loads it on first use. Never throws.</summary>
        public static bool EnsureRegistered() => RuntimeSprites.Get(SpriteName, Width, Height) != null;
    }
}
