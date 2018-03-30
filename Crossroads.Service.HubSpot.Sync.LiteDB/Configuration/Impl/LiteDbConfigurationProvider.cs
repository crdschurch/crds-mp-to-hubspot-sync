using System;
using Crossroads.Service.HubSpot.Sync.LiteDB;

namespace Crossroads.Service.HubSpot.Sync.LiteDb.Configuration.Impl
{
    public class LiteDbConfigurationProvider : ILiteDbConfigurationProvider
    {
        private readonly ILiteDbRepository _liteDbRepository;

        public LiteDbConfigurationProvider(ILiteDbRepository liteDbRepository)
        {
            _liteDbRepository = liteDbRepository ?? throw new ArgumentNullException(nameof(liteDbRepository));
        }

        public TU GetConfiguration<T, TU>() where T : struct, IKeyValuePair<string, TU>
        {
            return _liteDbRepository.SingleOrDefault<T>().Value;
        }
    }
}