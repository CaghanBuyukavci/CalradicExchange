using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CalradicExchange.Adapters;
using CalradicExchange.Behaviors;
using CalradicExchange.Core.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradicExchange.Diagnostics
{
    /// <summary>
    /// World stock of every commodity, appended to Logs/world_supply.csv after each daily tick (ADR-040): units in town
    /// markets, in village stores, carried by caravans and villagers, and held by the player, with the towns' demand and
    /// the villages' production capacity (all, and not raided). Calibration data for the stock-based commodity price.
    /// TEMPORARY: remove once the stock model is tuned.
    /// </summary>
    public static class SupplyDump
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string FilePath => Path.Combine(ModulePaths.LogsFolder, "world_supply.csv");

        public static int Append(ExchangeCampaignBehavior b)
        {
            try
            {
                var items = new List<ItemObject>();
                foreach (InstrumentDefinition def in b.Catalog.Instruments)
                {
                    if (def.Class != InstrumentClass.Commodity || def.ItemId == null) continue;
                    ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(def.ItemId);
                    if (item?.ItemCategory != null) items.Add(item);
                }

                var sb = new StringBuilder();
                if (!File.Exists(FilePath)) sb.Append("exchange_day,campaign_day,item,town_stock,village_stock,caravan_stock,villager_stock,player_stock,town_demand,capacity,capacity_now,raided_producers\n");
                string day = b.State.Market.LastTickDay.ToString(Inv);
                string campaignDay = CampaignTime.Now.ToDays.ToString("0.00", Inv);
                foreach (ItemObject item in items)
                {
                    int towns = 0, villages = 0, caravans = 0, villagers = 0, raided = 0;
                    double demand = 0, capacity = 0, capacityNow = 0;
                    foreach (Town town in Town.AllTowns)
                    {
                        if (town?.Settlement == null || !town.Settlement.IsTown) continue;
                        towns += town.Settlement.ItemRoster.GetItemNumber(item);
                        if (town.MarketData != null) demand += town.MarketData.GetDemand(item.ItemCategory);
                    }
                    foreach (Village v in Village.All)
                    {
                        if (v?.Settlement == null) continue;
                        villages += v.Settlement.ItemRoster.GetItemNumber(item);
                        double c = EventBridge.ProductionCapacity(v, item);
                        capacity += c;
                        if (v.VillageState == Village.VillageStates.Normal) capacityNow += c;
                        else if (c > 0) raided++;
                    }
                    foreach (MobileParty p in MobileParty.AllCaravanParties) caravans += p?.ItemRoster?.GetItemNumber(item) ?? 0;
                    foreach (MobileParty p in MobileParty.AllVillagerParties) villagers += p?.ItemRoster?.GetItemNumber(item) ?? 0;
                    int player = MobileParty.MainParty?.ItemRoster?.GetItemNumber(item) ?? 0;
                    sb.Append(string.Format(Inv, "{0},{1},{2},{3},{4},{5},{6},{7},{8:0.#},{9:0.##},{10:0.##},{11}\n",
                        day, campaignDay, item.StringId, towns, villages, caravans, villagers, player, demand, capacity, capacityNow, raided));
                }
                File.AppendAllText(FilePath, sb.ToString());
                return items.Count;
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Supply dump failed", ex);
                return 0;
            }
        }
    }
}
