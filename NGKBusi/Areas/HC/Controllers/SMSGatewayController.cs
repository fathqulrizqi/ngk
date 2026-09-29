using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.HC.Controllers
{
    public class SMSGatewayController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: HC/SMSGateway
        [Authorize]
        public ActionResult Index()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 11
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            //return View();
            return Redirect("http://192.168.1.248:81/user/login?iNIK=999.99.99&iPassword=100%NGKbusi!&iRedirectURL=");
        }

        [Authorize]
        public ActionResult Inbox()
        {
            return View();
        }
        [Authorize]
        public ActionResult Outbox()
        {
            return View();
        }
        [Authorize]
        public ActionResult PhoneBook()
        {
            return View();
        }
        [Authorize]
        public ActionResult PhoneGroup()
        {
            return View();
        }
        [Authorize]
        public ActionResult SMSTemplate()
        {
            return View();
        }
        [Authorize]
        public ActionResult SendSMS()
        {
            return View();
        }

    }
}