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

        /// <summary>A castle changed kingdom (towns are SettlementCaptured). ADR-032.</summary>
        CastleCaptured,

        /// <summary>A kingdom's ruler or clan leader died. ADR-032.</summary>
        HeroDied,

        /// <summary>A battle between two kingdoms at war ended with a winner. ADR-032.</summary>
        BattleEnded,

        /// <summary>A war ended in peace with tribute: the payer lost the war. Sent with PeaceMade. ADR-032.</summary>
        WarEnded,

        /// <summary>A clan left a kingdom (defection, rebellion) or joined one. ADR-032.</summary>
        ClanChangedKingdom,
    }
}
