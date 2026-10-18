using System.Collections.Generic;

namespace CalradicExchange.Core.Model
{
    /// <summary>Static description of a tradable instrument, built from instruments.json.</summary>
    public sealed class InstrumentDefinition
    {
        public string Id { get; set; }
        public InstrumentClass Class { get; set; }

        /// <summary>Localization id (without braces) and English fallback name.</summary>
        public string NameId { get; set; }
        public string Name { get; set; }

        /// <summary>Commodity only: the game item the share tracks.</summary>
        public string ItemId { get; set; }

        /// <summary>Sector only: item basket (normalized fundamentals).</summary>
        public List<ItemWeight> ItemComponents { get; set; } = new List<ItemWeight>();

        /// <summary>Sector only: item category basket (price factors), e.g. Military Supply.</summary>
        public List<CategoryWeight> CategoryComponents { get; set; } = new List<CategoryWeight>();

        /// <summary>Category sectors: weight of active wars in the fundamental (ECONOMY_MODEL 2.2).</summary>
        public double WarFactor { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>Guilds and town bonds (Phase 2/3): the town this instrument belongs to.</summary>
        public string TownId { get; set; }

        /// <summary>Kingdom indices and war bonds (Phase 2/3).</summary>
        public string KingdomId { get; set; }

        public bool HasTag(string tag)
        {
            return Tags != null && Tags.Contains(tag);
        }
    }
}
