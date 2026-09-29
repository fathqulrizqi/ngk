using System.Web.Mvc;

namespace NGKBusi.Areas.PE
{
    public class PEAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "PE";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "PE_default",
                "PE/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}