using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities.Guid;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Extensions;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class SyncMpContactsToHubSpotService : ISyncMpContactsToHubSpotService
    {
        private readonly IMinistryPlatformContactRepository _ministryPlatformContactRepository;
        private readonly IMapper _mapper;
        private readonly ICreateOrUpdateContactsInHubSpot _hubSpotContactCreatorUpdater;
        private readonly IClock _clock;
        private readonly IConfigurationService _configurationService;
        private readonly IJobRepository _jobRepository;
        private readonly IPrepareMpContactCoreUpdatesForHubSpot _coreUpdatePreparer;
        private readonly IValidator<ISyncActivity> _syncActivityValidator;
        private readonly IGenerateCombGuid _combGuidGenerator;
        private readonly ILogger<SyncMpContactsToHubSpotService> _logger;

        public SyncMpContactsToHubSpotService(
            IMinistryPlatformContactRepository ministryPlatformContactRepository,
            IMapper mapper,
            ICreateOrUpdateContactsInHubSpot hubSpotContactCreatorUpdater,
            IClock clock,
            IConfigurationService configurationService,
            IJobRepository jobRepository,
            IPrepareMpContactCoreUpdatesForHubSpot coreUpdatePreparer,
            IValidator<ISyncActivity> syncActivityValidator,
            IGenerateCombGuid combGuidGenerator,
            ILogger<SyncMpContactsToHubSpotService> logger)
        {
            _ministryPlatformContactRepository = ministryPlatformContactRepository ?? throw new ArgumentNullException(nameof(ministryPlatformContactRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _hubSpotContactCreatorUpdater = hubSpotContactCreatorUpdater ?? throw new ArgumentNullException(nameof(hubSpotContactCreatorUpdater));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _coreUpdatePreparer = coreUpdatePreparer ?? throw new ArgumentNullException(nameof(coreUpdatePreparer));
            _syncActivityValidator = syncActivityValidator ?? throw new ArgumentNullException(nameof(syncActivityValidator));
            _combGuidGenerator = combGuidGenerator ?? throw new ArgumentNullException(nameof(combGuidGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Sync()
        {
            ISyncActivity syncJob = new SyncActivity(_combGuidGenerator.Generate(), _clock.UtcNow);
            var syncState = default(SyncProcessingState);

            _logger.LogInformation("Starting MP to HubSpot one-way sync operations (create first, then update).");
            try
            {
                syncState = _configurationService.GetCurrentJobProcessingState();
                if (syncState == SyncProcessingState.Processing)
                {
                    _logger.LogWarning("Job is already currently processing.");
                    return;
                }

                // set job processing state; get last successful sync dates
                syncState = _jobRepository.SetSyncJobProcessingState(SyncProcessingState.Processing);
                var syncDates = syncJob.PreviousSyncDates = _configurationService.GetLastSuccessfulSyncDates();

                try
                {
                    // create contacts
                    syncJob.NewRegistrationOperation = Create(syncJob.PreviousSyncDates.RegistrationSyncDate);
                    if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.Registration).IsValid)
                    {
                        syncDates.RegistrationSyncDate = syncJob.NewRegistrationOperation.Execution.StartUtc;
                        _jobRepository.SetLastSuccessfulSyncDates(syncDates);
                    }
                }
                catch { /* logging has already happened; suppressing so core update process can run */ }

                // update core contact properties
                syncJob.CoreUpdateOperation = Update(syncJob.PreviousSyncDates.CoreUpdateSyncDate);
                if (_syncActivityValidator.Validate(syncJob, ruleSet: RuleSetName.CoreUpdate).IsValid)
                {
                    syncDates.CoreUpdateSyncDate = syncJob.CoreUpdateOperation.Execution.StartUtc;
                    _jobRepository.SetLastSuccessfulSyncDates(syncDates);
                }

                // reset sync job processing state
                syncState = _jobRepository.SetSyncJobProcessingState(SyncProcessingState.Idle);
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
                activity.BulkCreateSyncResult = _hubSpotContactCreatorUpdater.BulkCreateOrUpdate(_mapper.Map<BulkContact[]>(newContacts));
                activity.SerialCreateSyncResult = _hubSpotContactCreatorUpdater.SerialCreate(activity.BulkCreateSyncResult.GetContactsThatFailedToSync(_mapper));
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
                var dto = _coreUpdatePreparer.Prepare(_ministryPlatformContactRepository.GetContactUpdates(lastSuccessfulSyncDate));

                // try email changed update operation and retry as create operation for any contacts that do not yet exist in HubSpot
                activity.EmailChangedSyncResult = _hubSpotContactCreatorUpdater.SerialUpdate(dto.EmailChangedContacts);
                activity.RetryEmailChangeAsCreateSyncResult = RetryWhenContactsDoNotYetExistInHubSpot(activity.EmailChangedSyncResult);

                // try core update change operation and retry as create operation for any contacts that do not yet exist in HubSpot
                activity.CoreUpdateSyncResult = _hubSpotContactCreatorUpdater.SerialUpdate(dto.CoreOnlyChangedContacts);
                activity.RetryCoreUpdateAsCreateSyncResult = RetryWhenContactsDoNotYetExistInHubSpot(activity.CoreUpdateSyncResult);
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

        private SerialCreateSyncResult<EmailAddressCreatedContact> RetryWhenContactsDoNotYetExistInHubSpot<T>(CoreUpdateResult<T> originalResult)
            where T : IUpdateContact
        {
            if (originalResult.ContactDoesNotExistCount > 0)
            {   // retries contacts that do not exist... as a create operation
                var retryResult = _hubSpotContactCreatorUpdater.SerialCreate(originalResult.ContactsThatDoNotExist.ToArray());
                originalResult.ContactsThatDoNotExist.Clear(); // blowing these away to save space in LiteDb when activity is persisted
                return retryResult;
            }

            return new SerialCreateSyncResult<EmailAddressCreatedContact>();
        }
    }
}
