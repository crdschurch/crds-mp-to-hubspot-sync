using System;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services
{
    public interface IDetermineHowToUpdateContacts
    {
        void Determine(IDictionary<string, List<MpContactUpdateDto>> contactUpdates);
    }

    public class DetermineHowToUpdateContacts : IDetermineHowToUpdateContacts
    {
        private readonly IMapper _mapper;

        public DetermineHowToUpdateContacts(IMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public void Determine(IDictionary<string, List<MpContactUpdateDto>> contactUpdates)
        {
            const string emailPropertyName = "email";
            var updateByEmailSerialList = new List<UpdateByEmailContact>();
            var createBulkList = new List<BulkContact>();

            foreach (var contactKeyValue in contactUpdates)
            {
                var ministryPlatformContactId = contactKeyValue.Key;

                var containsEmailAddressChange =
                    contactKeyValue.Value.Exists(update => update.PropertyName.Equals(emailPropertyName, StringComparison.OrdinalIgnoreCase));

                if (containsEmailAddressChange) // if true, *potentially* updating by email
                {
                    var emailAddressWasPreviouslyEmpty =
                        contactKeyValue.Value.First(update => update.PropertyName.Equals(emailPropertyName, StringComparison.OrdinalIgnoreCase))
                            .PreviousValue
                            .IsNullOrEmpty();

                    if (emailAddressWasPreviouslyEmpty)
                    {
                        // create scenario (serial or bulk?) and continue
                        createBulkList.Add(_mapper.Map<BulkContact>(contactKeyValue.Value));
                        continue;
                    }

                    // serial update by email scenario (serial) and continue
                    updateByEmailSerialList.Add(_mapper.Map<UpdateByEmailContact>(contactKeyValue.Value));
                    continue;
                }

                
            }
        }
    }
}
