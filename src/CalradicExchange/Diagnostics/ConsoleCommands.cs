using System;
using System.Collections.Generic;
using CalradicExchange.Behaviors;
using CalradicExchange.Core;
using TaleWorlds.Library;

namespace CalradicExchange.Diagnostics
{
    /// <summary>
    /// Developer console commands, invoked as "exchange.&lt;name&gt;".
    /// Discovered automatically by CommandLineFunctionality (it scans every assembly that references TaleWorlds.Library).
    /// </summary>
    public static class ConsoleCommands
    {
        private const string Group = "exchange";

        [CommandLineFunctionality.CommandLineArgumentFunction("ping", Group)]
        public static string Ping(List<string> args)
        {
            return Run("ping", () =>
            {
                var behavior = ExchangeCampaignBehavior.Current;
                string state = behavior?.State == null
                    ? "no campaign state"
                    : $"createdOnDay={behavior.State.CreatedOnDay}, lastTickDay={behavior.State.LastTickDay}, schema={behavior.State.SchemaVersion}";
                return $"pong - Calradic Exchange {CoreInfo.Version}; {state}; log={ExchangeLog.FilePath}";
            });
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("dump_prices", Group)]
        public static string DumpPrices(List<string> args) => NotYet("dump_prices", "1.22");

        [CommandLineFunctionality.CommandLineArgumentFunction("dump_portfolio", Group)]
        public static string DumpPortfolio(List<string> args) => NotYet("dump_portfolio", "1.22");

        [CommandLineFunctionality.CommandLineArgumentFunction("fire_event", Group)]
        public static string FireEvent(List<string> args) => NotYet("fire_event", "1.22");

        [CommandLineFunctionality.CommandLineArgumentFunction("set_price", Group)]
        public static string SetPrice(List<string> args) => NotYet("set_price", "1.22");

        [CommandLineFunctionality.CommandLineArgumentFunction("export_csv", Group)]
        public static string ExportCsv(List<string> args) => NotYet("export_csv", "1.22");

        private static string NotYet(string name, string planTask)
        {
            return Run(name, () => $"exchange.{name} is not implemented yet (PLAN {planTask}).");
        }

        private static string Run(string name, Func<string> body)
        {
            try
            {
                ExchangeLog.Info($"Console command exchange.{name}");
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
