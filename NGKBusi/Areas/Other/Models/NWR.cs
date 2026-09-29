using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;


namespace NGKBusi.Areas.Other.Models
{
    public class NWR
    {
    }

    public class OTH_NWR_Master_Rooms_Category
    {

        [Key]
        public int ID { get; set; }
        public string Category { get; set; }
    }
    
    public class OTH_NWR_Master_Rooms
    {

        [Key]
        public int ID { get; set; }
        public string RoomTitle { get; set; }
        public string Image { get; set; }
        public int IDRoomCat { get; set; }
        public int? ExtensionNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class OTH_NWR_Rooms_Properties
    {
        [Key]
        public int ID { get; set; }
        public int RoomID { get; set; }
        public string PropsName { get; set; }
        public int Quantity { get; set; }
    }

    public class OTH_NWR_Bookings
    {
        [Key]
        public int ID { get; set; }
        public string UserNIK { get; set; }
        public int RoomID { get; set; }
        public string Subject { get; set; }
        public DateTime Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public byte Status { get; set; }
        public DateTime? Timestamps { get; set; }
        public int? Attendance { get; set; }
        public string Link { get; set; }
        public string LinkMeet { get; set; }
    }

    public class OTH_NWR_Booking_Tools
    {
        [Key]
        public int ID { get; set; }
        public int BookingID { get; set; }
        public int ToolID { get; set; }
    }

    public class OTH_NWR_Master_RoomCat
    {
        public int ID { get; set; }
        public string RoomCat { get; set; }
        public bool? isActive { get; set; }
        public bool? isMaintenance { get; set; }
        public int sequence { get; set; }
        public int sequence2 { get; set; }

    }

    public class OTH_NWR_Master_Additional_Tools
    {
        [Key]
        public int ID { get; set; }
        public string ToolName { get; set; }
        public int Quantity { get; set; }
    }
    public class JS_OTH_NWR_Master_Rooms
    {

        public int ID { get; set; }
        public string RoomTitle { get; set; }
        public string Image { get; set; }
        public int IDRoomCat { get; set; }
        public int? ExtensionNumber { get; set; }
        public bool? IsActive { get; set; }
    }
   
    public class Tbl_Events
    {
        public int id { get; set; }
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string color { get; set; }
        public int roomid { get; set; }
        public string linkmeet { get; set; }
        public string subject { get; set; }
        public string user { get; set; }
        public string username { get; set; }
        public string roomtitle { get; set; }
        public string timestamps { get; set; }
        public int attendance { get; set; }
        public string link { get; set; }

    }

    public class OTH_NWR_Requests
    {
        public int id { get; set; }
        public int idConflict { get; set; }
        public string NIKConflict { get; set; }
        public string dateConflict { get; set; }
        public string startConflict { get; set; }
        public string endConflict { get; set; }
        public string roomConflict { get; set; }
        public string subjectConflict { get; set; }
        public int attendanceConflict { get; set; }
        public string NIKRequest { get; set; }
        public string roomRequest { get; set; }
        public string dateRequest { get; set; }
        public string startRequest { get; set; }
        public string endRequest { get; set; }
        public string messageRequest { get; set; }
        public int attendanceRequest { get; set; }
        public byte? statusR { get; set; }
        public DateTime timestamps { get; set; }
    }


    public class NWRConnection : DbContext
    {
        public DbSet<OTH_NWR_Master_Rooms> OTH_NWR_Master_Rooms { get; set; }
        public DbSet<OTH_NWR_Master_Rooms_Category> OTH_NWR_Master_Rooms_Category { get; set; }
        public DbSet<OTH_NWR_Rooms_Properties> OTH_NWR_Rooms_Properties { get; set; }
        public DbSet<OTH_NWR_Bookings> OTH_NWR_Bookings { get; set; }
        public DbSet<OTH_NWR_Master_RoomCat> OTH_NWR_Master_RoomCat { get; set; }
        public DbSet<OTH_NWR_Requests> OTH_NWR_Requests { get; set; }
        public DbSet<OTH_NWR_Booking_Tools> OTH_NWR_Booking_Tools { get; set; }
        public DbSet<OTH_NWR_Master_Additional_Tools> OTH_NWR_Master_Additional_Tools { get; set; }
        public NWRConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}