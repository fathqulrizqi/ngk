using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult index()
        {
            return View();
        }
        public ActionResult _404()
        {
            return View();
        }
    }
}