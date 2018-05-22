-- For an understanding of why...
-- PreviousValue <> NewValue
-- ... is now ...
-- isnull(PreviousValue, '') <> NewValue
-- ...consult the following url: https://docs.microsoft.com/en-us/sql/t-sql/statements/set-ansi-nulls-transact-sql?view=sql-server-2017
use MinistryPlatform
go

-- gets a list of contact data updated after a given date
create or alter procedure dbo.api_crds_get_mp_contact_updates_for_hubspot
    @LastSuccessfulSyncDateUtc datetime
as

    -- each common table expression query definition's audit log data is grouped by the Ministry Platform database table of origin:
    -- dp_Users, Contacts, Households and Addresses
    with UserAuditLog as (
        select          MostRecentFieldChanges.RecordId as UserId,
                        'email' as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (   -- in the event multiple changes were made to a field between updates, we'll be diligent to grab only the last change
                            select          *
                            from            dbo.get_most_recent_field_changes('dp_Users', @LastSuccessfulSyncDateUtc)
                            where           FieldName = 'User_Name'
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.RecordId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        where           AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             lower(isnull(AuditLog.PreviousValue, '')) <> lower(AuditLog.NewValue) -- we only want email addresses that have actually changed and case differences do not qualify
    ),
    ContactAuditLog as (
        select          MostRecentFieldChanges.RecordId as ContactId,
                        case MostRecentFieldChanges.FieldName
                            when 'Nickname' then 'firstname'
                            when 'Last_Name' then 'lastname'
                            when 'Marital_Status_ID' then 'marital_status'
                            when 'Gender_ID' then 'gender'
                            when 'Mobile_Phone' then 'mobilephone'
                        end as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (   -- in the event multiple changes were made to a field between updates, we'll be diligent to grab only the last change
                            select          *
                            from            dbo.get_most_recent_field_changes('Contacts', @LastSuccessfulSyncDateUtc)
                            where           FieldName in ('Nickname', 'Last_Name', 'Marital_Status_ID', 'Gender_ID', 'Mobile_Phone')
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.RecordId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        where           AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             isnull(AuditLog.PreviousValue, '') <> AuditLog.NewValue
    ),
    HouseholdAuditLog as (
        select          MostRecentFieldChanges.RecordId as HouseholdId,
                        case MostRecentFieldChanges.FieldName
                            when 'Congregation_ID' then 'community'
                            when 'Home_Phone' then 'phone'
                        end as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (
                            select          *
                            from            dbo.get_most_recent_field_changes('Households', @LastSuccessfulSyncDateUtc)
                            where           FieldName in ('Congregation_ID', 'Home_Phone')
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.RecordId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        and             AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             isnull(AuditLog.PreviousValue, '') <> AuditLog.NewValue
    ),
    HouseholdAddressAuditLog as (
        select          Households.Household_ID as HouseholdId,
                        'zip' as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (   -- in the event multiple changes were made to a field between updates, we'll be diligent to grab only the last change
                            select          *
                            from            dbo.get_most_recent_field_changes('Addresses', @LastSuccessfulSyncDateUtc)
                            where           FieldName = 'Postal_Code'
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.RecordId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        join            dbo.Households on Households.Address_ID = MostRecentFieldChanges.RecordId
        where           AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             lower(isnull(AuditLog.PreviousValue, '')) <> lower(AuditLog.NewValue) -- we only want email addresses that have actually changed and case differences do not qualify
    ),
    RelevantContacts as ( -- contacts of age (if we know their age), with logins (they've registered).
        select          Contacts.Contact_ID as MinistryPlatformContactId,
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
        left join       dbo.crds_get_child_age_and_grade_counts() as KidsClubStudentMinistryCounts on KidsClubStudentMinistryCounts.HouseholdId = Contacts.Household_ID

        --              Active, registered contacts over 12 years old (if we have an age) whose dbo.Contacts.Email_Address hasn't been blanked out
        where           (Contacts.__Age > 12 or Contacts.__Age is null)
        and             Contacts.Email_Address is not null
        and             Contacts.Email_Address <> ''
        and             Contacts.Contact_Status_ID = 1 -- active (2 = inactive, 3 = deceased) -> dbo.Contact_Statuses

        -- significant where clause criteria, b/c a contact with a user record but an empty
        -- Contacts.Email_Address indicates it has hard bounced and should not be used, meaning
        -- they're still allowed to log in, but sending them over to HubSpot for bulk email
        -- marketing purposes would be a mistake
    )

    --              email address legitimately (not casing, etc) changed
    select          UserAuditLog.PropertyName,
                    UserAuditLog.PreviousValue,
                    UserAuditLog.NewValue,
                    RelevantContacts.*

    from            UserAuditLog
    join            RelevantContacts
    on              RelevantContacts.UserId = UserAuditLog.UserId

    union

    --              nick name, last name, marital status, gender changed
    select          ContactAuditLog.PropertyName,
                    ContactAuditLog.PreviousValue,
                    ContactAuditLog.NewValue,
                    RelevantContacts.*

    from            ContactAuditLog
    join            RelevantContacts
    on              RelevantContacts.MinistryPlatformContactId = ContactAuditLog.ContactId

    union

    --              community/congregation changed
    select          HouseholdAuditLog.PropertyName,
                    HouseholdAuditLog.PreviousValue,
                    HouseholdAuditLog.NewValue,
                    RelevantContacts.*

    from            HouseholdAuditLog
    join            RelevantContacts
    on              RelevantContacts.HouseholdId = HouseholdAuditLog.HouseholdId
    
    union

    --              Household address zip code changed
    select          HouseholdAddressAuditLog.PropertyName,
                    HouseholdAddressAuditLog.PreviousValue,
                    HouseholdAddressAuditLog.NewValue,
                    RelevantContacts.*

    from            HouseholdAddressAuditLog
    join            RelevantContacts
    on              RelevantContacts.HouseholdId = HouseholdAddressAuditLog.HouseholdId;