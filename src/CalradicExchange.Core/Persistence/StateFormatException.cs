using System;

namespace CalradicExchange.Core.Persistence
{
    /// <summary>Thrown when a save string cannot be read.</summary>
    public sealed class StateFormatException : Exception
    {
        public StateFormatException(string message) : base(message) { }
        public StateFormatException(string message, Exception inner) : base(message, inner) { }
    }
}
