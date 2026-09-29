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

namespace NGKBusi.Areas.Sales.Models
{
    public class D365ExcelForm
    {
        
    }

    public class Sales_D365ImporForm_OrderLines
    {
        [Key]
        public int ID { get; set; }
        public DateTime? ShipDate { get; set; }
        public DateTime? RequestedReceiptDate { get; set; }
        public DateTime? RequestedShipDate { get; set; }
        public string Customer { get; set; }
        public string DeliveryName { get; set; }
        public string SalesOrder { get; set; }
        public string LineStatus { get; set; }
        public string ItemNumber { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }  
        public decimal Discount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal NetAmount { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDateAndTime { get; set; }
        public string CustomerReference { get; set; }
        public string ItemSalesTaxGroup { get; set; }
        public string NTT_NonCommercial { get; set; }
        public string SalesTaxGroup { get; set; }
        public int? LineNumber { get; set; }
        public string DescriptionForSalesReport { get; set; }
        public string Text { get; set; }
    }

    public class Sales_D365ImporForm_SalesBI
    {
        [Key]
        public int ID { get; set; }
        public string Company { get; set; }
        public DateTime? InvoiceDateYYMM { get; set; }
        public DateTime? InvoiceDateYYYYMMDD { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? SOCreateDate { get; set; }
        public DateTime? RequestReceiptDate { get; set; }
        public string Invoice { get; set; }
        public string SONo { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerSearchName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Division { get; set; }
        public string DivisionName { get; set; }
        public string ProductCategory { get; set; }
        public string ProductCategoryName { get; set; }
        public string Country { get; set; }
        public string Area { get; set; }
        public string SalesPerson { get; set; }
        public string SalesPersonName { get; set; }
        public string BusinessType { get; set; }
        public string BusinessTypeName { get; set; }
        public string Design { get; set; }
        public string Brand { get; set; }
        public string ProductCateMajor { get; set; }
        public string ProductCateMajorName { get; set; }
        public string ProductCateMiddle { get; set; }
        public string ProductCateMiddleName { get; set; }
        public string ProductCateMinor { get; set; }
        public string ProductCateMinorName { get; set; }
        public string PackagingCountry { get; set; }
        public int Quantity { get; set; }
        public string Currency { get; set; }
        public decimal PriceByIVCurrency { get; set; }
        public decimal AmountByIVCurrency { get; set; }
        public decimal PriceByLocalCurrency { get; set; }
        public decimal AmountByLocalCurrency { get; set; }
        public string RecID { get; set; }
        public string CustomerReference { get; set; }
        public string DescriptionForSalesReport { get; set; }
        public string ItemGroup { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal Tax { get; set; }
        public decimal NetAmount { get; set; }
        public string TaxInvoiceId { get; set; }
        public string NPWP { get; set; }
        public decimal LineDisc { get; set; }
        public string InvoiceAddress { get; set; }
        public string ExternalItemId { get; set; }
    }

    public class Sales_D365ImporForm_Sales
    {
        [Key]
        public int ID { get; set; }
        public string SalesOrder { get; set; }
        public decimal SalesLineNumber { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerReference { get; set; }
        public string ExternalItemNumber { get; set; }
        public string ItemID { get; set; }
        public string ProductName { get; set; }
        public string ItemDescription { get; set; }
        public string SearchNameAll { get; set; }
        public string SearchNameReleased { get; set; }
        public string SPType { get; set; }
        public string VehicleID { get; set; }
        public string MasterVehicle { get; set; }
        public string ItemGroup { get; set; }
        public string PackingSlipID { get; set; }
        public DateTime? PackingSlipDate { get; set; }
        public int PackingQty { get; set; }
        public string YearDlv { get; set; }
        public string MonthDlv { get; set; }
        public string ProductCategory { get; set; }
        public string ProcateName { get; set; }
        public string Genuine { get; set; }
        public string Motor { get; set; }
        public string OriginalInvoiceNo { get; set; }
        public decimal SalesPrice { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal DiscPercent { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Currency { get; set; }
        public string SalesClass { get; set; }
        public decimal AmountGrossCurrency { get; set; }
        public decimal AmountDiscountCurrency { get; set; }
        public decimal AmountNetCurrency { get; set; }
        public decimal AmountGrossLocalCurrency { get; set; }
        public decimal AmountDiscountLocalCurrency { get; set; }
        public decimal AmountNetLocalCurrency { get; set; }
        public string Status_GIT { get; set; }
        public DateTime? Date_Inv { get; set; }
        public string Year_Inv { get; set; }
        public string Month_Inv { get; set; }
        public string City { get; set; }
        public string Month_Odr { get; set; }
        public string CommercialStatus { get; set; }
        public decimal VAT { get; set; }
        public decimal STDCost { get; set; }
        public decimal GITCost { get; set; }
        public string PeriodDLV { get; set; }
        public string PeriodINV { get; set; }
        public int DaysDueDateInv { get; set; }
        public string NPWP { get; set; }
        public string TaxInvoiceNo { get; set; }
        public string DescriptionforSalesReport { get; set; }
    }

    public class Sales_D365ImporForm_SalesPackingQuantity
    {
        [Key]
        public int ID { get; set; }
        public string SalesOrder { get; set; }
        public decimal SalesLineNumber { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerReference { get; set; }
        public string ExternalItemNumber { get; set; }
        public string ItemID { get; set; }
        public string ProductName { get; set; }
        public string ItemDescription { get; set; }
        public string DescriptionforSalesReport { get; set; }
        public string SearchNameAll { get; set; }
        public string SearchNameReleased { get; set; }
        public string SPType { get; set; }
        public string VehicleID { get; set; }
        public string ItemGroup { get; set; }
        public string PackingSlipID { get; set; }
        public DateTime? PackingSlipDeliveryDate { get; set; }
        public int PackingQty { get; set; }
        public string YearDlv { get; set; }
        public string MonthDlv { get; set; }
        public string ProductCategory { get; set; }
        public string ProcateName { get; set; }
        public string Genuine { get; set; }
        public string Motor { get; set; }
        public string InvoiceNo { get; set; }
        public decimal SalesPrice { get; set; }
        public decimal DiscAmount { get; set; }
        public decimal DiscPercent { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Currency { get; set; }
        public string SalesClass { get; set; }
        public decimal AmountGrossCurrency { get; set; }
        public decimal AmountDiscountCurrency { get; set; }
        public decimal AmountNetCurrency { get; set; }
        public decimal AmountGrossLocalCurrency { get; set; }
        public decimal AmountDiscountLocalCurrency { get; set; }
        public decimal AmountNetLocalCurrency { get; set; }
        public string Status_GIT { get; set; }
        public DateTime? Date_Inv { get; set; }
        public string Year_Inv { get; set; }
        public string Month_Inv { get; set; }
        public string City { get; set; }
        public string Month_Odr { get; set; }
        public string CommercialStatus { get; set; }
        public decimal GITPrice { get; set; }
        public decimal GITCost { get; set; }
    }
    public class V_Sales_Back_Order
    {
        [Key]
        public string SalesOrder { get; set; }
        public string ItemNumber { get; set; }
        public string ProductName { get; set; }
        public string Customer { get; set; }
        public string CustomerReference { get; set; }
        public DateTime RequestedShipDate { get; set; }
        public decimal qty_order { get; set; }
        public int qty_packing { get; set; }
        public decimal remaining_qty { get; set; }
    }
    public class Tbl_V_Sales_Back_Order
    {
        public int No { get; set; }
        public string SalesOrder { get; set; }
        public string ItemNumber { get; set; }
        public string ProductName { get; set; }
        public string Customer { get; set; }
        public string CustomerReference { get; set; }
        public string RequestedShipDate { get; set; }
        public decimal qty_order { get; set; }
        public int qty_packing { get; set; }
        public decimal remaining_qty { get; set; }
    }

    public class SCM_D365_Pickinglist
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string PickingRoute { get; set; }
        public string ItemNumber { get; set; }
        public string HandlingStatus { get; set; }
        public string Description { get; set; }
        public int OrderQuantity { get; set; }
        public string OrderUnit { get; set; }
        public int CWQuantity { get; set; }
        public string CWUnit { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int Quantity { get; set; }
        public string Picked { get; set; }
    }

    public class Tbl_SCM_D365_Pickinglist
    {
        public int Id { get; set; }
        public int No { get; set; }
        public string PickingRoute { get; set; }
        public string ItemNumber { get; set; }
        public string HandlingStatus { get; set; }
        public string Description { get; set; }
        public int OrderQuantity { get; set; }
        public string OrderUnit { get; set; }
        public int CWQuantity { get; set; }
        public string CWUnit { get; set; }
        public string DeliveryDate { get; set; }
        public int Quantity { get; set; }
        public string Picked { get; set; }
    }

    /// <summary>
    /// Model untuk menyimpan history/log upload Excel files
    /// </summary>
    public class Sales_D365ExcelUploadLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        /// <summary>
        /// Tipe upload (OrderLines, SalesBI, Sales, SalesPackingQuantity, dll)
        /// </summary>
        public string UploadType { get; set; }

        /// <summary>
        /// Nama file yang diupload
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Jumlah baris data yang berhasil diimpor
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Tanggal dan waktu upload
        /// </summary>
        public DateTime UploadDateTime { get; set; }

        /// <summary>
        /// User ID yang melakukan upload (NIK)
        /// </summary>
        public string UploadedBy { get; set; }

        /// <summary>
        /// Status upload (Success, Failed, Partial)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Pesan error jika ada (optional)
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class D365ExcelFormConnection : DbContext
    {
        public DbSet<Sales_D365ImporForm_OrderLines> Sales_D365ImporForm_OrderLines { get; set; }
        public DbSet<Sales_D365ImporForm_SalesBI> Sales_D365ImporForm_SalesBI { get; set; }
        public DbSet<Sales_D365ImporForm_Sales> Sales_D365ImporForm_Sales { get; set; }
        public DbSet<Sales_D365ImporForm_SalesPackingQuantity> Sales_D365ImporForm_SalesPackingQuantity { get; set; }
        public DbSet<V_Sales_Back_Order> V_Sales_Back_Order { get; set; }
        public DbSet<SCM_D365_Pickinglist> SCM_D365_Pickinglist { get; set; }
        public DbSet<Sales_D365ExcelUploadLog> Sales_D365ExcelUploadLog { get; set; }
        
        public D365ExcelFormConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
        }
    }
}