using System;
using System.Collections.Generic;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>
    /// Fundamental values F from a world snapshot (ECONOMY_MODEL 2.1, 2.2). Pure functions.
    /// Town averages are weighted by market volume (supply + stock, ADR-017); when no town reports any
    /// volume for an item the prosperity weighting is used instead.
    /// </summary>
    public static class FundamentalCalculator
    {
        /// <summary>Volume-weighted average unit price of an item over towns not under siege. Null when no town quotes it.</summary>
        public static decimal? AverageItemPrice(WorldSnapshot world, string itemId)
        {
            double? avg = Average(world, itemId,
                t => t.ItemPrices != null && t.ItemPrices.TryGetValue(itemId, out decimal p) ? (double)p : 0,
                t => t.ItemVolumes);
            return avg == null ? (decimal?)null : (decimal)avg.Value;
        }

        /// <summary>Volume-weighted average price factor of an item category. Null when unknown.</summary>
        public static double? AverageCategoryFactor(WorldSnapshot world, string categoryId)
        {
            return Average(world, categoryId,
                t => t.CategoryFactors != null && t.CategoryFactors.TryGetValue(categoryId, out double f) ? f : 0,
                t => t.CategoryVolumes);
        }

        private static double? Average(WorldSnapshot world, string id, Func<TownSnapshot, double> valueOf,
            Func<TownSnapshot, Dictionary<string, double>> volumesOf)
        {
            double weighted = 0, weights = 0, pWeighted = 0, pWeights = 0;
            foreach (TownSnapshot town in world.Towns)
            {
                if (town.IsUnderSiege) continue;
                double value = valueOf(town);
                if (value <= 0) continue;

                double pw = Math.Max(1.0, town.Prosperity);
                pWeighted += pw * value;
                pWeights += pw;

                Dictionary<string, double> volumes = volumesOf(town);
                if (volumes != null && volumes.TryGetValue(id, out double vw) && vw > 0)
                {
                    weighted += vw * value;
                    weights += vw;
                }
            }
            if (weights > 0) return weighted / weights;
            if (pWeights > 0) return pWeighted / pWeights;
            return null;
        }

        /// <summary>Commodity share: units per share x average price.</summary>
        public static decimal? Commodity(WorldSnapshot world, string itemId, int unitsPerShare)
        {
            decimal? avg = AverageItemPrice(world, itemId);
            return avg * unitsPerShare;
        }

        /// <summary>
        /// Sector basket: 100 x sum(w_i x F_i / F_i,0). Components without data are left out and the remaining
        /// weights are renormalized. Category baskets also apply the war multiplier.
        /// </summary>
        public static decimal? Sector(WorldSnapshot world, InstrumentDefinition def, MarketState state)
        {
            double sum = 0, weights = 0;
            foreach (ItemWeight c in def.ItemComponents)
            {
                decimal? avg = AverageItemPrice(world, c.ItemId);
                if (avg == null || !state.BaseItemPrices.TryGetValue(c.ItemId, out decimal baseline) || baseline <= 0) continue;
                sum += c.Weight * (double)(avg.Value / baseline);
                weights += c.Weight;
            }
            foreach (CategoryWeight c in def.CategoryComponents)
            {
                double? avg = AverageCategoryFactor(world, c.CategoryId);
                if (avg == null || !state.BaseCategoryFactors.TryGetValue(c.CategoryId, out double baseline) || baseline <= 0) continue;
                sum += c.Weight * (avg.Value / baseline);
                weights += c.Weight;
            }
            if (weights <= 0) return null;

            double value = 100.0 * sum / weights;
            if (def.CategoryComponents.Count > 0 && def.WarFactor > 0)
            {
                value *= 1.0 + def.WarFactor * WarIntensity(world);
            }
            return (decimal)value;
        }

        /// <summary>Active war pairs per kingdom (ECONOMY_MODEL 2.2 "aktif_savaş_sayısı_normalize").</summary>
        public static double WarIntensity(WorldSnapshot world)
        {
            return world.KingdomCount > 0 ? (double)world.ActiveWarPairs / world.KingdomCount : 0;
        }
    }
}
