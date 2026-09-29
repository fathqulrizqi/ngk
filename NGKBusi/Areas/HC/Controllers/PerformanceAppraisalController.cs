using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Globalization;


namespace NGKBusi.Areas.HC.Controllers
{
    [Authorize]
    public class PerformanceAppraisalController : Controller
    {
        PerformanceAppraisalConnection dbPA = new PerformanceAppraisalConnection();
        DefaultConnection db = new DefaultConnection();
        // GET: HC/PerformanceAppraisal
        public ActionResult Index()
        {
            return View();
        }
        public class ApprovalStats
        {
            public string Stat { get; set; }
            public string Badge { get; set; }
        }
        public ApprovalStats ApprovalStatus(int Approval, int Approval_Sub)
        {
            ApprovalStats stats = new ApprovalStats();
            stats.Stat = "Submitted";
            stats.Badge = "info";
            switch (Approval)
            {
                case 1:
                    switch (Approval_Sub)
                    {
                        case 1:
                            stats.Stat = "Submitted";
                            stats.Badge = "warning";
                            break;
                        case 2:
                            stats.Stat = "Dept-Checked";
                            break;
                        default:
                            stats.Stat = "Not-Submitted";
                            stats.Badge = "danger";
                            break;
                    }
                    break;
                case 2:
                    switch (Approval_Sub)
                    {
                        case 1:
                            stats.Stat = "Reviewed";
                            break;
                        default:
                            stats.Stat = "Dept-Approved";
                            stats.Badge = "success";
                            break;
                    }
                    break;
                case 3:
                    stats.Stat = "Approved";
                    stats.Badge = "success";
                    break;
                case 4:
                    stats.Stat = "Finalized";
                    stats.Badge = "success";
                    break;
                default:
                    stats.Stat = "Returned";
                    stats.Badge = "warning";
                    break;
            }

            return stats;
        }

        public ActionResult Form()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData?.DivisionName;
            var currDeptName = currUserData?.DeptName;
            var currSectName = currUserData?.SectionName;

            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iPAPeriod"]) ? Request["iPAPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 10 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            ViewBag.Period = periodFY;
            var period = Request["iPAPeriod"] != null ? int.Parse(Request["iPAPeriod"]) : DateTime.Now.Year;
            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            if (currUserID == "822.01.19")
            {
                subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking <= currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            }
            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            var DataList = dbPA.HC_Performance_Appraisal
                .Include("EmployeeBy")
                .Include("DirectBy")
                .Include("IndirectBy")
                .Where(w => w.Period_FY == periodFY && (w.EmployeeBy.Users_Position.Position_Ranking < currUserData.Users_Position.Position_Ranking || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
                //.Where(w => w.Period_FY == periodFY && (subOrdinate.Contains(w.PostName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            var MemberList = db.V_Users_Active.Where(w => (subOrdinate.Contains(w.PositionName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            //string[] position0 = { "FOREMAN" };
            //string[] position1 = { "SUPERVISOR" };
            //string[] position2 = { "ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "MANAGER (ACTING)" };
            //string[] position3 = { "SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            //string[] position4 = { "BOD" };
            string[] position0 = { "FOREMAN", "3A - TEAM LEADER", "3B - TEAM LEADER" };
            string[] position1 = { "SUPERVISOR", "5A - SUPERVISOR", "5B - SUPERVISOR", "5C - SUPERVISOR" };
            string[] position2 = { "6A - ASSISTANT MANAGER", "6B - ASSISTANT MANAGER", "6C - ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "7A - MANAGER", "7B - MANAGER", "MANAGER (ACTING)" };
            string[] position3 = { "SENIOR MANAGER", "8A - SENIOR MANAGER", "8B - SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "9A - DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            string[] position4 = { "BOD" };
            if (position0.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "763.11.16" && periodFY != "FY123")
                {
                    DataList = DataList.Where(w => w.SectionName == currSectName);
                    MemberList = MemberList.Where(w => w.SectionName == currSectName);
                }
                else
                {
                    DataList = DataList.Where(w => w.NIK == currUserID);
                    MemberList = MemberList.Where(w => w.NIK == currUserID);
                }
            }
            else if (position1.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "676.05.14")
                {
                    DataList = DataList.Where(w => w.SectionName == "PACKAGING & OEM" || w.SectionName == currSectName);
                    MemberList = MemberList.Where(w => w.SectionName == "PACKAGING & OEM" || w.SectionName == currSectName);
                }
                else if (currUserID == "864.12.22")
                {
                    DataList = DataList.Where(w => w.DeptName == "PC PD - PLUG CAP" || w.SectionName == currSectName);
                    MemberList = MemberList.Where(w => w.DeptName == "PC PD - PLUG CAP" || w.SectionName == currSectName);
                }
                else
                {
                    DataList = DataList.Where(w => w.SectionName == currSectName);
                    MemberList = MemberList.Where(w => w.SectionName == currSectName);
                }
            }
            else if (position2.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "664.08.13")
                {

                    DataList = DataList.Where(w => w.DeptName == "SP PD - METAL SHELL & INSULATOR" || (w.DeptName == currDeptName && w.NIK == "664.08.13"));
                    MemberList = MemberList.Where(w => w.DeptName == "SP PD - METAL SHELL & INSULATOR" || (w.DeptName == currDeptName && w.NIK == "664.08.13"));
                }
                else if (currUserID == "719.04.15")
                {
                    DataList = DataList.Where(w => w.SectionName == "INDIRECT PROCUREMENT" || w.SectionName == "DIRECT PROCUREMENT" || (w.DeptName == currDeptName));
                    MemberList = MemberList.Where(w => w.SectionName == "INDIRECT PROCUREMENT" || w.SectionName == "DIRECT PROCUREMENT" || (w.DeptName == currDeptName));
                }
                else if (currUserID == "685.05.14")
                {
                    DataList = DataList.Where(w => w.DeptName == "SP PD - SPARK PLUG" || (w.DeptName == currDeptName && w.NIK == "685.05.14"));
                    MemberList = MemberList.Where(w => w.DeptName == "SP PD - SPARK PLUG" || (w.DeptName == currDeptName && w.NIK == "685.05.14"));
                }
                else if (currUserID == "793.03.18")
                {
                    DataList = DataList.Where(w => w.DeptName == "QUALITY" || w.DeptName == currDeptName);
                    MemberList = MemberList.Where(w => w.DeptName == "QUALITY" || w.DeptName == currDeptName);
                }
                else if (currUserID == "822.01.19")
                {
                    DataList = DataList.Where(w => w.SectionName != "SALES ADMIN" && w.DeptName == currDeptName);
                    MemberList = MemberList.Where(w => w.SectionName != "SALES ADMIN" && w.DeptName == currDeptName);
                }
                else if (currUserID == "589.10.09")
                {
                    DataList = DataList.Where(w => w.SectionName == "SALES ADMIN" || w.DeptName == currDeptName);
                    MemberList = MemberList.Where(w => w.SectionName == "SALES ADMIN" || w.DeptName == currDeptName);
                }
                else if (currUserID == "814.01.19")
                {
                    DataList = DataList.Where(w => w.DeptName == currDeptName || w.NIK == "822.01.19");
                    MemberList = MemberList.Where(w => w.DeptName == currDeptName || w.NIK == "822.01.19");
                }
                else
                {
                    DataList = DataList.Where(w => w.DeptName == currDeptName);
                    MemberList = MemberList.Where(w => w.DeptName == currDeptName);
                }
            }
            else if (position3.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "851.02.21")
                {
                    DataList = DataList.Where(w => (w.DivName == currDivName || w.DeptName == "SALES - OEM & OES") || (w.NIK == "663.01.14" || w.NIK == "665.11.13"));
                    MemberList = MemberList.Where(w => (w.DivisionName == currDivName || w.DeptName == "SALES - OEM & OES") || (w.NIK == "663.01.14" || w.NIK == "665.11.13"));
                }
                else if (currUserID == "546.08.05")
                {
                    DataList = DataList.Where(w => w.DivName == currDivName || w.DivName == "SUPPLY CHAIN MANAGEMENT");
                    MemberList = MemberList.Where(w => w.DivisionName == currDivName || w.DivisionName == "SUPPLY CHAIN MANAGEMENT");
                }
                else if (currUserID == "618.04.12")
                {
                    DataList = DataList.Where(w => w.DivName == "PROD. ENGINEERING & MAINTENANCE" || w.DivName == "PRODUCTION");
                    MemberList = MemberList.Where(w => w.DivisionName == "PROD. ENGINEERING & MAINTENANCE" || w.DivisionName == "PRODUCTION");
                }
                else if (currUserID == "EXP.014")
                {
                    DataList = DataList.Where(w => w.DivName == "PROD. ENGINEERING & MAINTENANCE" || w.DivName == "PRODUCTION" || w.DivName == "QUALITY MANAGEMENT");
                    MemberList = MemberList.Where(w => w.DivisionName == "PROD. ENGINEERING & MAINTENANCE" || w.DivisionName == "PRODUCTION" || w.DivisionName == "QUALITY MANAGEMENT");
                }
                else if (currUserID == "EXP.015")
                {
                    DataList = DataList.Where(w => w.DivName == "SALES & MARKETING");
                    MemberList = MemberList.Where(w => w.DivisionName == "SALES & MARKETING");
                }
                else if (currUserID == "EXP.011")
                {
                    DataList = DataList.Where(w => w.DivName == "ACCOUNTING & IT");
                    MemberList = MemberList.Where(w => w.DivisionName == "ACCOUNTING & IT");
                }
                else
                {
                    DataList = DataList.Where(w => w.DivName == currDivName);
                    MemberList = MemberList.Where(w => w.DivisionName == currDivName);
                }
            }
            else if (position4.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "EXP.014")
                {
                    DataList = DataList.Where(w => w.DivName == "PROD. ENGINEERING & MAINTENANCE" || w.DivName == "PRODUCTION" || w.DivName == "QUALITY MANAGEMENT");
                    MemberList = MemberList.Where(w => w.DivisionName == "PROD. ENGINEERING & MAINTENANCE" || w.DivisionName == "PRODUCTION" || w.DivisionName == "QUALITY MANAGEMENT");
                }
                else if (currUserID == "EXP.015")
                {
                    DataList = DataList.Where(w => w.DivName == "SALES & MARKETING");
                    MemberList = MemberList.Where(w => w.DivisionName == "SALES & MARKETING");
                }
                else if (currUserID == "EXP.011")
                {
                    DataList = DataList.Where(w => w.DivName == "ACCOUNTING & IT");
                    MemberList = MemberList.Where(w => w.DivisionName == "ACCOUNTING & IT");
                }
                else if (currUserID == "EXP.013")
                {
                    //AllDiv No need to place where clause
                }
                else
                {
                    DataList = DataList.Where(w => w.DivName == currDivName);
                    MemberList = MemberList.Where(w => w.DivisionName == currDivName);
                }
            }
            else
            {
                DataList = DataList.Where(w => w.NIK == currUserID);
                MemberList = MemberList.Where(w => w.NIK == currUserID);
            }
            var DataNIK = DataList.Select(s => s.NIK).ToArray();
            MemberList = MemberList.Where(w => !DataNIK.Contains(w.NIK));
            ViewBag.DataList = DataList.OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberList = MemberList.OrderByDescending(o => o.DivisionName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.NavHide = true;
            return View();
        }


        public ActionResult FormAll()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData?.DivisionName;
            var currDeptName = currUserData?.DeptName;
            var currSectName = currUserData?.SectionName;
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 101
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iPAPeriod"]) ? Request["iPAPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 10 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            ViewBag.Period = periodFY;
            var period = Request["iPAPeriod"] != null ? int.Parse(Request["iPAPeriod"]) : DateTime.Now.Year;
            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            var DataList = dbPA.HC_Performance_Appraisal
                .Include("EmployeeBy")
                .Include("DirectBy")
                .Include("IndirectBy")
                .Where(w => w.Period_FY == periodFY && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            var MemberList = db.V_Users_Active.Where(w => w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");

            var DataNIK = DataList.Select(s => s.NIK).ToArray();
            MemberList = MemberList.Where(w => !DataNIK.Contains(w.NIK));
            ViewBag.DataList = DataList.OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberList = MemberList.OrderByDescending(o => o.DivisionName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.NavHide = true;
            return View();
        }
        public ActionResult PAAdd()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currUserDiv = currUser.FindFirstValue("DivName");
            var currUserDept = currUser.FindFirstValue("DeptName");
            var currUserSect = currUser.FindFirstValue("SectName");
            var currUserPost = currUser.FindFirstValue("PostName");
            var currUserCostID = currUser.FindFirstValue("CostID");
            var currUserCostName = currUser.FindFirstValue("CostName");

            var newData = new HC_Performance_Appraisal();
            newData.GUID = Guid.NewGuid().ToString();
            newData.Period_FY = Request["iPAPeriodFY"];
            newData.Period_Year = int.Parse(Request["iPAPeriodYear"]);
            newData.NIK = Request["iPANIK"];
            newData.Name = Request["iPAName"];
            newData.DivName = currUserDiv;
            newData.DeptName = Request["iPADept"];
            newData.SectionName = Request["iPASect"];
            newData.PostName = currUserPost;
            newData.CostID = currUserCostID;
            newData.CostName = currUserCostName;
            newData.Performance_KPI_Self = decimal.Parse(Request["iSelfScoreKPI"]);
            //newData.Performance_KPI = decimal.Parse(Request["iScoreKPI"]);
            newData.Integrity_Discipline_Self = int.Parse(Request["iSelfScoreDiscipline"]);
            //newData.Integrity_Discipline = decimal.Parse(Request["iScoreDiscipline"]);
            newData.Integrity_Participation_Self = int.Parse(Request["iSelfScoreParticipation"]);
            //newData.Integrity_Participation = decimal.Parse(Request["iScoreParticipation"]);
            newData.Integrity_Participation_Comittee = Request["iScoreComittee[]"];
            newData.Integrity_Participation_Participant = Request["iScoreParticipant[]"];
            newData.Integrity_Participation_Remark = Request["iRemarkIntegrity"];
            newData.Competency_Knowledge_Self = int.Parse(Request["iSelfScoreKnowledge"]);
            newData.Competency_Skill_Self = int.Parse(Request["iSelfScoreSkill"]);
            //newData.Competency_Skill = decimal.Parse(Request["iScoreSkill"]);
            newData.Competency_Behaviour_Self = int.Parse(Request["iSelfScoreBehaviour"]);
            //newData.Competency_Behaviour = decimal.Parse(Request["iScoreBehaviour"]);
            //newData.Note_Strenght_Indirect = Request["iStrenghtIndirect"];
            //newData.Note_Strenght_Direct = Request["iStrenghtDirect"];
            newData.Note_Strenght_Self = Request["iStrenghtSelf"];
            //newData.Note_Weakness_Indirect = Request["iWeaknessIndirect"];
            //newData.Note_Weakness_Direct = Request["iWeaknessDirect"];
            newData.Note_Weakness_Self = Request["iWeaknessSelf"];
            //newData.Note_Planning_Indirect = Request["iPlanningIndirect"];
            //newData.Note_Planning_Direct = Request["iPlanningDirect"];
            newData.Note_Planning_Self = Request["iPlanningSelf"];
            newData.IsSaved = true;
            newData.IsReleased = false;
            newData.IsWarning = 0;
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            dbPA.HC_Performance_Appraisal.Add(newData);
            dbPA.SaveChanges();

            return RedirectToAction("form", "PerformanceAppraisal", new { area = "HC", GUID = newData.GUID });
        }

        public ActionResult PAEdit()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currUserDept = currUser.FindFirstValue("DeptName");
            var currUserSect = currUser.FindFirstValue("SectName");
            var currFormID = int.Parse(Request["iPAID"]);

            var newData = dbPA.HC_Performance_Appraisal.Where(w => w.ID == currFormID).FirstOrDefault();
            // Log the raw value received from the form
            System.Diagnostics.Debug.WriteLine("iSelfScoreKPI (raw): " + Request["iSelfScoreKPI"]);
            newData.NIK = Request["iPANIK"];
            newData.Name = Request["iPAName"];
            newData.DeptName = Request["iPADept"];
            newData.SectionName = Request["iPASect"]; 
            newData.Performance_KPI_Self = decimal.Parse(Request["iSelfScoreKPI"], CultureInfo.GetCultureInfo("en-US"));
            newData.Integrity_Discipline_Self = int.Parse(Request["iSelfScoreDiscipline"]);
            newData.Integrity_Participation_Self = int.Parse(Request["iSelfScoreParticipation"]);
            newData.Integrity_Participation_Comittee = Request["iScoreComittee[]"];
            newData.Integrity_Participation_Participant = Request["iScoreParticipant[]"];
            newData.Integrity_Participation_Remark = Request["iRemarkIntegrity"];
            newData.Competency_Knowledge_Self = int.Parse(Request["iSelfScoreKnowledge"]);
            newData.Competency_Skill_Self = int.Parse(Request["iSelfScoreSkill"]);
            newData.Competency_Behaviour_Self = int.Parse(Request["iSelfScoreBehaviour"]);
            if (newData.Approval == 1 && newData.Approval_Sub == 1)
            {
                newData.Performance_KPI_Direct = decimal.Parse(Request["iScoreKPIDirect"]);
                newData.Integrity_Discipline_Direct = decimal.Parse(Request["iScoreDisciplineDirect"]);
                newData.Integrity_Participation_Direct = decimal.Parse(Request["iScoreParticipationDirect"]);
                newData.Competency_Knowledge_Direct = decimal.Parse(Request["iScoreKnowledgeDirect"]);
                newData.Competency_Skill_Direct = decimal.Parse(Request["iScoreSkillDirect"]);
                newData.Competency_Behaviour_Direct = decimal.Parse(Request["iScoreBehaviourDirect"]);
            }
            if (newData.Approval == 1 && newData.Approval_Sub > 1)
            {
                newData.Performance_KPI = decimal.Parse(Request["iScoreKPI"]);
                newData.Integrity_Discipline = decimal.Parse(Request["iScoreDiscipline"]);
                newData.Integrity_Participation = decimal.Parse(Request["iScoreParticipation"]);
                newData.Competency_Knowledge = decimal.Parse(Request["iScoreKnowledge"]);
                newData.Competency_Skill = decimal.Parse(Request["iScoreSkill"]);
                newData.Competency_Behaviour = decimal.Parse(Request["iScoreBehaviour"]);
            }
            newData.Note_Strenght_Indirect = Request["iStrenghtIndirect"];
            newData.Note_Strenght_Direct = Request["iStrenghtDirect"];
            newData.Note_Strenght_Self = Request["iStrenghtSelf"];
            newData.Note_Weakness_Indirect = Request["iWeaknessIndirect"];
            newData.Note_Weakness_Direct = Request["iWeaknessDirect"];
            newData.Note_Weakness_Self = Request["iWeaknessSelf"];
            newData.Note_Planning_Indirect = Request["iPlanningIndirect"];
            newData.Note_Planning_Direct = Request["iPlanningDirect"];
            newData.Note_Planning_Self = Request["iPlanningSelf"];
            newData.IsSaved = true;

            dbPA.SaveChanges();
            System.Diagnostics.Debug.WriteLine("iSelfScoreKPI (Saved): " + newData.Performance_KPI_Self);

            //return RedirectToAction("form", "PerformanceAppraisal", new { area = "HC", GUID = newData.GUID });
            return Redirect(Request.UrlReferrer.ToString());
        }
        public ActionResult PAEditSummary()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currUserDept = currUser.FindFirstValue("DeptName");
            var currUserSect = currUser.FindFirstValue("SectName");
            var currFormID = int.Parse(Request["iPAID"]);

            var newData = dbPA.HC_Performance_Appraisal.Where(w => w.ID == currFormID).FirstOrDefault();
            newData.Performance_KPI = decimal.Parse(Request["iScoreKPI"]);
            newData.Integrity_Discipline = decimal.Parse(Request["iScoreDiscipline"]);
            newData.Integrity_Participation = decimal.Parse(Request["iScoreParticipation"]);
            newData.Competency_Knowledge = decimal.Parse(Request["iScoreKnowledge"]);
            newData.Competency_Skill = decimal.Parse(Request["iScoreSkill"]);
            newData.Competency_Behaviour = decimal.Parse(Request["iScoreBehaviour"]);

            dbPA.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult PAEditSummaryHC()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currUserDept = currUser.FindFirstValue("DeptName");
            var currUserSect = currUser.FindFirstValue("SectName");
            var currFormID = int.Parse(Request["iPAID"]);

            var newData = dbPA.HC_Performance_Appraisal.Where(w => w.ID == currFormID).FirstOrDefault();
            newData.Performance_KPI = decimal.Parse(Request["iScoreKPI"]);
            newData.Integrity_Discipline = decimal.Parse(Request["iScoreDiscipline"]);
            newData.Integrity_Participation = decimal.Parse(Request["iScoreParticipation"]);
            newData.Competency_Knowledge = decimal.Parse(Request["iScoreKnowledge"]);
            newData.Competency_Skill = decimal.Parse(Request["iScoreSkill"]);
            newData.Competency_Behaviour = decimal.Parse(Request["iScoreBehaviour"]);

            dbPA.SaveChanges();

            return RedirectToAction("SummaryHC", "PerformanceAppraisal", new { area = "HC" });
        }

        public ActionResult Summary()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData.DivisionName;
            var currDeptName = currUserData.DeptName;
            var currSectName = currUserData.SectionName;
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 95
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            var period = Request["iPAPeriod"] != null ? int.Parse(Request["iPAPeriod"]) : DateTime.Now.Year;
            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iPAPeriod"]) ? Request["iPAPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 10 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            var position = String.IsNullOrEmpty(Request["iPAPosition"]) ? "1-2-3-4-5-6-7-8-9-10-11-12-13-14" : Request["iPAPosition"];
            ViewBag.Period = periodFY;
            ViewBag.PeriodYear = period;
            ViewBag.ApprovalPost = Request["iApprovalPost"] != null ? Request["iApprovalPost"] : "Reviewer";
            var positionList = position.Split('-').Select(long.Parse).ToList();

            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking && positionList.Contains(w.Position_Ranking)).Select(s => s.Position_Name).ToArray();
            if (currUserID == "822.01.19")
            {
                subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking <= currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            }
            if(ViewBag.PeriodYear < 2026)
            {
                subOrdinate = db.Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            }

            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            var DataList = dbPA.HC_Performance_Appraisal
                .Include("EmployeeBy")
                .Include("DirectBy")
                .Include("IndirectBy")
                .Where(w => w.Period_FY == periodFY && w.Approval >= 2 && (subOrdinate.Contains(w.PostName) && w.NIK != currUserID) && w.NIK.Substring(0, 1) != "P");
            //string[] position0 = { "FOREMAN" };
            //string[] position1 = { "SUPERVISOR" };
            //string[] position2 = { "ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "MANAGER (ACTING)" };
            //string[] position3 = { "SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            //string[] position4 = { "BOD" };
            string[] position0 = { "FOREMAN", "3A - TEAM LEADER", "3B - TEAM LEADER"};
            string[] position1 = { "SUPERVISOR","5A - SUPERVISOR", "5B - SUPERVISOR", "5C - SUPERVISOR" };
            string[] position2 = { "6A - ASSISTANT MANAGER", "6B - ASSISTANT MANAGER", "6C - ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "7A - MANAGER", "7B - MANAGER", "MANAGER (ACTING)" };
            string[] position3 = { "SENIOR MANAGER", "8A - SENIOR MANAGER", "8B - SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "9A - DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            string[] position4 = { "BOD" };

            if (position0.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "763.11.16" && periodFY != "FY123")
                {
                    DataList = DataList.Where(w => w.SectionName == currSectName);
                }
                else
                {
                    DataList = DataList.Where(w => w.NIK == currUserID);
                }
            }
            else if (position1.Contains(currUserData.Users_Position.Position_Name))
            {
                DataList = DataList.Where(w => w.SectionName == currSectName);
            }
            else if (position2.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "664.08.13")
                {
                    DataList = DataList.Where(w => w.DeptName == "SP PD - METAL SHELL & INSULATOR" || (w.DeptName == currDeptName && w.NIK == "664.08.13"));
                }
                else if (currUserID == "719.04.15")
                {
                    DataList = DataList.Where(w => w.SectionName == "INDIRECT PROCUREMENT" || w.SectionName == "DIRECT PROCUREMENT" || (w.DeptName == currDeptName));
                }
                else if (currUserID == "685.05.14")
                {
                    DataList = DataList.Where(w => w.DeptName == "SP PD - SPARK PLUG" || (w.DeptName == currDeptName && w.NIK == "685.05.14"));
                }
                else if (currUserID == "793.03.18")
                {
                    DataList = DataList.Where(w => w.DeptName == "QUALITY" || w.DeptName == currDeptName);
                }
                else if (currUserID == "822.01.19")
                {
                    DataList = DataList.Where(w => w.SectionName != "SALES ADMIN" && w.DeptName == currDeptName);
                }
                else if (currUserID == "589.10.09")
                {
                    DataList = DataList.Where(w => w.SectionName == "SALES ADMIN" || w.DeptName == currDeptName);
                }
                else if (currUserID == "814.01.19")
                {
                    DataList = DataList.Where(w => w.DeptName == currDeptName || w.NIK == "822.01.19");
                }
                else
                {
                    DataList = DataList.Where(w => w.DeptName == currDeptName);
                }
            }
            else if (position3.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "851.02.21")
                {
                    DataList = DataList.Where(w => (w.DivName == currDivName || w.DeptName == "SALES - OEM & OES") || (w.NIK == "663.01.14" || w.NIK == "665.11.13"));
                }
                else if (currUserID == "546.08.05")
                {
                    DataList = DataList.Where(w => w.DivName == currDivName || w.DivName == "SUPPLY CHAIN MANAGEMENT");
                }
                else if (currUserID == "618.04.12")
                {
                    DataList = DataList.Where(w => w.DivName == "PROD. ENGINEERING & MAINTENANCE" || w.DivName == "PRODUCTION");
                }
                else if (currUserID == "EXP.014")
                {
                    DataList = DataList.Where(w => w.DivName == "PROD. ENGINEERING & MAINTENANCE" || w.DivName == "PRODUCTION" || w.DivName == "QUALITY MANAGEMENT");
                }
                else if (currUserID == "EXP.015")
                {
                    DataList = DataList.Where(w => w.DivName == "SALES & MARKETING");
                }
                else if (currUserID == "EXP.011")
                {
                    DataList = DataList.Where(w => w.DivName == "ACCOUNTING & IT");
                }
                else
                {
                    DataList = DataList.Where(w => w.DivName == currDivName);
                }
            }
            else if (position4.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "EXP.014")
                {
                    DataList = DataList.Where(w => w.DivName == "PROD. ENGINEERING & MAINTENANCE" || w.DivName == "PRODUCTION" || w.DivName == "QUALITY MANAGEMENT");
                }
                else if (currUserID == "EXP.015")
                {
                    DataList = DataList.Where(w => w.DivName == "SALES & MARKETING");
                }
                else if (currUserID == "EXP.011")
                {
                    DataList = DataList.Where(w => w.DivName == "ACCOUNTING & IT");
                }
                else if (currUserID == "EXP.013")
                {
                    //AllDiv No need to place where clause
                }
                else
                {
                    DataList = DataList.Where(w => w.DivName == currDivName);
                }
            }
            else
            {
                DataList = DataList.Where(w => w.NIK == currUserID);
            }

            ViewBag.DataList = DataList.OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberPost = DataList.Select(s => s.PostName).ToArray();
            ViewBag.Position = position;
            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            ViewBag.ApprovalStatus = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            ViewBag.NavHide = true;

            return View();
        }
        public ActionResult SummaryAll()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData.DivisionName;
            var currDeptName = currUserData.DeptName;
            var currSectName = currUserData.SectionName;
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 102
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }


            var period = Request["iPAPeriod"] != null ? int.Parse(Request["iPAPeriod"]) : DateTime.Now.Year;
            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iPAPeriod"]) ? Request["iPAPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 10 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            ViewBag.Period = periodFY;
            ViewBag.PeriodYear = period;
            ViewBag.ApprovalPost = Request["iApprovalPost"] != null ? Request["iApprovalPost"] : "Reviewer";

            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            if (currUserData.Users_Position.Position_Ranking >= 17 && ViewBag.ApprovalPost == "Reviewer")
            {
                subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking && w.Position_Ranking < 15).Select(s => s.Position_Name).ToArray();
            }
            else if (currUserData.Users_Position.Position_Ranking >= 17 && ViewBag.ApprovalPost == "Approver")
            {
                subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking && w.Position_Ranking >= 15).Select(s => s.Position_Name).ToArray();
            }

            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            var DataList = dbPA.HC_Performance_Appraisal
                .Include("EmployeeBy")
                .Include("DirectBy")
                .Include("IndirectBy")
                .Where(w => w.Period_FY == periodFY && w.Approval >= 2 && w.NIK.Substring(0, 1) != "P");


            ViewBag.DataList = DataList.OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberPost = DataList.Select(s => s.PostName).ToArray();
            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            ViewBag.ApprovalStatus = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            ViewBag.NavHide = true;
            return View();
        }

        public ActionResult SummaryHC()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData.DivisionName;
            var currDeptName = currUserData.DeptName;
            var currSectName = currUserData.SectionName;
            var period = Request["iPAPeriod"] != null ? int.Parse(Request["iPAPeriod"]) : DateTime.Now.Year;
            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iPAPeriod"]) ? Request["iPAPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 10 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            ViewBag.Period = periodFY;
            ViewBag.PeriodYear = period;
            ViewBag.ApprovalPost = Request["iApprovalPost"] != null ? Request["iApprovalPost"] : "Reviewer";

            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            if (currUserData.Users_Position.Position_Ranking >= 17 && ViewBag.ApprovalPost == "Reviewer")
            {
                subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking && w.Position_Ranking < 15).Select(s => s.Position_Name).ToArray();
            }
            else if (currUserData.Users_Position.Position_Ranking >= 17 && ViewBag.ApprovalPost == "Approver")
            {
                subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking && w.Position_Ranking >= 15).Select(s => s.Position_Name).ToArray();
            }

            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            var DataList = dbPA.HC_Performance_Appraisal
                .Include("EmployeeBy")
                .Include("DirectBy")
                .Include("IndirectBy")
                .Where(w => w.Period_FY == periodFY && w.Approval >= 3 && w.NIK.Substring(0, 1) != "P");

            ViewBag.DataList = DataList.OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberPost = DataList.Select(s => s.PostName).ToArray();
            ViewBag.Percentage = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            ViewBag.ApprovalStatus = dbPA.HC_Performance_Appraisal_Percentage.Where(w => w.Period_Year <= period).OrderByDescending(o => o.Period_Year).FirstOrDefault();
            ViewBag.NavHide = true;

            return View();
        }

        public ActionResult PASign()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currMethod = Request["iPASignMethod"];
            var currID = Int32.Parse(Request["iPASignFormID"]);
            var currData = dbPA.HC_Performance_Appraisal.Where(w => w.ID == currID).First();
            currData.Approval = (currMethod == "Indirect" ? currData.Approval + 1 : currData.Approval);
            currData.Approval_Sub = (currMethod == "Indirect" ? 0 : currData.Approval_Sub + 1);
            if (currMethod == "Indirect")
            {
                currData.Sign_Indirect_By = currUserID;
                currData.Sign_Indirect_At = DateTime.Now;
            }
            else
            {
                switch (currData.Approval_Sub)
                {
                    case 2:
                        currData.Sign_Direct_By = currUserID;
                        currData.Sign_Direct_At = DateTime.Now;
                        break;
                    default:
                        currData.Sign_Employee_By = currUserID;
                        currData.Sign_Employee_At = DateTime.Now;
                        break;
                }
            }
            currData.IsSaved = false;
            dbPA.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult PASignSummary()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currMethod = Request["iPASignMethod"];
            var currPeriod = Request["iPASignPeriod"];
            var currNIKList = Request["iPASignNIK"].ToString().Split(',');
            var currData = dbPA.HC_Performance_Appraisal.Where(w => w.Period_FY == currPeriod && currNIKList.Contains(w.NIK) && w.Approval == 2).ToList();

            foreach (var data in currData)
            {
                data.Approval = (currMethod == "Approve" ? data.Approval + 1 : data.Approval);
                data.Approval_Sub = (currMethod == "Approve" ? 0 : data.Approval_Sub + 1);

                if (currMethod == "Approve")
                {
                    data.Sign_Approved_By = currUserID;
                    data.Sign_Approved_At = DateTime.Now;
                }
                else
                {
                    data.Sign_Reviewed_By = currUserID;
                    data.Sign_Reviewed_At = DateTime.Now;
                }
            }
            dbPA.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }


        public ActionResult PASignSummaryHC()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currMethod = Request["iPASignMethod"];
            var currPeriod = Request["iPASignPeriod"];
            var currData = dbPA.HC_Performance_Appraisal.Where(w => w.Period_FY == currPeriod && w.Approval == 3).ToList();

            foreach (var data in currData)
            {
                data.Approval = 4;
                data.Approval_Sub = 0;
                data.Sign_Finalize_By = currUserID;
                data.Sign_Finalize_At = DateTime.Now;
            }

            dbPA.SaveChanges();

            return RedirectToAction("summaryHC", "PerformanceAppraisal", new { area = "HC" });
        }

        public ActionResult PAReleaseScore()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currPeriod = Request["iPASignPeriod"];
            var currData = dbPA.HC_Performance_Appraisal.Where(w => w.Period_FY == currPeriod && w.Approval == 4).ToList();

            foreach (var data in currData)
            {
                data.IsReleased = !data.IsReleased;
            }

            dbPA.SaveChanges();

            return RedirectToAction("summaryHC", "PerformanceAppraisal", new { area = "HC" });
        }

        public ActionResult PAReturn()
        {
            var currMethod = Request["iPASignMethod"];
            var currID = Int32.Parse(Request["iPASignFormID"]);
            var currData = dbPA.HC_Performance_Appraisal.Where(w => w.ID == currID).First();
            currData.Approval = 1;
            currData.Approval_Sub = currData.Approval_Sub - 1;
            if (currData.Approval_Sub == 1)
            {
                currData.Sign_Direct_By = null;
                currData.Sign_Direct_At = null;
            }
            else
            {
                currData.Sign_Employee_By = null;
                currData.Sign_Employee_At = null;
            }
            currData.IsSaved = false;
            dbPA.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }


        public ActionResult PAReturnSummary()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currMethod = Request["iPASignMethod"];
            var currDept = Request["iPASignDept"];
            var currData = dbPA.HC_Performance_Appraisal.Where(w => w.DeptName == currDept).ToList();
            foreach (var data in currData)
            {
                data.Approval = 2;
                data.Approval_Sub = 0;
                data.Sign_Reviewed_By = null;
                data.Sign_Reviewed_At = null;
                data.Sign_Approved_By = null;
                data.Sign_Approved_At = null;
            }
            dbPA.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }
    }
}