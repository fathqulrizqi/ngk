using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System.IO;
using NGKBusi.Models;
using NGKBusi.Areas.MTN.Models;

namespace NGKBusi.Areas.MTN.Controllers
{
    public class MachineDatabaseController : Controller
    {
        MachineDatabaseConnection db = new MachineDatabaseConnection();
        DefaultConnection dbDef = new DefaultConnection();

        // GET: MTN/MachineDatabase
        [Authorize]
        public ActionResult Index()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = (from usr in dbDef.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 22
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }
            ViewBag.NavHide = true;
            ViewBag.areaList = db.MTN_MachineDatabase_Area.OrderBy(o => o.Name).ToList();
            ViewBag.machineList = db.MTN_MachineDatabase_List.Where(z => z.Machine_Parent_ID == null).ToList();
            return View();
        }
        [Authorize]
        public ActionResult Overhaul()
        {
            ViewBag.MachineArea = db.MTN_MachineDatabase_Area.ToList();
            return View();
        }

        public ActionResult OverhaulArea()
        {
            var currArea = Request["iOverhaulArea[]"].Split(',').ToList();
            var Area = db.MTN_MachineDatabase_Area.Where(w => currArea.Contains(w.id.ToString())).Select(s => new
            {
                id = s.id,
                content = s.Name
            });
            return Json(Area, JsonRequestBehavior.AllowGet);
        }
        public ActionResult OverhaulItem()
        {
            var currArea = Request["iOverhaulArea[]"].Split(',').ToList();
            var currOverhaul = db.MTN_MachineDatabase_List.Where(w => currArea.Contains(w.Area_ID.ToString())).Select(s => new
            {
                id = s.id.ToString(),
                group = s.Area_ID,
                content = s.Name + (s.End_Date == null ? "" : "(Write Off)"),
                start = s.Start_Date.ToString(),
                end = s.End_Date == null ? null : s.End_Date.ToString(),
                className = (s.End_Date == null ? "over-green" : "over-red")
            });
            var overhaulPlan = db.MTN_MachineDatabase_List.Where(w => currArea.Contains(w.Area_ID.ToString()) && w.Overhaul_Schedule != null).Select(s => new
            {
                id = s.id + "_" + s.Area_ID,
                group = s.Area_ID,
                content = s.Name + (s.Overhaul_Schedule == null ? "" : "(Overhaul Plan)"),
                start = s.Overhaul_Schedule.ToString(),
                end = s.Overhaul_Schedule != null ? null : s.Overhaul_Schedule.ToString(),
                className = "over-orange"
            });
            var Overhaul = currOverhaul.Concat(overhaulPlan);
            return Json(Overhaul, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult MDDelete()
        {
            var currID = Int32.Parse(Request["iID"]);
            var currData = db.MTN_MachineDatabase_List.Where(w => w.id == currID).FirstOrDefault();
            db.MTN_MachineDatabase_List.Remove(currData);
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult MDAdd()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var newData = new MTN_MachineDatabase_List();
            newData.Area_ID = Int32.Parse(Request["iMDArea"]);
            newData.Machine_Old_ID = !String.IsNullOrEmpty(Request["iMachineOld"]) ? Int32.Parse(Request["iMachineOld"]) : (int?)null;
            newData.Machine_Parent_ID = !String.IsNullOrEmpty(Request["iMachineParent"]) ? Int32.Parse(Request["iMachineParent"]) : (int?)null;
            newData.Name = Request["iMachine"];
            newData.Machine_Number = Request["iMDMachineNo"];
            newData.Asset_No = Request["iMDAssetNo"];
            newData.Model = Request["iMDModel"];
            newData.Power = Request["iMDPower"];
            newData.Maker = Request["iMDMaker"];
            newData.Serial_No = Request["iMDSerial"];
            newData.Coming_Date = !String.IsNullOrEmpty(Request["iMDComingDate"]) ? DateTime.ParseExact(Request["iMDComingDate"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.Qty = !String.IsNullOrEmpty(Request["iMDQty"]) ? Int32.Parse(Request["iMDQty"]) : (int?)null;
            newData.Start_Date = !String.IsNullOrEmpty(Request["iMDStartDate"]) ? DateTime.ParseExact(Request["iMDStartDate"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.End_Date = !String.IsNullOrEmpty(Request["iMDEndDate"]) ? DateTime.ParseExact(Request["iMDEndDate"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.Overhaul_Schedule = !String.IsNullOrEmpty(Request["iMDOverhaulSchedule"]) ? DateTime.ParseExact(Request["iMDOverhaulSchedule"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.Remark = Request["iMDRemark"];
            newData.Status = Request["iMDStatus"];
            newData.Issued_By = currUser.GetUserId();
            newData.Issued_Date = DateTime.Now;
            newData.Is_Scheduled = Request["iIsScheduled"] != null ? true : false;
            db.MTN_MachineDatabase_List.Add(newData);
            db.SaveChanges();

            if (!String.IsNullOrEmpty(Request["iMachineOld"]))
            {
                var oldMachine = Int32.Parse(Request["iMachineOld"]);
                var changeData = db.MTN_MachineDatabase_List.Where(w => w.Machine_Parent_ID == oldMachine).ToList();
                changeData.ForEach(f => f.Machine_Parent_ID = newData.id);
                db.SaveChanges();
            }

            return RedirectToAction("Index", "MachineDatabase", new { area = "MTN" });
        }

        [HttpPost]
        public ActionResult MDEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Int32.Parse(Request["iMachineID"]);
            var newData = db.MTN_MachineDatabase_List.Where(w => w.id == currID).FirstOrDefault();
            newData.Area_ID = Int32.Parse(Request["iMDArea"]);
            newData.Name = Request["iMachine"];
            newData.Machine_Number = Request["iMDMachineNo"];
            newData.Asset_No = Request["iMDAssetNo"];
            newData.Model = Request["iMDModel"];
            newData.Power = Request["iMDPower"];
            newData.Maker = Request["iMDMaker"];
            newData.Serial_No = Request["iMDSerial"];
            newData.Coming_Date = !String.IsNullOrEmpty(Request["iMDComingDate"]) ? DateTime.ParseExact(Request["iMDComingDate"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.Qty = !String.IsNullOrEmpty(Request["iMDQty"]) ? Int32.Parse(Request["iMDQty"]) : (int?)null;
            newData.Start_Date = !String.IsNullOrEmpty(Request["iMDStartDate"]) ? DateTime.ParseExact(Request["iMDStartDate"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.End_Date = !String.IsNullOrEmpty(Request["iMDEndDate"]) ? DateTime.ParseExact(Request["iMDEndDate"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.Overhaul_Schedule = !String.IsNullOrEmpty(Request["iMDOverhaulSchedule"]) ? DateTime.ParseExact(Request["iMDOverhaulSchedule"], "MMM-yyyy", CultureInfo.InvariantCulture) : (DateTime?)null;
            newData.Remark = Request["iMDRemark"];
            newData.Status = Request["iMDStatus"];
            newData.Updated_By = currUser.GetUserId();
            newData.Updated_Date = DateTime.Now;
            newData.Is_Scheduled = Request["iIsScheduled"] != null ? true : false;
            db.SaveChanges();

            return RedirectToAction("Index", "MachineDatabase", new { area = "MTN" });
        }
        [Authorize]
        public ActionResult MaintenanceSchedule()
        {
            Int32 period = !String.IsNullOrEmpty(Request["iPeriod"]) ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month <= 3 ? DateTime.Now.Year - 1 : DateTime.Now.Year);
            var periodFY = !String.IsNullOrEmpty(Request["iPeriodFY"]) ? Request["iPeriodFY"] : "FY1" + (DateTime.Now.Month <= 3 ? DateTime.Now.Year.ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2));
            ViewBag.PeriodFY = periodFY;
            ViewBag.Period = period;
            ViewBag.IsLock = db.MTN_MachineDatabase_Maintenance_Schedule_Header.Where(w => w.Period_FY == periodFY).FirstOrDefault()?.isLock ?? false;
            ViewBag.Header = db.MTN_MachineDatabase_Maintenance_Schedule_Header.Where(w => w.Period_FY == periodFY).FirstOrDefault();
            ViewBag.MachineList = db.V_MachineDatabase_Maintenance_Schedule.Where(w => w.Period_FY == periodFY).Select(s => new VBMachineList { Area_ID = s.Area_ID, Area = s.Area, Machine_ID = s.Machine_ID, Machine_Name = s.Machine_Name, Machine_No = s.Machine.Machine_Number, Period_FY = s.Period_FY, Sequence = s.Sequence }).Distinct().OrderBy(o => o.Sequence).ToList();

            return View();
        }
        public class VBMachineList
        {
            public int? Area_ID { get; set; }
            public string Area { get; set; }
            public int? Machine_ID { get; set; }
            public string Machine_Name { get; set; }
            public string Machine_No { get; set; }
            public string Period_FY { get; set; }
            public int? Year { get; set; }
            public int? Month { get; set; }
            public int? Day { get; set; }
            public string Category { get; set; }
            public int? Sequence { get; set; }
        }
        public JsonResult setMaintenanceSchedule()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var areaID = Int32.Parse(Request["iAreaID"]);
            var area = Request["iArea"];
            var machineID = Int32.Parse(Request["iMachineID"]);
            var machineNo = Request["iMachineNo"];
            var machine = Request["iMachine"];
            var dataYear = Int32.Parse(Request["iYear"]);
            var dataFY = Request["iFY"];
            var dataMonth = Int32.Parse(Request["iMonth"]);
            var dataSchedule = Request["iSchedule"];
            //Check If Record Already Exists============================= Start ==========
            var updateData = db.MTN_MachineDatabase_Maintenance_Schedule.Where(w => w.Area_ID == areaID && w.Machine_ID == machineID && w.Period_FY == dataFY && w.Month == dataMonth).FirstOrDefault();
            bool? lastStatus = true;
            if (updateData != null)
            {
                if (dataSchedule == "3B")
                {
                    lastStatus = !updateData.is3B;
                    updateData.is3B = lastStatus;
                }
                else if (dataSchedule == "6B")
                {
                    lastStatus = !updateData.is6B;
                    updateData.is6B = lastStatus;
                }
                else
                {
                    lastStatus = !updateData.is1T;
                    updateData.is1T = lastStatus;
                }
                updateData.Updated_By = currUserID;
                updateData.Updated_Date = DateTime.Now;
            }
            else
            {
                var newData = new MTN_MachineDatabase_Maintenance_Schedule();
                newData.Area_ID = areaID;
                newData.Area = area;
                newData.Machine_ID = machineID;
                newData.Machine_Name = machine;
                newData.Period_FY = dataFY;
                newData.Year = dataYear;
                newData.Month = dataMonth;
                newData.is3B = dataSchedule == "3B" ? true : false;
                newData.is6B = dataSchedule == "6B" ? true : false;
                newData.is1T = dataSchedule == "1T" ? true : false;
                newData.Issued_By = currUserID;
                newData.Issued_Date = DateTime.Now;
                db.MTN_MachineDatabase_Maintenance_Schedule.Add(newData);
            }

            //Check If Record Already Exists============================= End ============

            //Check If Header Already Exists ============================= Start ==========
            var checkHeader = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.Period_FY == dataFY && w.Machine_Name == machine && w.Area == area && w.Machine_No == machineNo).FirstOrDefault();
            if (checkHeader == null)
            {
                var newHeader = new MTN_MachineDatabase_Maintenance_Checklist_Header();
                newHeader.Period_FY = dataFY;
                newHeader.Period = dataYear;
                newHeader.Area = area;
                newHeader.Machine_Name = machine;
                newHeader.Machine_No = machineNo;
                newHeader.Created_At = DateTime.Now;
                newHeader.Created_By = currUserID;
                db.MTN_MachineDatabase_Maintenance_Checklist_Header.Add(newHeader);

                for (var i = 1; i <= 12; i++)
                {
                    var newHeaderMonthly = new MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header();
                    newHeaderMonthly.Period_FY = dataFY;
                    newHeaderMonthly.Year = dataYear;
                    newHeaderMonthly.Month = i;
                    newHeaderMonthly.Approval = 1;
                    newHeaderMonthly.Approval_Sub = 0;
                    db.MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header.Add(newHeaderMonthly);
                };
            }
            //Check  If Header Already Exists ============================= End ============


            db.SaveChanges();
            return Json(lastStatus, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult getMaintenanceSchedule()
        {
            var areaID = Int32.Parse(Request["iAreaID"]);
            var area = Request["iArea"];
            var machineID = Int32.Parse(Request["iMachineID"]);
            var machine = Request["iMachine"];
            var dataYear = Int32.Parse(Request["iYear"]);
            var dataFY = Request["iFY"];
            var getData = db.MTN_MachineDatabase_Maintenance_Schedule.Where(w => w.Area_ID == areaID && w.Machine_ID == machineID && w.Period_FY == dataFY).ToList();

            var headerData = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.Period_FY == dataFY).ToList();

            return Json(new { schedules = getData, headers = headerData }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult signMaintenanceSchedule()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currPeriodFY = Request["iPeriodFY"];
            var btnType = Request["btnType"];
            var updateSign = db.MTN_MachineDatabase_Maintenance_Schedule_Header.Where(w => w.Period_FY == currPeriodFY).FirstOrDefault();
            var curApproval = updateSign.Approval;
            var curApprovalSub = updateSign.Approval_Sub;
            var checkApprovalMaster = dbDef.Approval_Master.Where(w => w.Document_Id == 1 && w.Menu_Id == 22 && w.Levels == updateSign.Approval && w.Levels_Sub > updateSign.Approval_Sub).OrderBy(o => o.Levels_Sub).FirstOrDefault();


            if (btnType != "Return")
            {
                if (checkApprovalMaster != null)
                {
                    updateSign.Approval_Sub = checkApprovalMaster.Levels_Sub;
                }
                else
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
            }
            else
            {
                updateSign.Approval_Sub -= 1;
            }
            db.SaveChanges();

            var getApprovalMaster = dbDef.Approval_Master.Where(w => w.User_NIK == currUserID && w.Document_Id == 1 && w.Menu_Id == 22).FirstOrDefault();
            var updateSignHistory = new Approval_History();
            updateSignHistory.Menu_Id = 22;
            updateSignHistory.Menu_Name = "Maintenance Schedule";
            updateSignHistory.Document_Id = 1;
            updateSignHistory.Document_Name = "Yearly Schedule";
            updateSignHistory.Reveral_ID = currPeriodFY;
            updateSignHistory.Reveral_ID_Sub = null;
            updateSignHistory.Title = getApprovalMaster.Title;
            updateSignHistory.Header = getApprovalMaster.Header;
            updateSignHistory.Label = getApprovalMaster.Label;
            updateSignHistory.Note = "";
            updateSignHistory.Approval = curApproval;
            updateSignHistory.Approval_Sub = curApprovalSub;
            updateSignHistory.IsReject = false;
            updateSignHistory.Status = (btnType != "Sign" ? btnType : ApprovalStatus(updateSign.Approval, updateSign.Approval_Sub));
            updateSignHistory.Created_At = DateTime.Now;
            updateSignHistory.Created_By_ID = currUserID;
            updateSignHistory.Created_By_Name = currUserName;
            dbDef.Approval_History.Add(updateSignHistory);
            dbDef.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }
        public ActionResult signMaintenanceScheduleMonthly()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currPeriodFY = Request["iPeriodFY"];
            var currYear = int.Parse(Request["iYear"]);
            var currMonth = int.Parse(Request["iMonth"]);
            var btnType = Request["btnType"];
            var updateSign = db.MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header.Where(w => w.Period_FY == currPeriodFY && w.Year == currYear && w.Month == currMonth).FirstOrDefault();
            var curApproval = updateSign.Approval;
            var curApprovalSub = updateSign.Approval_Sub;
            var checkApprovalMaster = dbDef.Approval_Master.Where(w => w.Document_Id == 2 && w.Menu_Id == 22 && w.Levels == updateSign.Approval && w.Levels_Sub > updateSign.Approval_Sub).OrderBy(o => o.Levels_Sub).FirstOrDefault();

            if (btnType != "Return")
            {
                if (checkApprovalMaster != null)
                {
                    updateSign.Approval_Sub = checkApprovalMaster.Levels_Sub;
                }
                else
                {
                    updateSign.Approval += 1;
                    updateSign.Approval_Sub = 0;
                }
            }
            else
            {
                updateSign.Approval_Sub -= 1;
            }
            db.SaveChanges();

            var getApprovalMaster = dbDef.Approval_Master.Where(w => w.User_NIK == currUserID && w.Document_Id == 1 && w.Menu_Id == 22).FirstOrDefault();
            var updateSignHistory = new Approval_History();
            updateSignHistory.Menu_Id = 22;
            updateSignHistory.Menu_Name = "Maintenance Schedule";
            updateSignHistory.Document_Id = 2;
            updateSignHistory.Document_Name = "Monthly Schedule";
            updateSignHistory.Reveral_ID = currPeriodFY;
            updateSignHistory.Reveral_ID_Sub = null;
            updateSignHistory.Title = getApprovalMaster.Title;
            updateSignHistory.Header = getApprovalMaster.Header;
            updateSignHistory.Label = getApprovalMaster.Label;
            updateSignHistory.Note = "";
            updateSignHistory.Approval = curApproval;
            updateSignHistory.Approval_Sub = curApprovalSub;
            updateSignHistory.IsReject = false;
            updateSignHistory.Status = (btnType != "Sign" ? btnType : ApprovalStatus(updateSign.Approval, updateSign.Approval_Sub));
            updateSignHistory.Created_At = DateTime.Now;
            updateSignHistory.Created_By_ID = currUserID;
            updateSignHistory.Created_By_Name = currUserName;
            dbDef.Approval_History.Add(updateSignHistory);
            dbDef.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }
        public ActionResult signMaintenanceChecklist()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var currUserName = currUser.FindFirstValue("fullName");
            var currChecklistID = int.Parse(Request["iChecklistID"]);
            var currApprovalNumber = int.Parse(Request["iApprovalNumber"]);
            var currApprovalType = Request["iApprovalType"];
            var btnType = Request["btnType"];
            var curApproval = 1;
            var curApprovalSub = 0;
            var updateSign = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.ID == currChecklistID).FirstOrDefault();
            if (btnType == "Sign")
            {
                switch (currApprovalType)
                {
                    case "Approver":
                        curApproval = 1;
                        curApprovalSub = 1;
                        switch (currApprovalNumber)
                        {
                            case 1:
                                updateSign.Approved_By1 = currUserID;
                                updateSign.Approved_At1 = DateTime.Now;
                                break;
                            case 2:
                                updateSign.Approved_By2 = currUserID;
                                updateSign.Approved_At2 = DateTime.Now;
                                break;
                            case 3:
                                updateSign.Approved_By3 = currUserID;
                                updateSign.Approved_At3 = DateTime.Now;
                                break;
                            default:
                                updateSign.Approved_By4 = currUserID;
                                updateSign.Approved_At4 = DateTime.Now;
                                break;
                        }
                        break;
                    default:
                        switch (currApprovalNumber)
                        {
                            case 1:
                                updateSign.Checked_By1 = currUserID;
                                updateSign.Checked_At1 = DateTime.Now;
                                break;
                            case 2:
                                updateSign.Checked_By2 = currUserID;
                                updateSign.Checked_At2 = DateTime.Now;
                                break;
                            case 3:
                                updateSign.Checked_By3 = currUserID;
                                updateSign.Checked_At3 = DateTime.Now;
                                break;
                            default:
                                updateSign.Checked_By4 = currUserID;
                                updateSign.Checked_At4 = DateTime.Now;
                                break;
                        }
                        break;
                }
            }
            else
            {
                switch (currApprovalNumber)
                {
                    case 1:
                        updateSign.Checked_By1 = null;
                        updateSign.Checked_At1 = null;
                        break;
                    case 2:
                        updateSign.Checked_By2 = null;
                        updateSign.Checked_At2 = null;
                        break;
                    case 3:
                        updateSign.Checked_By3 = null;
                        updateSign.Checked_At3 = null;
                        break;
                    default:
                        updateSign.Checked_By4 = null;
                        updateSign.Checked_At4 = null;
                        break;
                }
            }
            db.SaveChanges();

            var getApprovalMaster = dbDef.Approval_Master.Where(w => w.User_NIK == currUserID && w.Document_Id == 1 && w.Menu_Id == 22).FirstOrDefault();
            var updateSignHistory = new Approval_History();
            updateSignHistory.Menu_Id = 22;
            updateSignHistory.Menu_Name = "Maintenance Checklist";
            updateSignHistory.Document_Id = 3;
            updateSignHistory.Document_Name = "Machine Checklist";
            updateSignHistory.Reveral_ID = currChecklistID.ToString();
            updateSignHistory.Reveral_ID_Sub = null;
            updateSignHistory.Title = getApprovalMaster.Title;
            updateSignHistory.Header = getApprovalMaster.Header;
            updateSignHistory.Label = getApprovalMaster.Label;
            updateSignHistory.Note = "";
            updateSignHistory.Approval = curApproval;
            updateSignHistory.Approval_Sub = curApprovalSub;
            updateSignHistory.IsReject = false;
            updateSignHistory.Status = btnType;
            updateSignHistory.Created_At = DateTime.Now;
            updateSignHistory.Created_By_ID = currUserID;
            updateSignHistory.Created_By_Name = currUserName;
            dbDef.Approval_History.Add(updateSignHistory);
            dbDef.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public String ApprovalStatus(int Approval, int Approval_Sub)
        {
            var stat = "Created";
            switch (Approval_Sub)
            {
                case 1:
                    stat = "Submitted";
                    break;
                case 2:
                    stat = "Checked";
                    break;
                case 3:
                    stat = "Approved";
                    break;
                case 4:
                    stat = "Approved";
                    break;
                default:
                    stat = "Created";
                    break;
            }

            return stat;
        }
        public String ApprovalHistory(string Reveral_ID, int Approval, int Approval_Sub, int getType, int documentID = 1)
        {
            var str = "";
            var getApprovalHistory = dbDef.Approval_History.Where(w => w.Menu_Id == 22 && w.Document_Id == documentID && w.Reveral_ID == Reveral_ID && w.Approval == Approval && w.Approval_Sub == Approval_Sub).OrderByDescending(o => o.id).FirstOrDefault();

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

        [HttpGet]
        public ActionResult MaintenanceChecklist()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var currPeriod = Int32.Parse(Request["iPeriod"]);
            var currPeriodFY = Request["iPeriodFY"];
            var currArea = Request["iArea"];
            var currMachineName = Request["iMachineName"];
            var currMachineNo = Request["iMachineNo"];

            //Check If Header Already Exists ============================= Start ==========
            var checkHeader = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.Period_FY == currPeriodFY && w.Machine_Name == currMachineName && w.Area == currArea && w.Machine_No == currMachineNo).FirstOrDefault();
            if (checkHeader == null)
            {
                if (!String.IsNullOrEmpty(Request["iPeriodFY"]))
                {
                    var newHeader = new MTN_MachineDatabase_Maintenance_Checklist_Header();
                    newHeader.Period_FY = currPeriodFY;
                    newHeader.Period = currPeriod;
                    newHeader.Area = currArea;
                    newHeader.Machine_Name = currMachineName;
                    newHeader.Machine_No = currMachineNo;
                    newHeader.Created_At = DateTime.Now;
                    newHeader.Created_By = currUserID;
                    db.MTN_MachineDatabase_Maintenance_Checklist_Header.Add(newHeader);

                    for (var i = 1; i <= 12; i++)
                    {
                        var newHeaderMonthly = new MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header();
                        newHeaderMonthly.Period_FY = currPeriodFY;
                        newHeaderMonthly.Year = currPeriod;
                        newHeaderMonthly.Month = i;
                        newHeaderMonthly.Approval = 1;
                        newHeaderMonthly.Approval_Sub = 0;
                        db.MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header.Add(newHeaderMonthly);
                    };

                    db.SaveChanges();
                }
            }
            //Check  If Header Already Exists ============================= End ============

            ViewBag.MachineItemList = db.MTN_MachineDatabase_Maintenance_Checklist_Master.Where(w => w.Machine_Name.Trim() == currMachineName.Trim()).ToList();

            return View();
        }
        public JsonResult setMChecklistHeaderDate()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var period = Int32.Parse(Request["iPeriod"]);
            var periodFY = Request["iPeriodFY"];
            var area = Request["iArea"];
            var machineName = Request["iMachineName"];
            var machineNo = Request["iMachineNo"];
            var currInterval = Request["iInterval"];
            DateTime? setDate = string.IsNullOrEmpty(Request["iDate"]) ? (DateTime?)null : Convert.ToDateTime(Request["iDate"]);

            var updateData = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.Period_FY == periodFY && w.Area == area && w.Machine_Name == machineName && w.Machine_No == machineNo).FirstOrDefault();
            switch (currInterval)
            {
                case "1":
                    updateData.Date1 = setDate;
                    updateData.Date1_By = currUserID;
                    break;
                case "2":
                    updateData.Date2 = setDate;
                    updateData.Date2_By = currUserID;
                    break;
                case "3":
                    updateData.Date3 = setDate;
                    updateData.Date3_By = currUserID;
                    break;
                case "4":
                    updateData.Date4 = setDate;
                    updateData.Date4_By = currUserID;
                    break;
            }
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult setMChecklistHeaderNote()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var period = Int32.Parse(Request["iPeriod"]);
            var area = Request["iArea"];
            var machineName = Request["iMachineName"];
            var machineNo = Request["iMachineNo"];
            var currInterval = Request["iInterval"];
            var note = Request["iNote"];

            var updateData = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.Period == period && w.Area == area && w.Machine_Name == machineName && w.Machine_No == machineNo).FirstOrDefault();
            switch (currInterval)
            {
                case "1":
                    updateData.Note1 = note;
                    break;
                case "2":
                    updateData.Note2 = note;
                    break;
                case "3":
                    updateData.Note3 = note;
                    break;
                case "4":
                    updateData.Note4 = note;
                    break;
            }
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult setMChecklistLines()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var iHeaderID = Int32.Parse(Request["iHeaderID"]);
            var iItemID = Int32.Parse(Request["iItemID"]);
            var iCategory = Request["iCategory"];
            var iInterval = Int32.Parse(Request["iInterval"]);
            var iModule = Request["iModule"];
            var iImageNote = Request["iImageNote"];
            var iCheckImage = Request["iCheckImage"];

            var linesData = db.MTN_MachineDatabase_Maintenance_Checklist_Lines.Where(w => w.Header_ID == iHeaderID && w.Item_ID == iItemID && w.Category == iCategory && w.Interval == iInterval).FirstOrDefault();
            var lineID = 0;
            if (linesData == null)
            {
                var newData = new MTN_MachineDatabase_Maintenance_Checklist_Lines();
                newData.Header_ID = iHeaderID;
                newData.Item_ID = iItemID;
                newData.Category = iCategory;
                newData.Interval = iInterval;
                newData.Created_At = DateTime.Now;
                newData.Created_By = currUserID;
                switch (iModule)
                {
                    case "ConditionOK":
                        newData.isCondition = true;
                        break;
                    case "ConditionNOT":
                        newData.isCondition = false;
                        break;
                    case "ActionOK":
                        newData.isCondition = true;
                        break;
                    case "ActionRepair":
                        newData.isAction = true;
                        break;
                    case "ActionChange":
                        newData.isAction = false;
                        break;
                }
                db.MTN_MachineDatabase_Maintenance_Checklist_Lines.Add(newData);
                db.SaveChanges();
                lineID = newData.ID;
            }
            else
            {
                switch (iModule)
                {
                    case "ConditionOK":
                        linesData.isCondition = true;
                        linesData.isAction = null;
                        break;
                    case "ConditionNOT":
                        linesData.isCondition = false;
                        linesData.isAction = null;
                        break;
                    case "ActionOK":
                        linesData.isCondition = true;
                        linesData.isAction = null;
                        break;
                    case "ActionRepair":
                        linesData.isCondition = false;
                        linesData.isAction = true;
                        break;
                    case "ActionChange":
                        linesData.isCondition = false;
                        linesData.isAction = false;
                        break;
                }
                linesData.Updated_At = DateTime.Now;
                linesData.Updated_By = currUserID;
                db.SaveChanges();
                lineID = linesData.ID;
            }
            this.setMChecklistReport(iHeaderID, lineID, iItemID, iInterval, iImageNote);
            return Json(lineID, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult getMChecklistHeaders()
        {
            Int32 period = !String.IsNullOrEmpty(Request["iPeriod"]) ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month < 3 ? DateTime.Now.Year - 1 : DateTime.Now.Year);
            var periodFY = !String.IsNullOrEmpty(Request["iPeriodFY"]) ? Request["iPeriodFY"] : "FY1" + (DateTime.Now.Month <= 3 ? DateTime.Now.Year.ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2));

            var linesData = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.Period_FY == periodFY).ToList();

            return Json(linesData, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult getMChecklistLines()
        {
            var iHeaderID = Int32.Parse(Request["iHeaderID"]);

            var linesData = db.MTN_MachineDatabase_Maintenance_Checklist_Lines.Where(w => w.Header_ID == iHeaderID).ToList();

            return Json(linesData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult setMChecklistReport(int headerID = 0, int lineID = 0, int itemID = 0, int cInterval = 0, string imageNote = "")
        {
            var currHeaderID = headerID;
            var currLineID = lineID;
            var currItemID = itemID;
            var currInterval = cInterval;
            var checkLines = db.MTN_MachineDatabase_Maintenance_Checklist_Lines.Where(w => w.ID == currLineID).FirstOrDefault();
            checkLines.Image_Note = imageNote;
            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFileBase iFile = Request.Files[i];
                // extract only the filename
                if (iFile.ContentLength > 0)
                {
                    var fileName = iFile.FileName;
                    string extension = Path.GetExtension(fileName);
                    // store the file inside ~/App_Data/uploads folder
                    var path = Path.Combine(Server.MapPath("~/Files/MTN/MachineChecklist/"), currHeaderID.ToString());
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    path = Path.Combine(Server.MapPath("~/Files/MTN/MachineChecklist/" + currHeaderID), fileName);
                    iFile.SaveAs(path);
                    var imgUrl = "/Files/MTN/MachineChecklist/" + currHeaderID + "/" + fileName;
                    var checkFile = db.MTN_MachineDatabase_Maintenance_Checklist_Report.Where(w => w.Header_ID == currHeaderID && w.Checklist_ID == currLineID && itemID == currItemID && w.Interval == currInterval && w.Image_URL == imgUrl).FirstOrDefault();
                    if (checkFile == null)
                    {
                        db.MTN_MachineDatabase_Maintenance_Checklist_Report.Add(new MTN_MachineDatabase_Maintenance_Checklist_Report()
                        {
                            Header_ID = currHeaderID,
                            Checklist_ID = currLineID,
                            Interval = currInterval,
                            Item_ID = currItemID,
                            Image_URL = imgUrl
                        });
                    }
                }
            }
            db.SaveChanges();

            return Json(true, JsonRequestBehavior.AllowGet);
            //return Redirect(Request.UrlReferrer.ToString());
        }
        public ActionResult MaintenanceScheduleMonthly()
        {
            Int32 period = !String.IsNullOrEmpty(Request["iPeriod"]) ? Int32.Parse(Request["iPeriod"]) : (DateTime.Now.Month <= 3 ? DateTime.Now.Year - 1 : DateTime.Now.Year);
            Int32 mth = !String.IsNullOrEmpty(Request["iMonth"]) ? Int32.Parse(Request["iMonth"]) : 1;
            Int32 periodLock = !String.IsNullOrEmpty(Request["iPeriod"]) && Int32.Parse(Request["iPeriod"]) > 3 ? Int32.Parse(Request["iPeriod"]) : (Int32.Parse(Request["iPeriod"]) - 1);
            ViewBag.IsLock = db.MTN_MachineDatabase_Maintenance_Schedule_Header.Where(w => w.Year == periodLock).FirstOrDefault()?.isLock ?? false;
            ViewBag.Period = period;
            ViewBag.Month = mth;
            var periodFY = !String.IsNullOrEmpty(Request["iPeriodFY"]) ? Request["iPeriodFY"] : "FY1" + (DateTime.Now.Month <= 3 ? DateTime.Now.Year.ToString().Substring(2, 2) : (DateTime.Now.Year + 1).ToString().Substring(2, 2));
            ViewBag.PeriodFY = periodFY;
            ViewBag.Header = db.MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header.Where(w => w.Period_FY == periodFY && w.Year == period && w.Month == mth).FirstOrDefault();
            ViewBag.MachineList = db.MTN_MachineDatabase_Maintenance_Schedule.Where(w => w.Period_FY == periodFY && w.Month == mth && (w.is3B == true || w.is6B == true || w.is1T == true)).Select(s => new VBMachineList { Area_ID = s.Area_ID, Area = s.Area, Machine_ID = s.Machine_ID, Machine_Name = s.Machine_Name, Machine_No = s.Machine.Machine_Number, Period_FY = s.Period_FY, Month = s.Month }).Distinct().OrderBy(o => o.Area_ID).ToList();

            return View();
        }

        public JsonResult setMaintenanceScheduleMonthly()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var areaID = Int32.Parse(Request["iAreaID"]);
            var area = Request["iArea"];
            var machineID = Int32.Parse(Request["iMachineID"]);
            var machineNo = Request["iMachineNo"];
            var machine = Request["iMachine"];
            var dataYear = Int32.Parse(Request["iYear"]);
            var dataFY = Request["iFY"];
            var dataMonth = Int32.Parse(Request["iMonth"]);
            var dataDay = Int32.Parse(Request["iDay"]);
            var dataCategory = Request["iCategory"];

            var updateData = db.MTN_MachineDatabase_Maintenance_Schedule_Monthly.Where(w => w.Area_ID == areaID && w.Machine_ID == machineID && w.Year == dataYear && w.Month == dataMonth && w.Day == dataDay).FirstOrDefault();
            if (updateData != null)
            {
                if (dataCategory == "X")
                {
                    db.MTN_MachineDatabase_Maintenance_Schedule_Monthly.Remove(updateData);
                }
                else
                {
                    updateData.Category = dataCategory;
                    updateData.Updated_By = currUserID;
                    updateData.Updated_Date = DateTime.Now;
                }
            }
            else
            {
                if (dataCategory != "X")
                {
                    var newData = new MTN_MachineDatabase_Maintenance_Schedule_Monthly();
                    newData.Area_ID = areaID;
                    newData.Area = area;
                    newData.Machine_ID = machineID;
                    newData.Machine_Name = machine;
                    newData.Year = dataYear;
                    newData.Period_FY = dataFY;
                    newData.Month = dataMonth;
                    newData.Day = dataDay;
                    newData.Category = dataCategory;
                    newData.Issued_By = currUserID;
                    newData.Issued_Date = DateTime.Now;
                    db.MTN_MachineDatabase_Maintenance_Schedule_Monthly.Add(newData);
                }
            }

            db.SaveChanges();
            return Json(dataCategory, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult getMaintenanceScheduleMonthly()
        {
            var areaID = Request["iAreaID"] != null ? Int32.Parse(Request["iAreaID"]) : 0;
            var area = Request["iArea"];
            var machineID = Request["iMachineID"] != null ? Int32.Parse(Request["iMachineID"]) : 0;
            var machine = Request["iMachine"];
            var dataYear = Request["iYear"] != null ? Int32.Parse(Request["iYear"]) : 0;
            var dataMonth = Request["iMonth"] != null ? Int32.Parse(Request["iMonth"]) : 0;
            var dataFY = Request["iFY"];
            var getData = db.MTN_MachineDatabase_Maintenance_Schedule_Monthly.Where(w => w.Area_ID == areaID && w.Machine_ID == machineID && w.Period_FY == dataFY && w.Month == dataMonth).ToList();

            var headerData = db.MTN_MachineDatabase_Maintenance_Checklist_Header.Where(w => w.Period_FY == dataFY).ToList();
            var scheduleYear = db.MTN_MachineDatabase_Maintenance_Schedule.Where(w => w.Area_ID == areaID && w.Machine_ID == machineID && w.Period_FY == dataFY && w.Month == dataMonth).FirstOrDefault();

            return Json(new { schedules = getData, headers = headerData, scheduleYearly = scheduleYear }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult MaintenanceChecklistReport()
        {
            var headerID = Int32.Parse(Request["iHeaderID"]);
            var inverval = Int32.Parse(Request["iInterval"]);
            ViewBag.ReportList = db.MTN_MachineDatabase_Maintenance_Checklist_Lines.Where(w => w.Header_ID == headerID && w.Interval == inverval).ToList();

            return View();
        }
        public ActionResult deleteMaintenanceChecklistReport()
        {
            var reportID = Int32.Parse(Request["iReportID"]);
            var imageURL = Server.MapPath("~" + Request["iImageURL"]);
            var reportList = db.MTN_MachineDatabase_Maintenance_Checklist_Report.Where(w => w.ID == reportID).First();
            if (System.IO.File.Exists(imageURL))
            {
                System.IO.File.Delete(imageURL);
            }
            db.MTN_MachineDatabase_Maintenance_Checklist_Report.Remove(reportList);
            db.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult setMaintenanceScheduleLock()
        {

            var period = Int32.Parse(Request["iPeriod"]);
            var periodFY = Request["iPeriodFY"];

            var updateLock = db.MTN_MachineDatabase_Maintenance_Schedule_Header.Where(w => w.Period_FY == periodFY).FirstOrDefault();

            if (updateLock != null)
            {
                updateLock.isLock = !updateLock.isLock;
            }
            else
            {
                var newLock = new MTN_MachineDatabase_Maintenance_Schedule_Header();
                newLock.Period_FY = periodFY;
                newLock.Year = period;
                newLock.Approval = 1;
                newLock.Approval_Sub = 0;
                newLock.isLock = true;
                db.MTN_MachineDatabase_Maintenance_Schedule_Header.Add(newLock);

                var newLockMonth = new MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header();
                for (var i = 1; i <= 12; i++)
                {
                    newLockMonth.Period_FY = periodFY;
                    newLockMonth.Year = period;
                    newLockMonth.Month = i;
                    newLockMonth.Approval = 1;
                    newLockMonth.Approval_Sub = 0;
                    db.MTN_MachineDatabase_Maintenance_Schedule_Monthly_Header.Add(newLockMonth);
                }
            }
            db.SaveChanges();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult setMaintenanceScheduleMonthlyNote()
        {

            var areaID = Int32.Parse(Request["iAreaID"]);
            var machineID = Int32.Parse(Request["iMachineID"]);
            var currYear = Int32.Parse(Request["iYear"]);
            var currMonth = Int32.Parse(Request["iMonth"]);
            var currNotes = Request["iNotes"];

            var updateNote = db.MTN_MachineDatabase_Maintenance_Schedule.Where(w => w.Area_ID == areaID && w.Machine_ID == machineID && w.Year == currYear && w.Month == currMonth).FirstOrDefault();

            if (updateNote != null)
            {
                updateNote.Note = currNotes;
            }

            db.SaveChanges();

            return Json(updateNote, JsonRequestBehavior.AllowGet);
        }
    }


}