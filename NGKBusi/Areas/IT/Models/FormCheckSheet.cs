using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Web.Mvc;

namespace NGKBusi.Areas.IT.Models
{
    public class FormCheckSheet
    {
    }
    public class Tbl_IT_CheckSheetForm_Header
    {
        public int ID { get; set; }
        public DateTime? Periode { get; set; }
        public string Prd { get; set; }
        public string Status { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateDate { get; set; }
        public string SubmitBy { get; set; }
        public DateTime? SubmitTime { get; set; }
        public string CheckedBy { get; set; }
        public DateTime? CheckedTime { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public string ViewButton { get; set; }
    }
    public class IT_CheckSheetForm_Header
    {
        public int ID { get; set; }
        public DateTime? Periode { get; set; }
        public byte Status { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateTime { get; set; }
        public string SubmitBy { get; set; }
        public DateTime? SubmitTime { get; set; }
        public string CheckedBy { get; set; }
        public DateTime? CheckedTime { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public string Notes { get; set; }
    }
    public class V_IT_CheckSheetForm_Header
    { 
        public int ID { get; set; }
        public DateTime Periode { get; set; }
        public byte Status { get; set; }
        public DateTime? CheckedTime { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public string Notes { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateName { get; set; }
        public string SubmitBy { get; set; }
        public DateTime? SubmitTime { get; set; }
        public string SubmitName { get; set; }
        public string CheckedBy { get; set; }
        public string CheckedName { get; set; }
        public string ApprovedBy { get; set; }
        public string ApprovedName { get; set; }
    }
    public class IT_CheckSheetForm_Daily_Detail
    {
        [Key]
        public int DailyID { get; set; }
        public string ItemControlID { get; set; }
        public string ItemControlName { get; set; }
        public String ChecksheetID { get; set; }
        public string Standar { get; set; }
        public string D1 { get; set; }
        public string D2 { get; set; }
        public string D3 { get; set; }
        public string D4 { get; set; }
        public string D5 { get; set; }
        public string D6 { get; set; }
        public string D7 { get; set; }
        public string D8 { get; set; }
        public string D9 { get; set; }
        public string D10 { get; set; }
        public string D11 { get; set; }
        public string D12 { get; set; }
        public string D13 { get; set; }
        public string D14 { get; set; }
        public string D15 { get; set; }
        public string D16 { get; set; }
        public string D17 { get; set; }
        public string D18 { get; set; }
        public string D19 { get; set; }
        public string D20 { get; set; }
        public string D21 { get; set; }
        public string D22 { get; set; }
        public string D23 { get; set; }
        public string D24 { get; set; }
        public string D25 { get; set; }
        public string D26 { get; set; }
        public string D27 { get; set; }
        public string D28 { get; set; }
        public string D29 { get; set; }
        public string D30 { get; set; }
        public string D31 { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateTime { get; set; }
    }

    public class IT_CheckSheetForm_Weekly_Detail
    {
        [Key]
        public int WeeklyID { get; set; }
        public string ItemControlID { get; set; }
        public string ItemControlName { get; set; }
        public String ChecksheetID { get; set; }
        public string Standar { get; set; }
        public string W1 { get; set; }
        public string W2 { get; set; }
        public string W3 { get; set; }
        public string W4 { get; set; }
        public string W5 { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateTime { get; set; }
    }
    public class IT_CheckSheetForm_Monthly_Detail
    {
        [Key]
        public int MonthlyID { get; set; }
        public string ItemControlID { get; set; }
        public string ItemControlName { get; set; }
        public String ChecksheetID { get; set; }
        public string Standar { get; set; }
        public string M1 { get; set; }
        public string M2 { get; set; }
        public string M3 { get; set; }
        public string M4 { get; set; }
        public string M5 { get; set; }
        public string M6 { get; set; }
        public string M7 { get; set; }
        public string M8 { get; set; }
        public string M9 { get; set; }
        public string M10 { get; set; }
        public string M11 { get; set; }
        public string M12 { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateTime { get; set; }
    }

    public class IT_CheckSheetForm_Master_Item
    { 
        [Key]
        public string ControlItemID { get; set; }
        public string ControlItemName { get; set; }
        public string Standar { get; set; }
        public byte Type { get; set; }
        public byte Rack { get; set; }
    }

    public class IT_CheckSheetForm_InitialChecker
    {
        [Key]
        public int ID { get; set; }
        public string CheckerSequence { get; set; }
        public string CheckerBy { get; set; }
        public string CheckSheetID { get; set; }
        public string Interval { get; set; }
        public string Initial { get; set; }
    }

    public class IT_CheckSheetForm_Approval
    {
        [Key]
        public int ID { get; set; }
        public string Role { get; set; }
        public string UserHandler { get; set; }
    }

    // ------------------------------------------- Checksheet Maintenance Hardware and Software ------------------------------------------ //

    public class IT_CheckSheetMTC_Header
    {
        [Key]
        public int ID { get; set; }
        public string Dept {  get; set; }
        public string Section {  get; set;}
        public string NIK { get; set; }
        public string Nama {  get; set; }
        public string Spesifikasi {  get; set;}
        public string TahunMulaiPemakaian {  get; set; }
        public byte Status { get; set; }
        public string CreateBy { get; set; }
        public string CreateByName { get; set; }
        public DateTime? CreateTime {  get; set; }
        public string CheckedBy {  get; set; }
        public string CheckedByName { get; set; }
        public DateTime? CheckedTime {  get; set; }
        public string SignBy {  get; set; }
        public string SignByName { get; set; }
        public DateTime? SignTime {  get; set; }
        public string ApproveBy {  get; set; }
        public string ApproveByName { get; set; }
        public DateTime? ApproveTime {  get; set; }

    }
    public class IT_CheckSheetMTC_Detail { 
        [Key]
        public int ID { get; set; }
        public int HeaderID { get; set; }
        public int DevicesID { get; set; }
        public string DevicesName { get; set; }
        public byte Lepas { get; set; }
        public byte Pasang { get; set; }
        public byte Bersihkan { get; set; }
        public byte Checked { get; set; }
    }

    public class Tbl_CheckSheetMTC_Header
    {
        public int ID { get; set; }
        public string Dept {  get; set; }
        public string Section {  get; set;}
        public string Nama {  get; set; }
        public string Spesifikasi {  get; set;}
        public string TahunMulaiPemakaian {  get; set; }
        public string Status {  get; set; }
        public string CreateBy { get; set; }
        public string CreateTime {  get; set; }
        public string CheckedBy {  get; set; }
        public string CheckedTime {  get; set; }
        public string AcknowledgeBy {  get; set; }
        public string AcknowledgeTime {  get; set; }
        public string ApproveBy {  get; set; }
        public string ApproveTime {  get; set; }
        public string ViewButton { get; set; }
    }

    public class IT_ChecksSheetMTC_Tools
    {
        [Key]
        public int ID { get; set; }
        public string Type { get; set; }
        public string Tools { get; set; }
        public byte is_delete { get; set; }
    }

    public class IT_CheckSheetMTC_Devices
    {
        [Key]
        public int ID { get; set; }
        public string Device { get; set; }
        public int Tools_ID { get; set; }
        public byte is_delete { get; set; }
        public string Type { get; set; }
    }
    public class Tbl_CheckSheetMTC_Device_Checklist
    {
        public int ID { get; set; }
        public int NO { get; set; }
        public string Type { get; set; }
        public string Tools { get; set; }
        public string Device { get; set; }
        public byte Lepas { get; set; }
        public byte Pasang { get; set; }
        public byte Bersihkan { get; set; }
        public byte Check { get; set; }

    }

    public class FormCheckSheetConnection : DbContext
    {
        public DbSet<IT_CheckSheetForm_Header> IT_CheckSheetForm_Header { get; set; }
        public DbSet<V_IT_CheckSheetForm_Header> V_IT_CheckSheetForm_Header { get; set; }
        public DbSet<IT_CheckSheetForm_Daily_Detail> IT_CheckSheetForm_Daily_Detail { get; set; }
        public DbSet<IT_CheckSheetForm_Weekly_Detail> IT_CheckSheetForm_Weekly_Detail { get; set; }
        public DbSet<IT_CheckSheetForm_Monthly_Detail> IT_CheckSheetForm_Monthly_Detail { get; set; }
        public DbSet<IT_CheckSheetForm_Master_Item> IT_CheckSheetForm_Master_Item { get; set; }
        public DbSet<IT_CheckSheetForm_InitialChecker> IT_CheckSheetForm_InitialChecker { get; set; }
        public DbSet<IT_CheckSheetForm_Approval> IT_CheckSheetForm_Approval { get; set; }
        public DbSet<IT_CheckSheetMTC_Header> IT_CheckSheetMTC_Header { get; set; }
        public DbSet<IT_CheckSheetMTC_Detail> IT_CheckSheetMTC_Detail { get; set; }
        public DbSet<IT_ChecksSheetMTC_Tools> IT_ChecksSheetMTC_Tools { get; set; }
        public DbSet<IT_CheckSheetMTC_Devices> IT_CheckSheetMTC_Devices { get; set; }
        public FormCheckSheetConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}