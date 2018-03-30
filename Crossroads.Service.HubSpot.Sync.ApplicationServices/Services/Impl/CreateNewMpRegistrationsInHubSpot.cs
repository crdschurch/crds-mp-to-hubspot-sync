using System;
using System.Threading.Tasks;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.Data.MP;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CreateNewMpRegistrationsInHubSpot : ICreateNewMpRegistrationsInHubSpot
    {
        private readonly IMinistryPlatformContactRepository _ministryPlatformContactRepository;
        private readonly IMapper _mapper;
        private readonly ICreateOrUpdateContactsInHubSpot _hubSpotContactCreatorUpdater;
        private readonly IClock _clock;
        private readonly IConfigurationService _configurationService;
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<CreateNewMpRegistrationsInHubSpot> _logger;

        public CreateNewMpRegistrationsInHubSpot(
            IMinistryPlatformContactRepository ministryPlatformContactRepository,
            IMapper mapper,
            ICreateOrUpdateContactsInHubSpot hubSpotContactCreatorUpdater,
            IClock clock,
            IConfigurationService configurationService,
            IJobRepository jobRepository,
            ILogger<CreateNewMpRegistrationsInHubSpot> logger)
        {
            _ministryPlatformContactRepository = ministryPlatformContactRepository ?? throw new ArgumentNullException(nameof(ministryPlatformContactRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _hubSpotContactCreatorUpdater = hubSpotContactCreatorUpdater ?? throw new ArgumentNullException(nameof(hubSpotContactCreatorUpdater));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync()
        {
            var timeJustBeforeTheProcessKickedOff = _clock.Now;
            try
            {
                _logger.LogInformation("Starting new MP registrations to HubSpot one-way sync operation...");
                _logger.LogInformation("Checking job processing state...");
                var jobState = _configurationService.GetCurrentJobProcessingState();
                if (jobState == JobProcessingState.Processing)
                {
                    _logger.LogWarning("Job is already currently processing. Exiting...");
                    return;
                }

                _logger.LogInformation("Job is currently 'Idle'. Moving to 'Processing' state.");
                _jobRepository.SetJobProcessingState(JobProcessingState.Processing);
                _logger.LogInformation("Job is now in 'Processing' state.");

                _logger.LogInformation("Fetching last successful sync date...");
                var lastSuccessfulSyncDate = _configurationService.GetLastSuccessfulSyncDate();
                _logger.LogInformation($"Last successful sync date: {lastSuccessfulSyncDate}");

                _logger.LogInformation("Fetching newly registered contacts...");
                var newContacts = _ministryPlatformContactRepository.GetNewlyRegisteredContacts(lastSuccessfulSyncDate);
                _logger.LogInformation($"Number of contacts fetched: {newContacts.Count}");

                _logger.LogDebug("Mapping MP dtos to HubSpot contacts...");
                var hubSpotContacts = _mapper.Map<HubSpotContact[]>(newContacts);
                _logger.LogDebug("Mapping complete.");

                _logger.LogInformation("Creating newly registered MP contacts in HubSpot...");
                var jobActivityDto = await _hubSpotContactCreatorUpdater.CreateOrUpdateAsync(hubSpotContacts).ConfigureAwait(false);
                _logger.LogInformation("Creation complete.");

                jobActivityDto.ActivityDateTime = lastSuccessfulSyncDate;
                // reset the last successful sync date to the date time we grab just before starting this whole process
                // *** IF *** all batches were completed successfully or there was nothing to do.

                if (jobActivityDto.TotalContacts == 0) // nothing to do on this run
                {
                    _logger.LogWarning("0 newly registered contacts to sync to HubSpot on this run. Exiting...");
                    _jobRepository.SetLastSuccessfulSyncDate(timeJustBeforeTheProcessKickedOff);
                    SetJobProcessingStateToIdle();
                    return;
                }

                if (jobActivityDto.SuccessCount == jobActivityDto.TotalContacts)
                {
                    _logger.LogWarning("100% success! Don't get used to it.");
                    _jobRepository.SetLastSuccessfulSyncDate(timeJustBeforeTheProcessKickedOff);
                }

                // nothing succeeded, do not update the last successful sync date. simply re-reun with the previous sync date on the next run.
                if (jobActivityDto.FailureCount == jobActivityDto.TotalContacts)
                {
                    _logger.LogWarning($"{0} of {jobActivityDto.TotalContacts} contacts synced successfully.");
                    SetJobProcessingStateToIdle();
                    return;
                }

            }
            catch (Exception exc)
            {
                _logger.LogError(CoreEvent.Exception, exc, "An exception occurred while syncing newly registered MP contacts to HubSpot.");
                SetJobProcessingStateToIdle();
            }
        }

        private void SetJobProcessingStateToIdle()
        {
            _jobRepository.SetJobProcessingState(JobProcessingState.Idle);
            _logger.LogInformation("Job is now in an 'Idle' state.");
        }
    }
}