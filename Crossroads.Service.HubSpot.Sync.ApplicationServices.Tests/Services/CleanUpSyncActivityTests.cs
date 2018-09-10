using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services.Impl;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Test.Services
{
    public class CleanUpSyncActivityTests
    {
        private readonly CleanUpActivity _fixture = new CleanUpActivity();

        [Fact]
        public void Given_A_Null_Activity_Clean_Up_Should_Not_Throw_An_Exception()
        {
            Action action = () => _fixture.CleanUp(null);
            action.Should().NotThrow<Exception>();
        }

        [Fact]
        public void Given_An_Activity_With_Contacts_That_Failed_To_Sync_Clean_Up_Should_Null_All_Failed_Contacts()
        {
            var bulkContacts = new List<BulkHubSpotContact> {new BulkHubSpotContact {Email = "i@t.co.uk"}, new BulkHubSpotContact{Email = "y@a.net"}};
            var activity = new Activity();
            activity.NewRegistrationSyncOperation.SerialCreateResult.Failures.Add(new SerialSyncFailure {HubSpotContact = new SerialHubSpotContact { Email = "r@f.org" }});
            activity.NewRegistrationSyncOperation.SerialUpdateResult.Failures.Add(new SerialSyncFailure { HubSpotContact = new SerialHubSpotContact { Email = "s@g.org" } });

            activity.CoreContactAttributeSyncOperation.SerialUpdateResult.Failures.Add(new SerialSyncFailure { HubSpotContact = new SerialHubSpotContact { Email = "r@f.org" }});
            activity.CoreContactAttributeSyncOperation.SerialCreateResult.Failures.Add(new SerialSyncFailure { HubSpotContact = new SerialHubSpotContact { Email = "s@g.org" }});
            activity.CoreContactAttributeSyncOperation.SerialReconciliationResult.Failures.Add(new SerialSyncFailure { HubSpotContact = new SerialHubSpotContact { Email = "t@h.org" } });

            activity.ChildAgeAndGradeSyncOperation.BulkUpdateSyncResult100.FailedBatches.AddRange(new[] { new BulkSyncFailure { HubSpotContacts = bulkContacts.ToArray() } });
            activity.ChildAgeAndGradeSyncOperation.BulkUpdateSyncResult10.FailedBatches.AddRange(new[] { new BulkSyncFailure { HubSpotContacts = bulkContacts.ToArray() } });
            activity.ChildAgeAndGradeSyncOperation.RetryBulkUpdateAsSerialUpdateResult.Failures.Add(new SerialSyncFailure { HubSpotContact = new SerialHubSpotContact { Email = "r@f.org" } });

            _fixture.CleanUp(activity);

            activity.NewRegistrationSyncOperation.SerialCreateResult.Failures.ForEach(f => f.HubSpotContact.Should().BeNull());
            activity.NewRegistrationSyncOperation.SerialUpdateResult.Failures.ForEach(f => f.HubSpotContact.Should().BeNull());
            activity.CoreContactAttributeSyncOperation.SerialUpdateResult.Failures.ForEach(f => f.HubSpotContact.Should().BeNull());
            activity.CoreContactAttributeSyncOperation.SerialCreateResult.Failures.ForEach(f => f.HubSpotContact.Should().BeNull());
            activity.CoreContactAttributeSyncOperation.SerialReconciliationResult.Failures.ForEach(f => f.HubSpotContact.Should().BeNull());
            activity.ChildAgeAndGradeSyncOperation.BulkUpdateSyncResult100.FailedBatches.ForEach(batch => batch.HubSpotContacts.Should().BeNull());
            activity.ChildAgeAndGradeSyncOperation.BulkUpdateSyncResult10.FailedBatches.ForEach(batch => batch.HubSpotContacts.Should().BeNull());
            activity.ChildAgeAndGradeSyncOperation.RetryBulkUpdateAsSerialUpdateResult.Failures.ForEach(f => f.HubSpotContact.Should().BeNull());
        }
    }
}