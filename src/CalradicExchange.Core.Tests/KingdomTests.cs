using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Kingdoms;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using CalradicExchange.Core.Trading;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class KingdomTests
    {
        private const string K1 = "cx_kingdom_k1";
        private const string K2 = "cx_kingdom_k2";

        private static WorldSnapshot World(int day, params KingdomSnapshot[] kingdoms)
        {
            WorldSnapshot w = TestData.World(day);
            w.Kingdoms = kingdoms.Length > 0 ? kingdoms.ToList() : new List<KingdomSnapshot> { K("k1"), K("k2") };
            return w;
        }

        private static KingdomSnapshot K(string id, double prosperity = 10000, int fiefs = 8, int enemies = 0, int sieges = 0)
        {
            return new KingdomSnapshot { Id = id, Prosperity = prosperity, FiefCount = fiefs, EnemyCount = enemies, FiefsUnderSiege = sieges };
        }

        private static MarketEngine Engine(MarketState state = null)
        {
            return new MarketEngine(TestData.Catalog(), TestData.NoNoiseSettings(), state ?? new MarketState(), new XorShift128Plus(5));
        }

        [Fact]
        public void KingdomsAliveAtTheStart_AreListedAtOnce_AtIndex100()
        {
            MarketEngine engine = Engine();
            engine.Tick(World(1));

            Assert.Equal(new[] { K1, K2 }, engine.Kingdoms.Select(k => k.Id));
            InstrumentDefinition def = engine.Find(K1);
            Assert.Equal(InstrumentClass.Kingdom, def.Class);
            Assert.Equal("k1", def.KingdomId);
            Assert.Equal(100m, engine.Get(K1).InitialPrice);
        }

        [Fact]
        public void NewKingdom_WaitsMinListingDays()
        {
            MarketEngine engine = Engine();
            engine.Tick(World(1));
            for (int d = 2; d <= 8; d++) engine.Tick(World(d, K("k1"), K("k2"), K("rebels", 2000, 1)));
            Assert.Null(engine.Find("cx_kingdom_rebels"));

            engine.Tick(World(9, K("k1"), K("k2"), K("rebels", 2000, 1)));
            Assert.NotNull(engine.Find("cx_kingdom_rebels"));
            Assert.True(engine.Get("cx_kingdom_rebels").HasPrice);
        }

        [Fact]
        public void ObservedValue_FollowsProsperityFiefsWarsAndSieges()
        {
            var s = new KingdomSettings();
            var record = new KingdomRecord { KingdomId = "k1", BaseProsperity = 10000, BaseFiefs = 8 };

            Assert.Equal(100m, KingdomInstruments.ObservedValue(record, K("k1"), s));
            Assert.Equal(50m, KingdomInstruments.ObservedValue(record, K("k1", prosperity: 10000, fiefs: 2), s)); // sqrt(2/8)
            Assert.Equal(90m, KingdomInstruments.ObservedValue(record, K("k1", enemies: 2), s));
            Assert.Equal(70m, KingdomInstruments.ObservedValue(record, K("k1", enemies: 9), s)); // war floor
            Assert.Equal(60m, KingdomInstruments.ObservedValue(record, K("k1", sieges: 7), s)); // siege floor
            Assert.True(KingdomInstruments.ObservedValue(record, K("k1", prosperity: 0, fiefs: 0), s) > 0);
        }

        [Fact]
        public void LosingFiefs_PullsTheIndexDown()
        {
            MarketEngine engine = Engine();
            engine.Tick(World(1));
            for (int d = 2; d <= 30; d++) engine.Tick(World(d, K("k1", 5000, 4), K("k2", 15000, 12)));

            Assert.True(engine.Get(K1).Price < 90m);
            Assert.True(engine.Get(K2).Price > 110m);
        }

        [Fact]
        public void FallenKingdom_ClosesAtZero_FromTheEventOrFromTheSnapshot()
        {
            MarketEngine engine = Engine();
            engine.Tick(World(1));
            engine.Tick(World(2));

            IReadOnlyList<string> affected = engine.ApplyEvent(new MarketEvent { Type = MarketEventType.KingdomDestroyed, Day = 3, KingdomId = "k1" });
            Assert.Contains(K1, affected);
            Assert.True(engine.Get(K1).Closed);
            Assert.Equal(0m, engine.Get(K1).Price);

            TickSummary gone = engine.Tick(World(3, K("k1")));
            Assert.Contains(K2, gone.ClosedInstruments);
            Assert.Equal(0m, engine.Get(K2).Price);

            // A fallen kingdom never reopens, and cannot be bought.
            engine.Tick(World(4, K("k1"), K("k2")));
            Assert.True(engine.Get(K1).Closed);
            var orders = new OrderEngine(engine);
            Assert.False(orders.Quote(K1, OrderSide.Buy, 1, new TraderProfile(100000, 6, 0), new Portfolio()).IsValid);
        }

        [Fact]
        public void CapturedTown_MovesBothIndices()
        {
            MarketEngine engine = Engine();
            engine.Tick(World(1));
            engine.ApplyEvent(new MarketEvent { Type = MarketEventType.SettlementCaptured, Day = 2, TownId = "town_A", KingdomId = "k1", OtherKingdomId = "k2" });
            Assert.Contains(engine.Get(K1).Shocks, c => c.Value < 0);
            Assert.Contains(engine.Get(K2).Shocks, c => c.Value > 0);
        }

        [Fact]
        public void EmptyKingdomList_ChangesNothing()
        {
            var state = new MarketState();
            MarketEngine engine = Engine(state);
            engine.Tick(World(1));
            WorldSnapshot blank = TestData.World(2);
            engine.Tick(blank);
            Assert.False(engine.Get(K1).Closed);
            Assert.DoesNotContain(state.Kingdoms, k => k.Eliminated);
        }

        [Fact]
        public void KingdomRecords_SurviveSaveAndLoad()
        {
            var state = ExchangeState.CreateNew(1);
            MarketEngine engine = Engine(state.Market);
            engine.Tick(World(1));

            string json = StateSerializer.Serialize(state);
            ExchangeState back = StateSerializer.Deserialize(json);

            Assert.Equal(json, StateSerializer.Serialize(back));
            Assert.Equal(2, back.Market.Kingdoms.Count);
            Assert.NotNull(Engine(back.Market).Find(K2));
        }
    }
}
