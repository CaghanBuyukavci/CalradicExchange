using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradicExchange.Adapters
{
    /// <summary>
    /// Copies the parts of the campaign Core needs into a <see cref="WorldSnapshot"/> (PLAN 1.15).
    /// Item price = item base value x town price factor of its category (ADR-009).
    /// </summary>
    public sealed class WorldReader
    {
        private readonly List<ItemObject> _items = new List<ItemObject>();
        private readonly List<ItemCategory> _categories = new List<ItemCategory>();

        public WorldReader(InstrumentCatalog catalog)
        {
            foreach (string id in catalog.RequiredItemIds)
            {
                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                if (item == null || item.ItemCategory == null) ExchangeLog.Warn($"WorldReader: item '{id}' not found in this game; instruments using it get no data.");
                else _items.Add(item);
            }
            foreach (string id in catalog.RequiredCategoryIds)
            {
                ItemCategory cat = MBObjectManager.Instance.GetObject<ItemCategory>(id);
                if (cat == null) ExchangeLog.Warn($"WorldReader: item category '{id}' not found in this game.");
                else _categories.Add(cat);
            }
            ExchangeLog.Info($"WorldReader: tracking {_items.Count} items and {_categories.Count} categories.");
        }

        public WorldSnapshot Read(int exchangeDay)
        {
            var world = new WorldSnapshot
            {
                Day = exchangeDay,
                Calendar = new CalendarInfo(CampaignTime.DaysInWeek, CampaignTime.DaysInSeason, CampaignTime.DaysInYear),
                Season = (Season)(int)CampaignTime.Now.GetSeasonOfYear,
            };

            foreach (Town town in Town.AllTowns)
            {
                if (town?.Settlement == null || !town.Settlement.IsTown) continue; // castles are Towns too
                TownMarketData market = town.MarketData;
                var snap = new TownSnapshot
                {
                    Id = town.Settlement.StringId,
                    KingdomId = town.MapFaction?.StringId,
                    Prosperity = town.Prosperity,
                    IsUnderSiege = town.IsUnderSiege,
                    FoodStocks = town.FoodStocks,
                };
                if (market != null)
                {
                    foreach (ItemObject item in _items)
                    {
                        if (!HasMarket(market, item.ItemCategory)) continue;
                        float factor = market.GetPriceFactor(item.ItemCategory);
                        if (factor > 0) snap.ItemPrices[item.StringId] = (decimal)(item.Value * factor);
                    }
                    foreach (ItemCategory cat in _categories)
                    {
                        if (!HasMarket(market, cat)) continue;
                        float factor = market.GetPriceFactor(cat);
                        if (factor > 0) snap.CategoryFactors[cat.StringId] = factor;
                    }
                }
                world.Towns.Add(snap);
            }

            List<Kingdom> alive = Kingdom.All.Where(k => k != null && !k.IsEliminated).ToList();
            world.KingdomCount = alive.Count;
            for (int i = 0; i < alive.Count; i++)
            {
                for (int j = i + 1; j < alive.Count; j++)
                {
                    if (FactionManager.IsAtWarAgainstFaction(alive[i], alive[j])) world.ActiveWarPairs++;
                }
            }
            return world;
        }

        /// <summary>
        /// A town without any supply/demand record for a category would report the 0.1 floor factor;
        /// leave such towns out of the average instead.
        /// </summary>
        private static bool HasMarket(TownMarketData market, ItemCategory category)
        {
            ItemData data = market.GetCategoryData(category);
            return data.Demand > 0 || data.Supply > 0;
        }
    }
}
