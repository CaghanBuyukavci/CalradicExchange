using System;
using System.Collections.Generic;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using Newtonsoft.Json;

namespace CalradicExchange.Core.Config
{
    /// <summary>Parses and validates settings.json.</summary>
    public static class SettingsLoader
    {
        public const string FileName = "settings.json";

        public static MarketSettings Parse(string json)
        {
            MarketSettings s;
            try
            {
                s = JsonConvert.DeserializeObject<MarketSettings>(json ?? string.Empty, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    Culture = System.Globalization.CultureInfo.InvariantCulture,
                });
            }
            catch (JsonException ex)
            {
                throw new ConfigException(FileName, "could not be read: " + ex.Message, ex);
            }
            if (s == null) throw new ConfigException(FileName, "is empty.");

            var errors = new List<string>();
            Validate(s, errors);
            if (errors.Count > 0) throw new ConfigException(FileName, errors);
            return s;
        }

        private static void Validate(MarketSettings s, List<string> e)
        {
            Range(e, "volatilityMultiplier", s.VolatilityMultiplier, 0, 5);
            Range(e, "maxDailyLogReturn", s.MaxDailyLogReturn, 0.01, 1);
            Range(e, "maxAbsShock", s.MaxAbsShock, 0.01, 5);
            Range(e, "historyDays", s.HistoryDays, 7, 2000);
            Range(e, "newsKeep", s.NewsKeep, 0, 500);
            Range(e, "famineCooldownDays", s.FamineCooldownDays, 0, 1000);
            Range(e, "shockBaselineHalfLifeDays", s.ShockBaselineHalfLifeDays, 0, 10000);
            Range(e, "economyWeight", s.EconomyWeight, 0, 2);
            Range(e, "economyHalfLifeDays", s.EconomyHalfLifeDays, 0, 1000);
            Range(e, "economyMaxAbs", s.EconomyMaxAbs, 0, 2);
            Range(e, "economyMaxDailyMove", s.EconomyMaxDailyMove, 0, 1);
            Range(e, "anchorHalfLifeDays", s.AnchorHalfLifeDays, 0, 10000);

            foreach (InstrumentClass cls in Enum.GetValues(typeof(InstrumentClass)))
            {
                if (s.Classes == null || !s.Classes.TryGetValue(cls, out ClassParameters p) || p == null)
                {
                    e.Add($"classes.{cls} is missing.");
                    continue;
                }
                string w = $"classes.{cls}";
                RangeOpenLow(e, w + ".kappa", p.Kappa, 0, 1);
                Range(e, w + ".sigma", p.Sigma, 0, 0.5);
                Range(e, w + ".spread", p.Spread, 0, 0.5);
                RangeOpenLow(e, w + ".depth", p.Depth, 0, 1e9);
                Range(e, w + ".lambda", p.Lambda, 0, 10);
            }

            if (s.Guilds != null) Range(e, "guilds.profitWindowDays", s.Guilds.ProfitWindowDays, 1, 336);
            if (s.Npc != null)
            {
                RangeOpenLow(e, "npc.supplyDepthMultiple", s.Npc.SupplyDepthMultiple, 0, 1000);
                Range(e, "npc.initialHoldingFraction", s.Npc.InitialHoldingFraction, 0, 0.9);
                RangeOpenLow(e, "npc.impactMultiplierMin", s.Npc.ImpactMultiplierMin, 0, 1);
                Range(e, "npc.impactMultiplierMax", s.Npc.ImpactMultiplierMax, 1, 100);
                Range(e, "npc.flowNoise", s.Npc.FlowNoise, 0, 5);
                Range(e, "npc.flowMomentum", s.Npc.FlowMomentum, -10, 10);
                Range(e, "npc.holdingReversion", s.Npc.HoldingReversion, 0, 10);
                Range(e, "npc.holdingReversionChance", s.Npc.HoldingReversionChance, 0, 1);
                Range(e, "npc.whaleDailyChance", s.Npc.WhaleDailyChance, 0, 1);
                Range(e, "npc.whaleMinDepths", s.Npc.WhaleMinDepths, 0, 100);
                Range(e, "npc.whaleMaxDepths", s.Npc.WhaleMaxDepths, s.Npc.WhaleMinDepths, 100);
                Range(e, "npc.whaleBuyChance", s.Npc.WhaleBuyChance, 0, 1);
            }
            if (s.Orders != null)
            {
                Range(e, "orders.feeRate", s.Orders.FeeRate, 0, 0.5);
                Range(e, "orders.feePerDay", s.Orders.FeePerDay, 0, 0.1);
                Range(e, "orders.minFee", s.Orders.MinFee, 0, 100000);
                Range(e, "orders.maxDays", s.Orders.MaxDays, 1, 336);
                Range(e, "orders.maxTakeProfitPercent", s.Orders.MaxTakeProfitPercent, 1, 1000);
                Range(e, "orders.maxStopLossPercent", s.Orders.MaxStopLossPercent, 1, 99);
                Range(e, "orders.ordersKept", s.Orders.OrdersKept, 1, 1000);
            }
            if (s.Kingdoms != null) Range(e, "kingdoms.minListingDays", s.Kingdoms.MinListingDays, 0, 336);
            if (s.Trading == null) e.Add("trading is missing.");
            else
            {
                Range(e, "trading.commissionRate", s.Trading.CommissionRate, 0, 0.5);
                Range(e, "trading.minCommission", s.Trading.MinCommission, 0, 100000);
                Range(e, "trading.profitTaxRate", s.Trading.ProfitTaxRate, 0, 1);
                Range(e, "trading.permanentImpactFactor", s.Trading.PermanentImpactFactor, 0, 1);
                Range(e, "trading.tradeHistoryLimit", s.Trading.TradeHistoryLimit, 0, 10000);
            }

            if (s.Limits == null) e.Add("limits is missing.");
            else
            {
                Range(e, "limits.baseTotal", s.Limits.BaseTotal, 0, int.MaxValue);
                Range(e, "limits.perClanTier", s.Limits.PerClanTier, 0, int.MaxValue);
                Range(e, "limits.perTradeSkill", s.Limits.PerTradeSkill, 0, int.MaxValue);
                RangeOpenLow(e, "limits.perInstrumentDepthMultiple", s.Limits.PerInstrumentDepthMultiple, 0, 100);
            }

            foreach (Season season in Enum.GetValues(typeof(Season)))
            {
                if (s.SeasonImpulses == null || !s.SeasonImpulses.ContainsKey(season)) e.Add($"seasonImpulses.{season} is missing.");
            }

            if (s.EventShocks == null) s.EventShocks = new Dictionary<MarketEventType, List<ShockRule>>();
            foreach (KeyValuePair<MarketEventType, List<ShockRule>> pair in s.EventShocks)
            {
                if (pair.Value == null) continue;
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    ShockRule r = pair.Value[i];
                    string w = $"eventShocks.{pair.Key}[{i}]";
                    if (!ShockMapper.IsValidTarget(r.Target)) e.Add($"{w}.target '{r.Target}' is not a known target.");
                    RangeOpenLow(e, w + ".halfLifeDays", r.HalfLifeDays, 0, 10000);
                    Range(e, w + ".impulse", r.Impulse, -5, 5);
                    if (r.VolMultiplier != 0) Range(e, w + ".volMultiplier", r.VolMultiplier, 1, 10);
                    Range(e, w + ".volDays", r.VolDays, 0, 10000);
                    Range(e, w + ".townShareMultiplier", r.TownShareMultiplier, 0, 1000);
                }
            }
        }

        private static void Range(List<string> e, string name, double v, double min, double max)
        {
            if (double.IsNaN(v) || v < min || v > max) e.Add($"{name} must be in [{min}, {max}], got {v}.");
        }

        private static void RangeOpenLow(List<string> e, string name, double v, double min, double max)
        {
            if (double.IsNaN(v) || v <= min || v > max) e.Add($"{name} must be in ({min}, {max}], got {v}.");
        }
    }
}
