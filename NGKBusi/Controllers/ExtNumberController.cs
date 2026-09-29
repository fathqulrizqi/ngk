using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace NGKBusi.Controllers
{
    public class ExtNumberController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: ExtNumber
        public ActionResult Index()
        {
            var currUserId = User.Identity.GetUserId();
            ViewBag.pageContent = db.Pages.Where(z => z.id == 1).ToList();
            ViewBag.allowedUpdate = db.Users_Menus_Roles.Where(x => x.userNIK == currUserId && x.menuID == 6).Select(x => x.allowUpdate).SingleOrDefault();
            return View();
        }

        public ActionResult Edit()
        {
            ViewBag.pageContent = db.Pages.Where(z => z.id == 1).ToList();
            return View();
        }

        [ValidateInput(false)]
        public ActionResult Save()
        {
            var currPage = db.Pages.Where(z => z.id == 1).FirstOrDefault();
            currPage.pageContent = Request["iContent"];
            currPage.updated_by = User.Identity.GetUserId();
            db.SaveChanges();

            return RedirectToAction("Index", "ExtNumber");
        }
        
        public ActionResult Email()
        {
            MailMessage mail = new MailMessage();
            mail.To.Add(new MailAddress("simpleminimalis@gmail.com"));
            mail.From = new MailAddress("admin@ngkbusi.com");
            mail.Subject = "Email Test";
            string Body = "Email Test";
            mail.Body = Body;
            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "px6.cloudgate.jp";
            smtp.Port = 23006;
            smtp.Credentials = new System.Net.NetworkCredential
            ("admin@ngkbusi.com", "101%NGKbusi!");
            smtp.EnableSsl = false;
            smtp.Send(mail);
            return RedirectToAction("Index", "ExtNumber");

        }
    }
}