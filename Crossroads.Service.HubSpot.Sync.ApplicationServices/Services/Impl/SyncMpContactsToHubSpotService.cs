using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class SyncMpContactsToHubSpotService : ISyncMpContactsToHubSpotService
    {
        private readonly IMinistryPlatformContactRepository _ministryPlatformContactRepository;
        private readonly ICreateOrUpdateContactsInHubSpot _hubSpotContactSyncer;
        private readonly IClock _clock;
        private readonly IConfigurationService _configurationService;
        private readonly IJobRepository _jobRepository;
        private readonly IPrepareDataForHubSpot _dataPrep;
        private readonly IValidator<ISyncActivity> _syncActivityValidator;
        private readonly ICleanUpSyncActivity _syncActivityCleaner;
        private readonly ILogger<SyncMpContactsToHubSpotService> _logger;

        public SyncMpContactsToHubSpotService(
            IMinistryPlatformContactRepository ministryPlatformContactRepository,
            ICreateOrUpdateContactsInHubSpot hubSpotContactCreatorUpdater,
            IClock clock,
            IConfigurationService configurationService,
            IJobRepository jobRepository,
            IPrepareDataForHubSpot dataPrep,
            IValidator<ISyncActivity> syncActivityValidator,
            ICleanUpSyncActivity syncActivityCleaner,
            ILogger<SyncMpContactsToHubSpotService> logger)
        {
            _ministryPlatformContactRepository = ministryPlatformContactRepository ?? throw new ArgumentNullException(nameof(ministryPlatformContactRepository));
            _hubSpotContactSyncer = hubSpotContactCreatorUpdater ?? throw new ArgumentNullException(nameof(hubSpotContactCreatorUpdater));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _dataPrep = dataPrep ?? throw new ArgumentNullException(nameof(dataPrep));
            _syncActivityValidator = syncActivityValidator ?? throw new ArgumentNullException(nameof(syncActivityValidator));
            _syncActivityCleaner = syncActivityCleaner ?? throw new ArgumentNullException(nameof(syncActivityCleaner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ISyncActivity> Sync()
        {
            ISyncActivity syncJob = new SyncActivity(_clock.UtcNow);
            var syncState = default(SyncProcessingState);

            _logger.LogInformation("Starting MP to HubSpot one-way sync operations (create new registrations first, followed by 2 update operations).");
            try
            {
                syncState = _configurationService.GetCurrentJobProcessingState();
                if (syncState == SyncProcessingState.Processing)
                {
                    _logger.LogWarning("Job is already currently processing.");
                    return syncJob;
                }

                // set job processing state; get last successful sync dates
                syncState = _jobRepository.SetSyncJobProcessingState(SyncProcessingState.Processing);
                var syncDates = syncJob.PreviousSyncDates = _configurationService.GetLastSuccessfulSyncDates();
                var ageGradeDeltaLog = default(ChildAgeAndGradeDeltaLogDto);

                Util.TryCatchSwallow(() => { // running this in advance of the create process with the express purpose of avoiding create/update race conditions when we consume the results for update
                    ageGradeDeltaLog = _ministryPlatformContactRepository.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas();
                    syncDates.AgeAndGradeProcessDate = ageGradeDeltaLog.ProcessedUtc;
                    syncDates.AgeAndGradeSyncDate = ageGradeDeltaLog.SyncCompletedUtc ?? default(DateTime); // go ahead and set in case there's nothing to do
                    _jobRepository.SetLastSuccessfulSyncDates(syncDates);
                }, () => _logger.LogWarning("Age/grade calculation and persistence operation aborted."));
                
                Util.TryCatchSwallow(() => { // create contacts
                    syncJob.NewRegistrationOperation = Create(syncJob.PreviousSyncDates.RegistrationSyncDate);
                    if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.Registration).IsValid)
                    {
                        syncDates.RegistrationSyncDate = syncJob.NewRegistrationOperation.Execution.StartUtc;
                        _jobRepository.SetLastSuccessfulSyncDates(syncDates);
                    }
                }, () => _logger.LogWarning("Sync new registrations operation aborted."));

                Util.TryCatchSwallow(() => { // update core contact properties
                    syncJob.CoreUpdateOperation = Update(syncJob.PreviousSyncDates.CoreUpdateSyncDate);
                    if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.CoreUpdate).IsValid)
                    {
                        syncDates.CoreUpdateSyncDate = syncJob.CoreUpdateOperation.Execution.StartUtc;
                        _jobRepository.SetLastSuccessfulSyncDates(syncDates);
                    }
                }, () => _logger.LogWarning("Sync core updates operation aborted."));

                // sync age and grade data to hubspot contacts
                syncJob.ChildAgeAndGradeUpdateOperation = UpdateChildAgeAndGradeData(ageGradeDeltaLog, syncJob.PreviousSyncDates.AgeAndGradeSyncDate);
                if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.AgeGradeUpdate).IsValid)
                {
                    ageGradeDeltaLog.SyncCompletedUtc = syncDates.AgeAndGradeSyncDate = _ministryPlatformContactRepository.SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate();
                    _jobRepository.SetLastSuccessfulSyncDates(syncDates);
                }

                // reset sync job processing state
                syncState = _jobRepository.SetSyncJobProcessingState(SyncProcessingState.Idle);
                return syncJob;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contacts to HubSpot.");
                syncState = _jobRepository.SetSyncJobProcessingState(SyncProcessingState.Idle);
                throw;
            }
            finally // *** ALWAYS *** capture the activity, even if the job is already processing or an exception occurs
            {
                syncJob.SyncProcessingState = syncState;
                syncJob.Execution.FinishUtc = _clock.UtcNow;
                _syncActivityCleaner.CleanUp(syncJob);
                _jobRepository.SaveSyncActivity(syncJob);
                _logger.LogInformation("Exiting...");
            }
        }

        private ISyncActivityNewRegistrationOperation Create(DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityNewRegistrationOperation(_clock.UtcNow) {PreviousSyncDate = lastSuccessfulSyncDate};

            try
            {
                _logger.LogInformation("Starting new MP registrations to HubSpot one-way sync operation...");
                var newContacts = _ministryPlatformContactRepository.GetNewlyRegisteredContacts(lastSuccessfulSyncDate); // talk to MP
                activity.BulkCreateSyncResult = _hubSpotContactSyncer.BulkSync(_dataPrep.Prep(newContacts));
                activity.SerialCreateSyncResult = _hubSpotContactSyncer.SerialSync(_dataPrep.ToSerial(activity.BulkCreateSyncResult.FailedBatches));
                return activity;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing new MP contacts to HubSpot.");
                throw;
            }
            finally // *** ALWAYS *** capture the HubSpot API request count, even if an exception occurs
            {
                activity.Execution.FinishUtc = _clock.UtcNow;
                _jobRepository.SaveHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }

        private ISyncActivityCoreUpdateOperation Update(DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityCoreUpdateOperation(_clock.UtcNow) { PreviousSyncDate = lastSuccessfulSyncDate };

            try
            {
                _logger.LogInformation("Starting MP contact core updates to HubSpot one-way sync operation...");
                var updates = _dataPrep.Prep(_ministryPlatformContactRepository.GetAuditedContactUpdates(lastSuccessfulSyncDate));

                // try both email changed and core updates for any contacts that do not yet exist in HubSpot
                activity.SerialUpdateResult = _hubSpotContactSyncer.SerialSync(updates);
                activity.RetryEmailExistsAsSerialUpdateResult = _hubSpotContactSyncer.SerialSync(activity.SerialUpdateResult.EmailAddressesAlreadyExist.ToArray());

                return activity;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contact core updates to HubSpot.");
                throw;
            }
            finally // *** ALWAYS *** capture the HubSpot API request count, even if an exception occurs
            {
                activity.Execution.FinishUtc = _clock.UtcNow;
                _jobRepository.SaveHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }

        private ISyncActivityChildAgeAndGradeUpdateOperation UpdateChildAgeAndGradeData(ChildAgeAndGradeDeltaLogDto deltaResult, DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityChildAgeAndGradeUpdateOperation(_clock.UtcNow)
            {
                PreviousSyncDate = lastSuccessfulSyncDate,
                AgeAndGradeDelta = deltaResult
            };

            try
            {
                _logger.LogInformation("Starting MP contact age/grade count updates to HubSpot one-way sync operation...");
                activity.BulkUpdateSyncResult100 = _hubSpotContactSyncer.BulkSync(_dataPrep.Prep(_ministryPlatformContactRepository.GetAgeAndGradeGroupDataForContacts()), batchSize: 100);
                activity.BulkUpdateSyncResult10 = _hubSpotContactSyncer.BulkSync(_dataPrep.ToBulk(activity.BulkUpdateSyncResult100.FailedBatches), batchSize: 10);
                activity.RetryBulkUpdateAsSerialUpdateResult = _hubSpotContactSyncer.SerialSync(_dataPrep.ToSerial(activity.BulkUpdateSyncResult10.FailedBatches));
                return activity;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contact age & grade updates to HubSpot.");
                throw;
            }
            finally // *** ALWAYS *** capture the HubSpot API request count, even if an exception occurs
            {
                activity.Execution.FinishUtc = _clock.UtcNow;
                _jobRepository.SaveHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }
    }
}
