using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing
{
    /// <summary>
    /// Represents the activity for updating Kids Club and Student Ministry HubSpot properties:
    /// Number_of_Infants
    /// Number_of_1_Year_Olds
    /// Number_of_2_Year_Olds
    /// Number_of_3_Year_Olds 
    /// Number_of_4_Year_Olds
    /// Number_of_5_Year_Olds
    /// Number_of_Kindergartners
    /// Number_of_1st_Graders 
    /// Number_of_2nd_Graders
    /// Number_of_3rd_Graders
    /// Number_of_4th_Graders
    /// Number_of_5th_Graders 
    /// Number_of_6th_Graders
    /// Number_of_7th_Graders
    /// Number_of_8th_Graders
    /// Number_of_9th_Graders 
    /// Number_of_10th_Graders
    /// Number_of_11th_Graders
    /// Number_of_12th_Graders 
    /// Number_of_Graduated_Seniors
    /// 
    /// It also captures results (stats, errors, etc) around the sync operation.
    /// </summary>
    public interface IActivityChildAgeAndGradeSyncOperation
    {
        IExecutionTime Execution { get; }

        DateTime PreviousSyncDate { get; set; }

        int TotalContacts { get; }

        int SuccessCount { get; }

        int InitialSuccessCount { get; }

        int RetrySuccessCount { get; }

        int InitialFailureCount { get; }

        int RetryFailureCount { get; }

        int EmailAddressAlreadyExistsCount { get; }

        int HubSpotApiRequestCount { get; }

        ChildAgeAndGradeDeltaLogDto AgeAndGradeDelta { get; set; }

        BulkSyncResult BulkUpdateSyncResult1000 { get; set; }

        BulkSyncResult BulkUpdateSyncResult100 { get; set; }

        BulkSyncResult BulkUpdateSyncResult10 { get; set; }

        SerialSyncResult RetryBulkUpdateAsSerialUpdateResult { get; set; }

        SerialSyncResult SerialCreateResult { get; set; }
    }
}
