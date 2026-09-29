using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace NGKBusi.Areas.HC.Models
{
    [Table("HC_Corporate_Policy")]
    public class CorporatePolicyModel
    {
        [Key]
        public int ID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string PolicyName { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; }
        
        public bool IsActive { get; set; }
        
        [StringLength(50)]
        public string CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        [StringLength(50)]
        public string UpdatedBy { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
    }

    public class CorporatePolicyConnection : DbContext
    {
        public DbSet<CorporatePolicyModel> CorporatePolicies { get; set; }

        public CorporatePolicyConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}
