using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradicExchange.Adapters
{
    /// <summary>
    /// Copies the parts of the campaign Core needs into a <see cref="WorldSnapshot"/> (PLAN 1.15).
    /// Item price = item base value x town price factor of its category (ADR-009); weight = supply + stock (ADR-017).
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
                        if (factor <= 0) continue;
                        snap.ItemPrices[item.StringId] = (decimal)(item.Value * factor);
                        snap.ItemVolumes[item.StringId] = Volume(market, item.ItemCategory);
                    }
                    foreach (ItemCategory cat in _categories)
                    {
                        if (!HasMarket(market, cat)) continue;
                        float factor = market.GetPriceFactor(cat);
                        if (factor <= 0) continue;
                        snap.CategoryFactors[cat.StringId] = factor;
                        snap.CategoryVolumes[cat.StringId] = Volume(market, cat);
                    }
                }
                ReadWorkshops(town, snap);
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
        /// Groups the town's workshops by type (PLAN 2.1). Profit = the owner's daily income from the finance model
        /// (ADR-010): owners withdraw a fifth of the accumulated profit each day, so this tracks the daily profit.
        /// Empty slots and hidden types (artisans) are skipped.
        /// </summary>
        private static void ReadWorkshops(Town town, TownSnapshot snap)
        {
            Workshop[] workshops = town.Workshops;
            if (workshops == null) return;
            ClanFinanceModel finance = Campaign.Current?.Models?.ClanFinanceModel;
            foreach (Workshop w in workshops)
            {
                WorkshopType type = w?.WorkshopType;
                if (type == null || type.IsHidden || string.IsNullOrEmpty(type.StringId)) continue;
                if (!snap.Workshops.TryGetValue(type.StringId, out WorkshopGroupSnapshot group))
                {
                    group = new WorkshopGroupSnapshot();
                    snap.Workshops[type.StringId] = group;
                }
                group.Count++;
                group.DailyProfit += finance != null ? Math.Max(0, finance.CalculateOwnerIncomeFromWorkshop(w)) : 0;
            }
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

        /// <summary>Averaging weight (ADR-017): daily supply plus units in store.</summary>
        private static double Volume(TownMarketData market, ItemCategory category)
        {
            ItemData data = market.GetCategoryData(category);
            return Math.Max(0, data.Supply) + Math.Max(0, data.InStore);
        }
    }
}
