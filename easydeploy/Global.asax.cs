using easydeploy.Models;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace easydeploy
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static DataManager dataManagerInstance = null;

        protected void Application_Start()
        {
            MvcApplication.dataManagerInstance = new DataManager();
            try
            {
                TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["instrumentationkey"];

            }catch(Exception ex)
            {
                MvcApplication.dataManagerInstance.AppInfo().UpdateFeatureInfo(KPIName.ApplicationDeployment, "Application Start Fail!", FeatureStatus.Error, "Application Settings Error, need to reinstall the application!");
            }

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }


    }
}
