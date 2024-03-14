using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class SubscriptionInfoUpdateResponse
{
    [JsonProperty("succeed")]
    public Boolean succeed { get; set; }
    [JsonProperty("errors")]
    public List<error> errors { get; set; }

    public SubscriptionInfoUpdateResponse()
    { 
        errors = new List<error>();
    }

    public class error
    {
        public String errorCode { get; set; }
        public int severity { get; set; }
        public String description { get; set; }
    }
} 