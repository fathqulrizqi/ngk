using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OleDb;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using System.Globalization;
using System.Data.SqlClient;

namespace NGKBusi.Areas.FA.Controllers
{
    public class LaborCostController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: FA/LaborCost
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        [Authorize]
        public ActionResult EmployeeData()
        {
            Int32 period = (Request["iLCEmployeeDataPeriod"] != null ? Int32.Parse(Request["iLCEmployeeDataPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            return View();
        }
        [Authorize]
        public ActionResult EditManPowerPlanRemark()
        {
            var iID = Int32.Parse(Request["iID"]);
            var iCat = Request["iCat"];
            var iVal = Request["iVal"];

            if (iCat == "Current Employee")
            {
                var updateData = db.FA_Labor_Cost_Employee_List.Where(w => w.ID == iID).FirstOrDefault();
                updateData.Remark = iVal;
            }
            else
            {
                var updateData = db.FA_Labor_Cost_MPP.Where(w => w.ID == iID).FirstOrDefault();
                updateData.Remark = iVal;
            }
            db.SaveChanges();
            return Json(true);
        }
        [Authorize]
        public ActionResult Master()
        {
            Int32 period = (Request["iLCWorkingDayPeriod"] != null ? Int32.Parse(Request["iLCWorkingDayPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            ViewBag.positionList = JsonConvert.SerializeObject(db.FA_Labor_Cost_Master.Select(s => new { id = s.Position, name = s.Position }).ToList());
            return View();
        }
        [Authorize]
        public ActionResult IncrementFactor()
        {
            Int32 period = (Request["iLCWorkingDayPeriod"] != null ? Int32.Parse(Request["iLCWorkingDayPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            ViewBag.positionList = JsonConvert.SerializeObject(db.FA_Labor_Cost_Master.Select(s => new { id = s.Position, name = s.Position }).ToList());
            return View();
        }
        [Authorize]
        public ActionResult Rate()
        {
            Int32 period = (Request["iLCWorkingDayPeriod"] != null ? Int32.Parse(Request["iLCWorkingDayPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            ViewBag.positionList = db.V_Users_Position.ToList();
            ViewBag.rate = db.FA_Labor_Cost_Rate.ToList();
            return View();
        }
        [Authorize]
        public ActionResult Access()
        {
            ViewBag.Access = db.FA_Labor_Cost_Access.OrderBy(o => o.User.Name).ToList();
            var AccessUserNIK = db.Users_Menus_Roles.Where(w => w.menuID == 24).Select(s => s.userNIK).ToList();
            ViewBag.AccessUser = db.V_Users_Active.Where(w => !AccessUserNIK.Contains(w.NIK)).OrderBy(o => o.Name).ToList();

            return View();
        }
        [Authorize]
        public ActionResult WorkingDay()
        {
            Int32 period = (Request["iLCWorkingDayPeriod"] != null ? Int32.Parse(Request["iLCWorkingDayPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;

            return View();
        }
        [Authorize]
        public ActionResult ManPowerPlan()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            //Tweak Iwaizako-san same as Kawai-san
            if (currUser.GetUserId().Trim() == "EXP.012")
            {
                currUserID = "EXP.014";
            }
            var currUserDivID = currUser.FindFirst("divID").Value;
            var currUserDeptID = currUser.FindFirst("deptID").Value;
            var currUserDeptName = currUser.FindFirst("deptName").Value;
            var currUserPostID = currUser.FindFirst("postID").Value;
            var lvl0 = 0;
            var lvl1 = 0;
            var lvl2 = 0;
            var lvl3 = 0;
            var lvl4 = 0;
            var lvl5 = 0;
            var lvl6 = 0;
            var section = new List<FA_Section>();
            Int32 period = (Request["iLCManPowerPlanPeriod"] != null ? Int32.Parse(Request["iLCManPowerPlanPeriod"]) : (DateTime.Now.Month >= 4 ? DateTime.Now.Year : DateTime.Now.AddYears(-1).Year));
            ViewBag.Period = TempData["period"] != null ? TempData["period"] : period;

            ViewBag.CheckLevel0 = (currUserID == "843.06.20" || currUserID == "546.08.05" || currUserID == "EXP.016" ? 1 : 0);
            ViewBag.CheckLevel1 = db.FA_Section.Where(w => w.labor_cost_signed == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel2 = db.FA_Section.Where(w => w.labor_cost_approval == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel3 = db.FA_Section.Where(w => w.labor_cost_approval2 == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel4 = db.FA_Section.Where(w => w.labor_cost_approval3 == currUserID && w.Period == period).ToList().Count();
            //ViewBag.CheckLevel3 = (currUserID == "625.05.12" ? 1 : 0);
            //ViewBag.CheckLevel4 = (currUserID == "EXP.0006" ? 1 : 0);
            ViewBag.CheckLevel5 = (currUserID == "592.02.10" || currUserID == "568.03.08" || currUserID == "843.06.20" || currUserID == "546.08.05" || currUserID == "EXP.0009" || currUserID == "EXP.016" ? 1 : 0);
            ViewBag.CheckLevel6 = (currUserID == "592.02.10" ? 1 : 0);

            var currLevel = String.IsNullOrEmpty(Request["iLCManPowerPlanLevel"]) ? (ViewBag.CheckLevel6 > 0 ? "6" : (ViewBag.CheckLevel5 > 0 ? "5" : (ViewBag.CheckLevel4 > 0 ? "4" : (ViewBag.CheckLevel3 > 0 ? "3" : (ViewBag.CheckLevel2 > 0 ? "2" : "1"))))) : Request["iLCManPowerPlanLevel"];
            ViewBag.LastLevel = currLevel;
            if (Int32.Parse(currLevel) > 0)
            {
                lvl0 = (Int32.Parse(currLevel) == 0 ? 1 : 0);
                lvl1 = (Int32.Parse(currLevel) == 1 ? 1 : 0);
                lvl2 = (Int32.Parse(currLevel) == 2 ? 1 : 0);
                lvl3 = (Int32.Parse(currLevel) == 3 ? 1 : 0);
                lvl4 = (Int32.Parse(currLevel) == 4 ? 1 : 0);
                lvl5 = (Int32.Parse(currLevel) == 5 ? 1 : 0);
                lvl6 = (Int32.Parse(currLevel) == 6 ? 1 : 0);
            }
            else
            {
                lvl0 = ViewBag.CheckLevel0;
                lvl1 = ViewBag.CheckLevel1;
                lvl2 = ViewBag.CheckLevel2;
                lvl3 = ViewBag.CheckLevel3;
                lvl4 = ViewBag.CheckLevel4;
                lvl5 = ViewBag.CheckLevel5;
                lvl6 = ViewBag.CheckLevel6;
            }

            if (lvl4 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval3 == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else if (lvl3 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval2 == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else if (lvl2 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else
            {
                if (lvl1 > 0)
                {
                    section = db.FA_Section.Where(w => w.labor_cost_signed == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
                }else if (lvl5 > 0 || lvl6 > 0 || lvl0 > 0)
                {
                    section = db.FA_Section.Where(w =>w.Period == period).OrderBy(o => o.Section_Name).ToList();
                }
            }

            if (Int32.Parse(currLevel) == 1 && currUserID == "592.02.10")
            {
                section = db.FA_Section.Where(w => w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }

            var sectionCost = new List<String>();
            foreach (var item in section)
            {
                sectionCost.Add(item.Section_Name);
            }
            var otp = new List<FA_Labor_Cost_OTP>();
            var otpAddNew = new List<FA_Labor_Cost_OTP>();
            if (Int32.Parse(currLevel) == 2)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By != null && w.Approved_By == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 3)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By != null && w.Approved_By2 == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 4)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By2 != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By2 != null && w.Approved_By3 == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 5)
            {
                //otp = db.FA_Labor_Cost_OTP.Where(w => w.Approved_By3 != null && w.Period == period).ToList();
                otp = db.FA_Labor_Cost_OTP.Where(w => w.Sign_By != null && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By3 != null && w.HCSign_By == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 6)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => w.HCSign_By != null && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.HCSign_By != null).ToList();
            }
            else if (Int32.Parse(currLevel) == 0)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period).ToList();
            }
            else
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By == null).ToList();
            }
            var otpSection = new List<String>();
            foreach (var item in otp)
            {
                otpSection.Add(item.Section);
            }
            var otpSectionAddNew = new List<String>();
            foreach (var item in otpAddNew)
            {
                otpSectionAddNew.Add(item.Section);
            }
            otpSection = otpSection.Distinct().ToList();
            ViewBag.Employee = db.V_Labor_Cost_List.Where(w => otpSectionAddNew.Contains(w.DeptName) && w.Period == period && w.Category != "Vacant").ToList();
            ViewBag.Section = db.FA_Section.AsEnumerable().Where(w => otpSectionAddNew.Contains(w.Section_Name) && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            ViewBag.SectionList = db.FA_Section.AsEnumerable().Where(w => otpSection.Contains(w.Section_Name) && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            ViewBag.SectionAll = db.FA_Section.Where(w => w.Period == period).OrderBy(o => o.Section_Name).ToList();
            ViewBag.MPPStatusSection = db.FA_Section.AsEnumerable().Where(w => sectionCost.Contains(w.Section_Name) && w.Period == period).ToList();
            ViewBag.Position = db.FA_Labor_Cost_Master.ToList();
            ViewBag.ManPowerPlan = db.V_Labor_Cost_List.AsEnumerable().Where(w => (otpSection.Contains(w.DeptName) || w.Created_By == currUserID) && w.Period == period).ToList(); ;
            ViewBag.SectionSummary = db.V_Labor_Cost_MPP_Summary.Where(w => otpSection.Contains(w.DeptName) && w.Period == period).AsEnumerable().OrderBy(o => o.Group).ThenBy(o => o.Action_Group).ThenByDescending(t => t.Status).ToList();
            if (Int32.Parse(currLevel) == 5 || Int32.Parse(currLevel) == 6)
            {
                ViewBag.MPPStatus = db.V_Labor_Cost_MPP_Status.Where(w => w.Period == period).ToList();
                ViewBag.MPPStatusSection = db.FA_Section.AsEnumerable().Where(w => w.Period == period).ToList();
            }
            else
            {
                ViewBag.MPPStatus = db.V_Labor_Cost_MPP_Status.Where(w => sectionCost.Contains(w.Section) && w.Period == period).ToList();
            }
            return View();
        }
        [Authorize]
        public ActionResult OvertimePlan()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            //Tweak Iwaizako-san same as Kawai-san
            if (currUser.GetUserId().Trim() == "EXP.012")
            {
                currUserID = "EXP.014";
            }
            var currUserDivID = currUser.FindFirst("divID").Value;
            var currUserDeptID = currUser.FindFirst("deptID").Value;
            var currUserDeptName = currUser.FindFirst("deptName").Value;
            var currUserPostID = currUser.FindFirst("postID").Value;
            var lvl0 = 0;
            var lvl1 = 0;
            var lvl2 = 0;
            var lvl3 = 0;
            var lvl4 = 0;
            var lvl5 = 0;
            var lvl6 = 0;
            var section = new List<FA_Section>();
            String[] excludePosition = new String[] { "ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "MANAGER (ACTING)", "SENIOR MANAGER", "GENERAL MANAGER", "ASSISTANT GENERAL MANAGER", "DEPUTY GENERAL MANAGER", "BOD" };
            Int32 period = (Request["iLCOvertimePlanPeriod"] != null ? Int32.Parse(Request["iLCOvertimePlanPeriod"]) : (DateTime.Now.Month >= 4 ? DateTime.Now.Year : DateTime.Now.AddYears(-1).Year));
            ViewBag.Period = TempData["period"] != null ? TempData["period"] : period;
            
            ViewBag.CheckLevel0 = (currUserID == "843.06.20" || currUserID == "546.08.05" || currUserID == "EXP.016" ? 1 : 0);
            ViewBag.CheckLevel1 = db.FA_Section.Where(w => w.labor_cost_signed == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel2 = db.FA_Section.Where(w => w.labor_cost_approval == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel3 = db.FA_Section.Where(w => w.labor_cost_approval2 == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel4 = db.FA_Section.Where(w => w.labor_cost_approval3 == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel5 = (currUserID == "592.02.10" || currUserID == "568.03.08" || currUserID == "843.06.20" || currUserID == "546.08.05" || currUserID == "EXP.0009" || currUserID == "EXP.016" ? 1 : 0);
            ViewBag.CheckLevel6 = (currUserID == "592.02.10" ? 1 : 0);

            var currLevel = String.IsNullOrEmpty(Request["iLCOverTimePlanLevel"]) ? (ViewBag.CheckLevel6 > 0 ? "6" : (ViewBag.CheckLevel5 > 0 ? "5" : (ViewBag.CheckLevel4 > 0 ? "4" : (ViewBag.CheckLevel3 > 0 ? "3" : (ViewBag.CheckLevel2 > 0 ? "2" : "1"))))) : Request["iLCOverTimePlanLevel"];
            ViewBag.LastLevel = currLevel;
            if (Int32.Parse(currLevel) > 0)
            {
                lvl0 = (Int32.Parse(currLevel) == 0 ? 1 : 0);
                lvl1 = (Int32.Parse(currLevel) == 1 ? 1 : 0);
                lvl2 = (Int32.Parse(currLevel) == 2 ? 1 : 0);
                lvl3 = (Int32.Parse(currLevel) == 3 ? 1 : 0);
                lvl4 = (Int32.Parse(currLevel) == 4 ? 1 : 0);
                lvl5 = (Int32.Parse(currLevel) == 5 ? 1 : 0);
                lvl6 = (Int32.Parse(currLevel) == 6 ? 1 : 0);
            }
            else
            {
                lvl0 = ViewBag.CheckLevel0;
                lvl1 = ViewBag.CheckLevel1;
                lvl2 = ViewBag.CheckLevel2;
                lvl3 = ViewBag.CheckLevel3;
                lvl4 = ViewBag.CheckLevel4;
                lvl5 = ViewBag.CheckLevel5;
                lvl6 = ViewBag.CheckLevel6;
            }


            if (lvl4 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval3 == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else if (lvl3 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval2 == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else if (lvl2 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else
            {
                if (lvl1 > 0)
                {
                    section = db.FA_Section.Where(w => w.labor_cost_signed == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
                }
                else if (lvl5 > 0 || lvl6 > 0 || lvl0 > 0)
                {
                    section = db.FA_Section.Where(w => w.Period == period).OrderBy(o => o.Section_Name).ToList();
                }
            }
                       
            if (Int32.Parse(currLevel) == 1 && currUserID == "592.02.10")
            {
                section = db.FA_Section.Where(w => w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }

            var sectionCost = new List<String>();
            foreach (var item in section)
            {
                sectionCost.Add(item.Section_Name);
            }
            var otp = new List<FA_Labor_Cost_OTP>();
            var otpAddNew = new List<FA_Labor_Cost_OTP>();
            if (Int32.Parse(currLevel) == 2)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By != null && w.Approved_By == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 3)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By != null && w.Approved_By2 == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 4)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By2 != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By2 != null && w.Approved_By3 == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 5)
            {
                //otp = db.FA_Labor_Cost_OTP.Where(w => w.Approved_By3 != null && w.Period == period).ToList();
                otp = db.FA_Labor_Cost_OTP.Where(w => w.Sign_By != null && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By3 != null && w.HCSign_By == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 6)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => w.HCSign_By != null && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.HCSign_By != null).ToList();
            }
            else if (Int32.Parse(currLevel) == 0)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period).ToList();
            }
            else
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By == null).ToList();
            }
            var otpSection = new List<String>();
            foreach (var item in otp)
            {
                otpSection.Add(item.Section);
            }
            var otpSectionAddNew = new List<String>();
            foreach (var item in otpAddNew)
            {
                otpSectionAddNew.Add(item.Section);
            }
            otpSection = otpSection.Distinct().ToList();
            ViewBag.Employee = db.FA_Labor_Cost_Employee_List.Where(w => otpSectionAddNew.Contains(w.CostName) && w.Period == period).ToList();

            ViewBag.SectionSummary = db.V_Labor_Cost_OTP_Summary.AsEnumerable().Where(w => otpSection.Contains(w.DeptName) && !excludePosition.Contains(w.Position) && w.Period == period).OrderBy(o => o.Group).ThenBy(o => o.Action_Group).ThenByDescending(t => t.Status).ToList();
            var SectionSumm = db.V_Labor_Cost_OTP_Summary.AsEnumerable().Where(w => otpSection.Contains(w.DeptName) && !excludePosition.Contains(w.Position) && w.Period == period).Select(s => s.DeptName).ToList();
            ViewBag.Section = db.FA_Section.AsEnumerable().Where(w => SectionSumm.Contains(w.Section_Name) && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            ViewBag.OTPList = db.V_Labor_Cost_OTP.Where(w => otpSection.Contains(w.Section) && !excludePosition.Contains(w.Position) && w.Category != "Pensiun/Resign" && w.Period == period).OrderBy(o => o.Group).ThenBy(o => o.Action_Group).ThenByDescending(t => t.Status).ThenBy(o => o.Name).ToList();

            return View();
        }
        [Authorize]
        public ActionResult MPPStatus()
        {
            Int32 period = (Request["iLCMPPStatusPeriod"] != null ? Int32.Parse(Request["iLCMPPStatusPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = TempData["period"] != null ? TempData["period"] : period;
            ViewBag.MPPStatus = db.V_Labor_Cost_MPP_Status.Where(w => w.Period == period).ToList();
            return View();
        }

        [Authorize]
        public ActionResult BudgetExpenseLabor()
        {
            Int32 period = (Request["iLCBudgetExpenseLaborPeriod"] != null ? Int32.Parse(Request["iLCBudgetExpenseLaborPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            TempData["period"] = period;
            ViewBag.BEL = db.FA_Labor_Cost_BEL.ToList();
            return View();
        }

        [Authorize]
        public ActionResult GeneralLedger()
        {
            Int32 period = (Request["iLCGeneralLedgerPeriod"] != null ? Int32.Parse(Request["iLCGeneralLedgerPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = TempData["period"] != null ? TempData["period"] : period;
            ViewBag.GL = db.FA_Labor_Cost_GL.Include("Sect").OrderBy(o => o.Account_Group).ThenBy(o => o.Action_Group_Section).ThenBy(o => o.COA_ID).ToList();
            return View();
        }

        [Authorize]
        public ActionResult GeneralLedgerDelete()
        {
            var objCtx = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)db).ObjectContext;
            objCtx.ExecuteStoreCommand("TRUNCATE TABLE [FA_Labor_Cost_GL]");
            return View();
        }
        [Authorize]
        public ActionResult generateBEL()
        {
            Int32 period = (Request["iLCBudgetExpenseLaborPeriod"] != null ? Int32.Parse(Request["iLCBudgetExpenseLaborPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            TempData["period"] = period;
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("laborCostGenerateBEL", conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                conn.Open();
                command.Parameters.AddWithValue("@period", period);
                command.ExecuteNonQuery();
                conn.Close();
            }
            return RedirectToAction("BudgetExpenseLabor", "LaborCost", new { area = "FA" });
        }
        [Authorize]
        public ActionResult generateGL()
        {
            Int32 period = (Request["iLCGeneralLedgerPeriod"] != null ? Int32.Parse(Request["iLCGeneralLedgerPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            TempData["period"] = period;
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("laborCostGenerateGL", conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                conn.Open();
                command.Parameters.AddWithValue("@period", period);
                command.ExecuteNonQuery();
                conn.Close();
            }

            return RedirectToAction("GeneralLedger", "LaborCost", new { area = "FA" });
        }

        [Authorize]
        [HttpPost]
        public ActionResult AccessAdd()
        {
            var NIK = Request["iLCAccessNIK"];
            var master = String.IsNullOrEmpty(Request["iLCAccessMaster"]) ? false : true;
            var dataEntry = String.IsNullOrEmpty(Request["iLCAccessDataEntry"]) ? false : true;
            var report = String.IsNullOrEmpty(Request["iLCAccessreport"]) ? false : true;

            db.Users_Menus_Roles.Add(new Users_Menus_Roles
            {
                userNIK = NIK,
                roleID = 3,
                menuID = 24,
                allowDelete = true,
                allowInsert = true,
                allowUpdate = true,
                isActive = true
            });

            db.FA_Labor_Cost_Access.Add(new FA_Labor_Cost_Access
            {
                NIK = NIK,
                isMaster = master,
                isDataEntry = dataEntry,
                isReport = report
            });
            db.SaveChanges();
            return RedirectToAction("Access", "LaborCost", new { area = "FA" });
        }
        [Authorize]
        [HttpPost]
        public ActionResult AccessDelete()
        {
            var NIK = Request["iLCADeleteNIK"];

            var deleteMenu = db.Users_Menus_Roles.Where(w => w.userNIK == NIK && w.menuID == 24).FirstOrDefault();
            db.Users_Menus_Roles.Remove(deleteMenu);
            var deleteAccess = db.FA_Labor_Cost_Access.Where(w => w.NIK == NIK).FirstOrDefault();
            db.FA_Labor_Cost_Access.Remove(deleteAccess);
            db.SaveChanges();

            return RedirectToAction("Access", "LaborCost", new { area = "FA" });
        }
        [Authorize]
        [HttpPost]
        public ActionResult setAccess()
        {
            var id = Int32.Parse(Request["iID"]);
            var menu = Request["iMenu"];
            var updateData = db.FA_Labor_Cost_Access.Where(w => w.ID == id).FirstOrDefault();
            switch (menu)
            {
                case "Master":
                    updateData.isMaster = !updateData.isMaster;
                    break;
                case "Data Entry":
                    updateData.isDataEntry = !updateData.isDataEntry;
                    break;
                case "Report":
                    updateData.isReport = !updateData.isReport;
                    break;
            }
            db.SaveChanges();
            return Json(true);
        }
        [HttpPost]
        [Authorize]
        public ActionResult ManPowerPlanAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            //Tweak Iwaizako-san same as Kawai-san
            if (currUser.GetUserId().Trim() == "EXP.012")
            {
                currUserID = "EXP.014";
            }
            Int32 period = (Request["iLCPeriod"] != null ? Int32.Parse(Request["iLCPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            TempData["period"] = period;
            var type = Request["iLCType"];
            var relNIK = Request["iLCRelated"];
            var sectID = !String.IsNullOrEmpty(Request["iLCSectionAll"]) ? Request["iLCSectionAll"] : Request["iLCSection"];
            var post = Request["iLCPosition"];
            var level = Request["iMPPAddLevel"];
            var status = !String.IsNullOrEmpty(Request["iLCStatus"]) ? Request["iLCStatus"] : "Permanent";
            var dateFromYear = Request["iLCFromYear"];
            var dateFromMonth = Request["iLCFromMonth"];
            var dateToYear = Request["iLCToYear"];
            var dateToMonth = Request["iLCToMonth"];
            var timePeriod = Request["iLCTimePeriod"];
            var qty = Int32.Parse(Request["iLCQty"]);
            var remark = Request["iLCRemark"];
            DateTime dateFrom, dateTo;
            DateTime dateBegin, dateLast;
            dateBegin = new DateTime(period, 10, 1);
            dateLast = new DateTime(period + 4, 3, 1);
            if (String.IsNullOrEmpty(dateFromYear))
            {
                dateFrom = new DateTime(period, 10, 1);
            }
            else
            {
                dateFrom = new DateTime(Int32.Parse(dateFromYear), Int32.Parse(dateFromMonth), 1);
            }

            if (String.IsNullOrEmpty(dateToYear))
            {
                dateTo = new DateTime(period + 4, 3, 1);
            }
            else
            {
                dateTo = new DateTime(Int32.Parse(dateToYear), Int32.Parse(dateToMonth), 1);
            }

            if (!String.IsNullOrEmpty(timePeriod))
            {
                dateTo = dateFrom.AddMonths(Int32.Parse(timePeriod) - 1);
                dateTo = (dateTo > dateLast ? dateLast : dateTo);
            }
            if (String.IsNullOrEmpty(relNIK))
            {
                for (int f = 1; f <= qty; f++)
                {
                    var newData = new FA_Labor_Cost_MPP();
                    newData.Period = period;
                    newData.Type = type;
                    newData.Related_NIK = relNIK;
                    newData.Section_Cost = sectID;
                    newData.Position = post;
                    newData.Status = status;
                    newData.Date_From = dateFrom;
                    newData.Date_To = dateTo;
                    newData.Created_By = currUserID;
                    newData.Created_Date = DateTime.Now;
                    newData.Remark = remark;
                    db.FA_Labor_Cost_MPP.Add(newData);
                    db.SaveChanges();
                }
                generateBasicOTP(relNIK, sectID, level, period);
            }
            else
            {
                //var updateData = db.FA_Labor_Cost_MPP.Where(w => w.Period == period && w.Related_NIK == relNIK).FirstOrDefault();
                //if (updateData != null)
                //{
                //    updateData.Type = type;
                //    updateData.Section_Cost = sectID;
                //    updateData.Position = post;
                //    updateData.Status = status;
                //    updateData.Date_From = dateFrom;
                //    updateData.Date_To = dateTo;
                //    updateData.Created_By = currUserID;
                //    updateData.Created_Date = DateTime.Now;
                //    updateData.Remark = remark;
                //    db.SaveChanges();
                //    generateBasicOTP(relNIK, null, level, period);
                //}
                //else
                //{
                var latestData = db.FA_Labor_Cost_MPP.Where(w => w.Related_NIK == relNIK && w.Period == period).OrderByDescending(o => o.ID).FirstOrDefault();
                var newData = new FA_Labor_Cost_MPP();
                newData.Period = period;
                newData.Type = type;
                newData.Related_NIK = relNIK;
                newData.Section_Cost = (type == "Pensiun/Resign" ? (latestData != null ? latestData.Section_Cost : sectID) : sectID);
                newData.Position = (type == "Pensiun/Resign" || type == "Mutasi" ? (latestData != null ? latestData.Position : post) : post);
                newData.Status = status;
                newData.Date_From = (type == "Pensiun/Resign" ? dateTo : dateFrom);
                newData.Date_To = dateTo;
                newData.Created_By = currUserID;
                newData.Created_Date = DateTime.Now;
                newData.Remark = remark;
                db.FA_Labor_Cost_MPP.Add(newData);
                db.SaveChanges();
                generateBasicOTP(relNIK, null, level, period);
                //}
            }
            return RedirectToAction("ManPowerPlan", "LaborCost", new { area = "FA" , iLCManPowerPlanPeriod = period, iLCManPowerPlanLevel = level });
        }


        [HttpPost]
        [Authorize]
        public ActionResult ManPowerPlanEdit()
        {
            var dataID = Int32.Parse(Request["iLCDataID"]);
            Int32 period = (Request["iLCPeriod"] != null ? Int32.Parse(Request["iLCPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            TempData["period"] = period;
            var relatedNIK = Request["iLCRelated"];
            var sectionCostID = Request["iLCSection"];
            var positionID = Request["iLCPosition"];
            var dateFromString = Request["iLCDate"].Split('-');
            var dateFrom = new DateTime(Int32.Parse(dateFromString[2]), Int32.Parse(dateFromString[1]), Int32.Parse(dateFromString[0]));

            var updateMPP = db.FA_Labor_Cost_MPP.Where(w => w.ID == dataID).FirstOrDefault();
            updateMPP.Period = period;
            updateMPP.Related_NIK = relatedNIK;
            updateMPP.Section_Cost = sectionCostID;
            updateMPP.Position = positionID;
            updateMPP.Date_From = dateFrom;
            db.SaveChanges();
            return RedirectToAction("ManPowerPlan", "LaborCost", new { area = "FA" });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ManPowerPlanDelete()
        {
            String[] iID = Request["iLCMPPDelete[]"].Split(',');
            int[] dataID = Array.ConvertAll<String, int>(iID, int.Parse);
            var lvl = Request["iMPPDeleteLevel"];
            Int32 period = (Request["iMPPDeletePeriod"] != null ? Int32.Parse(Request["iMPPDeletePeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            TempData["period"] = period;

            var currData = db.FA_Labor_Cost_MPP.Where(w => dataID.Contains(w.ID) && w.Period == period).ToList();
            var currLCData = db.V_Labor_Cost_List.Where(w => iID.Contains(w.ID) && w.Period == period).Select(s => s.ID).ToList();
            var currLCDataGenerate = db.V_Labor_Cost_List.Where(w => iID.Contains(w.ID) && w.Period == period).Select(s => s.NIK).ToList();
            db.FA_Labor_Cost_MPP.RemoveRange(currData);
            var currOTPData = db.FA_Labor_Cost_OTP.Where(w => currLCData.Contains(w.Related_ID) && w.Category != "Current Employee" && w.Period == period).ToList();
            db.FA_Labor_Cost_OTP.RemoveRange(currOTPData);
            db.SaveChanges();
            foreach (var item in currLCDataGenerate)
            {
                generateBasicOTP(item, null, lvl, period);
            }

            return RedirectToAction("ManPowerPlan", "LaborCost", new { area = "FA", iLCManPowerPlanPeriod = period, iLCManPowerPlanLevel = lvl });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ManPowerPlanSign(string submit)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            //Tweak Iwaizako-san same as Kawai-san
            if (currUser.GetUserId().Trim() == "EXP.012")
            {
                currUserID = "EXP.014";
            }
            String lvl = Request["iMPPSignLevel"];
            String[] section = Request["iLCMPPSign[]"].Split(',');
            Int32 period = (Request["iMPPSignPeriod"] != null ? Int32.Parse(Request["iMPPSignPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            TempData["period"] = period;
            section = section.Select(s => s.Replace("#", ",")).ToArray();
            foreach (var sect in section)
            {
                var currOTPData = db.FA_Labor_Cost_OTP.Where(w => w.Section == sect && w.Period == period).ToList();
                var currSection = db.FA_Section.Where(w => w.Section_Name == sect && w.Period == period).FirstOrDefault();
                if (submit == "Sign")
                {
                    if (lvl == "1")
                    {
                        currOTPData.ForEach(f => { f.Sign_By = currUserID; f.Sign_Date = DateTime.Now; });
                        currOTPData.ForEach(f => { f.Approved_By = (currSection.labor_cost_approval == null ? currUserID : null); f.Approved_Date = (currSection.labor_cost_approval == null ? DateTime.Now : (DateTime?)null); });
                        currOTPData.ForEach(f => { f.Approved_By2 = (currSection.labor_cost_approval2 == null ? currUserID : null); f.Approved_Date2 = (currSection.labor_cost_approval2 == null ? DateTime.Now : (DateTime?)null); });
                        currOTPData.ForEach(f => { f.Approved_By3 = (currSection.labor_cost_approval2 == null && currSection.labor_cost_approval3 == null ? currUserID : null); f.Approved_Date3 = (currSection.labor_cost_approval2 == null && currSection.labor_cost_approval3 == null ? DateTime.Now : (DateTime?)null); });
                    }
                    else if (lvl == "2")
                    {
                        currOTPData.ForEach(f => { f.Approved_By = currUserID; f.Approved_Date = DateTime.Now; });
                        currOTPData.ForEach(f => { f.Approved_By2 = (currSection.labor_cost_approval2 == null ? currUserID : null); f.Approved_Date2 = (currSection.labor_cost_approval2 == null ? DateTime.Now : (DateTime?)null); });
                        currOTPData.ForEach(f => { f.Approved_By3 = (currSection.labor_cost_approval2 == null && currSection.labor_cost_approval3 == null ? currUserID : null); f.Approved_Date3 = (currSection.labor_cost_approval2 == null && currSection.labor_cost_approval3 == null ? DateTime.Now : (DateTime?)null); });
                    }
                    else if (lvl == "3")
                    {
                        currOTPData.ForEach(f => { f.Approved_By2 = currUserID; f.Approved_Date2 = DateTime.Now; });
                        currOTPData.ForEach(f => { f.Approved_By3 = (currSection.labor_cost_approval3 == null ? currUserID : null); f.Approved_Date3 = (currSection.labor_cost_approval3 == null ? DateTime.Now : (DateTime?)null); });
                    }
                    else if (lvl == "4")
                    {
                        currOTPData.ForEach(f => { f.Approved_By3 = currUserID; f.Approved_Date3 = DateTime.Now; });
                    }
                    else if (lvl == "5")
                    {
                        currOTPData.ForEach(f => { f.HCSign_By = currUserID; f.HCSign_Date = DateTime.Now; });
                    }
                    else if (lvl == "6")
                    {
                        currOTPData.ForEach(f => { f.HCApproved_By = currUserID; f.HCApproved_Date = DateTime.Now; });
                    }
                }
                else if (submit == "Return")
                {
                    if (lvl == "2")
                    {
                        currOTPData.ForEach(f => { f.Sign_By = null; f.Sign_Date = null; });
                    }
                    else if (lvl == "3")
                    {
                        currOTPData.ForEach(f => { f.Approved_By = null; f.Approved_Date = null; });
                    }
                    else if (lvl == "4")
                    {
                        currOTPData.ForEach(f => { f.Approved_By2 = null; f.Approved_Date2 = null; });
                        currOTPData.ForEach(f => { f.Approved_By = (currSection.labor_cost_approval2 == null ? null : f.Approved_By); f.Approved_Date3 = (currSection.labor_cost_approval2 == null ? (DateTime?)null : f.Approved_Date3); });
                    }
                    else if (lvl == "5")
                    {
                        currOTPData.ForEach(f => { f.Approved_By3 = null; f.Approved_Date3 = null; f.Approved_By2 = null; f.Approved_Date2 = null; f.Approved_By = null; f.Approved_Date = null; f.Sign_By = null; f.Sign_Date = null; });
                        //currOTPData.ForEach(f => { f.Approved_By3 = null; f.Approved_Date3 = null; });
                        //currOTPData.ForEach(f => { f.Approved_By2 = (currSection.labor_cost_approval3 == null ? null : f.Approved_By2); f.Approved_Date2 = (currSection.labor_cost_approval3 == null ? (DateTime?)null : f.Approved_Date2); });
                        //currOTPData.ForEach(f => { f.Approved_By = (currSection.labor_cost_approval2 == null ? null : f.Approved_By); f.Approved_Date3 = (currSection.labor_cost_approval2 == null ? (DateTime?)null : f.Approved_Date3); });
                    }
                    else if (lvl == "6")
                    {
                        currOTPData.ForEach(f => { f.HCSign_By = null; f.HCSign_Date = null; f.Approved_By3 = null; f.Approved_Date3 = null; f.Approved_By2 = null; f.Approved_Date2 = null; f.Approved_By = null; f.Approved_Date = null; f.Sign_By = null; f.Sign_Date = null; });
                        //currOTPData.ForEach(f => { f.HCSign_By = null; f.HCSign_Date = null; });
                    }
                }
                db.SaveChanges();
            }

            return RedirectToAction("ManPowerPlan", "LaborCost", new { area = "FA", iLCManPowerPlanPeriod = period, iLCManPowerPlanLevel = lvl });
        }


        [HttpPost]
        [Authorize]
        public ActionResult RateAdd()
        {
            var period = Int32.Parse(Request["iLCPeriod"]);
            var bpjs_kes = Decimal.Parse(Request["iLCBPJSKes"]);
            var bpjs_kes_max = Int32.Parse(Request["iLCBPJSKesMax"]);
            var bpjs_jkk_jk = Decimal.Parse(Request["iLCBPJSJKKJK"]);
            var jshk = Decimal.Parse(Request["iLCJSHK"]);
            var tax_allowance = Decimal.Parse(Request["iLCTaxAllowance"]);
            var bpjs_jht = Decimal.Parse(Request["iLCBPJSJHT"]);
            var bpjs_jp = Decimal.Parse(Request["iLCBPJSJP"]);
            var bpjs_jp_max = Int32.Parse(Request["iLCBPJSJPMAX"]);
            var thr = Decimal.Parse(Request["iLCTHR"]);
            var alpha = Decimal.Parse(Request["iLCALPHA"]);
            var tat = Decimal.Parse(Request["iLCTAT"]);
            var ptkp = Int32.Parse(Request["iLCPTKP"]);
            var promosi = Decimal.Parse(Request["iLCPromosi"]);
            var asst_manager_overtime = Int32.Parse(Request["iLCAsstManOvertime"]);

            db.FA_Labor_Cost_Rate.Add(new FA_Labor_Cost_Rate
            {
                Period = period,
                BPJS_Kesehatan = bpjs_kes,
                BPJS_Kesehatan_Max = bpjs_kes_max,
                BPJS_JKK_JK = bpjs_jkk_jk,
                JSHK = jshk,
                Tax_Allowance = tax_allowance,
                BPJS_JHT = bpjs_jht,
                BPJS_JP = bpjs_jp,
                BPJS_JP_Max = bpjs_jp_max,
                THR = thr,
                ALPHA = alpha,
                TAT = tat,
                PTKP = ptkp,
                Promosi = promosi,
                Asst_Manager_Overtime = asst_manager_overtime
            });
            db.SaveChanges();

            return RedirectToAction("Rate", "LaborCost", new { area = "FA" });
        }

        [HttpPost]
        [Authorize]
        public ActionResult RateEdit()
        {
            var id = Int32.Parse(Request["iLCID"]);
            var period = Int32.Parse(Request["iLCPeriod"]);
            var bpjs_kes = Decimal.Parse(Request["iLCBPJSKes"]);
            var bpjs_kes_max = Int32.Parse(Request["iLCBPJSKesMax"]);
            var bpjs_jkk_jk = Decimal.Parse(Request["iLCBPJSJKKJK"]);
            var jshk = Decimal.Parse(Request["iLCJSHK"]);
            var tax_allowance = Decimal.Parse(Request["iLCTaxAllowance"]);
            var bpjs_jht = Decimal.Parse(Request["iLCBPJSJHT"]);
            var bpjs_jp = Decimal.Parse(Request["iLCBPJSJP"]);
            var bpjs_jp_max = Int32.Parse(Request["iLCBPJSJPMAX"]);
            var thr = Decimal.Parse(Request["iLCTHR"]);
            var alpha = Decimal.Parse(Request["iLCALPHA"]);
            var tat = Decimal.Parse(Request["iLCTAT"]);
            var ptkp = Int32.Parse(Request["iLCPTKP"]);
            var promosi = Decimal.Parse(Request["iLCPromosi"]);
            var asst_manager_overtime = Int32.Parse(Request["iLCAsstManOvertime"]);

            var updateData = db.FA_Labor_Cost_Rate.Where(w => w.ID == id).FirstOrDefault();
            updateData.Period = period;
            updateData.BPJS_Kesehatan = bpjs_kes;
            updateData.BPJS_Kesehatan_Max = bpjs_kes_max;
            updateData.BPJS_JKK_JK = bpjs_jkk_jk;
            updateData.JSHK = jshk;
            updateData.Tax_Allowance = tax_allowance;
            updateData.BPJS_JHT = bpjs_jht;
            updateData.BPJS_JP = bpjs_jp;
            updateData.BPJS_JP_Max = bpjs_jp_max;
            updateData.THR = thr;
            updateData.ALPHA = alpha;
            updateData.TAT = tat;
            updateData.PTKP = ptkp;
            updateData.Promosi = promosi;
            updateData.Asst_Manager_Overtime = asst_manager_overtime;
            db.SaveChanges();

            return RedirectToAction("Rate", "LaborCost", new { area = "FA" });
        }

        [HttpPost]
        [Authorize]
        public ActionResult RateDelete()
        {
            var iID = Int32.Parse(Request["iID"]);

            var currData = db.FA_Labor_Cost_Rate.Where(x => x.ID == iID).FirstOrDefault();
            db.FA_Labor_Cost_Rate.Remove(currData);
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public JsonResult SetEmployeeData(List<String[]> dataList, int periodes = 0)
        {
            Int32 periode = (periodes > 0 ? periodes : (Request["iLCEmployeeDataPeriod"] != null ? Int32.Parse(Request["iLCEmployeeDataPeriod"]) : DateTime.Now.Year));
            var arrayNIK = new List<String>();
            foreach (var i in dataList)
            {
                arrayNIK.Add(i[0]);
                var period = periode;
                var nik = i[0];
                var name = i[1];
                var basicSal = !String.IsNullOrEmpty(i[2]) ? Int32.Parse(i[2].Replace(".", "")) : (int?)null;
                var medical = !String.IsNullOrEmpty(i[3]) ? Int32.Parse(i[3].Replace(".", "")) : (int?)null;
                var trans = !String.IsNullOrEmpty(i[4]) ? Int32.Parse(i[4].Replace(".", "")) : (int?)null;
                var fixIncome = !String.IsNullOrEmpty(i[5]) ? Int32.Parse(i[5].Replace(".", "")) : (int?)null;
                var transDaily = !String.IsNullOrEmpty(i[6]) ? Int32.Parse(i[6].Replace(".", "")) : (int?)null;
                var allowJob = !String.IsNullOrEmpty(i[7]) ? Int32.Parse(i[7].Replace(".", "")) : (int?)null;
                var allowMeal = !String.IsNullOrEmpty(i[8]) ? Int32.Parse(i[8].Replace(".", "")) : (int?)null;
                var insentive = !String.IsNullOrEmpty(i[9]) ? Int32.Parse(i[9].Replace(".", "")) : (int?)null;
                var insentive2S2G = !String.IsNullOrEmpty(i[10]) ? Int32.Parse(i[10].Replace(".", "")) : (int?)null;
                var overtime = !String.IsNullOrEmpty(i[11]) ? Int32.Parse(i[11].Replace(".", "")) : (int?)null;
                var shift = !String.IsNullOrEmpty(i[12]) ? Int32.Parse(i[12].Replace(".", "")) : (int?)null;
                var thtTAT = !String.IsNullOrEmpty(i[13]) ? Int32.Parse(i[13].Replace(".", "")) : (int?)null;
                var rapel = !String.IsNullOrEmpty(i[14]) ? Int32.Parse(i[14].Replace(".", "")) : (int?)null;
                var other = !String.IsNullOrEmpty(i[15]) ? Int32.Parse(i[15].Replace(".", "")) : (int?)null;
                var atm = !String.IsNullOrEmpty(i[16]) ? Int32.Parse(i[16].Replace(".", "")) : (int?)null;
                var allowSkill = !String.IsNullOrEmpty(i[17]) ? Int32.Parse(i[17].Replace(".", "")) : (int?)null;
                var pph = !String.IsNullOrEmpty(i[18]) ? Int32.Parse(i[18].Replace(".", "")) : (int?)null;
                var unfixIncome = !String.IsNullOrEmpty(i[19]) ? Int32.Parse(i[19].Replace(".", "")) : (int?)null;
                var grossIncome = !String.IsNullOrEmpty(i[20]) ? Int32.Parse(i[20].Replace(".", "")) : (int?)null;
                if (!String.IsNullOrEmpty(nik))
                {
                    var checkData = db.FA_Labor_Cost_List.Where(w => w.Period == period && w.NIK == nik).FirstOrDefault();
                    if (checkData == null)
                    {
                        db.FA_Labor_Cost_List.Add(new FA_Labor_Cost_List
                        {
                            Period = period,
                            NIK = nik,
                            Name = name,
                            Basic_Salary = basicSal,
                            Medical = medical,
                            Transportation = trans,
                            Fix_Income = fixIncome,
                            Transportation_Daily = transDaily,
                            Allowance_Job = allowJob,
                            Allowance_Meal = allowMeal,
                            Insentive_Kehadiran = insentive,
                            Insentive_2S3G = insentive2S2G,
                            Overtime = overtime,
                            Shift = shift,
                            THR_TAT = thtTAT,
                            Rapel = rapel,
                            Other = other,
                            ATM = atm,
                            Allowance_Skill = allowSkill,
                            PPH21 = pph,
                            Unfix_Income = unfixIncome,
                            Gross_Income = grossIncome
                        });
                    }
                    else
                    {
                        checkData.Period = period;
                        checkData.NIK = nik;
                        checkData.Name = name;
                        checkData.Basic_Salary = basicSal;
                        checkData.Medical = medical;
                        checkData.Transportation = trans;
                        checkData.Fix_Income = fixIncome;
                        checkData.Transportation_Daily = transDaily;
                        checkData.Allowance_Job = allowJob;
                        checkData.Allowance_Meal = allowMeal;
                        checkData.Insentive_Kehadiran = insentive;
                        checkData.Insentive_2S3G = insentive2S2G;
                        checkData.Overtime = overtime;
                        checkData.Shift = shift;
                        checkData.THR_TAT = thtTAT;
                        checkData.Rapel = rapel;
                        checkData.Other = other;
                        checkData.ATM = atm;
                        checkData.Allowance_Skill = allowSkill;
                        checkData.PPH21 = pph;
                        checkData.Unfix_Income = unfixIncome;
                        checkData.Gross_Income = grossIncome;
                    }


                }
            }

            var deleteUnusedData = db.FA_Labor_Cost_List.Where(w => w.Period == DateTime.Now.Year && !arrayNIK.Contains(w.NIK)).ToList();
            db.FA_Labor_Cost_List.RemoveRange(deleteUnusedData);

            db.SaveChanges();

            return Json(true);
        }

        [HttpGet]
        public JsonResult GetEmployeeData()
        {
            Int32 period = (Request["iLCEmployeeDataPeriod"] != null ? Int32.Parse(Request["iLCEmployeeDataPeriod"]) : DateTime.Now.Year);
            CultureInfo idID = CultureInfo.CreateSpecificCulture("id-ID");
            var getData = db.FA_Labor_Cost_List.Where(w => w.Period == period).AsEnumerable().Select(s =>
         new
         {
             s.NIK,
             s.Name,
             Basic_Salary = s.Basic_Salary != null ? String.Format(idID, "{0:N0}", s.Basic_Salary.Value) : null,
             Medical = s.Medical != null ? String.Format(idID, "{0:N0}", s.Medical.Value) : null,
             Transportation = s.Transportation != null ? String.Format(idID, "{0:N0}", s.Transportation.Value) : null,
             Fix_Income = s.Fix_Income != null ? String.Format(idID, "{0:N0}", s.Fix_Income.Value) : null,
             Transportation_Daily = s.Transportation_Daily != null ? String.Format(idID, "{0:N0}", s.Transportation_Daily.Value) : null,
             Allowance_Job = s.Allowance_Job != null ? String.Format(idID, "{0:N0}", s.Allowance_Job.Value) : null,
             Allowance_Meal = s.Allowance_Meal != null ? String.Format(idID, "{0:N0}", s.Allowance_Meal.Value) : null,
             Insentive_Kehadiran = s.Insentive_Kehadiran != null ? String.Format(idID, "{0:N0}", s.Insentive_Kehadiran.Value) : null,
             Insentive_2S3G = s.Insentive_2S3G != null ? String.Format(idID, "{0:N0}", s.Insentive_2S3G.Value) : null,
             Overtime = s.Overtime != null ? String.Format(idID, "{0:N0}", s.Overtime.Value) : null,
             Shift = s.Shift != null ? String.Format(idID, "{0:N0}", s.Shift.Value) : null,
             THR_TAT = s.THR_TAT != null ? String.Format(idID, "{0:N0}", s.THR_TAT.Value) : null,
             Rapel = s.Rapel != null ? String.Format(idID, "{0:N0}", s.Rapel.Value) : null,
             Other = s.Other != null ? String.Format(idID, "{0:N0}", s.Other.Value) : null,
             ATM = s.ATM != null ? String.Format(idID, "{0:N0}", s.ATM.Value) : null,
             Allowance_Skill = s.Allowance_Skill != null ? String.Format(idID, "{0:N0}", s.Allowance_Skill.Value) : null,
             PPH21 = s.PPH21 != null ? String.Format(idID, "{0:N0}", s.PPH21.Value) : null,
             Unfix_Income = s.Unfix_Income != null ? String.Format(idID, "{0:N0}", s.Unfix_Income.Value) : null,
             Gross_Income = s.Gross_Income != null ? String.Format(idID, "{0:N0}", s.Gross_Income.Value) : null
         }
           ).ToList();
            if (getData.Count == 0)
            {
                var emptyList = new List<String[]>();
                emptyList.Add(new[] { "" });
                return Json(JsonConvert.SerializeObject(emptyList), JsonRequestBehavior.AllowGet);
            }
            return Json(JsonConvert.SerializeObject(GetObjectArray(getData)), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UploadEmployeeData()
        {
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/LaborCost"), fname);
                        file.SaveAs(fname);
                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=Excel 12.0;";

                        using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                        {
                            conn.Open();
                            using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                            {
                                string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                string query = "SELECT * FROM [" + sheetName + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        var newData = new List<String[]>();
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            newData.Add(new[] {
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["NIK"].ToString()) ? ds.Tables[0].Rows[i]["NIK"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["NAME"].ToString()) ? ds.Tables[0].Rows[i]["NAME"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["BASIC SALARY"].ToString()) ? ds.Tables[0].Rows[i]["BASIC SALARY"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["MEDICAL"].ToString()) ? ds.Tables[0].Rows[i]["MEDICAL"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["FIX TRANSP"].ToString()) ? ds.Tables[0].Rows[i]["FIX TRANSP"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["TOTAL FIX INCOME"].ToString()) ? ds.Tables[0].Rows[i]["TOTAL FIX INCOME"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["DAILY TRANSP"].ToString()) ? ds.Tables[0].Rows[i]["DAILY TRANSP"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["JOB ALLOW"].ToString()) ? ds.Tables[0].Rows[i]["JOB ALLOW"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["MEAL"].ToString()) ? ds.Tables[0].Rows[i]["MEAL"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["INCENTIVE"].ToString()) ? ds.Tables[0].Rows[i]["INCENTIVE"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["INCENTIVE (2S3G)"].ToString()) ? ds.Tables[0].Rows[i]["INCENTIVE (2S3G)"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["OVERTIME"].ToString()) ? ds.Tables[0].Rows[i]["OVERTIME"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["SHIFT"].ToString()) ? ds.Tables[0].Rows[i]["SHIFT"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["THR / TAT"].ToString()) ? ds.Tables[0].Rows[i]["THR / TAT"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["RAPEL"].ToString()) ? ds.Tables[0].Rows[i]["RAPEL"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["OTHERS"].ToString()) ? ds.Tables[0].Rows[i]["OTHERS"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["ATM"].ToString()) ? ds.Tables[0].Rows[i]["ATM"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["SKILL ALLOW"].ToString()) ? ds.Tables[0].Rows[i]["SKILL ALLOW"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["PPH 21"].ToString()) ? ds.Tables[0].Rows[i]["PPH 21"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["TOTAL UNFIX INCOME"].ToString()) ? ds.Tables[0].Rows[i]["TOTAL UNFIX INCOME"].ToString() : ""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["GROSS INCOME"].ToString()) ? ds.Tables[0].Rows[i]["GROSS INCOME"].ToString() : ""
                                            });
                                        }
                                        SetEmployeeData(newData, Int32.Parse(Request["iPeriod"]));
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

        public FileResult DownloadEmployeeDataFormat()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Files/LaborCost/";
            string fileName = "LaborCost_EmployeeData.xlsx";
            byte[] fileBytes = System.IO.File.ReadAllBytes(path + fileName);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        [HttpPost]
        public JsonResult SetMasterData(List<String[]> dataList)
        {
            var arrayPosition = new List<String>();
            foreach (var i in dataList)
            {
                arrayPosition.Add(i[1]);
                var period = Int32.Parse(i[0]);
                var position = i[1];
                var basicSal = !String.IsNullOrEmpty(i[2]) ? Int32.Parse(i[2].Replace(".", "").Replace(",", "")) : (int?)null;
                var trans = !String.IsNullOrEmpty(i[3]) ? Int32.Parse(i[3].Replace(".", "").Replace(",", "")) : (int?)null;
                var medical = !String.IsNullOrEmpty(i[4]) ? Int32.Parse(i[4].Replace(".", "").Replace(",", "")) : (int?)null;
                var transDaily = !String.IsNullOrEmpty(i[5]) ? Int32.Parse(i[5].Replace(".", "").Replace(",", "")) : (int?)null;
                var allowJob = !String.IsNullOrEmpty(i[6]) ? Int32.Parse(i[6].Replace(".", "").Replace(",", "")) : (int?)null;
                var shift = !String.IsNullOrEmpty(i[7]) ? Int32.Parse(i[7].Replace(".", "").Replace(",", "")) : (int?)null;
                var insentive = !String.IsNullOrEmpty(i[8]) ? Int32.Parse(i[8].Replace(".", "").Replace(",", "")) : (int?)null;
                var insentive2S2G = !String.IsNullOrEmpty(i[9]) ? Int32.Parse(i[9].Replace(".", "").Replace(",", "")) : (int?)null;
                var allowMeal = !String.IsNullOrEmpty(i[10]) ? Int32.Parse(i[10].Replace(".", "").Replace(",", "")) : (int?)null;
                var allowSkill = !String.IsNullOrEmpty(i[11]) ? Int32.Parse(i[11].Replace(".", "").Replace(",", "")) : (int?)null;
                var atm = !String.IsNullOrEmpty(i[12]) ? Int32.Parse(i[12].Replace(".", "").Replace(",", "")) : (int?)null;
                if (!String.IsNullOrEmpty(period.ToString()) && !String.IsNullOrEmpty(position))
                {
                    var checkData = db.FA_Labor_Cost_Master.Where(w => w.Period == period && w.Position == position).FirstOrDefault();
                    if (checkData == null)
                    {
                        db.FA_Labor_Cost_Master.Add(new FA_Labor_Cost_Master
                        {
                            Period = period,
                            Position = position,
                            Basic_Salary = basicSal,
                            Transportation = trans,
                            Medical = medical,
                            Transportation_Daily = transDaily,
                            Allowance_Job = allowJob,
                            Shift = shift,
                            Insentive_Kehadiran = insentive,
                            Insentive_2S3G = insentive2S2G,
                            Allowance_Skill = allowSkill,
                            Allowance_Meal = allowMeal,
                            ATM = atm
                        });
                    }
                    else
                    {
                        checkData.Period = period;
                        checkData.Position = position;
                        checkData.Basic_Salary = basicSal;
                        checkData.Transportation = trans;
                        checkData.Medical = medical;
                        checkData.Transportation_Daily = transDaily;
                        checkData.Allowance_Job = allowJob;
                        checkData.Shift = shift;
                        checkData.Insentive_Kehadiran = insentive;
                        checkData.Insentive_2S3G = insentive2S2G;
                        checkData.Allowance_Skill = allowSkill;
                        checkData.Allowance_Meal = allowMeal;
                        checkData.ATM = atm;
                    }
                }
            }

            var deleteUnusedData = db.FA_Labor_Cost_Master.Where(w => !arrayPosition.Contains(w.Position)).ToList();
            db.FA_Labor_Cost_Master.RemoveRange(deleteUnusedData);

            db.SaveChanges();

            return Json(true);
        }

        [HttpGet]
        public JsonResult GetMasterData()
        {
            CultureInfo idID = CultureInfo.CreateSpecificCulture("id-ID");
            var getData = db.FA_Labor_Cost_Master.AsEnumerable().Select(s =>
             new
             {
                 s.Period,
                 s.Position,
                 Basic_Salary = s.Basic_Salary != null ? String.Format(idID, "{0:N2", s.Basic_Salary.Value) : null,
                 Transportation = s.Transportation != null ? String.Format(idID, "{0:N2}", s.Transportation.Value) : null,
                 Medical = s.Medical != null ? String.Format(idID, "{0:N2}", s.Medical.Value) : null,
                 Transportation_Daily = s.Transportation_Daily != null ? String.Format(idID, "{0:N2}", s.Transportation_Daily.Value) : null,
                 Allowance_Job = s.Allowance_Job != null ? String.Format(idID, "{0:N2}", s.Allowance_Job.Value) : null,
                 Shift = s.Shift != null ? String.Format(idID, "{0:N2}", s.Shift.Value) : null,
                 Insentive_Kehadiran = s.Insentive_Kehadiran != null ? String.Format(idID, "{0:N2}", s.Insentive_Kehadiran.Value) : null,
                 Insentive_2S3G = s.Insentive_2S3G != null ? String.Format(idID, "{0:N2}", s.Insentive_2S3G.Value) : null,
                 Allowance_Meal = s.Allowance_Meal != null ? String.Format(idID, "{0:N2}", s.Allowance_Meal.Value) : null,
                 Allowance_Skill = s.Allowance_Skill != null ? String.Format(idID, "{0:N2}", s.Allowance_Skill.Value) : null,
                 ATM = s.ATM != null ? String.Format(idID, "{0:N2}", s.ATM.Value) : null
             }
                ).ToList();
            if (getData.Count == 0)
            {
                var emptyList = new List<String[]>();
                emptyList.Add(new[] { "" });
                return Json(JsonConvert.SerializeObject(emptyList), JsonRequestBehavior.AllowGet);
            }
            return Json(JsonConvert.SerializeObject(GetObjectArray(getData)), JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult UploadMasterData()
        {
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/LaborCost"), fname);
                        file.SaveAs(fname);
                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=Excel 12.0;";

                        using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                        {
                            conn.Open();
                            using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                            {
                                string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                string query = "SELECT * FROM [" + sheetName + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        var newData = new List<String[]>();
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            newData.Add(new[] {
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][0].ToString())?ds.Tables[0].Rows[i][0].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][1].ToString())?ds.Tables[0].Rows[i][1].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][2].ToString())?ds.Tables[0].Rows[i][2].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][3].ToString())?ds.Tables[0].Rows[i][3].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][4].ToString())?ds.Tables[0].Rows[i][4].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][5].ToString())?ds.Tables[0].Rows[i][5].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][6].ToString())?ds.Tables[0].Rows[i][6].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][7].ToString())?ds.Tables[0].Rows[i][7].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][8].ToString())?ds.Tables[0].Rows[i][8].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][9].ToString())?ds.Tables[0].Rows[i][9].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][10].ToString())?ds.Tables[0].Rows[i][10].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][11].ToString())?ds.Tables[0].Rows[i][11].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][12].ToString())?ds.Tables[0].Rows[i][12].ToString():""
                                            });
                                        }
                                        SetMasterData(newData);
                                    }
                                }
                            }
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

        public FileResult DownloadMasterDataFormat()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Files/LaborCost/";
            string fileName = "LaborCost_MasterData.xlsx";
            byte[] fileBytes = System.IO.File.ReadAllBytes(path + fileName);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        [HttpPost]
        public JsonResult SetIncrementFactorData(List<String[]> dataList)
        {
            var deleteUnusedData = db.FA_Labor_Cost_Increment_Factor.ToList();
            db.FA_Labor_Cost_Increment_Factor.RemoveRange(deleteUnusedData);

            db.SaveChanges();
            foreach (var i in dataList)
            {
                var period = Int32.Parse(i[0]);
                var position = i[1];
                var basicSal = !String.IsNullOrEmpty(i[2]) ? (Decimal.Parse(i[2].Replace("%", "")) < 10 ? (Decimal.Parse(i[2].Replace("%", "")) * 100).ToString("N2") + "%" : i[2].Replace(",", "")) : null;
                var trans = !String.IsNullOrEmpty(i[3]) ? (Decimal.Parse(i[3].Replace("%", "")) < 10 ? (Decimal.Parse(i[3].Replace("%", "")) * 100).ToString("N2") + "%" : i[3].Replace(",", "")) : null;
                var medical = !String.IsNullOrEmpty(i[4]) ? (Decimal.Parse(i[4].Replace("%", "")) < 10 ? (Decimal.Parse(i[4].Replace("%", "")) * 100).ToString("N2") + "%" : i[4].Replace(",", "")) : null;
                var transDaily = !String.IsNullOrEmpty(i[5]) ? (Decimal.Parse(i[5].Replace("%", "")) < 10 ? (Decimal.Parse(i[5].Replace("%", "")) * 100).ToString("N2") + "%" : i[5].Replace(",", "")) : null;
                var allowJob = !String.IsNullOrEmpty(i[6]) ? (Decimal.Parse(i[6].Replace("%", "")) < 10 ? (Decimal.Parse(i[6].Replace("%", "")) * 100).ToString("N2") + "%" : i[6].Replace(",", "")) : null;
                var shift = !String.IsNullOrEmpty(i[7]) ? (Decimal.Parse(i[7].Replace("%", "")) < 10 ? (Decimal.Parse(i[7].Replace("%", "")) * 100).ToString("N2") + "%" : i[7].Replace(",", "")) : null;
                var insentive = !String.IsNullOrEmpty(i[8]) ? (Decimal.Parse(i[8].Replace("%", "")) < 10 ? (Decimal.Parse(i[8].Replace("%", "")) * 100).ToString("N2") + "%" : i[8].Replace(",", "")) : null;
                var insentive2S2G = !String.IsNullOrEmpty(i[9]) ? (Decimal.Parse(i[9].Replace("%", "")) < 10 ? (Decimal.Parse(i[9].Replace("%", "")) * 100).ToString("N2") + "%" : i[9].Replace(",", "")) : null;
                var allowMeal = !String.IsNullOrEmpty(i[10]) ? (Decimal.Parse(i[10].Replace("%", "")) < 10 ? (Decimal.Parse(i[10].Replace("%", "")) * 100).ToString("N2") + "%" : i[10].Replace(",", "")) : null;
                var allowSkill = !String.IsNullOrEmpty(i[11]) ? (Decimal.Parse(i[11].Replace("%", "")) < 10 ? (Decimal.Parse(i[11].Replace("%", "")) * 100).ToString("N2") + "%" : i[11].Replace(",", "")) : null;
                var atm = !String.IsNullOrEmpty(i[12]) ? (Decimal.Parse(i[12].Replace("%", "")) < 10 ? (Decimal.Parse(i[12].Replace("%", "")) * 100).ToString("N2") + "%" : i[12].Replace(",", "")) : null;
                if (!String.IsNullOrEmpty(period.ToString()) && !String.IsNullOrEmpty(position))
                {
                    var checkData = db.FA_Labor_Cost_Increment_Factor.Where(w => w.Period == period && w.Position == position).FirstOrDefault();
                    if (checkData == null)
                    {
                        db.FA_Labor_Cost_Increment_Factor.Add(new FA_Labor_Cost_Increment_Factor
                        {
                            Period = period,
                            Position = position,
                            Basic_Salary = basicSal,
                            Transportation = trans,
                            Medical = medical,
                            Transportation_Daily = transDaily,
                            Allowance_Job = allowJob,
                            Shift = shift,
                            Insentive_Kehadiran = insentive,
                            Insentive_2S3G = insentive2S2G,
                            Allowance_Skill = allowSkill,
                            Allowance_Meal = allowMeal,
                            ATM = atm
                        });
                    }
                    else
                    {
                        checkData.Period = period;
                        checkData.Position = position;
                        checkData.Basic_Salary = basicSal;
                        checkData.Transportation = trans;
                        checkData.Medical = medical;
                        checkData.Transportation_Daily = transDaily;
                        checkData.Allowance_Job = allowJob;
                        checkData.Shift = shift;
                        checkData.Insentive_Kehadiran = insentive;
                        checkData.Insentive_2S3G = insentive2S2G;
                        checkData.Allowance_Skill = allowSkill;
                        checkData.Allowance_Meal = allowMeal;
                        checkData.ATM = atm;
                    }
                }
            }
            db.SaveChanges();

            return Json(true);
        }

        [HttpGet]
        public JsonResult GetIncrementFactorData()
        {
            CultureInfo idID = CultureInfo.CreateSpecificCulture("id-ID");
            Int32[] periodArray = { DateTime.Now.Year, DateTime.Now.Year + 1, DateTime.Now.Year + 2, DateTime.Now.Year + 3, DateTime.Now.Year + 4 };
            var getData = db.FA_Labor_Cost_Increment_Factor.Where(w => periodArray.Contains(w.Period)).AsEnumerable().Select(s =>
             new
             {
                 s.Period,
                 s.Position,
                 Basic_Salary = s.Basic_Salary != null ? String.Format(idID, "{0:N2}", s.Basic_Salary) : null,
                 Transportation = s.Transportation != null ? String.Format(idID, "{0:N2}", s.Transportation) : null,
                 Medical = s.Medical != null ? String.Format(idID, "{0:N2}", s.Medical) : null,
                 Transportation_Daily = s.Transportation_Daily != null ? String.Format(idID, "{0:N2}", s.Transportation_Daily) : null,
                 Allowance_Job = s.Allowance_Job != null ? String.Format(idID, "{0:N2}", s.Allowance_Job) : null,
                 Shift = s.Shift != null ? String.Format(idID, "{0:N2}", s.Shift) : null,
                 Insentive_Kehadiran = s.Insentive_Kehadiran != null ? String.Format(idID, "{0:N2}", s.Insentive_Kehadiran) : null,
                 Insentive_2S3G = s.Insentive_2S3G != null ? String.Format(idID, "{0:N2}", s.Insentive_2S3G) : null,
                 Allowance_Meal = s.Allowance_Meal != null ? String.Format(idID, "{0:N2}", s.Allowance_Meal) : null,
                 Allowance_Skill = s.Allowance_Skill != null ? String.Format(idID, "{0:N2}", s.Allowance_Skill) : null,
                 ATM = s.ATM != null ? String.Format(idID, "{0:N2}", s.ATM) : null
             }
                ).ToList();
            if (getData.Count == 0)
            {
                var emptyList = new List<String[]>();
                emptyList.Add(new[] { "" });
                return Json(JsonConvert.SerializeObject(emptyList), JsonRequestBehavior.AllowGet);
            }
            return Json(JsonConvert.SerializeObject(GetObjectArray(getData)), JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult UploadIncrementFactorData()
        {
            if (Request.Files.Count > 0)
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
                    fname = Path.Combine(Server.MapPath("~/Files/Temp/LaborCost"), fname);
                    file.SaveAs(fname);
                    DataSet ds = new DataSet();

                    //A 32-bit provider which enables the use of

                    string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=Excel 12.0;";

                    using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                    {
                        conn.Open();
                        using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                        {
                            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            //if (sheetName.Trim() != "LaborCost_IncrementFactorData")
                            //{
                            //    return Json("Incorrect File Format, Please Use 'LaborCost_IncrementFactorData' File!");
                            //}
                            string query = "SELECT * FROM [" + sheetName + "]";
                            OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                            //DataSet ds = new DataSet();
                            adapter.Fill(ds, "Items");
                            if (ds.Tables.Count > 0)
                            {
                                if (ds.Tables[0].Rows.Count > 0)
                                {
                                    var newData = new List<String[]>();
                                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                    {
                                        newData.Add(new[] {
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][0].ToString())?ds.Tables[0].Rows[i][0].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][1].ToString())?ds.Tables[0].Rows[i][1].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][2].ToString())?ds.Tables[0].Rows[i][2].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][3].ToString())?ds.Tables[0].Rows[i][3].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][4].ToString())?ds.Tables[0].Rows[i][4].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][5].ToString())?ds.Tables[0].Rows[i][5].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][6].ToString())?ds.Tables[0].Rows[i][6].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][7].ToString())?ds.Tables[0].Rows[i][7].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][8].ToString())?ds.Tables[0].Rows[i][8].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][9].ToString())?ds.Tables[0].Rows[i][9].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][10].ToString())?ds.Tables[0].Rows[i][10].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][11].ToString())?ds.Tables[0].Rows[i][11].ToString():""
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][12].ToString())?ds.Tables[0].Rows[i][12].ToString():""
                                            });
                                    }
                                    SetIncrementFactorData(newData);
                                }
                            }
                        }
                    }
                }
                // Returns message that successfully uploaded  
                return Json("File Uploaded Successfully!");
            }
            else
            {
                return Json("No files selected.");
            }
        }

        public FileResult DownloadIncrementFactorFormat()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Files/LaborCost/";
            string fileName = "LaborCost_IncrementFactorData.xlsx";
            byte[] fileBytes = System.IO.File.ReadAllBytes(path + fileName);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }


        public static IEnumerable<object> GetObjectArray<T>(IEnumerable<T> obj)
        {
            return obj.Select(o => o.GetType().GetProperties().Select(p => p.GetValue(o, null)));
        }

        [HttpGet]
        public JsonResult GetOvertimePlan()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            //Tweak Iwaizako-san same as Kawai-san
            if (currUser.GetUserId().Trim() == "EXP.012")
            {
                currUserID = "EXP.014";
            }
            var currUserDivID = currUser.FindFirst("divID").Value;
            var currUserDeptID = currUser.FindFirst("deptID").Value;
            var currUserDeptName = currUser.FindFirst("deptName").Value;
            var currUserPostID = currUser.FindFirst("postID").Value;
            var employee = new List<FA_Labor_Cost_Employee_List>();
            String[] excludePosition = new String[] { "ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "MANAGER (ACTING)", "SENIOR MANAGER", "GENERAL MANAGER", "ASSISTANT GENERAL MANAGER", "DEPUTY GENERAL MANAGER", "BOD" };

            var lvl0 = 0;
            var lvl1 = 0;
            var lvl2 = 0;
            var lvl3 = 0;
            var lvl4 = 0;
            var lvl5 = 0;
            var lvl6 = 0;
            var section = new List<FA_Section>();
            Int32 period = (Request["iOTPPeriod"] != null ? Int32.Parse(Request["iOTPPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;

            ViewBag.CheckLevel0 = (currUserID == "843.06.20" || currUserID == "546.08.05" || currUserID == "EXP.016" ? 1 : 0);
            ViewBag.CheckLevel1 = db.FA_Section.Where(w => w.labor_cost_signed == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel2 = db.FA_Section.Where(w => w.labor_cost_approval == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel3 = db.FA_Section.Where(w => w.labor_cost_approval2 == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel4 = db.FA_Section.Where(w => w.labor_cost_approval3 == currUserID && w.Period == period).ToList().Count();
            ViewBag.CheckLevel5 = (currUserID == "592.02.10" || currUserID == "568.03.08" || currUserID == "843.06.20" || currUserID == "546.08.05" || currUserID == "EXP.016" ? 1 : 0);
            ViewBag.CheckLevel6 = (currUserID == "592.02.10" ? 1 : 0);

            var currLevel = String.IsNullOrEmpty(Request["iOTPSignLevel"]) ? (ViewBag.CheckLevel6 > 0 ? "6" : (ViewBag.CheckLevel5 > 0 ? "5" : (ViewBag.CheckLevel4 > 0 ? "4" : (ViewBag.CheckLevel3 > 0 ? "3" : (ViewBag.CheckLevel2 > 0 ? "2" : "1"))))) : Request["iOTPSignLevel"];
            ViewBag.LastLevel = currLevel;
            if (Int32.Parse(currLevel) > 0)
            {
                lvl0 = (Int32.Parse(currLevel) == 0 ? 1 : 0);
                lvl1 = (Int32.Parse(currLevel) == 1 ? 1 : 0);
                lvl2 = (Int32.Parse(currLevel) == 2 ? 1 : 0);
                lvl3 = (Int32.Parse(currLevel) == 3 ? 1 : 0);
                lvl4 = (Int32.Parse(currLevel) == 4 ? 1 : 0);
                lvl5 = (Int32.Parse(currLevel) == 5 ? 1 : 0);
                lvl6 = (Int32.Parse(currLevel) == 6 ? 1 : 0);
            }
            else
            {
                lvl0 = ViewBag.CheckLevel0;
                lvl1 = ViewBag.CheckLevel1;
                lvl2 = ViewBag.CheckLevel2;
                lvl3 = ViewBag.CheckLevel3;
                lvl4 = ViewBag.CheckLevel4;
                lvl5 = ViewBag.CheckLevel5;
                lvl6 = ViewBag.CheckLevel6;
            }


            if (lvl4 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval3 == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else if (lvl3 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval2 == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else if (lvl2 > 0)
            {
                section = db.FA_Section.Where(w => w.labor_cost_approval == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
            }
            else
            {
                if (lvl1 > 0)
                {
                    section = db.FA_Section.Where(w => w.labor_cost_signed == currUserID && w.Period == period).OrderBy(o => o.Section_Name).ToList();
                }
                else if (lvl5 > 0 || lvl6 > 0 || lvl0 > 0)
                {
                    section = db.FA_Section.Where(w => w.Period == period).OrderBy(o => o.Section_Name).ToList();
                }
            }

            var sectionCost = new List<String>();
            foreach (var item in section)
            {
                sectionCost.Add(item.Section_Name);
            }
            var otp = new List<FA_Labor_Cost_OTP>();
            var otpAddNew = new List<FA_Labor_Cost_OTP>();
            if (Int32.Parse(currLevel) == 2)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By != null && w.Approved_By == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 3)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By != null && w.Approved_By2 == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 4)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By2 != null).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By2 != null && w.Approved_By3 == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 5)
            {
                //otp = db.FA_Labor_Cost_OTP.Where(w => w.Approved_By3 != null && w.Period == period).ToList();
                otp = db.FA_Labor_Cost_OTP.Where(w => w.Sign_By != null && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Approved_By3 != null && w.HCSign_By == null).ToList();
            }
            else if (Int32.Parse(currLevel) == 6)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => w.HCSign_By != null && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.HCSign_By != null).ToList();
            }
            else if (Int32.Parse(currLevel) == 0)
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period).ToList();
            }
            else
            {
                otp = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period).ToList();
                otpAddNew = db.FA_Labor_Cost_OTP.Where(w => sectionCost.Contains(w.Section) && w.Period == period && w.Sign_By == null).ToList();
            }
            var otpSection = new List<String>();
            foreach (var item in otp)
            {
                otpSection.Add(item.Section);
            }
            var otpSectionAddNew = new List<String>();
            foreach (var item in otpAddNew)
            {
                otpSectionAddNew.Add(item.Section);
            }
            otpSection = otpSection.Distinct().ToList();

            var getData = db.V_Labor_Cost_OTP.Where(w => otpSection.Contains(w.Section) && !excludePosition.Contains(w.Position) && w.Category != "Pensiun/Resign" && w.Period == period).Select(s =>
             new
             {
                 s.OTP_NIK,
                 s.Name,
                 s.Section,
                 s.Position,
                 s.Status,
                 s.Category,
                 M010 = (s.M010 != null ? s.M010.ToString() : "-"),
                 M011 = (s.M011 != null ? s.M011.ToString() : "-"),
                 M012 = (s.M012 != null ? s.M012.ToString() : "-"),
                 M101 = (s.M101 != null ? s.M101.ToString() : "-"),
                 M102 = (s.M102 != null ? s.M102.ToString() : "-"),
                 M103 = (s.M103 != null ? s.M103.ToString() : "-"),
                 M104 = (s.M104 != null ? s.M104.ToString() : "-"),
                 M105 = (s.M105 != null ? s.M105.ToString() : "-"),
                 M106 = (s.M106 != null ? s.M106.ToString() : "-"),
                 M107 = (s.M107 != null ? s.M107.ToString() : "-"),
                 M108 = (s.M108 != null ? s.M108.ToString() : "-"),
                 M109 = (s.M109 != null ? s.M109.ToString() : "-"),
                 M110 = (s.M110 != null ? s.M110.ToString() : "-"),
                 M111 = (s.M111 != null ? s.M111.ToString() : "-"),
                 M112 = (s.M112 != null ? s.M112.ToString() : "-"),
                 M201 = (s.M201 != null ? s.M201.ToString() : "-"),
                 M202 = (s.M202 != null ? s.M202.ToString() : "-"),
                 M203 = (s.M203 != null ? s.M203.ToString() : "-"),
                 M204 = (s.M204 != null ? s.M204.ToString() : "-"),
                 M205 = (s.M205 != null ? s.M205.ToString() : "-"),
                 M206 = (s.M206 != null ? s.M206.ToString() : "-"),
                 M207 = (s.M207 != null ? s.M207.ToString() : "-"),
                 M208 = (s.M208 != null ? s.M208.ToString() : "-"),
                 M209 = (s.M209 != null ? s.M209.ToString() : "-"),
                 M210 = (s.M210 != null ? s.M210.ToString() : "-"),
                 M211 = (s.M211 != null ? s.M211.ToString() : "-"),
                 M212 = (s.M212 != null ? s.M212.ToString() : "-"),
                 M301 = (s.M301 != null ? s.M301.ToString() : "-"),
                 M302 = (s.M302 != null ? s.M302.ToString() : "-"),
                 M303 = (s.M303 != null ? s.M303.ToString() : "-"),
                 M304 = (s.M304 != null ? s.M304.ToString() : "-"),
                 M305 = (s.M305 != null ? s.M305.ToString() : "-"),
                 M306 = (s.M306 != null ? s.M306.ToString() : "-"),
                 M307 = (s.M307 != null ? s.M307.ToString() : "-"),
                 M308 = (s.M308 != null ? s.M308.ToString() : "-"),
                 M309 = (s.M309 != null ? s.M309.ToString() : "-"),
                 M310 = (s.M310 != null ? s.M310.ToString() : "-"),
                 M311 = (s.M311 != null ? s.M311.ToString() : "-"),
                 M312 = (s.M312 != null ? s.M312.ToString() : "-"),
                 M401 = (s.M401 != null ? s.M401.ToString() : "-"),
                 M402 = (s.M402 != null ? s.M402.ToString() : "-"),
                 M403 = (s.M403 != null ? s.M403.ToString() : "-"),
                 Signed = (currLevel == "1" ? (s.Sign_By != null ? "Signed" : "") : (currLevel == "2" ? (s.Approved_By != null ? "Signed" : "") : (currLevel == "3" ? (s.Approved_By2 != null ? "Signed" : "") : (s.Approved_By3 != null ? "Signed" : "")))),
                 Remark = (s.Category == "Current Employee" ? db.FA_Labor_Cost_Employee_List.Where(w => w.NIK == s.OTP_NIK && w.Period == period).Select(o => o.Remark).FirstOrDefault() : db.FA_Labor_Cost_MPP.Where(w => w.ID.ToString() == s.Related_ID && w.Period == period).Select(o => o.Remark).FirstOrDefault()),
                 ID = s.ID,
                 Group = s.Group,
                 Action_Group = s.Action_Group
             }
                ).OrderBy(o => o.Group).ThenBy(o => o.Action_Group).ThenBy(o => o.Name).ThenBy(o => o.ID).ThenByDescending(t => t.Status).ToList();
            return Json(JsonConvert.SerializeObject(GetObjectArray(getData)), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SetOvertimePlan(List<String[]> dataList, int periodes = 0)
        {
            Int32 periode = (periodes > 0 ? periodes : (Request["iOTPPeriod"] != null ? Int32.Parse(Request["iOTPPeriod"]) : DateTime.Now.Year));
            var currUser = (ClaimsIdentity)User.Identity;
            foreach (var i in dataList)
            {
                var period = periode;
                var nik = i[0];
                var name = i[1];
                var section = System.Net.WebUtility.HtmlDecode(i[2]);
                var position = i[3];
                var status = i[4];
                var category = i[5];
                var m010 = !String.IsNullOrEmpty(i[6]) ? i[6] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[6].Replace(",", ".")),1) : 0;
                var m011 = !String.IsNullOrEmpty(i[7]) ? i[7] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[7].Replace(",", ".")),1) : 0;
                var m012 = !String.IsNullOrEmpty(i[8]) ? i[8] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[8].Replace(",", ".")),1) : 0;
                var m101 = !String.IsNullOrEmpty(i[9]) ? i[9] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[9].Replace(",", ".")),1) : 0;
                var m102 = !String.IsNullOrEmpty(i[10]) ? i[10] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[10].Replace(",", ".")),1) : 0;
                var m103 = !String.IsNullOrEmpty(i[11]) ? i[11] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[11].Replace(",", ".")),1) : 0;
                var m104 = !String.IsNullOrEmpty(i[12]) ? i[12] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[12].Replace(",", ".")),1) : 0;
                var m105 = !String.IsNullOrEmpty(i[13]) ? i[13] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[13].Replace(",", ".")),1) : 0;
                var m106 = !String.IsNullOrEmpty(i[14]) ? i[14] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[14].Replace(",", ".")),1) : 0;
                var m107 = !String.IsNullOrEmpty(i[15]) ? i[15] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[15].Replace(",", ".")),1) : 0;
                var m108 = !String.IsNullOrEmpty(i[16]) ? i[16] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[16].Replace(",", ".")),1) : 0;
                var m109 = !String.IsNullOrEmpty(i[17]) ? i[17] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[17].Replace(",", ".")),1) : 0;
                var m110 = !String.IsNullOrEmpty(i[18]) ? i[18] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[18].Replace(",", ".")),1) : 0;
                var m111 = !String.IsNullOrEmpty(i[19]) ? i[19] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[19].Replace(",", ".")),1) : 0;
                var m112 = !String.IsNullOrEmpty(i[20]) ? i[20] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[20].Replace(",", ".")),1) : 0;
                var m201 = !String.IsNullOrEmpty(i[21]) ? i[21] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[21].Replace(",", ".")),1) : 0;
                var m202 = !String.IsNullOrEmpty(i[22]) ? i[22] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[22].Replace(",", ".")),1) : 0;
                var m203 = !String.IsNullOrEmpty(i[23]) ? i[23] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[23].Replace(",", ".")),1) : 0;
                var m204 = !String.IsNullOrEmpty(i[24]) ? i[24] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[24].Replace(",", ".")),1) : 0;
                var m205 = !String.IsNullOrEmpty(i[25]) ? i[25] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[25].Replace(",", ".")),1) : 0;
                var m206 = !String.IsNullOrEmpty(i[26]) ? i[26] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[26].Replace(",", ".")),1) : 0;
                var m207 = !String.IsNullOrEmpty(i[27]) ? i[27] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[27].Replace(",", ".")),1) : 0;
                var m208 = !String.IsNullOrEmpty(i[28]) ? i[28] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[28].Replace(",", ".")),1) : 0;
                var m209 = !String.IsNullOrEmpty(i[29]) ? i[29] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[29].Replace(",", ".")),1) : 0;
                var m210 = !String.IsNullOrEmpty(i[30]) ? i[30] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[30].Replace(",", ".")),1) : 0;
                var m211 = !String.IsNullOrEmpty(i[31]) ? i[31] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[31].Replace(",", ".")),1) : 0;
                var m212 = !String.IsNullOrEmpty(i[32]) ? i[32] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[32].Replace(",", ".")),1) : 0;
                var m301 = !String.IsNullOrEmpty(i[33]) ? i[33] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[33].Replace(",", ".")),1) : 0;
                var m302 = !String.IsNullOrEmpty(i[34]) ? i[34] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[34].Replace(",", ".")),1) : 0;
                var m303 = !String.IsNullOrEmpty(i[35]) ? i[35] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[35].Replace(",", ".")),1) : 0;
                var m304 = !String.IsNullOrEmpty(i[36]) ? i[36] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[36].Replace(",", ".")),1) : 0;
                var m305 = !String.IsNullOrEmpty(i[37]) ? i[37] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[37].Replace(",", ".")),1) : 0;
                var m306 = !String.IsNullOrEmpty(i[38]) ? i[38] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[38].Replace(",", ".")),1) : 0;
                var m307 = !String.IsNullOrEmpty(i[39]) ? i[39] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[39].Replace(",", ".")),1) : 0;
                var m308 = !String.IsNullOrEmpty(i[40]) ? i[40] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[40].Replace(",", ".")),1) : 0;
                var m309 = !String.IsNullOrEmpty(i[41]) ? i[41] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[41].Replace(",", ".")),1) : 0;
                var m310 = !String.IsNullOrEmpty(i[42]) ? i[42] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[42].Replace(",", ".")),1) : 0;
                var m311 = !String.IsNullOrEmpty(i[43]) ? i[43] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[43].Replace(",", ".")),1) : 0;
                var m312 = !String.IsNullOrEmpty(i[44]) ? i[44] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[44].Replace(",", ".")),1) : 0;
                var m401 = !String.IsNullOrEmpty(i[45]) ? i[45] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[45].Replace(",", ".")),1) : 0;
                var m402 = !String.IsNullOrEmpty(i[46]) ? i[46] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[46].Replace(",", ".")),1) : 0;
                var m403 = !String.IsNullOrEmpty(i[47]) ? i[47] == "-" ? (decimal?)null : Math.Round(Decimal.Parse(i[47].Replace(",", ".")),1) : 0;
                var remark = i[49];

                var checkData = db.FA_Labor_Cost_OTP.Where(w => w.Period == period && w.OTP_NIK == nik && w.Position == position && w.Section == section && w.Status == status && w.Category == category).FirstOrDefault();
                if (checkData != null)
                {
                    checkData.M010 = (checkData.M010 != null ? (m010 != null ? m010 : 0) : null);
                    checkData.M011 = (checkData.M011 != null ? (m011 != null ? m011 : 0) : null);
                    checkData.M012 = (checkData.M012 != null ? (m012 != null ? m012 : 0) : null);
                    checkData.M101 = (checkData.M101 != null ? (m101 != null ? m101 : 0) : null);
                    checkData.M102 = (checkData.M102 != null ? (m102 != null ? m102 : 0) : null);
                    checkData.M103 = (checkData.M103 != null ? (m103 != null ? m103 : 0) : null);
                    checkData.M104 = (checkData.M104 != null ? (m104 != null ? m104 : 0) : null);
                    checkData.M105 = (checkData.M105 != null ? (m105 != null ? m105 : 0) : null);
                    checkData.M106 = (checkData.M106 != null ? (m106 != null ? m106 : 0) : null);
                    checkData.M107 = (checkData.M107 != null ? (m107 != null ? m107 : 0) : null);
                    checkData.M108 = (checkData.M108 != null ? (m108 != null ? m108 : 0) : null);
                    checkData.M109 = (checkData.M109 != null ? (m109 != null ? m109 : 0) : null);
                    checkData.M110 = (checkData.M110 != null ? (m110 != null ? m110 : 0) : null);
                    checkData.M111 = (checkData.M111 != null ? (m111 != null ? m111 : 0) : null);
                    checkData.M112 = (checkData.M112 != null ? (m112 != null ? m112 : 0) : null);
                    checkData.M201 = (checkData.M201 != null ? (m201 != null ? m201 : 0) : null);
                    checkData.M202 = (checkData.M202 != null ? (m202 != null ? m202 : 0) : null);
                    checkData.M203 = (checkData.M203 != null ? (m203 != null ? m203 : 0) : null);
                    checkData.M204 = (checkData.M204 != null ? (m204 != null ? m204 : 0) : null);
                    checkData.M205 = (checkData.M205 != null ? (m205 != null ? m205 : 0) : null);
                    checkData.M206 = (checkData.M206 != null ? (m206 != null ? m206 : 0) : null);
                    checkData.M207 = (checkData.M207 != null ? (m207 != null ? m207 : 0) : null);
                    checkData.M208 = (checkData.M208 != null ? (m208 != null ? m208 : 0) : null);
                    checkData.M209 = (checkData.M209 != null ? (m209 != null ? m209 : 0) : null);
                    checkData.M210 = (checkData.M210 != null ? (m210 != null ? m210 : 0) : null);
                    checkData.M211 = (checkData.M211 != null ? (m211 != null ? m211 : 0) : null);
                    checkData.M212 = (checkData.M212 != null ? (m212 != null ? m212 : 0) : null);
                    checkData.M301 = (checkData.M301 != null ? (m301 != null ? m301 : 0) : null);
                    checkData.M302 = (checkData.M302 != null ? (m302 != null ? m302 : 0) : null);
                    checkData.M303 = (checkData.M303 != null ? (m303 != null ? m303 : 0) : null);
                    checkData.M304 = (checkData.M304 != null ? (m304 != null ? m304 : 0) : null);
                    checkData.M305 = (checkData.M305 != null ? (m305 != null ? m305 : 0) : null);
                    checkData.M306 = (checkData.M306 != null ? (m306 != null ? m306 : 0) : null);
                    checkData.M307 = (checkData.M307 != null ? (m307 != null ? m307 : 0) : null);
                    checkData.M308 = (checkData.M308 != null ? (m308 != null ? m308 : 0) : null);
                    checkData.M309 = (checkData.M309 != null ? (m309 != null ? m309 : 0) : null);
                    checkData.M310 = (checkData.M310 != null ? (m310 != null ? m310 : 0) : null);
                    checkData.M311 = (checkData.M311 != null ? (m311 != null ? m311 : 0) : null);
                    checkData.M312 = (checkData.M312 != null ? (m312 != null ? m312 : 0) : null);
                    checkData.M401 = (checkData.M401 != null ? (m401 != null ? m401 : 0) : null);
                    checkData.M402 = (checkData.M402 != null ? (m402 != null ? m402 : 0) : null);
                    checkData.M403 = (checkData.M403 != null ? (m403 != null ? m403 : 0) : null);
                    checkData.User_By = currUser.GetUserId();
                    checkData.Date_At = DateTime.Now;
                    var mppID = db.V_Labor_Cost_List.Where(w => w.NIK == nik && w.DeptName == section && w.Position == position && w.Status == status && w.Category == category && w.Period == period).Select(f => f.MPP_ID).Single();
                    if (category == "Current Employee")
                    {
                        var updateRemark = db.FA_Labor_Cost_Employee_List.Where(w => w.ID == mppID && w.Period == period).FirstOrDefault();
                        updateRemark.Remark = remark;
                    }
                    else
                    {
                        var updateRemark = db.FA_Labor_Cost_MPP.Where(w => w.ID == mppID && w.Period == period).FirstOrDefault();
                        updateRemark.Remark = remark;
                    }
                }
            }
            db.SaveChanges();

            return Json(true);
        }

        [HttpPost]
        public ActionResult UploadOvertimePlan()
        {
            if (Request.Files.Count > 0)
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
                    fname = Path.Combine(Server.MapPath("~/Files/Temp/LaborCost"), fname);
                    file.SaveAs(fname);
                    DataSet ds = new DataSet();
                    var ext = Path.GetExtension(fname);

                    //A 32-bit provider which enables the use of

                    string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=Excel 12.0;";

                    using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                    {
                        conn.Open();
                        using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                        {
                            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            //if (sheetName.Trim() != "LaborCost_IncrementFactorData")
                            //{
                            //    return Json("Incorrect File Format, Please Use 'LaborCost_IncrementFactorData' File!");
                            //}
                            string query = "SELECT * FROM [" + sheetName + "]";
                            OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                            //DataSet ds = new DataSet();
                            adapter.Fill(ds, "Items");
                            if (ds.Tables.Count > 0)
                            {
                                if (ds.Tables[0].Rows.Count > 0)
                                {
                                    var newData = new List<String[]>();
                                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                    {
                                        newData.Add(new[] {
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][0].ToString())?ds.Tables[0].Rows[i][0].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][1].ToString())?ds.Tables[0].Rows[i][1].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][2].ToString())?ds.Tables[0].Rows[i][2].ToString().Replace("&amp;","&"):"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][3].ToString())?ds.Tables[0].Rows[i][3].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][4].ToString())?ds.Tables[0].Rows[i][4].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][5].ToString())?ds.Tables[0].Rows[i][5].ToString():"-"
                                                ,"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][6].ToString())?ds.Tables[0].Rows[i][6].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][7].ToString())?ds.Tables[0].Rows[i][7].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][8].ToString())?ds.Tables[0].Rows[i][8].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][9].ToString())?ds.Tables[0].Rows[i][9].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][10].ToString())?ds.Tables[0].Rows[i][10].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][11].ToString())?ds.Tables[0].Rows[i][11].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][12].ToString())?ds.Tables[0].Rows[i][12].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][13].ToString())?ds.Tables[0].Rows[i][13].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][14].ToString())?ds.Tables[0].Rows[i][14].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][15].ToString())?ds.Tables[0].Rows[i][15].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][16].ToString())?ds.Tables[0].Rows[i][16].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][17].ToString())?ds.Tables[0].Rows[i][17].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][18].ToString())?ds.Tables[0].Rows[i][18].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][19].ToString())?ds.Tables[0].Rows[i][19].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][20].ToString())?ds.Tables[0].Rows[i][20].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][21].ToString())?ds.Tables[0].Rows[i][21].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][22].ToString())?ds.Tables[0].Rows[i][22].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][23].ToString())?ds.Tables[0].Rows[i][23].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][24].ToString())?ds.Tables[0].Rows[i][24].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][25].ToString())?ds.Tables[0].Rows[i][25].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][26].ToString())?ds.Tables[0].Rows[i][26].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][27].ToString())?ds.Tables[0].Rows[i][27].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][28].ToString())?ds.Tables[0].Rows[i][28].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][29].ToString())?ds.Tables[0].Rows[i][29].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][30].ToString())?ds.Tables[0].Rows[i][30].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][31].ToString())?ds.Tables[0].Rows[i][31].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][32].ToString())?ds.Tables[0].Rows[i][32].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][33].ToString())?ds.Tables[0].Rows[i][33].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][34].ToString())?ds.Tables[0].Rows[i][34].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][35].ToString())?ds.Tables[0].Rows[i][35].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][36].ToString())?ds.Tables[0].Rows[i][36].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][37].ToString())?ds.Tables[0].Rows[i][37].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][38].ToString())?ds.Tables[0].Rows[i][38].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][39].ToString())?ds.Tables[0].Rows[i][39].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][40].ToString())?ds.Tables[0].Rows[i][40].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][41].ToString())?ds.Tables[0].Rows[i][41].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][42].ToString())?ds.Tables[0].Rows[i][42].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][43].ToString())?ds.Tables[0].Rows[i][43].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][44].ToString())?ds.Tables[0].Rows[i][44].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][45].ToString())?ds.Tables[0].Rows[i][45].ToString():"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][46].ToString())?ds.Tables[0].Rows[i][46].ToString():"-"
                                                ,"-"
                                                ,!String.IsNullOrEmpty(ds.Tables[0].Rows[i][47].ToString())?ds.Tables[0].Rows[i][47].ToString():""
                                            });
                                    }
                                    SetOvertimePlan(newData, Int32.Parse(Request["iPeriod"]));
                                }
                            }
                        }
                    }
                }
                // Returns message that successfully uploaded  
                return Json("File Uploaded Successfully!");
            }
            else
            {
                return Json("No files selected.");
            }
        }

        public ActionResult generateOTP()
        {
            generateBasicOTP();
            return RedirectToAction("Index", "LaborCost", new { area = "FA" });
        }

        public void generateBasicOTP(String id = null, String Section = null, String Level = null, int Period = 0)
        {
            var currPeriod = Period == 0 ? DateTime.Now.Year : Period;
            var currUser = (ClaimsIdentity)User.Identity;
            var currEmployee = new List<V_Labor_Cost_List>();
            if (!String.IsNullOrEmpty(id))
            {
                if (id.Contains("V"))
                {
                    var vID = id.Substring(0, id.IndexOf('.', id.IndexOf('.') + 1));
                    currEmployee = db.V_Labor_Cost_List.Where(w => w.Period == currPeriod && w.NIK.Contains(vID) && w.DeptName == Section).ToList();
                }
                else
                {
                    currEmployee = db.V_Labor_Cost_List.Where(w => w.Period == currPeriod && w.NIK == id).ToList();
                }
            }
            else
            {
                if (Section != null)
                {
                    currEmployee = db.V_Labor_Cost_List.Where(w => w.Period == currPeriod && w.DeptName == Section && w.Category == "Vacant").ToList();
                }
                else
                {
                    currEmployee = db.V_Labor_Cost_List.Where(w => w.Period == currPeriod).ToList();
                }
            }
            foreach (var item in currEmployee)
            {
                DateTime startDate = DateTime.Parse(item.Date_From.ToString());
                DateTime endDate = DateTime.Parse(item.Date_To.ToString());
                var checkData = new FA_Labor_Cost_OTP();
                var currSection = db.FA_Section.Where(w => (w.Section_Name == item.DeptName || w.Section_Name == Section) && w.Period == item.Period).FirstOrDefault();
                if (item.Category == "Current Employee")
                {
                    checkData = db.FA_Labor_Cost_OTP.Where(w => w.Period == item.Period && w.Related_ID == item.ID && w.Position == item.Position && w.Category == item.Category).FirstOrDefault();
                }
                else
                {
                    checkData = db.FA_Labor_Cost_OTP.Where(w => w.Period == item.Period && w.Related_ID == item.ID && w.Section == item.DeptName && w.Position == item.Position && w.Category != "Current Employee").FirstOrDefault();
                }
                if (item.Category == "Pensiun/Resign")
                {
                    //if (checkData != null)
                    //{
                    //    db.FA_Labor_Cost_OTP.Remove(checkData);
                    //}
                    if (checkData != null)
                    {
                        checkData.M010 = (decimal?)null;
                        checkData.M011 = (decimal?)null;
                        checkData.M012 = (decimal?)null;
                        checkData.M101 = (decimal?)null;
                        checkData.M102 = (decimal?)null;
                        checkData.M103 = (decimal?)null;
                        checkData.M104 = (decimal?)null;
                        checkData.M105 = (decimal?)null;
                        checkData.M106 = (decimal?)null;
                        checkData.M107 = (decimal?)null;
                        checkData.M108 = (decimal?)null;
                        checkData.M109 = (decimal?)null;
                        checkData.M110 = (decimal?)null;
                        checkData.M111 = (decimal?)null;
                        checkData.M112 = (decimal?)null;
                        checkData.M201 = (decimal?)null;
                        checkData.M202 = (decimal?)null;
                        checkData.M203 = (decimal?)null;
                        checkData.M204 = (decimal?)null;
                        checkData.M205 = (decimal?)null;
                        checkData.M206 = (decimal?)null;
                        checkData.M207 = (decimal?)null;
                        checkData.M208 = (decimal?)null;
                        checkData.M209 = (decimal?)null;
                        checkData.M210 = (decimal?)null;
                        checkData.M211 = (decimal?)null;
                        checkData.M212 = (decimal?)null;
                        checkData.M301 = (decimal?)null;
                        checkData.M302 = (decimal?)null;
                        checkData.M303 = (decimal?)null;
                        checkData.M304 = (decimal?)null;
                        checkData.M305 = (decimal?)null;
                        checkData.M306 = (decimal?)null;
                        checkData.M307 = (decimal?)null;
                        checkData.M308 = (decimal?)null;
                        checkData.M309 = (decimal?)null;
                        checkData.M310 = (decimal?)null;
                        checkData.M311 = (decimal?)null;
                        checkData.M312 = (decimal?)null;
                        checkData.M401 = (decimal?)null;
                        checkData.M402 = (decimal?)null;
                        checkData.M403 = (decimal?)null;
                        checkData.Related_ID = item.ID;
                        checkData.Category = item.Category;
                        checkData.User_By = currUser.GetUserId();
                        checkData.Date_At = DateTime.Now;
                        checkData.Sign_By = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_signed : null);
                        checkData.Sign_Date = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Approved_By = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_approval : null);
                        checkData.Approved_Date = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Approved_By2 = (Level == "4" || Level == "5" || Level == "6" ? (currSection.labor_cost_approval2 != null ? currSection.labor_cost_approval2 : currSection.labor_cost_approval) : null);
                        checkData.Approved_Date2 = (Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Approved_By3 = (Level == "5" || Level == "6" ? (currSection.labor_cost_approval3 != null? currSection.labor_cost_approval3: currSection.labor_cost_approval2 != null? currSection.labor_cost_approval2: currSection.labor_cost_approval) : null);
                        checkData.Approved_Date3 = (Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.HCSign_By = (Level == "6" ? "592.02.10" : null);
                        checkData.HCSign_Date = (Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Section = item.DeptName;
                        checkData.Position = item.Position;
                        checkData.Status = item.Status;
                        checkData.Name = item.Name;
                    }
                    else
                    {
                        db.FA_Labor_Cost_OTP.Add(new FA_Labor_Cost_OTP
                        {
                            Period = item.Period,
                            Category = item.Category,
                            OTP_NIK = item.NIK,
                            Name = item.Name,
                            Section = item.DeptName,
                            Position = item.Position,
                            Status = item.Status,
                            M010 = (decimal?)null,
                            M011 = (decimal?)null,
                            M012 = (decimal?)null,
                            M101 = (decimal?)null,
                            M102 = (decimal?)null,
                            M103 = (decimal?)null,
                            M104 = (decimal?)null,
                            M105 = (decimal?)null,
                            M106 = (decimal?)null,
                            M107 = (decimal?)null,
                            M108 = (decimal?)null,
                            M109 = (decimal?)null,
                            M110 = (decimal?)null,
                            M111 = (decimal?)null,
                            M112 = (decimal?)null,
                            M201 = (decimal?)null,
                            M202 = (decimal?)null,
                            M203 = (decimal?)null,
                            M204 = (decimal?)null,
                            M205 = (decimal?)null,
                            M206 = (decimal?)null,
                            M207 = (decimal?)null,
                            M208 = (decimal?)null,
                            M209 = (decimal?)null,
                            M210 = (decimal?)null,
                            M211 = (decimal?)null,
                            M212 = (decimal?)null,
                            M301 = (decimal?)null,
                            M302 = (decimal?)null,
                            M303 = (decimal?)null,
                            M304 = (decimal?)null,
                            M305 = (decimal?)null,
                            M306 = (decimal?)null,
                            M307 = (decimal?)null,
                            M308 = (decimal?)null,
                            M309 = (decimal?)null,
                            M310 = (decimal?)null,
                            M311 = (decimal?)null,
                            M312 = (decimal?)null,
                            M401 = (decimal?)null,
                            M402 = (decimal?)null,
                            M403 = (decimal?)null,
                            Related_ID = item.ID,
                            User_By = currUser.GetUserId(),
                            Date_At = DateTime.Now,
                            Sign_By = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_signed : null),
                            Sign_Date = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            Approved_By = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_approval : null),
                            Approved_Date = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            Approved_By2 = (Level == "4" || Level == "5" || Level == "6" ? (currSection.labor_cost_approval2 != null ? currSection.labor_cost_approval2 : currSection.labor_cost_approval) : null),
                            Approved_Date2 = (Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            Approved_By3 = (Level == "5" || Level == "6" ? (currSection.labor_cost_approval3 != null ? currSection.labor_cost_approval3 : currSection.labor_cost_approval2 != null ? currSection.labor_cost_approval2 : currSection.labor_cost_approval) : null),
                            Approved_Date3 = (Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            HCSign_By = (Level == "6" ? "592.02.10" : null),
                            HCSign_Date = (Level == "6" ? DateTime.Now : (DateTime?)null)
                        });
                    }
                }
                else
                {
                    if (checkData != null)
                    {
                        checkData.M010 = compareDate(new DateTime(item.Period, 10, 1), startDate, endDate) ? (checkData.M010 != null ? checkData.M010 : 0) : (decimal?)null;
                        checkData.M011 = compareDate(new DateTime(item.Period, 11, 1), startDate, endDate) ? (checkData.M011 != null ? checkData.M011 : 0) : (decimal?)null;
                        checkData.M012 = compareDate(new DateTime(item.Period, 12, 1), startDate, endDate) ? (checkData.M012 != null ? checkData.M012 : 0) : (decimal?)null;
                        checkData.M101 = compareDate(new DateTime(item.Period + 1, 1, 1), startDate, endDate) ? (checkData.M101 != null ? checkData.M101 : 0) : (decimal?)null;
                        checkData.M102 = compareDate(new DateTime(item.Period + 1, 2, 1), startDate, endDate) ? (checkData.M102 != null ? checkData.M102 : 0) : (decimal?)null;
                        checkData.M103 = compareDate(new DateTime(item.Period + 1, 3, 1), startDate, endDate) ? (checkData.M103 != null ? checkData.M103 : 0) : (decimal?)null;
                        checkData.M104 = compareDate(new DateTime(item.Period + 1, 4, 1), startDate, endDate) ? (checkData.M104 != null ? checkData.M104 : 0) : (decimal?)null;
                        checkData.M105 = compareDate(new DateTime(item.Period + 1, 5, 1), startDate, endDate) ? (checkData.M105 != null ? checkData.M105 : 0) : (decimal?)null;
                        checkData.M106 = compareDate(new DateTime(item.Period + 1, 6, 1), startDate, endDate) ? (checkData.M106 != null ? checkData.M106 : 0) : (decimal?)null;
                        checkData.M107 = compareDate(new DateTime(item.Period + 1, 7, 1), startDate, endDate) ? (checkData.M107 != null ? checkData.M107 : 0) : (decimal?)null;
                        checkData.M108 = compareDate(new DateTime(item.Period + 1, 8, 1), startDate, endDate) ? (checkData.M108 != null ? checkData.M108 : 0) : (decimal?)null;
                        checkData.M109 = compareDate(new DateTime(item.Period + 1, 9, 1), startDate, endDate) ? (checkData.M109 != null ? checkData.M109 : 0) : (decimal?)null;
                        checkData.M110 = compareDate(new DateTime(item.Period + 1, 10, 1), startDate, endDate) ? (checkData.M110 != null ? checkData.M110 : 0) : (decimal?)null;
                        checkData.M111 = compareDate(new DateTime(item.Period + 1, 11, 1), startDate, endDate) ? (checkData.M111 != null ? checkData.M111 : 0) : (decimal?)null;
                        checkData.M112 = compareDate(new DateTime(item.Period + 1, 12, 1), startDate, endDate) ? (checkData.M112 != null ? checkData.M112 : 0) : (decimal?)null;
                        checkData.M201 = compareDate(new DateTime(item.Period + 2, 1, 1), startDate, endDate) ? (checkData.M201 != null ? checkData.M201 : 0) : (decimal?)null;
                        checkData.M202 = compareDate(new DateTime(item.Period + 2, 2, 1), startDate, endDate) ? (checkData.M202 != null ? checkData.M202 : 0) : (decimal?)null;
                        checkData.M203 = compareDate(new DateTime(item.Period + 2, 3, 1), startDate, endDate) ? (checkData.M203 != null ? checkData.M203 : 0) : (decimal?)null;
                        checkData.M204 = compareDate(new DateTime(item.Period + 2, 4, 1), startDate, endDate) ? (checkData.M204 != null ? checkData.M204 : 0) : (decimal?)null;
                        checkData.M205 = compareDate(new DateTime(item.Period + 2, 5, 1), startDate, endDate) ? (checkData.M205 != null ? checkData.M205 : 0) : (decimal?)null;
                        checkData.M206 = compareDate(new DateTime(item.Period + 2, 6, 1), startDate, endDate) ? (checkData.M206 != null ? checkData.M206 : 0) : (decimal?)null;
                        checkData.M207 = compareDate(new DateTime(item.Period + 2, 7, 1), startDate, endDate) ? (checkData.M207 != null ? checkData.M207 : 0) : (decimal?)null;
                        checkData.M208 = compareDate(new DateTime(item.Period + 2, 8, 1), startDate, endDate) ? (checkData.M208 != null ? checkData.M208 : 0) : (decimal?)null;
                        checkData.M209 = compareDate(new DateTime(item.Period + 2, 9, 1), startDate, endDate) ? (checkData.M209 != null ? checkData.M209 : 0) : (decimal?)null;
                        checkData.M210 = compareDate(new DateTime(item.Period + 2, 10, 1), startDate, endDate) ? (checkData.M210 != null ? checkData.M210 : 0) : (decimal?)null;
                        checkData.M211 = compareDate(new DateTime(item.Period + 2, 11, 1), startDate, endDate) ? (checkData.M211 != null ? checkData.M211 : 0) : (decimal?)null;
                        checkData.M212 = compareDate(new DateTime(item.Period + 2, 12, 1), startDate, endDate) ? (checkData.M212 != null ? checkData.M212 : 0) : (decimal?)null;
                        checkData.M301 = compareDate(new DateTime(item.Period + 3, 1, 1), startDate, endDate) ? (checkData.M301 != null ? checkData.M301 : 0) : (decimal?)null;
                        checkData.M302 = compareDate(new DateTime(item.Period + 3, 2, 1), startDate, endDate) ? (checkData.M302 != null ? checkData.M302 : 0) : (decimal?)null;
                        checkData.M303 = compareDate(new DateTime(item.Period + 3, 3, 1), startDate, endDate) ? (checkData.M303 != null ? checkData.M303 : 0) : (decimal?)null;
                        checkData.M304 = compareDate(new DateTime(item.Period + 3, 4, 1), startDate, endDate) ? (checkData.M304 != null ? checkData.M304 : 0) : (decimal?)null;
                        checkData.M305 = compareDate(new DateTime(item.Period + 3, 5, 1), startDate, endDate) ? (checkData.M305 != null ? checkData.M305 : 0) : (decimal?)null;
                        checkData.M306 = compareDate(new DateTime(item.Period + 3, 6, 1), startDate, endDate) ? (checkData.M306 != null ? checkData.M306 : 0) : (decimal?)null;
                        checkData.M307 = compareDate(new DateTime(item.Period + 3, 7, 1), startDate, endDate) ? (checkData.M307 != null ? checkData.M307 : 0) : (decimal?)null;
                        checkData.M308 = compareDate(new DateTime(item.Period + 3, 8, 1), startDate, endDate) ? (checkData.M308 != null ? checkData.M308 : 0) : (decimal?)null;
                        checkData.M309 = compareDate(new DateTime(item.Period + 3, 9, 1), startDate, endDate) ? (checkData.M309 != null ? checkData.M309 : 0) : (decimal?)null;
                        checkData.M310 = compareDate(new DateTime(item.Period + 3, 10, 1), startDate, endDate) ? (checkData.M310 != null ? checkData.M310 : 0) : (decimal?)null;
                        checkData.M311 = compareDate(new DateTime(item.Period + 3, 11, 1), startDate, endDate) ? (checkData.M311 != null ? checkData.M311 : 0) : (decimal?)null;
                        checkData.M312 = compareDate(new DateTime(item.Period + 3, 12, 1), startDate, endDate) ? (checkData.M312 != null ? checkData.M312 : 0) : (decimal?)null;
                        checkData.M401 = compareDate(new DateTime(item.Period + 4, 1, 1), startDate, endDate) ? (checkData.M401 != null ? checkData.M401 : 0) : (decimal?)null;
                        checkData.M402 = compareDate(new DateTime(item.Period + 4, 2, 1), startDate, endDate) ? (checkData.M402 != null ? checkData.M402 : 0) : (decimal?)null;
                        checkData.M403 = compareDate(new DateTime(item.Period + 4, 3, 1), startDate, endDate) ? (checkData.M403 != null ? checkData.M403 : 0) : (decimal?)null;
                        checkData.Related_ID = item.ID;
                        checkData.Category = item.Category;
                        checkData.User_By = currUser.GetUserId();
                        checkData.Date_At = DateTime.Now;
                        checkData.Sign_By = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_signed : null);
                        checkData.Sign_Date = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Approved_By = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_approval : null);
                        checkData.Approved_Date = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Approved_By2 = (Level == "4" || Level == "5" || Level == "6" ? (currSection.labor_cost_approval2 != null ? currSection.labor_cost_approval2 : currSection.labor_cost_approval) : null);
                        checkData.Approved_Date2 = (Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Approved_By3 = (Level == "5" || Level == "6" ? (currSection.labor_cost_approval3 != null ? currSection.labor_cost_approval3 : currSection.labor_cost_approval2 != null ? currSection.labor_cost_approval2 : currSection.labor_cost_approval) : null);
                        checkData.Approved_Date3 = (Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.HCSign_By = (Level == "6" ? "592.02.10" : null);
                        checkData.HCSign_Date = (Level == "6" ? DateTime.Now : (DateTime?)null);
                        checkData.Section = item.DeptName;
                        checkData.Position = item.Position;
                        checkData.Status = item.Status;
                        checkData.Name = item.Name;
                    }
                    else
                    {
                        db.FA_Labor_Cost_OTP.Add(new FA_Labor_Cost_OTP
                        {
                            Period = item.Period,
                            Category = item.Category,
                            OTP_NIK = item.NIK,
                            Name = item.Name,
                            Section = item.DeptName,
                            Position = item.Position,
                            Status = item.Status,
                            M010 = compareDate(new DateTime(item.Period, 10, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M011 = compareDate(new DateTime(item.Period, 11, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M012 = compareDate(new DateTime(item.Period, 12, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M101 = compareDate(new DateTime(item.Period + 1, 1, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M102 = compareDate(new DateTime(item.Period + 1, 2, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M103 = compareDate(new DateTime(item.Period + 1, 3, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M104 = compareDate(new DateTime(item.Period + 1, 4, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M105 = compareDate(new DateTime(item.Period + 1, 5, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M106 = compareDate(new DateTime(item.Period + 1, 6, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M107 = compareDate(new DateTime(item.Period + 1, 7, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M108 = compareDate(new DateTime(item.Period + 1, 8, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M109 = compareDate(new DateTime(item.Period + 1, 9, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M110 = compareDate(new DateTime(item.Period + 1, 10, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M111 = compareDate(new DateTime(item.Period + 1, 11, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M112 = compareDate(new DateTime(item.Period + 1, 12, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M201 = compareDate(new DateTime(item.Period + 2, 1, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M202 = compareDate(new DateTime(item.Period + 2, 2, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M203 = compareDate(new DateTime(item.Period + 2, 3, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M204 = compareDate(new DateTime(item.Period + 2, 4, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M205 = compareDate(new DateTime(item.Period + 2, 5, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M206 = compareDate(new DateTime(item.Period + 2, 6, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M207 = compareDate(new DateTime(item.Period + 2, 7, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M208 = compareDate(new DateTime(item.Period + 2, 8, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M209 = compareDate(new DateTime(item.Period + 2, 9, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M210 = compareDate(new DateTime(item.Period + 2, 10, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M211 = compareDate(new DateTime(item.Period + 2, 11, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M212 = compareDate(new DateTime(item.Period + 2, 12, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M301 = compareDate(new DateTime(item.Period + 3, 1, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M302 = compareDate(new DateTime(item.Period + 3, 2, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M303 = compareDate(new DateTime(item.Period + 3, 3, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M304 = compareDate(new DateTime(item.Period + 3, 4, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M305 = compareDate(new DateTime(item.Period + 3, 5, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M306 = compareDate(new DateTime(item.Period + 3, 6, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M307 = compareDate(new DateTime(item.Period + 3, 7, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M308 = compareDate(new DateTime(item.Period + 3, 8, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M309 = compareDate(new DateTime(item.Period + 3, 9, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M310 = compareDate(new DateTime(item.Period + 3, 10, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M311 = compareDate(new DateTime(item.Period + 3, 11, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M312 = compareDate(new DateTime(item.Period + 3, 12, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M401 = compareDate(new DateTime(item.Period + 4, 1, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M402 = compareDate(new DateTime(item.Period + 4, 2, 1), startDate, endDate) ? 0 : (decimal?)null,
                            M403 = compareDate(new DateTime(item.Period + 4, 3, 1), startDate, endDate) ? 0 : (decimal?)null,
                            Related_ID = item.ID,
                            User_By = currUser.GetUserId(),
                            Date_At = DateTime.Now,
                            Sign_By = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_signed : null),
                            Sign_Date = (Level == "2" || Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            Approved_By = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? currSection.labor_cost_approval : null),
                            Approved_Date = (Level == "3" || Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            Approved_By2 = (Level == "4" || Level == "5" || Level == "6" ? (currSection.labor_cost_approval2 != null ? currSection.labor_cost_approval2 : currSection.labor_cost_approval) : null),
                            Approved_Date2 = (Level == "4" || Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            Approved_By3 = (Level == "5" || Level == "6" ? (currSection.labor_cost_approval3 != null ? currSection.labor_cost_approval3 : currSection.labor_cost_approval2 != null ? currSection.labor_cost_approval2 : currSection.labor_cost_approval) : null),
                            Approved_Date3 = (Level == "5" || Level == "6" ? DateTime.Now : (DateTime?)null),
                            HCSign_By = (Level == "6" ? "592.02.10" : null),
                            HCSign_Date = (Level == "6" ? DateTime.Now : (DateTime?)null)
                        });
                    }
                }
                db.SaveChanges();
            }
        }

        [HttpGet]
        public JsonResult GetWorkingDay()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            Int32 period = (Request["iLCWorkingDayPeriod"] != null ? Int32.Parse(Request["iLCWorkingDayPeriod"]) : DateTime.Now.Year);
            ViewBag.Period = period;
            var checkData = db.FA_Labor_Cost_Working_Day.Where(w => w.Period == period).ToList();
            if (checkData.Count() == 0)
            {
                var insertData = new FA_Labor_Cost_Working_Day();
                insertData.Period = period;
                insertData.Category = "Working Day";
                insertData.User_By = currUser.GetUserId();
                insertData.Date_At = DateTime.Now;
                db.FA_Labor_Cost_Working_Day.Add(insertData);
                db.SaveChanges();
                insertData = new FA_Labor_Cost_Working_Day();
                insertData.Period = period;
                insertData.Category = "2S3G";
                insertData.User_By = currUser.GetUserId();
                insertData.Date_At = DateTime.Now;
                db.FA_Labor_Cost_Working_Day.Add(insertData);
                db.SaveChanges();
                insertData = new FA_Labor_Cost_Working_Day();
                insertData.Period = period;
                insertData.Category = "Fasting";
                insertData.User_By = currUser.GetUserId();
                insertData.Date_At = DateTime.Now;
                db.FA_Labor_Cost_Working_Day.Add(insertData);
                db.SaveChanges();
            }

            var getData = db.FA_Labor_Cost_Working_Day.Where(w => w.Period == period).Select(s =>
          new
          {
              s.Category,
              s.M010,
              s.M011,
              s.M012,
              s.M101,
              s.M102,
              s.M103,
              s.M104,
              s.M105,
              s.M106,
              s.M107,
              s.M108,
              s.M109,
              s.M110,
              s.M111,
              s.M112,
              s.M201,
              s.M202,
              s.M203,
              s.M204,
              s.M205,
              s.M206,
              s.M207,
              s.M208,
              s.M209,
              s.M210,
              s.M211,
              s.M212,
              s.M301,
              s.M302,
              s.M303,
              s.M304,
              s.M305,
              s.M306,
              s.M307,
              s.M308,
              s.M309,
              s.M310,
              s.M311,
              s.M312,
              s.M401,
              s.M402,
              s.M403
          }
             ).ToList();
            return Json(JsonConvert.SerializeObject(GetObjectArray(getData)), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SetWorkingDay(List<String[]> dataList)
        {
            Int32 periode = (Request["iLCWorkingDayPeriod"] != null ? Int32.Parse(Request["iLCWorkingDayPeriod"]) : DateTime.Now.Year);
            var currUser = (ClaimsIdentity)User.Identity;
            foreach (var i in dataList)
            {
                var period = periode;
                var category = i[0];
                var m010 = !String.IsNullOrEmpty(i[1]) ? Int32.Parse(i[1].Replace(".", "")) : (int?)null;
                var m011 = !String.IsNullOrEmpty(i[2]) ? Int32.Parse(i[2].Replace(".", "")) : (int?)null;
                var m012 = !String.IsNullOrEmpty(i[3]) ? Int32.Parse(i[3].Replace(".", "")) : (int?)null;
                var m101 = !String.IsNullOrEmpty(i[4]) ? Int32.Parse(i[4].Replace(".", "")) : (int?)null;
                var m102 = !String.IsNullOrEmpty(i[5]) ? Int32.Parse(i[5].Replace(".", "")) : (int?)null;
                var m103 = !String.IsNullOrEmpty(i[6]) ? Int32.Parse(i[6].Replace(".", "")) : (int?)null;
                var m104 = !String.IsNullOrEmpty(i[7]) ? Int32.Parse(i[7].Replace(".", "")) : (int?)null;
                var m105 = !String.IsNullOrEmpty(i[8]) ? Int32.Parse(i[8].Replace(".", "")) : (int?)null;
                var m106 = !String.IsNullOrEmpty(i[9]) ? Int32.Parse(i[9].Replace(".", "")) : (int?)null;
                var m107 = !String.IsNullOrEmpty(i[10]) ? Int32.Parse(i[10].Replace(".", "")) : (int?)null;
                var m108 = !String.IsNullOrEmpty(i[11]) ? Int32.Parse(i[11].Replace(".", "")) : (int?)null;
                var m109 = !String.IsNullOrEmpty(i[12]) ? Int32.Parse(i[12].Replace(".", "")) : (int?)null;
                var m110 = !String.IsNullOrEmpty(i[13]) ? Int32.Parse(i[13].Replace(".", "")) : (int?)null;
                var m111 = !String.IsNullOrEmpty(i[14]) ? Int32.Parse(i[14].Replace(".", "")) : (int?)null;
                var m112 = !String.IsNullOrEmpty(i[15]) ? Int32.Parse(i[15].Replace(".", "")) : (int?)null;
                var m201 = !String.IsNullOrEmpty(i[16]) ? Int32.Parse(i[16].Replace(".", "")) : (int?)null;
                var m202 = !String.IsNullOrEmpty(i[17]) ? Int32.Parse(i[17].Replace(".", "")) : (int?)null;
                var m203 = !String.IsNullOrEmpty(i[18]) ? Int32.Parse(i[18].Replace(".", "")) : (int?)null;
                var m204 = !String.IsNullOrEmpty(i[19]) ? Int32.Parse(i[19].Replace(".", "")) : (int?)null;
                var m205 = !String.IsNullOrEmpty(i[20]) ? Int32.Parse(i[20].Replace(".", "")) : (int?)null;
                var m206 = !String.IsNullOrEmpty(i[21]) ? Int32.Parse(i[21].Replace(".", "")) : (int?)null;
                var m207 = !String.IsNullOrEmpty(i[22]) ? Int32.Parse(i[22].Replace(".", "")) : (int?)null;
                var m208 = !String.IsNullOrEmpty(i[23]) ? Int32.Parse(i[23].Replace(".", "")) : (int?)null;
                var m209 = !String.IsNullOrEmpty(i[24]) ? Int32.Parse(i[24].Replace(".", "")) : (int?)null;
                var m210 = !String.IsNullOrEmpty(i[25]) ? Int32.Parse(i[25].Replace(".", "")) : (int?)null;
                var m211 = !String.IsNullOrEmpty(i[26]) ? Int32.Parse(i[26].Replace(".", "")) : (int?)null;
                var m212 = !String.IsNullOrEmpty(i[27]) ? Int32.Parse(i[27].Replace(".", "")) : (int?)null;
                var m301 = !String.IsNullOrEmpty(i[28]) ? Int32.Parse(i[28].Replace(".", "")) : (int?)null;
                var m302 = !String.IsNullOrEmpty(i[29]) ? Int32.Parse(i[29].Replace(".", "")) : (int?)null;
                var m303 = !String.IsNullOrEmpty(i[30]) ? Int32.Parse(i[30].Replace(".", "")) : (int?)null;
                var m304 = !String.IsNullOrEmpty(i[31]) ? Int32.Parse(i[31].Replace(".", "")) : (int?)null;
                var m305 = !String.IsNullOrEmpty(i[32]) ? Int32.Parse(i[32].Replace(".", "")) : (int?)null;
                var m306 = !String.IsNullOrEmpty(i[33]) ? Int32.Parse(i[33].Replace(".", "")) : (int?)null;
                var m307 = !String.IsNullOrEmpty(i[34]) ? Int32.Parse(i[34].Replace(".", "")) : (int?)null;
                var m308 = !String.IsNullOrEmpty(i[35]) ? Int32.Parse(i[35].Replace(".", "")) : (int?)null;
                var m309 = !String.IsNullOrEmpty(i[36]) ? Int32.Parse(i[36].Replace(".", "")) : (int?)null;
                var m310 = !String.IsNullOrEmpty(i[37]) ? Int32.Parse(i[37].Replace(".", "")) : (int?)null;
                var m311 = !String.IsNullOrEmpty(i[38]) ? Int32.Parse(i[38].Replace(".", "")) : (int?)null;
                var m312 = !String.IsNullOrEmpty(i[39]) ? Int32.Parse(i[39].Replace(".", "")) : (int?)null;
                var m401 = !String.IsNullOrEmpty(i[40]) ? Int32.Parse(i[40].Replace(".", "")) : (int?)null;
                var m402 = !String.IsNullOrEmpty(i[41]) ? Int32.Parse(i[41].Replace(".", "")) : (int?)null;
                var m403 = !String.IsNullOrEmpty(i[42]) ? Int32.Parse(i[42].Replace(".", "")) : (int?)null;

                var checkData = db.FA_Labor_Cost_Working_Day.Where(w => w.Period == period && w.Category == category).FirstOrDefault();
                if (checkData != null)
                {
                    checkData.Period = period;
                    checkData.Category = category;
                    checkData.M010 = m010;
                    checkData.M011 = m011;
                    checkData.M012 = m012;
                    checkData.M101 = m101;
                    checkData.M102 = m102;
                    checkData.M103 = m103;
                    checkData.M104 = m104;
                    checkData.M105 = m105;
                    checkData.M106 = m106;
                    checkData.M107 = m107;
                    checkData.M108 = m108;
                    checkData.M109 = m109;
                    checkData.M110 = m110;
                    checkData.M111 = m111;
                    checkData.M112 = m112;
                    checkData.M201 = m201;
                    checkData.M202 = m202;
                    checkData.M203 = m203;
                    checkData.M204 = m204;
                    checkData.M205 = m205;
                    checkData.M206 = m206;
                    checkData.M207 = m207;
                    checkData.M208 = m208;
                    checkData.M209 = m209;
                    checkData.M210 = m210;
                    checkData.M211 = m211;
                    checkData.M212 = m212;
                    checkData.M301 = m301;
                    checkData.M302 = m302;
                    checkData.M303 = m303;
                    checkData.M304 = m304;
                    checkData.M305 = m305;
                    checkData.M306 = m306;
                    checkData.M307 = m307;
                    checkData.M308 = m308;
                    checkData.M309 = m309;
                    checkData.M310 = m310;
                    checkData.M311 = m311;
                    checkData.M312 = m312;
                    checkData.M401 = m401;
                    checkData.M402 = m402;
                    checkData.M403 = m403;
                    checkData.User_By = currUser.GetUserId();
                    checkData.Date_At = DateTime.Now;
                }
            }
            db.SaveChanges();

            return Json(true);
        }

        public Boolean compareDate(DateTime dt, DateTime startDate, DateTime endDate)
        {
            if (DateTime.Compare(dt, startDate) < 0 || DateTime.Compare(dt, endDate) > 0)
            {
                return false;
            }

            return true;
        }
    }
}