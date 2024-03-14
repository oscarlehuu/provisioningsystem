using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.PeerToPeer;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;
using CRMPlatform.Cloud.ServiceManagersSDK.Libraries.Logs;
using Newtonsoft.Json;

public class VendorSystemClient
{
    readonly string BASE_URL = "https://api.vendor.com/api";
    private readonly string clientKey = "";
    private readonly string clientSecret = "";

    private HttpClient GetHeaders()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientKey}:{clientSecret}"));
        HttpClient headers = new HttpClient();
        headers.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        headers.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64String);
        return headers;
    }

    private T Post<T>(Guid logUUID, LogsWrapper log, string stack, object value, string action) where T : class, new()
    {
        stack += "-> Post " + action;
        T result = default(T);
        using (new LogTracer(log.LogSendReceive, log, stack, logUUID, new List<object> { value, action }))
        {
            try
            {
                using (HttpClient httpClient = GetHeaders())
                {
                    string jsonContent = JsonConvert.SerializeObject(value);
                    using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    {
                        HttpResponseMessage response = httpClient.PostAsync(BASE_URL + action, content).Result;
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
            }
            catch (Exception ex)
            {
                // Log the exception
                LogsHelper.LogException(ex, log.Logger, logUUID, stack, true);
            }
        }
        return result;
    }

    private T Get<T>(Guid logUUID, LogsWrapper log, string stack, string action) where T : class, new()
    {
        stack += "-> Get " + action;
        T result = default(T);
        using (new LogTracer(log.LogSendReceive, log, stack, logUUID, new List<object> { action }))
        {
            try
            {
                using (HttpClient httpClient = GetHeaders())
                {
                    HttpResponseMessage response = httpClient.GetAsync(BASE_URL + action).Result;
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


    public VendorSystemPreviewOrderResponse PreviewOrder(VendorSystemCreateOrderRequest line, string stack, Guid logUUID, LogsWrapper log)
    {
        stack += "-> PreviewOrder";
        var action = "?readOnly=true";
        return Post<VendorSystemPreviewOrderResponse>(logUUID, log, stack, line, action);
    }

    public VendorSystemPreviewOrderResponse ProcessOrder(VendorSystemCreateOrderRequest line, string stack, Guid logUUID, LogsWrapper log)
    {
        stack += "-> ProcessOrder";
        var action = "?readOnly=false";
        return Post<VendorSystemPreviewOrderResponse>(logUUID, log, stack, line, action);
    }

    public VendorSystemOrderMargin GetMarginData(string partnerId, string stack, Guid logUUID, LogsWrapper log)
    {
        stack += "-> GetMarginData";
        var action = $"/Margin?partnerId={partnerId}";
        return Get<VendorSystemOrderMargin>(logUUID, log, stack, action);
    }
}