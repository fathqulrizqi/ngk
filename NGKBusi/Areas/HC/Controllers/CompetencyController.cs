using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;

namespace NGKBusi.Areas.HC.Controllers
{
    [Authorize]
    public class CompetencyController : Controller
    {
        CompetencyMapConnection dbCM = new CompetencyMapConnection();
        DefaultConnection db = new DefaultConnection();
        // GET: HC/Competency
        public ActionResult Map()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData?.DivisionName;
            var currDeptName = currUserData?.DeptName;
            var currSectName = currUserData?.SectionName;
            var currCostName = currUserData?.CostName;

            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 107
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }
            //all Competency map access to Agnes, Kiswoyo & Yogi
            if (currUserID == "834.10.19" || currUserID == "P070624" || currUserID == "592.02.10" || currUserID == "853.07.21")
            {
                //ViewBag.DeptList = db.V_Users_Active.Where(w => w.TitleName != null && w.TitleName != "" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 3) != "PKL" && w.NIK != "SCReward").AsEnumerable().GroupBy(g => new { g.DivisionName, g.DeptName, g.SectionName, g.CostName, g.TitleName, g.PositionName }).Select(s => new V_Users_Active { DivisionName = s.Key.DivisionName, DeptName = s.Key.DeptName, SectionName = s.Key.SectionName, CostName = s.Key.CostName, TitleName = s.Key.TitleName, PositionName = s.Key.PositionName }).ToList();
                ViewBag.DeptList = dbCM.HC_Competency_JobTitle.ToList();
            }
            else
            {
                //ViewBag.DeptList = db.V_Users_Active.Where(w => w.DeptName.ToLower() == currDeptName.ToLower() && w.TitleName != null && w.TitleName != "" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 3) != "PKL" && w.NIK != "SCReward").AsEnumerable().GroupBy(g => new { g.DivisionName, g.DeptName, g.SectionName, g.CostName, g.TitleName, g.PositionName }).Select(s => new V_Users_Active { DivisionName = s.Key.DivisionName, DeptName = s.Key.DeptName, SectionName = s.Key.SectionName, CostName = s.Key.CostName, TitleName = s.Key.TitleName, PositionName = s.Key.PositionName }).ToList();
                ViewBag.DeptList = dbCM.HC_Competency_JobTitle.Where(w => w.DeptName.ToLower() == currDeptName.ToLower()).ToList();
            }

            ViewBag.NavHide = true;

            return View();
        }

        public ActionResult MapAll()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData?.DivisionName;
            var currDeptName = currUserData?.DeptName;
            var currSectName = currUserData?.SectionName;
            var currCostName = currUserData?.CostName;

            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 1038
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }
            //ViewBag.DeptList = db.V_Users_Active.Where(w => w.TitleName != null && w.TitleName != "" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 3) != "PKL" && w.NIK != "SCReward").AsEnumerable().GroupBy(g => new { g.DivisionName, g.DeptName, g.SectionName, g.CostName, g.TitleName }).Select(s => new V_Users_Active { DivisionName = s.Key.DivisionName, DeptName = s.Key.DeptName, SectionName = s.Key.SectionName, CostName = s.Key.CostName, TitleName = s.Key.TitleName }).ToList();
            ViewBag.DeptList = dbCM.HC_Competency_JobTitle.ToList();

            ViewBag.NavHide = true;

            return View("Map");
        }
        public ActionResult Result()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData?.DivisionName;
            var currDeptName = currUserData?.DeptName;
            var currSectName = currUserData?.SectionName;
            var currGUID = Request["GUID"];
            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iCRPeriod"]) ? Request["iCRPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 10 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            ViewBag.Period = periodFY;


            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 107
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            ViewBag.DeptList = db.V_Users_Active.Where(w => w.DeptName.ToLower() == currDeptName.ToLower() && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK != "SCReward").AsEnumerable().Select(s => new Users { DeptName = s.DeptName, SectionName = s.SectionName, PositionName = s.PositionName }).Distinct().ToList();

            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking < currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            if (currUserID == "822.01.19")
            {
                subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking <= currUserData.Users_Position.Position_Ranking).Select(s => s.Position_Name).ToArray();
            }

            var DataList = dbCM.HC_Competency_Result_Header
                .Where(w => (subOrdinate.Contains(w.PostName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            var MemberList = db.V_Users_Active.Where(w => (subOrdinate.Contains(w.PositionName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            string[] position0 = { "FOREMAN" };
            string[] position1 = { "SUPERVISOR" };
            string[] position2 = { "ASSISTANT MANAGER", "ASSISTANT MANAGER (ACTING)", "MANAGER", "MANAGER (ACTING)" };
            string[] position3 = { "SENIOR MANAGER", "SENIOR MANAGER (ACTING)", "DEPUTY GENERAL MANAGER", "DEPUTY GENERAL MANAGER ACTING", "GENERAL MANAGER" };
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
                DataList = DataList.Where(w => w.SectionName == currSectName);
                MemberList = MemberList.Where(w => w.SectionName == currSectName);
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
                    DataList = DataList.Where(w => (w.DivName == currDivName && w.DeptName != "SALES - OEM & OES") || (w.NIK == "663.01.14" || w.NIK == "665.11.13"));
                    MemberList = MemberList.Where(w => (w.DivisionName == currDivName && w.DeptName != "SALES - OEM & OES") || (w.NIK == "663.01.14" || w.NIK == "665.11.13"));
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
            var DataNIK = DataList.Where(w => w.Period_FY == periodFY).Select(s => s.NIK).ToArray();
            MemberList = MemberList.Where(w => !DataNIK.Contains(w.NIK));
            ViewBag.DataList = DataList.Where(w => w.Period_FY == periodFY).OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberList = MemberList.OrderByDescending(o => o.DivisionName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.CurrData = dbCM.HC_Competency_Result_Header.Where(w => w.GUID == currGUID).FirstOrDefault();

            // Build the list of selectable periods (as 4-digit years), starting from year 2026 up to the current fiscal year.
            // Japanese fiscal year: new FY starts in April, so from April onwards the FY rolls over to (currentYear + 1)
            var currentPeriodYear = DateTime.Now.Month < 4 ? DateTime.Now.Year : DateTime.Now.Year + 1;
            var periodList = new List<int>();
            for (var y = 2026; y <= currentPeriodYear; y++)
            {
                periodList.Add(y);
            }
            ViewBag.PeriodList = periodList;

            ViewBag.NavHide = true;

            return View();
        }

        public ActionResult ResultAll()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserData = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currDivName = currUserData?.DivisionName;
            var currDeptName = currUserData?.DeptName;
            var currSectName = currUserData?.SectionName;
            var currGUID = Request["GUID"];
            var periodFY = "FY1" + (!String.IsNullOrEmpty(Request["iCRPeriod"]) ? Request["iCRPeriod"].ToString().Substring(2, 2) : (DateTime.Now.Month < 10 ? (DateTime.Now.Year).ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2)));
            ViewBag.Period = periodFY;


            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 1039
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            ViewBag.DeptList = db.V_Users_Active.Where(w => w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK != "SCReward").AsEnumerable().Select(s => new Users { DeptName = s.DeptName, SectionName = s.SectionName, PositionName = s.PositionName }).Distinct().ToList();
            // 20 -> 9A (DGM)
            var subOrdinate = db.V_Users_Position.Where(w => w.Position_Ranking <= 20).Select(s => s.Position_Name).ToArray();
            

            var DataList = dbCM.HC_Competency_Result_Header
                .Where(w => (subOrdinate.Contains(w.PostName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");
            var MemberList = db.V_Users_Active.Where(w => (subOrdinate.Contains(w.PositionName) || w.NIK == currUserID) && w.NIK.Substring(0, 1) != "P" && w.NIK.Substring(0, 3) != "NGK" && w.NIK.Substring(0, 3) != "EXP" && w.NIK.Substring(0, 3) != "BOD" && w.NIK.Substring(0, 1) != "K" && w.NIK.Substring(0, 1) != "M" && w.NIK != "SCReward");

            var DataNIK = DataList.Where(w => w.Period_FY == periodFY).Select(s => s.NIK).ToArray();
            MemberList = MemberList.Where(w => !DataNIK.Contains(w.NIK));
            ViewBag.DataList = DataList.Where(w => w.Period_FY == periodFY).OrderByDescending(o => o.DivName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.MemberList = MemberList.OrderByDescending(o => o.DivisionName).ThenByDescending(o => o.DeptName).OrderByDescending(o => o.SectionName).ToList();
            ViewBag.CurrData = dbCM.HC_Competency_Result_Header.Where(w => w.GUID == currGUID).FirstOrDefault();

            // Build the list of selectable periods (as 4-digit years), starting from year 2026 up to the current fiscal year.
            // Japanese fiscal year: new FY starts in April, so from April onwards the FY rolls over to (currentYear + 1)
            var currentPeriodYear = DateTime.Now.Month < 4 ? DateTime.Now.Year : DateTime.Now.Year + 1;
            var periodList = new List<int>();
            for (var y = 2026; y <= currentPeriodYear; y++)
            {
                periodList.Add(y);
            }
            ViewBag.PeriodList = periodList;

            ViewBag.NavHide = true;

            return View("Result");
        }
        public class competencyMapData
        {
            public List<object[]> MapData { get; set; }
            public String MapMergeCells { get; set; }
        }

        public JsonResult getMapData()
        {
            var _Division = Request["iDivision"];
            var _Department = Request["iDepartment"];
            var _Section = Request["iSection"];
            var _CostName = Request["iCostName"];
            var _Title = Request["iTitleName"];
            var _Position = Request["iPosition"];
            competencyMapData data = new competencyMapData();
            var _mapData = new List<object[]>();
            _mapData.Add(new object[] { "A. Pengetahuan Teknis / Technical Knowledge", null, null, null, null, null, null, null, null, null, null });
            _mapData.Add(new object[] { 1, null, null, null, null, null, null, null, null, null, null });
            _mapData.Add(new object[] { "B. Kemampuan Praktik / Practical Skill", null, null, null, null, null, null, null, null, null, null });
            _mapData.Add(new object[] { 1, null, null, null, null, null, null, null, null, null, null });
            _mapData.Add(new object[] { "C. Perilaku / Behaviour", null, null, null, null, null, null, null, null, null, null });
            _mapData.Add(new object[] { 1, null, null, null, null, null, null, null, null, null, null });
            data.MapData = _mapData;
            data.MapMergeCells = "[{\"row\":0,\"col\":0,\"rowspan\":1,\"colspan\":11},{\"row\":2,\"col\":0,\"rowspan\":1,\"colspan\":11},{\"row\":4,\"col\":0,\"rowspan\":1,\"colspan\":11}]";

            var checkData = dbCM.HC_Competency_Map_Header.Where(w => w.Division == _Division && w.Department == _Department && w.Section == _Section && w.CostName == _CostName && w.TitleName == _Title).FirstOrDefault();
            if (checkData != null)
            {
                var _data = dbCM.HC_Competency_Map_Line.Where(w => w.Header_ID == checkData.ID).OrderBy(o => o.Idx).Select(s => new { s.No, s.Requirement, s.Score, s.Module, s.Internal, s.Internal_Duration, s.External, s.External_Duration, s.Trainer, s.Evaluation_Method, s.Remark }).ToList();
                if (_data.Count() > 0)
                {
                    var newdata = new List<object[]>();
                    for (var i = 0; i < _data.Count(); i++)
                    {
                        newdata.Add(new object[] { _data[i].No, _data[i].Requirement, _data[i].Score, _data[i].Module, _data[i].Internal, _data[i].Internal_Duration, _data[i].External, _data[i].External_Duration, _data[i].Trainer, _data[i].Evaluation_Method, _data[i].Remark });
                    }
                    data.MapData = newdata;
                    data.MapMergeCells = checkData.MergeCells;

                    return Json(data, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult setMapData(string iDivision, string iDepartment, string iSection, string iCostName, string iTitleName, string iPosition, string[][] iData, string iMergeCells)
        {
            var _Division = iDivision;
            var _Department = iDepartment;
            var _Section = iSection;
            var _CostName = iCostName;
            var _Title = iTitleName;
            var _Position = iPosition;
            var _data = iData;
            var _mergecells = iMergeCells;
            var headerID = 0;

            var checkHeaderData = dbCM.HC_Competency_Map_Header.Where(w => w.Division == _Division && w.Department == _Department && w.Section == _Section && w.CostName == _CostName && w.TitleName == _Title).FirstOrDefault();
            if (checkHeaderData == null)
            {
                var newHeaderData = new HC_Competency_Map_Header();
                newHeaderData.GUID = Guid.NewGuid().ToString();
                newHeaderData.Division = _Division;
                newHeaderData.Department = _Department;
                newHeaderData.Section = _Section;
                newHeaderData.CostName = _CostName;
                newHeaderData.TitleName = _Title;
                newHeaderData.Job_Position = _Position;
                newHeaderData.MergeCells = _mergecells;
                dbCM.HC_Competency_Map_Header.Add(newHeaderData);
                dbCM.SaveChanges();
                headerID = newHeaderData.ID;
            }
            else
            {
                checkHeaderData.MergeCells = _mergecells;
                headerID = checkHeaderData.ID;
            }
            List<int> _hashList = new List<int>();
            var __idx = 1;
            foreach (var data in _data)
            {
                var _hash = (__idx + data[0] + data[1] + data[2] + data[3] + data[4] + data[5] + data[6] + data[7] + data[8] + data[9] + data[10]).GetHashCode();

                __idx++;
                _hashList.Add(_hash);
            }

            var checkLineList = dbCM.HC_Competency_Map_Line.Where(w => w.Header_ID == headerID && !_hashList.Contains(w.HashCode)).ToList();
            dbCM.HC_Competency_Map_Line.RemoveRange(checkLineList);

            var _idx = 1;
            foreach (var data in _data)
            {
                var _hash = (_idx + data[0] + data[1] + data[2] + data[3] + data[4] + data[5] + data[6] + data[7] + data[8] + data[9] + data[10]).GetHashCode();
                var checkLineData = dbCM.HC_Competency_Map_Line.Where(w => w.Header_ID == headerID && w.HashCode == _hash).FirstOrDefault();
                if (checkLineData != null)
                {
                    checkLineData.No = data[0] == "" ? null : data[0];
                    checkLineData.Requirement = data[1] == "" ? null : data[1];
                    checkLineData.Score = data[2] == "" ? (int?)null : int.Parse(data[2]);
                    checkLineData.Module = data[3] == "" ? null : data[3];
                    checkLineData.Internal = data[4] == "" ? null : data[4];
                    checkLineData.Internal_Duration = data[5] == "" ? (int?)null : int.Parse(data[5]);
                    checkLineData.External = data[6] == "" ? null : data[6];
                    checkLineData.External_Duration = data[7] == "" ? (int?)null : int.Parse(data[7]);
                    checkLineData.Trainer = data[8] == "" ? null : data[8];
                    checkLineData.Evaluation_Method = data[9] == "" ? null : data[9];
                    checkLineData.Remark = data[10] == "" ? null : data[10];
                    checkLineData.Idx = _idx;
                    checkLineData.HashCode = _hash;
                }
                else
                {
                    var newLineData = new HC_Competency_Map_Line();
                    newLineData.Header_ID = headerID;
                    newLineData.No = data[0] == "" ? null : data[0];
                    newLineData.Requirement = data[1] == "" ? null : data[1];
                    newLineData.Score = data[2] == "" ? (int?)null : int.Parse(data[2]);
                    newLineData.Module = data[3] == "" ? null : data[3];
                    newLineData.Internal = data[4] == "" ? null : data[4];
                    newLineData.Internal_Duration = data[5] == "" ? (int?)null : int.Parse(data[5]);
                    newLineData.External = data[6] == "" ? null : data[6];
                    newLineData.External_Duration = data[7] == "" ? (int?)null : int.Parse(data[7]);
                    newLineData.Trainer = data[8] == "" ? null : data[8];
                    newLineData.Evaluation_Method = data[9] == "" ? null : data[9];
                    newLineData.Remark = data[10] == "" ? null : data[10];
                    newLineData.Idx = _idx;
                    newLineData.HashCode = _hash;
                    dbCM.HC_Competency_Map_Line.Add(newLineData);
                }
                _idx++;

            }
            dbCM.SaveChanges();
            return Json(_data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getResultData()
        {
            var _PeriodFY = Request["iPeriodFY"];
            var _NIK = Request["iNIK"];
            var _Name = Request["iName"];
            var _Division = Request["iDivision"];
            var _Department = Request["iDepartment"];
            var _Section = Request["iSection"];
            var _CostName = Request["iCostName"];
            var _Position = Request["iPosition"];
            var _TitleName = Request["iTitleName"];
            var _Competency = Request["iCompetency"];

            List<object[]> _ResultData = new List<object[]>();
            switch (_Competency)
            {
                case "B":
                    _ResultData.Add(new object[] { "B. Kemampuan Praktik / Practical Skill", null, null, null, null, null, null, null, null, null, null, null });
                    _ResultData.Add(new object[] { 1, null, null, null, null, null, null, null, null, null, null, null });
                    break;
                case "C":
                    _ResultData.Add(new object[] { "C. Perilaku / Behaviour", null, null, null, null, null, null, null, null, null, null, null });
                    _ResultData.Add(new object[] { 1, null, null, null, null, null, null, null, null, null, null, null });
                    break;
                default:
                    _ResultData.Add(new object[] { "A. Pengetahuan Teknis / Technical Knowledge", null, null, null, null, null, null, null, null, null, null, null });
                    _ResultData.Add(new object[] { 1, null, null, null, null, null, null, null, null, null, null, null });
                    break;
            }


            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString());
            con.Open();

            SqlCommand cmd = new SqlCommand("sp_CompetencyResult", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@periodFY", _PeriodFY);
            cmd.Parameters.AddWithValue("@NIK", _NIK);
            cmd.Parameters.AddWithValue("@divName", _Division);
            cmd.Parameters.AddWithValue("@deptName", _Department);
            cmd.Parameters.AddWithValue("@sectName", _Section);
            cmd.Parameters.AddWithValue("@costName", _CostName);
            //cmd.Parameters.AddWithValue("@positionName", _Position.Replace(" (ACTING)", ""));
            cmd.Parameters.AddWithValue("@titleName", _TitleName);
            cmd.Parameters.AddWithValue("@tableType", _Competency);
            cmd.CommandTimeout = 30000;
            DataTable dt = new DataTable();
            List<string> resultData = new List<string>();
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                _ResultData.Clear();
                var idx = 1;
                foreach (DataRow row in dt.Rows)
                {
                    _ResultData.Add(new object[] { row["No"].ToString(), row["Requirement"].ToString(), row["Standard_Score"].ToString(), checkResultScore(row["Result5"].ToString()), checkResultScore(row["Result4"].ToString()), checkResultScore(row["Result3"].ToString()), checkResultScore(row["Result2"].ToString()), checkResultScore(row["Result1"].ToString()), checkResultScore(row["Result0"].ToString()), row["Score"]?.ToString(), "=IF(OR(J" + @idx + " < C" + @idx + ",J" + @idx + "=\"\"),\"UNFIT\",\"FIT\")", row["Note"].ToString() });
                    idx++;
                }
            }


            //var result = JsonConvert.SerializeObject(dt,new JsonSerializerSettings{ReferenceLoopHandling = ReferenceLoopHandling.Ignore});

            return Json(_ResultData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult setResultData(string iPeriodFY, string iNIK, string iName, string iDivision, string iDepartment, string iSection, string iCostName, string iTitleName, string iPosition, string[][][] iData)
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var _PeriodFY = iPeriodFY;
            var _NIK = iNIK;
            var _Name = iName;
            var _Division = iDivision;
            var _Department = iDepartment;
            var _Section = iSection;
            var _CostName = iCostName;
            var _Title = iTitleName;
            var _Position = iPosition;
            var _data = iData;
            var currYear = DateTime.Now.Year;
            var joinYear = int.Parse(_NIK.Substring(_NIK.Length - 2)) > int.Parse(currYear.ToString().Substring(currYear.ToString().Length - 2)) ? int.Parse((currYear - 1).ToString().Substring(0, 2) + _NIK.Substring(_NIK.Length - 2).ToString()) : int.Parse((currYear).ToString().Substring(0, 2) + _NIK.Substring(_NIK.Length - 2).ToString());
            var JoinMonth = int.Parse(_NIK.ToString().Substring(4, 2));
            var joinDate = new DateTime(joinYear, JoinMonth, 1);

            var headerID = 0;

            var checkHeaderData = dbCM.HC_Competency_Result_Header.Where(w => w.Period_FY == _PeriodFY && w.NIK == _NIK && w.DivName == _Division && w.DeptName == _Department && w.SectionName == _Section && w.CostName == _CostName && w.PostName == _Position && w.TitleName == _Title).FirstOrDefault();
            if (checkHeaderData == null)
            {
                var newHeaderData = new HC_Competency_Result_Header();
                newHeaderData.GUID = Guid.NewGuid().ToString();
                newHeaderData.Period_FY = _PeriodFY;
                newHeaderData.NIK = _NIK;
                newHeaderData.Name = _Name;
                newHeaderData.DivName = _Division;
                newHeaderData.DeptName = _Department;
                newHeaderData.SectionName = _Section;
                newHeaderData.CostName = _CostName;
                newHeaderData.TitleName = _Title;
                newHeaderData.PostName = _Position;
                newHeaderData.Join_Date = joinDate;
                newHeaderData.Working_Years = (int)(DateTime.Now - joinDate).Days / 365;
                newHeaderData.Created_At = DateTime.Now;
                newHeaderData.Created_By = currUserID;
                dbCM.HC_Competency_Result_Header.Add(newHeaderData);
                dbCM.SaveChanges();
                headerID = newHeaderData.ID;
            }
            else
            {
                headerID = checkHeaderData.ID;
            }
            List<int> _hashList = new List<int>();
            var __idx = 1;
            foreach (var data in _data)
            {
                foreach (var subData in data)
                {
                    var _hash = (__idx + subData[0] + subData[1] + subData[2] + subData[3] + subData[4] + subData[5] + subData[6] + subData[7] + subData[8] + subData[9] + subData[10] + subData[11]).GetHashCode();

                    __idx++;
                    _hashList.Add(_hash);
                }
            }

            var checkLineList = dbCM.HC_Competency_Result_Line.Where(w => w.Header_ID == headerID && !_hashList.Contains(w.HashCode)).ToList();
            dbCM.HC_Competency_Result_Line.RemoveRange(checkLineList);

            var _idx = 1;
            foreach (var data in _data)
            {
                var tableType = data[0][0].ToString().Substring(0, 1);
                foreach (var subData in data)
                {
                    var _hash = (_idx + subData[0] + subData[1] + subData[2] + subData[3] + subData[4] + subData[5] + subData[6] + subData[7] + subData[8] + subData[9] + subData[10] + subData[11]).GetHashCode();
                    var checkLineData = dbCM.HC_Competency_Result_Line.Where(w => w.Header_ID == headerID && w.HashCode == _hash).FirstOrDefault();
                    if (checkLineData != null)
                    {
                        checkLineData.Type = tableType;
                        checkLineData.No = subData[0] == "" ? null : subData[0];
                        checkLineData.Requirement = subData[1] == "" ? null : subData[1];
                        checkLineData.Standard_Score = subData[2] == "" ? (int?)null : int.Parse(subData[2]);
                        checkLineData.Score = subData[9] == "" ? (int?)null : int.Parse(subData[9]);
                        checkLineData.Result = subData[9] != "" && subData[2] != "" ? ((int.Parse(subData[9]) >= int.Parse(subData[2])) ? "FIT" : "UNFIT") : "";
                        checkLineData.Note = subData[11] == "" ? null : subData[11];
                        checkLineData.Idx = _idx;
                        checkLineData.HashCode = _hash;
                    }
                    else
                    {
                        var newLineData = new HC_Competency_Result_Line();
                        newLineData.Header_ID = headerID;
                        newLineData.Type = tableType;
                        newLineData.No = subData[0] == "" ? null : subData[0];
                        newLineData.Requirement = subData[1] == "" ? null : subData[1];
                        newLineData.Standard_Score = subData[2] == "" ? (int?)null : int.Parse(subData[2]);
                        newLineData.Score = subData[9] == "" ? (int?)null : int.Parse(subData[9]);
                        newLineData.Result = subData[9] != "" && subData[2] != "" ? ((int.Parse(subData[9]) >= int.Parse(subData[2])) ? "FIT" : "UNFIT") : "";
                        newLineData.Note = subData[11] == "" ? null : subData[11];
                        newLineData.Idx = _idx;
                        newLineData.HashCode = _hash;
                        dbCM.HC_Competency_Result_Line.Add(newLineData);
                    }
                    _idx++;
                }
            }
            dbCM.SaveChanges();
            return Json(_data, JsonRequestBehavior.AllowGet);
        }

        public bool checkResultScore(string Score)
        {
            if (int.Parse(Score) == 1)
            {
                return true;
            }

            return false;
        }

        public JsonResult RefreshResultMap(string iPeriodFY, string iNIK, string iName, string iDivision, string iDepartment, string iSection, string iCostName, string iTitleName, string iPosition)
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();

            var checkMapHeaderData = dbCM.HC_Competency_Map_Header.Where(w => w.Division == iDivision && w.Department == iDepartment && w.Section == iSection && w.CostName == iCostName && w.TitleName == iTitleName).FirstOrDefault();
            if (checkMapHeaderData == null)
            {
                return Json(new { success = false, message = "Competency Map not found for this Job Title." });
            }

            var mapLines = dbCM.HC_Competency_Map_Line.Where(w => w.Header_ID == checkMapHeaderData.ID).OrderBy(o => o.Idx).ToList();

            // Determine the competency type (A/B/C) each map line belongs to, based on the section header rows.
            // The header/type-label rows themselves (e.g. "A. Pengetahuan Teknis / Technical Knowledge") must be
            // kept as part of mapItems since they are also persisted as rows in HC_Competency_Result_Line
            // (saved from the Handsontable's first row) - skipping them here would cause them to be
            // incorrectly treated as obsolete and deleted during cleanup.
            var currentType = "A";
            var mapItems = new List<Tuple<HC_Competency_Map_Line, string>>();
            foreach (var line in mapLines)
            {
                if (string.IsNullOrEmpty(line.Requirement) && !string.IsNullOrEmpty(line.No) && (line.No.StartsWith("A.") || line.No.StartsWith("B.") || line.No.StartsWith("C.")))
                {
                    currentType = line.No.Substring(0, 1);
                    mapItems.Add(new Tuple<HC_Competency_Map_Line, string>(line, currentType));
                    continue;
                }
                if (string.IsNullOrEmpty(line.Requirement))
                {
                    // Filler/placeholder rows (e.g. No == "1" with no Requirement) are not real data, skip them.
                    continue;
                }
                mapItems.Add(new Tuple<HC_Competency_Map_Line, string>(line, currentType));
            }

            var resultHeaderData = dbCM.HC_Competency_Result_Header.Where(w => w.Period_FY == iPeriodFY && w.NIK == iNIK && w.DivName == iDivision && w.DeptName == iDepartment && w.SectionName == iSection && w.CostName == iCostName && w.PostName == iPosition && w.TitleName == iTitleName).FirstOrDefault();
            if (resultHeaderData == null)
            {
                var currYear = DateTime.Now.Year;
                var joinYear = int.Parse(iNIK.Substring(iNIK.Length - 2)) > int.Parse(currYear.ToString().Substring(currYear.ToString().Length - 2)) ? int.Parse((currYear - 1).ToString().Substring(0, 2) + iNIK.Substring(iNIK.Length - 2)) : int.Parse((currYear).ToString().Substring(0, 2) + iNIK.Substring(iNIK.Length - 2));
                var joinMonth = int.Parse(iNIK.Substring(4, 2));
                var joinDate = new DateTime(joinYear, joinMonth, 1);

                var newHeaderData = new HC_Competency_Result_Header();
                newHeaderData.GUID = Guid.NewGuid().ToString();
                newHeaderData.Period_FY = iPeriodFY;
                newHeaderData.NIK = iNIK;
                newHeaderData.Name = iName;
                newHeaderData.DivName = iDivision;
                newHeaderData.DeptName = iDepartment;
                newHeaderData.SectionName = iSection;
                newHeaderData.CostName = iCostName;
                newHeaderData.TitleName = iTitleName;
                newHeaderData.PostName = iPosition;
                newHeaderData.Join_Date = joinDate;
                newHeaderData.Working_Years = (int)(DateTime.Now - joinDate).Days / 365;
                newHeaderData.Created_At = DateTime.Now;
                newHeaderData.Created_By = currUserID;
                dbCM.HC_Competency_Result_Header.Add(newHeaderData);
                dbCM.SaveChanges();
                resultHeaderData = newHeaderData;
            }

            var headerID = resultHeaderData.ID;
            var resultLines = dbCM.HC_Competency_Result_Line.Where(w => w.Header_ID == headerID).ToList();

            // Sync existing/matching items, add new ones, and re-align Idx/HashCode with the Map's current order
            var idx = 1;
            foreach (var item in mapItems)
            {
                var mapLine = item.Item1;
                var type = item.Item2;
                var existing = resultLines.FirstOrDefault(w => w.No == mapLine.No && w.Requirement == mapLine.Requirement && w.Type == type);
                var newHash = (idx + mapLine.No + mapLine.Requirement + type).GetHashCode();
                if (existing == null)
                {
                    var newLineData = new HC_Competency_Result_Line();
                    newLineData.Header_ID = headerID;
                    newLineData.Type = type;
                    newLineData.No = mapLine.No;
                    newLineData.Requirement = mapLine.Requirement;
                    newLineData.Standard_Score = mapLine.Score;
                    newLineData.Score = null;
                    newLineData.Result = "";
                    newLineData.Note = null;
                    newLineData.Idx = idx;
                    newLineData.HashCode = newHash;
                    dbCM.HC_Competency_Result_Line.Add(newLineData);
                }
                else
                {
                    if (existing.Standard_Score != mapLine.Score)
                    {
                        existing.Standard_Score = mapLine.Score;
                    }
                    if (existing.Idx != idx)
                    {
                        existing.Idx = idx;
                    }
                    if (existing.HashCode != newHash)
                    {
                        existing.HashCode = newHash;
                    }
                }
                idx++;
            }

            // Delete result lines that no longer exist in the map (master)
            var linesToRemove = resultLines.Where(rl => !mapItems.Any(mi => mi.Item1.No == rl.No && mi.Item1.Requirement == rl.Requirement && mi.Item2 == rl.Type)).ToList();
            if (linesToRemove.Count > 0)
            {
                dbCM.HC_Competency_Result_Line.RemoveRange(linesToRemove);
            }

            dbCM.SaveChanges();

            return Json(new { success = true, message = "Result Map refreshed successfully." });
        }
    }
}