using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.HC.Models
{
    public class HC_Business_Trip_Request
    {
        public int ID { get; set; }
        public string Req_Number { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Cost_ID { get; set; }
        public string Cost_Name { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string Position { get; set; }
        public string Destination { get; set; }
        public DateTime? Date_From { get; set; }
        public DateTime? Date_To { get; set; }
        public string Purpose { get; set; }
        public string Visited_To { get; set; }
        public string Destination_Type { get; set; }
        public string Travel_Method_By { get; set; }
        public string Travel_Method { get; set; }
        public string Travel_Need { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_At { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? isReject { get; set; }

        public ICollection<HC_Business_Trip_CA> HC_Business_Trip_CA { get; set; }
        public ICollection<HC_Business_Trip_Settlement> HC_Business_Trip_Settlement { get; set; }
    }
    public class HC_Business_Trip_CA
    {
        [ForeignKey("HC_Business_Trip_CA_Detail")]
        public int ID { get; set; }
        [ForeignKey("HC_Business_Trip_Request")]
        public int Request_ID { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string Position { get; set; }
        public double? Hotel_Total { get; set; }
        public double? Meals_Total { get; set; }
        public double? Daily_Total { get; set; }
        public double? Transport_Total { get; set; }
        public double? Misc_Total { get; set; }
        public string Hotel_Budget { get; set; }
        public string Daily_Budget { get; set; }
        public string Meals_Budget { get; set; }
        public string Flight_Budget { get; set; }
        public string RentCar_Budget { get; set; }
        public string Entertaintment_Budget { get; set; }
        public string Gasoline_Budget { get; set; }
        public string Toll_Budget { get; set; }
        public string Taxi_Budget { get; set; }
        public string Baggage_Budget { get; set; }
        public double? Exchange_Rate { get; set; }
        public virtual HC_Business_Trip_Request HC_Business_Trip_Request { get; set; }
        public ICollection<HC_Business_Trip_CA_Detail> HC_Business_Trip_CA_Detail { get; set; }

    }
    public class HC_Business_Trip_CA_Detail
    {
        public int ID { get; set; }
        [ForeignKey("HC_Business_Trip_CA")]
        public int CA_ID { get; set; }
        public string NIK { get; set; }
        public string Item_Type { get; set; }
        public string Item { get; set; }
        public double? CA_Amount { get; set; }
        public int? CA_Days { get; set; }
        public double? CA_Total { get; set; }
        public virtual HC_Business_Trip_CA HC_Business_Trip_CA { get; set; }
    }

    public class HC_Business_Trip_Settlement
    {
        [ForeignKey("HC_Business_Trip_Settlement_Detail")]
        public int ID { get; set; }
        [ForeignKey("HC_Business_Trip_Request")]
        public int Request_ID { get; set; }
        [ForeignKey("User")]
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Section { get; set; }
        public string Position { get; set; }
        public double? Hotel_Total { get; set; }
        public double? Meals_Total { get; set; }
        public double? Daily_Total { get; set; }
        public double? Transport_Total { get; set; }
        public double? Misc_Total { get; set; }
        public string Hotel_Budget { get; set; }
        public string Daily_Budget { get; set; }
        public string Meals_Budget { get; set; }
        public string Flight_Budget { get; set; }
        public string RentCar_Budget { get; set; }
        public string Entertaintment_Budget { get; set; }
        public string Gasoline_Budget { get; set; }
        public string Toll_Budget { get; set; }
        public string Taxi_Budget { get; set; }
        public string Baggage_Budget { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? isReject { get; set; }
        public bool? isRevise { get; set; }
        public double? Exchange_Rate { get; set; }
        public virtual Users User { get; set; }
        public virtual HC_Business_Trip_Request HC_Business_Trip_Request { get; set; }
        public ICollection<HC_Business_Trip_Settlement_Detail> HC_Business_Trip_Settlement_Detail { get; set; }
    }
    public class HC_Business_Trip_Settlement_Detail
    {
        public int ID { get; set; }
        [ForeignKey("HC_Business_Trip_Settlement")]
        public int CA_ID { get; set; }
        public string NIK { get; set; }
        public string Item_Type { get; set; }
        public string Item { get; set; }
        public double? CA_Amount { get; set; }
        public int? CA_Days { get; set; }
        public double? CA_Total { get; set; }
        public virtual HC_Business_Trip_Settlement HC_Business_Trip_Settlement { get; set; }
    }
    public class HC_Business_Trip_Settlement_Attachment
    {
        public int ID { get; set; }
        public int Settlement_ID { get; set; }
        public string ReqNumber { get; set; }
        public string Expenses { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
    }
    public class HC_Business_Trip_Request_Attachment
    {
        public int ID { get; set; }
        public string ReqNumber { get; set; }
        public string Filename { get; set; }
        public string Ext { get; set; }
    }

    public class HC_Business_Trip_Settlement_Comments
    {
        [Key]
        public int comment_id { get; set; }
        public string id { get; set; }
        public string ReqNumber { get; set; }
        public string ReqNIK { get; set; }
        [ForeignKey("Users")]
        public string nik { get; set; }
        public string parent { get; set; }
        public string created { get; set; }
        public long? modified { get; set; }
        public string content { get; set; }
        //public string attachments { get; set; }
        //public string pings { get; set; }
        //public string creator { get; set; }
        public string fullname { get; set; }
        public bool? isNew { get; set; }
        public bool? createdByAdmin { get; set; }
        public bool? created_by_current_user { get; set; }
        public int? upvoteCount { get; set; }
        public bool? userHasUpvoted { get; set; }
        public virtual Users Users { get; set; }
        [ForeignKey("Comment_ID")]
        public ICollection<HC_Business_Trip_Settlement_Comments_Attachment> attachments { get; set; }
    }
    public class HC_Business_Trip_Settlement_Comments_Attachment
    {
        [Key]
        public int id { get; set; }
        public int? Comment_ID { get; set; }
        public string file { get; set; }
        public string mime_type { get; set; }
    }

    public class HC_Business_Trip_Request_Comments
    {
        [Key]
        public int comment_id { get; set; }
        public string id { get; set; }
        public string ReqNumber { get; set; }
        [ForeignKey("Users")]
        public string nik { get; set; }
        public string parent { get; set; }
        public string created { get; set; }
        public long? modified { get; set; }
        public string content { get; set; }
        //public string attachments { get; set; }
        //public string pings { get; set; }
        //public string creator { get; set; }
        public string fullname { get; set; }
        public bool? isNew { get; set; }
        public bool? createdByAdmin { get; set; }
        public bool? created_by_current_user { get; set; }
        public int? upvoteCount { get; set; }
        public bool? userHasUpvoted { get; set; }
        public virtual Users Users { get; set; }
        [ForeignKey("Comment_ID")]
        public ICollection<HC_Business_Trip_Request_Comments_Attachment> attachments { get; set; }
    }
    public class HC_Business_Trip_Request_Comments_Attachment
    {
        [Key]
        public int id { get; set; }
        public int? Comment_ID { get; set; }
        public string file { get; set; }
        public string mime_type { get; set; }
    }

    public class BusinessTripConnection : DbContext
    {
        public DbSet<HC_Business_Trip_Request> HC_Business_Trip_Request { get; set; }
        public DbSet<HC_Business_Trip_CA> HC_Business_Trip_CA { get; set; }
        public DbSet<HC_Business_Trip_CA_Detail> HC_Business_Trip_CA_Detail { get; set; }
        public DbSet<HC_Business_Trip_Settlement> HC_Business_Trip_Settlement { get; set; }
        public DbSet<HC_Business_Trip_Settlement_Detail> HC_Business_Trip_Settlement_Detail { get; set; }
        public DbSet<HC_Business_Trip_Request_Attachment> HC_Business_Trip_Request_Attachment { get; set; }
        public DbSet<HC_Business_Trip_Settlement_Attachment> HC_Business_Trip_Settlement_Attachment { get; set; }
        public DbSet<HC_Business_Trip_Settlement_Comments> HC_Business_Trip_Settlement_Comments { get; set; }
        public DbSet<HC_Business_Trip_Settlement_Comments_Attachment> HC_Business_Trip_Settlement_Comments_Attachment { get; set; }
        public DbSet<HC_Business_Trip_Request_Comments> HC_Business_Trip_Request_Comments { get; set; }
        public DbSet<HC_Business_Trip_Request_Comments_Attachment> HC_Business_Trip_Request_Comments_Attachment { get; set; }

        public BusinessTripConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}