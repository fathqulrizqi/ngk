using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using System.Security.Claims;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Data.SqlClient;

namespace NGKBusi.Areas.SCM.Controllers
{
    public class KDPartController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: SCM/KDPart
        public ActionResult Index()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Users_Menus_Roles.Where(z => z.userNIK == currUserID && z.menuID == 52).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }
            return View();
        }
        [Authorize]
        public ActionResult ShippingSchedule()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Users_Menus_Roles.Where(z => z.userNIK == currUserID && z.menuID == 52).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }
            ViewBag.ShippingSchedule = db.SCM_KDPart_Shipment_Schedule.ToList();

            return View();
        }
        [HttpPost]
        public JsonResult GetUsage()
        {
            var currDate = DateTime.Parse(Request["iDate"]);
            var gims = Request["iGIMS"];
            var type = Request["iType"];
            int usageQTY = _GetUsage(currDate, gims, type);

            return Json(Math.Abs(usageQTY), JsonRequestBehavior.AllowGet);
        }
        public int _GetUsage(DateTime _currDate, String _gims, string _type = "")
        {
            var workingDays = 21;
            var currDate = _currDate;
            var currDateAverage = DateTime.Now;
            var currDate1 = currDateAverage.AddMonths(-1);
            var currDate2 = currDateAverage.AddMonths(-2);
            var currDate3 = currDateAverage.AddMonths(-3);
            var dtMonth = currDate.Month;
            var dtYear = currDate.Year;
            var gims = _gims;
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            int usageQTY = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("getAXTotalUsage", conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                conn.Open();
                command.Parameters.AddWithValue("@Year", dtYear);
                command.Parameters.AddWithValue("@Month", dtMonth);
                command.Parameters.AddWithValue("@ItemID", gims);
                command.Parameters.Add(new SqlParameter("@RETURNVALUE", SqlDbType.Int));
                command.Parameters["@RETURNVALUE"].Direction = ParameterDirection.Output;
                command.ExecuteNonQuery();

                usageQTY = (int)command.Parameters["@RETURNVALUE"].Value;

                conn.Close();
            }

            if (Math.Abs(usageQTY) <= 0 || currDate == DateTime.Now || _type == "B")
            {
                int getUsageQTY = 0;
                using (var conn = new SqlConnection(connectionString))
                using (var command = new SqlCommand("getAXTotalUsage3LastMonth", conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    conn.Open();
                    command.Parameters.AddWithValue("@Year1", currDate1.Year);
                    command.Parameters.AddWithValue("@Month1", currDate1.Month);
                    command.Parameters.AddWithValue("@Year2", currDate2.Year);
                    command.Parameters.AddWithValue("@Month2", currDate2.Month);
                    command.Parameters.AddWithValue("@Year3", currDate3.Year);
                    command.Parameters.AddWithValue("@Month3", currDate3.Month);
                    command.Parameters.AddWithValue("@ItemID", gims);
                    command.Parameters.Add(new SqlParameter("@RETURNVALUE", SqlDbType.Int));
                    command.Parameters["@RETURNVALUE"].Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    getUsageQTY = (int)command.Parameters["@RETURNVALUE"].Value;

                    conn.Close();
                }
                usageQTY = (int)((Math.Abs(getUsageQTY) / (workingDays * 3)) * workingDays);
            }
            return usageQTY;
        }
        [HttpPost]
        public JsonResult GetArrival()
        {
            var currDate = DateTime.Parse(Request["iDate"]);
            var gims = Request["iGIMS"];
            var type = Request["iType"];

            double arrivalQTY = _GetArrival(currDate, gims, type);

            return Json(arrivalQTY, JsonRequestBehavior.AllowGet);
        }

        public double _GetArrival(DateTime _currDate,String _gims, string _type = "")
        {
            var currDate = _currDate;
            var dtMonth = currDate.Month;
            var dtYear = currDate.Year;
            var gims = _gims;

            double arrivalQTY = 0;
            if ((currDate.Year == DateTime.Now.Year && currDate.Month < DateTime.Now.Month) || _type == "A")
            {
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                arrivalQTY = (double)0;
                using (var conn = new SqlConnection(connectionString))
                using (var command = new SqlCommand("getAXTotalArrival", conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    conn.Open();
                    command.Parameters.AddWithValue("@Year", dtYear);
                    command.Parameters.AddWithValue("@Month", dtMonth);
                    command.Parameters.AddWithValue("@ItemID", gims);
                    command.Parameters.Add(new SqlParameter("@RETURNVALUE", SqlDbType.Int));
                    command.Parameters["@RETURNVALUE"].Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    arrivalQTY = (int)command.Parameters["@RETURNVALUE"].Value;

                    conn.Close();
                }
            }
            else
            {
                arrivalQTY = db.SCM_KDPart_Shipment_Schedule.Where(w => w.GIMS == gims && w.ETD.Month == dtMonth && w.ETD.Year == dtYear).Sum(s => s.Ship_Qty).GetValueOrDefault();
            }

            return arrivalQTY;
        }

        [HttpPost]
        public JsonResult GetStock()
        {
            var currDate = DateTime.Parse(Request["iDate"]);
            var gims = Request["iGIMS"];
            var type = Request["iType"];
            var stockQTY = _GetStock(currDate, gims, type);

            //(decimal)arrivalQTY + stockQTY + usageQTY
            return Json(stockQTY, JsonRequestBehavior.AllowGet);
        }
        public decimal _GetStock(DateTime _currDate, string _gims, string _type = "")
        {
            var currDate = _currDate.AddMonths(1);
            var dtMonth = currDate.Month;
            var dtYear = currDate.Year;
            var gims = _gims;
            decimal stockQTY = 0;
            if ((_currDate.Year >= DateTime.Now.Year && _currDate.Month >= DateTime.Now.Month) || _type == "B")
            {
                currDate = _currDate;
                dtMonth = currDate.Month;
                dtYear = currDate.Year;
                var currDate1 = currDate.AddMonths(-1);
                var currDate2 = currDate.AddMonths(-2);
                var currDate3 = currDate.AddMonths(-3);
                stockQTY = db.V_AXBegInventory.Where(w => w.AX_ItemID == gims && w.Month == dtMonth && w.Year == dtYear).Sum(s => (decimal?)s.Balance).GetValueOrDefault();

                //var arrivalQTY = db.SCM_KDPart_Shipment_Schedule.Where(w => w.GIMS == gims && w.ETD.Month == dtMonth && w.ETD.Year == dtYear).Sum(s => s.Ship_Qty).GetValueOrDefault();
                var arrivalQTY = _GetArrival(_currDate, _gims, _type);

                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                var usageQTY = _GetUsage(_currDate, _gims, _type);

                stockQTY = ((decimal)stockQTY - (decimal)Math.Abs(usageQTY)) + (decimal)arrivalQTY;
            }
            else
            {
                stockQTY = db.V_AXBegInventory.Where(w => w.AX_ItemID == gims && w.Month == dtMonth && w.Year == dtYear).Sum(s => (decimal?)s.Balance).GetValueOrDefault();
            }

            return stockQTY;
        }
        [HttpPost]
        public JsonResult GetStockLevel()
        {
            var currDate = DateTime.Parse(Request["iDate"]).AddMonths(1);
            var gims = Request["iGIMS"];



            return Json("", JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GetStockLevel_()
        {
            var workingDays = 21;
            var currDate = DateTime.Parse(Request["iDate"]).AddMonths(1);
            var dtMonth = currDate.Month;
            var dtYear = currDate.Year;
            var currDateAverage = DateTime.Now;
            var currDate1 = currDateAverage.AddMonths(-1);
            var currDate2 = currDateAverage.AddMonths(-2);
            var currDate3 = currDateAverage.AddMonths(-3);
            var gims = Request["iGIMS"];

            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            double arrivalQTY = 0;
            double arrivalQTYLevel = 0;
            if (DateTime.Parse(Request["iDate"]).AddMonths(1).Month < DateTime.Now.Month)
            {
                arrivalQTY = (double)0;
                using (var conn = new SqlConnection(connectionString))
                using (var command = new SqlCommand("getAXTotalArrival", conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    conn.Open();
                    command.Parameters.AddWithValue("@Year", dtYear);
                    command.Parameters.AddWithValue("@Month", dtMonth);
                    command.Parameters.AddWithValue("@ItemID", gims);
                    command.Parameters.Add(new SqlParameter("@RETURNVALUE", SqlDbType.Int));
                    command.Parameters["@RETURNVALUE"].Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    arrivalQTY = (int)command.Parameters["@RETURNVALUE"].Value;

                    conn.Close();
                }
            }
            else
            {
                arrivalQTY = db.SCM_KDPart_Shipment_Schedule.Where(w => w.GIMS == gims && w.ETD.Month == dtMonth && w.ETD.Year == dtYear).Sum(s => s.Ship_Qty).GetValueOrDefault();

                currDate = DateTime.Parse(Request["iDate"]);
                dtMonth = currDate.Month;
                dtYear = currDate.Year;
                arrivalQTYLevel = db.SCM_KDPart_Shipment_Schedule.Where(w => w.GIMS == gims && w.ETD.Month == dtMonth && w.ETD.Year == dtYear).Sum(s => s.Ship_Qty).GetValueOrDefault();
            }

            var usageQTY = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("getAXTotalUsage", conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                conn.Open();
                command.Parameters.AddWithValue("@Year", dtYear);
                command.Parameters.AddWithValue("@Month", dtMonth);
                command.Parameters.AddWithValue("@ItemID", gims);
                command.Parameters.Add(new SqlParameter("@RETURNVALUE", SqlDbType.Int));
                command.Parameters["@RETURNVALUE"].Direction = ParameterDirection.Output;
                command.ExecuteNonQuery();

                usageQTY = (int)command.Parameters["@RETURNVALUE"].Value;

                conn.Close();
            }


            var usageQTYLevel = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("getAXTotalUsage", conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                currDate = DateTime.Parse(Request["iDate"]);
                dtMonth = currDate.Month;
                dtYear = currDate.Year;

                conn.Open();
                command.Parameters.AddWithValue("@Year", dtYear);
                command.Parameters.AddWithValue("@Month", dtMonth);
                command.Parameters.AddWithValue("@ItemID", gims);
                command.Parameters.Add(new SqlParameter("@RETURNVALUE", SqlDbType.Int));
                command.Parameters["@RETURNVALUE"].Direction = ParameterDirection.Output;
                command.ExecuteNonQuery();

                usageQTYLevel = (int)command.Parameters["@RETURNVALUE"].Value;

                conn.Close();
            }


            if (Math.Abs(usageQTY) <= 0 || currDate.AddMonths(1) == DateTime.Now)
            {
                int getUsageQTY = 0;
                using (var conn = new SqlConnection(connectionString))
                using (var command = new SqlCommand("getAXTotalUsage3LastMonth", conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    conn.Open();
                    command.Parameters.AddWithValue("@Year1", currDate1.Year);
                    command.Parameters.AddWithValue("@Month1", currDate1.Month);
                    command.Parameters.AddWithValue("@Year2", currDate2.Year);
                    command.Parameters.AddWithValue("@Month2", currDate2.Month);
                    command.Parameters.AddWithValue("@Year3", currDate3.Year);
                    command.Parameters.AddWithValue("@Month3", currDate3.Month);
                    command.Parameters.AddWithValue("@ItemID", gims);
                    command.Parameters.Add(new SqlParameter("@RETURNVALUE", SqlDbType.Int));
                    command.Parameters["@RETURNVALUE"].Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    getUsageQTY = (int)command.Parameters["@RETURNVALUE"].Value;

                    conn.Close();
                }
                usageQTY = (int)((Math.Abs(getUsageQTY) / (workingDays * 3)) * workingDays);
            }
            decimal stockQTYLevel = 0;
            var stockQTY = db.V_AXBegInventory.Where(w => w.AX_ItemID == gims && w.Month == dtMonth && w.Year == dtYear).Sum(s => (decimal?)s.Balance).GetValueOrDefault();
            stockQTYLevel = stockQTY;
            if (DateTime.Parse(Request["iDate"]).Month >= DateTime.Now.Month)
            {
                currDate = DateTime.Parse(Request["iDate"]);
                dtMonth = currDate.Month;
                dtYear = currDate.Year;
                stockQTY = db.V_AXBegInventory.Where(w => w.AX_ItemID == gims && w.Month == dtMonth && w.Year == dtYear).Sum(s => (decimal?)s.Balance).GetValueOrDefault();
                
                stockQTYLevel = stockQTY;
                stockQTY = ((decimal)stockQTY - (decimal)Math.Abs(usageQTYLevel)) + (decimal)arrivalQTYLevel;
            }

            var stockLevel = Math.Abs(usageQTY) > 0 ? (decimal?)(((decimal)arrivalQTY + stockQTY) / Math.Abs(usageQTY)) :  (decimal?)0.00;
            return Json(stockQTYLevel + "##" + usageQTYLevel + "##" + arrivalQTYLevel, JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult Calculation()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = db.Users_Menus_Roles.Where(z => z.userNIK == currUserID && z.menuID == 52).FirstOrDefault();
            if (coll == null)
            {
                return View("UnAuthorized");
            }
            string[] typeArray = { "GASKET", "WIRE PACKING", "SHEET PACKING", "TERMINAL NUT" };
            ViewBag.Calculation = db.V_AXItemMaster.Where(w => w.ItemGroup == "KDRM-NGK-M" && typeArray.Contains(w.SearchName))
                .OrderBy(o => o.SearchName).OrderBy(o => o.ProductName).OrderBy(o => o.ITEMID)
                .Select(s => new calculationItems {
                    Type = s.SearchName,
                    Item_Name = s.ProductName,
                    GIMS = s.ITEMID
                }).Distinct();

            return View();
        }
        public class calculationItems
        {
            public string Type { get; set; }
            public string Item_Name { get; set; }
            public string GIMS { get; set; }
        }
        [HttpPost]
        public ActionResult UploadData()
        {
            if (Request.Files.Count > 0)
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
                    fname = Path.Combine(Server.MapPath("~/Files/Temp/KDPart"), fname);
                    file.SaveAs(fname);
                    DataSet dsHeaderName = new DataSet();
                    DataSet ds = new DataSet();

                    //A 32-bit provider which enables the use of

                    string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties='Excel 12.0;HDR=YES'";

                    using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                    {
                        conn.Open();
                        using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                        {
                            //string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            //string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            string sheetName = "Upload$";
                            string query = "SELECT * FROM [" + sheetName + "] where GIMS is not null and GIMS <> ''";
                            OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                            //DataSet ds = new DataSet();
                            adapter.Fill(ds, "Items");
                            if (ds.Tables.Count > 0)
                            {
                                if (ds.Tables[0].Rows.Count > 0)
                                {
                                    List<shippingScheduleData> newData = new List<shippingScheduleData>();
                                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                    {
                                        if (!String.IsNullOrEmpty(ds.Tables[0].Rows[i]["GIMS"].ToString()))
                                        {
                                            var ArrivalDate = 0; //Days
                                            if (ds.Tables[0].Rows[i]["Freight"].ToString() == "SEA")
                                            {
                                                switch (ds.Tables[0].Rows[i]["Origin"].ToString())
                                                {
                                                    case "JP": //Japan "redline" + 14 (All)
                                                        ArrivalDate = 28;
                                                        break;
                                                    case "TH": //Thailand
                                                        ArrivalDate = 19;
                                                        break;
                                                    case "MY": //Malaysia
                                                        ArrivalDate = 24;
                                                        break;
                                                    case "SH": //Shanghai
                                                        ArrivalDate = 24;
                                                        break;
                                                    case "BR": //Brazil
                                                        ArrivalDate = 70;
                                                        break;
                                                    case "US": //USA
                                                        ArrivalDate = 70;
                                                        break;
                                                    case "SG": //Singapore
                                                        ArrivalDate = 24;
                                                        break;
                                                    case "ID": //Indonesia
                                                        ArrivalDate = 2;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                switch (ds.Tables[0].Rows[i]["Origin"].ToString())
                                                {
                                                    case "JP": //Japan
                                                        ArrivalDate = 9;
                                                        break;
                                                    case "TH": //Thailand
                                                        ArrivalDate = 9;
                                                        break;
                                                    case "MY": //Malaysia
                                                        ArrivalDate = 9;
                                                        break;
                                                    case "SH": //Shanghai
                                                        ArrivalDate = 9;
                                                        break;
                                                    case "BR": //Brazil
                                                        ArrivalDate = 14;
                                                        break;
                                                    case "US": //USA
                                                        ArrivalDate = 14;
                                                        break;
                                                    case "SG": //Singapore
                                                        ArrivalDate = 9;
                                                        break;
                                                    case "ID": //Indonesia
                                                        ArrivalDate = 0;
                                                        break;
                                                }
                                            }
                                            newData.Add(new shippingScheduleData()
                                            {
                                                Origin = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Origin"].ToString()) ? ds.Tables[0].Rows[i]["Origin"].ToString() : "",
                                                Invoice = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Invoice"].ToString()) ? ds.Tables[0].Rows[i]["Invoice"].ToString() : "",
                                                ETD = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["ETD"].ToString()) ? DateTime.Parse(ds.Tables[0].Rows[i]["ETD"].ToString()).AddDays(ArrivalDate): default(DateTime),
                                                Freight = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Freight"].ToString()) ? ds.Tables[0].Rows[i]["Freight"].ToString() : "",
                                                GIMS = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["GIMS"].ToString()) ? ds.Tables[0].Rows[i]["GIMS"].ToString() : "",
                                                PO_Number = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["PO_Number"].ToString()) ? ds.Tables[0].Rows[i]["PO_Number"].ToString() : null,
                                                PO_Qty = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["PO_Qty"].ToString()) ? (double)ds.Tables[0].Rows[i]["PO_Qty"] : (double)0.0,
                                                Ship_Qty = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Ship_Qty"].ToString()) ? (double)ds.Tables[0].Rows[i]["Ship_Qty"] : (double)0.0,
                                                Original_ETD = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Original_ETD"].ToString()) ? (DateTime)ds.Tables[0].Rows[i]["Original_ETD"] : default(DateTime),
                                                Order_Month = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Order_Month"].ToString()) ? ds.Tables[0].Rows[i]["Order_Month"].ToString() : "",
                                                Correction_GIMS = !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Correction_GIMS"].ToString()) ? ds.Tables[0].Rows[i]["Correction_GIMS"].ToString() : null
                                            });
                                        }

                                    }

                                    SetUploadData(newData).ToString();
                                }

                            }
                        }
                    }
                    if (System.IO.File.Exists(fname))
                    {
                        System.IO.File.Delete(fname);
                    }
                }
            }
            return RedirectToAction("ShippingSchedule", "KDPart", new { area = "SCM" });
        }

        public class shippingScheduleData
        {
            public string Origin { get; set; }
            public string Invoice { get; set; }
            public DateTime ETD { get; set; }
            public string Freight { get; set; }
            public string GIMS { get; set; }
            public string PO_Number { get; set; }
            public double? PO_Qty { get; set; }
            public double? Ship_Qty { get; set; }
            public DateTime Original_ETD { get; set; }
            public string Order_Month { get; set; }
            public string Correction_GIMS { get; set; }
        }

        [HttpPost]
        public ActionResult SetUploadData(List<shippingScheduleData> dataList)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();

            //var combineData = dataList.Select(s => new { s.GIMS, s.PO_Number, s.PO_Qty }).Distinct().ToList();

            foreach (var i in dataList)
            {
                var gims = i.Correction_GIMS ?? i.GIMS;

                var checkData = db.SCM_KDPart_Shipment_Schedule.Where(w => w.GIMS == gims && w.PO_Number == i.PO_Number).ToList();

                if (checkData.Count() > 0)
                {
                 var deleteData = db.SCM_KDPart_Shipment_Schedule.RemoveRange(checkData);
                }
                var newData = db.SCM_KDPart_Shipment_Schedule.Add(new SCM_KDPart_Shipment_Schedule
                {
                    Origin = i.Origin,
                    Invoice = i.Invoice,
                    ETD = i.ETD,
                    Freight = i.Freight,
                    GIMS = i.GIMS,
                    PO_Number = i.PO_Number,
                    PO_Qty = i.PO_Qty,
                    Ship_Qty = i.Ship_Qty,
                    Original_ETD = i.Original_ETD,
                    Order_Month = i.Order_Month
                });
            }
            db.SaveChanges();

            return Json(dataList);
        }
    }
}