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
        private readonly Mock<IPrepareDataForHubSpot> _dataPrepMock;
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
            _dataPrepMock = new Mock<IPrepareDataForHubSpot>(MockBehavior.Strict);
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

            var ageGradeGroups = new List<AgeAndGradeGroupCountsForMpContactDto>
            {
                //new AgeAndGradeGroupCountsForMpContactDto {Email = ""}
            };

            _mpContactRepoMock.Setup(repo => repo.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas()).Returns(initialChildAgeGradeDto);
            _mpContactRepoMock.Setup(repo => repo.GetAgeAndGradeGroupDataForContacts()).Returns(ageGradeGroups);
            _mpContactRepoMock.Setup(repo => repo.SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate()).Returns(finalChildAgeGradeDto.SyncCompletedUtc ?? default(DateTime));
            _hubSpotSyncerMock.Setup(syncer => syncer.BulkSync(It.IsAny<BulkContact[]>(), 100)).Returns(new BulkSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.BulkSync(It.IsAny<BulkContact[]>(), 10)).Returns(new BulkSyncResult());
            _hubSpotSyncerMock.Setup(syncer => syncer.SerialSync(It.IsAny<SerialContact[]>())).Returns(new SerialSyncResult());
            _clockMock.Setup(clock => clock.UtcNow).Returns(utcNowMockDateTime);
            _configSvcMock.Setup(svc => svc.GetCurrentJobProcessingState()).Returns(SyncProcessingState.Idle);
            _configSvcMock.Setup(svc => svc.GetLastSuccessfulSyncDates()).Returns(syncDates);
            _jobRepoMock.Setup(repo => repo.SetSyncJobProcessingState(It.IsAny<SyncProcessingState>())).Returns<SyncProcessingState>(x => x);
            _jobRepoMock.Setup(repo => repo.SetLastSuccessfulSyncDates(It.IsAny<SyncDates>())).Returns<SyncDates>(x => x);
            _jobRepoMock.Setup(repo => repo.SaveHubSpotApiDailyRequestCount(It.IsAny<int>(), It.IsAny<DateTime>())).Returns(true);
            _jobRepoMock.Setup(repo => repo.SaveSyncActivity(It.IsAny<ISyncActivity>())).Returns(true);
            _dataPrepMock.Setup(prep => prep.Prep(It.IsAny<IList<NewlyRegisteredMpContactDto>>())).Returns(new BulkContact[0]);
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
            _hubSpotSyncerMock.Verify(syncer => syncer.SerialSync(It.IsAny<SerialContact[]>()), Times.Once);
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

        /*
        public async Task Sync()
        {
            ISyncActivity syncJob = new SyncActivity(_clockMock.UtcNow);
            var syncState = default(SyncProcessingState);

            _loggerMock.LogInformation("Starting MP to HubSpot one-way sync operations (create new registrations first, followed by 2 update operations).");
            try
            {
                syncState = _configSvcMock.GetCurrentJobProcessingState();
                if (syncState == SyncProcessingState.Processing)
                {
                    _loggerMock.LogWarning("Job is already currently processing.");
                    return;
                }

                // set job processing state; get last successful sync dates
                syncState = _jobRepoMock.SetSyncJobProcessingState(SyncProcessingState.Processing);
                var syncDates = syncJob.PreviousSyncDates = _configSvcMock.GetLastSuccessfulSyncDates();
                var ageGradeDeltaLog = default(ChildAgeAndGradeDeltaLogDto);

                Util.TryCatchSwallow(() =>
                { // running this in advance of the create process with the express purpose of avoiding create/update race conditions when we consume the results for update
                    ageGradeDeltaLog = _mpContactRepoMock.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas();
                    syncDates.AgeAndGradeProcessDate = ageGradeDeltaLog.ProcessedUtc;
                    syncDates.AgeAndGradeSyncDate = ageGradeDeltaLog.SyncCompletedUtc ?? default(DateTime); // go ahead and set in case there's nothing to do
                    _jobRepoMock.SetLastSuccessfulSyncDates(syncDates);
                });

                Util.TryCatchSwallow(() =>
                { // create contacts
                    syncJob.NewRegistrationOperation = Create(syncJob.PreviousSyncDates.RegistrationSyncDate);
                    if (_syncActivityValidatorMock.Validate(syncJob, ruleSet: RuleSetName.Registration).IsValid)
                    {
                        syncDates.RegistrationSyncDate = syncJob.NewRegistrationOperation.Execution.StartUtc;
                        _jobRepoMock.SetLastSuccessfulSyncDates(syncDates);
                    }
                });

                Util.TryCatchSwallow(() =>
                { // update core contact properties
                    syncJob.CoreUpdateOperation = Update(syncJob.PreviousSyncDates.CoreUpdateSyncDate);
                    if (_syncActivityValidatorMock.Validate(syncJob, ruleSet: RuleSetName.CoreUpdate).IsValid)
                    {
                        syncDates.CoreUpdateSyncDate = syncJob.CoreUpdateOperation.Execution.StartUtc;
                        _jobRepoMock.SetLastSuccessfulSyncDates(syncDates);
                    }
                });

                // sync age and grade data to hubspot contacts
                syncJob.ChildAgeAndGradeUpdateOperation = UpdateChildAgeAndGradeData(ageGradeDeltaLog, syncJob.PreviousSyncDates.AgeAndGradeSyncDate);
                if (_syncActivityValidatorMock.Validate(syncJob, ruleSet: RuleSetName.AgeGradeUpdate).IsValid)
                {
                    ageGradeDeltaLog.SyncCompletedUtc = syncDates.AgeAndGradeSyncDate = _mpContactRepoMock.SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate();
                    _jobRepoMock.SetLastSuccessfulSyncDates(syncDates);
                }

                // reset sync job processing state
                syncState = _jobRepoMock.SetSyncJobProcessingState(SyncProcessingState.Idle);
            }
            catch (Exception exc)
            {
                _loggerMock.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contacts to HubSpot.");
                syncState = _jobRepoMock.SetSyncJobProcessingState(SyncProcessingState.Idle);
                throw;
            }
            finally // *** ALWAYS *** capture the activity, even if the job is already processing or an exception occurs
            {
                syncJob.SyncProcessingState = syncState;
                syncJob.Execution.FinishUtc = _clockMock.UtcNow;
                _syncActivityCleanerMock.CleanUp(syncJob);
                _jobRepoMock.SaveSyncActivity(syncJob);
                _loggerMock.LogInformation("Exiting...");
            }
        }

        private ISyncActivityNewRegistrationOperation Create(DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityNewRegistrationOperation(_clockMock.UtcNow) { PreviousSyncDate = lastSuccessfulSyncDate };

            try
            {
                _loggerMock.LogInformation("Starting new MP registrations to HubSpot one-way sync operation...");
                var newContacts = _mpContactRepoMock.GetNewlyRegisteredContacts(lastSuccessfulSyncDate); // talk to MP
                activity.BulkCreateSyncResult = _hubSpotSyncerMock.BulkSync(_dataPrepMock.Prep(newContacts));
                activity.SerialCreateSyncResult = _hubSpotSyncerMock.SerialSync(_dataPrepMock.ToSerial(activity.BulkCreateSyncResult.FailedBatches));
                return activity;
            }
            catch (Exception exc)
            {
                _loggerMock.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing new MP contacts to HubSpot.");
                throw;
            }
            finally // *** ALWAYS *** capture the HubSpot API request count, even if an exception occurs
            {
                activity.Execution.FinishUtc = _clockMock.UtcNow;
                _jobRepoMock.SaveHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }

        private ISyncActivityCoreUpdateOperation Update(DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityCoreUpdateOperation(_clockMock.UtcNow) { PreviousSyncDate = lastSuccessfulSyncDate };

            try
            {
                _loggerMock.LogInformation("Starting MP contact core updates to HubSpot one-way sync operation...");
                var updates = _dataPrepMock.Prep(_mpContactRepoMock.GetAuditedContactUpdates(lastSuccessfulSyncDate));

                // try both email changed and core updates for any contacts that do not yet exist in HubSpot
                activity.SerialUpdateResult = _hubSpotSyncerMock.SerialSync(updates);
                activity.RetryEmailExistsAsSerialUpdateResult = _hubSpotSyncerMock.SerialSync(activity.SerialUpdateResult.EmailAddressesAlreadyExist.ToArray());

                return activity;
            }
            catch (Exception exc)
            {
                _loggerMock.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contact core updates to HubSpot.");
                throw;
            }
            finally // *** ALWAYS *** capture the HubSpot API request count, even if an exception occurs
            {
                activity.Execution.FinishUtc = _clockMock.UtcNow;
                _jobRepoMock.SaveHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }

        private ISyncActivityChildAgeAndGradeUpdateOperation UpdateChildAgeAndGradeData(ChildAgeAndGradeDeltaLogDto deltaResult, DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityChildAgeAndGradeUpdateOperation(_clockMock.UtcNow)
            {
                PreviousSyncDate = lastSuccessfulSyncDate,
                AgeAndGradeDelta = deltaResult
            };

            try
            {
                _loggerMock.LogInformation("Starting MP contact age/grade count updates to HubSpot one-way sync operation...");
                activity.BulkUpdateSyncResult100 = _hubSpotSyncerMock.BulkSync(_dataPrepMock.Prep(_mpContactRepoMock.GetAgeAndGradeGroupDataForContacts()), batchSize: 100);
                activity.BulkUpdateSyncResult10 = _hubSpotSyncerMock.BulkSync(_dataPrepMock.ToBulk(activity.BulkUpdateSyncResult100.FailedBatches), batchSize: 10);
                activity.RetryBulkUpdateAsSerialUpdateResult = _hubSpotSyncerMock.SerialSync(_dataPrepMock.ToSerial(activity.BulkUpdateSyncResult10.FailedBatches));
                return activity;
            }
            catch (Exception exc)
            {
                _loggerMock.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contact age & grade updates to HubSpot.");
                throw;
            }
            finally // *** ALWAYS *** capture the HubSpot API request count, even if an exception occurs
            {
                activity.Execution.FinishUtc = _clockMock.UtcNow;
                _jobRepoMock.SaveHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }
        */
    }
}
