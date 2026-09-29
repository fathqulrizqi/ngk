using System.Web.Mvc;

namespace NGKBusi.Areas.FA
{
    public class FAAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "FA";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "FA_default",
                "FA/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}