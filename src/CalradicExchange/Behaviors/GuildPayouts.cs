using System;
using System.Collections.Generic;
using System.Linq;
using CalradicExchange.Adapters;
using CalradicExchange.Core.Config;
using CalradicExchange.Core.Guilds;
using CalradicExchange.Core.Model;
using CalradicExchange.Core.Pricing;
using CalradicExchange.Core.Trading;
using CalradicExchange.Diagnostics;
using CalradicExchange.Localization;
using CalradicExchange.Menus;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CalradicExchange.Behaviors
{
    /// <summary>
    /// Game side of guild partnership shares (PLAN 2.2-2.3, ADR-027): buys back shares of closed guilds, accrues the
    /// daily profit share, runs the weekly payout and the debt relation penalty. The money math is in Core (GuildLedger).
    /// </summary>
    public sealed class GuildPayouts
    {
        private readonly ExchangeCampaignBehavior _b;

        public GuildPayouts(ExchangeCampaignBehavior behavior)
        {
            _b = behavior;
        }

        private GuildSettings Settings => _b.Market.Settings.Guilds ?? new GuildSettings();

        /// <summary>Runs after each processed market tick.</summary>
        public void AfterTick(TickSummary summary, WorldSnapshot world)
        {
            foreach (string id in summary.ClosedInstruments) BuyBackClosed(id, summary.Day);
            foreach (string id in summary.ReopenedInstruments) ExchangeLog.Info($"Guild reopened: {id}");

            GuildLedger.Accrue(_b.Portfolio, _b.Market.GuildSeries, summary.Day, Settings.SharesPerGuild);

            int week = Math.Max(1, world.Calendar.DaysInWeek);
            if (summary.Day % week == 0) Settle(summary.Day);
        }

        private void BuyBackClosed(string id, int day)
        {
            int qty = _b.Portfolio.QuantityOf(id);
            PriceState ps = _b.Market.Get(id);
            if (qty <= 0 || ps == null) return;
            int gold = _b.Portfolio.ForceSell(id, qty, ps.Price, day, _b.Market.Settings.Trading.TradeHistoryLimit);
            if (gold > 0) GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, true);
            TextObject msg = Strings.GuildClosed;
            msg.SetTextVariable("NAME", Names.Instrument(_b.Market.Find(id)));
            msg.SetTextVariable("QTY", qty);
            msg.SetTextVariable("GOLD", gold);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Yellow));
            ExchangeLog.Info($"Guild closed: {id}, bought back {qty} shares at {ps.Price} for {gold}.");
        }

        private void Settle(int day)
        {
            Portfolio pf = _b.Portfolio;
            if (pf.GuildAccruals.Count == 0 && pf.GuildDebts.Count == 0) return;

            Hero hero = Hero.MainHero;
            GuildSettlement s = GuildLedger.Settle(pf, hero.Gold, id => _b.Market.Get(id)?.Price ?? 0, day, _b.Market.Settings.Trading.TradeHistoryLimit);

            if (s.GoldDelta > 0) GiveGoldAction.ApplyBetweenCharacters(null, hero, s.GoldDelta, true);
            else if (s.GoldDelta < 0) GiveGoldAction.ApplyBetweenCharacters(hero, null, Math.Min(-s.GoldDelta, hero.Gold), true);

            int profit = s.Lines.Where(l => l.Amount > 0).Sum(l => l.Amount);
            int loss = s.Lines.Where(l => l.Amount < 0).Sum(l => l.Amount);
            if (profit > 0) Show(Strings.GuildPayoutProfit.SetTextVariable("AMOUNT", Fmt.SignedDenars(profit)), Colors.Green);
            if (loss < 0) Show(Strings.GuildPayoutLoss.SetTextVariable("AMOUNT", Fmt.SignedDenars(loss)), Colors.Red);
            if (s.DebtRepaid > 0) Show(Strings.GuildDebtRepaid.SetTextVariable("AMOUNT", s.DebtRepaid), Colors.Yellow);
            foreach (GuildSettlementLine line in s.Lines)
            {
                string name = Names.Instrument(_b.Market.Find(line.InstrumentId));
                if (line.ForcedQuantity > 0)
                {
                    TextObject t = Strings.GuildForcedSale;
                    t.SetTextVariable("NAME", name);
                    t.SetTextVariable("QTY", line.ForcedQuantity);
                    t.SetTextVariable("GOLD", line.ForcedProceeds);
                    Show(t, Colors.Red);
                }
                if (line.NewDebt > 0)
                {
                    Show(Strings.GuildNewDebt.SetTextVariable("NAME", name).SetTextVariable("DEBT", line.NewDebt), Colors.Red);
                }
            }

            ExchangeLog.Info($"Guild payout day {day}: gold {s.GoldDelta:+0;-0}, profit {profit}, loss {loss}, debt repaid {s.DebtRepaid}, " +
                $"lines [{string.Join("; ", s.Lines.Select(l => $"{l.InstrumentId} {l.Amount:+0;-0} forced {l.ForcedQuantity}/{l.ForcedProceeds} debt+{l.NewDebt}"))}], " +
                $"debts [{string.Join("; ", s.DebtsAfter.Select(kv => $"{kv.Key} {kv.Value:0}"))}]. Gold now {hero.Gold}.");

            foreach (KeyValuePair<string, decimal> debt in s.DebtsAfter) PunishDebt(debt.Key, debt.Value);
            ExchangeMenus.Refresh();
        }

        /// <summary>
        /// While the player owes a guild, each payout week costs relation with the ruler of the guild's kingdom
        /// (the town owner clan's leader if the town has no kingdom), shown in the top-of-screen panel.
        /// </summary>
        private void PunishDebt(string instrumentId, decimal debt)
        {
            int penalty = Settings.DebtRelationPenalty;
            InstrumentDefinition def = _b.Market.Find(instrumentId);
            if (penalty <= 0 || def?.TownId == null) return;
            Settlement town = Settlement.Find(def.TownId);
            Hero ruler = (town?.MapFaction as Kingdom)?.Leader ?? town?.OwnerClan?.Leader;
            if (ruler == null || ruler == Hero.MainHero || !ruler.IsAlive) return;

            ChangeRelationAction.ApplyPlayerRelation(ruler, -penalty, false, false);
            TextObject t = Strings.GuildRelationLost;
            t.SetTextVariable("HERO", ruler.Name);
            t.SetTextVariable("CHANGE", penalty);
            t.SetTextVariable("GUILD", Names.Instrument(def));
            t.SetTextVariable("DEBT", Fmt.Denars(debt));
            MBInformationManager.AddQuickInformation(t, 0, ruler.CharacterObject);
            ExchangeLog.Info($"Guild debt penalty: relation with {ruler.StringId} -{penalty} for {instrumentId} (debt {debt:0}).");
        }

        private static void Show(TextObject text, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(text.ToString(), color));
        }
    }
}
