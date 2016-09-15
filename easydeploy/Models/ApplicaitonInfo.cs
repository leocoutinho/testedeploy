using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace easydeploy.Models
{
    public enum KPIName
    {
        ApplicationDeployment = 0,
        OperationMode = 2,
        VtexOmsFeed = 3,
        AzureEventHub = 4,
        AzureApplicationInsights = 5,
        ApplicationPoolingTime = 6,
        DataLastFeed = 7,
        DataLastOrder = 8,

        //// Application Settings
        ApplicationSettingsGenerateRandomValues = 9,
        ApplicationSettingsServicebusConnectionString = 10,
        ApplicationSettingsEventhubName = 11,
        ApplicationSettingsVtexAccountName = 12,
        ApplicationSettingsVtexAppkey = 13,
        ApplicationSettingsVtexappToken = 14,
        ApplicationSettingsInstrumentationkey = 15


    }

    public class ApplicationInfo
    {
        private Dictionary<String, FeatureInfo> Infos { get; set; }

        public bool CriticalError { get; set; }

        public ApplicationInfo()
        {
            CriticalError = false;

            Infos = new Dictionary<string, FeatureInfo>();
            Infos.Add(KPIName.ApplicationDeployment.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationDeployment.ToString(), Name = "Application Deployment", Description = "Application was deployed on Azure { Error: Error have occurred during the deployment; Unavailable: The status was not able to be determined; Success: Application was successfuly deployed }", Status = FeatureStatus.Success });
            Infos.Add(KPIName.OperationMode.ToString(), new FeatureInfo() { KpiName = KPIName.OperationMode.ToString(), Name = "Operation Mode", Description = "Indicates what type off operation the application is running Demostration or Vtex { Consuming Vtex OMS Feed : Accessing Vtex Services; Demostration Mode - Generating Values Randomically }", Details = "", Status = FeatureStatus.Success, Value = "" });
            Infos.Add(KPIName.VtexOmsFeed.ToString(), new FeatureInfo() { KpiName = KPIName.VtexOmsFeed.ToString(), Name = "Vtex Oms Feed", Description = "Vtex Oms Feed endpoint { Error: Some error has happen check the access vtextoken, vtexaccount; Unavailable: We were not able to determine the service status, probably the application is running in Demostration Mode; Success: The service is available and the application was able to access }", Status = FeatureStatus.Unavailable });
            Infos.Add(KPIName.AzureEventHub.ToString(), new FeatureInfo() { KpiName = KPIName.AzureEventHub.ToString(), Name = "Azure Event Hub", Description = "Azure Event Hub{ Error: Not able to connect, this is a critical error and it is possible that you need to recreate the enviroment; Unavailable: We were not able to determine the service status; Success: The service is running and the application was able to use it  }", Status = FeatureStatus.Unavailable });
            Infos.Add(KPIName.AzureApplicationInsights.ToString(), new FeatureInfo() { KpiName = KPIName.AzureApplicationInsights.ToString(), Name = "Azure Application Insights", Description = "Azure Application Insights { Error: Not able to write event, this not a critical error in terms of functionality ; Unavailable: We were not able to determine the service status; Success: The service is running and the application was able to use it }", Status = FeatureStatus.Unavailable });
            Infos.Add(KPIName.ApplicationPoolingTime.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationPoolingTime.ToString(), Name = "Application Pooling Time", Description = "Application Pooling Time {Success: can't assume other status}", Status = FeatureStatus.Success, Value = "0" });

            Infos.Add(KPIName.DataLastFeed.ToString(), new FeatureInfo() { KpiName = KPIName.DataLastFeed.ToString(), Name = "Last Feed Date", Description = "The last host timestamp that the application tried to read the feed {Unavailable: can't assume other status; Success:Has value from the last feed}", Status = FeatureStatus.Unavailable, Value = "" });
            Infos.Add(KPIName.DataLastOrder.ToString(), new FeatureInfo() { KpiName = KPIName.DataLastOrder.ToString(), Name = "Last Order Data", Description = "Basic information from the last order { Error: Not able to read the data feed, this can be a temporary if the Vtex OMS Feed has a Success state  ; Unavailable: We were not able to determine the service status; Success: The service is running and the application was able to use it }", Status = FeatureStatus.Unavailable, Value = "" });

            Infos.Add(KPIName.ApplicationSettingsEventhubName.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationSettingsEventhubName.ToString(), Name = "Application Settings Eventhub Name", Description = "Application Setup information, has to have the name of the Event Hub { Error: the value is null or empty. This is a critical error, better to redeploy the Application  ; Unavailable: The application setting is not available. This is a critical error, better to redeploy the Application ; Success: The value is present }", Status = FeatureStatus.Unavailable, Value = "" });
            Infos.Add(KPIName.ApplicationSettingsServicebusConnectionString.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationSettingsServicebusConnectionString.ToString(), Name = "Application Servicebus Connection String", Description = "Application Servicebus Connection String { Unavailable: The application setting is not available. This is not a critical error the default value wil be used; Success: The value is present }", Status = FeatureStatus.Unavailable, Value = "" });
            Infos.Add(KPIName.ApplicationSettingsGenerateRandomValues.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationSettingsGenerateRandomValues.ToString(), Name = "Application Generate Random Values", Description = "Application Generate Random Values{ Unavailable: The application setting is not available. This is not a critical error the default value wil be used; Success: The value is present }", Status = FeatureStatus.Unavailable, Value = "" });

            Infos.Add(KPIName.ApplicationSettingsVtexAccountName.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationSettingsVtexAccountName.ToString(), Name = "Application Settings Vtex Account Name", Description = "Application Settings Vtex Account Name { Unavailable: The application setting is not available. This is a critical error the default value wil be used; Success: The value is present }", Status = FeatureStatus.Unavailable, Value = "" });
            Infos.Add(KPIName.ApplicationSettingsVtexAppkey.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationSettingsVtexAppkey.ToString(), Name = "Application Settings Vtex App Key", Description = "Application Settings Vtex App Key { Unavailable: The application setting is not available. This is a critical error the default value wil be used; Success: The value is present }", Status = FeatureStatus.Unavailable, Value = "" });
            Infos.Add(KPIName.ApplicationSettingsVtexappToken.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationSettingsVtexappToken.ToString(), Name = "Application Settings Vtex App Token", Description = "Application Settings Vtex App Token { Unavailable: The application setting is not available. This is a critical error the default value wil be used; Success: The value is present }", Status = FeatureStatus.Unavailable, Value = "" });
            Infos.Add(KPIName.ApplicationSettingsInstrumentationkey.ToString(), new FeatureInfo() { KpiName = KPIName.ApplicationSettingsInstrumentationkey.ToString(), Name = "Application Settings Instrumentationkey", Description = "Application Settings Instrumentationkey { Unavailable: The application setting is not available. This is not a critical error the default value wil be used; Success: The value is present }", Status = FeatureStatus.Unavailable, Value = "" });

        }


        public void UpdateFeatureInfo(KPIName kpi, string feature_value, FeatureStatus status, string details)
        {
            FeatureInfo feature_info = this.Infos[kpi.ToString()];
            if (feature_info != null)
            {
                feature_info.Value = feature_value;
                feature_info.Status = status;
                feature_info.Details = details;
            }
        }

        public FeatureInfo GetApplicationInfo(String KPIName)
        {
            return Infos[KPIName];
        }

        public Dictionary<String, FeatureInfo> GetApplicationInfos()
        {
            return Infos;
        }

        public List<FeatureInfo> GetFeatureInfo()
        {
            List<FeatureInfo> lista = new List<FeatureInfo>(Infos.Values);
            return lista;
        }

        #region [   ApplicationInfoCheckMethods   ]
        public void CheckApplicationSettings()
        {

            foreach (string kpiName in Infos.Keys)
            {
                KPIName kpi = (KPIName)Enum.Parse(typeof(KPIName), kpiName);
                String appSettingName = GetApplicationSetting(kpi);
                if (!String.IsNullOrEmpty(appSettingName))
                {
                    try
                    {
                        string value = ConfigurationManager.AppSettings[appSettingName];
                        if (String.IsNullOrEmpty(value))
                            UpdateFeatureInfo(kpi, appSettingName, FeatureStatus.Error, appSettingName + " is null or empty, reinstall the application!");
                        else
                            UpdateFeatureInfo(kpi, value, FeatureStatus.Success, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        UpdateFeatureInfo(kpi, null, FeatureStatus.Error, ex.Message);
                    }
                }
            }

        }
        #endregion

        public string GetApplicationSetting(KPIName name)
        {

            switch (name)
            {
                case KPIName.ApplicationSettingsEventhubName: return "eventhubname";
                case KPIName.ApplicationSettingsGenerateRandomValues: return "GenerateRandomValues";
                case KPIName.ApplicationSettingsInstrumentationkey: return "instrumentationkey";
                case KPIName.ApplicationSettingsServicebusConnectionString: return "servicebusconnectionstring";
                case KPIName.ApplicationSettingsVtexAccountName: return "vtexaccountname";
                case KPIName.ApplicationSettingsVtexAppkey: return "vtexappkey";
                case KPIName.ApplicationSettingsVtexappToken: return "vtexapptoken";
                default: return "";
            }

        }
    }
}