using System.Net.Http;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities
{
    /// <summary>
    /// Posts data to the provided endpoint.
    /// </summary>
    public interface IHttpPost
    {
        /// <summary>
        /// Posts the specified DTO to the provided endpoint.
        /// </summary>
        /// <typeparam name="TDto">Type to post.</typeparam>
        /// <param name="requestUriPathAndQuery">Endpoint to which we will post.</param>
        /// <param name="postBody">Instance of type <see cref="TDto"> to post</see>/></param>
        /// <returns>Returns the http response message.</returns>
        HttpResponseMessage Post<TDto>(string requestUriPathAndQuery, TDto postBody);

        /// <summary>
        /// Attempts to ask for, receive and deserialize the response content stream to the
        /// specified type.
        /// </summary>
        TDto GetResponseContent<TDto>(HttpResponseMessage httpResponseMessage);
    }
}