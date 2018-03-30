using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Flurl.Util;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CreateOrUpdateContactsInHubSpot : ICreateOrUpdateContactsInHubSpot
    {
        private const int MaxBatchSize = 100; // per HubSpot documentation: https://developers.hubspot.com/docs/methods/contacts/batch_create_or_update

        private readonly IHttpPost _httpPost;
        private readonly string _hubSpotApiKey;
        private readonly ILogger<CreateOrUpdateContactsInHubSpot> _logger;

        public CreateOrUpdateContactsInHubSpot (IHttpPost httpPost, string hubSpotApiKey, ILogger<CreateOrUpdateContactsInHubSpot> logger)
        {
            _httpPost = httpPost ?? throw new ArgumentNullException(nameof(httpPost));
            _hubSpotApiKey = hubSpotApiKey ?? throw new ArgumentNullException(nameof(hubSpotApiKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<JobActivityDto> CreateOrUpdateAsync(HubSpotContact[] contactsToPushUpToHubSpot)
        {
            var activityDto = new JobActivityDto {TotalContacts = contactsToPushUpToHubSpot.Length};
            var contactCount = activityDto.TotalContacts;
            var numberOfBatches = (contactCount / MaxBatchSize) + (contactCount % MaxBatchSize > 0 ? 1 : 0);

            for (int currentBatchNumber = 0; currentBatchNumber < numberOfBatches; currentBatchNumber++)
            {
                var contactBatch = contactsToPushUpToHubSpot.Skip(currentBatchNumber * MaxBatchSize).Take(MaxBatchSize).ToArray();
                var response = await _httpPost.PostAsync($"contacts/v1/contact/batch?hapikey={_hubSpotApiKey}", new HubSpotContactRoot { Contacts = contactBatch.ToArray() })
                    .ConfigureAwait(false);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Accepted: // deemed successful by HubSpot -- all in the batch were accepted
                        activityDto.SuccessCount += contactBatch.Length;
                        _logger.LogInformation($"ACCEPTED: contact batch {currentBatchNumber} of {numberOfBatches}");
                        break;
                    default: // HttpStatusCode.BadRequest: 400 or a 429 too many requests
                             // whatever the code, something went awry and NONE of the contacts were accpeted
                        activityDto.FailureCount += contactBatch.Length;
                        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        // cast to print out the HTTP status code, just in case what's returned isn't
                        // defined in the https://stackoverflow.com/a/22645395
                        _logger.LogWarning($@"REJECTED: contact batch {currentBatchNumber} of {numberOfBatches}
httpstatuscode: {(int)response.StatusCode}
reason: {responseString}
contactids: {string.Join(",", contactBatch.Select(contact => contact.Properties.Where(prop => prop.Property == "MinistryPlatformContactId").Select(prop => prop.Value)))}");

                        activityDto.FailedBatches.Add(new FailedBatchDto
                        {
                            Count = contactBatch.Length,
                            BatchNumber = currentBatchNumber,
                            HttpStatusCode = response.StatusCode.ToInvariantString(),
                            Reason = responseString,
                            Contacts = contactBatch
                        });
                        break;
                }

                if (currentBatchNumber % 5 == 0) // spread requests out a bit to 5/s (not critical that this process be lightning fast)
                {
                    _logger.LogDebug("Avoiding HTTP 429 start...");
                    await Task.Delay(1000);
                    _logger.LogDebug("Avoiding HTTP 429 end.");
                }
            }

            return activityDto;
        }

        //public async Task<HubSpotCreateOrUpdateResultDto> RetryCreateAsync(HubSpotContact[] contactsToRetry)
        //{
        //    var resultDto = 
        //}
    }
}
