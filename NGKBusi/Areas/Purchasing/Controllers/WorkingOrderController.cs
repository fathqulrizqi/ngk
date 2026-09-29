using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NGKBusi.Areas.Purchasing.Models;
using NGKBusi.Areas.WebService.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.PeerToPeer;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Windows.Interop;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace NGKBusi.Areas.Purchasing.Controllers
{
    public class WorkingOrderController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        WOConnection dbm = new WOConnection();

        public ActionResult Index()
        {
            var currUserId = User.Identity.GetUserId();
            var now = DateTime.Now.Date;
            string date = now.ToString("dd-MM-yyyy");

            var user = db.Users.FirstOrDefault(u => u.NIK == currUserId);
            ViewBag.vendorList = db.AX_Vendor_List.Where(w => w.IsActive == true && w.VENDGROUP != "OTH").ToList();
            var allowedTypes = new[] { "BEX", "BEL", "BIP" };
            ViewBag.budgetList = db.V_FA_Payment_Request_Budget_List
                .Where(w => w.Period_FY == "FY126" && allowedTypes.Contains(w.Budget_Type))
                .Select(w => new { w.Budget_No, w.Description })
                .ToList();
            ViewBag.currUsr = currUserId;
            ViewBag.currDate = date;
            ViewBag.currUsrName = user?.Name ?? "";

            return View();
        }



        [HttpGet]
        public JsonResult GetDatas()
        {
            var datas = dbm.Purchasing_WorkingOrder_List
                .AsEnumerable()
                .Select(data => new
                {
                    data.ID,
                    Date = data.Date.ToString("yyyy-MM-dd"),
                    data.Number,
                    data.Vendor,
                    data.VendorName,
                    data.Subject,
                    data.NIK,
                    data.NIKName,
                }).ToList();

            return Json(new { datas = datas }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDataById(int id)
        {

            var datas = dbm.Purchasing_WorkingOrder_List
                .Where(w => w.ID == id)
                .AsEnumerable()
                .Select(data => new
                {
                    data.ID,
                    Date = data.Date.ToString("yyyy-MM-dd"),
                    data.Number,
                    data.Vendor,
                    data.VendorName,
                    data.Subject,
                    data.NIK,
                    data.NIKName,
                    Timestamps = data.Timestamps.ToString("yyyy-MM-dd HH:mm:ss"),
                }).FirstOrDefault();

            return Json(new { datas }, JsonRequestBehavior.AllowGet);
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
        public JsonResult CreateData()
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            string sectionID = _currUser.FindFirstValue("sectID");

            if (sectionID != "25")
            {
                return Json(new { success = false, message = "You are not allowed to make Working Order" }, JsonRequestBehavior.AllowGet );
            }

            try
            {
                // Parse date
                DateTime date;
                if (!ParseDateFromRequest("Date", out date))
                {
                    return Json(new { success = false, message = "Invalid date format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
                }

                // Get user details
                var userNik = Request["NIK"];
                var un = db.Users.FirstOrDefault(w => w.NIK == userNik);
                var username = un != null ? un.Name : "";

                // Get vendor details
                var vendor = Request["selVendorList"];
                var vn = db.AX_Vendor_List.FirstOrDefault(w => w.ACCOUNTNUM == vendor);
                var vendorname = vn != null ? vn.Name : "";

                var subject = Request["Subject"];

                // Get year and month
                var year = date.ToString("yyyy");
                var month = date.ToString("MM");
                var romanMonth = toRomawi(month);

                // Get last number for current year - FIXED QUERY
                var lastNumber = dbm.Purchasing_WorkingOrder_List
                    .Where(w => w.Number != null &&
                               w.Number.Contains("/Niterra/") &&
                               w.Number.EndsWith(year))
                    .ToList() // Execute query here
                    .OrderByDescending(o => o.Number)
                    .Select(s => s.Number)
                    .FirstOrDefault();

                // Generate sequence number
                string sequenceNumber;
                if (string.IsNullOrEmpty(lastNumber))
                {
                    sequenceNumber = "0001";
                }
                else
                {
                    try
                    {
                        var currentNumber = lastNumber.Split('/')[0];
                        sequenceNumber = (int.Parse(currentNumber) + 1).ToString("D4");
                    }
                    catch
                    {
                        sequenceNumber = "0001";
                    }
                }

                var newNumber = $"{sequenceNumber}/WO/Niterra/{romanMonth}/{year}";

                // Create new Working Order
                Purchasing_WorkingOrder_List data = new Purchasing_WorkingOrder_List
                {
                    Date = date,
                    Number = newNumber,
                    Vendor = vendor,
                    VendorName = vendorname,
                    Subject = subject,
                    NIK = userNik,
                    NIKName = username,
                    Timestamps = DateTime.Now
                };

                dbm.Purchasing_WorkingOrder_List.Add(data);
                int addWO = dbm.SaveChanges();

                if (addWO > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Working Order Added Successfully",
                        number = newNumber
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to Add Working Order"
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while creating Working Order: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


        private bool ParseDateFromRequest(string key, out DateTime date)
        {
            date = default(DateTime);
            string dateStr = Request[key];
            string format = "dd-MM-yyyy";

            return DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }


        [HttpPost]
        public JsonResult UpdateData(int id)
        {
            var dateStr = Request["Date"];

            var userNik = Request["NIK"];
            var un = db.Users.FirstOrDefault(w => w.NIK == userNik);
            var username = un != null ? un.Name : "";

            var vendor = Request["selVendorList"];
            var vn = db.AX_Vendor_List.FirstOrDefault(w => w.ACCOUNTNUM == vendor);
            var vendorname = vn != null ? vn.Name : "";

            var subject = Request["Subject"];
            var number = Request["Number"];
            var timestamps = Request["Timestamps"];

            DateTime date;
            if (!DateTime.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return Json(new { success = false, message = "Invalid date format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
            }
            var data = dbm.Purchasing_WorkingOrder_List.First(w => w.ID == id);

            data.Timestamps = DateTime.Now;
            data.ID = id;
            data.Date = date;
            data.Vendor = vendor;
            data.VendorName = vendorname;
            data.Subject = subject;
            data.NIK = userNik;
            data.NIKName = username;


            int updateWO = dbm.SaveChanges();

            if (updateWO > 0)
            {
                return Json(new { success = true, message = "Working Order Updated Successfully" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "Failed Update Working Order" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Detail(int id)
        {

            var currUserName = User.Identity.GetUserName();
            var currUserId = User.Identity.GetUserId();

            var now = DateTime.Now;
            string dateX = now.ToString("dd-MM-yyyy");

            var user = db.Users.Where(w => w.NIK == currUserId).FirstOrDefault();
            var name = user?.Name?.Split(' ').FirstOrDefault() ?? "";

            var allowedTypes = new[] { "BEX", "BEL", "BIP" };
            var periodFY = "FY1" + DateTime.Now.ToString("yy");
            var periodFYNext = "FY1" + DateTime.Now.AddYears(1).ToString("yy");

            ViewBag.budgetList = db.V_FA_Payment_Request_Budget_List
      .Where(w =>
          (w.Period_FY == periodFY || w.Period_FY == periodFYNext)
          && allowedTypes.Contains(w.Budget_Type)
      )
      .ToList();
            ViewBag.username = name;
            ViewBag.userNIK = currUserId;
            ViewBag.Now = now;
            ViewBag.Date = dateX;
            ViewBag.Name = name;

            return View();
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult SaveLetter()
        {
            var idlist = Int32.Parse(Request["IDList"]);
            string number = Request["Number"];
            var vendor = Request["Vendor"];
            var attn = Request["Attn"];
            var refs = Request["Ref"];
            var project = Request["Project"];
            var html = Request["Html"];
            var prepared = Request["Prepared"];
            var preparedname = Request["PreparedName"];
            var startdate = Request["StartDate"];
            var enddate = Request["EndDate"];
            var currUserId = User.Identity.GetUserId();

            var priceRowsJson = Request["PriceRowsJson"];
            var priceRows = new List<Purchasing_WorkingOrder_Price>();
            if (!string.IsNullOrEmpty(priceRowsJson))
            {
                priceRows = JsonConvert.DeserializeObject<List<Purchasing_WorkingOrder_Price>>(priceRowsJson);
            }

            var userexist = dbm.Purchasing_WorkingOrder_List.Where(w => w.ID == idlist).FirstOrDefault();
            var userNIK = userexist?.NIK;
            if (prepared != userNIK)
            {
                return Json(new { success = false, message = "You Are Not Authorize to Save Letter." }, JsonRequestBehavior.AllowGet);
            }

            DateTime date;
            if (!ParseDateFromRequest("Date", out date))
            {
                return Json(new { success = false, message = "Invalid date format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
            }

            decimal? total = null;
            var totalRequestValue = Request["Total"];
            if (!string.IsNullOrEmpty(totalRequestValue))
            {
                totalRequestValue = totalRequestValue.Replace(',', '.');
                if (!decimal.TryParse(totalRequestValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedTotal))
                {
                    return Json(new { success = false, message = "Invalid total format. Please input a valid number." }, JsonRequestBehavior.AllowGet);
                }
                total = parsedTotal;
            }

            var paymentterm = Request["PaymentTerm"];
            var budgetNos = Request["BudgetNo"];
            var budgetDescriptions = new List<string>();

            if (!string.IsNullOrEmpty(budgetNos))
            {
                var budgetNoArray = budgetNos.Split(',').Select(b => b.Trim()).ToList();
                foreach (var budgetNo in budgetNoArray)
                {
                    if (budgetNo == "UNB")
                    {
                        budgetDescriptions.Add("Unbudget");
                        continue;
                    }
                    var budget = db.V_FA_BudgetSystem_BEX_BEL.FirstOrDefault(w => w.Budget_No == budgetNo);
                    budgetDescriptions.Add(budget != null ? budget.Description : "");
                }
            }

            var combinedBudgetDesc = string.Join(", ", budgetDescriptions);
            var remark = Request["Remark"];

            var existing = dbm.Purchasing_WorkingOrder_Letter.FirstOrDefault(w => w.IDList == idlist);
            if (existing != null)
            {
                return Json(new { success = false, message = "Data surat sudah pernah disimpan. Silakan gunakan fitur update." }, JsonRequestBehavior.AllowGet);
            }

            Purchasing_WorkingOrder_Letter data = new Purchasing_WorkingOrder_Letter
            {
                IDList = idlist,
                Number = number,
                Vendor = vendor,
                Attn = attn,
                Ref = refs,
                Project = project,
                Html = html,
                Date = date,
                Total = total,
                PaymentTerm = paymentterm,
                StartDate = startdate,
                EndDate = enddate,
                BudgetNo = budgetNos,
                BudgetDesc = combinedBudgetDesc,
                Remark = remark,
                NIKPrepared = prepared,
                NamePrepared = preparedname,
                Timestamps = DateTime.Now,
            };

            dbm.Purchasing_WorkingOrder_Letter.Add(data);

            if (priceRows != null && priceRows.Count > 0)
            {
                foreach (var item in priceRows)
                {
                    item.WO_Number = number; 
                    item.Timestamps = DateTime.Now;
                    dbm.Purchasing_WorkingOrder_Price.Add(item);
                }
            }

            int saveWO = dbm.SaveChanges();

            if (saveWO > 0)
            {
                return Json(new { success = true, message = "Working Order Saved Successfully", id = data.IDList }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "Failed to Save Working Order" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult UpdateLetter(int id)
        {
            string idListStr = Request["IDList"];
            var prepared = Request["Prepared"];
            int idlist;
            if (!int.TryParse(idListStr, out idlist))
            {
                return Json(new { success = false, message = "Invalid IDList format" }, JsonRequestBehavior.AllowGet);
            }
            string number = Request["Number"];
            var vendor = Request["Vendor"];
            var attn = Request["Attn"];
            var refs = Request["Ref"];
            var project = Request["Project"];
            var html = Request["Html"];
            var startdate = Request["StartDate"];
            var enddate = Request["EndDate"];

            DateTime timestamps;
            if (!DateTime.TryParseExact(Request["Timestamps"], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamps))
            {
                return Json(new { success = false, message = "Invalid Timestamps format. Please use 'yyyy-MM-dd HH:mm:ss'." }, JsonRequestBehavior.AllowGet);
            }

            DateTime date;
            if (!ParseDateFromRequest("Date", out date))
            {
                return Json(new { success = false, message = "Invalid date format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
            }

            // === AMBIL DATA PRICE TABLE DARI REQUEST ===
            var priceRowsJson = Request["PriceRowsJson"];
            var priceRows = new List<Purchasing_WorkingOrder_Price>();
            if (!string.IsNullOrEmpty(priceRowsJson))
            {
                priceRows = JsonConvert.DeserializeObject<List<Purchasing_WorkingOrder_Price>>(priceRowsJson);
            }
            // ===========================================

            decimal? total = null;
            var totalRequestValue = Request["Total"];
            if (!string.IsNullOrEmpty(totalRequestValue))
            {
                totalRequestValue = totalRequestValue.Replace(',', '.');
                if (!decimal.TryParse(totalRequestValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedTotal))
                {
                    return Json(new { success = false, message = "Invalid total format. Please input a valid number." }, JsonRequestBehavior.AllowGet);
                }
                total = parsedTotal;
            }

            var paymentterm = Request["PaymentTerm"];

            var exist = dbm.Purchasing_WorkingOrder_Letter.Where(w => w.ID == id).FirstOrDefault();
            var existnik = exist?.NIKPrepared;
            if (existnik != prepared)
            {
                return Json(new { success = false, message = "You Are Not Authorize to Update Letter." }, JsonRequestBehavior.AllowGet);
            }
            var budgetNos = Request["BudgetNo"];
            var budgetDescriptions = new List<string>();

            if (!string.IsNullOrEmpty(budgetNos))
            {
                var budgetNoArray = budgetNos.Split(',').Select(b => b.Trim()).ToList();
                foreach (var budgetNo in budgetNoArray)
                {
                    if (budgetNo == "UNB")
                    {
                        budgetDescriptions.Add("Unbudget");
                        continue;
                    }
                    var budget = db.V_FA_BudgetSystem_BEX_BEL.FirstOrDefault(w => w.Budget_No == budgetNo);
                    budgetDescriptions.Add(budget != null ? budget.Description : "");
                }
            }

            var combinedBudgetDesc = string.Join(", ", budgetDescriptions);
            var remark = Request["Remark"];

            // 1. Update Data Utama (Header)
            var data = dbm.Purchasing_WorkingOrder_Letter.First(w => w.ID == id);
            data.IDList = idlist;
            data.Number = number;
            data.Vendor = vendor;
            data.Attn = attn;
            data.Ref = refs;
            data.Project = project;
            data.Html = html;
            data.Date = date;
            data.Total = total;
            data.PaymentTerm = paymentterm;
            data.StartDate = startdate;
            data.EndDate = enddate;
            data.BudgetNo = budgetNos;
            data.BudgetDesc = combinedBudgetDesc;
            data.Remark = remark;
            data.Timestamps = timestamps;

            // 2. Bersihkan Data Baris Harga Lama (Wipe & Re-insert)
            var oldPrices = dbm.Purchasing_WorkingOrder_Price.Where(w => w.WO_Number == number).ToList();
            if (oldPrices.Any())
            {
                dbm.Purchasing_WorkingOrder_Price.RemoveRange(oldPrices);
            }

            // 3. Masukkan Data Baris Harga yang Baru
            if (priceRows != null && priceRows.Count > 0)
            {
                foreach (var item in priceRows)
                {
                    item.WO_Number = number;
                    item.Timestamps = DateTime.Now;
                    dbm.Purchasing_WorkingOrder_Price.Add(item);
                }
            }

            int updateWO = dbm.SaveChanges();
            if (updateWO > 0)
            {
                return Json(new { success = true, message = "Working Order Updated Successfully" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "Failed Update Working Order" }, JsonRequestBehavior.AllowGet);
            }
        }

        private bool ParseDecimalFromRequest(string key, out decimal value)
        {
            value = 0;  
            string valStr = Request[key];

            if (!string.IsNullOrWhiteSpace(valStr))
            {
                valStr = valStr.Replace(",", "").Replace("Rp", "").Trim();
            }

            return decimal.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        [HttpGet]
        public JsonResult GetMenuRoles(int id)
        {
            try
            {
                var data = dbm.Purchasing_WorkingOrder_List
                    .Where(x => x.ID == id)
                    .Select(x => new
                    {
                        x.ID,
                        x.Number,
                        x.Vendor,
                        x.VendorName,
                        x.NIKName,
                        x.NIK,
                        x.Timestamps
                    })
                    .FirstOrDefault();

                if (data == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Data not found"
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    datas = data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetLetterById(int id)
        {
            var data = dbm.Purchasing_WorkingOrder_Letter.Where(w => w.IDList == id).FirstOrDefault();
            if (data == null)
            {
                return Json(new
                {
                    success = true,
                    message = "Detail Data Empty",
                }, JsonRequestBehavior.AllowGet);
            }

            // Ambil Vendor Code dari Purchasing_WorkingOrder_List
            var woList = dbm.Purchasing_WorkingOrder_List.FirstOrDefault(w => w.ID == id);
            var vendorCode = woList != null ? woList.Vendor : data.Vendor; // Ambil kode seperti 'E0687'

            var budget = db.V_FA_BudgetSystem_BEX_BEL.FirstOrDefault(w => w.Budget_No == data.BudgetNo);

            var priceRows = dbm.Purchasing_WorkingOrder_Price
                               .Where(w => w.WO_Number == data.Number)
                               .Select(p => new
                               {
                                   p.ID,
                                   p.Price_Before,
                                   p.Discount,
                                   p.Price_Total
                               })
                               .ToList();

            return Json(new
            {
                success = true,
                data = new
                {
                    data.ID,
                    data.IDList,
                    data.Number,
                    data.Vendor,         // Ini kode Vendor (misal: E0687)
                    VendorCode = vendorCode, // Properti eksplisit untuk Kode Vendor
                    data.Attn,
                    data.Ref,
                    data.Project,
                    data.Html,
                    data.Date,
                    data.Total,
                    data.PaymentTerm,
                    data.StartDate,
                    data.EndDate,
                    data.BudgetNo,
                    BudgetDesc = budget != null ? budget.Description : "",
                    data.Remark,
                    Timestamps = data.Timestamps.ToString("yyyy-MM-dd HH:mm:ss"),
                    data.NIKApproval,
                    data.DateApproval,
                    data.DatePrepared,
                    data.NIKPrepared,
                    data.NamePrepared,
                    PriceRows = priceRows
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult SaveApproval(int id)
        {
            try
            {
                var attn = Request["Attn"];
                var refs = Request["Ref"];
                var project = Request["Project"];
                var html = Request["Html"];
                var startdate = Request["StartDate"];
                var enddate = Request["EndDate"];
                var currUserId = User.Identity.GetUserId();

                var priceRowsJson = Request["PriceRowsJson"];
                var priceRows = new List<Purchasing_WorkingOrder_Price>();
                if (!string.IsNullOrEmpty(priceRowsJson))
                {
                    priceRows = JsonConvert.DeserializeObject<List<Purchasing_WorkingOrder_Price>>(priceRowsJson);
                }

                DateTime date;
                if (!ParseDateFromRequest("Date", out date))
                {
                    return Json(new { success = false, message = "Invalid date format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
                }

                decimal? total = null;
                var totalRequestValue = Request["Total"];
                if (!string.IsNullOrEmpty(totalRequestValue))
                {
                    totalRequestValue = totalRequestValue.Replace(',', '.');
                    if (!decimal.TryParse(totalRequestValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedTotal))
                    {
                        return Json(new { success = false, message = "Invalid total format. Please input a valid number." }, JsonRequestBehavior.AllowGet);
                    }
                    total = parsedTotal;
                }

                var paymentterm = Request["PaymentTerm"];


                //DateTime? startdate = null;
                //var startDateRequestValue = Request["StartDate"];
                //if (!string.IsNullOrEmpty(startDateRequestValue))
                //{
                //    if (!ParseDateFromRequest("StartDate", out DateTime parsedStartDate))
                //    {
                //        return Json(new { success = false, message = "Invalid date start format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
                //    }
                //    startdate = parsedStartDate;
                //}

                //DateTime? enddate = null;
                //var endDateRequestValue = Request["EndDate"];
                //if (!string.IsNullOrEmpty(endDateRequestValue))
                //{
                //    if (!ParseDateFromRequest("EndDate", out DateTime parsedEndDate))
                //    {
                //        return Json(new { success = false, message = "Invalid date end format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
                //    }
                //    enddate = parsedEndDate;
                //}

                var budgetno = Request["BudgetNo"];
                var budgets = db.V_FA_BudgetSystem_BEX_BEL.FirstOrDefault(w => w.Budget_No == budgetno);
                var budgetdesc = budgets != null ? budgets.Description : "";
                var remark = Request["Remark"];

                var preparedname = Request["PreparedName"];
                var approved = Request["Approved"];
                var prepared = Request["Prepared"];
                DateTime? dateapproved = null;
                if (!string.IsNullOrEmpty(Request["DateApproved"]))
                {
                    DateTime tempDate;
                    if (!ParseDateFromRequest("DateApproved", out tempDate))
                    {
                        return Json(new { success = false, message = "Invalid date format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
                    }
                    dateapproved = tempDate;
                }


                var data = dbm.Purchasing_WorkingOrder_Letter.FirstOrDefault(x => x.ID == id);
                var datanik = data?.NIKPrepared;
                if (datanik != prepared)
                {
                    return Json(new { success = false, message = "You Are Not Authorize to Approve Letter." }, JsonRequestBehavior.AllowGet);
                }

                data.Attn = attn;
                data.Ref = refs;
                data.Project = project;
                data.Html = html;
                data.Date = date;
                data.Total = total;
                data.PaymentTerm = paymentterm;
                data.StartDate = startdate;
                data.EndDate = enddate;
                data.BudgetNo = budgetno;
                data.BudgetDesc = budgetdesc;
                data.Remark = remark;
                data.DatePrepared = dateapproved;
                data.NamePrepared = preparedname;
                data.NIKApproval = approved;
                data.DateApproval = dateapproved;
                data.Timestamps = DateTime.Now;

                var oldPrices = dbm.Purchasing_WorkingOrder_Price.Where(w => w.WO_Number == data.Number).ToList();
                if (oldPrices.Any())
                {
                    dbm.Purchasing_WorkingOrder_Price.RemoveRange(oldPrices);
                }

                if (priceRows != null && priceRows.Count > 0)
                {
                    foreach (var item in priceRows)
                    {
                        item.WO_Number = data.Number;
                        item.Timestamps = DateTime.Now;
                        dbm.Purchasing_WorkingOrder_Price.Add(item);
                    }
                }

                int saveWO = dbm.SaveChanges();

                if (saveWO > 0)
                {
                    return Json(new { success = true, message = "Working Order Approved Successfully", id = data.IDList }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Failed to Approve Working Order" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult UpdateApproval(int id)
        {
            try
            {
                var preparedname = Request["PreparedName"];
                var approved = Request["Approved"];
                var prepared = Request["Prepared"];
                DateTime? dateapproved = null;
                if (!string.IsNullOrEmpty(Request["DateApproved"]))
                {
                    DateTime tempDate;
                    if (!ParseDateFromRequest("DateApproved", out tempDate))
                    {
                        return Json(new { success = false, message = "Invalid date format. Please use 'dd-MM-yyyy'." }, JsonRequestBehavior.AllowGet);
                    }
                    dateapproved = tempDate;
                }


                var data = dbm.Purchasing_WorkingOrder_Letter.FirstOrDefault(x => x.ID == id);
                var datanik = data?.NIKPrepared;
                if (datanik != prepared)
                {
                    return Json(new { success = false, message = "You Are Not Authorize to Approve Letter." }, JsonRequestBehavior.AllowGet);
                }

                data.DatePrepared = dateapproved;
                data.NamePrepared = preparedname;
                data.NIKApproval = approved;
                data.DateApproval = dateapproved;
                data.Timestamps = DateTime.Now;

                int saveWO = dbm.SaveChanges();

                if (saveWO > 0)
                {
                    return Json(new { success = true, message = "Working Order Approved Successfully", id = data.IDList }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Failed to Approve Working Order" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult DownloadWorkingOrderReport()
        {
            var data = dbm.V_Purchasing_WorkingOrder_Report
                          .OrderBy(x => x.Date)
                          .ThenBy(x => x.Number)
                          .ToList();

            var woNumbers = data.Select(x => x.Number).Distinct().ToList();

            // Optimasi: Ambil data Before dan Discount sekaligus lewat GroupBy
            var priceLookup = dbm.Purchasing_WorkingOrder_Price
                                 .Where(p => woNumbers.Contains(p.WO_Number))
                                 .GroupBy(p => p.WO_Number)
                                 .ToDictionary(
                                     g => g.Key,
                                     g => new
                                     {
                                         Before = g.Sum(p => (decimal?)p.Price_Before) ?? 0,
                                         Discount = g.Sum(p => (decimal?)p.Discount) ?? 0
                                     }
                                 );

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Working Order Report");

                // --- HEADER TABEL UTAMA ---
                ws.Cell(1, 1).Value = "WO Date";
                ws.Cell(1, 2).Value = "WO Number";
                ws.Cell(1, 3).Value = "Vendor Name";
                ws.Cell(1, 4).Value = "Project";
                ws.Cell(1, 5).Value = "Budget Desc";
                ws.Cell(1, 6).Value = "Payment Term";
                ws.Cell(1, 7).Value = "Prepared By";
                ws.Cell(1, 8).Value = "Before";                  // Kolom 8
                ws.Cell(1, 9).Value = "Discount";                // Kolom 9
                ws.Cell(1, 10).Value = "Total";                  // Kolom 10
                ws.Cell(1, 11).Value = "Cost Reduction (CR)";    // Kolom 11

                // Terapkan Warna Template Biru untuk Header (Range pas sampai kolom 11)
                var mainHeaderRange = ws.Range(1, 1, 1, 11);
                mainHeaderRange.Style.Fill.BackgroundColor = XLColor.SteelBlue;
                mainHeaderRange.Style.Font.FontColor = XLColor.White;
                mainHeaderRange.Style.Font.Bold = true;
                mainHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var d in data)
                {
                    ws.Cell(row, 1).Value = d.Date.HasValue ? d.Date.Value.ToString("yyyy-MM-dd") : "";
                    ws.Cell(row, 2).Value = d.Number;
                    ws.Cell(row, 3).Value = d.VendorName;
                    ws.Cell(row, 4).Value = d.Project;
                    ws.Cell(row, 5).Value = d.BudgetDesc;
                    ws.Cell(row, 6).Value = d.PaymentTerm;
                    ws.Cell(row, 7).Value = d.NamePrepared;

                    // Tarik data agregat price dari lookup dictionary
                    decimal totalBefore = 0;
                    decimal totalDiscount = 0;

                    if (priceLookup.TryGetValue(d.Number, out var prices))
                    {
                        totalBefore = prices.Before;
                        totalDiscount = prices.Discount;
                    }

                    // Kolom 8: Before
                    ws.Cell(row, 8).Value = totalBefore;
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

                    // Kolom 9: Discount
                    ws.Cell(row, 9).Value = totalDiscount;
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

                    // Kolom 10: Total (Setelah Diskon)
                    decimal currentTotal = d.Total ?? 0;
                    ws.Cell(row, 10).Value = currentTotal;
                    ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

                    // Hitung Logika CR (Total Before - Current Total)
                    decimal costReduction = totalBefore - currentTotal;
                    if (costReduction < 0) costReduction = 0;

                    // Kolom 11: Cost Reduction (CR)
                    ws.Cell(row, 11).Value = costReduction;
                    ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 11).Style.Font.Bold = true;

                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"WorkingOrderReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    );
                }
            }
        }
    }
}