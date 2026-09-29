using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Net;
using System.Net.Mail;
using System.Web;
using NGKBusi.Models;

namespace NGKBusi.Helpers
{
    public class OverdueReminderResult
    {
        public int TotalTasks { get; set; }
        public int TotalUsers { get; set; }
        public int EmailsSent { get; set; }
        public List<string> SentUsers { get; set; } = new List<string>();
        public List<string> SkippedUsers { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public static class ApprovalHelper
    {
        public static void SaveApprovalList(List<Task_Approval_List> approvals)
        {
            if (approvals == null || !approvals.Any()) return;

            using (var db = new DefaultConnection())
            {
                foreach (var approval in approvals)
                {
                    if (string.IsNullOrEmpty(approval.TaskType))
                    {
                        approval.TaskType = "Approval";
                    }
                    // If deadline is not specified, default to 1 day from now
                    if (approval.ApprovalDeadline == default(DateTime))
                    {
                        approval.ApprovalDeadline = DateTime.Now.AddDays(1);
                    }
                    approval.IsCompleteTask = false;
                    approval.CreatedDate = DateTime.Now;
                    db.Task_Approval_Lists.Add(approval);
                }
                db.SaveChanges();

                // Push real-time SignalR notifications to connection groups for each NIK
                try
                {
                    var hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<NGKBusi.SignalR.NotificationHub>();
                    if (hubContext != null)
                    {
                        foreach (var approval in approvals)
                        {
                            string message = $"New task '{approval.TaskType}' for {approval.Module} (Ref: {approval.Referal_id})";
                            string detailUrl = approval.DetailUrl ?? "";
                            if (!string.IsNullOrEmpty(detailUrl))
                            {
                                if (detailUrl.StartsWith("~/")) detailUrl = detailUrl.Substring(2);
                                else if (detailUrl.StartsWith("/")) detailUrl = detailUrl.Substring(1);
                            }
                            
                            string appPath = "/NGKBusi";
                            string absoluteUrl = $"{appPath}/{detailUrl}";

                            hubContext.Clients.Group(approval.NIK).broadcastNotif("Portal Notification", message, absoluteUrl);
                        }
                    }
                }
                catch (Exception) { /* Skip SignalR broadcast failure */ }
            }
        }

        /// <summary>
        /// Helper to save a single approval request with optional detail URL and custom task type.
        /// </summary>
        public static void SaveSingleApproval(string nik, int menuId, string module, string referralId, DateTime deadline, string taskTitle, int? documentId = null, string detailUrl = null, string taskType = "Approval")
        {
            var approval = new Task_Approval_List
            {
                NIK = nik,
                Menu_Id = menuId,
                Module = module,
                Referal_id = referralId,
                DetailUrl = detailUrl,
                TaskType = string.IsNullOrEmpty(taskType) ? "Approval" : taskType,
                ApprovalDeadline = deadline,
                TaskTitle = taskTitle,
                DocumentID = documentId,
                IsCompleteTask = false
            };
            SaveApprovalList(new List<Task_Approval_List> { approval });
        }

        /// <summary>
        /// Retrieves pending approval tasks for a specific user (NIK).
        /// </summary>
        public static List<Task_Approval_List> GetPendingApprovals(string nik)
        {
            using (var db = new DefaultConnection())
            {
                return db.Task_Approval_Lists
                    .Include(a => a.Menu)
                    .Where(a => a.NIK == nik && !a.IsCompleteTask)
                    .OrderBy(a => a.ApprovalDeadline)
                    .ToList();
            }
        }

        /// <summary>
        /// Marks a specific approval task as completed/approved.
        /// </summary>
        public static bool CompleteApprovalTask(string nik, int menuId, string referralId, int? documentId = null)
        {
            using (var db = new DefaultConnection())
            {
                var task = db.Task_Approval_Lists
                    .FirstOrDefault(a => a.NIK == nik && a.Menu_Id == menuId && a.Referal_id == referralId && a.DocumentID == documentId && !a.IsCompleteTask);

                if (task != null)
                {
                    task.IsCompleteTask = true;
                    task.ApprovalDate = DateTime.Now;
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Marks approval tasks for a specific group/phase as completed (completes for all assigned users).
        /// </summary>
        public static bool CompleteApprovalTaskForGroup(int menuId, string referralId, string taskType,int? documentId = null)
        {
            using (var db = new DefaultConnection())
            {
                var tasks = db.Task_Approval_Lists
                    .Where(a => a.Menu_Id == menuId && a.Referal_id == referralId && a.TaskType == taskType && a.DocumentID == documentId && !a.IsCompleteTask)
                    .ToList();

                if (tasks.Any())
                {
                    foreach (var task in tasks)
                    {
                        task.IsCompleteTask = true;
                        task.ApprovalDate = DateTime.Now;
                    }
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Cancels/completes all pending tasks for a specific referral ID.
        /// </summary>
        public static void CancelApprovalTasks(int menuId, string referralId, int? documentId = null)
        {
            using (var db = new DefaultConnection())
            {
                var tasks = db.Task_Approval_Lists
                    .Where(a => a.Menu_Id == menuId && a.Referal_id == referralId && a.DocumentID == documentId && !a.IsCompleteTask)
                    .ToList();

                foreach (var task in tasks)
                {
                    task.IsCompleteTask = true;
                    task.ApprovalDate = DateTime.Now;
                }
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Sends daily reminder email for overdue tasks.
        /// </summary>
        public static OverdueReminderResult SendOverdueTaskReminders()
        {
            var result = new OverdueReminderResult();
            using (var db = new DefaultConnection())
            {
                DateTime now = DateTime.Now;
                var overdueTasks = db.Task_Approval_Lists
                    .Include(t => t.Menu)
                    .Where(t => !t.IsCompleteTask && t.CreatedDate < now)
                    .ToList();

                result.TotalTasks = overdueTasks.Count;

                if (!overdueTasks.Any()) return result;

                var groupedTasks = overdueTasks.GroupBy(t => t.NIK).ToList();
                result.TotalUsers = groupedTasks.Count;

                foreach (var group in groupedTasks)
                {
                    string nik = group.Key;
                    var user = db.V_Users_Active.FirstOrDefault(u => u.NIK == nik);
                    if (user == null)
                    {
                        result.SkippedUsers.Add($"NIK '{nik}': User not found in V_Users_Active ({group.Count()} task(s))");
                        continue;
                    }
                    if (string.IsNullOrEmpty(user.Email))
                    {
                        result.SkippedUsers.Add($"NIK '{nik}' ({user.Name}): Email is empty ({group.Count()} task(s))");
                        continue;
                    }

                    string emailAddress = user.Email;
                    string userName = user.Name;

                    string tableRows = "";
                    foreach (var task in group)
                    {
                        string moduleName = task.Module;
                        string taskType = task.TaskType ?? "Approval";
                        string docNo = task.Referal_id;
                        string deadlineStr = task.ApprovalDeadline.ToString("dd MMM yyyy HH:mm");
                        string taskCreate = task.CreatedDate.ToString("dd MMM yyyy HH:mm");

                        string detailUrl = task.DetailUrl ?? "";
                        if (!string.IsNullOrEmpty(detailUrl))
                        {
                            if (detailUrl.StartsWith("~/")) detailUrl = detailUrl.Substring(2);
                            else if (detailUrl.StartsWith("/")) detailUrl = detailUrl.Substring(1);
                        }

                        //string domain = HttpContext.Current != null 
                        //    ? HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) 
                        //    : "https://portal.ngkbusi.com";
                        string domain = "https://portal.ngkbusi.com";

                        string appPath = HttpContext.Current != null
                            ? HttpContext.Current.Request.ApplicationPath
                            : "/NGKBusi";

                        if (appPath == "/") appPath = "";
                        string absoluteUrl = $"{domain}{appPath}/{detailUrl}";

                        tableRows += $@"
                            <tr>
                                <td style='padding: 10px; border: 1px solid #e2e8f0;'>{moduleName}</td>
                                <td style='padding: 10px; border: 1px solid #e2e8f0;'>{docNo}</td>
                                <td style='padding: 10px; border: 1px solid #e2e8f0;'><span style='background: #fef3c7; color: #d97706; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: bold;'>{taskType.ToUpper()}</span></td>
                                <td style='padding: 10px; border: 1px solid #e2e8f0; color: #dc2626; font-weight: bold;'>{taskCreate}</td>
                                <td style='padding: 10px; border: 1px solid #e2e8f0; text-align: center;'>
                                    <a href='{absoluteUrl}' style='display: inline-block; background: #0043ee; color: #ffffff; padding: 6px 12px; border-radius: 6px; text-decoration: none; font-size: 12px; font-weight: bold;'>Process</a>
                                </td>
                            </tr>";
                    }

                    string emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 650px; margin: 0 auto; color: #334155; line-height: 1.6;'>
                            <div style='background: linear-gradient(31deg, #0043ee, #007580); padding: 24px; text-align: center; border-radius: 12px 12px 0 0;'>
                                <h2 style='color: #ffffff; margin: 0; font-size: 20px;'>Overdue Tasks Reminder</h2>
                            </div>
                            <div style='padding: 24px; background: #ffffff; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 12px 12px;'>
                                <p>Dear <strong>{userName}</strong>,</p>
                                <p>You have pending approval tasks that have passed their deadline. Please review and complete these tasks as soon as possible:</p>
                                
                                <table style='width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 13px;'>
                                    <thead>
                                        <tr style='background: #f8fafc; text-align: left;'>
                                            <th style='padding: 10px; border: 1px solid #e2e8f0;'>Module / Menu</th>
                                            <th style='padding: 10px; border: 1px solid #e2e8f0;'>Doc No</th>
                                            <th style='padding: 10px; border: 1px solid #e2e8f0;'>Task Type</th>
                                            <th style='padding: 10px; border: 1px solid #e2e8f0;'>Task Create</th>
                                            <th style='padding: 10px; border: 1px solid #e2e8f0; text-align: center;'>Action</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {tableRows}
                                    </tbody>
                                </table>
                                <p style='margin-top: 24px; font-size: 12px; color: #64748b;'>
                                    This is an automated notification from NGK Portal. Please do not reply directly to this email.
                                </p>
                            </div>
                        </div>";

                    try
                    {
                        var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra Portal Reminder");
                        var receiverEmail = new MailAddress(emailAddress, userName);
                        var password = "100%NGKbusi!";
                        var smtp = new SmtpClient
                        {
                            Host = "ngkbusi.com",
                            Port = 587,
                            EnableSsl = false,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(senderEmail.Address, password)
                        };

                        using (var mess = new MailMessage(senderEmail, receiverEmail)
                        {
                            Subject = "[Reminder] Overdue Task List - Niterra Portal",
                            Body = emailBody,
                            IsBodyHtml = true
                        })
                        {
                            mess.Bcc.Add(new MailAddress("ikhsan.sholihin@ngkbusi.com"));
                            mess.Bcc.Add(new MailAddress("azis.abdillah@ngkbusi.com"));
                            smtp.Send(mess);
                        }
                        result.EmailsSent++;
                        result.SentUsers.Add($"{userName} ({emailAddress}) - {group.Count()} task(s)");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Failed sending email to {emailAddress} ({userName}): {ex.Message}");
                    }
                }
            }
            return result;
        }
    }
}
