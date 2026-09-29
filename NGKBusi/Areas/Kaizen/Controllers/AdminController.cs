using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using System.Security.Claims;

namespace NGKBusi.Areas.Kaizen.Controllers
{
    public class AdminController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: Kaizen/Admin
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
            ViewBag.userList = db.Users.Where(x => x.Status != "Not Active" && !x.NIK.Contains("Exp.") && x.NIK != "001.05.87").ToList();
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
        public ActionResult setRewarded(Int32 iID)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var updateReward = db.Kaizen_Data.Where(x => x.ID == iID).FirstOrDefault();
            updateReward.hasRewarded = !updateReward.hasRewarded;
            db.SaveChanges();

            return Json(new { success = true });
        }
    }
}