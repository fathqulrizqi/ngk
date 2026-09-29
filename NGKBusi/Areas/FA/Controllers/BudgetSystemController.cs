using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace NGKBusi.Areas.FA.Controllers
{
    public class BudgetSystemController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: FA/BudgetSystem
        public ActionResult Index()
        {
            return View();
        }

        public class returnSectionList
        {
            public string Section_From_Code { get; set; }
            public string Section_From_Name { get; set; }

        }
        public class _VersionList
        {
            public int Version { get; set; }
            public int Final_Version { get; set; }

        }
        public class returnSectionListDistinct : IEqualityComparer<returnSectionList>
        {
            public bool Equals(returnSectionList x, returnSectionList y)
            {
                // compare multiple fields
                return
                    x.Section_From_Code == y.Section_From_Code &&
                    x.Section_From_Name == y.Section_From_Name;
            }

            public int GetHashCode(returnSectionList obj)
            {
                return
                    obj.Section_From_Code.GetHashCode() +
                    obj.Section_From_Name.GetHashCode();
            }
        }

        public class _VersionListDistinct : IEqualityComparer<_VersionList>
        {
            public bool Equals(_VersionList x, _VersionList y)
            {
                // compare multiple fields
                return
                    x.Version == y.Version &&
                    x.Final_Version == y.Final_Version;
            }

            public int GetHashCode(_VersionList obj)
            {
                return
                    obj.Version.GetHashCode() +
                    obj.Final_Version.GetHashCode();
            }
        }

        public void BudgetSystem_Expense(List<Approval_Master> getApprovalList, String _Budget_Type)
        {
            var _period = string.IsNullOrEmpty(Request["iPeriod"]) ? (("FY1" + (DateTime.Now.Month < 10 ? DateTime.Now.AddYears(1) : DateTime.Now.AddYears(2)).ToString("yy")) + "|" + (DateTime.Now.Month < 10 ? (DateTime.Now.AddYears(-1)).Year : DateTime.Now.Year)).Split('|') : Request["iPeriod"].Split('|');
            var _period_FY = _period[0];
            var _period_Year = int.Parse(_period[1]);
            var getApprovalLevel = getApprovalList.Select(s => s.Levels + "-" + s.Levels_Sub).Distinct();
            ViewBag.Level = string.IsNullOrEmpty(Request["iLevel"]) ? getApprovalLevel.FirstOrDefault() : Request["iLevel"];
            var currLevel = ViewBag.Level.ToString().Split('-');
            int currApproval = int.Parse(currLevel[0]);
            int currApprovalSub = int.Parse(currLevel[1]);
            var getSectionList = (string.IsNullOrEmpty(Request["iSection"]) ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : (Request["iSection"] == "All" ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).Where(w => w.Dept_Code == Request["iSection"]).Select(s => s.Dept_Code).Distinct()));
            var getApprovalSection = getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct();

            ViewBag.SectionChoose = string.IsNullOrEmpty(Request["iSection"]) ? "All" : Request["iSection"];
            ViewBag.Period = _period[0] + "|" + _period[1];
            var DataList = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Budget_Type == _Budget_Type && w.Period_FY == _period_FY && ((w.Approval >= currApproval && w.Approval_Sub >= currApprovalSub) || w.Approval > currApproval) && getSectionList.Contains(w.Section_From_Code)).OrderBy(o => o.Budget_No).ToList();
            var LatestVersion = string.IsNullOrEmpty(Request["iVersion"]) ? (DataList.Count() > 0 ? DataList.OrderByDescending(o => o.Version).First().Version : 0) : int.Parse(Request["iVersion"]);

            ViewBag.VersionList = DataList.Count() > 0 ? DataList.Select(s => new _VersionList { Version = s.Version, Final_Version = s.Final_Version }).Distinct(new _VersionListDistinct()) : null;
            ViewBag.ReturnSectionList = DataList.Where(w => w.Latest == 1).Select(s => new returnSectionList { Section_From_Code = s.Section_From_Code, Section_From_Name = s.Section_From_Name }).Distinct(new returnSectionListDistinct()).ToList();
            if (ViewBag.SectionChoose == "All")
            {
                DataList = DataList.Where(w => w.Latest == 1).ToList();
            }
            else
            {
                DataList = DataList.Where(w => w.Version == LatestVersion).ToList();
            }

            ViewBag.Version = LatestVersion;
            ViewBag.DataList = DataList;
            ViewBag.SectionList = getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).Select(s => new Approval_Master { Dept_Code = s.Dept_Code, Dept_Name = s.Dept_Name }).ToList();
            ViewBag.ApprovalLevel = getApprovalLevel;
            ViewBag.StatusSectionList = getSectionList;
            ViewBag.StatusSection = db.FA_Section.AsEnumerable().Where(w => getSectionList.Contains(w.New_Section_Code) && w.Period == _period_Year).ToList();
            ViewBag.StatusData = db.V_FA_BudgetSystem_Approval_Status.Where(w => w.Period == _period_Year && w.Budget_Type == _Budget_Type && getApprovalSection.Contains(w.Section_Code)).ToList();

        }


        public void BudgetSystem_Investment(List<Approval_Master> getApprovalList, String _Budget_Type)
        {
            var _period = string.IsNullOrEmpty(Request["iPeriod"]) ? (("FY1" + (DateTime.Now.Month < 10 ? DateTime.Now.AddYears(1) : DateTime.Now.AddYears(2)).ToString("yy")) + "|" + (DateTime.Now.Month < 10 ? (DateTime.Now.AddYears(-1)).Year : DateTime.Now.Year)).Split('|') : Request["iPeriod"].Split('|');
            var _period_FY = _period[0];
            var _period_Year = int.Parse(_period[1]);
            var getApprovalLevel = getApprovalList.Select(s => s.Levels + "-" + s.Levels_Sub).Distinct();
            ViewBag.Level = string.IsNullOrEmpty(Request["iLevel"]) ? getApprovalLevel.FirstOrDefault() : Request["iLevel"];

            var currLevel = ViewBag.Level.ToString().Split('-');
            int currApproval = int.Parse(currLevel[0]);
            int currApprovalSub = int.Parse(currLevel[1]);
            var getSectionList = (string.IsNullOrEmpty(Request["iSection"]) ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : (Request["iSection"] == "All" ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).Where(w => w.Dept_Code == Request["iSection"]).Select(s => s.Dept_Code).Distinct()));
            var getApprovalSection = getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct();

            ViewBag.SectionChoose = string.IsNullOrEmpty(Request["iSection"]) ? "All" : Request["iSection"];
            ViewBag.Period = _period[0] + "|" + _period[1];
            var DataList = db.V_FA_BudgetSystem_BIP_CIP_CFA.Where(w => w.Budget_Type == _Budget_Type && w.Period_FY == _period_FY && ((w.Approval >= currApproval && w.Approval_Sub >= currApprovalSub) || w.Approval > currApproval) && getSectionList.Contains(w.Section_From_Code)).OrderBy(o => o.Budget_No).ToList();
            var LatestVersion = string.IsNullOrEmpty(Request["iVersion"]) ? (DataList.Count() > 0 ? DataList.OrderByDescending(o => o.Version).First().Version : 0) : int.Parse(Request["iVersion"]);
            ViewBag.VersionList = DataList.Count() > 0 ? DataList.Select(s => new _VersionList { Version = s.Version, Final_Version = s.Final_Version }).Distinct(new _VersionListDistinct()) : null;
            ViewBag.ReturnSectionList = DataList.Where(w => w.Latest == 1).Select(s => new returnSectionList { Section_From_Code = s.Section_From_Code, Section_From_Name = s.Section_From_Name }).Distinct(new returnSectionListDistinct()).ToList();

            if (ViewBag.SectionChoose == "All")
            {
                DataList = DataList.Where(w => w.Latest == 1).ToList();
            }
            else
            {
                DataList = DataList.Where(w => w.Version == LatestVersion).ToList();
            }

            ViewBag.Version = LatestVersion;
            ViewBag.DataList = DataList;
            ViewBag.SectionList = getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).Select(s => new Approval_Master { Dept_Code = s.Dept_Code, Dept_Name = s.Dept_Name }).Distinct().ToList();
            ViewBag.ApprovalLevel = getApprovalLevel;
            ViewBag.StatusSection = db.FA_Section.AsEnumerable().Where(w => getSectionList.Contains(w.New_Section_Code) && w.Period == _period_Year).ToList();
            ViewBag.StatusData = db.V_FA_BudgetSystem_Approval_Status.Where(w => w.Period == _period_Year && w.Budget_Type == _Budget_Type && getApprovalSection.Contains(w.Section_Code)).ToList();

        }

        [Authorize]
        public ActionResult Expense()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();
            if (getApprovalList.Count() == 0)
            {
                return View("UnAuthorized");
            }
            BudgetSystem_Expense(getApprovalList, "BEX");
            return View();
        }

        [Authorize]
        public ActionResult ExpenseSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();

            var signID = Request["iSignID"].ToString().Split(',').Select(int.Parse);
            var allSignID = Request["iAllSignID"].ToString().Split(',')?.Select(int.Parse).Except(signID);
            var signLevel = Request["iSignLevel"].ToString().Split('-');
            int signLevelApproval = int.Parse(signLevel[0]);
            int signLevelApprovalSub = int.Parse(signLevel[1]);

            var updateData = db.FA_BudgetSystem_BEX.Where(w => signID.Contains(w.id)).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels ?? signLevelApproval + 1;
                f.Approval_Sub = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels_Sub ?? 0;
            });
            var rejectData = db.FA_BudgetSystem_BEX.Where(w => allSignID.Contains(w.id)).ToList();
            if (rejectData.Count() > 0)
            {
                rejectData.ForEach(f =>
                {
                    f.Is_Reject = true;
                });
            }
            db.SaveChanges();

            return RedirectToAction("Expense", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult ExpenseReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var currSection = Request["iReturnSection"];
            var currPeriod = Request["iReturnPeriod"].ToString().Split('|');
            var currLevel = Request["iReturnLevel"].ToString().Split('-');
            var currVersion = int.Parse(Request["iReturnVersion"]);
            var _Period_FY = currPeriod[0];
            var _Approval = int.Parse(currLevel[0]);
            var _ApprovalSub = int.Parse(currLevel[1]);

            var updateData = db.FA_BudgetSystem_BEX.Where(w => w.Section_From_Code == currSection && w.Period_FY == _Period_FY).ToList();
            var latestVersion = updateData.OrderByDescending(o => o.Version).First().Version;
            updateData = updateData.Where(w => w.Version == latestVersion).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = 1;
                f.Approval_Sub = 0;
                f.Is_Reject = false;
            });

            db.SaveChanges();

            return RedirectToAction("Expense", "BudgetSystem", new { area = "FA" });
        }

        [Authorize]
        public ActionResult Labor()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 59 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();
            if (getApprovalList.Count() == 0)
            {
                return View("UnAuthorized");
            }
            BudgetSystem_Expense(getApprovalList, "BEL");
            return View();
        }
        [Authorize]
        public ActionResult LaborSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 59 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();

            var signID = Request["iSignID"].ToString().Split(',').Select(int.Parse);
            var allSignID = Request["iAllSignID"].ToString().Split(',')?.Select(int.Parse).Except(signID);
            var signLevel = Request["iSignLevel"].ToString().Split('-');
            int signLevelApproval = int.Parse(signLevel[0]);
            int signLevelApprovalSub = int.Parse(signLevel[1]);

            var updateData = db.FA_BudgetSystem_BEL.Where(w => signID.Contains(w.id)).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = db.Approval_Master.Where(w => w.Menu_Id == 59 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels ?? signLevelApproval + 1;
                f.Approval_Sub = db.Approval_Master.Where(w => w.Menu_Id == 59 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels_Sub ?? 0;
            });
            var rejectData = db.FA_BudgetSystem_BEL.Where(w => allSignID.Contains(w.id)).ToList();
            if (rejectData.Count() > 0)
            {
                rejectData.ForEach(f =>
                {
                    f.Is_Reject = true;
                });
            }
            db.SaveChanges();

            return RedirectToAction("Labor", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult LaborReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var currSection = Request["iReturnSection"];
            var currPeriod = Request["iReturnPeriod"].ToString().Split('|');
            var currLevel = Request["iReturnLevel"].ToString().Split('-');
            var currVersion = int.Parse(Request["iReturnVersion"]);
            var _Period_FY = currPeriod[0];
            var _Approval = int.Parse(currLevel[0]);
            var _ApprovalSub = int.Parse(currLevel[1]);

            var updateData = db.FA_BudgetSystem_BEL.Where(w => w.Section_From_Code == currSection && w.Period_FY == _Period_FY).ToList();
            var latestVersion = updateData.OrderByDescending(o => o.Version).First().Version;
            updateData = updateData.Where(w => w.Version == latestVersion).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = 1;
                f.Approval_Sub = 0;
                f.Is_Reject = false;
            });

            db.SaveChanges();

            return RedirectToAction("Labor", "BudgetSystem", new { area = "FA" });
        }

        [Authorize]
        public ActionResult Investment()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();
            if (getApprovalList.Count() == 0)
            {
                return View("UnAuthorized");
            }
            BudgetSystem_Investment(getApprovalList, "BIP");

            return View();
        }

        [Authorize]
        public ActionResult InvestmentSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();

            var signID = Request["iSignID"].ToString().Split(',').Select(int.Parse);
            var allSignID = Request["iAllSignID"].ToString().Split(',')?.Select(int.Parse).Except(signID);
            var signLevel = Request["iSignLevel"].ToString().Split('-');
            int signLevelApproval = int.Parse(signLevel[0]);
            int signLevelApprovalSub = int.Parse(signLevel[1]);

            var updateData = db.FA_BudgetSystem_BIP.Where(w => signID.Contains(w.id)).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels ?? signLevelApproval + 1;
                f.Approval_Sub = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels_Sub ?? 0;
            });
            var rejectData = db.FA_BudgetSystem_BIP.Where(w => allSignID.Contains(w.id)).ToList();
            if (rejectData.Count() > 0)
            {
                rejectData.ForEach(f =>
                {
                    f.Is_Reject = true;
                });
            }
            db.SaveChanges();

            return RedirectToAction("Investment", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult InvestmentReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var currSection = Request["iReturnSection"];
            var currPeriod = Request["iReturnPeriod"].ToString().Split('|');
            var currLevel = Request["iReturnLevel"].ToString().Split('-');
            var currVersion = int.Parse(Request["iReturnVersion"]);
            var _Period_FY = currPeriod[0];
            var _Approval = int.Parse(currLevel[0]);
            var _ApprovalSub = int.Parse(currLevel[1]);

            var updateData = db.FA_BudgetSystem_BIP.Where(w => w.Section_From_Code == currSection && w.Period_FY == _Period_FY).ToList();
            var latestVersion = updateData.OrderByDescending(o => o.Version).First().Version;
            updateData = updateData.Where(w => w.Version == latestVersion).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = 1;
                f.Approval_Sub = 0;
                f.Is_Reject = false;
            });

            db.SaveChanges();

            return RedirectToAction("Investment", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult AssetInProgress()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();
            if (getApprovalList.Count() == 0)
            {
                return View("UnAuthorized");
            }
            BudgetSystem_Investment(getApprovalList, "CIP");

            return View();
        }

        [Authorize]
        public ActionResult AssetInProgressSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();

            var signID = Request["iSignID"].ToString().Split(',').Select(int.Parse);
            var allSignID = Request["iAllSignID"].ToString().Split(',')?.Select(int.Parse).Except(signID);
            var signLevel = Request["iSignLevel"].ToString().Split('-');
            int signLevelApproval = int.Parse(signLevel[0]);
            int signLevelApprovalSub = int.Parse(signLevel[1]);

            var updateData = db.FA_BudgetSystem_CIP.Where(w => signID.Contains(w.id)).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels ?? signLevelApproval + 1;
                f.Approval_Sub = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels_Sub ?? 0;
            });
            var rejectData = db.FA_BudgetSystem_CIP.Where(w => allSignID.Contains(w.id)).ToList();
            if (rejectData.Count() > 0)
            {
                rejectData.ForEach(f =>
                {
                    f.Is_Reject = true;
                });
            }
            db.SaveChanges();

            return RedirectToAction("Investment", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult AssetInProgressReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var currSection = Request["iReturnSection"];
            var currPeriod = Request["iReturnPeriod"].ToString().Split('|');
            var currLevel = Request["iReturnLevel"].ToString().Split('-');
            var currVersion = int.Parse(Request["iReturnVersion"]);
            var _Period_FY = currPeriod[0];
            var _Approval = int.Parse(currLevel[0]);
            var _ApprovalSub = int.Parse(currLevel[1]);

            var updateData = db.FA_BudgetSystem_CIP.Where(w => w.Section_From_Code == currSection && w.Period_FY == _Period_FY).ToList();
            var latestVersion = updateData.OrderByDescending(o => o.Version).First().Version;
            updateData = updateData.Where(w => w.Version == latestVersion).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = 1;
                f.Approval_Sub = 0;
                f.Is_Reject = false;
            });

            db.SaveChanges();

            return RedirectToAction("Investment", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult CurrentAsset()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 62 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();
            if (getApprovalList.Count() == 0)
            {
                return View("UnAuthorized");
            }
            BudgetSystem_Investment(getApprovalList, "CFA");

            return View();
        }

        [Authorize]
        public ActionResult CurrentAssetSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 62 && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();

            var signID = Request["iSignID"].ToString().Split(',').Select(int.Parse);
            var allSignID = Request["iAllSignID"].ToString().Split(',')?.Select(int.Parse).Except(signID);
            var signLevel = Request["iSignLevel"].ToString().Split('-');
            int signLevelApproval = int.Parse(signLevel[0]);
            int signLevelApprovalSub = int.Parse(signLevel[1]);

            var updateData = db.FA_BudgetSystem_CFA.Where(w => signID.Contains(w.id)).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = db.Approval_Master.Where(w => w.Menu_Id == 62 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels ?? signLevelApproval + 1;
                f.Approval_Sub = db.Approval_Master.Where(w => w.Menu_Id == 62 && w.Document_Id == 1 && w.Dept_Code == f.Section_From_Code && ((w.Levels + w.Levels_Sub) > (f.Approval + f.Approval_Sub))).OrderBy(o => o.Levels).ThenBy(t => t.Levels_Sub).FirstOrDefault()?.Levels_Sub ?? signLevelApproval + 1;
            });
            var rejectData = db.FA_BudgetSystem_CFA.Where(w => allSignID.Contains(w.id)).ToList();
            if (rejectData.Count() > 0)
            {
                rejectData.ForEach(f =>
                {
                    f.Is_Reject = true;
                });
            }
            db.SaveChanges();

            return RedirectToAction("Investment", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult CurrentAssetReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var currSection = Request["iReturnSection"];
            var currPeriod = Request["iReturnPeriod"].ToString().Split('|');
            var currLevel = Request["iReturnLevel"].ToString().Split('-');
            var currVersion = int.Parse(Request["iReturnVersion"]);
            var _Period_FY = currPeriod[0];
            var _Approval = int.Parse(currLevel[0]);
            var _ApprovalSub = int.Parse(currLevel[1]);

            var updateData = db.FA_BudgetSystem_CIP.Where(w => w.Section_From_Code == currSection && w.Period_FY == _Period_FY).ToList();
            var latestVersion = updateData.OrderByDescending(o => o.Version).First().Version;
            updateData = updateData.Where(w => w.Version == latestVersion).ToList();
            updateData.ForEach(f =>
            {
                f.Approval = 1;
                f.Approval_Sub = 0;
                f.Is_Reject = false;
            });

            db.SaveChanges();

            return RedirectToAction("Investment", "BudgetSystem", new { area = "FA" });
        }
        public ActionResult SummarizeBudget()
        {
            var _period = string.IsNullOrEmpty(Request["iPeriod"]) ? (("FY1" + DateTime.Now.AddYears(2).ToString("yy")) + "|" + DateTime.Now.Year).Split('|') : Request["iPeriod"].Split('|');
            var _period_FY = _period[0];
            var _period_Year = int.Parse(_period[1]);

            var DataListBEX = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Budget_Type == "BEX" && w.Period_FY == _period_FY && w.Latest == 1).ToList().Select(s => new returnSectionList { Section_From_Code = s.Section_From_Code, Section_From_Name = s.Section_From_Name }).Distinct(new returnSectionListDistinct()).ToList();
            var DataListBIP = db.V_FA_BudgetSystem_BIP_CIP_CFA.Where(w => w.Budget_Type == "BIP" && w.Period_FY == _period_FY && w.Latest == 1).ToList().Select(s => new returnSectionList { Section_From_Code = s.Section_From_Code, Section_From_Name = s.Section_From_Name }).Distinct(new returnSectionListDistinct()).ToList();

            ViewBag.Period = _period[0] + "|" + _period[1];
            ViewBag.StatusSection = db.FA_Section.AsEnumerable().Where(w => w.Period == _period_Year).ToList();
            ViewBag.StatusData = db.V_FA_BudgetSystem_Approval_Status.Where(w => w.Period == _period_Year).OrderBy(o => o.Budget_Type).ToList();
            ViewBag.ReturnSectionList = DataListBEX.Concat(DataListBIP).Distinct(new returnSectionListDistinct()).ToList();

            return View();
        }
        public ActionResult Finalize()
        {
            var _period = string.IsNullOrEmpty(Request["iPeriod"]) ? (("FY1" + DateTime.Now.AddYears(1).ToString("yy")) + "|" + (DateTime.Now.Year - 1)).Split('|') : Request["iPeriod"].Split('|');
            var _period_FY = _period[0];
            var _period_Year = int.Parse(_period[1]);

            ViewBag.Period = _period[0] + "|" + _period[1];
            ViewBag.StatusSection = db.FA_Section.AsEnumerable().Where(w => w.Period == _period_Year).ToList();
            ViewBag.StatusData = db.V_FA_BudgetSystem_Approval_Status.Where(w => w.Period == _period_Year).OrderBy(o => o.Budget_Type).ToList();


            return View();
        }

        [HttpPost]
        public JsonResult _GetDataBEX()
        {
            return _GetData("Data$", 33, "BEX");
        }
        [HttpPost]
        public JsonResult _GetDataBEL()
        {
            return _GetData("Data$", 33, "BEL");
        }
        [HttpPost]
        public JsonResult _GetDataBIP()
        {
            return _GetData("Data$", 16, "BIP");
        }
        [HttpPost]
        public JsonResult _GetDataCIP()
        {
            return _GetData("Data$", 16, "CIP");
        }
        [HttpPost]
        public JsonResult _GetDataCFA()
        {
            return _GetData("Data$", 16, "CFA");
        }
        [HttpPost]
        public JsonResult UploadBEXBELFinal()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var data = Request["uploadData"].Split('|');
            if (data[0] == "BEX" || data[0] == "BEL")
            {
                var _period = Request["period"].Split('|');
                var _period_FY = _period[0];
                var level = Request["level"].ToString().Split('-');
                int levelApproval = int.Parse(level[0]);
                int levelApprovalSub = int.Parse(level[1]);
                //var checkDept = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.User_NIK == currUserID && w.Levels == levelApproval && w.Levels_Sub == levelApprovalSub).ToList();

                var _Section_From_Code = data[1];
                var _Section_From_Name = data[2];
                var currVersion = 0;
                var currFinalVersion = 0;

                // for get latest budget version
                var minus3Second = DateTime.Now.AddSeconds(-60);
                if (data[0] == "BEX")
                {
                    var checkData = db.FA_BudgetSystem_BEX.Where(w => w.Period_FY == _period_FY && w.Section_From_Code == _Section_From_Code && w.Created_At <= minus3Second && w.Is_Reject == false).OrderByDescending(o => o.Version).FirstOrDefault();
                    if (checkData != null)
                    {
                        currVersion = checkData.Version + 1;
                        currFinalVersion = checkData.Final_Version + 1;
                    }
                    var entity = new FA_BudgetSystem_BEX();
                    AssignBEXBELFields(entity, data, _period, currUserID, levelApproval, levelApprovalSub, currVersion, currFinalVersion);
                    db.FA_BudgetSystem_BEX.Add(entity);
                }
                else
                {
                    var checkData = db.FA_BudgetSystem_BEL.Where(w => w.Period_FY == _period_FY && w.Section_From_Code == _Section_From_Code && w.Created_At <= minus3Second && w.Is_Reject == false).OrderByDescending(o => o.Version).FirstOrDefault();
                    if (checkData != null)
                    {
                        currVersion = checkData.Version + 1;
                        currFinalVersion = checkData.Final_Version + 1;
                    }
                    var entity = new FA_BudgetSystem_BEL();
                    AssignBEXBELFields(entity, data, _period, currUserID, levelApproval, levelApprovalSub, currVersion, currFinalVersion);
                    db.FA_BudgetSystem_BEL.Add(entity);
                }
                db.SaveChanges();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        private void AssignBEXBELFields(dynamic entity, string[] data, string[] period, string currUserID, int levelApproval, int levelApprovalSub, int currVersion, int currFinalVersion = 0)
        {
            entity.Period_FY = period[0];
            entity.Period_Year = int.Parse(period[1]);
            entity.Budget_Type = data[0];
            entity.Section_From_Code = data[1];
            entity.Section_From_Name = data[2];
            entity.Section_To_Code = data[3];
            entity.Section_To_Name = data[4];
            entity.COA_Code = data[5];
            entity.COA_Name = data[6];
            entity.Description = data[7];
            entity.Group_Section = data[8];
            entity.Group_Cost = data[9];
            entity.month010 = data[10].Length > 0 && data[10] != "0" ? (Int64)Math.Round(decimal.Parse(data[10], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month011 = data[11].Length > 0 && data[11] != "0" ? (Int64)Math.Round(decimal.Parse(data[11], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month012 = data[12].Length > 0 && data[12] != "0" ? (Int64)Math.Round(decimal.Parse(data[12], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month101 = data[13].Length > 0 && data[13] != "0" ? (Int64)Math.Round(decimal.Parse(data[13], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month102 = data[14].Length > 0 && data[14] != "0" ? (Int64)Math.Round(decimal.Parse(data[14], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month103 = data[15].Length > 0 && data[15] != "0" ? (Int64)Math.Round(decimal.Parse(data[15], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month104 = data[16].Length > 0 && data[16] != "0" ? (Int64)Math.Round(decimal.Parse(data[16], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month105 = data[17].Length > 0 && data[17] != "0" ? (Int64)Math.Round(decimal.Parse(data[17], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month106 = data[18].Length > 0 && data[18] != "0" ? (Int64)Math.Round(decimal.Parse(data[18], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month107 = data[19].Length > 0 && data[19] != "0" ? (Int64)Math.Round(decimal.Parse(data[19], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month108 = data[20].Length > 0 && data[20] != "0" ? (Int64)Math.Round(decimal.Parse(data[20], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month109 = data[21].Length > 0 && data[21] != "0" ? (Int64)Math.Round(decimal.Parse(data[21], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month110 = data[22].Length > 0 && data[22] != "0" ? (Int64)Math.Round(decimal.Parse(data[22], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month111 = data[23].Length > 0 && data[23] != "0" ? (Int64)Math.Round(decimal.Parse(data[23], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month112 = data[24].Length > 0 && data[24] != "0" ? (Int64)Math.Round(decimal.Parse(data[24], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month201 = data[25].Length > 0 && data[25] != "0" ? (Int64)Math.Round(decimal.Parse(data[25], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month202 = data[26].Length > 0 && data[26] != "0" ? (Int64)Math.Round(decimal.Parse(data[26], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.month203 = data[27].Length > 0 && data[27] != "0" ? (Int64)Math.Round(decimal.Parse(data[27], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.TotalFY1 = data[28].Length > 0 && data[28] != "0" ? (Int64)Math.Round(decimal.Parse(data[28], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.TotalFY2 = data[29].Length > 0 && data[29] != "0" ? (Int64)Math.Round(decimal.Parse(data[29], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.TotalFY3 = data[30].Length > 0 && data[30] != "0" ? (Int64)Math.Round(decimal.Parse(data[30], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.TotalFY4 = data[31].Length > 0 && data[31] != "0" ? (Int64)Math.Round(decimal.Parse(data[31], CultureInfo.InvariantCulture)) : (Int64?)null;
            entity.Priority_Category = data[32];
            entity.Created_At = DateTime.Now;
            entity.Created_By = currUserID;
            entity.Approval = levelApproval;
            entity.Approval_Sub = levelApprovalSub;
            entity.Version = currVersion;
            entity.Budget_No = data[data.Length - 1];
            entity.Is_Reject = false;
            entity.Final_Version = currFinalVersion;
        }

        [HttpPost]
        public JsonResult UploadBEX()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var data = Request["uploadData"].Split('|');
            var _period = Request["period"].Split('|');
            var _period_FY = _period[0];
            var level = Request["level"].ToString().Split('-');
            int levelApproval = int.Parse(level[0]);
            int levelApprovalSub = int.Parse(level[1]);
            var checkDept = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.User_NIK == currUserID && w.Levels == levelApproval && w.Levels_Sub == levelApprovalSub).ToList();

            var _Section_From_Code = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Code : data[1]);
            var _Section_From_Name = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Name : data[2]);
            var currVersion = 0;

            // for get latest budget version
            var minus3Second = DateTime.Now.AddSeconds(-30);
            var checkData = db.FA_BudgetSystem_BEX.Where(w => w.Period_FY == _period_FY && w.Section_From_Code == _Section_From_Code && w.Created_At <= minus3Second && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).OrderByDescending(o => o.Version).FirstOrDefault();
            //if (checkData != null && checkData.Approval > 1)
            if (checkData != null)
            {
                currVersion = checkData.Version + 1;
            }

            db.FA_BudgetSystem_BEX.Add(new FA_BudgetSystem_BEX
            {
                Period_FY = _period[0],
                Period_Year = int.Parse(_period[1]),
                Budget_Type = data[0],
                Section_From_Code = _Section_From_Code,
                Section_From_Name = _Section_From_Name,
                Section_To_Code = data[3],
                Section_To_Name = data[4],
                COA_Code = data[5],
                COA_Name = data[6],
                Description = data[7],
                Group_Section = data[8],
                Group_Cost = data[9],
                month010 = data[10].Length > 0 && data[10] != "0" ? (Int64)Math.Round(decimal.Parse(data[10], CultureInfo.InvariantCulture)) : (Int64?)null,
                month011 = data[11].Length > 0 && data[11] != "0" ? (Int64)Math.Round(decimal.Parse(data[11], CultureInfo.InvariantCulture)) : (Int64?)null,
                month012 = data[12].Length > 0 && data[12] != "0" ? (Int64)Math.Round(decimal.Parse(data[12], CultureInfo.InvariantCulture)) : (Int64?)null,
                month101 = data[13].Length > 0 && data[13] != "0" ? (Int64)Math.Round(decimal.Parse(data[13], CultureInfo.InvariantCulture)) : (Int64?)null,
                month102 = data[14].Length > 0 && data[14] != "0" ? (Int64)Math.Round(decimal.Parse(data[14], CultureInfo.InvariantCulture)) : (Int64?)null,
                month103 = data[15].Length > 0 && data[15] != "0" ? (Int64)Math.Round(decimal.Parse(data[15], CultureInfo.InvariantCulture)) : (Int64?)null,
                month104 = data[16].Length > 0 && data[16] != "0" ? (Int64)Math.Round(decimal.Parse(data[16], CultureInfo.InvariantCulture)) : (Int64?)null,
                month105 = data[17].Length > 0 && data[17] != "0" ? (Int64)Math.Round(decimal.Parse(data[17], CultureInfo.InvariantCulture)) : (Int64?)null,
                month106 = data[18].Length > 0 && data[18] != "0" ? (Int64)Math.Round(decimal.Parse(data[18], CultureInfo.InvariantCulture)) : (Int64?)null,
                month107 = data[19].Length > 0 && data[19] != "0" ? (Int64)Math.Round(decimal.Parse(data[19], CultureInfo.InvariantCulture)) : (Int64?)null,
                month108 = data[20].Length > 0 && data[20] != "0" ? (Int64)Math.Round(decimal.Parse(data[20], CultureInfo.InvariantCulture)) : (Int64?)null,
                month109 = data[21].Length > 0 && data[21] != "0" ? (Int64)Math.Round(decimal.Parse(data[21], CultureInfo.InvariantCulture)) : (Int64?)null,
                month110 = data[22].Length > 0 && data[22] != "0" ? (Int64)Math.Round(decimal.Parse(data[22], CultureInfo.InvariantCulture)) : (Int64?)null,
                month111 = data[23].Length > 0 && data[23] != "0" ? (Int64)Math.Round(decimal.Parse(data[23], CultureInfo.InvariantCulture)) : (Int64?)null,
                month112 = data[24].Length > 0 && data[24] != "0" ? (Int64)Math.Round(decimal.Parse(data[24], CultureInfo.InvariantCulture)) : (Int64?)null,
                month201 = data[25].Length > 0 && data[25] != "0" ? (Int64)Math.Round(decimal.Parse(data[25], CultureInfo.InvariantCulture)) : (Int64?)null,
                month202 = data[26].Length > 0 && data[26] != "0" ? (Int64)Math.Round(decimal.Parse(data[26], CultureInfo.InvariantCulture)) : (Int64?)null,
                month203 = data[27].Length > 0 && data[27] != "0" ? (Int64)Math.Round(decimal.Parse(data[27], CultureInfo.InvariantCulture)) : (Int64?)null,
                TotalFY1 = data[28].Length > 0 && data[28] != "0" ? (Int64)Math.Round(decimal.Parse(data[28], CultureInfo.InvariantCulture)) : (Int64?)null,
                TotalFY2 = data[29].Length > 0 && data[29] != "0" ? (Int64)Math.Round(decimal.Parse(data[29], CultureInfo.InvariantCulture)) : (Int64?)null,
                TotalFY3 = data[30].Length > 0 && data[30] != "0" ? (Int64)Math.Round(decimal.Parse(data[30], CultureInfo.InvariantCulture)) : (Int64?)null,
                TotalFY4 = data[31].Length > 0 && data[31] != "0" ? (Int64)Math.Round(decimal.Parse(data[31], CultureInfo.InvariantCulture)) : (Int64?)null,
                Priority_Category = data[32],
                Created_At = DateTime.Now,
                Created_By = currUserID,
                Approval = levelApproval,
                Approval_Sub = levelApprovalSub,
                Version = currVersion,
                Budget_No = data[data.Length - 1],
                Is_Reject = false,
                Final_Version = 0
            });
            db.SaveChanges();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UploadBEL()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var data = Request["uploadData"].Split('|');
            var _period = Request["period"].Split('|');
            var _period_FY = _period[0];
            var level = Request["level"].ToString().Split('-');
            int levelApproval = int.Parse(level[0]);
            int levelApprovalSub = int.Parse(level[1]);
            var checkDept = db.Approval_Master.Where(w => w.Menu_Id == 59 && w.User_NIK == currUserID && w.Levels == levelApproval && w.Levels_Sub == levelApprovalSub).ToList();

            var _Section_From_Code = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Code : data[1]);
            var _Section_From_Name = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Name : data[2]);
            var currVersion = 0;

            // for get latest budget version
            var minus3Second = DateTime.Now.AddSeconds(-60);
            var checkData = db.FA_BudgetSystem_BEL.Where(w => w.Period_FY == _period_FY && w.Section_From_Code == _Section_From_Code && w.Created_At <= minus3Second && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).OrderByDescending(o => o.Version).FirstOrDefault();
            //if (checkData != null && checkData.Approval > 1)
            if (checkData != null)
            {
                currVersion = checkData.Version + 1;
            }

            db.FA_BudgetSystem_BEL.Add(new FA_BudgetSystem_BEL
            {
                Period_FY = _period[0],
                Period_Year = int.Parse(_period[1]),
                Budget_Type = data[0],
                Section_From_Code = _Section_From_Code,
                Section_From_Name = _Section_From_Name,
                Section_To_Code = data[3],
                Section_To_Name = data[4],
                COA_Code = data[5],
                COA_Name = data[6],
                Description = data[7],
                Group_Section = data[8],
                Group_Cost = data[9],
                month010 = data[10].Length > 0 && data[10] != "0" ? (Int64)Math.Round(decimal.Parse(data[10])) : (Int64?)null,
                month011 = data[11].Length > 0 && data[11] != "0" ? (Int64)Math.Round(decimal.Parse(data[11])) : (Int64?)null,
                month012 = data[12].Length > 0 && data[12] != "0" ? (Int64)Math.Round(decimal.Parse(data[12])) : (Int64?)null,
                month101 = data[13].Length > 0 && data[13] != "0" ? (Int64)Math.Round(decimal.Parse(data[13])) : (Int64?)null,
                month102 = data[14].Length > 0 && data[14] != "0" ? (Int64)Math.Round(decimal.Parse(data[14])) : (Int64?)null,
                month103 = data[15].Length > 0 && data[15] != "0" ? (Int64)Math.Round(decimal.Parse(data[15])) : (Int64?)null,
                month104 = data[16].Length > 0 && data[16] != "0" ? (Int64)Math.Round(decimal.Parse(data[16])) : (Int64?)null,
                month105 = data[17].Length > 0 && data[17] != "0" ? (Int64)Math.Round(decimal.Parse(data[17])) : (Int64?)null,
                month106 = data[18].Length > 0 && data[18] != "0" ? (Int64)Math.Round(decimal.Parse(data[18])) : (Int64?)null,
                month107 = data[19].Length > 0 && data[19] != "0" ? (Int64)Math.Round(decimal.Parse(data[19])) : (Int64?)null,
                month108 = data[20].Length > 0 && data[20] != "0" ? (Int64)Math.Round(decimal.Parse(data[20])) : (Int64?)null,
                month109 = data[21].Length > 0 && data[21] != "0" ? (Int64)Math.Round(decimal.Parse(data[21])) : (Int64?)null,
                month110 = data[22].Length > 0 && data[22] != "0" ? (Int64)Math.Round(decimal.Parse(data[22])) : (Int64?)null,
                month111 = data[23].Length > 0 && data[23] != "0" ? (Int64)Math.Round(decimal.Parse(data[23])) : (Int64?)null,
                month112 = data[24].Length > 0 && data[24] != "0" ? (Int64)Math.Round(decimal.Parse(data[24])) : (Int64?)null,
                month201 = data[25].Length > 0 && data[25] != "0" ? (Int64)Math.Round(decimal.Parse(data[25])) : (Int64?)null,
                month202 = data[26].Length > 0 && data[26] != "0" ? (Int64)Math.Round(decimal.Parse(data[26])) : (Int64?)null,
                month203 = data[27].Length > 0 && data[27] != "0" ? (Int64)Math.Round(decimal.Parse(data[27])) : (Int64?)null,
                TotalFY1 = data[28].Length > 0 && data[28] != "0" ? (Int64)Math.Round(decimal.Parse(data[28])) : (Int64?)null,
                TotalFY2 = data[29].Length > 0 && data[29] != "0" ? (Int64)Math.Round(decimal.Parse(data[29])) : (Int64?)null,
                TotalFY3 = data[30].Length > 0 && data[30] != "0" ? (Int64)Math.Round(decimal.Parse(data[30])) : (Int64?)null,
                TotalFY4 = data[31].Length > 0 && data[31] != "0" ? (Int64)Math.Round(decimal.Parse(data[31])) : (Int64?)null,
                Priority_Category = data[32],
                Created_At = DateTime.Now,
                Created_By = currUserID,
                Approval = levelApproval,
                Approval_Sub = levelApprovalSub,
                Version = currVersion,
                Budget_No = data[data.Length - 1],
                Is_Reject = false
            });
            db.SaveChanges();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UploadBIP()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var data = Request["uploadData"].Split('|');
            var _period = Request["period"].Split('|');
            var _period_FY = _period[0];
            var level = Request["level"].ToString().Split('-');
            int levelApproval = int.Parse(level[0]);
            int levelApprovalSub = int.Parse(level[1]);
            var checkDept = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.User_NIK == currUserID && w.Levels == levelApproval && w.Levels_Sub == levelApprovalSub).ToList();

            var _Section_From_Code = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Code : data[1]);
            var _Section_From_Name = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Name : data[2]);

            var currVersion = 0;

            // for get latest budget version
            var minus3Second = DateTime.Now.AddSeconds(-60);
            var checkData = db.FA_BudgetSystem_BIP.Where(w => w.Period_FY == _period_FY && w.Section_From_Code == _Section_From_Code && w.Created_At <= minus3Second && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).OrderByDescending(o => o.Version).FirstOrDefault();
            //if (checkData != null && checkData.Approval > 1)
            if (checkData != null)
            {
                currVersion = checkData.Version + 1;
            }

            db.FA_BudgetSystem_BIP.Add(new FA_BudgetSystem_BIP
            {
                Period_FY = _period_FY,
                Period_Year = int.Parse(_period[1]),
                Budget_Type = data[0],
                Section_From_Code = _Section_From_Code,
                Section_From_Name = _Section_From_Name,
                Section_To_Code = data[3],
                Section_To_Name = data[4],
                COA_Code = data[5],
                COA_Name = data[6],
                Asset_Name = data[7],
                Group_Section = data[8],
                Group_Cost = data[9],
                Acquisition_Value = (Int64)Math.Round(decimal.Parse(data[10])),
                Useful_Life = (int)Math.Round(decimal.Parse(data[11])),
                Allocation = decimal.Parse(data[12]),
                Depre_Start = DateTime.Parse(data[13]),
                Priority_Code = data[14],
                Budget_Period = data[15],
                Budget_Allocation = (Int64)Math.Round(decimal.Parse(data[16])),
                Created_At = DateTime.Now,
                Created_By = currUserID,
                Approval = 1,
                Approval_Sub = 0,
                Version = currVersion,
                Budget_No = data[data.Length - 1],
                Is_Reject = false
            });
            db.SaveChanges();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UploadCIP()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var data = Request["uploadData"].Split('|');
            var _period = Request["period"].Split('|');
            var _period_FY = _period[0];
            var level = Request["level"].ToString().Split('-');
            int levelApproval = int.Parse(level[0]);
            int levelApprovalSub = int.Parse(level[1]);
            var checkDept = db.Approval_Master.Where(w => w.Menu_Id == 57 && w.User_NIK == currUserID && w.Levels == levelApproval && w.Levels_Sub == levelApprovalSub).ToList();

            var _Section_From_Code = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Code : data[1]);
            var _Section_From_Name = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Name : data[2]);

            var currVersion = 0;

            // for get latest budget version
            var minus3Second = DateTime.Now.AddSeconds(-60);
            var checkData = db.FA_BudgetSystem_CIP.Where(w => w.Period_FY == _period_FY && w.Section_From_Code == _Section_From_Code && w.Created_At <= minus3Second && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).OrderByDescending(o => o.Version).FirstOrDefault();
            //if (checkData != null && checkData.Approval > 1)
            if (checkData != null)
            {
                currVersion = checkData.Version + 1;
            }

            db.FA_BudgetSystem_CIP.Add(new FA_BudgetSystem_CIP
            {
                Period_FY = _period_FY,
                Period_Year = int.Parse(_period[1]),
                Budget_Type = data[0],
                Section_From_Code = _Section_From_Code,
                Section_From_Name = _Section_From_Name,
                Section_To_Code = data[3],
                Section_To_Name = data[4],
                COA_Code = data[5],
                COA_Name = data[6],
                Asset_Name = data[7],
                Group_Section = data[8],
                Group_Cost = data[9],
                Acquisition_Value = (Int64)Math.Round(decimal.Parse(data[10])),
                Useful_Life = (int)Math.Round(decimal.Parse(data[11])),
                Allocation = decimal.Parse(data[12]),
                Depre_Start = DateTime.Parse(data[13]),
                Priority_Code = data[14],
                Budget_Period = data[15],
                Budget_Allocation = (Int64)Math.Round(decimal.Parse(data[16])),
                Created_At = DateTime.Now,
                Created_By = currUserID,
                Approval = levelApproval,
                Approval_Sub = levelApprovalSub,
                Version = currVersion,
                Budget_No = data[data.Length - 1],
                Is_Reject = false
            });
            db.SaveChanges();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UploadCFA()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var data = Request["uploadData"].Split('|');
            var _period = Request["period"].Split('|');
            var _period_FY = _period[0];
            var level = Request["level"].ToString().Split('-');
            int levelApproval = int.Parse(level[0]);
            int levelApprovalSub = int.Parse(level[1]);
            var checkDept = db.Approval_Master.Where(w => w.Menu_Id == 62 && w.User_NIK == currUserID && w.Levels == levelApproval && w.Levels_Sub == levelApprovalSub).ToList();

            var _Section_From_Code = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Code : data[1]);
            var _Section_From_Name = (checkDept.Count() == 1 && (levelApproval == 1 && levelApprovalSub == 0) ? checkDept.First().Dept_Name : data[2]);

            var currVersion = 0;

            // for get latest budget version
            var minus3Second = DateTime.Now.AddSeconds(-60);
            var checkData = db.FA_BudgetSystem_CFA.Where(w => w.Period_FY == _period_FY && w.Section_From_Code == _Section_From_Code && w.Created_At <= minus3Second && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).OrderByDescending(o => o.Version).FirstOrDefault();
            //if (checkData != null && checkData.Approval > 1)
            if (checkData != null)
            {
                currVersion = checkData.Version + 1;
            }

            db.FA_BudgetSystem_CFA.Add(new FA_BudgetSystem_CFA
            {
                Period_FY = _period_FY,
                Period_Year = int.Parse(_period[1]),
                Budget_Type = data[0],
                Section_From_Code = _Section_From_Code,
                Section_From_Name = _Section_From_Name,
                Section_To_Code = data[3],
                Section_To_Name = data[4],
                COA_Code = data[5],
                COA_Name = data[6],
                Asset_Name = data[7],
                Group_Section = data[8],
                Group_Cost = data[9],
                Acquisition_Value = (Int64)Math.Round(decimal.Parse(data[10])),
                Useful_Life = (int)Math.Round(decimal.Parse(data[11])),
                Allocation = decimal.Parse(data[12]),
                Depre_Start = DateTime.Parse(data[13]),
                Priority_Code = data[14],
                Budget_Period = data[15],
                Budget_Allocation = (Int64)Math.Round(decimal.Parse(data[16])),
                Created_At = DateTime.Now,
                Created_By = currUserID,
                Approval = levelApproval,
                Approval_Sub = levelApprovalSub,
                Version = currVersion,
                Budget_No = data[data.Length - 1],
                Is_Reject = false
            });
            db.SaveChanges();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult _GetData(string _sheetName, int _arrayCount, string _budgetType)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var _period = Request["period"].Split('|');
            //This version checked by creating sheet on the template file
            string _latestVersionChecked = "V1$";
            var level = Request["level"].ToString().Split('-');
            var isFinal = Request["isFinal"];
            int levelApproval = int.Parse(level[0]);
            int levelApprovalSub = int.Parse(level[1]);
            var _menu_id = _budgetType == "CFA" ? 62 : _budgetType == "BEL" ? 59 : 57;
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == _menu_id && w.Document_Id == 1 && w.User_NIK == currUserID && w.Levels == levelApproval && w.Levels_Sub == levelApprovalSub).ToList();
            if (isFinal == "true")
            {
                getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == _menu_id && w.Document_Id == 1).ToList();
            }

            if (Request.Files.Count > 0)
            {
                try
                {
                    HttpFileCollectionBase files = Request.Files;
                    for (int z = 0; z < files.Count; z++)
                    {
                        HttpPostedFileBase file = files[z];
                        string fname;

                        if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split(new char[] { '\\' });
                            fname = testfiles[testfiles.Length - 1];
                        }
                        else
                        {
                            fname = file.FileName;
                        }

                        // Get the complete folder path and store the file inside it.  
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/BudgetSystem"), fname);
                        file.SaveAs(fname);
                        XLWorkbook xLWorkbook = new XLWorkbook(fname);

                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties='Excel 12.0;IMEX=0;';";

                        using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                        {
                            conn.Open();
                            using (System.Data.DataTable dtExcelSchema = conn.GetSchema("Tables"))
                            {
                                List<string> listSheet = new List<string>();
                                foreach (DataRow drSheet in dtExcelSchema.Rows)
                                {
                                    if (drSheet["TABLE_NAME"].ToString().Contains("$"))//checks whether row contains '_xlnm#_FilterDatabase' or sheet name(i.e. sheet name always ends with $ sign)
                                    {
                                        listSheet.Add(drSheet["TABLE_NAME"].ToString());
                                    }
                                }
                                //string sheetNameCheck = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                if (!listSheet.Contains(_latestVersionChecked) && _budgetType != "BEL")
                                {
                                    return Json("Error Version", JsonRequestBehavior.AllowGet);
                                }

                                string sheetName = _sheetName;
                                string query = "SELECT * FROM [" + sheetName + "] where [BUDGET TYPE] = '" + _budgetType + "' and [DESCRIPTION] <> '' order by [Section Code (From)],[COA-ID],[DESCRIPTION]";
                                if (_budgetType == "BIP" || _budgetType == "CIP" || _budgetType == "CFA")
                                {
                                    query = "SELECT * FROM [" + sheetName + "] where [BUDGET TYPE] = '" + _budgetType + "' and [Asset Name] <> '' order by [Section From (Code)],[Coa ID],[Asset Name]";
                                }
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        var newData = new List<ArrayList>();
                                        var numberCheck = "";
                                        var section = "";
                                        var BudgetNumber = "";
                                        var BudgetCount = 0;
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            ArrayList dataArray = new ArrayList();
                                            var _Period_FY = _period[0];
                                            var _Section_From_Code = ds.Tables[0].Rows[i][1].ToString();
                                            var checkDept = getApprovalList.Where(w => w.Dept_Code == _Section_From_Code).ToList();
                                            if (checkDept.Count() > 0 || isFinal == "true")
                                            {
                                                for (int j = 0; j <= _arrayCount; j++)
                                                {
                                                    dataArray.Add(!String.IsNullOrEmpty(ds.Tables[0].Rows[i][j].ToString()) ? ds.Tables[0].Rows[i][j].ToString() : "");
                                                }

                                                if (section != ds.Tables[0].Rows[i][1].ToString())
                                                {
                                                    section = ds.Tables[0].Rows[i][1].ToString();
                                                    BudgetCount = 0;
                                                }
                                                if (numberCheck != ds.Tables[0].Rows[i][1].ToString() + ds.Tables[0].Rows[i][5].ToString() + ds.Tables[0].Rows[i][7].ToString())
                                                {
                                                    numberCheck = ds.Tables[0].Rows[i][1].ToString() + ds.Tables[0].Rows[i][5].ToString() + ds.Tables[0].Rows[i][7].ToString();
                                                    BudgetCount++;
                                                }
                                                BudgetNumber = _budgetType + _period[0].Substring(_period[0].Length - 2, 2) + "-" + ds.Tables[0].Rows[i][1].ToString().Replace("B", "") + "-" + ds.Tables[0].Rows[i][3].ToString().Replace("B", "") + "-" + ("000" + BudgetCount).Substring(("000" + BudgetCount).Length - 3, 3);
                                                dataArray.Add(BudgetNumber);
                                                newData.Add(dataArray);
                                                if (_budgetType == "BEX")
                                                {
                                                    var deleteBEX = db.FA_BudgetSystem_BEX.Where(w => w.Period_FY == _Period_FY && w.Section_From_Code == _Section_From_Code && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).ToList();
                                                    deleteBEX = deleteBEX.Where(w => w.Version == deleteBEX.Max(m => m.Version)).ToList();
                                                    if (deleteBEX.Count() > 0 && (levelApproval == 1 && levelApprovalSub == 0))
                                                    {
                                                        deleteBEX = deleteBEX.Where(w => w.Created_By == currUserID).ToList();
                                                    }
                                                    if (deleteBEX.Count() > 0)
                                                    {
                                                        db.FA_BudgetSystem_BEX.RemoveRange(deleteBEX);
                                                        db.SaveChanges();
                                                    }
                                                }
                                                else if (_budgetType == "BEL")
                                                {
                                                    var deleteBEL = db.FA_BudgetSystem_BEL.Where(w => w.Period_FY == _Period_FY && w.Section_From_Code == _Section_From_Code && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).ToList();
                                                    deleteBEL = deleteBEL.Where(w => w.Version == deleteBEL.Max(m => m.Version)).ToList();
                                                    if (deleteBEL.Count() > 0 && (levelApproval == 1 && levelApprovalSub == 0))
                                                    {
                                                        deleteBEL = deleteBEL.Where(w => w.Created_By == currUserID).ToList();
                                                        db.FA_BudgetSystem_BEL.RemoveRange(deleteBEL);
                                                        db.SaveChanges();
                                                    }
                                                }
                                                else if (_budgetType == "BIP")
                                                {
                                                    var deleteBIP = db.FA_BudgetSystem_BIP.Where(w => w.Period_FY == _Period_FY && w.Section_From_Code == _Section_From_Code && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).ToList();
                                                    if (deleteBIP.Count() > 0 && (levelApproval == 1 && levelApprovalSub == 0))
                                                    {
                                                        deleteBIP = deleteBIP.Where(w => w.Created_By == currUserID).ToList();
                                                    }

                                                    if (deleteBIP.Count() > 0)
                                                    {
                                                        db.FA_BudgetSystem_BIP.RemoveRange(deleteBIP);
                                                        db.SaveChanges();
                                                    }
                                                }
                                                else if (_budgetType == "CIP")
                                                {
                                                    var deleteCIP = db.FA_BudgetSystem_CIP.Where(w => w.Period_FY == _Period_FY && w.Section_From_Code == _Section_From_Code && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).ToList();
                                                    if (deleteCIP.Count() > 0 && (levelApproval == 1 && levelApprovalSub == 0))
                                                    {
                                                        deleteCIP = deleteCIP.Where(w => w.Created_By == currUserID).ToList();
                                                    }

                                                    if (deleteCIP.Count() > 0)
                                                    {
                                                        db.FA_BudgetSystem_CIP.RemoveRange(deleteCIP);
                                                        db.SaveChanges();
                                                    }
                                                }
                                                else if (_budgetType == "CFA")
                                                {
                                                    var deleteCFA = db.FA_BudgetSystem_CFA.Where(w => w.Period_FY == _Period_FY && w.Section_From_Code == _Section_From_Code && w.Approval <= 1 && w.Approval_Sub == 0 && w.Is_Reject == false).ToList();
                                                    if (deleteCFA.Count() > 0 && (levelApproval == 1 && levelApprovalSub == 0))
                                                    {
                                                        deleteCFA = deleteCFA.Where(w => w.Created_By == currUserID).ToList();
                                                    }

                                                    if (deleteCFA.Count() > 0)
                                                    {
                                                        db.FA_BudgetSystem_CFA.RemoveRange(deleteCFA);
                                                        db.SaveChanges();
                                                    }
                                                }
                                            }
                                        }
                                        return Json(newData, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                        if (System.IO.File.Exists(fname))
                        {
                            System.IO.File.Delete(fname);
                        }
                    }

                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }

        public ActionResult BEXBELDownloadData()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var _menu_id = Request["iDownloadType"] == "BEX" ? 57 : 59;
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == _menu_id && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();
            var getApprovalLevel = getApprovalList.Select(s => s.Levels + "-" + s.Levels_Sub).Distinct();
            ViewBag.Level = string.IsNullOrEmpty(Request["iDownloadLevel"]) ? getApprovalLevel.FirstOrDefault() : Request["iDownloadLevel"];

            var currLevel = ViewBag.Level.ToString().Split('-');
            int currApproval = int.Parse(currLevel[0]);
            int currApprovalSub = int.Parse(currLevel[1]);
            var getSectionList = (string.IsNullOrEmpty(Request["iDownloadSection"]) ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : (Request["iDownloadSection"] == "All" ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).Where(w => w.Dept_Code == Request["iDownloadSection"]).Select(s => s.Dept_Code).Distinct()));
            var _budgetType = Request["iDownloadType"];
            var _dataChecked = Request["iDownloadDataChecked[]"].Split(',');
            var _period = Request["iDownloadPeriod"].Split('|');
            var _Period_FY = _period[0];
            var _Period_Year = int.Parse(_period[1]);

            var fileName = Request["iDownloadType"] == "BEX" ? "Budget_Expense_Data" : "Budget_Labor_Data";

            var path = Server.MapPath("~/Files/FA/BudgetSystem/Download/" + fileName + ".xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/BudgetSystem/Master/" + fileName + ".xlsx"), path, true);


            var monthQuery = "";
            var headerArray = new string[] { }.ToList();
            for (var i = 0; i <= 17; i++)
            {
                var mth = new DateTime(_Period_Year, 10, 1).AddMonths(i).ToString("yyyy_MM");
                monthQuery += ",`" + mth + "`";
                headerArray.Add(mth);
            }
            var FYQuery = "";
            for (var i = 1; i <= 4; i++)
            {
                var FY = new DateTime(_Period_Year, 10, 1).AddYears(i).ToString("yy");
                FYQuery += ",`TOTAL FY1" + FY + "`";
                headerArray.Add("TOTAL FY1" + FY);
            }
            var headerArrayList = new List<String[]>();
            headerArrayList.Add(headerArray.ToArray());

            var _Data = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Budget_Type == _budgetType && w.Period_FY == _Period_FY && (w.Approval > currApproval || (w.Approval == currApproval && w.Approval_Sub >= currApprovalSub)) && w.Is_Reject == false).ToList();
            if (_dataChecked.Contains("Section-From") && _dataChecked.Contains("Section-To"))
            {
                _Data = _Data.Where(w => getSectionList.Contains(w.Section_From_Code) || getSectionList.Contains(w.Section_To_Code)).ToList();
            }
            else
            {
                if (_dataChecked.Contains("Section-To"))
                {
                    _Data = _Data.Where(w => !getSectionList.Contains(w.Section_From_Code) && getSectionList.Contains(w.Section_To_Code)).ToList();
                }
                else
                {
                    _Data = _Data.Where(w => getSectionList.Contains(w.Section_From_Code)).ToList();
                }
            }

            var LatestVersion = string.IsNullOrEmpty(Request["iDownloadVersion"]) && Request["iDownloadSection"] != "All" ? (_Data.Count() > 0 ? _Data.OrderByDescending(o => o.Version).First().Version : 0) : int.Parse(Request["iDownloadVersion"]);

            if (Request["iDownloadSection"] == "All")
            {
                _Data = _Data.Where(w => w.Latest == 1).ToList();
            }
            else
            {
                _Data = _Data.Where(w => w.Version == LatestVersion).ToList();
            }
            var _DataList = _Data.Select(s => new
            {
                s.Budget_Type,
                s.Section_From_Code,
                s.Section_From_Name,
                s.Section_To_Code,
                s.Section_To_Name,
                s.COA_Code,
                s.COA_Name,
                s.Description,
                s.Group_Section,
                s.Group_Cost,
                s.month010,
                s.month011,
                s.month012,
                s.month101,
                s.month102,
                s.month103,
                s.month104,
                s.month105,
                s.month106,
                s.month107,
                s.month108,
                s.month109,
                s.month110,
                s.month111,
                s.month112,
                s.month201,
                s.month202,
                s.month203,
                s.TotalFY1,
                s.TotalFY2,
                s.TotalFY3,
                s.TotalFY4,
                s.Priority_Category,
                s.Budget_No
            }).ToList();


            var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(1);
            var ts = ws.Cell(1, 11).InsertData(headerArrayList);
            var ts2 = ws.Cell(2, 1).InsertData(_DataList);
            workbook.SaveAs(path);

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName + ".xlsx");
            Response.TransmitFile(path);
            Response.End();

            var _redirect = Request["iDownloadType"] == "BEX" ? "Expense" : "Labor";
            return RedirectToAction(_redirect, "BudgetSystem", new { area = "FA" });
        }
        public ActionResult BEXBELDownloadTemplate()
        {
            ////var _period = Request["iDownloadPeriod"].Split('|');
            ////var _Period_Year = int.Parse(_period[1]);

            //var _Period_Year = (DateTime.Now.Month < 4? DateTime.Now.AddMonths(-4).Year : DateTime.Now.Year);
            //var monthQuery = "";
            //var headerArray = new string[] { }.ToList();
            //for (var i = 0; i <= 17; i++)
            //{
            //    var mth = new DateTime(_Period_Year, 10, 1).AddMonths(i).ToString("yyyy_MM");
            //    monthQuery += ",`" + mth + "`";
            //    headerArray.Add(mth);
            //}
            //var FYQuery = "";
            //for (var i = 1; i <= 4; i++)
            //{
            //    var FY = new DateTime(_Period_Year, 10, 1).AddYears(i).ToString("yy");
            //    FYQuery += ",`TOTAL FY1" + FY + "`";
            //    headerArray.Add("TOTAL FY1" + FY);
            //}
            //var headerArrayList = new List<String[]>();
            //headerArrayList.Add(headerArray.ToArray());

            var path = Server.MapPath("~/Files/FA/BudgetSystem/Master/Budget Upload - Expense (BEX,BEL).xlsx");

            ////insert dynamic year_month Header
            //var workbook = new XLWorkbook(path);
            //var ws = workbook.Worksheet(1);
            //var ts = ws.Cell(1, 11).InsertData(headerArrayList);
            //workbook.SaveAs(path);

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=\"Budget Upload - Expense (BEX,BEL).xlsx\";");
            Response.TransmitFile(path);
            Response.End();

            var _redirect = Request["iDownloadType"] == "BEX" ? "Expense" : "Labor";
            return RedirectToAction(_redirect, "BudgetSystem", new { area = "FA" });
        }
        public ActionResult BIPCIPCFADownloadData()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var _menu_id = Request["iDownloadType"] == "CFA" ? 62 : 57;
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == _menu_id && w.Document_Id == 1 && w.User_NIK == currUserID).ToList();
            var getApprovalLevel = getApprovalList.Select(s => s.Levels + "-" + s.Levels_Sub).Distinct();
            ViewBag.Level = string.IsNullOrEmpty(Request["iDownloadLevel"]) ? getApprovalLevel.FirstOrDefault() : Request["iDownloadLevel"];

            var currLevel = ViewBag.Level.ToString().Split('-');
            int currApproval = int.Parse(currLevel[0]);
            int currApprovalSub = int.Parse(currLevel[1]);
            var getSectionList = (string.IsNullOrEmpty(Request["iDownloadSection"]) ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : (Request["iDownloadSection"] == "All" ? getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).OrderBy(o => o.Dept_Name).Select(s => s.Dept_Code).Distinct() : getApprovalList.Where(w => w.Levels == currApproval && w.Levels_Sub == currApprovalSub).Where(w => w.Dept_Code == Request["iDownloadSection"]).Select(s => s.Dept_Code).Distinct()));
            var _budgetType = Request["iDownloadType"];
            var _dataChecked = Request["iDownloadDataChecked[]"].Split(',');
            var _period = Request["iDownloadPeriod"].Split('|');
            var _Period_FY = _period[0];
            var _Period_Year = int.Parse(_period[1]);

            var fileName = Request["iDownloadType"] == "CFA" ? "Budget_Current_Asset_Data" : Request["iDownloadType"] == "BIP" ? "Budget_Investment_Data" : "Budget_Asset_In_Progress_Data";

            var path = Server.MapPath("~/Files/FA/BudgetSystem/Download/" + fileName + ".xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/BudgetSystem/Master/" + fileName + ".xlsx"), path, true);

            var _Data = db.V_FA_BudgetSystem_BIP_CIP_CFA.Where(w => w.Budget_Type == _budgetType && w.Period_FY == _Period_FY && (w.Approval > currApproval || (w.Approval == currApproval && w.Approval_Sub >= currApprovalSub)) && w.Is_Reject == false).ToList();
            if (_dataChecked.Contains("Section-From") && _dataChecked.Contains("Section-To"))
            {
                _Data = _Data.Where(w => getSectionList.Contains(w.Section_From_Code) || getSectionList.Contains(w.Section_To_Code)).ToList();
            }
            else
            {
                if (_dataChecked.Contains("Section-To"))
                {
                    _Data = _Data.Where(w => !getSectionList.Contains(w.Section_From_Code) && getSectionList.Contains(w.Section_To_Code)).ToList();
                }
                else
                {
                    _Data = _Data.Where(w => getSectionList.Contains(w.Section_From_Code)).ToList();
                }
            }
            var LatestVersion = string.IsNullOrEmpty(Request["iDownloadVersion"]) ? (_Data.Count() > 0 ? _Data.OrderByDescending(o => o.Version).First().Version : 0) : int.Parse(Request["iDownloadVersion"]);

            if (Request["iDownloadSection"] == "All")
            {
                _Data = _Data.Where(w => w.Latest == 1).ToList();
            }
            else
            {
                _Data = _Data.Where(w => w.Version == LatestVersion).ToList();
            }
            var _DataList = _Data.Select(s => new { s.Budget_Type, s.Section_From_Code, s.Section_From_Name, s.Section_To_Code, s.Section_To_Name, s.COA_Code, s.COA_Name, s.Asset_Name, s.Group_Section, s.Group_Cost, s.Acquisition_Value, s.Useful_Life, s.Allocation, s.Depre_Start, s.Priority_Code, s.Budget_Period, s.Budget_Allocation, s.Budget_No }).ToList();

            var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(1);
            var ts2 = ws.Cell(2, 1).InsertData(_DataList);
            workbook.SaveAs(path);

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName + ".xlsx");
            Response.TransmitFile(path);
            Response.End();

            var _redirect = Request["iDownloadType"] == "CFA" ? "CurrentAsset" : Request["iDownloadType"] == "BIP" ? "Investment" : "AssetInProgress";
            return RedirectToAction(_redirect, "BudgetSystem", new { area = "FA" });
        }
        public ActionResult BIPCIPCFADownloadTemplate()
        {
            var path = Server.MapPath("~/Files/FA/BudgetSystem/Master/Budget Upload - Invesment (BIP,CIP,CFA).xlsx");


            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=\"Budget Upload - Invesment (BIP,CIP,CFA).xlsx\";");
            Response.TransmitFile(path);
            Response.End();

            var _redirect = Request["iDownloadType"] == "CFA" ? "CurrentAsset" : Request["iDownloadType"] == "BIP" ? "Investment" : "AssetInProgress";
            return RedirectToAction(_redirect, "BudgetSystem", new { area = "FA" });
        }

        public ActionResult ListAssetPlanDownloadData()
        {
            var _period = Request["iDownloadPeriod"].Split('|');
            var _Period_FY = _period[0];
            var _Period_Year = int.Parse(_period[1]);

            var path = Server.MapPath("~/Files/FA/BudgetSystem/Download/List_Asset_Plan.xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/BudgetSystem/Master/List_Asset_Plan.xlsx"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + path + "';Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {
                    var BIPCIPCFAData = db.V_FA_BudgetSystem_BIP_CIP_CFA.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false).ToList();

                    foreach (var data in BIPCIPCFAData)
                    {
                        if (data.Budget_Type != "")
                        {
                            queryText = string.Format("Insert into [Data$] " +
                                   "(`Budget Type`,`Section From (Code)`,`Section From (Name)`,`To Section (Code)`,`To Section (Name)`,`Coa ID`,`Coa Name`,`Asset Name`,`Group Section`,`Group Cost`,`Acquisition Value`,`Useful Life`,`Allocation %`,`Depre Start`,`Priority Code`,`Budget_Period`,`Budget Allocation`) " +
                                   "values('" + data.Budget_Type + "','" + data.Section_From_Code + "','" + data.Section_From_Name + "','" + data.Section_To_Code + "','" + data.Section_To_Name + "','" + data.COA_Code + "','" + data.COA_Name + "','" + data.Asset_Name.Replace("'", "-") + "','" + data.Group_Section + "','" + data.Group_Cost + "','" + data.Acquisition_Value.ToString() + "','" + data.Useful_Life.ToString() + "','" + data.Allocation.ToString() + "','" + data.Depre_Start.ToString("dd/MM/yyyy") + "','" + data.Priority_Code + "','" + data.Budget_Period + "','" + data.Budget_Allocation.ToString() + "');");
                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=List_Asset_Plan.xlsx");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("SummarizeBudget", "BudgetSystem", new { area = "FA" });
        }


        public ActionResult ListExpensesPlanDownloadData()
        {
            var _period = Request["iDownloadPeriod"].Split('|');
            var _Period_FY = _period[0];
            var _Period_Year = int.Parse(_period[1]);

            var path = Server.MapPath("~/Files/FA/BudgetSystem/Download/List_Expenses_Plan.xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/BudgetSystem/Master/List_Expenses_Plan.xlsx"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + path + "';Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {
                    var monthQuery = "";
                    for (var i = 0; i <= 17; i++)
                    {
                        var mth = new DateTime(_Period_Year, 10, 1).AddMonths(i).ToString("yyyy_MM");
                        monthQuery += ",`" + mth + "`";
                    }
                    var FYQuery = "";
                    for (var i = 1; i <= 4; i++)
                    {
                        var FY = new DateTime(_Period_Year, 10, 1).AddYears(i).ToString("yy");
                        FYQuery += ",`TOTAL FY1" + FY + "`";
                    }


                    var BEXBELData = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false).ToList();

                    foreach (var data in BEXBELData)
                    {
                        if (data.Budget_Type != "")
                        {
                            queryText = string.Format("Insert into [Data$] " +
                                   "(`Budget Type`,`Section Code (From)`,`Section Name (From/Who Prepare)`,`Section Code (To)`,`Section Name (To/Who Use The Cost)`,`COA-ID`,`COA NAME`,`DESCRIPTION`,`GROUP SECTION`,`GROUP COST`" + monthQuery + FYQuery + ",`Priority Category`,`BUDGET NO`) " +
                                   "values('" + data.Budget_Type + "','" + data.Section_From_Code + "','" + data.Section_From_Name + "','" + data.Section_To_Code + "','" + data.Section_To_Name + "','" + data.COA_Code + "','" + data.COA_Name + "','" + data.Description.Replace("'", "-") + "','" + data.Group_Section + "','" + data.Group_Cost + "'" +
                                   ",'" + data.month010 + "','" + data.month011 + "','" + data.month012 + "','" + data.month101 + "','" + data.month102 + "','" + data.month103 + "','" + data.month104 + "','" + data.month105 + "','" + data.month106 + "','" + data.month107 + "','" + data.month108 + "','" + data.month109 + "','" + data.month110 + "','" + data.month111 + "','" + data.month112 + "','" + data.month201 + "','" + data.month202 + "','" + data.month203 + "'" +
                                   ",'" + data.TotalFY1 + "','" + data.TotalFY2 + "','" + data.TotalFY3 + "','" + data.TotalFY4 + "','" + data.Priority_Category + "','" + data.Budget_No + "');");
                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }
                    }


                    var BIPCIPCFADepreData = db.V_FA_BudgetSystem_BIP_CIP_CFA.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false).ToList().Select(s => new
                    {
                        Budget_No = s.Budget_No,
                        Period_Year = s.Period_Year,
                        Budget_Type = s.Budget_Type,
                        Section_From_Code = s.Section_From_Code,
                        Section_From_Name = s.Section_From_Name,
                        Section_To_Code = s.Section_To_Code,
                        Section_To_Name = s.Section_To_Name,
                        COA_Code = db.FA_BudgetSystem_COA_Conversion.Where(w => w.COA_Code == s.COA_Code && w.COA_Name == s.COA_Name).First().COA_Code_Conversion,
                        COA_Name = db.FA_BudgetSystem_COA_Conversion.Where(w => w.COA_Code == s.COA_Code && w.COA_Name == s.COA_Name).First().COA_Name_Conversion,
                        Description = s.Asset_Name,
                        Group_Section = s.Group_Section,
                        Group_Cost = s.Group_Cost,
                        Acquisition_Value = s.Acquisition_Value,
                        Useful_Life = s.Useful_Life,
                        Allocation = s.Allocation,
                        Depre_Start = s.Depre_Start,
                        Priority_Code = s.Priority_Code,
                        Asset_Allocation = s.Budget_Allocation,
                        Monthly_Depre = s.Budget_Allocation / s.Useful_Life,
                        Depre_End = s.Depre_Start.AddMonths(s.Useful_Life - 1),
                        month010 = (new DateTime(s.Period_Year, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month011 = (new DateTime(s.Period_Year, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month012 = (new DateTime(s.Period_Year, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month101 = (new DateTime(s.Period_Year + 1, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month102 = (new DateTime(s.Period_Year + 1, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month103 = (new DateTime(s.Period_Year + 1, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month104 = (new DateTime(s.Period_Year + 1, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month105 = (new DateTime(s.Period_Year + 1, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month106 = (new DateTime(s.Period_Year + 1, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month107 = (new DateTime(s.Period_Year + 1, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month108 = (new DateTime(s.Period_Year + 1, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month109 = (new DateTime(s.Period_Year + 1, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month110 = (new DateTime(s.Period_Year + 1, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month111 = (new DateTime(s.Period_Year + 1, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month112 = (new DateTime(s.Period_Year + 1, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month201 = (new DateTime(s.Period_Year + 2, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month202 = (new DateTime(s.Period_Year + 2, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month203 = (new DateTime(s.Period_Year + 2, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month204 = (new DateTime(s.Period_Year + 2, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month205 = (new DateTime(s.Period_Year + 2, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month206 = (new DateTime(s.Period_Year + 2, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month207 = (new DateTime(s.Period_Year + 2, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month208 = (new DateTime(s.Period_Year + 2, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month209 = (new DateTime(s.Period_Year + 2, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month210 = (new DateTime(s.Period_Year + 2, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month211 = (new DateTime(s.Period_Year + 2, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month212 = (new DateTime(s.Period_Year + 2, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month301 = (new DateTime(s.Period_Year + 3, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month302 = (new DateTime(s.Period_Year + 3, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month303 = (new DateTime(s.Period_Year + 3, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month304 = (new DateTime(s.Period_Year + 3, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month305 = (new DateTime(s.Period_Year + 3, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month306 = (new DateTime(s.Period_Year + 3, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month307 = (new DateTime(s.Period_Year + 3, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month308 = (new DateTime(s.Period_Year + 3, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month309 = (new DateTime(s.Period_Year + 3, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month310 = (new DateTime(s.Period_Year + 3, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month311 = (new DateTime(s.Period_Year + 3, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month312 = (new DateTime(s.Period_Year + 3, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month401 = (new DateTime(s.Period_Year + 4, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month402 = (new DateTime(s.Period_Year + 4, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month403 = (new DateTime(s.Period_Year + 4, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                    });

                    var BIPCIPCFAExpenseData = BIPCIPCFADepreData.Select(s => new
                    {
                        Budget_Type = s.Budget_Type,
                        Section_From_Code = s.Section_From_Code,
                        Section_From_Name = s.Section_From_Name,
                        Section_To_Code = s.Section_To_Code,
                        Section_To_Name = s.Section_To_Name,
                        COA_Code = s.COA_Code,
                        COA_Name = s.COA_Name,
                        Description = s.Description,
                        Group_Section = s.Group_Section,
                        Group_Cost = s.Group_Cost,
                        month010 = s.month010 == 0 ? (long?)null : s.month010,
                        month011 = s.month011 == 0 ? (long?)null : s.month011,
                        month012 = s.month012 == 0 ? (long?)null : s.month012,
                        month101 = s.month101 == 0 ? (long?)null : s.month101,
                        month102 = s.month102 == 0 ? (long?)null : s.month102,
                        month103 = s.month103 == 0 ? (long?)null : s.month103,
                        month104 = s.month104 == 0 ? (long?)null : s.month104,
                        month105 = s.month105 == 0 ? (long?)null : s.month105,
                        month106 = s.month106 == 0 ? (long?)null : s.month106,
                        month107 = s.month107 == 0 ? (long?)null : s.month107,
                        month108 = s.month108 == 0 ? (long?)null : s.month108,
                        month109 = s.month109 == 0 ? (long?)null : s.month109,
                        month110 = s.month110 == 0 ? (long?)null : s.month110,
                        month111 = s.month111 == 0 ? (long?)null : s.month111,
                        month112 = s.month112 == 0 ? (long?)null : s.month112,
                        month201 = s.month201 == 0 ? (long?)null : s.month201,
                        month202 = s.month202 == 0 ? (long?)null : s.month202,
                        month203 = s.month203 == 0 ? (long?)null : s.month203,
                        TotalFY1 = s.month010 + s.month011 + s.month012 + s.month101 + s.month102 + s.month103,
                        TotalFY2 = s.month104 + s.month105 + s.month106 + s.month107 + s.month108 + s.month109 + s.month110 + s.month111 + s.month112 + s.month201 + s.month202 + s.month203,
                        TotalFY3 = s.month204 + s.month205 + s.month206 + s.month207 + s.month208 + s.month209 + s.month210 + s.month211 + s.month212 + s.month301 + s.month302 + s.month303,
                        TotalFY4 = s.month304 + s.month305 + s.month306 + s.month307 + s.month308 + s.month309 + s.month310 + s.month311 + s.month312 + s.month401 + s.month402 + s.month403,
                        Priority_Category = s.Priority_Code,
                        Budget_No = s.Budget_No
                    }).ToList();

                    foreach (var data in BIPCIPCFAExpenseData)
                    {
                        if (data.Budget_Type != "")
                        {
                            queryText = string.Format("Insert into [Data$] " +
                                   "(`Budget Type`,`Section Code (From)`,`Section Name (From/Who Prepare)`,`Section Code (To)`,`Section Name (To/Who Use The Cost)`,`COA-ID`,`COA NAME`,`DESCRIPTION`,`GROUP SECTION`,`GROUP COST`" + monthQuery + FYQuery + ",`Priority Category`,`BUDGET NO`) " +
                                   "values('" + data.Budget_Type + "','" + data.Section_From_Code + "','" + data.Section_From_Name + "','" + data.Section_To_Code + "','" + data.Section_To_Name + "','" + data.COA_Code + "','" + data.COA_Name + "','" + data.Description.Replace("'", "-") + "','" + data.Group_Section + "','" + data.Group_Cost + "'" +
                                   ",'" + data.month010 + "','" + data.month011 + "','" + data.month012 + "','" + data.month101 + "','" + data.month102 + "','" + data.month103 + "','" + data.month104 + "','" + data.month105 + "','" + data.month106 + "','" + data.month107 + "','" + data.month108 + "','" + data.month109 + "','" + data.month110 + "','" + data.month111 + "','" + data.month112 + "','" + data.month201 + "','" + data.month202 + "','" + data.month203 + "'" +
                                   ",'" + ((long?)data.TotalFY1 == 0 ? (long?)null : data.TotalFY1) + "','" + ((long?)data.TotalFY2 == 0 ? (long?)null : data.TotalFY2) + "','" + ((long?)data.TotalFY3 == 0 ? (long?)null : data.TotalFY3) + "','" + ((long?)data.TotalFY4 == 0 ? (long?)null : data.TotalFY4) + "','" + data.Priority_Category + "','" + data.Budget_No + "');");
                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=List_Expenses_Plan.xlsx");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("SummarizeBudget", "BudgetSystem", new { area = "FA" });
        }

        public ActionResult ListGLDownloadData()
        {
            var _period = Request["iDownloadPeriod"].Split('|');
            var _Period_FY = _period[0];
            var _Period_Year = int.Parse(_period[1]);

            var path = Server.MapPath("~/Files/FA/BudgetSystem/Download/List_GL.xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/BudgetSystem/Master/List_GL.xlsx"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + path + "';Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {
                    int[] arrayMonth = { 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 93, 94 };
                    string[] arrayColumn = { "month010", "month011", "month012", "month101", "month102", "month103", "month104", "month105", "month106", "month107", "month108", "month109", "month110", "month111", "month112", "month201", "month202", "month203", "TotalFY3", "TotalFY4" };
                    for (var i = 0; i < arrayMonth.Count(); i++)
                    {
                        var _colName = arrayColumn[i].ToString();
                        var BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false).ToList();
                        if (i == 0)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month010 != null && w.month010 > 0).ToList();
                        }
                        else if (i == 1)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month011 != null && w.month011 > 0).ToList();
                        }
                        else if (i == 2)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month012 != null && w.month012 > 0).ToList();
                        }
                        else if (i == 3)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month101 != null && w.month101 > 0).ToList();
                        }
                        else if (i == 4)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month102 != null && w.month102 > 0).ToList();
                        }
                        else if (i == 5)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month103 != null && w.month103 > 0).ToList();
                        }
                        else if (i == 6)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month104 != null && w.month104 > 0).ToList();
                        }
                        else if (i == 7)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month105 != null && w.month105 > 0).ToList();
                        }
                        else if (i == 8)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month106 != null && w.month106 > 0).ToList();
                        }
                        else if (i == 9)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month107 != null && w.month107 > 0).ToList();
                        }
                        else if (i == 10)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month108 != null && w.month108 > 0).ToList();
                        }
                        else if (i == 11)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month109 != null && w.month109 > 0).ToList();
                        }
                        else if (i == 12)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month110 != null && w.month110 > 0).ToList();
                        }
                        else if (i == 13)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month111 != null && w.month111 > 0).ToList();
                        }
                        else if (i == 14)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month112 != null && w.month112 > 0).ToList();
                        }
                        else if (i == 15)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month201 != null && w.month201 > 0).ToList();
                        }
                        else if (i == 16)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month202 != null && w.month202 > 0).ToList();
                        }
                        else if (i == 17)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month203 != null && w.month203 > 0).ToList();
                        }
                        else if (i == 18)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.TotalFY3 != null && w.TotalFY3 > 0).ToList();
                        }
                        else if (i == 19)
                        {
                            BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.TotalFY4 != null && w.TotalFY4 > 0).ToList();
                        }
                        var currFY = (new DateTime((int)_Period_Year, 10, 1).AddMonths(i).Month < 4 ? (new DateTime((int)_Period_Year, 10, 1).AddMonths(i).Year).ToString().Substring(2, 2) : (new DateTime((int)_Period_Year, 10, 1).AddMonths(i).Year + 1).ToString().Substring(2, 2));
                        var BEXBELData = BEXBEL.Select(s => new
                        {
                            GL_Type = (i > 5 ? "BGT." : "FCT.") + "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year + (i == 19 ? 2 : 1)).ToString().Substring(2, 2)),
                            Period_Fy = "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year + (i == 19 ? 2 : 1)).ToString().Substring(2, 2)),
                            Budget_Type = s.Budget_Type,
                            Budget_Number = s.Budget_No,
                            Section_From_Code = s.Section_From_Code,
                            Section_From_Name = s.Section_From_Name,
                            COA_Code = s.COA_Code,
                            COA_Name = s.COA_Name,
                            Procate_Code = (string)null,
                            Procate_name = (string)null,
                            Section_To_Code = s.Section_To_Code,
                            Section_To_Name = s.Section_To_Name,
                            Description = s.Description.ToString(),
                            Currency = "IDR",
                            Amount = s.GetType().GetProperty(_colName).GetValue(s).ToString(),
                            Period_Month = (i == 18 || i == 19 ? "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year + (i == 19 ? 2 : 1)).ToString().Substring(2, 2) : new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).ToString("yyyy_MM")),
                            Group_Section = s.Group_Section,
                            Group_Cost = s.Group_Cost,
                            Priority_Category = s.Priority_Category
                        });
                        if (conn.State == ConnectionState.Closed)
                        {
                            conn.Open();
                        }

                        foreach (var data in BEXBELData)
                        {
                            queryText = string.Format("Insert into [Data$] " +
                                       "(`GL Type`,`Budget Period (Yearly)`,`BUDGET TYPE`,`Budget Number`,`Section From (Code)`,`Section From (Name)`,`COA CODE`,`COA NAME`,`Procate Code`,`Procate Name`,`Section To (Code)`,`Section To (Name)`,`DESCRIPTION`,`Currency`,`Amount`,`Budget Period (Monthly)`,`GROUP SECTION`,`GROUP COST`,`Priority Category`) " +
                                       "values('" + data.GL_Type + "','" + data.Period_Fy + "','" + data.Budget_Type + "','" + data.Budget_Number + "','" + data.Section_From_Code + "','" + data.Section_From_Name + "','" + data.COA_Code + "','" + data.COA_Name + "','" + data.Procate_Code + "','" + data.Procate_name + "','" + data.Section_To_Code + "','" + data.Section_To_Name + "','" + data.Description.Replace("'", "-") + "','" + data.Currency + "','" + data.Amount.ToString() + "', '" + data.Period_Month + "','','','" + data.Priority_Category + "');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }

                        //conn.Close();
                    }

                    //BIP, CIP, CFA Data =================================================================================================
                    //==========================================================================================================
                    //==========================================================================================================
                    var BIPCIPCFADepreData = db.V_FA_BudgetSystem_BIP_CIP_CFA.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false).ToList().Select(s => new
                    {
                        Budget_No = s.Budget_No,
                        Period_Year = s.Period_Year,
                        Budget_Type = s.Budget_Type,
                        Section_From_Code = s.Section_From_Code,
                        Section_From_Name = s.Section_From_Name,
                        Section_To_Code = s.Section_To_Code,
                        Section_To_Name = s.Section_To_Name,
                        COA_Code = db.FA_BudgetSystem_COA_Conversion.Where(w => w.COA_Code == s.COA_Code).First().COA_Code_Conversion,
                        COA_Name = db.FA_BudgetSystem_COA_Conversion.Where(w => w.COA_Code == s.COA_Code).First().COA_Name_Conversion,
                        Description = s.Asset_Name,
                        Group_Section = s.Group_Section,
                        Group_Cost = s.Group_Cost,
                        Acquisition_Value = s.Acquisition_Value,
                        Useful_Life = s.Useful_Life,
                        Allocation = s.Allocation,
                        Depre_Start = s.Depre_Start,
                        Priority_Code = s.Priority_Code,
                        Asset_Allocation = s.Budget_Allocation,
                        Monthly_Depre = s.Budget_Allocation / s.Useful_Life,
                        Depre_End = s.Depre_Start.AddMonths(s.Useful_Life - 1),
                        month010 = (new DateTime(s.Period_Year, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month011 = (new DateTime(s.Period_Year, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month012 = (new DateTime(s.Period_Year, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month101 = (new DateTime(s.Period_Year + 1, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month102 = (new DateTime(s.Period_Year + 1, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month103 = (new DateTime(s.Period_Year + 1, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month104 = (new DateTime(s.Period_Year + 1, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month105 = (new DateTime(s.Period_Year + 1, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month106 = (new DateTime(s.Period_Year + 1, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month107 = (new DateTime(s.Period_Year + 1, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month108 = (new DateTime(s.Period_Year + 1, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month109 = (new DateTime(s.Period_Year + 1, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month110 = (new DateTime(s.Period_Year + 1, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month111 = (new DateTime(s.Period_Year + 1, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month112 = (new DateTime(s.Period_Year + 1, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month201 = (new DateTime(s.Period_Year + 2, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month202 = (new DateTime(s.Period_Year + 2, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month203 = (new DateTime(s.Period_Year + 2, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month204 = (new DateTime(s.Period_Year + 2, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month205 = (new DateTime(s.Period_Year + 2, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month206 = (new DateTime(s.Period_Year + 2, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month207 = (new DateTime(s.Period_Year + 2, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month208 = (new DateTime(s.Period_Year + 2, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month209 = (new DateTime(s.Period_Year + 2, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month210 = (new DateTime(s.Period_Year + 2, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month211 = (new DateTime(s.Period_Year + 2, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month212 = (new DateTime(s.Period_Year + 2, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month301 = (new DateTime(s.Period_Year + 3, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month302 = (new DateTime(s.Period_Year + 3, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month303 = (new DateTime(s.Period_Year + 3, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month304 = (new DateTime(s.Period_Year + 3, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month305 = (new DateTime(s.Period_Year + 3, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month306 = (new DateTime(s.Period_Year + 3, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month307 = (new DateTime(s.Period_Year + 3, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month308 = (new DateTime(s.Period_Year + 3, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month309 = (new DateTime(s.Period_Year + 3, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month310 = (new DateTime(s.Period_Year + 3, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month311 = (new DateTime(s.Period_Year + 3, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month312 = (new DateTime(s.Period_Year + 3, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month401 = (new DateTime(s.Period_Year + 4, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month402 = (new DateTime(s.Period_Year + 4, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                        month403 = (new DateTime(s.Period_Year + 4, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
                    });

                    var BIPCIPCFAExpenseData = BIPCIPCFADepreData.Select(s => new
                    {
                        Period_Year = s.Period_Year,
                        Budget_Type = s.Budget_Type,
                        Section_From_Code = s.Section_From_Code,
                        Section_From_Name = s.Section_From_Name,
                        Section_To_Code = s.Section_To_Code,
                        Section_To_Name = s.Section_To_Name,
                        COA_Code = s.COA_Code,
                        COA_Name = s.COA_Name,
                        Description = s.Description,
                        Group_Section = s.Group_Section,
                        Group_Cost = s.Group_Cost,
                        month010 = s.month010 == 0 ? (long?)null : s.month010,
                        month011 = s.month011 == 0 ? (long?)null : s.month011,
                        month012 = s.month012 == 0 ? (long?)null : s.month012,
                        month101 = s.month101 == 0 ? (long?)null : s.month101,
                        month102 = s.month102 == 0 ? (long?)null : s.month102,
                        month103 = s.month103 == 0 ? (long?)null : s.month103,
                        month104 = s.month104 == 0 ? (long?)null : s.month104,
                        month105 = s.month105 == 0 ? (long?)null : s.month105,
                        month106 = s.month106 == 0 ? (long?)null : s.month106,
                        month107 = s.month107 == 0 ? (long?)null : s.month107,
                        month108 = s.month108 == 0 ? (long?)null : s.month108,
                        month109 = s.month109 == 0 ? (long?)null : s.month109,
                        month110 = s.month110 == 0 ? (long?)null : s.month110,
                        month111 = s.month111 == 0 ? (long?)null : s.month111,
                        month112 = s.month112 == 0 ? (long?)null : s.month112,
                        month201 = s.month201 == 0 ? (long?)null : s.month201,
                        month202 = s.month202 == 0 ? (long?)null : s.month202,
                        month203 = s.month203 == 0 ? (long?)null : s.month203,
                        TotalFY1 = s.month010 + s.month011 + s.month012 + s.month101 + s.month102 + s.month103,
                        TotalFY2 = s.month104 + s.month105 + s.month106 + s.month107 + s.month108 + s.month109 + s.month110 + s.month111 + s.month112 + s.month201 + s.month202 + s.month203,
                        TotalFY3 = s.month204 + s.month205 + s.month206 + s.month207 + s.month208 + s.month209 + s.month210 + s.month211 + s.month212 + s.month301 + s.month302 + s.month303,
                        TotalFY4 = s.month304 + s.month305 + s.month306 + s.month307 + s.month308 + s.month309 + s.month310 + s.month311 + s.month312 + s.month401 + s.month402 + s.month403,
                        Priority_Category = s.Priority_Code,
                        Budget_No = s.Budget_No
                    }).ToList();

                    for (var j = 0; j < arrayMonth.Count(); j++)
                    {

                        var _colName = arrayColumn[j].ToString();
                        var BIPCIPCFA = BIPCIPCFAExpenseData.ToList();
                        if (j == 0)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month010 != (long?)null && w.month010 > (long?)0).ToList();
                        }
                        else if (j == 1)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month011 != (long?)null && w.month011 > (long?)0).ToList();
                        }
                        else if (j == 2)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month012 != (long?)null && w.month012 > (long?)0).ToList();
                        }
                        else if (j == 3)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month101 != (long?)null && w.month101 > (long?)0).ToList();
                        }
                        else if (j == 4)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month102 != (long?)null && w.month102 > (long?)0).ToList();
                        }
                        else if (j == 5)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month103 != (long?)null && w.month103 > (long?)0).ToList();
                        }
                        else if (j == 6)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month104 != (long?)null && w.month104 > (long?)0).ToList();
                        }
                        else if (j == 7)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month105 != (long?)null && w.month105 > (long?)0).ToList();

                        }
                        else if (j == 8)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month106 != (long?)null && w.month106 > (long?)0).ToList();
                        }
                        else if (j == 9)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month107 != (long?)null && w.month107 > (long?)0).ToList();
                        }
                        else if (j == 10)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month108 != (long?)null && w.month108 > (long?)0).ToList();
                        }
                        else if (j == 11)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month109 != (long?)null && w.month109 > (long?)0).ToList();
                        }
                        else if (j == 12)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month110 != (long?)null && w.month110 > (long?)0).ToList();
                        }
                        else if (j == 13)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month111 != (long?)null && w.month111 > (long?)0).ToList();
                        }
                        else if (j == 14)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month112 != (long?)null && w.month112 > (long?)0).ToList();
                        }
                        else if (j == 15)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month201 != (long?)null && w.month201 > (long?)0).ToList();
                        }
                        else if (j == 16)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month202 != (long?)null && w.month202 > (long?)0).ToList();
                        }
                        else if (j == 17)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month203 != (long?)null && w.month203 > (long?)0).ToList();
                        }
                        else if (j == 18)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.TotalFY3 != (long?)null && w.TotalFY3 > (long?)0).ToList();
                        }
                        else if (j == 19)
                        {
                            BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.TotalFY4 != (long?)null && w.TotalFY4 > (long?)0).ToList();
                        }
                        var BIPCIPCFAData = BIPCIPCFA.Select(s => new
                        {
                            GL_Type = (j > 5 ? "BGT." : "FCT.") + "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year + (j == 19 ? 2 : 1)).ToString().Substring(2, 2)),
                            Period_Fy = "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year + (j == 19 ? 2 : 1)).ToString().Substring(2, 2)),
                            Budget_Type = s.Budget_Type,
                            Budget_Number = s.Budget_No,
                            Section_From_Code = s.Section_From_Code,
                            Section_From_Name = s.Section_From_Name,
                            COA_Code = s.COA_Code,
                            COA_Name = s.COA_Name,
                            Procate_Code = (string)null,
                            Procate_name = (string)null,
                            Section_To_Code = s.Section_To_Code,
                            Section_To_Name = s.Section_To_Name,
                            Description = s.Description.ToString(),
                            Currency = "IDR",
                            Amount = s.GetType().GetProperty(_colName).GetValue(s).ToString(),
                            Period_Month = (j == 18 || j == 19 ? "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year + (j == 19 ? 2 : 1)).ToString().Substring(2, 2) : new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).ToString("yyyy_MM")),
                            Group_Section = s.Group_Section,
                            Group_Cost = s.Group_Cost,
                            Priority_Category = s.Priority_Category
                        });

                        if (conn.State == ConnectionState.Closed)
                        {
                            conn.Open();
                        }

                        foreach (var data in BIPCIPCFAData)
                        {
                            queryText = string.Format("Insert into [Data$] " +
                                       "(`GL Type`,`Budget Period (Yearly)`,`BUDGET TYPE`,`Budget Number`,`Section From (Code)`,`Section From (Name)`,`COA CODE`,`COA NAME`,`Procate Code`,`Procate Name`,`Section To (Code)`,`Section To (Name)`,`DESCRIPTION`,`Currency`,`Amount`,`Budget Period (Monthly)`,`GROUP SECTION`,`GROUP COST`,`Priority Category`) " +
                                       "values('" + data.GL_Type + "','" + data.Period_Fy + "','" + data.Budget_Type + "','" + data.Budget_Number + "','" + data.Section_From_Code + "','" + data.Section_From_Name + "','" + data.COA_Code + "','" + data.COA_Name + "','" + data.Procate_Code + "','" + data.Procate_name + "','" + data.Section_To_Code + "','" + data.Section_To_Name + "','" + data.Description.Replace("'", "-") + "','" + data.Currency + "','" + data.Amount.ToString() + "', '" + data.Period_Month + "','','','" + data.Priority_Category + "');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }

                        //conn.Close();
                    }

                }
            }

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=List_GL.xlsx");
            Response.TransmitFile(path);
            Response.End();
            //Response.ClearContent();

            return RedirectToAction("SummarizeBudget", "BudgetSystem", new { area = "FA" });
        }
        //public ActionResult ListGLDownloadDataOld()
        //{
        //    var _period = Request["iDownloadPeriod"].Split('|');
        //    var _Period_FY = _period[0];
        //    var _Period_Year = int.Parse(_period[1]);

        //    var path = Server.MapPath("~/Files/FA/BudgetSystem/Download/List_GL.xlsx");
        //    System.IO.File.Copy(Server.MapPath("~/Files/FA/BudgetSystem/Master/List_GL.xlsx"), path, true);
        //    string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + path + "';Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
        //    using (OleDbConnection conn = new OleDbConnection(ConnectionString))
        //    {
        //        conn.Open();
        //        var queryText = "";
        //        using (OleDbCommand command = conn.CreateCommand())
        //        {
        //            int[] arrayMonth = { 10, 11, 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1, 2, 3, 93, 94 };
        //            string[] arrayColumn = { "month010", "month011", "month012", "month101", "month102", "month103", "month104", "month105", "month106", "month107", "month108", "month109", "month110", "month111", "month112", "month201", "month202", "month203", "TotalFY3", "TotalFY4" };
        //            for (var i = 0; i < arrayMonth.Count(); i++)
        //            {
        //                var _colName = arrayColumn[i].ToString();
        //                var BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false).ToList();
        //                if (i == 0)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month010 != null && w.month010 > 0).ToList();
        //                }
        //                else if (i == 1)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month011 != null && w.month011 > 0).ToList();
        //                }
        //                else if (i == 2)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month012 != null && w.month012 > 0).ToList();
        //                }
        //                else if (i == 3)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month101 != null && w.month101 > 0).ToList();
        //                }
        //                else if (i == 4)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month102 != null && w.month102 > 0).ToList();
        //                }
        //                else if (i == 5)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month103 != null && w.month103 > 0).ToList();
        //                }
        //                else if (i == 6)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month104 != null && w.month104 > 0).ToList();
        //                }
        //                else if (i == 7)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month105 != null && w.month105 > 0).ToList();
        //                }
        //                else if (i == 8)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month106 != null && w.month106 > 0).ToList();
        //                }
        //                else if (i == 9)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month107 != null && w.month107 > 0).ToList();
        //                }
        //                else if (i == 10)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month108 != null && w.month108 > 0).ToList();
        //                }
        //                else if (i == 11)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month109 != null && w.month109 > 0).ToList();
        //                }
        //                else if (i == 12)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month110 != null && w.month110 > 0).ToList();
        //                }
        //                else if (i == 13)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month111 != null && w.month111 > 0).ToList();
        //                }
        //                else if (i == 14)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month112 != null && w.month112 > 0).ToList();
        //                }
        //                else if (i == 15)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month201 != null && w.month201 > 0).ToList();
        //                }
        //                else if (i == 16)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month202 != null && w.month202 > 0).ToList();
        //                }
        //                else if (i == 17)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.month203 != null && w.month203 > 0).ToList();
        //                }
        //                else if (i == 18)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.TotalFY3 != null && w.TotalFY3 > 0).ToList();
        //                }
        //                else if (i == 19)
        //                {
        //                    BEXBEL = db.V_FA_BudgetSystem_BEX_BEL.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false && w.TotalFY4 != null && w.TotalFY4 > 0).ToList();
        //                }
        //                var currFY = (new DateTime((int)_Period_Year, 10, 1).AddMonths(i).Month < 4 ? (new DateTime((int)_Period_Year, 10, 1).AddMonths(i).Year).ToString().Substring(2, 2) : (new DateTime((int)_Period_Year, 10, 1).AddMonths(i).Year + 1).ToString().Substring(2, 2));
        //                var BEXBELData = BEXBEL.Select(s => new
        //                {
        //                    GL_Type = (i > 5 ? "BGT." : "FCT.") + "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year + (i == 19 ? 2 : 1)).ToString().Substring(2, 2)),
        //                    Period_Fy = "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year + (i == 19 ? 2 : 1)).ToString().Substring(2, 2)),
        //                    Budget_Type = s.Budget_Type,
        //                    Budget_Number = s.Budget_No,
        //                    Section_From_Code = s.Section_From_Code,
        //                    Section_From_Name = s.Section_From_Name,
        //                    COA_Code = s.COA_Code,
        //                    COA_Name = s.COA_Name,
        //                    Procate_Code = (string)null,
        //                    Procate_name = (string)null,
        //                    Section_To_Code = s.Section_To_Code,
        //                    Section_To_Name = s.Section_To_Name,
        //                    Description = s.Description.ToString(),
        //                    Currency = "IDR",
        //                    Amount = s.GetType().GetProperty(_colName).GetValue(s).ToString(),
        //                    Period_Month = (i == 18 || i == 19 ? "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).Year + (i == 19 ? 2 : 1)).ToString().Substring(2, 2) : new DateTime((int)s.Period_Year, 10, 1).AddMonths(i).ToString("yyyy_MM")),
        //                    Group_Section = s.Group_Section,
        //                    Group_Cost = s.Group_Cost,
        //                    Priority_Category = s.Priority_Category
        //                });
        //                //if (conn.State == ConnectionState.Closed)
        //                //{
        //                //    conn.Open();
        //                //}

        //                foreach (var data in BEXBELData)
        //                {
        //                    queryText = string.Format("Insert into [Data$] " +
        //                               "(`GL Type`,`BUDGET TYPE`,`Section From (Code)`,`Section From (Name)`,`COA CODE`,`COA NAME`,`Procate Code`,`Procate Name`,`Section To (Code)`,`Section To (Name)`,`DESCRIPTION`,`Currency`,`Amount`,`Budget Period (Monthly)`,`GROUP COST`,`UNIQ`,`COA BUDGET`,`BUDGET GROUP`,`BUDGET GROUP NAME`,`GROUP SEGMENT`,`Priority Category`,`Budget Number`) " +
        //                               "values('" + data.GL_Type + "','" + data.Budget_Type + "','" + data.Section_From_Code + "','" + data.Section_From_Name + "','" + data.COA_Code + "','" + data.COA_Name + "','" + data.Procate_Code + "','" + data.Procate_name + "','" + data.Section_To_Code + "','" + data.Section_To_Name + "','" + data.Description.Replace("'", "-") + "','" + data.Currency + "','" + data.Amount.ToString() + "', '" + data.Period_Month + "','','','','','','','" + data.Priority_Category + "','" + data.Budget_Number + "');");

        //                    command.CommandText = queryText;
        //                    command.ExecuteNonQuery();
        //                }

        //                //conn.Close();
        //            }

        //            //BIP, CIP, CFA Data =================================================================================================
        //            //==========================================================================================================
        //            //==========================================================================================================
        //            var BIPCIPCFADepreData = db.V_FA_BudgetSystem_BIP_CIP_CFA.Where(w => w.Period_FY == _Period_FY && w.Latest == 1 && w.Is_Reject == false).ToList().Select(s => new
        //            {
        //                Budget_No = s.Budget_No,
        //                Period_Year = s.Period_Year,
        //                Budget_Type = s.Budget_Type,
        //                Section_From_Code = s.Section_From_Code,
        //                Section_From_Name = s.Section_From_Name,
        //                Section_To_Code = s.Section_To_Code,
        //                Section_To_Name = s.Section_To_Name,
        //                COA_Code = db.FA_BudgetSystem_COA_Conversion.Where(w => w.COA_Code == s.COA_Code).First().COA_Code_Conversion,
        //                COA_Name = db.FA_BudgetSystem_COA_Conversion.Where(w => w.COA_Code == s.COA_Code).First().COA_Name_Conversion,
        //                Description = s.Asset_Name,
        //                Group_Section = s.Group_Section,
        //                Group_Cost = s.Group_Cost,
        //                Acquisition_Value = s.Acquisition_Value,
        //                Useful_Life = s.Useful_Life,
        //                Allocation = s.Allocation,
        //                Depre_Start = s.Depre_Start,
        //                Priority_Code = s.Priority_Code,
        //                Asset_Allocation = s.Budget_Allocation,
        //                Monthly_Depre = s.Budget_Allocation / s.Useful_Life,
        //                Depre_End = s.Depre_Start.AddMonths(s.Useful_Life - 1),
        //                month010 = (new DateTime(s.Period_Year, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month011 = (new DateTime(s.Period_Year, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month012 = (new DateTime(s.Period_Year, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month101 = (new DateTime(s.Period_Year + 1, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month102 = (new DateTime(s.Period_Year + 1, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month103 = (new DateTime(s.Period_Year + 1, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month104 = (new DateTime(s.Period_Year + 1, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month105 = (new DateTime(s.Period_Year + 1, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month106 = (new DateTime(s.Period_Year + 1, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month107 = (new DateTime(s.Period_Year + 1, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month108 = (new DateTime(s.Period_Year + 1, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month109 = (new DateTime(s.Period_Year + 1, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month110 = (new DateTime(s.Period_Year + 1, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month111 = (new DateTime(s.Period_Year + 1, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month112 = (new DateTime(s.Period_Year + 1, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 1, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month201 = (new DateTime(s.Period_Year + 2, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month202 = (new DateTime(s.Period_Year + 2, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month203 = (new DateTime(s.Period_Year + 2, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month204 = (new DateTime(s.Period_Year + 2, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month205 = (new DateTime(s.Period_Year + 2, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month206 = (new DateTime(s.Period_Year + 2, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month207 = (new DateTime(s.Period_Year + 2, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month208 = (new DateTime(s.Period_Year + 2, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month209 = (new DateTime(s.Period_Year + 2, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month210 = (new DateTime(s.Period_Year + 2, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month211 = (new DateTime(s.Period_Year + 2, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month212 = (new DateTime(s.Period_Year + 2, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 2, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month301 = (new DateTime(s.Period_Year + 3, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month302 = (new DateTime(s.Period_Year + 3, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month303 = (new DateTime(s.Period_Year + 3, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month304 = (new DateTime(s.Period_Year + 3, 4, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 4, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month305 = (new DateTime(s.Period_Year + 3, 5, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 5, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month306 = (new DateTime(s.Period_Year + 3, 6, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 6, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month307 = (new DateTime(s.Period_Year + 3, 7, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 7, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month308 = (new DateTime(s.Period_Year + 3, 8, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 8, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month309 = (new DateTime(s.Period_Year + 3, 9, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 9, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month310 = (new DateTime(s.Period_Year + 3, 10, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 10, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month311 = (new DateTime(s.Period_Year + 3, 11, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 11, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month312 = (new DateTime(s.Period_Year + 3, 12, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 3, 12, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month401 = (new DateTime(s.Period_Year + 4, 1, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 1, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month402 = (new DateTime(s.Period_Year + 4, 2, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 2, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //                month403 = (new DateTime(s.Period_Year + 4, 3, 1) >= s.Depre_Start && new DateTime(s.Period_Year + 4, 3, 1) <= s.Depre_Start.AddMonths(s.Useful_Life - 1) ? (s.Budget_Allocation / s.Useful_Life) : (long?)0),
        //            });

        //            var BIPCIPCFAExpenseData = BIPCIPCFADepreData.Select(s => new
        //            {
        //                Period_Year = s.Period_Year,
        //                Budget_Type = s.Budget_Type,
        //                Section_From_Code = s.Section_From_Code,
        //                Section_From_Name = s.Section_From_Name,
        //                Section_To_Code = s.Section_To_Code,
        //                Section_To_Name = s.Section_To_Name,
        //                COA_Code = s.COA_Code,
        //                COA_Name = s.COA_Name,
        //                Description = s.Description,
        //                Group_Section = s.Group_Section,
        //                Group_Cost = s.Group_Cost,
        //                month010 = s.month010 == 0 ? (long?)null : s.month010,
        //                month011 = s.month011 == 0 ? (long?)null : s.month011,
        //                month012 = s.month012 == 0 ? (long?)null : s.month012,
        //                month101 = s.month101 == 0 ? (long?)null : s.month101,
        //                month102 = s.month102 == 0 ? (long?)null : s.month102,
        //                month103 = s.month103 == 0 ? (long?)null : s.month103,
        //                month104 = s.month104 == 0 ? (long?)null : s.month104,
        //                month105 = s.month105 == 0 ? (long?)null : s.month105,
        //                month106 = s.month106 == 0 ? (long?)null : s.month106,
        //                month107 = s.month107 == 0 ? (long?)null : s.month107,
        //                month108 = s.month108 == 0 ? (long?)null : s.month108,
        //                month109 = s.month109 == 0 ? (long?)null : s.month109,
        //                month110 = s.month110 == 0 ? (long?)null : s.month110,
        //                month111 = s.month111 == 0 ? (long?)null : s.month111,
        //                month112 = s.month112 == 0 ? (long?)null : s.month112,
        //                month201 = s.month201 == 0 ? (long?)null : s.month201,
        //                month202 = s.month202 == 0 ? (long?)null : s.month202,
        //                month203 = s.month203 == 0 ? (long?)null : s.month203,
        //                TotalFY1 = s.month010 + s.month011 + s.month012 + s.month101 + s.month102 + s.month103,
        //                TotalFY2 = s.month104 + s.month105 + s.month106 + s.month107 + s.month108 + s.month109 + s.month110 + s.month111 + s.month112 + s.month201 + s.month202 + s.month203,
        //                TotalFY3 = s.month204 + s.month205 + s.month206 + s.month207 + s.month208 + s.month209 + s.month210 + s.month211 + s.month212 + s.month301 + s.month302 + s.month303,
        //                TotalFY4 = s.month304 + s.month305 + s.month306 + s.month307 + s.month308 + s.month309 + s.month310 + s.month311 + s.month312 + s.month401 + s.month402 + s.month403,
        //                Priority_Category = s.Priority_Code,
        //                Budget_No = s.Budget_No
        //            }).ToList();

        //            for (var j = 0; j < arrayMonth.Count(); j++)
        //            {

        //                var _colName = arrayColumn[j].ToString();
        //                var BIPCIPCFA = BIPCIPCFAExpenseData.ToList();
        //                if (j == 0)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month010 != (long?)null && w.month010 > (long?)0).ToList();
        //                }
        //                else if (j == 1)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month011 != (long?)null && w.month011 > (long?)0).ToList();
        //                }
        //                else if (j == 2)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month012 != (long?)null && w.month012 > (long?)0).ToList();
        //                }
        //                else if (j == 3)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month101 != (long?)null && w.month101 > (long?)0).ToList();
        //                }
        //                else if (j == 4)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month102 != (long?)null && w.month102 > (long?)0).ToList();
        //                }
        //                else if (j == 5)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month103 != (long?)null && w.month103 > (long?)0).ToList();
        //                }
        //                else if (j == 6)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month104 != (long?)null && w.month104 > (long?)0).ToList();
        //                }
        //                else if (j == 7)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month105 != (long?)null && w.month105 > (long?)0).ToList();

        //                }
        //                else if (j == 8)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month106 != (long?)null && w.month106 > (long?)0).ToList();
        //                }
        //                else if (j == 9)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month107 != (long?)null && w.month107 > (long?)0).ToList();
        //                }
        //                else if (j == 10)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month108 != (long?)null && w.month108 > (long?)0).ToList();
        //                }
        //                else if (j == 11)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month109 != (long?)null && w.month109 > (long?)0).ToList();
        //                }
        //                else if (j == 12)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month110 != (long?)null && w.month110 > (long?)0).ToList();
        //                }
        //                else if (j == 13)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month111 != (long?)null && w.month111 > (long?)0).ToList();
        //                }
        //                else if (j == 14)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month112 != (long?)null && w.month112 > (long?)0).ToList();
        //                }
        //                else if (j == 15)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month201 != (long?)null && w.month201 > (long?)0).ToList();
        //                }
        //                else if (j == 16)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month202 != (long?)null && w.month202 > (long?)0).ToList();
        //                }
        //                else if (j == 17)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.month203 != (long?)null && w.month203 > (long?)0).ToList();
        //                }
        //                else if (j == 18)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.TotalFY3 != (long?)null && w.TotalFY3 > (long?)0).ToList();
        //                }
        //                else if (j == 19)
        //                {
        //                    BIPCIPCFA = BIPCIPCFAExpenseData.Where(w => w.TotalFY4 != (long?)null && w.TotalFY4 > (long?)0).ToList();
        //                }
        //                var BIPCIPCFAData = BIPCIPCFA.Select(s => new
        //                {
        //                    GL_Type = (j > 5 ? "BGT." : "FCT.") + "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year + (j == 19 ? 2 : 1)).ToString().Substring(2, 2)),
        //                    Period_Fy = "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Month < 4 ? (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year).ToString().Substring(2, 2) : (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year + (j == 19 ? 2 : 1)).ToString().Substring(2, 2)),
        //                    Budget_Type = s.Budget_Type,
        //                    Budget_Number = s.Budget_No,
        //                    Section_From_Code = s.Section_From_Code,
        //                    Section_From_Name = s.Section_From_Name,
        //                    COA_Code = s.COA_Code,
        //                    COA_Name = s.COA_Name,
        //                    Procate_Code = (string)null,
        //                    Procate_name = (string)null,
        //                    Section_To_Code = s.Section_To_Code,
        //                    Section_To_Name = s.Section_To_Name,
        //                    Description = s.Description.ToString(),
        //                    Currency = "IDR",
        //                    Amount = s.GetType().GetProperty(_colName).GetValue(s).ToString(),
        //                    Period_Month = (j == 18 || j == 19 ? "FY1" + (new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).Year + (j == 19 ? 2 : 1)).ToString().Substring(2, 2) : new DateTime((int)s.Period_Year, 10, 1).AddMonths(j).ToString("yyyy_MM")),
        //                    Group_Section = s.Group_Section,
        //                    Group_Cost = s.Group_Cost,
        //                    Priority_Category = s.Priority_Category
        //                });

        //                //if (conn.State == ConnectionState.Closed)
        //                //{
        //                //    conn.Open();
        //                //}

        //                foreach (var data in BIPCIPCFAData)
        //                {
        //                    queryText = string.Format("Insert into [Data$] " +
        //                               "(`GL Type`,`BUDGET TYPE`,`Section From (Code)`,`Section From (Name)`,`COA CODE`,`COA NAME`,`Procate Code`,`Procate Name`,`Section To (Code)`,`Section To (Name)`,`DESCRIPTION`,`Currency`,`Amount`,`Budget Period (Monthly)`,`GROUP COST`,`UNIQ`,`COA BUDGET`,`BUDGET GROUP`,`BUDGET GROUP NAME`,`GROUP SEGMENT`,`Priority Category`,`Budget Number`) " +
        //                               "values('" + data.GL_Type + "','" + data.Budget_Type + "','" + data.Section_From_Code + "','" + data.Section_From_Name + "','" + data.COA_Code + "','" + data.COA_Name + "','" + data.Procate_Code + "','" + data.Procate_name + "','" + data.Section_To_Code + "','" + data.Section_To_Name + "','" + data.Description.Replace("'", "-") + "','" + data.Currency + "','" + data.Amount.ToString() + "', '" + data.Period_Month + "','','','','','','','" + data.Priority_Category + "','" + data.Budget_Number + "');");

        //                    command.CommandText = queryText;
        //                    command.ExecuteNonQuery();
        //                }

        //                //conn.Close();
        //            }

        //        }
        //    }

        //    Response.ContentType = "application/x-msexcel";
        //    Response.AppendHeader("Content-Disposition", "attachment; filename=List_GL.xlsx");
        //    Response.TransmitFile(path);
        //    Response.End();
        //    //Response.ClearContent();

        //    return RedirectToAction("SummarizeBudget", "BudgetSystem", new { area = "FA" });
        //}
    }
}