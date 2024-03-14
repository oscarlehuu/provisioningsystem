using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using CRMPlatform.Cloud.ServiceManagersSDK.Libraries.Controllers;
using CRMPlatform.Extensions.Models.Accounts;
using CRMPlatform.Extensions.Models.CustomFields;
using CRMPlatform.Extensions.Models.Fields;
using CRMPlatform.Extensions.Models.ProductTypes;
using CRMPlatform.Extensions.Models.Results;
using CRMPlatform.Extensions.Models.Services;
using CRMPlatform.Extensions.Models.ExternalPricing;
using CRMPlatform.Cloud.ServiceManagersSDK.Libraries.Logs;
using ServiceManager.VendorX.RecurringService.Code;
using ServiceManager.VendorX.RecurringService.Models.CRMPlatform;
using System.Net.Http;
using static VendorSystemPreviewOrderResponse;
using CRMPlatform.Extensions.Models.ExternalPricing.Contract;
using CRMPlatform.Extensions.Models.ExternalPricing.Response;
using static CRMPlatform.Extensions.Models.ProductTypes.RuleDefinition;

using System.IO;
using Newtonsoft.Json;
using System.Web.Helpers;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Office2016.Excel;
using System.Web.Http.Results;
using System.Diagnostics.Contracts;
using DocumentFormat.OpenXml.Bibliography;
using System.Threading.Tasks;






// ----- I try to replicate the way CRMPlatform configure Service Manager for Dropbox / Kaspersky. CRMPlatform puts every API in one file ----- //
namespace ServiceManager.VendorX.RecurringService.Controllers
{

    // ------ This is the main prefix of every api related to vendorSystem ------ //
    [RoutePrefix("api/vendorSystem")]
    public class VendorSystemCRMPlatformController : ServiceManagerBaseController
    {

        // ----- Variable ----- //
        private string vendorApiKey = "";
        private VendorSystemClient vendorSystemClient = new VendorSystemClient();
        private CRMPlatformClient crmPlatformClient = new CRMPlatformClient();
        private string bssApiAccessToken;

        // ----- ----- //

        // --------------------------- --------------------------- --------------------------- --------------------------- --------------------------- --------------------------- //
        private string _username = null;
        private string _password = null;
        private string _globalPartnerId = null;

        protected override void InitializeDerived(HttpControllerContext controllerContext)
        {
            GetHeaderValue(controllerContext.Request.Headers, "FieldUsername", out _username);
            GetHeaderValue(controllerContext.Request.Headers, "FieldPassword", out _password);
        }

        // --------------------------- --------------------------- --------------------------- --------------------------- --------------------------- --------------------------- //


        // ----- Accounts Controller (merge from AccountsController.cs) ----- //
        [Route("Accounts/SyncOptions")]
        public override IHttpActionResult AccountsSychronizationOptions()
        {
            IHttpActionResult result;

            using (new LogTracer(base.LogActionInput, base.Logging, base.ActionName, base.ActionLogUUID, null, null, null, null))
            {
                CustomFieldCollection obj = new CustomFieldCollection
                {
                    Fields = new List<CustomField>
                    {
                        new CustomField("role")
                        {
                            Definition = new CustomFieldDefinition(new Dictionary<string, string>
                            {
                                //{ "1", "User" },
                                { "2", "Reseller" }
                            })
                            {
                                ID = "role",
                                Name = "Role",
                                Kind = CustomFieldKind.List,
                                DataType = CustomFieldDataType.Text,
                                IsRequired = true,
                                AvailableToStorefront = true
                            }
                        },
                        new CustomField("partnerId")
                        {
                            Definition = new CustomFieldDefinition
                            {
                                ID = "partnerId",
                                Name = "Partner ID",
                                Kind = CustomFieldKind.SimpleValue,
                                DataType = CustomFieldDataType.Text,
                                IsRequired = true,
                                AvailableToStorefront= true,
                            }
                        }
                    }
                };
                result = base.SuccessResult<CustomFieldCollection>(base.ActionLogUUID, obj);
            }

            return result;
        }

        [Route("Accounts/Exists")]
        public override IHttpActionResult AccountsExists(AccountDefinition account)
        {
            string empty = string.Empty;
            IHttpActionResult result;
            using (new LogTracer(base.LogActionInput, base.Logging, base.ActionName, base.ActionLogUUID, new List<object>
    {
        account
    }, null, null, null))
            {
                if (account.IsTest)
                {
                    ResultDefinition resultDefinition = new ResultDefinition();
                    Random random = new Random();
                    resultDefinition.Code = 0;
                    resultDefinition.Result = random.Next().ToString();
                    result = base.SuccessResult<ResultDefinition>(base.ActionLogUUID, resultDefinition);
                }
                else
                {
                    ResultDefinition resultDefinition = new ResultDefinition
                    {
                        Code = 0,
                        Message = "vendorSystem_SUCCESS"
                    };
                    result = base.SuccessResult<ResultDefinition>(base.ActionLogUUID, resultDefinition);
                }
            }
            return result;
        }

        private bool IsValidPartnerId(string partnerId) { return !string.IsNullOrEmpty(partnerId) && partnerId.Trim().Length == 6; }

        [Route("Accounts/Synchronize")]
        public override IHttpActionResult AccountsSychronize(AccountDefinition account)
        {
            bool flag = false;
            using (new LogTracer(base.LogActionInput, base.Logging, base.ActionName, base.ActionLogUUID, new List<object>
            {
                account
            }, null, null, null))
            {
                try
                {
                    if (account.IsTest)
                    {
                        ResultDefinition resultDefinition = new ResultDefinition();
                        resultDefinition.Code = 0;
                        resultDefinition.Result = new Random().Next().ToString();
                        return base.SuccessResult<ResultDefinition>(base.ActionLogUUID, resultDefinition);
                    }
                    string text = account.SyncOptions["role"];
                    if (string.IsNullOrEmpty(text))
                    {
                        return base.ErrorResult(base.ActionLogUUID, -1, "Could not get Accoutn Sync Role Information", null, null);
                    }
                    flag = text.Equals("reseller", StringComparison.OrdinalIgnoreCase);
                    bool flag2 = false;
                    object obj;
                    //BillingInformation billingInformation = base.CallBssBillingAPI<BillingInformation>(uri, HttpMethod.Get, BSSBillingVersion.v2_2, base.ActionLogUUID, out flag2, out obj, true);
                    /*if (flag2 || billingInformation == null)
                    {
                        return base.ErrorResult(base.ActionLogUUID, -1, "Could not get BSS Billing Information", null, null);
                    }
                    if (string.IsNullOrEmpty(account.ContactDetails.PrimaryFirstName))
                    {
                        return base.ErrorResult(base.ActionLogUUID, -12, "Provide valid first name", null, null);
                    }
                    if (string.IsNullOrEmpty(account.ContactDetails.PrimaryLastName))
                    {
                        return base.ErrorResult(base.ActionLogUUID, -13, "Provide valid last name", null, null);
                    }
                    if (billingInformation.Addresses.Length == 0)
                    {
                        return base.ErrorResult(base.ActionLogUUID, -15, "Provide billing address for account", null, null);
                    }
                    if (string.IsNullOrEmpty(billingInformation.Addresses[0].CountryCode))
                    {
                        return base.ErrorResult(base.ActionLogUUID, -16, "Provide valid country", null, null);
                    }
                    if (string.IsNullOrEmpty(billingInformation.Addresses[0].City))
                    {
                        return base.ErrorResult(base.ActionLogUUID, -18, "Provide valid city", null, null);
                    }
                    if (string.IsNullOrEmpty(billingInformation.Addresses[0].PostCode))
                    {
                        return base.ErrorResult(base.ActionLogUUID, -19, "Provide valid post code", null, null);
                    }
                    if (flag && !this.IsValidPartnerId(account.SyncOptions["partnerId"].Trim()))
                    {
                        return base.ErrorResult(base.ActionLogUUID, -20, "Provide valid Partner ID", null, null);
                    }*/
                }
                catch (Exception ex)
                {
                    LogsHelper.LogException(ex, base.Logging.Logger, base.ActionLogUUID, base.ActionName, true);
                    return base.ErrorResult(base.ActionLogUUID, -1, ex.Message, null, null);
                }
                AccountResultDefinition obj2 = new AccountResultDefinition
                {
                    Code = 0,
                    Message = "Account synchronized as " + (flag ? "Reseller" : "User"),
                    Result = (flag ? account.SyncOptions["partnerId"].Trim() : null)
                };

                if (flag)
                {
                    _globalPartnerId = account.SyncOptions["partnerId"].Trim();
                }
                else
                {
                    _globalPartnerId = null;
                    return base.ErrorResult(base.ActionLogUUID, -20, "The partner is " + account.SyncOptions["partnerId"] + " but could not save, please try again or contact administrator", null, null);
                }
                return base.SuccessResult<AccountResultDefinition>(base.ActionLogUUID, obj2);
            }
        }

        [Route("Accounts/Delete")]
        public override IHttpActionResult AccountsDelete(AccountDefinition account)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { account }))
            {
                ResultDefinition result = new ResultDefinition()
                {
                    Code = 0,
                    Message = "Account deleted"
                };

                //... implement your delete procedure

                result.Result = account.ExternalID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- //

        // ----- Add-ons Controller (merge from AddONsCtronoller.cs) ----- //

        [Route("Addons/Cancel")]
        public override IHttpActionResult AddonsCancel(ServiceDefinition definition)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { definition }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();

                string externalSubscriptionAddonID = definition.Addons.Where(a => a.ActionType == ActionType.Cancel).FirstOrDefault().ID;

                ///...do something on the other side to update

                // The external subscription ID
                result.Result = externalSubscriptionAddonID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        [Route("Addons/Create")]
        public override IHttpActionResult AddonsCreate(ServiceDefinition definition)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { definition }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();
                result.Result = Guid.NewGuid().ToString();

                return SuccessResult(ActionLogUUID, result);
            }
        }

        [Route("Addons/Update")]
        public override IHttpActionResult AddonsUpdate(ServiceDefinition definition)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { definition }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();

                string externalSubscriptionAddonID = definition.Addons.Where(a => a.ActionType == ActionType.Provision).FirstOrDefault().ID;

                ///...do something on the other side to update

                // The external subscription addon ID
                result.Result = externalSubscriptionAddonID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- //

        // ----- Fields Controller (merge from FieldsController.cs) ----- //

        [Route("Fields/Get")]
        public override IHttpActionResult FieldsGet()
        {
            IHttpActionResult result;
            using (LogTracer tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, null, null, null, null))
            {
                FieldCollection obj = new FieldCollection
                {
                    Fields = new List<Field>
                    {
                        new Field("apikey", new FieldDefinition
                        {
                            ID = "apikey",
                            Name = "API Key",
                            IsRequired = true,
                            Kind = FieldKind.Text,
                            SortOrder = 1,
                            IsAdditionalField = false,
                        })
                    }
                };

                result = SuccessResult<FieldCollection>(ActionLogUUID, obj);
            }

            return result;
        }

        [Route("Fields/Validate")]
        public override IHttpActionResult FieldsValidate(FieldCollection fields)
        {
            List<string> errors = new List<string>() { };

            using (LogTracer tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { fields }))
            {
                var username = fields.Fields.SingleOrDefault(r => r.ID.Equals("apikey")).GetSingleValue().ToString();

                if (username == null)
                {
                    errors.Add("Missing API Key, please contact your service provider for the API Key.");
                }

                return SuccessResult(ActionLogUUID, errors);
            }
        }

        public static FieldCollection GetFieldsCollection()
        {
            return new FieldCollection()
            {
                Fields = new List<Field>()
                {
                    new Field()
                    {
                        ID = "FieldUsername",
                        Definition = new FieldDefinition()
                        {
                            ID = "FieldUsername",
                            Name = "Username",
                            Description = "Username info",
                            Kind = FieldKind.Text,
                            MaxLength = 50,
                            IsRequired = true,
                            SortOrder = 1
                        }
                    },
                    new Field()
                    {
                        ID = "FieldPassword",
                        Definition = new FieldDefinition()
                        {
                            ID = "FieldPassword",
                            Name = "Password",
                            Description = "Password info",
                            Kind = FieldKind.PasswordText,
                            MaxLength = 50,
                            IsRequired = true,
                            SortOrder = 2
                        }
                    }
                }
            };
        }
        public static bool ValidateCredentials(string username, string password)
        {
            bool success = false;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                success = true;

            return success;
        }

        public ICollection<ProductTypeDefinition> GetProductTypes()
        {
            return new List<ProductTypeDefinition>()
            {
                new ProductTypeDefinition("vendorSystemSubscriptionBased", GetAttributes(), GetProducts())
                {
                    ID = "vendorSystemSubscriptionBased",
                    Name = "vendorSystem - Integration Service Manager",
                    Description = "Subscription based services provided by vendorSystem",
                    Derivative = Derivative.SUBSCRIPTION,
                    PortalUrl = "https://vendorSystem.com"
                }
            };
        }

        private List<PredefinedValueDefinition> GetLicenseTypeValues()
        {
            return new List<PredefinedValueDefinition>
            {
                new PredefinedValueDefinition()
                {
                    ID = "NewLicense",
                    Code = "NewLicense",
                    Name = "New License",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "RenewAnnual",
                    Code = "RenewAnnual",
                    Name = "Renew Annual",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "Upgrade",
                    Code = "Upgrade",
                    Name = "Upgrade",
                    IsDefault = true,
                }
            };
        }

        private List<PredefinedValueDefinition> GetLicenseEditionValues()
        {
            return new List<PredefinedValueDefinition>()
            {
                   new PredefinedValueDefinition()
                   {
                       ID = "None",
                       Code = "None",
                       Name = "None",
                       IsDefault = true,
                   },
                   new PredefinedValueDefinition()
                   {
                       ID = "Startup",
                       Code = "Startup",
                       Name = "Startup",
                       IsDefault = true,
                   },
                   new PredefinedValueDefinition()
                   {
                       ID = "Professional",
                       Code = "Professional",
                       Name = "Professional",
                       IsDefault = true,
                   },
                   new PredefinedValueDefinition()
                   {
                       ID = "Enterprise",
                       Code = "Enterprise",
                       Name = "Enterprise",
                       IsDefault = true,
                   },
            };
        }

        private List<PredefinedValueDefinition> GetSimultaneousCallsValues()
        {
            return new List<PredefinedValueDefinition>
            {
                new PredefinedValueDefinition()
                {
                    ID = "None",
                    Code = "None",
                    Name = "None",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "4",
                    Code = "4",
                    Name = "4",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "8",
                    Code = "8",
                    Name = "8",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "16",
                    Code = "16",
                    Name = "16",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "24",
                    Code = "24",
                    Name = "24",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "32",
                    Code = "32",
                    Name = "32",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "48",
                    Code = "48",
                    Name = "48",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "64",
                    Code = "64",
                    Name = "64",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "96",
                    Code = "96",
                    Name = "96",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "128",
                    Code = "128",
                    Name = "128",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "192",
                    Code = "192",
                    Name = "192",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "256",
                    Code = "256",
                    Name = "256",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "512",
                    Code = "512",
                    Name = "512",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "1024",
                    Code = "1024",
                    Name = "1024",
                    IsDefault = true,
                }
            };
        }
        private List<PredefinedValueDefinition> GetIsPerpetualValues()
        {
            return new List<PredefinedValueDefinition>
            {
                new PredefinedValueDefinition()
                {
                    ID = "False",
                    Code = "False",
                    Name = "False",
                    IsDefault = true,
                }
            };
        }
        private List<PredefinedValueDefinition> GetAdditionalInsuranceYears()
        {
            return new List<PredefinedValueDefinition>
            {
                new PredefinedValueDefinition()
                {
                    ID = "0",
                    Code = "0",
                    Name = "0",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "1",
                    Code = "1",
                    Name = "1",
                    IsDefault = true,
                }
            };
        }
        private List<PredefinedValueDefinition> GetPartnerId()
        {
            return new List<PredefinedValueDefinition>()
            {
                new PredefinedValueDefinition()
                {
                    ID = "partnerId",
                    Code = "partnerID",
                    Name = "Partner ID",
                    IsDefault = true,
                }
            };
        }
        private List<PredefinedValueDefinition> GetAddHostingValues()
        {
            return new List<PredefinedValueDefinition>()
            {
                new PredefinedValueDefinition()
                {
                    ID = "None",
                    Code = "None",
                    Name = "None",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "false",
                    Code = "false",
                    Name = "false",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "true",
                    Code = "true",
                    Name = "true",
                    IsDefault = true,
                }
            };
        }
        private List<PredefinedValueDefinition> GetExtensionValues()
        {
            return new List<PredefinedValueDefinition>()
            {
                new PredefinedValueDefinition()
                {
                    ID = "None",
                    Code = "None",
                    Name = "None",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "10",
                    Code = "10",
                    Name = "10",
                    IsDefault = true,
                },
                new PredefinedValueDefinition()
                {
                    ID = "20",
                    Code = "20",
                    Name = "20",
                    IsDefault = true,
                }
            };
        }

        // ----- Attributes for vendorSystem Pre-configured product ----- //
        private IList<AttributeDefinition> GetAttributes()
        {
            return new List<AttributeDefinition>
            {
                new AttributeDefinition(this.GetLicenseTypeValues())
                {
                    ID = "Type",
                    Name = "License Type",
                    Description = "License Type",
                    IsRequired = true,
                    SortOrder = 1,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.ProductCharacteristic,
                    UsageSpecified = true,
                },
                 new AttributeDefinition(this.GetLicenseEditionValues())
                 {
                    ID = "Edition",
                    Name = "Edition",
                    Description = "Edition",
                    IsRequired = true,
                    SortOrder = 1,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.ProductCharacteristic,
                    UsageSpecified= true,
                 },
                new AttributeDefinition(this.GetSimultaneousCallsValues())
                {
                    ID = "SimultaneousCalls",
                    Name = "Simultaneous Calls",
                    Description = "Simultaneous Calls",
                    IsRequired = true,
                    SortOrder = 2,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.ProductCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition(this.GetAdditionalInsuranceYears())
                {
                    ID = "AdditionalInsuranceYears",
                    Name = "Additional Insurance Years",
                    Description = "Additional Insurance Years",
                    IsRequired = true,
                    SortOrder = 3,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.HiddenCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition(this.GetExtensionValues())
                {
                    ID = "Extension",
                    Name = "Extension",
                    Description = "Extension",
                    IsRequired = true,
                    SortOrder = 4,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.ProductCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition(this.GetAddHostingValues())
                {
                    ID = "AddHosting",
                    Name = "Add Hosting",
                    Description = "Add Hosting",
                    IsRequired = true,
                    SortOrder = 5,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.ProductCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition()
                {
                    ID = "UpgradeKey",
                    Name = "License Key",
                    Description = "Renew License Key",
                    IsRequired = true,
                    SortOrder = 6,
                    Kind = AttributeKind.Text,
                    KindSpecified = true,
                    Usage = AttributeUsage.OrderCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition()
                {
                    ID = "PO",
                    Name = "PO",
                    Description = "PO",
                    IsRequired = false,
                    SortOrder = 7,
                    Kind = AttributeKind.Text,
                    KindSpecified = true,
                    Usage = AttributeUsage.HiddenCharacteristic,
                    UsageSpecified = true,
                },
            };
        }

        // ----- Attributes for vendorSystem - Upgrade license key that was not purchased from LC ----- //
        private IList<AttributeDefinition> GetAttributesForvendorSystemUpgrade()
        {
            return new List<AttributeDefinition>
            {
                new AttributeDefinition(this.GetLicenseTypeValues())
                {
                    ID = "Type",
                    Name = "License Type",
                    Description = "License Type",
                    IsRequired = true,
                    SortOrder = 1,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.ProductCharacteristic,
                    UsageSpecified = true,
                },
                 new AttributeDefinition(this.GetLicenseEditionValues())
                 {
                    ID = "Edition",
                    Name = "Edition",
                    Description = "Edition",
                    IsRequired = true,
                    SortOrder = 1,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.OrderCharacteristic,
                    UsageSpecified= true,
                 },
                new AttributeDefinition(this.GetSimultaneousCallsValues())
                {
                    ID = "SimultaneousCalls",
                    Name = "Simultaneous Calls",
                    Description = "Simultaneous Calls",
                    IsRequired = true,
                    SortOrder = 2,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.OrderCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition(this.GetAdditionalInsuranceYears())
                {
                    ID = "AdditionalInsuranceYears",
                    Name = "Additional Insurance Years",
                    Description = "Additional Insurance Years",
                    IsRequired = true,
                    SortOrder = 3,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.HiddenCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition(this.GetExtensionValues())
                {
                    ID = "Extension",
                    Name = "Extension",
                    Description = "Extension",
                    IsRequired = true,
                    SortOrder = 4,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.OrderCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition(this.GetAddHostingValues())
                {
                    ID = "AddHosting",
                    Name = "Add Hosting",
                    Description = "Add Hosting",
                    IsRequired = true,
                    SortOrder = 5,
                    Kind = AttributeKind.PredefinedChooseOne,
                    KindSpecified = true,
                    Usage = AttributeUsage.OrderCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition()
                {
                    ID = "UpgradeKey",
                    Name = "License Key",
                    Description = "Renew License Key",
                    IsRequired = true,
                    SortOrder = 6,
                    Kind = AttributeKind.Text,
                    KindSpecified = true,
                    Usage = AttributeUsage.OrderCharacteristic,
                    UsageSpecified = true,
                },
                new AttributeDefinition()
                {
                    ID = "PO",
                    Name = "PO",
                    Description = "PO",
                    IsRequired = false,
                    SortOrder = 7,
                    Kind = AttributeKind.Text,
                    KindSpecified = true,
                    Usage = AttributeUsage.HiddenCharacteristic,
                    UsageSpecified = true,
                },
            };
        }

        // ----- Product list for vendorSystem pre-cofigured Products ----- //
        private ProductsCollection GetProducts()
        {
            return new ProductsCollection()
            {
                Products = new List<Product>()
                {
                        // Name Rule: vendorSystem - Edition - Pre-choose Configuration - Has/No Hosted (New License)

                        // ------ 1. vendorSystem - Startup Edition - 10 Extensions - No Hosted (New License) ------ //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemStartup_10Extensions_NoHosted",
                            Code = "vendorSystemNAT4M12:E10-H0",
                            Name = "vendorSystem - Startup Edition - 10 Extensions - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemStartup_20Extensions_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                }
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Startup" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "4" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "10" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 105M, Price = 175M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 210M, Price = 350M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 315M, Price = 525M }
                                    }
                                }
                            }
                        },

                        // ----- 2. vendorSystem - Startup Edition - 20 Extensions - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemStartup_20Extensions_NoHosted",
                            Code = "vendorSystemNAT4M12:E20-H0",
                            Name = "vendorSystem - Startup Edition - 20 Extensions - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            /*RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemStartup_20Extensions_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                }
                            },*/
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Startup" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "4" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "10" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 165M, Price = 275M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 333M, Price = 550M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 495M, Price = 825M }
                                    }
                                }
                            }
                        },

                        // ----- 3. vendorSystem - Professional Edition - 4 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_4Calls_NoHosted",
                            Code = "vendorSystemNAP4M12-H0",
                            Name = "vendorSystem - Professional Edition - 4 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 8 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_8Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 16 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_16Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 8 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 4 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_4Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 8 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 4 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_4Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "4" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 150M, Price = 250M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 300M, Price = 500M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 450M, Price = 750M }
                                    }
                                }
                            }
                        },

                        // ----- 4. vendorSystem - Professional Edition - 8 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_8Calls_NoHosted",
                            Code = "vendorSystemNAP8M12-H0",
                            Name = "vendorSystem - Professional Edition - 8 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 16 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_16Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 8 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "8" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 270M, Price = 450M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 540M, Price = 900M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 810M, Price = 1350M }
                                    }
                                }
                            }
                        },

                        // ----- 5. vendorSystem - Professional Edition - 16 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_16Calls_NoHosted",
                            Code = "vendorSystemNAP16M12-H0",
                            Name = "vendorSystem - Professional Edition - 16 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 16 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "16" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 612M, Price = 1020M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 1224M, Price = 2040M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 1836M, Price = 3060M }
                                    }
                                }
                            }
                        },

                        // ----- 6. vendorSystem - Professional Edition - 24 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_24Calls_NoHosted",
                            Code = "vendorSystemNAP24M12-H0",
                            Name = "vendorSystem - Professional Edition - 24 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "24" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 921M, Price = 1535M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 1842M, Price = 3070M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 2763M, Price = 4605M }
                                    }
                                }
                            }
                        },

                        // ----- 7. vendorSystem - Professional Edition - 32 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_32Calls_NoHosted",
                            Code = "vendorSystemNAP32M12-H0",
                            Name = "vendorSystem - Professional Edition - 32 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "32" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1230M, Price = 2050M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 2460M, Price = 4100M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 3690M, Price = 6150M }
                                    }
                                }
                            }
                        },

                        // ----- 8. vendorSystem - Professional Edition - 48 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_48Calls_NoHosted",
                            Code = "vendorSystemNAP48M12-H0",
                            Name = "vendorSystem - Professional Edition - 48 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "48" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1845M, Price = 3075M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 3690M, Price = 6150M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 5535M, Price = 9225M }
                                    }
                                }
                            }
                        },

                        // ----- 9. vendorSystem - Professional Edition - 64 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_64Calls_NoHosted",
                            Code = "vendorSystemNAP64M12-H0",
                            Name = "vendorSystem - Professional Edition - 64 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "64" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 2463M, Price = 4105M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 4926M, Price = 8210M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 7389M, Price = 12315M }
                                    }
                                }
                            }
                        },

                        // ----- 10. vendorSystem - Professional Edition - 96 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_96Calls_NoHosted",
                            Code = "vendorSystemNAP96M12-H0",
                            Name = "vendorSystem - Professional Edition - 96 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "96" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 4158M, Price = 6930M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 8316M, Price = 13860M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 12474M, Price = 20790M }
                                    }
                                }
                            }
                        },

                        // ----- 11. vendorSystem - Professional Edition - 128 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_128Calls_NoHosted",
                            Code = "vendorSystemNAP128M12-H0",
                            Name = "vendorSystem - Professional Edition - 128 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "128" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 5085M, Price = 8475M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 10170M, Price = 16950M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 15255M, Price = 25425M }
                                    }
                                }
                            }
                        },

                        // ----- 12. vendorSystem - Professional Edition - 192 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_192Calls_NoHosted",
                            Code = "vendorSystemNAP192M12-H0",
                            Name = "vendorSystem - Professional Edition - 192 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "196" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 8097M, Price = 13495M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 16194M, Price = 26990M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 24291M, Price = 40485M }
                                    }
                                }
                            }
                        },

                        // ----- 13. vendorSystem - Professional Edition - 256 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_256Calls_NoHosted",
                            Code = "vendorSystemNAP192M12-H0",
                            Name = "vendorSystem - Professional Edition - 256 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "256" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 10791M, Price = 17985M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 21582M, Price = 35970M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 32373M, Price = 53955M }
                                    }
                                }
                            }
                        },

                        // ----- 14. vendorSystem - Professional Edition - 512 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_512Calls_NoHosted",
                            Code = "vendorSystemNAP512M12-H0",
                            Name = "vendorSystem - Professional Edition - 512 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Professional With Hosted ----------------------------------------------///


                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "512" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 16959M, Price = 28265M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 33918M, Price = 56530M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 50877M, Price = 84795M }
                                    }
                                }
                            }
                        },

                        // ----- 15. vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_1024Calls_NoHosted",
                            Code = "vendorSystemNAP1024M12-H0",
                            Name = "vendorSystem - Professional Edition - 1024 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//


                                // --------------------------------- Professional With Hosted ----------------------------------------------///


                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "1024" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 0M, Price = 0M }
                                    }
                                }
                            }
                        },

                        // ----- 16. vendorSystem - Professional Edition - 4 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_4Calls_HasHosted",
                            Code = "vendorSystemNAP4M12-H1",
                            Name = "vendorSystem - Professional Edition - 4 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "4" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 270M, Price = 400M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 540M, Price = 800M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 810M, Price = 1200M }
                                    }
                                }
                            }
                        },

                        // ----- 17. vendorSystem - Professional Edition - 8 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_8Calls_HasHosted",
                            Code = "vendorSystemNAP8M12-H1",
                            Name = "vendorSystem - Professional Edition - 8 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "8" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 470M, Price = 700M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 940M, Price = 1400M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 1410M, Price = 2100M }
                                    }
                                }
                            }
                        },

                        // ----- 18. vendorSystem - Professional Edition - 16 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_16Calls_HasHosted",
                            Code = "vendorSystemNAP16M12-H1",
                            Name = "vendorSystem - Professional Edition - 16 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "16" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 932M, Price = 1420M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 1864M, Price = 2840M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 2796M, Price = 4260M }
                                    }
                                }
                            }
                        },

                        // ----- 19. vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_24Calls_HasHosted",
                            Code = "vendorSystemNAP24M12-H1",
                            Name = "vendorSystem - Professional Edition - 24 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "24" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1241M, Price = 1935M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 2482M, Price = 3870M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 3723M, Price = 5805M }
                                    }
                                }
                            }
                        },

                        // ----- 20. vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_32Calls_HasHosted",
                            Code = "vendorSystemNAP32M12-H1",
                            Name = "vendorSystem - Professional Edition - 32 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "32" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1830M, Price = 2800M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 3660M, Price = 5600M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 5490M, Price = 8400M }
                                    }
                                }
                            }
                        },

                        // ----- 21. vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_48Calls_HasHosted",
                            Code = "vendorSystemNAP48M12-H1",
                            Name = "vendorSystem - Professional Edition - 48 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "48" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 2445M, Price = 3825M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 4890M, Price = 7650M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 7335M, Price = 11475M }
                                    }
                                }
                            }
                        },

                        // ----- 22. vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_64Calls_HasHosted",
                            Code = "vendorSystemNAP64M12-H1",
                            Name = "vendorSystem - Professional Edition - 64 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "64" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 3063M, Price = 4855M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 6126M, Price = 9710M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 9189M, Price = 14565M }
                                    }
                                }
                            }
                        },

                        // ----- 23. vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_96Calls_HasHosted",
                            Code = "vendorSystemNAP96M12-H1",
                            Name = "vendorSystem - Professional Edition - 96 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "96" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 5354M, Price = 8425M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 10708M, Price = 16850M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 16062M, Price = 25275M }
                                    }
                                }
                            }
                        },

                        // ----- 24. vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_128Calls_HasHosted",
                            Code = "vendorSystemNAP128M12-H1",
                            Name = "vendorSystem - Professional Edition - 128 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "128" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 6281M, Price = 9970M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 12562M, Price = 19940M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 18843M, Price = 29910M }
                                    }
                                }
                            }
                        },

                        // ----- 25. vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_192Calls_HasHosted",
                            Code = "vendorSystemNAP192M12-H1",
                            Name = "vendorSystem - Professional Edition - 192 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // ----- vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "192" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 9293M, Price = 14990M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 18586M, Price = 29980M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 27879M, Price = 44970M }
                                    }
                                }
                            }
                        },

                        // ----- 26. vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemProfessional_256Calls_HasHosted",
                            Code = "vendorSystemNAP256M12-H1",
                            Name = "vendorSystem - Professional Edition - 256 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Professional" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "256" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 11987M, Price = 19480M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 23974M, Price = 38960M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 35961M, Price = 58440M }
                                    }
                                }
                            }
                        },

                        // ----- 27. vendorSystem - Enterprise Edition - 4 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_4Calls_NoHosted",
                            Code = "vendorSystemNAE4M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 4 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 8 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 4 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemProfessional_4Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "4" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 183M, Price = 305M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 366M, Price = 610M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 549M, Price = 915M }
                                    }
                                }
                            }
                        },

                        // ----- 28. vendorSystem - Enterprise Edition - 8 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_8Calls_NoHosted",
                            Code = "vendorSystemNAE8M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 8 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 16 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "8" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 333M, Price = 555M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 666M, Price = 1110M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 999M, Price = 1665M }
                                    }
                                }
                            }
                        },

                        // ----- 29. vendorSystem - Enterprise Edition - 16 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_16Calls_NoHosted",
                            Code = "vendorSystemNAE16M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 16 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "16" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 753M, Price = 1255M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 1506M, Price = 2510M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 2259M, Price = 3765M }
                                    }
                                }
                            }
                        },

                        // ----- 30. vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_24Calls_NoHosted",
                            Code = "vendorSystemNAE24M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 24 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "24" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1134M, Price = 1890M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 2268M, Price = 3780M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 3402M, Price = 5670M }
                                    }
                                }
                            }
                        },

                        // ----- 31. vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_32Calls_NoHosted",
                            Code = "vendorSystemNAE32M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 32 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "32" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1515M, Price = 2525M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 3030M, Price = 5050M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 4545M, Price = 7575M }
                                    }
                                }
                            }
                        },

                        // ----- 32. vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_48Calls_NoHosted",
                            Code = "vendorSystemNAE48M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 48 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "48" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 2274M, Price = 3790M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 4548M, Price = 7580M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 6822M, Price = 11370M }
                                    }
                                }
                            }
                        },

                        // ----- 33. vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_64Calls_NoHosted",
                            Code = "vendorSystemNAE64M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 64 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "64" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 3036M, Price = 5060M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 6072M, Price = 10120M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 9108M, Price = 15180M }
                                    }
                                }
                            }
                        },

                        // ----- 34. vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_96Calls_NoHosted",
                            Code = "vendorSystemNAE96M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 96 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "96" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 5127M, Price = 8545M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 10254M, Price = 17090M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 15381M, Price = 25635M }
                                    }
                                }
                            }
                        },

                        // ----- 35. vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_128Calls_NoHosted",
                            Code = "vendorSystemNAE128M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 128 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "128" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 6267M, Price = 10445M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 12534M, Price = 20890M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 18801M, Price = 31335M }
                                    }
                                }
                            }
                        },

                        // ----- 36. vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_192Calls_NoHosted",
                            Code = "vendorSystemNAE192M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 192 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "192" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 9984M, Price = 16640M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 19968M, Price = 33280M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 29952M, Price = 49920M }
                                    }
                                }
                            }
                        },

                        // ----- 37. vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_256Calls_NoHosted",
                            Code = "vendorSystemNAE256M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 256 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "256" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 13302M, Price = 22170M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 26604M, Price = 44340M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 39906M, Price = 66510M }
                                    }
                                }
                            }
                        },

                        // ----- 38. vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_512Calls_NoHosted",
                            Code = "vendorSystemNAE512M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 512 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                                    Relation = ProductRelation.Upgrade
                                },

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "512" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 20910M, Price = 34850M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 41820M, Price = 69700M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 62730M, Price = 104550M }
                                    }
                                }
                            }
                        },

                        // ----- 39. vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_1024Calls_NoHosted",
                            Code = "vendorSystemNAE1024M12-H0",
                            Name = "vendorSystem - Enterprise Edition - 1024 Calls - No Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "1024" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 38022M, Price = 63370M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 76044M, Price = 126740M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 114066M, Price = 190110M }
                                    }
                                }
                            }
                        },

                        // ----- 40. vendorSystem - Enterprise Edition - 4 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_4Calls_HasHosted",
                            Code = "vendorSystemNAE4M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 4 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_8Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "4" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 303M, Price = 455M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 606M, Price = 910M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 909M, Price = 1365M }
                                    }
                                }
                            }
                        },

                        // ----- 41. vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_8Calls_HasHosted",
                            Code = "vendorSystemNAE8M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 8 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "8" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 533M, Price = 805M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 1066M, Price = 1610M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 1599M, Price = 2415M }
                                    }
                                }
                            }
                        },

                        // ----- 42. vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_16Calls_HasHosted",
                            Code = "vendorSystemNAE16M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 16 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "16" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1073M, Price = 1655M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 2146M, Price = 3310M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 3219M, Price = 4965M }
                                    }
                                }
                            }
                        },

                        // ----- 43. vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_24Calls_HasHosted",
                            Code = "vendorSystemNAE24M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 24 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "24" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 1454M, Price = 2290M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 2908M, Price = 4580M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 4362M, Price = 6870M }
                                    }
                                }
                            }
                        },

                        // ----- 44. vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_32Calls_HasHosted",
                            Code = "vendorSystemNAE32M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 32 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "32" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 2115M, Price = 3275M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 4230M, Price = 6550M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 6345M, Price = 9825M }
                                    }
                                }
                            }
                        },

                        // ----- 45. vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_48Calls_HasHosted",
                            Code = "vendorSystemNAE48M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 48 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "48" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 2874M, Price = 4540M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 5748M, Price = 9080M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 8622M, Price = 13620M }
                                    }
                                }
                            }
                        },

                        // ----- 46. vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_64Calls_HasHosted",
                            Code = "vendorSystemNAE64M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 64 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "64" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 3636M, Price = 5810M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 7272M, Price = 11620M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 10908M, Price = 17430M }
                                    }
                                }
                            }
                        },

                        // ----- 47. vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_96Calls_HasHosted",
                            Code = "vendorSystemNAE96M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 96 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "96" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 6323M, Price = 10040M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 12646M, Price = 20080M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 18969M, Price = 30120M }
                                    }
                                }
                            }
                        },

                        // ----- 48. vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_128Calls_HasHosted",
                            Code = "vendorSystemNAE128M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 128 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "128" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 7463M, Price = 11940M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 14926M, Price = 23880M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 22389M, Price = 35820M }
                                    }
                                }
                            }
                        },

                        // ----- 49. vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                            Code = "vendorSystemNAE192M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 192 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "192" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 11180M, Price = 18135M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 22360M, Price = 36270M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 33540M, Price = 54405M }
                                    }
                                }
                            }
                        },

                        // ----- 50. vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                        new Product()
                        {
                            ID = "NewLicense_vendorSystemEnterprise_256Calls_HasHosted",
                            Code = "vendorSystemNAE256M12-H1",
                            Name = "vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License)",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            RelatedProducts = new List<ProductRelationDefinition>()
                            {
                                // --------------------------------- Professional No Hosted ----------------------------------------------//

                                // --------------------------------- Professional With Hosted ----------------------------------------------///

                                // --------------------------------- Enterprise No Hosted ----------------------------------------------//

                                // --------------------------------- Enterprise Has Hosted ----------------------------------------------//

                                // ----- vendorSystem - Enterprise Edition - 256 Calls - Has Hosted (New License) ----- //
                                new ProductRelationDefinition()
                                {
                                    ProductID = "NewLicense_vendorSystemEnterprise_192Calls_HasHosted",
                                    Relation = ProductRelation.Upgrade
                                },
                            },
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "NewLicense" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Enterprise" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "256" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                }
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 43494M, Price = 70995M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 28996M, Price = 47330M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 14498M, Price = 23665M }
                                    }
                                }
                            }
                        },

                        // ----- 51. vendorSystem - Renew Annual
                        new Product()
                        {
                            ID = "RenewAnnual_vendorSystem",
                            Code = "vendorSystemNRA",
                            Name = "vendorSystem - Renew Annual",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",                           
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "RenewAnnual" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "None" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "SimultaneousCalls",
                                    Values = new List<object>() { "None" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "None" }
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Extension",
                                    Values = new List<object>() { "None" }
                                },
								new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
								{
									ID = "UpgradeKey",
									Values = new List<object>() { "" }
								}
							},
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 0M, Price = 0M }
                                    }
                                }
                            }
                        },
                },

            };
        }

        // ----- vendorSystem - Upgrade Product ----- //
        private ProductsCollection GetvendorSystemUpgradeProduct()
        {
            return new ProductsCollection
            {
                Products = new List<Product>()
                {
                    new Product()
                        {
                            ID = "Upgrade_vendorSystem_Startup",
                            Code = "Startup_vendorSystemUpgrade-H0",
                            Name = "vendorSystem - Upgrade - For Startup Edition",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing, UpdateOptions.AllowsCustomEndDate },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "Upgrade" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "Startup" },
                                },
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 0M, Price = 0M }
                                    }
                                }
                            }
                        },
                    new Product()
                        {
                            ID = "Upgrade_vendorSystem_Pro_Ent_H0",
                            Code = "Pro_Ent_vendorSystemUpgrade-H0",
                            Name = "vendorSystem - Upgrade - For Enterprise / Professional Edition - No Hosted",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing, UpdateOptions.AllowsCustomEndDate },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "Upgrade" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "None" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "false" },
                                },
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 0M, Price = 0M }
                                    }
                                }
                            }
                        },
                    new Product()
                        {
                            ID = "Upgrade_vendorSystem_Pro_Ent_H1",
                            Code = "Pro_Ent_vendorSystemUpgrade-H1",
                            Name = "vendorSystem - Upgrade - For Enterprise / Professional Edition - Has Hosted",
                            UpdateOptions = new List<UpdateOptions>() { UpdateOptions.Name, UpdateOptions.Prices, UpdateOptions.UnitBillingCycles, UpdateOptions.RelatedProducts, UpdateOptions.ExternalPricing, UpdateOptions.AllowsCustomEndDate },
                            UnitBillingCycles = new List<ProductUnitBillingCycle>(){ ProductUnitBillingCycle.Annually, ProductUnitBillingCycle.TwoYears, ProductUnitBillingCycle.ThreeYears },
                            ExternalPricing = "ExternalPricing_vendorSystem",
                            Attributes = new List<CRMPlatform.Extensions.Models.ProductTypes.Attribute>()
                            {
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Type",
                                    Values = new List<object>() { "Upgrade" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "Edition",
                                    Values = new List<object>() { "None" },
                                },
                                new CRMPlatform.Extensions.Models.ProductTypes.Attribute()
                                {
                                    ID = "AddHosting",
                                    Values = new List<object>() { "true" },
                                },
                            },
                            Prices = new List<PriceDefinition>()
                            {
                                new PriceDefinition()
                                {
                                    Currency = "AUD",
                                    Units = new List<UnitDefinition>()
                                    {
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.Annually, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.TwoYears, Cost = 0M, Price = 0M },
                                        new UnitDefinition() { BillingCycle = ProductUnitBillingCycle.ThreeYears, Cost = 0M, Price = 0M }
                                    }
                                }
                            }
                        },
                }
            };
        }

        [Route("Fields/ServiceDefinition")]
        public override IHttpActionResult FieldsServiceDefinition()
        {
            IHttpActionResult result;
            IHttpActionResult resultForvendorSystemUpgrade;
            using (new LogTracer(base.LogActionInput, base.Logging, base.ActionName, base.ActionLogUUID, null, null, null, null))
            {
                try
                {
                    ResultDefinition resultDefinition = new ResultDefinition();
                    if ("test".Equals(this.vendorApiKey.ToLower()))
                    {
                        Random random = new Random();
                        resultDefinition.Code = 0;
                        resultDefinition.Result = random.Next().ToString();
                        result = base.SuccessResult<ResultDefinition>(base.ActionLogUUID, resultDefinition);
                    }
                    else
                    {
                        // ----- Product Type Collection for vendorSystem Pre-configured Products ----- //
                        ProductTypeCollection productTypeCollection = new ProductTypeCollection();

                        // ----- Rule List of vendorSystem Pre-configured Products ----- //
                        ProductTypeCollection productTypeCollectionForvendorSystemUpgrade = new ProductTypeCollection();

                        // ----- Rule List for vendorSystem Pre-configured Products ----- //
                        List<RuleDefinition> ruleList = new List<RuleDefinition>();

                        List<RuleCondition> licenseTypeCondition = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "Type",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "New License",

                                RuleField = "UpgradeKey",
                                RuleOperator = RuleOperator.IsNotAvailable,
                            }
                        };

                        RuleDefinition licenseTypeDefintion = new RuleDefinition(licenseTypeCondition)
                        {
                            ID = "licenseTypeDefinition",
                            Name = "License Type Definition",
                            Description = "New License does not need License Key",
                        };

                        ruleList.Add(licenseTypeDefintion);

                        List<RuleCondition> startupEditionRuleCondition1 = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "Edition",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "Startup",

                                RuleField = "SimultaneousCalls",
                                RuleOperator = RuleOperator.IsNotAvailable,
                            }
                        };

                        RuleDefinition startUpEditionRuleDefinition1 = new RuleDefinition(startupEditionRuleCondition1)
                        {
                            ID = "startupEditionRule1",
                            Name = "Startup Edition Rule - No Choice of Calls",
                            Description = "Startup Edition does not need Calls",
                        };

                        ruleList.Add(startUpEditionRuleDefinition1);

                        List<RuleCondition> startupEditionRuleCondition2 = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "Edition",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "Startup",

                                RuleField = "AddHosting",
                                RuleOperator = RuleOperator.IsEqualTo,
                                RuleValue = "false"
                            }
                        };

                        RuleDefinition startUpEditionRuleDefinition2 = new RuleDefinition(startupEditionRuleCondition2)
                        {
                            ID = "startupEditionRule2",
                            Name = "Startup Edition Rule - No Choice of Hosting",
                            Description = "Startup Edition does not need Hosting",
                        };

                        ruleList.Add(startUpEditionRuleDefinition2);

                        List<RuleCondition> callsBasedOnHostingRuleCondition1 = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "AddHosting",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "true",

                                RuleField = "SimultaneousCalls",
                                RuleOperator = RuleOperator.IsEqualTo,
                                RuleValue = "4;8;16;24;32;48;64;96;128;192;256",
                            }
                        };

                        RuleDefinition callsBasedOnHostingRuleDefinitionForAddHostingTrue = new RuleDefinition(callsBasedOnHostingRuleCondition1)
                        {
                            ID = "callsBasedOnHostingRule1",
                            Name = "Calls Options Based on Hosting Rule If AddHosting is True",
                            Description = "AddHosting just has 3 options"
                        };

                        ruleList.Add(callsBasedOnHostingRuleDefinitionForAddHostingTrue);

                        List<RuleCondition> callsBasedOnHostingRuleCondition2 = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "AddHosting",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "false",

                                RuleField = "SimultaneousCalls",
                                RuleOperator = RuleOperator.NotContains,
                                RuleValue = "None",
                            }
                        };

                        RuleDefinition callsBasedOnHostingRuleDefinitionForAddHostingFalse = new RuleDefinition(callsBasedOnHostingRuleCondition2)
                        {
                            ID = "callsBasedOnHostingRule",
                            Name = "Calls Options Based on Hosting Rule If AddHosting is False",
                            Description = "AddHosting just has 3 options"
                        };

                        ruleList.Add(callsBasedOnHostingRuleDefinitionForAddHostingFalse);

                        List<RuleCondition> noCallsInRenewAnnualRuleCondition = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "Type",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "Renew Annual",

                                RuleField = "SimultaneousCalls",
                                RuleOperator = RuleOperator.IsNotAvailable,
                            }
                        };

                        RuleDefinition noCallsInRenewAnnualRuleDefinition = new RuleDefinition(noCallsInRenewAnnualRuleCondition)
                        {
                            ID = "noCallsInRenewAnnualRule",
                            Name = "No Calls in Renew Annual Rule",
                            Description = "Renew Annual does not need Calls",
                        };

                        ruleList.Add(noCallsInRenewAnnualRuleDefinition);

                        // ------ ------ ------ //

                        // ----- Rule List of vendorSystem Upgrade Products ----- //
                        /*List<RuleDefinition> ruleListForUpgradeProducts = new List<RuleDefinition>();

                        // ---- Startup Edition does not need Calls ----- //
                        List<RuleCondition> startupEditionConditionNoCallsForUpgrade = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "Edition",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "Startup",

                                RuleField = "Calls",
                                RuleOperator = RuleOperator.IsNotAvailable,
                            }
                        };
                        RuleDefinition startupEditionDefinitionNoCallsForUpgrade = new RuleDefinition(startupEditionConditionNoCallsForUpgrade)
                        {
                            ID = "startupEditionDefinitionNoCallsForUpgrade",
                            Name = "Startup No Calls for Upgrade",
                            Description = "Startup does not need calls for upgrade.",
                        };
                        ruleListForUpgradeProducts.Add(startupEditionDefinitionNoCallsForUpgrade);

                        // ----- Professional and Enterprise Edition don't need Extensions ----- //
                        List<RuleCondition> enterpriseProfessionalEditionConditionNoExtensionsForUpgrade = new List<RuleCondition>
                        {
                            new RuleCondition
                            {
                                ConditionField = "Edition",
                                ConditionOperator = ConditionOperator.IsEqualTo,
                                ConditionValue = "Startup",

                                RuleField = "Calls",
                                RuleOperator = RuleOperator.IsNotAvailable,
                            }
                        };
                        RuleDefinition enterpriseProfessionalEditionDefinitionNoExtensionsForUpgrade = new RuleDefinition(enterpriseProfessionalEditionConditionNoExtensionsForUpgrade)
                        {
                            ID = "enterpriseProfessionalEditionDefinitionNoExtensionsForUpgrade",
                            Name = "No Extensions for Enterprise and Professional",
                            Description = "Enterprise and Professional do not need extensions for upgrade.",
                        };
                        ruleList.Add(enterpriseProfessionalEditionDefinitionNoExtensionsForUpgrade);*/

                        // ----- Product Type Definition for vendorSystem Pre-configured Products ----- //
                        ProductTypeDefinition productTypeDefinition = new ProductTypeDefinition("vendorSystemSubscriptionBased", this.GetAttributes(), ruleList, GetProducts());
                        // ------ Create a collection for ProductTypeDefinition ------ //
                        ICollection<ProductTypeDefinition> collection = new List<ProductTypeDefinition>();

                        // ------ Define ProductTypeDefinition property ------ //
                        productTypeDefinition.Name = "vendorSystem Service Manager";
                        productTypeDefinition.Description = "vendorSystem Service Manager";
                        productTypeDefinition.PortalUrl = "https://vendorSystem.com";
                        productTypeDefinition.Scope = UsageScope.Both;
                        productTypeDefinition.AllowMultipleSubscriptions = true;
                        productTypeDefinition.AutoExecuteAddonCancelRequest = false;
                        productTypeDefinition.AutoExecuteSubscriptionCancelRequest = false;
                        productTypeDefinition.AutoExecuteSubscriptionDowngradeRequest = false;
                        productTypeDefinition.QuantityLimit = 1;
                        productTypeDefinition.QuantityLimitLocked = true;
                        productTypeDefinition.CustomFieldCollection = new CustomFieldCollection
                        {
                            Fields = new List<CustomField>
                            {
                                // ----- General Information ----- //
                                new CustomField("UniqueId")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "UniqueId",
                                        Name = "Unique ID",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 1,
                                    }
                                },
                                new CustomField("TrackingCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "TrackingCode",
                                        Name = "Tracking Code",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 2
                                    }

                                },
                                new CustomField("Currency")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "Currency",
                                        Name = "Currency",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 3
                                    }

                                },
                                new CustomField("AdditionalDiscountPerc")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "AdditionalDiscountPerc",
                                        Name = "Order Additional Discount Perc",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 4
                                    }

                                },
                                new CustomField("AdditionalDiscount")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "AdditionalDiscount",
                                        Name = "Order Additional Discount",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 5
                                    }

                                },
                                new CustomField("SubTotal")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "SubTotal",
                                        Name = "Order Sub Total",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 6
                                    }

                                },
                                new CustomField("TaxPerc")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "TaxPerc",
                                        Name = "Order Tax Perc",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 7
                                    }

                                },
                                new CustomField("Tax")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "Tax",
                                        Name = "Order Tax",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 8
                                    }

                                },
                                new CustomField("GrandTotal")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "GrandTotal",
                                        Name = "Order Grand Total",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 9
                                    }

                                },
                                // ----- License Information ----- //
                                new CustomField("LicenseType")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseType",
                                        Name = "License Type",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 10
                                    }
                                },
                                new CustomField("LicenseProductCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseProductCode",
                                        Name = "License Product Code",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired  = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 11,
                                    }
                                },
                                new CustomField("LicenseSKU")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseSKU",
                                        Name = "License SKU",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 12
                                    }
                                },
                                new CustomField("LicenseProductName")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseProductName",
                                        Name = "License Product Name",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 13
                                    }
                                },
                                new CustomField("LicenseProductDescription")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseProductDescription",
                                        Name = "License Product Description",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 14
                                    }
                                },
                                new CustomField("LicenseUnitPrice")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseUnitPrice",
                                        Name = "License RRP",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 15
                                    }
                                },
                                new CustomField("LicenseDistiDiscount")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseDistiDiscount",
                                        Name = "Distributor Discount",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 16
                                    }
                                },
                                new CustomField("LicenseQuantity")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseQuantity",
                                        Name = "License Quantity",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 17
                                    }
                                },
                                new CustomField("LicenseDistiNet")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseDistiNet",
                                        Name = "License Distributor Cost",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 18,
                                    }
                                },
                                new CustomField("LicenseTax")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseTax",
                                        Name = "License Tax",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 19,
                                    }
                                },
                                new CustomField("ResellerId")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                            ID = "ResellerId",
                                            Name = "Reseller ID",
                                            Kind = CustomFieldKind.SimpleValue,
                                            DataType = CustomFieldDataType.Text,
                                            IsRequired = true,
                                            IsReadOnly = true,
                                            AvailableToStorefront = false,
                                            SortOrder = 20,
                                    }
                                },
                                new CustomField("LicenseResellerPrice")
                                {
                                       Definition = new CustomFieldDefinition
                                       {
                                           ID = "LicenseResellerPrice",
                                           Name = "License Partner Price",
                                           Kind = CustomFieldKind.SimpleValue,
                                           DataType = CustomFieldDataType.Text,
                                           IsRequired = true,
                                           IsReadOnly = true,
                                           AvailableToStorefront = true,
                                           SortOrder = 21
                                       }
                                },
                                new CustomField("PrivateKeyPassword")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "PrivateKeyPassword",
                                        Name = "License Private Key Password",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 22,
                                    }
                                },
                                // ------ License Details ------ //
                                new CustomField("LicenseKey")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseKey",
                                        Name = "License Key",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 23,
                                    }
                                },
                                new CustomField("SimultaneousCalls")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "SimultaneousCalls",
                                        Name = "License Simultaneous Calls",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 24,
                                    }
                                },
                                new CustomField("IsPerpetual")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "IsPerpetual",
                                        Name = "License Perpetual",
                                        Kind = CustomFieldKind.Boolean,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 25
                                    }
                                },
                                new CustomField("Edition")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "Edition",
                                        Name = "License Edition",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 26
                                    }
                                },
                                new CustomField("ExpiryIncludedMonths")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "ExpiryIncludedMonths",
                                        Name = "License Expiry Included Months",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 27,
                                    }
                                },
                                new CustomField("ExpiryDate")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "ExpiryDate",
                                        Name = "License Expiry Date",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 28,
                                    }
                                },
                                new CustomField("MaintenanceIncludedMonths")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "MaintenanceIncludedMonths",
                                        Name = "License Maintenance Included Months",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 29,
                                    }
                                },
                                new CustomField("MaintenanceDate")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "MaintenanceDate",
                                        Name = "License Maintenance Date",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 30,
                                    }
                                },
                                new CustomField("HostingIncludedMonths")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingIncludedMonths",
                                        Name = "License Hosting Included Months",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 31,
                                    }
                                },
                                new CustomField("HostingExpiry")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingExpiry",
                                        Name = "License Hosting Expiry",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 32,
                                    }
                                },
                                new CustomField("LicenseErrorCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseErrorCode",
                                        Name = "License Error Code",
                                        Kind = CustomFieldKind.Unknown,
                                        DataType = CustomFieldDataType.Unknown,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 33,
                                    }
                                },

                                // ----- Hosting Information ----- //
                                new CustomField("HostingType")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingType",
                                        Name = "Hosting Type",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 34,
                                    }
                                },
                                new CustomField("HostingSKU")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingSKU",
                                        Name = "Hosting SKU",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 35,
                                    }
                                },
                                new CustomField("HostingProductName")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingProductName",
                                        Name = "Hosting Product Name",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 36,
                                    }
                                },
                                new CustomField("HostingProductDescription")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingProductDescription",
                                        Name = "Hosting Product Description",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 37,
                                    }
                                },
                                new CustomField("HostingUnitPrice")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingUnitPrice",
                                        Name = "Hosting RRP",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 38
                                    }
                                },
                                new CustomField("HostingDistiDiscount")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingDistiDiscount",
                                        Name = "Hosting Distributor Discount",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 39,
                                    }
                                },
                                new CustomField("HostingQuantity")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingQuantity",
                                        Name = "Hosting Quantity",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 40,
                                    }
                                },
                                new CustomField("HostingDistiNet")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingDistiNet",
                                        Name = "Hosting Distributor Cost",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired=  false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 41,
                                    }
                                },
                                new CustomField("HostingTax")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingTax",
                                        Name = "Hosting Tax",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 42,
                                    }
                                },
                                new CustomField("HostingResellerPrice")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingResellerPrice",
                                        Name = "Hosting Partner Price",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 43
                                    }
                                },
                                new CustomField("HostingErrorCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingErrorCode",
                                        Name = "Hosting Error Code",
                                        Kind = CustomFieldKind.Unknown,
                                        DataType = CustomFieldDataType.Unknown,
                                        IsReadOnly = true,
                                        IsRequired = false,
                                        AvailableToStorefront = false,
                                        SortOrder = 44,
                                    }
                                }
                            }
                        };

                        // ------ Define ExterPricingDefinition Properties ------ //
                        ExternalPricingDefinition externalPricingDefinition = new ExternalPricingDefinition();
                        externalPricingDefinition.ID = "ExternalPricing_vendorSystem";
                        externalPricingDefinition.Description = "vendorSystem External Pricing";
                        externalPricingDefinition.Endpoint = new System.Uri("http://crm_platform.com.au:9098/api/vendorSystem/Pricing/GetPrices");
                        ICollection<ExternalPricingDefinition> externalPricingCollection = new List<ExternalPricingDefinition>();

                        // ------ Add productTypeDefinition to collection ------ //
                        collection.Add(productTypeDefinition);
                        // ------ Add externalPricingDefinition to externalPricingCollection ------ //
                        externalPricingCollection.Add(externalPricingDefinition);

                        // ------- Set ProductTypes in productTypeCollection as collection ------ //
                        productTypeCollection.ProductTypes = collection;
                        // ------- Set ExternalPricing in productTypeCollection as externalPricingCollection ------ //
                        productTypeCollection.ExternalPricing = externalPricingCollection;

                        result = base.SuccessResult<ProductTypeCollection>(base.ActionLogUUID, productTypeCollection);

                        // ----- Product Type for vendorSystem - Upgrade ----- //
                        //ProductTypeDefinition productTypeDefinitionforvendorSystemUpgrade = new ProductTypeDefinition("vendorSystemSubscriptionBased", this.GetAttributesForvendorSystemUpgrade(), ruleListForUpgradeProducts, this.GetvendorSystemUpgradeProduct());
                        // ------ Create a collection for ProductTypeDefinition ------ //
                       // ICollection<ProductTypeDefinition> collectionForvendorSystemProductUpgrade = new List<ProductTypeDefinition>();

                        // ------ Define ProductTypeDefinition property ------ //
                       /* productTypeDefinitionforvendorSystemUpgrade.Name = "vendorSystem Service Manager - For Upgrade";
                        productTypeDefinitionforvendorSystemUpgrade.Description = "vendorSystem Service Manager - For Upgrade";
                        productTypeDefinitionforvendorSystemUpgrade.PortalUrl = "https://vendorSystem.com";
                        productTypeDefinitionforvendorSystemUpgrade.Scope = UsageScope.Both;
                        productTypeDefinitionforvendorSystemUpgrade.AllowMultipleSubscriptions = true;
                        productTypeDefinitionforvendorSystemUpgrade.AutoExecuteAddonCancelRequest = false;
                        productTypeDefinitionforvendorSystemUpgrade.AutoExecuteSubscriptionCancelRequest = false;
                        productTypeDefinitionforvendorSystemUpgrade.AutoExecuteSubscriptionDowngradeRequest = false;
                        productTypeDefinitionforvendorSystemUpgrade.QuantityLimit = 1;
                        productTypeDefinitionforvendorSystemUpgrade.QuantityLimitLocked = true;
                        productTypeDefinitionforvendorSystemUpgrade.CustomFieldCollection = new CustomFieldCollection
                        {
                            Fields = new List<CustomField>
                            {
                                // ----- General Information ----- //
                                new CustomField("UniqueId")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "UniqueId",
                                        Name = "Unique ID",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 1,
                                    }
                                },
                                new CustomField("TrackingCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "TrackingCode",
                                        Name = "Tracking Code",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 2
                                    }

                                },
                                new CustomField("Currency")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "Currency",
                                        Name = "Currency",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 3
                                    }

                                },
                                new CustomField("AdditionalDiscountPerc")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "AdditionalDiscountPerc",
                                        Name = "Order Additional Discount Perc",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 4
                                    }

                                },
                                new CustomField("AdditionalDiscount")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "AdditionalDiscount",
                                        Name = "Order Additional Discount",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 5
                                    }

                                },
                                new CustomField("SubTotal")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "SubTotal",
                                        Name = "Order Sub Total",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 6
                                    }

                                },
                                new CustomField("TaxPerc")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "TaxPerc",
                                        Name = "Order Tax Perc",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 7
                                    }

                                },
                                new CustomField("Tax")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "Tax",
                                        Name = "Order Tax",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 8
                                    }

                                },
                                new CustomField("GrandTotal")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "GrandTotal",
                                        Name = "Order Grand Total",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 9
                                    }

                                },
                                // ----- License Information ----- //
                                new CustomField("LicenseType")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseType",
                                        Name = "License Type",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 10
                                    }
                                },
                                new CustomField("LicenseProductCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseProductCode",
                                        Name = "License Product Code",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired  = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 11,
                                    }
                                },
                                new CustomField("LicenseSKU")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseSKU",
                                        Name = "License SKU",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 12
                                    }
                                },
                                new CustomField("LicenseProductName")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseProductName",
                                        Name = "License Product Name",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 13
                                    }
                                },
                                new CustomField("LicenseProductDescription")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseProductDescription",
                                        Name = "License Product Description",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 14
                                    }
                                },
                                new CustomField("LicenseUnitPrice")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseUnitPrice",
                                        Name = "License RRP",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 15
                                    }
                                },
                                new CustomField("LicenseDistiDiscount")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseDistiDiscount",
                                        Name = "Distributor Discount",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 16
                                    }
                                },
                                new CustomField("LicenseQuantity")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseQuantity",
                                        Name = "License Quantity",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 17
                                    }
                                },
                                new CustomField("LicenseDistiNet")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseDistiNet",
                                        Name = "License Distributor Cost",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 18,
                                    }
                                },
                                new CustomField("LicenseTax")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseTax",
                                        Name = "License Tax",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 19,
                                    }
                                },
                                new CustomField("ResellerId")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                            ID = "ResellerId",
                                            Name = "Reseller ID",
                                            Kind = CustomFieldKind.SimpleValue,
                                            DataType = CustomFieldDataType.Text,
                                            IsRequired = true,
                                            IsReadOnly = true,
                                            AvailableToStorefront = false,
                                            SortOrder = 20,
                                    }
                                },
                                new CustomField("LicenseResellerPrice")
                                {
                                       Definition = new CustomFieldDefinition
                                       {
                                           ID = "LicenseResellerPrice",
                                           Name = "License Partner Price",
                                           Kind = CustomFieldKind.SimpleValue,
                                           DataType = CustomFieldDataType.Text,
                                           IsRequired = true,
                                           IsReadOnly = true,
                                           AvailableToStorefront = true,
                                           SortOrder = 21
                                       }
                                },
                                new CustomField("PrivateKeyPassword")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "PrivateKeyPassword",
                                        Name = "License Private Key Password",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 22,
                                    }
                                },
                                // ------ License Details ------ //
                                new CustomField("LicenseKey")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseKey",
                                        Name = "License Key",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 23,
                                    }
                                },
                                new CustomField("SimultaneousCalls")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "SimultaneousCalls",
                                        Name = "License Simultaneous Calls",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 24,
                                    }
                                },
                                new CustomField("IsPerpetual")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "IsPerpetual",
                                        Name = "License Perpetual",
                                        Kind = CustomFieldKind.Boolean,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 25
                                    }
                                },
                                new CustomField("Edition")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "Edition",
                                        Name = "License Edition",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 26
                                    }
                                },
                                new CustomField("ExpiryIncludedMonths")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "ExpiryIncludedMonths",
                                        Name = "License Expiry Included Months",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 27,
                                    }
                                },
                                new CustomField("ExpiryDate")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "ExpiryDate",
                                        Name = "License Expiry Date",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 28,
                                    }
                                },
                                new CustomField("MaintenanceIncludedMonths")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "MaintenanceIncludedMonths",
                                        Name = "License Maintenance Included Months",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 29,
                                    }
                                },
                                new CustomField("MaintenanceDate")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "MaintenanceDate",
                                        Name = "License Maintenance Date",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = true,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 30,
                                    }
                                },
                                new CustomField("HostingIncludedMonths")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingIncludedMonths",
                                        Name = "License Hosting Included Months",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 31,
                                    }
                                },
                                new CustomField("HostingExpiry")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingExpiry",
                                        Name = "License Hosting Expiry",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 32,
                                    }
                                },
                                new CustomField("LicenseErrorCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "LicenseErrorCode",
                                        Name = "License Error Code",
                                        Kind = CustomFieldKind.Unknown,
                                        DataType = CustomFieldDataType.Unknown,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 33,
                                    }
                                },

                                // ----- Hosting Information ----- //
                                new CustomField("HostingType")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingType",
                                        Name = "Hosting Type",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 34,
                                    }
                                },
                                new CustomField("HostingSKU")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingSKU",
                                        Name = "Hosting SKU",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 35,
                                    }
                                },
                                new CustomField("HostingProductName")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingProductName",
                                        Name = "Hosting Product Name",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 36,
                                    }
                                },
                                new CustomField("HostingProductDescription")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingProductDescription",
                                        Name = "Hosting Product Description",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 37,
                                    }
                                },
                                new CustomField("HostingUnitPrice")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingUnitPrice",
                                        Name = "Hosting RRP",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 38
                                    }
                                },
                                new CustomField("HostingDistiDiscount")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingDistiDiscount",
                                        Name = "Hosting Distributor Discount",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 39,
                                    }
                                },
                                new CustomField("HostingQuantity")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingQuantity",
                                        Name = "Hosting Quantity",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 40,
                                    }
                                },
                                new CustomField("HostingDistiNet")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingDistiNet",
                                        Name = "Hosting Distributor Cost",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired=  false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 41,
                                    }
                                },
                                new CustomField("HostingTax")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingTax",
                                        Name = "Hosting Tax",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = false,
                                        SortOrder = 42,
                                    }
                                },
                                new CustomField("HostingResellerPrice")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingResellerPrice",
                                        Name = "Hosting Partner Price",
                                        Kind = CustomFieldKind.SimpleValue,
                                        DataType = CustomFieldDataType.Text,
                                        IsRequired = false,
                                        IsReadOnly = true,
                                        AvailableToStorefront = true,
                                        SortOrder = 43
                                    }
                                },
                                new CustomField("HostingErrorCode")
                                {
                                    Definition = new CustomFieldDefinition
                                    {
                                        ID = "HostingErrorCode",
                                        Name = "Hosting Error Code",
                                        Kind = CustomFieldKind.Unknown,
                                        DataType = CustomFieldDataType.Unknown,
                                        IsReadOnly = true,
                                        IsRequired = false,
                                        AvailableToStorefront = false,
                                        SortOrder = 44,
                                    }
                                }
                            }
                        };
*/
                        // ------ Define ExterPricingDefinition Properties ------ //
                        /*ExternalPricingDefinition externalPricingDefinitionForvendorSystemUpgrade = new ExternalPricingDefinition();
                        externalPricingDefinitionForvendorSystemUpgrade.ID = "ExternalPricing_vendorSystem";
                        externalPricingDefinitionForvendorSystemUpgrade.Description = "vendorSystem External Pricing";
                        externalPricingDefinitionForvendorSystemUpgrade.Endpoint = new System.Uri("http://crm_platform.com.au:9098/api/vendorSystem/Pricing/GetPrices");
                        ICollection<ExternalPricingDefinition> externalPricingCollectionForvendorSystemUpgrade = new List<ExternalPricingDefinition>();

                        // ------ Add productTypeDefinition to collection ------ //
                        collectionForvendorSystemProductUpgrade.Add(productTypeDefinitionforvendorSystemUpgrade);
                        // ------ Add externalPricingDefinition to externalPricingCollection ------ //
                        externalPricingCollectionForvendorSystemUpgrade.Add(externalPricingDefinitionForvendorSystemUpgrade);

                        // ------- Set ProductTypes in productTypeCollection as collection ------ //
                        productTypeCollectionForvendorSystemUpgrade.ProductTypes = collectionForvendorSystemProductUpgrade;
                        // ------- Set ExternalPricing in productTypeCollection as externalPricingCollection ------ //
                        productTypeCollectionForvendorSystemUpgrade.ExternalPricing = externalPricingCollectionForvendorSystemUpgrade;

                        resultForvendorSystemUpgrade = base.SuccessResult<ProductTypeCollection>(base.ActionLogUUID, productTypeCollectionForvendorSystemUpgrade);*/
                    }
                }
                catch (Exception ex)
                {
                    LogsHelper.LogException(ex, base.Logging.Logger, base.ActionLogUUID, base.ActionName, true);
                    result = base.ErrorResult(base.ActionLogUUID, -1, ex.Message, null, null);
                    resultForvendorSystemUpgrade = base.ErrorResult(base.ActionLogUUID, -1, ex.Message, null, null);
                }
            }

            return result;
        }

        [Route("Fields/AdditionalInfo")]
        public override IHttpActionResult GetAdditionalInfo()
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID))
            {
                Dictionary<string, string> additionalInfo = new Dictionary<string, string>();
                additionalInfo.Add("ProfileId", System.Guid.NewGuid().ToString());

                return SuccessResult(ActionLogUUID, additionalInfo);
            }
        }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- //

        // ----- Subscriptions Controller (merge from Subscriptions.cs) ----- //
        [Route("Subscriptions/Activate")]
        public override IHttpActionResult SubscriptionsActivate(ServiceDefinition definition)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { definition }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();

                string externalSubscriptionID = definition.ID;

                ///...do something on the other side to activate

                // The external subscription ID
                result.Result = externalSubscriptionID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        [Route("Subscriptions/Cancel")]
        public override IHttpActionResult SubscriptionsCancel(ServiceDefinition definition)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { definition }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();

                string externalSubscriptionID = definition.ID;

                ///...do something on the other side to cancel

                // The external subscription ID
                result.Result = externalSubscriptionID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        // ----- ExternalPricingResponseItem ----- //
        private ExternalPricingResponseItem GetExternalPricingItem(ExternalPricingContract contract, ExternalPricingContractItem item)
        {
            // ----- vendorSystem Preview Order Response ----- //
            VendorSystemPreviewOrderResponse vendorSystemPreviewOrderResponse = new VendorSystemPreviewOrderResponse();

            // ----- Get Access Token as this API does not design to have Token ----- //
            bssApiAccessToken = crmPlatformClient.GetAccessToken();

            VendorSystemOrderMargin vendorSystemOrderMarginResponse = new VendorSystemOrderMargin();

            // ----- Get vendorSystem Partner ID from Account Custom Fields ----- //
            List<AccountCustomFields> accountCustomFields
                = crmPlatformClient.accountCustomFieldsResponse((contract.BillToAccount.Id).GetValueOrDefault(), bssApiAccessToken, "3", base.ActionName, base.ActionLogUUID, base.Logging);
            string _vendorSystemPartnerId = null;
            if (accountCustomFields != null)
            {
                foreach (var accountCustomField in accountCustomFields)
                {
                    foreach (var accountCustomFieldGroupFields in accountCustomField.groupFields)
                    {
                        if (accountCustomFieldGroupFields.name == "vendorSystem Partner ID")
                        {
                            if (accountCustomFieldGroupFields.values.Any())
                            {
                                _vendorSystemPartnerId = accountCustomFieldGroupFields.values[0].value ?? "";
                            }
                            else
                            {
                                _vendorSystemPartnerId = "";
                            }
                            //_vendorSystemPartnerId = accountCustomFieldGroupFields.values[0].value;
                            break;
                        }
                    }
                }
            }

            vendorSystemOrderMarginResponse = vendorSystemClient.GetMarginData(_vendorSystemPartnerId, base.ActionName, base.ActionLogUUID, base.Logging);

            // ------ vendorSystem Product Details ------ //
            // ------ Extract vendorSystem Order properties from item.ItemCharecteristic ------ //

            // ----- License Type ----- //
            var licenseType = item.Product.ProductCharacteristics.FirstOrDefault(c => c.Name == "License Type").ExternalValue;

            // ----- Edition ----- //
            var edition = item.Product.ProductCharacteristics.FirstOrDefault(c => c.Name == "Edition").ExternalValue;
            if (edition != null) { edition = edition.Replace("Edition_", ""); }

            // ------ SimultaneousCalls ----- //
            var callsString = item.Product.ProductCharacteristics.FirstOrDefault(c => c.Name == "Simultaneous Calls").ExternalValue;
            if (callsString != null)
            {
                callsString = callsString.Replace("SimultaneousCalls_", "").TrimStart('0');
            }
            else if (callsString == null)
            {
                callsString = "0";
            }
            else {
                callsString = "0";
            }
            short.TryParse(callsString, out short calls);

            // ----- AdditionalInsuranceYears ----- //
            short additionalInsuranceYear = 0;

            // ----- AddHosting ------ //
            var addHostingValue = item.Product.ProductCharacteristics.FirstOrDefault(c => c.Name == "Add Hosting").ExternalValue;
            if (addHostingValue != null) { addHostingValue = addHostingValue.Replace("AddHosting_", ""); }
            Boolean.TryParse(addHostingValue, out bool addHosting);

            // ----- PO ----- //
            var po = "LC" + DateTime.Now.ToString("ddMMyyyy@hh:mmtt");
            // ------ Extensions ----- //
            var extensionsString = item.Product.ProductCharacteristics.FirstOrDefault(c => c.Name == "Extension")?.ExternalValue;
            if (extensionsString != null) { extensionsString = extensionsString.Replace("Extension_", ""); } else { extensionsString = "0"; }
            int.TryParse(extensionsString, out int extensions);

            // ------ Upgrade Key ----- //
            var upgradeKey = item.ItemCharacteristics.FirstOrDefault(c => c.Name == "License Key")?.Value;

            // ----- ----- ----- //

            // ----- Get Order Billing Cycle (1 Year, 2 Years or 3 Years) ----- //
            int unitValue = item.Unit.Value; // = 12, 24 or 36
            string unitType = item.Unit.Type; // = "month"
            switch (unitValue)
            {
                case 12 when unitType == "month":
                    additionalInsuranceYear = 0;
                    break;
                case 24 when unitType == "month":
                    additionalInsuranceYear = 1;
                    break;
                case 36 when unitType == "month":
                    additionalInsuranceYear = 2;
                    break;
            }
            using (new LogTracer(base.LogActionInput, base.Logging, base.ActionName, base.ActionLogUUID, new List<object> { item }, null, null, null))
            {
                VendorSystemCreateOrderRequest request = null;
                switch (licenseType)
                {
                    case "Type_NewLicense":
                        switch (edition)
                        {
                            case "Professional":
                            case "Enterprise":
                                switch (addHosting)
                                {
                                    case true:
                                        request = new VendorSystemCreateOrderRequest
                                        {
                                            PO = po,
                                            Lines = new List<VendorSystemCreateOrderRequest.Line>
                                                    {
                                                        new VendorSystemCreateOrderRequest.Line
                                                        {
                                                            Type = "NewLicense",
                                                            Edition = edition,
                                                            SimultaneousCalls = calls,
                                                            IsPerpetual = false,
                                                            Quantity = 1,
                                                            AdditionalInsuranceYears = additionalInsuranceYear,
                                                            ResellerId = _vendorSystemPartnerId, //Replace with _vendorSystemPartnerId
                                                            AddHosting = true
                                                        }
                                                    }
                                        };
                                        break;
                                    case false:
                                        request = new VendorSystemCreateOrderRequest
                                        {
                                            PO = po,
                                            Lines = new List<VendorSystemCreateOrderRequest.Line>
                                                    {
                                                        new VendorSystemCreateOrderRequest.Line
                                                        {
                                                            Type = "NewLicense",
                                                            Edition = edition,
                                                            SimultaneousCalls = calls,
                                                            IsPerpetual = false,
                                                            Quantity = 1,
                                                            AdditionalInsuranceYears = additionalInsuranceYear,
                                                            ResellerId = _vendorSystemPartnerId,
                                                            AddHosting = false
                                                        }
                                                    }
                                        };
                                        break;
                                }
                                break;
                            case "Startup":
                                request = new VendorSystemCreateOrderRequest
                                {
                                    PO = po,
                                    Lines = new List<VendorSystemCreateOrderRequest.Line>
                                            {
                                                  new VendorSystemCreateOrderRequest.Line
                                                  {
                                                      Type = "NewLicense",
                                                      Edition = "Startup",
                                                      IsPerpetual = false,
                                                      Extensions = extensions,
                                                      SimultaneousCalls = 4,
                                                      Quantity = 1,
                                                      AdditionalInsuranceYears = additionalInsuranceYear,
                                                      ResellerId = _vendorSystemPartnerId,
                                                      AddHosting = false
                                                  }
                                            }
                                };
                                break;
                        }
                        break;
                    case "Type_RenewAnnual":
                                request = new VendorSystemCreateOrderRequest
                                {
                                    PO = po,
                                    Lines = new List<VendorSystemCreateOrderRequest.Line>
                                    {
                                                  new VendorSystemCreateOrderRequest.Line
                                                  {
                                                      Type = "RenewAnnual",
                                                      Quantity = (byte)(additionalInsuranceYear+1),
                                                      UpgradeKey = upgradeKey,
                                                      ResellerId = _vendorSystemPartnerId,
                                                      SimultaneousCalls = 4
                                                  }
                                    }
                                };
                        break;
                    case "Type_Upgrade":
                        switch(edition)
                        {
                            case "Professional":
                            case "Enterprise":
                                switch(addHosting)
                                {
                                    case true:
                                        request = new VendorSystemCreateOrderRequest
                                        {
                                            PO = po,
                                            Lines = new List<VendorSystemCreateOrderRequest.Line>
                                            {
                                                new VendorSystemCreateOrderRequest.Line
                                                {
                                                    Type = "Upgrade",
                                                    Edition = edition,
                                                    SimultaneousCalls = calls,
                                                    UpgradeKey = upgradeKey,
                                                    ResellerId = _vendorSystemPartnerId,
                                                    AddHosting = true,
                                                }
                                            }
                                        };
                                        break;
                                    case false:
                                        request = new VendorSystemCreateOrderRequest
                                        {
                                            PO = po,
                                            Lines = new List<VendorSystemCreateOrderRequest.Line>
                                            {
                                                new VendorSystemCreateOrderRequest.Line
                                                {
                                                    Type = "Upgrade",
                                                    Edition = edition,
                                                    SimultaneousCalls = calls,
                                                    UpgradeKey = upgradeKey,
                                                    ResellerId = _vendorSystemPartnerId,
                                                    AddHosting = false,
                                                }
                                            }
                                        };
                                        break;
                                }
                                break;
                            case "Startup":
                                request = new VendorSystemCreateOrderRequest
                                {
                                    PO = po,
                                    Lines = new List<VendorSystemCreateOrderRequest.Line>
                                    {

                                    }
                                };
                                break;
                        }
                        break;
                }
                vendorSystemPreviewOrderResponse = vendorSystemClient.PreviewOrder(request, base.ActionName, base.ActionLogUUID, base.Logging);
            }
            var itemResponse = new ExternalPricingResponseItem()
            {
                Id = item.Id.ToString(),
                GeneratedAt = DateTime.Now
            };


            if (contract.Account.Country == null || string.IsNullOrWhiteSpace(contract.Account.Country.Code))
            {
                itemResponse.Status = new ExternalPricingResponseStatus()
                {
                    Code = -81001,
                    Message = "Account Country information is missing."
                };
            }
            else if (_vendorSystemPartnerId == "" || string.IsNullOrWhiteSpace(_vendorSystemPartnerId))
            {
                itemResponse.Status = new ExternalPricingResponseStatus()
                {
                    Code = -81002,
                    Message = "You have not entered vendorSystem Partner ID. Please update this field by redirectly to My Account -> vendorSystem Partner ID field. Your order won't be able to process. If you have any question, please contact vendorSystemSales@gmail.com.au."
                };
            }
            else if (_vendorSystemPartnerId.Length < 6 || vendorSystemOrderMarginResponse.status == 404)
            {
                itemResponse.Status = new ExternalPricingResponseStatus()
                {
                    Code = -81003,
                    Message = "Your vendorSystem Partner ID is invalid, please check your vendorSystem Partner ID in My Account -> vendorSystem Partner ID field. It must be a string of maximum 6 digits. If you have any question, please contact vendorSystemSales@gmail.com.au."
                };
            }
            else
            {
                itemResponse.CostPrice = Convert.ToDouble(vendorSystemPreviewOrderResponse.Items.Sum(vendorSystemOrderResponseItem => vendorSystemOrderResponseItem.Net));
                itemResponse.SellPrice = Convert.ToDouble(vendorSystemPreviewOrderResponse.Items.Sum(vendorSystemOrderResponseItem => vendorSystemOrderResponseItem.ResellerPrice));

                itemResponse.Status = new ExternalPricingResponseStatus()
                {
                    Code = 0,
                    Message = "Prices Retrieved"
                };
            }

            return itemResponse;
        }

        [HttpPost]
        [Route("Pricing/GetPrices")]
        public IHttpActionResult PricingGetPrices(ExternalPricingContract contract)
        {
            var response = new ExternalPricingResponse();
            response.Currency = "AUD";
            response.Items = new List<ExternalPricingResponseItem>();


            if (contract.Items != null)
            {
                foreach (var item in contract.Items)
                {
                    var itemResponse = GetExternalPricingItem(contract, item);
                    response.Items.Add(itemResponse);
                }
            }

            response.ExchangeRate = new ExternalPricingResponseExchangeRate()
            {
                Currency = "AUD",
                Rate = 0,
                GeneratedAt = DateTime.Now
            };

            return Ok(response);

        }

        [Route("Subscriptions/Create")]
        public override IHttpActionResult SubscriptionsCreate(ServiceDefinition service)
        {

            VendorSystemPreviewOrderResponse vendorSystemPreviewOrderResponse = new VendorSystemPreviewOrderResponse();
            string accessToken = Request.Headers.Contains("X-CloudPlatform-Token") ? Request.Headers.GetValues("X-CloudPlatform-Token").FirstOrDefault() : string.Empty;
            bssApiAccessToken = crmPlatformClient.GetAccessToken();
            VendorSystemOrderMargin vendorSystemOrderMarginResponse = new VendorSystemOrderMargin();
            // ----- Get Bill To Account ID - //
            AccountInfoResponse accountInfoResponse = crmPlatformClient.getAccountInfo(int.Parse(service.Account.ID), bssApiAccessToken, "3", base.ActionName, base.ActionLogUUID, base.Logging);
            int billToAccountId = accountInfoResponse.billToAccount.id;

            // ----- Get vendorSystem Partner ID from Account Custom Fields ----- //
            string _vendorSystemPartnerId = null;
            List<AccountCustomFields> accountCustomFields
                = crmPlatformClient.accountCustomFieldsResponse(billToAccountId, bssApiAccessToken, "3", base.ActionName, base.ActionLogUUID, base.Logging);

            if (accountCustomFields != null)
            {
                foreach (var accountCustomField in accountCustomFields)
                {
                    foreach (var accountCustomFieldGroupFields in accountCustomField.groupFields)
                    {
                        if (accountCustomFieldGroupFields.name == "vendorSystem Partner ID")
                        {
                            if (accountCustomFieldGroupFields.values.Any())
                            {
                                _vendorSystemPartnerId = accountCustomFieldGroupFields.values[0].value ?? "";
                            }
                            else
                            {
                                _vendorSystemPartnerId = "";
                            }
                            //_vendorSystemPartnerId = accountCustomFieldGroupFields.values[0].value;
                            break;
                        }
                    }
                }
            }

            vendorSystemOrderMarginResponse = vendorSystemClient.GetMarginData(_vendorSystemPartnerId, base.ActionName, base.ActionLogUUID, base.Logging);

            string empty = string.Empty;
            using (new LogTracer(base.LogActionInput, base.Logging, base.ActionName, base.ActionLogUUID, new List<object> { service }, null, null, null))
            {
                try
                {
                    if (service.IsTest)
                    {
                        ResultDefinition resultDefinition = new ResultDefinition();
                        Random random = new Random();
                        resultDefinition.Code = 0;
                        resultDefinition.Result = random.Next().ToString();
                        return base.SuccessResult<ResultDefinition>(base.ActionLogUUID, resultDefinition);
                    }
/*                    else
                    {*/
                        string text = service.Account.SyncOptions["role"];
                        /*if (string.IsNullOrEmpty(text) || text.Equals("User"))
                        {
                            return base.ErrorResult(base.ActionLogUUID, -1, "Could not get Account Sync Role Information", null, null);
                        }*/
                        //bool flag = text.Equals("reseller", StringComparison.OrdinalIgnoreCase);
                        bool flag2 = false;
                        object obj;
                        BillingInformation billingInformation = base.CallBssBillingAPI<BillingInformation>("/api/accounts/" + service.Account.ID,
                            HttpMethod.Get, BSSBillingVersion.v2_2, base.ActionLogUUID, out flag2, out obj, true);
                        if (flag2 || billingInformation == null)
                        {
                            return base.ErrorResult(base.ActionLogUUID, -1, "Could not get Billing Information", null, null);
                        }
                        string reseller = string.Empty;
                        if (_vendorSystemPartnerId == "" || string.IsNullOrWhiteSpace(_vendorSystemPartnerId))
                        {
                            return base.ErrorResult(base.ActionLogUUID, -1, "You have not entered vendorSystem Partner ID. Please update this field by redirectly to My Account -> vendorSystem Partner ID field. Your order won't be able to process. If you have any question, please contact vendorSystemSales@gmail.com.au.", null, null);
                        } else if (_vendorSystemPartnerId.Length < 6 || vendorSystemOrderMarginResponse.status == 404)
                        {
                            return base.ErrorResult(base.ActionLogUUID, -1, "Your vendorSystem Partner ID is invalid, please check your vendorSystem Partner ID in My Account -> vendorSystem Partner ID field. It must be a string of maximum 6 digits. If you have any question, please contact vendorSystemSales@gmail.com.au.", null, null);
                        }

                        short additionalInsuranceYear = 0;

                        string licenseType = service.AttributeList["Type"].Code.Trim();
                        string edition = service.AttributeList["Edition"].Value.Trim();
                        string addHosting = service.AttributeList["AddHosting"].Value.Trim();
                        string po = DateTime.Now.ToString("ddMMyyyy@hh:mmtt");

                        int billingCycleDuration = int.Parse(service.BillingCycle.Duration); // == 12; 24 or 36
                        string billingCycleDurationType = service.BillingCycle.DurationType; // == "month"

                        switch (billingCycleDuration)
                        {
                            case 12 when billingCycleDurationType == "month":
                                additionalInsuranceYear = 0;
                                break;
                            case 24 when billingCycleDurationType == "month":
                                additionalInsuranceYear = 1;
                                break;
                            case 36 when billingCycleDurationType == "month":
                                additionalInsuranceYear = 2;
                                break;
                        }

                        VendorSystemCreateOrderRequest request = null;
                        switch (licenseType)
                        {
                            case "NewLicense":
                                switch (edition)
                                {
                                    case "Professional":
                                    case "Enterprise":
                                        switch (addHosting)
                                        {
                                            case "true":
                                                request = new VendorSystemCreateOrderRequest
                                                {
                                                    PO = "NewLicense_LC" + po,
                                                    Lines = new List<VendorSystemCreateOrderRequest.Line>
                                                    {
                                                        new VendorSystemCreateOrderRequest.Line
                                                        {
                                                            Type = "NewLicense",
                                                            Edition = edition,
                                                            SimultaneousCalls = short.Parse(service.AttributeList["SimultaneousCalls"].Value.Trim()),
                                                            IsPerpetual = false,
                                                            Quantity = 1,
                                                            AdditionalInsuranceYears = additionalInsuranceYear,
                                                            ResellerId = _vendorSystemPartnerId,
                                                            AddHosting = true
                                                        }
                                                    }
                                                };
                                                break;
                                            case "false":
                                                request = new VendorSystemCreateOrderRequest
                                                {
                                                    PO = "NewLicense_LC" + po,
                                                    Lines = new List<VendorSystemCreateOrderRequest.Line>
                                                    {
                                                        new VendorSystemCreateOrderRequest.Line
                                                        {
                                                            Type = "NewLicense",
                                                            Edition = edition,
                                                            SimultaneousCalls = short.Parse(service.AttributeList["SimultaneousCalls"].Value.Trim()),
                                                            IsPerpetual = false,
                                                            Quantity = 1,
                                                            AdditionalInsuranceYears = additionalInsuranceYear,
                                                            ResellerId = _vendorSystemPartnerId,
                                                            AddHosting = false
                                                        }
                                                    }
                                                };
                                                break;
                                        }
                                        break;
                                    case "Startup":
                                        request = new VendorSystemCreateOrderRequest
                                        {
                                            PO = "NewLicense_LC" + po,
                                            Lines = new List<VendorSystemCreateOrderRequest.Line>
                                            {
                                                  new VendorSystemCreateOrderRequest.Line
                                                  {
                                                      Type = "NewLicense",
                                                      Edition = "Startup",
                                                      IsPerpetual = false,
                                                      Extensions = int.Parse(service.AttributeList["Extension"].Value.Trim()),
                                                      SimultaneousCalls = 4,
                                                      Quantity = 1,
                                                      AdditionalInsuranceYears = additionalInsuranceYear,
                                                      ResellerId = _vendorSystemPartnerId,
                                                      AddHosting = false
                                                  }
                                            }
                                        };
                                        break;
                                }
                                break;
                            case "RenewAnnual":
                                request = new VendorSystemCreateOrderRequest
                                {
                                    PO = "RenewAnnual_LC" + po,
                                    Lines = new List<VendorSystemCreateOrderRequest.Line>
                                            {
                                                  new VendorSystemCreateOrderRequest.Line
                                                  {
                                                      Type = "RenewAnnual",
                                                      Quantity = (byte)(service.Quantity),
                                                      UpgradeKey = service.AttributeList["UpgradeKey"].Value.Trim(),
                                                      ResellerId = _vendorSystemPartnerId,
                                                      AddHosting = true
                                                  }
                                            }
                                };
                                break;
                            case "Upgrade":
                                break;
                        }

                    //vendorSystemPreviewOrderResponse = vendorSystemClient.PreviewOrder(request, base.ActionName, base.ActionLogUUID, base.Logging);
                    /*                    }*/
                    vendorSystemPreviewOrderResponse = vendorSystemClient.ProcessOrder(request, base.ActionName, base.ActionLogUUID, base.Logging);
                    //vendorSystemPreviewOrderResponse = vendorSystemClient.PreviewOrder(request, base.ActionName, base.ActionLogUUID, base.Logging);


					if (vendorSystemPreviewOrderResponse == null)
                    {
                        return base.ErrorResult(base.ActionLogUUID, -5, "Failed to create order", null, null);
                    }
                    if (vendorSystemPreviewOrderResponse.Items[0].ErrorCode != null)
                    {
                        return base.ErrorResult(base.ActionLogUUID, -5, vendorSystemPreviewOrderResponse.Items[0].ErrorCode, null, null);
                    }
                }
                catch (Exception ex)
                {
                    LogsHelper.LogException(ex, base.Logging.Logger, base.ActionLogUUID, base.ActionName, true);
                    return base.ErrorResult(base.ActionLogUUID, -1, ex.Message, null, null);
                }
            }

            ServiceResultDefinition serviceResultDefinition = new ServiceResultDefinition();

            serviceResultDefinition.Result = vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any() ?
                                                vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].LicenseKey.ToString()
                                                : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                                                ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].LicenseKey.ToString()
                                                : "None";


            serviceResultDefinition.CustomFieldValues = new Dictionary<string, string>
            {
                // ----- Order General Information ----- //
                { "UniqueId", 
                    vendorSystemPreviewOrderResponse.UniqueId != null 
                    ? vendorSystemPreviewOrderResponse.UniqueId.ToString() 
                    : vendorSystemPreviewOrderResponse.UniqueId == null
                    ? "None"
                    : "None" 
                },
                { "TrackingCode", 
                    vendorSystemPreviewOrderResponse.TrackingCode != null 
                    ? vendorSystemPreviewOrderResponse.TrackingCode.ToString()
                    : vendorSystemPreviewOrderResponse.TrackingCode == null
                    ? "None"
                    : "None" 
                },
                { "Currency", "AUD" },
                { "AdditionalDiscountPerc", vendorSystemPreviewOrderResponse.AdditionalDiscountPerc.ToString() },
                { "AdditionalDiscount", vendorSystemPreviewOrderResponse.AdditionalDiscount.ToString() },
                { "SubTotal", vendorSystemPreviewOrderResponse.SubTotal.ToString() },
                { "TaxPerc", vendorSystemPreviewOrderResponse.TaxPerc.ToString() },
                { "Tax", vendorSystemPreviewOrderResponse.Tax.ToString() },
                { "GrandTotal", vendorSystemPreviewOrderResponse.GrandTotal.ToString() },

                { "LicenseType",
                    vendorSystemPreviewOrderResponse.Items != null ?
                    vendorSystemPreviewOrderResponse.Items.Count == 2 ?
                    vendorSystemPreviewOrderResponse.Items[1].Type.ToString() :
                    vendorSystemPreviewOrderResponse.Items.Count == 1 ? vendorSystemPreviewOrderResponse.Items[0].Type.ToString()
                    : "None"
                    : "None" },
                { "LicenseProductCode",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ProductCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductCode.ToString()
                    : "None" },
                { "LicenseSKU",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].SKU.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].SKU.ToString()
                    : "None" },
                { "LicenseProductName",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ProductName.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductName.ToString()
                    : "None" },
                { "LicenseProductDescription",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ProductDescription.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductDescription.ToString()
                    : "None" },
                { "LicenseUnitPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].UnitPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].UnitPrice.ToString()
                    : "0" },
                { "LicenseDistiDiscount",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Discount.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Discount.ToString()
                    : "0" },
                { "LicenseQuantity",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Quantity.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Quantity.ToString()
                    : "0" },
                { "LicenseDistiNet", 
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Net.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Net.ToString()
                    : "0"
                },
                { "LicenseTax",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Tax.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Tax.ToString()
                    : "0" },
                { "ResellerId",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ResellerId.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ResellerId.ToString()
                    : "None" },
                { "LicenseResellerPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ResellerPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ResellerPrice.ToString()
                    : "0" },
                { "PrivateKeyPassword",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[1].PrivateKeyPassword.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[0].PrivateKeyPassword.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode == null
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].ErrorCode == null
                    ? "None"
                    : "None" 
                },
                { "LicenseKey",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].LicenseKey.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].LicenseKey.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None" 
                },
                { "SimultaneousCalls",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].SimultaneousCalls.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].SimultaneousCalls.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0" },
                { "IsPerpetual",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].IsPerpetual.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].IsPerpetual.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "false" },
                { "Edition",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].Edition.ToString() 
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].Edition.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None" },
                { "ExpiryIncludedMonths",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].ExpiryIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].ExpiryIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0" 
                },
                { "ExpiryDate",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].ExpiryDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].ExpiryDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0"
                },
                { "MaintenanceIncludedMonths",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].MaintenanceIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].MaintenanceIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0" },
                { "MaintenanceDate",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].MaintenanceDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].MaintenanceDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None" },
                { "HostingIncludedMonths",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].HostingIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].HostingIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0" },
                { "HostingExpiry",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].HostingExpiry.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].HostingExpiry.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None" },
                { "LicenseErrorCode",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[1].ErrorCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[0].ErrorCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1] == null
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0] == null
                    ? "None"
                    : "None"
                },
                { "HostingType",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Type.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingTax", 
                    vendorSystemPreviewOrderResponse.Items.Count == 2 
                    ? vendorSystemPreviewOrderResponse.Items[0].Tax.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingQuantity",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Quantity.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingSKU",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].SKU.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingProductDescription",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductDescription.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingProductName",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductDescription.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingUnitPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].UnitPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingDistiDiscount",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Discount.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingDistiNet",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Net.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingResellerPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].ResellerPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingErrorCode",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[1].ErrorCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode == null
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
            };

            serviceResultDefinition.Message = "[New License] vendorSystem_SUCCESS License Key Created: " +
                                                      (vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                                                       ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].LicenseKey.ToString()
                                                       : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                                                       ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].LicenseKey.ToString()
                                                       : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                                                       ? "None"
                                                       : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                                                       ? "None"
                                                       : "None");

            serviceResultDefinition.SendNotification = true;

            string subscriptionId = service.SubscriptionID;

            string endUserName = service.Account.Name;

            SubscriptionInfoUpdateRequest updateRequestToTurnOffAutoRenew;
            updateRequestToTurnOffAutoRenew = new SubscriptionInfoUpdateRequest
            {
                autoRenewal = false,
            };

            SubscriptionInfoUpdateResponse updateSubscriptionAutoRenewResponse = crmPlatformClient.UpdateSubscriptionInfo(updateRequestToTurnOffAutoRenew, subscriptionId, accessToken, "2.2", base.ActionName, base.ActionLogUUID, base.Logging);

            String[] skuDescriptionParts = null; // first part of items response if the hosted is false, if it is true, this will be second part
            String[] hostingDescriptionParts = null;

            String skuPart = null;
            String hostingPart = null;

            if (vendorSystemPreviewOrderResponse.Items.Count > 1)
            {
                hostingDescriptionParts = vendorSystemPreviewOrderResponse.Items[0].ProductDescription.Split('\n');
                skuDescriptionParts = vendorSystemPreviewOrderResponse.Items[1].ProductDescription.Split('\n');

                skuPart = skuDescriptionParts[0];
                hostingPart = hostingDescriptionParts[0];

            }
            else
            {
                skuDescriptionParts = vendorSystemPreviewOrderResponse.Items[0].ProductDescription.Split('\n');
                skuPart = skuDescriptionParts[0];
            }


            // ----- Approach: need to separate between request to change the subscriptions' name and turn off auto-renew ----- //
            // ----- Turn-off auto-renew ----- //

            // ----- Change Subscription Name ----- //
            SubscriptionInfoUpdateRequest updateRequestToChangeName;

            if (hostingPart != null)
            {
                updateRequestToChangeName = new SubscriptionInfoUpdateRequest()
                {
                    name = endUserName + " - " + skuPart + " AND " + hostingPart,
                };
            }
            else
            {
                updateRequestToChangeName = new SubscriptionInfoUpdateRequest()
                {
                    name = endUserName + " - " + skuPart,
                };
            }
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("updateRequestToChangeName: " + JsonConvert.SerializeObject(updateRequestToChangeName));
            }

            SubscriptionInfoUpdateResponse updateSubscriptionNameResponse = crmPlatformClient.UpdateSubscriptionInfo(updateRequestToChangeName, subscriptionId, accessToken, "2.2", base.ActionName, base.ActionLogUUID, base.Logging);
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("updateSubscriptionNameResponse: " + JsonConvert.SerializeObject(updateSubscriptionNameResponse));
            }

            return base.SuccessResult<ServiceResultDefinition>(base.ActionLogUUID, serviceResultDefinition);
        }

        [Route("Subscriptions/UpgradeDowngrade")]
        public override IHttpActionResult SubscriptionsUpgradeDowngrade(ServiceDefinition service)
        {


            }*/
            VendorSystemPreviewOrderResponse vendorSystemPreviewOrderResponse = new VendorSystemPreviewOrderResponse();
            string accessToken = Request.Headers.Contains("X-CloudPlatform-Token") ? Request.Headers.GetValues("X-CloudPlatform-Token").FirstOrDefault() : string.Empty;
			bssApiAccessToken = crmPlatformClient.GetAccessToken();

			// ----- Get Bill To Account ID - //
			AccountInfoResponse accountInfoResponse = crmPlatformClient.getAccountInfo(int.Parse(service.Account.ID), bssApiAccessToken, "3", base.ActionName, base.ActionLogUUID, base.Logging);
			int billToAccountId = accountInfoResponse.billToAccount.id;

			// ----- Get vendorSystem Partner ID from Account Custom Fields ----- //
			string _vendorSystemPartnerId = null;
			List<AccountCustomFields> accountCustomFields
				= crmPlatformClient.accountCustomFieldsResponse(billToAccountId, bssApiAccessToken, "3", base.ActionName, base.ActionLogUUID, base.Logging);

			if (accountCustomFields != null)
			{
				foreach (var accountCustomField in accountCustomFields)
				{
					foreach (var accountCustomFieldGroupFields in accountCustomField.groupFields)
					{
						if (accountCustomFieldGroupFields.name == "vendorSystem Partner ID")
						{
							_vendorSystemPartnerId = accountCustomFieldGroupFields.values[0].value;
							break;
						}
					}
				}
			}
			using (new LogTracer(base.LogActionInput, base.Logging, base.ActionName, base.ActionLogUUID, new List<object> { service }, null, null, null))
            {
                try
                {
                    if (service.IsTest)
                    {
                        ResultDefinition resultDefinition = new ResultDefinition();
                        Random random = new Random();
                        resultDefinition.Code = 0;
                        resultDefinition.Result = random.Next().ToString();
                        return base.SuccessResult<ResultDefinition>(base.ActionLogUUID, resultDefinition);
                    }
                    else
                    {
/*                        string text = service.Account.SyncOptions["role"];
                        if (string.IsNullOrEmpty(text) || text.Equals("User"))
                        {
                            return base.ErrorResult(base.ActionLogUUID, -1, "Could not get Account Sync Role Information", null, null);
                        }*/
                        //bool flag = text.Equals("reseller", StringComparison.OrdinalIgnoreCase);
                        bool flag2 = false;
                        object obj;
                        BillingInformation billingInformation = base.CallBssBillingAPI<BillingInformation>("/api/accounts/" + service.Account.ID,
                            HttpMethod.Get, BSSBillingVersion.v2_2, base.ActionLogUUID, out flag2, out obj, true);
                        if (flag2 || billingInformation == null)
                        {
                            return base.ErrorResult(base.ActionLogUUID, -1, "Could not get Billing Information", null, null);
                        }
                        string reseller = string.Empty;
                        /*if (flag)
                        {
                            reseller = _vendorSystemPartnerId;
                        }
                        else if (!string.IsNullOrEmpty(service.Account.ResellerID) && !string.IsNullOrEmpty(service.Account.ResellerExternalID))
                        {
                            reseller = _vendorSystemPartnerId;
                        }*/

                        // ------ Get Original Subscription Info ----- //
                        string originalSubscriptionId = service.OriginalSubscriptionID;
                        SubscriptionCustomField subscriptionCustomField = base.CallBssBillingAPI<SubscriptionCustomField>("/api/Subscriptions/" + originalSubscriptionId + "/customfields?page=1&size=100",
                            HttpMethod.Get, BSSBillingVersion.v2_2, base.ActionLogUUID, out flag2, out obj, true);
                        if (flag2 || subscriptionCustomField == null)
                        {
                            return base.ErrorResult(base.ActionLogUUID, -1, "Could not find original subscription information", null, null);
                        }

                        string licenseKey = null;

                        foreach (var item in subscriptionCustomField.datum)
                        {
                            if (item.name == "License Key")
                            {
                                licenseKey = item.values.value;
                                break;
                            }
                        }
                        //string json = JsonConvert.SerializeObject(subscriptionCustomField.datum, Formatting.Indented);

                        

                        // ------ Get Product and Order Characteristics ----- //

                        string licenseType = service.AttributeList["Type"].Code.Trim();
                        string edition = service.AttributeList["Edition"].Value.Trim();
                        string addHosting = service.AttributeList["AddHosting"].Value.Trim();

                        

                        // ----- Get the users action ------ //
                        String actionType = service.ActionType.ToString(); // == "Upgrade" or "Downgrade"
                        string po = "Upgrade_LC" + DateTime.Now.ToString("ddMMyyyy@hh:mmtt");

                        using (StreamWriter writer = new StreamWriter(path, true))
                        {
                            writer.WriteLine("-------- Orginal Subscription Info --------");
                            writer.WriteLine("original subscription id: " + originalSubscriptionId);
                            writer.WriteLine("license key: " + licenseKey);
                            writer.WriteLine("---------------------");
                        }

                        VendorSystemCreateOrderRequest request = null;

                        switch (actionType)
                        {
                            case "Upgrade":
                                switch (edition)
                                {
                                    case "Professional":
                                    case "Enterprise":
                                        switch (addHosting)
                                        {
                                            case "true":

                                                request = new VendorSystemCreateOrderRequest
                                                {
                                                    PO = po,
                                                    Lines = new List<VendorSystemCreateOrderRequest.Line>()
                                                    {
                                                        new VendorSystemCreateOrderRequest.Line
                                                        {
                                                            Type = "Upgrade",
                                                            Edition = edition,
                                                            SimultaneousCalls = short.Parse(service.AttributeList["SimultaneousCalls"].Value.Trim()),
                                                            UpgradeKey = licenseKey,
                                                            Quantity = 1,
                                                            ResellerId = _vendorSystemPartnerId,
                                                            AddHosting = true,
                                                        }
                                                    }
                                                };
                                                break;
                                            case "false":
                                                request = new VendorSystemCreateOrderRequest
                                                {
                                                    PO = po,
                                                    Lines = new List<VendorSystemCreateOrderRequest.Line>()
                                                    {
                                                        new VendorSystemCreateOrderRequest.Line
                                                        {
                                                            Type = "Upgrade",
                                                            Edition = edition,
                                                            SimultaneousCalls = short.Parse(service.AttributeList["SimultaneousCalls"].Value.Trim()),
                                                            UpgradeKey = licenseKey,
                                                            Quantity = 1,
                                                            ResellerId = _vendorSystemPartnerId,
                                                            AddHosting = false,
                                                        }
                                                    }
                                                };
                                                break;
                                        }
                                        break;
                                    case "Startup":
                                        request = new VendorSystemCreateOrderRequest
                                        {
                                            PO = po,
                                            Lines = new List<VendorSystemCreateOrderRequest.Line>()
                                            {
                                                new VendorSystemCreateOrderRequest.Line
                                                {
                                                    Type = "Upgrade",
                                                    Edition = "Startup",
                                                    SimultaneousCalls = 4,
                                                    Extensions = int.Parse(service.AttributeList["Extension"].Value.Trim()),
                                                    UpgradeKey = licenseKey,
                                                    Quantity = 1,
                                                    ResellerId = _vendorSystemPartnerId,
                                                    AddHosting = false
                                                }
                                            }
                                        };
                                        break;
                                }
                                break;
                            case "Downgrade":
                                break;
                        }
                        vendorSystemPreviewOrderResponse = vendorSystemClient.PreviewOrder(request, base.ActionName, base.ActionLogUUID, base.Logging);

                        //vendorSystemPreviewOrderResponse = vendorSystemClient.ProcessOrder(request, base.ActionName, base.ActionLogUUID, base.Logging);

                        if (vendorSystemPreviewOrderResponse == null)
                        {
                            using (StreamWriter writer = new StreamWriter(path, true))
                            {
                                writer.WriteLine("-------- Upgrade Request Info --------");
                                writer.WriteLine(JsonConvert.SerializeObject(request, Formatting.Indented));
                                writer.WriteLine("---------------------");
                            }
                            return base.ErrorResult(base.ActionLogUUID, -5, "Failed to create order", null, null);
                        }
                        if (vendorSystemPreviewOrderResponse.Items[0].ErrorCode != null)
                        {
                            return base.ErrorResult(base.ActionLogUUID, -5, vendorSystemPreviewOrderResponse.Items[0].ErrorCode, null, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogsHelper.LogException(ex, base.Logging.Logger, base.ActionLogUUID, base.ActionName, true);
                    return base.ErrorResult(base.ActionLogUUID, -1, ex.Message, null, null);
                }
            }

            ServiceResultDefinition serviceResultDefinition = new ServiceResultDefinition();

            serviceResultDefinition.CustomFieldValues = new Dictionary<string, string>
            {
                // ----- Order General Information ----- //
                { "UniqueId",
                    vendorSystemPreviewOrderResponse.UniqueId != null
                    ? vendorSystemPreviewOrderResponse.UniqueId.ToString()
                    : vendorSystemPreviewOrderResponse.UniqueId == null
                    ? "None"
                    : "None"
                },
                { "TrackingCode",
                    vendorSystemPreviewOrderResponse.TrackingCode != null
                    ? vendorSystemPreviewOrderResponse.TrackingCode.ToString()
                    : vendorSystemPreviewOrderResponse.TrackingCode == null
                    ? "None"
                    : "None"
                },
                { "Currency", "AUD" },
                { "AdditionalDiscountPerc", vendorSystemPreviewOrderResponse.AdditionalDiscountPerc.ToString() },
                { "AdditionalDiscount", vendorSystemPreviewOrderResponse.AdditionalDiscount.ToString() },
                { "SubTotal", vendorSystemPreviewOrderResponse.SubTotal.ToString() },
                { "TaxPerc", vendorSystemPreviewOrderResponse.TaxPerc.ToString() },
                { "Tax", vendorSystemPreviewOrderResponse.Tax.ToString() },
                { "GrandTotal", vendorSystemPreviewOrderResponse.GrandTotal.ToString() },

                { "LicenseType",
                    vendorSystemPreviewOrderResponse.Items != null ?
                    vendorSystemPreviewOrderResponse.Items.Count == 2 ?
                    vendorSystemPreviewOrderResponse.Items[1].Type.ToString() :
                    vendorSystemPreviewOrderResponse.Items.Count == 1 ? vendorSystemPreviewOrderResponse.Items[0].Type.ToString()
                    : "None"
                    : "None" },
                { "LicenseProductCode",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ProductCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductCode.ToString()
                    : "None" },
                { "LicenseSKU",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].SKU.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].SKU.ToString()
                    : "None" },
                { "LicenseProductName",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ProductName.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductName.ToString()
                    : "None" },
                { "LicenseProductDescription",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ProductDescription.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductDescription.ToString()
                    : "None" },
                { "LicenseUnitPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].UnitPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].UnitPrice.ToString()
                    : "0" },
                { "LicenseDistiDiscount",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Discount.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Discount.ToString()
                    : "0" },
                { "LicenseQuantity",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Quantity.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Quantity.ToString()
                    : "0" },
                { "LicenseDistiNet",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Net.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Net.ToString()
                    : "0"
                },
                { "LicenseTax",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].Tax.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].Tax.ToString()
                    : "0" },
                { "ResellerId",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ResellerId.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ResellerId.ToString()
                    : "None" },
                { "LicenseResellerPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[1].ResellerPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? vendorSystemPreviewOrderResponse.Items[0].ResellerPrice.ToString()
                    : "0" },
                { "PrivateKeyPassword",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[1].PrivateKeyPassword.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[0].PrivateKeyPassword.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode == null
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].ErrorCode == null
                    ? "None"
                    : "None"
                },
                { "LicenseKey",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].LicenseKey.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].LicenseKey.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None"
                },
                { "SimultaneousCalls",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].SimultaneousCalls.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].SimultaneousCalls.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0" },
                { "IsPerpetual",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].IsPerpetual.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].IsPerpetual.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "false" },
                { "Edition",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].Edition.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].Edition.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None" },
                { "ExpiryIncludedMonths",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].ExpiryIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].ExpiryIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0"
                },
                { "ExpiryDate",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].ExpiryDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].ExpiryDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0"
                },
                { "MaintenanceIncludedMonths",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].MaintenanceIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].MaintenanceIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0" },
                { "MaintenanceDate",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].MaintenanceDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].MaintenanceDate.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None" },
                { "HostingIncludedMonths",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].HostingIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].HostingIncludedMonths.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "0" },
                { "HostingExpiry",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].HostingExpiry.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].HostingExpiry.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                    ? "None"
                    : "None" },
                { "LicenseErrorCode",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[1].ErrorCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[0].ErrorCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1] == null
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0] == null
                    ? "None"
                    : "None"
                },
                { "HostingType",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Type.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingTax",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Tax.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingQuantity",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Quantity.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingSKU",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].SKU.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingProductDescription",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductDescription.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingProductName",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].ProductDescription.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingUnitPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].UnitPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingDistiDiscount",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Discount.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingDistiNet",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].Net.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingResellerPrice",
                    vendorSystemPreviewOrderResponse.Items.Count == 2
                    ? vendorSystemPreviewOrderResponse.Items[0].ResellerPrice.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
                { "HostingErrorCode",
                    vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode != null
                    ? vendorSystemPreviewOrderResponse.Items[1].ErrorCode.ToString()
                    : vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].ErrorCode == null
                    ? "None"
                    : vendorSystemPreviewOrderResponse.Items.Count == 1
                    ? "None"
                    : "None"
                },
            };

            serviceResultDefinition.Message = "[Upgrade] vendorSystem_SUCCESS License Key: " +
                                                      (vendorSystemPreviewOrderResponse.Items.Count == 2 && vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                                                       ? vendorSystemPreviewOrderResponse.Items[1].LicenseKeys[0].LicenseKey.ToString()
                                                       : vendorSystemPreviewOrderResponse.Items.Count == 1 && vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                                                       ? vendorSystemPreviewOrderResponse.Items[0].LicenseKeys[0].LicenseKey.ToString()
                                                       : vendorSystemPreviewOrderResponse.Items.Count == 2 && !vendorSystemPreviewOrderResponse.Items[1].LicenseKeys.Any()
                                                       ? "None"
                                                       : vendorSystemPreviewOrderResponse.Items.Count == 1 && !vendorSystemPreviewOrderResponse.Items[0].LicenseKeys.Any()
                                                       ? "None"
                                                       : "None");

            serviceResultDefinition.SendNotification = true;

            string subscriptionId = service.SubscriptionID;

			string endUserName = service.Account.Name;

			String[] skuDescriptionParts = null; // first part of items response if the hosted is false, if it is true, this will be second part
			String[] hostingDescriptionParts = null;

			String skuPart = null;
			String hostingPart = null;

			if (vendorSystemPreviewOrderResponse.Items.Count > 1)
			{
				hostingDescriptionParts = vendorSystemPreviewOrderResponse.Items[0].ProductDescription.Split('\n');
				skuDescriptionParts = vendorSystemPreviewOrderResponse.Items[1].ProductDescription.Split('\n');

				skuPart = skuDescriptionParts[0];
				hostingPart = hostingDescriptionParts[0];

			}
			else
			{
				skuDescriptionParts = vendorSystemPreviewOrderResponse.Items[0].ProductDescription.Split('\n');
				skuPart = skuDescriptionParts[0];
			}


			// ----- Approach: need to separate between request to change the subscriptions' name and turn off auto-renew ----- //
			// ----- Change Subscription Name ----- //
			SubscriptionInfoUpdateRequest updateRequestToChangeName;

			if (hostingPart != null)
			{
				updateRequestToChangeName = new SubscriptionInfoUpdateRequest()
				{
					name = endUserName + " - " + skuPart + " AND " + hostingPart,
				};
			}
			else
			{
				updateRequestToChangeName = new SubscriptionInfoUpdateRequest()
				{
					name = endUserName + " - " + skuPart,
				};
			}

			SubscriptionInfoUpdateResponse updateSubscriptionNameResponse =  crmPlatformClient.UpdateSubscriptionInfo(updateRequestToChangeName, subscriptionId, accessToken, "2.2", base.ActionName, base.ActionLogUUID, base.Logging);

			// ----- Turn-off auto-renew ----- //
			SubscriptionInfoUpdateRequest updateRequestToTurnOffAutoRenew;
			updateRequestToTurnOffAutoRenew = new SubscriptionInfoUpdateRequest
			{
				autoRenewal = false,
			};

			SubscriptionInfoUpdateResponse updateSubscriptionAutoRenewResponse =  crmPlatformClient.UpdateSubscriptionInfo(updateRequestToTurnOffAutoRenew, subscriptionId, accessToken, "2.2", base.ActionName, base.ActionLogUUID, base.Logging);
			return base.SuccessResult<ServiceResultDefinition>(base.ActionLogUUID, serviceResultDefinition);
        }

        [Route("Subscriptions/Suspend")]
        public override IHttpActionResult SubscriptionsSuspend(ServiceDefinition service)
        {
            
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { service }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();

                string externalSubscriptionID = service.ID;

                ///...do something on the other side to suspend

                // The external subscription ID
                result.Result = externalSubscriptionID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        [Route("Subscriptions/Update")]
        public override IHttpActionResult SubscriptionsUpdate(ServiceDefinition definition)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { definition }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();

              
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine("definition: " + JsonConvert.SerializeObject(definition));
                    writer.WriteLine("result: " + result);
                }
                string externalSubscriptionID = definition.ID;

                // -> if /api/sbscriptions/{subId}/set is called, will it call this api?

                ///...do something on the other side to update
                /// If the question is NO
                /// Auto renew is off at default
                /// Turn on auto renew -> futurerequest/create -> how to get futureRequestID
                /// Turn off auto renew -> futurerequest/cancel -> how to get futureRequestID

                // The external subscription ID
                result.Result = externalSubscriptionID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        [Route("Subscriptions/UpgradeToPaid")]
        public override IHttpActionResult SubscriptionsUpgradeToPaid(ServiceDefinition definition)
        {
            using (var tracer = new LogTracer(LogActionInput, Logging, ActionName, ActionLogUUID, new List<object>() { definition }))
            {
                ServiceResultDefinition result = new ServiceResultDefinition();

                string externalSubscriptionID = definition.ID;

                ///...do something on the other side to update

                // The external subscription ID
                result.Result = externalSubscriptionID;

                return SuccessResult(ActionLogUUID, result);
            }
        }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- ----- //
    }

}

