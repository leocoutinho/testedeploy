using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace easydeploy.Models
{
    public enum FeatureStatus
    {
        Success = 0,
        Error = 1,
        Unavailable = 2
    }

    public class FeatureInfo
    {
        public String KpiName { get; set; }
        public String Name { get; set; }
        public FeatureStatus Status { get; set; }
        public String Description { get; set; }
        public String Value { get; set; }
        public String Details { get; set; }
    }

}