using System;

namespace ServiceManager.VendorX.RecurringService.Models.CRMPlatform
{
    public class BillingAddress
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string region { get; set; }
        public string postCode { get; set; }
        public string country { get; set; }
        public string countryCode { get; set; }
        public string state { get; set; }
        public string stateCode { get; set; }
        public bool isBilling { get; set; }
    }
}