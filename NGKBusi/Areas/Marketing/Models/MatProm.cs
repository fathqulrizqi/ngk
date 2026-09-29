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

namespace NGKBusi.Areas.Marketing.Models
{
    public class MatProm
    {
    }

    public class V_Marketing_MatProm_ItemMaster
    {
        [Key]
        public string ITEMID { get; set; }
        public int FirstStock { get; set; }
        public string ProductName { get; set; }
        public string ItemGroup { get; set; }
        public string ItemDescription { get; set; }
        public string ProductCategory { get; set; }
        public string ProCateName { get; set; }
        public string Section { get; set; }
        public int Stock { get; set; }
        public decimal Weight { get; set; }
        public byte IsActive { get; set; }
        public byte IsExternalItem { get; set; }
    }

    public class Tbl_Marketing_MatProm_ItemMaster
    {
        [Key]
        public string ITEMID { get; set; }
        public int FirstStock { get; set; }
        public string ProductName { get; set; }
        public string ItemDescription { get; set; }
        public string ItemGroup { get; set; }
        public string ProductCategory { get; set; }
        public string ProCateName { get; set; }
        public string Section { get; set; }
        public int Stock { get; set; }
        public decimal Weight { get; set; }
        public string Status { get; set; }
        public byte IsExternalItem { get; set; }
        public string Image { get; set; }
        public string EditButton { get; set; }
    }
    public class Marketing_MatProm_ItemMaster
    {
        [Key]
        public string ITEMID { get; set; }
        public int FirstStock { get; set; }
        public decimal? Weight { get; set; }
        public byte IsActive { get; set; }
        public byte IsExternalItem { get; set; }
        public string ProductName { get; set; }
        public string ProductCategory { get; set; }
        public string ItemGroup { get; set; }
        public string ItemDescription { get; set; }
        public string Section { get; set; }
        public string ProCateName { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateTime { get; set; }
    }
    public class Marketing_MatProm_AddressMaster
    {
        public int ID { get; set; }
        public string AddressCode { get; set; }
        public string AddressTitle { get; set; }
        public string AddressDetail { get; set; }
        public string AddressCity { get; set; }
        public string AddressPerson { get; set; }
        public string Phone { get; set; }

    }
    public class Marketing_MatProm_Template_Header
    {
        [Key]
        public int ID { get; set; }
        public string TemplateName { get; set; }
        public byte IsActive { get; set; }
    }
    public class Marketing_MatProm_Template_Detail
    {
        [Key]
        public int ID { get; set; }
        public int TemplateID { get; set; }
        public string ITEMID { get; set; }

    }
    public class Marketing_MatProm_Template_Views
    { 
        public int TemplateID { get; set; }
        public string TemplateName { get; set; }
        public string ITEMID { get; set; }

    }
    public class Tbl_Marketing_MatProm_Template_Detail
    {
        [Key]
        public int TemplateID { get; set; }
        public string ITEMID { get; set; }
        public string ItemName { get; set; }
        public string Tools { get; set; }
        public int No { get; set; }

    }

    public class Marketing_MatProm_Request_Temp
    {
        [Key]
        [Column(Order = 0)]
        public string ITEMID { get; set; }
        public int quantity { get; set; }
        [Key]
        [Column(Order = 1)]
        public string userRequest { get; set; }
        public string Notes { get; set; }
        public string formType { get; set; }
        [Key]
        [Column(Order = 2)]
        public int? TemplateID { get; set; }
    }
    public class Marketing_MatProm_Request_Header
    {
        [Key]
        public string RequestNo { get; set; }
        public string DistributorID { get; set; }
        public string Event { get; set; }
        public string Class { get; set; }
        public string Usage { get; set; }
        public string DivisionName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string SubSectionName { get; set; }
        public string CostName { get; set; }
        public string Status { get; set; }
        public string UserRequest { get; set; }
        public string FormType { get; set; }
        public DateTime Create_Time { get; set; }
        public string SignBy { get; set; }
        public DateTime? SignTime { get; set; }
        public string AcknowledgeBy { get; set; }
        public DateTime? AcknowledgeTime { get; set; }
        public byte AcknowledgeStatus { get; set; }
        public string AcknowledgeNotes { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public byte ApprovedStatus { get; set; }
        public string ApprovedNotes { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerifiedTime { get; set; }
        public byte VerifiedStatus { get; set; }
        public string VerifiedNotes { get; set; }
        public byte IsReject { get; set; }
        public byte IsChangeQty { get; set; }
        public string RejectBy { get; set; }
        public DateTime? RejectTime { get; set; }
        public string RejectNotes { get; set; }
        public byte IsCancel { get; set; }
        public string CancelBy { get; set; }
        public DateTime? CancelTime { get; set; }

    }

    public class Marketing_MatProm_Request_Detail
    {
        [Key]
        public int Id { get; set; }
        public string ITEMID { get; set; }
        public int Quantity { get; set; }
        public string RequestNo { get; set; }
        public int Qty_Realization { get; set; }
        public int Qty_Retur { get; set; }
        public string ReturNotes { get; set; }
        public byte IsReady { get; set; }
        public byte IsCancel { get; set; }
        public byte IsChangeQty { get; set; }
        public int Qty_Change_Temp { get; set; }
    }
    public class Marketing_MatProm_Request_Recipient
    {
        [Key]
        public int ID { get; set; }
        public string RequestNo { get; set; }
        public string Recipient { get; set; }
    }
    public class Marketing_MatProm_Request_Approval
    {
        [Key]
        public int ID { get; set; }
        public string RequestNo { get; set; }
        public string ApprovalUser { get; set; }
        public string ApprovalType { get; set; }
        public byte Status { get; set; }
    }
    public class Marketing_MatProm_Process_Task
    {
        [Key]
        public int ID { get; set; }
        public string AssignedBy { get; set; }
        public DateTime? AssignedTime { get; set; }
        public string TaskDescription { get; set; }
        public byte IsReject { get; set; }
        public string Notes { get; set; }
        public string RequestNo { get; set; }

    }

    public class Tbl_Marketing_MatProm_Request_Temp_Views
    {
        [Key]
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        public string UserRequest { get; set; }
        public int Stock { get; set; }
        public string TemplateID { get; set; }
    }
    public class Tbl_Marketing_MatProm_ItemList_Request
    {
        public int Id { get; set; }
        public string ITEMID { get; set; }
        public int Quantity { get; set; }
        public string RequestNotes { get; set; }
        public string RequestNo { get; set; }
        public string ProductName { get; set; }
        public string ItemGroup { get; set; }
        public int Qty_Realization { get; set; }
        public string ProductCategory { get; set; }
        public string Tools { get; set; }
        public string Qty_Retur { get; set; }
        public decimal Weight { get; set; }
        public byte IsChangeQty { get; set; }
    }
    public class Marketing_MatProm_Request_Temp_Views
    {
        [Key]
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        public string UserRequest { get; set; }
        public int Stock { get; set; }
        public string Image { get; set; }
        public int Id { get; set; }
        public int TemplateID { get; set; }
    }

    public class V_Marketing_MatProm_Request
    {
        [Key]
        public string RequestNo { get; set; }
        public string UserRequest { get; set; }
        public string FormType { get; set; }
        public string Event { get; set; }
        public string Class { get; set; }
        public string Usage { get; set;  }
        public string DivisionName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string SubSectionName { get; set; }
        public string CostName { get; set; }
        public string Status { get; set; }
        public DateTime Create_Time { get; set; }
        public string Name { get; set; }
        public string SignBy { get; set; }
        public DateTime? SignTime { get; set; }
        public string AcknowledgeBy { get; set; }
        public string AcknowledgeName { get;  set; }
        public DateTime? AcknowledgeTime { get; set; }
        public byte AcknowledgeStatus { get; set; }
        public string AcknowledgeNotes { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public string ApprovedByName { get; set; }
        public byte ApprovedStatus { get; set; }
        public string ApprovedNotes { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerifiedTime { get; set; }
        public string VerifiedByName { get; set; }
        public byte VerifiedStatus { get; set; }
        public string VerifiedNotes { get; set; }
        public byte IsReject { get; set; }
        public byte IsChangeQty { get; set; }
        public string RejectBy { get; set; }
        public DateTime? RejectTime { get; set; }
        public string RejectNotes { get; set; }
        public byte? useTemplate { get; set; }
        public string UserRequestName { get; set; }
    }

    public class V_Marketing_MatProm_Request_Detail
    {

        public int Id { get; set; }
        public string ITEMID { get; set; }
        public int Quantity { get; set; }
        public string RequestNotes { get; set; }
        public string RequestNo { get; set; }
        public string ProductName { get; set; }
        public string ItemGroup { get; set; }
        public int Qty_Realization { get; set; }
        public int Qty_Retur { get; set; }
        public string ProductCategory { get; set; }
        public decimal Weight { get; set; }
        public byte IsReady { get; set; }
        public byte IsChangeQty { get; set; }
    }
    public class Tbl_Marketing_MatProm_AddressMaster
    {
        public string AddressCode { get; set; }
        public string AddressTitle { get; set; }
        public string AddressDetail { get; set; }
        public string AddressCity { get; set; }
        public string AddressPerson { get; set; }
        public string Phone { get; set; }
        public string BtnEdit { get; set; }

    }
    public class Tbl_Marketing_Matprom_Request
    {
        [Key]
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string UserRequest { get; set; }
        public int Stock { get; set; }
        public string Image { get; set; }
        public int Id { get; set; }
    }
    public class Tbl_V_Marketing_MatProm_Request
    {
        public string RequestNo { get; set; }
        public string UserRequest { get; set; }
        public string FormType { get; set; }
        public string Event { get; set; }
        public string Class { get; set; }
        public string Usage { get; set; }
        public string DivisionName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string SubSectionName { get; set; }
        public string CostName { get; set; }
        public string Status { get; set; }
        public DateTime Create_Time { get; set; }
        public string Name { get; set; }
        public string SignBy { get; set; }
        public DateTime? SignTime { get; set; }
        public string AcknowledgeBy { get; set; }
        public string AcknowledgeName { get; set; }
        public DateTime? AcknowledgeTime { get; set; }
        public byte AcknowledgeStatus { get; set; }
        public string AcknowledgeNotes { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedTime { get; set; }
        public string ApprovedByName { get; set; }
        public byte ApprovedStatus { get; set; }
        public string ApprovedNotes { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime? VerifiedTime { get; set; }
        public string VerifiedByName { get; set; }
        public byte VerifiedStatus { get; set; }
        public string VerifiedNotes { get; set; }
        public byte IsReject { get; set; }
        public byte IsChangeQty { get; set; }
        public string RejectBy { get; set; }
        public DateTime? RejectTime { get; set; }
        public string RejectNotes { get; set; }
        public byte? useTemplate { get; set; }
        public string UserRequestName { get; set; }
        public string RequestTime { get; set; }
        public string Action { get; set; }
    }
    public class Tbl_Marketing_MatProm_Recipient
    {
        public string RecipientName { get; set; }
        public string RecipientAddress { get; set; }
    }

    public class Tbl_Marketing_MatProm_Report
    {
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public string Date { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string VerifiedTime { get; set; }
        public string Usage { get; set; }
        public string Recipient { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public string RequestNo { get; set; }
    }

    public class Users_Section_AX
    {
        [Key]
        public Int64 ID { get; set; }
        public string COSTNAME { get; set; }
        public string SECTIONTYPE { get; set; }
        public string SECTION { get; set; }
    }


    public class MatPromConnection : DbContext
    {
        public DbSet<Marketing_MatProm_Request_Temp> Marketing_MatProm_Request_Temp { get; set; }
        public DbSet<Marketing_MatProm_AddressMaster> Marketing_MatProm_AddressMaster { get; set; }
        public DbSet<Marketing_MatProm_Template_Header> Marketing_MatProm_Template_Header { get; set; }
        public DbSet<Marketing_MatProm_Template_Detail> Marketing_MatProm_Template_Detail { get; set; }
        public DbSet<Marketing_MatProm_Request_Header> Marketing_MatProm_Request_Header { get; set; }
        public DbSet<Marketing_MatProm_Request_Detail> Marketing_MatProm_Request_Detail { get; set; }
        public DbSet<Marketing_MatProm_Request_Recipient> Marketing_MatProm_Request_Recipient { get; set; }
        public DbSet<Marketing_MatProm_Request_Approval> Marketing_MatProm_Request_Approval { get; set; }
        public DbSet<Marketing_MatProm_Process_Task> Marketing_MatProm_Process_Task { get; set; }
        public DbSet<Marketing_MatProm_ItemMaster> Marketing_MatProm_ItemMaster { get; set; }
        public DbSet<V_Marketing_MatProm_ItemMaster> V_Marketing_MatProm_ItemMaster { get; set; }        
        public DbSet<V_Marketing_MatProm_Request> V_Marketing_MatProm_Request { get; set; }
        public DbSet<V_Marketing_MatProm_Request_Detail> V_Marketing_MatProm_Request_Detail { get; set; }
        public DbSet<Users_Section_AX> Users_Section_AX { get; set; }
        public MatPromConnection()
        {   
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}