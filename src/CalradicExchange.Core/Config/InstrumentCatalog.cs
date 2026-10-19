using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Config
{
    /// <summary>All instrument definitions, in a fixed order (the order of instruments.json). Order matters for determinism.</summary>
    public sealed class InstrumentCatalog
    {
        private readonly Dictionary<string, InstrumentDefinition> _byId;

        public InstrumentCatalog(int unitsPerShare, IEnumerable<InstrumentDefinition> instruments, IEnumerable<GuildTypeDefinition> guildTypes)
        {
            UnitsPerShare = unitsPerShare;
            Instruments = instruments.ToList();
            GuildTypes = guildTypes.ToList();
            GuildWorkshopTypes = GuildTypes.Select(g => g.WorkshopTypeId).ToList();
            _byId = Instruments.ToDictionary(i => i.Id);

            var items = new List<string>();
            var categories = new List<string>();
            foreach (InstrumentDefinition def in Instruments)
            {
                if (def.ItemId != null && !items.Contains(def.ItemId)) items.Add(def.ItemId);
                foreach (ItemWeight c in def.ItemComponents)
                {
                    if (!items.Contains(c.ItemId)) items.Add(c.ItemId);
                }
                foreach (CategoryWeight c in def.CategoryComponents)
                {
                    if (!categories.Contains(c.CategoryId)) categories.Add(c.CategoryId);
                }
            }
            RequiredItemIds = items;
            RequiredCategoryIds = categories;
        }

        /// <summary>Game units represented by one commodity share (ECONOMY_MODEL 2.1).</summary>
        public int UnitsPerShare { get; }

        public IReadOnlyList<InstrumentDefinition> Instruments { get; }

        /// <summary>Workshop types eligible for guild shares (PLAN 2.2), in file order.</summary>
        public IReadOnlyList<GuildTypeDefinition> GuildTypes { get; }

        /// <summary>Ids of <see cref="GuildTypes"/>.</summary>
        public IReadOnlyList<string> GuildWorkshopTypes { get; }

        public GuildTypeDefinition FindGuildType(string workshopTypeId)
        {
            return GuildTypes.FirstOrDefault(g => g.WorkshopTypeId == workshopTypeId);
        }

        /// <summary>Item ids whose town prices the game layer must put in the snapshot.</summary>
        public IReadOnlyList<string> RequiredItemIds { get; }

        /// <summary>Category ids whose price factors the game layer must put in the snapshot.</summary>
        public IReadOnlyList<string> RequiredCategoryIds { get; }

        public InstrumentDefinition Find(string id)
        {
            return id != null && _byId.TryGetValue(id, out InstrumentDefinition def) ? def : null;
        }
    }
}
