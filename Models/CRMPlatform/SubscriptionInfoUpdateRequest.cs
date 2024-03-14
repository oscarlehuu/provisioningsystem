using Newtonsoft.Json;
public class SubscriptionInfoUpdateRequest
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string name { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool autoRenewal { get; set; }
}