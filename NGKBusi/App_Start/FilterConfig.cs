using System.Web;
using System.Web.Mvc;
using NGKBusi.Helpers;

namespace NGKBusi
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new CheckExternalDomainAuth());
        }
    }
}
