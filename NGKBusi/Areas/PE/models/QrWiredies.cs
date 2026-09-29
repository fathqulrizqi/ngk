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
using NGKBusi.Areas.WebService.Models;

namespace NGKBusi.Areas.PE.Models
{
    public class PE_Wiredies_QR_Result
    {
        public PE_Wiredies_QR_Result()
        {
            this.created_at = DateTime.Now;
        }
        [Key]
        public int id { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string GimzCode { get; set; }
        public int Qty { get; set; }
        public string QRcode { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class PE_Wiredies_Printer
    {
        public int id { get; set; }
        public string printer_ip { get; set; }
        public string printer_name { get; set; }
        public string shared_printer { get; set; }
        public bool printer_status { get; set; }
        public int port { get; set; }
    }

    public class CheckingRequest
    {
        public string ip { get; set; }
        public int port { get; set; }
        public string shared_printer { get; set; }
    }

    public class WirediesConnection : DbContext
    {
        public DbSet<PE_Wiredies_QR_Result> PE_Wiredies_QR_Result { get; set; }
        public DbSet<PE_Wiredies_Printer> PE_Wiredies_Printer { get; set; }

        public WirediesConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}