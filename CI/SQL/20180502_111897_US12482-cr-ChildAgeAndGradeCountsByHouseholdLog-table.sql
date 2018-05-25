use MinistryPlatform
go

-- drop table cr_ChildAgeAndGradeDeltaLog
if not exists (select 1 from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = 'dbo' and TABLE_NAME = 'cr_ChildAgeAndGradeDeltaLog')
begin

    create table dbo.cr_ChildAgeAndGradeDeltaLog
    (
        ProcessedUtc datetime primary key not null,
        CreatedUtc datetime default(getutcdate()) not null,
        InsertCount int not null,
        UpdateCount int not null,
        SyncCompletedUtc datetime null,
        LastModifiedUtc datetime null
    )

    create nonclustered index IDX_cr_ChildAgeAndGradeDeltaLog_SyncCompletedUtc on dbo.cr_ChildAgeAndGradeDeltaLog (SyncCompletedUtc);

    create nonclustered index IDX_cr_ChildAgeAndGradeDeltaLog_CreatedUtc on dbo.cr_ChildAgeAndGradeDeltaLog (CreatedUtc);

    create nonclustered index IDX_cr_ChildAgeAndGradeDeltaLog_LastModifiedUtc on dbo.cr_ChildAgeAndGradeDeltaLog (LastModifiedUtc);

end