using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile(IConfigurationService configurationService)
        {
            var environment = configurationService.GetEnvironmentName();

            // CREATE OR UPDATE SCENARIOS
            // MP data to HubSpot mapping definitions
            CreateMap<NewlyRegisteredMpContactDto, BulkContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(dto => dto.Email))
                .AfterMap(AddCoreAttributesToHubSpotProperties)
                .AfterMap((updates, targetContact) => AddTangentialAttributesToHubSpotProperties(updates, targetContact, environment));

            // HubSpot bulk contact to HubSpot serial contact
            // HubSpot structures contact properties slightly differently for bulk (create or update) and serial create endpoints
            CreateMap<BulkContact, SerialContact>();

            // CREATE OR UPDATE SCENARIOS
            // when an updated contact's email address changed from one value to another
            CreateMap<List<CoreUpdateMpContactDto>, EmailAddressChangedContact>()
                .ForMember(emailChanged => emailChanged.Email, memberOptions => memberOptions.MapFrom(updates => updates.First(update => update.PropertyName == "email").PreviousValue))
                .ForMember(emailChanged => emailChanged.Properties, memberOptions => memberOptions.MapFrom(updates => ToProperties(updates)))
                .AfterMap((coreProperties, targetContact) => AddCoreAttributesToHubSpotProperties(coreProperties.First(), targetContact))
                .AfterMap((updates, targetContact) => AddTangentialAttributesToHubSpotProperties(updates.First(), targetContact, environment));

            // when non-email updates have occurred
            CreateMap<List<CoreUpdateMpContactDto>, NonEmailAttributesChangedContact>()
                .ForMember(nonEmailChanges => nonEmailChanges.Email, memberOptions => memberOptions.MapFrom(updates => updates.First().Email))
                .ForMember(nonEmailChanges => nonEmailChanges.Properties, memberOptions => memberOptions.MapFrom(updates => ToProperties(updates)))
                .AfterMap((coreProperties, targetContact) => AddCoreAttributesToHubSpotProperties(coreProperties.First(), targetContact))
                .AfterMap((tangentialProperties, targetContact) => AddTangentialAttributesToHubSpotProperties(tangentialProperties.First(), targetContact, environment));

            // WE KIND OF TREAT THIS AS AN UPDATE-ONLY SCENARIO B/C WE DON'T INCLUDE CORE PROPERTIES (MAYBE WE SHOULD FOR SAFETY?)
            // BUT TECHNICALLY THIS SHOULD BE PICKED UP BY THE NEW REGISTRATION OR CORE UPDATE PROCESSES. WILL STILL CREATE THE CONTACT...
            // ...THEY JUST WON'T HAVE FIRSTNAME, LASTNAME, COMMUNITY, ETC UNTIL ONE OF THE OTHER 2 PROCESSES PICKS THEM UP
            // when Kids Club and Student Ministry age/grade data changes
            CreateMap<AgeAndGradeGroupCountsForMpContactDto, BulkContact>()
                .ForMember(contact => contact.Email, memberOptions => memberOptions.MapFrom(updates => updates.Email))
                .ForMember(contact => contact.Properties, memberOptions => memberOptions.MapFrom(updates => ToProperties(updates)))
                .AfterMap((tangentialProperties, targetContact) => AddTangentialAttributesToHubSpotProperties(tangentialProperties, targetContact, environment));
        }

        /// <summary>
        /// Ensures the Ministry Platform Contact Id, Source, Environment and lifecycle stage are included in the payload
        /// to be sent to HubSpot alongside each contact.
        /// </summary>
        /// <param name="devProperties">The developer-specific integration values from MP we want passed to HubSpot.</param>
        /// <param name="targetContact">Contact to which we will assign the integration attributes.</param>
        /// <param name="environmentName">Name of the environment in which the application is executing.</param>
        private static void AddTangentialAttributesToHubSpotProperties(IDeveloperIntegrationProperties devProperties, IContact targetContact, string environmentName)
        {
            // preserve existing properties (the HashSet will keep the data clean/unique)
            targetContact.Properties = new HashSet<ContactProperty>(targetContact.Properties ?? Enumerable.Empty<ContactProperty>())
            {
                // captures reference metadata to pass along when updating HubSpot contact data
                new ContactProperty
                {
                    Property = "MinistryPlatformContactId".ToLowerInvariant(),
                    Value = devProperties.MinistryPlatformContactId
                },
                new ContactProperty
                {
                    Property = "source",
                    Value = devProperties.Source
                },
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
            targetContact.Properties = new HashSet<ContactProperty>(targetContact.Properties ?? Enumerable.Empty<ContactProperty>())
            {
                new ContactProperty
                {
                    Property = "email",
                    Value = coreContactProperties.Email
                },
                new ContactProperty
                {
                    Property = "firstname",
                    Value = coreContactProperties.Firstname
                },
                new ContactProperty
                {
                    Property = "lastname",
                    Value = coreContactProperties.Lastname
                },
                new ContactProperty
                {
                    Property = "marital_status",
                    Value = coreContactProperties.MaritalStatus
                },
                new ContactProperty
                {
                    Property = "gender",
                    Value = coreContactProperties.Gender
                },
                new ContactProperty
                {
                    Property = "community",
                    Value = coreContactProperties.Community
                }
            }.ToList();
        }

        /// <summary>
        /// Projects contact updates originating from the MP audit log to HubSpot contact properties.
        /// </summary>
        /// <param name="contactUpdates">
        /// Individual audit log records of change for a given contact. Equates to 1 log per field change.
        /// </param>
        private ISet<ContactProperty> ToProperties(List<CoreUpdateMpContactDto> contactUpdates)
        {
            return new HashSet<ContactProperty>(contactUpdates.Select(update => new ContactProperty
            {
                // captures all updates found in the MP audit log
                Property = update.PropertyName,
                Value = update.NewValue
            }));
        }

        /// <summary>
        /// Projects Kids Club and Student Ministry updates originating from the MP age & grade deltas to HubSpot contact properties.
        /// </summary>
        /// <param name="contactAgeGradeUpdates">
        /// Age/grade data
        /// </param>
        private ISet<ContactProperty> ToProperties(AgeAndGradeGroupCountsForMpContactDto contactAgeGradeUpdates)
        {
            return new HashSet<ContactProperty>
            {
                // all age and grade data from here down
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_Infants).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_Infants.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_1_Year_Olds).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_1_Year_Olds.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_2_Year_Olds).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_2_Year_Olds.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_3_Year_Olds).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_3_Year_Olds.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_4_Year_Olds).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_4_Year_Olds.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_5_Year_Olds).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_5_Year_Olds.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_Kindergartners).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_Kindergartners.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_1st_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_1st_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_2nd_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_2nd_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_3rd_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_3rd_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_4th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_4th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_5th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_5th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_6th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_6th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_7th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_7th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_8th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_8th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_9th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_9th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_10th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_10th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_11th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_11th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_12th_Graders).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_12th_Graders.ToString()
                },
                new ContactProperty
                {
                    Property = nameof(contactAgeGradeUpdates.Number_of_Graduated_Seniors).ToLowerInvariant(),
                    Value = contactAgeGradeUpdates.Number_of_Graduated_Seniors.ToString()
                }
            };
        }
    }
}
