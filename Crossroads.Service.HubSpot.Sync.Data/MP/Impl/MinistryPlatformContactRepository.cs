using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using Crossroads.Web.Common.MinistryPlatform;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Service.HubSpot.Sync.Data.MP.Impl
{
    public class MinistryPlatformContactRepository : IMinistryPlatformContactRepository
    {
        private readonly IMinistryPlatformRestRequestBuilderFactory _mpRestBuilder;
        private readonly IApiUserRepository _apiUserRepository;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<MinistryPlatformContactRepository> _logger;

        public MinistryPlatformContactRepository(
            IMinistryPlatformRestRequestBuilderFactory mpRestBuilder,
            IApiUserRepository apiUserRepository,
            IJsonSerializer jsonSerializer,
            ILogger<MinistryPlatformContactRepository> logger)
        {
            _mpRestBuilder = mpRestBuilder ?? throw new ArgumentNullException(nameof(mpRestBuilder));
            _apiUserRepository = apiUserRepository ?? throw new ArgumentNullException(nameof(apiUserRepository));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ChildAgeAndGradeDeltaLogDto CalculateAndPersistKidsClubAndStudentMinistryAgeAndGradeDeltas()
        {
            const string storedProcedureName = "api_crds_calculate_and_persist_current_child_age_and_grade_counts_by_household_for_hubspot";
            _logger.LogInformation($"sproc: {storedProcedureName}");

            try
            {
                var result = FetchData(storedProcedureName)?.Select(jObject => _jsonSerializer.ToObject<ChildAgeAndGradeDeltaLogDto>(jObject)).First();
                Log(result);

                return result;
            }
            catch (Exception exc)
            {
                _logger.LogError("Exception occurred while updating age and grade group data.", exc);
                throw;
            }
        }

        public IList<AgeAndGradeGroupCountsForMpContactDto> GetAgeAndGradeGroupDataForContacts()
        {
            const string storedProcedureName = "api_crds_get_child_age_and_grade_counts_for_hubspot";
            _logger.LogInformation($"sproc: {storedProcedureName}");

            try
            {
                var updates = FetchData(storedProcedureName)?.Select(jObject => _jsonSerializer.ToObject<AgeAndGradeGroupCountsForMpContactDto>(jObject)).ToList()
                              ?? Enumerable.Empty<AgeAndGradeGroupCountsForMpContactDto>().ToList();

                _logger.LogInformation($"Number of age and group updates fetched from MP: {updates.Count}");

                return updates;
            }
            catch (Exception exc)
            {
                _logger.LogError("An exception occurred while fetching Ministry Platform age & grade updates.", exc);
                throw;
            }
        }

        public DateTime SetChildAgeAndGradeDeltaLogSyncCompletedUtcDate()
        {
            const string storedProcedureName = "api_crds_set_child_age_and_grade_delta_log_sync_date";
            _logger.LogInformation($"sproc: {storedProcedureName}");

            try
            {
                var result = FetchData(storedProcedureName)?.Select(jObject => _jsonSerializer.ToObject<ChildAgeAndGradeDeltaLogDto>(jObject)).First();
                Log(result);

                return result.SyncCompletedUtc.Value; // prefer the exception as this *should* not happen
            }
            catch (Exception exc)
            {
                _logger.LogError("An exception occurred while setting the age & grade sync completed date in Ministry Platform.", exc);
                throw;
            }
        }

        public IList<NewlyRegisteredMpContactDto> GetNewlyRegisteredContacts(DateTime lastSuccessfulSyncDateUtc)
        {
            const string storedProcedureName = "api_crds_get_newly_registered_mp_contacts_for_hubspot";
            var lastSuccessfulSyncDateLocal = lastSuccessfulSyncDateUtc.ToLocalTime();
            Log(storedProcedureName, lastSuccessfulSyncDateLocal, "Fetching newly registered contacts from MP via stored proc.");

            try
            {
                var parameters = new Dictionary<string, object> { { "@LastSuccessfulSyncDate", lastSuccessfulSyncDateLocal } };
                var data = FetchData(storedProcedureName, parameters);
                var contacts = data?.Select(jObject => _jsonSerializer.ToObject<NewlyRegisteredMpContactDto>(jObject)).ToList()
                               ?? Enumerable.Empty<NewlyRegisteredMpContactDto>().ToList();

                _logger.LogInformation($"Number of newly registered MP contacts fetched: {contacts.Count}");

                return contacts;
            }
            catch(Exception exc)
            {
                _logger.LogError("An exception occurred while fetching newly registered Ministry Platform contacts.", exc);
                throw;
            }
        }

        public IDictionary<string, List<CoreUpdateMpContactDto>> GetAuditedContactUpdates(DateTime lastSuccessfulSyncDateUtc)
        {
            const string storedProcedureName = "api_crds_get_mp_contact_updates_for_hubspot";
            var lastSuccessfulSyncDateLocal = lastSuccessfulSyncDateUtc.ToLocalTime();
            Log(storedProcedureName, lastSuccessfulSyncDateLocal, "Fetching MP contacts with recently updated data via stored proc.");

            try
            {
                //
                var parameters = new Dictionary<string, object> { { "@LastSuccessfulSyncDateLocal", lastSuccessfulSyncDateLocal } };
                var data = FetchData(storedProcedureName, parameters);
                var columnUpdates = data?.Select(jObject => _jsonSerializer.ToObject<CoreUpdateMpContactDto>(jObject)).ToList()
                                    ?? Enumerable.Empty<CoreUpdateMpContactDto>().ToList();

                var contactColumnUpdates = columnUpdates.GroupBy(key => key.MinistryPlatformContactId, value => value)
                                                        .ToDictionary(keySelector => keySelector.Key, values => values.ToList());

                _logger.LogInformation($"Number of column updates fetched from MP: {columnUpdates.Count}");
                _logger.LogInformation($"Number of contacts to update in HubSpot: {contactColumnUpdates.Count}");

                return contactColumnUpdates;
            }
            catch(Exception exc)
            {
                _logger.LogError("An exception occurred while fetching Ministry Platform core contact updates.", exc);
                throw;
            }
        }

        private List<JObject> FetchData(string storedProcedureName, Dictionary<string, object> storedProcedureParameters = null)
        {
            var token = _apiUserRepository.GetDefaultApiClientToken(); // dp_Audit_Logs.Date_Time stores Utc
            return _mpRestBuilder.NewRequestBuilder()
                .WithAuthenticationToken(token)
                .Build()
                .ExecuteStoredProc<JObject>(storedProcedureName, storedProcedureParameters ?? new Dictionary<string, object>())
                .FirstOrDefault(); // unwraps/accommodates SQL Server's ability return multiple result sets in a single query represented as a list of lists
        }

        private void Log(string storedProcedureName, DateTime lastSuccessfulSyncDate, string logMessage)
        {
            _logger.LogInformation($@"{logMessage}
sproc: {storedProcedureName}
last successful sync date: {lastSuccessfulSyncDate}");
        }

        private void Log(ChildAgeAndGradeDeltaLogDto result)
        {
            _logger.LogInformation($@"
ProcessedUtc: {result?.ProcessedUtc}
SyncCompletedUtc: {result?.SyncCompletedUtc}
Inserts: {result?.InsertCount}
Updates: {result?.UpdateCount}");
        }
    }
}