using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ServiceManager.VendorX.RecurringService
{
    using CRMPlatform = CRMPlatform.Cloud.ServiceManagersSDK.Libraries.Filters;

    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Filters.Add(new CRMPlatform.AuthorizationAttribute());
            config.Filters.Add(new CRMPlatform.NoAccessActionFilterAttribute());
            config.Filters.Add(new CRMPlatform.LogExceptionFilterAtribute());
            config.Filters.Add(new CRMPlatform.ActionNameActionFilterAttribute());
        }
    }
}
