using System;
using System.IO;
using CalradicExchange.Core;

namespace CalradicExchange.Sim
{
    /// <summary>
    /// Runs Core against synthetic event streams and writes a CSV (ECONOMY_MODEL section 8).
    /// Phase 0 skeleton: parses options and writes the CSV header only. Pricing arrives in PLAN 1.11.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            SimOptions options;
            try
            {
                options = SimOptions.Parse(args);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException)
            {
                Console.Error.WriteLine(ex.Message);
                return 2;
            }

            string dir = Path.GetDirectoryName(Path.GetFullPath(options.OutPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(options.OutPath, "day,instrument,P,F,S" + Environment.NewLine);

            Console.WriteLine($"Calradic Exchange simulator (Core {CoreInfo.Version})");
            Console.WriteLine($"days={options.Days} seed={options.Seed} out={options.OutPath}");
            Console.WriteLine("Pricing not implemented yet (PLAN 1.11). Wrote CSV header only.");
            return 0;
        }
    }
}
