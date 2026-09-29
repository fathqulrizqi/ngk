using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.FA.Models
{
    public class StockOpname
    {
    }

    public class T_OutFG_DebitCredit
    {
        [Key]
        public decimal DebitNum { get; set; }
        public string TrNo { get; set; }
        public decimal NumID { get; set; }
        public DateTime? Entrydate { get; set; }
        public string SectionID { get; set; }
        public string PositionID { get; set; }
        public string PM { get; set; }
        public string MotorID { get; set; }
        public string TypeBox { get; set; }
        public string RackNo { get; set; }
        public decimal QtyReal { get; set; }
        public string UserID { get; set; }
        public DateTime? SysDate { get; set; }
        public decimal IsFifo { get; set; }
        public string ScanBarcodeID { get; set; }
    }

    public class T_OutFG_Location
    {
        [Key]
        public string FID { get; set; }
        public string FDescription { get; set; }
        public int FCapacity { get; set; }
    }


    public class StockOpnameConnection : DbContext
    {
        public DbSet<T_OutFG_DebitCredit> T_OutFG_DebitCredits { get; set; }
        public DbSet<T_OutFG_Location> T_OutFG_Locations { get; set; }
        public StockOpnameConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["BarcodeConnection"].ConnectionString;
        }
    }
}