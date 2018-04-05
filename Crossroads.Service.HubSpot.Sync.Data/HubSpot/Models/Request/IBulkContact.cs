namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request
{
    public interface IBulkContact : IContact
    {
        string Email { get; set; }
    }
}
