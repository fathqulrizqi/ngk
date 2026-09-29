using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;
using NGKBusi.Models;

namespace NGKBusi.Areas.MTN.Models
{
    public class MTN_MachineDatabase_Area
    {
        public int id { get; set; }
        public string Name { get; set; }
    }

    public class MTN_MachineDatabase_List
    {
        public int id { get; set; }
        [ForeignKey("Area")]
        public int? Area_ID { get; set; }
        public int? Machine_Old_ID { get; set; }
        public int? Machine_Parent_ID { get; set; }
        public string Name { get; set; }
        public string Machine_Number { get; set; }
        public string Asset_No { get; set; }
        public string Model { get; set; }
        public string Power { get; set; }
        public string Maker { get; set; }
        public string Serial_No { get; set; }
        public DateTime? Coming_Date { get; set; }
        public int? Qty { get; set; }
        public DateTime? Start_Date { get; set; }
        public DateTime? End_Date { get; set; }
        public DateTime? Overhaul_Schedule { get; set; }
        public string Remark { get; set; }
        public string Status { get; set; }
        public bool? Is_Scheduled { get; set; }
        public string Issued_By { get; set; }
        public DateTime? Issued_Date { get; set; }
        public string Updated_By { get; set; }
        public DateTime? Updated_Date { get; set; }
        public virtual MTN_MachineDatabase_Area Area { get; set; }
    }
    public class MTN_MachineDatabase_Maintenance_Schedule
    {
        public int id { get; set; }
        [ForeignKey("MachineArea")]
        public int? Area_ID { get; set; }
        public string Area { get; set; }
        [ForeignKey("Machine")]
        public int? Machine_ID { get; set; }
        public string Machine_Name { get; set; }
        public string Period_FY { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public bool? is3B { get; set; }
        public bool? is6B { get; set; }
        public bool? is1T { get; set; }
        public string Note { get; set; }
        public string Issued_By { get; set; }
        public DateTime? Issued_Date { get; set; }
        public string Updated_By { get; set; }
        public DateTime? Updated_Date { get; set; }
        public virtual MTN_MachineDatabase_Area MachineArea { get; set; }
        public virtual MTN_MachineDatabase_List Machine { get; set; }
    }
    public class MTN_MachineDatabase_Maintenance_Schedule_Monthly
    {
        public int id { get; set; }
        [ForeignKey("MachineArea")]
        public int? Area_ID { get; set; }
        public string Area { get; set; }
        [ForeignKey("Machine")]
        public int? Machine_ID { get; set; }
        public string Machine_Name { get; set; }
        public string Period_FY { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public string Category { get; set; }
        public string Issued_By { get; set; }
        public DateTime? Issued_Date { get; set; }
        public string Updated_By { get; set; }
        public DateTime? Updated_Date { get; set; }
        public virtual MTN_MachineDatabase_Area MachineArea { get; set; }
        public virtual MTN_MachineDatabase_List Machine { get; set; }
    }
    public class MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header
    {
        public int ID { get; set; }
        public string Period_FY { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
    }

    public class MTN_MachineDatabase_Maintenance_Schedule_Header
    {
        public int ID { get; set; }
        public string Period_FY { get; set; }
        public int Year { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? isLock { get; set; }
    }
    public class V_MachineDatabase_Maintenance_Schedule
    {
        public string ID { get; set; }
        public string Area { get; set; }
        public int? Area_ID { get; set; }
        [ForeignKey("Machine")]
        public int? Machine_ID { get; set; }
        public string Machine_Name { get; set; }
        public string Period_FY { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public bool? is3B { get; set; }
        public bool? is6B { get; set; }
        public bool? is1T { get; set; }
        public string Data_Source { get; set; }
        public int? Sequence { get; set; }
        public virtual MTN_MachineDatabase_List Machine { get; set; }
    }
    public class MTN_MachineDatabase_Maintenance_Checklist_Master
    {
        public int ID { get; set; }
        public string Machine_Name { get; set; }
        public string Item { get; set; }
        public bool? is3B { get; set; }
        public bool? is6B { get; set; }
        public bool? is1T { get; set; }
        public string Category { get; set; }
    }
    public class MTN_MachineDatabase_Maintenance_Checklist_Header
    {
        public int ID { get; set; }
        public string Period_FY { get; set; }
        public int Period { get; set; }
        public string Area { get; set; }
        public string Machine_Name { get; set; }
        public string Machine_No { get; set; }
        public DateTime? Date1 { get; set; }
        public DateTime? Date2 { get; set; }
        public DateTime? Date3 { get; set; }
        public DateTime? Date4 { get; set; }
        public string Note1 { get; set; }
        public string Note2 { get; set; }
        public string Note3 { get; set; }
        public string Note4 { get; set; }
        public string Date1_By { get; set; }
        public string Date2_By { get; set; }
        public string Date3_By { get; set; }
        public string Date4_By { get; set; }
        public DateTime? Checked_At1 { get; set; }
        [ForeignKey("Checked_User1")]
        public string Checked_By1 { get; set; }
        public DateTime? Approved_At1 { get; set; }
        [ForeignKey("Approved_User1")]
        public string Approved_By1 { get; set; }
        public DateTime? Checked_At2 { get; set; }
        [ForeignKey("Checked_User2")]
        public string Checked_By2 { get; set; }
        public DateTime? Approved_At2 { get; set; }
        [ForeignKey("Approved_User2")]
        public string Approved_By2 { get; set; }
        public DateTime? Checked_At3 { get; set; }
        [ForeignKey("Checked_User3")]
        public string Checked_By3 { get; set; }
        public DateTime? Approved_At3 { get; set; }
        [ForeignKey("Approved_User3")]
        public string Approved_By3 { get; set; }
        public DateTime? Checked_At4 { get; set; }
        [ForeignKey("Checked_User4")]
        public string Checked_By4 { get; set; }
        public DateTime? Approved_At4 { get; set; }
        [ForeignKey("Approved_User4")]
        public string Approved_By4 { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public virtual Users Checked_User1 { get; set; }
        public virtual Users Checked_User2 { get; set; }
        public virtual Users Checked_User3 { get; set; }
        public virtual Users Checked_User4 { get; set; }
        public virtual Users Approved_User1 { get; set; }
        public virtual Users Approved_User2 { get; set; }
        public virtual Users Approved_User3 { get; set; }
        public virtual Users Approved_User4 { get; set; }
    }
    public class MTN_MachineDatabase_Maintenance_Checklist_Lines
    {
        public int ID { get; set; }
        public int Header_ID { get; set; }
        [ForeignKey("Item_Master")]
        public int Item_ID { get; set; }
        public bool? isCondition { get; set; }
        public bool? isAction { get; set; }
        public string Category { get; set; }
        public int Interval { get; set; }
        public string Image_Note { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public DateTime? Updated_At { get; set; }
        public string Updated_By { get; set; }
        public virtual MTN_MachineDatabase_Maintenance_Checklist_Master Item_Master { get; set; }
    }
    public class MTN_MachineDatabase_Maintenance_Checklist_Report
    {
        public int ID { get; set; }
        public int Header_ID { get; set; }
        public int Checklist_ID { get; set; }
        public int Interval { get; set; }
        public int Item_ID { get; set; }
        public string Image_URL { get; set; }
    }

    public class MachineDatabaseConnection : DbContext
    {
        public DbSet<MTN_MachineDatabase_Area> MTN_MachineDatabase_Area { get; set; }
        public DbSet<MTN_MachineDatabase_List> MTN_MachineDatabase_List { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Schedule> MTN_MachineDatabase_Maintenance_Schedule { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Schedule_Monthly> MTN_MachineDatabase_Maintenance_Schedule_Monthly { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Checklist_Master> MTN_MachineDatabase_Maintenance_Checklist_Master { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Checklist_Header> MTN_MachineDatabase_Maintenance_Checklist_Header { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Checklist_Lines> MTN_MachineDatabase_Maintenance_Checklist_Lines { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Checklist_Report> MTN_MachineDatabase_Maintenance_Checklist_Report { get; set; }
        public DbSet<V_MachineDatabase_Maintenance_Schedule> V_MachineDatabase_Maintenance_Schedule { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Schedule_Header> MTN_MachineDatabase_Maintenance_Schedule_Header { get; set; }
        public DbSet<MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header> MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header { get; set; }
        public MachineDatabaseConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}