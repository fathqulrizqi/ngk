
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.Production.Models;
using NGKBusi.Models;
using NGKBusi.SignalR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.Production.Controllers
{
    public class TransferLaborController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        TransferLaborConnection dbt = new TransferLaborConnection();
        WebIReporterConnection dbi = new WebIReporterConnection();

        public ActionResult Index()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            ViewBag.CurrentNIK = userNik;

            var userAccessFromDb = dbt.Production_TransferLabor_Approval_Access
                                      .Where(a => a.nik == userNik)
                                      .Select(a => new SectionDropdownDTO
                                      {
                                          Section = a.section,
                                          Department = a.department
                                      })
                                      .ToList();

            var userAccessRaw = userAccessFromDb.SelectMany(a =>
            {
                if (a.Section == "SP PD - ASSEMBLY")
                {
                    return new[] {
                new SectionDropdownDTO { Section = "SP PD - ASSEMBLY (ASSY LINE)", Department = a.Department },
                new SectionDropdownDTO { Section = "SP PD - ASSEMBLY (TALC POWDER)", Department = a.Department }
            };
                }
                else if (a.Section == "PC PD - ASSEMBLY")
                {
                    return new[] {
                new SectionDropdownDTO { Section = "PC PD - ASSEMBLY (PLUG CAP)", Department = a.Department },
                new SectionDropdownDTO { Section = "PC PD - ASSEMBLY (INSPECTION PLUG CAP)", Department = a.Department }
            };
                }
                return new[] { a };
            })
            .GroupBy(x => new { x.Department, x.Section })
            .Select(g => g.FirstOrDefault())
            .ToList();

            int jumlahAkses = userAccessRaw.Count;

            if (jumlahAkses == 0)
            {
                return View("V403");
            }

            ViewBag.UserAccess = userAccessRaw;
            var descriptions = dbi.absensi_list_descriptions
                .Select(x => new SelectListDescription
                {
                    CodeDesc = x.Code,          
                    TextDesc = x.Description    
                })
                .ToList();

            ViewBag.ListDescription = descriptions;

            var allSectionsFromDb = dbt.Production_TransferLabor_Approval_Access
                                       .Select(a => new SectionDropdownDTO
                                       {
                                           Section = a.section,
                                           Department = a.department
                                       })
                                       .ToList();

            var allSectionsRaw = allSectionsFromDb.SelectMany(a =>
            {
                if (a.Section == "SP PD - ASSEMBLY")
                {
                    return new[] {
                new SectionDropdownDTO { Section = "SP PD - ASSEMBLY (ASSY LINE)", Department = a.Department },
                new SectionDropdownDTO { Section = "SP PD - ASSEMBLY (TALC POWDER)", Department = a.Department }
            };
                }
                else if (a.Section == "PC PD - ASSEMBLY")
                {
                    return new[] {
                new SectionDropdownDTO { Section = "PC PD - ASSEMBLY (PLUG CAP)", Department = a.Department },
                new SectionDropdownDTO { Section = "PC PD - ASSEMBLY (INSPECTION PLUG CAP)", Department = a.Department }
            };
                }
                return new[] { a };
            })

            .GroupBy(x => new { x.Department, x.Section })
            .Select(g => g.FirstOrDefault())
            .OrderBy(o => o.Department).ThenBy(o => o.Section)
            .ToList();

            ViewBag.AllSections = allSectionsRaw;


            var allowedSections = userAccessRaw.Select(a => a.Section).ToList();

            ViewBag.items = dbt.Production_TransferLabor_Request_Header
                               .Where(h => allowedSections.Contains(h.from_section) ||
                                           allowedSections.Contains(h.to_section))
                               .OrderByDescending(o => o.start_date)
                               .ToList();

            return View();
        }

        [HttpGet]
        public JsonResult getManpower(string section)
        {
            try
            {
                DateTime today = DateTime.Today;
                string subSection = null;

                // --- TAMBAHAN BARU: Pisahkan Section dan Department ---
                // Jika parameter section tidak kosong dan mengandung karakter '|'
                if (!string.IsNullOrEmpty(section) && section.Contains("|"))
                {
                    // Ambil bagian kiri sebelum '|' (yaitu nama Section-nya saja)
                    section = section.Split('|')[0];
                }
                // ------------------------------------------------------

                // 1. Cek apakah section memiliki nilai dan mengandung tanda kurung ()
                if (!string.IsNullOrEmpty(section) && section.Contains("(") && section.Contains(")"))
                {
                    int startIndex = section.IndexOf('(');
                    int endIndex = section.IndexOf(')');

                    // 2. Ambil nilai HANYA yang ada di dalam kurung
                    subSection = section.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

                    // 3. Potong nama section utama (buang spasi dan kurungnya)
                    section = section.Substring(0, startIndex).Trim();
                }

                using (var ireporterDb = new WebIReporterConnection())
                {
                    // 4. Buat query dasar (tanpa SubSection)
                    var query = ireporterDb.Absensi
                        .Where(x => x.SectionName == section
                                 && x.Date.HasValue
                                 && DbFunctions.TruncateTime(x.Date) == today);

                    // 5. Tambahkan kondisi WHERE secara dinamis jika subSection memiliki isi
                    if (!string.IsNullOrEmpty(subSection))
                    {
                        // PERHATIAN: Sesuaikan "x.SubSectionName" dengan nama kolom yang sebenarnya 
                        // di dalam tabel/class Absensi Anda (misal: x.Line, x.SubSection, dll)
                        query = query.Where(x => x.SubSection == subSection);
                    }

                    var manpowerList = query
                        .Select(x => new
                        {
                            NIK = x.NIK,
                            Name = x.Name,
                            Description = x.Description,
                            Status = x.Status
                        })
                        .ToList();

                    return Json(new { success = true, data = manpowerList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Letakkan di level class Controller, bukan di dalam method createRequest
        private static readonly Dictionary<string, string> sectionCodeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SP PD - COLD FORMING", "CF" },
            { "SP PD - CHUCKING MACHINE", "CM" },
            { "SP PD - WELDING THREADING", "WT" },
            { "SP PD - PLATING", "PL" },
            { "SP PD - INSULATOR SEALING", "IS" },
            { "SP PD - ASSEMBLY (ASSY LINE)", "AA" },
            { "SP PD - ASSEMBLY (TALC POWDER)", "AA" },
            { "SP PD - ASSEMBLY", "AA" },
            { "SP PD - BENDING", "BD" },
            { "SP PD - FINAL INSPECTION", "FI" },
            { "PC PD - INJECTION MOLDING", "IM" },
            { "PC PD - ASSEMBLY (INSPECTION PLUG CAP)", "PA" },
            { "PC PD - ASSEMBLY (PLUG CAP)", "PA" },
            { "PC PD - ASSEMBLY", "PA" },
            { "PC PD - PACKAGING", "PK" },
            { "QUALITY CONTROL", "QC" },
            { "QUALITY ASSURANCE", "QA" },
            { "PRODUCTION ENGINEERING", "PE" },
            { "MAINTENANCE", "MT" },
            { "UTILITY", "UT" },
            { "PRODUCTION OFFICE", "PO" },
            { "PRODUCTION PLAN", "PP" }
        };

        [HttpPost]
        public JsonResult createRequest(TransferLaborRequestVM input)
        {
            try
            {
                if (input == null || input.nik == null || input.nik.Count == 0)
                {
                    return Json(new { success = false, message = "Data request tidak valid atau manpower kosong." });
                }

                string jobCode = "";
                string jobDetailName = "";
                string toSection = "";
                string toDepartment = "";
                string fromSection = "";
                string fromDepartment = "";

                // 2. Parsing Input dengan aman
                if (!string.IsNullOrEmpty(input.job_detail) && input.job_detail.Contains("|"))
                {
                    string[] parts = input.job_detail.Split('|');
                    jobCode = parts[0].Trim();
                    if (parts.Length > 1) jobDetailName = parts[1].Trim();
                }

                if (!string.IsNullOrEmpty(input.to_section) && input.to_section.Contains("|"))
                {
                    string[] parts = input.to_section.Split('|');
                    toSection = parts[0].Trim();
                    if (parts.Length > 1) toDepartment = parts[1].Trim();
                }

                if (!string.IsNullOrEmpty(input.from_section) && input.from_section.Contains("|"))
                {
                    string[] parts = input.from_section.Split('|');
                    fromSection = parts[0].Trim();
                    if (parts.Length > 1) fromDepartment = parts[1].Trim();
                }

                // 3. Generate Header ID dari Dictionary
                string fromCode = sectionCodeMap.ContainsKey(fromSection) ? sectionCodeMap[fromSection] : "XX";
                string toCode = sectionCodeMap.ContainsKey(toSection) ? sectionCodeMap[toSection] : "XX";

                // Hasilnya misalnya: TL-CF-QA-20231027-143000
                string newHeaderId = $"TL{fromCode}{toCode}-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                bool isSameDepartment = fromDepartment.Equals(toDepartment, StringComparison.OrdinalIgnoreCase);

                // 4. Mapping data ke Model Header
                var header = new Production_TransferLabor_Request_Header
                {
                    id = newHeaderId,
                    start_date = input.start_date,
                    end_date = input.end_date,
                    from_section = fromSection,
                    from_department = fromDepartment,
                    to_section = toSection,
                    to_department = toDepartment,
                    request_creator_by = input.request_creator_by,
                    request_creator_sign = input.request_creator_sign,
                    job_detail = jobDetailName,
                    code_job_detail = jobCode,
                    request_creator_sign_date = input.request_creator_sign_date,
                    status = isSameDepartment ? "COMPLETED" : "WAITING REQUESTOR MANAGER",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                    created_by = input.created_by
                };
                if (isSameDepartment)
                {
                    header.request_approval_by = "SYSTEM";
                    header.request_approval_sign = "APPROVED";
                    header.request_approval_sign_date = DateTime.Now;

                    header.provider_issued_by = "SYSTEM";
                    header.provider_issued_sign = "CHECKED";
                    header.provider_issued_sign_date = DateTime.Now;

                    header.provider_approval_by = "SYSTEM";
                    header.provider_approval_sign = "APPROVED";
                    header.provider_approval_sign_date = DateTime.Now;
                }
                // 5. Mapping data ke Model Detail
                var details = new List<Production_TransferLabor_Request_Detail>();
                for (int i = 0; i < input.nik.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(input.nik[i])) continue;

                    float tlHours = input.transfer_labor_hours != null && input.transfer_labor_hours.Count > i ? input.transfer_labor_hours[i] : 0;
                    float otHours = input.transfer_overtime_hours != null && input.transfer_overtime_hours.Count > i ? input.transfer_overtime_hours[i] : 0;
                    string empPlanning = input.planning != null && input.planning.Count > i ? input.planning[i] : "";
                    var detail = new Production_TransferLabor_Request_Detail
                    {
                        referal_id = newHeaderId,
                        nik = input.nik[i],
                        name = input.name[i],
                        description = input.description[i],
                        status = input.status[i],
                        transfer_labor_hours = tlHours,
                        transfer_overtime_hours = otHours,
                        total_transfer_hours = tlHours + otHours,
                        planning = empPlanning
                    };

                    details.Add(detail);
                }

                // 6. Simpan ke Database
                dbt.Production_TransferLabor_Request_Header.Add(header);
                dbt.Production_TransferLabor_Request_Detail.AddRange(details);
                dbt.SaveChanges();

                string errorReason = "";

                if (isSameDepartment)
                {
                    var inject = InjectTransferLabor(header);
                    // ==========================================
                    // LOGIKA EMAIL NOTIFIKASI (AUTO-APPROVE)
                    // ==========================================
                    var notificationTargets = dbt.Production_TransferLabor_Approval_Access
                        .Where(h => (h.section == header.from_section || h.section == header.to_section) && (h.access == "APPROVER" || h.access == "CHECKER"))
                        .Select(p => p.nik)
                        .Distinct()
                        .ToList();

                    foreach (var targetNik in notificationTargets)
                    {
                        if (!string.IsNullOrEmpty(targetNik))
                        {
                            SendEmailTransferLabor(header, targetNik, out errorReason);
                        }
                    }
                }
                else
                {
                    // ==========================================
                    // LOGIKA EMAIL APPROVAL (LINTAS DEPARTEMEN)
                    // ==========================================
                    var requestorApprovalNiks = dbt.Production_TransferLabor_Approval_Access
                        .Where(h => h.section == header.from_section && h.access == "APPROVER")
                        .Select(p => p.nik)
                        .Distinct()
                        .ToList();

                    foreach (var targetNik in requestorApprovalNiks)
                    {
                        if (!string.IsNullOrEmpty(targetNik))
                        {
                            SendEmailTransferLabor(header, targetNik, out errorReason);
                        }
                    }
                }

                // 7. Kembalikan response sukses
                return Json(new { success = true, message = "Formulir Transfer Labor berhasil disimpan!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan server: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult updateRequest(string id, TransferLaborRequestVM input)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, message = "ID Request tidak ditemukan." });
                }

                if (input == null || input.nik == null || input.nik.Count == 0)
                {
                    return Json(new { success = false, message = "Data request tidak valid atau manpower kosong." });
                }

                // 1. Cari Header eksisting di Database
                var header = dbt.Production_TransferLabor_Request_Header.FirstOrDefault(h => h.id == id);
                if (header == null)
                {
                    return Json(new { success = false, message = "Data Formulir tidak ditemukan di database." });
                }

                string jobCode = header.code_job_detail;
                string jobDetailName = header.job_detail;
                string toSection = header.to_section;
                string toDepartment = header.to_department;
                string fromSection = header.from_section;
                string fromDepartment = header.from_department;

                // 2. Parsing Input dengan aman (Jika ada perubahan data dari form)
                if (!string.IsNullOrEmpty(input.job_detail) && input.job_detail.Contains("|"))
                {
                    string[] parts = input.job_detail.Split('|');
                    jobCode = parts[0].Trim();
                    if (parts.Length > 1) jobDetailName = parts[1].Trim();
                }

                if (!string.IsNullOrEmpty(input.to_section) && input.to_section.Contains("|"))
                {
                    string[] parts = input.to_section.Split('|');
                    toSection = parts[0].Trim();
                    if (parts.Length > 1) toDepartment = parts[1].Trim();
                }

                if (!string.IsNullOrEmpty(input.from_section) && input.from_section.Contains("|"))
                {
                    string[] parts = input.from_section.Split('|');
                    fromSection = parts[0].Trim();
                    if (parts.Length > 1) fromDepartment = parts[1].Trim();
                }

                bool isSameDepartment = fromDepartment.Equals(toDepartment, StringComparison.OrdinalIgnoreCase);

                // 3. Update nilai pada Model Header
                header.start_date = input.start_date;
                header.end_date = input.end_date;
                header.from_section = fromSection;
                header.from_department = fromDepartment;
                header.to_section = toSection;
                header.to_department = toDepartment;
                header.request_creator_sign = input.request_creator_sign;
                header.job_detail = jobDetailName;
                header.code_job_detail = jobCode;
                header.request_creator_sign_date = input.request_creator_sign_date;
                header.status = isSameDepartment ? "COMPLETED" : "WAITING REQUESTOR MANAGER";
                header.updated_at = DateTime.Now;

                // 4. Update Status Approval
                if (isSameDepartment)
                {
                    header.request_approval_by = "SYSTEM";
                    header.request_approval_sign = "APPROVED";
                    header.request_approval_sign_date = DateTime.Now;

                    header.provider_issued_by = "SYSTEM";
                    header.provider_issued_sign = "CHECKED";
                    header.provider_issued_sign_date = DateTime.Now;

                    header.provider_approval_by = "SYSTEM";
                    header.provider_approval_sign = "APPROVED";
                    header.provider_approval_sign_date = DateTime.Now;
                }
                else
                {
                    // Reset approval fields jika lintas departemen (mengulang flow)
                    header.request_approval_by = null;
                    header.request_approval_sign = null;
                    header.request_approval_sign_date = null;

                    header.provider_issued_by = null;
                    header.provider_issued_sign = null;
                    header.provider_issued_sign_date = null;

                    header.provider_approval_by = null;
                    header.provider_approval_sign = null;
                    header.provider_approval_sign_date = null;
                }

                // 5. Hapus Detail Lama
                var existingDetails = dbt.Production_TransferLabor_Request_Detail.Where(d => d.referal_id == id).ToList();
                dbt.Production_TransferLabor_Request_Detail.RemoveRange(existingDetails);

                // 6. Mapping data ke Model Detail Baru dengan Safe Indexing
                var newDetails = new List<Production_TransferLabor_Request_Detail>();
                for (int i = 0; i < input.nik.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(input.nik[i])) continue;

                    float tlHours = input.transfer_labor_hours != null && input.transfer_labor_hours.Count > i ? input.transfer_labor_hours[i] : 0;
                    float otHours = input.transfer_overtime_hours != null && input.transfer_overtime_hours.Count > i ? input.transfer_overtime_hours[i] : 0;

                    // Safe index checking untuk array string
                    string empName = input.name != null && input.name.Count > i ? input.name[i] : "";
                    string empDesc = input.description != null && input.description.Count > i ? input.description[i] : "";
                    string empStatus = input.status != null && input.status.Count > i ? input.status[i] : "";
                    string empPlanning = input.planning != null && input.planning.Count > i ? input.planning[i] : "";

                    var detail = new Production_TransferLabor_Request_Detail
                    {
                        referal_id = id, 
                        nik = input.nik[i],
                        name = empName,
                        description = empDesc,
                        status = empStatus,
                        transfer_labor_hours = tlHours,
                        transfer_overtime_hours = otHours,
                        total_transfer_hours = tlHours + otHours,
                        planning = empPlanning
                    };

                    newDetails.Add(detail);
                }

                dbt.Production_TransferLabor_Request_Detail.AddRange(newDetails);

                // 7. Simpan ke Database
                dbt.SaveChanges();

                string errorReason = "";

                if (isSameDepartment)
                {
                    var inject = InjectTransferLabor(header);
                    // ==========================================
                    // LOGIKA EMAIL NOTIFIKASI (AUTO-APPROVE)
                    // ==========================================
                    var notificationTargets = dbt.Production_TransferLabor_Approval_Access
                        .Where(h => (h.section == header.from_section || h.section == header.to_section) && (h.access == "APPROVER" || h.access == "CHECKER"))
                        .Select(p => p.nik)
                        .Distinct()
                        .ToList();

                    foreach (var targetNik in notificationTargets)
                    {
                        if (!string.IsNullOrEmpty(targetNik))
                        {
                            SendEmailTransferLabor(header, targetNik, out errorReason);
                        }
                    }
                }
                else
                {
                    // ==========================================
                    // LOGIKA EMAIL APPROVAL (LINTAS DEPARTEMEN)
                    // ==========================================
                    var requestorApprovalNiks = dbt.Production_TransferLabor_Approval_Access
                        .Where(h => h.section == header.from_section && h.access == "APPROVER")
                        .Select(p => p.nik)
                        .Distinct()
                        .ToList();

                    foreach (var targetNik in requestorApprovalNiks)
                    {
                        if (!string.IsNullOrEmpty(targetNik))
                        {
                            SendEmailTransferLabor(header, targetNik, out errorReason);
                        }
                    }
                }

                // 8. Kembalikan response sukses
                return Json(new { success = true, message = "Formulir Transfer Labor berhasil diupdate!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan server: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetRequestData(string id)
        {
            try
            {
                var header = dbt.Production_TransferLabor_Request_Header.FirstOrDefault(h => h.id == id);
                if (header == null) return Json(new { success = false, message = "Data tidak ditemukan." }, JsonRequestBehavior.AllowGet);

                var details = dbt.Production_TransferLabor_Request_Detail
                                 .Where(d => d.referal_id == id)
                                 .Select(d => new {
                                     d.nik,
                                     d.name,
                                     d.transfer_labor_hours,
                                     d.transfer_overtime_hours
                                 }).ToList();

                // Format tanggal ke "yyyy-MM-ddTHH:mm" agar terbaca oleh input type datetime-local HTML5
                var responseData = new
                {
                    header = new
                    {
                        header.id,
                        start_date = header.start_date.ToString("yyyy-MM-ddTHH:mm"),
                        end_date = header.end_date.ToString("yyyy-MM-ddTHH:mm"),
                        header.from_section,
                        header.from_department,
                        header.to_section,
                        header.to_department,
                        header.job_detail,
                        header.code_job_detail,
                        header.request_creator_sign,
                        request_creator_sign_date = header.request_creator_sign_date?.ToString("yyyy-MM-dd HH:mm:ss")
                    },
                    details = details
                };

                return Json(new { success = true, data = responseData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Details(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "ID Request tidak ditemukan.");
            }

            var header = dbt.Production_TransferLabor_Request_Header.Where(h => h.id == id).FirstOrDefault();
            if (header == null)
            {
                return HttpNotFound("Data Transfer Labor tidak ditemukan.");
            }

            var RequestorApprovalNik = dbt.Production_TransferLabor_Approval_Access
                .Where(h => h.section == header.from_section && h.access == "APPROVER")
                .ToList();

            var ProviderIssuedNik = dbt.Production_TransferLabor_Approval_Access
                .Where(h => h.section == header.to_section && h.access == "CHECKER")
                .ToList();

            var ProviderApprovallNik = dbt.Production_TransferLabor_Approval_Access
                .Where(h => h.section == header.to_section && h.access == "APPROVER")
                .ToList();
            var details = dbt.Production_TransferLabor_Request_Detail
                             .Where(d => d.referal_id == id)
                             .ToList();
            ViewBag.RequestorApproval = RequestorApprovalNik;
            ViewBag.ProviderIssued = ProviderIssuedNik;
            ViewBag.ProviderApproval = ProviderApprovallNik;

            ViewBag.Details = details;
            ViewBag.Header = header;



            ViewBag.CurrentUser = currUser.Name;
            ViewBag.CurrentNIK = userNik;

            return View();
        }

        [HttpPost]
        public ActionResult SignProcess(string id, string signType)
        {
            try
            {
                var header = dbt.Production_TransferLabor_Request_Header.FirstOrDefault(h => h.id == id);
                if (header == null) return Json(new { success = false, message = "Data tidak ditemukan." });

                string currUser = User.Identity.Name;
                DateTime now = DateTime.Now;

                // Ambil daftar akses Provider
                var ProviderIssued = dbt.Production_TransferLabor_Approval_Access
                    .Where(h => h.section == header.to_section && h.access == "CHECKER")
                    .ToList();

                var ProviderApprovall = dbt.Production_TransferLabor_Approval_Access
                    .Where(h => h.section == header.to_section && h.access == "APPROVER")
                    .ToList();

                var creatorApproval = header.created_by;

                List<string> targetEmailNiks = new List<string>();

                switch (signType)
                {
                    // --- TAHAP REQUESTOR ---
                    case "RequestorApprove":
                        header.request_approval_by = currUser;
                        header.request_approval_sign = "APPROVED";
                        header.request_approval_sign_date = now;
                        header.status = "WAITING SPV APPROVAL"; // Diteruskan ke Provider Issue

                        // Tambahkan NIK tim Provider Issue ke daftar penerima email
                        // CATATAN: Ganti '.nik' dengan nama field yang benar di tabel Approval_Access kamu (misal: .user_id atau .employee_id)
                        targetEmailNiks.AddRange(ProviderIssued.Select(p => p.nik));
                        break;

                    case "RequestorReject":
                        header.request_approval_by = currUser;
                        header.request_approval_sign = "REJECTED";
                        header.request_approval_sign_date = now;
                        header.status = "REJECTED BY REQUESTOR MANAGER";

                        // Kembalikan ke Creator
                        targetEmailNiks.Add(creatorApproval);
                        break;

                    // --- TAHAP PROVIDER ISSUE ---
                    case "ProviderIssue":
                        header.provider_issued_by = currUser;
                        header.provider_issued_sign = "CHECKED";
                        header.provider_issued_sign_date = now;
                        header.status = "WAITING MANAGER APPROVAL"; // Diteruskan ke Provider Approver

                        // Tambahkan NIK tim Provider Approver ke daftar penerima email
                        targetEmailNiks.AddRange(ProviderApprovall.Select(p => p.nik));
                        break;

                    case "ProviderIssueReject":
                        header.provider_issued_by = currUser;
                        header.provider_issued_sign = "REJECTED";
                        header.provider_issued_sign_date = now;
                        header.status = "REJECTED BY CHECKER";

                        // Kembalikan ke Creator
                        targetEmailNiks.Add(creatorApproval);
                        break;

                    // --- TAHAP PROVIDER APPROVAL ---
                    case "ProviderApprove":
                        header.provider_approval_by = currUser;
                        header.provider_approval_sign = "APPROVED";
                        header.provider_approval_sign_date = now;
                        header.status = "COMPLETED";

                        var injectResult = InjectTransferLabor(header);
                        if (injectResult.success)
                        {
                            header.status = "COMPLETED";
                            targetEmailNiks.Add(creatorApproval);
                        }
                        else
                        {
                            throw new Exception($"Gagal melakukan inject transfer labor: {injectResult.message}");
                        }
                        break;


                    case "ProviderReject":
                        header.provider_approval_by = currUser;
                        header.provider_approval_sign = "REJECTED";
                        header.provider_approval_sign_date = now;
                        header.status = "REJECTED BY PROVIDER MANAGER";

                        // Kembalikan ke Creator
                        targetEmailNiks.Add(creatorApproval);
                        break;

                    default:
                        return Json(new { success = false, message = "Tipe tanda tangan tidak valid." });
                }

                header.updated_at = now;

                dbt.SaveChanges();

                int emailSuccessCount = 0;
                int emailFailCount = 0;
                List<string> failedDetails = new List<string>();

                foreach (var targetNik in targetEmailNiks.Distinct())
                {
                    if (!string.IsNullOrEmpty(targetNik))
                    {
                        string errorReason = "";

                        // Panggil fungsi dengan parameter out
                        bool isEmailSent = SendEmailTransferLabor(header, targetNik, out errorReason);

                        if (isEmailSent)
                        {
                            emailSuccessCount++;
                        }
                        else
                        {
                            emailFailCount++;
                            // Simpan NIK beserta alasan gagalnya
                            failedDetails.Add($"{targetNik} (Alasan: {errorReason})");
                        }
                    }
                }

                string responseMessage = "Proses persetujuan berhasil disimpan.";
                if (emailFailCount > 0)
                {
                    // Menampilkan NIK dan alasan gagalnya langsung ke layar UI
                    responseMessage += $" Gagal mengirim email ke: {string.Join(", ", failedDetails)}";
                }

                return Json(new
                {
                    success = true,
                    message = responseMessage
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }


        private (bool success, string message) InjectTransferLabor(Production_TransferLabor_Request_Header header)
        {
            try
            {
                // 1. Tarik data detail dari database dan jadikan List
                var details = dbt.Production_TransferLabor_Request_Detail
                                 .Where(d => d.referal_id == header.id)
                                 .ToList();

                // 2. Setel Tanggal TL
                DateTime dateTL = header.start_date.Date;
                if (header.start_date.TimeOfDay >= new TimeSpan(22, 0, 0))
                {
                    dateTL = dateTL.AddDays(1);
                }

                // =================================================================
                // 3. LOGIKA PENENTUAN SHIFT 8 JAM / 12 JAM (Dihitung di luar loop)
                // =================================================================
                string shiftSekarang = "Shift-3"; // Default
                var waktuStart = header.start_date.TimeOfDay;

                // Hitung durasi jam langsung dari selisih DateTime
                TimeSpan durasi = header.end_date - header.start_date;
                bool isLongShift = durasi.TotalHours >= 12;

                if (isLongShift)
                {
                    if (waktuStart >= new TimeSpan(6, 0, 0) && waktuStart < new TimeSpan(18, 0, 0))
                    {
                        shiftSekarang = "Shift-1";
                    }
                }
                else
                {
                    if (waktuStart >= new TimeSpan(6, 0, 0) && waktuStart < new TimeSpan(14, 0, 0))
                    {
                        shiftSekarang = "Shift-1";
                    }
                    else if (waktuStart >= new TimeSpan(14, 0, 0) && waktuStart < new TimeSpan(22, 0, 0))
                    {
                        shiftSekarang = "Shift-2";
                    }
                }

                var dataTransfer = new List<AbsensiTransferLaborTambahan>();

                foreach (var item in details)
                {
                    var newRecord = new AbsensiTransferLaborTambahan
                    {
                        Section = header.to_section,
                        Date = dateTL,
                        NIK = item.nik,
                        Name = item.name,
                        TransferTo = header.from_section,
                        Description = item.description,

                        TransferLaborNormal = ((decimal)item.transfer_labor_hours).ToString(),
                        TransferOvertime = ((decimal)item.transfer_overtime_hours).ToString(),
                        TransferLabor = ((decimal)item.transfer_overtime_hours + (decimal)item.transfer_labor_hours).ToString(),

                        DescTransferLabor = item.transfer_labor_hours != 0 ? header.code_job_detail : null,
                        DescTransferLaborOvertime = item.transfer_overtime_hours != 0 ? header.code_job_detail : null,
                        Planning = item.planning,
                        TransferOvertimePlanning = item.planning,

                        TransferLaborId = header.id,
                        Shift = shiftSekarang,
                        Status = item.status,

                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    dataTransfer.Add(newRecord);
                }

                var existingRecords = dbi.AbsensiTransferLaborTambahan
                                         .Where(x => x.TransferLaborId == header.id)
                                         .ToList();

                if (existingRecords.Any())
                {
                    dbi.AbsensiTransferLaborTambahan.RemoveRange(existingRecords);
                }

                if (dataTransfer.Any())
                {
                    dbi.AbsensiTransferLaborTambahan.AddRange(dataTransfer);
                }

                if (existingRecords.Any() || dataTransfer.Any())
                {
                    dbi.SaveChanges();
                }

                return (true, "Data berhasil ditransfer");
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                Exception innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    errorMessage += " | Detail: " + innerEx.Message;
                    innerEx = innerEx.InnerException;
                }

                return (false, errorMessage);
            }
        }

        private bool SendEmailTransferLabor(Production_TransferLabor_Request_Header header, string receiverNik, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                var receiver = db.V_Users_Active.FirstOrDefault(u => u.NIK == receiverNik);
                if (receiver == null)
                    throw new Exception($"Receiver with NIK {receiverNik} not found in V_Users_Active.");

                if (string.IsNullOrEmpty(receiver.Email))
                    throw new Exception($"Receiver with NIK {receiverNik} has no email registered.");

                var dept = receiver.CostName ?? "-";
                var name = receiver.Name ?? receiver.NIK;
                string senderAddress = "ngkportal-notification@ngkbusi.com";
                string senderName = "Transfer Labor Notification";
                string password = "100%NGKbusi!";

                string receiverEmail = receiver.Email;
                string receiverName = receiver.Name ?? receiver.NIK;

                string filePath = Path.Combine(Server.MapPath("~/Emails/Production/TransferLabor/"), "ApprovalTransferLabor.html");
                string mailText = System.IO.File.ReadAllText(filePath);

                string subject = "📩 Transfer Labor Request : "+ header.id ;

                string statusNote = header.status ?? "Draft/Unknown Status";
                string context1 = "Information";
                string context2 = "";
                string themeColor = "#007bff";
                string themeDark = "#0056b3";
                string bgStatus = "#e7f3ff";

                switch (statusNote.ToLower())
                {
                    case "waiting requestor manager":
                        context1 = "Requestor Manager Approval Required";
                        context2 = "A Transfer Labor request has been created from your department and is waiting for Manager Approval.";
                        break;
                    case "waiting spv approval":
                        context1 = "Checker Approval Required";
                        context2 = "A Transfer Labor request has been created and is waiting for SPV Approval.";
                        break;
                    case "waiting manager approval":
                        context1 = "Provider Manager Approval Required";
                        context2 = "The Transfer Labor request details have been checked. Waiting for Manager Approval.";
                        break;
                    case "completed":
                        context1 = "Completed";
                        context2 = "The Transfer Labor request has been fully completed and approved.";
                        break;
                    case "rejected":
                        context1 = "Rejected";
                        context2 = "The Transfer Labor request has been rejected.";
                        themeColor = "#dc3545";
                        themeDark = "#a71d2a";
                        bgStatus = "#f8d7da";
                        break;
                    case "rejected by checker":
                        context1 = "Rejected";
                        context2 = "The Transfer Labor request has been rejected.";
                        themeColor = "#dc3545";
                        themeDark = "#a71d2a";
                        bgStatus = "#f8d7da";
                        break;
                    case "rejected by provider manager":
                        context1 = "Rejected";
                        context2 = "The Transfer Labor request has been rejected.";
                        themeColor = "#dc3545";
                        themeDark = "#a71d2a";
                        bgStatus = "#f8d7da";
                        break;
                    case "rejected by requestor manager":
                        context1 = "Rejected";
                        context2 = "The Transfer Labor request has been rejected.";
                        themeColor = "#dc3545";
                        themeDark = "#a71d2a";
                        bgStatus = "#f8d7da";
                        break;
                    default:
                        context1 = "Notification";
                        context2 = $"There is an update on Transfer Labor Request. Current status: {statusNote}.";
                        break;
                }

                // 6. Ganti Warna Tema di Template HTML
                mailText = mailText
                    .Replace("#007bff", themeColor)
                    .Replace("#0056b3", themeDark)
                    .Replace("#e7f3ff", bgStatus);

                // 7. Ganti Placeholder Konten di Template HTML
                string mailBody = mailText
                    .Replace("##Context1##", context1)
                    .Replace("##Context2##", context2)
                    .Replace("##Message##", "Please log in to the system to review the details.")
                    .Replace("##HeaderID##", header.id)
                    .Replace("##FromDept##", $"{header.from_department} ({header.from_section})")
                    .Replace("##ToDept##", $"{header.to_department} ({header.to_section})")
                    .Replace("##StartDate##", header.start_date.ToString("dd MMM yyyy"))
                    .Replace("##EndDate##", header.end_date.ToString("dd MMM yyyy"))
                    .Replace("##JobDetail##", header.job_detail ?? "-")
                    .Replace("##Status##", statusNote)
                    .Replace("##Name##", name);

                // 8. Tentukan URL Detail Link
                string baseUrl = "http://localhost:8085";
                //string baseUrl = "https://portal.ngkbusi.com/NGKBusi";
                mailBody = mailBody.Replace("##Link##", $"{baseUrl}/Production/TransferLabor/Details/{header.id}");

                // 9. Kirim Email (SMTP)
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
                errorMessage = ex.Message;

                if (ex.InnerException != null)
                {
                    errorMessage += " | Detail: " + ex.InnerException.Message;
                }
                System.Diagnostics.Debug.WriteLine($"[EMAIL ERROR] {ex.Message}");
                return false;
            }
        }

    }

}