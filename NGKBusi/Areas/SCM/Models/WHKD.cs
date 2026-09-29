using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.SCM.Models
{
    public class WHKD
    {
    }

    public class SCM_WHKD_Checker_Header
    {
        [Key]
        public int ID { get; set; }
        public string NoTrans { get; set; }
        public string SendingCard { get; set; }
        public int TotalQuantity { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
    }
    public class SCM_WHKD_Checker_Detail
    {
        [Key]
        public int ID { get; set; }
        public int CheckerID { get; set; }
        public string NoTrans { get; set; }
        public string ScanItem { get; set; }
        public int Quantity { get; set; }
        public byte IsDelete { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
    }
    public class Tbl_SCM_WHKD_Checker_Header
    {
        public int ID { get; set; }
        public string NoTrans { get; set; }
        public string SendingCard { get; set; }
        public int TotalQuantity { get; set; }
        public string CreateTime { get; set; }
        public string CreateBy { get; set; }
    }
    public class Tbl_SCM_WHKD_Checker_Detail
    {
        [Key]
        public int ID { get; set; }
        public int No { get; set; }
        public int CheckerID { get; set; }
        public string NoTrans { get; set; }
        public string ScanItem { get; set; }
        public int Quantity { get; set; }
        public string CreateTime { get; set; }
        public string CreateBy { get; set; }
        public int IsNG { get; set; }
        public string btnAction { get; set; }
    }

    public class SCM_WHKD_DeliveryNote_Header
    { 
        public int ID { get; set; }
        public string NoTrans { get; set; }
        public string SupplierName { get; set; }
        public string Address { get; set; }
        public string VehicleNumberPlate { get; set; }
        public string RecipientName { get; set; }
        public byte IsDelete { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateBy { get; set; }
        public byte IsCommit { get; set; }
        public string CommitBy { get; set; }
        public DateTime? CommitTime { get; set; }
    }
    public class SCM_WHKD_DeliveryNote_Detail
    {
        public int ID { get; set; }
        public int HeaderID { get; set; }
        public string NoTrans { get; set; }
        public string ItemName { get; set; }
        public int Qty { get; set; }
        public string Description { get; set; }        
        public string Type { get; set; }
        public byte IsDelete { get; set; }
        public DateTime CreateTime { get; set; }
        public string CreateBy { get; set; }
    }

    public class Tbl_SCM_WHKD_DeliveryNote_Header
    {
        public int ID { get; set; }
        public string NoTrans { get; set; }
        public string SupplierName { get; set; }
        public string Address { get; set; }
        public int TotalItem { get; set; }
        public byte IsDelete { get; set; }
        public string CreateTime { get; set; }
        public string CreateBy { get; set; }
        public string BtnPrint { get; set; }
        public string Status { get; set; }
    }

    public class WHKDConnection : DbContext
    {
        public DbSet<SCM_WHKD_Checker_Header> SCM_WHKD_Checker_Header { get; set; }
        public DbSet<SCM_WHKD_Checker_Detail> SCM_WHKD_Checker_Detail { get; set; }
        public DbSet<SCM_WHKD_DeliveryNote_Header> SCM_WHKD_DeliveryNote_Header { get; set; }
        public DbSet<SCM_WHKD_DeliveryNote_Detail> SCM_WHKD_DeliveryNote_Detail { get; set; }
        public WHKDConnection()
        {

            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}