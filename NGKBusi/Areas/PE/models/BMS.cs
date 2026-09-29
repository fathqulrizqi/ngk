using System;
using System.Collections.Generic;
using NGKBusi.Areas.PE.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace NGKBusi.Areas.PE.Models
{
    public class BMS
    {
    }

    public class PE_BMS_Access
    {
        [Key]
        public int access_id { get; set; }
        public string name { get; set; }
        public string NIK { get; set; }
        public string access { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
    }

    public class PE_BMS_Tooling_Category
    {
        [Key]
        public int category_id { get; set; }
        public string category_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        public virtual ICollection<PE_BMS_Tooling_Spec> ComponentSpecs { get; set; }
    }

    public class PE_BMS_Tooling_Spec
    {
        [Key]
        public int spec_id { get; set; }

        // Ini menggantikan 'string tooling_category'
        public int category_id { get; set; }

        public string spec_name { get; set; }
        public int min_stock { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        [ForeignKey("category_id")]
        public virtual PE_BMS_Tooling_Category ToolingCategory { get; set; }

        public virtual ICollection<PE_BMS_Stock_Management> StockManagements { get; set; }
        public virtual ICollection<PE_BMS_Stock_In_Production_Area> StockInProductions { get; set; }

        public virtual ICollection<PE_BMS_Log_Transaction> LogTransactions { get; set; }
    }

    public class PE_BMS_Stock_Management
    {
        [Key]
        public int stock_id { get; set; }
        public int spec_id { get; set; }
        public int? quantity { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
        [ForeignKey("spec_id")]
        public virtual PE_BMS_Tooling_Spec ComponentSpec { get; set; }
    }

    public class PE_BMS_Stock_In_Production_Area
    {
        [Key]
        public int stock_id { get; set; }
        public int spec_id { get; set; }
        public int? quantity { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }
        [ForeignKey("spec_id")]
        public virtual PE_BMS_Tooling_Spec ComponentSpecProduction { get; set; }
    }

    public class PE_BMS_Log_Transaction
    {
        [Key]
        public int log_transaction_id { get; set; }
        public int spec_id { get; set; }
        public string tooling_code { get; set; }
        public string item_no { get; set; }
        public string transaction_type { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        [ForeignKey("spec_id")]
        public virtual PE_BMS_Tooling_Spec ToolingSpec { get; set; }
    }

    public class PE_BMS_Tooling_Scrap
    {
        [Key]
        public int scrap_id { get; set; }
        public int spec_id { get; set; }
        public string tooling_code { get; set; }
        public string item_no { get; set; }
        public int last_usage_lifetime { get; set; }
        public int repair_count { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

        [ForeignKey("spec_id")]
        public virtual PE_BMS_Tooling_Spec ToolingSpecScrap { get; set; }
    }

    public class PE_BMS_Inventory_list
    {
        [Key]
        public int inventory_id { get; set; }
        public string url_image { get; set; }
        public string category { get; set; }
        public string name { get; set; }
        public int Qty { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? deleted_at { get; set; }

    }
    public class PE_BMS_Inventory_Usage_Header
    {
        [Key]
        public int header_id { get; set; }
        public string project_name { get; set; } 
        public string remarks { get; set; }      
        public string created_by_nik { get; set; }
        public string created_by_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class PE_BMS_Inventory_Usage_Detail
    {
        [Key]
        public int detail_id { get; set; }
        public int header_id { get; set; }
        public int inventory_id { get; set; }
        public int qty_used { get; set; }
    }

    public class PE_BMS_Workshop_Request_Form
    {
        [Key]
        public int form_id { get; set; }
        public string department { get; set; }
        public string request { get; set; }
        public string request_type { get; set; }
        public string request_desc { get; set; }
        public string request_image { get; set; }
        public string reference_image{ get; set; }
        public string reference_no_image { get; set; }
        public int request_qty { get; set; }
        public DateTime due_date { get; set; }
        public string remarks { get; set; }
        public string status { get; set; }
        public DateTime create_at { get; set; }
        public DateTime? update_at { get; set; }
        public string request_by_nik { get; set; }
        public string request_by_name { get; set; }
        public string finished_image { get; set; }
    }

    public class InventoryUsageDTO
    {
        public string project_name { get; set; }
        public string remarks { get; set; }
        public List<InventoryDetailDTO> details { get; set; }
    }

    public class InventoryDetailDTO
    {
        public int inventory_id { get; set; }
        public int qty_used { get; set; }
    }

    public class BMSConnection : DbContext
    {
        public DbSet<PE_BMS_Access> PE_BMS_Access { get; set; }
        public DbSet<PE_BMS_Tooling_Spec> PE_BMS_Tooling_Spec { get; set; }
        public DbSet<PE_BMS_Tooling_Category> PE_BMS_Tooling_Category { get; set; }
        public DbSet<PE_BMS_Stock_Management> PE_BMS_Stock_Management { get; set; }
        public DbSet<PE_BMS_Stock_In_Production_Area> PE_BMS_Stock_In_Production_Area { get; set; }
        public DbSet<PE_BMS_Log_Transaction> PE_BMS_Log_Transactions { get; set; }
        public DbSet<PE_BMS_Tooling_Scrap> PE_BMS_Tooling_Scrap { get; set; }
        public DbSet<PE_BMS_Inventory_list> PE_BMS_Inventory_list { get; set; }
        public DbSet<PE_BMS_Inventory_Usage_Header> PE_BMS_Inventory_Usage_Header { get; set; }
        public DbSet<PE_BMS_Inventory_Usage_Detail> PE_BMS_Inventory_Usage_Detail { get; set; }
        public DbSet<PE_BMS_Workshop_Request_Form> PE_BMS_Workshop_Request_Form { get; set; }


        public BMSConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}