using TaleWorlds.Localization;

namespace CalradicExchange.Localization
{
    /// <summary>
    /// Player-facing strings. English text is the fallback; translations live in
    /// ModuleData/Languages/&lt;LANG&gt;/cx_strings_*.xml keyed by the {=id}.
    /// </summary>
    public static class Strings
    {
        public const string MenuVisitExchange = "{=cx_menu_visit_exchange}Visit the Exchange";

        public static TextObject ComingSoon => new TextObject("{=cx_msg_coming_soon}The Calradic Exchange is opening soon.");
    }
}
