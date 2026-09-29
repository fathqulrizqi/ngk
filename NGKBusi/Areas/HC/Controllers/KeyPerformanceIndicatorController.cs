using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.HC.Controllers
{
    [Authorize]
    public class KeyPerformanceIndicatorController : Controller
    {

        KPIConnection dbKPI = new KPIConnection();
        DefaultConnection db = new DefaultConnection();

        // GET: HC/KeyPerformanceIndicator
        public ActionResult Form()
        {
            ViewBag.NavHide = true;
            var _currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = Request["addNIK"] != null ? Request["addNIK"] : _currUser.GetUserId();
            var currFormGUID = Request["guid"];
            var currHeader = dbKPI.HC_KPI_Header.Where(w => w.GUID == currFormGUID).OrderByDescending(o => o.ID).FirstOrDefault();
            var currHeaderID = currHeader?.ID ?? 0;

            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currHeader != null ? currHeader.DivName : currUserData?.DivisionName;
            var currDeptName = currHeader != null ? currHeader.DeptName : currUserData?.DeptName;
            var currSectName = currHeader != null ? currHeader.SectionName : currUserData?.SectionName;
            var currCostName = currHeader != null ? currHeader.CostName : currUserData.CostName;
            var currPostName = currHeader != null ? currHeader.PostName : currUserData.PositionName.Replace(" (ACTING)", "");

            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iKPIPeriod"]) ? Request["iKPIPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 3 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            periodFY = !String.IsNullOrEmpty(Request["periodFY"]) ? Request["periodFY"] : periodFY;
            ViewBag.Period = periodFY;
            var period = Request["iKPIPeriod"] != null ? int.Parse(Request["iKPIPeriod"]) : DateTime.Now.Year;
            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking <= currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();

            var DataList = dbKPI.HC_KPI_Header
                .Where(w => w.Period_FY == periodFY && (subOrdinate.Contains(w.PostName) || w.NIK == currUserID) && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            var MemberList = db.V_Users_Active.Where(w => (subOrdinate.Contains(w.PositionName) || w.NIK == currUserID) && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            //string[] position1 = { "SUPERVISOR" };
            //string[] position2 = { "ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "MANAGER (ACTING)" };
            //string[] position3 = { "SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            //string[] position4 = { "BOD" };
            string[] position1 = { "SUPERVISOR", "5A - SUPERVISOR", "5B - SUPERVISOR", "5C - SUPERVISOR" };
            string[] position2 = { "6A - ASSISTANT MANAGER", "6B - ASSISTANT MANAGER", "6C - ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "7A - MANAGER", "7B - MANAGER", "MANAGER (ACTING)" };
            string[] position3 = { "SENIOR MANAGER", "8A - SENIOR MANAGER", "8B - SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "9A - DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            string[] position4 = { "BOD" };
            if (position1.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "676.05.14")
                {
                    DataList = DataList.Where(w => w.SectionName == "PACKAGING & OEM" || w.SectionName == currSectName);
                    MemberList = MemberList.Where(w => w.SectionName == "PACKAGING & OEM" || w.SectionName == currSectName);
                }
                else
                {
                    DataList = DataList.Where(w => w.SectionName == currSectName);
                    MemberList = MemberList.Where(w => w.SectionName == currSectName);
                }
            }
            else if (position2.Contains(currUserData.Users_Position.Position_Name))
            {
                if (currUserID == "685.05.14")
                {

                    DataList = DataList.Where(w => w.DeptName == "SP PD - METAL SHELL & INSULATOR" || (w.DeptName == currDeptName && w.NIK == "685.05.14"));
                    MemberList = MemberList.Where(w => w.DeptName == "SP PD - METAL SHELL & INSULATOR" || (w.DeptName == currDeptName && w.NIK == "685.05.14"));
                }
                else if (currUserID == "719.04.15")
                {
                    DataList = DataList.Where(w => w.SectionName == "INDIRECT PROCUREMENT" || (w.DeptName == currDeptName));
                    MemberList = MemberList.Where(w => w.SectionName == "INDIRECT PROCUREMENT" || (w.DeptName == currDeptName));
                }
                else if (currUserID == "692.08.14")
                {
                    DataList = DataList.Where(w => w.DeptName == "SP PD - SPARK PLUG" || (w.DeptName == currDeptName && w.NIK == "692.08.14"));
                    MemberList = MemberList.Where(w => w.DeptName == "SP PD - SPARK PLUG" || (w.DeptName == currDeptName && w.NIK == "692.08.14"));
                }
                else if (currUserID == "664.08.13")
                {
                    DataList = DataList.Where(w => w.DeptName == "PC PD - PLUG CAP" || (w.DeptName == currDeptName && w.NIK == "664.08.13"));
                    MemberList = MemberList.Where(w => w.DeptName == "PC PD - PLUG CAP" || (w.DeptName == currDeptName && w.NIK == "664.08.13"));
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
                    DataList = DataList.Where(w => w.DivName == currDivName && w.DeptName != "SALES - OEM & OES");
                    MemberList = MemberList.Where(w => w.DivisionName == currDivName && w.DeptName != "SALES - OEM & OES");
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
                                
            ViewBag.PeriodFY = periodFY;
            ViewBag.KPIHeader = currHeader;
            ViewBag.KPIData = dbKPI.HC_KPI_Data.Where(w => w.Header_ID == currHeaderID).OrderBy(o => o.HC_KPI_Perspective_List.Seq).ToList();
            ViewBag.KPIPerspectiveWeight = dbKPI.HC_KPI_Perspective_Weight.Where(w => w.PeriodFY == periodFY && w.Department.ToLower() == currDeptName.ToLower() && w.Position == currPostName).FirstOrDefault();

            return View();
        }
        public ActionResult FormAll()
        {
            ViewBag.NavHide = true;
            var _currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = Request["addNIK"] != null ? Request["addNIK"] : _currUser.GetUserId();
            var currFormGUID = Request["guid"];
            var currHeader = dbKPI.HC_KPI_Header.Where(w => w.GUID == currFormGUID).OrderByDescending(o => o.ID).FirstOrDefault();
            var currHeaderID = currHeader?.ID ?? 0;

            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData?.DivisionName;
            var currDeptName = currUserData?.DeptName;
            var currSectName = currUserData?.SectionName;
            var currCostName = currUserData.CostName;
            var currPostName = currUserData.PositionName.Replace(" (ACTING)", "");

            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iKPIPeriod"]) ? Request["iKPIPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 3 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            ViewBag.Period = periodFY;
            var period = Request["iKPIPeriod"] != null ? int.Parse(Request["iKPIPeriod"]) : DateTime.Now.Year;
            var subOrdinate = db.V_Users_Position.Select(s => s.Position_Name).ToArray();

            var DataList = dbKPI.HC_KPI_Header
                .Where(w => w.Period_FY == periodFY && (subOrdinate.Contains(w.PostName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            var MemberList = db.V_Users_Active.Where(w => (subOrdinate.Contains(w.PositionName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            //string[] position1 = { "SUPERVISOR" };
            //string[] position2 = { "ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "MANAGER (ACTING)" };
            //string[] position3 = { "SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            //string[] position4 = { "BOD" };
            string[] position1 = { "SUPERVISOR", "5A - SUPERVISOR", "5B - SUPERVISOR", "5C - SUPERVISOR" };
            string[] position2 = { "6A - ASSISTANT MANAGER", "6B - ASSISTANT MANAGER", "6C - ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "7A - MANAGER", "7B - MANAGER", "MANAGER (ACTING)" };
            string[] position3 = { "SENIOR MANAGER", "8A - SENIOR MANAGER", "8B - SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "9A - DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
            string[] position4 = { "BOD" };

            var DataNIK = DataList.Select(s => s.NIK).ToArray();
            MemberList = MemberList.Where(w => !DataNIK.Contains(w.NIK));
            ViewBag.DataList = DataList.OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberList = MemberList.OrderByDescending(o => o.DivisionName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();

            ViewBag.PeriodFY = periodFY;
            ViewBag.KPIHeader = currHeader;
            ViewBag.KPIData = dbKPI.HC_KPI_Data.Where(w => w.Header_ID == currHeaderID).OrderBy(o => o.HC_KPI_Perspective_List.Seq).ToList();
            ViewBag.KPIPerspectiveWeight = dbKPI.HC_KPI_Perspective_Weight.Where(w => w.PeriodFY == periodFY && w.Department.ToLower() == currDeptName.ToLower() && w.Position == currPostName).FirstOrDefault();

            return View();
        }
        public ActionResult Form2()
        {
            ViewBag.NavHide = true;
            var _currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = _currUser.GetUserId();
            var currHeader = dbKPI.HC_KPI_Header.Where(w => w.Period_FY == "FY124" && w.NIK == currUserID).OrderByDescending(o => o.ID).FirstOrDefault();
            var currHeaderID = currHeader?.ID ?? 0;
            var currCostName = _currUser.FindFirstValue("costName");
            var currPostName = _currUser.FindFirstValue("postName").Replace(" (ACTING)", "");
            var AXSection = db.Users_Section_AX.Where(w => w.COSTNAME == currCostName).FirstOrDefault();
            var currSectName = AXSection.SECTION;
            ViewBag.PeriodFY = "FY124";
            ViewBag.KPIHeader = currHeader;
            ViewBag.KPIData = dbKPI.HC_KPI_Data.Where(w => w.Header_ID == currHeaderID).ToList();
            ViewBag.KPIPerspectiveWeight = dbKPI.HC_KPI_Perspective_Weight.Where(w => w.Department.ToLower() == currSectName.ToLower() && w.Position == currPostName).FirstOrDefault();

            return View();
        }

        public ActionResult FormSubmit()
        {
            ViewBag.NavHide = true;
            var _currUser = ((ClaimsIdentity)User.Identity);

            var currUserID = Request["addNIK"] != null ? Request["addNIK"] : _currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currUserName = currUserData.Name;
            var currUserDiv = currUserData.DivisionName;
            var currUserDept = currUserData.DeptName;
            var currUserSect = currUserData.SectionName;
            var currUserPost = currUserData.PositionName;
            var currUserCostID = currUserData.CostID;
            var currUserCostName = currUserData.CostName;
            var currGUID = Request["iGUID"];
            var headerID = int.Parse(Request["iHeaderID"]);
            var checkHeader = dbKPI.HC_KPI_Header.Where(w => w.ID == headerID).FirstOrDefault();
            var currHeaderID = headerID;
            var currDataID = 0;
            if (checkHeader == null)
            {
                var newHeader = new HC_KPI_Header();
                newHeader.GUID = Guid.NewGuid().ToString();
                newHeader.Period_FY = Request["iPeriodFY"];
                newHeader.NIK = Request["iNIK"];
                newHeader.Name = Request["iName"];
                newHeader.DivName = currUserDiv;
                newHeader.DeptName = currUserDept;
                newHeader.SectionName = currUserSect;
                newHeader.PostName = currUserPost;
                newHeader.CostID = currUserCostID;
                newHeader.CostName = currUserCostName;
                newHeader.Created_At = DateTime.Now;
                newHeader.Created_By = currUserID;
                newHeader.Approval = 1;
                newHeader.Approval_Sub = 0;
                dbKPI.HC_KPI_Header.Add(newHeader);
                dbKPI.SaveChanges();
                currHeaderID = newHeader.ID;
                currGUID = newHeader.GUID;
            }
            else
            {
                checkHeader.Period_FY = Request["iPeriodFY"];
                checkHeader.NIK = Request["iNIK"];
                checkHeader.Name = Request["iName"];
                checkHeader.PostName = currUserPost;
                dbKPI.SaveChanges();
                currHeaderID = checkHeader.ID;
                currGUID = checkHeader.GUID;
            }

            for (var i = 0; i <= Request.Form.GetValues("iKPI[]").Count() - 1; i++)
            {
                var dataID = int.Parse(Request.Form.GetValues("iDataID[]")[i]);
                var checkData = dbKPI.HC_KPI_Data.Where(w => w.ID == dataID).FirstOrDefault();
                if (checkData == null)
                {
                    var newData = new HC_KPI_Data();
                    newData.Header_ID = currHeaderID;
                    newData.Period_FY = Request["iPeriodFY"];
                    newData.Perspective = Request.Form.GetValues("iPerspective[]")[i];
                    newData.KPI = Request.Form.GetValues("iKPI[]")[i];
                    newData.Last_Achievement = Request.Form.GetValues("iLastAchievement[]")[i];
                    newData.Target = Request.Form.GetValues("iTarget[]")[i];
                    newData.Weight = double.Parse(Request.Form.GetValues("iWeight[]")[i].Replace("%", ""));
                    newData.Action_Plan = Request.Form.GetValues("iActionPlan[]")[i];
                    newData.Primary_Share = Request.Form.GetValues("iPrimaryShare[]")[i];
                    newData.Type = Request.Form.GetValues("iKPIType[]")[i];
                    newData.Total = double.Parse(Request.Form.GetValues("iTotal[]")[i].Replace("%", ""));
                    newData.Status = Request.Form.GetValues("iStatus[]")[i];
                    newData.Remark = Request.Form.GetValues("iRemark[]")[i];
                    newData.Plan_Apr = double.Parse(Request.Form.GetValues("iPlanApr[]")[i]);
                    newData.Plan_May = double.Parse(Request.Form.GetValues("iPlanMay[]")[i]);
                    newData.Plan_Jun = double.Parse(Request.Form.GetValues("iPlanJun[]")[i]);
                    newData.Plan_Jul = double.Parse(Request.Form.GetValues("iPlanJul[]")[i]);
                    newData.Plan_Aug = double.Parse(Request.Form.GetValues("iPlanAug[]")[i]);
                    newData.Plan_Sep = double.Parse(Request.Form.GetValues("iPlanSep[]")[i]);
                    newData.Plan_Oct = double.Parse(Request.Form.GetValues("iPlanOct[]")[i]);
                    newData.Plan_Nov = double.Parse(Request.Form.GetValues("iPlanNov[]")[i]);
                    newData.Plan_Des = double.Parse(Request.Form.GetValues("iPlanDes[]")[i]);
                    newData.Plan_Jan = double.Parse(Request.Form.GetValues("iPlanJan[]")[i]);
                    newData.Plan_Feb = double.Parse(Request.Form.GetValues("iPlanFeb[]")[i]);
                    newData.Plan_Mar = double.Parse(Request.Form.GetValues("iPlanMar[]")[i]);
                    newData.Plan_Q1 = double.Parse(Request.Form.GetValues("iPlanQ1[]")[i]);
                    newData.Plan_Q2 = double.Parse(Request.Form.GetValues("iPlanQ2[]")[i]);
                    newData.Plan_Q3 = double.Parse(Request.Form.GetValues("iPlanQ3[]")[i]);
                    newData.Plan_Q4 = double.Parse(Request.Form.GetValues("iPlanQ4[]")[i]);
                    newData.Act_Apr = double.Parse(Request.Form.GetValues("iActApr[]")[i]);
                    newData.Act_May = double.Parse(Request.Form.GetValues("iActMay[]")[i]);
                    newData.Act_Jun = double.Parse(Request.Form.GetValues("iActJun[]")[i]);
                    newData.Act_Jul = double.Parse(Request.Form.GetValues("iActJul[]")[i]);
                    newData.Act_Aug = double.Parse(Request.Form.GetValues("iActAug[]")[i]);
                    newData.Act_Sep = double.Parse(Request.Form.GetValues("iActSep[]")[i]);
                    newData.Act_Oct = double.Parse(Request.Form.GetValues("iActOct[]")[i]);
                    newData.Act_Nov = double.Parse(Request.Form.GetValues("iActNov[]")[i]);
                    newData.Act_Des = double.Parse(Request.Form.GetValues("iActDes[]")[i]);
                    newData.Act_Jan = double.Parse(Request.Form.GetValues("iActJan[]")[i]);
                    newData.Act_Feb = double.Parse(Request.Form.GetValues("iActFeb[]")[i]);
                    newData.Act_Mar = double.Parse(Request.Form.GetValues("iActMar[]")[i]);
                    newData.Act_Q1 = double.Parse(Request.Form.GetValues("iActQ1[]")[i]);
                    newData.Act_Q2 = double.Parse(Request.Form.GetValues("iActQ2[]")[i]);
                    newData.Act_Q3 = double.Parse(Request.Form.GetValues("iActQ3[]")[i]);
                    newData.Act_Q4 = double.Parse(Request.Form.GetValues("iActQ4[]")[i]);
                    newData.Percent_Apr = double.Parse(Request.Form.GetValues("iPercentApr[]")[i].Replace("%", ""));
                    newData.Percent_May = double.Parse(Request.Form.GetValues("iPercentMay[]")[i].Replace("%", ""));
                    newData.Percent_Jun = double.Parse(Request.Form.GetValues("iPercentJun[]")[i].Replace("%", ""));
                    newData.Percent_Jul = double.Parse(Request.Form.GetValues("iPercentJul[]")[i].Replace("%", ""));
                    newData.Percent_Aug = double.Parse(Request.Form.GetValues("iPercentAug[]")[i].Replace("%", ""));
                    newData.Percent_Sep = double.Parse(Request.Form.GetValues("iPercentSep[]")[i].Replace("%", ""));
                    newData.Percent_Oct = double.Parse(Request.Form.GetValues("iPercentOct[]")[i].Replace("%", ""));
                    newData.Percent_Nov = double.Parse(Request.Form.GetValues("iPercentNov[]")[i].Replace("%", ""));
                    newData.Percent_Des = double.Parse(Request.Form.GetValues("iPercentDes[]")[i].Replace("%", ""));
                    newData.Percent_Jan = double.Parse(Request.Form.GetValues("iPercentJan[]")[i].Replace("%", ""));
                    newData.Percent_Feb = double.Parse(Request.Form.GetValues("iPercentFeb[]")[i].Replace("%", ""));
                    newData.Percent_Mar = double.Parse(Request.Form.GetValues("iPercentMar[]")[i].Replace("%", ""));
                    newData.Percent_Q1 = double.Parse(Request.Form.GetValues("iPercentQ1[]")[i].Replace("%", ""));
                    newData.Percent_Q2 = double.Parse(Request.Form.GetValues("iPercentQ2[]")[i].Replace("%", ""));
                    newData.Percent_Q3 = double.Parse(Request.Form.GetValues("iPercentQ3[]")[i].Replace("%", ""));
                    newData.Percent_Q4 = double.Parse(Request.Form.GetValues("iPercentQ4[]")[i].Replace("%", ""));
                    newData.Created_At = DateTime.Now;
                    newData.Created_By = currUserID;
                    dbKPI.HC_KPI_Data.Add(newData);
                    dbKPI.SaveChanges();
                    currDataID = newData.ID;
                }
                else
                {
                    checkData.Header_ID = currHeaderID;
                    checkData.Period_FY = Request["iPeriodFY"];
                    checkData.Perspective = Request.Form.GetValues("iPerspective[]")[i];
                    checkData.KPI = Request.Form.GetValues("iKPI[]")[i];
                    checkData.Last_Achievement = Request.Form.GetValues("iLastAchievement[]")[i];
                    checkData.Target = Request.Form.GetValues("iTarget[]")[i];
                    checkData.Weight = double.Parse(Request.Form.GetValues("iWeight[]")[i].Replace("%", ""));
                    checkData.Action_Plan = Request.Form.GetValues("iActionPlan[]")[i];
                    checkData.Primary_Share = Request.Form.GetValues("iPrimaryShare[]")[i];
                    checkData.Type = Request.Form.GetValues("iKPIType[]")[i];
                    checkData.Total = double.Parse(Request.Form.GetValues("iTotal[]")[i].Replace("%", ""));
                    checkData.Status = Request.Form.GetValues("iStatus[]")[i];
                    checkData.Remark = Request.Form.GetValues("iRemark[]")[i];
                    checkData.Plan_Apr = double.Parse(Request.Form.GetValues("iPlanApr[]")[i]);
                    checkData.Plan_May = double.Parse(Request.Form.GetValues("iPlanMay[]")[i]);
                    checkData.Plan_Jun = double.Parse(Request.Form.GetValues("iPlanJun[]")[i]);
                    checkData.Plan_Jul = double.Parse(Request.Form.GetValues("iPlanJul[]")[i]);
                    checkData.Plan_Aug = double.Parse(Request.Form.GetValues("iPlanAug[]")[i]);
                    checkData.Plan_Sep = double.Parse(Request.Form.GetValues("iPlanSep[]")[i]);
                    checkData.Plan_Oct = double.Parse(Request.Form.GetValues("iPlanOct[]")[i]);
                    checkData.Plan_Nov = double.Parse(Request.Form.GetValues("iPlanNov[]")[i]);
                    checkData.Plan_Des = double.Parse(Request.Form.GetValues("iPlanDes[]")[i]);
                    checkData.Plan_Jan = double.Parse(Request.Form.GetValues("iPlanJan[]")[i]);
                    checkData.Plan_Feb = double.Parse(Request.Form.GetValues("iPlanFeb[]")[i]);
                    checkData.Plan_Mar = double.Parse(Request.Form.GetValues("iPlanMar[]")[i]);
                    checkData.Plan_Q1 = double.Parse(Request.Form.GetValues("iPlanQ1[]")[i]);
                    checkData.Plan_Q2 = double.Parse(Request.Form.GetValues("iPlanQ2[]")[i]);
                    checkData.Plan_Q3 = double.Parse(Request.Form.GetValues("iPlanQ3[]")[i]);
                    checkData.Plan_Q4 = double.Parse(Request.Form.GetValues("iPlanQ4[]")[i]);
                    checkData.Act_Apr = double.Parse(Request.Form.GetValues("iActApr[]")[i]);
                    checkData.Act_May = double.Parse(Request.Form.GetValues("iActMay[]")[i]);
                    checkData.Act_Jun = double.Parse(Request.Form.GetValues("iActJun[]")[i]);
                    checkData.Act_Jul = double.Parse(Request.Form.GetValues("iActJul[]")[i]);
                    checkData.Act_Aug = double.Parse(Request.Form.GetValues("iActAug[]")[i]);
                    checkData.Act_Sep = double.Parse(Request.Form.GetValues("iActSep[]")[i]);
                    checkData.Act_Oct = double.Parse(Request.Form.GetValues("iActOct[]")[i]);
                    checkData.Act_Nov = double.Parse(Request.Form.GetValues("iActNov[]")[i]);
                    checkData.Act_Des = double.Parse(Request.Form.GetValues("iActDes[]")[i]);
                    checkData.Act_Jan = double.Parse(Request.Form.GetValues("iActJan[]")[i]);
                    checkData.Act_Feb = double.Parse(Request.Form.GetValues("iActFeb[]")[i]);
                    checkData.Act_Mar = double.Parse(Request.Form.GetValues("iActMar[]")[i]);
                    checkData.Act_Q1 = double.Parse(Request.Form.GetValues("iActQ1[]")[i]);
                    checkData.Act_Q2 = double.Parse(Request.Form.GetValues("iActQ2[]")[i]);
                    checkData.Act_Q3 = double.Parse(Request.Form.GetValues("iActQ3[]")[i]);
                    checkData.Act_Q4 = double.Parse(Request.Form.GetValues("iActQ4[]")[i]);
                    checkData.Percent_Apr = double.Parse(Request.Form.GetValues("iPercentApr[]")[i].Replace("%", ""));
                    checkData.Percent_May = double.Parse(Request.Form.GetValues("iPercentMay[]")[i].Replace("%", ""));
                    checkData.Percent_Jun = double.Parse(Request.Form.GetValues("iPercentJun[]")[i].Replace("%", ""));
                    checkData.Percent_Jul = double.Parse(Request.Form.GetValues("iPercentJul[]")[i].Replace("%", ""));
                    checkData.Percent_Aug = double.Parse(Request.Form.GetValues("iPercentAug[]")[i].Replace("%", ""));
                    checkData.Percent_Sep = double.Parse(Request.Form.GetValues("iPercentSep[]")[i].Replace("%", ""));
                    checkData.Percent_Oct = double.Parse(Request.Form.GetValues("iPercentOct[]")[i].Replace("%", ""));
                    checkData.Percent_Nov = double.Parse(Request.Form.GetValues("iPercentNov[]")[i].Replace("%", ""));
                    checkData.Percent_Des = double.Parse(Request.Form.GetValues("iPercentDes[]")[i].Replace("%", ""));
                    checkData.Percent_Jan = double.Parse(Request.Form.GetValues("iPercentJan[]")[i].Replace("%", ""));
                    checkData.Percent_Feb = double.Parse(Request.Form.GetValues("iPercentFeb[]")[i].Replace("%", ""));
                    checkData.Percent_Mar = double.Parse(Request.Form.GetValues("iPercentMar[]")[i].Replace("%", ""));
                    checkData.Percent_Q1 = double.Parse(Request.Form.GetValues("iPercentQ1[]")[i].Replace("%", ""));
                    checkData.Percent_Q2 = double.Parse(Request.Form.GetValues("iPercentQ2[]")[i].Replace("%", ""));
                    checkData.Percent_Q3 = double.Parse(Request.Form.GetValues("iPercentQ3[]")[i].Replace("%", ""));
                    checkData.Percent_Q4 = double.Parse(Request.Form.GetValues("iPercentQ4[]")[i].Replace("%", ""));
                    dbKPI.SaveChanges();
                    currDataID = checkData.ID;
                }
            }
            uploadEvidence(headerID, currDataID);
            return RedirectToAction("Form", "KeyPerformanceIndicator", new { area = "HC", GUID = currGUID });
        }


        public ActionResult PerspectiveWeight(int iID = 0)
        {
            ViewBag.NavHide = true;
            Int32 period = DateTime.Now.AddYears(-1).Year;

            var _currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = _currUser.GetUserId();
            var currDivName = _currUser.FindFirstValue("divName");
            var currDeptName = _currUser.FindFirstValue("deptName");
            var currSectName = _currUser.FindFirstValue("sectName");
            ViewBag.PeriodFY = "FY124";
            ViewBag.KPIPerspectiveWeight = dbKPI.HC_KPI_Perspective_Weight.Where(w => w.ID == iID).FirstOrDefault();
            ViewBag.KPIPerspectiveWeightList = dbKPI.HC_KPI_Perspective_Weight.Where(w => w.Department == currDeptName).ToList();
            ViewBag.DeptAll = db.V_Users_Active.Where(w => w.DeptName == currDeptName).OrderBy(o => o.DeptName).Select(s => s.DeptName).Distinct().ToList();
            if (currDeptName == null)
            {
                ViewBag.DeptAll = db.V_Users_Active.Where(w => w.DivisionName == currDivName).OrderBy(o => o.DeptName).Select(s => s.DeptName).Distinct().ToList();
            }
            if (currUserID == "685.05.14")
            {
                ViewBag.DeptAll = db.V_Users_Active.Where(w => w.DeptName == currDeptName || w.DeptName == "SP PD - METAL SHELL & INSULATOR").OrderBy(o => o.DeptName).Select(s => s.DeptName).Distinct().ToList();
            }
            else if (currUserID == "692.08.14")
            {
                ViewBag.DeptAll = db.V_Users_Active.Where(w => w.DeptName == currDeptName || w.DeptName == "SP PD - SPARK PLUG").OrderBy(o => o.DeptName).Select(s => s.DeptName).Distinct().ToList();
            }
            else if (currUserID == "664.08.13")
            {
                ViewBag.DeptAll = db.V_Users_Active.Where(w => w.DeptName == currDeptName || w.DeptName == "PC PD - PLUG CAP").OrderBy(o => o.DeptName).Select(s => s.DeptName).Distinct().ToList();
            }

            string[] positionList = { "CASUAL", "ADMIN", "OPERATOR", "JUNIOR STAFF", "FOREMAN", "SPECIALIST", "JUNIOR ENGINEER", "ENGINEER", "GROUP LEADER", "SUPERVISOR", "SENIOR STAFF", "ASSISTANT MANAGER", "MANAGER", "SENIOR MANAGER", "ASSISTANT GENERAL MANAGER", "DEPUTY GENERAL MANAGER", "GENERAL MANAGER" };
            ViewBag.Position = positionList;

            return View();
        }
        public ActionResult PerspectiveWeightSubmit()
        {
            ViewBag.NavHide = true;
            var _currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = _currUser.GetUserId();

            var periodFY = Request["iPeriodFY"];
            var department = Request["iDepartment"];
            var position = Request["iPosition"];
            var finansial = float.Parse(Request["iFinancial"]);
            var customer = float.Parse(Request["iCustomer"]);
            var internalProcess = float.Parse(Request["iInternalProcess"]);
            var learnGrowth = float.Parse(Request["iLearnGrowth"]);
            var isDataNull = false;

            HC_KPI_Perspective_Weight checkData = dbKPI.HC_KPI_Perspective_Weight.Where(w => w.PeriodFY == periodFY && w.Department == department && w.Position == position).FirstOrDefault();
            if (checkData == null)
            {
                isDataNull = true;
                checkData = new HC_KPI_Perspective_Weight();
            }
            checkData.PeriodFY = periodFY;
            checkData.Department = department;
            checkData.Position = position;
            checkData.Financial = finansial;
            checkData.Customer = customer;
            checkData.Internal_Process = internalProcess;
            checkData.Learning_Growth = learnGrowth;
            checkData.Created_At = DateTime.Now;
            checkData.Created_By = currUserID;

            if (isDataNull == true)
            {
                dbKPI.HC_KPI_Perspective_Weight.Add(checkData);
            }
            dbKPI.SaveChanges();
            return RedirectToAction("PerspectiveWeight", "KeyPerformanceIndicator", new { area = "HC" });

        }
        public ActionResult PerspectiveWeightDelete(int iID = 0)
        {
            var deleteData = dbKPI.HC_KPI_Perspective_Weight.Where(w => w.ID == iID).FirstOrDefault();
            dbKPI.HC_KPI_Perspective_Weight.Remove(deleteData);
            dbKPI.SaveChanges();
            return RedirectToAction("PerspectiveWeight", "KeyPerformanceIndicator", new { area = "HC" });

        }

        [HttpPost]
        public void uploadEvidence(int headerID = 0, int dataID = 0)
        {

            //var currReqNumber = Request["iReqNumber"];
            var currDataID = dataID;
            if (currDataID > 0)
            {
                IList<HttpPostedFileBase> iFiles = Request.Files.GetMultiple("iFiles");
                for (int i = 0; i < iFiles.Count; i++)
                {
                    HttpPostedFileBase iFile = iFiles[i];
                    // extract only the filename
                    if (iFile.ContentLength > 0)
                    {
                        string checkFolder = "~/Files/HC/KPI/Evidence/" + headerID; // Your code goes here
                        bool exists = System.IO.Directory.Exists(Server.MapPath(checkFolder));
                        if (!exists)
                            System.IO.Directory.CreateDirectory(Server.MapPath(checkFolder));

                        var fileName = iFile.FileName;
                        string extension = Path.GetExtension(fileName);
                        // store the file inside ~/App_Data/uploads folder
                        var path = Path.Combine(Server.MapPath("~/Files/HC/KPI/Evidence/" + headerID), fileName);
                        iFile.SaveAs(path);
                        var checkFile = dbKPI.HC_KPI_Attachment.Where(w => w.Header_ID == headerID && w.Filename == fileName).FirstOrDefault();
                        if (checkFile == null)
                        {
                            dbKPI.HC_KPI_Attachment.Add(new HC_KPI_Attachment()
                            {
                                Header_ID = headerID,
                                Filename = fileName,
                                Ext = extension
                            });
                        }
                    }
                }
                dbKPI.SaveChanges();
            }
            //return RedirectToAction("Form", "KeyPerformanceIndicator", new { area = "HC" });
        }


        [HttpPost]
        public ActionResult getEvidence()
        {
            var currDataID = int.Parse(Request["iHeaderID"]);

            var getFiles = dbKPI.HC_KPI_Attachment.Where(w => w.Header_ID == currDataID).Select(s => new { filename = s.Filename, ext = s.Ext, id = s.ID });

            return Json(new { files = getFiles }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult deleteEvidence()
        {
            var currID = Int32.Parse(Request["iID"]);
            var del = dbKPI.HC_KPI_Attachment.Where(w => w.ID == currID).FirstOrDefault();

            var path = Server.MapPath("~/Files/HC/KPI/Evidence/" + del.Header_ID + "/" + del.Filename);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            dbKPI.HC_KPI_Attachment.Remove(del);
            dbKPI.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult deleteData()
        {
            var currID = Int32.Parse(Request["iID"]);
            var del = dbKPI.HC_KPI_Data.Where(w => w.ID == currID).FirstOrDefault();
            if (del != null)
            {
                dbKPI.HC_KPI_Data.Remove(del);
                dbKPI.SaveChanges();
            }

            return Content(Boolean.TrueString);
        }

        public ActionResult KPISign()
        {
            var btnType = Request["btnType"];
            var headerID = int.Parse(Request["iHeaderID"]);
            //var iApproval = (btnType == "Month-Checker-Return" || btnType == "Month-Approver-Return" ? 1 : int.Parse(Request["iApproval"]));            
            var iApproval = int.Parse(Request["iApproval"]);
            var iApprovalSub = (btnType == "Month-Checker-Return" || btnType == "Month-Approver-Return" ? 0 : int.Parse(Request["iApprovalSub"]));
            var updateSign = dbKPI.HC_KPI_Header.Where(w => w.ID == headerID).FirstOrDefault();

            if (updateSign != null)
            {
                updateSign.Approval = iApproval;
                updateSign.Approval_Sub = iApprovalSub;
            }

            dbKPI.SaveChanges();

            //sendNotification(updateSign.GUID, "Form", btnType, updateSign.NIK);
            return Redirect(Request.UrlReferrer.ToString());
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
        [HttpPost]
        public async Task<ActionResult> UploadDocumentKPI(HttpPostedFileBase fileDocumentKPI)
        {
            HttpPostedFileBase file = Request.Files["fileDocumentKPI"];

            //string filePath = Server.MapPath("~/Files/Sales/EDI/Download/");
            //string fileName = "sales_suzuki";
            //filePath = filePath + fileName + ".xls";

            //fileOESSuzuki.SaveAs(filePath);

            string fileName = "";
            var usrNIK = Request.Form.Get("uNIK");
            var periodeYear = Request.Form.Get("uPeriodFY");

            if (file != null && file.ContentLength > 0)
            {
                // Ganti dengan alamat IP dan kredensial server FTP Anda
                string ftpServerIP = "ftp://192.168.1.248:21/HC/KPI/";
                string ftpUsername = "it_admin";
                string ftpPassword = "N6K4dm1n!";
                fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                string remotePath = ftpServerIP + "/" + fileName;
                //int statusUpload;
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                    try
                    {
                        // Cek apakah file sudah ada di server FTP
                        if (FileExists(remotePath, client))
                        {
                            // Jika file sudah ada, hapus file lama
                            DeleteFile(remotePath, client);
                        }

                        // Baca konten file ke dalam buffer
                        byte[] fileBytes;
                        using (Stream inputStream = file.InputStream)
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                inputStream.CopyTo(memoryStream);
                                fileBytes = memoryStream.ToArray();
                            }
                        }

                        // Unggah file baru ke server FTP
                        client.UploadData(remotePath, fileBytes);
                        //statusUpload = 1;
                    }
                    catch (Exception)
                    {
                        //statusUpload = 0;
                    }
                }
            }
            else
            {
                fileName = "asdf";
                return Json(new { ErrorMessage = "Failed to upload file" }, JsonRequestBehavior.AllowGet);
            }

            //return Json(new { status = 1 });

            //dbs.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Temp");
            //dbs.SaveChanges();
            string url = $"http://192.168.1.248:8081/niterra_webservice/impor_kpi.php?FileName={fileName}";
            using (var client = new HttpClient())
            {
                // Ganti URL dengan URL web service yang sesuai


                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true, MustRevalidate = true };
                // Mengirim permintaan GET ke web service
                HttpResponseMessage response = await client.SendAsync(request);
                var arrTest = new ArrayList();

                if (response.IsSuccessStatusCode)
                {

                    var json = await response.Content.ReadAsStringAsync();
                    var deliveryData = JsonConvert.DeserializeObject<DeliveryData>(json);
                    int totalRows = deliveryData.data.Count();
                    int headerID = 0;
                    var guid = "";
                    if (totalRows > 0)
                    {
                        // cek header id apakah sudah ada untuk periode yg sama
                        var DataHeader = dbKPI.HC_KPI_Header.Where(w => w.NIK == usrNIK && w.Period_FY == periodeYear).FirstOrDefault();

                        if (DataHeader != null)
                        {
                            // delete kpi data existing
                            var data_KPI_Exst = dbKPI.HC_KPI_Data.Where(w => w.Header_ID == DataHeader.ID).ToList();
                            dbKPI.HC_KPI_Data.RemoveRange(data_KPI_Exst);
                            dbKPI.SaveChanges();

                            headerID = DataHeader.ID;
                            guid = DataHeader.GUID;
                        }
                        else
                        {
                            var detailUsr = db.Users.Where(w => w.NIK == usrNIK).FirstOrDefault();

                            HC_KPI_Header defineHeader = new HC_KPI_Header();

                            defineHeader.GUID = Guid.NewGuid().ToString();
                            defineHeader.Period_FY = periodeYear;
                            defineHeader.NIK = usrNIK;
                            defineHeader.Name = detailUsr.Name;
                            defineHeader.DivName = detailUsr.DivisionName;
                            defineHeader.DeptName = detailUsr.DeptName;
                            defineHeader.SectionName = detailUsr.SectionName;
                            defineHeader.PostName = detailUsr.PositionName;
                            defineHeader.CostID = detailUsr.CostID;
                            defineHeader.CostName = detailUsr.CostName;
                            defineHeader.Created_At = DateTime.Now;
                            defineHeader.Created_By = usrNIK;
                            defineHeader.Approval = 1;
                            defineHeader.Approval_Sub = 0;

                            dbKPI.HC_KPI_Header.Add(defineHeader);
                            dbKPI.SaveChanges();

                            headerID = defineHeader.ID;
                            guid = defineHeader.GUID;
                        }
                    }

                    //List<HC_KPI_Data> KPIData = new List<HC_KPI_Data>();
                    foreach (var item in deliveryData.data)
                    {

                        HC_KPI_Data KPIData = new HC_KPI_Data();

                        KPIData.Header_ID = headerID;
                        KPIData.Period_FY = periodeYear;
                        KPIData.Perspective = item.perspective;
                        KPIData.KPI = item.kpi;
                        KPIData.Last_Achievement = item.last_achievement;
                        KPIData.Target = item.target;
                        KPIData.Action_Plan = item.action_plan;
                        KPIData.Primary_Share = item.primary_share;
                        KPIData.Type = item.type;
                        KPIData.Weight = item.weight;
                        KPIData.Plan_Jan = item.plan_jan;
                        KPIData.Plan_Feb = item.plan_feb;
                        KPIData.Plan_Mar = item.plan_mar;
                        KPIData.Plan_Apr = item.plan_apr;
                        KPIData.Plan_May = item.plan_may;
                        KPIData.Plan_Jun = item.plan_jun;
                        KPIData.Plan_Jul = item.plan_jul;
                        KPIData.Plan_Aug = item.plan_aug;
                        KPIData.Plan_Sep = item.plan_sep;
                        KPIData.Plan_Oct = item.plan_oct;
                        KPIData.Plan_Nov = item.plan_nov;
                        KPIData.Plan_Des = item.plan_des;
                        KPIData.Act_Jan = item.act_jan;
                        KPIData.Act_Feb = item.act_feb;
                        KPIData.Act_Mar = item.act_mar;
                        KPIData.Act_Apr = item.act_apr;
                        KPIData.Act_May = item.act_may;
                        KPIData.Act_Jun = item.act_jun;
                        KPIData.Act_Jul = item.act_jul;
                        KPIData.Act_Aug = item.act_aug;
                        KPIData.Act_Sep = item.act_sep;
                        KPIData.Act_Oct = item.act_oct;
                        KPIData.Act_Nov = item.act_nov;
                        KPIData.Act_Des = item.act_des;
                        KPIData.Percent_Jan = item.percent_jan;
                        KPIData.Percent_Feb = item.percent_feb;
                        KPIData.Percent_Mar = item.percent_mar;
                        KPIData.Percent_Apr = item.percent_apr;
                        KPIData.Percent_May = item.percent_may;
                        KPIData.Percent_Jun = item.percent_jun;
                        KPIData.Percent_Jul = item.percent_jul;
                        KPIData.Percent_Aug = item.percent_aug;
                        KPIData.Percent_Sep = item.percent_sep;
                        KPIData.Percent_Oct = item.percent_oct;
                        KPIData.Percent_Nov = item.percent_nov;
                        KPIData.Percent_Des = item.percent_des;
                        KPIData.Plan_Q1 = item.plan_q1;
                        KPIData.Plan_Q2 = item.plan_q2;
                        KPIData.Plan_Q3 = item.plan_q3;
                        KPIData.Plan_Q4 = item.plan_q4;
                        KPIData.Act_Q1 = item.act_q1;
                        KPIData.Act_Q2 = item.act_q2;
                        KPIData.Act_Q3 = item.act_q3;
                        KPIData.Act_Q4 = item.act_q4;
                        KPIData.Percent_Q1 = item.percent_q1;
                        KPIData.Percent_Q2 = item.percent_q2;
                        KPIData.Percent_Q3 = item.percent_q3;
                        KPIData.Percent_Q4 = item.percent_q4;
                        KPIData.Total = item.total;
                        KPIData.Status = item.status;
                        KPIData.Remark = item.remark;
                        KPIData.Created_At = DateTime.Now;
                        KPIData.Created_By = "usrNIK";

                        if (item.perspective != null && item.perspective != "")
                        {
                            dbKPI.HC_KPI_Data.Add(KPIData);
                        }

                    }

                    int i = dbKPI.SaveChanges();
                    if (i > 0)
                    {
                        return Json(new
                        {
                            status = totalRows == 0 ? 0 : 1,
                            guid = guid,
                            totalRow = totalRows

                        }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { ErrorMessage = "Failed to retrieve data from the web service." }, JsonRequestBehavior.AllowGet);
                    }

                }
                else
                {
                    // Handle kesalahan jika ada
                    return Json(new { ErrorMessage = "Failed to retrieve data from the web service." }, JsonRequestBehavior.AllowGet);
                }
            }

        }
        public void sendNotification(string _GUID, string currMenu, string currStatus, string currNIK = "", string deptCode = "", int approval = 0, int approval_sub = 0, string note = "-")
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            string FilePath = Path.Combine(Server.MapPath("~/Emails/HC/KeyPerformanceIndicator/"), "KeyPerformanceIndicatorApproval.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();
            var stat = "";
            var needs = "Approval";
            if (currStatus == "Month-Checker-Return" || currStatus == "Month-Approver-Return")
            {
                stat = "Returned!";
                needs = "Attention";
            }

            var settlementSubject = " - Monthly";
            var doc = "Key Performance Indicator - Approval";
            
            var currURL = Url.Action(currMenu, "KeyPerformanceIndicator", new { area = "HC", GUID = _GUID }, this.Request.Url.Scheme);
            var currURLOpen = Url.Action(currMenu, "KeyPerformanceIndicator", new { area = "HC" }, this.Request.Url.Scheme);
            var documentID = 1;
            var emailList = db.Approval_Master.Where(w => w.Menu_Id == 40 && w.Document_Id == documentID && w.Dept_Code == deptCode && w.Levels == approval && w.Levels_Sub == approval_sub).Select(s => s.Users.Email).Distinct().ToList();
            
            if (currStatus != "Sign" && currStatus != "Comment")
            {
                emailList = db.Users.Where(w => w.NIK == currNIK).Select(s => s.Email).Distinct().ToList();
            }


            //Repalce [newusername] = signup user name   
            MailText = MailText.Replace("##document##", doc);
            MailText = MailText.Replace("##needs##", needs);
            MailText = MailText.Replace("##link##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##url##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##note##", note);
            MailText = MailText.Replace("##noteby##", currUserName);
            MailText = MailText.Replace("##linkOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##urlOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##guid##", Guid.NewGuid().ToString());

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
            var password = "100%NGKbusi!";
            var sub = "[Niterra-Portal-Notification]" + stat + " - Key Performance Indikator -" + currNIK + settlementSubject;
            var body = MailText;
            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,

                Credentials = new NetworkCredential(senderEmail.Address, password)
            };
            using (var mess = new MailMessage()
            {
                From = senderEmail,
                Subject = sub,
                Body = body,
                IsBodyHtml = true
            })
            {
                foreach (var dataEmail in emailList)
                {
                    if (dataEmail.Length > 0)
                    {
                        mess.To.Add(new MailAddress(dataEmail));
                    }
                }
                mess.Bcc.Add(new MailAddress("azis.abdillah@ngkbusi.com"));
                smtp.Send(mess);
            }
        }

        private bool FileExists(string remotePath, WebClient client)
        {
            try
            {
                client.DownloadData(remotePath);
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }
        public class DeliveryData
        {
            public List<DeliveryItem> data { get; set; }
        }
        private void DeleteFile(string remotePath, WebClient client)
        {
            client.UploadString(remotePath, WebRequestMethods.Ftp.DeleteFile, "");
        }
        public class DeliveryItem
        {
            public string perspective { get; set; }
            public string kpi { get; set; }
            public string last_achievement { get; set; }
            public string target { get; set; }
            public string action_plan { get; set; }
            public string primary_share { get; set; }
            public string type { get; set; }
            public float weight { get; set; }
            public float plan_apr { get; set; }
            public float plan_may { get; set; }
            public float plan_jun { get; set; }
            public float plan_jul { get; set; }
            public float plan_aug { get; set; }
            public float plan_sep { get; set; }
            public float plan_oct { get; set; }
            public float plan_nov { get; set; }
            public float plan_des { get; set; }
            public float plan_jan { get; set; }
            public float plan_feb { get; set; }
            public float plan_mar { get; set; }
            public float act_apr { get; set; }
            public float act_may { get; set; }
            public float act_jun { get; set; }
            public float act_jul { get; set; }
            public float act_aug { get; set; }
            public float act_sep { get; set; }
            public float act_oct { get; set; }
            public float act_nov { get; set; }
            public float act_des { get; set; }
            public float act_jan { get; set; }
            public float act_feb { get; set; }
            public float act_mar { get; set; }
            public float percent_apr { get; set; }
            public float percent_may { get; set; }
            public float percent_jun { get; set; }
            public float percent_jul { get; set; }
            public float percent_aug { get; set; }
            public float percent_sep { get; set; }
            public float percent_oct { get; set; }
            public float percent_nov { get; set; }
            public float percent_des { get; set; }
            public float percent_jan { get; set; }
            public float percent_feb { get; set; }
            public float percent_mar { get; set; }
            public float plan_q1 { get; set; }
            public float plan_q2 { get; set; }
            public float plan_q3 { get; set; }
            public float plan_q4 { get; set; }
            public float act_q1 { get; set; }
            public float act_q2 { get; set; }
            public float act_q3 { get; set; }
            public float act_q4 { get; set; }
            public float percent_q1 { get; set; }
            public float percent_q2 { get; set; }
            public float percent_q3 { get; set; }
            public float percent_q4 { get; set; }
            public float total { get; set; }
            public string status { get; set; }
            public string remark { get; set; }
        }

        public FileResult DownloadTemplate()
        {
            string fileName = "KPI_Template.xlsm";
            //Build the File Path.
            string path = Server.MapPath("~/Files/HC/KPI/") + fileName;

            //Read the File data into Byte Array.
            byte[] bytes = System.IO.File.ReadAllBytes(path);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", fileName);
        }
    }
}