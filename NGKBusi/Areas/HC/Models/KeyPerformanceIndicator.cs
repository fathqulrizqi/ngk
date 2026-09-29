using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HC.Models
{
    public class HC_KPI_Header
    {

        public int ID { get; set; }
        public string GUID { get; set; }
        public string Period_FY { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string DivName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string PostName { get; set; }
        public string CostID { get; set; }
        public string CostName { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
        public int? Approval0 { get; set; }
        public int? Approval1 { get; set; }
        public int? Approval2 { get; set; }
        public int? Approval3 { get; set; }
        public int? Approval4 { get; set; }
        public int? Approval5 { get; set; }
        public int? Approval6 { get; set; }
        public int? Approval7 { get; set; }
        public int? Approval8 { get; set; }
        public int? Approval9 { get; set; }
        public int? Approval10 { get; set; }
        public int? Approval11 { get; set; }
        public int? Approval12 { get; set; }
        public int? Approval { get; set; }
        public int? Approval_Sub { get; set; }
        public ICollection<HC_KPI_Data> HC_KPI_Data { get; set; }
        public ICollection<HC_KPI_Attachment> HC_KPI_Attachment { get; set; }
    }
        public class HC_KPI_Data
    {
        public int ID { get; set; }
        [ForeignKey("HC_KPI_Header")]
        public int Header_ID { get; set; }
        public string Period_FY { get; set; }
        [ForeignKey("HC_KPI_Perspective_List")]
        public string Perspective { get; set; }
        public string KPI { get; set; }
        public string Last_Achievement { get; set; }
        public string Target { get; set; }
        public string Action_Plan { get; set; }
        public string Primary_Share { get; set; }
        public string Type { get; set; }
        public double? Weight { get; set; }
        public double? Plan_Jan { get; set; }
        public double? Plan_Feb { get; set; }
        public double? Plan_Mar { get; set; }
        public double? Plan_Apr { get; set; }
        public double? Plan_May { get; set; }
        public double? Plan_Jun { get; set; }
        public double? Plan_Jul { get; set; }
        public double? Plan_Aug { get; set; }
        public double? Plan_Sep { get; set; }
        public double? Plan_Oct { get; set; }
        public double? Plan_Nov { get; set; }
        public double? Plan_Des { get; set; }
        public double? Act_Jan { get; set; }
        public double? Act_Feb { get; set; }
        public double? Act_Mar { get; set; }
        public double? Act_Apr { get; set; }
        public double? Act_May { get; set; }
        public double? Act_Jun { get; set; }
        public double? Act_Jul { get; set; }
        public double? Act_Aug { get; set; }
        public double? Act_Sep { get; set; }
        public double? Act_Oct { get; set; }
        public double? Act_Nov { get; set; }
        public double? Act_Des { get; set; }
        public double? Percent_Jan { get; set; }
        public double? Percent_Feb { get; set; }
        public double? Percent_Mar { get; set; }
        public double? Percent_Apr { get; set; }
        public double? Percent_May { get; set; }
        public double? Percent_Jun { get; set; }
        public double? Percent_Jul { get; set; }
        public double? Percent_Aug { get; set; }
        public double? Percent_Sep { get; set; }
        public double? Percent_Oct { get; set; }
        public double? Percent_Nov { get; set; }
        public double? Percent_Des { get; set; }
        public double? Plan_Q1 { get; set; }
        public double? Plan_Q2 { get; set; }
        public double? Plan_Q3 { get; set; }
        public double? Plan_Q4 { get; set; }
        public double? Act_Q1 { get; set; }
        public double? Act_Q2 { get; set; }
        public double? Act_Q3 { get; set; }
        public double? Act_Q4 { get; set; }
        public double? Percent_Q1 { get; set; }
        public double? Percent_Q2 { get; set; }
        public double? Percent_Q3 { get; set; }
        public double? Percent_Q4 { get; set; }
        public double? Total { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }

        public virtual HC_KPI_Header HC_KPI_Header { get; set; }

        public virtual HC_KPI_Perspective_List HC_KPI_Perspective_List { get; set; }
    }
    public class HC_KPI_Perspective_Weight
    {
        public int ID { get; set; }
        public string PeriodFY { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public double Financial { get; set; }
        public double Customer { get; set; }
        public double Internal_Process { get; set; }
        public double Learning_Growth { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
    }
    public class HC_KPI_Perspective_List
    {
        public int Seq { get; set; }
        [Key]
        public string Perspective { get; set; }
    }


    public class HC_KPI_Attachment
    {
        public int ID { get; set; }
        [ForeignKey("HC_KPI_Header")]
        public int Header_ID { get; set; }
        public int Data_ID { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
        public virtual HC_KPI_Header HC_KPI_Header { get; set; }
    }
        public class KPIConnection : DbContext
    {
        public DbSet<HC_KPI_Header> HC_KPI_Header { get; set; }
        public DbSet<HC_KPI_Data> HC_KPI_Data { get; set; }
        public DbSet<HC_KPI_Perspective_Weight> HC_KPI_Perspective_Weight { get; set; }
        public DbSet<HC_KPI_Attachment> HC_KPI_Attachment { get; set; }
        public DbSet<HC_KPI_Perspective_List> HC_KPI_Perspective_List { get; set; }

        public KPIConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}