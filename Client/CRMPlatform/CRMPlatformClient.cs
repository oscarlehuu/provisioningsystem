using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CRMPlatform.Cloud.ServiceManagersSDK.Libraries.Logs;
using DocumentFormat.OpenXml.Office2016.Excel;
using System.Security.Cryptography;
using System.Configuration;

namespace ServiceManager.VendorX.RecurringService.Models.CRMPlatform
{
    public class IWClient
    {
        private String baseUrl = "http://crmplatform.com.au";
        private readonly string client_id = "";
        private readonly string client_secret = "";
        private readonly string username = "";
        private readonly string password = "";

        private string accessToken = string.Empty;
        private readonly HttpClient _httpClient;

        public string GetAccessToken()
        {
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			string basicAuthToken = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(client_id + ":" + client_secret));
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthToken);
                var values = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "username", username },
                    { "password", password }
                };
                var content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = httpClient.PostAsync(baseUrl + "/oauth/token", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    string responseData = response.Content.ReadAsStringAsync().Result;
                    TokenResponse tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseData);
                    accessToken = tokenResponse.access_token;
                }
                else
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    throw new HttpRequestException($"Error: {response.StatusCode}. Reason: {response.ReasonPhrase}. Content: {errorContent}");
                }
            }
            return accessToken;
        }

        private HttpClient GetHeaders(String accessToken, String apiVerison)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            HttpClient headers = new HttpClient();
            headers.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            headers.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);
            headers.DefaultRequestHeaders.Add("X-api-version", apiVerison);
            return headers;
        }

        private  T Post<T>(Guid logUUID, LogsWrapper log, string stack, object value, string action, String accessToken, String apiVersion) where T : class, new()
        {
            stack += "-> Post " + action;
            T result = default(T);
            using (new LogTracer(log.LogSendReceive, log, stack, logUUID, new List<object> { value, action }))
            {
                try
                {
                    using (HttpClient httpClient = GetHeaders(this.accessToken, apiVersion))
                    {
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        string jsonContent = JsonConvert.SerializeObject(value);
                        using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                        {
                            //HttpResponseMessage response = httpClient.PostAsync(baseUrl + action, content).Result;
                            HttpResponseMessage response = httpClient.PostAsync(baseUrl + action, content).ConfigureAwait(false).GetAwaiter().GetResult();
                            if (response.IsSuccessStatusCode)
                            {
                                //string responseData = response.Content.ReadAsStringAsync().Result;
                                string responseData = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                                result = JsonConvert.DeserializeObject<T>(responseData);
                            }
                            else
                            {
                                //var errorContent = response.Content.ReadAsStringAsync().Result;
                                var errorContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                                throw new HttpRequestException($"Error: {response.StatusCode}. Reason: {response.ReasonPhrase}. Content: {errorContent}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    LogsHelper.LogException(ex, log.Logger, logUUID, stack, true);
                }
            }
            return result;
        }

        private T Get<T>(Guid logUUID, LogsWrapper log, string stack, string action, string accessToken, String apiVersion) where T : class, new()
        {
            stack += "-> Get " + action;
            T result = default(T);
            using (new LogTracer(log.LogSendReceive, log, stack, logUUID, new List<object> { action }))
            {
                try
                {
                    using (HttpClient httpClient = GetHeaders(this.accessToken, apiVersion))
                    {
                        HttpResponseMessage response = httpClient.GetAsync(baseUrl + action).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            string responseData = response.Content.ReadAsStringAsync().Result;
                            result = JsonConvert.DeserializeObject<T>(responseData);
                        }
                        else
                        {
                            var errorContent = response.Content.ReadAsStringAsync().Result;
                            throw new HttpRequestException($"Error: {response.StatusCode}. Reason: {response.ReasonPhrase}. Content: {errorContent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    LogsHelper.LogException(ex, log.Logger, logUUID, stack, true);
                }
            }
            return result;
        }

        public SubscriptionInfoUpdateResponse UpdateSubscriptionInfo(SubscriptionInfoUpdateRequest updateRequest, String subscriptionId, String accessToken, String apiVersion,  string stack, Guid logUUID, LogsWrapper log)
        {
            stack += "-> UpdateSubscriptionInfo";
            var action = $"/api/Subscriptions/{subscriptionId}/set";
            return  Post<SubscriptionInfoUpdateResponse>(logUUID, log, stack, updateRequest, action, this.accessToken, apiVersion);
        }

        public List<AccountCustomFields> accountCustomFieldsResponse(int accountId, String accessToken, String apiVersion, string stack, Guid logUUID, LogsWrapper log)
        {
            stack += "-> AccountCustomFields";
            var action = $"/api/Accounts/{accountId}/customfields";
            return Get<List<AccountCustomFields>>(logUUID, log, stack, action, this.accessToken, apiVersion);
        }

        public AccountInfoResponse getAccountInfo(int accountId, String accessToken, String apiVersion, string stack, Guid logUUID, LogsWrapper log)
        {
            stack += "-> AccountInfo";
            var action = $"/api/accounts/{accountId}";
            return Get<AccountInfoResponse>(logUUID, log, stack, action, this.accessToken, apiVersion);
        }
    }
    
}
