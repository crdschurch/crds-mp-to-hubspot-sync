using System;
using Crossroads.Service.HubSpot.Sync.Core.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Crossroads.Service.HubSpot.Sync.Core.Serialization.Impl
{
    public class JsonSerializer : IJsonSerializer
    {
        private readonly ILogger<JsonSerializer> _logger;

        public JsonSerializer(ILogger<JsonSerializer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Json node selector from which to start parsing.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="serializedInput">String representation of the target type.</param>
        /// <param name="selector">Json root node selector from which to start parsing.
        /// This is necessary on the occasion when a root node is specified in the payload
        /// for an individual object.</param>
        public T Deserialize<T>(string serializedInput, string selector = null)
        {
            using (_logger.BeginScope(CoreEvent.Serialization.Deserialize))
            {
                if (serializedInput.IsNullOrEmpty())
                    return default(T);
                try
                {
                    _logger.LogDebug($@"Begin deserialization...
json: {serializedInput}");
                    if (selector.IsNotNullOrEmpty())
                    {
                        _logger.LogDebug($"Using provided JSON root node selector: {selector}.");
                        return JObject.Parse(serializedInput).SelectToken(selector).ToObject<T>();
                    }

                    return JsonConvert.DeserializeObject<T>(serializedInput);
                }
                catch (Exception exc)
                {
                    _logger.LogError(CoreEvent.Exception, exc, "An error occurred during deserialization.");
                    throw;
                }
                finally
                {
                    _logger.LogDebug("Deserialization complete.");
                }
            }
        }

        /// <summary>
        /// Serializes a type (simple or complex) to a JSON string.
        /// </summary>
        /// <typeparam name="T">Type to serialize.</typeparam>
        /// <param name="inputToBeSerialized">Instance to serialize.</param>
        public string Serialize<T>(T inputToBeSerialized)
        {
            using (_logger.BeginScope(CoreEvent.Serialization.Serialize))
            {
                try
                {
                    _logger.LogDebug("Begin serialization...");
                    return JsonConvert.SerializeObject(inputToBeSerialized);
                }
                catch (Exception exc)
                {
                    _logger.LogError(CoreEvent.Exception, exc, "An error occurred during serialization.");
                    throw;
                }
                finally
                {
                    _logger.LogDebug("Serialization complete.");
                }
            }
        }
    }
}