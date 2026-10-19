using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Random;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class SaveChunksTests
    {
        /// <summary>Bannerlord stores a string's size (byte count + 4) as a signed 16-bit number.</summary>
        private const int SaveStringLimitBytes = 32767 - 4;

        private static string BigJson(int approxChars)
        {
            // Hard-to-compress content so the payload really needs several chunks.
            var rng = new XorShift128Plus(7);
            var sb = new StringBuilder("{\"schemaVersion\":8,\"data\":[");
            while (sb.Length < approxChars) sb.Append(rng.NextDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append("0]}");
            return sb.ToString();
        }

        [Fact]
        public void SmallState_IsOneChunk_AndRoundTrips()
        {
            const string json = "{\"schemaVersion\":8,\"name\":\"Pravend çarşısı\"}";
            List<string> chunks = SaveChunks.Pack(json);

            Assert.Single(chunks);
            Assert.StartsWith("CXZ1|0|1|", chunks[0]);
            Assert.Equal(1, SaveChunks.PartCount(chunks[0]));
            Assert.Equal(json, SaveChunks.Unpack(chunks));
        }

        [Fact]
        public void LargeState_IsSplit_EveryChunkFitsTheSaveLimit_AndRoundTrips()
        {
            string json = BigJson(400_000);
            List<string> chunks = SaveChunks.Pack(json);

            Assert.True(chunks.Count > 1);
            Assert.All(chunks, c => Assert.True(Encoding.UTF8.GetByteCount(c) <= SaveStringLimitBytes));
            Assert.Equal(chunks.Count, SaveChunks.PartCount(chunks[0]));
            Assert.Equal(json, SaveChunks.Unpack(chunks));
        }

        [Fact]
        public void LegacyPlainJson_IsReadAsIs()
        {
            const string json = "{\"schemaVersion\":7}";
            Assert.Equal(1, SaveChunks.PartCount(json));
            Assert.Equal(json, SaveChunks.Unpack(new[] { json }));
        }

        [Fact]
        public void MissingChunk_Throws()
        {
            List<string> chunks = SaveChunks.Pack(BigJson(400_000));
            chunks.RemoveAt(chunks.Count - 1);
            Assert.Throws<StateFormatException>(() => SaveChunks.Unpack(chunks));
        }

        [Fact]
        public void DamagedChunk_Throws()
        {
            List<string> chunks = SaveChunks.Pack(BigJson(400_000));
            chunks[1] = null; // key absent in the save
            Assert.Throws<StateFormatException>(() => SaveChunks.Unpack(chunks));
            Assert.Throws<StateFormatException>(() => SaveChunks.Unpack(new[] { "CXZ1|0|1|not base64!" }));
        }

        [Fact]
        public void Nothing_HasZeroParts()
        {
            Assert.Equal(0, SaveChunks.PartCount(null));
            Assert.Equal(0, SaveChunks.PartCount(""));
        }

        [Fact]
        public void FullState_RoundTripsThroughChunks()
        {
            ExchangeState state = ExchangeState.CreateNew(91077);
            state.Market.News.AddRange(Enumerable.Range(0, 3).Select(i => new Pricing.NewsItem { Day = i }));
            string json = StateSerializer.Serialize(state);

            ExchangeState back = StateSerializer.Deserialize(SaveChunks.Unpack(SaveChunks.Pack(json)));

            Assert.Equal(json, StateSerializer.Serialize(back));
        }
    }
}
