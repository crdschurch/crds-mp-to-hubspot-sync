using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Test.Services
{
    public class SyncMpContactsToHubSpotServiceTests
    {
        private readonly Mock<IMinistryPlatformContactRepository> _mpContactRepoMock;
        private readonly Mock<ICreateOrUpdateContactsInHubSpot> _hubSpotSyncerMock;
        private readonly Mock<IClock> _clockMock;
        private readonly Mock<IConfigurationService> _configSvcMock;
        private readonly Mock<IJobRepository> _jobRepoMock;
        private readonly Mock<IPrepareMpDataForHubSpot> _dataPrepMock;
        private readonly Mock<ICleanUpActivity> _activityCleanerMock;
        private readonly Mock<ILogger<SyncMpContactsToHubSpotService>> _loggerMock;
        private readonly SyncMpContactsToHubSpotService _fixture;

        public SyncMpContactsToHubSpotServiceTests()
        {
            _mpContactRepoMock = new Mock<IMinistryPlatformContactRepository>(MockBehavior.Strict);
            _hubSpotSyncerMock = new Mock<ICreateOrUpdateContactsInHubSpot>(MockBehavior.Strict);
            _clockMock = new Mock<IClock>(MockBehavior.Strict);
            _configSvcMock = new Mock<IConfigurationService>(MockBehavior.Strict);
            _jobRepoMock = new Mock<IJobRepository>(MockBehavior.Strict);
            _dataPrepMock = new Mock<IPrepareMpDataForHubSpot>(MockBehavior.Strict);
            IValidator<IActivity> activityValidator = new ActivityValidator();
            _activityCleanerMock = new Mock<ICleanUpActivity>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<SyncMpContactsToHubSpotService>>(MockBehavior.Default);

            _fixture = new SyncMpContactsToHubSpotService(
                _mpContactRepoMock.Object,
                _hubSpotSyncerMock.Object,
                _clockMock.Object,
                _configSvcMock.Object,
                _jobRepoMock.Object,
                _dataPrepMock.Object,
                activityValidator,
                _activityCleanerMock.Object,
                _loggerMock.Object);
        }

        private void SetUpDefaults()
        {
            var utcNowMockDateTime = DateTime.Parse("2018-05-21T08:00:00"); // 4a "local"

            var ageGradeProcessDate = DateTime.Parse("2018-05-21 12:00:00AM");
            var registrationDate = DateTime.Parse("2018-05-21 1:00:00AM");
            var coreUpdateDate = DateTime.Parse("2018-05-21 2:00:00AM");
            var ageGradeSyncDate = DateTime.Parse("2018-05-21 3:00:00AM");
            var operationDates = new OperationDates
            {
                RegistrationSyncDate = registrationDate,
                CoreUpdateSyncDate = coreUpdateDate,
                AgeAndGradeProcessDate = ageGradeProcessDate,
                AgeAndGradeSyncDate = ageGradeSyncDate
            };

            var initialChildAgeGradeDto = new ChildAgeAndGradeDeltaLogDto
            {
                InsertCount = 20,
                UpdateCount = 2,
                ProcessedUtc = DateTime.Parse("2018-05-22 12:00:00AM"),
                SyncCompletedUtc = null
            };

            var finalChildAgeGradeDto = new ChildAgeAndGradeDeltaLogDto
            {
                InsertCount = 20,
                UpdateCount = 2,
                ProcessedUtc = DateTime.Parse("2018-05-22 12:00:00AM"),
                SyncCompletedUtc = DateTime.Parse("2018-05-22 03:00:00AM")
            };

            var ageGradeGroups = new List<AgeAndGradeGroupCountsForMpContactDto>();

            _mpContactRepoMock.Setup(repo => repo.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas()).Returns(initialChildAgeGradeDto);
            _mpContactRepoMock.Setup(repo => repo.GetAgeAndGradeGroupDataForContacts()).Returns(ageGradeGroups);
            _mpContactRepoMock.Setup(repo => repo.SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate()).Returns(finalChildAgeGradeDto.SyncCompletedUtc ?? default(DateTime));
            _mpContactRepoMock.Setup(repo => repo.GetNewlyRegisteredContacts(operationDates.RegistrationSyncDate)).Returns(new List<NewlyRegisteredMpContactDto>());
            _mpContactRepoMock.Setup(repo => repo.GetAuditedContactUpdates(operationDates.CoreUpdateSyncDate)).Returns(new Dictionary<string, List<CoreUpdateMpContactDto>>());
            _hubSpotSyncerMock.Setup(syncer => syncer.BulkSync(It.IsAny<BulkHubSpotContact[]>(), 1000)).Returns(new BulkSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.BulkSync(It.IsAny<BulkHubSpotContact[]>(), 100)).Returns(new BulkSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.BulkSync(It.IsAny<BulkHubSpotContact[]>(), 10)).Returns(new BulkSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.SerialCreate(It.IsAny<SerialHubSpotContact[]>())).Returns(new SerialSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.SerialUpdate(It.IsAny<SerialHubSpotContact[]>())).Returns(new SerialSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.ReconcileConflicts(It.IsAny<SerialHubSpotContact[]>())).Returns(new SerialSyncResult());
            _clockMock.Setup(clock => clock.UtcNow).Returns(utcNowMockDateTime);
            _configSvcMock.Setup(svc => svc.GetCurrentActivityProgress()).Returns(new ActivityProgress { ActivityState = ActivityState.Idle});
            _configSvcMock.Setup(svc => svc.GetLastSuccessfulOperationDates()).Returns(operationDates);
            _configSvcMock.Setup(svc => svc.PersistActivity()).Returns(true);
            _jobRepoMock.Setup(repo => repo.PersistActivityProgress(It.IsAny<ActivityProgress>()));
            _jobRepoMock.Setup(repo => repo.PersistLastSuccessfulOperationDates(It.IsAny<OperationDates>())).Returns<OperationDates>(x => x);
            _jobRepoMock.Setup(repo => repo.PersistHubSpotApiDailyRequestCount(It.IsAny<int>(), It.IsAny<DateTime>()));
            _jobRepoMock.Setup(repo => repo.PersistActivity(It.IsAny<Activity>()));
            _dataPrepMock.Setup(prep => prep.Prep(It.IsAny<IDictionary<string, List<CoreUpdateMpContactDto>>>())).Returns(new SerialHubSpotContact[0]);
            _dataPrepMock.Setup(prep => prep.Prep(It.IsAny<List<AgeAndGradeGroupCountsForMpContactDto>>())).Returns(new BulkHubSpotContact[0]);
            _dataPrepMock.Setup(prep => prep.Prep(It.IsAny<List<NewlyRegisteredMpContactDto>>())).Returns(new SerialHubSpotContact[0]);
            _dataPrepMock.Setup(prep => prep.ToBulk(It.IsAny<List<BulkSyncFailure>>())).Returns(new BulkHubSpotContact[0]);
            _dataPrepMock.Setup(prep => prep.ToSerial(It.IsAny<List<BulkSyncFailure>>())).Returns(new SerialHubSpotContact[0]);
            _activityCleanerMock.Setup(activityCleaner => activityCleaner.CleanUp(It.IsAny<IActivity>()));
        }

        [Fact]
        public async Task HappyPath()
        {
            // arrange
            SetUpDefaults();

            // act
            var result = await _fixture.Sync();

            // assert
            result.NewRegistrationSyncOperation.Should().NotBeNull();
            result.CoreContactAttributeSyncOperation.Should().NotBeNull();
            result.ChildAgeAndGradeSyncOperation.Should().NotBeNull();

            _mpContactRepoMock.Verify(repo => repo.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas(), Times.Once);
            _hubSpotSyncerMock.Verify(syncer => syncer.BulkSync(It.IsAny<BulkHubSpotContact[]>(), 1000), Times.Once);
            _hubSpotSyncerMock.Verify(syncer => syncer.BulkSync(It.IsAny<BulkHubSpotContact[]>(), 100), Times.Once);
            _hubSpotSyncerMock.Verify(syncer => syncer.BulkSync(It.IsAny<BulkHubSpotContact[]>(), 10), Times.Once);
            _hubSpotSyncerMock.Verify(syncer => syncer.SerialCreate(It.IsAny<SerialHubSpotContact[]>()), Times.Exactly(3));
            _hubSpotSyncerMock.Verify(syncer => syncer.SerialUpdate(It.IsAny<SerialHubSpotContact[]>()), Times.Exactly(3));
            _clockMock.Verify(clock => clock.UtcNow, Times.Exactly(10));
            _configSvcMock.Verify(svc => svc.GetCurrentActivityProgress(), Times.Once);
            _configSvcMock.Verify(svc => svc.GetLastSuccessfulOperationDates(), Times.Once);
            _jobRepoMock.Verify(repo => repo.PersistActivityProgress(It.IsAny<ActivityProgress>()), Times.Exactly(14));
            _jobRepoMock.Verify(repo => repo.PersistLastSuccessfulOperationDates(It.IsAny<OperationDates>()), Times.Exactly(4));
            _jobRepoMock.Verify(repo => repo.PersistHubSpotApiDailyRequestCount(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Exactly(3));
            _jobRepoMock.Verify(repo => repo.PersistActivity(It.IsAny<Activity>()), Times.Once);
            _dataPrepMock.Verify(prep => prep.Prep(It.IsAny<IList<NewlyRegisteredMpContactDto>>()), Times.Once);
            _dataPrepMock.Verify(prep => prep.Prep(It.IsAny<IDictionary<string, List<CoreUpdateMpContactDto>>>()), Times.Once);
            _dataPrepMock.Verify(prep => prep.Prep(It.IsAny<List<AgeAndGradeGroupCountsForMpContactDto>>()), Times.Once);
            _dataPrepMock.Verify(prep => prep.ToBulk(It.IsAny<List<BulkSyncFailure>>()), Times.Exactly(2));
            _dataPrepMock.Verify(prep => prep.ToSerial(It.IsAny<List<BulkSyncFailure>>()), Times.Once);
            _activityCleanerMock.Verify(activityCleaner => activityCleaner.CleanUp(It.IsAny<IActivity>()), Times.Once);
        }
    }
}
