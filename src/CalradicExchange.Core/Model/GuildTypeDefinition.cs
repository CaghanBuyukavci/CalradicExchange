namespace CalradicExchange.Core.Model
{
    /// <summary>A workshop type that can form guilds, from instruments.json "guildTypes" (PLAN 2.2).</summary>
    public sealed class GuildTypeDefinition
    {
        public string WorkshopTypeId { get; set; }

        /// <summary>Localization id (without braces) and English fallback of the guild name, e.g. "Smiths' Guild".</summary>
        public string NameId { get; set; }
        public string Name { get; set; }
    }
}
