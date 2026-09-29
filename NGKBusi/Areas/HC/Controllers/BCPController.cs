using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;

namespace NGKBusi.Areas.HC.Controllers
{
    [Authorize]
    public class BCPController : Controller
    {
        BCPConnection bcpDb = new BCPConnection();
        DefaultConnection db = new DefaultConnection();
        private readonly bool _enableEmail = false; // Set to true to enable email sending (disabled for trial)
    
        // GET: HC/BCP/Dashboard
        public ActionResult Dashboard()
        {
            ViewBag.Title = "BCP Dashboard";
            string currNik = User.Identity.GetUserId() ?? User.Identity.Name;
            if (currNik.Contains("\\")) { currNik = currNik.Split('\\').Last(); }
            currNik = (currNik ?? "").Trim();
            
            // 1. Fetch Summary Stats
            ViewBag.TotalIncidents = bcpDb.HC_BCP_Incident.Count();
            ViewBag.PendingAssessments = bcpDb.HC_BCP_Incident.Count(x => x.Status == 2);
            ViewBag.ImpactedToday = bcpDb.HC_BCP_Incident.Count(x => DbFunctions.TruncateTime(x.Created_At) == DbFunctions.TruncateTime(DateTime.Now));
            ViewBag.RecoveredCount = bcpDb.HC_BCP_Incident.Count(x => x.Status >= 6);


            // 2. Fetch Active BCP for Banner
            var activeBcp = bcpDb.HC_BCP_Incident.OrderByDescending(x => x.Created_At).FirstOrDefault(x => x.Status == 6);
            ViewBag.ActiveBCP = activeBcp;

            // 3. Fetch Top 5 Recent Incidents
            ViewBag.RecentIncidents = bcpDb.HC_BCP_Incident
                                        .OrderByDescending(x => x.Created_At)
                                        .Take(5)
                                        .ToList();

            // 4. Notifications / Awaiting Action
            // In a real app, this would be more complex logic filtering by role
            var userBcp = bcpDb.HC_BCP_Organization.FirstOrDefault(x => x.NIK == currNik && x.IsActive);
            var myTasks = new List<BCPTask>();

            if (userBcp != null)
            {
                // 1. If Committee Member (Manager/AssManager/Member): Need to assess areas?
                var myAreaIds = bcpDb.HC_BCP_User_Area.Where(x => x.NIK == currNik).Select(x => x.AreaID).ToList();
                if (myAreaIds.Any()) {
                    var pendingIncidents = bcpDb.HC_BCP_Incident.Where(x => x.Status <= 2).ToList();
                    foreach(var inc in pendingIncidents) {
                        var assessedInThisInc = bcpDb.HC_BCP_Assessment.Where(a => a.IncidentID == inc.ID && a.Status >= 1).Select(a => a.AreaID).ToList();
                        var unassessedAreas = myAreaIds.Where(aid => !assessedInThisInc.Contains(aid)).ToList();
                        
                        if (unassessedAreas.Any()) {
                            myTasks.Add(new BCPTask { 
                                IncidentNo = inc.IncidentNo, 
                                Title = inc.Title, 
                                Message = $"Assessment needed for {unassessedAreas.Count} areas.", 
                                Type = "Assessment" 
                            });
                        }
                    }
                }

                // 2. If Management: Review required (Status 2, 3, 5)
                if (userBcp.PositionName == "DEPUTY GENERAL MANAGER" || userBcp.PositionName == "SENIOR MANAGER") {
                    var subNiks = GetSubordinateNiks(userBcp.ID, bcpDb);
                    
                    // Incidents where subordinates are involved (Snapshot or User-Area mapping)
                    var pendingReviews = bcpDb.HC_BCP_Incident
                        .Where(x => x.Status == 2 || x.Status == 3 || x.Status == 5)
                        .OrderByDescending(x => x.Created_At)
                        .ToList();

                    foreach(var i in pendingReviews) {
                        // Check if any subordinate is assigned to assess this incident
                        bool isSubInvolved = bcpDb.HC_BCP_User_Area.Any(ua => subNiks.Contains(ua.NIK));
                        // Or if any assessment has already been submitted by a subordinate
                        bool subAssessed = bcpDb.HC_BCP_Assessment.Any(a => a.IncidentID == i.ID && subNiks.Contains(a.AssessorNIK));

                        if (isSubInvolved || subAssessed) {
                            myTasks.Add(new BCPTask { 
                                IncidentNo = i.IncidentNo, 
                                Title = i.Title, 
                                Message = i.Status == 2 ? "Monitoring assessment progress." : "Pending your official review.", 
                                Type = "Review" 
                            });
                        }
                    }
                }
            }
            ViewBag.AwaitingAction = myTasks;

            var myAssignedTodos = bcpDb.HC_BCP_Recovery_Todo
                                       .Include(t => t.RecoveryPlan)
                                       .Include(t => t.RecoveryPlan.Incident)
                                       .Include(t => t.RecoveryPlan.Criteria)
                                       .Where(t => t.PIC_NIK.Trim() == currNik && t.Status != "Complete" && t.RecoveryPlan.Status == "Approved")
                                       .OrderBy(t => t.DueDate)
                                       .ToList();
            ViewBag.MyAssignedTodos = myAssignedTodos;

            bool isAllowedManageBcp = false;
            bool isAllowedSubmitReview = false;
            if (userBcp != null)
            {
                isAllowedManageBcp = bcpDb.HC_BCP_Role_Permission.Any(p => p.IsAllowedManageBCP &&
                    ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                     (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                isAllowedSubmitReview = bcpDb.HC_BCP_Role_Permission.Any(p => p.IsAllowedSubmitReview &&
                    ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                     (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));
            }
            ViewBag.IsAllowedManageBCP = isAllowedManageBcp;
            ViewBag.IsAllowedSubmitReview = isAllowedSubmitReview;

            return View();
        }

        public class BCPTask {
            public string IncidentNo { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public string Type { get; set; }
        }

        // GET: HC/BCP/IncidentList
        public ActionResult IncidentList()
        {
            ViewBag.Title = "Incident History";
            
            // Fetch real dynamic statistics
            ViewBag.TotalIncidents = bcpDb.HC_BCP_Incident.Count();
            ViewBag.OpenIncidents = bcpDb.HC_BCP_Incident.Count(x => x.Status < 6);
            ViewBag.ActiveIncidents = bcpDb.HC_BCP_Incident.Count(x => x.Status == 6);
            ViewBag.ResolvedIncidents = bcpDb.HC_BCP_Incident.Count(x => x.Status == 7);
            
            var incidents = bcpDb.HC_BCP_Incident
                                 .OrderByDescending(x => x.Created_At)
                                 .ToList();
                                 
            ViewBag.Incidents = incidents;
                                 
            return View();
        }

        // GET: HC/BCP/DownloadExcel
        [HttpGet]
        public ActionResult DownloadExcel()
        {
            try
            {
                var incidents = bcpDb.HC_BCP_Incident.OrderByDescending(x => x.Created_At).ToList();

                using (var ms = new System.IO.MemoryStream())
                {
                    using (var wb = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("BCP Incidents");


                        // Title Block
                        ws.Cell(1, 1).Value = "BUSINESS CONTINUITY PLAN - INCIDENT HISTORY REPORT";
                        ws.Cell(1, 1).Style.Font.Bold = true;
                        ws.Cell(1, 1).Style.Font.FontSize = 14;
                        var range1 = ws.Range("A1:L1");
                        range1.Merge();
                        range1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        range1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        ws.Cell(2, 1).Value = "Exported Date: " + DateTime.Now.ToString("dd MMM yyyy, HH:mm");
                        ws.Cell(2, 1).Style.Font.Italic = true;
                        var range2 = ws.Range("A2:L2");
                        range2.Merge();
                        range2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        range2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // Header row
                        var headers = new[] { 
                            "No", "Incident No", "Title", "Category", 
                            "Severity", "Status", "Location", "Personnel Status", 
                            "Reported Date", "Reported By", "Reported By Name", "Description" 
                        };
                        
                        int headerRow = 4;
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = ws.Cell(headerRow, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#007581");
                            cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                            cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        }

                        int row = 5;
                        int no = 1;
                        foreach (var inc in incidents)
                        {
                            // Status mapping
                            string phaseLabel = "Draft";
                            if (inc.Status == 2) phaseLabel = "Assessment";
                            else if (inc.Status == 3) phaseLabel = "Review";
                            else if (inc.Status == 5) phaseLabel = "Approved";
                            else if (inc.Status == 6) phaseLabel = "Activated BCP";
                            else if (inc.Status == 7) phaseLabel = "Resolved";

                            string createdByNIK = (inc.Created_By ?? "").Trim();
                            var CreatedProfile = db.V_Users_Active.AsNoTracking().FirstOrDefault(u => u.NIK.Trim() == createdByNIK);

                            // Severity mapping
                            string s = (inc.Severity ?? "").Trim();
                            string severityLabel = s;
                            if (s == "Critical" || s == "4") severityLabel = "Critical";
                            else if (s == "High" || s == "3") severityLabel = "High";
                            else if (s == "Medium" || s == "2") severityLabel = "Medium";
                            else if (s == "Low" || s == "1") severityLabel = "Low";

                            ws.Cell(row, 1).Value = no;
                            ws.Cell(row, 2).Value = inc.IncidentNo ?? "";
                            ws.Cell(row, 3).Value = inc.Title ?? "";
                            ws.Cell(row, 4).Value = inc.Category ?? "";
                            ws.Cell(row, 5).Value = severityLabel;
                            ws.Cell(row, 6).Value = phaseLabel;
                            ws.Cell(row, 7).Value = inc.Location ?? "";
                            ws.Cell(row, 8).Value = inc.PersonnelStatus ?? "";
                            ws.Cell(row, 9).Value = inc.Created_At.HasValue ? inc.Created_At.Value.ToString("dd MMM yyyy, HH:mm") : "";
                            ws.Cell(row, 10).Value = createdByNIK;
                            ws.Cell(row, 11).Value = CreatedProfile.Name;
                            ws.Cell(row, 12).Value = inc.Description ?? "";

                            // Formatting values
                            ws.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 5).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 9).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 10).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            ws.Cell(row, 11).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                            // Borders
                            for (int col = 1; col <= headers.Length; col++)
                            {
                                ws.Cell(row, col).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                                ws.Cell(row, col).Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromHtml("#e2e8f0");
                            }

                            row++;
                            no++;
                        }

                        // Auto-fit columns
                        ws.Columns().AdjustToContents();
                        ws.Column(11).Width = 40; // Limit description width

                        wb.SaveAs(ms);
                    }

                    ms.Position = 0;
                    var content = ms.ToArray();
                    string fileName = $"BCP_Incident_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Export failed: " + ex.Message;
                return RedirectToAction("IncidentList");
            }
        }

        // GET: HC/BCP/ExportDepartmentMatrixExcel
        public ActionResult ExportDepartmentMatrixExcel(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            try
            {
                using (var db = new BCPConnection())
                {
                    var incident = db.HC_BCP_Incident.FirstOrDefault(x => x.IncidentNo == id);
                    if (incident == null)
                    {
                        TempData["ErrorMessage"] = "Incident not found.";
                        return RedirectToAction("Dashboard");
                    }

                    var departments = db.HC_BCP_Department.Where(x => x.IsActive).OrderBy(x => x.ID).ToList();
                    var areas = db.HC_BCP_Area.ToList();
                    var allCriteria = db.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();

                    var deptAreaMap = new Dictionary<string, List<HC_BCP_Area>>();
                    int totalMatrixCols = 0;
                    foreach (var dept in departments)
                    {
                        var deptAreas = areas.Where(a => a.DepartmentCode == dept.Code).ToList();
                        deptAreaMap[dept.Code] = deptAreas;
                        totalMatrixCols += deptAreas.Any() ? deptAreas.Count : 1;
                    }

                    var assessmentsVM = ComputeIncidentEffectiveAssessments(incident.ID, db);

                    using (var ms = new System.IO.MemoryStream())
                    {
                        using (var wb = new ClosedXML.Excel.XLWorkbook())
                        {
                            var ws = wb.Worksheets.Add("BCP Section Matrix");
                            ws.ShowGridLines = true;

                            int totalTableCols = 3 + totalMatrixCols;

                            // 1. Title Banner
                            ws.Row(1).Height = 30;
                            var titleRange = ws.Range(1, 1, 1, totalTableCols);
                            titleRange.Merge();
                            titleRange.Value = "BCP SECTION DEPARTMENT MATRIX";
                            titleRange.Style.Font.Bold = true;
                            titleRange.Style.Font.FontSize = 14;
                            titleRange.Style.Font.FontColor = XLColor.White;
                            titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00555E");
                            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            // Subtitle Banner
                            ws.Row(2).Height = 20;
                            var subtitleRange = ws.Range(2, 1, 2, totalTableCols);
                            subtitleRange.Merge();
                            subtitleRange.Value = $"Incident: {incident.IncidentNo} - {incident.Title}  |  Category: {incident.Category ?? "-"}  |  Reported: {(incident.Created_At.HasValue ? incident.Created_At.Value.ToString("dd MMM yyyy, HH:mm") : "-")}  |  Exported: {DateTime.Now:dd MMM yyyy, HH:mm}";
                            subtitleRange.Style.Font.Italic = true;
                            subtitleRange.Style.Font.FontSize = 9.5;
                            subtitleRange.Style.Font.FontColor = XLColor.FromHtml("#00555E");
                            subtitleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F3F5");
                            subtitleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            subtitleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            ws.Row(3).Height = 8;

                            // 2. Super Header (Row 4)
                            ws.Row(4).Height = 24;
                            var leftSuper = ws.Range(4, 1, 4, 3);
                            leftSuper.Merge();
                            leftSuper.Value = "CRITERIA / STANDARD / HIGHEST SCORE";
                            leftSuper.Style.Font.Bold = true;
                            leftSuper.Style.Font.FontSize = 10;
                            leftSuper.Style.Font.FontColor = XLColor.FromHtml("#065F46");
                            leftSuper.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
                            leftSuper.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            leftSuper.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            leftSuper.Style.Alignment.Indent = 1;

                            var rightSuper = ws.Range(4, 4, 4, totalTableCols);
                            rightSuper.Merge();
                            rightSuper.Value = "BCP SECTION & AREAS";
                            rightSuper.Style.Font.Bold = true;
                            rightSuper.Style.Font.FontSize = 10;
                            rightSuper.Style.Font.FontColor = XLColor.FromHtml("#065F46");
                            rightSuper.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
                            rightSuper.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            rightSuper.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            // 3. Header Level 1 (Row 5) & Level 2 (Row 6)
                            ws.Row(5).Height = 24;
                            ws.Row(6).Height = 36;

                            // Criteria column (A5:A6)
                            var critH = ws.Range(5, 1, 6, 1);
                            critH.Merge();
                            critH.Value = "Criteria";
                            critH.Style.Font.Bold = true;
                            critH.Style.Font.FontSize = 10;
                            critH.Style.Font.FontColor = XLColor.FromHtml("#334155");
                            critH.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                            critH.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            critH.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            // Standard column (B5:B6)
                            var stdH = ws.Range(5, 2, 6, 2);
                            stdH.Merge();
                            stdH.Value = "Standard";
                            stdH.Style.Font.Bold = true;
                            stdH.Style.Font.FontSize = 10;
                            stdH.Style.Font.FontColor = XLColor.FromHtml("#B45309");
                            stdH.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                            stdH.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            stdH.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            // Highest Score column (C5:C6)
                            var hsH = ws.Range(5, 3, 6, 3);
                            hsH.Merge();
                            hsH.Value = "Highest Score";
                            hsH.Style.Font.Bold = true;
                            hsH.Style.Font.FontSize = 10;
                            hsH.Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
                            hsH.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                            hsH.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            hsH.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            int currCol = 4;
                            int deptIdx = 0;
                            foreach (var dept in departments)
                            {
                                var deptAreas = deptAreaMap[dept.Code];
                                int span = deptAreas.Any() ? deptAreas.Count : 1;
                                var deptBg = (deptIdx % 2 == 0) ? XLColor.FromHtml("#F1F5F9") : XLColor.White;

                                var dRange = ws.Range(5, currCol, 5, currCol + span - 1);
                                if (span > 1) dRange.Merge();
                                dRange.Value = dept.DeptName;
                                dRange.Style.Font.Bold = true;
                                dRange.Style.Font.FontSize = 10;
                                dRange.Style.Font.FontColor = XLColor.FromHtml("#0369A1");
                                dRange.Style.Fill.BackgroundColor = deptBg;
                                dRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                dRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                dRange.Style.Alignment.WrapText = true;

                                if (deptAreas.Any())
                                {
                                    for (int a = 0; a < deptAreas.Count; a++)
                                    {
                                        var aCell = ws.Cell(6, currCol + a);
                                        aCell.Value = deptAreas[a].AreaName;
                                        aCell.Style.Font.Bold = true;
                                        aCell.Style.Font.FontSize = 9;
                                        aCell.Style.Font.FontColor = XLColor.FromHtml("#475569");
                                        aCell.Style.Fill.BackgroundColor = deptBg;
                                        aCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                        aCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                        aCell.Style.Alignment.WrapText = true;
                                    }
                                }
                                else
                                {
                                    var aCell = ws.Cell(6, currCol);
                                    aCell.Value = "-";
                                    aCell.Style.Font.FontSize = 9;
                                    aCell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                                    aCell.Style.Fill.BackgroundColor = deptBg;
                                    aCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    aCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                }

                                currCol += span;
                                deptIdx++;
                            }

                            // 4. Data Rows
                            decimal overallMaxScore = 0m;
                            int dataRow = 7;
                            bool isAltRow = false;

                            foreach (var crit in allCriteria)
                            {
                                isAltRow = !isAltRow;
                                ws.Row(dataRow).Height = 26;
                                var rowBg = isAltRow ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                                // Compute max score for this criteria
                                decimal maxScoreInRow = 0m;
                                foreach (var dept in departments)
                                {
                                    var deptAreas = deptAreaMap[dept.Code];
                                    foreach (var area in deptAreas)
                                    {
                                        var areaAss = assessmentsVM.FirstOrDefault(a => a.AreaID == area.ID);
                                        if (areaAss != null && areaAss.CategoryScores != null)
                                        {
                                            var cs = areaAss.CategoryScores.FirstOrDefault(x => x.CriteriaID == crit.ID);
                                            decimal scoreVal = cs != null ? cs.WeightedScore : 0m;
                                            if (scoreVal > maxScoreInRow) maxScoreInRow = scoreVal;
                                        }
                                    }
                                }

                                if (maxScoreInRow > overallMaxScore) overallMaxScore = maxScoreInRow;

                                // Col 1: Criteria Name
                                var c1 = ws.Cell(dataRow, 1);
                                c1.Value = crit.Name;
                                c1.Style.Font.Bold = true;
                                c1.Style.Font.FontSize = 10;
                                c1.Style.Font.FontColor = XLColor.FromHtml("#334155");
                                c1.Style.Fill.BackgroundColor = rowBg;
                                c1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                c1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                c1.Style.Alignment.WrapText = true;

                                // Col 2: Standard
                                var c2 = ws.Cell(dataRow, 2);
                                c2.Value = crit.Description ?? "";
                                c2.Style.Font.Bold = true;
                                c2.Style.Font.FontSize = 9.5;
                                c2.Style.Font.FontColor = XLColor.FromHtml("#B45309");
                                c2.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFBEB");
                                c2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                c2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                c2.Style.Alignment.WrapText = true;

                                // Col 3: Highest Score
                                var c3 = ws.Cell(dataRow, 3);
                                c3.Value = maxScoreInRow;
                                c3.Style.NumberFormat.Format = "0.00";
                                c3.Style.Font.Bold = true;
                                c3.Style.Font.FontSize = 10;
                                c3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                c3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                                if (maxScoreInRow >= 4.50m) { c3.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE4E6"); c3.Style.Font.FontColor = XLColor.FromHtml("#BE123C"); }
                                else if (maxScoreInRow >= 3.50m) { c3.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEDD5"); c3.Style.Font.FontColor = XLColor.FromHtml("#C2410C"); }
                                else if (maxScoreInRow >= 2.50m) { c3.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF9C3"); c3.Style.Font.FontColor = XLColor.FromHtml("#A16207"); }
                                else if (maxScoreInRow >= 1.50m) { c3.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE"); c3.Style.Font.FontColor = XLColor.FromHtml("#1D4ED8"); }
                                else { c3.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5"); c3.Style.Font.FontColor = XLColor.FromHtml("#047857"); }

                                // Area Columns
                                int colPointer = 4;
                                int bDeptIdx = 0;
                                foreach (var dept in departments)
                                {
                                    var deptAreas = deptAreaMap[dept.Code];
                                    var colBandBg = (bDeptIdx % 2 == 0) ? (isAltRow ? XLColor.FromHtml("#F1F5F9") : XLColor.FromHtml("#F8FAFC")) : rowBg;

                                    if (deptAreas.Any())
                                    {
                                        foreach (var area in deptAreas)
                                        {
                                            var cell = ws.Cell(dataRow, colPointer);
                                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                                            var areaAss = assessmentsVM.FirstOrDefault(a => a.AreaID == area.ID);
                                            if (areaAss != null && areaAss.CategoryScores != null)
                                            {
                                                var cs = areaAss.CategoryScores.FirstOrDefault(x => x.CriteriaID == crit.ID);
                                                decimal scoreVal = cs != null ? cs.WeightedScore : 0m;

                                                cell.Value = scoreVal;
                                                cell.Style.NumberFormat.Format = "0.00";
                                                cell.Style.Font.Bold = true;
                                                cell.Style.Font.FontSize = 10;

                                                if (scoreVal >= 4.50m) { cell.Style.Font.FontColor = XLColor.FromHtml("#DC2626"); cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2"); }
                                                else if (scoreVal >= 3.50m) { cell.Style.Font.FontColor = XLColor.FromHtml("#EA580C"); cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED"); }
                                                else if (scoreVal >= 2.50m) { cell.Style.Font.FontColor = XLColor.FromHtml("#CA8A04"); cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEFCE8"); }
                                                else if (scoreVal >= 1.50m) { cell.Style.Font.FontColor = XLColor.FromHtml("#2563EB"); cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF"); }
                                                else { cell.Style.Font.FontColor = XLColor.FromHtml("#059669"); cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5"); }
                                            }
                                            else
                                            {
                                                cell.Value = "-";
                                                cell.Style.Font.FontSize = 9;
                                                cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                                                cell.Style.Fill.BackgroundColor = colBandBg;
                                            }
                                            colPointer++;
                                        }
                                    }
                                    else
                                    {
                                        var cell = ws.Cell(dataRow, colPointer);
                                        cell.Value = "-";
                                        cell.Style.Font.FontSize = 9;
                                        cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                                        cell.Style.Fill.BackgroundColor = colBandBg;
                                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                        colPointer++;
                                    }

                                    bDeptIdx++;
                                }

                                dataRow++;
                            }

                            // 5. Summary / Overall Highest Score Row
                            int summaryRow = dataRow;
                            ws.Row(summaryRow).Height = 30;

                            XLColor sumBg;
                            XLColor sumFont;
                            if (overallMaxScore >= 4.50m) { sumBg = XLColor.FromHtml("#FFE4E6"); sumFont = XLColor.FromHtml("#9F1239"); }
                            else if (overallMaxScore >= 3.50m) { sumBg = XLColor.FromHtml("#FFEDD5"); sumFont = XLColor.FromHtml("#9A3412"); }
                            else if (overallMaxScore >= 2.50m) { sumBg = XLColor.FromHtml("#FEF9C3"); sumFont = XLColor.FromHtml("#854D0E"); }
                            else if (overallMaxScore >= 1.50m) { sumBg = XLColor.FromHtml("#DBEAFE"); sumFont = XLColor.FromHtml("#1E40AF"); }
                            else { sumBg = XLColor.FromHtml("#D1FAE5"); sumFont = XLColor.FromHtml("#065F46"); }

                            var sumLabelRange = ws.Range(summaryRow, 1, summaryRow, 2);
                            sumLabelRange.Merge();
                            sumLabelRange.Value = "OVERALL HIGHEST SCORE";
                            sumLabelRange.Style.Font.Bold = true;
                            sumLabelRange.Style.Font.FontSize = 10.5;
                            sumLabelRange.Style.Font.FontColor = sumFont;
                            sumLabelRange.Style.Fill.BackgroundColor = sumBg;
                            sumLabelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            sumLabelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            sumLabelRange.Style.Alignment.Indent = 1;

                            var sumVal = ws.Cell(summaryRow, 3);
                            sumVal.Value = overallMaxScore;
                            sumVal.Style.NumberFormat.Format = "0.00";
                            sumVal.Style.Font.Bold = true;
                            sumVal.Style.Font.FontSize = 11.5;
                            sumVal.Style.Font.FontColor = sumFont;
                            sumVal.Style.Fill.BackgroundColor = sumBg;
                            sumVal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            sumVal.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            for (int c = 4; c <= totalTableCols; c++)
                            {
                                var cell = ws.Cell(summaryRow, c);
                                cell.Value = "";
                                cell.Style.Fill.BackgroundColor = sumBg;
                            }

                            // 6. Borders
                            for (int r = 4; r <= summaryRow; r++)
                            {
                                for (int c = 1; c <= totalTableCols; c++)
                                {
                                    var cell = ws.Cell(r, c);
                                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                                }
                            }

                            // 7. Column Widths
                            ws.Column(1).Width = 32; // Criteria
                            ws.Column(2).Width = 24; // Standard
                            ws.Column(3).Width = 15; // Highest Score
                            for (int c = 4; c <= totalTableCols; c++)
                            {
                                ws.Column(c).Width = 18; // Area columns
                            }

                            // Freeze Panes
                            ws.SheetView.FreezeRows(6);
                            ws.SheetView.FreezeColumns(3);

                            wb.SaveAs(ms);
                        }

                        ms.Position = 0;
                        var content = ms.ToArray();
                        string fileName = $"BCP_Section_Department_Matrix_{incident.IncidentNo}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Export Matrix failed: " + ex.Message;
                return RedirectToAction("AssessmentReview", new { id = id });
            }
        }

        // GET: HC/BCP/IncidentCreate
        public ActionResult IncidentCreate()
        {
            string userId = User.Identity.GetUserId() ?? User.Identity.Name;
            if (!string.IsNullOrEmpty(userId) && userId.Contains("\\"))
            {
                userId = userId.Split('\\').Last();
            }
            userId = (userId ?? "").Trim();
            
            // Logically finding the user in BCP Organization
            var userBcp = bcpDb.HC_BCP_Organization.AsNoTracking()
                .ToList() // Fetch to memory to do more flexible string comparison if needed, or just keep it SQL
                .FirstOrDefault(x => 
                    (x.NIK != null && x.NIK.Trim().Equals(userId, StringComparison.OrdinalIgnoreCase)) 
                    && x.IsActive);

            if (userBcp == null)
            {
                // Jika tidak terdaftar, alihkan ke dashboard dengan peringatan
                TempData["ErrorMessage"] = "Akses ditolak. NIK (" + (userId ?? "N/A") + ") ttidak terdaftar dalam organisasi BCP.";
                return RedirectToAction("Dashboard");
            }

            ViewBag.Title = "Create Incident Report";
            ViewBag.UserBcpRole = (userBcp.PositionName ?? "Member") + " " + (userBcp.DepartmentName ?? "");
            
            // Filter areas by user's department
            var deptName = userBcp.DepartmentName;
            var deptNiks = bcpDb.HC_BCP_Organization.Where(x => x.DepartmentName == deptName).Select(x => x.NIK).ToList();
            var deptAreaIds = bcpDb.HC_BCP_User_Area.Where(x => deptNiks.Contains(x.NIK)).Select(x => x.AreaID).Distinct().ToList();
            
            ViewBag.Areas = bcpDb.HC_BCP_Area
                                .Where(x => x.IsActive && deptAreaIds.Contains(x.ID))
                                .OrderBy(x => x.AreaName)
                                .ToList();
            
            return View();
        }

        [HttpPost]
        public ActionResult SubmitIncident(string Title, string Location, string Category, string Severity, string Description, string InfrastructureImpact, string PersonnelStatus, string ImmediateActions, int? SourceAreaID, IEnumerable<System.Web.HttpPostedFileBase> Photos)
        {
            string userId = User.Identity.GetUserId() ?? User.Identity.Name;
            if (!string.IsNullOrEmpty(userId) && userId.Contains("\\"))
            {
                userId = userId.Split('\\').Last();
            }
            userId = (userId ?? "").Trim();

            // 1. Validasi BCP Organization (RBAC)
            var userBcp = bcpDb.HC_BCP_Organization
                .FirstOrDefault(x => 
                    x.NIK.Trim() == userId 
                    && x.IsActive);

            if (userBcp == null)
            {
                // Jika tidak tercatat di organisasi BCP
                return Json(new { status = "error", title = "Access Denied", message = "NIK Anda (" + userId + ") tidak terdaftar sebagai komite aktif." });
            }

            try
            {
                // 2. Generate Incident No (BCP-YYMM-XXXX)
                string dateTag = DateTime.Now.ToString("yyMM");
                var latest = bcpDb.HC_BCP_Incident
                                .Where(x => x.IncidentNo.StartsWith("BCP-" + dateTag))
                                .OrderByDescending(x => x.IncidentNo)
                                .FirstOrDefault();
                int nextSeq = 1;
                if (latest != null && !string.IsNullOrEmpty(latest.IncidentNo))
                {
                    string[] parts = latest.IncidentNo.Split('-');
                    if (parts.Length == 3)
                    {
                        int.TryParse(parts[2], out nextSeq);
                        nextSeq++;
                    }
                }
                string incidentNo = $"BCP-{dateTag}-{nextSeq:D4}";

                // 3. Create Record
                var newIncident = new HC_BCP_Incident
                {
                    IncidentNo = incidentNo,
                    Title = Title,
                    Category = Category,
                    Severity = Severity,
                    Description = Description,
                    Location = Location ?? "Corporate Plant",
                    InfrastructureImpact = InfrastructureImpact,
                    PersonnelStatus = PersonnelStatus,
                    ImmediateActions = ImmediateActions,
                    Status = 2, // Move to Assessment Phase immediately
                    Created_At = DateTime.Now,
                    Created_By = userBcp.NIK.Trim(), // Use the validated NIK from database
                    ForwardedToPresdir = false,
                    SourceAreaID = SourceAreaID
                };

                bcpDb.HC_BCP_Incident.Add(newIncident);
                bcpDb.SaveChanges();

                // NOTE: Source area is required to fill in their own assessment via AssessmentFill,
                // just like any other area. No auto-creation here.

                // 3.5. Snapshot Committee Members (Ensures historical integrity)
                var activeOrg = bcpDb.HC_BCP_Organization.Where(x => x.IsActive).ToList();
                foreach (var member in activeOrg)
                {
                    string parentNik = null;
                    if (member.ParentID.HasValue)
                    {
                        var parent = activeOrg.FirstOrDefault(p => p.ID == member.ParentID.Value);
                        if (parent != null) parentNik = parent.NIK.Trim();
                    }

                    bcpDb.HC_BCP_Incident_Member.Add(new HC_BCP_Incident_Member
                    {
                        IncidentID = newIncident.ID,
                        NIK = member.NIK.Trim(),
                        Name = member.Name,
                        BCP_Role = member.BCP_Role,
                        DepartmentName = member.DepartmentName,
                        ParentNIK = parentNik,
                        Created_At = DateTime.Now
                    });
                }
                bcpDb.SaveChanges();

                // 4. Handle Photo Uploads
                if (Photos != null)
                {
                    string folderPath = Server.MapPath("~/Files/HC/BCP/Incidents/" + newIncident.ID + "/");
                    if (!System.IO.Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(folderPath);
                    }

                    foreach (var photo in Photos)
                    {
                        if (photo != null && photo.ContentLength > 0)
                        {
                            string fileName = System.IO.Path.GetFileName(photo.FileName);
                            string path = System.IO.Path.Combine(folderPath, fileName);
                            photo.SaveAs(path);

                            bcpDb.HC_BCP_Incident_Photo.Add(new HC_BCP_Incident_Photo
                            {
                                IncidentID = newIncident.ID,
                                FileName = fileName.Length > 255 ? fileName.Substring(0, 250) : fileName,
                                FilePath = "~/Files/HC/BCP/Incidents/" + newIncident.ID + "/" + fileName,
                                Uploaded_At = DateTime.Now,
                                Uploaded_By = userBcp.NIK.Trim()
                            });
                        }
                    }
                    bcpDb.SaveChanges();
                }

                // 5. Send Email Notification
                // Scenario: Notify all other BCP Members
                // Fix: Split context query and handle NIK trimming/case-insensitivity robusly
                var activeBcpFromDb = bcpDb.HC_BCP_Organization
                                      .AsNoTracking()
                                      .Where(x => x.IsActive)
                                      .ToList();

                var senderNik = userBcp.NIK.Trim().ToUpper();
                var bcpMemberNiks = activeBcpFromDb
                                    .Select(x => x.NIK.Trim().ToUpper())
                                    .Where(n => n != senderNik)
                                    .ToList();

                // Memory-side filtering to ensure 100% match regardless of DB collation/formatting
                var allPortalUsers = db.V_Users_Active.AsNoTracking().ToList();
                var notificationRecipients = allPortalUsers
                                              .Where(u => !string.IsNullOrEmpty(u.NIK) && bcpMemberNiks.Contains(u.NIK.Trim().ToUpper()))
                                              .Select(u => new
                                              {
                                                  NIK = u.NIK.Trim(),
                                                  u.Email,
                                                  u.Name
                                              }).ToList();

                // Logging for developer/user to verify
                int recipientsFound = notificationRecipients.Count;
                int membersInBcpTable = bcpMemberNiks.Count;

                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "BCP Command Center");
                var password = "100%NGKbusi!";
                
                var smtp = new SmtpClient
                {
                    Host = "ngkbusi.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, password)
                };

                int emailsSent = 0;
                string debugEmails = "";
                string lastError = "";

                // Get dynamic base URL for local or production testing
                var baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                var detailUrl = baseUrl + Url.Action("IncidentDetail", "BCP", new { area = "HC", id = incidentNo });
                var dashboardUrl = baseUrl + Url.Action("Dashboard", "BCP", new { area = "HC" });

                foreach(var recipient in notificationRecipients)
                {
                    if(!string.IsNullOrEmpty(recipient.Email))
                    {
                        // Logic: Adjust target URL based on role (Committee vs Others)
                        var recipientBcp = activeBcpFromDb.FirstOrDefault(x => x.NIK.Trim().ToUpper() == recipient.NIK.Trim().ToUpper());
                        var targetUrl = (recipientBcp != null && recipientBcp.BCP_Role != "Member") ? dashboardUrl : detailUrl;
                        var buttonText = (recipientBcp != null && recipientBcp.BCP_Role != "Member") ? "Access BCP Dashboard" : "View Incident Detail";

                        debugEmails += recipient.Email + "; ";
                        try 
                        {
                            using (var mess = new MailMessage())
                            {
                                mess.From = senderEmail;
                                mess.To.Add(recipient.Email);
                                mess.Subject = $"[EMERGENCY] BCP Alert: {Title} ({incidentNo})";
                                mess.IsBodyHtml = true;
                                mess.Body = $@"
                                    <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; padding: 40px 20px; border-radius: 8px;"">
                                        <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05); border: 1px solid #e0f3f5;"">
                                            <div style=""background-color: #e11d48; padding: 30px; text-align: center;"">
                                                <h1 style=""color: #ffffff; margin: 0; font-size: 24px; text-transform: uppercase; letter-spacing: 2px;"">BCP ALERT</h1>
                                                <p style=""color: #ffdce0; margin: 10px 0 0 0; font-size: 14px;"">Emergency Activation Requested</p>
                                            </div>
                                            
                                            <div style=""padding: 40px 30px;"">
                                                <p style=""color: #334155; font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">
                                                    Hello <strong>{recipient.Name}</strong>,<br><br>
                                                    A critical incident has been reported that requires immediate BCP Command Center attention.
                                                </p>
                                                
                                                <div style=""background-color: #e0f3f5; border-left: 4px solid #00555e; padding: 15px 20px; margin: 25px 0; border-radius: 4px;"">
                                                    <p style=""margin: 0 0 8px 0; font-size: 14px; color: #00555e;""><strong>Incident No:</strong> {incidentNo}</p>
                                                    <p style=""margin: 0 0 8px 0; font-size: 14px; color: #00555e;""><strong>Title:</strong> {Title}</p>
                                                    <p style=""margin: 0 0 8px 0; font-size: 14px; color: #00555e;""><strong>Category:</strong> {Category}</p>
                                                </div>

                                                <p style=""color: #64748b; font-size: 14px; margin-bottom: 30px;"">
                                                    Please log in to the BCP Dashboard to perform an initial assessment and review the documentation.
                                                </p>
                                                
                                                <div style=""text-align: center; margin-bottom: 20px;"">
                                                    <a href=""{targetUrl}"" 
                                                       style=""background-color: #e11d48; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 30px; display: inline-block; font-weight: bold; text-transform: uppercase; font-size: 13px; letter-spacing: 1px;"">
                                                        {buttonText}
                                                    </a>
                                                </div>
                                            </div>
                                            
                                            <div style=""background-color: #f1f5f9; padding: 20px; text-align: center; border-top: 1px solid #e2e8f0;"">
                                                <p style=""margin: 0; font-size: 12px; color: #94a3b8;"">
                                                    Sent by NGK BCP Command Center Automated System<br>
                                                    &copy; {DateTime.Now.Year} PT Niterra Mobility Indonesia
                                                </p>
                                            </div>
                                        </div>
                                    </div>";

                                if (_enableEmail)
                                {
                                    smtp.Send(mess);
                                }
                                emailsSent++;
                                
                                bcpDb.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                                {
                                    IncidentID = newIncident.ID,
                                    RecipientNIK = recipient.NIK,
                                    Channel = "Email",
                                    Message = $"Incident {incidentNo} reported.",
                                    IsSent = true,
                                    SentAt = DateTime.Now
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            lastError = ex.Message;
                            bcpDb.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                            {
                                IncidentID = newIncident.ID,
                                RecipientNIK = recipient.NIK,
                                Channel = "Email",
                                Message = $"Incident {incidentNo} reported.",
                                IsSent = false,
                                SentAt = DateTime.Now,
                                ErrorMessage = ex.Message
                            });
                        }
                    }
                }
                bcpDb.SaveChanges();

                string finalMsg = $"Incident {incidentNo} successfully reported. {emailsSent}/{recipientsFound} members notified.";
                if (recipientsFound > 0)
                {
                    if (emailsSent == 0)
                        finalMsg += " ERROR: SMTP failed. Last error: " + lastError;
                    
                    finalMsg += " (Target: " + debugEmails.TrimEnd(';', ' ') + ")";
                }

                return Json(new { status = "success", title = "Incident Reported!", message = finalMsg, incidentNo = incidentNo });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                return Json(new { status = "error", title = "Penyimpanan Gagal", message = "Validation Error: " + fullErrorMessage });
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null) {
                    msg += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null) {
                         msg += " | Root: " + ex.InnerException.InnerException.Message;
                    }
                }
                return Json(new { status = "error", title = "Sistem Error", message = "Terjadi kesalahan sistem: " + msg });
            }
        }

        // GET: HC/BCP/IncidentDetail
        public ActionResult IncidentDetail(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            using (var bcpDb = new BCPConnection())
            {
                var incident = bcpDb.HC_BCP_Incident
                                .AsNoTracking()
                                .FirstOrDefault(x => x.IncidentNo == id);

                if (incident == null) return HttpNotFound();

                // AUTO-FIX: If status is 1, bump to 2 (Assessment Phase) for visibility
                if (incident.Status == 1) {
                    var dbIncident = bcpDb.HC_BCP_Incident.Find(incident.ID);
                    if (dbIncident != null) {
                        dbIncident.Status = 2;
                        bcpDb.SaveChanges();
                        incident.Status = 2; // Update local object too
                    }
                }

                var photos = bcpDb.HC_BCP_Incident_Photo
                                .AsNoTracking()
                                .Where(x => x.IncidentID == incident.ID)
                                .ToList();

                // Get Current User BCP Role & Permission from database
                string curNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (!string.IsNullOrEmpty(curNik) && curNik.Contains("\\")) curNik = curNik.Split('\\').Last();
                curNik = (curNik ?? "").Trim();
                
                var userBcp = bcpDb.HC_BCP_Organization.AsNoTracking().FirstOrDefault(x => x.NIK.Trim() == curNik && x.IsActive);
                ViewBag.CurrentUserRole = userBcp != null ? userBcp.PositionName : "";

                bool canReview = false;
                if (userBcp != null)
                {
                    // Evaluate DB-driven review permission
                    bool hasPermission = bcpDb.HC_BCP_Role_Permission.Any(p => p.IsAllowedReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK)));

                    // Also allow the incident reporter (Created_By) to review — compare trimmed, case-insensitive
                    bool isReporter = false;
                    if (!string.IsNullOrEmpty(incident.Created_By) && !string.IsNullOrEmpty(userBcp.NIK))
                    {
                        isReporter = incident.Created_By.Trim().Equals(userBcp.NIK.Trim(), StringComparison.OrdinalIgnoreCase);
                    }

                    canReview = hasPermission || isReporter;
                }

                ViewBag.CanReview = canReview;

                ViewBag.Incident = incident;
                ViewBag.Photos = photos;
                
                // Get reporter name
                using (var dbConnection = new DefaultConnection()) {
                    var reporter = dbConnection.V_Users_Active.AsNoTracking().FirstOrDefault(u => u.NIK.Trim() == incident.Created_By.Trim());
                    ViewBag.ReporterName = reporter != null ? reporter.Name : incident.Created_By;
                }

                // Get Committee Members for this specific incident (Snapshot)
                var committee = bcpDb.HC_BCP_Incident_Member
                                .AsNoTracking()
                                .Where(x => x.IncidentID == incident.ID && x.BCP_Role != "Presdir")
                                .ToList();

                // Fallback for legacy incidents without snapshots
                if (committee.Count == 0)
                {
                    var liveOrg = bcpDb.HC_BCP_Organization
                                  .AsNoTracking()
                                  .Where(x => x.IsActive && x.BCP_Role != "Presdir")
                                  .ToList();
                    
                    committee = liveOrg.Select(o => new HC_BCP_Incident_Member {
                        NIK = o.NIK,
                        Name = o.Name,
                        BCP_Role = o.BCP_Role,
                        DepartmentName = o.DepartmentName,
                        ParentNIK = (o.ParentID != null ? bcpDb.HC_BCP_Organization.FirstOrDefault(p => p.ID == o.ParentID)?.NIK : null)
                    }).ToList();
                }

                var assessments = bcpDb.HC_BCP_Assessment
                                   .AsNoTracking()
                                   .Where(x => x.IncidentID == incident.ID)
                                   .ToList();

                // NEW LOGIC: Area-based Status for Sidebar
                var allAreas = bcpDb.HC_BCP_Area.Where(x => x.IsActive).OrderBy(x => x.AreaName).ToList();
                var areaStatus = new List<BCPAreaStatus>();
                
                foreach(var area in allAreas) {
                    var ass = assessments.FirstOrDefault(x => x.AreaID == area.ID);
                    areaStatus.Add(new BCPAreaStatus {
                        AreaID = area.ID,
                        AreaName = area.AreaName,
                        IsAssessed = ass != null,
                        AssessorName = ass != null ? ass.AssessorName : "Pending",
                        Score = ass != null ? (ass.TotalScore ?? 0) : 0,
                        IsSource = incident.SourceAreaID == area.ID
                    });
                }
                ViewBag.AreaStatus = areaStatus;
            }

            return View();
        }

        // GET: HC/BCP/AssessmentFill
        public ActionResult AssessmentFill(string id, int? areaId)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            using (var bcpDb = new BCPConnection())
            {
                var incident = bcpDb.HC_BCP_Incident.FirstOrDefault(x => x.IncidentNo == id);
                if (incident == null) return HttpNotFound();

                // 1. Identification
                string curNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (!string.IsNullOrEmpty(curNik) && curNik.Contains("\\")) curNik = curNik.Split('\\').Last();
                curNik = (curNik ?? "").Trim();

                // 2. Load Assigned Areas
                var myAreaIds = bcpDb.HC_BCP_User_Area.Where(x => x.NIK == curNik).Select(x => x.AreaID).ToList();
                var myAreas = bcpDb.HC_BCP_Area.Where(x => x.IsActive && myAreaIds.Contains(x.ID)).ToList();
                ViewBag.MyAreas = myAreas;

                if (areaId.HasValue)
                {
                    var selectedArea = myAreas.FirstOrDefault(x => x.ID == areaId.Value);
                    if (selectedArea == null) {
                        TempData["ErrorMessage"] = "Anda tidak memiliki akses ke area ini.";
                        return RedirectToAction("AssessmentFill", new { id = id });
                    }
                    ViewBag.SelectedArea = selectedArea;
                    
                    // Load Existing Assessment for this Incident + Area
                    var existing = bcpDb.HC_BCP_Assessment.Include(x => x.Details)
                                    .FirstOrDefault(x => x.IncidentID == incident.ID && x.AreaID == areaId.Value);
                    ViewBag.ExistingAssessment = existing;
                    
                    if (existing != null) {
                        ViewBag.ExistingAnswers = bcpDb.HC_BCP_Assessment_Answer.Where(x => x.AssessmentID == existing.ID).ToList();
                    }
                }

                ViewBag.Incident = incident;
                ViewBag.Criteria = bcpDb.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
                
                var rawQuestions = bcpDb.HC_BCP_Assessment_Question.Include(x => x.Choices).Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
                List<HC_BCP_Assessment_Question> filteredQuestions;

                if (areaId.HasValue)
                {
                    var selectedAreaObj = myAreas.FirstOrDefault(x => x.ID == areaId.Value);
                    string areaDept = selectedAreaObj != null ? (selectedAreaObj.DepartmentCode ?? "").Trim() : "";
                    var userBcp = bcpDb.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == curNik && x.IsActive);
                    string userDept = userBcp != null ? (userBcp.DepartmentCode ?? "").Trim() : "";

                    filteredQuestions = rawQuestions.Where(q => {
                        // Jika AllowedDepartments kosong/null, pertanyaan terbuka untuk SEMUA department
                        if (string.IsNullOrWhiteSpace(q.AllowedDepartments))
                            return true;

                        var allowedList = q.AllowedDepartments
                            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(d => d.Trim())
                            .ToList();

                        if (!allowedList.Any())
                            return true;

                        // Cocokkan dengan DepartmentCode dari selectedArea
                        if (!string.IsNullOrEmpty(areaDept) && allowedList.Any(d => string.Equals(d, areaDept, StringComparison.OrdinalIgnoreCase)))
                            return true;

                        // Fallback jika area belum memiliki DepartmentCode terisi, cocokkan dengan DepartmentCode user
                        if (string.IsNullOrEmpty(areaDept) && !string.IsNullOrEmpty(userDept) && allowedList.Any(d => string.Equals(d, userDept, StringComparison.OrdinalIgnoreCase)))
                            return true;

                        return false;
                    }).ToList();
                }
                else
                {
                    filteredQuestions = rawQuestions;
                }

                ViewBag.Questions = filteredQuestions;
                
                // Status for all areas for this incident
                var areaAssessments = bcpDb.HC_BCP_Assessment
                                       .Where(x => x.IncidentID == incident.ID)
                                       .ToList();
                ViewBag.AreaAssessments = areaAssessments;
            }

            ViewBag.Title = "Impact Assessment";
            ViewBag.IncidentNo = id;
            ViewBag.NavHide = true;
            return View();
        }

        [HttpPost]
        public ActionResult SubmitAssessment(int IncidentID, int AreaID, bool IsImpacted, string ImpactDescription, List<int> CriteriaID, List<string> Notes, List<int> QuestionID, List<int> ChoiceID)
        {
            try
            {
                string userId = User.Identity.GetUserId() ?? User.Identity.Name;
                if (!string.IsNullOrEmpty(userId) && userId.Contains("\\")) userId = userId.Split('\\').Last();
                string currUserNik = (userId ?? "").Trim();
                
                using (var bcpRepo = new BCPConnection())
                {
                    var incident = bcpRepo.HC_BCP_Incident.Find(IncidentID);
                    if (incident == null) return Json(new { status = "error", message = "Incident not found." });

                    var userBcp = bcpRepo.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik.Trim() && x.IsActive);
                    if (userBcp == null) return Json(new { status = "error", message = "You are not authorized." });

                    // 1. Create or Update Assessment by AreaID
                    var assessment = bcpRepo.HC_BCP_Assessment.FirstOrDefault(x => x.IncidentID == IncidentID && x.AreaID == AreaID);
                    bool isNew = assessment == null;
                    if (isNew)
                    {
                        assessment = new HC_BCP_Assessment
                        {
                            IncidentID = IncidentID,
                            AreaID = AreaID,
                            AssessorNIK = currUserNik,
                            AssessorName = userBcp.Name,
                            DepartmentName = userBcp.DepartmentName,
                            Created_At = DateTime.Now,
                            Status = 1
                        };
                        bcpRepo.HC_BCP_Assessment.Add(assessment);
                    }
                    else
                    {
                        assessment.LastUpdatedBy = currUserNik;
                        assessment.LastUpdatedAt = DateTime.Now;
                    }

                    assessment.IsImpacted = IsImpacted;
                    assessment.ImpactDescription = ImpactDescription;
                    assessment.Submitted_At = DateTime.Now;
                    assessment.Status = 1; // Submitted

                    bcpRepo.SaveChanges(); // Get ID for details

                    // 2. Clear old Answers and Details first
                    var oldAnswers = bcpRepo.HC_BCP_Assessment_Answer.Where(x => x.AssessmentID == assessment.ID).ToList();
                    bcpRepo.HC_BCP_Assessment_Answer.RemoveRange(oldAnswers);

                    var oldDetails = bcpRepo.HC_BCP_Assessment_Detail.Where(x => x.AssessmentID == assessment.ID).ToList();
                    bcpRepo.HC_BCP_Assessment_Detail.RemoveRange(oldDetails);

                    bcpRepo.SaveChanges(); // save deletes first

                    if (IsImpacted)
                    {
                        var allChoices = bcpRepo.HC_BCP_Assessment_Question_Choice.ToList();
                        var allActiveQuestions = bcpRepo.HC_BCP_Assessment_Question.Where(q => q.IsActive).ToList();
                        var submittedQuestionIds = new HashSet<int>();

                        if (QuestionID != null && ChoiceID != null)
                        {
                            for (int i = 0; i < QuestionID.Count; i++)
                            {
                                int qid = QuestionID[i];
                                int chid = (ChoiceID.Count > i) ? ChoiceID[i] : 0;
                                if (chid > 0)
                                {
                                    var choice = allChoices.FirstOrDefault(c => c.ID == chid);
                                    bcpRepo.HC_BCP_Assessment_Answer.Add(new HC_BCP_Assessment_Answer
                                    {
                                        AssessmentID = assessment.ID,
                                        QuestionID = qid,
                                        Score = choice != null ? choice.ScoreValue : 0,
                                        SelectedOptionText = choice != null ? choice.ChoiceText : "0"
                                    });
                                    submittedQuestionIds.Add(qid);
                                }
                            }
                        }

                        // Untuk semua pertanyaan aktif yang tidak disubmit (misal dikhususkan untuk department lain),
                        // otomatis simpan record jawaban dengan nilai Score = 0 (bukan null)
                        var unsubmittedQuestions = allActiveQuestions.Where(q => !submittedQuestionIds.Contains(q.ID)).ToList();
                        foreach (var unQ in unsubmittedQuestions)
                        {
                            bcpRepo.HC_BCP_Assessment_Answer.Add(new HC_BCP_Assessment_Answer
                            {
                                AssessmentID = assessment.ID,
                                QuestionID = unQ.ID,
                                Score = 0,
                                SelectedOptionText = "0"
                            });
                        }

                        bcpRepo.SaveChanges(); // save answers before calculating detail scores

                        // 3. Save Details & Calculate Weighted Score using Question-level weights
                        decimal totalWeighted = 0;
                        var allCriteria = bcpRepo.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).ToList();

                        // Get fresh answers
                        var currentAnswers = bcpRepo.HC_BCP_Assessment_Answer.Where(x => x.AssessmentID == assessment.ID).ToList();
                        var allQuestions = bcpRepo.HC_BCP_Assessment_Question.Where(q => q.IsActive).ToList();

                        foreach (var crit in allCriteria)
                        {
                            int cid = crit.ID;
                            string n = "";
                            if (CriteriaID != null && CriteriaID.Contains(cid))
                            {
                                int idx = CriteriaID.IndexOf(cid);
                                n = (Notes != null && Notes.Count > idx) ? Notes[idx] : "";
                            }

                            var qForCrit = allQuestions.Where(q => q.CriteriaID == cid).ToList();
                            var ansForCrit = currentAnswers.Where(a => qForCrit.Select(q => q.ID).Contains(a.QuestionID)).ToList();

                            // Compute effective weighted score per question
                            decimal criteriaSumOfWeighted = 0;
                            decimal totalQWeight = 0;

                            foreach (var ans in ansForCrit)
                            {
                                var q = qForCrit.FirstOrDefault(x => x.ID == ans.QuestionID);
                                if (q != null)
                                {
                                    decimal qWeight = (q.Weight.HasValue && q.Weight.Value > 0) ? q.Weight.Value : 100m;
                                    decimal effectiveScore = (decimal)(ans.Score ?? 0) * qWeight;
                                    criteriaSumOfWeighted += effectiveScore;
                                    totalQWeight += qWeight;
                                }
                            }

                            decimal avgScore = totalQWeight > 0 ? (criteriaSumOfWeighted / totalQWeight) : 0m;
                            int? maxScore = (int)Math.Round(avgScore);

                            // Contribution to total = criteriaScore × criteriaWeight / 100
                            totalWeighted += avgScore * (crit.Weight ?? 0) / 100m;

                            bcpRepo.HC_BCP_Assessment_Detail.Add(new HC_BCP_Assessment_Detail
                            {
                                AssessmentID = assessment.ID,
                                CriteriaID = cid,
                                MaxScore = maxScore,
                                AverageScore = avgScore,
                                Notes = n
                            });
                        }

                        assessment.TotalScore = totalWeighted;
                    }
                    else
                    {
                        assessment.TotalScore = 0;
                        assessment.ImpactDescription = ""; // Clear description if not impacted

                        var allActiveQuestions = bcpRepo.HC_BCP_Assessment_Question.Where(q => q.IsActive).ToList();
                        foreach (var unQ in allActiveQuestions)
                        {
                            bcpRepo.HC_BCP_Assessment_Answer.Add(new HC_BCP_Assessment_Answer
                            {
                                AssessmentID = assessment.ID,
                                QuestionID = unQ.ID,
                                Score = 0,
                                SelectedOptionText = "0"
                            });
                        }

                        var allCriteria = bcpRepo.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).ToList();
                        foreach (var crit in allCriteria)
                        {
                            bcpRepo.HC_BCP_Assessment_Detail.Add(new HC_BCP_Assessment_Detail
                            {
                                AssessmentID = assessment.ID,
                                CriteriaID = crit.ID,
                                MaxScore = 0,
                                AverageScore = 0m,
                                Notes = ""
                            });
                        }
                    }


                    // Auto-transition to Review (Status 3) if ALL areas are assessed
                    var totalAreasCount = bcpRepo.HC_BCP_Area.Count(x => x.IsActive);
                    var assessedAreasCount = bcpRepo.HC_BCP_Assessment.Where(x => x.IncidentID == IncidentID && x.Status >= 1).Select(x => x.AreaID).Distinct().Count();

                    // If all areas assessed, move to status 3
                    if (assessedAreasCount >= totalAreasCount)
                    {
                        incident.Status = 3; // Ready for DGM Review
                    }
                    else if (incident.Status < 2)
                    {
                        incident.Status = 2; // Ensure at least in assessment phase
                    }

                    bcpRepo.SaveChanges();

                    // Synchronize & propagate inherited highest scores across all assessments of this incident
                    SyncIncidentAssessmentDetails(IncidentID, bcpRepo);

                    return Json(new { status = "success", message = "Assessment " + (isNew ? "submitted" : "updated") + " successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: HC/BCP/AssessmentScore
        public ActionResult AssessmentScore(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            using (var bcpDb = new BCPConnection())
            {
                var incident = bcpDb.HC_BCP_Incident.FirstOrDefault(x => x.IncidentNo == id);
                if (incident == null) return HttpNotFound();

                var assessments = bcpDb.HC_BCP_Assessment.Include(x => x.Details).Where(x => x.IncidentID == incident.ID && x.Status >= 1).ToList();
                var criteria = bcpDb.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).ToList();

                ViewBag.Incident = incident;
                ViewBag.Assessments = assessments;
                ViewBag.Criteria = criteria;
                ViewBag.IncidentNo = id;
            }

            ViewBag.Title = "Assessment Scores";
            return View();
        }

        // GET: HC/BCP/AssessmentReview
        public ActionResult AssessmentReview(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            using (var db = new BCPConnection())
            {
                // Get Current User NIK
                string curNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (!string.IsNullOrEmpty(curNik) && curNik.Contains("\\")) curNik = curNik.Split('\\').Last();
                curNik = (curNik ?? "").Trim();

                var reviewer = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == curNik && x.IsActive);
                ViewBag.CurrentUserRole = reviewer != null ? reviewer.PositionName : "";

                var incident = db.HC_BCP_Incident.FirstOrDefault(x => x.IncidentNo == id);
                if (incident == null) return HttpNotFound();

                // [ITEM-1] Guard akses: HANYA dari HC_BCP_Role_Permission.IsAllowedReview = true
                // Reporter (Created_By) TIDAK lagi mendapat akses otomatis
                var ReviewPermission = reviewer != null
                    ? db.HC_BCP_Role_Permission.FirstOrDefault(p => p.IsAllowedReview &&
                        ((p.RoleName != null && p.RoleName == reviewer.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == reviewer.NIK.Trim())))
                    : null;

                bool canReview = ReviewPermission != null;
                if (!canReview)
                {
                    TempData["ErrorMessage"] = "Anda tidak memiliki wewenang untuk mereview incident ini.";
                    return RedirectToAction("Dashboard");
                }

                // [ITEM-1b] Guard submit: IsAllowedSubmitReview & Validasi Status
                bool canSubmitReview = ReviewPermission != null && ReviewPermission.IsAllowedSubmitReview;

                // Cek semua review yang sudah ada untuk incident ini
                var allExistingReviews = db.HC_BCP_Review
                    .Where(r => r.IncidentID == incident.ID)
                    .OrderByDescending(r => r.Created_At)
                    .ToList();

                var myPreviousReview = allExistingReviews
                    .FirstOrDefault(r => r.ReviewerNIK.Trim() == curNik);
                ViewBag.MyPreviousReview = myPreviousReview;

                var latestReview = allExistingReviews.FirstOrDefault();
                ViewBag.LatestReview = latestReview;

                bool hasAnyReviewSubmitted = allExistingReviews.Any();
                ViewBag.HasAnyReviewSubmitted = hasAnyReviewSubmitted;

                // Jika sudah ada DGM yang submit review ATAU status incident sudah mencapai Final Decision (Status >= 5) / Activated / ForwardedToPresdir,
                // maka DGM lain TIDAK BOLEH lagi melihat/men-submit form review.
                if (incident.Status < 2 || incident.Status >= 5 || incident.ForwardedToPresdir == true || hasAnyReviewSubmitted || myPreviousReview != null) {
                    canSubmitReview = false;
                }
                ViewBag.CanSubmitReview = canSubmitReview;

                var nextReviewer = reviewer?.ParentID != null ? db.HC_BCP_Organization.Find(reviewer.ParentID) : null;

                var subNiks = reviewer != null ? GetSubordinateNiks(reviewer.ID, db) : new List<string>();
                ViewBag.SubordinateNiks = subNiks;

                SyncIncidentAssessmentDetails(incident.ID, db);
                var effectiveAssessments = ComputeIncidentEffectiveAssessments(incident.ID, db, subNiks);
                ViewBag.AssessmentsWithArea = effectiveAssessments;

                var rawAssessments = db.HC_BCP_Assessment.Where(x => x.IncidentID == incident.ID && x.Status >= 1).ToList();
                var departments = db.HC_BCP_Department.Where(x => x.IsActive).OrderBy(x => x.ID).ToList();
                var areas = db.HC_BCP_Area.ToList();
                var allCriteria = db.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();

                ViewBag.Criteria = allCriteria;
                ViewBag.Assessments = rawAssessments;
                ViewBag.Departments = departments;
                ViewBag.Areas = areas;
                var revs = db.HC_BCP_Review.Where(x => x.IncidentID == incident.ID).OrderBy(x => x.Created_At).ToList();
                var revNiks = revs.Select(r => r.ReviewerNIK.Trim()).Distinct().ToList();
                var revNames = new Dictionary<string, string>();
                var revPositions = new Dictionary<string, string>();
                foreach(var nik in revNiks)
                {
                    string nikLower = nik.ToLower();
                    var user = this.db.V_Users_Active.FirstOrDefault(x => x.NIK != null && x.NIK.Trim().ToLower() == nikLower);

                    var org = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim().ToLower() == nikLower);
                    if (org != null && !string.IsNullOrEmpty(org.Name))
                    {
                        revNames[nik] = org.Name;
                    }
                    else
                    {
                        revNames[nik] = user != null && !string.IsNullOrEmpty(user.Name) ? user.Name : "Unknown";
                    }

                    revPositions[nik] = user != null && !string.IsNullOrEmpty(user.TitleName) ? user.TitleName : "Unknown Position";
                }
                ViewBag.Reviews = revs;
                ViewBag.ReviewerNames = revNames;
                ViewBag.ReviewerPositions = revPositions;
                ViewBag.ReviewerNames = revNames;
                ViewBag.CurrentReviewer = reviewer;
                ViewBag.NextReviewer = nextReviewer;
                ViewBag.Incident = incident;
                ViewBag.Permissions = ReviewPermission;

                // Load Recovery Plan data if status is Activated/Deactivated
                if (incident.Status == 6 || incident.Status == 7)
                {
                    ViewBag.RecoveryPlans = db.HC_BCP_Recovery_Plan
                                              .Include(x => x.Criteria)
                                              .Include(x => x.Todos)
                                              .Where(x => x.IncidentID == incident.ID)
                                              .ToList();
                    ViewBag.OrgList = db.HC_BCP_Organization.Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
                    ViewBag.QuestionList = db.HC_BCP_Assessment_Question.Where(x => x.IsActive).OrderBy(x => x.QuestionText).ToList();
                }
                ViewBag.IsReviewer = ReviewPermission != null && ReviewPermission.IsAllowedSubmitReview;
            }

            ViewBag.Title = "Management Review";
            return View();
        }

        [HttpPost]
        public ActionResult SubmitReview(int IncidentID, string Decision, string ReviewNote)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    var incident = db.HC_BCP_Incident.Find(IncidentID);
                    if (incident == null) return Json(new { status = "error", message = "Incident not found." });

                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    if (userBcp == null) return Json(new { status = "error", message = "Not authorized." });

                    // Validasi Status & Eskalasi
                    if (incident.Status < 3 || incident.Status >= 5 || incident.ForwardedToPresdir == true) {
                        return Json(new { status = "error", message = "Status incident saat ini tidak mengizinkan submit review baru karena sudah direview / dieskalasi." });
                    }

                    // Validasi permission IsAllowedSubmitReview
                    var perm = db.HC_BCP_Role_Permission.FirstOrDefault(p => p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));
                    if (perm == null) return Json(new { status = "error", message = "Anda tidak memiliki izin untuk submit review." });

                    // Validasi jika sudah ada review yang tersimpan untuk incident ini (oleh siapa pun)
                    var anyExistingReview = db.HC_BCP_Review.FirstOrDefault(r => r.IncidentID == incident.ID);
                    if (anyExistingReview != null) {
                        return Json(new { status = "error", message = "Review untuk incident ini sudah pernah disubmit oleh reviewer lain." });
                    }

                    var myPrev = db.HC_BCP_Review.FirstOrDefault(r => r.IncidentID == incident.ID && r.ReviewerNIK.Trim() == currUserNik);
                    if (myPrev != null) {
                        return Json(new { status = "error", message = "Anda sudah pernah melakukan review untuk incident ini." });
                    }

                    db.HC_BCP_Review.Add(new HC_BCP_Review
                    {
                        IncidentID = IncidentID,
                        ReviewerNIK = currUserNik,
                        ReviewerRole = userBcp.BCP_Role,
                        ReviewNote = ReviewNote,
                        Decision = Decision,
                        Created_At = DateTime.Now
                    });

                    // [ITEM-2] Hanya 2 Decision yang valid:
                    // EscalateToPresdir → Status 5, ForwardedToPresdir = true, kirim email ke pemegang hak Activate
                    // ERTOnly           → Status 5, tanpa notifikasi Presdir
                    if (Decision == "EscalateToPresdir")
                    {
                        incident.Status = 5;
                        incident.ForwardedToPresdir = true;
                        db.SaveChanges();

                        // Kumpulkan semua NIK yang berhak Activate (Presdir + IsAllowedActivate = true)
                        var activatorNiks = new List<string>();
                        var presdirNiks = db.HC_BCP_Organization
                            .Where(x => x.BCP_Role == "Presdir" && x.IsActive)
                            .Select(x => x.NIK.Trim()).ToList();
                        activatorNiks.AddRange(presdirNiks);

                        var delegatePerms = db.HC_BCP_Role_Permission.Where(p => p.IsAllowedActivate).ToList();
                        foreach (var dp in delegatePerms)
                        {
                            if (!string.IsNullOrEmpty(dp.UserNIK)) activatorNiks.Add(dp.UserNIK.Trim());
                            if (!string.IsNullOrEmpty(dp.RoleName))
                            {
                                var roleNiks = db.HC_BCP_Organization
                                    .Where(x => x.BCP_Role == dp.RoleName && x.IsActive)
                                    .Select(x => x.NIK.Trim()).ToList();
                                activatorNiks.AddRange(roleNiks);
                            }
                        }
                        activatorNiks = activatorNiks.Distinct().ToList();

                        // Kirim email ke semua pemegang hak Activate
                        var activateUrl = Request.Url.GetLeftPart(UriPartial.Authority) +
                            Url.Action("Activate", "BCP", new { area = "HC", id = incident.IncidentNo });

                        var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "BCP Command Center");
                        var smtp = new SmtpClient
                        {
                            Host = "ngkbusi.com", Port = 587, EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(senderEmail.Address, "100%NGKbusi!")
                        };

                        var allPortalUsers = this.db.V_Users_Active.AsNoTracking().ToList();
                        var recipients = allPortalUsers
                            .Where(u => !string.IsNullOrEmpty(u.NIK) && activatorNiks.Contains(u.NIK.Trim()))
                            .ToList();

                        foreach (var rec in recipients)
                        {
                            if (string.IsNullOrEmpty(rec.Email)) continue;
                            try
                            {
                                using (var mess = new MailMessage())
                                {
                                    mess.From = senderEmail;
                                    mess.To.Add(rec.Email);
                                    mess.Subject = $"[ACTION REQUIRED] BCP Activation Decision — {incident.IncidentNo}";
                                    mess.IsBodyHtml = true;
                                    mess.Body = $@"
                                        <div style=""font-family:'Segoe UI',sans-serif;background:#f8fafc;padding:40px 20px"">
                                          <div style=""max-width:600px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,.05);border:1px solid #e0f3f5"">
                                            <div style=""background:#e11d48;padding:30px;text-align:center"">
                                              <h1 style=""color:#fff;margin:0;font-size:22px;text-transform:uppercase;letter-spacing:2px"">BCP ACTIVATION REQUIRED</h1>
                                              <p style=""color:#ffdce0;margin:8px 0 0;font-size:13px"">Your authority is required to proceed</p>
                                            </div>
                                            <div style=""padding:40px 30px"">
                                              <p style=""color:#334155;font-size:15px;line-height:1.6"">
                                                Hello <strong>{rec.Name}</strong>,<br><br>
                                                The management review chain for incident <strong>{incident.IncidentNo}</strong> has been completed.
                                                Your authorization is required to make the final BCP Activation decision.
                                              </p>
                                              <div style=""background:#fff5f5;border-left:4px solid #e11d48;padding:15px 20px;margin:25px 0;border-radius:4px"">
                                                <p style=""margin:0 0 6px;font-size:14px;color:#7f1d1d""><strong>Incident:</strong> {incident.IncidentNo}</p>
                                                <p style=""margin:0 0 6px;font-size:14px;color:#7f1d1d""><strong>Title:</strong> {incident.Title}</p>
                                                <p style=""margin:0;font-size:14px;color:#7f1d1d""><strong>Severity:</strong> {incident.Severity}</p>
                                              </div>
                                              <div style=""text-align:center;margin:30px 0"">
                                                <a href=""{activateUrl}"" style=""background:#e11d48;color:#fff;padding:14px 32px;text-decoration:none;border-radius:30px;display:inline-block;font-weight:bold;text-transform:uppercase;font-size:13px;letter-spacing:1px"">
                                                  Open Activation Panel
                                                </a>
                                              </div>
                                            </div>
                                            <div style=""background:#f1f5f9;padding:20px;text-align:center;border-top:1px solid #e2e8f0"">
                                              <p style=""margin:0;font-size:12px;color:#94a3b8"">Sent by NGK BCP Command Center &copy; {DateTime.Now.Year} PT Niterra Mobility Indonesia</p>
                                            </div>
                                          </div>
                                        </div>";
                                    if (_enableEmail)
                                    {
                                        smtp.Send(mess);
                                    }
                                    db.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                                    {
                                        IncidentID = incident.ID,
                                        RecipientNIK = rec.NIK?.Trim(),
                                        Channel = "Email",
                                        Message = $"[EscalateToPresdir] Activate decision required for {incident.IncidentNo}",
                                        IsSent = true,
                                        SentAt = DateTime.Now
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                db.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                                {
                                    IncidentID = incident.ID,
                                    RecipientNIK = rec.NIK?.Trim(),
                                    Channel = "Email",
                                    Message = $"[EscalateToPresdir] {incident.IncidentNo}",
                                    IsSent = false,
                                    SentAt = DateTime.Now,
                                    ErrorMessage = ex.Message
                                });
                            }
                        }
                        db.SaveChanges();
                        return Json(new { status = "success", message = $"Escalated to final authority. {recipients.Count} decision maker(s) notified." });
                    }
                    else if (Decision == "ERTOnly")
                    {
                        incident.Status = 5;
                        // ERTOnly: tidak kirim notifikasi ke Presdir
                    }
                    else
                    {
                        return Json(new { status = "error", message = "Invalid decision value. Use EscalateToPresdir or ERTOnly." });
                    }

                    db.SaveChanges();
                    return Json(new { status = "success", message = "Review submitted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: HC/BCP/Activate
        public ActionResult Activate(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Dashboard");

            using (var db = new BCPConnection())
            {
                var incident = db.HC_BCP_Incident.FirstOrDefault(x => x.IncidentNo == id);
                if (incident == null) return HttpNotFound();

                // [ITEM-7] Guard akses: Presdir ATAU IsAllowedActivate = true OR DGM (IsAllowedSubmitReview = true)
                string curNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (!string.IsNullOrEmpty(curNik) && curNik.Contains("\\")) curNik = curNik.Split('\\').Last();
                curNik = (curNik ?? "").Trim();

                var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == curNik && x.IsActive);
                bool isPresdir = userBcp != null && userBcp.BCP_Role == "Presdir";
                bool isDelegated = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                    p.IsAllowedActivate &&
                    ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                     (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));
                bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                    p.IsAllowedReview && p.IsAllowedSubmitReview &&
                    ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                     (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));
                                                                    
                if (!isPresdir && !isDelegated) 
                {   
                    TempData["ErrorMessage"] = "Anda tidak memiliki wewenang untuk halaman ini.";
                    return RedirectToAction("Dashboard");
                }

                ViewBag.CurrentActivator = userBcp;
                ViewBag.IsPresdir = isPresdir;
                ViewBag.Incident = incident;
                ViewBag.IncidentNo = id;
                var assessments = db.HC_BCP_Assessment.Where(x => x.IncidentID == incident.ID).ToList();
                ViewBag.Assessments = assessments;
                ViewBag.Areas = db.HC_BCP_Area.ToList();
                ViewBag.Departments = db.HC_BCP_Department.ToList();
                
                var assessmentIds = assessments.Select(a => a.ID).ToList();
                ViewBag.AllDetails = db.HC_BCP_Assessment_Detail.Where(d => assessmentIds.Contains(d.AssessmentID)).ToList();
                ViewBag.Criteria = db.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
                var revs = db.HC_BCP_Review.Where(x => x.IncidentID == incident.ID).OrderBy(x => x.Created_At).ToList();
                var revNiks = revs.Select(r => r.ReviewerNIK.Trim()).Distinct().ToList();
                var revNames = new Dictionary<string, string>();
                var revPositions = new Dictionary<string, string>();
                foreach(var nik in revNiks)
                {
                    string nikLower = nik.ToLower();
                    var user = this.db.V_Users_Active.FirstOrDefault(x => x.NIK != null && x.NIK.Trim().ToLower() == nikLower);

                    var org = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim().ToLower() == nikLower);
                    if (org != null && !string.IsNullOrEmpty(org.Name))
                    {
                        revNames[nik] = org.Name;
                    }
                    else
                    {
                        revNames[nik] = user != null && !string.IsNullOrEmpty(user.Name) ? user.Name : "Unknown";
                    }

                    revPositions[nik] = user != null && !string.IsNullOrEmpty(user.TitleName) ? user.TitleName : "Unknown Position";
                }
                
                string reporterNik = incident.Created_By?.Trim().ToLower() ?? "";
                var repUser = this.db.V_Users_Active.FirstOrDefault(x => x.NIK != null && x.NIK.Trim().ToLower() == reporterNik);
                ViewBag.ReporterName = repUser != null && !string.IsNullOrEmpty(repUser.Name) ? repUser.Name : incident.Created_By;

                ViewBag.Reviews = revs;
                ViewBag.ReviewerNames = revNames;
                ViewBag.ReviewerPositions = revPositions;
                ViewBag.ReviewerNames = revNames;
                
                string activatorNik = incident.Activated_By?.Trim().ToLower() ?? "";
                var actUser = this.db.V_Users_Active.FirstOrDefault(x => x.NIK != null && x.NIK.Trim().ToLower() == activatorNik);
                ViewBag.ActivatorName = actUser != null && !string.IsNullOrEmpty(actUser.Name) ? actUser.Name : incident.Activated_By;

                ViewBag.ActivationLogs = db.HC_BCP_Activation_Log.Where(x => x.IncidentID == incident.ID).OrderByDescending(x => x.ActionAt).ToList();

                // Load Recovery Plan data
                ViewBag.RecoveryPlans = db.HC_BCP_Recovery_Plan
                                          .Include(x => x.Criteria)
                                          .Include(x => x.Todos)
                                          .Where(x => x.IncidentID == incident.ID)
                                          .ToList();
                ViewBag.OrgList = db.HC_BCP_Organization.Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
                ViewBag.QuestionList = db.HC_BCP_Assessment_Question.Where(x => x.IsActive).OrderBy(x => x.QuestionText).ToList();
                ViewBag.IsReviewer = isReviewer;
                ViewBag.CanApprovePlan = isPresdir || isDelegated;
            }

            ViewBag.Title = "BCP Final Decision";
            return View();
        }

        [HttpPost]
        public ActionResult FinalDecision(int IncidentID, string Decision, string Note)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    var incident = db.HC_BCP_Incident.Find(IncidentID);
                    if (incident == null) return Json(new { status = "error", message = "Incident not found." });

                    // [ITEM-7] Validasi: hanya Presdir atau pemegang IsAllowedActivate
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isPresdir = userBcp != null && userBcp.BCP_Role == "Presdir";
                    bool isDelegated = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedActivate &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));
                    if (!isPresdir && !isDelegated)
                        return Json(new { status = "error", message = "Unauthorized." });

                    // [ITEM-5] Decision yang valid: Activated (→6) dan Deactivated (→7)
                    if (Decision == "Activated")
                    {
                        incident.Status = 6;
                    }
                    else if (Decision == "Deactivated")
                    {
                        incident.Status = 7;
                    }
                    else
                    {
                        return Json(new { status = "error", message = "Invalid decision. Use Activated or Deactivated." });
                    }

                    incident.Activation_Decision = Decision;
                    incident.Activated_By = currUserNik;
                    incident.Activated_At = DateTime.Now;

                    db.HC_BCP_Activation_Log.Add(new HC_BCP_Activation_Log
                    {
                        IncidentID = IncidentID,
                        Action = Decision,
                        ActionBy = currUserNik,
                        ActionAt = DateTime.Now,
                        Note = Note
                    });

                    db.SaveChanges();
                    string msg = Decision == "Activated"
                        ? $"BCP has been ACTIVATED for incident {incident.IncidentNo}."
                        : $"BCP has been DEACTIVATED for incident {incident.IncidentNo}.";
                    return Json(new { status = "success", message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult CloseAssessment(int IncidentID)
        {
            try
            {
                using (var db = new BCPConnection())
                {
                    var incident = db.HC_BCP_Incident.Find(IncidentID);
                    if (incident == null) return Json(new { status = "error", message = "Incident not found." });

                    incident.Status = 3; 
                    db.SaveChanges();
                    return Json(new { status = "success", message = "Assessment phase closed." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: HC/BCP/Organization
        public ActionResult Organization()
        {
            ViewBag.Title = "BCP Organization Structure";
            return View();
        }

        [HttpGet]
        public ActionResult GetOrganizationList(string search = "")
        {
            try
            {
                var orgMembers = bcpDb.HC_BCP_Organization.ToList();
                var departments = bcpDb.HC_BCP_Department.ToList();

                var query = from o in orgMembers
                            join p in orgMembers on o.ParentID equals p.ID into pj
                            from p in pj.DefaultIfEmpty()
                            join d in departments on o.DepartmentCode equals d.Code into dj
                            from d in dj.DefaultIfEmpty()
                            select new
                            {
                                ID = o.ID,
                                NIK = o.NIK.Trim(),
                                Name = o.Name != null ? o.Name.Trim() : "",
                                BCP_Role = o.BCP_Role != null ? o.BCP_Role.Trim() : "",
                                PositionName = o.PositionName != null ? o.PositionName.Trim() : "",
                                DepartmentName = d != null ? (d.DeptName != null ? d.DeptName.Trim() : "") : (o.DepartmentName != null ? o.DepartmentName.Trim() : ""),
                                DepartmentCode = o.DepartmentCode != null ? o.DepartmentCode.Trim() : "",
                                ParentID = o.ParentID,
                                ParentName = p != null ? p.Name.Trim() : "",
                                IsActive = o.IsActive
                            };

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();
                    query = query.Where(x => x.NIK.ToLower().Contains(search) ||
                                             x.Name.ToLower().Contains(search) ||
                                             x.BCP_Role.ToLower().Contains(search) ||
                                             x.PositionName.ToLower().Contains(search) ||
                                             x.DepartmentName.ToLower().Contains(search) ||
                                             x.ParentName.ToLower().Contains(search));
                }

                var list = query.OrderBy(x => x.Name).ToList();
                return Json(new { status = "success", data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetOrganizationFormData(int? currentMemberId)
        {
            try
            {
                var users = db.V_Users_Active
                              .AsNoTracking()
                              .Where(x => x.Status != "Not Active" && x.NIK != null && x.Name != null)
                              .ToList()
                              .Select(x => new
                              {
                                  NIK = x.NIK.Trim(),
                                  Name = x.Name.Trim(),
                                  DeptName = x.DeptName != null ? x.DeptName.Trim() : "",
                                  DeptID = x.DeptID != null ? x.DeptID.Trim() : ""
                              })
                              .OrderBy(x => x.Name)
                              .ToList();

                var parentQuery = bcpDb.HC_BCP_Organization
                                       .Where(x => x.IsActive);
                
                if (currentMemberId.HasValue && currentMemberId.Value > 0)
                {
                    parentQuery = parentQuery.Where(x => x.ID != currentMemberId.Value);
                }

                var parents = parentQuery.ToList()
                                         .Select(x => new
                                         {
                                             ID = x.ID,
                                             Name = x.Name != null ? x.Name.Trim() : x.NIK.Trim(),
                                             BCP_Role = x.BCP_Role != null ? x.BCP_Role.Trim() : "",
                                             PositionName = x.PositionName != null ? x.PositionName.Trim() : ""
                                         })
                                         .OrderBy(x => x.Name)
                                         .ToList();

                var roles = bcpDb.HC_BCP_Role_Permission
                                 .Where(x => x.RoleName != null && x.RoleName != "")
                                 .Select(x => x.RoleName)
                                 .Distinct()
                                 .OrderBy(x => x)
                                 .ToList();

                return Json(new { status = "success", users = users, parents = parents, roles = roles }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveOrganization(int? ID, string NIK, string Name, string BCP_Role, string DepartmentName, string DepartmentCode, int? ParentID, bool IsActive)
        {
            try
            {
                if (string.IsNullOrEmpty(NIK)) return Json(new { status = "error", message = "NIK is required." });
                if (string.IsNullOrEmpty(BCP_Role)) return Json(new { status = "error", message = "BCP Role is required." });

                NIK = NIK.Trim();
                Name = (Name ?? "").Trim();
                BCP_Role = BCP_Role.Trim();
                DepartmentName = (DepartmentName ?? "").Trim();
                DepartmentCode = (DepartmentCode ?? "").Trim();

                var duplicate = bcpDb.HC_BCP_Organization
                                     .Any(x => x.NIK.Trim() == NIK && x.IsActive && (!ID.HasValue || x.ID != ID.Value));
                if (duplicate && IsActive)
                {
                    return Json(new { status = "error", message = "This employee is already an active member of the BCP Organization." });
                }

                string posName = "";
                var activeUser = db.V_Users_Active.AsNoTracking().FirstOrDefault(x => x.NIK.Trim() == NIK.Trim());
                if (activeUser != null)
                {
                    posName = activeUser.PositionName ?? "";
                }

                if (ID.HasValue && ID.Value > 0)
                {
                    var existing = bcpDb.HC_BCP_Organization.Find(ID.Value);
                    if (existing == null) return Json(new { status = "error", message = "Member record not found." });

                    if (ParentID.HasValue && ParentID.Value == ID.Value)
                    {
                        return Json(new { status = "error", message = "A member cannot be their own superior." });
                    }

                    existing.NIK = NIK;
                    existing.Name = Name;
                    existing.BCP_Role = BCP_Role;
                    existing.PositionName = posName;
                    existing.DepartmentName = DepartmentName;
                    existing.DepartmentCode = DepartmentCode;
                    existing.ParentID = ParentID;
                    existing.IsActive = IsActive;

                    bcpDb.Entry(existing).State = EntityState.Modified;
                }
                else
                {
                    string creatorNik = User.Identity.GetUserId() ?? User.Identity.Name;
                    if (creatorNik.Contains("\\")) { creatorNik = creatorNik.Split('\\').Last(); }
                    creatorNik = (creatorNik ?? "").Trim();
                    if (creatorNik.Length > 20) { creatorNik = creatorNik.Substring(0, 20); }

                    var newMember = new HC_BCP_Organization
                    {
                        NIK = NIK,
                        Name = Name,
                        BCP_Role = BCP_Role,
                        PositionName = posName,
                        DepartmentName = DepartmentName,
                        DepartmentCode = DepartmentCode,
                        ParentID = ParentID,
                        IsActive = IsActive,
                        Created_At = DateTime.Now,
                        Created_By = creatorNik
                    };

                    bcpDb.HC_BCP_Organization.Add(newMember);
                }

                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Organization member saved successfully." });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                return Json(new { status = "error", message = "Validation Error: " + fullErrorMessage });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteOrganization(int ID)
        {
            try
            {
                var existing = bcpDb.HC_BCP_Organization.Find(ID);
                if (existing == null) return Json(new { status = "error", message = "Member record not found." });

                existing.IsActive = false;
                bcpDb.Entry(existing).State = EntityState.Modified;
                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Member deactivated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: HC/BCP/UserArea
        public ActionResult UserArea()
        {
            ViewBag.Title = "BCP User Area Mapping";
            return View();
        }

        [HttpGet]
        public ActionResult GetUserAreaList(string search = "")
        {
            try
            {
                var userAreas = bcpDb.HC_BCP_User_Area.ToList();
                var areas = bcpDb.HC_BCP_Area.ToList();
                var activeOrg = bcpDb.HC_BCP_Organization.ToList();
                var departments = bcpDb.HC_BCP_Department.ToList();

                var query = from ua in userAreas
                            join ar in areas on ua.AreaID equals ar.ID
                            join dep in departments on ar.DepartmentCode equals dep.Code into depj
                            from dep in depj.DefaultIfEmpty()
                            join u in activeOrg on ua.NIK.Trim() equals u.NIK.Trim() into uj
                            from u in uj.DefaultIfEmpty()
                            select new
                            {
                                ID = ua.ID,
                                NIK = ua.NIK.Trim(),
                                EmployeeName = u != null ? u.Name.Trim() : ua.NIK.Trim(),
                                AreaID = ua.AreaID,
                                AreaName = ar.AreaName,
                                DepartmentName = dep != null ? (dep.DeptName != null ? dep.DeptName.Trim() : "") : (u != null ? (u.DepartmentName != null ? u.DepartmentName.Trim() : "") : (ar.DepartmentCode != null ? ar.DepartmentCode.Trim() : ""))
                            };

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();
                    query = query.Where(x => x.NIK.ToLower().Contains(search) || 
                                             x.EmployeeName.ToLower().Contains(search) || 
                                             x.AreaName.ToLower().Contains(search) ||
                                             x.DepartmentName.ToLower().Contains(search));
                }

                var list = query.OrderBy(x => x.EmployeeName).ToList();
                return Json(new { status = "success", data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetUserAreaFormData()
        {
            try
            {
                var areas = bcpDb.HC_BCP_Area
                                 .Where(x => x.IsActive)
                                 .Select(x => new { x.ID, x.AreaName })
                                 .OrderBy(x => x.AreaName)
                                 .ToList();

                var users = bcpDb.HC_BCP_Organization
                                 .Where(x => x.IsActive && x.NIK != null && x.Name != null)
                                 .ToList()
                                 .Select(x => new { 
                                     NIK = x.NIK.Trim(), 
                                     Name = x.Name.Trim(), 
                                     DeptName = x.DepartmentName != null ? x.DepartmentName.Trim() : "" 
                                 })
                                 .OrderBy(x => x.Name)
                                 .ToList();

                return Json(new { status = "success", areas = areas, users = users }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveUserArea(int? ID, string NIK, int AreaID)
        {
            try
            {
                if (string.IsNullOrEmpty(NIK))
                {
                    return Json(new { status = "error", message = "Employee is required." });
                }

                NIK = NIK.Trim();

                var duplicate = bcpDb.HC_BCP_User_Area
                                     .Any(x => x.NIK.Trim() == NIK && x.AreaID == AreaID && (!ID.HasValue || x.ID != ID.Value));

                if (duplicate)
                {
                    return Json(new { status = "error", message = "This employee is already mapped to the selected area." });
                }

                if (ID.HasValue && ID.Value > 0)
                {
                    var existing = bcpDb.HC_BCP_User_Area.Find(ID.Value);
                    if (existing == null)
                    {
                        return Json(new { status = "error", message = "Mapping record not found." });
                    }

                    existing.NIK = NIK;
                    existing.AreaID = AreaID;
                    bcpDb.Entry(existing).State = EntityState.Modified;
                }
                else
                {
                    var newMapping = new HC_BCP_User_Area
                    {
                        NIK = NIK,
                        AreaID = AreaID
                    };
                    bcpDb.HC_BCP_User_Area.Add(newMapping);
                }

                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Mapping saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteUserArea(int ID)
        {
            try
            {
                var existing = bcpDb.HC_BCP_User_Area.Find(ID);
                if (existing == null)
                {
                    return Json(new { status = "error", message = "Mapping record not found." });
                }

                bcpDb.HC_BCP_User_Area.Remove(existing);
                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Mapping deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // GET: HC/BCP/Department
        public ActionResult Department()
        {
            ViewBag.Title = "BCP Department Master";
            return View();
        }

        [HttpGet]
        public ActionResult GetDepartmentList(string search = "")
        {
            try
            {
                var query = bcpDb.HC_BCP_Department.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();
                    query = query.Where(x => x.DeptName.ToLower().Contains(search) || 
                                             x.Code.ToLower().Contains(search));
                }

                var list = query.OrderBy(x => x.DeptName).ToList();
                return Json(new { status = "success", data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveDepartment(int? ID, string DeptName, string Code, bool IsActive)
        {
            try
            {
                if (string.IsNullOrEmpty(DeptName))
                {
                    return Json(new { status = "error", message = "Department Name is required." });
                }
                if (string.IsNullOrEmpty(Code))
                {
                    return Json(new { status = "error", message = "Department Code is required." });
                }

                DeptName = DeptName.Trim();
                Code = Code.Trim();

                // Check for duplicate Code
                var duplicate = bcpDb.HC_BCP_Department
                                     .Any(x => x.Code.Trim() == Code && (!ID.HasValue || x.ID != ID.Value));

                if (duplicate)
                {
                    return Json(new { status = "error", message = "Department Code already exists." });
                }

                if (ID.HasValue && ID.Value > 0)
                {
                    var existing = bcpDb.HC_BCP_Department.Find(ID.Value);
                    if (existing == null)
                    {
                        return Json(new { status = "error", message = "Department record not found." });
                    }

                    existing.DeptName = DeptName;
                    existing.Code = Code;
                    existing.IsActive = IsActive;
                    bcpDb.Entry(existing).State = EntityState.Modified;
                }
                else
                {
                    var newDept = new HC_BCP_Department
                    {
                        DeptName = DeptName,
                        Code = Code,
                        IsActive = IsActive,
                        Created_At = DateTime.Now
                    };
                    bcpDb.HC_BCP_Department.Add(newDept);
                }

                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Department saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteDepartment(int ID)
        {
            try
            {
                var existing = bcpDb.HC_BCP_Department.Find(ID);
                if (existing == null)
                {
                    return Json(new { status = "error", message = "Department record not found." });
                }

                bcpDb.HC_BCP_Department.Remove(existing);
                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Department deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult DownloadDepartmentTemplate()
        {
            try
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Template Department");

                    worksheet.Cell(1, 1).Value = "Department Code";
                    worksheet.Cell(1, 2).Value = "Department Name";
                    worksheet.Cell(1, 3).Value = "Is Active (TRUE/FALSE)";

                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Font.FontSize = 11;
                    headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                    worksheet.Cell(2, 1).Value = "IT";
                    worksheet.Cell(2, 2).Value = "Information Technology";
                    worksheet.Cell(2, 3).Value = "TRUE";

                    worksheet.Cell(3, 1).Value = "HR";
                    worksheet.Cell(3, 2).Value = "Human Resources";
                    worksheet.Cell(3, 3).Value = "TRUE";

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new System.IO.MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BCP_Department_Template.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("Error generating template: " + ex.Message);
            }
        }

        [HttpPost]
        public ActionResult UploadDepartmentExcel(System.Web.HttpPostedFileBase file)
        {
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new { status = "error", message = "Please select a valid Excel file." });
                }

                string extension = System.IO.Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx")
                {
                    return Json(new { status = "error", message = "Only Excel files (.xlsx) are allowed." });
                }

                int insertedCount = 0;
                int updatedCount = 0;

                using (var workbook = new ClosedXML.Excel.XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    using (var db = new BCPConnection())
                    {
                        foreach (var row in rows)
                        {
                            string code = row.Cell(1).GetValue<string>();
                            string deptName = row.Cell(2).GetValue<string>();
                            string isActiveStr = row.Cell(3).GetValue<string>();

                            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(deptName))
                            {
                                continue;
                            }

                            code = code.Trim();
                            deptName = deptName.Trim();

                            bool isActive = true;
                            if (!string.IsNullOrEmpty(isActiveStr))
                            {
                                string val = isActiveStr.Trim().ToUpper();
                                if (val == "FALSE" || val == "0" || val == "N" || val == "NO")
                                {
                                    isActive = false;
                                }
                            }

                            var existing = db.HC_BCP_Department.FirstOrDefault(x => x.Code.Trim().ToLower() == code.ToLower());
                            if (existing != null)
                            {
                                existing.DeptName = deptName;
                                existing.IsActive = isActive;
                                db.Entry(existing).State = EntityState.Modified;
                                updatedCount++;
                            }
                            else
                            {
                                var newDept = new HC_BCP_Department
                                {
                                    Code = code,
                                    DeptName = deptName,
                                    IsActive = isActive,
                                    Created_At = DateTime.Now
                                };
                                db.HC_BCP_Department.Add(newDept);
                                insertedCount++;
                            }
                        }

                        db.SaveChanges();
                    }
                }

                return Json(new { status = "success", message = $"Import completed successfully. {insertedCount} departments added, {updatedCount} departments updated." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Failed to upload excel: " + ex.Message });
            }
        }

        // GET: HC/BCP/Area
        public ActionResult Area()
        {
            ViewBag.Title = "BCP Area Master";
            return View();
        }

        [HttpGet]
        public ActionResult GetAreaList(string search = "")
        {
            try
            {
                var areas = bcpDb.HC_BCP_Area.AsNoTracking().ToList();
                var departments = bcpDb.HC_BCP_Department.AsNoTracking().ToList();

                var query = from ar in areas
                            join dep in departments on ar.DepartmentCode equals dep.Code into depj
                            from dep in depj.DefaultIfEmpty()
                            select new
                            {
                                ID = ar.ID,
                                AreaName = ar.AreaName,
                                DepartmentCode = ar.DepartmentCode,
                                DepartmentName = dep != null ? dep.DeptName : "-",
                                IsActive = ar.IsActive
                            };

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();
                    query = query.Where(x => x.AreaName.ToLower().Contains(search) || 
                                             x.DepartmentName.ToLower().Contains(search) ||
                                             (x.DepartmentCode != null && x.DepartmentCode.ToLower().Contains(search)));
                }

                var list = query.OrderBy(x => x.AreaName).ToList();
                return Json(new { status = "success", data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetAreaFormData()
        {
            try
            {
                var departments = bcpDb.HC_BCP_Department
                                       .Where(x => x.IsActive)
                                       .Select(x => new { x.Code, x.DeptName })
                                       .OrderBy(x => x.DeptName)
                                       .ToList();

                return Json(new { status = "success", departments = departments }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveArea(int? ID, string AreaName, string DepartmentCode, bool IsActive)
        {
            try
            {
                if (string.IsNullOrEmpty(AreaName))
                {
                    return Json(new { status = "error", message = "Area Name is required." });
                }

                AreaName = AreaName.Trim();
                DepartmentCode = string.IsNullOrEmpty(DepartmentCode) ? null : DepartmentCode.Trim();

                if (ID.HasValue && ID.Value > 0)
                {
                    var existing = bcpDb.HC_BCP_Area.Find(ID.Value);
                    if (existing == null)
                    {
                        return Json(new { status = "error", message = "Area record not found." });
                    }

                    existing.AreaName = AreaName;
                    existing.DepartmentCode = DepartmentCode;
                    existing.IsActive = IsActive;
                    bcpDb.Entry(existing).State = EntityState.Modified;
                }
                else
                {
                    var newArea = new HC_BCP_Area
                    {
                        AreaName = AreaName,
                        DepartmentCode = DepartmentCode,
                        IsActive = IsActive
                    };
                    bcpDb.HC_BCP_Area.Add(newArea);
                }

                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Area saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteArea(int ID)
        {
            try
            {
                var existing = bcpDb.HC_BCP_Area.Find(ID);
                if (existing == null)
                {
                    return Json(new { status = "error", message = "Area record not found." });
                }

                bcpDb.HC_BCP_Area.Remove(existing);
                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Area deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult DownloadAreaTemplate()
        {
            try
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Template Area");

                    worksheet.Cell(1, 1).Value = "Area Name";
                    worksheet.Cell(1, 2).Value = "Department Code (Optional)";
                    worksheet.Cell(1, 3).Value = "Is Active (TRUE/FALSE)";

                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Font.FontSize = 11;
                    headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                    worksheet.Cell(2, 1).Value = "Line Assembly 1";
                    worksheet.Cell(2, 2).Value = "PROD";
                    worksheet.Cell(2, 3).Value = "TRUE";

                    worksheet.Cell(3, 1).Value = "Server Room";
                    worksheet.Cell(3, 2).Value = "IT";
                    worksheet.Cell(3, 3).Value = "TRUE";

                    worksheet.Cell(4, 1).Value = "Main Lobby";
                    worksheet.Cell(4, 2).Value = "";
                    worksheet.Cell(4, 3).Value = "TRUE";

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new System.IO.MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BCP_Area_Template.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("Error generating template: " + ex.Message);
            }
        }

        [HttpPost]
        public ActionResult UploadAreaExcel(System.Web.HttpPostedFileBase file)
        {
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new { status = "error", message = "Please select a valid Excel file." });
                }

                string extension = System.IO.Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx")
                {
                    return Json(new { status = "error", message = "Only Excel files (.xlsx) are allowed." });
                }

                int insertedCount = 0;
                int updatedCount = 0;

                using (var workbook = new ClosedXML.Excel.XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    using (var db = new BCPConnection())
                    {
                        foreach (var row in rows)
                        {
                            string areaName = row.Cell(1).GetValue<string>();
                            string deptCode = row.Cell(2).GetValue<string>();
                            string isActiveStr = row.Cell(3).GetValue<string>();

                            if (string.IsNullOrEmpty(areaName))
                            {
                                continue;
                            }

                            areaName = areaName.Trim();
                            deptCode = string.IsNullOrEmpty(deptCode) ? null : deptCode.Trim();

                            bool isActive = true;
                            if (!string.IsNullOrEmpty(isActiveStr))
                            {
                                string val = isActiveStr.Trim().ToUpper();
                                if (val == "FALSE" || val == "0" || val == "N" || val == "NO")
                                {
                                    isActive = false;
                                }
                            }

                            var existing = db.HC_BCP_Area.ToList().FirstOrDefault(x => 
                                x.AreaName.Trim().Equals(areaName, StringComparison.OrdinalIgnoreCase) && 
                                (
                                    (string.IsNullOrEmpty(x.DepartmentCode) && string.IsNullOrEmpty(deptCode)) ||
                                    (!string.IsNullOrEmpty(x.DepartmentCode) && !string.IsNullOrEmpty(deptCode) && x.DepartmentCode.Trim().Equals(deptCode, StringComparison.OrdinalIgnoreCase))
                                )
                            );

                            if (existing != null)
                            {
                                existing.IsActive = isActive;
                                db.Entry(existing).State = EntityState.Modified;
                                updatedCount++;
                            }
                            else
                            {
                                var newArea = new HC_BCP_Area
                                {
                                    AreaName = areaName,
                                    DepartmentCode = deptCode,
                                    IsActive = isActive
                                };
                                db.HC_BCP_Area.Add(newArea);
                                insertedCount++;
                            }
                        }

                        db.SaveChanges();
                    }
                }

                return Json(new { status = "success", message = $"Import completed successfully. {insertedCount} areas added, {updatedCount} areas updated." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Failed to upload excel: " + ex.Message });
            }
        }

        // GET: HC/BCP/AssessmentQuestion
        public ActionResult AssessmentQuestion()
        {
            ViewBag.Title = "BCP Assessment Questions";
            return View();
        }

        [HttpGet]
        public ActionResult GetAssessmentQuestionList(string search = "")
        {
            try
            {
                var questions = bcpDb.HC_BCP_Assessment_Question
                                     .Include(q => q.Criteria)
                                     .Include(q => q.Choices)
                                     .ToList();

                var departments = bcpDb.HC_BCP_Department.ToList();
                var deptMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in departments)
                {
                    if (!string.IsNullOrEmpty(d.Code) && !deptMap.ContainsKey(d.Code.Trim()))
                    {
                        deptMap[d.Code.Trim()] = !string.IsNullOrWhiteSpace(d.DeptName) ? d.DeptName.Trim() : d.Code.Trim();
                    }
                }

                var query = questions.Select(q => {
                    var deptNameList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(q.AllowedDepartments))
                    {
                        var codes = q.AllowedDepartments.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var code in codes)
                        {
                            var trimmed = code.Trim();
                            if (deptMap.TryGetValue(trimmed, out var name) && !string.IsNullOrWhiteSpace(name))
                            {
                                deptNameList.Add(name);
                            }
                            else
                            {
                                deptNameList.Add(trimmed);
                            }
                        }
                    }

                    return new
                    {
                        ID = q.ID,
                        CriteriaID = q.CriteriaID,
                        CriteriaName = q.Criteria != null ? q.Criteria.Name : "",
                        QuestionText = q.QuestionText,
                        Weight = q.Weight,
                        AllowedDepartments = q.AllowedDepartments,
                        AllowedDepartmentNames = string.Join(", ", deptNameList),
                        DepartmentBadgeList = deptNameList,
                        SortOrder = q.SortOrder,
                        IsActive = q.IsActive,
                        Choices = q.Choices.Select(c => new
                        {
                            ID = c.ID,
                            ScoreValue = c.ScoreValue,
                            ChoiceText = c.ChoiceText
                        }).OrderBy(c => c.ScoreValue).ToList()
                    };
                });

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();
                    query = query.Where(x => x.QuestionText.ToLower().Contains(search) ||
                                             (x.CriteriaName != null && x.CriteriaName.ToLower().Contains(search)) ||
                                             (x.AllowedDepartments != null && x.AllowedDepartments.ToLower().Contains(search)) ||
                                             (x.AllowedDepartmentNames != null && x.AllowedDepartmentNames.ToLower().Contains(search)));
                }

                var list = query.OrderBy(x => x.CriteriaName).ThenBy(x => x.SortOrder).ThenBy(x => x.ID).ToList();
                return Json(new { status = "success", data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetAssessmentQuestionFormData()
        {
            try
            {
                var criteria = bcpDb.HC_BCP_Assessment_Criteria
                                    .Where(x => x.IsActive)
                                    .OrderBy(x => x.SortOrder)
                                    .ThenBy(x => x.Name)
                                    .Select(x => new { x.ID, x.Name, x.Weight })
                                    .ToList();

                var departments = bcpDb.HC_BCP_Department
                                       .Where(x => x.IsActive)
                                       .Select(x => new { x.Code, x.DeptName })
                                       .OrderBy(x => x.DeptName)
                                       .ToList();

                return Json(new { status = "success", criteria = criteria, departments = departments }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public class QuestionChoiceDTO
        {
            public int? ID { get; set; }
            public int ScoreValue { get; set; }
            public string ChoiceText { get; set; }
        }

        [HttpPost]
        public ActionResult SaveAssessmentQuestion(int? ID, int CriteriaID, string QuestionText, decimal? Weight, string AllowedDepartments, int? SortOrder, bool IsActive, List<QuestionChoiceDTO> Choices)
        {
            try
            {
                if (string.IsNullOrEmpty(QuestionText))
                {
                    return Json(new { status = "error", message = "Question text is required." });
                }

                HC_BCP_Assessment_Question question;
                if (ID.HasValue && ID.Value > 0)
                {
                    question = bcpDb.HC_BCP_Assessment_Question.Include(q => q.Choices).FirstOrDefault(q => q.ID == ID.Value);
                    if (question == null)
                    {
                        return Json(new { status = "error", message = "Question not found." });
                    }

                    question.CriteriaID = CriteriaID;
                    question.QuestionText = QuestionText.Trim();
                    question.Weight = Weight;
                    question.AllowedDepartments = AllowedDepartments;
                    question.SortOrder = SortOrder;
                    question.IsActive = IsActive;

                    bcpDb.Entry(question).State = EntityState.Modified;
                }
                else
                {
                    question = new HC_BCP_Assessment_Question
                    {
                        CriteriaID = CriteriaID,
                        QuestionText = QuestionText.Trim(),
                        Weight = Weight,
                        AllowedDepartments = AllowedDepartments,
                        SortOrder = SortOrder,
                        IsActive = IsActive,
                        Choices = new List<HC_BCP_Assessment_Question_Choice>()
                    };
                    bcpDb.HC_BCP_Assessment_Question.Add(question);
                }

                bcpDb.SaveChanges();

                var submittedChoiceIds = (Choices ?? new List<QuestionChoiceDTO>())
                                         .Where(c => c.ID.HasValue && c.ID.Value > 0)
                                         .Select(c => c.ID.Value)
                                         .ToList();

                var choicesToDelete = bcpDb.HC_BCP_Assessment_Question_Choice
                                           .Where(c => c.QuestionID == question.ID && !submittedChoiceIds.Contains(c.ID))
                                           .ToList();
                bcpDb.HC_BCP_Assessment_Question_Choice.RemoveRange(choicesToDelete);

                if (Choices != null)
                {
                    foreach (var choiceDto in Choices)
                    {
                        if (string.IsNullOrEmpty(choiceDto.ChoiceText)) continue;

                        if (choiceDto.ID.HasValue && choiceDto.ID.Value > 0)
                        {
                            var existingChoice = bcpDb.HC_BCP_Assessment_Question_Choice.Find(choiceDto.ID.Value);
                            if (existingChoice != null)
                            {
                                existingChoice.ScoreValue = choiceDto.ScoreValue;
                                existingChoice.ChoiceText = choiceDto.ChoiceText.Trim();
                                bcpDb.Entry(existingChoice).State = EntityState.Modified;
                            }
                        }
                        else
                        {
                            var newChoice = new HC_BCP_Assessment_Question_Choice
                            {
                                QuestionID = question.ID,
                                ScoreValue = choiceDto.ScoreValue,
                                ChoiceText = choiceDto.ChoiceText.Trim()
                            };
                            bcpDb.HC_BCP_Assessment_Question_Choice.Add(newChoice);
                        }
                    }
                }

                bcpDb.SaveChanges();
                return Json(new { status = "success", message = "Assessment question saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteAssessmentQuestion(int ID)
        {
            try
            {
                var question = bcpDb.HC_BCP_Assessment_Question.Find(ID);
                if (question == null)
                {
                    return Json(new { status = "error", message = "Question not found." });
                }

                bool hasAnswers = bcpDb.HC_BCP_Assessment_Answer.Any(a => a.QuestionID == ID);
                if (hasAnswers)
                {
                    question.IsActive = false;
                    bcpDb.Entry(question).State = EntityState.Modified;
                    bcpDb.SaveChanges();
                    return Json(new { status = "success", code = "deactivated", message = "This question has existing assessment answers. It has been deactivated instead of permanently deleted to maintain historical integrity." });
                }

                var choices = bcpDb.HC_BCP_Assessment_Question_Choice.Where(c => c.QuestionID == ID).ToList();
                bcpDb.HC_BCP_Assessment_Question_Choice.RemoveRange(choices);

                bcpDb.HC_BCP_Assessment_Question.Remove(question);
                bcpDb.SaveChanges();
                return Json(new { status = "success", code = "deleted", message = "Assessment question and its choices deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult GetAreaAssessmentDetails(int incidentId, int areaId)
        {
            try
            {
                var assessment = bcpDb.HC_BCP_Assessment
                                      .Include(x => x.Details)
                                      .FirstOrDefault(x => x.IncidentID == incidentId && x.AreaID == areaId);

                if (assessment == null)
                {
                    return Json(new { status = "error", message = "Assessment not found for this area." }, JsonRequestBehavior.AllowGet);
                }

                var targetArea = bcpDb.HC_BCP_Area.FirstOrDefault(a => a.ID == areaId);
                string targetAreaDept = targetArea != null ? (targetArea.DepartmentCode ?? "").Trim() : "";

                var criteriaList = bcpDb.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
                var questions = bcpDb.HC_BCP_Assessment_Question.Include(q => q.Choices).Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
                var answers = bcpDb.HC_BCP_Assessment_Answer.Where(x => x.AssessmentID == assessment.ID).ToList();

                // Compute highest score for restricted questions from authorized departments in this incident
                var rawAssessments = bcpDb.HC_BCP_Assessment.Where(x => x.IncidentID == incidentId && x.Status >= 1).ToList();
                var allAssessmentIds = rawAssessments.Select(a => a.ID).ToList();
                var allIncidentAnswers = bcpDb.HC_BCP_Assessment_Answer.Where(a => allAssessmentIds.Contains(a.AssessmentID)).ToList();
                var allAreas = bcpDb.HC_BCP_Area.ToList();

                var specializedMaxScores = new Dictionary<int, int>();
                foreach (var q in questions)
                {
                    if (!string.IsNullOrWhiteSpace(q.AllowedDepartments))
                    {
                        int maxScore = 0;
                        foreach (var ass in rawAssessments)
                        {
                            var ar = allAreas.FirstOrDefault(a => a.ID == ass.AreaID);
                            string arDept = ar != null ? (ar.DepartmentCode ?? "").Trim() : "";
                            if (IsAreaAuthorizedForQuestion(q, arDept))
                            {
                                var ans = allIncidentAnswers.FirstOrDefault(a => a.AssessmentID == ass.ID && a.QuestionID == q.ID);
                                int s = ans?.Score ?? 0;
                                if (s > maxScore) maxScore = s;
                            }
                        }
                        specializedMaxScores[q.ID] = maxScore;
                    }
                }

                var details = criteriaList.Select(c =>
                {
                    var d = assessment.Details.FirstOrDefault(det => det.CriteriaID == c.ID);
                    var qForCrit = questions.Where(q => q.CriteriaID == c.ID).ToList();

                    var qDetails = qForCrit.Select(q =>
                    {
                        bool isAuth = IsAreaAuthorizedForQuestion(q, targetAreaDept);
                        int score = 0;
                        string choiceText = "";

                        if (isAuth)
                        {
                            var a = answers.FirstOrDefault(ans => ans.QuestionID == q.ID);
                            score = a != null ? (a.Score ?? 0) : 0;
                            choiceText = a != null ? (q.Choices.FirstOrDefault(ch => ch.ScoreValue == (a.Score ?? 0))?.ChoiceText ?? "") : "";
                        }
                        else
                        {
                            if (specializedMaxScores.TryGetValue(q.ID, out int maxSpec))
                            {
                                score = maxSpec;
                            }
                            choiceText = q.Choices.FirstOrDefault(ch => ch.ScoreValue == score)?.ChoiceText ?? "";
                            if (score > 0)
                            {
                                choiceText = string.IsNullOrEmpty(choiceText)
                                    ? "(Inherited from specialized department)"
                                    : choiceText + " (Inherited from specialized department)";
                            }
                        }

                        return new
                        {
                            QuestionText = q.QuestionText,
                            Weight = q.Weight,
                            Score = score,
                            ChoiceText = choiceText
                        };
                    }).ToList();

                    return new
                    {
                        CriteriaName = c.Name,
                        CriteriaWeight = c.Weight,
                        Description = c.Description,
                        AverageScore = d != null ? d.AverageScore : 0m,
                        Notes = d != null ? d.Notes : "",
                        Questions = qDetails
                    };
                }).ToList();

                return Json(new
                {
                    status = "success",
                    assessorName = assessment.AssessorName,
                    submittedAt = assessment.Submitted_At?.ToString("dd MMM yyyy, HH:mm"),
                    isImpacted = assessment.IsImpacted,
                    impactDescription = assessment.ImpactDescription,
                    totalScore = assessment.TotalScore,
                    details = details
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveRecoveryPlan(int? ID, int IncidentID, int CriteriaID, string ActionPlanText)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    // Validation: only users with IsAllowedSubmitReview = true
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isReviewer) return Json(new { status = "error", message = "Unauthorized." });

                    HC_BCP_Recovery_Plan plan;
                    if (ID.HasValue && ID.Value > 0)
                    {
                        plan = db.HC_BCP_Recovery_Plan.Find(ID.Value);
                        if (plan == null) return Json(new { status = "error", message = "Recovery plan not found." });

                        if (!string.Equals((plan.Created_By ?? "").Trim(), currUserNik, StringComparison.OrdinalIgnoreCase))
                        {
                            return Json(new { status = "error", message = "Anda tidak berhak mengubah action plan ini karena dibuat oleh user lain." });
                        }

                        if (plan.Status == "Submitted" || plan.Status == "Approved")
                        {
                            return Json(new { status = "error", message = "Action plan tidak dapat diubah karena statusnya saat ini: " + plan.Status });
                        }

                        plan.CriteriaID = CriteriaID;
                        plan.ActionPlanText = ActionPlanText;
                        plan.Updated_At = DateTime.Now;
                        plan.Updated_By = currUserNik;
                        db.Entry(plan).State = EntityState.Modified;
                    }
                    else
                    {
                        plan = new HC_BCP_Recovery_Plan
                        {
                            IncidentID = IncidentID,
                            CriteriaID = CriteriaID,
                            ActionPlanText = ActionPlanText,
                            Created_At = DateTime.Now,
                            Created_By = currUserNik,
                            Status = "Draft"
                        };
                        db.HC_BCP_Recovery_Plan.Add(plan);
                    }

                    db.SaveChanges();
                    return Json(new { status = "success", message = "Action plan saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SaveRecoveryTodo(int? ID, int RecoveryPlanID, string TodoText, string PIC_NIK, string DueDate)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                DateTime parsedDueDate;
                if (!DateTime.TryParse(DueDate, out parsedDueDate))
                {
                    return Json(new { status = "error", message = "Invalid Due Date format." });
                }

                using (var db = new BCPConnection())
                {
                    // Validation: only users with IsAllowedSubmitReview = true
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isReviewer) return Json(new { status = "error", message = "Unauthorized." });

                    // Find PIC Name dynamically
                    var picMember = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == PIC_NIK.Trim() && x.IsActive);
                    string picName = picMember != null ? picMember.Name : PIC_NIK;

                    HC_BCP_Recovery_Todo todo;
                    if (ID.HasValue && ID.Value > 0)
                    {
                        todo = db.HC_BCP_Recovery_Todo.Find(ID.Value);
                        if (todo == null) return Json(new { status = "error", message = "To Do item not found." });

                        // Ownership validation: Check parent plan creator NIK
                        var parentPlan = db.HC_BCP_Recovery_Plan.Find(todo.RecoveryPlanID);
                        if (parentPlan == null) return Json(new { status = "error", message = "Parent action plan not found." });
                        if (!string.Equals((parentPlan.Created_By ?? "").Trim(), currUserNik, StringComparison.OrdinalIgnoreCase))
                        {
                            return Json(new { status = "error", message = "Anda tidak berhak mengubah to do list pada action plan ini karena action plan ini dibuat oleh user lain." });
                        }

                        if (parentPlan.Status == "Submitted")
                        {
                            return Json(new { status = "error", message = "To Do list tidak dapat diubah karena status action plannya: " + parentPlan.Status });
                        }

                        todo.TodoText = TodoText;
                        todo.PIC_NIK = PIC_NIK.Trim();
                        todo.PIC_Name = picName;
                        todo.DueDate = parsedDueDate;
                        todo.Updated_At = DateTime.Now;
                        todo.Updated_By = currUserNik;
                        db.Entry(todo).State = EntityState.Modified;
                    }
                    else
                    {
                        // Ownership validation: Check parent plan creator NIK
                        var parentPlan = db.HC_BCP_Recovery_Plan.Find(RecoveryPlanID);
                        if (parentPlan == null) return Json(new { status = "error", message = "Parent action plan not found." });
                        if (!string.Equals((parentPlan.Created_By ?? "").Trim(), currUserNik, StringComparison.OrdinalIgnoreCase))
                        {
                            return Json(new { status = "error", message = "Anda tidak berhak menambah to do list pada action plan ini karena action plan ini dibuat oleh user lain." });
                        }

                        if (parentPlan.Status == "Submitted")
                        {
                            return Json(new { status = "error", message = "Tidak dapat menambah To Do list karena status action plannya: " + parentPlan.Status });
                        }

                        todo = new HC_BCP_Recovery_Todo
                        {
                            RecoveryPlanID = RecoveryPlanID,
                            TodoText = TodoText,
                            PIC_NIK = PIC_NIK.Trim(),
                            PIC_Name = picName,
                            DueDate = parsedDueDate,
                            Status = "On Progress",
                            Created_At = DateTime.Now,
                            Created_By = currUserNik
                        };
                        db.HC_BCP_Recovery_Todo.Add(todo);
                    }

                    db.SaveChanges();
                    return Json(new { status = "success", message = "To Do item saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UpdateTodoStatus(int TodoID, string Status, string PendingNote = null, string DueDate = null)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    var todo = db.HC_BCP_Recovery_Todo.Find(TodoID);
                    if (todo == null) return Json(new { status = "error", message = "To Do item not found." });

                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isReviewer) return Json(new { status = "error", message = "Unauthorized. Hanya user dengan wewenang Submit Review yang dapat memperbarui progress task." });

                    // Ownership validation: Check parent plan creator NIK
                    var parentPlan = db.HC_BCP_Recovery_Plan.Find(todo.RecoveryPlanID);
                    if (parentPlan == null) return Json(new { status = "error", message = "Parent action plan not found." });

                    if (Status != "On Progress" && Status != "Pending" && Status != "Complete")
                    {
                        return Json(new { status = "error", message = "Invalid status value." });
                    }

                    if (Status == "Pending")
                    {
                        if (string.IsNullOrEmpty(PendingNote))
                        {
                            return Json(new { status = "error", message = "Pending note is required." });
                        }
                        if (string.IsNullOrEmpty(DueDate))
                        {
                            return Json(new { status = "error", message = "New due date is required." });
                        }
                        DateTime parsedDueDate;
                        if (!DateTime.TryParse(DueDate, out parsedDueDate))
                        {
                            return Json(new { status = "error", message = "Invalid Due Date format." });
                        }
                        todo.PendingNote = PendingNote;
                        todo.DueDate = parsedDueDate;
                    }
                    else
                    {
                        todo.PendingNote = null;
                    }

                    todo.Status = Status;
                    todo.Updated_At = DateTime.Now;
                    todo.Updated_By = currUserNik;
                    db.Entry(todo).State = EntityState.Modified;

                    db.SaveChanges();
                    return Json(new { status = "success", newStatus = todo.Status, message = $"Status updated to {todo.Status}." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteRecoveryPlan(int PlanID)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    // Validation: only users with IsAllowedSubmitReview = true
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isReviewer) return Json(new { status = "error", message = "Unauthorized." });

                    var plan = db.HC_BCP_Recovery_Plan.Find(PlanID);
                    if (plan == null) return Json(new { status = "error", message = "Recovery plan not found." });

                    // Ownership validation: Check plan creator NIK
                    if (!string.Equals((plan.Created_By ?? "").Trim(), currUserNik, StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { status = "error", message = "Anda tidak berhak menghapus action plan ini karena dibuat oleh user lain." });
                    }

                    if (plan.Status == "Submitted" || plan.Status == "Approved")
                    {
                        return Json(new { status = "error", message = "Action plan tidak dapat dihapus karena statusnya saat ini: " + plan.Status });
                    }

                    db.HC_BCP_Recovery_Plan.Remove(plan);
                    db.SaveChanges();
                    return Json(new { status = "success", message = "Action plan deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteRecoveryTodo(int TodoID)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    // Validation: only users with IsAllowedSubmitReview = true
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isReviewer) return Json(new { status = "error", message = "Unauthorized." });

                    var todo = db.HC_BCP_Recovery_Todo.Find(TodoID);
                    if (todo == null) return Json(new { status = "error", message = "To Do item not found." });

                    // Ownership validation: Check parent plan creator NIK
                    var parentPlan = db.HC_BCP_Recovery_Plan.Find(todo.RecoveryPlanID);
                    if (parentPlan == null) return Json(new { status = "error", message = "Parent action plan not found." });
                    if (!string.Equals((parentPlan.Created_By ?? "").Trim(), currUserNik, StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { status = "error", message = "Anda tidak berhak menghapus to do list pada action plan ini karena action plan ini dibuat oleh user lain." });
                    }

                    if (parentPlan.Status == "Submitted")
                    {
                        return Json(new { status = "error", message = "To Do item tidak dapat dihapus karena status action plannya: " + parentPlan.Status });
                    }

                    db.HC_BCP_Recovery_Todo.Remove(todo);
                    db.SaveChanges();
                    return Json(new { status = "success", message = "To Do item deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SubmitRecoveryPlan(int PlanID)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isReviewer) return Json(new { status = "error", message = "Unauthorized." });

                    var plan = db.HC_BCP_Recovery_Plan.Include(p => p.Todos).FirstOrDefault(p => p.ID == PlanID);
                    if (plan == null) return Json(new { status = "error", message = "Recovery plan not found." });

                    if (!string.Equals((plan.Created_By ?? "").Trim(), currUserNik, StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { status = "error", message = "Anda tidak berhak submit action plan ini karena dibuat oleh user lain." });
                    }

                    if (plan.Status != "Draft" && plan.Status != "Revised")
                    {
                        return Json(new { status = "error", message = "Hanya action plan dengan status Draft atau Revised yang bisa disubmit." });
                    }

                    if (plan.Todos == null || !plan.Todos.Any())
                    {
                        return Json(new { status = "error", message = "Action plan harus memiliki minimal 1 item To Do List sebelum disubmit." });
                    }

                    plan.Status = "Submitted";
                    plan.Updated_At = DateTime.Now;
                    plan.Updated_By = currUserNik;
                    db.Entry(plan).State = EntityState.Modified;

                    db.SaveChanges();
                    return Json(new { status = "success", message = "Action plan submitted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SubmitAllRecoveryPlans(int IncidentID)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isReviewer = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedReview && p.IsAllowedSubmitReview &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isReviewer) return Json(new { status = "error", message = "Unauthorized." });

                    var plans = db.HC_BCP_Recovery_Plan.Include(p => p.Todos)
                                  .Where(p => p.IncidentID == IncidentID && 
                                              (p.Status == "Draft" || p.Status == "Revised" || string.IsNullOrEmpty(p.Status)))
                                  .ToList();

                    // Filter only owned plans
                    var ownedPlans = plans.Where(p => string.Equals((p.Created_By ?? "").Trim(), currUserNik, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (!ownedPlans.Any())
                    {
                        return Json(new { status = "error", message = "Tidak ada action plan berstatus Draft/Revised milik Anda yang dapat disubmit." });
                    }

                    // Check that all owned plans have at least one todo list item
                    foreach (var plan in ownedPlans)
                    {
                        if (plan.Todos == null || !plan.Todos.Any())
                        {
                            return Json(new { status = "error", message = $"Action Plan '{plan.ActionPlanText}' harus memiliki minimal 1 item To Do List sebelum disubmit." });
                        }
                    }

                    foreach (var plan in ownedPlans)
                    {
                        plan.Status = "Submitted";
                        plan.Updated_At = DateTime.Now;
                        plan.Updated_By = currUserNik;
                        db.Entry(plan).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    return Json(new { status = "success", message = $"{ownedPlans.Count} Action plan(s) submitted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        private void SendEmailsForApprovedPlans(int incidentId, List<int> planIds)
        {
            try
            {
                using (var localBcpDb = new BCPConnection())
                {
                    var incident = localBcpDb.HC_BCP_Incident.FirstOrDefault(i => i.ID == incidentId);
                    if (incident == null) return;

                    var plans = localBcpDb.HC_BCP_Recovery_Plan
                                         .Include(p => p.Criteria)
                                         .Include(p => p.Todos)
                                         .Where(p => planIds.Contains(p.ID))
                                         .ToList();
                    if (!plans.Any()) return;

                    var allTodos = plans.SelectMany(p => p.Todos ?? new List<HC_BCP_Recovery_Todo>()).ToList();
                    if (!allTodos.Any()) return;

                    var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "BCP Command Center");
                    var password = "100%NGKbusi!";
                    
                    var smtp = new SmtpClient
                    {
                        Host = "ngkbusi.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(senderEmail.Address, password)
                    };

                    var allPortalUsers = this.db.V_Users_Active.AsNoTracking().ToList();

                    string baseUrl = "http://localhost/ngk-portal";
                    try
                    {
                        if (Request != null && Request.Url != null)
                        {
                            baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                        }
                    }
                    catch { }
                    
                    var dashboardUrl = baseUrl + Url.Action("Dashboard", "BCP", new { area = "HC" });

                    // Group tasks by PIC_NIK to send a single combined email per user
                    var groupedTodos = allTodos
                                           .Where(t => !string.IsNullOrEmpty(t.PIC_NIK))
                                           .GroupBy(t => t.PIC_NIK.Trim().ToUpper())
                                           .ToList();

                    foreach (var group in groupedTodos)
                    {
                        string picNik = group.Key;
                        var picUser = allPortalUsers.FirstOrDefault(u => !string.IsNullOrEmpty(u.NIK) && u.NIK.Trim().Equals(picNik, StringComparison.OrdinalIgnoreCase));
                        if (picUser == null || string.IsNullOrEmpty(picUser.Email))
                        {
                            try
                            {
                                localBcpDb.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                                {
                                    IncidentID = incidentId,
                                    RecipientNIK = picNik,
                                    Channel = "Email",
                                    Message = $"Skipped sending to NIK {picNik}: PIC profile or email is missing.",
                                    IsSent = false,
                                    SentAt = DateTime.Now
                                });
                                localBcpDb.SaveChanges();
                            }
                            catch { }
                            continue;
                        }

                        // Build tasks list table HTML
                        var tasksHtml = "";
                        foreach (var todo in group)
                        {
                            bool isOverdue = todo.DueDate < DateTime.Today;
                            string dateColor = isOverdue ? "#e11d48" : "#64748b";
                            string overdueBadge = isOverdue ? @"<span style=""background-color: #ffe4e6; color: #b91c1c; padding: 2px 6px; font-size: 9px; border-radius: 4px; font-weight: bold; margin-left: 5px;"">OVERDUE</span>" : "";
                            
                            var p = plans.FirstOrDefault(plan => plan.ID == todo.RecoveryPlanID);
                            string critName = p?.Criteria?.Name ?? "-";
                            string actText = p?.ActionPlanText ?? "-";

                            tasksHtml += $@"
                                <tr style=""border-bottom: 1px solid #f1f5f9;"">
                                    <td style=""padding: 10px 0; vertical-align: top; font-weight: bold; color: #007581;"">{critName}</td>
                                    <td style=""padding: 10px 10px; vertical-align: top;"">{actText}</td>
                                    <td style=""padding: 10px 10px; vertical-align: top;"">{todo.TodoText}</td>
                                    <td style=""padding: 10px 0; vertical-align: top; text-align: right; color: {dateColor}; white-space: nowrap;"">
                                        {todo.DueDate.ToString("dd MMM yyyy")}{overdueBadge}
                                    </td>
                                </tr>";
                        }

                        try
                        {
                            using (var mess = new MailMessage())
                            {
                                mess.From = senderEmail;
                                mess.To.Add(picUser.Email);
                                mess.Subject = $"[ACTION REQUIRED] BCP Tasks Assigned ({group.Count()} task(s)) — Incident {incident.IncidentNo}";
                                mess.IsBodyHtml = true;
                                mess.Body = $@"
                                    <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; padding: 40px 20px; border-radius: 8px;"">
                                        <div style=""max-width: 750px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05); border: 1px solid #e0f3f5;"">
                                            <div style=""background-color: #007581; padding: 30px; text-align: center;"">
                                                <h1 style=""color: #ffffff; margin: 0; font-size: 24px; text-transform: uppercase; letter-spacing: 2px;"">NEW BCP TASKS</h1>
                                                <p style=""color: #ffdce0; margin: 10px 0 0 0; font-size: 14px;"">Incident Recovery Action Items</p>
                                            </div>
                                            
                                            <div style=""padding: 40px 30px;"">
                                                <p style=""color: #334155; font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">
                                                    Hello <strong>{picUser.Name}</strong>,<br><br>
                                                    You have been designated as the PIC for <strong>{group.Count()}</strong> recovery task(s) for incident <strong>{incident.IncidentNo}</strong>.
                                                </p>
                                                
                                                <div style=""background-color: #e0f3f5; border-left: 4px solid #007581; padding: 15px 20px; margin: 25px 0; border-radius: 4px;"">
                                                    <p style=""margin: 0 0 8px 0; font-size: 14px; color: #00555e;""><strong>Incident No:</strong> {incident.IncidentNo}</p>
                                                    <p style=""margin: 0 0 8px 0; font-size: 14px; color: #00555e;""><strong>Incident Title:</strong> {incident.Title}</p>
                                                </div>

                                                <h3 style=""color: #00555e; font-size: 14px; border-bottom: 2px solid #e0f3f5; padding-bottom: 8px; margin-bottom: 15px; text-transform: uppercase;"">Your Task List</h3>
                                                
                                                <table style=""width: 100%; border-collapse: collapse; margin-bottom: 30px;"">
                                                    <thead>
                                                        <tr style=""border-bottom: 1px solid #e2e8f0; text-align: left; font-size: 11px; text-transform: uppercase; color: #94a3b8;"">
                                                            <th style=""padding: 8px 0; font-weight: bold; width: 20%;"">Critical Issue</th>
                                                            <th style=""padding: 8px 10px; font-weight: bold; width: 25%;"">Action Plan</th>
                                                            <th style=""padding: 8px 10px; font-weight: bold; width: 40%;"">Task Description</th>
                                                            <th style=""padding: 8px 0; font-weight: bold; text-align: right; width: 15%;"">Due Date</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody style=""font-size: 13px; color: #334155;"">
                                                        {tasksHtml}
                                                    </tbody>
                                                </table>

                                                <p style=""color: #64748b; font-size: 14px; margin-bottom: 30px;"">
                                                    Please complete these tasks on or before their respective due dates. You can monitor and view your tasks directly in the BCP Dashboard.
                                                </p>
                                                
                                                <div style=""text-align: center; margin-bottom: 20px;"">
                                                    <a href=""{dashboardUrl}"" 
                                                       style=""background-color: #007581; color: #ffffff; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; box-shadow: 0 4px 6px rgba(0, 117, 129, 0.2);"">
                                                        Access BCP Dashboard
                                                    </a>
                                                </div>
                                            </div>
                                            
                                            <div style=""background-color: #f1f5f9; padding: 20px; text-align: center; border-top: 1px solid #e2e8f0;"">
                                                <p style=""margin: 0; font-size: 12px; color: #94a3b8;"">This is an automated notification from NGK Portal - BCP Module.</p>
                                            </div>
                                        </div>
                                    </div>";

                                if (_enableEmail)
                                {
                                    smtp.Send(mess);
                                }

                                try
                                {
                                    localBcpDb.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                                    {
                                        IncidentID = incidentId,
                                        RecipientNIK = picNik,
                                        Channel = "Email",
                                        Message = $"Combined task notification sent ({group.Count()} task(s)).",
                                        IsSent = true,
                                        SentAt = DateTime.Now
                                    });
                                    localBcpDb.SaveChanges();
                                }
                                catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                localBcpDb.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                                {
                                    IncidentID = incidentId,
                                    RecipientNIK = picNik,
                                    Channel = "Email",
                                    Message = $"Combined task notification failed ({group.Count()} task(s)).",
                                    IsSent = false,
                                    SentAt = DateTime.Now,
                                    ErrorMessage = ex.Message
                                });
                                localBcpDb.SaveChanges();
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    using (var errDb = new BCPConnection())
                    {
                        errDb.HC_BCP_Notification_Log.Add(new HC_BCP_Notification_Log
                        {
                            IncidentID = incidentId,
                            RecipientNIK = "SYSTEM",
                            Channel = "Email",
                            Message = $"Outer exception in SendEmailsForApprovedPlans.",
                            IsSent = false,
                            SentAt = DateTime.Now,
                            ErrorMessage = ex.ToString()
                        });
                        errDb.SaveChanges();
                    }
                }
                catch { }
            }
        }

        [HttpPost]
        public ActionResult ApproveRecoveryPlan(int PlanID)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isPresdir = userBcp != null && userBcp.BCP_Role == "Presdir";
                    bool isDelegated = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedActivate &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isPresdir && !isDelegated) return Json(new { status = "error", message = "Unauthorized. Only Presdir or Authorized Delegate can approve recovery plans." });

                    var plan = db.HC_BCP_Recovery_Plan.Find(PlanID);
                    if (plan == null) return Json(new { status = "error", message = "Recovery plan not found." });

                    if (plan.Status != "Submitted")
                    {
                        return Json(new { status = "error", message = "Hanya action plan dengan status Submitted yang dapat disetujui." });
                    }

                    plan.Status = "Approved";
                    plan.Updated_At = DateTime.Now;
                    plan.Updated_By = currUserNik;
                    db.Entry(plan).State = EntityState.Modified;

                    db.SaveChanges();

                    try
                    {
                        SendEmailsForApprovedPlans(plan.IncidentID, new List<int> { PlanID });
                    }
                    catch { }

                    return Json(new { status = "success", message = "Action plan approved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ReviseRecoveryPlan(int PlanID, string RevisionComment)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                if (string.IsNullOrEmpty(RevisionComment))
                {
                    return Json(new { status = "error", message = "Revision comment is required." });
                }

                using (var db = new BCPConnection())
                {
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isPresdir = userBcp != null && userBcp.BCP_Role == "Presdir";
                    bool isDelegated = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedActivate &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isPresdir && !isDelegated) return Json(new { status = "error", message = "Unauthorized. Only Presdir or Authorized Delegate can request revisions." });

                    var plan = db.HC_BCP_Recovery_Plan.Find(PlanID);
                    if (plan == null) return Json(new { status = "error", message = "Recovery plan not found." });

                    if (plan.Status != "Submitted")
                    {
                        return Json(new { status = "error", message = "Hanya action plan dengan status Submitted yang dapat direvisi." });
                    }

                    plan.Status = "Revised";
                    plan.RevisionComment = RevisionComment;
                    plan.Updated_At = DateTime.Now;
                    plan.Updated_By = currUserNik;
                    db.Entry(plan).State = EntityState.Modified;

                    db.SaveChanges();
                    return Json(new { status = "success", message = "Revision requested successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ApproveAllRecoveryPlans(int IncidentID)
        {
            try
            {
                string currUserNik = User.Identity.GetUserId() ?? User.Identity.Name;
                if (currUserNik.Contains("\\")) { currUserNik = currUserNik.Split('\\').Last(); }
                currUserNik = (currUserNik ?? "").Trim();

                using (var db = new BCPConnection())
                {
                    var userBcp = db.HC_BCP_Organization.FirstOrDefault(x => x.NIK.Trim() == currUserNik && x.IsActive);
                    bool isPresdir = userBcp != null && userBcp.BCP_Role == "Presdir";
                    bool isDelegated = userBcp != null && db.HC_BCP_Role_Permission.Any(p =>
                        p.IsAllowedActivate &&
                        ((p.RoleName != null && p.RoleName == userBcp.BCP_Role) ||
                         (p.UserNIK != null && p.UserNIK == userBcp.NIK.Trim())));

                    if (!isPresdir && !isDelegated) return Json(new { status = "error", message = "Unauthorized. Only Presdir or Authorized Delegate can approve recovery plans." });

                    var plans = db.HC_BCP_Recovery_Plan.Where(p => p.IncidentID == IncidentID && p.Status == "Submitted").ToList();
                    
                    if (!plans.Any())
                    {
                        return Json(new { status = "error", message = "Tidak ada action plan dengan status Submitted yang perlu disetujui." });
                    }

                    foreach (var plan in plans)
                    {
                        plan.Status = "Approved";
                        plan.Updated_At = DateTime.Now;
                        plan.Updated_By = currUserNik;
                        db.Entry(plan).State = EntityState.Modified;
                    }

                    db.SaveChanges();

                    try
                    {
                        var planIds = plans.Select(p => p.ID).ToList();
                        SendEmailsForApprovedPlans(IncidentID, planIds);
                    }
                    catch { }

                    return Json(new { status = "success", message = $"{plans.Count} Action plan(s) approved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }


        // GET: HC/BCP/RolePermission
        public ActionResult RolePermission()
        {
            ViewBag.Title = "Role Permissions";
            using (var db = new BCPConnection())
            {
                ViewBag.Roles = db.HC_BCP_Organization
                    .Where(x => x.IsActive && x.BCP_Role != null && x.BCP_Role != "")
                    .Select(x => x.BCP_Role)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                ViewBag.Users = db.HC_BCP_Organization
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToList();
            }
            return View();
        }

        [HttpGet]
        public JsonResult GetRolePermissionList(string search)
        {
            try
            {
                using (var db = new BCPConnection())
                {
                    var org = db.HC_BCP_Organization.Where(x => x.IsActive).ToList();
                    var perms = db.HC_BCP_Role_Permission.ToList();

                    var data = perms.Select(p => {
                        string targetName = "-";
                        if (!string.IsNullOrEmpty(p.UserNIK))
                        {
                            var u = org.FirstOrDefault(x => x.NIK.Trim() == p.UserNIK.Trim());
                            targetName = u != null ? u.Name : p.UserNIK;
                        }
                        return new {
                            p.ID,
                            p.RoleName,
                            p.UserNIK,
                            TargetName = targetName,
                            p.IsAllowedReview,
                            p.IsAllowedSubmitReview,
                            p.IsAllowedActivate,
                            p.IsAllowedManageBCP,
                            Created_At = p.Created_At.HasValue ? p.Created_At.Value.ToString("yyyy-MM-dd HH:mm") : "-"
                        };
                    }).ToList();

                    if (!string.IsNullOrEmpty(search))
                    {
                        search = search.ToLower();
                        data = data.Where(x => 
                            (x.RoleName != null && x.RoleName.ToLower().Contains(search)) ||
                            (x.UserNIK != null && x.UserNIK.ToLower().Contains(search)) ||
                            (x.TargetName != null && x.TargetName.ToLower().Contains(search))
                        ).ToList();
                    }

                    return Json(new { status = "success", data = data }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveRolePermission(HC_BCP_Role_Permission model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.RoleName) && string.IsNullOrEmpty(model.UserNIK))
                {
                    return Json(new { status = "error", message = "Role Name atau User NIK harus diisi." });
                }

                using (var db = new BCPConnection())
                {
                    HC_BCP_Role_Permission item;
                    
                    if (model.ID == 0)
                    {
                        bool duplicate = false;
                        if (!string.IsNullOrEmpty(model.RoleName))
                        {
                            duplicate = db.HC_BCP_Role_Permission.Any(p => p.RoleName == model.RoleName);
                        }
                        else if (!string.IsNullOrEmpty(model.UserNIK))
                        {
                            duplicate = db.HC_BCP_Role_Permission.Any(p => p.UserNIK == model.UserNIK);
                        }

                        if (duplicate)
                        {
                            return Json(new { status = "error", message = "Aturan izin untuk Role atau User NIK ini sudah ada." });
                        }

                        item = new HC_BCP_Role_Permission
                        {
                            RoleName = model.RoleName,
                            UserNIK = model.UserNIK,
                            IsAllowedReview = model.IsAllowedReview,
                            IsAllowedSubmitReview = model.IsAllowedSubmitReview,
                            IsAllowedActivate = model.IsAllowedActivate,
                            IsAllowedManageBCP = model.IsAllowedManageBCP,
                            Created_At = DateTime.Now
                        };
                        db.HC_BCP_Role_Permission.Add(item);
                    }
                    else
                    {
                        bool duplicate = false;
                        if (!string.IsNullOrEmpty(model.RoleName))
                        {
                            duplicate = db.HC_BCP_Role_Permission.Any(p => p.RoleName == model.RoleName && p.ID != model.ID);
                        }
                        else if (!string.IsNullOrEmpty(model.UserNIK))
                        {
                            duplicate = db.HC_BCP_Role_Permission.Any(p => p.UserNIK == model.UserNIK && p.ID != model.ID);
                        }

                        if (duplicate)
                        {
                            return Json(new { status = "error", message = "Aturan izin untuk Role atau User NIK ini sudah ada." });
                        }

                        item = db.HC_BCP_Role_Permission.Find(model.ID);
                        if (item == null) return Json(new { status = "error", message = "Data tidak ditemukan." });

                        item.RoleName = model.RoleName;
                        item.UserNIK = model.UserNIK;
                        item.IsAllowedReview = model.IsAllowedReview;
                        item.IsAllowedSubmitReview = model.IsAllowedSubmitReview;
                        item.IsAllowedActivate = model.IsAllowedActivate;
                        item.IsAllowedManageBCP = model.IsAllowedManageBCP;
                        db.Entry(item).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    return Json(new { status = "success", message = "Permission rules saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteRolePermission(int ID)
        {
            try
            {
                using (var db = new BCPConnection())
                {
                    var item = db.HC_BCP_Role_Permission.Find(ID);
                    if (item == null) return Json(new { status = "error", message = "Data tidak ditemukan." });

                    // Check if role name is assigned to any active user/employee
                    if (!string.IsNullOrEmpty(item.RoleName))
                    {
                        string roleNameClean = item.RoleName.Trim().ToLower();
                        bool isAssigned = db.HC_BCP_Organization.Any(x => x.IsActive && x.BCP_Role != null && x.BCP_Role.Trim().ToLower() == roleNameClean);
                        if (isAssigned)
                        {
                            return Json(new { status = "error", message = "Role ini tidak dapat dihapus karena masih digunakan oleh user/karyawan aktif." });
                        }
                    }

                    db.HC_BCP_Role_Permission.Remove(item);
                    db.SaveChanges();
                    return Json(new { status = "success", message = "Permission rule deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        private List<string> GetSubordinateNiks(int parentId, BCPConnection db)
        {
            var subs = db.HC_BCP_Organization.Where(x => x.ParentID == parentId && x.IsActive).ToList();
            var niks = subs.Select(x => x.NIK.Trim()).ToList();

            foreach (var sub in subs)
            {
                niks.AddRange(GetSubordinateNiks(sub.ID, db));
            }

            return niks.Distinct().ToList();
        }

        private static bool IsAreaAuthorizedForQuestion(HC_BCP_Assessment_Question q, string areaDeptCode)
        {
            if (q == null || string.IsNullOrWhiteSpace(q.AllowedDepartments))
                return true; // Open to all

            if (string.IsNullOrWhiteSpace(areaDeptCode))
                return false;

            var allowedList = q.AllowedDepartments
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .ToList();

            if (!allowedList.Any())
                return true;

            return allowedList.Any(d => string.Equals(d, areaDeptCode.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private List<NGKBusi.Areas.HC.Models.BCPAssessmentVM> ComputeIncidentEffectiveAssessments(int incidentId, BCPConnection db, List<string> subNiks = null)
        {
            var rawAssessments = db.HC_BCP_Assessment.Where(x => x.IncidentID == incidentId && x.Status >= 1).ToList();
            var departments = db.HC_BCP_Department.Where(x => x.IsActive).OrderBy(x => x.ID).ToList();
            var areas = db.HC_BCP_Area.ToList();
            var allCriteria = db.HC_BCP_Assessment_Criteria.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
            var allQuestions = db.HC_BCP_Assessment_Question.Include(x => x.Choices).Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();

            var assessmentIds = rawAssessments.Select(a => a.ID).ToList();
            var allAnswers = db.HC_BCP_Assessment_Answer.Where(a => assessmentIds.Contains(a.AssessmentID)).ToList();
            var allDetails = db.HC_BCP_Assessment_Detail.Where(d => assessmentIds.Contains(d.AssessmentID)).ToList();

            if (subNiks == null) subNiks = new List<string>();

            // 1. Calculate highest score for each restricted question among authorized departments
            var specializedMaxScores = new Dictionary<int, int>();
            foreach (var q in allQuestions)
            {
                if (!string.IsNullOrWhiteSpace(q.AllowedDepartments))
                {
                    int maxScore = 0;
                    foreach (var ass in rawAssessments)
                    {
                        var ar = areas.FirstOrDefault(a => a.ID == ass.AreaID);
                        string arDept = ar != null ? (ar.DepartmentCode ?? "").Trim() : "";
                        if (IsAreaAuthorizedForQuestion(q, arDept))
                        {
                            var ans = allAnswers.FirstOrDefault(a => a.AssessmentID == ass.ID && a.QuestionID == q.ID);
                            int s = ans?.Score ?? 0;
                            if (s > maxScore) maxScore = s;
                        }
                    }
                    specializedMaxScores[q.ID] = maxScore;
                }
            }

            // 2. Build VM and compute effective category scores per area
            var list = new List<NGKBusi.Areas.HC.Models.BCPAssessmentVM>();
            foreach (var a in rawAssessments)
            {
                var area = areas.FirstOrDefault(ar => ar.ID == a.AreaID);
                string areaDept = area != null ? (area.DepartmentCode ?? "").Trim() : "";
                var dept = (area != null && !string.IsNullOrEmpty(area.DepartmentCode)) ? departments.FirstOrDefault(d => d.Code == area.DepartmentCode) : null;

                decimal totalWeighted = 0m;
                var categoryScores = new List<NGKBusi.Areas.HC.Models.BCPCategoryScoreVM>();

                foreach (var c in allCriteria)
                {
                    var qForCrit = allQuestions.Where(q => q.CriteriaID == c.ID).ToList();
                    var existDetail = allDetails.FirstOrDefault(d => d.AssessmentID == a.ID && d.CriteriaID == c.ID);

                    decimal criteriaSumOfWeighted = 0m;
                    decimal totalQWeight = 0m;
                    foreach (var q in qForCrit)
                    {
                        decimal qWeight = (q.Weight.HasValue && q.Weight.Value > 0) ? q.Weight.Value : 100m;
                        int effectiveScore = 0;

                        bool isAuth = IsAreaAuthorizedForQuestion(q, areaDept);
                        if (isAuth)
                        {
                            var ans = allAnswers.FirstOrDefault(ansObj => ansObj.AssessmentID == a.ID && ansObj.QuestionID == q.ID);
                            effectiveScore = ans?.Score ?? 0;
                        }
                        else
                        {
                            // Inherit highest score from authorized department(s)
                            if (specializedMaxScores.TryGetValue(q.ID, out int maxSpec))
                            {
                                effectiveScore = maxSpec;
                            }
                        }

                        criteriaSumOfWeighted += (decimal)effectiveScore * qWeight;
                        totalQWeight += qWeight;
                    }

                    decimal weightedScore = totalQWeight > 0 ? (criteriaSumOfWeighted / totalQWeight) : 0m;
                    int maxScore = (int)Math.Round(weightedScore);

                    totalWeighted += weightedScore * (c.Weight ?? 0) / 100m;

                    categoryScores.Add(new NGKBusi.Areas.HC.Models.BCPCategoryScoreVM
                    {
                        CriteriaID = c.ID,
                        CriteriaName = c.Name,
                        CriteriaWeight = c.Weight ?? 0,
                        MaxScore = maxScore,
                        WeightedScore = weightedScore,
                        Notes = existDetail != null ? (existDetail.Notes ?? "") : ""
                    });
                }

                list.Add(new NGKBusi.Areas.HC.Models.BCPAssessmentVM
                {
                    ID = a.ID,
                    AssessorNIK = a.AssessorNIK,
                    AssessorName = a.AssessorName,
                    DepartmentName = a.DepartmentName,
                    IsImpacted = a.IsImpacted,
                    TotalScore = totalWeighted,
                    AreaID = a.AreaID,
                    DepartmentID = dept?.ID,
                    DepartmentCode = dept?.Code ?? "Unknown",
                    AreaName = area?.AreaName ?? "Unknown Area",
                    IsMySubordinate = subNiks.Contains((a.AssessorNIK ?? "").Trim()),
                    CategoryScores = categoryScores
                });
            }

            return list;
        }

        private void SyncIncidentAssessmentDetails(int incidentId, BCPConnection db)
        {
            var effectiveList = ComputeIncidentEffectiveAssessments(incidentId, db);
            foreach (var item in effectiveList)
            {
                var ass = db.HC_BCP_Assessment.FirstOrDefault(x => x.ID == item.ID);
                if (ass != null)
                {
                    ass.TotalScore = item.TotalScore;

                    var existingDetails = db.HC_BCP_Assessment_Detail.Where(d => d.AssessmentID == ass.ID).ToList();
                    foreach (var cs in item.CategoryScores)
                    {
                        var det = existingDetails.FirstOrDefault(d => d.CriteriaID == cs.CriteriaID);
                        if (det != null)
                        {
                            det.MaxScore = cs.MaxScore;
                            det.AverageScore = cs.WeightedScore;
                        }
                        else
                        {
                            db.HC_BCP_Assessment_Detail.Add(new HC_BCP_Assessment_Detail
                            {
                                AssessmentID = ass.ID,
                                CriteriaID = cs.CriteriaID,
                                MaxScore = cs.MaxScore,
                                AverageScore = cs.WeightedScore,
                                Notes = cs.Notes ?? ""
                            });
                        }
                    }
                }
            }
            db.SaveChanges();
        }
    }
}
