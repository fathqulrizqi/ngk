using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.Production.Models
{
    public class AbsensiWebIreporter
    {
    }

    [Table("absensi")]
    public class Absensi
    {
        [Key]
        public int id { get; set; }
        public string SectionName { get; set; }
        public string SubSection { get; set; }
        public DateTime? Date { get; set; }
        public string Name { get; set; }
        public string NIK { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
    }

    public class absensi_list_descriptions
    {
        [Key]
        public int id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class list_section_production
    {
        [Key]
        public long id { get; set; }
        public string Section { get; set; }
    }

    [Table("absensi_transfer_labor_tambahans")]
    public class AbsensiTransferLaborTambahan
    {
        public long id { get; set; }
        public string Section { get; set; }
        public DateTime? Date { get; set; }
        public string NIK { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
        public string Shift { get; set; }
        public string TransferLabor { get; set; }
        public string TransferLaborNormal { get; set; }
        public string Planning { get; set; }

        public string DescTransferLabor { get; set; }

        public string TransferOvertime { get; set; }
        public string TransferOvertimePlanning { get; set; }

        public string DescTransferLaborOvertime { get; set; }
        public string TransferTo { get; set; }

        public string TransferLaborId { get; set; }

        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }

        public string TransferType { get; set; }
    }

    public class WebIReporterConnection : DbContext
    {
        public DbSet<Absensi> Absensi { get; set; }
        public DbSet<absensi_list_descriptions> absensi_list_descriptions { get; set; }
        public DbSet<list_section_production> list_section_production { get; set; }
        public DbSet<AbsensiTransferLaborTambahan> AbsensiTransferLaborTambahan { get; set; }

        public WebIReporterConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["WebIReporterConnection"].ConnectionString;
        }
    }
}