using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.SCM.Controllers
{
    [Authorize]
    public class SparepartPORequestController : Controller
    {
        private DefaultConnection db = new DefaultConnection();
        private SparepartConnection dbsp = new SparepartConnection();

        private bool IsConfiguredApprover(string nik)
        {
            return dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                .Any(c => c.NIK == nik && c.IsApprover && c.IsActive);
        }

        // GET: SCM/SparepartPORequest
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);
            ViewBag.Role = CurrUser != null ? CurrUser.RoleName : "User";

            ViewBag.CanCreate = IsConfiguredApprover(currUser);

            bool isAdmin = User.IsInRole("Administrator") || User.IsInRole("AdminSparepart");

            IQueryable<SCM_Sparepart_PO_Request_Header> query = dbsp.SCM_Sparepart_PO_Request_Headers;

            if (!isAdmin)
            {
                var myApprovals = db.Task_Approval_Lists
                    .Where(t => t.NIK == currUser && t.Module == "SCM Sparepart PO Request")
                    .Select(t => t.Referal_id)
                    .Distinct()
                    .ToList();

                query = query.Where(r => r.RequesterNIK == currUser || myApprovals.Contains(r.RequestNo));
            }

            var list = query.Include(r => r.Details).OrderByDescending(r => r.CreatedDate).ToList();

            // KPI Summary Counts
            ViewBag.TotalCount = list.Count;
            ViewBag.PendingCount = list.Count(r => r.Status == "Pending Approval");
            ViewBag.CompletedCount = list.Count(r => r.Status == "Completed" || r.Status == "Approved");
            ViewBag.RejectedCount = list.Count(r => r.Status == "Rejected");
            ViewBag.DraftCount = list.Count(r => r.Status == "Draft");
            ViewBag.RegularCount = list.Count(r => string.IsNullOrEmpty(r.Category) || r.Category == "Regular");
            ViewBag.ProjectCount = list.Count(r => r.Category == "Project");
            ViewBag.CurrentNIK = currUser;
            ViewBag.NavHide = true;

            // Department ID to Name mapping
            var deptMap = db.V_Users_Active
                .Where(u => u.AXCostID != null && u.AXCostID != "" && u.AXCostName != null && u.AXCostName != "")
                .Select(u => new { u.AXCostID, u.AXCostName })
                .Distinct()
                .ToList()
                .GroupBy(u => u.AXCostID)
                .ToDictionary(g => g.Key, g => g.First().AXCostName);
            ViewBag.DeptMap = deptMap;

            // Requester NIK to Name mapping
            var reqNiks = list.Select(r => r.RequesterNIK).Distinct().ToList();
            var requesterMap = db.V_Users_Active
                .Where(u => reqNiks.Contains(u.NIK))
                .Select(u => new { u.NIK, u.Name })
                .Distinct()
                .ToList()
                .GroupBy(u => u.NIK)
                .ToDictionary(g => g.Key, g => g.First().Name);
            ViewBag.RequesterMap = requesterMap;

            // Pending Approvers mapping for Pending Approval requests
            var pendingReqNos = list.Where(r => r.Status == "Pending Approval").Select(r => r.RequestNo).ToList();
            var activeTasks = db.Task_Approval_Lists
                .Where(t => pendingReqNos.Contains(t.Referal_id) && !t.IsCompleteTask && t.Module == "SCM Sparepart PO Request")
                .ToList();

            var pendingLogs = dbsp.SCM_Sparepart_PO_Approval_Logs
                .Where(l => pendingReqNos.Contains(l.RequestNo))
                .ToList();

            var approverNiks = activeTasks.Select(t => t.NIK).Distinct().ToList();
            var approverUserMap = db.V_Users_Active
                .Where(u => approverNiks.Contains(u.NIK))
                .Select(u => new { u.NIK, u.Name })
                .Distinct()
                .ToList()
                .GroupBy(u => u.NIK)
                .ToDictionary(g => g.Key, g => g.First().Name);

            var configList = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                .Where(c => c.IsActive && approverNiks.Contains(c.NIK))
                .ToList();

            var pendingApproversMap = new Dictionary<string, List<string>>();
            foreach (var reqNo in pendingReqNos)
            {
                var tasksForReq = activeTasks.Where(t => t.Referal_id == reqNo).ToList();
                var header = list.FirstOrDefault(r => r.RequestNo == reqNo);
                var targetDepts = header != null && !string.IsNullOrEmpty(header.TargetDepartment)
                    ? header.TargetDepartment.Split(',')
                    : new string[0];

                var logsForReq = pendingLogs.Where(l => l.RequestNo == reqNo).ToList();
                int maxApprovedLevel = logsForReq.Where(l => l.Action == "Approved").Select(l => l.Level).DefaultIfEmpty(1).Max();

                var approverLabels = new List<string>();
                foreach (var task in tasksForReq)
                {
                    if (task.TaskTitle != null && task.TaskTitle.StartsWith("[SKIPPED]"))
                    {
                        continue;
                    }

                    var cfg = configList.FirstOrDefault(c => c.NIK == task.NIK && targetDepts.Contains(c.Department));
                    if (cfg == null)
                    {
                        cfg = configList.FirstOrDefault(c => c.NIK == task.NIK);
                    }

                    // Abaikan approver yang opsional
                    if (cfg != null && cfg.IsOptional)
                    {
                        continue;
                    }

                    // Abaikan approver dengan level <= level yang sudah diapprove
                    if (cfg != null && cfg.Level <= maxApprovedLevel)
                    {
                        continue;
                    }

                    string name = approverUserMap.ContainsKey(task.NIK) ? approverUserMap[task.NIK] : task.NIK;

                    string roleLabel = "";
                    if (cfg != null && !string.IsNullOrWhiteSpace(cfg.TaskTitle))
                    {
                        roleLabel = " (" + cfg.TaskTitle + ")";
                    }
                    else if (cfg != null)
                    {
                        roleLabel = " (Level " + cfg.Level + ")";
                    }

                    approverLabels.Add(name + roleLabel);
                }

                if (approverLabels.Any())
                {
                    pendingApproversMap[reqNo] = approverLabels.Distinct().ToList();
                }
            }
            ViewBag.PendingApproversMap = pendingApproversMap;

            return View(list);
        }

        // GET: SCM/SparepartPORequest/Create
        public ActionResult Create(int? id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);

            if (!IsConfiguredApprover(currUser))
            {
                TempData["Error"] = "Akses ditolak. Hanya user yang terdaftar di SCM_Sparepart_PO_Request_Approval_Config dengan IsApprover = true dan IsActive = true yang dapat membuat/melihat pengajuan.";
                return RedirectToAction("Index");
            }
            
            // Get configured department IDs from SCM_Sparepart_PO_Request_Approval_Config
            var configuredDeptIds = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                .Where(c => c.IsActive)
                .Select(c => c.Department)
                .Distinct()
                .ToList();

            // Get departments list using AXCostID and AXCostName
            ViewBag.Departments = db.V_Users_Active
                .Where(u => configuredDeptIds.Contains(u.AXCostID) && u.AXCostID != null && u.AXCostID != "" && u.AXCostName != null && u.AXCostName != "")
                .Select(u => new { u.AXCostID, u.AXCostName })
                .Distinct()
                .OrderBy(u => u.AXCostName)
                .ToList()
                .Select(u => new System.Web.Mvc.SelectListItem
                {
                    Value = u.AXCostID,
                    Text = u.AXCostName
                })
                .ToList();

            if (id.HasValue)
            {
                var draft = dbsp.SCM_Sparepart_PO_Request_Headers
                    .Include(d => d.Details)
                    .Include(d => d.Attachments)
                    .FirstOrDefault(d => d.ID == id.Value && d.Status == "Draft" && d.RequesterNIK == currUser);

                if (draft != null)
                {
                    return View(draft);
                }
            }

            return View(new SCM_Sparepart_PO_Request_Header
            {
                CreatedDate = DateTime.Now,
                Category = "Regular",
                ReminderDaysAfter = 1,
                ReminderRepeatInterval = "None",
                ReminderSendTime = 8
            });
        }

        // POST: SCM/SparepartPORequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int? ID, string Title, string Category, string[] TargetDepartment, int? ReminderDaysAfter, bool? IsReminderRepeated, string ReminderRepeatInterval, int? ReminderCustomDays, int? ReminderSendTime, HttpPostedFileBase excelFile, HttpPostedFileBase[] attachments, string submitType)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            if (!IsConfiguredApprover(currUser))
            {
                TempData["Error"] = "Akses ditolak. Hanya user yang terdaftar di SCM_Sparepart_PO_Request_Approval_Config dengan IsApprover = true dan IsActive = true yang dapat membuat/melihat pengajuan.";
                return RedirectToAction("Index");
            }

            string targetDeptsStr = TargetDepartment != null ? string.Join(",", TargetDepartment) : "";
            int sendTimeVal = (ReminderSendTime.HasValue && ReminderSendTime.Value > 0) ? ReminderSendTime.Value : 8;
            int reminderDaysVal = (ReminderDaysAfter.HasValue && ReminderDaysAfter.Value > 0) ? ReminderDaysAfter.Value : 1;
            bool isRepeatedVal = IsReminderRepeated.GetValueOrDefault(false);
            string repeatIntervalVal = isRepeatedVal ? (!string.IsNullOrEmpty(ReminderRepeatInterval) ? ReminderRepeatInterval : "Daily") : "None";
            int? customDaysVal = (isRepeatedVal && repeatIntervalVal == "Custom") ? ReminderCustomDays : null;

            if (submitType == "Submit")
            {
                if (string.IsNullOrWhiteSpace(Title))
                {
                    ModelState.AddModelError("Title", "Title / Judul Request wajib diisi.");
                }
                if (TargetDepartment == null || TargetDepartment.Length == 0)
                {
                    ModelState.AddModelError("TargetDepartment", "Target Department wajib dipilih minimal 1.");
                }
                
                // Validate Excel file (must be uploaded or already exist in DB for editing)
                bool hasExcel = (excelFile != null && excelFile.ContentLength > 0);
                bool hasExistingDetails = false;
                if (ID.HasValue && ID.Value > 0)
                {
                    hasExistingDetails = dbsp.SCM_Sparepart_PO_Request_Details.Any(d => d.HeaderID == ID.Value);
                }
                if (!hasExcel && !hasExistingDetails)
                {
                    ModelState.AddModelError("ExcelFile", "File Excel berisi data item wajib diunggah.");
                }

                if (!ReminderDaysAfter.HasValue || ReminderDaysAfter.Value < 1)
                {
                    ModelState.AddModelError("ReminderDaysAfter", "Kapan email reminder dikirim (H+ Hari) wajib diset minimal 1 hari.");
                }

                if (isRepeatedVal && repeatIntervalVal == "Custom" && (!ReminderCustomDays.HasValue || ReminderCustomDays.Value < 1))
                {
                    ModelState.AddModelError("ReminderCustomDays", "Ulangi Setiap (Hari) wajib diset minimal 1 hari jika interval Kustom.");
                }
            }
            else
            {
                // In draft mode, only Title is required
                ModelState.Clear();
                if (string.IsNullOrWhiteSpace(Title))
                {
                    ModelState.AddModelError("Title", "Title / Judul Request wajib diisi untuk menyimpan Draft.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Get configured department IDs from SCM_Sparepart_PO_Request_Approval_Config
                var configuredDeptIds = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .Where(c => c.IsActive)
                    .Select(c => c.Department)
                    .Distinct()
                    .ToList();

                ViewBag.Departments = db.V_Users_Active
                    .Where(u => configuredDeptIds.Contains(u.AXCostID) && u.AXCostID != null && u.AXCostID != "" && u.AXCostName != null && u.AXCostName != "")
                    .Select(u => new { u.AXCostID, u.AXCostName })
                    .Distinct()
                    .OrderBy(u => u.AXCostName)
                    .ToList()
                    .Select(u => new System.Web.Mvc.SelectListItem
                    {
                        Value = u.AXCostID,
                        Text = u.AXCostName
                    })
                    .ToList();

                var errorMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = string.Join("<br/>", errorMessages);

                if (ID.HasValue && ID.Value > 0)
                {
                    var draft = dbsp.SCM_Sparepart_PO_Request_Headers
                        .Include("Details")
                        .Include("Attachments")
                        .FirstOrDefault(d => d.ID == ID.Value);
                    if (draft != null)
                    {
                        return View(draft);
                    }
                }

                return View(new SCM_Sparepart_PO_Request_Header
                {
                    Title = Title,
                    TargetDepartment = targetDeptsStr,
                    ReminderDaysAfter = reminderDaysVal,
                    IsReminderRepeated = isRepeatedVal,
                    ReminderRepeatInterval = repeatIntervalVal,
                    ReminderCustomDays = customDaysVal,
                    ReminderSendTime = sendTimeVal
                });
            }

            SCM_Sparepart_PO_Request_Header header;

            if (ID.HasValue && ID.Value > 0)
            {
                header = dbsp.SCM_Sparepart_PO_Request_Headers.Find(ID.Value);
                if (header == null || header.RequesterNIK != currUser || header.Status != "Draft")
                {
                    return HttpNotFound();
                }

                string categoryVal = (!string.IsNullOrWhiteSpace(Category) && (Category.Trim() == "Project" || Category.Trim() == "Regular"))
                    ? Category.Trim()
                    : "Regular";

                header.Title = Title;
                header.Category = categoryVal;
                header.TargetDepartment = targetDeptsStr;
                header.ReminderDaysAfter = reminderDaysVal;
                header.IsReminderRepeated = isRepeatedVal;
                header.ReminderRepeatInterval = repeatIntervalVal;
                header.ReminderCustomDays = customDaysVal;
                header.ReminderSendTime = sendTimeVal;
            }
            else
            {
                string categoryVal = (!string.IsNullOrWhiteSpace(Category) && (Category.Trim() == "Project" || Category.Trim() == "Regular"))
                    ? Category.Trim()
                    : "Regular";

                string requestNo = "SPO-" + DateTime.Now.ToString("yyyyMMdd") + "-" + (dbsp.SCM_Sparepart_PO_Request_Headers.Count() + 1).ToString("D3");
                header = new SCM_Sparepart_PO_Request_Header
                {
                    Title = Title,
                    Category = categoryVal,
                    RequestNo = requestNo,
                    RequesterNIK = currUser,
                    CreatedDate = DateTime.Now,
                    Status = "Draft",
                    TargetDepartment = targetDeptsStr,
                    ReminderDaysAfter = reminderDaysVal,
                    IsReminderRepeated = isRepeatedVal,
                    ReminderRepeatInterval = repeatIntervalVal,
                    ReminderCustomDays = customDaysVal,
                    ReminderSendTime = sendTimeVal
                };
                dbsp.SCM_Sparepart_PO_Request_Headers.Add(header);
            }

            dbsp.SaveChanges();

            // Handle Excel Data Items Upload
            bool clearExistingExcel = false;
            if (Request.Form["ClearExistingExcel"] != null)
            {
                bool.TryParse(Request.Form["ClearExistingExcel"], out clearExistingExcel);
            }

            if (clearExistingExcel && ID.HasValue && ID.Value > 0)
            {
                var oldDetails = dbsp.SCM_Sparepart_PO_Request_Details.Where(d => d.HeaderID == header.ID);
                dbsp.SCM_Sparepart_PO_Request_Details.RemoveRange(oldDetails);
                dbsp.SaveChanges();
            }

            if (excelFile != null && excelFile.ContentLength > 0)
            {
                // Remove existing details if editing draft (and not already cleared)
                if (!clearExistingExcel && ID.HasValue && ID.Value > 0)
                {
                    var oldDetails = dbsp.SCM_Sparepart_PO_Request_Details.Where(d => d.HeaderID == header.ID);
                    dbsp.SCM_Sparepart_PO_Request_Details.RemoveRange(oldDetails);
                }

                using (var workbook = new XLWorkbook(excelFile.InputStream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet != null)
                    {
                        int row = 7;
                        while (!worksheet.Cell(row, 1).IsEmpty() || !worksheet.Cell(row, 2).IsEmpty() || !worksheet.Cell(row, 6).IsEmpty())
                        {
                            var detail = new SCM_Sparepart_PO_Request_Detail
                            {
                                HeaderID = header.ID,
                                PartName = worksheet.Cell(row, 3).GetString().Trim(),
                                MachineNumber = worksheet.Cell(row, 5).GetString().Trim(),
                                AxItemID = worksheet.Cell(row, 6).GetString().Trim(),
                                Maker = worksheet.Cell(row, 7).GetString().Trim(),
                                Dimensi = worksheet.Cell(row, 9).GetString().Trim(),
                                Machine = worksheet.Cell(row, 10).GetString().Trim(),
                                DrawingNo = worksheet.Cell(row, 11).GetString().Trim(),
                                DrawingNoKanrikogu = worksheet.Cell(row, 12).GetString().Trim(),
                                LeadTime = worksheet.Cell(row, 13).GetString().Trim()
                            };

                            var noCell = worksheet.Cell(row, 1);
                            if (!noCell.IsEmpty())
                            {
                                int tempNo;
                                if (int.TryParse(noCell.GetString(), out tempNo)) detail.No = tempNo;
                            }

                            var qtyCell = worksheet.Cell(row, 8);
                            if (!qtyCell.IsEmpty())
                            {
                                decimal tempQty;
                                if (decimal.TryParse(qtyCell.GetString(), out tempQty)) detail.Quantity = tempQty;
                            }

                            var etaCell = worksheet.Cell(row, 14);
                            if (!etaCell.IsEmpty())
                            {
                                if (etaCell.DataType == XLDataType.DateTime)
                                {
                                    detail.ETA = etaCell.GetDateTime();
                                }
                                else
                                {
                                    DateTime tempEta;
                                    if (DateTime.TryParse(etaCell.GetString(), out tempEta)) detail.ETA = tempEta;
                                }
                            }

                            dbsp.SCM_Sparepart_PO_Request_Details.Add(detail);
                            row++;
                            if (row > 1000) break;
                        }
                    }
                }
                dbsp.SaveChanges();
            }

            // Handle Multiple Attachments Upload
            if (attachments != null && attachments.Length > 0)
            {
                string folderPath = Server.MapPath("~/Files/SCM/SparepartPORequest/Attachments/" + header.ID);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                foreach (var file in attachments)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(folderPath, fileName);
                        file.SaveAs(filePath);

                        var att = new SCM_Sparepart_PO_Request_Attachment
                        {
                            HeaderID = header.ID,
                            FileName = fileName,
                            FilePath = "/NGKBusi/Files/SCM/SparepartPORequest/Attachments/" + header.ID + "/" + fileName,
                            UploadedDate = DateTime.Now
                        };
                        dbsp.SCM_Sparepart_PO_Request_Attachments.Add(att);
                    }
                }
                dbsp.SaveChanges();
            }

            if (submitType == "Submit")
            {
                var targetDepts = header.TargetDepartment.Split(',');

                // Find Level 2 approver
                var approver = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .FirstOrDefault(c => c.Level == 2 && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));

                if (approver == null)
                {
                    approver = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                        .FirstOrDefault(c => c.Level == 2 && c.IsApprover && c.IsActive);
                }

                if (approver != null)
                {
                    header.Status = "Pending Approval";
                    
                    // Record Creator Log
                    var requesterUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == currUser);
                    dbsp.SCM_Sparepart_PO_Approval_Logs.Add(new SCM_Sparepart_PO_Approval_Log
                    {
                        HeaderID = header.ID,
                        RequestNo = header.RequestNo,
                        Level = 1,
                        TaskTitle = "Creator",
                        NIK = currUser,
                        ApproverName = requesterUser != null ? requesterUser.Name : currUser,
                        Action = "Submitted",
                        Comment = "Pengajuan dibuat dan dikirim ke approval",
                        ActionDate = DateTime.Now
                    });

                    dbsp.SaveChanges();

                    int menuId = db.Menus.Where(m => m.name.Contains("Sparepart")).Select(m => m.id).FirstOrDefault();
                    if (menuId == 0) menuId = 1;

                    // Create task for Level 2 Approver
                    Helpers.ApprovalHelper.SaveSingleApproval(
                        approver.NIK,
                        menuId,
                        "SCM Sparepart PO Request",
                        header.RequestNo,
                        DateTime.Now.AddDays(3),
                        "Approval Request PO: " + header.Title,
                        header.ID,
                        "~/SCM/SparepartPORequest/Detail?id=" + header.ID
                    );
                    SendApprovalRequestEmail(header, approver.NIK, !string.IsNullOrEmpty(approver.TaskTitle) ? approver.TaskTitle : "Level " + approver.Level);

                    // If Level 2 is optional, also create task for Level 3 in parallel
                    if (approver.IsOptional)
                    {
                        var nextApprover = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                            .FirstOrDefault(c => c.Level == 3 && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));

                        if (nextApprover == null)
                        {
                            nextApprover = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                                .FirstOrDefault(c => c.Level == 3 && c.IsApprover && c.IsActive);
                        }

                        if (nextApprover != null)
                        {
                            Helpers.ApprovalHelper.SaveSingleApproval(
                                nextApprover.NIK,
                                menuId,
                                "SCM Sparepart PO Request",
                                header.RequestNo,
                                DateTime.Now.AddDays(3),
                                "Approval Request PO: " + header.Title,
                                header.ID,
                                "~/SCM/SparepartPORequest/Detail?id=" + header.ID
                            );
                            SendApprovalRequestEmail(header, nextApprover.NIK, !string.IsNullOrEmpty(nextApprover.TaskTitle) ? nextApprover.TaskTitle : "Level " + nextApprover.Level);
                        }
                    }
                }
                else
                {
                    // If no approver is configured, save as draft and give error
                    header.Status = "Draft";
                    dbsp.SaveChanges();
                    TempData["Error"] = "No approver is configured for the selected departments (" + header.TargetDepartment + "). Saved as Draft.";
                    return RedirectToAction("Index");
                }
            }

            return RedirectToAction("Index");
        }

        // GET: SCM/SparepartPORequest/DownloadTemplate
        public ActionResult DownloadTemplate()
        {
            string fileName = "Template_Request_PO_Sparepart.xlsx";
            string folderPath = Server.MapPath("~/Files/SCM/SparepartPORequest/Template/");
            string filePath = Path.Combine(folderPath, fileName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // If physical template file exists, download it directly
            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            // Check if any other .xlsx exists in the template folder
            var existingFiles = Directory.GetFiles(folderPath, "*.xlsx");
            if (existingFiles.Length > 0)
            {
                string targetFile = existingFiles[0];
                byte[] fileBytes = System.IO.File.ReadAllBytes(targetFile);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Path.GetFileName(targetFile));
            }

            // If no file uploaded yet, generate standard template using ClosedXML
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Template Request PO");

                // Title Banner
                ws.Cell("A2").Value = "TEMPLATE REQUEST PURCHASE ORDER SPAREPART";
                ws.Cell("A2").Style.Font.Bold = true;
                ws.Cell("A2").Style.Font.FontSize = 14;
                ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#1e3a8a");

                ws.Cell("A3").Value = "Catatan: Data rincian item dimulai dari Baris ke-7 (Row 7). Harap isi sesuai kolom yang tersedia.";
                ws.Cell("A3").Style.Font.Italic = true;
                ws.Cell("A3").Style.Font.FontSize = 9;
                ws.Cell("A3").Style.Font.FontColor = XLColor.FromHtml("#64748b");

                // Headers at Row 6
                string[] headers = new string[]
                {
                    "NO",                       // Col 1 (A)
                    "CODE",                     // Col 2 (B)
                    "PART NAME",                // Col 3 (C)
                    "SPEC",                     // Col 4 (D)
                    "MACHINE NUMBER",           // Col 5 (E)
                    "AX ITEM ID",               // Col 6 (F)
                    "MAKER",                    // Col 7 (G)
                    "QUANTITY",                 // Col 8 (H)
                    "DIMENSI",                  // Col 9 (I)
                    "MACHINE",                  // Col 10 (J)
                    "DRAWING NO",               // Col 11 (K)
                    "DRAWING NO KANRIKOGU",     // Col 12 (L)
                    "LEAD TIME",                // Col 13 (M)
                    "ETA"                       // Col 14 (N)
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(6, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af"); // Indigo / Navy
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#cbd5e1");
                }
                ws.Row(6).Height = 28;

                // Example Row at Row 7
                ws.Cell(7, 1).Value = 1;
                ws.Cell(7, 3).Value = "CAULKING PUNCH";
                ws.Cell(7, 5).Value = "MC-01";
                ws.Cell(7, 6).Value = "AX-SP-00123";
                ws.Cell(7, 7).Value = "NGK SPARK PLUG";
                ws.Cell(7, 8).Value = 2;
                ws.Cell(7, 9).Value = "D12 x 50mm";
                ws.Cell(7, 10).Value = "Assembly Line 1";
                ws.Cell(7, 11).Value = "DWG-2026-001";
                ws.Cell(7, 12).Value = "KNR-9981";
                ws.Cell(7, 13).Value = "30 Days";
                ws.Cell(7, 14).Value = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");

                for (int c = 1; c <= headers.Length; c++)
                {
                    var cell = ws.Cell(7, c);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e2e8f0");
                }

                ws.Columns().AdjustToContents();

                // Save generated template to folder so it is cached
                try
                {
                    workbook.SaveAs(filePath);
                }
                catch { }

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // GET: SCM/SparepartPORequest/ExportFilteredItems
        public ActionResult ExportFilteredItems(string keyword, string status, string targetDept, string requestNo, string poNumber, string category, DateTime? startDate, DateTime? endDate)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            bool isAdmin = User.IsInRole("Administrator") || User.IsInRole("AdminSparepart");

            IQueryable<SCM_Sparepart_PO_Request_Header> headerQuery = dbsp.SCM_Sparepart_PO_Request_Headers;
            if (!isAdmin)
            {
                var myApprovals = db.Task_Approval_Lists
                    .Where(t => t.NIK == currUser && t.Module == "SCM Sparepart PO Request")
                    .Select(t => t.Referal_id)
                    .Distinct()
                    .ToList();

                headerQuery = headerQuery.Where(r => r.RequesterNIK == currUser || myApprovals.Contains(r.RequestNo));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                string cleanStatus = status.Trim();
                if (cleanStatus == "Completed")
                {
                    headerQuery = headerQuery.Where(h => h.Status == "Completed" || h.Status == "Approved");
                }
                else
                {
                    headerQuery = headerQuery.Where(h => h.Status == cleanStatus);
                }
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                string cleanCat = category.Trim();
                if (cleanCat == "Project")
                {
                    headerQuery = headerQuery.Where(h => h.Category == "Project");
                }
                else if (cleanCat == "Regular")
                {
                    headerQuery = headerQuery.Where(h => string.IsNullOrEmpty(h.Category) || h.Category == "Regular");
                }
            }

            if (!string.IsNullOrWhiteSpace(requestNo))
            {
                string cleanReqNo = requestNo.Trim();
                headerQuery = headerQuery.Where(h => h.RequestNo.Contains(cleanReqNo));
            }

            if (!string.IsNullOrWhiteSpace(poNumber))
            {
                string cleanPo = poNumber.Trim();
                headerQuery = headerQuery.Where(h => h.PONumber.Contains(cleanPo));
            }

            if (!string.IsNullOrWhiteSpace(targetDept))
            {
                string cleanDept = targetDept.Trim();
                headerQuery = headerQuery.Where(h => h.TargetDepartment.Contains(cleanDept));
            }

            if (startDate.HasValue)
            {
                var minDate = startDate.Value.Date;
                headerQuery = headerQuery.Where(h => h.CreatedDate >= minDate);
            }

            if (endDate.HasValue)
            {
                var maxDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                headerQuery = headerQuery.Where(h => h.CreatedDate <= maxDate);
            }

            // Query details joining headers
            var headers = headerQuery.Include(h => h.Details).OrderByDescending(h => h.CreatedDate).ToList();

            // Department map & Requester map for display
            var deptMap = db.V_Users_Active
                .Where(u => u.AXCostID != null && u.AXCostID != "" && u.AXCostName != null && u.AXCostName != "")
                .Select(u => new { u.AXCostID, u.AXCostName })
                .Distinct()
                .ToList()
                .GroupBy(u => u.AXCostID)
                .ToDictionary(g => g.Key, g => g.First().AXCostName);

            var requesterNiks = headers.Select(h => h.RequesterNIK).Distinct().ToList();
            var requesterMap = db.V_Users_Active
                .Where(u => requesterNiks.Contains(u.NIK))
                .Select(u => new { u.NIK, u.Name })
                .Distinct()
                .ToList()
                .GroupBy(u => u.NIK)
                .ToDictionary(g => g.Key, g => g.First().Name);

            // Filter items by drawing/partname keyword
            string searchKw = !string.IsNullOrWhiteSpace(keyword) ? keyword.Trim().ToLower() : "";

            var exportRows = new List<dynamic>();
            foreach (var h in headers)
            {
                if (h.Details == null || !h.Details.Any()) continue;

                var matchingDetails = h.Details.AsEnumerable();
                if (!string.IsNullOrEmpty(searchKw))
                {
                    matchingDetails = matchingDetails.Where(d =>
                        (!string.IsNullOrEmpty(d.DrawingNo) && d.DrawingNo.ToLower().Contains(searchKw)) ||
                        (!string.IsNullOrEmpty(d.DrawingNoKanrikogu) && d.DrawingNoKanrikogu.ToLower().Contains(searchKw)) ||
                        (!string.IsNullOrEmpty(d.PartName) && d.PartName.ToLower().Contains(searchKw)) ||
                        (!string.IsNullOrEmpty(d.AxItemID) && d.AxItemID.ToLower().Contains(searchKw)) ||
                        (!string.IsNullOrEmpty(d.Maker) && d.Maker.ToLower().Contains(searchKw))
                    );
                }

                // Target Dept Name
                string targetDeptNames = "-";
                if (!string.IsNullOrEmpty(h.TargetDepartment))
                {
                    var deptIds = h.TargetDepartment.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var names = deptIds.Select(id => deptMap.ContainsKey(id.Trim()) ? deptMap[id.Trim()] : id.Trim());
                    targetDeptNames = string.Join(", ", names);
                }

                string requesterName = requesterMap.ContainsKey(h.RequesterNIK) ? requesterMap[h.RequesterNIK] : h.RequesterNIK;

                foreach (var d in matchingDetails.OrderBy(d => d.No))
                {
                    exportRows.Add(new
                    {
                        RequestNo = h.RequestNo,
                        CreatedDate = h.CreatedDate,
                        Category = !string.IsNullOrEmpty(h.Category) ? h.Category : "Regular",
                        Title = h.Title,
                        Requester = $"{requesterName} ({h.RequesterNIK})",
                        TargetDepartment = targetDeptNames,
                        Status = (h.Status == "Approved" ? "Completed" : h.Status),
                        PONumber = h.PONumber ?? "-",
                        DetailNo = d.No,
                        PartName = d.PartName ?? "-",
                        DrawingNo = d.DrawingNo ?? "-",
                        DrawingNoKanrikogu = d.DrawingNoKanrikogu ?? "-",
                        AxItemID = d.AxItemID ?? "-",
                        MachineNumber = d.MachineNumber ?? "-",
                        Maker = d.Maker ?? "-",
                        Dimensi = d.Dimensi ?? "-",
                        Machine = d.Machine ?? "-",
                        Quantity = d.Quantity.HasValue ? d.Quantity.Value : 0,
                        LeadTime = d.LeadTime ?? "-",
                        ETA = d.ETA
                    });
                }
            }

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Export Sparepart Items");

                // Table Headers Definition
                string[] colHeaders = new string[]
                {
                    "NO",                       // 1
                    "REQUEST NO",               // 2
                    "TANGGAL REQUEST",          // 3
                    "KATEGORI",                 // 4
                    "JUDUL PENGAJUAN",          // 5
                    "PEMOHON (REQUESTER)",      // 6
                    "TARGET DEPARTEMEN",        // 7
                    "STATUS",                   // 8
                    "NO PO",                    // 9
                    "ITEM NO",                  // 10
                    "PART NAME",                // 11
                    "DRAWING NO",               // 12
                    "DRAWING NO KANRIKOGU",     // 13
                    "AX ITEM ID",               // 14
                    "MACHINE NUMBER",           // 15
                    "MAKER",                    // 16
                    "DIMENSI",                  // 17
                    "MACHINE",                  // 18
                    "QTY",                      // 19
                    "LEAD TIME",                // 20
                    "ETA"                       // 21
                };

                // Title Block (Merged across all columns)
                var titleRange = ws.Range(2, 1, 2, colHeaders.Length);
                titleRange.Merge();
                ws.Cell("A2").Value = "EXPORT RINCIAN ITEM & DRAWING SPAREPART PO REQUEST";
                ws.Cell("A2").Style.Font.Bold = true;
                ws.Cell("A2").Style.Font.FontSize = 14;
                ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#1e3a8a");
                ws.Cell("A2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                string dateRangeText = "";
                if (startDate.HasValue && endDate.HasValue)
                {
                    dateRangeText = $" | Periode: {startDate.Value:dd/MM/yyyy} s/d {endDate.Value:dd/MM/yyyy}";
                }
                else if (startDate.HasValue)
                {
                    dateRangeText = $" | Dari Tanggal: {startDate.Value:dd/MM/yyyy}";
                }
                else if (endDate.HasValue)
                {
                    dateRangeText = $" | Sampai Tanggal: {endDate.Value:dd/MM/yyyy}";
                }

                string catInfoText = !string.IsNullOrEmpty(category) ? $" | Kategori: {category.Trim()}" : "";

                string filterInfo = (string.IsNullOrEmpty(searchKw) ? "Semua Item" : $"Filter Drawing / Part Name: '{keyword.Trim()}'")
                    + catInfoText
                    + dateRangeText
                    + $" | Diekspor pada: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Total: {exportRows.Count} Item";
                var subtitleRange = ws.Range(3, 1, 3, colHeaders.Length);
                subtitleRange.Merge();
                ws.Cell("A3").Value = filterInfo;
                ws.Cell("A3").Style.Font.Italic = true;
                ws.Cell("A3").Style.Font.FontSize = 9;
                ws.Cell("A3").Style.Font.FontColor = XLColor.FromHtml("#64748b");
                ws.Cell("A3").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Table Headers at Row 5

                for (int i = 0; i < colHeaders.Length; i++)
                {
                    var cell = ws.Cell(5, i + 1);
                    cell.Value = colHeaders[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af"); // Navy
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#cbd5e1");
                }
                ws.Row(5).Height = 26;

                int row = 6;
                int seqNo = 1;
                foreach (var item in exportRows)
                {
                    ws.Cell(row, 1).Value = seqNo++;
                    ws.Cell(row, 2).Value = item.RequestNo;
                    ws.Cell(row, 3).Value = ((DateTime)item.CreatedDate).ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 4).Value = item.Category;
                    ws.Cell(row, 5).Value = item.Title;
                    ws.Cell(row, 6).Value = item.Requester;
                    ws.Cell(row, 7).Value = item.TargetDepartment;
                    ws.Cell(row, 8).Value = item.Status;
                    ws.Cell(row, 9).Value = item.PONumber;
                    ws.Cell(row, 10).Value = item.DetailNo != null ? item.DetailNo.ToString() : "-";
                    ws.Cell(row, 11).Value = item.PartName;
                    ws.Cell(row, 12).Value = item.DrawingNo;
                    ws.Cell(row, 13).Value = item.DrawingNoKanrikogu;
                    ws.Cell(row, 14).Value = item.AxItemID;
                    ws.Cell(row, 15).Value = item.MachineNumber;
                    ws.Cell(row, 16).Value = item.Maker;
                    ws.Cell(row, 17).Value = item.Dimensi;
                    ws.Cell(row, 18).Value = item.Machine;
                    ws.Cell(row, 19).Value = item.Quantity;
                    ws.Cell(row, 20).Value = item.LeadTime;
                    ws.Cell(row, 21).Value = item.ETA != null ? ((DateTime)item.ETA).ToString("dd/MM/yyyy") : "-";

                    // Row Styling
                    for (int c = 1; c <= colHeaders.Length; c++)
                    {
                        var cell = ws.Cell(row, c);
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#e2e8f0");
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        if (c == 1 || c == 2 || c == 3 || c == 4 || c == 8 || c == 9 || c == 10 || c == 12 || c == 13 || c == 14 || c == 15 || c == 21)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                        else if (c == 19)
                        {
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                    }

                    if (row % 2 == 1)
                    {
                        for (int c = 1; c <= colHeaders.Length; c++)
                        {
                            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                        }
                    }

                    row++;
                }

                ws.Columns().AdjustToContents();

                string outFileName = $"Export_Sparepart_Items_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", outFileName);
                }
            }
        }

        // POST: SCM/SparepartPORequest/PreviewExcel
        [HttpPost]
        public JsonResult PreviewExcel(HttpPostedFileBase excelFile)
        {
            try
            {
                if (excelFile == null || excelFile.ContentLength == 0)
                {
                    return Json(new { success = false, message = "No file selected." });
                }

                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var extension = Path.GetExtension(excelFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Only Excel files (.xlsx, .xls) are allowed." });
                }

                var items = new List<object>();

                using (var workbook = new XLWorkbook(excelFile.InputStream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        return Json(new { success = false, message = "Worksheet is empty." });
                    }

                    int row = 7;
                    while (!worksheet.Cell(row, 1).IsEmpty() || !worksheet.Cell(row, 2).IsEmpty() || !worksheet.Cell(row, 6).IsEmpty())
                    {
                        var noStr = worksheet.Cell(row, 1).GetString().Trim();
                        var partName = worksheet.Cell(row, 3).GetString().Trim();
                        var machineNumber = worksheet.Cell(row, 5).GetString().Trim();
                        var axItemID = worksheet.Cell(row, 6).GetString().Trim();
                        var maker = worksheet.Cell(row, 7).GetString().Trim();

                        decimal qty = 0;
                        var qtyCell = worksheet.Cell(row, 8);
                        if (!qtyCell.IsEmpty())
                        {
                            decimal.TryParse(qtyCell.GetString(), out qty);
                        }

                        var dimensi = worksheet.Cell(row, 9).GetString().Trim();
                        var machine = worksheet.Cell(row, 10).GetString().Trim();
                        var drawingNo = worksheet.Cell(row, 11).GetString().Trim();
                        var drawingNoKanri = worksheet.Cell(row, 12).GetString().Trim();
                        var leadTime = worksheet.Cell(row, 13).GetString().Trim();

                        string etaStr = "";
                        var etaCell = worksheet.Cell(row, 14);
                        if (!etaCell.IsEmpty())
                        {
                            if (etaCell.DataType == XLDataType.DateTime)
                            {
                                etaStr = etaCell.GetDateTime().ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                DateTime tempDate;
                                if (DateTime.TryParse(etaCell.GetString(), out tempDate))
                                {
                                    etaStr = tempDate.ToString("yyyy-MM-dd");
                                }
                                else
                                {
                                    etaStr = etaCell.GetString();
                                }
                            }
                        }

                        items.Add(new
                        {
                            No = noStr,
                            PartName = partName,
                            MachineNumber = machineNumber,
                            AxItemID = axItemID,
                            Maker = maker,
                            Quantity = qty,
                            Dimensi = dimensi,
                            Machine = machine,
                            DrawingNo = drawingNo,
                            DrawingNoKanrikogu = drawingNoKanri,
                            LeadTime = leadTime,
                            ETA = etaStr
                        });

                        row++;
                        if (row > 1000) break;
                    }
                }

                return Json(new { success = true, items = items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: SCM/SparepartPORequest/Detail/5
        public ActionResult Detail(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var header = dbsp.SCM_Sparepart_PO_Request_Headers
                .Include(h => h.Details)
                .Include(h => h.Attachments)
                .FirstOrDefault(h => h.ID == id);

            if (header == null)
            {
                return HttpNotFound();
            }

            var requester = db.V_Users_Active.FirstOrDefault(u => u.NIK == header.RequesterNIK);
            ViewBag.RequesterName = requester != null ? requester.Name : header.RequesterNIK;

            // Translate TargetDepartment IDs to names for display
            if (!string.IsNullOrEmpty(header.TargetDepartment))
            {
                var deptIds = header.TargetDepartment.Split(',');
                var deptNames = db.V_Users_Active
                    .Where(u => deptIds.Contains(u.AXCostID) && u.AXCostName != null)
                    .Select(u => u.AXCostName)
                    .Distinct()
                    .ToList();
                ViewBag.TargetDepartmentNames = deptNames.Any() ? string.Join(", ", deptNames) : header.TargetDepartment;
            }
            else
            {
                ViewBag.TargetDepartmentNames = "-";
            }

            // Get all approval logs
            var logs = dbsp.SCM_Sparepart_PO_Approval_Logs
                .Where(l => l.HeaderID == header.ID)
                .OrderBy(l => l.ActionDate)
                .ToList();
            ViewBag.ApprovalLogs = logs;

            // Check if current user is active approver
            var activeTask = db.Task_Approval_Lists
                .FirstOrDefault(t => t.Referal_id == header.RequestNo && t.NIK == currUser && !t.IsCompleteTask && t.Module == "SCM Sparepart PO Request");

            ViewBag.IsApproverActive = activeTask != null;
            ViewBag.TaskId = activeTask != null ? activeTask.ID : 0;

            // Get configured approvers sequentially matching the routing logic
            var targetDepts = !string.IsNullOrEmpty(header.TargetDepartment) ? header.TargetDepartment.Split(',') : new string[0];

            // Check if current active approver is Level 5
            var currentApproverConfig = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                .FirstOrDefault(c => c.NIK == currUser && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));
            if (currentApproverConfig == null)
            {
                currentApproverConfig = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .FirstOrDefault(c => c.NIK == currUser && c.IsApprover && c.IsActive);
            }
            ViewBag.IsLevel5Approver = currentApproverConfig != null && currentApproverConfig.Level == 5;

            var configuredApprovers = new List<SCM_Sparepart_PO_Request_Approval_Config>();
            for (int lvl = 2; lvl <= 10; lvl++)
            {
                var app = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .FirstOrDefault(c => c.Level == lvl && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));
                if (app == null)
                {
                    app = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                        .FirstOrDefault(c => c.Level == lvl && c.IsApprover && c.IsActive);
                }
                if (app != null)
                {
                    configuredApprovers.Add(app);
                }
            }

            // Get all actual tasks created for this request
            var actualTasks = db.Task_Approval_Lists
                .Where(t => t.Referal_id == header.RequestNo && t.Module == "SCM Sparepart PO Request")
                .ToList();

            // Build full approval flow list for UI display
            var approvalFlow = new List<NGKBusi.Areas.SCM.Models.SCM_Sparepart_PO_Approval_Flow_Item>();
            foreach (var config in configuredApprovers)
            {
                var appUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == config.NIK);
                string approverName = appUser != null ? appUser.Name : config.NIK;

                // Match with logs first
                var matchedLog = logs.LastOrDefault(l => l.NIK == config.NIK && l.Level == config.Level);

                // Match with actual task list
                var matchedTask = actualTasks.FirstOrDefault(t => t.NIK == config.NIK);

                string status = "Not Reached"; // Approved, Skipped, Pending, Not Reached, Rejected
                DateTime? actionDate = null;
                string comment = null;

                int maxApprovedLvl = logs.Where(l => l.Action == "Approved").Select(l => l.Level).DefaultIfEmpty(0).Max();

                if (matchedLog != null)
                {
                    status = matchedLog.Action;
                    actionDate = matchedLog.ActionDate;
                    comment = matchedLog.Comment;
                }
                else if (matchedTask != null)
                {
                    if (matchedTask.IsCompleteTask)
                    {
                        if (matchedTask.TaskTitle != null && matchedTask.TaskTitle.StartsWith("[SKIPPED]"))
                        {
                            status = "Skipped";
                        }
                        else
                        {
                            status = "Approved";
                        }
                        actionDate = matchedTask.ApprovalDate;
                    }
                    else if (config.Level <= maxApprovedLvl || header.Status == "Approved" || header.Status == "Completed")
                    {
                        status = "Skipped";
                    }
                    else
                    {
                        status = "Pending";
                    }
                }
                else if (config.Level <= maxApprovedLvl || header.Status == "Approved" || header.Status == "Completed")
                {
                    status = "Skipped";
                }

                approvalFlow.Add(new NGKBusi.Areas.SCM.Models.SCM_Sparepart_PO_Approval_Flow_Item
                {
                    Level = config.Level,
                    ApproverName = approverName,
                    NIK = config.NIK,
                    Status = status,
                    Date = actionDate,
                    IsOptional = config.IsOptional,
                    TaskTitle = config.TaskTitle,
                    Comment = comment,
                    Action = status
                });
            }

            ViewBag.ApprovalFlow = approvalFlow;
            ViewBag.NavHide = true;

            return View(header);
        }

        // POST: SCM/SparepartPORequest/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int ID, string comment, string poNumber)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var header = dbsp.SCM_Sparepart_PO_Request_Headers.Find(ID);

            if (header == null) return HttpNotFound();

            var task = db.Task_Approval_Lists
                .FirstOrDefault(t => t.Referal_id == header.RequestNo && t.NIK == currUser && !t.IsCompleteTask && t.Module == "SCM Sparepart PO Request");

            if (task == null)
            {
                TempData["Error"] = "You are not authorized to approve this request or the task has already been processed.";
                return RedirectToAction("Detail", new { id = ID });
            }

            // Find current approval config
            var targetDepts = header.TargetDepartment.Split(',');
            var config = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                .FirstOrDefault(c => c.NIK == currUser && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));
            if (config == null)
            {
                config = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .FirstOrDefault(c => c.NIK == currUser && c.IsApprover && c.IsActive);
            }
            int currentLevel = config != null ? config.Level : 1;

            // Validation for Level 5: PO Number is mandatory and unique
            if (currentLevel == 5)
            {
                if (string.IsNullOrWhiteSpace(poNumber))
                {
                    TempData["Error"] = "PO Number wajib diisi untuk persetujuan Level 5.";
                    return RedirectToAction("Detail", new { id = ID });
                }
                string cleanPo = poNumber.Trim();
                bool poExists = dbsp.SCM_Sparepart_PO_Request_Headers
                    .Any(h => h.PONumber == cleanPo && h.ID != header.ID);
                if (poExists)
                {
                    TempData["Error"] = "No PO '" + cleanPo + "' sudah digunakan pada pengajuan lain. Silakan gunakan No PO yang berbeda.";
                    return RedirectToAction("Detail", new { id = ID });
                }
                header.PONumber = cleanPo;
            }

            task.IsCompleteTask = true;
            task.ApprovalDate = DateTime.Now;

            // If current approver has StartsReminder flag, initialize ReminderDateStart (H + X Days)
            if (config != null && config.StartsReminder)
            {
                header.ReminderDateStart = DateTime.Now.Date.AddDays(header.ReminderDaysAfter);
            }

            // Record Approval Log
            var approverUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == currUser);
            string finalComment = !string.IsNullOrWhiteSpace(poNumber)
                ? (!string.IsNullOrWhiteSpace(comment) ? $"PO Number: {poNumber.Trim()} | {comment.Trim()}" : $"PO Number: {poNumber.Trim()}")
                : (!string.IsNullOrWhiteSpace(comment) ? comment.Trim() : "Disetujui");

            dbsp.SCM_Sparepart_PO_Approval_Logs.Add(new SCM_Sparepart_PO_Approval_Log
            {
                HeaderID = header.ID,
                RequestNo = header.RequestNo,
                Level = currentLevel,
                TaskTitle = config != null && !string.IsNullOrEmpty(config.TaskTitle) ? config.TaskTitle : "Level " + currentLevel,
                NIK = currUser,
                ApproverName = approverUser != null ? approverUser.Name : currUser,
                Action = "Approved",
                Comment = finalComment,
                ActionDate = DateTime.Now
            });

            // Trigger info notification to target departments if configured
            if (config != null && config.TriggersInfoNotification)
            {
                var infoUsers = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .Where(c => targetDepts.Contains(c.Department) && !c.IsApprover && c.IsActive)
                    .ToList();

                var emails = new ArrayList();
                foreach (var u in infoUsers)
                {
                    var usr = db.V_Users_Active.FirstOrDefault(x => x.NIK == u.NIK);
                    if (usr != null && !string.IsNullOrEmpty(usr.Email))
                    {
                        emails.Add(usr.Email);
                    }
                }

                if (emails.Count > 0)
                {
                    SendInfoEmail(header, emails);
                }
            }

            // Auto-complete/bypass lower or equal level active tasks (since this required/higher level is approved)
            var otherActiveTasks = db.Task_Approval_Lists
                .Where(t => t.Referal_id == header.RequestNo && !t.IsCompleteTask && t.Module == "SCM Sparepart PO Request" && t.ID != task.ID)
                .ToList();

            foreach (var activeT in otherActiveTasks)
            {
                var activeConfig = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .FirstOrDefault(c => c.NIK == activeT.NIK && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));
                if (activeConfig == null)
                {
                    activeConfig = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                        .FirstOrDefault(c => c.NIK == activeT.NIK && c.IsApprover && c.IsActive);
                }

                if (activeConfig != null && activeConfig.Level <= currentLevel)
                {
                    activeT.IsCompleteTask = true;
                    activeT.ApprovalDate = DateTime.Now;
                    if (activeT.TaskTitle != null && !activeT.TaskTitle.StartsWith("[SKIPPED]"))
                    {
                        string newTitle = "[SKIPPED] " + activeT.TaskTitle.Trim();
                        if (newTitle.Length > 250)
                        {
                            newTitle = newTitle.Substring(0, 250);
                        }
                        activeT.TaskTitle = newTitle;
                    }

                    var skippedUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == activeT.NIK);
                    dbsp.SCM_Sparepart_PO_Approval_Logs.Add(new SCM_Sparepart_PO_Approval_Log
                    {
                        HeaderID = header.ID,
                        RequestNo = header.RequestNo,
                        Level = activeConfig.Level,
                        TaskTitle = !string.IsNullOrEmpty(activeConfig.TaskTitle) ? activeConfig.TaskTitle : "Level " + activeConfig.Level,
                        NIK = activeT.NIK,
                        ApproverName = skippedUser != null ? skippedUser.Name : activeT.NIK,
                        Action = "Skipped",
                        Comment = "Dilewati karena approval di atas disetujui",
                        ActionDate = DateTime.Now
                    });
                }
            }

            // Search next level
            var nextApprover = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                .FirstOrDefault(c => c.Level == currentLevel + 1 && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));

            if (nextApprover == null)
            {
                nextApprover = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .FirstOrDefault(c => c.Level == currentLevel + 1 && c.IsApprover && c.IsActive);
            }

            if (nextApprover != null)
            {
                int menuId = db.Menus.Where(m => m.name.Contains("Sparepart")).Select(m => m.id).FirstOrDefault();
                if (menuId == 0) menuId = 1;

                // Save task for Level N + 1
                Helpers.ApprovalHelper.SaveSingleApproval(
                    nextApprover.NIK,
                    menuId,
                    "SCM Sparepart PO Request",
                    header.RequestNo,
                    DateTime.Now.AddDays(3),
                    "Approval Request PO: " + header.Title,
                    header.ID,
                    "~/SCM/SparepartPORequest/Detail?id=" + header.ID
                );
                SendApprovalRequestEmail(header, nextApprover.NIK, !string.IsNullOrEmpty(nextApprover.TaskTitle) ? nextApprover.TaskTitle : "Level " + nextApprover.Level);

                // If Level N + 1 is optional, also create task for Level N + 2 in parallel
                if (nextApprover.IsOptional)
                {
                    var nextNextApprover = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                        .FirstOrDefault(c => c.Level == currentLevel + 2 && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));
                    if (nextNextApprover == null)
                    {
                        nextNextApprover = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                            .FirstOrDefault(c => c.Level == currentLevel + 2 && c.IsApprover && c.IsActive);
                    }

                    if (nextNextApprover != null)
                    {
                        Helpers.ApprovalHelper.SaveSingleApproval(
                            nextNextApprover.NIK,
                            menuId,
                            "SCM Sparepart PO Request",
                            header.RequestNo,
                            DateTime.Now.AddDays(3),
                            "Approval Request PO: " + header.Title,
                            header.ID,
                            "~/SCM/SparepartPORequest/Detail?id=" + header.ID
                        );
                        SendApprovalRequestEmail(header, nextNextApprover.NIK, !string.IsNullOrEmpty(nextNextApprover.TaskTitle) ? nextNextApprover.TaskTitle : "Level " + nextNextApprover.Level);
                    }
                }
            }
            else
            {
                // Final level approved
                header.Status = "Completed";
                header.LastApprovalDate = DateTime.Now;
                header.ReminderDateStart = null; // Clear scheduled reminder

                // Complete all remaining tasks for this request in Task_Approval_Lists
                var remainingTasks = db.Task_Approval_Lists
                    .Where(t => t.Referal_id == header.RequestNo && !t.IsCompleteTask && t.Module == "SCM Sparepart PO Request")
                    .ToList();
                foreach (var remT in remainingTasks)
                {
                    remT.IsCompleteTask = true;
                    remT.ApprovalDate = DateTime.Now;
                    if (remT.TaskTitle != null && !remT.TaskTitle.StartsWith("[SKIPPED]"))
                    {
                        string newTitle = "[SKIPPED] " + remT.TaskTitle.Trim();
                        if (newTitle.Length > 250)
                        {
                            newTitle = newTitle.Substring(0, 250);
                        }
                        remT.TaskTitle = newTitle;
                    }
                }
            }

            try
            {
                db.SaveChanges();
                dbsp.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var errorMsgs = new List<string>();
                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        errorMsgs.Add($"{ve.PropertyName}: {ve.ErrorMessage}");
                    }
                }
                TempData["Error"] = "Validation Error: " + string.Join("; ", errorMsgs);
                return RedirectToAction("Detail", new { id = ID });
            }

            TempData["Success"] = "Request approved successfully.";
            return RedirectToAction("Detail", new { id = ID });
        }

        // GET: SCM/SparepartPORequest/CheckPONumberUnique
        [HttpGet]
        public JsonResult CheckPONumberUnique(string poNumber, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(poNumber))
            {
                return Json(new { isUnique = true }, JsonRequestBehavior.AllowGet);
            }
            string cleanPo = poNumber.Trim();
            bool exists = dbsp.SCM_Sparepart_PO_Request_Headers
                .Any(h => h.PONumber == cleanPo && (!excludeId.HasValue || h.ID != excludeId.Value));
            return Json(new { isUnique = !exists }, JsonRequestBehavior.AllowGet);
        }

        // POST: SCM/SparepartPORequest/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int ID, string comment)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var header = dbsp.SCM_Sparepart_PO_Request_Headers.Find(ID);

            if (header == null) return HttpNotFound();

            if (string.IsNullOrEmpty(comment))
            {
                TempData["Error"] = "Comments / Reject Reason is required.";
                return RedirectToAction("Detail", new { id = ID });
            }

            var task = db.Task_Approval_Lists
                .FirstOrDefault(t => t.Referal_id == header.RequestNo && t.NIK == currUser && !t.IsCompleteTask && t.Module == "SCM Sparepart PO Request");

            if (task == null)
            {
                TempData["Error"] = "You are not authorized to reject this request or the task has already been processed.";
                return RedirectToAction("Detail", new { id = ID });
            }

            int menuId = db.Menus.Where(m => m.name.Contains("Sparepart")).Select(m => m.id).FirstOrDefault();
            if (menuId == 0) menuId = 1;

            Helpers.ApprovalHelper.CancelApprovalTasks(menuId, header.RequestNo);

            header.Status = "Rejected";
            header.RejectReason = comment;
            header.ReminderDateStart = null; // Clear scheduled reminder

            // Record Reject Log
            var rejectUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == currUser);
            var targetDepts = header.TargetDepartment.Split(',');
            var config = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                .FirstOrDefault(c => c.NIK == currUser && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));
            if (config == null)
            {
                config = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                    .FirstOrDefault(c => c.NIK == currUser && c.IsApprover && c.IsActive);
            }
            int currentLevel = config != null ? config.Level : 2;

            dbsp.SCM_Sparepart_PO_Approval_Logs.Add(new SCM_Sparepart_PO_Approval_Log
            {
                HeaderID = header.ID,
                RequestNo = header.RequestNo,
                Level = currentLevel,
                TaskTitle = config != null && !string.IsNullOrEmpty(config.TaskTitle) ? config.TaskTitle : "Level " + currentLevel,
                NIK = currUser,
                ApproverName = rejectUser != null ? rejectUser.Name : currUser,
                Action = "Rejected",
                Comment = comment,
                ActionDate = DateTime.Now
            });

            dbsp.SaveChanges();

            TempData["Success"] = "Request rejected successfully.";
            return RedirectToAction("Detail", new { id = ID });
        }

        // GET: SCM/SparepartPORequest/SendReminders
        [AllowAnonymous]
        public ActionResult SendReminders()
        {
            int sentCount = 0;
            try
            {
                DateTime now = DateTime.Now;
                DateTime today = now.Date;

                // 1. Fetch headers that are Pending Approval and due for reminder today or earlier (exclude if already has PO Number)
                var dueHeaders = dbsp.SCM_Sparepart_PO_Request_Headers
                    .Where(h => h.Status == "Pending Approval" && (h.PONumber == null || h.PONumber == "") && h.ReminderDateStart != null && DbFunctions.TruncateTime(h.ReminderDateStart) <= today)
                    .ToList();

                foreach (var header in dueHeaders)
                {
                    if (!string.IsNullOrEmpty(header.PONumber))
                    {
                        continue;
                    }

                    // Check send hour
                    int targetSendHour = header.ReminderSendTime > 0 ? header.ReminderSendTime : 8;
                    if (now.Hour < targetSendHour)
                    {
                        continue;
                    }

                    // If already sent today, skip to prevent duplicate sending in the same day
                    if (header.LastReminderSentDate.HasValue && header.LastReminderSentDate.Value.Date == today)
                    {
                        continue;
                    }

                    // Check if there is still any active pending task for this request
                    var activeTasks = db.Task_Approval_Lists
                        .Where(t => t.Referal_id == header.RequestNo && !t.IsCompleteTask && t.Module == "SCM Sparepart PO Request")
                        .ToList();

                    if (!activeTasks.Any())
                    {
                        // No active tasks remain, clear ReminderDateStart
                        header.ReminderDateStart = null;
                        continue;
                    }

                    // Get target departments
                    var targetDepts = !string.IsNullOrEmpty(header.TargetDepartment) ? header.TargetDepartment.Split(',') : new string[0];

                    // Find who this request is currently waiting approval from (distinct approver list)
                    var pendingApproverNames = new List<string>();
                    var distinctActiveNiks = activeTasks.Select(t => t.NIK).Distinct().ToList();

                    foreach (var nik in distinctActiveNiks)
                    {
                        var u = db.V_Users_Active.FirstOrDefault(x => x.NIK == nik);
                        string name = u != null ? u.Name : nik;

                        var cfg = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                            .FirstOrDefault(c => c.NIK == nik && c.IsApprover && c.IsActive && targetDepts.Contains(c.Department));
                        if (cfg == null)
                        {
                            cfg = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                                .FirstOrDefault(c => c.NIK == nik && c.IsApprover && c.IsActive);
                        }

                        string roleName = cfg != null && !string.IsNullOrEmpty(cfg.TaskTitle)
                            ? cfg.TaskTitle
                            : (cfg != null ? "Level " + cfg.Level : "Approver");

                        string entry = $"{name} ({roleName})";
                        if (!pendingApproverNames.Contains(entry))
                        {
                            pendingApproverNames.Add(entry);
                        }
                    }

                    string waitingApprovalInfo = pendingApproverNames.Any()
                        ? string.Join(", ", pendingApproverNames)
                        : "-";

                    var requesterUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == header.RequesterNIK);
                    string requesterName = requesterUser != null ? requesterUser.Name : header.RequesterNIK;

                    // Collect recipients: Requestor + ALL Approvers (both configured & task-assigned across all levels)
                    var recipientNiks = new List<string>();
                    if (!string.IsNullOrEmpty(header.RequesterNIK))
                    {
                        recipientNiks.Add(header.RequesterNIK);
                    }

                    // Get all configured approvers for target department
                    var configuredApproverNiks = dbsp.SCM_Sparepart_PO_Request_Approval_Configs
                        .Where(c => c.IsApprover && c.IsActive && (targetDepts.Contains(c.Department) || string.IsNullOrEmpty(c.Department)))
                        .Select(c => c.NIK)
                        .Distinct()
                        .ToList();

                    foreach (var nik in configuredApproverNiks)
                    {
                        if (!recipientNiks.Contains(nik))
                        {
                            recipientNiks.Add(nik);
                        }
                    }

                    // Also get any approvers from all created tasks for this request (completed & pending)
                    var allTaskNiks = db.Task_Approval_Lists
                        .Where(t => t.Referal_id == header.RequestNo && t.Module == "SCM Sparepart PO Request")
                        .Select(t => t.NIK)
                        .Distinct()
                        .ToList();

                    foreach (var nik in allTaskNiks)
                    {
                        if (!recipientNiks.Contains(nik))
                        {
                            recipientNiks.Add(nik);
                        }
                    }

                    bool emailSent = false;
                    foreach (var nik in recipientNiks)
                    {
                        var targetUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == nik);
                        if (targetUser != null && !string.IsNullOrEmpty(targetUser.Email))
                        {
                            string taskTitle = "Approval Request PO: " + header.Title;
                            SendReminderEmail(header, taskTitle, targetUser.Email, targetUser.Name, waitingApprovalInfo, requesterName);
                            emailSent = true;
                            sentCount++;
                        }
                    }

                    if (emailSent)
                    {
                        header.LastReminderSentDate = now;

                        // Update ReminderDateStart for next cycle or clear if not repeated
                        if (header.IsReminderRepeated)
                        {
                            int intervalDays = 1;
                            if (header.ReminderRepeatInterval == "Weekly") intervalDays = 7;
                            else if (header.ReminderRepeatInterval == "Custom" && header.ReminderCustomDays.HasValue) intervalDays = header.ReminderCustomDays.Value;

                            header.ReminderDateStart = today.AddDays(intervalDays);
                        }
                        else
                        {
                            header.ReminderDateStart = null; // One-time reminder finished
                        }
                    }
                }
                dbsp.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, emailsSent = sentCount }, JsonRequestBehavior.AllowGet);
        }

        private void SendApprovalRequestEmail(SCM_Sparepart_PO_Request_Header header, string approverNik, string roleName)
        {
            try
            {
                var approver = db.V_Users_Active.FirstOrDefault(u => u.NIK == approverNik);
                if (approver == null || string.IsNullOrEmpty(approver.Email)) return;

                var requesterUser = db.V_Users_Active.FirstOrDefault(u => u.NIK == header.RequesterNIK);
                string requesterName = requesterUser != null ? requesterUser.Name : header.RequesterNIK;

                // Resolve Target Department Names
                string targetDepartmentNames = "-";
                if (!string.IsNullOrEmpty(header.TargetDepartment))
                {
                    var deptIds = header.TargetDepartment.Split(',');
                    var deptNames = db.V_Users_Active
                        .Where(u => deptIds.Contains(u.AXCostID) && u.AXCostName != null && u.AXCostName != "")
                        .Select(u => u.AXCostName)
                        .Distinct()
                        .ToList();
                    targetDepartmentNames = deptNames.Any() ? string.Join(", ", deptNames) : header.TargetDepartment;
                }

                string subject = $"[APPROVAL REQUEST] {header.RequestNo} - {header.Title}";
                string detailUrl = "https://portal.ngkbusi.com/NGKBusi/SCM/SparepartPORequest/Detail?id=" + header.ID;
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 650px; margin: 0 auto; color: #334155; line-height: 1.6;'>
                        <div style='background: linear-gradient(135deg, #1e40af, #3b82f6); padding: 22px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <h2 style='color: #ffffff; margin: 0; font-size: 20px; font-weight: 700; letter-spacing: -0.5px;'>SCM Sparepart PO Request System</h2>
                            <p style='color: #dbeafe; margin: 4px 0 0 0; font-size: 12px;'>Permintaan Persetujuan (Approval Request)</p>
                        </div>
                        <div style='padding: 24px; background: #ffffff; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 12px 12px;'>
                            <p style='margin-top: 0;'>Yth. Bapak/Ibu <strong>{approver.Name}</strong> ({roleName}),</p>
                            <p>Terdapat permohonan pengadaan Sparepart PO yang saat ini memerlukan persetujuan dari Anda:</p>
                            
                            <table style='width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 13px;'>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; width: 160px; color: #64748b;'>No. Request</td>
                                    <td style='padding: 8px 4px; font-weight: 600; color: #1e293b;'>{header.RequestNo}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Kategori Pengajuan</td>
                                    <td style='padding: 8px 4px;'>
                                        {(header.Category == "Project" 
                                            ? "<span style='background: #e0e7ff; color: #4338ca; padding: 3px 10px; border-radius: 6px; font-weight: bold; font-size: 12px; display: inline-block;'>🚀 Project (Khusus / Improvement)</span>" 
                                            : "<span style='background: #dbeafe; color: #1d4ed8; padding: 3px 10px; border-radius: 6px; font-weight: bold; font-size: 12px; display: inline-block;'>📦 Regular (Consumables / Operasional)</span>")}
                                    </td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Judul Permintaan</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{header.Title}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Pemohon (Requester)</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{requesterName} ({header.RequesterNIK})</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Tanggal Pengajuan</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{header.CreatedDate:dd MMM yyyy HH:mm} WIB</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Target Departemen</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{targetDepartmentNames}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Tahap / Peran Anda</td>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #2563eb;'>{roleName}</td>
                                </tr>
                            </table>

                            <div style='margin-top: 28px; text-align: center;'>
                                <a href='{detailUrl}' style='display: inline-block; background: #2563eb; color: #ffffff; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: bold; font-size: 13px; box-shadow: 0 2px 4px rgba(37,99,235,0.2);'>Buka & Proses Persetujuan</a>
                            </div>

                            <p style='margin-top: 28px; font-size: 12px; color: #94a3b8; border-top: 1px solid #f1f5f9; padding-top: 16px;'>
                                Email ini dikirim secara otomatis oleh NGK Portal System. Mohon untuk tidak membalas email ini secara langsung.
                            </p>
                        </div>
                    </div>";

                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Sparepart PO Notification");
                var password = "100%NGKbusi!";

                using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                {
                    smtp.EnableSsl = false;
                    smtp.Credentials = new NetworkCredential(senderEmail.Address, password);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var mess = new MailMessage(senderEmail, new MailAddress(approver.Email))
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        smtp.Send(mess);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[APPROVAL NOTIF EMAIL ERROR] " + ex.Message);
            }
        }

        private void SendInfoEmail(SCM_Sparepart_PO_Request_Header header, ArrayList emailSendTo)
        {
            try
            {
                string subject = "[INFO] Approval Progress PO: " + header.Title;
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #334155;'>
                        <div style='background: #2563eb; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                            <h2>Informasi Pengajuan Sparepart PO</h2>
                        </div>
                        <div style='padding: 20px; background: #ffffff; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 8px 8px;'>
                            <p>Halo,</p>
                            <p>Pemberitahuan bahwa pengajuan PO sparepart berikut telah disetujui di salah satu tahap approval:</p>
                            <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                                <tr style='border-bottom: 1px solid #e2e8f0;'>
                                    <td style='padding: 8px; font-weight: bold;'>Request No</td>
                                    <td style='padding: 8px;'>{header.RequestNo}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e2e8f0;'>
                                    <td style='padding: 8px; font-weight: bold;'>Kategori</td>
                                    <td style='padding: 8px;'>
                                        {(header.Category == "Project" 
                                            ? "<span style='background: #e0e7ff; color: #4338ca; padding: 2px 8px; border-radius: 4px; font-weight: bold; font-size: 12px;'>🚀 Project</span>" 
                                            : "<span style='background: #dbeafe; color: #1d4ed8; padding: 2px 8px; border-radius: 4px; font-weight: bold; font-size: 12px;'>📦 Regular</span>")}
                                    </td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e2e8f0;'>
                                    <td style='padding: 8px; font-weight: bold;'>Title</td>
                                    <td style='padding: 8px;'>{header.Title}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e2e8f0;'>
                                    <td style='padding: 8px; font-weight: bold;'>Status Saat Ini</td>
                                    <td style='padding: 8px;'>{header.Status}</td>
                                </tr>
                            </table>
                            <p style='margin-top: 24px;'>You can view the details on the portal.</p>
                            <p style='margin-top: 24px; font-size: 12px; color: #64748b;'>
                                This is an automated notification from NGK Portal. Please do not reply directly to this email.
                            </p>
                        </div>
                    </div>";

                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Sparepart PO Notification");
                var password = "100%NGKbusi!";

                using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                {
                    smtp.EnableSsl = false;
                    smtp.Credentials = new NetworkCredential(senderEmail.Address, password);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var mess = new MailMessage()
                    {
                        From = senderEmail,
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        foreach (string email in emailSendTo)
                        {
                            mess.To.Add(new MailAddress(email));
                        }
                        smtp.Send(mess);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[INFO EMAIL ERROR] " + ex.Message);
            }
        }

        private void SendReminderEmail(SCM_Sparepart_PO_Request_Header header, string taskTitle, string toEmail, string toName, string waitingApprovalInfo, string requesterName)
        {
            try
            {
                // Resolve Target Department Names
                string targetDepartmentNames = "-";
                if (!string.IsNullOrEmpty(header.TargetDepartment))
                {
                    var deptIds = header.TargetDepartment.Split(',');
                    var deptNames = db.V_Users_Active
                        .Where(u => deptIds.Contains(u.AXCostID) && u.AXCostName != null && u.AXCostName != "")
                        .Select(u => u.AXCostName)
                        .Distinct()
                        .ToList();
                    targetDepartmentNames = deptNames.Any() ? string.Join(", ", deptNames) : header.TargetDepartment;
                }

                string subject = "[REMINDER] " + taskTitle;
                string detailUrl = "https://portal.ngkbusi.com/NGKBusi/SCM/SparepartPORequest/Detail?id=" + header.ID;
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 650px; margin: 0 auto; color: #334155; line-height: 1.6;'>
                        <div style='background: linear-gradient(135deg, #2563eb, #1d4ed8); padding: 22px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <h2 style='color: #ffffff; margin: 0; font-size: 20px; font-weight: 700; letter-spacing: -0.5px;'>Pending Approval Reminder</h2>
                            <p style='color: #bfdbfe; margin: 4px 0 0 0; font-size: 12px;'>SCM Sparepart PO Request System</p>
                        </div>
                        <div style='padding: 24px; background: #ffffff; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 12px 12px;'>
                            <p style='margin-top: 0;'>Yth. Bapak/Ibu <strong>{toName}</strong>,</p>
                            <p>Berikut adalah pengingat bahwa permintaan Sparepart PO berikut masih memerlukan proses persetujuan (approval):</p>
                            
                            <!-- Highlight Waiting Approval Box -->
                            <div style='background: #fffbeb; border: 1px solid #fef3c7; border-left: 4px solid #f59e0b; padding: 12px 16px; border-radius: 6px; margin: 16px 0;'>
                                <span style='font-size: 11px; font-weight: bold; color: #b45309; text-transform: uppercase; display: block;'>Status Saat Ini:</span>
                                <strong style='font-size: 14px; color: #92400e; display: block; margin-top: 2px;'>Menunggu Approval dari: <span style='color: #dc2626;'>{waitingApprovalInfo}</span></strong>
                            </div>

                            <table style='width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 13px;'>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; width: 160px; color: #64748b;'>No. Request</td>
                                    <td style='padding: 8px 4px; font-weight: 600; color: #1e293b;'>{header.RequestNo}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Kategori Pengajuan</td>
                                    <td style='padding: 8px 4px;'>
                                        {(header.Category == "Project" 
                                            ? "<span style='background: #e0e7ff; color: #4338ca; padding: 3px 10px; border-radius: 6px; font-weight: bold; font-size: 12px; display: inline-block;'>🚀 Project (Khusus / Improvement)</span>" 
                                            : "<span style='background: #dbeafe; color: #1d4ed8; padding: 3px 10px; border-radius: 6px; font-weight: bold; font-size: 12px; display: inline-block;'>📦 Regular (Consumables / Operasional)</span>")}
                                    </td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Judul Permintaan</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{header.Title}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Pemohon (Requester)</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{requesterName} ({header.RequesterNIK})</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Tanggal Pengajuan</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{header.CreatedDate:dd MMM yyyy HH:mm} WIB</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Target Departemen</td>
                                    <td style='padding: 8px 4px; color: #1e293b;'>{targetDepartmentNames}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #f1f5f9;'>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #64748b;'>Waiting Approval</td>
                                    <td style='padding: 8px 4px; font-weight: bold; color: #dc2626;'>{waitingApprovalInfo}</td>
                                </tr>
                            </table>

                            <div style='margin-top: 28px; text-align: center;'>
                                <a href='{detailUrl}' style='display: inline-block; background: #2563eb; color: #ffffff; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: bold; font-size: 13px; box-shadow: 0 2px 4px rgba(37,99,235,0.2);'>Lihat Rincian & Proses Persetujuan</a>
                            </div>

                            <p style='margin-top: 28px; font-size: 12px; color: #94a3b8; border-top: 1px solid #f1f5f9; padding-top: 16px;'>
                                Email ini dikirim secara otomatis oleh NGK Portal System. Mohon untuk tidak membalas email ini secara langsung.
                            </p>
                        </div>
                    </div>";

                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Sparepart PO Reminder");
                var password = "100%NGKbusi!";

                using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                {
                    smtp.EnableSsl = false;
                    smtp.Credentials = new NetworkCredential(senderEmail.Address, password);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var mess = new MailMessage(senderEmail, new MailAddress(toEmail))
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        mess.Bcc.Add(new MailAddress("ikhsan.sholihin@ngkbusi.com"));
                        smtp.Send(mess);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[REMINDER EMAIL ERROR] " + ex.Message);
            }
        }

        // Seeding helper to register the current user as level 1, level 2, and notification recipient for testing.
        public ActionResult SeedConfig()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);

            if (CurrUser != null)
            {
                var existing = dbsp.SCM_Sparepart_PO_Request_Approval_Configs.Where(c => c.NIK == currUser).ToList();
                if (existing.Count == 0)
                {
                    dbsp.SCM_Sparepart_PO_Request_Approval_Configs.Add(new SCM_Sparepart_PO_Request_Approval_Config
                    {
                        NIK = currUser,
                        Level = 1,
                        IsApprover = true,
                        Department = CurrUser.AXCostID ?? "100",
                        IsActive = true,
                        IsOptional = true,
                        TriggersInfoNotification = true,
                        StartsReminder = false,
                        TaskTitle = "Checked By"
                    });

                    dbsp.SCM_Sparepart_PO_Request_Approval_Configs.Add(new SCM_Sparepart_PO_Request_Approval_Config
                    {
                        NIK = currUser,
                        Level = 2,
                        IsApprover = true,
                        Department = CurrUser.AXCostID ?? "100",
                        IsActive = true,
                        IsOptional = false,
                        TriggersInfoNotification = false,
                        StartsReminder = true,
                        TaskTitle = "Approve By"
                    });

                    dbsp.SCM_Sparepart_PO_Request_Approval_Configs.Add(new SCM_Sparepart_PO_Request_Approval_Config
                    {
                        NIK = currUser,
                        Level = 0,
                        IsApprover = false,
                        Department = CurrUser.AXCostID ?? "100",
                        IsActive = true,
                        IsOptional = false,
                        TriggersInfoNotification = false,
                        StartsReminder = false,
                        TaskTitle = "Received By"
                    });

                    dbsp.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }
    }
}
