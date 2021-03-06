﻿using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Test.Services
{
    public class CreateOrUpdateContactsInHubSpotTests
    {
        private readonly CreateOrUpdateContactsInHubSpot _fixture;
        private readonly Mock<IHttpClientFacade> _httpMock;
        private readonly Mock<IJsonSerializer> _serializerMock;
        private readonly Mock<ISleep> _sleeperMock;
        const string HubSpotApiKey = "apiKey_123456789";
        private readonly Mock<ILogger<CreateOrUpdateContactsInHubSpot>> _loggerMock;

        /// <summary>
        /// One thousand milliseconds = 1 second
        /// </summary>
        private const int OneSecond = 1000;

        private readonly BulkHubSpotContact[] _bulkHubSpotContacts =
        {
            new BulkHubSpotContact { Email = "email@1.com", Properties = PopulateProperties()},
            new BulkHubSpotContact { Email = "email@2.com", Properties = PopulateProperties() },
            new BulkHubSpotContact { Email = "email@3.com", Properties = PopulateProperties() },
            new BulkHubSpotContact { Email = "email@4.com", Properties = PopulateProperties() },
            new BulkHubSpotContact { Email = "email@5.com", Properties = PopulateProperties() },
            new BulkHubSpotContact { Email = "email@6.com", Properties = PopulateProperties() },
            new BulkHubSpotContact { Email = "email@7.com", Properties = PopulateProperties() },
            new BulkHubSpotContact { Email = "email@8.com", Properties = PopulateProperties() },
            new BulkHubSpotContact { Email = "email@9.com", Properties = PopulateProperties() }
        };

        private static List<HubSpotContactProperty> PopulateProperties()
        {
            return new List<HubSpotContactProperty> {new HubSpotContactProperty {Name = "email"}};
        }

        private readonly SerialHubSpotContact[] _serialHubSpotContacts =
        {
            new SerialHubSpotContact { Email = "email@1.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@2.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@3.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@4.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@5.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@6.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@7.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@8.com", Properties = PopulateProperties() },
            new SerialHubSpotContact { Email = "email@9.com", Properties = PopulateProperties() }
        };
        private readonly DateTime _utcNowMockDateTime = DateTime.Parse("2018-05-16T13:05:01");

        public CreateOrUpdateContactsInHubSpotTests()
        {
            _httpMock = new Mock<IHttpClientFacade>(MockBehavior.Strict);
            var clockMock = new Mock<IClock>(MockBehavior.Strict);
            _serializerMock = new Mock<IJsonSerializer>(MockBehavior.Strict);
            _sleeperMock = new Mock<ISleep>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<CreateOrUpdateContactsInHubSpot>>(MockBehavior.Default);
            _fixture = new CreateOrUpdateContactsInHubSpot(_httpMock.Object, clockMock.Object, _serializerMock.Object, _sleeperMock.Object, HubSpotApiKey, _loggerMock.Object);

            // default setups
            clockMock.Setup(clock => clock.UtcNow).Returns(_utcNowMockDateTime);
            _sleeperMock.Setup(s => s.Sleep(OneSecond));
        }

        private void SetUpMockDefinitions(HttpStatusCode httpStatusCode, bool isNew = false)
        {
            var httpResponseMessage = new HttpResponseMessage(httpStatusCode);
            _httpMock.Setup(http => http.Post(It.IsAny<string>(), It.IsAny<BulkHubSpotContact[]>())).Returns(httpResponseMessage);
            _httpMock.Setup(http => http.Post(It.IsAny<string>(), It.IsAny<SerialHubSpotContact>())).Returns(httpResponseMessage);
            _httpMock.Setup(h => h.GetResponseContent<HubSpotException>(It.IsAny<HttpResponseMessage>())).Returns(new HubSpotException()); // for bulk/serial
            _serializerMock.Setup(s => s.Serialize(It.IsAny<IHubSpotContact>())).Returns("");
        }

        private void HappyOrSadPathTruths(BulkSyncResult result, BulkHubSpotContact[] hubSpotContacts, int expectedBatchCount, int successCount, int failureCount)
        {
            HappyOrSadPathTruths(result, hubSpotContacts.Length, expectedBatchCount, successCount, failureCount);

            // assert data
            result.BatchCount.Should().Be(expectedBatchCount);

            // assert behavior
            _httpMock.Verify(http => http.Post(It.IsAny<string>(), It.IsAny<BulkHubSpotContact[]>()), Times.Exactly(expectedBatchCount));
        }

        private void HappyOrSadPathTruths(SerialSyncResult result, SerialHubSpotContact[] hubSpotContacts, int successCount, int failureCount)
        {
            HappyOrSadPathTruths(result, hubSpotContacts.Length, hubSpotContacts.Length, successCount, failureCount);

            // assert behavior
            _httpMock.Verify(http => http.Post(It.IsAny<string>(), It.IsAny<SerialHubSpotContact>()), Times.Exactly(hubSpotContacts.Length));
        }

        private void HappyOrSadPathTruths(ISyncResult result, int contactCount, int numberOfRequests, int successCount, int failureCount)
        {
            result.TotalContacts.Should().Be(contactCount);
            result.SuccessCount.Should().Be(successCount);
            result.FailureCount.Should().Be(failureCount);
            result.Execution.StartUtc.Should().Be(_utcNowMockDateTime);
            result.Execution.FinishUtc.Should().Be(_utcNowMockDateTime);

            // assert behavior
            _sleeperMock.Verify(sleeper => sleeper.Sleep(OneSecond), Times.Exactly(numberOfRequests > 6 ? 1 : 0));
        }

        [Theory]
        [InlineData(9, 1)]
        [InlineData(5, 2)]
        [InlineData(3, 3)]
        [InlineData(2, 5)]
        [InlineData(1, 9)]
        public void BulkSyncResult_HappyPath(int batchSize, int expectedBatchCount)
        {
            // arrange
            SetUpMockDefinitions(HttpStatusCode.Accepted);

            // act
            var result = _fixture.BulkSync(_bulkHubSpotContacts, batchSize: batchSize);

            // assert data
            HappyOrSadPathTruths(result, _bulkHubSpotContacts, expectedBatchCount, successCount: _bulkHubSpotContacts.Length, failureCount: 0); // data and behavior
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, 9, 1)]
        [InlineData(HttpStatusCode.Unauthorized, 1, 9)]
        [InlineData(HttpStatusCode.InternalServerError, 3, 3)]
        public void BulkSyncResult_When_All_Requests_Have_A_Negative_Result(HttpStatusCode httpStatusCode, int batchSize, int expectedBatchCount)
        {
            // arrange
            SetUpMockDefinitions(httpStatusCode);

            // act
            var result = _fixture.BulkSync(_bulkHubSpotContacts, batchSize: batchSize);

            // assert data
            result.FailedBatches.Count.Should().Be(expectedBatchCount);
            result.FailedBatches.Count(fail => fail.HttpStatusCode == httpStatusCode).Should().Be(expectedBatchCount);
            HappyOrSadPathTruths(result, _bulkHubSpotContacts, expectedBatchCount, successCount: 0, failureCount: _bulkHubSpotContacts.Length); // data and behavior
        }

        [Fact]
        public void BulkSyncResult_When_Request_Exception_Occurs_Let_It_Propagate()
        {   // if we can't connect to HubSpot, let's hope the failure is temporal and try again later.
            // arrange
            SetUpMockDefinitions(HttpStatusCode.Ambiguous);
            _httpMock.Setup(http => http.Post(It.IsAny<string>(), It.IsAny<BulkHubSpotContact[]>())).Throws<HttpRequestException>();

            // act
            Action action = () => _fixture.BulkSync(_bulkHubSpotContacts);

            action.Should().Throw<HttpRequestException>();
        }

        [Theory]
        [InlineData(9)]
        [InlineData(8)]
        [InlineData(7)]
        [InlineData(6)]
        public void SerialCreateResult_HappyPath(int numberOfContactsToSync)
        {
            // arrange
            var contacts = _serialHubSpotContacts.Take(numberOfContactsToSync).ToArray();
            SetUpMockDefinitions(HttpStatusCode.OK);

            // act
            var result = _fixture.SerialCreate(contacts);

            // assert data
            HappyOrSadPathTruths(result, contacts, successCount: contacts.Length, failureCount: 0); // data and behavior
        }

        [Theory]
        [InlineData(9)]
        [InlineData(8)]
        [InlineData(7)]
        [InlineData(6)]
        public void SerialUpdateResult_HappyPath(int numberOfContactsToSync)
        {
            // arrange
            var contacts = _serialHubSpotContacts.Take(numberOfContactsToSync).ToArray();
            SetUpMockDefinitions(HttpStatusCode.NoContent);

            // act
            var result = _fixture.SerialUpdate(contacts);

            // assert data
            HappyOrSadPathTruths(result, contacts, successCount: contacts.Length, failureCount: 0); // data and behavior
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, 9)]
        [InlineData(HttpStatusCode.Unauthorized, 8)]
        [InlineData(HttpStatusCode.InternalServerError, 7)]
        [InlineData(HttpStatusCode.Forbidden, 6)]
        public void SerialCreateResult_When_All_Requests_Have_A_Negative_Result(HttpStatusCode httpStatusCode, int numberOfContactsToSync)
        {
            // arrange
            var contacts = _serialHubSpotContacts.Take(numberOfContactsToSync).ToArray();
            SetUpMockDefinitions(httpStatusCode);

            // act
            var result = _fixture.SerialCreate(contacts);

            // assert data
            result.Failures.Count.Should().Be(contacts.Length);
            result.Failures.Count(fail => fail.HttpStatusCode == httpStatusCode).Should().Be(contacts.Length);
            HappyOrSadPathTruths(result, contacts, successCount: 0, failureCount: contacts.Length); // data and behavior
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, 9)]
        [InlineData(HttpStatusCode.Unauthorized, 8)]
        [InlineData(HttpStatusCode.InternalServerError, 7)]
        [InlineData(HttpStatusCode.Forbidden, 6)]
        public void SerialUpdateResult_When_All_Requests_Have_A_Negative_Result(HttpStatusCode httpStatusCode, int numberOfContactsToSync)
        {
            // arrange
            var contacts = _serialHubSpotContacts.Take(numberOfContactsToSync).ToArray();
            SetUpMockDefinitions(httpStatusCode);

            // act
            var result = _fixture.SerialUpdate(contacts);

            // assert data
            result.Failures.Count.Should().Be(contacts.Length);
            result.Failures.Count(fail => fail.HttpStatusCode == httpStatusCode).Should().Be(contacts.Length);
            HappyOrSadPathTruths(result, contacts, successCount: 0, failureCount: contacts.Length); // data and behavior
        }

        [Fact]
        public void SerialCreateResult_When_An_Email_Address_Already_Exists()
        {
            // arrange
            SetUpMockDefinitions(HttpStatusCode.Conflict);

            // act
            var result = _fixture.SerialCreate(_serialHubSpotContacts);

            // assert data
            result.Failures.Count.Should().Be(0);
            result.EmailAddressesAlreadyExist.Count.Should().Be(_serialHubSpotContacts.Length);
            result.EmailAddressAlreadyExistsCount.Should().Be(_serialHubSpotContacts.Length);

            HappyOrSadPathTruths(result, _serialHubSpotContacts, successCount: 0, failureCount: 0); // data and behavior
        }

        [Fact]
        public void SerialUpdateResult_When_An_Email_Address_Does_Not_Exist()
        {
            // arrange
            SetUpMockDefinitions(HttpStatusCode.NotFound);

            // act
            var result = _fixture.SerialUpdate(_serialHubSpotContacts);

            // assert data
            result.Failures.Count.Should().Be(0);
            result.EmailAddressesDoNotExist.Count.Should().Be(_serialHubSpotContacts.Length);
            result.EmailAddressDoesNotExistCount.Should().Be(_serialHubSpotContacts.Length);

            HappyOrSadPathTruths(result, _serialHubSpotContacts, successCount: 0, failureCount: 0); // data and behavior
        }

        [Fact]
        public void SerialCreateResult_When_Request_Exception_Occurs_Let_It_Propagate()
        {   // if we can't connect to HubSpot, let's hope the failure is temporal and try again later.
            // arrange
            SetUpMockDefinitions(HttpStatusCode.Ambiguous);
            _httpMock.Setup(http => http.Post(It.IsAny<string>(), It.IsAny<SerialHubSpotContact>())).Throws<HttpRequestException>();

            // act
            Action action = () => _fixture.SerialCreate(_serialHubSpotContacts);

            action.Should().Throw<HttpRequestException>();
        }

        [Fact]
        public void SerialUpdateResult_When_Request_Exception_Occurs_Let_It_Propagate()
        {   // if we can't connect to HubSpot, let's hope the failure is temporal and try again later.
            // arrange
            SetUpMockDefinitions(HttpStatusCode.Ambiguous);
            _httpMock.Setup(http => http.Post(It.IsAny<string>(), It.IsAny<SerialHubSpotContact>())).Throws<HttpRequestException>();

            // act
            Action action = () => _fixture.SerialUpdate(_serialHubSpotContacts);

            action.Should().Throw<HttpRequestException>();
        }
    }
}
