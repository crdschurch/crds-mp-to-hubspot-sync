using System;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.LiteDB;

namespace Crossroads.Service.HubSpot.Sync.LiteDb.Configuration.Impl
{
    public class MongoDbConfigurationProvider : IMongoDBConfigurationProvider
    {
        private readonly ILiteDbRepository _liteDbRepository;

        public MongoDbConfigurationProvider(ILiteDbRepository liteDbRepository)
        {
            _liteDbRepository = liteDbRepository ?? throw new ArgumentNullException(nameof(liteDbRepository));
        }

        public TU Get<T, TU>() where T : struct, IKeyValuePair<string, TU>
        {
            return Util.Retry(() => _liteDbRepository.SingleOrDefault<T>().Value );
        }
    }
}