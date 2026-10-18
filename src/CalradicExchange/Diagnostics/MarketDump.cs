using System;
using System.Globalization;
using System.IO;
using System.Text;
using CalradicExchange.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradicExchange.Diagnostics
{
    /// <summary>
    /// Raw per-town market data (supply, demand, stock, price factor) for every tracked item and category,
    /// appended to Logs/market_raw.csv. Calibration aid for how fundamentals are averaged (PLAN 1.15).
    /// Runs automatically for the first <see cref="AutoDumpDays"/> exchange days and via exchange.dump_market.
    /// TEMPORARY: remove the automatic dump once calibration is done.
    /// </summary>
    public static class MarketDump
    {
        public const int AutoDumpDays = 40;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string FilePath => Path.Combine(ModulePaths.LogsFolder, "market_raw.csv");

        public static int Append(ExchangeCampaignBehavior b)
        {
            try
            {
                bool header = !File.Exists(FilePath);
                var sb = new StringBuilder();
                if (header) sb.Append("exchange_day,campaign_day,town,kingdom,prosperity,under_siege,food,kind,id,value,supply,demand,in_store,in_store_value,price_factor\n");
                int rows = 0;
                string day = b.State.Market.LastTickDay.ToString(Inv);
                string campaignDay = CampaignTime.Now.ToDays.ToString("0.00", Inv);
                foreach (Town town in Town.AllTowns)
                {
                    if (town?.Settlement == null || !town.Settlement.IsTown || town.MarketData == null) continue;
                    string prefix = string.Format(Inv, "{0},{1},{2},{3},{4:0},{5},{6:0}", day, campaignDay, town.Settlement.StringId,
                        town.MapFaction?.StringId, town.Prosperity, town.IsUnderSiege ? 1 : 0, town.FoodStocks);
                    foreach (string id in b.Catalog.RequiredItemIds)
                    {
                        ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(id);
                        if (item?.ItemCategory == null) continue;
                        Row(sb, prefix, "item", id, item.Value, town.MarketData, item.ItemCategory);
                        rows++;
                    }
                    foreach (string id in b.Catalog.RequiredCategoryIds)
                    {
                        ItemCategory cat = MBObjectManager.Instance.GetObject<ItemCategory>(id);
                        if (cat == null) continue;
                        Row(sb, prefix, "category", id, 0, town.MarketData, cat);
                        rows++;
                    }
                }
                File.AppendAllText(FilePath, sb.ToString());
                return rows;
            }
            catch (Exception ex)
            {
                ExchangeLog.Error("Market dump failed", ex);
                return 0;
            }
        }

        private static void Row(StringBuilder sb, string prefix, string kind, string id, int value, TownMarketData market, ItemCategory category)
        {
            ItemData d = market.GetCategoryData(category);
            sb.Append(string.Format(Inv, "{0},{1},{2},{3},{4:0.###},{5:0.###},{6},{7},{8:0.####}\n",
                prefix, kind, id, value, d.Supply, d.Demand, d.InStore, d.InStoreValue, market.GetPriceFactor(category)));
        }
    }
}
