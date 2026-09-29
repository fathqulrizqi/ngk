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
using System.Data.OleDb;
using System.Data;
using System.IO;

namespace NGKBusi.Areas.Sales.Models
{
    public class SalesOrder
    {

    }

    public static class Utility
    {
    }
        
    public class Tbl_Sales_EDI_Suzuki
    {
        public string PONo { get; set; }
        public string ITEMID { get; set; }
        public string ItemName { get; set; }
        public int QTY { get; set; }
        public string DeliveryDate { get; set; }
    }

    public class Sales_EDI_Suzuki_Temp
    {
        public int ID { get; set; }
        public string PartNo { get; set; }
        public string DeliveryShipDate { get; set; }
        public int DeliveryQTY { get; set; }
        public string SlipNo { get; set; }
        public string CustomerCode { get; set; }
        public DateTime? deliveryDate { get; set; }
    }

    public class V_Sales_EDI
    {
        [Key]
        public int ID { get; set; }
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public int DeliveryQTY { get; set; }        
        public string PoNo { get; set; }
        public DateTime? DeliveryShipDate { get; set; }
    }

    public class V_Sales_EDI_HPM
    {
        [Key]
        public int ID { get; set; }
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public int DeliveryQTY { get; set; }
        public string PoNo { get; set; }
        public DateTime? DeliveryShipDate { get; set; }
    }

    public class SalesOrderConnection : DbContext
    {
        public DbSet<Sales_EDI_Suzuki_Temp> Sales_EDI_Suzuki_Temp { get; set; }
        public DbSet<V_Sales_EDI> V_Sales_EDI { get; set; }
        public DbSet<V_Sales_EDI_HPM> V_Sales_EDI_HPM { get; set; }
        public SalesOrderConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}