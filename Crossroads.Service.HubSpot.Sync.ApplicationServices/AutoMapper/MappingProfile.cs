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

            // CREATE OR UPDATE SCENARIOS
            // MP data to HubSpot mapping definitions
            CreateMap<NewlyRegisteredMpContactDto, SerialContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(dto => dto.Email))
                .AfterMap(AddCoreAttributesToHubSpotProperties)
                .AfterMap((updates, targetContact) => AddTangentialAttributesToHubSpotProperties(targetContact, environment));

            // HubSpot bulk contact to HubSpot serial contact
            // HubSpot structures contact properties slightly differently for bulk (create or update) and serial create endpoints
            CreateMap<BulkContact, SerialContact>();

            // CREATE OR UPDATE SCENARIOS
            // when an updated contact's email address changed from one value to another
            CreateMap<List<CoreUpdateMpContactDto>, EmailAddressChangedContact>()
                .ForMember(emailChanged => emailChanged.Email, memberOptions => memberOptions.MapFrom(updates => updates.First(update => update.PropertyName == "email").PreviousValue))
                .ForMember(emailChanged => emailChanged.Properties, memberOptions => memberOptions.MapFrom(updates => ToContactProperties(updates)))
                .AfterMap((coreProperties, targetContact) => AddCoreAttributesToHubSpotProperties(coreProperties.First(), targetContact))
                .AfterMap((updates, targetContact) => AddTangentialAttributesToHubSpotProperties(targetContact, environment));

            // when non-email updates have occurred
            CreateMap<List<CoreUpdateMpContactDto>, NonEmailAttributesChangedContact>()
                .ForMember(nonEmailChanges => nonEmailChanges.Email, memberOptions => memberOptions.MapFrom(updates => updates.First().Email))
                .ForMember(nonEmailChanges => nonEmailChanges.Properties, memberOptions => memberOptions.MapFrom(updates => ToContactProperties(updates)))
                .AfterMap((coreProperties, targetContact) => AddCoreAttributesToHubSpotProperties(coreProperties.First(), targetContact))
                .AfterMap((tangentialProperties, targetContact) => AddTangentialAttributesToHubSpotProperties(targetContact, environment));

            // WE KIND OF TREAT THIS AS AN UPDATE-ONLY SCENARIO B/C WE DON'T INCLUDE CORE PROPERTIES (MAYBE WE SHOULD FOR SAFETY?)
            // BUT TECHNICALLY THIS SHOULD BE PICKED UP BY THE NEW REGISTRATION OR CORE UPDATE PROCESSES. WILL STILL CREATE THE CONTACT...
            // ...THEY JUST WON'T HAVE FIRSTNAME, LASTNAME, COMMUNITY, ETC UNTIL ONE OF THE OTHER 2 PROCESSES PICKS THEM UP
            // when Kids Club and Student Ministry age/grade data changes
            CreateMap<AgeAndGradeGroupCountsForMpContactDto, BulkContact>()
                .ForMember(contact => contact.Email, memberOptions => memberOptions.MapFrom(ageGradeUpdates => ageGradeUpdates.Email))
                .ForMember(contact => contact.Properties, memberOptions => memberOptions.MapFrom(ageGradeUpdates => ReflectToContactProperties(ageGradeUpdates)))
                .AfterMap((tangentialProperties, targetContact) => AddTangentialAttributesToHubSpotProperties(targetContact, environment));
        }

        /// <summary>
        /// Ensures the Ministry Platform Contact Id, Source, Environment and lifecycle stage are included in the payload
        /// to be sent to HubSpot alongside each contact.
        /// </summary>
        /// <param name="targetContact">Contact to which we will assign the integration attributes.</param>
        /// <param name="environmentName">Name of the environment in which the application is executing.</param>
        private static void AddTangentialAttributesToHubSpotProperties(IContact targetContact, string environmentName)
        {
            // preserve existing properties (the HashSet will keep the data clean/unique)
            targetContact.Properties = new HashSet<ContactProperty>(targetContact.Properties ?? Enumerable.Empty<ContactProperty>())
            {
                // captures reference metadata to pass along when updating HubSpot contact data
                new ContactProperty
                {
                    Property = "environment",
                    Value = environmentName
                },
                new ContactProperty
                {
                    Property = "lifecyclestage",
                    Value = "customer"
                }
            }.ToList();
        }

        /// <summary>
        /// Ensures email, firstname, lastname, marital status, gender and community are included in the event a contact
        /// does not yet exist. This way they'll be created with all the data we care to know about them that they've
        /// actually provided via the Crossroads.net Profile feature.
        /// </summary>
        /// <param name="coreContactProperties"></param>
        /// <param name="targetContact"></param>
        private static void AddCoreAttributesToHubSpotProperties(ICoreContactProperties coreContactProperties, IContact targetContact)
        {
            // *Attempt* to add all the core properties for create/update operation (the HashSet will keep the data clean/unique)
            var preExistingCoreProperties = new HashSet<ContactProperty>(targetContact.Properties ?? Enumerable.Empty<ContactProperty>());
            preExistingCoreProperties.UnionWith(ReflectToContactProperties(coreContactProperties));
            targetContact.Properties = preExistingCoreProperties.ToList();
        }

        /// <summary>
        /// Projects contact updates originating from the MP audit log to HubSpot contact properties.
        /// </summary>
        /// <param name="contactUpdates">
        /// Individual audit log records of change for a given contact. Equates to 1 log per field change.
        /// </param>
        private ISet<ContactProperty> ToContactProperties(List<CoreUpdateMpContactDto> contactUpdates)
        {
            return new HashSet<ContactProperty>(contactUpdates.Select(update => new ContactProperty
            {
                // captures all updates found in the MP audit log
                Property = update.PropertyName,
                Value = update.NewValue
            }));
        }

        /// <summary>
        /// Reflects over the specified argument to convert its defined object properties to an ISet collection of
        /// type <see cref="ContactProperty"/>. Goal is to sacrifice a mite of readability (hard decision to make)
        /// for the sake of simplifying future updates; should we need to add any new properties to the contact,
        /// this will account for any additions where mapping to HubSpot models is concerned.
        /// </summary>
        private static ISet<ContactProperty> ReflectToContactProperties<T>(T contactProperties)
        {
            return new HashSet<ContactProperty>(contactProperties.GetType()
                .GetInterfaces()
                .SelectMany(i => i.GetProperties().Select(propertyInfo => BuildContactProperty(propertyInfo, contactProperties))));
        }

        private static ContactProperty BuildContactProperty(PropertyInfo property, object obj)
        {
            return new ContactProperty
            {
                Property = property.Name.ToLowerInvariant(),
                Value = property.GetValue(obj)?.ToString() ?? string.Empty
            };
        }
    }
}
