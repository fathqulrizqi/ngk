using NGKBusi.Areas.Purchasing.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.QCC.Models
{
    public class QCC
    {
    }
    public class QCC_List
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string Period_FY { get; set; }
        public string Group { get; set; }
        public int? Time_Duration { get; set; }
        public string Category { get; set; }
        public string Losses { get; set; }
        public string Type { get; set; }
        public string Theme { get; set; }
        public string Background { get; set; }
        public string Current_Condition { get; set; }
        public string Improvement_Target { get; set; }
        [ForeignKey("UserCreator")]
        public string Created_By { get; set; }
        public DateTime? Created_At { get; set; }
        [ForeignKey("UserFacilitator")]
        public string Facilitator { get; set; }
        [ForeignKey("UserAdvisor1")]
        public string Advisor1 { get; set; }
        [ForeignKey("UserAdvisor2")]
        public string Advisor2 { get; set; }
        [ForeignKey("UserLeader")]
        public string Leader { get; set; }

        public int Approval { get; set; }

        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }

        public virtual Users UserCreator { get; set; }
        public virtual Users UserFacilitator { get; set; }
        public virtual Users UserAdvisor1 { get; set; }
        public virtual Users UserAdvisor2 { get; set; }
        public virtual Users UserLeader { get; set; }
        [ForeignKey("List_ID")]
        public ICollection<QCC_Progress> QCCProgress { get; set; }
    }
    public class QCC_List_Member
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("QCCList")]
        public int List_ID { get; set; }
        [ForeignKey("UserMember")]
        public string Member { get; set; }
        public string Section { get; set; }
        public virtual Users UserMember { get; set; }
        public virtual QCC_List QCCList { get; set; }
    }
    public class QCC_Progress
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("QCCList")]
        public int List_ID { get; set; }
        public int Step { get; set; }
        public string Note { get; set; }
        public DateTime? Created_At { get; set; }
        public virtual QCC_List QCCList { get; set; }
    }
    public class QCC_Progress_Files
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("QCCList")]
        public int List_ID { get; set; }
        public int Step { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
        public virtual QCC_List QCCList { get; set; }
    }
    public class QCCConnection : DbContext
    {
        public DbSet<QCC_List> QCC_List { get; set; }
        public DbSet<QCC_List_Member> QCC_List_Member { get; set; }
        public DbSet<QCC_Progress> QCC_Progress { get; set; }
        public DbSet<QCC_Progress_Files> QCC_Progress_Files { get; set; }
        public QCCConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}