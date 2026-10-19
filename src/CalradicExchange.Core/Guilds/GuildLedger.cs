using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Core.Trading;

namespace CalradicExchange.Core.Guilds
{
    /// <summary>
    /// The player's share of guild profit and loss (ADR-027). Pure: gold is moved by the game layer.
    /// Accrue runs every day after the market tick; Settle runs on payout days.
    /// </summary>
    public static class GuildLedger
    {
        /// <summary>
        /// Adds today's profit share of every held guild: today's profit x shares held / shares per guild.
        /// Only guilds whose profit was measured today count.
        /// </summary>
        public static void Accrue(Portfolio portfolio, Func<string, GuildProfitSeries> seriesOf, int day, int sharesPerGuild)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            foreach (Position pos in portfolio.Positions)
            {
                if (pos.Quantity <= 0 || !GuildInstruments.IsGuildId(pos.InstrumentId)) continue;
                GuildProfitSeries series = seriesOf(pos.InstrumentId);
                if (series == null || series.LastProfitDay != day || series.DailyProfit.Count == 0) continue;
                double today = series.DailyProfit[series.DailyProfit.Count - 1];
                decimal share = (decimal)today * pos.Quantity / Math.Max(1, sharesPerGuild);
                portfolio.GuildAccruals.TryGetValue(pos.InstrumentId, out decimal sum);
                portfolio.GuildAccruals[pos.InstrumentId] = Math.Round(sum + share, 4);
            }
        }

        /// <summary>
        /// Pays out the accrued profit and collects the accrued losses (ADR-027):
        /// 1. profits first repay older debts, the rest goes to the player;
        /// 2. each loss is paid from gold (incl. this week's profit), then by force-selling that guild's shares at
        ///    <paramref name="priceOf"/> without fees, and what is still missing becomes a debt to that guild.
        /// Changes the portfolio (accruals cleared, debts, forced sales). Returns the gold change and the details.
        /// </summary>
        public static GuildSettlement Settle(Portfolio portfolio, int gold, Func<string, decimal> priceOf, int day, int historyLimit)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            var result = new GuildSettlement { Day = day };
            List<KeyValuePair<string, int>> amounts = portfolio.GuildAccruals
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new KeyValuePair<string, int>(kv.Key, (int)Math.Round(kv.Value, MidpointRounding.AwayFromZero)))
                .ToList();
            portfolio.GuildAccruals.Clear();

            int income = amounts.Where(a => a.Value > 0).Sum(a => a.Value);
            foreach (KeyValuePair<string, int> a in amounts.Where(x => x.Value > 0)) result.Lines.Add(new GuildSettlementLine { InstrumentId = a.Key, Amount = a.Value });

            // 1. Profit repays old debts first.
            foreach (string id in portfolio.GuildDebts.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList())
            {
                if (income <= 0) break;
                int owed = (int)Math.Ceiling(portfolio.GuildDebts[id]);
                int repay = Math.Min(owed, income);
                income -= repay;
                result.DebtRepaid += repay;
                ReduceDebt(portfolio, id, repay);
            }

            // 2. Losses: gold (plus what is left of the income), then that guild's shares, then debt.
            int cash = Math.Max(0, gold) + income;
            foreach (KeyValuePair<string, int> a in amounts.Where(x => x.Value < 0))
            {
                var line = new GuildSettlementLine { InstrumentId = a.Key, Amount = a.Value };
                int loss = -a.Value;
                int paid = Math.Min(loss, cash);
                cash -= paid;
                loss -= paid;
                if (loss > 0)
                {
                    decimal price = priceOf(a.Key);
                    int held = portfolio.QuantityOf(a.Key);
                    if (price > 0 && held > 0)
                    {
                        int qty = Math.Min(held, (int)Math.Ceiling(loss / (double)price - 1e-9));
                        int proceeds = portfolio.ForceSell(a.Key, qty, price, day, historyLimit);
                        line.ForcedQuantity = qty;
                        line.ForcedProceeds = proceeds;
                        int covered = Math.Min(loss, proceeds);
                        loss -= covered;
                        cash += proceeds - covered;
                    }
                }
                if (loss > 0)
                {
                    portfolio.GuildDebts.TryGetValue(a.Key, out decimal debt);
                    portfolio.GuildDebts[a.Key] = debt + loss;
                    line.NewDebt = loss;
                }
                result.Lines.Add(line);
            }

            result.GoldDelta = cash - Math.Max(0, gold);
            portfolio.GuildIncomeTotal += amounts.Sum(a => a.Value);
            result.DebtsAfter = portfolio.GuildDebts.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            return result;
        }

        private static void ReduceDebt(Portfolio portfolio, string id, int amount)
        {
            decimal left = portfolio.GuildDebts[id] - amount;
            if (left <= 0) portfolio.GuildDebts.Remove(id);
            else portfolio.GuildDebts[id] = left;
        }
    }
}
