using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class MarketEngineTests
    {
        private static MarketEngine NewEngine(MarketSettings settings = null, InstrumentCatalog catalog = null, ulong seed = 1)
        {
            return new MarketEngine(catalog ?? TestData.Catalog(), settings ?? TestData.NoNoiseSettings(), new MarketState(), new XorShift128Plus(seed));
        }

        [Fact]
        public void FirstTick_SetsPriceToFundamental()
        {
            MarketEngine e = NewEngine();
            TickSummary s = e.Tick(TestData.World(1));

            Assert.True(s.Initialized);
            Assert.Equal(36, s.InstrumentCount);
            Assert.Equal(100m, e.Get("cx_grain").Price);
            Assert.Equal(100m, e.Get("cx_sector_agriculture").Price);
            Assert.Equal(100m, e.Get("cx_grain").InitialPrice);
        }

        [Fact]
        public void SameDayTwice_IsIgnored()
        {
            MarketEngine e = NewEngine(TestData.Settings());
            e.Tick(TestData.World(1));
            e.Tick(TestData.World(2));
            decimal p = e.Get("cx_grain").Price;

            Assert.False(e.Tick(TestData.World(2)).Processed);
            Assert.False(e.Tick(TestData.World(1)).Processed);
            Assert.Equal(p, e.Get("cx_grain").Price);
        }

        [Fact]
        public void Price_ConvergesToFundamental()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            e.SetPrice("cx_grain", 150m);
            for (int d = 2; d < 80; d++) e.Tick(TestData.World(d));

            Assert.InRange((double)e.Get("cx_grain").Price, 100.0, 100.1);
        }

        [Fact]
        public void Price_FollowsRisingFundamental()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            for (int d = 2; d < 80; d++) e.Tick(TestData.World(d, multiplier: 1.3));

            Assert.Equal(130m, e.Get("cx_grain").Fundamental);
            Assert.InRange((double)e.Get("cx_grain").Price, 129.0, 130.0);
            Assert.InRange((double)e.Get("cx_sector_luxury").Price, 129.0, 130.0);
        }

        [Fact]
        public void VillageRaid_PushesProductUp_ThenDecaysWithHalfLife()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            IReadOnlyList<string> affected = e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 1, ItemId = "grain", TownId = "town_A" });

            Assert.Equal(new[] { "cx_grain" }, affected);
            e.Tick(TestData.World(2));
            PriceState grain = e.Get("cx_grain");
            Assert.Equal(0.15, grain.Shock, 9);
            Assert.Equal(100 * System.Math.Exp(0.20 * 0.15), (double)grain.Price, 2);

            for (int d = 3; d <= 12; d++) e.Tick(TestData.World(d));
            Assert.Equal(0.075, grain.Shock, 6);
            Assert.Equal(400m, e.Get("cx_salt").Price); // untouched: 10 x 40
        }

        [Fact]
        public void RaidOnUntrackedProduct_AffectsNothing()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            Assert.Empty(e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 1, ItemId = "spice" }));
        }

        [Fact]
        public void Shock_IsClampedToMaxAbsShock()
        {
            MarketSettings s = TestData.NoNoiseSettings();
            s.RulesFor(MarketEventType.VillageLooted)[0].MaxStack = 0;
            var e = new MarketEngine(TestData.Catalog(), s, new MarketState(), new XorShift128Plus(1));
            e.Tick(TestData.World(1));
            for (int i = 0; i < 40; i++) e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 1, ItemId = "grain" });
            e.Tick(TestData.World(2));

            Assert.Equal(1.0, e.Get("cx_grain").Shock, 9);
        }

        [Fact]
        public void RepeatedRaids_StackOnlyUpToMaxStack()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            for (int i = 0; i < 40; i++) e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 1, ItemId = "grain" });
            e.Tick(TestData.World(2));

            Assert.Equal(0.15 * 4, e.Get("cx_grain").Shock, 9);
        }

        [Fact]
        public void StackRoom_RefillsAsOldRaidsDecay_AndOtherItemsAreSeparate()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            for (int i = 0; i < 4; i++) e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 1, ItemId = "grain" });
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 1, ItemId = "salt" });
            for (int d = 2; d <= 11; d++) e.Tick(TestData.World(d)); // day 2 applies, days 3..11 decay 9 times

            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 11, ItemId = "grain" });
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 11, ItemId = "grain" });
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, ItemShare = TestData.RaidShare, Day = 11, ItemId = "grain" });
            e.Tick(TestData.World(12));

            // The new raids only fill the room left under 4 x 0.15 (not 3 x 0.15); then the old ones decay once more.
            double left = 0.6 * System.Math.Pow(0.5, 0.9);
            double expected = left * System.Math.Pow(0.5, 0.1) + (0.6 - left);
            Assert.Equal(expected, e.Get("cx_grain").Shock, 6);
            Assert.Equal(0.15 * 0.5, e.Get("cx_salt").Shock, 6);
        }

        [Fact]
        public void SiegeShock_IsHeldUntilSiegeEnds()
        {
            InstrumentCatalog cat = CatalogWith(new InstrumentDefinition { Id = "g_A", Class = InstrumentClass.Guild, TownId = "town_A" });
            MarketEngine e = NewEngine(catalog: cat);
            e.Tick(TestData.World(1));
            e.SetPrice("g_A", 50m);

            e.ApplyEvent(new MarketEvent { Type = MarketEventType.SiegeStarted, Day = 1, TownId = "town_A" });
            for (int d = 2; d <= 8; d++) e.Tick(TestData.World(d));
            Assert.Equal(-0.45, e.Get("g_A").Shock, 9);

            e.ApplyEvent(new MarketEvent { Type = MarketEventType.SiegeEnded, Day = 8, TownId = "town_A" });
            for (int d = 9; d <= 15; d++) e.Tick(TestData.World(d));
            Assert.Equal(-0.225, e.Get("g_A").Shock, 6);
        }

        [Fact]
        public void SiegeEnd_ReleasesOnlyThatTown()
        {
            InstrumentCatalog cat = CatalogWith(
                new InstrumentDefinition { Id = "g_A", Class = InstrumentClass.Guild, TownId = "town_A" },
                new InstrumentDefinition { Id = "g_B", Class = InstrumentClass.Guild, TownId = "town_B" });
            MarketEngine e = NewEngine(catalog: cat);
            e.Tick(TestData.World(1));
            e.SetPrice("g_A", 50m);
            e.SetPrice("g_B", 50m);
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.SiegeStarted, Day = 1, TownId = "town_A" });
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.SiegeStarted, Day = 1, TownId = "town_B" });
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.SiegeEnded, Day = 1, TownId = "town_A" });
            for (int d = 2; d <= 8; d++) e.Tick(TestData.World(d));

            // Released before its first tick: tick 2 applies it in full, ticks 3-8 decay it 6 times.
            Assert.Equal(-0.45 * System.Math.Pow(0.5, 6.0 / 7), e.Get("g_A").Shock, 9);
            Assert.Equal(-0.45, e.Get("g_B").Shock, 9);
        }

        [Fact]
        public void SeasonChange_IsDetected_AndHitsAgriculture()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1, Season.Autumn));
            Assert.Empty(e.Tick(TestData.World(2, Season.Autumn)).GeneratedEvents);

            TickSummary s = e.Tick(TestData.World(3, Season.Winter));

            MarketEvent ev = Assert.Single(s.GeneratedEvents);
            Assert.Equal(MarketEventType.SeasonChanged, ev.Type);
            Assert.Equal(Season.Winter, ev.Season);
            Assert.Equal(0.21, e.Get("cx_grain").Shock, 9);
            Assert.Equal(0.21, e.Get("cx_sector_agriculture").Shock, 9);
            Assert.Equal(0.0, e.Get("cx_iron").Shock);
        }

        [Fact]
        public void Famine_FiresOncePerEpisode()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            int Famines(int day, double food)
            {
                WorldSnapshot w = TestData.World(day);
                w.Towns[0].FoodStocks = food;
                return e.Tick(w).GeneratedEvents.Count(x => x.Type == MarketEventType.FamineStarted);
            }

            Assert.Equal(1, Famines(2, 0));
            Assert.Equal(0, Famines(3, 0));
            Assert.Equal(0, Famines(4, 10));
            Assert.Equal(0, Famines(5, -1)); // same town again within the 21-day cooldown
        }

        [Fact]
        public void Famine_CanStartAgainAfterCooldown()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            int Famines(int day, double food)
            {
                WorldSnapshot w = TestData.World(day);
                w.Towns[0].FoodStocks = food;
                return e.Tick(w).GeneratedEvents.Count(x => x.Type == MarketEventType.FamineStarted);
            }

            Assert.Equal(1, Famines(2, 0));
            Assert.Equal(0, Famines(10, 10));
            Assert.Equal(1, Famines(23, 0));
        }

        [Fact]
        public void FamineShock_IsScaledByTownShare()
        {
            MarketEngine e = NewEngine();
            e.Tick(TestData.World(1));
            WorldSnapshot w = TestData.World(2);
            w.Towns[0].FoodStocks = 0;
            w.Towns[0].Prosperity = 2000; // share 2000 / 4000 = 0.5

            MarketEvent ev = e.Tick(w).GeneratedEvents.Single();

            Assert.Equal(0.5, ev.TownShare.Value, 9);
            Assert.Equal(0.24 * 0.5 * 2.0, e.Get("cx_grain").Shock, 9);
        }

        [Fact]
        public void TownCapture_AddsTemporaryVolatilityToItsGuilds()
        {
            InstrumentCatalog cat = CatalogWith(new InstrumentDefinition { Id = "g_A", Class = InstrumentClass.Guild, TownId = "town_A" });
            MarketEngine e = NewEngine(catalog: cat);
            e.Tick(TestData.World(1));
            e.SetPrice("g_A", 50m);
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.SettlementCaptured, Day = 1, TownId = "town_A", KingdomId = "k1", OtherKingdomId = "k2" });

            VolBoost boost = Assert.Single(e.Get("g_A").VolBoosts);
            Assert.Equal(2.0, boost.Multiplier);
            Assert.Equal(15, boost.UntilDay);

            for (int d = 2; d <= 16; d++) e.Tick(TestData.World(d));
            Assert.Empty(e.Get("g_A").VolBoosts);
        }

        [Fact]
        public void KingdomDestroyed_ClosesItsIndex()
        {
            InstrumentCatalog cat = CatalogWith(new InstrumentDefinition { Id = "k_index", Class = InstrumentClass.Kingdom, KingdomId = "k9" });
            MarketEngine e = NewEngine(catalog: cat);
            e.Tick(TestData.World(1));
            e.SetPrice("k_index", 100m);

            Assert.Contains("k_index", e.ApplyEvent(new MarketEvent { Type = MarketEventType.KingdomDestroyed, Day = 1, KingdomId = "k9" }));
            Assert.True(e.Get("k_index").Closed);
            Assert.Equal(0m, e.Get("k_index").Price);
        }

        [Fact]
        public void News_KeepsTheLatestOnly()
        {
            MarketSettings s = TestData.NoNoiseSettings();
            s.NewsKeep = 3;
            MarketEngine e = NewEngine(s);
            for (int i = 0; i < 5; i++) e.ApplyEvent(new MarketEvent { Type = MarketEventType.WarDeclared, Day = i });

            Assert.Equal(new[] { 2, 3, 4 }, e.State.News.Select(n => n.Day));
        }

        [Fact]
        public void History_IsCapped()
        {
            MarketSettings s = TestData.NoNoiseSettings();
            s.HistoryDays = 10;
            MarketEngine e = NewEngine(s);
            for (int d = 1; d <= 30; d++) e.Tick(TestData.World(d));

            Assert.Equal(10, e.Get("cx_grain").History.Count);
            Assert.Equal(30, e.Get("cx_grain").History.Last().Day);
        }

        [Fact]
        public void SameSeedAndInputs_GiveSamePrices()
        {
            decimal[] Run(ulong seed)
            {
                MarketEngine e = NewEngine(TestData.Settings(), seed: seed);
                for (int d = 1; d <= 50; d++)
                {
                    if (d == 10) e.ApplyEvent(new MarketEvent { Type = MarketEventType.WarDeclared, Day = d });
                    e.Tick(TestData.World(d));
                }
                return e.State.Prices.Select(p => p.Price).ToArray();
            }

            Assert.Equal(Run(5), Run(5));
            Assert.NotEqual(Run(5), Run(6));
        }

        [Fact]
        public void ItemWithoutData_StartsTradingWhenDataArrives()
        {
            MarketEngine e = NewEngine();
            WorldSnapshot w1 = TestData.World(1);
            foreach (TownSnapshot t in w1.Towns) t.ItemPrices.Remove("velvet");
            e.Tick(w1);
            Assert.False(e.Get("cx_velvet").HasPrice);

            e.Tick(TestData.World(2));
            Assert.True(e.Get("cx_velvet").HasPrice);
            Assert.Equal(5750m, e.Get("cx_velvet").Price);
        }

        private static InstrumentCatalog CatalogWith(params InstrumentDefinition[] extra)
        {
            InstrumentCatalog baseCat = TestData.Catalog();
            return new InstrumentCatalog(baseCat.UnitsPerShare, baseCat.Instruments.Concat(extra), baseCat.GuildTypes);
        }
    }
}
