using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Persistence;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Random;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    /// <summary>ADR-040: made-up market news, rumors, jumps in the noise and raid shocks scaled by production share.</summary>
    public class MarketNewsTests
    {
        private const string Grain = "cx_grain";

        private static MarketSettings NewsOnGrain(double dailyChance, double rumorChance, double falseShare, double impulse = 0.3)
        {
            MarketSettings s = TestData.NoNoiseSettings();
            MarketNewsSettings n = s.MarketNews;
            n.Enabled = true;
            n.DailyChance = dailyChance;
            n.RumorChance = rumorChance;
            n.FalseRumorShare = falseShare;
            n.RumorLeadMinDays = 2;
            n.RumorLeadMaxDays = 2;
            n.Templates = new List<MarketNewsTemplate>
            {
                new MarketNewsTemplate { Id = "blight", Items = new List<string> { "grain" }, MinImpulse = impulse, MaxImpulse = impulse, HalfLifeDays = 20 },
            };
            return s;
        }

        private static MarketEngine Start(MarketSettings s, ulong seed = 1)
        {
            var e = new MarketEngine(TestData.Catalog(), s, new MarketState(), new XorShift128Plus(seed));
            e.Tick(TestData.World(1));
            return e;
        }

        [Fact]
        public void News_JumpsThePriceOnTheDay_AndKappaPullsInTheRest()
        {
            MarketSettings s = NewsOnGrain(1, 0, 0);
            MarketEngine e = Start(s);
            decimal before = e.Get(Grain).Price;

            TickSummary t = e.Tick(TestData.World(2));

            Assert.Contains(t.GeneratedEvents, ev => ev.Type == MarketEventType.MarketNews && ev.InstrumentId == Grain && ev.Detail == "blight");
            Assert.Equal(MarketEventType.MarketNews, e.State.News.Last().Event.Type);
            double expected = s.For(InstrumentClass.Commodity).Kappa * 0.3 + s.MarketNews.ImmediateFraction * 0.3;
            Assert.Equal(expected, Math.Log((double)(e.Get(Grain).Price / before)), 3);

            // The rest of the move comes over the next days.
            for (int d = 3; d <= 8; d++) e.Tick(TestData.World(d));
            Assert.True(Math.Log((double)(e.Get(Grain).Price / before)) > expected + 0.05);
        }

        [Fact]
        public void Rumor_ComesFirst_AndItsNewsFollowsOnTheDay()
        {
            MarketEngine e = Start(NewsOnGrain(1, 1, 0));

            TickSummary t2 = e.Tick(TestData.World(2));
            Assert.Contains(t2.GeneratedEvents, ev => ev.Type == MarketEventType.MarketRumor && ev.InstrumentId == Grain);
            Assert.DoesNotContain(t2.GeneratedEvents, ev => ev.Type == MarketEventType.MarketNews);
            Assert.Contains(e.State.ScheduledNews, n => n.Day == 4 && n.InstrumentId == Grain);
            Assert.Contains(e.Get(Grain).Shocks, c => c.Source == "MarketRumor:blight");

            e.Tick(TestData.World(3));
            TickSummary t4 = e.Tick(TestData.World(4));
            Assert.Contains(t4.GeneratedEvents, ev => ev.Type == MarketEventType.MarketNews && ev.InstrumentId == Grain);
            Assert.DoesNotContain(e.State.ScheduledNews, n => n.Day <= 4);
        }

        [Fact]
        public void FalseRumors_AreTheConfiguredShareOfAllRumors()
        {
            MarketSettings s = NewsOnGrain(0.5, 1, 0.5);
            s.MarketNews.Templates[0].Items.Clear(); // any commodity, so one story at a time rarely blocks a pick
            MarketEngine e = Start(s);
            int rumors = 0, news = 0;
            for (int d = 2; d <= 1201; d++)
            {
                TickSummary t = e.Tick(TestData.World(d));
                rumors += t.GeneratedEvents.Count(ev => ev.Type == MarketEventType.MarketRumor);
                news += t.GeneratedEvents.Count(ev => ev.Type == MarketEventType.MarketNews);
            }

            double falseShare = 1 - (double)news / rumors;
            Assert.InRange(falseShare, 0.42, 0.58);
        }

        [Fact]
        public void ACommodity_HasOneStoryAtATime()
        {
            // Every day a rumor and a false rumor, and grain is the only commodity: only the first can land on it.
            MarketEngine e = Start(NewsOnGrain(1, 1, 0.5));
            for (int d = 2; d <= 60; d++)
            {
                TickSummary t = e.Tick(TestData.World(d));
                Assert.True(t.GeneratedEvents.Count(ev => ev.InstrumentId == Grain && (ev.Type == MarketEventType.MarketRumor || ev.Type == MarketEventType.MarketNews)) <= 1, $"day {d}");
                Assert.True(e.State.ScheduledNews.Count(n => n.InstrumentId == Grain) <= 1, $"day {d}");
            }
        }

        [Fact]
        public void Disabled_MakesNoNews()
        {
            MarketSettings s = NewsOnGrain(1, 1, 0.5);
            s.MarketNews.Enabled = false;
            MarketEngine e = Start(s);
            for (int d = 2; d <= 30; d++) Assert.Empty(e.Tick(TestData.World(d)).GeneratedEvents.Where(ev => ev.Type == MarketEventType.MarketNews || ev.Type == MarketEventType.MarketRumor));
        }

        [Fact]
        public void ScheduledNews_SurvivesASave()
        {
            MarketEngine e = Start(NewsOnGrain(1, 1, 0));
            e.Tick(TestData.World(2));
            var state = new ExchangeState { Market = e.State };

            ExchangeState back = StateSerializer.Deserialize(StateSerializer.Serialize(state));

            ScheduledMarketNews n = Assert.Single(back.Market.ScheduledNews);
            Assert.Equal(4, n.Day);
            Assert.Equal("blight", n.TemplateId);
            Assert.Equal(Grain, n.InstrumentId);
            Assert.Equal(0.3, n.Impulse, 9);
        }

        [Fact]
        public void RaidShock_IsScaledByTheVillagesShareOfWorldProduction()
        {
            MarketEngine e = Start(TestData.NoNoiseSettings());
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, Day = 1, ItemId = "grain", ItemShare = 0.02 });
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, Day = 1, ItemId = "salt", ItemShare = 0.14 });
            e.ApplyEvent(new MarketEvent { Type = MarketEventType.VillageLooted, Day = 1, ItemId = "iron" });
            e.Tick(TestData.World(2));

            Assert.Equal(0.05, e.Get("cx_grain").Shock, 9);
            Assert.Equal(0.35, e.Get("cx_salt").Shock, 9);
            Assert.Equal(0, e.Get("cx_iron").Shock, 9); // no share known: no shock
        }

        [Fact]
        public void Jumps_MoveThePriceEvenWithoutNoise()
        {
            MarketSettings s = TestData.NoNoiseSettings();
            ClassParameters c = s.Classes[InstrumentClass.Commodity];
            c.JumpChance = 1;
            c.JumpSigma = 0.05;
            MarketEngine e = Start(s);
            e.Tick(TestData.World(2));

            Assert.NotEqual(0, e.Get(Grain).LastReturn);
            Assert.Equal(0, e.Get("cx_sector_agriculture").LastReturn, 9);
        }

        [Fact]
        public void DefaultSettings_CommoditiesHaveFatTails()
        {
            MarketSettings s = TestData.Settings();
            s.Npc.Enabled = false;
            s.MarketNews.Enabled = false;
            s.EconomyWeight = 0;
            var returns = new List<double>();
            MarketEngine e = Start(s, 7);
            for (int d = 2; d <= 1001; d++)
            {
                e.Tick(TestData.World(d));
                foreach (InstrumentDefinition def in e.Catalog.Instruments.Where(x => x.Class == InstrumentClass.Commodity)) returns.Add(e.Get(def.Id).LastReturn);
            }

            double mean = returns.Average();
            double var = returns.Average(r => (r - mean) * (r - mean));
            double kurtosis = returns.Average(r => Math.Pow(r - mean, 4)) / (var * var);
            Assert.InRange(Math.Sqrt(var), 0.012, 0.025);
            Assert.True(kurtosis > 4, $"kurtosis {kurtosis:0.00}");
        }
    }
}
