use [MinistryPlatform]
go

begin
	-- add the proc and permissions to the mp db
	if not exists(select 1 from dbo.dp_API_Procedures where [Procedure_Name] = 'api_crds_get_mp_contact_updates_for_hubspot')
	begin
		declare @API_Procedure_ID int,
				@Role_ID int,
				@Domain_ID int = 1; -- only 1 domain: select * from dp_Domains;

		-- adds proc
		insert into dbo.dp_API_Procedures ([Procedure_Name], [Description])
		values(
            'api_crds_get_mp_contact_updates_for_hubspot',
			'Fetches Ministry Platform contact data updates to be synced to the Crossroads HubSpot CRM instance.'
		);
		set @API_Procedure_ID = SCOPE_IDENTITY();
		set @Role_ID = (select top 1 Role_ID from dbo.dp_Roles where Role_Name = 'unauthenticatedCreate'); -- selecting for readability/clarity

		if not exists(select 1 from dbo.dp_Role_API_Procedures where API_Procedure_ID = @API_Procedure_ID and Role_ID = @Role_ID)
		begin
			-- adds permission
			insert into		dbo.dp_Role_API_Procedures (Role_ID, API_Procedure_ID, Domain_ID)
			values			(@Role_ID, @API_Procedure_ID, @Domain_ID);
		end
	end
end