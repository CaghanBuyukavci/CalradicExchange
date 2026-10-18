using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Model;

namespace CalradicExchange.Sim
{
    /// <summary>
    /// Indirect manipulation (GDD 8): buy a commodity, raid its villages yourself for a few days, sell into the rise.
    /// One cycle every <see cref="CycleDays"/> days; villages need two weeks to recover anyway.
    /// </summary>
    public sealed class ManipulatorStrategy : IStrategy, IPlayerActor
    {
        public const string Target = "cx_grain";
        public const string TargetItem = "grain";
        public const int CycleDays = 21;
        public const int RaidDays = 4;
        public const int SellAfter = 8;

        /// <summary>Share of the total limit put into the position.</summary>
        private const double BudgetShare = 0.5;

        /// <summary>Position cap: beyond this, slippage eats the gain (found by sweeping sizes).</summary>
        private const double MaxBudget = 12000;

        public string Name => "manipulator";

        /// <summary>Profit of every finished cycle, in denars.</summary>
        public List<int> CycleProfits { get; } = new List<int>();

        private int _goldAtBuy;

        public IEnumerable<string> RaidsToday(SimContext ctx)
        {
            int phase = (ctx.Day - 1) % CycleDays;
            return phase >= 1 && phase <= RaidDays ? new[] { TargetItem } : Enumerable.Empty<string>();
        }

        public void OnDay(SimContext ctx, IReadOnlyList<MarketEvent> events)
        {
            int phase = (ctx.Day - 1) % CycleDays;
            if (phase == 0)
            {
                _goldAtBuy = ctx.Gold;
                ctx.BuyFor(Target, System.Math.Min(ctx.Orders.TotalLimit(ctx.Trader) * BudgetShare, MaxBudget));
            }
            else if (phase == SellAfter && ctx.Portfolio.QuantityOf(Target) > 0)
            {
                ctx.Sell(Target, ctx.Portfolio.QuantityOf(Target));
                CycleProfits.Add(ctx.Gold - _goldAtBuy);
            }
        }
    }
}
