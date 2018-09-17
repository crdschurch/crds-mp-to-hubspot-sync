using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile(IConfigurationService configurationService)
        {
            var environment = configurationService.GetEnvironmentName();

            // HubSpot bulk contact to HubSpot serial contact
            // HubSpot structures contact properties slightly differently for bulk (create or update) and serial create endpoints
            CreateMap<BulkHubSpotContact, SerialHubSpotContact>();

            // newly registered contact(s)
            CreateMap<NewlyRegisteredMpContactDto, SerialHubSpotContact>().MapMpContactToHubSpotContact(dto => dto.Email, environment);

            // when email address changed
            CreateMap<CoreUpdateMpContactDto, EmailAddressChangedHubSpotContact>().MapMpContactToHubSpotContact(dto => dto.PreviousValue, environment);

            // when non-email updates have occurred
            CreateMap<CoreUpdateMpContactDto, NonEmailAttributesChangedHubSpotContact>().MapMpContactToHubSpotContact(dto => dto.Email, environment);

            // age/grade data changed
            CreateMap<AgeAndGradeGroupCountsForMpContactDto, BulkHubSpotContact>().MapMpContactToHubSpotContact(dto => dto.Email, environment);
        }
    }
}