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


namespace NGK_AX.Models
{
    public class NGKAx
    {

    }
    public class SCM_D365ImporForm_ProductReceipt   
    {
        [Key]
        public int ID { get; set; }
        public string ProductReceipt { get; set; }
        public string Site { get; set; }
        public string Warehouse { get; set; }
        public string InternalProductReceipt { get; set; }
        public string ItemGroup { get; set; }
        public string Item { get; set; }
        public string ProductName { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public DateTime Date { get; set; }
        public byte ConfirmReceivedStatus { get; set; }
    }

    public class Sales_D365UploadLog
    {
        [Key]
        public int ID { get; set; }
        public string FileName { get; set; }
        public string UploadType { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadTime { get; set; }
        public int RowCount { get; set; }
    }
    
    public class NGK_AXConnection : DbContext
    {
        public DbSet<SCM_D365ImporForm_ProductReceipt> SCM_D365ImporForm_ProductReceipt { get; set; }
        public DbSet<Sales_D365UploadLog> Sales_D365UploadLog { get; set; }

        public NGK_AXConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
        }
    }
}