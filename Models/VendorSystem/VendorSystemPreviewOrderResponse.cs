using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class VendorSystemPreviewOrderResponse
{
    [JsonProperty("UniqueId")]
    public string UniqueId { get; set; }
    [JsonProperty("TrackingCode")]
    public string TrackingCode { get; set; }
    [JsonProperty("Currency")]
    public string Currency { get; set; }
    [JsonProperty("AdditionalDiscountPerc")]
    public decimal AdditionalDiscountPerc { get; set; }
    [JsonProperty("AdditionalDiscount")]
    public decimal AdditionalDiscount { get; set; }
    [JsonProperty("SubTotal")]
    public Decimal SubTotal { get; set; }
    [JsonProperty("TaxPerc")]
    public Decimal TaxPerc { get; set; }
    [JsonProperty("Tax")]
    public Decimal Tax { get; set; }
    [JsonProperty("GrandTotal")]
    public Decimal GrandTotal { get; set; }

    [JsonProperty("Items")]
    public List<Item> Items { get; set; }

    public VendorSystemPreviewOrderResponse()
    { 
        Items = new List<Item>();
    }

    public class Item
    {
        [JsonProperty("Line")]
        public int Line { get; set;}
        [JsonProperty("Index")]
        public int Index { get; set; }
        [JsonProperty("Type")]
        public string Type { get; set; }
        [JsonProperty("ProductCode")]
        public string ProductCode { get; set; }
        [JsonProperty("SKU")]
        public string SKU { get; set; }
        [JsonProperty("ProductName")]
        public string ProductName { get; set; }
        [JsonProperty("ProductDescription")]
        public string ProductDescription { get; set; }
        [JsonProperty("UnitPrice")]
        public Decimal UnitPrice { get; set; }
        [JsonProperty("Discount")]
        public Decimal Discount { get; set; }
        [JsonProperty("Quantity")]
        public int Quantity { get; set; }
        [JsonProperty("Net")]
        public Decimal Net { get; set; }
        [JsonProperty("Tax")]
        public Decimal Tax { get; set; }
        [JsonProperty("ResellerId")]
        public string ResellerId { get; set; }
        [JsonProperty("ResellerPrice")]
        public Decimal ResellerPrice { get; set; }
        [JsonProperty("PrivateKeyPassword")]
        public string PrivateKeyPassword { get; set; }
        [JsonProperty("LicenseKeys")]
        public List<LicenseKeysList> LicenseKeys { get; set; }

        public Item()
        {
            LicenseKeys = new List<LicenseKeysList>();
        }

        public class LicenseKeysList
        {
            [JsonProperty("LicenseKey")]
            public string LicenseKey { get; set; }
            [JsonProperty("SimultaneousCalls")]
            public int? SimultaneousCalls { get; set; }
            [JsonProperty("IsPerpetual")]
            public bool IsPerpetual { get; set; }
            [JsonProperty("Edition")]
            public string Edition { get; set; }
            [JsonProperty("ExpiryIncludedMonths")]
            public int? ExpiryIncludedMonths { get; set; }
            [JsonProperty("ExpiryDate")]
            public DateTime? ExpiryDate { get; set;}
            [JsonProperty("MaintenanceIncludedMonths")]
            public int? MaintenanceIncludedMonths { get; set; }
            [JsonProperty("MaintenanceDate")]
            public DateTime? MaintenanceDate { get; set; }
            [JsonProperty("HostingIncludedMonths")]
            public int? HostingIncludedMonths { get; set; }
            [JsonProperty("HostingExpiry")]
            public DateTime? HostingExpiry { get; set; }
        }
        [JsonProperty("ErrorCode")]
        public string ErrorCode { get; set; }
    }
}