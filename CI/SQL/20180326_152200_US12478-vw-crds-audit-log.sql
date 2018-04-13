use [MinistryPlatform]
go

-- knits the 2 Audit tables together; i like this, but think we should seriously consider
-- creating a materialized view against this view, so as to not bog down this highly
-- transactional set of tables with reads (assuming reads are implicitly doing "Read Commited" by default)
create or alter view dbo.vw_crds_audit_log
as

    select          dp_Audit_Log.Audit_Item_ID as AuditItemId,
                    dp_Audit_Log.Table_Name as TableName,
                    dp_Audit_Log.Record_ID as RecordId,                     -- unique identifier 
                    dp_Audit_Log.Audit_Description as AuditDescription,
                    dp_Audit_Log.User_Name as UserName,                     -- person who changed it?
                    dp_Audit_Log.User_ID as UserId,                         -- don't think this corresponds to dp_Users.UserId
                    dp_Audit_Log.Date_Time as OperationDateTime,
                    dp_Audit_Detail.Field_Name as FieldName,
                    dp_Audit_Detail.Field_Label as FieldLabel,
                    dp_Audit_Detail.Previous_Value as PreviousValue,
                    dp_Audit_Detail.New_Value as NewValue,
                    dp_Audit_Detail.Previous_ID as PreviousId,
                    dp_Audit_Detail.New_ID as [NewId]

    from            dbo.dp_Audit_Log
    join            dbo.dp_Audit_Detail
    on              dbo.dp_Audit_Detail.Audit_Item_ID = dbo.dp_Audit_Log.Audit_Item_ID;
