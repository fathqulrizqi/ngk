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

namespace NGKBusi.Models
{

    public class NGKViewModel
    {
        public ICollection<Users> Users { get; set; }
        public ICollection<Menus> Menus { get; set; }
        //public ICollection<Section> Section { get; set; }
        public ICollection<Departments> Departments { get; set; }
        public ICollection<Roles> Roles { get; set; }
        public ICollection<Users_Menus_Roles> Users_Menus_Roles { get; set; }
        public ICollection<IT_Service_Request_Header> IT_Service_Request_Header { get; set; }
        public ICollection<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }
        public List<IT_Service_Request_Categories> IT_Service_Request_Categories { get; set; }
        public ICollection<IT_Service_Request_Report_List> IT_Service_Request_Report_List { get; set; }
    }

    public class RecursiveMenu
    {
        public int? parentID { get; set; }
        public int? itemCount { get; set; }
        public ICollection<Menus> Menus { get; set; }
    }

    public class RecursiveNSChild
    {
        public int? parentID { get; set; }
        public int? itemCount { get; set; }
        public ICollection<PE_NumberingSystem_NumberingList> PE_NumberingSystem_NumberingList { get; set; }
    }

    public class Users
    {
        [Key]
        [Display(Name = "NIK")]
        [Required(ErrorMessage = "NIK Diperlukan.")]
        public string NIK { get; set; }
        public string Name { get; set; }
        [Display(Name = "Password")]
        [Required(ErrorMessage = "Password Diperlukan.")]
        public string Password { get; set; }
        public string Email { get; set; }
        public string DivisionID { get; set; }
        public string DivisionName { get; set; }
        public string DeptID { get; set; }
        public string DeptName { get; set; }
        public string SectionID { get; set; }
        public string SectionName { get; set; }
        public string SubSectionID { get; set; }
        public string SubSectionName { get; set; }
        public string CostID { get; set; }
        public string CostName { get; set; }
        public string TitleID { get; set; }
        public string TitleName { get; set; }
        [ForeignKey("Users_Position")]
        public string PositionID { get; set; }
        [ForeignKey("FA_LaborCost_Position")]
        public string PositionName { get; set; }
        public string Status { get; set; }
        public DateTime? Birthday { get; set; }

        [ForeignKey("UserNIK")]
        public ICollection<Users_Menus_Roles> Users_Menus_Roles { get; set; }
        [ForeignKey("IssuedBy")]
        public ICollection<IT_Service_Request_Data> IT_SR_IssuedBy { get; set; }
        [ForeignKey("IssuedFor")]
        public ICollection<IT_Service_Request_Data> IT_SR_IssuedFor { get; set; }
        [ForeignKey("ForemanBy")]
        public ICollection<IT_Service_Request_Data> IT_SR_ForemanBy { get; set; }
        [ForeignKey("NIK")]
        public ICollection<Kaizen_Data> Kaizen_Data { get; set; }
        [ForeignKey("userNIK")]
        public ICollection<Kaizen_Group_User> Kaizen_Group_User { get; set; }
        [ForeignKey("userNIK")]
        public ICollection<Kaizen_Data_Implementor> Implementor { get; set; }
        [ForeignKey("Position")]
        public ICollection<FA_Labor_Cost_Master> FA_LaborCost_Position { get; set; }

        public virtual V_Users_Position Users_Position { get; set; }

    }

    public class V_Users_Active
    {
        [Key]
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string DivisionID { get; set; }
        public string DivisionName { get; set; }
        public string DeptID { get; set; }
        public string DeptName { get; set; }
        public string SectionID { get; set; }
        public string SectionName { get; set; }
        public string SubSectionID { get; set; }
        public string SubSectionName { get; set; }
        public string CostID { get; set; }
        //[ForeignKey("Section_AX")]
        public string CostName { get; set; }
        public string AXCostID { get; set; }
        public string AXCostName { get; set; }
        public string TitleID { get; set; }
        public string TitleName { get; set; }
        [ForeignKey("Users_Position")]
        public string PositionID { get; set; }
        public string PositionName { get; set; }
        public string Status { get; set; }
        public DateTime? Birthday { get; set; }
        public string RoleName { get; set; }

        public virtual V_Users_Position Users_Position { get; set; }
        //[ForeignKey("COSTNAME")]
        //public virtual Users_Section_AX Section_AX { get; set; }
    }

    public class Users_Auth_Logs
    {
        [Key]
        public Int64 ID { get; set; } // Use Int64 (BigInt) as logs grow specific fast

        [ForeignKey("User")]
        public string NIK { get; set; }
        public string Name { get; set; }
        public DateTime LogDate { get; set; }
        public string IPAddress { get; set; }
        public string PCName { get; set; }
        public string Browser { get; set; } // Helps identify device/browser type
        public string SessionID { get; set; }
        public bool IsSuccess { get; set; } // True = Login OK, False = Failed
        public string Remarks { get; set; } // Useful for tracking specific sessions

        public virtual Users User { get; set; }
    }

    public class FA_Labor_Cost_Employee_List
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Division { get; set; }
        public string CostName { get; set; }
        public string PositionName { get; set; }
        public string Status { get; set; }
        public string New_Section_Code { get; set; }
        public string Remark { get; set; }
    }

    //public class Section
    //{
    //    [Key]
    //    public string SectionID { get; set; }
    //    public string Name { get; set; }

    //    [ForeignKey("SectionID")]
    //    public ICollection<Users> Users { get; set; }
    //    [ForeignKey("SectionID")]
    //    public ICollection<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }
    //}

    public class Departments
    {
        [Key]
        public string id { get; set; }
        public string Name { get; set; }

        [ForeignKey("DeptID")]
        public ICollection<Users> Users { get; set; }
        [ForeignKey("DeptID")]
        public ICollection<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }
    }

    public class Roles
    {
        [ForeignKey("Users_Menus_Roles")]
        public int id { get; set; }
        public string name { get; set; }
        //Log ========================================
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedIP { get; set; }
        public string CreatedPC { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedIP { get; set; }
        public string UpdatedPC { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string DeletedIP { get; set; }
        public string DeletedPC { get; set; }
        //======================================== Log

        public ICollection<Users_Menus_Roles> Users_Menus_Roles { get; set; }
    }

    public class Menus
    {
        [ForeignKey("Users_Menus_Roles")]
        public int id { get; set; }
        public int sequence { get; set; }
        public string name { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public string area { get; set; }
        public string redirect { get; set; }
        public int? parentID { get; set; }
        public string icon { get; set; }
        //Log ========================================
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedIP { get; set; }
        public string CreatedPC { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedIP { get; set; }
        public string UpdatedPC { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string DeletedIP { get; set; }
        public string DeletedPC { get; set; }
        //======================================== Log
        public bool isActive { get; set; }
        public bool isGlobal { get; set; }
        public bool isShow { get; set; }
        public bool isExternal { get; set; }

        public ICollection<Users_Menus_Roles> Users_Menus_Roles { get; set; }
    }

    public class Users_Menus_Roles
    {
        public int id { get; set; }

        [ForeignKey("Users")]
        public string userNIK { get; set; }
        [ForeignKey("Roles")]
        public int? roleID { get; set; }
        [ForeignKey("Menus")]
        public int? menuID { get; set; }
        public string roleIds { get; set; }
        public bool allowInsert { get; set; }
        public bool allowUpdate { get; set; }
        public bool allowDelete { get; set; }
        public bool allowView { get; set; }
        //Log ========================================
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedIP { get; set; }
        public string CreatedPC { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedIP { get; set; }
        public string UpdatedPC { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string DeletedIP { get; set; }
        public string DeletedPC { get; set; }
        //======================================== Log
        public bool isActive { get; set; }

        public virtual Users Users { get; set; }
        public virtual Roles Roles { get; set; }
        public virtual Menus Menus { get; set; }
    }
    public class IT_Service_Request_Header
    {
        [ForeignKey("IT_Service_Request_Data")]
        public int id { get; set; }
        public string DocNo { get; set; }
        [ForeignKey("UsersCreatedBy")]
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedIP { get; set; }
        public string CreatedPC { get; set; }
        [ForeignKey("UsersCheckedBy")]
        public string CheckedBy { get; set; }
        public DateTime? CheckedDate { get; set; }
        public string CheckedIP { get; set; }
        public string CheckedPC { get; set; }
        [ForeignKey("UsersApprovedBy")]
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ApprovedIP { get; set; }
        public string ApprovedPC { get; set; }
        public int? Status { get; set; }

        public ICollection<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }
        public virtual Users UsersCreatedBy { get; set; }
        public virtual Users UsersCheckedBy { get; set; }
        public virtual Users UsersApprovedBy { get; set; }
    }

    public class IT_Service_Request_Data
    {
        public int id { get; set; }
        public string Detail { get; set; }
        [ForeignKey("Departments")]
        public string DeptID { get; set; }
        [ForeignKey("IT_Service_Request_Header")]
        public int? HeaderID { get; set; }
        [ForeignKey("IT_Service_Request_Report_List")]
        public int? ReportID { get; set; }
        [ForeignKey("IT_Service_Request_Categories")]
        public int? CategoryID { get; set; }
        public string Action { get; set; }
        public string Comment { get; set; }
        public DateTime? JobStart { get; set; }
        public DateTime? JobEnd { get; set; }
        public string JobTotal { get; set; }
        //Log ========================================
        [ForeignKey("UsersIssuedBy")]
        public string IssuedBy { get; set; }
        [ForeignKey("UsersIssuedFor")]
        public string IssuedFor { get; set; }
        public DateTime? IssuedDate { get; set; }
        public string IssuedIP { get; set; }
        public string IssuedPC { get; set; }
        [ForeignKey("UsersActionBy")]
        public string ActionBy { get; set; }
        public string ActionIP { get; set; }
        public string ActionPC { get; set; }
        [ForeignKey("UsersForemanBy")]
        public string ForemanBy { get; set; }
        public DateTime? ForemanDate { get; set; }
        public string ForemanIP { get; set; }
        public string ForemanPC { get; set; }
        //======================================== Log
        public int? Rate { get; set; }
        public DateTime? RateDate { get; set; }
        [ForeignKey("IT_Service_Request_Status")]
        public int? StatusID { get; set; }

        public virtual IT_Service_Request_Header IT_Service_Request_Header { get; set; }
        public virtual IT_Service_Request_Report_List IT_Service_Request_Report_List { get; set; }
        public virtual IT_Service_Request_Categories IT_Service_Request_Categories { get; set; }
        public virtual IT_Service_Request_Status IT_Service_Request_Status { get; set; }
        public virtual Departments Departments { get; set; }
        //public virtual Section Section { get; set; }
        public virtual Users UsersIssuedBy { get; set; }
        public virtual Users UsersIssuedFor { get; set; }
        public virtual Users UsersActionBy { get; set; }
        public virtual Users UsersForemanBy { get; set; }
    }

    public class IT_Service_Request_Categories
    {
        [ForeignKey("IT_Service_Request_Data")]
        public int id { get; set; }
        public string Name { get; set; }
        //Log ========================================
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedIP { get; set; }
        public string CreatedPC { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedIP { get; set; }
        public string UpdatedPC { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string DeletedIP { get; set; }
        public string DeletedPC { get; set; }
        public bool? IsActive { get; set; }
        //======================================== Log
        public ICollection<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }
    }

    public class IT_Service_Request_Report_List
    {
        [ForeignKey("IT_Service_Request_Data")]
        public int id { get; set; }
        public string Name { get; set; }
        //Log ========================================
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedIP { get; set; }
        public string CreatedPC { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedIP { get; set; }
        public string UpdatedPC { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string DeletedIP { get; set; }
        public string DeletedPC { get; set; }
        //======================================== Log
        public ICollection<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }
    }

    public class IT_Service_Request_Status
    {
        public int id { get; set; }
        public string Name { get; set; }

        [ForeignKey("StatusID")]
        public ICollection<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }

    }

    public class Master_Roles
    {
        [Key]
        public int RoleID { get; set; }
        public string RoleName { get; set; }
    }

    public class IT_Master_List_Data
    {
        public int id { get; set; }
        public string Asset_No { get; set; }
        [ForeignKey("User")]
        public string NIK { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string computerName { get; set; }
        public string processor { get; set; }
        public string MSOffice { get; set; }
        public string MSOffice_User { get; set; }
        public string RAM { get; set; }
        public string HDD { get; set; }
        public string OS { get; set; }
        public string IP { get; set; }
        public string Mac_Address { get; set; }
        public string Anydesk { get; set; }
        public int? purchase { get; set; }
        public int? month { get; set; }
        public string type { get; set; }
        public bool? is_used { get; set; }
        public string Remark { get; set; }
        public DateTime? created_at { get; set; }
        [ForeignKey("UserCreatedBy")]
        public string created_by { get; set; }
        public DateTime? updated_at { get; set; }
        [ForeignKey("UserUpdatedBy")]
        public string updated_by { get; set; }
        public virtual Users User { get; set; }
        public virtual Users UserCreatedBy { get; set; }
        public virtual Users UserUpdatedBy { get; set; }
    }

    public class PE_NumberingSystem_NumberingList
    {
        public int id { get; set; }
        public string DocNumber { get; set; }
        public string ParentDoc { get; set; }
        public string Remark { get; set; }
        [ForeignKey("UserIssuedBy")]
        public string IssuedBy { get; set; }
        public DateTime? IssuedDate { get; set; }
        public string IssuedIP { get; set; }
        public string IssuedPC { get; set; }

        public virtual Users UserIssuedBy { get; set; }
    }

    public class PE_NumberingSystem_DocList
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class PE_NumberingSystem_DocList_Sub
    {
        public int id { get; set; }
        public int docid { get; set; }
        public string name { get; set; }
    }
    public class PE_NumberingSystem_DocCode
    {
        public int id { get; set; }
        [ForeignKey("PE_NumberingSystem_DocList")]
        public int docid { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string IssuedBy { get; set; }
        public DateTime? IssuedDate { get; set; }
        public string IssuedIP { get; set; }
        public string IssuedPC { get; set; }
        public virtual PE_NumberingSystem_DocList PE_NumberingSystem_DocList { get; set; }
    }
    public class PE_NumberingSystem_Appendix
    {
        public int id { get; set; }
        public string appcode { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string IssuedBy { get; set; }
        public DateTime? IssuedDate { get; set; }
        public string IssuedIP { get; set; }
        public string IssuedPC { get; set; }
    }

    public class PE_NumberingSystem_Products
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
    }

    public class Pages
    {
        public int id { get; set; }
        public int menu_id { get; set; }
        [AllowHtml]
        public string pageContent { get; set; }
        public string updated_by { get; set; }
        public DateTime? updated_date { get; set; }
    }

    public class Events
    {
        public int id { get; set; }
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public string text { get; set; }
        public string color { get; set; }
        public string textColor { get; set; }
        public string type { get; set; }
    }

    public class V_SO_Variance_Check
    {
        [Key]
        public Int64 ID { get; set; }
        public string SectionID { get; set; }
        public string SectionName { get; set; }
        public string Product { get; set; }
        public string Raw { get; set; }
        public string CutOff_PrimeStockID { get; set; }
        public string SO_PrimeStockID { get; set; }
        public string Description { get; set; }
        public decimal? CutOff_Quantity { get; set; }
        public double? SO_Quantity { get; set; }
        public double? variance { get; set; }

    }

    public class HC_SMSGateway_Phone_Book
    {
        [Key]
        public int ID { get; set; }
        public string Phone { get; set; }
        public string Name { get; set; }
    }

    public class Kaizen_Data
    {
        [Key]
        public int ID { get; set; }
        public string RegNo { get; set; }
        public string improveType { get; set; }
        [ForeignKey("issuedUser")]
        public string NIK { get; set; }
        public string Division { get; set; }
        public string Dept { get; set; }
        public string Section { get; set; }
        public string SubSection { get; set; }
        [ForeignKey("leader")]
        public string lineLeader { get; set; }
        public DateTime issuedDate { get; set; }
        public string Title { get; set; }
        public string processName { get; set; }
        public string Area { get; set; }
        public string ideaType { get; set; }
        public double? OCDScore { get; set; }
        public DateTime? OCDDate { get; set; }
        [ForeignKey("OCDUser")]
        public string OCDBy { get; set; }
        public DateTime? OCDReviseDate { get; set; }
        [ForeignKey("OCDReviser")]
        public string OCDRevise { get; set; }
        public double? KOCScore { get; set; }
        public DateTime? KOCDate { get; set; }
        public string KOCNote { get; set; }
        [ForeignKey("KOCUser")]
        public string KOCBy { get; set; }
        public DateTime? KOCReviseDate { get; set; }
        [ForeignKey("KOCReviser")]
        public string KOCRevise { get; set; }
        public double? SCScore { get; set; }
        public DateTime? SCDate { get; set; }
        public string SCNote { get; set; }
        [ForeignKey("SCUser")]
        public string SCBy { get; set; }
        [ForeignKey("SCReviser")]
        public string SCRevise { get; set; }
        public double? Reward { get; set; }
        public bool? hasRewarded { get; set; }
        public double? CostMaterial { get; set; }
        public double? CostServices { get; set; }
        public string CostOtherDesc { get; set; }
        public double? CostOther { get; set; }
        public double? CostTotal { get; set; }
        public string BenefitProductType { get; set; }
        public int? BenefitPeriod { get; set; }
        public int? BenefitQtyPcs { get; set; }
        public double? BenefitQty { get; set; }
        public int? BenefitProcessTime { get; set; }
        public double? BenefitProcess { get; set; }
        public string BenefitOtherDesc { get; set; }
        public double? BenefitOther { get; set; }
        public double? BenefitTotal { get; set; }
        public double? CostBenefitTotal { get; set; }
        public bool? hasImplement { get; set; }
        [AllowHtml]
        public string implementContent { get; set; }
        public bool? Has_Feedback { get; set; }



        public virtual Users issuedUser { get; set; }
        public virtual Users OCDUser { get; set; }
        public virtual Users KOCUser { get; set; }
        public virtual Users SCUser { get; set; }
        public virtual Users OCDReviser { get; set; }
        public virtual Users KOCReviser { get; set; }
        public virtual Users SCReviser { get; set; }
        public virtual Users leader { get; set; }

        [ForeignKey("dataID")]
        public ICollection<Kaizen_Score> ScoreList { get; set; }
        [ForeignKey("dataID")]
        public ICollection<Kaizen_Data_Implementor> implementorList { get; set; }
    }

    public class Kaizen_Data_Implementor
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("data")]
        public int dataID { get; set; }
        [ForeignKey("implementor")]
        public string userNIK { get; set; }


        public virtual Kaizen_Data data { get; set; }
        public virtual Users implementor { get; set; }
    }

    public class Kaizen_Group
    {
        [Key]
        public int ID { get; set; }
        public string ABBR { get; set; }
        public string Name { get; set; }

        [ForeignKey("groupID")]
        public ICollection<Kaizen_Score_Categories> Kaizen_Score_Categories { get; set; }
    }
    public class Kaizen_Group_User
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("Users")]
        public string userNIK { get; set; }
        [ForeignKey("Kaizen_Group")]
        public int groupID { get; set; }
        [ForeignKey("Kaizen_Score_Categories")]
        public int? catID { get; set; }

        public virtual Users Users { get; set; }
        public virtual Kaizen_Group Kaizen_Group { get; set; }
        public virtual Kaizen_Score_Categories Kaizen_Score_Categories { get; set; }
    }
    public class Kaizen_Master_CostBenefit_CPP
    {
        [Key]
        public int ID { get; set; }
        public string Area { get; set; }
        public double? CPP { get; set; }
        public DateTime? Start_Date { get; set; }
        public DateTime? Created_at { get; set; }
        [ForeignKey("Created")]
        public string Created_by { get; set; }
        public DateTime? Modified_at { get; set; }
        [ForeignKey("Modified")]
        public string Modified_by { get; set; }

        public virtual Users Created { get; set; }
        public virtual Users Modified { get; set; }
    }
    public class Kaizen_Master_CostBenefit_UMP
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public int UMP { get; set; }
        public DateTime? Created_at { get; set; }
        [ForeignKey("Created")]
        public string Created_by { get; set; }
        public DateTime? Modified_at { get; set; }
        [ForeignKey("Modified")]
        public string Modified_by { get; set; }

        public virtual Users Created { get; set; }
        public virtual Users Modified { get; set; }
    }

    public class Kaizen_Score_Categories
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        [ForeignKey("Kaizen_Group")]
        public int groupID { get; set; }

        public virtual Kaizen_Group Kaizen_Group { get; set; }
        [ForeignKey("catID")]
        public ICollection<Kaizen_Score_Categories_Sub> Kaizen_Score_Categories_Sub { get; set; }
    }

    public class Kaizen_Score_Categories_Sub
    {
        [Key]
        public int ID { get; set; }
        public string Question { get; set; }
        public double Score { get; set; }
        [ForeignKey("Kaizen_Score_Categories")]
        public int catID { get; set; }

        public virtual Kaizen_Score_Categories Kaizen_Score_Categories { get; set; }
    }
    public class Kaizen_Score
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("Kaizen_Data")]
        public int dataID { get; set; }
        public int catID { get; set; }
        public int subCatID { get; set; }
        public double? Score { get; set; }

        public virtual Kaizen_Data Kaizen_Data { get; set; }
    }
    public class FA_Labor_Cost_Master
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string Position { get; set; }
        public int? Basic_Salary { get; set; }
        public int? Transportation { get; set; }
        public int? Medical { get; set; }
        public int? Transportation_Daily { get; set; }
        public int? Allowance_Job { get; set; }
        public int? Shift { get; set; }
        public int? Insentive_Kehadiran { get; set; }
        public int? Insentive_2S3G { get; set; }
        public int? Allowance_Meal { get; set; }
        public int? Allowance_Skill { get; set; }
        public int? ATM { get; set; }
    }
    public class FA_Labor_Cost_Increment_Factor
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string Position { get; set; }
        public string Basic_Salary { get; set; }
        public string Transportation { get; set; }
        public string Medical { get; set; }
        public string Transportation_Daily { get; set; }
        public string Allowance_Job { get; set; }
        public string Shift { get; set; }
        public string Insentive_Kehadiran { get; set; }
        public string Insentive_2S3G { get; set; }
        public string Allowance_Meal { get; set; }
        public string Allowance_Skill { get; set; }
        public string ATM { get; set; }
    }
    public class FA_Labor_Cost_List
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public int? Basic_Salary { get; set; }
        public int? Medical { get; set; }
        public int? Transportation { get; set; }
        public int? Fix_Income { get; set; }
        public int? Transportation_Daily { get; set; }
        public int? Allowance_Job { get; set; }
        public int? Allowance_Meal { get; set; }
        public int? Insentive_Kehadiran { get; set; }
        public int? Insentive_2S3G { get; set; }
        public int? Overtime { get; set; }
        public int? Shift { get; set; }
        public int? THR_TAT { get; set; }
        public int? Rapel { get; set; }
        public int? Other { get; set; }
        public int? ATM { get; set; }
        public int? Allowance_Skill { get; set; }
        public int? PPH21 { get; set; }
        public int? Unfix_Income { get; set; }
        public int? Gross_Income { get; set; }
    }
    public class FA_Labor_Cost_Rate
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public decimal? BPJS_Kesehatan { get; set; }
        public int? BPJS_Kesehatan_Max { get; set; }
        public decimal? BPJS_JKK_JK { get; set; }
        public decimal? JSHK { get; set; }
        public decimal? Tax_Allowance { get; set; }
        public decimal? BPJS_JHT { get; set; }
        public decimal? BPJS_JP { get; set; }
        public int? BPJS_JP_Max { get; set; }
        public decimal? THR { get; set; }
        public decimal? ALPHA { get; set; }
        public decimal? TAT { get; set; }
        public int? PTKP { get; set; }
        public decimal? Promosi { get; set; }
        public int? Asst_Manager_Overtime { get; set; }
    }

    public class FA_Labor_Cost_Access
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("User")]
        public string NIK { get; set; }
        public bool? isMaster { get; set; }
        public bool? isDataEntry { get; set; }
        public bool? isReport { get; set; }
        public virtual Users User { get; set; }
    }

    public class FA_Labor_Cost_BEL
    {
        [Key]
        public int ID { get; set; }
        public string Related_ID { get; set; }
        public string BEL { get; set; }
        public int Period { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Section { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public string OTP_NIK { get; set; }
        public int? Basic_Salary { get; set; }
        public int? Transportation { get; set; }
        public int? Medical { get; set; }
        public int? Transportation_Daily { get; set; }
        public int? Allowance_Job { get; set; }
        public int? Shift { get; set; }
        public int? Insentive_Kehadiran { get; set; }
        public int? Insentive_2S3G { get; set; }
        public int? Allowance_Meal { get; set; }
        public int? Allowance_Skill { get; set; }
        public int? ATM { get; set; }
        public int? Overtime_Hour { get; set; }
        public decimal? BPJS_Kesehatan { get; set; }
        public decimal? BPJS_JKK_JK { get; set; }
        public decimal? JSHK { get; set; }
        public decimal? Tax_Allowance { get; set; }
        public decimal? Tax { get; set; }
        public decimal? BPJS_JHT { get; set; }
        public decimal? BPJS_JP { get; set; }
        public decimal? THR { get; set; }
        public decimal? ALPHA { get; set; }
        public decimal? TAT { get; set; }
    }

    public class FA_Labor_Cost_GL
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        [ForeignKey("Sect")]
        public string Section { get; set; }
        public string COA_ID { get; set; }
        public string COA_Name { get; set; }
        public string Description { get; set; }
        public decimal? M010 { get; set; }
        public decimal? M011 { get; set; }
        public decimal? M012 { get; set; }
        public decimal? M101 { get; set; }
        public decimal? M102 { get; set; }
        public decimal? M103 { get; set; }
        public decimal? M104 { get; set; }
        public decimal? M105 { get; set; }
        public decimal? M106 { get; set; }
        public decimal? M107 { get; set; }
        public decimal? M108 { get; set; }
        public decimal? M109 { get; set; }
        public decimal? M110 { get; set; }
        public decimal? M111 { get; set; }
        public decimal? M112 { get; set; }
        public decimal? M201 { get; set; }
        public decimal? M202 { get; set; }
        public decimal? M203 { get; set; }
        public decimal? M204 { get; set; }
        public decimal? M205 { get; set; }
        public decimal? M206 { get; set; }
        public decimal? M207 { get; set; }
        public decimal? M208 { get; set; }
        public decimal? M209 { get; set; }
        public decimal? M210 { get; set; }
        public decimal? M211 { get; set; }
        public decimal? M212 { get; set; }
        public decimal? M301 { get; set; }
        public decimal? M302 { get; set; }
        public decimal? M303 { get; set; }
        public decimal? M304 { get; set; }
        public decimal? M305 { get; set; }
        public decimal? M306 { get; set; }
        public decimal? M307 { get; set; }
        public decimal? M308 { get; set; }
        public decimal? M309 { get; set; }
        public decimal? M310 { get; set; }
        public decimal? M311 { get; set; }
        public decimal? M312 { get; set; }
        public decimal? M401 { get; set; }
        public decimal? M402 { get; set; }
        public decimal? M403 { get; set; }
        public string Account_Group { get; set; }
        public string Action_Group_Section { get; set; }

        [ForeignKey("Section_Name")]
        public virtual FA_Section Sect { get; set; }
    }


    public class FA_Labor_Cost_MPP
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int Period { get; set; }
        public string Type { get; set; }
        [ForeignKey("RelatedNIK")]
        public string Related_NIK { get; set; }
        public string Section_Cost { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public DateTime? Date_From { get; set; }
        public DateTime? Date_To { get; set; }
        public string Created_By { get; set; }
        public DateTime? Created_Date { get; set; }
        public string Sign_By { get; set; }
        public DateTime? Sign_Date { get; set; }
        public string Approved_By { get; set; }
        public DateTime? Approved_Date { get; set; }
        public virtual Users RelatedNIK { get; set; }
    }
    public class FA_Labor_Cost_OTP
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string Related_ID { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string OTP_NIK { get; set; }
        public string Section { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public decimal? M010 { get; set; }
        public decimal? M011 { get; set; }
        public decimal? M012 { get; set; }
        public decimal? M101 { get; set; }
        public decimal? M102 { get; set; }
        public decimal? M103 { get; set; }
        public decimal? M104 { get; set; }
        public decimal? M105 { get; set; }
        public decimal? M106 { get; set; }
        public decimal? M107 { get; set; }
        public decimal? M108 { get; set; }
        public decimal? M109 { get; set; }
        public decimal? M110 { get; set; }
        public decimal? M111 { get; set; }
        public decimal? M112 { get; set; }
        public decimal? M201 { get; set; }
        public decimal? M202 { get; set; }
        public decimal? M203 { get; set; }
        public decimal? M204 { get; set; }
        public decimal? M205 { get; set; }
        public decimal? M206 { get; set; }
        public decimal? M207 { get; set; }
        public decimal? M208 { get; set; }
        public decimal? M209 { get; set; }
        public decimal? M210 { get; set; }
        public decimal? M211 { get; set; }
        public decimal? M212 { get; set; }
        public decimal? M301 { get; set; }
        public decimal? M302 { get; set; }
        public decimal? M303 { get; set; }
        public decimal? M304 { get; set; }
        public decimal? M305 { get; set; }
        public decimal? M306 { get; set; }
        public decimal? M307 { get; set; }
        public decimal? M308 { get; set; }
        public decimal? M309 { get; set; }
        public decimal? M310 { get; set; }
        public decimal? M311 { get; set; }
        public decimal? M312 { get; set; }
        public decimal? M401 { get; set; }
        public decimal? M402 { get; set; }
        public decimal? M403 { get; set; }
        public string User_By { get; set; }
        public DateTime? Date_At { get; set; }
        public string Sign_By { get; set; }
        public DateTime? Sign_Date { get; set; }
        public string Approved_By { get; set; }
        public DateTime? Approved_Date { get; set; }
        public string Approved_By2 { get; set; }
        public DateTime? Approved_Date2 { get; set; }
        public string Approved_By3 { get; set; }
        public DateTime? Approved_Date3 { get; set; }
        public string HCSign_By { get; set; }
        public DateTime? HCSign_Date { get; set; }
        public string HCApproved_By { get; set; }
        public DateTime? HCApproved_Date { get; set; }

    }

    public class V_Labor_Cost_OTP
    {
        public int ID { get; set; }
        public int Period { get; set; }
        [Key]
        public string Related_ID { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string OTP_NIK { get; set; }
        public string Section { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public decimal? M010 { get; set; }
        public decimal? M011 { get; set; }
        public decimal? M012 { get; set; }
        public decimal? M101 { get; set; }
        public decimal? M102 { get; set; }
        public decimal? M103 { get; set; }
        public decimal? M104 { get; set; }
        public decimal? M105 { get; set; }
        public decimal? M106 { get; set; }
        public decimal? M107 { get; set; }
        public decimal? M108 { get; set; }
        public decimal? M109 { get; set; }
        public decimal? M110 { get; set; }
        public decimal? M111 { get; set; }
        public decimal? M112 { get; set; }
        public decimal? M201 { get; set; }
        public decimal? M202 { get; set; }
        public decimal? M203 { get; set; }
        public decimal? M204 { get; set; }
        public decimal? M205 { get; set; }
        public decimal? M206 { get; set; }
        public decimal? M207 { get; set; }
        public decimal? M208 { get; set; }
        public decimal? M209 { get; set; }
        public decimal? M210 { get; set; }
        public decimal? M211 { get; set; }
        public decimal? M212 { get; set; }
        public decimal? M301 { get; set; }
        public decimal? M302 { get; set; }
        public decimal? M303 { get; set; }
        public decimal? M304 { get; set; }
        public decimal? M305 { get; set; }
        public decimal? M306 { get; set; }
        public decimal? M307 { get; set; }
        public decimal? M308 { get; set; }
        public decimal? M309 { get; set; }
        public decimal? M310 { get; set; }
        public decimal? M311 { get; set; }
        public decimal? M312 { get; set; }
        public decimal? M401 { get; set; }
        public decimal? M402 { get; set; }
        public decimal? M403 { get; set; }
        public string User_By { get; set; }
        public DateTime? Date_At { get; set; }
        public string Sign_By { get; set; }
        public DateTime? Sign_Date { get; set; }
        public string Approved_By { get; set; }
        public DateTime? Approved_Date { get; set; }
        public string Approved_By2 { get; set; }
        public DateTime? Approved_Date2 { get; set; }
        public string Approved_By3 { get; set; }
        public DateTime? Approved_Date3 { get; set; }
        public string HCSign_By { get; set; }
        public DateTime? HCSign_Date { get; set; }
        public string HCApproved_By { get; set; }
        public DateTime? HCApproved_Date { get; set; }
        public string Action_Group { get; set; }
        public string Group { get; set; }
    }
    public class FA_Labor_Cost_Working_Day
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string Category { get; set; }
        public int? M010 { get; set; }
        public int? M011 { get; set; }
        public int? M012 { get; set; }
        public int? M101 { get; set; }
        public int? M102 { get; set; }
        public int? M103 { get; set; }
        public int? M104 { get; set; }
        public int? M105 { get; set; }
        public int? M106 { get; set; }
        public int? M107 { get; set; }
        public int? M108 { get; set; }
        public int? M109 { get; set; }
        public int? M110 { get; set; }
        public int? M111 { get; set; }
        public int? M112 { get; set; }
        public int? M201 { get; set; }
        public int? M202 { get; set; }
        public int? M203 { get; set; }
        public int? M204 { get; set; }
        public int? M205 { get; set; }
        public int? M206 { get; set; }
        public int? M207 { get; set; }
        public int? M208 { get; set; }
        public int? M209 { get; set; }
        public int? M210 { get; set; }
        public int? M211 { get; set; }
        public int? M212 { get; set; }
        public int? M301 { get; set; }
        public int? M302 { get; set; }
        public int? M303 { get; set; }
        public int? M304 { get; set; }
        public int? M305 { get; set; }
        public int? M306 { get; set; }
        public int? M307 { get; set; }
        public int? M308 { get; set; }
        public int? M309 { get; set; }
        public int? M310 { get; set; }
        public int? M311 { get; set; }
        public int? M312 { get; set; }
        public int? M401 { get; set; }
        public int? M402 { get; set; }
        public int? M403 { get; set; }
        public string User_By { get; set; }
        public DateTime? Date_At { get; set; }
        public string Approved_By { get; set; }
        public DateTime? Approved_At { get; set; }
    }
    public class V_Labor_Cost_List
    {
        [Key]
        [ForeignKey("OTP")]
        public string ID { get; set; }
        public int MPP_ID { get; set; }
        public int Period { get; set; }
        public string Category { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string DeptName { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public DateTime? Date_From { get; set; }
        public DateTime? Date_To { get; set; }
        public string Source { get; set; }
        public string Created_By { get; set; }
        public string Remark { get; set; }

        public virtual V_Labor_Cost_OTP OTP { get; set; }
    }
    public class FA_Section
    {
        [Key]
        public string Section_ID { get; set; }
        public string Section_Name { get; set; }
        public string Action_Group_Section { get; set; }
        public string Account_Group { get; set; }
        public string Group_Section { get; set; }
        public string Segmen { get; set; }
        public int Period { get; set; }
        public string New_Section_Code { get; set; }
        [ForeignKey("approval")]
        public string labor_cost_approval { get; set; }
        [ForeignKey("approval2")]
        public string labor_cost_approval2 { get; set; }
        [ForeignKey("approval3")]
        public string labor_cost_approval3 { get; set; }
        [ForeignKey("sign")]
        public string labor_cost_signed { get; set; }
        public virtual Users sign { get; set; }
        public virtual Users approval { get; set; }
        public virtual Users approval2 { get; set; }
        public virtual Users approval3 { get; set; }
    }

    public class Sliders
    {
        [Key]
        public int ID { get; set; }
        public string Category { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }
        public bool Is_Visible { get; set; }
        public DateTime? Expired_Date { get; set; }
        [ForeignKey("Users")]
        public string User_NIK { get; set; }
        public DateTime? Date_at { get; set; }

        public virtual Users Users { get; set; }
    }
    public class V_Asset
    {
        [Key]
        public string AssetID { get; set; }
        public string AssetType { get; set; }
        public string Name { get; set; }
        public string Descr { get; set; }
        public string Section { get; set; }
        public string Location { get; set; }
        public string EntryDate { get; set; }
        public decimal AccValue { get; set; }
        public string AssetNo { get; set; }
        public string updatedDescr { get; set; }
        public string updatedSection { get; set; }
        public string updatedLocation { get; set; }
        [ForeignKey("Users")]
        public string updatedUser { get; set; }
        public DateTime? updatedDate { get; set; }

        public virtual Users Users { get; set; }
    }
    public class Users_Position
    {
        [Key]
        public string PositionID { get; set; }
        public string Position_ID { get; set; }
        public string Position_Name { get; set; }
        public Int64 Position_Ranking { get; set; }
    }
    public class V_Users_Position
    {
        [Key]
        public string PositionID { get; set; }
        public string Position_ID { get; set; }
        public string Position_Name { get; set; }
        public Int64 Position_Ranking { get; set; }
    }
    public class FA_Asset_Location
    {
        [Key]
        public string LocationID { get; set; }
        public string Name { get; set; }
        public string Descr { get; set; }
    }
    public class FA_Asset_Audit
    {
        [Key]
        public int ID { get; set; }
        public int Period { get; set; }
        public string Asset_No { get; set; }
        public string Descr { get; set; }
        public string Section { get; set; }
        public string Location { get; set; }
        public string User_By { get; set; }
        public DateTime Date_At { get; set; }
    }
    public class V_Labor_Cost_MPP_Summary
    {
        [Key]
        public Int64 ID { get; set; }
        public Int32 Period { get; set; }
        public string DeptName { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public int? M010 { get; set; }
        public int? M011 { get; set; }
        public int? M012 { get; set; }
        public int? M101 { get; set; }
        public int? M102 { get; set; }
        public int? M103 { get; set; }
        public int? M104 { get; set; }
        public int? M105 { get; set; }
        public int? M106 { get; set; }
        public int? M107 { get; set; }
        public int? M108 { get; set; }
        public int? M109 { get; set; }
        public int? M110 { get; set; }
        public int? M111 { get; set; }
        public int? M112 { get; set; }
        public int? M201 { get; set; }
        public int? M202 { get; set; }
        public int? M203 { get; set; }
        public int? M204 { get; set; }
        public int? M205 { get; set; }
        public int? M206 { get; set; }
        public int? M207 { get; set; }
        public int? M208 { get; set; }
        public int? M209 { get; set; }
        public int? M210 { get; set; }
        public int? M211 { get; set; }
        public int? M212 { get; set; }
        public int? M301 { get; set; }
        public int? M302 { get; set; }
        public int? M303 { get; set; }
        public int? M304 { get; set; }
        public int? M305 { get; set; }
        public int? M306 { get; set; }
        public int? M307 { get; set; }
        public int? M308 { get; set; }
        public int? M309 { get; set; }
        public int? M310 { get; set; }
        public int? M311 { get; set; }
        public int? M312 { get; set; }
        public int? M401 { get; set; }
        public int? M402 { get; set; }
        public int? M403 { get; set; }
        public string Sign_By { get; set; }
        public string Approved_By { get; set; }
        public string Approved_By2 { get; set; }
        public string Approved_By3 { get; set; }
        public string HCSign_By { get; set; }
        public string HCApproved_By { get; set; }
        public string Action_Group { get; set; }
        public string Group { get; set; }
    }
    public class V_Labor_Cost_OTP_Summary
    {
        [Key]
        public Int64 ID { get; set; }
        public Int32 Period { get; set; }
        public string DeptName { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public decimal? M010 { get; set; }
        public decimal? M011 { get; set; }
        public decimal? M012 { get; set; }
        public decimal? M101 { get; set; }
        public decimal? M102 { get; set; }
        public decimal? M103 { get; set; }
        public decimal? M104 { get; set; }
        public decimal? M105 { get; set; }
        public decimal? M106 { get; set; }
        public decimal? M107 { get; set; }
        public decimal? M108 { get; set; }
        public decimal? M109 { get; set; }
        public decimal? M110 { get; set; }
        public decimal? M111 { get; set; }
        public decimal? M112 { get; set; }
        public decimal? M201 { get; set; }
        public decimal? M202 { get; set; }
        public decimal? M203 { get; set; }
        public decimal? M204 { get; set; }
        public decimal? M205 { get; set; }
        public decimal? M206 { get; set; }
        public decimal? M207 { get; set; }
        public decimal? M208 { get; set; }
        public decimal? M209 { get; set; }
        public decimal? M210 { get; set; }
        public decimal? M211 { get; set; }
        public decimal? M212 { get; set; }
        public decimal? M301 { get; set; }
        public decimal? M302 { get; set; }
        public decimal? M303 { get; set; }
        public decimal? M304 { get; set; }
        public decimal? M305 { get; set; }
        public decimal? M306 { get; set; }
        public decimal? M307 { get; set; }
        public decimal? M308 { get; set; }
        public decimal? M309 { get; set; }
        public decimal? M310 { get; set; }
        public decimal? M311 { get; set; }
        public decimal? M312 { get; set; }
        public decimal? M401 { get; set; }
        public decimal? M402 { get; set; }
        public decimal? M403 { get; set; }
        public string Sign_By { get; set; }
        public string Approved_By { get; set; }
        public string Approved_By2 { get; set; }
        public string Approved_By3 { get; set; }
        public string HCSign_By { get; set; }
        public string HCApproved_By { get; set; }
        public string Action_Group { get; set; }
        public string Group { get; set; }
    }
    public class V_Labor_Cost_MPP_Status
    {
        [Key]
        public Int64 ID { get; set; }
        public int Period { get; set; }
        public string Section { get; set; }
        public int Level_1 { get; set; }
        public int Level_2 { get; set; }
        public int Level_3 { get; set; }
        public int Level_4 { get; set; }
        public int Level_5 { get; set; }
        public int Level_6 { get; set; }
        [ForeignKey("Level_1_User")]
        public string Sign_By { get; set; }
        [ForeignKey("Level_2_User")]
        public string Approved_By { get; set; }
        [ForeignKey("Level_3_User")]
        public string Approved_By2 { get; set; }
        [ForeignKey("Level_4_User")]
        public string Approved_By3 { get; set; }
        [ForeignKey("Level_5_User")]
        public string HCSign_By { get; set; }
        [ForeignKey("Level_6_User")]
        public string HCApproved_By { get; set; }
        public virtual V_Users_Active Level_1_User { get; set; }
        public virtual V_Users_Active Level_2_User { get; set; }
        public virtual V_Users_Active Level_3_User { get; set; }
        public virtual V_Users_Active Level_4_User { get; set; }
        public virtual V_Users_Active Level_5_User { get; set; }
        public virtual V_Users_Active Level_6_User { get; set; }
    }
    
    public class HC_MailNumbering_List
    {
        [Key]
        public int ID { get; set; }
        public string Number { get; set; }
        public DateTime Date { get; set; }
        public string Purpose { get; set; }
        public string Subject { get; set; }
        [ForeignKey("Users")]
        public string User_By { get; set; }
        public virtual Users Users { get; set; }
    }
    public class AX_Item_Master_Class
    {
        [Key]
        public int ID { get; set; }
        public string Item_Group { get; set; }
        public string Category { get; set; }
        public string QID_Code { get; set; }
        public string Definition { get; set; }
    }
    public class AX_Item_Master_Class_Sub
    {
        [Key]
        public int ID { get; set; }
        public int Class_ID { get; set; }
        public int Sequence { get; set; }
        public int? Parent_ID { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int Digit { get; set; }
    }
    public class AX_Bank_Account
    {
        [Key]
        public int id { get; set; }
        public string Bank_Account { get; set; }
        public string Bank_Name { get; set; }
    }
    public class V_AXBankAccount
    {
        [Key]
        public string Bank_Account { get; set; }
        public string Bank_Name { get; set; }
    }
    public class V_AXInventTrans
    {
        [Key]
        public string ITEMID { get; set; }
        public decimal? QTY { get; set; }
        public DateTime DATEPHYSICAL { get; set; }
        public string Status { get; set; }
        public string ReferenceCategoryName { get; set; }
    }
    public class V_AXItemMaster
    {
        [Key]
        public string ITEMID { get; set; }
        public string ProductName { get; set; }
        public string SearchName { get; set; }
        public string SearchNameAll { get; set; }
        public string ItemDescription { get; set; }
        public string ItemGroup { get; set; }
        public string ProductType { get; set; }
        public string ProductSubType { get; set; }
        public string VehicleID { get; set; }
        public decimal? NetWeight { get; set; }
        public decimal? PackingQuantity { get; set; }
        public string UnitPurchase { get; set; }
        public string UnitInventory { get; set; }
        public string ProductCategory { get; set; }
        public string ProCateName { get; set; }
        public string MasterVehicle { get; set; }
        public string SPType { get; set; }
        public string SectionType { get; set; }
        public string Section { get; set; }
    }
    public class AX_ItemMaster
    {
        [Key]
        public string ITEMID { get; set; }
        public string DisplayProductNumber { get; set; }
        public string ProductName { get; set; }
        public string ItemDescription { get; set; }
        public string ItemGroup { get; set; }
    }
    public class AX_COA_to_ItemGroup
    {
        [Key]
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Item_Group { get; set; }
    }
    public class FA_StockOpname_Section
    {
        [Key]
        public string ID { get; set; }
        public string NIK { get; set; }
        public string Warehouse { get; set; }
        public string Section { get; set; }

    }
    public class FA_StockOpname_EndInventory
    {
        [Key]
        public string AX_ItemID { get; set; }
        public string AX_ProductName { get; set; }
        public string AX_ItemDescription { get; set; }
        public string AX_SearchName { get; set; }
        public string AX_NameAlias { get; set; }
        public string Period { get; set; }
        public string Site { get; set; }
        public string Warehouse { get; set; }
        public string Batch_Number { get; set; }
        public Decimal Debit { get; set; }
        public Decimal Credit { get; set; }
        public Decimal Balance { get; set; }
        public Decimal DebitAmount { get; set; }
        public Decimal CreditAmount { get; set; }
        public Decimal BalanceAmount { get; set; }
        public Decimal DebitAmountSTD { get; set; }
        public Decimal CreditAmountSTD { get; set; }
        public Decimal BalanceAmountSTD { get; set; }
    }
    public class FA_StockOpname_StockTake
    {
        [Key]
        public int ID { get; set; }
        public string Period { get; set; }
        public string Warehouse { get; set; }
        public string Batch_Number { get; set; }
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string Product { get; set; }
        public string Unit { get; set; }
        public Decimal Qty { get; set; }
        public string Description { get; set; }
        [ForeignKey("createdBy")]
        public string Created_By { get; set; }
        public DateTime? Created_At { get; set; }
        [ForeignKey("updatedBy")]
        public string Updated_By { get; set; }
        public DateTime? Updated_At { get; set; }
        public virtual Users createdBy { get; set; }
        public virtual Users updatedBy { get; set; }
    }
    public class V_StockOpname_Sample
    {
        public string Location1 { get; set; }
        public string Location2 { get; set; }
        public string Item_Group { get; set; }
        [Key]
        public string AX_ItemID { get; set; }
        public string AX_ProductName { get; set; }
        [ForeignKey("PICBy")]
        public string PIC { get; set; }
        public string Status { get; set; }
        public virtual Users PICBy { get; set; }
    }
    public class V_AXBegInventory
    {
        [Key]
        public int ID { get; set; }
        public string DC { get; set; }
        public string Transaction { get; set; }
        public string Status { get; set; }
        public string AX_ItemID { get; set; }
        public string AX_ProductName { get; set; }
        public string AX_ItemDescription { get; set; }
        public string AX_SearchName { get; set; }
        public string AX_NameAlias { get; set; }
        public string Batch { get; set; }
        public string Site { get; set; }
        public string Warehouse { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public Decimal Debit { get; set; }
        public Decimal Credit { get; set; }
        public Decimal Balance { get; set; }
        public Decimal DebitAmount { get; set; }
        public Decimal CreditAmount { get; set; }
        public Decimal BalanceAmount { get; set; }
        public Decimal DebitAmountSTD { get; set; }
        public Decimal CreditAmountSTD { get; set; }
        public Decimal BalanceAmountSTD { get; set; }
        public DateTime DateCreated { get; set; }
    }
    public class V_StockOpname_Reconcile
    {
        [Key]
        public string Warehouse { get; set; }
        public string Product { get; set; }
        public string ItemGroup { get; set; }
        public string CutOff_ItemID { get; set; }
        public string SO_ItemID { get; set; }
        public string ItemName { get; set; }
        public Decimal CutOff_Quantity { get; set; }
        public Decimal SO_Quantity { get; set; }
        public Decimal Variance { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
    }
    public class AX_GRNumber
    {
        [Key]
        public string PURCHID { get; set; }
        public string PACKINGSLIPID { get; set; }
        public string COSTLEDGERVOUCHER { get; set; }
    }
    public class AX_COA
    {
        [Key]
        public string MAINACCOUNTID { get; set; }
        public string NAME { get; set; }
    }
    public class AX_Currency
    {
        [Key]
        public string CURRENCY_CODE { get; set; }
        public string TXT { get; set; }
    }
    public class AX_Procate
    {
        [Key]
        public string Procate_Code { get; set; }
        public string Procate_Name { get; set; }
    }
    public class AX_Section
    {
        [Key]
        public string Section_Code { get; set; }
        public string Section_Name { get; set; }
    }
    public class AX_Vendor_List
    {
        [Key]
        public string ACCOUNTNUM { get; set; }
        public string VENDGROUP { get; set; }
        public string Name { get; set; }
        public string SearchName { get; set; }
        public bool IsActive { get; set; }
    }
    public class V_AXVendorList
    {
        [Key]
        public string ACCOUNTNUM { get; set; }
        public string VENDGROUP { get; set; }
        public string Name { get; set; }
        public string SearchName { get; set; }
    }
    public class FA_Payment_Users
    {
        [Key]
        public int id { get; set; }
        public string NIK { get; set; }
        public string Section_ID { get; set; }
        public string Section_Name { get; set; }
        public int Levels { get; set; }
    }
    public class FA_Payment_Request_PO
    {
        [Key]
        public int id { get; set; }
        public string Receive_Number { get; set; }
        public string AP_Number { get; set; }
        public DateTime Entry_Date { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Invoice_Number { get; set; }
        public string Third_Party_Id { get; set; }
        public string Third_Party_Name { get; set; }
        public string Currency_Code { get; set; }
        public double? Amount_of_Invoice { get; set; }
        public string Description { get; set; }
        public DateTime? Due_Date { get; set; }
        public string Due_Date_Reason { get; set; }
        [ForeignKey("Users")]
        public string Created_By { get; set; }
        public double? VAT { get; set; }
        public DateTime? Tax_Date { get; set; }
        public string Tax_Number { get; set; }
        public string Tax_Number_17 { get; set; }
        public string WHT_COA_Code { get; set; }
        public string WHT_COA_Name { get; set; }
        public double? WHT_Amount { get; set; }
        public string WHT_Description { get; set; }
        public string Bank { get; set; }
        public DateTime? Created_At { get; set; }
        public int? Approval { get; set; }
        public int? Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int? Payment_ID { get; set; }
        public virtual Users Users { get; set; }
    }
    public class FA_Payment_Request_Non_PO
    {
        [Key]
        public int id { get; set; }
        public string Receive_Number { get; set; }
        public string Settlement_For { get; set; }
        public string Non_Payment_ID { get; set; }
        public string AP_Number { get; set; }
        public DateTime Entry_Date { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Invoice_Number { get; set; }
        public string Third_Party_Id { get; set; }
        public string Third_Party_Name { get; set; }
        public string Currency_Code { get; set; }
        public double? Amount_of_Invoice { get; set; }
        public string Description { get; set; }
        public DateTime? Due_Date { get; set; }
        public string Due_Date_Reason { get; set; }
        public double? VAT { get; set; }
        public DateTime? Tax_Date { get; set; }
        public string Tax_Number { get; set; }
        public string Tax_Number_17 { get; set; }
        public string WHT_COA_Code { get; set; }
        public string WHT_COA_Name { get; set; }
        public double? WHT_Amount { get; set; }
        public string Bank { get; set; }
        public string Payment_Method { get; set; }
        public string Payment_Type { get; set; }
        [ForeignKey("Users")]
        public string Created_By { get; set; }
        public DateTime? Created_At { get; set; }
        public int? Approval { get; set; }
        public int? Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int? Payment_ID { get; set; }
        public bool? Is_Working_Order { get; set; }
        public virtual Users Users { get; set; }
    }

    public class FA_Payment_Request_Non_PO_Sub
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("Non_PO")]
        public int Non_PO_ID { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public double Allocation_Amount { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Procate_Code { get; set; }
        public string Procate_Name { get; set; }
        public string Budget_Number { get; set; }
        public string Budget_Desc { get; set; }
        public string Description { get; set; }
        public double? VAT { get; set; }
        public DateTime? Tax_Date { get; set; }
        public string Tax_Number { get; set; }
        public string Tax_Number_17 { get; set; }
        public virtual FA_Payment_Request_Non_PO Non_PO { get; set; }
    }
    public class FA_Payment_Request_PO_Sub
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("With_PO")]
        public int With_PO_ID { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public double Allocation_Amount { get; set; }
        public string Budget_Number { get; set; }
        public string Budget_Desc { get; set; }
        public virtual FA_Payment_Request_PO With_PO { get; set; }
    }
    public class FA_Payment_Request_Non_Payment
    {
        [Key]
        public int id { get; set; }
        public string Receive_Number { get; set; }
        public string Settlement_For { get; set; }
        public string AP_Number { get; set; }
        public DateTime Entry_Date { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Third_Party_Id { get; set; }
        public string Third_Party_Name { get; set; }
        public string Currency_Code { get; set; }
        public double? Amount_of_Invoice { get; set; }
        public string Description { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Bank { get; set; }
        public string Type { get; set; }
        [ForeignKey("Users")]
        public string Created_By { get; set; }
        public DateTime? Created_At { get; set; }
        public int? Approval { get; set; }
        public int? Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public virtual Users Users { get; set; }
    }
    public class FA_Payment_Request_Non_Payment_Sub
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("Non_PO")]
        public int Non_PO_ID { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public double Allocation_Amount { get; set; }
        public double Credit_Amount { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Procate_Code { get; set; }
        public string Procate_Name { get; set; }
        public string Budget_Number { get; set; }
        public string Budget_Desc { get; set; }
        public string Description { get; set; }
        public virtual FA_Payment_Request_Non_Payment Non_PO { get; set; }
    }
    public class FA_Payment_Request_History
    {
        [Key]
        public int id { get; set; }
        public string Document_Type { get; set; }
        public int Document_Id { get; set; }
        public string Message { get; set; }
        public string Note { get; set; }
        [ForeignKey("Users")]
        public string User_Id { get; set; }
        public DateTime? Date_At { get; set; }
        public virtual Users Users { get; set; }
    }
    public class FA_Payment_Request_Treasury
    {
        [Key]
        public int id { get; set; }
        public string Receive_Number { get; set; }
        public string Type { get; set; }
        public DateTime? Payment_Date { get; set; }
        public string Third_Party_Id { get; set; }
        public string Third_Party_Name { get; set; }
        public string Currency_Code { get; set; }
        public string Bank_Account { get; set; }
        public string Bank_Name { get; set; }
        [ForeignKey("Users")]
        public string Created_By { get; set; }
        public DateTime? Created_At { get; set; }
        public string Payment_File { get; set; }
        public bool? Is_Complete { get; set; }

        public virtual Users Users { get; set; }
    }
    public class FA_Budget_List
    {
        [Key]
        public int id { get; set; }
        public string Period { get; set; }
        public string Dept_Code { get; set; }
        public string Number { get; set; }
        public string Description { get; set; }
    }
    public class FA_Closing_Calendar
    {
        [Key]
        public int id { get; set; }
        public DateTime Closing_Date { get; set; }
        public string Type { get; set; }
    }
    public class FA_Payment_Request_Note
    {
        [Key]
        public int id { get; set; }
        public int Payment_ID { get; set; }
        public string Payment_Type { get; set; }
        public string Note_Type { get; set; }
        public string Note { get; set; }
        [ForeignKey("Users")]
        public string Note_By { get; set; }
        public DateTime Note_At { get; set; }
        public virtual Users Users { get; set; }
    }
    public class Approval_Master
    {
        [Key]
        public int id { get; set; }
        public int Menu_Id { get; set; }
        public int Document_Id { get; set; }
        [ForeignKey("Users")]
        public string User_NIK { get; set; }
        public string Dept_Code { get; set; }
        public string Dept_Name { get; set; }
        public string Title { get; set; }
        public string Header { get; set; }
        public string Label { get; set; }
        public int Levels { get; set; }
        public int Levels_Sub { get; set; }
        public bool? IsEmailOnly { get; set; }
        public bool? IsSkip { get; set; }
        public decimal minAmount { get; set; }
        public virtual Users Users { get; set; }

    }
    public class Approval_List
    {
        [Key]
        public int id { get; set; }
        public string Reveral_ID { get; set; }
        public string Reveral_ID_Sub { get; set; }
        public int Menu_Id { get; set; }
        public int Document_Id { get; set; }
        [ForeignKey("Users")]
        public string User_NIK { get; set; }
        public string Dept_Code { get; set; }
        public string Dept_Name { get; set; }
        public string Title { get; set; }
        public string Header { get; set; }
        public string Label { get; set; }
        public int Levels { get; set; }
        public int Levels_Sub { get; set; }
        public bool? Is_Skip { get; set; }
        public bool? IsEmailOnly { get; set; }
        public virtual Users Users { get; set; }
    }

    public class Approval_History
    {
        [Key]
        public int id { get; set; }
        public int Menu_Id { get; set; }
        public string Menu_Name { get; set; }
        public int Document_Id { get; set; }
        public string Document_Name { get; set; }
        public string Reveral_ID { get; set; }
        public string Reveral_ID_Sub { get; set; }
        public string Title { get; set; }
        public string Header { get; set; }
        public string Label { get; set; }
        public string Note { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? IsReject { get; set; }
        public bool? IsRevise { get; set; }
        public string Status { get; set; }
        public DateTime Created_At { get; set; }
        [ForeignKey("Users")]
        public string Created_By_ID { get; set; }
        public string Created_By_Name { get; set; }
        public virtual Users Users { get; set; }
    }
    public class SCM_FID_List
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("Header")]
        public int Header_ID { get; set; }
        public string Customer_ID { get; set; }
        public string Customer_Name { get; set; }
        public string Item_ID { get; set; }
        public string Item_Name { get; set; }
        public string External_ID { get; set; }
        public string Remark { get; set; }
        public int Value_1 { get; set; }
        public int Value_2 { get; set; }
        public int Value_3 { get; set; }
        public int Value_4 { get; set; }
        public int Value_5 { get; set; }
        public int Value_6 { get; set; }
        public int Value_7 { get; set; }
        public virtual SCM_FID_Header Header { get; set; }
    }
    public class SCM_FID_Header
    {
        [Key]
        public int id { get; set; }
        public string Business_Type_Name { get; set; }
        public int Start_Month { get; set; }
        public int Start_Year { get; set; }
        public DateTime Created_Date { get; set; }
        [ForeignKey("Users")]
        public string Created_By { get; set; }
        public int Levels { get; set; }
        public int Levels_Sub { get; set; }
        public bool is_reject { get; set; }
        public virtual Users Users { get; set; }
        [ForeignKey("Header_ID")]
        public ICollection<SCM_FID_List> List { get; set; }
    }
    public class SCM_KDPart_Shipment_Schedule
    {
        [Key]
        public int id { get; set; }
        public string Origin { get; set; }
        public string Invoice { get; set; }
        public DateTime ETD { get; set; }
        public string Freight { get; set; }
        public string GIMS { get; set; }
        public string PO_Number { get; set; }
        public double? PO_Qty { get; set; }
        public double? Ship_Qty { get; set; }
        public DateTime Original_ETD { get; set; }
        public string Order_Month { get; set; }
    }
    public class FA_BudgetSystem_BEX
    {
        [Key]
        public int id { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string Budget_Type { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string Description { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public Int64? month010 { get; set; }
        public Int64? month011 { get; set; }
        public Int64? month012 { get; set; }
        public Int64? month101 { get; set; }
        public Int64? month102 { get; set; }
        public Int64? month103 { get; set; }
        public Int64? month104 { get; set; }
        public Int64? month105 { get; set; }
        public Int64? month106 { get; set; }
        public Int64? month107 { get; set; }
        public Int64? month108 { get; set; }
        public Int64? month109 { get; set; }
        public Int64? month110 { get; set; }
        public Int64? month111 { get; set; }
        public Int64? month112 { get; set; }
        public Int64? month201 { get; set; }
        public Int64? month202 { get; set; }
        public Int64? month203 { get; set; }
        public Int64? TotalFY1 { get; set; }
        public Int64? TotalFY2 { get; set; }
        public Int64? TotalFY3 { get; set; }
        public Int64? TotalFY4 { get; set; }
        public string Priority_Category { get; set; }
        public string Budget_No { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int Final_Version { get; set; }
    }
    public class FA_BudgetSystem_BEL
    {
        [Key]
        public int id { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string Budget_Type { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string Description { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public Int64? month010 { get; set; }
        public Int64? month011 { get; set; }
        public Int64? month012 { get; set; }
        public Int64? month101 { get; set; }
        public Int64? month102 { get; set; }
        public Int64? month103 { get; set; }
        public Int64? month104 { get; set; }
        public Int64? month105 { get; set; }
        public Int64? month106 { get; set; }
        public Int64? month107 { get; set; }
        public Int64? month108 { get; set; }
        public Int64? month109 { get; set; }
        public Int64? month110 { get; set; }
        public Int64? month111 { get; set; }
        public Int64? month112 { get; set; }
        public Int64? month201 { get; set; }
        public Int64? month202 { get; set; }
        public Int64? month203 { get; set; }
        public Int64? TotalFY1 { get; set; }
        public Int64? TotalFY2 { get; set; }
        public Int64? TotalFY3 { get; set; }
        public Int64? TotalFY4 { get; set; }
        public string Priority_Category { get; set; }
        public string Budget_No { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int Final_Version { get; set; }
    }
    public class FA_BudgetSystem_BIP
    {
        [Key]
        public int id { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string Budget_Type { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Asset_Name { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public Int64 Acquisition_Value { get; set; }
        public int Useful_Life { get; set; }
        public decimal Allocation { get; set; }
        public DateTime Depre_Start { get; set; }
        public string Priority_Code { get; set; }
        public string Budget_Period { get; set; }
        public Int64 Budget_Allocation { get; set; }
        public string Budget_No { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int Final_Version { get; set; }
    }
    public class FA_BudgetSystem_CIP
    {
        [Key]
        public int id { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string Budget_Type { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Asset_Name { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public Int64 Acquisition_Value { get; set; }
        public int Useful_Life { get; set; }
        public decimal Allocation { get; set; }
        public DateTime Depre_Start { get; set; }
        public string Priority_Code { get; set; }
        public string Budget_Period { get; set; }
        public Int64 Budget_Allocation { get; set; }
        public string Budget_No { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int Final_Version { get; set; }
    }
    public class FA_BudgetSystem_CFA
    {
        [Key]
        public int id { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string Budget_Type { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Asset_Name { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public Int64 Acquisition_Value { get; set; }
        public int Useful_Life { get; set; }
        public decimal Allocation { get; set; }
        public DateTime Depre_Start { get; set; }
        public string Priority_Code { get; set; }
        public string Budget_Period { get; set; }
        public Int64 Budget_Allocation { get; set; }
        public string Budget_No { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int Final_Version { get; set; }
    }
    public class FA_BudgetSystem_History
    {
        [Key]
        public int id { get; set; }
        public string Document_Type { get; set; }
        public int Document_Id { get; set; }
        public string Message { get; set; }
        public string Note { get; set; }
        [ForeignKey("Users")]
        public string User_Id { get; set; }
        public DateTime? Date_At { get; set; }
        public virtual Users Users { get; set; }
    }
    public class FA_BudgetSystem_COA_Conversion
    {
        [Key]
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string COA_Code_Conversion { get; set; }
        public string COA_Name_Conversion { get; set; }
    }
    public class V_FA_BudgetSystem_BEX_BEL
    {
        public int id { get; set; }
        public int rel_id { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string Budget_Type { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string Description { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public Int64? month010 { get; set; }
        public Int64? month011 { get; set; }
        public Int64? month012 { get; set; }
        public Int64? month101 { get; set; }
        public Int64? month102 { get; set; }
        public Int64? month103 { get; set; }
        public Int64? month104 { get; set; }
        public Int64? month105 { get; set; }
        public Int64? month106 { get; set; }
        public Int64? month107 { get; set; }
        public Int64? month108 { get; set; }
        public Int64? month109 { get; set; }
        public Int64? month110 { get; set; }
        public Int64? month111 { get; set; }
        public Int64? month112 { get; set; }
        public Int64? month201 { get; set; }
        public Int64? month202 { get; set; }
        public Int64? month203 { get; set; }
        public Int64? TotalFY1 { get; set; }
        public Int64? TotalFY2 { get; set; }
        public Int64? TotalFY3 { get; set; }
        public Int64? TotalFY4 { get; set; }
        public string Priority_Category { get; set; }
        public string Budget_No { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int Latest { get; set; }
        public int Final_Version { get; set; }
    }
    public class V_FA_BudgetSystem_BIP_CIP_CFA
    {
        public int id { get; set; }
        public int rel_id { get; set; }
        public string Period_FY { get; set; }
        public int Period_Year { get; set; }
        public string Budget_Type { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Asset_Name { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public Int64 Acquisition_Value { get; set; }
        public int Useful_Life { get; set; }
        public decimal Allocation { get; set; }
        public DateTime Depre_Start { get; set; }
        public string Priority_Code { get; set; }
        public string Budget_Period { get; set; }
        public Int64 Budget_Allocation { get; set; }
        public string Budget_No { get; set; }
        public DateTime? Created_At { get; set; }
        public string Created_By { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public int Latest { get; set; }
        public int Final_Version { get; set; }
    }
    public class V_FA_BudgetSystem_Approval_Status
    {
        public int ID { get; set; }
        public string Budget_Type { get; set; }
        public int Period { get; set; }
        public string Section_Code { get; set; }
        public string Section_Name { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public int? Version { get; set; }
    }
    public class Sales_EDI_Yamaha_Import
    {
        public int ID { get; set; }
        public string Ext_Item { get; set; }
        public string U_Code { get; set; }
        public string p_f { get; set; }
        public string Order_No { get; set; }
        public DateTime Delivery_Date { get; set; }
        public int Quantity { get; set; }
        public string Sales_Order_OEM { get; set; }
        public string Sales_Order_OES { get; set; }
    }
    public class Sales_EDI_Honda_Import
    {
        public int ID { get; set; }
        public string Ext_Item { get; set; }
        public string Po_Number { get; set; }
        public DateTime Delivery_Date { get; set; }
        public int Quantity { get; set; }
        public string Sales_Order { get; set; }
        public string Third_Party { get; set; }
        public string Gate { get; set; }
    }
    public class V_AXCustExternalItem
    {
        public int ID { get; set; }
        public string EXTERNALITEMID { get; set; }
        public string CUSTVENDRELATION { get; set; }
        public string ITEMID { get; set; }
    }
    public class V_AX_SalesOrder_Upload_Yamaha
    {
        public int ID { get; set; }
        public string AX_Ext_Item { get; set; }
        public string Currency { get; set; }
        public string Cust_Account { get; set; }
        public string Cust_Group { get; set; }
        public Int32? Line_Num { get; set; }
        public string Sales_ID { get; set; }
        public DateTime Shipping_Date { get; set; }
        public string Customer_Ref { get; set; }
        public string ItemID { get; set; }
        public Int32? Sales_Qty { get; set; }
        public Int32? Sales_Price { get; set; }
        public Int32? Line_Disc { get; set; }
        public Int32? Line_Amount { get; set; }
        public string Invent_Location_ID { get; set; }
        public string Invent_Batch_ID { get; set; }
        public string Invent_Site_ID { get; set; }
        public string Default_Dimension { get; set; }
        public Int32? Price_Unit { get; set; }
        public string Tax_Group { get; set; }
        public string Tax_Item_Group { get; set; }
        public string Sales_Status { get; set; }
        public string Sales_Type { get; set; }
        public string Sales_Unit { get; set; }
        public string Name { get; set; }
        public DateTime Receipt_Date_Requested { get; set; }
    }
    public class V_AX_SalesOrder_Upload_Honda
    {
        public int ID { get; set; }
        public string AX_Ext_Item { get; set; }
        public string Currency { get; set; }
        public string Cust_Account { get; set; }
        public string Cust_Group { get; set; }
        public Int32? Line_Num { get; set; }
        public string Sales_ID { get; set; }
        public DateTime Shipping_Date { get; set; }
        public string Customer_Ref { get; set; }
        public string ItemID { get; set; }
        public string Sales_Qty { get; set; }
        public string Sales_Price { get; set; }
        public string Line_Disc { get; set; }
        public string Line_Amount { get; set; }
        public string Invent_Location_ID { get; set; }
        public string Invent_Batch_ID { get; set; }
        public string Invent_Site_ID { get; set; }
        public string Default_Dimension { get; set; }
        public string Price_Unit { get; set; }
        public string Tax_Group { get; set; }
        public string Tax_Item_Group { get; set; }
        public string Sales_Status { get; set; }
        public string Sales_Type { get; set; }
        public string Sales_Unit { get; set; }
        public string Name { get; set; }
        public DateTime Receipt_Date_Requested { get; set; }
    }

    public class Sales_EDI_Yamaha_Package
    {
        public int ID { get; set; }
        public string Cust_Account { get; set; }
        public string U_Code { get; set; }
        public string Ext_Item { get; set; }
        public string ItemID { get; set; }
    }

    public class V_FA_Payment_Request_Budget_List
    {
        public int ID { get; set; }
        public string Period_FY { get; set; }
        public string Budget_Type { get; set; }
        [ForeignKey("ItemGroup")]
        public string COA_Code { get; set; }
        public string COA_Name { get; set; }
        public string Section_From_Code { get; set; }
        public string Section_From_Name { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string Description { get; set; }
        public string Group_Section { get; set; }
        public string Group_Cost { get; set; }
        public string Budget_No { get; set; }
        public virtual AX_COA_to_ItemGroup ItemGroup { get; set; }
    }

    public class HSE_SIK_Form
    {
        public int ID { get; set; }
        public string Number { get; set; }
        public string Third_Party_Code { get; set; }
        public string Third_Party_Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool? Is_Risk { get; set; }
        public bool? Is_Long_Day { get; set; }
        public string Risk_Class { get; set; }
        public string KTP { get; set; }
        public string Badge_Number { get; set; }
        public string MCU_Result { get; set; }
        public string SOP { get; set; }
        public string Expertise_Certificate { get; set; }
        public string AK3U_Certificate { get; set; }
        public string List_Equipment { get; set; }
        public string Job_Safety_Analysis { get; set; }
        public string Worker_Attendance { get; set; }
        public string Others { get; set; }
        public string Worker_Name { get; set; }
        public string Worker_Position { get; set; }
        public string Worker_Photo { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
    }
    public class HSE_SIK_Form_Daily
    {
        public int ID { get; set; }
        public int? SIK_ID { get; set; }
        public string SID { get; set; }
        public string Third_Party_Code { get; set; }
        public string Third_Party_Name { get; set; }
        public string PIC { get; set; }
        public string Job { get; set; }
        public string Location { get; set; }
        public string Dept { get; set; }
        public string Job_Description { get; set; }
        public string Date_From { get; set; }
        public string Date_To { get; set; }
        public string Time_From { get; set; }
        public string Time_To { get; set; }
        public string Worker_Name { get; set; }
        public string Worker_Position { get; set; }
        public string Job_Field { get; set; }
        public string Job_Type { get; set; }
        public string Job_Tools_Name { get; set; }
        public string Job_Tools_Qty { get; set; }
        public string Hot_Jobs { get; set; }
        public string Cold_Jobs { get; set; }
        public string Height_Jobs { get; set; }
        public string Limited_Jobs { get; set; }
        public string Lifting_Jobs { get; set; }
        public string Digging_Jobs { get; set; }
        public string Electric_Jobs { get; set; }
        public string B3_Jobs { get; set; }
        public string Radiation_Jobs { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
    }

    public class HSE_SIK_Form_Daily_Check
    {

        public int ID { get; set; }
        public int? Daily_Form_ID { get; set; }
        public string Is_Protected { get; set; }
        public string Is_Protected_Photo { get; set; }
        public string Is_Protected_Note { get; set; }
        public string Is_Toolsafe { get; set; }
        public string Is_Toolsafe_Photo { get; set; }
        public string Is_Toolsafe_Note { get; set; }
        public string Is_Risk { get; set; }
        public string Is_Risk_Photo { get; set; }
        public string Is_Risk_Note { get; set; }
        public string Is_Document { get; set; }
        public string Is_Document_Photo { get; set; }
        public string Is_Document_Note { get; set; }
        public string Is_Time { get; set; }
        public string Is_Time_Photo { get; set; }
        public string Is_Time_Note { get; set; }
        public string Is_Induction { get; set; }
        public string Is_Induction_Photo { get; set; }
        public string Is_Induction_Note { get; set; }
        public string Is_Uniform { get; set; }
        public string Is_Uniform_Photo { get; set; }
        public string Is_Uniform_Note { get; set; }
        public string Is_Smoke { get; set; }
        public string Is_Smoke_Photo { get; set; }
        public string Is_Smoke_Note { get; set; }
        public string Is_Waste { get; set; }
        public string Is_Waste_Photo { get; set; }
        public string Is_Waste_Note { get; set; }
        public string Is_Video { get; set; }
        public string Is_Video_Photo { get; set; }
        public string Is_Video_Note { get; set; }
        public string Is_Emergency { get; set; }
        public string Is_Emergency_Photo { get; set; }
        public string Is_Emergency_Note { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
    }
    public class HSE_SIK_Form_Finish_Check
    {

        public int ID { get; set; }
        public int SIK_ID { get; set; }
        public string Clean_Photo { get; set; }
        public string Clean_Note { get; set; }
        public string Waste_Photo { get; set; }
        public string Waste_Note { get; set; }
        public string Damage_Photo { get; set; }
        public string Damage_Note { get; set; }
        public string Tools_Photo { get; set; }
        public string Tools_Note { get; set; }
        public DateTime Created_At { get; set; }
        public string Created_By { get; set; }
    }

    public class V_AXCustomerList
    {
        [Key]
        public Int64 RECID { get; set; }
        public string ACCOUNTNUM { get; set; }
        public string CURRENCY { get; set; }
        public string CUSTGROUP { get; set; }
        public string CUSTOMER_NAME { get; set; }
    }
    public class V_AXCustInvoice
    {
        [Key]
        public Int64 RECID { get; set; }
        public string INVOICEID { get; set; }
        public decimal SALESBALANCE { get; set; }
        public decimal SUMTAX { get; set; }
        public string INVOICEACCOUNT { get; set; }
        public string INVOICINGNAME { get; set; }
        public string LEDGERVOUCHER { get; set; }
        public string PURCHASEORDER { get; set; }
        public string CURRENCYCODE { get; set; }
        public DateTime INVOICEDATE { get; set; }
    }
    public class V_AXCustInvoiceTrans
    {
        [Key]
        public Int64 RECID { get; set; }
        public string INVOICEID { get; set; }
        public string ITEMID { get; set; }
        public string EXTERNALITEMID { get; set; }
        public Decimal SALESPRICE { get; set; }
        public Decimal QTY { get; set; }
        public Decimal LINEAMOUNT { get; set; }
        public Decimal DISCAMOUNT { get; set; }
        public Decimal TAXAMOUNT { get; set; }
    }
    public class V_AXCustInvoiceAddress
    {
        [Key]
        public Int64 DIRPARTYLOC { get; set; }
        public string ACCOUNTNUM { get; set; }
        public string LOCATIONNAME { get; set; }
        public string ADDRESS { get; set; }
        public string REGISTRATIONNUMBER { get; set; }
    }
    public class HC_TransferLabor_RequestionSheet
    {
        [Key]
        public int ID { get; set; }
        public string Req_Number { get; set; }
        public int Qty { get; set; }
        public string Section_Code { get; set; }
        public string Section_Name { get; set; }
        public string Division_Name { get; set; }
        public DateTime? JobStart { get; set; }
        public DateTime? JobEnd { get; set; }
        public string Section_To_Code { get; set; }
        public string Section_To_Name { get; set; }
        public string Job { get; set; }
        public string JobType { get; set; }
        public string JobDesc { get; set; }
        [ForeignKey("UserCreatedBy")]
        public string Created_By { get; set; }
        public DateTime Created_At { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public virtual Users UserCreatedBy { get; set; }
    }

    public class Users_Section_AX
    {
        [Key]
        public Int64 ID { get; set; }
        //[ForeignKey("Users_Active")]
        public string COSTNAME { get; set; }
        public string SECTIONTYPE { get; set; }
        public string SECTION { get; set; }
        //[ForeignKey("CostName")]
        //public ICollection<V_Users_Active> Users_Active { get; set; }
    }

    public class Users_Menus_Favorites
    {
        [Key]
        public Int64 ID { get; set; }
        public string UserName { get; set; }
        public int IdMenu { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class V_User_Menu_Favorites
    {
        [Key]
        public Int64 ID { get; set; }
        public string UserName { get; set; }
        public int IdMenu { get; set; }
        public string name { get; set; }
        public string action { get; set; }
        public string controller { get; set; }
        public string area { get; set; }
        public int sequence { get; set; }
        public string redirect { get; set; }
    }

    public class Task_List
    {
        [Key]
        public int ID { get; set; }
        public string TaskName { get; set; }
        public string TaskForUser { get; set; }
        public string ModuleArea { get; set; }
        public string ModuleController { get; set; }
        public string Module { get; set; }
        public byte IsActive { get; set; }
        public string ModuleID { get; set; }
        public string ModuleParameter { get; set; }
    }
    public class Master_Organization
    {
        [Key]
        public int ID { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationUser { get; set; }
        public int ParentID { get; set; }
        public string DeptCode { get; set; }
        public int userLevel { get; set; }
    }

    public class DefaultConnection : DbContext
    {
        public DbSet<Users> Users { get; set; }
        public DbSet<Users_Auth_Logs> Users_Auth_Logs { get; set; }
        //public DbSet<Section> Section { get; set; }
        public DbSet<Departments> Departments { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Menus> Menus { get; set; }
        public DbSet<Pages> Pages { get; set; }
        public DbSet<Events> Events { get; set; }
        public DbSet<Users_Menus_Roles> Users_Menus_Roles { get; set; }
        public DbSet<IT_Master_List_Data> IT_Master_List_Data { get; set; }
        public DbSet<IT_Service_Request_Header> IT_Service_Request_Header { get; set; }
        public DbSet<IT_Service_Request_Data> IT_Service_Request_Data { get; set; }
        public DbSet<IT_Service_Request_Categories> IT_Service_Request_Categories { get; set; }
        public DbSet<IT_Service_Request_Report_List> IT_Service_Request_Report_List { get; set; }
        public DbSet<IT_Service_Request_Status> IT_Service_Request_Status { get; set; }
        public DbSet<PE_NumberingSystem_NumberingList> PE_NumberingSystem_NumberingList { get; set; }
        public DbSet<PE_NumberingSystem_DocList> PE_NumberingSystem_DocList { get; set; }
        public DbSet<PE_NumberingSystem_DocList_Sub> PE_NumberingSystem_DocList_Sub { get; set; }
        public DbSet<PE_NumberingSystem_Appendix> PE_NumberingSystem_Appendix { get; set; }
        public DbSet<PE_NumberingSystem_DocCode> PE_NumberingSystem_DocCode { get; set; }
        public DbSet<PE_NumberingSystem_Products> PE_NumberingSystem_Products { get; set; }
        public DbSet<HC_SMSGateway_Phone_Book> HC_SMSGateway_Phone_Book { get; set; }
        public DbSet<V_SO_Variance_Check> V_SO_Variance_Check { get; set; }
        public DbSet<V_Users_Active> V_Users_Active { get; set; }
        public DbSet<Kaizen_Data> Kaizen_Data { get; set; }
        public DbSet<Kaizen_Data_Implementor> Kaizen_Data_Implementor { get; set; }
        public DbSet<Kaizen_Group> Kaizen_Group { get; set; }
        public DbSet<Kaizen_Group_User> Kaizen_Group_User { get; set; }
        public DbSet<Kaizen_Master_CostBenefit_CPP> Kaizen_Master_CostBenefit_CPP { get; set; }
        public DbSet<Kaizen_Master_CostBenefit_UMP> Kaizen_Master_CostBenefit_UMP { get; set; }
        public DbSet<Kaizen_Score> Kaizen_Score { get; set; }
        public DbSet<Kaizen_Score_Categories> Kaizen_Score_Categories { get; set; }
        public DbSet<Kaizen_Score_Categories_Sub> Kaizen_Score_Categories_Sub { get; set; }
        public DbSet<FA_Section> FA_Section { get; set; }
        public DbSet<FA_Asset_Location> FA_Asset_Location { get; set; }
        public DbSet<FA_Asset_Audit> FA_Asset_Audit { get; set; }
        public DbSet<FA_Labor_Cost_Master> FA_Labor_Cost_Master { get; set; }
        public DbSet<FA_Labor_Cost_Increment_Factor> FA_Labor_Cost_Increment_Factor { get; set; }
        public DbSet<FA_Labor_Cost_Rate> FA_Labor_Cost_Rate { get; set; }
        public DbSet<FA_Labor_Cost_MPP> FA_Labor_Cost_MPP { get; set; }
        public DbSet<FA_Labor_Cost_OTP> FA_Labor_Cost_OTP { get; set; }
        public DbSet<FA_Labor_Cost_List> FA_Labor_Cost_List { get; set; }
        public DbSet<FA_Labor_Cost_Working_Day> FA_Labor_Cost_Working_Day { get; set; }
        public DbSet<FA_StockOpname_Section> FA_StockOpname_Section { get; set; }
        public DbSet<FA_StockOpname_EndInventory> FA_StockOpname_EndInventory { get; set; }
        public DbSet<FA_StockOpname_StockTake> FA_StockOpname_StockTake { get; set; }
        public DbSet<V_StockOpname_Sample> V_StockOpname_Sample { get; set; }
        public DbSet<Sliders> Sliders { get; set; }
        public DbSet<Users_Position> Users_Position { get; set; }
        public DbSet<V_Asset> V_Asset { get; set; }
        public DbSet<V_Users_Position> V_Users_Position { get; set; }
        public DbSet<V_Labor_Cost_List> V_Labor_Cost_List { get; set; }
        public DbSet<V_Labor_Cost_MPP_Summary> V_Labor_Cost_MPP_Summary { get; set; }
        public DbSet<V_Labor_Cost_OTP_Summary> V_Labor_Cost_OTP_Summary { get; set; }
        public DbSet<V_Labor_Cost_OTP> V_Labor_Cost_OTP { get; set; }
        public DbSet<FA_Labor_Cost_Employee_List> FA_Labor_Cost_Employee_List { get; set; }
        public DbSet<FA_Labor_Cost_BEL> FA_Labor_Cost_BEL { get; set; }
        public DbSet<FA_Labor_Cost_GL> FA_Labor_Cost_GL { get; set; }
        public DbSet<V_Labor_Cost_MPP_Status> V_Labor_Cost_MPP_Status { get; set; }
        public DbSet<FA_Labor_Cost_Access> FA_Labor_Cost_Access { get; set; }
        public DbSet<HC_MailNumbering_List> HC_MailNumbering_List { get; set; }
        public DbSet<AX_Item_Master_Class> AX_Item_Master_Class { get; set; }
        public DbSet<AX_Item_Master_Class_Sub> AX_Item_Master_Class_Sub { get; set; }
        public DbSet<V_AXItemMaster> V_AXItemMaster { get; set; }
        public DbSet<V_AXBegInventory> V_AXBegInventory { get; set; }
        public DbSet<V_StockOpname_Reconcile> V_StockOpname_Reconcile { get; set; }
        public DbSet<AX_GRNumber> AX_GRNumber { get; set; }
        public DbSet<AX_COA> AX_COA { get; set; }
        public DbSet<AX_Currency> AX_Currency { get; set; }
        public DbSet<AX_Procate> AX_Procate { get; set; }
        public DbSet<AX_Section> AX_Section { get; set; }
        public DbSet<AX_Vendor_List> AX_Vendor_List { get; set; }
        public DbSet<V_AXVendorList> V_AXVendorList { get; set; }
        public DbSet<AX_Bank_Account> AX_Bank_Account { get; set; }
        public DbSet<V_AXBankAccount> V_AXBankAccount { get; set; }
        public DbSet<V_AXInventTrans> V_AXInventTrans { get; set; }
        public DbSet<FA_Payment_Request_PO> FA_Payment_Request_PO { get; set; }
        public DbSet<FA_Payment_Request_PO_Sub> FA_Payment_Request_PO_Sub { get; set; }
        public DbSet<FA_Payment_Request_Non_PO> FA_Payment_Request_Non_PO { get; set; }
        public DbSet<FA_Payment_Request_Non_PO_Sub> FA_Payment_Request_Non_PO_Sub { get; set; }
        public DbSet<FA_Payment_Request_Non_Payment> FA_Payment_Request_Non_Payment { get; set; }
        public DbSet<FA_Payment_Request_Non_Payment_Sub> FA_Payment_Request_Non_Payment_Sub { get; set; }
        public DbSet<FA_Payment_Request_Treasury> FA_Payment_Request_Treasury { get; set; }
        public DbSet<FA_Payment_Request_Note> FA_Payment_Request_Note { get; set; }
        public DbSet<FA_Payment_Request_History> FA_Payment_Request_History { get; set; }
        public DbSet<FA_Payment_Users> FA_Payment_Users { get; set; }
        public DbSet<FA_Closing_Calendar> FA_Closing_Calendar { get; set; }
        public DbSet<FA_Budget_List> FA_Budget_List { get; set; }
        public DbSet<Approval_Master> Approval_Master { get; set; }
        public DbSet<Approval_List> Approval_List { get; set; }
        public DbSet<Approval_History> Approval_History { get; set; }
        public DbSet<SCM_FID_Header> SCM_FID_Header { get; set; }
        public DbSet<SCM_FID_List> SCM_FID_List { get; set; }
        public DbSet<SCM_KDPart_Shipment_Schedule> SCM_KDPart_Shipment_Schedule { get; set; }
        public DbSet<FA_BudgetSystem_BEX> FA_BudgetSystem_BEX { get; set; }
        public DbSet<FA_BudgetSystem_BEL> FA_BudgetSystem_BEL { get; set; }
        public DbSet<FA_BudgetSystem_BIP> FA_BudgetSystem_BIP { get; set; }
        public DbSet<FA_BudgetSystem_CIP> FA_BudgetSystem_CIP { get; set; }
        public DbSet<FA_BudgetSystem_CFA> FA_BudgetSystem_CFA { get; set; }
        public DbSet<V_FA_BudgetSystem_BEX_BEL> V_FA_BudgetSystem_BEX_BEL { get; set; }
        public DbSet<V_FA_BudgetSystem_BIP_CIP_CFA> V_FA_BudgetSystem_BIP_CIP_CFA { get; set; }
        public DbSet<V_FA_BudgetSystem_Approval_Status> V_FA_BudgetSystem_Approval_Status { get; set; }
        public DbSet<FA_BudgetSystem_History> FA_BudgetSystem_History { get; set; }
        public DbSet<FA_BudgetSystem_COA_Conversion> FA_BudgetSystem_COA_Conversion { get; set; }
        public DbSet<Sales_EDI_Yamaha_Import> Sales_EDI_Yamaha_Import { get; set; }
        public DbSet<V_AXCustExternalItem> V_AXCustExternalItem { get; set; }
        public DbSet<V_AX_SalesOrder_Upload_Yamaha> V_AX_SalesOrder_Upload_Yamaha { get; set; }
        public DbSet<V_AX_SalesOrder_Upload_Honda> V_AX_SalesOrder_Upload_Honda { get; set; }
        public DbSet<Sales_EDI_Yamaha_Package> Sales_EDI_Yamaha_Package { get; set; }
        public DbSet<Sales_EDI_Honda_Import> Sales_EDI_Honda_Import { get; set; }
        public DbSet<V_FA_Payment_Request_Budget_List> V_FA_Payment_Request_Budget_List { get; set; }
        public DbSet<HSE_SIK_Form> HSE_SIK_Form { get; set; }
        public DbSet<HSE_SIK_Form_Daily> HSE_SIK_Form_Daily { get; set; }
        public DbSet<HSE_SIK_Form_Daily_Check> HSE_SIK_Form_Daily_Check { get; set; }
        public DbSet<HSE_SIK_Form_Finish_Check> HSE_SIK_Form_Finish_Check { get; set; }
        public DbSet<V_AXCustomerList> V_AXCustomerList { get; set; }
        public DbSet<V_AXCustInvoice> V_AXCustInvoice { get; set; }
        public DbSet<V_AXCustInvoiceTrans> V_AXCustInvoiceTrans { get; set; }
        public DbSet<V_AXCustInvoiceAddress> V_AXCustInvoiceAddress { get; set; }
        public DbSet<HC_TransferLabor_RequestionSheet> HC_TransferLabor_RequestionSheet { get; set; }
        public DbSet<Users_Section_AX> Users_Section_AX { get; set; }
        public DbSet<Users_Menus_Favorites> Users_Menus_Favorites { get; set; }
        public DbSet<V_User_Menu_Favorites> V_User_Menu_Favorites { get; set; }
        public DbSet<Task_List> Task_List { get; set; }
        public DbSet<Master_Organization> Master_Organization { get; set; }
        public DbSet<AX_COA_to_ItemGroup> AX_COA_to_ItemGroup { get; set; }
        public DbSet<Master_Roles> Master_Roles { get; set; }
        public DbSet<Task_Approval_List> Task_Approval_Lists { get; set; }
    }

    [Table("Task_Approval_List")]
    public class Task_Approval_List
    {
        public Task_Approval_List()
        {
            CreatedDate = DateTime.Now;
            IsCompleteTask = false;
            TaskType = "Approval";
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public string NIK { get; set; }

        [Required]
        public int Menu_Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Module { get; set; }

        [Required]
        [StringLength(100)]
        public string Referal_id { get; set; }

        [Required]
        public DateTime ApprovalDeadline { get; set; }

        [Required]
        public bool IsCompleteTask { get; set; }

        public DateTime? ApprovalDate { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [StringLength(500)]
        public string DetailUrl { get; set; }

        [Required]
        [StringLength(100)]
        public string TaskType { get; set; }

        [StringLength(255)]
        public string TaskTitle { get; set; }

        public int? DocumentID { get; set; }

        [ForeignKey("Menu_Id")]
        public virtual Menus Menu { get; set; }
    }
}