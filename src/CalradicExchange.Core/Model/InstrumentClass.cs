namespace CalradicExchange.Core.Model
{
    /// <summary>Instrument families. Each has its own pricing parameters in settings.json "classes".</summary>
    public enum InstrumentClass
    {
        Commodity,
        Sector,
        Guild,
        Kingdom,
        Bond,
    }
}
