using System;
using System.Collections.Generic;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Kingdoms
{
    /// <summary>Kingdom indices as instruments (PLAN 2.4, ADR-030): ids, tracking, definitions and the observed value.</summary>
    public static class KingdomInstruments
    {
        public const string IdPrefix = "cx_kingdom_";

        public static string IdFor(string kingdomId) => IdPrefix + kingdomId;

        public static bool IsKingdomId(string instrumentId) => instrumentId != null && instrumentId.StartsWith(IdPrefix, StringComparison.Ordinal);

        public static InstrumentDefinition Define(KingdomRecord record)
        {
            return new InstrumentDefinition
            {
                Id = IdFor(record.KingdomId),
                Class = InstrumentClass.Kingdom,
                Name = record.KingdomId,
                KingdomId = record.KingdomId,
                Tags = { "kingdom" },
            };
        }

        /// <summary>
        /// Updates the records from today's living kingdoms. New kingdoms get today's values as their base; kingdoms
        /// missing from the snapshot have fallen. A snapshot without any kingdom (reader failure, simulator) changes nothing.
        /// </summary>
        public static void Track(List<KingdomRecord> records, WorldSnapshot world, KingdomSettings settings)
        {
            if (world.Kingdoms == null || world.Kingdoms.Count == 0) return;
            bool firstBatch = records.Count == 0;
            var alive = new HashSet<string>();
            foreach (KingdomSnapshot k in world.Kingdoms)
            {
                if (string.IsNullOrEmpty(k.Id)) continue;
                alive.Add(k.Id);
                KingdomRecord r = records.Find(x => x.KingdomId == k.Id);
                if (r == null)
                {
                    r = new KingdomRecord
                    {
                        KingdomId = k.Id,
                        FirstSeenDay = world.Day,
                        ListDay = firstBatch ? world.Day : world.Day + Math.Max(0, settings.MinListingDays),
                        BaseProsperity = k.Prosperity,
                        BaseFiefs = k.FiefCount,
                    };
                    records.Add(r);
                }
                SetMissingBases(r, k, settings);
                r.LastSeenDay = world.Day;
                if (!r.Listed && world.Day >= r.ListDay) r.Listed = true;
            }
            foreach (KingdomRecord r in records)
            {
                if (!alive.Contains(r.KingdomId)) r.Eliminated = true;
            }
        }

        /// <summary>
        /// Bases of the parts added in schema 12 are taken from the first reading that has them. For a kingdom tracked
        /// before, the weighted fief base is scaled from the old fief count base, so fiefs lost since then still count.
        /// </summary>
        private static void SetMissingBases(KingdomRecord r, KingdomSnapshot k, KingdomSettings s)
        {
            if (r.BaseFiefScore <= 0)
            {
                double score = FiefScore(k, s);
                r.BaseFiefScore = r.BaseFiefs > 0 && k.FiefCount > 0 ? score / k.FiefCount * r.BaseFiefs : score;
            }
            if (r.BaseHearth <= 0) r.BaseHearth = k.VillageHearth;
            if (r.BaseStrength <= 0) r.BaseStrength = k.Strength;
            if (r.BaseClans <= 0) r.BaseClans = k.Clans;
        }

        /// <summary>Towns x townWeight + castles x castleWeight; the plain fief count when towns and castles are not given.</summary>
        public static double FiefScore(KingdomSnapshot k, KingdomSettings s)
        {
            if (k.Towns + k.Castles == 0) return k.FiefCount * s.CastleWeight;
            return k.Towns * s.TownWeight + k.Castles * s.CastleWeight;
        }

        /// <summary>
        /// F = 100 x prosperity^a x fiefs^b x villages^c x strength^d x clans^e x war x siege x raids (ECONOMY_MODEL 2.4,
        /// ADR-032). Each ratio is today's value over the kingdom's base; a part with no base (0) counts as 1.
        /// war = max(warFloor, 1 - warStep x enemies), siege = max(siegeFloor, 1 - siegeStep x fiefs under siege),
        /// raids = max(raidFloor, 1 - raidStep x villages looted or being raided).
        /// </summary>
        public static decimal ObservedValue(KingdomRecord record, KingdomSnapshot k, KingdomSettings s)
        {
            double floor = Math.Max(1, s.ProsperityFloor);
            double prosperity = Math.Pow(Math.Max(k.Prosperity, floor) / Math.Max(record.BaseProsperity, floor), s.ProsperityExponent);
            double fiefBase = record.BaseFiefScore > 0 ? record.BaseFiefScore : Math.Max(record.BaseFiefs, 1) * s.CastleWeight;
            double fiefs = Math.Pow(Math.Max(FiefScore(k, s), 0.25 * s.CastleWeight) / Math.Max(fiefBase, 1e-9), s.FiefExponent);
            double villages = Part(k.VillageHearth, record.BaseHearth, 0.1, 1e9, s.VillageExponent);
            double strength = Part(k.Strength, record.BaseStrength, s.StrengthRatioMin, s.StrengthRatioMax, s.StrengthExponent);
            double clans = Part(k.Clans, record.BaseClans, 0.1, 1e9, s.ClanExponent);
            double war = Math.Max(s.WarFloor, 1 - s.WarStep * k.EnemyCount);
            double siege = Math.Max(s.SiegeFloor, 1 - s.SiegeStep * k.FiefsUnderSiege);
            double raids = Math.Max(s.RaidFloor, 1 - s.RaidStep * k.VillagesRaided);
            return (decimal)Math.Round(100 * prosperity * fiefs * villages * strength * clans * war * siege * raids, 4);
        }

        /// <summary>(value / base) clamped to [min, max], to the given power; 1 when there is no base.</summary>
        private static double Part(double value, double baseValue, double min, double max, double exponent)
        {
            if (baseValue <= 0 || exponent == 0) return 1;
            double ratio = Math.Min(max, Math.Max(min, value / baseValue));
            return Math.Pow(ratio, exponent);
        }
    }
}
