using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace NGKBusi.Areas.Production.Models
{
    public class PlugCapPaletRecod
    {
    }

    public class Production_PlugCap_Tag_FG
    {
        [Key]
        public int? id { get; set; }
        public string pallet_no { get; set; }
        public string part_no {  get; set; }
        public string type {  get; set; }
        public string lot_no { get; set; }
        public int qty { get; set;}
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }

    }

    public class Production_PlugCap_Tag_Part
    {
        [Key]
        public int? id { get; set; }
        public int fg_id { get; set; }
        public string pallet_no { get; set; }
        public string type { get; set; }
        public string part_no { get; set; }
        public int qty { get; set;}
        public string lot_prod { get; set; }
        public DateTime tgl_repacking { get; set; }
        public string BN { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class PlugCapPalletConnection : DbContext
    {
        public DbSet<Production_PlugCap_Tag_FG> Production_PlugCap_Tag_FG { get; set; }
        public DbSet<Production_PlugCap_Tag_Part> Production_PlugCap_Tag_Part { get; set; }
        public PlugCapPalletConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }

}