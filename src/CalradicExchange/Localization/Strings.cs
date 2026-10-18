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
        public const string OptMarket = "{=cx_opt_market}Look at the market";
        public const string OptBuy = "{=cx_opt_buy}Buy shares";
        public const string OptSell = "{=cx_opt_sell}Sell shares";
        public const string OptPortfolio = "{=cx_opt_portfolio}Your portfolio";
        public const string OptNews = "{=cx_opt_news}News";
        public const string OptLeave = "{=cx_opt_leave}Leave";
        public static TextObject NothingToSell => new TextObject("{=cx_tip_nothing_to_sell}You hold no shares.");
        public static TextObject ClosedReasonData => new TextObject("{=cx_closed_data}its ledgers could not be read, see exchange.log");
        public static TextObject ClosedReasonPrices => new TextObject("{=cx_closed_prices}no prices yet");

        // Generic buttons
        public static TextObject Close => new TextObject("{=cx_btn_close}Close");
        public static TextObject Confirm => new TextObject("{=cx_btn_confirm}Confirm");
        public static TextObject Cancel => new TextObject("{=cx_btn_cancel}Cancel");
        public static TextObject Continue => new TextObject("{=cx_btn_continue}Continue");

        // Market report
        public static TextObject MarketTitle => new TextObject("{=cx_market_title}Market prices");
        public static TextObject HeaderCommodities => new TextObject("{=cx_hdr_commodities}Commodity shares (1 share = {UNITS} units)");
        public static TextObject HeaderSectors => new TextObject("{=cx_hdr_sectors}Baskets");
        public static TextObject MarketLine => new TextObject("{=cx_line_market}{NAME}: {PRICE}   today {DAY}   7 days {WEEK}");
        // Table column headers
        public static TextObject ColInstrument => new TextObject("{=cx_col_instrument}Instrument");
        public static TextObject ColPrice => new TextObject("{=cx_col_price}Price");
        public static TextObject ColToday => new TextObject("{=cx_col_today}Today");
        public static TextObject ColWeek => new TextObject("{=cx_col_week}7 days");
        public static TextObject ColQuantity => new TextObject("{=cx_col_quantity}Qty");
        public static TextObject ColBoughtAt => new TextObject("{=cx_col_bought_at}Bought at");
        public static TextObject ColNow => new TextObject("{=cx_col_now}Now");
        public static TextObject ColProfit => new TextObject("{=cx_col_profit}Profit");
        public static TextObject ColProfitPct => new TextObject("{=cx_col_profit_pct}Profit %");
        public static TextObject NoPrice => new TextObject("{=cx_no_price_short}-");

        public static TextObject NoPriceYet => new TextObject("{=cx_no_price}{NAME}: no price yet");

        // Buy / sell flow
        public static TextObject BuyTitle => new TextObject("{=cx_buy_title}Buy shares");
        public static TextObject BuyDescription => new TextObject("{=cx_buy_desc}Choose what to buy. Gold: {GOLD} denars. Limit left: {LEFT} denars.");
        public static TextObject SellTitle => new TextObject("{=cx_sell_title}Sell shares");
        public static TextObject SellDescription => new TextObject("{=cx_sell_desc}Choose what to sell.");
        public static TextObject BuyElement => new TextObject("{=cx_buy_element}{NAME} - {PRICE}");
        public static TextObject SellElement => new TextObject("{=cx_sell_element}{NAME} - {QTY} shares at {PRICE}");
        public static TextObject QuantityBuy => new TextObject("{=cx_qty_buy}{NAME} costs {PRICE} per share before fees.\nHow many shares? (at most {MAX})");
        public static TextObject QuantitySell => new TextObject("{=cx_qty_sell}You hold {QTY} shares of {NAME}.\nHow many do you sell?");
        public static TextObject QuantityEstimateBuy => new TextObject("{=cx_qty_estimate_buy}To pay: {TOTAL} denars (broker's fee {FEE})");
        public static TextObject QuantityEstimateSell => new TextObject("{=cx_qty_estimate_sell}You receive: {TOTAL} denars (broker's fee {FEE}, tax {TAX})");
        public static TextObject QuantityInvalid => new TextObject("{=cx_qty_invalid}Enter a whole number from 1 to {MAX}.");
        public static TextObject ConfirmBuy => new TextObject("{=cx_confirm_buy}Buy {QTY} x {NAME}\n \nMarket price: {PRICE}\nAfter spread and slippage: {EXEC}\nShares: {GROSS} denars\nBroker's fee: {FEE} denars\n \nTotal to pay: {TOTAL} denars");
        public static TextObject ConfirmSell => new TextObject("{=cx_confirm_sell}Sell {QTY} x {NAME}\n \nMarket price: {PRICE}\nAfter spread and slippage: {EXEC}\nShares: {GROSS} denars\nBroker's fee: {FEE} denars\nTax on profit: {TAX} denars\n \nYou receive: {TOTAL} denars");
        public static TextObject Bought => new TextObject("{=cx_msg_bought}Bought {QTY} {NAME} for {TOTAL} denars.");
        public static TextObject Sold => new TextObject("{=cx_msg_sold}Sold {QTY} {NAME} for {TOTAL} denars ({PNL}).");
        public static TextObject Rebased => new TextObject("{=cx_msg_rebased}The exchange has rewritten its ledgers. Your shares were bought back at market price: {GOLD} denars.");
        public static TextObject BigMove => new TextObject("{=cx_msg_big_move}Exchange: {NAME} {CHANGE} ({PRICE}).");

        // Portfolio
        public static TextObject PortfolioTitle => new TextObject("{=cx_portfolio_title}Your portfolio");
        public static TextObject PortfolioEmpty => new TextObject("{=cx_portfolio_empty}You hold no shares.");
        public static TextObject PortfolioLine => new TextObject("{=cx_portfolio_line}{NAME}: {QTY} shares, bought at {AVG}, now {PRICE}, {PNL}");
        public static TextObject PortfolioTotals => new TextObject("{=cx_portfolio_totals}Value {VALUE} denars, open profit {OPEN}.\nRealized profit {REALIZED}. Fees paid {FEES}, taxes paid {TAXES} denars.");

        // News
        public static TextObject NewsTitle => new TextObject("{=cx_news_title}News at the exchange");
        public static TextObject NewsEmpty => new TextObject("{=cx_news_empty}No news has reached the exchange yet.");
        public static TextObject NewsToday => new TextObject("{=cx_news_today}Today");
        public static TextObject NewsDaysAgo => new TextObject("{=cx_news_days_ago}{DAYS} days ago");
        public static TextObject NewsLine => new TextObject("{=cx_news_line}{WHEN}: {TEXT}");

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
                default: return new TextObject(type.ToString());
            }
        }

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
                default: return new TextObject("{=cx_err_generic}The trade could not be made.");
            }
        }

        // Formatting pieces that differ between languages
        public static TextObject PercentFormat => new TextObject("{=cx_fmt_pct}{SIGN}{ABS}%");
        public static TextObject DecimalSeparator => new TextObject("{=cx_fmt_decimal}.");
    }
}
