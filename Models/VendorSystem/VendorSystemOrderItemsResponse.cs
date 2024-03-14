using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class VendorSystemOrderItemsResponse
{
    [JsonProperty("Line")]
    int Line;
    [JsonProperty("Index")]
    int Index;
    [JsonProperty("Type")]
    string Type;
    [JsonProperty("ProductCode")]
    string ProductCode;
    [JsonProperty("SKU")]
    string SKU;
    [JsonProperty("ProductName")]
    string productName;
    [JsonProperty("ProductDescription")]
    string ProductDescription;
    [JsonProperty("UnitPrice")]
    Decimal UnitPrice;
    [JsonProperty("Discount")]
    Decimal Discount;
    [JsonProperty("Quantity")]
    int Quantity;
    [JsonProperty("Net")]
    float Net;
    [JsonProperty("Tax")]
    float Tax;
    [JsonProperty("ResellerId")]
    string ResellerId;
    [JsonProperty("ResellerPrice")]
    Decimal ResellerPrice;
    [JsonProperty("PrivateKeyPassword")]
    Decimal PrivateKeyPassword;
    [JsonProperty("LicenseKeys")]
    List<VendorSystemOrderItemsLicenseKeysResponse> LicenseKeys;
    [JsonProperty("ErrorCode")]
    String ErrorCode;
}