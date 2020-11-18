using BCloudServiceUtilities_BFileService_AZ.Models;
using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BCloudServiceUtilities_BFileService_AZ
{
    public class AZSystemTopicOperations
    {
        private const string API_VERSION = "api-version=2020-04-01-preview";
        private EventGridManagementClient Client;

        public AZSystemTopicOperations(EventGridManagementClient _Client)
        {
            Client = _Client;
        }

        /// <summary>
        /// Create a system topic.
        /// </summary>
        /// <remarks>
        /// Asynchronously creates a new topic with the specified parameters.
        /// </remarks>
        /// <param name='_ResourceGroupName'>
        /// The name of the resource group within the user's subscription.
        /// </param>
        /// <param name='_SystemTopicName'>
        /// Name of the topic.
        /// </param>
        /// <param name='_SystemTopicInfo'>
        /// System Topic information.
        /// </param>
        /// <param name='_FinalTopicInfo'>
        /// The resulting System topic if successfully created.
        /// </param>
        /// <param name='_ErrorMessageAction'>
        /// Method to write exceptions to.
        /// </param>
        /// <exception cref="SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        /// <return>
        /// A response object containing the response body and response headers.
        /// </return>
        public bool CreateOrUpdate(string _ResourceGroupName, string _SystemTopicName, SystemTopic _SystemTopicInfo, out SystemTopic _FinalTopicInfo, Action<string> _ErrorMessageAction = null)
        {
            if (Client.SubscriptionId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "this.Client.SubscriptionId");
            }
            if (_ResourceGroupName == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "resourceGroupName");
            }
            if (_SystemTopicName == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "topicName");
            }
            if (_SystemTopicInfo == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "topicInfo");
            }
            if (_SystemTopicInfo != null)
            {
                _SystemTopicInfo.Validate();
            }
            if (Client.ApiVersion == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "this.Client.ApiVersion");
            }

            // Construct URL
            var _baseUrl = Client.BaseUri.AbsoluteUri;
            var _url = new System.Uri(new System.Uri(_baseUrl + (_baseUrl.EndsWith("/") ? "" : "/")), "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/systemTopics/{systemTopicName}").ToString();
            _url = _url.Replace("{subscriptionId}", System.Uri.EscapeDataString(Client.SubscriptionId));
            _url = _url.Replace("{resourceGroupName}", System.Uri.EscapeDataString(_ResourceGroupName));
            _url = _url.Replace("{systemTopicName}", System.Uri.EscapeDataString(_SystemTopicName));

            List<string> _queryParameters = new List<string>();

            if (Client.ApiVersion != null)
            {
                _queryParameters.Add(API_VERSION);
            }
            if (_queryParameters.Count > 0)
            {
                _url += (_url.Contains("?") ? "&" : "?") + string.Join("&", _queryParameters);
            }

            // Create HTTP transport objects
            var _httpRequest = new HttpRequestMessage();
            HttpResponseMessage _httpResponse = null;
            _httpRequest.Method = new HttpMethod("PUT");
            _httpRequest.RequestUri = new System.Uri(_url);

            // Serialize Request
            string _requestContent = null;
            if (_SystemTopicInfo != null)
            {
                _requestContent = JsonConvert.SerializeObject(_SystemTopicInfo, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                });

                _httpRequest.Content = new StringContent(_requestContent, System.Text.Encoding.UTF8);
                _httpRequest.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
            }
            // Set Credentials
            CancellationToken CancellationToken = new CancellationToken();

            CancellationToken.ThrowIfCancellationRequested();
            Client.Credentials.ProcessHttpRequestAsync(_httpRequest, CancellationToken);

            Task<HttpResponseMessage> Response = Client.HttpClient.SendAsync(_httpRequest, CancellationToken);
            Response.Wait();

            _httpResponse = Response.Result;

            Response.Dispose();

            string _responseContent;

            if (_httpResponse.StatusCode == HttpStatusCode.OK || _httpResponse.StatusCode == HttpStatusCode.Created)
            {
                Task<string> ContentReadTask = _httpResponse.Content.ReadAsStringAsync();
                ContentReadTask.Wait();

                _responseContent = ContentReadTask.Result;

                ContentReadTask.Dispose();

                try
                {
                    _FinalTopicInfo = JsonConvert.DeserializeObject<SystemTopic>(_responseContent);
                    return true;
                }
                catch (JsonException ex)
                {
                    _httpRequest.Dispose();
                    if (_httpResponse != null)
                    {
                        _httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", _responseContent, ex);
                }
            }
            else
            {
                Task<string> ContentReadTask = _httpResponse.Content.ReadAsStringAsync();
                ContentReadTask.Wait();

                _responseContent = ContentReadTask.Result;
                _ErrorMessageAction?.Invoke(_responseContent);

                ContentReadTask.Dispose();
            }

            _FinalTopicInfo = null;
            return false;
        }
    }
}
