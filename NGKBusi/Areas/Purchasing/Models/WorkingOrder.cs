using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using NGKBusi.Areas.WebService.Models;


namespace NGKBusi.Areas.Purchasing.Models
{
    public class WorkingOrder
    {
    }

    public class Purchasing_WorkingOrder_List
    {
        [Key]
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public string Number { get; set; }
        public string Vendor { get; set; }
        public string VendorName { get; set; }
        public string Subject { get; set; }
        public string NIK { get; set; }
        public string NIKName { get; set; }
        public DateTime Timestamps { get; set; }
    }

    public class Purchasing_WorkingOrder_Letter
    {
        [Key]
        public int ID { get; set; }
        public int IDList { get; set; }
        public string Number { get; set; }
        public string Vendor { get; set; }
        public string Attn { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal? Total { get; set; }
        public string PaymentTerm { get; set; } = string.Empty;
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string BudgetNo { get; set; }
        public string BudgetDesc { get; set; }
        public string Remark { get; set; } = string.Empty;
        public DateTime Timestamps { get; set; }
        public string NIKPrepared { get; set; } = string.Empty;
        public string NamePrepared { get; set; } = string.Empty;
        public DateTime? DatePrepared { get; set; }
        public string NIKApproval { get; set; } = string.Empty;
        public DateTime? DateApproval { get; set; }
    }

    public class Purchasing_WorkingOrder_Price
    {
        [Key]

        public int ID { get; set; }
        public string WO_Number { get; set; }
        public decimal Price_Before { get; set; }
        public decimal Discount { get; set; }
        public decimal Price_Total { get; set; }
        public DateTime Timestamps { get; set; }
    }

    public class V_Purchasing_WorkingOrder_Report
    {
        [Key]
        public int ListID { get; set; }

        public DateTime? Date { get; set; }
        public string Number { get; set; }
        public string Vendor { get; set; }
        public string VendorName { get; set; }
        public string Expr1 { get; set; }
        public string NIK { get; set; }
        public string NIKName { get; set; }

        public int? LetterID { get; set; } 

        public string Attn { get; set; }
        public string Ref { get; set; }
        public string Project { get; set; }
        public string Html { get; set; }

        public decimal? Total { get; set; }

        public string PaymentTerm { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string BudgetNo { get; set; }
        public string BudgetDesc { get; set; }
        public string Remark { get; set; }
        public string NIKPrepared { get; set; }
        public DateTime? DatePrepared { get; set; }
        public string NIKApproval { get; set; }
        public DateTime? DateApproval { get; set; }
        public string NamePrepared { get; set; }
        public DateTime? LastUpdated { get; set; }
    }


    public class WOConnection : DbContext
    {
        public DbSet<Purchasing_WorkingOrder_List> Purchasing_WorkingOrder_List { get; set; }
        public DbSet<Purchasing_WorkingOrder_Letter> Purchasing_WorkingOrder_Letter { get; set; }
        public DbSet<Purchasing_WorkingOrder_Price> Purchasing_WorkingOrder_Price { get; set; }
        public DbSet<V_Purchasing_WorkingOrder_Report> V_Purchasing_WorkingOrder_Report { get; set; }
        public WOConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}