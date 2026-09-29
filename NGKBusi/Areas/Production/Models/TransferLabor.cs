using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace NGKBusi.Areas.Production.Models
{
    public class TransferLabor
    {
    }

    public class Production_TransferLabor_Request_Header
    {
        [Key]
        public string id { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string from_section { get; set; }
        public string from_department { get; set; }
        public string to_section { get; set; }
        public string to_department { get; set; }
        public string job_detail { get; set; }
        public string code_job_detail { get; set; }
        public string request_creator_by { get; set; }
        public string request_creator_sign { get; set; }
        public DateTime? request_creator_sign_date { get; set; }
        public string request_approval_by { get; set; }
        public string request_approval_sign { get; set; }
        public DateTime? request_approval_sign_date { get; set; }
        public string provider_issued_by { get; set; }
        public string provider_issued_sign { get; set; }
        public DateTime? provider_issued_sign_date { get; set; }
        public string provider_approval_by { get; set; }
        public string provider_approval_sign { get; set; }
        public DateTime? provider_approval_sign_date { get; set; }
        public string status { get; set; }
        public string created_by { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }

    public class SectionDropdownDTO
    {
        public string Section { get; set; }
        public string Department { get; set; }
    }

    public class SelectListDescription
    {
        public string CodeDesc {  get; set; }
        public string TextDesc { get; set; }
    }

    public class Production_TransferLabor_Request_Detail
    {
        [Key]
        public int id { get; set; }
        public string referal_id { get; set; }
        public string nik { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string status {get; set; }
        public double transfer_labor_hours { get; set; }
        public double transfer_overtime_hours { get; set; }
        public double total_transfer_hours { get; set; }
        public string planning {  get; set; }

    }

    public class TransferLaborRequestVM
    {
        public string request_creator_by { get; set; }
        public string from_section { get; set; }
        public string to_section { get; set; }
        public string job_detail { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string request_creator_sign { get; set; }
        public string created_by { get; set; }
        public DateTime? request_creator_sign_date { get; set; }

        // Bagian Detail (berupa List karena dari input array HTML "name[]", "nik[]", dll)
        public List<string> name { get; set; }
        public List<string> nik { get; set; }
        public List<string> planning { get; set; }
        public List<string> description { get; set; }
        public List<string> status { get; set; }
        public List<float> transfer_labor_hours { get; set; }
        public List<float> transfer_overtime_hours { get; set; }

    }

    public class Production_TransferLabor_Approval_Access
    {
        public int id { get; set; }
        public string nik { set; get; }
        public string section { get; set; }
        public string department { get; set; }
        public string access { get; set; }
    }

    public class TransferLaborConnection : DbContext
    {
        public DbSet<Production_TransferLabor_Request_Header> Production_TransferLabor_Request_Header { get; set; }
        public DbSet<Production_TransferLabor_Request_Detail> Production_TransferLabor_Request_Detail { get; set; }
        public DbSet<Production_TransferLabor_Approval_Access> Production_TransferLabor_Approval_Access { get; set; }
        public TransferLaborConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }

}