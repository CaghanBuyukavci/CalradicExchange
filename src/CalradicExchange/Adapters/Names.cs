using System.Linq;
using CalradicExchange.Core.Model;
using CalradicExchange.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CalradicExchange.Adapters
{
    /// <summary>Turns ids stored by Core back into localized display names.</summary>
    public static class Names
    {
        public static string Instrument(InstrumentDefinition def)
        {
            if (def == null) return "?";
            if (def.Class == InstrumentClass.Guild)
            {
                string guild = string.IsNullOrEmpty(def.NameId) ? def.Name : new TextObject("{=" + def.NameId + "}" + def.Name).ToString();
                return Strings.GuildName.SetTextVariable("TOWN", Settlement(def.TownId)).SetTextVariable("GUILD", guild).ToString();
            }
            if (def.Class == InstrumentClass.Kingdom)
            {
                return Strings.KingdomIndexName.SetTextVariable("KINGDOM", Kingdom(def.KingdomId)).ToString();
            }
            return string.IsNullOrEmpty(def.NameId)
                ? def.Name ?? def.Id
                : new TextObject("{=" + def.NameId + "}" + (def.Name ?? def.Id)).ToString();
        }

        /// <summary>
        /// Commodity name without the "share" word, for the commodity market table ("Grain" instead of "Grain Share").
        /// Other classes keep their full name.
        /// </summary>
        public static string ShortInstrument(InstrumentDefinition def)
        {
            if (def == null || def.Class != InstrumentClass.Commodity || string.IsNullOrEmpty(def.NameId)) return Instrument(def);
            string fallback = def.Name ?? def.Id;
            if (fallback.EndsWith(" Share")) fallback = fallback.Substring(0, fallback.Length - " Share".Length);
            return new TextObject("{=" + def.NameId + "_short}" + fallback).ToString();
        }

        /// <summary>Settlement name with its current owner in parentheses, e.g. "Pravend (Vlandia)", for the news.</summary>
        public static string SettlementWithOwner(string id)
        {
            string name = Settlement(id);
            if (string.IsNullOrEmpty(id)) return name;
            string owner = TaleWorlds.CampaignSystem.Settlements.Settlement.Find(id)?.MapFaction?.Name?.ToString();
            return string.IsNullOrEmpty(owner) ? name : Strings.NameWithOwner.SetTextVariable("NAME", name).SetTextVariable("OWNER", owner).ToString();
        }

        public static string Hero(string id)
        {
            if (string.IsNullOrEmpty(id)) return Strings.Unknown.ToString();
            return TaleWorlds.CampaignSystem.Hero.Find(id)?.Name?.ToString() ?? id;
        }

        /// <summary>Kingdom, clan or bandit faction name.</summary>
        public static string Faction(string id)
        {
            if (string.IsNullOrEmpty(id)) return Strings.Unknown.ToString();
            string kingdom = TaleWorlds.CampaignSystem.Kingdom.All.FirstOrDefault(k => k.StringId == id)?.Name?.ToString();
            return kingdom ?? TaleWorlds.CampaignSystem.Clan.All.FirstOrDefault(c => c.StringId == id)?.Name?.ToString() ?? id;
        }

        public static string Settlement(string id)
        {
            if (string.IsNullOrEmpty(id)) return Strings.Unknown.ToString();
            return TaleWorlds.CampaignSystem.Settlements.Settlement.Find(id)?.Name?.ToString() ?? id;
        }

        public static string Kingdom(string id)
        {
            if (string.IsNullOrEmpty(id)) return Strings.Unknown.ToString();
            return TaleWorlds.CampaignSystem.Kingdom.All.FirstOrDefault(k => k.StringId == id)?.Name?.ToString() ?? id;
        }

        public static string Item(string id)
        {
            if (string.IsNullOrEmpty(id)) return Strings.Unknown.ToString();
            return MBObjectManager.Instance.GetObject<ItemObject>(id)?.Name?.ToString() ?? id;
        }

        /// <summary>Item behind a commodity share, for the inquiry list icon. Null for baskets.</summary>
        public static ItemObject ItemOf(InstrumentDefinition def)
        {
            return def?.ItemId == null ? null : MBObjectManager.Instance.GetObject<ItemObject>(def.ItemId);
        }
    }
}
