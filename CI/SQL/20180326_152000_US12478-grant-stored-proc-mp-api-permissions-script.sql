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
        @Description = 'Fetches Ministry Platform core contact data updates to be synced to the Crossroads HubSpot CRM instance.';

    exec dbo.crds_grant_mp_api_stored_procedure_execution_permission
        @StoredProcedureName = 'api_crds_calculate_and_persist_current_child_age_and_grade_counts_by_household_for_hubspot',
        @RoleName = 'unauthenticatedCreate',
        @Description = 'Determines how many children in a series of 20 different age and grade ranges are members of a given household in the Ministry Platform CRM, stores the initial results and subsequent deltas in dbo.cr_ChildAgeAndGradeCountsPerHousehold, ultimately to be transferred to HubSpot as attributes of registered, active contacts belonging to the same household.';

    exec dbo.crds_grant_mp_api_stored_procedure_execution_permission
        @StoredProcedureName = 'api_crds_get_child_age_and_grade_counts_for_hubspot',
        @RoleName = 'unauthenticatedCreate',
        @Description = 'Fetches a list detailing the number of children within 20 different age and grade ranges (infancy -> graduated high school seniors) by household for each registered, active contact within that household. This fetched, contact attribute data will be synced to HubSpot and be used to determine which contacts will receive HubSpot-powered Kids Club and Student Ministry email notifications.';

    exec dbo.crds_grant_mp_api_stored_procedure_execution_permission
        @StoredProcedureName = 'api_crds_set_child_age_and_grade_delta_log_sync_date',
        @RoleName = 'unauthenticatedCreate',
        @Description = 'Puts a bow on the age and grade counts sync process by setting the dbo.cr_ChildAgeAndGradeDeltaLog.SyncCompletedUtc when the HubSpot sync process completes successfully.';

end