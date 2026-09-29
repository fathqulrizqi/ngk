using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.IC.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Windows;

namespace NGKBusi.Areas.IC.Controllers
{
    [Authorize]
    public class GIMSController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        GIMSConnection dbGIMS = new GIMSConnection();

        // GET: IC/GIMS
        public ActionResult NewRequest(String ReqNumber)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currUserLevel = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Menu_Id == 128 && w.Document_Id == 1).FirstOrDefault();
            var currUserLevels = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Menu_Id == 128 && w.Document_Id == 1).ToList();
            var currUserSection = currUserLevels.Select(s => s.Dept_Code + " | " + s.Dept_Name);

            var currFilterLevel = Request["iGIMSFilterLevel"] != null && Request["iGIMSFilterLevel"] != "" ? int.Parse(Request["iGIMSFilterLevel"].Split('|')[0]) : currUserLevel?.Levels;
            var currFilterLevelSub = Request["iGIMSFilterLevel"] != null && Request["iGIMSFilterLevel"] != "" ? int.Parse(Request["iGIMSFilterLevel"].Split('|')[1]) : currUserLevel?.Levels_Sub;

            var currRequest = dbGIMS.IC_GIMS_Request_Header.Where(w => w.ReqNumber == ReqNumber).FirstOrDefault();
            var currRequestLines = dbGIMS.IC_GIMS_Request_Line.Where(w => w.ReqNumber == ReqNumber).ToList();

            var currYear = Request["iGIMSFilterYear"] ?? DateTime.Now.Year.ToString();
            var currStatus = Request["iGIMSFilterStatus"] ?? "Open";
            ViewBag.CurrRequest = currRequest;
            ViewBag.CurrRequestLines = currRequestLines;

            var currApprove = db.Approval_Master.Where(w => w.Menu_Id == 128 && w.Document_Id == 1 && w.User_NIK == currUserID);
            var currApproval = currApprove.Where(w => w.Levels == currFilterLevel && w.Levels_Sub == currFilterLevelSub).FirstOrDefault();


            if (currStatus == "Open")
            {
                ViewBag.RequestList = dbGIMS.IC_GIMS_Request_Header.Where(w => w.Created_At.Year.ToString() == currYear && w.Approval == currFilterLevel && w.Approval_Sub == currFilterLevelSub).OrderByDescending(o => o.ID).ToList();
            }
            else if (currStatus == "Signed")
            {
                ViewBag.RequestList = dbGIMS.IC_GIMS_Request_Header.Where(w => w.Created_At.Year.ToString() == currYear && (w.Approval > currFilterLevel || (w.Approval == currFilterLevel && w.Approval_Sub > currFilterLevelSub))).OrderByDescending(o => o.ID).ToList();
            }
            else
            {
                ViewBag.RequestList = dbGIMS.IC_GIMS_Request_Header.Where(w => w.Created_At.Year.ToString() == currYear).OrderByDescending(o => o.ID).ToList();
            }

            ViewBag.currFilterYear = currYear;
            ViewBag.currFilterStatus = currStatus;
            ViewBag.UserLevel = currFilterLevel != null ? currFilterLevel : currUserLevel.Levels;
            ViewBag.UserLevelSub = currFilterLevelSub != null ? currFilterLevelSub : currUserLevel.Levels_Sub;
            ViewBag.SectionList = currUserLevels;
            ViewBag.currUserLevels = currUserLevels;
            ViewBag.CurrApproval = currApproval;
            return View();
        }
        public ActionResult NewRequestAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currCostName = currUser.FindFirstValue("CostName");
            var item = Request["iItem"];
            var desc = Request["iDescription"];

            var newData = new IC_GIMS_Request_Header();
            newData.ReqNumber = this.getSequence("NI");
            newData.Description = desc;
            newData.Dept = currCostName;
            newData.Created_At = DateTime.Now;
            newData.Created_By = currUserID;
            newData.Approval = 1;
            newData.Is_Reject = false;
            dbGIMS.IC_GIMS_Request_Header.Add(newData);
            dbGIMS.SaveChanges();


            for (var i = 0; i <= Request.Form.GetValues("iItem[]").Count() - 1; i++)
            {
                var newDataSub = new IC_GIMS_Request_Line();
                newDataSub.ReqNumber = newData.ReqNumber;
                newDataSub.Item = Request.Form.GetValues("iItem[]")[i];
                newDataSub.Category = Request.Form.GetValues("iCategory[]")[i];
                newDataSub.Unit = Request.Form.GetValues("iUnit[]")[i];
                newDataSub.Usage = Request.Form.GetValues("iUsage[]")[i] != null ? double.Parse(Request.Form.GetValues("iUsage[]")[i]) : 0;
                dbGIMS.IC_GIMS_Request_Line.Add(newDataSub);
            }
            dbGIMS.SaveChanges();

            return RedirectToAction("NewRequest", "GIMS", new { area = "IC", ReqNumber = newData.ReqNumber });

        }
        public ActionResult NewRequestEdit(String ReqNumber)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currCostName = currUser.FindFirstValue("CostName");
            var item = Request["iItem"];
            var desc = Request["iDescription"];

            var editData = dbGIMS.IC_GIMS_Request_Header.Where(w => w.ReqNumber == ReqNumber).FirstOrDefault();
            editData.Description = desc;
            editData.Dept = currCostName;
            dbGIMS.SaveChanges();

            var delDataSub = dbGIMS.IC_GIMS_Request_Line.Where(w => w.ReqNumber == ReqNumber).ToList();
            dbGIMS.IC_GIMS_Request_Line.RemoveRange(delDataSub);
            dbGIMS.SaveChanges();
            for (var i = 0; i <= Request.Form.GetValues("iItem[]").Count() - 1; i++)
            {
                var newDataSub = new IC_GIMS_Request_Line();
                newDataSub.ReqNumber = editData.ReqNumber;
                newDataSub.Item = Request.Form.GetValues("iItem[]")[i];
                newDataSub.Category = Request.Form.GetValues("iCategory[]")[i];
                newDataSub.Unit = Request.Form.GetValues("iUnit[]")[i];
                newDataSub.Usage = Request.Form.GetValues("iUsage[]")[i] != null ? double.Parse(Request.Form.GetValues("iUsage[]")[i]) : 0;
                if (editData.Approval >= 2)
                {
                    newDataSub.GIMS = Request.Form.GetValues("iGIMS[]")[i];
                }
                dbGIMS.IC_GIMS_Request_Line.Add(newDataSub);
            }
            dbGIMS.SaveChanges();

            return RedirectToAction("NewRequest", "GIMS", new { area = "IC", ReqNumber = editData.ReqNumber });

        }
        public String getSequence(string type)
        {
            var lastSeq = "";
            var seqHeader = type + DateTime.Now.ToString("yy");
            var latestSequence = "";
            if (type == "NI")
            {
                latestSequence = dbGIMS.IC_GIMS_Request_Header.Where(w => w.ReqNumber.Substring(0, 4) == seqHeader).OrderByDescending(o => o.ID).Select(s => s.ReqNumber.Substring(s.ReqNumber.Length - 4, 4)).FirstOrDefault();
            }
            lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
            lastSeq = seqHeader + "-" + lastSeq.Substring(lastSeq.Length - 4, 4);

            return lastSeq;
        }

        public ActionResult newRequestSign()
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var reqNumber = Request["iSignReqNumber"];
            var btnType = Request["btnType"];
            var currNote = Request["iGIMSReturnNote"] ?? "";
            var updateSign = dbGIMS.IC_GIMS_Request_Header.Where(w => w.ReqNumber == reqNumber).FirstOrDefault();

            if (btnType == "Sign")
            {
                if (updateSign.Approval == 1 && updateSign.Approval_Sub == 1)
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
                else if (updateSign.Approval == 2 && updateSign.Approval_Sub == 0)
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
                else
                {
                    updateSign.Approval_Sub += 1;
                }
            }
            else if (btnType == "Return")
            {
                updateSign.Approval = 1;
                updateSign.Approval_Sub = 0;
            }
            else
            {
                if (updateSign.Approval == 1 || (updateSign.Approval == 2 && updateSign.Approval_Sub == 2) || updateSign.Approval == 3)
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
                else
                {
                    updateSign.Approval_Sub += 1;
                }
                //updateSign.Is_Reject = true;
            }
            dbGIMS.SaveChanges();

            var currApproval = btnType == "Reject" ? 1 : updateSign.Approval;
            var currApprovalSub = btnType == "Reject" ? 0 : updateSign.Approval_Sub;

            var getApprovalMaster = db.Approval_Master.Where(w => w.User_NIK == currUserID && w.Document_Id == 1 && w.Menu_Id == 128).FirstOrDefault();
            var updateSignHistory = new Approval_History();
            updateSignHistory.Menu_Id = 128;
            updateSignHistory.Menu_Name = "New Item Registration";
            updateSignHistory.Document_Id = 1;
            updateSignHistory.Document_Name = "New Request";
            updateSignHistory.Reveral_ID = reqNumber;
            updateSignHistory.Reveral_ID_Sub = null;
            updateSignHistory.Title = getApprovalMaster.Title;
            updateSignHistory.Header = getApprovalMaster.Header;
            updateSignHistory.Label = getApprovalMaster.Label;
            updateSignHistory.Note = currNote;
            updateSignHistory.Approval = updateSign.Approval;
            updateSignHistory.Approval_Sub = updateSign.Approval_Sub;
            updateSignHistory.IsReject = updateSign.Is_Reject ?? false;
            updateSignHistory.IsReject = false;
            updateSignHistory.IsRevise = false;
            updateSignHistory.Status = (btnType != "Sign" ? btnType : ApprovalStatus(updateSign.Approval, updateSign.Approval_Sub));
            updateSignHistory.Created_At = DateTime.Now;
            updateSignHistory.Created_By_ID = currUserID;
            updateSignHistory.Created_By_Name = currUserName;
            db.Approval_History.Add(updateSignHistory);
            db.SaveChanges();

            var updateDeptCode = db.Users_Section_AX.Where(w => w.COSTNAME == updateSign.Dept).FirstOrDefault().SECTIONTYPE;

            sendNotification(reqNumber, "PRequest", btnType, updateSign.Created_By, updateDeptCode, currApproval, currApprovalSub, currNote);
            return RedirectToAction("NewRequest", "GIMS", new { area = "IC", ReqNumber = reqNumber });
        }

        public String ApprovalStatus(int Approval, int Approval_Sub, int Type = 1)
        {
            var stat = "Submitted";
            if (Type == 1)
            {
                switch (Approval_Sub)
                {
                    case 1:
                        stat = "Submitted";
                        break;
                    default:
                        stat = "Created";
                        break;
                }

                if (Approval > 1)
                {
                    stat = "Approved";
                }
            }
            else if (Type == 2)
            {

                if (Approval == 1)
                {
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Submitted";
                            break;
                        case 2:
                            stat = "Purchasing - Checked";
                            break;
                        case 3:
                            stat = "Purchasing - Reviewed";
                            break;
                        case 4:
                            stat = "Purchasing - Approved";
                            break;
                        default:
                            stat = "Created";
                            break;
                    }
                }
                else if (Approval == 2)
                {
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Dept - Signed";
                            break;
                        case 2:
                            stat = "Dept - Checked";
                            break;
                        case 3:
                            stat = "Dept - Approved";
                            break;
                        case 4:
                            stat = "Dept - Approved";
                            break;
                        default:
                            stat = "Purchasing - Approved";
                            break;
                    }
                }
                else if (Approval == 3)
                {
                    switch (Approval_Sub)
                    {
                        case 1:
                            stat = "Management - Reviewed";
                            break;
                        case 2:
                            stat = "Management - Reviewed";
                            break;
                        case 3:
                            stat = "Management - Approved";
                            break;
                        case 4:
                            stat = "Management - Approved";
                            break;
                        default:
                            stat = "Dept - Approved";
                            break;
                    }
                }
                else
                {
                    stat = "Management - Approved";
                }

            }
            return stat;
        }

        public String ApprovalHistory(string Reveral_ID, int Approval, int Approval_Sub, int getType, int documentID = 1)
        {
            var str = "";
            var getApprovalHistory = db.Approval_History.Where(w => w.Menu_Id == 128 && w.Document_Id == documentID && w.Reveral_ID == Reveral_ID && w.Approval == Approval && w.Approval_Sub == Approval_Sub).OrderByDescending(o => o.id).FirstOrDefault();

            if (getType == 1)
            {
                str = getApprovalHistory?.Created_By_Name ?? "";
            }
            else if (getType == 2)
            {
                str = getApprovalHistory?.Created_At.ToString("dd-MMM-yyyy") ?? "";
            }
            else
            {
                str = getApprovalHistory?.Note ?? "";
            }

            return str;
        }

        public ActionResult WFL()
        {
            return View();
        }

        public ActionResult WFLDownloadData()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            //var fileName = "DB Sheet (Item)";
            var fileName = "Test";

            var path = Server.MapPath("~/Files/IC/GIMS/Download/" + fileName + ".xlsm");
            System.IO.File.Copy(Server.MapPath("~/Files/IC/GIMS/Master/" + fileName + ".xlsm"), path, true);

            var _DataList = dbGIMS.V_IC_GIMS_Progress.Where(w => w.WFL == null).Select(s => new
            {
                q1 = "Create new item(GIMS)",
                s.GIMSRequest
            }).ToList();


            var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(1);
            var ts2 = ws.Cell(15, 5).InsertData(_DataList);
            workbook.SaveAs(path);

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName + ".xlsm");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("Progress", "GIMS", new { area = "IC" });
        }
        public ActionResult DownloadRequestData()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();

            //var fileName = "DB Sheet (Item)";
            var fileName = "Request Data";

            var path = Server.MapPath("~/Files/IC/GIMS/Download/" + fileName + ".xlsx");
            System.IO.File.Copy(Server.MapPath("~/Files/IC/GIMS/Master/" + fileName + ".xlsx"), path, true);

            var _DataList = dbGIMS.IC_GIMS_Request_Line.Where(w => w.Header.Approval == 3).Select(s => new
            {
                ReqNumber = s.ReqNumber,
                Dept = s.Header.Dept,
                Description = s.Header.Description,
                Item = s.Item,
                GIMS = s.GIMS,
                Category = s.Category,
                Unit = s.Unit,
                Usage = s.Usage,
            }).ToList();


            var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(1);
            var ts2 = ws.Cell(2, 1).InsertData(_DataList);
            workbook.SaveAs(path);

            Response.ContentType = "application/x-msexcel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName + ".xlsx");
            Response.TransmitFile(path);
            Response.End();

            return RedirectToAction("Progress", "GIMS", new { area = "IC" });
        }
        public ActionResult JapanRequest()
        {
            return View();
        }
        public ActionResult Kniguris()
        {
            return View();
        }
        public ActionResult FlexNet()
        {
            return View();
        }
        public ActionResult Progress()
        {
            ViewBag.Progress = dbGIMS.V_IC_GIMS_Progress.ToList();

            return View();
        }
        public ActionResult StandardCost()
        {
            ViewBag.STDList = dbGIMS.IC_GIMS_WFL.Where(w => w.isSTDInputted == false).ToList();

            return View();
        }

        [HttpPost]
        public JsonResult _GetData()
        {

            if (Request.Files.Count > 0)
            {
                //db.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Yamaha_Import");
                //db.SaveChanges();

                try
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/IC_GIMS"), fname);
                        file.SaveAs(fname);
                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";

                        using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                        {
                            conn.Open();
                            using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                            {
                                //string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                string sheetName = "INPUT sheet$";
                                //string query = "SELECT [Item No.],[U Code],[P/F],[Order No.],[Delivery Date],[Quantity] FROM [" + sheetName + "]";
                                string query = "SELECT * FROM [" + sheetName + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        var newData = new List<ArrayList>();
                                        for (int i = 14; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            if (i > 0 && ds.Tables[0].Rows[i][5].ToString().Length > 0)
                                            {
                                                ArrayList dataArray = new ArrayList();
                                                dataArray.Add(ds.Tables[0].Rows[i][4].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][5].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][8].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][10].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][16].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][20].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][23].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][26].ToString());
                                                for (int j = 28; j <= 42; j++)
                                                {
                                                    dataArray.Add(ds.Tables[0].Rows[i][j].ToString());
                                                }
                                                for (int j = 44; j <= 58; j++)
                                                {
                                                    dataArray.Add(ds.Tables[0].Rows[i][j].ToString());
                                                }
                                                dataArray.Add(ds.Tables[0].Rows[i][63].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][66].ToString());
                                                for (int j = 84; j <= 92; j++)
                                                {
                                                    dataArray.Add(ds.Tables[0].Rows[i][j].ToString());
                                                }
                                                newData.Add(dataArray);
                                            }
                                        }
                                        return Json(newData, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                        if (System.IO.File.Exists(fname))
                        {
                            System.IO.File.Delete(fname);
                        }
                    }

                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }
        [HttpPost]
        public JsonResult _CheckWFLItem()
        {
            var _item = Request["iItem"];

            var checkItem = dbGIMS.IC_GIMS_WFL.Where(w => w.Q2 == _item).FirstOrDefault();
            if (checkItem != null)
            {
                return Json(checkItem.Q2);
            }

            return Json(true);

        }
        [HttpPost]
        public JsonResult _GetDataRegistration()
        {

            if (Request.Files.Count > 0)
            {
                //db.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Yamaha_Import");
                //db.SaveChanges();

                try
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/IC_GIMS"), fname);
                        file.SaveAs(fname);
                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";

                        using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                        {
                            conn.Open();
                            using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                            {
                                //string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                string sheetName = "Registration form (Multiple)$";
                                //string query = "SELECT [Item No.],[U Code],[P/F],[Order No.],[Delivery Date],[Quantity] FROM [" + sheetName + "]";
                                string query = "SELECT * FROM [" + sheetName + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        var newData = new List<ArrayList>();
                                        for (int i = 8; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            if (i > 0 && ds.Tables[0].Rows[i][6].ToString().Length > 0 && ds.Tables[0].Rows[i][2].ToString().ToUpper() == "C")
                                            {
                                                ArrayList dataArray = new ArrayList();
                                                dataArray.Add(ds.Tables[0].Rows[i][3].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][4].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][5].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][6].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][8].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][9].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][10].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][11].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][13].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][14].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][15].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][16].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][17].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][20].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][32].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][33].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][36].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][37].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][38].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][39].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][53].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][55].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][57].ToString());
                                                newData.Add(dataArray);
                                            }
                                        }
                                        return Json(newData, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                        if (System.IO.File.Exists(fname))
                        {
                            System.IO.File.Delete(fname);
                        }
                    }

                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }
        [HttpPost]
        public JsonResult _GetDataFlexNet()
        {

            if (Request.Files.Count > 0)
            {

                try
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/IC_GIMS"), fname);
                        file.SaveAs(fname);
                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";

                        using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                        {
                            conn.Open();
                            using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                            {
                                //string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                string sheetName = "ITEM_MASTER$";
                                //string query = "SELECT [Item No.],[U Code],[P/F],[Order No.],[Delivery Date],[Quantity] FROM [" + sheetName + "]";
                                string query = "SELECT * FROM [" + sheetName + "]";
                                OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                //DataSet ds = new DataSet();
                                adapter.Fill(ds, "Items");
                                if (ds.Tables.Count > 0)
                                {
                                    if (ds.Tables[0].Rows.Count > 0)
                                    {
                                        var newData = new List<ArrayList>();
                                        for (int i = 11; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            if (i > 0 && ds.Tables[0].Rows[i][2].ToString().Length > 0)
                                            {
                                                ArrayList dataArray = new ArrayList();

                                                dataArray.Add(ds.Tables[0].Rows[i][0].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][1].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][2].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][3].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][4].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][6].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][7].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][9].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][10].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][11].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][12].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][18].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][20].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][24].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][26].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][27].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][28].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][56].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][57].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][58].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][60].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][63].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][64].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][65].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][66].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][67].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][68].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][69].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][75].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][76].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][77].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][78].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][79].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][80].ToString());
                                                dataArray.Add(ds.Tables[0].Rows[i][83].ToString());
                                                newData.Add(dataArray);
                                            }
                                        }
                                        return Json(newData, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                        if (System.IO.File.Exists(fname))
                        {
                            System.IO.File.Delete(fname);
                        }
                    }

                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }
        [HttpPost]
        public JsonResult _GetDataKniguris()
        {

            if (Request.Files.Count > 0)
            {
                var formType = Request["iFormType"];
                string[] sheetName = new string[] { "MMaster$" };
                if (formType == "KD")
                {
                    sheetName = new string[] { "MMaster$", "KDItemMaster$" };
                }
                try
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
                        fname = Path.Combine(Server.MapPath("~/Files/Temp/IC_GIMS"), fname);
                        file.SaveAs(fname);
                        DataSet ds = new DataSet();

                        //A 32-bit provider which enables the use of

                        string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";
                        foreach (var sheet in sheetName)
                        {
                            using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                            {
                                conn.Open();
                                using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
                                {
                                    //string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                                    //string sheetName = "MMaster$";
                                    //string query = "SELECT [Item No.],[U Code],[P/F],[Order No.],[Delivery Date],[Quantity] FROM [" + sheetName + "]";
                                    string query = "SELECT * FROM [" + sheet + "]";
                                    OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
                                    //DataSet ds = new DataSet();
                                    adapter.Fill(ds, "Items");
                                    if (ds.Tables.Count > 0)
                                    {
                                        if (ds.Tables[0].Rows.Count > 0)
                                        {
                                            //var newData = new List<ArrayList>();
                                            var newData = new List<String>();
                                            var startingRow = 2;
                                            if (sheet == "KDItemMaster")
                                            {
                                                startingRow = 3;
                                            }
                                            for (int i = startingRow; i < ds.Tables[0].Rows.Count; i++)
                                            {
                                                if (i > 0 && ds.Tables[0].Rows[i][4].ToString().Length > 0)
                                                {
                                                    //ArrayList dataArray = new ArrayList();

                                                    if (formType == "FG")
                                                    {
                                                        newData.Add(ds.Tables[0].Rows[i][4].ToString());
                                                    }
                                                    else
                                                    {
                                                        if (sheet == "KDItemMaster")
                                                        {
                                                            newData.Add(ds.Tables[0].Rows[i][4].ToString());
                                                        }
                                                        else
                                                        {
                                                            newData.Add(ds.Tables[0].Rows[i][4].ToString());
                                                            newData.Add(ds.Tables[0].Rows[i][7].ToString());
                                                            newData.Add(ds.Tables[0].Rows[i][15].ToString());
                                                        }
                                                    }
                                                    //for (int j = 1; j <= 42; j++)
                                                    //{
                                                    //    dataArray.Add(ds.Tables[0].Rows[i][j].ToString());
                                                    //}
                                                    //newData.Add(dataArray);
                                                }
                                            }
                                            newData = newData.Where(s => !string.IsNullOrWhiteSpace(s) && !string.IsNullOrEmpty(s) && s != "-").Distinct().ToList();
                                            return Json(newData, JsonRequestBehavior.AllowGet);
                                        }
                                    }
                                }
                            }
                        }
                        if (System.IO.File.Exists(fname))
                        {
                            System.IO.File.Delete(fname);
                        }
                    }

                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }

        [HttpPost]
        public JsonResult UploadDataKniguris()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var data = Request["uploadData"];

            if (data.Length > 0)
            {
                var gims = data;
                var checkData = dbGIMS.IC_GIMS_Kniguris.Where(w => w.GIMS == gims).FirstOrDefault();
                if (checkData == null)
                {
                    dbGIMS.IC_GIMS_Kniguris.Add(new IC_GIMS_Kniguris
                    {
                        GIMS = gims,
                        Created_At = DateTime.Now,
                        Created_By = currUserID
                    });
                }
                dbGIMS.SaveChanges();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UploadData()
        {

            var data = Request["uploadData"].Split('|');
            ////this if because excel setting => HDR=No
            //// Delivery_Date = DateTime.ParseExact(data[4], "dd/MM/yyyy",CultureInfo.InvariantCulture),

            if (data[1].Length > 0)
            {
                var gims = data[1];
                var checkData = dbGIMS.IC_GIMS_WFL.Where(w => w.Q2 == gims).FirstOrDefault();
                if (checkData != null)
                {
                    checkData.Q3 = data[2];
                    checkData.Q4 = data[3];
                    checkData.Q5 = data[4];
                    checkData.Q6 = data[5];
                    checkData.Q7 = data[6];
                    checkData.Item_Group = data[7];
                    checkData.Product_Name = data[8];
                    checkData.Search_Name = data[9];
                    checkData.Description = data[10];
                    checkData.Storage_Dimension_Group = data[11];
                    checkData.Tracking_Dimension_Group = data[12];
                    checkData.Item_Model_Group = data[13];
                    checkData.PUR_MFG = data[14];
                    checkData.Business_Type = data[15];
                    checkData.Material_Category_Major = data[16];
                    checkData.Material_Category_Medium = data[17];
                    checkData.Material_Category_Small = data[18];
                    checkData.Inventory_Unit = data[19];
                    checkData.BOM_Unit = data[20];
                    checkData.Production_Type = data[21];
                    checkData.Warehouse = data[22];
                    checkData.Financial_Business_Type = data[23];
                    checkData.Financial_Product_Category = data[24];
                    checkData.Financial_Section = data[25];
                    checkData.Purchase_Unit = data[26];
                    checkData.Purchase_Tax_Group = data[27];
                    checkData.PriceMaster_Purchase_AccountCode = data[28];
                    checkData.PriceMaster_Purchase_AccountRelation = data[29];
                    checkData.PriceMaster_Purchase_Vendor_Name = data[30];
                    checkData.PriceMaster_Purchase_Price = data[31];
                    checkData.PriceMaster_Purchase_Currency = data[32];
                    checkData.PriceMaster_Purchase_Price_Unit = data[35];
                    checkData.PriceMaster_Purchase_Date_From = data[36];
                    checkData.PriceMaster_Purchase_Date_To = data[37];
                    checkData.Sales_Unit = data[38];
                    checkData.Sales_Tax_Group = data[39];
                    checkData.Standard_Cost_Price_Type = data[40];
                    checkData.Standard_Cost_Version = data[41];
                    checkData.Standard_Cost_Name = data[42];
                    checkData.Standard_Cost_Price = data[43];
                    checkData.Standard_Cost_Price_Charge = data[44];
                    checkData.Standard_Cost_Price_Quantity = data[45];
                    checkData.Standard_Cost_Incl_In_Unit_Price = data[46];
                    checkData.Standard_Cost_Unit = data[47];
                    checkData.Standard_Cost_Activation_Date = data[48];
                    checkData.isWFLRegistered = false;
                    checkData.isSTDInputted = false;
                    checkData.isSTDPosted = false;
                }
                else
                {
                    dbGIMS.IC_GIMS_WFL.Add(new IC_GIMS_WFL
                    {
                        Q1 = data[0],
                        Q2 = data[1],
                        Q3 = data[2],
                        Q4 = data[3],
                        Q5 = data[4],
                        Q6 = data[5],
                        Q7 = data[6],
                        Item_Group = data[7],
                        Product_Name = data[8],
                        Search_Name = data[9],
                        Description = data[10],
                        Storage_Dimension_Group = data[11],
                        Tracking_Dimension_Group = data[12],
                        Item_Model_Group = data[13],
                        PUR_MFG = data[14],
                        Business_Type = data[15],
                        Material_Category_Major = data[16],
                        Material_Category_Medium = data[17],
                        Material_Category_Small = data[18],
                        Inventory_Unit = data[19],
                        BOM_Unit = data[20],
                        Production_Type = data[21],
                        Warehouse = data[22],
                        Financial_Business_Type = data[23],
                        Financial_Product_Category = data[24],
                        Financial_Section = data[25],
                        Purchase_Unit = data[26],
                        Purchase_Tax_Group = data[27],
                        PriceMaster_Purchase_AccountCode = data[28],
                        PriceMaster_Purchase_AccountRelation = data[29],
                        PriceMaster_Purchase_Vendor_Name = data[30],
                        PriceMaster_Purchase_Price = data[31],
                        PriceMaster_Purchase_Currency = data[32],
                        PriceMaster_Purchase_Price_Unit = data[35],
                        PriceMaster_Purchase_Date_From = data[36],
                        PriceMaster_Purchase_Date_To = data[37],
                        Sales_Unit = data[38],
                        Sales_Tax_Group = data[39],
                        Standard_Cost_Price_Type = data[40],
                        Standard_Cost_Version = data[41],
                        Standard_Cost_Name = data[42],
                        Standard_Cost_Price = data[43],
                        Standard_Cost_Price_Charge = data[44],
                        Standard_Cost_Price_Quantity = data[45],
                        Standard_Cost_Incl_In_Unit_Price = data[46],
                        Standard_Cost_Unit = data[47],
                        Standard_Cost_Activation_Date = data[48],
                        isWFLRegistered = false,
                        isSTDInputted = false,
                        isSTDPosted = false
                    });
                }

                dbGIMS.SaveChanges();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UploadDataRegistration()
        {
            var data = Request["uploadData"].Split('|');
            ////this if because excel setting => HDR=No
            //// Delivery_Date = DateTime.ParseExact(data[4], "dd/MM/yyyy",CultureInfo.InvariantCulture),

            if (data[5].Length > 0)
            {
                var gims = data[5];
                var checkData = dbGIMS.IC_GIMS_Japan_Request.Where(w => w.Previous_GIMS_Number == gims).FirstOrDefault();
                if (checkData != null)
                {
                    checkData.Reg_Pattern = data[0];
                    checkData.Due_Date = data[1];
                    checkData.SOP_Date = data[2];
                    checkData.Drawing_Part_Number = data[3];
                    checkData.Reason_Code = data[4];
                    checkData.Previous_GIMS_Number = data[5];
                    checkData.Supplier = data[6];
                    checkData.Is_After_Repack = data[7];
                    checkData.Is_Ship_to_Japan = data[8];
                    checkData.Registration_Company = data[9];
                    checkData.GIMS_Material_Number = data[10];
                    checkData.Unit = data[11];
                    checkData.Division = data[12];
                    checkData.Global_Material_Text = data[13];
                    checkData.Production_Country = data[14];
                    checkData.Packaging_Country = data[15];
                    checkData.Business_Type = data[16];
                    checkData.Packaging_Brand = data[17];
                    checkData.Package_Type = data[18];
                    checkData.Terminal_Specification = data[19];
                    checkData.Min_Pkg_Qty = data[20];
                    checkData.Pcs_Pkg_Qty = data[21];
                    checkData.Bulk_Pkg_Qty = data[22];
                }
                else
                {
                    dbGIMS.IC_GIMS_Japan_Request.Add(new IC_GIMS_Japan_Request
                    {
                        Reg_Pattern = data[0],
                        Due_Date = data[1],
                        SOP_Date = data[2],
                        Drawing_Part_Number = data[3],
                        Reason_Code = data[4],
                        Previous_GIMS_Number = data[5],
                        Supplier = data[6],
                        Is_After_Repack = data[7],
                        Is_Ship_to_Japan = data[8],
                        Registration_Company = data[9],
                        GIMS_Material_Number = data[10],
                        Unit = data[11],
                        Division = data[12],
                        Global_Material_Text = data[13],
                        Production_Country = data[14],
                        Packaging_Country = data[15],
                        Business_Type = data[16],
                        Packaging_Brand = data[17],
                        Package_Type = data[18],
                        Terminal_Specification = data[19],
                        Min_Pkg_Qty = data[20],
                        Pcs_Pkg_Qty = data[21],
                        Bulk_Pkg_Qty = data[22],
                    });
                }

                dbGIMS.SaveChanges();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public void sendNotification(string currReqNumber, string currMenu, string currStatus, string currNIK = "", string deptCode = "", int approval = 0, int approval_sub = 0, string note = "-")
        {
            var currUser = ((ClaimsIdentity)User.Identity);
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            string FilePath = Path.Combine(Server.MapPath("~/Emails/IC/GIMS/"), "NewItemRegistration.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();
            var needs = "Approval";

            var settlementSubject = " - New Request";
            var doc = "New Item Registration - Request";

            var currURL = Url.Action("NewRequest", "GIMS", new { area = "IC", ReqNumber = currReqNumber }, this.Request.Url.Scheme);
            var currURLOpen = Url.Action("NewRequest", "GIMS", new { area = "IC", iPRFilterStatus = "Open" }, this.Request.Url.Scheme);
            var documentID = 1;
            var emailList = db.Approval_Master.Where(w => w.Menu_Id == 128 && w.Document_Id == documentID && w.Dept_Code == deptCode && w.Levels == approval && w.Levels_Sub == approval_sub).Select(s => s.Users.Email).Distinct().ToList();

            //if (documentID == 2 && currStatus == "Comment")
            //{
            //    var latestRejectID = db.Approval_History.Where(w => w.Menu_Id == 40 && w.Document_Id == 2 && w.Reveral_ID == currReqNumber && (w.Status == "Return" || w.Status == "Reject")).OrderByDescending(o => o.id).FirstOrDefault()?.id ?? 0;
            //    var emailListQRY = db.Approval_History.Where(w => w.Menu_Id == 40 && w.Document_Id == 2 && w.Reveral_ID == currReqNumber);
            //    var commentList = dbGIMS.Purchasing_PurchaseRequest_Quotation_Comments.Where(w => w.QuoNumber == currReqNumber).Select(s => s.Users.Email).Distinct().ToList();
            //    if (latestRejectID != 0)
            //    {
            //        emailListQRY = emailListQRY.Where(w => w.id > latestRejectID && w.Status != "Return" && w.Status != "Reject");
            //    }
            //    emailList = emailListQRY.Select(s => s.Users.Email).Distinct().ToList();

            //    foreach (var comment in commentList)
            //    {
            //        emailList.Add(comment);
            //    }
            //}
            //if (currStatus != "Sign" && currStatus != "Comment")
            //{
            //    emailList = db.Users.Where(w => w.NIK == currNIK).Select(s => s.Email).Distinct().ToList();
            //}

            //Repalce [newusername] = signup user name   
            MailText = MailText.Replace("##document##", doc);
            MailText = MailText.Replace("##needs##", needs);
            MailText = MailText.Replace("##link##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##url##", currURL.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##note##", note);
            MailText = MailText.Replace("##noteby##", currUserName);
            MailText = MailText.Replace("##linkOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##urlOpen##", currURLOpen.Replace("http://192.168.1.248/", "https://portal.ngkbusi.com/"));
            MailText = MailText.Replace("##guid##", Guid.NewGuid().ToString());

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Niterra-Portal-Notification");
            var password = "100%NGKbusi!";
            var sub = "[Niterra-Portal-Notification]" + " New GIMS Registration -" + currReqNumber + settlementSubject;
            var body = MailText;
            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,

                Credentials = new NetworkCredential(senderEmail.Address, password)
            };
            using (var mess = new MailMessage()
            {
                From = senderEmail,
                Subject = sub,
                Body = body,
                IsBodyHtml = true
            })
            {
                foreach (var dataEmail in emailList)
                {
                    if (dataEmail.Length > 0)
                    {
                        mess.To.Add(new MailAddress(dataEmail));
                    }
                }
                mess.Bcc.Add(new MailAddress("azis.abdillah@ngkbusi.com"));
                smtp.Send(mess);
            }
        }
        


        //[HttpPost]
        //public JsonResult UploadDataKniguris()
        //{
        //    var data = Request["uploadData"].Split('|');
        //    //this if because excel setting => HDR=No
        //    // Delivery_Date = DateTime.ParseExact(data[4], "dd/MM/yyyy",CultureInfo.InvariantCulture),

        //    if (data[2].Length > 0)
        //    {
        //        var gims = data[2];
        //        var _key = data[8];
        //        var checkData = dbGIMS.IC_GIMS_Kniguris.Where(w => w.FG == gims && w._Key == _key).FirstOrDefault();
        //        if (checkData != null)
        //        {
        //            checkData.No = data[0];
        //            checkData.Category = data[1];
        //            checkData.FG = data[2];
        //            checkData.Lay = data[3];
        //            checkData.F_K = data[4];
        //            checkData.Plant = data[5];
        //            checkData.PSI = data[6];
        //            checkData.SystemKey = data[7];
        //            checkData._Key = data[8];
        //            checkData.UsFr = data[9];
        //            checkData.Free = data[10];
        //            checkData.From = data[11];
        //            checkData.To = data[12];
        //            checkData.ToFG = data[13];
        //            checkData.Ave_Sales12 = data[14];
        //            checkData.Ave_Sales6 = data[15];
        //            checkData.STDev = data[16];
        //            checkData.Ave_Sales3 = data[17];
        //            checkData.CV = data[18];
        //            checkData.Ave_FC6 = data[19];
        //            checkData.FR = data[20];
        //            checkData.LT = data[21];
        //            checkData.O = data[22];
        //            checkData.CS = data[23];
        //            checkData.Supply_LT = data[24];
        //            checkData.Sheet = data[25];
        //            checkData.SS_Max = data[26];
        //            checkData.TS_Min = data[27];
        //            checkData.BoxQty = data[28];
        //            checkData.Type = data[29];
        //            checkData.BT = data[30];
        //            checkData.InsuQty = data[31];
        //            checkData.ABC_Analyze = data[32];
        //            checkData.Code = data[33];
        //            checkData.Asprova_Flag = data[34];
        //            checkData.Supplier_Code = data[35];
        //            checkData.Partial = data[36];
        //            checkData.SOP = data[37];
        //            checkData.EOP = data[38];
        //            checkData.Sum_Flag = data[39];
        //            checkData.Order_Start_Term = data[40];
        //            checkData.Allocate = data[41];
        //        }
        //        else
        //        {
        //            dbGIMS.IC_GIMS_Kniguris.Add(new IC_GIMS_Kniguris
        //            {
        //                No = data[0],
        //                Category = data[1],
        //                FG = data[2],
        //                Lay = data[3],
        //                F_K = data[4],
        //                Plant = data[5],
        //                PSI = data[6],
        //                SystemKey = data[7],
        //                _Key = data[8],
        //                UsFr = data[9],
        //                Free = data[10],
        //                From = data[11],
        //                To = data[12],
        //                ToFG = data[13],
        //                Ave_Sales12 = data[14],
        //                Ave_Sales6 = data[15],
        //                STDev = data[16],
        //                Ave_Sales3 = data[17],
        //                CV = data[18],
        //                Ave_FC6 = data[19],
        //                FR = data[20],
        //                LT = data[21],
        //                O = data[22],
        //                CS = data[23],
        //                Supply_LT = data[24],
        //                Sheet = data[25],
        //                SS_Max = data[26],
        //                TS_Min = data[27],
        //                BoxQty = data[28],
        //                Type = data[29],
        //                BT = data[30],
        //                InsuQty = data[31],
        //                ABC_Analyze = data[32],
        //                Code = data[33],
        //                Asprova_Flag = data[34],
        //                Supplier_Code = data[35],
        //                Partial = data[36],
        //                SOP = data[37],
        //                EOP = data[38],
        //                Sum_Flag = data[39],
        //                Order_Start_Term = data[40],
        //                Allocate = data[41]
        //            });
        //        }
        //        dbGIMS.SaveChanges();
        //    }

        //    return Json(data, JsonRequestBehavior.AllowGet);
        //}

        //[HttpPost]
        //public JsonResult _GetDataKniguris()
        //{

        //    if (Request.Files.Count > 0)
        //    {
        //        db.Database.ExecuteSqlCommand("Truncate table Sales_EDI_Yamaha_Import");
        //        db.SaveChanges();

        //        try
        //        {
        //            HttpFileCollectionBase files = Request.Files;
        //            for (int z = 0; z < files.Count; z++)
        //            {
        //                HttpPostedFileBase file = files[z];
        //                string fname;

        //                if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
        //                {
        //                    string[] testfiles = file.FileName.Split(new char[] { '\\' });
        //                    fname = testfiles[testfiles.Length - 1];
        //                }
        //                else
        //                {
        //                    fname = file.FileName;
        //                }

        //                 Get the complete folder path and store the file inside it.  
        //                fname = Path.Combine(Server.MapPath("~/Files/Temp/IC_GIMS"), fname);
        //                file.SaveAs(fname);
        //                DataSet ds = new DataSet();
        //                DataSet ds2 = new DataSet();

        //                A 32-bit provider which enables the use of

        //                string ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fname + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";

        //                using (OleDbConnection conn = new OleDbConnection(ConnectionString))
        //                {
        //                    conn.Open();
        //                    using (DataTable dtExcelSchema = conn.GetSchema("Tables"))
        //                    {
        //                        string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
        //                        string sheetName = "MMaster$";
        //                        string query = "SELECT [Item No.],[U Code],[P/F],[Order No.],[Delivery Date],[Quantity] FROM [" + sheetName + "]";
        //                        string query = "SELECT * FROM [" + sheetName + "]";
        //                        OleDbDataAdapter adapter = new OleDbDataAdapter(query, conn);
        //                        DataSet ds = new DataSet();
        //                        adapter.Fill(ds, "Items");
        //                        if (ds.Tables.Count > 0)
        //                        {
        //                            if (ds.Tables[0].Rows.Count > 0)
        //                            {
        //                                var newData = new List<ArrayList>();
        //                                for (int i = 2; i < ds.Tables[0].Rows.Count; i++)
        //                                {
        //                                    if (i > 0 && ds.Tables[0].Rows[i][4].ToString().Length > 0)
        //                                    {
        //                                        ArrayList dataArray = new ArrayList();

        //                                        for (int j = 1; j <= 18; j++)
        //                                        {
        //                                            dataArray.Add(ds.Tables[0].Rows[i][j].ToString());
        //                                        }
        //                                        newData.Add(dataArray);
        //                                    }
        //                                }
        //                                return Json(newData, JsonRequestBehavior.AllowGet);
        //                            }
        //                        }
        //                    }
        //                }
        //                if (System.IO.File.Exists(fname))
        //                {
        //                    System.IO.File.Delete(fname);
        //                }
        //            }

        //             Returns message that successfully uploaded  
        //            return Json("File Uploaded Successfully!");
        //        }
        //        catch (Exception ex)
        //        {
        //            return Json("Error occurred. Error details: " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        return Json("No files selected.");
        //    }
        //}


        [HttpPost]
        public JsonResult UploadDataFlexnet()
        {
            var data = Request["uploadData"].Split('|');
            ////this if because excel setting => HDR=No
            //// Delivery_Date = DateTime.ParseExact(data[4], "dd/MM/yyyy",CultureInfo.InvariantCulture),

            if (data[2].Length > 0)
            {
                var gims = data[2];
                var checkData = dbGIMS.IC_GIMS_FlexNet.Where(w => w.Product_Code == gims).FirstOrDefault();
                if (checkData != null)
                {
                    checkData.Category = data[0];
                    checkData.Product_Code = data[1];
                    checkData.Product_Name = data[2];
                    checkData.Product_Group_Code = data[3];
                    checkData.Product_Type = data[4];
                    checkData.Unit_Price = data[5];
                    checkData.Automatic_Replenish_Flag = data[6];
                    checkData.Spec_1Code = data[7];
                    checkData.Spec_2Code = data[8];
                    checkData.Spec_3Code = data[9];
                    checkData.Spec_4Code = data[10];
                    checkData.Display_Order = data[11];
                    checkData.Product_Priority = data[12];
                    checkData.Inventory_Constraint_Flag = data[13];
                    checkData.Product_Qty_Min = data[14];
                    checkData.Product_Qty_Max = data[15];
                    checkData.Product_Qty_Unit = data[16];
                    checkData.ERP_Plant = data[17];
                    checkData.ERP_MRP_Admin_Group = data[18];
                    checkData.Type = data[19];
                    checkData.Customer_Name = data[20];
                    checkData.Condition_Formula_For_Linking = data[21];
                    checkData.Production_Method = data[22];
                    checkData.Transfer_ASP = data[23];
                    checkData.Process_Code = data[24];
                    checkData.Neckel_Item_Text = data[25];
                    checkData.Package_Spec = data[26];
                    checkData.Shipmement_Destination = data[27];
                    checkData.Direction_Type = data[28];
                    checkData.Model_D_S = data[29];
                    checkData.Check_D_S = data[30];
                    checkData.FG_Category = data[31];
                    checkData.Defective_Rate = data[32];
                    checkData.Set_Production_Item = data[33];
                    checkData.Production_Country = data[34];
                }
                else
                {
                    dbGIMS.IC_GIMS_FlexNet.Add(new IC_GIMS_FlexNet
                    {
                        Category = data[0],
                        Product_Code = data[1],
                        Product_Name = data[2],
                        Product_Group_Code = data[3],
                        Product_Type = data[4],
                        Unit_Price = data[5],
                        Automatic_Replenish_Flag = data[6],
                        Spec_1Code = data[7],
                        Spec_2Code = data[8],
                        Spec_3Code = data[9],
                        Spec_4Code = data[10],
                        Display_Order = data[11],
                        Product_Priority = data[12],
                        Inventory_Constraint_Flag = data[13],
                        Product_Qty_Min = data[14],
                        Product_Qty_Max = data[15],
                        Product_Qty_Unit = data[16],
                        ERP_Plant = data[17],
                        ERP_MRP_Admin_Group = data[18],
                        Type = data[19],
                        Customer_Name = data[20],
                        Condition_Formula_For_Linking = data[21],
                        Production_Method = data[22],
                        Transfer_ASP = data[23],
                        Process_Code = data[24],
                        Neckel_Item_Text = data[25],
                        Package_Spec = data[26],
                        Shipmement_Destination = data[27],
                        Direction_Type = data[28],
                        Model_D_S = data[29],
                        Check_D_S = data[30],
                        FG_Category = data[31],
                        Defective_Rate = data[32],
                        Set_Production_Item = data[33],
                        Production_Country = data[34]
                    });
                }
                dbGIMS.SaveChanges();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}