using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities
{
    /// <summary>
    /// Utility for http GETting JSON data asynchronously, deserializing and mapping it to normalized data transfer objects.
    /// <see cref="GetContentAsync{TDto, TApiModel}(string, string)"/> and
    /// <see cref="GetContentListAsync{TNormalizedDto, TApiModel, TApiRoot}(string, Func{TApiRoot, IEnumerable{TApiModel}})"/>
    /// methods are your one-stop shop for making a JSON request, deserlizing and mapping it to a normalized DTO of choice
    /// (only one mapping implementation as of this writing which relies on AutoMapper registrations for object transformation/assignment).
    /// 
    /// The individual helper methods for fetching, deserializing and normalizing JSON data are available to future devs to
    /// consume as needed.
    /// </summary>
    public interface IGetSimpleContentOverHttp
    {
        /// <summary>
        /// Requests a single piece of content, desierializes the API's response to the API's object graph and
        /// ultimatley transforms/normalizes the API graph to a "standard" data transfer object.
        /// </summary>
        /// <typeparam name="TDto">Data transfer object.</typeparam>
        /// <typeparam name="TApiModel">Native API object.</typeparam>
        /// <param name="requestUriPathAndQuery">Path and query string.</param>
        /// <param name="jsonRootNodeSelector">Json root node from which to start parsing.</param>
        Task<TDto> GetContentAsync<TDto, TApiModel>(string requestUriPathAndQuery, string jsonRootNodeSelector = null);

        /// <summary>
        /// Requests content, deserializes the API's response to the API's object graph and
        /// ultimatley transforms/normalizes the API graph to a list of "standard" data transfer objects.
        /// </summary>
        /// <typeparam name="TNormalizedDto">Data transfer object.</typeparam>
        /// <typeparam name="TApiModel">Native API object.</typeparam>
        /// <typeparam name="TApiRoot">Native API root object.</typeparam>
        /// <param name="requestUriPathAndQuery">Path and query string.</param>
        /// <param name="apiCollectionSelector">Func for surfacing the argument living inside the root object graph.</param>
        Task<IEnumerable<TNormalizedDto>> GetContentListAsync<TNormalizedDto, TApiModel, TApiRoot>(
            string requestUriPathAndQuery,
            Func<TApiRoot, IEnumerable<TApiModel>> apiCollectionSelector);

        /// <summary>
        /// Makes async http client call to retrieve data from API.
        /// </summary>
        Task<string> FetchContentStringAsync(string requestUriPathAndQuery);

        /// <summary>
        /// Deserializes API payload to its native, strongly typed object graph representation.
        /// </summary>
        TApiObject Deserialize<TApiObject>(string serializedContent, string jsonNodeSelector = null);

        /// <summary>
        /// Transforms API model to a normalized DTO in order to preserve the attributes we wish to surface to the requesting party.
        /// </summary>
        TNormalizedDto Normalize<TNormalizedDto, TApiModel>(TApiModel apiModelToBeNormalized);
    }
}