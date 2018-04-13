use [MinistryPlatform]

go

begin

    if not exists (select 1 from sys.indexes where name='IDX_dp_Audit_Detail_FieldName_AuditItemId_Previous_Value_New_Value' and object_id = object_id('dbo.dp_Audit_Detail'))
    begin

        -- creating this index per the MSSQL Execution Plan suggestion
        create nonclustered index IDX_dp_Audit_Detail_FieldName_AuditItemId_INCLUDE_Previous_Value_New_Value on dbo.dp_Audit_Detail (Field_Name, Audit_Item_ID) include (Previous_Value, New_Value);

        -- just in case we need to roll back; an index on these massive tables could potentially gum up the transactional works
        -- drop index IDX_dp_Audit_Detail_FieldName_AuditItemId_INCLUDE_Previous_Value_New_Value on dbo.dp_Audit_Detail

    end

end
