using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace NGKBusi.Areas.IT.Controllers
{
    public class ITDisposalController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        ITDisposalConnection dbv = new ITDisposalConnection();
        ITWastehandoverConnection dbx = new ITWastehandoverConnection();
        [Authorize]

        public ActionResult Index()
        {
            var currentUserName = User.Identity.Name;
            var userRecord = db.V_Users_Active
                                 .FirstOrDefault(u => u.Name == currentUserName);

            var allowedDepartments = new List<string>
            {
                "INFORMATION TECHNOLOGY"
            };

            string userDept = userRecord?.CostName?.ToUpper() ?? "";

            if (userRecord == null || !allowedDepartments.Contains(userDept))
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                ViewBag.DeptName = _currUser.FindFirstValue("deptName");
                return View("~/Areas/IT/Views/MasterMenus/_NoAccess.cshtml");
            }

            return View();
        }

        [HttpGet]
        public ActionResult Create()
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            ViewBag.department = _currUser.FindFirstValue("deptName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                string userNik = _currUser.FindFirstValue(ClaimTypes.NameIdentifier);
                string userName = _currUser.Name;
                string deptName = _currUser.FindFirstValue("deptName");

                var lastRecord = dbv.IT_Disposal_Header.OrderByDescending(x => x.id).FirstOrDefault();
                int lastNumber = lastRecord != null ? int.Parse(lastRecord.header_id.Substring(5, 4)) : 0;
                string currentHeaderId = $"DISP-{(lastNumber + 1):D4}{DateTime.Now.Year}";

                var disposalHeader = new IT_Disposal_Header
                {
                    header_id = currentHeaderId,
                    department = deptName,
                    requester_nik = userNik,
                    requester_name = userName,
                    status = "Pending"
                };

                dbv.IT_Disposal_Header.Add(disposalHeader);
                dbv.SaveChanges();

                SendEmail(disposalHeader.header_id, "pending", userNik);

                return RedirectToAction("Details", new { id = disposalHeader.header_id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                var _currUser = (ClaimsIdentity)User.Identity;
                ViewBag.department = _currUser.FindFirstValue("deptName");
                return View();
            }
        }

        private bool IsAuthorized(string action)
        {
            string currentUserName = User.Identity.Name;
            var userRecord = db.V_Users_Active.FirstOrDefault(u => u.Name == currentUserName);
            if (userRecord == null) return false;

            string currentUserNik = userRecord.NIK != null ? userRecord.NIK.ToString() : string.Empty;

            // TODO: Ambil daftar NIK Checker, Approver, dan Creator dari database atau konfigurasi (App Settings)
            var checkerNiks = new List<string> { /* TODO: NIK Checker */ };
            var approverNiks = new List<string> { /* TODO: NIK Approver */ };
            var creatorNiks = new List<string> { /* TODO: NIK Creator */ };

            if (action == "Check") return checkerNiks.Contains(currentUserNik);
            if (action == "Approve") return approverNiks.Contains(currentUserNik);
            if (action == "Delete") return creatorNiks.Contains(currentUserNik);

            return false;
        }

        [HttpPost]
        public JsonResult DeleteDisposal(int id)
        {
            try
            {
                if (!IsAuthorized("Delete"))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Access Denied."
                    });
                }

                var header = dbv.IT_Disposal_Header
                                .FirstOrDefault(x => x.id == id);

                if (header == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Data not found."
                    });
                }

                var details = dbv.IT_Disposal_Detail
                               .Where(x => x.header_id == header.header_id)
                               .ToList();

                if (details.Any())
                {
                    dbv.IT_Disposal_Detail.RemoveRange(details);
                }

                var approvals = dbv.IT_Disposal_Approval
                                   .Where(x => x.header_id == header.header_id)
                                   .ToList();

                if (approvals.Any())
                {
                    dbv.IT_Disposal_Approval.RemoveRange(approvals);
                }

                dbv.IT_Disposal_Header.Remove(header);
                dbv.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]

        public JsonResult SaveDisposal(
            string header_id,
            string items,
            int? quantity,
            string date,
            string unit,
            string asset_no,
            string type,
            string condition,
            string actions,
            string confidentiality,
            string department,
            string status)
        {
            try
            {
                string currentHeaderId = header_id;

                if (string.IsNullOrEmpty(currentHeaderId))
                {
                    var lastRecord = dbv.IT_Disposal_Header
                                        .OrderByDescending(x => x.id)
                                        .FirstOrDefault();

                    int lastNumber = 0;

                    if (lastRecord != null)
                    {
                        lastNumber = int.Parse(lastRecord.header_id.Substring(5, 4));
                    }

                    currentHeaderId = $"DISP-{(lastNumber + 1):D4}{DateTime.Now.Year}";

                    dbv.IT_Disposal_Header.Add(new IT_Disposal_Header
                    {
                        header_id = currentHeaderId,
                        department = department,
                        requester_nik = User.Identity.GetUserId(),
                        requester_name = User.Identity.GetUserName(),
                        status = string.IsNullOrEmpty(status) ? "Draft" : status
                    });

                    dbv.SaveChanges();
                }
                else
                {
                    var header = dbv.IT_Disposal_Header
                                    .FirstOrDefault(x => x.header_id == currentHeaderId);

                    if (header != null)
                    {
                        header.status = status;
                        dbv.SaveChanges();
                    }
                }

                dbv.IT_Disposal_Detail.Add(new IT_Disposal_Detail
                {
                    header_id = currentHeaderId,
                    items = items,
                    quantity = quantity ?? 0,
                    unit = unit,
                    asset_no = asset_no,
                    type = type,
                    condition = condition,
                    actions = actions,
                    confidentiality = confidentiality,
                    created_date = string.IsNullOrEmpty(date) ? DateTime.Now : DateTime.Parse(date)
                });

                dbv.SaveChanges();

                if (status == "Pending")
                {
                    try
                    {
                        SendEmail(currentHeaderId, "pending", User.Identity.GetUserId());
                    }
                    catch (Exception)
                    {
                    }
                }

                return Json(new { success = true, id = currentHeaderId }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Details(string id)
        {
            var header = dbv.IT_Disposal_Header.FirstOrDefault(h => h.header_id == id);
            if (header == null) return HttpNotFound();

            ViewBag.IsChecker = IsAuthorized("Check");
            ViewBag.IsApprover = IsAuthorized("Approve");
            ViewBag.IsCreator = (User.Identity.Name != null && header.requester_name != null &&
                                User.Identity.Name.Trim().Equals(header.requester_name.Trim(), StringComparison.OrdinalIgnoreCase));

            var latestFeedback = dbv.IT_Disposal_Approval
                                     .Where(a => a.header_id == id &&
                                       (a.status == "ReturnedByChecker" || a.status == "ReturnedByApprover" ||
                                        a.status == "RejectedByChecker" || a.status == "RejectedByApprover"))
                                     .OrderByDescending(a => a.approval_id)
                                     .FirstOrDefault();

            if (header.status != null &&
               (header.status == "Checked" || header.status == "Approved" || header.status.Contains("Completed")))
            {
                ViewBag.LatestMessage = null;
                ViewBag.FeedbackStatus = null;
            }
            else
            {
                ViewBag.LatestMessage = latestFeedback?.message;
                ViewBag.FeedbackStatus = latestFeedback?.status;
            }

            var details = dbv.IT_Disposal_Detail.Where(d => d.header_id == id).ToList();

            ViewBag.Header = header;
            ViewBag.Details = details;
            ViewBag.CreatedDate = details.FirstOrDefault()?.created_date;

            return View();
        }

        [HttpPost]
        public JsonResult UpdateStatus(string id, string newStatus, string message)
        {
            try
            {
                var header = dbv.IT_Disposal_Header.FirstOrDefault(h => h.header_id == id);
                if (header == null) return Json(new { success = false, message = "Data not found" });

                header.status = newStatus;
                string feedbackMessage = message;

                if (newStatus.Contains("Checked") || newStatus == "ReturnedByChecker" || newStatus == "RejectedByChecker")
                {
                    header.CheckedBy = User.Identity.Name;
                    header.CheckedDate = DateTime.Now;
                }
                else if (newStatus.Contains("Approved") || newStatus == "ReturnedByApprover" || newStatus == "RejectedByApprover")
                {
                    header.ApprovedBy = User.Identity.Name;
                    header.ApprovedDate = DateTime.Now;
                }

                var approvalLog = new IT_Disposal_Approval
                {
                    header_id = id,
                    status = newStatus,
                    message = message,
                    signed_by = User.Identity.Name,
                    signed_date = DateTime.Now.ToString("yyyy-MM-dd")
                };

                dbv.IT_Disposal_Approval.Add(approvalLog);
                dbv.SaveChanges();

                string emailAction = "pending";
                if (newStatus.Contains("Checked")) emailAction = "approve_checker";
                else if (newStatus.Contains("Approved")) emailAction = "approve";
                else if (newStatus.Contains("Returned")) emailAction = "return";
                else if (newStatus.Contains("Rejected")) emailAction = "reject";

                SendEmail(id, emailAction, User.Identity.GetUserId(), feedbackMessage);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveAndResubmit(string headerId, string detailsJson)
        {
            try
            {
                var header = dbv.IT_Disposal_Header.FirstOrDefault(h => h.header_id == headerId);
                if (header == null) return Json(new { success = false, message = "Header not found." });

                header.CheckedBy = null;
                header.CheckedDate = null;
                header.ApprovedBy = null;
                header.ApprovedDate = null;
                header.status = "Pending";

                var serializer = new JavaScriptSerializer();
                var incomingDetails = serializer.Deserialize<List<Dictionary<string, object>>>(detailsJson);

                var existingDetails = dbv.IT_Disposal_Detail.Where(d => d.header_id == headerId).ToList();
                var incomingIds = new List<int>();

                foreach (var item in incomingDetails)
                {
                    int detailId = 0;
                    if (item.ContainsKey("id") && item["id"] != null && int.TryParse(item["id"].ToString(), out detailId))
                    {
                        incomingIds.Add(detailId);
                    }

                    var detail = existingDetails.FirstOrDefault(d => d.detail_id == detailId);

                    if (detail != null)
                    {
                        detail.items = item["items"]?.ToString();
                        detail.quantity = int.Parse(item["quantity"]?.ToString() ?? "0");
                        detail.unit = item["unit"]?.ToString();
                        detail.asset_no = item["asset_no"]?.ToString();
                        detail.type = item["type"]?.ToString();
                        detail.condition = item["condition"]?.ToString();
                        detail.actions = item["actions"]?.ToString();
                        detail.confidentiality = item["confidentiality"]?.ToString();
                    }
                    else
                    {
                        var newDetail = new IT_Disposal_Detail
                        {
                            header_id = headerId,
                            items = item["items"]?.ToString(),
                            quantity = int.Parse(item["quantity"]?.ToString() ?? "0"),
                            unit = item["unit"]?.ToString(),
                            asset_no = item["asset_no"]?.ToString(),
                            type = item["type"]?.ToString(),
                            condition = item["condition"]?.ToString(),
                            actions = item["actions"]?.ToString(),
                            confidentiality = item["confidentiality"]?.ToString()
                        };
                        dbv.IT_Disposal_Detail.Add(newDetail);
                    }
                }

                var detailsToDelete = existingDetails.Where(d => !incomingIds.Contains(d.detail_id)).ToList();
                foreach (var obsoleteDetail in detailsToDelete)
                {
                    dbv.IT_Disposal_Detail.Remove(obsoleteDetail);
                }

                dbv.SaveChanges();

                SendEmail(headerId, "pending", User.Identity.GetUserId(), "");

                return Json(new { success = true, message = "Resubmitted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetDisposalHeaders()
        {
            var currentUserName = User.Identity.Name;
            var userRecord = db.V_Users_Active.FirstOrDefault(u => u.Name == currentUserName);
            string currentUserNik = userRecord != null && userRecord.NIK != null ? userRecord.NIK.ToString() : string.Empty;

            // TODO: Ambil daftar NIK dari database atau konfigurasi
            var creatorNiks = new List<string> { /* TODO: NIK Creator */ };
            bool canDelete = creatorNiks.Contains(currentUserNik);
            bool canCreate = creatorNiks.Contains(currentUserNik);

            var data = dbv.IT_Disposal_Header
                          .OrderByDescending(h => h.id)
                          .Select(h => new {
                              id = h.id,
                              header_id = h.header_id,
                              department = h.department,
                              requester_name = h.requester_name,
                              status = h.status,
                              canDelete = canDelete
                          }).ToList();

            return Json(new
            {
                data = data,
                canCreate = canCreate
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToExcel(string id)
        {
            var header = dbv.IT_Disposal_Header.FirstOrDefault(h => h.header_id == id);

            if (header == null)
                return HttpNotFound();

            var details = dbv.IT_Disposal_Detail
                                .Where(x => x.header_id == id)
                                .ToList();

            using (XLWorkbook workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("IT Disposal");

                ws.ShowGridLines = true;

                #region COLUMN WIDTH
                ws.Column("A").Width = 5;
                ws.Column("B").Width = 16;
                ws.Column("C").Width = 16;
                ws.Column("D").Width = 20;
                ws.Column("E").Width = 26;
                ws.Column("F").Width = 22;
                ws.Column("G").Width = 22;
                ws.Column("H").Width = 22;
                ws.Column("I").Width = 10;
                ws.Column("J").Width = 16;
                #endregion

                #region LOGO
                ws.Range("A1:B2").Merge();
                string logoPath = Server.MapPath("~/Content/images/niterra-logo.jpg"); // TODO: Sesuaikan dengan nama logo perusahaan

                if (System.IO.File.Exists(logoPath))
                {
                    var pic = ws.AddPicture(logoPath)
                                .MoveTo(ws.Cell("A1"), 8, 4);

                    pic.Width = 95;
                    pic.Height = 32;
                }
                #endregion

                #region TITLE
                ws.Range("C1:H1").Merge();
                ws.Cell("C1").Value = "FORMULIR INTEGRASI";

                ws.Range("C2:H2").Merge();
                ws.Cell("C2").Value = "IT DISPOSAL";

                ws.Range("C1:H2").Style.Font.Bold = true;
                ws.Range("C1:H2").Style.Font.FontName = "Arial";
                ws.Range("C1:H2").Style.Font.FontSize = 12;
                ws.Range("C1:H2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range("C1:H2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                ws.Row(1).Height = 20;
                ws.Row(2).Height = 20;
                ws.Row(3).Height = 20;
                ws.Row(4).Height = 20;
                #endregion

                #region DOCUMENT INFO
                ws.Cell("I1").Value = "No.Dok.";
                ws.Cell("I2").Value = "Revisi";
                ws.Cell("I3").Value = "Tanggal";
                ws.Cell("I4").Value = "Halaman";

                ws.Cell("J1").Value = "PMLK3-IT-01/L13";
                ws.Cell("J2").Value = "00";
                ws.Cell("J3").Value = "05-Apr-23";
                ws.Cell("J4").Value = "1 of 1";

                ws.Range("I1:J4").Style.Font.FontName = "Arial";
                ws.Range("I1:J4").Style.Font.FontSize = 9;
                ws.Range("I1:J4").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range("I1:I4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Range("I1:I4").Style.Font.Bold = true;
                ws.Range("J1:J4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                #endregion

                #region HEADER BORDER
                ws.Range("A1:J4").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range("A1:J4").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                #endregion

                #region EMPTY ROWS
                ws.Row(3).Height = 18;
                ws.Row(4).Height = 18;
                ws.Range("A3:H4").Merge();
                ws.Range("A3:J4").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range("A3:J4").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                #endregion

                #region DEPARTMENT
                ws.Row(5).Height = 22;
                ws.Range("A5:B5").Merge();
                ws.Cell("A5").Value = "Departement";

                ws.Range("C5:J5").Merge();
                ws.Cell("C5").Value = ": " + header.department;

                ws.Range("A5:J5").Style.Font.Bold = true;
                ws.Range("A5:J5").Style.Font.FontName = "Arial";
                ws.Range("A5:J5").Style.Font.FontSize = 10;
                ws.Range("A5:J5").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range("A5:J5").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                #endregion

                #region TABLE HEADER
                int headerRow = 6;

                ws.Cell(headerRow, 1).Value = "NO";
                ws.Cell(headerRow, 2).Value = "ITEMS";
                ws.Cell(headerRow, 3).Value = "QUANTITY &\nUNIT";
                ws.Cell(headerRow, 4).Value = "ASSET NO";
                ws.Cell(headerRow, 5).Value = "TYPE\n(Laptop / PC / Server /\nStorage / Other)";
                ws.Cell(headerRow, 6).Value = "CONDITION";
                ws.Cell(headerRow, 7).Value = "ACTIONS\n(Auctioned /\nDonated /\nDestroyed)";
                ws.Cell(headerRow, 8).Value = "CONFIDENTIALITY\n(Public /\nInternal /\nConfidential)";

                ws.Range(headerRow, 9, headerRow, 10).Merge();
                ws.Cell(headerRow, 9).Value = "Date";

                ws.Range("A6:J6").Style.Font.Bold = true;
                ws.Range("A6:J6").Style.Font.FontName = "Arial";
                ws.Range("A6:J6").Style.Font.FontSize = 9;
                ws.Range("A6:J6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range("A6:J6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range("A6:J6").Style.Alignment.WrapText = true;

                ws.Row(6).Height = 55;
                ws.Range("A6:J6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range("A6:J6").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                #endregion

                #region DETAIL
                int startRow = 7;
                int no = 1;
                int totalRowsToDisplay = Math.Max(details.Count, 18);

                for (int i = 0; i < totalRowsToDisplay; i++)
                {
                    if (i < details.Count)
                    {
                        var item = details[i];
                        ws.Cell(startRow, 1).Value = no++;
                        ws.Cell(startRow, 2).Value = item.items;
                        ws.Cell(startRow, 3).Value = $"{item.quantity} {item.unit}";
                        ws.Cell(startRow, 4).Value = item.asset_no;
                        ws.Cell(startRow, 5).Value = item.type;
                        ws.Cell(startRow, 6).Value = item.condition;
                        ws.Cell(startRow, 7).Value = item.actions;
                        ws.Cell(startRow, 8).Value = item.confidentiality;

                        ws.Range(startRow, 9, startRow, 10).Merge();
                        ws.Cell(startRow, 9).Value = item.created_date.ToString("dd-MM-yyyy");
                    }
                    else
                    {
                        ws.Range(startRow, 9, startRow, 10).Merge();
                    }

                    ws.Row(startRow).Height = 22;
                    var range = ws.Range(startRow, 1, startRow, 10);

                    range.Style.Font.FontName = "Arial";
                    range.Style.Font.FontSize = 9;
                    range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    range.Style.Alignment.WrapText = true;
                    range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    ws.Cell(startRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(startRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(startRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(startRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    startRow++;
                }
                #endregion

                #region SIGNATURE
                int signatureRow = startRow + 2;

                ws.Cell(signatureRow, 7).Value = "Approved By";
                ws.Cell(signatureRow, 8).Value = "Checked By";
                ws.Range(signatureRow, 9, signatureRow, 10).Merge();
                ws.Cell(signatureRow, 9).Value = "PIC IT";

                var headerSigRange = ws.Range(signatureRow, 7, signatureRow, 10);
                headerSigRange.Style.Font.Bold = true;
                headerSigRange.Style.Font.FontName = "Arial";
                headerSigRange.Style.Font.FontSize = 9;
                headerSigRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerSigRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var firstDetail = details.FirstOrDefault();

                string approvedVal = !string.IsNullOrEmpty(header.ApprovedBy)
                    ? "✔ Approved\n" + header.ApprovedBy + (header.ApprovedDate.HasValue ? "\n" + header.ApprovedDate.Value.ToString("dd-MMM-yyyy") : "")
                    : "-";

                string checkedVal = !string.IsNullOrEmpty(header.CheckedBy)
                    ? "✔ Checked\n" + header.CheckedBy + (header.CheckedDate.HasValue ? "\n" + header.CheckedDate.Value.ToString("dd-MMM-yyyy") : "")
                    : "-";

                string createdVal = "✔ Created\n" +
                    header.requester_name +
                    (firstDetail != null ? "\n" + firstDetail.created_date.ToString("dd-MMM-yyyy") : "");

                ws.Cell(signatureRow + 1, 7).Value = approvedVal;
                ws.Cell(signatureRow + 1, 8).Value = checkedVal;
                ws.Range(signatureRow + 1, 9, signatureRow + 1, 10).Merge();
                ws.Cell(signatureRow + 1, 9).Value = createdVal;

                int sigBoxEndRow = signatureRow + 3;

                ws.Range(signatureRow + 1, 7, sigBoxEndRow, 7).Merge();
                ws.Range(signatureRow + 1, 8, sigBoxEndRow, 8).Merge();
                ws.Range(signatureRow + 1, 9, sigBoxEndRow, 10).Merge();

                var fullSigRange = ws.Range(signatureRow, 7, sigBoxEndRow, 10);
                fullSigRange.Style.Font.FontName = "Arial";
                fullSigRange.Style.Font.FontSize = 9;
                fullSigRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                fullSigRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                fullSigRange.Style.Alignment.WrapText = true;
                fullSigRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                fullSigRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                #endregion

                #region OUTER BORDER
                ws.Range(1, 1, sigBoxEndRow, 10)
                    .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                #endregion

                #region SAVE FILE
                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "IT_Disposal_" + header.header_id + ".xlsx");
                }
                #endregion
            }
        }

        [HttpPost]
        public JsonResult SendFromWasteHandover(List<string> selectedIds)
        {
            try
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    return Json(new { success = false, message = "Tidak ada data yang dipilih." });
                }

                var _currUser = (ClaimsIdentity)User.Identity;
                string userNik = _currUser.FindFirstValue(ClaimTypes.NameIdentifier);
                string userName = _currUser.Name;
                string deptName = _currUser.FindFirstValue("deptName");

                string currentHeaderId = "";

                using (var dbDisposal = new ITDisposalConnection())
                {
                    var lastRecord = dbDisposal.IT_Disposal_Header.OrderByDescending(x => x.id).FirstOrDefault();
                    int lastNumber = lastRecord != null && !string.IsNullOrEmpty(lastRecord.header_id) && lastRecord.header_id.Length >= 9
                                   ? int.Parse(lastRecord.header_id.Substring(5, 4)) : 0;
                    currentHeaderId = $"DISP-{(lastNumber + 1):D4}{DateTime.Now.Year}";

                    var disposalHeader = new IT_Disposal_Header
                    {
                        header_id = currentHeaderId,
                        department = deptName,
                        requester_nik = userNik,
                        requester_name = userName,
                        status = "Pending"
                    };

                    dbDisposal.IT_Disposal_Header.Add(disposalHeader);
                    dbDisposal.SaveChanges();

                    foreach (var idStr in selectedIds)
                    {
                        int detailId = int.Parse(idStr);

                        var wasteDetail = dbx.IT_Wastehandover_Detail
                                             .FirstOrDefault(x => x.detail_id == detailId);

                        if (wasteDetail != null)
                        {
                            wasteDetail.is_generated = true;

                            var disposalDetail = new IT_Disposal_Detail
                            {
                                header_id = currentHeaderId,
                                items = wasteDetail.items,
                                quantity = wasteDetail.quantity,
                                unit = wasteDetail.unit,
                                asset_no = wasteDetail.asset_no,
                                type = wasteDetail.type,
                                condition = wasteDetail.condition,
                                actions = wasteDetail.actions,
                                confidentiality = wasteDetail.confidentiality,
                                created_date = DateTime.Now
                            };
                            dbDisposal.IT_Disposal_Detail.Add(disposalDetail);

                            string wasteRefCode = "";
                            var wasteMaster = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == wasteDetail.disposal_id);

                            if (wasteMaster != null && !string.IsNullOrEmpty(wasteMaster.disposal_ref_no) && wasteMaster.disposal_ref_no != "-")
                            {
                                wasteRefCode = wasteMaster.disposal_ref_no;
                            }
                            else if (!string.IsNullOrEmpty(wasteDetail.header_id) && wasteDetail.header_id != "-")
                            {
                                wasteRefCode = wasteDetail.header_id;
                            }
                            else
                            {
                                wasteRefCode = $"WH-{DateTime.Now.Year}-{wasteDetail.detail_id}";
                            }

                            var relationRecord = new IT_Disp_WH_Relation
                            {
                                disposal_id = currentHeaderId,
                                waste_handover_id = wasteRefCode
                            };
                            dbDisposal.IT_Disp_WH_Relation.Add(relationRecord);
                        }
                    }

                    dbDisposal.SaveChanges();
                    dbx.SaveChanges();
                }

                try
                {
                    SendEmail(currentHeaderId, "sent_to_disposal", userNik);
                }
                catch (Exception)
                {
                }

                return Json(new { success = true, message = "Data berhasil dikirim dan direkam ke relasi IT Disposal!" });
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? (ex.InnerException.InnerException?.Message ?? ex.InnerException.Message) : ex.Message;
                return Json(new { success = false, message = "Error: " + errorMsg });
            }
        }

        [HttpGet]
        public JsonResult GetWasteHandoverHeaders()
        {
            var currentUserName = User.Identity.Name;
            var userRecord = db.V_Users_Active.FirstOrDefault(u => u.Name == currentUserName);
            string currentUserNik = userRecord != null && userRecord.NIK != null ? userRecord.NIK.ToString() : string.Empty;

            // TODO: Ambil daftar NIK dari database atau konfigurasi
            var checkerNiks = new List<string> { /* TODO: NIK Checker */ };
            var creatorNiks = new List<string> { /* TODO: NIK Creator */ };

            bool canCheck = checkerNiks.Contains(currentUserNik);
            bool canDelete = creatorNiks.Contains(currentUserNik);

            var detailList = dbx.IT_Wastehandover_Detail
                                .Where(d => d.items != null && d.items.Trim() != "")
                                .OrderByDescending(d => d.created_date)
                                .ToList();

            var data = detailList.Select(detail => {
                var m = dbx.IT_Disposal_Master.FirstOrDefault(x => x.disposal_id == detail.disposal_id);
                var latestApproval = dbx.IT_Wastehandover_Approval
                                        .Where(a => a.disposal_id == detail.disposal_id)
                                        .OrderByDescending(a => a.approval_id)
                                        .FirstOrDefault();

                DateTime finalDate = detail.created_date != default(DateTime)
                                     ? detail.created_date
                                     : (m != null ? m.disposal_date : DateTime.Now);

                string currentStatus = latestApproval?.status ?? (m != null ? m.status : "Draft");

                return new
                {
                    disposal_id = detail.detail_id,
                    master_id = detail.disposal_id,
                    header_id = detail.header_id ?? (m != null ? m.disposal_ref_no : "-"),
                    department = m != null ? (m.department ?? "GENERAL") : "GENERAL",
                    items = detail.items,
                    quantity = detail.quantity,
                    unit = detail.unit ?? "unit",
                    asset_no = detail.asset_no,
                    type = detail.type,
                    condition = detail.condition,
                    actions = detail.actions,
                    confidentiality = detail.confidentiality,
                    requester_name = latestApproval?.signed_by ?? "-",
                    created_date = finalDate,
                    status = currentStatus,
                    canCheck = canCheck,
                    canDelete = canDelete
                };
            })
        .Where(x => x.status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                    x.status.Equals("Checked", StringComparison.OrdinalIgnoreCase) ||
                    x.status.Equals("SignedByCreator", StringComparison.OrdinalIgnoreCase))
        .ToList();

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExportToPdf(string id)
        {
            var header = dbv.IT_Disposal_Header.FirstOrDefault(h => h.header_id == id);
            if (header == null)
                return HttpNotFound();

            return new Rotativa.ActionAsPdf("Details", new { id = id })
            {
                FileName = "IT_Disposal_" + header.header_id + ".pdf",
                PageOrientation = Rotativa.Options.Orientation.Landscape,
                PageSize = Rotativa.Options.Size.A4,
                CustomSwitches = "--disable-smart-shrinking"
            };
        }

        public void SendEmail(string idRequest, string action, string senderNik, string feedbackMessage = "")
        {
            try
            {
                string filePath = Server.MapPath("~/Emails/IT/Disposal/notif.html");
                if (!System.IO.File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("Template HTML tidak ditemukan di: " + filePath);
                    return;
                }

                string mailText = System.IO.File.ReadAllText(filePath);

                var request = dbv.IT_Disposal_Header.FirstOrDefault(w => w.header_id == idRequest);
                if (request == null) return;

                var user = db.V_Users_Active.FirstOrDefault(w => w.NIK == request.requester_nik);
                string username = user?.Name ?? request.requester_name;

                var checkerData = dbv.IT_Disposal_Admin.FirstOrDefault(w => w.role.ToLower() == "checker" && w.is_active == 1);
                var approverData = dbv.IT_Disposal_Admin.FirstOrDefault(w => w.role.ToLower() == "approver" && w.is_active == 1);

                string checkerName = GetNameByNik(checkerData?.nik ?? "");
                string approverName = GetNameByNik(approverData?.nik ?? "");

                string receiverName = "", headMessage = "", originalTarget = "";

                if (action == "pending")
                {
                    headMessage = "We would like to inform you that an IT Disposal request has been submitted and requires checking.";
                    receiverName = checkerName;
                    originalTarget = GetEmailByNik(checkerData?.nik ?? "");
                }
                else if (action == "check" || action == "approved_by_checker" || action == "checked")
                {
                    headMessage = "An IT Disposal request has been checked and now requires your final approval.";
                    receiverName = approverName;
                    originalTarget = GetEmailByNik(approverData?.nik ?? "");
                }
                else if (action == "approve" || action == "approved")
                {
                    headMessage = "Your IT Disposal request has been fully approved.";
                    receiverName = username;
                    originalTarget = user?.Email;
                }
                else if (action == "return")
                {
                    string reason = !string.IsNullOrEmpty(feedbackMessage) ? feedbackMessage : "No reason provided.";
                    headMessage = $"Your IT Disposal request has been returned. Reason: {reason}";
                    receiverName = username;
                    originalTarget = user?.Email;
                }
                else if (action == "reject")
                {
                    string reason = !string.IsNullOrEmpty(feedbackMessage) ? feedbackMessage : "No reason provided.";
                    headMessage = $"Your IT Disposal request has been rejected. Reason: {reason}";
                    receiverName = username;
                    originalTarget = user?.Email;
                }
                else if (action == "sent_to_disposal")
                {
                    headMessage = "An IT Waste Handover item has been sent to IT Disposal and requires your review.";
                    receiverName = checkerName;
                    originalTarget = GetEmailByNik(checkerData?.nik ?? "");
                }
                else
                {
                    headMessage = "An update has been made to your IT Disposal request.";
                    receiverName = username;
                    originalTarget = user?.Email;
                }

                // TODO: Ambil email default dari AppSettings / Configuration
                string receivermail = !string.IsNullOrEmpty(originalTarget) ? originalTarget : "TODO_DEFAULT_RECEIVER_EMAIL";
                // TODO: Ambil Base URL dari AppSettings / Configuration
                string targetUrl = $"TODO_BASE_URL/IT/ITDisposal/Details/{request.header_id}";

                string linkHtml = $"<div style='margin: 20px 0;'>" +
                                  $"<a href='{targetUrl}' target='_blank' style='background-color: #007bff; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Buka Detail IT Disposal</a>" +
                                  $"</div>";

                mailText = mailText.Replace("##RequestNo##", request.header_id)
                                   .Replace("##ReceiverName##", receiverName)
                                   .Replace("##Subject##", $"IT Disposal Request - {request.department}")
                                   .Replace("##DueDate##", DateTime.Now.ToString("dd MMM yyyy"))
                                   .Replace("##UserNik##", username)
                                   .Replace("##HeadMessage##", headMessage)
                                   .Replace("##Link##", linkHtml);

                // TODO: Konfigurasi SMTP Host dan Port diletakkan di Web.config / AppSettings
                var smtp = new SmtpClient("TODO_SMTP_HOST", 587 /* TODO: SMTP_PORT */)
                {
                    EnableSsl = false, // TODO: Sesuaikan dengan environment security policy
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    // TODO: Tambahkan SMTP Credentials dari environment vars / web.config yang aman
                    // Credentials = new NetworkCredential("TODO_USERNAME", "TODO_PASSWORD") 
                };

                // TODO: Ambil email pengirim (sender email) dari Web.config / AppSettings
                using (var message = new MailMessage("TODO_SENDER_EMAIL", receivermail.Trim())
                {
                    Subject = $"[IT Disposal] {request.header_id} | {request.status}",
                    Body = mailText,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Send Error: " + ex.Message);
            }
        }

        private string GetEmailByNik(string nik)
        {
            var emp = db.V_Users_Active.FirstOrDefault(x => x.NIK.ToString() == nik);
            return emp?.Email ?? "";
        }

        private string GetNameByNik(string nik)
        {
            var emp = db.V_Users_Active.FirstOrDefault(x => x.NIK.ToString() == nik);
            return emp?.Name ?? "";
        }
    }
}