using Newtonsoft.Json;

public class VendorSystemOrderMargin : VendorSystemErrorResponse
{
    [JsonProperty("PartnerLevel")]
    public string PartnerLevel { get; set; }
    [JsonProperty("DiscountPercent")]
    public double? DiscountPercent { get; set; }
    [JsonProperty("HostingDiscount")]
    public double? HostingDiscount { get; set; }
}