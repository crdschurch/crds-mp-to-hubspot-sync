-- For an understanding of why...
-- PreviousValue <> NewValue
-- ... is now ...
-- isnull(PreviousValue, '') <> NewValue
-- ...consult the following url: https://docs.microsoft.com/en-us/sql/t-sql/statements/set-ansi-nulls-transact-sql?view=sql-server-2017
use MinistryPlatform
go

-- gets a list of contact data updated after a given date
create or alter procedure dbo.api_crds_get_mp_contact_updates_for_hubspot
    @LastSuccessfulSyncDateLocal datetime
as

    -- each common table expression query definition's audit log data is grouped by the Ministry Platform database table of origin:
    -- dp_Users, Contacts, Households and Addresses
    with EmailAddressAuditLog as (
        select          MostRecentFieldChanges.RecordId as UserId,
                        'email' as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        InitialEmailChangeAuditLog.PreviousValue,
                        LatestEmailChangeAuditLog.NewValue

        from            (
                            select          MostRecentEmailChange.RecordId,
                                            InitialEmail.Updated as InitialEmailChangeDate,
                                            MostRecentEmailChange.Updated as LatestEmailChangeDate,
                                            MostRecentEmailChange.FieldName,
                                            MostRecentEmailChange.TableName

                            from            dbo.crds_get_initial_field_changes('dp_Users', @LastSuccessfulSyncDateLocal) InitialEmail
                            join            dbo.crds_get_most_recent_field_changes('dp_Users', @LastSuccessfulSyncDateLocal) MostRecentEmailChange
                            on              InitialEmail.RecordId = MostRecentEmailChange.RecordId
                            where           MostRecentEmailChange.FieldName = 'User_Name'
                            and             InitialEmail.FieldName = 'User_Name'
                        ) MostRecentFieldChanges

        join            dbo.vw_crds_audit_log InitialEmailChangeAuditLog
        on              InitialEmailChangeAuditLog.RecordId = MostRecentFieldChanges.RecordId
        and             InitialEmailChangeAuditLog.OperationDateTime = MostRecentFieldChanges.InitialEmailChangeDate
        and             InitialEmailChangeAuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             InitialEmailChangeAuditLog.TableName = MostRecentFieldChanges.TableName

        join            dbo.vw_crds_audit_log LatestEmailChangeAuditLog
        on              LatestEmailChangeAuditLog.RecordId = MostRecentFieldChanges.RecordId
        and             LatestEmailChangeAuditLog.OperationDateTime = MostRecentFieldChanges.LatestEmailChangeDate
        and             LatestEmailChangeAuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             LatestEmailChangeAuditLog.TableName = MostRecentFieldChanges.TableName

        where           LatestEmailChangeAuditLog.NewValue is not null
        and             LatestEmailChangeAuditLog.NewValue <> ''
        and             lower(isnull(InitialEmailChangeAuditLog.PreviousValue, '')) <> lower(LatestEmailChangeAuditLog.NewValue) -- we only want email addresses that have actually changed and case differences do not qualify
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
                            from            dbo.crds_get_most_recent_field_changes('Contacts', @LastSuccessfulSyncDateLocal)
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
        join            (select * from dbo.crds_get_most_recent_field_changes('Households', @LastSuccessfulSyncDateLocal) where FieldName in ('Congregation_ID', 'Home_Phone')) MostRecentFieldChanges
        on              AuditLog.RecordId = MostRecentFieldChanges.RecordId
        and             AuditLog.OperationDateTime = MostRecentFieldChanges.Updated
        and             AuditLog.FieldName = MostRecentFieldChanges.FieldName
        and             AuditLog.TableName = MostRecentFieldChanges.TableName
        where           AuditLog.NewValue is not null
        and             AuditLog.NewValue <> ''
        and             isnull(AuditLog.PreviousValue, '') <> AuditLog.NewValue
    ),
    HouseholdAddressAuditLog as (
        select          Households.Household_ID as HouseholdId,
                        'zip' as PropertyName, -- the value of the "PropertyName" column corresponds to the "property name" used in HubSpot (passed along in the HS API payload)
                        AuditLog.PreviousValue,
                        AuditLog.NewValue

        from            dbo.vw_crds_audit_log AuditLog
        join            (select * from dbo.crds_get_most_recent_field_changes('Addresses', @LastSuccessfulSyncDateLocal) where FieldName = 'Postal_Code') MostRecentFieldChanges
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
        select          MinistryPlatformContactId,
                        UserId,
                        HouseholdId,
                        Email,
                        Firstname,
                        Lastname,
                        Community,
                        Marital_Status,
                        Gender,
                        Phone,
                        MobilePhone,
                        Zip,
                        Number_of_Infants,
                        Number_of_1_Year_Olds,
                        Number_of_2_Year_Olds,
                        Number_of_3_Year_Olds,
                        Number_of_4_Year_Olds,
                        Number_of_5_Year_Olds,
                        Number_of_Kindergartners,
                        Number_of_1st_Graders,
                        Number_of_2nd_Graders,
                        Number_of_3rd_Graders,
                        Number_of_4th_Graders,
                        Number_of_5th_Graders,
                        Number_of_6th_Graders,
                        Number_of_7th_Graders,
                        Number_of_8th_Graders,
                        Number_of_9th_Graders,
                        Number_of_10th_Graders,
                        Number_of_11th_Graders,
                        Number_of_12th_Graders,
                        Number_of_Graduated_Seniors

        from            dbo.vw_crds_hubspot_users
    )

    --              email address legitimately (not casing, etc) changed
    select          EmailAddressAuditLog.PropertyName,
                    EmailAddressAuditLog.PreviousValue,
                    EmailAddressAuditLog.NewValue,
                    RelevantContacts.*

    from            EmailAddressAuditLog
    join            RelevantContacts
    on              RelevantContacts.UserId = EmailAddressAuditLog.UserId

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
