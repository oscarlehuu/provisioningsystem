using System;
using System.Collections.Generic;

public class VendorSystemCreateOrderRequest
{
    public string PO { get; set; }
    public List<Line> Lines { get; set; }

    public VendorSystemCreateOrderRequest()
    {
        Lines = new List<Line>();
    }

    public class Line
    {
        public string Type { get; set; }
        public string Edition { get; set; }
        public short SimultaneousCalls { get; set; }
        public int Extensions { get; set; }
        public Boolean IsPerpetual = false;
        public byte Quantity { get; set; }
        public short AdditionalInsuranceYears { get; set; }
        public Boolean AddHosting { get; set; }
        public String ResellerId { get; set; }
        public String UpgradeKey { get; set; }
        public bool ShouldSerializeExtensions()
        {
			// Don't serialize the Extensions property if the typeLicense is "Enterprise" or "Professional"
			//return !(Type == "NewLicense" || Type == "Upgrade" && ((Edition == "Professional" || Edition == "Enterprise"))) && !(Type =="RenewAnnual");
			return (Edition == "Startup") || (!(Type == "NewLicense" || Type == "Upgrade" && ((Edition == "Professional" || Edition == "Enterprise"))) && !(Type == "RenewAnnual"));
		}
		public bool ShouldSerializeUpgradeKey()
        {
            return !(Type == "NewLicense");
        }
        public bool ShoudSerializedSimultaneousCalls()
        {
            return !(Type == "RenewAnnual");
        }
        public bool ShouldSerializeAddHosting()
        {
            return !(Type == "RenewAnnual");
        }
        public bool ShouldSerializeEdition()
        {
            return !(Type == "RenewAnnual");
        }
    }
}