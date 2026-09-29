using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using NGKBusi.Areas.Sales.Models;
using NGKBusi.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.Sales.Controllers
{
    public class DigitalApprovalController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        DigitalApprovalConnection dbDA = new DigitalApprovalConnection();
        // GET: Sales/DigitalApproval
        public ActionResult List(String ReqNumber)
        {
            ViewBag.NavHide = true;
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 137
                        select new { usr, rol }).AsEnumerable().Select(s => s.usr);

            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            var currYear = Request["iDAFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iDAFilterStatus"] ?? "Open";
            var currUserLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Menu_Id == 137 && w.Document_Id == 1).FirstOrDefault();
            var currUserLevels = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Menu_Id == 137 && w.Document_Id == 1).ToList();
            var currFilterLevel = Request["iDAFilterLevel"] != null ? int.Parse(Request["iDAFilterLevel"].Split('|')[0]) : currUserLevel?.Levels;
            var currFilterLevelSub = Request["iDAFilterLevel"] != null ? int.Parse(Request["iDAFilterLevel"].Split('|')[1]) : currUserLevel?.Levels_Sub;
            var currApprove = db.Approval_Master.Where(w => w.Menu_Id == 137 && w.Document_Id == 1 && w.User_NIK == currUserID);
            var currApproval = currApprove.Where(w => w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub).FirstOrDefault();
            var currApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 137 && w.Document_Id == 1 && w.User_NIK == currUserID && w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub).Select(s => s.Dept_Code).Distinct().ToList();

            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.UserLevel = currFilterLevel != null ? currFilterLevel : currUserLevel.Levels;
            ViewBag.UserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currUserLevel.Levels_Sub;
            ViewBag.CurrUserSection = currUserLevel.Dept_Code + " | " + currUserLevel.Dept_Name;
            ViewBag.currUserLevels = currUserLevels;
            ViewBag.CurrApproval = currApproval;
            ViewBag.CurrData = dbDA.Sales_DigitalApproval_List.Where(w => w.ReqNumber == ReqNumber).FirstOrDefault();

            if (currStatus == "Open")
            {
                ViewBag.CurrDataList = dbDA.Sales_DigitalApproval_List.Where(w => w.Created_At.Year.ToString() == currYear && w.Approval == currFilterLevel && w.Approval_Sub == currFilterLevelSub && w.Is_Reject == false).OrderByDescending(o => o.ID).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.CurrDataList = dbDA.Sales_DigitalApproval_List.Where(w => w.Created_At.Year.ToString() == currYear && (w.Approval > currFilterLevel || (w.Approval == currFilterLevel && w.Approval_Sub > currFilterLevelSub))).OrderByDescending(o => o.ID).ToList();
            }
            else
            {
                ViewBag.CurrDataList = dbDA.Sales_DigitalApproval_List.Where(w => w.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.ID).ToList();
            }


            return View();
        }
        public ActionResult FormAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            IFormatProvider culture = new CultureInfo("en-US", true);

            var newData = new Sales_DigitalApproval_List();
            newData.ReqNumber = this.getSequence("DA");
            newData.Description = Request["iDescription"];
            newData.Section = Request["iSection"];
            newData.Created_At = DateTime.Now;
            newData.Created_By = currUserID;
            newData.Approval = 1;
            newData.Approval_Sub = 0;
            newData.Is_Reject = false;

            dbDA.Sales_DigitalApproval_List.Add(newData);
            dbDA.SaveChanges();

            var getApprovalList = db.Approval_Master.Where(w => w.Menu_Id == 137 && w.Document_Id == 1 && w.Dept_Code == newData.Section.Trim().Substring(0, 5)).ToList();
            foreach (var dList in getApprovalList)
            {
                var newApprovalList = new Approval_List();
                newApprovalList.Reveral_ID = newData.ReqNumber;
                newApprovalList.Menu_Id = dList.Menu_Id;
                newApprovalList.Document_Id = dList.Document_Id;
                newApprovalList.User_NIK = dList.User_NIK;
                newApprovalList.Dept_Code = dList.Dept_Code;
                newApprovalList.Dept_Name = dList.Dept_Name;
                newApprovalList.Title = dList.Title;
                newApprovalList.Header = dList.Header;
                newApprovalList.Label = dList.Label;
                newApprovalList.Levels = dList.Levels;
                newApprovalList.Levels_Sub = dList.Levels_Sub;
                newApprovalList.Is_Skip = false;
                db.Approval_List.Add(newApprovalList);
            }
            db.SaveChanges();

            uploadAttachment(newData.ReqNumber);

            return RedirectToAction("List", "DigitalApproval", new { area = "Sales", ReqNumber = newData.ReqNumber, iPRFilterLevel = "1|0" });
        }
        public ActionResult FormEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currReqNumber = Request["iReqNumber"];
            IFormatProvider culture = new CultureInfo("en-US", true);

            var newData = dbDA.Sales_DigitalApproval_List.Where(w => w.ReqNumber == currReqNumber).FirstOrDefault();
            newData.Description = Request["iDescription"];

            dbDA.SaveChanges();

            uploadAttachment(newData.ReqNumber);
            return RedirectToAction("List", "DigitalApproval", new { area = "Sales", ReqNumber = newData.ReqNumber });
        }
        public ActionResult FormDelete()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currReqNumber = Request["iReqNumber"];
            IFormatProvider culture = new CultureInfo("en-US", true);

            var deleteData = dbDA.Sales_DigitalApproval_List.Where(w => w.ReqNumber == currReqNumber).FirstOrDefault();
            dbDA.Sales_DigitalApproval_List.Remove(deleteData);

            var deleteAttachment = dbDA.Sales_DigitalApproval_List_Attachment.Where(w => w.ReqNumber == currReqNumber).ToList();
            foreach (var del in deleteAttachment)
            {
                deleteDAAttachment(del.ID);
            }
            dbDA.Sales_DigitalApproval_List_Attachment.RemoveRange(deleteAttachment);
            dbDA.SaveChanges();

            return RedirectToAction("List", "DigitalApproval", new { area = "Sales" });
        }

        public void uploadAttachment(string currReqNumber)
        {
            string checkFolder = "~/Files/Sales/DigitalApproval/" + currReqNumber; // Your code goes here
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
                    var path = Path.Combine(Server.MapPath("~/Files/Sales/DigitalApproval/" + currReqNumber), fileName);
                    iFile.SaveAs(path);
                    var checkFile = dbDA.Sales_DigitalApproval_List_Attachment.Where(w => w.ReqNumber == currReqNumber && w.Filename == fileName).FirstOrDefault();
                    if (checkFile == null)
                    {
                        dbDA.Sales_DigitalApproval_List_Attachment.Add(new Sales_DigitalApproval_List_Attachment()
                        {
                            ReqNumber = currReqNumber,
                            Filename = fileName,
                            Ext = extension

                        });
                    }
                }
            }

            dbDA.SaveChanges();
        }
        [HttpPost]
        public ActionResult getDAAttachment()
        {
            var currReqNumber = Request["iReqNumber"];

            var getFiles = dbDA.Sales_DigitalApproval_List_Attachment.Where(w => w.ReqNumber == currReqNumber).Select(s => new { filename = s.Filename, ext = s.Ext, id = s.ID });

            return Json(new { files = getFiles }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult deleteDAAttachment(int iID)
        {
            var currID = iID;
            var del = dbDA.Sales_DigitalApproval_List_Attachment.Where(w => w.ID == currID).FirstOrDefault();

            var path = Server.MapPath("~/Files/Sales/DigitalApproval/" + del.ReqNumber + "/" + del.Filename);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            dbDA.Sales_DigitalApproval_List_Attachment.Remove(del);
            dbDA.SaveChanges();

            return Content(Boolean.TrueString);
        }
        public String getSequence(string type)
        {
            var lastSeq = "";
            var seqHeader = type + DateTime.Now.ToString("yy");
            var latestSequence = "";
            if (type == "DA")
            {
                latestSequence = dbDA.Sales_DigitalApproval_List.Where(w => w.ReqNumber.Substring(0, 4) == seqHeader).OrderByDescending(o => o.ID).Select(s => s.ReqNumber.Substring(s.ReqNumber.Length - 4, 4)).FirstOrDefault();
            }
            lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
            lastSeq = seqHeader + "-" + lastSeq.Substring(lastSeq.Length - 4, 4);

            return lastSeq;
        }

        public String ApprovalHistory(string Reveral_ID, int Approval, int Approval_Sub, int getType, int documentID = 1)
        {
            var str = "";
            var getApprovalHistory = db.Approval_History.Where(w => w.Menu_Id == 137 && w.Document_Id == documentID && w.Reveral_ID == Reveral_ID && w.Approval == Approval && w.Approval_Sub == Approval_Sub).OrderByDescending(o => o.id).FirstOrDefault();

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
        public ActionResult FormSign()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var reqNumber = Request["iSignReqNumber"];
            var btnType = Request["btnType"];
            var currNote = Request["iPRNote"] ?? "";
            var updateSign = dbDA.Sales_DigitalApproval_List.Where(w => w.ReqNumber == reqNumber).FirstOrDefault();
            var curApproval = updateSign.Approval;
            var curApprovalSub = updateSign.Approval_Sub;
            var checkApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == reqNumber && w.Dept_Code == updateSign.Section.Substring(0, 5) && w.Document_Id == 1 && w.Menu_Id == 137 && w.Levels == updateSign.Approval && w.Levels_Sub > updateSign.Approval_Sub && w.Is_Skip == false).OrderBy(o => o.Levels_Sub).FirstOrDefault();
            if (checkApprovalMaster == null)
            {
                checkApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == reqNumber && w.Dept_Code == updateSign.Section.Substring(0, 5) && w.Document_Id == 1 && w.Menu_Id == 137 && w.Levels > updateSign.Approval && w.Is_Skip == false).OrderBy(o => o.Levels).ThenBy(o => o.Levels_Sub).FirstOrDefault();
            }
            var getApprovalMaster = db.Approval_List.Where(w => w.Reveral_ID == reqNumber && w.User_NIK == currUserID && w.Document_Id == 1 && w.Menu_Id == 137 && w.Levels == updateSign.Approval && w.Levels_Sub == updateSign.Approval_Sub && w.Is_Skip == false).FirstOrDefault();

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
            dbDA.SaveChanges();

            var currApproval = btnType == "Reject" ? 1 : updateSign.Approval;
            var currApprovalSub = btnType == "Reject" ? 0 : updateSign.Approval_Sub;

            var updateSignHistory = new Approval_History();
            updateSignHistory.Menu_Id = 137;
            updateSignHistory.Menu_Name = "Sales - Digital Approval";
            updateSignHistory.Document_Id = 1;
            updateSignHistory.Document_Name = "Digital Approval";
            updateSignHistory.Reveral_ID = reqNumber;
            updateSignHistory.Reveral_ID_Sub = null;
            updateSignHistory.Title = getApprovalMaster.Title;
            updateSignHistory.Header = getApprovalMaster.Header;
            updateSignHistory.Label = getApprovalMaster.Label;
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

            sendNotification(reqNumber, "List", btnType, updateSign.Created_By, updateSign.Section.Split('|')[0].Trim(), currApproval, currApprovalSub, currNote);

            return RedirectToAction("List", "DigitalApproval", new { area = "Sales", reqNumber = reqNumber });
        }
        public void sendNotification(string currReqNumber, string currMenu, string currStatus, string currNIK = "", string deptCode = "", int approval = 0, int approval_sub = 0, string note = "-")
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            string FilePath = Path.Combine(Server.MapPath("~/Emails/Sales/DigitalApproval/"), "DigitalApproval.html");
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

            var settlementSubject = " - Digital Document Approval";
            var doc = "Sales - Digital Document Approval";

            var currURL = Url.Action(currMenu, "DigitalApproval", new { area = "Sales", ReqNumber = currReqNumber }, this.Request.Url.Scheme);
            var currURLOpen = Url.Action(currMenu, "DigitalApproval", new { area = "Sales", iPRFilterStatus = "Open" }, this.Request.Url.Scheme);
            var documentID = 1;
            var emailList = db.Approval_Master.Where(w => w.Menu_Id == 137 && w.Document_Id == documentID && w.Dept_Code == deptCode && w.Levels == approval && w.Levels_Sub == approval_sub).Select(s => s.Users.Email).Distinct().ToList();

            //if (documentID == 2 && currStatus == "Comment")
            //{
            //    var latestRejectID = db.Approval_History.Where(w => w.Menu_Id == 40 && w.Document_Id == 2 && w.Reveral_ID == currReqNumber && (w.Status == "Return" || w.Status == "Reject")).OrderByDescending(o => o.id).FirstOrDefault()?.id ?? 0;
            //    var emailListQRY = db.Approval_History.Where(w => w.Menu_Id == 40 && w.Document_Id == 2 && w.Reveral_ID == currReqNumber);
            //    var commentList = dbPR.Purchasing_PurchaseRequest_Quotation_Comments.Where(w => w.ReqNumber == currReqNumber).Select(s => s.Users.Email).Distinct().ToList();
            //    if (latestRejectID != 0)
            //    {
            //        emailListQRY = emailListQRY.Where(w => w.id > latestRejectID && w.Status != "Return" && w.Status != "Reject");
            //    }
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
            var sub = "[Niterra-Portal-Notification]" + stat + " - Sales -" + currReqNumber + settlementSubject;
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

        public JsonResult DigitalApprovalCommentGet()
        {
            var currReqNumber = Request["iReqNumber"];
            var currNIK = Request["iNIK"];
            var getComments = dbDA.Sales_DigitalApproval_List_Comments.Include("Attachments").Where(w => w.ReqNumber == currReqNumber).ToList();
            foreach (var data in getComments)
            {
                bool checkCurrentUser = (currNIK == data.nik ? true : false);
                data.created_by_current_user = checkCurrentUser;
            }


            return Json(getComments, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DigitalApprovalCommentAdd()
        {
            var currID = Request["id"];
            var currParent = Request["parent"];
            var currCreated = Request["created"];
            var currModified = Request["modified"];
            var currContent = Request["content"];
            var currAttachments = Request["attachments"];
            var currFullName = Request["fullname"];
            var currProfilePicture = Request["profile_picture_url"];
            var currCurrentUser = Request["created_by_current_user"];
            var currUpvoteCount = Request["upvote_count"];
            var currUserHasUpvoted = Request["user_has_upvoted"];
            var currReqNumber = Request["ReqNumber"];
            var currNIK = Request["nik"];
            var checkData = dbDA.Sales_DigitalApproval_List_Comments.Where(w => w.id == currID && w.ReqNumber == currReqNumber).OrderByDescending(o => o.id).FirstOrDefault();

            var newData = new Sales_DigitalApproval_List_Comments();
            newData.id = checkData != null ? "c" + (int.Parse(checkData.id.Replace("c", "")) + 1) : currID;
            newData.parent = currParent.Length > 0 && currParent != "null" ? currParent : null;
            newData.created = currCreated;
            newData.content = currContent;
            //newData.attachments = currAttachments != null ? currAttachments : "attachments";
            newData.fullname = currFullName;
            newData.created_by_current_user = bool.Parse(currCurrentUser);
            newData.upvoteCount = int.Parse(currUpvoteCount);
            newData.userHasUpvoted = bool.Parse(currUserHasUpvoted);
            newData.ReqNumber = currReqNumber;
            newData.nik = currNIK;
            dbDA.Sales_DigitalApproval_List_Comments.Add(newData);
            dbDA.SaveChanges();

            //uploadQTAttachment(currReqNumber, "", newData.comment_id);
            sendNotification(currReqNumber, "List", "Comment", "", "", 0, 0, currContent);

            return Json(newData, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DigitalApprovalCommentDelete()
        {
            var currID = Request["id"];
            var currReqNumber = Request["ReqNumber"];
            var currNIK = Request["nik"];
            var deleteData = dbDA.Sales_DigitalApproval_List_Comments.Where(w => w.id == currID && w.ReqNumber == currReqNumber).FirstOrDefault();
            dbDA.Sales_DigitalApproval_List_Comments.Remove(deleteData);
            dbDA.SaveChanges();

            return Json(JsonConvert.SerializeObject(deleteData), JsonRequestBehavior.AllowGet);
        }
        public ActionResult DigitalApprovalCommentEdit()
        {
            var currID = Request["id"];
            var currParent = Request["parent"];
            var currCreated = Request["created"];
            var currModified = Request["modified"];
            var currContent = Request["content"];
            var currAttachments = Request["attachments"];
            var currFullName = Request["fullname"];
            var currProfilePicture = Request["profile_picture_url"];
            var currCurrentUser = Request["created_by_current_user"];
            var currReqNumber = Request["ReqNumber"];
            var currNIK = Request["nik"];
            var newData = dbDA.Sales_DigitalApproval_List_Comments.Include("Attachments").Where(w => w.id == currID && w.ReqNumber == currReqNumber).FirstOrDefault();
            newData.id = currID;
            newData.parent = currParent.Length > 0 && currParent != "null" ? currParent : null;
            newData.created = currCreated;
            newData.modified = long.Parse(currModified);
            newData.content = currContent;
            //newData.attachments = currAttachments != null ? currAttachments : "attachments";
            newData.fullname = currFullName;
            newData.created_by_current_user = bool.Parse(currCurrentUser);
            newData.ReqNumber = currReqNumber;
            newData.nik = currNIK;
            dbDA.SaveChanges();
            sendNotification(currReqNumber, "List", "Comment", "", "", 0, 0, currContent);

            return Json(newData, JsonRequestBehavior.AllowGet);
        }
    }
}