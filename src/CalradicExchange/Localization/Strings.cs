using CalradicExchange.Core.Model;
using CalradicExchange.Core.Trading;
using TaleWorlds.Localization;

namespace CalradicExchange.Localization
{
    /// <summary>
    /// Player-facing strings. English text is the fallback; translations live in
    /// ModuleData/Languages/&lt;LANG&gt;/cx_strings_*.xml keyed by the {=id}.
    /// Each property returns a fresh TextObject so variables never leak between uses.
    /// </summary>
    public static class Strings
    {
        // Town menu
        public const string MenuVisitExchange = "{=cx_menu_visit_exchange}Visit the Exchange";

        // Exchange menu
        public static TextObject ExchangeMenuText => new TextObject("{=cx_menu_exchange_text}Money changers sit behind their tables in the back room of the inn, calling out prices for shares and baskets.\n \nYour holdings: {VALUE} denars ({PNL}).\nInvestment limit used: {USED} / {LIMIT} denars.\nBroker's fee: {FEE}. Tax on profit: {TAX}.");
        public static TextObject ExchangeClosedText => new TextObject("{=cx_menu_exchange_closed}The exchange is closed today. ({REASON})");
        public const string OptMarkets = "{=cx_opt_markets}Markets";
        public const string OptPortfolio = "{=cx_opt_portfolio}Your portfolio";
        public const string OptNews = "{=cx_opt_news}News";
        public const string OptHistory = "{=cx_opt_history}Your trade history";
        public const string OptLeave = "{=cx_opt_leave}Leave";
        public static TextObject ClosedReasonData => new TextObject("{=cx_closed_data}its ledgers could not be read, see exchange.log");
        public static TextObject ClosedReasonPrices => new TextObject("{=cx_closed_prices}no prices yet");

        // Generic buttons
        public static TextObject Close => new TextObject("{=cx_btn_close}Close");
        public static TextObject Confirm => new TextObject("{=cx_btn_confirm}Confirm");
        public static TextObject Cancel => new TextObject("{=cx_btn_cancel}Cancel");
        public static TextObject Continue => new TextObject("{=cx_btn_continue}Continue");

        // Market report
        public static TextObject HeaderCommodities => new TextObject("{=cx_hdr_commodities}Commodity shares (1 share = {UNITS} units)");
        // Table column headers
        public static TextObject ColInstrument => new TextObject("{=cx_col_instrument}Instrument");
        public static TextObject ColPrice => new TextObject("{=cx_col_price}Price");
        public static TextObject ColToday => new TextObject("{=cx_col_today}Today");
        public static TextObject ColWeek => new TextObject("{=cx_col_week}7 days");
        public static TextObject ColQuantity => new TextObject("{=cx_col_quantity}Qty");
        public static TextObject ColBoughtAt => new TextObject("{=cx_col_bought_at}Bought at");
        public static TextObject ColNow => new TextObject("{=cx_col_now}Now");
        public static TextObject ColProfit => new TextObject("{=cx_col_profit}P/L");
        public static TextObject ColProfitPct => new TextObject("{=cx_col_profit_pct}P/L %");
        public static TextObject ColHeld => new TextObject("{=cx_col_held}Held");
        public static TextObject ColGuild => new TextObject("{=cx_col_guild}Guild");
        public static TextObject ColWeekProfitPerShare => new TextObject("{=cx_col_week_profit}Week profit/share");
        public static TextObject ColDate => new TextObject("{=cx_col_date}Date");
        public static TextObject ColSide => new TextObject("{=cx_col_side}Trade");
        public static TextObject ColTotal => new TextObject("{=cx_col_total}Total");
        public static TextObject ColCosts => new TextObject("{=cx_col_costs}Fees");
        public static TextObject NoPrice => new TextObject("{=cx_no_price_short}-");


        // Buy / sell flow
        public static TextObject BuyTitle => new TextObject("{=cx_buy_title}Buy shares");
        public static TextObject CommodityMarketTitle => new TextObject("{=cx_commodity_market_title}Commodity market");
        public static TextObject BasketMarketTitle => new TextObject("{=cx_basket_market_title}Basket market");
        public static TextObject GuildMarketTitle => new TextObject("{=cx_guild_market_title}Guild market");
        public static TextObject KingdomMarketTitle => new TextObject("{=cx_kingdom_market_title}Index market");
        public static TextObject MarketTabShares => new TextObject("{=cx_mtab_shares}Shares");
        public static TextObject MarketTabBaskets => new TextObject("{=cx_mtab_baskets}Baskets");
        public static TextObject MarketTabGuilds => new TextObject("{=cx_mtab_guilds}Partnerships");
        public static TextObject MarketTabIndices => new TextObject("{=cx_mtab_indices}Indices");
        public static TextObject KingdomMarketDescription => new TextObject("{=cx_kingdom_market_desc}Indices of the kingdoms of Calradia. They rise with the kingdom's prosperity, towns, castles, villages, armies and clans and with its victories, and fall with its wars, sieges, raids, defeats and the deaths of its rulers. If a kingdom is destroyed, its index is worth nothing.");
        public static TextObject KingdomIndexName => new TextObject("{=cx_kingdom_index_name}{KINGDOM} index");
        public static TextObject KingdomFallen => new TextObject("{=cx_kingdom_fallen}{KINGDOM} has fallen. Your {QTY} shares of its index are worthless: {LOSS} denars lost.");
        public static TextObject GuildMarketDescription => new TextObject("{=cx_guild_market_desc}Partnership shares in the workshops of this town. Every 7 days each share brings its part of the guild's profit, or of its loss. Shares of other towns can be sold anywhere.");
        public static TextObject GuildSectionLocal => new TextObject("{=cx_guild_section_local}Guilds of {TOWN}");
        public static TextObject OwnWorkshopSold => new TextObject("{=cx_own_workshop_sold}You now own a workshop of {NAME}. Your {QTY} partnership shares were sold at the current price for {TOTAL} denars ({PNL}).");
        public static TextObject OwnWorkshopMark => new TextObject("{=cx_own_workshop_mark}{NAME} (your workshop)");
        public static TextObject GuildSectionOther => new TextObject("{=cx_guild_section_other}Your shares in other towns");
        public static TextObject GuildNoneHere => new TextObject("{=cx_guild_none_here}No guild of this town is listed yet.");
        public static TextObject GuildName => new TextObject("{=cx_guild_name}{TOWN} {GUILD}");
        public static TextObject BuyGuildTitle => new TextObject("{=cx_buy_guild_title}Buy partnership shares");
        public static TextObject SellGuildTitle => new TextObject("{=cx_sell_guild_title}Sell partnership shares");
        public static TextObject QuantityBuyGuild => new TextObject("{=cx_qty_buy_guild}A partnership share of {NAME} costs {PRICE} before fees.\nHow many shares? (at most {MAX})");
        public static TextObject QuantitySellGuild => new TextObject("{=cx_qty_sell_guild}You hold {QTY} partnership shares of {NAME}.\nHow many do you sell?");
        public static TextObject TradeDescription => new TextObject("{=cx_trade_desc}Gold: {GOLD} denars\nLimit left: {LEFT} denars");
        public static TextObject BtnBuy => new TextObject("{=cx_btn_buy}BUY");
        public static TextObject BtnSell => new TextObject("{=cx_btn_sell}SELL");
        public static TextObject SellTitle => new TextObject("{=cx_sell_title}Sell shares");
        public static TextObject BuyElement => new TextObject("{=cx_buy_element}{NAME} - {PRICE}");
        public static TextObject QuantityBuy => new TextObject("{=cx_qty_buy}{NAME} costs {PRICE} per share before fees.\nHow many shares? (at most {MAX})");
        public static TextObject QuantitySell => new TextObject("{=cx_qty_sell}You hold {QTY} shares of {NAME}.\nHow many do you sell?");
        public static TextObject QuantityEstimateBuy => new TextObject("{=cx_qty_estimate_buy}To pay: {TOTAL} denars (broker's fee {FEE})");
        public static TextObject QuantityEstimateSell => new TextObject("{=cx_qty_estimate_sell}You receive: {TOTAL} denars (broker's fee {FEE}, tax {TAX})");
        public static TextObject QuantityInvalid => new TextObject("{=cx_qty_invalid}Enter a whole number from 1 to {MAX}.");
        public static TextObject ConfirmBuyHeading => new TextObject("{=cx_cf_heading_buy}Buy {QTY} x {NAME}");
        public static TextObject ConfirmSellHeading => new TextObject("{=cx_cf_heading_sell}Sell {QTY} x {NAME}");
        public static TextObject CfMarketPrice => new TextObject("{=cx_cf_market}Market price");
        public static TextObject CfExecPrice => new TextObject("{=cx_cf_exec}After spread and slippage");
        public static TextObject CfAverageCost => new TextObject("{=cx_cf_avg}Your average cost");
        public static TextObject CfGross => new TextObject("{=cx_cf_gross}Shares");
        public static TextObject CfFee => new TextObject("{=cx_cf_fee}Broker's fee");
        public static TextObject CfTax => new TextObject("{=cx_cf_tax}Tax on profit");
        public static TextObject CfTotalPay => new TextObject("{=cx_cf_pay}Total to pay");
        public static TextObject CfTotalReceive => new TextObject("{=cx_cf_receive}You receive");
        public static TextObject CfProfit => new TextObject("{=cx_cf_profit}Profit / loss");
        public static TextObject DenarsAmount => new TextObject("{=cx_denars_amount}{AMOUNT} denars");
        public static TextObject ConfirmBuy => new TextObject("{=cx_confirm_buy}Buy {QTY} x {NAME}\n \nMarket price: {PRICE}\nAfter spread and slippage: {EXEC}\nShares: {GROSS} denars\nBroker's fee: {FEE} denars\n \nTotal to pay: {TOTAL} denars");
        public static TextObject ConfirmSell => new TextObject("{=cx_confirm_sell}Sell {QTY} x {NAME}\n \nMarket price: {PRICE}\nAfter spread and slippage: {EXEC}\nShares: {GROSS} denars\nBroker's fee: {FEE} denars\nTax on profit: {TAX} denars\n \nYou receive: {TOTAL} denars");
        public static TextObject Bought => new TextObject("{=cx_msg_bought}Bought {QTY} {NAME} for {TOTAL} denars.");
        public static TextObject Sold => new TextObject("{=cx_msg_sold}Sold {QTY} {NAME} for {TOTAL} denars ({PNL}).");
        public static TextObject Rebased => new TextObject("{=cx_msg_rebased}The exchange has rewritten its ledgers. Your shares were bought back at market price: {GOLD} denars.");
        public static TextObject BigMove => new TextObject("{=cx_msg_big_move}Exchange: {NAME} {CHANGE} ({PRICE}).");

        // Portfolio
        public static TextObject PortfolioTitle => new TextObject("{=cx_portfolio_title}My portfolio");
        public static TextObject PortfolioEmpty => new TextObject("{=cx_portfolio_empty}You hold no shares.");
        public static TextObject PortfolioTabEmpty => new TextObject("{=cx_portfolio_tab_empty}You hold nothing of this kind.");
        public static TextObject TabShares => new TextObject("{=cx_tab_shares}Shares ({COUNT})");
        public static TextObject TabBaskets => new TextObject("{=cx_tab_baskets}Baskets ({COUNT})");
        public static TextObject TabGuilds => new TextObject("{=cx_tab_guilds}Partnerships ({COUNT})");
        public static TextObject TabKingdoms => new TextObject("{=cx_tab_kingdoms}Indices ({COUNT})");
        public static TextObject ColKingdom => new TextObject("{=cx_col_kingdom}Kingdom indices");
        public static TextObject ColBasket => new TextObject("{=cx_col_basket}Basket");
        public static TextObject PortfolioLine => new TextObject("{=cx_portfolio_line}{NAME}: {QTY} shares, bought at {AVG}, now {PRICE}, {PNL}");
        public static TextObject PortfolioTotals => new TextObject("{=cx_portfolio_totals}Value {VALUE} denars, open profit {OPEN}.\nRealized profit {REALIZED}. Fees paid {FEES}, taxes paid {TAXES} denars.");

        // Trade history
        public static TextObject HistoryTitle => new TextObject("{=cx_history_title}Your trade history");
        public static TextObject HistorySessionTrades => new TextObject("{=cx_history_session_trades}Trades this session: {COUNT}");
        public static TextObject HistoryEmpty => new TextObject("{=cx_history_empty}You have not traded yet.");
        public static TextObject HistoryTotals => new TextObject("{=cx_history_totals}{COUNT} trades. Realized profit {REALIZED}. Fees paid {FEES}, taxes paid {TAXES} denars.");
        public static TextObject SideBuy => new TextObject("{=cx_side_buy}Buy");
        public static TextObject SideSell => new TextObject("{=cx_side_sell}Sell");
        public static TextObject SideForced => new TextObject("{=cx_side_forced}Forced sale");

        // Guild payouts (ADR-027)
        public static TextObject GuildPayoutProfit => new TextObject("{=cx_guild_payout_profit}Your weekly share of guild profits: {AMOUNT} denars.");
        public static TextObject GuildPayoutLoss => new TextObject("{=cx_guild_payout_loss}Your share of guild losses this week: {AMOUNT} denars.");
        public static TextObject GuildDebtRepaid => new TextObject("{=cx_guild_debt_repaid}{AMOUNT} denars of guild profit went to your guild debts.");
        public static TextObject GuildForcedSale => new TextObject("{=cx_guild_forced_sale}{NAME}: {QTY} of your shares were sold for {GOLD} denars to cover its loss.");
        public static TextObject GuildNewDebt => new TextObject("{=cx_guild_new_debt}You now owe {NAME} {DEBT} denars. It will be taken from your next guild profits.");
        public static TextObject GuildRelationLost => new TextObject("{=cx_guild_relation_lost}Your relation with {HERO} decreased by {CHANGE}: you owe {GUILD} {DEBT} denars.");
        public static TextObject GuildClosed => new TextObject("{=cx_guild_closed}{NAME} has closed: no such workshop is left in the town. Your {QTY} shares were bought back for {GOLD} denars.");
        public static TextObject MenuGuildDebt => new TextObject("{=cx_menu_guild_debt}Guild debt: {DEBT} denars.");
        public static TextObject PortfolioGuildTotals => new TextObject("{=cx_portfolio_guild_totals}Guild profit received {INCOME} denars, still accruing {PENDING}. Guild debt {DEBT} denars.");

        // News
        public static TextObject NewsTitle => new TextObject("{=cx_news_title}News at the exchange");
        public static TextObject NewsEmpty => new TextObject("{=cx_news_empty}No news has reached the exchange yet.");
        public static TextObject NewsToday => new TextObject("{=cx_news_today}Today");
        public static TextObject NewsDaysAgo => new TextObject("{=cx_news_days_ago}{DAYS} days ago");
        public static TextObject NewsLine => new TextObject("{=cx_news_line}{WHEN}: {TEXT}");
        public static TextObject ColWhen => new TextObject("{=cx_col_when}When");
        public static TextObject ColNewsKind => new TextObject("{=cx_col_news_kind}Kind");
        public static TextObject ColNews => new TextObject("{=cx_col_news}News");

        public static TextObject NewsCategory(MarketEventType type)
        {
            switch (type)
            {
                case MarketEventType.VillageLooted: return new TextObject("{=cx_news_cat_raid}Raid");
                case MarketEventType.SiegeStarted:
                case MarketEventType.SiegeEnded: return new TextObject("{=cx_news_cat_siege}Siege");
                case MarketEventType.SettlementCaptured: return new TextObject("{=cx_news_cat_conquest}Conquest");
                case MarketEventType.WarDeclared: return new TextObject("{=cx_news_cat_war}War");
                case MarketEventType.PeaceMade: return new TextObject("{=cx_news_cat_peace}Peace");
                case MarketEventType.SeasonChanged: return new TextObject("{=cx_news_cat_season}Season");
                case MarketEventType.FamineStarted: return new TextObject("{=cx_news_cat_famine}Famine");
                case MarketEventType.KingdomDestroyed: return new TextObject("{=cx_news_cat_fall}Fall");
                case MarketEventType.HeroCaptured: return new TextObject("{=cx_news_cat_captive}Captive");
                case MarketEventType.CastleCaptured: return new TextObject("{=cx_news_cat_conquest}Conquest");
                case MarketEventType.HeroDied: return new TextObject("{=cx_news_cat_death}Death");
                case MarketEventType.BattleEnded: return new TextObject("{=cx_news_cat_battle}Battle");
                case MarketEventType.WarEnded: return new TextObject("{=cx_news_cat_war_end}War's end");
                case MarketEventType.ClanChangedKingdom: return new TextObject("{=cx_news_cat_clan}Clan");
                case MarketEventType.WhaleTrade: return new TextObject("{=cx_news_cat_whale}Whale");
                default: return new TextObject(type.ToString());
            }
        }

        public static TextObject EventText(MarketEventType type)
        {
            switch (type)
            {
                case MarketEventType.VillageLooted: return new TextObject("{=cx_ev_village_looted}{VILLAGE} was raided, {ITEM} will be scarce.");
                case MarketEventType.SiegeStarted: return new TextObject("{=cx_ev_siege_started}{TOWN} is under siege.");
                case MarketEventType.SiegeEnded: return new TextObject("{=cx_ev_siege_ended}The siege of {TOWN} has ended.");
                case MarketEventType.SettlementCaptured: return new TextObject("{=cx_ev_captured}{TOWN} passed from {K1} to {K2}.");
                case MarketEventType.WarDeclared: return new TextObject("{=cx_ev_war}{K1} and {K2} are at war.");
                case MarketEventType.PeaceMade: return new TextObject("{=cx_ev_peace}{K1} and {K2} have made peace.");
                case MarketEventType.SeasonChanged: return new TextObject("{=cx_ev_season}{SEASON} has come.");
                case MarketEventType.FamineStarted: return new TextObject("{=cx_ev_famine}Famine in {TOWN}.");
                case MarketEventType.KingdomDestroyed: return new TextObject("{=cx_ev_kingdom_destroyed}{K1} has fallen.");
                case MarketEventType.HeroCaptured: return new TextObject("{=cx_ev_hero_captured}{HERO} ({K1}) was taken prisoner by {CAPTOR} ({K2}).");
                case MarketEventType.CastleCaptured: return new TextObject("{=cx_ev_castle_captured}{SETTLEMENT} passed from {K1} to {K2}.");
                case MarketEventType.WarEnded: return new TextObject("{=cx_ev_war_ended}{K1} won the war against {K2}. {K2} pays {AMOUNT} denars a day in tribute.");
                default: return new TextObject(type.ToString());
            }
        }

        public static TextObject RulerDied => new TextObject("{=cx_ev_ruler_died}{HERO}, ruler of {K1}, has died{CAUSE}.");
        public static TextObject ClanLeaderDied => new TextObject("{=cx_ev_clan_leader_died}{HERO}, clan leader of {K1}, has died{CAUSE}.");

        public static TextObject DeathCause(string detail)
        {
            switch (detail)
            {
                case "DiedInBattle": return new TextObject("{=cx_cause_battle} in battle");
                case "WoundedInBattle": return new TextObject("{=cx_cause_wounds} of wounds");
                case "DiedOfOldAge": return new TextObject("{=cx_cause_old_age} of old age");
                case "Executed":
                case "ExecutionAfterMapEvent": return new TextObject("{=cx_cause_executed}, executed");
                case "Murdered": return new TextObject("{=cx_cause_murdered}, murdered");
                case "DiedInLabor": return new TextObject("{=cx_cause_labor} in childbirth");
                default: return TextObject.GetEmpty();
            }
        }

        public static TextObject WhaleBought => new TextObject("{=cx_ev_whale_buy}A merchant of {TOWN} bought {QTY} {NAME} shares ({CHANGE}).");
        public static TextObject WhaleSold => new TextObject("{=cx_ev_whale_sell}A merchant of {TOWN} sold {QTY} {NAME} shares ({CHANGE}).");
        public static TextObject WhaleBoughtNoTown => new TextObject("{=cx_ev_whale_buy_any}A large investor bought {QTY} {NAME} shares ({CHANGE}).");
        public static TextObject WhaleSoldNoTown => new TextObject("{=cx_ev_whale_sell_any}A large investor sold {QTY} {NAME} shares ({CHANGE}).");

        public static TextObject BattleWon => new TextObject("{=cx_ev_battle}{K1} defeated {K2} ({CASUALTIES} casualties).");
        public static TextObject BattleWonNear => new TextObject("{=cx_ev_battle_near}{K1} defeated {K2} near {SETTLEMENT} ({CASUALTIES} casualties).");
        public static TextObject ClanRebelled => new TextObject("{=cx_ev_clan_rebelled}{CLAN} rebelled against {K1}.");
        public static TextObject ClanRebelledFounded => new TextObject("{=cx_ev_clan_founded}{CLAN} rebelled against {K1} and founded {K2}.");
        public static TextObject ClanDefected => new TextObject("{=cx_ev_clan_defected}{CLAN} left {K1} for {K2}.");
        public static TextObject ClanLeft => new TextObject("{=cx_ev_clan_left}{CLAN} left {K1}.");
        public static TextObject ClanJoined => new TextObject("{=cx_ev_clan_joined}{CLAN} swore fealty to {K2}.");

        /// <summary>HeroCaptured when the capturing party had no leader (e.g. a garrison): the faction only.</summary>
        public static TextObject HeroCapturedByFaction => new TextObject("{=cx_ev_hero_captured_faction}{HERO} ({K1}) was taken prisoner by {K2}.");

        public static TextObject NameWithOwner => new TextObject("{=cx_name_with_owner}{NAME} ({OWNER})");
        public static TextObject BtnPerformance => new TextObject("{=cx_btn_performance}Performance");
        public static TextObject PerformanceTitle => new TextObject("{=cx_perf_title}Portfolio Performance");
        public static TextObject PerformancePeriod => new TextObject("{=cx_perf_period}{DAYS} days");
        public static TextObject PerformanceChange => new TextObject("{=cx_perf_change}Last {DAYS} days: {CHANGE} denars");
        public static TextObject PerformanceChangePartial => new TextObject("{=cx_perf_change_partial}Last {DAYS} days: {CHANGE} denars (recorded for {COVERED} days)");
        public static TextObject PerformanceTotals => new TextObject("{=cx_perf_totals}Total profit/loss: {PNL} denars. Portfolio value: {VALUE} denars.");
        public static TextObject SumValue => new TextObject("{=cx_sum_value}Portfolio value");
        public static TextObject SumOpen => new TextObject("{=cx_sum_open}Open P/L");
        public static TextObject SumRealized => new TextObject("{=cx_sum_realized}Realized P/L");
        public static TextObject SumGuildIncome => new TextObject("{=cx_sum_guild_income}Profit received from guilds");
        public static TextObject SumGuildPending => new TextObject("{=cx_sum_guild_pending}Guild profit accruing");
        public static TextObject SumGuildDebt => new TextObject("{=cx_sum_guild_debt}Guild debt");
        public static TextObject SumFees => new TextObject("{=cx_sum_fees}Commission paid");
        public static TextObject SumTaxes => new TextObject("{=cx_sum_taxes}Tax paid");
        public static TextObject SumTrades => new TextObject("{=cx_sum_trades}Trades");
        public static TextObject SumTotal => new TextObject("{=cx_sum_total}Total P/L");
        public static TextObject PerformanceNotEnough => new TextObject("{=cx_perf_not_enough}One point is recorded every day. The chart appears after the first recorded day.");
        public static TextObject BtnPortfolio => new TextObject("{=cx_btn_portfolio}Portfolio");
        public static TextObject SearchNoMatch => new TextObject("{=cx_search_no_match}Nothing matches the search.");
        public static TextObject SearchPlaceholder => new TextObject("{=cx_search_placeholder}Search...");

        public static TextObject SeasonName(Season season)
        {
            switch (season)
            {
                case Season.Spring: return new TextObject("{=cx_season_spring}Spring");
                case Season.Summer: return new TextObject("{=cx_season_summer}Summer");
                case Season.Autumn: return new TextObject("{=cx_season_autumn}Autumn");
                default: return new TextObject("{=cx_season_winter}Winter");
            }
        }

        public static TextObject Unknown => new TextObject("{=cx_unknown}somewhere");

        public static TextObject TradeErrorText(TradeError error)
        {
            switch (error)
            {
                case TradeError.InvalidQuantity: return new TextObject("{=cx_err_quantity}Invalid number of shares.");
                case TradeError.UnknownInstrument: return new TextObject("{=cx_err_unknown}That instrument is not traded here.");
                case TradeError.InstrumentClosed: return new TextObject("{=cx_err_closed}That instrument is no longer traded.");
                case TradeError.NoPrice: return new TextObject("{=cx_err_no_price}There is no price for that yet.");
                case TradeError.InsufficientGold: return new TextObject("{=cx_err_gold}You do not have enough gold.");
                case TradeError.InsufficientShares: return new TextObject("{=cx_err_shares}You do not hold that many shares.");
                case TradeError.TotalLimitExceeded: return new TextObject("{=cx_err_total_limit}The money changers will not take more of your money. Raise your clan's renown or your trade skill.");
                case TradeError.InstrumentLimitExceeded: return new TextObject("{=cx_err_instrument_limit}The market is too thin to take that much of one instrument.");
                case TradeError.NotInGuildTown: return new TextObject("{=cx_err_guild_town}Partnership shares of a guild are only sold in its own town.");
                case TradeError.GuildSharesExceeded: return new TextObject("{=cx_err_guild_shares}The guild does not have that many shares left for you.");
                case TradeError.OwnWorkshop: return new TextObject("{=cx_err_own_workshop}You own a workshop of this guild. You cannot buy partnership shares in your own business.");
                default: return new TextObject("{=cx_err_generic}The trade could not be made.");
            }
        }

        // Formatting pieces that differ between languages
        public static TextObject PercentFormat => new TextObject("{=cx_fmt_pct}{SIGN}{ABS}%");
        public static TextObject DecimalSeparator => new TextObject("{=cx_fmt_decimal}.");
    }
}
