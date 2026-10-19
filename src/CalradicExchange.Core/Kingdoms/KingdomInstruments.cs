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
                r.LastSeenDay = world.Day;
                if (!r.Listed && world.Day >= r.ListDay) r.Listed = true;
            }
            foreach (KingdomRecord r in records)
            {
                if (!alive.Contains(r.KingdomId)) r.Eliminated = true;
            }
        }

        /// <summary>
        /// F = 100 x (prosperity / base prosperity) x (fiefs / base fiefs)^fiefExponent x war x siege (ECONOMY_MODEL 2.4).
        /// war = max(warFloor, 1 - warStep x enemies), siege = max(siegeFloor, 1 - siegeStep x fiefs under siege).
        /// </summary>
        public static decimal ObservedValue(KingdomRecord record, KingdomSnapshot k, KingdomSettings s)
        {
            double floor = Math.Max(1, s.ProsperityFloor);
            double prosperity = Math.Max(k.Prosperity, floor) / Math.Max(record.BaseProsperity, floor);
            double fiefs = Math.Pow(Math.Max(k.FiefCount, 0.25) / Math.Max(record.BaseFiefs, 1), s.FiefExponent);
            double war = Math.Max(s.WarFloor, 1 - s.WarStep * k.EnemyCount);
            double siege = Math.Max(s.SiegeFloor, 1 - s.SiegeStep * k.FiefsUnderSiege);
            return (decimal)Math.Round(100 * prosperity * fiefs * war * siege, 4);
        }
    }
}
