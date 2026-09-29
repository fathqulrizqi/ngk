using Microsoft.AspNet.Identity;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.SCM.Controllers
{
    [Authorize]
    public class WHKDController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        WHKDConnection dbw = new WHKDConnection();
        // GET: SCM/WHKD
        
        private string GenerateKode()
        {
            string prefix = "C" + DateTime.Now.ToString("yyMM"); // CYYMM
            string lastNumber = dbw.SCM_WHKD_Checker_Header
                .Where(d => d.NoTrans.StartsWith(prefix))
                .OrderByDescending(d => d.NoTrans)
                .Select(d => d.NoTrans)
                .FirstOrDefault();

            int nextNumber = 1; // Default jika belum ada

            if (!string.IsNullOrEmpty(lastNumber))
            {
                string lastDigits = lastNumber.Substring(lastNumber.Length - 3);
                if (int.TryParse(lastDigits, out int lastNumeric))
                {
                    nextNumber = lastNumeric + 1;
                }
            }

            return $"{prefix}-{nextNumber:000}";                   
        }
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Checker(string NoTrans)
        {
            var qHeader = dbw.SCM_WHKD_Checker_Header.Where(w => w.NoTrans == NoTrans).FirstOrDefault();

            ViewBag.Header = qHeader;
            return View();
        }

        public ActionResult CheckerData()
        {
            return View();
        }
        [HttpPost]
        public ActionResult GetCheckerData()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var spl = dbw.SCM_WHKD_Checker_Header.AsQueryable();
            spl = spl.Where(x => 1 == 1);
            var result = spl.OrderByDescending(o => o.CreateTime).ToList();
            List<Tbl_SCM_WHKD_Checker_Header> checkerList = new List<Tbl_SCM_WHKD_Checker_Header>();
            foreach (var rs in result)
            {
                var urlAction = Url.Action("Checker", "WHKD", new { area = "SCM", NoTrans = rs.NoTrans });

                //get create by name
                var qUser = db.V_Users_Active.Where(w => w.NIK == rs.CreateBy).FirstOrDefault();
                checkerList.Add(
                    new Tbl_SCM_WHKD_Checker_Header
                    {
                        
                        ID = rs.ID,
                        NoTrans = "<a href=\"" + urlAction + "\" title=\"view detail\">" + rs.NoTrans + "</a>",
                        SendingCard = rs.SendingCard,
                        TotalQuantity = rs.TotalQuantity,
                        CreateTime = rs.CreateTime.ToString("dd MMM yyyy"),
                        CreateBy = qUser != null ? qUser.Name : (rs.CreateBy ?? ""),

                    });
            }
            return Json(new { checkerList = checkerList }, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        public ActionResult AddSendingCard(SCM_WHKD_Checker_Header smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            SCM_WHKD_Checker_Header data = new SCM_WHKD_Checker_Header();
            data.NoTrans = GenerateKode();
            data.SendingCard = smodel.SendingCard;
            data.TotalQuantity = smodel.TotalQuantity;
            data.CreateBy = currUser;
            data.CreateTime = DateTime.Now;
            dbw.SCM_WHKD_Checker_Header.Add(data);
            int i = dbw.SaveChanges();
            if (i > 0)
            {
                return Json(new { status = 1, msg = "Success Add Data" }, JsonRequestBehavior.AllowGet);

            }
            else
            {
                return Json(new { status = 0, msg = "Failed Save Data" }, JsonRequestBehavior.AllowGet);

            }
        }
        [HttpPost]
        public ActionResult AddScanBox(SCM_WHKD_Checker_Detail smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            SCM_WHKD_Checker_Detail data = new SCM_WHKD_Checker_Detail();
            data.CheckerID = smodel.CheckerID;
            data.NoTrans = smodel.NoTrans;
            data.ScanItem = smodel.ScanItem;
            data.Quantity = smodel.Quantity;
            data.CreateBy = currUser;
            data.CreateTime = DateTime.Now;

            dbw.SCM_WHKD_Checker_Detail.Add(data);
            int i = dbw.SaveChanges();

            if (i > 0)
            {
                return Json(new { status = 1, msg = "Success Add Data" }, JsonRequestBehavior.AllowGet);

            }
            else
            {
                return Json(new { status = 0, msg = "Failed Save Data" }, JsonRequestBehavior.AllowGet);

            }
        }
        [HttpPost]
        public ActionResult GetCheckerDetailBox(int ID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var spl = dbw.SCM_WHKD_Checker_Detail.AsQueryable();
            spl = spl.Where(x => x.CheckerID == ID && x.IsDelete == 0);
            var result = spl.OrderByDescending(o => o.CreateTime).ToList();

            int no = 0;
            int count = 0;
            List<Tbl_SCM_WHKD_Checker_Detail> checkerList = new List<Tbl_SCM_WHKD_Checker_Detail>();
            foreach (var rs in result)
            {
                no++;
                count = count + rs.Quantity;
                var urlActionView = Url.Action("Checker", "WHKD", new { area = "SCM", NoTrans = rs.NoTrans });
                var urlActionDelete = Url.Action("DeleteItemBox", "WHKD", new { area = "SCM", NoTrans = rs.NoTrans });

                //get create by name
                var qUser = db.V_Users_Active.Where(w => w.NIK == rs.CreateBy).FirstOrDefault();
                checkerList.Add(
                    new Tbl_SCM_WHKD_Checker_Detail
                    {

                        ID = rs.ID,
                        No = no,
                        NoTrans = "<a href=\"" + urlActionView + "\" title=\"view detail\">" + rs.NoTrans + "</a>",
                        ScanItem = rs.ScanItem,
                        Quantity = rs.Quantity,
                        CreateTime = rs.CreateTime.ToString("dd MMM yyyy"),
                        CreateBy = qUser != null ? qUser.Name : (rs.CreateBy ?? ""),
                        btnAction = "<button type=\"button\" data-id=\"" + rs.ID + "\" title=\"view detail\"  class=\"btn btn-warning btn-sm\" id=\"btnDeleteItem\" ><i class=\"fa fa-trash\"></button>"

                    }); ;
            }
            return Json(new { checkerList = checkerList , count = count}, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        public ActionResult DeleteItemBox(int Id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            
            try
            {
                var data = dbw.SCM_WHKD_Checker_Detail.Where(w => w.ID == Id).FirstOrDefault();
                data.IsDelete = 1;
                var del = dbw.SaveChanges();
                if (del == 1)
                {
                    return Json(new
                    {
                        status = "1",
                        msg = "Item Deleted"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "0",
                        msg = "failed"
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "2",
                    msg = "failed"
                });
            }

        }


        // ------------------------------------     Delivery Note    ------------------------------------------------------------- //
        private string GetRomanMonth(int month)
        {
            string[] romanMonths = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
            return romanMonths[month - 1];
        }
        public string GenerateCodeDLV()
        {
            string monthRoman = GetRomanMonth(DateTime.Now.Month);
            string year = DateTime.Now.Year.ToString();
            string Comp = "NMI-WH";

            // Ambil data terakhir dari database berdasarkan kode yang sudah ada
            var countData = dbw.SCM_WHKD_DeliveryNote_Header.Where(w => w.IsDelete == 0).ToList().Count();
            int nextNumber = 1;
            if (countData > 0)
            {
                var lastEntry = dbw.SCM_WHKD_DeliveryNote_Header.AsEnumerable().Where(w => w.NoTrans.EndsWith($"/{year}") && w.IsDelete == 0)
                    .OrderByDescending(x => x.NoTrans)
                    .FirstOrDefault();
               
                if (lastEntry != null && !string.IsNullOrEmpty(lastEntry.NoTrans))
                {
                    string[] parts = lastEntry.NoTrans.Split('/');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }
            }
            else
            {
                nextNumber = 1;
            }
            string formattedNumber = nextNumber.ToString("D3");
            return $"{formattedNumber}/{Comp}/{monthRoman}/{year}";
        }
        public ActionResult DeliveryNote()
        {
            return View();
        }
        public ActionResult CreateDeliveryNote()
        {
            return View();
        }
        [HttpPost]
        public ActionResult GetDeliveryNoteData()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var spl = dbw.SCM_WHKD_DeliveryNote_Header.AsQueryable();
            spl = spl.Where(x => 1 == 1);
            var result = spl.OrderByDescending(o => o.CreateTime).ToList();
            List<Tbl_SCM_WHKD_DeliveryNote_Header> DeliveryNoteList = new List<Tbl_SCM_WHKD_DeliveryNote_Header>();
            foreach (var rs in result)
            {
                var urlAction = Url.Action("DeliveryNoteDetail", "WHKD", new { area = "SCM", NoTrans = rs.NoTrans });
                var urlPrintwithLogo = Url.Action("GeneratePdf", "WHKD", new { area = "SCM", Notrans = rs.NoTrans, logo = "yes" });
                var urlPrintNoLogo = Url.Action("GeneratePdf", "WHKD", new { area = "SCM", Notrans = rs.NoTrans, logo = "no" });
                // get total item
                int totalItem = dbw.SCM_WHKD_DeliveryNote_Detail.Where(w => w.HeaderID == rs.ID).ToList().Count();

                string PrintBtn;
                PrintBtn = "<div class='btn-group dropup'>";
                PrintBtn += "<button type =\"button\" class=\"btn btn-warning dropdown-toggle\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"false\">";
                PrintBtn +=  "<i class=\"fa fa-print\"></i> Print ";
                PrintBtn += "</button>";
                PrintBtn += "<div class=\"dropdown-menu\">";
                PrintBtn += "<a href=\"" + urlPrintwithLogo + "\" class=\"dropdown-item\" title=\"Print with Niterra Logo\" target=\"_blank\" >Print With Logo</a>";
                PrintBtn += "<a href=\"" + urlPrintNoLogo + "\" class=\"dropdown-item\" title=\"Print without Niterra Logo\" target=\"_blank\" >Print Without Logo</a>";
                PrintBtn += "</div></div>";


                //get create by name
                var qUser = db.V_Users_Active.Where(w => w.NIK == rs.CreateBy).FirstOrDefault();
                DeliveryNoteList.Add(
                    new Tbl_SCM_WHKD_DeliveryNote_Header
                    {
                        
                          ID = rs.ID,
                        NoTrans = "<a href=\"" + urlAction + "\" title=\"view detail\">" + rs.NoTrans + "</a>",
                        SupplierName = rs.SupplierName,
                        TotalItem = totalItem,
                        CreateTime = rs.CreateTime.ToString("dd MMM yyyy"),
                        CreateBy = qUser != null ? qUser.Name : (rs.CreateBy ?? ""),
                        BtnPrint = PrintBtn,
                        Status = rs.IsCommit == 1 ? "<span class=\"badge badge-info\">Commit</span>" : "<span class=\"badge badge-warning\">Not commit yet</span>"
                    });
            }
            return Json(new { DeliveryNoteList = DeliveryNoteList }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SubmitDeliveryNote(string SupplierName, string Address, string VehicleNumberPlate, string RecipientName, string[] ItemName, string[] Type, int[] Qty, string[] Description)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            SCM_WHKD_DeliveryNote_Header data = new SCM_WHKD_DeliveryNote_Header();
            data.NoTrans = GenerateCodeDLV();
            data.SupplierName = SupplierName;
            data.Address = Address;
            data.VehicleNumberPlate = VehicleNumberPlate;
            data.RecipientName = RecipientName;
            data.CreateBy = currUser;
            data.CreateTime = DateTime.Now;

            dbw.SCM_WHKD_DeliveryNote_Header.Add(data);
            int h = dbw.SaveChanges();
            int LastID = data.ID;
            /* */
            int rowIndex = 1;
            while (Request.Form[$"ItemName[{rowIndex}]"] != null)
            {
                var Lines = new SCM_WHKD_DeliveryNote_Detail
                {
                    HeaderID = LastID,
                    NoTrans = data.NoTrans,                  
                    ItemName = Request.Form[$"ItemName[{rowIndex}]"],
                    Type = Request.Form[$"Type[{rowIndex}]"],
                    Qty = int.Parse(Request.Form[$"Qty[{rowIndex}]"]),
                    Description = Request.Form[$"Description[{rowIndex}]"],
                    CreateBy = currUser,
                    CreateTime = DateTime.Now
                };
                dbw.SCM_WHKD_DeliveryNote_Detail.Add(Lines);

                rowIndex++;
            }
            dbw.SaveChanges();

            return Json(new
            {
                status = "1",
                msg = "success",
                supplierName = SupplierName,
                ItemName = ItemName,
                NoTrans = data.NoTrans
            });
        }
        [HttpPost]
        public ActionResult UpdateDeliveryNote(int HeaderID, string SupplierName, string Address, string VehicleNumberPlate, string RecipientName, string[] ItemName, string[] Type, int[] Qty, string[] Description)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            /* update header */
            var dlvHeader = dbw.SCM_WHKD_DeliveryNote_Header.Where(w => w.ID == HeaderID).FirstOrDefault();

            dlvHeader.SupplierName = SupplierName;
            dlvHeader.Address = Address;
            dlvHeader.UpdateBy = currUser;
            dlvHeader.UpdateTime = DateTime.Now;
            dlvHeader.RecipientName = RecipientName;
            dlvHeader.VehicleNumberPlate = VehicleNumberPlate;


            int h = dbw.SaveChanges();
            
            /* update existing item lines */
            var dlvLineExist = dbw.SCM_WHKD_DeliveryNote_Detail.Where(w => w.HeaderID == HeaderID && w.IsDelete == 0).ToList();
            foreach(var itemExist in dlvLineExist)
            {
                var lineExist = dbw.SCM_WHKD_DeliveryNote_Detail.Where(w => w.ID == itemExist.ID).FirstOrDefault();
                lineExist.ItemName = Request.Form[$"ItemName[{itemExist.ID}]"];
                lineExist.Type = Request.Form[$"Type[{itemExist.ID}]"];
                lineExist.Qty = int.Parse(Request.Form[$"Qty[{itemExist.ID}]"]);
                lineExist.Description = Request.Form[$"Description[{itemExist.ID}]"];
                dbw.SaveChanges();
            }

            /* save additional row */
            int rowIndex = 1;
            while (Request.Form[$"NewItemName[{rowIndex}]"] != null)
            {
                var Lines = new SCM_WHKD_DeliveryNote_Detail
                {
                    HeaderID = HeaderID,
                    NoTrans = dlvHeader.NoTrans,
                    ItemName = Request.Form[$"NewItemName[{rowIndex}]"],
                    Type = Request.Form[$"NewType[{rowIndex}]"],
                    Qty = int.Parse(Request.Form[$"NewQty[{rowIndex}]"]),
                    Description = Request.Form[$"NewDescription[{rowIndex}]"],
                    CreateBy = currUser,
                    CreateTime = DateTime.Now
                };
                dbw.SCM_WHKD_DeliveryNote_Detail.Add(Lines);

                rowIndex++;
            }
            dbw.SaveChanges();

            return Json(new
            {
                status = "1",
                msg = "success",
                supplierName = SupplierName,
                ItemName = ItemName,
                isUpdate = h
            });
        }
        public ActionResult DeliveryNoteDetail(string NoTrans)
        {
            var DlvHeader = dbw.SCM_WHKD_DeliveryNote_Header.Where(w => w.NoTrans == NoTrans).FirstOrDefault();
            var DlvDetail = dbw.SCM_WHKD_DeliveryNote_Detail.Where(w => w.HeaderID == DlvHeader.ID && w.IsDelete == 0).ToList();

            ViewBag.DlvHeader = DlvHeader;
            ViewBag.DlvDetail = DlvDetail;

            return View();
        }
        public ActionResult CommitDeliveryNote(int ID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var DlvHeader = dbw.SCM_WHKD_DeliveryNote_Header.Where(w => w.ID == ID).FirstOrDefault();

            DlvHeader.IsCommit = 1;
            DlvHeader.CommitBy = currUser;
            DlvHeader.CommitTime = DateTime.Now;

            int del = dbw.SaveChanges();

            return Json(new
            {
                status = "1",
                msg = "Delivery Note Is Commit",
                id = ID
            });
        }
        public ActionResult DeleteItem(int id)
        {
            var DlvDetail = dbw.SCM_WHKD_DeliveryNote_Detail.Where(w => w.ID == id && w.IsDelete == 0).FirstOrDefault();

            DlvDetail.IsDelete = 1;
           int del = dbw.SaveChanges();

            return Json(new
            {
                status = "1",
                msg = "success",
                id = id
            });
        }
        public ActionResult GeneratePdf(string NoTrans, string logo)
        {
            var DlvHeader = dbw.SCM_WHKD_DeliveryNote_Header.Where(w => w.NoTrans == NoTrans).FirstOrDefault();
            var DlvDetail = dbw.SCM_WHKD_DeliveryNote_Detail.Where(w => w.HeaderID == DlvHeader.ID && w.IsDelete == 0).ToList();
            var CreateByName = db.V_Users_Active.Where(w => w.NIK == DlvHeader.CreateBy).FirstOrDefault();

            ViewBag.DlvHeader = DlvHeader;
            ViewBag.DlvDetail = DlvDetail;
            ViewBag.CreateByName = CreateByName;
            ViewBag.Logo = logo;
            return new PartialViewAsPdf("GeneratePdf");
        }
    }
}