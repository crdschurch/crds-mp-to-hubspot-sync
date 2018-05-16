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

        public BulkContact[] Prep(IList<NewlyRegisteredMpContactDto> newContacts)
        {
            return _mapper.Map<BulkContact[]>(newContacts);
        }

        public SerialContact[] Prep(IDictionary<string, List<CoreUpdateMpContactDto>> contactUpdates)
        {
            var contacts = new List<SerialContact>();

            foreach (var contactKeyValue in contactUpdates)
            {
                var containsEmailAddressChange =
                    contactKeyValue.Value.Exists(update => update.PropertyName.Equals("email", StringComparison.OrdinalIgnoreCase));

                // MP audit log does NOT contain email address in contact's manifest of changes
                if (containsEmailAddressChange == false)
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
            return _mapper.Map<List<BulkContact>>(mpContacts).ToArray();
        }

        public BulkContact[] ToBulk(List<BulkSyncFailure> failedBatches)
        {
            return failedBatches.SelectMany(batch => batch.Contacts).ToArray();
        }

        public SerialContact[] ToSerial(List<BulkSyncFailure> failedBatches)
        {
            return _mapper.Map<SerialContact[]>(failedBatches.SelectMany(batch => batch.Contacts));
        }
    }
}