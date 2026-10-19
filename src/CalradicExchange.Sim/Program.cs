using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CalradicExchange.Core;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;

namespace CalradicExchange.Sim
{
    /// <summary>
    /// Runs Core against synthetic event streams over several seeds, writes a CSV for the first seed
    /// and checks the ECONOMY_MODEL 8 targets. Exit code 0 = all targets pass, 1 = some fail, 2 = bad input.
    /// </summary>
    public static class Program
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static int Main(string[] args)
        {
            SimOptions o;
            InstrumentCatalog catalog;
            MarketSettings settings;
            try
            {
                o = SimOptions.Parse(args);
                string dir = o.DataDir ?? Path.Combine(AppContext.BaseDirectory, "ModuleData");
                catalog = CatalogLoader.Parse(File.ReadAllText(Path.Combine(dir, CatalogLoader.FileName)));
                settings = SettingsLoader.Parse(File.ReadAllText(Path.Combine(dir, SettingsLoader.FileName)));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException || ex is ConfigException || ex is IOException)
            {
                Console.Error.WriteLine(ex.Message);
                return 2;
            }

            int daysPerYear = CalendarInfo.Default.DaysInYear;
            double years = o.Days / (double)daysPerYear;
            Console.WriteLine($"Calradic Exchange simulator (Core {CoreInfo.Version}) - {o.Days} days ({years:0.#} years), seeds {o.Seed}..{o.Seed + (ulong)o.Seeds - 1}");

            var vol = new Dictionary<string, List<double>>();
            var corr = new Dictionary<string, List<double>>();
            var fVol = new Dictionary<string, List<double>>();
            var minR = new Dictionary<string, double>();
            var maxR = new Dictionary<string, double>();
            var bh = new List<double>();
            var rnd = new List<double>();
            var eventTotals = new Dictionary<MarketEventType, int>();
            TraderSetup[] profiles = { TraderSetup.Early, TraderSetup.Mid, TraderSetup.Late };
            var follow = profiles.ToDictionary(p => p, p => new List<double>());
            var chase = profiles.ToDictionary(p => p, p => new List<double>());
            var chaseStats = new Dictionary<MarketEventType, (int Trades, long Pnl)>();
            var lateStats = new Dictionary<MarketEventType, (int Trades, long Pnl)>();
            var earlyPerDay = new List<double>();
            var earlyByBot = new Dictionary<string, List<double>>();
            var manip = profiles.ToDictionary(p => p, p => new List<double>());
            var manipCycle = profiles.ToDictionary(p => p, p => new List<double>());

            for (int k = 0; k < o.Seeds; k++)
            {
                ulong seed = o.Seed + (ulong)k;
                SimRun baseRun = SimRun.Execute(catalog, settings, seed, o.Days);
                if (k == 0) WriteCsv(o.OutPath, baseRun);
                foreach (KeyValuePair<MarketEventType, int> e in baseRun.EventCounts)
                {
                    eventTotals.TryGetValue(e.Key, out int n);
                    eventTotals[e.Key] = n + e.Value;
                }

                foreach (KeyValuePair<string, List<PricePoint>> s in baseRun.Series)
                {
                    if (s.Value.Count < 31) continue;
                    Add(vol, s.Key, Metrics.AnnualVolatility(s.Value, daysPerYear));
                    Add(corr, s.Key, Metrics.FundamentalCorrelation(s.Value));
                    Add(fVol, s.Key, Metrics.FundamentalDailyVolatility(s.Value));
                    minR[s.Key] = Math.Min(minR.TryGetValue(s.Key, out double mn) ? mn : double.MaxValue, Metrics.MinRatio(s.Value));
                    maxR[s.Key] = Math.Max(maxR.TryGetValue(s.Key, out double mx) ? mx : 0, Metrics.MaxRatio(s.Value));
                }

                SimContext b = SimRun.Execute(catalog, settings, seed, o.Days, new BuyAndHoldStrategy()).Context;
                bh.Add(b.PeakInvested > 0 ? (double)((b.Gold - b.StartGold) / b.PeakInvested) : 0);

                SimContext r = SimRun.Execute(catalog, settings, seed, o.Days, new RandomStrategy()).Context;
                rnd.Add((r.Gold - r.StartGold) / (double)r.StartGold);

                foreach (TraderSetup p in profiles)
                {
                    follow[p].Add(RunFollower(catalog, settings, seed, o.Days, years, p, includeRaids: false, p == TraderSetup.Late ? lateStats : null));
                    chase[p].Add(RunFollower(catalog, settings, seed, o.Days, years, p, includeRaids: true, p == TraderSetup.Mid ? chaseStats : null));
                }

                foreach (TraderSetup p in profiles)
                {
                    var bot = new ManipulatorStrategy();
                    SimContext c = SimRun.Execute(catalog, settings, seed, o.Days, bot, p).Context;
                    manip[p].Add(Math.Pow(c.Gold / (double)c.StartGold, 1.0 / years) - 1.0);
                    if (bot.CycleProfits.Count > 0) manipCycle[p].Add(bot.CycleProfits.Average());
                }

                // Early-game cap: best of all bots over the first year with the early profile, in denars per day.
                IStrategy[] earlyBots = { new EventFollowerStrategy(false), new EventFollowerStrategy(true), new BuyAndHoldStrategy(), new RandomStrategy(), new ManipulatorStrategy() };
                earlyPerDay.Add(earlyBots.Max(bot =>
                {
                    SimContext c = SimRun.Execute(catalog, settings, seed, daysPerYear, bot, TraderSetup.Early).Context;
                    double perDay = (c.Gold - c.StartGold) / (double)daysPerYear;
                    string key = bot is EventFollowerStrategy ef ? ef.Name : bot.Name;
                    Add(earlyByBot, key, perDay);
                    return perDay;
                }));
            }

            PrintInstrumentTable(catalog, vol, corr, minR, maxR);
            Console.WriteLine();
            List<double> commodityFVol = catalog.Instruments.Where(i => i.Class == InstrumentClass.Commodity && fVol.ContainsKey(i.Id)).Select(i => fVol[i.Id].Average()).ToList();
            Console.WriteLine($"Fundamental daily volatility, commodities (median): {Metrics.Median(commodityFVol) * 100:0.00}%  [real game v1.4.8, 10 days: ~4.0%]");
            Console.WriteLine("Events per seed (avg): " + string.Join(", ", eventTotals.OrderBy(e => e.Key).Select(e => $"{e.Key} {e.Value / (double)o.Seeds:0.#}")));
            Console.WriteLine();
            Console.WriteLine($"Strategies (mean over {o.Seeds} seeds, min..max):");
            Console.WriteLine($"  buy-and-hold   total return on invested: {Pct(bh.Average())}  ({Pct(bh.Min())} .. {Pct(bh.Max())})");
            Console.WriteLine($"  random         total return on capital:  {Pct(rnd.Average())}  ({Pct(rnd.Min())} .. {Pct(rnd.Max())})");
            foreach (TraderSetup p in profiles)
            {
                Console.WriteLine($"  event-follower {p.Name,-26} annualized: {Pct(follow[p].Average())}  ({Pct(follow[p].Min())} .. {Pct(follow[p].Max())})"
                    + $"   raid-chaser: {Pct(chase[p].Average())}");
            }
            Console.WriteLine("  raid-chaser (mid) per event type (trades per seed x avg profit per trade, denars): " + StatsLine(chaseStats, o.Seeds));
            foreach (TraderSetup p in profiles)
            {
                Console.WriteLine($"  manipulator    {p.Name,-26} annualized: {Pct(manip[p].Average())}  ({Pct(manip[p].Min())} .. {Pct(manip[p].Max())})"
                    + $"   avg profit per {ManipulatorStrategy.RaidDays}-raid cycle: {manipCycle[p].DefaultIfEmpty(0).Average():+0;-0} denars");
            }
            Console.WriteLine("  event-follower (late) per event type: " + StatsLine(lateStats, o.Seeds));
            Console.WriteLine("  early game per bot (denars/day, mean): " + string.Join(", ", earlyByBot.Select(x => $"{x.Key} {x.Value.Average():+0.0;-0.0}")));
            Console.WriteLine($"  early game, best bot, first year: {earlyPerDay.Average():0.0} denars/day on average, worst seed {earlyPerDay.Max():0.0}");

            var checks = new List<(string Name, bool Pass, string Detail)>();
            List<string> commodities = catalog.Instruments.Where(i => i.Class == InstrumentClass.Commodity).Select(i => i.Id).ToList();
            List<string> sectors = catalog.Instruments.Where(i => i.Class == InstrumentClass.Sector).Select(i => i.Id).ToList();

            List<string> outOfBounds = minR.Keys.Where(id => minR[id] < 0.2 || maxR[id] > 5).ToList();
            checks.Add(("Prices stay within 0.2x - 5x of start", outOfBounds.Count == 0,
                $"worst min {minR.Values.Min():0.00}x, worst max {maxR.Values.Max():0.00}x" + Fails(outOfBounds)));
            checks.Add(RangeCheck("Commodity annual volatility 25% - 60%", commodities, vol, 0.25, 0.60));
            checks.Add(RangeCheck("Sector annual volatility 15% - 35%", sectors, vol, 0.15, 0.35));
            List<string> lowCorr = corr.Keys.Where(id => corr[id].Average() <= 0.65).ToList();
            checks.Add(("Correlation with target F x e^S (30d avg) > 0.65", lowCorr.Count == 0,
                $"lowest {corr.Min(c => c.Value.Average()):0.00}" + Fails(lowCorr)));
            checks.Add(("Buy-and-hold 4y return -10% .. +15%", bh.Average() >= -0.10 && bh.Average() <= 0.15, Pct(bh.Average())));
            checks.Add(("Random trading average negative", rnd.Average() < 0, Pct(rnd.Average())));
            List<double> lateFollow = follow[TraderSetup.Late];
            checks.Add(("Event-follower (late game) positive, <= 40%/year", lateFollow.Average() > 0 && lateFollow.Average() <= 0.40, Pct(lateFollow.Average())));
            checks.Add(("Early game (tier 1, trade 30) best bot <= 50 denars/day in year 1", earlyPerDay.Average() <= 50,
                $"{earlyPerDay.Average():0.0}/day (worst seed {earlyPerDay.Max():0.0})"));

            Console.WriteLine();
            Console.WriteLine("Targets (ECONOMY_MODEL 8):");
            foreach ((string name, bool pass, string detail) in checks)
            {
                Console.WriteLine($"  [{(pass ? "PASS" : "FAIL")}] {name}: {detail}");
            }
            Console.WriteLine($"CSV: {Path.GetFullPath(o.OutPath)} (seed {o.Seed})");
            return checks.All(c => c.Pass) ? 0 : 1;
        }

        private static (string, bool, string) RangeCheck(string name, List<string> ids, Dictionary<string, List<double>> vol, double lo, double hi)
        {
            List<double> means = ids.Where(vol.ContainsKey).Select(id => vol[id].Average()).ToList();
            List<string> bad = ids.Where(id => vol.ContainsKey(id) && (vol[id].Average() < lo || vol[id].Average() > hi)).ToList();
            return (name, bad.Count == 0, $"range {Pct(means.Min())} .. {Pct(means.Max())}, median {Pct(Metrics.Median(means))}" + Fails(bad));
        }

        private static void PrintInstrumentTable(InstrumentCatalog catalog, Dictionary<string, List<double>> vol, Dictionary<string, List<double>> corr,
            Dictionary<string, double> minR, Dictionary<string, double> maxR)
        {
            Console.WriteLine();
            Console.WriteLine($"{"instrument",-24}{"vol/yr",9}{"corr(F)",9}{"min x",8}{"max x",8}");
            foreach (InstrumentDefinition def in catalog.Instruments)
            {
                if (!vol.ContainsKey(def.Id)) continue;
                Console.WriteLine($"{def.Id,-24}{Pct(vol[def.Id].Average()),9}{corr[def.Id].Average(),9:0.00}{minR[def.Id],8:0.00}{maxR[def.Id],8:0.00}");
            }
        }

        private static void WriteCsv(string path, SimRun run)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var sb = new StringBuilder("day,instrument,P,F,S\n");
            foreach (KeyValuePair<string, List<PricePoint>> s in run.Series)
            {
                foreach (PricePoint p in s.Value)
                {
                    sb.Append(p.Day.ToString(Inv)).Append(',').Append(s.Key).Append(',')
                      .Append(p.P.ToString(Inv)).Append(',').Append(p.F.ToString("0.####", Inv)).Append(',')
                      .Append(p.S.ToString("0.#####", Inv)).Append('\n');
                }
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static double RunFollower(InstrumentCatalog catalog, MarketSettings settings, ulong seed, int days, double years,
            TraderSetup trader, bool includeRaids, Dictionary<MarketEventType, (int Trades, long Pnl)> stats)
        {
            var bot = new EventFollowerStrategy(includeRaids) { FinalDay = days - 1 };
            SimContext ctx = SimRun.Execute(catalog, settings, seed, days, bot, trader).Context;
            if (stats == null) return Math.Pow(ctx.Gold / (double)ctx.StartGold, 1.0 / years) - 1.0;
            foreach (KeyValuePair<MarketEventType, (int Trades, long Pnl)> st in bot.Stats)
            {
                stats.TryGetValue(st.Key, out (int Trades, long Pnl) acc);
                stats[st.Key] = (acc.Trades + st.Value.Trades, acc.Pnl + st.Value.Pnl);
            }
            return Math.Pow(ctx.Gold / (double)ctx.StartGold, 1.0 / years) - 1.0;
        }

        private static string StatsLine(Dictionary<MarketEventType, (int Trades, long Pnl)> stats, int seeds)
        {
            return string.Join(", ", stats.OrderBy(x => x.Key)
                .Select(x => $"{x.Key} {x.Value.Trades / (double)seeds:0.#} x {x.Value.Pnl / (double)Math.Max(1, x.Value.Trades):+0;-0}"));
        }

        private static void Add(Dictionary<string, List<double>> d, string key, double value)
        {
            if (!d.TryGetValue(key, out List<double> list)) d[key] = list = new List<double>();
            list.Add(value);
        }

        private static string Fails(List<string> ids) => ids.Count == 0 ? "" : " -> " + string.Join(", ", ids);

        private static string Pct(double v) => (v * 100).ToString("+0.0;-0.0", Inv) + "%";
    }
}
