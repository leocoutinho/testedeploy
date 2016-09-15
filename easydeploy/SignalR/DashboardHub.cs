using easydeploy.Models;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace easydeploy.SignalR
{
    public class DashboardHub : Hub
    {

        public DashboardHub()
        {
        }

        public List<FeatureInfo> GetFeatureInfo()
        {
            return MvcApplication.dataManagerInstance.AppInfo().GetFeatureInfo();
        }

    }
}