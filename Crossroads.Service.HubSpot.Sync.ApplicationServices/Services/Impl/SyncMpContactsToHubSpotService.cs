using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum;
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
        private readonly IValidator<IActivity> _activityValidator;
        private readonly ICleanUpActivity _activityCleaner;
        private readonly ILogger<SyncMpContactsToHubSpotService> _logger;

        public SyncMpContactsToHubSpotService(
            IMinistryPlatformContactRepository ministryPlatformContactRepository,
            ICreateOrUpdateContactsInHubSpot hubSpotContactCreatorUpdater,
            IClock clock,
            IConfigurationService configurationService,
            IJobRepository jobRepository,
            IPrepareMpDataForHubSpot dataPrep,
            IValidator<IActivity> activityValidator,
            ICleanUpActivity activityCleaner,
            ILogger<SyncMpContactsToHubSpotService> logger)
        {
            _ministryPlatformContactRepository = ministryPlatformContactRepository ?? throw new ArgumentNullException(nameof(ministryPlatformContactRepository));
            _contactSyncer = hubSpotContactCreatorUpdater ?? throw new ArgumentNullException(nameof(hubSpotContactCreatorUpdater));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _dataPrep = dataPrep ?? throw new ArgumentNullException(nameof(dataPrep));
            _activityValidator = activityValidator ?? throw new ArgumentNullException(nameof(activityValidator));
            _activityCleaner = activityCleaner ?? throw new ArgumentNullException(nameof(activityCleaner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Activity> Sync()
        {
            var activity = new Activity(_clock.UtcNow);
            var activityProgress = default(ActivityProgress);

            _logger.LogInformation("Starting MP to HubSpot one-way sync operations (create new registrations first, followed by 2 update operations).");
            try
            {
                activityProgress = _configurationService.GetCurrentActivityProgress();
                if (activityProgress.ActivityState == ActivityState.Processing)
                {
                    _logger.LogWarning("Job is already currently processing.");
                    return activity;
                }

                // set job processing state; get last successful sync dates
                _jobRepository.PersistActivityProgress(activityProgress = new ActivityProgress { ActivityState = ActivityState.Processing });
                var operationDates = activity.PreviousOperationDates = _configurationService.GetLastSuccessfulOperationDates();

                var ageGradeDeltaLog = default(ChildAgeAndGradeDeltaLogDto);

                Util.TryCatchSwallow(() => { // running this in advance of the create process with the express purpose of avoiding create/update race conditions when we consume the results for update
                    PersistActivityProgress(activityProgress, OperationName.AgeGradeDataCalculationInMp, OperationState.Processing);
                    activity.ChildAgeAndGradeCalculationOperation = CalculateAndPersistChildAgeGradeDataByHousehold(activity.PreviousOperationDates);
                    ageGradeDeltaLog = activity.ChildAgeAndGradeCalculationOperation.AgeGradeDeltaLog;
                    var operationContactCount = ageGradeDeltaLog.InsertCount + ageGradeDeltaLog.UpdateCount;
                    var operationDuration = activity.ChildAgeAndGradeCalculationOperation.Execution.Duration;
                    PersistActivityProgress(activityProgress, OperationName.AgeGradeDataCalculationInMp, operationContactCount: operationContactCount, operationDuration: operationDuration);
                    operationDates.AgeAndGradeProcessDate = ageGradeDeltaLog.ProcessedUtc;
                    operationDates.AgeAndGradeSyncDate = ageGradeDeltaLog.SyncCompletedUtc ?? default(DateTime); // go ahead and set in case there's nothing to do
                    _jobRepository.PersistLastSuccessfulOperationDates(operationDates);
                    PersistActivityProgress(activityProgress, OperationName.AgeGradeDataCalculationInMp, OperationState.Completed);
                }, () => {
                    _logger.LogWarning("Age/grade calculation and persistence operation aborted.");
                    PersistActivityProgress(activityProgress, OperationName.AgeGradeDataCalculationInMp, OperationState.Aborted);
                });

                Util.TryCatchSwallow(() => { // sync newly registered contacts to hubspot
                    PersistActivityProgress(activityProgress, OperationName.NewContactRegistrationSync, OperationState.Processing);
                    activity.NewRegistrationSyncOperation = SyncNewRegistrations(activity.PreviousOperationDates.RegistrationSyncDate, activityProgress);
                    if (_activityValidator.Validate(activity, ruleSet: RuleSetName.NewRegistrationSync).IsValid)
                    {
                        operationDates.RegistrationSyncDate = activity.NewRegistrationSyncOperation.Execution.StartUtc;
                        _jobRepository.PersistLastSuccessfulOperationDates(operationDates);
                        PersistActivityProgress(activityProgress, OperationName.NewContactRegistrationSync, OperationState.Completed, operationDuration: activity.NewRegistrationSyncOperation.Execution.Duration);
                    }
                    else
                        PersistActivityProgress(activityProgress, OperationName.NewContactRegistrationSync, OperationState.CompletedButWithIssues, operationDuration: activity.NewRegistrationSyncOperation.Execution.Duration);
                }, () => {
                    _logger.LogWarning("Sync new registrations operation aborted.");
                    PersistActivityProgress(activityProgress, OperationName.NewContactRegistrationSync, OperationState.Aborted, operationDuration: activity.NewRegistrationSyncOperation.Execution.Duration);
                });

                Util.TryCatchSwallow(() => { // sync core contact property updates to hubspot
                    PersistActivityProgress(activityProgress, OperationName.CoreContactAttributeUpdateSync, OperationState.Processing);
                    activity.CoreContactAttributeSyncOperation = SyncCoreUpdates(activity.PreviousOperationDates.CoreUpdateSyncDate, activityProgress);
                    if (_activityValidator.Validate(activity, ruleSet: RuleSetName.CoreContactAttributeSync).IsValid)
                    {
                        operationDates.CoreUpdateSyncDate = activity.CoreContactAttributeSyncOperation.Execution.StartUtc;
                        _jobRepository.PersistLastSuccessfulOperationDates(operationDates);
                        PersistActivityProgress(activityProgress, OperationName.CoreContactAttributeUpdateSync, OperationState.Completed, operationDuration: activity.CoreContactAttributeSyncOperation.Execution.Duration);
                    }
                    else
                        PersistActivityProgress(activityProgress, OperationName.CoreContactAttributeUpdateSync, OperationState.CompletedButWithIssues, operationDuration: activity.CoreContactAttributeSyncOperation.Execution.Duration);
                }, () => {
                    _logger.LogWarning("Sync core updates operation aborted.");
                    PersistActivityProgress(activityProgress, OperationName.CoreContactAttributeUpdateSync, OperationState.Aborted, operationDuration: activity.CoreContactAttributeSyncOperation.Execution.Duration);
                });

                Util.TryCatchSwallow(() => { // sync age/grade data to hubspot contacts
                    PersistActivityProgress(activityProgress, OperationName.AgeGradeDataSync, OperationState.Processing);
                    activity.ChildAgeAndGradeSyncOperation = SyncChildAgeAndGradeData(ageGradeDeltaLog, activity.PreviousOperationDates.AgeAndGradeSyncDate);
                    if (_activityValidator.Validate(activity, ruleSet: RuleSetName.ChildAgeGradeSync).IsValid)
                    {
                        if(ageGradeDeltaLog.InsertCount > 0 || ageGradeDeltaLog.UpdateCount > 0)
                            ageGradeDeltaLog.SyncCompletedUtc = operationDates.AgeAndGradeSyncDate = _ministryPlatformContactRepository.SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate();
                        _jobRepository.PersistLastSuccessfulOperationDates(operationDates);
                        PersistActivityProgress(activityProgress, OperationName.AgeGradeDataSync, OperationState.Completed, activity.ChildAgeAndGradeSyncOperation.SuccessCount, activity.ChildAgeAndGradeSyncOperation.Execution.Duration);
                    }
                    else
                        PersistActivityProgress(activityProgress, OperationName.AgeGradeDataSync, OperationState.CompletedButWithIssues, operationDuration: activity.ChildAgeAndGradeSyncOperation.Execution.Duration);
                }, () => {
                    _logger.LogWarning("Sync age/grade data operation aborted.");
                    PersistActivityProgress(activityProgress, OperationName.AgeGradeDataSync, OperationState.Aborted, operationDuration: activity.ChildAgeAndGradeSyncOperation.Execution.Duration);
                });

                // reset sync job state
                PersistActivityProgress(activityProgress, ActivityState.Idle);

                return activity;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing MP contacts to HubSpot.");
                PersistActivityProgress(activityProgress, ActivityState.Idle);
                throw;
            }
            finally // *** ALWAYS *** capture the activity, even if the job is already processing or an exception occurs
            {
                activity.ActivityProgress = activityProgress;
                activity.Execution.FinishUtc = _clock.UtcNow;
                PersistActivityProgress(activityProgress, activityDuration: activity.Execution.Duration);
                _activityCleaner.CleanUp(activity);
                if(_configurationService.PersistActivity())
                    _jobRepository.PersistActivity(activity);
                _logger.LogInformation("Exiting...");
            }
        }

        private ActivityChildAgeAndGradeCalculationOperation CalculateAndPersistChildAgeGradeDataByHousehold(OperationDates previousOperationDates)
        {
            var activity = new ActivityChildAgeAndGradeCalculationOperation(_clock.UtcNow)
            {
                PreviousProcessDate = previousOperationDates.AgeAndGradeProcessDate,
                PreviousSyncDate = previousOperationDates.AgeAndGradeSyncDate
            };

            try
            {
                activity.AgeGradeDeltaLog = _ministryPlatformContactRepository.CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas();
                return activity;
            }
            catch(Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while calculating & persisting MP contact child age & grade data.");
                throw;
            }
            finally
            {
                activity.Execution.FinishUtc = _clock.UtcNow;
            }
        }

        /// <summary>
        /// Handles create, update and reconciliation scenarios for MP CRM contacts identified as new registrants that ought to exist
        /// in the HubSpot CRM.
        /// </summary>
        private ActivitySyncOperation SyncNewRegistrations(DateTime lastSuccessfulSyncDate, ActivityProgress activityProgress)
        {
            var activity = new ActivitySyncOperation(_clock.UtcNow) {PreviousSyncDate = lastSuccessfulSyncDate};

            try
            {
                _logger.LogInformation("Starting new MP registrations to HubSpot one-way sync operation...");
                var newContacts = _ministryPlatformContactRepository.GetNewlyRegisteredContacts(lastSuccessfulSyncDate); // talk to MP
                PersistActivityProgress(activityProgress, OperationName.NewContactRegistrationSync, operationContactCount: newContacts.Count);
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
                _jobRepository.PersistHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }

        /// <summary>
        /// Capable of accommodating email address (unique identifier in HubSpot) change and other core contact attribute/property updates.
        /// Can also create a contact if it does not exist.
        /// </summary>
        private ActivitySyncOperation SyncCoreUpdates(DateTime lastSuccessfulSyncDate, ActivityProgress activityProgress)
        {
            var activity = new ActivitySyncOperation(_clock.UtcNow) { PreviousSyncDate = lastSuccessfulSyncDate };

            try
            {
                _logger.LogInformation("Starting MP contact core updates to HubSpot one-way sync operation...");
                var updates = _dataPrep.Prep(_ministryPlatformContactRepository.GetAuditedContactUpdates(lastSuccessfulSyncDate));
                PersistActivityProgress(activityProgress, OperationName.CoreContactAttributeUpdateSync, operationContactCount: updates.Length);
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
                _jobRepository.PersistHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }

        private ActivityChildAgeAndGradeSyncOperation SyncChildAgeAndGradeData(ChildAgeAndGradeDeltaLogDto deltaResult, DateTime lastSuccessfulSyncDate)
        {
            var activity = new ActivityChildAgeAndGradeSyncOperation(_clock.UtcNow)
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
                _jobRepository.PersistHubSpotApiDailyRequestCount(activity.HubSpotApiRequestCount, activity.Execution.StartUtc);
            }
        }

        private void PersistActivityProgress(ActivityProgress activityProgress, OperationName operationName, OperationState? operationState = null, int? operationContactCount = null, string operationDuration = null)
        {
            activityProgress.Operations[operationName.ToString()].Duration = operationDuration ?? activityProgress.Operations[operationName.ToString()].Duration;
            activityProgress.Operations[operationName.ToString()].ContactCount = operationContactCount ?? activityProgress.Operations[operationName.ToString()].ContactCount;
            activityProgress.Operations[operationName.ToString()].OperationState = operationState ?? activityProgress.Operations[operationName.ToString()].OperationState;
            _jobRepository.PersistActivityProgress(activityProgress);
        }

        private void PersistActivityProgress(ActivityProgress activityProgress, ActivityState? activityState = null, string activityDuration = null)
        {
            activityProgress.ActivityState = activityState ?? activityProgress.ActivityState;
            activityProgress.Duration = activityDuration ?? activityProgress.Duration;
            _jobRepository.PersistActivityProgress(activityProgress);
        }
    }
}
