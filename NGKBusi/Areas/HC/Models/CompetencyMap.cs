using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HC.Models
{
    public class HC_Competency_Map_Header
    {
        public int ID { get; set; }
        public string GUID { get; set; }
        public string Division { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string TitleName { get; set; }
        public string CostName { get; set; }
        public string Job_Position { get; set; }
        public string MergeCells { get; set; }
    }
    public class HC_Competency_Map_Line
    {
        public int ID { get; set; }
        public int Header_ID { get; set; }
        public string No { get; set; }
        public string Requirement { get; set; }
        public int? Score { get; set; }
        public string Module { get; set; }
        public string Internal { get; set; }
        public int? Internal_Duration { get; set; }
        public string External { get; set; }
        public int? External_Duration { get; set; }
        public string Trainer { get; set; }
        public string Evaluation_Method { get; set; }
        public string Remark { get; set; }
        public int Idx { get; set; }
        public int HashCode { get; set; }
    }
    public class HC_Competency_Result_Header
    {
        public int ID { get; set; }
        public string GUID { get; set; }
        public string Period_FY { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string DivName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string TitleName { get; set; }
        public string PostName { get; set; }
        public string CostName { get; set; }
        public DateTime Join_Date { get; set; }
        public int? Working_Years { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
    }
    public class HC_Competency_Result_Line
    {
        public int ID { get; set; }
        public int Header_ID { get; set; }
        public string Type { get; set; }
        public string No { get; set; }
        public string Requirement { get; set; }
        public int? Standard_Score { get; set; }
        public int? Score { get; set; }
        public string Result { get; set; }
        public string Note { get; set; }
        public int Idx { get; set; }
        public int HashCode { get; set; }
    }

    public class HC_Competency_JobTitle
    {
        public int ID { get; set; }
        public string DivisionName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string CostName { get; set; }
        public string TitleName { get; set; }
    }

    public class CompetencyMapConnection : DbContext
    {
        public DbSet<HC_Competency_Map_Header> HC_Competency_Map_Header { get; set; }
        public DbSet<HC_Competency_Map_Line> HC_Competency_Map_Line { get; set; }
        public DbSet<HC_Competency_Result_Header> HC_Competency_Result_Header { get; set; }
        public DbSet<HC_Competency_Result_Line> HC_Competency_Result_Line { get; set; }
        public DbSet<HC_Competency_JobTitle> HC_Competency_JobTitle { get; set; }

        public CompetencyMapConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}