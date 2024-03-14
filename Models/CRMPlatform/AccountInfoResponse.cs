using Newtonsoft.Json;

public class AccountInfoResponse
{
    [JsonProperty("id")]
    public int id { get; set; }
    [JsonProperty("name")]
    public string name { get; set; }
    [JsonProperty("billToAccount")]
    public BillToAccount billToAccount { get; set; }

    public class BillToAccount
    {
        [JsonProperty("id")]
        public int id { get; set; }
        [JsonProperty("name")]
        public string name { get; set; }
    }
}