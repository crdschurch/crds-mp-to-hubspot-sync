using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Web.Common.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CreateOrUpdateContactsInHubSpot : ICreateOrUpdateContactsInHubSpot
    {
        private const int MaxBatchSize = 100; // per HubSpot documentation: https://developers.hubspot.com/docs/methods/contacts/batch_create_or_update

        private readonly IHttpPost _http;
        private readonly IClock _clock;
        private readonly IJsonSerializer _serializer;
        private readonly ISleep _sleeper;
        private readonly string _hubSpotApiKey;
        private readonly ILogger<CreateOrUpdateContactsInHubSpot> _logger;

        public CreateOrUpdateContactsInHubSpot(IHttpPost http, IClock clock, IJsonSerializer serializer, ISleep sleeper, string hubSpotApiKey,
            ILogger<CreateOrUpdateContactsInHubSpot> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _sleeper = sleeper ?? throw new ArgumentNullException(nameof(sleeper));
            _hubSpotApiKey = hubSpotApiKey ?? throw new ArgumentNullException(nameof(hubSpotApiKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public BulkSyncResult BulkCreateOrUpdate(BulkContact[] contacts)
        {
            var run = new BulkSyncResult (_clock.UtcNow)
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
                            run.FailedBatches.Add(new BulkSyncFailure
                            {
                                Count = contactBatch.Length,
                                BatchNumber = currentBatchNumber + 1,
                                HttpStatusCode = response.StatusCode,
                                Exception = GetContent<HubSpotException>(response),
                                Contacts = contactBatch
                            });

                            // cast to print out the HTTP status code, just in case what's returned isn't
                            // defined in the https://stackoverflow.com/a/22645395
                            _logger.LogWarning($@"REJECTED: contact batch {currentBatchNumber + 1} of {run.BatchCount}
httpstatuscode: {(int) response.StatusCode}
More details will be available in the serial processing logs.");
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

        public SerialCreateSyncResult<TCreateContact> SerialCreate<TCreateContact>(TCreateContact[] contacts) where TCreateContact : IContact
        {
            var run = new SerialCreateSyncResult<TCreateContact>(_clock.UtcNow) { TotalContacts = contacts.Length };
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
                        case HttpStatusCode.OK: // 200: deemed successful by HubSpot
                            run.SuccessCount++;
                            _logger.LogDebug($"OK: contact {currentContactIndex + 1} of {contacts.Length}");
                            break;
                        case HttpStatusCode.Conflict: // 409: contact already exists
                            run.ContactAlreadyExistsCount++;
                            break;
                        default: // contact was rejected for creation
                            run.FailureCount++;
                            var failure = new SerialCreateSyncFailure<TCreateContact>
                            {
                                HttpStatusCode = response.StatusCode,
                                Exception = GetContent<HubSpotException>(response),
                                Contact = contact
                            };
                            run.Failures.Add(failure);
                            LogContactFailure(failure, contact, currentContactIndex, contacts.Length);
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
        /// https://developers.hubspot.com/docs/methods/contacts/update_contact-by-email
        /// </summary>
        public CoreUpdateResult<TUpdateContact> SerialUpdate<TUpdateContact>(TUpdateContact[] contacts) where TUpdateContact : IUpdateContact
        {
            var run = new CoreUpdateResult<TUpdateContact>(_clock.UtcNow) { TotalContacts = contacts.Length };
            try
            {
                for (int currentContactIndex = 0; currentContactIndex < contacts.Length; currentContactIndex++)
                {
                    var contact = contacts[currentContactIndex];
                    var response = _http.Post($"contacts/v1/contact/email/{contact.Email}/profile?hapikey={_hubSpotApiKey}", contact);
                    if (response == null)
                    {
                        run.FailureCount++;
                        continue;
                    }

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NoContent: // deemed successful by HubSpot
                            run.SuccessCount++;
                            _logger.LogDebug($"No Content: contact {currentContactIndex + 1} of {contacts.Length}");
                            break;
                        case HttpStatusCode.NotFound:
                            run.ContactDoesNotExistCount++;
                            run.ContactsThatDoNotExist.Add(contact.ContactDoesNotExistContingency);
                            break;
                        default: // contact was rejected for creation
                            run.FailureCount++;
                            var failure = new CoreUpdateFailure<TUpdateContact>
                            {
                                HttpStatusCode = response.StatusCode,
                                Exception = GetContent<HubSpotException>(response),
                                Contact = contact
                            };
                            run.Failures.Add(failure);
                            LogContactFailure(failure, contact, currentContactIndex, contacts.Length);
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

        private void PumpTheBreaksEvery7RequestsToAvoid429Exceptions(int requestCount)
        {
            if (requestCount % 7 == 0) // spread requests out a bit to 7/s (not critical that this process be lightning fast)
            {
                _logger.LogDebug("Avoiding HTTP 429 start...");
                _sleeper.Sleep(1000);
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

        private void LogContactFailure(IFailureDetails failure, IContact contact, int currentContactIndex, int contactCount)
        {
            // cast to print out the HTTP status code, just in case what's returned isn't
            // defined in the enum https://stackoverflow.com/a/22645395

            var hubSpotException = failure.Exception;
            _logger.LogWarning($@"REJECTED: contact {currentContactIndex + 1} of {contactCount}
httpstatuscode: {(int)failure.HttpStatusCode}
issue: {hubSpotException?.Message} for ({hubSpotException?.ValidationResults?.FirstOrDefault()?.Name})
error: {hubSpotException?.ValidationResults?.FirstOrDefault()?.Error}
contact: {_serializer.Serialize(contact)}");
        }
    }
}
