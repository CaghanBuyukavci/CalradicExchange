using System.Collections.Generic;
using System.Linq;

namespace CalradicExchange.Core.Pricing
{
    /// <summary>Mutable market state of one instrument: P, F, S and history (ECONOMY_MODEL 1).</summary>
    public sealed class PriceState
    {
        public string InstrumentId { get; set; }

        /// <summary>False until a fundamental has been observed once (e.g. no town sells the item yet).</summary>
        public bool HasPrice { get; set; }

        /// <summary>Instrument is delisted (e.g. kingdom destroyed). Price stays at its final value.</summary>
        public bool Closed { get; set; }

        public decimal Price { get; set; }
        public decimal Fundamental { get; set; }
        public decimal InitialPrice { get; set; }

        /// <summary>Effective sentiment S after the last tick: raw shocks minus their baseline, clamped.</summary>
        public double Shock { get; set; }

        /// <summary>
        /// Slow moving average of the raw shock sum: the part of S the market already expects because such
        /// events keep happening (frequent raids, famines). Subtracted from S (ADR-019). Added in schema 3.
        /// </summary>
        public double ShockBaseline { get; set; }

        /// <summary>Log return of the last tick.</summary>
        public double LastReturn { get; set; }

        public List<ShockComponent> Shocks { get; set; } = new List<ShockComponent>();
        public List<VolBoost> VolBoosts { get; set; } = new List<VolBoost>();
        public List<PricePoint> History { get; set; } = new List<PricePoint>();

        /// <summary>Change of price over the last <paramref name="days"/> history points, as a fraction. 0 when unknown.</summary>
        public double ChangeOver(int days)
        {
            if (History.Count < 2) return 0;
            PricePoint last = History[History.Count - 1];
            PricePoint first = History[System.Math.Max(0, History.Count - 1 - days)];
            return first.P > 0 ? (double)(last.P / first.P) - 1.0 : 0;
        }

        public IEnumerable<decimal> RecentPrices(int days)
        {
            return History.Skip(System.Math.Max(0, History.Count - days)).Select(h => h.P);
        }
    }
}
