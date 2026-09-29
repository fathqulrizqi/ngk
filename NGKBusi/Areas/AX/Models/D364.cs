using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.AX.Models
{
    public class AX_Sales_BI
    {
        public long ID { get; set; }
        public string Company { get; set; }
        public int InvoiceDateYYYYMM { get; set; }
        public int InvoiceDateYYYYMMDD { get; set; }
        public int Due_date { get; set; }
        public int SO_Create_Date_YYYYMMDD { get; set; }
        public int Request_receipt_date { get; set; }
        public string Invoice { get; set; }
        public string SO_No { get; set; }
        public string Customer_code { get; set; }
        public string Customer_name { get; set; }
        public string Customer_search_name { get; set; }
        public string Item_code { get; set; }
        public string Item_name { get; set; }
        public string Division { get; set; }
        public string Division_name { get; set; }
        public string Product_Category { get; set; }
        public string Product_category_name { get; set; }
        public string Country { get; set; }
        public string Area { get; set; }
        public string Sales_person { get; set; }
        public string Sales_person_name { get; set; }
        public int Business_type { get; set; }
        public string Business_type_name { get; set; }
        public string Design { get; set; }
        public string Brand { get; set; }
        public string Product_cate_major { get; set; }
        public string Product_cate_major_name { get; set; }
        public string Product_cate_middle { get; set; }
        public string Product_cate_middle_name { get; set; }
        public string Product_cate_minor { get; set; }
        public string Product_cate_minor_name { get; set; }
        public string Packaging_country { get; set; }
        public int Quantity { get; set; }
        public string Currency { get; set; }
        public decimal Price_by_IV_currency { get; set; }
        public decimal Amount_by_IV_currency { get; set; }
        public decimal Price_by_Local_currency { get; set; }
        public decimal Amount_by_Local_currency { get; set; }
        public long Rec_id { get; set; }
        public string Customer_reference { get; set; }
        public string Description_for_sales_report { get; set; }
        public string Item_Group { get; set; }
        public decimal Tax_Percent { get; set; }
        public decimal Tax { get; set; }
        public decimal Net_amount { get; set; }
        public string Tax_Invoice_Id { get; set; }
        public string NPWP { get; set; }
        public decimal Line_Disc { get; set; }
        public string Invoice_Address { get; set; }
        public string External_Item_Id { get; set; }
    }

    public class AX_Customer_List
    {
        [Key]
        public string Account { get; set; }
        public string Name { get; set; }
        public string Customer_Group { get; set; }
        public string Search_Name { get; set; }
        public string NPWP15 { get; set; }
        public string NPWP16 { get; set; }
        public string NITKU { get; set; }
    }

        public class D365Connection : DbContext
    {
        public DbSet<AX_Sales_BI> AX_Sales_BI { get; set; }
        public DbSet<AX_Customer_List> AX_Customer_List { get; set; }
        public D365Connection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}