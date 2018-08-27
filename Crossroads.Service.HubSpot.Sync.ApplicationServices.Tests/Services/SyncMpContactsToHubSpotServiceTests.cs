using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
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
        private readonly Mock<ICleanUpSyncActivity> _syncActivityCleanerMock;
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
            IValidator<ISyncActivity> syncActivityValidator = new SyncActivityValidator();
            _syncActivityCleanerMock = new Mock<ICleanUpSyncActivity>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<SyncMpContactsToHubSpotService>>(MockBehavior.Default);

            _fixture = new SyncMpContactsToHubSpotService(
                _mpContactRepoMock.Object,
                _hubSpotSyncerMock.Object,
                _clockMock.Object,
                _configSvcMock.Object,
                _jobRepoMock.Object,
                _dataPrepMock.Object,
                syncActivityValidator,
                _syncActivityCleanerMock.Object,
                _loggerMock.Object);
        }

        private void SetUpDefaults()
        {
            var utcNowMockDateTime = DateTime.Parse("2018-05-21T08:00:00"); // 4a "local"

            var ageGradeProcessDate = DateTime.Parse("2018-05-21 12:00:00AM");
            var registrationDate = DateTime.Parse("2018-05-21 1:00:00AM");
            var coreUpdateDate = DateTime.Parse("2018-05-21 2:00:00AM");
            var ageGradeSyncDate = DateTime.Parse("2018-05-21 3:00:00AM");
            var syncDates = new SyncDates
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
            _hubSpotSyncerMock.Setup(syncer => syncer.BulkSync(It.IsAny<BulkContact[]>(), 100)).Returns(new BulkSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.BulkSync(It.IsAny<BulkContact[]>(), 10)).Returns(new BulkSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.SerialCreate(It.IsAny<SerialContact[]>())).Returns(new SerialSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.SerialUpdate(It.IsAny<SerialContact[]>())).Returns(new SerialSyncResult());
            _clockMock.Setup(clock => clock.UtcNow).Returns(utcNowMockDateTime);
            _configSvcMock.Setup(svc => svc.GetCurrentJobProcessingState()).Returns(SyncProcessingState.Idle);
            _configSvcMock.Setup(svc => svc.GetLastSuccessfulSyncDates()).Returns(syncDates);
            _configSvcMock.Setup(svc => svc.PersistActivity()).Returns(true);
            _jobRepoMock.Setup(repo => repo.SetSyncJobProcessingState(It.IsAny<SyncProcessingState>())).Returns<SyncProcessingState>(x => x);
            _jobRepoMock.Setup(repo => repo.SetLastSuccessfulSyncDates(It.IsAny<SyncDates>())).Returns<SyncDates>(x => x);
            _jobRepoMock.Setup(repo => repo.SaveHubSpotApiDailyRequestCount(It.IsAny<int>(), It.IsAny<DateTime>())).Returns(true);
            _jobRepoMock.Setup(repo => repo.SaveSyncActivity(It.IsAny<ISyncActivity>())).Returns(true);
            _dataPrepMock.Setup(prep => prep.Prep(It.IsAny<IDictionary<string, List<CoreUpdateMpContactDto>>>())).Returns(new SerialContact[0]);
            _dataPrepMock.Setup(prep => prep.Prep(It.IsAny<List<AgeAndGradeGroupCountsForMpContactDto>>())).Returns(new BulkContact[0]);
            _dataPrepMock.Setup(prep => prep.ToBulk(It.IsAny<List<BulkSyncFailure>>())).Returns(new BulkContact[0]);
            _dataPrepMock.Setup(prep => prep.ToSerial(It.IsAny<List<BulkSyncFailure>>())).Returns(new SerialContact[0]);
            _syncActivityCleanerMock.Setup(activityCleaner => activityCleaner.CleanUp(It.IsAny<ISyncActivity>()));
        }

        [Fact]
        public async Task HappyPath()
        {
            // arrange
            SetUpDefaults();

            // act
            var result = await _fixture.Sync();

            // assert
            result.NewRegistrationOperation.Should().NotBeNull();
            result.CoreUpdateOperation.Should().NotBeNull();
            result.ChildAgeAndGradeUpdateOperation.Should().NotBeNull();

            _mpContactRepoMock.Verify(repo => repo.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas(), Times.Once);
            _hubSpotSyncerMock.Verify(syncer => syncer.BulkSync(It.IsAny<BulkContact[]>(), 100), Times.Once);
            _hubSpotSyncerMock.Verify(syncer => syncer.SerialCreate(It.IsAny<SerialContact[]>()), Times.Never);
            _hubSpotSyncerMock.Verify(syncer => syncer.SerialUpdate(It.IsAny<SerialContact[]>()), Times.Once);
            _clockMock.Verify(clock => clock.UtcNow, Times.Exactly(8));
            _configSvcMock.Verify(svc => svc.GetCurrentJobProcessingState(), Times.Once);
            _configSvcMock.Verify(svc => svc.GetLastSuccessfulSyncDates(), Times.Once);
            _jobRepoMock.Verify(repo => repo.SetSyncJobProcessingState(It.IsAny<SyncProcessingState>()), Times.Exactly(2));
            _jobRepoMock.Verify(repo => repo.SetLastSuccessfulSyncDates(It.IsAny<SyncDates>()), Times.Exactly(2));
            _jobRepoMock.Verify(repo => repo.SaveHubSpotApiDailyRequestCount(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Exactly(3));
            _jobRepoMock.Verify(repo => repo.SaveSyncActivity(It.IsAny<ISyncActivity>()), Times.Once);
            _dataPrepMock.Verify(prep => prep.Prep(It.IsAny<IList<NewlyRegisteredMpContactDto>>()), Times.Never);
            _dataPrepMock.Verify(prep => prep.Prep(It.IsAny<IDictionary<string, List<CoreUpdateMpContactDto>>>()), Times.Never);
            _dataPrepMock.Verify(prep => prep.Prep(It.IsAny<List<AgeAndGradeGroupCountsForMpContactDto>>()), Times.Once);
            _dataPrepMock.Verify(prep => prep.ToBulk(It.IsAny<List<BulkSyncFailure>>()), Times.Once);
            _dataPrepMock.Verify(prep => prep.ToSerial(It.IsAny<List<BulkSyncFailure>>()), Times.Once);
            _syncActivityCleanerMock.Verify(activityCleaner => activityCleaner.CleanUp(It.IsAny<ISyncActivity>()), Times.Once);
        }
    }
}
