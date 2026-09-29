using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.SCM.Controllers
{
    public class D365ExcelFormController : Controller
    {
        // GET: SCM/D365ExcelForm
        DefaultConnection db = new DefaultConnection();
        D365ExcelFormConnection dbsi = new D365ExcelFormConnection();
        [Authorize(Roles = "AdminPurchasing,  Administrator")]

        public class test
        {
            string productRec { get; set; }
            string itemid { get; set; }
        }

        public ActionResult Index(HttpPostedFileBase uploadFile, string dateReceipt, string remark)
        {
            return View();
        }
        [HttpPost]
        
        public ActionResult ProductReceipt(HttpPostedFileBase uploadFile, string dateReceipt)
        {
            HttpPostedFileBase file = Request.Files["uploadFile"];

            if (uploadFile.ContentLength > 0)
            {
                try
                {
                    string filePath = Server.MapPath("~/Files/SCM/D365ExcelForm/");
                    //string filePath = "C:\\inetpub\\wwwroot\\NGKDev\\NGKBusi\\Files\\SCM\\D365ExcelForm\\";
                    string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                    filePath = filePath + fileName + ".xlsx";

                    uploadFile.SaveAs(filePath);

                    XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                    XLWorkbook xLWorkbook2 = new XLWorkbook(filePath);

                    //int rowCheck = 2;

                    List<SCM_D365ImporForm_ProductReceipt> ReceiptDate = new List<SCM_D365ImporForm_ProductReceipt>();

                    int row = 2;
                    int count = 0;
                    int skipRwow = 0;
                    int updateRow = 0;
                    int delRow = 0;
                    int testnumMonth = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<int>();

                    int testDiffMonth = DateTime.Now.Month - testnumMonth;
                    int differenceMonthfirst = 0;
                    int differenceYearfirst = 0;
                    // if row empty
                    bool firstRow = true;

                    string CheckMonth;
                    string CheckYear;
                    CheckMonth = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<int>().ToString();
                    CheckYear = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<int>().ToString();
                    List<SCM_D365ImporForm_ProductReceipt> listReceived = new List<SCM_D365ImporForm_ProductReceipt>();

                    var currMonth = "";
                    var currYear = "";

                    listReceived = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.Year == CheckYear && w.Month == CheckMonth && w.ConfirmReceivedStatus == 1).ToList();

                    List<int> testMonth = new List<int>();
                    List<int> testYear = new List<int>();
                    List<int> diff = new List<int>();

                    while (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString() != "")
                    {

                        string ITEMID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                        // int QTY = int.Parse(xLWorkbook.Worksheets.Worksheet(1).Cell(row, ).GetString());
                        string VendorName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();
                        string Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                        string DocumentDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                        string ProductReceipt = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();

                        int numMonth = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<int>();
                        int numyear = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<int>();

                        // set value of each field table SCM_D365ImporForm_ProductReceipt
                        SCM_D365ImporForm_ProductReceipt item = new SCM_D365ImporForm_ProductReceipt();

                        item.PurchaseOrder = ITEMID;
                        item.VendorName = VendorName;
                        item.Currency = Currency;
                        item.PurchaseType = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                        item.InvoiceAccount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                        item.OrderAccount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                        item.Date = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetDateTime();
                        if (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).IsEmpty())
                        {
                            item.DocumentDate = null;
                        }
                        else
                        {
                            item.DocumentDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetDateTime();
                        }
                        item.ProductReceipt = ProductReceipt;
                        item.Site = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetString();
                        item.Warehouse = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                        item.InternalProductReceipt = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetString();
                        item.OuterPackageNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetString();
                        item.LineNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetString();
                        item.Item = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetString();
                        item.Description = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetString();
                        item.Unit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetString();
                        item.Ordered = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetValue<decimal>();
                        item.ReceivedQuantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetValue<decimal>();
                        item.PriceUnit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetValue<decimal>();
                        item.RemainingQuantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetValue<decimal>();
                        item.Value = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetValue<decimal>();
                        item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                        item.DescriptionPurchaseReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();
                        item.LocalImport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetString();
                        item.ProCateName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetString();
                        item.SearchNameAll = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetString();
                        item.SearchNameReleased = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 28).GetString();
                        item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 29).GetString();
                        item.Year = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<int>().ToString();
                        item.Month = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<int>().ToString();
                        item.Factor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 32).GetValue<decimal>();
                        item.Denominator = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 33).GetValue<int>();
                        item.Numerator = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 34).GetValue<int>();
                        item.QtyConversion = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 35).GetValue<decimal>();
                        item.UnitPrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 36).GetValue<decimal>();
                        item.RelUnRel = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 37).GetString();
                        item.ExchangeRate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 38).GetValue<decimal>();
                        item.CreatedBy = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetString();
                        item.CreatedDateTime = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 40).GetDateTime();
                        item.DiscAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 41).GetValue<decimal>();
                        item.DiscPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 42).GetValue<decimal>();
                        item.CostLedgerVoucher = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 43).GetString();
                        item.InternalInvoiceId = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 44).GetString();
                        item.LedgerVoucher = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 45).GetString();
                        item.Periode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 46).GetString();
                        item.Standard_Cost = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 47).GetValue<decimal>();
                        item.Standard_Cost_Beneran = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 48).GetValue<decimal>();
                        item.UploadTime = DateTime.Now;

                        if ((item.Unit == "kg" || item.Unit == "KG") && item.Factor > 1)
                        {
                            row++;
                            continue;
                        }

                        // if this data row is first row (row = 2)
                        if (firstRow)
                        {
                            // add nummonth to array list
                            testMonth.Add(numMonth);
                            testYear.Add(numyear);

                            // get month and year frow first row data
                            //var Month = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<int>().ToString();
                            //var Year = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<int>().ToString();

                            // validate for different month and year of this row is <= 1
                            //// 1. get difference of row month
                            differenceMonthfirst = DateTime.Now.Month - numMonth;
                            differenceYearfirst = DateTime.Now.Year - numyear;

                            // if same year between year of row and year now
                            if (differenceYearfirst == 0 || differenceYearfirst == 1)
                            {
                                // if difference mont of row and month now less than or equal 1
                                if (differenceMonthfirst <= 1 || differenceMonthfirst == -11)
                                {
                                    // else if difference month <= 1 and year now same with current row then delete and insert or update existing database 
                                    //// delete existing data with condition confirmReceivedStatus = 0 (not received sparepart stock in)
                                    var spl = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.Year == CheckYear && w.Month == CheckMonth && w.ConfirmReceivedStatus == 0).ToList();
                                    dbsi.SCM_D365ImporForm_ProductReceipt.RemoveRange(spl);
                                    var del = dbsi.SaveChanges();

                                    // count delete row 
                                    delRow++;

                                    //// cek if current row data is part of listReceived with condition field ProductReceipt, Item, and InternalProductReceipt is same
                                    if (listReceived.Any(x => x.ProductReceipt == item.ProductReceipt && x.Item == item.Item && x.InternalProductReceipt == item.InternalProductReceipt))
                                    {
                                        // if current row is part of listReceived than update InternalInvoiceID and LedgerVoucher on existing database where ProductReceipt, item and InternalProductReceipt is same with current row data
                                        var qProductReceipt = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt && w.Item == item.Item && w.InternalProductReceipt == item.InternalProductReceipt).FirstOrDefault();
                                        qProductReceipt.InternalInvoiceId = item.InternalInvoiceId;
                                        qProductReceipt.LedgerVoucher = item.LedgerVoucher;

                                        // save update data
                                        dbsi.SaveChanges();

                                        //count updat row
                                        updateRow++;
                                    }
                                    else
                                    {
                                        ////// if current row data is not part of ListReceived than insert to table SCM_D365ImporForm_ProductReceipt
                                        dbsi.SCM_D365ImporForm_ProductReceipt.Add(item);

                                        // save insert data
                                        var save = dbsi.SaveChanges();
                                        if (save > 0)
                                        {
                                            // count save row
                                            count++;
                                        }
                                    }
                                }
                                // else if difference month of row and mont now more than 1
                                else
                                {
                                    // skip process delete and insert or update to existing database 
                                    ////count skip row
                                    skipRwow++;
                                }

                            }
                            else
                            {
                                // if difference month > 1 and year now not same with current row then
                                // skip process delete and insert or update to existing database 
                                ////count skip row
                                skipRwow++;
                            }

                            firstRow = false;

                        }
                        // not first row data
                        else
                        {
                            // get current row month and year
                            currMonth = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<int>().ToString();
                            currYear = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<int>().ToString();

                            // get condition if current row month and year different with previous row or current month different but same year or different year
                            if ((CheckMonth != currMonth && CheckYear != currYear) || (CheckMonth != currMonth && CheckYear == currYear) || (CheckMonth == currMonth && CheckYear != currYear))
                            {

                                //// if current row month and year are different with previos row then validate to current row month is same or -1 month to datetime now

                                // add nummonth to array list
                                testMonth.Add(numMonth);
                                testYear.Add(numyear);

                                // get difference month
                                int differenceMonth = DateTime.Now.Month - numMonth;
                                int differenceYear = DateTime.Now.Year - numyear;
                                // add to list diff to debugging
                                diff.Add(differenceYear);

                                // if same year row and year now
                                if (differenceYear == 0 || differenceYear == 1)
                                {
                                    // if difference month less than or equal 1
                                    if (differenceMonth <= 1 || differenceMonth == -11)
                                    {
                                        // 1. process delete existing data which same month in current month and year row
                                        // 1.1 get list of data with month = current month row and year = current year row
                                        var spl = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.Year == currYear && w.Month == currMonth && w.ConfirmReceivedStatus == 0).ToList();
                                        dbsi.SCM_D365ImporForm_ProductReceipt.RemoveRange(spl);
                                        var del = dbsi.SaveChanges();
                                        // count delete row 
                                        delRow++;

                                        // 2. get list received data for current row month and year
                                        listReceived = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.Year == currYear && w.Month == currMonth && w.ConfirmReceivedStatus == 1).ToList();

                                        // 3. validation cek if this row is part of list received
                                        if (listReceived.Any(x => x.ProductReceipt == item.ProductReceipt && x.Item == item.Item && x.InternalProductReceipt == item.InternalProductReceipt))
                                        {
                                            // if this row is part of list received then update internalinvoiceid dan ledgervoucher to existing data
                                            //// get data product receipt where productReceitp, Item and InternalProductReceipt same wiht current row data
                                            var qProductReceipt = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt && w.Item == item.Item && w.InternalProductReceipt == item.InternalProductReceipt).FirstOrDefault();
                                            qProductReceipt.InternalInvoiceId = item.InternalInvoiceId;
                                            qProductReceipt.LedgerVoucher = item.LedgerVoucher;

                                            // save update changes
                                            dbsi.SaveChanges();
                                            //count update row
                                            updateRow++;
                                        }
                                        else
                                        {
                                            // if this row is not part of list received then insert current row data
                                            dbsi.SCM_D365ImporForm_ProductReceipt.Add(item);
                                            //// save insert data
                                            var save = dbsi.SaveChanges();
                                            if (save > 0)
                                            {
                                                // add count of insert row
                                                count++;
                                            }
                                        }
                                    }
                                    // else difference month more than 1
                                    else
                                    {
                                        // no action needed
                                        //// count skip row
                                        skipRwow++;
                                    }


                                }
                                // else if the difference > 1 skip process insert
                                else
                                {
                                    // no action needed
                                    //// count skip row
                                    skipRwow++;
                                }
                                // update checkmonth and checkyear data base on current row for validate with next row
                                CheckMonth = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<int>().ToString();
                                CheckYear = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<int>().ToString();
                            }
                            else
                            {
                                //// month of row is same with previous row 

                                // get difference month
                                int differenceMonth = DateTime.Now.Month - numMonth;
                                int differenceYear = DateTime.Now.Year - numyear;

                                // add to list diff to debugging
                                diff.Add(differenceYear);

                                // if same year row and year now
                                if (differenceYear == 0 || differenceYear == 1)
                                {
                                    // iff difference month less than or equal 1
                                    if (differenceMonth <= 1 || differenceMonth == -11)
                                    {


                                        // 2. get list received data for current row month and year
                                        listReceived = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.Year == currYear && w.Month == currMonth && w.ConfirmReceivedStatus == 1).ToList();

                                        // 3. validation cek if this row is part of list received
                                        if (listReceived.Any(x => x.ProductReceipt == item.ProductReceipt && x.Item == item.Item && x.InternalProductReceipt == item.InternalProductReceipt))
                                        {
                                            // if this row is part of list received then update internalinvoiceid dan ledgervoucher to existing data
                                            //// get data product receipt where productReceitp, Item and InternalProductReceipt same wiht current row data
                                            var qProductReceipt = dbsi.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt && w.Item == item.Item && w.InternalProductReceipt == item.InternalProductReceipt).FirstOrDefault();
                                            qProductReceipt.InternalInvoiceId = item.InternalInvoiceId;
                                            qProductReceipt.LedgerVoucher = item.LedgerVoucher;

                                            // save update changes
                                            dbsi.SaveChanges();
                                            //count update row
                                            updateRow++;
                                        }
                                        else
                                        {
                                            // if this row is not part of list received then insert current row data
                                            dbsi.SCM_D365ImporForm_ProductReceipt.Add(item);
                                            //// save insert data
                                            var save = dbsi.SaveChanges();
                                            if (save > 0)
                                            {
                                                // add count of insert row
                                                count++;
                                            }
                                        }
                                    }
                                    /// if difference year row and year now
                                    else
                                    {
                                        // no action needed
                                        //// count skip row
                                        skipRwow++;
                                    }

                                }
                                // if the difference > 1 skip process insert
                                else
                                {
                                    // no action needed
                                    //// count skip row
                                    skipRwow++;
                                }
                            }
                        }
                        row++;
                    }
                    var arrayMonth = testMonth.ToArray();
                    var arrayYear = testYear.ToArray();
                    var arrayDiff = diff.ToArray();
                    if (count > 0)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

                        return Json(new
                        {
                            status = 1,
                            msg = "File Uploaded",
                            displayData = "prepared",
                            updateRow = updateRow,
                            skipRow = skipRwow,
                            insertRow = count,
                            totalRow = row,
                            testdiffmonth = testDiffMonth,
                            deleteRow = delRow,
                            monthNow = DateTime.Now.Month,
                            yearNow = DateTime.Now.Year,
                            arrayMonth = arrayMonth,
                            arrayYear = arrayYear,
                            arrayDiff = arrayDiff
                        });
                    }
                    else
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                        return Json(new
                        {
                            status = 0,
                            msg = "emptysave row",
                            displayData = "prepared",
                            updateRow = updateRow,
                            skipRow = skipRwow,
                            insertRow = count,
                            totalRow = row,
                            testdiffmonth = testDiffMonth,
                            deleteRow = delRow,
                            monthNow = DateTime.Now.Month,
                            yearNow = DateTime.Now.Year,
                            arrayYear = arrayYear,
                            arrayMonth = arrayMonth,
                            arrayDiff = arrayDiff
                        });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "Gagal membaca file Excel: " + ex.Message,
                        detail = ex.InnerException?.Message,
                        stack = ex.StackTrace
                    });
                }
                

            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "File Not Found",
                    displayData = "prepared"
                });
            }

        }

        [HttpPost]
        public ActionResult PurchaseBI(HttpPostedFileBase uploadFilePurchaseBI, string dateReceipt)
        {
            HttpPostedFileBase file = Request.Files["uploadFile"];
            List<SCM_D365ImporForm_PurchaseBI> bi = new List<SCM_D365ImporForm_PurchaseBI>();

            if (uploadFilePurchaseBI.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/SCM/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + "_PurchaseBI.xlsx";

                uploadFilePurchaseBI.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int row = 2;
                int count = 0;
                // if row empty
                bool firstRow = true;                

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString() != "")
                {
                    SCM_D365ImporForm_PurchaseBI item = new SCM_D365ImporForm_PurchaseBI();
                    item.Company = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.VendorCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();
                    item.VendorName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    item.InvoiceDate = DateTime.Parse(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString());
                    item.InvoiceID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.Line_No = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetValue<int>();
                    item.PONo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.POLineNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetValue<int>();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                    item.ItemId = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetString();
                    item.Text = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.Quantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetValue<decimal>();
                    item.PurchaseUnit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetString();
                    item.PurchasePrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetValue<decimal>();
                    item.Discount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetValue<decimal>();
                    item.DiscountPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetValue<decimal>();
                    item.Amount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetValue<decimal>();
                    item.Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetString();
                    item.CompanyCurrencyAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetValue<decimal>();
                    item.PurchaseReason = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.DeliveryDate = DateTime.Parse(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString());
                    item.SectionCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetString();
                    item.SectionName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                    item.Voucher = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();
                    item.CostingVoucher = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetString();
                    item.PRNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetString();
                    item.CustomerRequisition = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetString();

                    if (firstRow)
                    {
                        var Year = item.InvoiceDate.Value.Year;
                        var Month = item.InvoiceDate.Value.Month;

                        var firstDayOfMonth = new DateTime(item.InvoiceDate.Value.Year, item.InvoiceDate.Value.Month, 1);

                        var spl = dbsi.SCM_D365ImporForm_PurchaseBI.Where(w => w.InvoiceDate.Value.Year == Year && w.InvoiceDate.Value.Month == Month);
                        bi = spl.ToList();
                        dbsi.SCM_D365ImporForm_PurchaseBI.RemoveRange(spl);
                        var del = dbsi.SaveChanges();

                        firstRow = false;

                        dbsi.SCM_D365ImporForm_PurchaseBI.Add(item);
                        var save = dbsi.SaveChanges();
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;

                    }
                    else
                    {
                        dbsi.SCM_D365ImporForm_PurchaseBI.Add(item);
                        var save = dbsi.SaveChanges();
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;
                    }

                }
                if (count > 0)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    return Json(new
                    {
                        status = 1,
                        msg = "File Uploaded",
                        displayData = "prepared",
                        bi = bi
                    });
                }
                else
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    return Json(new
                    {
                        status = 0,
                        msg = "emptysave row",
                        displayData = "prepared",
                        bi = bi
                    });
                }
            }
            else
            {

                return Json(new
                {
                    status = 0,
                    msg = "File Not Found",
                    displayData = "prepared",
                    bi = bi
                });
            }

        }

        [HttpPost]
        public ActionResult PurchaseOrderLines(HttpPostedFileBase uploadFilePurchaseOrderLines, string dateReceipt)
        {
            HttpPostedFileBase file = Request.Files["uploadFile"];
            List<SCM_D365ImporForm_PurchaseLines> bi = new List<SCM_D365ImporForm_PurchaseLines>();

            if (uploadFilePurchaseOrderLines.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/SCM/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + "_PurchaseLines.xlsx";

                uploadFilePurchaseOrderLines.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                XLWorkbook xLWorkbook2 = new XLWorkbook(filePath);

                int row = 2;
                int rowCheck = 2;

                int count = 0;
                // if row empty
                bool firstRow = true;

                List<SCM_D365ImporForm_PurchaseLines> Created_at = new List<SCM_D365ImporForm_PurchaseLines>();

                while (xLWorkbook2.Worksheets.Worksheet(1).Cell(rowCheck, 1).GetString() != "")
                {
                    var sp = new SCM_D365ImporForm_PurchaseLines();
                    var a = xLWorkbook.Worksheets.Worksheet(1).Cell(rowCheck, 16).GetDateTime() ;
                    //sp.Created_at = DateTime.ParseExact(a, "dd/MM/yyyy", null);
                    sp.Created_at = a;
                    Created_at.Add(sp);

                    rowCheck++;
                }

                DateTime minDate;
                DateTime maxDate;

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString() != "")
                {

                    SCM_D365ImporForm_PurchaseLines item = new SCM_D365ImporForm_PurchaseLines();
                    item.Purchase_order = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.Vendor_account = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();  
                    item.Line_number = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetValue<int>();
                    item.Item_number = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                    item.Product_name = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.Description = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                    item.PRNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.Line_status = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                    item.Quantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetValue<decimal>();
                    item.Deliver_reminder = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetValue<decimal>();
                    item.Unit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.Unit_price = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetValue<decimal>();
                    item.Discount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetValue<decimal>();
                    item.Net_amount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetValue<decimal>();
                    item.Created_by = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetString();
                    item.Created_at = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetDateTime();


                    if (firstRow)
                    {
                        minDate = Created_at.Min(r => r.Created_at);
                        maxDate = Created_at.Max(r => r.Created_at);

                        var Year = item.Created_at.Year;
                        var Month = item.Created_at.Month;

                        var firstDayOfMonth = new DateTime(item.Created_at.Year, item.Created_at.Month, 1);

                        var spl = dbsi.SCM_D365ImporForm_PurchaseLines.Where(w => w.Created_at >= minDate && w.Created_at <= maxDate);
                        bi = spl.ToList();
                        dbsi.SCM_D365ImporForm_PurchaseLines.RemoveRange(spl);
                        var del = dbsi.SaveChanges();

                        firstRow = false;

                        dbsi.SCM_D365ImporForm_PurchaseLines.Add(item);
                        var save = dbsi.SaveChanges();
                        //var save = 1;
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;

                    }
                    else
                    {
                        

                        dbsi.SCM_D365ImporForm_PurchaseLines.Add(item);
                        var save = dbsi.SaveChanges();
                        //var save = 1;
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;
                    }

                }
                minDate = Created_at.Min(r => r.Created_at);
                maxDate = Created_at.Max(r => r.Created_at);
                if (count > 0)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    return Json(new
                    {
                        status = 1,
                        msg = "File Uploaded",
                        displayData = "prepared",
                        min = minDate.ToString("dd MM yyyy hh:mm:ss"),
                        max = maxDate.ToString("dd MM yyyy hh:mm:ss")
                    });
                }
                else
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    return Json(new
                    {
                        status = 0,
                        msg = "emptysave row",
                        displayData = "prepared",
                        bi = bi
                    });
                }
            }
            else
            {

                return Json(new
                {
                    status = 0,
                    msg = "File Not Found",
                    displayData = "prepared",
                    bi = bi
                });
            }

        }

        public ActionResult StockManagement()
        {
            HttpPostedFileBase uploadFile = Request.Files["uploadFileStockManagement"];

            if (uploadFile.ContentLength > 0)
            {
                string filePath = "C:\\inetpub\\wwwroot\\NGKDev\\NGKBusi\\Files\\SCM\\D365ExcelForm\\";
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + "_StockManagement.xlsx";

                uploadFile.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                XLWorkbook xLWorkbook2 = new XLWorkbook(filePath);

                int rowCheck = 13;

                List<SCM_D365ImporForm_StockManagement> ReceiptDate = new List<SCM_D365ImporForm_StockManagement>();

                while (xLWorkbook2.Worksheets.Worksheet(1).Cell(rowCheck, 1).GetString() != "")
                {
                    var sp = new SCM_D365ImporForm_StockManagement();
                    var a = xLWorkbook.Worksheets.Worksheet(1).Cell(rowCheck, 2).GetDateTime();
                    sp.Date = a;
                    ReceiptDate.Add(sp);

                    rowCheck++;
                }

                List<SCM_D365ImporForm_StockManagement> ol = new List<SCM_D365ImporForm_StockManagement>();

                int row = 13;

                int count = 0;
                // if row empty
                bool firstRow = true;

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString() != "")
                {

                    //string ITEMID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    //// int QTY = int.Parse(xLWorkbook.Worksheets.Worksheet(1).Cell(row, ).GetString());
                    //string VendorName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();
                    //string Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    //string DocumentDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();

                    SCM_D365ImporForm_StockManagement item = new SCM_D365ImporForm_StockManagement();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString(); 
                    item.Date = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetDateTime();
                    item.Warehouse = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    item.Item = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                    item.Batch = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.InventoryUnit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                    item.BeginningBalanceQty = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetValue<decimal>();
                    item.BeginningBalanceAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetValue<decimal>();
                    item.TransferQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetValue<decimal>();
                    item.TransferAmountPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetValue<decimal>();
                    item.ProductionQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetValue<decimal>();
                    item.ProductionAmountPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetValue<decimal>();
                    item.PurchasedQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetValue<decimal>();
                    item.PurchaseAmountPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetValue<decimal>();
                    item.AdjustQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetValue<decimal>();
                    item.AdjustAmountPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetValue<decimal>();
                    item.CountingQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetValue<decimal>();
                    item.CountingAmountPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetValue<decimal>();
                    item.TransferQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetValue<decimal>();
                    item.TransferAmountMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetValue<decimal>();
                    item.SalesQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetValue<decimal>();
                    item.SalesAmountMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetValue<decimal>();
                    item.ConsumptionQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetValue<decimal>();
                    item.ConsumptionAmountMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetValue<decimal>();
                    item.AdjustQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetValue<decimal>();
                    item.AdjustAmountMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 28).GetValue<decimal>();
                    item.CountingQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 29).GetValue<decimal>();
                    item.CountingAmountMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<decimal>();
                    item.EndingBalanceQuantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetValue<decimal>();
                    item.EndingBalanceAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 32).GetValue<decimal>();
                    item.Procate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 33).GetString();
                    item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 34).GetString();
                    item.DescriptionFromPurchaseReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 35).GetString();
                    item.DescriptionFromSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 36).GetString();

                    if (firstRow)
                    {
                        DateTime minDate = ReceiptDate.Min(r => r.Date.Value);
                        DateTime maxDate = ReceiptDate.Max(r => r.Date.Value);

                        var spl = dbsi.SCM_D365ImporForm_StockManagement.Where(w => w.Date >= minDate && w.Date <= maxDate);
                        ol = spl.ToList();

                        dbsi.SCM_D365ImporForm_StockManagement.RemoveRange(spl);
                        var del = dbsi.SaveChanges();

                        firstRow = false;

                        dbsi.SCM_D365ImporForm_StockManagement.Add(item);
                        var save = dbsi.SaveChanges();
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;
                    }
                    else
                    {
                        dbsi.SCM_D365ImporForm_StockManagement.Add(item);
                        var save = dbsi.SaveChanges();
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;
                    }
                }
                if (count > 0)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    return Json(new
                    {
                        status = 1,
                        msg = "File Uploaded",
                        displayData = "prepared"
                    });
                }
                else
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    return Json(new
                    {
                        status = 0,
                        msg = "emptysave row",
                        displayData = "prepared"
                    });
                }



            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "File Not Found",
                    displayData = "prepared"
                });
            }

        }

        [HttpPost]
        public ActionResult AffiliateEDICSV(HttpPostedFileBase uploadFilePurchaseOrderLines)
        {
            if (uploadFilePurchaseOrderLines != null && uploadFilePurchaseOrderLines.ContentLength > 0)
            {
                try
                {
                    using (var stream = uploadFilePurchaseOrderLines.InputStream)
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var sheet = workbook.Worksheet("North America");

                        string poDate = GetDateValue(sheet.Cell("C10"), "MMMM dd, yyyy");
                        string poShippingMethod = sheet.Cell("C11").GetString().Trim();
                        string poNumber = sheet.Cell("C12").GetString().Trim();

                        string originalEtd = GetDateValue(sheet.Cell("C13"), "MMMM dd, yyyy");
                        string reqMtd = GetDateValue(sheet.Cell("C14"), "MMMM dd, yyyy");

                        string reqDeliverTo = "JL. RAYA JAKARTA-BOGOR, KM 26, 6 CIRACAS, JAKARTA TIMUR, 13740 INDONESIA";
                        string supplierCd = "A4050";

                        if (string.IsNullOrEmpty(poNumber)) poNumber = "PO_Unknown";

                        StringBuilder csvBuilder = new StringBuilder();

                        var headers = new List<string>
                {
                    "PONO", "ITEM_NO", "ITEM_SPEC1", "PO_QTY", "PO_UNIT_PRICE",
                    "PO_Shipping_Method", "Original_ETD", "ReqMTD", "ReqDeliverTo",
                    "SUPPLIER_CD", "PO_DATE"
                };
                        csvBuilder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

                        int startRow = 19;
                        int row = startRow;

                        while (!string.IsNullOrWhiteSpace(sheet.Cell(row, 1).GetString()))
                        {
                            var rowData = new List<string>();

                            // 1. PONO 
                            rowData.Add(poNumber);

                            // 2. ITEM_NO 
                            string itemNo = sheet.Cell(row, 2).GetString().Trim();
                            rowData.Add(itemNo);

                            // 3. ITEM_SPEC1 
                            string itemSpec1 = sheet.Cell(row, 2).GetString().Trim();
                            rowData.Add(itemSpec1);

                            // 4. PO_QTY 
                            string poQty = sheet.Cell(row, 5).GetString().Trim();
                            rowData.Add(poQty);

                            // 5. PO_UNIT_PRICE 
                            string poUnitPrice = sheet.Cell(row, 7).GetFormattedString().Trim();
                            rowData.Add(poUnitPrice);

                            // 6. PO_Shipping_Method 
                            rowData.Add(poShippingMethod);

                            // 7. Original_ETD 
                            rowData.Add(originalEtd);

                            // 8. ReqMTD 
                            rowData.Add(reqMtd);

                            // 9. ReqDeliverTo 
                            rowData.Add(reqDeliverTo);

                            // 10. SUPPLIER_CD 
                            rowData.Add(supplierCd);

                            // 11. PO_DATE 
                            rowData.Add(poDate);

                            // GABUNGKAN
                            csvBuilder.AppendLine(string.Join(",", rowData.Select(EscapeCsv)));

                            row++;
                        }

                        byte[] fileBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
                        string outputFileName = $"{poNumber}.csv";

                        return File(fileBytes, "text/csv", outputFileName);
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error membaca Excel: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "File tidak ditemukan." });
        }

        private string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                field = field.Replace("\"", "\"\"");
                return $"\"{field}\"";
            }
            return field;
        }

        private string GetDateValue(ClosedXML.Excel.IXLCell cell, string format)
        {
            try
            {
                if (cell.TryGetValue(out DateTime dt))
                {
                    return dt.ToString(format);
                }

                if (cell.TryGetValue(out double serialDate))
                {
                    return DateTime.FromOADate(serialDate).ToString(format);
                }
            }
            catch
            {
            }

            return cell.GetString().Trim();
        }
    }
}