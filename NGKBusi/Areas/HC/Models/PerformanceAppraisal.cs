using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HC.Models
{
    public class HC_Performance_Appraisal
    {
        public int ID { get; set; }
        public string GUID { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string DivName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string PostName { get; set; }
        public string CostID { get; set; }
        public string CostName { get; set; }
        public decimal Performance_KPI_Self { get; set; }
        public decimal Performance_KPI_Direct { get; set; }
        public decimal Performance_KPI { get; set; }
        public int Integrity_Discipline_Self { get; set; }
        public decimal Integrity_Discipline_Direct { get; set; }
        public decimal Integrity_Discipline { get; set; }
        public int Integrity_Participation_Self { get; set; }
        public decimal Integrity_Participation_Direct { get; set; }
        public decimal Integrity_Participation { get; set; }
        public string Integrity_Participation_Comittee { get; set; }
        public string Integrity_Participation_Participant { get; set; }
        public string Integrity_Participation_Remark { get; set; }
        public int Competency_Knowledge_Self { get; set; }
        public decimal Competency_Knowledge_Direct { get; set; }
        public decimal Competency_Knowledge { get; set; }
        public int Competency_Skill_Self { get; set; }
        public decimal Competency_Skill_Direct { get; set; }
        public decimal Competency_Skill { get; set; }
        public int Competency_Behaviour_Self { get; set; }
        public decimal Competency_Behaviour_Direct { get; set; }
        public decimal Competency_Behaviour { get; set; }
        public string Note_Strenght_Indirect { get; set; }
        public string Note_Strenght_Direct { get; set; }
        public string Note_Strenght_Self { get; set; }
        public string Note_Weakness_Indirect { get; set; }
        public string Note_Weakness_Direct { get; set; }
        public string Note_Weakness_Self { get; set; }
        public string Note_Planning_Indirect { get; set; }
        public string Note_Planning_Direct { get; set; }
        public string Note_Planning_Self { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_At { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        [ForeignKey("EmployeeBy")]
        public string Sign_Employee_By { get; set; }
        public DateTime? Sign_Employee_At { get; set; }
        [ForeignKey("DirectBy")]
        public string Sign_Direct_By { get; set; }
        public DateTime? Sign_Direct_At { get; set; }
        [ForeignKey("IndirectBy")]
        public string Sign_Indirect_By { get; set; }
        public DateTime? Sign_Indirect_At { get; set; }
        [ForeignKey("ReviewedBy")]
        public string Sign_Reviewed_By { get; set; }
        public DateTime? Sign_Reviewed_At { get; set; }
        [ForeignKey("ApprovedBy")]
        public string Sign_Approved_By { get; set; }
        public DateTime? Sign_Approved_At { get; set; }
        [ForeignKey("FinalizedBy")]
        public string Sign_Finalize_By { get; set; }
        public DateTime? Sign_Finalize_At { get; set; }
        public bool IsSaved { get; set; }
        public bool IsReleased { get; set; }
        public int IsWarning { get; set; }

        public virtual Users EmployeeBy { get; set; }
        public virtual Users DirectBy { get; set; }
        public virtual Users IndirectBy { get; set; }
        public virtual Users ReviewedBy { get; set; }
        public virtual Users ApprovedBy { get; set; }
        public virtual Users FinalizedBy { get; set; }
    }

    public class HC_Performance_Appraisal_Percentage
    {
        public int ID { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public decimal Performance_KPI { get; set; }
        public decimal Integrity_Discipline { get; set; }
        public decimal Integrity_Participation { get; set; }
        public decimal Competency_Knowledge { get; set; }
        public decimal Competency_Skill { get; set; }
        public decimal Competency_Behaviour { get; set; }
    }


    public class HC_Performance_Appraisal_Positioning
    {
        public int ID { get; set; }
        public string Employee { get; set; }
        public string Direct { get; set; }
        public string InDirect { get; set; }
    }



    public class PerformanceAppraisalConnection : DbContext
    {
        public DbSet<HC_Performance_Appraisal> HC_Performance_Appraisal { get; set; }
        public DbSet<HC_Performance_Appraisal_Percentage> HC_Performance_Appraisal_Percentage { get; set; }
        public DbSet<HC_Performance_Appraisal_Positioning> HC_Performance_Appraisal_Positioning { get; set; }

        public PerformanceAppraisalConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HC_Performance_Appraisal>()
                .Property(p => p.Performance_KPI_Self)
                .HasPrecision(18, 3);

            // ...other configuration...
            base.OnModelCreating(modelBuilder);
        }
    }
}