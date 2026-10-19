using System.Collections.Generic;
using CalradicExchange.Core.Guilds;
using CalradicExchange.Core.Model;
using Xunit;

namespace CalradicExchange.Core.Tests
{
    public class GuildProfitTrackerTests
    {
        private static WorldSnapshot World(int day, params (string town, string type, int count, double profit)[] groups)
        {
            var world = new WorldSnapshot { Day = day };
            var towns = new Dictionary<string, TownSnapshot>();
            foreach (var g in groups)
            {
                if (!towns.TryGetValue(g.town, out TownSnapshot t))
                {
                    t = new TownSnapshot { Id = g.town };
                    towns[g.town] = t;
                    world.Towns.Add(t);
                }
                t.Workshops[g.type] = new WorkshopGroupSnapshot { Count = g.count, DailyProfit = g.profit };
            }
            return world;
        }

        [Fact]
        public void NewPairs_AreCreated_AndAveraged()
        {
            var guilds = new List<GuildProfitSeries>();
            GuildProfitTracker.Record(guilds, World(1, ("pravend", "linen_weavery", 2, 30)), 28);
            GuildProfitTracker.Record(guilds, World(2, ("pravend", "linen_weavery", 2, 50)), 28);

            GuildProfitSeries g = Assert.Single(guilds);
            Assert.Equal(new List<double> { 30, 50 }, g.DailyProfit);
            Assert.Equal(40, g.AverageDailyProfit, 9);
            Assert.Equal(2, g.WorkshopCount);
            Assert.Equal(2, g.LastDay);
        }

        [Fact]
        public void History_IsTrimmedToTheWindow()
        {
            var guilds = new List<GuildProfitSeries>();
            for (int d = 1; d <= 5; d++) GuildProfitTracker.Record(guilds, World(d, ("t", "smithy", 1, d)), 3);

            Assert.Equal(new List<double> { 3, 4, 5 }, Assert.Single(guilds).DailyProfit);
        }

        [Fact]
        public void MissingPair_DropsToZeroWorkshops_ButKeepsItsHistory()
        {
            var guilds = new List<GuildProfitSeries>();
            GuildProfitTracker.Record(guilds, World(1, ("t", "smithy", 1, 20), ("t", "brewery", 1, 10)), 28);
            GuildProfitTracker.Record(guilds, World(2, ("t", "brewery", 1, 10)), 28);

            GuildProfitSeries smithy = guilds.Find(g => g.WorkshopTypeId == "smithy");
            Assert.Equal(0, smithy.WorkshopCount);
            Assert.Equal(1, smithy.LastDay);
            Assert.Equal(new List<double> { 20 }, smithy.DailyProfit);
        }

        [Fact]
        public void SameDayTwice_IsRecordedOnce_AndLossesStayNegative()
        {
            var guilds = new List<GuildProfitSeries>();
            GuildProfitTracker.Record(guilds, World(4, ("t", "smithy", 1, -15)), 28);
            GuildProfitTracker.Record(guilds, World(4, ("t", "smithy", 1, 99)), 28);

            GuildProfitSeries g = Assert.Single(guilds);
            Assert.Equal(new List<double> { -15 }, g.DailyProfit);
            Assert.Equal(-15, g.AverageDailyProfit, 9);
            Assert.Equal(1, g.WorkshopCount);
        }

        [Fact]
        public void UnmeasuredDay_UpdatesCount_ButAppendsNoProfit()
        {
            var guilds = new List<GuildProfitSeries>();
            WorldSnapshot first = World(1, ("t", "smithy", 2, 0));
            first.Towns[0].Workshops["smithy"].HasProfit = false; // first reading after a load
            GuildProfitTracker.Record(guilds, first, 28);
            GuildProfitTracker.Record(guilds, World(2, ("t", "smithy", 2, 30)), 28);

            GuildProfitSeries g = Assert.Single(guilds);
            Assert.Equal(new List<double> { 30 }, g.DailyProfit);
            Assert.Equal(2, g.WorkshopCount);
            Assert.Equal(2, g.LastDay);
        }

        [Fact]
        public void EmptyGroups_AreIgnored()
        {
            var guilds = new List<GuildProfitSeries>();
            GuildProfitTracker.Record(guilds, World(1, ("t", "smithy", 0, 0)), 28);
            Assert.Empty(guilds);
        }
    }
}
