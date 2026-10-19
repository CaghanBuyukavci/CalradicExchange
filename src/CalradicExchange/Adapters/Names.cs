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
            return string.IsNullOrEmpty(def.NameId)
                ? def.Name ?? def.Id
                : new TextObject("{=" + def.NameId + "}" + (def.Name ?? def.Id)).ToString();
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
