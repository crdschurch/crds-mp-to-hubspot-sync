use [MinistryPlatform]
go

-- add stored procedure execution permissions to the mp db

begin

    exec dbo.crds_grant_mp_api_stored_procedure_execution_permission
        @StoredProcedureName = 'api_crds_get_newly_registered_mp_contacts_for_hubspot',
        @RoleName = 'unauthenticatedCreate',
        @Description = 'Fetches newly registered Ministry Platform contact data to be synced to the Crossroads HubSpot CRM instance.';

    exec dbo.crds_grant_mp_api_stored_procedure_execution_permission
        @StoredProcedureName = 'api_crds_get_mp_contact_updates_for_hubspot',
        @RoleName = 'unauthenticatedCreate',
        @Description = 'Fetches Ministry Platform contact data updates to be synced to the Crossroads HubSpot CRM instance.';

end