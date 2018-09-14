namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Configuration
{
    public interface IMongoDbConfigurationProvider
    {
        /// <summary>
        /// For use when there's only ever a single document in the collection.
        /// </summary>
        TU Get<T, TU>() where T: class, IKeyValuePair<string, TU>;
    }
}