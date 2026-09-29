using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Data.Entity;

namespace NGKBusi.Areas.IT.Models
{
    [Table("IT_Disposal_Master")]
    public class IT_Disposal_Master
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int disposal_id { get; set; }

        [StringLength(50)]
        public string disposal_ref_no { get; set; }

        [StringLength(100)]
        public string department { get; set; }

        public DateTime disposal_date { get; set; }

        [StringLength(50)]
        public string status { get; set; }

        public virtual ICollection<IT_Wastehandover_Detail> IT_Wastehandover_Details { get; set; }
        public virtual ICollection<IT_Wastehandover_Approval> IT_Wastehandover_Approvals { get; set; }
    }

    [Table("IT_Wastehandover_Detail")]
    public class IT_Wastehandover_Detail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int detail_id { get; set; }

        [StringLength(50)]
        public string header_id { get; set; }

        public int disposal_id { get; set; }

        [ForeignKey("disposal_id")]
        public virtual IT_Disposal_Master IT_Disposal_Master { get; set; }

        [StringLength(250)]
        public string items { get; set; }

        public int quantity { get; set; }

        [StringLength(50)]
        public string unit { get; set; }

        [StringLength(100)]
        public string asset_no { get; set; }

        [StringLength(100)]
        public string type { get; set; }

        [StringLength(100)]
        public string condition { get; set; }

        [StringLength(250)]
        public string actions { get; set; }

        [StringLength(100)]
        public string confidentiality { get; set; }

        public DateTime created_date { get; set; }
        public bool is_generated { get; set; }

        [NotMapped]
        [StringLength(100)]
        public string creator_name { get; set; }
    }

    [Table("IT_Wastehandover_Approval")]
    public class IT_Wastehandover_Approval
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int approval_id { get; set; }

        [StringLength(50)]
        public string header_id { get; set; }

        public int? disposal_id { get; set; }

        [ForeignKey("disposal_id")]
        public virtual IT_Disposal_Master IT_Disposal_Master { get; set; }

        [StringLength(50)]
        public string status { get; set; }

        [StringLength(500)]
        public string message { get; set; }

        [StringLength(100)]
        public string signed_by { get; set; }

        [StringLength(50)]
        public string signed_date { get; set; }
    }

    public class ITWastehandoverConnection : DbContext
    {
        public ITWastehandoverConnection() : base()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<IT_Disposal_Master> IT_Disposal_Master { get; set; }
        public DbSet<IT_Wastehandover_Detail> IT_Wastehandover_Detail { get; set; }
        public DbSet<IT_Wastehandover_Approval> IT_Wastehandover_Approval { get; set; }
        public DbSet<IT_Disp_WH_Relation> IT_Disp_WH_Relation { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IT_Wastehandover_Detail>()
                .HasRequired(d => d.IT_Disposal_Master)
                .WithMany(m => m.IT_Wastehandover_Details)
                .HasForeignKey(d => d.disposal_id)
                .WillCascadeOnDelete(true);
        }
    }
}