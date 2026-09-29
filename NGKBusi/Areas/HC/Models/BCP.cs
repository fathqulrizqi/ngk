using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HC.Models
{
    // ═══════════════════════════════════════════════════════════
    // 1. HC_BCP_Organization — BCP Committee Structure
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Organization")]
    public class HC_BCP_Organization
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(20)]
        public string NIK { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Roles: Presdir, DGM, DGMDeputy, SrManager, Manager
        /// </summary>
        [Required]
        [StringLength(20)]
        public string BCP_Role { get; set; }

        [StringLength(100)]
        public string PositionName { get; set; }

        [StringLength(100)]
        public string DepartmentName { get; set; }

        [StringLength(20)]
        public string DepartmentCode { get; set; }

        /// <summary>
        /// FK self-reference — direct superior in BCP hierarchy
        /// </summary>
        public int? ParentID { get; set; }

        public bool IsActive { get; set; }

        public DateTime? Created_At { get; set; }

        [StringLength(20)]
        public string Created_By { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 2. HC_BCP_Incident — Incident Report
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Incident")]
    public class HC_BCP_Incident
    {
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// Auto-generated: BCP-YYMM-XXXX
        /// </summary>
        [StringLength(20)]
        public string IncidentNo { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        public DateTime? IncidentDate { get; set; }

        /// <summary>
        /// Natural Disaster, Fire, Flood, IT System Failure, 
        /// Supply Chain, Pandemic, Power Outage, Other
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; }

        /// <summary>
        /// Critical, High, Medium, Low
        /// </summary>
        [StringLength(20)]
        public string Severity { get; set; }

        /// <summary>
        /// 1=Submitted, 2=Assessment, 3=SrManagerReview, 4=DGMReview, 
        /// 5=PresdirReview, 6=BCP_Activated, 7=BCP_NotActivated, 8=Resolved
        /// </summary>
        public int Status { get; set; }

        [StringLength(20)]
        public string Created_By { get; set; }

        public DateTime? Created_At { get; set; }

        /// <summary>
        /// Whether DGM has forwarded this to Presdir
        /// </summary>
        public bool ForwardedToPresdir { get; set; }

        [StringLength(20)]
        public string Activated_By { get; set; }

        public DateTime? Activated_At { get; set; }

        public string InfrastructureImpact { get; set; }
        public string PersonnelStatus { get; set; }
        public string ImmediateActions { get; set; }

        /// <summary>
        /// Activated, NotActivated
        /// </summary>
        [StringLength(20)]
        public string Activation_Decision { get; set; }

        public DateTime? Resolved_At { get; set; }

        public string Resolved_Note { get; set; }
        public int? SourceAreaID { get; set; }

        [ForeignKey("IncidentID")]
        public virtual ICollection<HC_BCP_Incident_Photo> Photos { get; set; }

        [ForeignKey("IncidentID")]
        public virtual ICollection<HC_BCP_Assessment> Assessments { get; set; }

        [ForeignKey("IncidentID")]
        public virtual ICollection<HC_BCP_Review> Reviews { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 2.9 HC_BCP_Department — BCP Department / Section Master
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Department")]
    public class HC_BCP_Department
    {
        [Key]
        public int ID { get; set; }
        [Required, StringLength(100)]
        public string DeptName { get; set; }
        [StringLength(20)]
        public string Code { get; set; }
        public bool IsActive { get; set; }
        public DateTime? Created_At { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 3. HC_BCP_Area — BCP Area Master
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Area")]
    public class HC_BCP_Area
    {
        [Key]
        public int ID { get; set; }
        [Required, StringLength(100)]
        public string AreaName { get; set; }
        public bool IsActive { get; set; }
        
        [StringLength(20)]
        public string DepartmentCode { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 3.1 HC_BCP_User_Area — Mapping User to Area
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_User_Area")]
    public class HC_BCP_User_Area
    {
        [Key]
        public int ID { get; set; }
        [Required, StringLength(20)]
        public string NIK { get; set; }
        public int AreaID { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 4. HC_BCP_Incident_Photo — Incident Photo Attachments
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Incident_Photo")]
    public class HC_BCP_Incident_Photo
    {
        [Key]
        public int ID { get; set; }

        public int IncidentID { get; set; }

        [StringLength(255)]
        public string FileName { get; set; }

        [StringLength(500)]
        public string FilePath { get; set; }

        public DateTime? Uploaded_At { get; set; }

        [StringLength(20)]
        public string Uploaded_By { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 4. HC_BCP_Assessment — Assessment per Manager per Incident
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Assessment")]
    public class HC_BCP_Assessment
    {
        [Key]
        public int ID { get; set; }

        public int IncidentID { get; set; }

        [Required]
        [StringLength(20)]
        public string AssessorNIK { get; set; }

        [StringLength(100)]
        public string AssessorName { get; set; }

        [StringLength(100)]
        public string DepartmentName { get; set; }

        /// <summary>
        /// null = Pending, true = Impacted, false = Not Impacted
        /// </summary>
        public bool? IsImpacted { get; set; }

        public string ImpactDescription { get; set; }

        /// <summary>
        /// Weighted total score (max 5.00)
        /// </summary>
        public decimal? TotalScore { get; set; }

        /// <summary>
        /// 0=Pending, 1=Submitted, 2=SrReviewed
        /// </summary>
        public int Status { get; set; }

        public int? AreaID { get; set; }

        [StringLength(20)]
        public string LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedAt { get; set; }

        public DateTime? Created_At { get; set; }

        public DateTime? Submitted_At { get; set; }

        [ForeignKey("AssessmentID")]
        public virtual ICollection<HC_BCP_Assessment_Detail> Details { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 4.1 HC_BCP_Assessment_Question — Assessment Question
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Assessment_Question")]
    public class HC_BCP_Assessment_Question
    {
        [Key]
        public int ID { get; set; }

        public int CriteriaID { get; set; }

        [Required]
        public string QuestionText { get; set; }

        /// <summary>Bobot pertanyaan dalam persen (0-100). Nilai akhir = Score × Weight / 100</summary>
        public decimal? Weight { get; set; }

        public string AllowedDepartments { get; set; }

        public int? SortOrder { get; set; }
        public bool IsActive { get; set; }

        [ForeignKey("CriteriaID")]
        public virtual HC_BCP_Assessment_Criteria Criteria { get; set; }

        [ForeignKey("QuestionID")]
        public virtual ICollection<HC_BCP_Assessment_Question_Choice> Choices { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 4.2 HC_BCP_Assessment_Question_Choice
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Assessment_Question_Choice")]
    public class HC_BCP_Assessment_Question_Choice
    {
        [Key]
        public int ID { get; set; }

        public int QuestionID { get; set; }

        public int ScoreValue { get; set; }

        [Required]
        public string ChoiceText { get; set; }

        [ForeignKey("QuestionID")]
        public virtual HC_BCP_Assessment_Question Question { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 4.3 HC_BCP_Assessment_Answer
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Assessment_Answer")]
    public class HC_BCP_Assessment_Answer
    {
        [Key]
        public int ID { get; set; }

        public int AssessmentID { get; set; }
        public int QuestionID { get; set; }
        
        public int? Score { get; set; }
        public string SelectedOptionText { get; set; }

        [ForeignKey("AssessmentID")]
        public virtual HC_BCP_Assessment Assessment { get; set; }

        [ForeignKey("QuestionID")]
        public virtual HC_BCP_Assessment_Question Question { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 5. HC_BCP_Assessment_Detail — Score per Criteria
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Assessment_Detail")]
    public class HC_BCP_Assessment_Detail
    {
        [Key]
        public int ID { get; set; }

        public int AssessmentID { get; set; }

        public int CriteriaID { get; set; }

        /// <summary>
        /// Score 1-5 (Deprecated, but kept for backward compatibility if needed)
        /// </summary>
        public int? Score { get; set; }

        /// <summary>
        /// Highest point among answered questions in this criteria
        /// </summary>
        public int? MaxScore { get; set; }

        /// <summary>
        /// Average point of answered questions in this criteria
        /// </summary>
        public decimal? AverageScore { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [ForeignKey("CriteriaID")]
        public virtual HC_BCP_Assessment_Criteria Criteria { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 6. HC_BCP_Assessment_Criteria — Master Scoring Criteria
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Assessment_Criteria")]
    public class HC_BCP_Assessment_Criteria
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Column("standar")]
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Weight in percentage (e.g. 25.00 = 25%)
        /// </summary>
        public decimal? Weight { get; set; }

        public int? SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 7. HC_BCP_Review — Review by Sr. Manager / DGM / Presdir
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Review")]
    public class HC_BCP_Review
    {
        [Key]
        public int ID { get; set; }

        public int IncidentID { get; set; }

        [Required]
        [StringLength(20)]
        public string ReviewerNIK { get; set; }

        /// <summary>
        /// SrManager, DGM, DGMDeputy, Presdir
        /// </summary>
        [StringLength(20)]
        public string ReviewerRole { get; set; }

        public string ReviewNote { get; set; }

        /// <summary>
        /// Approved, Returned, ForwardToPresdir, ActivateBCP, DoNotActivate
        /// </summary>
        [StringLength(30)]
        public string Decision { get; set; }

        public DateTime? Created_At { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 8. HC_BCP_Notification_Log — Notification Audit Trail
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Notification_Log")]
    public class HC_BCP_Notification_Log
    {
        [Key]
        public int ID { get; set; }

        public int IncidentID { get; set; }

        [StringLength(20)]
        public string RecipientNIK { get; set; }

        /// <summary>
        /// SignalR, Email, Telegram, WhatsApp, Push
        /// </summary>
        [StringLength(20)]
        public string Channel { get; set; }

        public string Message { get; set; }

        public bool IsSent { get; set; }

        public DateTime? SentAt { get; set; }

        [StringLength(500)]
        public string ErrorMessage { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 9. HC_BCP_Activation_Log — BCP Activation History
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Activation_Log")]
    public class HC_BCP_Activation_Log
    {
        [Key]
        public int ID { get; set; }

        public int IncidentID { get; set; }

        /// <summary>
        /// Activated, Deactivated, Resolved
        /// </summary>
        [StringLength(20)]
        public string Action { get; set; }

        [StringLength(20)]
        public string ActionBy { get; set; }

        public DateTime? ActionAt { get; set; }

        public string Note { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 11. HC_BCP_Incident_Member — Snapshot of Committee at Creation
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Incident_Member")]
    public class HC_BCP_Incident_Member
    {
        [Key]
        public int ID { get; set; }

        public int IncidentID { get; set; }

        [Required]
        [StringLength(20)]
        public string NIK { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string BCP_Role { get; set; }

        [StringLength(100)]
        public string DepartmentName { get; set; }

        /// <summary>
        /// Snapshot of direct superior's NIK at creation time
        /// </summary>
        [StringLength(20)]
        public string ParentNIK { get; set; }

        public DateTime? Created_At { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 11.5 HC_BCP_Role_Permission — Review Authorization Rules
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Role_permission")]
    public class HC_BCP_Role_Permission
    {
        [Key]
        public int ID { get; set; }
        
        [StringLength(50)]
        public string RoleName { get; set; }
        
        [StringLength(20)]
        public string UserNIK { get; set; }
        
        public bool IsAllowedReview { get; set; }
        public bool IsAllowedSubmitReview { get; set; }

        /// <summary>
        /// Jika true, user/role ini berhak mengakses halaman Activate dan melakukan Activate/Deactivate BCP
        /// Digunakan sebagai delegasi kewenangan jika Presdir tidak tersedia
        /// </summary>
        public bool IsAllowedActivate { get; set; }
        public bool IsAllowedManageBCP { get; set; }
        
        public DateTime? Created_At { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 10. BCPConnection — Module Specific DbContext
    // ═══════════════════════════════════════════════════════════
    public class BCPConnection : DbContext
    {
        public DbSet<HC_BCP_Organization> HC_BCP_Organization { get; set; }
        public DbSet<HC_BCP_Incident> HC_BCP_Incident { get; set; }
        public DbSet<HC_BCP_Incident_Photo> HC_BCP_Incident_Photo { get; set; }
        public DbSet<HC_BCP_Assessment> HC_BCP_Assessment { get; set; }
        public DbSet<HC_BCP_Assessment_Detail> HC_BCP_Assessment_Detail { get; set; }
        public DbSet<HC_BCP_Assessment_Criteria> HC_BCP_Assessment_Criteria { get; set; }
        public DbSet<HC_BCP_Assessment_Question> HC_BCP_Assessment_Question { get; set; }
        public DbSet<HC_BCP_Assessment_Question_Choice> HC_BCP_Assessment_Question_Choice { get; set; }
        public DbSet<HC_BCP_Assessment_Answer> HC_BCP_Assessment_Answer { get; set; }
        public DbSet<HC_BCP_Review> HC_BCP_Review { get; set; }
        public DbSet<HC_BCP_Notification_Log> HC_BCP_Notification_Log { get; set; }
        public DbSet<HC_BCP_Activation_Log> HC_BCP_Activation_Log { get; set; }
        public DbSet<HC_BCP_Incident_Member> HC_BCP_Incident_Member { get; set; }
        public DbSet<HC_BCP_Area> HC_BCP_Area { get; set; }
        public DbSet<HC_BCP_User_Area> HC_BCP_User_Area { get; set; }
        public DbSet<HC_BCP_Department> HC_BCP_Department { get; set; }
        public DbSet<HC_BCP_Role_Permission> HC_BCP_Role_Permission { get; set; }
        public DbSet<HC_BCP_Recovery_Plan> HC_BCP_Recovery_Plan { get; set; }
        public DbSet<HC_BCP_Recovery_Todo> HC_BCP_Recovery_Todo { get; set; }

        public BCPConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
 
    // ═══════════════════════════════════════════════════════════
    // Helper Models for UI / ViewModels
    // ═══════════════════════════════════════════════════════════
 
    public class BCPAreaStatus
    {
        public int AreaID { get; set; }
        public string AreaName { get; set; }
        public bool IsAssessed { get; set; }
        public string AssessorName { get; set; }
        public decimal Score { get; set; }
        public bool IsSource { get; set; }
    }
 
    public class BCPCommitteeStatus
    {
        public string NIK { get; set; }
        public string Name { get; set; }
        public string BCP_Role { get; set; }
        public string DepartmentName { get; set; }
        public bool HasAssessed { get; set; }
    }
 
    public class BCPTask
    {
        public string IncidentNo { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // Assessment, Review
    }

    public class BCPCategoryScoreVM
    {
        public int CriteriaID { get; set; }
        public string CriteriaName { get; set; }
        public decimal CriteriaWeight { get; set; }
        public int MaxScore { get; set; }
        public decimal WeightedScore { get; set; }
        public string Notes { get; set; }
    }

    public class BCPAssessmentVM
    {
        public int ID { get; set; }
        public string AssessorNIK { get; set; }
        public string AssessorName { get; set; }
        public string DepartmentName { get; set; }
        public bool? IsImpacted { get; set; }
        public decimal? TotalScore { get; set; }
        public int? AreaID { get; set; }
        public int? DepartmentID { get; set; }
        public string DepartmentCode { get; set; }
        public string AreaName { get; set; }
        public bool IsMySubordinate { get; set; }
        public List<BCPCategoryScoreVM> CategoryScores { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 12. HC_BCP_Recovery_Plan — Action Plan per Critical Issue
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Recovery_Plan")]
    public class HC_BCP_Recovery_Plan
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int IncidentID { get; set; }

        [Required]
        public int CriteriaID { get; set; }

        [Required]
        public string ActionPlanText { get; set; }

        [Required]
        public DateTime Created_At { get; set; }

        [StringLength(20)]
        public string Created_By { get; set; }

        public DateTime? Updated_At { get; set; }

        [StringLength(20)]
        public string Updated_By { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        public string RevisionComment { get; set; }

        [ForeignKey("IncidentID")]
        public virtual HC_BCP_Incident Incident { get; set; }

        [ForeignKey("CriteriaID")]
        public virtual HC_BCP_Assessment_Criteria Criteria { get; set; }

        [ForeignKey("RecoveryPlanID")]
        public virtual ICollection<HC_BCP_Recovery_Todo> Todos { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // 13. HC_BCP_Recovery_Todo — To Do item for Recovery Action Plan
    // ═══════════════════════════════════════════════════════════
    [Table("HC_BCP_Recovery_Todo")]
    public class HC_BCP_Recovery_Todo
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int RecoveryPlanID { get; set; }

        [Required]
        public string TodoText { get; set; }

        [Required]
        [StringLength(20)]
        public string PIC_NIK { get; set; }

        [Required]
        [StringLength(100)]
        public string PIC_Name { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [Required]
        public DateTime Created_At { get; set; }

        [StringLength(20)]
        public string Created_By { get; set; }

        public DateTime? Updated_At { get; set; }

        [StringLength(20)]
        public string Updated_By { get; set; }

        public string PendingNote { get; set; }

        [ForeignKey("RecoveryPlanID")]
        public virtual HC_BCP_Recovery_Plan RecoveryPlan { get; set; }
    }
}
