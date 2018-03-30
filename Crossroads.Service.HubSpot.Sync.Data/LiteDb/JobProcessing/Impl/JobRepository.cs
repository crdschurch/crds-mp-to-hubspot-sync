using System;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Crossroads.Service.HubSpot.Sync.LiteDB;

namespace Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Impl
{
    public class JobRepository : IJobRepository
    {
        private readonly ILiteDbRepository _liteDbRepository;

        public JobRepository(ILiteDbRepository liteDbRepository)
        {
            _liteDbRepository = liteDbRepository ?? throw new ArgumentNullException(nameof(liteDbRepository));
        }

        public bool SetLastSuccessfulSyncDate(DateTime dateTime)
        {
            return _liteDbRepository.Upsert(new LastSuccessfulSyncDateDto {Value = dateTime});
        }

        public bool SetJobProcessingState(JobProcessingState jobProcessingState)
        {
            return _liteDbRepository.Upsert(new JobProcessingStatusDto { Value = jobProcessingState });
        }

        public bool StoreJobActivity(JobActivityDto activityDto)
        {
            return _liteDbRepository.Upsert(activityDto);
        }
    }
}
