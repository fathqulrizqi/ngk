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

namespace NGKBusi.Areas.IT.Models
{
    public class RequestSystem
    {
    }
    
    public class IT_RequestSystem_ItemMaster
    {
        [Key]
        public int ID { get; set; }
        public string ItemName { get; set; }
    }
    public class IT_RequestSystem_Header
    {
        [Key]
        public int ID { get; set; }
        public string RequestNo { get; set; }
        public string NIK { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public DateTime? CreateTime { get; set; }
        public string CreateBy { get; set; }
        public byte IsDelete { get; set; }
        public string SignReqManagerBy { get; set; }
        public DateTime? SignReqManagerTime { get; set; }
        public string SignITManagerBy { get; set; }
        public DateTime? SignITManagerTime { get; set; }
        public string SignGMITBy { get; set; }
        public DateTime? SignGMITTime { get; set; }
        public string CompleteITStaffBy { get; set; }
        public DateTime? CompleteITStaffTime { get; set; }
    }
    public class IT_RequestSystem_Lines
    {
        [Key]
        public int ID { get; set; }
        public int ReqID { get; set; }
        public int ItemID { get; set; }
        public string ItemName { get; set; }
        public string Explains { get; set; }
        public byte IsChecked { get; set; }
    }
    public class IT_RequestSystem_LinesSub
    {
        [Key]
        public int ID { get; set; }
        public int LineID { get; set; }
        public int LineItemID { get; set; }
        public string LineSub_Name { get; set; }
        public string LineSub_Explain { get; set; }
        public int ReqID { get; set; }
    }
    public class Tbl_IT_RequestSystem_Header
    {
        public int ID { get; set; }
        public string RequestNo { get; set; }
        public string DateRequest { get; set; }
        public string NIK { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public byte IsDelete { get; set; }
        public string CreateBy { get; set; }
        public string ActionButton { get; set; }

    }

    public class RequestSystemConnection : DbContext
    {
        public DbSet<IT_RequestSystem_ItemMaster> IT_RequestSystem_ItemMaster { get; set; }
        public DbSet<IT_RequestSystem_Header> IT_RequestSystem_Header { get; set; }
        public DbSet<IT_RequestSystem_Lines> IT_RequestSystem_Lines { get; set; }
        public DbSet<IT_RequestSystem_LinesSub> IT_RequestSystem_LinesSub { get; set; }
        public RequestSystemConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}