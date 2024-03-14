using Newtonsoft.Json;
using System;

namespace ServiceManager.VendorX.RecurringService.Models.CRMPlatform
{
    public class BillingInformation
    {
        public long id { get; set; }
        public string name { get; set; }
        public BillingAddress[] addresses { get; set; }
    }
}