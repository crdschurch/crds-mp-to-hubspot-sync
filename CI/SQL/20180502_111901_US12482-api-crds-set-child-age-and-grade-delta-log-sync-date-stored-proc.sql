use MinistryPlatform
go

-- drop procedure dbo.api_crds_set_child_age_and_grade_delta_log_sync_date;

create or alter procedure dbo.api_crds_set_child_age_and_grade_delta_log_sync_date
as

    if exists (select 1 from dbo.cr_ChildAgeAndGradeDeltaLog where ProcessedUtc = (select max(ProcessedUtc) from dbo.cr_ChildAgeAndGradeDeltaLog where SyncCompletedUtc is null))
    begin

        declare @SyncCompletedUtc datetime = getutcdate();

        update      cr_ChildAgeAndGradeDeltaLog
        set         SyncCompletedUtc = @SyncCompletedUtc,
                    LastModifiedUtc = @SyncCompletedUtc

        from        dbo.cr_ChildAgeAndGradeDeltaLog
        where       ProcessedUtc = (select max(ProcessedUtc) from dbo.cr_ChildAgeAndGradeDeltaLog where SyncCompletedUtc is null);

        select ProcessedUtc, SyncCompletedUtc, InsertCount, UpdateCount from dbo.cr_ChildAgeAndGradeDeltaLog where SyncCompletedUtc = @SyncCompletedUtc;

    end

    select ProcessedUtc, SyncCompletedUtc, InsertCount, UpdateCount from dbo.cr_ChildAgeAndGradeDeltaLog where ProcessedUtc = (select max(ProcessedUtc) from dbo.cr_ChildAgeAndGradeDeltaLog);
