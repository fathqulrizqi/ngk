using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Controllers
{
    public class GlobalController : Controller
    {
        // GET: Global
        public ActionResult Index()
        {
            return View();
        }

        public class Swal
        {
            public string Title { get; set; }
            public string Text { get; set; }
            public string Icon { get; set; }
        }
    }
}