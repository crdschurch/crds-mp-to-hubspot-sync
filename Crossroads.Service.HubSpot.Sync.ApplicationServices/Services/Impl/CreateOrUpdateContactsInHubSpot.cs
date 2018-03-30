using System;
using System.Linq;
using System.Net;
using System.Threading;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Web.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    /// <summary>
    /// Don't worry Tim, this is going to get refactored, refactored, refactored.
    /// </summary>
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

        public TActivity BulkCreateOrUpdate<TActivity>(HubSpotContact[] contactsToPushUpToHubSpot, TActivity activity) where TActivity : IActivity
        {
            activity.TotalContacts = contactsToPushUpToHubSpot.Length;
            var contactCount = activity.TotalContacts;
            var numberOfBatches = (contactCount / MaxBatchSize) + (contactCount % MaxBatchSize > 0 ? 1 : 0);

            for (int currentBatchNumber = 0; currentBatchNumber < numberOfBatches; currentBatchNumber++)
            {
                var contactBatch = contactsToPushUpToHubSpot.Skip(currentBatchNumber * MaxBatchSize).Take(MaxBatchSize).ToArray();
                var response = _httpPost.Post($"contacts/v1/contact/batch?hapikey={_hubSpotApiKey}", new HubSpotContactRoot { Contacts = contactBatch.ToArray() });

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Accepted: // deemed successful by HubSpot -- all in the batch were accepted
                        activity.SuccessCount += contactBatch.Length;
                        _logger.LogInformation($"ACCEPTED: contact batch {currentBatchNumber} of {numberOfBatches}");
                        break;
                    default: // HttpStatusCode.BadRequest: 400 or a 429 too many requests
                             // whatever the code, something went awry and NONE of the contacts were accpeted
                        activity.FailureCount += contactBatch.Length;
                        var responseString = response.GetContent();

                        // cast to print out the HTTP status code, just in case what's returned isn't
                        // defined in the https://stackoverflow.com/a/22645395
                        _logger.LogWarning($@"REJECTED: contact batch {currentBatchNumber} of {numberOfBatches}
httpstatuscode: {(int)response.StatusCode}
reason: {responseString}");

                        activity.FailedBatches.Add(new FailedBatch
                        {
                            Count = contactBatch.Length,
                            BatchNumber = currentBatchNumber,
                            HttpStatusCode = response.StatusCode.ToString(),
                            Reason = responseString,
                            Contacts = contactBatch
                        });
                        break;
                }

                if (currentBatchNumber % 7 == 0) // spread requests out a bit to 7/s (not critical that this process be lightning fast)
                {
                    _logger.LogDebug("Avoiding HTTP 429 start...");
                    Thread.Sleep(1000);
                    _logger.LogDebug("Avoiding HTTP 429 end.");
                }
            }

            return activity;
        }

        public NewContactActivity RetryBulkCreate(NewContactActivity firstPass)
        {
            _logger.LogInformation($"Retrying failed new MP contact registration sync to HubSpot in bulk. {firstPass.FailureCount} in play.");
            var secondPass = BulkCreateOrUpdate(firstPass.FailedBatches.SelectMany(batchRoot => batchRoot.Contacts).ToArray(), new NewContactActivity{ActivityDateTime = firstPass.ActivityDateTime});

            if (secondPass.SuccessCount == secondPass.TotalContacts)
                return secondPass;

            if (firstPass.FailureCount == secondPass.FailureCount) // let's try individual contact creation and then shut it down
                _logger.LogInformation("2nd pass at syncing new MP contacts to HubSpot failed at the same rate of the first run.");

            if(secondPass.FailureCount < firstPass.FailureCount) // still some failure
                _logger.LogInformation($"More success on the 2nd run. {secondPass.SuccessCount} out of {secondPass.TotalContacts} succeeded.");

            _logger.LogInformation("Resorting to serial syncing of new contacts.");
            return Create(new NewContactActivity {ActivityDateTime = secondPass.ActivityDateTime});
        }

        public NewContactActivity Create(NewContactActivity thirdPass)
        {
            var contacts = thirdPass.FailedBatches.SelectMany(batchHeader => batchHeader.Contacts) // we'll find a better way to accommodate the finicky API in refactor; another type + AutoMapper?
                .Select(contact =>
                {
                    contact.Properties.Add(new ContactProperty {Property = "email", Value = contact.Email});
                    return contact;
                }).ToArray();

            for (int currentContactIndex = 0; currentContactIndex < contacts.Length; currentContactIndex++)
            {
                var contact = contacts[currentContactIndex];
                var response = _httpPost.Post($"contacts/v1/contact?hapikey={_hubSpotApiKey}", contact);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Accepted: // deemed successful by HubSpot -- all in the batch were accepted
                        thirdPass.SuccessCount++;
                        _logger.LogDebug($"ACCEPTED: contact {currentContactIndex+1} of {contacts.Length}");
                        break;
                    default: // HttpStatusCode.BadRequest: 400 or a 429 too many requests
                        // whatever the code, something went awry and NONE of the contacts were accpeted
                        thirdPass.FailureCount++;
                        var responseString = response.GetContent();

                        // cast to print out the HTTP status code, just in case what's returned isn't
                        // defined in the https://stackoverflow.com/a/22645395
                        _logger.LogWarning($@"REJECTED: contact {currentContactIndex + 1} of {contacts.Length}
httpstatuscode: {(int)response.StatusCode}
reason: {responseString}");

                        thirdPass.FailedBatches.Add(new FailedBatch
                        {
                            Count = contacts.Length,
                            BatchNumber = 1,
                            HttpStatusCode = response.StatusCode.ToString(),
                            Reason = responseString,
                            Contacts = new [] {contact}
                        });
                        break;
                }

                if (currentContactIndex % 7 == 0) // spread requests out a bit to 7/s (not critical that this process be lightning fast)
                {
                    _logger.LogDebug("Avoiding HTTP 429 start...");
                    Thread.Sleep(1000);
                    _logger.LogDebug("Avoiding HTTP 429 end.");
                }
            }

            return thirdPass;
        }
    }
}
