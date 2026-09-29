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
using System.Collections;

namespace NGKBusi.Areas.SCM.Controllers
{
    [Authorize]
    public class SparepartReturnController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        SparepartReturnConnection dbr = new SparepartReturnConnection();
        SparepartConnection dbsp = new SparepartConnection();
        // GET: SCM/SparepartRetur
        public ActionResult Index()
        {
            return View();
        }
        public class ItemDetailDTO
        {
            public string ReturnNo { get; set; }
            public DateTime Create_Time { get; set; }
            public byte Status { get; set; }
            public string Remark { get; set; }
            public string UserReturn { get; set; }

            // Other field you may need from the Product entity
        }
        public ActionResult DataReturn()
        {

            var spl = from sp in dbr.V_SCM_Sparepart_Return select sp;
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator")
            {
                Console.WriteLine('1');
                spl = spl.Where(w => w.IsAccept == 0 && w.IsReject == 0).OrderByDescending(o => o.Create_Time);
            }
            else
            {
                Console.WriteLine('2');
                spl = spl.Where(w => w.IsAccept == 0 && w.IsReject == 0).OrderByDescending(o => o.Create_Time);
            }

            var result = spl.ToList();

            ViewBag.ReturnList = result;
            ViewBag.status = 1;
            ViewBag.dateStart = null;
            ViewBag.dateEnd = null;
            ViewBag.autopick = "false";

            return View();
        }
        [HttpPost]
        public ActionResult ReturnRequestJson()
        {

            //var spl = db.V_AXItemMaster.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var spl = dbr.V_SCM_Sparepart_Return.Where(w => w.Status == "open");
            var result = spl.ToList();
            List<Tbl_SCM_Sparepart_Return_Request> returnList = new List<Tbl_SCM_Sparepart_Return_Request>();
            foreach(var returnRequest in result)
            {
                var status = "<h5><span class=\"badge badge-primary\">" + returnRequest.Status + "</span></h5>";
                returnList.Add(
                    new Tbl_SCM_Sparepart_Return_Request
                    {
                        Id = returnRequest.Id.ToString(),
                        ITEMID = returnRequest.ITEMID,
                        Name = returnRequest.Name,
                        CostName = returnRequest.CostName,
                        Quantity = returnRequest.Quantity,
                        RequestNo = returnRequest.RequestNo,
                        ReturnNo = returnRequest.ReturnNo,
                        ProductName = returnRequest.ProductName,
                        ItemGroup = returnRequest.ItemGroup,
                        ReturnNotes = returnRequest.ReturnNotes,
                        ProductCategory = returnRequest.ProductCategory,
                        Section = returnRequest.Section,
                        IsAccept = returnRequest.IsAccept,
                        IsReject = returnRequest.IsReject,
                        IsCancel = returnRequest.IsCancel,
                        Create_Time = Convert.ToDateTime(returnRequest.Create_Time).ToString("yyyy MMMM dd"),
                        Status = status

                    });
            }
            //    {
            //        spl = spl.Where(w => w.IsCancel == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
            //    }
            //if (status == 0)
            //{
            //    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator")
            //    {
            //        spl = spl.Where(w => w.IsCancel == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
            //    }
            //    else
            //    {
            //        spl = spl.Where(w => w.IsCancel == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.Create_By == currUser);
            //    }

            //}
            //else if (status == 1)
            //{
            //    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator")
            //    {
            //        spl = spl.Where(w => w.IsAccept == 0 && w.IsReject == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
            //    }
            //    else
            //    {
            //        spl = spl.Where(w => w.IsAccept == 0 && w.IsReject == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.Create_By == currUser);
            //    }

            //}
            //// if accepet
            //else if (status == 2)
            //{
            //    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator")
            //    {
            //        spl = spl.Where(w => w.IsAccept == 1 && w.IsReject == 0 && w.IsCancel == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
            //    }
            //    else
            //    {
            //        spl = spl.Where(w => w.IsAccept == 1 && w.IsReject == 0 && w.IsCancel == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.Create_By == currUser);
            //    }
            //}
            //else if (status == 3)
            //{
            //    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator")
            //    {
            //        spl = spl.Where(w => w.IsAccept == 0 && w.IsReject == 1 && w.IsCancel == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
            //    }
            //    else
            //    {
            //        spl = spl.Where(w => w.IsAccept == 0 && w.IsReject == 1 && w.IsCancel == 0 && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.Create_By == currUser);
            //    }
            //}
            


            var CheckedAction = "";

            //ViewBag.dater = spl;
            //return Json(new
            //{
            //    status = status,
            //    from = startDate,
            //    to = endDate,
            //    result = spl
            //});

            ViewBag.autopick = "true";
            ViewBag.CheckedAction = CheckedAction;
            return Json(new { rows = returnList }, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "AdminSparepart, Administrator, UserSparepart, WarehouseSparepart")]
        [HttpPost]
        public ActionResult GetReturnHistory()
        {
            //var spl = db.V_AXItemMaster.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var spl = from sp in dbr.V_SCM_Sparepart_Return select sp;
            //DateTime fromDate;
            //var startDate = "";

            //DateTime toDate;
            //var endDate = "";

            //if (dateTo != null || dateFrom != null)
            //{
            //    fromDate = DateTime.UtcNow.Date;
            //    startDate = fromDate.ToString("yyyy-MM-dd");

            //    toDate = DateTime.UtcNow.Date;
            //    endDate = toDate.ToString("yyyy-MM-dd");
            //}
            //else
            //{
            //    fromDate = Convert.ToDateTime(dateFrom);
            //    startDate = fromDate.ToString("yyyy-MM-dd");

            //    toDate = Convert.ToDateTime(dateTo);
            //    endDate = toDate.ToString("yyyy-MM-dd");
            //}
           
            var result = spl.Where(w=>w.Status != "open").ToList();
            List<Tbl_SCM_Sparepart_Return_History> actions = new List<Tbl_SCM_Sparepart_Return_History>();
            foreach (var returnList in result)
            {
                var status = "";
                if (returnList.IsReject == 1)
                {
                    status = "Rejected";
                } else if (returnList.IsAccept == 1)
                {
                    status = "Accepted";
                } else
                {
                    status = "Cancelled";
                }

                actions.Add(
                    new Tbl_SCM_Sparepart_Return_History
                    {
                        ITEMID = returnList.ITEMID,
                        Name = returnList.Name,
                        CostName = returnList.CostName,
                        Quantity = returnList.Quantity,
                        RequestNo = returnList.RequestNo,
                        ReturnNo = returnList.ReturnNo,
                        ProductName = returnList.ProductName,
                        ItemGroup = returnList.ItemGroup,
                        ReturnNotes = returnList.ReturnNotes,
                        ProductCategory = returnList.ProductCategory,
                        IsAccept = returnList.IsAccept,
                        IsReject = returnList.IsReject,
                        IsCancel = returnList.IsCancel,
                        Create_Time = Convert.ToDateTime(returnList.Create_Time).ToString("yyyy MMMM dd"),
                        Status = status

                    });
            }

            return Json(new { rows = actions }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ProcessReturn(int[] Id, string status)
        {
            Console.WriteLine(Id);

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var updateDataHeader = dbr.SCM_Sparepart_Return_Detail.Where(w => Id.Contains(w.Id)).ToList();
            foreach (var data in updateDataHeader)
            {
                
                if (status == "reject")
                {
                    data.IsAccept = 0;
                    data.IsReject = 1;
                }
                else if (status == "accept")
                {
                    data.IsAccept = 1;
                    data.IsReject = 0;

                    // simpan qty return di table scm_sparepart_request_detail di kolum Qty_Retur
                    var sparepart_request_detail = dbsp.SCM_Sparepart_Request_Detail.Where(w => w.ITEMID == data.ITEMID && w.RequestNo == data.RequestNo).FirstOrDefault();
                    sparepart_request_detail.Qty_Retur = data.Quantity;
                }
                data.Status = status;

                // data.CloseTime = DateTime.UtcNow.Date;
            }
            dbsp.SaveChanges();

            var updateHeader = dbr.SaveChanges();

            //return Json(requestNo, JsonRequestBehavior.AllowGet);


            if (updateHeader > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Item Accept",
                    displayData = "Accept"
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed Save Data",
                    displayData = "Opened"
                });
            }



        }


    }
}