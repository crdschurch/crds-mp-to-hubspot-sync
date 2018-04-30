-- For an understanding of why...
-- PreviousValue <> NewValue
-- ... is now ...
-- isnull(PreviousValue, '') <> NewValue
-- ...consult the following url: https://docs.microsoft.com/en-us/sql/t-sql/statements/set-ansi-nulls-transact-sql?view=sql-server-2017
use [MinistryPlatform]
go

-- gets a list of contact data updated after a given date
create or alter procedure dbo.api_crds_get_mp_contact_updates_for_hubspot
    @LastSuccessfulSyncDateUtc datetime
as

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
                            and             FieldName in ('User_Name')
                            and             TableName = 'dp_Users'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            and             NewValue is not null
                            and             NewValue <> ''
                            and             isnull(PreviousValue, '') <> NewValue
                            group by        RecordId,
                                            FieldName,
                                            TableName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.UserId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
    ),
    ContactAuditLog as (
        select          MostRecentFieldChanges.ContactId,
                        case MostRecentFieldChanges.FieldName
                            when 'Nickname' then 'firstname'
                            when 'Last_Name' then 'lastname'
                            when 'Marital_Status_ID' then 'marital_status'
                            when 'Gender_ID' then 'gender'
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
                            and             FieldName in ('Nickname', 'Last_Name', 'Marital_Status_ID', 'Gender_ID')
                            and             TableName = 'Contacts'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            and             NewValue is not null
                            and             NewValue <> ''
                            and             isnull(PreviousValue, '') <> NewValue
                            group by        RecordId,
                                            FieldName,
                                            TableName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.ContactId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
    ),
    HouseholdAuditLog as (
        select          MostRecentFieldChanges.HouseholdId,
                        'community' as PropertyName,
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
                            and             FieldName = 'Congregation_ID'
                            and             TableName = 'Households'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            and             NewValue is not null
                            and             NewValue <> ''
                            and             isnull(PreviousValue, '') <> NewValue
                            group by        RecordId,
                                            FieldName,
                                            TableName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.HouseholdId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
    ),
    RelevantContacts as ( -- contacts of age (if we know their age), with logins (they've registered).
        select          Contacts.Contact_ID as MinistryPlatformContactId,
                        dp_Users.[User_ID] as UserId,
                        Households.Household_ID as HouseholdId,
                        dp_Users.[User_Name] as Email,
                        Contacts.Nickname as Firstname,
                        Contacts.Last_Name as Lastname,
                        isnull(Congregations.Congregation_Name, '') as Community,
                        isnull(Marital_Statuses.Marital_Status, '') as MaritalStatus,
                        isnull(Genders.Gender, '') as Gender

        from            dbo.Contacts
        join            dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
        left join       dbo.Households on HouseHolds.Household_ID = Contacts.Household_ID
        left join       dbo.Congregations on Congregations.Congregation_ID = Households.Congregation_ID
        left join       dbo.Marital_Statuses on Marital_Statuses.Marital_Status_ID = Contacts.Marital_Status_ID
        left join       dbo.Genders on Genders.Gender_ID = Contacts.Gender_ID
        where           (Contacts.__Age > 12 or Contacts.__Age is null)
        and             Contacts.Email_Address is not null
        and             Contacts.Email_Address <> ''

        -- significant where clause criteria, b/c a contact with a user record but an empty
        -- Contacts.Email_Address indicates it has hard bounced and should not be used, meaning
        -- they're still allowed to log in, but sending them over to HubSpot for bulk email
        -- marketing purposes would be a mistake
    )

    select          RelevantContacts.MinistryPlatformContactId,
                    UserAuditLog.PropertyName,
                    UserAuditLog.PreviousValue,
                    UserAuditLog.NewValue,
                    RelevantContacts.Firstname,
                    RelevantContacts.Lastname,
                    RelevantContacts.Email,
                    RelevantContacts.Community,
                    RelevantContacts.MaritalStatus,
                    RelevantContacts.Gender

    from            UserAuditLog
    join            RelevantContacts
    on              RelevantContacts.UserId = UserAuditLog.UserId

    union

    select          RelevantContacts.MinistryPlatformContactId,
                    ContactAuditLog.PropertyName,
                    ContactAuditLog.PreviousValue,
                    ContactAuditLog.NewValue,
                    RelevantContacts.Firstname,
                    RelevantContacts.Lastname,
                    RelevantContacts.Email,
                    RelevantContacts.Community,
                    RelevantContacts.MaritalStatus,
                    RelevantContacts.Gender

    from            ContactAuditLog
    join            RelevantContacts
    on              RelevantContacts.MinistryPlatformContactId = ContactAuditLog.ContactId

    union

    select          RelevantContacts.MinistryPlatformContactId,
                    HouseholdAuditLog.PropertyName,
                    HouseholdAuditLog.PreviousValue,
                    HouseholdAuditLog.NewValue,
                    RelevantContacts.Firstname,
                    RelevantContacts.Lastname,
                    RelevantContacts.Email,
                    RelevantContacts.Community,
                    RelevantContacts.MaritalStatus,
                    RelevantContacts.Gender

    from            HouseholdAuditLog
    join            RelevantContacts
    on              RelevantContacts.HouseholdId = HouseholdAuditLog.HouseholdId;

