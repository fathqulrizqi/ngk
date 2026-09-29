using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using NGKBusi.Areas.FA.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Data.OleDb;
using ClosedXML.Excel;
using System.Collections;
using System.Web.Script.Serialization;

namespace NGKBusi.Areas.FA.Controllers
{
    [Authorize]
    public class D365ExcelFormController : Controller
    {

        public class ObjStandardCost
        {
            public string Company { get; set; }
            public string ItemNumber { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public decimal PriceUnit { get; set; }
            public decimal At { get; set; }
            public string ActivateDate { get; set; }
            public string ExtractDate { get; set; }
            public string UploadBy { get; set; }
            public string UploadTime { get; set; }
        }

        public class ObjStockManagement
        {
            public string ItemGroup { get; set; }
            public string Date { get; set; }
            public string Warehouse { get; set; }
            public string Item { get; set; }
            public string Batch { get; set; }
            public string InventoryUnit { get; set; }
            public decimal BeginningBalanceQty { get; set; }
            public decimal TransferQtyPlus { get; set; }
            public decimal ProductionQtyPlus { get; set; }
            public decimal PurchasedQtyPlus { get; set; }
            public decimal AdjustQtyPlus { get; set; }
            public decimal CountingQtyPlus { get; set; }
            public decimal TransferQtyMinus { get; set; }
            public decimal SalesQtyMinus { get; set; }
            public decimal ConsumptionQtyMinus { get; set; }
            public decimal AdjustQtyMinus { get; set; }
            public decimal CountingQtyMinus { get; set; }
            public decimal EndingBalanceQuantity { get; set; }
            public string Procate { get; set; }
            public string ProductName { get; set; }
            public string DescriptionFromPurchaseReport { get; set; }
            public string DescriptionFromSalesReport { get; set; }
            public string periode { get; set; }
        
        }


        DefaultConnection db = new DefaultConnection();
        D365ExcelFormConnection dbsi = new D365ExcelFormConnection();
        // GET: FA/D365ExcelForm
        public ActionResult Index()
        {
            // get date start dan end start untuk form generate WAD
            var startDate = (from Periode in dbsi.FA_D365ImporForm_StockManagement
                             select Periode).Distinct();
            return View();
        }
        
        public JsonResult GetTotalRowStandardCost(HttpPostedFileBase fileStandardCost, int startRow)
        {
            HttpPostedFileBase file = Request.Files["fileStandardCost"];

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            if (fileStandardCost.ContentLength > 0)
            {
                string filePath = "C:\\inetpub\\wwwroot\\NGKDev\\NGKBusi\\Files\\Sales\\D365ExcelForm\\";
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                fileStandardCost.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int firstRow = 2;
                int totalRow = 0;
                int row = firstRow;

                List<FA_D365ImporForm_StandardCost> ReceiptDate = new List<FA_D365ImporForm_StandardCost>();

                ArrayList dataArray = new ArrayList();

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 2).GetString() != "")
                {
                    var sp = new FA_D365ImporForm_StandardCost();
                    //sp.RequestedReceiptDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetDateTime();
                    //ReceiptDate.Add(sp);
                    DateTime ActivateDateyyyymmdd = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    DateTime ExtractDateyyyymmdd = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetDateTime();

                    DateTime uploadTimeyyyymmdd = DateTime.Now;

                    ObjStandardCost item = new ObjStandardCost();
                    item.Company = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.ItemNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();
                    item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    item.Price = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetValue<decimal>();
                    item.PriceUnit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetValue<decimal>();
                    item.At = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetValue<decimal>();
                    item.ActivateDate = ActivateDateyyyymmdd.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                    item.ExtractDate = ExtractDateyyyymmdd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    item.UploadBy = currUser;
                    item.UploadTime = uploadTimeyyyymmdd.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

                    firstRow++;
                    totalRow++;
                    row++;

                    dataArray.Add(item);
                }

                var spl = dbsi.FA_D365ImporForm_StandardCost;
                //ol = spl.ToList();

                dbsi.FA_D365ImporForm_StandardCost.RemoveRange(spl);
                var del = dbsi.SaveChanges();

                var jsonResult =
                 Json(new
                 {
                     status = totalRow == 0 ? 0 : 1,
                     totalRow = totalRow,
                     fileName = fileName,
                     data = dataArray,

                 }, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
            else
            {
                return Json(new
                {
                    status = 0,
                }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult ImportRowStandardCost(FA_D365ImporForm_StandardCost item)
        {
           dbsi.FA_D365ImporForm_StandardCost.Add(item);
            var save = dbsi.SaveChanges();

            if (save > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = "success save row"
                });
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Failed upload data",
                });
            }

        }

        public JsonResult GetTotalRowStockManagement(HttpPostedFileBase fileStockManagement, int startRow, DateTime periode)
        {
            HttpPostedFileBase file = Request.Files["fileStockManagement"];

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            if (fileStockManagement.ContentLength > 0)
            {
                string filePath = "C:\\inetpub\\wwwroot\\NGKDev\\NGKBusi\\Files\\Sales\\D365ExcelForm\\";
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                fileStockManagement.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int firstRow = 13;
                int totalRow = 0;
                int row = firstRow;

                List<FA_D365ImporForm_StockManagement> ReceiptDate = new List<FA_D365ImporForm_StockManagement>();

                ArrayList dataArray = new ArrayList();

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 2).GetString() != "")
                {
                    var sp = new FA_D365ImporForm_StockManagement();
                    //sp.RequestedReceiptDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetDateTime();
                    //ReceiptDate.Add(sp);
                    DateTime Dateyyyymmdd = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString(), "yyyy/MM/dd", CultureInfo.InvariantCulture);

                    ObjStockManagement item = new ObjStockManagement();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.Date = Dateyyyymmdd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    item.Warehouse = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    item.Item = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                    item.Batch = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.InventoryUnit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                    item.BeginningBalanceQty = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetValue<decimal>();
                    item.TransferQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetValue<decimal>();
                    item.ProductionQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetValue<decimal>();
                    item.PurchasedQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetValue<decimal>();
                    item.AdjustQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetValue<decimal>();
                    item.CountingQtyPlus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetValue<decimal>();
                    item.TransferQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetValue<decimal>();
                    item.SalesQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetValue<decimal>();
                    item.ConsumptionQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetValue<decimal>();
                    item.AdjustQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetValue<decimal>();
                    item.CountingQtyMinus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetValue<decimal>();
                    item.EndingBalanceQuantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetValue<decimal>();
                    item.Procate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetString();
                    item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.DescriptionFromPurchaseReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString();
                    item.DescriptionFromSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetString();
                    item.periode = periode.ToString("yyyy-MM", CultureInfo.InvariantCulture);

                    firstRow++;
                    totalRow++;
                    row++;

                    dataArray.Add(item);
                }
                var month = periode.ToString("MM", CultureInfo.InvariantCulture);
                var year = periode.ToString("yyyy", CultureInfo.InvariantCulture);
                DateTime periodeMonth = DateTime.ParseExact(periode.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                var spl = dbsi.FA_D365ImporForm_StockManagement.Where(w=> w.Periode == periodeMonth);
                var countData = spl.Count();
                if (countData > 0)
                {
                    dbsi.FA_D365ImporForm_StockManagement.RemoveRange(spl);
                    var del = dbsi.SaveChanges();
                }

                var jsonResult =
                 Json(new
                 {
                     status = totalRow == 0 ? 0 : 1,
                     totalRow = totalRow,
                     fileName = fileName,
                     data = dataArray,

                 }, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
            else
            {
                return Json(new
                {
                    status = 0,
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult ImportRowStockManagement(FA_D365ImporForm_StockManagement smodel)
        {
            dbsi.FA_D365ImporForm_StockManagement.Add(smodel);
            var save = dbsi.SaveChanges();
            //int save = 0;

            if (save > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = smodel
                });
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = smodel,
                });
            }

        }

        [HttpPost]
        public ActionResult UploadExcelStockManagement(HttpPostedFileBase fileStockManagement, string Periode)
        {
            HttpPostedFileBase file = Request.Files["fileStockManagement"];

            //string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
            //SqlConnection con = new SqlConnection(cnnString);

            //var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            //var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            if (fileStockManagement.ContentLength > 0)
            {
                
                string filePath = Server.MapPath("~/Files/FA/D365ExcelForm/");
                //string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string fileName = "StockManagementV2";
                //string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                fileStockManagement.SaveAs(filePath);

                //int rowsToDelete = 11; // Jumlah baris yang ingin dihapus

                //using (var workbook = new XLWorkbook(filePath))
                //{
                //    var worksheet = workbook.Worksheet(1); // Mengambil worksheet pertama

                //    // Menghapus baris
                //    //worksheet.Rows(1, rowsToDelete).Delete();

                //    // Menyimpan file Excel yang telah diperbarui
                //    workbook.SaveAs(filePath);
                //}


                //if (System.IO.File.Exists(filePath))
                //{
                //    // execute store procedure
                //    SqlCommand cmd = new SqlCommand("ImportDataFromExcelStockManagement", con);
                //    cmd.CommandType = CommandType.StoredProcedure;

                //    cmd.Parameters.AddWithValue("@fileName", fileName+".xlsx");
                //    cmd.Parameters.AddWithValue("@TableName", "FA_D365ImporForm_StockManagement");

                //    con.Open();
                //    int i = cmd.ExecuteNonQuery();
                //    con.Close();
                //    if (i > 0)
                //    {
                //        return Json(new
                //        {
                //            status = 1,
                //            msg = "Upload Success"
                //        });
                //    }
                //    else
                //    {
                //        return Json(new
                //        {
                //            status = 0,
                //            msg = "failed save to database"
                //        });
                //    }
                //}
                //else
                //{
                //    // informasi bahwa gagal menghapus file
                //    return Json(new
                //    {
                //        status = 0,
                //        msg = "failed Upload Excel File"
                //    });
                //}
                return Json(new
                {
                    status = 1,
                    filepath = fileName,
                    periode = Periode,
                    msg = "Upload Success"
                });

            }
            return Json(new
            {
                status = 0,
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult InsertStockManagement(string fileName, string periode)
        {
            HttpPostedFileBase file = Request.Files["fileStockManagement"];

            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //if (System.IO.File.Exists(filePath))
            //{
                //  execute store procedure
                SqlCommand cmd = new SqlCommand("ImportDataFromExcelStockManagement", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@fileName", fileName + ".xlsx");
                cmd.Parameters.AddWithValue("@TableName", "FA_D365ImporForm_StockManagement");
                cmd.Parameters.AddWithValue("@Periode", periode);

                con.Open();
                int i = cmd.ExecuteNonQuery();
                con.Close();
                if (i > 0)
                {
                    return Json(new
                    {
                        status = 1,
                        msg = "Upload Success",
                        date = periode
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "failed save to database",
                        date = periode
                    });
                }
            //}
            //else
            //{
            //    // informasi bahwa gagal menghapus file
            //    return Json(new
            //    {
            //        status = 0,
            //        msg = "failed Upload Excel File"
            //    });
            //}

        }
    }
}