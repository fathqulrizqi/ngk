using System.Web.Mvc;

namespace NGKBusi.Areas.IReporter
{
    public class IReporterAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "IReporter";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "IReporter_default",
                "IReporter/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}