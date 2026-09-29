using NGKBusi.Areas.HC.Models;
using NGKBusi.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace NGKBusi.Areas.HC.Controllers
{
    public class MedicalController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        MedicalConnection dbMedical = new MedicalConnection();

        // GET: HC/Medical
        [Authorize]
        public ActionResult Claim()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 1010
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            ViewBag.NavHide = true;
            ViewBag.ThirdParty = db.AX_Vendor_List.Where(w => w.VENDGROUP == "DOM-OTH" || w.VENDGROUP == "OTH").OrderBy(o => o.Name).ToList();
            return View();
        }

        [HttpGet]
        public JsonResult GetNIKSuggestions(string term)
        {
            var results = db.V_Users_Active
                .Where(u => u.NIK.Contains(term) || u.Name.Contains(term))
                .OrderBy(u => u.NIK)
                .Select(u => new
                {
                    label = u.NIK + " - " + u.Name,
                    value = u.NIK + " - " + u.Name,
                    nik = u.NIK,
                    name = u.Name,
                    division = u.DivisionName,
                    department = u.DeptName,
                    position = u.PositionName,
                    costname = u.CostName
                })
                .Take(10)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        // DTOs for deserialization
        private class ClaimDetailPayload
        {
            public string PatientName { get; set; }
            public string Beneficiary { get; set; }
            public string TreatmentCategory { get; set; }
            public string SubCategory { get; set; }
            public string Diagnosa { get; set; }
            public string StartEndDate { get; set; }
            public string InvoiceAmount { get; set; }
            public string ActualAmount { get; set; }
            public string Saving { get; set; }
            public string Deduction { get; set; }
        }

        private class ClaimLinePayload
        {
            public string employee { get; set; } // "NIK - Name"
            public string division { get; set; }
            public string department { get; set; }
            public string position { get; set; }
            public string costname { get; set; }
            public List<ClaimDetailPayload> details { get; set; }
        }

        private class ClaimPayload
        {
            public int? id { get; set; }
            public string invoice { get; set; }
            public string thirdParty { get; set; } // "AccountNum|Name"
            public string hospitalClinic { get; set; } // "Hospital/Clinic Name"
            public List<ClaimLinePayload> lines { get; set; }
        }

        [HttpGet]
        public JsonResult GetClaims(int? year) // Added parameter
        {
            // Create queryable
            var query = dbMedical.HC_Welfare_Medical_Claim_Header.AsQueryable();

            // Apply Filter
            if (year.HasValue)
            {
                query = query.Where(h => h.Created_At.Year == year.Value);
            }

            var raw = query
                .OrderByDescending(h => h.Created_At)
                .Select(h => new
                {
                    ID = h.ID,
                    Invoice = h.Invoice,
                    ThirdPartyId = h.Third_Party_ID,
                    ThirdPartyName = h.Third_Party_Name,
                    CreatedAt = h.Created_At,
                    LinesCount = h.Lines.Count()
                })
                .AsNoTracking()
                .ToList(); // execute query in database

            var result = raw.Select(h => new
            {
                id = h.ID,
                invoice = h.Invoice,
                thirdParty = (h.ThirdPartyId ?? "") + "|" + (h.ThirdPartyName ?? ""),
                thirdPartyName = h.ThirdPartyName,
                createdAt = h.CreatedAt,
                createdAtText = h.CreatedAt.ToString("yyyy-MM-dd"),
                linesCount = h.LinesCount
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetClaim(int id)
        {
            var header = dbMedical.HC_Welfare_Medical_Claim_Header
                .Include(h => h.Lines.Select(l => l.Details))
                .FirstOrDefault(h => h.ID == id);

            if (header == null) return Json(new { success = false, message = "Not found" }, JsonRequestBehavior.AllowGet);

            var payload = new
            {
                id = header.ID,
                invoice = header.Invoice,
                thirdParty = (header.Third_Party_ID ?? "") + "|" + (header.Third_Party_Name ?? ""),
                hospitalClinic = header.Hospital_Clinic,
                createdAt = header.Created_At.ToString("yyyy-MM-dd"),
                lines = header.Lines.Select(l => new
                {
                    employee = (string.IsNullOrWhiteSpace(l.NIK) ? "" : l.NIK + " - " + l.Name),
                    division = l.Division,
                    department = l.Department,
                    position = l.Position,
                    costname = l.CostName,
                    details = l.Details.Select(d => new
                    {
                        PatientName = d.Name,
                        Beneficiary = d.Beneficiary,
                        TreatmentCategory = d.Treatment_Category,
                        SubCategory = d.Sub_Category,
                        Diagnosa = d.Diagnose,
                        StartEndDate = (d.Start_Date != null ? d.Start_Date.ToString("dd-MM-yyyy") : "") + (d.End_Date != null ? " - " + d.End_Date.ToString("dd-MM-yyyy") : ""),
                        InvoiceAmount = d.Invoice_Ammount.ToString(CultureInfo.InvariantCulture),
                        ActualAmount = d.Actual_Ammount.ToString(CultureInfo.InvariantCulture),
                        Saving = d.Saving_Ammount.ToString(CultureInfo.InvariantCulture),
                        Deduction = d.Deduction.ToString(CultureInfo.InvariantCulture)
                    }).ToList()
                }).ToList()
            };

            return Json(new { success = true, claim = payload }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SubmitClaim()
        {
            try
            {
                // Ensure input stream is at start
                if (Request.InputStream.CanSeek)
                    Request.InputStream.Position = 0;

                string input;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    input = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(input))
                    return Json(new { success = false, message = "Empty request body" });

                var js = new JavaScriptSerializer();
                var payload = js.Deserialize<ClaimPayload>(input);

                if (payload == null || string.IsNullOrEmpty(payload.invoice))
                {
                    return Json(new { success = false, message = "Invalid payload" });
                }

                // parse third party
                string tpId = "", tpName = "";
                if (!string.IsNullOrEmpty(payload.thirdParty))
                {
                    var parts = payload.thirdParty.Split(new[] { '|' }, 2);
                    tpId = parts.Length > 0 ? parts[0].Trim() : "";
                    tpName = parts.Length > 1 ? parts[1].Trim() : (parts.Length > 0 ? parts[0].Trim() : "");
                }

                HC_Welfare_Medical_Claim_Header header = null;

                // Check if this is an update (ID provided and > 0)
                if (payload.id.HasValue && payload.id.Value > 0)
                {
                    header = dbMedical.HC_Welfare_Medical_Claim_Header
                        .Include(h => h.Lines.Select(l => l.Details))
                        .FirstOrDefault(h => h.ID == payload.id.Value);

                    if (header == null)
                    {
                        return Json(new { success = false, message = "Claim not found" });
                    }

                    // Update Header Fields
                    header.Invoice = payload.invoice;
                    header.Third_Party_ID = tpId;
                    header.Third_Party_Name = tpName;
                    header.Hospital_Clinic = payload.hospitalClinic;
                    // Note: Created_At and Created_By are preserved from the original record

                    // Replace Lines and Details:
                    // 1. Remove existing lines (and their details)
                    // We create a list copy to iterate safely while removing
                    var linesToDelete = header.Lines.ToList();
                    foreach (var line in linesToDelete)
                    {
                        if (line.Details != null && line.Details.Any())
                        {
                            dbMedical.HC_Welfare_Medical_Claim_Line_Detail.RemoveRange(line.Details);
                        }
                    }
                    dbMedical.HC_Welfare_Medical_Claim_Line.RemoveRange(linesToDelete);
                }
                else
                {
                    // Insert New Header
                    header = new HC_Welfare_Medical_Claim_Header
                    {
                        Invoice = payload.invoice,
                        Third_Party_ID = tpId,
                        Third_Party_Name = tpName,
                        Hospital_Clinic = payload.hospitalClinic,
                        Created_At = DateTime.Now,
                        Created_By = User?.Identity?.Name ?? "",
                        Approval = 1,
                        Approval_Sub = 0,
                        Lines = new List<HC_Welfare_Medical_Claim_Line>()
                    };
                    dbMedical.HC_Welfare_Medical_Claim_Header.Add(header);
                }

                // Populate Lines (for both Insert and Update)
                if (payload.lines != null && payload.lines.Any())
                {
                    foreach (var l in payload.lines)
                    {
                        // employee may be "NIK - Name" or "NIK -Name" etc.
                        string nik = "", name = "";
                        if (!string.IsNullOrEmpty(l.employee))
                        {
                            // try to split by " - " first (the UI uses "NIK - Name"), fallback to '-'
                            var arr = l.employee.Split(new[] { " - " }, StringSplitOptions.None);
                            if (arr.Length == 1)
                                arr = l.employee.Split(new[] { '-' }, 2);
                            nik = arr.Length > 0 ? arr[0].Trim() : l.employee.Trim();
                            name = arr.Length > 1 ? arr[1].Trim() : "";
                        }

                        var line = new HC_Welfare_Medical_Claim_Line
                        {
                            NIK = nik,
                            Name = name,
                            Division = l.division,
                            Department = l.department,
                            Position = l.position,
                            CostName = l.costname,
                            Details = new List<HC_Welfare_Medical_Claim_Line_Detail>()
                        };

                        if (l.details != null && l.details.Any())
                        {
                            foreach (var d in l.details)
                            {
                                // Start/End date parsing:
                                DateTime startDate = DateTime.MinValue;
                                DateTime endDate = DateTime.MinValue;

                                if (!string.IsNullOrWhiteSpace(d.StartEndDate) && d.StartEndDate != "-")
                                {
                                    // UI sends date in one of: "dd/mm/yy", "dd/mm/yyyy", "yyyy-MM-dd", "dd-mm-yyyy"
                                    var parts = d.StartEndDate.Split(new[] { " - " }, StringSplitOptions.None);
                                    var formats = new[] { "dd/MM/yy", "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d/M/yy", "d/M/yyyy" };
                                    DateTime tmp;
                                    if (parts.Length > 0 && DateTime.TryParseExact(parts[0].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmp))
                                    {
                                        startDate = tmp;
                                    }
                                    else if (parts.Length > 0 && DateTime.TryParse(parts[0].Trim(), out tmp))
                                    {
                                        startDate = tmp;
                                    }

                                    if (parts.Length > 1)
                                    {
                                        if (DateTime.TryParseExact(parts[1].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmp))
                                        {
                                            endDate = tmp;
                                        }
                                        else if (DateTime.TryParse(parts[1].Trim(), out tmp))
                                        {
                                            endDate = tmp;
                                        }
                                    }
                                }

                                // numeric parsing (invariant culture)
                                float invoiceAmount = 0f, actualAmount = 0f, savingAmount = 0f, deduction = 0f;
                                if (!string.IsNullOrWhiteSpace(d.InvoiceAmount?.ToString()))
                                    float.TryParse(d.InvoiceAmount.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out invoiceAmount);
                                if (!string.IsNullOrWhiteSpace(d.ActualAmount?.ToString()))
                                    float.TryParse(d.ActualAmount.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out actualAmount);
                                // recompute saving if not provided
                                savingAmount = invoiceAmount - actualAmount;
                                if (!string.IsNullOrWhiteSpace(d.Deduction?.ToString()))
                                    float.TryParse(d.Deduction.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out deduction);

                                var detail = new HC_Welfare_Medical_Claim_Line_Detail
                                {
                                    Name = d.PatientName,
                                    Beneficiary = d.Beneficiary,
                                    Treatment_Category = d.TreatmentCategory,
                                    Sub_Category = d.SubCategory,
                                    Diagnose = d.Diagnosa,
                                    Start_Date = startDate == DateTime.MinValue ? DateTime.Now : startDate,
                                    End_Date = endDate == DateTime.MinValue ? (startDate == DateTime.MinValue ? DateTime.Now : startDate) : endDate,
                                    Invoice_Ammount = invoiceAmount,
                                    Actual_Ammount = actualAmount,
                                    Saving_Ammount = savingAmount,
                                    Deduction = deduction
                                };
                                line.Details.Add(detail);
                            }
                        }

                        header.Lines.Add(line);
                    }
                }

                // minimal validation: payload must have at least one line and at least one detail
                // We check the payload because header.Lines might be in a mixed state during update (contains deleted items)
                bool hasLines = payload.lines != null && payload.lines.Any();
                bool hasDetails = hasLines && payload.lines.Any(x => x.details != null && x.details.Any());

                if (!hasLines || !hasDetails)
                {
                    return Json(new { success = false, message = "No claim lines or details provided" });
                }

                // Save using EF to MedicalConnection
                dbMedical.SaveChanges();

                return Json(new { success = true, id = header.ID });
            }
            catch (Exception ex)
            {
                // return both message and inner message when available
                var inner = ex.InnerException != null ? " | " + ex.InnerException.Message : "";
                return Json(new { success = false, message = ex.Message + inner });
            }
        }

        [HttpPost]
        public JsonResult DeleteClaim(int id)
        {
            try
            {
                var header = dbMedical.HC_Welfare_Medical_Claim_Header
                    .Include(h => h.Lines.Select(l => l.Details))
                    .FirstOrDefault(h => h.ID == id);

                if (header == null) return Json(new { success = false, message = "Claim not found" });

                // Delete details and lines manually to ensure cleanup
                foreach (var line in header.Lines.ToList())
                {
                    if (line.Details != null && line.Details.Any())
                    {
                        dbMedical.HC_Welfare_Medical_Claim_Line_Detail.RemoveRange(line.Details);
                    }
                    dbMedical.HC_Welfare_Medical_Claim_Line.Remove(line);
                }

                dbMedical.HC_Welfare_Medical_Claim_Header.Remove(header);
                dbMedical.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? " | " + ex.InnerException.Message : "";
                return Json(new { success = false, message = ex.Message + inner });
            }
        }
    }
}