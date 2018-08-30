using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile(IConfigurationService configurationService)
        {
            var environment = configurationService.GetEnvironmentName();

            // HubSpot bulk contact to HubSpot serial contact
            // HubSpot structures contact properties slightly differently for bulk (create or update) and serial create endpoints
            CreateMap<BulkContact, SerialContact>();

            // newly registered contact(s)
            CreateMap<NewlyRegisteredMpContactDto, SerialContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(mpContact => mpContact.Email))
                .ForMember(hubSpotContact => hubSpotContact.Properties, memberOptions => memberOptions.MapFrom(mpContact => ReflectToContactProperties(mpContact)))
                .AfterMap((mpContact, hubSpotContact) => AddTangentialAttributesToHubSpotProperties(hubSpotContact, environment));

            // when email address changed
            CreateMap<CoreUpdateMpContactDto, EmailAddressChangedContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(mpContact => mpContact.PreviousValue))
                .ForMember(hubSpotContact => hubSpotContact.Properties, memberOptions => memberOptions.MapFrom(mpContact => ReflectToContactProperties(mpContact)))
                .AfterMap((mpContact, hubSpotContact) => AddTangentialAttributesToHubSpotProperties(hubSpotContact, environment));

            // when non-email updates have occurred
            CreateMap<CoreUpdateMpContactDto, NonEmailAttributesChangedContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(mpContact => mpContact.Email))
                .ForMember(hubSpotContact => hubSpotContact.Properties, memberOptions => memberOptions.MapFrom(mpContact => ReflectToContactProperties(mpContact)))
                .AfterMap((mpContact, hubSpotContact) => AddTangentialAttributesToHubSpotProperties(hubSpotContact, environment));

            // age/grade data changed
            CreateMap<AgeAndGradeGroupCountsForMpContactDto, BulkContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(mpContact => mpContact.Email))
                .ForMember(hubSpotContact => hubSpotContact.Properties, memberOptions => memberOptions.MapFrom(mpContact => ReflectToContactProperties(mpContact)))
                .AfterMap((mpContact, hubSpotContact) => AddTangentialAttributesToHubSpotProperties(hubSpotContact, environment));
        }

        /// <summary>
        /// Reflects over the specified argument to convert its defined object properties to an ISet collection of
        /// type <see cref="ContactProperty"/>. Goal is to sacrifice a mite of readability (hard decision to make)
        /// for the sake of simplifying future updates; should we need to add any new properties to the contact,
        /// this will account for any additions where mapping to HubSpot models is concerned. This operates under the
        /// assumption that all properties expressed in the type's definition have corrollaries in HubSpot.
        /// </summary>
        public static ISet<ContactProperty> ReflectToContactProperties<T>(T contactProperties)
        {
            return new HashSet<ContactProperty>(contactProperties.GetType()
                .GetInterfaces()
                .SelectMany(i => i.GetProperties().Select(propertyInfo => BuildContactProperty(propertyInfo, contactProperties))));
        }

        private static ContactProperty BuildContactProperty(PropertyInfo property, object obj)
        {
            return new ContactProperty
            {
                Name = property.Name.ToLowerInvariant(),
                Value = property.GetValue(obj)?.ToString() ?? string.Empty
            };
        }

        /// <summary>
        /// Ensures the Environment and lifecycle stage are included in the payload to be sent to HubSpot alongside each contact.
        /// </summary>
        /// <param name="targetContact">Contact to which we will assign the integration attributes.</param>
        /// <param name="environmentName">Name of the environment in which the application is executing.</param>
        public static void AddTangentialAttributesToHubSpotProperties(IContact targetContact, string environmentName)
        {
            // preserve existing properties (the HashSet will keep the data clean/unique)
            targetContact.Properties = new HashSet<ContactProperty>(targetContact.Properties ?? Enumerable.Empty<ContactProperty>())
            {
                // captures reference metadata to pass along when updating HubSpot contact data
                new ContactProperty
                {
                    Name = "environment",
                    Value = environmentName
                },
                new ContactProperty
                {
                    Name = "lifecyclestage",
                    Value = "customer"
                }
            }.ToList();
        }
    }
}