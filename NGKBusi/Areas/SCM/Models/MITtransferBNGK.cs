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
    public class MITtransferBNGK
    {

    }

    public class V_D365_MIT
    {
        [Key]
        public Int64 ID { get; set; }
        public string ProductReceipt { get; set; }
        public string Periode { get; set; }
        public string InvoiceAccount { get; set; }
        public string VendorName { get; set; }
        public string PurchaseType { get; set; }
        public DateTime Date { get; set; }
    }

    public class T_AXInvoiceTransfer
    {
        [Key]
        public string Invoice { get; set; }
        public string PeriodEDI { get; set; }
        public DateTime DatePhysical { get; set; }
        public string Status { get; set; }
        public string InvoiceAccount { get; set; }
        public string PurchName { get; set; }
        public string ReceiveDocumentBy { get; set; }
        public DateTime? ReceiveDocumentTime { get; set; }
        public string ArriveBy { get; set; }
        public DateTime? ArriveTime { get; set; }
        public byte ArriveStatus { get; set; }
        public byte? IsJournal { get; set; }
        public string JournalNumber { get; set; }
        public string JournalTransferBy { get; set; }
        public DateTime? JournalTransferTime { get; set; }
        public byte? IsPostingJournal { get; set; }
        public string PostingJournalBy { get; set; }
        public DateTime? PostingJournalTime { get; set; }
        public byte IsFlexnetItem { get; set; }

    }




    public class Tbl_V_D365_MIT
    {
        public int No { get; set; }
        public string ProductReceipt { get; set; }
        public string Periode { get; set; }
        public string InvoiceAccount { get; set; }
        public string VendorName { get; set; }
        public string Date { get; set; }
        public string Button { get; set; }
    }
    public class Tbl_MIT_Received
    {
        public int No { get; set; }
        public string ProductReceipt { get; set; }
        public string Periode { get; set; }
        public string InvoiceAccount { get; set; }
        public string VendorName { get; set; }
        public string Date { get; set; }
        public string ReceivedBy { get; set; }
        public string JournalTransferNumber { get; set; }
        public string JournalTransferBy { get; set; }
        public string JournalTransferTime { get; set; }
        public string PostingJournalBy { get; set; }
        public string PostingJournalTime { get; set; }
        public string Button { get; set; }
    }
    public class Tbl_ProductList
    {
        public int No { get; set; }
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public decimal ReceivedQuantity { get; set; }
    }
    public class MITtransferBNGKConnection : DbContext
    {
        public DbSet<V_D365_MIT> V_D365_MIT { get; set; }
        public DbSet<T_AXInvoiceTransfer> T_AXInvoiceTransfer { get; set; }
        public MITtransferBNGKConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AxConnection"].ConnectionString;
        }
    }


}