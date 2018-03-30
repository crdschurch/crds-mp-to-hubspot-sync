use [MinistryPlatform]
go

-- gets a list of contacts that were created after a given date (presumably, new registrations)
create or alter procedure api_crds_get_newly_registered_mp_contacts_for_hubspot
	@LastSuccessfulSyncDate date
as

select			Contacts.Contact_ID as MinistryPlatformContactId,
				Contacts.Nickname as Firstname,
				Contacts.Last_Name as Lastname,
				Contacts.Email_Address as Email,
				Congregations.Congregation_Name as Community,
				Participants.Participant_Start_Date as Created

from			Contacts
join			Participants
on				Contacts.Contact_ID = Participants.Contact_ID
join			Households
on				Households.Household_ID = Contacts.Household_ID
left join		Congregations
on				Congregations.Congregation_ID = Households.Congregation_ID
where			(Contacts.__Age > 12 OR Contacts.__Age is null)
and				Contacts.Email_Address is not null
and				Participants.Participant_Start_Date > @LastSuccessfulSyncDate;