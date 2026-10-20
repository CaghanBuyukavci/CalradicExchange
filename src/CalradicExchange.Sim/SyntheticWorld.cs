using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Random;

namespace CalradicExchange.Sim
{
    /// <summary>
    /// A fake Calradia that produces town prices and events with roughly the rhythm of a real campaign
    /// (ECONOMY_MODEL 8): ~0.8 raids/day, a siege every ~30 days, war/peace every ~60 days, seasons, random famine.
    /// Prices are base value x exp(x), where x is a mean-reverting log factor per (town, item).
    /// </summary>
    public sealed class SyntheticWorld
    {
        /// <summary>Game base values (v1.4.8).</summary>
        private static readonly Dictionary<string, double> BaseValues = new Dictionary<string, double>
        {
            ["grain"] = 10, ["fish"] = 12, ["olives"] = 30, ["date_fruit"] = 50, ["grape"] = 20, ["oil"] = 290,
            ["salt"] = 40, ["iron"] = 50, ["tools"] = 250, ["flax"] = 15, ["linen"] = 245, ["wool"] = 22,
            ["felt"] = 230, ["velvet"] = 575, ["cotton"] = 80, ["silver"] = 100, ["jewelry"] = 675, ["wine"] = 90,
            ["clay"] = 18, ["hardwood"] = 25, ["meat"] = 30, ["cheese"] = 40, ["butter"] = 25, ["beer"] = 50, ["sheep"] = 80,
            ["hog"] = 60, ["pottery"] = 210, ["planks"] = 180, ["hides"] = 50, ["leather"] = 230, ["fur"] = 400,
        };

        private static readonly HashSet<string> Foods = new HashSet<string> { "grain", "fish", "olives", "date_fruit", "grape" };

        /// <summary>Processed good -> raw input whose market moves it partly follows.</summary>
        private static readonly Dictionary<string, string> Inputs = new Dictionary<string, string>
        {
            ["oil"] = "olives", ["wine"] = "grape", ["linen"] = "flax", ["felt"] = "wool", ["velvet"] = "cotton",
            ["tools"] = "iron", ["jewelry"] = "silver", ["beer"] = "grain", ["pottery"] = "clay", ["planks"] = "hardwood",
            ["leather"] = "hides",
        };

        /// <summary>Village products and their relative frequency.</summary>
        private static readonly (string Item, int Weight)[] VillageProducts =
        {
            ("grain", 8), ("olives", 2), ("date_fruit", 2), ("grape", 3), ("fish", 3), ("flax", 2), ("wool", 3),
            ("cotton", 1), ("iron", 2), ("salt", 1), ("silver", 1), ("clay", 2), ("hardwood", 2),
            ("sheep", 1), ("hog", 1), ("hides", 1), ("fur", 1), ("cheese", 1), ("butter", 1), ("meat", 1),
        };

        private static readonly Dictionary<Season, double> FoodSeason = new Dictionary<Season, double>
        {
            [Season.Spring] = 0.03, [Season.Summer] = -0.03, [Season.Autumn] = -0.08, [Season.Winter] = 0.08,
        };

        public const int TownCount = 24;
        public const int KingdomCount = 6;
        public const int VillagesPerTown = 3;

        /// <summary>The real map has 274 villages (SandBox settlements.xml, v1.4.8); production shares are scaled to it.</summary>
        private const double RealVillagesPerSimVillage = 274.0 / (TownCount * VillagesPerTown);

        private const double LocalReversion = 0.08;
        private const double LocalSigma = 0.08;
        private const double GlobalReversion = 0.03;
        private const double GlobalSigma = 0.045;
        private const double RaidSupplyShock = 0.25;
        private const double FamineShock = 0.30;
        private const double RaidsPerDay = 0.8;
        private const int VillageRecoveryDays = 14;
        private const double SiegeRate = 1.0 / 30;
        private const double DiplomacyRate = 1.0 / 60;
        /// <summary>Daily chance per town to run out of food. Real v1.4.8 game: ~1 in 40 (11 of 53 towns in 10 days).</summary>
        private const double TownFamineRate = 1.0 / 40;

        private readonly XorShift128Plus _rng;
        private readonly CalendarInfo _calendar = CalendarInfo.Default;
        private readonly List<SimTown> _towns = new List<SimTown>();
        private readonly List<SimVillage> _villages = new List<SimVillage>();
        private readonly HashSet<(int, int)> _wars = new HashSet<(int, int)>();
        private readonly Dictionary<string, double> _global = new Dictionary<string, double>();
        private readonly IReadOnlyList<string> _categories;

        public SyntheticWorld(ulong seed, IReadOnlyList<string> categoryIds)
        {
            _rng = new XorShift128Plus(seed);
            _categories = categoryIds;
            foreach (string item in BaseValues.Keys) _global[item] = 0;

            for (int t = 0; t < TownCount; t++)
            {
                var town = new SimTown
                {
                    Id = $"town_{t:00}",
                    Kingdom = t % KingdomCount,
                    Prosperity = 1500 + _rng.NextDouble() * 4500,
                };
                foreach (string item in BaseValues.Keys)
                {
                    town.X[item] = Normal(0.15);
                    town.Volume[item] = 0.2 + _rng.NextDouble();
                }
                foreach (string cat in _categories) town.Cat[cat] = Normal(0.05);
                _towns.Add(town);

                for (int v = 0; v < VillagesPerTown; v++)
                {
                    var village = new SimVillage { Id = $"village_{t:00}_{v}", Town = town, Product = PickProduct() };
                    town.Volume[village.Product] = town.Volume.TryGetValue(village.Product, out double vol) ? vol + 4 : 4;
                    _villages.Add(village);
                }
            }
            _wars.Add((0, 1));
            _wars.Add((2, 4));
        }

        public static Season SeasonOf(int day) => (Season)(day / CalendarInfo.Default.DaysInSeason % 4);

        /// <summary>Moves the world one day and returns the events that happened.</summary>
        public List<MarketEvent> Advance(int day)
        {
            var events = new List<MarketEvent>();
            Season season = SeasonOf(day);
            double warIntensity = (double)_wars.Count / KingdomCount;

            foreach (string item in BaseValues.Keys)
            {
                _global[item] += -GlobalReversion * _global[item] + GlobalSigma * _rng.NextGaussian();
            }

            foreach (SimTown town in _towns)
            {
                town.Prosperity = Math.Max(500, town.Prosperity * Math.Exp(0.004 * _rng.NextGaussian()));
                foreach (string item in BaseValues.Keys)
                {
                    double mean = _global[item];
                    if (Foods.Contains(item)) mean += FoodSeason[season];
                    if (Inputs.TryGetValue(item, out string input)) mean += 0.3 * _global[input];
                    if (town.SiegeDaysLeft > 0 && Foods.Contains(item)) mean += 0.2;
                    town.X[item] += LocalReversion * (mean - town.X[item]) + LocalSigma * _rng.NextGaussian();
                }
                foreach (string cat in _categories)
                {
                    double mean = 0.08 * warIntensity;
                    town.Cat[cat] += 0.05 * (mean - town.Cat[cat]) + 0.015 * _rng.NextGaussian();
                }
            }

            Raids(day, events);
            Sieges(day, events);
            Diplomacy(day, events);
            Famine();
            return events;
        }

        public WorldSnapshot Snapshot(int day)
        {
            var world = new WorldSnapshot
            {
                Day = day,
                Calendar = _calendar,
                Season = SeasonOf(day),
                KingdomCount = KingdomCount,
                ActiveWarPairs = _wars.Count,
            };
            foreach (SimTown town in _towns)
            {
                var snap = new TownSnapshot
                {
                    Id = town.Id,
                    KingdomId = KingdomId(town.Kingdom),
                    Prosperity = town.Prosperity,
                    IsUnderSiege = town.SiegeDaysLeft > 0,
                    FoodStocks = town.FamineDaysLeft > 0 ? 0 : 50,
                };
                foreach (KeyValuePair<string, double> p in BaseValues)
                {
                    double factor = Math.Max(0.1, Math.Min(10, Math.Exp(town.X[p.Key])));
                    snap.ItemPrices[p.Key] = (decimal)Math.Round(p.Value * factor, 2);
                    snap.ItemVolumes[p.Key] = town.Volume[p.Key];
                }
                foreach (string cat in _categories)
                {
                    // The game clamps non-trade-good categories to [0.8, 1.3] (DefaultTradeItemPriceFactorModel).
                    snap.CategoryFactors[cat] = Math.Max(0.8, Math.Min(1.3, Math.Exp(town.Cat[cat])));
                }
                world.Towns.Add(snap);
            }
            return world;
        }

        private void Raids(int day, List<MarketEvent> events)
        {
            int raids = Poisson(RaidsPerDay);
            for (int i = 0; i < raids; i++)
            {
                events.Add(Loot(day, _villages[_rng.NextInt(_villages.Count)]));
            }
        }

        /// <summary>
        /// The player raids a village that produces <paramref name="itemId"/> and has not been looted for
        /// <see cref="VillageRecoveryDays"/> days. Returns null when no such village exists.
        /// </summary>
        public MarketEvent PlayerRaid(int day, string itemId)
        {
            List<SimVillage> targets = _villages.Where(v => v.Product == itemId && day - v.LastLootedDay >= VillageRecoveryDays).ToList();
            if (targets.Count == 0) return null;
            return Loot(day, targets[_rng.NextInt(targets.Count)]);
        }

        private MarketEvent Loot(int day, SimVillage v)
        {
            if (v.Town.X.ContainsKey(v.Product)) v.Town.X[v.Product] += RaidSupplyShock;
            v.LastLootedDay = day;
            return new MarketEvent
            {
                Type = MarketEventType.VillageLooted,
                Day = day,
                SettlementId = v.Id,
                ItemId = v.Product,
                TownId = v.Town.Id,
                KingdomId = KingdomId(v.Town.Kingdom),
                ItemShare = 1.0 / (_villages.Count(x => x.Product == v.Product) * RealVillagesPerSimVillage),
            };
        }

        private void Sieges(int day, List<MarketEvent> events)
        {
            foreach (SimTown town in _towns.Where(t => t.SiegeDaysLeft > 0))
            {
                town.SiegeDaysLeft--;
                if (town.SiegeDaysLeft > 0) continue;

                events.Add(new MarketEvent { Type = MarketEventType.SiegeEnded, Day = day, TownId = town.Id, KingdomId = KingdomId(town.Kingdom) });
                if (_rng.NextDouble() < 0.4)
                {
                    int loser = town.Kingdom;
                    town.Kingdom = town.Besieger;
                    events.Add(new MarketEvent
                    {
                        Type = MarketEventType.SettlementCaptured,
                        Day = day,
                        TownId = town.Id,
                        KingdomId = KingdomId(loser),
                        OtherKingdomId = KingdomId(town.Besieger),
                    });
                }
            }

            if (_wars.Count == 0 || _rng.NextDouble() >= SiegeRate) return;
            (int a, int b) war = _wars.ElementAt(_rng.NextInt(_wars.Count));
            int attacker = _rng.NextDouble() < 0.5 ? war.a : war.b;
            int defender = attacker == war.a ? war.b : war.a;
            List<SimTown> targets = _towns.Where(t => t.Kingdom == defender && t.SiegeDaysLeft == 0).ToList();
            if (targets.Count == 0) return;

            SimTown target = targets[_rng.NextInt(targets.Count)];
            target.SiegeDaysLeft = 7 + _rng.NextInt(14);
            target.Besieger = attacker;
            if (target.SiegeDaysLeft > 12 && _rng.NextDouble() < 0.3) target.FamineDaysLeft = target.SiegeDaysLeft - 10;
            events.Add(new MarketEvent { Type = MarketEventType.SiegeStarted, Day = day, TownId = target.Id, KingdomId = KingdomId(defender) });
        }

        private void Diplomacy(int day, List<MarketEvent> events)
        {
            if (_rng.NextDouble() >= DiplomacyRate) return;
            int a = _rng.NextInt(KingdomCount);
            int b = (a + 1 + _rng.NextInt(KingdomCount - 1)) % KingdomCount;
            (int, int) pair = a < b ? (a, b) : (b, a);
            bool atWar = _wars.Contains(pair);
            if (atWar) _wars.Remove(pair);
            else _wars.Add(pair);
            events.Add(new MarketEvent
            {
                Type = atWar ? MarketEventType.PeaceMade : MarketEventType.WarDeclared,
                Day = day,
                KingdomId = KingdomId(pair.Item1),
                OtherKingdomId = KingdomId(pair.Item2),
            });
        }

        private void Famine()
        {
            foreach (SimTown town in _towns)
            {
                if (town.FamineDaysLeft > 0)
                {
                    town.FamineDaysLeft--;
                    foreach (string food in Foods) town.X[food] += FamineShock / 10;
                }
            }
            foreach (SimTown town in _towns)
            {
                if (town.FamineDaysLeft == 0 && _rng.NextDouble() < TownFamineRate) town.FamineDaysLeft = 5 + _rng.NextInt(11);
            }
        }

        private string PickProduct()
        {
            int total = VillageProducts.Sum(p => p.Weight);
            int roll = _rng.NextInt(total);
            foreach ((string item, int weight) in VillageProducts)
            {
                if (roll < weight) return item;
                roll -= weight;
            }
            return "grain";
        }

        private int Poisson(double lambda)
        {
            double l = Math.Exp(-lambda), p = 1;
            int k = 0;
            do
            {
                k++;
                p *= _rng.NextDouble();
            }
            while (p > l);
            return k - 1;
        }

        private double Normal(double sd) => sd * _rng.NextGaussian();

        private static string KingdomId(int k) => $"kingdom_{k}";

        private sealed class SimTown
        {
            public string Id;
            public int Kingdom;
            public int Besieger;
            public double Prosperity;
            public int SiegeDaysLeft;
            public int FamineDaysLeft;
            public readonly Dictionary<string, double> X = new Dictionary<string, double>();
            public readonly Dictionary<string, double> Cat = new Dictionary<string, double>();
            public readonly Dictionary<string, double> Volume = new Dictionary<string, double>();
        }

        private sealed class SimVillage
        {
            public string Id;
            public SimTown Town;
            public string Product;
            public int LastLootedDay = -1000;
        }
    }
}
