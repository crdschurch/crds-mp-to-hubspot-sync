using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class PrepareDataForHubSpot : IPrepareDataForHubSpot
    {
        private readonly IMapper _mapper;

        public PrepareDataForHubSpot(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public SerialContact[] Prep(IList<NewlyRegisteredMpContactDto> newContacts)
        {
            if(newContacts == null || newContacts.Count == decimal.Zero) return new SerialContact[0];
            return _mapper.Map<SerialContact[]>(newContacts);
        }

        public SerialContact[] Prep(IDictionary<string, List<CoreUpdateMpContactDto>> contactUpdates)
        {
            if(contactUpdates == null || contactUpdates.Count == decimal.Zero) return new SerialContact[0];
            var contacts = new List<SerialContact>();

            foreach (var contactKeyValue in contactUpdates)
            {
                var containsEmailAddressChange = contactKeyValue.Value.Exists(update => update.PropertyName.Equals("email", StringComparison.OrdinalIgnoreCase));
                if (containsEmailAddressChange == false) // MP audit log does NOT contain email address in contact's manifest of changes
                {
                    contacts.Add(_mapper.Map<NonEmailAttributesChangedContact>(contactKeyValue.Value));
                    continue;
                }

                contacts.Add(_mapper.Map<EmailAddressChangedContact>(contactKeyValue.Value));
            }

            return contacts.ToArray();
        }

        public BulkContact[] Prep(IList<AgeAndGradeGroupCountsForMpContactDto> mpContacts)
        {
            if(mpContacts == null || mpContacts.Count == decimal.Zero) return new BulkContact[0];
            return _mapper.Map<List<BulkContact>>(mpContacts).ToArray();
        }

        public BulkContact[] ToBulk(List<BulkSyncFailure> failedBatches)
        {
            if(failedBatches == null || failedBatches.Count == decimal.Zero) return new BulkContact[0];
            return failedBatches.SelectMany(batch => batch.Contacts).ToArray();
        }

        public SerialContact[] ToSerial(List<BulkSyncFailure> failedBatches)
        {
            if (failedBatches == null || failedBatches.Count == decimal.Zero) return new SerialContact[0];
            return _mapper.Map<SerialContact[]>(failedBatches.SelectMany(batch => batch.Contacts));
        }
    }
}