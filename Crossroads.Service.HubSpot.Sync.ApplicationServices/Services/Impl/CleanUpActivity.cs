using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl
{
    public class CleanUpActivity : ICleanUpActivity
    {
        public void CleanUp(IActivity activity)
        {
            if (activity == null) return;
            RemoveContactsFromFailures(activity);
            RemoveContactsFromConflictReconciliationLists(activity);
        }

        private void RemoveContactsFromFailures(IActivity activity)
        {
            activity.NewRegistrationSyncOperation.SerialCreateResult.Failures.ForEach(NullFailedContact);
            activity.NewRegistrationSyncOperation.SerialUpdateResult.Failures.ForEach(NullFailedContact);

            activity.CoreContactAttributeSyncOperation.SerialUpdateResult.Failures.ForEach(NullFailedContact);
            activity.CoreContactAttributeSyncOperation.SerialCreateResult.Failures.ForEach(NullFailedContact);
            activity.CoreContactAttributeSyncOperation.SerialReconciliationResult.Failures.ForEach(NullFailedContact);

            activity.ChildAgeAndGradeSyncOperation.BulkUpdateSyncResult1000.FailedBatches.ForEach(NullFailedContacts);
            activity.ChildAgeAndGradeSyncOperation.BulkUpdateSyncResult100.FailedBatches.ForEach(NullFailedContacts);
            activity.ChildAgeAndGradeSyncOperation.BulkUpdateSyncResult10.FailedBatches.ForEach(NullFailedContacts);
            activity.ChildAgeAndGradeSyncOperation.RetryBulkUpdateAsSerialUpdateResult.Failures.ForEach(NullFailedContact);
        }

        private void RemoveContactsFromConflictReconciliationLists(IActivity activity)
        {
            activity.NewRegistrationSyncOperation.SerialCreateResult.EmailAddressesAlreadyExist.Clear();
            activity.NewRegistrationSyncOperation.SerialUpdateResult.EmailAddressesAlreadyExist.Clear();
            activity.NewRegistrationSyncOperation.SerialUpdateResult.EmailAddressesDoNotExist.Clear();

            activity.CoreContactAttributeSyncOperation.SerialUpdateResult.EmailAddressesAlreadyExist.Clear();
            activity.CoreContactAttributeSyncOperation.SerialUpdateResult.EmailAddressesDoNotExist.Clear();
            activity.CoreContactAttributeSyncOperation.SerialCreateResult.EmailAddressesAlreadyExist.Clear();

            activity.ChildAgeAndGradeSyncOperation.RetryBulkUpdateAsSerialUpdateResult.EmailAddressesDoNotExist.Clear();
        }

        private void NullFailedContacts(BulkSyncFailure failure)
        {
            failure.HubSpotContacts = null;
        }

        private void NullFailedContact(SerialSyncFailure failure)
        {
            failure.HubSpotContact = null;
        }
    }
}