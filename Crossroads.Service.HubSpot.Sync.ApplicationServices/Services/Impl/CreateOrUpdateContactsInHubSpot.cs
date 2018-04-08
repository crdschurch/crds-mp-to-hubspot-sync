using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Web.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CreateOrUpdateContactsInHubSpot : ICreateOrUpdateContactsInHubSpot
    {
        private const int MaxBatchSize = 100; // per HubSpot documentation: https://developers.hubspot.com/docs/methods/contacts/batch_create_or_update

        private readonly IHttpPost _http;
        private readonly IClock _clock;
        private readonly IJsonSerializer _serializer;
        private readonly string _hubSpotApiKey;
        private readonly ILogger<CreateOrUpdateContactsInHubSpot> _logger;

        public CreateOrUpdateContactsInHubSpot(
            IHttpPost http,
            IClock clock,
            IJsonSerializer serializer,
            string hubSpotApiKey, ILogger<CreateOrUpdateContactsInHubSpot> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _hubSpotApiKey = hubSpotApiKey ?? throw new ArgumentNullException(nameof(hubSpotApiKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public BulkRunResult BulkCreateOrUpdate(BulkContact[] contacts)
        {
            var run = new BulkRunResult (_clock.UtcNow)
            {
                TotalContacts = contacts.Length,
                BatchCount = (contacts.Length / MaxBatchSize) + (contacts.Length % MaxBatchSize > 0 ? 1 : 0)
            };

            try
            {
                for (int currentBatchNumber = 0; currentBatchNumber < run.BatchCount; currentBatchNumber++)
                {
                    var contactBatch = contacts.Skip(currentBatchNumber * MaxBatchSize).Take(MaxBatchSize).ToArray();
                    var response = _http.Post($"contacts/v1/contact/batch?hapikey={_hubSpotApiKey}", contactBatch);
                    if (response == null)
                    {
                        run.FailureCount += contactBatch.Length;
                        continue;
                    }

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Accepted: // deemed successful by HubSpot -- all in the batch were accepted
                            run.SuccessCount += contactBatch.Length;
                            _logger.LogInformation($"ACCEPTED: contact batch {currentBatchNumber + 1} of {run.BatchCount}");
                            break;
                        default: // 400, 429, etc; something went awry and NONE of the contacts were accepted
                            run.FailureCount += contactBatch.Length;
                            run.FailedBatches.Add(new BulkFailure
                            {
                                Count = contactBatch.Length,
                                BatchNumber = currentBatchNumber + 1,
                                HttpStatusCode = response.StatusCode,
                                Reason = GetContent(response),
                                Contacts = contactBatch
                            });

                            // cast to print out the HTTP status code, just in case what's returned isn't
                            // defined in the https://stackoverflow.com/a/22645395
                            _logger.LogWarning($@"REJECTED: contact batch {currentBatchNumber} of {run.BatchCount}
httpstatuscode: {(int) response.StatusCode}
reason: {run.FailedBatches[run.FailedBatches.Count - 1].Reason}");

                            break;
                    }

                    PumpTheBreaksEvery7RequestsToAvoid429Exceptions(currentBatchNumber);
                }

                return run;
            }
            finally
            {
                run.Execution.FinishUtc = _clock.UtcNow;
            }
        }

        public SerialRunResult SerialCreate(SerialContact[] contacts)
        {
            var run = new SerialRunResult(_clock.UtcNow) { TotalContacts = contacts.Length };
            try
            {
                for (int currentContactIndex = 0; currentContactIndex < contacts.Length; currentContactIndex++)
                {
                    var contact = contacts[currentContactIndex];
                    var response = _http.Post($"contacts/v1/contact?hapikey={_hubSpotApiKey}", contact);
                    if (response == null)
                    {
                        run.FailureCount++;
                        continue;
                    }

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK: // deemed successful by HubSpot
                            run.SuccessCount++;
                            _logger.LogDebug($"OK: contact {currentContactIndex + 1} of {contacts.Length}");
                            break;
                        default: // contact was rejected for creation
                            var contactAlreadyExists = ContactAlreadyExists(response);
                            if (contactAlreadyExists)
                            {
                                run.ContactAlreadyExistsCount++;
                                continue;
                            }

                            run.FailureCount++;
                            run.Failures.Add(new SerialFailure
                            {
                                HttpStatusCode = response.StatusCode,
                                Reason = GetContent(response),
                                Contact = contact
                            });
                            // cast to print out the HTTP status code, just in case what's returned isn't
                            // defined in the https://stackoverflow.com/a/22645395
                            _logger.LogWarning($@"REJECTED: contact {currentContactIndex + 1} of {contacts.Length}
httpstatuscode: {(int) response.StatusCode}
reason: {run.Failures[run.Failures.Count - 1].Reason}
contact: {_serializer.Serialize(contacts)}");

                            break;
                    }

                    PumpTheBreaksEvery7RequestsToAvoid429Exceptions(currentContactIndex);
                }

                return run;
            }
            finally
            {
                run.Execution.FinishUtc = _clock.UtcNow;
            }
        }

        /// <summary>
        /// Let's exclude any "Contact already exists" errors b/c this is an acceptable failure when attempting to
        /// explicitly create a contact.
        /// </summary>
        private bool ContactAlreadyExists(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.Conflict)
                return false;

            var conflict = GetContent<Conflict>(response);

            // a bit brittle (should HubSpot change the error), but the worst that happens is we capture contact exists
            // errors in our failure collection stored in the activity
            return conflict?.Error.Equals("CONTACT_EXISTS", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private void PumpTheBreaksEvery7RequestsToAvoid429Exceptions(int requestCount)
        {
            if (requestCount % 7 == 0) // spread requests out a bit to 7/s (not critical that this process be lightning fast)
            {
                _logger.LogDebug("Avoiding HTTP 429 start...");
                Thread.Sleep(1000);
                _logger.LogDebug("Avoiding HTTP 429 end.");
            }
        }

        /// <summary>
        /// Wraps getting content stream in a try catch just in case something goes awry.
        /// </summary>
        private string GetContent(HttpResponseMessage response)
        {
            try
            {
                return response.GetContent();
            }
            catch (Exception exc)
            {
                string message = "Exception occurred while getting content stream.";
                _logger.LogError(exc, message);
                return $@"{message}
{exc}";
            }
        }

        /// <summary>
        /// Wraps getting content stream in a try catch just in case something goes awry.
        /// </summary>
        private T GetContent<T>(HttpResponseMessage response)
        {
            try
            {
                return response.GetContent<T>();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Exception occurred while getting content stream.");
                return default(T);
            }
        }
    }
}
