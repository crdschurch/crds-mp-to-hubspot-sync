using System;
using System.Threading.Tasks;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities.Guid;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Extensions;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using Microsoft.Extensions.Logging;

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
        private readonly IGenerateCombGuid _combGuidGenerator;
        private readonly ILogger<SyncMpContactsToHubSpotService> _logger;

        public SyncMpContactsToHubSpotService(
            IMinistryPlatformContactRepository ministryPlatformContactRepository,
            IMapper mapper,
            ICreateOrUpdateContactsInHubSpot hubSpotContactCreatorUpdater,
            IClock clock,
            IConfigurationService configurationService,
            IJobRepository jobRepository,
            IGenerateCombGuid combGuidGenerator,
            ILogger<SyncMpContactsToHubSpotService> logger)
        {
            _ministryPlatformContactRepository = ministryPlatformContactRepository ?? throw new ArgumentNullException(nameof(ministryPlatformContactRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _hubSpotContactCreatorUpdater = hubSpotContactCreatorUpdater ?? throw new ArgumentNullException(nameof(hubSpotContactCreatorUpdater));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _combGuidGenerator = combGuidGenerator ?? throw new ArgumentNullException(nameof(combGuidGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ISyncActivity> Sync()
        {
            var utcNow = _clock.UtcNow;
            var syncJob = new SyncActivity(_combGuidGenerator.Generate(), utcNow);
            SyncProcessingState syncState = default(SyncProcessingState);

            _logger.LogInformation("Starting MP to HubSpot one-way sync operations (create first, then update).");
            try
            {
                syncState = _configurationService.GetCurrentJobProcessingState();
                if (syncState == SyncProcessingState.Processing)
                {
                    _logger.LogWarning("Job is already currently processing.");
                    return syncJob;
                }

                syncState = _jobRepository.SetSyncJobProcessingState(SyncProcessingState.Processing);
                syncJob.PreviousSyncDates = _configurationService.GetLastSuccessfulSyncDates();
                syncJob.CreateOperation = Create(syncJob.PreviousSyncDates.CreateSyncDate);
                syncJob.TotalContacts = syncJob.CreateOperation.TotalContacts;
                _jobRepository.SetLastSuccessfulSyncDate(new SyncDates { CreateSyncDate = syncJob.CreateOperation.Execution.StartUtc });
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
                _jobRepository.SaveSyncActivity(syncJob);
                _logger.LogInformation("Exiting...");
            }
        }

        public ISyncActivityOperation Create(DateTime lastSuccessfulSyncDate)
        {
            var activity = new SyncActivityOperation(_clock.UtcNow) {PreviousSyncDate = lastSuccessfulSyncDate};

            try
            {
                _logger.LogInformation("Starting new MP registrations to HubSpot one-way sync operation...");
                // convert this to be the invocation of a func for requesting MP data (passed in as an argument to this method)
                var newContacts = _ministryPlatformContactRepository.GetNewlyRegisteredContacts(lastSuccessfulSyncDate); // talk to MP

                _logger.LogInformation("Creating newly registered MP contacts in HubSpot...");
                activity.BulkSyncResult = _hubSpotContactCreatorUpdater.BulkCreateOrUpdate(_mapper.Map<BulkContact[]>(newContacts));
                activity.TotalContacts = activity.BulkSyncResult.TotalContacts;
                _jobRepository.SaveHubSpotApiDailyRequestCount(activity.BulkSyncResult.BatchCount, activity.Execution.StartUtc);

                if (activity.BulkSyncResult.TotalContacts == 0 || // either nothing to do *OR* all contacts were synced to HubSpot successfully
                    activity.BulkSyncResult.SuccessCount == activity.BulkSyncResult.TotalContacts)
                {
                    return activity;
                }

                // convert this to be the invocation of a func for serial create or update (passed in as an argument to this method)
                activity.SerialSyncResult = _hubSpotContactCreatorUpdater.SerialCreate(activity.BulkSyncResult.GetContactsThatFailedToSync(_mapper));
                _jobRepository.SaveHubSpotApiDailyRequestCount(activity.SerialSyncResult.TotalContacts, activity.Execution.StartUtc);

                return activity;
            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing contacts to HubSpot.");
                throw;
            }
            finally // *** ALWAYS *** capture the activity, even if the job is already processing or an exception occurs
            {
                activity.Execution.FinishUtc = _clock.UtcNow;
            }
        }
    }
}