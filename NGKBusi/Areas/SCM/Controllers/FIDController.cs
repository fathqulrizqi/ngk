using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.OleDb;
using NGKBusi.Models;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNet.Identity;

namespace NGKBusi.Areas.SCM.Controllers
{
    public class FIDController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        // GET: SCM/FID
        public ActionResult Index()
        {
            ViewBag.FIDHeader = db.SCM_FID_Header.ToList();
            return View();
        }
        [HttpPost]
        public ActionResult UploadData()
        {
            if (Request.Files.Count > 0)
            {
                HttpFileCollectionBase files = Request.Files;
                for (int z = 0; z < files.Count; z++)
                {
                    HttpPostedFileBase file = files[z];
                    string fname;

                    if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                    {
                        string[] testfiles = file.FileName.Split(new char[] { '\\' });
                        fname = testfiles[testfiles.Length - 1];
                    }
                    else
                    {
                        fname = file.FileName;
                    }

                    // Get the complete folder path and store the file inside it.  
                    fname = Path.Combine(Server.MapPath("~/Files/Temp/FID"), fname);
                    file.SaveAs(fname);
                    DataSet dsHeaderName = new DataSet();
                    DataSet ds = new DataSet();

                    //A 32-bit provider which enables the use of

                    string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties='Excel 12.0;HDR=YES'";

                    using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                    {
                        conn.Open();
                        using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                        {
                            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            string query = "SELECT * FROM [" + sheetName + "]";
                            OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                            //DataSet ds = new DataSet();
                            adapter.Fill(ds, "Items");
                            var startYear = ds.Tables[0].Columns[7].ColumnName.Trim().Substring(0, 4);
                            var startMonth = ds.Tables[0].Columns[7].ColumnName.Trim().Substring(4, 2);
                            if (ds.Tables.Count > 0)
                            {
                                if (ds.Tables[0].Rows.Count > 0)
                                {
                                    var newData = new List<String[]>();
                                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                    {
                                        newData.Add(new[] {
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Business_Type"].ToString()) ? ds.Tables[0].Rows[i]["Business_Type"].ToString() : "",
                                                startYear,startMonth,
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Customers_ID"].ToString()) ? ds.Tables[0].Rows[i]["Customers_ID"].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Customers_Name"].ToString()) ? ds.Tables[0].Rows[i]["Customers_Name"].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Item_ID"].ToString()) ? ds.Tables[0].Rows[i]["Item_ID"].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Item_Name"].ToString()) ? ds.Tables[0].Rows[i]["Item_Name"].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["External_Item"].ToString()) ? ds.Tables[0].Rows[i]["External_Item"].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][6].ToString()) ? ds.Tables[0].Rows[i][6].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][7].ToString()) ? ds.Tables[0].Rows[i][7].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][8].ToString()) ? ds.Tables[0].Rows[i][8].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][9].ToString()) ? ds.Tables[0].Rows[i][9].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][10].ToString()) ? ds.Tables[0].Rows[i][10].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][11].ToString()) ? ds.Tables[0].Rows[i][11].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i][12].ToString()) ? ds.Tables[0].Rows[i][12].ToString() : "",
                                                !String.IsNullOrEmpty(ds.Tables[0].Rows[i]["Remark"].ToString()) ? ds.Tables[0].Rows[i]["Remark"].ToString() : ""
                                            });
                                    }
                                    SetUploadData(newData, int.Parse(startYear), int.Parse(startMonth));
                                }
                            }
                        }
                    }
                    if (System.IO.File.Exists(fname))
                    {
                        System.IO.File.Delete(fname);
                    }
                }
            }
            return RedirectToAction("Index", "FID", new { area = "SCM" });
        }

        [HttpPost]
        public JsonResult SetUploadData(List<String[]> dataList, int startYear, int startMonth)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var arrayItemID = new List<String>();
            var arrayBusType = new List<String>();
            var newHeaderID = 0;
            foreach (var i in dataList)
            {
                arrayItemID.Add(i[5]);
                arrayBusType.Add(i[0]);
                var businessType = i[0];
                var sYear = i[1];
                var sMonth = i[2];
                var custID = i[3];
                var custName = i[4];
                var itemID = i[5];
                var itemName = i[6];
                var extID = i[7];
                var val1 = Int32.Parse(i[8]);
                var val2 = Int32.Parse(i[9]);
                var val3 = Int32.Parse(i[10]);
                var val4 = Int32.Parse(i[11]);
                var val5 = Int32.Parse(i[12]);
                var val6 = Int32.Parse(i[13]);
                var val7 = Int32.Parse(i[14]);
                var remark = i[15];
                if (!String.IsNullOrEmpty(businessType))
                {
                    var checkHeader = db.SCM_FID_Header.Where(w => w.Business_Type_Name == businessType && w.Start_Year == startYear && w.Start_Month == startMonth).FirstOrDefault();
                    if (checkHeader == null)
                    {
                        var newHeader = db.SCM_FID_Header.Add(new SCM_FID_Header
                        {
                            Business_Type_Name = businessType,
                            Start_Year = startYear,
                            Start_Month = startMonth,
                            Created_Date = DateTime.Now,
                            Created_By = currUserID
                        });
                        db.SaveChanges();
                        newHeaderID = newHeader.id;
                    }
                    else
                    {
                        newHeaderID = checkHeader.id;
                    }
                    var checkData = db.SCM_FID_List.Where(w => w.Header_ID == newHeaderID && w.Item_ID == itemID && w.Customer_ID == custID).FirstOrDefault();
                    if (checkData == null)
                    {
                        db.SCM_FID_List.Add(new SCM_FID_List
                        {
                            Header_ID = newHeaderID,
                            Customer_ID = custID,
                            Customer_Name = custName,
                            Item_ID = itemID,
                            Item_Name = itemName,
                            External_ID = extID,
                            Value_1 = val1,
                            Value_2 = val2,
                            Value_3 = val3,
                            Value_4 = val4,
                            Value_5 = val5,
                            Value_6 = val6,
                            Value_7 = val7,
                            Remark = remark
                        });
                    }
                    else
                    {
                        checkData.Customer_ID = custID;
                        checkData.Customer_Name = custName;
                        checkData.Item_ID = itemID;
                        checkData.Item_Name = itemName;
                        checkData.External_ID = extID;
                        checkData.Value_1 = val1;
                        checkData.Value_2 = val2;
                        checkData.Value_3 = val3;
                        checkData.Value_4 = val4;
                        checkData.Value_5 = val5;
                        checkData.Value_6 = val6;
                        checkData.Value_7 = val7;
                        checkData.Remark = remark;
                    }
                }
            }

            var deleteUnusedData = db.SCM_FID_List.Where(w => w.Header_ID == newHeaderID && !arrayItemID.Contains(w.Item_ID)).ToList();
            db.SCM_FID_List.RemoveRange(deleteUnusedData);

            db.SaveChanges();

            return Json(true);
        }
    }
}