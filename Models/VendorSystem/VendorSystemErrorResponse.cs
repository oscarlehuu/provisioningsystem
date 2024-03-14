using Newtonsoft.Json;

public class VendorSystemErrorResponse
{
    [JsonProperty("FieldName")]
    public string FiledName;
    [JsonProperty("ErrorCode")]
    public string ErrorCode;
    [JsonProperty("Value")]
    public string Value;
    [JsonProperty("type")]
    public string type;
    [JsonProperty("title")]
    public string title;
    [JsonProperty("status")]
    public int status;
    [JsonProperty("detail")]
    public string detail;
    [JsonProperty("instance")]
    public string instace;
}