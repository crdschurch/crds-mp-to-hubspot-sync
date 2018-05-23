use MinistryPlatform
go

-- drop procedure dbo.api_crds_get_child_age_and_grade_counts_for_hubspot;

create or alter procedure dbo.api_crds_get_child_age_and_grade_counts_for_hubspot
as

    declare @NoKiddosRegisteredHash binary(20) = hashbytes('SHA1', '00000000000000000000'), -- represents the hash equating to zero children in a given household
            @ProcessedUtc datetime = (select max(ProcessedUtc) from dbo.cr_ChildAgeAndGradeDeltaLog where SyncCompletedUtc is null);

    select          HubSpotUsers.MinistryPlatformContactId,
                    HubSpotUsers.Email,
                    KidsClubStudentMinistryCounts.Number_of_Infants,
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

    --              Attach kiddo info to any contact we want in HubSpot (could even be the minor themselves, accommodates HOH kiddo)
    from            dbo.cr_ChildAgeAndGradeCountsByHousehold as KidsClubStudentMinistryCounts
    --              inner and NOT left joining here explicitly to indicate we want all age/grade count changes that are associated with contacts that ought to already be in HubSpot; left joining elsewhere b/c we want the contacts, even if the counts don't exist
    join            dbo.vw_crds_hubspot_users as HubSpotUsers on HubSpotUsers.HouseholdId = KidsClubStudentMinistryCounts.HouseholdId

    --              Only the counts that have changed most recently and are NOT zeros across the child-age-range-board
    where           KidsClubStudentMinistryCounts.LastModifiedUtc = @ProcessedUtc
    and             KidsClubStudentMinistryCounts.HouseHoldId not in                   -- exclude all "never registered any kids" records
                    (
                        select          HouseholdId
                        from            dbo.cr_ChildAgeAndGradeCountsByHousehold
                        where           CreatedUtc = LastModifiedUtc
                        and             Hash = @NoKiddosRegisteredHash
                    );