using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using NGKBusi.Areas.Sales.Models;
using NGKBusi.Models;
using NPOI.HSSF.UserModel;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.UserModel;
using Renci.SshNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NGKBusi.Areas.Sales.Controllers
{
    public class SalesOrderController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        SalesOrderConnection dbs = new SalesOrderConnection();
        // GET: Sales/SalesOrder
        [Authorize]
        public ActionResult Index()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 65
                        select new { usr, rol }).AsEnumerable().Select(s => s.usr);
                                   
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            return View();
        }

        [HttpPost]
        public JsonResult _GetData()
        {
            var iThirdParty = Request["iThirdParty"];
            var iType = Request["iType"];
            if (iThirdParty == "Yamaha")
            {
                return _GetDataYamaha();
            }
            else if (iThirdParty == "Honda")
            {
                return _GetDataHonda(iType);
            }
            else
            {
                return _GetDataSuzuki(iType);
            }
        }

        public JsonResult _GetDataYamaha()
        {

            if (Request.Files.Count > 0)
            {
                db.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Yamaha_Import");
                db.SaveChanges();

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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/Sales_EDI"), fname);
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
                                string query = "SELECT * FROM [" + sheetName + "]";
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
                                                dataArray.Add(ds.Tables[0].Rows[i][5].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][7].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][8].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][12].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][13].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][16].ToString());

                                                newData.Add(dataArray);
                                            }
                                        }
                                        return Json(newData, JsonRequestBehavior.AllowGet);
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
        public JsonResult _GetDataHonda(string iType)
        {

            if (Request.Files.Count > 0)
            {
                db.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Honda_Import");
                db.SaveChanges();

                try
                {
                    HttpFileCollectionBase files = Request.Files;
                    var newData = new List<ArrayList>();
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/Sales_EDI"), fname);
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
                                string query = "SELECT * FROM [" + sheetName + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            if (iType == "OEM")
                                            {
                                                if (int.TryParse(ds.Tables[0].Rows[i][8].ToString(), out int value))
                                                {
                                                    ArrayList dataArray = new ArrayList();
                                                    dataArray.Add(ds.Tables[0].Rows[i][6].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][2].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][16].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][8].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][1].ToString());

                                                    newData.Add(dataArray);
                                                }
                                            }
                                            else
                                            {
                                                //New Excel Version (13 Feb 2026)
                                                if (int.TryParse(ds.Tables[0].Rows[i][6].ToString(), out int value))
                                                {
                                                    ArrayList dataArray = new ArrayList(); 
                                                    dataArray.Add(ds.Tables[0].Rows[i][1].ToString()); //Ext_Item
                                                    dataArray.Add("99"); //Po_Number
                                                    dataArray.Add(DateTime.Now.ToString()); //Delivery_Date
                                                    dataArray.Add(ds.Tables[0].Rows[i][6].ToString()); // Quantity
                                                    dataArray.Add(null); //Sales_Order

                                                    newData.Add(dataArray);
                                                }
                                                //Old Excel Version
                                                //if (int.TryParse(ds.Tables[0].Rows[i][6].ToString(), out int value))
                                                //{
                                                //    ArrayList dataArray = new ArrayList();
                                                //    dataArray.Add(ds.Tables[0].Rows[i][4].ToString());
                                                //    dataArray.Add(ds.Tables[0].Rows[i][2].ToString());
                                                //    dataArray.Add(DateTime.Now.ToString());
                                                //    dataArray.Add(ds.Tables[0].Rows[i][6].ToString());
                                                //    dataArray.Add(null);

                                                //    newData.Add(dataArray);
                                                //}
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (System.IO.File.Exists(fname))
                        {
                            System.IO.File.Delete(fname);
                        }
                    }
                    return Json(newData, JsonRequestBehavior.AllowGet);

                    // Returns message that successfully uploaded  
                    //return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected Honda.");
            }
        }
        public JsonResult _GetDataSuzuki(string iType)
        {

            if (Request.Files.Count > 0)
            {
                db.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Import");
                db.SaveChanges();

                try
                {
                    HttpFileCollectionBase files = Request.Files;
                    var newData = new List<ArrayList>();
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/Sales_EDI"), fname);
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
                                string query = "SELECT * FROM [" + sheetName + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            if (iType == "OEM")
                                            {
                                                if (int.TryParse(ds.Tables[0].Rows[i][13].ToString(), out int value))
                                                {
                                                    ArrayList dataArray = new ArrayList();
                                                    dataArray.Add(ds.Tables[0].Rows[i][2].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][4].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][11].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][13].ToString());

                                                    newData.Add(dataArray);
                                                }
                                            }
                                            else
                                            {
                                                if (int.TryParse(ds.Tables[0].Rows[i][13].ToString(), out int value))
                                                {
                                                    ArrayList dataArray = new ArrayList();
                                                    dataArray.Add(ds.Tables[0].Rows[i][4].ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][2].ToString());
                                                    dataArray.Add(DateTime.Now.ToString());
                                                    dataArray.Add(ds.Tables[0].Rows[i][6].ToString());
                                                    dataArray.Add(null);

                                                    newData.Add(dataArray);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (System.IO.File.Exists(fname))
                        {
                            System.IO.File.Delete(fname);
                        }
                    }
                    return Json(newData, JsonRequestBehavior.AllowGet);

                    // Returns message that successfully uploaded  
                    //return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected Honda.");
            }
        }

        [HttpPost]
        public JsonResult UploadData()
        {

            var data = Request["uploadData"].Split('|');
            var iOEMSO = Request["iOEMSO"];
            var iOESSO = Request["iOESSO"];
            //this if because excel setting => HDR=No
            // Delivery_Date = DateTime.ParseExact(data[4], "dd/MM/yyyy",CultureInfo.InvariantCulture),
            if (int.TryParse(data[5], out int value))
            {
                db.Sales_EDI_Yamaha_Import.Add(new Sales_EDI_Yamaha_Import
                {
                    Ext_Item = data[0],
                    U_Code = data[1],
                    p_f = data[2],
                    Order_No = data[3].Length > 0 ? ("00000" + data[3]).Substring(("00000" + data[3]).Length - 5, 5) : null,
                    Delivery_Date = DateTime.ParseExact(data[4], "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    Quantity = Int32.Parse(data[5]),
                    Sales_Order_OEM = iOEMSO,
                    Sales_Order_OES = iOESSO
                });

                db.SaveChanges();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UploadDataHonda()
        {

            var data = Request["uploadData"].Split('|');
            var iOEMSO = Request["iOEMSO"];
            var iOESSO = Request["iOESSO"];
            var iType = Request["itype"];
            //this if because excel setting => HDR=No
            // Delivery_Date = DateTime.ParseExact(data[4], "dd/MM/yyyy",CultureInfo.InvariantCulture),
            if (int.TryParse(data[3], out int value))
            {
                db.Sales_EDI_Honda_Import.Add(new Sales_EDI_Honda_Import
                {
                    Ext_Item = data[0],
                    Po_Number = data[1],
                    Delivery_Date = iType == "OEM"? DateTime.ParseExact(data[2], "dd-MMM-yyyy", CultureInfo.InvariantCulture) : DateTime.Parse(data[2]),
                    Quantity = Int32.Parse(data[3]),
                    Sales_Order = iType == "OEM" ? iOEMSO : iOESSO,
                    Third_Party = iType == "OEM" ? "B0006" : "D0002",
                    Gate = iType == "OEM" ? data[4] : "",
                });

                db.SaveChanges();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult DownloadData()
        {
            db.Database.CommandTimeout = 3600;
            var _Data = db.V_AX_SalesOrder_Upload_Yamaha.ToList();

            var path = Server.MapPath("~/Files/Sales/EDI/Download/Yamaha SO.xls");

            System.IO.File.Copy(Server.MapPath("~/Files/Sales/EDI/Master/Yamaha SO.xls"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + path + "';Extended Properties='Excel 12.0;HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {


                    foreach (var data in _Data)
                    {
                        //queryText = string.Format("Insert into [Sales_order_line$] " +
                        //       "(`CurrencyCode`,`CustAccount`,`CustGroup`,`LineNum`,`SalesId`,`ShippingDateRequested`,`CustomerRef`,`ItemId`,`SalesQty`,`SalesPrice`,`LineDisc`,`LineAmount`,`InventLocationId`,`inventBatchId`,`InventSiteId`,`DefaultDimension`,`PriceUnit`,`TaxGroup`,`TaxItemGroup`,`SalesStatus`,`SalesType`,`SalesUnit`,`Name`,`ReceiptDateRequested`) " +
                        //       "values('" + data.Currency + "','" + data.Cust_Account + "','" + data.Cust_Group + "','" + data.Line_Num + "','" + data.Sales_ID + "','" + data.Shipping_Date + "','" + data.Customer_Ref + "','" + data.ItemID + "','" + data.Sales_Qty + "','" + data.Sales_Price + "'" +
                        //       ",'" + data.Line_Disc + "','" + data.Line_Amount + "','" + data.Invent_Location_ID + "','" + data.Invent_Batch_ID + "','" + data.Invent_Site_ID + "','" + data.Default_Dimension + "','" + data.Price_Unit + "','" + data.Tax_Group + "','" + data.Tax_Item_Group + "','" + data.Sales_Status + "','" + data.Sales_Type + "','" + data.Sales_Unit + "','" + data.Name + "','" + data.Receipt_Date_Requested + "')");


                        //Exclude Price
                        queryText = string.Format("Insert into [Sales_order_line$] " +
                               "(`CurrencyCode`,`CustAccount`,`CustGroup`,`LineNum`,`SalesId`,`ShippingDateRequested`,`CustomerRef`,`ItemId`,`SalesQty`,`SalesPrice`,`LineDisc`,`LineAmount`,`InventLocationId`,`inventBatchId`,`InventSiteId`,`DefaultDimension`,`PriceUnit`,`TaxGroup`,`TaxItemGroup`,`SalesStatus`,`SalesType`,`SalesUnit`,`Name`,`ReceiptDateRequested`,`ExternalItem`) " +
                               "values('" + data.Currency + "','" + data.Cust_Account + "','" + data.Cust_Group + "','" + data.Line_Num + "','" + data.Sales_ID + "','" + data.Shipping_Date + "','" + data.Customer_Ref + "','" + data.ItemID + "','" + data.Sales_Qty + "',''" +
                               ",'" + data.Line_Disc + "','" + data.Line_Amount + "','" + data.Invent_Location_ID + "','" + data.Invent_Batch_ID + "','" + data.Invent_Site_ID + "','" + data.Default_Dimension + "','" + data.Price_Unit + "','" + data.Tax_Group + "','" + data.Tax_Item_Group + "','" + data.Sales_Status + "','" + data.Sales_Type + "','" + data.Sales_Unit + "','" + data.Name + "','" + data.Receipt_Date_Requested + "','" + data.AX_Ext_Item + "')");

                        command.CommandText = queryText;
                        command.ExecuteNonQuery();
                    }
                }
            }
            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=\"Yamaha SO.xls\";");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("Index", "SalesOrder", new { area = "Sales" });
        }
        public ActionResult DownloadDataHonda()
        {
            db.Database.CommandTimeout = 3600;
            var _Data = db.V_AX_SalesOrder_Upload_Honda.ToList();

            var path = Server.MapPath("~/Files/Sales/EDI/Download/Honda SO.xls");

            System.IO.File.Copy(Server.MapPath("~/Files/Sales/EDI/Master/Honda SO.xls"), path, true);
            string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source='" + path + "';Extended Properties='Excel 12.0;HDR=YES;READONLY=FALSE;'";
            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
            {
                conn.Open();
                var queryText = "";
                using (OleDbCommand command = conn.CreateCommand())
                {

                    foreach (var data in _Data)
                    {
                        //queryText = string.Format("Insert into [Sales_order_line$] " +
                        //       "(`CurrencyCode`,`CustAccount`,`CustGroup`,`LineNum`,`SalesId`,`ShippingDateRequested`,`CustomerRef`,`ItemId`,`SalesQty`,`SalesPrice`,`LineDisc`,`LineAmount`,`InventLocationId`,`inventBatchId`,`InventSiteId`,`DefaultDimension`,`PriceUnit`,`TaxGroup`,`TaxItemGroup`,`SalesStatus`,`SalesType`,`SalesUnit`,`Name`,`ReceiptDateRequested`) " +
                        //       "values('" + data.Currency + "','" + data.Cust_Account + "','" + data.Cust_Group + "','" + data.Line_Num + "','" + data.Sales_ID + "','" + data.Shipping_Date + "','" + data.Customer_Ref + "','" + data.ItemID + "','" + data.Sales_Qty + "','" + data.Sales_Price + "'" +
                        //       ",'" + data.Line_Disc + "','" + data.Line_Amount + "','" + data.Invent_Location_ID + "','" + data.Invent_Batch_ID + "','" + data.Invent_Site_ID + "','" + data.Default_Dimension + "','" + data.Price_Unit + "','" + data.Tax_Group + "','" + data.Tax_Item_Group + "','" + data.Sales_Status + "','" + data.Sales_Type + "','" + data.Sales_Unit + "','" + data.Name + "','" + data.Receipt_Date_Requested + "')");


                        //Exclude Price
                        queryText = string.Format("Insert into [Sales_order_line$] " +
                               "(`CurrencyCode`,`CustAccount`,`CustGroup`,`LineNum`,`SalesId`,`ShippingDateRequested`,`CustomerRef`,`ItemId`,`SalesQty`,`SalesPrice`,`LineDisc`,`LineAmount`,`InventLocationId`,`inventBatchId`,`InventSiteId`,`DefaultDimension`,`PriceUnit`,`TaxGroup`,`TaxItemGroup`,`SalesStatus`,`SalesType`,`SalesUnit`,`Name`,`ReceiptDateRequested`,`ExternalItem`) " +
                               "values('" + data.Currency + "','" + data.Cust_Account + "','" + data.Cust_Group + "','" + data.Line_Num + "','" + data.Sales_ID + "','" + data.Shipping_Date + "','" + data.Customer_Ref + "','" + data.ItemID + "','" + data.Sales_Qty + "',''" +
                               ",'" + data.Line_Disc + "','" + data.Line_Amount + "','" + data.Invent_Location_ID + "','" + data.Invent_Batch_ID + "','" + data.Invent_Site_ID + "','" + data.Default_Dimension + "','" + data.Price_Unit + "','" + data.Tax_Group + "','" + data.Tax_Item_Group + "','" + data.Sales_Status + "','" + data.Sales_Type + "','" + data.Sales_Unit + "','" + data.Name + "','" + data.Receipt_Date_Requested + "','" + data.AX_Ext_Item + "')");

                        command.CommandText = queryText;
                        command.ExecuteNonQuery();
                    }
                }
            }
            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=\"Honda SO.xls\";");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("Index", "SalesOrder", new { area = "Sales" });
        }

        public ActionResult DownloadDataSuzuki()
        {
            string SourcePdfPath = Server.MapPath("~/SourceFiles/" + Session["fName"]);
            //string outputPath = Server.MapPath("~/DestinationFiles/");
            StringBuilder text = new StringBuilder();
            //using (PdfReader reader = new PdfReader(SourcePdfPath))
            //{
                //for (int i = 1; i <= reader.GetFileLength(); i++)
                //{
                //    text.AppendLine(PdfTextExtractor.GetTextFromPage(reader, i));
                //}
            //}
            //using (StreamWriter outputFile = new StreamWriter(System.IO.Path.Combine(outputPath, "Pdf2Text.txt")))
            //{
            //    outputFile.WriteLine(text);
            //}
            return Json("Test",JsonRequestBehavior.AllowGet);
        }

        public class Obj_Sales_EDI_Suzuki_Temp
        {
            [Name("No")]
            public int No { get; set; }
            [Name("Part Tag ID")]
            public string PartTagID { get; set; }
            [Name("Delivery Date")]
            public string DeliveryShipDate { get; set; }
            [Name("Org Name")]
            public string OrgName { get; set; }
            [Name("DN No")]
            public string SlipNo { get; set; }
            [Name("Supplier Code")]
            public string SupplierCode { get; set; }
            [Name("Vendor Name")]
            public string VendorName { get; set; }
            [Name("Supplier Part No")]
            public string SupplierPartNo { get; set; }
            [Name("Unloading Area")]
            public string UnloadingArea { get; set; }
            [Name("Location")]
            public string Location { get; set; }
            [Name("Remarks")]
            public string Remarks { get; set; }
            [Name("Part No")]
            public string PartNo { get; set; }
            [Name("Part Name")]
            public string PartName { get; set; }
            [Name("Qty")]
            public int DeliveryQty { get; set; }
            [Name("Production Lot No")]
            public string ProductionLotNo { get; set; }
            [Name("Delivery Time")]
            public string DeliveryTime { get; set; }
            [Name("Qty Unit")]
            public string QtyUnit { get; set; }
        }

        public class Obj_Sales_EDI_Suzuki_OES_Temp
        {
            [Name("Po No")]
            public string SlipNo { get; set; }
            [Name("Part Nomor")]
            public string PartNo { get; set; }
            [Name("Qty Order")]
            public int DeliveryQty { get; set; }
            [Name("PERIODE")]
            public string DeliveryShipDate { get; set; }
            [Name("PERIODE")]
            public DateTime DeliveryDate { get; set; }
        }

        public class DeliveryItem
        {
            public string SlipNo { get; set; }
            public string PartNo { get; set; }
            public int DeliveryQty { get; set; }
            public string DeliveryShipDate { get; set; }
            public string CustomerCode { get; set; }
        }

        public class DeliveryData
        {
            public List<DeliveryItem> data { get; set; }
        }
        [HttpPost]
        public JsonResult GetTotalSalesSuzukiOEM(HttpPostedFileBase fileOEMSuzuki)
        {
            // truncate existing data
            dbs.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Temp");
            dbs.SaveChanges();

            HttpPostedFileBase file = Request.Files["fileOEMSuzuki"];

            var records = new List<Obj_Sales_EDI_Suzuki_Temp>();

            string filePath = string.Empty;
            string path = Server.MapPath("~/Files/Sales/EDI/Download/");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            filePath = path + Path.GetFileName(fileOEMSuzuki.FileName);
            //string extension = Path.GetExtension(fileOEMSuzuki.FileName);
            fileOEMSuzuki.SaveAs(filePath);

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                records = csv.GetRecords<Obj_Sales_EDI_Suzuki_Temp>().ToList();

            }
            List<Sales_EDI_Suzuki_Temp> salesData = new List<Sales_EDI_Suzuki_Temp>();
            foreach (var rec in records)
            {
                salesData.Add(
                   new Sales_EDI_Suzuki_Temp
                   {
                       PartNo = rec.PartNo,
                       SlipNo = rec.SlipNo,
                       DeliveryShipDate = rec.DeliveryShipDate,
                       DeliveryQTY = rec.DeliveryQty,
                       CustomerCode = "B0008"
                   });
            }
            int totalRows = records.Count;
            return Json(new
            {
                status = totalRows == 0 ? 0 : 1,
                data = salesData,
                totalRow = totalRows
            }, JsonRequestBehavior.AllowGet);           

        }
        
        //[HttpPost]
        //public JsonResult GetTotalSalesSuzukiOES(HttpPostedFileBase fileOESSuzuki)
        //{
        //    // truncate existing data
        //    dbs.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Temp");
        //    dbs.SaveChanges();

        //    HttpPostedFileBase file = Request.Files["fileOEMSuzuki"];

        //    var records = new List<Obj_Sales_EDI_Suzuki_OES_Temp>();

        //    string filePath = string.Empty;
        //    string path = Server.MapPath("~/Files/Sales/EDI/Download/");
        //    if (!Directory.Exists(path))
        //    {
        //        Directory.CreateDirectory(path);
        //    }

        //    filePath = path + Path.GetFileName(fileOESSuzuki.FileName);
        //    //string extension = Path.GetExtension(fileOEMSuzuki.FileName);
        //    fileOESSuzuki.SaveAs(filePath);

        //    using (var reader = new StreamReader(filePath))
        //    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        //    {
        //        csv.Context.TypeConverterCache.AddConverter<DateTime>(new CustomDateConverter());
        //        records = csv.GetRecords<Obj_Sales_EDI_Suzuki_OES_Temp>().ToList();
        //        FillEmptyCells(records);
        //    }
        //    int totalRows = records.Count;
        //    return Json(new
        //    {
        //        status = totalRows == 0 ? 0 : 1,
        //        data = records,
        //        totalRow = totalRows
        //    }, JsonRequestBehavior.AllowGet);

        //}
        private void FillEmptyCells(List<Obj_Sales_EDI_Suzuki_OES_Temp> records)
        {
            string lastNonEmptySlipNo = null;
            string lastNonEmptyDeliveryDate = null;

            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.SlipNo))
                {
                    record.SlipNo = lastNonEmptySlipNo;
                }
                else
                {
                    lastNonEmptySlipNo = record.SlipNo;
                }

                if (string.IsNullOrEmpty(record.DeliveryShipDate))
                {
                    record.DeliveryShipDate = lastNonEmptyDeliveryDate;
                }
                else
                {
                    lastNonEmptyDeliveryDate = Convert.ToDateTime(record.DeliveryShipDate).ToString();
                }

            }
        }
        public ActionResult ImportRowSales(Sales_EDI_Suzuki_Temp item)
        {
            
            dbs.Sales_EDI_Suzuki_Temp.Add(item);
            var save = dbs.SaveChanges();

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
        public ActionResult GetDataSalesSuzukiOEM(string viewAction)
        {
            //var viewAction = Request.Form.Get("viewAction");
            
            if (viewAction == "default")
            {
                var spl = dbs.V_Sales_EDI.ToList();

                List<Tbl_Sales_EDI_Suzuki> CorrectionLists = new List<Tbl_Sales_EDI_Suzuki>();
                foreach (var Item in spl)
                {
                    var FinalDate = Convert.ToDateTime(Item.DeliveryShipDate);
                    var StrFinalDate = FinalDate.ToString("yyyy-MM-dd");
                    CorrectionLists.Add(
                        new Tbl_Sales_EDI_Suzuki
                        {
                            PONo = Item.PoNo,
                            ITEMID = Item.ITEMID,
                            ItemName = Item.ProductName,
                            QTY = Item.DeliveryQTY,
                            DeliveryDate = StrFinalDate
                        });
                }
                var CountRow = dbs.V_Sales_EDI.Count();
                return Json(new
                {
                    rows = CorrectionLists,
                    data = spl,
                    totalNotFiltered = CountRow,
                    total = CountRow,
                }, JsonRequestBehavior.AllowGet); 
            } else
            {
                var spl = dbs.V_Sales_EDI_HPM.ToList();

                List<Tbl_Sales_EDI_Suzuki> CorrectionLists = new List<Tbl_Sales_EDI_Suzuki>();
                foreach (var Item in spl)
                {
                    var FinalDate = Convert.ToDateTime(Item.DeliveryShipDate);
                    var StrFinalDate = FinalDate.ToString("yyyy-MM-dd");
                    CorrectionLists.Add(
                        new Tbl_Sales_EDI_Suzuki
                        {
                            PONo = Item.PoNo,
                            ITEMID = Item.ITEMID,
                            ItemName = Item.ProductName,
                            QTY = Item.DeliveryQTY,
                            DeliveryDate = StrFinalDate
                        });
                }
                var CountRow = dbs.V_Sales_EDI_HPM.Count();
                return Json(new
                {
                    rows = CorrectionLists,
                    data = spl,
                    totalNotFiltered = CountRow,
                    total = CountRow,
                }, JsonRequestBehavior.AllowGet); ;
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

        private void DeleteFile(string remotePath, WebClient client)
        {
            client.UploadString(remotePath, WebRequestMethods.Ftp.DeleteFile, "");
        }
        [HttpPost]        
        public async Task<ActionResult> upload(HttpPostedFileBase fileOESSuzuki)
        {
            HttpPostedFileBase file = Request.Files["fileOESSuzuki"];

            //string filePath = Server.MapPath("~/Files/Sales/EDI/Download/");
            //string fileName = "sales_suzuki";
            //filePath = filePath + fileName + ".xls";

            //fileOESSuzuki.SaveAs(filePath);



            //if (file != null && file.ContentLength > 0)
            //{
            //    // Ganti dengan alamat IP dan kredensial server FTP Anda
            //    string ftpServerIP = "ftp://192.168.1.248:21/Sales/EDI/Download";
            //    string ftpUsername = "it_admin";
            //    string ftpPassword = "N6K4dm1n!";

            //    string fileName = "sales_suzuki.xls";
            //    string remotePath = ftpServerIP + "/" + fileName;
            //    //int statusUpload;
            //    using (WebClient client = new WebClient())
            //    {
            //        client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

            //        try
            //        {
            //            // Cek apakah file sudah ada di server FTP
            //            if (FileExists(remotePath, client))
            //            {
            //                // Jika file sudah ada, hapus file lama
            //                DeleteFile(remotePath, client);
            //            }

            //            // Baca konten file ke dalam buffer
            //            byte[] fileBytes;
            //            using (Stream inputStream = file.InputStream)
            //            {
            //                using (MemoryStream memoryStream = new MemoryStream())
            //                {
            //                    inputStream.CopyTo(memoryStream);
            //                    fileBytes = memoryStream.ToArray();
            //                }
            //            }

            //            // Unggah file baru ke server FTP
            //            client.UploadData(remotePath, fileBytes);
            //            //statusUpload = 1;
            //        }
            //        catch (Exception)
            //        {
            //            //statusUpload = 0;
            //        }
            //    }
            //}

            if (file != null && file.ContentLength > 0)
            {
                // KONFIGURASI SERVER LINUX
                string sftpHost = "192.168.0.127";
                int sftpPort = 22;
                string sftpUser = "admin_it";
                string sftpPass = "@NM1_53rv3r_4dm1n!";

                // KONFIGURASI SERVER WINDOWS (FTP)
                //string sftpHost = "192.168.1.248";  
                //int sftpPort = 21;                 
                //string sftpUser = "administrator";  
                //string sftpPass = "N6K4dm1n";

                // PATH DI LINUX (Perhatikan format slash '/')
                // Pastikan folder ini sudah ada dan permission-nya benar (lihat panduan sebelumnya)
                string remoteDirectory = "/var/www/html/EDIwebservice/uploads/";
                string fileName = "sales_suzuki.xls";
                string fullRemotePath = remoteDirectory + fileName;

                // Menggunakan SftpClient dari library SSH.NET
                using (var client = new SftpClient(sftpHost, sftpPort, sftpUser, sftpPass))
                {
                    try
                    {
                        client.Connect();

                        // Cek apakah file sudah ada
                        if (client.Exists(fullRemotePath))
                        {
                            client.DeleteFile(fullRemotePath);
                        }

                        // Upload File
                        using (var stream = file.InputStream)
                        {
                            // Parameter true di akhir artinya "canOverride" (bisa menimpa)
                            client.UploadFile(stream, fullRemotePath, true);
                        }

                        client.Disconnect();

                        // statusUpload = 1; // Sukses
                    }
                    catch (Exception)
                    {

                         //Log error: ex.Message
                        // statusUpload = 0; // Gagal
                    }
                }
            }

            dbs.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Temp");
            dbs.SaveChanges();
            using (var client = new HttpClient())
            {
                // Ganti URL dengan URL web service yang sesuai
                string url = "http://192.168.0.127/EDIwebservice/impor_xls.php";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true, MustRevalidate = true };
                // Mengirim permintaan GET ke web service
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Membaca response JSON dan mengubahnya menjadi objek DeliveryData
                    var json = await response.Content.ReadAsStringAsync();
                    var deliveryData = JsonConvert.DeserializeObject<DeliveryData>(json);
                    int totalRows = deliveryData.data.Count();
                    return Json(new
                    {
                        status = totalRows == 0 ? 0 : 1,
                        data = deliveryData.data,
                        totalRow = totalRows

                    }, JsonRequestBehavior.AllowGet);;
                }
                else
                {
                    // Handle kesalahan jika ada
                    return View("Error");
                }
            }

        }
        public class Obj_Sales_EDI_HPM
        {
            public string DeliveryShipDate { get; set; }
            public string PartNo { get; set; }
            public string SlipNo { get; set; }
            public string DeliveryQTY { get; set; }
            public string CustomerCode { get; set; }
        }
        [HttpPost]
        public async Task<ActionResult> GetTotalSalesHPMOEM(HttpPostedFileBase fileOEMHPM)
        {
            HttpPostedFileBase file = Request.Files["fileOEMHPM"];

            //string filePath = Server.MapPath("~/Files/Sales/EDI/Download/");
            //string fileName = "OEM_HPM";
            //filePath = filePath + fileName + ".xlsx";

            //fileOEMHPM.SaveAs(filePath);

            //if (file != null && file.ContentLength > 0)
            //{
            //    // Ganti dengan alamat IP dan kredensial server FTP Anda
            //    string ftpServerIP = "ftp://192.168.1.248:21/Sales/EDI/Download";
            //    string ftpUsername = "it_admin";
            //    string ftpPassword = "N6K4dm1n!";

            //    string fileName = "OEM_HPM.xlsx";
            //    string remotePath = ftpServerIP + "/" + fileName;
            //    //int statusUpload;
            //    using (WebClient client = new WebClient())
            //    {
            //        client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

            //        try
            //        {
            //            // Cek apakah file sudah ada di server FTP
            //            if (FileExists(remotePath, client))
            //            {
            //                // Jika file sudah ada, hapus file lama
            //                DeleteFile(remotePath, client);
            //            }

            //            // Baca konten file ke dalam buffer
            //            byte[] fileBytes;
            //            using (Stream inputStream = file.InputStream)
            //            {
            //                using (MemoryStream memoryStream = new MemoryStream())
            //                {
            //                    inputStream.CopyTo(memoryStream);
            //                    fileBytes = memoryStream.ToArray();
            //                }
            //            }

            //            // Unggah file baru ke server FTP
            //            client.UploadData(remotePath, fileBytes);
            //            //statusUpload = 1;
            //        }
            //        catch (Exception)
            //        {
            //            //statusUpload = 0;
            //        }
            //    }
            //}

            if (file != null && file.ContentLength > 0)
            {
                // KONFIGURASI SERVER LINUX
                string sftpHost = "192.168.0.127";
                int sftpPort = 22;
                string sftpUser = "admin_it";
                string sftpPass = "@NM1_53rv3r_4dm1n!";

                // PATH DI LINUX (Perhatikan format slash '/')
                // Pastikan folder ini sudah ada dan permission-nya benar (lihat panduan sebelumnya)
                string remoteDirectory = "/var/www/html/EDIwebservice/uploads/";
                string fileName = "OEM_HPM.xlsx";
                string fullRemotePath = remoteDirectory + fileName;

                // Menggunakan SftpClient dari library SSH.NET
                using (var client = new SftpClient(sftpHost, sftpPort, sftpUser, sftpPass))
                {
                    try
                    {
                        client.Connect();

                        // Cek apakah file sudah ada
                        if (client.Exists(fullRemotePath))
                        {
                            client.DeleteFile(fullRemotePath);
                        }

                        // Upload File
                        using (var stream = file.InputStream)
                        {
                            // Parameter true di akhir artinya "canOverride" (bisa menimpa)
                            client.UploadFile(stream, fullRemotePath, true);
                        }

                        client.Disconnect();

                        // statusUpload = 1; // Sukses
                    }
                    catch (Exception)
                    {
                        //Log error: ex.Message
                        // statusUpload = 0; // Gagal
                    }
                }
            }

            dbs.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Temp");
            dbs.SaveChanges();

            
            using (var client = new HttpClient())
            {
                // Ganti URL dengan URL web service yang sesuai
                string url = "http://192.168.0.127/EDIwebservice/impor_xls_hpm_oem.php";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true, MustRevalidate = true };
                // Mengirim permintaan GET ke web service
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Membaca response JSON dan mengubahnya menjadi objek DeliveryData
                    var json = await response.Content.ReadAsStringAsync();
                    var deliveryData = JsonConvert.DeserializeObject<DeliveryData>(json);
                    int totalRows = deliveryData.data.Count();
                    return Json(new
                    {
                        status = totalRows == 0 ? 0 : 1,
                        data = deliveryData.data,
                        totalRow = totalRows
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    // Handle kesalahan jika ada
                    return View("Error");
                }
            }

        }
        [HttpPost]
        public JsonResult GetTotalSalesHPMOES(HttpPostedFileBase fileOESHPM)
        {
            // truncate existing data
            dbs.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Temp");
            dbs.SaveChanges();

            HttpPostedFileBase file = Request.Files["fileOESHPM"];

            string filePath = Server.MapPath("~/Files/Sales/EDI/Download/");
            string fileName = Path.GetFileName(file.FileName);
            filePath = filePath + fileName ;

            fileOESHPM.SaveAs(filePath);

            XLWorkbook xLWorkbook = new XLWorkbook(filePath);

            int firstRow = 5;
            int totalRow = 0;
            int row = firstRow;

            //List<Sales_D365ImporForm_SalesPackingQuantity> PackingSlipDeliveryDate = new List<Sales_D365ImporForm_SalesPackingQuantity>();

            ArrayList dataArray = new ArrayList();

            //List<Sales_EDI_Suzuki_Temp> salesData = new List<Sales_EDI_Suzuki_Temp>();
            while (xLWorkbook.Worksheets.Worksheet(1).Cell(firstRow, 2).GetString() != "")
            {

                Obj_Sales_EDI_HPM item = new Obj_Sales_EDI_HPM();
                item.DeliveryShipDate = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 2).GetString();
                item.PartNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 6).GetString();
                item.SlipNo = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 3).GetString();
                item.DeliveryQTY = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 9).GetString();
                item.CustomerCode = "D0005 ";

                firstRow++;
                totalRow++;
                row++;

                dataArray.Add(item);
            }

            var jsonResult =
             Json(new
             {
                 status = totalRow == 0 ? 0 : 1,
                 totalRow = totalRow,
                 data = dataArray,
                 filepath = filePath
             }, JsonRequestBehavior.AllowGet);

            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;

        }
        [HttpPost]
        public async Task<ActionResult> uploadKawasaki(HttpPostedFileBase fileOEMKawasaki)
        {
            HttpPostedFileBase file = Request.Files["fileOEMKawasaki"];

            //string filePath = Server.MapPath("~/Files/Sales/EDI/Download/");
            //string fileName = "OEM_Kawasaki";
            //filePath = filePath + fileName + ".xls";

            //fileOEMKawasaki.SaveAs(filePath);

            //if (file != null && file.ContentLength > 0)
            //{
            //    // Ganti dengan alamat IP dan kredensial server FTP Anda
            //    string ftpServerIP = "ftp://192.168.1.248:21/Sales/EDI/Download";
            //    string ftpUsername = "it_admin";
            //    string ftpPassword = "N6K4dm1n!";

            //    string fileName = "OEM_Kawasaki.xls";
            //    string remotePath = ftpServerIP + "/" + fileName;
            //    //int statusUpload;
            //    using (WebClient client = new WebClient())
            //    {
            //        client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

            //        try
            //        {
            //            // Cek apakah file sudah ada di server FTP
            //            if (FileExists(remotePath, client))
            //            {
            //                // Jika file sudah ada, hapus file lama
            //                DeleteFile(remotePath, client);
            //            }

            //            // Baca konten file ke dalam buffer
            //            byte[] fileBytes;
            //            using (Stream inputStream = file.InputStream)
            //            {
            //                using (MemoryStream memoryStream = new MemoryStream())
            //                {
            //                    inputStream.CopyTo(memoryStream);
            //                    fileBytes = memoryStream.ToArray();
            //                }
            //            }

            //            // Unggah file baru ke server FTP
            //            client.UploadData(remotePath, fileBytes);
            //            //statusUpload = 1;
            //        }
            //        catch (Exception)
            //        {
            //            //statusUpload = 0;
            //        }
            //    }
            //}

            if (file != null && file.ContentLength > 0)
            {
                // KONFIGURASI SERVER LINUX
                string sftpHost = "192.168.0.127";
                int sftpPort = 22;
                string sftpUser = "admin_it";
                string sftpPass = "@NM1_53rv3r_4dm1n!";

                // KONFIGURASI SERVER WINDOWS
                //string sftpHost = "192.168.1.248";
                //int sftpPort = 21;
                //string sftpUser = "administrator";
                //string sftpPass = "N6K4dm1n";

                // PATH DI LINUX (Perhatikan format slash '/')
                // Pastikan folder ini sudah ada dan permission-nya benar (lihat panduan sebelumnya)
                string remoteDirectory = "/var/www/html/EDIwebservice/uploads/";
                string fileName = "OEM_Kawasaki.xls";
                string fullRemotePath = remoteDirectory + fileName;

                // Menggunakan SftpClient dari library SSH.NET
                using (var client = new SftpClient(sftpHost, sftpPort, sftpUser, sftpPass))
                {
                    try
                    {
                        client.Connect();

                        // Cek apakah file sudah ada
                        if (client.Exists(fullRemotePath))
                        {
                            client.DeleteFile(fullRemotePath);
                        }

                        // Upload File
                        using (var stream = file.InputStream)
                        {
                            // Parameter true di akhir artinya "canOverride" (bisa menimpa)
                            client.UploadFile(stream, fullRemotePath, true);
                        }

                        client.Disconnect();

                        // statusUpload = 1; // Sukses
                    }
                    catch (Exception)
                    {
                        //Log error: ex.Message
                        // statusUpload = 0; // Gagal
                    }
                }
            }

            dbs.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Suzuki_Temp");
            dbs.SaveChanges();
            using (var client = new HttpClient())
            {
                // Ganti URL dengan URL web service yang sesuai
                string url = "http://192.168.0.127/EDIwebservice/impor_xls_kawasaki.php";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true, MustRevalidate = true };
                // Mengirim permintaan GET ke web service
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Membaca response JSON dan mengubahnya menjadi objek DeliveryData
                    var json = await response.Content.ReadAsStringAsync();
                    var deliveryData = JsonConvert.DeserializeObject<DeliveryData>(json);
                    int totalRows = deliveryData.data.Count();
                    return Json(new
                    {
                        status = totalRows == 0 ? 0 : 1,
                        data = deliveryData.data,
                        totalRow = totalRows
                    }, JsonRequestBehavior.AllowGet); ;
                }
                else
                {
                    // Handle kesalahan jika ada
                    return View("Error");
                }
            }

        }
    }

    public class CustomDateConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (DateTime.TryParseExact(text, "MMM-yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return DateTime.MinValue; // Nilai default jika konversi gagal
        }
    }

    
}