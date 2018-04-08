using System;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Extensions;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class SyncNewMpRegistrationsToHubSpot : ISyncNewMpRegistrationsToHubSpot
    {
        private readonly IMinistryPlatformContactRepository _ministryPlatformContactRepository;
        private readonly IMapper _mapper;
        private readonly ICreateOrUpdateContactsInHubSpot _hubSpotContactCreatorUpdater;
        private readonly IClock _clock;
        private readonly IConfigurationService _configurationService;
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<SyncNewMpRegistrationsToHubSpot> _logger;

        public SyncNewMpRegistrationsToHubSpot(
            IMinistryPlatformContactRepository ministryPlatformContactRepository,
            IMapper mapper,
            ICreateOrUpdateContactsInHubSpot hubSpotContactCreatorUpdater,
            IClock clock,
            IConfigurationService configurationService,
            IJobRepository jobRepository,
            ILogger<SyncNewMpRegistrationsToHubSpot> logger)
        {
            _ministryPlatformContactRepository = ministryPlatformContactRepository ?? throw new ArgumentNullException(nameof(ministryPlatformContactRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _hubSpotContactCreatorUpdater = hubSpotContactCreatorUpdater ?? throw new ArgumentNullException(nameof(hubSpotContactCreatorUpdater));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActivityResult Execute()
        {
            var activity = new NewContactActivityResult(_clock.UtcNow);
            JobProcessingState jobState = default(JobProcessingState);

            try
            {
                _logger.LogInformation("Starting new MP registrations to HubSpot one-way sync operation...");
                jobState = _configurationService.GetCurrentJobProcessingState();
                if (jobState == JobProcessingState.Processing)
                {
                    _logger.LogWarning("Job is already currently processing. Exiting...");
                    return activity;
                }

                jobState = _jobRepository.SetJobProcessingState(JobProcessingState.Processing);
                activity.LastSuccessfulSyncDate = _configurationService.GetLastSuccessfulSyncDate();
                var newContacts = _ministryPlatformContactRepository.GetNewlyRegisteredContacts(activity.LastSuccessfulSyncDate); // talk to MP

                _logger.LogInformation("Creating newly registered MP contacts in HubSpot...");
                var firstRun = _hubSpotContactCreatorUpdater.BulkCreateOrUpdate(_mapper.Map<BulkContact[]>(newContacts));
                activity.TotalContacts = firstRun.TotalContacts;
                activity.BulkRunResults.Add(firstRun);
                _jobRepository.SaveHubSpotDailyRequestCount(firstRun.BatchCount, activity.Execution.StartUtc);

                var continueProcessing = DetermineNextStepsAfter1stRun(firstRun, activity);
                if (continueProcessing == false)
                {
                    jobState = MarkSuccessfulExecutionComplete(activity);
                    return activity;
                }

                // mixed bag -- keep going, try to pare it down more
                _logger.LogWarning($"{firstRun.SuccessCount} succeeded and {firstRun.FailureCount} failed out of {firstRun.TotalContacts} contacts.");
                var secondRun = _hubSpotContactCreatorUpdater.BulkCreateOrUpdate(firstRun.GetContactsThatFailedToSync());
                activity.BulkRunResults.Add(secondRun);
                _jobRepository.SaveHubSpotDailyRequestCount(secondRun.BatchCount, activity.Execution.StartUtc);

                continueProcessing = DetermineNextStepsAfter2ndRun(secondRun, firstRun);
                if (continueProcessing == false)
                {
                    jobState = MarkSuccessfulExecutionComplete(activity);
                    return activity;
                }

                // we'll find a better way to accommodate the finicky API in refactor; another type + AutoMapper?
                activity.SerialRunResult = _hubSpotContactCreatorUpdater.SerialCreate(secondRun.GetContactsThatFailedToSync(_mapper));

                _jobRepository.SetLastSuccessfulSyncDate(activity.Execution.StartUtc);
                jobState = _jobRepository.SetJobProcessingState(JobProcessingState.Idle);

                return activity;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing newly registered MP contacts to HubSpot.");
                jobState = _jobRepository.SetJobProcessingState(JobProcessingState.Idle);
                throw;
            }
            finally // *** ALWAYS *** capture the activity, even if the job is already processing or an exception occurs
            {
                activity.JobProcessingState = jobState;
                activity.Execution.FinishUtc = _clock.UtcNow;
                _jobRepository.SaveActivityResult(activity);
                _logger.LogInformation("Exiting...");
            }
        }

        private JobProcessingState MarkSuccessfulExecutionComplete(NewContactActivityResult activity)
        {
            _jobRepository.SetLastSuccessfulSyncDate(activity.Execution.StartUtc);
            return _jobRepository.SetJobProcessingState(JobProcessingState.Idle);
        }

        private bool DetermineNextStepsAfter1stRun(BulkRunResult firstRun, NewContactActivityResult activity)
        {
            if (firstRun.TotalContacts == 0) // nothing to do on this run
            {
                _logger.LogInformation("0 newly registered contacts to sync to HubSpot on this run.");
                return false;
            }

            if (firstRun.SuccessCount == firstRun.TotalContacts) // everyone synced
            {
                _logger.LogInformation("100% success! Don't get used to it.");
                return false;
            }

            var onlyOneBatchFailed = firstRun.FailedBatches.Count == 1;
            if (onlyOneBatchFailed) // log
            {
                _logger.LogWarning("There are only enough contacts for 1 batch, which previously failed. Proceeding to serial processing...");
            }

            // everyone failed to sync; go straight to individual processing
            var allContactSyncAttemptsFailed = firstRun.FailureCount == firstRun.TotalContacts;
            if(allContactSyncAttemptsFailed) // log
            {
                _logger.LogWarning($"All {firstRun.TotalContacts} contacts failed to sync. Proceeding to serial processing...");
            }

            if (allContactSyncAttemptsFailed || onlyOneBatchFailed) // execute
            {
                activity.SerialRunResult = _hubSpotContactCreatorUpdater.SerialCreate(firstRun.GetContactsThatFailedToSync(_mapper));
                _jobRepository.SaveHubSpotDailyRequestCount(activity.SerialRunResult.TotalContacts, activity.Execution.StartUtc);
                return false;
            }

            return true;
        }

        private bool DetermineNextStepsAfter2ndRun(BulkRunResult secondRun, BulkRunResult firstRun)
        {
            if (secondRun.SuccessCount == secondRun.TotalContacts)
                return false;

            if (firstRun.FailureCount == secondRun.FailureCount) // let's try individual contact creation and then shut it down
                _logger.LogInformation("2nd pass at syncing new MP contacts to HubSpot failed at the same rate of the first run.");

            if (secondRun.FailureCount < firstRun.FailureCount) // still some failure
                _logger.LogInformation($"More success on the 2nd run. {secondRun.SuccessCount} out of {secondRun.TotalContacts} succeeded.");

            _logger.LogInformation("Resorting to serial syncing of new contacts.");

            return true;
        }
    }
}