use [MinistryPlatform]
go

-- Creates the appropriate database table records required in order to allow a stored procedure to be executed
-- via the Ministry Platform web API
-- Idempotent, so will only create if it needs to, otherwise no harm done
create or alter procedure dbo.crds_grant_mp_api_stored_procedure_execution_permission
    @StoredProcedureName nvarchar(128), -- name of the stored procedure to make accessible via the MP API web services
    @RoleName nvarchar(30),             -- name of the role allowed to execute the stored procedure (supplied on invocation of the MP web API)
    @Description nvarchar(500)          -- description of what the stored procedure's purpose/what it accomplishes
as

begin
    if (@RoleName is null)
        raiserror('A null value is not allowed for parameter "RoleName". Check dbo.dp_Roles.Role_Name for the correct role name value.', 17, 1)

    if (@StoredProcedureName is null)
        raiserror('A null value is not allowed for parameter "ProcedureName". Please specify the name of the stored procedure to be granted Ministry Platform API execution permission.', 17, 1)

    if (@Description is null)
        raiserror('A null value is not allowed for parameter "Description". Please specify the purpose of the procedure for the proliferation of domain knowledge.', 9, 1)

	-- add the proc and permissions to the mp db
	if not exists(select 1 from dbo.dp_API_Procedures where [Procedure_Name] = @StoredProcedureName)
	begin
		declare @API_Procedure_ID int,
				@Role_ID int,
				@Domain_ID int = 1; -- apparently, there's only 1 domain: select * from dp_Domains;

		-- adds proc
		insert into dbo.dp_API_Procedures ([Procedure_Name], [Description])
		values(@StoredProcedureName, @Description);
		set @API_Procedure_ID = SCOPE_IDENTITY();
		set @Role_ID = (select top 1 Role_ID from dbo.dp_Roles where Role_Name = @RoleName); -- selecting for readability/clarity

		if not exists(select 1 from dbo.dp_Role_API_Procedures where API_Procedure_ID = @API_Procedure_ID and Role_ID = @Role_ID)
		begin
			-- adds permission
			insert into		dbo.dp_Role_API_Procedures (Role_ID, API_Procedure_ID, Domain_ID)
			values			(@Role_ID, @API_Procedure_ID, @Domain_ID);
		end
	end
end