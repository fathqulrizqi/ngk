using System;
using NGKBusi.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace NGKBusi.Areas.HC.Models
{
    public class Inventory
    {
    }

    public class HC_GA_Inventory_Category
    {
        [Key]
        public int ID { get; set; }
        public string Category_Name { get; set; }
    }

    public class HC_GA_Inventory_Subcategory
    {
        [Key]
        public int ID { get; set; }
        public int Category_Id { get; set; }
        public string Subcategory_Name { get; set; }
        public string Subcategory_Image { get; set; }
    }
    public class HC_GA_Inventory_Item_List
    {
        [Key]
        public int ID { get; set; }
        public string Item_Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public int Qty { get; set; }
        public string Unit { get; set; }
        public int Category_Id { get; set; }
        public string Image { get; set; }
        public int Subcategory_Id { get; set; }
        public string User_NIK { get; set; }
        public DateTime Timestamps { get; set; }
        public string Ref_Code { get; set; }
        public decimal Price { get; set; }
        public DateTime? Last_Price_Update { get; set; }
    }

    public class HC_GA_Inventory_Carts
    {
        [Key]
        public int ID { get; set; }
        public string Cart_No { get; set; }
        public int Item_Id { get; set; }
        public string Item_Name { get; set; }
        public int Qty_Request { get; set; }
        public string Unit_Request { get; set; }
        public string Budget_No { get; set; }
        public string Budget_Desc { get; set; }
        public string User_NIK { get; set; }
        public int Status { get; set; }
        public DateTime Timestamps { get; set; }
        public string Notes { get; set; }
        public string User_Name { get; set; }
        public string Dept_Name { get; set; }
        public string Section_Name { get; set; }
        public string Division_Name { get; set; }
        public int? Qty_Acc { get; set; }
        public string Request_No { get; set; }
        public string Section_To_Name { get; set; }
        public string Section_To_Code { get; set; }
        public bool isReady { get; set; }
        public decimal? Price { get; set; }
        public DateTime? Pick_Date { get; set; }
    }

    public class HC_GA_Inventory_Requests
    {
        [Key]
        public int ID { get; set; }
        public string Request_No { get; set; }
        public string Cart_No { get; set; }
        public string Subject { get; set; }
        public DateTime Due_Date { get; set; }
        public string Reason_Message { get; set; }
        public string User_NIK { get; set; }
        public string User_Name { get; set; }
        public string Dept_Name { get; set; }
        public string Status { get; set; }
        public DateTime Timestamps { get; set; }
        public string Dept_Request { get; set; }
        public string Invoice_Number { get; set; }
        public DateTime? Invoice_Date { get; set; }
        public string Receive_Number { get; set; }
    }

    public class HC_GA_Inventory_Requests_Approval
    {
        [Key]
        public int ID { get; set; }
        public string Request_No { get; set; }
        public string Prepared_NIK { get; set; }
        public string Prepared_Name { get; set; }
        public DateTime? Prepared_Date { get; set; }
        public string Approved_NIK { get; set; }
        public string Approved_Name { get; set; }
        public DateTime? Approved_Date { get; set; }
        public string Checked_NIK { get; set; }
        public string Checked_Name { get; set; }
        public DateTime? Checked_Date { get; set; }
    }

    public class HC_GA_Inventory_Request_Confirm
    {
        [Key]
        public int ID { get; set; }
        public string Request_No { get; set; }
        public string Cart_No { get; set; }
        public int Item_Id { get; set; }
        public string Item_Name { get; set; }
        public int Qty_Request { get; set; }
        public int Qty_Acc { get; set; }
        public string User_NIK { get; set; }
        public string User_NIK_Acc { get; set; }
        public string User_NIK_Acc_Name { get; set; }
        public string Manager_NIK { get; set; }
        public string Manager_Name { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime Timestamps { get; set; }
        public string Date_Prepare { get; set; }
        public string Date_Approve { get; set; }
    }

    public class HC_GA_Inventory_Order
    {
        [Key]
        public int ID { get; set; }
        public string Order_No { get; set; }
        public string Type { get; set; }
        public string Request_No { get; set; }
        public string Cart_No { get; set; }
        public int Item_Order_Id { get; set; }
        public string Item_Order_Name { get; set; }
        public int Item_Order_Qty { get; set; }
        public string Item_Order_Unit { get; set; }
        public string Requestor_NIK { get; set; }
        public string Requestor_Name { get; set; }
        public string Requestor_Dept { get; set; }
        public string Status { get; set; }
        public DateTime Timestamps { get; set; }
        public string Prepare_NIK { get; set; }
        public string Prepare_Name { get; set; }
        public DateTime? Prepare_Date { get; set; }
        public string Check_NIK { get; set; }
        public string Check_Name { get; set; }
        public DateTime? Check_Date { get; set; }
        public string Approve_NIK { get; set; }
        public string Approve_Name { get; set; }
        public DateTime? Approve_Date { get; set; }
        public string Attn { get; set; }
        public string Subject { get; set; }
        public string Reject_Message { get; set; }
        public int? Qty_Kop { get; set; }
        public int? Qty_GA { get; set; }
        public string Dispatcher_NIK { get; set; }
        public string Dispatcher_Name { get; set; }
        public DateTime? Dispatcher_Date { get; set; }
        public string Receiver_NIK { get; set; }
        public string Receiver_Name { get; set; }
        public DateTime? Receiver_Date { get; set; }
    }

    public class QtyInput
    {
        [Key]
        public int Item_Order_Id { get; set; }
        public int Qty_Kop { get; set; }
        public int Qty_GA { get; set; }
        public string Dispatcher_NIK { get; set; }
        public string Dispatcher_Name { get; set; }
        public DateTime? Dispatcher_Date { get; set; }
        public string Request_No { get; set; }
        public string Order_No { get; set; }
    }

    public class QtyGAInput
    {
        [Key]
        public int Item_Order_Id { get; set; }
        public int Qty_GA { get; set; }
        public string Receiver_NIK { get; set; }
        public string Receiver_Name { get; set; }
        public DateTime? Receiver_Date { get; set; }
        public string Order_No { get; set; }
        public string Request_Type { get; set; }
        public string Request_No { get; set; }
    }

    public class QtyAcceptedInput
    {
        [Key]
        public string Cart_No { get; set; }
        public string Request_No { get; set; }
        public int Item_Id { get; set; }
        public int Qty_Acc { get; set; }
    }

    public class RequestConfirmationModel
    {
        public string Request_No { get; set; }
        public string Cart_No { get; set; }
        public int Item_Id { get; set; }
        public int Qty_Request { get; set; }
        public int Qty_Acc { get; set; }
        public string Status { get; set; }
        public string User_NIK { get; set; }
        public string User_NIK_Acc { get; set; }
        public string User_NIK_Acc_Name { get; set; }
        public string Message { get; set; }
    }

    public class BulkRequestConfirmationModel
    {
        public List<RequestConfirmationModel> Items { get; set; }
    }


    public class HC_GA_Inventory_Role
    {
        [Key]
        public int ID { get; set; }
        public string Role { get; set; }
    }

    public class HC_GA_Inventory_Users
    {
        [Key]
        public int ID { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class HC_GA_Inventory_Dept
    {
        [Key]
        public int ID { get; set; }
        public string Cost_ID { get; set; }
        public string Cost_Name { get; set; }
        public string AX_Cost_ID { get; set; }
        public string AX_Cost_Name { get; set; }
    }

    public class HC_GA_Inventory_UserDept
    {
        [Key]
        public int ID { get; set; }
        public string NIK { get; set; }
        public string Name { get; set; }
        public int Dept_ID { get; set; }
        public string Role { get; set; }
        public int Route { get; set; }
    }

    public class V_HC_GA_ItemSummary
    {
        [Key]
        public string Request_No { get; set; }
        public DateTime Date_Issued { get; set; }
        public string Ref_Code { get; set; }
        public string Department { get; set; }
        public string Item_Name { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal Total { get; set; }
    }

    public class HC_GA_Inventory_Adjustment_Detail
    {
        [Key]
        public int ID { get; set; }
        public string Request_No { get; set; }
        public int Item_ID { get; set; }
        public int Qty_Before { get; set; }
        public int Qty_After { get; set; }
        public string Creator_NIK { get; set; }
        public DateTime? Created_Date { get; set; }
    }

    public class AdjustmentInventoryDto
    {
        public string RequestNo { get; set; }
        public int ItemId { get; set; }
        public int Qty { get; set; }
    }

    public class HC_GA_Inventory_Price_Logs
    {
        [Key]
        public int ID { get; set; }
        public string Request_No { get; set; }
        public int Cart_ID { get; set; }
        public int Item_Id { get; set; }
        public decimal Old_Price { get; set; }
        public decimal New_Price { get; set; }
        public DateTime Update_Date { get; set; }
        public string Updated_By { get; set; }
    }

    public class GAConnection : DbContext
    {
        public DbSet<HC_GA_Inventory_Category> HC_GA_Inventory_Category { get; set; }
        public DbSet<HC_GA_Inventory_Users> HC_GA_Inventory_Users { get; set; }
        public DbSet<HC_GA_Inventory_UserDept> HC_GA_Inventory_UserDept { get; set; }
        public DbSet<HC_GA_Inventory_Dept> HC_GA_Inventory_Dept { get; set; }
        public DbSet<HC_GA_Inventory_Role> HC_GA_Inventory_Role { get; set; }
        public DbSet<HC_GA_Inventory_Item_List> HC_GA_Inventory_Item_List { get; set; }
        public DbSet<HC_GA_Inventory_Carts> HC_GA_Inventory_Carts { get; set; }
        public DbSet<HC_GA_Inventory_Requests> HC_GA_Inventory_Requests { get; set; }
        public DbSet<HC_GA_Inventory_Requests_Approval> HC_GA_Inventory_Requests_Approval { get; set; }
        public DbSet<HC_GA_Inventory_Request_Confirm> HC_GA_Inventory_Request_Confirm { get; set; }
        public DbSet<HC_GA_Inventory_Price_Logs> HC_GA_Inventory_Price_Logs { get; set; }
        public DbSet<QtyAcceptedInput> QtyAcceptedInput { get; set; }
        public DbSet<V_HC_GA_ItemSummary> V_HC_GA_ItemSummary { get; set; }
        public DbSet<QtyInput> QtyInput { get; set; }
        public DbSet<QtyGAInput> QtyGAInput { get; set; }
        public DbSet<HC_GA_Inventory_Order> HC_GA_Inventory_Order { get; set; }
        public DbSet<HC_GA_Inventory_Subcategory> HC_GA_Inventory_Subcategory { get; set; }
        public DbSet<HC_GA_Inventory_Adjustment_Detail> HC_GA_Inventory_Adjustment_Detail { get; set; }
        public GAConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}