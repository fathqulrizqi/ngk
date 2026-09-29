using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using System.Security.Claims;
using System.Data.Entity;

namespace NGKBusi.Areas.Kaizen.Controllers
{
    public class PIController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: Kaizen/PI
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult DataList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Kaizen_Group_User.Where(z => z.userNIK == currUserID && z.groupID == 5).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }

            Int32 period = (Request["iPeriod"] != null ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 4 ? DateTime.Now.Year - 1 : DateTime.Now.Year));
            ViewBag.Period = period;
            var rangeStart = new DateTime(period, 4, 1);
            var rangeEnd = new DateTime(period + 1, 4, 1);
            ViewBag.kaizenList = db.Kaizen_Data.Where(w => w.issuedDate >= rangeStart && w.issuedDate < rangeEnd).OrderByDescending(x => x.ID).ToList();
            ViewBag.userList = db.Users.Where(x => x.Status != "Not Active" && !x.NIK.Contains("Exp.") && x.NIK != "001.05.87").OrderBy(x => x.Name).ToList();
            ViewBag.OCDList = db.Kaizen_Score_Categories.Where(x => x.groupID == 1).ToList();
            ViewBag.KOCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 2).ToList();
            ViewBag.SCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 3).ToList();
            var CPPLatestDate = db.Kaizen_Master_CostBenefit_CPP.Where(x => x.Start_Date <= DateTime.Now).Max(m => m.Start_Date);
            ViewBag.CPPList = db.Kaizen_Master_CostBenefit_CPP.Where(x => x.Start_Date == CPPLatestDate).OrderBy(o => o.Area).ToList();
            var UMP = db.Kaizen_Master_CostBenefit_UMP.Where(x => x.Period == period).FirstOrDefault()?.UMP ?? 0;
            ViewBag.ManMinute = UMP > 0 ? Math.Round((decimal)(UMP / 22 / 8 / 60)) : 0;
            ViewBag.NavHide = true;

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Implement()
        {
            var currURL = Request.UrlReferrer;
            var dataID = Int32.Parse(Request["iPIImplementID"]);
            var impContent = Request["iPIImplement"];
            var impStatus = Int32.Parse(Request["iPIImplemented"]) > 0 ? true : false;
            string[] implementor = Request["iPIImplementor[]"].Split(new[] { "," }, StringSplitOptions.None);
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var updateImplement = db.Kaizen_Data.Where(x => x.ID == dataID).FirstOrDefault();
            updateImplement.implementContent = impContent;
            updateImplement.hasImplement = impStatus;

            var deleteOldImplementor = db.Kaizen_Data_Implementor.Where(x => x.dataID == dataID);
            db.Kaizen_Data_Implementor.RemoveRange(deleteOldImplementor);

            foreach(var imp in implementor)
            {
                db.Kaizen_Data_Implementor.Add(new Kaizen_Data_Implementor
                {
                    dataID = dataID,
                    userNIK = imp
                });
            }
            db.SaveChanges();

            return Redirect(currURL.AbsoluteUri);
        }

        [HttpPost]
        public ActionResult setImplement(Int32 iID)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var updateImplement = db.Kaizen_Data.Where(x => x.ID == iID).FirstOrDefault();
            updateImplement.hasImplement = !updateImplement.hasImplement;
            db.SaveChanges();

            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult getImplement(Int32 iID)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var impData = db.Kaizen_Data.Include(zz =>zz.implementorList).Where(x => x.ID == iID).Select(x => new {
               content= x.implementContent,implementor = x.implementorList.Select(z => new { z.userNIK})
            }).ToList();

            return Json(impData);
        }
    }
}