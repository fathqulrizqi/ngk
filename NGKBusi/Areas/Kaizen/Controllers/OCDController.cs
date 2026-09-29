using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;

namespace NGKBusi.Areas.Kaizen.Controllers
{
    public class OCDController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: Kaizen/OCD
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult DataList_()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Kaizen_Group_User.Where(z => z.userNIK == currUserID && z.groupID == 1).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }

            Int32 period = (Request["iPeriod"] != null ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 4 ? DateTime.Now.Year - 1 : DateTime.Now.Year));
            ViewBag.Period = period;
            var rangeStart = new DateTime(period, 4, 1);
            var rangeEnd = new DateTime(period + 1, 4, 1);
            ViewBag.kaizenList = db.Kaizen_Data.Where(w => (w.issuedDate >= rangeStart && w.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
            ViewBag.userList = db.V_Users_Active.ToList();
            ViewBag.OCDList = db.Kaizen_Score_Categories.Where(x => x.groupID == 1).ToList();
            ViewBag.KOCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 2).ToList();
            ViewBag.SCList = db.Kaizen_Score_Categories.Where(x => x.groupID == 3).ToList();
            ViewBag.NavHide = true;

            return View();
        }
        [Authorize]
        public ActionResult DataList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Kaizen_Group_User.Where(z => z.userNIK == currUserID && z.groupID == 1).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }

            Int32 period = (Request["iPeriod"] != null ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 4 ? DateTime.Now.Year - 1 : DateTime.Now.Year));
            ViewBag.Period = period;
            var rangeStart = new DateTime(period, 4, 1);
            var rangeEnd = new DateTime(period + 1, 4, 1);
            ViewBag.kaizenList = db.Kaizen_Data.Where(w => (w.issuedDate >= rangeStart && w.issuedDate < rangeEnd)).OrderByDescending(x => x.ID).ToList();
            ViewBag.userList = db.V_Users_Active.ToList();
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

        public String getAbbr(string divID, string deptID, string sectionID, string subSectionID)
        {
            String abbr;

            switch (divID)
            {
                case "02":
                    abbr = "HC";
                    switch (deptID)
                    {
                        case "02":
                            abbr = "HSE";
                            switch (sectionID)
                            {
                                case "02":
                                    abbr = "HSE";
                                    switch (subSectionID)
                                    {
                                        case "02":
                                        case "03":
                                        case "04":
                                            abbr = "HSE";
                                            break;
                                        default:
                                            abbr = "HSE";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "HSE";
                                    break;
                            }
                            break;
                        case "03":
                            abbr = "HRD";
                            switch (sectionID)
                            {
                                case "03":
                                    abbr = "HRD";
                                    switch (subSectionID)
                                    {
                                        case "05":
                                        case "06":
                                            abbr = "HRD";
                                            break;
                                        default:
                                            abbr = "HRD";
                                            break;
                                    }
                                    abbr = "HRD";
                                    break;
                                case "04":
                                    abbr = "GA";
                                    switch (subSectionID)
                                    {
                                        case "07":
                                        case "08":
                                        case "09":
                                            abbr = "GA";
                                            break;
                                        default:
                                            abbr = "GA";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "HC";
                                    break;
                            }
                            break;
                        default:
                            abbr = "HC";
                            break;
                    }
                    break;
                case "03":
                    abbr = "MKT";
                    switch (deptID)
                    {
                        case "04":
                            abbr = "MKT";
                            switch (sectionID)
                            {
                                case "05":
                                    abbr = "MKT";
                                    switch (subSectionID)
                                    {
                                        case "10":
                                            abbr = "OEM";
                                            break;
                                        case "11":
                                            abbr = "OES";
                                            break;
                                        default:
                                            abbr = "MKT";
                                            break;
                                    }
                                    break;
                                case "06":
                                    abbr = "MKT";
                                    switch (subSectionID)
                                    {
                                        case "12":
                                            abbr = "AMP";
                                            break;
                                        default:
                                            abbr = "MKT";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "MKT";
                                    break;
                            }
                            break;
                        case "05":
                            abbr = "MKT";
                            switch (sectionID)
                            {
                                case "07":
                                    abbr = "MKT";
                                    switch (subSectionID)
                                    {
                                        case "13":
                                            abbr = "MKT";
                                            break;
                                        default:
                                            abbr = "MKT";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "MKT";
                                    break;
                            }
                            break;
                        case "06":
                            abbr = "MKT";
                            switch (sectionID)
                            {
                                case "08":
                                    abbr = "MKT";
                                    switch (subSectionID)
                                    {
                                        case "15":
                                            abbr = "SA";
                                            break;
                                        default:
                                            abbr = "SA";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "MKT";
                                    break;
                            }
                            break;
                        default:
                            abbr = "MKT";
                            break;
                    }
                    break;
                case "04":
                    abbr = "FA";
                    switch (deptID)
                    {
                        case "07":
                            abbr = "IT";
                            switch (sectionID)
                            {
                                case "09":
                                    abbr = "IT";
                                    switch (subSectionID)
                                    {
                                        case "16":
                                            abbr = "IT";
                                            break;
                                        default:
                                            abbr = "IT";
                                            break;
                                    }
                                    break;
                                case "10":
                                    abbr = "IT";
                                    switch (subSectionID)
                                    {
                                        case "17":
                                            abbr = "IT";
                                            break;
                                        default:
                                            abbr = "IT";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "IT";
                                    break;
                            }
                            break;

                        case "08":
                            abbr = "FA";
                            switch (sectionID)
                            {
                                case "11":
                                    abbr = "FA";
                                    switch (subSectionID)
                                    {
                                        case "18":
                                        case "19":
                                        case "20":
                                        case "21":
                                            abbr = "FA";
                                            break;
                                        default:
                                            abbr = "FA";
                                            break;
                                    }
                                    break;
                                case "12":
                                    abbr = "FA";
                                    switch (subSectionID)
                                    {
                                        case "22":
                                            abbr = "FA";
                                            break;
                                        default:
                                            abbr = "FA";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "FA";
                                    break;
                            }
                            break;
                        default:
                            abbr = "FA";
                            break;
                    }
                    break;
                case "05":
                    abbr = "PRD";
                    switch (deptID)
                    {
                        case "09":
                            abbr = "PRD";
                            switch (sectionID)
                            {
                                case "13":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "23":
                                            abbr = "CF";
                                            break;
                                        case "24":
                                            abbr = "CM";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                case "14":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "25":
                                            abbr = "WT";
                                            break;
                                        case "26":
                                            abbr = "MI";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                case "15":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "27":
                                            abbr = "PLT";
                                            break;
                                        case "28":
                                            abbr = "ASI";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "PRD";
                                    break;
                            }
                            break;
                        case "10":
                            abbr = "PRD";
                            switch (sectionID)
                            {
                                case "16":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "29":
                                            abbr = "ASA";
                                            break;
                                        case "30":
                                            abbr = "ASC";
                                            break;
                                        case "31":
                                            abbr = "ASB";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                case "17":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "32":
                                            abbr = "ASF";
                                            break;
                                        case "33":
                                            abbr = "ASO";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "PRD";
                                    break;
                            }
                            break;
                        case "11":
                            abbr = "PRD";
                            switch (sectionID)
                            {
                                case "18":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "34":
                                            abbr = "IJM";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                case "19":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "35":
                                            abbr = "APC";
                                            break;
                                        case "36":
                                            abbr = "PPC";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "PRD";
                                    break;
                            }
                            break;
                        case "14":
                            abbr = "PRD";
                            switch (sectionID)
                            {
                                case "22":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "39":
                                            abbr = "PA";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                case "23":
                                    abbr = "PRD";
                                    switch (subSectionID)
                                    {
                                        case "40":
                                            abbr = "PP";
                                            break;
                                        default:
                                            abbr = "PRD";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "PRD";
                                    break;
                            }
                            break;
                        default:
                            abbr = "PRD";
                            break;
                    }
                    break;
                case "07":
                    abbr = "FOS";
                    switch (deptID)
                    {
                        case "13":
                            abbr = "FOS";
                            switch (sectionID)
                            {
                                case "21":
                                    abbr = "FOS";
                                    switch (subSectionID)
                                    {
                                        case "38":
                                            abbr = "FOS";
                                            break;
                                        default:
                                            abbr = "FOS";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "FOS";
                                    break;
                            }
                            break;
                        default:
                            abbr = "FOS";
                            break;
                    }
                    break;
                case "08":
                    abbr = "SCM";
                    switch (deptID)
                    {
                        case "15":
                            abbr = "PRC";
                            switch (sectionID)
                            {
                                case "24":
                                case "25":
                                    abbr = "PRC";
                                    switch (subSectionID)
                                    {
                                        case "41":
                                        case "42":
                                        case "43":
                                        case "44":
                                            abbr = "PRC";
                                            break;
                                        default:
                                            abbr = "PRC";
                                            break;
                                    }
                                    break;
                                case "26":
                                    abbr = "IC";
                                    switch (subSectionID)
                                    {
                                        case "45":
                                            abbr = "IC";
                                            break;
                                        default:
                                            abbr = "IC";
                                            break;
                                    }
                                    break;
                                case "27":
                                    abbr = "LOG";
                                    switch (subSectionID)
                                    {
                                        case "46":
                                        case "47":
                                            abbr = "LOG";
                                            break;
                                        default:
                                            abbr = "LOG";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "PRC";
                                    break;
                            }
                            break;
                        default:
                            abbr = "SCM";
                            break;
                    }
                    break;
                case "09":
                    abbr = "PE";
                    switch (deptID)
                    {
                        case "17":
                            abbr = "PE";
                            switch (sectionID)
                            {
                                case "28":
                                    abbr = "PE";
                                    switch (subSectionID)
                                    {
                                        case "48":
                                            abbr = "PE";
                                            break;
                                        case "49":
                                            abbr = "WS";
                                            break;
                                        default:
                                            abbr = "PE";
                                            break;
                                    }
                                    break;
                                case "29":
                                    abbr = "IE";
                                    switch (subSectionID)
                                    {
                                        case "50":
                                            abbr = "IE";
                                            break;
                                        default:
                                            abbr = "IE";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "PE";
                                    break;
                            }
                            break;
                        case "18":
                            abbr = "MTN";
                            switch (sectionID)
                            {
                                case "30":
                                    abbr = "MTN";
                                    switch (subSectionID)
                                    {
                                        case "51":
                                            abbr = "MTN";
                                            break;
                                        default:
                                            abbr = "MTN";
                                            break;
                                    }
                                    break;
                                case "31":
                                    abbr = "MTN";
                                    switch (subSectionID)
                                    {
                                        case "52":
                                            abbr = "MTN";
                                            break;
                                        default:
                                            abbr = "MTN";
                                            break;
                                    }
                                    break;
                                case "32":
                                    abbr = "MUT";
                                    switch (subSectionID)
                                    {
                                        case "53":
                                            abbr = "MUT";
                                            break;
                                        default:
                                            abbr = "MUT";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "MTN";
                                    break;
                            }
                            break;
                        default:
                            abbr = "PE";
                            break;
                    }
                    break;
                case "10":
                    abbr = "QA";
                    switch (deptID)
                    {
                        case "19":
                            abbr = "QA";
                            switch (sectionID)
                            {
                                case "33":
                                    abbr = "QA";
                                    switch (subSectionID)
                                    {
                                        case "54":
                                        case "55":
                                        case "56":
                                            abbr = "QA";
                                            break;
                                        default:
                                            abbr = "QA";
                                            break;
                                    }
                                    break;
                                case "34":
                                    abbr = "QC";
                                    switch (subSectionID)
                                    {
                                        case "57":
                                            abbr = "QCI";
                                            break;
                                        default:
                                            abbr = "QA";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "QA";
                                    break;
                            }
                            break;
                        default:
                            abbr = "QA";
                            break;
                    }
                    break;
                case "11":
                    abbr = "MR";
                    switch (deptID)
                    {
                        case "20":
                            abbr = "MR";
                            switch (sectionID)
                            {
                                case "35":
                                    abbr = "MR";
                                    switch (subSectionID)
                                    {
                                        case "59":
                                            abbr = "MR";
                                            break;
                                        default:
                                            abbr = "MR";
                                            break;
                                    }
                                    break;
                                default:
                                    abbr = "MR";
                                    break;
                            }
                            break;
                        default:
                            abbr = "MR";
                            break;
                    }
                    break;
                default:
                    abbr = "ALL";
                    break;
            }

            return abbr;
        }

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

        [HttpPost]
        [Authorize]
        public ActionResult Add(HttpPostedFileBase iScan)
        {
            var regNo = Request["iRegNo"];
            var impType = Request["iImpType"];
            var NIK = Request["iNIK"];
            var Division = Request["iDivision"] != "-" ? Request["iDivision"] : null;
            var Dept = Request["iDept"] != "-" ? Request["iDept"] : null;
            var Section = Request["iSection"] != "-" ? Request["iSection"] : null;
            var SubSection = Request["iSubSection"] != "-" ? Request["iSubSection"] : null;
            var currDate = Request["iDate"];
            var title = Request["iTitle"];
            var area = (Request["iAreaOther"] != "" ? Request["iAreaOther"] : Request["iArea"]);
            var LineLeader = Request["iLineLeader"];

            double CostMaterial = 0, CostServices = 0, CostOther = 0, CostTotal = 0, BenefitQty = 0, BenefitProcess = 0, BenefitOther = 0, BenefitTotal = 0, CostBenefitTotal = 0;
            string CostOtherDesc = "", BenefitProductType = "", BenefitOtherDesc = "";
            int BenefitPeriod = 0, BenefitQtyPcs = 0, BenefitProcessTime = 0;

            if (Request["iCostBenefitTotal"] != null)
            {
                CostMaterial = double.Parse(Request["iCostMaterial"]);
                CostServices = double.Parse(Request["iCostServices"]);
                CostOtherDesc = Request["iCostOtherDesc"];
                CostOther = double.Parse(Request["iCostOther"]);
                CostTotal = double.Parse(Request["iCostTotal"]);
                BenefitProductType = Request["iBenefitProductType"];
                BenefitPeriod = int.Parse(Request["iBenefitPeriod"]);
                BenefitQtyPcs = int.Parse(Request["iBenefitQtyPcs"]);
                BenefitQty = double.Parse(Request["iBenefitQty"]);
                BenefitProcessTime = int.Parse(Request["iBenefitProcessTime"]);
                BenefitProcess = double.Parse(Request["iBenefitProcess"]);
                BenefitOtherDesc = Request["iBenefitOtherDesc"];
                BenefitOther = double.Parse(Request["iBenefitOther"]);
                BenefitTotal = double.Parse(Request["iBenefitTotal"]);
                CostBenefitTotal = double.Parse(Request["iCostBenefitTotal"]);
            }

            var currUser = (ClaimsIdentity)User.Identity;
            var curNIK = db.Users.Where(x => x.NIK == NIK).First();
            var abbr = getAbbr(curNIK.DivisionID, curNIK.DeptID, curNIK.SectionID, curNIK.SubSectionID);
            var checkRegNo = db.Kaizen_Data.Where(x => x.RegNo.Contains(DateTime.Now.Year.ToString())).OrderByDescending(x => x.ID).Select(x => x.RegNo).FirstOrDefault();
            var currRegNo = "";

            if (checkRegNo != null)
            {
                var currSequence = checkRegNo.ToString().Split('-');
                var seq = ("0000" + (Int32.Parse(currSequence[4]) + 1));
                currRegNo = abbr + "-" + DateTime.Now.ToString("dd") + "-" + toRomawi(DateTime.Now.ToString("MM")) + "-" + DateTime.Now.ToString("yyyy") + "-" + seq.Substring(seq.Length - 4);
            }
            else
            {
                currRegNo = abbr + "-" + DateTime.Now.ToString("dd") + "-" + toRomawi(DateTime.Now.ToString("MM")) + "-" + DateTime.Now.ToString("yyyy") + "-" + "0001";
            }

            if (iScan != null && iScan.ContentLength > 0)
            {
                // extract only the filename
                var fileName = (regNo != "" ? (regNo.Replace("/", "-")).Replace(".", "-") : currRegNo) + ".pdf";
                // store the file inside ~/App_Data/uploads folder
                var path = Path.Combine(Server.MapPath("~/Files/Kaizen"), fileName);
                iScan.SaveAs(path);
            }

            Kaizen_Data newData = new Kaizen_Data();
            newData.RegNo = (regNo != "" ? (regNo.Replace("/", "-")).Replace(".", "-") : currRegNo);
            newData.improveType = impType;
            newData.NIK = NIK;
            newData.Division = Division;
            newData.Dept = Dept;
            newData.Section = Section;
            newData.SubSection = SubSection;
            newData.lineLeader = LineLeader;
            newData.issuedDate = DateTime.Now;
            newData.Title = title;
            newData.Area = area;
            newData.OCDDate = DateTime.Now;
            newData.OCDBy = currUser.GetUserId();
            newData.KOCScore = 0;
            newData.SCScore = 0;

            if (Request["iCostBenefitTotal"] != null)
            {
                newData.CostMaterial = CostMaterial;
                newData.CostServices = CostServices;
                newData.CostOtherDesc = CostOtherDesc;
                newData.CostOther = CostOther;
                newData.CostTotal = CostTotal;
                newData.BenefitProductType = BenefitProductType;
                newData.BenefitPeriod = BenefitPeriod;
                newData.BenefitQtyPcs = BenefitQtyPcs;
                newData.BenefitQty = BenefitQty;
                newData.BenefitProcessTime = BenefitProcessTime;
                newData.BenefitProcess = BenefitProcess;
                newData.BenefitOtherDesc = BenefitOtherDesc;
                newData.BenefitOther = BenefitOther;
                newData.BenefitTotal = BenefitTotal;
                newData.CostBenefitTotal = CostBenefitTotal;
            }
            newData.Reward = 0;
            newData.hasRewarded = false;
            newData.Has_Feedback = false;
            db.Kaizen_Data.Add(newData);
            db.SaveChanges();

            var currURL = Request.UrlReferrer;
            var currDataID = newData.ID;
            var currScore = (Request["iScore"] == "null" ? 0 : double.Parse(Request["iScore"]));
            var currReward = double.Parse(Request["iReward"]);
            var updateScore = db.Kaizen_Score.Where(z => z.dataID == currDataID);
            var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
            updateData.OCDDate = DateTime.Now;
            updateData.OCDScore = currScore;
            updateData.Reward = (currReward == 75 ? currReward / 100 : (currReward == 5 ? currReward / 10 : currReward));
            var allowNotScoring = string.IsNullOrEmpty(Request["iAllowNotScoring"]) ? false : Boolean.Parse(Request["iAllowNotScoring"]);
            if (!allowNotScoring)
            {
                updateData.OCDBy = currUser.GetUserId();
                if (updateScore.Count() > 0)
                {
                    for (var i = 1; i <= 4; i++)
                    {
                        string[] currOCD = Request["iOCDScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                        var updateDScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i).FirstOrDefault();
                        updateDScore.subCatID = Int32.Parse(currOCD[0]);
                        updateDScore.Score = double.Parse(currOCD[1]);
                        if (i == 4)
                        {
                            var updateKaizenData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
                            if (Int32.Parse(currOCD[0]) == 15 || Int32.Parse(currOCD[0]) == 16)
                            {
                                updateKaizenData.hasImplement = true;
                            }
                            else
                            {
                                updateKaizenData.hasImplement = false;
                            }

                        }
                    }
                }
                else
                {
                    for (var i = 1; i <= 4; i++)
                    {
                        string[] currOCD = Request["iOCDScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                        db.Kaizen_Score.Add(new Kaizen_Score
                        {
                            dataID = currDataID,
                            catID = i,
                            subCatID = Int32.Parse(currOCD[0]),
                            Score = double.Parse(currOCD[1])
                        });

                        if (i == 4)
                        {
                            var updateKaizenData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
                            if (Int32.Parse(currOCD[0]) == 15 || Int32.Parse(currOCD[0]) == 16)
                            {
                                updateKaizenData.hasImplement = true;
                            }
                            else
                            {
                                updateKaizenData.hasImplement = false;
                            }

                        }
                    }
                }

            }
            db.SaveChanges();
            return RedirectToAction("DataList", "OCD", new { area = "Kaizen" });
        }

        [HttpPost]
        [Authorize]
        public ActionResult Edit(HttpPostedFileBase iScan)
        {
            var kaizenID = Int32.Parse(Request["iDataID"]);
            var regNo = Request["iRegNo"];
            var oldRegNo = Request["iOldRegNo"];
            var impType = Request["iImpType"];
            var NIK = Request["iNIK"];
            var Division = Request["iDivision"] != "-" ? Request["iDivision"] : null;
            var Dept = Request["iDept"] != "-" ? Request["iDept"] : null;
            var Section = Request["iSection"] != "-" ? Request["iSection"] : null;
            var SubSection = Request["iSubSection"] != "-" ? Request["iSubSection"] : null;
            var currDate = Request["iDate"];
            var title = Request["iTitle"];
            var area = (Request["iAreaOther"] != "" ? Request["iAreaOther"] : Request["iArea"]);
            var LineLeader = Request["iLineLeader"];

            var currUser = (ClaimsIdentity)User.Identity;
            var curNIK = db.Users.Where(x => x.NIK == NIK).First();
            var abbr = getAbbr(curNIK.DivisionID, curNIK.DeptID, curNIK.SectionID, curNIK.SubSectionID);

            var fileName = Path.Combine(Server.MapPath("~/Files/Kaizen"), (regNo.Replace("/", "-")).Replace(".", "-") + ".pdf");
            var oldFileName = Path.Combine(Server.MapPath("~/Files/Kaizen"), (oldRegNo.Replace("/", "-")).Replace(".", "-") + ".pdf");
            if (oldRegNo != regNo)
            {
                if (System.IO.File.Exists(fileName))
                {
                    System.IO.File.Delete(fileName);
                }

                if (System.IO.File.Exists(oldFileName))
                {
                    System.IO.File.Move(oldFileName, fileName);
                }
            }

            if (iScan != null && iScan.ContentLength > 0)
            {
                // extract only the filename
                // store the file inside ~/App_Data/uploads folder
                if (System.IO.File.Exists(fileName))
                {
                    System.IO.File.Delete(fileName);
                }

                iScan.SaveAs(fileName);
            }

            var currKaizen = db.Kaizen_Data.Where(x => x.ID == kaizenID).FirstOrDefault();
            currKaizen.RegNo = (regNo.Replace("/", "-")).Replace(".", "-");
            currKaizen.improveType = impType;
            currKaizen.NIK = NIK;
            currKaizen.Division = Division;
            currKaizen.Dept = Dept;
            currKaizen.Section = Section;
            currKaizen.SubSection = SubSection;
            currKaizen.lineLeader = LineLeader;
            currKaizen.issuedDate = DateTime.Parse(currDate);
            currKaizen.Title = title;
            currKaizen.Area = area;

            db.SaveChanges();
            //return RedirectToAction("Score", "OCD", new { area = "Kaizen" });
            return RedirectToAction("DataList", "OCD", new { area = "Kaizen" });
        }

        public ActionResult Update()
        {
            var currRegNo = "";
            var currKaizen = db.Kaizen_Data.Where(x => x.RegNo.Contains(DateTime.Now.Year.ToString()) || x.issuedDate > new DateTime(2017, 03, 31)).OrderBy(x => x.ID).ToList();
            var Seq = 1;
            foreach (var kaizen in currKaizen)
            {
                var Sequence = "0000" + Seq;
                var updKaizen = db.Kaizen_Data.Where(x => x.ID == kaizen.ID).FirstOrDefault();
                var abbr = getAbbr(updKaizen.issuedUser.DivisionID, updKaizen.issuedUser.DeptID, updKaizen.issuedUser.SectionID, updKaizen.issuedUser.SubSectionID);

                currRegNo = abbr + "-" + DateTime.Now.ToString("dd") + "-" + toRomawi(DateTime.Now.ToString("MM")) + "-" + DateTime.Now.ToString("yyyy") + "-" + Sequence.Substring(Sequence.Length - 4);

                var fileName = Path.Combine(Server.MapPath("~/Files/Kaizen"), currRegNo + ".pdf");
                var oldFileName = Path.Combine(Server.MapPath("~/Files/Kaizen"), (updKaizen.RegNo.Replace("/", "-")).Replace(".", "-") + ".pdf");
                if (System.IO.File.Exists(fileName))
                {
                    System.IO.File.Delete(fileName);
                }
                if (System.IO.File.Exists(oldFileName))
                {
                    System.IO.File.Move(oldFileName, fileName);
                }

                updKaizen.RegNo = currRegNo;
                Seq++;
            }

            db.SaveChanges();
            return RedirectToAction("DataList", "OCD", new { area = "Kaizen" });
        }


        [HttpPost]
        [Authorize]
        public ActionResult Delete()
        {
            var iID = Int32.Parse(Request["iID"]);
            var regNo = Request["iRegNo"];
            var fileName = (regNo.Replace("/", "-")).Replace(".", "-") + ".pdf";

            string fullPath = Path.Combine(Server.MapPath("~/Files/Kaizen"), fileName); ;
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
            var currKaizen = db.Kaizen_Data.Where(x => x.ID == iID).FirstOrDefault();
            db.Kaizen_Data.Remove(currKaizen);
            var scoreData = db.Kaizen_Score.Where(z => z.dataID == iID);
            db.Kaizen_Score.RemoveRange(scoreData);
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        public ActionResult Check()
        {
            return Content(CultureInfo.CurrentCulture.NumberFormat.ToString());
        }

        [HttpPost]
        [Authorize]
        public ActionResult Score()
        {
            var currURL = Request.UrlReferrer;
            var currUser = (ClaimsIdentity)User.Identity;
            var currDataID = Int32.Parse(Request["iDataID"]);
            var currScore = (Request["iScore"] == "null" ? 0 : double.Parse(Request["iScore"]));
            var currReward = double.Parse(Request["iReward"]);


            double CostMaterial = 0, CostServices = 0, CostOther = 0, CostTotal = 0, BenefitQty = 0, BenefitProcess = 0, BenefitOther = 0, BenefitTotal = 0, CostBenefitTotal = 0;
            string CostOtherDesc = "", BenefitProductType = "", BenefitOtherDesc = "";
            int BenefitPeriod = 0, BenefitQtyPcs = 0, BenefitProcessTime = 0;

            if (Request["iCostBenefitTotal"] != null)
            {
                CostMaterial = double.Parse(Request["iCostMaterial"]);
                CostServices = double.Parse(Request["iCostServices"]);
                CostOtherDesc = Request["iCostOtherDesc"];
                CostOther = double.Parse(Request["iCostOther"]);
                CostTotal = double.Parse(Request["iCostTotal"]);
                BenefitProductType = Request["iBenefitProductType"];
                BenefitPeriod = int.Parse(Request["iBenefitPeriod"]);
                BenefitQtyPcs = int.Parse(Request["iBenefitQtyPcs"]);
                BenefitQty = double.Parse(Request["iBenefitQty"]);
                BenefitProcessTime = int.Parse(Request["iBenefitProcessTime"]);
                BenefitProcess = double.Parse(Request["iBenefitProcess"]);
                BenefitOtherDesc = Request["iBenefitOtherDesc"];
                BenefitOther = double.Parse(Request["iBenefitOther"]);
                BenefitTotal = double.Parse(Request["iBenefitTotal"]);
                CostBenefitTotal = double.Parse(Request["iCostBenefitTotal"]);
            }

            var updateScore = db.Kaizen_Score.Where(z => z.dataID == currDataID);
            var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
            updateData.OCDDate = DateTime.Now;

            if (Request["iCostBenefitTotal"] != null)
            {
                updateData.CostMaterial = CostMaterial;
                updateData.CostServices = CostServices;
                updateData.CostOtherDesc = CostOtherDesc;
                updateData.CostOther = CostOther;
                updateData.CostTotal = CostTotal;
                updateData.BenefitProductType = BenefitProductType;
                updateData.BenefitPeriod = BenefitPeriod;
                updateData.BenefitQtyPcs = BenefitQtyPcs;
                updateData.BenefitQty = BenefitQty;
                updateData.BenefitProcessTime = BenefitProcessTime;
                updateData.BenefitProcess = BenefitProcess;
                updateData.BenefitOtherDesc = BenefitOtherDesc;
                updateData.BenefitOther = BenefitOther;
                updateData.BenefitTotal = BenefitTotal;
                updateData.CostBenefitTotal = CostBenefitTotal;
            }
            updateData.OCDScore = currScore;
            updateData.Reward = (currReward == 75 ? currReward / 100 : (currReward == 5 ? currReward / 10 : currReward));
            updateData.OCDBy = currUser.GetUserId();
            if (updateScore.Count() > 0)
            {
                for (var i = 1; i <= 4; i++)
                {
                    string[] currOCD = Request["iOCDScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                    var updateDScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i).FirstOrDefault();
                    updateDScore.subCatID = Int32.Parse(currOCD[0]);
                    updateDScore.Score = double.Parse(currOCD[1]);
                    if (i == 4)
                    {
                        var updateKaizenData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
                        if (Int32.Parse(currOCD[0]) == 15 || Int32.Parse(currOCD[0]) == 16)
                        {
                            updateKaizenData.hasImplement = true;
                        }
                        else
                        {
                            updateKaizenData.hasImplement = false;
                        }

                    }
                }
            }
            else
            {
                for (var i = 1; i <= 4; i++)
                {
                    string[] currOCD = Request["iOCDScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                    db.Kaizen_Score.Add(new Kaizen_Score
                    {
                        dataID = currDataID,
                        catID = i,
                        subCatID = Int32.Parse(currOCD[0]),
                        Score = double.Parse(currOCD[1])
                    });

                    if (i == 4)
                    {
                        var updateKaizenData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
                        if (Int32.Parse(currOCD[0]) == 15 || Int32.Parse(currOCD[0]) == 16)
                        {
                            updateKaizenData.hasImplement = true;
                        }
                        else
                        {
                            updateKaizenData.hasImplement = false;
                        }

                    }
                }
            }

            db.SaveChanges();
            return Redirect(currURL.AbsoluteUri);
        }

        [HttpPost]
        [Authorize]
        public ActionResult ScoreRevise()
        {
            var currURL = Request.UrlReferrer;
            var currUser = (ClaimsIdentity)User.Identity;
            var currDataID = Int32.Parse(Request["iDataID"]);
            var currScore = double.Parse(Request["iScore"]);
            var currReward = double.Parse(Request["iReward"]);

            double CostMaterial = 0, CostServices = 0, CostOther = 0, CostTotal = 0, BenefitQty = 0, BenefitProcess = 0, BenefitOther = 0, BenefitTotal = 0, CostBenefitTotal = 0;
            string CostOtherDesc = "", BenefitProductType = "", BenefitOtherDesc = "";
            int BenefitPeriod = 0, BenefitQtyPcs = 0, BenefitProcessTime = 0;

            if (Request["iCostBenefitTotal"] != null)
            {
                CostMaterial = double.Parse(Request["iCostMaterial"]);
                CostServices = double.Parse(Request["iCostServices"]);
                CostOtherDesc = Request["iCostOtherDesc"];
                CostOther = double.Parse(Request["iCostOther"]);
                CostTotal = double.Parse(Request["iCostTotal"]);
                BenefitProductType = Request["iBenefitProductType"];
                BenefitPeriod = int.Parse(Request["iBenefitPeriod"]);
                BenefitQtyPcs = int.Parse(Request["iBenefitQtyPcs"]);
                BenefitQty = double.Parse(Request["iBenefitQty"]);
                BenefitProcessTime = int.Parse(Request["iBenefitProcessTime"]);
                BenefitProcess = double.Parse(Request["iBenefitProcess"]);
                BenefitOtherDesc = Request["iBenefitOtherDesc"];
                BenefitOther = double.Parse(Request["iBenefitOther"]);
                BenefitTotal = double.Parse(Request["iBenefitTotal"]);
                CostBenefitTotal = double.Parse(Request["iCostBenefitTotal"]);
            }

            var updateScore = db.Kaizen_Score.Where(z => z.dataID == currDataID);
            var updateData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
            updateData.OCDScore = currScore;
            updateData.Reward = (currReward == 75 ? currReward / 100 : (currReward == 5 ? currReward / 10 : currReward));
            updateData.OCDReviseDate = DateTime.Now;
            updateData.OCDRevise = currUser.GetUserId();

            if (Request["iCostBenefitTotal"] != null)
            {
                updateData.CostMaterial = CostMaterial;
                updateData.CostServices = CostServices;
                updateData.CostOtherDesc = CostOtherDesc;
                updateData.CostOther = CostOther;
                updateData.CostTotal = CostTotal;
                updateData.BenefitProductType = BenefitProductType;
                updateData.BenefitPeriod = BenefitPeriod;
                updateData.BenefitQtyPcs = BenefitQtyPcs;
                updateData.BenefitQty = BenefitQty;
                updateData.BenefitProcessTime = BenefitProcessTime;
                updateData.BenefitProcess = BenefitProcess;
                updateData.BenefitOtherDesc = BenefitOtherDesc;
                updateData.BenefitOther = BenefitOther;
                updateData.BenefitTotal = BenefitTotal;
                updateData.CostBenefitTotal = CostBenefitTotal;
            }

            updateData.KOCScore = 0;
            updateData.KOCDate = null;
            updateData.KOCBy = null;
            updateData.KOCRevise = null;
            updateData.SCScore = 0;
            updateData.SCDate = null;
            updateData.SCBy = null;
            if (updateScore.Count() > 0)
            {
                for (var i = 1; i <= 4; i++)
                {
                    string[] currOCD = Request["iOCDScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                    var updateDScore = db.Kaizen_Score.Where(z => z.dataID == currDataID && z.catID == i).FirstOrDefault();
                    updateDScore.subCatID = Int32.Parse(currOCD[0]);
                    updateDScore.Score = double.Parse(currOCD[1]);
                    if (i == 4)
                    {
                        var updateKaizenData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
                        if (Int32.Parse(currOCD[0]) == 15 || Int32.Parse(currOCD[0]) == 16)
                        {
                            updateKaizenData.hasImplement = true;
                        }
                        else
                        {
                            updateKaizenData.hasImplement = false;
                        }

                    }
                }
            }
            else
            {
                for (var i = 1; i <= 4; i++)
                {
                    string[] currOCD = Request["iOCDScore" + i].Split(new[] { "||" }, StringSplitOptions.None);
                    db.Kaizen_Score.Add(new Kaizen_Score
                    {
                        dataID = currDataID,
                        catID = i,
                        subCatID = Int32.Parse(currOCD[0]),
                        Score = double.Parse(currOCD[1])
                    });
                    if (i == 4)
                    {
                        var updateKaizenData = db.Kaizen_Data.Where(z => z.ID == currDataID).FirstOrDefault();
                        if (Int32.Parse(currOCD[0]) == 15 || Int32.Parse(currOCD[0]) == 16)
                        {
                            updateKaizenData.hasImplement = true;
                        }
                        else
                        {
                            updateKaizenData.hasImplement = false;
                        }

                    }
                }
            }

            db.SaveChanges();
            return Redirect(currURL.AbsoluteUri);
        }

        [HttpPost]
        public JsonResult getCurrScore(Int32 iID)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var userID = currUser.GetUserId();
            var catIDList = new List<int> { 1, 2, 3, 4 };
            var getScore = db.Kaizen_Score.Where(z => z.dataID == iID && catIDList.Contains(z.catID)).Select(x => new
            {
                ID = x.ID,
                dataID = x.dataID,
                catID = x.catID,
                subCatID = x.subCatID,
                Score = x.Score,
                ScoredDate = x.Kaizen_Data.OCDDate,
                ScoredBy = x.Kaizen_Data.OCDBy,
                ScoredByName = x.Kaizen_Data.OCDUser.Name,
                ScoredReviserName = x.Kaizen_Data.OCDReviser.Name,
                currUser = userID,
                KOCScore = x.Kaizen_Data.KOCScore,
                KOCScoreBy = x.Kaizen_Data.KOCBy,
                SCScoreBy = x.Kaizen_Data.SCBy,
                CostMaterial = x.Kaizen_Data.CostMaterial,
                CostServices = x.Kaizen_Data.CostServices,
                CostOtherDesc = x.Kaizen_Data.CostOtherDesc,
                CostOther = x.Kaizen_Data.CostOther,
                CostTotal = x.Kaizen_Data.CostTotal,
                BenefitProductType = x.Kaizen_Data.BenefitProductType,
                BenefitPeriod = x.Kaizen_Data.BenefitPeriod,
                BenefitQtyPcs = x.Kaizen_Data.BenefitQtyPcs,
                BenefitQty = x.Kaizen_Data.BenefitQty,
                BenefitProcessTime = x.Kaizen_Data.BenefitProcessTime,
                BenefitProcess = x.Kaizen_Data.BenefitProcess,
                BenefitOtherDesc = x.Kaizen_Data.BenefitOtherDesc,
                BenefitOther = x.Kaizen_Data.BenefitOther,
                BenefitTotal = x.Kaizen_Data.BenefitTotal,
                CostBenefitTotal = x.Kaizen_Data.CostBenefitTotal
            }).ToList();
            return Json(getScore);
        }


        [HttpPost]
        public JsonResult getTitle()
        {
            var currTitle = Request["currTitle"].ToLower().Split(null).ToList();
            var currUser = (ClaimsIdentity)User.Identity;
            var userID = currUser.GetUserId();
            var getTitle = db.Kaizen_Data.Select(z => new
            {
                Title = z.Title,
                RegNo = z.RegNo
            }).ToList();

            foreach (var t in currTitle)
            {
                getTitle = getTitle.Where(x => x.Title.ToLower().Contains(t)).Select(z => new
                {
                    Title = z.Title,
                    RegNo = z.RegNo
                }).ToList();
            }
            return Json(getTitle);
        }

        public JsonResult hasFeedback(Int32 iID)
        {
            var updateData = db.Kaizen_Data.Where(w => w.ID == iID).FirstOrDefault();
            updateData.Has_Feedback = !updateData.Has_Feedback;
            db.SaveChanges();

            return Json(updateData.Has_Feedback, JsonRequestBehavior.AllowGet);
        }
    }
}