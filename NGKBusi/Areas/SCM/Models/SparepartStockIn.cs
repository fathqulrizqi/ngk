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
    public class SparepartStockIn
    {

    }

    public class SCM_Sparepart_Upload_StockIn
    {
        [Key]
        public int ID { get; set; }
        public string ITEMID { get; set; }
        public int QTYReceived { get; set; }
        public string HeaderID { get; set; }
        public byte IsConfirm { get; set; }
    }

    public class SCM_Sparepart_Upload_StockIn_Header
    {
        [Key]
        public string HeaderID { get; set; }
        public string Remark { get; set; }
        public DateTime DateReceived { get; set; }
        public byte Status { get; set; }
    }

    public class V_SCM_Sparepart_Upload_StockIn_Detail
    {
        [Key]
        public int ID { get; set; }
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public int QTYReceived { get; set; }
        public string HeaderID { get; set; }
        public byte IsConfirm { get; set; }
    }


    public class Tbl_Sparepart_Upload_StockIn_Header
    {
        public string No { get; set; }
        public string HeaderID { get; set; }
        public string Remark { get; set; }
        public string DateReceived { get; set; }
        public string Status { get; set; }
        public string Action { get; set; }
    }
    public class Tbl_SCM_Sparepart_Upload_StockIn_Detail
    {
        [Key]
        public int ID { get; set; }
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public int QTYReceived { get; set; }
        public string HeaderID { get; set; }
        public string Action { get; set; }
    }


    public class SparepartStockInConnection : DbContext
    {
        public DbSet<V_Users_Active> V_Users_Active { get; set; }
        public DbSet<SCM_Sparepart_Upload_StockIn> SCM_Sparepart_Upload_StockIn { get; set; }
        public DbSet<SCM_Sparepart_Upload_StockIn_Header> SCM_Sparepart_Upload_StockIn_Header { get; set; }
        public DbSet<V_SCM_Sparepart_Upload_StockIn_Detail> V_SCM_Sparepart_Upload_StockIn_Detail { get; set; }
        public DbSet<SCM_Sparepart_Return_Detail> SCM_Sparepart_Return_Detail { get; set; }
        public DbSet<V_SCM_Sparepart_Return> V_SCM_Sparepart_Return_Detail { get; set; }
        public DbSet<V_SCM_Sparepart_Return_ItemList> V_SCM_Sparepart_Return_ItemList { get; set; }
        public DbSet<SCM_Sparepart_Return_Temp_Views> SCM_Sparepart_Return_Temp_Views { get; set; }
        public SparepartStockInConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}