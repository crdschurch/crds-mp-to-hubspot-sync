use MinistryPlatform
go

-- drop procedure dbo.api_crds_get_child_age_and_grade_counts_for_hubspot;

create or alter procedure dbo.api_crds_get_child_age_and_grade_counts_for_hubspot
as

    declare @NoKiddosRegisteredHash binary(20) = hashbytes('SHA1', '00000000000000000000'), -- represents the hash equating to zero children in a given household
            @ProcessedUtc datetime = (select max(ProcessedUtc) from dbo.cr_ChildAgeAndGradeDeltaLog where SyncCompletedUtc is null);

    select          Contacts.Contact_ID as MinistryPlatformContactId,
                    dp_Users.[User_Name] as Email,
                    KidsClubStudentMinistryCounts.*

    --              Attach kiddo info to any contact we want in HubSpot (could even be the minor themselves, accommodates HOH kiddo)
    from            dbo.crds_get_child_age_and_grade_counts() as KidsClubStudentMinistryCounts
    join            dbo.Contacts on Contacts.Household_ID = KidsClubStudentMinistryCounts.HouseholdId
    join            dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
    join            dbo.Participants on Contacts.Contact_ID = Participants.Contact_ID

    --              Active, registered contacts over 12 years old (if we have an age) whose dbo.Contacts.Email_Address hasn't been blanked out
    where           (Contacts.__Age > 12 or Contacts.__Age is null)
    and             Contacts.Email_Address is not null
    and             Contacts.Email_Address <> ''
    and             Contacts.Contact_Status_ID = 1              -- active (2 = inactive, 3 = deceased) -> dbo.Contact_Statuses

    --              Only the counts that have changed most recently and are NOT zeros across the child-age-range-board
    and             KidsClubStudentMinistryCounts.LastModifiedUtc = @ProcessedUtc
    and             KidsClubStudentMinistryCounts.HouseHoldId not in                   -- exclude all "never registered any kids" records
                    (
                        select          HouseholdId
                        from            dbo.cr_ChildAgeAndGradeCountsByHousehold
                        where           CreatedUtc = LastModifiedUtc
                        and             Hash = @NoKiddosRegisteredHash
                    );