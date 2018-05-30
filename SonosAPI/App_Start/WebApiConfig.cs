using System;
using System.Web;
using System.Web.Http;
using SonosAPI.Classes;

namespace SonosAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "sonos/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "ActionApi",
                routeTemplate: "sonos/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
            name: "ActionApiValues",
            routeTemplate: "sonos/{controller}/{action}/{id}/{v}",
            defaults: new { v = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
            name: "ActionApi2Values",
            routeTemplate: "sonos/{controller}/{action}/{id}/{v}/{v2}",
            defaults: new { v = RouteParameter.Optional }
            );

            try
            {

                SonosHelper.LoggingPfad = HttpRuntime.AppDomainAppPath;
            }
            catch(Exception ex)
            {
                //ignore
            }
        }
    }
}
