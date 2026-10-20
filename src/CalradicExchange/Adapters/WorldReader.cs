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

        /// <summary>Workshop readings of the previous Read, for the daily net profit (not saved; ADR-027).</summary>
        private Dictionary<Workshop, WorkshopReading> _previous = new Dictionary<Workshop, WorkshopReading>();

        /// <summary>Totals of the last Read over all workshops, for the daily log (measure check).</summary>
        public double LastNetTotal { get; private set; }
        public double LastIncomeTotal { get; private set; }
        public int LastMeasured { get; private set; }
        public int LastUnmeasured { get; private set; }

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

            LastNetTotal = LastIncomeTotal = 0;
            LastMeasured = LastUnmeasured = 0;
            var current = new Dictionary<Workshop, WorkshopReading>();
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
                ReadWorkshops(town, snap, current);
                world.Towns.Add(snap);
            }

            _previous = current;

            List<Kingdom> alive = Kingdom.All.Where(k => k != null && !k.IsEliminated).ToList();
            world.KingdomCount = alive.Count;
            for (int i = 0; i < alive.Count; i++)
            {
                for (int j = i + 1; j < alive.Count; j++)
                {
                    if (FactionManager.IsAtWarAgainstFaction(alive[i], alive[j])) world.ActiveWarPairs++;
                }
            }
            foreach (Kingdom k in alive) world.Kingdoms.Add(ReadKingdom(k, alive));
            return world;
        }

        /// <summary>
        /// One living kingdom (PLAN 2.4, ADR-032): towns and castles with their prosperity and sieges, villages with
        /// their hearths and raids, military strength, sworn clans and enemies.
        /// </summary>
        private static KingdomSnapshot ReadKingdom(Kingdom k, List<Kingdom> alive)
        {
            var snap = new KingdomSnapshot { Id = k.StringId };
            foreach (Town fief in k.Fiefs)
            {
                if (fief == null) continue;
                snap.FiefCount++;
                if (fief.Settlement != null && fief.Settlement.IsCastle) snap.Castles++;
                else snap.Towns++;
                snap.Prosperity += Math.Max(0, fief.Prosperity);
                if (fief.IsUnderSiege) snap.FiefsUnderSiege++;
            }
            foreach (Village v in k.Villages)
            {
                if (v == null) continue;
                snap.Villages++;
                snap.VillageHearth += Math.Max(0, v.Hearth);
                if (v.VillageState == Village.VillageStates.Looted || v.VillageState == Village.VillageStates.BeingRaided) snap.VillagesRaided++;
            }
            snap.Strength = Math.Max(0, k.CurrentTotalStrength);
            snap.Clans = k.Clans.Count(c => c != null && !c.IsEliminated && !c.IsUnderMercenaryService);
            foreach (Kingdom other in alive)
            {
                if (other != k && FactionManager.IsAtWarAgainstFaction(k, other)) snap.EnemyCount++;
            }
            return snap;
        }

        /// <summary>
        /// Groups the town's workshops by type (PLAN 2.1). Net profit per workshop since the previous reading
        /// (ADR-027) = capital change + what the owner withdrew. The owner withdraws a fifth of the profit above the
        /// initial capital each day (ClanFinanceModel.CalculateOwnerIncomeFromWorkshop); the amount computed at the
        /// previous reading stands in for it. A workshop is not measured on its first reading or after its owner or
        /// type changed (both reset the capital). Empty slots and hidden types (artisans) are skipped.
        /// </summary>
        private void ReadWorkshops(Town town, TownSnapshot snap, Dictionary<Workshop, WorkshopReading> current)
        {
            Workshop[] workshops = town.Workshops;
            if (workshops == null) return;
            ClanFinanceModel finance = Campaign.Current?.Models?.ClanFinanceModel;
            var measured = new HashSet<string>();
            foreach (Workshop w in workshops)
            {
                WorkshopType type = w?.WorkshopType;
                if (type == null || type.IsHidden || string.IsNullOrEmpty(type.StringId)) continue;
                if (!snap.Workshops.TryGetValue(type.StringId, out WorkshopGroupSnapshot group))
                {
                    group = new WorkshopGroupSnapshot { HasProfit = false };
                    snap.Workshops[type.StringId] = group;
                }
                group.Count++;

                var now = new WorkshopReading(type.StringId, w.Owner, w.Capital,
                    finance != null ? Math.Max(0, finance.CalculateOwnerIncomeFromWorkshop(w)) : 0);
                current[w] = now;
                LastIncomeTotal += now.OwnerIncome;
                if (_previous.TryGetValue(w, out WorkshopReading before) && before.TypeId == now.TypeId && before.Owner == now.Owner)
                {
                    group.DailyProfit += now.Capital - before.Capital + before.OwnerIncome;
                    LastNetTotal += now.Capital - before.Capital + before.OwnerIncome;
                    measured.Add(type.StringId);
                    LastMeasured++;
                }
                else
                {
                    LastUnmeasured++;
                }
            }
            foreach (string typeId in measured) snap.Workshops[typeId].HasProfit = true;
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
