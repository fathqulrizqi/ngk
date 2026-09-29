using Microsoft.AspNet.Identity;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Models;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace NGKBusi.Areas.IT.Controllers
{
    public class ReminderController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        ReminderConnection dbr = new ReminderConnection();
        // GET: IT/Reminder
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        [Authorize]
        public ActionResult Home()
        {
          
            return View();
        }
        
        public JsonResult GetReminderDashData()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            List<Tbl_Event> events = new List<Tbl_Event>();
           
            

            var spl = (from r in dbr.IT_Reminder
                       where 
                       //r.IsActive == 1 &&
                       r.CreateBy == currUser
                       select new
                       {
                           ID = r.ID,

                       }).ToList();
            var spl2 = (from ru in dbr.IT_Reminder_User
                        join r in dbr.IT_Reminder on ru.ReminderID equals r.ID
                        where ru.SendToUserEmail == CurrUser.Email
                        select new
                        {
                            ID = r.ID,

                        }).ToList();

            var newList = spl.Union(spl2);

            foreach (var item in newList)
            {
                var dtlReminder = dbr.IT_Reminder.Where(w => w.ID == item.ID).FirstOrDefault();
                events.Add(new Tbl_Event
                {
                    id = Convert.ToInt32(dtlReminder.ID),
                    title = dtlReminder.ReminderTitle,
                    start = dtlReminder.DueDate.ToString("yyyy-MM-dd") + "T" + dtlReminder.NotifTime + ":00:00",
                    end = dtlReminder.DueDate.ToString("yyyy-MM-dd") + "T" + dtlReminder.NotifTime + ":59:00",
                    IsActive = dtlReminder.IsActive
                });
                
            }

            return Json(events, JsonRequestBehavior.AllowGet);
        }
        
        public JsonResult GetReminderListUpcoming()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            
            List<Tbl_Event> upcomingEvents = new List<Tbl_Event>();
            
            var spl = (from r in dbr.IT_Reminder
                       where r.IsActive == 1 && r.CreateBy == currUser
                       select new
                       {
                           ID = r.ID,
                           
                       }).ToList();
            var spl2 = (from ru in dbr.IT_Reminder_User
                        join r in dbr.IT_Reminder on ru.ReminderID equals r.ID
                        where ru.SendToUserEmail == CurrUser.Email
                        select new
                        {
                            ID = r.ID,
                            
                        }).ToList();

            var newList = spl.Union(spl2);

            foreach (var item in newList)
            {
                var dtlReminder = dbr.IT_Reminder.Where(w => w.ID == item.ID).FirstOrDefault();
                if (dtlReminder.DueDate.Date >= DateTime.Now.Date)
                {
                    upcomingEvents.Add(new Tbl_Event
                    {
                        id = Convert.ToInt32(dtlReminder.ID),
                        title = dtlReminder.ReminderTitle,
                        start = dtlReminder.DueDate.ToString("yyyy-MM-dd") + "T" + dtlReminder.NotifTime + ":00:00",
                        end = dtlReminder.DueDate.ToString("yyyy-MM-dd") + "T" + dtlReminder.NotifTime + ":59:00",
                        module = dtlReminder.Module,
                        Thirdparty = dtlReminder.Thirdparty,
                        IsActive = dtlReminder.IsActive
                    });
                }

            }

            return Json(upcomingEvents, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetReminderDashInfo(int id)
        {
            var spl = dbr.IT_Reminder.Where(w => w.ID == id).FirstOrDefault();
            if (spl != null)
            {
                return Json(new
                {
                    title = spl.ReminderTitle,
                    start = spl.DueDate.ToString("yyyy-MM-dd") + "T" + spl.NotifTime + ":00:00",
                    end = spl.DueDate.ToString("yyyy-MM-dd") + "T" + spl.NotifTime + ":00:00" // Jika end date tidak null
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(null, JsonRequestBehavior.AllowGet);

        }

        [Authorize]
        
        public ActionResult AddReminder()
        {
            var ListEmail = db.V_Users_Active.Where(w => w.Email != null).GroupBy(g => g.Email).Select(s => new ListEmail { Email = s.Key }).ToList();
            var ListVendor = db.V_AXVendorList.ToList();
            ViewBag.ListEmail = ListEmail;
            ViewBag.ListVendor = ListVendor;
            //return View();
            return PartialView();
        }
        [HttpPost]
        public ActionResult AddReminder(IT_Reminder smodel, string[] selReminderUser, string txtDueDate, HttpPostedFileBase FileAttachment)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            int status = 0;
            string msg = "";
            //DateTime DueDate = DateTime.Now;
            var DueDate = DateTime.ParseExact(txtDueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            // action insert to IT_reminder // 
            IT_Reminder reminder = new IT_Reminder();
            reminder.ReminderTitle = smodel.ReminderTitle;
            reminder.Module = smodel.Module;
            reminder.Type = smodel.Type;
            reminder.Thirdparty = smodel.Thirdparty;
            reminder.DueDate = DueDate;
            reminder.Description = smodel.Description;
            reminder.NotifStart = smodel.NotifStart * -1;
            reminder.NotifTime = smodel.NotifTime;
            reminder.IntervalRepetReminderType = smodel.IntervalRepetReminderType;
            reminder.IntervalRepeatReminderNumber = smodel.IntervalRepeatReminderNumber;
            reminder.IntervalRepeatNotifType = smodel.IntervalRepeatNotifType;
            reminder.IntervalRepeatNotifNumber = smodel.IntervalRepeatNotifNumber;
            reminder.CreateTime = DateTime.Now;
            reminder.CreateBy = CurrUser.NIK;
            reminder.IsActive = 1;

            // get file attachment
            string filePath = "";
            string fileName = "";
            HttpPostedFileBase uploadFile = FileAttachment;
            if (uploadFile != null && uploadFile.ContentLength > 0)
            {
                fileName = uploadFile.FileName;
                string extension = Path.GetExtension(fileName);
                filePath = Server.MapPath("~/Files/IT/Reminder/");
                filePath = filePath + fileName;

                string deletedFile = filePath + reminder.Attachment;
                //remove old attachment
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                reminder.Attachment = fileName;                
            }
            else
            {
                fileName = "not found";
                filePath = "not found";
            }
            dbr.IT_Reminder.Add(reminder);
            int ins = dbr.SaveChanges();

            if (ins > 0)
            {
                // upload file attachment
                uploadFile.SaveAs(filePath);
                // action insert to IT_reminder_user
                foreach (var user in selReminderUser)
                {
                    //var userInfo = db.V_Users_Active.Where(w => w.NIK == user).FirstOrDefault();
                    IT_Reminder_User userList = new IT_Reminder_User();
                    userList.ReminderID = reminder.ID;
                    userList.SendToUser = user;
                    userList.SendToUserEmail = user;
                    userList.IsActive = 1;

                    dbr.IT_Reminder_User.Add(userList);
                }
                int ins_user = dbr.SaveChanges();
                if (ins_user > 0)
                {
                    // action insert to IT_reminder_task
                    IT_Reminder_Task newTask = new IT_Reminder_Task();
                    newTask.ReminderID = reminder.ID;
                    newTask.ReminderDate = reminder.DueDate.AddDays(reminder.NotifStart).Date;
                    newTask.ReminderTime = Convert.ToInt32(smodel.NotifTime);
                    newTask.ReminderDueDate = reminder.DueDate;

                    dbr.IT_Reminder_Task.Add(newTask);
                    int ins_task = dbr.SaveChanges();
                    if (ins_task > 0)
                    {
                        msg = "Success Insert Reminder";
                        status = 1;
                    }
                    else
                    {
                        msg = "Failed Save Task";
                        status = 0;
                    }
                }
                else
                {
                    msg = "Failed Insert User";
                    status = 0;
                }

            }
            else
            {
                msg = "Failed Insert Reminder";
                status = 0;
            }

            return Json(new { status = status, msg = msg, reminder = reminder, DueDate = DueDate });
        }
        [HttpPost]
        public JsonResult GetReminderLIst()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var rawData = dbr.IT_Reminder.Where(w => w.CreateBy == CurrUser.NIK).ToList();
            var CountRow = rawData.Count();
            List<Tbl_IT_Reminder> data = new List<Tbl_IT_Reminder>();

            int No = 0;
            foreach (var raw in rawData)
            {
                No++;

                var UrlAction = Url.Action("FormSettingReminder", "Reminder", new { area = "IT", ID = raw.ID });
                var ActionButton = "<a href=\"" + UrlAction + "\" title=\"Setting\" class=\"btn btn-primary\"><i class=\"fa fa-cog\"></i></a>";

                // Ambil task pending (IsSend == 0) terdekat untuk reminder ini
                var pendingTask = dbr.IT_Reminder_Task
                    .Where(w => w.ReminderID == raw.ID && w.IsSend == 0)
                    .OrderBy(x => x.ReminderDate)
                    .FirstOrDefault();

                // Jika tidak ada task pending, ambil task terakhir yang pernah ada
                var lastTask = pendingTask ?? dbr.IT_Reminder_Task
                    .Where(w => w.ReminderID == raw.ID)
                    .OrderByDescending(x => x.ReminderDueDate)
                    .FirstOrDefault();

                string dueDateStr = raw.DueDate != default(DateTime)
                    ? raw.DueDate.ToString("dd MMM yyyy")
                    : (lastTask != null ? lastTask.ReminderDueDate.ToString("dd MMM yyyy") : "Not Found");

                string nextNotifStr = pendingTask != null
                    ? $"{pendingTask.ReminderDate:dd MMM yyyy} {pendingTask.ReminderTime:D2}:00"
                    : (raw.IsActive == 1 ? "Completed / Waiting Cycle" : "Inactive");

                data.Add(
                    new Tbl_IT_Reminder
                    {
                        No = No,
                        ReminderTitle = raw.ReminderTitle,
                        Module = raw.Module,
                        Type = raw.Type,
                        Thirdparty = raw.Thirdparty,
                        Description = raw.Description,
                        DueDate = dueDateStr,
                        NextNotif = nextNotifStr,
                        ActionButton = ActionButton
                    });
            }

            var jsonResult = Json(new { rows = data, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public static void RescheduleReminderTask(int reminderId, ReminderConnection dbr)
        {
            var reminder = dbr.IT_Reminder.Find(reminderId);
            if (reminder == null) return;

            if (reminder.IsActive == 0)
            {
                var pendingList = dbr.IT_Reminder_Task.Where(w => w.ReminderID == reminderId && w.IsSend == 0).ToList();
                foreach (var pt in pendingList)
                {
                    pt.IsSend = 1;
                }
                dbr.SaveChanges();
                return;
            }

            int notifTimeInt = 9;
            int.TryParse(reminder.NotifTime, out notifTimeInt);
            DateTime targetReminderDate = reminder.DueDate.AddDays(reminder.NotifStart).Date;

            var existingPending = dbr.IT_Reminder_Task.Where(w => w.ReminderID == reminderId && w.IsSend == 0).ToList();
            if (existingPending.Any())
            {
                var primaryPending = existingPending.First();
                primaryPending.ReminderDate = targetReminderDate;
                primaryPending.ReminderTime = notifTimeInt;
                primaryPending.ReminderDueDate = reminder.DueDate;

                if (existingPending.Count > 1)
                {
                    dbr.IT_Reminder_Task.RemoveRange(existingPending.Skip(1));
                }
            }
            else
            {
                if (reminder.DueDate.Date >= DateTime.Now.Date)
                {
                    IT_Reminder_Task newTask = new IT_Reminder_Task
                    {
                        ReminderID = reminder.ID,
                        ReminderDate = targetReminderDate,
                        ReminderTime = notifTimeInt,
                        ReminderDueDate = reminder.DueDate,
                        IsSend = 0
                    };
                    dbr.IT_Reminder_Task.Add(newTask);
                }
            }
            dbr.SaveChanges();
        }

        [HttpGet]
        public JsonResult GetReminderTask()
        {
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            int currentHour = now.Hour;

            // Ambil semua task yang IsSend == 0 dan jadwalnya sudah tiba atau terlewat
            var pendingTasks = (from t in dbr.IT_Reminder_Task
                                join r in dbr.IT_Reminder on t.ReminderID equals r.ID
                                where t.IsSend == 0 &&
                                      r.IsActive == 1 &&
                                      (t.ReminderDate < today || (t.ReminderDate == today && t.ReminderTime <= currentHour))
                                select t).ToList();

            int totalProcessed = 0;
            int sentCount = 0;
            List<object> logs = new List<object>();

            foreach (var task in pendingTasks)
            {
                totalProcessed++;
                var reminder = dbr.IT_Reminder.FirstOrDefault(w => w.ID == task.ReminderID && w.IsActive == 1);
                if (reminder == null)
                {
                    task.IsSend = 1;
                    dbr.SaveChanges();
                    continue;
                }

                // Ambil daftar penerima khusus task ini
                var recipients = dbr.IT_Reminder_User
                    .Where(w => w.ReminderID == task.ReminderID && w.IsActive == 1)
                    .Select(s => s.SendToUserEmail)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .Distinct()
                    .ToList();

                bool emailSent = false;
                string sendError = null;

                if (recipients.Any())
                {
                    try
                    {
                        SendEmail(
                            reminder.ReminderTitle,
                            reminder.Description,
                            reminder.Thirdparty,
                            reminder.DueDate.ToString("dd MMM yyyy"),
                            recipients
                        );
                        emailSent = true;
                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        sendError = ex.Message;
                        try { Elmah.ErrorSignal.FromCurrentContext()?.Raise(ex); } catch { }
                    }
                }
                else
                {
                    sendError = "No valid recipient email configured.";
                }

                // Tandai task saat ini sudah diproses
                task.IsSend = 1;
                dbr.SaveChanges();

                // GENERATE NEXT TASK SESUAI KONFIGURASI IT_Reminder
                bool repeatTask = !string.Equals(reminder.IntervalRepeatNotifType, "OneTime", StringComparison.OrdinalIgnoreCase) 
                                  && !string.IsNullOrEmpty(reminder.IntervalRepeatNotifType);
                bool repeatReminderCycle = !string.Equals(reminder.IntervalRepetReminderType, "OneTime", StringComparison.OrdinalIgnoreCase) 
                                           && !string.IsNullOrEmpty(reminder.IntervalRepetReminderType);

                int intervalNotifNumber = reminder.IntervalRepeatNotifNumber > 0 ? reminder.IntervalRepeatNotifNumber : 1;
                int intervalCycleNumber = reminder.IntervalRepeatReminderNumber > 0 ? reminder.IntervalRepeatReminderNumber : 1;
                int notifHour = 9;
                int.TryParse(reminder.NotifTime, out notifHour);

                if (repeatTask)
                {
                    DateTime nextDate;
                    int nextHour = notifHour;

                    if (string.Equals(reminder.IntervalRepeatNotifType, "hour", StringComparison.OrdinalIgnoreCase))
                    {
                        DateTime nextDateTime = task.ReminderDate.Date.AddHours(task.ReminderTime).AddHours(intervalNotifNumber);
                        if (nextDateTime < now)
                        {
                            nextDateTime = now.AddHours(intervalNotifNumber);
                        }
                        nextDate = nextDateTime.Date;
                        nextHour = nextDateTime.Hour;
                    }
                    else // default "day"
                    {
                        nextDate = task.ReminderDate.Date.AddDays(intervalNotifNumber);
                        if (nextDate < today)
                        {
                            nextDate = today.AddDays(intervalNotifNumber);
                        }
                    }

                    if (nextDate <= reminder.DueDate.Date)
                    {
                        IT_Reminder_Task nextTask = new IT_Reminder_Task
                        {
                            ReminderID = reminder.ID,
                            ReminderDate = nextDate,
                            ReminderTime = nextHour,
                            ReminderDueDate = reminder.DueDate,
                            IsSend = 0
                        };
                        dbr.IT_Reminder_Task.Add(nextTask);
                        dbr.SaveChanges();
                    }
                    else
                    {
                        if (repeatReminderCycle)
                        {
                            DateTime newDueDate;
                            if (string.Equals(reminder.IntervalRepetReminderType, "year", StringComparison.OrdinalIgnoreCase))
                            {
                                newDueDate = reminder.DueDate.AddYears(intervalCycleNumber).Date;
                            }
                            else
                            {
                                newDueDate = reminder.DueDate.AddMonths(intervalCycleNumber).Date;
                            }

                            DateTime newStartDate = newDueDate.AddDays(reminder.NotifStart).Date;

                            IT_Reminder_Task nextCycleTask = new IT_Reminder_Task
                            {
                                ReminderID = reminder.ID,
                                ReminderDate = newStartDate,
                                ReminderTime = notifHour,
                                ReminderDueDate = newDueDate,
                                IsSend = 0
                            };
                            reminder.DueDate = newDueDate;
                            dbr.IT_Reminder_Task.Add(nextCycleTask);
                            dbr.SaveChanges();
                        }
                        else
                        {
                            if (reminder.DueDate.Date <= today)
                            {
                                reminder.IsActive = 0;
                                dbr.SaveChanges();
                            }
                        }
                    }
                }
                else
                {
                    if (repeatReminderCycle)
                    {
                        DateTime newDueDate;
                        if (string.Equals(reminder.IntervalRepetReminderType, "year", StringComparison.OrdinalIgnoreCase))
                        {
                            newDueDate = reminder.DueDate.AddYears(intervalCycleNumber).Date;
                        }
                        else
                        {
                            newDueDate = reminder.DueDate.AddMonths(intervalCycleNumber).Date;
                        }

                        DateTime newStartDate = newDueDate.AddDays(reminder.NotifStart).Date;

                        IT_Reminder_Task nextCycleTask = new IT_Reminder_Task
                        {
                            ReminderID = reminder.ID,
                            ReminderDate = newStartDate,
                            ReminderTime = notifHour,
                            ReminderDueDate = newDueDate,
                            IsSend = 0
                        };
                        reminder.DueDate = newDueDate;
                        dbr.IT_Reminder_Task.Add(nextCycleTask);
                        dbr.SaveChanges();
                    }
                    else
                    {
                        if (reminder.DueDate.Date <= today)
                        {
                            reminder.IsActive = 0;
                            dbr.SaveChanges();
                        }
                    }
                }

                logs.Add(new
                {
                    taskId = task.ID,
                    reminderId = reminder.ID,
                    title = reminder.ReminderTitle,
                    recipients = recipients,
                    sent = emailSent,
                    error = sendError
                });
            }

            return Json(new
            {
                status = 1,
                msg = $"Processed {totalProcessed} reminder task(s), {sentCount} email(s) sent successfully.",
                totalProcessed = totalProcessed,
                sentCount = sentCount,
                details = logs
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult SyncAllReminderTasks()
        {
            var activeReminders = dbr.IT_Reminder.Where(w => w.IsActive == 1).ToList();
            int syncedCount = 0;
            int deactivatedCount = 0;

            foreach (var r in activeReminders)
            {
                bool repeatCycle = !string.Equals(r.IntervalRepetReminderType, "OneTime", StringComparison.OrdinalIgnoreCase) 
                                   && !string.IsNullOrEmpty(r.IntervalRepetReminderType);
                
                if (r.DueDate.Date < DateTime.Now.Date && !repeatCycle)
                {
                    r.IsActive = 0;
                    var oldPending = dbr.IT_Reminder_Task.Where(w => w.ReminderID == r.ID && w.IsSend == 0).ToList();
                    foreach (var op in oldPending) op.IsSend = 1;
                    deactivatedCount++;
                }
                else
                {
                    RescheduleReminderTask(r.ID, dbr);
                    syncedCount++;
                }
            }
            dbr.SaveChanges();

            return Json(new
            {
                status = 1,
                msg = $"Sync completed. {syncedCount} active reminder(s) checked/rescheduled, {deactivatedCount} expired reminder(s) deactivated."
            }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [HttpGet]
        public ActionResult FormSettingReminder(int ID)
        {
            var reminder = dbr.IT_Reminder.Where(w => w.ID == ID).FirstOrDefault();
            var reminderUser = dbr.IT_Reminder_User.Where(w => w.ReminderID == ID).ToList();
            var ListEmail = db.V_Users_Active.Where(w => w.Email != null).GroupBy(g => g.Email).Select(s => new ListEmail { Email = s.Key }).ToList();
            var ListNewEmail = dbr.IT_Reminder_User.GroupBy(g => g.SendToUser).Select(s => new ListEmail { Email = s.Key }).ToList();
            var ListVendor = db.V_AXVendorList.ToList();            

            ViewBag.reminder = reminder;
            ViewBag.reminderUser = reminderUser;
            ViewBag.ListEmail = ListEmail.Union(ListNewEmail).GroupBy(x => x.Email).Select(y => new ListEmail { Email = y.Key }).ToList();
            ViewBag.ListVendor = ListVendor;
            return PartialView();
        }

        [HttpPost]
        public ActionResult UpdateSettingReminder(IT_Reminder smodel, string[] selReminderUser, string txtDueDate)
        {
            var DueDate = DateTime.ParseExact(txtDueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            // action update IT_reminder // 
            var reminder = dbr.IT_Reminder.Where(w => w.ID == smodel.ID).FirstOrDefault();
            if (reminder == null)
            {
                return Json(new { msg = "Reminder not found", status = 0 });
            }

            reminder.ReminderTitle = smodel.ReminderTitle;
            reminder.Module = smodel.Module;
            reminder.Type = smodel.Type;
            reminder.Thirdparty = smodel.Thirdparty;
            reminder.DueDate = DueDate;
            reminder.Description = smodel.Description;
            reminder.NotifStart = smodel.NotifStart * -1;
            reminder.NotifTime = smodel.NotifTime;
            reminder.IntervalRepetReminderType = smodel.IntervalRepetReminderType;
            reminder.IntervalRepeatReminderNumber = smodel.IntervalRepeatReminderNumber;
            reminder.IntervalRepeatNotifType = smodel.IntervalRepeatNotifType;
            reminder.IntervalRepeatNotifNumber = smodel.IntervalRepeatNotifNumber;

            string filePath = "";
            string fileName = "";
            HttpPostedFileBase uploadFile = Request.Files["FileAttachment"];
            if (uploadFile != null && uploadFile.ContentLength > 0)
            {
                fileName = uploadFile.FileName;
                filePath = Server.MapPath("~/Files/IT/Reminder/");
                filePath = filePath + fileName;

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                reminder.Attachment = fileName;
                uploadFile.SaveAs(filePath);
            }

            dbr.SaveChanges();

            // Reschedule active task
            RescheduleReminderTask(reminder.ID, dbr);

            return Json(new { msg = "Update Successfully", status = 1, data = reminder, duedate = txtDueDate, id = smodel.ID });
        }

        [Authorize]
        [HttpPost]
        public ActionResult StopNotificationReminder(IT_Reminder smodel)
        {
            var reminder = dbr.IT_Reminder.Where(w => w.ID == smodel.ID).FirstOrDefault();
            if (reminder == null)
            {
                return Json(new { msg = "Reminder not found", status = 0 });
            }

            reminder.IsActive = 0;

            // update all remaining tasks status isSend = 1
            var pendingTasks = dbr.IT_Reminder_Task.Where(w => w.ReminderID == smodel.ID && w.IsSend == 0).ToList();
            foreach (var t in pendingTasks)
            {
                t.IsSend = 1;
            }
            dbr.SaveChanges();

            return Json(new { msg = "Stop Notification Successfully", status = 1, reminder = reminder });
        }

        public void SendEmail(string ReminderTitle, string Description, string ThirdParty, string DueDate, IEnumerable<string> emailSendTo)
        {
            var validEmails = (emailSendTo ?? Enumerable.Empty<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e) && e.Contains("@"))
                .Distinct()
                .ToList();

            if (!validEmails.Any())
            {
                return;
            }

            string filePath = null;
            if (HostingEnvironment.IsHosted)
            {
                filePath = HostingEnvironment.MapPath("~/Emails/IT/Reminder/notif.html");
            }
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                if (System.Web.HttpContext.Current != null)
                {
                    filePath = Server.MapPath("~/Emails/IT/Reminder/notif.html");
                }
            }

            string mailText = "";
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                using (var str = new StreamReader(filePath))
                {
                    mailText = str.ReadToEnd();
                }
            }
            else
            {
                mailText = "<p>You have an upcoming reminder task.</p><p><b>Title:</b> ##ReminderTitle##</p><p><b>Third Party:</b> ##ThirdParty##</p><p><b>Due Date:</b> ##ReminderDueDate##</p>";
            }

            mailText = mailText.Replace("##ReminderTitle##", ReminderTitle ?? "-");
            mailText = mailText.Replace("##ThirdParty##", ThirdParty ?? "-");
            mailText = mailText.Replace("##ReminderDueDate##", DueDate ?? "-");

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Portal Reminder");
            var password = "100%NGKbusi!";
            var sub = string.IsNullOrWhiteSpace(ReminderTitle) ? "Portal Reminder" : $"[Reminder] {ReminderTitle} - Niterra Portal";

            using (var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, password)
            })
            using (var mess = new MailMessage
            {
                From = senderEmail,
                Subject = sub,
                Body = mailText,
                IsBodyHtml = true
            })
            {
                foreach (string dataEmail in validEmails)
                {
                    mess.To.Add(new MailAddress(dataEmail.Trim()));
                }

                smtp.Send(mess);
            }
        }

        [HttpGet]
        public ActionResult ViewPDF(string fileName)
        {
            string filePath = "~/Files/IT/Reminder/" + fileName;
            Response.AddHeader("Content-Disposition", "inline; filename=" + fileName);

            return File(filePath, "application/pdf");
        }

        [HttpGet]
        public ActionResult SendOverdueReminders()
        {
            var result = Helpers.ApprovalHelper.SendOverdueTaskReminders();
            return Json(new
            {
                status = 1,
                totalTasks = result.TotalTasks,
                totalUsers = result.TotalUsers,
                emailsSent = result.EmailsSent,
                sentUsers = result.SentUsers,
                skippedUsers = result.SkippedUsers,
                errors = result.Errors,
                msg = $"Successfully processed {result.TotalTasks} overdue task(s) across {result.TotalUsers} user(s). {result.EmailsSent} reminder email(s) sent."
            }, JsonRequestBehavior.AllowGet);
        }
    }
}