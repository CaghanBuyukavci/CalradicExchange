using System;
using CalradicExchange.Core.Pricing;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class PriceMathTests
    {
        [Fact]
        public void Decay_HalvesAfterHalfLife()
        {
            double v = 1.0;
            for (int i = 0; i < 10; i++) v *= PriceMath.DecayFactor(10);
            Assert.Equal(0.5, v, 9);
        }

        [Fact]
        public void LogReturn_IsZeroAtTargetWithoutNoise()
        {
            Assert.Equal(0.0, PriceMath.LogReturn(100m, 100m, 0, 0.12, 0.02, 0, 0.25), 12);
        }

        [Fact]
        public void LogReturn_PullsTowardsTarget()
        {
            // T = 100 x e^0.1, r = 0.12 x ln(T / 100) = 0.012
            Assert.Equal(0.012, PriceMath.LogReturn(100m, 100m, 0.1, 0.12, 0, 0, 0.25), 9);
            Assert.True(PriceMath.LogReturn(120m, 100m, 0, 0.12, 0, 0, 0.25) < 0);
        }

        [Fact]
        public void LogReturn_IsClamped()
        {
            Assert.Equal(0.25, PriceMath.LogReturn(1m, 1000m, 0, 1, 0, 0, 0.25));
            Assert.Equal(-0.25, PriceMath.LogReturn(100m, 100m, 0, 0.12, 1, -10, 0.25));
        }

        [Fact]
        public void Apply_NeverGoesToZero()
        {
            Assert.Equal(PriceMath.MinPrice, PriceMath.Apply(0.0001m, -0.25));
            Assert.Equal(Math.Round(100 * Math.Exp(0.01), 4), (double)PriceMath.Apply(100m, 0.01), 4);
        }
    }
}
