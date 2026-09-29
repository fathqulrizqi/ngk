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

namespace NGKBusi.Areas.FA.Models
{
    public class D365ExcelForm
    {

    }
    public class FA_D365ImporForm_StandardCost
    {
        [Key]
        public int ID { get; set; }
        public string Company { get; set; }
        public string ItemNumber { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public decimal PriceUnit { get; set; }
        public decimal At { get; set; }
        public DateTime? ActivateDate { get; set; }
        public DateTime? ExtractDate { get; set; }
        public string UploadBy { get; set; }
        public DateTime? UploadTime { get; set; }
    }

    public class FA_D365ImporForm_StockManagement
    {
        [Key]
        public int ID { get; set; }
        public string ItemGroup { get; set; }
        public DateTime? Date { get; set; }
        public string Warehouse { get; set; }
        public string Item { get; set; }
        public string Batch { get; set; }
        public string InventoryUnit { get; set; }
        public decimal BeginningBalanceQty { get; set; }
        public decimal TransferQtyPlus { get; set; }
        public decimal ProductionQtyPlus { get; set; }
        public decimal PurchasedQtyPlus { get; set; }
        public decimal AdjustQtyPlus { get; set; }
        public decimal CountingQtyPlus { get; set; }
        public decimal TransferQtyMinus { get; set; }
        public decimal SalesQtyMinus { get; set; }
        public decimal ConsumptionQtyMinus { get; set; }
        public decimal AdjustQtyMinus { get; set; }
        public decimal CountingQtyMinus { get; set; }
        public decimal EndingBalanceQuantity { get; set; }
        public string Procate { get; set; }
        public string ProductName { get; set; }
        public string DescriptionFromPurchaseReport { get; set; }
        public string DescriptionFromSalesReport { get; set; }
        public DateTime? Periode { get; set; }
    }

    public class D365ExcelFormConnection : DbContext
    {
        public DbSet<FA_D365ImporForm_StandardCost> FA_D365ImporForm_StandardCost { get; set; }
        public DbSet<FA_D365ImporForm_StockManagement> FA_D365ImporForm_StockManagement { get; set; }
        public D365ExcelFormConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
        }
    }
}