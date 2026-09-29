using System.Web.Mvc;

namespace NGKBusi.Areas.HC
{
    public class HCAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "HC";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "HC_default",
                "HC/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}