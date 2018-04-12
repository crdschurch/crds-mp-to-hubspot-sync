use [MinistryPlatform]
go

-- gets a list of contacts that were created after a given date (presumably, new registrations)
create or alter procedure dbo.api_crds_get_mp_contact_updates_for_hubspot
    @LastSuccessfulSyncDate datetime
as

    with ContactAuditLog as (
        select                  dp_Audit_Log.Record_ID as ContactId,
                                dp_Audit_Log.Audit_Item_ID as AuditItemId,
                                case dp_Audit_Detail.Field_Name
                                    when 'First_Name' then 'firstname'
                                    when 'Last_Name' then 'lastname'
                                    when 'Email_Address' then 'email'
                                end as PropertyName,
                                dp_Audit_Detail.Previous_Value as PreviousValue,
                                dp_Audit_Detail.New_Value as NewValue

        from                    dbo.dp_Audit_Log
        join                    dbo.dp_Audit_Detail
        on                      dp_Audit_Detail.Audit_Item_ID = dp_Audit_Log.Audit_Item_ID
        and                     dp_Audit_Detail.Field_Name in ('First_Name', 'Last_Name', 'Email_Address')
        where                   dp_Audit_Log.Table_Name = 'Contacts'
        and                     dp_Audit_Log.Audit_Description like '%Updated'  --This will capture "Updated" and "Mass Updated"
        and                     dp_Audit_Detail.Previous_Value <> dp_Audit_Detail.New_Value
        and                     dp_Audit_Detail.New_Value is not null
        and                     dp_Audit_Detail.New_Value <> ''
        and                     dp_Audit_Log.Date_Time > '4-10-2018' --@LastSuccessfulSyncDate
    ),
    HouseholdAuditLog as (
        select                  dp_Audit_Log.Record_ID as HouseholdId,
                                dp_Audit_Log.Audit_Item_ID as AuditItemId,
                                'community' as PropertyName,
                                dp_Audit_Detail.Previous_Value as PreviousValue,
                                dp_Audit_Detail.New_Value as NewValue

        from                    dbo.dp_Audit_Log
        join                    dbo.dp_Audit_Detail
        on                      dp_Audit_Detail.Audit_Item_ID = dp_Audit_Log.Audit_Item_ID
        and                     dp_Audit_Detail.Field_Name = 'Congregation_ID'
        where                   dp_Audit_Log.Table_Name = 'Households'
        and                     dp_Audit_Log.Audit_Description like '%Updated'  --This will capture "Updated" and "Mass Updated"
        and                     dp_Audit_Detail.Previous_Value <> dp_Audit_Detail.New_Value
        and                     dp_Audit_Detail.New_Value is not null
        and                     dp_Audit_Detail.New_Value <> ''
        and                     dp_Audit_Log.Date_Time > '4-10-2018' --@LastSuccessfulSyncDate
    ),
    RelevantContacts as ( -- contacts of age (if we know their age), with logins (they've registered).
        select              Contacts.Contact_ID as ContactId,
                            Households.Household_ID as HouseholdId

        from                dbo.Contacts
        join                dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
        join                Households on HouseHolds.Household_ID = Contacts.Household_ID
        where               (Contacts.__Age > 12 or Contacts.__Age is null)
        and                 Contacts.Email_Address is not null
    )
    select                  RelevantContacts.ContactId,
                            ContactAuditLog.PropertyName,
                            ContactAuditLog.PreviousValue,
                            ContactAuditLog.NewValue
    from                    ContactAuditLog
    join                    RelevantContacts
    on                      RelevantContacts.ContactId = ContactAuditLog.ContactId
    where                   ContactAuditLog.AuditItemId in (select max(ContactAuditLog.AuditItemId) from ContactAuditLog group by ContactAuditLog.ContactId, ContactAuditLog.PropertyName)

    union

    select                  RelevantContacts.ContactId,
                            HouseholdAuditLog.PropertyName,
                            HouseholdAuditLog.PreviousValue,
                            HouseholdAuditLog.NewValue
    from                    HouseholdAuditLog
    join                    RelevantContacts
    on                      RelevantContacts.HouseholdId = HouseholdAuditLog.HouseholdId
    where                   HouseholdAuditLog.AuditItemId in (select max(HouseholdAuditLog.AuditItemId) from HouseholdAuditLog group by HouseholdAuditLog.HouseholdId, HouseholdAuditLog.PropertyName);
