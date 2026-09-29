using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;


namespace NGKBusi.Areas.FA.Models
{
    public class InvoiceNumber
    {
    }
    public class Sales_D365ImporForm_Sales
    {
        [Key]
        public int ID { get; set; }
        public string SalesOrder { get; set; }
        public decimal? SalesLineNumber { get; set; }
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
        public int? PackingQty { get; set; }
        public string YearDlv { get; set; }
        public string MonthDlv { get; set; }
        public string ProductCategory { get; set; }
        public string ProcateName { get; set; }
        public string Genuine { get; set; }
        public string Motor { get; set; }
        public string OriginalInvoiceNo { get; set; }
        public decimal? SalesPrice { get; set; }
        public decimal? DiscAmount { get; set; }
        public decimal? DiscPercent { get; set; }
        public decimal? ExchangeRate { get; set; }
        public string Currency { get; set; }
        public string SalesClass { get; set; }
        public decimal? AmountGrossCurrency { get; set; }
        public decimal? AmountDiscountCurrency { get; set; }
        public decimal? AmountNetCurrency { get; set; }
        public decimal? AmountGrossLocalCurrency { get; set; }
        public decimal? AmountDiscountLocalCurrency { get; set; }
        public decimal? AmountNetLocalCurrency { get; set; }
        public string Status_GIT { get; set; }
        public DateTime? Date_Inv { get; set; }
        public string Year_Inv { get; set; }
        public string Month_Inv { get; set; }
        public string City { get; set; }
        public string Month_Odr { get; set; }
        public string CommercialStatus { get; set; }
        public decimal? VAT { get; set; }
        public decimal? STDCost { get; set; }
        public decimal? GITCost { get; set; }
        public string PeriodDLV { get; set; }
        public string PeriodINV { get; set; }
        public int? DaysDueDateInv { get; set; }
        public string NPWP { get; set; }
        public string TaxInvoiceNo { get; set; }
        public string DescriptionforSalesReport { get; set; }
    }

    public class InvoiceNumberConnection : DbContext
    {
        public DbSet<Sales_D365ImporForm_Sales> sales_D365ImporForm_Sales { get; set; }
        public InvoiceNumberConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
        }
    }
}