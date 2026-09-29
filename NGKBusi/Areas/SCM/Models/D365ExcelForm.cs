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
    public class D365ExcelForm
    {

    }

    public class SCM_D365ImporForm_ProductReceipt
    {
        [Key]
        public int ID { get; set; }
        public string PurchaseOrder { get; set; }
        public string VendorName { get; set; }
        public string Currency { get; set; }
        public string PurchaseType { get; set; }
        public string InvoiceAccount { get; set; }
        public string OrderAccount { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string ProductReceipt { get; set; }
        public string Site { get; set; }
        public string Warehouse { get; set; }
        public string InternalProductReceipt { get; set; }
        public string OuterPackageNumber { get; set; }
        public string LineNumber { get; set; }
        public string Item { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public decimal Ordered { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal PriceUnit { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal Value { get; set; }
        public string ProductName { get; set; }
        public string DescriptionPurchaseReport { get; set; }
        public string LocalImport { get; set; }
        public string ProCateName { get; set; }
        public string SearchNameAll { get; set; }
        public string SearchNameReleased { get; set; }
        public string ItemGroup { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public decimal Factor { get; set; }
        public decimal Denominator { get; set; }
        public decimal Numerator { get; set; }
        public decimal QtyConversion { get; set; }
        public decimal? UnitPrice { get; set; }
        public string RelUnRel { get; set; }
        public decimal ExchangeRate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal DiscPercent { get; set; }
        public string CostLedgerVoucher { get; set; }
        public string InternalInvoiceId { get; set; }
        public string LedgerVoucher { get; set; }
        public string Periode { get; set; }
        public decimal Standard_Cost { get; set; }
        public decimal Standard_Cost_Beneran { get; set; }
        public DateTime? UploadTime { get; set; }
        public byte ConfirmReceivedStatus { get; set; }
    }

    public class SCM_D365ImporForm_PurchaseBI
    {
        [Key]
        public int ID { get; set; }
        public string Company { get; set; }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceID { get; set; }
        public int Line_No { get; set; }
        public string PONo { get; set; }
        public int POLineNo { get; set; }
        public string ItemGroup { get; set; }
        public string ItemId { get; set; }
        public string Text { get; set; }
        public decimal Quantity { get; set; }
        public string PurchaseUnit { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public decimal CompanyCurrencyAmount { get; set; }
        public string PurchaseReason { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string SectionCode { get; set; }
        public string SectionName { get; set; }
        public string Voucher { get; set; }
        public string CostingVoucher { get; set; }
        public string PRNo { get; set; }
        public string CustomerRequisition { get; set; }
    }

    public class SCM_D365ImporForm_PurchaseBI_trial
    {
        [Key]
        public int ID { get; set; }
        public string Company { get; set; }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceID { get; set; }
        public int Line_No { get; set; }
        public string PONo { get; set; }
        public int POLineNo { get; set; }
        public string ItemGroup { get; set; }
        public string ItemId { get; set; }
        public string Text { get; set; }
        public decimal Quantity { get; set; }
        public string PurchaseUnit { get; set; }
        public decimal PurchasePrice { get; set; }
        public int Discount { get; set; }
        public int DiscountPercent { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public decimal CompanyCurrencyAmount { get; set; }
        public string PurchaseReason { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string SectionCode { get; set; }
        public string SectionName { get; set; }
        public string Voucher { get; set; }
        public string CostingVoucher { get; set; }
        public string PRNo { get; set; }
        public string CustomerRequisition { get; set; }
    }
    public class SCM_D365ImporForm_PurchaseLines
    {
        [Key]
        public int ID { get; set; }
        public string Purchase_order { get; set; }
        public string Vendor_account { get; set; }
        public int Line_number { get; set; }
        public string Item_number { get; set; }
        public string Product_name { get; set; }
        public string Description { get; set; }
        public string PRNo { get; set; }
        public string Line_status { get; set; }
        public decimal Quantity { get; set; }
        public decimal Deliver_reminder { get; set; }
        public string Unit { get; set; }
        public decimal Unit_price { get; set; }
        public decimal Discount { get; set; }
        public decimal Net_amount { get; set; }
        public string Created_by { get; set; }
        public DateTime Created_at { get; set; }
    }

    public class SCM_D365ImporForm_StockManagement
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
        public decimal BeginningBalanceAmount { get; set; }
        public decimal TransferQtyPlus { get; set; }
        public decimal TransferAmountPlus { get; set; }
        public decimal ProductionQtyPlus { get; set; }
        public decimal ProductionAmountPlus { get; set; }
        public decimal PurchasedQtyPlus { get; set; }
        public decimal PurchaseAmountPlus { get; set; }
        public decimal AdjustQtyPlus { get; set; }
        public decimal AdjustAmountPlus { get; set; }
        public decimal CountingQtyPlus { get; set; }
        public decimal CountingAmountPlus { get; set; }
        public decimal TransferQtyMinus { get; set; }
        public decimal TransferAmountMinus { get; set; }
        public decimal SalesQtyMinus { get; set; }
        public decimal SalesAmountMinus { get; set; }
        public decimal ConsumptionQtyMinus { get; set; }
        public decimal ConsumptionAmountMinus { get; set; }
        public decimal AdjustQtyMinus { get; set; }
        public decimal AdjustAmountMinus { get; set; }
        public decimal CountingQtyMinus { get; set; }
        public decimal CountingAmountMinus { get; set; }
        public decimal EndingBalanceQuantity { get; set; }
        public decimal EndingBalanceAmount { get; set; }
        public string Procate { get; set; }
        public string ProductName { get; set; }
        public string DescriptionFromPurchaseReport { get; set; }
        public string DescriptionFromSalesReport { get; set; }
    }
    public class D365ExcelFormConnection : DbContext
    {
        public DbSet<V_Users_Active> V_Users_Active { get; set; }
        public DbSet<SCM_D365ImporForm_ProductReceipt> SCM_D365ImporForm_ProductReceipt { get; set; }
        public DbSet<SCM_D365ImporForm_PurchaseBI> SCM_D365ImporForm_PurchaseBI { get; set; }
        public DbSet<SCM_D365ImporForm_PurchaseLines> SCM_D365ImporForm_PurchaseLines { get; set; }
        public DbSet<SCM_D365ImporForm_PurchaseBI_trial> SCM_D365ImporForm_PurchaseBI_trial { get; set; }
        public DbSet<SCM_D365ImporForm_StockManagement> SCM_D365ImporForm_StockManagement { get; set; }
        public D365ExcelFormConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
        }
    }
}