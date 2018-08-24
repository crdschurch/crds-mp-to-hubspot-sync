using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CleanUpSyncActivity : ICleanUpSyncActivity
    {
        public void CleanUp(ISyncActivity activity)
        {
            if (activity == null) return;
            RemoveContactsFromFailures(activity);
        }

        private void RemoveContactsFromFailures(ISyncActivity activity)
        {
            activity.NewRegistrationOperation.SerialCreateResult.Failures.ForEach(NullFailedContact);
            activity.NewRegistrationOperation.SerialUpdateResult.Failures.ForEach(NullFailedContact);

            activity.CoreUpdateOperation.SerialUpdateResult.Failures.ForEach(NullFailedContact);
            activity.CoreUpdateOperation.SerialCreateResult.Failures.ForEach(NullFailedContact);
            activity.CoreUpdateOperation.SerialReconciliationResult.Failures.ForEach(NullFailedContact);

            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult100.FailedBatches.ForEach(NullFailedContacts);
            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult10.FailedBatches.ForEach(NullFailedContacts);
            activity.ChildAgeAndGradeUpdateOperation.RetryBulkUpdateAsSerialUpdateResult.Failures.ForEach(NullFailedContact);
        }

        private void NullFailedContacts(BulkSyncFailure failure)
        {
            failure.Contacts = null;
        }

        private void NullFailedContact(SerialSyncFailure failure)
        {
            failure.Contact = null;
        }
    }
}