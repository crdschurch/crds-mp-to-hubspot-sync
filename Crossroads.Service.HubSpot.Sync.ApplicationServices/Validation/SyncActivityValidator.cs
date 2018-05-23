using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Validation
{
    /// <summary>
    /// Interrogates the activity object to determine if any 
    /// </summary>
    public class SyncActivityValidator : AbstractValidator<ISyncActivity>
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

        public SyncActivityValidator()
        {
            RuleFor(activity => activity).NotNull();

            RuleSet(RuleSetName.Registration, () =>
            {
                RuleFor(activity => activity.NewRegistrationOperation).NotNull();
                RuleFor(activity => activity.NewRegistrationOperation).Must(NotHaveEncounteredHubSpotIssuesDuringNewRegistrationOperation);
            });

            RuleSet(RuleSetName.CoreUpdate, () =>
            {
                RuleFor(activity => activity.CoreUpdateOperation).NotNull();
                RuleFor(activity => activity.CoreUpdateOperation).Must(NotHaveEncounteredHubSpotIssuesDuringCoreUpdateOperation);
            });

            RuleSet(RuleSetName.AgeGradeUpdate, () =>
            {
                RuleFor(activity => activity.ChildAgeAndGradeUpdateOperation).NotNull();
                RuleFor(activity => activity.ChildAgeAndGradeUpdateOperation).Must(NotHaveEncounteredHubSpotIssuesDuringAgeGradeUpdateOperation);
                RuleFor(activity => activity.ChildAgeAndGradeUpdateOperation.AgeAndGradeDelta).Must(HaveDeltasInOrderToBotherTryingToPushDataUpToHubSpot);
            });
        }

        private bool HaveDeltasInOrderToBotherTryingToPushDataUpToHubSpot(ChildAgeAndGradeDeltaLogDto deltaLog)
        {
            return (deltaLog.InsertCount == 0 && deltaLog.UpdateCount == 0) == false;
        }

        private bool NotHaveEncounteredHubSpotIssuesDuringAgeGradeUpdateOperation(ISyncActivityChildAgeAndGradeUpdateOperation operation)
        {
            var failures = operation.BulkUpdateSyncResult100.FailedBatches
                    .Union<IFailureDetails>(operation.BulkUpdateSyncResult10.FailedBatches)
                    .Union(operation.RetryBulkUpdateAsSerialUpdateResult.Failures).ToList();

            return HubSpotIssuesEncountered(failures) == false;
        }

        /// <summary>
        /// If a property doesn't exist in HubSpot, then don't save a last successful date for
        /// new registrations b/c records have been rejected that need to be retried.
        /// </summary>
        private bool NotHaveEncounteredHubSpotIssuesDuringNewRegistrationOperation(ISyncActivityNewRegistrationOperation operation)
        {
            var failures = operation.BulkCreateSyncResult.FailedBatches
                    .Union<IFailureDetails>(operation.SerialCreateSyncResult.Failures)
                    .ToList();

            return HubSpotIssuesEncountered(failures) == false;
        }

        /// <summary>
        /// If a property doesn't exist in HubSpot, then don't save last successful dates for core
        /// contact updates b/c records have been rejected that need to be retried.
        /// </summary>
        public bool NotHaveEncounteredHubSpotIssuesDuringCoreUpdateOperation(ISyncActivityCoreUpdateOperation operation)
        {
            var failures = operation.SerialUpdateResult.Failures.Union<IFailureDetails>(operation.RetryEmailExistsAsSerialUpdateResult.Failures).ToList();
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
        /// </summary>
        /// <param name="failures">Failures recorded while syncing MP contact data to HubSpot.</param>
        private bool HubSpotIssuesEncountered(IReadOnlyCollection<IFailureDetails> failures)
        {
            if (failures == null || failures.Count == 0) return false;

            var httpStatusCodes = new HashSet<HttpStatusCode>(failures.Select(f => f.HttpStatusCode));
            var errors = new HashSet<string>(failures.Select(GetErrors).SelectMany(error => error)); // flatten errors & keep only distinct versions
            return httpStatusCodes.Contains(HttpStatusCode.InternalServerError) ||  // error on the HubSpot server
                    httpStatusCodes.Contains(HttpStatusCode.Unauthorized) ||         // issue with our HubSpot API key
                    errors.Contains(HubSpotPropertyDoesNotExistSearchString) ||      // HubSpot property doesn't exist (possible user error)
                    errors.Contains(HubSpotPropertyInvalidOptionSearchString);       // Value passed to a static list HubSpot property is invalid (possible user error)
        }

        private IEnumerable<string> GetErrors(IFailureDetails failure)
        {
            return failure?.Exception?.ValidationResults?.Select(result => result?.Error) ?? Enumerable.Empty<string>();
        }
    }
}
