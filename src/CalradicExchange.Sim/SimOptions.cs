using System;
using System.Globalization;

namespace CalradicExchange.Sim
{
    /// <summary>Command-line options: --days N --seed N --seeds N --out path --data folder.</summary>
    public sealed class SimOptions
    {
        public int Days { get; private set; } = 336;

        /// <summary>First seed; also the seed of the run written to the CSV.</summary>
        public ulong Seed { get; private set; } = 42;

        /// <summary>Number of seeds (seed, seed+1, ...) the targets are averaged over.</summary>
        public int Seeds { get; private set; } = 20;

        public string OutPath { get; private set; } = "sim/out.csv";

        /// <summary>Folder with instruments.json and settings.json. Defaults to the copy next to the exe.</summary>
        public string DataDir { get; private set; }

        public static SimOptions Parse(string[] args)
        {
            var options = new SimOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string value = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "--days":
                        options.Days = int.Parse(Require(args[i], value), CultureInfo.InvariantCulture);
                        break;
                    case "--seed":
                        options.Seed = ulong.Parse(Require(args[i], value), CultureInfo.InvariantCulture);
                        break;
                    case "--seeds":
                        options.Seeds = int.Parse(Require(args[i], value), CultureInfo.InvariantCulture);
                        break;
                    case "--out":
                        options.OutPath = Require(args[i], value);
                        break;
                    case "--data":
                        options.DataDir = Require(args[i], value);
                        break;
                    default:
                        throw new ArgumentException($"Unknown argument '{args[i]}'. Usage: --days N --seed N --seeds N --out path --data folder");
                }
                i++;
            }
            if (options.Days < 30) throw new ArgumentException("--days must be at least 30.");
            if (options.Seeds < 1) throw new ArgumentException("--seeds must be at least 1.");
            return options;
        }

        private static string Require(string flag, string value)
        {
            if (value == null) throw new ArgumentException($"Missing value for {flag}.");
            return value;
        }
    }
}
