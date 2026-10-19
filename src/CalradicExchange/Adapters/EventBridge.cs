using System;
using CalradicExchange.Core.Model;
using CalradicExchange.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;

namespace CalradicExchange.Adapters
{
    /// <summary>
    /// Translates CampaignEvents into Core <see cref="MarketEvent"/>s (PLAN 1.16). Season changes and famine
    /// are detected by Core from the daily snapshot. The Day field is filled in by the receiver.
    /// </summary>
    public static class EventBridge
    {
        public static void Register(object owner, Action<MarketEvent> sink)
        {
            CampaignEvents.VillageLooted.AddNonSerializedListener(owner, v => Safe("VillageLooted", () => sink(FromVillage(v))));
            CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(owner, s => Safe("SiegeStarted", () => sink(FromSiege(s, MarketEventType.SiegeStarted))));
            CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(owner, s => Safe("SiegeEnded", () => sink(FromSiege(s, MarketEventType.SiegeEnded))));
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(owner,
                (settlement, openToClaim, newOwner, oldOwner, capturer, detail) => Safe("SettlementOwnerChanged", () => sink(FromCapture(settlement, newOwner, oldOwner))));
            CampaignEvents.WarDeclared.AddNonSerializedListener(owner, (a, b, detail) => Safe("WarDeclared", () => sink(FromFactions(MarketEventType.WarDeclared, a, b))));
            CampaignEvents.MakePeace.AddNonSerializedListener(owner, (a, b, detail) => Safe("MakePeace", () => sink(FromFactions(MarketEventType.PeaceMade, a, b))));
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(owner, (capturer, prisoner) => Safe("HeroPrisonerTaken", () => sink(FromPrisoner(capturer, prisoner))));
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(owner, k => Safe("KingdomDestroyed", () => sink(new MarketEvent
            {
                Type = MarketEventType.KingdomDestroyed,
                KingdomId = k?.StringId,
            })));
        }

        private static MarketEvent FromVillage(Village village)
        {
            if (village == null) return null;
            return new MarketEvent
            {
                Type = MarketEventType.VillageLooted,
                SettlementId = village.Settlement?.StringId,
                ItemId = village.VillageType?.PrimaryProduction?.StringId,
                TownId = village.Bound?.StringId,
                KingdomId = village.Settlement?.MapFaction?.StringId,
            };
        }

        private static MarketEvent FromSiege(SiegeEvent siege, MarketEventType type)
        {
            Settlement s = siege?.BesiegedSettlement;
            if (s == null || !s.IsTown) return null; // castle sieges do not move town markets
            return new MarketEvent { Type = type, TownId = s.StringId, KingdomId = s.MapFaction?.StringId };
        }

        private static MarketEvent FromCapture(Settlement settlement, Hero newOwner, Hero oldOwner)
        {
            if (settlement == null || !settlement.IsTown) return null;
            IFaction loser = oldOwner?.MapFaction;
            IFaction winner = newOwner?.MapFaction;
            if (loser == winner) return null; // same kingdom: a fief grant, not a capture
            return new MarketEvent
            {
                Type = MarketEventType.SettlementCaptured,
                TownId = settlement.StringId,
                KingdomId = loser?.StringId,
                OtherKingdomId = winner?.StringId,
            };
        }

        /// <summary>Only lords of kingdoms make the news; companions, wanderers and minor clans are left out.</summary>
        private static MarketEvent FromPrisoner(PartyBase capturer, Hero prisoner)
        {
            if (prisoner == null || !prisoner.IsLord || prisoner.MapFaction == null || !prisoner.MapFaction.IsKingdomFaction) return null;
            return new MarketEvent
            {
                Type = MarketEventType.HeroCaptured,
                HeroId = prisoner.StringId,
                KingdomId = prisoner.MapFaction.StringId,
                OtherHeroId = capturer?.LeaderHero?.StringId,
                OtherKingdomId = capturer?.MapFaction?.StringId,
            };
        }

        private static MarketEvent FromFactions(MarketEventType type, IFaction a, IFaction b)
        {
            if (a == null || b == null || !a.IsKingdomFaction || !b.IsKingdomFaction) return null; // bandits and minor clans
            return new MarketEvent { Type = type, KingdomId = a.StringId, OtherKingdomId = b.StringId };
        }

        private static void Safe(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ExchangeLog.Error($"EventBridge: {name} handler failed", ex);
            }
        }
    }
}
