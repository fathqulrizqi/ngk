using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using NGKBusi.Models;

namespace NGKBusi.Areas.Kaizen.Controllers
{
    public class SCController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: Kaizen/SC
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult DataList_()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Kaizen_Group_User.Where(z => z.userNIK == currUserID && z.groupID == 3).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }
            var impType = "";
            switch (coll.catID)
            {
                case 5:
                    impType = "Perbaikan Produktifitas";
                    break;
                case 6:
                    impType = "Perbaikan Kualitas";
                    break;
                case 7:
                    impType = "Perbaikan K3 & Sumber Daya";
                    break;
                case 8:
                    impType = "Perbaikan 5S & Lingkungan";
                    break;
                default:
                    impType = "%";
                    break;
            }

            Int32 period = (Request["iPeriod"] != null ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 4 ? DateTime.Now.Year - 1 : DateTime.Now.Year));
            ViewBag.Period = period;
            var rangeStart = new DateTime(period, 4, 1);
            var rangeEnd = new DateTime(period + 1, 4, 1);
            ViewBag.OCDList = db.Kaizen_Score_Categories.Where(x => x.groupID == 1).ToList();
            ViewBag.KOCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 2).ToList();
            ViewBag.SCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 3).ToList();
            ViewBag.NavHide = true;
            if (currUserID == "SCReward")
            {
                ViewBag.kaizenList = db.Kaizen_Data.Where(z => z.issuedDate < new DateTime(2017,1,1)).OrderByDescending(x => x.ID).ToList();
            }
            else
            {
                if (impType != "%")
                {
                    ViewBag.kaizenList = db.Kaizen_Data.Where(x => (x.OCDScore + x.KOCScore) >= 40 && x.improveType.Contains(impType) && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
                }
                else
                {
                    ViewBag.kaizenList = db.Kaizen_Data.Where(x => (x.OCDScore + x.KOCScore) >= 40 && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
                }

            }

            return View();
        }
        [Authorize]
        public ActionResult DataList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Kaizen_Group_User.Where(z => z.userNIK == currUserID && z.groupID == 3).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }
            var impType = "";
            switch (coll.catID)
            {
                case 5:
                    impType = "Perbaikan Produktifitas";
                    break;
                case 6:
                    impType = "Perbaikan Kualitas";
                    break;
                case 7:
                    impType = "Perbaikan K3 & Sumber Daya";
                    break;
                case 8:
                    impType = "Perbaikan 5S & Lingkungan";
                    break;
                default:
                    impType = "%";
                    break;
            }

            Int32 period = (Request["iPeriod"] != null ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 4 ? DateTime.Now.Year - 1 : DateTime.Now.Year));
            ViewBag.Period = period;
            var rangeStart = new DateTime(period, 4, 1);
            var rangeEnd = new DateTime(period + 1, 4, 1);
            ViewBag.OCDList = db.Kaizen_Score_Categories.Where(x => x.groupID == 1).ToList();
            ViewBag.KOCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 2).ToList();
            ViewBag.SCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 3).ToList();
            ViewBag.NavHide = true;
            if (currUserID == "SCReward")
            {
                ViewBag.kaizenList = db.Kaizen_Data.Where(z => z.issuedDate < new DateTime(2017, 1, 1)).OrderByDescending(x => x.ID).ToList();
            }
            else
            {
                if (impType != "%")
                {
                    ViewBag.kaizenList = db.Kaizen_Data.Where(x => (x.OCDScore + x.KOCScore) >= 40 && x.improveType.Contains(impType) && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
                }
                else
                {
                    ViewBag.kaizenList = db.Kaizen_Data.Where(x => (x.OCDScore + x.KOCScore) >= 40 && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
                }
            }

            var CPPLatestDate = db.Kaizen_Master_CostBenefit_CPP.Where(x => x.Start_Date <= DateTime.Now).Max(m => m.Start_Date);
            ViewBag.CPPList = db.Kaizen_Master_CostBenefit_CPP.Where(x => x.Start_Date == CPPLatestDate).OrderBy(o => o.Area).ToList();
            var UMP = db.Kaizen_Master_CostBenefit_UMP.Where(x => x.Period == period).FirstOrDefault()?.UMP ?? 0;
            ViewBag.ManMinute = UMP > 0 ? Math.Round((decimal)(UMP / 22 / 8 / 60)) : 0;

            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult Score(HttpPostedFileBase iScan)
        {
            var currURL = Request.UrlReferrer;
            var currDataID = Int32.Parse(Request["iDataID"]);
            var currScore = double.Parse(Request["iScore"]);
            var currNote = Request["iScoreNote"];
            var currReward = double.Parse(Request["iReward"]);
            if (currScore == 0)
            {
                return Redirect(currURL.AbsoluteUri);
            }

            var currUser = (ClaimsIdentity)User.Identity;
            var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
            updateData.SCScore = currScore;
            updateData.Reward = currReward;
            updateData.SCDate = DateTime.Now;
            updateData.SCBy = currUser.GetUserId();
            updateData.SCNote = currNote;
            for (var i = 1; i <= 4; i++)
            {
                if (Request["iSCScore" + i] != null)
                {
                    string[] currKOC = Request["iSCScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                    var updateScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i + 8);

                    if (updateScore.Count() > 0)
                    {
                        var updateDBScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i + 8).FirstOrDefault();
                        updateDBScore.subCatID = Int32.Parse(currKOC[0]);
                        updateDBScore.Score = double.Parse(currKOC[1]);
                    }
                    else
                    {
                        db.Kaizen_Score.Add(new Kaizen_Score
                        {
                            dataID = currDataID,
                            catID = i + 8,
                            subCatID = Int32.Parse(currKOC[0]),
                            Score = double.Parse(currKOC[1])
                        });
                    }
                }
            }
            db.SaveChanges();
            return Redirect(currURL.AbsoluteUri);
        }

        [HttpPost]
        [Authorize]
        public ActionResult Reward()
        {
            var currURL = Request.UrlReferrer;
            var currDataID = Int32.Parse(Request["iDataID"]);
            var currReward = double.Parse(Request["iReward"]);            
            var currUser = (ClaimsIdentity)User.Identity;
            var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
            updateData.Reward = currReward;
            updateData.SCDate = DateTime.Now;
            updateData.SCBy = currUser.GetUserId();
            
            db.SaveChanges();
            return Redirect(currURL.AbsoluteUri);
        }

        [HttpPost]
        public JsonResult getCurrScore(Int32 iID)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var userID = currUser.GetUserId();
            var catIDList = new List<int> { 9, 10, 11, 12 };
            var getScore = db.Kaizen_Score.Where(z => z.dataID == iID && catIDList.Contains(z.catID)).Select(x => new
            {
                ID = x.ID,
                dataID = x.dataID,
                catID = x.catID,
                subCatID = x.subCatID,
                Score = x.Score,
                ScoredDate = x.Kaizen_Data.SCDate,
                ScoredBy = x.Kaizen_Data.SCBy,
                ScoredByName = x.Kaizen_Data.SCUser.Name,
                ScoredNote = x.Kaizen_Data.SCNote,
                currUser = userID,
                CostTotal = x.Kaizen_Data.CostTotal,
                BenefitTotal = x.Kaizen_Data.BenefitTotal,
                CostBenefitTotal = x.Kaizen_Data.CostBenefitTotal
            }).ToList();
            var getData = db.Kaizen_Data.Where(z => z.ID == iID).Select(x => new
            {
                Score = x.SCScore,
                ScoredDate = x.SCDate,
                ScoredBy = x.SCBy,
                ScoredByName = x.SCUser.Name,
                ScoredNote = x.SCNote,
                currUser = userID,
                CostTotal = x.CostTotal,
                BenefitTotal = x.BenefitTotal,
                CostBenefitTotal = x.CostBenefitTotal
            }).ToList();
            if (getScore.Count == 0)
            {
                return Json(getData);
            }
            return Json(getScore);
        }

        [HttpPost]
        public ActionResult Lock()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var currDataID = Int32.Parse(Request["iID"]);
            var isLocked = true;
            var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
            if (updateData.SCBy == null)
            {
                updateData.SCBy = currUserID;
                isLocked = true;
            }
            else
            {
                updateData.SCBy = null;
                isLocked = false;
            }
            db.SaveChanges();

            return Json(new { locked = isLocked });
        }
    }
}