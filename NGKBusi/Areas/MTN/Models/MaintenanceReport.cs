using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.MTN.Models
{
    public class MTN_MaintenanceReport_Breakdown_Master_List
    {
        public int ID { get; set; }
        public string Category { get; set; }
        public string Item { get; set; }
    }
    public class MTN_MaintenanceReport_Breakdown_Form
    {
        [ForeignKey("Sparepart_List")]
        public int ID { get; set; }
        public string Issued_Number { get; set; }
        public string Issuer_NIK { get; set; }
        public string Issuer_Name { get; set; }
        public string Type { get; set; }
        public DateTime? Issued_Date { get; set; }
        public string Section { get; set; }
        public string Machine { get; set; }
        public string Breakdown_Problem { get; set; }
        public string Breakdown_Condition { get; set; }
        public string Breakdown_Cause { get; set; }
        public int? Breakdown_Cause_Time { get; set; }
        public string Breakdown_Action { get; set; }
        public int? Breakdown_Action_Time { get; set; }
        public DateTime? Start_Date { get; set; }
        public DateTime? End_Date { get; set; }
        public string Breakdown_Fix_Result { get; set; }
        public string Breakdown_Fix_PIC { get; set; }
        public string Status { get; set; }
        public int? Breakdown_Sparepart_Time { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int? Approval { get; set; }
        public int? Approval_Sub { get; set; }
        public ICollection<MTN_MaintenanceReport_Breakdown_Form_Sparepart> Sparepart_List { get; set; }
    }
    public class MTN_MaintenanceReport_Breakdown_Form_Sparepart
    {
        public int ID { get; set; }
        [ForeignKey("Breakdown_Form")]
        public int Form_ID { get; set; }
        public string Breakdown_Factor { get; set; }
        public string Breakdown_Part { get; set; }
        public string Breakdown_Specific { get; set; }
        public string Sparepart_Code { get; set; }
        public string Sparepart_Name { get; set; }
        public int? Qty { get; set; }
        public virtual MTN_MaintenanceReport_Breakdown_Form Breakdown_Form { get; set; }
    }
    public class MTN_MaintenanceReport_Breakdown_Form_Attachment
    {
        public int ID { get; set; }
        [ForeignKey("Breakdown_Form")]
        public int Form_ID { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
        public virtual MTN_MaintenanceReport_Breakdown_Form Breakdown_Form { get; set; }
    }


    public class MaintenanceReportConnection : DbContext
    {
        public DbSet<MTN_MaintenanceReport_Breakdown_Master_List> MTN_MaintenanceReport_Breakdown_Master_List { get; set; }
        public DbSet<MTN_MaintenanceReport_Breakdown_Form> MTN_MaintenanceReport_Breakdown_Form { get; set; }
        public DbSet<MTN_MaintenanceReport_Breakdown_Form_Sparepart> MTN_MaintenanceReport_Breakdown_Form_Sparepart { get; set; }
        public DbSet<MTN_MaintenanceReport_Breakdown_Form_Attachment> MTN_MaintenanceReport_Breakdown_Form_Attachment { get; set; }
        public MaintenanceReportConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}