using System;
using NGKBusi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.Purchasing.Models
{
    public class PurchaseRequest
    {
    }
    public class Purchasing_PurchaseRequest_Header
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [Key]
        [ForeignKey("Lines")]
        public string ReqNumber { get; set; }
        public string Section { get; set; }
        public DateTime? Due_Date { get; set; }
        public string Description { get; set; }
        public DateTime Created_At { get; set; }
        [ForeignKey("Creator")]
        public string Created_By { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public ICollection<Purchasing_PurchaseRequest_Line> Lines { get; set; }
        public virtual Users Creator { get; set; }
    }

    public class Purchasing_PurchaseRequest_Line
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("Headers")]
        public string ReqNumber { get; set; }
        public string QuoNumber { get; set; }
        public string Budget_Number { get; set; }
        public string Section_To { get; set; }
        public string Item_Group { get; set; }
        public string Item_ID { get; set; }
        public string Item_Name { get; set; }
        public string Unit { get; set; }
        public string Note { get; set; }
        public double? Qty { get; set; }
        public double? Price_Estimation { get; set; }
        public DateTime? Quotation_At { get; set; }
        [ForeignKey("Creator")]
        public string Quotation_By { get; set; }
        public string Assign_To { get; set; }
        public virtual Purchasing_PurchaseRequest_Header Headers { get; set; }
        public virtual Users Creator { get; set; }
    }
    public class Purchasing_PurchaseRequest_Attachment
    {
        [Key]
        public int ID { get; set; }
        public string ReqNumber { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
    }
    public class Purchasing_PurchaseRequest_Quotation_Attachment
    {
        [Key]
        public int ID { get; set; }
        public string QuoNumber { get; set; }
        public string Third_Party_Name { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
    }

    public class Purchasing_PurchaseRequest_Quotation_Header
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [Key]
        [ForeignKey("Lines")]
        public string QuoNumber { get; set; }
        public string Category { get; set; }
        public string PONo { get; set; }
        public string Section { get; set; }
        public DateTime? Due_Date { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public string Basis_Award { get; set; }
        public string Currency { get; set; }
        public DateTime Created_At { get; set; }
        [ForeignKey("Creator")]
        public string Created_By { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public ICollection<Purchasing_PurchaseRequest_Quotation_Line> Lines { get; set; }
        public virtual Users Creator { get; set; }
    }

    public class Purchasing_PurchaseRequest_Quotation_Line
    {
        [Key]
        [ForeignKey("Vendors")]
        public int ID { get; set; }
        public int? Sequence { get; set; }
        [ForeignKey("Headers")]
        public string QuoNumber { get; set; }
        public string ReqNumber { get; set; }
        public DateTime ETA_Date { get; set; }
        public string Budget_Number { get; set; }
        public string Section_To { get; set; }
        public string Item_Group { get; set; }
        public string Item_ID { get; set; }
        public string Item_Name { get; set; }
        public string Unit { get; set; }
        public string Note { get; set; }
        public double? Qty { get; set; }
        public double? Price_Estimation { get; set; }
        public bool? Is_Reject { get; set; }
        public virtual Purchasing_PurchaseRequest_Quotation_Header Headers { get; set; }
        public ICollection<Purchasing_PurchaseRequest_Quotation_Line_Vendor> Vendors { get; set; }
    }
    public class Purchasing_PurchaseRequest_Quotation_Line_Vendor
    {
        [Key]
        public int ID { get; set; }
        public string QuoNumber { get; set; }
        [ForeignKey("Line")]
        public int Line_ID { get; set; }
        public string Third_Party_ID { get; set; }
        public string Third_Party_Name { get; set; }
        public double? Discount { get; set; }
        public string Discount_Type { get; set; }
        public double? Price { get; set; }
        public double? Qty { get; set; }
        public double? Total { get; set; }
        public Boolean IsChoosen { get; set; }
        public virtual Purchasing_PurchaseRequest_Quotation_Line Line { get; set; }
    }
    public class Purchasing_PurchaseRequest_Quotation_History_Price_Custom
    {
        [Key]
        public int ID { get; set; }
        public string Item_ID { get; set; }
        public string Item_Name { get; set; }
        public DateTime Price_Date { get; set; }
        public double? Price { get; set; }
    }

    public class Purchasing_PurchaseRequest_WFL_Header
    {
        [Key]
        public int ID { get; set; }
        public string WFL_Name { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
    }
    public class Purchasing_PurchaseRequest_WFL_Line
    {
        [Key]
        public int ID { get; set; }
        public int Header_ID { get; set; }
        public int PR_Line_ID { get; set; }
        public string Item_ID { get; set; }
        public string Item_Name { get; set; }
        public string Referal_Name { get; set; }
    }

    public class V_Budget_Usage_CutOff
    {
        [Key]
        public string Budget_Number { get; set; }
        public string Budget_Desc { get; set; }
        public Int64 Total_Amount { get; set; }
        public double Usage { get; set; }
        public string Period_FY { get; set; }
    }
    public class Purchasing_PurchaseRequest_Quotation_Comments
    {
        [Key]
        public int comment_id { get; set; }
        public string id { get; set; }
        public string QuoNumber { get; set; }
        [ForeignKey("Users")]
        public string nik { get; set; }
        public string parent { get; set; }
        public string created { get; set; }
        public long? modified { get; set; }
        public string content { get; set; }
        //public string attachments { get; set; }
        //public string pings { get; set; }
        //public string creator { get; set; }
        public string fullname { get; set; }
        public bool? isNew { get; set; }
        public bool? createdByAdmin { get; set; }
        public bool? created_by_current_user { get; set; }
        public int? upvoteCount { get; set; }
        public bool? userHasUpvoted { get; set; }
        public virtual Users Users { get; set; }
        [ForeignKey("Comment_ID")]
        public ICollection<Purchasing_PurchaseRequest_Quotation_Comments_Attachment> attachments { get; set; }
    }
    public class Purchasing_PurchaseRequest_Quotation_Comments_Attachment
    {
        [Key]
        public int id { get; set; }
        public int? Comment_ID { get; set; }
        public string file { get; set; }
        public string mime_type { get; set; }
    }


        public class PurchaseRequestConnection : DbContext
    {
        public DbSet<Purchasing_PurchaseRequest_Header> Purchasing_PurchaseRequest_Header { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Line> Purchasing_PurchaseRequest_Line { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Attachment> Purchasing_PurchaseRequest_Attachment { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Quotation_Attachment> Purchasing_PurchaseRequest_Quotation_Attachment { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Quotation_Header> Purchasing_PurchaseRequest_Quotation_Header { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Quotation_Line> Purchasing_PurchaseRequest_Quotation_Line { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Quotation_Line_Vendor> Purchasing_PurchaseRequest_Quotation_Line_Vendor { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Quotation_History_Price_Custom> Purchasing_PurchaseRequest_Quotation_History_Price_Custom { get; set; }
        public DbSet<Purchasing_PurchaseRequest_WFL_Header> Purchasing_PurchaseRequest_WFL_Header { get; set; }
        public DbSet<Purchasing_PurchaseRequest_WFL_Line> Purchasing_PurchaseRequest_WFL_Line { get; set; }
        public DbSet<V_Budget_Usage_CutOff> V_Budget_Usage_CutOff { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Quotation_Comments> Purchasing_PurchaseRequest_Quotation_Comments { get; set; }
        public DbSet<Purchasing_PurchaseRequest_Quotation_Comments_Attachment> Purchasing_PurchaseRequest_Quotation_Comments_Attachment { get; set; }

        public PurchaseRequestConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}