using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.MTN.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.MTN.Controllers
{
    public class MaintenanceReportController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        MachineDatabaseConnection dbM = new MachineDatabaseConnection();
        MaintenanceReportConnection dbMR = new MaintenanceReportConnection();
        // GET: MTN/MaintenanceReport
        [Authorize]
        public ActionResult Index()
        {
            ViewBag.NavHide = true;
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currIssuedNumber = Request["ReqNumber"];
            string[] itemGroup = { "MachineP", "Tooling", "Misc" };
            ViewBag.SparepartList = db.V_AXItemMaster.Where(w => itemGroup.Contains(w.ItemGroup) && (w.ITEMID.Substring(0, 5) == "QIDTL" || w.ITEMID.Substring(0, 5) == "QIDMP" || (w.ItemGroup == "Misc" && w.ITEMID.Substring(0, 5) == "QIDFS") || (w.ItemGroup == "Misc" && w.ITEMID.Substring(0, 4) == "QIDM"))).OrderBy(o => o.ITEMID).ToList();
            ViewBag.machineList = dbM.MTN_MachineDatabase_List.Where(z => z.Is_Scheduled == true).OrderBy(o => o.Name).ToList();
            ViewBag.CurrentUser = db.V_Users_Active.Where(w => w.NIK == currUserID).FirstOrDefault();
            ViewBag.PICList = db.V_Users_Active.Where(w => w.DeptName == "MAINTENANCE").ToList();
            ViewBag.BCategory = dbMR.MTN_MaintenanceReport_Breakdown_Master_List.OrderBy(o => o.Category).Select(s => s.Category).Distinct();
            ViewBag.BSpecific = dbMR.MTN_MaintenanceReport_Breakdown_Master_List.OrderBy(o => o.Category).OrderBy(o => o.Item).ToList();
            ViewBag.Employee = db.V_Users_Active.Where(w => !w.NIK.Contains("NGK") && !w.NIK.Contains("EXP")).ToList();
            var currData = dbMR.MTN_MaintenanceReport_Breakdown_Form.Where(w => w.Issued_Number == currIssuedNumber).FirstOrDefault();
            var currDataID = currData != null ? currData.ID : 0;
            var currDataApproval = currData?.Approval;
            var currDataApprovalSub = currData?.Approval_Sub;
            ViewBag.DataAttachment = dbMR.MTN_MaintenanceReport_Breakdown_Form_Attachment.Where(w => w.Form_ID == currDataID).ToList();
            ViewBag.CurrentData = currData;
            ViewBag.CurrentApproval = db.Approval_Master.Where(w => w.Menu_Id == 91 && w.User_NIK == currUserID && w.Levels == currDataApproval && w.Levels_Sub == currDataApprovalSub).FirstOrDefault();

            ViewBag.BList = dbMR.MTN_MaintenanceReport_Breakdown_Form.ToList();

            return View();
        }

        public ActionResult MTNReportSign()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = Int32.Parse(Request["iMTNReportSignID"]);

            var currApproval = db.Approval_Master.Where(w => w.Menu_Id == 91 && w.User_NIK == currUserID).FirstOrDefault();
            var currData = dbMR.MTN_MaintenanceReport_Breakdown_Form.Where(w => w.ID == currID).First();
            var currLevel = currApproval?.Levels ?? 99;
            var currLevelSub = currApproval?.Levels_Sub ?? 99;
            var currApprovalNext = db.Approval_Master.Where(w => w.Menu_Id == 91 && w.Levels >= currLevel && w.Levels_Sub > currLevelSub).FirstOrDefault();

            if (currApprovalNext == null)
            {
                currData.Approval += 1;
                currData.Approval_Sub = 0;
            }
            else
            {
                currData.Approval_Sub += 1;
            }
            dbMR.SaveChanges();

            return RedirectToAction("Index", "MaintenanceReport", new { area = "MTN", ReqNumber = currData.Issued_Number });
        }

        public String ApprovalStatus(int Approval, int Approval_Sub)
        {
            var stat = "Created";
            switch (Approval)
            {
                case 1:
                    stat = "Created";
                    break;
                case 2:
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Checked";
                            break;
                        default:
                            stat = "Issued";
                            break;
                    }
                    break;
                case 3:
                    stat = "Approved";
                    break;
                default:
                    stat = "Finish";
                    break;
            }
            return stat;
        }

        public ActionResult MTNReportAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var dtIssued = (Request["iDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDate"] + " " + (Request["iTime"].Length == 0 ? "00:00" : Request["iTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtStartDate = (Request["iStartDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iStartDate"] + " " + (Request["iStartTime"].Length == 0 ? "00:00" : Request["iStartTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtEndDate = (Request["iEndDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iEndDate"] + " " + (Request["iEndTime"].Length == 0 ? "00:00" : Request["iEndTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));

            var shift = (dtIssued.Value.Hour >= 6 && dtIssued.Value.Hour < 14 ? "S1" : (dtIssued.Value.Hour >= 14 && dtIssued.Value.Hour < 22 ? "S2" : "S3"));
            var newData = new MTN_MaintenanceReport_Breakdown_Form();
            newData.Issued_Number = this.getSequence(Request["iMachine"].ToString().Split('|')[0], shift);
            newData.Type = Request["iType"];
            newData.Issued_Date = dtIssued;
            newData.Issuer_NIK = Request["iNik"];
            newData.Issuer_Name = Request["iName"];
            newData.Section = Request["iSection"];
            newData.Machine = Request["iMachine"];
            newData.Breakdown_Problem = Request["iBProblem"];
            newData.Breakdown_Condition = Request["iBCondition"];
            newData.Breakdown_Cause = Request["iBCause"];
            newData.Breakdown_Cause_Time = Request["iBCauseTime"].Length != 0 ? Int32.Parse(Request["iBCauseTime"]) : (int?)null;
            newData.Breakdown_Action = Request["iBAction"];
            newData.Breakdown_Action_Time = Request["iBActionTime"].Length != 0 ? Int32.Parse(Request["iBActionTime"]) : (int?)null;
            newData.Start_Date = dtStartDate;
            newData.End_Date = dtEndDate;
            newData.Breakdown_Fix_Result = Request["iFixResult"];
            newData.Breakdown_Fix_PIC = Request["iFixPIC[]"];
            newData.Status = Request["iStatus"];
            newData.Breakdown_Sparepart_Time = Request["iBSparepartTime"].Length != 0 ? Int32.Parse(Request["iBSparepartTime"]) : (int?)null;
            newData.Created_At = DateTime.Now;
            newData.Created_By = currUserID;
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            dbMR.MTN_MaintenanceReport_Breakdown_Form.Add(newData);
            dbMR.SaveChanges();

            if (Request.Form.GetValues("iBSparepart[]")[0].Length > 0)
            {
                for (var i = 0; i <= Request.Form.GetValues("iBSparepart[]").Count() - 1; i++)
                {
                    var newSubData = new MTN_MaintenanceReport_Breakdown_Form_Sparepart();
                    newSubData.Form_ID = newData.ID;
                    newSubData.Sparepart_Code = Request.Form.GetValues("iBSparepart[]")[i].Split('|')[0];
                    newSubData.Sparepart_Name = Request.Form.GetValues("iBSparepart[]")[i].Split('|')[1];
                    newSubData.Breakdown_Factor = Request.Form.GetValues("iBSparepartFactor[]")[i];
                    newSubData.Breakdown_Part = Request.Form.GetValues("iBSparepartCategory[]")[i];
                    newSubData.Breakdown_Specific = Request.Form.GetValues("iBSparepartSpecific[]")[i];
                    newSubData.Qty = Request.Form.GetValues("iBSparepartQty[]")[i].Length != 0 ? Int32.Parse(Request.Form.GetValues("iBSparepartQty[]")[i]) : (int?)null;
                    dbMR.MTN_MaintenanceReport_Breakdown_Form_Sparepart.Add(newSubData);
                }
            }
            dbMR.SaveChanges();

            return RedirectToAction("Index", "MaintenanceReport", new { area = "MTN", ReqNumber = newData.Issued_Number });
        }

        public ActionResult MTNReportEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currDataID = Int32.Parse(Request["iCurrID"]);
            var dtIssued = (Request["iDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDate"] + " " + (Request["iTime"].Length == 0 ? "00:00" : Request["iTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtStartDate = (Request["iStartDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iStartDate"] + " " + (Request["iStartTime"].Length == 0 ? "00:00" : Request["iStartTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtEndDate = (Request["iEndDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iEndDate"] + " " + (Request["iEndTime"].Length == 0 ? "00:00" : Request["iEndTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));

            var shift = (dtIssued.Value.Hour >= 6 && dtIssued.Value.Hour < 14 ? "S1" : (dtIssued.Value.Hour >= 14 && dtIssued.Value.Hour < 22 ? "S2" : "S3"));
            var newData = dbMR.MTN_MaintenanceReport_Breakdown_Form.Where(w => w.ID == currDataID).FirstOrDefault();
            newData.Type = Request["iType"];
            newData.Issued_Date = dtIssued;
            newData.Issuer_NIK = Request["iNik"];
            newData.Issuer_Name = Request["iName"];
            newData.Section = Request["iSection"];
            newData.Machine = Request["iMachine"];
            newData.Breakdown_Problem = Request["iBProblem"];
            newData.Breakdown_Condition = Request["iBCondition"];
            newData.Breakdown_Cause = Request["iBCause"];
            newData.Breakdown_Cause_Time = Request["iBCauseTime"].Length != 0 ? Int32.Parse(Request["iBCauseTime"]) : (int?)null;
            newData.Breakdown_Action = Request["iBAction"];
            newData.Breakdown_Action_Time = Request["iBActionTime"].Length != 0 ? Int32.Parse(Request["iBActionTime"]) : (int?)null;
            newData.Start_Date = dtStartDate;
            newData.End_Date = dtEndDate;
            newData.Breakdown_Fix_Result = Request["iFixResult"];
            newData.Breakdown_Fix_PIC = Request["iFixPIC[]"];
            newData.Status = Request["iStatus"];
            newData.Breakdown_Sparepart_Time = Request["iBSparepartTime"].Length != 0 ? Int32.Parse(Request["iBSparepartTime"]) : (int?)null;
            dbMR.SaveChanges();

            var deleteSparepartData = dbMR.MTN_MaintenanceReport_Breakdown_Form_Sparepart.Where(w => w.Form_ID == currDataID).ToList();
            dbMR.MTN_MaintenanceReport_Breakdown_Form_Sparepart.RemoveRange(deleteSparepartData);
            dbMR.SaveChanges();

            if (Request.Form.GetValues("iBSparepart[]")[0].Length > 0)
            {
                for (var i = 0; i <= Request.Form.GetValues("iBSparepart[]").Count() - 1; i++)
                {
                    if (Request.Form.GetValues("iBSparepart[]")[i] != "")
                    {
                        var newSubData = new MTN_MaintenanceReport_Breakdown_Form_Sparepart();
                        newSubData.Form_ID = newData.ID;
                        newSubData.Sparepart_Code = Request.Form.GetValues("iBSparepart[]")[i].Split('|')[0];
                        newSubData.Sparepart_Name = Request.Form.GetValues("iBSparepart[]")[i].Split('|')[1];
                        newSubData.Breakdown_Factor = Request.Form.GetValues("iBSparepartFactor[]")[i];
                        newSubData.Breakdown_Part = Request.Form.GetValues("iBSparepartCategory[]")[i];
                        newSubData.Breakdown_Specific = Request.Form.GetValues("iBSparepartSpecific[]")[i];
                        newSubData.Qty = Request.Form.GetValues("iBSparepartQty[]")[i].Length != 0 ? Int32.Parse(Request.Form.GetValues("iBSparepartQty[]")[i]) : (int?)null;
                        dbMR.MTN_MaintenanceReport_Breakdown_Form_Sparepart.Add(newSubData);
                    }
                }
            }
            dbMR.SaveChanges();
            uploadPicture();

            return RedirectToAction("Index", "MaintenanceReport", new { area = "MTN", ReqNumber = newData.Issued_Number });
        }


        public String getSequence(string MachineNo, string Shift)
        {
            var lastSeq = "";
            var seqHeader = MachineNo + "/" + DateTime.Now.ToString("ddMMyy") + "/" + Shift;

            var latestSequence = dbMR.MTN_MaintenanceReport_Breakdown_Form.Where(w => w.Issued_Number.Substring(0, seqHeader.Length) == seqHeader).OrderByDescending(o => o.ID).Select(s => s.Issued_Number.Substring(s.Issued_Number.Length - 1, 1)).FirstOrDefault();
            lastSeq = latestSequence != null ? (Int32.Parse(latestSequence) + 1).ToString() : "1";
            lastSeq = seqHeader + "/" + lastSeq;

            return lastSeq;
        }

        public ActionResult MTNReportDownload()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var fileName = "Maintenance Report";

            var path = Server.MapPath("~/Files/MTN/MaintenanceReport/Download/" + fileName + ".xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/MTN/MaintenanceReport/Master/" + fileName + ".xlsx"), path, true);

            var _Data = dbMR.MTN_MaintenanceReport_Breakdown_Form_Sparepart.Where(w => w.Breakdown_Form.Approval == 4).ToList();

            var _DataList = _Data.Select(s => new
            {
                Issued_Number = s.Breakdown_Form?.Issued_Number,
                Issued_Date = s.Breakdown_Form?.Issued_Date,
                Start_Date = s.Breakdown_Form?.Start_Date,
                Investigation_Date = s.Breakdown_Form?.Start_Date.Value.AddMinutes((double)s.Breakdown_Form.Breakdown_Cause_Time),
                Sparepart_Date = s.Breakdown_Form?.Start_Date.Value.AddMinutes((double)s.Breakdown_Form.Breakdown_Cause_Time + (double)s.Breakdown_Form.Breakdown_Sparepart_Time),
                Done_Date = s.Breakdown_Form?.Start_Date.Value.AddMinutes((double)s.Breakdown_Form.Breakdown_Cause_Time + (double)s.Breakdown_Form.Breakdown_Sparepart_Time + (double)s.Breakdown_Form.Breakdown_Action_Time),
                Running_Date = s.Breakdown_Form?.End_Date,
                Date = s.Breakdown_Form?.Issued_Date.Value.ToString("dd/MM/yyyy"),
                Month = s.Breakdown_Form?.Issued_Date.Value.ToString("MM/yy"),
                Shift = (s.Breakdown_Form.Issued_Date.Value.Hour >= 6 && s.Breakdown_Form.Issued_Date.Value.Hour < 14 ? "S1" : (s.Breakdown_Form.Issued_Date.Value.Hour >= 14 && s.Breakdown_Form.Issued_Date.Value.Hour < 22 ? "S2" : "S3")),
                Week = s.Breakdown_Form.Issued_Date.Value.DayOfWeek,
                Machine = s.Breakdown_Form.Machine.Split('|')[1],
                Dept = s.Breakdown_Form.Section,
                Machine_No = s.Breakdown_Form.Machine.Split('|')[0],
                Breakdown_Problem = s.Breakdown_Form.Breakdown_Problem,
                Breakdown_Cause = s.Breakdown_Form.Breakdown_Cause,
                Breakdown_Action = s.Breakdown_Form.Breakdown_Action,
                Breakdown_Factor = s.Breakdown_Factor,
                Breakdown_Part = s.Breakdown_Part,
                Breakdown_Specific = s.Breakdown_Specific,
                Sparepart = s.Sparepart_Code + " | " + s.Sparepart_Name,
                Price = "",
                Qty = s.Qty,
                SubTotal = "",
                Status = s.Breakdown_Form.Status,
                Response_Time = (s.Breakdown_Form?.Start_Date - s.Breakdown_Form.Issued_Date).Value.Minutes,
                Investigate_Time = s.Breakdown_Form.Breakdown_Cause_Time,
                Sparepart_Time = s.Breakdown_Form.Breakdown_Sparepart_Time,
                Fix_Time = s.Breakdown_Form.Breakdown_Action_Time,
                Running_Time = (s.Breakdown_Form?.End_Date - s.Breakdown_Form?.Start_Date.Value.AddMinutes((double)s.Breakdown_Form.Breakdown_Cause_Time + (double)s.Breakdown_Form.Breakdown_Sparepart_Time + (double)s.Breakdown_Form.Breakdown_Action_Time)).Value.Minutes,
                BD_Total_Time = s.Breakdown_Form.Type == "Breakdown" ? (s.Breakdown_Form?.End_Date - s.Breakdown_Form.Issued_Date).Value.Minutes : 0,
                Work_Start = s.Breakdown_Form?.Start_Date.Value.ToString("HH:mm"),
                Work_Finish = s.Breakdown_Form?.End_Date.Value.ToString("HH:mm"),
                Work_Total = (s.Breakdown_Form?.End_Date - s.Breakdown_Form.Issued_Date).Value.Minutes,
                Type = s.Breakdown_Form.Type,
                PIC = s.Breakdown_Form.Breakdown_Fix_PIC,
                PIC_Total = s.Breakdown_Form.Breakdown_Fix_PIC.Split(',').Length,
                Input_Date = s.Breakdown_Form.Created_At
            }).ToList();


            var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(1);
            var ts = ws.Cell(2, 1).InsertData(_DataList);
            workbook.SaveAs(path);

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName + ".xlsx");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("Index", "MaintenanceReport", new { area = "MTN" });
        }

        public void uploadPicture()
        {

            var currFormID = int.Parse(Request["iFormID"]);
            string checkFolder = "~/Files/MTN/MaintenanceReport/Pictures/" + currFormID; // Your code goes here
            bool exists = System.IO.Directory.Exists(Server.MapPath(checkFolder));
            if (!exists)
                System.IO.Directory.CreateDirectory(Server.MapPath(checkFolder));
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase iFile = Request.Files[i];
                // extract only the filename
                if (iFile.ContentLength > 0)
                {
                    var fileName = iFile.FileName;
                    string extension = Path.GetExtension(fileName);
                    // store the file inside ~/App_Data/uploads folder
                    var path = Path.Combine(Server.MapPath("~/Files/MTN/MaintenanceReport/Pictures/" + currFormID), fileName);
                    iFile.SaveAs(path);
                    var checkFile = dbMR.MTN_MaintenanceReport_Breakdown_Form_Attachment.Where(w => w.Form_ID == currFormID && w.Filename == fileName).FirstOrDefault();
                    if (checkFile == null)
                    {
                        dbMR.MTN_MaintenanceReport_Breakdown_Form_Attachment.Add(new MTN_MaintenanceReport_Breakdown_Form_Attachment()
                        {
                            Form_ID = currFormID,
                            Filename = fileName,
                            Ext = extension
                        });
                    }
                }
            }
            dbMR.SaveChanges();
        }

        public ActionResult deleteReportAttachment()
        {
            var reportID = Int32.Parse(Request["iImageID"]);
            var imageURL = Request["iImageURL"];
            var reportList = dbMR.MTN_MaintenanceReport_Breakdown_Form_Attachment.Where(w => w.ID == reportID).First();
            if (System.IO.File.Exists(imageURL))
            {
                System.IO.File.Delete(imageURL);
            }
            dbMR.MTN_MaintenanceReport_Breakdown_Form_Attachment.Remove(reportList);
            dbMR.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }
    }
}