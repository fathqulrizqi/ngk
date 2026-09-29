using Microsoft.AspNet.Identity;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.IT.Controllers
{
    public class RequestSystemController : Controller
    {

        DefaultConnection db = new DefaultConnection();
        RequestSystemConnection dbrs = new RequestSystemConnection();

        // GET: IT/RequestSystem
        public ActionResult index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var SectionList = db.Users_Section_AX.ToList();

            ViewBag.CurrUser = CurrUser;
            ViewBag.SectionList = SectionList;
            return View();
        }
        [HttpPost]
        public ActionResult AddRequestSystem(IT_RequestSystem_Header smodel, String[] ItemAccess, List<string> LineSub_Name, List<string> LineSub_Explain, List<int> LineItemID, string submitValue)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            string generatedCode = string.Empty;

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_IT_GenerateKodeITForm", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter outputParam = new SqlParameter
                    {
                        ParameterName = "@GeneratedKode",
                        SqlDbType = SqlDbType.VarChar,
                        Size = 10,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    generatedCode = outputParam.Value.ToString();
                }
            }

            string[] explains = Request.Form.GetValues("explains");

            IT_RequestSystem_Header header = new IT_RequestSystem_Header();
            header.RequestNo = generatedCode;
            header.CreateBy = currUser;
            header.NIK = smodel.NIK;
            header.EmployeeName = smodel.EmployeeName;
            header.Department = smodel.Department;
            header.Status = "draft";
            header.IsDelete = 0;

            dbrs.IT_RequestSystem_Header.Add(header);
            int sh = dbrs.SaveChanges();
            int ReqID = header.ID;
            if (sh > 0)
            {
                var itemMaster = dbrs.IT_RequestSystem_ItemMaster.ToList();
                foreach(var ims in itemMaster)
                {
                    byte isChecked = Convert.ToByte(ItemAccess.Contains(ims.ID.ToString()) ? 1 : 0);
                    IT_RequestSystem_Lines lines = new IT_RequestSystem_Lines();
                    lines.ReqID = ReqID;
                    lines.IsChecked = isChecked;
                    lines.ItemID = ims.ID;
                    lines.ItemName = ims.ItemName;
                    lines.Explains = Request.Form[$"explains[{ims.ID}]"];

                    dbrs.IT_RequestSystem_Lines.Add(lines);

                    dbrs.SaveChanges();
                    int LineID = lines.ID;
                    if (lines.ItemID == 2)
                    {
                        for (int i = 1; i <= 2; i++)
                        {
                            IT_RequestSystem_LinesSub sub = new IT_RequestSystem_LinesSub();
                            sub.LineID = LineID;
                            sub.LineItemID = Convert.ToInt32(Request.Form[$"LineItemID[{i}]"]);
                            sub.LineSub_Name = Request.Form[$"LineSub_Name[{i}]"];
                            sub.LineSub_Explain = Request.Form[$"LineSub_Explain[{i}]"];
                            sub.ReqID = ReqID;

                            dbrs.IT_RequestSystem_LinesSub.Add(sub);
                            dbrs.SaveChanges();
                        }
                    }
                    if (lines.ItemID == 3)
                    {
                        for (int c = 3; c <= 4; c++)
                        {

                            IT_RequestSystem_LinesSub sub = new IT_RequestSystem_LinesSub();
                            sub.LineID = LineID;
                            sub.LineItemID = Convert.ToInt32(Request.Form[$"LineItemID[{c}]"]);
                            sub.LineSub_Name = Request.Form[$"LineSub_Name[{c}]"];
                            sub.LineSub_Explain = Request.Form[$"LineSub_Explain[{c}]"];
                            sub.ReqID = ReqID;

                            dbrs.IT_RequestSystem_LinesSub.Add(sub);
                            dbrs.SaveChanges();
                        }
                    }
                }
                if (submitValue == "Submit")
                {
                    int SignAction = SignAct(ReqID, submitValue);
                }

                return Json(new { status = 1, msg = "save success", reqID = ReqID, explains = Request.Form[$"explains[{1}]"], LineSub_Name = Request.Form[$"LineSub_Name[{1}]"], LineItemID = LineItemID, ItemAccess = ItemAccess });
            }
            else
            {
                return Json(new { status = 0, msg = "Failed to save", reqID = ReqID });
            }

        }
        public ActionResult RequestList()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            ViewBag.CurrUser = CurrUser;

            return View();
        }
        [HttpPost]
        public ActionResult GetDataRequestSystem()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var listITStaff = db.V_Users_Active.Where(w => w.DeptName == "INFORMATION TECHNOLOGY" && w.PositionName != "MANAGER").Select(r => r.NIK).ToList();

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var lvl = "";
            IQueryable<IT_RequestSystem_Header>  query = dbrs.IT_RequestSystem_Header;
                      
            if(listITStaff.Contains(currUser))
            {
                lvl = "it";
                query = query.Where(w=>w.IsDelete==0).OrderByDescending(a => a.ID);
            } else
            {
                lvl = "non it";
                query = query.Where(w => w.IsDelete == 0 && w.CreateBy == currUser || w.SignReqManagerBy == currUser || w.SignITManagerBy == currUser || w.SignGMITBy == currUser).OrderByDescending(o=>o.ID);
            }
            var qheader = query.ToList();
            var CountRow = dbrs.IT_RequestSystem_Header.Count();
            List<Tbl_IT_RequestSystem_Header> actions = new List<Tbl_IT_RequestSystem_Header>();
            int No = 0;
            foreach (var Item in qheader)
            {
                No++;

                var UrlAction = Url.Action("RequestForm", "RequestSystem", new { area = "IT", ID = Item.ID });

                var ActionButton = "";

                //if (User.IsInRole("Administrator"))
                //{
                    ActionButton = "<a href=\"" + UrlAction + "\" title=\"View Detail\" > " + Item.RequestNo + "</a>";
                //}
                //else
                //{
                //    ActionButton = "";
                //}
                //var Prd = Item.Periode;
                //string monthYear = Prd.ToString("MMMM yyyy");
                var CreateDate = Item.CreateTime;
                string shortDate = CreateDate?.ToString("dd-MM-yyyy");

                var statusForm = "";

                if (Item.Status == "draft")
                {
                    statusForm = "<span class=\"badge badge-warning\">Draft</span>";
                } else if (Item.Status == "submit")
                {
                    statusForm = "<span class=\"badge badge-primary\">Submitted</span>";
                } else if (Item.Status == "Sign Requestor Manager")
                {
                    statusForm = "<span class=\"badge badge-primary\">Approved by Manager</span>";
                }
                else if (Item.Status == "Sign IT Manager")
                {
                    statusForm = "<span class=\"badge badge-primary\">Approved by IT Manager</span>";
                }
                else if (Item.Status == "Sign GM IT")
                {
                    statusForm = "<span class=\"badge badge-info\">Approved by IT Manager</span>";
                }
                else
                {
                    statusForm = "<span class=\"badge badge-success\">Complete</span>";
                }
                //else if (Item.Status == 3)
                //{
                //    statusForm = "<span class=\"badge badge-danger\">Checked</span>";
                //}
                //if (Item.Status == 2)
                //{
                //    statusForm = "<span class=\"badge badge-warning\">Submitted</span>";
                //}
                //if (Item.Status == 1)
                //{
                //    statusForm = "<span class=\"badge badge-primary\">Open</span>";
                //}

                actions.Add(
                    new Tbl_IT_RequestSystem_Header
                    {
                        ID = No,
                        RequestNo = ActionButton,
                        EmployeeName = Item.EmployeeName,
                        Department = Item.Department,
                        DateRequest = Item.CreateTime.ToString(),
                        Status = statusForm
                    });
            }

            //return Json(new
            //{
            //    rows = actions,
            //    totalNotFiltered = CountRow,
            //    total = CountRow
            //}, JsonRequestBehavior.AllowGet);

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow, lvl = lvl, curuUser = currUser }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        public ActionResult RequestForm(int ID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var SectionList = db.Users_Section_AX.ToList();

            var qHeader = dbrs.IT_RequestSystem_Header.Where(w => w.ID == ID).FirstOrDefault();
            var qLines = dbrs.IT_RequestSystem_Lines.Where(w => w.ReqID == ID).ToList();
            var qLinesSub2 = dbrs.IT_RequestSystem_LinesSub.Where(w => w.ReqID == ID && ( w.LineItemID == 2 ||w.LineItemID == 1)).ToList();
            var qLinesSub3 = dbrs.IT_RequestSystem_LinesSub.Where(w => w.ReqID == ID && (w.LineItemID == 3 || w.LineItemID == 4)).ToList();
            var listITStaff = db.V_Users_Active.Where(w => w.DeptName == "INFORMATION TECHNOLOGY" && w.PositionName != "MANAGER").Select(r => r.NIK).ToList();

            if (qHeader.CreateBy != null) { var CreateByName = db.V_Users_Active.Where(w => w.NIK == qHeader.CreateBy).FirstOrDefault(); ViewBag.CreateByName = CreateByName.Name; }
            if (qHeader.SignReqManagerBy != null) { var SignReqManagerByName = db.V_Users_Active.Where(w => w.NIK == qHeader.SignReqManagerBy).FirstOrDefault(); ViewBag.SignReqManagerByName = SignReqManagerByName.Name; }
            if (qHeader.SignITManagerBy != null) { var SignITManagerByName = db.V_Users_Active.Where(w => w.NIK == qHeader.SignITManagerBy).FirstOrDefault(); ViewBag.SignITManagerByName = SignITManagerByName.Name; }
            if (qHeader.SignGMITBy != null) { var SignGMITByName = db.V_Users_Active.Where(w => w.NIK == qHeader.SignGMITBy).FirstOrDefault(); ViewBag.SignGMITByName = SignGMITByName.Name; }
            if (qHeader.CompleteITStaffBy != null) { var CompleteITStaffByName = db.V_Users_Active.Where(w => w.NIK == qHeader.CompleteITStaffBy).FirstOrDefault(); ViewBag.CompleteITStaffByName = CompleteITStaffByName.Name; }
            ViewBag.header = qHeader;
            ViewBag.Lines = qLines;
            ViewBag.LinesSub2 = qLinesSub2;
            ViewBag.LinesSub3 = qLinesSub3;
            ViewBag.currUser = currUser;
            ViewBag.StaffIT = listITStaff;

            ViewBag.SectionList = SectionList;

            return View();
        }
        [HttpPost]
        public ActionResult UpdateRequestSystem(IT_RequestSystem_Header smodel, String[] ItemAccess, List<string> LineSub_Name, List<string> LineSub_Explain, List<int> LineItemID, string submitValue)
        {
            if (smodel == null)
            {
                return Json(new { status = 0, msg = "Request data tidak ditemukan." });
            }

            byte isChecked;

            // update header
            var qHeader = dbrs.IT_RequestSystem_Header.Where(w => w.ID == smodel.ID).FirstOrDefault();
            if (qHeader == null)
            {
                return Json(new { status = 0, msg = "Request tidak ditemukan." });
            }

            try
            {
                qHeader.NIK = smodel.NIK;
            qHeader.EmployeeName = smodel.EmployeeName;
            qHeader.Department = smodel.Department;
            var sHeader = dbrs.SaveChanges();
            // update header
                      
            int SignAction;
            if (submitValue == "saveOnly")
            {
                // save onlye, no sign action
                SignAction = 0;
                var qLines = dbrs.IT_RequestSystem_Lines.Where(w => w.ReqID == smodel.ID).ToList();
                foreach (var ln in qLines)
                {
                    isChecked = Convert.ToByte(ItemAccess != null && ItemAccess.Contains(ln.ItemID.ToString()) ? 1 : 0);
                    ln.IsChecked = isChecked;
                    ln.Explains = Request.Form[$"explains[{ln.ItemID}]"];
                    dbrs.SaveChanges();
                }
                var qLinesSub = dbrs.IT_RequestSystem_LinesSub.Where(w => w.ReqID == smodel.ID).ToList();
                foreach (var ls in qLinesSub)
                {
                    //byte isChecked = Convert.ToByte(ItemAccess.Contains(ln.ItemID.ToString()) ? 1 : 0);
                    ls.LineSub_Explain = Request.Form[$"LineSub_Explain[{ls.LineItemID}]"];
                    dbrs.SaveChanges();
                }
            } 
            else if (submitValue == "Submit")
            {
                // if submit, then take sign action
                SignAction = SignAct(smodel.ID, submitValue);
                var qLines = dbrs.IT_RequestSystem_Lines.Where(w => w.ReqID == smodel.ID).ToList();
                foreach (var ln in qLines)
                {
                    isChecked = Convert.ToByte(ItemAccess != null && ItemAccess.Contains(ln.ItemID.ToString()) ? 1 : 0);
                    ln.IsChecked = isChecked;
                    ln.Explains = Request.Form[$"explains[{ln.ItemID}]"];
                    dbrs.SaveChanges();
                }
                var qLinesSub = dbrs.IT_RequestSystem_LinesSub.Where(w => w.ReqID == smodel.ID).ToList();
                foreach (var ls in qLinesSub)
                {
                    //byte isChecked = Convert.ToByte(ItemAccess.Contains(ln.ItemID.ToString()) ? 1 : 0);
                    ls.LineSub_Explain = Request.Form[$"LineSub_Explain[{ls.LineItemID}]"];
                    dbrs.SaveChanges();
                }                
            }
            else
            {
                SignAction = SignAct(smodel.ID, submitValue);
            }
                return Json(new { status = 1, msg = "saved Success", reqID = smodel.ID, SignAct = SignAction, submitValue = submitValue });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { status = 0, msg = ex.Message, reqID = smodel.ID, submitValue = submitValue });
            }
        }

        private int SignAct(int ReqID, string ProcessAction)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            if (CurrUser == null)
            {
                throw new InvalidOperationException("User aktif tidak ditemukan untuk NIK " + currUser + ".");
            }

            string status;
            var EmailSendTo = new ArrayList();



            var dataHeader = dbrs.IT_RequestSystem_Header.Where(w => w.ID == ReqID).FirstOrDefault();
            if (dataHeader == null)
            {
                throw new InvalidOperationException("Request dengan ID " + ReqID + " tidak ditemukan.");
            }

            // get requestor name
            var qReqUser = db.V_Users_Active.Where(w => w.NIK == dataHeader.CreateBy).FirstOrDefault();

            if (ProcessAction == "Submit")
            {
                dataHeader.Status = "submit";
                dataHeader.CreateBy = CurrUser.NIK;
                dataHeader.CreateTime = DateTime.Now;

                // get requestor Manager
                var qReqManager = db.Master_Organization.Where(w => w.OrganizationName == CurrUser.CostName && w.userLevel == 1).FirstOrDefault();
                if (qReqManager == null)
                {
                    throw new InvalidOperationException("Manager requestor tidak ditemukan untuk organisasi '" + CurrUser.CostName + "' dengan userLevel 1.");
                }

                dataHeader.SignReqManagerBy = qReqManager.OrganizationUser;

                // set status
                status = "Submitted Request";
                ////insert ke table task_list untuk notifikasi verifikasi user
                //Task_List task = new Task_List();
                //task.TaskName = "Approval";
                //task.TaskForUser = dataHeader.NIK;
                //task.ModuleArea = "IT";
                //task.ModuleController = "FormCheckSheet";
                //task.Module = "DetailCheckSheetMtcHardSoft";
                //task.IsActive = 1;
                //task.ModuleID = HeaderID.ToString();
                //task.ModuleParameter = "HeaderID";

                //db.Task_List.Add(task);
                //db.SaveChanges();

                // add email recipient
                EmailSendTo.Add(qReqManager.OrganizationUser);

            }
            else if (ProcessAction == "signReqManager")
            {
                dataHeader.SignReqManagerBy = CurrUser.NIK;
                dataHeader.SignReqManagerTime = DateTime.Now;                
                dataHeader.Status = "Sign Requestor Manager";

                // get manager IT
                var qITmanager = db.Master_Organization.Where(w => w.DeptCode == "B2400" && w.userLevel == 1).FirstOrDefault();
                dataHeader.SignITManagerBy = qITmanager.OrganizationUser;
                
                // set status
                status = "Sign By Requestor Manager";
                //// insert ke table task_list untuk notifikasi approval user
                //var approvalUserIT = db.Master_Organization.Where(w => w.DeptCode == "B2400").ToList();
                //foreach (var approval in approvalUserIT)
                //{
                //    Task_List task = new Task_List();
                //    task.TaskName = "Approval";
                //    task.TaskForUser = approval.OrganizationUser;
                //    task.ModuleArea = "IT";
                //    task.ModuleController = "FormCheckSheet";
                //    task.Module = "DetailCheckSheetMtcHardSoft";
                //    task.IsActive = 1;
                //    task.ModuleID = HeaderID.ToString();
                //    task.ModuleParameter = "HeaderID";

                //    db.Task_List.Add(task);
                //}

                //db.SaveChanges();

                // add email recipient
                EmailSendTo.Add(qITmanager.OrganizationUser);


            }
            else if (ProcessAction == "SignManagerIT")
            {
                dataHeader.SignITManagerBy = CurrUser.NIK;
                dataHeader.SignITManagerTime = DateTime.Now;
                dataHeader.SignGMITBy = "546.08.05";
                dataHeader.Status = "Sign IT Manager";

                // set status
                status = "Sign By IT Manager";

                // add email recipient
                EmailSendTo.Add(dataHeader.SignGMITBy);

            }
            else if (ProcessAction == "SignGMIT")
            {
                dataHeader.SignGMITBy = CurrUser.NIK;
                dataHeader.SignGMITTime = DateTime.Now;

                dataHeader.Status = "Sign GM IT";

                // set status
                status = "Sign By GM IT";

                // add email recipient
                var listITStaff = db.V_Users_Active.Where(w => w.DeptName == "INFORMATION TECHNOLOGY" && w.PositionName != "MANAGER").Select(r => r.NIK).ToList();
                EmailSendTo.Add(listITStaff);

            } else if (ProcessAction == "complete")
            {
                dataHeader.CompleteITStaffBy = CurrUser.NIK;
                dataHeader.CompleteITStaffTime = DateTime.Now;

                dataHeader.Status = "Complete";

                // set status
                status = "Completed";
                // add email recipient
                EmailSendTo.Add(dataHeader.CreateBy);
            }
            else
            {
                status = "not found";
            }
            int s = dbrs.SaveChanges();
            if (s > 0)
            {
                // Resolve and process approval task updates
                string referralId = ReqID.ToString();
                int menuId = 138;
                string module = "IT Request System";
                string detailUrl = "~/IT/RequestSystem/RequestForm?ID=" + ReqID;

                if (ProcessAction == "Submit")
                {
                    // get requestor Manager
                    var qReqManager = db.Master_Organization.Where(w => w.OrganizationName == CurrUser.CostName && w.userLevel == 1).FirstOrDefault();
                    if (qReqManager != null)
                    {
                        NGKBusi.Helpers.ApprovalHelper.SaveSingleApproval(
                            nik: qReqManager.OrganizationUser,
                            menuId: menuId,
                            module: module,
                            referralId: referralId,
                            deadline: DateTime.Now.AddDays(1),
                            taskTitle: "Sign Requestor Manager",
                            documentId: null,
                            detailUrl: detailUrl,
                            taskType: "Acknowledge"
                        );
                    }
                }
                else if (ProcessAction == "signReqManager")
                {
                    // Complete previous manager task
                    NGKBusi.Helpers.ApprovalHelper.CompleteApprovalTaskForGroup(menuId, referralId, "Acknowledge");

                    // Create next step task for IT Manager
                    var qITmanager = db.Master_Organization.Where(w => w.DeptCode == "B2400" && w.userLevel == 1).FirstOrDefault();
                    if (qITmanager != null)
                    {
                        NGKBusi.Helpers.ApprovalHelper.SaveSingleApproval(
                            nik: qITmanager.OrganizationUser,
                            menuId: menuId,
                            module: module,
                            referralId: referralId,
                            deadline: DateTime.Now.AddDays(1),
                            taskTitle: "Sign IT Manager",
                            documentId: null,
                            detailUrl: detailUrl,
                            taskType: "Verify"
                        );
                    }
                }
                else if (ProcessAction == "SignManagerIT")
                {
                    // Complete previous IT Manager task
                    NGKBusi.Helpers.ApprovalHelper.CompleteApprovalTaskForGroup(menuId, referralId, "Verify");

                    // Create next step task for GM IT
                    NGKBusi.Helpers.ApprovalHelper.SaveSingleApproval(
                        nik: "546.08.05",
                        menuId: menuId,
                        module: module,
                        referralId: referralId,
                        deadline: DateTime.Now.AddDays(1),
                        taskTitle: "Sign GM IT",
                        documentId: null,
                        detailUrl: detailUrl,
                        taskType: "Approval"
                    );
                }
                else if (ProcessAction == "SignGMIT")
                {
                    // Complete previous GM IT task
                    NGKBusi.Helpers.ApprovalHelper.CompleteApprovalTaskForGroup(menuId, referralId, "Approval");

                    // Query IT Staff list
                    var listITStaff = db.V_Users_Active.Where(w => w.DeptName == "INFORMATION TECHNOLOGY" && w.PositionName != "MANAGER").Select(r => r.NIK).ToList();
                    var newTasks = new List<Task_Approval_List>();
                    foreach (var staffNik in listITStaff)
                    {
                        newTasks.Add(new Task_Approval_List
                        {
                            NIK = staffNik,
                            Menu_Id = menuId,
                            Module = module,
                            Referal_id = referralId,
                            DetailUrl = detailUrl,
                            TaskType = "Finalization",
                            ApprovalDeadline = DateTime.Now.AddDays(1),
                            TaskTitle = "Complete IT Staff Request",
                            DocumentID = null
                        });
                    }
                    if (newTasks.Any())
                    {
                        NGKBusi.Helpers.ApprovalHelper.SaveApprovalList(newTasks);
                    }
                }
                else if (ProcessAction == "complete")
                {
                    // Complete the IT staff assignment task
                    NGKBusi.Helpers.ApprovalHelper.CompleteApprovalTaskForGroup(menuId, referralId, "Finalization");
                }

                SendEmail(ReqID, dataHeader.RequestNo, EmailSendTo, status, qReqUser.Name );
            }
            return s;
        }

        public void SendEmail(int ReqID, string RequestNo, ArrayList emailSendTo, string Status, string RequestorName)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();


            string FilePath = Path.Combine(Server.MapPath("~/Emails/IT/ITForm/"), "notif.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();

            string domain = Request.Url.Scheme + "://" + Request.Url.Authority;
            string actionUrl = domain + Url.Action("RequestForm", "RequestSystem", new { area = "IT", ID = ReqID });

            //Repalce [newusername] = signup user name      
            MailText = MailText.Replace("##RequestNo##", RequestNo);
            MailText = MailText.Replace("##Url##", actionUrl);
            MailText = MailText.Replace("##Status##", Status);
            MailText = MailText.Replace("##requestByName##", RequestorName);

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "NGK Portal");
            //var receiverEmail = new MailAddress(EmailAddress, "Receiver");
            var password = "100%NGKbusi!";
            var sub = "Portal Apps Notification";
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

                foreach (string dataEmail in emailSendTo)
                {
                    if (dataEmail.Length > 0)
                    {
                        var CurrUser = db.V_Users_Active.Where(w => w.NIK == dataEmail).First();
                        var EmailAddress = CurrUser.Email;

                        mess.To.Add(new MailAddress(EmailAddress));

                    }

                }
                mess.Bcc.Add(new MailAddress("ikhsan.sholihin@ngkbusi.com"));
                smtp.Send(mess);
            }


        }

    }
}