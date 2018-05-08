use MinistryPlatform
go

-- drop procedure dbo.api_crds_calculate_and_persist_current_child_age_and_grade_counts_by_household_for_hubspot;

create or alter procedure dbo.api_crds_calculate_and_persist_current_child_age_and_grade_counts_by_household_for_hubspot
    @LastModifiedUtc datetime
as
    declare @InsertCount int = 0,
            @UpdateCount int = 0;
    declare @ChildAgeAndGradeCountsByHousehold table
            (
	            Hash binary(20) not null,
                CreatedUtc datetime not null,
	            LastModifiedUtc datetime not null,
	            HouseholdId int primary key not null,
	            Number_of_Infants int not null,
	            Number_of_1_Year_Olds int not null,
	            Number_of_2_Year_Olds int not null,
	            Number_of_3_Year_Olds int not null,
	            Number_of_4_Year_Olds int not null,
	            Number_of_5_Year_Olds int not null,
	            Number_of_Kindergartners int not null,
	            Number_of_1st_Graders int not null,
	            Number_of_2nd_Graders int not null,
	            Number_of_3rd_Graders int not null,
	            Number_of_4th_Graders int not null,
	            Number_of_5th_Graders int not null,
	            Number_of_6th_Graders int not null,
	            Number_of_7th_Graders int not null,
	            Number_of_8th_Graders int not null,
	            Number_of_9th_Graders int not null,
	            Number_of_10th_Graders int not null,
	            Number_of_11th_Graders int not null,
	            Number_of_12th_Graders int not null,
	            Number_of_Graduated_Seniors int not null
            );

    -- "CHILD OF AGE" AND "CHILD IN GRADE" DATA
    -- AGE DATA: infant up to 5 years old
    -- GRADE DATA: Kindergarten up to graduated senior
    -- 20 data points/columns total
    insert into @ChildAgeAndGradeCountsByHousehold
    select              hashbytes(
                            'SHA1',
                            concat(
                                Number_of_infants, '',
                                Number_of_1_Year_Olds, '',
                                Number_of_2_Year_Olds, '',
                                Number_of_3_Year_Olds, '', 
                                Number_of_4_Year_Olds, '',
                                Number_of_5_Year_Olds, '',
                                Number_of_Kindergartners, '',
                                Number_of_1st_Graders, '', 
                                Number_of_2nd_Graders, '',
                                Number_of_3rd_Graders, '',
                                Number_of_4th_Graders, '',
                                Number_of_5th_Graders, '', 
                                Number_of_6th_Graders, '',
                                Number_of_7th_Graders, '',
                                Number_of_8th_Graders, '',
                                Number_of_9th_Graders, '', 
                                Number_of_10th_Graders, '',
                                Number_of_11th_Graders, '',
                                Number_of_12th_Graders, '', 
                                Number_of_Graduated_Seniors
                            )
                        ) as Hash,
                        @LastModifiedUtc as Created,
                        @LastModifiedUtc as LastModified,
                        counts.*
    from
    (
        select              Household_ID as HouseholdId,
                            (select count(1) from dbo.Contacts where Contacts.__Age < 1 and Contacts.Contact_Status_ID = 1 and Contacts.Household_ID = Households.Household_ID and not exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id in (173939, 173938, 173937, 173936, 173935, 173934, 173933, 173932, 173931, 173930, 173929, 173928, 173927))) as Number_of_infants,
                            (select count(1) from dbo.Contacts where Contacts.__Age = 1 and Contacts.Contact_Status_ID = 1 and Contacts.Household_ID = Households.Household_ID and not exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id in (173939, 173938, 173937, 173936, 173935, 173934, 173933, 173932, 173931, 173930, 173929, 173928, 173927))) as Number_of_1_Year_Olds,
                            (select count(1) from dbo.Contacts where Contacts.__Age = 2 and Contacts.Contact_Status_ID = 1 and Contacts.Household_ID = Households.Household_ID and not exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id in (173939, 173938, 173937, 173936, 173935, 173934, 173933, 173932, 173931, 173930, 173929, 173928, 173927))) as Number_of_2_Year_Olds,
                            (select count(1) from dbo.Contacts where Contacts.__Age = 3 and Contacts.Contact_Status_ID = 1 and Contacts.Household_ID = Households.Household_ID and not exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id in (173939, 173938, 173937, 173936, 173935, 173934, 173933, 173932, 173931, 173930, 173929, 173928, 173927))) as Number_of_3_Year_Olds,
                            (select count(1) from dbo.Contacts where Contacts.__Age = 4 and Contacts.Contact_Status_ID = 1 and Contacts.Household_ID = Households.Household_ID and not exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id in (173939, 173938, 173937, 173936, 173935, 173934, 173933, 173932, 173931, 173930, 173929, 173928, 173927))) as Number_of_4_Year_Olds,
                            (select count(1) from dbo.Contacts where Contacts.__Age = 5 and Contacts.Contact_Status_ID = 1 and Contacts.Household_ID = Households.Household_ID and not exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id in (173939, 173938, 173937, 173936, 173935, 173934, 173933, 173932, 173931, 173930, 173929, 173928, 173927))) as Number_of_5_Year_Olds,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173939 and End_Date is null)) as Number_of_Kindergartners,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173938 and End_Date is null)) as Number_of_1st_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173937 and End_Date is null)) as Number_of_2nd_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173936 and End_Date is null)) as Number_of_3rd_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173935 and End_Date is null)) as Number_of_4th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173934 and End_Date is null)) as Number_of_5th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173933 and End_Date is null)) as Number_of_6th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173932 and End_Date is null)) as Number_of_7th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173931 and End_Date is null)) as Number_of_8th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173930 and End_Date is null)) as Number_of_9th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173929 and End_Date is null)) as Number_of_10th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173928 and End_Date is null)) as Number_of_11th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 173927 and End_Date is null)) as Number_of_12th_Graders,
                            (select count(1) from dbo.Contacts where Contacts.Household_ID = Households.Household_ID and Contacts.Contact_Status_ID = 1 and exists (select 1 from group_participants where participant_id = Contacts.participant_record and group_id = 179944 and End_Date is null)) as Number_of_Graduated_Seniors
        from                dbo.Households
        group by            Household_ID
    ) counts;

    begin
        -- only update records that have changed
        update              destination
        set                 destination.Hash = source.Hash,
                            destination.LastModifiedUtc = source.LastModifiedUtc,
                            destination.Number_of_infants = source.Number_of_infants,
                            destination.Number_of_1_Year_Olds = source.Number_of_1_Year_Olds,
                            destination.Number_of_2_Year_Olds = source.Number_of_2_Year_Olds,
                            destination.Number_of_3_Year_Olds = source.Number_of_3_Year_Olds, 
                            destination.Number_of_4_Year_Olds = source.Number_of_4_Year_Olds,
                            destination.Number_of_5_Year_Olds = source.Number_of_5_Year_Olds,
                            destination.Number_of_Kindergartners = source.Number_of_Kindergartners,
                            destination.Number_of_1st_Graders = source.Number_of_1st_Graders, 
                            destination.Number_of_2nd_Graders = source.Number_of_2nd_Graders,
                            destination.Number_of_3rd_Graders = source.Number_of_3rd_Graders,
                            destination.Number_of_4th_Graders = source.Number_of_4th_Graders,
                            destination.Number_of_5th_Graders = source.Number_of_5th_Graders, 
                            destination.Number_of_6th_Graders = source.Number_of_6th_Graders,
                            destination.Number_of_7th_Graders = source.Number_of_7th_Graders,
                            destination.Number_of_8th_Graders = source.Number_of_8th_Graders,
                            destination.Number_of_9th_Graders = source.Number_of_9th_Graders, 
                            destination.Number_of_10th_Graders = source.Number_of_10th_Graders,
                            destination.Number_of_11th_Graders = source.Number_of_11th_Graders,
                            destination.Number_of_12th_Graders = source.Number_of_12th_Graders, 
                            destination.Number_of_Graduated_Seniors = source.Number_of_Graduated_Seniors

        from                dbo.cr_ChildAgeAndGradeCountsByHousehold destination
        join                @ChildAgeAndGradeCountsByHousehold source
        on                  destination.HouseholdId = source.HouseholdId
        where               destination.Hash <> source.Hash;

        set @UpdateCount = @@rowcount;

        -- add newly created households
        insert into         dbo.cr_ChildAgeAndGradeCountsByHousehold
        select              *
        from                @ChildAgeAndGradeCountsByHousehold
        where               HouseholdId not in (select HouseHoldId from dbo.cr_ChildAgeAndGradeCountsByHousehold);

        set @InsertCount = @@rowcount;
    end

    select @LastModifiedUtc as LastModifiedUtc, @InsertCount as InsertCount, @UpdateCount as UpdateCount;
