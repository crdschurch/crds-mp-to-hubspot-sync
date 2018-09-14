using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class PrepareMpDataForHubSpot : IPrepareMpDataForHubSpot
    {
        private readonly IMapper _mapper;

        public PrepareMpDataForHubSpot(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public SerialHubSpotContact[] Prep(IList<NewlyRegisteredMpContactDto> newContacts)
        {
            if(newContacts == null || newContacts.Count == decimal.Zero) return new SerialHubSpotContact[0];
            return _mapper.Map<SerialHubSpotContact[]>(newContacts);
        }

        public SerialHubSpotContact[] Prep(IDictionary<string, List<CoreUpdateMpContactDto>> contactUpdates)
        {
            if(contactUpdates == null || contactUpdates.Count == decimal.Zero) return new SerialHubSpotContact[0];
            var contacts = new List<SerialHubSpotContact>();

            foreach (var contactKeyValue in contactUpdates)
            {
                var containsEmailAddressChange = contactKeyValue.Value.Exists(update => update.PropertyName.Equals("email", StringComparison.OrdinalIgnoreCase));
                if (containsEmailAddressChange == false) // MP audit log does NOT contain email address in contact's manifest of changes
                {
                    contacts.Add(_mapper.Map<NonEmailAttributesChangedHubSpotContact>(contactKeyValue.Value.First()));
                    continue;
                }

                contacts.Add(_mapper.Map<EmailAddressChangedHubSpotContact>(contactKeyValue.Value.First(dto => dto.PropertyName.Equals("email", StringComparison.OrdinalIgnoreCase))));
            }

            return contacts.ToArray();
        }

        public BulkHubSpotContact[] Prep(IList<AgeAndGradeGroupCountsForMpContactDto> mpContacts)
        {
            if(mpContacts == null || mpContacts.Count == decimal.Zero) return new BulkHubSpotContact[0];
            return _mapper.Map<List<BulkHubSpotContact>>(mpContacts).ToArray();
        }

        public BulkHubSpotContact[] ToBulk(List<BulkSyncFailure> failedBatches)
        {
            if(failedBatches == null || failedBatches.Count == decimal.Zero) return new BulkHubSpotContact[0];
            return failedBatches.SelectMany(batch => batch.HubSpotContacts).ToArray();
        }

        public SerialHubSpotContact[] ToSerial(List<BulkSyncFailure> failedBatches)
        {
            if (failedBatches == null || failedBatches.Count == decimal.Zero) return new SerialHubSpotContact[0];
            return _mapper.Map<SerialHubSpotContact[]>(failedBatches.SelectMany(batch => batch.HubSpotContacts));
        }
    }
}