using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LiteDB;

namespace Crossroads.Service.HubSpot.Sync.LiteDB
{
    public interface ILiteDbRepository : IDisposable
    {
        BsonValue Insert<T>(T entity, string collectionName = null);
        int Insert<T>(IEnumerable<T> entities, string collectionName = null);
        bool Update<T>(T entity, string collectionName = null);
        int Update<T>(IEnumerable<T> entities, string collectionName = null);
        bool Upsert<T>(T entity, string collectionName = null);
        int Upsert<T>(IEnumerable<T> entities, string collectionName = null);
        bool Delete<T>(BsonValue id, string collectionName = null);
        int Delete<T>(Query query, string collectionName = null);
        int Delete<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        LiteQueryable<T> Query<T>(string collectionName = null);
        T SingleById<T>(BsonValue id, string collectionName = null);
        List<T> Fetch<T>(Query query = null, string collectionName = null);
        List<T> Fetch<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        T First<T>(Query query = null, string collectionName = null);
        T First<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        T FirstOrDefault<T>(Query query, string collectionName = null);
        T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        T Single<T>(Query query = null, string collectionName = null);
        T Single<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        T SingleOrDefault<T>(Query query = null, string collectionName = null);
        T SingleOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null);
        LiteDatabase Database { get; }
        LiteEngine Engine { get; }
        LiteStorage FileStorage { get; }
    }
}