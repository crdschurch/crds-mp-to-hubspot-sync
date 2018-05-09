use MinistryPlatform
go

-- drop procedure dbo.api_crds_get_child_age_and_grade_counts_for_hubspot;

create or alter procedure dbo.api_crds_get_child_age_and_grade_counts_for_hubspot
    @LastModifiedUtc datetime
as

    declare @NoKiddosRegisteredHash binary(20) = hashbytes('SHA1', '00000000000000000000'); -- represents the hash equating to zero children in a given household

    select          Contacts.Contact_ID as MinistryPlatformContactId,
                    dp_Users.[User_Name] as Email,
                    KidsClubStudentMinistryCounts.Number_of_infants,
                    KidsClubStudentMinistryCounts.Number_of_1_Year_Olds,
                    KidsClubStudentMinistryCounts.Number_of_2_Year_Olds,
                    KidsClubStudentMinistryCounts.Number_of_3_Year_Olds, 
                    KidsClubStudentMinistryCounts.Number_of_4_Year_Olds,
                    KidsClubStudentMinistryCounts.Number_of_5_Year_Olds,
                    KidsClubStudentMinistryCounts.Number_of_Kindergartners,
                    KidsClubStudentMinistryCounts.Number_of_1st_Graders, 
                    KidsClubStudentMinistryCounts.Number_of_2nd_Graders,
                    KidsClubStudentMinistryCounts.Number_of_3rd_Graders,
                    KidsClubStudentMinistryCounts.Number_of_4th_Graders,
                    KidsClubStudentMinistryCounts.Number_of_5th_Graders, 
                    KidsClubStudentMinistryCounts.Number_of_6th_Graders,
                    KidsClubStudentMinistryCounts.Number_of_7th_Graders,
                    KidsClubStudentMinistryCounts.Number_of_8th_Graders,
                    KidsClubStudentMinistryCounts.Number_of_9th_Graders, 
                    KidsClubStudentMinistryCounts.Number_of_10th_Graders,
                    KidsClubStudentMinistryCounts.Number_of_11th_Graders,
                    KidsClubStudentMinistryCounts.Number_of_12th_Graders, 
                    KidsClubStudentMinistryCounts.Number_of_Graduated_Seniors

    --              Attach kiddo info to any contact we want in HubSpot (could even be the minor themselves, if they're registered)
    from            dbo.cr_ChildAgeAndGradeCountsByHousehold KidsClubStudentMinistryCounts
    join            dbo.Contacts on Contacts.Household_ID = KidsClubStudentMinistryCounts.HouseholdId
    join            dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID

    --              Active, registered contacts over 12 years old (if we have an age) whose email address hasn't been blanked out
    where           (Contacts.__Age > 12 or Contacts.__Age is null)
    and             Contacts.Email_Address is not null
    and             Contacts.Email_Address <> ''
    and             Contacts.Contact_Status_ID = 1              -- active (2 = inactive, 3 = deceased) -> dbo.Contact_Statuses

    --              Only the counts that have changed most recently and are NOT zeros across the child-age-range-board
    and             KidsClubStudentMinistryCounts.LastModifiedUtc = @LastModifiedUtc
    and             KidsClubStudentMinistryCounts.HouseHoldId not in                   -- exclude all "never registered any kids" records
                    (
                        select          HouseholdId
                        from            dbo.cr_ChildAgeAndGradeCountsByHousehold
                        where           CreatedUtc = LastModifiedUtc
                        and             Hash = @NoKiddosRegisteredHash
                    );