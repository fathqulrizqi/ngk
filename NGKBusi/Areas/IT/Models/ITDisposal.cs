using DocumentFormat.OpenXml.Drawing;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Areas.WebService.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace NGKBusi.Areas.IT.Models
{
    public class IT_Disposal_Header
    {
        [Key]
        public int id { get; set; }
        public string header_id { get; set; }
        [Column("status")]
        public string status { get; set; }
        public string department { get; set; }

        [Column("approved_by")]
        public string ApprovedBy { get; set; }

        // Added column for date only
        [Column(TypeName = "date")]
        public DateTime? ApprovedDate { get; set; }

        public string requester_nik { get; set; }
        public string requester_name { get; set; }

        [Column("checked_by")]
        public string CheckedBy { get; set; }

        // Added column for date only
        [Column(TypeName = "date")]
        public DateTime? CheckedDate { get; set; }
        public string checker_nik { get; set; }
        public string approver_nik { get; set; }
    }

    public class IT_Disposal_Detail
    {
        [Key]
        public int detail_id { get; set; }
        public string header_id { get; set; }
        public int quantity { get; set; }
        public string unit { get; set; }
        public string asset_no { get; set; }
        public string items { get; set; }
        public string type { get; set; }
        public string condition { get; set; }
        public string actions { get; set; }
        public string confidentiality { get; set; }

        public DateTime created_date { get; set; }
    }

    public class IT_Disposal_Approval
    {
        [Key]
        public int approval_id { get; set; }

        public string header_id { get; set; }
        public string signed_by { get; set; }
        public string signed_date { get; set; }
        public string status { get; set; }
        public string message { get; set; }

        // These fields exist in the class for your UI logic, 
        // but the database will now ignore them.
        [NotMapped]
        public string approval_name { get; set; }

        [NotMapped]
        public string title { get; set; }
    }

    // --- Model Baru untuk Admin / Checker / Approver ---
    public class IT_Disposal_Admin
    {
        [Key]
        public int id { get; set; }
        public string nik { get; set; }
        public string role { get; set; }
        public byte is_active { get; set; }
    }

    // --- DbContext ---
    public class ITDisposalConnection : DbContext
    {
        public DbSet<IT_Disposal_Header> IT_Disposal_Header { get; set; }
        public DbSet<IT_Disposal_Detail> IT_Disposal_Detail { get; set; }
        public DbSet<IT_Disposal_Approval> IT_Disposal_Approval { get; set; }
        public DbSet<IT_Disposal_Admin> IT_Disposal_Admin { get; set; }
        public DbSet<IT_Disp_WH_Relation> IT_Disp_WH_Relation { get; set; }

        public ITDisposalConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}