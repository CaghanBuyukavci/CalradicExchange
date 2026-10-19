namespace CalradicExchange.Core.Model
{
    /// <summary>World events Core reacts to. Names match the keys of settings.json "eventShocks".</summary>
    public enum MarketEventType
    {
        VillageLooted,
        SiegeStarted,
        SiegeEnded,
        SettlementCaptured,
        WarDeclared,
        PeaceMade,
        SeasonChanged,
        FamineStarted,
        KingdomDestroyed,

        /// <summary>A kingdom lord was taken prisoner. News only; no shock rules by default.</summary>
        HeroCaptured,
    }
}
