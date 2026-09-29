using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace NGKBusi.Areas.SCM.Models
{
    [Table("WHFG_Checksheet_Users")]
    public class WHFG_Checksheet_User
    {
        [Key]
        public int UserID { get; set; }
        public string NIK { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
    }

    [Table("WHFG_Master_Vehicles")]
    public class WHFG_Master_Vehicle
    {
        [Key]
        public int VehicleID { get; set; }
        public string LicensePlate { get; set; }
        public string VehicleType { get; set; }
        public bool IsActive { get; set; }
    }

    [Table("WHFG_Master_Destinations")]
    public class WHFG_Master_Destination
    {
        [Key]
        public int DestinationID { get; set; }
        public string DestinationName { get; set; }
        public bool IsActive { get; set; }
    }

    [Table("WHFG_VehicleChecksheet_Items")]
    public class WHFG_VehicleChecksheet_Item
    {
        [Key]
        public int ItemID { get; set; }
        public string Category { get; set; }
        public int ItemNumber { get; set; }
        public string ItemName { get; set; }
        public string Standard { get; set; }
        public bool IsActive { get; set; }
    }

    [Table("WHFG_VehicleChecksheet_Headers")]
    public class WHFG_VehicleChecksheet_Header
    {
        [Key]
        public int HeaderID { get; set; }
        public string Supplier { get; set; }
        public string DriverName { get; set; }
        public string DriverNIK { get; set; }
        public int VehicleID { get; set; }
        public int DestinationID { get; set; }
        public DateTime CheckDate { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("VehicleID")]
        public virtual WHFG_Master_Vehicle Vehicle { get; set; }

        [ForeignKey("DestinationID")]
        public virtual WHFG_Master_Destination Destination { get; set; }
    }

    [Table("WHFG_VehicleChecksheet_Details")]
    public class WHFG_VehicleChecksheet_Detail
    {
        [Key]
        public int DetailID { get; set; }
        public int HeaderID { get; set; }
        public int ItemID { get; set; }
        public string ResultValue { get; set; }

        [ForeignKey("ItemID")]
        public virtual WHFG_VehicleChecksheet_Item CheckItem { get; set; }
    }

    public class WHFGConnection : DbContext
    {
        public DbSet<WHFG_Checksheet_User> ChecksheetUsers { get; set; }
        public DbSet<WHFG_Master_Vehicle> Vehicles { get; set; }
        public DbSet<WHFG_Master_Destination> Destinations { get; set; }
        public DbSet<WHFG_VehicleChecksheet_Item> Items { get; set; }
        public DbSet<WHFG_VehicleChecksheet_Header> Headers { get; set; }
        public DbSet<WHFG_VehicleChecksheet_Detail> Details { get; set; }

        public WHFGConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}
