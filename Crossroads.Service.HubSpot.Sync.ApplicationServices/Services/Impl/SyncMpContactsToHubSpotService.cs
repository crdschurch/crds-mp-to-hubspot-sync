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
        private readonly ICreateOrUpdateContactsInHubSpot _contactSyncer;
        private readonly IClock _clock;
        private readonly IConfigurationService _configurationService;
        private readonly IJobRepository _jobRepository;
        private readonly IPrepareMpDataForHubSpot _dataPrep;
        private readonly IValidator<ISyncActivity> _syncActivityValidator;
        private readonly ICleanUpSyncActivity _syncActivityCleaner;
        private readonly ILogger<SyncMpContactsToHubSpotService> _logger;

        public SyncMpContactsToHubSpotService(
            IMinistryPlatformContactRepository ministryPlatformContactRepository,
            ICreateOrUpdateContactsInHubSpot hubSpotContactCreatorUpdater,
            IClock clock,
            IConfigurationService configurationService,
            IJobRepository jobRepository,
            IPrepareMpDataForHubSpot dataPrep,
            IValidator<ISyncActivity> syncActivityValidator,
            ICleanUpSyncActivity syncActivityCleaner,
            ILogger<SyncMpContactsToHubSpotService> logger)
        {
            _ministryPlatformContactRepository = ministryPlatformContactRepository ?? throw new ArgumentNullException(nameof(ministryPlatformContactRepository));
            _contactSyncer = hubSpotContactCreatorUpdater ?? throw new ArgumentNullException(nameof(hubSpotContactCreatorUpdater));
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
            var syncProgress = default(SyncProgress);

            _logger.LogInformation("Starting MP to HubSpot one-way sync operations (create new registrations first, followed by 2 update operations).");
            try
            {
                syncProgress = _configurationService.GetCurrentSyncProgress();
                if (syncProgress.SyncState == SyncState.Processing)
                {
                    _logger.LogWarning("Job is already currently processing.");
                    return syncJob;
                }

                // set job processing state; get last successful sync dates
                syncProgress = new SyncProgress {SyncState = SyncState.Processing};
                _jobRepository.SetSyncProgress(syncProgress);
                var syncDates = syncJob.PreviousSyncDates = _configurationService.GetLastSuccessfulSyncDates();
                var ageGradeDeltaLog = default(ChildAgeAndGradeDeltaLogDto);

                Util.TryCatchSwallow(() => { // running this in advance of the create process with the express purpose of avoiding create/update race conditions when we consume the results for update
                    PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataCalculationInMp, SyncStepState.Processing);
                    ageGradeDeltaLog = _ministryPlatformContactRepository.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas();
                    PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataSync, ageGradeDeltaLog.InsertCount + ageGradeDeltaLog.UpdateCount);
                    syncDates.AgeAndGradeProcessDate = ageGradeDeltaLog.ProcessedUtc;
                    syncDates.AgeAndGradeSyncDate = ageGradeDeltaLog.SyncCompletedUtc ?? default(DateTime); // go ahead and set in case there's nothing to do
                    _jobRepository.PersistLastSuccessfulSyncDates(syncDates);
                    PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataCalculationInMp, SyncStepState.Completed);
                }, () => {
                    _logger.LogWarning("Age/grade calculation and persistence operation aborted.");
                    PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataCalculationInMp, SyncStepState.Aborted);
                });

                Util.TryCatchSwallow(() => { // sync newly registered contacts to hubspot
                    PersistSyncProgress(syncProgress, SyncStepName.NewContactRegistrationSync, SyncStepState.Processing);
                    syncJob.NewRegistrationOperation = SyncNewRegistrations(syncJob.PreviousSyncDates.RegistrationSyncDate, syncProgress);
                    if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.Registration).IsValid)
                    {
                        syncDates.RegistrationSyncDate = syncJob.NewRegistrationOperation.Execution.StartUtc;
                        _jobRepository.PersistLastSuccessfulSyncDates(syncDates);
                        PersistSyncProgress(syncProgress, SyncStepName.NewContactRegistrationSync, SyncStepState.Completed);
                    }
                    else
                        PersistSyncProgress(syncProgress, SyncStepName.NewContactRegistrationSync, SyncStepState.CompletedButWithIssues);
                }, () => {
                    _logger.LogWarning("Sync new registrations operation aborted.");
                    PersistSyncProgress(syncProgress, SyncStepName.NewContactRegistrationSync, SyncStepState.Aborted);
                });

                Util.TryCatchSwallow(() => { // sync core contact property updates to hubspot
                    PersistSyncProgress(syncProgress, SyncStepName.CoreContactAttributeUpdateSync, SyncStepState.Processing);
                    syncJob.CoreUpdateOperation = SyncCoreUpdates(syncJob.PreviousSyncDates.CoreUpdateSyncDate, syncProgress);
                    if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.CoreUpdate).IsValid)
                    {
                        syncDates.CoreUpdateSyncDate = syncJob.CoreUpdateOperation.Execution.StartUtc;
                        _jobRepository.PersistLastSuccessfulSyncDates(syncDates);
                        PersistSyncProgress(syncProgress, SyncStepName.CoreContactAttributeUpdateSync, SyncStepState.Completed);
                    }
                    else
                        PersistSyncProgress(syncProgress, SyncStepName.CoreContactAttributeUpdateSync, SyncStepState.CompletedButWithIssues);
                }, () => {
                    _logger.LogWarning("Sync core updates operation aborted.");
                    PersistSyncProgress(syncProgress, SyncStepName.CoreContactAttributeUpdateSync, SyncStepState.Aborted);
                });

                Util.TryCatchSwallow(() => { // sync age/grade data to hubspot contacts
                    PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataSync, SyncStepState.Processing);
                    syncJob.ChildAgeAndGradeUpdateOperation = SyncChildAgeAndGradeData(ageGradeDeltaLog, syncJob.PreviousSyncDates.AgeAndGradeSyncDate);
                    if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.AgeGradeUpdate).IsValid)
                    {
                        if(ageGradeDeltaLog.InsertCount > 0 || ageGradeDeltaLog.UpdateCount > 0)
                            ageGradeDeltaLog.SyncCompletedUtc = syncDates.AgeAndGradeSyncDate = _ministryPlatformContactRepository.SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate();
                        _jobRepository.PersistLastSuccessfulSyncDates(syncDates);
                        PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataSync, SyncStepState.Completed);
                    }
                    else
                        PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataSync, SyncStepState.CompletedButWithIssues);
                }, () => {
                    _logger.LogWarning("Sync age/grade data operation aborted.");
                    PersistSyncProgress(syncProgress, SyncStepName.AgeGradeDataSync, SyncStepState.Aborted);
                });

                // reset sync job state
                PersistSyncProgress(syncProgress, SyncState.Idle);

                return syncJob;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contacts to HubSpot.");
                PersistSyncProgress(syncProgress, SyncState.Idle);
                throw;
            }
            finally // *** ALWAYS *** capture the activity, even if the job is already processing or an exception occurs
            {
                syncJob.SyncProgress = syncProgress;
                syncJob.Execution.FinishUtc = _clock.UtcNow;
                _syncActivityCleaner.CleanUp(syncJob);
                if(_configurationService.PersistActivity())
                    _jobRepository.SaveSyncActivity(syncJob);
                _logger.LogInformation("Exiting...");
            }
        }

        /// <summary>
        /// Handles create, update and reconciliation scenarios for MP CRM contacts identified as new registrants that ought to exist
        /// in the HubSpot CRM.
        /// </summary>
        private ISyncActivityOperation SyncNewRegistrations(DateTime lastSuccessfulSyncDate, SyncProgress syncProgress)
        {
            var activity = new SyncActivityOperation(_clock.UtcNow) {PreviousSyncDate = lastSuccessfulSyncDate};

            try
            {
                _logger.LogInformation("Starting new MP registrations to HubSpot one-way sync operation...");
                var newContacts = _ministryPlatformContactRepository.GetNewlyRegisteredContacts(lastSuccessfulSyncDate); // talk to MP
                PersistSyncProgress(syncProgress, SyncStepName.NewContactRegistrationSync, newContacts.Count);
                activity.SerialCreateResult = _contactSyncer.SerialCreate(_dataPrep.Prep(newContacts)); // create in HubSpot
                activity.SerialUpdateResult = _contactSyncer.SerialUpdate(activity.SerialCreateResult.EmailAddressesAlreadyExist.ToArray()); // update in HubSpot
                activity.SerialReconciliationResult = _contactSyncer.ReconcileConflicts(activity.SerialUpdateResult.EmailAddressesAlreadyExist.ToArray());

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

        /// <summary>
        /// Capable of accommodating email address (unique identifier in HubSpot) change and other core contact attribute/property updates.
        /// Can also create a contact if it does not exist.
        /// </summary>
        private ISyncActivityOperation SyncCoreUpdates(DateTime lastSuccessfulSyncDate, SyncProgress syncProgress)
        {
            var activity = new SyncActivityOperation(_clock.UtcNow) { PreviousSyncDate = lastSuccessfulSyncDate };

            try
            {
                _logger.LogInformation("Starting MP contact core updates to HubSpot one-way sync operation...");
                var updates = _dataPrep.Prep(_ministryPlatformContactRepository.GetAuditedContactUpdates(lastSuccessfulSyncDate));
                PersistSyncProgress(syncProgress, SyncStepName.CoreContactAttributeUpdateSync, updates.Length);
                activity.SerialUpdateResult = _contactSyncer.SerialUpdate(updates);
                activity.SerialCreateResult = _contactSyncer.SerialCreate(activity.SerialUpdateResult.EmailAddressesDoNotExist.ToArray());
                activity.SerialReconciliationResult = _contactSyncer.ReconcileConflicts(activity.SerialUpdateResult.EmailAddressesAlreadyExist.ToArray());

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

        private ISyncActivityChildAgeAndGradeUpdateOperation SyncChildAgeAndGradeData(ChildAgeAndGradeDeltaLogDto deltaResult, DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityChildAgeAndGradeUpdateOperation(_clock.UtcNow)
            {
                PreviousSyncDate = lastSuccessfulSyncDate,
                AgeAndGradeDelta = deltaResult
            };

            try
            {
                _logger.LogInformation("Starting MP contact age/grade count updates to HubSpot one-way sync operation...");
                activity.BulkUpdateSyncResult1000 = _contactSyncer.BulkSync(_dataPrep.Prep(_ministryPlatformContactRepository.GetAgeAndGradeGroupDataForContacts()), batchSize: 1000);
                activity.BulkUpdateSyncResult100 = _contactSyncer.BulkSync(_dataPrep.ToBulk(activity.BulkUpdateSyncResult1000.FailedBatches), batchSize: 100);
                activity.BulkUpdateSyncResult10 = _contactSyncer.BulkSync(_dataPrep.ToBulk(activity.BulkUpdateSyncResult100.FailedBatches), batchSize: 10);
                activity.RetryBulkUpdateAsSerialUpdateResult = _contactSyncer.SerialUpdate(_dataPrep.ToSerial(activity.BulkUpdateSyncResult10.FailedBatches));
                activity.SerialCreateResult = _contactSyncer.SerialCreate(activity.RetryBulkUpdateAsSerialUpdateResult.EmailAddressesDoNotExist.ToArray()); // in the event they don't yet exist but won't be picked up any other way b/c their kiddo count is the only thing to change

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

        private void PersistSyncProgress(SyncProgress syncProgress, SyncStepName stepName, SyncStepState stepState)
        {
            syncProgress.Steps[stepName].StepState = stepState;
            _jobRepository.SetSyncProgress(syncProgress);
        }

        private void PersistSyncProgress(SyncProgress syncProgress, SyncStepName stepName, int numberOfContactsToSync)
        {
            syncProgress.Steps[stepName].NumberOfContactsToSync = numberOfContactsToSync;
            _jobRepository.SetSyncProgress(syncProgress);
        }

        private void PersistSyncProgress(SyncProgress syncProgress, SyncState state)
        {
            syncProgress.SyncState = state;
            _jobRepository.SetSyncProgress(syncProgress);
        }
    }
}
