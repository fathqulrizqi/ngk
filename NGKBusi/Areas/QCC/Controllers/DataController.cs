using Highsoft.Web.Mvc.Charts;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.QCC.Models;
using NGKBusi.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using static NGKBusi.Controllers.GlobalController;

namespace NGKBusi.Areas.QCC.Controllers
{
    public class DataController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        QCCConnection dbQCC = new QCCConnection();
        // GET: QCC/Data
        public ActionResult List()
        {
            var currPeriodFY = !String.IsNullOrEmpty(Request["iPeriodFY"]) ? Request["iPeriodFY"] : "";
            var currGroup = !String.IsNullOrEmpty(Request["iGroup"]) ? Request["iGroup"] : "";
            //Foreman up to Asst. Manager
            //string[] facilitator = { "I-C", "III", "III C", "III-A", "IIIA", "IIID", "IV-A", "IV-B", "IV-C", "V", "V-A" };
            string[] facilitator = { "4A", "4B", "4C", "5A", "5B", "5C", "6A", "6B", "6C" };
            //Manager up
            //string[] advisor = { "VI-A", "VI-A1", "VI-B", "VI-B1", "VII-A", "VII-B", "VIII" };
            string[] advisor = { "7A", "7B", "8A", "8B", "9A" };

            ViewBag.QCC = dbQCC.QCC_List.Where(w => w.Period_FY == currPeriodFY && w.Group == currGroup).FirstOrDefault();
            ViewBag.QCCList = dbQCC.QCC_List.OrderByDescending(o => o.Period).OrderByDescending(o => o.ID).ToList();
            ViewBag.UserList = db.V_Users_Active.Where(w => !w.NIK.Contains("NGK") && !w.NIK.Contains("EXP")).ToList();
            ViewBag.FacilitatorList = db.V_Users_Active.Where(w => !w.NIK.Contains("NGK") && !w.NIK.Contains("EXP") && facilitator.Contains(w.PositionID)).ToList();
            ViewBag.AdvisorList = db.V_Users_Active.Where(w => !w.NIK.Contains("NGK") && !w.NIK.Contains("EXP") && advisor.Contains(w.PositionID)).ToList();
            ViewBag.NavHide = true;
            if (TempData["Swal"] != null)
            {
                ViewBag.Swal = new Swal { Title = "Data Saved!", Text = "Your data has been Saved!", Icon = "success" };
            }
            return View();
        }
        // GET: QCC/Data
        public ActionResult Progress()
        {
            var currPeriod = !String.IsNullOrEmpty(Request["iPeriod"]) ? Int32.Parse(Request["iPeriod"]) : 0;
            var currGroup = !String.IsNullOrEmpty(Request["iGroup"]) ? Request["iGroup"] : "";
            var QCCData = dbQCC.QCC_List.Select(s => new QCCDataList
            {
                ID = s.ID,
                Period = s.Period,
                Period_FY = s.Period_FY,
                Group = s.Group,
                Type = s.Type,
                Theme = s.Theme,
                Leader = s.Leader,
                Facilitator = s.Facilitator,
                Created_By = s.Created_By,
                Step1 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 1).FirstOrDefault(),
                Step2 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 2).FirstOrDefault(),
                Step3 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 3).FirstOrDefault(),
                Step4 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 4).FirstOrDefault(),
                Step5 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 5).FirstOrDefault(),
                Step6 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 6).FirstOrDefault(),
                Step7 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 7).FirstOrDefault(),
                Step8 = dbQCC.QCC_Progress.Where(w => w.List_ID == s.ID && w.Step == 8).FirstOrDefault()
            }).OrderByDescending(o => o.Period).OrderByDescending(o => o.ID).ToList();
            if (!String.IsNullOrEmpty(Request["iPeriod"]) && !String.IsNullOrEmpty(Request["iGroup"]))
            {
                QCCData = QCCData.Where(w => w.Period == currPeriod && w.Group == currGroup).ToList();
            }
            ViewBag.QCCList = QCCData;
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult insertList()
        {
            var arrayAnggota = Request["iProjectMember[]"].Split(',');
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var newData = new QCC_List();
            newData.Period = DateTime.Now.Year;
            newData.Period_FY = Request["iPeriodFY"];
            newData.Group = Request["iProjectGroup"];
            newData.Time_Duration = Int32.Parse(Request["iTimeDuration"]);
            newData.Category = Request["iCategory"];
            newData.Losses = Request["iLossesWastes[]"];
            newData.Type = "QCC";
            newData.Theme = Request["iTheme"];
            newData.Background = Request["iThemeBackground"];
            newData.Current_Condition = Request["iCurrentCondition"];
            newData.Improvement_Target = Request["iImprovementTarget"];
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            newData.Facilitator = Request["iFacilitator"];
            newData.Leader = Request["iProjectLeader"];
            newData.Advisor1 = Request["iAdvisor1"];
            newData.Advisor2 = Request["iAdvisor2"];
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            newData.Is_Reject = false;
            dbQCC.QCC_List.Add(newData);
            dbQCC.SaveChanges();
            var path = Path.Combine(Server.MapPath("~/Files/QCC/Progress/"), newData.ID.ToString());
            Directory.CreateDirectory(path);
            for (var i = 1; i <= 8; i++)
            {
                Directory.CreateDirectory(Path.Combine(path, i.ToString()));
            }

            //var deleteDataMember = db.QCC_List_Member.Where(w => w.List_ID == newData.ID).ToList();
            //db.QCC_List_Member.RemoveRange(deleteDataMember);
            foreach (var anggota in arrayAnggota)
            {
                var section = db.Users.Where(w => w.NIK == anggota).FirstOrDefault();
                dbQCC.QCC_List_Member.Add(new QCC_List_Member
                {
                    List_ID = newData.ID,
                    Member = anggota,
                    Section = (section.SubSectionName != "" ? section.SubSectionName : (section.SectionName != "" ? section.SectionName : (section.DeptName != "" ? section.DeptName : section.DivisionName)))
                });
            }
            dbQCC.SaveChanges();

            string[] approvalMember = { newData.Leader, newData.Facilitator, newData.Advisor1, newData.Advisor2 };
            string[] approvalTitle = { "Project Leader", "Facilitator", "Advisor 1", "Advisor 2" };
            int _idx = 0;
            foreach (var item in approvalMember)
            {
                if (item.Length > 0)
                {
                    var approvalList = new Approval_List();
                    approvalList.Reveral_ID = newData.ID.ToString();
                    approvalList.Menu_Id = 29;
                    approvalList.Document_Id = 1;
                    approvalList.User_NIK = item;
                    approvalList.Title = approvalTitle[_idx];
                    approvalList.Header = approvalTitle[_idx];
                    approvalList.Label = approvalTitle[_idx];
                    approvalList.Levels = 1;
                    approvalList.Levels_Sub = _idx;
                    approvalList.Is_Skip = false;
                    approvalList.IsEmailOnly = false;
                    db.Approval_List.Add(approvalList);
                }
                _idx++;
            }
            db.SaveChanges();

            TempData["Swal"] = new Swal { Title = "Data Saved!", Text = "Your data has been Saved!", Icon = "success" };
            return RedirectToAction("List", "Data", new { area = "QCC", iPeriodFY = newData.Period_FY, iGroup = newData.Group });
        }
        [HttpPost]
        [Authorize]
        public ActionResult editList()
        {
            var arrayAnggota = Request["iProjectMember[]"].Split(',');
            var currID = Int32.Parse(Request["iID"]);
            var currUser = (ClaimsIdentity)User.Identity;
            var editData = dbQCC.QCC_List.Where(w => w.ID == currID).FirstOrDefault();
            editData.Period = DateTime.Now.Year;
            editData.Period_FY = Request["iPeriodFY"];
            editData.Group = Request["iProjectGroup"];
            editData.Time_Duration = Int32.Parse(Request["iTimeDuration"]);
            editData.Category = Request["iCategory"];
            editData.Losses = Request["iLossesWastes[]"];
            editData.Type = "QCC";
            editData.Theme = Request["iTheme"];
            editData.Background = Request["iThemeBackground"];
            editData.Current_Condition = Request["iCurrentCondition"];
            editData.Improvement_Target = Request["iImprovementTarget"];
            editData.Facilitator = Request["iFacilitator"];
            editData.Leader = Request["iProjectLeader"];
            editData.Advisor1 = Request["iAdvisor1"];
            editData.Advisor2 = Request["iAdvisor2"];
            foreach (var anggota in arrayAnggota)
            {
                var section = db.Users.Where(w => w.NIK == anggota).FirstOrDefault();
                var checkMember = dbQCC.QCC_List_Member.Where(w => w.List_ID == currID && w.Member == anggota).FirstOrDefault();
                if (checkMember == null)
                {
                    dbQCC.QCC_List_Member.Add(new QCC_List_Member
                    {
                        List_ID = editData.ID,
                        Member = anggota,
                        Section = (section.SubSectionName != "" ? section.SubSectionName : (section.SectionName != "" ? section.SectionName : (section.DeptName != "" ? section.DeptName : section.DivisionName)))
                    });
                }
            }
            var deleteNotExistMember = dbQCC.QCC_List_Member.Where(w => w.List_ID == currID && !arrayAnggota.Contains(w.Member)).ToList();
            dbQCC.QCC_List_Member.RemoveRange(deleteNotExistMember);
            dbQCC.SaveChanges();

            string[] approvalMember = { editData.Leader, editData.Facilitator, editData.Advisor1, editData.Advisor2 };
            string[] approvalTitle = { "Project Leader", "Facilitator", "Advisor 1", "Advisor 2" };
            int _idx = 0;

            var removeCurrentApprovalList = db.Approval_List.Where(w => w.Menu_Id == 29 && w.Document_Id == 1 && w.Reveral_ID == editData.ID.ToString()).ToList();
            db.Approval_List.RemoveRange(removeCurrentApprovalList);
            db.SaveChanges();

            foreach (var item in approvalMember)
            {
                if (item.Length > 0)
                {
                    var approvalList = new Approval_List();
                    approvalList.Reveral_ID = editData.ID.ToString();
                    approvalList.Menu_Id = 29;
                    approvalList.Document_Id = 1;
                    approvalList.User_NIK = item;
                    approvalList.Title = approvalTitle[_idx];
                    approvalList.Header = approvalTitle[_idx];
                    approvalList.Label = approvalTitle[_idx];
                    approvalList.Levels = 1;
                    approvalList.Levels_Sub = _idx;
                    approvalList.Is_Skip = false;
                    approvalList.IsEmailOnly = false;
                    db.Approval_List.Add(approvalList);
                }
                _idx++;
            }
            db.SaveChanges();

            TempData["Swal"] = new Swal { Title = "Data Saved!", Text = "Your data has been Saved!", Icon = "success" };

            return RedirectToAction("List", "Data", new { area = "QCC", iPeriodFY = editData.Period_FY, iGroup = editData.Group });
        }
        [HttpPost]
        [Authorize]
        public ActionResult insertStep()
        {
            var step = int.Parse(Request["iStep"]);
            var listID = int.Parse(Request["iListID"]);
            var note = Request["iNote"];
            var checkProgress = dbQCC.QCC_Progress.Where(w => w.List_ID == listID && w.Step == step).FirstOrDefault();

            if (checkProgress != null)
            {
                checkProgress.Note = note;
            }
            else
            {
                dbQCC.QCC_Progress.Add(new QCC_Progress
                {
                    List_ID = listID,
                    Step = step,
                    Note = note,
                    Created_At = DateTime.Now
                });
            }
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase iFile = Request.Files[i];
                // extract only the filename
                if (iFile.ContentLength > 0)
                {
                    var fileName = iFile.FileName;
                    string extension = Path.GetExtension(fileName);
                    // store the file inside ~/App_Data/uploads folder
                    var path = Path.Combine(Server.MapPath("~/Files/QCC/Progress/" + listID + "/" + step), fileName);
                    iFile.SaveAs(path);
                    var checkFile = dbQCC.QCC_Progress_Files.Where(w => w.List_ID == listID && w.Step == step && w.Filename == fileName).FirstOrDefault();
                    if (checkFile == null)
                    {
                        dbQCC.QCC_Progress_Files.Add(new QCC_Progress_Files()
                        {
                            List_ID = listID,
                            Step = step,
                            Filename = fileName,
                            Ext = extension

                        });
                    }
                }
            }
            dbQCC.SaveChanges();

            return RedirectToAction("Progress", "Data", new { area = "QCC" });
        }
        [HttpPost]
        public ActionResult getStep()
        {
            var step = int.Parse(Request["iStep"]);
            var listID = int.Parse(Request["iListID"]);
            var checkProgress = dbQCC.QCC_Progress.Where(w => w.List_ID == listID && w.Step == step).FirstOrDefault();
            var notes = "";
            var stats = false;
            var getFiles = dbQCC.QCC_Progress_Files.Where(w => w.List_ID == listID && w.Step == step).Select(s => new { filename = s.Filename, ext = s.Ext, id = s.ID });
            if (checkProgress != null)
            {
                notes = checkProgress.Note;
                stats = true;
            }
            return Json(new { stat = stats, note = notes, files = getFiles }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize]
        public ActionResult deleteList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Int32.Parse(Request["iID"]);
            var currDataList = dbQCC.QCC_List.Where(x => x.ID == currID).FirstOrDefault();
            var currDataMember = dbQCC.QCC_List_Member.Where(x => x.List_ID == currID).ToList();
            var currDataProgress = dbQCC.QCC_Progress.Where(x => x.List_ID == currID).ToList();
            var currDataProgressFiles = dbQCC.QCC_Progress_Files.Where(x => x.List_ID == currID).ToList();
            dbQCC.QCC_List_Member.RemoveRange(currDataMember);
            dbQCC.QCC_List.Remove(currDataList);
            dbQCC.QCC_Progress.RemoveRange(currDataProgress);
            dbQCC.QCC_Progress_Files.RemoveRange(currDataProgressFiles);

            var path = Server.MapPath("~/Files/QCC/Progress/" + currID);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            dbQCC.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult deleteFile()
        {
            var currID = Int32.Parse(Request["iID"]);
            var del = dbQCC.QCC_Progress_Files.Where(w => w.ID == currID).FirstOrDefault();

            var path = Server.MapPath("~/Files/QCC/Progress/" + del.List_ID + "/" + del.Step + "/" + del.Filename);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            dbQCC.QCC_Progress_Files.Remove(del);
            dbQCC.SaveChanges();

            return Content(Boolean.TrueString);
        }
        [HttpPost]
        public ActionResult cancelStep()
        {
            var currID = Int32.Parse(Request["iID"]);
            var currStep = Int32.Parse(Request["iStep"]);
            var currDataProgress = dbQCC.QCC_Progress.Where(x => x.List_ID == currID && x.Step == currStep).ToList();
            var currDataProgressFiles = dbQCC.QCC_Progress_Files.Where(x => x.List_ID == currID && x.Step == currStep).ToList();
            dbQCC.QCC_Progress.RemoveRange(currDataProgress);
            dbQCC.QCC_Progress_Files.RemoveRange(currDataProgressFiles);

            var path = Server.MapPath("~/Files/QCC/Progress/" + currID + "/" + currStep);
            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            dbQCC.SaveChanges();

            return Content(Boolean.TrueString);
        }

        public String ApprovalHistory(string Reveral_ID, int Approval, int Approval_Sub, int getType, int documentID = 1)
        {
            var str = "";
            var getApprovalHistory = db.Approval_History.Where(w => w.Menu_Id == 29 && w.Document_Id == documentID && w.Reveral_ID == Reveral_ID && w.Approval == Approval && w.Approval_Sub == Approval_Sub).OrderByDescending(o => o.id).FirstOrDefault();

            if (getType == 1)
            {
                str = getApprovalHistory?.Created_By_Name ?? "";
            }
            else if (getType == 2)
            {
                str = getApprovalHistory?.Created_At.ToString("dd-MMM-yyyy") ?? "";
            }
            else
            {
                str = getApprovalHistory?.Note ?? "";
            }

            return str;
        }


        public ActionResult RequestSign()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currDataID = int.Parse(Request["iSignID"]);
            var btnType = Request["btnType"];
            var currNote = Request["iQCCNote"] ?? "";
            var updateSign = dbQCC.QCC_List.Where(w => w.ID == currDataID).FirstOrDefault();
            var curApproval = updateSign.Approval;
            var curApprovalSub = updateSign.Approval_Sub;
            if (btnType != "Reminder")
            {
                var checkApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == currDataID.ToString() && w.Document_Id == 1 && w.Menu_Id == 29 && w.Levels == updateSign.Approval && w.Levels_Sub > updateSign.Approval_Sub && w.Is_Skip == false && w.IsEmailOnly == false).OrderBy(o => o.Levels_Sub).FirstOrDefault();
                if (checkApprovalMaster == null)
                {
                    checkApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == currDataID.ToString() && w.Document_Id == 1 && w.Menu_Id == 29 && w.Levels > updateSign.Approval && w.Is_Skip == false && w.IsEmailOnly == false).OrderBy(o => o.Levels).ThenBy(o => o.Levels_Sub).FirstOrDefault();
                }
                var getApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == currDataID.ToString() && w.User_NIK == currUserID && w.Document_Id == 1 && w.Menu_Id == 29 && w.Levels == updateSign.Approval && w.Levels_Sub == updateSign.Approval_Sub && w.Is_Skip == false && w.IsEmailOnly == false).FirstOrDefault();

                if (btnType == "Return")
                {
                    updateSign.Approval = 1;
                    updateSign.Approval_Sub = 0;
                }
                else
                {
                    if (checkApprovalMaster != null)
                    {
                        updateSign.Approval = checkApprovalMaster.Levels;
                        updateSign.Approval_Sub = checkApprovalMaster.Levels_Sub;
                    }
                    else
                    {
                        updateSign.Approval += 1;
                        updateSign.Approval_Sub = 0;
                    }

                    if (btnType == "Reject")
                    {
                        updateSign.Is_Reject = true;
                    }
                }
                dbQCC.SaveChanges();
                var currApproval = btnType == "Reject" ? 1 : updateSign.Approval;
                var currApprovalSub = btnType == "Reject" ? 0 : updateSign.Approval_Sub;

                var updateSignHistory = new Approval_History();
                updateSignHistory.Menu_Id = 29;
                updateSignHistory.Menu_Name = "QCC Registration";
                updateSignHistory.Document_Id = 1;
                updateSignHistory.Document_Name = "Registration Form";
                updateSignHistory.Reveral_ID = currDataID.ToString();
                updateSignHistory.Reveral_ID_Sub = null;
                updateSignHistory.Title = getApprovalMaster != null ? getApprovalMaster.Title : "QM Team";
                updateSignHistory.Header = getApprovalMaster != null ? getApprovalMaster.Header : "QM Team";
                updateSignHistory.Label = getApprovalMaster != null ? getApprovalMaster.Label : "QM Team";
                updateSignHistory.Note = currNote;
                updateSignHistory.Approval = curApproval;
                updateSignHistory.Approval_Sub = curApprovalSub;
                updateSignHistory.IsReject = updateSign.Is_Reject ?? false;
                updateSignHistory.IsRevise = false;
                updateSignHistory.Status = (btnType != "Sign" ? btnType : ApprovalStatus(updateSign.Approval, updateSign.Approval_Sub));
                updateSignHistory.Created_At = DateTime.Now;
                updateSignHistory.Created_By_ID = currUserID;
                updateSignHistory.Created_By_Name = currUserName;
                db.Approval_History.Add(updateSignHistory);
                db.SaveChanges();
            }

            sendNotification(currDataID.ToString(), "List", btnType, updateSign.Created_By, "", updateSign.Approval, updateSign.Approval_Sub, currNote);

            return RedirectToAction("List", "Data", new { area = "QCC", iPeriodFY = updateSign.Period_FY, iGroup = updateSign.Group });
        }


        public String ApprovalStatus(int Approval, int Approval_Sub, int Type = 1)
        {
            var stat = "Submitted";
            if (Type == 1)
            {
                switch (Approval_Sub)
                {
                    case 1:
                        stat = "Submitted";
                        break;
                    case 2:
                        stat = "Checked";
                        break;
                    case 3:
                        stat = "Reviewed";
                        break;
                    default:
                        stat = "Created";
                        break;
                }

                if (Approval > 1)
                {
                    stat = "Approved";
                }
            }
            return stat;
        }

        public void sendNotification(string currReqNumber, string currMenu, string currStatus, string currNIK = "", string deptCode = "", int approval = 0, int approval_sub = 0, string note = "-")
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            string FilePath = Path.Combine(Server.MapPath("~/Emails/QCC/"), "QCCRegistrationApproval.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();
            var stat = "";
            var needs = "Approval";
            if (currStatus == "Reject")
            {
                stat = "Rejected!";
                needs = "Attention";
            }
            else if (currStatus == "Return")
            {
                stat = "Returned!";
                needs = "Attention";
            }
            else if (currStatus == "Comment")
            {
                stat = "Commented";
                needs = "Attention";
            }

            var doc = "Registration Form";
            var currQCCDataID = int.Parse(currReqNumber);
            var currQCCData = dbQCC.QCC_List.Where(w => w.ID == currQCCDataID).FirstOrDefault();

            var currURL = Url.Action(currMenu, "Data", new { area = "QCC", iPeriodFY = currQCCData.Period_FY, iGroup = currQCCData.Group }, this.Request.Url.Scheme);
            var currURLOpen = Url.Action(currMenu, "Data", new { area = "QCC" }, this.Request.Url.Scheme);
            var documentID = 1;
            var emailList = db.Approval_List.Where(w => w.Menu_Id == 29 && w.Document_Id == documentID && w.Reveral_ID == currQCCDataID.ToString() && w.Levels == approval && w.Levels_Sub == approval_sub).Select(s => s.Users.Email).Distinct().ToList();

            //if (documentID == 2 && currStatus == "Comment")
            //{
            //    //var latestRejectID = db.Approval_History.Where(w => w.Menu_Id == 29 && w.Document_Id == 2 && w.Reveral_ID == currReqNumber && (w.Status == "Return" || w.Status == "Reject")).OrderByDescending(o => o.id).FirstOrDefault()?.id ?? 0;
            //    var currQuotation = dbPR.Purchasing_PurchaseRequest_Quotation_Header.Where(w => w.QuoNumber == currReqNumber).FirstOrDefault();
            //    var currApproval = currQuotation.Approval;
            //    var currApprovalSub = currQuotation.Approval_Sub;
            //    var emailListQRY = db.Approval_List.Where(w => w.Menu_Id == 29 && w.Document_Id == 2 && w.Reveral_ID == currReqNumber && w.Is_Skip == false && (w.Levels <= currApproval && ((w.Levels == currApproval && w.Levels_Sub < currApprovalSub) || (w.Levels < currApproval))));
            //    var commentList = dbPR.Purchasing_PurchaseRequest_Quotation_Comments.Where(w => w.QuoNumber == currReqNumber).Select(s => s.Users.Email).Distinct().ToList();
            //    //if (latestRejectID != 0)
            //    //{
            //    //    emailListQRY = emailListQRY.Where(w => w.id > latestRejectID && w.Status != "Return" && w.Status != "Reject");
            //    //}
            //    emailList = emailListQRY.Select(s => s.Users.Email).Distinct().ToList();

            //    foreach (var comment in commentList)
            //    {
            //        emailList.Add(comment);
            //    }
            //}
            if (currStatus != "Sign" && currStatus != "Comment")
            {
                emailList = db.Users.Where(w => w.NIK == currNIK).Select(s => s.Email).Distinct().ToList();
            }

            //Repalce [newusername] = signup user name   
            MailText = MailText.Replace("##document##", doc);
            MailText = MailText.Replace("##needs##", needs);
            MailText = MailText.Replace("##link##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##url##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##note##", note);
            MailText = MailText.Replace("##noteby##", currUserName);
            MailText = MailText.Replace("##linkOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##urlOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##guid##", Guid.NewGuid().ToString());

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
            var password = "100%NGKbusi!";
            var sub = "[Niterra-Portal-Notification]" + stat + " - QCC Registration - " + currQCCData.Period_FY + " - " + currQCCData.Group;
            var body = MailText;
            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
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
                foreach (var dataEmail in emailList)
                {
                    if (dataEmail.Length > 0)
                    {
                        mess.To.Add(new MailAddress(dataEmail));
                    }
                }
                mess.Bcc.Add(new MailAddress("azis.abdillah@ngkbusi.com"));
                smtp.Send(mess);
            }
        }

    }

    public class QCCDataList
    {
        public int ID { get; set; }
        public int Period { get; set; }
        public string Period_FY { get; set; }
        public string Group { get; set; }
        public string Type { get; set; }
        public string Theme { get; set; }
        public string Facilitator { get; set; }
        public string Leader { get; set; }
        public string Created_By { get; set; }
        public QCC_Progress Step1 { get; set; }
        public QCC_Progress Step2 { get; set; }
        public QCC_Progress Step3 { get; set; }
        public QCC_Progress Step4 { get; set; }
        public QCC_Progress Step5 { get; set; }
        public QCC_Progress Step6 { get; set; }
        public QCC_Progress Step7 { get; set; }
        public QCC_Progress Step8 { get; set; }
    }
}