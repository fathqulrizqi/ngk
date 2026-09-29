using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using NGKBusi.Areas.SCM.Models;
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

namespace NGKBusi.Areas.SCM.Controllers
{
    [Authorize]
    public class SparepartStockInController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        SparepartStockInConnection dbsi = new SparepartStockInConnection();
        SparepartConnection dbsp = new SparepartConnection();
        // GET: SCM/SparepartRetur
        public ActionResult Index()
        {
            return View();
        }

        public class ExcelList
        {
            public int SNo { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }
        public ActionResult UploadReceipt()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            ViewBag.SparepartList = spl;

            return View();
        }
        [HttpPost]
        public ActionResult UploadReceipt(HttpPostedFileBase uploadFile, string dateReceipt, string remark)
        {

            //string filePath = string.Empty;
            HttpPostedFileBase file = Request.Files["uploadFile"];

            //var usersList = new List<ExcelList>();
            if (uploadFile.ContentLength > 0)
            {
                string filePath = "C:\\inetpub\\wwwroot\\NGKBusi\\NGKBusi\\Files\\SCM\\Sparepart\\Excel\\";
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                string HeaderID = DateTime.Now.ToString("yyyyMMddHHmmss");
                filePath = filePath + fileName + ".xlsx";

                uploadFile.SaveAs(filePath);

                XLWorkbook xLWorkbook = new XLWorkbook(filePath);

                int row = 31;
                int count = 0;
                // if row empty

                while (xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString() != "")
                {
                    string ITEMID = xLWorkbook.Worksheets.Worksheet(1).Cell(row, 1).GetString();
                    int QTY = int.Parse(xLWorkbook.Worksheets.Worksheet(1).Cell(row, 11).GetString());

                    SCM_Sparepart_Upload_StockIn item = new SCM_Sparepart_Upload_StockIn();
                    item.ITEMID = ITEMID;
                    item.QTYReceived = QTY;
                    item.HeaderID = HeaderID;
                    item.IsConfirm = 0;

                    dbsi.SCM_Sparepart_Upload_StockIn.Add(item);
                    var save = dbsi.SaveChanges();
                    if (save > 0)
                    {
                        count++;
                    }
                    row++;
                }
                if (count > 0)
                {
                    SCM_Sparepart_Upload_StockIn_Header header = new SCM_Sparepart_Upload_StockIn_Header();
                    header.HeaderID = HeaderID;
                    header.Remark = remark;
                    header.DateReceived = Convert.ToDateTime(dateReceipt);
                    header.Status = 1;

                    dbsi.SCM_Sparepart_Upload_StockIn_Header.Add(header);
                    dbsi.SaveChanges();

                    return Json(new
                    {
                        status = 1,
                        msg = "File Uploaded",
                        displayData = "prepared"
                    });
                }
                else
                {
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
        public ActionResult StockInDataHeader()
        {
            var spl = dbsi.SCM_Sparepart_Upload_StockIn_Header.ToList();
            var CountRow = dbsi.SCM_Sparepart_Upload_StockIn_Header.Count();

            List<Tbl_Sparepart_Upload_StockIn_Header> actions = new List<Tbl_Sparepart_Upload_StockIn_Header>();
            int no = 0;
            foreach (var Item in spl)
            {
                no++;
                var Tools = "";

                Tools = "<a href=\"#\" id=\"btnDetail\"  data-id=\"" + Item.HeaderID + "\" class=\"btn-sm btn-primary\" title=\"View Detail\"><i class=\"fa fa-eye\"></i></a>";

                var toDate = Convert.ToDateTime(Item.DateReceived);
                var endDate = toDate.ToString("yyyy-MM-dd");

                actions.Add(
                    new Tbl_Sparepart_Upload_StockIn_Header
                    {
                        DateReceived = endDate,
                        Remark = Item.Remark,
                        Action = Tools,
                        No = Convert.ToString(no)
                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult StockInDataDetail(string HeaderID)
        {
            var spl = dbsi.V_SCM_Sparepart_Upload_StockIn_Detail.Where(w => w.HeaderID == HeaderID).ToList();
            var CountRow = dbsi.V_SCM_Sparepart_Upload_StockIn_Detail.Where(w => w.HeaderID == HeaderID).Count();

            List<Tbl_SCM_Sparepart_Upload_StockIn_Detail> actions = new List<Tbl_SCM_Sparepart_Upload_StockIn_Detail>();
            int no = 0;
            foreach (var Item in spl)
            {
                no++;
                var Tools = "";

                Tools = "<a href=\"#\" id=\"btnDetail\"  data-id=\"" + Item.HeaderID + "\" class=\"btn-sm btn-primary\" title=\"View Detail\"><i class=\"fa fa-eye\"></i></a>";

                actions.Add(
                    new Tbl_SCM_Sparepart_Upload_StockIn_Detail
                    {
                        ProductName = Item.ProductName,
                        ITEMID = Item.ITEMID,
                        QTYReceived = Item.QTYReceived,
                        Action = Tools
                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ConfirmStockIn(string HeaderID)
        {
            Console.WriteLine(HeaderID);
            var spl = dbsi.SCM_Sparepart_Upload_StockIn_Header.Where(w => w.HeaderID == HeaderID).FirstOrDefault();
            spl.Status = 2;
            var update = dbsi.SaveChanges();

            var some = dbsi.SCM_Sparepart_Upload_StockIn.Where(x => HeaderID.Contains(x.HeaderID)).ToList();
            some.ForEach(a => a.IsConfirm = 1);
            var udpateDetail = dbsi.SaveChanges();

            var stockInDetail = dbsi.SCM_Sparepart_Upload_StockIn.Where(x => HeaderID.Contains(x.HeaderID)).ToList();
            foreach (var Item in stockInDetail)
            {
                var sparepart = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == Item.ITEMID).FirstOrDefault();
                sparepart.Quantity = sparepart.Quantity + Item.QTYReceived;
                dbsp.SaveChanges();
            }
            if (update > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = "Confirm Success",
                    headerID = HeaderID
                }, JsonRequestBehavior.AllowGet);
            } else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Confirm Failed",
                    headerID = HeaderID
                }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}