using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HPM.Models
{
    public class HPM_GenerateQR_Items
    {
        public HPM_GenerateQR_Items()
        {
            this.created_at = DateTime.Now;
        }
        public int id { get; set; }

        public string filename { get; set; }
        public string path { get; set; }

        public DateTime created_at { get; set; }

    }

    public class HPM_GenerateQR_Item_Detail
    {
        public int id { get; set; }
        public int item_id { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string supply_address { get; set; }
        public string ms_id { get; set; }
        public string inventory_category { get; set; }
        public string mid_coloum_1 { get; set; }
        public string mid_coloum_2 { get; set; }
        public string ps_code { get; set; }
        public string order_class { get; set; }
        public string production_seq_no { get; set; }
        public string kd_lot_no { get; set; }
        public string barcode_no { get; set; }
        public string ship { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string hns { get; set; }
    }
    public class GenerateQRConnection : DbContext
    {
        public DbSet<HPM_GenerateQR_Items> HPM_GenerateQR_Items { get; set; }
        public DbSet<HPM_GenerateQR_Item_Detail> HPM_GenerateQR_Item_Detail { get; set; }
        public GenerateQRConnection()
        {
             this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}