using Newtonsoft.Json;
using System;

public class VendorSystemOrderItemsLicenseKeysResponse
{
    [JsonProperty("LicenseKey")]
    string LicenseKey;
    [JsonProperty("SimultaneousCalls")]
    int SimultaneousCalls;
    [JsonProperty("IsPerpetual")]
    Boolean IsPerpetual;
    [JsonProperty("Edition")]
    String Edition;
    [JsonProperty("ExpiryIncludedMonths")]
    int ExpiryIncludedMonths;
    [JsonProperty("ExpiryDate")]
    DateTime ExpiryDate;
    [JsonProperty("MaintenanceIncludedMonths")]
    int MaintenanceIncludedMonths;
    [JsonProperty("MaintenanceDate")]
    DateTime MaintenanceDate;
    [JsonProperty("HostingIncludedMonths")]
    int HostingIncludedMonths;
    [JsonProperty("HostingExpiry")]
    DateTime HostingExpiry;
}