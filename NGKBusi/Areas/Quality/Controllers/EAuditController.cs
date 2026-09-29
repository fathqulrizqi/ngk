using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NGKBusi.Areas.Production.Models;
using NGKBusi.Areas.Quality.Models;
using NGKBusi.Models;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;


namespace NGKBusi.Areas.Quality.Controllers
{
    public class EAuditController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        QualityConnection dbq = new QualityConnection();

        // ==========================================
        // DIRECT VIEW
        // ==========================================

        public ActionResult Index()
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik);
            if (userAccess == null)
            {
                return View("V403");
            }


            ViewBag.EventList = dbq.Quality_EAudit_Audit_Event.Where(a => a.deleted_at == null)
                   .AsNoTracking()
                   .OrderByDescending(x => x.created_at)
                   .ToList();

        
            return View();
        }

        public ActionResult AuditEvent()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }


            ViewBag.Role = userAccess.access;
            ViewBag.AuditStandard = dbq.Quality_EAudit_Audit_Standard_Master.ToList();

            return View();
        }

        public ActionResult DetailEvent(string eventId)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();
                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);

                if (userProfile == null)
                {
                    return View("V401");
                }

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik);
                if (userAccess == null)
                {
                    return View("V403");
                }

                // Ambil info detail event saja
                var infoEvent = dbq.Quality_EAudit_Audit_Event.FirstOrDefault(x => x.audit_event_id == eventId);

                ViewBag.AvailableSections = dbq.Quality_EAudit_Section.ToList();
                ViewBag.Role = userAccess.access;
                ViewBag.EventInfo = infoEvent;
                ViewBag.CurrentNIK = userNik;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        public ActionResult RecapAudit()
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();
                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);

                if (userProfile == null)
                {
                    return View("V401");
                }

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik);
                if (userAccess == null)
                {
                    return View("V403");
                }


                ViewBag.role = userAccess.access;
                ViewBag.CurrentNIK = userNik;

                ViewBag.EventList = dbq.Quality_EAudit_Audit_Event.Where(a => a.deleted_at == null)
                       .AsNoTracking()
                       .OrderByDescending(x => x.created_at)
                       .ToList();

                ViewBag.AvailableSections = dbq.Quality_EAudit_Section.ToList();

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [HttpGet]
        public ActionResult FindingsReport(string id)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                if (userProfile == null)
                {
                    return View("V401");
                }

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik);
                if (userAccess == null)
                {
                    return View("V403");
                }

                if (string.IsNullOrEmpty(id))
                {
                    return RedirectToAction("AuditRecap", "EAudit");
                }

                var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                    .FirstOrDefault(s => s.audit_event_stakeholder_id == id && s.deleted_at == null);

                if (stakeholder == null)
                {
                    return HttpNotFound("Data section/stakeholder tidak ditemukan.");
                }

                // 1. Tarik semua assignment di stakeholder ini untuk mendapatkan list (tanpa filter userNik dulu)
                var allAssignments = dbq.Quality_EAudit_Audity_Auditor_Event
                    .Where(a => a.audit_event_stakeholder_id == id && a.deleted_at == null)
                    .ToList();

                // Inisialisasi variabel role
                bool isAuditor = false;
                bool isAuditee = false;
                bool isAdministrator = false; // Tambahan isAdministrator

                // 2. Cek Role User saat ini
                if (userAccess.access == "Administrator")
                {
                    isAuditor = true;
                    isAuditee = true;
                    isAdministrator = true; // Set true jika admin
                }
                else
                {
                    var userAssignments = allAssignments.Where(a => a.nik == userNik).ToList();
                    isAuditor = userAssignments.Any(a => a.role == "Auditor");
                    isAuditee = userAssignments.Any(a => a.role == "Auditee");
                }

                // =======================================================
                // LOGIKA GET LIST AUDITOR & AUDITEE (NAMA, NIK, EMAIL)
                // =======================================================

                // Ambil list NIK Auditor dan Auditee secara unik (distinct)
                var auditorNiks = allAssignments.Where(a => a.role == "Auditor").Select(a => a.nik).Distinct().ToList();
                var auditeeNiks = allAssignments.Where(a => a.role == "Auditee").Select(a => a.nik).Distinct().ToList();

                // Ambil data user dari tabel Access untuk mendapatkan Name dan Email
                var accessUsers = dbq.Quality_EAudit_Access
                    .Where(u => auditorNiks.Contains(u.NIK) || auditeeNiks.Contains(u.NIK))
                    .ToList();

                // Mapping ke UserReminderDto untuk Auditor
                var listAuditor = accessUsers
                    .Where(u => auditorNiks.Contains(u.NIK))
                    .Select(u => new UserReminderDto
                    {
                        NIK = u.NIK,
                        Name = u.name,
                        Email = u.email
                    })
                    .ToList();

                var listAuditee = accessUsers
                    .Where(u => auditeeNiks.Contains(u.NIK))
                    .Select(u => new UserReminderDto
                    {
                        NIK = u.NIK,
                        Name = u.name,
                        Email = u.email
                    })
                    .ToList();

                ViewBag.ListAuditor = listAuditor;
                ViewBag.ListAuditee = listAuditee;

                // Lempar flag status ke View
                ViewBag.IsAuditor = isAuditor;
                ViewBag.IsAuditee = isAuditee;
                ViewBag.IsAdministrator = isAdministrator; // Lempar isAdministrator ke ViewBag

                ViewBag.CurrentUserName = string.Join(" ", (userProfile.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));
                ViewBag.DeptName = stakeholder.responsibility_section ?? "-";
                ViewBag.FindingsReportId = id;

                ViewBag.TanggalAudit = stakeholder.start_event?.ToString("dd-MMM-yyyy");
                ViewBag.PeriodeAudit = stakeholder.start_event?.ToString("MMM-yyyy");

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [HttpGet]
        public ActionResult FindingsReportAll(string eventId)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                if (userProfile == null)
                {
                    return View("V401");
                }

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik && w.deleted_at == null);
                if (userAccess == null)
                {
                    return View("V403");
                }

                if (string.IsNullOrEmpty(eventId))
                {
                    return RedirectToAction("AuditRecap", "EAudit");
                }

                var auditEvent = dbq.Quality_EAudit_Audit_Event
                    .FirstOrDefault(e => e.audit_event_id == eventId && e.deleted_at == null);

                if (auditEvent == null)
                {
                    return HttpNotFound("Data Audit Event tidak ditemukan.");
                }

                bool isAdministrator = (userAccess.access == "Administrator");

                bool isMR = false;
                string currentEmail = userProfile.Email;

                if (!string.IsNullOrEmpty(currentEmail))
                {
                    var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                        .FirstOrDefault(x => x.audit_standard_name == auditEvent.audit_standard);

                    if (standardMaster != null &&
                        !string.IsNullOrEmpty(standardMaster.mr_email) &&
                        standardMaster.mr_email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        isMR = true;
                    }
                }

                ViewBag.IsAdministrator = isAdministrator;
                ViewBag.IsMR = isMR;

                ViewBag.CurrentUserName = string.Join(" ", (userProfile.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

                var earliestStakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                    .Where(s => s.audit_event_id == eventId
                             && s.deleted_at == null
                             && s.start_event != null)
                    .OrderBy(s => s.start_event)
                    .FirstOrDefault();

                ViewBag.TanggalAudit = earliestStakeholder?.start_event?.ToString("dd-MMM-yyyy") ?? "-";
                ViewBag.PeriodeAudit = earliestStakeholder?.start_event?.ToString("MMM-yyyy") ?? "-";

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [HttpGet]
        public ActionResult AuditNotes(string id)
        {
            try
            {
                // 1. Autentikasi & Otorisasi User
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                if (userProfile == null)
                {
                    return View("V401");
                }

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik);
                if (userAccess == null)
                {
                    return View("V403");
                }

                // 2. Validasi ID
                if (string.IsNullOrEmpty(id))
                {
                    return RedirectToAction("AuditRecap", "EAudit");
                }

                var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                    .FirstOrDefault(s => s.audit_event_stakeholder_id == id && s.deleted_at == null);

                if (stakeholder == null)
                {
                    return HttpNotFound("Data section/stakeholder tidak ditemukan.");
                }

                // Tarik data assignment ke memory (.ToList()) agar bisa menggunakan string.Join dan GroupBy
                var userAssignments = dbq.Quality_EAudit_Audity_Auditor_Event
                    .Where(a => a.audit_event_stakeholder_id == id && a.deleted_at == null)
                    .ToList();

                var eligibleAuditorCats = new List<string>();
                var eligibleAuditeeCats = new List<string>();

                bool isAdministrator = false; // <-- Inisialisasi isAdministrator

                // Jika Administrator, beri akses ke semua kategori secara default
                if (userAccess.access == "Administrator")
                {
                    isAdministrator = true; // <-- Set true jika user adalah Admin
                    eligibleAuditorCats = new List<string> { "System", "Process", "Product" };
                    eligibleAuditeeCats = new List<string> { "System", "Process", "Product" };
                }
                else
                {
                    eligibleAuditorCats = userAssignments
                        .Where(a => a.nik == userNik && a.role == "Auditor")
                        .Select(a => a.category_audit)
                        .Distinct()
                        .ToList();

                    eligibleAuditeeCats = userAssignments
                        .Where(a => a.nik == userNik && a.role == "Auditee")
                        .Select(a => a.category_audit)
                        .Distinct()
                        .ToList();
                }

                // =======================================================
                // LOGIKA GET LIST AUDITOR & AUDITEE BESERTA KATEGORINYA
                // =======================================================

                // 1. Group Auditor by NIK & gabung category-nya
                var auditorGroups = userAssignments
                    .Where(a => a.role == "Auditor")
                    .GroupBy(a => a.nik)
                    .Select(g => new {
                        NIK = g.Key,
                        Categories = string.Join(", ", g.Select(x => x.category_audit).Distinct())
                    }).ToList();

                var auditeeGroups = userAssignments
                    .Where(a => a.role == "Auditee")
                    .GroupBy(a => a.nik)
                    .Select(g => new {
                        NIK = g.Key,
                        Categories = string.Join(", ", g.Select(x => x.category_audit).Distinct())
                    }).ToList();

                var auditorNiks = auditorGroups.Select(x => x.NIK).ToList();
                var auditeeNiks = auditeeGroups.Select(x => x.NIK).ToList();

                var accessUsers = dbq.Quality_EAudit_Access
                    .Where(u => auditorNiks.Contains(u.NIK) || auditeeNiks.Contains(u.NIK))
                    .ToList();

                var listAuditor = accessUsers
                    .Where(u => auditorNiks.Contains(u.NIK))
                    .Select(u => new UserReminderDto
                    {
                        NIK = u.NIK,
                        Name = $"{u.name} ({auditorGroups.First(g => g.NIK == u.NIK).Categories})",
                        Email = u.email
                    })
                    .ToList();

                var listAuditee = accessUsers
                    .Where(u => auditeeNiks.Contains(u.NIK))
                    .Select(u => new UserReminderDto
                    {
                        NIK = u.NIK,
                        Name = $"{u.name} ({auditeeGroups.First(g => g.NIK == u.NIK).Categories})",
                        Email = u.email
                    })
                    .ToList();

                ViewBag.ListAuditor = listAuditor;
                ViewBag.ListAuditee = listAuditee;

                ViewBag.IsAdministrator = isAdministrator; // <-- Lempar isAdministrator ke ViewBag

                ViewBag.EligibleAuditorCategories = eligibleAuditorCats;
                ViewBag.EligibleAuditeeCategories = eligibleAuditeeCats;
                ViewBag.CurrentUserName = string.Join(" ", (userProfile.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));
                ViewBag.DeptName = stakeholder.responsibility_section ?? "-";
                ViewBag.TanggalAudit = stakeholder.created_at.ToString("dd-MMMM-yyyy");
                ViewBag.AuditNotesId = id;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        public ActionResult AuditQuestion(string stakeholderId, string type)
        {

            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }
            if (string.IsNullOrEmpty(type) || (type != "Product" && type != "Process" && type != "System"))
            {
                ViewBag.ErrorMessage = "Invalid audit type specified.";
                return View("Error");
            }

            ViewBag.CriteriaList = dbq.Quality_EAudit_Judgement_Criteria.ToList();
            ViewBag.Role = userAccess?.access;
            ViewBag.StakeholderId = stakeholderId;
            ViewBag.Auditor = dbq.Quality_EAudit_Audity_Auditor_Master
                .Where(a => a.role == "Auditor")
                .Select(s => new
                {
                    nik = s.nik,
                    person_name = s.person_name,
                    email = s.email,
                    section = s.section,
                    role = s.role
                }).ToList();

            ViewBag.Auditie = dbq.Quality_EAudit_Audity_Auditor_Master
                 .Where(a => a.role == "Auditee")
                 .Select(s => new
                 {
                     nik = s.nik,
                     person_name = s.person_name,
                     email = s.email,
                     section = s.section,
                     role = s.role
                 }).ToList();

            ViewBag.isAssignedAuditor = dbq.Quality_EAudit_Audity_Auditor_Event.Any(
                    a => a.audit_event_stakeholder_id == stakeholderId &&
                         a.role == "Auditor" &&
                         a.deleted_at == null &&
                         a.nik == userNik
                );
            ViewBag.ListUser = dbq.Quality_EAudit_Access.ToList();
            ViewBag.AuditType = type;
            ViewBag.UserNik = userNik;

            return View();
        }

        public ActionResult MasterAuditor()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }

            var Auditors = dbq.Quality_EAudit_Audity_Auditor_Master
            .Select(a => new
            {
                audity_auditor_id = a.audity_auditor_id,
                person_name = a.person_name,
                role = a.role,
                nik = a.nik,
                email = a.email,
                status = a.status,
                section = a.section,
                created_at = a.created_at,

                Details = a.AuditorDetails.Select(s => new
                {
                    SectionName = s.Section.section_name,
                    AuditStandardName = s.Audit_Standard.audit_standard_name,
                    CategoryAudit = s.category_audit
                }).ToList()
            })
            .ToList();



            ViewBag.GroupedQuestions = Auditors.GroupBy(a => a.role).ToList();
            ViewBag.AccessUsers = dbq.Quality_EAudit_Access.ToList();

            ViewBag.AuditStandard = dbq.Quality_EAudit_Audit_Standard_Master.ToList();
            ViewBag.Section = dbq.Quality_EAudit_Section.ToList();

            return View();
        }

        public ActionResult MasterQuestion()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }



            var questions = dbq.Quality_EAudit_Question_Master
                               .Include(q => q.Section)
                               .Include(p => p.Audit_Standard)
                               .Where(q => q.deleted_at == null)
                               .ToList();

            ViewBag.AuditStandard = dbq.Quality_EAudit_Audit_Standard_Master.ToList();

            var groupedQuestions = questions.GroupBy(g => g.Section).ToList();

            ViewBag.GroupedQuestions = groupedQuestions;

            ViewBag.Section = dbq.Quality_EAudit_Section.ToList();

            return View();
        }

        public ActionResult OtherSetting()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }

            ViewBag.Section = dbq.Quality_EAudit_Section.ToList();
            ViewBag.Judgement = dbq.Quality_EAudit_Judgement_Criteria.ToList();
            ViewBag.Access = dbq.Quality_EAudit_Access;
            ViewBag.Standard_Audit = dbq.Quality_EAudit_Audit_Standard_Master;

            return View();
        }

        public ActionResult DetailCAR(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }

            ViewBag.CARID = id;
            ViewBag.Name = string.Join(" ", (userProfile.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

            var role = dbq.Quality_EAudit_Audity_Auditor_Event
                        .Where(a => a.AuditEventStakeholder.Questions
                            .Any(q => q.CARs.Any(c => c.no_report == id)) && a.deleted_at == null)
                        .Select(a => new
                        {
                            a.role,
                            a.category_audit,
                            a.nik
                        })
                        .ToList();

            var question = dbq.Quality_EAudit_Question_Event
                  .FirstOrDefault(a => a.CARs.FirstOrDefault().no_report == id);

            var isAuditor = false;
            var isAuditee = false;
            var isAdministrator = false;

            // Menyiapkan variabel untuk menampung list NIK
            List<string> auditorNiks = new List<string>();
            List<string> auditeeNiks = new List<string>();

            if (question != null)
            {
                var stakeholder = role.Where(a => a.nik == userNik).ToList();

                if (userAccess.access == "Administrator")
                {
                    isAuditor = true;
                    isAuditee = true;
                    isAdministrator = true;
                }
                else
                {
                    if (stakeholder.Any(a => a.category_audit == question.category_audit && a.role == "Auditee"))
                    {
                        isAuditee = true;
                    }

                    if (stakeholder.Any(a => a.category_audit == question.category_audit && a.role == "Auditor"))
                    {
                        isAuditor = true;
                    }
                }

                auditorNiks = role.Where(a => a.category_audit == question.category_audit && a.role == "Auditor")
                                  .Select(a => a.nik).Distinct().ToList();

                auditeeNiks = role.Where(a => a.category_audit == question.category_audit && a.role == "Auditee")
                                  .Select(a => a.nik).Distinct().ToList();
            }

            var listAuditor = dbq.Quality_EAudit_Access
                    .Where(u => auditorNiks.Contains(u.NIK))
                    .Select(u => new UserReminderDto
                    {
                        NIK = u.NIK,
                        Name = u.name,
                        Email = u.email
                    })
                    .ToList();

            var listAuditee = dbq.Quality_EAudit_Access
                                .Where(u => auditeeNiks.Contains(u.NIK))
                                .Select(u => new UserReminderDto
                                {
                                    NIK = u.NIK,
                                    Name = u.name,
                                    Email = u.email 
                                })
                                .ToList();

            ViewBag.isAuditor = isAuditor;
            ViewBag.isAuditee = isAuditee;
            ViewBag.isAdministrator = isAdministrator;

            ViewBag.ListAuditor = listAuditor;
            ViewBag.ListAuditee = listAuditee;

            return View();
        }

        public ActionResult DetailOFI(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }

            // Potong max 2 kata untuk ViewBag
            ViewBag.Name = string.Join(" ", (userProfile.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

            ViewBag.OFI_ID = id;

            // Inisialisasi variabel pengecekan role
            var isAuditor = false;
            var isAuditee = false;
            var isAdministrator = false;

            List<string> auditeeNiks = new List<string>();
            List<string> auditorNiks = new List<string>();

            // 1. Cari Header OFI berdasarkan parameter 'id' (ofi_header_id)
            var ofiHeader = dbq.Quality_EAudit_OFI_Header.FirstOrDefault(o => o.ofi_header_id == id);

            if (ofiHeader != null)
            {
                var stakeholders = dbq.Quality_EAudit_Audity_Auditor_Event
                    .Where(a => a.audit_event_stakeholder_id == ofiHeader.audit_event_stakeholder_id && a.deleted_at == null)
                    .ToList();

                // 2. Logika penentuan isAdministrator, isAuditor, dan isAuditee
                if (userAccess.access == "Administrator")
                {
                    isAuditor = true;
                    isAuditee = true;
                    isAdministrator = true;
                }
                else
                {
                    if (stakeholders.Any(a => a.nik == userNik && a.role == "Auditee"))
                    {
                        isAuditee = true;
                    }

                    if (stakeholders.Any(a => a.nik == userNik && a.role == "Auditor"))
                    {
                        isAuditor = true;
                    }
                }

                // 3. Tarik NIK untuk kebutuhan list modal reminder
                auditeeNiks = stakeholders.Where(a => a.role == "Auditee").Select(a => a.nik).Distinct().ToList();
                auditorNiks = stakeholders.Where(a => a.role == "Auditor").Select(a => a.nik).Distinct().ToList();
            }

            var listAuditee = dbq.Quality_EAudit_Access
                .Where(u => auditeeNiks.Contains(u.NIK))
                 .Select(u => new UserReminderDto
                 {
                     NIK = u.NIK,
                     Name = u.name,
                     Email = u.email
                 })
                .ToList();

            var listAuditor = dbq.Quality_EAudit_Access
                .Where(u => auditorNiks.Contains(u.NIK))
                 .Select(u => new UserReminderDto
                 {
                     NIK = u.NIK,
                     Name = u.name,
                     Email = u.email
                 })
                .ToList();

            // Lempar data ke ViewBag
            ViewBag.ListAuditee = listAuditee;
            ViewBag.ListAuditor = listAuditor;

            // Lempar flag role ke ViewBag untuk dibaca oleh frontend (.cshtml)
            ViewBag.isAuditor = isAuditor;
            ViewBag.isAuditee = isAuditee;
            ViewBag.isAdministrator = isAdministrator;

            return View();
        }

        [HttpGet]
        public JsonResult GetCatatanAuditData(string stakeholderId, string type = "ALL")
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(userNik))
                    return Json(new { success = false, message = "Sesi telah habis." }, JsonRequestBehavior.AllowGet);

                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                if (userProfile == null)
                    return Json(new { success = false, message = "Unauthorized access." }, JsonRequestBehavior.AllowGet);

                if (string.IsNullOrEmpty(stakeholderId))
                    return Json(new { success = false, message = "Stakeholder ID tidak valid." }, JsonRequestBehavior.AllowGet);

                var carQuery = dbq.Quality_EAudit_CAR_Header
                    .Where(c => c.deleted_at == null
                                && c.QuestionEvent != null
                                && c.QuestionEvent.audit_event_stakeholder_id == stakeholderId
                                && c.QuestionEvent.deleted_at == null);

                if (!string.IsNullOrEmpty(type) && type != "ALL")
                    carQuery = carQuery.Where(c => c.QuestionEvent.category_audit == type);

                var carRawData = carQuery
                    .Select(c => new
                    {
                        c.no_report,
                        c.question_id,
                        QuestionCategory = c.QuestionEvent.category_audit,
                        QuestionClausul = c.QuestionEvent.clausul,
                        QuestionText = c.QuestionEvent.question,
                        QuestionNote = c.QuestionEvent.note,
                        FindingDetail = c.QuestionEvent.Finding
                            .Where(f => f.deleted_at == null)
                            .OrderByDescending(f => f.created_at)
                            .FirstOrDefault()
                    })
                    .ToList();

                var carFormatted = carRawData.Select(c => new
                {
                    QuestionId = c.question_id,
                    CategoryAudit = c.QuestionCategory ?? "-",
                    Clausul = c.QuestionClausul ?? "-",
                    Pertanyaan = c.QuestionText ?? "-",
                    Note = c.QuestionNote ?? "",

                    Problem = c.FindingDetail != null ? c.FindingDetail.problem : "",
                    Location = c.FindingDetail != null ? c.FindingDetail.location : "",
                    Object = c.FindingDetail != null ? c.FindingDetail.object_name : "",
                    Reference = c.FindingDetail != null ? c.FindingDetail.reference : ""
                }).ToList();


                List<dynamic> ofiFormatted = new List<dynamic>();

                if (string.IsNullOrEmpty(type) || type == "ALL" || type == "OFI")
                {
                    var ofiRawData = dbq.Quality_EAudit_OFI_Detail
                        .Where(d => d.OFIHeader != null
                                    && d.OFIHeader.audit_event_stakeholder_id == stakeholderId
                                    && d.OFIHeader.deleted_at == null
                                    && d.QuestionEvent != null
                                    && d.QuestionEvent.audit_event_stakeholder_id == stakeholderId
                                    && d.QuestionEvent.deleted_at == null)
                        .Select(d => new
                        {
                            d.question_id,
                            QuestionCategory = d.QuestionEvent.category_audit,
                            QuestionClausul = d.QuestionEvent.clausul,
                            QuestionText = d.QuestionEvent.question,
                            QuestionNote = d.QuestionEvent.note
                        })
                        .ToList();

                    ofiFormatted = ofiRawData.Select(o => (dynamic)new
                    {
                        QuestionId = o.question_id,
                        CategoryAudit = o.QuestionCategory ?? "-",
                        Clausul = o.QuestionClausul ?? "-",
                        Pertanyaan = o.QuestionText ?? "-",
                        Note = o.QuestionNote ?? "",

                        Problem = (string)null,
                        Location = (string)null,
                        Object = (string)null,
                        Reference = (string)null
                    }).ToList();
                }

                var formattedData = carFormatted.Cast<object>()
                    .Concat(ofiFormatted.Cast<object>())
                    .ToList();

                // ==========================================
                // 3. DATA GUIDES (Historical Problem / Checkbox)
                // ==========================================
                var guidesQuery = dbq.Quality_EAudit_Audit_Guide
                    .Where(g => g.audit_event_stakeholder_id == stakeholderId && g.deleted_at == null);

                if (!string.IsNullOrEmpty(type) && type != "ALL")
                    guidesQuery = guidesQuery.Where(g => g.category_audit == type);

                var guidesData = guidesQuery.Select(g => new
                {
                    CategoryAudit = g.category_audit,
                    GuideName = g.guide_name
                }).ToList();

                // ==========================================
                // 4. DATA GENERAL NOTE (Catatan Auditor di Bawah Tabel)
                // ==========================================
                var notesQuery = dbq.Quality_EAudit_Audit_Note
                    .Where(n => n.audit_event_stakeholder_id == stakeholderId && n.deleted_at == null);

                if (!string.IsNullOrEmpty(type) && type != "ALL")
                    notesQuery = notesQuery.Where(n => n.category_audit == type);

                var notesData = notesQuery.Select(n => new
                {
                    CategoryAudit = n.category_audit,
                    Note = n.note
                }).ToList();

                // ==========================================
                // 5. DATA APPROVAL YANG SUDAH ADA (Stampel TTD Auditor/Auditee)
                // recap_sign_by menyimpan NIK (dipakai juga untuk otorisasi di endpoint Upsert),
                // jadi di sini perlu join manual ke V_Users_Active untuk dapat nama tampilannya.
                // ==========================================
                var rawApprovals = dbq.Quality_EAudit_Recap_Approval
                    .Where(a => a.audit_event_stakeholder_id == stakeholderId)
                    .Select(a => new
                    {
                        a.category_recap,
                        a.recap_sign_role,
                        a.recap_sign_status,
                        a.recap_sign_date,
                        a.recap_sign_by // NIK
                    })
                    .ToList();

                var signerNiks = rawApprovals
                    .Where(a => !string.IsNullOrEmpty(a.recap_sign_by))
                    .Select(a => a.recap_sign_by)
                    .Distinct()
                    .ToList();

                var signerNameMap = db.V_Users_Active
                    .Where(u => signerNiks.Contains(u.NIK))
                    .ToDictionary(u => u.NIK, u => u.Name);

                var existingApprovals = rawApprovals.Select(a => new
                {
                    category_recap = a.category_recap,
                    recap_sign_role = a.recap_sign_role,
                    recap_sign_status = a.recap_sign_status,
                    recap_sign_date = a.recap_sign_date,
                    recap_sign_by_name = (!string.IsNullOrEmpty(a.recap_sign_by) && signerNameMap.ContainsKey(a.recap_sign_by))
                        ? signerNameMap[a.recap_sign_by]
                        : a.recap_sign_by // fallback: tampilkan NIK kalau nama tidak ketemu
                }).ToList();


                var auditStandardStr = dbq.Quality_EAudit_Audit_Event
                    .Where(x => x.Stakeholders.Any(s => s.audit_event_stakeholder_id == stakeholderId))
                    .Select(x => x.audit_standard)
                    .FirstOrDefault(); // Hasil dari DB: "ISO 9001 & IATF 16949"

                // Proses Split & Pembersihan Spasi
                List<string> auditStandards = auditStandardStr?
                    .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList() ?? new List<string>();
                // ==========================================
                // 6. RETURN KESELURUHAN DATA
                // ==========================================
                return Json(new
                {
                    success = true,
                    data = formattedData,         // Tabel CAR & OFI (gabungan)
                    guides = guidesData,          // Checkbox Historical Problem
                    generalNotes = notesData,     // Kotak Catatan Auditor di bawah tabel
                    auditStandards = auditStandards,
                    existingApprovals = existingApprovals, // Stampel TTD yang sudah pernah disimpan
                    currentUserName = userProfile.Name     // Nama user yang login, untuk render stampel baru
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message;
                if (ex.InnerException != null) errorMsg += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = errorMsg }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult CAROFIManagement()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }
            var role = userAccess.access;
            ViewBag.Role = userAccess.access;
            ViewBag.EventList = dbq.Quality_EAudit_Audit_Event.Where(a => a.deleted_at == null)
                       .AsNoTracking()
                       .OrderByDescending(x => x.created_at)
                       .ToList();


            return View();
        }

        public ActionResult PrintCAR()
        {
            return View();
        }


        //FUNCTION MASTER

        // ==========================================
        // CRUD: AUDITOR MASTER
        // ==========================================
        [HttpPost]
        public JsonResult CreateAuditorMaster(AuditorDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            using (var transaction = dbq.Database.BeginTransaction())
            {
                try
                {
                    bool isDuplicate = dbq.Quality_EAudit_Audity_Auditor_Master
                                          .Any(x => x.nik == dto.nik && x.role == dto.role);

                    if (isDuplicate)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Failed to save. NIK {dto.nik} with the role '{dto.role}' is already registered in the system."
                        });
                    }

                    var item = new Quality_EAudit_Audity_Auditor_Master
                    {
                        person_name = dto.person_name,
                        role = dto.role,
                        email = dto.email,
                        nik = dto.nik,
                        status = dto.status ?? "Active",
                        section = dto.section,
                        created_at = DateTime.Now
                    };
                    dbq.Quality_EAudit_Audity_Auditor_Master.Add(item);
                    dbq.SaveChanges();

                    if (dto.Details != null && dto.Details.Any())
                    {
                        foreach (var detail in dto.Details)
                        {
                            var newDetail = new Quality_EAudit_Audity_Auditor_Master_Detail
                            {
                                audity_auditor_id = item.audity_auditor_id,
                                section_id = detail.section_id,
                                audit_standard_id = detail.audit_standard_id,
                                category_audit = detail.category_audit,
                                created_at = DateTime.Now
                            };
                            dbq.Quality_EAudit_Audity_Auditor_Master_Detail.Add(newDetail);
                        }
                        dbq.SaveChanges();
                    }

                    transaction.Commit();
                    return Json(new { success = true, message = "Data successfully added." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult UpdateAuditorMaster(int id, AuditorDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            using (var transaction = dbq.Database.BeginTransaction())
            {
                try
                {
                    var item = dbq.Quality_EAudit_Audity_Auditor_Master.Find(id);
                    if (item == null || item.Deleted_at != null)
                    {
                        return Json(new { success = false, message = "Auditor not found or has been deleted." });
                    }

                    bool isDuplicate = dbq.Quality_EAudit_Audity_Auditor_Master
                                          .Any(x => x.nik == dto.nik &&
                                                    x.role == dto.role &&
                                                    x.audity_auditor_id != id &&
                                                    x.Deleted_at == null);

                    if (isDuplicate)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Failed to update. NIK {dto.nik} with the role '{dto.role}' is already registered in the system."
                        });
                    }

                    item.person_name = dto.person_name;
                    item.role = dto.role;
                    item.email = dto.email;
                    item.nik = dto.nik;
                    item.status = dto.status ?? item.status;
                    item.section = dto.section;
                    item.updated_at = DateTime.Now;

                    var existingDetails = dbq.Quality_EAudit_Audity_Auditor_Master_Detail
                                             .Where(d => d.audity_auditor_id == id)
                                             .ToList();

                    dbq.Quality_EAudit_Audity_Auditor_Master_Detail.RemoveRange(existingDetails);

                    if (dto.Details != null && dto.Details.Any())
                    {
                        foreach (var detail in dto.Details)
                        {
                            var newDetail = new Quality_EAudit_Audity_Auditor_Master_Detail
                            {
                                audity_auditor_id = id,
                                section_id = detail.section_id,
                                audit_standard_id = detail.audit_standard_id,
                                category_audit = detail.category_audit,
                                created_at = DateTime.Now
                            };
                            dbq.Quality_EAudit_Audity_Auditor_Master_Detail.Add(newDetail);
                        }
                    }

                    dbq.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Auditor data successfully updated." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public JsonResult DeleteAuditorMaster(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            using (var transaction = dbq.Database.BeginTransaction())
            {
                try
                {
                    var item = dbq.Quality_EAudit_Audity_Auditor_Master.Find(id);

                    if (item != null)
                    {
                        var relatedDetails = dbq.Quality_EAudit_Audity_Auditor_Master_Detail
                                                .Where(d => d.audity_auditor_id == id)
                                                .ToList();

                        if (relatedDetails.Any())
                        {
                            dbq.Quality_EAudit_Audity_Auditor_Master_Detail.RemoveRange(relatedDetails);
                        }

                        dbq.Quality_EAudit_Audity_Auditor_Master.Remove(item);

                        dbq.SaveChanges();
                        transaction.Commit();

                        return Json(new { success = true, message = "Auditor data permanently deleted." });
                    }

                    return Json(new { success = false, message = "Data not found." });
                }
                catch (Exception ex)
                {
                    // Jika terjadi error, batalkan semua proses penghapusan
                    transaction.Rollback();
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                }
            }
        }

        // ==========================================
        // CRUD: USER QUESTION MASTER
        // ==========================================
        [HttpPost]
        public JsonResult CreateQuestionMaster(QuestionDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            var item = new Quality_EAudit_Question_Master
            {
                section_id = dto.section_id,
                audit_standard_id = dto.audit_standard_id,
                clausul = dto.clausul,
                category_audit = dto.category_audit,
                process_name = dto.process_name,
                question = dto.question,
                created_at = DateTime.Now
            };
            dbq.Quality_EAudit_Question_Master.Add(item);
            dbq.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateQuestionMaster(int id, QuestionDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            var item = dbq.Quality_EAudit_Question_Master.Find(id);
            if (item != null)
            {
                item.clausul = dto.clausul;
                item.category_audit = dto.category_audit;
                item.process_name = dto.process_name;
                item.question = dto.question;
                item.updated_at = DateTime.Now;
                dbq.SaveChanges();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteQuestionMaster(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            var item = dbq.Quality_EAudit_Question_Master.Find(id);
            if (item != null)
            {
                dbq.Quality_EAudit_Question_Master.Remove(item);

                dbq.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Data not found!" });
        }

        // ==========================================
        // CRUD: SECTION
        // ==========================================

        [HttpGet]
        public JsonResult GetSectionsByEvent(string eventId)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null) return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            string role = userAccess.access;

            try
            {
                List<string> listSection = new List<string>();

                if (role == "Administrator")
                {
                    listSection = dbq.Quality_EAudit_Audit_Event_Stakeholder
                        .Where(y => y.audit_event_id == eventId)
                        .Select(x => x.responsibility_section)
                        .Distinct()
                        .ToList();
                }
                else
                {
                    listSection = dbq.Quality_EAudit_Audit_Event_Stakeholder
                        .Where(y => y.audit_event_id == eventId && y.Auditors.Any(w => w.nik == userNik))
                        .Select(x => x.responsibility_section)
                        .Distinct()
                        .ToList();
                }

                return Json(new { success = true, data = listSection }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal mengambil data Section: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetSections()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null) return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var data = dbq.Quality_EAudit_Section.ToList();
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal mengambil data Section: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult CreateSection(SectionDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }

            var item = new Quality_EAudit_Section
            {
                section_name = dto.section_name,
                created_at = DateTime.Now
            };

            dbq.Quality_EAudit_Section.Add(item);
            dbq.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateSection(long id, SectionDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // Parameter id menggunakan tipe 'long' menyesuaikan dengan model
                var item = dbq.Quality_EAudit_Section.Find(id);

                if (item == null)
                {
                    return Json(new { success = false, message = "Data Section tidak ditemukan." });
                }

                item.section_name = dto.section_name;
                item.updated_at = DateTime.Now; // Set waktu update secara otomatis

                dbq.SaveChanges();

                return Json(new { success = true, message = "Data berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteSection(long id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }

            // Parameter id menggunakan tipe 'long' menyesuaikan dengan model
            var item = dbq.Quality_EAudit_Section.Find(id);

            if (item != null)
            {
                dbq.Quality_EAudit_Section.Remove(item);
                dbq.SaveChanges();
            }

            return Json(new { success = true });
        }

        // ==========================================
        // CRUD: JUDGEMENT
        // ==========================================
        [HttpGet]
        public JsonResult GetJudgements()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null) return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var data = dbq.Quality_EAudit_Judgement_Criteria.ToList();
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal mengambil data Judgement: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult CreateJudgement(JudgementDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            var item = new Quality_EAudit_Judgement_Criteria
            {
                score = dto.score,
                criteria = dto.criteria,
                judgement = dto.judgement
            };
            dbq.Quality_EAudit_Judgement_Criteria.Add(item);
            dbq.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateJudgement(int id, JudgementDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                var item = dbq.Quality_EAudit_Judgement_Criteria.Find(id);

                if (item == null)
                {
                    return Json(new { success = false, message = "Data Judgement tidak ditemukan." });
                }

                item.score = dto.score;
                item.criteria = dto.criteria;
                item.judgement = dto.judgement;
                dbq.SaveChanges();

                return Json(new { success = true, message = "Data berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteJudgement(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            var item = dbq.Quality_EAudit_Judgement_Criteria.Find(id);
            if (item != null)
            {
                dbq.Quality_EAudit_Judgement_Criteria.Remove(item);
                dbq.SaveChanges();
            }
            return Json(new { success = true });
        }

        // ==========================================
        // CRUD: USER ACCESS
        // ==========================================
        [HttpGet]
        public JsonResult GetAccesses()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null) return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var data = dbq.Quality_EAudit_Access.ToList();
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal mengambil data Access: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CreateAccess(Quality_EAudit_Access dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var isNikExist = dbq.Quality_EAudit_Access.Any(a => a.NIK == dto.NIK);
                if (isNikExist)
                {
                    return Json(new { success = false, message = $"Validasi gagal: NIK '{dto.NIK}' sudah terdaftar dalam sistem." });
                }

                dto.created_at = DateTime.Now;
                dbq.Quality_EAudit_Access.Add(dto);
                dbq.SaveChanges();

                return Json(new { success = true, message = "Access Mapping berhasil ditambahkan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal create: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateAccess(int id, Quality_EAudit_Access dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbq.Quality_EAudit_Access.Find(id);

                if (item == null) return Json(new { success = false, message = "Data Access tidak ditemukan." });

                var isNikExist = dbq.Quality_EAudit_Access.Any(a => a.NIK == dto.NIK && a.access_id != id);
                if (isNikExist)
                {
                    return Json(new { success = false, message = $"Validasi gagal: NIK '{dto.NIK}' sudah digunakan oleh user lain." });
                }

                item.NIK = dto.NIK;
                item.access = dto.access;
                item.email = dto.email;
                item.name = dto.name;
                item.updated_at = DateTime.Now;

                dbq.SaveChanges();
                return Json(new { success = true, message = "Access Mapping berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteAccess(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbq.Quality_EAudit_Access.Find(id);
                if (item == null) return Json(new { success = false, message = "Data Access tidak ditemukan." });

                dbq.Quality_EAudit_Access.Remove(item);
                dbq.SaveChanges();

                return Json(new { success = true, message = "Access Mapping berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal delete: " + ex.Message });
            }
        }

        // ==========================================
        // CRUD: AUDIT STANDARD MASTER
        // ==========================================
        [HttpGet]
        public JsonResult GetStandards()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null) return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                // Pastikan menggunakan .ToList() agar dieksekusi menjadi array
                var data = dbq.Quality_EAudit_Audit_Standard_Master.ToList();
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal mengambil data Standard: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult CreateStandard(Quality_EAudit_Audit_Standard_Master dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                dto.created_at = DateTime.Now;
                dbq.Quality_EAudit_Audit_Standard_Master.Add(dto);
                dbq.SaveChanges();

                return Json(new { success = true, message = "Audit Standard berhasil ditambahkan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal create: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateStandard(int id, Quality_EAudit_Audit_Standard_Master dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbq.Quality_EAudit_Audit_Standard_Master.Find(id);

                if (item == null) return Json(new { success = false, message = "Data Audit Standard tidak ditemukan." });

                item.audit_standard_name = dto.audit_standard_name;

                dbq.SaveChanges();
                return Json(new { success = true, message = "Audit Standard berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteStandard(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbq.Quality_EAudit_Audit_Standard_Master.Find(id);
                if (item == null) return Json(new { success = false, message = "Data Audit Standard tidak ditemukan." });

                dbq.Quality_EAudit_Audit_Standard_Master.Remove(item);
                dbq.SaveChanges();

                return Json(new { success = true, message = "Audit Standard berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal delete: " + ex.Message });
            }
        }
        //FUNCTION MASTER END

        //FUNCTION TRANSACTION
        // ==========================================
        // CRUD: AUDIT EVENT
        // ==========================================
        [HttpGet]
        public JsonResult GetAuditData(int page = 1, string searchString = null)
        {
            int pageSize = 6;

            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }

            IQueryable<Quality_EAudit_Audit_Event> query;

            if (userAccess.access == "Administrator")
            {
                query = dbq.Quality_EAudit_Audit_Event.AsQueryable();
            }
            else
            {
                query = dbq.Quality_EAudit_Audit_Event
                           .Where(evt => evt.Stakeholders.Any(stk => stk.Auditors.Any(aud => aud.nik == userNik)));
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(a => a.audit_name.Contains(searchString) ||
                                         a.PIC.Contains(searchString));
            }
            query = query.Where(a => a.deleted_at == null);

            if (query == null)
            {
                return Json(new
                {
                    success = true,
                    message = "no data"
                }, JsonRequestBehavior.AllowGet);
            }

            query = query.OrderByDescending(a => a.created_at);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedData = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = pagedData.Select(a => new
            {
                a.audit_event_id,
                a.audit_name,
                a.audit_type,
                a.audit_status,
                a.audit_standard,
                a.PIC,
                a.fiscal_year,
                created_at = a.created_at.ToString("dd MMM yyyy")
            });

            return Json(new
            {
                success = true,
                data = result,
                currentPage = page,
                totalPages = totalPages
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateEvent(CreateAuditDto model)
        {
            try
            {
                var currUser = (ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(userNik))
                {
                    return Json(new { success = false, message = "Sesi telah habis, silakan login kembali." }, JsonRequestBehavior.AllowGet);
                }

                var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

                if (userProfile == null)
                {
                    return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);
                }

                var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

                if (userAccess == null)
                {
                    return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);
                }

                using (var transaction = dbq.Database.BeginTransaction())
                {
                    try
                    {
                        var newId = $"{model.audit_type.Substring(0, 3).ToUpper()}|{model.audit_standard.Substring(0, 3).ToUpper()}|{DateTime.Now:yyyyMMddHHmmss}|{Guid.NewGuid().ToString().Substring(0, 4)}";
                        var newEvent = new Quality_EAudit_Audit_Event
                        {
                            audit_event_id = newId,
                            audit_type = model.audit_type,
                            audit_standard = model.audit_standard,
                            audit_name = model.audit_name,
                            fiscal_year = model.fiscal_year,
                            PIC = model.PIC,
                            audit_status = "PENDING",
                            created_at = DateTime.Now,
                            updated_at = null,
                            deleted_at = null
                        };

                        dbq.Quality_EAudit_Audit_Event.Add(newEvent);
                        dbq.SaveChanges();

                        var allSections = dbq.Quality_EAudit_Section.ToList();
                        var allQuestions = dbq.Quality_EAudit_Question_Master.ToList();
                        var allAuditors = dbq.Quality_EAudit_Audity_Auditor_Master
                                             .Include(a => a.AuditorDetails)
                                             .ToList();

                        var listStakeholders = new List<Quality_EAudit_Audit_Event_Stakeholder>();
                        var listQuestions = new List<Quality_EAudit_Question_Event>();
                        var listAuditors = new List<Quality_EAudit_Audity_Auditor_Event>();

                        foreach (var item in allSections)
                        {
                            // 1. Pengamanan Substring (Mencegah error jika nama section < 2 karakter)
                            var sectionPrefix = item.section_name.Length >= 2
                                ? item.section_name.Substring(0, 2).ToUpper()
                                : item.section_name.ToUpper();

                            // 2. Suntikkan 4 digit GUID acak untuk memastikan ID selalu unik
                            var dateString = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();

                            var stakeholderId = $"DAU-{sectionPrefix}-{dateString}-{uniqueSuffix}";

                            listStakeholders.Add(new Quality_EAudit_Audit_Event_Stakeholder
                            {
                                audit_event_stakeholder_id = stakeholderId,
                                responsibility_section = item.section_name,
                                start_event = model.audit_start,
                                end_event = model.audit_end,
                                status = "PENDING",
                                audit_event_id = newId,
                                created_at = DateTime.Now
                            });

                            var questions = allQuestions.Where(x => x.section_id == item.section_id).ToList();
                            int questionCounter = 1;

                            foreach (var q in questions)
                            {
                                var questionId = $"QUID-{sectionPrefix}-{dateString}-{uniqueSuffix}-{questionCounter.ToString("D3")}";

                                listQuestions.Add(new Quality_EAudit_Question_Event
                                {
                                    question_id = questionId,
                                    audit_event_stakeholder_id = stakeholderId,
                                    category_audit = q.category_audit,
                                    clausul = q.clausul,
                                    process_name = q.process_name,
                                    question = q.question,
                                    created_at = DateTime.Now
                                });

                                questionCounter++;
                            }

                            var auditors = allAuditors.Where(x => x.AuditorDetails.Any(d => d.section_id == item.section_id && d.Deleted_at == null)).ToList();
                            int auditorsCounter = 1;

                            foreach (var a in auditors)
                            {
                                var matchedDetails = a.AuditorDetails
                                                      .Where(d => d.section_id == item.section_id && d.Deleted_at == null)
                                                      .ToList();

                                foreach (var detail in matchedDetails)
                                {
                                    var auditorId = $"AUID-{sectionPrefix}-{dateString}-{uniqueSuffix}-{auditorsCounter.ToString("D3")}";

                                    listAuditors.Add(new Quality_EAudit_Audity_Auditor_Event
                                    {
                                        audity_auditor_id = auditorId,
                                        audit_event_stakeholder_id = stakeholderId,
                                        person_name = a.person_name,
                                        nik = a.nik,
                                        role = a.role,
                                        category_audit = detail.category_audit,
                                        email = a.email,
                                        status = a.status,
                                        section = a.section,
                                        created_at = DateTime.Now
                                    });

                                    auditorsCounter++;
                                }
                            }
                        }

                        dbq.Quality_EAudit_Audit_Event_Stakeholder.AddRange(listStakeholders);
                        dbq.Quality_EAudit_Question_Event.AddRange(listQuestions);
                        dbq.Quality_EAudit_Audity_Auditor_Event.AddRange(listAuditors);

                        dbq.SaveChanges();
                        transaction.Commit();

                        return Json(new { success = true, message = "Event berhasil dibuat" });
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                    {
                        transaction.Rollback();
                        var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");

                        var fullErrorMessage = string.Join("; ", errorMessages);
                        return Json(new { success = false, message = "Gagal Validasi: " + fullErrorMessage });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Exception deepestEx = ex;
                        while (deepestEx.InnerException != null)
                        {
                            deepestEx = deepestEx.InnerException;
                        }
                        return Json(new { success = false, message = "Gagal DB: " + deepestEx.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi Kesalahan Server: " + ex.Message });
            }
        }

        public JsonResult UpdateEvent(string id, CreateAuditDto dto)
        {
            try
            {
                var currUser = (ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(userNik))
                {
                    return Json(new { success = false, message = "Sesi telah habis, silakan login kembali." }, JsonRequestBehavior.AllowGet);
                }

                var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

                if (userProfile == null)
                {
                    return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);
                }

                var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

                if (userAccess == null)
                {
                    return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);
                }

                var item = dbq.Quality_EAudit_Audit_Event.Find(id);

                if (item == null) return Json(new { success = false, message = "Data Event tidak ditemukan." });

                item.audit_type = dto.audit_type;
                item.audit_standard = dto.audit_standard;
                item.audit_name = dto.audit_name;
                item.fiscal_year = dto.fiscal_year;
                item.PIC = dto.PIC;

                dbq.SaveChanges();

                return Json(new { success = true, message = "Event berhasil diupdate" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi Kesalahan Server: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult DeleteEvent(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();


            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbq.Quality_EAudit_Audit_Event.Find(id);
                if (item == null) return Json(new { success = false, message = "Data Event tidak ditemukan." });

                item.deleted_at = DateTime.Now;
                dbq.SaveChanges();

                return Json(new { success = true, message = "Audit Event berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal delete: " + ex.Message });
            }
        }

        // ==========================================
        // CRUD: AUDITIE AUDITOR EVENT
        // ==========================================

        [HttpPost]
        public JsonResult AddPerson(AuditorAuditeeDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden. Hanya Admin yang bisa menambahkan." });

            try
            {
                if (string.IsNullOrEmpty(dto.audit_event_stakeholder_id))
                    return Json(new { success = false, message = "Stakeholder ID tidak boleh kosong." });

                var newPerson = new Quality_EAudit_Audity_Auditor_Event
                {
                    audity_auditor_id = Guid.NewGuid().ToString(),
                    audit_event_stakeholder_id = dto.audit_event_stakeholder_id,
                    person_name = dto.person_name,
                    nik = dto.nik,
                    role = dto.role,
                    email = dto.email,
                    category_audit = dto.category_audit,
                    section = dto.section,
                    status = "Active",
                    created_at = DateTime.Now
                };

                dbq.Quality_EAudit_Audity_Auditor_Event.Add(newPerson);
                dbq.SaveChanges();

                return Json(new { success = true, message = dto.role + " berhasil ditambahkan.", data = newPerson.audity_auditor_id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal menyimpan: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdatePerson(string id, AuditorAuditeeDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden." });

            try
            {
                var item = dbq.Quality_EAudit_Audity_Auditor_Event.Find(id);

                if (item == null || item.deleted_at != null)
                    return Json(new { success = false, message = "Data tidak ditemukan." });

                item.person_name = dto.person_name;
                item.nik = dto.nik;
                item.role = dto.role;
                item.email = dto.email;
                item.category_audit = dto.category_audit;
                item.section = dto.section;
                item.updated_at = DateTime.Now;

                dbq.SaveChanges();

                return Json(new { success = true, message = "Data berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeletePerson(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden." });

            try
            {
                var item = dbq.Quality_EAudit_Audity_Auditor_Event.Find(id);

                if (item == null || item.deleted_at != null)
                    return Json(new { success = false, message = "Data tidak ditemukan." });

                item.deleted_at = DateTime.Now;
                item.status = "Deleted";

                dbq.SaveChanges();

                return Json(new { success = true, message = item.role + " berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal delete: " + ex.Message });
            }
        }


        public JsonResult GetDataStakeholderList(string eventId)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(userNik))
                    return Json(new { success = false, message = "Sesi telah habis." }, JsonRequestBehavior.AllowGet);

                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                if (userProfile == null)
                    return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik && w.deleted_at == null);
                if (userAccess == null)
                    return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

                bool isBackendAdmin = userAccess.access == "Administrator";

                var query = dbq.Quality_EAudit_Audit_Event_Stakeholder
                    .Where(x => x.audit_event_id == eventId && x.deleted_at == null);

                if (!isBackendAdmin)
                {
                    query = query.Where(s => dbq.Quality_EAudit_Audity_Auditor_Event
                        .Any(mapping => mapping.audit_event_stakeholder_id == s.audit_event_stakeholder_id
                                     && mapping.nik == userNik
                                     && mapping.deleted_at == null));
                }

                var data = query.Select(x => new
                {
                    stakeholder_id = x.audit_event_stakeholder_id,
                    section_name = x.responsibility_section,
                    start_event = x.start_event,
                    end_event = x.end_event,
                    status = x.status,

                    has_system = dbq.Quality_EAudit_Question_Event.Any(q => q.audit_event_stakeholder_id == x.audit_event_stakeholder_id && q.category_audit == "System" && q.deleted_at == null),
                    has_process = dbq.Quality_EAudit_Question_Event.Any(q => q.audit_event_stakeholder_id == x.audit_event_stakeholder_id && q.category_audit == "Process" && q.deleted_at == null),
                    has_product = dbq.Quality_EAudit_Question_Event.Any(q => q.audit_event_stakeholder_id == x.audit_event_stakeholder_id && q.category_audit == "Product" && q.deleted_at == null),

                    user_can_system = isBackendAdmin || dbq.Quality_EAudit_Audity_Auditor_Event.Any(m => m.audit_event_stakeholder_id == x.audit_event_stakeholder_id && m.nik == userNik && m.category_audit == "System" && m.deleted_at == null),
                    user_can_process = isBackendAdmin || dbq.Quality_EAudit_Audity_Auditor_Event.Any(m => m.audit_event_stakeholder_id == x.audit_event_stakeholder_id && m.nik == userNik && m.category_audit == "Process" && m.deleted_at == null),
                    user_can_product = isBackendAdmin || dbq.Quality_EAudit_Audity_Auditor_Event.Any(m => m.audit_event_stakeholder_id == x.audit_event_stakeholder_id && m.nik == userNik && m.category_audit == "Product" && m.deleted_at == null),

                    user_role = dbq.Quality_EAudit_Audity_Auditor_Event
                                   .Where(m => m.audit_event_stakeholder_id == x.audit_event_stakeholder_id && m.nik == userNik && m.deleted_at == null)
                                   .Select(m => m.role)
                                   .FirstOrDefault()
                }).ToList();

                var formattedData = data.Select(x => new
                {
                    stakeholder_id = x.stakeholder_id,
                    section_name = x.section_name,
                    start_event = x.start_event.HasValue ? x.start_event.Value.ToString("dd MMM yyyy") : null,
                    end_event = x.end_event.HasValue ? x.end_event.Value.ToString("dd MMM yyyy") : null,
                    status = x.status ?? "Pending",
                    has_system = x.has_system,
                    has_process = x.has_process,
                    has_product = x.has_product,
                    user_can_system = x.user_can_system,
                    user_can_process = x.user_can_process,
                    user_can_product = x.user_can_product,

                    user_role = !string.IsNullOrEmpty(x.user_role) ? x.user_role : (isBackendAdmin ? "Administrator" : "")
                }).ToList();

                return Json(new { success = true, data = formattedData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi Kesalahan Server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpPost]
        public JsonResult CreateStakeholder(string eventId, CreateStakeholderDto dto)
        {
            try
            {
                var currUser = (ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(userNik))
                    return Json(new { success = false, message = "Sesi telah habis, silakan login kembali." });

                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                if (userProfile == null)
                    return Json(new { success = false, message = "UnAuthorization" });

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
                if (userAccess == null)
                    return Json(new { success = false, message = "Forbidden" });

                if (dto.ListSection == null || !dto.ListSection.Any())
                    return Json(new { success = false, message = "Pilih minimal satu section." });

                var auditEvent = dbq.Quality_EAudit_Audit_Event.Find(eventId);
                if (auditEvent == null || auditEvent.deleted_at != null)
                    return Json(new { success = false, message = "Event Audit tidak ditemukan." });

                using (var transaction = dbq.Database.BeginTransaction())
                {
                    try
                    {
                        var allSections = dbq.Quality_EAudit_Section
                                             .Where(s => dto.ListSection.Contains(s.section_name))
                                             .ToList();

                        var allQuestions = dbq.Quality_EAudit_Question_Master.ToList();
                        var allAuditors = dbq.Quality_EAudit_Audity_Auditor_Master
                                             .Include(a => a.AuditorDetails)
                                             .ToList();

                        var existingSections = dbq.Quality_EAudit_Audit_Event_Stakeholder
                                                  .Where(x => x.audit_event_id == eventId && x.deleted_at == null)
                                                  .Select(x => x.responsibility_section)
                                                  .ToList();

                        var listStakeholders = new List<Quality_EAudit_Audit_Event_Stakeholder>();
                        var listQuestions = new List<Quality_EAudit_Question_Event>();
                        var listAuditors = new List<Quality_EAudit_Audity_Auditor_Event>();

                        int addedCount = 0;

                        foreach (var sectionMaster in allSections)
                        {
                            if (existingSections.Contains(sectionMaster.section_name))
                                continue;

                            var dateString = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            var stakeholderId = $"DAU-{sectionMaster.section_name.Substring(0, 2).ToUpper()}-{dateString}-{Guid.NewGuid().ToString().Substring(0, 4)}";

                            listStakeholders.Add(new Quality_EAudit_Audit_Event_Stakeholder
                            {
                                audit_event_stakeholder_id = stakeholderId,
                                audit_event_id = eventId,
                                responsibility_section = sectionMaster.section_name,
                                start_event = null,
                                end_event = null,
                                status = "PENDING",
                                created_at = DateTime.Now
                            });

                            var questions = allQuestions.Where(x => x.section_id == sectionMaster.section_id);
                            foreach (var q in questions)
                            {
                                var questionId = $"QUID-{sectionMaster.section_name.Substring(0, 2).ToUpper()}-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}-{Guid.NewGuid().ToString().Substring(0, 4)}";
                                listQuestions.Add(new Quality_EAudit_Question_Event
                                {
                                    question_id = questionId,
                                    audit_event_stakeholder_id = stakeholderId,
                                    category_audit = q.category_audit,
                                    clausul = q.clausul,
                                    process_name = q.process_name,
                                    question = q.question,
                                    created_at = DateTime.Now
                                });
                            }

                            var auditors = allAuditors.Where(x => x.AuditorDetails.Any(d => d.section_id == sectionMaster.section_id && d.Deleted_at == null));

                            foreach (var a in auditors)
                            {
                                var matchedDetails = a.AuditorDetails
                                                      .Where(d => d.section_id == sectionMaster.section_id && d.Deleted_at == null)
                                                      .ToList();

                                foreach (var detail in matchedDetails)
                                {
                                    listAuditors.Add(new Quality_EAudit_Audity_Auditor_Event
                                    {
                                        audity_auditor_id = Guid.NewGuid().ToString(),
                                        audit_event_stakeholder_id = stakeholderId,
                                        person_name = a.person_name,
                                        nik = a.nik,
                                        role = a.role,
                                        category_audit = detail.category_audit,
                                        email = a.email,
                                        status = a.status,
                                        section = a.section,
                                        created_at = DateTime.Now
                                    });
                                }
                            }

                            addedCount++;
                        }

                        if (addedCount == 0)
                        {
                            return Json(new { success = false, message = "Semua section yang dipilih sudah ada di Event ini (Duplikat)." });
                        }

                        dbq.Quality_EAudit_Audit_Event_Stakeholder.AddRange(listStakeholders);
                        dbq.Quality_EAudit_Question_Event.AddRange(listQuestions);
                        dbq.Quality_EAudit_Audity_Auditor_Event.AddRange(listAuditors);

                        dbq.SaveChanges();
                        transaction.Commit();

                        return Json(new { success = true, message = $"{addedCount} Stakeholder berhasil ditambahkan ke Event." });
                    }
                    catch (DbEntityValidationException ex)
                    {
                        transaction.Rollback();
                        var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                        return Json(new { success = false, message = "Gagal Validasi: " + string.Join("; ", errorMessages) });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Gagal menyimpan ke database: " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi Kesalahan Server: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateStakeholder(string id, StakeholderDto dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                var item = dbq.Quality_EAudit_Audit_Event_Stakeholder.Find(id);

                if (item == null || item.deleted_at != null)
                {
                    return Json(new { success = false, message = "Data tidak ditemukan atau sudah dihapus." });
                }

                item.start_event = dto.start_event;
                item.end_event = dto.end_event;
                item.updated_at = DateTime.Now;

                dbq.SaveChanges();

                return Json(new { success = true, message = "Stakeholder berhasil diupdate." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteStakeholder(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                var item = dbq.Quality_EAudit_Audit_Event_Stakeholder.Find(id);

                if (item != null)
                {
                    item.deleted_at = DateTime.Now;
                    dbq.SaveChanges();

                    return Json(new { success = true, message = "Stakeholder berhasil dihapus." });
                }

                return Json(new { success = false, message = "Data tidak ditemukan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetAuditNote(string stakeholderId, string categoryAudit)
        {
            try
            {
                var existingRecord = dbq.Quality_EAudit_Audit_Note
                    .FirstOrDefault(x => x.audit_event_stakeholder_id == stakeholderId
                                      && x.category_audit == categoryAudit
                                      && x.deleted_at == null);

                var noteContent = existingRecord != null ? existingRecord.note : "";

                return Json(new { success = true, data = noteContent }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to load note: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveAuditNote(AuditNoteSubmitDto dto)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(userNik))
                    return Json(new { success = false, message = "Session expired." });

                if (string.IsNullOrEmpty(dto.audit_event_stakeholder_id))
                    return Json(new { success = false, message = "Invalid Stakeholder ID." });

                var existingNote = dbq.Quality_EAudit_Audit_Note
                    .FirstOrDefault(x => x.audit_event_stakeholder_id == dto.audit_event_stakeholder_id
                                      && x.category_audit == dto.category_audit
                                      && x.deleted_at == null);

                if (existingNote != null)
                {
                    existingNote.note = dto.note;
                    existingNote.updated_at = DateTime.Now;
                }
                else
                {
                    dbq.Quality_EAudit_Audit_Note.Add(new Quality_EAudit_Audit_Note
                    {
                        audit_note_id = "NOT-" + Guid.NewGuid().ToString("N").Substring(0, 16),
                        audit_event_stakeholder_id = dto.audit_event_stakeholder_id,
                        category_audit = dto.category_audit,
                        note = dto.note,
                        created_at = DateTime.Now
                    });
                }

                dbq.SaveChanges();

                return Json(new { success = true, message = "Audit Note saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to save note: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetAuditGuides(string stakeholderId, string categoryAudit)
        {
            try
            {
                var guides = dbq.Quality_EAudit_Audit_Guide
                    .Where(x => x.audit_event_stakeholder_id == stakeholderId
                             && x.category_audit == categoryAudit
                             && x.deleted_at == null)
                    .Select(x => x.guide_name)
                    .ToList();

                return Json(new { success = true, data = guides }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to load guides: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveAuditGuides(AuditGuideSubmitDto dto)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(userNik))
                    return Json(new { success = false, message = "Session expired." });

                if (string.IsNullOrEmpty(dto.audit_event_stakeholder_id))
                    return Json(new { success = false, message = "Invalid Stakeholder ID." });

                var submittedGuides = dto.selected_guides ?? new List<string>();

                var existingGuides = dbq.Quality_EAudit_Audit_Guide
                    .Where(x => x.audit_event_stakeholder_id == dto.audit_event_stakeholder_id
                             && x.category_audit == dto.category_audit)
                    .ToList();

                var guidesToDelete = existingGuides
                    .Where(e => !submittedGuides.Contains(e.guide_name))
                    .ToList();

                var guidesToKeep = existingGuides
                    .Where(e => submittedGuides.Contains(e.guide_name))
                    .Select(e => e.guide_name)
                    .ToList();

                if (guidesToDelete.Any())
                {
                    dbq.Quality_EAudit_Audit_Guide.RemoveRange(guidesToDelete);
                }

                foreach (var sel in submittedGuides)
                {
                    if (!guidesToKeep.Contains(sel))
                    {
                        dbq.Quality_EAudit_Audit_Guide.Add(new Quality_EAudit_Audit_Guide
                        {
                            audit_guide_id = "GUI-" + Guid.NewGuid().ToString("N").Substring(0, 16),
                            audit_event_stakeholder_id = dto.audit_event_stakeholder_id,
                            category_audit = dto.category_audit,
                            guide_name = sel,
                            created_at = DateTime.Now
                        });
                    }
                }

                dbq.SaveChanges();

                return Json(new { success = true, message = "Audit Guide updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to save guides: " + ex.Message });
            }
        }

        [HttpPost]
        // 1. Tambahkan parameter category_audit
        public JsonResult SubmitAuditSection(string audit_event_stakeholder_id, string category_audit)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder.Find(audit_event_stakeholder_id);
                string eventId = stakeholder != null ? stakeholder.audit_event_id : "";
                if (stakeholder == null)
                {
                    return Json(new { success = false, message = "Stakeholder tidak ditemukan." });
                }

                var items = dbq.Quality_EAudit_Question_Event
                               .Where(a => a.audit_event_stakeholder_id == audit_event_stakeholder_id && a.deleted_at == null)
                               .ToList();

                if (items.Any())
                {
                    // ==============================================================
                    // 1. LOGIKA STATUS BERDASARKAN PROCESS, SYSTEM, PRODUCT
                    // ==============================================================
                    var processItems = items.Where(x => x.category_audit == "Process").ToList();
                    var systemItems = items.Where(x => x.category_audit == "System").ToList();
                    var productItems = items.Where(x => x.category_audit == "Product").ToList();

                    bool hasProcess = processItems.Any();
                    bool hasSystem = systemItems.Any();
                    bool hasProduct = productItems.Any();

                    bool processAllAnswered = hasProcess && processItems.All(x => !string.IsNullOrWhiteSpace(x.score));
                    bool systemAllAnswered = hasSystem && systemItems.All(x => !string.IsNullOrWhiteSpace(x.score));
                    bool productAllAnswered = hasProduct && productItems.All(x => !string.IsNullOrWhiteSpace(x.score));

                    string currentStatus = stakeholder.status ?? "";
                    bool isFullySubmitted = currentStatus == "Submitted";

                    bool processCompleted = hasProcess && (processAllAnswered || category_audit == "Process" || isFullySubmitted || currentStatus.Contains("Process Submitted"));

                    bool systemCompleted = hasSystem && (systemAllAnswered || category_audit == "System" || isFullySubmitted || currentStatus.Contains("System Submitted"));

                    bool productCompleted = hasProduct && (productAllAnswered || category_audit == "Product" || isFullySubmitted || currentStatus.Contains("Product Submitted"));

                    bool allCompleted = true;
                    if (hasProcess && !processCompleted) allCompleted = false;
                    if (hasSystem && !systemCompleted) allCompleted = false;
                    if (hasProduct && !productCompleted) allCompleted = false;

                    string finalStatus = "";

                    if (allCompleted)
                    {
                        finalStatus = "Submitted";
                    }
                    else
                    {
                        List<string> statusDetails = new List<string>();

                        if (hasProcess) statusDetails.Add(processCompleted ? "Process Submitted" : "Process On Progress");
                        if (hasSystem) statusDetails.Add(systemCompleted ? "System Submitted" : "System On Progress");
                        if (hasProduct) statusDetails.Add(productCompleted ? "Product Submitted" : "Product On Progress");

                        finalStatus = string.Join("<br>", statusDetails);
                    }

                    stakeholder.status = finalStatus;

                    var now = DateTime.Now;
                    string dateStr = now.ToString("yyyyMMdd");
                    string namaSection = stakeholder.responsibility_section ?? "UnknownSection";

                    string namaSectionSingkat = namaSection;
                    int startIndex = namaSection.IndexOf('(');
                    int endIndex = namaSection.IndexOf(')');

                    if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                    {
                        namaSectionSingkat = namaSection.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                    }
                    List<string> generatedOfiIds = new List<string>();
                    List<string> generatedCarIds = new List<string>();

                    // ==============================================================
                    // 2. LOGIKA AUTO-CREATE, UPDATE, & DELETE OFI
                    // ==============================================================
                    var ofiQuestions = items.Where(x => !string.IsNullOrWhiteSpace(x.judgement) &&
                                                        x.judgement.Trim().ToUpper() == "OFI").ToList();

                    var nonOfiQuestions = items.Where(x => string.IsNullOrWhiteSpace(x.judgement) ||
                                                           x.judgement.Trim().ToUpper() != "OFI").ToList();

                    foreach (var nofi in nonOfiQuestions)
                    {
                        var existingOfiDetails = dbq.Quality_EAudit_OFI_Detail.Where(d => d.question_id == nofi.question_id).ToList();
                        if (existingOfiDetails.Any())
                        {
                            dbq.Quality_EAudit_OFI_Detail.RemoveRange(existingOfiDetails);
                        }
                    }

                    var existingHeader = dbq.Quality_EAudit_OFI_Header
                                            .FirstOrDefault(h => h.audit_event_stakeholder_id == audit_event_stakeholder_id && h.deleted_at == null);
                    string ofiHeaderId = "";

                    if (ofiQuestions.Any())
                    {
                        if (existingHeader == null)
                        {
                            string ofiPrefix = $"OFI-IA-{namaSectionSingkat}-{dateStr}";
                            int ofiSeq = dbq.Quality_EAudit_OFI_Header.Count(x => x.ofi_header_id.StartsWith(ofiPrefix)) + 1;
                            ofiHeaderId = $"{ofiPrefix}-{ofiSeq:D3}";

                            var newHeader = new Quality_EAudit_OFI_Header
                            {
                                ofi_header_id = ofiHeaderId,
                                audit_event_stakeholder_id = audit_event_stakeholder_id,
                                reply_deadline = now.AddDays(30).ToString("yyyy-MM-dd"),
                                status = "Draft",
                                created_at = now
                            };
                            dbq.Quality_EAudit_OFI_Header.Add(newHeader);

                            string[] signTypes = { "Dibuat", "Diperiksa", "DiSetujui" };
                            foreach (var type in signTypes)
                            {
                                var newApproval = new Quality_EAudit_OFI_Approval
                                {
                                    approval_id = $"APV-{Guid.NewGuid()}",
                                    ofi_header_id = ofiHeaderId,
                                    sign_type = type
                                };
                                dbq.Quality_EAudit_OFI_Approval.Add(newApproval);
                            }
                            generatedOfiIds.Add(ofiHeaderId);
                        }
                        else
                        {
                            ofiHeaderId = existingHeader.ofi_header_id;
                        }

                        foreach (var q in ofiQuestions)
                        {
                            var existingDetail = dbq.Quality_EAudit_OFI_Detail.FirstOrDefault(d => d.question_id == q.question_id);
                            if (existingDetail == null)
                            {
                                var newDetail = new Quality_EAudit_OFI_Detail
                                {
                                    ofi_detail_id = $"OFI-DTL-{Guid.NewGuid()}",
                                    ofi_header_id = ofiHeaderId,
                                    question_id = q.question_id,
                                    clause = q.clausul,
                                    observation_comment = q.note ?? "",
                                    action_plan = "",
                                    action_date = null,
                                };
                                dbq.Quality_EAudit_OFI_Detail.Add(newDetail);
                            }
                            else
                            {
                                // UPDATE jika klausul atau note berubah
                                existingDetail.clause = q.clausul;
                                existingDetail.observation_comment = q.note ?? "";
                            }
                        }
                    }
                    else
                    {
                        if (existingHeader != null)
                        {
                            existingHeader.deleted_at = now;

                        }
                    }

                    // ==============================================================
                    // PERSIAPAN UNTUK CAR HEADER (Tarik data Auditor & Auditee)
                    // ==============================================================
                    var allAuditees = dbq.Quality_EAudit_Audity_Auditor_Event
                                            .Where(a => a.audit_event_stakeholder_id == audit_event_stakeholder_id && a.role == "Auditee" && a.deleted_at == null)
                                            .Select(a => new { a.person_name, a.category_audit, a.email, a.nik, a.role })
                                            .Distinct()
                                            .ToList();

                    var allAuditors = dbq.Quality_EAudit_Audity_Auditor_Event
                                            .Where(a => a.audit_event_stakeholder_id == audit_event_stakeholder_id && a.role == "Auditor" && a.deleted_at == null)
                                            .Select(a => new { a.person_name, a.category_audit, a.email, a.nik, a.role })
                                            .Distinct()
                                            .ToList();

                    // ==============================================================
                    // 3. LOGIKA AUTO-CREATE, UPDATE, & DELETE CAR
                    // ==============================================================
                    var carQuestions = items.Where(x => !string.IsNullOrWhiteSpace(x.judgement) &&
                                                        (x.judgement.Trim().ToUpper() == "MINOR" || x.judgement.Trim().ToUpper() == "MAJOR")).ToList();

                    var nonCarQuestions = items.Where(x => string.IsNullOrWhiteSpace(x.judgement) ||
                                                           (x.judgement.Trim().ToUpper() != "MINOR" && x.judgement.Trim().ToUpper() != "MAJOR")).ToList();

                    foreach (var q in nonCarQuestions)
                    {
                        var existingCars = dbq.Quality_EAudit_CAR_Header.Where(c => c.question_id == q.question_id && c.deleted_at == null).ToList();
                        foreach (var car in existingCars)
                        {
                            car.deleted_at = now;

                        }
                    }

                    string carPrefix = $"CAR-IA-{namaSectionSingkat}-{dateStr}";
                    int carSeq = dbq.Quality_EAudit_CAR_Header.Count(x => x.no_report.StartsWith(carPrefix)) + 1;

                    foreach (var q in carQuestions)
                    {
                        var existingCar = dbq.Quality_EAudit_CAR_Header.FirstOrDefault(c => c.question_id == q.question_id && c.deleted_at == null);

                        string judgementStr = q.judgement.Trim().ToUpper();
                        string strDeadline = judgementStr == "MINOR" ? now.AddDays(30).ToString("yyyy-MM-dd") : now.AddDays(7).ToString("yyyy-MM-dd");

                        string insiden_status = (q.criteria != null && q.criteria.ToLower().Contains("berulang")) ? "Pernah" : "Pertama";

                        var finding = dbq.Quality_EAudit_Finding_Question.FirstOrDefault(f => f.question_id == q.question_id && f.deleted_at == null);
                        string probName = finding != null ? finding.problem : null;
                        string probLocation = finding != null ? finding.location : null;
                        string objName = finding != null ? finding.object_name : null;
                        string reference = finding != null ? finding.reference : null;
                        string detailNote = q.note ?? null;
                        string combinedDetail = $"PROBLEM : {probName}\nLOCATION : {probLocation}\nOBJECT : {objName}\nREFERENCE : {reference}\nDETAIL : {detailNote}";

                        if (existingCar == null)
                        {
                            string newNoReport = $"{carPrefix}-{carSeq:D3}";
                            carSeq++;

                            string namaAuditieFiltered = string.Join(", ", allAuditees
                                    .Where(a => a.category_audit == q.category_audit)
                                    .Select(a => a.person_name));

                            string namaAuditorFiltered = string.Join(", ", allAuditors
                                                                .Where(a => a.category_audit == q.category_audit)
                                                                .Select(a => a.person_name));

                            namaAuditieFiltered = string.IsNullOrWhiteSpace(namaAuditieFiltered) ? null : namaAuditieFiltered;
                            namaAuditorFiltered = string.IsNullOrWhiteSpace(namaAuditorFiltered) ? null : namaAuditorFiltered;
                            var newCarHeader = new Quality_EAudit_CAR_Header
                            {
                                no_report = newNoReport,
                                question_id = q.question_id,
                                date = now.Date,
                                to_Dept = stakeholder.responsibility_section ?? "",
                                attention = namaAuditieFiltered,
                                from_auditor = namaAuditorFiltered,
                                incident_status = insiden_status,
                                reply_deadline = strDeadline,
                                created_at = now
                            };
                            dbq.Quality_EAudit_CAR_Header.Add(newCarHeader);

                            var newCarAction = new Quality_EAudit_CAR_Action
                            {
                                action_id = $"ACT-{Guid.NewGuid()}",
                                no_report = newNoReport,
                                PIC_dept = stakeholder.responsibility_section ?? ""
                            };

                            dbq.Quality_EAudit_CAR_Action.Add(newCarAction);

                            var newCarProblem = new Quality_EAudit_CAR_Problem
                            {
                                problem_id = $"PRB-{Guid.NewGuid()}",
                                no_report = newNoReport,
                                problem_name = probName,
                                problem_location = probLocation,
                                problem_detail = combinedDetail,
                                problem_category = "Internal Audit",
                                problem_status = judgementStr
                            };
                            dbq.Quality_EAudit_CAR_Problem.Add(newCarProblem);

                            string[] groupTypes = { "Auditor Approval", "Auditee Approval" };
                            string[] signTypesCar = { "Dibuat", "Diperiksa", "DiSetujui" };

                            foreach (var group in groupTypes)
                            {
                                foreach (var sign in signTypesCar)
                                {
                                    var newApproval = new Quality_EAudit_CAR_Approval
                                    {
                                        approval_id = $"APV-{Guid.NewGuid()}",
                                        no_report = newNoReport,
                                        group_type = group,
                                        role_type = sign,
                                    };
                                    dbq.Quality_EAudit_CAR_Approval.Add(newApproval);
                                }
                            }
                            generatedCarIds.Add(newNoReport);
                        }
                        else
                        {
                            existingCar.reply_deadline = strDeadline;
                            existingCar.incident_status = insiden_status;

                            var existingProblem = dbq.Quality_EAudit_CAR_Problem.FirstOrDefault(p => p.no_report == existingCar.no_report);
                            if (existingProblem != null)
                            {
                                existingProblem.problem_status = judgementStr;
                                existingProblem.problem_detail = combinedDetail;
                            }

                            var existingAction = dbq.Quality_EAudit_CAR_Action.FirstOrDefault(a => a.no_report == existingCar.no_report);
                            if (existingAction == null)
                            {
                                var newCarAction = new Quality_EAudit_CAR_Action
                                {
                                    action_id = $"ACT-{Guid.NewGuid()}",
                                    no_report = existingCar.no_report,
                                    PIC_dept = stakeholder.responsibility_section ?? ""
                                };
                                dbq.Quality_EAudit_CAR_Action.Add(newCarAction);
                            }
                        }
                    }

                    dbq.SaveChanges();

                    if (generatedOfiIds.Any() || generatedCarIds.Any())
                    {
                        var auditorEmails = allAuditors.Select(x => x.email).ToList();
                        var auditeeEmails = allAuditees.Select(x => x.email).ToList();

                        var targetEmails = auditorEmails.Concat(auditeeEmails)
                                                        .Where(email => !string.IsNullOrWhiteSpace(email))
                                                        .Distinct()
                                                        .ToList();

                        if (targetEmails.Any())
                        {
                            SendEmailNotificationFindingsProblem(targetEmails, generatedOfiIds, generatedCarIds);
                        }
                    }

                    return Json(new { success = true, message = finalStatus, eventId = eventId });
                }
                else
                {
                    return Json(new { success = false, message = "Data pertanyaan tidak ditemukan untuk stakeholder ini." });
                }
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMsg += " | DB Error: " + ex.InnerException.InnerException.Message;
                    }
                }

                return Json(new { success = false, message = "Error: " + errorMsg });
            }
        }
        // ==========================================
        // CRUD: QUESTION EVENT
        // ==========================================
        [HttpGet]
        public JsonResult GetAuditInfo(string stakeholderId)
        {
            try
            {
                var info = dbq.Quality_EAudit_Audit_Event_Stakeholder
                    .Include(e => e.AuditEvent)
                    .Where(s => s.audit_event_stakeholder_id == stakeholderId && s.deleted_at == null)
                    .Select(s => new VmStakeholderInfo
                    {
                        stakeholder_id = s.audit_event_stakeholder_id,
                        section_name = s.responsibility_section,
                        audit_name = s.AuditEvent.audit_name,
                        start_event = s.start_event,
                        end_event = s.end_event
                    })
                    .FirstOrDefault();

                if (info == null)
                {
                    return Json(new { success = false, message = "Stakeholder / Audit Info not found" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, data = info }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetAuditAuditor(string stakeholderId, string type)
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    return Json(new { success = false, message = "Audit type is required" }, JsonRequestBehavior.AllowGet);
                }

                var auditors = dbq.Quality_EAudit_Audity_Auditor_Event
                    .Where(a => a.audit_event_stakeholder_id == stakeholderId
                             && a.deleted_at == null
                             && a.role == "Auditor"
                             && a.category_audit.Trim().ToLower() == type.Trim().ToLower())
                    .Select(a => new VmAuditor
                    {
                        audity_auditor_id = a.audity_auditor_id,
                        person_name = a.person_name,
                        role = a.role,
                        email = a.email,
                        nik = a.nik
                    })
                    .ToList();

                return Json(new { success = true, data = auditors }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetAuditAuditie(string stakeholderId, string type)
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    return Json(new { success = false, message = "Audit type is required" }, JsonRequestBehavior.AllowGet);
                }

                var auditors = dbq.Quality_EAudit_Audity_Auditor_Event
                    .Where(a => a.audit_event_stakeholder_id == stakeholderId
                             && a.deleted_at == null
                             && a.role == "Auditee"
                             && a.category_audit.Trim().ToLower() == type.Trim().ToLower())
                    .Select(a => new VmAuditor
                    {
                        audity_auditor_id = a.audity_auditor_id,
                        person_name = a.person_name,
                        role = a.role,
                        email = a.email,
                        nik = a.nik
                    })
                    .ToList();

                return Json(new { success = true, data = auditors }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetAuditQuestion(string stakeholderId, string type)
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    return Json(new { success = false, message = "Audit type is required" }, JsonRequestBehavior.AllowGet);
                }

                var questions = dbq.Quality_EAudit_Question_Event
                    .Where(q => q.audit_event_stakeholder_id == stakeholderId
                             && q.deleted_at == null
                             && q.category_audit.Trim().ToLower() == type.Trim().ToLower())
                    .Select(q => new VmQuestion
                    {
                        question_id = q.question_id,
                        clausul = q.clausul,
                        question = q.question,
                        score = q.score,
                        judgement = q.judgement,
                        note = q.note,
                        Evidences = q.Evidences.Where(e => e.deleted_at == null).Select(e => new VmEvidence
                        {
                            evidence_id = e.evidence_id,
                            file_url = e.file_url
                        }).ToList(),

                        Finding = q.Finding.Where(f => f.deleted_at == null).Select(f => new VmFinding
                        {
                            problem = f.problem,
                            location = f.location,
                            object_name = f.object_name,
                            reference = f.reference
                        }).ToList()
                    })
                    .ToList();

                var summary = new
                {
                    OK = questions.Count(q => q.judgement != null && q.judgement.Trim().ToUpper() == "OK"),
                    OFI = questions.Count(q => q.judgement != null && q.judgement.Trim().ToUpper() == "OFI"),
                    MINOR = questions.Count(q => q.judgement != null && q.judgement.Trim().ToUpper() == "MINOR"),
                    MAJOR = questions.Count(q => q.judgement != null && q.judgement.Trim().ToUpper() == "MAJOR")
                };

                return Json(new { success = true, data = questions, summary = summary }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public async Task<JsonResult> CreateQuestionEvent(QuestionEventCreateDto request)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new { success = false, message = "Unauthorized access." }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return Json(new { success = false, message = "Forbidden access." }, JsonRequestBehavior.AllowGet);
            }

            if (request == null)
                return Json(new { success = false, message = "Invalid request data." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Validation failed.", errors = errors });
            }


            try
            {
                var now = DateTime.Now;

                string dateString = now.ToString("yyyyMMddHHmmssfff");

                string prefix = !string.IsNullOrEmpty(request.section_name) && request.section_name.Length >= 2
                                ? request.section_name.Substring(0, 2).ToUpper()
                                : (request.section_name ?? "XX").PadRight(2, 'X').ToUpper();

                var questionId = $"QUID-{prefix}-{dateString}-001";

                var newEventQuestion = new Quality_EAudit_Question_Event
                {
                    question_id = questionId,
                    audit_event_stakeholder_id = request.audit_event_stakeholder_id,

                    category_audit = request.category_audit?.Trim(),
                    process_name = request.process_name?.Trim(),
                    clausul = request.clausul?.Trim(),
                    question = request.question?.Trim(),

                    score = null,
                    criteria = null,
                    judgement = null,
                    note = null,

                    created_at = now,
                    updated_at = null,
                    deleted_at = null
                };

                // 5. Save to Database
                dbq.Quality_EAudit_Question_Event.Add(newEventQuestion);
                await dbq.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Question has been successfully created."
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "A system error occurred.", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateQuestionEvent(string id, QuestionEventDto dto)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();

            var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);

            if (userProfile == null)
                return Json(new { success = false, message = "UnAuthorization" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbq.Quality_EAudit_Question_Event
                    .Include(q => q.Evidences)
                    .Include(q => q.Finding)
                    .FirstOrDefault(q => q.question_id == id);

                if (item == null || item.deleted_at != null)
                    return Json(new { success = false, message = "Data pertanyaan tidak ditemukan." });

                item.clausul = dto.clausul;
                item.process_name = dto.process_name;
                item.question = dto.question;
                item.score = dto.score;
                item.criteria = dto.criteria;
                item.judgement = dto.judgement;
                item.note = dto.note;
                item.updated_at = DateTime.Now;

                string judgeUpper = (dto.judgement ?? "").ToUpper().Trim();
                var activeFinding = item.Finding.FirstOrDefault(f => f.deleted_at == null);

                if (judgeUpper == "MINOR" || judgeUpper == "MAJOR")
                {
                    if (activeFinding != null)
                    {
                        activeFinding.problem = dto.Finding?.problem;
                        activeFinding.location = dto.Finding?.location;
                        activeFinding.object_name = dto.Finding?.object_name;
                        activeFinding.reference = dto.Finding?.reference;
                        activeFinding.updated_at = DateTime.Now;
                    }
                    else
                    {
                        item.Finding.Add(new Quality_EAudit_Finding_Question
                        {
                            finding_id = Guid.NewGuid().ToString(),
                            problem = dto.Finding?.problem,
                            location = dto.Finding?.location,
                            object_name = dto.Finding?.object_name,
                            reference = dto.Finding?.reference,
                            created_at = DateTime.Now
                        });
                    }
                }
                else
                {
                    if (activeFinding != null)
                    {
                        activeFinding.deleted_at = DateTime.Now;
                    }
                }

                if (dto.evidenceFiles != null && dto.evidenceFiles.Count > 0)
                {
                    string folderPath = Server.MapPath("~/Files/Quality/Question/Evidence/");

                    if (!System.IO.Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(folderPath);
                    }

                    foreach (var file in dto.evidenceFiles)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var extension = System.IO.Path.GetExtension(file.FileName);
                            string uniqueFileName = "EVD-Q-" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension;
                            string finalPath = System.IO.Path.Combine(folderPath, uniqueFileName);

                            file.SaveAs(finalPath);

                            item.Evidences.Add(new Quality_EAudit_Evidence_Question
                            {
                                evidence_id = Guid.NewGuid().ToString(),
                                file_url = "/Files/Quality/Question/Evidence/" + uniqueFileName,
                                created_at = DateTime.Now
                            });
                        }
                    }
                }

                dbq.SaveChanges();
                return Json(new { success = true, message = "Pertanyaan berhasil diupdate." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal update data: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteQuestionEvent(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "UnAuthorization"
                }, JsonRequestBehavior.AllowGet);
            }
            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden"
                }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                var item = dbq.Quality_EAudit_Question_Event.Find(id);

                if (item != null)
                {
                    item.deleted_at = DateTime.Now;

                    dbq.SaveChanges();

                    return Json(new { success = true, message = "Pertanyaan berhasil dihapus." });
                }

                return Json(new { success = false, message = "Data tidak ditemukan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal menghapus data: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteEvidenceEvent(string evidence_id, string question_id, string is_delete)
        {
            try
            {
                if (is_delete == "true" && !string.IsNullOrEmpty(evidence_id))
                {
                    var evidence = dbq.Quality_EAudit_Evidence_Question
                                      .FirstOrDefault(e => e.evidence_id == evidence_id);

                    if (evidence != null)
                    {
                        if (!string.IsNullOrEmpty(evidence.file_url))
                        {
                            string physicalPath = Server.MapPath(evidence.file_url);
                            if (System.IO.File.Exists(physicalPath))
                            {
                                System.IO.File.Delete(physicalPath);
                            }
                        }

                        dbq.Quality_EAudit_Evidence_Question.Remove(evidence);

                        dbq.SaveChanges();

                        return Json(new { success = true, message = "File bukti dan data berhasil dihapus permanen." });
                    }

                    return Json(new { success = false, message = "Data file tidak ditemukan di database." });
                }

                return Json(new { success = false, message = "Parameter instruksi hapus tidak valid." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan sistem: " + ex.Message });
            }
        }
        // ==========================================
        // CRUD : CAR & OFI Management
        // ==========================================
        [HttpGet]
        public JsonResult GetCAROFI(
            string search = "",
            int page = 1,
            int pageSize = 10,
            string eventId = "",
            string section = "",
            string type = "ALL"
        )
                {
                    var currUser = (ClaimsIdentity)User.Identity;
                    string userNik = currUser.GetUserId();
                    var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

                    if (userProfile == null)
                    {
                        return Json(new { success = false, message = "Unauthorized access." }, JsonRequestBehavior.AllowGet);
                    }

                    var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
                    if (userAccess == null)
                    {
                        return Json(new { success = false, message = "Forbidden access." }, JsonRequestBehavior.AllowGet);
                    }

                    if (string.IsNullOrEmpty(eventId))
                    {
                        var latestEvent = dbq.Quality_EAudit_Audit_Event_Stakeholder
                            .Where(e => e.deleted_at == null)
                            .OrderByDescending(e => e.created_at)
                            .FirstOrDefault();

                        if (latestEvent != null)
                        {
                            eventId = latestEvent.audit_event_id;
                        }
                    }

                    var Event = dbq.Quality_EAudit_Audit_Event.Where(x => x.audit_event_id == eventId).FirstOrDefault();

                    var MrEvent = dbq.Quality_EAudit_Audit_Standard_Master.Where(x => x.audit_standard_name == Event.audit_standard).FirstOrDefault();



                    IQueryable<AuditFindingListViewModel> combinedQuery = Enumerable.Empty<AuditFindingListViewModel>().AsQueryable();
                    bool isQueryInitialized = false;

                    if (type == "ALL" || type == "CAR")
                    {
                        if (userAccess.access == "Administrator" || MrEvent.mr_email == userProfile.Email)
                        {
                            var carQuery = dbq.Quality_EAudit_CAR_Header
                            .Where(c => c.deleted_at == null &&
                                        c.QuestionEvent != null &&
                                        c.QuestionEvent.AuditEventStakeholder != null &&
                                        c.QuestionEvent.AuditEventStakeholder.audit_event_id == eventId)
                            .Select(c => new AuditFindingListViewModel
                            {
                                Id = c.no_report,
                                StatusProblem = c.Problem.FirstOrDefault() != null ? c.Problem.FirstOrDefault().problem_status : "-",
                                Section = c.QuestionEvent.AuditEventStakeholder.responsibility_section,
                                Clause = c.QuestionEvent.clausul,
                                CategoryAudit = c.QuestionEvent.category_audit,
                                Question = c.QuestionEvent.question,
                                DueDate = c.reply_deadline,
                                Verification1 = c.Verification.FirstOrDefault(x => x.verification_name == "Verifikasi 1").verification_mr_status ?? null,
                                Verification2 = c.Verification.FirstOrDefault(x => x.verification_name == "Verifikasi 2").verification_mr_status ?? null,
                                CreatedDate = c.created_at
                            });

                            combinedQuery = carQuery;
                            isQueryInitialized = true;
                        }
                        else
                        {
                            var carQuery = dbq.Quality_EAudit_CAR_Header
                           .Where(c => c.deleted_at == null &&
                                       c.QuestionEvent != null &&
                                       c.QuestionEvent.AuditEventStakeholder != null &&
                                       c.QuestionEvent.AuditEventStakeholder.audit_event_id == eventId &&
                                       c.QuestionEvent.AuditEventStakeholder.Auditors.Any(a => a.nik == userNik))
                           .Select(c => new AuditFindingListViewModel
                           {
                               Id = c.no_report,
                               StatusProblem = c.Problem.FirstOrDefault() != null ? c.Problem.FirstOrDefault().problem_status : "-",
                               Section = c.QuestionEvent.AuditEventStakeholder.responsibility_section,
                               Clause = c.QuestionEvent.clausul,
                               CategoryAudit = c.QuestionEvent.category_audit,
                               Question = c.QuestionEvent.question,
                               DueDate = c.reply_deadline,
                               Verification1 = c.Verification.FirstOrDefault(x => x.verification_name == "Verifikasi 1").verification_mr_status ?? null,
                               Verification2 = c.Verification.FirstOrDefault(x => x.verification_name == "Verifikasi 2").verification_mr_status ?? null,
                               CreatedDate = c.created_at
                           });

                            combinedQuery = carQuery;
                            isQueryInitialized = true;
                        }
                    }

                    if (type == "ALL" || type == "OFI")
                    {
                        IQueryable<AuditFindingListViewModel> ofiQuery;

                        if (userAccess.access == "Administrator")
                        {
                            ofiQuery = dbq.Quality_EAudit_OFI_Header
                                .Where(o => o.deleted_at == null &&
                                            o.AuditEventStakeholder != null &&
                                            o.AuditEventStakeholder.audit_event_id == eventId)
                                .Select(o => new AuditFindingListViewModel
                                {
                                    Id = o.ofi_header_id,
                                    StatusProblem = "OFI",
                                    Section = o.AuditEventStakeholder.responsibility_section,
                                    Clause = "~",
                                    CategoryAudit = "ALL",
                                    Question = dbq.Quality_EAudit_OFI_Detail.Count(d => d.ofi_header_id == o.ofi_header_id).ToString() +
                                               " Temuan OFI Section " + o.AuditEventStakeholder.responsibility_section,
                                    DueDate = o.reply_deadline,
                                    Verification1 = o.ApprovalOFI.FirstOrDefault(x => x.sign_type == "DiSetujui").status ?? null,
                                    Verification2 = o.ApprovalOFI.FirstOrDefault(x => x.sign_type == "DiSetujui").status ?? null,
                                    CreatedDate = o.created_at
                                });
                        }
                        else
                        {
                            ofiQuery = dbq.Quality_EAudit_OFI_Header
                                .Where(o => o.deleted_at == null &&
                                            o.AuditEventStakeholder != null &&
                                            o.AuditEventStakeholder.audit_event_id == eventId &&
                                            o.AuditEventStakeholder.Auditors.Any(a => a.nik == userNik))
                                .Select(o => new AuditFindingListViewModel
                                {
                                    Id = o.ofi_header_id,
                                    StatusProblem = "OFI",
                                    Section = o.AuditEventStakeholder.responsibility_section,
                                    Clause = "~",
                                    CategoryAudit = "All",
                                    Question = dbq.Quality_EAudit_OFI_Detail.Count(d => d.ofi_header_id == o.ofi_header_id).ToString() +
                                               " Temuan OFI Section " + o.AuditEventStakeholder.responsibility_section,
                                    DueDate = o.reply_deadline,
                                    Verification1 = o.ApprovalOFI.FirstOrDefault(x => x.sign_type == "DiSetujui").status ?? null,
                                    Verification2 = o.ApprovalOFI.FirstOrDefault(x => x.sign_type == "DiSetujui").status ?? null,
                                    CreatedDate = o.created_at
                                });
                        }

                        if (isQueryInitialized)
                        {
                            combinedQuery = combinedQuery.Union(ofiQuery);
                        }
                        else
                        {
                            combinedQuery = ofiQuery;
                        }
                    }

                    if (!string.IsNullOrEmpty(section))
                    {
                        combinedQuery = combinedQuery.Where(x => x.Section == section);
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        search = search.ToLower();
                        combinedQuery = combinedQuery.Where(x =>
                            (x.Id != null && x.Id.ToLower().Contains(search)) ||
                            (x.Section != null && x.Section.ToLower().Contains(search)) ||
                            (x.Clause != null && x.Clause.ToLower().Contains(search)) ||
                            (x.CategoryAudit != null && x.CategoryAudit.ToLower().Contains(search)) ||
                            (x.Question != null && x.Question.ToLower().Contains(search))
                        );
                    }

                    int majorCount = combinedQuery.Count(x => x.StatusProblem.ToLower() == "major");
                    int minorCount = combinedQuery.Count(x => x.StatusProblem.ToLower() == "minor");

                    var filteredOfiIds = combinedQuery.Where(x => x.StatusProblem.ToUpper() == "OFI").Select(x => x.Id).ToList();

                    int ofiDetailCount = 0;
                    if (filteredOfiIds.Any())
                    {
                        ofiDetailCount = dbq.Quality_EAudit_OFI_Detail.Count(d => filteredOfiIds.Contains(d.ofi_header_id));
                    }

                    int totalFindings = majorCount + minorCount + ofiDetailCount;

                    int totalRecords = combinedQuery.Count(); // Ini hanya untuk paging table (berdasarkan header)

                    var data = combinedQuery
                        .OrderByDescending(x => x.CreatedDate)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    int totalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 0;

                    return Json(new
                    {
                        success = true,
                        data = data,
                        summary = new
                        {
                            total = totalFindings,
                            major = majorCount,
                            minor = minorCount,
                            ofi = ofiDetailCount
                        },
                        pagination = new
                        {
                            totalRecords = totalRecords,
                            currentPage = page,
                            pageSize = pageSize,
                            totalPages = totalPages
                        }
                    }, JsonRequestBehavior.AllowGet);
                }

        public JsonResult GetDetailCAR(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new { success = false, message = "Unauthorized access." }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return Json(new { success = false, message = "Forbidden access." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // 1. Tarik data utama beserta seluruh tabel anak menggunakan Include
                var dataCAR = dbq.Quality_EAudit_CAR_Header
                    .Include(c => c.Problem)
                    .Include(c => c.Action)
                    .Include(c => c.FiveWhy)
                    .Include(c => c.Verification)
                    .Include(c => c.Ilustration)
                    .Include(c => c.Approval)
                    .FirstOrDefault(c => c.no_report == id);

                if (dataCAR == null)
                {
                    return Json(new { success = false, message = "Data CAR tidak ditemukan." }, JsonRequestBehavior.AllowGet);
                }

                var responseData = new
                {
                    // --- DATA HEADER ---
                    no_report = dataCAR.no_report,
                    question_id = dataCAR.question_id,
                    date = dataCAR.date.ToString("yyyy-MM-dd"),
                    to_Dept = dataCAR.to_Dept,
                    attention = dataCAR.attention,
                    from_auditor = dataCAR.from_auditor,
                    qty_check = dataCAR.qty_check,
                    ng_rasio = dataCAR.ng_rasio,
                    incident_status = dataCAR.incident_status,
                    reply_deadline = dataCAR.reply_deadline,
                    created_at = dataCAR.created_at,
                    updated_at = dataCAR.updated_at,

                    // --- DATA 1 TO 1 (Otomatis NULL jika di DB kosong) ---
                    Problem = dataCAR.Problem != null && dataCAR.Problem.Any()
                        ? dataCAR.Problem.Select(p => new
                        {
                            p.problem_id,
                            p.no_report,
                            p.problem_name,
                            p.problem_location,
                            p.problem_part_name,
                            p.problem_lot_number,
                            p.problem_qty,
                            p.problem_detail,
                            p.problem_category,
                            p.problem_status
                        }).FirstOrDefault()
                        : null,

                    Action = dataCAR.Action != null && dataCAR.Action.Any()
                        ? dataCAR.Action.Select(a => new
                        {
                            a.action_id,
                            a.no_report,
                            a.PIC_dept,
                            a.action_date,
                            a.review_PFMEA,
                            a.containment_action,
                            a.PIC_containment_action,
                            a.date_containment_action,
                            a.detail_correction,
                            a.PIC_detail_correction,
                            a.date_detail_correction,
                            a.detail_corrective_action,
                            a.PIC_detail_corrective_action,
                            a.date_detail_corrective_action,
                            a.yokotenkai,
                            a.initial_lot_after_repair,
                            a.PIC_yokotenkai,
                            a.date_yokotenkai,
                            a.is_document_updated,
                            a.document_type_updated,
                            a.document_number
                        }).FirstOrDefault()
                        : null,

                    Verification = dataCAR.Verification != null && dataCAR.Verification.Any()
                        ? dataCAR.Verification.Select(v => new
                        {
                            v.verification_id,
                            v.no_report,
                            v.verification_name,
                            v.verification_comment,
                            v.verification_auditor_sign_by,
                            v.verification_auditor_status,
                            v.verification_auditor_date,
                            v.verification_mr_date,
                            v.verification_mr_sign_by,
                            v.verification_mr_status,
                            v.verification_status
                        }).ToList()
                        : null,

                    Approval = dataCAR.Approval != null && dataCAR.Approval.Any()
                        ? dataCAR.Approval.Select(ap => new
                        {
                            ap.approval_id,
                            ap.no_report,
                            ap.group_type,
                            ap.role_type,
                            ap.sign_by,
                            ap.sign_date,
                            ap.status
                        }).ToList()
                        : null,

                    FiveWhy = dataCAR.FiveWhy != null && dataCAR.FiveWhy.Any()
                        ? dataCAR.FiveWhy.Select(fw => new
                        {
                            fw.why_id,
                            fw.no_report,
                            fw.why_number,
                            fw.why_problem
                        }).ToList()
                        : null,

                    Ilustration = dataCAR.Ilustration != null && dataCAR.Ilustration.Any()
                        ? dataCAR.Ilustration.Select(img => new
                        {
                            img.ilustration_id,
                            img.no_report,
                            img.file_url
                        }).ToList()
                        : null,

                    Evidence = dataCAR.Evidence != null && dataCAR.Evidence.Any()
                        ? dataCAR.Evidence.Select(img => new
                        {
                            img.evidence_id,
                            img.no_report,
                            img.evidence_name,
                            img.evidence_url
                        }).ToList()
                        : null

                };
                return Json(new { success = true, data = responseData }, JsonRequestBehavior.AllowGet);
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan internal: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetDetailOFI(string id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new { success = false, message = "Unauthorized access." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var dataOFI = dbq.Quality_EAudit_OFI_Header
                    .Where(o => o.ofi_header_id == id && o.deleted_at == null)
                    .Select(o => new
                    {
                        ofi_header_id = o.ofi_header_id,
                        section = o.AuditEventStakeholder != null ? o.AuditEventStakeholder.responsibility_section : "",
                        reply_deadline = o.reply_deadline,
                        status = o.status,
                        Approvals = o.ApprovalOFI.Select(a => new
                        {
                            approval_id = a.approval_id,
                            sign_type = a.sign_type,
                            sign_by = a.sign_by,
                            status = a.status,
                            sign_date = a.sign_date
                        }).ToList(),
                        Details = o.OFIDetails.Select(d => new
                        {
                            ofi_detail_id = d.ofi_detail_id,
                            question_id = d.question_id,
                            clause = d.clause,
                            question_text = d.QuestionEvent != null ? d.QuestionEvent.question : "-",
                            observation_comment = d.observation_comment,
                            action_plan = d.action_plan,
                            action_date = d.action_date
                        }).ToList(),

                        Periode = new
                        {
                            start = o.AuditEventStakeholder != null ? o.AuditEventStakeholder.start_event : (DateTime?)null,
                            end = o.AuditEventStakeholder != null ? o.AuditEventStakeholder.end_event : (DateTime?)null
                        }

                    }).FirstOrDefault();

                if (dataOFI == null)
                {
                    return Json(new { success = false, message = "OFI data not found." }, JsonRequestBehavior.AllowGet);
                }

                string shortName = string.Join(" ", (userProfile.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

                return Json(new { success = true, data = dataOFI, currentUserName = shortName }, JsonRequestBehavior.AllowGet);
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetRekapitulasiAudit(string eventId = "", string search = "")
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();
                if (string.IsNullOrEmpty(userNik)) return Json(new { success = false, message = "Sesi telah habis." }, JsonRequestBehavior.AllowGet);

                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                if (userProfile == null) return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

                var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik);
                if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

                bool isAdministrator = (userAccess != null && userAccess.access == "Administrator");

                // 3. CEK MR UNTUK EVENT YANG DIPILIH
                bool isMR = false;
                var auditEvent = dbq.Quality_EAudit_Audit_Event.FirstOrDefault(x => x.audit_event_id == eventId);

                if (auditEvent != null && !string.IsNullOrEmpty(userProfile.Email))
                {
                    var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                        .FirstOrDefault(x => x.audit_standard_name == auditEvent.audit_standard);

                    if (standardMaster != null &&
                        !string.IsNullOrEmpty(standardMaster.mr_email) &&
                        standardMaster.mr_email.Equals(userProfile.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        isMR = true;
                    }
                }

                if (string.IsNullOrEmpty(eventId))
                {
                    var latestEvent = dbq.Quality_EAudit_Audit_Event_Stakeholder
                        .Where(e => e.deleted_at == null)
                        .OrderByDescending(e => e.created_at)
                        .Select(e => e.audit_event_id)
                        .FirstOrDefault();

                    if (latestEvent != null) eventId = latestEvent;
                }

                var stakeholdersQuery = dbq.Quality_EAudit_Audit_Event_Stakeholder
                    .Where(s => s.audit_event_id == eventId && s.deleted_at == null);

                if (userAccess.access != "Administrator")
                {
                    stakeholdersQuery = stakeholdersQuery.Where(s => s.Auditors.Any(a => a.nik == userNik));
                }

                var stakeholders = stakeholdersQuery.ToList();
                var resultData = new List<dynamic>();

                int totalMajorEvent = 0;
                int totalMinorEvent = 0;
                int totalOfiEvent = 0;

                // ==========================================
                // VARIABEL GLOBAL UNTUK SUMMARY CARD (DITAMBAH REJECTED)
                // ==========================================
                int globalCatatanRequired = 0;
                int globalCatatanApproved = 0;
                int globalCatatanRejected = 0; // <-- BARU

                int globalLaporanRequired = 0;
                int globalLaporanApproved = 0;
                int globalLaporanRejected = 0;

                foreach (var sh in stakeholders)
                {
                    // A. CAR (Major / Minor)
                    var cars = dbq.Quality_EAudit_CAR_Header
                        .Where(c => c.deleted_at == null && c.QuestionEvent != null && c.QuestionEvent.audit_event_stakeholder_id == sh.audit_event_stakeholder_id)
                        .Select(c => new
                        {
                            Category = c.QuestionEvent.category_audit,
                            Statuses = c.Problem.Select(p => p.problem_status).ToList()
                        }).ToList();

                    int majSys = cars.Where(c => c.Category == "System").SelectMany(c => c.Statuses).Count(s => string.Equals(s, "major", StringComparison.OrdinalIgnoreCase));
                    int majPro = cars.Where(c => c.Category == "Process").SelectMany(c => c.Statuses).Count(s => string.Equals(s, "major", StringComparison.OrdinalIgnoreCase));
                    int majPrd = cars.Where(c => c.Category == "Product").SelectMany(c => c.Statuses).Count(s => string.Equals(s, "major", StringComparison.OrdinalIgnoreCase));
                    int majTot = majSys + majPro + majPrd;

                    int minSys = cars.Where(c => c.Category == "System").SelectMany(c => c.Statuses).Count(s => string.Equals(s, "minor", StringComparison.OrdinalIgnoreCase));
                    int minPro = cars.Where(c => c.Category == "Process").SelectMany(c => c.Statuses).Count(s => string.Equals(s, "minor", StringComparison.OrdinalIgnoreCase));
                    int minPrd = cars.Where(c => c.Category == "Product").SelectMany(c => c.Statuses).Count(s => string.Equals(s, "minor", StringComparison.OrdinalIgnoreCase));
                    int minTot = minSys + minPro + minPrd;

                    // B. OFI
                    var ofiHeaderIds = dbq.Quality_EAudit_OFI_Header
                        .Where(o => o.deleted_at == null && o.audit_event_stakeholder_id == sh.audit_event_stakeholder_id)
                        .Select(o => o.ofi_header_id)
                        .ToList();

                    var ofiCategories = ofiHeaderIds.Any()
                        ? dbq.Quality_EAudit_OFI_Detail
                            .Where(d => ofiHeaderIds.Contains(d.ofi_header_id))
                            .Select(d => d.QuestionEvent != null ? d.QuestionEvent.category_audit : null)
                            .ToList()
                        : new List<string>();

                    int ofiSys = ofiCategories.Count(c => string.Equals(c, "System", StringComparison.OrdinalIgnoreCase));
                    int ofiPro = ofiCategories.Count(c => string.Equals(c, "Process", StringComparison.OrdinalIgnoreCase));
                    int ofiPrd = ofiCategories.Count(c => string.Equals(c, "Product", StringComparison.OrdinalIgnoreCase));
                    int ofiTot = ofiCategories.Count;

                    // C. APPROVAL STATUS
                    var approvals = dbq.Quality_EAudit_Recap_Approval
                        .Where(a => a.audit_event_stakeholder_id == sh.audit_event_stakeholder_id)
                        .ToList();

                    var validJudgements = new[] { "major", "minor", "ofi" };
                    var activeCategories = dbq.Quality_EAudit_Question_Event
                                        .Where(q => q.audit_event_stakeholder_id == sh.audit_event_stakeholder_id
                                                    && q.deleted_at == null
                                                    && q.judgement != null)
                                        .AsEnumerable()
                                        .Where(q => validJudgements.Contains(q.judgement.ToLower()))
                                        .Select(q => q.category_audit)
                                        .Distinct()
                                        .ToList();

                    int totalCatRequired = activeCategories.Count;
                    int totalCatApproved = 0;
                    int totalCatRejected = 0;

                    string GetCatStatus(string catName)
                    {
                        if (!activeCategories.Any(c => string.Equals(c, catName, StringComparison.OrdinalIgnoreCase))) return "N/A";

                        string recapKey = $"Catatan Audit ({catName})";
                        var catApprovals = approvals.Where(a => string.Equals(a.category_recap, recapKey, StringComparison.OrdinalIgnoreCase)).ToList();

                        // Jika MINIMAL ADA 1 YANG REJECT, maka status global kategori tersebut adalah Rejected
                        if (catApprovals.Any(a => a.recap_sign_status == "Rejected")) return "Rejected";

                        bool auditeeApproved = catApprovals.Any(a => a.recap_sign_role != null && a.recap_sign_role.IndexOf("Auditee", StringComparison.OrdinalIgnoreCase) >= 0 && a.recap_sign_status == "Approved");
                        bool auditorApproved = catApprovals.Any(a => a.recap_sign_role != null && a.recap_sign_role.IndexOf("Auditor", StringComparison.OrdinalIgnoreCase) >= 0 && a.recap_sign_status == "Approved");

                        if (auditeeApproved && auditorApproved) return "Approved";
                        if (catApprovals.Any(a => a.recap_sign_status != null && a.recap_sign_status.IndexOf("Pending", StringComparison.OrdinalIgnoreCase) >= 0)) return "Pending";

                        return "Draft";
                    }

                    string catSysStatus = GetCatStatus("System");
                    string catProStatus = GetCatStatus("Process");
                    string catPrdStatus = GetCatStatus("Product");

                    // Akumulasi Approved & Rejected per Section
                    if (catSysStatus == "Approved") totalCatApproved++;
                    else if (catSysStatus == "Rejected") totalCatRejected++;

                    if (catProStatus == "Approved") totalCatApproved++;
                    else if (catProStatus == "Rejected") totalCatRejected++;

                    if (catPrdStatus == "Approved") totalCatApproved++;
                    else if (catPrdStatus == "Rejected") totalCatRejected++;

                    // SUMMARY CATATAN AUDIT
                    globalCatatanRequired += totalCatRequired;
                    globalCatatanApproved += totalCatApproved;
                    globalCatatanRejected += totalCatRejected;

                    // SUMMARY LAPORAN TEMUAN
                    bool isLaporanRequired = (majTot + minTot + ofiTot) > 0;
                    string lapStatus = "N/A";

                    if (isLaporanRequired)
                    {
                        globalLaporanRequired++;
                        var lapApprovals = approvals.Where(a => a.category_recap == "Laporan Temuan").ToList();

                        // Jika MINIMAL ADA 1 YANG REJECT, langsung ditolak
                        if (lapApprovals.Any(a => a.recap_sign_status == "Rejected"))
                        {
                            lapStatus = "Rejected";
                            globalLaporanRejected++;
                        }
                        else
                        {
                            // Terapkan logika yang sama dengan Catatan: Minimal 1 Auditee dan 1 Auditor Approve
                            bool auditeeLapApproved = lapApprovals.Any(a => a.recap_sign_role != null && a.recap_sign_role.IndexOf("Auditee", StringComparison.OrdinalIgnoreCase) >= 0 && a.recap_sign_status == "Approved");
                            bool auditorLapApproved = lapApprovals.Any(a => a.recap_sign_role != null && a.recap_sign_role.IndexOf("Auditor", StringComparison.OrdinalIgnoreCase) >= 0 && a.recap_sign_status == "Approved");

                            if (auditeeLapApproved && auditorLapApproved)
                            {
                                lapStatus = "Approved";
                                globalLaporanApproved++;
                            }
                            else if (lapApprovals.Any(a => a.recap_sign_status != null && a.recap_sign_status.IndexOf("Pending", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                lapStatus = "Pending";
                            }
                            else
                            {
                                lapStatus = "Draft";
                            }
                        }
                    }

                    totalMajorEvent += majTot;
                    totalMinorEvent += minTot;
                    totalOfiEvent += ofiTot;

                    resultData.Add(new
                    {
                        StakeholderId = sh.audit_event_stakeholder_id,
                        SectionName = sh.responsibility_section ?? "-",
                        Major = new { System = majSys, Process = majPro, Product = majPrd, Total = majTot },
                        Minor = new { System = minSys, Process = minPro, Product = minPrd, Total = minTot },
                        OFI = new { System = ofiSys, Process = ofiPro, Product = ofiPrd, Total = ofiTot },
                        CatatanStatus = new
                        {
                            System = catSysStatus,
                            Process = catProStatus,
                            Product = catPrdStatus,
                            TotalCategoryActive = totalCatRequired,
                            TotalCategoryFullyApproved = totalCatApproved,
                            TotalCategoryRejected = totalCatRejected
                        },
                        LaporanStatus = lapStatus
                    });
                }

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    resultData = resultData.Where(x => x.SectionName != null && x.SectionName.ToLower().Contains(search)).ToList();
                }

                return Json(new
                {
                    success = true,
                    data = resultData,
                    isAdministrator = isAdministrator, 
                    isMR = isMR,
                    summary = new
                    {
                        totalFindings = totalMajorEvent + totalMinorEvent + totalOfiEvent,
                        major = totalMajorEvent,
                        minor = totalMinorEvent,
                        ofi = totalOfiEvent,
                        catatanApproved = globalCatatanApproved,
                        catatanRequired = globalCatatanRequired,
                        catatanRejected = globalCatatanRejected,
                        laporanApproved = globalLaporanApproved,
                        laporanRequired = globalLaporanRequired,
                        laporanRejected = globalLaporanRejected
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetLaporanTemuanAudit(string stakeholderId)
        {
            try
            {
                if (string.IsNullOrEmpty(stakeholderId))
                    return Json(new { success = false, message = "Stakeholder ID tidak valid." }, JsonRequestBehavior.AllowGet);

                // Ambil data User untuk TTD
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();
                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                string currentUserName = string.Join(" ", (userProfile?.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

                // ==========================================
                // Ambil Data Stempel (Mapping NIK ke Nama)
                // ==========================================
                var rawApprovals = dbq.Quality_EAudit_Recap_Approval
                    .Where(a => a.audit_event_stakeholder_id == stakeholderId)
                    .Select(a => new
                    {
                        a.category_recap,
                        a.recap_sign_role,
                        a.recap_sign_status,
                        a.recap_sign_date,
                        a.recap_sign_by // Menyimpan NIK
                    })
                    .ToList();

                var signerNiks = rawApprovals
                    .Where(a => !string.IsNullOrEmpty(a.recap_sign_by))
                    .Select(a => a.recap_sign_by)
                    .Distinct()
                    .ToList();

                var signerNameMap = db.V_Users_Active
                    .Where(u => signerNiks.Contains(u.NIK))
                    .ToDictionary(u => u.NIK, u => u.Name);

                var approvals = rawApprovals.Select(a => new
                {
                    category_recap = a.category_recap,
                    recap_sign_role = a.recap_sign_role,
                    recap_sign_status = a.recap_sign_status,
                    recap_sign_date = a.recap_sign_date,
                    recap_sign_by_name = (!string.IsNullOrEmpty(a.recap_sign_by) && signerNameMap.ContainsKey(a.recap_sign_by))
                        ? signerNameMap[a.recap_sign_by]
                        : a.recap_sign_by // fallback: tampilkan NIK kalau nama tidak ketemu
                }).ToList();


                // Kriteria Audit
                var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder.FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholderId);
                var eventMaster = dbq.Quality_EAudit_Audit_Event.FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);
                var auditStandards = eventMaster?.audit_standard?.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim()).ToList() ?? new List<string>();

                // Ambil data Question yang valid (Major, Minor, OFI)
                var validJudgements = new string[] { "major", "minor", "ofi" };
                var questions = dbq.Quality_EAudit_Question_Event
                    .Where(q => q.audit_event_stakeholder_id == stakeholderId && q.deleted_at == null && q.judgement != null)
                    .Include(q => q.Finding)
                    .Include(q => q.CARs.Select(c => c.Verification))
                    .Include(q => q.CARs.Select(c => c.Action))
                    .Include(q => q.CARs.Select(c => c.Problem))
                    .Include(q => q.OFIs.Select(o => o.OFIHeader))
                    .ToList()
                    .Where(q => validJudgements.Contains(q.judgement.ToLower()))
                    .ToList();

                var resultData = questions.Select(q =>
                {
                    var finding = q.Finding?.FirstOrDefault(f => f.deleted_at == null);
                    var car = q.CARs?.FirstOrDefault(c => c.deleted_at == null);
                    var ofi = q.OFIs?.FirstOrDefault();

                    // Mapping Problem, Location, Object
                    string problemStr = finding?.problem;
                    string locationStr = finding?.location;
                    string objectStr = finding?.object_name;

                    if (string.IsNullOrEmpty(problemStr))
                    {
                        if (car != null && car.Problem != null && car.Problem.Any())
                        {
                            var carProb = car.Problem.FirstOrDefault();
                            problemStr = carProb.problem_name;
                            locationStr = carProb.problem_location;
                            objectStr = carProb.problem_part_name;
                        }
                        else if (ofi != null)
                        {
                            problemStr = null;
                        }
                    }

                    // Mapping Action Date
                    DateTime? actionDateObj = null;
                    if (car != null && car.Action != null && car.Action.Any()) actionDateObj = car.Action.FirstOrDefault()?.action_date;
                    else if (ofi != null) actionDateObj = ofi.action_date;

                    string actionDateFormatted = actionDateObj.HasValue ? actionDateObj.Value.ToString("dd-MMM-yyyy") : "-";

                    // Mapping Status Tindak Lanjut
                    string statusTindakLanjut = "-";
                    if (car != null)
                    {
                        string carNo = car.no_report;
                        string carStatus = "OPEN";

                        var verif1 = car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 1");
                        var verif2 = car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 2");

                        bool isVerif1MrFilled = verif1 != null && !string.IsNullOrEmpty(verif1.verification_mr_status);
                        bool isVerif2MrFilled = verif2 != null && !string.IsNullOrEmpty(verif2.verification_mr_status);

                        if (isVerif1MrFilled && isVerif2MrFilled) carStatus = "CLOSED";

                        statusTindakLanjut = $"<a href='{Url.Action("DetailCAR", "EAudit", new { id = carNo }, Request.Url.Scheme)}' class='text-blue-600 underline hover:text-blue-800 font-semibold transition-colors'>{carNo}</a> / {carStatus}";
                    }
                    else if (ofi != null)
                    {
                        string ofiNo = ofi.ofi_header_id ?? "-";
                        string ofiStatus = ofi.OFIHeader?.status ?? "OPEN";
                        statusTindakLanjut = $"<a href='{Url.Action("DetailOFI", "EAudit", new { id = ofiNo }, Request.Url.Scheme)}' class='text-blue-600 underline hover:text-blue-800 font-semibold transition-colors'>LINK</a>";
                    }

                    return new
                    {
                        clause = q.clausul ?? "-",
                        problem = problemStr ?? null,
                        location = locationStr ?? null,
                        Object = objectStr ?? null,
                        detail = q.note,
                        klasifikasi_temuan = q.judgement,
                        action_date = actionDateFormatted,
                        status_tindak_lanjut = statusTindakLanjut
                    };
                }).ToList();

                // Return Data Gabungan untuk UI
                return Json(new
                {
                    success = true,
                    data = resultData,
                    existingApprovals = approvals,
                    auditStandards = auditStandards,
                    currentUserName = currentUserName
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveOFI(OFISaveDto model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.ofi_header_id))
                    return Json(new { success = false, message = "Invalid Header ID." });

                // 1. Update Header Status jika diperlukan
                var header = dbq.Quality_EAudit_OFI_Header.FirstOrDefault(h => h.ofi_header_id == model.ofi_header_id);
                if (header != null)
                {
                    header.updated_at = DateTime.Now;
                }

                // 2. Upsert Details (Update Observation, Action Plan & Date)
                if (model.Details != null && model.Details.Any())
                {
                    foreach (var d in model.Details)
                    {
                        var detail = dbq.Quality_EAudit_OFI_Detail.FirstOrDefault(x => x.ofi_detail_id == d.ofi_detail_id);
                        if (detail != null)
                        {
                            detail.observation_comment = d.observation_comment; // TAMBAHAN: Agar observasi tersimpan
                            detail.action_plan = d.action_plan;
                            detail.action_date = d.action_date;
                        }
                    }
                }

                // 3. Upsert Approvals (Stempel / Signature)
                if (model.Approvals != null && model.Approvals.Any())
                {
                    // Ambil semua approval untuk dokumen ini ke memory agar fungsi Replace() bisa jalan
                    var existingApprovals = dbq.Quality_EAudit_OFI_Approval.Where(x => x.ofi_header_id == model.ofi_header_id).ToList();

                    foreach (var a in model.Approvals)
                    {
                        // PERBAIKAN: Abaikan spasi dan huruf besar/kecil (Match "Di Setujui" dengan "Disetujui")
                        var appv = existingApprovals.FirstOrDefault(x =>
                            (x.sign_type ?? "").Replace(" ", "").ToLower() == (a.sign_type ?? "").Replace(" ", "").ToLower()
                        );

                        if (appv != null)
                        {
                            appv.status = a.status;
                            appv.sign_by = a.sign_by;
                            appv.sign_date = string.IsNullOrEmpty(a.status) ? (DateTime?)null : DateTime.Now;
                        }
                    }
                }

                dbq.SaveChanges();
                return Json(new { success = true, message = "Data saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpsertAction(Quality_EAudit_CAR_Action request)
        {
            try
            {
                var existingAction = dbq.Quality_EAudit_CAR_Action
                    .FirstOrDefault(a => a.action_id == request.action_id);

                if (existingAction != null)
                {
                    dbq.Entry(existingAction).CurrentValues.SetValues(request);
                }
                else
                {
                    dbq.Quality_EAudit_CAR_Action.Add(request);
                }

                dbq.SaveChanges();

                return Json(new { status = "success", message = "Data Action berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpsertProblem(Quality_EAudit_CAR_Problem request)
        {
            try
            {
                var existingProblem = dbq.Quality_EAudit_CAR_Problem
                    .FirstOrDefault(p => p.problem_id == request.problem_id);

                if (existingProblem != null)
                {
                    dbq.Entry(existingProblem).CurrentValues.SetValues(request);
                }
                else
                {
                    dbq.Quality_EAudit_CAR_Problem.Add(request);
                }

                dbq.SaveChanges();

                return Json(new { status = "success", message = "Data Problem berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpsertFiveWhy(List<Quality_EAudit_CAR_5_Why> requests)
        {
            try
            {
                if (requests == null || requests.Count == 0)
                {
                    return Json(new { status = "warning", message = "Tidak ada data 5 Why yang dikirim." });
                }

                foreach (var req in requests)
                {
                    var existingWhy = dbq.Quality_EAudit_CAR_5_Why
                        .FirstOrDefault(w => w.why_id == req.why_id);

                    if (existingWhy != null)
                    {
                        dbq.Entry(existingWhy).CurrentValues.SetValues(req);
                    }
                    else
                    {
                        dbq.Quality_EAudit_CAR_5_Why.Add(req);
                    }
                }

                dbq.SaveChanges();

                return Json(new { status = "success", message = "Data 5 Why berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        public JsonResult UpsertApproval(Quality_EAudit_CAR_Approval request)
        {
            try
            {
                var existingApproval = dbq.Quality_EAudit_CAR_Approval
                    .FirstOrDefault(a => a.approval_id == request.approval_id);

                // Ambil status saat ini dan sebelumnya, jadikan lowercase untuk menghindari case-sensitive (Approved vs approved)
                string currentStatus = !string.IsNullOrEmpty(request.status) ? request.status.ToLower() : "";
                string previousStatus = (existingApproval != null && !string.IsNullOrEmpty(existingApproval.status))
                                        ? existingApproval.status.ToLower() : "";

                bool isStatusChanged = currentStatus != previousStatus;
                bool isApproved = currentStatus == "approved";
                bool isRejected = currentStatus == "rejected";

                // Pengecekan role_type == Disetujui (case-insensitive)
                bool isRoleDisetujui = !string.IsNullOrEmpty(request.role_type) &&
                                       request.role_type.Equals("Disetujui", StringComparison.OrdinalIgnoreCase);

                if (existingApproval != null)
                {
                    dbq.Entry(existingApproval).CurrentValues.SetValues(request);
                }
                else
                {
                    dbq.Quality_EAudit_CAR_Approval.Add(request);
                }

                dbq.SaveChanges();

                if (isStatusChanged && (isApproved || isRejected) && isRoleDisetujui)
                {
                    var carHeader = dbq.Quality_EAudit_CAR_Header.FirstOrDefault(c => c.no_report == request.no_report);

                    if (carHeader != null)
                    {
                        var question = dbq.Quality_EAudit_Question_Event.FirstOrDefault(q => q.question_id == carHeader.question_id);

                        if (question != null)
                        {
                            var targetEmails = dbq.Quality_EAudit_Audity_Auditor_Event
                                .Where(a => a.audit_event_stakeholder_id == question.audit_event_stakeholder_id
                                         && a.category_audit == question.category_audit
                                         && a.deleted_at == null)
                                .Select(a => a.email)
                                .Where(email => !string.IsNullOrEmpty(email))
                                .Distinct()
                                .ToList();

                            if (targetEmails.Any())
                            {
                                SendEmailApprovalCAR(targetEmails, request.no_report, request.group_type, isApproved);
                            }
                        }
                    }
                }

                return Json(new { status = "success", message = "Data Approval berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Terjadi kesalahan pada Approval: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpsertVerification(Quality_EAudit_CAR_Verification request)
        {
            try
            {
                var existingVerification = dbq.Quality_EAudit_CAR_Verification
                    .FirstOrDefault(v => v.verification_id == request.verification_id);

                // ==============================================================
                // 1. CEK TRIGGER EMAIL (Approved & Rejected untuk Auditor & MR)
                // ==============================================================
                bool isAuditorTriggered = false;
                bool isMrTriggered = false;

                // --- Cek Status AUDITOR ---
                string currentAuditorStatus = !string.IsNullOrEmpty(request.verification_auditor_status) ? request.verification_auditor_status.ToLower() : "";
                string previousAuditorStatus = (existingVerification != null && !string.IsNullOrEmpty(existingVerification.verification_auditor_status))
                                        ? existingVerification.verification_auditor_status.ToLower() : "";

                if ((currentAuditorStatus == "approved" || currentAuditorStatus == "aproved" || currentAuditorStatus == "rejected")
                    && previousAuditorStatus != currentAuditorStatus)
                {
                    isAuditorTriggered = true;
                }

                // --- Cek Status MR ---
                string currentMrStatus = !string.IsNullOrEmpty(request.verification_mr_status) ? request.verification_mr_status.ToLower() : "";
                string previousMrStatus = (existingVerification != null && !string.IsNullOrEmpty(existingVerification.verification_mr_status))
                                        ? existingVerification.verification_mr_status.ToLower() : "";

                if ((currentMrStatus == "approved" || currentMrStatus == "aproved" || currentMrStatus == "rejected")
                    && previousMrStatus != currentMrStatus)
                {
                    isMrTriggered = true;
                }

                if (existingVerification != null)
                {
                    dbq.Entry(existingVerification).CurrentValues.SetValues(request);
                }
                else
                {
                    dbq.Quality_EAudit_CAR_Verification.Add(request);
                }

                dbq.SaveChanges();

                if (isAuditorTriggered || isMrTriggered)
                {
                    var carHeader = dbq.Quality_EAudit_CAR_Header.FirstOrDefault(c => c.no_report == request.no_report);

                    if (carHeader != null)
                    {
                        var question = dbq.Quality_EAudit_Question_Event.FirstOrDefault(q => q.question_id == carHeader.question_id);

                        if (question != null)
                        {
                            var targetEmails = dbq.Quality_EAudit_Audity_Auditor_Event
                                .Where(a => a.audit_event_stakeholder_id == question.audit_event_stakeholder_id
                                         && a.category_audit == question.category_audit
                                         && a.deleted_at == null)
                                .Select(a => a.email)
                                .Where(email => !string.IsNullOrEmpty(email))
                                .ToList();




                            if (targetEmails.Any())
                            {
                                string approverRole = isMrTriggered ? "MR" : "Auditor";
                                string sentStatus = isMrTriggered ? request.verification_mr_status : request.verification_auditor_status;

                                SendEmailVerificationCAR(
                                    targetEmails,
                                    request.no_report,
                                    sentStatus,
                                    request.verification_name,
                                    approverRole,
                                    question.audit_event_stakeholder_id
                                );
                            }
                        }
                    }
                }

                return Json(new { status = "success", message = "Data Verification berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Terjadi kesalahan pada Verification: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpsertIlustrationImage(string ilustration_id, string no_report, HttpPostedFileBase file)
        {
            try
            {
                var existingImage = dbq.Quality_EAudit_CAR_Ilustration_Image
                    .FirstOrDefault(i => i.ilustration_id == ilustration_id);

                string savedFilePath = null;

                if (file == null && existingImage != null && Request["is_delete"] == "true")
                {
                    existingImage.file_url = null;
                    dbq.SaveChanges();
                    return Json(new { status = "success", message = "Gambar dihapus." });
                }

                // 2. Proses Upload File Fisik
                if (file != null && file.ContentLength > 0)
                {
                    var extension = Path.GetExtension(file.FileName);
                    string uniqueFileName = "IMG-" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension;

                    string folderPath = Server.MapPath("~/Files/Quality/CAR/IlustrationImage/");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string finalPath = Path.Combine(folderPath, uniqueFileName);
                    file.SaveAs(finalPath);

                    savedFilePath = "/Files/Quality/CAR/IlustrationImage/" + uniqueFileName;
                }

                // 3. Simpan Database
                if (existingImage != null)
                {
                    if (savedFilePath != null) existingImage.file_url = savedFilePath;
                }
                else
                {
                    if (savedFilePath != null)
                    {
                        var newImg = new Quality_EAudit_CAR_Ilustration_Image
                        {
                            ilustration_id = string.IsNullOrEmpty(ilustration_id) ? Guid.NewGuid().ToString() : ilustration_id,
                            no_report = no_report,
                            file_url = savedFilePath
                        };
                        dbq.Quality_EAudit_CAR_Ilustration_Image.Add(newImg);
                    }
                }

                dbq.SaveChanges();

                return Json(new { status = "success", message = "Data Ilustrasi berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Terjadi kesalahan Ilustrasi: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpsertEvidence(string evidence_id, string no_report, HttpPostedFileBase file)
        {
            try
            {
                var existingEvidence = dbq.Quality_EAudit_CAR_Evidence
                    .FirstOrDefault(e => e.evidence_id == evidence_id && !string.IsNullOrEmpty(evidence_id));

                string savedFilePath = null;
                string fileName = "Unknown";

                // 1. Proses File Fisik Jika Ada
                if (file != null && file.ContentLength > 0)
                {
                    fileName = file.FileName;
                    var extension = Path.GetExtension(file.FileName);
                    string uniqueFileName = "EVD-" + Guid.NewGuid().ToString("N").Substring(0, 8) + extension;

                    string folderPath = Server.MapPath("~/Files/Quality/CAR/Evidence/");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string finalPath = Path.Combine(folderPath, uniqueFileName);
                    file.SaveAs(finalPath);

                    savedFilePath = "/Files/Quality/CAR/Evidence/" + uniqueFileName;
                }

                // 2. Simpan ke Database
                if (existingEvidence != null)
                {
                    if (savedFilePath != null)
                    {
                        existingEvidence.evidence_url = savedFilePath;
                        existingEvidence.evidence_name = fileName;
                    }

                }
                else
                {
                    string newId = string.IsNullOrEmpty(evidence_id) ? Guid.NewGuid().ToString() : evidence_id;
                    var newRecord = new Quality_EAudit_CAR_Evidence
                    {
                        evidence_id = newId,
                        no_report = no_report,
                        evidence_name = fileName,
                        evidence_url = savedFilePath // Simpan lokasi folder, bukan Base64
                    };
                    dbq.Quality_EAudit_CAR_Evidence.Add(newRecord);
                    evidence_id = newId;
                }

                dbq.SaveChanges();

                return Json(new
                {
                    status = "success",
                    message = "Data Evidence berhasil disimpan.",
                    evidence_id = evidence_id,
                    file_url = savedFilePath
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateHeader(Quality_EAudit_CAR_Header request)
        {
            try
            {
                // Cari data Header berdasarkan Primary Key
                var existingHeader = dbq.Quality_EAudit_CAR_Header
                    .FirstOrDefault(h => h.no_report == request.no_report);

                if (existingHeader == null)
                {
                    return Json(new { status = "error", message = $"Data Header dengan nomor report {request.no_report} tidak ditemukan." });
                }

                // --- SOLUSI: Hanya update field yang dikirim dari frontend (Partial Update) ---
                existingHeader.to_Dept = request.to_Dept;
                existingHeader.attention = request.attention;
                existingHeader.date = request.date;
                existingHeader.from_auditor = request.from_auditor;
                existingHeader.qty_check = request.qty_check;
                existingHeader.ng_rasio = request.ng_rasio;
                existingHeader.incident_status = request.incident_status;
                existingHeader.reply_deadline = request.reply_deadline;

                // Atur waktu audit otomatis
                existingHeader.updated_at = DateTime.Now;

                // Simpan perubahan
                dbq.SaveChanges();

                return Json(new { status = "success", message = "Data Header berhasil diupdate." });
            }
            catch (Exception ex)
            {
                // MODIFIKASI: Menarik pesan error asli dari database (Inner Exception) agar lebih detail jika terjadi error lagi
                var databaseErrorMessage = ex.InnerException?.InnerException?.Message
                                           ?? ex.InnerException?.Message
                                           ?? ex.Message;

                return Json(new { status = "error", message = "Terjadi kesalahan saat update Header: " + databaseErrorMessage });
            }
        }


        [HttpPost]
        public JsonResult UpsertApprovalRecapEventAudit(ApprovalEventUpsertDto request)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string currentNik = currUser?.GetUserId();

                if (string.IsNullOrEmpty(currentNik))
                {
                    return Json(new { success = false, message = "Your session has expired. Please log in again." });
                }

                // =========================================================================
                // AMBIL DATA PROFIL USER (EMAIL DAN NAMA)
                // =========================================================================
                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == currentNik);

                string currentEmail = userProfile?.Email;
                string currentName = userProfile?.Name;

                if (string.IsNullOrEmpty(currentName))
                {
                    return Json(new { success = false, message = "User name not found in your active profile." });
                }

                if (string.IsNullOrEmpty(request.audit_event_id) ||
                    string.IsNullOrEmpty(request.recap_sign_category) ||
                    string.IsNullOrEmpty(request.recap_sign_status))
                {
                    return Json(new { success = false, message = "Event ID, category, or status cannot be empty." });
                }

                // =========================================================================
                // RULE 1: Pengecekan Otorisasi Berdasarkan Kategori
                // =========================================================================
                if (request.recap_sign_category == "Dibuat" || request.recap_sign_category == "Diperiksa")
                {
                    // CEK ROLE DI BACKEND DARI TABEL Quality_EAudit_Access
                    // Kita juga memastikan data belum di-delete (deleted_at == null)
                    var userAccess = dbq.Quality_EAudit_Access
                        .FirstOrDefault(x => x.NIK == currentNik && x.deleted_at == null);

                    bool isAdministrator = userAccess != null && userAccess.access == "Administrator";

                    if (!isAdministrator)
                    {
                        return Json(new { success = false, message = $"Access denied. Only Administrators can process the '{request.recap_sign_category}' category." });
                    }
                }
                else if (request.recap_sign_category == "Disetujui")
                {
                    if (string.IsNullOrEmpty(currentEmail))
                    {
                        return Json(new { success = false, message = "User email not found in your active profile." });
                    }

                    var auditEvent = dbq.Quality_EAudit_Audit_Event
                        .FirstOrDefault(x => x.audit_event_id == request.audit_event_id);

                    if (auditEvent == null)
                    {
                        return Json(new { success = false, message = "Audit event not found." });
                    }

                    var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                        .FirstOrDefault(x => x.audit_standard_name == auditEvent.audit_standard);

                    if (standardMaster == null ||
                        string.IsNullOrEmpty(standardMaster.mr_email) ||
                        !standardMaster.mr_email.Equals(currentEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { success = false, message = "Access denied. Only the MR associated with this standard can approve it." });
                    }
                }

                // =========================================================================
                // RULE 2: Pengecekan Urutan (Sequence) Approval
                // =========================================================================
                var existingApprovals = dbq.Quality_EAudit_Approval_Recap_Event
                    .Where(x => x.audit_event_id == request.audit_event_id)
                    .ToList();

                if (request.recap_sign_category == "Diperiksa")
                {
                    var stepDibuat = existingApprovals.FirstOrDefault(x => x.recap_sign_category == "Dibuat");
                    if (stepDibuat == null || stepDibuat.recap_sign_status != "Approved")
                    {
                        return Json(new { success = false, message = "Category 'Dibuat' must be approved first before proceeding to 'Diperiksa'." });
                    }
                }
                else if (request.recap_sign_category == "Disetujui")
                {
                    var stepDiperiksa = existingApprovals.FirstOrDefault(x => x.recap_sign_category == "Diperiksa");
                    if (stepDiperiksa == null || stepDiperiksa.recap_sign_status != "Approved")
                    {
                        return Json(new { success = false, message = "Category 'Diperiksa' must be approved first before proceeding to 'Disetujui'." });
                    }
                }

                // =========================================================================
                // RULE 3: Proses Insert / Update (Upsert)
                // =========================================================================
                var targetApproval = existingApprovals.FirstOrDefault(x => x.recap_sign_category == request.recap_sign_category);

                if (targetApproval != null) // Lakukan UPDATE
                {
                    bool isAlreadyProcessed = targetApproval.recap_sign_status == "Approved" ||
                                              targetApproval.recap_sign_status == "Rejected";

                    if (isAlreadyProcessed)
                    {
                        if (targetApproval.recap_sign_by != currentName)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Access denied. This document has already been processed and can only be modified by the original reviewer."
                            });
                        }
                    }

                    targetApproval.recap_sign_status = request.recap_sign_status;

                    if (request.recap_sign_status == "Approved" || request.recap_sign_status == "Rejected")
                    {
                        targetApproval.recap_sign_by = currentName;
                        targetApproval.recap_sign_date = DateTime.Now;
                    }
                    else
                    {
                        targetApproval.recap_sign_by = null;
                        targetApproval.recap_sign_date = null;
                    }

                    dbq.Entry(targetApproval).State = System.Data.Entity.EntityState.Modified;
                }
                else
                {
                    bool isDecided = request.recap_sign_status == "Approved" || request.recap_sign_status == "Rejected";

                    var newApproval = new Quality_EAudit_Approval_Recap_Event
                    {
                        recap_event_approval_id = Guid.NewGuid().ToString(),
                        audit_event_id = request.audit_event_id,
                        recap_sign_category = request.recap_sign_category,
                        recap_sign_status = request.recap_sign_status,
                        recap_sign_by = isDecided ? currentName : null,
                        recap_sign_date = isDecided ? DateTime.Now : (DateTime?)null
                    };

                    dbq.Quality_EAudit_Approval_Recap_Event.Add(newApproval);
                }

                dbq.SaveChanges();

                return Json(new { success = true, message = "Approval status saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An internal error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpsertApprovalRecapAudit(ApprovalUpsertDto request)
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string currentNik = currUser?.GetUserId();
                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == currentNik);

                if (string.IsNullOrEmpty(currentNik))
                {
                    return Json(new { success = false, message = "Your session has expired. Please log in again." });
                }

                if (string.IsNullOrEmpty(request.audit_event_stakeholder_id) ||
                    string.IsNullOrEmpty(request.category_recap) ||
                    string.IsNullOrEmpty(request.recap_sign_role) ||
                    string.IsNullOrEmpty(request.recap_sign_status))
                {
                    return Json(new { success = false, message = "Stakeholder data, category, role, or status cannot be empty." });
                }

                string currentRole = request.recap_sign_role;

                var existingApproval = dbq.Quality_EAudit_Recap_Approval
                    .FirstOrDefault(x => x.audit_event_stakeholder_id == request.audit_event_stakeholder_id
                                      && x.category_recap == request.category_recap
                                      && x.recap_sign_role == currentRole);

                if (existingApproval != null)
                {
                    bool isAlreadyProcessed = existingApproval.recap_sign_status == "Approved" ||
                                               existingApproval.recap_sign_status == "Rejected";

                    if (isAlreadyProcessed)
                    {
                        if (existingApproval.recap_sign_by != currentNik || existingApproval.recap_sign_role != currentRole)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Access denied. This document has already been processed and can only be modified by the original reviewer with the same role."
                            });
                        }
                    }

                    existingApproval.recap_sign_status = request.recap_sign_status;

                    if (request.recap_sign_status == "Approved" || request.recap_sign_status == "Rejected")
                    {
                        existingApproval.recap_sign_by = currentNik;
                        existingApproval.recap_sign_date = DateTime.Now;
                    }
                    else
                    {
                        existingApproval.recap_sign_by = null;
                        existingApproval.recap_sign_date = null;
                    }

                    dbq.Entry(existingApproval).State = System.Data.Entity.EntityState.Modified;
                }
                else
                {
                    bool isDecided = request.recap_sign_status == "Approved" || request.recap_sign_status == "Rejected";

                    var newApproval = new Quality_EAudit_Recap_Approval
                    {
                        recap_id = Guid.NewGuid().ToString(),
                        audit_event_stakeholder_id = request.audit_event_stakeholder_id,
                        category_recap = request.category_recap,
                        recap_sign_role = currentRole,
                        recap_sign_status = request.recap_sign_status,

                        recap_sign_by = isDecided ? currentNik : null,
                        recap_sign_date = isDecided ? DateTime.Now : (DateTime?)null
                    };

                    dbq.Quality_EAudit_Recap_Approval.Add(newApproval);
                }

                dbq.SaveChanges();

                return Json(new { success = true, message = "Approval status saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An internal error occurred: " + ex.Message });
            }
        }
        public ActionResult DownloadExcelTemplateAuditorMaster()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("TemplateAuditor");

                worksheet.Cell(1, 1).Value = "FORM TAMBAH DATA AUDITOR / AUDITIE";
                var titleRange = worksheet.Range(1, 1, 1, 10);
                titleRange.Merge();
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontSize = 14;
                titleRange.Style.Font.FontColor = XLColor.MidnightBlue;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Row(1).Height = 30;

                worksheet.Cell(2, 1).Value = "PILIH PERSON (Ketik Nama/NIK)";
                worksheet.Cell(2, 2).Value = "FULL NAME";
                worksheet.Cell(2, 3).Value = "NIK";
                worksheet.Cell(2, 4).Value = "EMAIL";
                worksheet.Cell(2, 5).Value = "ROLE";
                worksheet.Cell(2, 6).Value = "STATUS";
                worksheet.Cell(2, 7).Value = "MAIN SECTION";
                worksheet.Cell(2, 8).Value = "AUDIT STANDARD";
                worksheet.Cell(2, 9).Value = "RESPONSIBILITY SECTION";
                worksheet.Cell(2, 10).Value = "CATEGORY AUDIT";

                var headerRange = worksheet.Range(2, 1, 2, 10);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Fill.BackgroundColor = XLColor.MidnightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                worksheet.Row(2).Height = 25;

                worksheet.SheetView.FreezeRows(2);

                var accesses = dbq.Quality_EAudit_Access.ToList();
                var sections = dbq.Quality_EAudit_Section.ToList();
                var auditStandards = dbq.Quality_EAudit_Audit_Standard_Master.ToList();

                var refSheet = workbook.Worksheets.Add("ReferenceData");
                refSheet.Hide();

                for (int i = 0; i < accesses.Count; i++)
                {
                    refSheet.Cell(i + 1, 1).Value = $"{accesses[i].NIK} - {accesses[i].name}";
                    refSheet.Cell(i + 1, 2).Value = accesses[i].name;
                    refSheet.Cell(i + 1, 3).Value = accesses[i].NIK;
                    refSheet.Cell(i + 1, 4).Value = accesses[i].email;
                }

                for (int i = 0; i < sections.Count; i++)
                {
                    refSheet.Cell(i + 1, 5).Value = sections[i].section_name;
                }

                for (int i = 0; i < auditStandards.Count; i++)
                {
                    refSheet.Cell(i + 1, 6).Value = auditStandards[i].audit_standard_name;
                }

                refSheet.Cell(1, 7).Value = "Auditor";
                refSheet.Cell(2, 7).Value = "Auditee";

                refSheet.Cell(1, 8).Value = "Active";
                refSheet.Cell(2, 8).Value = "Inactive";

                refSheet.Cell(1, 9).Value = "Process";
                refSheet.Cell(2, 9).Value = "System";
                refSheet.Cell(3, 9).Value = "Product";

                int maxRows = 500;
                var dataRange = worksheet.Range(3, 1, maxRows, 10);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.OutsideBorderColor = XLColor.LightGray;
                dataRange.Style.Border.InsideBorderColor = XLColor.LightGray;
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Range(3, 3, maxRows, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // NIK
                worksheet.Range(3, 5, maxRows, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Role & Status
                worksheet.Range(3, 10, maxRows, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Category Audit

                if (accesses.Any())
                {
                    var personValidation = worksheet.Range($"A3:A{maxRows}").SetDataValidation();
                    personValidation.AllowedValues = XLAllowedValues.List;
                    personValidation.List($"=ReferenceData!$A$1:$A${accesses.Count}");
                    personValidation.ErrorStyle = XLErrorStyle.Stop;
                    personValidation.ErrorTitle = "Data Tidak Valid";
                    personValidation.ErrorMessage = "Silakan pilih data dari daftar yang tersedia.";

                    worksheet.Range($"A3:A{maxRows}").Style.Fill.BackgroundColor = XLColor.AliceBlue;
                }

                var roleValidation = worksheet.Range($"E3:E{maxRows}").SetDataValidation();
                roleValidation.AllowedValues = XLAllowedValues.List;
                roleValidation.List($"=ReferenceData!$G$1:$G$2");

                var statusValidation = worksheet.Range($"F3:F{maxRows}").SetDataValidation();
                statusValidation.AllowedValues = XLAllowedValues.List;
                statusValidation.List($"=ReferenceData!$H$1:$H$2");

                if (sections.Any())
                {
                    var sectionValidation1 = worksheet.Range($"G3:G{maxRows}").SetDataValidation();
                    sectionValidation1.AllowedValues = XLAllowedValues.List;
                    sectionValidation1.List($"=ReferenceData!$E$1:$E${sections.Count}");

                    var sectionValidation2 = worksheet.Range($"I3:I{maxRows}").SetDataValidation();
                    sectionValidation2.AllowedValues = XLAllowedValues.List;
                    sectionValidation2.List($"=ReferenceData!$E$1:$E${sections.Count}");
                    sectionValidation2.ShowErrorMessage = false;
                }

                if (auditStandards.Any())
                {
                    var standardValidation = worksheet.Range($"H3:H{maxRows}").SetDataValidation();
                    standardValidation.AllowedValues = XLAllowedValues.List;
                    standardValidation.List($"=ReferenceData!$F$1:$F${auditStandards.Count}");
                }

                var categoryAuditValidation = worksheet.Range($"J3:J{maxRows}").SetDataValidation();
                categoryAuditValidation.AllowedValues = XLAllowedValues.List;
                categoryAuditValidation.List($"=ReferenceData!$I$1:$I$3");
                categoryAuditValidation.ErrorStyle = XLErrorStyle.Stop;
                categoryAuditValidation.ErrorTitle = "Kategori Tidak Valid";
                categoryAuditValidation.ErrorMessage = "Pilih kategori Process, System, atau Product.";

                for (int row = 3; row <= maxRows; row++)
                {
                    worksheet.Cell(row, 2).FormulaA1 = $"=IF(ISBLANK(A{row}), \"\", VLOOKUP(A{row}, ReferenceData!A:D, 2, FALSE))";
                    worksheet.Cell(row, 3).FormulaA1 = $"=IF(ISBLANK(A{row}), \"\", VLOOKUP(A{row}, ReferenceData!A:D, 3, FALSE))";
                    worksheet.Cell(row, 4).FormulaA1 = $"=IF(ISBLANK(A{row}), \"\", VLOOKUP(A{row}, ReferenceData!A:D, 4, FALSE))";
                }

                worksheet.Range($"B3:D{maxRows}").Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
                worksheet.Range($"B3:D{maxRows}").Style.Font.FontColor = XLColor.DimGray;

                worksheet.Column(1).Width = 35; // Pilih Person
                worksheet.Column(2).Width = 30; // Full Name
                worksheet.Column(3).Width = 15; // NIK
                worksheet.Column(4).Width = 30; // Email
                worksheet.Column(5).Width = 20; // Role
                worksheet.Column(6).Width = 15; // Status
                worksheet.Column(7).Width = 25; // Main Section
                worksheet.Column(8).Width = 25; // Audit Standard
                worksheet.Column(9).Width = 35; // Responsibility Section
                worksheet.Column(10).Width = 20; // Category Audit <-- Tambahan Baru

                // ==========================================
                // 8. GENERATE FILE EXCEL
                // ==========================================
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Master_Auditor.xlsx");
                }
            }
        }

        public ActionResult DownloadExcelTemplateQuestionMaster()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("AuditList");

                // --- 1. CREATE HEADER (Now 5 Columns) ---
                worksheet.Cell(1, 1).Value = "No";
                worksheet.Cell(1, 2).Value = "Category Audit";
                worksheet.Cell(1, 3).Value = "Process Name";
                worksheet.Cell(1, 4).Value = "Clause";
                worksheet.Cell(1, 5).Value = "Audit Parameters & Question List";

                // --- 2. HEADER STYLING ---
                var headerRange = worksheet.Range(1, 1, 1, 5); // Range changed up to column 5
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // --- 3. ADD SAMPLE DATA (ROW 2) ---
                worksheet.Cell(2, 1).Value = 1;
                worksheet.Cell(2, 2).Value = "Product"; // Example choice from the dropdown
                worksheet.Cell(2, 3).Value = "Risk Analysis & Planning";
                worksheet.Cell(2, 4).Value = "6.1";
                worksheet.Cell(2, 5).Value = "Has management identified risk analysis, planning, and action plans integrated with the QMS?";

                // --- 4. APPLY DROPDOWN VALIDATION FOR AUDIT TYPE (ROWS 2 - 1000) ---
                // Using SetDataValidation() according to your ClosedXML version
                var auditTypeValidation = worksheet.Range("B2:B1000").SetDataValidation();
                auditTypeValidation.AllowedValues = XLAllowedValues.List;

                // Inserting a static list directly into the text formula (must be wrapped with internal quotes \" )
                auditTypeValidation.List("\"Product,Process,System\"");

                auditTypeValidation.IgnoreBlanks = true;
                auditTypeValidation.InCellDropdown = true;
                auditTypeValidation.ErrorStyle = XLErrorStyle.Stop;
                auditTypeValidation.ErrorTitle = "Invalid Selection";
                auditTypeValidation.ErrorMessage = "Please select an available Audit Type from the dropdown list (Product / System/Process).";

                // --- 5. CONTENT STYLING & BORDERS ---
                var dataRange = worksheet.Range(1, 1, 2, 5); // Range from header to sample data

                // Set borders for all cells in the data range
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.OutsideBorderColor = XLColor.Black;
                dataRange.Style.Border.InsideBorderColor = XLColor.Black;

                // Set text wrap for the question column to keep it neat if it's long
                worksheet.Column(5).Style.Alignment.WrapText = true;

                // Center align text for No, Audit Type, and Clause columns
                worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Vertical alignment for all data rows (center)
                worksheet.Range(2, 1, 2, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // --- 6. SET COLUMN WIDTHS ---
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 20;
                worksheet.Column(3).Width = 30;
                worksheet.Column(4).Width = 15;
                worksheet.Column(5).Width = 80;

                // --- 7. GENERATE AND DOWNLOAD FILE ---
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    string fileName = "Format_Master_Question_Audit.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(stream.ToArray(), contentType, fileName);
                }
            }
        }

        // ==========================================
        // IMPORT EXCEL
        // ==========================================
        [HttpPost]
        public async Task<JsonResult> ImportMasterQuestion(HttpPostedFileBase file, List<long> sectionIds, int audit_standard_id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Unauthorized access."
                }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();
            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Forbidden access."
                }, JsonRequestBehavior.AllowGet);
            }

            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "The Excel file cannot be empty." });

            string fileExtension = Path.GetExtension(file.FileName);
            if (!string.Equals(fileExtension, ".xlsx", StringComparison.OrdinalIgnoreCase) && !string.Equals(fileExtension, ".xls", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Unsupported file format. Please upload an Excel file (.xlsx / .xls)." });

            if (sectionIds == null || !sectionIds.Any())
                return Json(new { success = false, message = "Please select at least one target Section ID." });

            var targetSectionIds = sectionIds.Select(id => (long)id).ToList();

            var now = DateTime.Now;
            var importedData = new List<Quality_EAudit_Question_Master>();

            try
            {
                // Delete existing questions for the selected sections & standard
                var existingQuestions = dbq.Quality_EAudit_Question_Master
                                            .Where(q => targetSectionIds.Contains(q.section_id) && q.audit_standard_id == audit_standard_id)
                                            .ToList();

                if (existingQuestions.Any())
                {
                    dbq.Quality_EAudit_Question_Master.RemoveRange(existingQuestions);
                    await dbq.SaveChangesAsync();
                }

                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        return Json(new { success = false, message = "Worksheet not found in the Excel file." });

                    var lastRowUsed = worksheet.LastRowUsed();
                    int rowCount = lastRowUsed != null ? lastRowUsed.RowNumber() : 0;

                    // Loop starts at row 2 (Row 1 is Header)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        // SHIFTED COLUMNS: Adjusted index to match the new 5-column template
                        var category_audit = worksheet.Cell(row, 2).GetString()?.Trim();
                        var processName = worksheet.Cell(row, 3).GetString()?.Trim();
                        var clausul = worksheet.Cell(row, 4).GetString()?.Trim();
                        var question = worksheet.Cell(row, 5).GetString()?.Trim();

                        // Skip if both Process Name and Question are empty
                        if (string.IsNullOrEmpty(processName) && string.IsNullOrEmpty(question))
                            continue;

                        foreach (var sectionId in targetSectionIds)
                        {
                            importedData.Add(new Quality_EAudit_Question_Master
                            {
                                section_id = sectionId,
                                audit_standard_id = audit_standard_id,
                                category_audit = category_audit,
                                process_name = processName,
                                clausul = clausul,
                                question = question,
                                created_at = now,
                                deleted_at = null
                            });
                        }
                    }
                }

                if (importedData.Any())
                {
                    dbq.Quality_EAudit_Question_Master.AddRange(importedData);
                    await dbq.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Master questions have been successfully imported.",
                        totalRowsExcel = importedData.Count / targetSectionIds.Count,
                        totalDataInserted = importedData.Count
                    });
                }

                return Json(new { success = false, message = "Existing data was successfully cleared, but no valid new data was found to import from Excel." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "A system error occurred.", error = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult ImportExcelAuditor(HttpPostedFileBase fileUpload)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);

            if (userProfile == null)
            {
                return Json(new { success = false, message = "Unauthorized access." }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null)
            {
                return Json(new { success = false, message = "Forbidden access." }, JsonRequestBehavior.AllowGet);
            }

            if (fileUpload == null || fileUpload.ContentLength <= 0)
            {
                return Json(new { success = false, message = "File not found or is empty." });
            }

            if (!fileUpload.FileName.EndsWith(".xls") && !fileUpload.FileName.EndsWith(".xlsx"))
            {
                return Json(new { success = false, message = "Invalid file format. Please upload an Excel file." });
            }

            using (var transaction = dbq.Database.BeginTransaction())
            {
                try
                {
                    using (var workbook = new XLWorkbook(fileUpload.InputStream))
                    {
                        var worksheet = workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null) return Json(new { success = false, message = "Worksheet not found in the Excel file." });

                        var lastRow = worksheet.LastRowUsed();
                        if (lastRow == null) return Json(new { success = false, message = "The Excel data is empty." });

                        int rowCount = lastRow.RowNumber();
                        int successCount = 0;

                        for (int row = 3; row <= rowCount; row++)
                        {
                            string fullName = worksheet.Cell(row, 2).GetString().Trim();
                            string nik = worksheet.Cell(row, 3).GetString().Trim();

                            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(nik)) continue;

                            string email = worksheet.Cell(row, 4).GetString().Trim();
                            string role = worksheet.Cell(row, 5).GetString().Trim();
                            string status = worksheet.Cell(row, 6).GetString().Trim();
                            string mainSection = worksheet.Cell(row, 7).GetString().Trim();

                            string auditStandardRaw = worksheet.Cell(row, 8).GetString().Trim();
                            string detailSectionRaw = worksheet.Cell(row, 9).GetString().Trim();

                            string categoryAuditRaw = worksheet.Cell(row, 10).GetString().Trim();

                            int auditStandardId = 0;
                            if (!string.IsNullOrEmpty(auditStandardRaw))
                            {
                                var std = dbq.Quality_EAudit_Audit_Standard_Master
                                             .FirstOrDefault(x => x.audit_standard_name == auditStandardRaw);
                                if (std != null) auditStandardId = std.audit_standard_id;
                            }

                            List<int> detailSectionIds = new List<int>();
                            if (!string.IsNullOrEmpty(detailSectionRaw))
                            {
                                var splitSections = detailSectionRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var secName in splitSections)
                                {
                                    string cleanName = secName.Trim();
                                    var dbSec = dbq.Quality_EAudit_Section
                                                   .FirstOrDefault(x => x.section_name == cleanName);

                                    if (dbSec != null) detailSectionIds.Add((int)dbSec.section_id);
                                }
                            }
                            var existingMaster = dbq.Quality_EAudit_Audity_Auditor_Master
                                                    .FirstOrDefault(x => x.nik == nik && x.role == role && x.Deleted_at == null);

                            int masterId;

                            if (existingMaster == null)
                            {
                                var newMaster = new Quality_EAudit_Audity_Auditor_Master
                                {
                                    person_name = fullName,
                                    nik = nik,
                                    email = email,
                                    role = string.IsNullOrEmpty(role) ? "Auditor" : role,
                                    status = string.IsNullOrEmpty(status) ? "Active" : status,
                                    section = mainSection,
                                    created_at = DateTime.Now
                                };
                                dbq.Quality_EAudit_Audity_Auditor_Master.Add(newMaster);
                                dbq.SaveChanges();

                                masterId = newMaster.audity_auditor_id;
                                successCount++;
                            }
                            else
                            {
                                existingMaster.email = email;
                                existingMaster.status = string.IsNullOrEmpty(status) ? existingMaster.status : status;
                                existingMaster.section = mainSection;

                                masterId = existingMaster.audity_auditor_id;
                            }

                            if (auditStandardId > 0 && detailSectionIds.Any())
                            {
                                foreach (var detSecId in detailSectionIds)
                                {
                                    bool detailExists = dbq.Quality_EAudit_Audity_Auditor_Master_Detail
                                                           .Any(d => d.audity_auditor_id == masterId &&
                                                                     d.audit_standard_id == auditStandardId &&
                                                                     d.section_id == detSecId &&
                                                                     d.category_audit == categoryAuditRaw &&
                                                                     d.Deleted_at == null);

                                    if (!detailExists)
                                    {
                                        var newDetail = new Quality_EAudit_Audity_Auditor_Master_Detail
                                        {
                                            audity_auditor_id = masterId,
                                            audit_standard_id = auditStandardId,
                                            section_id = detSecId,
                                            category_audit = categoryAuditRaw,
                                            created_at = DateTime.Now
                                        };
                                        dbq.Quality_EAudit_Audity_Auditor_Master_Detail.Add(newDetail);
                                    }
                                }
                            }
                        }

                        dbq.SaveChanges();
                        transaction.Commit();

                        return Json(new { success = true, message = $"{successCount} profile(s) successfully imported/updated from Excel!" });
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    // Menggali error SQL terdalam jika terjadi kesalahan (misal string kepanjangan)
                    Exception deepestEx = ex;
                    while (deepestEx.InnerException != null)
                    {
                        deepestEx = deepestEx.InnerException;
                    }

                    return Json(new { success = false, message = "A system error occurred: " + deepestEx.Message });
                }
            }
        }


        [HttpGet]
        public ActionResult ExportExcelCAR(string id)
        {
            try
            {
                dynamic dataCAR = FetchCARData(id);

                if (dataCAR == null)
                {
                    return HttpNotFound("Data CAR tidak ditemukan.");
                }

                string templatePath = Server.MapPath("~/Areas/Quality/TemplateExport/PMLK3-MR-11 L1_Form CAR _Rev-00.xlsx");

                using (var workbook = new XLWorkbook(templatePath))
                {
                    var ws = workbook.Worksheet(1);

                    ws.Cell("G5").Value = dataCAR.to_Dept;
                    ws.Cell("E6").Value = dataCAR.attention;
                    ws.Cell("R5").Value = dataCAR.no_report;

                    // PERBAIKAN: Gunakan Convert.ToDateTime()
                    if (dataCAR.date != null && !string.IsNullOrEmpty(dataCAR.date.ToString()))
                        ws.Cell("R6").Value = Convert.ToDateTime(dataCAR.date).ToString("dd-MMM-yyyy");

                    ws.Cell("T7").Value = dataCAR.from_auditor;

                    if (dataCAR.Problem != null)
                    {
                        ws.Cell("B10").Value = dataCAR.Problem.problem_name;
                        ws.Cell("G10").Value = dataCAR.Problem.problem_location;
                        ws.Cell("L10").Value = dataCAR.Problem.problem_part_name;
                        ws.Cell("R10").Value = dataCAR.Problem.problem_lot_number;
                        ws.Cell("V10").Value = dataCAR.Problem.problem_qty;
                        ws.Cell("AA10").Value = dataCAR.qty_check;
                        ws.Cell("AE10").Value = dataCAR.ng_rasio;

                        ws.Cell("B13").Value = dataCAR.Problem.problem_detail;

                        string kat = dataCAR.Problem.problem_category ?? "";
                        ws.Cell("H18").Value =
                            $"[ ] Sarmut/Sarling    [ ] Customer claim    [ ] Produk abnormal\n" +
                            $"[ ] Proses abnormal   [V] Audit (internal)    [ ] Lain-lain";

                        string stat = (dataCAR.Problem.problem_status ?? "").ToUpper();

                        ws.Cell("B20").Value =
                            $"{(stat == "CRITICAL" ? "[V]" : "[ ]")} Critical     " +
                            $"{(stat == "MAJOR" ? "[V]" : "[ ]")} Major     " +
                            $"{(stat == "MEDIUM" ? "[V]" : "[ ]")} Medium     " +
                            $"{(stat == "MINOR" ? "[V]" : "[ ]")} Minor     " +
                            $"{(stat == "OFI" ? "[V]" : "[ ]")} OFI";
                    }

                    // Status Kejadian (Radio Button)
                    string cbPertama = dataCAR.incident_status == "Pertama" ? "[ V ]" : "[   ]";
                    string cbPernah = dataCAR.incident_status == "Pernah" ? "[ V ]" : "[   ]";
                    ws.Cell("AH10").Value = $"{cbPertama} Pertama terjadi\n{cbPernah} Pernah terjadi";

                    // PERBAIKAN: Gunakan Convert.ToDateTime()
                    if (dataCAR.reply_deadline != null && !string.IsNullOrEmpty(dataCAR.reply_deadline.ToString()))
                        ws.Cell("AE18").Value = Convert.ToDateTime(dataCAR.reply_deadline).ToString("dd-MMM-yyyy");

                    // --- TAB 2: ANALYSIS & CORRECTIVE ACTIONS ---
                    if (dataCAR.Action != null)
                    {
                        ws.Cell("T21").Value = dataCAR.Action.PIC_dept;

                        // PERBAIKAN: Gunakan Convert.ToDateTime()
                        if (dataCAR.Action.action_date != null && !string.IsNullOrEmpty(dataCAR.Action.action_date.ToString()))
                        {
                            DateTime actDate = Convert.ToDateTime(dataCAR.Action.action_date);
                            ws.Cell("T23").Value = $"Tgl: {actDate:dd} Bln: {actDate:MM} Th: {actDate:yyyy}";
                        }

                        ws.Cell("L39").Value = dataCAR.Action.review_PFMEA;

                        ws.Cell("B42").Value = dataCAR.Action.containment_action;
                        ws.Cell("AG42").Value = dataCAR.Action.PIC_containment_action;

                        if (dataCAR.Action.date_containment_action != null && !string.IsNullOrEmpty(dataCAR.Action.date_containment_action.ToString()))
                            ws.Cell("AJ42").Value = Convert.ToDateTime(dataCAR.Action.date_containment_action).ToString("dd-MMM-yyyy");

                        ws.Cell("B47").Value = dataCAR.Action.detail_correction;
                        ws.Cell("AG47").Value = dataCAR.Action.PIC_detail_correction;

                        if (dataCAR.Action.date_detail_correction != null && !string.IsNullOrEmpty(dataCAR.Action.date_detail_correction.ToString()))
                            ws.Cell("AJ47").Value = Convert.ToDateTime(dataCAR.Action.date_detail_correction).ToString("dd-MMM-yyyy");

                        ws.Cell("B52").Value = dataCAR.Action.detail_corrective_action;
                        ws.Cell("AG52").Value = dataCAR.Action.PIC_detail_corrective_action;

                        if (dataCAR.Action.date_detail_corrective_action != null && !string.IsNullOrEmpty(dataCAR.Action.date_detail_corrective_action.ToString()))
                            ws.Cell("AJ52").Value = Convert.ToDateTime(dataCAR.Action.date_detail_corrective_action).ToString("dd-MMM-yyyy");

                        ws.Cell("B57").Value = dataCAR.Action.yokotenkai;
                        ws.Cell("AG57").Value = dataCAR.Action.PIC_yokotenkai;

                        if (dataCAR.Action.date_yokotenkai != null && !string.IsNullOrEmpty(dataCAR.Action.date_yokotenkai.ToString()))
                            ws.Cell("AJ57").Value = Convert.ToDateTime(dataCAR.Action.date_yokotenkai).ToString("dd-MMM-yyyy");

                        ws.Cell("I58").Value = dataCAR.Action.initial_lot_after_repair;

                        // Dokumen Diperbaharui
                        string docYa = dataCAR.Action.is_document_updated == "Ya" ? "[V]" : "[ ]";
                        string docTidak = dataCAR.Action.is_document_updated == "Tidak" ? "[V]" : "[ ]";
                        ws.Cell("B60").Value = $"{docYa} Ya      {docTidak} Tidak";

                        string docs = dataCAR.Action.document_type_updated ?? "";
                        ws.Cell("L59").Value =
                            $"{(docs.Contains("SOP") ? "[V]" : "[ ]")} SOP/WI      {(docs.Contains("Check Sheet") ? "[V]" : "[ ]")} Check sheet      {(docs.Contains("Control Plan") ? "[V]" : "[ ]")} Control Plan      {(docs.Contains("Lain") ? "[V]" : "[ ]")} Lain-lain";

                        ws.Cell("R60").Value = dataCAR.Action.document_number;
                    }

                    // 5-Why
                    if (dataCAR.FiveWhy != null)
                    {
                        foreach (var fw in dataCAR.FiveWhy)
                        {
                            if (fw.why_number == 1) ws.Cell("N27").Value = fw.why_problem;
                            if (fw.why_number == 2) ws.Cell("N29").Value = fw.why_problem;
                            if (fw.why_number == 3) ws.Cell("N31").Value = fw.why_problem;
                            if (fw.why_number == 4) ws.Cell("N33").Value = fw.why_problem;
                            if (fw.why_number == 5) ws.Cell("N35").Value = fw.why_problem;
                        }
                    }

                    // --- TAB 3: VERIFICATION ---
                    if (dataCAR.Verification != null)
                    {
                        var verifList = ((IEnumerable<dynamic>)dataCAR.Verification).ToList();

                        var v1 = verifList.LastOrDefault(v => v.verification_name == "Verifikasi 1");
                        var v2 = verifList.LastOrDefault(v => v.verification_name == "Verifikasi 2");

                        if (v1 != null)
                        {
                            ws.Cell("B62").Value =
                                $"{(v1.verification_status == "Selesai" ? "[ V ]" : "[   ]")} Corrective actions sudah dilakukan(Selesai)\n\n" +
                                $"{(v1.verification_status == "Belum Selesai" ? "[ V ]" : "[   ]")} Status belum selesai ( lihat comment )";
                            ws.Cell("H62").Value = v1.verification_comment;
                        }

                        if (v2 != null)
                        {
                            // PERBAIKAN: Gunakan Convert.ToDateTime()
                            if (v2.verification_mr_date != null && !string.IsNullOrEmpty(v2.verification_mr_date.ToString()))
                                ws.Cell("AI62").Value = Convert.ToDateTime(v2.verification_mr_date).ToString("dd-MMM-yyyy");

                            ws.Cell("X63").Value =
                                $"{(v2.verification_status == "Effektif" ? "[ V ]" : "[   ]")} Effektif     {(v2.verification_status == "Tidak Effektif" ? "[ V ]" : "[   ]")} Tdk Effektif";
                            ws.Cell("X64").Value = $"Comments: \n{v2.verification_comment}";
                        }
                    }

                    if (dataCAR.Approval != null)
                    {
                        var appList = ((IEnumerable<dynamic>)dataCAR.Approval).ToList();

                        // 1. AUDITOR APPROVAL (Baris 6 & 8)
                        InsertDynamicStampExcel(ws, "AA6", appList, "Auditor Approval", "Disetujui");
                        ws.Cell("AA8").Value = GetApprovalNameDynamic(appList, "Auditor Approval", "Disetujui");

                        InsertDynamicStampExcel(ws, "AE6", appList, "Auditor Approval", "Diperiksa");
                        ws.Cell("AE8").Value = GetApprovalNameDynamic(appList, "Auditor Approval", "Diperiksa");

                        InsertDynamicStampExcel(ws, "AI6", appList, "Auditor Approval", "Dibuat");
                        ws.Cell("AI8").Value = GetApprovalNameDynamic(appList, "Auditor Approval", "Dibuat");

                        // 2. AUDITEE APPROVAL (Baris 21 & 23)
                        InsertDynamicStampExcel(ws, "AD21", appList, "Auditee Approval", "Disetujui");
                        ws.Cell("AD23").Value = GetApprovalNameDynamic(appList, "Auditee Approval", "Disetujui");

                        InsertDynamicStampExcel(ws, "AG21", appList, "Auditee Approval", "Diperiksa");
                        ws.Cell("AG23").Value = GetApprovalNameDynamic(appList, "Auditee Approval", "Diperiksa");

                        InsertDynamicStampExcel(ws, "AJ21", appList, "Auditee Approval", "Dibuat");
                        ws.Cell("AJ23").Value = GetApprovalNameDynamic(appList, "Auditee Approval", "Dibuat");
                    }

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        string fileName = $"CAR_{dataCAR.no_report}_{DateTime.Now:yyyyMMdd}.xlsx";

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (System.Exception ex)
            {
                return Content($"Gagal melakukan Export Excel: {ex.Message}");
            }
        }

        private void InsertDynamicStampExcel(IXLWorksheet ws, string cellRef, List<dynamic> approvals, string group, string role)
        {
            var ap = approvals.FirstOrDefault(a => a.group_type == group && a.role_type == role);

            if (ap != null && !string.IsNullOrEmpty(ap.status))
            {
                string status = ap.status.ToUpper();
                string dateStr = ap.sign_date != null ? ((DateTime)ap.sign_date).ToString("dd MMM yyyy").ToUpper() : "";

                // PERBAIKAN: Gunakan System.Drawing.Color secara eksplisit
                System.Drawing.Color stampColor = status == "APPROVED"
                    ? System.Drawing.Color.FromArgb(5, 150, 105)  // Hijau
                    : System.Drawing.Color.FromArgb(220, 38, 38); // Merah

                // Buat kanvas gambar kosong berukuran 200x200 pixel
                using (Bitmap bmp = new Bitmap(200, 200))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        // Pengaturan agar kualitas gambar tajam dan tidak pecah (Anti-Alias)
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;

                        // PERBAIKAN: Gunakan System.Drawing.Color.Transparent
                        g.Clear(System.Drawing.Color.Transparent);

                        // Buat stempel miring / rotasi -12 derajat seperti di web
                        g.TranslateTransform(100, 100);
                        g.RotateTransform(-12);
                        g.TranslateTransform(-100, -100);

                        // 1. Gambar Garis Lingkaran Luar
                        using (Pen penOuter = new Pen(stampColor, 6))
                        {
                            g.DrawEllipse(penOuter, 10, 10, 180, 180);
                        }

                        // 2. Gambar Garis Lingkaran Dalam
                        using (Pen penInner = new Pen(stampColor, 2))
                        {
                            g.DrawEllipse(penInner, 18, 18, 164, 164);
                        }

                        // 3. Tulis Teks Status (APPROVED / REJECTED)
                        using (Font fontStatus = new Font("Arial", 26, FontStyle.Bold))
                        {
                            SizeF textSize = g.MeasureString(status, fontStatus);
                            g.DrawString(status, fontStatus, new SolidBrush(stampColor), (200 - textSize.Width) / 2, 65);
                        }

                        // 4. Gambar Garis Pemisah (Border-top tanggal)
                        using (Pen penLine = new Pen(stampColor, 2))
                        {
                            g.DrawLine(penLine, 40, 105, 160, 105);
                        }

                        // 5. Tulis Teks Tanggal di dalam cap
                        using (Font fontDate = new Font("Arial", 16, FontStyle.Bold))
                        {
                            SizeF dateSize = g.MeasureString(dateStr, fontDate);
                            g.DrawString(dateStr, fontDate, new SolidBrush(stampColor), (200 - dateSize.Width) / 2, 115);
                        }
                    }

                    // Simpan hasil lukisan gambar ke dalam MemoryStream
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;

                        var cell = ws.Cell(cellRef);
                        string shortName = "Stamp_" + Guid.NewGuid().ToString("N").Substring(0, 10);

                        ws.AddPicture(ms, shortName)
                          .MoveTo(cell)
                          .Scale(0.25);
                    }
                }
            }
        }


        private string GetApprovalNameDynamic(List<dynamic> approvals, string group, string role)
        {
            var ap = approvals.FirstOrDefault(a => a.group_type == group && a.role_type == role);
            return ap != null && ap.status == "Approved" ? ap.sign_by : "";
        }


        [HttpPost]
        public async Task<JsonResult> ImportQuestionEvent(HttpPostedFileBase file, string audit_event_stakeholder_id, string section_name)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new { success = false, message = "Unauthorized access." }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbq.Quality_EAudit_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();
            if (userAccess == null)
            {
                return Json(new { success = false, message = "Forbidden access." }, JsonRequestBehavior.AllowGet);
            }

            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "The Excel file cannot be empty." });

            string fileExtension = Path.GetExtension(file.FileName);
            if (!string.Equals(fileExtension, ".xlsx", StringComparison.OrdinalIgnoreCase) && !string.Equals(fileExtension, ".xls", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Unsupported file format. Please upload an Excel file (.xlsx / .xls)." });

            if (string.IsNullOrEmpty(audit_event_stakeholder_id))
                return Json(new { success = false, message = "Audit Event Stakeholder ID cannot be empty." });

            var now = DateTime.Now;
            var importedData = new List<Quality_EAudit_Question_Event>();

            try
            {
                string prefix = !string.IsNullOrEmpty(section_name) && section_name.Length >= 2
                                ? section_name.Substring(0, 2).ToUpper()
                                : (section_name ?? "XX").PadRight(2, 'X').ToUpper();

                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        return Json(new { success = false, message = "Worksheet not found in the Excel file." });

                    var lastRowUsed = worksheet.LastRowUsed();
                    int rowCount = lastRowUsed != null ? lastRowUsed.RowNumber() : 0;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var category_audit = worksheet.Cell(row, 2).GetString()?.Trim();
                        var processName = worksheet.Cell(row, 3).GetString()?.Trim();
                        var clausul = worksheet.Cell(row, 4).GetString()?.Trim();
                        var question = worksheet.Cell(row, 5).GetString()?.Trim();

                        if (string.IsNullOrEmpty(processName) && string.IsNullOrEmpty(question))
                            continue;

                        string dateString = now.ToString("yyyyMMddHHmmss") + row.ToString("D3");
                        var questionId = $"QUID-{prefix}-{dateString}";

                        importedData.Add(new Quality_EAudit_Question_Event
                        {
                            question_id = questionId,
                            audit_event_stakeholder_id = audit_event_stakeholder_id,
                            category_audit = category_audit,
                            process_name = processName,
                            clausul = clausul,
                            question = question,

                            score = null,
                            criteria = null,
                            judgement = null,
                            note = null,

                            created_at = now,
                            updated_at = null,
                            deleted_at = null
                        });
                    }
                }

                // 6. Save Data
                if (importedData.Any())
                {
                    dbq.Quality_EAudit_Question_Event.AddRange(importedData);
                    await dbq.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Event questions have been successfully imported.",
                        totalDataInserted = importedData.Count
                    });
                }

                return Json(new { success = false, message = "Existing data was successfully cleared, but no valid new data was found to import from Excel." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "A system error occurred.", error = ex.Message });
            }
        }

        private object FetchCARData(string id)
        {
            var dataCAR = dbq.Quality_EAudit_CAR_Header
                .Include(c => c.Problem)
                .Include(c => c.Action)
                .Include(c => c.FiveWhy)
                .Include(c => c.Verification)
                .Include(c => c.Ilustration)
                .Include(c => c.Approval)
                .Include(c => c.Evidence)
                .FirstOrDefault(c => c.no_report == id);

            if (dataCAR == null)
            {
                return null;
            }

            var responseData = new
            {
                no_report = dataCAR.no_report,
                question_id = dataCAR.question_id,
                date = dataCAR.date,
                to_Dept = dataCAR.to_Dept,
                attention = dataCAR.attention,
                from_auditor = dataCAR.from_auditor,
                qty_check = dataCAR.qty_check,
                ng_rasio = dataCAR.ng_rasio,
                incident_status = dataCAR.incident_status,
                reply_deadline = dataCAR.reply_deadline,
                created_at = dataCAR.created_at,
                updated_at = dataCAR.updated_at,

                Problem = dataCAR.Problem != null && dataCAR.Problem.Any()
                    ? dataCAR.Problem.Select(p => new
                    {
                        p.problem_id,
                        p.no_report,
                        p.problem_name,
                        p.problem_location,
                        p.problem_part_name,
                        p.problem_lot_number,
                        p.problem_qty,
                        p.problem_detail,
                        p.problem_category,
                        p.problem_status
                    }).FirstOrDefault()
                    : null,

                Action = dataCAR.Action != null && dataCAR.Action.Any()
                    ? dataCAR.Action.Select(a => new
                    {
                        a.action_id,
                        a.no_report,
                        a.PIC_dept,
                        a.action_date,
                        a.review_PFMEA,
                        a.containment_action,
                        a.PIC_containment_action,
                        a.date_containment_action,
                        a.detail_correction,
                        a.PIC_detail_correction,
                        a.date_detail_correction,
                        a.detail_corrective_action,
                        a.PIC_detail_corrective_action,
                        a.date_detail_corrective_action,
                        a.yokotenkai,
                        a.initial_lot_after_repair,
                        a.PIC_yokotenkai,
                        a.date_yokotenkai,
                        a.is_document_updated,
                        a.document_type_updated,
                        a.document_number
                    }).FirstOrDefault()
                    : null,

                Verification = dataCAR.Verification != null && dataCAR.Verification.Any()
                    ? dataCAR.Verification.Select(v => new
                    {
                        v.verification_id,
                        v.no_report,
                        v.verification_name,
                        v.verification_comment,
                        v.verification_auditor_sign_by,
                        v.verification_auditor_status,
                        v.verification_auditor_date,
                        v.verification_mr_date,
                        v.verification_mr_sign_by,
                        v.verification_mr_status,
                        v.verification_status
                    }).ToList()
                    : null,

                Approval = dataCAR.Approval != null && dataCAR.Approval.Any()
                    ? dataCAR.Approval.Select(ap => new
                    {
                        ap.approval_id,
                        ap.no_report,
                        ap.group_type,
                        ap.role_type,
                        ap.sign_by,
                        ap.sign_date,
                        ap.status
                    }).ToList()
                    : null,

                FiveWhy = dataCAR.FiveWhy != null && dataCAR.FiveWhy.Any()
                    ? dataCAR.FiveWhy.Select(fw => new
                    {
                        fw.why_id,
                        fw.no_report,
                        fw.why_number,
                        fw.why_problem
                    }).ToList()
                    : null,

                Ilustration = dataCAR.Ilustration != null && dataCAR.Ilustration.Any()
                    ? dataCAR.Ilustration.Select(img => new
                    {
                        img.ilustration_id,
                        img.no_report,
                        img.file_url
                    }).ToList()
                    : null,

                Evidence = dataCAR.Evidence != null && dataCAR.Evidence.Any()
                    ? dataCAR.Evidence.Select(img => new
                    {
                        img.evidence_id,
                        img.no_report,
                        img.evidence_name,
                        img.evidence_url
                    }).ToList()
                    : null
            };

            return responseData;
        }

        

        public ActionResult ExportExcelOFI(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound("ID tidak valid.");

            using (var db = new QualityConnection())
            {
                // 1. QUERY DATA DARI DATABASE (Include Relasi)
                var ofiHeader = db.Quality_EAudit_OFI_Header
                    .Include(x => x.OFIDetails)
                    .Include(x => x.ApprovalOFI)
                    .Include(x => x.AuditEventStakeholder)
                    .Include(x => x.AuditEventStakeholder.AuditEvent)
                    .FirstOrDefault(x => x.ofi_header_id == id);

                if (ofiHeader == null)
                    return HttpNotFound("Data OFI tidak ditemukan.");

                string kriteria = ofiHeader.AuditEventStakeholder?.AuditEvent?.audit_standard ?? "-";
                string proses = ofiHeader.AuditEventStakeholder?.responsibility_section ?? "-";

                string startStr = ofiHeader.AuditEventStakeholder?.start_event?.ToString("dd JUN yyyy").ToUpper() ?? "-";
                string endStr = ofiHeader.AuditEventStakeholder?.end_event?.ToString("dd JUN yyyy").ToUpper() ?? "-";
                string periode = $"{startStr} s/d {endStr}";

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("OFI Report");

                    // =========================================================
                    // 0. GRID SYSTEM (29 Kolom dengan lebar 3.89 fix)
                    // =========================================================
                    for (int i = 1; i <= 29; i++)
                    {
                        ws.Column(i).Width = 3.89;
                    }

                    // =========================================================
                    // 1. KOP LAPORAN (HEADER)
                    // =========================================================

                    // -- KOTAK LOGO (Merge Kolom 1-3, Baris 1-4) <-- DIUBAH KE BARIS 4
                    var rangeLogo = ws.Range(1, 1, 4, 3).Merge();
                    rangeLogo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rangeLogo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    rangeLogo.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                    try
                    {
                        string imagePath = Server.MapPath("~/Images/niterra-logo.jpg");
                        if (System.IO.File.Exists(imagePath))
                        {
                            var image = ws.AddPicture(imagePath)
                                          .MoveTo(ws.Cell(1, 1), 5, 5) // Margin gambar
                                          .WithSize(92, 51);
                        }
                        else
                        {
                            ws.Cell(1, 1).Value = "Niterra\nPT NMI";
                            ws.Cell(1, 1).Style.Font.Bold = true;
                            ws.Cell(1, 1).Style.Font.FontSize = 8;
                            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0f766e");
                            ws.Cell(1, 1).Style.Alignment.WrapText = true;
                        }
                    }
                    catch { }

                    // -- KOTAK JUDUL UTAMA DIGABUNG (Merge Kolom 4-29, Baris 1-4) <-- DIUBAH KE BARIS 4
                    var rangeJudul = ws.Range(1, 4, 4, 29).Merge();

                    // WAJIB: WrapText agar teks bisa turun ke bawah
                    rangeJudul.Style.Alignment.WrapText = true;
                    rangeJudul.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rangeJudul.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var cellJudul = ws.Cell(1, 4);
                    cellJudul.Value = "";
                    cellJudul.RichText.ClearText();

                    // Baris 1: Judul Utama
                    cellJudul.RichText.AddText("OPPORTUNITIES FOR IMPROVEMENT").SetBold().SetFontSize(14);
                    cellJudul.RichText.AddNewLine();

                    // Baris 2: Kriteria
                    cellJudul.RichText.AddText($"Kriteria : {kriteria}").SetFontSize(11);
                    cellJudul.RichText.AddNewLine();

                    // Baris 3: Periode
                    cellJudul.RichText.AddText($"Periode : {periode}").SetFontSize(10).SetFontColor(XLColor.FromHtml("#2563eb"));

                    // Bingkai tebal luar area Kop (Baris 1-4)
                    ws.Range(1, 1, 4, 29).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

                    // -- BARIS 5: SUB-HEADER PROSES <-- DIUBAH KE BARIS 5
                    var rangeProses = ws.Range(5, 1, 5, 29).Merge();
                    rangeProses.Value = $"  Proses : {proses}";
                    rangeProses.Style.Font.Bold = true;
                    rangeProses.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                    rangeProses.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Row(5).Height = 22;

                    // =========================================================
                    // 2. HEADER TABEL (BARIS KE-6)
                    // =========================================================
                    int currentRow = 6;

                    var thClause = ws.Range(currentRow, 1, currentRow, 3).Merge();
                    thClause.Value = "Clause";

                    var thObs = ws.Range(currentRow, 4, currentRow, 14).Merge(); // 11 kolom
                    thObs.Value = "Observation and comments";

                    var thPlan = ws.Range(currentRow, 15, currentRow, 26).Merge(); // 12 kolom
                    thPlan.Value = "Action Plan";

                    var thDate = ws.Range(currentRow, 27, currentRow, 29).Merge();
                    thDate.Value = "Date";

                    var tableHeader = ws.Range(currentRow, 1, currentRow, 29);
                    tableHeader.Style.Font.Bold = true;
                    tableHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#f0fdfa");
                    tableHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    tableHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    tableHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    ws.Row(currentRow).Height = 25;

                    // =========================================================
                    // 3. TABEL DETAIL (LOOPING DATA MODEL)
                    // =========================================================
                    currentRow++;

                    // --> PERSIAPAN TRIK KOLOM TERSEMBUNYI <--
                    // Total lebar 11 kolom (Observation) = 11 * 3.89 = 42.79
                    ws.Column(30).Width = 42.79;
                    ws.Column(30).Hide(); // Sembunyikan kolom

                    // Total lebar 12 kolom (Action Plan) = 12 * 3.89 = 46.68
                    ws.Column(31).Width = 46.68;
                    ws.Column(31).Hide(); // Sembunyikan kolom


                    if (ofiHeader.OFIDetails != null && ofiHeader.OFIDetails.Any())
                    {
                        foreach (var dtl in ofiHeader.OFIDetails)
                        {
                            string obsText = dtl.observation_comment ?? "";
                            string planText = dtl.action_plan ?? "";

                            // -- TAMPILAN UTAMA (CELL YANG DI-MERGE) --
                            var tdClause = ws.Range(currentRow, 1, currentRow, 3).Merge();
                            tdClause.Value = dtl.clause ?? "-";
                            tdClause.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            tdClause.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            tdClause.Style.Font.Bold = true;

                            var tdObs = ws.Range(currentRow, 4, currentRow, 14).Merge();
                            tdObs.Value = obsText;
                            tdObs.Style.Alignment.WrapText = true;
                            tdObs.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                            var tdPlan = ws.Range(currentRow, 15, currentRow, 26).Merge();
                            tdPlan.Value = planText;
                            tdPlan.Style.Alignment.WrapText = true;
                            tdPlan.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                            var tdDate = ws.Range(currentRow, 27, currentRow, 29).Merge();
                            tdDate.Value = dtl.action_date?.ToString("yyyy-MM-dd") ?? "-";
                            tdDate.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            tdDate.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                            ws.Range(currentRow, 1, currentRow, 29).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            ws.Range(currentRow, 1, currentRow, 29).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                            ws.Cell(currentRow, 30).Value = obsText;
                            ws.Cell(currentRow, 30).Style.Alignment.WrapText = true;

                            ws.Cell(currentRow, 31).Value = planText;
                            ws.Cell(currentRow, 31).Style.Alignment.WrapText = true;

                            ws.Row(currentRow).AdjustToContents();

                            ws.Row(currentRow).Height = ws.Row(currentRow).Height + 5;

                            currentRow++;
                        }
                    }
                    else
                    {
                        ws.Range(currentRow, 1, currentRow, 29).Merge().Value = "Tidak ada data OFI detail.";
                        ws.Range(currentRow, 1, currentRow, 29).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(currentRow, 1, currentRow, 29).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        currentRow++;
                    }

                    currentRow += 2;

                    // =========================================================
                    // 4. BAGIAN TANDA TANGAN
                    // =========================================================
                    var headSetuju = ws.Range(currentRow, 21, currentRow, 23).Merge();
                    headSetuju.Value = "Disetujui,";
                    headSetuju.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var headPeriksa = ws.Range(currentRow, 24, currentRow, 26).Merge();
                    headPeriksa.Value = "Diperiksa,";
                    headPeriksa.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var headBuat = ws.Range(currentRow, 27, currentRow, 29).Merge();
                    headBuat.Value = "Dibuat,";
                    headBuat.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var ttdHeaderArea = ws.Range(currentRow, 21, currentRow, 29);
                    ttdHeaderArea.Style.Font.Bold = true;
                    ttdHeaderArea.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ttdHeaderArea.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");

                    currentRow++;
                    ws.Row(currentRow).Height = 55;

                    var boxSetuju = ws.Range(currentRow, 21, currentRow, 23).Merge();
                    boxSetuju.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var boxPeriksa = ws.Range(currentRow, 24, currentRow, 26).Merge();
                    boxPeriksa.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var boxBuat = ws.Range(currentRow, 27, currentRow, 29).Merge();
                    boxBuat.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var setuju = ofiHeader.ApprovalOFI?.FirstOrDefault(x => x.sign_type == "DiSetujui");
                    var periksa = ofiHeader.ApprovalOFI?.FirstOrDefault(x => x.sign_type == "Diperiksa");
                    var buat = ofiHeader.ApprovalOFI?.FirstOrDefault(x => x.sign_type == "Dibuat");

                    // Pastikan Controller Anda punya fungsi InsertStampToCell
                    InsertStampToCell(ws, currentRow, 21, setuju?.status, setuju?.sign_date);
                    InsertStampToCell(ws, currentRow, 24, periksa?.status, periksa?.sign_date);
                    InsertStampToCell(ws, currentRow, 27, buat?.status, buat?.sign_date);

                    currentRow++;

                    var nameSetuju = ws.Range(currentRow, 21, currentRow, 23).Merge();
                    nameSetuju.Value = setuju?.sign_by ?? "";
                    nameSetuju.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var namePeriksa = ws.Range(currentRow, 24, currentRow, 26).Merge();
                    namePeriksa.Value = periksa?.sign_by ?? "";
                    namePeriksa.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var nameBuat = ws.Range(currentRow, 27, currentRow, 29).Merge();
                    nameBuat.Value = buat?.sign_by ?? "";
                    nameBuat.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    var ttdNameArea = ws.Range(currentRow, 21, currentRow, 29);
                    ttdNameArea.Style.Font.Bold = true;
                    ttdNameArea.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ttdNameArea.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    ttdNameArea.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    ttdNameArea.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                    // =========================================================
                    // 5. GENERATE EXCEL STREAM & DOWNLOAD
                    // =========================================================
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        string safeProsesName = string.Join("_", proses.Split(Path.GetInvalidFileNameChars()));
                        string fileName = $"OFI_Report_{safeProsesName}.xlsx";

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
        }

        

        private void InsertStampToCell(IXLWorksheet sheet, int row, int col, string status, DateTime? date)
        {
            if (string.IsNullOrEmpty(status)) return;

            using (MemoryStream stampStream = GenerateStampImage(status, date))
            {
                if (stampStream != null)
                {
                    sheet.AddPicture(stampStream)
                         .MoveTo(sheet.Cell(row, col), 14, 3)
                         .WithSize(65, 65);
                }
            }
        }

        private MemoryStream GenerateStampImage(string status, DateTime? signDate)
        {
            if (string.IsNullOrEmpty(status)) return null;

            int size = 150;
            Bitmap bmp = new Bitmap(size, size);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                // Mencegah error ambiguous dengan mendefinisikan secara eksplisit System.Drawing.Color
                g.Clear(System.Drawing.Color.Transparent);

                System.Drawing.Color stampColor = System.Drawing.Color.Black;
                string statusText = "";

                if (status == "Approved")
                {
                    stampColor = System.Drawing.ColorTranslator.FromHtml("#059669");
                    statusText = "APPROVED";
                }
                else if (status == "Rejected")
                {
                    stampColor = System.Drawing.ColorTranslator.FromHtml("#dc2626");
                    statusText = "REJECTED";
                }
                else if (status == "Signed")
                {
                    stampColor = System.Drawing.ColorTranslator.FromHtml("#2563eb");
                    statusText = "SIGNED";
                }

                if (string.IsNullOrEmpty(statusText)) return null;

                Pen thickPen = new Pen(stampColor, 4f);
                Pen thinPen = new Pen(stampColor, 1.5f);

                // Efek Transformasi Rotasi Kemiringan -12 Derajat (Persis CSS Web)
                g.TranslateTransform(size / 2, size / 2);
                g.RotateTransform(-12);
                g.TranslateTransform(-size / 2, -size / 2);

                // 1. Gambar Lingkaran Luar
                int padOuter = 8;
                g.DrawEllipse(thickPen, padOuter, padOuter, size - 2 * padOuter, size - 2 * padOuter);

                // 2. Gambar Lingkaran Dalam
                int padInner = 14;
                g.DrawEllipse(thinPen, padInner, padInner, size - 2 * padInner, size - 2 * padInner);

                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                // 3. Tulis Teks Utama (APPROVED / SIGNED)
                float fontSize = status == "Signed" ? 18f : 19f;
                Font fontStatus = new Font("Arial", fontSize, FontStyle.Bold);
                RectangleF rectStatus = new RectangleF(0, size / 2 - 25, size, 30);
                g.DrawString(statusText, fontStatus, new SolidBrush(stampColor), rectStatus, sf);

                // 4. Gambar Garis Tengah Pembatas Tanggal
                int lineY = size / 2 + 8;
                g.DrawLine(thinPen, size / 2 - 40, lineY, size / 2 + 40, lineY);

                // 5. Tulis Teks Tanggal Stempel
                string dateStr = signDate.HasValue ? signDate.Value.ToString("dd MMM yyyy").ToUpper() : DateTime.Now.ToString("dd MMM yyyy").ToUpper();
                Font fontDate = new Font("Arial", 12f, FontStyle.Bold);
                RectangleF rectDate = new RectangleF(0, lineY + 2, size, 25);
                g.DrawString(dateStr, fontDate, new SolidBrush(stampColor), rectDate, sf);
            }

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }

        [HttpGet]
        public ActionResult ExportExcelLaporanTemuan(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound("ID Stakeholder tidak valid.");

            try
            {
                using (var dbq = new QualityConnection())
                {
                    var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                        .FirstOrDefault(s => s.audit_event_stakeholder_id == id);

                    if (stakeholder == null)
                        return HttpNotFound("Data Audit tidak ditemukan.");

                    var eventMaster = dbq.Quality_EAudit_Audit_Event
                        .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                    string kriteria = eventMaster?.audit_standard ?? "-";
                    string proses = stakeholder.responsibility_section ?? "-";
                    string startStr = stakeholder.start_event?.ToString("dd-MMM-yy") ?? "-";
                    string endStr = stakeholder.end_event?.ToString("dd-MMM-yy") ?? "-";
                    string periode = $"{startStr} s/d {endStr}";
                    string tanggalAudit = stakeholder.start_event?.ToString("dd-MMM-yyyy") ?? "-";

                    var approvals = dbq.Quality_EAudit_Recap_Approval
                        .Where(a => a.audit_event_stakeholder_id == id && a.category_recap == "Laporan Temuan")
                        .ToList();

                    var signerNiks = approvals.Where(a => !string.IsNullOrEmpty(a.recap_sign_by))
                                              .Select(a => a.recap_sign_by)
                                              .Distinct()
                                              .ToList();

                    var accessUsers = dbq.Quality_EAudit_Access
                        .Where(u => signerNiks.Contains(u.NIK))
                        .ToList();

                    var validJudgements = new string[] { "major", "minor", "ofi" };
                    var questions = dbq.Quality_EAudit_Question_Event
                        .Where(q => q.audit_event_stakeholder_id == id && q.deleted_at == null && q.judgement != null)
                        .Include(q => q.Finding)
                        .Include(q => q.CARs.Select(c => c.Verification))
                        .Include(q => q.CARs.Select(c => c.Action))
                        .Include(q => q.CARs.Select(c => c.Problem))
                        .Include(q => q.OFIs.Select(o => o.OFIHeader))
                        .ToList()
                        .Where(q => validJudgements.Contains(q.judgement.ToLower()))
                        .ToList();

                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Laporan Temuan");

                        for (int i = 1; i <= 68; i++) ws.Column(i).Width = 1.8;

                        var rangeLogo = ws.Range(1, 1, 4, 9).Merge();
                        rangeLogo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rangeLogo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rangeLogo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        try
                        {
                            string imagePath = Server.MapPath("~/Images/niterra-logo.jpg");
                            if (System.IO.File.Exists(imagePath))
                                ws.AddPicture(imagePath).MoveTo(ws.Cell(1, 1), 10, 10).WithSize(120, 60);
                            else
                                rangeLogo.Value = "LOGO";
                        }
                        catch { }

                        var rangeJudul = ws.Range(1, 10, 2, 56).Merge();
                        rangeJudul.Value = "FORMULIR INTEGRASI";
                        rangeJudul.Style.Font.Bold = true;
                        rangeJudul.Style.Font.FontSize = 14;
                        rangeJudul.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rangeJudul.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rangeJudul.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        ws.Range(1, 57, 1, 60).Merge().Value = "No. Dok";
                        ws.Range(1, 61, 1, 68).Merge().Value = "PMLK3-MR-09/L4";

                        ws.Range(2, 57, 2, 60).Merge().Value = "Revisi";
                        ws.Range(2, 61, 2, 68).Merge().Value = "01";

                        var rangeSubJudul = ws.Range(3, 10, 4, 56).Merge();
                        rangeSubJudul.Value = "LAPORAN TEMUAN INTERNAL AUDIT";
                        rangeSubJudul.Style.Font.Bold = true;
                        rangeSubJudul.Style.Font.FontSize = 14;
                        rangeSubJudul.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rangeSubJudul.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rangeSubJudul.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        ws.Range(3, 57, 3, 60).Merge().Value = "Tanggal";
                        ws.Range(3, 61, 3, 68).Merge().Value = "4-Aug-2025";

                        ws.Range(4, 57, 4, 60).Merge().Value = "Halaman";
                        ws.Range(4, 61, 4, 68).Merge().Value = "1 / 1";

                        ws.Range(1, 57, 4, 68).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(1, 57, 4, 68).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(1, 57, 4, 60).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                        ws.Range(5, 1, 5, 6).Merge().Value = "Tanggal Audit";
                        ws.Range(5, 7, 5, 20).Merge().Value = tanggalAudit;
                        ws.Range(5, 21, 5, 30).Merge().Value = "Periode Internal audit";
                        ws.Range(5, 31, 5, 44).Merge().Value = periode;

                        ws.Range(5, 45, 5, 56).Merge().Value = "Auditor";
                        ws.Range(5, 45, 5, 56).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(5, 45, 5, 56).Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");

                        ws.Range(5, 57, 5, 68).Merge().Value = "Auditee";
                        ws.Range(5, 57, 5, 68).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(5, 57, 5, 68).Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");

                        ws.Range(6, 1, 6, 6).Merge().Value = "Nama Proses";
                        ws.Range(6, 7, 6, 44).Merge().Value = proses;
                        ws.Range(6, 7, 6, 44).Style.Font.Bold = true;

                        CreateSignatureBox(ws, 6, 45, approvals, accessUsers, "Auditor 1");
                        CreateSignatureBox(ws, 6, 49, approvals, accessUsers, "Auditor 2");
                        CreateSignatureBox(ws, 6, 53, approvals, accessUsers, "Auditor 3");
                        CreateSignatureBox(ws, 6, 57, approvals, accessUsers, "Auditee 1");
                        CreateSignatureBox(ws, 6, 61, approvals, accessUsers, "Auditee 2");
                        CreateSignatureBox(ws, 6, 65, approvals, accessUsers, "Auditee 3");

                        ws.Range(7, 1, 7, 6).Merge().Value = "Kriteria Audit";

                        var rangeKriteria = ws.Range(7, 7, 7, 44).Merge();

                        var cellKriteria = rangeKriteria.FirstCell();

                        string kriteriaFull = "ISO 9001 : 2015 / IATF 16949 : 2016 / ISO 14001 : 2015 / ISO 45001 : 2018";

                        var activeStandards = kriteria.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                                                      .Select(s => s.Trim().ToLower())
                                                      .ToList();

                        var parts = kriteriaFull.Split('/').Select(s => s.Trim()).ToList();

                        cellKriteria.Value = "";
                        cellKriteria.RichText.ClearText();

                        for (int i = 0; i < parts.Count; i++)
                        {
                            string part = parts[i];
                            bool isMatch = activeStandards.Any(std => part.ToLower().Contains(std));

                            if (isMatch)
                            {
                                cellKriteria.RichText.AddText(part).SetBold(true).SetFontColor(XLColor.Black);
                            }
                            else
                            {
                                cellKriteria.RichText.AddText(part).SetStrikethrough(true).SetFontColor(XLColor.Gray);
                            }

                            if (i < parts.Count - 1)
                            {
                                cellKriteria.RichText.AddText(" / ").SetFontColor(XLColor.Black).SetBold(false).SetStrikethrough(false);
                            }
                        }

                        cellKriteria.RichText.AddText(" *)").SetItalic(true).SetFontColor(XLColor.Black);

                        ws.Range(8, 1, 8, 44).Merge().Value = " ";

                        ws.Range(5, 1, 8, 44).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(5, 1, 8, 44).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(5, 45, 8, 68).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        Action<IXLRange> SetHeaderStyle = (rng) => {
                            rng.Style.Font.Bold = true;
                            rng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            rng.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            rng.Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");
                            rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        };

                        var thNo = ws.Range(9, 1, 10, 2).Merge(); thNo.Value = "No"; SetHeaderStyle(thNo);
                        var thKlausul = ws.Range(9, 3, 10, 7).Merge(); thKlausul.Value = "No. Klausul"; SetHeaderStyle(thKlausul);
                        var thSyarat = ws.Range(9, 8, 10, 19).Merge(); thSyarat.Value = "Isi Persyaratan Klausul"; SetHeaderStyle(thSyarat);

                        var thTemuan = ws.Range(9, 20, 9, 46).Merge(); thTemuan.Value = "Temuan Audit"; SetHeaderStyle(thTemuan);
                        var thDetail = ws.Range(10, 20, 10, 31).Merge(); thDetail.Value = "Details of Nonconformity"; SetHeaderStyle(thDetail);
                        var thEvid = ws.Range(10, 32, 10, 46).Merge(); thEvid.Value = "Objective Evidence Observed"; SetHeaderStyle(thEvid);

                        var thKlasifikasi = ws.Range(9, 47, 9, 55).Merge(); thKlasifikasi.Value = "Klasifikasi temuan"; SetHeaderStyle(thKlasifikasi);
                        var thMaj = ws.Range(10, 47, 10, 49).Merge(); thMaj.Value = "Major"; SetHeaderStyle(thMaj);
                        var thMin = ws.Range(10, 50, 10, 52).Merge(); thMin.Value = "Minor"; SetHeaderStyle(thMin);
                        var thOfi = ws.Range(10, 53, 10, 55).Merge(); thOfi.Value = "Ofi"; SetHeaderStyle(thOfi);

                        var thTgl = ws.Range(9, 56, 10, 59).Merge(); thTgl.Value = "Tanggal Selesai"; SetHeaderStyle(thTgl);
                        var thStatus = ws.Range(9, 60, 10, 68).Merge(); thStatus.Value = "Status tindak lanjut\nCAR No. / open / closed";
                        thStatus.Style.Alignment.WrapText = true;
                        SetHeaderStyle(thStatus);

                        // =========================================================
                        // LOOPING DATA TABEL
                        // =========================================================
                        int currentRow = 11;

                        if (questions.Count == 0)
                        {
                            var rngEmpty = ws.Range(currentRow, 1, currentRow, 68).Merge();
                            rngEmpty.Value = "Tidak ada temuan audit untuk section ini.";
                            rngEmpty.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            rngEmpty.Style.Font.Italic = true;
                            rngEmpty.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            currentRow++;
                        }
                        else
                        {
                            int index = 1;
                            foreach (var q in questions)
                            {
                                var finding = q.Finding?.FirstOrDefault(f => f.deleted_at == null);
                                var car = q.CARs?.FirstOrDefault(c => c.deleted_at == null);
                                var ofi = q.OFIs?.FirstOrDefault();

                                string problemStr = finding?.problem;
                                string locationStr = finding?.location;
                                string objectStr = finding?.object_name;

                                if (string.IsNullOrEmpty(problemStr))
                                {
                                    if (car != null && car.Problem != null && car.Problem.Any())
                                    {
                                        var carProb = car.Problem.FirstOrDefault();
                                        problemStr = carProb.problem_name;
                                        locationStr = carProb.problem_location;
                                        objectStr = carProb.problem_part_name;
                                    }
                                    else if (ofi != null) problemStr = null;
                                }

                                string klasifikasi = (q.judgement ?? "").ToUpper();
                                string isMajor = klasifikasi == "MAJOR" ? "V" : "";
                                string isMinor = klasifikasi == "MINOR" ? "V" : "";
                                string isOFI = klasifikasi == "OFI" ? "V" : "";

                                string detailStr = "";
                                if (!string.IsNullOrEmpty(problemStr)) detailStr += $"Problem :\n{problemStr}\n\n";
                                if (!string.IsNullOrEmpty(locationStr)) detailStr += $"Location :\n{locationStr}\n\n";
                                if (isOFI != "V" || !string.IsNullOrEmpty(q.note))
                                {
                                    if (isOFI != "V") detailStr += $"Detail:\n{q.note}";
                                    else detailStr += q.note;
                                }
                                if (string.IsNullOrEmpty(detailStr.Trim())) detailStr = "-";

                                string finalObjectStr = !string.IsNullOrEmpty(objectStr) ? $"Object :\n{objectStr}" : "-";

                                DateTime? actionDateObj = null;
                                if (car != null && car.Action != null && car.Action.Any()) actionDateObj = car.Action.FirstOrDefault()?.action_date;
                                else if (ofi != null) actionDateObj = ofi.action_date;
                                string actionDateFormatted = actionDateObj.HasValue ? actionDateObj.Value.ToString("dd-MMM-yyyy") : "-";

                                string statusTindakLanjut = "-";
                                if (car != null)
                                {
                                    string carNo = car.no_report;
                                    string carStatus = "OPEN";
                                    var verif1 = car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 1");
                                    var verif2 = car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 2");

                                    bool isVerif1MrFilled = verif1 != null && !string.IsNullOrEmpty(verif1.verification_mr_status);
                                    bool isVerif2MrFilled = verif2 != null && !string.IsNullOrEmpty(verif2.verification_mr_status);
                                    if (isVerif1MrFilled && isVerif2MrFilled) carStatus = "CLOSED";

                                    statusTindakLanjut = $"{carNo} / {carStatus}";
                                }
                                else if (ofi != null)
                                {
                                    string ofiStatus = ofi.OFIHeader?.status ?? "OPEN";
                                    statusTindakLanjut = $"OFI / {ofiStatus}";
                                }
                                if (isOFI == "V") statusTindakLanjut = "-";

                                // Map to Excel Cells
                                var tNo = ws.Range(currentRow, 1, currentRow, 2).Merge(); tNo.Value = index;

                                // *** PERBAIKAN: paksa format teks agar tidak dikonversi ke tanggal ***
                                var tKl = ws.Range(currentRow, 3, currentRow, 7).Merge();
                                tKl.Style.NumberFormat.Format = "@";
                                tKl.Value = q.clausul ?? "-";

                                var tIs = ws.Range(currentRow, 8, currentRow, 19).Merge(); tIs.Value = "-";

                                var tDet = ws.Range(currentRow, 20, currentRow, 31).Merge();
                                tDet.Value = detailStr.Trim();
                                tDet.Style.Alignment.WrapText = true;
                                tDet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                                var tObj = ws.Range(currentRow, 32, currentRow, 46).Merge();
                                tObj.Value = finalObjectStr;
                                tObj.Style.Alignment.WrapText = true;
                                tObj.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                                var tMaj = ws.Range(currentRow, 47, currentRow, 49).Merge(); tMaj.Value = isMajor;
                                var tMin = ws.Range(currentRow, 50, currentRow, 52).Merge(); tMin.Value = isMinor;
                                var tOfi = ws.Range(currentRow, 53, currentRow, 55).Merge(); tOfi.Value = isOFI;

                                var tTgl = ws.Range(currentRow, 56, currentRow, 59).Merge(); tTgl.Value = actionDateFormatted;
                                var tStat = ws.Range(currentRow, 60, currentRow, 68).Merge(); tStat.Value = statusTindakLanjut;

                                var dataRowRange = ws.Range(currentRow, 1, currentRow, 68);
                                dataRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                                dataRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                dataRowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                                tNo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                tKl.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                tMaj.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                tMin.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                tOfi.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                tTgl.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                tStat.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                ws.Row(currentRow).AdjustToContents();
                                ws.Row(currentRow).Height = ws.Row(currentRow).Height + 10;

                                index++;
                                currentRow++;
                            }
                        }

                        ws.Range(currentRow, 1, currentRow, 10).Merge().Value = "*) Coret yang tidak diperlukan";
                        ws.Range(currentRow, 1, currentRow, 10).Style.Font.Italic = true;
                        ws.Range(currentRow, 1, currentRow, 10).Style.Font.FontSize = 9;

                        // =========================================================
                        // GENERATE & DOWNLOAD
                        // =========================================================
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            string safeProsesName = string.Join("_", proses.Split(Path.GetInvalidFileNameChars()));
                            string fileName = $"LapTemuan_InternalAudit_{safeProsesName}.xlsx";

                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"Gagal mengekspor data: {ex.Message} {(ex.InnerException != null ? ex.InnerException.Message : "")}");
            }
        }

        private void CreateSignatureBox(IXLWorksheet ws, int startRow, int startCol, List<Quality_EAudit_Recap_Approval> approvals, List<Quality_EAudit_Access> accessUsers, string roleName)
        {
            // Menggabungkan 3 Baris dan 4 Kolom untuk kotak TTD
            var box = ws.Range(startRow, startCol, startRow + 2, startCol + 3).Merge();
            box.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Teks nama diatur agar berada di bagian paling bawah tengah
            box.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            box.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            box.Style.Font.FontSize = 8;
            box.Style.Font.Bold = true;

            box.Value = "-";

            if (approvals != null)
            {
                var approval = approvals.FirstOrDefault(x => x.recap_sign_role == roleName);

                if (approval != null && !string.IsNullOrEmpty(approval.recap_sign_status))
                {
                    string status = approval.recap_sign_status.ToUpper();
                    string dateStr = approval.recap_sign_date != null ?
                        Convert.ToDateTime(approval.recap_sign_date).ToString("dd MMM yyyy").ToUpper() : "";

                    // Pencarian Nama Berdasarkan NIK dari tabel Akses
                    string nik = approval.recap_sign_by;
                    var userAccess = accessUsers?.FirstOrDefault(u => u.NIK == nik);

                    // Jika nama ditemukan gunakan namanya, jika tidak gunakan NIK-nya sebagai fallback
                    string byName = userAccess != null && !string.IsNullOrEmpty(userAccess.name) ? userAccess.name : nik ?? "";

                    // Opsi: Potong menjadi maksimal 2 kata pertama agar tidak tumpang tindih di kotak Excel yang sempit
                    if (!string.IsNullOrEmpty(byName))
                    {
                        byName = string.Join(" ", byName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));
                    }

                    // Tulis nama penandatangan di bagian bawah kotak
                    box.Value = byName;

                    // --- PROSES MENGGAMBAR STEMPEL ---
                    System.Drawing.Color stampColor = status == "APPROVED"
                        ? System.Drawing.Color.FromArgb(5, 150, 105)  // Hijau Emerald
                        : System.Drawing.Color.FromArgb(220, 38, 38); // Merah Crimson

                    // Buat kanvas gambar kosong berukuran 200x200 pixel
                    using (Bitmap bmp = new Bitmap(200, 200))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.TextRenderingHint = TextRenderingHint.AntiAlias;
                            g.Clear(System.Drawing.Color.Transparent);

                            // Buat stempel miring / rotasi -12 derajat
                            g.TranslateTransform(100, 100);
                            g.RotateTransform(-12);
                            g.TranslateTransform(-100, -100);

                            // 1. Gambar Garis Lingkaran Luar
                            using (Pen penOuter = new Pen(stampColor, 6))
                            {
                                g.DrawEllipse(penOuter, 10, 10, 180, 180);
                            }

                            // 2. Gambar Garis Lingkaran Dalam
                            using (Pen penInner = new Pen(stampColor, 2))
                            {
                                g.DrawEllipse(penInner, 18, 18, 164, 164);
                            }

                            // 3. Tulis Teks Status (APPROVED / REJECTED)
                            using (Font fontStatus = new Font("Arial", 26, FontStyle.Bold))
                            {
                                SizeF textSize = g.MeasureString(status, fontStatus);
                                g.DrawString(status, fontStatus, new SolidBrush(stampColor), (200 - textSize.Width) / 2, 65);
                            }

                            // 4. Gambar Garis Pemisah (Border-top tanggal)
                            using (Pen penLine = new Pen(stampColor, 2))
                            {
                                g.DrawLine(penLine, 40, 105, 160, 105);
                            }

                            // 5. Tulis Teks Tanggal di dalam cap
                            using (Font fontDate = new Font("Arial", 16, FontStyle.Bold))
                            {
                                SizeF dateSize = g.MeasureString(dateStr, fontDate);
                                g.DrawString(dateStr, fontDate, new SolidBrush(stampColor), (200 - dateSize.Width) / 2, 115);
                            }
                        }

                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;

                            var cell = ws.Cell(startRow, startCol);
                            string shortName = "Stamp_" + Guid.NewGuid().ToString("N").Substring(0, 10);

                            ws.AddPicture(ms, shortName)
                              .MoveTo(cell, 7, 2)
                              .Scale(0.24);
                        }
                    }
                }
            }
        }

        [HttpGet]
        public ActionResult ExportExcelRekapLaporan(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return HttpNotFound("ID Event tidak valid.");

            try
            {
                using (var dbq = new QualityConnection()) // Sesuaikan dengan DbContext Anda
                {
                    // 1. AMBIL DATA EVENT & STAKEHOLDER TERAWAL
                    var auditEvent = dbq.Quality_EAudit_Audit_Event
                        .FirstOrDefault(e => e.audit_event_id == eventId && e.deleted_at == null);

                    if (auditEvent == null)
                        return HttpNotFound("Data Audit Event tidak ditemukan.");

                    var earliestStakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                        .Where(s => s.audit_event_id == eventId && s.deleted_at == null && s.start_event != null)
                        .OrderBy(s => s.start_event)
                        .FirstOrDefault();

                    string kriteria = auditEvent.audit_standard ?? "-";
                    string tanggalAudit = earliestStakeholder?.start_event?.ToString("dd-MMM-yyyy") ?? "-";
                    string periodeAudit = earliestStakeholder?.start_event?.ToString("MMM-yyyy") ?? "-";

                    // 2. AMBIL DATA APPROVAL RECAP EVENT
                    var approvals = dbq.Quality_EAudit_Approval_Recap_Event
                        .Where(a => a.audit_event_id == eventId)
                        .ToList();

                    var signerNiks = approvals.Where(a => !string.IsNullOrEmpty(a.recap_sign_by))
                                              .Select(a => a.recap_sign_by)
                                              .Distinct()
                                              .ToList();

                    var accessUsers = dbq.Quality_EAudit_Access
                        .Where(u => signerNiks.Contains(u.NIK))
                        .ToList();

                    // Tanggal Dokumen (Berdasarkan Dibuat -> Approved)
                    DateTime docDate = DateTime.Now;
                    var dibuatApp = approvals.FirstOrDefault(a => a.recap_sign_category == "Dibuat" && a.recap_sign_status == "Approved");
                    if (dibuatApp != null && dibuatApp.recap_sign_date.HasValue)
                    {
                        docDate = dibuatApp.recap_sign_date.Value;
                    }
                    string docDateFormatted = $"{docDate.Day} {new[] { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des" }[docDate.Month - 1]} {docDate.Year}";

                    // 3. AMBIL DATA TEMUAN
                    var validJudgements = new string[] { "major", "minor", "ofi" };
                    var questions = dbq.Quality_EAudit_Question_Event
                        .Where(q => q.AuditEventStakeholder.audit_event_id == eventId && q.deleted_at == null && q.judgement != null)
                        .Include(q => q.AuditEventStakeholder.Auditors)
                        .Include(q => q.Finding)
                        .Include(q => q.CARs.Select(c => c.Verification))
                        .Include(q => q.CARs.Select(c => c.Action))
                        .Include(q => q.CARs.Select(c => c.Problem))
                        .Include(q => q.OFIs.Select(o => o.OFIHeader))
                        .ToList()
                        .Where(q => validJudgements.Contains(q.judgement.ToLower()) && q.AuditEventStakeholder.deleted_at == null)
                        .ToList();

                    // 4. MENGHITUNG TOTAL
                    int totalMajor = questions.Count(q => q.judgement.ToLower() == "major");
                    int totalMinor = questions.Count(q => q.judgement.ToLower() == "minor");
                    int totalOfi = questions.Count(q => q.judgement.ToLower() == "ofi");

                    // 5. GENERATE EXCEL MENGGUNAKAN CLOSEDXML
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Rekap Laporan Temuan");

                        // =========================================================
                        // GRID SYSTEM (69 Kolom seperti Tabel HTML)
                        // =========================================================
                        for (int i = 1; i <= 69; i++) ws.Column(i).Width = 1.6;

                        // =========================================================
                        // HEADER DOKUMEN (BARIS 1 - 4)
                        // =========================================================
                        var rangeLogo = ws.Range(1, 1, 4, 7).Merge();
                        rangeLogo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rangeLogo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rangeLogo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        try
                        {
                            string imagePath = Server.MapPath("~/Images/niterra-logo.jpg");
                            if (System.IO.File.Exists(imagePath))
                                ws.AddPicture(imagePath).MoveTo(ws.Cell(1, 1), 10, 5).WithSize(100, 50);
                            else
                                rangeLogo.Value = "LOGO";
                        }
                        catch { }

                        var rangeJudul = ws.Range(1, 8, 2, 59).Merge();
                        rangeJudul.Value = "FORMULIR INTEGRASI";
                        rangeJudul.Style.Font.Bold = true;
                        rangeJudul.Style.Font.FontSize = 14;
                        rangeJudul.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rangeJudul.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rangeJudul.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        ws.Range(1, 60, 1, 63).Merge().Value = "No. Dok";
                        ws.Range(1, 64, 1, 69).Merge().Value = "PMLK3-MR-09/L6";

                        ws.Range(2, 60, 2, 63).Merge().Value = "Revisi";
                        ws.Range(2, 64, 2, 69).Merge().Value = "01";

                        var rangeSubJudul = ws.Range(3, 8, 4, 59).Merge();
                        rangeSubJudul.Value = "REKAP LAPORAN TEMUAN INTERNAL AUDIT";
                        rangeSubJudul.Style.Font.Bold = true;
                        rangeSubJudul.Style.Font.FontSize = 14;
                        rangeSubJudul.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rangeSubJudul.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rangeSubJudul.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        ws.Range(3, 60, 3, 63).Merge().Value = "Tanggal";
                        ws.Range(3, 64, 3, 69).Merge().Value = "4-Aug-2025";

                        ws.Range(4, 60, 4, 63).Merge().Value = "Halaman";
                        ws.Range(4, 64, 4, 69).Merge().Value = "1 / 1";

                        ws.Range(1, 60, 4, 69).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(1, 60, 4, 69).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(1, 60, 4, 63).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                        // =========================================================
                        // META DATA KOP SURAT BAWAH (BARIS 5 - 6)
                        // =========================================================
                        ws.Range(5, 1, 5, 4).Merge().Value = "Audit";
                        var cellKriteria = ws.Range(5, 5, 5, 69).Merge().FirstCell();

                        // Dynamic RichText untuk Kriteria
                        string kriteriaFull = "ISO 9001 : 2015 / IATF 16949 : 2016 / ISO 14001 : 2015 / ISO 45001 : 2018";
                        var activeStandards = kriteria.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToLower()).ToList();
                        var parts = kriteriaFull.Split('/').Select(s => s.Trim()).ToList();

                        cellKriteria.Value = "";
                        cellKriteria.RichText.ClearText();
                        cellKriteria.RichText.AddText(": ").SetBold(false).SetFontColor(XLColor.Black);

                        for (int i = 0; i < parts.Count; i++)
                        {
                            string part = parts[i];
                            if (activeStandards.Any(std => part.ToLower().Contains(std)))
                                cellKriteria.RichText.AddText(part).SetBold(true).SetFontColor(XLColor.Black);
                            else
                                cellKriteria.RichText.AddText(part).SetStrikethrough(true).SetFontColor(XLColor.Gray);

                            if (i < parts.Count - 1) cellKriteria.RichText.AddText(" / ").SetFontColor(XLColor.Black).SetBold(false).SetStrikethrough(false);
                        }

                        ws.Range(6, 1, 6, 4).Merge().Value = "Bulan/Tahun";
                        ws.Range(6, 5, 6, 69).Merge().Value = $": {periodeAudit}";

                        ws.Range(5, 1, 6, 69).Style.Font.Bold = true;

                        // =========================================================
                        // HEADER TABEL REKAP TEMUAN (BARIS 7 - 9)
                        // =========================================================
                        Action<IXLRange> SetHead = (rng) => {
                            rng.Style.Font.Bold = true; rng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            rng.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; rng.Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");
                            rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        };

                        // Row 7
                        var thNo = ws.Range(7, 1, 9, 1).Merge(); thNo.Value = "No"; SetHead(thNo);
                        var thNP = ws.Range(7, 2, 9, 4).Merge(); thNP.Value = "Nama Proses"; SetHead(thNP);
                        var thTA = ws.Range(7, 5, 9, 7).Merge(); thTA.Value = "Tgl Audit"; SetHead(thTA);
                        var thSh = ws.Range(7, 8, 9, 9).Merge(); thSh.Value = "Shift"; SetHead(thSh);
                        var thAu = ws.Range(7, 10, 9, 13).Merge(); thAu.Value = "Auditor"; SetHead(thAu);
                        var thCl = ws.Range(7, 14, 9, 19).Merge(); thCl.Value = "Clausul"; SetHead(thCl);
                        var thJA = ws.Range(7, 20, 9, 22).Merge(); thJA.Value = "Jenis Audit"; SetHead(thJA);
                        var thTe = ws.Range(7, 23, 7, 52).Merge(); thTe.Value = "Temuan Audit"; SetHead(thTe);
                        var thKT = ws.Range(7, 53, 7, 58).Merge(); thKT.Value = "Klasifikasi temuan"; SetHead(thKT);
                        var thCAR = ws.Range(7, 59, 7, 69).Merge(); thCAR.Value = "CAR"; SetHead(thCAR);

                        // Row 8
                        var thDet = ws.Range(8, 23, 9, 37).Merge(); thDet.Value = "Details of Nonconformity"; SetHead(thDet);
                        var thObj = ws.Range(8, 38, 9, 52).Merge(); thObj.Value = "Objective Evidence Observed"; SetHead(thObj);
                        var thMaj = ws.Range(8, 53, 9, 54).Merge(); thMaj.Value = "Major"; SetHead(thMaj);
                        var thMin = ws.Range(8, 55, 9, 56).Merge(); thMin.Value = "Minor"; SetHead(thMin);
                        var thOfi = ws.Range(8, 57, 9, 58).Merge(); thOfi.Value = "Ofi"; SetHead(thOfi);
                        var thNum = ws.Range(8, 59, 9, 63).Merge(); thNum.Value = "No."; SetHead(thNum);
                        var thVer = ws.Range(8, 64, 8, 69).Merge(); thVer.Value = "Status Verifikasi"; SetHead(thVer);

                        // Row 9
                        var thV1 = ws.Range(9, 64, 9, 66).Merge(); thV1.Value = "Ke-1"; SetHead(thV1);
                        var thV2 = ws.Range(9, 67, 9, 69).Merge(); thV2.Value = "Ke-2"; SetHead(thV2);

                        // =========================================================
                        // LOOPING DATA (BARIS 10 KE BAWAH)
                        // =========================================================
                        int currentRow = 10;

                        if (questions.Count == 0)
                        {
                            var rngEmpty = ws.Range(currentRow, 1, currentRow, 69).Merge();
                            rngEmpty.Value = "Tidak ada temuan audit.";
                            rngEmpty.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            rngEmpty.Style.Font.Italic = true;
                            rngEmpty.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            currentRow++;
                        }
                        else
                        {
                            int index = 1;
                            foreach (var q in questions)
                            {
                                var finding = q.Finding?.FirstOrDefault(f => f.deleted_at == null);
                                var car = q.CARs?.FirstOrDefault(c => c.deleted_at == null);
                                var ofi = q.OFIs?.FirstOrDefault();

                                string problemStr = finding?.problem;
                                string locationStr = finding?.location;
                                string objectStr = finding?.object_name;

                                if (string.IsNullOrEmpty(problemStr))
                                {
                                    if (car != null && car.Problem != null && car.Problem.Any())
                                    {
                                        var carProb = car.Problem.FirstOrDefault();
                                        problemStr = carProb.problem_name;
                                        locationStr = carProb.problem_location;
                                        objectStr = carProb.problem_part_name;
                                    }
                                    else if (ofi != null) problemStr = null;
                                }

                                string klasifikasi = (q.judgement ?? "").ToUpper();
                                string isMajor = klasifikasi == "MAJOR" ? "V" : "";
                                string isMinor = klasifikasi == "MINOR" ? "V" : "";
                                string isOFI = klasifikasi == "OFI" ? "V" : "";

                                string detailStr = "";
                                if (!string.IsNullOrEmpty(problemStr)) detailStr += $"Problem :\n{problemStr}\n\n";
                                if (!string.IsNullOrEmpty(locationStr)) detailStr += $"Lokasi : {locationStr}\n\n";
                                if (isOFI != "V" || !string.IsNullOrEmpty(q.note))
                                {
                                    if (isOFI != "V") detailStr += $"Detail:\n{q.note}";
                                    else detailStr += q.note;
                                }
                                if (string.IsNullOrEmpty(detailStr.Trim())) detailStr = "-";
                                string finalObjectStr = !string.IsNullOrEmpty(objectStr) ? objectStr : "-";

                                string verif1Status = "-", verif2Status = "-", noCAR = "-";
                                if (car != null)
                                {
                                    noCAR = car.no_report;
                                    verif1Status = (car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 1")?.verification_mr_status == "Approved") ? "CLOSE" : "OPEN";
                                    verif2Status = (car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 2")?.verification_mr_status == "Approved") ? "CLOSE" : "OPEN";
                                }
                                else if (ofi != null)
                                {
                                    noCAR = "-";
                                    verif1Status = (ofi.OFIHeader?.ApprovalOFI?.FirstOrDefault(a => a.sign_type == "DiSetujui")?.status == "Approved") ? "CLOSE" : "OPEN";
                                }

                                string secName = q.AuditEventStakeholder?.responsibility_section ?? "-";
                                string strDate = q.AuditEventStakeholder?.start_event.HasValue == true ? q.AuditEventStakeholder.start_event.Value.ToString("dd-MMM-yyyy") : "-";
                                string catAudit = !string.IsNullOrEmpty(q.category_audit) ? q.category_audit : "-";
                                var audList = q.AuditEventStakeholder?.Auditors?.Where(a => a.deleted_at == null && a.role != null && a.role.ToLower().Contains("auditor") && a.category_audit == q.category_audit).Select(a => a.person_name).ToList() ?? new List<string>();
                                string audNames = audList.Any() ? string.Join(", ", audList) : "-";

                                // Assign Value to Cells
                                ws.Range(currentRow, 1, currentRow, 1).Merge().Value = index;
                                ws.Range(currentRow, 2, currentRow, 4).Merge().Value = secName;
                                ws.Range(currentRow, 5, currentRow, 7).Merge().Value = strDate;
                                ws.Range(currentRow, 8, currentRow, 9).Merge().Value = "-";
                                ws.Range(currentRow, 10, currentRow, 13).Merge().Value = audNames;

                                // *** PERBAIKAN: Format kolom Clausul sebagai teks ***
                                var tCl = ws.Range(currentRow, 14, currentRow, 19).Merge();
                                tCl.Style.NumberFormat.Format = "@";
                                tCl.Value = q.clausul ?? "-";

                                ws.Range(currentRow, 20, currentRow, 22).Merge().Value = catAudit;

                                var tDet = ws.Range(currentRow, 23, currentRow, 37).Merge();
                                tDet.Value = detailStr.Trim(); tDet.Style.Alignment.WrapText = true; tDet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                                var tObj = ws.Range(currentRow, 38, currentRow, 52).Merge();
                                tObj.Value = finalObjectStr; tObj.Style.Alignment.WrapText = true; tObj.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                                ws.Range(currentRow, 53, currentRow, 54).Merge().Value = isMajor;
                                ws.Range(currentRow, 55, currentRow, 56).Merge().Value = isMinor;
                                ws.Range(currentRow, 57, currentRow, 58).Merge().Value = isOFI;
                                ws.Range(currentRow, 59, currentRow, 63).Merge().Value = noCAR;

                                if (isOFI == "V")
                                {
                                    ws.Range(currentRow, 64, currentRow, 69).Merge().Value = verif1Status;
                                }
                                else
                                {
                                    ws.Range(currentRow, 64, currentRow, 66).Merge().Value = verif1Status;
                                    ws.Range(currentRow, 67, currentRow, 69).Merge().Value = verif2Status;
                                }

                                // Apply Borders & Center alignment basic
                                var rowRange = ws.Range(currentRow, 1, currentRow, 69);
                                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                rowRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                tDet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                tObj.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                                // Height calculation
                                int maxLines = Math.Max(detailStr.Split('\n').Length + (detailStr.Length / 40), finalObjectStr.Split('\n').Length + (finalObjectStr.Length / 40));
                                ws.Row(currentRow).Height = Math.Max(30, (maxLines * 15) + 5);

                                index++;
                                currentRow++;
                            }
                        }

                        // =========================================================
                        // FOOTER (REMARK & SIGNATURES)
                        // =========================================================
                        currentRow += 2; // Spasi

                        // Box Remark (Sebelah Kiri)
                        ws.Range(currentRow, 2, currentRow, 8).Merge().Value = "Remark :";
                        ws.Range(currentRow, 2, currentRow, 8).Style.Font.Bold = true;
                        ws.Range(currentRow, 2, currentRow, 8).Style.Font.Underline = XLFontUnderlineValues.Single;

                        ws.Range(currentRow + 1, 2, currentRow + 1, 8).Merge().Value = "Klasifikasi";
                        ws.Range(currentRow + 1, 9, currentRow + 1, 15).Merge().Value = "Total";
                        ws.Range(currentRow + 1, 2, currentRow + 1, 15).Style.Font.Bold = true;
                        ws.Range(currentRow + 1, 2, currentRow + 1, 15).Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");

                        ws.Range(currentRow + 2, 2, currentRow + 2, 8).Merge().Value = "Major";
                        ws.Range(currentRow + 2, 9, currentRow + 2, 15).Merge().Value = totalMajor;

                        ws.Range(currentRow + 3, 2, currentRow + 3, 8).Merge().Value = "Minor";
                        ws.Range(currentRow + 3, 9, currentRow + 3, 15).Merge().Value = totalMinor;

                        ws.Range(currentRow + 4, 2, currentRow + 4, 8).Merge().Value = "Ofi";
                        ws.Range(currentRow + 4, 9, currentRow + 4, 15).Merge().Value = totalOfi;

                        ws.Range(currentRow + 1, 2, currentRow + 4, 15).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(currentRow + 1, 2, currentRow + 4, 15).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Range(currentRow + 1, 2, currentRow + 4, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Box Signatures (Sebelah Kanan)
                        ws.Range(currentRow, 40, currentRow, 69).Merge().Value = $"Jakarta, {docDateFormatted}";
                        ws.Range(currentRow, 40, currentRow, 69).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        ws.Range(currentRow, 40, currentRow, 69).Style.Font.Bold = true;

                        ws.Range(currentRow + 1, 40, currentRow + 1, 49).Merge().Value = "Disetujui";
                        ws.Range(currentRow + 1, 50, currentRow + 1, 59).Merge().Value = "Diperiksa";
                        ws.Range(currentRow + 1, 60, currentRow + 1, 69).Merge().Value = "Dibuat";
                        ws.Range(currentRow + 1, 40, currentRow + 1, 69).Style.Font.Bold = true;
                        ws.Range(currentRow + 1, 40, currentRow + 1, 69).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        CreateSignatureBoxEvent(ws, currentRow + 2, 40, 49, approvals, accessUsers, "Disetujui");
                        CreateSignatureBoxEvent(ws, currentRow + 2, 50, 59, approvals, accessUsers, "Diperiksa");
                        CreateSignatureBoxEvent(ws, currentRow + 2, 60, 69, approvals, accessUsers, "Dibuat");

                        // =========================================================
                        // GENERATE & DOWNLOAD
                        // =========================================================
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            string fileName = $"Rekap_Temuan_Audit_{eventId}.xlsx";
                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"Gagal mengekspor data: {ex.Message}");
            }
        }




        // =========================================================================
        // Helper Kotak Tanda Tangan Menggunakan StartCol & EndCol
        // =========================================================================
        private void CreateSignatureBoxEvent(IXLWorksheet ws, int startRow, int startCol, int endCol, List<Quality_EAudit_Approval_Recap_Event> approvals, List<Quality_EAudit_Access> accessUsers, string roleCategory)
        {
            var box = ws.Range(startRow, startCol, startRow + 3, endCol).Merge(); // Rowspan 4 
            box.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(startRow - 1, startCol, startRow - 1, endCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            box.Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
            box.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            box.Style.Font.FontSize = 9;
            box.Style.Font.Bold = true;
            ws.Row(startRow).Height = 25;

            box.Value = "-";

            if (approvals != null)
            {
                var approval = approvals.FirstOrDefault(x => x.recap_sign_category == roleCategory);

                if (approval != null && !string.IsNullOrEmpty(approval.recap_sign_status))
                {
                    string status = approval.recap_sign_status.ToUpper();
                    string dateStr = approval.recap_sign_date != null ? Convert.ToDateTime(approval.recap_sign_date).ToString("dd MMM yyyy").ToUpper() : "";

                    string nik = approval.recap_sign_by;
                    var userAccess = accessUsers?.FirstOrDefault(u => u.NIK == nik);
                    string byName = userAccess != null && !string.IsNullOrEmpty(userAccess.name) ? userAccess.name : nik ?? "";

                    if (!string.IsNullOrEmpty(byName))
                        byName = string.Join(" ", byName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

                    box.Value = byName;

                    System.Drawing.Color stampColor = status == "APPROVED" ? System.Drawing.Color.FromArgb(5, 150, 105) : System.Drawing.Color.FromArgb(220, 38, 38);

                    using (Bitmap bmp = new Bitmap(200, 200))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.TextRenderingHint = TextRenderingHint.AntiAlias;
                            g.Clear(System.Drawing.Color.Transparent);

                            g.TranslateTransform(100, 100); g.RotateTransform(-12); g.TranslateTransform(-100, -100);

                            using (Pen penOuter = new Pen(stampColor, 6)) { g.DrawEllipse(penOuter, 10, 10, 180, 180); }
                            using (Pen penInner = new Pen(stampColor, 2)) { g.DrawEllipse(penInner, 18, 18, 164, 164); }

                            using (Font fontStatus = new Font("Arial", 26, FontStyle.Bold))
                            {
                                SizeF textSize = g.MeasureString(status, fontStatus);
                                g.DrawString(status, fontStatus, new SolidBrush(stampColor), (200 - textSize.Width) / 2, 65);
                            }

                            using (Pen penLine = new Pen(stampColor, 2)) { g.DrawLine(penLine, 40, 105, 160, 105); }

                            using (Font fontDate = new Font("Arial", 16, FontStyle.Bold))
                            {
                                SizeF dateSize = g.MeasureString(dateStr, fontDate);
                                g.DrawString(dateStr, fontDate, new SolidBrush(stampColor), (200 - dateSize.Width) / 2, 115);
                            }
                        }

                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;

                            // --- LOGIKA POSISI TENGAH ---
                            var cell = ws.Cell(startRow, startCol);
                            string shortName = "Stamp_" + Guid.NewGuid().ToString("N").Substring(0, 10);

                            // 1. Hitung total lebar seluruh kolom di dalam rentang merged (dalam ukuran default Excel)
                            double totalWidth = 0;
                            for (int c = startCol; c <= endCol; c++)
                            {
                                totalWidth += ws.Column(c).Width;
                            }

                            // 2. Konversi kasar dari Excel Column Width ke pixels (1 unit lebar Excel kira-kira = 7.5 pixels)
                            double totalWidthPx = totalWidth * 7.5;

                            // 3. Lebar akhir gambar setelah di-scale 0.26 (Asal gambar 200px -> menjadi sekitar 52px)
                            double imageWidthPx = 200 * 0.26;

                            // 4. Hitung X Offset agar berada di tengah: (Total Lebar Kotak - Lebar Gambar) / 2
                            int offsetX = (int)Math.Max(0, (totalWidthPx - imageWidthPx) / 2);

                            // Posisikan gambar menggunakan titik X baru yang sudah dihitung
                            ws.AddPicture(ms, shortName).MoveTo(cell, offsetX, 3).Scale(0.26);
                        }
                    }
                }
            }
        }

        [HttpGet]
        public ActionResult ExportExcelCatatanAudit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound("ID Stakeholder tidak valid.");

            try
            {
                using (var dbq = new QualityConnection()) // Menggunakan DbContext Anda
                {
                    // 1. AMBIL DATA STAKEHOLDER & MASTER EVENT
                    var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                        .Include(s => s.Auditors)
                        .FirstOrDefault(s => s.audit_event_stakeholder_id == id && s.deleted_at == null);

                    if (stakeholder == null)
                        return HttpNotFound("Data Audit tidak ditemukan.");

                    var eventMaster = dbq.Quality_EAudit_Audit_Event
                        .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                    string kriteria = eventMaster?.audit_standard ?? "-";
                    string proses = stakeholder.responsibility_section ?? "-";
                    string tanggalAudit = stakeholder.start_event?.ToString("dd-MMM-yyyy") ?? "-";

                    // 2. QUERY MASTER DATA CAR & OFI (Sama persis dengan GetCatatanAuditData)
                    var validJudgements = new string[] { "major", "minor", "ofi" };

                    // Query CAR
                    var carRawData = dbq.Quality_EAudit_CAR_Header
                        .Where(c => c.deleted_at == null
                                    && c.QuestionEvent != null
                                    && c.QuestionEvent.audit_event_stakeholder_id == id
                                    && c.QuestionEvent.deleted_at == null)
                        .Select(c => new {
                            c.question_id,
                            QuestionCategory = c.QuestionEvent.category_audit,
                            QuestionClausul = c.QuestionEvent.clausul,
                            QuestionText = c.QuestionEvent.question,
                            QuestionNote = c.QuestionEvent.note,
                            FindingDetail = c.QuestionEvent.Finding
                                .Where(f => f.deleted_at == null)
                                .OrderByDescending(f => f.created_at)
                                .FirstOrDefault()
                        }).ToList();

                    // Query OFI
                    var ofiRawData = dbq.Quality_EAudit_OFI_Detail
                        .Where(d => d.OFIHeader != null
                                    && d.OFIHeader.audit_event_stakeholder_id == id
                                    && d.OFIHeader.deleted_at == null
                                    && d.QuestionEvent != null
                                    && d.QuestionEvent.audit_event_stakeholder_id == id
                                    && d.QuestionEvent.deleted_at == null)
                        .Select(d => new {
                            d.question_id,
                            QuestionCategory = d.QuestionEvent.category_audit,
                            QuestionClausul = d.QuestionEvent.clausul,
                            QuestionText = d.QuestionEvent.question,
                            QuestionNote = d.QuestionEvent.note
                        }).ToList();

                    // Query Guides (Historical Problem)
                    var guidesData = dbq.Quality_EAudit_Audit_Guide
                        .Where(g => g.audit_event_stakeholder_id == id && g.deleted_at == null).ToList();

                    // Query Notes (Kesimpulan)
                    var notesData = dbq.Quality_EAudit_Audit_Note
                        .Where(n => n.audit_event_stakeholder_id == id && n.deleted_at == null).ToList();

                    // Query Approvals & Join Nama Signer
                    var rawApprovals = dbq.Quality_EAudit_Recap_Approval
                        .Where(a => a.audit_event_stakeholder_id == id && a.category_recap.StartsWith("Catatan Audit"))
                        .ToList();

                    var signerNiks = rawApprovals.Where(a => !string.IsNullOrEmpty(a.recap_sign_by)).Select(a => a.recap_sign_by).Distinct().ToList();
                    var signerNameMap = db.V_Users_Active.Where(u => signerNiks.Contains(u.NIK)).ToDictionary(u => u.NIK, u => u.Name);

                    // 3. SEPARASI DATA GABUNGAN BERDASARKAN KATEGORI AUDIT
                    var allQuestionsMap = new Dictionary<string, List<dynamic>>();

                    // Loop data CAR masuk Map
                    foreach (var c in carRawData)
                    {
                        string cat = string.IsNullOrEmpty(c.QuestionCategory) || c.QuestionCategory == "-" ? "System" : c.QuestionCategory;
                        if (!allQuestionsMap.ContainsKey(cat)) allQuestionsMap[cat] = new List<dynamic>();

                        string detailStr = "";
                        if (c.FindingDetail != null)
                        {
                            if (!string.IsNullOrEmpty(c.FindingDetail.problem)) detailStr += $"Problem: {c.FindingDetail.problem}\n";
                            if (!string.IsNullOrEmpty(c.FindingDetail.location)) detailStr += $"Lokasi: {c.FindingDetail.location}\n";
                            if (!string.IsNullOrEmpty(c.FindingDetail.object_name)) detailStr += $"Objek: {c.FindingDetail.object_name}\n";
                            if (!string.IsNullOrEmpty(c.FindingDetail.reference)) detailStr += $"Ref: {c.FindingDetail.reference}\n";
                        }
                        if (!string.IsNullOrEmpty(c.QuestionNote)) detailStr += $"Detail: {c.QuestionNote}";

                        allQuestionsMap[cat].Add(new
                        {
                            Clausul = c.QuestionClausul ?? "-",
                            Pertanyaan = c.QuestionText ?? "-",
                            Catatan = string.IsNullOrEmpty(detailStr) ? "-" : detailStr.Trim()
                        });
                    }

                    // Loop data OFI masuk Map
                    foreach (var o in ofiRawData)
                    {
                        string cat = string.IsNullOrEmpty(o.QuestionCategory) || o.QuestionCategory == "-" ? "System" : o.QuestionCategory;
                        if (!allQuestionsMap.ContainsKey(cat)) allQuestionsMap[cat] = new List<dynamic>();

                        allQuestionsMap[cat].Add(new
                        {
                            Clausul = o.QuestionClausul ?? "-",
                            Pertanyaan = o.QuestionText ?? "-",
                            Catatan = !string.IsNullOrEmpty(o.QuestionNote) ? $"Detail: {o.QuestionNote}" : "-"
                        });
                    }

                    // Pastikan minimal ada 1 sheet jika database kosong
                    if (!allQuestionsMap.Any()) allQuestionsMap["System"] = new List<dynamic>();

                    // 4. GENERATE EXCEL WORKBOOK MULTIPLE SHEETS
                    using (var workbook = new XLWorkbook())
                    {
                        foreach (var pair in allQuestionsMap)
                        {
                            string category = pair.Key;
                            var itemRows = pair.Value;
                            var ws = workbook.Worksheets.Add(category);

                            // Setup Grid 62 Kolom fix
                            for (int i = 1; i <= 62; i++) ws.Column(i).Width = 1.65;

                            // =========================================================
                            // KOP LAPORAN / KEPALA TABEL (BARIS 1 - 4)
                            // =========================================================
                            var rangeLogo = ws.Range(1, 1, 4, 8).Merge();
                            rangeLogo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            rangeLogo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            rangeLogo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            try
                            {
                                string imagePath = Server.MapPath("~/Images/niterra-logo.jpg");
                                if (System.IO.File.Exists(imagePath))
                                    ws.AddPicture(imagePath).MoveTo(ws.Cell(1, 1), 6, 6).WithSize(105, 52);
                            }
                            catch { }

                            ws.Range(1, 9, 2, 50).Merge().Value = "FORMULIR INTEGRASI";
                            ws.Range(1, 9, 2, 50).Style.Font.Bold = true;
                            ws.Range(1, 9, 2, 50).Style.Font.FontSize = 13;
                            ws.Range(1, 9, 2, 50).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                            ws.Range(1, 9, 2, 50).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            ws.Range(1, 51, 1, 54).Merge().Value = "No. Dok";
                            ws.Range(1, 55, 1, 62).Merge().Value = "PMLK3-MR-09/L3";
                            ws.Range(2, 51, 2, 54).Merge().Value = "Revisi";
                            ws.Range(2, 55, 2, 62).Merge().Value = "03";

                            ws.Range(3, 9, 4, 50).Merge().Value = $"CATATAN AUDIT";
                            ws.Range(3, 9, 4, 50).Style.Font.Bold = true;
                            ws.Range(3, 9, 4, 50).Style.Font.FontSize = 13;
                            ws.Range(3, 9, 4, 50).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                            ws.Range(3, 9, 4, 50).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            ws.Range(3, 51, 3, 54).Merge().Value = "Tanggal";
                            ws.Range(3, 55, 3, 62).Merge().Value = "20-May-2026";
                            ws.Range(4, 51, 4, 54).Merge().Value = "Halaman";
                            ws.Range(4, 55, 4, 62).Merge().Value = "1 / 1";

                            ws.Range(1, 51, 4, 62).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                            ws.Range(1, 51, 4, 62).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            // =========================================================
                            // IDENTITAS AUDIT & SIGNATURE BOXES (BARIS 5 - 9)
                            // =========================================================
                            Action<IXLRange> SetDashedBorderY = (r) => {
                                r.Style.Border.TopBorder = XLBorderStyleValues.Dashed; r.Style.Border.BottomBorder = XLBorderStyleValues.Dashed;
                                r.Style.Border.LeftBorder = XLBorderStyleValues.None; r.Style.Border.RightBorder = XLBorderStyleValues.None;
                            };

                            ws.Range(5, 1, 5, 12).Merge().Value = "Tanggal Audit"; SetDashedBorderY(ws.Range(5, 1, 5, 12)); ws.Cell(5, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            ws.Cell(5, 13).Value = ":"; SetDashedBorderY(ws.Range(5, 13, 5, 13)); ws.Cell(5, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Range(5, 14, 5, 43).Merge().Value = tanggalAudit; SetDashedBorderY(ws.Range(5, 14, 5, 43)); ws.Cell(5, 43).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                            ws.Range(5, 44, 5, 53).Merge().Value = "Auditor"; ws.Range(5, 44, 5, 53).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; ws.Range(5, 44, 5, 53).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; ws.Range(5, 44, 5, 53).Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");
                            ws.Range(5, 54, 5, 62).Merge().Value = "Auditee"; ws.Range(5, 54, 5, 62).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; ws.Range(5, 54, 5, 62).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; ws.Range(5, 54, 5, 62).Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");

                            ws.Range(6, 1, 6, 12).Merge().Value = "Dept/Proses"; SetDashedBorderY(ws.Range(6, 1, 6, 12)); ws.Cell(6, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            ws.Cell(6, 13).Value = ":"; SetDashedBorderY(ws.Range(6, 13, 6, 13)); ws.Cell(6, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Range(6, 14, 6, 43).Merge().Value = proses; SetDashedBorderY(ws.Range(6, 14, 6, 43)); ws.Cell(6, 43).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                            ws.Range(7, 1, 7, 12).Merge().Value = "Kriteria Audit"; SetDashedBorderY(ws.Range(7, 1, 7, 12)); ws.Cell(7, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            ws.Cell(7, 13).Value = ":"; SetDashedBorderY(ws.Range(7, 13, 7, 13)); ws.Cell(7, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            var cellKrit = ws.Range(7, 14, 7, 43).Merge().FirstCell(); SetDashedBorderY(ws.Range(7, 14, 7, 43)); ws.Cell(7, 43).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                            cellKrit.RichText.ClearText();
                            string defaultKrit = "ISO 9001 : 2015 / IATF 16949 : 2016 / ISO 14001 : 2015 / ISO 45001 : 2018";
                            var splitActive = kriteria.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToLower()).ToList();
                            var splitParts = defaultKrit.Split('/').Select(s => s.Trim()).ToList();
                            for (int pIdx = 0; pIdx < splitParts.Count; pIdx++)
                            {
                                string segment = splitParts[pIdx];
                                if (splitActive.Any(sa => segment.ToLower().Contains(sa))) cellKrit.RichText.AddText(segment).SetBold(true).SetFontColor(XLColor.Black);
                                else cellKrit.RichText.AddText(segment).SetStrikethrough(true).SetFontColor(XLColor.Gray);
                                if (pIdx < splitParts.Count - 1) cellKrit.RichText.AddText(" / ").SetFontColor(XLColor.Black).SetBold(false).SetStrikethrough(false);
                            }
                            cellKrit.RichText.AddText(" *)").SetItalic(true).SetFontColor(XLColor.Black);

                            string isSysCheck = category == "System" ? "■" : "□";
                            string isProCheck = category == "Process" ? "■" : "□";
                            string isPrdCheck = category == "Product" ? "■" : "□";

                            ws.Range(8, 1, 8, 12).Merge().Value = "Jenis Audit"; SetDashedBorderY(ws.Range(8, 1, 8, 12)); ws.Cell(8, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            ws.Cell(8, 13).Value = ":"; SetDashedBorderY(ws.Range(8, 13, 8, 13)); ws.Cell(8, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Range(8, 14, 8, 43).Merge().Value = $"{isSysCheck} Sistem / {isProCheck} Proses / {isPrdCheck} Produk"; SetDashedBorderY(ws.Range(8, 14, 8, 43)); ws.Cell(8, 43).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                            ws.Range(9, 1, 9, 43).Merge().Value = " "; SetDashedBorderY(ws.Range(9, 1, 9, 43)); ws.Range(9, 1, 9, 43).Style.Border.BottomBorder = XLBorderStyleValues.Thin; ws.Cell(9, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin; ws.Cell(9, 43).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                            ws.Range(5, 1, 9, 43).Style.Font.Bold = true;

                            // Injeksi Stempel Gambar & Nama Approval ke Kotak (Baris 6 s/d 8)
                            string fullRecapCategory = $"Catatan Audit ({category})";

                            CreateSignatureBoxEventExcel(ws, 6, 44, 53, rawApprovals, signerNameMap, fullRecapCategory, "Auditor");
                            CreateSignatureBoxEventExcel(ws, 6, 54, 62, rawApprovals, signerNameMap, fullRecapCategory, "Auditee");

                            // =========================================================
                            // HISTORICAL PROBLEM / PANDUAN AUDIT (BARIS 10 - 12)
                            // =========================================================
                            var catGuides = guidesData.Where(g => g.category_audit == category).Select(g => g.guide_name).ToList();
                            Func<string, string> IsBoxCheck = (n) => catGuides.Contains(n) ? "■" : "□";
                            var listDefaults = new List<string> { "Customer Claim", "Rejection", "PQCC", "CSR", "Temuan audit", "FMEA", "Klausul ISO" };
                            var extraGuides = catGuides.Where(g => !listDefaults.Contains(g)).ToList();
                            string signLain = extraGuides.Any() ? "■" : "□";
                            string stringLain = extraGuides.Any() ? string.Join(", ", extraGuides) : "........................";

                            ws.Range(10, 1, 10, 62).Merge().Value = "Historical problem / panduan audit";
                            ws.Range(10, 1, 10, 62).Style.Font.Bold = true;
                            ws.Range(10, 1, 10, 62).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            ws.Range(10, 1, 10, 62).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            ws.Range(10, 1, 10, 62).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                            ws.Cell(11, 1).Value = IsBoxCheck("Customer Claim"); ws.Range(11, 2, 11, 10).Merge().Value = "Customer Claim";
                            ws.Cell(11, 11).Value = IsBoxCheck("Rejection"); ws.Range(11, 12, 11, 17).Merge().Value = "Rejection";
                            ws.Cell(11, 18).Value = IsBoxCheck("PQCC"); ws.Range(11, 19, 11, 25).Merge().Value = "PQCC";
                            ws.Cell(11, 26).Value = IsBoxCheck("CSR"); ws.Range(11, 27, 11, 62).Merge().Value = "CSR";
                            ws.Range(11, 1, 11, 62).Style.Font.Bold = true;
                            ws.Cell(11, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin; ws.Cell(11, 62).Style.Border.RightBorder = XLBorderStyleValues.Thin;

                            ws.Cell(12, 1).Value = IsBoxCheck("Temuan audit"); ws.Range(12, 2, 12, 10).Merge().Value = "Temuan audit";
                            ws.Cell(12, 11).Value = IsBoxCheck("FMEA"); ws.Range(12, 12, 12, 17).Merge().Value = "FMEA";
                            ws.Cell(12, 18).Value = IsBoxCheck("Klausul ISO"); ws.Range(12, 19, 12, 25).Merge().Value = "Klausul ISO";
                            ws.Cell(12, 26).Value = signLain; ws.Range(12, 27, 12, 62).Merge().Value = "Lain-lain: " + stringLain;
                            ws.Range(12, 1, 12, 62).Style.Font.Bold = true;
                            ws.Cell(12, 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin; ws.Cell(12, 62).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                            ws.Range(12, 1, 12, 62).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                            // =========================================================
                            // TABLE COLUMN HEADERS (BARIS 13)
                            // =========================================================
                            Action<IXLRange> SetHeadStyle = (rng) => {
                                rng.Style.Font.Bold = true; rng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                rng.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center; rng.Style.Fill.BackgroundColor = XLColor.FromHtml("#f9fafb");
                                rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            };

                            ws.Range(13, 1, 13, 2).Merge().Value = "No."; SetHeadStyle(ws.Range(13, 1, 13, 2));
                            ws.Range(13, 3, 13, 8).Merge().Value = "Klausul"; SetHeadStyle(ws.Range(13, 3, 13, 8));
                            ws.Range(13, 9, 13, 28).Merge().Value = "Pertanyaan"; SetHeadStyle(ws.Range(13, 9, 13, 28));
                            ws.Range(13, 29, 13, 62).Merge().Value = "CATATAN AUDIT"; SetHeadStyle(ws.Range(13, 29, 13, 62));

                            // =========================================================
                            // INJEKSI BARIS DATA DARI LIST KATEGORI (BARIS 14 KE BAWAH)
                            // =========================================================
                            int currentRow = 14;
                            if (itemRows.Count == 0)
                            {
                                var rngEmpty = ws.Range(currentRow, 1, currentRow, 62).Merge();
                                rngEmpty.Value = "Tidak ada catatan temuan untuk kategori ini.";
                                rngEmpty.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                rngEmpty.Style.Font.Italic = true;
                                rngEmpty.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                currentRow++;
                            }
                            else
                            {
                                int loopIdx = 1;
                                foreach (var rowItem in itemRows)
                                {
                                    ws.Range(currentRow, 1, currentRow, 2).Merge().Value = loopIdx;

                                    // *** PERBAIKAN: Format Klausul sebagai teks agar tidak berubah jadi tanggal ***
                                    var tKlausul = ws.Range(currentRow, 3, currentRow, 8).Merge();
                                    tKlausul.Style.NumberFormat.Format = "@";
                                    tKlausul.Value = rowItem.Clausul;

                                    var cellQ = ws.Range(currentRow, 9, currentRow, 28).Merge().FirstCell();
                                    cellQ.Value = rowItem.Pertanyaan;
                                    cellQ.Style.Alignment.WrapText = true;

                                    var cellA = ws.Range(currentRow, 29, currentRow, 62).Merge().FirstCell();
                                    cellA.Value = rowItem.Catatan;
                                    cellA.Style.Alignment.WrapText = true;

                                    var rowRange = ws.Range(currentRow, 1, currentRow, 62);
                                    rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                                    rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                    rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                                    ws.Range(currentRow, 1, currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                    // Kalkulasi Row Height Manual
                                    string pText = rowItem.Pertanyaan;
                                    string cText = rowItem.Catatan;
                                    int lineCount = Math.Max(pText.Split('\n').Length + (pText.Length / 45), cText.Split('\n').Length + (cText.Length / 75));
                                    ws.Row(currentRow).Height = Math.Max(35, (lineCount * 14) + 8);

                                    loopIdx++; currentRow++;
                                }
                            }

                            // =========================================================
                            // FOOTER SECTION: KESIMPULAN & NOTE PERBAIKAN
                            // =========================================================
                            var catNotes = notesData.Where(n => n.category_audit == category).Select(n => n.note).ToList();
                            if (catNotes.Any())
                            {
                                var cellKesimpulan = ws.Range(currentRow, 1, currentRow, 62).Merge().FirstCell();
                                cellKesimpulan.RichText.ClearText();
                                cellKesimpulan.RichText.AddText("Kesimpulan:").SetBold().SetItalic();
                                cellKesimpulan.RichText.AddText(Environment.NewLine);
                                foreach (var note in catNotes)
                                {
                                    cellKesimpulan.RichText.AddText(note);
                                    cellKesimpulan.RichText.AddText(Environment.NewLine);
                                }
                                ws.Range(currentRow, 1, currentRow, 62).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                ws.Range(currentRow, 1, currentRow, 62).Style.Alignment.WrapText = true;
                                ws.Range(currentRow, 1, currentRow, 62).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                                ws.Row(currentRow).Height = Math.Max(35, (catNotes.Count * 15) + 20);
                                currentRow++;
                            }

                            // 1. Ambil sel pertama (induk) dari merged range
                            var cellNote = ws.Range(currentRow, 1, currentRow, 62).Merge().FirstCell();

                            // 2. PAKSA WRAP TEXT LANGSUNG KE SEL INDUKNYA (Ini kuncinya)
                            cellNote.Style.Alignment.WrapText = true;
                            cellNote.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                            // 3. Isi RichText seperti biasa
                            cellNote.RichText.ClearText();
                            cellNote.RichText.AddText("*) Coret yang tidak diperlukan");
                            cellNote.RichText.AddText(Environment.NewLine);
                            cellNote.RichText.AddText("Note :").SetBold().SetItalic();
                            cellNote.RichText.AddText(Environment.NewLine);
                            cellNote.RichText.AddText("1. Beri tanda check list (√) pada jenis audit yang dilakukan ( Sistem & Proses ), sebagai bukti sudah dilakukan audit sistem & proses");
                            cellNote.RichText.AddText(Environment.NewLine);
                            cellNote.RichText.AddText("2. Isi & lengkapi kolom yang tertera pada kolom jenis audit untuk produk ( Type, Lot No & Q'ty ), sebagai bukti sudah dilakukan audit produk");
                            cellNote.RichText.AddText(Environment.NewLine);
                            cellNote.RichText.AddText("3. Beri tanda check list (√) pada historical problem, sebagai bukti bahwa sudah dilakukan audit internal berdasarkan item-item yang ada pada historical problem");

                            ws.Range(currentRow, 1, currentRow, 62).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            ws.Range(currentRow, 1, currentRow, 62).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                            ws.Row(currentRow).Height = 75;
                        }

                        // 5. EXPORT & DOWNLOAD FILESTREAM
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            string safeName = string.Join("_", proses.Split(Path.GetInvalidFileNameChars()));
                            string fileName = $"Catatan_Audit_Report_{safeName}.xlsx";
                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"Gagal melakukan Export Excel Catatan Audit: {ex.Message}");
            }
        }

        // ====================================================================================
        // Helper Khusus Menggambar Stempel & Centering Posisi Koordinat Sel Gabungan (Worksheet)
        // ====================================================================================
        private void CreateSignatureBoxEventExcel(IXLWorksheet ws, int startRow, int startCol, int endCol, List<Quality_EAudit_Recap_Approval> approvals, Dictionary<string, string> signerNameMap, string recapCategory, string roleName)
        {
            // 1. Pecah area menjadi dua: Area Stempel (3 baris atas) dan Area Nama (1 baris bawah)
            var signBox = ws.Range(startRow, startCol, startRow + 2, endCol).Merge(); // Rowspan 3
            var nameBox = ws.Range(startRow + 3, startCol, startRow + 3, endCol).Merge(); // Rowspan 1


            // 2. Set bingkai (border) luar untuk keseluruhan 4 baris agar menjadi 1 kotak utuh
            var fullBox = ws.Range(startRow, startCol, startRow + 3, endCol);
            fullBox.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // 3. Set posisi teks nama persis di kotak bawah (nameBox)
            nameBox.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            nameBox.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            nameBox.Style.Font.FontSize = 8.5;
            nameBox.Style.Font.Bold = true;

            // 4. KUNCI TINGGI BARIS (Row Height) agar gambar stempel punya ruang dan tidak memipih/hilang
            ws.Row(startRow).Height = 20;
            ws.Row(startRow + 1).Height = 20;
            ws.Row(startRow + 2).Height = 20;
            ws.Row(startRow + 3).Height = 20;

            // Value default jika belum sign
            nameBox.Value = "-";

            if (approvals != null)
            {
                // 5. PENGAMAN PENCARIAN STRING (Case-Insensitive, Trim, & StartsWith)
                var approval = approvals.FirstOrDefault(x =>
                    !string.IsNullOrEmpty(x.category_recap) &&
                    x.category_recap.Trim().ToLower() == recapCategory.Trim().ToLower() &&
                    !string.IsNullOrEmpty(x.recap_sign_role) &&
                    x.recap_sign_role.Trim().ToLower() == roleName.Trim().ToLower()
                );

                if (approval != null && !string.IsNullOrEmpty(approval.recap_sign_status))
                {
                    string status = approval.recap_sign_status.ToUpper();
                    string dateStr = approval.recap_sign_date != null ? Convert.ToDateTime(approval.recap_sign_date).ToString("dd MMM yyyy").ToUpper() : "";

                    string nik = approval.recap_sign_by;
                    string byName = (signerNameMap != null && signerNameMap.ContainsKey(nik)) ? signerNameMap[nik] : nik ?? "";

                    if (!string.IsNullOrEmpty(byName))
                        byName = string.Join(" ", byName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

                    // Masukkan nama ke baris paling bawah
                    nameBox.Value = byName;

                    System.Drawing.Color stampColor = status == "APPROVED" ? System.Drawing.Color.FromArgb(5, 150, 105) : System.Drawing.Color.FromArgb(220, 38, 38);

                    try
                    {
                        using (Bitmap bmp = new Bitmap(200, 200))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                                g.Clear(System.Drawing.Color.Transparent);

                                g.TranslateTransform(100, 100); g.RotateTransform(-12); g.TranslateTransform(-100, -100);

                                using (Pen penOuter = new Pen(stampColor, 6)) { g.DrawEllipse(penOuter, 10, 10, 180, 180); }
                                using (Pen penInner = new Pen(stampColor, 2)) { g.DrawEllipse(penInner, 18, 18, 164, 164); }

                                using (Font fontStatus = new Font("Arial", 26, FontStyle.Bold))
                                {
                                    SizeF textSize = g.MeasureString(status, fontStatus);
                                    g.DrawString(status, fontStatus, new SolidBrush(stampColor), (200 - textSize.Width) / 2, 65);
                                }

                                using (Pen penLine = new Pen(stampColor, 2)) { g.DrawLine(penLine, 40, 105, 160, 105); }

                                using (Font fontDate = new Font("Arial", 16, FontStyle.Bold))
                                {
                                    SizeF dateSize = g.MeasureString(dateStr, fontDate);
                                    g.DrawString(dateStr, fontDate, new SolidBrush(stampColor), (200 - dateSize.Width) / 2, 115);
                                }
                            }

                            using (MemoryStream ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Position = 0;

                                string shortName = "Stamp_" + Guid.NewGuid().ToString("N").Substring(0, 10);

                                const int MDW = 7; // sesuaikan kalau default font workbook bukan Calibri 11

                                // Hitung lebar box secara presisi
                                double totalWidthPx = 0;
                                for (int c = startCol; c <= endCol; c++)
                                {
                                    double w = ws.Column(c).Width;
                                    totalWidthPx += Math.Truncate(((256 * w + Math.Truncate(128.0 / MDW)) / 256) * MDW);
                                }

                                // Hitung tinggi box stempel (3 baris) secara presisi
                                double totalHeightPx = (ws.Row(startRow).Height + ws.Row(startRow + 1).Height + ws.Row(startRow + 2).Height) * 96.0 / 72.0;

                                double imageWidthPx = 200 * 0.23;
                                double imageHeightPx = 200 * 0.23;

                                int offsetX = (int)Math.Max(0, (totalWidthPx - imageWidthPx) / 2);
                                int offsetY = (int)Math.Max(0, (totalHeightPx - imageHeightPx) / 2);

                                var cell = ws.Cell(startRow, startCol);
                                ws.AddPicture(ms, shortName).MoveTo(cell, offsetX, offsetY).Scale(0.23);
                            }
                        }
                    }
                    catch
                    {
                        // Mencegah file Excel gagal dibuat sepenuhnya jika GDI+ / Bitmap gagal di server
                    }
                }
            }
        }

        private void SendEmailNotificationFindingsProblem(List<string> targetEmails, List<string> ofiIds, List<string> carIds)
        {
            try
            {
                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
                var password = "100%NGKbusi!";
                var sub = "New Internal Audit Findings (CAR & OFI)";

                StringBuilder htmlBody = new StringBuilder();
                htmlBody.Append("<p>Dear Auditors and Auditees,</p>");
                htmlBody.Append("<p>The E-Audit system has recorded new findings that require your follow-up. Please promptly review and complete the data via the links below:</p>");

                if (ofiIds != null && ofiIds.Any())
                {
                    htmlBody.Append("<h3>Opportunity for Improvement (OFI) List:</h3><ul>");
                    foreach (var ofiId in ofiIds.Distinct())
                    {
                        string url = Url.Action("DetailOFI", "EAudit", new { id = ofiId }, Request.Url.Scheme);
                        htmlBody.Append($"<li><a href='{url}'>{ofiId}</a></li>");
                    }
                    htmlBody.Append("</ul>");
                }

                if (carIds != null && carIds.Any())
                {
                    htmlBody.Append("<h3>Corrective Action Request (CAR) List:</h3><ul>");
                    foreach (var carId in carIds.Distinct())
                    {
                        string url = Url.Action("DetailCAR", "EAudit", new { id = carId }, Request.Url.Scheme);
                        htmlBody.Append($"<li><a href='{url}'>{carId}</a></li>");
                    }
                    htmlBody.Append("</ul>");
                }

                htmlBody.Append("<br><p>Thank you for your attention and cooperation.</p>");
                htmlBody.Append("<i>This is an automated email, please do not reply.</i>");
                var body = htmlBody.ToString();

                var smtp = new SmtpClient
                {
                    Host = "ngkbusi.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, password)
                };

                using (var mess = new MailMessage()
                {
                    From = senderEmail,
                    Subject = sub,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    if (targetEmails != null)
                    {
                        foreach (var dataEmail in targetEmails)
                        {
                            if (!string.IsNullOrWhiteSpace(dataEmail))
                            {
                                mess.To.Add(new MailAddress(dataEmail.Trim()));
                            }
                        }
                    }

                    mess.Bcc.Add(new MailAddress("azis.abdillah@niterragroup.com"));

                    smtp.Send(mess);
                }
            }
            catch (Exception ex)
            {
                string realError = ex.Message;
                if (ex.InnerException != null)
                {
                    realError += " | Inner: " + ex.InnerException.Message;
                }

                System.Diagnostics.Debug.WriteLine("=== GAGAL KIRIM EMAIL ===");
                System.Diagnostics.Debug.WriteLine(realError);
            }
        }

        private void SendEmailApprovalCAR(List<string> targetEmails, string noReport, string groupType, bool isApproved)
        {
            try
            {
                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
                var password = "100%NGKbusi!";

                string statusText = isApproved ? "Approved" : "Rejected";
                string statusColor = isApproved ? "inherit" : "red";

                var sub = $"[Niterra-Portal-Notification] CAR {groupType} {statusText} - {noReport}";

                var data = dbq.Quality_EAudit_CAR_Header
                    .Where(x => x.no_report == noReport)
                    .Select(y => new
                    {
                        no_report = y.no_report,
                        reply_deadline = y.reply_deadline
                    }).FirstOrDefault();

                StringBuilder htmlBody = new StringBuilder();

                string url = Url.Action("DetailCAR", "EAudit", new { id = noReport }, Request.Url.Scheme);
                string detailAction = isApproved ? "approval details" : "rejection details and feedback";

                if (!string.IsNullOrEmpty(groupType) && groupType.Equals("Auditor Approval", StringComparison.OrdinalIgnoreCase))
                {
                    string dueDatePlaceholder = (data != null && data.reply_deadline != null)
                        ? Convert.ToDateTime(data.reply_deadline).ToString("dd MMM yyyy")
                        : "[Belum Ditentukan]";

                    htmlBody.Append("<p>Dear Auditees,</p>");
                    htmlBody.Append($"<p>The Auditor Approval for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been <b style='color:{statusColor};'>{statusText}</b>.</p>");

                    htmlBody.Append($"<p>Please follow up <b>{noReport}</b> with the due date <b>{dueDatePlaceholder}</b>, and approved after CAR Completed.</p>");

                    string noteText = isApproved
                        ? "The auditee can now proceed to complete the CAR data."
                        : "The auditee needs to revise and update the CAR data based on the auditor's feedback.";
                    htmlBody.Append($"<p><b>Note:</b> {noteText}</p>");

                    htmlBody.Append($"<p>Please click the following link to view the {detailAction}:</p>");
                    htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                    htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                    htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");
                }
                else if (!string.IsNullOrEmpty(groupType) && groupType.Equals("Auditee Approval", StringComparison.OrdinalIgnoreCase))
                {
                    htmlBody.Append("<p>Dear Auditors,</p>");
                    htmlBody.Append($"<p>The Auditee Approval for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been <b style='color:{statusColor};'>{statusText}</b>.</p>");

                    // Memperbaiki format string interpolation C# dan operasi penambahan hari menggunakan AddDays()
                    string maxVerificationDate = DateTime.Now.AddDays(3).ToString("dd MMM yyyy");
                    htmlBody.Append($"<p>Please do the verification to 1 maximum <b>{maxVerificationDate}</b>.</p>");

                    string noteText = isApproved
                        ? "The auditor can now proceed to perform Verification 1."
                        : "The auditor needs to review the verification data again.";
                    htmlBody.Append($"<p><b>Note:</b> {noteText}</p>");

                    htmlBody.Append($"<p>Please click the following link to view the {detailAction}:</p>");
                    htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                    htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                    htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");
                }
                else
                {
                    htmlBody.Append("<p>Dear Auditors and Auditees,</p>");
                    htmlBody.Append($"<p>The <b>{groupType}</b> for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been <b style='color:{statusColor};'>{statusText}</b>.</p>");

                    htmlBody.Append($"<p>Please click the following link to view the {detailAction}:</p>");
                    htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                    htmlBody.Append("<br><p>Thank you for your attention and cooperation.</p>");
                    htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");
                }

                var smtp = new SmtpClient
                {
                    Host = "ngkbusi.com",
                    Port = 587,
                    EnableSsl = true,
                    Timeout = 5000,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, password)
                };

                using (var mess = new MailMessage()
                {
                    From = senderEmail,
                    Subject = sub,
                    Body = htmlBody.ToString(),
                    IsBodyHtml = true
                })
                {
                    foreach (var email in targetEmails)
                    {
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            mess.To.Add(new MailAddress(email.Trim()));
                        }
                    }

                    mess.Bcc.Add(new MailAddress("azis.abdillah@niterragroup.com"));

                    smtp.Send(mess);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send {(isApproved ? "Approval" : "Rejection")} Notification email: " + ex.Message);
            }
        }

        private void SendEmailVerificationCAR(List<string> targetEmails, string noReport, string verificationStatus, string verificationName, string approverRole, string stakeholder_id)
        {
            try
            {


                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
                var password = "100%NGKbusi!";

                var sub = $"[Niterra-Portal-Notification] CAR Verification Update - {noReport}";

                StringBuilder htmlBody = new StringBuilder();

                string url = Url.Action("DetailCAR", "EAudit", new { id = noReport }, Request.Url.Scheme);

                bool isVerifikasi1 = !string.IsNullOrEmpty(verificationName) && verificationName.Equals("Verifikasi 1", StringComparison.OrdinalIgnoreCase);
                bool isVerifikasi2 = !string.IsNullOrEmpty(verificationName) && verificationName.Equals("Verifikasi 2", StringComparison.OrdinalIgnoreCase);

                bool isRejected = verificationStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase);
                string statusColor = isRejected ? "red" : "green";

                // ================= VERIFIKASI 1 - AUDITOR =================
                if (isVerifikasi1 && approverRole == "Auditor")
                {
                    if (!isRejected) // Jika Approved
                    {
                        string maxDate = DateTime.Now.AddDays(7).ToString("dd MMM yyyy");

                        htmlBody.Append("<p>Dear MR,</p>");
                        htmlBody.Append($"<p>The Verification for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been verified 1.</p>");
                        htmlBody.Append($"<p>Current Verification Status: <b style='color:{statusColor};'>Approved 1st verification</b></p>");

                        htmlBody.Append($"<p>Please do the verification to 1 maximum <b>{maxDate}</b> (7 days after CAR Approved by Auditors).</p>");

                        htmlBody.Append("<p><b>Note to MR:</b> Verifikasi 1 has been <b>Approved</b> by the Auditor. Please proceed to review it.</p>");

                        htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");
                        var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                               .FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholder_id);

                        if (stakeholder != null)
                        {
                            var auditEvent = dbq.Quality_EAudit_Audit_Event
                                .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                            if (auditEvent != null && !string.IsNullOrEmpty(auditEvent.audit_standard))
                            {
                                var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                                    .FirstOrDefault(s => s.audit_standard_name == auditEvent.audit_standard && s.deleted_at == null);

                                if (standardMaster != null && !string.IsNullOrWhiteSpace(standardMaster.mr_email))
                                {
                                    targetEmails.Add(standardMaster.mr_email.Trim());
                                }
                            }
                        }

                        targetEmails = targetEmails.Distinct().ToList();
                    }
                    else // Jika Rejected
                    {
                        htmlBody.Append("<p>Dear Auditees,</p>");
                        htmlBody.Append($"<p>The Verification 1 for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been updated.</p>");
                        htmlBody.Append($"<p>Current Verification 1 Status: <b style='color:{statusColor};'>Rejected</b></p>");

                        htmlBody.Append("<p>Please revise CAR as Auditors recommendation.<br>");
                        htmlBody.Append("Please ask detail to Auditors for detail recommendation.</p>");

                        htmlBody.Append("<p><b>Note to Auditor & Auditee:</b> Verification 1 has been <b>Rejected by the MR</b>. Please review the feedback and make necessary revisions.</p>");

                        htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");


                    }
                }
                // ================= VERIFIKASI 1 - MR =================
                else if (isVerifikasi1 && approverRole == "MR")
                {
                    if (!isRejected) // Jika Approved
                    {
                        string maxDateVerif2 = DateTime.Now.AddDays(90).ToString("dd MMM yyyy");

                        htmlBody.Append("<p>Dear Auditors,</p>");
                        htmlBody.Append($"<p>The MR Approval for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been Approved.</p>");

                        htmlBody.Append($"<p>Please do the verification to 2 around <b>{maxDateVerif2}</b> (90 days after CAR Approved Verif 1 by MR).</p>");

                        htmlBody.Append("<p><b>Note:</b> The auditor can now proceed to perform Verification 2.</p>");

                        htmlBody.Append($"<p>Please click the following link to view the approval details:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");

                        var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                               .FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholder_id);

                        if (stakeholder != null)
                        {
                            var auditEvent = dbq.Quality_EAudit_Audit_Event
                                .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                            if (auditEvent != null && !string.IsNullOrEmpty(auditEvent.audit_standard))
                            {
                                var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                                    .FirstOrDefault(s => s.audit_standard_name == auditEvent.audit_standard && s.deleted_at == null);

                                if (standardMaster != null && !string.IsNullOrWhiteSpace(standardMaster.mr_email))
                                {
                                    targetEmails.Add(standardMaster.mr_email.Trim());
                                }
                            }
                        }

                        targetEmails = targetEmails.Distinct().ToList();
                    }
                    else // Jika Rejected
                    {
                        htmlBody.Append("<p>Dear Auditors,</p>");
                        htmlBody.Append($"<p>The Verification 1 for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been updated.</p>");
                        htmlBody.Append($"<p>Current Verification 1 Status: <b style='color:{statusColor};'>Rejected</b></p>");

                        htmlBody.Append("<p>Please request to auditee for revise CAR as MR recommendation.<br>");
                        htmlBody.Append("Please ask detail to MR for detail recommendation.</p>");

                        htmlBody.Append("<p><b>Note to Auditor & Auditee:</b> Verification 1 has been <b>Rejected by the MR</b>. Please review the feedback and make necessary revisions.</p>");

                        htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");

                        var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                               .FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholder_id);

                        if (stakeholder != null)
                        {
                            var auditEvent = dbq.Quality_EAudit_Audit_Event
                                .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                            if (auditEvent != null && !string.IsNullOrEmpty(auditEvent.audit_standard))
                            {
                                var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                                    .FirstOrDefault(s => s.audit_standard_name == auditEvent.audit_standard && s.deleted_at == null);

                                if (standardMaster != null && !string.IsNullOrWhiteSpace(standardMaster.mr_email))
                                {
                                    targetEmails.Add(standardMaster.mr_email.Trim());
                                }
                            }
                        }

                        targetEmails = targetEmails.Distinct().ToList();
                    }
                }
                // ================= VERIFIKASI 2 - AUDITOR =================
                else if (isVerifikasi2 && approverRole == "Auditor")
                {
                    if (!isRejected) // Jika Approved
                    {
                        string maxDate = DateTime.Now.AddDays(7).ToString("dd MMM yyyy");

                        htmlBody.Append("<p>Dear MR,</p>");
                        htmlBody.Append($"<p>The Verification for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been verified 2.</p>");
                        htmlBody.Append($"<p>Current Verification Status: <b style='color:{statusColor};'>Approved 2nd verification</b></p>");

                        htmlBody.Append($"<p>Please do the verification to 2 maximum <b>{maxDate}</b> (7 days after CAR Approved Verif 1 by Auditors).</p>");

                        htmlBody.Append("<p><b>Note to MR:</b> Verifikasi 2 has been <b>Approved</b> by the Auditor. Please proceed to review it.</p>");

                        htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");
                        var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                               .FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholder_id);

                        if (stakeholder != null)
                        {
                            var auditEvent = dbq.Quality_EAudit_Audit_Event
                                .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                            if (auditEvent != null && !string.IsNullOrEmpty(auditEvent.audit_standard))
                            {
                                var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                                    .FirstOrDefault(s => s.audit_standard_name == auditEvent.audit_standard && s.deleted_at == null);

                                if (standardMaster != null && !string.IsNullOrWhiteSpace(standardMaster.mr_email))
                                {
                                    targetEmails.Add(standardMaster.mr_email.Trim());
                                }
                            }
                        }

                        targetEmails = targetEmails.Distinct().ToList();
                    }
                    else // Jika Rejected
                    {
                        htmlBody.Append("<p>Dear Auditees,</p>");
                        htmlBody.Append($"<p>The Verification 2 for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been updated.</p>");
                        htmlBody.Append($"<p>Current Verification 2 Status: <b style='color:{statusColor};'>Rejected</b></p>");

                        htmlBody.Append("<p>Please revise CAR as Auditors recommendation.<br>");
                        htmlBody.Append("Please ask detail to Auditors for detail recommendation.</p>");

                        htmlBody.Append("<p><b>Note to Auditor & Auditee:</b> Verification 2 has been <b>Rejected by the Auditor</b>. Please review the feedback and make necessary revisions.</p>");

                        htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");

                        var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                               .FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholder_id);

                        if (stakeholder != null)
                        {
                            var auditEvent = dbq.Quality_EAudit_Audit_Event
                                .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                            if (auditEvent != null && !string.IsNullOrEmpty(auditEvent.audit_standard))
                            {
                                var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                                    .FirstOrDefault(s => s.audit_standard_name == auditEvent.audit_standard && s.deleted_at == null);

                                if (standardMaster != null && !string.IsNullOrWhiteSpace(standardMaster.mr_email))
                                {
                                    targetEmails.Add(standardMaster.mr_email.Trim());
                                }
                            }
                        }

                        targetEmails = targetEmails.Distinct().ToList();
                    }
                }
                // ================= VERIFIKASI 2 - MR =================
                else if (isVerifikasi2 && approverRole == "MR")
                {
                    if (!isRejected) // Jika Approved
                    {
                        htmlBody.Append("<p>Dear Auditors, Auditees, and MR,</p>");
                        htmlBody.Append($"<p>The Verification for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been updated.</p>");

                        htmlBody.Append($"<p>Current Verification Status: <b style='color:{statusColor};'>Approved and Closed</b></p>");

                        htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");
                        var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                               .FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholder_id);

                        if (stakeholder != null)
                        {
                            var auditEvent = dbq.Quality_EAudit_Audit_Event
                                .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                            if (auditEvent != null && !string.IsNullOrEmpty(auditEvent.audit_standard))
                            {
                                var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                                    .FirstOrDefault(s => s.audit_standard_name == auditEvent.audit_standard && s.deleted_at == null);

                                if (standardMaster != null && !string.IsNullOrWhiteSpace(standardMaster.mr_email))
                                {
                                    targetEmails.Add(standardMaster.mr_email.Trim());
                                }
                            }
                        }

                        targetEmails = targetEmails.Distinct().ToList();
                    }
                    else // Jika Rejected
                    {
                        htmlBody.Append("<p>Dear Auditors,</p>");
                        htmlBody.Append($"<p>The Verification 2 for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been updated.</p>");
                        htmlBody.Append($"<p>Current Verification 2 Status: <b style='color:{statusColor};'>Rejected</b></p>");

                        htmlBody.Append("<p>Please request to auditee for revise CAR as MR recommendation.<br>");
                        htmlBody.Append("Please ask detail to MR for detail recommendation.</p>");

                        htmlBody.Append("<p><b>Note to Auditor & Auditee:</b> Verification 2 has been <b>Rejected by the MR</b>. Please review the feedback and make necessary revisions.</p>");

                        htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                        htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                        htmlBody.Append("<br><p>Thank you for your attention and cooperation.<br>Quality Management Department</p>");
                        htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");

                        var stakeholder = dbq.Quality_EAudit_Audit_Event_Stakeholder
                               .FirstOrDefault(s => s.audit_event_stakeholder_id == stakeholder_id);

                        if (stakeholder != null)
                        {
                            var auditEvent = dbq.Quality_EAudit_Audit_Event
                                .FirstOrDefault(e => e.audit_event_id == stakeholder.audit_event_id);

                            if (auditEvent != null && !string.IsNullOrEmpty(auditEvent.audit_standard))
                            {
                                var standardMaster = dbq.Quality_EAudit_Audit_Standard_Master
                                    .FirstOrDefault(s => s.audit_standard_name == auditEvent.audit_standard && s.deleted_at == null);

                                if (standardMaster != null && !string.IsNullOrWhiteSpace(standardMaster.mr_email))
                                {
                                    targetEmails.Add(standardMaster.mr_email.Trim());
                                }
                            }
                        }

                        targetEmails = targetEmails.Distinct().ToList();
                    }
                }
                // ================= KONDISI DEFAULT =================
                else
                {
                    htmlBody.Append("<p>Dear Auditors, Auditees, and MR,</p>");
                    htmlBody.Append($"<p>The Verification for Corrective Action Request (CAR) with report number <b>{noReport}</b> has been updated.</p>");
                    htmlBody.Append($"<p>Current Verification Status: <b style='color:{statusColor};'>{verificationStatus}</b></p>");

                    htmlBody.Append($"<p>Please click the following link to view the verification details or comments:</p>");
                    htmlBody.Append($"<p><a href='{url}'>{url}</a></p>");

                    htmlBody.Append("<br><p>Thank you for your attention and cooperation.</p>");
                    htmlBody.Append("<i>This is an automated email, please do not reply to this message.</i>");
                }

                var smtp = new SmtpClient
                {
                    Host = "ngkbusi.com",
                    Port = 587,
                    EnableSsl = true,
                    Timeout = 5000,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, password)
                };

                using (var mess = new MailMessage()
                {
                    From = senderEmail,
                    Subject = sub,
                    Body = htmlBody.ToString(),
                    IsBodyHtml = true
                })
                {
                    foreach (var email in targetEmails)
                    {
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            mess.To.Add(new MailAddress(email.Trim()));
                        }
                    }

                    mess.Bcc.Add(new MailAddress("azis.abdillah@niterragroup.com"));
                    smtp.Send(mess);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to send Verification Notification email: " + ex.Message);
            }
        }
        [HttpPost]
        public JsonResult SendReminderEmail(List<string> emails, string message, string id, string jenisReminder)
        {
            try
            {
                if (emails == null || !emails.Any())
                {
                    return Json(new { status = "error", message = "Penerima email tidak boleh kosong." });
                }

                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
                var password = "100%NGKbusi!";

                string url = "";
                string headerTitle = "";
                string subject = "";

                // Penentuan URL dan Subject berdasarkan Jenis Reminder menggunakan Url.Action agar menyesuaikan domain host secara otomatis
                switch (jenisReminder)
                {
                    case "CAR":
                        url = Url.Action("DetailCAR", "EAudit", new { id = id }, Request.Url.Scheme);
                        subject = $"Reminder: Formulir CAR - {id}";
                        headerTitle = "Corrective Action Request (CAR)";
                        break;
                    case "OFI":
                        url = Url.Action("DetailOFI", "EAudit", new { id = id }, Request.Url.Scheme);
                        subject = $"Reminder: Formulir OFI - {id}";
                        headerTitle = "Opportunity for Improvement (OFI)";
                        break;
                    case "Catatan Audit":
                        // Buat base URL-nya saja, lalu tambahkan parameter secara manual
                        string baseNotesUrl = Url.Action("AuditNotes", "EAudit", null, Request.Url.Scheme);
                        url = $"{baseNotesUrl}?id={id}&type=ALL";

                        subject = $"Reminder: Catatan Audit - {id}";
                        headerTitle = "Catatan Audit";
                        break;

                    case "Laporan Temuan":
                        // Buat base URL-nya saja, lalu tambahkan parameter secara manual
                        string baseFindingsUrl = Url.Action("FindingsReport", "EAudit", null, Request.Url.Scheme);
                        url = $"{baseFindingsUrl}?id={id}";

                        subject = $"Reminder: Laporan Temuan Audit - {id}";
                        headerTitle = "Laporan Temuan Audit";
                        break;
                    default:
                        return Json(new { status = "error", message = "Jenis reminder tidak valid." });
                }

                // Susun HTML Body menggunakan StringBuilder
                StringBuilder htmlBody = new StringBuilder();
                htmlBody.Append("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>");
                htmlBody.Append($"<div style='background-color: #0f766e; color: white; padding: 20px; text-align: center;'><h2 style='margin: 0; font-size: 20px; letter-spacing: 1px;'>{headerTitle}</h2></div>");
                htmlBody.Append("<div style='padding: 30px; color: #374151; line-height: 1.6;'>");

                htmlBody.Append("<p style='margin-top: 0;'>Dear Auditors and Auditees,</p>");
                htmlBody.Append($"<p>The E-Audit system has recorded a reminder that requires your follow-up regarding document number: <strong>{id}</strong>.</p>");

                if (!string.IsNullOrWhiteSpace(message))
                {
                    htmlBody.Append("<div style='background-color: #fffbeb; padding: 15px; border-left: 4px solid #f59e0b; margin: 20px 0; border-radius: 4px;'>");
                    htmlBody.Append("<strong style='color: #b45309;'>Additional Message:</strong><br/>");
                    htmlBody.Append($"<span style='color: #451a03;'>{message}</span>");
                    htmlBody.Append("</div>");
                }

                htmlBody.Append("<p>Please promptly review and complete the data via the link below:</p>");
                htmlBody.Append($"<div style='text-align: center; margin: 35px 0;'><a href='{url}' style='background-color: #0f766e; color: white; padding: 12px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 14px; display: inline-block;'>Open Document</a></div>");

                htmlBody.Append("<br><p style='margin-bottom: 0;'>Thank you for your attention and cooperation.<br/><strong>E-Audit System</strong></p>");
                htmlBody.Append("</div>");
                htmlBody.Append("<div style='background-color: #f9fafb; color: #6b7280; text-align: center; padding: 15px; font-size: 12px; border-top: 1px solid #e5e7eb;'><i>This is an automated email, please do not reply.</i></div>");
                htmlBody.Append("</div>");

                var body = htmlBody.ToString();

                var smtp = new SmtpClient
                {
                    Host = "ngkbusi.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(senderEmail.Address, password)
                };

                using (var mess = new MailMessage()
                {
                    From = senderEmail,
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    // Tambahkan penerima utama
                    foreach (var dataEmail in emails)
                    {
                        if (!string.IsNullOrWhiteSpace(dataEmail))
                        {
                            mess.To.Add(new MailAddress(dataEmail.Trim()));
                        }
                    }

                    // Tambahkan BCC
                    mess.Bcc.Add(new MailAddress("azis.abdillah@niterragroup.com"));

                    // Kirim Email
                    smtp.Send(mess);
                }

                return Json(new { status = "success", message = $"Reminder successfully sent to {emails.Count} recipients." });
            }
            catch (Exception ex)
            {
                string realError = ex.Message;
                if (ex.InnerException != null)
                {
                    realError += " | Inner: " + ex.InnerException.Message;
                }

                System.Diagnostics.Debug.WriteLine("=== GAGAL KIRIM EMAIL REMINDER ===");
                System.Diagnostics.Debug.WriteLine(realError);

                return Json(new { status = "error", message = "Failed to send email: " + realError });
            }
        }

        public ActionResult ViewPrint()
        {
            return View();
        }


        [HttpGet]
        public JsonResult GetLaporanTemuanAuditByEvent(string eventId)
        {
            try
            {
                if (string.IsNullOrEmpty(eventId))
                    return Json(new { success = false, message = "Event ID tidak valid." }, JsonRequestBehavior.AllowGet);

                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();
                var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
                string currentUserName = string.Join(" ", (userProfile?.Name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));

                var eventMaster = dbq.Quality_EAudit_Audit_Event.FirstOrDefault(e => e.audit_event_id == eventId);
                if (eventMaster == null)
                    return Json(new { success = false, message = "Event tidak ditemukan." }, JsonRequestBehavior.AllowGet);

                var auditStandards = eventMaster.audit_standard?.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim()).ToList() ?? new List<string>();

                // [TETAP ADA JIKA MASIH DIGUNAKAN] Query lama untuk Recap Approval
                var approvals = dbq.Quality_EAudit_Recap_Approval
                    .Where(a => a.AuditEventStakeholder.audit_event_id == eventId)
                    .Select(a => new
                    {
                        category_recap = a.category_recap,
                        recap_sign_role = a.recap_sign_role,
                        recap_sign_status = a.recap_sign_status,
                        recap_sign_date = a.recap_sign_date,
                        recap_sign_by_name = a.recap_sign_by
                    })
                    .ToList();

                // [BARU] Menambahkan query untuk Approval Recap Event berdasarkan eventId
                var recapEventApprovals = dbq.Quality_EAudit_Approval_Recap_Event
                    .Where(a => a.audit_event_id == eventId)
                    .Select(a => new
                    {
                        recap_sign_category = a.recap_sign_category,
                        recap_sign_by = a.recap_sign_by,
                        recap_sign_date = a.recap_sign_date,
                        recap_sign_status = a.recap_sign_status
                    })
                    .ToList();

                var validJudgements = new string[] { "major", "minor", "ofi" };
                var questions = dbq.Quality_EAudit_Question_Event
                    .Where(q => q.AuditEventStakeholder.audit_event_id == eventId && q.deleted_at == null && q.judgement != null)
                    .Include(q => q.AuditEventStakeholder.Auditors)
                    .Include(q => q.Finding)
                    .Include(q => q.CARs.Select(c => c.Verification))
                    .Include(q => q.CARs.Select(c => c.Action))
                    .Include(q => q.CARs.Select(c => c.Problem))
                    .Include(q => q.OFIs.Select(o => o.OFIHeader))
                    .ToList()
                    .Where(q => validJudgements.Contains(q.judgement.ToLower()) && q.AuditEventStakeholder.deleted_at == null)
                    .ToList();

                var resultData = questions.Select(q =>
                {
                    var finding = q.Finding?.FirstOrDefault(f => f.deleted_at == null);
                    var car = q.CARs?.FirstOrDefault(c => c.deleted_at == null);
                    var ofi = q.OFIs?.FirstOrDefault();

                    string problemStr = finding?.problem;
                    string locationStr = finding?.location;
                    string objectStr = finding?.object_name;

                    if (string.IsNullOrEmpty(problemStr))
                    {
                        if (car != null && car.Problem != null && car.Problem.Any())
                        {
                            var carProb = car.Problem.FirstOrDefault();
                            problemStr = carProb.problem_name;
                            locationStr = carProb.problem_location;
                            objectStr = carProb.problem_part_name;
                        }
                        else if (ofi != null)
                        {
                            problemStr = null;
                        }
                    }

                    DateTime? actionDateObj = null;
                    if (car != null && car.Action != null && car.Action.Any()) actionDateObj = car.Action.FirstOrDefault()?.action_date;
                    else if (ofi != null) actionDateObj = ofi.action_date;

                    string actionDateFormatted = actionDateObj.HasValue ? actionDateObj.Value.ToString("dd-MMM-yyyy") : "-";


                    string statusTindakLanjut = "-";
                    string url = "-";
                    string verif1Status = "-";
                    string verif2Status = "-";
                    string no = "-";

                    if (car != null)
                    {
                        string carNo = car.no_report;
                        string carStatus = "OPEN";

                        var verif1 = car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 1");
                        var verif2 = car.Verification?.FirstOrDefault(v => v.verification_name == "Verifikasi 2");

                        verif1Status = verif1?.verification_mr_status ?? "-";
                        verif2Status = verif2?.verification_mr_status ?? "-";

                        no = carNo;

                        if (verif1Status != "-" && verif2Status != "-") carStatus = "CLOSED";

                        url = $"{Url.Action("DetailCAR", "EAudit", new { id = carNo }, Request.Url.Scheme)}";
                        statusTindakLanjut = carStatus;
                    }
                    else if (ofi != null)
                    {
                        string ofiNo = ofi.ofi_header_id ?? "-";

                        var targetApproval = ofi.OFIHeader?.ApprovalOFI?.FirstOrDefault(a => a.sign_type == "DiSetujui");

                        verif1Status = targetApproval?.status ?? "-";

                        string ofiStatus = (verif1Status == "Approved") ? "CLOSED" : "OPEN";

                        statusTindakLanjut = ofiStatus;
                        url = $"{Url.Action("DetailOFI", "EAudit", new { id = ofiNo }, Request.Url.Scheme)}";
                    }

                    string sectionName = q.AuditEventStakeholder?.responsibility_section ?? "-";

                    string startDate = q.AuditEventStakeholder?.start_event.HasValue == true
                                        ? q.AuditEventStakeholder.start_event.Value.ToString("dd-MMM-yyyy")
                                        : "-";

                    string categoryAudit = !string.IsNullOrEmpty(q.category_audit) ? q.category_audit : "-";

                    var auditorList = q.AuditEventStakeholder?.Auditors?
                                        .Where(a => a.deleted_at == null
                                                 && a.role != null && a.role.ToLower().Contains("auditor")
                                                 && a.category_audit == q.category_audit)
                                        .Select(a => a.person_name)
                                        .ToList() ?? new List<string>();

                    string auditorNames = auditorList.Any() ? string.Join(", ", auditorList) : "-";

                    return new
                    {
                        section = sectionName,
                        start_date = startDate,
                        auditor = auditorNames,
                        category_audit = categoryAudit,

                        clause = q.clausul ?? "-",
                        problem = problemStr ?? null,
                        location = locationStr ?? null,
                        Object = objectStr ?? null,
                        detail = q.note,
                        klasifikasi_temuan = q.judgement,
                        action_date = actionDateFormatted,
                        no = no,
                        verifikasi1 = verif1Status,
                        verifikasi2 = verif2Status,
                        status_tindak_lanjut = statusTindakLanjut,
                        url = url
                    };
                }).ToList();

                // Return Data Gabungan untuk UI
                return Json(new
                {
                    success = true,
                    data = resultData,
                    existingApprovals = approvals,
                    recapEventApprovals = recapEventApprovals, 
                    auditStandards = auditStandards,
                    currentUserName = currentUserName
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}