using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Web.Mvc;
using Org.BouncyCastle.Asn1.Crmf;

namespace NGKBusi.Areas.IT.Models
{
    public class ITPolicy
    {
    }
    public class IT_Regulation_Document
    {
        [Key]
        public int ID { get; set; }
        public string DocumentID { get; set; }
        public string DocumentName { get; set; }
        public string Filename { get; set; }
        public byte IsActive { get; set; }
    }

    public class IT_ITPolicy_Document
    {
        public int ID { get; set; }
        public string Year { get; set; }
        public string DocumentName { get; set; }
    }

    public class IT_ISMS_Document
    {
        public int ID { get; set; }
        public string Year { get; set; }
        public string DocumentName { get; set; }
    }
    public class IT_CSIRT_Document
    {
        [Key]
        public int ID { get; set; }
        public string Year { get; set; }
        public string DocumentName { get; set; }
    }
    public class ITPolicyConnection : DbContext
    {
        public DbSet<IT_Regulation_Document> IT_Regulation_Document { get; set; }
        public DbSet<IT_ITPolicy_Document> IT_ITPolicy_Document { get; set; }
        public DbSet<IT_ISMS_Document> IT_ISMS_Document { get; set; }
        public DbSet<IT_CSIRT_Document> IT_CSIRT_Document { get; set; }

        public ITPolicyConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}