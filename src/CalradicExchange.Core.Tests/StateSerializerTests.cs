using CalradicExchange.Core.Persistence;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class StateSerializerTests
    {
        [Fact]
        public void RoundTrip_PreservesAllFields()
        {
            var original = new ExchangeState { CreatedOnDay = 123, LastTickDay = 456 };

            string json = StateSerializer.Serialize(original);
            ExchangeState loaded = StateSerializer.Deserialize(json);

            Assert.Equal(ExchangeState.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(123, loaded.CreatedOnDay);
            Assert.Equal(456, loaded.LastTickDay);
            Assert.Equal(CoreInfo.Version, loaded.WrittenByVersion);
        }

        [Fact]
        public void RoundTrip_IsStable()
        {
            string first = StateSerializer.Serialize(ExchangeState.CreateNew(10));
            string second = StateSerializer.Serialize(StateSerializer.Deserialize(first));

            Assert.Equal(first, second);
        }

        [Fact]
        public void Serialize_WritesSchemaVersionAtRoot()
        {
            string json = StateSerializer.Serialize(ExchangeState.CreateNew(0));

            Assert.Contains("\"schemaVersion\":" + ExchangeState.CurrentSchemaVersion, json);
        }

        [Fact]
        public void Deserialize_MissingSchemaVersion_IsMigratedToCurrent()
        {
            ExchangeState loaded = StateSerializer.Deserialize("{\"createdOnDay\":5}");

            Assert.Equal(ExchangeState.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(5, loaded.CreatedOnDay);
            Assert.Equal(-1, loaded.LastTickDay);
        }

        [Fact]
        public void Deserialize_UnknownFields_AreIgnored()
        {
            ExchangeState loaded = StateSerializer.Deserialize("{\"schemaVersion\":1,\"createdOnDay\":7,\"futureField\":[1,2]}");

            Assert.Equal(7, loaded.CreatedOnDay);
        }

        [Fact]
        public void Deserialize_NewerSchema_Throws()
        {
            Assert.Throws<StateFormatException>(() => StateSerializer.Deserialize("{\"schemaVersion\":999}"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("not json")]
        public void Deserialize_Garbage_Throws(string input)
        {
            Assert.Throws<StateFormatException>(() => StateSerializer.Deserialize(input));
        }
    }
}
