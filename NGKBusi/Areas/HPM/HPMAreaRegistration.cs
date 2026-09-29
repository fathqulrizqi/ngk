using System.Web.Mvc;

namespace NGKBusi.Areas.HPM
{
    public class HPMAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "HPM";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "HPM_default",
                "HPM/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}