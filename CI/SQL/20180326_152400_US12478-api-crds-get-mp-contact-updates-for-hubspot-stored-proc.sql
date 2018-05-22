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
        select          MostRecentFieldChanges.UserId,
                        'email' as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (   -- in the event multiple changes were made to a field between updates, we'll be diligent to grab only the last change
                            select          RecordId as UserId, 
                                            FieldName,
                                            TableName,
                                            max(OperationDateTime) as Updated

                            from            dbo.vw_crds_audit_log
                            where           OperationDateTime > @LastSuccessfulSyncDateUtc
                            and             FieldName = 'User_Name'
                            and             TableName = 'dp_Users'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            group by        RecordId,
                                            FieldName,
                                            TableName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.UserId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        where           AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             lower(isnull(AuditLog.PreviousValue, '')) <> lower(AuditLog.NewValue) -- we only want email addresses that have actually changed and case differences do not qualify
    ),
    ContactAuditLog as (
        select          MostRecentFieldChanges.ContactId,
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
                            select          RecordId as ContactId, 
                                            FieldName,
                                            TableName,
                                            max(OperationDateTime) as Updated

                            from            dbo.vw_crds_audit_log
                            where           OperationDateTime > @LastSuccessfulSyncDateUtc
                            and             FieldName in ('Nickname', 'Last_Name', 'Marital_Status_ID', 'Gender_ID', 'Mobile_Phone')
                            and             TableName = 'Contacts'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            group by        RecordId,
                                            FieldName,
                                            TableName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.ContactId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        where           AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             isnull(AuditLog.PreviousValue, '') <> AuditLog.NewValue
    ),
    HouseholdAuditLog as (
        select          MostRecentFieldChanges.HouseholdId,
                        case MostRecentFieldChanges.FieldName
                            when 'Congregation_ID' then 'community'
                            when 'Home_Phone' then 'phone'
                        end as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (
                            select          RecordId as HouseholdId,
                                            FieldName,
                                            TableName,
                                            max(OperationDateTime) as Updated

                            from            dbo.vw_crds_audit_log
                            where           OperationDateTime > @LastSuccessfulSyncDateUtc
                            and             FieldName in ('Congregation_ID', 'Home_Phone')
                            and             TableName = 'Households'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            group by        RecordId,
                                            FieldName,
                                            TableName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.HouseholdId
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
                            select          RecordId as AddressId,
                                            FieldName,
                                            TableName,
                                            max(OperationDateTime) as Updated

                            from            dbo.vw_crds_audit_log
                            where           OperationDateTime > @LastSuccessfulSyncDateUtc
                            and             FieldName = 'Postal_Code'
                            and             TableName = 'Addresses'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            group by        RecordId,
                                            FieldName,
                                            TableName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.AddressId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        join            dbo.Households on Households.Address_ID = MostRecentFieldChanges.AddressId
        where           AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             lower(isnull(AuditLog.PreviousValue, '')) <> lower(AuditLog.NewValue) -- we only want email addresses that have actually changed and case differences do not qualify
    ),
    RelevantContacts as ( -- contacts of age (if we know their age), with logins (they've registered).
        select          Contacts.Contact_ID as MinistryPlatformContactId,
                        dp_Users.[User_ID] as UserId,
                        dp_Users.[User_Name] as Email,
                        Contacts.Nickname as Firstname,
                        Contacts.Last_Name as Lastname,
                        isnull(Congregations.Congregation_Name, '') as Community,
                        isnull(Marital_Statuses.Marital_Status, '') as Marital_Status,
                        isnull(Genders.Gender, '') as Gender,
                        isnull(HouseHolds.Home_Phone, '') as Phone,         -- HS internal id (lower case)
                        isnull(Contacts.Mobile_Phone, '') as MobilePhone,   -- HS internal id (lower case)
                        isnull(Addresses.Postal_Code, '') as Zip,           -- HS internal id (lower case)
                        KidsClubStudentMinistryCounts.*

        from            dbo.Contacts
        join            dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
        join            dbo.Participants on Contacts.Contact_ID = Participants.Contact_ID
        left join       dbo.Households on HouseHolds.Household_ID = Contacts.Household_ID
        left join       dbo.Congregations on Congregations.Congregation_ID = Households.Congregation_ID
        left join       dbo.Marital_Statuses on Marital_Statuses.Marital_Status_ID = Contacts.Marital_Status_ID
        left join       dbo.Genders on Genders.Gender_ID = Contacts.Gender_ID
        left join       dbo.Addresses on Addresses.Address_ID = Households.Address_ID
        left join       dbo.crds_get_child_age_and_grade_counts() as KidsClubStudentMinistryCounts on KidsClubStudentMinistryCounts.HouseholdId = Households.Household_ID

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