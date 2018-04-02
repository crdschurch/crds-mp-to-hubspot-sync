using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.Data.MP.Web.Common
{
    public class MinistryPlatformRestRequestAsync : IMinistryPlatformRestRequestAsync
    {
        internal string Select { get; set; }
        internal string Filter { get; set; }
        internal string OrderBy { get; set; }
        internal string GroupBy { get; set; }
        internal string Having { get; set; }
        internal int? Top { get; set; }
        internal int? Skip { get; set; }
        internal bool? Distinct { get; set; }
        internal string AuthenticationToken { get; set; }

        internal HttpClient MinistryPlatformRestClient { get; set; }
        internal HttpClient AuthorizationRestClient { get; set; }

        private const string DeleteRecordsStoredProcName = "api_crds_Delete_Table_Rows";

        internal MinistryPlatformRestRequestAsync(HttpClient ministryPlatformRestClient, HttpClient authorizationRestClient)
        {
            MinistryPlatformRestClient = ministryPlatformRestClient;
            AuthorizationRestClient = authorizationRestClient;
        }


        #region Create operations
        // --------------------------------------
        // Create
        // --------------------------------------
        public async Task<T> Create<T>(T objectToCreate, string tableName = null)
        {
            return await ExecutePutOrPost(objectToCreate, HttpMethod.Post, tableName);
        }

        public async Task<List<T>> Create<T>(List<T> objectsToCreate, string tableName = null)
        {
            return await ExecutePutOrPost(objectsToCreate, HttpMethod.Post, tableName);
        }

        #endregion

        #region Read (Get and Search) operations
        // --------------------------------------
        // Read
        // --------------------------------------
        public async Task<T> Get<T>(int id)
        {
            return await Get<T>(GetTableName<T>(), id);
        }

        public async Task<T> Get<T>(string tableName, int id)
        {
            var path = $"tables/{tableName}/{id}";
            var request = new HttpRequest(HttpMethod.Get, path);

            await ConfigureRequest(request);
            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error getting {tableName} for id {id}", true);

            var result = await response.GetContentAsync<List<T>>();
            if (result == null || !result.Any())
            {
                return default(T);
            }
            return result.First();
        }

        public async Task<List<T>> Search<T>()
        {
            return await Search<T>(GetTableName<T>());
        }

        public async Task<List<T>> Search<T>(string tableName)
        {
            var request = new HttpRequest(HttpMethod.Get, $"tables/{tableName}");
            await ConfigureRequest(request);

            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error searching {tableName}");

            var result = await response.GetContentAsync<List<T>>();
            return result;
        }
        #endregion

        #region Update operations
        // --------------------------------------
        // Update
        // --------------------------------------

        /// <summary>
        /// Updates the object in MP. The table name is optional - it will be inferred from the objectToUpdate
        /// if the appropriate attributes are set. If tableName is specified, it will be used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToUpdate"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<T> Update<T>(T objectToUpdate, string tableName = null)
        {
            return await ExecutePutOrPost(objectToUpdate, HttpMethod.Put, tableName);
        }

        /// <summary>
        /// Updates a list of objects in MP. The table name is optional - it will be inferred from the objectsToUpdate
        /// if the appropriate attributes are set. If tableName is specified, it will be used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectsToUpdate"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<List<T>> Update<T>(List<T> objectsToUpdate, string tableName = null)
        {
            return await ExecutePutOrPost(objectsToUpdate, HttpMethod.Put, tableName);
        }
        #endregion

        #region Delete operations
        // --------------------------------------
        // Delete
        // --------------------------------------
        public async Task Delete<T>(int recordId)
        {
            await Delete<T>(new[] { recordId });
        }

        public async Task Delete<T>(IEnumerable<int> recordIds)
        {
            var parms = new Dictionary<string, object>
            {
                {"TableName", GetTableName<T>()},
                {"PrimaryKeyColumnName", GetPrimaryKeyColumnName<T>()},
                {"IdentifiersToDelete", string.Join(",", recordIds)}
            };

            await ExecuteStoredProc<T>(DeleteRecordsStoredProcName, parms);
        }
        #endregion

        #region Email & Communication operations
        // --------------------------------------
        // Email
        // --------------------------------------
        public async Task<int> SendEmail(MpCommunication emailCommunication, List<MpFile> attachments = null)
        {
            var request = new HttpRequest(HttpMethod.Post, "communications");
            if (attachments == null || !attachments.Any())
            {
                request.SetJsonBody(emailCommunication);
            }
            else
            {
                var i = 0;
                attachments.ForEach(f => request.AddMultiPartFileBody($"file-{i++}", f.FileContents, f.FileName));
                request.AddMultiPartJsonBody("communication", emailCommunication);
            }
            await ConfigureRequest(request);

            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error sending email", true);

            return (int)response.StatusCode;
        }
        #endregion

        #region Stored Procedure operations
        // --------------------------------------
        // Stored Procedures
        // --------------------------------------
        public async Task<int> ExecuteStoredProc(string procedureName, Dictionary<string, object> parameters)
        {
            var request = new HttpRequest(HttpMethod.Post, $"procs/{procedureName}");
            await ConfigureRequest(request);

            request.SetJsonBody(EnsureSqlServerParameterNames(parameters));
            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error executing procedure {procedureName}", true);

            return (int)response.StatusCode;
        }

        public async Task<List<List<T>>> ExecuteStoredProc<T>(string procedureName, Dictionary<string, object> parameters)
        {
            var request = new HttpRequest(HttpMethod.Post, $"procs/{procedureName}");
            await ConfigureRequest(request);

            request.SetJsonBody(EnsureSqlServerParameterNames(parameters));
            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error executing procedure {procedureName}", true);

            var content = await response.GetContentAsync<List<List<T>>>();
            if (content == null || !content.Any())
            {
                return default(List<List<T>>);
            }
            return content;
        }

        public async Task<List<TOut>> PostWithReturn<TIn, TOut>(List<TIn> records)
        {
            var request = new HttpRequest(HttpMethod.Post, $"tables/{GetTableName<TIn>()}");
            await ConfigureRequest(request);

            request.SetJsonArrayBody(records);
            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error updating {GetTableName<TIn>()}", true);
            var content = await response.GetContentAsync<List<TOut>>();
            return content;
        }

        /// <summary>
        /// Make sure stored procedure parameters start with an '@' sign.
        /// </summary>
        /// <param name="parms">A Dictionary of parameters</param>
        /// <returns>A Dictionary of parameters, with all Keys starting with an '@' sign</returns>
        private static Dictionary<string, object> EnsureSqlServerParameterNames(Dictionary<string, object> parms)
        {
            return parms
                .Select(p => new KeyValuePair<string, object>(p.Key.StartsWith("@") ? p.Key : $"@{p.Key}", p.Value))
                .ToDictionary(x => x.Key, x => x.Value);
        }
        #endregion

        #region File operations
        public async Task<int> CreateFile<T>(int recordId, MpFile file)
        {
            return await CreateFile(GetTableName<T>(), recordId, file);
        }

        public async Task<int> CreateFile(string tableName, int recordId, MpFile file)
        {
            var request = new HttpRequest(HttpMethod.Post, $"files/{tableName}/{recordId}");
            await ConfigureRequest(request);
            request.SetFileBody("file", file.FileContents, file.FileName);

            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error creating file on [{tableName}] with record id [{recordId}]", true);

            return (int)response.StatusCode;
        }
        #endregion

        #region Table Metadata operations    
        /// <summary>
        /// Queries Ministry Platform for Table Metadata. If no table name is passed in, queries all tables 
        /// in Ministry Platform. 
        /// 
        /// The tableName needs to be named exactly to get results. Although a "*" can be used as a wild card.
        /// For example: GetTableMetaData("Contacts") will only return the Contacts table Metadata, but  GetTableMetaData("Contact*")
        /// will return table Metadata for Contacts and Contact_Relationships and Contact_Categories etc.
        /// </summary>
        /// <param name="tableName">Optional search string. Defaults to Null which will return all tables.</param>
        /// <returns>A list of tables that match the table name</returns>
        public async Task<List<TableMetaData>> GetTableMetaData(string tableName = null)
        {
            var path = $"tables";
            var request = new HttpRequest(HttpMethod.Get, path);
            if (tableName != null) request.AddQueryParameter("$search", tableName);
            await ConfigureRequest(request);
            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error searching for {tableName} metadata", true);

            var result = await response.GetContentAsync<List<TableMetaData>>();
            if (result == null || !result.Any())
            {
                return new List<TableMetaData>();
            }
            return result;
        }
        #endregion

        #region Internal methods

        private async Task<T> ExecutePutOrPost<T>(T record, HttpMethod method, string tblName = null)
        {
            var tableName = tblName ?? GetTableName<T>();
            var path = $"tables/{tableName}";
            var request = new HttpRequest(method, path).SetJsonArrayBody(record);

            await ConfigureRequest(request);

            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error {(method == HttpMethod.Put ? "updating existing" : "creating new")} {tableName}");

            var result = await response.GetContentAsync<List<T>>();
            if (result == null || !result.Any())
            {
                return default(T);
            }
            return result.First();
        }

        private async Task<List<T>> ExecutePutOrPost<T>(List<T> records, HttpMethod method, string tblName = null)
        {
            var tableName = tblName ?? GetTableName<T>();
            var path = $"tables/{tableName}";
            var request = new HttpRequest(method, path).SetJsonArrayBody(records);

            await ConfigureRequest(request);

            var response = await MinistryPlatformRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error {(method == HttpMethod.Put ? "updating existing" : "creating new")} {tableName}");

            var result = await response.GetContentAsync<List<T>>();
            if (result == null || !result.Any())
            {
                return default(List<T>);
            }
            return result;
        }

        private async Task ConfigureRequest(HttpRequest request)
        {
            request.AddQueryParameterIfSpecified("$select", Select);
            request.AddQueryParameterIfSpecified("$filter", Filter);
            request.AddQueryParameterIfSpecified("$orderBy", OrderBy);
            request.AddQueryParameterIfSpecified("$groupBy", GroupBy);
            request.AddQueryParameterIfSpecified("$having", Having);
            request.AddQueryParameterIfSpecified("$top", Top);
            request.AddQueryParameterIfSpecified("$skip", Skip);
            request.AddQueryParameterIfSpecified("$distinct", Distinct);

            if (!string.IsNullOrWhiteSpace(AuthenticationToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthenticationToken);
            }

            await AddOnBehalfOfUser(request);
        }

        private static string GetPrimaryKeyColumnName<T>()
        {
            var primaryKey = typeof(T).GetProperties().ToList().Select(p => p.GetCustomAttribute<MpRestApiPrimaryKey>()).FirstOrDefault();
            if (primaryKey == null)
            {
                throw new NoPrimaryKeyDefinitionException<T>();
            }
            return primaryKey.Name;
        }

        private static string GetTableName<T>()
        {
            var table = typeof(T).GetTypeInfo().GetCustomAttribute<MpRestApiTable>();
            if (table == null)
            {
                throw new NoTableDefinitionException<T>();
            }

            return table.Name;
        }

        /// <summary>
        /// Add a $userId query parameter for the "on behalf of" user, if necessary.  This will only be added if the request is
        /// updating data, and if we have a user auth token different than the one already on the request.
        /// </summary>
        /// <param name="request"></param>
        private async Task AddOnBehalfOfUser(HttpRequest request)
        {
            // If we don't have a User auth token, or if this request is only querying data, we don't need to add the $userId
            if (!UserAuthorizationTokenHolder.IsSet() || !(request.Method == HttpMethod.Delete || request.Method == HttpMethod.Post || request.Method == HttpMethod.Put))
            {
                return;
            }

            // Don't set "On Behalf Of" user if it will be the same as the user on the request.
            // This is not exact, as there could be a different token representing the same user, however
            // it is an optimization to avoid the GetUserId call, which would be an additional MP call.
            if (!string.IsNullOrWhiteSpace(AuthenticationToken) && AuthenticationToken.Equals(UserAuthorizationTokenHolder.Get()))
            {
                return;
            }

            var userId = await GetUserId(UserAuthorizationTokenHolder.Get());
            if (userId.HasValue)
            {
                request.AddQueryParameterIfSpecified("$userId", userId.Value.ToString());
            }
        }

        private async Task<int?> GetUserId(string token)
        {
            var request = new HttpRequest(HttpMethod.Get, "me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            var response = await AuthorizationRestClient.ExecuteAsync(request);
            response.CheckForErrors($"Error getting authorized user information for token {token}");

            var userId = (await response.GetContentAsync<JObject>())?["userid"];
            if (userId?.Value<object>() == null)
            {
                return null;
            }

            return userId.Value<int>();
        }
        #endregion


    }




    public class TableMetaData
    {
        [JsonProperty("AccessLevel")]
        public string AccessLevel { get; set; }

        [JsonProperty("Columns")]
        public TableColumn[] Columns { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }




    public class TableColumn
    {
        [JsonProperty("DataType")]
        public string DataType { get; set; }

        [JsonProperty("HasDefault")]
        public bool HasDefault { get; set; }

        [JsonProperty("IsComputed")]
        public bool IsComputed { get; set; }

        [JsonProperty("IsForeignKey")]
        public bool IsForeignKey { get; set; }

        [JsonProperty("IsPrimaryKey")]
        public bool IsPrimaryKey { get; set; }

        [JsonProperty("IsReadOnly")]
        public bool IsReadOnly { get; set; }

        [JsonProperty("IsRequired")]
        public bool IsRequired { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("ReferencedColumn")]
        public object ReferencedColumn { get; set; }

        [JsonProperty("ReferencedTable")]
        public object ReferencedTable { get; set; }

        [JsonProperty("Size")]
        public long Size { get; set; }
    }




    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MpRestApiPrimaryKey : Attribute
    {
        public string Name { get; set; }

        public MpRestApiPrimaryKey(string name)
        {
            Name = name;
        }
    }




    public class HttpRequest
    {
        private readonly Dictionary<string, string> _queryParameters = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _formParameters = new Dictionary<string, string>();
        public HttpContent Body { get; set; }

        private readonly HttpRequestMessage _request;
        private readonly string _path;

        public HttpRequest(HttpMethod method, string path)
        {
            _path = path;
            _request = new HttpRequestMessage(method, path);
        }

        /// <summary>
        /// Set the Body of this request to a JSON representation of the given record, wrapped in an array if it is not already a collection of some sort.  Note that this will clear any other Body previously set on the request, so use with caution.
        /// </summary>
        /// <param name="record">The object to serialize onto the request body - it will be wrapped in an array if it is not already a collection of some sort.</param>
        /// <returns>The HttpRequest, in case you want to chain method calls</returns>
        public HttpRequest SetJsonArrayBody(object record)
        {
            // Wrap the record in an array, if it is not already some sort of collection.

            if (record.GetType() == typeof(JObject))
            {
                return SetJsonBody(new List<object> { record });
            }

            var body = record.GetType().GetInterfaces().Any(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>))
                ? record
                : new List<object> { record };

            return SetJsonBody(body);
        }

        /// <summary>
        /// Set the Body of this request to a JSON representation of the given record.  Note that this will clear any other Body previously set on the request, so use with caution.
        /// </summary>
        /// <param name="record">The object to serialize onto the request body</param>
        /// <returns>The HttpRequest, in case you want to chain method calls</returns>
        public HttpRequest SetJsonBody(object record)
        {
            // This nonsense is needed because request.setJsonBody() does not honor Json name
            // attributes on the object, so proper names are not sent to MP. If the input
            // record is already a collection of some sort, just serialize it onto the body,
            // otherwise create a new collection containing the single record.
            var jsonBody = JsonConvert.SerializeObject(record);
            _request.Headers.Accept.ParseAdd("application/json");
            Body = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            return this;
        }

        /// <summary>
        /// Set the Body of this request to a Multi-Part Form Body, with the contents of the given file as the lone attachment.  Note that this will clear any other Body previously set on the request, so use with caution.
        /// </summary>
        /// <param name="name">The name of the form parameter (Content Disposition name)</param>
        /// <param name="bytes">The contents of the file</param>
        /// <param name="fileName">The name of the file (Content Disposition file name)</param>
        /// <returns>The HttpRequest, in case you want to chain method calls</returns>
        public HttpRequest SetFileBody(string name, byte[] bytes, string fileName)
        {
            Body = new MultipartFormDataContent { { new ByteArrayContent(bytes), name, fileName } };

            return this;
        }

        /// <summary>
        /// Adds the contents of the given file as a new part to the Multi-Part Form Body.  Note that if the body was previously set to something other than a Multi-Part form, this will clear it, so use with caution.
        /// </summary>
        /// <param name="name">The name of the form parameter (Content Disposition name)</param>
        /// <param name="bytes">The contents of the file</param>
        /// <param name="fileName">The name of the file (Content Disposition file name)</param>
        /// <returns>The HttpRequest, in case you want to chain method calls</returns>
        public HttpRequest AddMultiPartFileBody(string name, byte[] bytes, string fileName)
        {
            EnsureMultiPartFormBody().Add(new ByteArrayContent(bytes), name, fileName);

            return this;
        }

        /// <summary>
        /// Adds a JSON serialized form of the given record to the multi-part form body.  Note that if the body was previously set to something other than a Multi-Part form, this will clear it, so use with caution.
        /// </summary>
        /// <param name="name">The name of the form parameter (Content Disposition name)</param>
        /// <param name="record">The object to serialize to JSON and add</param>
        /// <returns>The HttpRequest, in case you want to chain method calls</returns>
        public HttpRequest AddMultiPartJsonBody(string name, object record)
        {
            EnsureMultiPartFormBody().Add(new StringContent(JsonConvert.SerializeObject(record), Encoding.UTF8, "application/json"), name);

            return this;
        }

        private MultipartFormDataContent EnsureMultiPartFormBody()
        {
            if (Body == null || Body.GetType() != typeof(MultipartFormDataContent))
            {
                Body = new MultipartFormDataContent();
            }

            return (MultipartFormDataContent)Body;
        }

        /// <summary>
        /// Add a query parameter to the request.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddQueryParameter(string name, string value)
        {
            _queryParameters[name] = value;
        }

        /// <summary>
        /// Add a query parameter to the request, only if the given value is not null or empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddQueryParameterIfSpecified<T>(string name, T value)
        {
            if (value == null)
            {
                return;
            }

            AddQueryParameterIfSpecified(name, value.ToString());
        }

        /// <summary>
        /// Add a query parameter to the request, only if the given value is not null or empty.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddQueryParameterIfSpecified(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _queryParameters[name] = value;
        }

        /// <summary>
        /// Get a read-only copy of the current query parameters.
        /// </summary>
        public IReadOnlyDictionary<string, string> QueryParameters => new ReadOnlyDictionary<string, string>(_queryParameters);

        public void AddFormParameter(string name, string value)
        {
            _formParameters[name] = value;
        }

        /// <summary>
        /// Get a read-only copy of the current multi-part form data parameters.
        /// </summary>
        public IReadOnlyDictionary<string, string> FormParameters => new ReadOnlyDictionary<string, string>(_formParameters);

        /// <summary>
        /// Gets or sets the request headers.
        /// </summary>
        public HttpRequestHeaders Headers => _request.Headers;

        /// <summary>
        /// Gets or sets the request method.
        /// </summary>
        public HttpMethod Method => _request.Method;

        /// <summary>
        /// Get a HttpRequestMessage based on the current configuration of this HttpRequest.  If any FormParameters are present, the request will be a multi-part/form, otherwise the specified Body (if any) will be used.
        /// </summary>
        public HttpRequestMessage Message
        {
            get
            {
                var requestUri = _path;
                if (_queryParameters.Any())
                {
                    requestUri = _queryParameters.Aggregate(requestUri, (current, p) => current.SetQueryParam(p.Key, p.Value, Flurl.NullValueHandling.NameOnly));
                }

                var requestMessage = new HttpRequestMessage(_request.Method, requestUri);

                foreach (var header in _request.Headers)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }

                if (_formParameters.Any())
                {
                    requestMessage.Content = new FormUrlEncodedContent(_formParameters);
                }
                else if (Body != null)
                {
                    requestMessage.Content = Body;
                }

                return requestMessage;
            }
        }
    }




    public interface IMinistryPlatformRestRequestBuilderFactory
    {
        IMinistryPlatformRestRequestBuilder NewRequestBuilder();
    }




    public interface IMinistryPlatformRestRequestBuilder
    {
        IMinistryPlatformRestRequestBuilder WithSelectColumns(IEnumerable<string> select);
        IMinistryPlatformRestRequestBuilder AddSelectColumn(string column);
        IMinistryPlatformRestRequestBuilder WithFilter(string filter);
        IMinistryPlatformRestRequestBuilder OrderBy(string orderBy);
        IMinistryPlatformRestRequestBuilder GroupBy(string groupBy);
        IMinistryPlatformRestRequestBuilder Having(string having);
        IMinistryPlatformRestRequestBuilder RestrictResultCount(int top);
        IMinistryPlatformRestRequestBuilder SkipResults(int skip);
        IMinistryPlatformRestRequestBuilder WithDistinct(bool distinct);
        IMinistryPlatformRestRequestBuilder WithAuthenticationToken(string authenticationToken);

        IMinistryPlatformRestRequest Build();
        IMinistryPlatformRestRequestAsync BuildAsync();
    }




    public interface IMinistryPlatformRestRequest
    {
        T Create<T>(T objectToCreate, string tableName = null);
        List<T> Create<T>(List<T> objectsToCreate, string tableName = null);
        T Get<T>(int id);
        T Get<T>(string tableName, int id);
        List<T> Search<T>();
        List<T> Search<T>(string tableName);
        T Update<T>(T objectToUpdate, string tableName = null);
        List<T> Update<T>(List<T> objectsToUpdate, string tableName = null);
        void Delete<T>(int recordId);
        void Delete<T>(IEnumerable<int> recordIds);
        int SendEmail(MpCommunication emailCommunication, List<MpFile> attachments = null);
        int ExecuteStoredProc(string procedureName, Dictionary<string, object> parameters);
        List<List<T>> ExecuteStoredProc<T>(string procedureName, Dictionary<string, object> parameters);
        List<TOut> PostWithReturn<TIn, TOut>(List<TIn> records);
        int CreateFile<T>(int recordId, MpFile file);
        int CreateFile(string tableName, int recordId, MpFile file);
        List<TableMetaData> GetTableMetaData(string tableName);
    }




    public interface IMinistryPlatformRestRequestAsync
    {
        Task<T> Create<T>(T objectToCreate, string tableName = null);
        Task<List<T>> Create<T>(List<T> objectsToCreate, string tableName = null);
        Task<T> Get<T>(int id);
        Task<T> Get<T>(string tableName, int id);
        Task<List<T>> Search<T>();
        Task<List<T>> Search<T>(string tableName);
        Task<T> Update<T>(T objectToUpdate, string tableName = null);
        Task<List<T>> Update<T>(List<T> objectsToUpdate, string tableName = null);
        Task Delete<T>(int recordId);
        Task Delete<T>(IEnumerable<int> recordIds);
        Task<int> SendEmail(MpCommunication emailCommunication, List<MpFile> attachments = null);
        Task<int> ExecuteStoredProc(string procedureName, Dictionary<string, object> parameters);
        Task<List<List<T>>> ExecuteStoredProc<T>(string procedureName, Dictionary<string, object> parameters);
        Task<List<TOut>> PostWithReturn<TIn, TOut>(List<TIn> records);
        Task<int> CreateFile<T>(int recordId, MpFile file);
        Task<int> CreateFile(string tableName, int recordId, MpFile file);
        Task<List<TableMetaData>> GetTableMetaData(string tableName);
    }




    public class MinistryPlatformRestRequestBuilderFactory : IMinistryPlatformRestRequestBuilderFactory
    {

        internal HttpClient MinistryPlatformRestClient { get; set; }
        internal HttpClient AuthorizationRestClient { get; set; }

        public MinistryPlatformRestRequestBuilderFactory(HttpClient ministryPlatformRestClient, HttpClient authorizationRestClient)
        {
            MinistryPlatformRestClient = ministryPlatformRestClient;
            AuthorizationRestClient = authorizationRestClient;
        }

        public IMinistryPlatformRestRequestBuilder NewRequestBuilder()
        {
            if (MinistryPlatformRestClient == null || AuthorizationRestClient == null)
            {
                throw new InvalidOperationException($"{typeof(MinistryPlatformRestRequestBuilderFactory).FullName} requires a MinistryPlatformRestClient and an AuthorizationRestClient");
            }

            return new MinistryPlatformRestRequestBuilder(MinistryPlatformRestClient, AuthorizationRestClient);
        }
    }




    public class MinistryPlatformRestRequestBuilder : IMinistryPlatformRestRequestBuilder
    {
        internal string Select;
        internal string Filter;
        internal string OrderByProperty;
        internal string GroupByProperty;
        internal string HavingProperty;
        internal int? Top;
        internal int? Skip;
        internal bool? Distinct;
        internal string AuthenticationToken;

        internal HttpClient MinistryPlatformRestClient { get; set; }
        internal HttpClient AuthorizationRestClient { get; set; }

        internal MinistryPlatformRestRequestBuilder(HttpClient ministryPlatformRestClient, HttpClient authorizationRestClient)
        {
            MinistryPlatformRestClient = ministryPlatformRestClient;
            AuthorizationRestClient = authorizationRestClient;
        }

        #region Fluent builder
        public IMinistryPlatformRestRequestBuilder WithSelectColumns(IEnumerable<string> select)
        {
            Select = string.Join(",", select);
            return this;
        }

        public IMinistryPlatformRestRequestBuilder AddSelectColumn(string column)
        {
            Select = string.IsNullOrWhiteSpace(Select) ? column : $"{Select},{column}";
            return this;
        }

        public IMinistryPlatformRestRequestBuilder WithFilter(string filter)
        {
            Filter = filter;
            return this;
        }

        public IMinistryPlatformRestRequestBuilder OrderBy(string orderBy)
        {
            OrderByProperty = orderBy;
            return this;
        }

        public IMinistryPlatformRestRequestBuilder GroupBy(string groupBy)
        {
            GroupByProperty = groupBy;
            return this;
        }

        public IMinistryPlatformRestRequestBuilder Having(string having)
        {
            HavingProperty = having;
            return this;
        }

        public IMinistryPlatformRestRequestBuilder RestrictResultCount(int top)
        {
            Top = top;
            return this;
        }

        public IMinistryPlatformRestRequestBuilder SkipResults(int skip)
        {
            Skip = skip;
            return this;
        }

        public IMinistryPlatformRestRequestBuilder WithDistinct(bool distinct)
        {
            Distinct = distinct;
            return this;
        }

        public IMinistryPlatformRestRequestBuilder WithAuthenticationToken(string authenticationToken)
        {
            AuthenticationToken = authenticationToken;
            return this;
        }
        #endregion

        public IMinistryPlatformRestRequest Build()
        {
            return new MinistryPlatformRestRequest(BuildAsync());
        }

        public IMinistryPlatformRestRequestAsync BuildAsync()
        {
            return new MinistryPlatformRestRequestAsync(MinistryPlatformRestClient, AuthorizationRestClient)
            {
                AuthenticationToken = AuthenticationToken,
                Distinct = Distinct,
                Filter = Filter,
                GroupBy = GroupByProperty,
                Having = HavingProperty,
                OrderBy = OrderByProperty,
                Select = Select,
                Skip = Skip,
                Top = Top
            };
        }
    }




    public class MinistryPlatformRestRequest : IMinistryPlatformRestRequest
    {
        private readonly IMinistryPlatformRestRequestAsync _ministryPlatformRestRequest;

        public MinistryPlatformRestRequest(IMinistryPlatformRestRequestAsync ministryPlatformRestRequest)
        {
            _ministryPlatformRestRequest = ministryPlatformRestRequest;
        }

        /// <summary>
        /// Creates the object in MP. The table name is optional - it will be inferred from the objectToCreate,
        /// if the appropriate attributes are set. If tableName is specified, it will be used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToCreate"></param>
        /// <param name="tableName"></param>
        public T Create<T>(T objectToCreate, string tableName = null)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Create(objectToCreate, tableName));
        }

        /// <summary>
        /// Creates the objects in MP. The table name is optional - it will be inferred from the objectsToCreate,
        /// if the appropriate attributes are set. If tableName is specified, it will be used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectsToCreate"></param>
        /// <param name="tableName"></param>
        public List<T> Create<T>(List<T> objectsToCreate, string tableName = null)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Create(objectsToCreate, tableName));
        }

        public T Get<T>(int id)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Get<T>(id));
        }

        public T Get<T>(string tableName, int id)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Get<T>(tableName, id));
        }

        public List<T> Search<T>()
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Search<T>());
        }

        public List<T> Search<T>(string tableName)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Search<T>(tableName));
        }

        /// <summary>
        /// Updates the object in MP. The table name is optional - it will be inferred from the objectToUpdate,
        /// if the appropriate attributes are set. If tableName is specified, it will be used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToUpdate"></param>
        /// <param name="tableName"></param>
        public T Update<T>(T objectToUpdate, string tableName = null)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Update(objectToUpdate, tableName));
        }

        /// <summary>
        /// Updates the objects in MP. The table name is optional - it will be inferred from the objectsToUpdate,
        /// if the appropriate attributes are set. If tableName is specified, it will be used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectsToUpdate"></param>
        /// <param name="tableName"></param>
        public List<T> Update<T>(List<T> objectsToUpdate, string tableName = null)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.Update(objectsToUpdate, tableName));
        }

        public void Delete<T>(int recordId)
        {
            ExecuteAndWait(() => _ministryPlatformRestRequest.Delete<T>(recordId));
        }

        public void Delete<T>(IEnumerable<int> recordIds)
        {
            ExecuteAndWait(() => _ministryPlatformRestRequest.Delete<T>(recordIds));
        }

        public int SendEmail(MpCommunication emailCommunication, List<MpFile> attachments = null)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.SendEmail(emailCommunication, attachments));
        }

        public int ExecuteStoredProc(string procedureName, Dictionary<string, object> parameters)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.ExecuteStoredProc(procedureName, parameters));
        }

        public List<List<T>> ExecuteStoredProc<T>(string procedureName, Dictionary<string, object> parameters)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.ExecuteStoredProc<T>(procedureName, parameters));
        }

        public List<TOut> PostWithReturn<TIn, TOut>(List<TIn> records)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.PostWithReturn<TIn, TOut>(records));
        }

        public int CreateFile<T>(int recordId, MpFile file)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.CreateFile<T>(recordId, file));
        }

        public int CreateFile(string tableName, int recordId, MpFile file)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.CreateFile(tableName, recordId, file));
        }

        /// <summary>
        /// Queries Ministry Platform for Table Metadata. If no table name is passed in, queries all tables 
        /// in Ministry Platform. 
        /// 
        /// The tableName needs to be named exactly to get results. Although a "*" can be used as a wild card.
        /// For example: GetTableMetaData("Contacts") will only return the Contacts table Metadata, but  GetTableMetaData("Contact*")
        /// will return table Metadata for Contacts and Contact_Relationships and Contact_Categories etc.
        /// </summary>
        /// <param name="tableName">Optional search string. Defaults to Null which will return all tables.</param>
        /// <returns>A list of tables that match the table name</returns>
        public List<TableMetaData> GetTableMetaData(string tableName = null)
        {
            return ExecuteAndWait(() => _ministryPlatformRestRequest.GetTableMetaData(tableName));
        }

        private static T ExecuteAndWait<T>(Func<Task<T>> doTheFunk)
        {
            var response = doTheFunk();
            response.Wait();

            return response.Result;
        }

        private static void ExecuteAndWait(Func<Task> doTheFunk)
        {
            var response = doTheFunk();
            response.Wait();
        }
    }




    [MpRestApiTable(Name = "Communications")]
    public class MpCommunication
    {
        [JsonProperty("communicationId")]
        public int CommunicationID { get; set; }

        [JsonProperty("authorUserId")]
        public int AuthorUserID { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("fromContactId")]
        public int FromContact { get; set; }

        [JsonProperty("fromName")]
        public string FromName { get; set; }

        [JsonProperty("fromAddress")]
        public string FromAddress { get; set; }

        [JsonProperty("replyToContactId")]
        public int ReplyToContact { get; set; }

        [JsonProperty("replyToName")]
        public string ReplyToName { get; set; }

        [JsonProperty("replyToAddress")]
        public string ReplyToAddress { get; set; }

        [JsonProperty("isTemplate")]
        public bool Template { get; set; }

        [JsonProperty("contacts")]
        public List<int> Contacts { get; set; }

        public MpCommunication()
        {
            Contacts = new List<int>();
        }

    }




    [AttributeUsage(AttributeTargets.Class)]
    public class MpRestApiTable : Attribute
    {
        public string Name { get; set; }
    }




    public static class HttpClientExtensions
    {
        /// <summary>
        /// Execute the given HttpRequest asynchronously.
        /// </summary>
        /// <param name="client">The HttpClient to use when sending this request</param>
        /// <param name="request">The HttpRequest to send</param>
        /// <returns>A Task that must be awaited</returns>
        public static Task<HttpResponseMessage> ExecuteAsync(this HttpClient client, HttpRequest request)
        {
            return client.SendAsync(request.Message);
        }

        /// <summary>
        /// Execute the given HttpRequest synchronously.
        /// </summary>
        /// <param name="client">The HttpClient to use when sending this request</param>
        /// <param name="request">The HttpRequest to send</param>
        /// <returns>A Task that has been awaited</returns>
        public static Task<HttpResponseMessage> Execute(this HttpClient client, HttpRequest request)
        {
            var response = client.SendAsync(request.Message);
            response.Wait();

            return response;
        }

        /// <summary>
        /// Determine if this response is an error.  It looks at the ResponseStatus and the HTTP StatusCode to make the determination.
        /// </summary>
        /// <param name="response">The response to check</param>
        /// <param name="errorNotFound">Indicates if a 404 should be considered an error</param>
        /// <returns></returns>
        public static bool IsError(this Task<HttpResponseMessage> response, bool errorNotFound = false)
        {
            // If the request is not completed, this is an error
            if (!response.IsCompleted)
            {
                return true;
            }

            return response.Result.IsError(errorNotFound);
        }

        public static bool IsError(this HttpResponseMessage response, bool errorNotFound = false)
        {
            // If we got a 404, and we're considering that an error, then it's an error
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return errorNotFound;
            }

            // If we have a bad response code, then it's an error
            return !response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Checks for error response, and throws a RestResponseException if the response is in error.
        /// </summary>
        /// <param name="response">The response to check</param>
        /// <param name="errorMessage">The error message to include in the exception, if error</param>
        /// <param name="errorNotFound">Indicates if a 404 should be considered an error</param>
        public static void CheckForErrors(this Task<HttpResponseMessage> response, string errorMessage, bool errorNotFound = false)
        {
            if (!IsError(response, errorNotFound))
            {
                return;
            }

            throw new HttpResponseMessageException(errorMessage, response);
        }

        public static void CheckForErrors(this HttpResponseMessage response, string errorMessage, bool errorNotFound = false)
        {
            if (!IsError(response, errorNotFound))
            {
                return;
            }

            throw new HttpResponseMessageException(errorMessage, response);
        }

        public static string GetContent(this HttpResponseMessage response)
        {
            var content = response.GetContentAsync();
            content.Wait();

            return content.Result;
        }

        public static T GetContent<T>(this HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject<T>(response.GetContent());
        }

        public static async Task<string> GetContentAsync(this HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            return content;
        }

        public static async Task<T> GetContentAsync<T>(this HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject<T>(await response.GetContentAsync());
        }
    }




    public class HttpResponseMessageException : Exception
    {
        public Task<HttpResponseMessage> Response { get; private set; }
        public HttpResponseMessage ResponseMessage { get; private set; }

        public HttpResponseMessageException(string message, HttpResponseMessage response)
            : base(
                $"{message} - Status Code: {response.StatusCode}, Error: {response.ReasonPhrase}, Content: {response.GetContent<string>()}")
        {
            ResponseMessage = response;
        }

        public HttpResponseMessageException(string message, Task<HttpResponseMessage> response)
            : base(
                $"{message} - Status: {response.Status}, Status Code: {response?.Result?.StatusCode}, Error: {response?.Result?.ReasonPhrase}, Content: {response?.Result?.GetContent<string>()}",
                response.Exception)
        {
            Response = response;
        }
    }




    [MpRestApiTable(Name = "Files")]
    public class MpFile
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        [JsonIgnore]
        public byte[] FileContents { get; set; }
    }




    public class NoTableDefinitionException<T> : Exception
    {
        public NoTableDefinitionException() : base($"No RestApiTable attribute specified on type {typeof(T)}")
        {
        }
    }




    public class NoPrimaryKeyDefinitionException<T> : Exception
    {
        public NoPrimaryKeyDefinitionException() : base($"No RestApiPrimaryKey attribute specified on type {typeof(T)}")
        {
        }
    }




    public class UserAuthorizationTokenHolder
    {
        private static readonly ThreadLocal<string> Token = new ThreadLocal<string>();

        public static void Set(string token)
        {
            Token.Value = token;
        }

        public static string Get()
        {
            return IsSet() ? Token.Value.Trim() : null;
        }

        public static void Clear()
        {
            Token.Value = null;
        }

        public static bool IsSet()
        {
            return Token.IsValueCreated && !string.IsNullOrWhiteSpace(Token.Value);
        }
    }
}
