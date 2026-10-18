namespace CalradicExchange.Core.Random
{
    /// <summary>Injected randomness for Core (CLAUDE.md rule 4). Same state = same sequence.</summary>
    public interface IRandom
    {
        /// <summary>Uniform in [0, 1).</summary>
        double NextDouble();

        /// <summary>Standard normal N(0, 1).</summary>
        double NextGaussian();

        /// <summary>Copy of the current state, for persistence.</summary>
        RandomState GetState();
    }
}
