using ClosedXML.Excel;
using MathNet.Numerics;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using NGKBusi.Areas.FA.Controllers;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Areas.WebService.Models;
using NGKBusi.Models;
using SixLabors.ImageSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Windows.Media.Animation;

namespace NGKBusi.Areas.HC.Controllers
{
    public class BusinessTripController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        BusinessTripConnection dbBusinessTrip = new BusinessTripConnection();
        String currPeriodFY = "FY1" + (DateTime.Now.Month >= 4 ? (DateTime.Now.Year + 1) % 100 : DateTime.Now.Year % 100).ToString("D2");

        // GET: HC/BusinessTrip
        // Test Rubah
        [Authorize]
        public ActionResult formRequest(String ReqNumber, String NIK)
        {
            ViewBag.NavHide = true;

            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserDiv = currUser.FindFirstValue("divName");
            var currUserDept = currUser.FindFirstValue("DeptName");
            var currYear = Request["iBTFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iBTFilterStatus"] ?? "Open";
            var businessTrip = dbBusinessTrip.HC_Business_Trip_Request.Include("HC_Business_Trip_CA").Where(w => w.Req_Number == ReqNumber).FirstOrDefault();
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Menu_Id == 75 && w.Document_Id == 1).OrderBy(o => o.Levels).ThenBy(o => o.Levels_Sub).ToList();
            var currFilterLevel = Request["iBTFilterLevel"] != null ? int.Parse(Request["iBTFilterLevel"].Split('|')[0]) : currPayLevelCheck?.OrderByDescending(o => o.Levels).ThenByDescending(o => o.Levels_Sub).First().Levels;
            var currFilterLevelSub = Request["iBTFilterLevel"] != null ? int.Parse(Request["iBTFilterLevel"].Split('|')[1]) : currPayLevelCheck?.OrderByDescending(o => o.Levels).ThenByDescending(o => o.Levels_Sub).First().Levels_Sub;

            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 75
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }
            var currApprove = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == 1 && w.User_NIK == currUserID);
            if (businessTrip != null)
            {
                currApprove = currApprove.Where(w => w.Dept_Code == businessTrip.Cost_ID);
                var caList = dbBusinessTrip.HC_Business_Trip_CA_Detail.Where(w => w.HC_Business_Trip_CA.HC_Business_Trip_Request.ID == businessTrip.ID).Select(s => s.NIK).Distinct().ToList();
                var caCheck = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.Request_ID == businessTrip.ID && !caList.Contains(w.NIK)).Select(s => s.NIK + "|" + s.Name).Distinct().ToArray();
                ViewBag.CACheck = caCheck;
            }
            var currApproval = currApprove.Where(w => w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub).FirstOrDefault();
            var currApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == 1 && w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub).Select(s => s.Dept_Code).Distinct().ToList();

            if (currUserID == "629.01.13" || currUserID == "831.03.19")
            {
                ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent" && !w.NIK.Contains("NGK")).ToList();
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX").ToList();
                ViewBag.BusinessTripList = dbBusinessTrip.HC_Business_Trip_Request.OrderByDescending(o => o.ID).ToList();
            }
            else
            {
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX").ToList();


                if (currUserID == "835.05.20")
                {
                    ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent" && (w.DeptName == currUserDept || w.DeptName == "MAINTENANCE") && !w.NIK.Contains("NGK")).ToList();
                } else if (currUserDept == "SALES - AFTERMARKET" || currUserDept == "MARKETING")
                {
                    ViewBag.UserList = db.V_Users_Active.Where(w => w.Status != "Not Active" && (w.DeptName == currUserDept || w.NIK == "851.02.21" || w.NIK == "atsushi.aoki" || w.NIK == "tomoyuki.kurita" || w.NIK == "EXP.015") && !w.NIK.Contains("NGK")).ToList();
                }
                else if (currUserDept == "QUALITY & MR")
                {
                    ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent" && (w.DeptName == currUserDept || w.NIK == "785.10.17") && !w.NIK.Contains("NGK")).ToList();
                }
                else if (currUserDiv == "PRODUCTION")
                {
                    ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent" && (w.DivisionName == currUserDiv) && !w.NIK.Contains("NGK")).ToList();
                }
                else
                {
                    ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent" && w.DeptName == currUserDept && !w.NIK.Contains("NGK")).ToList();
                }

                if (currStatus == "Open")
                {
                    ViewBag.BusinessTripList = dbBusinessTrip.HC_Business_Trip_Request.Where(w => (currApprovalList.Contains(w.Cost_ID) || w.NIK == currUserID) && (w.Approval == currFilterLevel && w.Approval_Sub == currFilterLevelSub)).OrderByDescending(o => o.ID).ToList();
                }
                else if (currStatus == "Signed")
                {
                    ViewBag.BusinessTripList = dbBusinessTrip.HC_Business_Trip_Request.Where(w => (currApprovalList.Contains(w.Cost_ID) || w.NIK == currUserID) && ((w.Approval == currFilterLevel && w.Approval_Sub > currFilterLevelSub) || w.Approval > currFilterLevel) && w.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.ID).ToList();
                }
                else
                {
                    ViewBag.BusinessTripList = dbBusinessTrip.HC_Business_Trip_Request.Where(w => (currApprovalList.Contains(w.Cost_ID) || w.NIK == currUserID) && ((w.Approval >= currFilterLevel && w.Approval_Sub >= currFilterLevelSub) || w.Approval > currFilterLevel) && w.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.ID).ToList();
                }
            }

            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currApproval?.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currApproval?.Levels_Sub;

            ViewBag.CurrApproval = currApproval;
            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUserID).First();
            ViewBag.BusinessTrip = businessTrip;

            return View();
        }

        [Authorize]
        public ActionResult formCA(String ReqNumber)
        {
            ViewBag.NavHide = true;

            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserDept = currUser.FindFirstValue("DeptName");
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 75
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            var currYear = Request["iBTFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iBTFilterStatus"] ?? "Open";
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Menu_Id == 75 && w.Document_Id == 1).OrderBy(o => o.Levels).ThenBy(o => o.Levels_Sub).ToList();
            var currFilterLevel = Request["iBTFilterLevel"] != null ? int.Parse(Request["iBTFilterLevel"].Split('|')[0]) : currPayLevelCheck?.First().Levels;
            var currFilterLevelSub = Request["iBTFilterLevel"] != null ? int.Parse(Request["iBTFilterLevel"].Split('|')[1]) : currPayLevelCheck?.First().Levels_Sub;
            var BusinessTrip = dbBusinessTrip.HC_Business_Trip_Request.Include("HC_Business_Trip_CA").Where(w => w.Req_Number == ReqNumber).FirstOrDefault();
            var currApproval = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == 1 && w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub).FirstOrDefault();
            var currApprovalDeptCode = currApproval?.Dept_Code;
            var currApprovalList = currPayLevelCheck.Select(s => s.Dept_Code).ToList();
            if (currUserID == "629.01.13")
            {
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX").ToList();
                ViewBag.BusinessTripList = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Approval >= 4).OrderByDescending(o => o.ID).ToList();
                if (currStatus == "Open")
                {
                    ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_CA.Where(w => (w.HC_Business_Trip_Request.Approval == 4 || (w.HC_Business_Trip_Request.Approval == 1 && w.HC_Business_Trip_Request.Cost_ID == "B2210")) && w.HC_Business_Trip_Request.isReject == false).OrderByDescending(o => o.ID).ToList();
                }
                else if (currStatus == "Signed")
                {
                    ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_CA.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID) && ((w.HC_Business_Trip_Request.Approval == currFilterLevel && w.HC_Business_Trip_Request.Approval_Sub > currFilterLevelSub) || w.HC_Business_Trip_Request.Approval > currFilterLevel) && w.HC_Business_Trip_Request.Created_At.Year.ToString() == currYear && w.HC_Business_Trip_Request.Approval == 4 && w.HC_Business_Trip_Request.isReject == false).OrderByDescending(o => o.ID).ToList();
                }
                else
                {
                    ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_CA.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID) && ((w.HC_Business_Trip_Request.Approval >= currFilterLevel && w.HC_Business_Trip_Request.Approval_Sub >= currFilterLevelSub) || w.HC_Business_Trip_Request.Approval > currFilterLevel) && w.HC_Business_Trip_Request.Created_At.Year.ToString() == currYear && w.HC_Business_Trip_Request.Approval == 4 && w.HC_Business_Trip_Request.isReject == false).OrderByDescending(o => o.ID).ToList();
                }
            }
            else
            {
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX" && w.Section_From_Code == currApprovalDeptCode).ToList();
                ViewBag.BusinessTripList = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Cost_ID == currApprovalDeptCode && ((w.Approval >= currFilterLevel && w.Approval_Sub >= currFilterLevelSub) || w.Approval > currFilterLevel)).OrderByDescending(o => o.ID).ToList();
                //ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.HC_Business_Trip_Request.Cost_ID == currApprovalDeptCode && ((w.HC_Business_Trip_Request.Approval >= currFilterLevel && w.HC_Business_Trip_Request.Approval_Sub >= currFilterLevelSub) || w.HC_Business_Trip_Request.Approval > currFilterLevel) && w.HC_Business_Trip_Request.Created_At.ToString() == currYear).OrderByDescending(o => o.HC_Business_Trip_Request.ID).ToList();

                if (currStatus == "Open")
                {
                    ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_CA.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID) && (w.HC_Business_Trip_Request.Approval == currFilterLevel && w.HC_Business_Trip_Request.Approval_Sub == currFilterLevelSub)).OrderByDescending(o => o.ID).ToList();
                }
                else if (currStatus == "Signed")
                {
                    ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_CA.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID) && ((w.HC_Business_Trip_Request.Approval == currFilterLevel && w.HC_Business_Trip_Request.Approval_Sub > currFilterLevelSub) || w.HC_Business_Trip_Request.Approval > currFilterLevel) && w.HC_Business_Trip_Request.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.ID).ToList();
                }
                else
                {
                    ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_CA.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID) && ((w.HC_Business_Trip_Request.Approval >= currFilterLevel && w.HC_Business_Trip_Request.Approval_Sub >= currFilterLevelSub) || w.HC_Business_Trip_Request.Approval > currFilterLevel) && w.HC_Business_Trip_Request.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.ID).ToList();
                }
            }
            if (currUserDept == "SALES - OEM & OES")
            {
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX" && (w.Section_From_Code == "B3120" || w.Section_From_Code == "B3110")).ToList();
            }

            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currApproval?.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currApproval?.Levels_Sub;
            ViewBag.BusinessTrip = BusinessTrip;
            ViewBag.CurrApproval = currApproval;
            int currReqID = ViewBag.BusinessTrip != null ? BusinessTrip.ID : 0;

            return View();
        }
        [Authorize]
        public ActionResult settlementApprovalReport()
        {
            var dateFrom = DateTime.ParseExact(Request["iReportDateFrom"], "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var dateTo = DateTime.ParseExact(Request["iReportDateTo"], "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var path = Server.MapPath("~/Files/HC/BusinessTrip/Report/Download/Settlement_Approval_Report.xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/HC/BusinessTrip/Report/Master/Settlement_Approval_Report.xlsx"), path, true);

            var getReportData = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.HC_Business_Trip_Request.Date_To >= dateFrom && w.HC_Business_Trip_Request.Date_To <= dateTo && w.HC_Business_Trip_Request.Approval == 4).AsEnumerable().Select(s => new
            {
                s.HC_Business_Trip_Request.Req_Number,
                Date_To = s.HC_Business_Trip_Request.Date_To.Value.ToString("dd/MM/yyyy"),
                s.NIK,
                s.Name,
                s.HC_Business_Trip_Request.Destination,
                s.Department,
                Status = ApprovalStatus(s.Approval, s.Approval_Sub, 2, 2, false),
                EVoucher = getEVoucherStatus(s),
                TotalCA = getSettlementTotal(s, 1),
                TotalSettlement = getSettlementTotal(s, 2),
                TotalVariance = getSettlementTotal(s, 3),
                CurrentApproval = getSettlementApproval(s, 1),
                NextApproval = getSettlementApproval(s, 2)
            }).ToList();

            var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(1);
            var ts2 = ws.Cell(2, 1).InsertData(getReportData);
            workbook.SaveAs(path);

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=Settlement_Approval_Report.xlsx");
            Response.TransmitFile(path);
            Response.End();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public String getSettlementApproval(HC_Business_Trip_Settlement data, int type)
        {
            var currDataID = data.ID.ToString();
            var getCurrApproval = db.Approval_History.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && w.Reveral_ID == currDataID && w.IsReject == false && w.IsRevise == false && w.Status != "Return").OrderByDescending(o => o.id).FirstOrDefault();
            var getNextApproval = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && w.Dept_Code == data.HC_Business_Trip_Request.Cost_ID && (w.Levels == data.Approval && w.Levels_Sub == data.Approval_Sub)).OrderBy(o => o.Levels).ThenBy(o => o.Levels_Sub).FirstOrDefault();
            var currApprovalName = "";
            switch (type)
            {
                case 1:
                    currApprovalName = getCurrApproval?.Created_By_Name;
                    break;
                case 2:
                    currApprovalName = getNextApproval?.Users.Name;
                    break;
                default:
                    currApprovalName = getCurrApproval?.Created_By_Name;
                    break;
            }
            return currApprovalName;
        }
        public double? getSettlementTotal(HC_Business_Trip_Settlement data, int type)
        {
            var totalSettlement = (data.Hotel_Total * data.Exchange_Rate) + (data.Meals_Total * data.Exchange_Rate) + (data.Daily_Total * data.Exchange_Rate) + (data.Transport_Total * data.Exchange_Rate) + (data.Misc_Total * data.Exchange_Rate);
            //var totalSettlement = 0;
            var currCA = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.Request_ID == data.HC_Business_Trip_Request.ID && w.NIK == data.NIK).FirstOrDefault();
            var totalCA = (currCA?.Hotel_Total * data.Exchange_Rate) + (currCA?.Meals_Total * data.Exchange_Rate) + (currCA?.Daily_Total * currCA?.Exchange_Rate) + (currCA?.Transport_Total * data.Exchange_Rate) + (currCA?.Misc_Total * data.Exchange_Rate);
            //var totalCA = 0;
            var totalVariance = totalSettlement - totalCA;

            double? getTotal = 0.0;
            switch (type)
            {
                case 1:
                    getTotal = totalCA;
                    break;
                case 2:
                    getTotal = totalSettlement;
                    break;
                case 3:
                    getTotal = totalVariance;
                    break;
                default:
                    getTotal = totalCA;
                    break;

            }
            return getTotal;
        }

        public String getEVoucherStatus(HC_Business_Trip_Settlement data)
        {
            string invoiceNumber = data.HC_Business_Trip_Request.Req_Number + "-" + data.NIK;
            var Status = db.FA_Payment_Request_Non_PO.Where(w => w.Invoice_Number == invoiceNumber && (w.Payment_Type == "Settlement" || w.Payment_Type == "Direct-Payment")).FirstOrDefault();

            return Status != null ? PaymentRequestController.ApprovalStatus(Status.Approval, Status.Approval_Sub, Status.Is_Reject, Status.Amount_of_Invoice) : data.Approval >= 4 ? "Not Created" : "Waiting for Settlement Approval";
        }

        [Authorize]
        public ActionResult formSettlement(String ReqNumber, String NIK)
        {
            var currYear = Request["iBTFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iBTFilterStatus"] ?? "Open";

            ViewBag.NavHide = true;

            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserDept = currUser.FindFirstValue("DeptName");
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Menu_Id == 75 && w.Document_Id == 2).ToList();
            var currFilterLevel = Request["iBTFilterLevel"] != null ? int.Parse(Request["iBTFilterLevel"].Split('|')[0]) : currPayLevelCheck?.OrderByDescending(o => o.Levels).ThenByDescending(o => o.Levels_Sub).First().Levels;
            var currFilterLevelSub = Request["iBTFilterLevel"] != null ? int.Parse(Request["iBTFilterLevel"].Split('|')[1]) : currPayLevelCheck?.OrderByDescending(o => o.Levels).ThenByDescending(o => o.Levels_Sub).First().Levels_Sub;

            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 75
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }
            var BusinessTrip = dbBusinessTrip.HC_Business_Trip_Request.Include("HC_Business_Trip_CA").Where(w => w.Req_Number == ReqNumber).FirstOrDefault();
            var currApprove = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub);
            if (BusinessTrip != null)
            {
                currApprove = currApprove.Where(w => w.Dept_Code == BusinessTrip.Cost_ID);
            }
            var currApproval = currApprove.FirstOrDefault();
            var currApprovalDeptCode = currApproval?.Dept_Code;
            var currApprovalList = currApprove.Select(s => s.Dept_Code).ToList();
            var currApprovalDeptList = currPayLevelCheck.Select(s => s.Dept_Code).ToList();
            var getRelatedUserList = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && currApprovalDeptList.Contains(w.Dept_Code)).Select(s => s.User_NIK).ToList();

            if (currUserID == "629.01.13" || currUserID == "592.02.10")
            {
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX").ToList();
            }
            else
            {
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX" && w.Section_From_Code == currApprovalDeptCode).ToList();
            }
            ViewBag.BusinessTripList = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Cost_ID == currApprovalDeptCode && w.Approval >= 4 && ((w.Approval >= currFilterLevel && w.Approval_Sub >= currFilterLevelSub) || w.Approval > currFilterLevel)).OrderByDescending(o => o.ID).ToList();
            if (currStatus == "Open")
            {
                ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID || getRelatedUserList.Contains(w.NIK)) && w.HC_Business_Trip_Request.Approval >= 4 && w.HC_Business_Trip_Request.isReject == false && (w.Approval == currFilterLevel && w.Approval_Sub == currFilterLevelSub) || (w.NIK == currUserID && w.isRevise == true)).OrderByDescending(o => o.HC_Business_Trip_Request.ID).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID || getRelatedUserList.Contains(w.NIK)) && w.HC_Business_Trip_Request.Approval >= 4 && w.HC_Business_Trip_Request.isReject == false && ((w.Approval == currFilterLevel && w.Approval_Sub > currFilterLevelSub) || w.Approval > currFilterLevel) && w.HC_Business_Trip_Request.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.HC_Business_Trip_Request.ID).ToList();
            }
            else
            {
                ViewBag.BusinessTripDetailList = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => (currApprovalList.Contains(w.HC_Business_Trip_Request.Cost_ID) || w.NIK == currUserID || getRelatedUserList.Contains(w.NIK)) && w.HC_Business_Trip_Request.Approval >= 4 && w.HC_Business_Trip_Request.isReject == false && ((w.Approval >= currFilterLevel && w.Approval_Sub >= currFilterLevelSub) || w.Approval > currFilterLevel) && w.HC_Business_Trip_Request.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.HC_Business_Trip_Request.ID).ToList();
            }

            if (currUserDept == "SALES - OEM & OES")
            {
                ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY && w.Budget_Type == "BEX" && (w.Section_From_Code == "B3120" || w.Section_From_Code == "B3110")).ToList();
            }

            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.RelatedUserList = getRelatedUserList;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currApproval?.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currApproval?.Levels_Sub;

            ViewBag.CurrApproval = currApproval;
            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUserID).First();
            ViewBag.BusinessTrip = BusinessTrip;
            int currReqID = ViewBag.BusinessTrip != null ? BusinessTrip.ID : 0;

            return View();
        }

        public String getSequence(string type)
        {
            var lastSeq = "";
            if (type == "BT")
            {
                var seqHeader = type + (DateTime.Now.Month >= 4 ? DateTime.Now.AddYears(1).ToString("yy") : DateTime.Now.ToString("yy"));

                var latestSequence = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Req_Number.Substring(0, 4) == seqHeader).OrderByDescending(o => o.ID).Select(s => s.Req_Number.Substring(s.Req_Number.Length - 4, 4)).FirstOrDefault();
                lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
                lastSeq = seqHeader + "-" + lastSeq.Substring(lastSeq.Length - 4, 4);
            }

            return lastSeq;
        }
        public ActionResult settlementReminderByButton(int iID = 0)
        {
            settlementReminder(iID);

            return Redirect(Request.UrlReferrer.ToString());
        }

        public void settlementReminder(int iID = 0)
        {
            var startDate = new DateTime(2025, 04, 01);
            var dayBefore = DateTime.Now.AddDays(-1);
            var dayAfter = DateTime.Now.AddDays(1);
            //var getSettlementID = dbBusinessTrip.HC_Business_Trip_Request.Where(w =>  w.Date_To > dayBefore && w.Date_To < dayAfter).Select(s => s.ID).ToList();
            //var checkSettlement = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => (w.Approval + w.Approval_Sub) == 1 && w.HC_Business_Trip_Request.Approval == 4 && (w.HC_Business_Trip_Request.Date_To >= startDate && DateTime.Now >= w.HC_Business_Trip_Request.Date_To)).ToList();
            var checkSettlement = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => (w.Approval + w.Approval_Sub) == 1 && w.HC_Business_Trip_Request.Approval == 4 && (DbFunctions.TruncateTime(w.HC_Business_Trip_Request.Date_To) >= startDate && DbFunctions.TruncateTime(w.HC_Business_Trip_Request.Date_To) <= DateTime.Now)).ToList();

            if (iID > 0)
            {
                checkSettlement = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.ID == iID).ToList();
            }

            foreach (var data in checkSettlement)
            {
                var isMadeByGA = data.HC_Business_Trip_Request.NIK == "629.01.13" ? true : false;
                var isSendReminder = false;
                DateTime arrivalDate = Convert.ToDateTime(data.HC_Business_Trip_Request.Date_To).Date;
                var defaultEmail = data.User.Email;
                if (isMadeByGA)
                {
                    defaultEmail = "silvya@ngkbusi.com";
                }

                if (arrivalDate == DateTime.Now.Date)
                {
                    //send email on arrivalDate
                    isSendReminder = true;
                }
                else if (DateTime.Now.Date == arrivalDate.AddDays(3))
                {
                    //send email threedays of arrivalDate
                    isSendReminder = true;
                }
                else if (DateTime.Now.Date >= arrivalDate.AddDays(7))
                {
                    //send email everyday after seven day of arrivalDate
                    isSendReminder = true;
                }

                if (iID > 0)
                {
                    isSendReminder = true;
                }

                if (isSendReminder == true)
                {
                    string FilePath = System.IO.Path.Combine(Server.MapPath("~/Emails/HC/BusinessTrip/"), "BusinessTripSettlementReminder.html");
                    StreamReader str = new StreamReader(FilePath);
                    string MailText = str.ReadToEnd();
                    str.Close();
                    var currURL = Url.Action("formSettlement", "BusinessTrip", new { area = "HC", ReqNumber = data.HC_Business_Trip_Request.Req_Number, NIK = data.NIK }, this.Request.Url.Scheme);
                    var currURLOpen = Url.Action("formSettlement", "BusinessTrip", new { area = "HC", iBTFilterStatus = "Open" }, this.Request.Url.Scheme);

                    MailText = MailText.Replace("##document##", "Settlement");
                    MailText = MailText.Replace("##needs##", "Input");
                    MailText = MailText.Replace("##link##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
                    MailText = MailText.Replace("##url##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
                    MailText = MailText.Replace("##note##", "-");
                    MailText = MailText.Replace("##noteby##", data.User.Name);
                    MailText = MailText.Replace("##linkOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
                    MailText = MailText.Replace("##urlOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
                    MailText = MailText.Replace("##guid##", Guid.NewGuid().ToString());

                    var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
                    //var receiverEmail = new MailAddress("azis.abdillah@ngkbusi.com");
                    var password = "100%NGKbusi!";
                    var sub = "[Niterra-Portal-Notification] Reminder!! - Business Trip - Settlement - " + data.HC_Business_Trip_Request.Req_Number + "-" + data.NIK;
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
                        mess.To.Add(new MailAddress(defaultEmail));
                        mess.CC.Add(new MailAddress("silvya@ngkbusi.com"));
                        mess.Bcc.Add(new MailAddress("azis.abdillah@ngkbusi.com"));
                        smtp.Send(mess);
                    }
                }
            }
        }

        public void sendNotification(string currReqNumber, string currMenu, string currStatus, string currNIK = "", string deptCode = "", int approval = 0, int approval_sub = 0, string note = "-")
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            string FilePath = System.IO.Path.Combine(Server.MapPath("~/Emails/HC/BusinessTrip/"), "BusinessTripApproval.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();
            var stat = "";
            var needs = "Approval";
            if (currStatus == "Reject")
            {
                stat = "Rejected!";
                needs = "Attention";
            }
            else if (currStatus == "Return")
            {
                stat = "Returned!";
                needs = "Attention";
            }
            else if (currStatus == "Revise")
            {
                stat = "Need Revise!";
                needs = "To be Revise";
            }
            else if (currStatus == "Revise Done")
            {
                stat = "Has been Revised!";
                needs = "To be Review";
            }
            else if (currStatus == "Comment")
            {
                stat = "Commented";
                needs = "Attention";
            }

            var settlementSubject = " - Cash Advance";
            var doc = "Business Trip - Cash Advance";
            if (currMenu == "formSettlement")
            {
                doc = "Business Trip - Settlement";
                settlementSubject = " - Settlement-" + currNIK;
            }

            var currURL = Url.Action(currMenu, "BusinessTrip", new { area = "HC", ReqNumber = currReqNumber }, this.Request.Url.Scheme);
            var currURLOpen = Url.Action(currMenu, "BusinessTrip", new { area = "HC", iBTFilterStatus = "Open" }, this.Request.Url.Scheme);
            var documentID = 1;
            if (currMenu == "formSettlement")
            {
                documentID = 2;
                currURL = Url.Action(currMenu, "BusinessTrip", new { area = "HC", ReqNumber = currReqNumber, NIK = currNIK }, this.Request.Url.Scheme);
                currURLOpen = Url.Action(currMenu, "BusinessTrip", new { area = "HC", iBTFilterStatus = "Open" }, this.Request.Url.Scheme);
            }
            var emailList = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == documentID && w.Dept_Code == deptCode && w.Levels == approval && w.Levels_Sub == approval_sub && w.IsSkip == false).Select(s => s.Users.Email).Distinct().ToList();
            if (currStatus != "Sign")
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

            if (currStatus == "Comment")
            {
                var reveralID = currReqNumber;
                var latestRejectID = db.Approval_History.Where(w => w.Menu_Id == 75 && w.Document_Id == documentID && w.Reveral_ID == reveralID && (w.Status == "Return" || w.Status == "Reject")).OrderByDescending(o => o.id).FirstOrDefault()?.id ?? 0;

                if (documentID == 2)
                {
                    if (latestRejectID != 0)
                    {
                        var emailListQRY = db.Approval_History.Where(w => w.Menu_Id == 75 && w.Document_Id == documentID && w.Reveral_ID == reveralID);

                        emailListQRY = emailListQRY.Where(w => w.id > latestRejectID && w.Status != "Return" && w.Status != "Reject");
                        var commentList = dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Where(w => w.ReqNumber == currReqNumber && w.ReqNIK == currNIK).Select(s => s.Users.Email).Distinct().ToList();

                        emailList = emailListQRY.Select(s => s.Users.Email).Distinct().ToList();
                        foreach (var comment in commentList)
                        {
                            emailList.Add(comment);
                        }
                    }
                    else
                    {
                        var getTripID = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Req_Number == currReqNumber).FirstOrDefault().ID;
                        var getSettlement = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.Request_ID == getTripID && w.NIK == currNIK);
                        reveralID = getSettlement.FirstOrDefault().ID.ToString();
                        var currApproval = getSettlement.FirstOrDefault().Approval;
                        var currApprovalSub = getSettlement.FirstOrDefault().Approval_Sub;
                        var emailListQRY = db.Approval_List.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && w.Reveral_ID == reveralID && w.Is_Skip == false && (w.Levels <= currApproval && ((w.Levels == currApproval && w.Levels_Sub < currApprovalSub) || (w.Levels < currApproval))));

                        var commentList = dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Where(w => w.ReqNumber == currReqNumber && w.ReqNIK == currNIK).Select(s => s.Users.Email).Distinct().ToList();

                        emailList = emailListQRY.Select(s => s.Users.Email).Distinct().ToList();
                        foreach (var comment in commentList)
                        {
                            emailList.Add(comment);
                        }
                    }
                }
                else
                {
                    var emailListQRY = db.Approval_History.Where(w => w.Menu_Id == 75 && w.Document_Id == documentID && w.Reveral_ID == reveralID);
                    var commentList = dbBusinessTrip.HC_Business_Trip_Request_Comments.Where(w => w.ReqNumber == currReqNumber).Select(s => s.Users.Email).Distinct().ToList();
                    if (latestRejectID != 0)
                    {
                        emailListQRY = emailListQRY.Where(w => w.id > latestRejectID && w.Status != "Return" && w.Status != "Reject");
                    }
                    emailList = emailListQRY.Select(s => s.Users.Email).Distinct().ToList();
                    foreach (var comment in commentList)
                    {
                        emailList.Add(comment);
                    }
                }
            }

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
            //var receiverEmail = new MailAddress("azis.abdillah@ngkbusi.com");
            var password = "100%NGKbusi!";
            var sub = "[Niterra-Portal-Notification]" + stat + " - Business Trip-" + currReqNumber + settlementSubject;
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

                if (approval == 4 && approval_sub == 0)
                {
                    mess.To.Add(new MailAddress("silvya@ngkbusi.com"));
                }
                mess.Bcc.Add(new MailAddress("azis.abdillah@ngkbusi.com"));
                smtp.Send(mess);
            }
        }

        public class StandardAmount
        {
            public double Daily { get; set; }
            public double Hotel { get; set; }
            public double Meals { get; set; }
        }

        public String ApprovalStatus(int Approval, int Approval_Sub, int Type = 1, int subType = 1, bool isReject = false)
        {
            var stat = "Created";
            if (Type == 1)
            {
                if (Approval == 2)
                {
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Checked By Manager";
                            break;
                        case 2:
                            stat = "Checked By SM Manager";
                            break;
                        default:
                            stat = "Submitted";
                            break;
                    }
                }
                else if (Approval == 3)
                {
                    stat = "Checked By Director";
                }
                else if (Approval > 3)
                {
                    stat = "Approved";
                }


                if (isReject)
                {
                    stat = "Rejected";
                }
            }
            else if (Type == 2)
            {
                if (Approval == 1)
                {
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Submitted";
                            break;
                        default:
                            stat = "Created";
                            break;
                    }
                }
                else if (Approval == 2)
                {
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Checked By Dept. Manager";
                            break;
                        case 2:
                            stat = "Checked By Dept. Sr. Manager";
                            break;
                        case 3:
                            stat = "Checked By Dept. Director/GM";
                            break;
                        case 4:
                            stat = "Checked By Human Capital";
                            break;
                        default:
                            stat = "Prepared By GA";
                            break;
                    }
                }
                else if (Approval == 3)
                {
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Approved By GM Finance";
                            break;
                        case 2:
                            stat = "Approved By President Director";
                            break;
                        default:
                            stat = "Checked By Finance";
                            break;
                    }
                }
                else if (Approval == 4)
                {
                    stat = "Approved By President Director";
                }

                if (isReject)
                {
                    stat = "Rejected";
                }
            }
            else
            {
                stat = "info";
                if (isReject)
                {
                    stat = "danger";
                }
                else
                {
                    if (subType == 1 && Approval > 3)
                    {
                        stat = "success";
                    }
                    else if (subType == 2 && Approval > 3 && Approval_Sub == 2)
                    {
                        stat = "success";
                    }
                    else if (subType == 2 && Approval == 4)
                    {
                        stat = "success";
                    }
                }
            }

            return stat;
        }
        public String ApprovalHistory(string Reveral_ID, int Approval, int Approval_Sub, int getType, int documentID = 1)
        {
            var str = "";
            int ApprovalHistoryTry = 0;
            int ApprovalHistoryTryMax = 4;
            int ApprovalHistoryReTry = 0;
            int ApprovalHistoryReTryMax = 1;
            var _Approval = Approval;
            var _Approval_Sub = Approval_Sub;
            if (documentID == 2)
            {
                if ((Approval == 1 && Approval_Sub <= 1) || (Approval == 2 && Approval_Sub < 4) || (Approval == 3 && Approval_Sub < 1))
                {
                    _Approval_Sub += 1;
                }
                else
                {
                    _Approval += 1;
                    _Approval_Sub = 0;
                }
            }

        historyWhile:
            var getApprovalHistory = db.Approval_History.Where(w => w.Menu_Id == 75 && w.Document_Id == documentID && w.Reveral_ID == Reveral_ID && w.Approval == _Approval && w.Approval_Sub == _Approval_Sub && w.IsRevise != true).OrderByDescending(o => o.id).FirstOrDefault();

            if (documentID == 2)
            {
                while (getApprovalHistory == null && ApprovalHistoryTry < ApprovalHistoryTryMax)
                {
                    _Approval_Sub += 1;
                    getApprovalHistory = db.Approval_History.Where(w => w.Menu_Id == 75 && w.Document_Id == documentID && w.Reveral_ID == Reveral_ID && w.Approval == _Approval && w.Approval_Sub == _Approval_Sub && w.IsRevise != true).OrderByDescending(o => o.id).FirstOrDefault();
                    ApprovalHistoryTry++;
                }

                if (getApprovalHistory == null && ApprovalHistoryReTry < ApprovalHistoryReTryMax)
                {
                    ApprovalHistoryReTry++;
                    _Approval += 1;
                    _Approval_Sub = 0;
                    ApprovalHistoryTry = 0;
                    goto historyWhile;
                }
            }

            if (getType == 1)
            {
                str = getApprovalHistory?.Created_By_Name ?? "";
            }
            else if (getType == 2)
            {
                str = getApprovalHistory?.Created_At.ToString("dd-MMM-yyyy") ?? "";
            }
            else if (getType == 3)
            {
                str = getApprovalHistory?.Label ?? "";
            }
            else
            {
                str = getApprovalHistory?.Note ?? "";
            }

            return str;
        }

        public static StandardAmount getStandardAmount(string Level, string Destination)
        {
            var std = new StandardAmount();
            switch (Level)
            {
                case "BOD & Expatriate":
                case "BOD":
                    std.Daily = (Destination == "Overseas Route A (Asean)" ? 35 : (Destination == "Overseas Route B (Outside Asean)" ? 40 : 300000));
                    std.Hotel = (Destination == "Overseas Route A (Asean)" ? 0 : (Destination == "Overseas Route B (Outside Asean)" ? 0 : 900000));
                    std.Meals = (Destination == "Overseas Route A (Asean)" ? 12 : (Destination == "Overseas Route B (Outside Asean)" ? 14 : 55000));
                    break;
                case "GENERAL MANAGER":
                case "SENIOR MANAGER":
                case "SENIOR MANAGER (ACTING)":
                case "MANAGER":
                case "MANAGER (ACTING)":
                    std.Daily = (Destination == "Overseas Route A (Asean)" ? 30 : (Destination == "Overseas Route B (Outside Asean)" ? 35 : 230000));
                    std.Hotel = (Destination == "Overseas Route A (Asean)" ? 0 : (Destination == "Overseas Route B (Outside Asean)" ? 0 : 600000));
                    std.Meals = (Destination == "Overseas Route A (Asean)" ? 10 : (Destination == "Overseas Route B (Outside Asean)" ? 12 : 44000));
                    break;
                case "ASSISTANT MANAGER":
                case "ASSISTANT MANAGER (ACTING)":
                case "SENIOR STAFF":
                case "SENIOR STAFF (ACTING)":
                case "JUNIOR STAFF":
                    std.Daily = (Destination == "Overseas Route A (Asean)" ? 25 : (Destination == "Overseas Route B (Outside Asean)" ? 30 : 180000));
                    std.Hotel = (Destination == "Overseas Route A (Asean)" ? 0 : (Destination == "Overseas Route B (Outside Asean)" ? 0 : 400000));
                    std.Meals = (Destination == "Overseas Route A (Asean)" ? 8 : (Destination == "Overseas Route B (Outside Asean)" ? 10 : 33000));
                    break;
                default:
                    std.Daily = (Destination == "Overseas Route A (Asean)" ? 25 : (Destination == "Overseas Route B (Outside Asean)" ? 30 : 180000));
                    std.Hotel = (Destination == "Overseas Route A (Asean)" ? 0 : (Destination == "Overseas Route B (Outside Asean)" ? 0 : 400000));
                    std.Meals = (Destination == "Overseas Route A (Asean)" ? 8 : (Destination == "Overseas Route B (Outside Asean)" ? 10 : 33000));
                    break;
            }
            if (Destination == "Domestic (East Region)")
            {
                std.Meals = std.Meals + 10000;
            }
            return std;
        }
        public void setApprovalList(string deptCode, HC_Business_Trip_Settlement currSettlement)
        {
            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && w.Dept_Code == deptCode).ToList();
            foreach (var dList in getApprovalList)
            {
                var newApprovalList = new Approval_List();
                newApprovalList.Reveral_ID = currSettlement.ID.ToString();
                newApprovalList.Menu_Id = dList.Menu_Id;
                newApprovalList.Document_Id = dList.Document_Id;
                newApprovalList.User_NIK = (dList.Levels == 1 && dList.Levels_Sub == 0) ? currSettlement.NIK : dList.User_NIK;
                newApprovalList.Dept_Code = dList.Dept_Code;
                newApprovalList.Dept_Name = dList.Dept_Name;
                newApprovalList.Title = dList.Title;
                newApprovalList.Header = dList.Header;
                newApprovalList.Label = dList.Label;
                newApprovalList.Levels = dList.Levels;
                newApprovalList.Levels_Sub = dList.Levels_Sub;
                newApprovalList.Is_Skip = (dList.Levels == 1 && dList.Levels_Sub == 0) ? (dList.User_NIK == currSettlement.NIK ? false : true) : checkApprovalRoute(dList.Levels, dList.Levels_Sub);
                newApprovalList.IsEmailOnly = dList.IsEmailOnly;
                db.Approval_List.Add(newApprovalList);
            }
            db.SaveChanges();
        }

        public bool checkApprovalRoute(int level, int level_sub)
        {
            var isSkip = false;
            if ((level == 2 && level_sub == 0) || (level == 2 && level_sub == 3))
            {
                isSkip = true;
            }
            return isSkip;
        }
        public ActionResult RequestAdd()
        {
            var dtStart = (Request["iDateFrom"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateFrom"] + " " + (Request["iTimeFrom"].Length == 0 ? "00:00" : Request["iTimeFrom"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtEnd = (Request["iDateTo"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateTo"] + " " + (Request["iTimeTo"].Length == 0 ? "00:00" : Request["iTimeTo"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var newData = new HC_Business_Trip_Request();
            newData.NIK = Request.Form.GetValues("_iAddNIK[]")[0];
            newData.Req_Number = this.getSequence("BT");
            newData.Name = Request.Form.GetValues("_iAddName[]")[0];
            newData.Cost_ID = Request.Form.GetValues("_iCostID[]")[0];
            newData.Cost_Name = Request.Form.GetValues("_iCostName[]")[0];
            newData.Department = Request.Form.GetValues("_iAddDept[]")[0];
            newData.Section = Request.Form.GetValues("_iAddSect[]")[0];
            newData.Position = Request.Form.GetValues("_iAddPos[]")[0];
            newData.Destination = Request["iDestination"];
            newData.Date_From = dtStart;
            newData.Date_To = dtEnd;
            newData.Purpose = Request["iPurpose"];
            newData.Visited_To = Request["iVisitTo"];
            newData.Destination_Type = Request["iDestinationType"];
            newData.Travel_Method_By = Request["iPrepareBy"];
            newData.Travel_Method = Request["iMethod[]"];
            newData.Travel_Need = Request["iNeeds[]"];
            newData.Approval = 1;
            newData.isReject = false;
            newData.Created_At = DateTime.Now;
            newData.Created_By = currUser;
            dbBusinessTrip.HC_Business_Trip_Request.Add(newData);
            dbBusinessTrip.SaveChanges();

            for (var i = 0; i <= Request.Form.GetValues("iAddNIK[]").Count() - 1; i++)
            {
                if (Request.Form.GetValues("iAddNIK[]")[i].Length > 0)
                {
                    var newDataSub = new HC_Business_Trip_CA();
                    newDataSub.Request_ID = newData.ID;
                    newDataSub.NIK = Request.Form.GetValues("iAddNIK[]")[i];
                    newDataSub.Name = Request.Form.GetValues("iAddName[]")[i];
                    newDataSub.Department = Request.Form.GetValues("iAddDept[]")[i];
                    newDataSub.Section = Request.Form.GetValues("iAddSect[]")[i];
                    newDataSub.Position = Request.Form.GetValues("iAddPos[]")[i];
                    newDataSub.Hotel_Total = 0;
                    newDataSub.Meals_Total = 0;
                    newDataSub.Daily_Total = 0;
                    newDataSub.Transport_Total = 0;
                    newDataSub.Exchange_Rate = 1;
                    newDataSub.Misc_Total = 0;
                    dbBusinessTrip.HC_Business_Trip_CA.Add(newDataSub);
                }

                if (Request.Form.GetValues("iAddNIK[]")[i].Length > 0)
                {
                    var newDataSubSettlement = new HC_Business_Trip_Settlement();
                    newDataSubSettlement.Request_ID = newData.ID;
                    newDataSubSettlement.NIK = Request.Form.GetValues("iAddNIK[]")[i];
                    newDataSubSettlement.Name = Request.Form.GetValues("iAddName[]")[i];
                    newDataSubSettlement.Department = Request.Form.GetValues("iAddDept[]")[i];
                    newDataSubSettlement.Section = Request.Form.GetValues("iAddSect[]")[i];
                    newDataSubSettlement.Position = Request.Form.GetValues("iAddPos[]")[i];
                    newDataSubSettlement.Hotel_Total = 0;
                    newDataSubSettlement.Meals_Total = 0;
                    newDataSubSettlement.Daily_Total = 0;
                    newDataSubSettlement.Transport_Total = 0;
                    newDataSubSettlement.Misc_Total = 0;
                    newDataSubSettlement.Exchange_Rate = 1;
                    newDataSubSettlement.Approval = 1;
                    newDataSubSettlement.Approval_Sub = 0;
                    newDataSubSettlement.isRevise = false;
                    dbBusinessTrip.HC_Business_Trip_Settlement.Add(newDataSubSettlement);
                    dbBusinessTrip.SaveChanges();
                    setApprovalList(newDataSubSettlement.HC_Business_Trip_Request.Cost_ID, newDataSubSettlement);
                }
            }
            uploadAttachmentFormRequest(newData.Req_Number);

            return RedirectToAction("formRequest", "BusinessTrip", new { area = "HC" });
        }

        public ActionResult RequestEdit()
        {
            var dtStart = (Request["iDateFrom"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateFrom"] + " " + (Request["iTimeFrom"].Length == 0 ? "00:00" : Request["iTimeFrom"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtEnd = (Request["iDateTo"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateTo"] + " " + (Request["iTimeTo"].Length == 0 ? "00:00" : Request["iTimeTo"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var currReqNumber = Request["iReqNumber"];
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var updateData = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Req_Number == currReqNumber).FirstOrDefault();
            updateData.NIK = Request.Form.GetValues("_iAddNIK[]")[0];
            updateData.Name = Request.Form.GetValues("_iAddName[]")[0];
            updateData.Cost_ID = Request.Form.GetValues("_iCostID[]")[0];
            updateData.Cost_Name = Request.Form.GetValues("_iCostName[]")[0];
            updateData.Department = Request.Form.GetValues("_iAddDept[]")[0];
            updateData.Section = Request.Form.GetValues("_iAddSect[]")[0];
            updateData.Position = Request.Form.GetValues("_iAddPos[]")[0];
            updateData.Destination = Request["iDestination"];
            updateData.Date_From = dtStart;
            updateData.Date_To = dtEnd;
            updateData.Purpose = Request["iPurpose"];
            updateData.Visited_To = Request["iVisitTo"];
            updateData.Destination_Type = Request["iDestinationType"];
            updateData.Travel_Method_By = Request["iPrepareBy"];
            updateData.Travel_Method = Request["iMethod[]"];
            updateData.Travel_Need = Request["iNeeds[]"];
            dbBusinessTrip.SaveChanges();

            var nikList = Request.Form.GetValues("iAddNIK[]");

            var deleteNotExistsNIK = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.Request_ID == updateData.ID && !nikList.Contains(w.NIK)).ToList();
            foreach (var data in deleteNotExistsNIK)
            {
                var deleteDetail = dbBusinessTrip.HC_Business_Trip_CA_Detail.Where(w => w.CA_ID == data.ID && w.NIK == data.NIK).ToList();
                dbBusinessTrip.HC_Business_Trip_CA_Detail.RemoveRange(deleteDetail);
            }
            dbBusinessTrip.HC_Business_Trip_CA.RemoveRange(deleteNotExistsNIK);


            var deleteNotExistsNIKSettlement = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.Request_ID == updateData.ID && !nikList.Contains(w.NIK)).ToList();
            foreach (var data in deleteNotExistsNIKSettlement)
            {
                var deleteDetail = dbBusinessTrip.HC_Business_Trip_Settlement_Detail.Where(w => w.CA_ID == data.ID && w.NIK == data.NIK).ToList();
                dbBusinessTrip.HC_Business_Trip_Settlement_Detail.RemoveRange(deleteDetail);
                var deleteApprovalList = db.Approval_List.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && w.Reveral_ID == data.ID.ToString()).ToList();
                db.Approval_List.RemoveRange(deleteApprovalList);
            }
            db.SaveChanges();
            dbBusinessTrip.HC_Business_Trip_Settlement.RemoveRange(deleteNotExistsNIKSettlement);
            dbBusinessTrip.SaveChanges();
            for (var i = 0; i <= Request.Form.GetValues("iAddNIK[]").Count() - 1; i++)
            {
                if (Request.Form.GetValues("iAddNIK[]")[i].Length > 0)
                {
                    var currCheckNIK = Request.Form.GetValues("iAddNIK[]")[i];
                    var checkDataSub = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.Request_ID == updateData.ID && w.NIK == currCheckNIK).FirstOrDefault();
                    if (checkDataSub == null)
                    {
                        var updateDataSub = new HC_Business_Trip_CA();
                        updateDataSub.Request_ID = updateData.ID;
                        updateDataSub.NIK = Request.Form.GetValues("iAddNIK[]")[i];
                        updateDataSub.Name = Request.Form.GetValues("iAddName[]")[i];
                        updateDataSub.Department = Request.Form.GetValues("iAddDept[]")[i];
                        updateDataSub.Section = Request.Form.GetValues("iAddSect[]")[i];
                        updateDataSub.Position = Request.Form.GetValues("iAddPos[]")[i];
                        updateDataSub.Hotel_Total = 0;
                        updateDataSub.Meals_Total = 0;
                        updateDataSub.Daily_Total = 0;
                        updateDataSub.Transport_Total = 0;
                        updateDataSub.Misc_Total = 0;
                        updateDataSub.Exchange_Rate = 1;
                        dbBusinessTrip.HC_Business_Trip_CA.Add(updateDataSub);
                    }
                }

                if (Request.Form.GetValues("iAddNIK[]")[i].Length > 0)
                {
                    var currCheckNIK = Request.Form.GetValues("iAddNIK[]")[i];
                    var checkDataSubSettlement = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.Request_ID == updateData.ID && w.NIK == currCheckNIK).FirstOrDefault();
                    if (checkDataSubSettlement == null)
                    {
                        var newDataSubSettlement = new HC_Business_Trip_Settlement();
                        newDataSubSettlement.Request_ID = updateData.ID;
                        newDataSubSettlement.NIK = Request.Form.GetValues("iAddNIK[]")[i];
                        newDataSubSettlement.Name = Request.Form.GetValues("iAddName[]")[i];
                        newDataSubSettlement.Department = Request.Form.GetValues("iAddDept[]")[i];
                        newDataSubSettlement.Section = Request.Form.GetValues("iAddSect[]")[i];
                        newDataSubSettlement.Position = Request.Form.GetValues("iAddPos[]")[i];
                        newDataSubSettlement.Hotel_Total = 0;
                        newDataSubSettlement.Meals_Total = 0;
                        newDataSubSettlement.Daily_Total = 0;
                        newDataSubSettlement.Transport_Total = 0;
                        newDataSubSettlement.Misc_Total = 0;
                        newDataSubSettlement.Exchange_Rate = 1;
                        newDataSubSettlement.Approval = 1;
                        newDataSubSettlement.Approval_Sub = 0;
                        dbBusinessTrip.HC_Business_Trip_Settlement.Add(newDataSubSettlement);
                        dbBusinessTrip.SaveChanges();
                        setApprovalList(newDataSubSettlement.HC_Business_Trip_Request.Cost_ID, newDataSubSettlement);

                    }
                }
            }
            uploadAttachmentFormRequest(updateData.Req_Number);
            return RedirectToAction("formRequest", "BusinessTrip", new { area = "HC", ReqNumber = currReqNumber });
        }


        public ActionResult CAAdd()
        {
            var currNIK = Request["iCurrNIK"];
            var currDetailID = Int32.Parse(Request["iCurrDetailID"]);
            var updateData = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.ID == currDetailID && w.NIK == currNIK).FirstOrDefault();
            updateData.Hotel_Total = double.Parse(Request["iHotelTotal"]);
            updateData.Meals_Total = double.Parse(Request["iMealsTotal"]);
            updateData.Daily_Total = double.Parse(Request["iDailyTotal"]);
            updateData.Transport_Total = double.Parse(Request["iTransportTotal"]);
            updateData.Misc_Total = double.Parse(Request["iMiscellanousTotal"]);
            updateData.Exchange_Rate = double.Parse(Request["iExchangeRate"]);
            updateData.Hotel_Budget = Request["iBudgetNumberHotel"];
            updateData.Daily_Budget = Request["iBudgetNumberDaily"];
            updateData.Meals_Budget = Request["iBudgetNumberMeals"];
            updateData.Flight_Budget = Request["iBudgetNumberFlight"];
            updateData.RentCar_Budget = Request["iBudgetNumberRentCar"];
            updateData.Entertaintment_Budget = Request["iBudgetNumberEntertainment"];
            updateData.Gasoline_Budget = Request["iBudgetNumberGasoline"];
            updateData.Toll_Budget = Request["iBudgetNumberToll"];
            updateData.Baggage_Budget = Request["iBudgetNumberBaggage"];
            updateData.Taxi_Budget = Request["iBudgetNumberTaxi"];
            dbBusinessTrip.SaveChanges();

            for (var i = 0; i <= Request.Form.GetValues("iDetailItemType[]").Count() - 1; i++)
            {
                var newDataSub = new HC_Business_Trip_CA_Detail();
                newDataSub.CA_ID = updateData.ID;
                newDataSub.NIK = Request["iCurrNIK"];
                newDataSub.Item_Type = Request.Form.GetValues("iDetailItemType[]")[i];
                newDataSub.Item = Request.Form.GetValues("iDetailItem[]")[i];
                newDataSub.CA_Amount = double.Parse(Request.Form.GetValues("iAmount[]")[i]);
                newDataSub.CA_Days = Int32.Parse(Request.Form.GetValues("iDays[]")[i]);
                newDataSub.CA_Total = double.Parse(Request.Form.GetValues("iDetailTotal[]")[i]);
                dbBusinessTrip.HC_Business_Trip_CA_Detail.Add(newDataSub);
            }

            dbBusinessTrip.SaveChanges();

            return RedirectToAction("formCA", "BusinessTrip", new { area = "HC", ReqNumber = Request["iCurrReqNumber"], NIK = updateData.NIK });
        }
        public ActionResult CAEdit()
        {
            var currNIK = Request["iCurrNIK"];
            var currDetailID = Int32.Parse(Request["iCurrDetailID"]);
            var updateData = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.ID == currDetailID && w.NIK == currNIK).FirstOrDefault();
            updateData.Hotel_Total = double.Parse(Request["iHotelTotal"]);
            updateData.Meals_Total = double.Parse(Request["iMealsTotal"]);
            updateData.Daily_Total = double.Parse(Request["iDailyTotal"]);
            updateData.Transport_Total = double.Parse(Request["iTransportTotal"]);
            updateData.Misc_Total = double.Parse(Request["iMiscellanousTotal"]);
            updateData.Exchange_Rate = double.Parse(Request["iExchangeRate"]);
            updateData.Hotel_Budget = Request["iBudgetNumberHotel"].Split('|')[0];
            updateData.Daily_Budget = Request["iBudgetNumberDaily"].Split('|')[0];
            updateData.Meals_Budget = Request["iBudgetNumberMeals"].Split('|')[0];
            updateData.Flight_Budget = Request["iBudgetNumberFlight"].Split('|')[0];
            updateData.RentCar_Budget = Request["iBudgetNumberRentCar"].Split('|')[0];
            updateData.Entertaintment_Budget = Request["iBudgetNumberEntertainment"].Split('|')[0];
            updateData.Gasoline_Budget = Request["iBudgetNumberGasoline"].Split('|')[0];
            updateData.Toll_Budget = Request["iBudgetNumberToll"].Split('|')[0];
            updateData.Baggage_Budget = Request["iBudgetNumberBaggage"].Split('|')[0];
            updateData.Taxi_Budget = Request["iBudgetNumberTaxi"].Split('|')[0];
            dbBusinessTrip.SaveChanges();

            for (var i = 0; i <= Request.Form.GetValues("iDetailItemType[]").Count() - 1; i++)
            {
                var currItemType = Request.Form.GetValues("iDetailItemType[]")[i];
                var currItem = Request.Form.GetValues("iDetailItem[]")[i];
                var updateDataSub = dbBusinessTrip.HC_Business_Trip_CA_Detail.Where(w => w.CA_ID == updateData.ID && w.NIK == currNIK && w.Item_Type == currItemType && w.Item == currItem).FirstOrDefault();
                if (updateDataSub != null)
                {
                    updateDataSub.CA_Amount = double.Parse(Request.Form.GetValues("iAmount[]")[i]);
                    updateDataSub.CA_Days = Int32.Parse(Request.Form.GetValues("iDays[]")[i]);
                    updateDataSub.CA_Total = double.Parse(Request.Form.GetValues("iDetailTotal[]")[i]);
                }
                else
                {
                    var newDataSub = new HC_Business_Trip_CA_Detail();
                    newDataSub.CA_ID = updateData.ID;
                    newDataSub.NIK = Request["iCurrNIK"];
                    newDataSub.Item_Type = Request.Form.GetValues("iDetailItemType[]")[i];
                    newDataSub.Item = Request.Form.GetValues("iDetailItem[]")[i];
                    newDataSub.CA_Amount = double.Parse(Request.Form.GetValues("iAmount[]")[i]);
                    newDataSub.CA_Days = Int32.Parse(Request.Form.GetValues("iDays[]")[i]);
                    newDataSub.CA_Total = double.Parse(Request.Form.GetValues("iDetailTotal[]")[i]);
                    dbBusinessTrip.HC_Business_Trip_CA_Detail.Add(newDataSub);
                }
            }

            dbBusinessTrip.SaveChanges();

            return RedirectToAction("formCA", "BusinessTrip", new { area = "HC", ReqNumber = Request["iCurrReqNumber"], NIK = updateData.NIK });
        }

        public ActionResult SettlementAdd()
        {
            var currNIK = Request["iCurrNIK"];
            var currDetailID = Int32.Parse(Request["iCurrDetailID"]);
            var updateData = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.ID == currDetailID && w.NIK == currNIK).FirstOrDefault();
            updateData.Hotel_Total = double.Parse(Request["iHotelTotal"]);
            updateData.Meals_Total = double.Parse(Request["iMealsTotal"]);
            updateData.Daily_Total = double.Parse(Request["iDailyTotal"]);
            updateData.Transport_Total = double.Parse(Request["iTransportTotal"]);
            updateData.Misc_Total = double.Parse(Request["iMiscellanousTotal"]);
            updateData.Exchange_Rate = double.Parse(Request["iExchangeRate"]);
            updateData.Hotel_Budget = Request["iBudgetNumberHotel"];
            updateData.Daily_Budget = Request["iBudgetNumberDaily"];
            updateData.Meals_Budget = Request["iBudgetNumberMeals"];
            updateData.Flight_Budget = Request["iBudgetNumberFlight"];
            updateData.RentCar_Budget = Request["iBudgetNumberRentCar"];
            updateData.Entertaintment_Budget = Request["iBudgetNumberEntertainment"];
            updateData.Gasoline_Budget = Request["iBudgetNumberGasoline"];
            updateData.Toll_Budget = Request["iBudgetNumberToll"];
            updateData.Baggage_Budget = Request["iBudgetNumberBaggage"];
            updateData.Taxi_Budget = Request["iBudgetNumberTaxi"];
            dbBusinessTrip.SaveChanges();

            for (var i = 0; i <= Request.Form.GetValues("iDetailItemType[]").Count() - 1; i++)
            {
                var newDataSub = new HC_Business_Trip_Settlement_Detail();
                newDataSub.CA_ID = updateData.ID;
                newDataSub.NIK = Request["iCurrNIK"];
                newDataSub.Item_Type = Request.Form.GetValues("iDetailItemType[]")[i];
                newDataSub.Item = Request.Form.GetValues("iDetailItem[]")[i];
                newDataSub.CA_Amount = double.Parse(Request.Form.GetValues("iAmount[]")[i]);
                newDataSub.CA_Days = Int32.Parse(Request.Form.GetValues("iDays[]")[i]);
                newDataSub.CA_Total = double.Parse(Request.Form.GetValues("iDetailTotal[]")[i]);
                dbBusinessTrip.HC_Business_Trip_Settlement_Detail.Add(newDataSub);
            }

            dbBusinessTrip.SaveChanges();
            uploadAttachment();

            return RedirectToAction("formSettlement", "BusinessTrip", new { area = "HC", ReqNumber = Request["iCurrReqNumber"], NIK = updateData.NIK });
        }
        public ActionResult SettlementEdit()
        {
            var currNIK = Request["iCurrNIK"];
            var currDetailID = Int32.Parse(Request["iCurrDetailID"]);
            var updateData = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.ID == currDetailID && w.NIK == currNIK).FirstOrDefault();
            updateData.Hotel_Total = double.Parse(Request["iHotelTotal"]);
            updateData.Meals_Total = double.Parse(Request["iMealsTotal"]);
            updateData.Daily_Total = double.Parse(Request["iDailyTotal"]);
            updateData.Transport_Total = double.Parse(Request["iTransportTotal"]);
            updateData.Misc_Total = double.Parse(Request["iMiscellanousTotal"]);
            updateData.Exchange_Rate = double.Parse(Request["iExchangeRate"]);
            updateData.Hotel_Budget = Request["iBudgetNumberHotel"].Split('|')[0];
            updateData.Daily_Budget = Request["iBudgetNumberDaily"].Split('|')[0];
            updateData.Meals_Budget = Request["iBudgetNumberMeals"].Split('|')[0];
            updateData.Flight_Budget = Request["iBudgetNumberFlight"].Split('|')[0];
            updateData.RentCar_Budget = Request["iBudgetNumberRentCar"].Split('|')[0];
            updateData.Entertaintment_Budget = Request["iBudgetNumberEntertainment"].Split('|')[0];
            updateData.Gasoline_Budget = Request["iBudgetNumberGasoline"].Split('|')[0];
            updateData.Toll_Budget = Request["iBudgetNumberToll"].Split('|')[0];
            updateData.Baggage_Budget = Request["iBudgetNumberBaggage"].Split('|')[0];
            updateData.Taxi_Budget = Request["iBudgetNumberTaxi"].Split('|')[0];
            if (updateData.isRevise == true)
            {
                updateData.isRevise = false;
                sendNotification(updateData.HC_Business_Trip_Request.Req_Number, "formSettlement", "Revise Done", updateData.NIK, updateData.HC_Business_Trip_Request.Cost_ID, updateData.Approval, updateData.Approval_Sub, "");
            }
            dbBusinessTrip.SaveChanges();

            for (var i = 0; i <= Request.Form.GetValues("iDetailItemType[]").Count() - 1; i++)
            {
                var currItemType = Request.Form.GetValues("iDetailItemType[]")[i];
                var currItem = Request.Form.GetValues("iDetailItem[]")[i];
                var updateDataSub = dbBusinessTrip.HC_Business_Trip_Settlement_Detail.Where(w => w.CA_ID == updateData.ID && w.NIK == currNIK && w.Item_Type == currItemType && w.Item == currItem).FirstOrDefault();
                if (updateDataSub != null)
                {
                    updateDataSub.CA_Amount = double.Parse(Request.Form.GetValues("iAmount[]")[i]);
                    updateDataSub.CA_Days = Int32.Parse(Request.Form.GetValues("iDays[]")[i]);
                    updateDataSub.CA_Total = double.Parse(Request.Form.GetValues("iDetailTotal[]")[i]);
                }
                else
                {
                    var newDataSub = new HC_Business_Trip_Settlement_Detail();
                    newDataSub.CA_ID = updateData.ID;
                    newDataSub.NIK = Request["iCurrNIK"];
                    newDataSub.Item_Type = Request.Form.GetValues("iDetailItemType[]")[i];
                    newDataSub.Item = Request.Form.GetValues("iDetailItem[]")[i];
                    newDataSub.CA_Amount = double.Parse(Request.Form.GetValues("iAmount[]")[i]);
                    newDataSub.CA_Days = Int32.Parse(Request.Form.GetValues("iDays[]")[i]);
                    newDataSub.CA_Total = double.Parse(Request.Form.GetValues("iDetailTotal[]")[i]);
                    dbBusinessTrip.HC_Business_Trip_Settlement_Detail.Add(newDataSub);
                }
            }

            dbBusinessTrip.SaveChanges();
            uploadAttachment();
            return RedirectToAction("formSettlement", "BusinessTrip", new { area = "HC", ReqNumber = Request["iCurrReqNumber"], NIK = updateData.NIK });
        }

        public ActionResult requestSign()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var reqNumber = Request["iSignReqNumber"];
            var btnType = Request["btnType"];
            var currNote = Request["iBTNote"] ?? "";
            var updateSign = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Req_Number == reqNumber).FirstOrDefault();

            if (btnType == "Sign")
            {
                if (updateSign.Approval == 1 || (updateSign.Approval == 2 && updateSign.Approval_Sub == 2) || updateSign.Approval == 3)
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
                else
                {
                    updateSign.Approval_Sub += 1;
                }
            }
            else if (btnType == "Return")
            {
                updateSign.Approval = 1;
                updateSign.Approval_Sub = 0;
            }
            else
            {
                if (updateSign.Approval == 1 || (updateSign.Approval == 2 && updateSign.Approval_Sub == 2) || updateSign.Approval == 3)
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
                else
                {
                    updateSign.Approval_Sub += 1;
                }
                updateSign.isReject = true;
            }
            dbBusinessTrip.SaveChanges();

            var currApproval = btnType == "Reject" ? 1 : updateSign.Approval;
            var currApprovalSub = btnType == "Reject" ? 0 : updateSign.Approval_Sub;

            var getApprovalMaster = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Document_Id == 1 && w.Menu_Id == 75).FirstOrDefault();
            var updateSignHistory = new Approval_History();
            updateSignHistory.Menu_Id = 75;
            updateSignHistory.Menu_Name = "Business Trip";
            updateSignHistory.Document_Id = 1;
            updateSignHistory.Document_Name = "Form Request";
            updateSignHistory.Reveral_ID = reqNumber;
            updateSignHistory.Reveral_ID_Sub = null;
            updateSignHistory.Title = getApprovalMaster.Title;
            updateSignHistory.Note = currNote;
            updateSignHistory.Approval = updateSign.Approval;
            updateSignHistory.Approval_Sub = updateSign.Approval_Sub;
            updateSignHistory.IsReject = updateSign.isReject ?? false;
            updateSignHistory.Status = (btnType != "Sign" ? btnType : ApprovalStatus(updateSign.Approval, updateSign.Approval_Sub));
            updateSignHistory.Created_At = DateTime.Now;
            updateSignHistory.Created_By_ID = currUserID;
            updateSignHistory.Created_By_Name = currUserName;
            db.Approval_History.Add(updateSignHistory);
            db.SaveChanges();

            sendNotification(reqNumber, "formRequest", btnType, updateSign.NIK, updateSign.Cost_ID, currApproval, currApprovalSub, currNote);

            return RedirectToAction("formRequest", "BusinessTrip", new { area = "HC", ReqNumber = reqNumber });
        }



        public ActionResult settlementSign()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var reqNumber = Request["iSignReqNumber"];
            var NIK = Request["iSignNIK"];
            var btnType = Request["btnType"];
            var currNote = Request["iBTNote"] ?? "";
            var requestData = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Req_Number == reqNumber).FirstOrDefault();
            var requestID = requestData.ID;
            var updateSign = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.Request_ID == requestID && w.NIK == NIK).FirstOrDefault();
            var currReveralID = updateSign.ID.ToString();
            var currCostID = updateSign.HC_Business_Trip_Request.Cost_ID;
            var checkApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == currReveralID && w.Dept_Code == currCostID && w.Document_Id == 2 && w.Menu_Id == 75 && w.Levels == updateSign.Approval && w.Levels_Sub > updateSign.Approval_Sub && w.Is_Skip == false && w.IsEmailOnly == false).OrderBy(o => o.Levels_Sub).FirstOrDefault();
            if (checkApprovalMaster == null)
            {
                checkApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == currReveralID && w.Dept_Code == currCostID && w.Document_Id == 2 && w.Menu_Id == 75 && w.Levels > updateSign.Approval && w.Is_Skip == false && w.IsEmailOnly == false).OrderBy(o => o.Levels).ThenBy(o => o.Levels_Sub).FirstOrDefault();
            }
            var getApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == currReveralID && w.User_NIK == currUserID && w.Document_Id == 2 && w.Menu_Id == 75 && w.Levels == updateSign.Approval && w.Levels_Sub == updateSign.Approval_Sub && w.Is_Skip == false && w.IsEmailOnly == false).FirstOrDefault();
            if(getApprovalMaster == null && (updateSign.Approval + updateSign.Approval_Sub > 1))
            {
                goto skipSign;
            }

            updateSign.isRevise = false;
            if (btnType == "Sign")
            {
                //if ((updateSign.Approval == 1 && updateSign.Approval_Sub < 1) || (updateSign.Approval == 2 && updateSign.Approval_Sub < 4) || (updateSign.Approval == 3 && updateSign.Approval_Sub < 1))
                //{
                //    updateSign.Approval_Sub += 1;
                //}

                //if from Traveller to GA
                if (updateSign.Approval == 1 && updateSign.Approval_Sub == 0)
                {
                    var getAllDeptEmailOnly = db.Approval_List.Where(w => w.Reveral_ID == currReveralID && w.Dept_Code == updateSign.HC_Business_Trip_Request.Cost_ID && w.Document_Id == 2 && w.Menu_Id == 75 && w.Levels == checkApprovalMaster.Levels && w.Levels_Sub < checkApprovalMaster.Levels_Sub && w.Is_Skip == false && w.IsEmailOnly == true).ToList();
                    foreach (var emailOnly in getAllDeptEmailOnly)
                    {
                        //sendNotification(currReveralID, "Quotation", btnType, updateSign.Created_By, updateSign.Section.Split('|')[0].Trim(), emailOnly.Levels, emailOnly.Levels_Sub, currNote);
                        sendNotification(reqNumber, "formSettlement", btnType, emailOnly.User_NIK, requestData.Cost_ID, emailOnly.Levels, emailOnly.Levels_Sub, currNote);
                    }
                }

                if (checkApprovalMaster != null)
                {
                    updateSign.Approval = checkApprovalMaster.Levels;
                    updateSign.Approval_Sub = checkApprovalMaster.Levels_Sub;
                }
                else
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
            }
            else if (btnType == "Return")
            {
                updateSign.Approval = 1;
                updateSign.Approval_Sub = 0;
            }
            else if (btnType == "Revise")
            {
                updateSign.isRevise = true;
            }
            else
            {
                //if ((updateSign.Approval == 1 && updateSign.Approval_Sub < 1) || (updateSign.Approval == 2 && updateSign.Approval_Sub < 4) || (updateSign.Approval == 3 && updateSign.Approval_Sub < 1))
                //{
                //    updateSign.Approval_Sub += 1;
                //}
                if (checkApprovalMaster != null)
                {
                    updateSign.Approval = checkApprovalMaster.Levels;
                    updateSign.Approval_Sub = checkApprovalMaster.Levels_Sub;
                }
                else
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
                updateSign.isReject = true;
            }

            dbBusinessTrip.SaveChanges();
            var currApproval = btnType == "Reject" ? 1 : updateSign.Approval;
            var currApprovalSub = btnType == "Reject" ? 0 : updateSign.Approval_Sub;

            var updateSignHistory = new Approval_History();
            updateSignHistory.Menu_Id = 75;
            updateSignHistory.Menu_Name = "Business Trip";
            updateSignHistory.Document_Id = 2;
            updateSignHistory.Document_Name = "Settlement";
            updateSignHistory.Reveral_ID = updateSign.ID.ToString();
            updateSignHistory.Reveral_ID_Sub = updateSign.NIK.ToString();
            updateSignHistory.Title = getApprovalMaster?.Title ?? "Creator";
            updateSignHistory.Header = getApprovalMaster?.Header ?? "Prepared By";
            updateSignHistory.Label = getApprovalMaster?.Label ?? "General Affair";
            updateSignHistory.Note = currNote;
            updateSignHistory.Approval = updateSign.Approval;
            updateSignHistory.Approval_Sub = updateSign.Approval_Sub;
            updateSignHistory.IsReject = updateSign.isReject ?? false;
            updateSignHistory.IsRevise = updateSign.isRevise ?? false;
            updateSignHistory.Status = (btnType != "Sign" ? btnType : ApprovalStatus(updateSign.Approval, updateSign.Approval_Sub, 2));
            updateSignHistory.Created_At = DateTime.Now;
            updateSignHistory.Created_By_ID = currUserID;
            updateSignHistory.Created_By_Name = currUserName;
            db.Approval_History.Add(updateSignHistory);
            db.SaveChanges();

            sendNotification(reqNumber, "formSettlement", btnType, NIK, requestData.Cost_ID, currApproval, currApprovalSub, currNote);
        skipSign:
            return RedirectToAction("formSettlement", "BusinessTrip", new { area = "HC", ReqNumber = reqNumber, NIK = NIK });
        }


        [HttpPost]
        [Authorize]
        public ActionResult uploadAttachment()
        {
            var currExpenses = Request["iExpenses"];
            var currReqNumber = Request["iReqNumber"];
            var currSettlementID = int.Parse(Request["iSettlementID"]);
            var currNIK = Request["iNIK"];
            string checkFolder = "~/Files/HC/BusinessTrip/Settlement/" + currReqNumber + "/" + currSettlementID; // Your code goes here
            bool exists = System.IO.Directory.Exists(Server.MapPath(checkFolder));
            if (!exists)
                System.IO.Directory.CreateDirectory(Server.MapPath(checkFolder));
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase iFile = Request.Files[i];
                // extract only the filename
                if (iFile.ContentLength > 0)
                {
                    var fileName = iFile.FileName.Replace("+", "&");
                    string extension = System.IO.Path.GetExtension(fileName);
                    // store the file inside ~/App_Data/uploads folder
                    var path = System.IO.Path.Combine(Server.MapPath("~/Files/HC/BusinessTrip/Settlement/" + currReqNumber + "/" + currSettlementID), fileName);
                    iFile.SaveAs(path);
                    var checkFile = dbBusinessTrip.HC_Business_Trip_Settlement_Attachment.Where(w => w.ReqNumber == currReqNumber && w.Settlement_ID == currSettlementID && w.Expenses == currExpenses && w.Filename == fileName).FirstOrDefault();
                    if (checkFile == null)
                    {
                        dbBusinessTrip.HC_Business_Trip_Settlement_Attachment.Add(new HC_Business_Trip_Settlement_Attachment()
                        {
                            Settlement_ID = currSettlementID,
                            ReqNumber = currReqNumber,
                            Expenses = currExpenses,
                            Filename = fileName,
                            Ext = extension

                        });
                    }
                }
            }
            dbBusinessTrip.SaveChanges();

            return RedirectToAction("formSettlement", "BusinessTrip", new { area = "HC", ReqNumber = currReqNumber, NIK = currNIK });
        }

        [HttpPost]
        [Authorize]
        public ActionResult uploadAttachmentFormRequest(string ReqNumber)
        {

            //var currReqNumber = Request["iReqNumber"];
            var currReqNumber = ReqNumber;
            if (currReqNumber != null && currReqNumber.Length > 0)
            {
                string checkFolder = "~/Files/HC/BusinessTrip/Request/" + currReqNumber; // Your code goes here
                bool exists = System.IO.Directory.Exists(Server.MapPath(checkFolder));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath(checkFolder));
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase iFile = Request.Files[i];
                    // extract only the filename
                    if (iFile.ContentLength > 0)
                    {
                        var fileName = iFile.FileName.Replace("+", "&");
                        string extension = System.IO.Path.GetExtension(fileName);
                        // store the file inside ~/App_Data/uploads folder
                        var path = System.IO.Path.Combine(Server.MapPath("~/Files/HC/BusinessTrip/Request/" + currReqNumber), fileName);
                        iFile.SaveAs(path);
                        var checkFile = dbBusinessTrip.HC_Business_Trip_Request_Attachment.Where(w => w.ReqNumber == currReqNumber && w.Filename == fileName).FirstOrDefault();
                        if (checkFile == null)
                        {
                            dbBusinessTrip.HC_Business_Trip_Request_Attachment.Add(new HC_Business_Trip_Request_Attachment()
                            {
                                ReqNumber = currReqNumber,
                                Filename = fileName,
                                Ext = extension
                            });
                        }
                    }
                }
                dbBusinessTrip.SaveChanges();
            }
            return RedirectToAction("formRequest", "BusinessTrip", new { area = "HC", ReqNumber = currReqNumber });
        }

        [HttpPost]
        public ActionResult getSettlementAttachment()
        {
            var currExpenses = Request["iExpenses"];
            var currReqNumber = Request["iReqNumber"];
            var currSettlementID = int.Parse(Request["iSettlementID"]);

            var getFiles = dbBusinessTrip.HC_Business_Trip_Settlement_Attachment.Where(w => w.ReqNumber == currReqNumber && w.Settlement_ID == currSettlementID && w.Expenses == currExpenses).Select(s => new { filename = s.Filename, ext = s.Ext, id = s.ID });

            return Json(new { files = getFiles }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult getRequestAttachment()
        {
            var currReqNumber = Request["iReqNumber"];

            var getFiles = dbBusinessTrip.HC_Business_Trip_Request_Attachment.Where(w => w.ReqNumber == currReqNumber).Select(s => new { filename = s.Filename, ext = s.Ext, id = s.ID });

            return Json(new { files = getFiles }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult deleteSettlementAttachment()
        {
            var currID = Int32.Parse(Request["iID"]);
            var del = dbBusinessTrip.HC_Business_Trip_Settlement_Attachment.Where(w => w.ID == currID).FirstOrDefault();

            var path = Server.MapPath("~/Files/HC/BusinessTrip/Settlement/" + del.ReqNumber + "/" + del.Settlement_ID + "/" + del.Filename);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            dbBusinessTrip.HC_Business_Trip_Settlement_Attachment.Remove(del);
            dbBusinessTrip.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult deleteRequestAttachment()
        {
            var currID = Int32.Parse(Request["iID"]);
            var del = dbBusinessTrip.HC_Business_Trip_Request_Attachment.Where(w => w.ID == currID).FirstOrDefault();

            var path = Server.MapPath("~/Files/HC/BusinessTrip/Request/" + del.ReqNumber + "/" + del.Filename);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            dbBusinessTrip.HC_Business_Trip_Request_Attachment.Remove(del);
            dbBusinessTrip.SaveChanges();

            return Content(Boolean.TrueString);
        }

        public ActionResult checkSettlement()
        {
            var currNIK = Request["iNIK"];
            DateTime twoWeekDate = DateTime.Now.AddDays(14);
            var checkSettled = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.NIK == currNIK && (w.Approval == 1 && w.Approval_Sub == 0) && w.HC_Business_Trip_Request.Approval >= 4 && w.HC_Business_Trip_Request.Date_To > twoWeekDate).ToList();
            var convertJSON = JsonConvert.SerializeObject(checkSettled, Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            return Content(convertJSON, "application/json");
        }

        public ActionResult requestDelete()
        {
            var currReqNumber = Request["iReqNumber"];

            var deleteRequest = dbBusinessTrip.HC_Business_Trip_Request.Where(w => w.Req_Number == currReqNumber).FirstOrDefault();
            var requestID = deleteRequest.ID;
            if (deleteRequest != null)
            {
                dbBusinessTrip.HC_Business_Trip_Request.Remove(deleteRequest);

                var deleteCA = dbBusinessTrip.HC_Business_Trip_CA.Where(w => w.Request_ID == requestID).ToList();
                foreach (var delCA in deleteCA)
                {
                    var deleteCADetail = dbBusinessTrip.HC_Business_Trip_CA_Detail.Where(w => w.CA_ID == delCA.ID).ToList();
                    dbBusinessTrip.HC_Business_Trip_CA_Detail.RemoveRange(deleteCADetail);
                }
                dbBusinessTrip.HC_Business_Trip_CA.RemoveRange(deleteCA);

                var deleteSettlement = dbBusinessTrip.HC_Business_Trip_Settlement.Where(w => w.Request_ID == requestID).ToList();
                foreach (var delSettlement in deleteSettlement)
                {
                    var deleteSettlementDetail = dbBusinessTrip.HC_Business_Trip_Settlement_Detail.Where(w => w.CA_ID == delSettlement.ID).ToList();
                    dbBusinessTrip.HC_Business_Trip_Settlement_Detail.RemoveRange(deleteSettlementDetail);
                    var deleteApprovalList = db.Approval_List.Where(w => w.Menu_Id == 75 && w.Document_Id == 2 && w.Reveral_ID == delSettlement.ID.ToString()).ToList();
                    db.Approval_List.RemoveRange(deleteApprovalList);
                }
                dbBusinessTrip.HC_Business_Trip_Settlement.RemoveRange(deleteSettlement);
            }
            db.SaveChanges();
            dbBusinessTrip.SaveChanges();

            return RedirectToAction("formRequest", "BusinessTrip", new { area = "HC" });
        }

        public JsonResult SettlementCommentGet()
        {
            var currReqNumber = Request["iReqNumber"];
            var currReqNIK = Request["iReqNIK"];
            var currNIK = Request["iNIK"];
            var getComments = dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Include("Attachments").Where(w => w.ReqNumber == currReqNumber && w.ReqNIK == currReqNIK).ToList();
            foreach (var data in getComments)
            {
                bool checkCurrentUser = (currNIK == data.nik ? true : false);
                data.created_by_current_user = checkCurrentUser;
            }

            return Json(getComments, JsonRequestBehavior.AllowGet);
        }
        public JsonResult SettlementCommentAdd()
        {
            var currID = Request["id"];
            var currParent = Request["parent"];
            var currCreated = Request["created"];
            var currModified = Request["modified"];
            var currContent = Request["content"];
            var currAttachments = Request["attachments"];
            var currFullName = Request["fullname"];
            var currProfilePicture = Request["profile_picture_url"];
            var currCurrentUser = Request["created_by_current_user"];
            var currUpvoteCount = Request["upvote_count"];
            var currUserHasUpvoted = Request["user_has_upvoted"];
            var currReqNumber = Request["ReqNumber"];
            var currReqNIK = Request["ReqNIK"];
            var currNIK = Request["nik"];
            var checkData = dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Where(w => w.id == currID && w.ReqNumber == currReqNumber).OrderByDescending(o => o.id).FirstOrDefault();

            var newData = new HC_Business_Trip_Settlement_Comments();
            newData.id = checkData != null ? "c" + (int.Parse(checkData.id.Replace("c", "")) + 1) : currID;
            newData.parent = currParent.Length > 0 && currParent != "null" ? currParent : null;
            newData.created = currCreated;
            newData.content = currContent;
            //newData.attachments = currAttachments != null ? currAttachments : "attachments";
            newData.fullname = currFullName;
            newData.created_by_current_user = bool.Parse(currCurrentUser);
            newData.upvoteCount = int.Parse(currUpvoteCount);
            newData.userHasUpvoted = bool.Parse(currUserHasUpvoted);
            newData.ReqNumber = currReqNumber;
            newData.ReqNIK = currReqNIK;
            newData.nik = currNIK;
            dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Add(newData);
            dbBusinessTrip.SaveChanges();

            //uploadQTAttachment(currReqNumber, "", newData.comment_id);
            sendNotification(currReqNumber, "formSettlement", "Comment", currReqNIK, "", 0, 0, currContent);

            return Json(newData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult SettlementCommentDelete()
        {
            var currID = Request["id"];
            var currReqNumber = Request["ReqNumber"];
            var currReqNIK = Request["ReqNIK"];
            var currNIK = Request["nik"];
            var deleteData = dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Where(w => w.id == currID && w.ReqNumber == currReqNumber && w.ReqNIK == currReqNIK).FirstOrDefault();
            dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Remove(deleteData);
            dbBusinessTrip.SaveChanges();

            return Json(JsonConvert.SerializeObject(deleteData), JsonRequestBehavior.AllowGet);
        }
        public ActionResult SettlementCommentEdit()
        {
            var currID = Request["id"];
            var currParent = Request["parent"];
            var currCreated = Request["created"];
            var currModified = Request["modified"];
            var currContent = Request["content"];
            var currAttachments = Request["attachments"];
            var currFullName = Request["fullname"];
            var currProfilePicture = Request["profile_picture_url"];
            var currCurrentUser = Request["created_by_current_user"];
            var currReqNumber = Request["ReqNumber"];
            var currReqNIK = Request["ReqNIK"];
            var currNIK = Request["nik"];
            var newData = dbBusinessTrip.HC_Business_Trip_Settlement_Comments.Include("Attachments").Where(w => w.id == currID && w.ReqNumber == currReqNumber && w.ReqNIK == currReqNIK).FirstOrDefault();
            newData.id = currID;
            newData.parent = currParent.Length > 0 && currParent != "null" ? currParent : null;
            newData.created = currCreated;
            newData.modified = long.Parse(currModified);
            newData.content = currContent;
            //newData.attachments = currAttachments != null ? currAttachments : "attachments";
            newData.fullname = currFullName;
            newData.created_by_current_user = bool.Parse(currCurrentUser);
            newData.ReqNumber = currReqNumber;
            newData.ReqNIK = currReqNIK;
            newData.nik = currNIK;
            dbBusinessTrip.SaveChanges();
            sendNotification(currReqNumber, "formSettlement", "Comment", currReqNIK, "", 0, 0, currContent);

            return Json(newData, JsonRequestBehavior.AllowGet);
        }


        public JsonResult RequestCommentGet()
        {
            var currReqNumber = Request["iReqNumber"];
            var currNIK = Request["iNIK"];
            var getComments = dbBusinessTrip.HC_Business_Trip_Request_Comments.Include("Attachments").Where(w => w.ReqNumber == currReqNumber).ToList();
            foreach (var data in getComments)
            {
                bool checkCurrentUser = (currNIK == data.nik ? true : false);
                data.created_by_current_user = checkCurrentUser;
            }

            return Json(getComments, JsonRequestBehavior.AllowGet);
        }
        public JsonResult RequestCommentAdd()
        {
            var currID = Request["id"];
            var currParent = Request["parent"];
            var currCreated = Request["created"];
            var currModified = Request["modified"];
            var currContent = Request["content"];
            var currAttachments = Request["attachments"];
            var currFullName = Request["fullname"];
            var currProfilePicture = Request["profile_picture_url"];
            var currCurrentUser = Request["created_by_current_user"];
            var currUpvoteCount = Request["upvote_count"];
            var currUserHasUpvoted = Request["user_has_upvoted"];
            var currReqNumber = Request["ReqNumber"];
            var currNIK = Request["nik"];
            var checkData = dbBusinessTrip.HC_Business_Trip_Request_Comments.Where(w => w.id == currID && w.ReqNumber == currReqNumber).OrderByDescending(o => o.id).FirstOrDefault();

            var newData = new HC_Business_Trip_Request_Comments();
            newData.id = checkData != null ? "c" + (int.Parse(checkData.id.Replace("c", "")) + 1) : currID;
            newData.parent = currParent.Length > 0 && currParent != "null" ? currParent : null;
            newData.created = currCreated;
            newData.content = currContent;
            //newData.attachments = currAttachments != null ? currAttachments : "attachments";
            newData.fullname = currFullName;
            newData.created_by_current_user = bool.Parse(currCurrentUser);
            newData.upvoteCount = int.Parse(currUpvoteCount);
            newData.userHasUpvoted = bool.Parse(currUserHasUpvoted);
            newData.ReqNumber = currReqNumber;
            newData.nik = currNIK;
            dbBusinessTrip.HC_Business_Trip_Request_Comments.Add(newData);
            dbBusinessTrip.SaveChanges();

            //uploadQTAttachment(currReqNumber, "", newData.comment_id);
            sendNotification(currReqNumber, "formRequest", "Comment", "", "", 0, 0, currContent);

            return Json(newData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult RequestCommentDelete()
        {
            var currID = Request["id"];
            var currReqNumber = Request["ReqNumber"];
            var currNIK = Request["nik"];
            var deleteData = dbBusinessTrip.HC_Business_Trip_Request_Comments.Where(w => w.id == currID && w.ReqNumber == currReqNumber).FirstOrDefault();
            dbBusinessTrip.HC_Business_Trip_Request_Comments.Remove(deleteData);
            dbBusinessTrip.SaveChanges();

            return Json(JsonConvert.SerializeObject(deleteData), JsonRequestBehavior.AllowGet);
        }
        public ActionResult RequestCommentEdit()
        {
            var currID = Request["id"];
            var currParent = Request["parent"];
            var currCreated = Request["created"];
            var currModified = Request["modified"];
            var currContent = Request["content"];
            var currAttachments = Request["attachments"];
            var currFullName = Request["fullname"];
            var currProfilePicture = Request["profile_picture_url"];
            var currCurrentUser = Request["created_by_current_user"];
            var currReqNumber = Request["ReqNumber"];
            var currNIK = Request["nik"];
            var newData = dbBusinessTrip.HC_Business_Trip_Request_Comments.Include("Attachments").Where(w => w.id == currID && w.ReqNumber == currReqNumber).FirstOrDefault();
            newData.id = currID;
            newData.parent = currParent.Length > 0 && currParent != "null" ? currParent : null;
            newData.created = currCreated;
            newData.modified = long.Parse(currModified);
            newData.content = currContent;
            //newData.attachments = currAttachments != null ? currAttachments : "attachments";
            newData.fullname = currFullName;
            newData.created_by_current_user = bool.Parse(currCurrentUser);
            newData.ReqNumber = currReqNumber;
            newData.nik = currNIK;
            dbBusinessTrip.SaveChanges();
            sendNotification(currReqNumber, "formRequest", "Comment", "", "", 0, 0, currContent);

            return Json(newData, JsonRequestBehavior.AllowGet);
        }
    }

}