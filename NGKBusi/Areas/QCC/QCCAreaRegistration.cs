using System.Web.Mvc;

namespace NGKBusi.Areas.QCC
{
    public class QCCAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "QCC";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "QCC_default",
                "QCC/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}