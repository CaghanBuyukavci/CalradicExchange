using System;
using System.Globalization;

namespace CalradicExchange.Sim
{
    /// <summary>Command-line options: --days N --seed N --out path.</summary>
    public sealed class SimOptions
    {
        public int Days { get; private set; } = 336;
        public ulong Seed { get; private set; } = 42;
        public string OutPath { get; private set; } = "sim/out.csv";

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
                        i++;
                        break;
                    case "--seed":
                        options.Seed = ulong.Parse(Require(args[i], value), CultureInfo.InvariantCulture);
                        i++;
                        break;
                    case "--out":
                        options.OutPath = Require(args[i], value);
                        i++;
                        break;
                    default:
                        throw new ArgumentException($"Unknown argument '{args[i]}'. Usage: --days N --seed N --out path");
                }
            }
            return options;
        }

        private static string Require(string flag, string value)
        {
            if (value == null) throw new ArgumentException($"Missing value for {flag}.");
            return value;
        }
    }
}
