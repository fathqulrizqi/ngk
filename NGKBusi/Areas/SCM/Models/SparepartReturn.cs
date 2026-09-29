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

namespace NGKBusi.Areas.SCM.Models
{
    public class SparepartReturn
    {

    }
    public class V_Users_Active
    {
        [Key]
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string DivisionID { get; set; }
        public string DivisionName { get; set; }
        public string DeptID { get; set; }
        public string DeptName { get; set; }
        public string SectionID { get; set; }
        public string SectionName { get; set; }
        public string SubSectionID { get; set; }
        public string SubSectionName { get; set; }
        public string CostID { get; set; }
        public string CostName { get; set; }
        public string TitleID { get; set; }
        public string TitleName { get; set; }
        public string PositionID { get; set; }
        public string PositionName { get; set; }
        public string Status { get; set; }
        public string RoleName { get; set; }
        public DateTime? Birthday { get; set; }
    }
    public class SCM_Sparepart_Return_Temp
    {
        [Key]
        [Column(Order = 0)]
        public string ITEMID { get; set; }
        public int quantity { get; set; }
        [Key]
        [Column(Order = 1)]
        public string userReturn { get; set; }
    }
    public class SCM_Sparepart_Return_Header
    {
        [Key]
        //public int Id { get; set; }
        public string ReturnNo { get; set; }
        public string UserReturn { get; set; }
        public string Remark { get; set; }
        public string DivisionName { get; set; }
        public string DeptName { get; set; }
        public string SectionName { get; set; }
        public string SubSectionName { get; set; }
        public string CostName { get; set; }
        public byte Status { get; set; }
        public DateTime Create_Time { get; set; }
        public DateTime? ApproveTime { get; set; }
        public DateTime? ReceivedTime { get; set; }
        public byte? IsCancel { get; set; }
        public DateTime? CancelTime { get; set; }
        public string CancelBy { get; set; }
        public string RequestNo { get; set; }
    }
    public class SCM_Sparepart_Return_Detail
    {
        [Key]
        public int Id { get; set; }
        public string ITEMID { get; set; }
        public int Quantity { get; set; }
        public int Qty_Realization { get; set; }
        public string RequestNo { get; set; }
        public string ReturnNo { get; set; }
        public string ReturnNotes { get; set; }
        public byte IsAccept { get; set; }
        public byte IsReject { get; set; }
        public byte IsCancel { get; set; }
        public DateTime? Create_Time { get; set; }
        public string Create_By { get; set; }
        public string Status { get; set; }

    }
    //public class V_SCM_Sparepart_Return
    //{
    //    public string userReturn { get; set; }
    //    [Key]
    //    public string ReturnNo { get; set; }
    //    public string Remark { get; set; }
    //    public string DivisionName { get; set; }
    //    public string DeptName { get; set; }
    //    public string SectionName { get; set; }
    //    public string SubSectionName { get; set; }
    //    public string CostName { get; set; }
    //    public byte Status { get; set; }
    //    public DateTime Create_Time { get; set; }
    //    public string Name { get; set; }
    //}
    public class V_SCM_Sparepart_Return_ItemList
    {

        public int Id { get; set; }
        public string ITEMID { get; set; }
        public int Quantity { get; set; }
        public string RequestNo { get; set; }
        public string ProductName { get; set; }
        public string ItemGroup { get; set; }
        public int Qty_Realization { get; set; }
        public string ProductCategory { get; set; }
        public string Tools { get; set; }
        public string ItemStatus { get; set; }
        public string Qty_Retur { get; set; }
        public string ReturnNotes { get; set; }
    }
    public class V_SCM_Sparepart_Return
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ITEMID { get; set; }
        public string CostName { get; set; }
        public int Quantity { get; set; }
        public string RequestNo { get; set; }
        public string ReturnNo { get; set; }
        public string ProductName { get; set; }
        public string ItemGroup { get; set; }
        public string ReturnNotes { get; set; }
        public string ProductCategory { get; set; }
        public string Section { get; set; }
        public byte IsAccept { get; set; }
        public byte IsReject { get; set; }
        public byte IsCancel { get; set; }
        public DateTime? Create_Time { get; set; }
        public string Create_By { get; set; }
        public string Status { get; set; }
    }
    public class SCM_Sparepart_Return_Temp_Views
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
    public class Tbl_SCM_Sparepart_Return_History
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ITEMID { get; set; }
        public string CostName { get; set; }
        public int Quantity { get; set; }
        public string RequestNo { get; set; }
        public string ReturnNo { get; set; }
        public string ProductName { get; set; }
        public string ItemGroup { get; set; }
        public string ReturnNotes { get; set; }
        public string ProductCategory { get; set; }
        public byte IsAccept { get; set; }
        public byte IsReject { get; set; }
        public byte IsCancel { get; set; }
        public string Create_Time { get; set; }
        public string Create_By { get; set; }
        public string Status { get; set; }
    }

    public class Tbl_SCM_Sparepart_Return_Request
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ITEMID { get; set; }
        public string CostName { get; set; }
        public int Quantity { get; set; }
        public string RequestNo { get; set; }
        public string ReturnNo { get; set; }
        public string ProductName { get; set; }
        public string ItemGroup { get; set; }
        public string ReturnNotes { get; set; }
        public string ProductCategory { get; set; }
        public string Section { get; set; }
        public byte IsAccept { get; set; }
        public byte IsReject { get; set; }
        public byte IsCancel { get; set; }
        public string Create_Time { get; set; }
        public string Create_By { get; set; }
        public string Status { get; set; }
    }

    public class SparepartReturnConnection : DbContext
    {
        public DbSet<V_Users_Active> V_Users_Active { get; set; }
        public DbSet<SCM_Sparepart_Return_Temp> SCM_Sparepart_Return_Temp { get; set; }
        public DbSet<V_SCM_Sparepart_Return> V_SCM_Sparepart_Return { get; set; }
        public DbSet<SCM_Sparepart_Return_Header> SCM_Sparepart_Return_Header { get; set; }
        public DbSet<SCM_Sparepart_Return_Detail> SCM_Sparepart_Return_Detail { get; set; }
        public DbSet<V_SCM_Sparepart_Return_ItemList> V_SCM_Sparepart_Return_ItemList { get; set; }
        public DbSet<SCM_Sparepart_Return_Temp_Views> SCM_Sparepart_Return_Temp_Views { get; set; }
        public SparepartReturnConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}