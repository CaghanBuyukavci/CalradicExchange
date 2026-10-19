namespace CalradicExchange.Core.Pricing
{
    /// <summary>
    /// One event impulse inside an instrument's sentiment term S. Each component decays with its own half-life,
    /// so S(t) = sum of components (ECONOMY_MODEL 4, generalized to per-event half-lives).
    /// </summary>
    public sealed class ShockComponent
    {
        public double Value { get; set; }
        public double HalfLifeDays { get; set; }

        /// <summary>While set, the component does not decay (e.g. "siege:town_V1" until the siege ends).</summary>
        public string HoldKey { get; set; }

        /// <summary>Event type and target of the rule that made it (e.g. "VillageLooted:eventItem"); used by maxStack. Added in schema 5.</summary>
        public string Source { get; set; }

        /// <summary>Added since the last tick; skips decay once so the full impulse is felt on the next update.</summary>
        public bool Pending { get; set; }
    }
}
