using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using NGKBusi.Models;

namespace NGKBusi.Areas.IC.Models
{
    public class IC_GIMS_Request_Header
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [Key]
        [ForeignKey("Lines")]
        public string ReqNumber { get; set; }
        public string Dept { get; set; }
        public string Description { get; set; }
        public DateTime Created_At { get; set; }
        [ForeignKey("Creator")]
        public string Created_By { get; set; }
        public int Approval { get; set; }
        public int Approval_Sub { get; set; }
        public bool? Is_Reject { get; set; }
        public ICollection<IC_GIMS_Request_Line> Lines { get; set; }
        public virtual Users Creator { get; set; }
    }
    public class IC_GIMS_Request_Line
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey("Header")]
        public string ReqNumber { get; set; }
        public string Item { get; set; }
        public string GIMS { get; set; }
        public string Category { get; set; }
        public string Business_Channel { get; set; }
        public string Unit { get; set; }
        public double? Usage { get; set; }
        public virtual IC_GIMS_Request_Header Header { get; set; }
    }
    public class IC_GIMS_WFL
    {
        [Key]
        public int ID { get; set; }
        public string Q1 { get; set; }
        public string Q2 { get; set; }
        public string Q3 { get; set; }
        public string Q4 { get; set; }
        public string Q5 { get; set; }
        public string Q6 { get; set; }
        public string Q7 { get; set; }
        public string Item_Group { get; set; }
        public string Product_Name { get; set; }
        public string Search_Name { get; set; }
        public string Description { get; set; }
        public string Storage_Dimension_Group { get; set; }
        public string Tracking_Dimension_Group { get; set; }
        public string Item_Model_Group { get; set; }
        public string PUR_MFG { get; set; }
        public string Business_Type { get; set; }
        public string Material_Category_Major { get; set; }
        public string Material_Category_Medium { get; set; }
        public string Material_Category_Small { get; set; }
        public string Inventory_Unit { get; set; }
        public string BOM_Unit { get; set; }
        public string Production_Type { get; set; }
        public string Warehouse { get; set; }
        public string Financial_Business_Type { get; set; }
        public string Financial_Product_Category { get; set; }
        public string Financial_Section { get; set; }
        public string Purchase_Unit { get; set; }
        public string Purchase_Tax_Group { get; set; }
        public string PriceMaster_Purchase_AccountCode { get; set; }
        public string PriceMaster_Purchase_AccountRelation { get; set; }
        public string PriceMaster_Purchase_Vendor_Name { get; set; }
        public string PriceMaster_Purchase_Price { get; set; }
        public string PriceMaster_Purchase_Currency { get; set; }
        public string PriceMaster_Purchase_Price_Unit { get; set; }
        public string PriceMaster_Purchase_Date_From { get; set; }
        public string PriceMaster_Purchase_Date_To { get; set; }
        public string Sales_Unit { get; set; }
        public string Sales_Tax_Group { get; set; }
        public string Standard_Cost_Price_Type { get; set; }
        public string Standard_Cost_Version { get; set; }
        public string Standard_Cost_Name { get; set; }
        public string Standard_Cost_Price { get; set; }
        public string Standard_Cost_Price_Charge { get; set; }
        public string Standard_Cost_Price_Quantity { get; set; }
        public string Standard_Cost_Incl_In_Unit_Price { get; set; }
        public string Standard_Cost_Unit { get; set; }
        public string Standard_Cost_Activation_Date { get; set; }
        public bool isWFLRegistered { get; set; }
        public bool isSTDInputted { get; set; }
        public bool isSTDPosted { get; set; }
    }
    public class IC_GIMS_Japan_Request
    {
        [Key]
        public int ID { get; set; }
        public string Reg_Pattern { get; set; }
        public string Due_Date { get; set; }
        public string SOP_Date { get; set; }
        public string Drawing_Part_Number { get; set; }
        public string Reason_Code { get; set; }
        public string Previous_GIMS_Number { get; set; }
        public string Supplier { get; set; }
        public string Is_After_Repack { get; set; }
        public string Is_Ship_to_Japan { get; set; }
        public string Registration_Company { get; set; }
        public string GIMS_Material_Number { get; set; }
        public string Unit { get; set; }
        public string Division { get; set; }
        public string Global_Material_Text { get; set; }
        public string Production_Country { get; set; }
        public string Packaging_Country { get; set; }
        public string Business_Type { get; set; }
        public string Packaging_Brand { get; set; }
        public string Package_Type { get; set; }
        public string Terminal_Specification { get; set; }
        public string Min_Pkg_Qty { get; set; }
        public string Pcs_Pkg_Qty { get; set; }
        public string Bulk_Pkg_Qty { get; set; }
    }

    public class IC_GIMS_Kniguris
    {
        [Key]
        public int ID { get; set; }
        public string GIMS { get; set; }
        public DateTime Created_At { get; set; }
        [ForeignKey("Creator")]
        public string Created_By { get; set; }
        public virtual Users Creator { get; set; }
    }
    public class IC_GIMS_FlexNet
    {
        [Key]
        public int ID { get; set; }
        public string Category { get; set; }
        public string Product_Code { get; set; }
        public string Product_Name { get; set; }
        public string Product_Group_Code { get; set; }
        public string Product_Type { get; set; }
        public string Unit_Price { get; set; }
        public string Automatic_Replenish_Flag { get; set; }
        public string Spec_1Code { get; set; }
        public string Spec_2Code { get; set; }
        public string Spec_3Code { get; set; }
        public string Spec_4Code { get; set; }
        public string Display_Order { get; set; }
        public string Product_Priority { get; set; }
        public string Inventory_Constraint_Flag { get; set; }
        public string Product_Qty_Min { get; set; }
        public string Product_Qty_Max { get; set; }
        public string Product_Qty_Unit { get; set; }
        public string ERP_Plant { get; set; }
        public string ERP_MRP_Admin_Group { get; set; }
        public string Type { get; set; }
        public string Customer_Name { get; set; }
        public string Condition_Formula_For_Linking { get; set; }
        public string Production_Method { get; set; }
        public string Transfer_ASP { get; set; }
        public string Process_Code { get; set; }
        public string Neckel_Item_Text { get; set; }
        public string Package_Spec { get; set; }
        public string Shipmement_Destination { get; set; }
        public string Direction_Type { get; set; }
        public string Model_D_S { get; set; }
        public string Check_D_S { get; set; }
        public string FG_Category { get; set; }
        public string Defective_Rate { get; set; }
        public string Set_Production_Item { get; set; }
        public string Production_Country { get; set; }
    }
    public class V_IC_GIMS_Progress
    {
        [Key]
        public Int64 ID { get; set; }
        public int Period_Year { get; set; }
        public string Category { get; set; }
        public string GIMSRequest { get; set; }
        public string WFL { get; set; }
        //public string Japan_Request { get; set; }
        public string Flexnet { get; set; }
        public string Kniguris { get; set; }
    }
        public class GIMSConnection : DbContext
    {
        public DbSet<IC_GIMS_Request_Header> IC_GIMS_Request_Header { get; set; }
        public DbSet<IC_GIMS_Request_Line> IC_GIMS_Request_Line { get; set; }
        public DbSet<IC_GIMS_WFL> IC_GIMS_WFL { get; set; }
        public DbSet<IC_GIMS_Japan_Request> IC_GIMS_Japan_Request { get; set; }
        public DbSet<IC_GIMS_Kniguris> IC_GIMS_Kniguris { get; set; }
        public DbSet<IC_GIMS_FlexNet> IC_GIMS_FlexNet { get; set; }
        public DbSet<V_IC_GIMS_Progress> V_IC_GIMS_Progress { get; set; }
        public GIMSConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}