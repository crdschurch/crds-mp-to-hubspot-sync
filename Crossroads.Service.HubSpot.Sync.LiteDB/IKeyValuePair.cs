
namespace Crossroads.Service.HubSpot.Sync.LiteDb
{
    public interface IKeyValuePair<out T, out TU>
    {
        T Key { get; }
        TU Value { get; }
    }
}
