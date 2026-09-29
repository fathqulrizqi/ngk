using System.Web.Mvc;

namespace NGKBusi.Areas.IC
{
    public class ICAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "IC";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "IC_default",
                "IC/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}