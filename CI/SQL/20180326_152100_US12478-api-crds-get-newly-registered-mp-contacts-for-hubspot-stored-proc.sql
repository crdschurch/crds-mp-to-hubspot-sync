use [MinistryPlatform]
go

-- gets a list of contacts that were created after a given date (presumably, new registrations)
create or alter procedure dbo.api_crds_get_newly_registered_mp_contacts_for_hubspot
	@LastSuccessfulSyncDate datetime
as

    select              Contacts.Contact_ID as MinistryPlatformContactId,
                        Contacts.Nickname as Firstname,
                        Contacts.Last_Name as Lastname,
                        Contacts.Email_Address as Email,
                        Congregations.Congregation_Name as Community,
                        dp_Audit_Log.Date_Time as 'Creation Date_Time',
                        Contacts.__Age as 'Age'

    from                Contacts
    join                dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
    join                dp_Audit_Log on dp_Audit_Log.Record_ID = dp_Users.User_ID
    left join           Households on Households.Household_ID = Contacts.Household_ID
    left join           Congregations on Congregations.Congregation_ID = Households.Congregation_ID

    where               (Contacts.__Age > 12 OR Contacts.__Age is null)
    and                 dp_Audit_Log.Audit_Description = 'Created'
    and                 dp_Audit_Log.Table_Name = 'dp_Users'
    and                 dp_Audit_Log.Date_Time > @LastSuccessfulSyncDate

    group by            Contacts.Contact_ID,
                        Contacts.Nickname,
                        Contacts.Last_Name,
                        Contacts.Email_Address,
                        Congregations.Congregation_Name,
                        dp_Audit_Log.Date_Time,
                        Contacts.__Age;
