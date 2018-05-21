using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Test.Services
{
    public class PrepareDataForHubSpotTests
    {
        private readonly PrepareDataForHubSpot _fixture;

        private readonly Mock<IMapper> _mapperMock;

        public PrepareDataForHubSpotTests()
        {
            _mapperMock = new Mock<IMapper>(MockBehavior.Strict);
            _fixture = new PrepareDataForHubSpot(_mapperMock.Object);
        }

        [Fact]
        public void given_a_contact_with_or_without_an_email_property_the_resulting_type_should_be_emailaddresschangedcontact_or_nonemailattributeschangedcontact()
        {
            var updates = new Dictionary<string, List<CoreUpdateMpContactDto>>
            {
                {
                    "87654321", new List<CoreUpdateMpContactDto>
                    {
                        new CoreUpdateMpContactDto
                        {
                            PropertyName = "email",
                            PreviousValue = "old@address.com",
                            NewValue = "new@address.com"
                        }
                    }
                },
                {
                    "12345678", new List<CoreUpdateMpContactDto>
                    {
                        new CoreUpdateMpContactDto
                        {
                            PropertyName = "firstname",
                            PreviousValue = "Bobby",
                            NewValue = "Bobbo"
                        }
                    }
                }
            };

            _mapperMock.Setup(m => m.Map<EmailAddressChangedContact>(updates["87654321"]))
                .Returns(new EmailAddressChangedContact { Properties = new List<ContactProperty> { new ContactProperty { Property = "email", Value = "new@address.com" } } });

            _mapperMock.Setup(m => m.Map<NonEmailAttributesChangedContact>(updates["12345678"]))
                .Returns(new NonEmailAttributesChangedContact { Properties = new List<ContactProperty> { new ContactProperty { Property = "firstname", Value = "Bobbo" } } });

            var result = _fixture.Prep(updates);
            result[0].Should().BeOfType<EmailAddressChangedContact>();
            result[1].Should().BeOfType<NonEmailAttributesChangedContact>();
        }
    }
}