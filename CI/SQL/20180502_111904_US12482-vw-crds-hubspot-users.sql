use MinistryPlatform
go

-- base query for all active, registered users that fit the criteria for being synced from MP to HubSpot
create or alter view dbo.vw_crds_hubspot_users
as

        -- all of the core data points we want to capture in HubSpot
        select          Contacts.Contact_ID as MinistryPlatformContactId,
                        Participants.Participant_Start_Date as ParticipantStartDate,
                        dp_Users.[User_ID] as UserId,
                        Contacts.Household_ID as HouseholdId,
                        dp_Users.[User_Name] as Email,
                        Contacts.Nickname as Firstname,
                        Contacts.Last_Name as Lastname,
                        isnull(Congregations.Congregation_Name, '') as Community,
                        isnull(Marital_Statuses.Marital_Status, '') as Marital_Status,
                        isnull(Genders.Gender, '') as Gender,
                        isnull(HouseHolds.Home_Phone, '') as Phone,         -- HS internal id (lower case)
                        isnull(Contacts.Mobile_Phone, '') as MobilePhone,   -- HS internal id (lower case)
                        isnull(Addresses.Postal_Code, '') as Zip,           -- HS internal id (lower case)
                        isnull(KidsClubStudentMinistryCounts.Number_of_Infants, 0) as Number_of_Infants,
                        isnull(KidsClubStudentMinistryCounts.Number_of_1_Year_Olds, 0) as Number_of_1_Year_Olds,
                        isnull(KidsClubStudentMinistryCounts.Number_of_2_Year_Olds, 0) as Number_of_2_Year_Olds,
                        isnull(KidsClubStudentMinistryCounts.Number_of_3_Year_Olds, 0) as Number_of_3_Year_Olds,
                        isnull(KidsClubStudentMinistryCounts.Number_of_4_Year_Olds, 0) as Number_of_4_Year_Olds,
                        isnull(KidsClubStudentMinistryCounts.Number_of_5_Year_Olds, 0) as Number_of_5_Year_Olds,
                        isnull(KidsClubStudentMinistryCounts.Number_of_Kindergartners, 0) as Number_of_Kindergartners,
                        isnull(KidsClubStudentMinistryCounts.Number_of_1st_Graders, 0) as Number_of_1st_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_2nd_Graders, 0) as Number_of_2nd_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_3rd_Graders, 0) as Number_of_3rd_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_4th_Graders, 0) as Number_of_4th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_5th_Graders, 0) as Number_of_5th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_6th_Graders, 0) as Number_of_6th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_7th_Graders, 0) as Number_of_7th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_8th_Graders, 0) as Number_of_8th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_9th_Graders, 0) as Number_of_9th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_10th_Graders, 0) as Number_of_10th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_11th_Graders, 0) as Number_of_11th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_12th_Graders, 0) as Number_of_12th_Graders,
                        isnull(KidsClubStudentMinistryCounts.Number_of_Graduated_Seniors, 0) as Number_of_Graduated_Seniors

        from            dbo.Contacts
        join            dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
        join            dbo.Participants on Contacts.Contact_ID = Participants.Contact_ID
        left join       dbo.Households on HouseHolds.Household_ID = Contacts.Household_ID
        left join       dbo.Congregations on Congregations.Congregation_ID = Households.Congregation_ID
        left join       dbo.Marital_Statuses on Marital_Statuses.Marital_Status_ID = Contacts.Marital_Status_ID
        left join       dbo.Genders on Genders.Gender_ID = Contacts.Gender_ID
        left join       dbo.Addresses on Addresses.Address_ID = Households.Address_ID
        left join       dbo.cr_ChildAgeAndGradeCountsByHousehold KidsClubStudentMinistryCounts on KidsClubStudentMinistryCounts.HouseHoldId = Contacts.HouseHold_ID

        --              Active, registered contacts over 12 years old (if we have an age) whose dbo.Contacts.Email_Address hasn't been blanked out
        where           (Contacts.__Age > 12 or Contacts.__Age is null)
        and             Contacts.Email_Address is not null
        and             Contacts.Email_Address <> ''
        and             Contacts.Contact_Status_ID = 1; -- active (2 = inactive, 3 = deceased) -> dbo.Contact_Statuses

        -- significant where clause criteria, b/c a contact with a user record but an empty
        -- Contacts.Email_Address indicates it has hard bounced and should not be used, meaning
        -- they're still allowed to log in, but sending them over to HubSpot for bulk email
        -- marketing purposes would be a mistake