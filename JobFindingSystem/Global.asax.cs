using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Gnostice.StarDocsSDK;

namespace JobFindingSystem
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static StarDocs starDocs;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //For pdf preview
            starDocs = new StarDocs(
                new ConnectionInfo(new Uri("https://api.gnostice.com/stardocs/v1"),
                "f1110c1cacbf42cba8d655b1720ccb5a", "a90635ab3458442984655a41fa2cdbce"));

            starDocs.Auth.loginApp();
        }
    }
}
