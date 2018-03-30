using System.Net.Http;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities
{
    /// <summary>
    /// Posts data to the provided endpoint.
    /// </summary>
    public interface IHttpPost
    {
        Task<HttpResponseMessage> PostAsync<TDto>(string requestUriPathAndQuery, TDto postBody);
    }
}