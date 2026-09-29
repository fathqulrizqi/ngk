using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Areas.Purchasing.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Windows.Documents;

namespace NGKBusi.Areas.Purchasing.Controllers
{
    public class BASTController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        BASTConnection dbb = new BASTConnection();
        WOConnection dbw = new WOConnection();

        public ActionResult Index()
        {
            var currUserId = User.Identity.GetUserId();
            ViewBag.currUserId = currUserId;

            var purchasingUserList = db.V_Users_Active.Where(w => w.DeptName == "SUPPLY & PROCUREMENT").ToList();
            ViewBag.purchasingUserList = purchasingUserList;

            var _currUser = (ClaimsIdentity)User.Identity;
            string deptName = _currUser.FindFirstValue("DeptName");

            bool isAllowedFromRole = db.Users_Menus_Roles.Any(w => w.menuID == 1050 && w.allowView == true && w.userNIK == currUserId);

            bool isAllowedFromDept = deptName != null && deptName.Trim().ToUpper() == "SUPPLY & PROCUREMENT";

            if (!isAllowedFromRole && !isAllowedFromDept)
            {
                ViewBag.DeptName = deptName;
                ViewBag.LinkTitle = "Back to Home";
                ViewBag.Link = "https://portal.ngkbusi.com/";

                return PartialView("~/Areas/HC/Views/Inventory/Partial/_NoAccess.cshtml");
            }

            return View();
        }

        public ActionResult Create()
        {
           
            var currUserId = User.Identity.GetUserId();
            var currUserName = User.Identity.GetUserName();

            ViewBag.currUserId = currUserId;
            ViewBag.currUserName = currUserName;
            ViewBag.userList = db.V_Users_Active.ToList();
            ViewBag.vendorList = db.AX_Vendor_List.Where(w => w.IsActive == true && w.VENDGROUP != "OTH").ToList();
            ViewBag.woNumberList = dbw.Purchasing_WorkingOrder_List.ToList();

            int currentYear = DateTime.Now.Year;

            string currentYearShort = (currentYear % 100).ToString("00");
            string nextYearShort = ((currentYear + 1) % 100).ToString("00");

            var periodFYList = new List<string>
            {
                "FY1" + currentYearShort,
                "FY1" + nextYearShort   
            };

            ViewBag.budgetList = db.V_FA_Payment_Request_Budget_List
                                   .Where(w => periodFYList.Contains(w.Period_FY))
                                   .ToList();

            return View("Detail", new Purchasing_BAST());
        }

        [HttpGet]
        public ActionResult Detail(string bast_number) 
        {
            var currUserId = User.Identity.GetUserId();
            var currUserName = User.Identity.GetUserName();

            ViewBag.currUserId = currUserId;
            ViewBag.currUserName = currUserName;
            ViewBag.userList = db.V_Users_Active.ToList();
            ViewBag.vendorList = db.AX_Vendor_List.Where(w => w.IsActive == true && w.VENDGROUP != "OTH").ToList();

            int currentYear = DateTime.Now.Year;
            int lastYear = currentYear - 1;

            ViewBag.woNumberList = dbw.Purchasing_WorkingOrder_List
                .Where(wo => wo.Date.Year == currentYear || wo.Date.Year == lastYear)
                .ToList();

            ViewBag.woNumberList = dbw.Purchasing_WorkingOrder_List.ToList();

            string currentYearShort = (currentYear % 100).ToString("00");
            string nextYearShort = ((currentYear + 1) % 100).ToString("00");

            var periodFYList = new List<string>
            {
                "FY1" + currentYearShort,
                "FY1" + nextYearShort
            };

            ViewBag.budgetList = db.V_FA_Payment_Request_Budget_List
                                   .Where(w => periodFYList.Contains(w.Period_FY))
                                   .ToList();

            using (var dbb = new BASTConnection())
            {
                var bastData = dbb.Purchasing_BAST.FirstOrDefault(b => b.bast_number == bast_number);

                if (bastData == null)
                {
                    TempData["ErrorMessage"] = "Data BAST tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                return View(bastData);
            }
        }

        [HttpGet]
        public JsonResult GetBASTList()
        {
            try
            {
                using (var dbb = new BASTConnection())
                {
                    var rawData = dbb.Purchasing_BAST
                                     .OrderByDescending(b => b.timestamps)
                                     .ToList();

                    var formattedList = rawData.Select(b => new
                    {
                        b.id,
                        b.bast_number,
                        b.title,
                        b.description,
                        b.vendor_id,
                        b.vendor_name,
                        b.budget_no,
                        b.budget_name,
                        b.section_name,
                        b.creator_nik,
                        b.creator_name,
                        b.status,
                        due_date = b.due_date.HasValue ? b.due_date.Value.ToString("yyyy-MM-dd") : "-",
                        timestamps = b.timestamps.HasValue ? b.timestamps.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-"
                    }).ToList();

                    return Json(new
                    {
                        success = true,
                        data = formattedList
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Gagal mengambil data BAST: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetBASTByNumber(string bast_number)
        {
            try
            {
                // Validasi input
                if (string.IsNullOrEmpty(bast_number))
                {
                    return Json(new { success = false, message = "BAST Number is required." }, JsonRequestBehavior.AllowGet);
                }

                using (var dbb = new BASTConnection())
                {
                    var rawData = dbb.Purchasing_BAST
                                         .FirstOrDefault(b => b.bast_number == bast_number);

                    if (rawData == null)
                    {
                        return Json(new { success = false, message = "Data BAST tidak ditemukan." }, JsonRequestBehavior.AllowGet);
                    }

                    var fileList = dbb.Purchasing_BAST_File
                                      .Where(f => f.bast_number == bast_number)
                                      .Select(f => new
                                      {
                                          f.id,
                                          f.file_name,
                                          f.status
                                      })
                                      .ToList();

                    // ==========================================
                    // 1. AMBIL DATA LAST HISTORY PADA TIAP TITLE
                    // ==========================================
                    var latestApprovalHistory = db.Approval_History
                        .Where(a => a.Menu_Id == 1050 && a.Document_Id == rawData.id && a.Reveral_ID == bast_number)
                        .GroupBy(a => a.Title.ToLower())
                        .Select(g => g.OrderByDescending(x => x.id).FirstOrDefault())
                        .ToList();

                    // ==========================================
                    // 2. GABUNGKAN APPROVAL MASTER DENGAN HISTORY TERBARU
                    // ==========================================
                    var rawApprovalMaster = db.Approval_Master
                        .Where(a => a.Menu_Id == 1050 && a.Document_Id == rawData.id && a.Label == bast_number)
                        .ToList();

                    var approvalList = rawApprovalMaster.Select(a =>
                    {
                        var titleKey = (a.Title ?? "").ToLower();
                        var history = latestApprovalHistory.FirstOrDefault(h => h != null && (h.Title ?? "").ToLower() == titleKey);

                        return new
                        {
                            a.User_NIK,
                            a.Title,
                            a.Levels,
                            a.Levels_Sub,
                            a.Header,
                            Status = history != null ? history.Status : "", // 'Sign', 'Return', atau 'Reject'
                            Note = history != null ? history.Note : "",
                            Approved_Date = history != null ? history.Created_At.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("en-US")) : "",
                            IsRevise = history != null ? history.IsRevise : false,
                            IsReject = history != null ? history.IsReject : false
                        };
                    }).ToList();

                    // ==========================================
                    // 3. LOGIKA MENENTUKAN ACTIVE APPROVER & NIK
                    // ==========================================
                    string activeApproverNik = "";
                    string activeApproverRole = "";

                    string currentStatusLower = (rawData.status ?? "").ToLower();

                    if (currentStatusLower == "created" || currentStatusLower == "waiting for creator")
                    {
                        activeApproverNik = rawData.creator_nik;
                        activeApproverRole = "creator";
                    }
                    else if (currentStatusLower == "waiting for checker")
                    {
                        var checker = rawApprovalMaster.FirstOrDefault(a => a.Title != null && a.Title.ToLower() == "checker");
                        if (checker != null)
                        {
                            activeApproverNik = checker.User_NIK;
                            activeApproverRole = "checker";
                        }
                    }
                    else if (currentStatusLower == "waiting for approver")
                    {
                        var approver = rawApprovalMaster.FirstOrDefault(a => a.Title != null && a.Title.ToLower() == "approver");
                        if (approver != null)
                        {
                            activeApproverNik = approver.User_NIK;
                            activeApproverRole = "approver";
                        }
                    }
                    else if (currentStatusLower == "waiting for final approver")
                    {
                        var approver = rawApprovalMaster.FirstOrDefault(a => a.Title != null && a.Title.ToLower() == "final approver");
                        if (approver != null)
                        {
                            activeApproverNik = approver.User_NIK;
                            activeApproverRole = "final approver";
                        }
                    }

                    var formattedData = new
                    {
                        rawData.id,
                        rawData.bast_number,
                        rawData.title,
                        rawData.description,
                        rawData.vendor_id,
                        rawData.vendor_name,
                        rawData.budget_no,
                        rawData.budget_name,
                        rawData.section_name,
                        rawData.creator_nik,
                        rawData.creator_name,
                        rawData.status,

                        active_approver_nik = activeApproverNik,
                        active_approver_role = activeApproverRole,

                        approvalHistory = latestApprovalHistory.Select(h => new
                        {
                            h.Header,
                            h.Title,
                            h.Approval,
                            h.Approval_Sub,
                            h.Note,
                            h.IsRevise,
                            h.IsReject,
                            h.Status,
                            Created_At = h.Created_At.ToString("yyyy-MM-dd HH:mm:ss")
                        }).ToList(),

                        due_date = rawData.due_date.HasValue ? rawData.due_date.Value.ToString("yyyy-MM-dd") : "-",
                        timestamps = rawData.timestamps.HasValue ? rawData.timestamps.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-",

                        files = fileList,
                        approvals = approvalList
                    };

                    return Json(new
                    {
                        success = true,
                        data = formattedData
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Gagal mengambil data BAST: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult SaveBAST()
        {
            try
            {
                // 1. Tangkap Data dari Request
                var idStr = Request["id"];
                int idbast = string.IsNullOrEmpty(idStr) ? 0 : int.Parse(idStr);

                var title = Request["title"];
                var description = Request["description"];
                var dueDateStr = Request["due_date"];
                var vendor_id = Request["vendor_id"];
                var vendor_name = Request["vendor_name"];
                var creator_nik = Request["creator_nik"];
                var creator_name = Request["creator_name"];
                var budget_no = Request["budget_no"];
                var budget_name = Request["budget_name"];
                var section_name = Request["section_name"];
                var wo_number = Request["wo_number"];
                var wo_desc = Request["wo_desc"];

                // Data Approval
                var checker_name = Request["checker_name"];
                var checker_id = Request["checker_id"];
                var approver_id = Request["approver_id"];
                var approver_name = Request["approver_name"];
                var final_approver_id = Request["final_approver_id"];
                var final_approver_name = Request["final_approver_name"];

                // 2. Validasi Input Dasar
                if (string.IsNullOrEmpty(checker_id) || string.IsNullOrEmpty(approver_id) || string.IsNullOrEmpty(final_approver_id))
                {
                    return Json(new { success = false, message = "Checker, Approver, and Final Approver must be selected." }, JsonRequestBehavior.AllowGet);
                }

                DateTime dueDate;
                if (!DateTime.TryParse(dueDateStr, out dueDate))
                {
                    return Json(new { success = false, message = "Invalid Due Date format. Please use a valid date." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(checker_id) || string.IsNullOrEmpty(approver_id))
                {
                    return Json(new { success = false, message = "Checker and Approver must be selected." }, JsonRequestBehavior.AllowGet);
                }

                // ==========================================
                // 3. PROSES SAVE / UPDATE DATA BAST (Gunakan BASTConnection - dbb)
                // ==========================================
                bool isNew = (idbast == 0);
                Purchasing_BAST bast;

                if (isNew)
                {
                    bast = new Purchasing_BAST();

                    string currentMonthYear = DateTime.Now.ToString("MMyy");

                    var lastBast = dbb.Purchasing_BAST
                                      .Where(b => b.bast_number.EndsWith(currentMonthYear))
                                      .OrderByDescending(b => b.bast_number)
                                      .FirstOrDefault();

                    int sequence = 1;

                    if (lastBast != null && !string.IsNullOrEmpty(lastBast.bast_number) && lastBast.bast_number.Length >= 12)
                    {
                        string lastSeqStr = lastBast.bast_number.Substring(5, 3);
                        if (int.TryParse(lastSeqStr, out int lastSeq))
                        {
                            sequence = lastSeq + 1;
                        }
                    }

                    bast.bast_number = $"BAST-{sequence:D3}{currentMonthYear}";

                    bast.creator_nik = creator_nik;
                    bast.creator_name = creator_name;
                    bast.status = "Created";
                    bast.timestamps = DateTime.Now;
                }
                else
                {
                    bast = dbb.Purchasing_BAST.FirstOrDefault(w => w.id == idbast);
                    if (bast == null)
                    {
                        return Json(new { success = false, message = "BAST Data Not Found. Please use create new feature." }, JsonRequestBehavior.AllowGet);
                    }
                }

                bast.title = title;
                bast.vendor_id = vendor_id;
                bast.vendor_name = vendor_name;
                bast.due_date = dueDate;
                bast.description = description;
                bast.budget_no = budget_no;
                bast.budget_name = budget_name;
                bast.wo_number = wo_number;
                bast.wo_description = wo_desc;
                bast.section_name = section_name;

                if (isNew)
                {
                    dbb.Purchasing_BAST.Add(bast);
                }

                int saveBast = dbb.SaveChanges();


                // ==========================================
                // 4. PROSES SAVE ATTACHMENT (FILES) (Gunakan BASTConnection - dbb)
                // ==========================================
                if (Request.Files.Count > 0)
                {
                    string uploadPath = Server.MapPath("~/Content/BAST_Files/");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        string inputName = Request.Files.AllKeys[i];
                        var file = Request.Files[i];

                        if (file != null && file.ContentLength > 0)
                        {
                            string fileName = Path.GetFileName(file.FileName);
                            string fullPath = Path.Combine(uploadPath, fileName);

                            file.SaveAs(fullPath);

                            string fileStatus = (inputName == "after_files") ? "after" : "before";

                            var bastFile = new Purchasing_BAST_File
                            {
                                bast_number = bast.bast_number,
                                file_name = fileName,
                                status = fileStatus   
                            };
                            dbb.Purchasing_BAST_File.Add(bastFile);
                        }
                    }
                    dbb.SaveChanges();
                }


                // ==========================================
                // 5. PROSES ROUTING APPROVAL (Gunakan DefaultConnection - db)
                // ==========================================
                if (saveBast > 0 || !isNew)
                {
                    if (!isNew)
                    {
                        var existingApprovals = db.Approval_Master
                                                  .Where(a => a.Menu_Id == 1050 && a.Document_Id == bast.id)
                                                  .ToList();
                        if (existingApprovals.Count > 0)
                        {
                            db.Approval_Master.RemoveRange(existingApprovals);
                        }
                    }

                    var approvalRoutes = new List<Approval_Master>
            {
                // Creator
                new Approval_Master {
                    Menu_Id = 1050,
                    Document_Id = bast.id,
                    User_NIK = creator_nik,
                    Title = "creator",
                    Header = creator_name,
                    Label = bast.bast_number,
                    Levels = 1,
                    Levels_Sub = 0
                },
                
                // Checker
                new Approval_Master {
                    Menu_Id = 1050,
                    Document_Id = bast.id,
                    User_NIK = checker_id,
                    Title = "checker",
                    Header = checker_name,
                    Label = bast.bast_number,
                    Levels = 1,
                    Levels_Sub = 1
                },
                
                // Approver
                new Approval_Master {
                    Menu_Id = 1050,
                    Document_Id = bast.id,
                    Label = bast.bast_number,
                    User_NIK = approver_id,
                    Header = approver_name,
                    Title = "approver",
                    Levels = 1,
                    Levels_Sub = 2
                },

                new Approval_Master {
                    Menu_Id = 1050,
                    Document_Id = bast.id,
                    Label = bast.bast_number,
                    User_NIK = final_approver_id,
                    Header = final_approver_name,
                    Title = "final approver",
                    Levels = 1,
                    Levels_Sub = 3
                }
            };

                    db.Approval_Master.AddRange(approvalRoutes);
                    int saveApproval = db.SaveChanges();

                    if (saveApproval > 0)
                    {
                        return Json(new { success = true, message = "BAST Saved Successfully", id = bast.id }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        // Jika error saat save approval
                        return Json(new { success = false, message = "BAST Saved, but failed to save Approval Routes." }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Failed to Save BAST Data." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteBAST(int id)
        {
            try
            {
                using (var dbb = new BASTConnection())
                {
                    var bast = dbb.Purchasing_BAST.FirstOrDefault(b => b.id == id);

                    if (bast == null)
                    {
                        return Json(new { success = false, message = "Data BAST tidak ditemukan." });
                    }

                    string bastNumber = bast.bast_number;

                    var files = dbb.Purchasing_BAST_File.Where(f => f.bast_number == bastNumber).ToList();
                    string uploadPath = Server.MapPath("~/Content/BAST_Files/");

                    foreach (var file in files)
                    {
                        string fullPath = Path.Combine(uploadPath, file.file_name);
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }

                    if (files.Count > 0)
                    {
                        dbb.Purchasing_BAST_File.RemoveRange(files);
                    }

                    dbb.Purchasing_BAST.Remove(bast);
                    dbb.SaveChanges();

                    var existingApprovals = db.Approval_Master
                                              .Where(a => a.Menu_Id == 1050 && a.Document_Id == id)
                                              .ToList();

                    if (existingApprovals.Count > 0)
                    {
                        db.Approval_Master.RemoveRange(existingApprovals);
                        db.SaveChanges();
                    }

                    return Json(new { success = true, message = "Data BAST dan file terkait berhasil dihapus secara permanen." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal menghapus data BAST: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SubmitApproval()
        {
            try
            {
                string bastNumber = Request["bast_number"];
                string actionType = Request["actionType"];
                string note = Request["note"] ?? ""; 

                string currentUserId = User.Identity.GetUserId() ?? Request["user_nik"];
                string currentUserName = User.Identity.GetUserName() ?? Request["user_name"];
                string currentRole = Request["currentRole"] ?? "";

                if (string.IsNullOrEmpty(bastNumber) || string.IsNullOrEmpty(actionType))
                {
                    return Json(new { success = false, message = "BAST Number and Action Type are required." }, JsonRequestBehavior.AllowGet);
                }

                var bast = dbb.Purchasing_BAST.FirstOrDefault(b => b.bast_number == bastNumber);
                if (bast == null)
                {
                    return Json(new { success = false, message = "BAST Data Not Found." }, JsonRequestBehavior.AllowGet);
                }

                var currentApprovalMaster = db.Approval_Master.FirstOrDefault(a =>
                    a.Menu_Id == 1050 &&
                    a.Document_Id == bast.id &&
                    a.User_NIK == currentUserId &&
                    (string.IsNullOrEmpty(currentRole) ? true : a.Title.ToLower() == currentRole.ToLower()) 
                );

                if (currentApprovalMaster == null)
                {
                    return Json(new { success = false, message = "You are not authorized to perform action on this document." }, JsonRequestBehavior.AllowGet);
                }

                string historyStatus = "";
                bool isReject = false;
                bool isRevise = false;

                switch (actionType.ToLower())
                {
                    case "approve":
                    case "sign":
                    case "signed":
                        historyStatus = "Sign";
                        break;

                    case "return":
                    case "returned":
                        historyStatus = "Return";
                        isRevise = true;
                        break;

                    case "reject":
                    case "rejected":
                        historyStatus = "Reject";
                        isReject = true;
                        break;

                    default:
                        return Json(new { success = false, message = "Invalid Action Type." }, JsonRequestBehavior.AllowGet);
                }

                if (historyStatus == "Sign")
                {
                    string roleTitle = currentApprovalMaster.Title.ToLower();

                    if (roleTitle == "creator")
                    {
                        bast.status = "Waiting for Checker";
                    }
                    else if (roleTitle == "checker")
                    {
                        bast.status = "Waiting for Approver";
                    }
                    else if (roleTitle == "approver")
                    {
                        bast.status = "Waiting for Final Approver";
                    }
                    else if (roleTitle == "final approver")
                    {
                        bast.status = "Fully Approved";
                    }
                }
                else if (historyStatus == "Return")
                {
                    bast.status = $"Returned by {currentApprovalMaster.Title}";
                }
                else if (historyStatus == "Reject")
                {
                    bast.status = $"Rejected by {currentApprovalMaster.Title}";
                }

                dbb.SaveChanges();

                var history = new Approval_History
                {
                    Menu_Id = 1050,
                    Menu_Name = "BAST",
                    Document_Id = bast.id,
                    Document_Name = bast.title,
                    Reveral_ID = bast.bast_number,
                    Reveral_ID_Sub = null,
                    Title = currentApprovalMaster.Title, // Creator / Checker / Approver
                    Header = currentApprovalMaster.Header ?? currentUserName, // Nama Pegawai
                    Label = bast.bast_number,
                    Note = note,
                    Approval = currentApprovalMaster.Levels,
                    Approval_Sub = currentApprovalMaster.Levels_Sub,
                    IsReject = isReject,
                    IsRevise = isRevise,
                    Status = historyStatus, // 'Sign', 'Return', atau 'Reject'
                    Created_At = DateTime.Now,
                    Created_By_ID = currentUserId,
                    Created_By_Name = currentUserName
                };

                db.Approval_History.Add(history);
                db.SaveChanges();

                if (historyStatus == "Sign")
                {
                    SendEmailNotification(bast, currentApprovalMaster, historyStatus);
                }

                return Json(new
                {
                    success = true,
                    message = $"Document has been successfully {historyStatus.ToLower()}ed."
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error processing approval: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==========================================
        // COMMENTS FEATURE (jquery-comments)
        // ==========================================

        [HttpGet]
        public JsonResult GetCommentUsers(string bast_number)
        {
            try
            {
                var currUserNik = User.Identity.GetUserId();

                if (string.IsNullOrEmpty(bast_number))
                {
                    return Json(new { success = false, message = "bast_number is required." }, JsonRequestBehavior.AllowGet);
                }

                // 1. Ambil list NIK unik dari Approval_Master berdasarkan Label = bast_number
                var approverNiks = db.Approval_Master
                    .Where(a => a.Menu_Id == 1050 && a.Label == bast_number)
                    .Select(a => a.User_NIK)
                    .Distinct()
                    .ToList();

                // 2. Ambil detail user dari V_Users_Active yang NIK-nya ada di daftar approverNiks
                var users = db.V_Users_Active
                    .Where(u => approverNiks.Contains(u.NIK))
                    .Select(u => new
                    {
                        id = u.NIK,
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
        public JsonResult GetComments(string bast_number)
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            var currUserNik = _currUser.GetUserId();
            try
            {
                var commentsData = dbb.Purchasing_BAST_Comments
                    .Where(c => c.bast_number == bast_number)
                    .OrderBy(c => c.created)
                    .ToList();

                var commentIds = commentsData.Select(c => c.c_id).ToList();
                var filesMetadata = dbb.Purchasing_BAST_Comment_Files
                    .Where(f => commentIds.Contains(f.c_id))
                    .ToList();

                var result = commentsData.Select(comment =>
                {
                    var currentAttachments = filesMetadata
                        .Where(f => f.c_id == comment.c_id)
                        .Select(f =>
                        {
                            string filePath = f.file_name;
                            if (filePath.Contains(@"\Files\"))
                            {
                                filePath = filePath.Substring(filePath.IndexOf(@"\Files\"))
                                                   .Replace(@"\", "/");
                            }

                            var baseUrl = Request.ApplicationPath == "/" ? "" : Request.ApplicationPath;
                            string fileUrl = $"{baseUrl}{filePath}";

                            return new
                            {
                                id = f.id,
                                file = fileUrl,
                                mimeType = f.mime_type,
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
                        c_id = comment.c_id,
                        creator = comment.creator,
                        fullname = comment.fullName,
                        created_by_current_user = (comment.creator == currUserNik)
                    };
                }).ToList();

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "";
                return Json(new { success = false, message = ex.Message, inner }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveComment()
        {
            try
            {
                string bast_number = Request.Form["bast_number"];
                string bast_title = Request.Form["bast_title"]; // Tangkap BAST Title

                if (string.IsNullOrEmpty(bast_number))
                    return Json(new { success = false, message = "bast_number is required." });

                var currCurrentUser = Request["created_by_current_user"];
                var comment = new Purchasing_BAST_Comments();

                comment.bast_number = bast_number;
                comment.created = DateTime.Now;
                if (bool.TryParse(currCurrentUser, out bool isCurrentUser))
                    comment.created_by_current_user = isCurrentUser;

                string parent = Request.Form["parent"];
                if (string.IsNullOrEmpty(parent) || parent == "null" || parent == "{}")
                    comment.parent = null;
                else
                    comment.parent = parent;

                string pings = Request.Form["pings"];
                if (string.IsNullOrEmpty(pings) || pings == "{}")
                    comment.pings = null;
                else
                    comment.pings = pings;

                comment.content = string.IsNullOrWhiteSpace(Request.Form["content"]) ? "-" : Request.Form["content"];
                comment.creator = Request.Form["creator"];

                string rawFullName = Request.Form["fullName"] ?? Request.Form["fullname"] ?? "";

                rawFullName = rawFullName.Replace("You,", "").Replace("You ,", "").Trim();

                if (rawFullName.Contains(","))
                {
                    rawFullName = rawFullName.Split(',')[0].Trim();
                }

                comment.fullName = string.IsNullOrWhiteSpace(rawFullName) ? "-" : rawFullName;

                // Generate next comment id ("c1", "c2", ...)
                var lastComment = dbb.Purchasing_BAST_Comments
                    .Where(c => c.bast_number == bast_number)
                    .OrderByDescending(c => c.c_id)
                    .FirstOrDefault();

                if (lastComment == null)
                {
                    comment.id = "c1";
                }
                else
                {
                    string lastCid = lastComment.id;
                    int num = 1;
                    if (!string.IsNullOrEmpty(lastCid) && lastCid.Length > 1)
                    {
                        int.TryParse(lastCid.Substring(1), out num);
                        num++;
                    }
                    comment.id = "c" + num;
                }

                dbb.Purchasing_BAST_Comments.Add(comment);
                dbb.SaveChanges();

                // Save uploaded files
                var commentFilesMetadata = new List<Purchasing_BAST_Comment_Files>();
                comment.comment_files = new List<Purchasing_BAST_Comment_Files>();

                if (Request.Files.Count > 0)
                {
                    string baseDirectory = Server.MapPath($"~/Files/BAST/Comments/{bast_number}/{comment.c_id}/");
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

                            var fileMetadata = new Purchasing_BAST_Comment_Files
                            {
                                c_id = comment.c_id,
                                file_name = filePath,
                                mime_type = file.ContentType
                            };
                            commentFilesMetadata.Add(fileMetadata);
                            dbb.Purchasing_BAST_Comment_Files.Add(fileMetadata);
                        }
                    }
                    dbb.SaveChanges();
                }

                var attachmentsForJS = commentFilesMetadata.Select(f => new
                {
                    id = f.id,
                    file = f.file_name,
                    mimeType = f.mime_type,
                    url = $"/Files/BAST/Comments/{bast_number}/{comment.c_id}/{Path.GetFileName(f.file_name)}"
                }).ToList();

                string parentId = string.IsNullOrEmpty(comment.parent) || comment.parent == "null" ? null : comment.parent;

                // Fire & forget send email dengan mengirimkan bast_title
                try
                {
                    string currentNik = User.Identity.GetUserId() ?? comment.creator ?? "";
                    SendCommentEmailNotification(bast_number, bast_title, currentNik, comment.fullName, comment.content);
                }
                catch (Exception emailEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMAIL TRIGGER ERROR] {emailEx.Message}");
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = comment.id,
                        c_id = comment.c_id,
                        parent = parentId,
                        created = comment.created.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        modified = comment.modified,
                        content = string.IsNullOrWhiteSpace(comment.content) ? "-" : comment.content,
                        attachments = attachmentsForJS,
                        pings = comment.pings,
                        creator = comment.creator,
                        fullname = comment.fullName,
                        created_by_current_user = true
                    }
                });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;

                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner: " + ex.InnerException.Message;

                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += " | Detail: " + ex.InnerException.InnerException.Message;
                    }
                }

                return Json(new { success = false, message = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult UpdateComment()
        {
            try
            {
                string commentId = Request.Form["id"];
                string bast_number = Request.Form["bast_number"];
                string bast_title = Request.Form["bast_title"]; // Tangkap BAST Title
                string creator = Request.Form["creator"];

                if (string.IsNullOrEmpty(commentId) || string.IsNullOrEmpty(bast_number))
                    return Json(new { success = false, message = "Invalid comment id or bast_number." });

                var existingComment = dbb.Purchasing_BAST_Comments
                    .FirstOrDefault(c => c.bast_number == bast_number && c.id == commentId);

                if (existingComment == null)
                    return Json(new { success = false, message = "Comment not found." });

                if (!string.Equals(existingComment.creator?.Trim(), creator?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "You can only edit your own comment." });

                string newContent = Request.Form["content"];
                if (string.IsNullOrWhiteSpace(newContent))
                    return Json(new { success = false, message = "Comment content cannot be empty." });

                existingComment.content = newContent;
                existingComment.modified = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                existingComment.pings = Request.Form["pings"];

                // Handle deleted files from client
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

                var filesInDb = dbb.Purchasing_BAST_Comment_Files
                    .Where(f => f.c_id == existingComment.c_id)
                    .ToList();

                var filesToDelete = filesInDb
                    .Where(f => !existingFilesInClient.Contains(Path.GetFileName(f.file_name)))
                    .ToList();

                foreach (var file in filesToDelete)
                {
                    try { if (System.IO.File.Exists(file.file_name)) System.IO.File.Delete(file.file_name); } catch { }
                    dbb.Purchasing_BAST_Comment_Files.Remove(file);
                }

                // Upload new files
                var commentFilesMetadata = new List<Purchasing_BAST_Comment_Files>();
                if (Request.Files.Count > 0)
                {
                    string baseDirectory = Server.MapPath($"~/Files/BAST/Comments/{bast_number}/{existingComment.c_id}/");
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

                            var fileMetadata = new Purchasing_BAST_Comment_Files
                            {
                                c_id = existingComment.c_id,
                                file_name = filePath,
                                mime_type = file.ContentType
                            };
                            commentFilesMetadata.Add(fileMetadata);
                            dbb.Purchasing_BAST_Comment_Files.Add(fileMetadata);
                        }
                    }
                }

                dbb.SaveChanges();

                var allFiles = dbb.Purchasing_BAST_Comment_Files
                    .Where(f => f.c_id == existingComment.c_id)
                    .ToList()
                    .Select(f =>
                    {
                        string fileNameOnly = Path.GetFileName(f.file_name);
                        string url = $"/Files/BAST/Comments/{bast_number}/{existingComment.c_id}/{fileNameOnly}";
                        return new
                        {
                            id = f.id,
                            file = fileNameOnly,
                            mimeType = f.mime_type,
                            url = url
                        };
                    }).ToList();

                // Kirim Notifikasi Email dengan bast_title
                SendCommentEmailNotification(bast_number, bast_title, existingComment.creator, existingComment.fullName, existingComment.content);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = existingComment.id,
                        parent = existingComment.parent,
                        content = existingComment.content,
                        created = existingComment.created.HasValue
                            ? existingComment.created.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            : null,
                        modified = existingComment.modified,
                        attachments = allFiles,
                        pings = existingComment.pings,
                        creator = existingComment.creator,
                        fullname = existingComment.fullName,
                        created_by_current_user = true
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error during update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteComment(string commentId, string bast_number)
        {
            try
            {
                string currentUserNik = User.Identity.GetUserId().Trim();

                var commentToDelete = dbb.Purchasing_BAST_Comments
                    .FirstOrDefault(c => c.id == commentId && c.bast_number == bast_number);

                if (commentToDelete == null)
                    return Json(new { success = false, message = "Comment not found." });

                if (commentToDelete.creator?.Trim() != currentUserNik)
                    return Json(new { success = false, message = "You are not authorized to delete this comment." });

                var filesToDelete = dbb.Purchasing_BAST_Comment_Files
                    .Where(f => f.c_id == commentToDelete.c_id).ToList();

                foreach (var fileMeta in filesToDelete)
                {
                    try { if (System.IO.File.Exists(fileMeta.file_name)) System.IO.File.Delete(fileMeta.file_name); } catch { }
                }

                dbb.Purchasing_BAST_Comment_Files.RemoveRange(filesToDelete);
                dbb.Purchasing_BAST_Comments.Remove(commentToDelete);
                dbb.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error during deletion: " + ex.Message });
            }
        }

        // ==========================================
        // EMAIL NOTIFICATION
        // ==========================================
        [HttpPost]
        public JsonResult SendReminderEmail(string bast_number, string target_nik)
        {
            try
            {
                if (string.IsNullOrEmpty(bast_number))
                    return Json(new { success = false, message = "BAST Number is required." });

                var bast = dbb.Purchasing_BAST.FirstOrDefault(b => b.bast_number == bast_number);
                if (bast == null)
                    return Json(new { success = false, message = "BAST record not found." });

                var targetApprover = db.Approval_Master.FirstOrDefault(a => a.Menu_Id == 1050
                    && a.Document_Id == bast.id && a.User_NIK == target_nik);
                if (targetApprover == null)
                    return Json(new { success = false, message = "Target approver not found." });

                var targetUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == target_nik);
                if (targetUser == null || string.IsNullOrEmpty(targetUser.Email))
                    return Json(new { success = false, message = "Target user email not found." });

                // --- Cari Email Creator untuk CC ---
                string creatorEmail = "";
                string creatorName = bast.creator_name ?? "Creator";

                string creatorNik = bast.creator_nik ?? "";

                if (!string.IsNullOrEmpty(creatorNik))
                {
                    var creatorUser = db.V_Users_Active.FirstOrDefault(u => u.NIK.Equals(creatorNik.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (creatorUser != null && !string.IsNullOrWhiteSpace(creatorUser.Email))
                    {
                        creatorEmail = creatorUser.Email.Trim();
                        if (!string.IsNullOrEmpty(creatorUser.Name))
                        {
                            creatorName = creatorUser.Name;
                        }
                    }
                }

                // Template
                string templatePath = Server.MapPath("~/Files/Purchasing/BAST/notif.html");
                if (!System.IO.File.Exists(templatePath))
                    return Json(new { success = false, message = "Email template not found." });

                string mailText = System.IO.File.ReadAllText(templatePath);

                string senderAddress = "ngkportal-notification@ngkbusi.com";
                string senderName = "BAST Notification";
                string password = "100%NGKbusi!";
                string baseUrl = "https://portal.ngkbusi.com";
                string linkUrl = $"{baseUrl}/NGKBusi/Purchasing/BAST/Detail?bast_number={bast.bast_number}";

                string dueDateDisplay = bast.due_date.HasValue
                    ? bast.due_date.Value.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("en-US"))
                    : "-";

                string mailBody = mailText
                    .Replace("[Approver Name]", targetUser.Name ?? target_nik)
                    .Replace("[BAST Number]", bast.bast_number ?? "-")
                    .Replace("[BAST Title]", bast.title ?? "-")
                    .Replace("[Vendor Name]", bast.vendor_name ?? "-")
                    .Replace("[Due Date]", dueDateDisplay)
                    .Replace("[Creator Name]", creatorName)
                    .Replace("[Link URL]", linkUrl);

                var senderEmail = new MailAddress(senderAddress, senderName);
                var receiverMail = new MailAddress(targetUser.Email, targetUser.Name ?? target_nik);

                using (var message = new MailMessage(senderEmail, receiverMail)
                {
                    Subject = $"[BAST Reminder] {bast.bast_number} - Pending Approval",
                    Body = mailBody,
                    IsBodyHtml = true
                })
                {
                    // Tambahkan CC ke Creator jika emailnya ada dan tidak sama dengan email Target (To)
                    if (!string.IsNullOrEmpty(creatorEmail) && !creatorEmail.Equals(targetUser.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        message.CC.Add(new MailAddress(creatorEmail, creatorName));
                    }

                    using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                    {
                        smtp.EnableSsl = false;
                        smtp.Credentials = new NetworkCredential(senderEmail.Address, password);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                        smtp.Send(message);
                    }
                }

                return Json(new { success = true, message = $"Reminder email sent to {targetUser.Name}!" });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "";
                return Json(new { success = false, message = "Error: " + ex.Message, inner = innerMsg });
            }
        }
        private void SendEmailNotification(Purchasing_BAST bast, Approval_Master currentApprover, string historyStatus)
        {
            try
            {
                string senderAddress = "ngkportal-notification@ngkbusi.com";
                string senderName = "BAST Notification";
                string password = "100%NGKbusi!";

                string baseUrl = "https://portal.ngkbusi.com";
                string linkUrl = $"{baseUrl}/NGKBusi/Purchasing/BAST/Detail?bast_number={bast.bast_number}";

                string receiverNik = "";
                string receiverName = "";
                string subject = "";
                string templatePath = "";

                // Cari level selanjutnya (Levels_Sub + 1)
                int nextSubLevel = currentApprover.Levels_Sub + 1;

                var nextApprover = db.Approval_Master
                    .FirstOrDefault(a => a.Menu_Id == 1050
                                      && a.Document_Id == bast.id
                                      && a.Levels_Sub == nextSubLevel);

                if (nextApprover == null)
                {
                    // Tidak ada level selanjutnya (Fully Approved) -> Kirim ke Creator
                    receiverNik = bast.creator_nik;
                    receiverName = bast.creator_name;
                    subject = $"[BAST Fully Approved] {bast.bast_number} - {bast.title}";
                    templatePath = Server.MapPath("~/Files/Purchasing/BAST/notif_approved.html");
                }
                else
                {
                    // Ada level selanjutnya (Pending Approval) -> Kirim ke Approver Selanjutnya
                    receiverNik = nextApprover.User_NIK;
                    receiverName = nextApprover.Header ?? nextApprover.User_NIK;
                    subject = $"[BAST Pending Approval] {bast.bast_number} - {bast.title}";
                    templatePath = Server.MapPath("~/Files/Purchasing/BAST/notif.html");
                }

                if (!System.IO.File.Exists(templatePath)) return;

                string mailText = System.IO.File.ReadAllText(templatePath);

                if (string.IsNullOrEmpty(receiverNik)) return;

                // 1. Dapatkan Email Penerima Utama (To)
                var receiver = db.V_Users_Active.FirstOrDefault(u => u.NIK == receiverNik);
                if (receiver == null || string.IsNullOrEmpty(receiver.Email)) return;

                // 2. Dapatkan Email Creator untuk CC (Jika To bukan Creator)
                string ccEmail = "";
                if (receiverNik != bast.creator_nik && !string.IsNullOrEmpty(bast.creator_nik))
                {
                    var creatorUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == bast.creator_nik);
                    if (creatorUser != null && !string.IsNullOrEmpty(creatorUser.Email))
                    {
                        ccEmail = creatorUser.Email;
                    }
                }

                string dueDateDisplay = bast.due_date.HasValue
                    ? bast.due_date.Value.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("en-US"))
                    : "-";

                string mailBody = mailText
                    .Replace("[Approver Name]", receiver.Name ?? receiverNik)
                    .Replace("[BAST Number]", bast.bast_number ?? "-")
                    .Replace("[BAST Title]", bast.title ?? "-")
                    .Replace("[Vendor Name]", bast.vendor_name ?? "-")
                    .Replace("[Due Date]", dueDateDisplay)
                    .Replace("[Creator Name]", bast.creator_name ?? "-")
                    .Replace("[Link URL]", linkUrl);

                var senderEmail = new MailAddress(senderAddress, senderName);
                var receiverMail = new MailAddress(receiver.Email, receiver.Name ?? receiverNik);

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
                        // 3. Tambahkan CC ke dalam MailMessage jika ccEmail ditemukan
                        if (!string.IsNullOrEmpty(ccEmail))
                        {
                            message.CC.Add(new MailAddress(ccEmail, bast.creator_name ?? bast.creator_nik));
                        }

                        smtp.Send(message);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[BAST EMAIL] Sent to {receiver.Email} | CC: {(string.IsNullOrEmpty(ccEmail) ? "None" : ccEmail)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BAST EMAIL ERROR] {ex.Message}");
            }
        }

        private void SendCommentEmailNotification(string bast_number, string bast_title, string commenterNik, string commenterName, string commentContent)
        {
            try
            {
                // Null & Empty Guarding
                bast_number = (bast_number ?? "").Trim();
                if (string.IsNullOrEmpty(bast_number)) return;

                // 1. Pastikan BAST ada
                var bast = dbb.Purchasing_BAST.FirstOrDefault(b => b.bast_number == bast_number);
                if (bast == null) return;

                // Tentukan BAST Title
                string bastTitleDisplay = !string.IsNullOrWhiteSpace(bast_title) ? bast_title.Trim() : (bast.title ?? bast_number);

                // 2. Load Template HTML
                string templatePath = Server.MapPath("~/Files/Purchasing/BAST/comment.html");
                if (!System.IO.File.Exists(templatePath)) return;
                string mailText = System.IO.File.ReadAllText(templatePath);

                // 3. Konfigurasi Email System
                string senderAddress = "ngkportal-notification@ngkbusi.com";
                string senderName = "BAST Comment";
                string password = "100%NGKbusi!";
                string baseUrl = "https://portal.ngkbusi.com";
                string linkUrl = $"{baseUrl}/NGKBusi/Purchasing/BAST/Detail?bast_number={bast_number}";

                // 4. Sanitasi NIK & Nama Pembuat Komentar
                string cleanCommenterNik = (commenterNik ?? "").Trim();
                if (cleanCommenterNik.Contains(","))
                {
                    cleanCommenterNik = cleanCommenterNik.Split(',')[0].Trim();
                }

                string cleanCommenterName = (commenterName ?? "").Replace("You,", "").Trim();
                if (string.IsNullOrEmpty(cleanCommenterName))
                {
                    cleanCommenterName = commenterNik ?? "User";
                }

                // 5. Tarik Email Pembuat Komentar
                string commenterEmail = "";
                if (!string.IsNullOrEmpty(cleanCommenterNik))
                {
                    var commenterUser = db.V_Users_Active.FirstOrDefault(u => u.NIK.Equals(cleanCommenterNik, StringComparison.OrdinalIgnoreCase));
                    if (commenterUser != null && !string.IsNullOrWhiteSpace(commenterUser.Email))
                    {
                        commenterEmail = commenterUser.Email.Trim();
                    }
                }

                // 6. Ambil Semua Target NIK dari Approval_History (created_by_id)
                // Sesuaikan 'db.Approval_History' atau 'dbb.Approval_History' serta nama kolom Menu_Id/Document_Id dengan DbContext Anda
                var historyNiks = db.Approval_History
                    .Where(a => a.Menu_Id == 1050 && a.Document_Id == bast.id && !string.IsNullOrEmpty(a.Created_By_ID))
                    .Select(a => a.Created_By_ID.Trim())
                    .Distinct()
                    .ToList();

                // Filter: Hanya kirim ke user di history yang BUKAN pembuat komentar saat ini
                var otherNiks = historyNiks
                    .Where(nik => !nik.Equals(cleanCommenterNik, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                List<string> finalTargetNiks = otherNiks.Any() ? otherNiks : historyNiks;

                if (!finalTargetNiks.Any()) return;

                string timestamp = DateTime.Now.ToString("dd MMM yyyy, HH:mm");
                string cleanContent = string.IsNullOrWhiteSpace(commentContent) ? "-" : commentContent.Replace("\n", "<br/>");

                // Subject email
                string subject = $"{cleanCommenterName} Commented on {bast_number} - {bastTitleDisplay}";

                // Replace tag template HTML
                string mailBody = mailText
                    .Replace("[BAST Number]", bast_number)
                    .Replace("[BAST Title]", bastTitleDisplay)
                    .Replace("[Title]", bastTitleDisplay)
                    .Replace("[Comment Content]", cleanContent)
                    .Replace("[Commenter Name]", cleanCommenterName)
                    .Replace("[Timestamp]", timestamp)
                    .Replace("[Link URL]", linkUrl);

                var senderEmail = new MailAddress(senderAddress, senderName);

                using (var message = new MailMessage())
                {
                    message.From = senderEmail;
                    message.Subject = subject;
                    message.Body = mailBody;
                    message.IsBodyHtml = true;

                    // Collect unique recipients to avoid duplicate emails
                    var addedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var rawTargetNik in finalTargetNiks)
                    {
                        string targetNik = (rawTargetNik ?? "").Trim();
                        var receiver = db.V_Users_Active.FirstOrDefault(u => u.NIK.Equals(targetNik, StringComparison.OrdinalIgnoreCase));

                        if (receiver == null || string.IsNullOrWhiteSpace(receiver.Email)) continue;

                        string emailTo = receiver.Email.Trim();

                        // Tambahkan ke 'To' jika belum pernah ditambahkan
                        if (addedEmails.Add(emailTo))
                        {
                            message.To.Add(new MailAddress(emailTo, receiver.Name ?? targetNik));
                        }
                    }

                    // Cegah pengiriman jika tidak ada email tujuan yang valid
                    if (!message.To.Any()) return;

                    // Tambahkan CC ke Pembuat Komentar jika belum ada di daftar 'To'
                    if (!string.IsNullOrEmpty(commenterEmail) && addedEmails.Add(commenterEmail))
                    {
                        message.CC.Add(new MailAddress(commenterEmail, cleanCommenterName));
                    }

                    // Kirim email sekali saja ke seluruh penerima
                    using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                    {
                        smtp.EnableSsl = false;
                        smtp.Credentials = new NetworkCredential(senderEmail.Address, password);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                        smtp.Send(message);
                    }

                    System.Diagnostics.Debug.WriteLine($"[BAST COMMENT EMAIL SUCCESS] Sent batch email to {message.To.Count} recipients | CC: {commenterEmail}");
                }
            }
            catch (Exception ex)
            {
                string errorDetail = ex.Message + (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "");
                System.Diagnostics.Debug.WriteLine($"[BAST COMMENT EMAIL ERROR] {errorDetail}");
            }
        }
    }
}