using NGKBusi.Areas.Purchasing.Models;
using NGKBusi.Models;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO.Packaging;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.PE.Models
{
    public class PCR
    {
    }

    public class PE_PCR_Head
    {
        [Key]
        public int ID { get; set; }

        public string Doc_No { get; set; } 
        public string Product { get; set; } 
        public string Line { get; set; }
        public string Title { get; set; } 
        public string Purpose { get; set; } 
        public string Condition { get; set; } 
        public string Applied_Part { get; set; } 
        public string Model { get; set; } 
        public string Part_No { get; set; } 
        public string BCP { get; set; } 
        public string Remark { get; set; }
        public long? Forecast { get; set; }
        public int? Sequence { get; set; }
        public string Dept { get; set; }
        public DateTime Timestamps { get; set; }

        public virtual ICollection<PE_PCR_Classification> PE_PCR_Classification { get; set; }
        public virtual ICollection<PE_PCR_DTBR> DTBRs { get; set; }
        public virtual PE_PCR_Detail Detail { get; set; } 
        public virtual ICollection<PE_PCR_Approval_Head> Approvals { get; set; }
        public virtual ICollection<PE_PCR_Files> PE_PCR_Files { get; set; }
        public virtual ICollection<PE_PCR_Department_Files> PE_PCR_Department_Files { get; set; }
        public virtual ICollection<PE_PCR_Management_Approval> PE_PCR_Management_Approval { get; set; }
        public virtual ICollection<PE_PCR_Comments> PE_PCR_Comments { get; set; }
    }

    public class PE_PCR_Classification
    {
        [Key]
        public int ID { get; set; }

        public int Head_ID { get; set; }

        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }


        public int Master_ID { get; set; }

        [ForeignKey("Master_ID")]
        public virtual PE_PCR_Classification_Master PE_PCR_Classification_Master { get; set; }


        public string Status { get; set; } 
    }

    public class PE_PCR_Classification_Master
    {
        [Key]
        public int ID { get; set; }
        public string Classification_Name { get; set; }

        public virtual ICollection<PE_PCR_Classification> PE_PCR_Classification { get; set; }

    }

    public class PE_PCR_DTBR
    {
        [Key]
        public int ID { get; set; }
        public int Head_ID { get; set; }

        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }

        public int DTBR_ID { get; set; }

        [ForeignKey("DTBR_ID")]
        public virtual PE_PCR_DTBR_Master PE_PCR_DTBR_Master { get; set; }

        public string Status { get; set; }
        public string Other_Description { get; set; }
        public string Classification { get; set; }
        public int isRequire { get; set; }
    }

    public class PE_PCR_DTBR_Master
    {
        [Key]
        public int ID { get; set; }
        public string DTBR_Name { get; set; }
        public virtual ICollection<PE_PCR_DTBR> PE_PCR_DTBR { get; set; }

    }

    public class PE_PCR_Detail
        {
        [Key, ForeignKey("PE_PCR_Head")]
        public int Head_ID { get; set; }

        public string Current_Statement { get; set; }

            [AllowHtml]
            public string Current_Sketch { get; set; }

            public string Current_Cost { get; set; }
            public string Future_Statement { get; set; }

            [AllowHtml]
            public string Future_Sketch { get; set; }

            public string Future_Cost { get; set; }
            public string Plan { get; set; }
            public string Actual { get; set; }

            public virtual PE_PCR_Head PE_PCR_Head { get; set; }
        }

    public class PE_PCR_Approval_Head
    {

        [Key]
        public int ID { get; set; }

        public int Head_ID { get; set; }
        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }

        public int Approval_ID { get; set; }
        [ForeignKey("Approval_ID")]
        public virtual PE_PCR_Approval_Status ApprovalStatus { get; set; }

        //--- Info First Issued ---
        public string First_Issued_NIK { get; set; }
        [ForeignKey("First_Issued_NIK")]
        public virtual Users FirstIssuedByUser { get; set; }
        public string First_Issued_Name { get; set; }
        public int First_Status_ID { get; set; }
        [ForeignKey("First_Status_ID")]
        public virtual PE_PCR_Sign_Master FirstStatus { get; set; }

        //--- Info Second Issued ---
        public string Second_Issued_NIK { get; set; }
        [ForeignKey("Second_Issued_NIK")]
        public virtual Users SecondIssuedByUser { get; set; }
        public string Second_Issued_Name { get; set; }
        public int Second_Status_ID { get; set; }
        [ForeignKey("Second_Status_ID")]
        public virtual PE_PCR_Sign_Master SecondStatus { get; set; }

        //--- Info Approver ---
        public string Approver_NIK { get; set; }
        [ForeignKey("Approver_NIK")]
        public virtual Users ApproverNIK { get; set; }
        public string Approver_Name { get; set; }
        public int Approver_Status_ID { get; set; }
        [ForeignKey("Approver_Status_ID")]
        public virtual PE_PCR_Sign_Master ApproverStatus { get; set; }

        //--- Info PE ---
        public string PE_NIK { get; set; }
        [ForeignKey("PE_NIK")]
        public virtual Users PE_User { get; set; }
        public string PE_Name { get; set; }
        public int PE_Status_ID { get; set; }
        [ForeignKey("PE_Status_ID")]
        public virtual PE_PCR_Sign_Master PEStatus { get; set; }

        //--- Info PE ---
        public string PE_Manager_NIK { get; set; }
        [ForeignKey("PE_NIK")]
        public virtual Users PE_Manager_User { get; set; }
        public string PE_Manager_Name { get; set; }
        public int PE_Manager_Status_ID { get; set; }
        [ForeignKey("PE_Manager_Status_ID")]
        public virtual PE_PCR_Sign_Master PEManagerStatus { get; set; }

        //--- Properti Lainnya ---
        public string Proposal_Check { get; set; }
        public string Message { get; set; }

        //--- Tanggal ---
        public DateTime? Date_Issued1 { get; set; }
        public DateTime? Date_Issued2 { get; set; }
        public DateTime? Date_Approved { get; set; }
        public DateTime? Date_PE_Approved { get; set; }
        public DateTime? Date_PE_Manager_Approved { get; set; }
    }

    public class PE_PCR_Approval_Status
    {
        public int ID { get; set; }
        public string Status_Name { get; set; }
    }

    public class PE_PCR_Files
    {
        [Key]
        public int ID { get; set; }
        public int Head_ID { get; set; }
        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }
       
        public string File_Name { get; set; }
    }

    public class PE_PCR_Department_Files
    {
        [Key]
        public int ID { get; set; }

        public int Head_ID { get; set; }
        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }
        public int Department_ID { get; set; }
        [ForeignKey("Department_ID")]
        public virtual PE_PCR_Department_Master PE_PCR_Department_Master { get; set; }

        public string File_Name { get; set; }
    }

    public class PE_PCR_ClassificationDTO
    {
        public int Master_ID { get; set; }
        public string Status { get; set; }
    }

    public class PE_PCR_DTBRDTO
    {
        public int DTBR_ID { get; set; }
        public string Status { get; set; }
        public string Classification { get; set; }
        public string Other_Description { get; set; }
    }

    public class PE_PCR_Sign_Master 
    { 
        public int ID { get; set; }
        public string Sign_Name { get; set; }
    }

    public class PEManagerInfo
    {
        public string NIK { get; set; }
        public string Name { get; set; }
    }


    // -- PHASE 2 --//

    public class PE_PCR_Department_Master
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string DbCostID { get; set; }
        public string DbCostName { get; set; }
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public virtual ICollection<PE_PCR_Department_Files> PE_PCR_Department_Files { get; set; }

    }

    [NotMapped]
    public class UserViewModel
    {
        public string NIK { get; set; }
        public string Name { get; set; }
    }
    public class PE_PCR_Department_Approval
    {
        [Key]
        public int ID { get; set; }
        public int Head_ID { get; set; }
        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }
        public int Department_ID { get; set; }
        [ForeignKey("Department_ID")]
        public virtual PE_PCR_Department_Master PE_PCR_Department_Master { get; set; }
        public string Signer_NIK { get; set; }
        public string Signer_Name { get; set; } 
        public DateTime? Signer_Date { get; set; }
        public int Status_ID { get; set; }
        [ForeignKey("Status_ID")]
        public virtual PE_PCR_Sign_Master StatusID { get; set; }
        public DateTime? Timestamps { get; set; }
        public string Comment { get; set; }
        public string Comment_Origin { get; set; }
    }

    public class ConcernDto
    {
        public int HeadId { get; set; }
        public int DepartmentId { get; set; }
        public string Signer_NIK { get; set; }
        public string Signer_Name { get; set; }
        public int StatusId { get; set; }
        public string Comment { get; set; }
    }

    public class ManagerReminderDto
    {
        public int HeadId { get; set; }
        public string TargetNIK { get; set; }
    }

    public class ConcernWrapperDto
    {
        public List<ConcernDto> Detail { get; set; }
        public int ApprovalId { get; set; } 
    }


    public class QMManagerInfo
    {
        public string NIK { get; set; }
        public string Name { get; set; }
    }

    public class PlantManagerInfo
    {
        public string NIK { get; set; }
        public string Name { get; set; }
    }
    public class GMInfo
    {
        public string NIK { get; set; }
        public string Name { get; set; }
    }


    public class PE_PCR_Management_Approval
    {
        [Key]
        public int ID { get; set; }

        public int Head_ID { get; set; }
        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }

        // --- Manager 1 ---
        public string M1_NIK { get; set; }   
        public int M1_Status { get; set; }   
        public string M1_Name { get; set; }
        public DateTime? M1_Date { get; set; }
        public string M1_Notes { get; set; }
        public bool M1_isHold { get; set; }

        // --- Manager 2 ---
        public string M2_NIK { get; set; }
        public int M2_Status { get; set; }
        public string M2_Name { get; set; }
        public DateTime? M2_Date { get; set; }
        public string M2_Notes { get; set; }
        public bool M2_isHold { get; set; }


        // --- Manager 3 ---
        public string M3_NIK { get; set; }
        public int M3_Status { get; set; }
        public string M3_Name { get; set; }
        public DateTime? M3_Date { get; set; }
        public string M3_Notes { get; set; }
        public bool M3_isHold { get; set; }

        public bool isHold { get; set; }
    }

    public class FileData
    {
        public string FileName { get; set; }
        public bool IsExisting { get; set; }
    }

    public class PE_PCR_Comments
    {
        [Key]
        public int C_ID { get; set; }
        public int Head_ID { get; set; }
        [ForeignKey("Head_ID")]
        public virtual PE_PCR_Head PE_PCR_Head { get; set; }
        public string id { get; set; }
        public string parent { get; set; }
        public string creator { get; set; }
        public string fullName { get; set; }
        public DateTime? created { get; set; }
        public long? modified { get; set; }
        public string content { get; set; }
        public string attachments { get; set; }
        public string pings { get; set; }
        //public string profile_picture_url { get; set; }
        public bool? created_by_current_user { get; set; }
        // --- Navigation Property ---
        public virtual ICollection<PE_PCR_Comment_Files> CommentFiles { get; set; }
    }

    public class V_PE_PCR_Summary
    {
        [Key]
        public int Head_ID { get; set; }
        public string Document_No { get; set; }
        public string Title { get; set; }
        public string Dept { get; set; }
        public string Originator { get; set; }
        public DateTime? Date_Issued { get; set; }
        public DateTime? Finish_Date { get; set; }
        public DateTime? Plan_Date { get; set; }
        public string Rank { get; set; }
        public string Status { get; set; }
        public decimal Progress_Percentage { get; set; }
        public string Remarks { get; set; }
        public int? Duration_Days { get; set; }
        public string CostImpact { get; set; }
    }

    public class PE_PCR_Comment_Files
    {
        [Key]
        public int ID { get; set; }
        public int C_ID { get; set; }
        [ForeignKey("C_ID")]
        public virtual PE_PCR_Comments PE_PCR_Comments { get; set; }
        public string File_Name { get; set; }
        public string Mime_Type { get; set; }

    }

    public class PE_PCR_Management_Files
    {
        [Key]
        public int ID { get; set; }
        public int Head_ID { get; set; }
        public int M_Level { get; set; }
        public string File_Name { get; set; }
    }

    public class PCRConnection : DbContext
    {
        public DbSet<V_Users_Active> V_Users_Active { get; set; }
        public DbSet<PE_PCR_Head> PE_PCR_Head { get; set; }
        public DbSet<PE_PCR_Classification> PE_PCR_Classification { get; set; }
        public DbSet<PE_PCR_Classification_Master> PE_PCR_Classification_Master { get; set; }
        public DbSet<PE_PCR_DTBR> PE_PCR_DTBR { get; set; }
        public DbSet<PE_PCR_DTBR_Master> PE_PCR_DTBR_Master { get; set; }
        public DbSet<PE_PCR_Detail> PE_PCR_Detail { get; set; }
        public DbSet<PE_PCR_Approval_Head> PE_PCR_Approval_Head { get; set; }
        public DbSet<PE_PCR_Approval_Status> PE_PCR_Approval_Status { get; set; }
        public DbSet<PE_PCR_Files> PE_PCR_Files { get; set; }
        public DbSet<PE_PCR_Sign_Master> PE_PCR_Sign_Master { get; set; }
        public DbSet<V_PE_PCR_Summary> V_PE_PCR_Summary { get; set; }

        // -- PHASE 2 -- //
        public DbSet<PE_PCR_Department_Master> PE_PCR_Department_Master { get; set; }
        public DbSet<PE_PCR_Department_Approval> PE_PCR_Department_Approval { get; set; }
        public DbSet<PE_PCR_Department_Files> PE_PCR_Department_Files { get; set; }
        public DbSet<PE_PCR_Management_Approval> PE_PCR_Management_Approval { get; set; }
        public DbSet<PE_PCR_Comment_Files> PE_PCR_Comment_Files { get; set; }
        public DbSet<PE_PCR_Comments> PE_PCR_Comments { get; set; }
        public DbSet<PE_PCR_Management_Files> PE_PCR_Management_Files { get; set; }
        public PCRConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}