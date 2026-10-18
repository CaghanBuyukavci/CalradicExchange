using System;
using System.Collections.Generic;

namespace CalradicExchange.Core.Config
{
    /// <summary>Thrown when instruments.json or settings.json is missing, malformed or invalid. Lists every problem found.</summary>
    public sealed class ConfigException : Exception
    {
        public ConfigException(string file, IReadOnlyList<string> errors)
            : base($"{file}: {errors.Count} problem(s):{Environment.NewLine}  - " + string.Join(Environment.NewLine + "  - ", errors))
        {
            File = file;
            Errors = errors;
        }

        public ConfigException(string file, string error, Exception inner = null)
            : base($"{file}: {error}", inner)
        {
            File = file;
            Errors = new[] { error };
        }

        public string File { get; }
        public IReadOnlyList<string> Errors { get; }
    }
}
