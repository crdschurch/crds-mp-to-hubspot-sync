using System.Linq;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Extensions
{
    public static class BulkActivityResultExtensions
    {
        /// <summary>
        /// Extracts contacts that failed to sync from the activity object. Also maps
        /// the incoming contact type to a different contact type.
        /// </summary>
        /// <param name="result">Activity from which to extract the contacts that failed to sync.</param>
        /// <param name="mapper"></param>
        public static SerialCreateContact[] GetContactsThatFailedToSync(this BulkSyncResult result, IMapper mapper)
        {
            var contacts = result.FailedBatches.SelectMany(batch => batch.Contacts).ToArray();
            return contacts.Length > 0 ? mapper.Map<SerialCreateContact[]>(contacts) : new SerialCreateContact[0];
        }
    }
}
