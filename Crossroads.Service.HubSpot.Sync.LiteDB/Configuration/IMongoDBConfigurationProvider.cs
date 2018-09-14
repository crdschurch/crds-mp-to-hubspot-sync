namespace Crossroads.Service.HubSpot.Sync.LiteDb.Configuration
{
    public interface IMongoDBConfigurationProvider
    {
        TU Get<T, TU>() where T: struct, IKeyValuePair<string, TU>;
    }
}