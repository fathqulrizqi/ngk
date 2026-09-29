using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.SCM.Models
{
    public class SCM_HPMQRGenerator_Items
    {
        public SCM_HPMQRGenerator_Items()
        {
            this.created_at = DateTime.Now;
        }

        public int id { get; set; }

        public string filename { get; set; }
        public string path { get; set; }

        public DateTime created_at { get; set; }

    }

    public class LabelDetailViewModel
    {
        public int item_id { get; set; }
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
        public string from_1 { get; set; }
        public string from_2 { get; set; }
        public string to_1 { get; set; }
        public string to_2 { get; set; }
        public string qr_code { get; set; }
    }
    public class SCM_HPMQRGenerator_Item_Detail {
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

    public class AX_CustExternalItem
    {
        public string ITEMID { get; set; }
        public int MODULETYPE { get; set; }
        public string CUSTVENDRELATION { get; set; }
        public string EXTERNALITEMTXT { get; set; }
        public string EXTERNALITEMID { get; set; }
        public int ABCCATEGORY { get; set; }
        public string DESCRIPTION { get; set; }
        public string INVENTDIMID { get; set; }
        public string MODIFIEDBY { get; set; }
        public string DATAAREAID { get; set; }
        public int RECVERSION { get; set; }
        public long PARTITION { get; set; }
        public long ID { get; set; }
        public string Barcode { get; set; }
        public string SalesClass { get; set; }
    }

    public class HPMQRGeneratorConnection : DbContext
    {
        public DbSet<SCM_HPMQRGenerator_Items> SCM_HPMQRGenerator_Items { get; set; }
        public DbSet<SCM_HPMQRGenerator_Item_Detail> SCM_HPMQRGenerator_Item_Detail { get; set; }
        public DbSet<AX_CustExternalItem> AX_CustExternalItem { get; set; }
        public HPMQRGeneratorConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}