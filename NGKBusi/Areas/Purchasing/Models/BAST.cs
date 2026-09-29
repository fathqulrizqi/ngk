using NGKBusi.Areas.PE.Models;
using NGKBusi.Areas.Purchasing.Models;
using NGKBusi.Models;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO.Packaging;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
namespace NGKBusi.Areas.Purchasing.Models
{
    public class Purchasing_BAST
    {
        [Key]
        public int id { get; set; }
        public string bast_number { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string wo_number { get; set; }
        public string wo_description { get; set; }
        public string vendor_id { get; set; }
        public string vendor_name { get; set; }
        public string budget_no { get; set; }
        public string budget_name { get; set; }
        public string section_name { get; set; }
        public string creator_nik { get; set; }
        public string creator_name { get; set; }
        public DateTime? due_date { get; set; }
        public string status { get; set; }
        public DateTime? timestamps { get; set; }
    }

    public class Purchasing_BAST_File
    {
        [Key]
        public int id { get; set; }
        public string bast_number { get; set; }
        public string file_name { get; set; }
        public string status { get; set; }
    }

    public class Purchasing_BAST_Comment_Files
    {
        [Key]
        public int id { get; set; }
        public int c_id { get; set; }
        [ForeignKey("c_id")]
        [JsonIgnore]
        public virtual Purchasing_BAST_Comments comment { get; set; }
        public string file_name { get; set; }
        public string mime_type { get; set; }
    }

    public class Purchasing_BAST_Comments
    {
        [Key]
        public int c_id { get; set; }
        public string bast_number { get; set; }
        public string id { get; set; }
        public string parent { get; set; }
        public string creator { get; set; }
        public string fullName { get; set; }
        public DateTime? created { get; set; }
        public long? modified { get; set; }
        public string content { get; set; }
        public string attachments { get; set; }
        public string pings { get; set; }
        [NotMapped]
        public bool? created_by_current_user { get; set; }
        public virtual ICollection<Purchasing_BAST_Comment_Files> comment_files { get; set; }
    }

    public class BASTConnection : DbContext
    {
        public DbSet<Purchasing_BAST> Purchasing_BAST { get; set; }
        public DbSet<Purchasing_BAST_File> Purchasing_BAST_File { get; set; }
        public DbSet<Purchasing_BAST_Comments> Purchasing_BAST_Comments { get; set; }
        public DbSet<Purchasing_BAST_Comment_Files> Purchasing_BAST_Comment_Files { get; set; }
        public BASTConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }

}