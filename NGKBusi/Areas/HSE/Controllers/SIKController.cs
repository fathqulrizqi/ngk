using Microsoft.AspNet.Identity;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.HSE.Controllers
{
    public class SIKController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: HSE/SIK
        [Authorize]
        public ActionResult Index(int SIK = 0)
        {

            ViewBag.NavHide = true;
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            ViewBag.SIKList = db.HSE_SIK_Form.ToList();
            ViewBag.FormSIK = db.HSE_SIK_Form.Where(w => w.ID == SIK).OrderByDescending(o => o.ID).FirstOrDefault();


            return View();
        }
        [Authorize]
        public ActionResult formDaily(int SIK = 0, string SID = "")
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            ViewBag.NavHide = true;
            //int? sikInt = sik.Length > 0 ? int.Parse(sik) : 0;
            string[] excludeSection = { "SERIKAT PEKERJA", "SC Reward", "CLINIC", "KOPERASI", "BOD IDL", "BOD ADM", "FACTORY OFFICE" };
            ViewBag.Division = db.V_Users_Active.Where(w => !excludeSection.Contains(w.DivisionName)).OrderBy(o => o.DivisionName).Select(s => s.DivisionName).Distinct();
            ViewBag.ThirdParty = db.V_AXVendorList.ToList();
            if (SID != "")
            {
                ViewBag.FormDaily = db.HSE_SIK_Form_Daily.Where(w => w.SIK_ID == SIK && w.SID == SID).OrderByDescending(o => o.ID).FirstOrDefault();
            }
            else
            {
                //var latestProject = db.HSE_SIK_Form.Where(w => w.Created_By == currUserID && w.Approval < 4).OrderByDescending(o => o.ID).FirstOrDefault();
                var latestProject = db.HSE_SIK_Form.Where(w => w.Created_By == currUserID && w.ID == SIK).OrderByDescending(o => o.ID).FirstOrDefault()?.ID ?? 0;
                //ViewBag.FormDaily = db.HSE_SIK_Form_Daily.Where(w => w.Created_By == currUserID && w.SIK_ID == latestProject.ID).OrderByDescending(o => o.ID).FirstOrDefault();
                ViewBag.FormDaily = db.HSE_SIK_Form_Daily.Where(w => w.Created_By == currUserID && w.SIK_ID == latestProject).OrderByDescending(o => o.ID).FirstOrDefault();
            }

            ViewBag.SIDList = db.HSE_SIK_Form_Daily.ToList();
            ViewBag.ActiveList = db.HSE_SIK_Form.Where(w => w.Created_By == currUserID && w.Approval < 4).OrderBy(o => o.ID).ToList();

            return View();
        }

        public ActionResult dailyCheck(int FDID = 0, int FDCID = 0)
        {

            ViewBag.NavHide = false;
            ViewBag.dailyCheck = db.HSE_SIK_Form_Daily_Check.Where(w => w.Daily_Form_ID == FDID && w.ID == FDCID).OrderByDescending(o => o.ID).FirstOrDefault();
            ViewBag.SIDList = db.HSE_SIK_Form_Daily.ToList();

            return View();
        }

        public ActionResult workerCheck(int SIK = 0)
        {

            ViewBag.NavHide = true;
            ViewBag.FormSIK = db.HSE_SIK_Form.Where(w => w.ID == SIK).OrderByDescending(o => o.ID).FirstOrDefault();
            ViewBag.SIDList = db.HSE_SIK_Form_Daily.ToList();


            return View();
        }
        
        public ActionResult finishCheck(int SIK = 0)
        {
            ViewBag.NavHide = true;
            ViewBag.FormSIK = db.HSE_SIK_Form.Where(w => w.ID == SIK).OrderByDescending(o => o.ID).FirstOrDefault();
            ViewBag.SIKList = db.HSE_SIK_Form.Where(w => w.Approval >= 3).ToList();

            return View();
        }

        public String getSequence(string type)
        {
            var lastSeq = "";
            var seqHeader = type + "-" + DateTime.Now.ToString("yy");

            var latestSequence = db.HSE_SIK_Form.Where(w => w.Number.Substring(0, 6) == seqHeader).OrderByDescending(o => o.ID).Select(s => s.Number.Substring(s.Number.Length - 4, 4)).FirstOrDefault();
            lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
            lastSeq = seqHeader + lastSeq.Substring(lastSeq.Length - 4, 4);

            return lastSeq;
        }

        public String RiskClass(string type)
        {
            var lastSeq = "A";
            switch (type)
            {
                case "A":
                    lastSeq = "A (High Risk)";
                    break;
                case "B":
                    lastSeq = "B (Medium Risk)";
                    break;
                default:
                    lastSeq = "C (Low Risk)";
                    break;
            }
            return lastSeq;
        }
        public String ApprovalStatus(int type)
        {
            var stat = "Submitted";
            switch (type)
            {
                case 1:
                    stat = "Submitted";
                    break;
                case 2:
                    stat = "Review By HSE";
                    break;
                case 3:
                    stat = "Approved By HSE";
                    break;
                default:
                    stat = "Done";
                    break;
            }

            return stat;
        }
        public String SIKFileUpload(string dataid, string type)
        {

            var folderPath = Path.Combine(Server.MapPath("~/Files/HSE/SIK/FormSubmit/"), dataid);
            Directory.CreateDirectory(folderPath);
            List<string> fileNameList = new List<string>();
            //for (int i = 0; i < Request.Files.Count; i++)
            //{
            //HttpPostedFileBase iFile = Request.Files[i];
            HttpPostedFileBase iFile = Request.Files[type];
            // extract only the filename
            if (iFile.ContentLength > 0)
            {
                var fileName = iFile.FileName;
                fileNameList.Add(fileName.Replace(",", "-"));
                string extension = Path.GetExtension(fileName);
                // store the file inside ~/App_Data/uploads folder
                var path = Path.Combine(Server.MapPath("~/Files/HSE/SIK/FormSubmit/" + dataid), fileName);
                iFile.SaveAs(path);
            }
            //}

            return string.Join(",", fileNameList);
        }
        public ActionResult formSubmitAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var newData = new HSE_SIK_Form();
            newData.Number = this.getSequence("SIK");
            newData.Third_Party_Code = Request["iVendor"].Split('|')[0];
            newData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            newData.Email = Request["iEmail"];
            newData.Phone = Request["iPhone"];
            newData.Is_Risk = Request["iRisk"] == "1" ? true : false;
            newData.Is_Long_Day = Request["iLongDay"] == "1" ? true : false;
            newData.Risk_Class = Request["iRiskClass"];
            newData.Badge_Number = Request["iBadgeNo"];
            newData.Worker_Name = Request["iWorker[]"];
            newData.Worker_Position = Request["iPosition[]"];
            newData.Created_At = DateTime.Now;
            newData.Created_By = currUserID;
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            newData.Is_Reject = false;
            db.HSE_SIK_Form.Add(newData);
            db.SaveChanges();

            newData.KTP = this.SIKFileUpload(newData.ID.ToString(), "iFCKTP[]");
            newData.MCU_Result = this.SIKFileUpload(newData.ID.ToString(), "iMCU[]");
            newData.SOP = this.SIKFileUpload(newData.ID.ToString(), "iSOP[]");
            newData.Expertise_Certificate = this.SIKFileUpload(newData.ID.ToString(), "iExpCertificate[]");
            newData.AK3U_Certificate = this.SIKFileUpload(newData.ID.ToString(), "iAK3U[]");
            newData.List_Equipment = this.SIKFileUpload(newData.ID.ToString(), "iTools[]");
            newData.Job_Safety_Analysis = this.SIKFileUpload(newData.ID.ToString(), "iSafetyAnalysis[]");
            newData.Worker_Attendance = this.SIKFileUpload(newData.ID.ToString(), "iWorkerAttendance[]");
            newData.Worker_Photo = this.SIKFileUpload(newData.ID.ToString(), "iPhoto[]");
            db.SaveChanges();

            return RedirectToAction("Index", "SIK", new { area = "HSE" });
        }
        public ActionResult formSubmitEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var formID = int.Parse(Request["iFormID"]);

            var updateData = db.HSE_SIK_Form.Where(w => w.ID == formID).First();
            updateData.Third_Party_Code = Request["iVendor"].Split('|')[0];
            updateData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            updateData.Email = Request["iEmail"];
            updateData.Phone = Request["iPhone"];
            updateData.Is_Risk = Request["iRisk"] == "1" ? true : false;
            updateData.Is_Long_Day = Request["iLongDay"] == "1" ? true : false;
            updateData.Risk_Class = Request["iRiskClass"];
            updateData.Badge_Number = Request["iBadgeNo"];
            //updateData.Worker_Name = Request["iWorker[]"];
            //updateData.Worker_Position = Request["iPosition[]"];

            updateData.KTP = (Request["iFCKTPList"] + this.SIKFileUpload(updateData.ID.ToString(), "iFCKTP[]")).TrimEnd(',');
            updateData.MCU_Result = (Request["iMCUList"] + this.SIKFileUpload(updateData.ID.ToString(), "iMCU[]")).TrimEnd(',');
            updateData.SOP = (Request["iSOPList"] + this.SIKFileUpload(updateData.ID.ToString(), "iSOP[]")).TrimEnd(',');
            updateData.Expertise_Certificate = (Request["iExpCertificateList"] + this.SIKFileUpload(updateData.ID.ToString(), "iExpCertificate[]")).TrimEnd(',');
            updateData.AK3U_Certificate = (Request["iAK3UList"] + this.SIKFileUpload(updateData.ID.ToString(), "iAK3U[]")).TrimEnd(',');
            updateData.List_Equipment = (Request["iToolsList"] + this.SIKFileUpload(updateData.ID.ToString(), "iTools[]")).TrimEnd(',');
            updateData.Job_Safety_Analysis = (Request["iSafetyAnalysisList"] + this.SIKFileUpload(updateData.ID.ToString(), "iSafetyAnalysis[]")).TrimEnd(',');
            updateData.Worker_Attendance = (Request["iWorkerAttendanceList"] + this.SIKFileUpload(updateData.ID.ToString(), "iWorkerAttendance[]")).TrimEnd(',');
            //updateData.Worker_Photo = this.SIKFileUpload(updateData.ID.ToString(), "iPhoto[]");
            db.SaveChanges();

            return RedirectToAction("Index", "SIK", new { area = "HSE", SIK = updateData.ID.ToString() });
        }


        public ActionResult formDailyAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            //var dtStart = (Request["iDateFrom"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateFrom"] + " " + (Request["iTimeFrom"].Length == 0 ? "00:00" : Request["iTimeFrom"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));
            //var dtEnd = (Request["iDateTo"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateTo"] + " " + (Request["iTimeTo"].Length == 0 ? "00:00" : Request["iTimeTo"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));

            var newData = new HSE_SIK_Form_Daily();
            newData.Third_Party_Code = Request["iVendor"].Split('|')[0];
            newData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            newData.PIC = Request["iPIC"];
            newData.Job = Request["iJob"];
            newData.Location = Request["iLocation"];
            newData.Dept = Request["iDept"];
            newData.Job_Description = Request["iJobDescription"];
            newData.Date_From = Request["iDateFrom"];
            newData.Date_To = Request["iDateTo"];
            newData.Time_From = Request["iTimeFrom"];
            newData.Time_To = Request["iTimeTo"];
            newData.Worker_Name = Request["iWorker[]"];
            newData.Worker_Position = Request["iPosition[]"];
            newData.Job_Field = Request["iJobField[]"];
            newData.Job_Type = Request["iJobType[]"];
            newData.Job_Tools_Name = Request["iToolsName[]"];
            newData.Job_Tools_Qty = Request["iToolsQty[]"];
            newData.Hot_Jobs = Request["iHotJobs[]"];
            newData.Cold_Jobs = Request["iColdJobs[]"];
            newData.Height_Jobs = Request["iHeightJobs[]"];
            newData.Limited_Jobs = Request["iLimitedJobs[]"];
            newData.Lifting_Jobs = Request["iLiftingJobs[]"];
            newData.Digging_Jobs = Request["iDiggingJobs[]"];
            newData.Electric_Jobs = Request["iElectricalJobs[]"];
            newData.B3_Jobs = Request["iB3Jobs[]"];
            newData.Radiation_Jobs = Request["iRadiationJobs[]"];
            newData.Created_At = DateTime.Now;
            newData.Created_By = currUserID;
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            newData.Is_Reject = false;
            db.HSE_SIK_Form_Daily.Add(newData);
            db.SaveChanges();

            return RedirectToAction("formDaily", "SIK", new { area = "HSE" });
        }
        public ActionResult formDailyEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var formID = int.Parse(Request["iFormID"]);
            //var dtStart = (Request["iDateFrom"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateFrom"] + " " + (Request["iTimeFrom"].Length == 0 ? "00:00" : Request["iTimeFrom"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));
            //var dtEnd = (Request["iDateTo"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iDateTo"] + " " + (Request["iTimeTo"].Length == 0 ? "00:00" : Request["iTimeTo"]), "dd-MMM-yyyy HH:mm", CultureInfo.InvariantCulture));

            var updateData = db.HSE_SIK_Form_Daily.Where(w => w.ID == formID).First();
            updateData.Third_Party_Code = Request["iVendor"].Split('|')[0];
            updateData.Third_Party_Name = Request["iVendor"].Split('|')[1];
            updateData.PIC = Request["iPIC"];
            updateData.Job = Request["iJob"];
            updateData.Location = Request["iLocation"];
            updateData.Dept = Request["iDept"];
            updateData.Job_Description = Request["iJobDescription"];
            updateData.Date_From = Request["iDateFrom"];
            updateData.Date_To = Request["iDateTo"];
            updateData.Time_From = Request["iTimeFrom"];
            updateData.Time_To = Request["iTimeTo"];
            updateData.Worker_Name = Request["iWorker[]"];
            updateData.Worker_Position = Request["iPosition[]"];
            updateData.Job_Field = Request["iJobField[]"];
            updateData.Job_Type = Request["iJobType[]"];
            updateData.Job_Tools_Name = Request["iToolsName[]"];
            updateData.Job_Tools_Qty = Request["iToolsQty[]"];
            updateData.Hot_Jobs = Request["iHotJobs[]"];
            updateData.Cold_Jobs = Request["iColdJobs[]"];
            updateData.Height_Jobs = Request["iHeightJobs[]"];
            updateData.Limited_Jobs = Request["iLimitedJobs[]"];
            updateData.Lifting_Jobs = Request["iLiftingJobs[]"];
            updateData.Digging_Jobs = Request["iDiggingJobs[]"];
            updateData.Electric_Jobs = Request["iElectricalJobs[]"];
            updateData.B3_Jobs = Request["iB3Jobs[]"];
            updateData.Radiation_Jobs = Request["iRadiationJobs[]"];
            db.SaveChanges();

            return RedirectToAction("formDaily", "SIK", new { area = "HSE", SID = updateData.ID.ToString() });
        }

        public ActionResult formDailyCheckAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            var newData = new HSE_SIK_Form_Daily_Check();
            newData.Daily_Form_ID = int.Parse(Request["iFDID"]);
            newData.Is_Protected = Request["iQ1"];
            newData.Is_Protected_Note = Request["iQ1Note"];
            newData.Is_Toolsafe = Request["iQ2"];
            newData.Is_Toolsafe_Note = Request["iQ2Note"];
            newData.Is_Risk = Request["iQ3"];
            newData.Is_Risk_Note = Request["iQ3Note"];
            newData.Is_Document = Request["iQ4"];
            newData.Is_Document_Note = Request["iQ4Note"];
            newData.Is_Time = Request["iQ5"];
            newData.Is_Time_Note = Request["iQ5Note"];
            newData.Is_Induction = Request["iQ6"];
            newData.Is_Induction_Note = Request["iQ6Note"];
            newData.Is_Uniform = Request["iQ7"];
            newData.Is_Uniform_Note = Request["iQ7Note"];
            newData.Is_Smoke = Request["iQ8"];
            newData.Is_Smoke_Note = Request["iQ8Note"];
            newData.Is_Waste = Request["iQ9"];
            newData.Is_Waste_Note = Request["iQ9Note"];
            newData.Is_Video = Request["iQ10"];
            newData.Is_Video_Note = Request["iQ10Note"];
            newData.Is_Emergency = Request["iQ11"];
            newData.Is_Emergency_Note = Request["iQ11Note"];
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            db.HSE_SIK_Form_Daily_Check.Add(newData);
            db.SaveChanges();

            newData.Is_Protected_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ1File");
            newData.Is_Toolsafe_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ2File");
            newData.Is_Risk_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ3File");
            newData.Is_Document_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ4File");
            newData.Is_Time_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ5File");
            newData.Is_Induction_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ6File");
            newData.Is_Uniform_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ7File");
            newData.Is_Smoke_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ8File");
            newData.Is_Waste_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ9File");
            newData.Is_Video_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ10File");
            newData.Is_Emergency_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ11File");

            db.SaveChanges();

            return RedirectToAction("dailyCheck", "SIK", new { area = "HSE" ,FDID = newData.Daily_Form_ID, FDCID = newData.ID });
        }

        public ActionResult formDailyCheckEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currID = int.Parse(Request["iFDCID"]);
            var newData = db.HSE_SIK_Form_Daily_Check.Where(w => w.ID == currID).FirstOrDefault();
            newData.Is_Protected = Request["iQ1"];
            newData.Is_Protected_Note = Request["iQ1Note"];
            newData.Is_Toolsafe = Request["iQ2"];
            newData.Is_Toolsafe_Note = Request["iQ2Note"];
            newData.Is_Risk = Request["iQ3"];
            newData.Is_Risk_Note = Request["iQ3Note"];
            newData.Is_Document = Request["iQ4"];
            newData.Is_Document_Note = Request["iQ4Note"];
            newData.Is_Time = Request["iQ5"];
            newData.Is_Time_Note = Request["iQ5Note"];
            newData.Is_Induction = Request["iQ6"];
            newData.Is_Induction_Note = Request["iQ6Note"];
            newData.Is_Uniform = Request["iQ7"];
            newData.Is_Uniform_Note = Request["iQ7Note"];
            newData.Is_Smoke = Request["iQ8"];
            newData.Is_Smoke_Note = Request["iQ8Note"];
            newData.Is_Waste = Request["iQ9"];
            newData.Is_Waste_Note = Request["iQ9Note"];
            newData.Is_Video = Request["iQ10"];
            newData.Is_Video_Note = Request["iQ10Note"];
            newData.Is_Emergency = Request["iQ11"];
            newData.Is_Emergency_Note = Request["iQ11Note"];

            newData.Is_Protected_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ1File");
            newData.Is_Toolsafe_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ2File");
            newData.Is_Risk_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ3File");
            newData.Is_Document_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ4File");
            newData.Is_Time_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ5File");
            newData.Is_Induction_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ6File");
            newData.Is_Uniform_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ7File");
            newData.Is_Smoke_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ8File");
            newData.Is_Waste_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ9File");
            newData.Is_Video_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ10File");
            newData.Is_Emergency_Photo = this.dailyCheckPhotoUpload(newData.ID.ToString(), "iQ11File");

            db.SaveChanges();

            return RedirectToAction("dailyCheck", "SIK", new { area = "HSE", FDID = newData.Daily_Form_ID, FDCID = newData.ID });
        }


        public String dailyCheckPhotoUpload(string dataid, string type)
        {

            var folderPath = Path.Combine(Server.MapPath("~/Files/HSE/SIK/dailyCheck/"), dataid);
            Directory.CreateDirectory(folderPath);
            List<string> fileNameList = new List<string>();
            //for (int i = 0; i < Request.Files.Count; i++)
            //{
            //HttpPostedFileBase iFile = Request.Files[i];
            HttpPostedFileBase iFile = Request.Files[type];
            // extract only the filename
            if (iFile.ContentLength > 0)
            {
                var fileName = iFile.FileName;
                fileNameList.Add(fileName.Replace(",", "-"));
                string extension = Path.GetExtension(fileName);
                // store the file inside ~/App_Data/uploads folder
                var path = Path.Combine(Server.MapPath("~/Files/HSE/SIK/dailyCheck/" + dataid), fileName);
                iFile.SaveAs(path);
            }
            //}


            return string.Join(",", fileNameList);
        }
    }
}