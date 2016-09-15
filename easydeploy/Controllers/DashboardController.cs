using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace easydeploy.Controllers
{
    //[Authorize]
    public class DashboardController : Controller
    {

        // GET: Dashboard
        public ActionResult Index()
        {
            var app = HttpContext.ApplicationInstance as MvcApplication;
            ViewBag.AppInfo = MvcApplication.dataManagerInstance.AppInfo();
            return View();
        }

        
    }
}
