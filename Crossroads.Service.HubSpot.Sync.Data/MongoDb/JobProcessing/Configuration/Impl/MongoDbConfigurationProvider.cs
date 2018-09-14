using System;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using MongoDB.Driver;

namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Configuration.Impl
{
    public class MongoDbConfigurationProvider : IMongoDbConfigurationProvider
    {
        private readonly IMongoDatabase _mongoDatabase;

        public MongoDbConfigurationProvider(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase));
        }

        public TU Get<T, TU>() where T : class, IKeyValuePair<string, TU>
        {
            return Util.Retry(() =>
                _mongoDatabase
                    .GetCollection<T>(typeof(T).Name)
                    .Find(Builders<T>.Filter.Eq("_id", typeof(T).Name))
                    .FirstOrDefault()
                    .Value);
        }
    }
}