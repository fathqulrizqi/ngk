using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.AX.Models
{
    public class AX_Vendor_List
    {
        [Key]
        public string AccountNum { get; set; }
        public string VendGroup { get; set; }
        public string Name { get; set; }
        public string SearchName { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_At { get; set; }
        public bool? IsActive { get; set; }
    }
       
    public class VendorConnection : DbContext
    {
        public DbSet<AX_Vendor_List> AX_Vendor_List { get; set; }
        public VendorConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}