using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.Sales.Models
{
    public class Distributor
    {
    }
    public class Sales_Distributor_PO_Master_Part
    {
        [Key]
        public int ID { get; set; }
        public string Product_Name { get; set; }
        public string Vehicle_ID { get; set; }
        public string S_P_Type { get; set; }
        public string Item_ID { get; set; }
        public string Category { get; set; }
        public int? Price { get; set; }
        public int HashCode { get; set; }
    }
    public class Sales_Distributor_PO_Header
    {
        [Key]
        public int ID { get; set; }
        public string Niterra_PO { get; set; }
        public string Cust_Code { get; set; }
        public string Cust_Name { get; set; }
        public DateTime? Date { get; set; }
        public string Distro_PO { get; set; }
        public string Niterra_SO { get; set; }
        public int? Approval { get; set; }
        public int? Approval_Sub { get; set; }
        public string Created_By { get; set; }
        public DateTime? Created_At { get; set; }
    }

    public class Sales_Distributor_PO_Line
    {
        [Key]
        public int ID { get; set; }
        public int Header_ID { get; set; }
        public string Vehicle_Category { get; set; }
        public string S_P_Type { get; set; }
        public string Part_Name { get; set; }
        public string Part_Number { get; set; }
        public int QTY { get; set; }
        public int hash { get; set; }
    }

    public class DistributorConnection : DbContext
    {
        public DbSet<Sales_Distributor_PO_Master_Part> Sales_Distributor_PO_Master_Part { get; set; }
        public DbSet<Sales_Distributor_PO_Header> Sales_Distributor_PO_Header { get; set; }
        public DbSet<Sales_Distributor_PO_Line> Sales_Distributor_PO_Line { get; set; }
        public DistributorConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}