using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Web.Mvc;

namespace NGKBusi.Areas.FA.Models
{

    public class TaxDocumentation
    {

    }
    public class FA_TaxDocumentation_Master_CorrectionList
    {
        [Key]
        public int CorrectionID { get; set; }
        public string CorrectionListName { get; set; }
        public byte HaveChild { get; set; }
        public int MotherID { get; set; }
    }
    public class FA_TaxDocumentation_Master_FileList
    {
        [Key]
        public int ID { get; set; }
        public string FileName { get; set; }
        public string DocumentType { get; set; }
        public byte IsActive { get; set; }
        public string Tooltip { get; set; }
    }

    public class FA_TaxDocumentation_Master_Approval
    {
        [Key]
        public int ID { get; set; }
        public string ApprovalNIK { get; set; }
        public int ApprovalLevel { get; set; }
        public byte IsActive { get; set; }
    }
    public class FA_TaxDocumentaion_Header
    {
        [Key]
        public int HeaderID { get; set; }
        public string DocumentationTitle { get; set; }
        public string Type { get; set; }
        public DateTime? TaxAuditDate { get; set; }
        public DateTime? FinalDiscussionDate { get; set; }
        public decimal TotalClaim { get; set; }
        public decimal TotalBeforeSPHP { get; set; }
        public decimal TotalSPHP { get; set; }
        public decimal TotalResult { get; set; }
        public decimal RefundBeforeSPHP { get; set; }
        public decimal RefundSPHP { get; set; }
        public decimal RefundResult { get; set; }
        public string Status { get; set; }
        public string RejectNotes { get; set; }
        public byte IsDelete { get; set; }
        public byte IsApproveBeforeSPHP { get; set; }
        public byte IsApproveSPHP { get; set; }
        public byte IsApproveResult { get; set; }
        public int TaskID { get; set; }

    }
    public class Tbl_FA_TaxDocumentation_Files
    {
        [Key]
        public int ID { get; set; }
        public int HeaderID { get; set; }
        public string FileName { get; set; }
        public string DocumentType { get; set; }
        public string FileLocation { get; set; }
        public byte IsEnable { get; set; }
        public int FileID { get; set; }
        public string Tooltip { get; set; }

    }

    public class FA_TaxDocumentation_Files
    {
        [Key]
        public int ID { get; set; }
        public int HeaderID { get; set; }
        public string FileName { get; set; }
        public string DocumentType { get; set; }
        public string FileLocation { get; set; }
        public byte IsEnable { get; set; }
        public int FileID { get; set; }

    }
    public class V_Users_Active
    {
        [Key]
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string DivisionID { get; set; }
        public string DivisionName { get; set; }
        public string DeptID { get; set; }
        public string DeptName { get; set; }
        public string SectionID { get; set; }
        public string SectionName { get; set; }
        public string SubSectionID { get; set; }
        public string SubSectionName { get; set; }
        public string CostID { get; set; }
        public string CostName { get; set; }
        public string TitleID { get; set; }
        public string TitleName { get; set; }
        public string PositionID { get; set; }
        public string PositionName { get; set; }
        public string Status { get; set; }
        public DateTime? Birthday { get; set; }
    }
    public class V_FA_TaxDocumentation
    {
        [Key]
        public int HeaderID { get; set; }
        public string DocumentationTitle { get; set; }
        public string Type { get; set; }
        public DateTime? TaxAuditDate { get; set; }
        public DateTime? FinalDiscussionDate { get; set; }
        public decimal TotalClaim { get; set; }
        public decimal TotalBeforeSPHP { get; set; }
        public decimal TotalSPHP { get; set; }
        public decimal  TotalResult { get; set; }
        public string Status { get; set; }
        public string RejectNotes { get; set; }
        public byte IsApproveBeforeSPHP { get; set; }
        public byte IsApproveSPHP { get; set; }
        public byte IsApproveResult { get; set; }
        public int TaskID { get; set; }
    }
    public class V_FA_TaxDocumentation_Files
    {
        [Key]
        public int ID { get; set; }
        public int HeaderID { get; set; }
        public string FileName { get; set; }
        public string DocumentType { get; set; }
        public string FileLocation { get; set; }
        public byte IsEnable { get; set; }
        public int FileID { get; set; }
        public string Tooltip { get; set; }
    }
    public class Tbl_FA_TaxDocumentation
    {
        [Key]
        public int HeaderID { get; set; }
        public string DocumentationTitle { get; set; }
        public string Type { get; set; }
        public string TaxAuditDate { get; set; }
        public string FinalDiscussionDate { get; set; }
        public string TotalClaim { get; set; }
        public string BeforeSPHP { get; set; }
        public string SPHP { get; set; }
        public string Received { get; set; }
        public string Status { get; set; }
        public string Button { get; set; }
    }

    public class FA_TaxDocumentation_CorrectionList
    {
        [Key]
        public int ID { get; set; }
        public int HeaderID { get; set; }
        public string CorrectionList { get; set; }
        public string motherID { get; set; }
        public byte HaveChild { get; set; }
        public decimal BeforeSPHP { get; set; }
        public decimal SPHP { get; set; }
        public decimal Result { get; set; }
        public DateTime? BeforeSPHP_CreateTime { get; set; }
        public DateTime? SPHP_CreateTime { get; set; }
        public DateTime? Result_CreateTime { get; set; }
        public byte BeforeSPHP_Status { get; set; }
        public byte SPHP_Status { get; set; }
        public byte Result_Status { get; set; }
    }
    public class Tbl_TaxDocumentation_CorrectionList
    {
        public int ID { get; set; }
        public int HeaderID { get; set; }
        public string CorrectionList { get; set; }
        public string motherID { get; set; }
        public byte HaveChild { get; set; }
        public decimal BeforeSPHP { get; set; }
        public decimal SPHP { get; set; }
        public decimal Result { get; set; }
        public DateTime? BeforeSPHP_CreateTime { get; set; }
        public DateTime? SPHP_CreateTime { get; set; }
        public DateTime? Result_CreateTime { get; set; }
        public byte BeforeSPHP_Status { get; set; }
        public byte SPHP_Status { get; set; }
        public byte Result_Status { get; set; }
    }
    public class TaxDocumentationConnection : DbContext
    {
        public DbSet<V_Users_Active> V_Users_Active { get; set; }
        public DbSet<FA_TaxDocumentaion_Header> FA_TaxDocumentaion_Header { get; set; }
        public DbSet<FA_TaxDocumentation_Files> FA_TaxDocumentation_Files { get; set; }
        public DbSet<V_FA_TaxDocumentation> V_FA_TaxDocumentation { get; set; }
        public DbSet<V_FA_TaxDocumentation_Files> V_FA_TaxDocumentation_Files { get; set; }
        public DbSet<FA_TaxDocumentation_CorrectionList> FA_TaxDocumentation_CorrectionList { get; set; }
        public DbSet<FA_TaxDocumentation_Master_FileList> FA_TaxDocumentation_Master_FileList { get; set; }
        public DbSet<FA_TaxDocumentation_Master_Approval> FA_TaxDocumentation_Master_Approval { get; set; }
        public DbSet<FA_TaxDocumentation_Master_CorrectionList> FA_TaxDocumentation_Master_CorrectionList { get; set; }
        public TaxDocumentationConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }

}