using System.Web.Mvc;

namespace NGKBusi.Areas.HSE
{
    public class HSEAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "HSE";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "HSE_default",
                "HSE/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}