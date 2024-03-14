using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ServiceManager.VendorX.RecurringService.Models.CRMPlatform
{
    public class SubscriptionCustomField
    {
        [JsonProperty("data")]
        public List<data> datum { get; set; }

        public SubscriptionCustomField()
        { 
            datum = new List<data>();
        }

        public class data
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("group")]
            public Group group { get; set; }
            public class Group
            {
                [JsonProperty("id")]
                public int id { get; set; }
                [JsonProperty("name")]
                public string name { get; set; }
            }
            [JsonProperty("kind")]
            public string kind { get; set; }
            [JsonProperty("dataType")]
            public string dataType { get; set; }
            [JsonProperty("required")]
            public bool required { get; set; }
            [JsonProperty("decimals")]
            public int decimals { get; set; }
            [JsonProperty("values")]
            public Values values { get; set; }
            public class Values
            {
                [JsonProperty("id")]
                public string id { get; set; }
                [JsonProperty("value")]
                public string value { get; set; }
            }
            [JsonProperty("readOnly")]
            public bool readOnly { get; set; }
            [JsonProperty("apiField")]
            public string apiField { get; set; }
            [JsonProperty("formulaValue")]
            public string formulaValue { get; set; }
            [JsonProperty("valuesList")]
            public ValuesList valuesList { get; set; }
            public class ValuesList
            {
                [JsonProperty("id")]
                public string id { get; set; }
                [JsonProperty("value")]
                public string value { get; set; }
            }
        }
    }
}