using System;
using CalradicExchange.Core.Random;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class RandomTests
    {
        [Fact]
        public void SameSeed_SameSequence()
        {
            var a = new XorShift128Plus(42);
            var b = new XorShift128Plus(42);
            for (int i = 0; i < 1000; i++) Assert.Equal(a.NextULong(), b.NextULong());
        }

        [Fact]
        public void DifferentSeed_DifferentSequence()
        {
            Assert.NotEqual(new XorShift128Plus(1).NextULong(), new XorShift128Plus(2).NextULong());
        }

        [Fact]
        public void ZeroSeed_IsValid()
        {
            var r = new XorShift128Plus(0);
            Assert.NotEqual(0UL, r.NextULong() | r.NextULong());
        }

        [Fact]
        public void State_RoundTrip_ContinuesSequence()
        {
            var a = new XorShift128Plus(7);
            for (int i = 0; i < 10; i++) a.NextDouble();
            var b = new XorShift128Plus(a.GetState());
            for (int i = 0; i < 100; i++) Assert.Equal(a.NextGaussian(), b.NextGaussian());
        }

        [Fact]
        public void AllZeroState_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new XorShift128Plus(new RandomState()));
        }

        [Fact]
        public void NextDouble_InUnitInterval()
        {
            var r = new XorShift128Plus(3);
            for (int i = 0; i < 10000; i++)
            {
                double d = r.NextDouble();
                Assert.InRange(d, 0.0, 0.9999999999);
            }
        }

        [Fact]
        public void Gaussian_HasRoughlyStandardMoments()
        {
            var r = new XorShift128Plus(11);
            const int n = 50000;
            double sum = 0, sumSq = 0;
            for (int i = 0; i < n; i++)
            {
                double g = r.NextGaussian();
                sum += g;
                sumSq += g * g;
            }
            double mean = sum / n;
            double variance = sumSq / n - mean * mean;
            Assert.InRange(mean, -0.02, 0.02);
            Assert.InRange(variance, 0.97, 1.03);
        }
    }
}
