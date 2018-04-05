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

        public IList<NewlyRegisteredContactDto> GetNewlyRegisteredContacts(DateTime lastSuccessfulSyncDate)
        {
            const string newlyRegisteredContactsStoredProc = "api_crds_get_newly_registered_mp_contacts_for_hubspot";
            _logger.LogInformation($@"Fetching newly registered contacts from MP via stored proc.
sproc: {newlyRegisteredContactsStoredProc}
last successful sync date: {lastSuccessfulSyncDate}");

            var token = _apiUserRepository.GetDefaultApiUserToken();
            var parameters = new Dictionary<string, object> { { "@LastSuccessfulSyncDate", lastSuccessfulSyncDate } };
            var data = _mpRestBuilder.NewRequestBuilder()
                .WithAuthenticationToken(token)
                .Build()
                .ExecuteStoredProc<JObject>(newlyRegisteredContactsStoredProc, parameters)
                .FirstOrDefault(); // unwraps/accommodates SQL Server's ability return multiple result sets in a single query...
                                   // ...represented as a list of lists

            var contacts = data?.Select(jObject => _jsonSerializer.ToObject<NewlyRegisteredContactDto>(jObject)).ToList()
                ?? Enumerable.Empty<NewlyRegisteredContactDto>().ToList();

            _logger.LogInformation($"Number of contacts fetched: {contacts.Count}");

            return contacts;
        }
    }
}
