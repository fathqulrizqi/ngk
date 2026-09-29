using Microsoft.AspNet.Identity;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Security.Claims;
using System.Web.Mvc;
using System.Net;
using System.Net.Mail;

namespace NGKBusi.Areas.IT.Controllers
{
    public class ITWasteHandoverController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        ITWastehandoverConnection dbx = new ITWastehandoverConnection();
        ITDisposalConnection dbv = new ITDisposalConnection();

        [Authorize]
        public ActionResult Index()
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            ViewBag.DeptName = _currUser.FindFirstValue("deptName");

            string currentUserName = User.Identity.Name;
            var userRecord = db.V_Users_Active.FirstOrDefault(u => u.Name == currentUserName || u.Name == currentUserName);
            string currentUserNik = userRecord != null && userRecord.NIK != null ? userRecord.NIK.ToString() : string.Empty;

            // TODO: Ambil daftar NIK Checker dari database atau konfigurasi (App Settings)
            var checkerNiks = new List<string> { /* TODO: NIK Checker */ };

            ViewBag.IsChecker = checkerNiks.Contains(currentUserNik);

            var masterList = dbx.IT_Disposal_Master
                                .OrderByDescending(m => m.disposal_date)
                                .ToList();

            var listData = masterList.Select(m =>
            {
                var detail = dbx.IT_Wastehandover_Detail.FirstOrDefault(d => d.disposal_id == m.disposal_id);
                var latestApproval = dbx.IT_Wastehandover_Approval
                                        .Where(a => a.disposal_id == m.disposal_id)
                                        .OrderByDescending(a => a.approval_id)
                                        .FirstOrDefault();

                return new
                {
                    disposal_id = m.disposal_id,
                    header_id = m.disposal_ref_no,
                    department = m.department ?? "GENERAL",
                    items = detail != null ? detail.items : "-",
                    quantity = detail != null ? detail.quantity : 0,
                    unit = detail != null ? detail.unit : "unit",
                    asset_no = detail != null ? detail.asset_no : "-",
                    type = detail != null ? detail.type : "-",
                    condition = detail != null ? detail.condition : "-",
                    actions = detail != null ? detail.actions : "-",
                    confidentiality = detail != null ? detail.confidentiality : "-",
                    requester_name = latestApproval?.signed_by ?? "-",
                    created_date = m.disposal_date,
                    status = latestApproval?.status ?? m.status ?? "Pending"
                };
            }).ToList();

            ViewBag.WasteList = listData;
            ViewBag.NavHide = true;

            return View();
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
        public JsonResult DeleteWasteHandover(int id)
        {
            try
            {
                // 1. Cek apakah yang dikirim adalah ID Detail (IT_Wastehandover_Detail)
                var detailItem = dbx.IT_Wastehandover_Detail.FirstOrDefault(d => d.detail_id == id);
                if (detailItem != null)
                {
                    int masterId = detailItem.disposal_id;

                    // Hapus data detail
                    dbx.IT_Wastehandover_Detail.Remove(detailItem);
                    dbx.SaveChanges();

                    // Cek apakah masih ada detail lain dengan disposal_id yang sama
                    var remainingDetails = dbx.IT_Wastehandover_Detail.Where(d => d.disposal_id == masterId).ToList();
                    if (!remainingDetails.Any())
                    {
                        // Jika sudah tidak ada detail tersisa, hapus juga master dan approval-nya
                        var approvals = dbx.IT_Wastehandover_Approval.Where(a => a.disposal_id == masterId).ToList();
                        dbx.IT_Wastehandover_Approval.RemoveRange(approvals);

                        var masterItem = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == masterId);
                        if (masterItem != null)
                        {
                            dbx.IT_Disposal_Master.Remove(masterItem);
                        }
                        dbx.SaveChanges();
                    }

                    return Json(new { success = true, message = "Data berhasil dihapus." });
                }

                // 2. Fallback: Jika yang dikirim langsung ID Master (IT_Disposal_Master)
                var masterRecord = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == id);
                if (masterRecord != null)
                {
                    var details = dbx.IT_Wastehandover_Detail.Where(d => d.disposal_id == id).ToList();
                    dbx.IT_Wastehandover_Detail.RemoveRange(details);

                    var approvals = dbx.IT_Wastehandover_Approval.Where(a => a.disposal_id == id).ToList();
                    dbx.IT_Wastehandover_Approval.RemoveRange(approvals);

                    dbx.IT_Disposal_Master.Remove(masterRecord);
                    dbx.SaveChanges();

                    return Json(new { success = true, message = "Data berhasil dihapus." });
                }

                return Json(new { success = false, message = "Data not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveWasteHandover(IT_Disposal_Master model, List<IT_Wastehandover_Detail> Details)
        {
            using (var transaction = dbx.Database.BeginTransaction())
            {
                try
                {
                    if (Details == null || !Details.Any())
                    {
                        return Json(new { success = false, message = "Minimal harus ada 1 baris item." });
                    }

                    // Ambil informasi user yang sedang login untuk fallback nama lengkap
                    string currentUserName = User.Identity.Name;
                    var activeUserRecord = db.V_Users_Active.FirstOrDefault(u => u.Name == currentUserName);
                    string userFullName = activeUserRecord != null ? activeUserRecord.Name : currentUserName;

                    // Looping setiap baris detail yang diinput user
                    foreach (var detail in Details)
                    {
                        string rowCreator = !string.IsNullOrEmpty(detail.creator_name) ? detail.creator_name : currentUserName;

                        // Cari department user tersebut dari database
                        var userRecord = db.V_Users_Active.FirstOrDefault(u => u.Name == rowCreator);
                        string rowDept = userRecord != null && !string.IsNullOrEmpty(userRecord.DeptName) ? userRecord.DeptName : "GENERAL";

                        // ⚠️ 1. BUAT MASTER BARU TERLEBIH DAHULU AGAR newMaster.disposal_id TERSEDIA
                        var newMaster = new IT_Disposal_Master
                        {
                            disposal_ref_no = model.disposal_ref_no,
                            department = rowDept,
                            disposal_date = model.disposal_date == default(DateTime) ? DateTime.Now : model.disposal_date,
                            status = "Pending"
                        };

                        dbx.IT_Disposal_Master.Add(newMaster);
                        dbx.SaveChanges(); // Generate ID Master unik per baris

                        // ⚠️ 2. SETELAH newMaster DISIMPAN, BARU BUAT VARIABEL SEPERTI rowChecker
                        var checkerApproval = dbx.IT_Wastehandover_Approval
                                                 .FirstOrDefault(a => a.disposal_id == newMaster.disposal_id && a.status == "Checked");

                        string rowChecker = (checkerApproval != null && !string.IsNullOrEmpty(checkerApproval.signed_by))
                                            ? checkerApproval.signed_by
                                            : userFullName;

                        // Masukkan detail
                        detail.disposal_id = newMaster.disposal_id;
                        detail.header_id = !string.IsNullOrEmpty(newMaster.disposal_ref_no) ? newMaster.disposal_ref_no : "-";
                        detail.created_date = DateTime.Now;
                        detail.is_generated = false;
                        dbx.IT_Wastehandover_Detail.Add(detail);

                        // Buat approval log pertama
                        var initialApproval = new IT_Wastehandover_Approval
                        {
                            disposal_id = newMaster.disposal_id,
                            header_id = newMaster.disposal_ref_no ?? "-",
                            status = "Pending",
                            signed_by = rowCreator,
                            signed_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            message = "Created"
                        };
                        dbx.IT_Wastehandover_Approval.Add(initialApproval);

                        // ⚠️ 3. FITUR KIRIM EMAIL NOTIFIKASI KE SEMUA CHECKER (TO: CHECKER, CC: CREATOR)
                        try
                        {
                            // TODO: Ambil daftar NIK Checker dari database / konfigurasi
                            var checkerNiks = new List<string> { /* TODO: NIK Checker */ };
                            System.Diagnostics.Debug.WriteLine($"[EMAIL DEBUG] Mencari checker dengan NIK: {string.Join(", ", checkerNiks)}");

                            // GUNAKAN WHERE DAN TOLIST AGAR SEMUA CHECKER TERAMBIL
                            var checkerUsers = db.V_Users_Active.Where(u => checkerNiks.Contains(u.NIK.ToString())).ToList();

                            if (!checkerUsers.Any())
                            {
                                System.Diagnostics.Debug.WriteLine("[EMAIL WARNING] Tidak ada checker user yang ditemukan di database V_Users_Active.");
                            }
                            else
                            {
                                foreach (var checkerUser in checkerUsers)
                                {
                                    if (string.IsNullOrEmpty(checkerUser.Email))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[EMAIL WARNING] Checker user {checkerUser.Name} ditemukan, tetapi kolom email kosong.");
                                        continue;
                                    }

                                    // TODO: Ambil sender email dari konfigurasi (Web.config / AppSettings)
                                    string senderAddress = "TODO_SENDER_EMAIL";
                                    var senderEmail = new MailAddress(senderAddress, "IT Waste Handover Notification");

                                    // TO: Masing-masing Checker
                                    var receiverMail = new MailAddress(checkerUser.Email, checkerUser.Name);

                                    string subject = $"🔔 [IT Waste Handover] Menunggu Verifikasi - {newMaster.disposal_ref_no}";
                                    // TODO: Ambil base URL dari konfigurasi (Web.config / AppSettings)
                                    string targetUrl = "TODO_BASE_URL/IT/ITWasteHandover";

                                    string mailBody = $"<html><body>" +
                                                      $"Halo Tim Checker (<strong>{checkerUser.Name}</strong>),<br><br>" +
                                                      $"Terdapat pengajuan item limbah IT baru dari departemen <strong>{rowDept}</strong> (Dibuat oleh: <strong>{rowCreator}</strong>) yang memerlukan pengecekan dan verifikasi Anda.<br><br>" +
                                                      $"<strong>Detail Pengajuan:</strong><br>" +
                                                      $"- No. Referensi: {newMaster.disposal_ref_no}<br>" +
                                                      $"- Item: {detail.items}<br>" +
                                                      $"- No Asset: {detail.asset_no}<br>" +
                                                      $"- Kuantitas: {detail.quantity} {detail.unit}<br>" +
                                                      $"Silakan klik tombol di bawah ini untuk membuka portal dan melakukan proses <em>Check</em>:<br><br>" +
                                                      $"<div style=\"margin: 20px 0;\">" +
                                                      $"<a href=\"{targetUrl}\" target=\"_blank\" style=\"background-color: #17a2b8; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;\">Cek IT Waste Handover</a>" +
                                                      $"</div>" +
                                                      $"Terima kasih,<br>" +
                                                      $"<strong>IT System Notification</strong>" +
                                                      $"</body></html>";

                                    // TODO: Konfigurasi SMTP diletakkan di Web.config / AppSettings
                                    string smtpHost = "TODO_SMTP_HOST";
                                    int smtpPort = 587; // TODO: Sesuaikan Port SMTP

                                    System.Diagnostics.Debug.WriteLine($"[EMAIL DEBUG] Mengirim email ke checker: {checkerUser.Email}...");

                                    using (var smtp = new SmtpClient(smtpHost, smtpPort))
                                    {
                                        smtp.EnableSsl = false; // TODO: Sesuaikan dengan environment security policy
                                        smtp.UseDefaultCredentials = true;
                                        // TODO: Tambahkan SMTP Credentials dari environment vars / web.config jika diperlukan
                                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                                        using (var message = new MailMessage(senderEmail, receiverMail)
                                        {
                                            Subject = subject,
                                            Body = mailBody,
                                            IsBodyHtml = true
                                        })
                                        {
                                            // Masukkan Creator ke CC untuk setiap email yang dikirim ke checker
                                            if (userRecord != null && !string.IsNullOrEmpty(userRecord.Email))
                                            {
                                                message.CC.Add(new MailAddress(userRecord.Email, rowCreator));
                                                System.Diagnostics.Debug.WriteLine($"[EMAIL DEBUG] CC ditambahkan ke Creator: {userRecord.Email}");
                                            }

                                            smtp.Send(message);
                                            System.Diagnostics.Debug.WriteLine($"[EMAIL SUCCESS] Email berhasil dikirim ke checker {checkerUser.Email}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception emailEx)
                        {
                            string emailErrorMsg = emailEx.InnerException != null ? (emailEx.InnerException.InnerException?.Message ?? emailEx.InnerException.Message) : emailEx.Message;
                            System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] Gagal kirim email notifikasi: {emailErrorMsg}");
                        }
                    }

                    dbx.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Data berhasil disimpan." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    string errorMsg = ex.InnerException != null ? (ex.InnerException.InnerException?.Message ?? ex.InnerException.Message) : ex.Message;
                    return Json(new { success = false, message = errorMsg });
                }
            }
        }

        [HttpGet]
        public ActionResult PrintWastePdf(string ids, string dateFrom, string dateTo)
        {
            using (var db = new NGKBusi.Areas.IT.Models.ITWastehandoverConnection())
            {
                // Gunakan .Include untuk menarik data master dan approval sekaligus secara aman
                var query = db.IT_Wastehandover_Detail
                              .Include("IT_Disposal_Master")
                              .AsQueryable();

                if (!string.IsNullOrEmpty(ids))
                {
                    var idList = ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(int.Parse)
                                    .ToList();

                    if (idList.Any())
                    {
                        query = query.Where(d => idList.Contains(d.detail_id));
                    }
                }
                else
                {
                    DateTime parsedDateFrom, parsedDateTo;
                    if (DateTime.TryParse(dateFrom, out parsedDateFrom))
                    {
                        query = query.Where(d => d.created_date >= parsedDateFrom);
                    }
                    if (DateTime.TryParse(dateTo, out parsedDateTo))
                    {
                        var maxDate = parsedDateTo.AddDays(1).AddSeconds(-1);
                        query = query.Where(d => d.created_date <= maxDate);
                    }
                }

                var list = query.ToList();

                // Ambil data approval dalam satu list terpisah untuk menghindari query berulang dalam loop
                var disposalIds = list.Select(x => (int?)x.disposal_id).Distinct().ToList();
                var approvals = db.IT_Wastehandover_Approval
                                  .Where(a => a.disposal_id != null && disposalIds.Contains(a.disposal_id))
                                  .ToList();

                foreach (var d in list)
                {
                    // Cari approval yang cocok berdasarkan disposal_id secara aman di memory
                    var approval = approvals.FirstOrDefault(a => a.disposal_id == d.disposal_id);

                    d.creator_name = (approval != null && !string.IsNullOrEmpty(approval.signed_by))
                                   ? approval.signed_by
                                   : "-";
                }

                ViewBag.PdfData = list;
                ViewBag.DateFrom = string.IsNullOrEmpty(dateFrom) ? "-" : dateFrom;
                ViewBag.DateTo = string.IsNullOrEmpty(dateTo) ? "-" : dateTo;

                return View("PrintPreview");
            }
        }

        public ActionResult Edit(int id)
        {
            var master = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == id);
            if (master == null) return HttpNotFound();

            var details = dbx.IT_Wastehandover_Detail.Where(d => d.disposal_id == id).ToList();
            var latestApproval = dbx.IT_Wastehandover_Approval
                                    .Where(a => a.disposal_id == id)
                                    .OrderByDescending(a => a.approval_id)
                                    .FirstOrDefault();

            ViewBag.IsChecker = IsAuthorized("Check");
            ViewBag.IsApprover = IsAuthorized("Approve");
            ViewBag.IsCreator = true;

            ViewBag.Master = master;
            ViewBag.DisposalId = master.disposal_id;
            ViewBag.HeaderId = master.disposal_ref_no;
            ViewBag.Details = details;
            ViewBag.Status = latestApproval?.status ?? master.status ?? "Draft";
            ViewBag.CreatedDate = master.disposal_date;
            ViewBag.DeptName = master.department;

            return View();
        }

        // POST: ITWasteHandover/Edit
        [Authorize]
        [HttpPost]
        public JsonResult Edit(int disposal_id, string department, string date, List<IT_Wastehandover_Detail> details)
        {
            try
            {
                var master = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == disposal_id);
                if (master == null)
                {
                    return Json(new { success = false, message = "Data master tidak ditemukan." });
                }

                master.department = department;
                if (DateTime.TryParse(date, out DateTime parsedDate))
                {
                    master.disposal_date = parsedDate;
                }

                // Hapus detail lama yang berelasi
                var oldDetails = dbx.IT_Wastehandover_Detail.Where(d => d.disposal_id == disposal_id).ToList();
                dbx.IT_Wastehandover_Detail.RemoveRange(oldDetails);

                // Masukkan kembali detail yang diperbarui dari form
                if (details != null)
                {
                    foreach (var item in details)
                    {
                        item.disposal_id = disposal_id;
                        item.header_id = master.disposal_ref_no;
                        item.created_date = parsedDate;
                        dbx.IT_Wastehandover_Detail.Add(item);
                    }
                }

                dbx.SaveChanges();
                return Json(new { success = true, message = "Data berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.InnerException?.Message ?? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = errorMsg });
            }
        }

        [HttpPost]
        public JsonResult SignWasteHandover(int id)
        {
            try
            {
                var currentUserName = User.Identity.Name;

                var detail = dbx.IT_Wastehandover_Detail.FirstOrDefault(d => d.detail_id == id);
                if (detail == null)
                {
                    return Json(new { success = false, message = "Data Waste Handover tidak ditemukan." });
                }

                var master = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == detail.disposal_id);

                var firstApproval = dbx.IT_Wastehandover_Approval
                                        .Where(a => a.disposal_id == detail.disposal_id)
                                        .OrderBy(a => a.approval_id)
                                        .FirstOrDefault();

                string creatorName = firstApproval?.signed_by ?? detail.creator_name;

                bool isTheRightCreator = !string.IsNullOrEmpty(creatorName) &&
                                         creatorName.Trim().Equals(currentUserName.Trim(), StringComparison.OrdinalIgnoreCase);

                if (!isTheRightCreator)
                {
                    return Json(new { success = false, message = "Access Denied: Anda bukan creator yang terdaftar untuk pengajuan ini." });
                }

                if (master != null)
                {
                    master.status = "SignedByCreator";

                    var signLog = new IT_Wastehandover_Approval
                    {
                        header_id = detail.header_id,
                        disposal_id = detail.disposal_id,
                        status = "SignedByCreator",
                        signed_by = currentUserName,
                        signed_date = DateTime.Now.ToString("yyyy-MM-dd"),
                        message = "Signed by Creator"
                    };
                    dbx.IT_Wastehandover_Approval.Add(signLog);
                    dbx.SaveChanges();
                }

                return Json(new { success = true, message = "Dokumen berhasil di-sign oleh Creator!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateStatus(int id, string newStatus, string message)
        {
            try
            {
                // 'id' adalah detail_id yang dikirim dari tombol Check per baris
                var detail = dbx.IT_Wastehandover_Detail.FirstOrDefault(d => d.detail_id == id);
                if (detail == null)
                    return Json(new { success = false, message = "Detail data not found." });

                // Ambil Master unik yang khusus memuat item ini
                var master = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == detail.disposal_id);
                if (master == null)
                    return Json(new { success = false, message = "Master data not found." });

                // Update status hanya pada master milik item tersebut
                master.status = newStatus;

                // ⚠️ JANGAN MENAMBAH BARIS APPROVAL BARU YANG MENIMPA NAMA PEMBUAT (signed_by)
                // Cukup update status pada baris approval pertama (creator asli tetap aman)
                var firstApproval = dbx.IT_Wastehandover_Approval
                                        .Where(a => a.disposal_id == master.disposal_id)
                                        .OrderBy(a => a.approval_id)
                                        .FirstOrDefault();

                if (firstApproval != null)
                {
                    firstApproval.status = newStatus; // Update status-nya saja tanpa mengubah signed_by pembuat asli
                }

                dbx.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? (ex.InnerException.InnerException?.Message ?? ex.InnerException.Message) : ex.Message;
                return Json(new { success = false, message = errorMsg });
            }
        }
        [HttpPost]
        [Authorize]
        public JsonResult UpdateWasteHandover(int disposal_id, string department, string date, List<IT_Wastehandover_Detail> details)
        {
            using (var transaction = dbx.Database.BeginTransaction())
            {
                try
                {
                    var master = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == disposal_id);
                    if (master == null)
                    {
                        return Json(new { success = false, message = "Data master tidak ditemukan." });
                    }

                    // Update Master
                    master.department = department;
                    if (DateTime.TryParse(date, out DateTime parsedDate))
                    {
                        master.disposal_date = parsedDate;
                    }

                    // Hapus detail lama yang berelasi dengan disposal_id ini
                    var oldDetails = dbx.IT_Wastehandover_Detail.Where(d => d.disposal_id == disposal_id).ToList();
                    dbx.IT_Wastehandover_Detail.RemoveRange(oldDetails);

                    // Masukkan kembali detail yang baru dari form edit
                    if (details != null && details.Any())
                    {
                        foreach (var item in details)
                        {
                            item.disposal_id = disposal_id;
                            item.header_id = !string.IsNullOrEmpty(master.disposal_ref_no) ? master.disposal_ref_no : "-";
                            item.created_date = parsedDate;
                            item.is_generated = false;
                            dbx.IT_Wastehandover_Detail.Add(item);
                        }
                    }

                    dbx.SaveChanges();
                    transaction.Commit();
                    return Json(new { success = true, message = "Data berhasil diperbarui!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    string errorMsg = ex.InnerException != null ? (ex.InnerException.InnerException?.Message ?? ex.InnerException.Message) : ex.Message;
                    return Json(new { success = false, message = errorMsg });
                }
            }
        }

        [HttpPost]
        public JsonResult SendToITDisposal(List<int> selectedIds)
        {
            using (var transactionDbx = dbx.Database.BeginTransaction())
            using (var transactionDbv = dbv.Database.BeginTransaction())
            {
                try
                {
                    if (selectedIds == null || selectedIds.Count == 0)
                    {
                        return Json(new { success = false, message = "Tidak ada data yang dipilih." });
                    }

                    // =========================================================================
                    // 1. GENERATE HEADER ID DENGAN FORMAT (disp26-001)
                    // =========================================================================
                    string currentYear = DateTime.Now.ToString("yy"); // Menghasilkan "26"
                    string prefix = $"DISP{currentYear}-"; // Menghasilkan "disp26-"

                    // Cari nomor dokumen terakhir di database (Tabel IT_Disposal_Header) yang diawali dengan "disp26-"
                    var lastDocument = dbv.IT_Disposal_Header
                                          .Where(h => h.header_id.StartsWith(prefix))
                                          .OrderByDescending(h => h.header_id)
                                          .FirstOrDefault();

                    int nextSeq = 1;
                    if (lastDocument != null && !string.IsNullOrEmpty(lastDocument.header_id))
                    {
                        // Ambil bagian angka di belakang "disp26-"
                        string lastSeqStr = lastDocument.header_id.Substring(prefix.Length);
                        if (int.TryParse(lastSeqStr, out int lastSeqNumber))
                        {
                            nextSeq = lastSeqNumber + 1; // Tambah 1 dari nomor terakhir
                        }
                    }

                    // Gabungkan prefix dengan nomor urut yang diformat 3 digit (001, 002, 003, dst)
                    string generatedHeaderId = prefix + nextSeq.ToString("D3");

                    // =========================================================================
                    // 2. CREATE HEADER IT DISPOSAL
                    // =========================================================================
                    var newDisposalHeader = new IT_Disposal_Header
                    {
                        header_id = generatedHeaderId,
                        status = "Draft",
                        department = "IT",
                        requester_nik = User.Identity.Name,
                        requester_name = User.Identity.Name,
                        ApprovedDate = null,
                        CheckedDate = null
                    };
                    dbv.IT_Disposal_Header.Add(newDisposalHeader);

                    // =========================================================================
                    // 3. LOOPING DATA DETAIL YANG DIPILIH
                    // =========================================================================
                    foreach (var detailId in selectedIds)
                    {
                        var detail = dbx.IT_Wastehandover_Detail.FirstOrDefault(d => d.detail_id == detailId);

                        if (detail != null)
                        {
                            // --- A. CREATE DETAIL IT DISPOSAL ---
                            var newDisposalDetail = new IT_Disposal_Detail
                            {
                                header_id = generatedHeaderId,
                                quantity = detail.quantity,
                                unit = detail.unit,
                                asset_no = detail.asset_no,
                                items = detail.items,
                                type = detail.type,
                                condition = detail.condition,
                                actions = detail.actions,
                                confidentiality = detail.confidentiality,
                                created_date = DateTime.Now
                            };
                            dbv.IT_Disposal_Detail.Add(newDisposalDetail);

                            // --- B. CREATE RELATION ---
                            string strDisposalId = generatedHeaderId;
                            string strDetailId = detail.detail_id.ToString();

                            var existingRelation = dbx.IT_Disp_WH_Relation
                                .FirstOrDefault(r => r.disposal_id == strDisposalId && r.waste_handover_id == strDetailId);

                            if (existingRelation == null)
                            {
                                var relationRecord = new IT_Disp_WH_Relation
                                {
                                    disposal_id = strDisposalId,
                                    waste_handover_id = strDetailId
                                };
                                dbx.IT_Disp_WH_Relation.Add(relationRecord);
                            }

                            // --- C. UPDATE MASTER WASTE HANDOVER STATUS ---
                            var master = dbx.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == detail.disposal_id);
                            if (master != null)
                            {
                                master.status = "Sent to IT Disposal";
                            }

                            // --- D. CREATE APPROVAL LOG ---
                            var approvalLog = new IT_Wastehandover_Approval
                            {
                                disposal_id = detail.disposal_id,
                                header_id = master?.disposal_ref_no ?? "-",
                                status = "Sent to IT Disposal",
                                message = "Data successfully sent to IT Disposal with Document No: " + generatedHeaderId,
                                signed_by = User.Identity.Name,
                                signed_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            };
                            dbx.IT_Wastehandover_Approval.Add(approvalLog);
                        }
                    }

                    // =========================================================================
                    // 4. SAVE CHANGES & COMMIT
                    // =========================================================================
                    dbv.SaveChanges();
                    dbx.SaveChanges();

                    transactionDbv.Commit();
                    transactionDbx.Commit();

                    return Json(new { success = true, message = $"Data berhasil dikirim dengan ID Dokumen: {generatedHeaderId}" });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    transactionDbx.Rollback();
                    transactionDbv.Rollback();
                    string errorMessages = "Validation Error: ";
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            errorMessages += $"{validationError.PropertyName}: {validationError.ErrorMessage} | ";
                        }
                    }
                    return Json(new { success = false, message = errorMessages });
                }
                catch (Exception ex)
                {
                    transactionDbx.Rollback();
                    transactionDbv.Rollback();

                    string errorMsg = ex.InnerException != null ? (ex.InnerException.InnerException?.Message ?? ex.InnerException.Message) : ex.Message;
                    return Json(new { success = false, message = errorMsg });
                }
            }
        }
        [HttpGet]
        public JsonResult GetActiveUsers()
        {
            var users = db.V_Users_Active
                          .Select(u => new
                          {
                              nik = u.NIK,
                              name = u.Name,
                              department = u.DeptName
                          })
                          .OrderBy(u => u.name)
                          .ToList();
            return Json(users, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetHistoryWasteHandover(string dateFrom, string dateTo)
        {
            using (var db = new NGKBusi.Areas.IT.Models.ITWastehandoverConnection())
            {
                var query = db.IT_Wastehandover_Detail.AsQueryable();

                DateTime parsedDateFrom, parsedDateTo;
                if (DateTime.TryParse(dateFrom, out parsedDateFrom))
                {
                    query = query.Where(d => d.created_date >= parsedDateFrom);
                }
                if (DateTime.TryParse(dateTo, out parsedDateTo))
                {
                    var maxDate = parsedDateTo.AddDays(1).AddSeconds(-1);
                    query = query.Where(d => d.created_date <= maxDate);
                }

                var list = query.ToList();

                var data = list.Select(d => {
                    var master = db.IT_Disposal_Master.FirstOrDefault(m => m.disposal_id == d.disposal_id);

                    var approval = db.IT_Wastehandover_Approval.FirstOrDefault(a => a.disposal_id == d.disposal_id);

                    return new
                    {
                        detail_id = d.detail_id,
                        disposal_id = d.disposal_id,
                        items = d.items ?? "-",
                        quantity = d.quantity,
                        unit = d.unit ?? "-",
                        asset_no = d.asset_no ?? "-",
                        type = d.type ?? "-",
                        condition = d.condition ?? "-",
                        actions = d.actions ?? "-",
                        confidentiality = d.confidentiality ?? "-",
                        requester_name = (approval != null && !string.IsNullOrEmpty(approval.signed_by)) ? approval.signed_by : "-",
                        department = (master != null && !string.IsNullOrEmpty(master.department)) ? master.department : "-",
                        created_date = d.created_date.ToString("yyyy-MM-dd"),
                        status = (master != null && !string.IsNullOrEmpty(master.status)) ? master.status : "-"
                    };
                }).ToList();

                return Json(new { data = data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetWasteHandoverHeaders(string dateFrom, string dateTo)
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            string deptName = _currUser.FindFirstValue("deptName") ?? "";
            string currentUserName = User.Identity.Name;

            // Cek apakah user dari Departemen IT
            bool isItDept = deptName.IndexOf("IT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            deptName.IndexOf("Information Technology", StringComparison.OrdinalIgnoreCase) >= 0;

            // Ambil nama lengkap user yang login dari V_Users_Active jika diperlukan pencocokan nama
            var activeUserRecord = db.V_Users_Active.FirstOrDefault(u => u.Name == currentUserName || u.Name == currentUserName);
            string userFullName = activeUserRecord != null ? activeUserRecord.Name : currentUserName;

            string currentUserNik = activeUserRecord != null && activeUserRecord.NIK != null ? activeUserRecord.NIK.ToString() : string.Empty;

            // TODO: Ambil daftar NIK Checker dan Creator dari database atau konfigurasi (App Settings)
            var checkerNiks = new List<string> { /* TODO: NIK Checker */ };
            var creatorNiks = new List<string> { /* TODO: NIK Creator */ };

            bool canCheck = checkerNiks.Contains(currentUserNik);
            bool canDelete = creatorNiks.Contains(currentUserNik);

            var query = dbx.IT_Disposal_Master.AsQueryable();

            // Filter berdasarkan Date Range jika diisi
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime parsedFrom))
            {
                query = query.Where(m => m.disposal_date >= parsedFrom);
            }
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime parsedTo))
            {
                var endOfDay = parsedTo.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(m => m.disposal_date <= endOfDay);
            }

            var masterList = query.OrderByDescending(m => m.disposal_date)
                      .ThenByDescending(m => m.disposal_id)
                      .ToList();

            var data = new List<object>();

            foreach (var m in masterList)
            {
                var details = dbx.IT_Wastehandover_Detail
                               .Where(d => d.disposal_id == m.disposal_id && !d.is_generated)
                               .ToList();

                if (!details.Any()) continue;

                var firstApproval = dbx.IT_Wastehandover_Approval
                                       .Where(a => a.disposal_id == m.disposal_id)
                                       .OrderBy(a => a.approval_id)
                                       .FirstOrDefault();

                // Ambil approval untuk checker (status 'Checked')
                var checkerApproval = dbx.IT_Wastehandover_Approval
                                         .Where(a => a.disposal_id == m.disposal_id && a.status == "Checked")
                                         .OrderByDescending(a => a.approval_id)
                                         .FirstOrDefault();

                string currentStatus = dbx.IT_Wastehandover_Approval
                                          .Where(a => a.disposal_id == m.disposal_id)
                                          .OrderByDescending(a => a.approval_id)
                                          .FirstOrDefault()?.status ?? (m.status ?? "Pending");

                string creatorName = (firstApproval != null && !string.IsNullOrEmpty(firstApproval.signed_by))
                                     ? firstApproval.signed_by
                                     : "-";

                string checkerName = (checkerApproval != null && !string.IsNullOrEmpty(checkerApproval.signed_by))
                                     ? checkerApproval.signed_by
                                     : "-";

                // ⚠️ ATURAN 1: Jika bukan Dept IT, hanya muncul data yang Name-nya sesuai dengan loginannya
                if (!isItDept && !creatorName.Equals(userFullName, StringComparison.OrdinalIgnoreCase) && !creatorName.Equals(currentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                bool isRowCreator = !string.IsNullOrEmpty(creatorName) &&
                        (creatorName.Equals(userFullName, StringComparison.OrdinalIgnoreCase) ||
                         creatorName.Equals(currentUserName, StringComparison.OrdinalIgnoreCase));

                foreach (var d in details)
                {
                    data.Add(new
                    {
                        disposal_id = m.disposal_id,
                        detail_id = d.detail_id,
                        header_id = m.disposal_ref_no ?? "-",
                        department = m.department ?? "GENERAL",
                        items = d.items ?? "-",
                        quantity = d.quantity,
                        unit = d.unit ?? "unit",
                        asset_no = d.asset_no ?? "-",
                        type = d.type ?? "-",
                        condition = d.condition ?? "-",
                        actions = d.actions ?? "-",
                        confidentiality = d.confidentiality ?? "-",
                        requester_name = creatorName,
                        checker_name = checkerName, // Ditambahkan di sini agar terkirim ke view
                        created_date = m.disposal_date,
                        status = currentStatus,
                        canCheck = canCheck,
                        canDelete = canDelete,
                        canSign = isRowCreator
                    });
                }
            }

            // Tambahkan "SignedByCreator" ke dalam kondisi filter agar tidak hilang
            var filteredData = data.Cast<dynamic>()
                                   .Where(x => x.status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                                               x.status.Equals("Checked", StringComparison.OrdinalIgnoreCase) ||
                                               x.status.Equals("SignedByCreator", StringComparison.OrdinalIgnoreCase))
                                   .ToList();

            return Json(new { data = filteredData }, JsonRequestBehavior.AllowGet);
        }

    }
}