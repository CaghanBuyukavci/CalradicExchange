using System;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>
    /// Limited share supply (ECONOMY_MODEL 5.1, ADR-037): the fewer shares are left in free float, the more an order
    /// moves the price. Free float = issued - investors' holding - player's holding.
    /// </summary>
    public static class Liquidity
    {
        /// <summary>Classes the town's investors trade. Guild partnerships are only traded by the player.</summary>
        public static bool TradedByInvestors(InstrumentClass cls)
        {
            return cls == InstrumentClass.Commodity || cls == InstrumentClass.Sector || cls == InstrumentClass.Kingdom;
        }

        /// <summary>Shares issued at listing: depth x supplyDepthMultiple worth of shares at the listing price.</summary>
        public static int SupplyFor(ClassParameters cp, NpcSettings npc, decimal listingPrice)
        {
            if (listingPrice <= 0) return 0;
            double shares = cp.Depth * npc.SupplyDepthMultiple / (double)listingPrice;
            return (int)Math.Max(1, Math.Min(int.MaxValue / 4, Math.Round(shares)));
        }

        /// <summary>Free float share at listing, the level at which the multiplier is 1.</summary>
        public static double ReferenceFreeFraction(InstrumentClass cls, NpcSettings npc)
        {
            return TradedByInvestors(cls) ? 1 - npc.InitialHoldingFraction : 1;
        }

        /// <summary>
        /// Impact multiplier for an order taking <paramref name="taken"/> shares out of free float (negative: a sell
        /// putting them back). Uses the free float halfway through the order, compared with the listing level, and
        /// is clamped to [impactMultiplierMin, impactMultiplierMax]. 1 when the supply is unknown.
        /// </summary>
        public static double ImpactMultiplier(int total, int investorShares, int playerShares, int taken, double referenceFree, NpcSettings npc)
        {
            if (total <= 0 || npc == null) return 1;
            double midFree = (total - investorShares - playerShares - taken / 2.0) / total;
            double m = midFree <= 1e-9 ? npc.ImpactMultiplierMax : referenceFree / midFree;
            return Math.Max(npc.ImpactMultiplierMin, Math.Min(npc.ImpactMultiplierMax, m));
        }
    }
}
