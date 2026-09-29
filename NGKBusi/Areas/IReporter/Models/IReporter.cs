using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.IReporter.Models
{
    public class Rep_Cluster
    {
        public int Cluster_ID { get; set; }
        public string Cluster_Name { get; set; }
        public string Cluster_Type { get; set; }
        public string Input_Value { get; set; }
        public string Display_Value { get; set; }
    }
    
    public class IReporterConnection : DbContext
    {
        public DbSet<Rep_Cluster> Rep_Cluster { get; set; }
    }
}