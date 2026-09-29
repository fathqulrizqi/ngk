using Highsoft.Web.Mvc.Charts;
using MathNet.Numerics.Distributions;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Models;
using Rotativa;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.IT.Controllers
{
    
    public class FormCheckSheetController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        FormCheckSheetConnection dbfcs = new FormCheckSheetConnection();
        // GET: IT/FormCheckSheet
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;


        public ActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = "AdminSparepart, UserSparepart, Administrator")]
        [HttpPost]
        public ActionResult GetDataCheckSheet()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dbfcs.V_IT_CheckSheetForm_Header.OrderByDescending(a=>a.Periode).ToList();
            var CountRow = dbfcs.V_IT_CheckSheetForm_Header.Count();
            List<Tbl_IT_CheckSheetForm_Header> actions = new List<Tbl_IT_CheckSheetForm_Header>();
            int No = 0;
            foreach (var Item in spl)
            {
                No++;

                var UrlAction = Url.Action("CreateCheckSheet", "FormCheckSheet", new { area = "IT", ID = Item.ID });

                var ViewButton = "";

                if (User.IsInRole("Administrator"))
                {
                    ViewButton = "<a href=\"" + UrlAction + "\" title=\"View Detail\" class=\"btn btn-primary\"><i class=\"fa fa-eye\"></i></a>";
                }
                else
                {
                    ViewButton = "";
                }
                var Prd = Item.Periode;
                string monthYear = Prd.ToString("MMMM yyyy");
                var CreateDate = Item.CreateTime;
                string shortDate = CreateDate.ToString("dd-MM-yyyy");

                var statusForm = "";

                if (Item.Status == 4)
                {
                    statusForm = "<span class=\"badge badge-success\">Approved</span>";
                }
                else if (Item.Status == 3)
                {
                    statusForm = "<span class=\"badge badge-danger\">Checked</span>";
                }
                if (Item.Status == 2)
                {
                    statusForm = "<span class=\"badge badge-warning\">Submitted</span>";
                }
                if (Item.Status == 1)
                {
                    statusForm = "<span class=\"badge badge-primary\">Open</span>";
                }

                actions.Add(
                    new Tbl_IT_CheckSheetForm_Header
                    {
                        ID = No,
                        Prd = monthYear,
                        CreateBy = Item.CreateName,
                        CreateDate = shortDate,
                        Status = statusForm,
                        ViewButton = ViewButton
                    });
            }

            //return Json(new
            //{
            //    rows = actions,
            //    totalNotFiltered = CountRow,
            //    total = CountRow
            //}, JsonRequestBehavior.AllowGet);

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "AdminSparepart, UserSparepart, Administrator")]
        [HttpGet]
        public ActionResult CreateCheckSheet(int ID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            string ChecksheetID = ID.ToString();

            var dataDaily = dbfcs.IT_CheckSheetForm_Daily_Detail.Where(x => x.ChecksheetID == ChecksheetID).ToList();
            var dataWeekly = dbfcs.IT_CheckSheetForm_Weekly_Detail.Where(x => x.ChecksheetID == ChecksheetID).ToList();
            var dataMonthly = dbfcs.IT_CheckSheetForm_Monthly_Detail.Where(x => x.ChecksheetID == ChecksheetID).ToList();
            var dataRole = dbfcs.IT_CheckSheetForm_Approval.Where(x=>x.UserHandler == CurrUser.NIK).ToList();

            var dataCheckerInitial = dbfcs.IT_CheckSheetForm_InitialChecker.Where(x => x.CheckSheetID == ChecksheetID).ToList();
            var dataHeader = dbfcs.V_IT_CheckSheetForm_Header.Where(x => x.ID == ID).FirstOrDefault();

            ViewBag.dailydata = dataDaily;
            ViewBag.weeklydata = dataWeekly;
            ViewBag.monthlydata = dataMonthly;
            ViewBag.dataInitialChecker = dataCheckerInitial;
            ViewBag.Header = dataHeader;
            ViewBag.Role = dataRole;

            return View();
        }
        [Authorize(Roles = "AdminSparepart, UserSparepart, Administrator")]
        [HttpPost]
        public ActionResult CreateCheckSheet(DateTime periode)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var StrDateTo = Convert.ToDateTime(periode).ToString("dd MMM yyyy");
            //IT_CheckSheetForm_Header smodel = new IT_CheckSheetForm_Header();

            //smodel.Periode = periode;
            //smodel.Status = 1;
            //smodel.CreateBy = CurrUser.NIK;
            //smodel.CreateTime = DateTime.Now;

            //dbfcs.IT_CheckSheetForm_Header.Add(smodel);
            //var i = dbfcs.SaveChanges();

            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand("sp_IT_CheckSheetForm_Create", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@CreateBy", CurrUser.NIK);
            cmd.Parameters.AddWithValue("@Periode", StrDateTo);

            conn.Open();
            int i = cmd.ExecuteNonQuery();
            conn.Close();

            if (i > 0)
            {
                return Json(new { status = 1, msg = periode });
            }
            else
            {
                return Json(new { status = 0, msg = periode });
            }


        }
        [Authorize(Roles = "AdminSparepart, UserSparepart, Administrator")]
        public ActionResult UpdateDetailCheckSheet()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UpdateItemCheckSheet(string itemID, string ChecklistCode, string CheckSheetID, string interval)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            SqlConnection conn = new SqlConnection(connectionString);
            //var CheckSheetID = Request["CheckSheetID"];
            //var itemID = Request["itemID"];
            String[] itemUpdate = itemID.Split('-');
            var control = itemUpdate[0];
            var Daily = itemUpdate[1];

            SqlCommand cmd = new SqlCommand("sp_IT_CheckSheet_Update_Item", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@userUpdate", CurrUser.NIK);
            cmd.Parameters.AddWithValue("@ControlItem", control);
            cmd.Parameters.AddWithValue("@Daily", Daily);
            cmd.Parameters.AddWithValue("@ChecklistCode", ChecklistCode);
            cmd.Parameters.AddWithValue("@CheckSheetID", CheckSheetID);
            cmd.Parameters.AddWithValue("@interval", interval);

            conn.Open();
            int i = cmd.ExecuteNonQuery();
            conn.Close();

            if (i > 0)
            {
                return Json(new { status = 1, msg = i, interval = interval, CheckSheetID = CheckSheetID });
            }
            else
            {
                return Json(new { status = 0, msg = i, interval = interval, CheckSheetID = CheckSheetID });
            }
            
        }
        [HttpPost]
        public ActionResult updateInitialChecker(string itemID, string CheckSheetID, string interval)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var Name = CurrUser.Name;

            SqlConnection conn = new SqlConnection(connectionString);
            String[] itemUpdate = itemID.Split('-');
            var control = itemUpdate[0];
            var Daily = itemUpdate[1];
            string init = Name.Substring(0, 1);

            var initial = dbfcs.IT_CheckSheetForm_InitialChecker.Where(w => w.CheckSheetID == CheckSheetID && w.CheckerSequence == Daily).FirstOrDefault();
            initial.CheckerBy = CurrUser.NIK;
            initial.Initial = init;
            var update = dbfcs.SaveChanges();

            if (update == 1)
            {
                
                return Json(new { status = 1, initial = init, CheckerSequence = Daily, msg = update });
            }
            else
            {
                return Json(new { status = 0, initial = init, CheckerSequence = Daily, msg = update });
            }

        }
        [HttpPost]
        public ActionResult submitCheckSheet(int ChecksheetID, string note)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var Name = CurrUser.Name;

            var header = dbfcs.IT_CheckSheetForm_Header.Where(w => w.ID == ChecksheetID).FirstOrDefault();
            header.Notes = note;
            header.Status = 2;
            header.SubmitBy = CurrUser.NIK;
            header.SubmitTime = DateTime.Now;

            var update = dbfcs.SaveChanges();

            if (update == 1)
            {

                return Json(new { status = 1, msg = "submit success" });
            }
            else
            {
                return Json(new { status = 0, msg="submit failed"});
            }

        }

        public ActionResult CheckedCheckSheet(int ChecksheetID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var Name = CurrUser.Name;

            var header = dbfcs.IT_CheckSheetForm_Header.Where(w => w.ID == ChecksheetID).FirstOrDefault();
            header.Status = 3;
            header.CheckedBy = CurrUser.NIK;
            header.CheckedTime = DateTime.Now;

            var update = dbfcs.SaveChanges();

            if (update == 1)
            {

                return Json(new { status = 1, msg = "Form Checked" });
            }
            else
            {
                return Json(new { status = 0, msg = "failed Check Form" });
            }

        }

        public ActionResult ApproveCheckSheet(int ChecksheetID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var Name = CurrUser.Name;

            var header = dbfcs.IT_CheckSheetForm_Header.Where(w => w.ID == ChecksheetID).FirstOrDefault();
            header.Status = 4;
            header.ApprovedBy = CurrUser.NIK;
            header.ApprovedTime = DateTime.Now;

            var update = dbfcs.SaveChanges();

            if (update == 1)
            {

                return Json(new { status = 1, msg = "Form Checked" });
            }
            else
            {
                return Json(new { status = 0, msg = "failed Check Form" });
            }

        }
        [HttpPost]
        public ActionResult ReturnCheckSheet(int ChecksheetID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var Name = CurrUser.Name;

            var header = dbfcs.IT_CheckSheetForm_Header.Where(w => w.ID == ChecksheetID).FirstOrDefault();
            header.Status = 1;
            header.ApprovedBy = null;
            header.ApprovedTime = null;
            header.CheckedBy = null;
            header.CheckedTime = null;

            var update = dbfcs.SaveChanges();

            if (update == 1)
            {

                return Json(new { status = 1, msg = "Form Checked" });
            }
            else
            {
                return Json(new { status = 0, msg = "failed Check Form" });
            }

        }

        /* ---------------------------------------------- Form Checksheet Maintenance Hardware & Software ------------------------------------------------- */
        [Authorize]
        public ActionResult IndexMTCHardSoft()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            ViewBag.CurrUser = CurrUser;

            return View();
        }
        public ActionResult GetDataCheckSheetMTCHardSoft()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = from s in dbfcs.IT_CheckSheetMTC_Header select s;
            if (CurrUser.DeptName == "INFORMATION TECHNOLOGY")
            {
                spl = spl.OrderByDescending(o => o.CreateTime);
            } else
            {
                spl = spl.Where(w => w.Section == CurrUser.SectionName).OrderByDescending(o => o.CreateTime);
            }
            
            var CountRow = dbfcs.IT_CheckSheetMTC_Header.Count();
            List<Tbl_CheckSheetMTC_Header> actions = new List<Tbl_CheckSheetMTC_Header>();
            int No = 0;
            foreach (var Item in spl)
            {
                No++;

                var UrlAction = Url.Action("DetailCheckSheetMtcHardSoft", "FormCheckSheet", new { area = "IT", HeaderID = Item.ID });

                var ViewButton = "";

              
                ViewButton = "<a href=\"" + UrlAction + "\" title=\"View Detail\" class=\"btn btn-primary\"><i class=\"fa fa-eye\"></i></a>";
                

                var statusForm = "";

                if (Item.Status == 4)
                {
                    statusForm = "<span class=\"badge badge-success\">Approved</span>";
                }
                else if (Item.Status == 3)
                {
                    statusForm = "<span class=\"badge badge-default\">Checked</span>";
                }
                if (Item.Status == 2)
                {
                    statusForm = "<span class=\"badge badge-warning\">Submitted</span>";
                }
                if (Item.Status == 1)
                {
                    statusForm = "<span class=\"badge badge-primary\">Open</span>";
                }

                actions.Add(
                    new Tbl_CheckSheetMTC_Header
                    {
                        ID = No,
                        Dept = Item.Dept,
                        Section = Item.Section,
                        Nama = Item.Nama,
                        TahunMulaiPemakaian = Item.TahunMulaiPemakaian,
                        Status = statusForm,
                        ViewButton = ViewButton
                    });
            }

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        [HttpGet]
        public ActionResult FormMTCHardSoft(string HeaderID = null)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var userList = db.V_Users_Active.ToList();
            var ToolsHardware = dbfcs.IT_ChecksSheetMTC_Tools.Where(w=>w.is_delete == 0 && w.Type == "Hardware").ToList();
            int countToolsHardware = ToolsHardware.Count();
            var deviceHardware = dbfcs.IT_CheckSheetMTC_Devices.Where(w => w.is_delete == 0 && w.Type == "Hardware").ToList();
            int countDevicesHardware = deviceHardware.Count();
            List<Tbl_CheckSheetMTC_Device_Checklist> dataChecklistHardware = new List<Tbl_CheckSheetMTC_Device_Checklist>();

            int no = 0;
            // variable untuk menghitung index array tools
            int i = 0;
            foreach (var itemDevice in deviceHardware)
            {
                no++;

                var ToolsName = "";
                if (i >= countToolsHardware)
                {
                    ToolsName = "";
                }

                else if (i < countToolsHardware)
                {
                    List<IT_ChecksSheetMTC_Tools> toolsData = new List<IT_ChecksSheetMTC_Tools>();
                    var stoolsData = ToolsHardware[i];

                    ToolsName = stoolsData.Tools;
                }

                dataChecklistHardware.Add(
                    new Tbl_CheckSheetMTC_Device_Checklist
                    {
                        ID = itemDevice.ID,
                        NO = no,
                        Type = "",
                        Tools = ToolsName,
                        Device = itemDevice.Device
                    });

                i++;
            }

            var ToolsSoftware = dbfcs.IT_ChecksSheetMTC_Tools.Where(w => w.is_delete == 0 && w.Type == "Software").ToList();
            int countToolsSoftware = ToolsSoftware.Count();
            //var deviceSoftware = dbfcs.IT_CheckSheetMTC_Devices.Where(w => w.is_delete == 0 && w.Type == "Software").ToList();
            var qryDeviceSoftware = from dv in dbfcs.IT_CheckSheetMTC_Devices
                                    join tl in dbfcs.IT_ChecksSheetMTC_Tools on dv.Tools_ID  equals tl.ID
                                    where dv.Type == "Software"
                                    select new
                                    {
                                        dv.ID,
                                        dv.Tools_ID,
                                        dv.Device,
                                        dv.Type,
                                        tl.Tools
                                    };
            var deviceSoftware = qryDeviceSoftware.ToList();
            int countDevicesSoftware = deviceSoftware.Count();
            List<Tbl_CheckSheetMTC_Device_Checklist> dataChecklistSoftware = new List<Tbl_CheckSheetMTC_Device_Checklist>();

            no = 0;
            // variable untuk menghitung index array tools
            i = 0;
            var toolsName = "";
            foreach (var itemDeviceS in deviceSoftware)
            {
                no++;
                if(toolsName == itemDeviceS.Tools)
                {
                    toolsName = "";
                } else
                {
                    toolsName = itemDeviceS.Tools;
                }
                    //var ToolsName = "";
                    //if (i >= countToolsSoftware)
                    //{
                    //    ToolsName = "";
                    //}

                    //else if (i < countToolsSoftware)
                    //{
                    //    List<IT_ChecksSheetMTC_Tools> toolsData = new List<IT_ChecksSheetMTC_Tools>();
                    //    var stoolsData = ToolsSoftware[i];

                    //    ToolsName = stoolsData.Tools;
                    //}
                dataChecklistSoftware.Add(
                    new Tbl_CheckSheetMTC_Device_Checklist
                    {
                        ID = itemDeviceS.ID,
                        NO = no,
                        Type = "",
                        Tools = toolsName,
                        Device = itemDeviceS.Device
                    });

                toolsName = itemDeviceS.Tools;
                
                i++;
            }

            ViewBag.DeviceHardware = dataChecklistHardware;
            ViewBag.DeviceSoftware = dataChecklistSoftware;
            ViewBag.countDevicesHardware = countDevicesHardware;
            ViewBag.countDevicesSoftware = countDevicesSoftware;
            ViewBag.userList = userList;
            ViewBag.currUser = CurrUser;
            return View();
        }
        [HttpPost]
        public ActionResult AddCheckSheetMtcHardSoft(string[] Lepas, string[] Pasang, string[] Bersihkan, string[] Check)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var nik = Request.Form.Get("nik");
            var spec = Request.Form.Get("spec");
            var year = Request.Form.Get("year");
            var userDetail = db.V_Users_Active.Where(w => w.NIK == nik).FirstOrDefault();

            IT_CheckSheetMTC_Header header = new IT_CheckSheetMTC_Header();
            header.Dept = userDetail.DeptName;
            header.Section = userDetail.SectionName;
            header.NIK = nik;
            header.Nama = userDetail.Name;
            header.Spesifikasi = spec;
            header.TahunMulaiPemakaian = year;
            header.Status = 1;
            header.CreateBy = currUser;
            header.CreateByName = CurrUser.Name;
            header.CreateTime = DateTime.Now;
            dbfcs.IT_CheckSheetMTC_Header.Add(header);
            dbfcs.SaveChanges();

            int insertedId = header.ID;

            // ambil data master devices
            var deviceHardware = dbfcs.IT_CheckSheetMTC_Devices.Where(w => w.is_delete == 0 && w.Type == "Hardware").ToList();
            int countDevicesHardware = deviceHardware.Count();
            List<IT_CheckSheetMTC_Detail> listDetail = new List<IT_CheckSheetMTC_Detail>();
            // insert devices hardware ke table  IT_CheckSheetMTC_detail
            foreach (var devicesH in deviceHardware) {
                IT_CheckSheetMTC_Detail Detail = new IT_CheckSheetMTC_Detail();
                Detail.HeaderID = insertedId;
                Detail.DevicesID = devicesH.ID;
                Detail.DevicesName = devicesH.Device;
                if (Lepas != null)
                {
                    if (Lepas.Contains(Detail.DevicesID.ToString()))
                    {
                        Detail.Lepas = 1;
                    }
                    else
                    {
                        Detail.Lepas = 0;
                    }
                } else
                {
                    Detail.Lepas = 0;
                }
                if (Pasang != null)
                {
                    if (Pasang.Contains(Detail.DevicesID.ToString()))
                    {
                        Detail.Pasang = 1;
                    }
                    else
                    {
                        Detail.Pasang = 0;
                    }
                } else
                {
                    Detail.Pasang = 0;
                }
                if (Bersihkan != null)
                {
                    if (Bersihkan.Contains(Detail.DevicesID.ToString()))
                    {
                        Detail.Bersihkan = 1;
                    }
                    else
                    {
                        Detail.Bersihkan = 0;
                    }
                } else
                {
                    Detail.Pasang = 0;
                }
                Detail.Checked = 0;

                dbfcs.IT_CheckSheetMTC_Detail.Add(Detail);
            }
            var deviceSoftware = dbfcs.IT_CheckSheetMTC_Devices.Where(w => w.is_delete == 0 && w.Type == "Software").ToList();
            int countDevicesSoftware = deviceSoftware.Count();
            // insert devices hardware ke table  IT_CheckSheetMTC_detail
            foreach (var devicesS in deviceSoftware)
            {
                IT_CheckSheetMTC_Detail Detail = new IT_CheckSheetMTC_Detail();
                Detail.HeaderID = insertedId;
                Detail.DevicesID = devicesS.ID;
                Detail.DevicesName = devicesS.Device;
                Detail.Lepas = 0;
                Detail.Bersihkan = 0;
                Detail.Pasang = 0;
                if (Check != null)
                {
                    if (Check.Contains(Detail.DevicesID.ToString()))
                    {
                        Detail.Checked = 1;
                    }
                    else
                    {
                        Detail.Checked = 0;
                    }
                } else
                {
                    Detail.Checked = 0;
                }
                dbfcs.IT_CheckSheetMTC_Detail.Add(Detail);
            }

            dbfcs.SaveChanges();
            return Json(new { status = "1", msg = "oke", versionMVC = typeof(Controller).Assembly.GetName().Version.ToString(), dataL = Lepas, list = listDetail });
        }
        [HttpGet]
        public ActionResult DetailCheckSheetMtcHardSoft(int HeaderID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            // get Approval IT User
            var approvalUserIT = db.Master_Organization.Where(w => w.DeptCode == "B2400").ToList();
            
            var ToolsHardware = dbfcs.IT_ChecksSheetMTC_Tools.Where(w => w.is_delete == 0 && w.Type == "Hardware").ToList();
            int countToolsHardware = ToolsHardware.Count();

            var deviceHardware = from detail in dbfcs.IT_CheckSheetMTC_Detail
                                 join devices in dbfcs.IT_CheckSheetMTC_Devices
                                 on detail.DevicesID equals devices.ID
                                 where (detail.HeaderID == HeaderID && devices.Type == "Hardware")
                                 select new { devices.ID, devices.Device, detail.Lepas, detail.Pasang, detail.Bersihkan };

            int countDevicesHardware = deviceHardware.Count();

            // get data header
            var DataMTCHardSoft = dbfcs.IT_CheckSheetMTC_Header.Where(w => w.ID == HeaderID).FirstOrDefault();

            List<Tbl_CheckSheetMTC_Device_Checklist> dataChecklistHardware = new List<Tbl_CheckSheetMTC_Device_Checklist>();

            int no = 0;
            // variable untuk menghitung index array tools
            int i = 0;
            foreach (var itemDevice in deviceHardware)
            {
                no++;

                var ToolsName = "";
                if (i >= countToolsHardware)
                {
                    ToolsName = "";
                }

                else if (i < countToolsHardware)
                {
                    List<IT_ChecksSheetMTC_Tools> toolsData = new List<IT_ChecksSheetMTC_Tools>();
                    var stoolsData = ToolsHardware[i];

                    ToolsName = stoolsData.Tools;
                }

                dataChecklistHardware.Add(
                    new Tbl_CheckSheetMTC_Device_Checklist
                    {
                        ID = itemDevice.ID,
                        NO = no,
                        Type = "",
                        Tools = ToolsName,
                        Device = itemDevice.Device,
                        Lepas = itemDevice.Lepas,
                        Pasang = itemDevice.Pasang,
                        Bersihkan = itemDevice.Bersihkan,
                    });

                i++;
            }

            var ToolsSoftware = dbfcs.IT_ChecksSheetMTC_Tools.Where(w => w.is_delete == 0 && w.Type == "Software").ToList();
            int countToolsSoftware = ToolsSoftware.Count();
            var deviceSoftware = from detail in dbfcs.IT_CheckSheetMTC_Detail
                                 join devices in dbfcs.IT_CheckSheetMTC_Devices
                                 on detail.DevicesID equals devices.ID
                                 join tl in dbfcs.IT_ChecksSheetMTC_Tools 
                                 on devices.Tools_ID equals tl.ID
                                 where (detail.HeaderID == HeaderID && devices.Type == "Software")
                                 select new { devices.ID, devices.Device, detail.Checked, tl.Tools };

            int countDevicesSoftware = deviceSoftware.Count();
            List<Tbl_CheckSheetMTC_Device_Checklist> dataChecklistSoftware = new List<Tbl_CheckSheetMTC_Device_Checklist>();

            no = 0;
            // variable untuk menghitung index array tools
            i = 0;
            var tlsName = "";
            foreach (var itemDeviceS in deviceSoftware)
            {
                no++;
                if (tlsName == itemDeviceS.Tools)
                {
                    tlsName = "";
                }
                else
                {
                    tlsName = itemDeviceS.Tools;
                }

                //var ToolsName = "";
                //if (i >= countToolsSoftware)
                //{
                //    ToolsName = "";
                //}

                //else if (i < countToolsSoftware)
                //{
                //    List<IT_ChecksSheetMTC_Tools> toolsData = new List<IT_ChecksSheetMTC_Tools>();
                //    var stoolsData = ToolsSoftware[i];

                //    ToolsName = stoolsData.Tools;
                //}

                dataChecklistSoftware.Add(
                    new Tbl_CheckSheetMTC_Device_Checklist
                    {
                        ID = itemDeviceS.ID,
                        NO = no,
                        Type = "",
                        Tools = tlsName,
                        Device = itemDeviceS.Device,
                        Check = itemDeviceS.Checked
                    });
                tlsName = itemDeviceS.Tools;
                i++;
            }

            ViewBag.DeviceHardware = dataChecklistHardware;
            ViewBag.DeviceSoftware = dataChecklistSoftware;
            ViewBag.countDevicesHardware = countDevicesHardware;
            ViewBag.countDevicesSoftware = countDevicesSoftware;
            ViewBag.DataMTCHardSoft = DataMTCHardSoft;
            ViewBag.CurrUser = currUser;
            ViewBag.ApprovalUser = approvalUserIT;

            //return Json( new { data = DataMTCHardSoft, HeaderID = HeaderID }, JsonRequestBehavior.AllowGet);
            return View();
        }
        [HttpPost]
        public ActionResult UpdateCheckSheetMtcHardSoft(int HeaderID, string[] Lepas, string[] Pasang, string[] Bersihkan, string[] Check)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var spec = Request.Form.Get("spec");
            var year = Request.Form.Get("year");
            // ambil informasi header berdasarkan headerID 
            var dataHeader = dbfcs.IT_CheckSheetMTC_Header.Where(w => w.ID == HeaderID).FirstOrDefault();
            dataHeader.Spesifikasi = spec;
            dataHeader.TahunMulaiPemakaian = year;
            
            // ambil data dari table detail
            var deviceHardware = from detail in dbfcs.IT_CheckSheetMTC_Detail
                                 join devices in dbfcs.IT_CheckSheetMTC_Devices
                                 on detail.DevicesID equals devices.ID
                                 where (detail.HeaderID == HeaderID && devices.Type == "Hardware")
                                 select new { devices.ID, devices.Device, detail.Lepas, detail.Pasang, detail.Bersihkan };
            int countDevicesHardware = deviceHardware.Count();
            List<IT_CheckSheetMTC_Detail> listDetail = new List<IT_CheckSheetMTC_Detail>();
            // insert devices hardware ke table  IT_CheckSheetMTC_detail
            foreach (var devicesH in deviceHardware)
            {
                var detail = dbfcs.IT_CheckSheetMTC_Detail.Where(w => w.ID == devicesH.ID).FirstOrDefault();

                if (Lepas.Contains(detail.ID.ToString()))
                {
                    detail.Lepas = 1;
                }
                else
                {
                    detail.Lepas = 0;
                }
                if (Pasang.Contains(detail.ID.ToString()))
                {
                    detail.Pasang = 1;
                }
                else
                {
                    detail.Pasang = 0;
                }
                if (Bersihkan.Contains(detail.ID.ToString()))
                {
                    detail.Bersihkan = 1;
                }
                else
                {
                    detail.Bersihkan = 0;
                }

            }
            var deviceSoftware = from detail in dbfcs.IT_CheckSheetMTC_Detail
                                 join devices in dbfcs.IT_CheckSheetMTC_Devices
                                 on detail.DevicesID equals devices.ID
                                 where (detail.HeaderID == HeaderID && devices.Type == "Software")
                                 select new { detail.ID , devices.Device, detail.Lepas, detail.Pasang, detail.Bersihkan };
            int countDevicesSoftware = deviceSoftware.Count();
            // insert devices hardware ke table  IT_CheckSheetMTC_detail
            foreach (var devicesS in deviceSoftware)
            {
                var detail = dbfcs.IT_CheckSheetMTC_Detail.Where(w => w.ID == devicesS.ID).FirstOrDefault();
                if (Check.Contains(detail.DevicesID.ToString()))
                {
                    detail.Checked = 1;
                }
                else
                {
                    detail.Checked = 0;
                }
            }

            dbfcs.SaveChanges();
            return Json(new { status = "1", msg = "oke", versionMVC = typeof(Controller).Assembly.GetName().Version.ToString(), dataL = Lepas, list = listDetail });
        }
        [HttpPost]
        public ActionResult SignCheckSheetMtcHardSoft(int HeaderID, string ProcessAction)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();            
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var dataHeader = dbfcs.IT_CheckSheetMTC_Header.Where(w => w.ID == HeaderID).FirstOrDefault();
            if (ProcessAction == "Check")
            {
                dataHeader.CheckedBy = CurrUser.NIK;
                dataHeader.CheckedByName = CurrUser.Name;
                dataHeader.CheckedTime = DateTime.Now;
                dataHeader.Status = 2;

                //insert ke table task_list untuk notifikasi verifikasi user
                Task_List task = new Task_List();
                task.TaskName = "Approval";
                task.TaskForUser = dataHeader.NIK;
                task.ModuleArea = "IT";
                task.ModuleController = "FormCheckSheet";
                task.Module = "DetailCheckSheetMtcHardSoft";
                task.IsActive = 1;
                task.ModuleID = HeaderID.ToString();
                task.ModuleParameter = "HeaderID";

                db.Task_List.Add(task);
                db.SaveChanges();

            } else if (ProcessAction == "Sign")
            {
                dataHeader.SignBy = CurrUser.NIK;
                dataHeader.SignTime = DateTime.Now;
                dataHeader.SignByName = CurrUser.Name;
                dataHeader.Status = 3;

                // insert ke table task_list untuk notifikasi approval user
                var approvalUserIT = db.Master_Organization.Where(w => w.DeptCode == "B2400").ToList();
                foreach(var approval in approvalUserIT)
                {
                    Task_List task = new Task_List();
                    task.TaskName = "Approval";
                    task.TaskForUser = approval.OrganizationUser;
                    task.ModuleArea = "IT";
                    task.ModuleController = "FormCheckSheet";
                    task.Module = "DetailCheckSheetMtcHardSoft";
                    task.IsActive = 1;
                    task.ModuleID = HeaderID.ToString();
                    task.ModuleParameter = "HeaderID";

                    db.Task_List.Add(task);
                }

                db.SaveChanges();
                

            }
            else if (ProcessAction == "Approve")
            {
                dataHeader.ApproveBy = CurrUser.NIK;
                dataHeader.ApproveByName = CurrUser.Name;
                dataHeader.ApproveTime = DateTime.Now;
                dataHeader.Status = 4;

            }

            int s = dbfcs.SaveChanges();
            if (s > 0)
            {
                return Json(new { status = 1, msg = "Sign Successfully" });
            }
            else
            {
                return Json(new { status = 0, msg = "Sign Failes, Please Contact the Application Administrator" });
            }
        }

    }
}