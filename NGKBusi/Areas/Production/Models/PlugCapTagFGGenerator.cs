using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace NGKBusi.Areas.Production.Models
{
    public class PlugCapTagFGGenerator
    {
    }

    public class Production_PlugCap_TagFG_Generator_Tag_FG
    {
        [Key]
        public int? id { get; set; }
        public string BN { get; set; }
        public string inspector { get; set; }
        public string part_name { get; set; }
        public string product_name { get; set; }
        public string part_no { get; set; }
        public string gimzcode { get; set; }
        public string lot { get; set; }       
        public int qty { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }

    }

    public class Production_PlugCap_TagFG_Generator_Tag_Part
    {
        [Key]
        public int? id { get; set; }
        public int fg_id { get; set; }
        public string BN { get; set; }
        public string inspector { get; set; }
        public string part_name { get; set; }
        public string product_name { get; set; }
        public string part_no { get; set; }
        public string gimzcode { get; set; }
        public string lot { get; set; }
        public int qty { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class PlugCapTagFGGeneratorConnection : DbContext
    {
        public DbSet<Production_PlugCap_TagFG_Generator_Tag_FG> Production_PlugCap_TagFG_Generator_Tag_FG { get; set; }
        public DbSet<Production_PlugCap_TagFG_Generator_Tag_Part> Production_PlugCap_TagFG_Generator_Tag_Part { get; set; }
        public PlugCapTagFGGeneratorConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }

}