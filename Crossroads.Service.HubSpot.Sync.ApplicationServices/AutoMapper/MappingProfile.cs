using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper
{
    public class MappingProfile : Profile
    {
        private const string EnvironmentPropertyName = "environment";
        private const string MaritalStatusPropertyName = "marital_status";

        public MappingProfile(IConfigurationService configurationService)
        {
            var environment = configurationService.GetEnvironmentName();

            // CREATE SCENARIOS
            // MP data to HubSpot mapping definitions
            CreateMap<NewlyRegisteredMpContactDto, BulkContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(dto => dto.Email))
                .ForMember(hubSpotContact => hubSpotContact.Properties, memberOptions => memberOptions.MapFrom(dto =>
                    new HashSet<ContactProperty>
                    {
                        new ContactProperty
                        {
                            Property = nameof(dto.MinistryPlatformContactId).ToLowerInvariant(),
                            Value = dto.MinistryPlatformContactId
                        },
                        new ContactProperty
                        {
                            Property = nameof(dto.Firstname).ToLowerInvariant(),
                            Value = dto.Firstname.CapitalizeFirstLetter()
                        },
                        new ContactProperty
                        {
                            Property = nameof(dto.Lastname).ToLowerInvariant(),
                            Value = dto.Lastname.CapitalizeFirstLetter()
                        },
                        new ContactProperty
                        {
                            Property = MaritalStatusPropertyName,
                            Value = dto.MaritalStatus
                        },
                        new ContactProperty
                        {
                            Property = nameof(dto.Gender).ToLowerInvariant(),
                            Value = dto.Gender
                        },
                        new ContactProperty
                        {
                            Property = nameof(dto.Community).ToLowerInvariant(),
                            Value = dto.Community
                        },
                        new ContactProperty
                        {
                            Property = nameof(dto.LifeCycleStage).ToLowerInvariant(),
                            Value = dto.LifeCycleStage
                        },
                        new ContactProperty
                        {
                            Property = nameof(dto.Source).ToLowerInvariant(),
                            Value = dto.Source
                        },
                        new ContactProperty
                        {
                            Property = EnvironmentPropertyName,
                            Value = environment
                        }
                    }));

            // HubSpot bulk contact to HubSpot serial contact
            // HubSpot structures contact properties slightly differently for bulk (create or update) and serial create endpoints
            CreateMap<BulkContact, SerialCreateContact>()
                .ForMember(serialCreateContact => serialCreateContact.Properties, memberOptions =>
                    memberOptions.MapFrom(bulkCreateContact =>
                        bulkCreateContact.Properties.Append(new ContactProperty
                        {
                            Property = nameof(bulkCreateContact.Email).ToLowerInvariant(),
                            Value = bulkCreateContact.Email
                        })));

            // UPDATE SCENARIOS
            // when an updated contact who previously had NO email address associated now has one, CREATE contact
            CreateMap<List<MpContactUpdateDto>, EmailAddressCreatedContact>()
                .ForMember(nowHasEmailContact => nowHasEmailContact.Properties, memberOptions => memberOptions.MapFrom(updates => ToProperties(updates, environment)))
                .AfterMap((updates, contactToBeCreated) => // *Attempt* to add all the core properties for creation (the HashSet will keep the data clean/unique)
                    contactToBeCreated.Properties = new HashSet<ContactProperty>(contactToBeCreated.Properties)
                    {
                        new ContactProperty
                        {
                            Property = "email",
                            Value = updates.First().Email
                        },
                        new ContactProperty
                        {
                            Property = "firstname",
                            Value = updates.First().Firstname
                        },
                            new ContactProperty
                        {
                            Property = "lastname",
                            Value = updates.First().Lastname
                        },
                        new ContactProperty
                        {
                            Property = MaritalStatusPropertyName,
                            Value = updates.First().MaritalStatus
                        },
                        new ContactProperty
                        {
                            Property = "gender",
                            Value = updates.First().Gender
                        },
                        new ContactProperty
                        {
                            Property = "community",
                            Value = updates.First().Community
                        }
                    }.ToList());

            // when an updated contact's email address changed from one value to another
            CreateMap<List<MpContactUpdateDto>, EmailAddressChangedContact>()
                .ForMember(emailChanged => emailChanged.Email, memberOptions =>
                    memberOptions.MapFrom(updates => updates.First(update => update.PropertyName == "email").PreviousValue))
                .ForMember(emailChanged => emailChanged.Properties, memberOptions => memberOptions.MapFrom(updates => ToProperties(updates, environment)));

            // when only core, non-email, updates have changed
            CreateMap<List<MpContactUpdateDto>, CoreOnlyChangedContact>()
                .ForMember(coreOnlyChanged => coreOnlyChanged.Email, memberOptions => memberOptions.MapFrom(updates => updates.First().Email))
                .ForMember(coreOnlyChanged => coreOnlyChanged.Properties, memberOptions => memberOptions.MapFrom(updates => ToProperties(updates, environment)));
        }

        /// <summary>
        /// Projects contact updates originating from the MP audit log to HubSpot contact properties.
        /// </summary>
        /// <param name="contactUpdates">
        /// Individual audit log records of change for a given contact. Equates to 1 log per field change.
        /// </param>
        /// <param name="environmentName">Name of the environment in which the application is executing.</param>
        private ISet<ContactProperty> ToProperties(List<MpContactUpdateDto> contactUpdates, string environmentName)
        {
            return new HashSet<ContactProperty>(contactUpdates.Select(update => new ContactProperty
            {
                // captures all updates found in the MP audit log
                Property = update.PropertyName,
                Value = update.NewValue
            }))
            {
                // captures reference metadata to pass along when updating HubSpot contact data
                new ContactProperty
                {
                    Property = "MinistryPlatformContactId".ToLowerInvariant(),
                    Value = contactUpdates.FirstOrDefault().MinistryPlatformContactId
                },
                new ContactProperty
                {
                    Property = "source",
                    Value = contactUpdates.FirstOrDefault().Source
                },
                new ContactProperty
                {
                    Property = EnvironmentPropertyName,
                    Value = environmentName
                }
            };
        }
    }
}
