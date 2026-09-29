using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office.CustomXsn;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGKBusi.Areas.PE.Models;
using NGKBusi.Areas.Purchasing.Models;
using NGKBusi.Models;
using NPOI.POIFS.FileSystem;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Windows.Interop;


namespace NGKBusi.Areas.PE.Controllers
{
    [Authorize]
    public class PCRController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        PCRConnection dbm = new PCRConnection();

        // GET: PE/PCR
        public ActionResult Index()
        {
            ViewBag.NavHide = true;
            var _currUser = (ClaimsIdentity)User.Identity;
            ViewBag.CurrentUserNik = _currUser.GetUserId();
            ViewBag.CostName = _currUser.FindFirstValue("CostName");
            ViewBag.CostID = _currUser.FindFirstValue("CostID");
            ViewBag.DeptWithUsers = GetDeptWithUsers();
            ViewBag.UserPEList = db.V_Users_Active.Where(w => w.CostID == "PE").ToList();

            ViewBag.QMManager = db.V_Users_Active // Pak Venu
                    .Where(w => w.CostID == "AW" && w.PositionID == "8B")
                    .Select(x => new QMManagerInfo
                    {
                        NIK = x.NIK,
                        Name = x.Name
                    })
                    .FirstOrDefault();
            ViewBag.PlantManager = db.V_Users_Active // Pak Eko
                 .Where(w => w.CostID == "AN" && w.PositionID == "9A")
                 .Select(x => new PlantManagerInfo
                 {
                     NIK = x.NIK,
                     Name = x.Name
                 })
                 .FirstOrDefault();
            ViewBag.ProductionGM = db.V_Users_Active // Pak Kawaii
                 .Where(w => w.CostID == "ALF" && w.PositionID == "VIII")
                 .Select(x => new GMInfo
                 {
                     NIK = x.NIK,
                     Name = x.Name
                 })
                 .FirstOrDefault();
            return View();
        }
        public ActionResult _Create(int? headId)
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            string userNik = _currUser.GetUserId();

            string costName = _currUser.FindFirstValue("CostName");
            string divName = _currUser.FindFirstValue("divName");
            string costid = _currUser.FindFirstValue("CostID");
            string username = _currUser.FindFirstValue("fullName");

            ViewBag.Usernik = userNik;
            ViewBag.Fullname = username;
            ViewBag.CostID = costid;
            ViewBag.CostName = costName;
            // -- PHASE 1 --
            ViewBag.Userlist = db.V_Users_Active.ToList();
            ViewBag.UserlistDept = db.V_Users_Active
                       .Where(u => u.DivisionName == divName || u.NIK == "659.04.13" || u.NIK == "664.08.13")
                       .ToList();
            ViewBag.DTBRList = dbm.PE_PCR_DTBR_Master.ToList();
            ViewBag.ClassificationList = dbm.PE_PCR_Classification_Master.ToList();
            ViewBag.UserPEList = db.V_Users_Active.Where(w => w.CostID == "PE").ToList();
            ViewBag.DocNoList = db.PE_NumberingSystem_NumberingList.Where(w => w.DocNumber.Contains("CR"));
            ViewBag.PEManager = db.V_Users_Active
                       .Where(w => w.CostID == "PE" && w.PositionID == "7B")
                       .Select(x => new PEManagerInfo
                       {
                           NIK = x.NIK,
                           Name = x.Name
                       })
                       .FirstOrDefault();
            //ViewBag.PEManager = db.V_Users_Active
            //           .Where(w => w.CostID == "PE" && w.PositionID == "VI-A" && w.NIK == "PCR.MGR.01")
            //           .Select(x => new PEManagerInfo
            //           {
            //               NIK = x.NIK,
            //               Name = x.Name
            //           })
            //           .FirstOrDefault();

            // -- PHASE 2 --
            ViewBag.QMManager = db.V_Users_Active // Pak Venu
                    .Where(w => w.CostID == "AW" && w.PositionID == "8B")
                    .Select(x => new QMManagerInfo
                    {
                        NIK = x.NIK,
                        Name = x.Name
                    })
                    .FirstOrDefault();
            ViewBag.PlantManager = db.V_Users_Active // Pak Eko
                 .Where(w => w.CostID == "AN" && w.PositionID == "9A")
                 .Select(x => new PlantManagerInfo
                 {
                     NIK = x.NIK,
                     Name = x.Name
                 })
                 .FirstOrDefault();
            ViewBag.ProductionGM = db.V_Users_Active // Pak Kawaii
                 .Where(w => w.CostID == "ALF" && w.PositionID == "VIII")
                 .Select(x => new GMInfo
                 {
                     NIK = x.NIK,
                     Name = x.Name
                 })
                 .FirstOrDefault();
            ViewBag.DeptWithUsers = GetDeptWithUsers();

            if (headId.HasValue)
            {
                ViewBag.Title = "Detail PCR";
            }
            else
            {
                ViewBag.Title = "Form PCR";
            }
            return View();
        }
        private List<PE_PCR_Department_Master> GetDeptWithUsers()
        {
            var depts = dbm.PE_PCR_Department_Master.ToList();
            var userActive = db.V_Users_Active.ToList();

            // buat grouping by dept name
            return depts.GroupBy(d => d.Name)
                .Select(g =>
                {
                    var firstDept = g.First();
                    // ambil semua CostID yang masuk group ini
                    var costIds = g.Select(x => x.DbCostID).ToList();

                    return new PE_PCR_Department_Master
                    {
                        ID = firstDept.ID,
                        Name = firstDept.Name,
                        DbCostID = string.Join(",", costIds),
                        DbCostName = string.Join(",", g.Select(x => x.DbCostName)),
                        Users = userActive
                            .Where(u => costIds.Contains(u.CostID) && (u.PositionID == "VI-A" || u.PositionID == "VI-A1" || u.PositionID == "VII-A" || u.PositionID == "7A" || u.PositionID == "7B" || u.PositionID == "8A" || u.PositionID == "8B" || u.PositionID == "VI-B1" || u.PositionID == "VI-B" || u.PositionID == "VIII"))
                            .Select(u => new UserViewModel
                            {
                                NIK = u.NIK,
                                Name = u.Name
                            })
                            .ToList()
                    };
                })
                .ToList();
        }

        public ActionResult _Phase1()
        {
            return View();
        }

        public ActionResult _Phase2()
        {
            return View();
        }

        public ActionResult Phase2()
        {

            return View();
        }

        public ActionResult _Phase3()
        {
            return View();
        }

        [HttpGet]
        public ActionResult LoadPartial(string partialViewName)
        {
            if (string.IsNullOrEmpty(partialViewName))
                return new HttpStatusCodeResult(400, "Partial view name is required.");

            try
            {
                // pastikan selalu load dari folder Partial
                return PartialView($"Partial/{partialViewName}");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"Error loading partial view: {ex.Message}");
            }
        }
        private (int currentStep, int maxStep) CalculateProgress(
       int totalConcern,
       int totalSigned,
       int totalLength)
        {
            if (totalConcern == 0)
                return (7, 10);

            int current = 6;

            if (totalSigned >= 2)
                current += 3;

            int max = 6 + totalLength + 3;

            return (current, max);
        }


        [HttpGet]
        public JsonResult GetAllPcrData()
        {
            try
            {
                var tempData = (from h in dbm.PE_PCR_Head
                                join a in dbm.PE_PCR_Approval_Head on h.ID equals a.Head_ID into approvalJoin
                                from sub in approvalJoin.DefaultIfEmpty()
                                join s in dbm.PE_PCR_Approval_Status on sub.Approval_ID equals s.ID into statusJoin
                                from status in statusJoin.DefaultIfEmpty()
                                orderby h.ID descending
                                select new
                                {
                                    h.ID,
                                    h.Doc_No,
                                    h.Title,
                                    h.Dept,
                                    Date_Issued = sub.Date_Issued1 ?? h.Timestamps,
                                    Date_Approved = (sub == null) ? (DateTime?)null : sub.Date_Approved,
                                    First_Issued_Name = (sub == null) ? "" : sub.First_Issued_Name,
                                    Approval_Name = (status == null) ? "Draft" : status.Status_Name,
                                    Approval_ID = (sub == null) ? 0 : sub.Approval_ID,

                                    // Manager data
                                    ManagerData = dbm.PE_PCR_Management_Approval
                                        .Where(m => m.Head_ID == h.ID)
                                        .FirstOrDefault(),

                                    // Department approvals
                                    DepartmentApprovals = dbm.PE_PCR_Department_Approval
                                        .Where(d => d.Head_ID == h.ID)
                                        .Select(d => new
                                        {
                                            d.ID,
                                            d.Department_ID,
                                            d.Signer_Name,
                                            d.Signer_Date,
                                            d.Comment,
                                            StatusID = d.Status_ID
                                        }).ToList(),

                                    // 🔹 Ambil Plan & Actual dari Detail
                                    DetailData = dbm.PE_PCR_Detail
                                        .Where(det => det.Head_ID == h.ID)
                                        .Select(det => new
                                        {
                                            det.Plan,
                                            det.Actual
                                        })
                                        .FirstOrDefault(),

                                    ClassificationData = dbm.PE_PCR_DTBR
                                .Where(dt => dt.Head_ID == h.ID)
                                .Select(dt => dt.Classification)
                                .FirstOrDefault(),

                                    CostImpact = dbm.V_PE_PCR_Summary
                                .Where(v => v.Head_ID == h.ID)
                                .Select(v => v.CostImpact)
                                .FirstOrDefault(),

                                }).ToList();

                var finalData = tempData.Select(item => new
                {
                    item.ID,
                    item.Doc_No,
                    item.Title,
                    item.Dept,
                    item.Date_Issued,
                    item.Date_Approved,
                    item.First_Issued_Name,
                    item.Approval_Name,
                    item.Approval_ID,
                    item.DepartmentApprovals,
                    CostImpact = item.CostImpact,

                    // concern data
                    TotalConcern = item.DepartmentApprovals.Count,
                    TotalSigned = item.DepartmentApprovals.Count(a => a.StatusID >= 2),

                    // hitung progress
                    Progress = CalculateProgress(
                        item.DepartmentApprovals.Count,
                        item.DepartmentApprovals.Count(a => a.StatusID >= 2),
                        item.DepartmentApprovals.Count
                    ),


                    // 🔹 Tambahkan Plan & Actual ke JSON final
                    Plan = item.DetailData?.Plan,
                    Actual = item.ManagerData?.M1_Date,
                    Classification = item.ClassificationData,
                    // ManagerApproval diubah jadi array list
                    ManagerApproval = (item.ManagerData == null)
                        ? new List<object>()
                        : new List<object>
                        {
                   new {
                        Level = 1,
                        NIK = item.ManagerData.M1_NIK,
                        Status = item.ManagerData.M1_Status,
                        Name = item.ManagerData.M1_Name,
                        Date = item.ManagerData.M1_Date,
                    },
                    new {
                        Level = 2,
                        NIK = item.ManagerData.M2_NIK,
                        Status = item.ManagerData.M2_Status,
                        Name = item.ManagerData.M2_Name,
                        Date = item.ManagerData.M2_Date,
                    },
                    new {
                        Level = 3,
                        NIK = item.ManagerData.M3_NIK,
                        Status = item.ManagerData.M3_Status,
                        Name = item.ManagerData.M3_Name,
                        Date = item.ManagerData.M3_Date,
                    }
                        }
                }).ToList();

                return Json(new { success = true, data = finalData }, JsonRequestBehavior.AllowGet);
            }
            catch (DbUpdateException ex)
            {
                string msg = ex.Message;
                return Json(new { status = "Fail", message = msg });
            }
        }

        [HttpGet]
        public JsonResult GetPcrDataById()
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                string userNik = _currUser.GetUserId(); // current user NIK

                var tempData = (from h in dbm.PE_PCR_Head
                                join a in dbm.PE_PCR_Approval_Head on h.ID equals a.Head_ID into approvalJoin
                                from sub in approvalJoin.DefaultIfEmpty()
                                join s in dbm.PE_PCR_Approval_Status on sub.Approval_ID equals s.ID into statusJoin
                                from status in statusJoin.DefaultIfEmpty()
                                where sub.First_Issued_NIK == userNik
                                orderby h.ID descending
                                select new
                                {
                                    h.ID,
                                    h.Doc_No,
                                    h.Title,
                                    h.Dept,
                                    Date_Issued = sub.Date_Issued1 ?? h.Timestamps,
                                    Date_Approved = (sub == null) ? (DateTime?)null : sub.Date_Approved,
                                    First_Issued_Name = (sub == null) ? "" : sub.First_Issued_Name,
                                    Approval_Name = (status == null) ? "Draft" : status.Status_Name,
                                    Approval_ID = (sub == null) ? 0 : sub.Approval_ID,

                                    // Manager data
                                    ManagerData = dbm.PE_PCR_Management_Approval
                                        .Where(m => m.Head_ID == h.ID)
                                        .FirstOrDefault(),

                                    // Department approvals
                                    DepartmentApprovals = dbm.PE_PCR_Department_Approval
                                        .Where(d => d.Head_ID == h.ID)
                                        .Select(d => new
                                        {
                                            d.ID,
                                            d.Department_ID,
                                            d.Signer_Name,
                                            d.Signer_Date,
                                            d.Comment,
                                            StatusID = d.Status_ID
                                        }).ToList(),

                                    // 🔹 Ambil Plan & Actual dari Detail
                                    DetailData = dbm.PE_PCR_Detail
                                        .Where(det => det.Head_ID == h.ID)
                                        .Select(det => new
                                        {
                                            det.Plan,
                                            det.Actual
                                        })
                                        .FirstOrDefault(),

                                    ClassificationData = dbm.PE_PCR_DTBR
                                .Where(dt => dt.Head_ID == h.ID)
                                .Select(dt => dt.Classification)
                                .FirstOrDefault(),

                                    CostImpact = dbm.V_PE_PCR_Summary
                                 .Where(v => v.Head_ID == h.ID)
                                 .Select(v => v.CostImpact)
                                 .FirstOrDefault(),

                                }).ToList();


                // 2. Transformasi objek ManagerData menjadi Array/List
                var finalData = tempData.Select(item => new
                {
                    item.ID,
                    item.Doc_No,
                    item.Title,
                    item.Dept,
                    item.Date_Issued,
                    item.Date_Approved,
                    item.First_Issued_Name,
                    item.Approval_Name,
                    item.Approval_ID,
                    item.DepartmentApprovals,
                    CostImpact = item.CostImpact,
                    Plan = item.DetailData?.Plan,
                    Actual = item.ManagerData?.M1_Date,
                    Classification = item.ClassificationData,
                    // Proyeksi objek tunggal menjadi Array (List)
                    ManagerApproval = (item.ManagerData == null)
                        ? new List<object>() // Array kosong jika data manager tidak ada
                        : new List<object>
                        {
                     new {
                        Level = 1,
                        NIK = item.ManagerData.M1_NIK,
                        Status = item.ManagerData.M1_Status,
                        Name = item.ManagerData.M1_Name,
                        Date = item.ManagerData.M1_Date,
                    },
                    new {
                        Level = 2,
                        NIK = item.ManagerData.M2_NIK,
                        Status = item.ManagerData.M2_Status,
                        Name = item.ManagerData.M2_Name,
                        Date = item.ManagerData.M2_Date,
                    },
                    new {
                        Level = 3,
                        NIK = item.ManagerData.M3_NIK,
                        Status = item.ManagerData.M3_Status,
                        Name = item.ManagerData.M3_Name,
                        Date = item.ManagerData.M3_Date,
                    }
                        }
                }).ToList(); // <-- Proyeksi C# menjadi array/list
                return Json(new { success = true, data = finalData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetPcrDataByDept()
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                // Asumsikan GetUserId() adalah Extension Method
                string userNik = _currUser.GetUserId();
                string deptName = _currUser.FindFirstValue("deptName");
                string divName = _currUser.FindFirstValue("divName");
                Debug.WriteLine("DeptName: " + deptName);
                Debug.WriteLine("DivName: " + divName);
                var divisionName = db.V_Users_Active
                    .Where(w => w.DeptName == deptName)
                    .Select(w => w.DivisionName)
                    .FirstOrDefault();
                var costNames = db.V_Users_Active
                .Where(u => u.DivisionName == divName)
                .Select(u => u.CostName)
                .Distinct()
                .ToList();
                var tempData = (from h in dbm.PE_PCR_Head
                                join a in dbm.PE_PCR_Approval_Head on h.ID equals a.Head_ID into approvalJoin
                                from sub in approvalJoin.DefaultIfEmpty()
                                join s in dbm.PE_PCR_Approval_Status on sub.Approval_ID equals s.ID into statusJoin
                                from status in statusJoin.DefaultIfEmpty()
                                where costNames.Contains(h.Dept)
                                orderby h.ID descending
                                select new
                                {
                                    h.ID,
                                    h.Doc_No,
                                    h.Title,
                                    h.Dept,
                                    Date_Issued = sub.Date_Issued1 ?? h.Timestamps,
                                    Date_Approved = (sub == null) ? (DateTime?)null : sub.Date_Approved,
                                    First_Issued_Name = (sub == null) ? "" : sub.First_Issued_Name,
                                    Approval_Name = (status == null) ? "Draft" : status.Status_Name,
                                    Approval_ID = (sub == null) ? 0 : sub.Approval_ID,

                                    // Manager data
                                    ManagerData = dbm.PE_PCR_Management_Approval
                                        .Where(m => m.Head_ID == h.ID)
                                        .FirstOrDefault(),

                                    // Department approvals
                                    DepartmentApprovals = dbm.PE_PCR_Department_Approval
                                        .Where(d => d.Head_ID == h.ID)
                                        .Select(d => new
                                        {
                                            d.ID,
                                            d.Department_ID,
                                            d.Signer_Name,
                                            d.Signer_Date,
                                            d.Comment,
                                            StatusID = d.Status_ID
                                        }).ToList(),

                                    // 🔹 Ambil Plan & Actual dari Detail
                                    DetailData = dbm.PE_PCR_Detail
                                        .Where(det => det.Head_ID == h.ID)
                                        .Select(det => new
                                        {
                                            det.Plan,
                                            det.Actual
                                        })
                                        .FirstOrDefault(),

                                    ClassificationData = dbm.PE_PCR_DTBR
                                .Where(dt => dt.Head_ID == h.ID)
                                .Select(dt => dt.Classification)
                                .FirstOrDefault(),

                                    CostImpact = dbm.V_PE_PCR_Summary
                                 .Where(v => v.Head_ID == h.ID)
                                 .Select(v => v.CostImpact)
                                 .FirstOrDefault(),

                                }).ToList();

                // 2. Transformasi objek ManagerData menjadi Array/List
                var finalData = tempData.Select(item => new
                {
                    item.ID,
                    item.Doc_No,
                    item.Title,
                    item.Dept,
                    item.Date_Issued,
                    item.Date_Approved,
                    item.First_Issued_Name,
                    item.Approval_Name,
                    item.Approval_ID,
                    item.DepartmentApprovals,
                    CostImpact = item.CostImpact,

                    Plan = item.DetailData?.Plan,
                    Actual = item.ManagerData?.M1_Date,
                    Classification = item.ClassificationData,
                    // Proyeksi objek tunggal menjadi Array (List)
                    ManagerApproval = (item.ManagerData == null)
                        ? new List<object>() // Array kosong jika data manager tidak ada
                        : new List<object>
                        {
                  new {
                        Level = 1,
                        NIK = item.ManagerData.M1_NIK,
                        Status = item.ManagerData.M1_Status,
                        Name = item.ManagerData.M1_Name,
                        Date = item.ManagerData.M1_Date,
                    },
                    new {
                        Level = 2,
                        NIK = item.ManagerData.M2_NIK,
                        Status = item.ManagerData.M2_Status,
                        Name = item.ManagerData.M2_Name,
                        Date = item.ManagerData.M2_Date,
                    },
                    new {
                        Level = 3,
                        NIK = item.ManagerData.M3_NIK,
                        Status = item.ManagerData.M3_Status,
                        Name = item.ManagerData.M3_Name,
                        Date = item.ManagerData.M3_Date,
                    }
                        }
                }).ToList(); // <-- Proyeksi C# menjadi array/list



                return Json(new { success = true, data = finalData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Logging exception di sini sangat disarankan
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetConcernDataByDept()
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                string costID = _currUser.FindFirstValue("CostID");
                string userNik = _currUser.GetUserId();


                // 1. Ambil daftar DepartmentMaster.ID yang costnya sama dengan user
                var deptIds = dbm.PE_PCR_Department_Master
                    .Where(d => d.DbCostID == costID)
                    .Select(d => d.ID)
                    .ToList();

                if (!deptIds.Any())
                {
                    return Json(new { success = true, data = new List<object>() }, JsonRequestBehavior.AllowGet);
                }

                // 2. Ambil Head_ID yang membutuhkan approval dari department user
                var headIDsToApprove = dbm.PE_PCR_Department_Approval
                    .Where(d => deptIds.Contains(d.Department_ID))
                    .Select(d => d.Head_ID)
                    .Distinct()
                    .ToList();


                // 3. Query utama (sisa tetap seperti punya kamu)
                var tempData = (
                    from h in dbm.PE_PCR_Head
                    join a in dbm.PE_PCR_Approval_Head on h.ID equals a.Head_ID into approvalJoin
                    from sub in approvalJoin.DefaultIfEmpty()
                    join s in dbm.PE_PCR_Approval_Status on sub.Approval_ID equals s.ID into statusJoin
                    from status in statusJoin.DefaultIfEmpty()

                    where headIDsToApprove.Contains(h.ID)

                    orderby h.ID descending
                    select new
                    {
                        h.ID,
                        h.Doc_No,
                        h.Title,
                        h.Dept,
                        Date_Issued = sub.Date_Issued1 ?? h.Timestamps,
                        Date_Approved = sub == null ? (DateTime?)null : sub.Date_Approved,
                        First_Issued_Name = sub == null ? "" : sub.First_Issued_Name,
                        Approval_Name = status == null ? "Draft" : status.Status_Name,
                        Approval_ID = sub == null ? 0 : sub.Approval_ID,

                        ManagerData = dbm.PE_PCR_Management_Approval
                            .Where(m => m.Head_ID == h.ID)
                            .FirstOrDefault(),

                        DepartmentApprovals = dbm.PE_PCR_Department_Approval
                            .Where(d => d.Head_ID == h.ID)
                            .Select(d => new
                            {
                                d.ID,
                                d.Department_ID,
                                d.Signer_Name,
                                d.Signer_Date,
                                d.Comment,
                                StatusID = d.Status_ID
                            }).ToList(),

                        DetailData = dbm.PE_PCR_Detail
                            .Where(det => det.Head_ID == h.ID)
                            .Select(det => new { det.Plan, det.Actual })
                            .FirstOrDefault(),

                        ClassificationData = dbm.PE_PCR_DTBR
                            .Where(dt => dt.Head_ID == h.ID)
                            .Select(dt => dt.Classification)
                            .FirstOrDefault(),

                        CostImpact = dbm.V_PE_PCR_Summary
                            .Where(v => v.Head_ID == h.ID)
                            .Select(v => v.CostImpact)
                            .FirstOrDefault()
                    }
                ).ToList();

                // Transformasi terakhir
                var finalData = tempData.Select(item => new
                {
                    item.ID,
                    item.Doc_No,
                    item.Title,
                    item.Dept,
                    item.Date_Issued,
                    item.Date_Approved,
                    item.First_Issued_Name,
                    item.Approval_Name,
                    item.Approval_ID,
                    item.DepartmentApprovals,
                    CostImpact = item.CostImpact,

                    Plan = item.DetailData?.Plan,
                    Actual = item.ManagerData?.M1_Date,
                    Classification = item.ClassificationData,

                    ManagerApproval = (item.ManagerData == null)
                        ? new List<object>()
                        : new List<object>
                        {
                    new {
                        Level = 1, NIK = item.ManagerData.M1_NIK,
                        Status = item.ManagerData.M1_Status,
                        Name = item.ManagerData.M1_Name,
                        Date = item.ManagerData.M1_Date,
                    },
                    new {
                        Level = 2, NIK = item.ManagerData.M2_NIK,
                        Status = item.ManagerData.M2_Status,
                        Name = item.ManagerData.M2_Name,
                        Date = item.ManagerData.M2_Date,
                    },
                    new {
                        Level = 3, NIK = item.ManagerData.M3_NIK,
                        Status = item.ManagerData.M3_Status,
                        Name = item.ManagerData.M3_Name,
                        Date = item.ManagerData.M3_Date,
                    }
                        }
                }).ToList();

                return Json(new { success = true, data = finalData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetPcrByApprover()
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                string userNik = _currUser.GetUserId(); // current user NIK

                var tempData = (from h in dbm.PE_PCR_Head
                                join a in dbm.PE_PCR_Approval_Head on h.ID equals a.Head_ID into approvalJoin
                                from sub in approvalJoin.DefaultIfEmpty()
                                join s in dbm.PE_PCR_Approval_Status on sub.Approval_ID equals s.ID into statusJoin
                                from status in statusJoin.DefaultIfEmpty()
                                where sub != null && (sub.Approval_ID == 12 || sub.Approval_ID == 13)
                                orderby h.ID descending
                                select new
                                {
                                    h.ID,
                                    h.Doc_No,
                                    h.Title,
                                    h.Dept,
                                    Date_Issued = sub.Date_Issued1 ?? h.Timestamps,
                                    Date_Approved = (sub == null) ? (DateTime?)null : sub.Date_Approved,
                                    First_Issued_Name = (sub == null) ? "" : sub.First_Issued_Name,
                                    Approval_Name = (status == null) ? "Draft" : status.Status_Name,
                                    Approval_ID = (sub == null) ? 0 : sub.Approval_ID,

                                    // Manager data
                                    ManagerData = dbm.PE_PCR_Management_Approval
                                        .Where(m => m.Head_ID == h.ID)
                                        .FirstOrDefault(),

                                    // Department approvals
                                    DepartmentApprovals = dbm.PE_PCR_Department_Approval
                                        .Where(d => d.Head_ID == h.ID)
                                        .Select(d => new
                                        {
                                            d.ID,
                                            d.Department_ID,
                                            d.Signer_Name,
                                            d.Signer_Date,
                                            d.Comment,
                                            StatusID = d.Status_ID
                                        }).ToList(),

                                    // Detail data
                                    DetailData = dbm.PE_PCR_Detail
                                        .Where(det => det.Head_ID == h.ID)
                                        .Select(det => new
                                        {
                                            det.Plan,
                                            det.Actual
                                        })
                                        .FirstOrDefault(),

                                    ClassificationData = dbm.PE_PCR_DTBR
                                        .Where(dt => dt.Head_ID == h.ID)
                                        .Select(dt => dt.Classification)
                                        .FirstOrDefault(),

                                    CostImpact = dbm.V_PE_PCR_Summary
                                     .Where(v => v.Head_ID == h.ID)
                                     .Select(v => v.CostImpact)
                                     .FirstOrDefault(),
                                }).ToList();

                // Transformasi ke final result
                var finalData = tempData.Select(item => new
                {
                    item.ID,
                    item.Doc_No,
                    item.Title,
                    item.Dept,
                    item.Date_Issued,
                    item.Date_Approved,
                    item.First_Issued_Name,
                    item.Approval_Name,
                    item.Approval_ID,
                    item.DepartmentApprovals,
                    CostImpact = item.CostImpact,

                    Plan = item.DetailData?.Plan,
                    Actual = item.ManagerData?.M1_Date,
                    Classification = item.ClassificationData,

                    ManagerApproval = (item.ManagerData == null)
                        ? new List<object>()
                        : new List<object>
                        {
                    new {
                        Level = 1,
                        NIK = item.ManagerData.M1_NIK,
                        Status = item.ManagerData.M1_Status,
                        Name = item.ManagerData.M1_Name,
                        Date = item.ManagerData.M1_Date,
                    },
                    new {
                        Level = 2,
                        NIK = item.ManagerData.M2_NIK,
                        Status = item.ManagerData.M2_Status,
                        Name = item.ManagerData.M2_Name,
                        Date = item.ManagerData.M2_Date,
                    },
                    new {
                        Level = 3,
                        NIK = item.ManagerData.M3_NIK,
                        Status = item.ManagerData.M3_Status,
                        Name = item.ManagerData.M3_Name,
                        Date = item.ManagerData.M3_Date,
                    }
                        }
                }).ToList();

                return Json(new { success = true, data = finalData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetManagerApprovalData()
        {
            try
            {
                // Ambil current user NIK
                string currentUserNik = User.Identity.GetUserId(); // atau sesuai cara kamu dapat NIK

                // Ambil semua Head_ID yg punya Management Approval untuk current user
                var headIDs = dbm.PE_PCR_Management_Approval
                                .Where(ma => ma.M1_NIK == currentUserNik
                                          || ma.M2_NIK == currentUserNik
                                          || ma.M3_NIK == currentUserNik)
                                .Select(ma => ma.Head_ID)
                                .Distinct()
                                .ToList();

                var data = (from h in dbm.PE_PCR_Head
                            where headIDs.Contains(h.ID)
                            orderby h.ID descending
                            select new
                            {
                                h.ID,
                                h.Doc_No,
                                h.Title,
                                h.Dept,
                                Date_Issued = h.Timestamps,

                                // Ambil approval yang terkait current user saja
                                ManagerApprovals = dbm.PE_PCR_Management_Approval
                                    .Where(ma => ma.Head_ID == h.ID
                                              && (ma.M1_NIK == currentUserNik
                                                  || ma.M2_NIK == currentUserNik
                                                  || ma.M3_NIK == currentUserNik))
                                    .Select(ma => new
                                    {
                                        ma.ID,
                                        // Manager 1
                                        ma.M1_NIK,
                                        ma.M1_Status,
                                        ma.M1_Name,
                                        ma.M1_Date,
                                        // Manager 2
                                        ma.M2_NIK,
                                        ma.M2_Status,
                                        ma.M2_Name,
                                        ma.M2_Date,
                                        // Manager 3
                                        ma.M3_NIK,
                                        ma.M3_Status,
                                        ma.M3_Name,
                                        ma.M3_Date,
                                    }).FirstOrDefault()
                            }).ToList();

                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetManagerApprovalData: {ex.Message}");
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetPcrById(int headId)
        {
            try
            {
                var data = dbm.PE_PCR_Head
                    .Include(h => h.PE_PCR_Classification.Select(c => c.PE_PCR_Classification_Master))
                    .Include(h => h.DTBRs.Select(d => d.PE_PCR_DTBR_Master))
                    .Include(h => h.Detail)
                    .Include(h => h.Approvals)
                    .Include(h => h.PE_PCR_Files)
                    .Where(h => h.ID == headId)
                    .Select(h => new
                    {
                        Head = new
                        {
                            h.ID,
                            h.Doc_No,
                            h.Product,
                            h.Line,
                            h.Title,
                            h.Purpose,
                            h.Condition,
                            h.Applied_Part,
                            h.Model,
                            h.Part_No,
                            h.BCP,
                            h.Remark,
                            Forecast = (int?)h.Forecast,
                            h.Sequence,
                            h.Dept,
                            h.Timestamps
                        },
                        Detail = h.Detail == null ? null : new
                        {
                            h.Detail.Current_Statement,
                            h.Detail.Current_Sketch,
                            h.Detail.Current_Cost,
                            h.Detail.Future_Statement,
                            h.Detail.Future_Sketch,
                            h.Detail.Future_Cost,
                            h.Detail.Plan,
                            h.Detail.Actual
                        },
                        Classifications = h.PE_PCR_Classification.Select(c => new
                        {
                            c.Master_ID,
                            MasterName = c.PE_PCR_Classification_Master.Classification_Name,
                            c.Status
                        }),
                        DTBRs = h.DTBRs.Select(d => new
                        {
                            d.DTBR_ID,
                            MasterName = d.PE_PCR_DTBR_Master.DTBR_Name,
                            d.Status,
                            d.Classification,
                            d.Other_Description,
                            d.isRequire
                        }),
                        DBTR_Others = h.DTBRs.Where(d => d.DTBR_ID == 0).Select(d => new
                        {
                            d.DTBR_ID,
                            d.Status,
                            d.Classification,
                            d.Other_Description
                        }).FirstOrDefault(),
                        Approvals = h.Approvals.Select(a => new
                        {
                            a.ID,
                            a.Date_Issued1,
                            a.Date_Issued2,
                            a.Date_Approved,
                            a.Approval_ID,
                            a.First_Issued_NIK,
                            a.First_Issued_Name,
                            a.First_Status_ID,
                            a.Second_Issued_NIK,
                            a.Second_Issued_Name,
                            a.Second_Status_ID,
                            a.Approver_NIK,
                            a.Approver_Name,
                            a.Approver_Status_ID,
                            a.PE_NIK,
                            a.PE_Name,
                            a.PE_Status_ID,
                            a.PE_Manager_NIK,
                            a.PE_Manager_Name,
                            a.PE_Manager_Status_ID,
                            a.Date_PE_Approved,
                            a.Date_PE_Manager_Approved,
                            a.Message,
                            Date = a.Date_Approved ?? a.Date_Issued2 ?? a.Date_Issued1,

                            ManagementHold = h.PE_PCR_Management_Approval
                        .Select(ma => new
                            {
                                // HOLD STATUS
                                M1_Hold = ma.M1_isHold,
                                M2_Hold = ma.M2_isHold,
                                M3_Hold = ma.M3_isHold,

                                ma.M1_Status,
                                ma.M2_Status,
                                ma.M3_Status,
                                // HOLD MESSAGE
                                M1_Message = ma.M1_isHold ? ma.M1_Notes : null,
                                M2_Message = ma.M2_isHold ? ma.M2_Notes : null,
                                M3_Message = ma.M3_isHold ? ma.M3_Notes : null,

                                // OPTIONAL: Tambahin nama yang hold (kalau perlu di UI)
                                ma.M1_Name,
                                ma.M2_Name,
                                ma.M3_Name,
                                ma.isHold
                            })
                            .FirstOrDefault(),
                        }),

                        // MANAGEMENT REJECT
                        ManagementReject = h.PE_PCR_Management_Approval
                    .Select(ma => new
                    {
                        
                        M1_Reject = ma.M1_Status == 3,
                        M2_Reject = ma.M2_Status == 3,
                        M3_Reject = ma.M3_Status == 3,
                        ma.M1_Name,
                        ma.M2_Name,
                        ma.M3_Name,
                        
                        M1_Message = ma.M1_Status == 3 ? ma.M1_Notes : null,
                        M2_Message = ma.M2_Status == 3 ? ma.M2_Notes : null,
                        M3_Message = ma.M3_Status == 3 ? ma.M3_Notes : null,
                    })
                    .FirstOrDefault(),

                        Files = h.PE_PCR_Files.Select(f => new
                        {
                            f.ID,
                            f.File_Name
                        }),

                        // 🔥 Ambil actualDate dari PE_PCR_Management_Approval
                        ActualDate = dbm.PE_PCR_Management_Approval
                            .Where(ma => ma.Head_ID == h.ID)
                            .Select(ma => ma.M1_Date)
                            .FirstOrDefault()
                    })
                    .FirstOrDefault();

                if (data == null)
                    return Json(new { success = false, message = "PCR not found" }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeletePcr(int headId)
        {
            try
            {
                var head = dbm.PE_PCR_Head.Find(headId);
                if (head == null)
                {
                    return Json(new { success = false, message = "Document not found." });
                }

                var _currUser = (ClaimsIdentity)User.Identity;
                string currentUserNik = _currUser.GetUserId();
                var isPelistUser = db.V_Users_Active.Any(u => u.CostID == "PE" && u.NIK == currentUserNik);

                var isFirstIssuedUser = dbm.PE_PCR_Approval_Head.Any(a => a.Head_ID == headId && a.First_Issued_NIK == currentUserNik);

                if (!isPelistUser && !isFirstIssuedUser)
                {
                    return Json(new { success = false, message = "You are not authorized to delete this document." });
                }

                var files = dbm.PE_PCR_Files.Where(f => f.Head_ID == headId);
                if (files.Any())
                {
                    dbm.PE_PCR_Files.RemoveRange(files);
                }

                var classifications = dbm.PE_PCR_Classification.Where(c => c.Head_ID == headId);
                if (classifications.Any())
                {
                    dbm.PE_PCR_Classification.RemoveRange(classifications);
                }

                var dtbrs = dbm.PE_PCR_DTBR.Where(d => d.Head_ID == headId);
                if (dtbrs.Any())
                {
                    dbm.PE_PCR_DTBR.RemoveRange(dtbrs);
                }

                var detail = dbm.PE_PCR_Detail.Where(d => d.Head_ID == headId);
                if (detail.Any())
                {
                    dbm.PE_PCR_Detail.RemoveRange(detail);
                }

                var deptApprovals = dbm.PE_PCR_Department_Approval.Where(da => da.Head_ID == headId);
                if (deptApprovals.Any())
                {
                    dbm.PE_PCR_Department_Approval.RemoveRange(deptApprovals);
                }

                var approvals = dbm.PE_PCR_Approval_Head.Where(a => a.Head_ID == headId);
                if (approvals.Any())
                {
                    dbm.PE_PCR_Approval_Head.RemoveRange(approvals);
                }

                dbm.PE_PCR_Head.Remove(head);

                dbm.SaveChanges();

                string folderPath = Server.MapPath($"~/Files/PE/PCR/{headId}");
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }

                return Json(new { success = true, message = "Document successfully deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        private string J(string input)
        {
            return string.IsNullOrEmpty(input)
                ? input
                : input.Normalize(NormalizationForm.FormC);
        }


        private int SaveDraftInternal(HttpRequestBase request, string currentUserNik, out string msg, out int pageIndicator, int headId = 0)
        {
            Request.ContentEncoding = Encoding.UTF8;
            Response.ContentEncoding = Encoding.UTF8;

            msg = "";
            int.TryParse(request.Form["pageIndicator"], out var formPage);
            pageIndicator = formPage > 0 ? formPage : (headId > 0 ? 2 : 1);

            try
            {
                // --- Override headId kalau ada di form ---
                if (int.TryParse(request.Form["ID"], out var tmpId))
                    headId = tmpId;

                // --- Ambil data Head dari Request.Form ---
                int forecast = int.TryParse(request.Form["Forecast"], out var tmpForecast) ? tmpForecast : 0;

                // --- Buat atau ambil Head ---
                PE_PCR_Head headEntity;
                if (headId > 0)
                {
                    headEntity = dbm.PE_PCR_Head.Find(headId);
                    if (headEntity == null)
                    {
                        msg = "Draft not found.";
                        return 0;
                    }
                }
                else
                {
                    headEntity = new PE_PCR_Head
                    {
                        Timestamps = DateTime.Now,
                        Sequence = 1
                    };
                    dbm.PE_PCR_Head.Add(headEntity);
                }

                // --- Assign field Head ---
                headEntity.Forecast = forecast;
                headEntity.Title = J(request.Form["Title"]);
                headEntity.Dept = J(HttpUtility.HtmlDecode(request.Form["Dept"]));
                headEntity.Product = J(request.Form["Product"]);
                headEntity.Line = J(request.Form["Line"]);
                headEntity.Purpose = J(request.Form["Purpose"]);
                headEntity.Condition = J(request.Form["Condition"]);
                headEntity.Applied_Part = J(request.Form["Applied_Part"]);
                headEntity.Model = J(request.Form["Model"]);
                headEntity.Part_No = J(request.Form["Part_No"]);
                headEntity.BCP = J(request.Form["BCP"]);
                headEntity.Remark = J(request.Form["Remark"]);

                // --- Save Head dulu supaya dapat ID ---
                dbm.SaveChanges();
                headId = headEntity.ID;


                // --- Classification ---
                var classificationJson = Request.Form["PE_PCR_Classification"];
                if (!string.IsNullOrEmpty(classificationJson))
                {
                    // Hapus classification lama
                    var existingClassifications = dbm.PE_PCR_Classification.Where(c => c.Head_ID == headId);
                    dbm.PE_PCR_Classification.RemoveRange(existingClassifications);

                    var classifications = JsonConvert.DeserializeObject<List<PE_PCR_Classification>>(classificationJson);
                    foreach (var c in classifications)
                    {
                        dbm.PE_PCR_Classification.Add(new PE_PCR_Classification
                        {
                            Head_ID = headId,
                            Master_ID = c.Master_ID,
                            Status = c.Status
                        });
                    }
                }

                // --- Detail ---
                var existingDetail = dbm.PE_PCR_Detail.FirstOrDefault(x => x.Head_ID == headId);
                if (existingDetail != null)
                    dbm.PE_PCR_Detail.Remove(existingDetail);

                var detail = new PE_PCR_Detail
                {
                    Head_ID = headId,
                    Current_Statement = J(request.Form["Detail.Current_Statement"]),
                    Current_Sketch = J(request.Form["Detail.Current_Sketch"]),
                    Current_Cost = J(request.Form["Detail.Current_Cost"]),
                    Future_Statement = J(request.Form["Detail.Future_Statement"]),
                    Future_Sketch = J(request.Form["Detail.Future_Sketch"]),
                    Future_Cost = J(request.Form["Detail.Future_Cost"]),
                    Plan = J(request.Form["Detail.Plan"]),
                    Actual = J(request.Form["Detail.Actual"])
                };
                dbm.PE_PCR_Detail.Add(detail);

                // --- Files ---
                string folderPath = Server.MapPath($"~/Files/PE/PCR/{headId}");
                var existingFilenamesToKeep = request.Form.GetValues("ExistingFilenames")?.ToList() ?? new List<string>();
                var existingFilesFromDb = dbm.PE_PCR_Files.Where(f => f.Head_ID == headId).ToList();

                foreach (var dbFile in existingFilesFromDb)
                {
                    if (!existingFilenamesToKeep.Contains(dbFile.File_Name))
                    {
                        string oldFilePath = Path.Combine(folderPath, dbFile.File_Name);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        dbm.PE_PCR_Files.Remove(dbFile);
                    }
                }

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                for (int i = 0; i < request.Files.Count; i++)
                {
                    var file = request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = J(Path.GetFileName(file.FileName));

                        string savePath = Path.Combine(folderPath, fileName);

                        if (System.IO.File.Exists(savePath))
                        {
                            string uniqueName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
                            savePath = Path.Combine(folderPath, uniqueName);
                            fileName = uniqueName;
                        }

                        file.SaveAs(savePath);

                        dbm.PE_PCR_Files.Add(new PE_PCR_Files
                        {
                            Head_ID = headId,
                            File_Name = fileName
                        });
                    }
                }

                // --- Approval Head ---
                var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(a => a.Head_ID == headId);
                if (approval == null)
                {
                    approval = new PE_PCR_Approval_Head
                    {
                        Head_ID = headId,
                        Approval_ID = 1, // default
                        First_Issued_NIK = request.Form["First_Issued_NIK"],
                        First_Issued_Name = request.Form["First_Issued_Name"],
                        First_Status_ID = 1,
                        Date_Issued1 = DateTime.Now
                    };
                    dbm.PE_PCR_Approval_Head.Add(approval);
                }
                else
                {
                    // First Issuer update (jika berbeda dari current user)
                    if (approval.First_Issued_NIK != request.Form["First_Issued_NIK"] && !string.IsNullOrEmpty(approval.First_Issued_NIK))
                    {
                        msg = "You are not allowed to update First Issue data.";
                        return 0;
                    }
                }

                // Approver
                var approverNIK = request.Form["Approved_By_NIK"];
                if (!string.IsNullOrEmpty(approverNIK))
                {
                    approval.Approver_NIK = approverNIK;
                    approval.Approver_Name = db.V_Users_Active.FirstOrDefault(u => u.NIK == approverNIK)?.Name;
                    approval.Approver_Status_ID = 0;
                }

                // Second Issuer
                var secondIssuerNIK = request.Form["Second_Issued_By_NIK"];
                if (secondIssuerNIK == "empty")
                {
                    approval.Second_Issued_NIK = null;
                    approval.Second_Issued_Name = "No Second Issuer";
                    approval.Second_Status_ID = 0;
                }
                else if (!string.IsNullOrEmpty(secondIssuerNIK))
                {
                    approval.Second_Issued_NIK = secondIssuerNIK;
                    approval.Second_Issued_Name = db.V_Users_Active.FirstOrDefault(u => u.NIK == secondIssuerNIK)?.Name;
                    approval.Second_Status_ID = 0;
                }

                dbm.SaveChanges();
                msg = (pageIndicator == 3)
                  ? "First issuer sign and email sent successfully"
                  : "Draft saved.";
                return headId;
            }
            catch (Exception ex)
            {
                // Telusuri inner exception paling dalam
                var err = ex;
                while (err.InnerException != null)
                    err = err.InnerException;

                msg = $"Error: {err.Message} \n\nStackTrace: {err.StackTrace}";
                return 0;
            }

        }


        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveDraftWithFiles(int headId = 0)
        {
            string msg;
            var identity = (ClaimsIdentity)User.Identity;
            string currentUserNik = identity.GetUserId();

            int pageIndicator;
            var newId = SaveDraftInternal(Request, currentUserNik, out msg, out pageIndicator, headId);

            if (newId == 0)
                return Json(new { success = false, message = msg });

            return Json(new { success = true, message = msg, newId, page = pageIndicator });
        }

        [HttpPost]
        public ActionResult SignFirstIssuer(int headId, string nik, string name)
        {
            // Membungkus seluruh logika DB dalam transaksi
            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(a => a.Head_ID == headId);
                    if (approval == null)
                    {
                        return Json(new { success = false, message = "Approval data not found." });
                    }

                    if (approval.First_Issued_NIK != nik)
                    {
                        return Json(new { success = false, message = "You are not the First Issuer." });
                    }

                    // BACKUP DATA untuk Rollback jika email gagal
                    var oldApprovalId = approval.Approval_ID;
                    var oldFirstStatus = approval.First_Status_ID;
                    var oldSecondStatus = approval.Second_Status_ID;
                    var oldDateIssued1 = approval.Date_Issued1;

                    // Perbarui status DB (Stage 1: Signed by First Issuer)
                    approval.Approval_ID = 2; // Initial state change
                    approval.First_Status_ID = 2; // signed
                    approval.Date_Issued1 = DateTime.Now;
                    approval.Approver_Status_ID = 1; // waiting approver

                    string receiverNik;

                    if (string.IsNullOrEmpty(approval.Second_Issued_NIK))
                    {
                        approval.Approval_ID = 4; // Skip Second Issuer, langsung ke Approver
                        approval.Second_Status_ID = 4; // skipped
                        approval.Second_Issued_Name = "No Second Issuer";
                        receiverNik = approval.Approver_NIK; // Kirim ke Approver
                    }
                    else
                    {
                        approval.Second_Status_ID = 1; // waiting Second Issuer
                        receiverNik = approval.Second_Issued_NIK; // Kirim ke Second Issuer
                    }

                    dbm.SaveChanges();

                    // --- BLOK EMAIL DENGAN INNER TRY-CATCH ---
                    try
                    {
                        // Panggil fungsi email
                        var emailResult = SendEmail(headId, receiverNik, 2, false);
                        //var emailResult = SendEmail(headId, "M2503288", 2, false);
                        if (!emailResult)
                        {
                            // Email gagal, lempar exception untuk memicu rollback
                            throw new Exception("Email failed to send (check debug output for details).");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Rollback status DB ke kondisi sebelum SAVECHANGES yang di atas
                        approval.Approval_ID = oldApprovalId;
                        approval.First_Status_ID = oldFirstStatus;
                        approval.Second_Status_ID = oldSecondStatus;
                        approval.Date_Issued1 = oldDateIssued1;
                        dbm.SaveChanges();

                        transaction.Rollback(); // Batalkan transaksi database
                        System.Diagnostics.Debug.WriteLine($"[EMAIL ROLLBACK ERROR] {emailEx.Message}");
                        return Json(new { success = false, message = "Failed to send email: " + emailEx.Message });
                    }

                    // Commit transaksi jika semua berhasil (DB & Email)
                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = "First Issuer signed successfully and email sent.",
                        page = (headId > 0 ? 2 : 1),
                        newId = headId
                    });
                }
                catch (Exception ex)
                {
                    if (transaction != null) transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[SIGN ERROR] {ex.Message}");
                    return Json(new { success = false, message = "General error: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult SignSecondIssuer(int headId, string nik, string name)
        {
            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(a => a.Head_ID == headId);
                    if (approval == null)
                        return Json(new { success = false, message = "Approval not found" });

                    string receiverNik;

                    if (nik == "empty")
                    {
                        approval.Second_Issued_NIK = null;
                        approval.Second_Issued_Name = null;
                        approval.Second_Status_ID = 4; // skipped
                        approval.Date_Issued2 = DateTime.Now;
                        receiverNik = approval.Approver_NIK;
                    }
                    else
                    {
                        approval.Second_Issued_NIK = nik;
                        approval.Second_Issued_Name = name;
                        approval.Second_Status_ID = 2;
                        approval.Date_Issued2 = DateTime.Now;
                        receiverNik = nik;
                    }

                    dbm.Entry(approval).State = EntityState.Modified;
                    dbm.SaveChanges();

                    UpdateWorkflowStep(approval);
                    var emailResult = SendEmail(headId, receiverNik, 2, false);
                    //var emailResult = SendEmail(headId, "M2503288", 2, false);
                    if (!emailResult)
                        throw new Exception("Email failed to send.");

                    transaction.Commit();
                    return Json(new { success = true, message = "Second issuer signed successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult Approve(int headId, string nik, string name, int status, string rejectMessage)
        {
            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(a => a.Head_ID == headId);
                    if (approval == null)
                        return Json(new { success = false, message = "Approval not found" });

                    approval.Approver_NIK = nik;
                    approval.Approver_Name = name;
                    approval.Approver_Status_ID = status;
                    approval.Date_Approved = DateTime.Now;

                    // === Receiver utama ===
                    string receiverNik = nik;

                    // === NIK tambahan untuk kirim email ===
                    var extraNiks = new List<string> { "835.05.20", "812.12.18"};

                    // === Update message jika reject ===
                    if (status == 3 && !string.IsNullOrWhiteSpace(rejectMessage))
                    {
                        approval.Message = rejectMessage;
                    }

                    dbm.Entry(approval).State = EntityState.Modified;
                    UpdateWorkflowStep(approval);
                    dbm.SaveChanges();

                    int emailStatus = (status == 3) ? 3 : 2;

                    // === Kirim email ke receiver utama ===
                    var emailResult = SendEmail(headId, receiverNik, emailStatus, false);
                    if (!emailResult)
                        throw new Exception("Email failed to send to main receiver.");

                    // === Kirim email tambahan ke NIK lain ===
                    foreach (var extraNik in extraNiks)
                    {
                        var extraResult = SendEmail(headId, extraNik, emailStatus, false);
                        if (!extraResult)
                            throw new Exception($"Email failed to send to {extraNik}.");
                    }

                    transaction.Commit();
                    return Json(new { success = true, message = "Approver signed and emails sent successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        private void UpdateWorkflowStep(PE_PCR_Approval_Head approval)
        {
            int priority = 0;

            // --- Issuers ---
            if (approval.First_Status_ID == 2 && approval.Second_Status_ID == 1 && priority < 20)
            {
                approval.Approval_ID = 2;
                priority = 20;
            }
            else if (approval.Second_Status_ID == 2 && priority < 30)
            {
                approval.Approval_ID = 3;
                priority = 30;
            }
            else if (approval.Second_Status_ID == 4 && priority < 30)
            {
                approval.Approval_ID = 4;
                priority = 30;
            }

            // --- Approver ---
            if (approval.Approver_Status_ID == 2 && priority < 80)
            {
                approval.Approval_ID = 5;
                priority = 80;
            }
            else if (approval.Approver_Status_ID == 3 && priority < 80)
            {
                approval.Approval_ID = 10;
                priority = 80;
            }

            // --- PE Manager ---
            if (approval.PE_Manager_Status_ID == 2 && priority < 90)
            {
                approval.Approval_ID = 7;
                approval.Message = null;
                priority = 90;
            }
            //else if (approval.PE_Manager_Status_ID == 3 && priority < 90)
            //{
            //    approval.Approval_ID = 8;
            //    priority = 90;
            //}
            //else if (approval.PE_Manager_Status_ID == 5 && priority < 100)
            //{
            //    approval.Approval_ID = 9;
            //    priority = 100;
            //}
        }

        [HttpPost]
        // Tambahkan parameter boolean di akhir
        public JsonResult SignPE(int headId, string nik, string name, int status, string rejectMessage = null)
        {
            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(a => a.Head_ID == headId);
                    if (approval == null)
                        return Json(new { success = false, message = "Approval not found." });

                    // Ambil DTBR data
                    var DTBRs = Request.Form["DTBRs"];

                    // Update field PE Manager
                    approval.PE_Manager_NIK = nik;
                    approval.PE_Manager_Name = name;
                    approval.PE_Manager_Status_ID = status;
                    approval.Date_PE_Manager_Approved = DateTime.Now;

                    // User login info
                    var identity = (ClaimsIdentity)User.Identity;
                    string currentUserNik = identity.GetUserId();

                    string receiverNikOrigin = approval.First_Issued_NIK;
                    string receiverNik = receiverNikOrigin;

                    // Update DTBRs
                    var existingDtbrs = dbm.PE_PCR_DTBR.Where(d => d.Head_ID == headId).ToList();
                    dbm.PE_PCR_DTBR.RemoveRange(existingDtbrs);

                    if (!string.IsNullOrEmpty(DTBRs))
                    {
                        var dtbrList = JsonConvert.DeserializeObject<List<PE_PCR_DTBR>>(DTBRs);
                        foreach (var d in dtbrList)
                        {
                            d.Head_ID = headId;
                            dbm.PE_PCR_DTBR.Add(d);
                        }
                    }

                    // ================= UPDATE APPROVAL WORKFLOW =================
                    if (status == 5) // HOLD
                    {
                        approval.Message = rejectMessage;

                       
                            // Rollback workflow biasa (Default Hold)
                            approval.Approval_ID = 9;
                            approval.First_Status_ID = 1;
                            approval.Second_Status_ID = 0;
                            approval.Date_Issued2 = null;
                            approval.Date_PE_Approved = null;
                            approval.Approver_Status_ID = 0;
                            approval.PE_Name = null;
                            approval.PE_NIK = null;
                            approval.PE_Status_ID = 0;
                    }
                    else if (status == 3) // REJECT
                    {
                        approval.Message = rejectMessage;
                        approval.Approval_ID = 8;
                    }
                    else
                    {
                        UpdateWorkflowStep(approval);
                    }

                    // Save to DB
                    dbm.SaveChanges();

                    // === SEND EMAIL ===
                    int emailStatus = (status == 5) ? 5 : (status == 3 ? 3 : 2);
                    try
                    {
                        bool emailResult = SendEmail(headId, receiverNik, emailStatus, false);
                        System.Diagnostics.Debug.WriteLine($"[EMAIL RESULT] Sent={emailResult}, Receiver={receiverNik}, Status={emailStatus}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log error tapi jangan batalkan transaksi
                        System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] {emailEx.Message}");
                    }

                    // Commit transaksi
                    transaction.Commit();

                    return Json(new { success = true, message = "PE Manager approval processed successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[SIGN PE ERROR] {ex.Message}");
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SaveByPE()
        {
            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var headIdStr = Request.Form["headId"];
                    int headId = int.TryParse(headIdStr, out var tmpHeadId) ? tmpHeadId : 0;

                    var docNo = Request.Form["docNo"];
                    var DTBRs = Request.Form["DTBRs"];
                    var peStatusIdStr = Request.Form["peStatusId"];
                    int peStatusId = int.TryParse(peStatusIdStr, out var tmpPeStatusId) ? tmpPeStatusId : 0;
                    var Dept = Request.Form["Dept"];

                    string msg;
                    var identity = (ClaimsIdentity)User.Identity;
                    string currentUserNik = identity.GetUserId();

                    var savedId = SaveDraftInternal(Request, currentUserNik, out msg, out var pageIndicator, headId);
                    if (savedId == 0)
                        return Json(new { success = false, message = msg });

                    var head = dbm.PE_PCR_Head.FirstOrDefault(h => h.ID == savedId);
                    if (head != null)
                    {
                        head.Doc_No = docNo;
                        head.Dept = Dept;
                        dbm.SaveChanges();
                    }

                    // === Classification ===
                    var classificationJson = Request.Form["PE_PCR_Classification"];
                    if (!string.IsNullOrEmpty(classificationJson))
                    {
                        var existingClassifications = dbm.PE_PCR_Classification.Where(c => c.Head_ID == headId);
                        dbm.PE_PCR_Classification.RemoveRange(existingClassifications);

                        var classifications = JsonConvert.DeserializeObject<List<PE_PCR_Classification>>(classificationJson);
                        foreach (var c in classifications)
                        {
                            dbm.PE_PCR_Classification.Add(new PE_PCR_Classification
                            {
                                Head_ID = headId,
                                Master_ID = c.Master_ID,
                                Status = c.Status
                            });
                        }
                    }

                    var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(a => a.Head_ID == savedId);
                    if (approval == null)
                        return Json(new { success = false, message = "Approval not found." });

                    approval.PE_NIK = currentUserNik;
                    approval.PE_Name = User.Identity.Name;
                    approval.PE_Status_ID = peStatusId;

                    if (peStatusId == 2)
                    {
                        approval.Approval_ID = 6;
                        approval.Date_PE_Approved = DateTime.Now;
                    }
                    else if (peStatusId == 1)
                    {
                        approval.Date_PE_Approved = null;
                    }

                    dbm.SaveChanges();

                    // === Receiver utama (PE yang sedang login) ===
                    string receiverNik = currentUserNik;

                    //=== Cari PE Manager ===
                    var peManager = db.V_Users_Active
                        .Where(w => w.CostID == "PE" && (w.PositionID == "VI-A"))
                        .Select(x => x.NIK)
                        .FirstOrDefault();
                    //string peManager = "M2503288";

                    // === Kirim email ke PE (yang login) ===
                    bool emailResult = SendEmail(headId, receiverNik, 2, false);
                    if (!emailResult)
                        throw new Exception("Email failed to send to PE.");

                    // === Kirim email juga ke PE Manager (kalau ada) ===
                    if (!string.IsNullOrEmpty(peManager))
                    {
                        bool emailMgr = SendEmail(headId, peManager, 2, false);
                        if (!emailMgr)
                            throw new Exception("Email failed to send to PE Manager.");
                    }

                    transaction.Commit();
                    return Json(new { success = true, message = "PE Filled successfully and email sent." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"[SAVE BY PE ERROR] {ex.Message}");
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        private bool SendEmail(int headId, string receiverNik, int context, bool isLastComment)
        {
            try
            {
                var receiver = dbm.V_Users_Active.FirstOrDefault(u => u.NIK == receiverNik);
                if (receiver == null)
                    throw new Exception($"Receiver with NIK {receiverNik} not found in V_Users_Active.");

                if (string.IsNullOrEmpty(receiver.Email))
                    throw new Exception($"Receiver with NIK {receiverNik} has no email registered.");

                var dept = receiver.CostName ?? "-";
                var name = receiver.Name ?? receiver.NIK;

                string senderAddress = "ngkportal-notification@ngkbusi.com";
                string senderName = "PCR Notification";
                string password = "100%NGKbusi!";

                string receiverEmail = receiver.Email;
                string receiverName = receiver.Name ?? receiver.NIK;

                // === ambil template HTML ===
                string filePath = Path.Combine(Server.MapPath("~/Emails/PE/PCR/"), "email.html");
                string mailText = System.IO.File.ReadAllText(filePath);

                string subject = "📩 PCR Notification";

                var head = dbm.PE_PCR_Head.FirstOrDefault(w => w.ID == headId)
                           ?? throw new Exception("Head not found.");

                var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(w => w.Head_ID == headId)
                               ?? throw new Exception("Approval not found.");

                string messageText = approval.Message ?? "-";

                // === Tentukan status note ===
                string statusNote;

                switch (approval.Approval_ID)
                {
                    case 2:
                        statusNote = "Waiting for Second Issuer Approval";
                        break;
                    case 3:
                        statusNote = "Waiting for Approver Approval";
                        break;
                    case 4:
                        statusNote = "Second Issuer Skipped, Waiting Approver Approval";
                        break;
                    case 5:
                        statusNote = "Approved by Approver, Waiting PE Filled the Information Needed";
                        break;
                    case 6:
                        statusNote = "Waiting for PE Manager Approval";
                        break;
                    case 7:
                        statusNote = "Approved by PE Manager";
                        break;
                    case 8:
                        statusNote = "Rejected by PE Manager";
                        break;
                    case 9:
                        statusNote = "Holded by PE Manager";
                        break;
                    case 10:
                        statusNote = "Rejected by Approver";
                        break;
                    case 11:
                        statusNote = "Concern Points";
                        break;
                    case 12:
                        statusNote = "Approval Manager";
                        break;
                    case 13:
                        statusNote = "PCR Complete";
                        break;
                    default:
                        statusNote = "Draft/Unknown Status";
                        break;
                }

                // === Tentukan Context berdasarkan value ===
                string context1, context2;
                switch (context)
                {
                    case 2:
                        if (statusNote == "Approved by Approver, Waiting PE Filled the Information Needed")
                        {
                            context1 = "Detail Information";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been approved by the Approver and now PE is required to fill in the necessary information.";
                        }
                        if (statusNote == "Waiting for PE Manager Approval")
                        {
                            context1 = "Detail Information";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been filled by PE in the necessary information and now require your approval.";
                        }
                        else if (statusNote == "Approved by PE Manager")
                        {
                            context1 = "Detail Information";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been approved in Phase 1 (Form PCR). The next step will be to collect concern points from the related departments.";
                        }
                        else if (statusNote == "Concern Points")
                        {
                            context1 = "Concern Points";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been created and waiting concern points from your department.";
                        }
                        else if (statusNote == "Approval Manager")
                        {
                            context1 = "Approval Manager";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been approved.";
                        }
                        else if (statusNote == "PCR Complete")
                        {
                            context1 = "PCR Complete";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been complete.";
                        }
                        else
                        {
                            context1 = "Approval";
                            context2 = "We would like to inform you that a PCR (Process Change Request) has been created and requires your Approval.";
                        }
                        break;

                    case 3:
                        if (statusNote == "Rejected by Approver")
                        {
                            context1 = "Rejected";
                            context2 = $"We would like to inform you that a PCR (Process Change Request) has been {statusNote}.<br><strong>Reject message:</strong> {messageText}";
                        }
                        else if (statusNote == "Rejected by PE Manager")
                        {
                            context1 = "Rejected";
                            context2 = $"We would like to inform you that a PCR (Process Change Request) has been {statusNote}.<br><strong>Reject message:</strong> {messageText}";
                        }
                        else if (statusNote == "Approval Manager")
                        {
                            context1 = "Rejected";
                            context2 = $"We would like to inform you that a PCR (Process Change Request) has been Rejected by {statusNote}.";
                        }
                        else
                        {
                            context1 = "Rejected";
                            context2 = $"We would like to inform you that a PCR (Process Change Request) has been Rejected.";
                        }
                        break;
                    case 4:
                        context1 = "Holded";
                        context2 = $"We would like to inform you that a PCR (Process Change Request) has been holded.<br><strong>Hold message:</strong> {messageText}.<br><br>Please revise the document according to the feedback provided.";
                        break;
                    case 5:
                        if (statusNote == "Approval Manager")
                        {
                            context1 = "Holded";
                            context2 = $"We would like to inform you that a PCR (Process Change Request) has been {statusNote}.<br><strong>Hold message:</strong> {messageText}.<br><br>Please revise the document according to the feedback provided.";
                        } else
                        {
                            context1 = "Holded";
                            context2 = $"We would like to inform you that a PCR (Process Change Request) has been {statusNote}.<br><strong>Hold message:</strong> {messageText}.<br><br>Please revise the document according to the feedback provided.";
                        }
                        break;

                    case 7:
                        context1 = "Submitted";
                        context2 = $"We would like to inform you that a PCR (Process Change Request) has been submitted by department. Please verify the concern points.";
                        break;

                    case 8:
                        context1 = "Information";
                        context2 = "We would like to inform you that the PCR (Process Change Request) concern points have been fully completed. The next step is management approval that require your approval.";
                        break;

                    case 9:
                        context1 = "Reminder";
                        context2 = "We would like to inform you that the PCR (Process Change Request) concern points need to be filled by your department. Please fill the information.";
                        break;

                    default:
                        context1 = "Information";
                        context2 = "We would like to inform you that a PCR (Process Change Request) update has been made.";
                        break;
                }

                // === Ganti warna CSS berdasarkan context ===
                string themeColor = "#007bff"; // default biru
                string themeDark = "#0056b3";
                string bgStatus = "#e7f3ff";

                if (context == 3) // rejected
                {
                    themeColor = "#dc3545"; // merah
                    themeDark = "#a71d2a";
                    bgStatus = "#f8d7da";
                }
                else if (context == 5) // hold
                {
                    themeColor = "#ff9800"; // oranye
                    themeDark = "#e65100";
                    bgStatus = "#fff3cd";
                }

                // Replace warna di template
                mailText = mailText
                    .Replace("#007bff", themeColor)
                    .Replace("#0056b3", themeDark)
                    .Replace("#e7f3ff", bgStatus);

                // === Bangun email body ===
                string mailBody = mailText
                    .Replace("##Context1##", context1)
                    .Replace("##Context2##", context2)
                    .Replace("##Message##", "Please review and provide your concern feedback.")
                    .Replace("##Title##", head.Title ?? "-")
                    .Replace("##First_Issued_NIK##", approval.First_Issued_Name ?? "-")
                    .Replace("##Dept##", dept)
                    .Replace("##Date_Issued1##", approval.Date_Issued1?.ToString("dd MMM yyyy") ?? "-")
                    .Replace("##Status##", statusNote)
                    .Replace("##Name##", name);

                // === Link detail ===
                //string baseUrl = "http://localhost:3078";
                string baseUrl = "https://portal.ngkbusi.com";
                mailBody = mailBody.Replace("##Link##", $"{baseUrl}/NGKBusi/PE/PCR/_Create?headId={headId}");

                // === Kirim email ===
                var senderEmail = new MailAddress(senderAddress, senderName);
                var receiverMail = new MailAddress(receiverEmail, receiverName);

                using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                {
                    smtp.EnableSsl = false;
                    smtp.Credentials = new NetworkCredential(senderEmail.Address, password);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var message = new MailMessage(senderEmail, receiverMail)
                    {
                        Subject = subject,
                        Body = mailBody,
                        IsBodyHtml = true
                    })
                    {
                        smtp.Send(message);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[EMAIL SUCCESS] Sent to {receiverEmail}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] {ex.Message}");
                return false;
            }
        }

        private string NormalizeCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            // Remove any existing "EXP." or "EXP" to re-format it correctly
            string cleanValue = value.Replace("EXP.", "").Replace("EXP", "").Trim();

            // If the remaining part is numeric and short (1-3 digits), it's an EXP user
            if (cleanValue.All(char.IsDigit) && cleanValue.Length <= 3)
            {
                // Pad to 3 digits (14 -> 014) and add prefix
                return "EXP." + cleanValue.PadLeft(3, '0');
            }

            // Handle standard 7-digit NIKs (7760617 -> 776.06.17)
            if (cleanValue.All(char.IsDigit) && cleanValue.Length == 7 && !value.Contains("."))
            {
                return cleanValue.Insert(3, ".").Insert(6, ".");
            }

            return value;
        }

        string SafeText(string text)
        {
            return System.Net.WebUtility.HtmlEncode(text)
                .Replace("\n", "<br/>");
        }



        private bool SendEmailComment(int headId, string commenterName, string commentText)
        {
            try
            {
                var nikList = new List<string>();

                var lastComment = dbm.PE_PCR_Comments
                    .Where(c => c.Head_ID == headId)
                    .OrderByDescending(c => c.created)
                    .FirstOrDefault();

                if (lastComment == null)
                    throw new Exception("Comment not found.");

                var commentId = lastComment.C_ID;

                // ======================
                // HANDLE MENTIONS (PING)
                // ======================
                if (!string.IsNullOrEmpty(lastComment.pings))
                {
                    var pings = JsonConvert.DeserializeObject<Dictionary<string, string>>(lastComment.pings);

                    foreach (var ping in pings)
                    {
                        var rawNik = ping.Key?.Trim(); // This is "14" from your JSON
                        var pingNik = NormalizeCode(rawNik); // This becomes "EXP.014"

                        if (!string.IsNullOrEmpty(pingNik))
                        {
                            nikList.Add(pingNik);

                            // 1. Get the Real Name from DB
                            var userDb = dbm.V_Users_Active.FirstOrDefault(u => u.NIK == pingNik);
                            string realName = (userDb != null) ? userDb.Name : pingNik;

                            // 2. SMART REPLACE: 
                            // We look for @ followed by the raw key (14) 
                            // OR @ followed by the padded version (014)
                            string paddedRaw = rawNik.PadLeft(3, '0');
                            string pattern = $@"@({Regex.Escape(rawNik)}|{Regex.Escape(paddedRaw)})\b";

                            commentText = Regex.Replace(
                                commentText,
                                pattern,
                                "@" + realName,
                                RegexOptions.IgnoreCase
                            );
                        }
                    }
                }

                // ======================
                // PCR DATA
                // ======================
                var pcr = dbm.PE_PCR_Head.FirstOrDefault(p => p.ID == headId);
                if (pcr == null)
                    throw new Exception($"PCR {headId} not found.");

                // ======================
                // WORKFLOW RECEIVERS
                // ======================
                var approvalHead = dbm.PE_PCR_Approval_Head.FirstOrDefault(a => a.Head_ID == headId);
                var deptApprovals = dbm.PE_PCR_Department_Approval.Where(d => d.Head_ID == headId).ToList();

                var peNiks = new List<string> { "675.02.14", "835.05.20", "812.12.18" };
                var mgmtApprovals = new List<string> { "785.10.17", "618.04.12", "EXP.014" };

                if (approvalHead != null)
                {
                    nikList.Add(approvalHead.First_Issued_NIK);
                    nikList.Add(approvalHead.Second_Issued_NIK);
                    nikList.Add(approvalHead.Approver_NIK);

                    if (approvalHead.Approval_ID == 12)
                        nikList.AddRange(mgmtApprovals);
                }

                foreach (var dept in deptApprovals)
                {
                    nikList.Add(dept.Signer_NIK);
                }

                nikList.AddRange(peNiks);

                // ======================
                // FINAL CLEAN
                // ======================
                nikList = nikList
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                if (!nikList.Any())
                    throw new Exception("No email receivers.");

                var receivers = dbm.V_Users_Active
                    .Where(u => nikList.Contains(u.NIK) && !string.IsNullOrEmpty(u.Email))
                    .ToList();

                if (!receivers.Any())
                    throw new Exception("Receivers have no valid email.");

                System.Diagnostics.Debug.WriteLine("=== EMAIL RECEIVERS LIST ===");

                foreach (var r in receivers)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"NIK: {r.NIK}, Name: {r.Name}, Email: {r.Email}"
                    );
                }

                System.Diagnostics.Debug.WriteLine("=== END RECEIVERS LIST ===");


                // ======================
                // EMAIL
                // ======================
                var sender = new MailAddress("ngkportal-notification@ngkbusi.com", "PCR Notification");

                string subject = $"[PCR COMMENT] {pcr.Title}";
                string link = $"https://portal.ngkbusi.com/NGKBusi/PE/PCR/_Create?headId={headId}";

                string body = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='font-family:Segoe UI, Arial, sans-serif; font-size:14px;'>
    <tr>
        <td style='padding:15px;'>
            <p style='margin:0 0 10px 0;'>
                <strong>{commenterName}</strong> commented on PCR
                <strong>{pcr.Title}</strong>
            </p>

            <table width='100%' style='border-left:4px solid #0078D4; background:#f4f6f8;'>
                <tr>
                    <td style='padding:10px; color:#333;'>
                        {SafeText(commentText)}
                    </td>
                </tr>
            </table>

            <p style='margin:15px 0 0 0;'>
                <a href='{link}'
                   style='display:inline-block;
                          padding:8px 14px;
                          background:#0078D4;
                          color:#ffffff;
                          text-decoration:none;
                          border-radius:4px;
                          font-size:13px;'>
                    Open PCR
                </a>
            </p>

            <p style='margin-top:20px; font-size:11px; color:#777;'>
                This is an automated notification from NGK Portal.
            </p>
        </td>
    </tr>
</table>";


                using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                {
                    smtp.EnableSsl = false;
                    smtp.Credentials = new NetworkCredential(sender.Address, "100%NGKbusi!");

                    using (var msg = new MailMessage())
                    {
                        msg.From = sender;
                        msg.Subject = subject;
                        msg.Body = body;
                        msg.IsBodyHtml = true;

                        foreach (var r in receivers)
                            msg.To.Add(r.Email);

                        smtp.Send(msg);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[EMAIL ERROR] " + ex.Message);
                return false;
            }
        }

        public ActionResult PreviewPdf(int headId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "File name is required.");

            // ✅ Decode URL-encoded filename
            fileName = Uri.UnescapeDataString(fileName);

            var head = dbm.PE_PCR_Head.FirstOrDefault(h => h.ID == headId);
            string title = head?.Title ?? "PCR_Document";
            string docno = head?.Doc_No ?? "";

            string safeTitle = title.Replace("/", "-").Replace("\\", "-");

            string suggestedFileName = $"{docno}_{safeTitle}_{fileName}";

            string folderPath = Server.MapPath($"~/Files/PE/PCR/{headId}/");
            string filePath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(filePath) ||
                !fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return HttpNotFound("File not found or is not a PDF.");

            Response.ContentEncoding = Encoding.UTF8;
            Response.HeaderEncoding = Encoding.UTF8;

            string utf8FileName = Uri.EscapeDataString(suggestedFileName);

            Response.AppendHeader(
                "Content-Disposition",
                $"inline; filename=\"{suggestedFileName}\"; filename*=UTF-8''{utf8FileName}"
            );

            return File(filePath, "application/pdf");
        }


        [HttpPost]
        public ActionResult UploadFile(int id, HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                var uploadDir = Server.MapPath($"~/Files/PE/PCR/{id}");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var filePath = Path.Combine(uploadDir, Path.GetFileName(file.FileName));
                file.SaveAs(filePath);
                return Json(new { success = true, message = "File uploaded successfully." });
            }

            return Json(new { success = false, message = "File upload failed." });
        }


        // -- PHASE 2 -- //
        [HttpGet]
        public JsonResult GetPhase2Data(int headId)
        {
            try
            {
                // --- PHASE HEAD ---
                var approvalHead = dbm.PE_PCR_Approval_Head
                    .Where(a => a.Head_ID == headId)
                    .Select(a => new
                    {
                        a.Approval_ID,
                        a.Message,
                        a.Date_PE_Manager_Approved,
                        a.First_Issued_NIK
                    })
                    .FirstOrDefault();

                var managerApproval = dbm.PE_PCR_Management_Approval
                    .Where(a => a.Head_ID == headId)
                    .Select(a => new
                    {
                        isHold = a.M1_isHold || a.M2_isHold || a.M3_isHold,
                        holdMessage = a.M1_Notes ?? a.M2_Notes ?? a.M3_Notes,

                        isReject_M1 = a.M1_Status == 3,
                        rejectMessage_M1 = a.M1_Status == 3 ? a.M1_Notes : null,

                        isReject_M2 = a.M2_Status == 3,
                        rejectMessage_M2 = a.M2_Status == 3 ? a.M2_Notes : null,

                        isReject_M3 = a.M3_Status == 3,
                        rejectMessage_M3 = a.M3_Status == 3 ? a.M3_Notes : null,
                    })
                    .FirstOrDefault();

                var PEmanagerApproval = dbm.PE_PCR_Approval_Head
                    .Where(a => a.Head_ID == headId)
                    .Select(a => new
                    {
                        a.Approval_ID,
                        a.Message
                    }).FirstOrDefault();

                // --- GET ALL DEPARTMENT APPROVAL ---
                var concernsQuery = from da in dbm.PE_PCR_Department_Approval
                                    join dept in dbm.PE_PCR_Department_Master
                                        on da.Department_ID equals dept.ID
                                    join st in dbm.PE_PCR_Sign_Master
                                        on da.Status_ID equals st.ID into stLeft
                                    from st in stLeft.DefaultIfEmpty()
                                    where da.Head_ID == headId
                                    select new
                                    {
                                        deptId = dept.ID,
                                        dept.Name,
                                        da.Signer_NIK,
                                        da.Signer_Name,
                                        da.Signer_Date,
                                        da.Status_ID,
                                        da.Comment,
                                        da.Timestamps,
                                        StatusName = st.Sign_Name,
                                    };

                // urutkan berdasarkan SIGNER_DATE yang lebih benar
                var concernsData = concernsQuery
                    .OrderBy(c => c.Signer_Date)
                    .ToList();

                // Return ke frontend
                return Json(new
                {
                    success = true,
                    Approval_ID = approvalHead?.Approval_ID,
                    IsHold = managerApproval?.isHold ?? false,

                    Reject_Message = approvalHead?.Message, // aman
                    Date_PE_Manager_Approved = approvalHead?.Date_PE_Manager_Approved,
                    Originator = approvalHead?.First_Issued_NIK,

                    Concerns = concernsData,
                    ManagerApproval = managerApproval,
                    PEManagerApproval = PEmanagerApproval
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveUserConcern(ConcernWrapperDto wrapper)
        {
            var concerns = wrapper?.Detail;
            if (concerns == null || !concerns.Any())
                return Json(new { success = false, message = "No concerns selected." });

            try
            {
                var headId = concerns.First().HeadId;
                int headIdInt = Convert.ToInt32(headId);

                // === Update Approval Head ===
                var head = dbm.PE_PCR_Approval_Head.FirstOrDefault(h => h.Head_ID == headIdInt);
                if (head != null)
                {
                    head.Approval_ID = wrapper.ApprovalId;
                    dbm.SaveChanges();
                }

                foreach (var c in concerns)
                {
                    var deptApproval = dbm.PE_PCR_Department_Approval
                        .FirstOrDefault(d => d.Head_ID == c.HeadId && d.Department_ID == c.DepartmentId);

                    // --- Kalau user pilih "No Need", hapus record lama ---
                    if (c.StatusId == -1)
                    {
                        if (deptApproval != null)
                            dbm.PE_PCR_Department_Approval.Remove(deptApproval);

                        continue;
                    }

                    bool isNewOrChanged = false;

                    // --- Insert / Update ---
                    if (deptApproval != null)
                    {
                        // Cek apakah NIK baru (biar gak spam email kalau cuma update name/status)
                        if (deptApproval.Signer_NIK != c.Signer_NIK)
                            isNewOrChanged = true;

                        deptApproval.Signer_NIK = c.Signer_NIK;
                        deptApproval.Signer_Name = c.Signer_Name;
                        deptApproval.Status_ID = c.StatusId;
                        deptApproval.Timestamps = DateTime.Now;
                    }
                    else
                    {
                        dbm.PE_PCR_Department_Approval.Add(new PE_PCR_Department_Approval
                        {
                            Head_ID = c.HeadId,
                            Department_ID = c.DepartmentId,
                            Signer_NIK = c.Signer_NIK,
                            Signer_Name = c.Signer_Name,
                            Status_ID = c.StatusId,
                            Timestamps = DateTime.Now
                        });
                        isNewOrChanged = true;
                    }

                    dbm.SaveChanges(); // simpan setiap record concern dulu

                    // === Kirim Email ke User Concern Baru ===
                    if (isNewOrChanged && !string.IsNullOrEmpty(c.Signer_NIK) && c.StatusId == 1)
                    {
                        try
                        {
                            bool sent = SendEmail(c.HeadId, c.Signer_NIK, 2, false);
                            System.Diagnostics.Debug.WriteLine($"[EMAIL SENT] {c.Signer_NIK}: {sent}");
                        }
                        catch (Exception mailEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] {c.Signer_NIK}: {mailEx.Message}");
                        }
                    }
                }

                // === Kirim Email Tambahan ke Approval Head ===
                if (head != null)
                {
                    var headNikList = new List<string>
                    {
                        head.First_Issued_NIK,
                        head.Second_Issued_NIK,
                        head.Approver_NIK
                    }
                    .Where(n => !string.IsNullOrWhiteSpace(n)) // skip kalau null / kosong
                    .Distinct()
                    .ToList();

                    foreach (var nik in headNikList)
                    {
                        try
                        {
                            bool sent = SendEmail(headIdInt, nik, 2, false);
                            System.Diagnostics.Debug.WriteLine($"[EMAIL SENT - HEAD] {nik}: {sent}");
                        }
                        catch (Exception mailEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR - HEAD] {nik}: {mailEx.Message}");
                        }
                    }
                }


                return Json(new { success = true, message = "User concerns saved and emails sent successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveSignerComment(ConcernDto dto)
        {
            if (dto == null || dto.HeadId <= 0 || dto.DepartmentId <= 0)
                return Json(new { success = false, message = "Invalid Head or Department ID." });

            string receiverNik = string.Empty;

            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var identity = (ClaimsIdentity)User.Identity;
                    var currentNik = identity.GetUserId();

                    var record = dbm.PE_PCR_Department_Approval
                                    .SingleOrDefault(x => x.Head_ID == dto.HeadId && x.Department_ID == dto.DepartmentId);

                    if (record == null)
                        return Json(new { success = false, message = "Record not found." });

                    // --- Authorization check ---
                    if (!string.Equals(record.Signer_NIK, currentNik, StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, message = "You are not authorized to sign this department." });

                    // --- Prevent duplicate signing ---
                    if (record.Status_ID == 2)
                        return Json(new { success = false, message = "This concern has already been signed." });

                    // --- Update record ---
                    record.Comment = dto.Comment?.Trim();
                    record.Signer_Date = DateTime.Now;
                    record.Status_ID = 2; // Signed
                    dbm.SaveChanges();

                    // --- Ambil receiver ---
                    var approvalHead = dbm.PE_PCR_Approval_Head
                                          .SingleOrDefault(h => h.Head_ID == dto.HeadId);
                    receiverNik = approvalHead?.First_Issued_NIK;

                    // 1. COMMIT DATABASE SEKARANG
                    // Dengan commit di sini, data dipastikan aman tersimpan 
                    // sebelum proses email berjalan.
                    transaction.Commit();
                }
                catch (DbEntityValidationException ex)
                {
                    transaction.Rollback();
                    var details = string.Join("; ", ex.EntityValidationErrors
                        .SelectMany(e => e.ValidationErrors)
                        .Select(v => v.ErrorMessage));
                    return Json(new { success = false, message = "Validation error: " + details });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var inner = ex.InnerException?.Message ?? ex.Message;
                    return Json(new { success = false, message = "Failed to save signer comment: " + inner });
                }
            }

            if (string.IsNullOrEmpty(receiverNik))
            {
                return Json(new
                {
                    success = true,
                    message = "Data berhasil disimpan, namun email tidak terkirim karena Receiver NIK tidak ditemukan."
                });
            }

            try
            {
                bool emailResult = SendEmail(dto.HeadId, receiverNik, 7, false);

                if (!emailResult)
                {
                    System.Diagnostics.Debug.WriteLine("EMAIL FAILED: SendEmail returned false.");
                    return Json(new
                    {
                        success = true,
                        message = "Data berhasil disimpan, namun pengiriman email gagal."
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EMAIL EXCEPTION: " + ex.Message);
                return Json(new
                {
                    success = true,
                    message = "Data berhasil disimpan, namun terjadi error saat mengirim email."
                });
            }

            return Json(new
            {
                success = true,
                message = "Signer comment successfully saved and marked as signed."
            });
        }
        [HttpGet]
        [Authorize]
        public JsonResult GetOriginatorNik(int headId)
        {
            if (headId <= 0)
            {
                return Json(new { success = false, message = "Invalid Head ID." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var originator = dbm.PE_PCR_Approval_Head
                                    .Where(a => a.Head_ID == headId)
                                    .Select(a => a.First_Issued_NIK) // Ambil NIK Originator
                                    .FirstOrDefault();

                if (originator == null)
                {
                    return Json(new { success = true, originatorNik = "" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, originatorNik = originator }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetOriginatorNik: {ex.Message}");
                return Json(new { success = false, message = "Failed to retrieve originator NIK." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult VerifyDepartmentConcern(int HeadId, int DeptId, int StatusId, string CommentOrigin, IEnumerable<HttpPostedFileBase> files)
        {
            try
            {
                if (HeadId <= 0 || DeptId <= 0)
                    return Json(new { success = false, message = "Invalid document or department ID." });

                var _currUser = (ClaimsIdentity)User.Identity;
                string costName = _currUser.FindFirstValue("CostName");
                string userNik = _currUser.GetUserId();

                var originatorNik = dbm.PE_PCR_Approval_Head
                                       .Where(a => a.Head_ID == HeadId)
                                       .Select(a => a.First_Issued_NIK)
                                       .FirstOrDefault();

                // 🔒 Authorization Check
                var originatorDept = dbm.PE_PCR_Head
                                       .Where(a => a.ID == HeadId)
                                       .Select(a => a.Dept)
                                       .FirstOrDefault();
                System.Diagnostics.Debug.WriteLine($"DEBUG HeadId: {HeadId}");
System.Diagnostics.Debug.WriteLine($"DEBUG costName (current user dept): {costName}");
System.Diagnostics.Debug.WriteLine($"DEBUG originatorDept (document originator): {originatorDept}");

                var approvalEntry = dbm.PE_PCR_Department_Approval
                                       .FirstOrDefault(d => d.Head_ID == HeadId && d.Department_ID == DeptId);

                if (approvalEntry == null)
                    return Json(new { success = false, message = "Department approval entry not found." });

                // 🧾 Update approval status & comment
                if (StatusId == 6)
                {
                    approvalEntry.Status_ID = 6;
                    approvalEntry.Comment_Origin = CommentOrigin;
                }

                // 📂 Folder path
                string folderPath = Server.MapPath($"~/Files/PE/PCR/{HeadId}/{DeptId}");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var existingFiles = dbm.PE_PCR_Department_Files
                    .Where(f => f.Head_ID == HeadId && f.Department_ID == DeptId)
                    .ToList();

                string[] physicalFiles = Directory.GetFiles(folderPath).Select(Path.GetFileName).ToArray();
                List<string> uploadedNames = new List<string>();

                // ===== UPLOAD FILE BARU =====
                if (files != null && files.Any())
                {
                    foreach (var file in files)
                    {
                        if (file == null || file.ContentLength == 0)
                            continue;

                        string originalName = Path.GetFileName(file.FileName);
                        string savePath = Path.Combine(folderPath, originalName);

                        if (System.IO.File.Exists(savePath))
                        {
                            string uniqueName = $"{Path.GetFileNameWithoutExtension(originalName)}_{Guid.NewGuid()}{Path.GetExtension(originalName)}";
                            savePath = Path.Combine(folderPath, uniqueName);
                            originalName = uniqueName;
                        }

                        file.SaveAs(savePath);
                        uploadedNames.Add(originalName);

                        if (!existingFiles.Any(x => x.File_Name == originalName))
                        {
                            dbm.PE_PCR_Department_Files.Add(new PE_PCR_Department_Files
                            {
                                Head_ID = HeadId,
                                Department_ID = DeptId,
                                File_Name = originalName
                            });
                        }
                    }
                }

                // ===== HAPUS FILE YANG SUDAH DIHAPUS DI FE =====
                var incomingNames = uploadedNames.Select(n => n.ToLower()).ToList();

                foreach (var oldFile in existingFiles)
                {
                    if (!incomingNames.Contains(oldFile.File_Name.ToLower()))
                    {
                        string fullPath = Path.Combine(folderPath, oldFile.File_Name);
                        if (System.IO.File.Exists(fullPath))
                            System.IO.File.Delete(fullPath);

                        dbm.PE_PCR_Department_Files.Remove(oldFile);
                    }
                }

                dbm.SaveChanges();

                // ✅ CEK SEMUA DEPT STATUS
                bool allVerified = dbm.PE_PCR_Department_Approval
                    .Where(d => d.Head_ID == HeadId)
                    .All(d => d.Status_ID == 6);

                if (allVerified)
                {
                    var headApproval = dbm.PE_PCR_Approval_Head.FirstOrDefault(h => h.Head_ID == HeadId);
                    if (headApproval != null)
                    {
                        headApproval.Approval_ID = 12; // Concern Verified
                        dbm.SaveChanges();
                    }

                    // ✅ Semua department sudah verified → kirim notifikasi
                    bool isComment = true;

                    var qmManager = db.V_Users_Active
                  .Where(w => w.CostID == "AW" && w.PositionID == "8B")
                  .Select(x => new
                  {
                      x.NIK,
                      x.Name
                  })
                  .FirstOrDefault();

                var plantManager = db.V_Users_Active
                    .Where(w => w.CostID == "AN" && w.PositionID == "9A")
                    .Select(x => new
                    {
                        x.NIK,
                        x.Name
                    })
                    .FirstOrDefault();

                var productionGM = db.V_Users_Active
                    .Where(w => w.CostID == "ALF" && w.PositionID == "VIII")
                    .Select(x => new
                    {
                        x.NIK,
                        x.Name
                    })
                    .FirstOrDefault();

                    string plantManagerNik = plantManager?.NIK;
                    string productionGMNik = productionGM?.NIK;


                    string qmManagerNik = qmManager?.NIK;
                    //var qmManagerNik = "M2503288"; // TODO: ambil dari role nanti

                    try
                    {
                        bool sentToOriginator = SendEmail(HeadId, originatorNik, 8, isComment);
                        bool sentToQM = SendEmail(HeadId, qmManagerNik, 8, isComment);
                        bool sentToPlant = SendEmail(HeadId, plantManagerNik, 8, isComment);
                        bool sentToGM = SendEmail(HeadId, productionGMNik, 8, isComment);

                        System.Diagnostics.Debug.WriteLine($"[EMAIL SENT] Concern complete to Originator: {sentToOriginator}, QM: {sentToQM}");
                    }
                    catch (Exception mailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] Concern complete notification: {mailEx.Message}");
                    }
                }


                string joinedFiles = uploadedNames.Any() ? string.Join(", ", uploadedNames) : "-";
                string statusName = StatusId == 6 ? "Verified" : "Not Verified";

                return Json(new
                {
                    success = true,
                    message = $"Concern successfully marked as {statusName}.",
                    files = joinedFiles
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VerifyDepartmentConcern: {ex.Message}");
                return Json(new { success = false, message = "Failed to process verification. Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetDeptConcernDetail(int headId, int deptId)
        {
            if (headId <= 0 || deptId <= 0)
                return Json(new { success = false, message = "Invalid Head or Department ID." }, JsonRequestBehavior.AllowGet);

            try
            {
                using (var db = new PCRConnection())
                {
                    // Ambil comment + signer dari approval
                    var concern = db.PE_PCR_Department_Approval
                                    .FirstOrDefault(d => d.Head_ID == headId && d.Department_ID == deptId);

                    if (concern == null)
                        return Json(new { success = false, message = "Concern data not found for this department." }, JsonRequestBehavior.AllowGet);

                    // Ambil semua file terkait
                    var fileNames = db.PE_PCR_Department_Files
                                      .Where(f => f.Head_ID == headId && f.Department_ID == deptId)
                                      .Select(f => f.File_Name)
                                      .ToList();

                    // Gabungkan nama file jadi 1 string (dipisah koma)
                    string filesJoined = fileNames != null && fileNames.Any()
                        ? string.Join(", ", fileNames)
                        : string.Empty;

                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            CommentOrigin = concern.Comment_Origin,
                            Files = filesJoined,
                            User = concern.Signer_NIK,
                            HeadId = headId,
                            DeptId = deptId
                        }
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Failed to retrieve concern details: " + inner }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult PreviewFile(int headId, int deptId, string fileName)
        {
            if (headId <= 0 || deptId <= 0 || string.IsNullOrEmpty(fileName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid parameters.");

            try
            {
                string basePath = Server.MapPath($"~/Files/PE/PCR/{headId}/{deptId}/");
                string fullPath = Path.Combine(basePath, fileName);

                if (!System.IO.File.Exists(fullPath))
                    return HttpNotFound("File not found.");

                string contentType = MimeMapping.GetMimeMapping(fileName);
                Response.AddHeader("Content-Disposition", $"inline; filename=\"{fileName}\"");

                return File(fullPath, contentType);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Error previewing file: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult SubmitConcernDocumentsPhase2()
        {
            try
            {
                var headId = int.Parse(Request.Form["headId"]);
                var deptId = int.Parse(Request.Form["deptId"]);

                // Validasi Head ID exists
                var headEntity = dbm.PE_PCR_Head.Find(headId);
                if (headEntity == null)
                {
                    return Json(new { success = false, message = "PCR Head not found." });
                }

                // --- Handle Files ---
                string folderPath = Server.MapPath($"~/Files/PE/PCR/{headId}/ConcernPhase2");

                // Get existing filenames to keep from form
                var existingFilenamesToKeep = Request.Form.GetValues("ExistingFilenames")?.ToList() ?? new List<string>();

                // Get existing files from database for this specific phase/department
                var existingFilesFromDb = dbm.PE_PCR_Department_Files.Where(f => f.Head_ID == headId &&
                                                                      f.Department_ID == deptId).ToList();

                // Remove files that are no longer needed
                foreach (var dbFile in existingFilesFromDb)
                {
                    if (!existingFilenamesToKeep.Contains(dbFile.File_Name))
                    {
                        string oldFilePath = Path.Combine(folderPath, dbFile.File_Name);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        dbm.PE_PCR_Department_Files.Remove(dbFile);
                    }
                }

                // Create directory if not exists
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Save new uploaded files
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string savePath = Path.Combine(folderPath, fileName);

                        // Handle duplicate filenames
                        if (System.IO.File.Exists(savePath))
                        {
                            string uniqueName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
                            savePath = Path.Combine(folderPath, uniqueName);
                            fileName = uniqueName;
                        }

                        file.SaveAs(savePath);

                        // Add to database
                        dbm.PE_PCR_Department_Files.Add(new PE_PCR_Department_Files
                        {
                            Head_ID = headId,
                            File_Name = fileName,
                            Department_ID = deptId,
                        });
                    }
                }

                // Update status or any other business logic for Phase 2 submission
                // You might want to update some status in PE_PCR_Head or create a tracking record

                dbm.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Concern documents for Phase 2 have been submitted successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }

     
        [HttpPost]
        public JsonResult SaveManagementApproval()
        {
            try
            {
                // ================= FORM DATA =================
                var requests = Request;
                int headId = int.Parse(requests.Form["headId"]);
                int level = int.Parse(requests.Form["level"]);
                string nik = requests.Form["nik"];
                string name = requests.Form["name"];
                string notes = requests.Form["notes"];
                int statusId = int.Parse(requests.Form["statusId"]);
                string message = requests.Form["message"];

                // ================= APPROVAL =================
                var approval = dbm.PE_PCR_Management_Approval
                    .FirstOrDefault(a => a.Head_ID == headId)
                    ?? new PE_PCR_Management_Approval { Head_ID = headId };

                if (approval.ID == 0)
                    dbm.PE_PCR_Management_Approval.Add(approval);

                var approvalHead = dbm.PE_PCR_Approval_Head
                    .FirstOrDefault(a => a.Head_ID == headId);

                switch (level)
                {
                    case 1:
                        approval.M1_NIK = nik;
                        approval.M1_Name = name;
                        approval.M1_Status = statusId;
                        approval.M1_Date = DateTime.Now;
                        approval.M1_Notes = notes;
                        break;
                    case 2:
                        approval.M2_NIK = nik;
                        approval.M2_Name = name;
                        approval.M2_Status = statusId;
                        approval.M2_Date = DateTime.Now;
                        approval.M2_Notes = notes;
                        break;
                    case 3:
                        approval.M3_NIK = nik;
                        approval.M3_Name = name;
                        approval.M3_Status = statusId;
                        approval.M3_Date = DateTime.Now;
                        approval.M3_Notes = notes;
                        break;
                }

                if (statusId == 2 && approvalHead != null)
                {
                    if (level == 1)
                    {
                        approvalHead.Approval_ID = 13; 
                    }
                    else
                    {
                        approvalHead.Approval_ID = 12; 
                    }
                }

                dbm.SaveChanges();

                // ================= FILE SAVE =================
                if (Request.Files.Count > 0)
                {
                    string subFolder = Path.Combine(Server.MapPath("~/Files/PE/PCR/"), headId.ToString());
                    if (!Directory.Exists(subFolder)) Directory.CreateDirectory(subFolder);

                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        HttpPostedFileBase iFile = Request.Files[i];
                        if (iFile != null && iFile.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(iFile.FileName);
                            string fullPath = Path.Combine(subFolder, fileName);

                            iFile.SaveAs(fullPath);

                            // Simpan ke Database File Manager
                            var exists = dbm.PE_PCR_Management_Files.Any(x =>
                                x.Head_ID == headId && x.M_Level == level && x.File_Name == fileName);

                            if (!exists)
                            {
                                dbm.PE_PCR_Management_Files.Add(new PE_PCR_Management_Files
                                {
                                    Head_ID = headId,
                                    M_Level = level,
                                    File_Name = fileName
                                });
                            }
                        }
                    }
                    dbm.SaveChanges();
                }

                // ================= EMAIL =================
                try
                {
                    // daftar NIK PE dan First Issue (sementara, nanti ganti ke tabel role)
                    List<string> peList = new List<string> { "675.02.14", "835.05.20", "812.12.18" };

                    List<string> receivers = new List<string>();

                    if (statusId == 2) // ✅ SIGN
                    {
                        switch (level)
                        {
                            case 1:
                                if (approvalHead?.First_Issued_NIK != null)
                                    receivers.Add(approvalHead.First_Issued_NIK);

                                receivers.AddRange(peList);

                                var deptNames = (
                                    from a in dbm.PE_PCR_Department_Approval
                                    join d in dbm.PE_PCR_Department_Master
                                        on a.Department_ID equals d.ID
                                    where a.Head_ID == headId
                                    select d.DbCostName
                                ).Distinct().ToList();
                                System.Diagnostics.Debug.WriteLine(
    $"[DEBUG] DeptNames for Head {headId}: {string.Join(", ", deptNames)}"
);

                                var deptUserEmails = dbm.V_Users_Active
                                 .Where(u =>
                                     deptNames.Contains(u.DeptName) &&
                                     !string.IsNullOrEmpty(u.NIK))
                                 .Select(u => u.NIK)
                                 .Distinct()
                                 .ToList();
                                System.Diagnostics.Debug.WriteLine(
    $"[DEBUG] Subordinate emails count: {deptUserEmails.Count}"
);
                                if (!deptUserEmails.Any())
                                {
                                    System.Diagnostics.Debug.WriteLine(
                                        $"[WARNING] No subordinates found for Head_ID {headId}"
                                    );

                                    return Json(new
                                    {
                                        success = false,
                                        message = "No subordinate found. Approval saved but email not sent."
                                    });
                                }

                                System.Diagnostics.Debug.WriteLine(
    $"[DEBUG] Final receivers NIK count: {receivers.Distinct().Count()}"
);

                                foreach (var mail in deptUserEmails)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Subordinate email: {mail}");
                                }


                                // 🟦 Masukkan ke receivers
                                receivers.AddRange(deptUserEmails);

                                //send email complete

                                break;


                            case 2: // Plant Manager → Production GM
                                    //var prodGM = dbm.V_Users_Active
                                    //    .Where(u => u.CostID == "AW" && u.PositionID == "VI-B")
                                    //    .Select(u => u.NIK)
                                    //    .FirstOrDefault();
                                    //var prodGM = "776.06.17";
                                    //if (!string.IsNullOrEmpty(prodGM))
                                    //    receivers.Add(prodGM);
                                if (approvalHead?.First_Issued_NIK != null)
                                    receivers.Add(approvalHead.First_Issued_NIK);

                                // Cek apakah Level 3 sudah Sign (Status == 2)
                                if (approval.M3_Status != 2)
                                {
                                    // Level 3 BELUM sign, kirim notif ke Level 3 untuk minta sign
                                    receivers.Add("785.10.17");
                                }
                                else
                                {
                                    // Level 3 SUDAH sign (artinya Level 2 dan 3 dua-duanya sudah sign)
                                    receivers.Add("EXP.014");
                                }


                                receivers.AddRange(peList);
                                // send email biasa
                                break;

                            case 3: // QM Manager → Plant Manager
                                    //var plantMgr = dbm.V_Users_Active
                                    //    .Where(u => u.CostID == "PE" && u.PositionID == "VI-A")
                                    //    .Select(u => u.NIK)
                                    //    .FirstOrDefault();
                                    ////var plantMgr = "776.06.17";
                                    //if (!string.IsNullOrEmpty(plantMgr))
                                    //    receivers.Add(plantMgr);

                                if (approvalHead?.First_Issued_NIK != null)
                                    receivers.Add(approvalHead.First_Issued_NIK);

                                // Cek apakah Level 2 sudah Sign (Status == 2)
                                if (approval.M2_Status != 2)
                                {
                                    // Level 2 BELUM sign, kirim notif ke Level 2 untuk minta sign
                                    receivers.Add("618.04.12");
                                }
                                else
                                {
                                    // Level 2 SUDAH sign (artinya Level 2 dan 3 dua-duanya sudah sign)
                                    receivers.Add("EXP.014");
                                }

                                receivers.AddRange(peList);
                                // send email biasa

                                break;
                        }
                    }
                    else if (statusId == 3) // ❌ REJECT
                    {
                        // Semua level → First Issue + PE
                        if (approvalHead?.First_Issued_NIK != null)
                            receivers.Add(approvalHead.First_Issued_NIK);
                        receivers.AddRange(peList);
                    }
                    else if (statusId == 4 || statusId == 5)
                    {
                        // Route ke originator (First_Issued)
                        if (approvalHead?.First_Issued_NIK != null)
                            receivers.Add(approvalHead.First_Issued_NIK);

                        // Route ke PE list
                        receivers.AddRange(peList);
                    }

                    // --- Kirim email ---
                    //foreach (var targetNik in receivers.Distinct())
                    //{
                    //    var userEmail = dbm.V_Users_Active
                    //        .Where(u => u.NIK == targetNik)
                    //        .Select(u => u.Email)
                    //        .FirstOrDefault();

                    //    if (!string.IsNullOrEmpty(userEmail))
                    //    {
                    //        var emailResult = SendEmail(headId, targetNik, statusId, false);
                    //        System.Diagnostics.Debug.WriteLine($"[EMAIL SENT] To {targetNik} ({userEmail}), Result: {emailResult}");
                    //    }
                    //    else
                    //    {
                    //        System.Diagnostics.Debug.WriteLine($"[EMAIL SKIPPED] {targetNik} has no email");
                    //    }
                    //}
                    // --- Kirim email (GABUNG JADI SATU) ---
                    var finalReceivers = receivers.Distinct().ToList();
                    if (finalReceivers.Any())
                    {
                        var emailResult = SendEmailGroup(headId, finalReceivers, statusId, false);
                        System.Diagnostics.Debug.WriteLine($"[EMAIL GROUP SENT] Result: {emailResult}");
                    }
                }
                catch (Exception exMail)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] {exMail.Message}");
                }


                return Json(new { success = true, message = "Approval & files saved." });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        private bool SendEmailGroup(int headId, List<string> receiverNiks, int context, bool isLastComment)
        {
            try
            {
                // Ambil SEMUA data user dari database yang NIK-nya ada di dalam list
                var receiverUsers = dbm.V_Users_Active
                    .Where(u => receiverNiks.Contains(u.NIK) && !string.IsNullOrEmpty(u.Email))
                    .ToList();

                if (!receiverUsers.Any())
                    throw new Exception("No valid emails found for the provided NIKs.");

                string senderAddress = "ngkportal-notification@ngkbusi.com";
                string senderName = "PCR Notification";
                string password = "100%NGKbusi!";

                // === ambil template HTML ===
                string filePath = Path.Combine(Server.MapPath("~/Emails/PE/PCR/"), "email.html");
                string mailText = System.IO.File.ReadAllText(filePath);

                string subject = "📩 PCR Notification";

                var head = dbm.PE_PCR_Head.FirstOrDefault(w => w.ID == headId)
                           ?? throw new Exception("Head not found.");

                var approval = dbm.PE_PCR_Approval_Head.FirstOrDefault(w => w.Head_ID == headId)
                               ?? throw new Exception("Approval not found.");

                string messageText = approval.Message ?? "-";

                // === Tentukan status note ===
                string statusNote;
                switch (approval.Approval_ID)
                {
                    case 2: statusNote = "Waiting for Second Issuer Approval"; break;
                    case 3: statusNote = "Waiting for Approver Approval"; break;
                    case 4: statusNote = "Second Issuer Skipped, Waiting Approver Approval"; break;
                    case 5: statusNote = "Approved by Approver, Waiting PE Filled the Information Needed"; break;
                    case 6: statusNote = "Waiting for PE Manager Approval"; break;
                    case 7: statusNote = "Approved by PE Manager"; break;
                    case 8: statusNote = "Rejected by PE Manager"; break;
                    case 9: statusNote = "Holded by PE Manager"; break;
                    case 10: statusNote = "Rejected by Approver"; break;
                    case 11: statusNote = "Concern Points"; break;
                    case 12: statusNote = "Approval Manager"; break;
                    case 13: statusNote = "PCR Complete"; break;
                    default: statusNote = "Draft/Unknown Status"; break;
                }

                // === Tentukan Context berdasarkan value ===
                string context1, context2;
                switch (context)
                {
                    case 2:
                        if (statusNote == "Approved by Approver, Waiting PE Filled the Information Needed")
                        {
                            context1 = "Detail Information";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been approved by the Approver and now PE is required to fill in the necessary information.";
                        }
                        else if (statusNote == "Waiting for PE Manager Approval")
                        {
                            context1 = "Detail Information";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been filled by PE in the necessary information and now require your approval.";
                        }
                        else if (statusNote == "Approved by PE Manager")
                        {
                            context1 = "Detail Information";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been approved in Phase 1 (Form PCR). The next step will be to collect concern points from the related departments.";
                        }
                        else if (statusNote == "Concern Points")
                        {
                            context1 = "Concern Points";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been created and waiting concern points from your department.";
                        }
                        else if (statusNote == "Approval Manager")
                        {
                            context1 = "Approval Manager";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been approved.";
                        }
                        else if (statusNote == "PCR Complete")
                        {
                            context1 = "PCR Complete";
                            context2 = "We would like to inform you that the PCR (Process Change Request) has been complete.";
                        }
                        else
                        {
                            context1 = "Approval";
                            context2 = "We would like to inform you that a PCR (Process Change Request) has been created and requires your Approval.";
                        }
                        break;
                    case 3:
                        context1 = "Rejected";
                        context2 = $"We would like to inform you that a PCR (Process Change Request) has been {statusNote}.<br><strong>Reject message:</strong> {messageText}";
                        break;
                    case 4:
                    case 5:
                        context1 = "Holded";
                        context2 = $"We would like to inform you that a PCR (Process Change Request) has been {statusNote}.<br><strong>Hold message:</strong> {messageText}.<br><br>Please revise the document according to the feedback provided.";
                        break;
                    case 7:
                        context1 = "Submitted";
                        context2 = $"We would like to inform you that a PCR (Process Change Request) has been submitted by department. Please verify the concern points.";
                        break;
                    case 8:
                        context1 = "Information";
                        context2 = "We would like to inform you that the PCR (Process Change Request) concern points have been fully completed. The next step is management approval that require your approval.";
                        break;
                    case 9:
                        context1 = "Reminder";
                        context2 = "We would like to inform you that the PCR (Process Change Request) concern points need to be filled by your department. Please fill the information.";
                        break;
                    default:
                        context1 = "Information";
                        context2 = "We would like to inform you that a PCR (Process Change Request) update has been made.";
                        break;
                }

                // === Ganti warna CSS berdasarkan context ===
                string themeColor = "#007bff";
                string themeDark = "#0056b3";
                string bgStatus = "#e7f3ff";

                if (context == 3) { themeColor = "#dc3545"; themeDark = "#a71d2a"; bgStatus = "#f8d7da"; }
                else if (context == 5 || context == 4) { themeColor = "#ff9800"; themeDark = "#e65100"; bgStatus = "#fff3cd"; }

                mailText = mailText.Replace("#007bff", themeColor).Replace("#0056b3", themeDark).Replace("#e7f3ff", bgStatus);

                // === Bangun email body ===
                string mailBody = mailText
                    .Replace("##Name##", "All")
                    .Replace("##Context1##", context1)
                    .Replace("##Context2##", context2)
                    .Replace("##Message##", "Please review and provide your concern feedback.")
                    .Replace("##Title##", head.Title ?? "-")
                    .Replace("##First_Issued_NIK##", approval.First_Issued_Name ?? "-")
                    .Replace("##Dept##", "Multiple Departments") // Diubah karena email grup
                    .Replace("##Date_Issued1##", approval.Date_Issued1?.ToString("dd MMM yyyy") ?? "-")
                    .Replace("##Status##", statusNote)
                    .Replace("##Name##", "Team"); // Diubah karena email grup

                string baseUrl = "https://portal.ngkbusi.com";
                mailBody = mailBody.Replace("##Link##", $"{baseUrl}/NGKBusi/PE/PCR/_Create?headId={headId}");

                var senderEmail = new MailAddress(senderAddress, senderName);

                using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                {
                    smtp.EnableSsl = false;
                    smtp.Credentials = new NetworkCredential(senderEmail.Address, password);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var message = new MailMessage())
                    {
                        message.From = senderEmail;
                        message.Subject = subject;
                        message.Body = mailBody;
                        message.IsBodyHtml = true;

                        // ✅ Tambahkan SEMUA user ke kolom "To"
                        foreach (var usr in receiverUsers)
                        {
                            // Pastikan format dasar email valid (ada @ nya) biar tidak error
                            if (!string.IsNullOrWhiteSpace(usr.Email) && usr.Email.Contains("@"))
                            {
                                message.To.Add(new MailAddress(usr.Email, usr.Name ?? usr.NIK));
                                System.Diagnostics.Debug.WriteLine($"[EMAIL TO ADDED] {usr.Email}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[EMAIL SKIPPED - INVALID FORMAT] NIK: {usr.NIK}, Email: {usr.Email}");
                            }
                        }

                        if (message.To.Count == 0)
                            throw new Exception("No valid email addresses found after filtering.");

                        smtp.Send(message);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[EMAIL SUCCESS] Sent grouped email to {receiverUsers.Count} users");
                return true;
            }
            // 1. Tangkap error spesifik jika ada email yang ditolak Server
            catch (System.Net.Mail.SmtpFailedRecipientsException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR - RECIPIENT REJECTED] {smtpEx.Message}");
                // Looping untuk melihat email mana saja yang ditolak
                foreach (var inner in smtpEx.InnerExceptions)
                {
                    System.Diagnostics.Debug.WriteLine($" -> Failed Email: {inner.FailedRecipient} | Status: {inner.StatusCode}");
                }
                return false;
            }
            // 2. Tangkap error umum (Timeout, File HTML tidak ketemu, dll)
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMAIL INNER ERROR] {ex.InnerException.Message}");
                }
                return false;
            }
        }

        [HttpGet]
        public JsonResult GetManagementApproval(int headId)
        {
            try
            {
                var approval = dbm.PE_PCR_Management_Approval
                    .FirstOrDefault(a => a.Head_ID == headId);

                var approvalFiles = dbm.PE_PCR_Management_Files
                    .Where(f => f.Head_ID == headId)
                    .Select(f => new
                    {
                        f.ID,
                        f.Head_ID,
                        f.M_Level,
                        f.File_Name
                    })
                    .ToList();

                if (approval == null)
                {
                    return Json(new { success = false, message = "Approval not found" }, JsonRequestBehavior.AllowGet);
                }

                Func<DateTime?, string> formatDate = d =>
                d.HasValue ? d.Value.ToString("dd MMMM yyyy") : null;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        approval.ID,
                        approval.Head_ID,

                        // Manager 1
                        approval.M1_NIK,
                        approval.M1_Status,
                        approval.M1_Name,
                        approval.M1_Notes,
                        M1_Date = formatDate(approval.M1_Date),
                        approval.M1_isHold,

                        // Manager 2
                        approval.M2_NIK,
                        approval.M2_Status,
                        approval.M2_Name,
                        approval.M2_Notes,
                        M2_Date = formatDate(approval.M2_Date),
                        approval.M2_isHold,
                        


                        // Manager 3
                        approval.M3_NIK,
                        approval.M3_Status,
                        approval.M3_Name,
                        approval.M3_Notes,
                        M3_Date = formatDate(approval.M3_Date),
                        approval.M3_isHold,

                        Files = approvalFiles
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult SaveComment(PE_PCR_Comments comment, IEnumerable<HttpPostedFileBase> files)
        {
            try
            {
                int headId = 0;
                if (Request.QueryString["headId"] != null)
                    int.TryParse(Request.QueryString["headId"], out headId);

                if (headId <= 0)
                    return Json(new { success = false, message = "Invalid Head ID" });

                var currCurrentUser = Request["created_by_current_user"];

                comment.Head_ID = headId;
                comment.created = DateTime.Now;
                comment.created_by_current_user = bool.Parse(currCurrentUser);

                if (string.IsNullOrEmpty(comment.parent) || comment.parent == "null" || comment.parent == "{}")
                    comment.parent = null;


                if (string.IsNullOrEmpty(comment.pings) || comment.pings == "{}")
                {
                    comment.pings = null;
                }
                comment.content = string.IsNullOrWhiteSpace(comment.content) ? "-" : comment.content;

                // --- GENERATE NEXT C_ID ---
                var lastComment = dbm.PE_PCR_Comments
                    .Where(c => c.Head_ID == headId)
                    .OrderByDescending(c => c.C_ID)
                    .FirstOrDefault();

                if (lastComment == null)
                {
                    comment.id = "c1";
                }
                else
                {
                    string lastCid = lastComment.id; // contoh: "c5"
                    int num = 1;

                    if (!string.IsNullOrEmpty(lastCid) && lastCid.Length > 1)
                    {
                        int.TryParse(lastCid.Substring(1), out num); // ambil angka 5
                        num++;
                    }

                    comment.id = "c" + num;
                }


                dbm.PE_PCR_Comments.Add(comment);
                dbm.SaveChanges();

                // Simpan file jika ada
                var commentFilesMetadata = new List<PE_PCR_Comment_Files>();
                comment.CommentFiles = new List<PE_PCR_Comment_Files>();

                if (Request.Files.Count > 0)
                {
                    string baseDirectory = Server.MapPath($"~/Files/PE/PCR/Comments/{comment.Head_ID}/{comment.C_ID}/");
                    if (!Directory.Exists(baseDirectory))
                        Directory.CreateDirectory(baseDirectory);

                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        HttpPostedFileBase file = Request.Files[i];
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(file.FileName);
                            string filePath = Path.Combine(baseDirectory, fileName);

                            file.SaveAs(filePath);

                            var fileMetadata = new PE_PCR_Comment_Files
                            {
                                C_ID = comment.C_ID,
                                File_Name = filePath,
                                Mime_Type = file.ContentType
                            };
                            commentFilesMetadata.Add(fileMetadata);
                            dbm.PE_PCR_Comment_Files.Add(fileMetadata);
                        }
                    }
                    dbm.SaveChanges();
                }

                SendEmailComment(headId, comment.fullName, comment.content);

                var attachmentsForJS = commentFilesMetadata.Select(f => new
                {
                    id = f.ID,
                    file = f.File_Name,
                    mimeType = f.Mime_Type,
                    url = $"/Files/PE/PCR/Comments/{comment.Head_ID}/{comment.C_ID}/{f.File_Name}"
                }).ToList();

                string parentId = string.IsNullOrEmpty(comment.parent) || comment.parent == "null" ? null : comment.parent;

                return Json(new
                {
                    id = comment.id,
                    C_ID = comment.C_ID,
                    parent = parentId,
                    created = comment.created.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    modified = comment.modified,
                    content = string.IsNullOrWhiteSpace(comment.content) ? "-" : comment.content,
                    attachments = attachmentsForJS,
                    pings = comment.pings,
                    creator = comment.creator,
                    fullname = comment.fullName,
                    created_by_current_user = comment.created_by_current_user,
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetCommentUsers(int headId)
        {
            try
            {
                var peNiks = new List<string> { "675.02.14", "835.05.20", "812.12.18" };
                var mgrNiks = new List<string> { "785.10.17", "618.04.12", "EXP.014" };

                var allNiks = new List<string>();

                // Tambahkan NIK statis
                allNiks.AddRange(peNiks);
                allNiks.AddRange(mgrNiks);

                var head = dbm.PE_PCR_Approval_Head.FirstOrDefault(h => h.Head_ID == headId);
                if (head != null)
                {
                    if (!string.IsNullOrEmpty(head.First_Issued_NIK)) allNiks.Add(head.First_Issued_NIK);
                    if (!string.IsNullOrEmpty(head.Second_Issued_NIK)) allNiks.Add(head.Second_Issued_NIK);
                    if (!string.IsNullOrEmpty(head.Approver_NIK)) allNiks.Add(head.Approver_NIK);
                }

                var deptSignerNiks = dbm.PE_PCR_Department_Approval
                    .Where(d => d.Head_ID == headId && !string.IsNullOrEmpty(d.Signer_NIK))
                    .Select(d => d.Signer_NIK)
                    .ToList();

                allNiks.AddRange(deptSignerNiks);

                var uniqueNiks = allNiks
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                var rawUsers = dbm.V_Users_Active
                    .Where(u => uniqueNiks.Contains(u.NIK))
                    .ToList();

                var users = rawUsers
                    .Select(u => new
                    {
                        id = Regex.Replace(u.NIK, @"[^\d]", ""),
                        fullname = u.Name,
                        profile_picture_url = ""
                    })
                    .ToList();

                return Json(new { success = true, data = users }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetComments(int headId)
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            var currUserNik = _currUser.GetUserId();
            try
            {
                var commentsData = dbm.PE_PCR_Comments
                    .Where(c => c.Head_ID == headId)
                    .OrderBy(c => c.created)
                    .ToList();

                var commentIds = commentsData.Select(c => c.C_ID).ToList();
                var filesMetadata = dbm.PE_PCR_Comment_Files
                    .Where(f => commentIds.Contains(f.C_ID))
                    .ToList();

                // 3. Transformasi data ke format yang diharapkan oleh jQuery-Comments
                var result = commentsData.Select(comment =>
                {
                    // Filter attachments untuk komentar ini
                    var currentAttachments = filesMetadata
                    .Where(f => f.C_ID == comment.C_ID)
                    .Select(f =>
                    {
                        string filePath = f.File_Name;

                        // kalau path absolut, ubah ke path relatif mulai dari "Files/"
                        if (filePath.Contains(@"\Files\"))
                        {
                            // replace dari \ ke /
                            filePath = filePath.Substring(filePath.IndexOf(@"\Files\"))
                                               .Replace(@"\", "/");
                        }

                        // pastikan prefix aplikasi (misal /NGKBusi)
                        var baseUrl = Request.ApplicationPath == "/" ? "" : Request.ApplicationPath;
                        string fileUrl = $"{baseUrl}{filePath}";

                        return new
                        {
                            id = f.ID,
                            file = fileUrl,
                            mimeType = f.Mime_Type,
                            url = fileUrl
                        };
                    })
                    .ToList();


                    object pingsObject = null;

                    if (!string.IsNullOrEmpty(comment.pings))
                    {
                        try
                        {
                            var raw = comment.pings.Trim();

                            if (raw.StartsWith("{") && raw.EndsWith("}"))
                            {
                                pingsObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(raw);
                            }
                            else
                            {
                                pingsObject = new Dictionary<string, string>();
                            }
                        }
                        catch
                        {
                            pingsObject = new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        pingsObject = new Dictionary<string, string>();
                    }

                    string parsedContent = comment.content;
                    if (pingsObject is JObject pingDict)
                    {
                        foreach (var kv in pingDict)
                        {
                            string id = kv.Key;
                            string name = kv.Value.ToString();

                            parsedContent = Regex.Replace(parsedContent, $@"@{id}\b", $"@{name}");
                        }
                    }

                    return new
                    {
                        id = comment.id,
                        parent = comment.parent,
                        content = comment.content,
                        created = comment.created.HasValue
        ? comment.created.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        : null,

                        modified = comment.modified,
                        attachments = currentAttachments,
                        pings = pingsObject,
                        C_ID = comment.C_ID,
                        creator = comment.creator,
                        fullname = comment.fullName,

                        created_by_current_user = (comment.creator == currUserNik)
                    };
                }).ToList();

                // Return seluruh list komentar
                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "";
                return Json(new { success = false, message = ex.Message, inner }, JsonRequestBehavior.AllowGet);
            }

        }


        [HttpPost]
        public JsonResult UpdateComment(PE_PCR_Comments updatedComment, IEnumerable<HttpPostedFileBase> files)
        {
            try
            {
                // --- 1. Ambil ID & Validasi ---
                string commentId = Request.Form["id"];
                string creator = Request.Form["creator"];
                int headId = 0;
                if (Request.QueryString["headId"] != null)
                    int.TryParse(Request.QueryString["headId"], out headId);

                if (string.IsNullOrEmpty(commentId) || headId <= 0)
                    return Json(new { success = false, message = "Invalid Comment ID or Head ID." });

                // --- 2. Cari Komentar ---
                var existingComment = dbm.PE_PCR_Comments
                    .FirstOrDefault(c => c.Head_ID == headId && c.id == commentId);

                if (existingComment == null)
                    return Json(new { success = false, message = "Comment not found." });

                // --- 3. Validasi Otorisasi ---
                if (!string.Equals(existingComment.creator.Trim(), creator.Trim(), StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "You can only edit your own comment." });


                // --- 4. Update Data Utama ---
                string newContent = Request.Form["content"];
                if (string.IsNullOrWhiteSpace(newContent))
                    return Json(new { success = false, message = "Comment content cannot be empty." });

                existingComment.content = newContent;
                existingComment.modified = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                existingComment.pings = Request.Form["pings"]; // Pings dikirim sebagai JSON string dari JS

                // --- 4.5. Handle Penghapusan File Lama ---
                var existingFilesInClient = new List<string>();
                try
                {
                    if (!string.IsNullOrEmpty(Request.Form["existingFiles"]))
                    {
                        var raw = Request.Form["existingFiles"];
                        if (raw != "[]" && raw != "[{}]")
                        {
                            existingFilesInClient = JsonConvert.DeserializeObject<List<string>>(raw)
                                .Where(f => !string.IsNullOrEmpty(f))
                                .ToList();
                        }
                    }
                }
                catch
                {
                    existingFilesInClient = new List<string>();
                }


                // Ambil semua file lama dari database
                var filesInDb = dbm.PE_PCR_Comment_Files
                    .Where(f => f.C_ID == existingComment.C_ID)
                    .ToList();

                // Cari file mana yang sudah tidak ada di UI
                var filesToDelete = filesInDb
                    .Where(f => !existingFilesInClient.Contains(Path.GetFileName(f.File_Name)))
                    .ToList();

                foreach (var file in filesToDelete)
                {
                    // Hapus file fisik di folder
                    try
                    {
                        if (System.IO.File.Exists(file.File_Name))
                            System.IO.File.Delete(file.File_Name);
                    }
                    catch (Exception ex)
                    {
                        // biarkan jalan terus, tapi log kalau mau
                        System.Diagnostics.Debug.WriteLine("Delete file error: " + ex.Message);
                    }

                    // Hapus dari database
                    dbm.PE_PCR_Comment_Files.Remove(file);
                }

                // --- 5. Handle File Upload (Menambah file baru) ---
                var commentFilesMetadata = new List<PE_PCR_Comment_Files>();

                if (Request.Files.Count > 0)
                {
                    string baseDirectory = Server.MapPath($"~/Files/PE/PCR/Comments/{headId}/{existingComment.C_ID}/");
                    if (!Directory.Exists(baseDirectory)) Directory.CreateDirectory(baseDirectory);

                    // Hapus semua file lama kalau ada upload baru
                    var oldFiles = dbm.PE_PCR_Comment_Files
                        .Where(f => f.C_ID == existingComment.C_ID)
                        .ToList();

                    foreach (var oldFile in oldFiles)
                    {
                        try
                        {
                            if (System.IO.File.Exists(oldFile.File_Name))
                                System.IO.File.Delete(oldFile.File_Name);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error deleting old file: " + ex.Message);
                        }

                        dbm.PE_PCR_Comment_Files.Remove(oldFile);
                    }

                    // Upload file baru
                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        HttpPostedFileBase file = Request.Files[i];
                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(file.FileName);
                            string filePath = Path.Combine(baseDirectory, fileName);

                            file.SaveAs(filePath);

                            var fileMetadata = new PE_PCR_Comment_Files
                            {
                                C_ID = existingComment.C_ID,
                                File_Name = filePath,
                                Mime_Type = file.ContentType
                            };
                            commentFilesMetadata.Add(fileMetadata);
                            dbm.PE_PCR_Comment_Files.Add(fileMetadata);
                        }
                    }
                }

                dbm.SaveChanges();

                // --- 6. Ambil Data Attachment Terbaru untuk Return ---
                var allFiles = dbm.PE_PCR_Comment_Files
                    .Where(f => f.C_ID == existingComment.C_ID)
                    .ToList()
                    .Select(f =>
                    {
                        string fileNameOnly = Path.GetFileName(f.File_Name);
                        string url = $"/Files/PE/PCR/Comments/{headId}/{existingComment.C_ID}/{fileNameOnly}";

                        return new
                        {
                            id = f.ID,
                            file = fileNameOnly,
                            mimeType = f.Mime_Type,
                            url = url
                        };
                    }).ToList();


                SendEmailComment(headId, existingComment.fullName, existingComment.content);

                // --- 7. Return Format Final ---
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = existingComment.id,
                        parent = existingComment.parent,
                        content = existingComment.content,

                        // Kembalikan nilai Time/Timestamp
                        created = existingComment.created,
                        modified = existingComment.modified,

                        attachments = allFiles,
                        pings = existingComment.pings,
                        creator = existingComment.creator,
                        fullname = existingComment.fullName,
                        created_by_current_user = existingComment.created_by_current_user
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server Error during update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteComment(string commentId, int headId)
        {
            try
            {
                // Ambil NIK user saat ini untuk otorisasi
                string currentUserNik = System.Web.HttpContext.Current.User.Identity.GetUserId().Trim();

                // 1. Cari komentar
                var commentToDelete = dbm.PE_PCR_Comments
                    .FirstOrDefault(c => c.id == commentId && c.Head_ID == headId);

                if (commentToDelete == null)
                    return Json(new { success = false, message = "Comment not found." });

                // 2. Otorisasi
                if (commentToDelete.creator.Trim() != currentUserNik)
                    return Json(new { success = false, message = "You are not authorized to delete this comment." });

                // 3. Hapus File Fisik dan Metadata Terkait
                var filesToDelete = dbm.PE_PCR_Comment_Files.Where(f => f.C_ID == commentToDelete.C_ID).ToList();

                // Loop dan hapus file fisik
                foreach (var fileMeta in filesToDelete)
                {
                    if (System.IO.File.Exists(fileMeta.File_Name))
                    {
                        System.IO.File.Delete(fileMeta.File_Name);
                    }
                }

                // Hapus metadata file dari DB
                dbm.PE_PCR_Comment_Files.RemoveRange(filesToDelete);

                // 4. Hapus Komentar Utama dan Simpan
                dbm.PE_PCR_Comments.Remove(commentToDelete);
                dbm.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error during deletion: " + ex.Message });
            }
        }

        public ActionResult DownloadPcrSummary()
        {
            var pcrData = dbm.Set<V_PE_PCR_Summary>()
                             .OrderByDescending(x => x.Head_ID)
                             .ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Summary");

                ws.Cell(1, 1).Value = "No";
                ws.Cell(1, 2).Value = "Head_ID";
                ws.Cell(1, 3).Value = "Date_Issued";
                ws.Cell(1, 4).Value = "Document_No";
                ws.Cell(1, 5).Value = "Title";
                ws.Cell(1, 6).Value = "Dept";
                ws.Cell(1, 7).Value = "Originator";
                ws.Cell(1, 8).Value = "Plan_Date";
                ws.Cell(1, 9).Value = "Finish_Date";
                ws.Cell(1, 10).Value = "Rank";
                ws.Cell(1, 11).Value = "Status";
                ws.Cell(1, 12).Value = "Progress_Percentage";
                ws.Cell(1, 13).Value = "Remarks";
                ws.Cell(1, 14).Value = "Duration_Days";
                ws.Cell(1, 14).Value = "CostImpact";

                var headerRange = ws.Range("A1:N1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontSize = 14;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Row(1).Height = 25;

                int row = 2;
                int no = 1;

                foreach (var d in pcrData)
                {
                    ws.Cell(row, 1).Value = no++;
                    ws.Cell(row, 2).Value = d.Head_ID;
                    ws.Cell(row, 3).Value = d.Date_Issued;
                    ws.Cell(row, 4).Value = d.Document_No;
                    ws.Cell(row, 5).Value = d.Title;
                    ws.Cell(row, 6).Value = d.Dept;
                    ws.Cell(row, 7).Value = d.Originator;
                    ws.Cell(row, 8).Value = d.Plan_Date;
                    ws.Cell(row, 9).Value = d.Finish_Date;
                    ws.Cell(row, 10).Value = d.Rank;
                    ws.Cell(row, 11).Value = d.Status;
                    ws.Cell(row, 12).Value = d.Progress_Percentage;
                    ws.Cell(row, 13).Value = d.Remarks;
                    ws.Cell(row, 14).Value = d.Duration_Days;
                    ws.Cell(row, 14).Value = d.CostImpact;
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
                        $"PCR_SUMMARY_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    );
                }
            }
        }

        [HttpPost]
        public JsonResult SendReminder(ConcernDto dto)
        {
            if (dto == null || dto.HeadId <= 0 || dto.DepartmentId <= 0)
                return Json(new { success = false, message = "Invalid Head or Department ID." });

            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var identity = (ClaimsIdentity)User.Identity;
                    var currentNik = identity.GetUserId();

                    // Ambil record department
                    var record = dbm.PE_PCR_Department_Approval
                                    .SingleOrDefault(x => x.Head_ID == dto.HeadId &&
                                                          x.Department_ID == dto.DepartmentId);

                    if (record == null)
                        return Json(new { success = false, message = "Record not found." });

                    if (string.IsNullOrWhiteSpace(record.Signer_NIK))
                        return Json(new { success = false, message = "No signer assigned for this department." });

                    string receiverNik = record.Signer_NIK; 

                   
                    bool emailResult = SendEmail(dto.HeadId, receiverNik, 9, false);

                    if (!emailResult)
                        throw new Exception("Failed to send reminder email.");

                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = "Reminder email has been sent to the department signer."
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var inner = ex.InnerException?.Message ?? ex.Message;
                    return Json(new { success = false, message = "Failed to send reminder: " + inner });
                }
            }
        }

        [HttpPost]
        public JsonResult SendManagerReminder(ManagerReminderDto dto)
        {
            // Validate inputs
            if (dto == null || dto.HeadId <= 0 || string.IsNullOrWhiteSpace(dto.TargetNIK))
                return Json(new { success = false, message = "Invalid Head ID or Target NIK." });

            // Keeping the transaction structure consistent with your previous code
            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    // We pass dto.TargetNIK directly into your SendEmail method
                    bool emailResult = SendEmail(dto.HeadId, dto.TargetNIK, 9, false);

                    if (!emailResult)
                        throw new Exception("Failed to send reminder email.");

                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = "Reminder email has been sent to the designated manager."
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var inner = ex.InnerException?.Message ?? ex.Message;
                    return Json(new { success = false, message = "Failed to send reminder: " + inner });
                }
            }
        }

        [HttpPost]
        public ActionResult EscalateToPhase2(int headId)
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                string userNik = _currUser.GetUserId();

                var pcrHead = dbm.PE_PCR_Approval_Head.FirstOrDefault(x => x.Head_ID == headId);

                if (pcrHead != null)
                {
                    pcrHead.Approval_ID = 11;

                    dbm.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "Document successfully escalated to Phase 2."
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Document record not found."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred: " + ex.Message
                });
            }
        }
    }



}