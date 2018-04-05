namespace Crossroads.Service.HubSpot.Sync.LiteDb.Configuration
{
    public interface ILiteDbConfigurationProvider
    {
        TU Get<T, TU>() where T: struct, IKeyValuePair<string, TU>;
    }
}