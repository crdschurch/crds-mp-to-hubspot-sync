use [MinistryPlatform]
go

-- gets a list of contacts that were created after a given date (presumably, new registrations)
create or alter procedure dbo.api_crds_get_newly_registered_mp_contacts_for_hubspot
    @LastSuccessfulSyncDate datetime
as

    select              Contacts.Contact_ID as MinistryPlatformContactId,
                        Contacts.Nickname as Firstname,
                        Contacts.Last_Name as Lastname,
                        dp_Users.[User_Name] as Email, -- switching to Contacts.Email_Address, b/c it is synced over to dp_Users.User_Name, making it the source of truth
                        isnull(Congregations.Congregation_Name, '') as Community,
                        isnull(Marital_Statuses.Marital_Status, '') as MaritalStatus,
                        isnull(Genders.Gender, '') as Gender

    from                dbo.Contacts
    join                dbo.Participants on Contacts.Contact_ID = Participants.Contact_ID
    join                dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
    left join           dbo.Households on Households.Household_ID = Contacts.Household_ID
    left join           dbo.Congregations on Congregations.Congregation_ID = Households.Congregation_ID
    left join           dbo.Marital_Statuses on Marital_Statuses.Marital_Status_ID = Contacts.Marital_Status_ID
    left join           dbo.Genders on Genders.Gender_ID = Contacts.Gender_ID
    where               (Contacts.__Age > 12 or Contacts.__Age is null)
    and                 Contacts.Email_Address is not null
    and                 Contacts.Email_Address <> ''
    and                 Participants.Participant_Start_Date > @LastSuccessfulSyncDate;

    -- significant where clause criteria, b/c a contact with a user record but an empty
    -- Contacts.Email_Address means it has hard bounced and should not be used, meaning
    -- they're still allowed to log in, but sending them over to HubSpot for bulk email
    -- marketing purposes would be a mistake