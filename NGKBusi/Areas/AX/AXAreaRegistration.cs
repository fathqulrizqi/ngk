using System.Web.Mvc;

namespace NGKBusi.Areas.AX
{
    public class AXAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "AX";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "AX_default",
                "AX/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}