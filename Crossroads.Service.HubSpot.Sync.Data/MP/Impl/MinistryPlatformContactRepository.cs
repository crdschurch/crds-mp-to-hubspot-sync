using System;
using System.Collections.Generic;
using System.Linq;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using Crossroads.Web.Common.MinistryPlatform;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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

        public IList<NewlyRegisteredMpContactDto> GetNewlyRegisteredContacts(DateTime lastSuccessfulSyncDateUtc)
        {
            const string storedProcName = "api_crds_get_newly_registered_mp_contacts_for_hubspot";
            var lastSuccessfulSyncDateLocal = lastSuccessfulSyncDateUtc.ToLocalTime();
            Log(storedProcName, lastSuccessfulSyncDateLocal, "Fetching newly registered contacts from MP via stored proc.");

            var token = _apiUserRepository.GetDefaultApiUserToken(); // dbo.Participants.Participant_Start_Date stores "local" datetime
            var parameters = new Dictionary<string, object> { { "@LastSuccessfulSyncDateLocal", lastSuccessfulSyncDateLocal } };
            var data = _mpRestBuilder.NewRequestBuilder()
                .WithAuthenticationToken(token)
                .Build()
                .ExecuteStoredProc<JObject>(storedProcName, parameters)
                .FirstOrDefault(); // unwraps/accommodates SQL Server's ability return multiple result sets in a single query...
                                   // ...represented as a list of lists

            var contacts = data?.Select(jObject => _jsonSerializer.ToObject<NewlyRegisteredMpContactDto>(jObject)).ToList()
                ?? Enumerable.Empty<NewlyRegisteredMpContactDto>().ToList();

            _logger.LogInformation($"Number of contacts fetched: {contacts.Count}");

            return contacts;
        }

        public IDictionary<string, List<MpContactUpdateDto>> GetContactUpdates(DateTime lastSuccessfulSyncDateUtc)
        {
            const string storedProcedureName = "api_crds_get_mp_contact_updates_for_hubspot";
            Log(storedProcedureName, lastSuccessfulSyncDateUtc, "Fetching MP contacts with recently updated data via stored proc.");

            var token = _apiUserRepository.GetDefaultApiUserToken(); // dp_Audit_Logs.Date_Time stores Utc
            var parameters = new Dictionary<string, object> { { "@LastSuccessfulSyncDateUtc", lastSuccessfulSyncDateUtc } };
            var data = _mpRestBuilder.NewRequestBuilder()
                .WithAuthenticationToken(token)
                .Build()
                .ExecuteStoredProc<JObject>(storedProcedureName, parameters)
                .FirstOrDefault(); // unwraps/accommodates SQL Server's ability return multiple result sets in a single query...
                                   // ...represented as a list of lists

            var columnUpdates = data?.Select(jObject => _jsonSerializer.ToObject<MpContactUpdateDto>(jObject)).ToList()
                          ?? Enumerable.Empty<MpContactUpdateDto>().ToList();

            _logger.LogInformation($"Number of column updates fetched: {columnUpdates.Count}");

            var contactColumnUpdates =
                columnUpdates.GroupBy(key => key.MinistryPlatformContactId, value => value)
                    .ToDictionary(keySelector => keySelector.Key, values => values.ToList());

            _logger.LogInformation($"Number of contacts updated: {contactColumnUpdates.Count}");

            return contactColumnUpdates;
        }

        private void Log(string storedProcedureName, DateTime lastSuccessfulSyncDateLocal, string logMessage)
        {
            _logger.LogInformation($@"{logMessage}
sproc: {storedProcedureName}
last successful sync date: {lastSuccessfulSyncDateLocal}");
        }
    }
}
