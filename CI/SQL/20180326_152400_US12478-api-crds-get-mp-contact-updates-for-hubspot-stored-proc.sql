use [MinistryPlatform]
go

-- gets a list of contact data updated after a given date
create or alter procedure dbo.api_crds_get_mp_contact_updates_for_hubspot
    @LastSuccessfulSyncDateUtc datetime
as

    with ContactAuditLog as (
        select          MostRecentFieldChanges.ContactId,
                        case MostRecentFieldChanges.FieldName
                            when 'First_Name' then 'firstname'
                            when 'Last_Name' then 'lastname'
                            when 'Email_Address' then 'email'
                        end as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (   -- in the event multiple changes were made to a field between updates, we'll be diligent to grab only the last change
                            select          RecordId as ContactId, 
                                            FieldName,
                                            max(OperationDateTime) as Updated

                            from            dbo.vw_crds_audit_log
                            where           OperationDateTime > @LastSuccessfulSyncDateUtc
                            and             FieldName in ('First_Name', 'Last_Name', 'Email_Address')
                            and             TableName = 'Contacts'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            and             NewValue is not null
                            and             NewValue <> ''
                            and             PreviousValue <> NewValue
                            group by        RecordId,
                                            FieldName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.ContactId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
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
                                            max(OperationDateTime) as Updated

                            from            dbo.vw_crds_audit_log
                            where           OperationDateTime > @LastSuccessfulSyncDateUtc
                            and             FieldName = 'Congregation_ID'
                            and             TableName = 'Households'
                            and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
                            and             NewValue is not null
                            and             NewValue <> ''
                            and             PreviousValue <> NewValue
                            group by        RecordId,
                                            FieldName
                        ) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.HouseholdId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
    ),
    RelevantContacts as ( -- contacts of age (if we know their age), with logins (they've registered).
        select              Contacts.Contact_ID as MinistryPlatformContactId,
                            Households.Household_ID as HouseholdId,
                            dp_Users.User_Email as Email,
                            Contacts.First_Name as Firstname,
                            Contacts.Last_Name as Lastname,
                            Congregations.Congregation_Name as Community

        from                dbo.Contacts
        join                dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
        left join           Households on HouseHolds.Household_ID = Contacts.Household_ID
        left join           Congregations on Congregations.Congregation_ID = Households.Congregation_ID
        where               (Contacts.__Age > 12 or Contacts.__Age is null)
        and                 dp_Users.User_Email is not null
    )

    select          RelevantContacts.MinistryPlatformContactId,
                    ContactAuditLog.PropertyName,
                    ContactAuditLog.PreviousValue,
                    ContactAuditLog.NewValue,
                    RelevantContacts.Firstname,
                    RelevantContacts.Lastname,
                    RelevantContacts.Email,
                    RelevantContacts.Community

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
                    RelevantContacts.Community

    from            HouseholdAuditLog
    join            RelevantContacts
    on              RelevantContacts.HouseholdId = HouseholdAuditLog.HouseholdId;
