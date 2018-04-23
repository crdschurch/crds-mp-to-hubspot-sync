use [MinistryPlatform]
go

-- gets a list of contacts that were created after a given date (presumably, new registrations)
create or alter procedure dbo.api_crds_get_newly_registered_mp_contacts_for_hubspot
    @LastSuccessfulSyncDate datetime
as

    select              Contacts.Contact_ID as MinistryPlatformContactId,
                        Contacts.Nickname as Firstname,
                        Contacts.Last_Name as Lastname,
                        dp_Users.User_Email as Email,
                        isnull(Congregations.Congregation_Name, '') as Community,
                        isnull(Marital_Statuses.Marital_Status, '') as MaritalStatus -- passing empty along to translate to 'Unspecified' in HubSpot

    from                dbo.Contacts
    join                dbo.Participants on Contacts.Contact_ID = Participants.Contact_ID
    join                dbo.dp_Users on dp_Users.Contact_ID = Contacts.Contact_ID
    left join           dbo.Households on Households.Household_ID = Contacts.Household_ID
    left join           dbo.Congregations on Congregations.Congregation_ID = Households.Congregation_ID
    left join           dbo.Marital_Statuses on Marital_Statuses.Marital_Status_ID = Contacts.Marital_Status_ID
    where               (Contacts.__Age > 12 or Contacts.__Age is null)
    and                 dp_Users.User_Email is not null
    and                 Participants.Participant_Start_Date > @LastSuccessfulSyncDate
