using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using System.Web;
using System.Web.Mvc;
using NGKBusi.Models;

namespace NGKBusi.Areas.HC.Controllers
{
    public class MailNumberingController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        public String toRomawi(string month)
        {
            var romawi = "";
            switch (month)
            {
                case "01":
                    romawi = "I";
                    break;
                case "02":
                    romawi = "II";
                    break;
                case "03":
                    romawi = "III";
                    break;
                case "04":
                    romawi = "IV";
                    break;
                case "05":
                    romawi = "V";
                    break;
                case "06":
                    romawi = "VI";
                    break;
                case "07":
                    romawi = "VII";
                    break;
                case "08":
                    romawi = "VIII";
                    break;
                case "09":
                    romawi = "IX";
                    break;
                case "10":
                    romawi = "X";
                    break;
                case "11":
                    romawi = "XI";
                    break;
                case "12":
                    romawi = "XII";
                    break;
                default:
                    break;

            }
            return romawi;
        }
        // GET: HC/MailNumbering
        public ActionResult Index()
        {
            int period = Request["iHCNMPeriod"] != null ? int.Parse(Request["iHCNMPeriod"]) : DateTime.Now.Year;
            ViewBag.DataList = db.HC_MailNumbering_List.Where(w => w.Date.Year == period).OrderByDescending(o => o.ID).ToList();
            ViewBag.Period = period;
            return View();
        }

        [Authorize]
        public ActionResult MailNumberingAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var purpose = Request["iHCMNPurpose"];
            var subject = Request["iHCMNSubject"];
            var year = DateTime.Now.ToString("yyyy");
            var lastNumber = db.HC_MailNumbering_List.Where(w => w.Number.Substring(w.Number.Length - 4,4) == year && w.Number.Substring(5, 7) == "Niterra").OrderByDescending(o => o.ID).Max(s =>  s.Number.Substring(0,4));
            lastNumber = lastNumber == null ? "0001" : "0000" + (Int32.Parse(lastNumber) + 1).ToString();
            lastNumber = lastNumber.Substring(lastNumber.Length - 4, 4);
            db.HC_MailNumbering_List.Add(new HC_MailNumbering_List
            {
                Number = lastNumber + "/Niterra/" + toRomawi(DateTime.Now.ToString("MM")) + "/" + DateTime.Now.ToString("yyyy"),
                Date = DateTime.Now,
                Purpose = purpose,
                Subject = subject,
                User_By = currUserID
            });
            db.SaveChanges();
            //return Content(lastNumber);
            return RedirectToAction("Index", "MailNumbering", new { area = "HC" });
        }
        [Authorize]
        public ActionResult MailNumberingEdit()
        {
            var currDataID = Int32.Parse(Request["iHCMNDataID"]);
            var purpose = Request["iHCMNPurpose"];
            var subject = Request["iHCMNSubject"];

            var editData = db.HC_MailNumbering_List.Where(w => w.ID == currDataID).FirstOrDefault();
            editData.Purpose = purpose;
            editData.Subject = subject;
            db.SaveChanges();
            return RedirectToAction("Index", "MailNumbering", new { area = "HC" });
        }


        public class LastNumb
        {
            public string lastNumb { get; set; }
        }
    }
}