using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGKBusi.Areas.Production.Models
{
    public class AssemblyQrSparkPlug
    {

    }

    public class Production_Assembly_QrSparkPlug_Master_Type
    {
        [Key]
        public int? id { get; set; }
        public string tipe { get; set; }
        public string PN { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class RequestHistory
    {
        public int id_tipe { get; set; }
        public string inspector { get; set; }
        public string lot { get; set; }
        public string operator_packing { get; set; }
        public int qty { get; set; }
        public DateTime tgl_inspection { get; set; }
        public int page_count { get; set; }
    }

    public class Production_Assembly_QrSparkPlug_History
    {
        [Key]
        public int id { get; set; }
        public string Part_No { get; set; }
        public string Part_Name { get; set; }
        public string Lot_No { get; set; }
        public string IRD_No { get; set; }
        public string Inspector { get; set; }
        public int Qty { get; set; }
        public DateTime? Tgl_Inspection { get; set; }

        public string QR_Header { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    [Table("Production_Assembly_QrSparkPlug_History_Detail")]
    public class Production_Assembly_QrSparkPlug_History_Detail
    {
        [Key]
        public int id { get; set; }
        public int History_Id { get; set; }

        [ForeignKey("History_Id")]
        public virtual Production_Assembly_QrSparkPlug_History History { get; set; }
        public string QR_Header { get; set; }
        public string Sequence_No { get; set; }
        public string QR_String { get; set; }
        public DateTime? created_at { get; set; }
    }

    public class AssemblyQrSparkPlugConnection : DbContext
    {
        public DbSet<Production_Assembly_QrSparkPlug_Master_Type> Production_Assembly_QrSparkPlug_Master_Type { get; set; }
        public DbSet<Production_Assembly_QrSparkPlug_History> Production_Assembly_QrSparkPlug_History { get; set; }
        public DbSet<Production_Assembly_QrSparkPlug_History_Detail> Production_Assembly_QrSparkPlug_History_Detail { get; set; }

        public AssemblyQrSparkPlugConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}