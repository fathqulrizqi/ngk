using System;
using System.Collections.Generic;
using NGKBusi.Areas.Quality.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace NGKBusi.Areas.Quality.Models
{
    public class EAudit
    {
    }

    public class AuditGuideSubmitDto
    {
        public string audit_event_stakeholder_id { get; set; }
        public string category_audit { get; set; }

        public List<string> selected_guides { get; set; }
    }

    public class OFISaveDto
    {
        public string ofi_header_id { get; set; }
        public List<OFIDetailDto> Details { get; set; }
        public List<OFIApprovalDto> Approvals { get; set; }
    }
    public class OFIDetailDto
    {
        public string ofi_detail_id { get; set; }
        public string observation_comment { get; set; }
        public string action_plan { get; set; }
        public DateTime? action_date { get; set; }
    }
    public class OFIApprovalDto
    {
        public string sign_type { get; set; } 
        public string status { get; set; } 
        public string sign_by { get; set; } 
    }

    public class AuditFindingListViewModel
    {
        public string Id { get; set; }
        public string StatusProblem { get; set; }
        public string Section { get; set; }       
        public string Clause { get; set; }
        public string CategoryAudit { get; set; }
        public string Question { get; set; }
        public string DueDate { get; set; }
        public string Verification1 { get; set; }
        public string Verification2 { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
    public class UserReminderDto
    {
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class ApprovalEventUpsertDto
    {
        public string audit_event_id { get; set; }
        public string recap_sign_category { get; set; }
        public string recap_sign_status { get; set; }  
    }

    public class FindingDto
    {
        public string problem { get; set; }
        public string location { get; set; }
        public string object_name { get; set; }
        public string reference { get; set; }
    }

    public class AuditorAuditeeDto
    {
        public string audity_auditor_id { get; set; }
        public string audit_event_stakeholder_id { get; set; }

        public string category_audit { get; set; }
        public string person_name { get; set; }
        public string nik { get; set; }
        public string role { get; set; } 
        public string email { get; set; }
        public string section { get; set; }
    }

    public class QuestionEventCreateDto
    {
        [Required(ErrorMessage = "Audit Event Stakeholder ID is required.")]
        public string audit_event_stakeholder_id { get; set; }

        [Required(ErrorMessage = "Section Name is required.")]
        public string section_name { get; set; }

        [Required(ErrorMessage = "Product Process is required.")]
        public string category_audit { get; set; }

        [Required(ErrorMessage = "Process Name is required.")]
        public string process_name { get; set; }

        [Required(ErrorMessage = "Clausul is required.")]
        public string clausul { get; set; }

        [Required(ErrorMessage = "Question is required.")]
        public string question { get; set; }
    }

    public class VmStakeholderInfo
    {
        public string stakeholder_id { get; set; }
        public string section_name { get; set; }
        public string audit_name { get; set; }
        public string status { get; set; }
        public DateTime? start_event { get; set; }
        public DateTime? end_event { get; set; }
    }

    public class VmAuditor
    {
        public string audity_auditor_id { get; set; }
        public string person_name { get; set; }
        public string role { get; set; }
        public string email { get; set; }
        public string nik { get; set; }
    }

    public class VmQuestion
    {
        public string question_id { get; set; }
        public string clausul { get; set; }
        public string question { get; set; }
        public string score { get; set; }
        public string judgement { get; set; }
        public string note { get; set; }
        public List<VmEvidence> Evidences { get; set; }
        public List<VmCAR> CARs { get; set; }
        public List<VmFinding> Finding { get; set; }
    }

    public class VmFinding
    {
        public string problem { get; set; }
        public string location { get; set; }
        public string object_name { get; set; }
        public string reference { get; set; }
    }

    public class VmEvidence { public string evidence_id { get; set; } public string file_url { get; set; } }
    public class VmCAR
    {
        public string no_report { get; set; }
        public string problem_name { get; set; }
        public string status_problem { get; set; }
        public List<VmIllustration> Ilustrations { get; set; }
        public List<VmFiveWhy> FiveWhys { get; set; }
    }
    public class VmIllustration { public string ilustration_id { get; set; } public string file_url { get; set; } }
    public class VmFiveWhy { public int why_number { get; set; } public string why_problem { get; set; } }
    public class CreateAuditDto
    {
        public string audit_type { get; set; }
        public string audit_standard { get; set; }
        public string audit_name { get; set; }
        public string fiscal_year { get; set; }
        public DateTime audit_start { get; set; }
        public DateTime audit_end { get; set; }
        public string PIC { get; set; }
    }

    public class SectionDto
    {
        public string section_name { get; set; }
    }
    public class AuditorDto
    {
        
        public string person_name { get; set; }
        public string role { get; set; }
        public string email { get; set; }
        public string nik { get; set; }
        public string status { get; set; }
        public string section { get; set; }
        public List<AuditorDetailDto> Details { get; set; }
    }

    public class AuditorDetailDto
    {
        public int audit_standard_id { get; set; }
        public string category_audit { get; set; }
        public long section_id { get; set; }
    }
    public class QuestionDto
    {
        public long section_id { get; set; }
        public int audit_standard_id { get; set; }
        public string category_audit { get; set; }
        public string clausul { get; set; }
        public string process_name { get; set; }
        public string question { get; set; }
        public List<HttpPostedFileBase> evidenceFiles { get; set; }
        public List<EvidenceDto> Evidences { get; set; }
    }

    public class CreateStakeholderDto
    {
        public List<string> ListSection { get; set; }
    }
    public class ApprovalUpsertDto
    {
        public string audit_event_stakeholder_id { get; set; }
        public string category_recap { get; set; }
        public string recap_sign_status { get; set; }

        public string recap_sign_role { get; set; }
    }

    public class QuestionEventDto
    {
        public string clausul { get; set; }
        public string process_name { get; set; }
        public string question { get; set; }
        public string score { get; set; }
        public string criteria { get; set; }
        public string judgement { get; set; }
        public string note { get; set; }

        public List<EvidenceDto> Evidences { get; set; }
        public FindingDto Finding { get; set; }
        public List<HttpPostedFileBase> evidenceFiles { get; set; }
    }
    public class EvidenceDto
    {
        public string evidence_id { get; set; }
        public string file_url { get; set; }
    }

    public class JudgementDto
    {
        public string score { get; set; }
        public string criteria { get; set; }
        public string judgement { get; set; }
    }
    public class StakeholderDto
    {
        public DateTime? start_event { get; set; }
        public DateTime? end_event { get; set; }
    }

    
    public class Quality_EAudit_Audit_Standard_Master
    {
        [Key]
        public int audit_standard_id { get; set; }
        public string audit_standard_name { get; set; }
        public string mr_name { get; set; }
        public string mr_email { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }



    public class Quality_EAudit_Audity_Auditor_Master
    {
        [Key]
        public int audity_auditor_id { get; set; }

        public string person_name { get; set; }

        public string role { get; set; }
        public string email { get; set; }
        public string nik { get; set; }
        public string status { get; set; }
        public string section { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? Deleted_at { get; set; }

        public virtual ICollection<Quality_EAudit_Audity_Auditor_Master_Detail> AuditorDetails { get; set; }
    }

    public class Quality_EAudit_Audity_Auditor_Master_Detail
    {
        [Key]
        public int id { get; set; }
        public int audity_auditor_id { get; set; }
        [ForeignKey("audity_auditor_id")]
        public virtual Quality_EAudit_Audity_Auditor_Master Audity_Auditor_Master { get; set; }
        public int audit_standard_id { get; set; }
        [ForeignKey("audit_standard_id")]
        public virtual Quality_EAudit_Audit_Standard_Master Audit_Standard { get; set; }

        public string category_audit { get; set; }

        public long section_id { get; set; }

        [ForeignKey("section_id")]
        public virtual Quality_EAudit_Section Section { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? Deleted_at { get; set; }
    }

    public class Quality_EAudit_Section
    {
        [Key]
        public long section_id { get; set; }
        public string section_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_Question_Master
    {
        [Key]
        public int question_master_id { get; set; }
        public long section_id { get; set; }
        [ForeignKey("section_id")]
        public virtual Quality_EAudit_Section Section { get; set; }
        public int audit_standard_id { get; set; }
        [ForeignKey("audit_standard_id")]
        public virtual Quality_EAudit_Audit_Standard_Master Audit_Standard { get; set; }
        public string category_audit { get; set; }
        public string clausul { get; set; }
        public string process_name { get; set; }
        public string question { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_Judgement_Criteria
    {
        [Key]
        public int judgement_criteri_id { get; set; }
        public string score { get; set; }
        public string criteria { get; set; }
        public string judgement { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_Audit_Event
    {
        [Key]
        public string audit_event_id { get; set; }
        public string audit_type { get; set; }
        public string audit_standard { get; set; }
        public string audit_name { get; set; }
        public string fiscal_year { get; set; }
        public string PIC { get; set; }
        public string audit_status { get; set; }
        public virtual ICollection<Quality_EAudit_Audit_Event_Stakeholder> Stakeholders { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_Approval_Recap_Event
    {
        [Key]
        public string recap_event_approval_id { get; set; }
        public string audit_event_id { get; set; }
        [ForeignKey("audit_event_id")]
        public virtual Quality_EAudit_Audit_Event AuditEventRecapAppvoal { get; set; }
        public string recap_sign_category { get; set; }
        public string recap_sign_by { get; set; }
        public DateTime? recap_sign_date { get; set; }
        public string recap_sign_status { get; set; }
        
    }

    public class Quality_EAudit_Audit_Event_Stakeholder
    {
        [Key]
        public string audit_event_stakeholder_id { get; set; }
        public DateTime? start_event { get; set; }
        public DateTime? end_event { get; set; }

        public string responsibility_section { get; set; }

        public string audit_event_id { get; set; }
        public string status { get; set; }

        [ForeignKey("audit_event_id")]
        public virtual Quality_EAudit_Audit_Event AuditEvent { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual ICollection<Quality_EAudit_Audity_Auditor_Event> Auditors { get; set; }
        public virtual ICollection<Quality_EAudit_Question_Event> Questions { get; set; }
    }

    public class Quality_EAudit_Audit_Guide
    {
        [Key]
        public string audit_guide_id { get; set; }
        public string category_audit { get; set; }
        public string guide_name { get; set; }

        public string audit_event_stakeholder_id { get; set; }

        [ForeignKey("audit_event_stakeholder_id")]
        public virtual Quality_EAudit_Audit_Event_Stakeholder AuditEventStakeholder { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

    }
    public class AuditNoteSubmitDto
    {
        public string audit_event_stakeholder_id { get; set; }
        public string category_audit { get; set; }
        public string note { get; set; }
    }

    public class Quality_EAudit_Audit_Note
    {
        [Key]
        public string audit_note_id { get; set; }
        public string category_audit { get; set; }
        public string note { get; set; }

        public string audit_event_stakeholder_id { get; set; }

        [ForeignKey("audit_event_stakeholder_id")]
        public virtual Quality_EAudit_Audit_Event_Stakeholder AuditEventStakeholder { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }


    public class Quality_EAudit_Recap_Approval
    {
        [Key]
        public string recap_id { get; set; }
        public string audit_event_stakeholder_id { get; set; }
        [ForeignKey("audit_event_stakeholder_id")]
        public virtual Quality_EAudit_Audit_Event_Stakeholder AuditEventStakeholder { get; set; }
        public string category_recap { get; set; }
        public string recap_sign_role { get; set; }
        public string recap_sign_by { get; set; }
        public DateTime? recap_sign_date { get; set; }
        public string recap_sign_status { get; set; }
    }


    public class Quality_EAudit_Audity_Auditor_Event
    {
        [Key]
        public string audity_auditor_id { get; set; }

        public string audit_event_stakeholder_id { get; set; }

        [ForeignKey("audit_event_stakeholder_id")]
        public virtual Quality_EAudit_Audit_Event_Stakeholder AuditEventStakeholder { get; set; }


        public string person_name { get; set; }
        public string nik { get; set; }
        public string role { get; set; }
        public string category_audit { get; set; }
        public string email { get; set; }
        public string status { get; set; }
        public string section { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_Question_Event
    {
        [Key]
        public string question_id { get; set; }

        public string audit_event_stakeholder_id { get; set; }

        [ForeignKey("audit_event_stakeholder_id")]
        public virtual Quality_EAudit_Audit_Event_Stakeholder AuditEventStakeholder { get; set; }
        public string category_audit { get; set; }

        public string clausul { get; set; }
        public string process_name { get; set; }
        public string question { get; set; }
        public string score { get; set; }
        public string criteria { get; set; }
        public string judgement { get; set; }
        public string note { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual ICollection<Quality_EAudit_Evidence_Question> Evidences { get; set; }
        public virtual ICollection<Quality_EAudit_CAR_Header> CARs { get; set; }
        public virtual ICollection<Quality_EAudit_OFI_Detail> OFIs { get; set; }
        public virtual ICollection<Quality_EAudit_Finding_Question> Finding { get; set; }

    }

    public class Quality_EAudit_Evidence_Question
    {
        [Key]
        public string evidence_id { get; set; }

        public string question_id { get; set; }

        [ForeignKey("question_id")]
        public virtual Quality_EAudit_Question_Event QuestionEvent { get; set; }

        public string file_url { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_Finding_Question
    {
        [Key]
        public string finding_id { get; set; }

        public string question_id { get; set; }

        [ForeignKey("question_id")]
        public virtual Quality_EAudit_Question_Event QuestionEvent { get; set; }

        public string problem { get; set; }
        public string location { get; set; }
        public string object_name { get; set; }
        public string reference { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_CAR_Header
    {
        [Key]
        public string no_report { get; set; }
        public string question_id { get; set; }

        [ForeignKey("question_id")]
        public virtual Quality_EAudit_Question_Event QuestionEvent { get; set; }
        public DateTime date { get; set; }
        public string to_Dept { get; set; }
        public string attention { get; set; }
        public string from_auditor { get; set; }
        public string qty_check { get; set; }
        public string ng_rasio { get; set; }
        public string incident_status { get; set; }
        public string reply_deadline { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual ICollection<Quality_EAudit_CAR_Ilustration_Image> Ilustration { get; set; }
        public virtual ICollection<Quality_EAudit_CAR_5_Why> FiveWhy { get; set; }
        public virtual ICollection<Quality_EAudit_CAR_Verification> Verification { get; set; }
        public virtual ICollection<Quality_EAudit_CAR_Approval> Approval { get; set; }
        public virtual ICollection<Quality_EAudit_CAR_Problem> Problem { get; set; }
        public virtual ICollection<Quality_EAudit_CAR_Action> Action { get; set; }
        public virtual ICollection<Quality_EAudit_CAR_Evidence> Evidence { get; set; }

    }

    public class Quality_EAudit_CAR_Action
    {
        [Key]
        public string action_id { get; set; }
        public string no_report { get; set; }

        [ForeignKey("no_report")]
        public virtual Quality_EAudit_CAR_Header CARHeader { get; set; }
        public string PIC_dept { get; set; }
        public DateTime? action_date { get; set; }
        public string review_PFMEA { get; set; }
        public string containment_action { get; set; }
        public string PIC_containment_action { get; set; }
        public DateTime? date_containment_action { get; set; }
        public string detail_correction { get; set; }
        public string PIC_detail_correction { get; set; }
        public DateTime? date_detail_correction { get; set; }
        public string detail_corrective_action { get; set; }
        public string PIC_detail_corrective_action { get; set; }
        public DateTime? date_detail_corrective_action { get; set; }
        public string yokotenkai { get; set; }
        public string initial_lot_after_repair { get; set; }
        public string PIC_yokotenkai { get; set; }
        public DateTime? date_yokotenkai { get; set; }
        public string is_document_updated { get; set; } 
        public string document_type_updated { get; set; } 
        public string document_number { get; set; }
    }

    public class Quality_EAudit_CAR_Problem
    {
        [Key]
        public string problem_id { get; set; }
        public string no_report { get; set; }

        [ForeignKey("no_report")]
        public virtual Quality_EAudit_CAR_Header CARHeader { get; set; }
        public string problem_name { get; set; }
        public string problem_location { get; set; }
        public string problem_part_name { get; set; }
        public string problem_lot_number { get; set; }
        public string problem_qty { get; set; }
        public string problem_detail { get; set; }
        public string problem_category { get; set; }
        public string problem_status { get; set; }
    }

    public class Quality_EAudit_CAR_Approval
    {
        [Key]
        public string approval_id { get; set; }

        public string no_report { get; set; }

        [ForeignKey("no_report")]
        public virtual Quality_EAudit_CAR_Header CARHeader { get; set; }
        public string group_type { get; set; }
        public string role_type { get; set; }
        public string sign_by { get; set; }
        public DateTime? sign_date { get; set; }
        public string status { get; set; }
    }

    public class Quality_EAudit_CAR_Verification
    {
        [Key]
        public string verification_id { get; set; }
        public string no_report { get; set; }


        [ForeignKey("no_report")]
        public virtual Quality_EAudit_CAR_Header CARHeader { get; set; }
        public string verification_name { get; set; }
        public string verification_comment { get; set; }
        public string verification_auditor_sign_by { get; set; }
        public string verification_auditor_status { get; set; }
        public DateTime? verification_auditor_date { get; set; }
        public DateTime? verification_mr_date { get; set; }
        public string verification_mr_sign_by { get; set; }
        public string verification_mr_status { get; set; }
        public string verification_status {  get; set; }
    }

    public class Quality_EAudit_CAR_Ilustration_Image
    {
        [Key]
        public string ilustration_id { get; set; }

        public string no_report { get; set; }

        [ForeignKey("no_report")]
        public virtual Quality_EAudit_CAR_Header CARHeader { get; set; }

        public string file_url { get; set; }
    }

    public class Quality_EAudit_CAR_5_Why
    {
        [Key]
        public string why_id { get; set; }

        public string no_report { get; set; }

        [ForeignKey("no_report")]
        public virtual Quality_EAudit_CAR_Header CARHeader { get; set; }
        public int why_number { get; set; }
        public string why_problem { get; set; }
    }

    public class Quality_EAudit_CAR_Evidence
    {
        [Key]
        public string evidence_id { get; set; }
        public string no_report { get; set; }
        [ForeignKey("no_report")]
        public virtual Quality_EAudit_CAR_Header CARHeader { get; set; }
        public string evidence_name { get; set; }
        public string evidence_url { get; set; }

    }

    public class Quality_EAudit_OFI_Header
    {
        [Key]
        public string ofi_header_id { get; set; }

        public string audit_event_stakeholder_id { get; set; }

        [ForeignKey("audit_event_stakeholder_id")]
        public virtual Quality_EAudit_Audit_Event_Stakeholder AuditEventStakeholder { get; set; }

        public string reply_deadline { get; set; }
        public string status { get; set; } // misal: "Draft", "Submitted", "Approved"

        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual ICollection<Quality_EAudit_OFI_Detail> OFIDetails { get; set; }
        public virtual ICollection<Quality_EAudit_OFI_Approval> ApprovalOFI { get; set; }
    }

    public class Quality_EAudit_OFI_Detail
    {
        [Key]
        public string ofi_detail_id { get; set; }

        public string ofi_header_id { get; set; }

        [ForeignKey("ofi_header_id")]
        public virtual Quality_EAudit_OFI_Header OFIHeader { get; set; }

        public string question_id { get; set; }

        [ForeignKey("question_id")]
        public virtual Quality_EAudit_Question_Event QuestionEvent { get; set; }

        public string clause { get; set; }
        public string observation_comment { get; set; }
        public string action_plan { get; set; }
        public DateTime? action_date { get; set; }
    }

    public class Quality_EAudit_OFI_Approval
    {
        [Key]
        public string approval_id { get; set; }

        public string ofi_header_id { get; set; } 

        [ForeignKey("ofi_header_id")]
        public virtual Quality_EAudit_OFI_Header OFIHeader { get; set; }

        public string sign_type { get; set; }
        public string sign_by { get; set; }
        public DateTime? sign_date { get; set; }
        public string status { get; set; }
    }



    public class Quality_EAudit_Access
    {
        [Key]
        public int access_id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        [Index(IsUnique = true)]
        public string NIK { get; set; }
        public string access { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class Quality_EAudit_Section_Leader
    {
        [Key]
        public int leader_id { get; set; }

        [Index("IX_SectionAndAccess", 1, IsUnique = true)]
        public long section_id { get; set; }

        [ForeignKey("section_id")]
        public virtual Quality_EAudit_Section Section { get; set; }

        [Index("IX_SectionAndAccess", 2, IsUnique = true)]
        public int access_id { get; set; }

        [ForeignKey("access_id")]
        public virtual Quality_EAudit_Access Access { get; set; }

        public string position { get; set; }

        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }





    public class QualityConnection : DbContext
    {
        public DbSet<Quality_EAudit_Section> Quality_EAudit_Section { get; set; }
        public DbSet<Quality_EAudit_Audity_Auditor_Master> Quality_EAudit_Audity_Auditor_Master { get; set; }
        public DbSet<Quality_EAudit_Audity_Auditor_Master_Detail> Quality_EAudit_Audity_Auditor_Master_Detail { get; set; }
        public DbSet<Quality_EAudit_Question_Master> Quality_EAudit_Question_Master { get; set; }
        public DbSet<Quality_EAudit_Audit_Event> Quality_EAudit_Audit_Event { get; set; }
        public DbSet<Quality_EAudit_Audit_Event_Stakeholder> Quality_EAudit_Audit_Event_Stakeholder { get; set; }
        public DbSet<Quality_EAudit_Judgement_Criteria> Quality_EAudit_Judgement_Criteria { get; set; }
        public DbSet<Quality_EAudit_Audity_Auditor_Event> Quality_EAudit_Audity_Auditor_Event { get; set; }
        public DbSet<Quality_EAudit_Question_Event> Quality_EAudit_Question_Event { get; set; }
        public DbSet<Quality_EAudit_Evidence_Question> Quality_EAudit_Evidence_Question { get; set; }
        public DbSet<Quality_EAudit_Access> Quality_EAudit_Access { get; set; }
        public DbSet<Quality_EAudit_CAR_Header> Quality_EAudit_CAR_Header { get; set; }
        public DbSet<Quality_EAudit_CAR_5_Why> Quality_EAudit_CAR_5_Why { get; set; }
        public DbSet<Quality_EAudit_CAR_Problem> Quality_EAudit_CAR_Problem { get; set; }
        public DbSet<Quality_EAudit_CAR_Action> Quality_EAudit_CAR_Action { get; set; }
        public DbSet<Quality_EAudit_CAR_Verification> Quality_EAudit_CAR_Verification { get; set; }
        public DbSet<Quality_EAudit_CAR_Approval> Quality_EAudit_CAR_Approval { get; set; }
        public DbSet<Quality_EAudit_CAR_Ilustration_Image> Quality_EAudit_CAR_Ilustration_Image { get; set; }
        public DbSet<Quality_EAudit_Audit_Standard_Master> Quality_EAudit_Audit_Standard_Master { get; set; }
        public DbSet<Quality_EAudit_CAR_Evidence> Quality_EAudit_CAR_Evidence { get; set; }
        public DbSet<Quality_EAudit_OFI_Header> Quality_EAudit_OFI_Header { get; set; }
        public DbSet<Quality_EAudit_OFI_Detail> Quality_EAudit_OFI_Detail { get; set; }
        public DbSet<Quality_EAudit_OFI_Approval> Quality_EAudit_OFI_Approval { get; set; }
        public DbSet<Quality_EAudit_Finding_Question> Quality_EAudit_Finding_Question { get; set; }
        public DbSet<Quality_EAudit_Audit_Guide> Quality_EAudit_Audit_Guide { get; set; }
        public DbSet<Quality_EAudit_Audit_Note> Quality_EAudit_Audit_Note { get; set; }
        public DbSet<Quality_EAudit_Recap_Approval> Quality_EAudit_Recap_Approval { get; set; }
        public DbSet<Quality_EAudit_Approval_Recap_Event> Quality_EAudit_Approval_Recap_Event { get; set; }
        public QualityConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

    }
}