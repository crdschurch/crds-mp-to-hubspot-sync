namespace Crossroads.Service.HubSpot.Sync.LiteDb.Configuration
{
    public interface ILiteDbConfigurationProvider
    {
        TU GetConfiguration<T, TU>() where T: struct, IKeyValuePair<string, TU>;
    }
}