using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;

public class VendorSystemOrderResponse
{
    [JsonProperty("UniqueId")]
    string UniqueId;
    [JsonProperty("TrackingCode")]
    long TrackingCode;
    [JsonProperty("Currency")]
    string Currency;
    [JsonProperty("AdditonalDiscountPerc")]
    float AdditionalDiscountPerc;
    [JsonProperty("AdditionalDiscount")]
    float AdditionalDiscount;
    [JsonProperty("SubTotal")]
    float SubTotal;
    [JsonProperty("TaxPerc")]
    float TaxPerc;
    [JsonProperty("Tax")]
    float Tax;
    [JsonProperty("GrandTotal")]
    float GrandTotal;
    List<VendorSystemOrderItemsResponse> Items;
}