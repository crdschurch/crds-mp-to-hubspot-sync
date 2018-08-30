using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Core.Time;
using Crossroads.Service.HubSpot.Sync.Core.Utilities;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CreateOrUpdateContactsInHubSpot : ICreateOrUpdateContactsInHubSpot
    {
        private const int DefaultBatchSize = 100; // per HubSpot documentation: https://developers.hubspot.com/docs/methods/contacts/batch_create_or_update

        private readonly IHttpClientFacade _http;
        private readonly IClock _clock;
        private readonly IJsonSerializer _serializer;
        private readonly ISleep _sleeper;
        private readonly string _hubSpotApiKey;
        private readonly ILogger<CreateOrUpdateContactsInHubSpot> _logger;

        public CreateOrUpdateContactsInHubSpot(IHttpClientFacade http, IClock clock, IJsonSerializer serializer, ISleep sleeper, string hubSpotApiKey,
            ILogger<CreateOrUpdateContactsInHubSpot> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _sleeper = sleeper ?? throw new ArgumentNullException(nameof(sleeper));
            _hubSpotApiKey = hubSpotApiKey ?? throw new ArgumentNullException(nameof(hubSpotApiKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// https://developers.hubspot.com/docs/methods/contacts/batch_create_or_update
        /// </summary>
        /// <param name="contacts">Contacts to create/update in HubSpot.</param>
        /// <param name="batchSize">Number of contacts to send to HubSpot per request.</param>
        public BulkSyncResult BulkSync(BulkContact[] contacts, int batchSize = DefaultBatchSize)
        {
            var run = new BulkSyncResult(_clock.UtcNow)
            {
                TotalContacts = contacts.Length,
                BatchCount = CalculateNumberOfBatches(contacts, batchSize)
            };

            try
            {
                for (int currentBatchNumber = 0; currentBatchNumber < run.BatchCount; currentBatchNumber++)
                {
                    var contactBatch = contacts.Skip(currentBatchNumber * batchSize).Take(batchSize).ToArray(); // extract the relevant group of contacts
                    var response = _http.Post($"contacts/v1/contact/batch?hapikey={_hubSpotApiKey}", contactBatch);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Accepted: // 202; deemed successful by HubSpot -- all in the batch were accepted
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
                                Exception = _http.GetResponseContent<HubSpotException>(response),
                                Contacts = contactBatch
                            });

                            // cast to print out the HTTP status code, just in case what's returned isn't
                            // defined in the https://stackoverflow.com/a/22645395
                            _logger.LogWarning($@"REJECTED: contact batch {currentBatchNumber + 1} of {run.BatchCount}
httpstatuscode: {(int) response.StatusCode}
More details will be available in the serial processing logs.");
                            break;
                    }

                    PumpTheBreaksEveryNRequestsToAvoid429Exceptions(currentBatchNumber + 1);
                }

                return run;
            }
            finally
            {
                run.Execution.FinishUtc = _clock.UtcNow;
            }
        }

        private int CalculateNumberOfBatches(BulkContact[] contacts, int prescribedBatchSize)
        {
            return (contacts.Length / prescribedBatchSize) + (contacts.Length % prescribedBatchSize > 0 ? 1 : 0);
        }

        /// <summary>
        /// https://developers.hubspot.com/docs/methods/contacts/create_contact
        /// </summary>
        public SerialSyncResult SerialCreate(SerialContact[] contacts)
        {
            var run = new SerialSyncResult(_clock.UtcNow) { TotalContacts = contacts.Length };
            try
            {
                for (int currentContactIndex = 0; currentContactIndex < contacts.Length; currentContactIndex++)
                {
                    var contact = contacts[currentContactIndex];
                    var response = _http.Post($"contacts/v1/contact?hapikey={_hubSpotApiKey}", contact);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK: // 200; create endpoint
                            _logger.LogInformation($"Created: contact {currentContactIndex + 1} of {contacts.Length}");
                            run.SuccessCount++;
                            run.InsertCount++;
                            break;
                        case HttpStatusCode.Conflict: // 409; create endpoint; already exists -- when a contact attempts to update their email address to one already claimed
                            run.EmailAddressesAlreadyExist.Add(contact);
                            run.EmailAddressAlreadyExistsCount++;
                            break;
                        default: // contact was rejected for create
                            SetFailureData(run, response, contact, contacts.Length, currentContactIndex);
                            break;
                    }

                    PumpTheBreaksEveryNRequestsToAvoid429Exceptions(currentContactIndex + 1);
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
        public SerialSyncResult SerialUpdate(SerialContact[] contacts)
        {
            var run = new SerialSyncResult(_clock.UtcNow) { TotalContacts = contacts.Length };
            try
            {
                for (int currentContactIndex = 0; currentContactIndex < contacts.Length; currentContactIndex++)
                {
                    var contact = contacts[currentContactIndex];
                    SerialUpdate(currentContactIndex, contact, contacts.Length, run);
                    PumpTheBreaksEveryNRequestsToAvoid429Exceptions(currentContactIndex + 1);
                }

                return run;
            }
            finally
            {
                run.Execution.FinishUtc = _clock.UtcNow;
            }
        }

        private SerialSyncResult SerialUpdate(int currentContactIndex, SerialContact contact, int contactCount, SerialSyncResult run)
        {
            var response = _http.Post($"contacts/v1/contact/email/{contact.Email}/profile?hapikey={_hubSpotApiKey}", contact);

            switch (response.StatusCode)
            {
                case HttpStatusCode.NoContent: // 204; update only endpoint
                    _logger.LogInformation($"Updated: contact {currentContactIndex + 1} of {contactCount}");
                    run.SuccessCount++;
                    run.UpdateCount++;
                    break;
                case HttpStatusCode.NotFound: // 404; update only endpoint; contact does not exist
                    run.EmailAddressesDoNotExist.Add(contact);
                    run.EmailAddressDoesNotExistCount++;
                    break;
                case HttpStatusCode.Conflict: // 409; update endpoint; already exists -- when a contact attempts to update their email address to one already claimed
                    run.EmailAddressesAlreadyExist.Add(contact);
                    run.EmailAddressAlreadyExistsCount++;
                    break;
                default: // contact was rejected for update (application exception)
                    SetFailureData(run, response, contact, contactCount, currentContactIndex);
                    break;
            }

            return run;
        }

        /// <summary>
        /// Responsible for deleting the contact record of the old email address that is not able to be updated
        /// to the "new" email address due to the fact that the email address we wish to switch to already exists.
        /// We're ok deleting the existing account b/c Ministry Platform's dp_Users.User_Name field is our source
        /// of truth, is not nullable and has a unique constraint; so the contact attempting to update to a given
        /// email address is the true owner of the account. An edge case, but I've seen this happen and would like to
        /// minimize the cruft created by this app in HubSpot.
        /// 
        /// 1) Get contact by old email address
        /// 2) Delete contact by VID (acquired by old email address)
        /// 3) Update contact in HubSpot with new email address in both the url and the post body
        /// </summary>
        public SerialSyncResult ReconcileConflicts(SerialContact[] contacts)
        {
            const int requestsPerReconciliation = 3, requestsPerSecond = 9;
            int reconciliationIteration = 1;

            var run = new SerialSyncResult(_clock.UtcNow) { TotalContacts = contacts.Length };
            try
            {
                for (int currentContactIndex = 0; currentContactIndex < contacts.Length; currentContactIndex++)
                {
                    var contact = contacts[currentContactIndex];
                    var hubSpotContact = SerialGet<HubSpotVidResult>(contact, run); // HubSpot request #1
                    run = SerialDelete(currentContactIndex, contact, hubSpotContact, contacts.Length, run); // HubSpot request #2
                    contact.Email = contact.Properties.First(p => p.Name == "email").Value; // reset email to the existing one and re-run it
                    run = SerialUpdate(currentContactIndex, contact, contacts.Length, run); // HubSpot request #3

                    PumpTheBreaksEveryNRequestsToAvoid429Exceptions(reconciliationIteration * requestsPerReconciliation, requestsPerSecond);

                    if(reconciliationIteration == requestsPerReconciliation)
                        reconciliationIteration = 0; // reset for the next sleep 3 reconciliations later

                    reconciliationIteration++;
                }

                return run;
            }
            finally
            {
                run.Execution.FinishUtc = _clock.UtcNow;
            }
        }

        private TDto SerialGet<TDto>(SerialContact contact, SerialSyncResult run)
        {
            var response = _http.Get($"contacts/v1/contact/email/{contact.Email}/profile?hapikey={_hubSpotApiKey}&property=vid");
            run.GetCount++;
            var dto = default(TDto);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK: // 200; update only endpoint
                    _logger.LogInformation($"Retrieved: contact {contact.Email}.\r\njson: {dto}");
                    dto = _http.GetResponseContent<TDto>(response);
                    break;
                case HttpStatusCode.NotFound: // 404; contact with supplied email address does not exist
                    _logger.LogWarning($"Not Found. Contact does not exist\r\njson: {_serializer.Serialize(contact)}");
                    break;
                default: // could not get contact
                    _logger.LogWarning($"Exception occurred while GETting contact [{contact.Email}]");
                    break;
            }

            return dto;
        }

        private SerialSyncResult SerialDelete(int currentContactIndex, SerialContact contact, HubSpotVidResult hubSpotContact, int contactCount, SerialSyncResult run)
        {
            if (hubSpotContact == null)
                return run;

            var response = _http.Delete($"contacts/v1/contact/vid/{hubSpotContact.ContactVid}?hapikey={_hubSpotApiKey}");
            run.DeleteCount++;

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK: // 200; when contact is deleted successfully
                    _logger.LogInformation($"Deleted: contact {currentContactIndex + 1} of {contactCount}");
                    break;
                case HttpStatusCode.NotFound: // 404; when the contact vid does not exist
                    _logger.LogWarning($"No contact in HubSpot to delete.\r\njson: {_serializer.Serialize(contact)}");
                    break;
                default: // contact was rejected for update (application exception)
                    SetFailureData(run, response, contact, contactCount, currentContactIndex);
                    break;
            }

            return run;
        }

        private void SetFailureData(SerialSyncResult run, HttpResponseMessage response, IContact contact, int contactLength, int currentContactIndex)
        {
            run.FailureCount++;
            var failure = new SerialSyncFailure
            {
                HttpStatusCode = response.StatusCode,
                Exception = _http.GetResponseContent<HubSpotException>(response),
                Contact = contact
            };
            run.Failures.Add(failure);
            LogContactFailure(failure, contact, currentContactIndex, contactLength);
        }

        /// <summary>
        /// Should NEVER exceed 10 requests/sec
        /// </summary>
        private void PumpTheBreaksEveryNRequestsToAvoid429Exceptions(int requestCount, int requestThresholdInterval = 7) // spread requests out a bit to 7/s (not critical that this process be lightning fast)
        {
            if (requestThresholdInterval > 10)
                requestThresholdInterval = 10;

            if (requestCount % requestThresholdInterval == 0) 
            {
                _logger.LogInformation("Avoiding HTTP 429 start...");
                _sleeper.Sleep(1000);
                _logger.LogInformation("Avoiding HTTP 429 end.");
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
