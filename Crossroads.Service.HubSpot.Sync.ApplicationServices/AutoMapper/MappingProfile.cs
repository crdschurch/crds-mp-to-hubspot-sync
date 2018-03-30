using System;
using System.Collections.Generic;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // MP data to HubSpot mapping definitions
            CreateMap<NewlyRegisteredContactDto, HubSpotContact>()
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(dto => dto.Email))
                .ForMember(hubSpotContact => hubSpotContact.Properties, memberOptions =>
                    memberOptions.MapFrom( dto =>
                        new List<ContactProperty>
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
                                Property = nameof(dto.Email).ToLowerInvariant(),
                                Value = dto.Email
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
                        }));
        }
    }
}
