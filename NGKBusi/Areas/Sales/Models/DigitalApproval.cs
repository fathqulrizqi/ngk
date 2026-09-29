using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.Sales.Models
{
    public class Sales_DigitalApproval_List
    {
        [Key]
        public int ID { get; set; }
        public string ReqNumber { get; set; }
        public string Section { get; set; }
        public string Description { get; set; }
        public DateTime Created_At { get; set; }
        [ForeignKey("Creator")]
        public string Created_By { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public virtual Users Creator { get; set; }
    }

    public class Sales_DigitalApproval_List_Attachment
    {
        [Key]
        public int ID { get; set; }
        public string ReqNumber { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
    }

    public class Sales_DigitalApproval_List_Comments
    {
        [Key]
        public int comment_id { get; set; }
        public string id { get; set; }
        public string ReqNumber { get; set; }
        [ForeignKey("Users")]
        public string nik { get; set; }
        public string parent { get; set; }
        public string created { get; set; }
        public long? modified { get; set; }
        public string content { get; set; }
        public string fullname { get; set; }
        public bool? isNew { get; set; }
        public bool? createdByAdmin { get; set; }
        public bool? created_by_current_user { get; set; }
        public int? upvoteCount { get; set; }
        public bool? userHasUpvoted { get; set; }
        public virtual Users Users { get; set; }
        [ForeignKey("Comment_ID")]
        public ICollection<Sales_DigitalApproval_List_Comments_Attachment> attachments { get; set; }
    }
    public class Sales_DigitalApproval_List_Comments_Attachment
    {
        [Key]
        public int id { get; set; }
        public int? Comment_ID { get; set; }
        public string file { get; set; }
        public string mime_type { get; set; }
    }

    public class DigitalApprovalConnection : DbContext
    {
        public DbSet<Sales_DigitalApproval_List> Sales_DigitalApproval_List { get; set; }
        public DbSet<Sales_DigitalApproval_List_Attachment> Sales_DigitalApproval_List_Attachment { get; set; }
        public DbSet<Sales_DigitalApproval_List_Comments> Sales_DigitalApproval_List_Comments { get; set; }
        public DbSet<Sales_DigitalApproval_List_Comments_Attachment> Sales_DigitalApproval_List_Comments_Attachment { get; set; }
        public DigitalApprovalConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}