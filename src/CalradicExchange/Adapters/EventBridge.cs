using System;
using CalradicExchange.Core.Model;
using CalradicExchange.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

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
            CampaignEvents.MakePeace.AddNonSerializedListener(owner, (a, b, detail) => Safe("MakePeace", () =>
            {
                sink(FromFactions(MarketEventType.PeaceMade, a, b));
                sink(FromPeaceTerms(a, b));
            }));
            CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(owner, (victim, killer, detail, notify) => Safe("BeforeHeroKilled", () => sink(FromDeath(victim, detail))));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(owner, e => Safe("MapEventEnded", () => sink(FromBattle(e))));
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(owner,
                (clan, oldKingdom, newKingdom, detail, notify) => Safe("ClanChangedKingdom", () => sink(FromClanChange(clan, oldKingdom, newKingdom, detail))));
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

        /// <summary>Towns are SettlementCaptured (towns, guilds, bonds), castles CastleCaptured (kingdom indices only). Villages follow their fief.</summary>
        private static MarketEvent FromCapture(Settlement settlement, Hero newOwner, Hero oldOwner)
        {
            if (settlement == null || !(settlement.IsTown || settlement.IsCastle)) return null;
            IFaction loser = oldOwner?.MapFaction;
            IFaction winner = newOwner?.MapFaction;
            if (loser == winner) return null; // same kingdom: a fief grant, not a capture
            if (settlement.IsCastle)
            {
                return new MarketEvent
                {
                    Type = MarketEventType.CastleCaptured,
                    SettlementId = settlement.StringId,
                    KingdomId = loser?.StringId,
                    OtherKingdomId = winner?.StringId,
                };
            }
            return new MarketEvent
            {
                Type = MarketEventType.SettlementCaptured,
                TownId = settlement.StringId,
                KingdomId = loser?.StringId,
                OtherKingdomId = winner?.StringId,
            };
        }

        /// <summary>
        /// Ruler, clan leader or plain lord of a sworn kingdom clan, read before the event changes it; null for heroes
        /// outside kingdoms and for mercenary clans.
        /// </summary>
        private static HeroRank? RankOf(Hero hero)
        {
            Clan clan = hero?.Clan;
            Kingdom kingdom = clan?.Kingdom;
            if (kingdom == null || clan.IsUnderMercenaryService || !hero.IsLord) return null;
            if (kingdom.Leader == hero) return HeroRank.Ruler;
            return clan.Leader == hero ? HeroRank.ClanLeader : HeroRank.Lord;
        }

        /// <summary>Fires before the game hands the clan (and maybe the throne) to an heir, so the rank is still right.</summary>
        private static MarketEvent FromDeath(Hero victim, KillCharacterAction.KillCharacterActionDetail detail)
        {
            HeroRank? rank = RankOf(victim);
            if (rank == null || rank == HeroRank.Lord) return null; // only rulers and clan leaders
            return new MarketEvent
            {
                Type = MarketEventType.HeroDied,
                HeroId = victim.StringId,
                KingdomId = victim.Clan.Kingdom.StringId,
                HeroRank = rank,
                Detail = detail.ToString(),
            };
        }

        /// <summary>Troops lost on both sides at which a battle counts in full (Magnitude 1).</summary>
        private const int FullBattleCasualties = 300;

        /// <summary>Smaller battles are ignored; battles below NewsCasualties move prices but are not news.</summary>
        private const int MinBattleCasualties = 20, NewsCasualties = 100;

        /// <summary>Battles between two kingdoms at war with a winner; raids are VillageLooted, captures come as owner changes.</summary>
        private static MarketEvent FromBattle(MapEvent e)
        {
            if (e == null || e.IsRaid || e.WinningSide == BattleSideEnum.None) return null;
            MapEventSide winner = e.GetMapEventSide(e.WinningSide), loser = e.GetMapEventSide(e.DefeatedSide);
            IFaction w = winner?.MapFaction, l = loser?.MapFaction;
            if (w == null || l == null || w == l || !w.IsKingdomFaction || !l.IsKingdomFaction) return null;
            if (!FactionManager.IsAtWarAgainstFaction(w, l)) return null;
            int casualties = winner.TroopCasualties + loser.TroopCasualties;
            if (casualties < MinBattleCasualties) return null;
            return new MarketEvent
            {
                Type = MarketEventType.BattleEnded,
                KingdomId = w.StringId,
                OtherKingdomId = l.StringId,
                SettlementId = e.MapEventSettlement?.StringId,
                Casualties = casualties,
                Magnitude = Math.Min(1.0, (double)casualties / FullBattleCasualties),
                Quiet = casualties < NewsCasualties,
            };
        }

        /// <summary>Peace with tribute: the payer lost the war. Peace without tribute has no winner.</summary>
        private static MarketEvent FromPeaceTerms(IFaction a, IFaction b)
        {
            if (a == null || b == null || !a.IsKingdomFaction || !b.IsKingdomFaction) return null;
            StanceLink stance = a.GetStanceWith(b);
            if (stance == null) return null;
            int aPays = stance.GetDailyTributeToPay(a), bPays = stance.GetDailyTributeToPay(b);
            if (aPays <= 0 && bPays <= 0) return null;
            bool aLost = aPays > 0;
            return new MarketEvent
            {
                Type = MarketEventType.WarEnded,
                KingdomId = (aLost ? b : a).StringId,
                OtherKingdomId = (aLost ? a : b).StringId,
                Amount = aLost ? aPays : bPays,
            };
        }

        /// <summary>Sworn clans leaving or joining a kingdom; mercenary contracts and kingdom destruction are left out.</summary>
        private static MarketEvent FromClanChange(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail)
        {
            if (clan == null || oldKingdom == newKingdom) return null;
            switch (detail)
            {
                case ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary:
                case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary:
                case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction:
                case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction:
                    return null;
            }
            bool rebellion = detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion
                || detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom;
            return new MarketEvent
            {
                Type = MarketEventType.ClanChangedKingdom,
                ClanId = clan.StringId,
                KingdomId = oldKingdom?.StringId,
                OtherKingdomId = newKingdom?.StringId,
                Detail = detail.ToString(),
                Magnitude = rebellion ? 1.6 : 1.0,
            };
        }

        /// <summary>Only lords of kingdoms make the news; companions, wanderers and minor clans are left out.</summary>
        private static MarketEvent FromPrisoner(PartyBase capturer, Hero prisoner)
        {
            if (prisoner == null || !prisoner.IsLord || prisoner.MapFaction == null || !prisoner.MapFaction.IsKingdomFaction) return null;
            return new MarketEvent
            {
                Type = MarketEventType.HeroCaptured,
                HeroRank = RankOf(prisoner),
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
