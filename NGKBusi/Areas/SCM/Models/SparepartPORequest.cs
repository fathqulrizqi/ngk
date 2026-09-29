using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGKBusi.Areas.SCM.Models
{
    [Table("SCM_Sparepart_PO_Request_Header")]
    public class SCM_Sparepart_PO_Request_Header
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestNo { get; set; }

        [Required]
        [StringLength(50)]
        public string RequesterNIK { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public DateTime? LastApprovalDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // Draft, Pending Approval, Approved, Rejected

        [StringLength(100)]
        public string TargetDepartment { get; set; }

        [StringLength(500)]
        public string RejectReason { get; set; }

        [Required]
        public int ReminderDaysAfter { get; set; }

        [Required]
        public bool IsReminderRepeated { get; set; }

        [StringLength(50)]
        public string ReminderRepeatInterval { get; set; } // None, Daily, Weekly, Custom

        public int? ReminderCustomDays { get; set; }

        public DateTime? LastReminderSentDate { get; set; }

        [Required]
        public int ReminderSendTime { get; set; }

        public DateTime? ReminderDateStart { get; set; }

        [StringLength(100)]
        public string PONumber { get; set; }

        [StringLength(50)]
        public string Category { get; set; } // Regular, Project

        public virtual ICollection<SCM_Sparepart_PO_Request_Detail> Details { get; set; }
        public virtual ICollection<SCM_Sparepart_PO_Request_Attachment> Attachments { get; set; }
        public virtual ICollection<SCM_Sparepart_PO_Approval_Log> ApprovalLogs { get; set; }
    }

    [Table("SCM_Sparepart_PO_Request_Detail")]
    public class SCM_Sparepart_PO_Request_Detail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int HeaderID { get; set; }

        public int? No { get; set; }

        [StringLength(255)]
        public string PartName { get; set; }

        [StringLength(255)]
        public string MachineNumber { get; set; }

        [StringLength(255)]
        public string AxItemID { get; set; }

        [StringLength(255)]
        public string Maker { get; set; }

        public decimal? Quantity { get; set; }

        [StringLength(255)]
        public string Dimensi { get; set; }

        [StringLength(255)]
        public string Machine { get; set; }

        [StringLength(255)]
        public string DrawingNo { get; set; }

        [StringLength(255)]
        public string DrawingNoKanrikogu { get; set; }

        [StringLength(255)]
        public string LeadTime { get; set; }

        public DateTime? ETA { get; set; }

        [ForeignKey("HeaderID")]
        public virtual SCM_Sparepart_PO_Request_Header Header { get; set; }
    }

    [Table("SCM_Sparepart_PO_Request_Attachment")]
    public class SCM_Sparepart_PO_Request_Attachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int HeaderID { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        [Required]
        public DateTime UploadedDate { get; set; }

        [ForeignKey("HeaderID")]
        public virtual SCM_Sparepart_PO_Request_Header Header { get; set; }
    }

    [Table("SCM_Sparepart_PO_Request_Approval_Config")]
    public class SCM_Sparepart_PO_Request_Approval_Config
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public string NIK { get; set; }

        [Required]
        public int Level { get; set; }

        [Required]
        public bool IsApprover { get; set; }

        [Required]
        [StringLength(100)]
        public string Department { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public bool StartsReminder { get; set; }

        [Required]
        public bool IsOptional { get; set; }

        [Required]
        public bool TriggersInfoNotification { get; set; }

        [StringLength(100)]
        public string TaskTitle { get; set; }
    }

    [Table("SCM_Sparepart_PO_Approval_Log")]
    public class SCM_Sparepart_PO_Approval_Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public int HeaderID { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestNo { get; set; }

        [Required]
        public int Level { get; set; }

        [StringLength(100)]
        public string TaskTitle { get; set; }

        [Required]
        [StringLength(50)]
        public string NIK { get; set; }

        [StringLength(100)]
        public string ApproverName { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } // Submitted, Approved, Rejected, Skipped

        [StringLength(500)]
        public string Comment { get; set; }

        [Required]
        public DateTime ActionDate { get; set; }

        [ForeignKey("HeaderID")]
        public virtual SCM_Sparepart_PO_Request_Header Header { get; set; }
    }

    public class SCM_Sparepart_PO_Approval_Flow_Item
    {
        public int Level { get; set; }
        public string ApproverName { get; set; }
        public string NIK { get; set; }
        public string Status { get; set; }
        public DateTime? Date { get; set; }
        public bool IsOptional { get; set; }
        public string TaskTitle { get; set; }
        public string Comment { get; set; }
        public string Action { get; set; }
    }
}
