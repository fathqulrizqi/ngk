using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NGKBusi.Models;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using System.Data.Entity;
using System.Globalization;
using Newtonsoft.Json;

namespace NGKBusi.Areas.IT.Controllers
{

    public class ServiceRequestController : Controller
    {

        DefaultConnection db = new DefaultConnection();
        // GET: IT/ServiceRequest
        public ActionResult Index()
        {
            return RedirectToAction("List", "ServiceRequest", new { area = "IT" });
        }

        [Authorize]
        public ActionResult List()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var currUserSectionID = currUser.FindFirst("deptID").Value;
            ViewBag.SRList = db.IT_Service_Request_Data.Include(z => z.IT_Service_Request_Status).OrderByDescending(z => z.IssuedDate).ToList();
            if (currUserSectionID != "07")
            {
                ViewBag.SRList = db.IT_Service_Request_Data.Where(x => x.DeptID == currUserSectionID).Include(z => z.IT_Service_Request_Status).OrderByDescending(z => z.IssuedDate).ToList();
            }
            ViewBag.Categories = db.IT_Service_Request_Categories.Where(w => w.IsActive == true).ToList();
            ViewBag.IT_Staff = db.V_Users_Active.Where(u => u.DeptID == "07").ToList();
            ViewBag.Users = db.V_Users_Active.ToList();
            ViewBag.ReportVia = db.IT_Service_Request_Report_List.ToList();

            return View();
        }

        public ActionResult Queue()
        {
            ViewBag.Queue = db.IT_Service_Request_Data.Where(x => x.JobEnd == null && x.StatusID != 1 && x.StatusID != 5).Include(z => z.IT_Service_Request_Status).OrderBy(z => z.IssuedDate).ToList();

            return View();
        }

        [HttpPost]
        public JsonResult getSection(string userID)
        {
            var user = db.Users.Where(z => z.NIK == userID).FirstOrDefault();
            return Json(user);
        }

        [HttpPost]
        public JsonResult setRate(int id, int rate)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var SR = db.IT_Service_Request_Data.Where(z => z.id == id).FirstOrDefault();
            SR.Rate = rate;
            SR.RateDate = DateTime.Now;
            db.SaveChanges();

            return Json(DateTime.Now.ToString("yyyyMMdd"));
        }

        [HttpPost]
        public JsonResult cancelRequest(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var SR = db.IT_Service_Request_Data.Where(z => z.id == id).FirstOrDefault();
            SR.StatusID = 1;
            db.SaveChanges();

            return Json(id);
        }

        [HttpPost]
        public JsonResult continueRequest(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var SR = db.IT_Service_Request_Data.Where(z => z.id == id).FirstOrDefault();
            SR.StatusID = 2;
            SR.IssuedDate = DateTime.Now;
            db.SaveChanges();

            return Json(DateTime.Now.ToString("dd-MMM-yyyy"));
        }

        [HttpPost]
        public JsonResult StartRequest(int id)
        {
            var currUserID = User.Identity.GetUserId();
            db.Configuration.ProxyCreationEnabled = false;
            var SR = db.IT_Service_Request_Data.Where(z => z.id == id).FirstOrDefault();
            SR.StatusID = 3;
            SR.ActionBy = currUserID;
            SR.JobStart = DateTime.Now;
            db.SaveChanges();

            return Json(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
        }

        [HttpPost]
        public JsonResult HoldRequest(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var SR = db.IT_Service_Request_Data.Where(z => z.id == id).FirstOrDefault();
            SR.StatusID = 4;
            db.SaveChanges();

            return Json(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
        }

        [HttpPost]
        public JsonResult DoneRequest(int id,string comment, int rate)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            db.Configuration.ProxyCreationEnabled = false;
            var SR = db.IT_Service_Request_Data.Where(z => z.id == id).FirstOrDefault();
            SR.StatusID = 5;
            SR.JobEnd = DateTime.Now;
            SR.Comment = comment;
            SR.RateDate = DateTime.Now;
            SR.Rate = rate;
            db.SaveChanges();

            return Json(DateTime.Now.ToString("dd-MMM-yyyy HH:mm"));
        }

        [HttpPost]
        [Authorize]
        public ActionResult insertList(string[] iITStaff)
        {
            var dtStart = (Request["iJobStartDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iJobStartDate"] + " " + (Request["iJobStartTime"].Length == 0 ? "00:00": Request["iJobStartTime"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtEnd = (Request["iJobEndDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iJobEndDate"] + " " + (Request["iJobEndTime"].Length == 0 ? "00:00" : Request["iJobEndTime"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var rt = (Request["iSectionID"] == "07" ? 5 : 0);
            var currUser = (ClaimsIdentity)User.Identity;
                db.IT_Service_Request_Data.Add(new IT_Service_Request_Data
                {
                    IssuedBy = currUser.GetUserId(),
                    IssuedFor = Request["iUserID"],
                    IssuedDate = (dtStart != null? dtStart:DateTime.Now),
                    IssuedIP = currUser.FindFirst("ipAddress").Value,
                    IssuedPC = currUser.FindFirst("pcName").Value,
                    CategoryID = (Request["iCategory"] == null ? (int?)null : Int32.Parse(Request["iCategory"])),
                    Detail = Request["iDetail"],
                    DeptID = Request["iSectionID"],
                    ReportID = Int32.Parse(Request["iVia"]),
                    ActionBy = string.Join(",", iITStaff),
                    Action = Request["iAction"],
                    JobStart = dtStart,
                    JobEnd = dtEnd,
                    ForemanBy = "719.04.15",
                    Rate = rt,
                    StatusID = (dtStart != null && dtEnd != null ? 5: (dtStart != null ? 3: 2))
                });
                db.SaveChanges();

            return RedirectToAction("List", "ServiceRequest", new { area = "IT" });
        }

        [HttpPost]
        [Authorize]
        public JsonResult editList(string[] iITStaff)
        {
            //var dtStart = (Request["iJobStartDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iJobStartDate"] + " " + (Request["iJobStartTime"].Length == 0 ? "00:00" : Request["iJobStartTime"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));
            //var dtEnd = (Request["iJobEndDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iJobEndDate"] + " " + (Request["iJobEndTime"].Length == 0 ? "00:00" : Request["iJobEndTime"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Int32.Parse(Request["id"]);
            var currData = db.IT_Service_Request_Data.Where(x => x.id == currID).FirstOrDefault();
            currData.IssuedFor = Request["iUserID"];
            currData.Detail = Request["iDetail"];
            currData.CategoryID = Int32.Parse(Request["iCategory"]);
            currData.ReportID = Int32.Parse(Request["iVia"]);
            currData.DeptID = Request["iSectionID"];
            currData.Action = Request["iAction"];
            currData.ActionBy = string.Join(",", iITStaff);
            //if(dtStart != null) { 
            //    currData.JobStart = dtStart;
            //}
            //if (dtEnd != null)
            //{
            //    currData.JobEnd = dtEnd;
            //}
            db.SaveChanges();

            return Json(true);
        }

        [Authorize]
        public ActionResult Report()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = currUser.GetUserId();

            ViewBag.ReportList = db.IT_Service_Request_Header.Include(z => z.IT_Service_Request_Data).ToList();
            ViewBag.CreateList = db.IT_Service_Request_Data.Where(x => x.StatusID == 5 && x.HeaderID == null && x.ActionBy == currID).ToList();
            return View();
        }

        [HttpPost]
        [Authorize]
        public JsonResult CreateReport(int[] id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = currUser.GetUserId();
            var currYear = DateTime.Now.ToString("yy");
            var currHeader = db.IT_Service_Request_Header.Where(z => z.DocNo.Substring(0,5) == "XR-" + currYear).Max(x => x.DocNo.Substring(x.DocNo.Length - 4));
            var lastSeq = "0000" + (currHeader == null ? 1 : Int32.Parse(currHeader) + 1);
            var insertHeader = new IT_Service_Request_Header
            {
                DocNo = "XR-" + currYear + (lastSeq.Substring(lastSeq.Length - 4)),
                CreatedBy = currUser.GetUserId(),
                CreatedDate = DateTime.Now,
                CreatedIP = currUser.FindFirst("ipAddress").Value,
                CreatedPC = currUser.FindFirst("pcName").Value,
                CheckedBy = "719.04.15",
                ApprovedBy = "719.04.15",
                Status = 1
            };
            db.IT_Service_Request_Header.Add(insertHeader);
            db.SaveChanges();
            var items = db.IT_Service_Request_Data.Where(x => id.Contains(x.id)).ToList();
            items.ForEach(i => i.HeaderID = insertHeader.id);
            db.SaveChanges();

            return Json(insertHeader.id);
        }

        [HttpPost]
        [Authorize]
        public JsonResult reportChecked(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = currUser.GetUserId();
            var currDate = DateTime.Now;
            var updateHeader = db.IT_Service_Request_Header.Where(z => z.id == id).FirstOrDefault();
            updateHeader.CheckedDate = currDate;
            updateHeader.CheckedIP = currUser.FindFirst("ipAddress").Value;
            updateHeader.CheckedPC = currUser.FindFirst("pcName").Value;
            updateHeader.Status = 2;
            db.SaveChanges();
            var lastInserted = new { checkedDate = currDate, checkedID = updateHeader.CheckedBy, checkedName = updateHeader.UsersCheckedBy.Name };

            return Json(lastInserted);
        }

        [HttpPost]
        [Authorize]
        public JsonResult reportApproved(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = currUser.GetUserId();
            var currDate = DateTime.Now;
            var updateHeader = db.IT_Service_Request_Header.Where(z => z.id == id).FirstOrDefault();
            updateHeader.ApprovedDate = currDate;
            updateHeader.ApprovedIP = currUser.FindFirst("ipAddress").Value;
            updateHeader.ApprovedPC = currUser.FindFirst("pcName").Value;
            updateHeader.Status = 3;
            db.SaveChanges();
            var lastInserted = new { approvedDate = currDate, approvedID = updateHeader.CheckedBy, approvedName = updateHeader.UsersCheckedBy.Name };

            return Json(lastInserted);
        }

        [HttpPost]
        public JsonResult getReport(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            db.Configuration.ProxyCreationEnabled = false;
            var currData = db.IT_Service_Request_Data
                .Include(z => z.IT_Service_Request_Header.UsersCreatedBy)
                .Include(z => z.IT_Service_Request_Header.UsersCheckedBy)
                .Include(z => z.IT_Service_Request_Header.UsersApprovedBy)
                .Include(z => z.IT_Service_Request_Header)
                .Include(z => z.IT_Service_Request_Report_List).Where(z => z.HeaderID == id)
                .Select(z => new {
                    HeaderCreatedByID = z.IT_Service_Request_Header.CreatedBy
                    , HeaderCreatedByName = z.IT_Service_Request_Header.UsersCreatedBy.Name
                    , HeaderCreatedDate = z.IT_Service_Request_Header.CreatedDate
                    , HeaderCheckedByID = z.IT_Service_Request_Header.CheckedBy
                    , HeaderCheckedByName = z.IT_Service_Request_Header.UsersCheckedBy.Name
                    , HeaderCheckedDate = z.IT_Service_Request_Header.CheckedDate
                    , HeaderApprovedByID = z.IT_Service_Request_Header.ApprovedBy
                    , HeaderApprovedByName = z.IT_Service_Request_Header.UsersApprovedBy.Name
                    , HeaderApprovedDate = z.IT_Service_Request_Header.ApprovedDate
                    , Data = z,ReportName = z.IT_Service_Request_Report_List.Name
                    , DataActionBy = z.UsersActionBy.Name
                    , DataForemanBy = z.UsersForemanBy.Name
                });
            return Json(currData);
        }
    }
}