using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using ClosedXML.Excel;
using NGKBusi.Models;

namespace NGKBusi.Areas.AX.Controllers
{
    public class ItemMasterController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: AX/ItemMaster
        public ActionResult Index()
        {
            ViewBag.ItemClass = db.AX_Item_Master_Class.OrderBy(o => o.Item_Group).ToList();
            return View();
        }
        public JsonResult getClassSub()
        {
            Int32 classID = Int32.Parse(Request["iID"]);
            switch (classID)
            {
                case 3:
                case 4:
                case 5:
                    classID = 345;
                    break;
            }
            var itemClassSub = db.AX_Item_Master_Class_Sub.Where(w => w.Class_ID == classID && w.Parent_ID == null).OrderBy(o => o.Description).ToList();
            return Json(itemClassSub, JsonRequestBehavior.AllowGet);
        }

        public string getD365Data()
        {
            string fname;
            string filePath = "";
           // HttpFileCollectionBase files = Request.Files;
            //for (int z = 0; z < files.Count; z++)
            //{
                //HttpPostedFileBase file = files[z];

                //if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                //{
                //    string[] testfiles = file.FileName.Split(new char[] { '\\' });
                //    fname = testfiles[testfiles.Length - 1];
                //}
                //else
                //{
                //    fname = file.FileName;
                //}
                //fname = "LedgerJournalLine.xlsx";

                // Get the complete folder path and store the file inside it.  
                //filePath = Path.Combine(Server.MapPath("~/Files/AX/"), fname);
                //file.SaveAs(filePath);
            //}
            fname = "LedgerJournalLine.xlsx";
            filePath = Path.Combine(Server.MapPath("~/Files/D365/"), fname);
            var filePaths = Path.Combine(Server.MapPath("~/Files/D365/"), "LedgerJournalLineTest.xlsx");

            //Open the Excel file using ClosedXML.
            using (XLWorkbook workBook = new XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                IXLWorksheet workSheet = workBook.Worksheet(1);

                //Create a new DataTable.
                DataTable dt = new DataTable();

                //Loop through the Worksheet rows.
                int rowIndex = 1;
                bool firstRow = true;
                Thread.Sleep(30000);
                workBook.Save();
                foreach (IXLRow row in workSheet.Rows())
                {
                    if (rowIndex >= 9)
                    {
                        if (row.Cell(1).GetString() != "")
                        {
                            //Use the first row to add columns to DataTable.
                            if (firstRow)
                            {
                                foreach (IXLCell cell in row.Cells())
                                {
                                    dt.Columns.Add(cell.Value.ToString());
                                }
                                firstRow = false;
                            }
                            else
                            {
                                //Add rows to DataTable.
                                dt.Rows.Add();
                                int i = 0;
                                foreach (IXLCell cell in row.Cells(1, dt.Columns.Count))
                                {
                                    dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                                    i++;
                                }
                            }
                        }
                    }
                    rowIndex++;
                }
                //if (System.IO.File.Exists(filePath))
                //{
                //    System.IO.File.Delete(filePath);
                //}

                var data = dt.Rows.OfType<DataRow>()
                .Select(row => dt.Columns.OfType<DataColumn>()
                    .ToDictionary(col => col.ColumnName, c => row[c]));
                var dtData = dt.AsEnumerable();

                return System.Text.Json.JsonSerializer.Serialize(data);
            }

        }
    }
}