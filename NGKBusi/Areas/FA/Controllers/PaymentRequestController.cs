using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.FA.Controllers
{
    public class PaymentRequestController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        GAConnection dbm = new GAConnection();

        String currPeriodFY = "FY1" + (DateTime.Now.Month >= 4 ? (DateTime.Now.Year + 1) % 100 : DateTime.Now.Year % 100).ToString("D2");
        String currPeriodFYWithPO = "FY1" + (DateTime.Now.Month >= 3 ? (DateTime.Now.Year + 1) % 100 : DateTime.Now.Year % 100).ToString("D2");


        // GET: FA/PaymentRequest
        public ActionResult Index()
        {
            return View();
        }
        [Authorize]
        public ActionResult WithPO()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currFilterLevel = Request["iWPOFilterLevel"] != null ? int.Parse(Request["iWPOFilterLevel"].Split('|')[0]) : (int?)null;
            var currFilterLevelSub = Request["iWPOFilterLevel"] != null ? int.Parse(Request["iWPOFilterLevel"].Split('|')[1]) : (int?)null;
            var currPayUsers = db.FA_Payment_Users.Where(w => w.NIK == currUserID).Select(s => s.Section_ID);
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels < 4 && w.Levels_Sub == 0 && w.Menu_Id == 43).ToList();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels < 4 && w.Levels_Sub == 0 && w.Menu_Id == 43).FirstOrDefault();
            if (currFilterLevel != null)
            {
                currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub && w.Menu_Id == 43).FirstOrDefault();
            }
            if (currPayLevel == null)
            {
                return View("UnAuthorized");
            }

            var WPOLevel = currPayLevel.Levels;
            int WPOLevelAdd = 0;
            if (WPOLevel == 1)
            {
                WPOLevelAdd = 1;
                WPOLevel = 0;
            }

            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID && w.Levels == 1).FirstOrDefault();
            ViewBag.PayUser = currPayUser;

            HttpCookie yearCookie = new HttpCookie("WPOFilterYear");
            yearCookie.Value = Request["iWPOFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie statusCookie = new HttpCookie("WPOFilterStatus");
            statusCookie.Value = Request["iWPOFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iWPOFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iWPOFilterStatus"] ?? "Open";

            ViewBag.closingDate = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Year == DateTime.Now.Year && w.Closing_Date.Month == DateTime.Now.Month && w.Type == "With-PO").First();
            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currPayLevel.Levels_Sub;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            //ViewBag.Budget = db.FA_Budget_List.Where(w => currPayUsers.Contains(w.Dept_Code) || w.Dept_Code == "ALL").ToList();
            //ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == currPeriodFY || w.Period_FY == currPeriodFYWithPO) && w.Budget_Type != "BEL").ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == currPeriodFYWithPO) && w.Budget_Type != "BEL").ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();


            if (currStatus == "Open")
            {
                ViewBag.WPO = db.FA_Payment_Request_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub) && w.Approval < WPOLevel + (WPOLevelAdd + 1) && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.WPO = db.FA_Payment_Request_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval > WPOLevel + WPOLevelAdd || (w.Approval >= WPOLevel && w.Approval_Sub > currPayLevel.Levels_Sub)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }
            else
            {
                ViewBag.WPO = db.FA_Payment_Request_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }

            return View();
        }
        [Authorize]
        public ActionResult APWithPO()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 4 && w.Menu_Id == 43).FirstOrDefault(); 

            HttpCookie yearCookie = new HttpCookie("APWPOFilterYear");
            yearCookie.Value = Request["iAPWPOFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie statusCookie = new HttpCookie("APWPOFilterStatus");
            statusCookie.Value = Request["iAPWPOFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iAPWPOFilterYear"] ?? DateTime.Now.Year.ToString();
            var currMonth = Request["iAPWPOFilterMonth"] ?? DateTime.Now.Month.ToString();
            var currStatus = Request["iAPWPOFilterStatus"] ?? "Open";
            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterMonth = currMonth;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevel = currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == currPeriodFY || w.Period_FY == currPeriodFYWithPO) && w.Budget_Type != "BEL").ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            if (currStatus == "Open")
            {
                ViewBag.WPO = db.FA_Payment_Request_PO.Where(w => (w.Approval >= 4 && w.Approval_Sub >= 0) && (w.Approval < 5) && w.Entry_Date.Year.ToString() == currYear && w.Entry_Date.Month.ToString() == currMonth && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.WPO = db.FA_Payment_Request_PO.Where(w => w.Approval >= 5 && w.Entry_Date.Year.ToString() == currYear && w.Entry_Date.Month.ToString() == currMonth).OrderByDescending(o => o.id).ToList();
            }
            else
            {
                ViewBag.WPO = db.FA_Payment_Request_PO.Where(w => w.Approval >= 4 && w.Approval_Sub >= 0 && w.Entry_Date.Year.ToString() == currYear && w.Entry_Date.Month.ToString() == currMonth).OrderByDescending(o => o.id).ToList();
            }
            return View();
        }
        [Authorize]
        public ActionResult TreasuryWithPO()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 5 && w.Menu_Id == 43).FirstOrDefault();

            ViewBag.PayUserLevel = currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == currPeriodFY || w.Period_FY == currPeriodFYWithPO) && w.Budget_Type != "BEL").ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.WPO = db.FA_Payment_Request_PO.Where(w => w.Approval >= 5 && w.Approval_Sub >= 0).OrderByDescending(o => o.id).ToList();
            return View();
        }
        public ActionResult WithPOSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            for (var i = 0; i <= Request.Form.GetValues("iWithPOID[]").Count() - 1; i++)
            {
                int currID = Int32.Parse(Request.Form.GetValues("iWithPOID[]")[i]);

                var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currID).FirstOrDefault();
                updateData.Approval = updateData.Approval + 1;
                ////Check Finance Closing Date
                //var currEntryDate = DateTime.Now;
                //var closingCalendar = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Month == DateTime.Now.Month && w.Closing_Date.Year == DateTime.Now.Year && w.Type == "With-PO").FirstOrDefault();
                //if (closingCalendar != null && DateTime.Now.Day > closingCalendar.Closing_Date.Day)
                //{
                //    currEntryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                //    updateData.Entry_Date = currEntryDate;
                //}
                db.SaveChanges();

                var newHistory = new FA_Payment_Request_History();
                newHistory.Document_Type = "WithPO";
                newHistory.Document_Id = updateData.id;
                newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
                newHistory.User_Id = currUserID;
                newHistory.Date_At = DateTime.Now;
                db.FA_Payment_Request_History.Add(newHistory);
                db.SaveChanges();
            }

            return RedirectToAction("WithPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult WithPOReject()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iRejectID"]);
            var note = Request["iRejectNote"];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Is_Reject = true;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = note;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("WithPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult WithPOReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iWithPOID"]);
            var currReturnReason = Request["iReturnReason"];
            var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currID).FirstOrDefault();
            updateData.Approval = 0;
            updateData.Approval_Sub = 0;
            updateData.WHT_COA_Code = null;
            updateData.WHT_COA_Name = null;
            updateData.WHT_Amount = null;
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = currReturnReason;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("WithPO", "PaymentRequest", new { area = "FA" });
        }

        public ActionResult NonPOReject()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iRejectID"]);
            var note = Request["iRejectNote"];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Is_Reject = true;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = note;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult NonPaymentReject()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iRejectID"]);
            var note = Request["iRejectNote"];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Is_Reject = true;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPayment";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = note;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("NonPayment", "PaymentRequest", new { area = "FA" });
        }
        //public ActionResult APWithPODownloadBank()
        //{
        //    var currUser = (ClaimsIdentity)User.Identity;
        //    var currUserID = currUser.GetUserId().Trim();
        //    var currID = Request["iDownloadBankID"].Split(',').Select(Int32.Parse).ToList();
        //    var culture = CultureInfo.CreateSpecificCulture("en-US");
        //    if (culture.NumberFormat.NumberDecimalSeparator != ".")
        //    {
        //        culture.NumberFormat.NumberDecimalSeparator = ".";
        //        culture.NumberFormat.CurrencyDecimalSeparator = ".";
        //        culture.NumberFormat.PercentDecimalSeparator = ".";
        //        CultureInfo.DefaultThreadCurrentCulture = culture;
        //        CultureInfo.DefaultThreadCurrentUICulture = culture;

        //        Thread.CurrentThread.CurrentCulture = culture;
        //        Thread.CurrentThread.CurrentUICulture = culture;
        //    }


        //    var path = Server.MapPath("~/Files/FA/E-Voucher/AX_Upload/AX_Upload.xlsx");
        //    System.IO.File.Copy(Server.MapPath("~/Files/FA/E-Voucher/Master/AX_Upload.xlsx"), path, true);
        //    string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
        //    using (OleDbConnection conn = new OleDbConnection(ConnectionString))
        //    {
        //        conn.Open();
        //        var queryText = "";
        //        using (OleDbCommand command = conn.CreateCommand())
        //        {
        //            var WithPOData = db.FA_Payment_Request_PO.Where(w => currID.Contains(w.id)).ToList();
        //            double? totalAllocation = 0;
        //            foreach (var data in WithPOData)
        //            {
        //                queryText = string.Format("Insert into [Upload Sheet$] " +
        //                         "(`Date`,`Account Type`,`Account`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`) " +
        //                         "values('" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','Vendor','" + data.Third_Party_Id + "','" + ("Payment Ven_" + data.AP_Number + "_" + data.Invoice_Number.Replace("'", "") + "_" + data.Third_Party_Name) + "'," + (data.Amount_of_Invoice - (data.WHT_Amount ?? (double)0)).ToString().Replace(",", ".") + ",'','" + data.Currency_Code + "','Ledger','GEN');");

        //                command.CommandText = queryText;
        //                command.ExecuteNonQuery();

        //                totalAllocation += data.Amount_of_Invoice - (data.WHT_Amount ?? 0);
        //            }

        //            queryText = string.Format("Insert into [Upload Sheet$] " +
        //                     "(`Date`,`Account Type`,`Account`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`) " +
        //                     "values('" + Convert.ToDateTime(WithPOData.First().Due_Date).ToString("dd/MM/yyyy") + "','Bank','" + WithPOData.First().Bank + "','" + ("Payment Ven_" + WithPOData.First().AP_Number + "_" + WithPOData.First().Invoice_Number.Replace("'", "") + "_" + WithPOData.First().Third_Party_Name) + "','','" + (totalAllocation).ToString().Replace(",", ".") + "','" + WithPOData.First().Currency_Code + "','Ledger','GEN');");

        //            command.CommandText = queryText;
        //            command.ExecuteNonQuery();
        //        }

        //        conn.Close();
        //        conn.Dispose();
        //        Response.ContentType = "application/x-msexcel";
        //        Response.AppendHeader("Content-Disposition", "attachment; filename=AX_Upload.xlsx");
        //        Response.TransmitFile(path);
        //        Response.End();

        //        return RedirectToAction("APWithPO", "PaymentRequest", new { area = "FA" });
        //    }
        //}

        public ActionResult APWithPODownloadBank()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Request["iDownloadBankID"].Split(',').Select(Int32.Parse).ToList();

            var culture = CultureInfo.CreateSpecificCulture("en-US");
            if (culture.NumberFormat.NumberDecimalSeparator != ".")
            {
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.NumberFormat.CurrencyDecimalSeparator = ".";
                culture.NumberFormat.PercentDecimalSeparator = ".";
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }

            try
            {
                var withPOData = db.FA_Payment_Request_PO.Where(w => currID.Contains(w.id)).ToList();
                if (withPOData == null || withPOData.Count == 0)
                {
                    return RedirectToAction("APWithPO", "PaymentRequest", new { area = "FA" });
                }

                double totalAllocation = 0d;
                using (var ms = new System.IO.MemoryStream())
                {
                    // ClosedXML writes real .xlsx without the 255-char OLEDB limit
                    using (var wb = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Upload Sheet");

                        // Header row (optional, keeps same columns used by OLEDB inserts)
                        var headers = new[] { "Journal Batch Number", "Date", "Account Type", "Account", "Account Name", "SEC", "SEC Name", "PROCATE", "PROCATE NAME", "BUSTYPE", "BUSTYPE NAME", "BOI", "BOI NAME", "Description", "Sales Tax group", "Item Sales Tax group", "Sales tax code", "Debit", "Credit", "Currency", "Offset Account Type", "Offset Account", "Offset Account Name", "Offset SEC", "Offset SEC Name", "Offset PROCATE", "Offset PROCATE NAME", "Offset BUSTYPE", "Offset BUSTYPE NAME", "Offset BOI", "Offset BOI NAME", "Posting Profie", "Document Number", "Document date", "Invoice number", "Invoice date", "Invoice receipt date","Tax Invoice number", "Tax Invoice date", "Tax Invoice receipt date", "Tax registration number", "Due Date" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            ws.Cell(1, i + 1).Value = headers[i];
                            ws.Cell(1, i + 1).Style.Font.Bold = true;
                        }

                        int row = 2;
                        foreach (var data in withPOData)
                        {
                            // Ensure nullable doubles are converted safely to non-nullable doubles
                            double amount = data.Amount_of_Invoice ?? 0d;
                            double wht = data.WHT_Amount ?? 0d;
                            double debitAmount = amount - wht;

                            ws.Cell(row, 2).Value = Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy");
                            ws.Cell(row, 3).Value = "Vendor";
                            ws.Cell(row, 4).Value = data.Third_Party_Id ?? "";
                            // ClosedXML supports long text; don't truncate unless business requires it
                            ws.Cell(row, 14).Value = string.Format("Payment Ven_{0}_{1}_{2}", data.AP_Number ?? "", (data.Invoice_Number ?? "").Replace("'", ""), data.Third_Party_Name ?? "");
                            ws.Cell(row, 18).Value = debitAmount;
                            ws.Cell(row, 19).Value = ""; // credit
                            ws.Cell(row, 20).Value = data.Currency_Code ?? "";
                            ws.Cell(row, 21).Value = "Ledger";
                            ws.Cell(row, 32).Value = "GEN";

                            // set numeric format for Debit
                            ws.Cell(row, 18).Style.NumberFormat.Format = "#,##0.00";
                            totalAllocation += debitAmount;
                            row++;
                        }

                        // bank line (credit)
                        var first = withPOData.First();
                        ws.Cell(row, 2).Value = Convert.ToDateTime(first.Due_Date).ToString("dd/MM/yyyy");
                        ws.Cell(row, 3).Value = "Bank";
                        ws.Cell(row, 4).Value = first.Bank ?? "";
                        ws.Cell(row, 14).Value = string.Format("Payment Ven_{0}_{1}_{2}", first.AP_Number ?? "", (first.Invoice_Number ?? "").Replace("'", ""), first.Third_Party_Name ?? "");
                        ws.Cell(row, 18).Value = ""; // debit
                        ws.Cell(row, 19).Value = totalAllocation;
                        ws.Cell(row, 20).Value = first.Currency_Code ?? "";
                        ws.Cell(row, 21).Value = "Ledger";
                        ws.Cell(row, 32).Value = "GEN";
                        ws.Cell(row, 19).Style.NumberFormat.Format = "#,##0.00";

                        // Auto-fit columns
                        ws.Columns().AdjustToContents();

                        wb.SaveAs(ms);
                    }

                    ms.Position = 0;
                    var content = ms.ToArray();
                    // stream file to client
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AX_Upload.xlsx");
                }
            }
            catch (Exception ex)
            {
                // keep original behavior: redirect back with error (or log)
                TempData["errorMessage"] = "Export failed: " + ex.Message;
                return RedirectToAction("APWithPO", "PaymentRequest", new { area = "FA" });
            }
        }
        public ActionResult APNonPODownloadBank()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Request["iDownloadBankID"].Split(',').Select(Int32.Parse).ToList();
            var culture = CultureInfo.CreateSpecificCulture("en-US");
            if (culture.NumberFormat.NumberDecimalSeparator != ".")
            {
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.NumberFormat.CurrencyDecimalSeparator = ".";
                culture.NumberFormat.PercentDecimalSeparator = ".";
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }


            var path = Server.MapPath("~/Files/FA/E-Voucher/AX_Upload/AX_Upload.xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/E-Voucher/Master/AX_Upload.xlsx"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {
                    var NonPOData = db.FA_Payment_Request_Non_PO.Where(w => currID.Contains(w.id)).ToList();
                    double? totalAllocation = 0;
                    foreach (var data in NonPOData)
                    {
                        queryText = string.Format("Insert into [Upload Sheet$] " +
                                 "(`Date`,`Account Type`,`Account`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`) " +
                                 "values('" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','Vendor','" + data.Third_Party_Id + "','" + ("Payment Ven_" + data.AP_Number + "_" + data.Invoice_Number.Replace("'", "") + "_" + data.Third_Party_Name) + "'," + (data.Amount_of_Invoice - (data.WHT_Amount ?? (double)0)).ToString().Replace(",", ".") + ",'','" + data.Currency_Code + "','Ledger','GEN');");

                        command.CommandText = queryText;
                        command.ExecuteNonQuery();

                        totalAllocation += data.Amount_of_Invoice - (data.WHT_Amount ?? 0);
                    }

                    queryText = string.Format("Insert into [Upload Sheet$] " +
                             "(`Date`,`Account Type`,`Account`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`) " +
                             "values('" + Convert.ToDateTime(NonPOData.First().Due_Date).ToString("dd/MM/yyyy") + "','Bank','" + NonPOData.First().Bank + "','" + ("Payment Ven_" + NonPOData.First().AP_Number + "_" + NonPOData.First().Invoice_Number.Replace("'", "") + "_" + NonPOData.First().Third_Party_Name) + "','','" + (totalAllocation).ToString().Replace(",", ".") + "','" + NonPOData.First().Currency_Code + "','Ledger','GEN');");

                    command.CommandText = queryText;
                    command.ExecuteNonQuery();
                }

                conn.Close();
                conn.Dispose();
                Response.ContentType = "application/x-msexcel";
                Response.AppendHeader("Content-Disposition", "attachment; filename=AX_Upload.xlsx");
                Response.TransmitFile(path);
                Response.End();

                return RedirectToAction("APNonPO", "PaymentRequest", new { area = "FA" });
            }
        }
        public ActionResult NonPODownloadAP()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Request["iDownloadID"].Split(',').Select(Int32.Parse).ToList();
            var culture = CultureInfo.CreateSpecificCulture("en-US");
            if (culture.NumberFormat.NumberDecimalSeparator != ".")
            {
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.NumberFormat.CurrencyDecimalSeparator = ".";
                culture.NumberFormat.PercentDecimalSeparator = ".";
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }

            var path = Server.MapPath("~/Files/FA/E-Voucher/AX_Upload/AX_Upload.xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/E-Voucher/Master/AX_Upload.xlsx"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {
                    var NonPOData = db.FA_Payment_Request_Non_PO.Where(w => currID.Contains(w.id)).ToList();
                    foreach (var data in NonPOData)
                    {
                        double totalAllocation = 0;
                        var NonPODataSub = db.FA_Payment_Request_Non_PO_Sub.Where(w => w.Non_PO_ID == data.id).ToList();
                        foreach (var dataSub in NonPODataSub)
                        {
                            var budgetNumberInDescription = new[] { "7", "8" }.Contains(dataSub.COA_Code.Substring(0, 1)) && !string.IsNullOrEmpty(dataSub.Budget_Number) ? "_" + dataSub.Budget_Number : "";
                            queryText = string.Format("Insert into [Upload Sheet$] " +
                                "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                                "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','Ledger','" + dataSub.COA_Code + "','" + dataSub.Procate_Code + "','" + dataSub.Section_To_Code + "','" + (dataSub.COA_Code == "1851104" ? "PPN for Invoice " + data.Invoice_Number : (dataSub.Description.Replace("'", "-").TrimEnd()) + budgetNumberInDescription) + "'," + ((double)dataSub.Allocation_Amount).ToString().Replace(",", ".") + ",'','" + data.Currency_Code + "','Ledger','','" + data.Invoice_Number + "','" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','" + dataSub.Budget_Number + "','','', '','" + (dataSub.COA_Code == "1851104" ? "VAT" + dataSub.VAT : "") + "');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();

                            totalAllocation += dataSub.Allocation_Amount;
                        }

                        if (data.VAT > 0)
                        {
                            queryText = string.Format("Insert into [Upload Sheet$] " +
                                "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                                "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','Ledger','1851104','S76010','B2300','" + "PPN for Invoice " + data.Invoice_Number + "'," + ((double)((totalAllocation / 100) * data.VAT)).ToString().Replace(",", ".") + ",'','" + data.Currency_Code + "','Ledger','','" + data.Invoice_Number + "','" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','" + data.Tax_Number + data.Tax_Number_17 + "','" + Convert.ToDateTime(data.Tax_Date).ToString("dd/MM/yyyy") + "','', '','" + "VAT" + data.VAT + "');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }

                        if (data.Payment_Type == "Pre-Payment")
                        {
                            queryText = string.Format("Insert into [Upload Sheet$] " +
                                   "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                                   "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','Ledger','1849101','S76010','','" + data.Description.Replace("'", "-") + "'," + ((double)data.Amount_of_Invoice).ToString().Replace(",", ".") + ",'','" + data.Currency_Code + "','Ledger','','" + data.Invoice_Number + "','" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','" + data.Tax_Number + data.Tax_Number_17 + "','" + (data.VAT > 0 ? Convert.ToDateTime(data.Tax_Date).ToString("dd/MM/yyyy") : "") + "','','','');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }

                        if (data.Payment_Type == "Settlement" && data.Amount_of_Invoice != totalAllocation)
                        {
                            queryText = string.Format("Insert into [Upload Sheet$] " +
                                   "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                                   "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','" + (data.Amount_of_Invoice > totalAllocation ? "Ledger" : "Vendor") + "','" + (data.Amount_of_Invoice > totalAllocation ? "1849101" : data.Third_Party_Id) + "','" + (data.Amount_of_Invoice > totalAllocation ? "S76010" : "") + "','" + (data.Amount_of_Invoice > totalAllocation ? data.Section_From_Code : "") + "','" + (data.Amount_of_Invoice > totalAllocation ? data.Description.Replace("'", "-") : "AP Other") + "','" + (data.Amount_of_Invoice > totalAllocation ? (data.Amount_of_Invoice - totalAllocation).ToString() : "").ToString().Replace(",", ".") + "','" + (data.Amount_of_Invoice < totalAllocation ? ((totalAllocation - data.Amount_of_Invoice) - (data.WHT_Amount != null ? data.WHT_Amount : 0)).ToString() : "").ToString().Replace(",", ".") + "','" + data.Currency_Code + "','Ledger','" + (data.Amount_of_Invoice > totalAllocation ? "" : "GEN") + "','" + data.Invoice_Number + "','" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','" + data.Tax_Number + data.Tax_Number_17 + "','" + (data.VAT > 0 ? Convert.ToDateTime(data.Tax_Date).ToString("dd/MM/yyyy") : "") + "','','','');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }

                        if (data.WHT_Amount > 0)
                        {
                            queryText = string.Format("Insert into [Upload Sheet$] " +
                                   "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                                   "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','Ledger','" + data.WHT_COA_Code + "','S76010','','" + data.WHT_COA_Name + " of Invoice : " + data.Invoice_Number + "',''," + ((double)data.WHT_Amount).ToString().Replace(",", ".") + ",'" + data.Currency_Code + "','Ledger','','" + data.Invoice_Number + "','" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','','','','','');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }

                        double lastAmount = 0;
                        if (data.Payment_Type == "Pre-Payment")
                        {
                            lastAmount = (double)((data.Amount_of_Invoice + (double)(((double)data.Amount_of_Invoice / 100) * data.VAT)) - (data.WHT_Amount ?? 0));
                        }
                        else
                        {
                            if (data.Payment_Type == "Settlement" && data.Amount_of_Invoice < totalAllocation)
                            {
                                lastAmount = (double)(data.Amount_of_Invoice);
                            }
                            else
                            {
                                lastAmount = (double)(data.Amount_of_Invoice - (data.WHT_Amount ?? 0));
                            }
                        }

                        queryText = string.Format("Insert into [Upload Sheet$] " +
                               "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                               "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','" + (data.Payment_Type == "Settlement" ? "Ledger" : "Vendor") + "','" + (data.Payment_Type == "Settlement" ? "1849101" : data.Third_Party_Id) + "','" + (data.Payment_Type == "Settlement" ? "S76010" : "") + "','" + (data.Payment_Type == "Settlement" ? data.Section_From_Code : "") + "','" + data.Description.Replace("'", "-") + "',''," + lastAmount.ToString().Replace(",", ".") + ",'" + data.Currency_Code + "','Ledger','" + (data.Payment_Type == "Settlement" ? "" : "GEN") + "','" + data.Invoice_Number + "','" + Convert.ToDateTime(data.Due_Date).ToString("dd/MM/yyyy") + "','','','','','');");

                        command.CommandText = queryText;
                        command.ExecuteNonQuery();
                    }
                }
            }

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=AX_Upload.xlsx");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("APNonPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult NonPaymentDownloadFA()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Request["iDownloadID"].Split(',').Select(Int32.Parse).ToList();

            var path = Server.MapPath("~/Files/FA/E-Voucher/AX_Upload/AX_Upload_Non_Payment.xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/FA/E-Voucher/Master/AX_Upload_Non_Payment.xlsx"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0; HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {
                    var NonPOData = db.FA_Payment_Request_Non_Payment.Where(w => currID.Contains(w.id)).ToList();
                    foreach (var data in NonPOData)
                    {
                        double totalAllocation = 0;
                        var NonPODataSub = db.FA_Payment_Request_Non_Payment_Sub.Where(w => w.Non_PO_ID == data.id).ToList();
                        var NonPODataSubCredit = db.FA_Payment_Request_Non_Payment_Sub.Where(w => w.Non_PO_ID == data.id && w.Credit_Amount > 0).ToList();
                        foreach (var dataSub in NonPODataSub)
                        {
                            queryText = string.Format("Insert into [Upload Sheet$] " +
                                "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                                "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','Ledger','" + dataSub.COA_Code + "','" + dataSub.Procate_Code + "','" + dataSub.Section_To_Code + "','" + dataSub.Description.Replace("'", "-") + "'," + dataSub.Allocation_Amount + "," + dataSub.Credit_Amount + ",'" + data.Currency_Code + "','Ledger','','','','" + dataSub.Budget_Number + "','','', '','');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();

                            totalAllocation += dataSub.Allocation_Amount;
                        }

                        if (NonPODataSubCredit.Count() == 0)
                        {
                            queryText = string.Format("Insert into [Upload Sheet$] " +
                                   "(`Date`,`Account Type`,`Account`,`PROCATE`,`SEC`,`Description`,`Debit`,`Credit`,`Currency`,`Offset Account Type`,`Posting Profie`,`Invoice Number`,`Due Date`,`Tax Invoice Number`,`Document Date`,`Sales Tax Group`,`Item Sales Tax Group`,`Sales Tax Code`) " +
                                   "values('" + Convert.ToDateTime(data.Entry_Date).ToString("dd/MM/yyyy") + "','" + (data.Type == "Auto-Debit" ? "Bank" : "Ledger") + "','" + (data.Type == "Auto-Debit" ? data.Bank : data.COA_Code) + "','','','" + data.Description.Replace("'", "-") + "',''," + data.Amount_of_Invoice + ",'" + data.Currency_Code + "','Ledger','GEN','','','','','','','');");

                            command.CommandText = queryText;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=AX_Upload_Non_Payment.xlsx");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("FANonPayment", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult HCApprovalReject()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iRejectID"]);
            var note = Request["iRejectNote"];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Is_Reject = true;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = note;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("HCApproval", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult FANonPaymentPrint()
        {
            var currID = Request["iNonPOID"].Split(',').Select(Int32.Parse).ToList();
            ViewBag.NonPOList = db.FA_Payment_Request_Non_Payment.Where(w => currID.Contains(w.id)).ToList();

            return PartialView("FANonPaymentPrint");
        }
        public ActionResult APNonPOPrint()
        {
            var currID = Request["iNonPOID"].Split(',').Select(Int32.Parse).ToList();
            ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => currID.Contains(w.id)).ToList();

            return PartialView("APNonPOPrint");
        }
        public ActionResult APWithPOPrint()
        {
            var currID = Request["iWithPOID"].Split(',').Select(Int32.Parse).ToList();
            ViewBag.WithPOList = db.FA_Payment_Request_PO.Where(w => currID.Contains(w.id)).ToList();

            return PartialView("APWithPOPrint");
        }

        public ActionResult APWithPOBank()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iWithPOID"]);
            string currBank = Request["iBank"];
            var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currID).FirstOrDefault();
            updateData.Bank = currBank;

            db.SaveChanges();

            return RedirectToAction("APWithPO", "PaymentRequest", new { area = "FA" });
        }

        public ActionResult APNonPOBank()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            string currBank = Request["iBank"];
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            updateData.Bank = currBank;

            db.SaveChanges();

            return RedirectToAction("APNonPO", "PaymentRequest", new { area = "FA" });
        }

        public ActionResult NonPOSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();

            for (var i = 0; i <= Request.Form.GetValues("iNonPOID[]").Count() - 1; i++)
            {
                int currID = Int32.Parse(Request.Form.GetValues("iNonPOID[]")[i]);

                string[] HRDCOAList = { "7515401", "7515402", "7515403", "7515404", "7515405", "7515499", "7651101", "7651102", "7651103" };
                string[] HRDSectionList = { "B2200", "B2210", "B2220" };
                var checkCOAHRD = db.FA_Payment_Request_Non_PO_Sub.Where(w => w.Non_PO_ID == currID && HRDCOAList.Contains(w.COA_Code)).ToList();

                var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();

                //Check Finance Closing Date
                var currEntryDate = DateTime.Now;
                var closingCalendar = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Month == DateTime.Now.Month && w.Closing_Date.Year == DateTime.Now.Year && w.Type == "Non-PO").FirstOrDefault();
                if (closingCalendar != null && DateTime.Now.Day > closingCalendar.Closing_Date.Day)
                {
                    currEntryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                    updateData.Entry_Date = currEntryDate;
                }

                //cek Coa HRD jika ada, perlu approval HRD
                if (checkCOAHRD.Count() > 0 && updateData.Approval == 3)
                {
                    //Jika yg input orang HRD
                    if (HRDSectionList.Contains(currPayUser.Section_ID) && updateData.Approval_Sub <= 3)
                    {
                        //Langsung Direct ke FA
                        updateData.Approval = 4;
                        updateData.Approval_Sub = 0;
                    }
                    else
                    {
                        if (updateData.Amount_of_Invoice <= 1000000 && updateData.Payment_Method != "Cash")
                        {
                            //langsung direct ke HRD
                            updateData.Approval_Sub = 2;
                        }
                        else
                        {
                            if (updateData.Payment_Method == "Cash")
                            {
                                //Melalui Approval 3(Director)
                                //updateData.Approval_Sub = updateData.Approval_Sub + 1;
                                updateData.Approval_Sub = 2;
                            }
                            else
                            {
                                //langsung direct ke HRD
                                updateData.Approval_Sub = 2;
                            }
                        }
                    }
                }
                else
                {
                    if (updateData.Amount_of_Invoice <= 1000000)
                    {
                        //Langsung Direct ke FA
                        updateData.Approval = updateData.Approval + 1;
                    }
                    else
                    {
                        if (updateData.Approval != 3)
                        {
                            //Dept Approval
                            updateData.Approval = updateData.Approval + 1;
                            if (updateData.Approval == 0)
                            {
                                //dari return ke checker
                                updateData.Approval = 2;
                            }
                        }
                        else
                        {
                            //Jika Cash dan lebih dari 1jt..harus melalui director
                            if (updateData.Payment_Method == "Cash")
                            {
                                if (updateData.Approval_Sub == 2)
                                {
                                    //Sign dari Director ke FA
                                    updateData.Approval = 4;
                                    updateData.Approval_Sub = 0;
                                }
                                else
                                {
                                    //Sign dari Dept Melalui Approval 3(Director)
                                    //updateData.Approval_Sub = updateData.Approval_Sub + 1;
                                    updateData.Approval_Sub = 2;
                                }
                            }
                            else
                            {
                                //Langsung Direct ke FA
                                updateData.Approval = updateData.Approval + 1;
                            }
                        }
                    }
                }

                var newHistory = new FA_Payment_Request_History();
                newHistory.Document_Type = "NonPO";
                newHistory.Document_Id = updateData.id;
                newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
                newHistory.User_Id = currUserID;
                newHistory.Date_At = DateTime.Now;
                db.FA_Payment_Request_History.Add(newHistory);
                db.SaveChanges();
            }

            return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult NonPaymentSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();

            for (var i = 0; i <= Request.Form.GetValues("iNonPOID[]").Count() - 1; i++)
            {
                int currID = Int32.Parse(Request.Form.GetValues("iNonPOID[]")[i]);

                string[] HRDCOAList = { "7515401", "7515402", "7515403", "7515404", "7515405", "7515499", "7651101", "7651102", "7651103" };
                string[] HRDSectionList = { "B2200", "B2210", "B2220" };
                var checkCOAHRD = db.FA_Payment_Request_Non_Payment_Sub.Where(w => w.Non_PO_ID == currID && HRDCOAList.Contains(w.COA_Code)).ToList();

                var updateData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).FirstOrDefault();

                //Check Finance Closing Date
                var currEntryDate = DateTime.Now;
                var closingCalendar = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Month == DateTime.Now.Month && w.Closing_Date.Year == DateTime.Now.Year && w.Type == "Non-PO").FirstOrDefault();
                if (closingCalendar != null && DateTime.Now.Day > closingCalendar.Closing_Date.Day)
                {
                    currEntryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                    updateData.Entry_Date = currEntryDate;
                }

                //cek Coa HRD jika ada, perlu approval HRD
                if (checkCOAHRD.Count() > 0 && updateData.Approval == 3)
                {
                    //Jika yg input orang HRD
                    if (HRDSectionList.Contains(currPayUser.Section_ID) && updateData.Approval_Sub <= 3)
                    {
                        //Langsung Direct ke FA
                        updateData.Approval = 4;
                        updateData.Approval_Sub = 0;
                    }
                    else
                    {
                        //langsung direct ke HRD
                        updateData.Approval_Sub = 2;
                    }
                }
                else
                {
                    if (updateData.Amount_of_Invoice <= 1000000)
                    {
                        //Langsung Direct ke FA
                        updateData.Approval = updateData.Approval + 1;
                    }
                    else
                    {
                        if (updateData.Approval != 3)
                        {
                            //Dept Approval
                            updateData.Approval = updateData.Approval + 1;
                            if (updateData.Approval == 0)
                            {
                                //dari return ke checker
                                updateData.Approval = 2;
                            }
                        }
                        else
                        {
                            //Langsung Direct ke FA
                            updateData.Approval = updateData.Approval + 1;
                        }
                    }
                }

                var newHistory = new FA_Payment_Request_History();
                newHistory.Document_Type = "NonPayment";
                newHistory.Document_Id = updateData.id;
                newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
                newHistory.User_Id = currUserID;
                newHistory.Date_At = DateTime.Now;
                db.FA_Payment_Request_History.Add(newHistory);
                db.SaveChanges();
            }

            return RedirectToAction("NonPayment", "PaymentRequest", new { area = "FA" });
        }

        public ActionResult NonPOReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currReturnReason = Request["iReturnReason"];
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            updateData.Approval = 0;
            updateData.Approval_Sub = 0;
            updateData.WHT_COA_Code = null;
            updateData.WHT_COA_Name = null;
            updateData.WHT_Amount = null;
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = currReturnReason;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult NonPaymentReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currReturnReason = Request["iReturnReason"];
            var updateData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).FirstOrDefault();
            updateData.Approval = 0;
            updateData.Approval_Sub = 0;
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPayment";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = currReturnReason;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("NonPayment", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult HCApprovalReturn()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currReturnReason = Request["iReturnReason"];
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            updateData.Approval = 0;
            updateData.Approval_Sub = 0;
            updateData.WHT_COA_Code = null;
            updateData.WHT_COA_Name = null;
            updateData.WHT_Amount = null;
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.Note = currReturnReason;
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("HCApproval", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult APNonPOSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currAPNumber = Request["iAPNumber"];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 5;
                updateData.Approval_Sub = 0;
                updateData.AP_Number = currAPNumber;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("APNonPO", "PaymentRequest", new { area = "FA" });
        }

        public ActionResult APNonPaymentSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currAPNumber = Request["iAPNumber"];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 5;
                updateData.Approval_Sub = 0;
                updateData.AP_Number = currAPNumber;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPayment";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("FANonPayment", "PaymentRequest", new { area = "FA" });
        }

        public JsonResult HCApprovalSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                //Check Finance Closing Date
                var currEntryDate = DateTime.Now;
                var closingCalendar = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Month == DateTime.Now.Month && w.Closing_Date.Year == DateTime.Now.Year && w.Type == "Non-PO").FirstOrDefault();
                if (closingCalendar != null && DateTime.Now.Day > closingCalendar.Closing_Date.Day)
                {
                    currEntryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                    updateData.Entry_Date = currEntryDate;
                }

                if (updateData.Approval_Sub == 2)
                {
                    //HC Checker Sign
                    updateData.Approval_Sub = updateData.Approval_Sub + 1;
                }
                else
                {
                    //HC Approver Sign
                    updateData.Approval = 4;
                    updateData.Approval_Sub = 0;
                }
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "HCApproval";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public ActionResult TreasuryNonPOCreatePayment()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Request["iPaymentID"].Split(',').Select(Int32.Parse).ToList();
            var bankAccount = Request["iBank"].Split('|')[0];
            var bankName = Request["iBank"].Split('|')[1];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => currID.Contains(w.id)).ToList();
            var newData = new FA_Payment_Request_Treasury();

            newData.Receive_Number = this.getSequence("P");
            newData.Type = "NonPO";
            newData.Payment_Date = Convert.ToDateTime(Request["iPaymentDate"]);
            newData.Third_Party_Id = updateData.First().Third_Party_Id;
            newData.Third_Party_Name = updateData.First().Third_Party_Name;
            newData.Currency_Code = updateData.First().Currency_Code;
            newData.Bank_Account = bankAccount;
            newData.Bank_Name = bankName;
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            newData.Is_Complete = false;
            db.FA_Payment_Request_Treasury.Add(newData);
            db.SaveChanges();

            foreach (var upData in updateData)
            {
                upData.Approval = 6;
                upData.Approval_Sub = 0;
                upData.Payment_ID = newData.id;

                var newHistory = new FA_Payment_Request_History();
                newHistory.Document_Type = "NonPO";
                newHistory.Document_Id = upData.id;
                newHistory.Message = ApprovalStatus(upData.Approval, upData.Approval_Sub, upData.Is_Reject, upData.Amount_of_Invoice);
                newHistory.User_Id = currUserID;
                newHistory.Date_At = DateTime.Now;
                db.FA_Payment_Request_History.Add(newHistory);
                db.SaveChanges();
            }

            return RedirectToAction("TreasuryNonPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult TreasuryNonPOAddToPayment()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Request["iPaymentID"].Split(',').Select(Int32.Parse).ToList();
            var bankAccount = Request["iBank"].Split('|')[0];
            var bankName = Request["iBank"].Split('|')[1];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => currID.Contains(w.id)).ToList();
            var newData = new FA_Payment_Request_Treasury();

            newData.Receive_Number = this.getSequence("P");
            newData.Type = "NonPO";
            newData.Payment_Date = Convert.ToDateTime(Request["iPaymentDate"]);
            newData.Third_Party_Id = updateData.First().Third_Party_Id;
            newData.Third_Party_Name = updateData.First().Third_Party_Name;
            newData.Currency_Code = updateData.First().Currency_Code;
            newData.Bank_Account = bankAccount;
            newData.Bank_Name = bankName;
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            newData.Is_Complete = false;
            db.FA_Payment_Request_Treasury.Add(newData);
            db.SaveChanges();

            foreach (var upData in updateData)
            {
                upData.Approval = 6;
                upData.Approval_Sub = 0;
                upData.Payment_ID = newData.id;

                var newHistory = new FA_Payment_Request_History();
                newHistory.Document_Type = "NonPO";
                newHistory.Document_Id = upData.id;
                newHistory.Message = ApprovalStatus(upData.Approval, upData.Approval_Sub, upData.Is_Reject, upData.Amount_of_Invoice);
                newHistory.User_Id = currUserID;
                newHistory.Date_At = DateTime.Now;
                db.FA_Payment_Request_History.Add(newHistory);
                db.SaveChanges();
            }

            return RedirectToAction("TreasuryNonPO", "PaymentRequest", new { area = "FA" });
        }

        public JsonResult NonPOEditData()
        {
            try
            {
                int currID = Int32.Parse(Request["iNonPOID"]);
            var NonPOData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).Select(s => new
            {
                Entry_Date = s.Entry_Date,
                Receive_Number = s.Receive_Number,
                Settlement_For = s.Settlement_For,
                Section_From_Code = s.Section_From_Code,
                Section_From_Name = s.Section_From_Name,
                Invoice_Number = s.Invoice_Number,
                Currency_Code = s.Currency_Code,
                Third_Party_Id = s.Third_Party_Id,
                Third_Party_Name = s.Third_Party_Name,
                Amount_of_Invoice = s.Amount_of_Invoice,
                Description = s.Description,
                Due_Date = s.Due_Date,
                VAT = s.VAT,
                Tax_Date = s.Tax_Date,
                Tax_Number = s.Tax_Number,
                Tax_Number_17 = s.Tax_Number_17,
                Payment_Method = s.Payment_Method,
                Payment_Type = s.Payment_Type,
                Is_Working_Order = s.Is_Working_Order,
                Non_PO_Sub = db.FA_Payment_Request_Non_PO_Sub.Where(w => w.Non_PO_ID == s.id).ToList()
            });

            return Json(NonPOData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Set status code ke 500 agar masuk ke callback 'error' di AJAX
                Response.StatusCode = 500;
                return Json(new { message = ex.Message, detail = ex.InnerException?.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult WithPOEditData()
        {
            int currID = Int32.Parse(Request["iWithPOID"]);
            var WithPOData = db.FA_Payment_Request_PO.Where(w => w.id == currID).Select(s => new
            {
                Entry_Date = s.Entry_Date,
                Receive_Number = s.Receive_Number,
                Section_From_Code = s.Section_From_Code,
                Section_From_Name = s.Section_From_Name,
                Invoice_Number = s.Invoice_Number,
                Currency_Code = s.Currency_Code,
                Third_Party_Id = s.Third_Party_Id,
                Third_Party_Name = s.Third_Party_Name,
                Amount_of_Invoice = s.Amount_of_Invoice,
                Description = s.Description,
                Due_Date = s.Due_Date,
                VAT = s.VAT,
                Tax_Date = s.Tax_Date,
                Tax_Number = s.Tax_Number,
                Tax_Number_17 = s.Tax_Number_17,
                PO_Sub = db.FA_Payment_Request_PO_Sub.Where(w => w.With_PO_ID == s.id).ToList()
            });

            return Json(WithPOData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult NonPaymentGetData()
        {
            string[] currReceiveNumber = Request["iNonPaymentID"].Split(',');
            var getID = db.FA_Payment_Request_Non_Payment.Where(w => currReceiveNumber.Contains(w.Receive_Number.ToString())).Select(s => s.id);
            var totalAmount = db.FA_Payment_Request_Non_Payment.Where(w => currReceiveNumber.Contains(w.Receive_Number.ToString())).Sum(s => s.Amount_of_Invoice);
            var NonPOData = db.FA_Payment_Request_Non_Payment.Where(w => currReceiveNumber.Contains(w.Receive_Number.ToString())).Select(s => new
            {
                Entry_Date = s.Entry_Date,
                Receive_Number = s.Receive_Number,
                Section_From_Code = s.Section_From_Code,
                Section_From_Name = s.Section_From_Name,
                Currency_Code = s.Currency_Code,
                Third_Party_Id = s.Third_Party_Id,
                Third_Party_Name = s.Third_Party_Name,
                Amount_of_Invoice = totalAmount,
                Description = s.Description,
                Coa_Code = s.COA_Code,
                Coa_Name = s.COA_Name,
                Type = s.Type,
                Bank = s.Bank,
                Non_PO_Sub = db.FA_Payment_Request_Non_Payment_Sub.Where(w => getID.Contains(w.Non_PO_ID)).ToList()
            });

            return Json(NonPOData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult NonPaymentEditData()
        {
            int currID = Int32.Parse(Request["iNonPOID"]);
            var NonPOData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).Select(s => new
            {
                Entry_Date = s.Entry_Date,
                Receive_Number = s.Receive_Number,
                Settlement_For = s.Settlement_For,
                Section_From_Code = s.Section_From_Code,
                Section_From_Name = s.Section_From_Name,
                Currency_Code = s.Currency_Code,
                Third_Party_Id = s.Third_Party_Id,
                Third_Party_Name = s.Third_Party_Name,
                Amount_of_Invoice = s.Amount_of_Invoice,
                Description = s.Description,
                Coa_Code = s.COA_Code,
                Coa_Name = s.COA_Name,
                Type = s.Type,
                Bank = s.Bank,
                Non_PO_Sub = db.FA_Payment_Request_Non_Payment_Sub.Where(w => w.Non_PO_ID == s.id).ToList()
            });

            return Json(NonPOData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NonPaymentUpload()
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/E-Voucher"), fname);
                        file.SaveAs(fname);
                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";

                        using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                        {
                            conn.Open();
                            using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                            {
                                string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                //string sheetName = _sheetName;
                                //string query = "SELECT [Item No.],[U Code],[P/F],[Order No.],[Delivery Date],[Quantity] FROM [" + sheetName + "]";
                                string query = "SELECT * FROM [" + "Template$" + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        var newData = new List<ArrayList>();
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            if (i > 0)
                                            {
                                                ArrayList dataArray = new ArrayList();
                                                dataArray.Add(ds.Tables[0].Rows[i][0].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][1].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][2].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][3].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][4].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][5].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][6].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][7].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][8].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][9].ToString().Replace(".", "").Replace(",", "."));
                                                dataArray.Add(ds.Tables[0].Rows[i][10].ToString().Replace(".", "").Replace(",", "."));

                                                newData.Add(dataArray);
                                            }
                                        }
                                        return Json(newData, "application/json", JsonRequestBehavior.AllowGet);
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

        public JsonResult APNonPOReceive()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 4;
                updateData.Approval_Sub = 2;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult APNonPaymentReceive()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 4;
                updateData.Approval_Sub = 2;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPayment";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult TNonPOReceive()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iNonPOID"]);
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 5;
                updateData.Approval_Sub = 1;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public ActionResult APWithPOSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iWithPOID"]);
            var currAPNumber = Request["iAPNumber"];
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 5;
                updateData.Approval_Sub = 0;
                updateData.AP_Number = currAPNumber;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("APWithPO", "PaymentRequest", new { area = "FA" });
        }
        public JsonResult TreasuryWithPOSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iWithPOID"]);
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 6;
                updateData.Approval_Sub = 0;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult APWithPOReceive()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            int currID = Int32.Parse(Request["iWithPOID"]);
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData != null)
            {
                updateData.Approval = 4;
                updateData.Approval_Sub = 2;
                db.SaveChanges();
            }

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = ApprovalStatus(updateData.Approval, updateData.Approval_Sub, updateData.Is_Reject, updateData.Amount_of_Invoice);
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult WithPODelete()
        {
            var currID = Int32.Parse(Request["iWithPOID"]);
            var deleteData = db.FA_Payment_Request_PO.Where(w => w.id == currID).First();
            db.FA_Payment_Request_PO.Remove(deleteData);
            var deleteDataSub = db.FA_Payment_Request_PO_Sub.Where(w => w.With_PO_ID == currID).ToList();
            db.FA_Payment_Request_PO_Sub.RemoveRange(deleteDataSub);
            db.SaveChanges();
            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public ActionResult WithPOAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currSeq = this.getSequence("W");

            var currEntryDate = DateTime.Now;
            var closingCalendar = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Month == DateTime.Now.Month && w.Closing_Date.Year == DateTime.Now.Year && w.Type == "With-PO").FirstOrDefault();
            //check Finance Closing Calendar
            if (closingCalendar != null && DateTime.Now.Day > closingCalendar.Closing_Date.Day)
            {
                currEntryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            }

            var newData = new FA_Payment_Request_PO();
            newData.Receive_Number = currSeq;
            newData.Entry_Date = currEntryDate;
            newData.Section_From_Code = Request["iSectionFrom"].Split('|')[0];
            newData.Section_From_Name = Request["iSectionFrom"].Split('|')[1];
            newData.Invoice_Number = Request["iInvoiceNumber"];
            newData.Currency_Code = Request["iCurrency"];
            newData.Amount_of_Invoice = double.Parse(Request["iInvoiceAmount"], CultureInfo.InvariantCulture);
            newData.Third_Party_Id = Request["iVendor"].Split('|')[0];
            newData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            newData.Description = Request["iDescription"];
            newData.Due_Date = Convert.ToDateTime(Request["iEntryDueDate"]);
            newData.VAT = Request["iVATRate"] == "" ? 0 : double.Parse(Request["iVATRate"], CultureInfo.InvariantCulture);
            newData.Tax_Date = Request["iTaxInvoiceDate"].Length > 0 ? Convert.ToDateTime(Request["iTaxInvoiceDate"]) : (DateTime?)null;
            newData.Tax_Number = Request["iTaxInvoiceNumber"];
            newData.Tax_Number_17 = Request["iTaxInvoiceNumber17Digit"];
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            newData.Is_Reject = false;
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            db.FA_Payment_Request_PO.Add(newData);
            db.SaveChanges();

            for (var i = 0; i <= Request["iSectionTo[]"].Split(',').Count() - 1; i++)
            {
                var newDataSub = new FA_Payment_Request_PO_Sub();
                newDataSub.With_PO_ID = newData.id;
                newDataSub.Section_To_Code = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[0];
                newDataSub.Section_To_Name = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[1];
                newDataSub.Allocation_Amount = double.Parse(Request.Form.GetValues("iAllocationAmount[]")[i], CultureInfo.InvariantCulture);
                newDataSub.Budget_Number = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[0];
                newDataSub.Budget_Desc = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[1];
                db.FA_Payment_Request_PO_Sub.Add(newDataSub);
            }
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = newData.id;
            newHistory.Message = "Created";
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("WithPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult WithPOEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Request["iWithPOID"];
            var currUserID = currUser.GetUserId().Trim();

            var updateData = db.FA_Payment_Request_PO.Where(w => w.id.ToString() == currID.ToString()).FirstOrDefault();
            updateData.Invoice_Number = Request["iInvoiceNumber"];
            updateData.Currency_Code = Request["iCurrency"];
            updateData.Amount_of_Invoice = double.Parse(Request["iInvoiceAmount"], CultureInfo.InvariantCulture);
            updateData.Third_Party_Id = Request["iVendor"].Split('|')[0];
            updateData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            updateData.Description = Request["iDescription"];
            updateData.Due_Date = Convert.ToDateTime(Request["iEntryDueDate"]);
            updateData.VAT = Request["iVATRate"] == "" ? 0 : double.Parse(Request["iVATRate"], CultureInfo.InvariantCulture);
            updateData.Tax_Date = Request["iTaxInvoiceDate"].Length > 0 ? Convert.ToDateTime(Request["iTaxInvoiceDate"]) : (DateTime?)null;
            updateData.Tax_Number = Request["iTaxInvoiceNumber"];
            updateData.Tax_Number_17 = Request["iTaxInvoiceNumber17Digit"];
            db.SaveChanges();

            var deleteOldData = db.FA_Payment_Request_PO_Sub.Where(w => w.With_PO_ID == updateData.id).ToList();
            db.FA_Payment_Request_PO_Sub.RemoveRange(deleteOldData);
            for (var i = 0; i <= Request["iSectionTo[]"].Split(',').Count() - 1; i++)
            {
                var newDataSub = new FA_Payment_Request_PO_Sub();
                newDataSub.With_PO_ID = updateData.id;
                newDataSub.Section_To_Code = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[0];
                newDataSub.Section_To_Name = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[1];
                newDataSub.Allocation_Amount = double.Parse(Request.Form.GetValues("iAllocationAmount[]")[i], CultureInfo.InvariantCulture);
                newDataSub.Budget_Number = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[0];
                newDataSub.Budget_Desc = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[1];
                db.FA_Payment_Request_PO_Sub.Add(newDataSub);
            }
            db.SaveChanges();

            //var newHistory = new FA_Payment_Request_History();
            //newHistory.Document_Type = "WithPO";
            //newHistory.Document_Id = newData.id;
            //newHistory.Message = "Created";
            //newHistory.User_Id = currUserID;
            //newHistory.Date_At = DateTime.Now;
            //db.FA_Payment_Request_History.Add(newHistory);
            //db.SaveChanges();

            return RedirectToAction("WithPO", "PaymentRequest", new { area = "FA" });
        }

        public String getSequence(string type)
        {
            var lastSeq = "";
            if (type == "W")
            {
                var seqHeader = type + DateTime.Now.ToString("yy");

                var latestSequence = db.FA_Payment_Request_PO.Where(w => w.Receive_Number.Substring(0, 3) == seqHeader).OrderByDescending(o => o.id).Select(s => s.Receive_Number.Substring(s.Receive_Number.Length - 4, 4)).FirstOrDefault();
                lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
                lastSeq = seqHeader + "-" + lastSeq.Substring(lastSeq.Length - 4, 4);
            }
            else if (type == "N")
            {
                var seqHeader = type + DateTime.Now.ToString("yy");
                var latestSequence = db.FA_Payment_Request_Non_PO.Where(w => w.Receive_Number.Substring(0, 3) == seqHeader).OrderByDescending(o => o.id).Select(s => s.Receive_Number.Substring(s.Receive_Number.Length - 4, 4)).FirstOrDefault();
                lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
                lastSeq = seqHeader + "-" + lastSeq.Substring(lastSeq.Length - 4, 4);

            }
            else if (type == "M")
            {
                var seqHeader = type + DateTime.Now.ToString("yy");
                var latestSequence = db.FA_Payment_Request_Non_Payment.Where(w => w.Receive_Number.Substring(0, 3) == seqHeader).OrderByDescending(o => o.id).Select(s => s.Receive_Number.Substring(s.Receive_Number.Length - 4, 4)).FirstOrDefault();
                lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
                lastSeq = seqHeader + "-" + lastSeq.Substring(lastSeq.Length - 4, 4);
            }
            else if (type == "P")
            {
                var seqHeader = type + DateTime.Now.ToString("yy");
                var latestSequence = db.FA_Payment_Request_Treasury.Where(w => w.Receive_Number.Substring(0, 3) == seqHeader).OrderByDescending(o => o.id).Select(s => s.Receive_Number.Substring(s.Receive_Number.Length - 4, 4)).FirstOrDefault();
                lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
                lastSeq = seqHeader + "-" + lastSeq.Substring(lastSeq.Length - 4, 4);

            }

            return lastSeq;
        }
        [Authorize]
        public ActionResult NonPO()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currFilterLevel = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[0]) : (int?)null;
            var currFilterLevelSub = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[1]) : (int?)null;
            var currPayUsers = db.FA_Payment_Users.Where(w => w.NIK == currUserID).Select(s => s.Section_ID);
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels < 4 && w.Levels_Sub == 0 && w.Menu_Id == 43).ToList();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels < 4 && w.Levels_Sub == 0 && w.Menu_Id == 43).FirstOrDefault();
            if (currFilterLevel != null)
            {
                currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub && w.Menu_Id == 43).FirstOrDefault();
            }
            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"];
            }

            HttpCookie yearCookie = new HttpCookie("NPOFilterYear");
            yearCookie.Value = Request["iNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie statusCookie = new HttpCookie("NPOFilterStatus");
            statusCookie.Value = Request["iNPOFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iNPOFilterStatus"] ?? "Open";
            if (currPayLevel == null)
            {
                return View("UnAuthorized");
            }
            var WPOLevel = currPayLevel.Levels;
            if (WPOLevel == 1)
            {
                WPOLevel = 0;
            }

            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID && w.Levels == 1).FirstOrDefault();
            ViewBag.PayUser = currPayUser;

            var settled = db.FA_Payment_Request_Non_PO.Where(w => w.Settlement_For != null).Select(s => s.id);
            var payed = db.FA_Payment_Request_Non_PO.Where(w => w.Non_Payment_ID != null).Select(s => s.Non_Payment_ID);


            ViewBag.closingDate = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Year == DateTime.Now.Year && w.Closing_Date.Month == DateTime.Now.Month && w.Type == "Non-PO").First();
            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currPayLevel.Levels_Sub;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.COA = db.AX_COA.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.SettlementList = db.FA_Payment_Request_Non_PO.Where(w => currPayUsers.Contains(w.Section_From_Code) && w.Payment_Type == "Pre-Payment" && !settled.Contains(w.id)).OrderByDescending(o => o.id).ToList();
            ViewBag.Procate = db.AX_Procate.ToList();
            ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => !payed.Contains(w.Receive_Number.ToString())).ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == currPeriodFY) && w.Budget_Type != "BEL").ToList();

            string[] exceptionUserInput = { "673.08.14", "P171119" };
            if (WPOLevel <= 1 && exceptionUserInput.Contains(currUserID))
            {
                if (currStatus == "Open")
                {
                    ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub) && w.Approval < WPOLevel + 2 && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
                }
                else if (currStatus == "Signed")
                {
                    ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval > WPOLevel + 1 || (w.Approval >= WPOLevel && w.Approval_Sub > currPayLevel.Levels_Sub)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                }
                else
                {
                    ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                }
            }
            else
            {
                if (WPOLevel <= 1)
                {
                    if (currStatus == "Open")
                    {
                        ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && ((w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub) && w.Approval < WPOLevel + 2) && !exceptionUserInput.Contains(w.Created_By) && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
                    }
                    else if (currStatus == "Signed")
                    {
                        ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval > WPOLevel + 1 || (w.Approval >= WPOLevel && w.Approval_Sub > currPayLevel.Levels_Sub)) && !exceptionUserInput.Contains(w.Created_By) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                    else
                    {
                        ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub && !exceptionUserInput.Contains(w.Created_By) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                }
                else
                {
                    if (currStatus == "Open")
                    {
                        ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub) && w.Approval < WPOLevel + 1 && w.Approval_Sub < currPayLevel.Levels_Sub + 1 && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
                    }
                    else if (currStatus == "Signed")
                    {
                        ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval > WPOLevel || (w.Approval >= WPOLevel && w.Approval_Sub > currPayLevel.Levels_Sub)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                    else
                    {
                        ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                }
            }
            return View();
        }
        public ActionResult NonPOView()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currEVoucher = Request["EVoucher"];
            var currFY = int.Parse(currEVoucher.Substring(1, 2));
            var FYFrom = "FY1" + (currFY - 1).ToString();
            var FYCurrent = "FY1" + currFY.ToString();
            var FYTo = "FY1" + (currFY + 1).ToString();
            //return Content(currFY + "##" + FYFrom + "##" + FYCurrent + "##" + FYTo);

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"];
            }
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            ViewBag.COA = db.AX_COA.ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.Procate = db.AX_Procate.ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == FYCurrent) && w.Budget_Type != "BEL").ToList();
            //ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == FYFrom || w.Period_FY == FYCurrent || w.Period_FY == FYTo) && w.Budget_Type != "BEL").ToList();
            ViewBag.EmailList = db.Users.Where(w => w.Email != null).Select(s => s.Email);
            var settled = db.FA_Payment_Request_Non_PO.Where(w => w.Settlement_For != null).Select(s => s.id);

            ViewBag.SettlementList = db.FA_Payment_Request_Non_PO.Where(w => !settled.Contains(w.id)).OrderByDescending(o => o.id).ToList();
            ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => w.Receive_Number == currEVoucher).ToList();

            ViewBag.EVoucherNumber = currEVoucher;

            return View();
        }
        [Authorize]
        public ActionResult NonPayment()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currFilterLevel = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[0]) : (int?)null;
            var currFilterLevelSub = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[1]) : (int?)null;
            var currPayUsers = db.FA_Payment_Users.Where(w => w.NIK == currUserID).Select(s => s.Section_ID);
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels < 4 && w.Levels_Sub == 0 && w.Menu_Id == 43).FirstOrDefault();
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels < 4 && w.Menu_Id == 43).OrderBy(o => o.Levels).ThenBy(o => o.Levels_Sub).ToList();
            if (currFilterLevel != null)
            {
                currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub && w.Menu_Id == 43).FirstOrDefault();
            }
            HttpCookie yearCookie = new HttpCookie("NPOFilterYear");
            yearCookie.Value = Request["iNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie statusCookie = new HttpCookie("NPOFilterStatus");
            statusCookie.Value = Request["iNPOFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iNPOFilterStatus"] ?? "Open";
            if (currPayLevel == null)
            {
                return View("UnAuthorized");
            }
            var WPOLevel = currPayLevel.Levels;
            if (WPOLevel == 1)
            {
                WPOLevel = 0;
            }

            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID && w.Levels == 1).FirstOrDefault();
            ViewBag.PayUser = currPayUser;
            var getSettlementData = db.FA_Payment_Request_Non_PO_Sub.Where(w => w.COA_Code == "1849101").Select(s => s.Non_PO_ID);

            ViewBag.closingDate = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Year == DateTime.Now.Year && w.Closing_Date.Month == DateTime.Now.Month && w.Type == "Non-PO").First();
            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currPayLevel.Levels_Sub;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.COA = db.AX_COA.ToList();
            ViewBag.COATo = db.AX_COA.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.Procate = db.AX_Procate.ToList();
            ViewBag.SettlementList = db.FA_Payment_Request_Non_PO.Where(w => getSettlementData.Contains(w.id)).OrderByDescending(o => o.id).ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY).ToList();

            string[] exceptionUserInput = { "673.08.14", "P171119" };
            if (WPOLevel <= 1 && exceptionUserInput.Contains(currUserID))
            {
                if (currStatus == "Open")
                {
                    ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub) && w.Approval < WPOLevel + 2 && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
                }
                else if (currStatus == "Signed")
                {
                    ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval > WPOLevel + 1 || (w.Approval >= WPOLevel && w.Approval_Sub > currPayLevel.Levels_Sub)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                }
                else
                {
                    ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                }
            }
            else
            {
                if (WPOLevel <= 1)
                {
                    if (currStatus == "Open")
                    {
                        ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && ((w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub) && w.Approval < WPOLevel + 2) && !exceptionUserInput.Contains(w.Created_By) && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
                    }
                    else if (currStatus == "Signed")
                    {
                        ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval > WPOLevel + 1 || (w.Approval >= WPOLevel && w.Approval_Sub > currPayLevel.Levels_Sub)) && !exceptionUserInput.Contains(w.Created_By) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                    else
                    {
                        ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub && !exceptionUserInput.Contains(w.Created_By) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                }
                else
                {
                    if (currStatus == "Open")
                    {
                        ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub) && w.Approval < WPOLevel + 1 && w.Approval_Sub < currPayLevel.Levels_Sub + 1 && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
                    }
                    else if (currStatus == "Signed")
                    {
                        ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && (w.Approval > WPOLevel || (w.Approval >= WPOLevel && w.Approval_Sub > currPayLevel.Levels_Sub)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                    else
                    {
                        ViewBag.NonPaymentList = db.FA_Payment_Request_Non_Payment.Where(w => (currPayUsers.Contains(w.Section_From_Code) || w.Created_By == currUserID) && w.Approval >= WPOLevel && w.Approval_Sub >= currPayLevel.Levels_Sub && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
                    }
                }
            }
            return View();
        }

        [HttpPost] 
        public ActionResult NonPOAdd()
        {
            try
            {
                var currUser = (ClaimsIdentity)User.Identity;
                var currUserID = currUser.GetUserId().Trim();
                var currDeptName = currUser.FindFirstValue("deptName");
                var currSeq = this.getSequence("N");
                var currEntryDate = DateTime.Now;

                // Cek Closing Calendar
                var closingCalendar = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Month == DateTime.Now.Month && w.Closing_Date.Year == DateTime.Now.Year && w.Type == "Non-PO").FirstOrDefault();

                IFormatProvider culture = new CultureInfo("en-US", true);

                var sectionCode = Request["iSectionFrom"].Split('|')[0];
                var thirdParty = Request["iVendor"].Split('|')[0];
                var invoice = Request["iInvoiceNumber"].Trim();
                var paymentType = Request["iPaymentType"].Trim();
                var isWorkingOrder = Request["iIsWorkingOrder"] != null ? true : false;

                var checkInvoice = db.FA_Payment_Request_Non_PO.Where(w => w.Third_Party_Id == thirdParty
                && w.Invoice_Number.Trim() == invoice && w.Payment_Type == paymentType).FirstOrDefault();

                if (checkInvoice != null)
                {
                    TempData["errorMessage"] = "Submit failed, Invoice '" + checkInvoice.Invoice_Number + "' for this vendor is already used in " + checkInvoice.Receive_Number + " by user " + checkInvoice.Users.Name + ".!";
                    return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
                }

                if (closingCalendar != null && DateTime.Now.Day > closingCalendar.Closing_Date.Day)
                {
                    currEntryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                }

                var newData = new FA_Payment_Request_Non_PO();
                newData.Receive_Number = currSeq;
                newData.Entry_Date = currEntryDate;
                newData.Section_From_Code = Request["iSectionFrom"].Split('|')[0];
                newData.Section_From_Name = Request["iSectionFrom"].Split('|')[1];
                newData.Settlement_For = Request["iPaymentType"] == "Settlement" ? Request["iSettlementFor"] : null;
                newData.Non_Payment_ID = Request["iPaymentType"] == "Realization" ? Request["iNonPaymentID"] : null;
                newData.Invoice_Number = Request["iInvoiceNumber"];
                newData.Currency_Code = Request["iCurrency"];
                newData.Amount_of_Invoice = double.Parse(Request["iInvoiceAmount"], CultureInfo.InvariantCulture);
                newData.Third_Party_Id = Request["iVendor"].Split('|')[0];
                newData.Third_Party_Name = Request["iVendor"].Split('|')[1];
                newData.Description = Request["iDescription"];
                newData.Due_Date = DateTime.ParseExact(Request["iEntryDueDate"], "MM/dd/yyyy", culture);
                newData.Payment_Type = Request["iPaymentType"];
                newData.VAT = Request["iVATRate"] == "" ? 0 : double.Parse(Request["iVATRate"], CultureInfo.InvariantCulture);
                newData.Tax_Date = Request["iTaxInvoiceDate"].Length > 0 ? DateTime.ParseExact(Request["iTaxInvoiceDate"], "MM/dd/yyyy", culture) : (DateTime?)null;
                newData.Tax_Number = Request["iTaxInvoiceNumber"];
                newData.Tax_Number_17 = Request["iTaxInvoiceNumber17Digit"];
                newData.Payment_Method = Request["iPaymentMethod"];
                newData.Approval = 1;
                newData.Approval_Sub = 0;
                newData.Is_Reject = false;
                newData.Is_Working_Order = isWorkingOrder;
                newData.Created_By = currUserID;
                newData.Created_At = DateTime.Now;

                db.FA_Payment_Request_Non_PO.Add(newData);
                db.SaveChanges();

                if (Request["iPaymentType"] == "Direct-Payment" || Request["iPaymentType"] == "Settlement")
                {
                    var arrSectionTo = Request.Form.GetValues("iSectionTo[]");

                    if (arrSectionTo != null && arrSectionTo.Length > 0)
                    {
                        // Fetch all arrays
                        var arrAmount = Request.Form.GetValues("iAllocationAmount[]");
                        var arrCOA = Request.Form.GetValues("iCOA[]");
                        var arrProcate = Request.Form.GetValues("iProcate[]");
                        var arrBudget = Request.Form.GetValues("iBudgetNumber[]"); 
                        var arrDesc = Request.Form.GetValues("iAllocationDesc[]");
                        var arrVAT = Request.Form.GetValues("iAllocVATRate[]");
                        var arrTaxDate = Request.Form.GetValues("iAllocTaxInvoiceDate[]");
                        var arrTaxNum = Request.Form.GetValues("iAllocTaxInvoiceNumber[]");
                        var arrTaxNum17 = Request.Form.GetValues("iAllocTaxInvoiceNumber17Digit[]");

                        for (var i = 0; i < arrSectionTo.Length; i++)
                        {
                            var newDataSub = new FA_Payment_Request_Non_PO_Sub();
                            newDataSub.Non_PO_ID = newData.id;

                            string[] sectionParts = arrSectionTo[i].ToString().Split('|');
                            newDataSub.Section_To_Code = sectionParts.Length > 0 ? sectionParts[0] : "";
                            newDataSub.Section_To_Name = sectionParts.Length > 1 ? sectionParts[1] : "";

                            string valAmount = (arrAmount != null && arrAmount.Length > i) ? arrAmount[i] : "0";
                            newDataSub.Allocation_Amount = double.Parse(valAmount, CultureInfo.InvariantCulture);

                            string valCOA = (arrCOA != null && arrCOA.Length > i) ? arrCOA[i] : "";
                            string[] coaParts = valCOA.Split('|');
                            newDataSub.COA_Code = coaParts.Length > 0 ? coaParts[0] : "";
                            newDataSub.COA_Name = coaParts.Length > 1 ? coaParts[1] : "";

                            string valProcate = (arrProcate != null && arrProcate.Length > i) ? arrProcate[i] : "";
                            string[] procateParts = valProcate.Split('|');
                            newDataSub.Procate_Code = procateParts.Length > 0 ? procateParts[0] : "";
                            newDataSub.Procate_Name = procateParts.Length > 1 ? procateParts[1] : "";

                            string valBudget = (arrBudget != null && arrBudget.Length > i) ? arrBudget[i] : "";

                            if (!string.IsNullOrEmpty(valBudget))
                            {
                                string[] budgetParts = valBudget.Split('|');
                                newDataSub.Budget_Number = budgetParts.Length > 0 ? budgetParts[0] : "";
                                newDataSub.Budget_Desc = budgetParts.Length > 1 ? budgetParts[1] : "";
                            }
                            else
                            {
                                newDataSub.Budget_Number = "";
                                newDataSub.Budget_Desc = "";
                            }

                            newDataSub.Description = (arrDesc != null && arrDesc.Length > i) ? arrDesc[i] : "";

                            string valVAT = (arrVAT != null && arrVAT.Length > i) ? arrVAT[i] : "";
                            newDataSub.VAT = !string.IsNullOrEmpty(valVAT) ? double.Parse(valVAT, CultureInfo.InvariantCulture) : 0;

                            string valTaxDate = (arrTaxDate != null && arrTaxDate.Length > i) ? arrTaxDate[i] : "";
                            newDataSub.Tax_Date = !string.IsNullOrEmpty(valTaxDate) ? DateTime.ParseExact(valTaxDate, "MM/dd/yyyy", culture) : (DateTime?)null;
                            newDataSub.Tax_Number = (arrTaxNum != null && arrTaxNum.Length > i) ? arrTaxNum[i] : null;
                            newDataSub.Tax_Number_17 = (arrTaxNum17 != null && arrTaxNum17.Length > i) ? arrTaxNum17[i] : null;

                            db.FA_Payment_Request_Non_PO_Sub.Add(newDataSub);
                        }
                        db.SaveChanges();
                    }
                }

                //var reqNo = Request.Form["requestNo"];
                //if (!string.IsNullOrWhiteSpace(reqNo))
                //{
                //    var req = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == reqNo);

                //    if (req != null && !string.IsNullOrEmpty(invoice) && invoice.ToUpper().Contains("/KOPKAR/"))
                //    {
                //        req.Receive_Number = currSeq;
                //        dbm.SaveChanges();
                //    }
                //}
                // --- UPDATE REQUEST RECEIVE NUMBER (LOGIC BARU) ---
                var reqNoString = Request.Form["requestNo"]; // Ambil string gabungan: "REQ-001,REQ-002"

                if (!string.IsNullOrWhiteSpace(reqNoString))
                {
                    // 1. Pecah string menjadi array berdasarkan koma
                    var reqNoList = reqNoString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(r => r.Trim()) // Hapus spasi jika ada
                                               .ToList();

                    // 2. Cek apakah ini transaksi KOPKAR (berdasarkan format Invoice)
                    if (reqNoList.Any() && !string.IsNullOrEmpty(invoice) && invoice.ToUpper().Contains("/KOPKAR/"))
                    {
                        // 3. Ambil semua request yang cocok dari database sekaligus
                        var requestsToUpdate = dbm.HC_GA_Inventory_Requests
                                                  .Where(r => reqNoList.Contains(r.Request_No))
                                                  .ToList();

                        // 4. Update Receive_Number untuk setiap request yang ditemukan
                        foreach (var req in requestsToUpdate)
                        {
                            req.Receive_Number = currSeq;
                            req.Invoice_Number = invoice;
                        }

                        // 5. Simpan perubahan ke database
                        if (requestsToUpdate.Any())
                        {
                            dbm.SaveChanges();
                        }
                    }
                }
                // --------------------------------------------------

                var newHistory = new FA_Payment_Request_History();
                newHistory.Document_Type = "NonPO";
                newHistory.Document_Id = newData.id;
                newHistory.Message = "Created";
                newHistory.User_Id = currUserID;
                newHistory.Date_At = DateTime.Now;
                db.FA_Payment_Request_History.Add(newHistory);
                db.SaveChanges();

                return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
            }
        }

        public ActionResult NonPOEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currDeptName = currUser.FindFirstValue("deptName");
            var currID = Int32.Parse(Request["iNonPOID"]);
            IFormatProvider culture = new CultureInfo("en-US", true);
            var sectionCode = Request["iSectionFrom"].Split('|')[0];
            var thirdParty = Request["iVendor"].Split('|')[0];
            var invoice = Request["iInvoiceNumber"].Trim();
            var paymentType = Request["iPaymentType"].Trim();
            var isWorkingOrder = Request["iIsWorkingOrder"] != null ? true : false;

            var checkInvoice = db.FA_Payment_Request_Non_PO.Where(w => w.Third_Party_Id == thirdParty
            && w.Invoice_Number.Trim() == invoice && w.Payment_Type == paymentType && w.id != currID).FirstOrDefault();

            if (checkInvoice != null)
            {
                TempData["errorMessage"] = "Submit failed, Invoice '" + checkInvoice.Invoice_Number + "' for this vendor is already used in " + checkInvoice.Receive_Number + " by user " + checkInvoice.Users.Name + ".!";
                return Redirect(Request.UrlReferrer.ToString());
            }

            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).FirstOrDefault();
            if (updateData.Entry_Date != DateTime.ParseExact(Request["iEntryDate"], "MM/dd/yyyy", culture))
            {
                updateData.Entry_Date = DateTime.ParseExact(Request["iEntryDate"], "MM/dd/yyyy", culture);
            }
            updateData.Settlement_For = Request["iPaymentType"] == "Settlement" ? Request["iSettlementFor"] : null;
            updateData.Invoice_Number = Request["iInvoiceNumber"];
            updateData.Amount_of_Invoice = double.Parse(Request["iInvoiceAmount"], CultureInfo.InvariantCulture);
            updateData.Third_Party_Id = Request["iVendor"].Split('|')[0];
            updateData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            updateData.Description = Request["iDescription"];
            updateData.Currency_Code = Request["iCurrency"];
            updateData.Due_Date = DateTime.ParseExact(Request["iEntryDueDate"], "MM/dd/yyyy", culture);
            updateData.Payment_Type = Request["iPaymentType"];
            updateData.VAT = Request["iVATRate"] == "" ? 0 : double.Parse(Request["iVATRate"], CultureInfo.InvariantCulture);
            updateData.Tax_Date = Request["iTaxInvoiceDate"].Length > 0 ? DateTime.ParseExact(Request["iTaxInvoiceDate"], "MM/dd/yyyy", culture) : (DateTime?)null;
            updateData.Tax_Number = Request["iTaxInvoiceNumber"];
            updateData.Tax_Number_17 = Request["iTaxInvoiceNumber17Digit"];
            updateData.Payment_Method = Request["iPaymentMethod"];
            if (currDeptName == "SUPPLY & PROCUREMENT")
            {
                updateData.Is_Working_Order = isWorkingOrder;
            }
            db.SaveChanges();

            var deleteOldData = db.FA_Payment_Request_Non_PO_Sub.Where(w => w.Non_PO_ID == updateData.id).ToList();
            db.FA_Payment_Request_Non_PO_Sub.RemoveRange(deleteOldData);
            if (Request["iPaymentType"] == "Direct-Payment" || Request["iPaymentType"] == "Settlement")
            {
                for (var i = 0; i <= Request.Form.GetValues("iSectionTo[]").Count() - 1; i++)
                {
                    var newDataSub = new FA_Payment_Request_Non_PO_Sub();
                    newDataSub.Non_PO_ID = updateData.id;
                    newDataSub.Section_To_Code = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[0];
                    newDataSub.Section_To_Name = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[1];
                    newDataSub.Allocation_Amount = double.Parse(Request.Form.GetValues("iAllocationAmount[]")[i], CultureInfo.InvariantCulture);
                    newDataSub.COA_Code = Request.Form.GetValues("iCOA[]")[i].ToString().Split('|')[0];
                    newDataSub.COA_Name = Request.Form.GetValues("iCOA[]")[i].ToString().Split('|')[1];
                    newDataSub.Procate_Code = Request.Form.GetValues("iProcate[]")[i].ToString().Split('|')[0];
                    newDataSub.Procate_Name = Request.Form.GetValues("iProcate[]")[i].ToString().Split('|')[1];
                    newDataSub.Budget_Number = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[0];
                    newDataSub.Budget_Desc = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[1];
                    newDataSub.Description = Request.Form.GetValues("iAllocationDesc[]")[i];
                    newDataSub.VAT = Request.Form.GetValues("iAllocVATRate[]")[i].Length > 0 ? double.Parse(Request.Form.GetValues("iAllocVATRate[]")[i], CultureInfo.InvariantCulture) : 0;
                    newDataSub.Tax_Date = Request.Form.GetValues("iAllocTaxInvoiceDate[]")[i].Length > 0 ? DateTime.ParseExact(Request.Form.GetValues("iAllocTaxInvoiceDate[]")[i], "MM/dd/yyyy", culture) : (DateTime?)null;
                    newDataSub.Tax_Number = Request.Form.GetValues("iAllocTaxInvoiceNumber[]")[i];
                    newDataSub.Tax_Number_17 = Request.Form.GetValues("iAllocTaxInvoiceNumber17Digit[]")[i];
                    db.FA_Payment_Request_Non_PO_Sub.Add(newDataSub);
                }
            }
            db.SaveChanges();

            //var newHistory = new FA_Payment_Request_History();
            //newHistory.Document_Type = "NonPO";
            //newHistory.Document_Id = newData.id;
            //newHistory.Message = "Created";
            //newHistory.User_Id = currUserID;
            //newHistory.Date_At = DateTime.Now;
            //db.FA_Payment_Request_History.Add(newHistory);
            //db.SaveChanges();

            // return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
            return Redirect(Request.UrlReferrer.ToString());
        }
        [Authorize]
        public ActionResult APNonPO()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            HttpCookie yearCookie = new HttpCookie("APNPOFilterYear");
            yearCookie.Value = Request["iAPNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie monthCookie = new HttpCookie("APNPOFilterMonth");
            monthCookie.Value = Request["iAPNPOFilterMonth"] ?? DateTime.Now.Month.ToString();
            Response.SetCookie(monthCookie);
            HttpCookie statusCookie = new HttpCookie("APNPOFilterStatus");
            statusCookie.Value = Request["iAPNPOFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iAPNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            var currMonth = Request["iAPNPOFilterMonth"] ?? DateTime.Now.Month.ToString();
            var currStatus = Request["iAPNPOFilterStatus"] ?? "Open";
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();

            var currFilterLevel = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[0]) : (int?)null;
            var currFilterLevelSub = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[1]) : (int?)null;
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 4 && w.Menu_Id == 43).FirstOrDefault();
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels >= 4 && w.Levels_Sub == 0 && w.Menu_Id == 43).ToList();
            if (currFilterLevel != null)
            {
                currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub && w.Menu_Id == 43).FirstOrDefault();
            }

            if (TempData["errorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["errorMessage"];
            }
            if (TempData["successMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["successMessage"];
            }
            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterMonth = currMonth;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            ViewBag.COA = db.AX_COA.ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.Procate = db.AX_Procate.ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == currPeriodFY) && w.Budget_Type != "BEL").ToList();
            ViewBag.EmailList = db.Users.Where(w => w.Email != null).Select(s => s.Email);
            var settled = db.FA_Payment_Request_Non_PO.Where(w => w.Settlement_For != null).Select(s => s.id);

            ViewBag.SettlementList = db.FA_Payment_Request_Non_PO.Where(w => !settled.Contains(w.id)).OrderByDescending(o => o.id).ToList();

            if (currStatus == "Open")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => (w.Approval == currPayLevel.Levels && w.Approval_Sub == currPayLevel.Levels_Sub) && (w.Approval <= 5) && w.Entry_Date.Year.ToString() == currYear && w.Entry_Date.Month.ToString() == currMonth && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => w.Approval > currPayLevel.Levels && w.Entry_Date.Year.ToString() == currYear && w.Entry_Date.Month.ToString() == currMonth).OrderByDescending(o => o.id).ToList();
            }
            else
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => w.Approval >= currPayLevel.Levels && w.Approval_Sub >= currPayLevel.Levels_Sub && w.Entry_Date.Year.ToString() == currYear && w.Entry_Date.Month.ToString() == currMonth).OrderByDescending(o => o.id).ToList();
            }
            return View();
        }

        [Authorize]
        public ActionResult FANonPayment()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            HttpCookie yearCookie = new HttpCookie("APNPOFilterYear");
            yearCookie.Value = Request["iAPNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie statusCookie = new HttpCookie("APNPOFilterStatus");
            statusCookie.Value = Request["iAPNPOFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iAPNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iAPNPOFilterStatus"] ?? "Open";
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 4 && w.Menu_Id == 43).FirstOrDefault();

            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevel = currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            ViewBag.COA = db.AX_COA.ToList();
            ViewBag.COATo = db.AX_COA.ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.Procate = db.AX_Procate.ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY).ToList();
            if (currStatus == "Open")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_Payment.Where(w => (w.Approval >= 4 && w.Approval_Sub >= 0) && (w.Approval < 5) && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_Payment.Where(w => w.Approval >= 5 && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }
            else
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_Payment.Where(w => w.Approval >= 4 && w.Approval_Sub >= 0 && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }
            return View();
        }
        [Authorize]
        public ActionResult HCNonPayment()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            string[] HRDCOAList = { "7515401", "7515402", "7515403", "7515404", "7515405", "7515499", "7651101", "7651102", "7651103" };
            string[] HRDSectionList = { "B2200", "B2210", "B2220" };
            HttpCookie yearCookie = new HttpCookie("APNPOFilterYear");
            yearCookie.Value = Request["iAPNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie statusCookie = new HttpCookie("APNPOFilterStatus");
            statusCookie.Value = Request["iAPNPOFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iAPNPOFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iAPNPOFilterStatus"] ?? "Open";
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 3 && w.Levels_Sub > 0 && w.Menu_Id == 43).FirstOrDefault();
            var checkCOAHRD = db.FA_Payment_Request_Non_Payment_Sub.Where(w => HRDCOAList.Contains(w.COA_Code) && w.Non_PO.Entry_Date.Year.ToString() == currYear).Select(s => s.Non_PO_ID);



            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevel = currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            ViewBag.COA = db.AX_COA.ToList();
            ViewBag.COATo = db.AX_COA.ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.Procate = db.AX_Procate.ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => w.Period_FY == currPeriodFY).ToList();
            if (currStatus == "Open")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_Payment.Where(w => checkCOAHRD.Contains(w.id) && (w.Approval == 3 && (w.Approval_Sub >= currPayLevel.Levels_Sub && w.Approval_Sub < currPayLevel.Levels_Sub + 1)) && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_Payment.Where(w => checkCOAHRD.Contains(w.id) && ((w.Approval >= 3 && w.Approval_Sub > currPayLevel.Levels_Sub) || (w.Approval > 3)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }
            else
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_Payment.Where(w => checkCOAHRD.Contains(w.id) && ((w.Approval == 3 && w.Approval_Sub >= currPayLevel.Levels_Sub) || (w.Approval > 3)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }
            return View();
        }
        [Authorize]
        public ActionResult TreasuryNonPO()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 5 && w.Menu_Id == 43).FirstOrDefault();

            ViewBag.PayUserLevel = currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Bank = db.V_AXBankAccount.ToList();
            ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => w.Approval >= 5 && w.Approval_Sub >= 0).OrderByDescending(o => o.id).ToList();
            ViewBag.PaymentList = db.FA_Payment_Request_Treasury.Where(w => w.Is_Complete == false).ToList();
            return View();
        }
        [Authorize]
        public ActionResult Treasury()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 5 && w.Menu_Id == 43).FirstOrDefault();

            ViewBag.PayUserLevel = currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Bank = db.V_AXBankAccount.ToList();
            ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => w.Approval >= 5 && w.Approval_Sub >= 0).OrderByDescending(o => o.id).ToList();
            ViewBag.WithPOList = db.FA_Payment_Request_PO.Where(w => w.Approval >= 5 && w.Approval_Sub >= 0).OrderByDescending(o => o.id).ToList();
            ViewBag.PaymentList = db.FA_Payment_Request_Treasury.Where(w => w.Is_Complete == false).ToList();
            return View();
        }
        [Authorize]
        public ActionResult HCApproval()
        {
            ViewBag.NavHide = true;
            string[] WHTCOAList = { "3195301", "3195302", "3195303", "3195304", "3195305", "3195306", "3195308" };
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currFilterLevel = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[0]) : (int?)null;
            var currFilterLevelSub = Request["iNPOFilterLevel"] != null ? int.Parse(Request["iNPOFilterLevel"].Split('|')[1]) : (int?)null;
            var currPayLevelCheck = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 3 && w.Levels_Sub > 0 && w.Menu_Id == 43).ToList();
            var currPayUser = db.FA_Payment_Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == 3 && w.Levels_Sub > 0 && w.Menu_Id == 43).FirstOrDefault();
            if (currFilterLevel != null)
            {
                currPayLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub && w.Menu_Id == 43).FirstOrDefault();
            }
            string[] HRDCOAList = { "7515401", "7515402", "7515403", "7515404", "7515405", "7515499", "7651101", "7651102", "7651103" };
            string[] HRDSectionList = { "B2200", "B2210", "B2220" };
            HttpCookie yearCookie = new HttpCookie("HCAFilterYear");
            yearCookie.Value = Request["iHCAFilterYear"] ?? DateTime.Now.Year.ToString();
            Response.SetCookie(yearCookie);
            HttpCookie statusCookie = new HttpCookie("HCAFilterStatus");
            statusCookie.Value = Request["iHCAFilterStatus"] ?? "Open";
            Response.SetCookie(statusCookie);
            var currYear = Request["iHCAFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iHCAFilterStatus"] ?? "Open";
            var checkCOAHRD = db.FA_Payment_Request_Non_PO_Sub.Where(w => HRDCOAList.Contains(w.COA_Code) && w.Non_PO.Entry_Date.Year.ToString() == currYear).Select(s => s.Non_PO_ID);


            ViewBag.closingDate = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Year == DateTime.Now.Year && w.Closing_Date.Month == DateTime.Now.Month && w.Type == "Non-PO").First();
            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.PayUserLevelCheck = currPayLevelCheck;
            ViewBag.PayUserLevel = currFilterLevel != null ? currFilterLevel : currPayLevel.Levels;
            ViewBag.PayUserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currPayLevel.Levels_Sub;
            ViewBag.PayUser = currPayUser;
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.Currency = db.AX_Currency.ToList();
            ViewBag.COA = db.AX_COA.ToList();
            ViewBag.WHTCOA = db.AX_COA.Where(w => WHTCOAList.Contains(w.MAINACCOUNTID)).ToList();
            ViewBag.Procate = db.AX_Procate.ToList();
            ViewBag.Budget = db.V_FA_Payment_Request_Budget_List.Where(w => (w.Period_FY == currPeriodFY) && w.Budget_Type != "BEL").ToList();

            if (currStatus == "Open")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => checkCOAHRD.Contains(w.id) && (w.Approval == 3 && (w.Approval_Sub >= currPayLevel.Levels_Sub && w.Approval_Sub < currPayLevel.Levels_Sub + 1)) && w.Entry_Date.Year.ToString() == currYear && w.Is_Reject == false).OrderByDescending(o => o.id).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => checkCOAHRD.Contains(w.id) && ((w.Approval >= 3 && w.Approval_Sub > currPayLevel.Levels_Sub) || (w.Approval > 3)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }
            else
            {
                ViewBag.NonPOList = db.FA_Payment_Request_Non_PO.Where(w => checkCOAHRD.Contains(w.id) && ((w.Approval == 3 && w.Approval_Sub >= currPayLevel.Levels_Sub) || (w.Approval > 3)) && w.Entry_Date.Year.ToString() == currYear).OrderByDescending(o => o.id).ToList();
            }
            return View();
        }
        public ActionResult NonPaymentAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currSeq = this.getSequence("M");
            var currEntryDate = DateTime.Now;
            var closingCalendar = db.FA_Closing_Calendar.Where(w => w.Closing_Date.Month == DateTime.Now.Month && w.Closing_Date.Year == DateTime.Now.Year && w.Type == "Non-PO").FirstOrDefault();
            IFormatProvider culture = new CultureInfo("en-US", true);
            //check Finance Closing Calendar
            if (closingCalendar != null && DateTime.Now.Day > closingCalendar.Closing_Date.Day)
            {
                currEntryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            }
            var newData = new FA_Payment_Request_Non_Payment();
            newData.Receive_Number = currSeq;
            newData.Settlement_For = Request["iPaymentType"] == "Settlement" ? Request["iSettlementFor"] : null;
            newData.Entry_Date = currEntryDate;
            newData.Section_From_Code = Request["iSectionFrom"].Split('|')[0];
            newData.Section_From_Name = Request["iSectionFrom"].Split('|')[1];
            newData.Currency_Code = Request["iCurrency"];
            newData.Amount_of_Invoice = double.Parse(Request["iInvoiceAmount"], CultureInfo.InvariantCulture);
            newData.Third_Party_Id = Request["iVendor"].Split('|')[0];
            newData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            newData.Description = Request["iDescription"];
            newData.COA_Code = Request["iCOATo"].ToString().Split('|')[0];
            newData.COA_Name = Request["iCOATo"].ToString().Split('|')[1];
            newData.Bank = Request["iBank"];
            newData.Type = Request["iNonPaymentType"];
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            newData.Is_Reject = false;
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            db.FA_Payment_Request_Non_Payment.Add(newData);
            db.SaveChanges();

            for (var i = 0; i <= Request.Form.GetValues("iSectionTo[]").Count() - 1; i++)
            {
                var newDataSub = new FA_Payment_Request_Non_Payment_Sub();
                newDataSub.Non_PO_ID = newData.id;
                newDataSub.Section_To_Code = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[0];
                newDataSub.Section_To_Name = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[1];
                newDataSub.Allocation_Amount = double.Parse(Request.Form.GetValues("iAllocationAmount[]")[i], CultureInfo.InvariantCulture);
                newDataSub.Credit_Amount = Request.Form.GetValues("iCreditAmount[]")[i] != null && Request.Form.GetValues("iCreditAmount[]")[i].Length > 0 ? double.Parse(Request.Form.GetValues("iCreditAmount[]")[i], CultureInfo.InvariantCulture) : 0;
                newDataSub.COA_Code = Request.Form.GetValues("iCOA[]")[i].ToString().Split('|')[0];
                newDataSub.COA_Name = Request.Form.GetValues("iCOA[]")[i].ToString().Split('|')[1];
                newDataSub.Procate_Code = Request.Form.GetValues("iProcate[]")[i].ToString().Split('|')[0];
                newDataSub.Procate_Name = Request.Form.GetValues("iProcate[]")[i].ToString().Split('|')[1];
                newDataSub.Budget_Number = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[0];
                newDataSub.Budget_Desc = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[1];
                newDataSub.Description = Request.Form.GetValues("iAllocationDesc[]")[i];
                db.FA_Payment_Request_Non_Payment_Sub.Add(newDataSub);
            }
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "NonPO";
            newHistory.Document_Id = newData.id;
            newHistory.Message = "Created";
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("NonPayment", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult NonPaymentEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Int32.Parse(Request["iNonPOID"]);
            IFormatProvider culture = new CultureInfo("en-US", true);

            var updateData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).FirstOrDefault();
            if (updateData.Entry_Date != DateTime.ParseExact(Request["iEntryDate"], "MM/dd/yyyy", culture))
            {
                updateData.Entry_Date = DateTime.ParseExact(Request["iEntryDate"], "MM/dd/yyyy", culture);
            }
            //updateData.Invoice_Number = Request["iInvoiceNumber"];
            updateData.Amount_of_Invoice = double.Parse(Request["iInvoiceAmount"], CultureInfo.InvariantCulture);
            updateData.Third_Party_Id = Request["iVendor"].Split('|')[0];
            updateData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            updateData.Description = Request["iDescription"];
            updateData.COA_Code = Request["iCOATo"].ToString().Split('|')[0];
            updateData.COA_Name = Request["iCOATo"].ToString().Split('|')[1];
            updateData.Bank = Request["iBank"];
            updateData.Type = Request["iNonPaymentType"];
            updateData.Currency_Code = Request["iCurrency"];
            db.SaveChanges();

            var deleteOldData = db.FA_Payment_Request_Non_Payment_Sub.Where(w => w.Non_PO_ID == updateData.id).ToList();
            db.FA_Payment_Request_Non_Payment_Sub.RemoveRange(deleteOldData);
            for (var i = 0; i <= Request.Form.GetValues("iSectionTo[]").Count() - 1; i++)
            {
                var newDataSub = new FA_Payment_Request_Non_Payment_Sub();
                newDataSub.Non_PO_ID = updateData.id;
                newDataSub.Section_To_Code = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[0];
                newDataSub.Section_To_Name = Request.Form.GetValues("iSectionTo[]")[i].ToString().Split('|')[1];
                newDataSub.Allocation_Amount = double.Parse(Request.Form.GetValues("iAllocationAmount[]")[i], CultureInfo.InvariantCulture);
                newDataSub.Credit_Amount = Request.Form.GetValues("iCreditAmount[]")[i] != null && Request.Form.GetValues("iCreditAmount[]")[i].Length > 0 ? double.Parse(Request.Form.GetValues("iCreditAmount[]")[i], CultureInfo.InvariantCulture) : 0;
                newDataSub.COA_Code = Request.Form.GetValues("iCOA[]")[i].ToString().Split('|')[0];
                newDataSub.COA_Name = Request.Form.GetValues("iCOA[]")[i].ToString().Split('|')[1];
                newDataSub.Procate_Code = Request.Form.GetValues("iProcate[]")[i].ToString().Split('|')[0];
                newDataSub.Procate_Name = Request.Form.GetValues("iProcate[]")[i].ToString().Split('|')[1];
                newDataSub.Budget_Number = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[0];
                newDataSub.Budget_Desc = Request.Form.GetValues("iBudgetNumber[]")[i].ToString().Split('|')[1];
                newDataSub.Description = Request.Form.GetValues("iAllocationDesc[]")[i];
                db.FA_Payment_Request_Non_Payment_Sub.Add(newDataSub);
            }
            db.SaveChanges();

            //var newHistory = new FA_Payment_Request_History();
            //newHistory.Document_Type = "NonPO";
            //newHistory.Document_Id = newData.id;
            //newHistory.Message = "Created";
            //newHistory.User_Id = currUserID;
            //newHistory.Date_At = DateTime.Now;
            //db.FA_Payment_Request_History.Add(newHistory);
            //db.SaveChanges();

            // return RedirectToAction("NonPO", "PaymentRequest", new { area = "FA" });
            return Redirect(Request.UrlReferrer.ToString());
        }
        public ActionResult APNonPOEdit()
        {

            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPaymentID = Int32.Parse(Request["iPaymentID"]);
            IFormatProvider culture = new CultureInfo("en-US", true);

            var updateData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currPaymentID).FirstOrDefault();
            updateData.Due_Date = DateTime.ParseExact(Request["iEntryDueDate"], "MM/dd/yyyy", culture);
            updateData.Due_Date_Reason = Request["iDueDateReason"];
            updateData.Payment_Method = Request["iPaymentMethod"];
            if (Request["iWHTAmount"] != "")
            {
                updateData.WHT_COA_Code = Request["iWHTCode"].ToString().Split('|')[0];
                updateData.WHT_COA_Name = Request["iWHTCode"].ToString().Split('|')[1];
                updateData.WHT_Amount = double.Parse(Request["iWHTAmount"], CultureInfo.InvariantCulture);
            }
            else
            {
                updateData.WHT_COA_Code = null;
                updateData.WHT_COA_Name = null;
                updateData.WHT_Amount = null;
            }
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = "FA - Edit";
            if (Convert.ToDateTime(updateData.Due_Date) != Convert.ToDateTime(Request["iEntryDueDate"]))
            {
                newHistory.Note += ",Due Date:" + DateTime.ParseExact(Request["iEntryDueDate"], "MM/dd/yyyy", culture);
            }
            if (Request["iDueDateReason"].Length > 0 && updateData.Due_Date_Reason != Request["iDueDateReason"])
            {
                newHistory.Note += ",Due Date Reason:" + Request["iDueDateReason"];
            }
            if (updateData.Payment_Method != Request["iPaymentMethod"])
            {
                newHistory.Note += ",Payment Method:" + Request["iPaymentMethod"];
            }
            if (updateData.WHT_COA_Code != Request["iWHTCode"].ToString().Split('|')[0])
            {
                newHistory.Note += ",WHT Code:" + updateData.WHT_COA_Code + "|WHT Name:" + updateData.WHT_COA_Name;
            }
            if (Request["iWHTAmount"] != "" && updateData.WHT_Amount != double.Parse(Request["iWHTAmount"], CultureInfo.InvariantCulture))
            {
                newHistory.Note += ",WHT Amount:" + updateData.WHT_Amount;
            }
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("APNonPO", "PaymentRequest", new { area = "FA" });
        }
        public ActionResult APWithPOEdit()
        {

            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currPaymentID = Int32.Parse(Request["iPaymentID"]);
            IFormatProvider culture = new CultureInfo("en-US", true);

            var updateData = db.FA_Payment_Request_PO.Where(w => w.id == currPaymentID).FirstOrDefault();

            updateData.Due_Date = DateTime.ParseExact(Request["iEntryDueDate"], "MM/dd/yyyy", culture);
            updateData.Due_Date_Reason = Request["iDueDateReason"];
            if (Request["iWHTAmount"] != "")
            {
                updateData.WHT_COA_Code = Request["iWHTCode"].ToString().Split('|')[0];
                updateData.WHT_COA_Name = Request["iWHTCode"].ToString().Split('|')[1];
                updateData.WHT_Amount = double.Parse(Request["iWHTAmount"], CultureInfo.InvariantCulture);
                updateData.WHT_Description = Request["iWHTDescription"];
            }
            else
            {
                updateData.WHT_COA_Code = null;
                updateData.WHT_COA_Name = null;
                updateData.WHT_Amount = null;
                updateData.WHT_Description = null;
            }
            db.SaveChanges();

            var newHistory = new FA_Payment_Request_History();
            newHistory.Document_Type = "WithPO";
            newHistory.Document_Id = updateData.id;
            newHistory.Message = "FA - Edit";
            if (Convert.ToDateTime(updateData.Due_Date) != Convert.ToDateTime(Request["iEntryDueDate"]))
            {
                newHistory.Note += ",Due Date:" + DateTime.ParseExact(Request["iEntryDueDate"], "MM/dd/yyyy", culture);
            }
            if (updateData.Due_Date_Reason != Request["iDueDateReason"])
            {
                newHistory.Note += ",Due Date Reason:" + Request["iDueDateReason"];
            }
            if (updateData.WHT_COA_Code != Request["iWHTCode"].ToString().Split('|')[0])
            {
                newHistory.Note += ",WHT Code:" + updateData.WHT_COA_Code + "|WHT Name:" + updateData.WHT_COA_Name;
            }
            if (Request["iWHTAmount"] != "" && updateData.WHT_Amount != double.Parse(Request["iWHTAmount"], CultureInfo.InvariantCulture))
            {
                newHistory.Note += ",WHT Amount:" + updateData.WHT_Amount;
            }
            newHistory.User_Id = currUserID;
            newHistory.Date_At = DateTime.Now;
            db.FA_Payment_Request_History.Add(newHistory);
            db.SaveChanges();

            return RedirectToAction("APWithPO", "PaymentRequest", new { area = "FA" });
        }
        public JsonResult NonPODelete()
        {
            var currID = Int32.Parse(Request["iNonPOID"]);
            var invoice = Request["InvoiceNumber"];
            var deleteData = db.FA_Payment_Request_Non_PO.Where(w => w.id == currID).First();
            db.FA_Payment_Request_Non_PO.Remove(deleteData);
            var deleteDataSub = db.FA_Payment_Request_Non_PO_Sub.Where(w => w.Non_PO_ID == currID).ToList();
            db.FA_Payment_Request_Non_PO_Sub.RemoveRange(deleteDataSub);
            db.SaveChanges();
            if (!string.IsNullOrWhiteSpace(invoice) && invoice.ToUpper().Contains("/KOPKAR/"))
            {
                var requestsToUpdate = dbm.HC_GA_Inventory_Requests
                                          .Where(r => r.Invoice_Number == invoice)
                                          .ToList();

                if (requestsToUpdate.Any())
                {
                    foreach (var req in requestsToUpdate)
                    {
                        req.Receive_Number = null; 
                    }

                    dbm.SaveChanges(); 
                }
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult NonPaymentDelete()
        {
            var currID = Int32.Parse(Request["iNonPOID"]);
            var deleteData = db.FA_Payment_Request_Non_Payment.Where(w => w.id == currID).First();
            db.FA_Payment_Request_Non_Payment.Remove(deleteData);
            var deleteDataSub = db.FA_Payment_Request_Non_Payment_Sub.Where(w => w.Non_PO_ID == currID).ToList();
            db.FA_Payment_Request_Non_Payment_Sub.RemoveRange(deleteDataSub);
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public static string ApprovalStatus(int? Approval = 1, int? SubApproval = 0, bool? Is_Reject = false, double? Amount_of_Invoice = 0)
        {
            var status = "Dept - Registered";
            switch (Approval)
            {
                case 0:
                    status = "Dept - Returned";
                    break;
                case 1:
                    status = "Dept - Created";
                    break;
                case 2:
                    status = "Dept - Created";
                    break;
                case 3:
                    switch (SubApproval)
                    {
                        case 1:
                            status = "Dept - Approved";
                            break;
                        case 2:
                            status = "Dept - Approved";
                            //if (Amount_of_Invoice > 1000000)
                            //{
                            //    status = "Director - Approved";
                            //}
                            break;
                        case 3:
                            status = "HR - Checked";
                            break;
                        case 4:
                            status = "HR - Approved";
                            break;
                        default:
                            status = "Dept - Checked";
                            break;
                    }
                    break;
                case 4:
                    switch (SubApproval)
                    {
                        case 1:
                            status = "HR - Approved";
                            break;
                        case 2:
                            status = "FA - Received";
                            break;
                        default:
                            status = "Dept - Approved";
                            break;
                    }
                    break;
                case 5:
                    switch (SubApproval)
                    {
                        case 1:
                            status = "Treasury - Received";
                            break;
                        default:
                            status = "FA - Approved";
                            break;
                    }
                    break;
                case 6:
                    switch (SubApproval)
                    {
                        case 1:
                            status = "FA - Paid";
                            break;
                        default:
                            status = "FA - Paid";
                            break;
                    }
                    break;
            }
            if (Is_Reject == true)
            {
                status = "Dept - Rejected";
                if (SubApproval == 2)
                {
                    status = "Director - Rejected";
                }
            }

            return status;
        }
        [HttpPost]
        public ActionResult SendReminderEmail()
        {
            try
            {
                sendNotification("Reminder");
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Email Reminder berhasil dikirim!" }, JsonRequestBehavior.AllowGet);
                }
                TempData["successMessage"] = "Email Reminder berhasil dikirim!";
            }
            catch (Exception ex)
            {
                try { Elmah.ErrorSignal.FromCurrentContext()?.Raise(ex); } catch { }
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Gagal mengirim email: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
                TempData["errorMessage"] = "Gagal mengirim email: " + ex.Message;
            }

            return RedirectToAction("APNonPO", "PaymentRequest", new { area = "FA" });
        }

        [HttpPost]
        public ActionResult SendInfoEmail()
        {
            try
            {
                sendNotification("Info");
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Email Info berhasil dikirim!" }, JsonRequestBehavior.AllowGet);
                }
                TempData["successMessage"] = "Email Info berhasil dikirim!";
            }
            catch (Exception ex)
            {
                try { Elmah.ErrorSignal.FromCurrentContext()?.Raise(ex); } catch { }
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Gagal mengirim email: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
                TempData["errorMessage"] = "Gagal mengirim email: " + ex.Message;
            }

            return RedirectToAction("APNonPO", "PaymentRequest", new { area = "FA" });
        }

        public void sendNotification(string type)
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            string FilePath = "";
            if (type == "Reminder")
            {
                FilePath = Path.Combine(Server.MapPath("~/Emails/FA/E-Voucher/"), "ReminderEmail.html");
            }
            else
            {
                FilePath = Path.Combine(Server.MapPath("~/Emails/FA/E-Voucher/"), "InfoEmail.html");
            }
            
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();
            var loggedUser = db.Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            var nonPOID = int.Parse(Request["iNonPOID"]);
            var eVoucher = db.FA_Payment_Request_Non_PO.Where(w => w.id == nonPOID).FirstOrDefault();
            double TotalAllocation = db.FA_Payment_Request_Non_PO_Sub
                .Where(w => w.Non_PO_ID == nonPOID)
                .Sum(s => (double?)s.Allocation_Amount) ?? 0d;

            var traveller = eVoucher.Third_Party_Name;
            double? amount = null;
            double invoice = eVoucher?.Amount_of_Invoice ?? 0d;
            double totalAlloc = TotalAllocation ;
            if (type == "Reminder")
            {
                amount = invoice - totalAlloc;
            }
            else
            {
                amount = invoice;
            }

            var description = "";
            if (type == "Reminder")
            {
                description = Request["iNonPOReminderDescription"];
            } else
            {
                description = Request["iNonPOInfoDescription"];
            }
            
            var duedate = eVoucher.Due_Date != null ? Convert.ToDateTime(eVoucher.Due_Date).ToString("dd-MM-yyyy") : "-";

            string[] emailListTo;
            string[] emailListCC;

            if (type == "Reminder")
            {
                emailListTo = Request["iNonPOReminderEmailListTo"]?.Split(',');
                emailListCC = Request["iNonPOReminderEmailListCC"]?.Split(',');
            } else
            {
                emailListTo = Request["iNonPOInfoEmailListTo"]?.Split(',');
                emailListCC = Request["iNonPOInfoEmailListCC"]?.Split(',');
            }
                
            var currURL = Url.Action("NonPOView", "PaymentRequest", new { area = "FA", EVoucher = eVoucher.Receive_Number }, this.Request.Url.Scheme);

            if (type == "Reminder")
            {
                MailText = MailText.Replace("##traveller##", traveller);
                MailText = MailText.Replace("##amount##", amount?.ToString("N0"));
                MailText = MailText.Replace("##link##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
                MailText = MailText.Replace("##description##", description);
                MailText = MailText.Replace("##guid##", Guid.NewGuid().ToString());
            }
            else
            {
                MailText = MailText.Replace("##traveller##", traveller);
                MailText = MailText.Replace("##duedate##", duedate);
                MailText = MailText.Replace("##amount##", amount?.ToString("N0"));
                MailText = MailText.Replace("##link##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
                MailText = MailText.Replace("##description##", description);
                MailText = MailText.Replace("##guid##", Guid.NewGuid().ToString());
            }
            

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
            var password = "100%NGKbusi!";
            var sub = "";
            if(type == "Reminder")
            {
                sub = "[Niterra-Portal-Notification] E-Voucher - " + eVoucher.Receive_Number + " - Refund Settlement";
            } else
            {
                sub = "[Niterra-Portal-Notification] E-Voucher Info - " + eVoucher.Receive_Number;
            }
               
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;

            var body = MailText;
            var smtp = new SmtpClient
            {
                Host = "mail.ngkbusi.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 180000,
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
                if (emailListTo != null)
                {
                    foreach (var dataEmail in emailListTo)
                    {
                        var emailTrimmed = dataEmail?.Trim();
                        if (!string.IsNullOrEmpty(emailTrimmed))
                        {
                            try { mess.To.Add(new MailAddress(emailTrimmed)); } catch { }
                        }
                    }
                }
                if (emailListCC != null)
                {
                    foreach (var dataEmail in emailListCC)
                    {
                        var emailTrimmed = dataEmail?.Trim();
                        if (!string.IsNullOrEmpty(emailTrimmed))
                        {
                            try { mess.CC.Add(new MailAddress(emailTrimmed)); } catch { }
                        }
                    }
                }
                if (type == "Reminder")
                {
                    mess.CC.Add(new MailAddress("edy.surya@niterragroup.com"));
                    mess.CC.Add(new MailAddress("ian.yustiar@niterragroup.com"));
                    mess.CC.Add(new MailAddress("ria.pratiwi@niterragroup.com"));
                    mess.CC.Add(new MailAddress("widy.indrayanto@niterragroup.com"));
                }
                if (loggedUser != null && !string.IsNullOrWhiteSpace(loggedUser.Email))
                {
                    try { mess.CC.Add(new MailAddress(loggedUser.Email.Trim())); } catch { }
                }
                mess.Bcc.Add(new MailAddress("azis.abdillah@niterragroup.com"));

                smtp.Send(mess);
            }
        }
    }
}