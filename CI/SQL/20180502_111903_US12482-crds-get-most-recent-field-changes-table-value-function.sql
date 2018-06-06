use MinistryPlatform
go

-- drop function dbo.crds_get_most_recent_field_changes;

create or alter function dbo.crds_get_most_recent_field_changes(@TableName varchar(50), @AfterThisOperationDateTime datetime)  
returns table
as
return

    select          RecordId,
                    FieldName,
                    TableName,
                    max(OperationDateTime) as Updated

    from            dbo.vw_crds_audit_log
    where           OperationDateTime > @AfterThisOperationDateTime
    and             TableName = @TableName
    and             AuditDescription like '%Updated'  --This will capture "Updated" and "Mass Updated"
    group by        RecordId,
                    FieldName,
                    TableName;