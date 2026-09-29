using System.Web.Mvc;

namespace NGKBusi.Areas.CI
{
    public class CIAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "CI";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "CI_default",
                "CI/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}