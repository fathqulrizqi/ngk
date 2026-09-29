using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace NGKBusi.Areas.Production.Models
{
    public class PlugCupQrGenerator
    {
    }

    public class Production_PlugCup_QrGenerator_Master_Type
    {
        [Key]
        public int? id { get; set; }
        public string tipe { get; set; }
        public string part_no { get; set; }
        public string part_name { get; set; }
        public string gimz_code {  get; set; }
        public string product_name {  get; set; }
        public string customer {  get; set; }
        public string customer_code { get; set; }
        public string type_customer { get; set; }
        public int? label_per_page { get; set; }

        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class RequestHistoryPlugCup
    {
        public int id_tipe { get; set; }
        public string inspector { get; set; }
        public string lot { get; set; }
        public int qty { get; set; }
        public DateTime tgl_inspection { get; set; }
        public int page_count { get; set; }
    }
    public class Production_PlugCup_QrGenerator_History
    {
        public int id { get; set; }
        public string Tipe { get; set; }
        public string Part_No { get; set; }
        public string Part_Name { get; set; }
        public string Product_name { get; set; }
        public string Lot_No { get; set; }
        public string IRD_No { get; set; }
        public string Inspector { get; set; }
        public int Qty { get; set; }
        public string QR_Header { get; set; }
        public string Running_Number { get; set; }
        public string Front_QR { get; set; } 
        public string Gimz_Code { get; set; }
        public string Customer {  get; set; }
        public string Customer_Type { get; set; }
        public string Operator_Packing { get; set; }
        public DateTime? Tgl_Inspection { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class Production_PlugCup_QrGenerator_History_Detail
    {
        [Key]
        public int id { get; set; }
        public int History_Id { get; set; }

        [ForeignKey("History_Id")]
        public virtual Production_PlugCup_QrGenerator_History History { get; set; }
        public string QR_Header { get; set; }
        public string Sequence_No { get; set; }
        public string QR_String { get; set; }
        public DateTime? created_at { get; set; }
    }

    public class PlugCupQrGeneratorConnection : DbContext
    {
        public DbSet<Production_PlugCup_QrGenerator_Master_Type> Production_PlugCup_QrGenerator_Master_Type { get; set; }
        public DbSet<Production_PlugCup_QrGenerator_History> Production_PlugCup_QrGenerator_History { get; set; }
        public DbSet<Production_PlugCup_QrGenerator_History_Detail> Production_PlugCup_QrGenerator_History_Detail { get; set; }

        public PlugCupQrGeneratorConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}