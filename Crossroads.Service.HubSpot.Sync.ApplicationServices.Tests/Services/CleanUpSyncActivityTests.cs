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
        private readonly CleanUpSyncActivity _fixture = new CleanUpSyncActivity();

        [Fact]
        public void Given_A_Null_Activity_Clean_Up_Should_Not_Throw_An_Exception()
        {
            Action action = () => _fixture.CleanUp(null);
            action.Should().NotThrow<Exception>();
        }

        [Fact]
        public void Given_An_Activity_With_Contacts_That_Failed_To_Sync_Clean_Up_Should_Null_All_Failed_Contacts()
        {
            var bulkContacts = new List<BulkContact> {new BulkContact {Email = "i@t.co.uk"}, new BulkContact{Email = "y@a.net"}};
            var activity = new SyncActivity();
            activity.NewRegistrationOperation.BulkCreateSyncResult.FailedBatches.AddRange(new[] { new BulkSyncFailure{Contacts = bulkContacts.ToArray()}});
            activity.NewRegistrationOperation.SerialCreateSyncResult.Failures.Add(new SerialSyncFailure {Contact = new SerialContact { Email = "r@f.org" }});

            activity.CoreUpdateOperation.SerialUpdateResult.Failures.Add(new SerialSyncFailure { Contact = new SerialContact { Email = "r@f.org" }});
            activity.CoreUpdateOperation.RetryEmailExistsAsSerialUpdateResult.Failures.Add(new SerialSyncFailure { Contact = new SerialContact { Email = "r@f.org" }});

            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult100.FailedBatches.AddRange(new[] { new BulkSyncFailure { Contacts = bulkContacts.ToArray() } });
            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult10.FailedBatches.AddRange(new[] { new BulkSyncFailure { Contacts = bulkContacts.ToArray() } });
            activity.ChildAgeAndGradeUpdateOperation.RetryBulkUpdateAsSerialUpdateResult.Failures.Add(new SerialSyncFailure { Contact = new SerialContact { Email = "r@f.org" } });

            _fixture.CleanUp(activity);

            activity.NewRegistrationOperation.BulkCreateSyncResult.FailedBatches.ForEach(batch => batch.Contacts.Should().BeNull());
            activity.NewRegistrationOperation.SerialCreateSyncResult.Failures.ForEach(f => f.Contact.Should().BeNull());
            activity.CoreUpdateOperation.SerialUpdateResult.Failures.ForEach(f => f.Contact.Should().BeNull());
            activity.CoreUpdateOperation.RetryEmailExistsAsSerialUpdateResult.Failures.ForEach(f => f.Contact.Should().BeNull());
            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult100.FailedBatches.ForEach(batch => batch.Contacts.Should().BeNull());
            activity.ChildAgeAndGradeUpdateOperation.BulkUpdateSyncResult10.FailedBatches.ForEach(batch => batch.Contacts.Should().BeNull());
            activity.ChildAgeAndGradeUpdateOperation.RetryBulkUpdateAsSerialUpdateResult.Failures.ForEach(f => f.Contact.Should().BeNull());
        }
    }
}