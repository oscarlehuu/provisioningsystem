using Newtonsoft.Json;

public class VendorSystemMarginData
{
    [JsonProperty("PartnerLevel")]
    public string PartnerLevel { get; set; }
    [JsonProperty("DiscountPercent")]
    public double DiscountPercent { get; set; }
    [JsonProperty("HostingDiscount")]
    public double HostingDiscount { get; set; }
}