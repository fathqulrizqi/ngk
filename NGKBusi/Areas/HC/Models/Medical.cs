using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HC.Models
{
    public class HC_Welfare_Medical_Claim_Header
    {
        [ForeignKey("Lines")]
        public int ID { get; set; }
        public string Invoice { get; set; }
        public string Third_Party_ID { get; set; }
        public string Third_Party_Name { get; set; }
        public string Hospital_Clinic { get; set; } 
        public DateTime Created_At { get; set; }
        [ForeignKey("User")]
        public string Created_By { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }

        public virtual Users User { get; set; }
        public List<HC_Welfare_Medical_Claim_Line> Lines { get; set; }
    }
    public class HC_Welfare_Medical_Claim_Line
    {
        [ForeignKey("Details")]
        public int ID { get; set; }
        [ForeignKey("Header")]
        public int Header_ID { get; set; }
        [ForeignKey("User")]
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string Position { get; set; }
        public string CostName { get; set; }

        public virtual Users User { get; set; }
        public List<HC_Welfare_Medical_Claim_Line_Detail> Details { get; set; }
        public virtual HC_Welfare_Medical_Claim_Header Header { get; set; }
    }
    public class HC_Welfare_Medical_Claim_Line_Detail
    {
        public int ID { get; set; }
        [ForeignKey("Line")]
        public int Line_ID { get; set; }
        public string Name { get; set; }
        public string Beneficiary { get; set; }
        public string Treatment_Category { get; set; }
        public string Sub_Category { get; set; }
        public string Diagnose { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime End_Date { get; set; }
        public double Invoice_Ammount { get; set; }
        public double Actual_Ammount { get; set; }
        public double Saving_Ammount { get; set; }
        public double Deduction { get; set; }
        public virtual HC_Welfare_Medical_Claim_Line Line { get; set; }
    }


    public class MedicalConnection : DbContext
    {
        public DbSet<HC_Welfare_Medical_Claim_Header> HC_Welfare_Medical_Claim_Header { get; set; }
        public DbSet<HC_Welfare_Medical_Claim_Line> HC_Welfare_Medical_Claim_Line { get; set; }
        public DbSet<HC_Welfare_Medical_Claim_Line_Detail> HC_Welfare_Medical_Claim_Line_Detail { get; set; }
        public MedicalConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}