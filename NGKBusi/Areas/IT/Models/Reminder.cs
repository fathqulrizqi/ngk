using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.IT.Models
{
    public class Reminder
    {
    }

    public class IT_Reminder
    {
        [Key]
        public int ID { get; set; }
        public string ReminderTitle { get; set; }
        public string Module { get; set; }
        public int ModuleReferalID { get; set; }
        public string ReferalDocumentNo { get; set; }
        public string Type { get; set; }
        public string Thirdparty { get; set; }
        public string Attachment { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public int IsRepeatReminder { get; set; }
        public int IntervalRepeatReminderNumber { get; set; }
        public string IntervalRepetReminderType { get; set; }
        public int NotifStart { get; set; }
        public string NotifTime { get; set; }
        public byte IsRepeatNotif { get; set; }
        public int IntervalRepeatNotifNumber { get; set; }
        public string IntervalRepeatNotifType { get; set; }
        public byte StopIfDueDate { get; set; }
        public byte IsActive { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public string SendToUser { get; set; }
    }
    public class IT_Reminder_User
    {
        [Key]
        public int ID { get; set; }
        public int ReminderID { get; set; }
        public string SendToUser { get; set; }
        public string SendToUserEmail { get; set; }
        public byte IsActive { get; set; }


    }

    public class IT_Reminder_Task
    {
        [Key]
        public int ID { get; set; }
        public int ReminderID { get; set; }
        public int ReminderTime { get; set; }
        public DateTime ReminderDate { get; set; }
        public DateTime ReminderDueDate { get; set; }
        public byte IsSend { get; set; }
    }
    public class Tbl_IT_Reminder_Task
    {
        public string Module { get; set; }
        public string ReminderTitle { get; set; }
        public string Description { get; set; }
        public string Thirdparty { get; set; }  
        public string ReminderDueDate { get; set; }
        public string[] SendToUser { get; set; }

    }

    public class Tbl_IT_Reminder
    {
        public int No { get; set; }
        public string ReminderTitle { get; set; }
        public string Module { get; set; }
        public string Type { get; set; }
        public string Thirdparty { get; set; }
        public string Attachment { get; set; }
        public string Description { get; set; }
        public string DueDate { get; set; }
        public int IsRepeatReminder { get; set; }
        public int IntervalRepeatReminderNumber { get; set; }
        public string IntervalRepetReminderType { get; set; }
        public int NotifStart { get; set; }
        public string NotifTime { get; set; }
        public byte IsRepeatNotif { get; set; }
        public int IntervalRepeatNotifNumber { get; set; }
        public string IntervalRepeatNotifType { get; set; }
        public byte StopIfDueDate { get; set; }
        public byte IsActive { get; set; }
        public string CreateBy { get; set; }
        public string CreateTime { get; set; }
        public string NextNotif { get; set; }
        public string ActionButton { get; set; }
    }
    public class Tbl_Event
    {
        public int id { get; set; }
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string display { get; set; }
        public string module { get; set; }
        public string Thirdparty { get; set; }
        public int IsActive { get; set; }
    }
    public class ListEmail
    {
        public string Email { get; set; }
    }
    public class ReminderConnection : DbContext
    {
        public DbSet<IT_Reminder> IT_Reminder { get; set; }
        public DbSet<IT_Reminder_Task> IT_Reminder_Task { get; set; }
        public DbSet<IT_Reminder_User> IT_Reminder_User { get; set; }
        public ReminderConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}