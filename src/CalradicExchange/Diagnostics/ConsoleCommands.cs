using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CalradicExchange.Behaviors;
using CalradicExchange.Core;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Trading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace CalradicExchange.Diagnostics
{
    /// <summary>
    /// Developer console commands, invoked as "exchange.&lt;name&gt;" (console: Alt + " on a Turkish keyboard).
    /// Discovered automatically by CommandLineFunctionality (it scans every assembly that references TaleWorlds.Library).
    /// </summary>
    public static class ConsoleCommands
    {
        private const string Group = "exchange";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [CommandLineFunctionality.CommandLineArgumentFunction("ping", Group)]
        public static string Ping(List<string> args)
        {
            return Run("ping", args, () =>
            {
                ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
                string state = b?.State == null
                    ? "no campaign state"
                    : $"exchange day {b.State.Market.LastTickDay}, ready {b.IsReady}, positions {b.State.Portfolio.Positions.Count}, schema {b.State.SchemaVersion}";
                return $"pong - Calradic Exchange {CoreInfo.Version}; {state}; log={ExchangeLog.FilePath}";
            });
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("dump_prices", Group)]
        public static string DumpPrices(List<string> args)
        {
            return RunReady("dump_prices", args, b =>
            {
                var sb = new StringBuilder($"Exchange day {b.State.Market.LastTickDay}\n");
                sb.AppendLine("id                       P          F          S        1d       7d");
                foreach (InstrumentDefinition def in b.Catalog.Instruments)
                {
                    PriceState ps = b.Market.Get(def.Id);
                    if (ps == null || !ps.HasPrice) { sb.AppendLine($"{def.Id,-22} (no price)"); continue; }
                    sb.AppendLine(string.Format(Inv, "{0,-22} {1,10:0.00} {2,10:0.00} {3,8:+0.000;-0.000} {4,8:+0.0%;-0.0%} {5,8:+0.0%;-0.0%}{6}",
                        def.Id, ps.Price, ps.Fundamental, ps.Shock, ps.ChangeOver(1), ps.ChangeOver(7), ps.Closed ? " CLOSED" : ""));
                }
                return sb.ToString();
            });
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("dump_portfolio", Group)]
        public static string DumpPortfolio(List<string> args)
        {
            return RunReady("dump_portfolio", args, b =>
            {
                Portfolio pf = b.Portfolio;
                TraderProfile t = b.CurrentTrader();
                var sb = new StringBuilder();
                sb.AppendLine($"gold {t.Gold}, clan tier {t.ClanTier}, trade {t.TradeSkill}, limit {b.Orders.TotalLimit(t)}, fee {b.Orders.CommissionRate(t.TradeSkill):P2}");
                foreach (Position p in pf.Positions)
                {
                    decimal price = b.Market.Get(p.InstrumentId)?.Price ?? 0;
                    sb.AppendLine(string.Format(Inv, "{0,-22} qty {1,6}  avg {2,9:0.00}  now {3,9:0.00}  open {4,9:+0;-0}",
                        p.InstrumentId, p.Quantity, p.AverageCost, price, p.Quantity * price - p.CostBasis));
                }
                sb.AppendLine(string.Format(Inv, "realized {0:+0;-0}, fees {1}, taxes {2}, trades {3}", pf.RealizedPnl, pf.TotalCommissionPaid, pf.TotalProfitTaxPaid, pf.TradeCount));
                return sb.ToString();
            });
        }

        /// <summary>
        /// exchange.fire_event VillageLooted &lt;itemId&gt; [townId] | SiegeStarted|SiegeEnded|FamineStarted &lt;townId&gt;
        /// | SettlementCaptured &lt;townId&gt; &lt;loserKingdom&gt; &lt;winnerKingdom&gt; | WarDeclared|PeaceMade &lt;k1&gt; &lt;k2&gt;
        /// | SeasonChanged &lt;Spring|Summer|Autumn|Winter&gt; | KingdomDestroyed &lt;kingdom&gt;
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("fire_event", Group)]
        public static string FireEvent(List<string> args)
        {
            return RunReady("fire_event", args, b =>
            {
                if (args.Count < 1 || !Enum.TryParse(args[0], true, out MarketEventType type))
                {
                    return "Usage: exchange.fire_event <type> <args>. Types: " + string.Join(", ", Enum.GetNames(typeof(MarketEventType)));
                }
                string Arg(int i) => args.Count > i ? args[i] : null;
                var ev = new MarketEvent { Type = type };
                switch (type)
                {
                    case MarketEventType.VillageLooted:
                        ev.ItemId = Arg(1); ev.TownId = Arg(2); ev.SettlementId = "console";
                        break;
                    case MarketEventType.SettlementCaptured:
                        ev.TownId = Arg(1); ev.KingdomId = Arg(2); ev.OtherKingdomId = Arg(3);
                        break;
                    case MarketEventType.WarDeclared:
                    case MarketEventType.PeaceMade:
                        ev.KingdomId = Arg(1); ev.OtherKingdomId = Arg(2);
                        break;
                    case MarketEventType.SeasonChanged:
                        if (!Enum.TryParse(Arg(1) ?? "", true, out Season season)) return "Usage: exchange.fire_event SeasonChanged <Spring|Summer|Autumn|Winter>";
                        ev.Season = season;
                        break;
                    case MarketEventType.KingdomDestroyed:
                        ev.KingdomId = Arg(1);
                        break;
                    default:
                        ev.TownId = Arg(1);
                        break;
                }
                IReadOnlyList<string> affected = b.ApplyEvent(ev);
                return $"Fired {ev}. Affected: {(affected.Count == 0 ? "none" : string.Join(", ", affected))}. Felt on the next tick (exchange.tick to run one now).";
            });
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("set_price", Group)]
        public static string SetPrice(List<string> args)
        {
            return RunReady("set_price", args, b =>
            {
                if (args.Count < 2 || !decimal.TryParse(args[1], NumberStyles.Number, Inv, out decimal price)) return "Usage: exchange.set_price <instrumentId> <price>";
                return b.Market.SetPrice(args[0], price) ? $"{args[0]} = {price}" : $"Unknown or closed instrument '{args[0]}'.";
            });
        }

        /// <summary>Runs one market tick immediately. The exchange day advances; game time does not.</summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("tick", Group)]
        public static string Tick(List<string> args)
        {
            return RunReady("tick", args, b =>
            {
                int n = args.Count > 0 && int.TryParse(args[0], out int v) ? Math.Max(1, Math.Min(100, v)) : 1;
                TickSummary last = null;
                for (int i = 0; i < n; i++) last = b.RunTick();
                return $"Ran {n} tick(s). Last: {last}";
            });
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("export_csv", Group)]
        public static string ExportCsv(List<string> args)
        {
            return RunReady("export_csv", args, b =>
            {
                var sb = new StringBuilder("day,instrument,P,F,S\n");
                foreach (PriceState ps in b.Market.State.Prices)
                {
                    foreach (PricePoint p in ps.History)
                    {
                        sb.Append(string.Format(Inv, "{0},{1},{2},{3},{4}\n", p.Day, ps.InstrumentId, p.P, p.F, p.S));
                    }
                }
                string path = Path.Combine(ModulePaths.LogsFolder, $"prices_day{b.State.Market.LastTickDay}.csv");
                File.WriteAllText(path, sb.ToString());
                return "Wrote " + path;
            });
        }

        /// <summary>
        /// Test helper: gives the player gold without cheat mode (vanilla campaign.add_gold_to_hero needs it).
        /// Usage: exchange.give_gold [amount, default 10000]. A negative amount takes gold away.
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("give_gold", Group)]
        public static string GiveGold(List<string> args)
        {
            return Run("give_gold", args, () =>
            {
                Hero hero = Hero.MainHero;
                if (Campaign.Current == null || hero == null) return "No campaign running.";
                int amount = args != null && args.Count > 0 && int.TryParse(args[0], out int n) ? n : 10000;
                if (amount > 0) GiveGoldAction.ApplyBetweenCharacters(null, hero, amount, true);
                else if (amount < 0) GiveGoldAction.ApplyBetweenCharacters(hero, null, Math.Min(-amount, hero.Gold), true);
                return $"Gold now {hero.Gold}.";
            });
        }

        /// <summary>
        /// Lists the most profitable guilds (town, workshop type) by average daily profit and writes all of them to
        /// Logs/guilds_day{N}.csv. Usage: exchange.dump_guilds [count, default 25].
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("dump_guilds", Group)]
        public static string DumpGuilds(List<string> args)
        {
            return RunReady("dump_guilds", args, b =>
            {
                int count = args != null && args.Count > 0 && int.TryParse(args[0], out int n) ? n : 25;
                string path = Path.Combine(ModulePaths.LogsFolder, $"guilds_day{b.State.Market.LastTickDay}.csv");
                File.WriteAllText(path, GuildReport.Csv(b.State.Market.Guilds));
                return GuildReport.Top(b.State.Market.Guilds, count) + "\nWrote " + path;
            });
        }

        /// <summary>Appends raw per-town market data to Logs/market_raw.csv (see <see cref="MarketDump"/>).</summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("dump_market", Group)]
        public static string DumpMarket(List<string> args)
        {
            return RunReady("dump_market", args, b => $"Appended {MarketDump.Append(b)} rows to {MarketDump.FilePath}");
        }

        private static string RunReady(string name, List<string> args, Func<ExchangeCampaignBehavior, string> body)
        {
            return Run(name, args, () =>
            {
                ExchangeCampaignBehavior b = ExchangeCampaignBehavior.Current;
                if (b == null) return "No campaign running.";
                if (!b.IsReady) return "Exchange not ready: " + (b.DisabledReason ?? "no prices yet");
                return body(b);
            });
        }

        private static string Run(string name, List<string> args, Func<string> body)
        {
            try
            {
                ExchangeLog.Info($"Console command exchange.{name} {string.Join(" ", args ?? new List<string>())}");
                return body();
            }
            catch (Exception ex)
            {
                ExchangeLog.Error($"Console command exchange.{name} failed", ex);
                return $"exchange.{name} failed: {ex.Message}";
            }
        }
    }
}
