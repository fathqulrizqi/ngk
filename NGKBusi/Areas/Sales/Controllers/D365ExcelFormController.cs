using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using NGKBusi.Areas.Sales.Models;
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
using NGK_AX.Models;
using ExcelDataReader;

namespace NGKBusi.Areas.Sales.Controllers
{
    public class D365ExcelFormController : Controller
    {

        public class ObjOrderLines
        {
            public string ShipDate { get; set; }
            public string RequestedReceiptDate { get; set; }
            public string RequestedShipDate { get; set; }
            public string Customer { get; set; }
            public string DeliveryName { get; set; }
            public string SalesOrder { get; set; }
            public string LineStatus { get; set; }
            public string ItemNumber { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public string Unit { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Discount { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal NetAmount { get; set; }
            public string CreatedBy { get; set; }
            public string CreatedDateAndTime { get; set; }
            public string CustomerReference { get; set; }
            public string ItemSalesTaxGroup { get; set; }
            public string NTT_NonCommercial { get; set; }
            public string SalesTaxGroup { get; set; }
            public int? LineNumber { get; set; }
            public string DescriptionForSalesReport { get; set; }
            public string Text { get; set; }
        }

        public class ObjSalesBI
        {
            public string Company { get; set; }
            public string InvoiceDateYYMM { get; set; }
            public string InvoiceDateYYYYMMDD { get; set; }
            public string DueDate { get; set; }
            public string SOCreateDate { get; set; }
            public string RequestReceiptDate { get; set; }
            public string Invoice { get; set; }
            public string SONo { get; set; }
            public string CustomerCode { get; set; }
            public string CustomerName { get; set; }
            public string CustomerSearchName { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Division { get; set; }
            public string DivisionName { get; set; }
            public string ProductCategory { get; set; }
            public string ProductCategoryName { get; set; }
            public string Country { get; set; }
            public string Area { get; set; }
            public string SalesPerson { get; set; }
            public string SalesPersonName { get; set; }
            public string BusinessType { get; set; }
            public string BusinessTypeName { get; set; }
            public string Design { get; set; }
            public string Brand { get; set; }
            public string ProductCateMajor { get; set; }
            public string ProductCateMajorName { get; set; }
            public string ProductCateMiddle { get; set; }
            public string ProductCateMiddleName { get; set; }
            public string ProductCateMinor { get; set; }
            public string ProductCateMinorName { get; set; }
            public string PackagingCountry { get; set; }
            public int Quantity { get; set; }
            public string Currency { get; set; }
            public decimal PriceByIVCurrency { get; set; }
            public decimal AmountByIVCurrency { get; set; }
            public decimal PriceByLocalCurrency { get; set; }
            public decimal AmountByLocalCurrency { get; set; }
            public string RecID { get; set; }
            public string CustomerReference { get; set; }
            public string DescriptionForSalesReport { get; set; }
            public string ItemGroup { get; set; }
            public decimal TaxPercent { get; set; }
            public decimal Tax { get; set; }
            public decimal NetAmount { get; set; }
            public string TaxInvoiceId { get; set; }
            public string NPWP { get; set; }
            public decimal LineDisc { get; set; }
            public string InvoiceAddress { get; set; }
            public string ExternalItemId { get; set; }
        }
        public class ObjSales
        {
            public string SalesOrder { get; set; }
            public decimal SalesLineNumber { get; set; }
            public string CustomerCode { get; set; }
            public string CustomerName { get; set; }
            public string CustomerReference { get; set; }
            public string ExternalItemNumber { get; set; }
            public string ItemID { get; set; }
            public string ProductName { get; set; }
            public string ItemDescription { get; set; }
            public string SearchNameAll { get; set; }
            public string SearchNameReleased { get; set; }
            public string SPType { get; set; }
            public string VehicleID { get; set; }
            public string MasterVehicle { get; set; }
            public string ItemGroup { get; set; }
            public string PackingSlipID { get; set; }
            public string PackingSlipDate { get; set; }
            public int PackingQty { get; set; }
            public string YearDlv { get; set; }
            public string MonthDlv { get; set; }
            public string ProductCategory { get; set; }
            public string ProcateName { get; set; }
            public string Genuine { get; set; }
            public string Motor { get; set; }
            public string OriginalInvoiceNo { get; set; }
            public decimal SalesPrice { get; set; }
            public decimal DiscAmount { get; set; }
            public decimal DiscPercent { get; set; }
            public decimal ExchangeRate { get; set; }
            public string Currency { get; set; }
            public string SalesClass { get; set; }
            public decimal AmountGrossCurrency { get; set; }
            public decimal AmountDiscountCurrency { get; set; }
            public decimal AmountNetCurrency { get; set; }
            public decimal AmountGrossLocalCurrency { get; set; }
            public decimal AmountDiscountLocalCurrency { get; set; }
            public decimal AmountNetLocalCurrency { get; set; }
            public string Status_GIT { get; set; }
            public string Date_Inv { get; set; }
            public string Year_Inv { get; set; }
            public string Month_Inv { get; set; }
            public string City { get; set; }
            public string Month_Odr { get; set; }
            public string CommercialStatus { get; set; }
            public decimal VAT { get; set; }
            public decimal STDCost { get; set; }
            public decimal GITCost { get; set; }
            public string PeriodDLV { get; set; }
            public string PeriodINV { get; set; }
            public int DaysDueDateInv { get; set; }
            public string NPWP { get; set; }
            public string TaxInvoiceNo { get; set; }
            public string DescriptionforSalesReport { get; set; }
        }

        public class ObjPackingQuantity
        {
            public string SalesOrder { get; set; }
            public decimal SalesLineNumber { get; set; }
            public string CustomerCode { get; set; }
            public string CustomerName { get; set; }
            public string CustomerReference { get; set; }
            public string ExternalItemNumber { get; set; }
            public string ItemID { get; set; }
            public string ProductName { get; set; }
            public string ItemDescription { get; set; }
            public string DescriptionforSalesReport { get; set; }
            public string SearchNameAll { get; set; }
            public string SearchNameReleased { get; set; }
            public string SPType { get; set; }
            public string VehicleID { get; set; }
            public string ItemGroup { get; set; }
            public string PackingSlipID { get; set; }
            public string PackingSlipDeliveryDate { get; set; }
            public int PackingQty { get; set; }
            public string YearDlv { get; set; }
            public string MonthDlv { get; set; }
            public string ProductCategory { get; set; }
            public string ProcateName { get; set; }
            public string Genuine { get; set; }
            public string Motor { get; set; }
            public string InvoiceNo { get; set; }
            public decimal SalesPrice { get; set; }
            public decimal DiscAmount { get; set; }
            public decimal DiscPercent { get; set; }
            public decimal ExchangeRate { get; set; }
            public string Currency { get; set; }
            public string SalesClass { get; set; }
            public decimal AmountGrossCurrency { get; set; }
            public decimal AmountDiscountCurrency { get; set; }
            public decimal AmountNetCurrency { get; set; }
            public decimal AmountGrossLocalCurrency { get; set; }
            public decimal AmountDiscountLocalCurrency { get; set; }
            public decimal AmountNetLocalCurrency { get; set; }
            public string Status_GIT { get; set; }
            public string Date_Inv { get; set; }
            public string Year_Inv { get; set; }
            public string Month_Inv { get; set; }
            public string City { get; set; }
            public string Month_Odr { get; set; }
            public string CommercialStatus { get; set; }
            public decimal GITPrice { get; set; }
            public decimal GITCost { get; set; }
        }

        // GET: SCM/D365ExcelForm
        DefaultConnection db = new DefaultConnection();
        D365ExcelFormConnection dbsi = new D365ExcelFormConnection();
        [Authorize(Roles = "AdminPurchasing,  Administrator")]
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult OrderLines(HttpPostedFileBase fileOrderLine)
        {
            if (fileOrderLine != null && fileOrderLine.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = Path.Combine(filePath, fileName + ".xlsx");

                // Ensure the directory exists
                string dirPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                fileOrderLine.SaveAs(filePath);

                try
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("ShipDate", typeof(DateTime));
                    dt.Columns.Add("RequestedReceiptDate", typeof(DateTime));
                    dt.Columns.Add("RequestedShipDate", typeof(DateTime));
                    dt.Columns.Add("Customer", typeof(string));
                    dt.Columns.Add("DeliveryName", typeof(string));
                    dt.Columns.Add("SalesOrder", typeof(string));
                    dt.Columns.Add("LineStatus", typeof(string));
                    dt.Columns.Add("ItemNumber", typeof(string));
                    dt.Columns.Add("ProductName", typeof(string));
                    dt.Columns.Add("Quantity", typeof(int));
                    dt.Columns.Add("Unit", typeof(string));
                    dt.Columns.Add("UnitPrice", typeof(decimal));
                    dt.Columns.Add("Discount", typeof(decimal));
                    dt.Columns.Add("DiscountPercent", typeof(decimal));
                    dt.Columns.Add("NetAmount", typeof(decimal));
                    dt.Columns.Add("CreatedBy", typeof(string));
                    dt.Columns.Add("CreatedDateAndTime", typeof(DateTime));
                    dt.Columns.Add("CustomerReference", typeof(string));
                    dt.Columns.Add("ItemSalesTaxGroup", typeof(string));
                    dt.Columns.Add("NTT_NonCommercial", typeof(string));
                    dt.Columns.Add("SalesTaxGroup", typeof(string));
                    dt.Columns.Add("LineNumber", typeof(int));
                    dt.Columns.Add("DescriptionForSalesReport", typeof(string));
                    dt.Columns.Add("Text", typeof(string));

                    DateTime? minDate = null;
                    DateTime? maxDate = null;
                    int rowCount = 0;

                    using (XLWorkbook xLWorkbook = new XLWorkbook(filePath))
                    {
                        IXLWorksheet worksheet = xLWorkbook.Worksheets.Worksheet(1);
                        int row = 2;

                        while (worksheet.Cell(row, 2).GetString() != "")
                        {
                            DataRow dr = dt.NewRow();
                            dr["ShipDate"] = string.IsNullOrEmpty(worksheet.Cell(row, 1).GetString()) ? (object)DBNull.Value : worksheet.Cell(row, 1).GetDateTime();
                            
                            DateTime receiptDate = worksheet.Cell(row, 2).GetDateTime();
                            dr["RequestedReceiptDate"] = receiptDate;
                            
                            if (minDate == null || receiptDate < minDate) minDate = receiptDate;
                            if (maxDate == null || receiptDate > maxDate) maxDate = receiptDate;

                            dr["RequestedShipDate"] = string.IsNullOrEmpty(worksheet.Cell(row, 3).GetString()) ? (object)DBNull.Value : worksheet.Cell(row, 3).GetDateTime();
                            dr["Customer"] = worksheet.Cell(row, 4).GetString();
                            dr["DeliveryName"] = worksheet.Cell(row, 5).GetString();
                            dr["SalesOrder"] = worksheet.Cell(row, 6).GetString();
                            dr["LineStatus"] = worksheet.Cell(row, 7).GetString();
                            dr["ItemNumber"] = worksheet.Cell(row, 8).GetString();
                            dr["ProductName"] = worksheet.Cell(row, 9).GetString();
                            dr["Quantity"] = worksheet.Cell(row, 10).GetValue<int>();
                            dr["Unit"] = worksheet.Cell(row, 11).GetString();
                            dr["UnitPrice"] = worksheet.Cell(row, 12).GetValue<decimal>();
                            dr["Discount"] = worksheet.Cell(row, 13).GetValue<decimal>();
                            dr["DiscountPercent"] = worksheet.Cell(row, 14).GetValue<decimal>();
                            dr["NetAmount"] = worksheet.Cell(row, 15).GetValue<decimal>();
                            dr["CreatedBy"] = worksheet.Cell(row, 16).GetString();
                            dr["CreatedDateAndTime"] = string.IsNullOrEmpty(worksheet.Cell(row, 17).GetString()) ? (object)DBNull.Value : worksheet.Cell(row, 17).GetDateTime();
                            dr["CustomerReference"] = worksheet.Cell(row, 18).GetString();
                            dr["ItemSalesTaxGroup"] = worksheet.Cell(row, 19).GetString();
                            dr["NTT_NonCommercial"] = worksheet.Cell(row, 20).GetString();
                            dr["SalesTaxGroup"] = worksheet.Cell(row, 21).GetString();
                            dr["LineNumber"] = string.IsNullOrEmpty(worksheet.Cell(row, 22).GetString()) ? (object)DBNull.Value : worksheet.Cell(row, 22).GetValue<int>();
                            dr["DescriptionForSalesReport"] = worksheet.Cell(row, 23).GetString();
                            dr["Text"] = worksheet.Cell(row, 24).GetString();

                            dt.Rows.Add(dr);
                            rowCount++;
                            row++;
                        }
                    }

                    if (rowCount > 0 && minDate.HasValue && maxDate.HasValue)
                    {
                        var startDate = new DateTime(minDate.Value.Year, minDate.Value.Month, 1);
                        var endDateStart = new DateTime(maxDate.Value.Year, maxDate.Value.Month, 1);
                        var endDate = endDateStart.AddMonths(1).AddDays(-1);

                        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    // Remove existing records in the date range
                                    string deleteSql = "DELETE FROM Sales_D365ImporForm_OrderLines WHERE RequestedReceiptDate >= @startDate AND RequestedReceiptDate <= @endDate";
                                    using (SqlCommand deleteCmd = new SqlCommand(deleteSql, conn, trans))
                                    {
                                        deleteCmd.Parameters.AddWithValue("@startDate", startDate);
                                        deleteCmd.Parameters.AddWithValue("@endDate", endDate);
                                        deleteCmd.ExecuteNonQuery();
                                    }

                                    // Stream new records to the database using SqlBulkCopy
                                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, trans))
                                    {
                                        bulkCopy.DestinationTableName = "Sales_D365ImporForm_OrderLines";
                                        bulkCopy.BatchSize = 5000;
                                        
                                        foreach (DataColumn col in dt.Columns)
                                        {
                                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                        }

                                        bulkCopy.WriteToServer(dt);
                                    }

                                    trans.Commit();
                                }
                                catch (Exception)
                                {
                                    trans.Rollback();
                                    throw;
                                }
                            }
                        }

                        try
                        {
                            SaveUploadLog(fileName + ".xlsx", "OrderLines", rowCount);
                        }
                        catch { }

                        return Json(new
                        {
                            status = 1,
                            msg = rowCount + " rows successfully imported and saved."
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            status = 0,
                            msg = "No rows found in the uploaded file."
                        });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "Error processing excel file: " + ex.Message
                    });
                }
                finally
                {
                    // Always delete the uploaded file
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            return Json(new
            {
                status = 0,
                msg = "No file uploaded or file is empty."
            });
        }

        public JsonResult GetTotalRowOrderLines(HttpPostedFileBase fileOrderLine, int startRow)
        {
            HttpPostedFileBase file = Request.Files["fileOrderLine"];

            if (fileOrderLine.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                fileOrderLine.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int firstRow = 2;
                int totalRow = 0;
                int row = firstRow;

                List<Sales_D365ImporForm_OrderLines> ReceiptDate = new List<Sales_D365ImporForm_OrderLines>();

                ArrayList dataArray = new ArrayList();

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 2).GetString() != "")
                {
                    var sp = new Sales_D365ImporForm_OrderLines();
                    sp.RequestedReceiptDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetDateTime();
                    ReceiptDate.Add(sp);

                    ObjOrderLines item = new ObjOrderLines();
                    item.ShipDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.RequestedReceiptDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();
                    item.RequestedShipDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    item.Customer = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                    item.DeliveryName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.SalesOrder = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                    item.LineStatus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.ItemNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                    item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                    item.Quantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetValue<int>();
                    item.Unit = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.UnitPrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetValue<decimal>();
                    item.Discount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetValue<decimal>();
                    item.DiscountPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetValue<decimal>();
                    item.NetAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetValue<decimal>();
                    item.CreatedBy = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetString();
                    item.CreatedDateAndTime = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetString();
                    item.CustomerReference = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetString();
                    item.ItemSalesTaxGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetString();
                    item.NTT_NonCommercial = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.SalesTaxGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString();
                    item.LineNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetValue<int>();
                    item.DescriptionForSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                    item.Text = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();

                    firstRow++;
                    totalRow++;
                    row++;

                    dataArray.Add(item);
                }

                DateTime minDate = ReceiptDate.Min(r => r.RequestedReceiptDate.Value);
                DateTime maxDate = ReceiptDate.Max(r => r.RequestedReceiptDate.Value);

                var startDate = new DateTime(minDate.Year, minDate.Month, 1);
                var endDateStart = new DateTime(maxDate.Year, maxDate.Month, 1);
                var endDate = endDateStart.AddMonths(1).AddDays(-1);

                var spl = dbsi.Sales_D365ImporForm_OrderLines.Where(w => w.RequestedReceiptDate >= startDate && w.RequestedReceiptDate <= endDate);
                //ol = spl.ToList();

                dbsi.Sales_D365ImporForm_OrderLines.RemoveRange(spl);
                var del = dbsi.SaveChanges();

                var jsonResult =
                 Json(new
                 {
                     status = totalRow == 0 ? 0 : 1,
                     totalRow = totalRow,
                     fileName = fileName,
                     startDate = startDate,
                     endDate = endDate,
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

        public ActionResult ImportRowOrderLines(Sales_D365ImporForm_OrderLines item)
        {
            int save = 0;
            if (item.SalesOrder != null)
            {
                dbsi.Sales_D365ImporForm_OrderLines.Add(item);
                save = dbsi.SaveChanges();
            }

            if (save > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = item
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
        public ActionResult SalesBI(HttpPostedFileBase uploadSalesBI)
        {
            HttpPostedFileBase file = Request.Files["uploadSalesBI"];


            if (uploadSalesBI.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                uploadSalesBI.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                XLWorkbook xLWorkbook2 = new XLWorkbook(filePath);

                int rowCheck = 2;

                int count = 0;
                // if row empty
                bool firstRow = true;

                List<Sales_D365ImporForm_SalesBI> ReceiptDate = new List<Sales_D365ImporForm_SalesBI>();

                while (xLWorkbook2.Worksheets.Worksheet(1).Cell(rowCheck, 1).GetString() != "")
                {
                    var sp = new Sales_D365ImporForm_SalesBI();
                    var a = xLWorkbook.Worksheets.Worksheet(1).Cell(rowCheck, 3).GetString();
                    sp.InvoiceDateYYYYMMDD = DateTime.ParseExact(a, "yyyyMMdd", null);
                    ReceiptDate.Add(sp);

                    rowCheck++;
                }

                List<Sales_D365ImporForm_SalesBI> ol = new List<Sales_D365ImporForm_SalesBI>();

                int row = 2;

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString() != "")
                {
                    Sales_D365ImporForm_SalesBI item = new Sales_D365ImporForm_SalesBI();
                    item.Company = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.InvoiceDateYYMM = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString(), "yyyyMM", CultureInfo.InvariantCulture);
                    item.InvoiceDateYYYYMMDD = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    item.DueDate = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    item.SOCreateDate = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    item.RequestReceiptDate = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    item.Invoice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.SONo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                    item.CustomerCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                    item.CustomerName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetString();
                    item.CustomerSearchName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.ItemCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetString();
                    item.ItemName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetString();
                    item.Division = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetString();
                    item.DivisionName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetString();
                    item.ProductCategory = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetString();
                    item.ProductCategoryName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetString();
                    item.Country = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetString();
                    item.Area = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetString();
                    item.SalesPerson = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.SalesPersonName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString();
                    item.BusinessType = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetString();
                    item.BusinessTypeName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                    item.Design = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();
                    item.Brand = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetString();
                    item.ProductCateMajor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetString();
                    item.ProductCateMajorName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetString();
                    item.ProductCateMiddle = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 28).GetString();
                    item.ProductCateMiddleName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 29).GetString();
                    item.ProductCateMinor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetString();
                    item.ProductCateMinorName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetString();
                    item.PackagingCountry = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 32).GetString();
                    item.Quantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 33).GetValue<int>();
                    item.Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 34).GetString();
                    item.PriceByIVCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 35).GetValue<decimal>();
                    item.AmountByIVCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 36).GetValue<decimal>();
                    item.PriceByLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 37).GetValue<decimal>();
                    item.AmountByLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 38).GetValue<decimal>();
                    item.RecID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetString();
                    item.CustomerReference = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 40).GetString();
                    item.DescriptionForSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 41).GetString();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 42).GetString();
                    item.TaxPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 43).GetValue<decimal>();
                    item.Tax = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 44).GetValue<decimal>();
                    item.NetAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 45).GetValue<decimal>();
                    item.TaxInvoiceId = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 46).GetString();
                    item.NPWP = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 47).GetString();
                    item.LineDisc = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 48).GetValue<decimal>();
                    item.InvoiceAddress = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 49).GetString();
                    item.ExternalItemId = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 50).GetString();

                    if (firstRow)
                    {

                        DateTime minDate = ReceiptDate.Min(r => r.InvoiceDateYYYYMMDD.Value);
                        DateTime maxDate = ReceiptDate.Max(r => r.InvoiceDateYYYYMMDD.Value);

                        var spl = dbsi.Sales_D365ImporForm_SalesBI.Where(w => w.InvoiceDateYYYYMMDD >= minDate && w.InvoiceDateYYYYMMDD <= maxDate);
                        ol = spl.ToList();

                        dbsi.Sales_D365ImporForm_SalesBI.RemoveRange(spl);
                        var del = dbsi.SaveChanges();

                        //var spl = dbsi.Sales_D365ImporForm_OrderLines.Where(w => w.CreatedDateAndTime.Value.Year == Year && w.CreatedDateAndTime.Value.Month == Month);
                        //ol = spl.ToList();

                        //dbsi.Sales_D365ImporForm_OrderLines.RemoveRange(spl);
                        //var del = dbsi.SaveChanges();

                        firstRow = false;

                        dbsi.Sales_D365ImporForm_SalesBI.Add(item);
                        var save = dbsi.SaveChanges();
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;
                    }
                    else
                    {
                        dbsi.Sales_D365ImporForm_SalesBI.Add(item);
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
                    try
                    {
                        SaveUploadLog(fileName + ".xlsx", "SalesBI", count);
                    }
                    catch { }

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    return Json(new
                    {
                        status = 1,
                        msg = count + " row saved",
                        displayData = "prepared",
                        ol = ol
                    });
                }
                else
                {
                    try
                    {
                        // Log upload attempt even if no rows were saved
                        SaveUploadLog(fileName + ".xlsx", "SalesBI", count);
                    }
                    catch { }

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    return Json(new
                    {
                        status = 0,
                        msg = "emptysave row",
                        displayData = "prepared",
                        ol = ol
                    });
                }
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Failed upload data",
                    displayData = "prepared"
                });
            }

        }

        public JsonResult GetTotalRowSalesBI(HttpPostedFileBase uploadSalesBI, int startRow)
        {
            HttpPostedFileBase file = Request.Files["fileOrderLine"];

            if (uploadSalesBI.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                uploadSalesBI.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int firstRow = 2;
                int totalRow = 0;
                int row = firstRow;

                List<Sales_D365ImporForm_SalesBI> ReceiptDate = new List<Sales_D365ImporForm_SalesBI>();

                ArrayList dataArray = new ArrayList();

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 2).GetString() != "")
                {
                    var sp = new Sales_D365ImporForm_SalesBI();
                    var a = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    sp.InvoiceDateYYYYMMDD = DateTime.ParseExact(a, "yyyyMMdd", null);
                    ReceiptDate.Add(sp);

                    DateTime dateInvoiceDateYYMM = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString(), "yyyyMM", CultureInfo.InvariantCulture);
                    DateTime dateyyyymmdd = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    DateTime dateDueDate = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    DateTime dateSOCreateDate = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                    DateTime dateRequestReceiptDate = DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);

                    ObjSalesBI item = new ObjSalesBI();
                    item.Company = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.InvoiceDateYYMM = dateInvoiceDateYYMM.ToString("yyyy/MM", CultureInfo.InvariantCulture);
                    item.InvoiceDateYYYYMMDD = dateyyyymmdd.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                    item.DueDate = dateDueDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                    item.SOCreateDate = dateSOCreateDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                    item.RequestReceiptDate = dateRequestReceiptDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                    item.Invoice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.SONo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                    item.CustomerCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                    item.CustomerName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetString();
                    item.CustomerSearchName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.ItemCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetString();
                    item.ItemName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetString();
                    item.Division = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetString();
                    item.DivisionName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetString();
                    item.ProductCategory = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetString();
                    item.ProductCategoryName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetString();
                    item.Country = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetString();
                    item.Area = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetString();
                    item.SalesPerson = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.SalesPersonName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString();
                    item.BusinessType = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetString();
                    item.BusinessTypeName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                    item.Design = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();
                    item.Brand = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetString();
                    item.ProductCateMajor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetString();
                    item.ProductCateMajorName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetString();
                    item.ProductCateMiddle = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 28).GetString();
                    item.ProductCateMiddleName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 29).GetString();
                    item.ProductCateMinor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetString();
                    item.ProductCateMinorName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetString();
                    item.PackagingCountry = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 32).GetString();
                    item.Quantity = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 33).GetValue<int>();
                    item.Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 34).GetString();
                    item.PriceByIVCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 35).GetValue<decimal>();
                    item.AmountByIVCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 36).GetValue<decimal>();
                    item.PriceByLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 37).GetValue<decimal>();
                    item.AmountByLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 38).GetValue<decimal>();
                    item.RecID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetString();
                    item.CustomerReference = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 40).GetString();
                    item.DescriptionForSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 41).GetString();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 42).GetString();
                    item.TaxPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 43).GetValue<decimal>();
                    item.Tax = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 44).GetValue<decimal>();
                    item.NetAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 45).GetValue<decimal>();
                    item.TaxInvoiceId = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 46).GetString();
                    item.NPWP = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 47).GetString();
                    item.LineDisc = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 48).GetValue<decimal>();
                    item.InvoiceAddress = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 49).GetString();
                    item.ExternalItemId = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 50).GetString();

                    firstRow++;
                    totalRow++;
                    row++;

                    dataArray.Add(item);
                }

                DateTime minDate = ReceiptDate.Min(r => r.InvoiceDateYYYYMMDD.Value);
                DateTime maxDate = ReceiptDate.Max(r => r.InvoiceDateYYYYMMDD.Value);

                var spl = dbsi.Sales_D365ImporForm_SalesBI.Where(w => w.InvoiceDateYYYYMMDD >= minDate && w.InvoiceDateYYYYMMDD <= maxDate);

                dbsi.Sales_D365ImporForm_SalesBI.RemoveRange(spl);
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

        public ActionResult ImportRowSalesBI(Sales_D365ImporForm_SalesBI item)
        {


            dbsi.Sales_D365ImporForm_SalesBI.Add(item);
            var save = dbsi.SaveChanges();

            if (save > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = item
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
        public ActionResult Sales(HttpPostedFileBase uploadSales)
        {
            if (uploadSales != null && uploadSales.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = Path.Combine(filePath, fileName + ".xlsx");

                // Ensure the directory exists
                string dirPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                uploadSales.SaveAs(filePath);

                try
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("SalesOrder", typeof(string));
                    dt.Columns.Add("SalesLineNumber", typeof(decimal));
                    dt.Columns.Add("CustomerCode", typeof(string));
                    dt.Columns.Add("CustomerName", typeof(string));
                    dt.Columns.Add("CustomerReference", typeof(string));
                    dt.Columns.Add("ExternalItemNumber", typeof(string));
                    dt.Columns.Add("ItemID", typeof(string));
                    dt.Columns.Add("ProductName", typeof(string));
                    dt.Columns.Add("ItemDescription", typeof(string));
                    dt.Columns.Add("SearchNameAll", typeof(string));
                    dt.Columns.Add("SearchNameReleased", typeof(string));
                    dt.Columns.Add("SPType", typeof(string));
                    dt.Columns.Add("VehicleID", typeof(string));
                    dt.Columns.Add("MasterVehicle", typeof(string));
                    dt.Columns.Add("ItemGroup", typeof(string));
                    dt.Columns.Add("PackingSlipID", typeof(string));
                    dt.Columns.Add("PackingSlipDate", typeof(DateTime));
                    dt.Columns.Add("PackingQty", typeof(int));
                    dt.Columns.Add("YearDlv", typeof(string));
                    dt.Columns.Add("MonthDlv", typeof(string));
                    dt.Columns.Add("ProductCategory", typeof(string));
                    dt.Columns.Add("ProcateName", typeof(string));
                    dt.Columns.Add("Genuine", typeof(string));
                    dt.Columns.Add("Motor", typeof(string));
                    dt.Columns.Add("OriginalInvoiceNo", typeof(string));
                    dt.Columns.Add("SalesPrice", typeof(decimal));
                    dt.Columns.Add("DiscAmount", typeof(decimal));
                    dt.Columns.Add("DiscPercent", typeof(decimal));
                    dt.Columns.Add("ExchangeRate", typeof(decimal));
                    dt.Columns.Add("Currency", typeof(string));
                    dt.Columns.Add("SalesClass", typeof(string));
                    dt.Columns.Add("AmountGrossCurrency", typeof(decimal));
                    dt.Columns.Add("AmountDiscountCurrency", typeof(decimal));
                    dt.Columns.Add("AmountNetCurrency", typeof(decimal));
                    dt.Columns.Add("AmountGrossLocalCurrency", typeof(decimal));
                    dt.Columns.Add("AmountDiscountLocalCurrency", typeof(decimal));
                    dt.Columns.Add("AmountNetLocalCurrency", typeof(decimal));
                    dt.Columns.Add("Status_GIT", typeof(string));
                    dt.Columns.Add("Date_Inv", typeof(DateTime));
                    dt.Columns.Add("Year_Inv", typeof(string));
                    dt.Columns.Add("Month_Inv", typeof(string));
                    dt.Columns.Add("City", typeof(string));
                    dt.Columns.Add("Month_Odr", typeof(string));
                    dt.Columns.Add("CommercialStatus", typeof(string));
                    dt.Columns.Add("VAT", typeof(decimal));
                    dt.Columns.Add("STDCost", typeof(decimal));
                    dt.Columns.Add("GITCost", typeof(decimal));
                    dt.Columns.Add("PeriodDLV", typeof(string));
                    dt.Columns.Add("PeriodINV", typeof(string));
                    dt.Columns.Add("DaysDueDateInv", typeof(int));
                    dt.Columns.Add("NPWP", typeof(string));
                    dt.Columns.Add("TaxInvoiceNo", typeof(string));
                    dt.Columns.Add("DescriptionforSalesReport", typeof(string));

                    DateTime? minDate = null;
                    DateTime? maxDate = null;
                    int rowCount = 0;

                    using (var stream = System.IO.File.Open(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                        {
                            int currentRow = 1;
                            while (reader.Read())
                            {
                                if (currentRow < 3) // Sales data starts at row 3
                                {
                                    currentRow++;
                                    continue;
                                }

                                string salesOrder = reader.GetValue(1)?.ToString();
                                if (string.IsNullOrEmpty(salesOrder))
                                {
                                    break; // Stop when Col 2 is empty
                                }

                                DataRow dr = dt.NewRow();
                                dr["SalesOrder"] = salesOrder;
                                dr["SalesLineNumber"] = Convert.ToDecimal(reader.GetValue(2) ?? 0);
                                dr["CustomerCode"] = reader.GetValue(3)?.ToString() ?? "";
                                dr["CustomerName"] = reader.GetValue(4)?.ToString() ?? "";
                                dr["CustomerReference"] = reader.GetValue(5)?.ToString() ?? "";
                                dr["ExternalItemNumber"] = reader.GetValue(6)?.ToString() ?? "";
                                dr["ItemID"] = reader.GetValue(7)?.ToString() ?? "";
                                dr["ProductName"] = reader.GetValue(8)?.ToString() ?? "";
                                dr["ItemDescription"] = reader.GetValue(9)?.ToString() ?? "";
                                dr["SearchNameAll"] = reader.GetValue(10)?.ToString() ?? "";
                                dr["SearchNameReleased"] = reader.GetValue(11)?.ToString() ?? "";
                                dr["SPType"] = reader.GetValue(12)?.ToString() ?? "";
                                dr["VehicleID"] = reader.GetValue(13)?.ToString() ?? "";
                                dr["MasterVehicle"] = reader.GetValue(14)?.ToString() ?? "";
                                dr["ItemGroup"] = reader.GetValue(15)?.ToString() ?? "";
                                dr["PackingSlipID"] = reader.GetValue(16)?.ToString() ?? "";

                                var packingSlipDateVal = reader.GetValue(17);
                                if (packingSlipDateVal == null || string.IsNullOrEmpty(packingSlipDateVal.ToString()))
                                {
                                    dr["PackingSlipDate"] = DBNull.Value;
                                }
                                else
                                {
                                    DateTime packingSlipDate = Convert.ToDateTime(packingSlipDateVal);
                                    dr["PackingSlipDate"] = packingSlipDate;
                                    if (minDate == null || packingSlipDate < minDate) minDate = packingSlipDate;
                                    if (maxDate == null || packingSlipDate > maxDate) maxDate = packingSlipDate;
                                }

                                dr["PackingQty"] = Convert.ToInt32(reader.GetValue(18) ?? 0);
                                dr["YearDlv"] = reader.GetValue(19)?.ToString() ?? "";
                                dr["MonthDlv"] = reader.GetValue(20)?.ToString() ?? "";
                                dr["ProductCategory"] = reader.GetValue(21)?.ToString() ?? "";
                                dr["ProcateName"] = reader.GetValue(22)?.ToString() ?? "";
                                dr["Genuine"] = reader.GetValue(23)?.ToString() ?? "";
                                dr["Motor"] = reader.GetValue(24)?.ToString() ?? "";
                                dr["OriginalInvoiceNo"] = reader.GetValue(25)?.ToString() ?? "";
                                dr["SalesPrice"] = Convert.ToDecimal(reader.GetValue(26) ?? 0);
                                dr["DiscAmount"] = Convert.ToDecimal(reader.GetValue(27) ?? 0);
                                dr["DiscPercent"] = Convert.ToDecimal(reader.GetValue(28) ?? 0);
                                dr["ExchangeRate"] = Convert.ToDecimal(reader.GetValue(29) ?? 0);
                                dr["Currency"] = reader.GetValue(30)?.ToString() ?? "";
                                dr["SalesClass"] = reader.GetValue(31)?.ToString() ?? "";
                                dr["AmountGrossCurrency"] = Convert.ToDecimal(reader.GetValue(32) ?? 0);
                                dr["AmountDiscountCurrency"] = Convert.ToDecimal(reader.GetValue(33) ?? 0);
                                dr["AmountNetCurrency"] = Convert.ToDecimal(reader.GetValue(34) ?? 0);
                                dr["AmountGrossLocalCurrency"] = Convert.ToDecimal(reader.GetValue(35) ?? 0);
                                dr["AmountDiscountLocalCurrency"] = Convert.ToDecimal(reader.GetValue(36) ?? 0);
                                dr["AmountNetLocalCurrency"] = Convert.ToDecimal(reader.GetValue(37) ?? 0);
                                dr["Status_GIT"] = reader.GetValue(38)?.ToString() ?? "";

                                var dateInvVal = reader.GetValue(39);
                                dr["Date_Inv"] = (dateInvVal == null || string.IsNullOrEmpty(dateInvVal.ToString())) ? (object)DBNull.Value : Convert.ToDateTime(dateInvVal);

                                dr["Year_Inv"] = reader.GetValue(40)?.ToString() ?? "";
                                dr["Month_Inv"] = reader.GetValue(41)?.ToString() ?? "";
                                dr["City"] = reader.GetValue(42)?.ToString() ?? "";
                                dr["Month_Odr"] = reader.GetValue(43)?.ToString() ?? "";
                                dr["CommercialStatus"] = reader.GetValue(44)?.ToString() ?? "";
                                dr["VAT"] = Convert.ToDecimal(reader.GetValue(45) ?? 0);
                                dr["STDCost"] = Convert.ToDecimal(reader.GetValue(46) ?? 0);
                                dr["GITCost"] = Convert.ToDecimal(reader.GetValue(47) ?? 0);
                                dr["PeriodDLV"] = reader.GetValue(48)?.ToString() ?? "";
                                dr["PeriodINV"] = reader.GetValue(49)?.ToString() ?? "";
                                dr["DaysDueDateInv"] = Convert.ToInt32(reader.GetValue(50) ?? 0);
                                dr["NPWP"] = reader.GetValue(51)?.ToString() ?? "";
                                dr["TaxInvoiceNo"] = reader.GetValue(52)?.ToString() ?? "";
                                dr["DescriptionforSalesReport"] = reader.GetValue(53)?.ToString() ?? "";

                                dt.Rows.Add(dr);
                                rowCount++;
                                currentRow++;
                            }
                        }
                    }

                    if (rowCount > 0 && minDate.HasValue && maxDate.HasValue)
                    {
                        var startDate = new DateTime(minDate.Value.Year, minDate.Value.Month, 1);
                        var endDateStart = new DateTime(maxDate.Value.Year, maxDate.Value.Month, 1);
                        var endDate = endDateStart.AddMonths(1).AddDays(-1);

                        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();
                            using (SqlTransaction trans = conn.BeginTransaction())
                            {
                                try
                                {
                                    // Remove existing records in the date range
                                    string deleteSql = "DELETE FROM Sales_D365ImporForm_Sales WHERE PackingSlipDate >= @startDate AND PackingSlipDate <= @endDate";
                                    using (SqlCommand deleteCmd = new SqlCommand(deleteSql, conn, trans))
                                    {
                                        deleteCmd.Parameters.AddWithValue("@startDate", startDate);
                                        deleteCmd.Parameters.AddWithValue("@endDate", endDate);
                                        deleteCmd.ExecuteNonQuery();
                                    }

                                    // Stream new records to the database using SqlBulkCopy
                                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, trans))
                                    {
                                        bulkCopy.DestinationTableName = "Sales_D365ImporForm_Sales";
                                        bulkCopy.BatchSize = 5000;
                                        
                                        foreach (DataColumn col in dt.Columns)
                                        {
                                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                        }

                                        bulkCopy.WriteToServer(dt);
                                    }

                                    trans.Commit();
                                }
                                catch (Exception)
                                {
                                    trans.Rollback();
                                    throw;
                                }
                            }
                        }

                        try
                        {
                            SaveUploadLog(fileName + ".xlsx", "Sales", rowCount);
                        }
                        catch { }

                        return Json(new
                        {
                            status = 1,
                            msg = rowCount + " rows successfully imported and saved."
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            status = 0,
                            msg = "No rows found in the uploaded file."
                        });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "Error processing excel file: " + ex.Message
                    });
                }
                finally
                {
                    // Always delete the uploaded file
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            return Json(new
            {
                status = 0,
                msg = "No file uploaded or file is empty."
            });
        }

        public ActionResult SalesPackingQuantity(HttpPostedFileBase uploadSalesPackingQuantity)
        {
            HttpPostedFileBase file = Request.Files["uploadSalesPackingQuantity"];


            if (uploadSalesPackingQuantity.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                uploadSalesPackingQuantity.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                XLWorkbook xLWorkbook2 = new XLWorkbook(filePath);

                int rowCheck = 2;

                int count = 0;
                // if row empty
                bool firstRow = true;

                List<Sales_D365ImporForm_SalesPackingQuantity> PackingSlipDeliveryDate = new List<Sales_D365ImporForm_SalesPackingQuantity>();

                while (xLWorkbook2.Worksheets.Worksheet(1).Cell(rowCheck, 2).GetString() != "")
                {
                    var sp = new Sales_D365ImporForm_SalesPackingQuantity();
                    var a = xLWorkbook.Worksheets.Worksheet(1).Cell(rowCheck, 17).GetDateTime();
                    sp.PackingSlipDeliveryDate = a;
                    PackingSlipDeliveryDate.Add(sp);

                    rowCheck++;
                }

                //DateTime.ParseExact(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString(), "yyyyMMdd", CultureInfo.InvariantCulture);

                List<Sales_D365ImporForm_SalesPackingQuantity> ol = new List<Sales_D365ImporForm_SalesPackingQuantity>();

                int row = 2;

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString() != "")
                {
                    Sales_D365ImporForm_SalesPackingQuantity item = new Sales_D365ImporForm_SalesPackingQuantity();
                    item.SalesOrder = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.SalesLineNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetValue<decimal>();
                    item.CustomerCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    item.CustomerName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                    item.CustomerReference = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.ExternalItemNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                    item.ItemID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                    item.ItemDescription = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                    item.DescriptionforSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetString();
                    item.SearchNameAll = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.SearchNameReleased = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetString();
                    item.SPType = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetString();
                    item.VehicleID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetString();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetValue<string>();
                    item.PackingSlipID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetValue<string>();
                    item.PackingSlipDeliveryDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetDateTime();
                    item.PackingQty = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetValue<int>();
                    item.YearDlv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetString();
                    item.MonthDlv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.ProductCategory = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString();
                    item.ProcateName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetString();
                    item.Genuine = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                    item.Motor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();
                    item.InvoiceNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetString();
                    item.SalesPrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetValue<decimal>();
                    item.DiscAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetValue<decimal>();
                    item.DiscPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 28).GetValue<decimal>();
                    item.ExchangeRate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 29).GetValue<decimal>();
                    item.Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetString();
                    item.SalesClass = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetString();
                    item.AmountGrossCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 32).GetValue<decimal>();
                    item.AmountDiscountCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 33).GetValue<decimal>();
                    item.AmountNetCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 34).GetValue<decimal>();
                    item.AmountGrossLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 35).GetValue<decimal>();
                    item.AmountDiscountLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 36).GetValue<decimal>();
                    item.AmountNetLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 37).GetValue<decimal>();
                    item.Status_GIT = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 38).GetString();
                    if (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetString() == "")
                    {
                        item.Date_Inv = null;
                    }
                    else
                    {
                        item.Date_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetDateTime();
                    }
                    item.Year_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 40).GetString();
                    item.Month_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 41).GetString();
                    item.City = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 42).GetString();
                    item.Month_Odr = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 43).GetString();
                    item.CommercialStatus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 44).GetString();
                    item.GITPrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 45).GetValue<decimal>();
                    item.GITCost = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 46).GetValue<decimal>();
                    if (firstRow)
                    {
                        DateTime minDate = PackingSlipDeliveryDate.Min(r => r.PackingSlipDeliveryDate.Value);
                        DateTime maxDate = PackingSlipDeliveryDate.Max(r => r.PackingSlipDeliveryDate.Value);

                        var spl = dbsi.Sales_D365ImporForm_SalesPackingQuantity.Where(w => w.PackingSlipDeliveryDate >= minDate && w.PackingSlipDeliveryDate <= maxDate);
                        ol = spl.ToList();

                        dbsi.Sales_D365ImporForm_SalesPackingQuantity.RemoveRange(spl);
                        var del = dbsi.SaveChanges();

                        firstRow = false;

                        dbsi.Sales_D365ImporForm_SalesPackingQuantity.Add(item);
                        var save = dbsi.SaveChanges();
                        if (save > 0)
                        {
                            count++;
                        }
                        row++;
                    }
                    else
                    {
                        dbsi.Sales_D365ImporForm_SalesPackingQuantity.Add(item);
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
                        msg = count + " row saved",
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
                        msg = "empty save row",
                        displayData = "prepared"
                    });
                }
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Failed upload data",
                    displayData = "prepared"
                });
            }
        }
        public JsonResult GetTotalRowPackingQuantity(HttpPostedFileBase uploadSalesPackingQuantity, int startRow)
        {
            HttpPostedFileBase file = Request.Files["uploadSalesPackingQuantity"];

            if (uploadSalesPackingQuantity.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                uploadSalesPackingQuantity.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int firstRow = 2;
                int totalRow = 0;
                int row = firstRow;

                List<Sales_D365ImporForm_SalesPackingQuantity> PackingSlipDeliveryDate = new List<Sales_D365ImporForm_SalesPackingQuantity>();

                ArrayList dataArray = new ArrayList();

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 2).GetString() != "")
                {
                    var sp = new Sales_D365ImporForm_SalesPackingQuantity();
                    var a = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetDateTime();
                    sp.PackingSlipDeliveryDate = a;
                    PackingSlipDeliveryDate.Add(sp);

                    DateTime datePackingSlipDeliveryDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetDateTime();


                    ObjPackingQuantity item = new ObjPackingQuantity();
                    item.SalesOrder = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    item.SalesLineNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetValue<decimal>();
                    item.CustomerCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                    item.CustomerName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                    item.CustomerReference = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.ExternalItemNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                    item.ItemID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                    item.ItemDescription = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                    item.DescriptionforSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetString();
                    item.SearchNameAll = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.SearchNameReleased = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetString();
                    item.SPType = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetString();
                    item.VehicleID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetString();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetValue<string>();
                    item.PackingSlipID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetValue<string>();
                    item.PackingSlipDeliveryDate = datePackingSlipDeliveryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    item.PackingQty = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetValue<int>();
                    item.YearDlv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetString();
                    item.MonthDlv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.ProductCategory = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString();
                    item.ProcateName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetString();
                    item.Genuine = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                    item.Motor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();
                    item.InvoiceNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetString();
                    item.SalesPrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetValue<decimal>();
                    item.DiscAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetValue<decimal>();
                    item.DiscPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 28).GetValue<decimal>();
                    item.ExchangeRate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 29).GetValue<decimal>();
                    item.Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetString();
                    item.SalesClass = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetString();
                    item.AmountGrossCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 32).GetValue<decimal>();
                    item.AmountDiscountCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 33).GetValue<decimal>();
                    item.AmountNetCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 34).GetValue<decimal>();
                    item.AmountGrossLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 35).GetValue<decimal>();
                    item.AmountDiscountLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 36).GetValue<decimal>();
                    item.AmountNetLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 37).GetValue<decimal>();
                    item.Status_GIT = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 38).GetString();
                    if (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetString() == "")
                    {
                        item.Date_Inv = null;
                    }
                    else
                    {
                        DateTime dateDateInv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetDateTime();
                        item.Date_Inv = dateDateInv.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    item.Year_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 40).GetString();
                    item.Month_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 41).GetString();
                    item.City = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 42).GetString();
                    item.Month_Odr = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 43).GetString();
                    item.CommercialStatus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 44).GetString();
                    item.GITPrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 45).GetValue<decimal>();
                    item.GITCost = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 46).GetValue<decimal>();

                    firstRow++;
                    totalRow++;
                    row++;

                    dataArray.Add(item);
                }

                DateTime minDate = PackingSlipDeliveryDate.Min(r => r.PackingSlipDeliveryDate.Value);
                DateTime maxDate = PackingSlipDeliveryDate.Max(r => r.PackingSlipDeliveryDate.Value);

                var spl = dbsi.Sales_D365ImporForm_SalesPackingQuantity.Where(w => w.PackingSlipDeliveryDate >= minDate && w.PackingSlipDeliveryDate <= maxDate);

                dbsi.Sales_D365ImporForm_SalesPackingQuantity.RemoveRange(spl);
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

        public ActionResult ImportRowPackingQuantity(Sales_D365ImporForm_SalesPackingQuantity item)
        {


            dbsi.Sales_D365ImporForm_SalesPackingQuantity.Add(item);
            var save = dbsi.SaveChanges();

            if (save > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = item
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

        [HttpPost]
        public JsonResult GetTotalRowSales(HttpPostedFileBase uploadSales, int startRow)
        {
            HttpPostedFileBase file = Request.Files["uploadSales"];

            if (uploadSales.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Files/Sales/D365ExcelForm/");
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                uploadSales.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int firstRow = 3;
                int totalRow = 0;
                int row = firstRow;
                List<Sales_D365ImporForm_Sales> PackingSlipDate = new List<Sales_D365ImporForm_Sales>();

                ArrayList dataArray = new ArrayList();

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 2).GetString() != "")
                {
                    var sp = new Sales_D365ImporForm_Sales();
                    var a = xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 18).GetDateTime();
                    sp.PackingSlipDate = a;
                    PackingSlipDate.Add(sp);

                    ObjSales item = new ObjSales();
                    item.SalesOrder = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();
                    item.SalesLineNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetValue<decimal>();
                    item.CustomerCode = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 4).GetString();
                    item.CustomerName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 5).GetString();
                    item.CustomerReference = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                    item.ExternalItemNumber = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 7).GetString();
                    item.ItemID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 8).GetString();
                    item.ProductName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                    item.ItemDescription = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 10).GetString();
                    item.SearchNameAll = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString();
                    item.SearchNameReleased = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 12).GetString();
                    item.SPType = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 13).GetString();
                    item.VehicleID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 14).GetString();
                    item.MasterVehicle = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 15).GetString();
                    item.ItemGroup = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 16).GetValue<string>();
                    item.PackingSlipID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 17).GetValue<string>();
                    item.PackingSlipDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 18).GetDateTime().ToString("MM/dd/yyyy");
                    item.PackingQty = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 19).GetValue<int>();
                    item.YearDlv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 20).GetString();
                    item.MonthDlv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 21).GetString();
                    item.ProductCategory = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 22).GetString();
                    item.ProcateName = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 23).GetString();
                    item.Genuine = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 24).GetString();
                    item.Motor = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 25).GetString();
                    item.OriginalInvoiceNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 26).GetString();
                    item.SalesPrice = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 27).GetValue<decimal>();
                    item.DiscAmount = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 28).GetValue<decimal>();
                    item.DiscPercent = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 29).GetValue<decimal>();
                    item.ExchangeRate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 30).GetValue<decimal>();
                    item.Currency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 31).GetString();
                    item.SalesClass = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 32).GetString();
                    item.AmountGrossCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 33).GetValue<decimal>();
                    item.AmountDiscountCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 34).GetValue<decimal>();
                    item.AmountNetCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 35).GetValue<decimal>();
                    item.AmountGrossLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 36).GetValue<decimal>();
                    item.AmountDiscountLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 37).GetValue<decimal>();
                    item.AmountNetLocalCurrency = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 38).GetValue<decimal>();
                    item.Status_GIT = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 39).GetString();
                    if (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 40).GetString() == "")
                    {
                        item.Date_Inv = null;
                    }
                    else
                    {
                        item.Date_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 40).GetDateTime().ToString("MM/dd/yyyy");
                    }
                    item.Year_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 41).GetString();
                    item.Month_Inv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 42).GetString();
                    item.City = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 43).GetString();
                    item.Month_Odr = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 44).GetString();
                    item.CommercialStatus = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 45).GetString();
                    item.VAT = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 46).GetValue<decimal>();
                    item.STDCost = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 47).GetValue<decimal>();
                    item.GITCost = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 48).GetValue<decimal>();
                    item.PeriodDLV = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 49).GetString();
                    item.PeriodINV = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 50).GetString();
                    item.DaysDueDateInv = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 51).GetValue<int>();
                    item.NPWP = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 52).GetString();
                    item.TaxInvoiceNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 53).GetString();
                    item.DescriptionforSalesReport = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 54).GetString();

                    firstRow++;
                    totalRow++;
                    row++;

                    dataArray.Add(item);
                }

                DateTime minDate = PackingSlipDate.Min(r => r.PackingSlipDate.Value);
                DateTime maxDate = PackingSlipDate.Max(r => r.PackingSlipDate.Value);

                var startDate = new DateTime(minDate.Year, minDate.Month, 1);
                var endDateStart = new DateTime(maxDate.Year, maxDate.Month, 1);
                var endDate = endDateStart.AddMonths(1).AddDays(-1);

                var spl = dbsi.Sales_D365ImporForm_Sales.Where(w => w.PackingSlipDate >= startDate && w.PackingSlipDate <= endDate);
                //ol = spl.ToList();

                dbsi.Sales_D365ImporForm_Sales.RemoveRange(spl);
                var del = dbsi.SaveChanges();

                var jsonResult =
                 Json(new
                 {
                     status = totalRow == 0 ? 0 : 1,
                     totalRow = totalRow,
                     fileName = fileName,
                     startDate = startDate,
                     endDate = endDate,
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
        public ActionResult ImportRowSales(Sales_D365ImporForm_Sales item)
        {
            int save = 0;
            if (item.PeriodDLV != null)
            {
                dbsi.Sales_D365ImporForm_Sales.Add(item);
                save = dbsi.SaveChanges();
            }

            if (save > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = item
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
        [HttpPost]
        public JsonResult GetDataBackOrder()
        {
            var spl = dbsi.V_Sales_Back_Order.ToList();
            var CountRow = dbsi.V_Sales_Back_Order.ToList();

            List<Tbl_V_Sales_Back_Order> Data = new List<Tbl_V_Sales_Back_Order>();
            var no = 0;
            foreach (var Item in spl)
            {
                var formattedDate = Convert.ToDateTime(Item.RequestedShipDate).ToString("dd MMM yyyy");
                no++;
                Data.Add(
                    new Tbl_V_Sales_Back_Order
                    {
                        No = no,
                        SalesOrder = Item.SalesOrder,
                        ItemNumber = Item.ItemNumber,
                        ProductName = Item.ProductName,
                        Customer = Item.Customer,
                        CustomerReference = Item.CustomerReference,
                        RequestedShipDate = formattedDate,
                        qty_order = Item.qty_order,
                        qty_packing = Item.qty_packing,
                        remaining_qty = Item.remaining_qty
                    }); ;
            }

            return Json(new
            {
                rows = Data,
                //totalNotFiltered = query,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ImportPickingList()
        {
            HttpFileCollectionBase uploadPickingList = Request.Files;

            if (uploadPickingList.Count == 0)
            {
                return Json(new { status = 0, msg = "No file uploaded." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                for (int i = 0; i < uploadPickingList.Count; i++)
                {
                    HttpPostedFileBase PickingList = uploadPickingList[i];

                    if (PickingList != null && PickingList.ContentLength > 0)
                    {

                        var allowedExtensions = new[] { ".xls", ".xlsx" };
                        if (!allowedExtensions.Contains(Path.GetExtension(PickingList.FileName).ToLower()))
                        {
                            return Json(new
                            {
                                status = 0,
                                msg = "Unsupported file format. Please upload an .xls or .xlsx file."
                            }, JsonRequestBehavior.AllowGet);
                        }

                        var importedData = new List<SCM_D365_Pickinglist>();
                        string mainPickingRoute = string.Empty;

                        using (var workbook = new XLWorkbook(PickingList.InputStream))
                        {
                            IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault();

                            if (worksheet == null)
                            {
                                return Json(new { status = 0, msg = "Worksheet not found within the Excel file." }, JsonRequestBehavior.AllowGet);
                            }

                            var pickingRouteCell = worksheet.Cell("N15");
                            if (!pickingRouteCell.IsEmpty())
                            {
                                mainPickingRoute = pickingRouteCell.GetValue<string>().Trim();
                            }
                            if (string.IsNullOrEmpty(mainPickingRoute))
                            {
                                return Json(new { status = 0, msg = "Picking Route value (N15) not found in the file." }, JsonRequestBehavior.AllowGet);
                            }

                            var firstDataRow = 28;

                            var lastRow = worksheet.LastRowUsed().RowNumber();


                            for (int row = firstDataRow; row <= lastRow; row++)
                            {
                                if (worksheet.Cell(row, 1).IsEmpty()) continue;

                                var item = new SCM_D365_Pickinglist();

                                item.PickingRoute = mainPickingRoute;

                                item.ItemNumber = worksheet.Cell(row, 1).GetValue<string>().Trim();

                                item.HandlingStatus = worksheet.Cell(row, 2).GetValue<string>().Trim();

                                item.Description = worksheet.Cell(row, 5).GetValue<string>().Trim();

                                item.OrderQuantity = worksheet.Cell(row, 7).GetValue<int>();

                                item.OrderUnit = worksheet.Cell(row, 8).GetValue<string>().Trim();

                                item.CWQuantity = worksheet.Cell(row, 9).GetValue<int>();

                                item.CWUnit = worksheet.Cell(row, 12).GetValue<string>().Trim();

                                item.DeliveryDate = worksheet.Cell(row, 13).GetValue<DateTime>();

                                item.Quantity = worksheet.Cell(row, 15).GetValue<int>();

                                item.Picked = worksheet.Cell(row, 16).GetValue<string>().Trim();

                                importedData.Add(item);
                            }
                        }
                        var deleteData = dbsi.SCM_D365_Pickinglist.Where(x => x.PickingRoute == mainPickingRoute).ToList();
                        dbsi.SCM_D365_Pickinglist.RemoveRange(deleteData);
                        dbsi.SCM_D365_Pickinglist.AddRange(importedData);
                        dbsi.SaveChanges();

                    }
                }
                return Json(new
                {
                    status = 1,
                    msg = $"Import successful!."
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = 0,
                    msg = "An error occurred while processing the Excel file. Check data types: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetDataPickingList()
        {
            var data = dbsi.SCM_D365_Pickinglist.ToList();

            List<Tbl_SCM_D365_Pickinglist> Data = new List<Tbl_SCM_D365_Pickinglist>();
            var no = 0;
            foreach (var Item in data)
            {
                var formattedDate = Convert.ToDateTime(Item.DeliveryDate).ToString("dd/MM/yyyy");
                no++;
                Data.Add(
                    new Tbl_SCM_D365_Pickinglist
                    {
                        No = no,
                        Id = Item.Id,
                        PickingRoute = Item.PickingRoute,
                        ItemNumber = Item.PickingRoute,
                        HandlingStatus = Item.ItemNumber,
                        Description = Item.Description,
                        OrderQuantity = Item.OrderQuantity,
                        OrderUnit = Item.OrderUnit,
                        CWQuantity = Item.CWQuantity,
                        CWUnit = Item.CWUnit,
                        DeliveryDate = formattedDate,
                        Quantity = Item.Quantity,
                        Picked = Item.Picked
                    }
                );
            }

            return Json(new
            {
                rows = Data,
            }, JsonRequestBehavior.AllowGet);
        }
        // helper to save upload log into database table Sales_D365UploadLog (creates table if missing)
        private void SaveUploadLog(string fileName, string uploadType, int rowCount)
        {
            try
            {
                // use NGK_AX database (AxConnection) to store upload log
                using (var ng = new NGK_AXConnection())
                {
                    // ensure table exists, create if missing
                    var connStr = ng.Database.Connection.ConnectionString;
                    using (var conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        string createSql = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Sales_D365UploadLog]') AND type in (N'U'))
                                            BEGIN
                                                CREATE TABLE [dbo].[Sales_D365UploadLog](
                                                    [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                                    [FileName] NVARCHAR(255) NULL,
                                                    [UploadType] NVARCHAR(50) NULL,
                                                    [UploadedBy] NVARCHAR(100) NULL,
                                                    [UploadTime] DATETIME NULL,
                                                    [RowCount] INT NULL
                                                )
                                            END";
                        using (var cmd = new SqlCommand(createSql, conn)) cmd.ExecuteNonQuery();
                    }

                    // insert via EF
                    string currUser = null;
                    try { currUser = ((ClaimsIdentity)User.Identity).GetUserId(); } catch { currUser = User?.Identity?.Name; }

                    var log = new Sales_D365UploadLog
                    {
                        FileName = fileName,
                        UploadType = uploadType,
                        UploadedBy = currUser,
                        UploadTime = DateTime.Now,
                        RowCount = rowCount
                    };
                    ng.Sales_D365UploadLog.Add(log);
                    ng.SaveChanges();
                }
            }
            catch
            {
                // do not throw from logging
            }
        }
    }
}