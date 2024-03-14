using Newtonsoft.Json;
using System.Collections.Generic;

namespace ServiceManager.VendorX.RecurringService.Models.CRMPlatform
{
    public class AccountCustomFields
    {
        [JsonProperty("group")]
        public Group group { get; set; }
        public class Group
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }
        }

        [JsonProperty("groupFields")]
        public List<GroupField> groupFields { get; set; }

        public class GroupField
        {
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("apiField")]
            public string apiField { get; set; }
            [JsonProperty("values")]
            public List<valueList> values { get; set; }
            public class valueList
            {
                [JsonProperty("id")]
                public string id;
                [JsonProperty("value")]
                public string value;
            }
        }
    }
}