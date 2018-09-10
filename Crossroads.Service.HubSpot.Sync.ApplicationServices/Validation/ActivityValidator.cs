using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation
{
    /// <summary>
    /// Interrogates the activity object to determine if any showstoppers/potentially temporary failures
    /// that could be retried next time around and finish with success.
    /// </summary>
    public class ActivityValidator : AbstractValidator<IActivity>
    {
        /// <summary>
        /// Contact attribute data is being passed across the wire to HubSpot before the property
        /// meant to receive it has been created in HubSpot.
        /// 
        /// *** ACTION ITEM ***: Create the property in HubSpot.
        /// </summary>
        private const string HubSpotPropertyDoesNotExistSearchString = "PROPERTY_DOESNT_EXIST";

        /// <summary>
        /// Contact attribute data is being passed to a HubSpot contact property that does not
        /// contain the provided value. This is relevant to a HubSpot static list where the allowed
        /// values are predetermined by its current definition.
        /// 
        /// *** ACTION ITEM ***: Create the entry in the HubSpot property's static list.
        /// </summary>
        private const string HubSpotPropertyInvalidOptionSearchString = "INVALID_OPTION";

        public ActivityValidator()
        {
            RuleFor(activity => activity).NotNull();

            RuleSet(RuleSetName.NewRegistrationSync, () =>
            {
                RuleFor(activity => activity.NewRegistrationSyncOperation).NotNull();
                RuleFor(activity => activity.NewRegistrationSyncOperation).Must(NotHaveEncounteredHubSpotIssuesDuringNewRegistrationSyncOperation);
            });

            RuleSet(RuleSetName.CoreContactAttributeSync, () =>
            {
                RuleFor(activity => activity.CoreContactAttributeSyncOperation).NotNull();
                RuleFor(activity => activity.CoreContactAttributeSyncOperation).Must(NotHaveEncounteredHubSpotIssuesDuringCoreUpdateSyncOperation);
            });

            RuleSet(RuleSetName.ChildAgeGradeSync, () =>
            {
                RuleFor(activity => activity.ChildAgeAndGradeSyncOperation).NotNull();
                RuleFor(activity => activity.ChildAgeAndGradeSyncOperation).Must(NotHaveEncounteredHubSpotIssuesDuringAgeGradeSyncOperation);
            });
        }

        /// <summary>
        /// If a property doesn't exist in HubSpot, then don't save a last successful date for
        /// new registrations b/c records have been rejected that need to be retried.
        /// </summary>
        private bool NotHaveEncounteredHubSpotIssuesDuringNewRegistrationSyncOperation(IActivitySyncOperation operation)
        {
            var failures = operation.SerialCreateResult.Failures
                .Union(operation.SerialUpdateResult.Failures)
                .Union(operation.SerialReconciliationResult.Failures).ToList();

            return HubSpotIssuesEncountered(failures) == false;
        }

        /// <summary>
        /// If a property doesn't exist in HubSpot, then don't save last successful dates for core
        /// contact updates b/c records have been rejected that need to be retried.
        /// </summary>
        private bool NotHaveEncounteredHubSpotIssuesDuringCoreUpdateSyncOperation(IActivitySyncOperation operation)
        {
            var failures = operation.SerialUpdateResult.Failures
                .Union(operation.SerialCreateResult.Failures)
                .Union(operation.SerialReconciliationResult.Failures).ToList();

            return HubSpotIssuesEncountered(failures) == false;
        }

        private bool NotHaveEncounteredHubSpotIssuesDuringAgeGradeSyncOperation(IActivityChildAgeAndGradeSyncOperation operation)
        {
            var failures = operation.BulkUpdateSyncResult1000.FailedBatches
                .Union(operation.BulkUpdateSyncResult100.FailedBatches)
                .Union(operation.BulkUpdateSyncResult10.FailedBatches)
                .Union<IFailureDetails>(operation.RetryBulkUpdateAsSerialUpdateResult.Failures)
                .Union(operation.SerialCreateResult.Failures).ToList();

            return HubSpotIssuesEncountered(failures) == false;
        }

        /// <summary>
        /// If we've experienced one or more of the following, we'll forestall recording a "last successful
        /// processing date" for a given operation:
        /// 1) received a HTTP status code of 500 from HubSpot.
        /// 2) Property doesn't exist exception.
        /// 3) Property value is not valid exception.
        /// 
        /// HTTP 400s (Bad Request) alone will not stop the recording of a last successful processing date,
        /// as sometimes this is the product of an invalid email address, etc.
        /// 
        /// https://en.wikipedia.org/wiki/List_of_HTTP_status_codes
        /// 4xx (Client Error): The request contains bad syntax or cannot be fulfilled
        /// 5xx (Server Error): The server failed to fulfill an apparently valid request
        /// </summary>
        /// <param name="failures">Failures recorded while syncing MP contact data to HubSpot.</param>
        private bool HubSpotIssuesEncountered(IReadOnlyCollection<IFailureDetails> failures)
        {
            if (failures == null || failures.Count == 0) return false;

            var httpStatusCodes = new HashSet<HttpStatusCode>(failures.Select(f => f.HttpStatusCode));
            var errors = new HashSet<string>(failures.Select(GetErrors).SelectMany(error => error));    // flatten errors & keep only distinct versions
            return httpStatusCodes.Any(IsAnUnhandledHttpClientOrServerError) ||                         // 4xx (Client Error) & 5xx (Server Error)
                   errors.Any(item => item.Contains(HubSpotPropertyDoesNotExistSearchString)) ||        // HubSpot property doesn't exist (possible user error)
                   errors.Any(item => item.Contains(HubSpotPropertyInvalidOptionSearchString));         // Value passed to a static list HubSpot property is invalid (possible user error)
        }

        private IEnumerable<string> GetErrors(IFailureDetails failure)
        {
            return failure?.Exception?.ValidationResults?.Select(result => result?.Error) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// This should cover things like 401 Unauthorized (API key is no good), 429s if we've got HubSpot
        /// cross-talk between apps and go over the 10/s rate-limit, 500 internal service errors, 504 gateway
        /// timeout errors, etc. This should NOT HAVE TO handle 409 Conflicts, b/c we handle these in the app
        /// layer.
        /// 
        /// We are EXPLICITLY, PURPOSELY excluding 400s, as we send bad email addresses over the wire ALL THE
        /// TIME. HubSpot validates email address construction, which results in HTTP 400 failures. And since we
        /// track and reconcile changes to the email address in MP, there's no need to prevent the last processed
		/// date from advancing by failing validation here, which would effectively make our pool of users to process
        /// grow indefinitely. We'll preserve validation failure for more pressing errors (temporary server failures,
		/// configuration-related exceptions, etc).
        /// </summary>
        private bool IsAnUnhandledHttpClientOrServerError(HttpStatusCode httpStatusCode)
        {
            return (int)httpStatusCode > 400 && (int)httpStatusCode < 600;
        }
    }
}
