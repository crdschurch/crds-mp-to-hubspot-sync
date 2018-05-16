using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CleanUpSyncActivity : ICleanUpSyncActivity
    {
        public void CleanUp(ISyncActivity activity)
        {
            RemoveContactsFromFailures(activity);
        }

        private void RemoveContactsFromFailures(ISyncActivity activity)
        {
            activity.NewRegistrationOperation.BulkCreateSyncResult.FailedBatches.ForEach(RemoveContacts);
            activity.NewRegistrationOperation.SerialCreateSyncResult.Failures.ForEach(RemoveContact);

            activity.CoreUpdateOperation.SerialUpdateResult.Failures.ForEach(RemoveContact);
            activity.CoreUpdateOperation.RetryEmailExistsAsSerialUpdateResult.Failures.ForEach(RemoveContact);

            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult100.FailedBatches.ForEach(RemoveContacts);
            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult10.FailedBatches.ForEach(RemoveContacts);
            activity.ChildAgeAndGradeUpdateOperation.RetryBulkUpdateAsSerialUpdateResult.Failures.ForEach(RemoveContact);
        }

        private void RemoveContacts(BulkSyncFailure failure)
        {
            failure.Contacts = null;
        }

        private void RemoveContact(SerialSyncFailure failure)
        {
            failure.Contact = null;
        }
    }
}