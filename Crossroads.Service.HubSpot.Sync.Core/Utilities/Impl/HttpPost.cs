using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities.Impl
{
    /// <summary>
    /// JSON posts data to the provided endpoint via a persistent HttpClient instance, defined at object construction.
    /// </summary>
    public class HttpPost : IHttpPost
    {
        private const string ApplicationJsonMediaType = "application/json";
        private readonly HttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<HttpPost> _logger;

        public HttpPost(HttpClient httpClient, IJsonSerializer jsonSerializer, ILogger<HttpPost> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HttpResponseMessage> PostAsync<TDto>(string requestUriPathAndQuery, TDto postBody)
        {
            var fullUrl = $"{_httpClient.BaseAddress}{requestUriPathAndQuery}";
            var json = _jsonSerializer.Serialize(postBody);

            using (_logger.BeginScope(CoreEvent.Http.Post))
            {
                _logger.LogInformation(CoreEvent.Http.Post, $"Begin post to {fullUrl}...");
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, ApplicationJsonMediaType);
                    return await _httpClient.PostAsync(requestUriPathAndQuery, content).ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    _logger.LogError(CoreEvent.Exception, exc,
$@"Exception occurred while making an API request.
url: {fullUrl}
json: {json}");
                    throw;
                }
            }
        }
    }
}