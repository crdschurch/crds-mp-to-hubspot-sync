using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Dto;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System;
using System.Collections.Generic;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class PrepareMpContactCoreUpdatesForHubSpot : IPrepareMpContactCoreUpdatesForHubSpot
    {
        private readonly IMapper _mapper;

        public PrepareMpContactCoreUpdatesForHubSpot(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public CategorizedContactUpdatesDto Prepare(IDictionary<string, List<MpContactUpdateDto>> contactUpdates)
        {
            const string emailPropertyName = "email";
            var coreOnlyChangedContacts = new List<CoreOnlyChangedContact>();
            var emailAddressChangedContacts = new List<EmailAddressChangedContact>();

            foreach (var contactKeyValue in contactUpdates)
            {
                var containsEmailAddressChange =
                    contactKeyValue.Value.Exists(update => update.PropertyName.Equals(emailPropertyName, StringComparison.OrdinalIgnoreCase));

                var contactToCreate = _mapper.Map<EmailAddressCreatedContact>(contactKeyValue.Value);

                // MP audit log doesn't contain email address in contact's manifest of changes -- simplest course, map, move on
                if (containsEmailAddressChange == false)
                {
                    var coreUpdate = _mapper.Map<CoreOnlyChangedContact>(contactKeyValue.Value);
                    coreUpdate.ContactDoesNotExistContingency = contactToCreate;
                    coreOnlyChangedContacts.Add(coreUpdate);
                    continue;
                }

                var emailUpdate = _mapper.Map<EmailAddressChangedContact>(contactKeyValue.Value);
                emailUpdate.ContactDoesNotExistContingency = contactToCreate;
                emailAddressChangedContacts.Add(emailUpdate);
            }

            return new CategorizedContactUpdatesDto
            {
                EmailChangedContacts = emailAddressChangedContacts,
                CoreOnlyChangedContacts = coreOnlyChangedContacts
            };
        }
    }
}