using System.IO;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.LiteDB.Impl
{
    public class LiteDbRepositoryWrapper : LiteRepository, ILiteDbRepository
    {
        public LiteDbRepositoryWrapper(LiteDatabase database, bool disposeDatabase = false) : base(database, disposeDatabase)
        {
        }

        public LiteDbRepositoryWrapper(string connectionString, BsonMapper mapper = null) : base(connectionString, mapper)
        {
        }

        public LiteDbRepositoryWrapper(ConnectionString connectionString, BsonMapper mapper = null) : base(connectionString, mapper)
        {
        }

        public LiteDbRepositoryWrapper(Stream stream, BsonMapper mapper = null, string password = null) : base(stream, mapper, password)
        {
        }
    }
}