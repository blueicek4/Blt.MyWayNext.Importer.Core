using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Blt.MyWayNext.WebHook
{
    public static class WebRoute
    {
        public static void Register(HttpConfiguration config)
        {
            // Servizi e configurazione dell'API Web

            // Route dell'API Web
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "WebhookApi",
                routeTemplate: "api/{guid}",
                defaults: new { controller = "Webhook",  guid = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "DataApi",
                routeTemplate: "api/{controller}",
                defaults: new { controller = "Data", guid = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "MetaApi",
                routeTemplate: "api/{controller}",
                defaults: new { controller = "Meta", guid = RouteParameter.Optional }
            );
        }
    }
}
