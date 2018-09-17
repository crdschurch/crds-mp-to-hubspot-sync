using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Test.Services
{
    public class PrepareDataForHubSpotTests
    {
        private readonly PrepareMpDataForHubSpot _fixture;

        private readonly Mock<IMapper> _mapperMock;

        public PrepareDataForHubSpotTests()
        {
            _mapperMock = new Mock<IMapper>(MockBehavior.Strict);
            _fixture = new PrepareMpDataForHubSpot(_mapperMock.Object);
        }

        [Fact]
        public void Given_A_List_Of_MP_Core_Contact_Updates__HubSpot_Bound_DTOs_Should_Be_Segmented_By_Attributes_Changed_To_Ensure_Successful_Sync()
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

            _mapperMock.Setup(m => m.Map<EmailAddressChangedHubSpotContact>(updates["87654321"].First()))
                .Returns(new EmailAddressChangedHubSpotContact { Properties = new List<HubSpotContactProperty> { new HubSpotContactProperty { Name = "email", Value = "new@address.com" } } });

            _mapperMock.Setup(m => m.Map<NonEmailAttributesChangedHubSpotContact>(updates["12345678"].First()))
                .Returns(new NonEmailAttributesChangedHubSpotContact { Properties = new List<HubSpotContactProperty> { new HubSpotContactProperty { Name = "firstname", Value = "Bobbo" } } });

            var result = _fixture.Prep(updates);
            result[0].Should().BeOfType<EmailAddressChangedHubSpotContact>();
            result[1].Should().BeOfType<NonEmailAttributesChangedHubSpotContact>();
        }
    }
}