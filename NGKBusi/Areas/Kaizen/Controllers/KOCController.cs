using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NGKBusi.Models;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using System.Text.RegularExpressions;

namespace NGKBusi.Areas.Kaizen.Controllers
{
    public class KOCController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: Kaizen/KOC
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult DataList_()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Kaizen_Group_User.Where(z => z.userNIK == currUserID && z.groupID == 2).FirstOrDefault();

            var currUserDivName = currUser.FindFirst("divName").Value;

            if (coll == null)
            {
                return View("UnAuthorized");
            }
            //var impType = "";
            //switch (coll.catID)
            //{
            //    case 5:
            //        impType = "Perbaikan Kualitas";
            //        break;
            //    case 6:
            //        impType = "Perbaikan Produktifitas";
            //        break;
            //    case 7:
            //        impType = "Perbaikan K3 & Sumber Daya";
            //        break;
            //    case 8:
            //        impType = "Perbaikan 5S & Lingkungan";
            //        break;
            //    default:
            //        impType = "%";
            //        break;
            //}


            Int32 period = (Request["iPeriod"] != null ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 4 ? DateTime.Now.Year - 1 : DateTime.Now.Year));
            ViewBag.Period = period;
            var rangeStart = new DateTime(period, 4, 1);
            var rangeEnd = new DateTime(period + 1, 4, 1);
            ViewBag.OCDList = db.Kaizen_Score_Categories.Where(x => x.groupID == 1).ToList();
            ViewBag.KOCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 2).ToList();
            ViewBag.SCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 3).ToList();
            ViewBag.NavHide = true;
            //if (impType != "%")
            //{
            //    ViewBag.kaizenList = db.Kaizen_Data.Where(x => x.OCDScore >= 16 && x.improveType.Contains(impType) && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd) && x.Division == currUserDivName).OrderByDescending(x => x.ID).ToList();
            //}
            //else
            //{
            //    ViewBag.kaizenList = db.Kaizen_Data.Where(x => x.OCDScore >= 16 && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
            //}
            ViewBag.kaizenList = db.Kaizen_Data.Where(x => x.OCDScore >= 16 && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();

            return View();
        }
        [Authorize]
        public ActionResult DataList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Kaizen_Group_User.Where(z => z.userNIK == currUserID && z.groupID == 2).FirstOrDefault();

            var currUserDivName = currUser.FindFirst("divName").Value;

            if (coll == null)
            {
                return View("UnAuthorized");
            }
            //var impType = "";
            //switch (coll.catID)
            //{
            //    case 5:
            //        impType = "Perbaikan Kualitas";
            //        break;
            //    case 6:
            //        impType = "Perbaikan Produktifitas";
            //        break;
            //    case 7:
            //        impType = "Perbaikan K3 & Sumber Daya";
            //        break;
            //    case 8:
            //        impType = "Perbaikan 5S & Lingkungan";
            //        break;
            //    default:
            //        impType = "%";
            //        break;
            //}


            Int32 period = (Request["iPeriod"] != null ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 4 ? DateTime.Now.Year - 1 : DateTime.Now.Year));
            ViewBag.Period = period;
            var rangeStart = new DateTime(period, 4, 1);
            var rangeEnd = new DateTime(period + 1, 4, 1);
            ViewBag.OCDList = db.Kaizen_Score_Categories.Where(x => x.groupID == 1).ToList();
            ViewBag.KOCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 2).ToList();
            ViewBag.SCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 3).ToList();
            ViewBag.NavHide = true;
            var CPPLatestDate = db.Kaizen_Master_CostBenefit_CPP.Where(x => x.Start_Date <= DateTime.Now).Max(m => m.Start_Date);
            ViewBag.CPPList = db.Kaizen_Master_CostBenefit_CPP.Where(x => x.Start_Date == CPPLatestDate).OrderBy(o => o.Area).ToList();
            var UMP = db.Kaizen_Master_CostBenefit_UMP.Where(x => x.Period == period).FirstOrDefault()?.UMP ?? 0;
            ViewBag.ManMinute = UMP > 0 ? Math.Round((decimal)(UMP / 22 / 8 / 60)) : 0;
            //if (impType != "%")
            //{
            //    ViewBag.kaizenList = db.Kaizen_Data.Where(x => x.OCDScore >= 16 && x.improveType.Contains(impType) && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd) && x.Division == currUserDivName).OrderByDescending(x => x.ID).ToList();
            //}
            //else
            //{
            //    ViewBag.kaizenList = db.Kaizen_Data.Where(x => x.OCDScore >= 16 && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
            //}
            ViewBag.kaizenList = db.Kaizen_Data.Where(x => (x.OCDScore >= 16 && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd) && x.issuedDate.Year <= 2024) || (x.OCDScore >= 16 && x.CostBenefitTotal > 0 && (x.issuedDate >= rangeStart && x.issuedDate < rangeEnd) && x.issuedDate.Year >= 2025)).OrderByDescending(x => x.ID).ToList();

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
            updateData.KOCScore = currScore;
            updateData.Reward = currReward;
            updateData.KOCDate = DateTime.Now;
            updateData.KOCBy = currUser.GetUserId();
            updateData.KOCNote = currNote;
            if (currReward > 0)
            {
                updateData.SCScore = 0;
                updateData.SCDate = null;
                updateData.SCBy = null;
            }
            for (var i = 1; i <= 4; i++)
            {
                if (Request["iKOCScore" + i] != null)
                {
                    string[] currKOC = Request["iKOCScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                    var updateScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i + 4);

                    if (updateScore.Count() > 0)
                    {
                        var updateDBScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i + 4).FirstOrDefault();
                        updateDBScore.subCatID = Int32.Parse(currKOC[0]);
                        updateDBScore.Score = double.Parse(currKOC[1]);
                    }
                    else
                    {
                        db.Kaizen_Score.Add(new Kaizen_Score
                        {
                            dataID = currDataID,
                            catID = i + 4,
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
        public ActionResult ScoreRevise(HttpPostedFileBase iScan)
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
            updateData.KOCScore = currScore;
            updateData.Reward = currReward;
            updateData.KOCDate = DateTime.Now;
            updateData.KOCReviseDate = DateTime.Now;
            updateData.KOCRevise = currUser.GetUserId();
            updateData.KOCNote = currNote;
            updateData.SCScore = 0;
            updateData.SCDate = null;
            updateData.SCBy = null;
            for (var i = 1; i <= 4; i++)
            {
                if (Request["iKOCScore" + i] != null)
                {
                    string[] currKOC = Request["iKOCScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                    var updateScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i + 4);

                    if (updateScore.Count() > 0)
                    {
                        var updateDBScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i + 4).FirstOrDefault();
                        updateDBScore.subCatID = Int32.Parse(currKOC[0]);
                        updateDBScore.Score = double.Parse(currKOC[1]);
                    }
                    else
                    {
                        db.Kaizen_Score.Add(new Kaizen_Score
                        {
                            dataID = currDataID,
                            catID = i + 4,
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
        public JsonResult getCurrScore(Int32 iID, Int32 iCatID)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var userID = currUser.GetUserId();
            var getScore = db.Kaizen_Score.Where(z => z.dataID == iID && z.catID == iCatID).Select(x => new
            {
                ID = x.ID,
                dataID = x.dataID,
                catID = x.catID,
                subCatID = x.subCatID,
                Score = x.Score,
                ScoredDate = x.Kaizen_Data.KOCDate,
                ScoredBy = x.Kaizen_Data.KOCBy,
                ScoredByName = x.Kaizen_Data.KOCUser.Name,
                ScoredReviserName = x.Kaizen_Data.KOCReviser.Name,
                ScoredNote = x.Kaizen_Data.KOCNote,
                currUser = userID,
                SCScore = x.Kaizen_Data.SCScore,
                SCScoreBy = x.Kaizen_Data.SCBy
            }).ToList();
            var getData = db.Kaizen_Data.Where(z => z.ID == iID).Select(x => new
            {
                Score = x.KOCScore,
                ScoredDate = x.KOCDate,
                ScoredBy = x.KOCBy,
                ScoredByName = x.KOCUser.Name,
                ScoredReviserName = x.KOCReviser.Name,
                ScoredNote = x.KOCNote,
                currUser = userID,
                SCScore = x.SCScore,
                SCScoreBy = x.SCBy
            }).ToList();
            if (getScore.Count == 0)
            {
                return Json(getData);
            }
            return Json(getScore);
        }

        //[HttpPost]
        //public ActionResult Benefit()
        //{
        //    var currUser = (ClaimsIdentity)User.Identity;
        //    var currUserID = currUser.GetUserId();
        //    var currURL = Request.UrlReferrer;
        //    var currNote = Request["iScoreNote"];
        //    var currDataID = Int32.Parse(Request["iDataID"]);
        //    var currSavings = double.Parse(Request["iSavings"]);
        //    var currUnit = (Request["iUnitOther"] != "" ? Request["iUnitOther"] : Request["iUnit"]);
        //    var currValue = Int32.Parse(Request["iValue"]);
        //    var currInvest = Int32.Parse(Request["iInvest"]);
        //    var currTotal = Int32.Parse(Request["iBenefit"]);

        //    var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
        //    updateData.BenefitSavings = currSavings;
        //    updateData.BenefitUnit = currUnit;
        //    updateData.BenefitValue = currValue;
        //    updateData.BenefitInvest = currInvest;
        //    updateData.BenefitTotal = currTotal;
        //    updateData.BenefitBy = currUserID;
        //    updateData.SCNote = currNote;
        //    db.SaveChanges();

        //    return Redirect(currURL.AbsoluteUri);
        //}

        [HttpPost]
        public ActionResult Lock()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var currDataID = Int32.Parse(Request["iID"]);
            var isLocked = true;
            var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
            if (updateData.KOCBy == null)
            {
                updateData.KOCBy = currUserID;
                isLocked = true;
            }
            else
            {
                updateData.KOCBy = null;
                isLocked = false;
            }
            db.SaveChanges();

            return Json(new { locked = isLocked });
        }
    }
}