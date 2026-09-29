using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using System.Web.Routing;

namespace NGKBusi.Helpers
{
    public class CheckExternalDomainAuth : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!HttpContext.Current.Request.Url.AbsoluteUri.ToLower().Contains("login"))
            {
                if ((!HttpContext.Current.User.Identity.IsAuthenticated))
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "User", action = "Login" }));
                }
            }
            base.OnActionExecuting(filterContext);
        }
    }
}