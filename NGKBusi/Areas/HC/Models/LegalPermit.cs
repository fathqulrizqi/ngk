using NGKBusi.Areas.WebService.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HC.Models
{
    public class LegalPermit
    {
    }

    public class HC_LegalPermit_Flow
    {
        [Key]
        public int ID { get; set; }
        public string FlowName { get; set; }
        public string IsActive { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
    }
    public class HC_LegalPermit_Flow_Detail
    {
        [Key]
        public int ID { get; set; }
        public string Flow_ID { get; set; }
        public string StepName { get; set; }
        public int StepNumber { get; set; }
        public string Description { get; set; }
        public string PIC { get; set; }
        public int Estimation_Time { get; set; }
        public string Requirement_Document { get; set; }
        public byte IsDelete { get; set; }
    }

    public class HC_LegalPermit_Request
    {
        public int ID { get; set; }
        public string RefQuotationNo { get; set; }
        public string SupplierName { get; set; }
        public string PresidentDirector { get; set; }
        public string Address { get; set; }
        public string TelpNo { get; set; }
        public string FaxNo { get; set; }
        public string ProjectName { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalPrice { get; set; }
        public string TermPayment { get; set; }
        public int LeadTime { get; set; }
        public int Penalty { get; set; }
        public string BankAccountDetail { get; set; }
        public string Warranty { get; set; }
        public DateTime PlanStartDate { get; set; }
        public DateTime PlanEndDate { get; set; }
        public string LegalDocument { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class HC_LegalPermit_Request_BreakdownCost
    {
        [Key]
        public int ID { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public int ReqID { get; set; }
    }
    public class HC_LegalPermit_Request_Progress
    {
        [Key]
        public int ID { get; set; }
        public int ReqID { get; set; }
        public int StepID { get; set; }
        public int StepNumber { get; set; }
        public string StepName { get; set; }
        public string PIC { get; set; }
        public int Estimation_Time { get; set; }
        public byte Status { get; set; }
    }
    public class HC_LegalPermit_Recap_Agreement
    {
        [Key]
        public int ID { get; set; }
        public string AgreementName { get; set; }
        public string SecondParty { get; set; }
        public string AgreementNo { get; set; }
        public DateTime PeriodeStart { get; set; }
        public DateTime PeriodeEnd { get; set; }
        public string Attachment { get; set; }
        public string AgreementType { get; set; }
        public string Note { get; set; }
        public int ReminderID { get; set; }
        public int? PrevAgreementID { get; set; }
        public string PrevAgreementNo { get; set; }
        public byte IsRenewal { get; set; }
        public int? RenewalRefID { get; set; }
        public byte IsActive { get; set; }
        public string DocumentID { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
    public class HC_LegalPermit_Recap_Permit
    {
        [Key]
        public int ID { get; set; }
        public string Permit { get; set; }
        public string Number { get; set; }
        public string SectionHandling { get; set; }
        public string Goverment { get; set; }
        public DateTime? Expired { get; set; }
        public string Attachment { get; set; }
        public string PIC { get; set; }
        public string Status { get; set; }
        public int ReminderID { get; set; }
        public int? PrevPermitID { get; set; }
        public string PrevPermitNo { get; set; }
        public byte IsRenewal { get; set; }
        public int? RenewalRefID { get; set; }
        public string DocumentID { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
    public class HC_LegalPermit_Share_User
    {
        [Key]
        public int ID { get; set; }
        public string NIK { get; set; }
        public byte IsDelete { get; set; }
        public string CreateBy { get; set; }
        public int Legal_ID { get; set; }
        public string LegalType { get; set; }
        public DateTime CreateTime { get; set; }

    }

    public class HC_LegalPermit_DraftAgreementRequest
    {
        [Key]
        public int ID { get; set; }
        public DateTime? RequestDate { get; set; }
        public string RefQuotationNo { get; set; }
        public string SupplierName { get; set; }
        public string PresidentDirector { get; set; }
        public string Address { get; set; }
        public string TelpNo { get; set; }
        public string EmailAddress { get; set; }
        public string ProjectName { get; set; }
        [System.Web.Mvc.AllowHtml]
        public string ProjectDetails { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalPrice { get; set; }
        public string TermOfPayment { get; set; }
        public string BankAccount { get; set; }
        public string LeadTimeWorks { get; set; }
        public string PenaltyClause { get; set; }
        public string Warranty { get; set; }
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanEndDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; }
        public int CurrApprovalLevel { get; set; }
        public int ReturnToLevel { get; set; }
        public string AgreementFilePath { get; set; }
        public byte IsDeleted { get; set; }
        public DateTime? LastReminderDate { get; set; } 
        [NotMapped]
        public List<string> Reviewers { get; set; }
    }

    public class HC_LegalPermit_DraftAgreementRequest_Reviewer
    {
        [Key]
        public int ID { get; set; }
        public int DraftingReqID { get; set; } 
        public string SendToNIK { get; set; }
        public string SendToEmail { get; set; }
    }

    public class HC_LegalPermit_UserAdmin
    {
        [Key]
        public int ID { get; set; }
        public string UserNIK { get; set; } // Relasi ke DraftAgreement
        public int UserLevel { get; set; }
        public byte IsDeleted { get; set; }
    }

    public class HC_LegalPermit_DraftAgreementRequest_Comment
    {
        [Key]
        public int ID { get; set; }

        public int DraftingRequestId { get; set; } // Relasi ke Parent Request

        public int? ParentId { get; set; } // Self-Join untuk Reply

        [Required]
        public string Content { get; set; }
        [Required]
        public string Source { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string CreatedBy { get; set; } // Username (misal: domain\budi)

        [StringLength(200)]
        public string FullName { get; set; }  // Nama Lengkap (misal: Budi Santoso)

        public bool CreatedByAdmin { get; set; }

        public int UpvoteCount { get; set; } = 0;

        // --- NAVIGATION PROPERTIES (Wajib 'virtual' di MVC 5 / EF6) ---

        // 1. Relasi ke Attachment (Satu komentar punya banyak file)
        // Hapus property 'string Attachments', ganti dengan ini:
        public virtual ICollection<HC_LegalPermit_DraftAgreementRequest_Comment_Attachment> Attachments { get; set; }

        // 2. Relasi ke Parent Comment (Untuk akses data parent reply)
        [ForeignKey("ParentId")]
        public virtual HC_LegalPermit_DraftAgreementRequest_Comment ParentComment { get; set; }
    }

    public class HC_LegalPermit_DraftAgreementRequest_Comment_Attachment
    {
        [Key]
        public int ID { get; set; }

        public int CommentId { get; set; } // Foreign Key ke tabel Comment

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } // Nama asli file (contoh: foto.png)

        [Required]
        public string FileUrl { get; set; }  // Path server (contoh: /uploads/comments/123_foto.png)

        [StringLength(200)]
        public string MimeType { get; set; } // Tipe file (image/png, application/pdf)

        // --- NAVIGATION PROPERTY ---
        [ForeignKey("CommentId")]
        public virtual HC_LegalPermit_DraftAgreementRequest_Comment Comment { get; set; }
    }

    public class HC_LegalPermit_ReviewAgreementRequest
    {
        [Key]
        public int ID { get; set; }
        public string ProjectName { get; set; }
        public string CompanyName { get; set; }
        public string ReviewDocument { get; set; }
        public string OriginalReviewDocumentName { get; set; }
        public string QuotationDocumentName { get; set; }
        public string SignerInfoDocumentName { get; set; }
        public string AfterReviewDocument { get; set; }
        public string OriginalAfterReviewDocument { get; set; }
        public string Status { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
        public DateTime? ReviewedTime { get; set; }        
        public string ReviewedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }        
        public string ApprovedBy { get; set; }
    }

    [Table("HC_LegalPermit_Notification")]
    public class HC_LegalPermit_Notification
    {
        [Key]
        public int ID { get; set; }
        public string RecipientNIK { get; set; } // Siapa yang terima notif
        public string senderNIK { get; set; }   // Siapa yang memicu (yg komen)
        public string Message { get; set; }      // Isi pesan singkat
        public string LinkUrl { get; set; }      // Link ke halaman detail
        public byte IsRead { get; set; }         // Status sudah dilihat/belum
        public DateTime CreatedDate { get; set; }
    }

    public class Tbl_HC_LegalPermit_Request
    {
        public string RequestNo { get; set; }
        public string RequestDate { get; set; }
        public string RefQuotationNo { get; set; }
        public string SupplierName { get; set; }
        public string PresidentDirector { get; set; }
        public string Address { get; set; }
        public string TelpNo { get; set; }
        public string FaxNo { get; set; }
        public string ProjectName { get; set; }
        public decimal Price { get; set; }
        public string Discount { get; set; }
        public string FinalPrice { get; set; }
        public string TermPayment { get; set; }
        public string LeadTime { get; set; }
        public string Penalty { get; set; }
        public string BankAccountDetail { get; set; }
        public string Warranty { get; set; }
        public string PlanStartDate { get; set; }
        public string PlanEndDate { get; set; }
        public string BtnEdit { get; set; }
    }

    public class Tbl_HC_LegalPermit_Flow_Detail
    {
        public int No { get; set; }
        public string PIC { get; set; }
        public string StepName { get; set; }
        public string StepNumber { get; set; }
        public string Description { get; set; }
        public string EstimationTime { get; set; }
        public string Button { get; set; }
        public string Requirement_Document { get; set; }
    }
    public class Tbl_HC_LegalPermit_Recap_Agreement
    {
        public string ID { get; set; }
        public int No { get; set; }
        public string Name { get; set; }
        public string SecondParty { get; set; }
        public string AgreementNo { get; set; }
        public string PeriodeStart { get; set; }
        public string PeriodeEnd { get; set; }
        public string Attachment { get; set; }
        public string AgreementType { get; set; }
        public string Note { get; set; }
        public string btnAlert { get; set; }
        public string DocumentID { get; set; }
    }
    public class Tbl_HC_LegalPermit_Recap_Permit
    {
        public int No { get; set; }
        public string ID { get; set; }
        public string Permit { get; set; }
        public string Number { get; set; }
        public string SectionHandling { get; set; }
        public string Goverment { get; set; }
        public string Expired { get; set; }
        public string PIC { get; set; }
        public string Attachment { get; set; }
        public string Status { get; set; }
        public string btnAlert { get; set; }
        public string DocumentID { get; set; }
    }
    public class Tbl_HC_LegalPermit_Share_User
    {
        public int ID { get; set; }
        public int No { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Section { get; set; }
        public byte IsDelete { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public string Action { get; set; }
    }

    // ViewModel untuk menangkap data dari Plugin Viima
    public class CommentViewModel
    {
        public int? id { get; set; }
        public int? parent { get; set; }
        public string content { get; set; }

        // Pastikan namanya 'attachments' (huruf kecil) agar cocok dengan JSON Viima
        public List<AttachmentViewModel> attachments { get; set; }
    }

    public class AttachmentViewModel
    {
        public int? id { get; set; }

        // Gunakan huruf kecil agar binding JSON 100% akurat
        public string url { get; set; }
        public string name { get; set; }
        public string mime_type { get; set; }
    }

    public class Tbl_LegalPermit_ReviewAgreementRequest
    {
        [Key]
        public int ID { get; set; }
        public string ProjectName { get; set; }
        public string CompanyName { get; set; }
        public string ReviewDocument { get; set; }
        public string OriginalReviewDocumentName { get; set; }
        public string QuotationDocumentName { get; set; }
        public string SignerInfoDocumentName { get; set; }
        public string AfterReviewDocument { get; set; }
        public string OriginalAfterReviewDocument { get; set; }
        public string Status { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
        public DateTime? ReviewedTime { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public string ApprovedBy { get; set; }
        public string CreatorName { get; set; }
    }

    public class ApprovalTimelineViewModel
    {
        // Dari Approval_List
        public string Title { get; set; }
        public string Dept_Name { get; set; }
        public string User_Name { get; set; }
        public int Levels { get; set; }
        public string Status { get; set; }
        public bool IsCurrent { get; set; }

        // Dari Approval_History
        public DateTime? ActionDate { get; set; } // Pengganti ApprovalDate
        public string Comments { get; set; }
    }

    public class LegalPermitConnection : DbContext
    {
        public DbSet<HC_LegalPermit_Flow> HC_LegalPermit_Flow { get; set; }
        public DbSet<HC_LegalPermit_Flow_Detail> HC_LegalPermit_Flow_Detail { get; set; }
        public DbSet<HC_LegalPermit_Request> HC_LegalPermit_Request { get; set; }
        public DbSet<HC_LegalPermit_Request_BreakdownCost> HC_LegalPermit_Request_BreakdownCost { get; set; }
        public DbSet<HC_LegalPermit_Request_Progress> HC_LegalPermit_Request_Progress { get; set; }
        public DbSet<HC_LegalPermit_Recap_Agreement> HC_LegalPermit_Recap_Agreement { get; set; }
        public DbSet<HC_LegalPermit_Recap_Permit> HC_LegalPermit_Recap_Permit { get; set; }
        public DbSet<HC_LegalPermit_DraftAgreementRequest> HC_LegalPermit_DraftAgreementRequest { get; set; }
        public DbSet<HC_LegalPermit_DraftAgreementRequest_Reviewer> HC_LegalPermit_DraftAgreementRequest_Reviewer { get; set; }
        public DbSet<HC_LegalPermit_DraftAgreementRequest_Comment> HC_LegalPermit_DraftAgreementRequest_Comment { get; set; }
        public DbSet<HC_LegalPermit_DraftAgreementRequest_Comment_Attachment> HC_LegalPermit_DraftAgreementRequest_Comment_Attachment { get; set; }
        public DbSet<HC_LegalPermit_ReviewAgreementRequest> HC_LegalPermit_ReviewAgreementRequest { get; set; }
        public DbSet<HC_LegalPermit_UserAdmin> HC_LegalPermit_UserAdmin { get; set; }        
        public IDbSet<HC_LegalPermit_Share_User> HC_LegalPermit_Share_User { get; set; }
        public DbSet<HC_LegalPermit_Notification> HC_LegalPermit_Notification { get; set; }
        public LegalPermitConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}