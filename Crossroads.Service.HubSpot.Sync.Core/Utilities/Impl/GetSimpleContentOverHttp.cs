using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities.Impl
{
    public class GetSimpleContentOverHttp : IGetSimpleContentOverHttp
    {
        private readonly HttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IMapper _mapper;
        private readonly ILogger<GetSimpleContentOverHttp> _logger;

        public GetSimpleContentOverHttp(HttpClient httpClient, IJsonSerializer jsonSerializer, IMapper mapper, ILogger<GetSimpleContentOverHttp> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TDto> GetContentAsync<TDto, TApiModel>(string requestUriPathAndQuery, string jsonRootNodeSelector = null)
        {
            if (requestUriPathAndQuery.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(requestUriPathAndQuery));

            var deserializedApiModel = await FetchContentAndDeserializeAsync<TApiModel>(requestUriPathAndQuery).ConfigureAwait(false);
            return Normalize<TDto, TApiModel>(deserializedApiModel);
        }

        public async Task<IEnumerable<TDto>> GetContentListAsync<TDto, TApiModel, TApiModelRoot>(
            string requestUriPathAndQuery,
            Func<TApiModelRoot, IEnumerable<TApiModel>> apiCollectionSelector)
        {
            if (requestUriPathAndQuery.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(requestUriPathAndQuery));

            var deserializedList = await FetchContentAndDeserializeAsync<TApiModelRoot>(requestUriPathAndQuery).ConfigureAwait(false);
            return NormalizeList<TDto, TApiModel>(apiCollectionSelector(deserializedList));
        }

        public async Task<string> FetchContentStringAsync(string requestUriPathAndQuery)
        {
            if (requestUriPathAndQuery.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(requestUriPathAndQuery));

            var fullUrl = $"{_httpClient.BaseAddress}{requestUriPathAndQuery}";
            using (_logger.BeginScope(CoreEvent.Http.Request))
            {
                try
                {
                    _logger.LogInformation(CoreEvent.Http.Request, $"Begin request to {fullUrl}...");
                    var response = await _httpClient.GetAsync(requestUriPathAndQuery).ConfigureAwait(false);
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (Exception exc) // API down for maintenance/unavailable?
                {
                    _logger.LogError(CoreEvent.Exception, exc,
                        $@"Exception occurred while making an API request.
url: {fullUrl}");
                    return null;
                }
            }
        }

        public TApiObject Deserialize<TApiObject>(string serializedContent, string jsonNodeSelector = null)
        {
            if (serializedContent.IsNullOrEmpty())
                return default(TApiObject);

            using (_logger.BeginScope(CoreEvent.Serialization.Deserialize))
            {
                try
                {
                    return _jsonSerializer.Deserialize<TApiObject>(serializedContent, jsonNodeSelector);
                }
                catch (Exception exc) // issue deserializing (change in API's payload?)
                {
                    _logger.LogError(
                        CoreEvent.Exception,
                        exc,
                        $@"Exception occurred while deserializing API response to API object graph.
target: {typeof(TApiObject)}
content: {serializedContent}");
                    return default(TApiObject);
                }
            }
        }

        public TDto Normalize<TDto, TApiModel>(TApiModel apiModelToBeNormalized)
        {
            if (apiModelToBeNormalized == null)
                return default(TDto);

            using (_logger.BeginScope(CoreEvent.Mapping.Map))
            {
                try
                {
                    return _mapper.Map<TDto>(apiModelToBeNormalized);
                }
                catch (Exception exc) // issue mapping (probably overkill, but it could happen)
                {
                    _logger.LogError(
                        CoreEvent.Exception,
                        exc,
                        $@"Exception occurred while mapping an API object to a normalized DTO.
source: {typeof(TApiModel)}
target: {typeof(TDto)}");

                    return default(TDto);
                }
            }
        }

        private IEnumerable<TDto> NormalizeList<TDto, TApiModel>(IEnumerable<TApiModel> listToBeNormalized)
        {
            return listToBeNormalized?.Select(Normalize<TDto, TApiModel>).Where(dto => dto != null) ?? Enumerable.Empty<TDto>();
        }

        private async Task<TApiObject> FetchContentAndDeserializeAsync<TApiObject>(string requestUriPathAndQuery)
        {
            var serializedContent = await FetchContentStringAsync(requestUriPathAndQuery).ConfigureAwait(false);
            return Deserialize<TApiObject>(serializedContent);
        }
    }
}