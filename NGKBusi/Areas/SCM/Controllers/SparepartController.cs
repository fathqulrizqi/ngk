using ClosedXML.Excel;
using Irony.Ast;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using NGK_AX.Models;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Controllers;
using NGKBusi.Helpers;
using NGKBusi.Models;
using NGKBusi.SignalR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Windows.Media.Media3D;
using OfficeOpenXml;

using static System.Net.WebRequestMethods;
using SCM_D365ImporForm_ProductReceipt = NGKBusi.Areas.SCM.Models.SCM_D365ImporForm_ProductReceipt;

namespace NGKBusi.Areas.SCM.Controllers
{
    [Authorize(Roles = "AdminSparepart, UserSparepart, Administrator, WarehouseSparepart, GroupLeader, Maintenance")]
    //[Authorize(Roles = "863.09.22")]
    public class SparepartController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        SparepartConnection dbsp = new SparepartConnection();
        SparepartReturnConnection dbsr = new SparepartReturnConnection();
        NGK_AXConnection dbax = new NGK_AXConnection();
        NotificationController notif = new NotificationController();

        public string breakDecimal(decimal number)
        {
            string NominalValue = number.ToString("N0");

            return NominalValue;
        }

        private int MakeNegative(int value)
        {
            return value > 0 ? -value : value;
        }
        
        // GET: SCM/Sparepart
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            //string q = "select ITEMID,  ProductName, ItemDescription, ItemGroup, ProductCategory, ProCateName, SectionType, Section from[dbo].[V_AXItemMaster] where ItemGroup like '%Tooling%' OR ItemGroup LIke '%MachineP%'";
            //SqlConnection conn = new SqlConnection(connectionString);
            //SqlCommand cmd = new SqlCommand(q, conn);
            //var Sparepart = new List<SCM_SparepartList>();
            //using (conn)
            //{
            //    conn.Open();
            //    SqlDataReader rdr = cmd.ExecuteReader();
            //    while (rdr.Read())
            //    {
            //        var sp = new SCM_SparepartList();
            //        sp.ITEMID = rdr["ITEMID"].ToString();
            //        sp.ProductName = rdr["ProductName"].ToString();
            //        sp.ItemDescription = rdr["ItemDescription"].ToString();
            //        sp.ItemGroup = rdr["ItemGroup"].ToString();
            //        sp.ProductCategory = rdr["ProductCategory"].ToString();
            //        sp.ProductCategory = rdr["ProductCategory"].ToString();

            //        Sparepart.Add(sp);
            //    }
            //}
            string[] itemGroup = { "Tooling,MachineP" };

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = from sp in dbsp.V_SCM_Sparepart_Master_List select sp;
            //if (CurrUser.RoleName == "UserSparepart")
            //{
            //    string[] costNameSection = { "SP PD - FINAL INSPECTION", "SP PD - ASSEMBLY", "SP PD - AUTO ASSEMBLY", "SP PD - BENDING" };
            //    if (CurrUser.SectionName == "ASSEMBLY SPARKPLUG & BENDING" || CurrUser.SectionName == "PACKAGING & OEM")
            //    {
            //        spl = spl.Where(w => w.IsActive == 1 && costNameSection.Contains(w.CostName));
            //    }
            //    else
            //    {
            //        spl = spl.Where(w => w.IsActive == 1 && w.CostName == CurrUser.CostName);
            //    }
                
            //} else
            //{
                //spl = spl.Where(w => w.IsActive == 1);
            //}
            ViewBag.SparepartList = spl.ToList();
            var itemList =  dbsp.V_SCM_Sparepart_Master_List.ToList();
            var sectionList = dbsp.V_SCM_Sparepart_Master_List.OrderBy(o=>o.CostName).Select(p => p.CostName).Distinct();
            var CupboardList = dbsp.SCM_Sparepart_Cupboard_Rack.ToList();
            var alertQtyMin = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.Stock <= w.MinQty && itemGroup.Contains(w.ItemGroup)).ToList();
            var countQtyMin = alertQtyMin.Count();
            ViewBag.alertQtyMin = alertQtyMin;
            ViewBag.countQtyMin = countQtyMin;
            ViewBag.ItemList = spl.ToList();
            ViewBag.SectionList = sectionList;
            ViewBag.CupboardList = CupboardList;
            ViewBag.NavHide = true;
            ViewBag.SessionUser = CurrUser;
            ViewBag.Role = CurrUser.RoleName;
            return View();
        }
        [Authorize(Roles = "AdminSparepart, Administrator, UserSparepart, WarehouseSparepart, GroupLeader")]
        [HttpPost]
        public JsonResult GetItemMasterList(string[] SelITEMID, string ItemGroup, string[] COSTNAME, string[] ProductCategory, string[] CupBoardRack, byte Status)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var sql = from p in dbsp.V_SCM_Sparepart_Master_List select p ;
            if (SelITEMID != null)
            {
                sql = from p in sql
                      where SelITEMID.Contains(p.ITEMID)
                      select p;
            }
            if (ItemGroup != "all")
            {
                sql = from p in sql
                      where ItemGroup.Contains(p.ItemGroup)
                      select p;
            }

            if (COSTNAME != null)
            {
                sql = from p in sql
                      where COSTNAME.Contains(p.CostName)
                      select p;
            }
            else if (COSTNAME == null)
            {

                if (CurrUser.RoleName == "UserSparepart")
                {
                    string[] costNameSection = { "SP PD - FINAL INSPECTION", "SP PD - ASSEMBLY", "SP PD - AUTO ASSEMBLY", "SP PD - BENDING" };
                    if (CurrUser.SectionName == "ASSEMBLY SPARKPLUG & BENDING" || CurrUser.SectionName == "PACKAGING & OEM")
                    {
                        sql = from p in sql
                              where costNameSection.Contains(p.CostName)
                              select p;
                    }
                    else
                    {
                        sql = from p in sql
                              where p.CostName == CurrUser.CostName
                              select p;
                    }

                }
                else
                {

                }
            }
            if (ProductCategory != null)
            {
                sql = from p in sql
                      where ProductCategory.Contains(p.ProCateName)
                      select p;
            }
            byte IsKanri = 2;
            byte IsLocalPart = 2;
            byte IsFastMoving = 2;
            if (Status != 2)
            {
                sql = from p in sql
                      where p.IsActive == Status
                      select p;
            }
            if (IsLocalPart != 2)
            {
                sql = from p in sql where p.IsLocalPart == IsLocalPart select p;
            }
            if (IsFastMoving != 2)
            {
                sql = from p in sql where p.IsFastMoving == IsFastMoving select p;
            }
            if (IsKanri != 2)
            {
                sql = from p in sql where p.IsKanri == IsKanri select p;
            }

            var CountRow = sql.Count();

            List<Tbl_SCM_Sparepart_Master_List> itemList = new List<Tbl_SCM_Sparepart_Master_List>();

            foreach (var Item in sql)
            {
                var Tools = "";
                var urlContent = Url.Content("~/Files/SCM/Sparepart/Images/" + Item.Image);
                var UrlAction = Url.Action("EditMasterItem", "Sparepart", new { area = "SCM", ITEMID = Item.ITEMID });

                Tools = "<a href=\"#\" class=\"imageItem\"><img id=\"imageresource\" src=\"" + urlContent + "\" + @tbl.Image)\" height=\"70px\" data-toggle=\"modal\" data-target=\"#exampleModal\" /></a>";

                var editButton = "";

                if (User.IsInRole("AdminSparepart") || User.IsInRole("Administrator") || User.IsInRole("WarehouseSparepart"))
                {
                    editButton = "<a href=\"" + UrlAction + "\" title=\"edit item\" class=\"btn btn-warning\"><i class=\"fa fa-edit\"></i></a>";
                }
                else
                {
                    editButton = "";
                }

                var activation = "";
                if (Item.IsActive == 1)
                {
                    activation = "<span class=\"badge badge-success\">Active</span>";
                }
                else
                {
                    activation = "<span class=\"badge badge-danger\">Not Active</span>";
                }

                // jika ada filter untuk rack name
                if(CupBoardRack != null)
                {
                    // cari yang sesuai filter dari form CupBoardRack
                    if(CupBoardRack.Contains(Item.RackBoxName))
                    {
                        itemList.Add(
                        new Tbl_SCM_Sparepart_Master_List
                        {
                            ITEMID = Item.ITEMID,
                            ProductName = Item.ProductName,
                            ItemGroup = Item.ItemGroup,
                            ProCateName = Item.ProCateName,
                            CostName = Item.CostName,
                            RackBoxName = Item.RackBoxName,
                            //RackName = Item.RackName,
                            cupBoardName = Item.cupBoardName,
                            Stock = Item.Stock,
                            Section = Item.Section,
                            Image = Tools,
                            Lifetime = breakDecimal(Item.Lifetime),
                            IsActive = activation,
                            IsKanri = Item.IsKanri == 1 ? "Kanri" : "Non Kanri",
                            IsLocalPart = Item.IsLocalPart == 1 ? "Local" : "Import",
                            IsFastMoving = Item.IsFastMoving == 1 ? "Fast Moving" : "Slow Moving",
                            EditButton = editButton
                        });
                    }
                } 
                else // jika tidak ada filter cupBoardRack
                {
                    itemList.Add(
                    new Tbl_SCM_Sparepart_Master_List
                    {
                        ITEMID = Item.ITEMID,
                        ProductName = Item.ProductName,
                        ItemGroup = Item.ItemGroup,
                        ProCateName = Item.ProCateName,
                        CostName = Item.CostName,
                        RackBoxName = Item.RackBoxName,
                        //RackName = Item.RackName,
                        cupBoardName = Item.cupBoardName,
                        Stock = Item.Stock,
                        Section = Item.Section,
                        Image = Tools,
                        Lifetime = breakDecimal(Item.Lifetime),
                        IsActive = activation,
                        EditButton = editButton,
                        IsKanri = Item.IsKanri == 1 ? "Kanri" : "Non Kanri",
                        IsLocalPart = Item.IsLocalPart == 1 ? "Local" : "Import",
                        IsFastMoving = Item.IsFastMoving == 1 ? "Fast Moving" : "Slow Moving",
                    });
                }
                
            }
            var jsonResult = Json(new { rows = itemList, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;

            //var qITEMID = "";

            //// definisikan value dari form filter
            ////var ITEMID = "all";
            ////var ProductCategory = "all";
            ////var COSTNAME = "all";
            ////var CupBoardRack = "all";
            ////ITEMID = Request.Form.Get("ITEMID");
            ////var ItemGroup = Request.Form.Get("ItemGroup");
            ////var Status = Request.Form.Get("Status");
            ////ProductCategory = Request.Form.Get("ProductCategory");
            ////COSTNAME = Request.Form.Get("COSTNAME");
            ////CupBoardRack = Request.Form.Get("CupBoardRack");

            ////if (ITEMID == null)
            ////{
            ////    qITEMID = "all";
            ////}
            ////else
            ////{
            ////    qITEMID = string.Join(",", ITEMID);
            ////}

            ////if (ProductCategory == null)
            ////{
            ////    ProductCategory = "all";
            ////}
            ////if (COSTNAME == null)
            ////{
            ////    COSTNAME = "all";
            ////}
            ////if (CupBoardRack == null)
            ////{
            ////    CupBoardRack = "all";
            ////}
            ////string[] itemGroup = { "MachineP", "Tooling" };

            ////string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            ////SqlConnection con = new SqlConnection(cnnString);

            ////List<Tbl_SCM_Sparepart_Master_List> actions = new List<Tbl_SCM_Sparepart_Master_List>();

            ////SqlCommand cmd = new SqlCommand("sp_SCM_Sparepart_GetItemMaster", con);
            ////cmd.CommandType = CommandType.StoredProcedure;

            ////cmd.Parameters.AddWithValue("@qITEMID", ITEMID);
            //////cmd.Parameters.AddWithValue("@ItemGroup", ItemGroup);
            //////cmd.Parameters.AddWithValue("@Status", Status);
            //////cmd.Parameters.AddWithValue("@ProductCategory", ProductCategory);
            //////cmd.Parameters.AddWithValue("@COSTNAME", COSTNAME);
            //////cmd.Parameters.AddWithValue("@CupBoardRack", CupBoardRack);

            ////SqlDataAdapter sd = new SqlDataAdapter(cmd);
            ////DataTable dt = new DataTable();

            ////con.Open();
            ////sd.Fill(dt);
            ////con.Close();

            ////int countRow = 0;
            ////foreach (DataRow dr in dt.Rows)
            ////{
            ////    countRow++;
            ////    actions.Add(
            ////        new Tbl_SCM_Sparepart_Master_List
            ////        {
            ////            ITEMID = Convert.ToString(dr["ITEMID"]),

            ////        });
            ////}


            //// end query report 

            //string[] itemGroup = { "Tooling", "MachineP" };
            ////var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            //var spl = from sp in dbsp.V_SCM_Sparepart_Master_List select sp;

            //if (SelITEMID != null)
            //{
            //    spl.Where(w => w.ITEMID == "QIDMP-0030259");
            //}
            //spl.ToList();

            //var CountRow = spl.Count();

            //List<Tbl_SCM_Sparepart_Master_List> actions = new List<Tbl_SCM_Sparepart_Master_List>();

            //foreach (var Item in spl)
            //{
            //    var Tools = "";
            //    var urlContent = Url.Content("~/Files/SCM/Sparepart/Images/" + Item.Image);
            //    var UrlAction = Url.Action("EditMasterItem", "Sparepart", new { area = "SCM", ITEMID = Item.ITEMID });

            //    Tools = "<a href=\"#\" class=\"imageItem\"><img id=\"imageresource\" src=\"" + urlContent + "\" + @tbl.Image)\" height=\"70px\" data-toggle=\"modal\" data-target=\"#exampleModal\" /></a>";

            //    var editButton = "";

            //    if (User.IsInRole("AdminSparepart") || User.IsInRole("Administrator") || User.IsInRole("WarehouseSparepart"))
            //    {
            //        editButton = "<a href=\"" + UrlAction + "\" title=\"edit item\" class=\"btn btn-warning\"><i class=\"fa fa-edit\"></i></a>";
            //    }
            //    else
            //    {
            //        editButton = "";
            //    }

            //    var activation = "";
            //    if (Item.IsActive == 1)
            //    {
            //        activation = "<span class=\"badge badge-success\">Active</span>";
            //    }
            //    else
            //    {
            //        activation = "<span class=\"badge badge-danger\">Not Active</span>";
            //    }

            //    actions.Add(
            //        new Tbl_SCM_Sparepart_Master_List
            //        {
            //            ITEMID = Item.ITEMID,
            //            ProductName = Item.ProductName,
            //            ItemGroup = Item.ItemGroup,
            //            ProCateName = Item.ProCateName,
            //            CostName = Item.CostName,
            //            RackName = Item.RackName,
            //            cupBoardName = Item.cupBoardName,
            //            Stock = Item.Stock,
            //            Section = Item.Section,
            //            Image = Tools,
            //            Lifetime = breakDecimal(Item.Lifetime),
            //            IsActive = activation,
            //            EditButton = editButton
            //        });
            //}

            //var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            ////var jsonResult = Json(new { rows = qITEMID }, JsonRequestBehavior.AllowGet);
            //jsonResult.MaxJsonLength = int.MaxValue;
            //return jsonResult;
        }

        [HttpGet]
        [Authorize(Roles = "AdminSparepart, Administrator, WarehouseSparepart")]
        public ActionResult EditMasterItem(string ITEMID)
        {
            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ITEMID == ITEMID).FirstOrDefault();
            ViewBag.SparepartDetail = spl;

            var rack = dbsp.SCM_Sparepart_Rack.Where(w => w.IsDelete == 0).ToList();
            ViewBag.Rack = rack;

            var cupboardList = dbsp.SCM_Sparepart_Cupboard.ToList();
            ViewBag.CupboardList = cupboardList;

            var rackList = dbsp.SCM_Sparepart_Cupboard_Rack.ToList();
            ViewBag.RackList = rackList;

            var boxList = dbsp.SCM_Sparepart_Cupboard_RackBox.ToList();
            ViewBag.BoxList = boxList;

            return View();
        }

        [Authorize(Roles = "AdminSparepart, Administrator, WarehouseSparepart")]
        [HttpPost]
        public ActionResult EditMasterItem(HttpPostedFileBase ImageFile, String RackId, string RackSequence, string ITEMID, Int32 MinQty, Int32 MaxQty, Int32 Lifetime, byte IsKanri, byte IsFastMoving, byte IsLocalPart)
        {
            HttpPostedFileBase file = Request.Files["ImageFile"];

            Guid guid = Guid.NewGuid();
            string newfileName = guid.ToString();

            string fileextention = Path.GetExtension(file.FileName);

            string fileName = ITEMID + fileextention;

            //string uploadpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images", fileName);

            //var stream = new FileStream(uploadpath, FileMode.Create);

            //file.CopyToAsync(stream);

            //return Json(new { status = ImageFile });
            if (ImageFile != null)
            {

                var query = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == ITEMID);
                var spl = query.FirstOrDefault();
                var queryCount = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == ITEMID);
                int count = queryCount.Count();

                string oldpath = Request.MapPath("~/Files/SCM/Sparepart/Images/" + spl.Image);

                if (System.IO.File.Exists(oldpath))
                {
                    System.IO.File.Delete(oldpath);
                }

                string pic = System.IO.Path.GetFileName(file.FileName);

                string path = Path.Combine(Server.MapPath("~/Files/SCM/Sparepart/Images"), fileName);
                file.SaveAs(path);

                spl.RackId = RackId;
                spl.RackSequence = RackSequence;
                spl.Image = fileName;
                spl.MinQty = MinQty;
                spl.MaxQty = MaxQty;
                spl.Lifetime = Lifetime;
                spl.IsFastMoving = IsFastMoving;
                spl.IsKanri = IsKanri;
                spl.IsLocalPart = IsLocalPart;

                if (count == 0)
                {
                    spl.ITEMID = ITEMID;
                    dbsp.SCM_Sparepart_Master_List.Add(spl);
                }

                var update = dbsp.SaveChanges();
                if (update > 0)
                {
                    return Json(new { status = "1", path = path, filename = pic, msg = "Update Success" });
                }
                else
                {
                    return Json(new { status = "0", path = path, filename = pic, msg = "update failed" });
                }
            }
            else
            {
                var query = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == ITEMID);
                var spl = query.FirstOrDefault();
                var queryCount = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == ITEMID);
                int count = queryCount.Count();

                spl.RackId = RackId;
                spl.RackSequence = RackSequence;
                spl.MinQty = MinQty;
                spl.MaxQty = MaxQty;
                spl.Lifetime = Lifetime;
                spl.IsFastMoving = IsFastMoving;
                spl.IsKanri = IsKanri;
                spl.IsLocalPart = IsLocalPart;

                if (count == 0)
                {
                    spl.ITEMID = ITEMID;
                    dbsp.SCM_Sparepart_Master_List.Add(spl);
                }

                var update = dbsp.SaveChanges();
                if (update == 1)
                {
                    return Json(new { status = "1", msg = "Update Success" });
                }
                else
                {
                    return Json(new { status = "0", msg = "update Failed" });
                }
            }
        }

        [HttpPost]
        public ActionResult PrintQR(string ITEMIDArr, int Width, int Height)
        {

            String[] result = ITEMIDArr.Split(',');
            var query = dbsp.V_SCM_Sparepart_Master_List.Where(w => result.Contains(w.ITEMID)).ToList();
            ViewBag.ITEMID = result;
            ViewBag.ItemList = query;
            ViewBag.width = Width;
            ViewBag.height = Height;
            return View();

        }

        [HttpPost]
        public ActionResult UpdateActivation(string ITEMID, string action)
        {
            var item = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == ITEMID).FirstOrDefault();

            if (action == "Deactivate")
            {
                item.IsActive = 0;
            } else
            {
                int CountItem = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == ITEMID).Count();
                if (CountItem > 0 )
                {
                    item.IsActive = 1;
                } else
                {
                    SCM_Sparepart_Master_List sparepart = new SCM_Sparepart_Master_List();

                    sparepart.ITEMID = ITEMID;
                    sparepart.Lifetime = 0;
                    sparepart.MaxQty = 0;
                    sparepart.MinQty = 0;
                    sparepart.Image = "default.png";
                    sparepart.IsActive = 1;

                    dbsp.SCM_Sparepart_Master_List.Add(sparepart);
                }
                
            }
            int i = dbsp.SaveChanges();
            if (i > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = "Update Success"
                });
            } else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Update Failed"
                });
            }
           

        }

        [HttpGet]
        public ActionResult PrintQR()
        {
            return View();

        }
        public class ItemDetailDTO
        {
            public string RequestNo { get; set; }
            public DateTime Create_Time { get; set; }
            public byte Status { get; set; }
            public string Remark { get; set; }
            public string MaintenanceType { get; set; }
            public string CostName { get; set; }
            public string UserRequest { get; set; }
            public DateTime? CloseTimeDueDate { get; set; }

            // Other field you may need from the Product entity
        }
        public ActionResult SendNotification()
        {
            // Ambil user ID dari Claims
            var userId = ((ClaimsIdentity)User.Identity).GetUserId();

            // Kirimkan notifikasi
            List<string> userList = new List<string>();
            var userData = dbsp.SCM_Sparepart_User_Management.Where(w => w.Role == "Admin" || w.Role == "Developer").ToList();
            foreach (var item in userData)
            {
                userList.Add(item.userNIK);
            }
            //List<string> users = new List<string> { "863.09.22", "" };
            notif.PushNotification("test Notification", userList);

            return Content("Notifikasi dikirim ke user dengan ID: " + userId);
        }
        [HttpPost]
        public ActionResult ItemListRequest(string RequestNo, string Status)
        {
            var spl = dbsp.V_SCM_Sparepart_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();
            var CountRow = dbsp.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo);

            List<V_SCM_Sparepart_ItemList> actions = new List<V_SCM_Sparepart_ItemList>();

            var countNotReady = dbsp.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo && w.IsReady == 0);

            foreach (var Item in spl)
            {
                var id = Item.Id;
                var Tools = "";
                if (Status == "1")
                {
                    Tools = "<a href=\"#EditQuantityModal\" data-toggle=\"modal\" title=\"Edit\" id=\"EditQtyItem\" data-itemid=\"" + Item.ITEMID + "\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning EditQtyItem\"><i class=\"fa fa-pencil\"></i></a> <a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";
                }
                else
                {
                    if (Item.IsReady == 0 && (User.IsInRole("Administrator") || User.IsInRole("WarehouseSparepart")))
                    {
                        Tools = "<a href=\"#\" title=\"ready\" id=\"procItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-primary procItem\"><i class=\"fa fa-check\"></i></a> <a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";
                    }
                    else
                    {
                        if (User.IsInRole("Administrator") || User.IsInRole("WarehouseSparepart"))
                        {
                            Tools = "<a href=\"#EditQuantityRealizationModal\" data-toggle=\"modal\" title=\"Decrease Qty\" id=\"editQty\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning procItem\"><i class=\"fa fa-edit btnEdit\"></i></a>";
                        }

                        //if (Status == "3" || Status == "4")
                        //{
                        //    Tools = "<a href=\"#\" onclick=\"reprintLabel(" + Item.Id + ")\" class=\"btn-sm btn-primary procItem\"><i class=\"fas fa-print btnEdit\"></i> </a> ";
                        //}
                    }
                }

                actions.Add(
                    new V_SCM_Sparepart_ItemList
                    {
                        Id = id,
                        ITEMID = Item.ITEMID,
                        ProductName = Item.ProductName,
                        Quantity = Item.Quantity,
                        Qty_Realization = Item.Qty_Realization,
                        RackName = Item.RackName,
                        RackSequence = Item.RackSequence,
                        RackBoxName = Item.RackBoxName,
                        Tools = Tools
                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
                notReady = countNotReady,
                status = 1
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        // GET: SCM/Sparepart/Details/5
        public ActionResult DetailRequest(string RequestNo)
        {
            //var data = db.SCM_Sparepart_Request_Header.FirstOrDefault(x => x.RequestNo == RequestNo);
            //ViewBag.header = data;

            //List<SCM_Sparepart_Request_Header> sCM_Sparepart_Request_Headers = db.SCM_Sparepart_Request_Header.ToList();
            //List<V_AXItemMaster> v_AXItemMasters = db.V_AXItemMaster.ToList();

            var data = (from c in dbsp.SCM_Sparepart_Request_Header
                        join cn in dbsp.V_Users_Active on c.UserRequest equals cn.NIK
                        where (c.RequestNo == RequestNo)
                        select new ItemDetailDTO { RequestNo = c.RequestNo, Create_Time = c.Create_Time, Status = c.Status, Remark = c.Remark, MaintenanceType = c.MaintenanceType, UserRequest = cn.Name, CloseTimeDueDate = c.CloseTimeDueDate, CostName = c.CostName }).FirstOrDefault();
            ViewBag.header = data;
            ViewBag.RequestNo = RequestNo;
            var countNotReady = dbsp.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo && w.IsReady == 0);
            ViewBag.NotReady = countNotReady;
            ViewBag.NavHide = true;
            //var header = new List<SCM_Sparepart_Request_Header>();
            //foreach ( var tdata in results)
            //{
            //    var sp = new SCM_Sparepart_Request_Header();
            //    sp.RequestNo = tdata.RequestNo;

            //    header.Add(sp);
            //}


            //return Json(new
            //{
            //    status = RequestNo,
            //    msg = results,
            //    spl =  spl
            //}, JsonRequestBehavior.AllowGet);

            return View();
        }

        // GET: SCM/Sparepart/Create
        [Authorize(Roles = "AdminSparepart, Administrator, UserSparepart, GroupLeader, Maintenance")]
        public ActionResult CreateRequest()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var section = db.Users_Section_AX.ToList();

            ViewBag.CurrUser = dbsp.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = dbsp.V_Users_Active.Where(w => w.Status == "Permanent").ToList();
            ViewBag.sectionCreator = CurrUser.CostName;
            ViewBag.section = section;
            ViewBag.NavHide = true;
            ViewBag.Role = CurrUser.RoleName;

            // get costname list for user production (foreman, SPV)
            if (CurrUser.RoleName == "UserSparepart")
            {
                var userCost = dbsp.SCM_Sparepart_User_CostName.Where(w => w.NIK == CurrUser.NIK && w.IsDelete == 0).ToList();
                ViewBag.userCost = userCost;
            }

            string[] itemGroup = { "" };
            string[] costNameSection = { "SP PD - FINAL INSPECTION", "SP PD - ASSEMBLY", "SP PD - AUTO ASSEMBLY", "SP PD - BENDING" };
            var spl = from sp in dbsp.V_SCM_Sparepart_Master_List select sp;
            //if (CurrUser.RoleName == "UserSparepart" && (CurrUser.SectionName != "ASSEMBLY SPARKPLUG & BENDING" || CurrUser.SectionName != "PACKAGING & OEM"))
            //{
            //    if (CurrUser.SectionName == "ASSEMBLY SPARKPLUG & BENDING" || CurrUser.SectionName == "PACKAGING & OEM")
            //    {
            //        spl = spl.Where(w => costNameSection.Contains(w.CostName) && w.IsActive == 1);
            //    } else
            //    {
            //        spl = spl.Where(w => w.CostName == CurrUser.CostName && w.IsActive == 1);
            //    }
                
            //}
            //else
            //{
                spl = spl.Where(w => w.IsActive == 1);
            //}

            ViewBag.SparepartList = spl.ToList() ;

            return View();
        }

        // POST: SCM/Sparepart/Create
        [Authorize(Roles = "AdminSparepart, Administrator,UserSparepart,GroupLeader")]
        [HttpPost]
        public ActionResult CreateRequest(SCM_Sparepart_Request_Header smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = dbsp.V_Users_Active.Where(w => w.NIK == currUser).First();

            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            var spl = dbsp.SCM_Sparepart_Request_Temp.Where(w => w.userRequest == CurrUser.NIK).ToList();
            var stockMin = new ArrayList();
            int s = 0;
            foreach (var dt in spl)
            {
                var itemid = dt.ITEMID;
                var QStock = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ITEMID == itemid).FirstOrDefault();
                int Stock = QStock.Stock;

                if (Stock < dt.quantity)
                {
                    s++;
                    stockMin.Add(dt.ITEMID);
                }
            }

            if (s == 0)
            {
                SqlCommand cmd = new SqlCommand("sp_SCM_Sparepart_Request_Create", con);
                cmd.CommandType = CommandType.StoredProcedure;

                //cek item temp
                var count = dbsp.SCM_Sparepart_Request_Temp.Count(me => me.userRequest == CurrUser.NIK);
                var remark = "";
                var CostName = "";
                var MaintenanceType = "";

                if (count > 0)
                {
                    if (smodel.Remark != null)
                    {
                        remark = smodel.Remark;
                    }
                    else
                    {
                        remark = "";
                    }
                    if (smodel.CostName != null)
                    {
                        CostName = smodel.CostName;
                    } else
                    {
                        CostName = "default";
                    }
                    if (smodel.MaintenanceType != null)
                    {
                        MaintenanceType = smodel.MaintenanceType;
                    }
                    else
                    {
                        MaintenanceType = "";
                    }

                    cmd.Parameters.AddWithValue("@userRequest", CurrUser.NIK);
                    cmd.Parameters.AddWithValue("@Remark", remark);
                    cmd.Parameters.AddWithValue("@CostName", CostName);
                    cmd.Parameters.AddWithValue("@MaintenanceType", MaintenanceType);

                    SqlParameter outputParam = new SqlParameter("@reqNo", SqlDbType.VarChar, 50)
                    {
                        Direction = ParameterDirection.Output
                    };

                    cmd.Parameters.Add(outputParam);

                    con.Open();
                    //int i = cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQuery();
                    //con.Close();

                    // Mengambil nilai output dari parameter @OrderId
                    var reqNo = outputParam.Value.ToString().Trim();

                    if (!string.IsNullOrEmpty(reqNo.ToString()))
                    {
                        List<string> userList = new List<string>();
                        var userData = dbsp.SCM_Sparepart_User_Management.Where(w => w.Role == "Admin" || w.Role == "Developer").ToList();
                        foreach(var item in userData)
                        {
                            userList.Add(item.userNIK);
                        }
                        //List<string> users = new List<string> { "863.09.22", "" };
                        notif.PushNotification(reqNo, userList);

                        return Json(new
                        {
                            status = 1,
                            msg = "Request Send",
                            minstock = spl,
                            itemidminus = stockMin,
                            reqNo = reqNo
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            status = 0,
                            msg = "failed",
                            minstock = spl,
                            itemidminus = stockMin
                        });
                    }
                }
                else
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "Empty Item Request" + stockMin,
                        minstock = s,
                        itemidminus = stockMin
                    });
                }
            }
            else
            {
                return Json(new
                {
                    status = 2,
                    msg = "Plese Check Stock for this Item " + stockMin,
                    minstock = s,
                    itemidminus = stockMin
                });
            }
        }

        [HttpPost]
        public ActionResult AddRequestList(SCM_Sparepart_Request_Temp smodel)
        {
            try
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
                var CurrUser = dbsp.V_Users_Active.Where(w => w.NIK == currUser).First();

                var data = dbsp.SCM_Sparepart_Request_Temp.FirstOrDefault(x => x.ITEMID == smodel.ITEMID && x.userRequest == CurrUser.NIK);
                if (data != null)
                {
                    return Json(new
                    {
                        status = "2",
                        msg = "Item Exist, please check your item list"
                    });
                }
                else
                {
                    dbsp.SCM_Sparepart_Request_Temp.Add(smodel);
                    var ins = dbsp.SaveChanges();
                    if (ins == 1)
                    {
                        return Json(new
                        {
                            status = "1",
                            msg = "Item Inserted"
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            status = "0",
                            msg = "failed"
                        });
                    }
                }
            }
            catch
            {
                return View();
            }
        }

        [HttpGet]
        public ActionResult RequestList()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            List<SCM_Sparepart_Request_Temp> sCM_Sparepart_Request_Temps = dbsp.SCM_Sparepart_Request_Temp.ToList();
            List<V_SCM_Sparepart_Master_List> V_SCM_Sparepart_Master_List = dbsp.V_SCM_Sparepart_Master_List.ToList();

            var item = (
                from ai in sCM_Sparepart_Request_Temps
                join al in V_SCM_Sparepart_Master_List on ai.ITEMID equals al.ITEMID
                where (ai.userRequest == CurrUser.NIK)
                select new SCM_Sparepart_Request_Temp_Views
                {
                    ITEMID = ai.ITEMID,
                    ProductName = al.ProductName,
                    Quantity = ai.quantity,
                    UserRequest = ai.userRequest
                }).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp_Views.SqlQuery("SELECT a.ITEMID, a.userRequest , a.quantity, b.ProductName from SCM_Sparepart_Request_Temp a LEFT JOIN V_AXItemMaster b ON b.ITEMID = a.ITEMID WHERE a.userRequest = {0}", CurrUser.NIK).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp.Where(w => w.userRequest == CurrUser.NIK).ToList();

            return Json(item, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult EditQuantityItemOpen(int DetailId)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            List<SCM_Sparepart_Request_Detail> sCM_Sparepart_Request_Detail = dbsp.SCM_Sparepart_Request_Detail.ToList();
            List<V_SCM_Sparepart_Master_List> v_SCM_Sparepart_Master_List = dbsp.V_SCM_Sparepart_Master_List.ToList();

            var item = (
                from ai in sCM_Sparepart_Request_Detail
                join al in v_SCM_Sparepart_Master_List on ai.ITEMID equals al.ITEMID
                where (ai.Id == DetailId)
                select new SCM_Sparepart_Request_Temp_Views
                {
                    ITEMID = ai.ITEMID,
                    ProductName = al.ProductName,
                    Quantity = ai.Quantity,
                    Stock = al.Stock,
                    Id = ai.Id
                }).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp_Views.SqlQuery("SELECT a.ITEMID, a.userRequest , a.quantity, b.ProductName from SCM_Sparepart_Request_Temp a LEFT JOIN V_AXItemMaster b ON b.ITEMID = a.ITEMID WHERE a.userRequest = {0}", CurrUser.NIK).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp.Where(w => w.userRequest == CurrUser.NIK).ToList();

            return Json(item, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult EditQuantityItemOpen(SCM_Sparepart_Request_Detail smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();


            var data = dbsp.SCM_Sparepart_Request_Detail.FirstOrDefault(x => x.Id == smodel.Id);
            data.Quantity = smodel.Quantity;

            var update = dbsp.SaveChanges();
            if (update == 1)
            {
                return Json(new
                {
                    status = "1",
                    msg = "Update Quntity Success"
                });
            }
            else
            {
                return Json(new
                {
                    status = "0",
                    msg = "failed"
                });
            }

        }
        [HttpGet]
        public ActionResult EditQuantityItem(string itemid)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            List<SCM_Sparepart_Request_Temp> sCM_Sparepart_Request_Temps = dbsp.SCM_Sparepart_Request_Temp.ToList();
            List<V_SCM_Sparepart_Master_List> v_SCM_Sparepart_Master_List = dbsp.V_SCM_Sparepart_Master_List.ToList();

            var item = (
                from ai in sCM_Sparepart_Request_Temps
                join al in v_SCM_Sparepart_Master_List on ai.ITEMID equals al.ITEMID
                where (ai.userRequest == CurrUser.NIK && ai.ITEMID == itemid)
                select new SCM_Sparepart_Request_Temp_Views
                {
                    ITEMID = ai.ITEMID,
                    ProductName = al.ProductName,
                    Quantity = ai.quantity,
                    UserRequest = ai.userRequest,
                    Stock = al.Stock
                }).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp_Views.SqlQuery("SELECT a.ITEMID, a.userRequest , a.quantity, b.ProductName from SCM_Sparepart_Request_Temp a LEFT JOIN V_AXItemMaster b ON b.ITEMID = a.ITEMID WHERE a.userRequest = {0}", CurrUser.NIK).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp.Where(w => w.userRequest == CurrUser.NIK).ToList();

            return Json(item, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult EditQuantityItem(SCM_Sparepart_Request_Temp smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();


            var data = dbsp.SCM_Sparepart_Request_Temp.FirstOrDefault(x => x.ITEMID == smodel.ITEMID && x.userRequest == CurrUser.NIK);
            data.quantity = smodel.quantity;

            var update = dbsp.SaveChanges();
            if (update == 1)
            {
                return Json(new
                {
                    status = "1",
                    msg = "Update Quntity Success"
                });
            }
            else
            {
                return Json(new
                {
                    status = "0",
                    msg = "failed"
                });
            }

        }

        [HttpGet]
        public ActionResult EditQuantityRealization(int DetailId)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            List<SCM_Sparepart_Request_Detail> sCM_Sparepart_Request_Details = dbsp.SCM_Sparepart_Request_Detail.ToList();
            List<V_SCM_Sparepart_Master_List> v_SCM_Sparepart_Master_List = dbsp.V_SCM_Sparepart_Master_List.ToList();

            var item = (
                from ai in sCM_Sparepart_Request_Details
                join al in v_SCM_Sparepart_Master_List on ai.ITEMID equals al.ITEMID
                where (ai.Id == DetailId)
                select new V_SCM_Sparepart_ItemList
                {
                    Id = ai.Id,
                    ITEMID = ai.ITEMID,
                    ProductName = al.ProductName,
                    Quantity = ai.Quantity,
                    Qty_Realization = ai.Qty_Realization

                }).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp_Views.SqlQuery("SELECT a.ITEMID, a.userRequest , a.quantity, b.ProductName from SCM_Sparepart_Request_Temp a LEFT JOIN V_AXItemMaster b ON b.ITEMID = a.ITEMID WHERE a.userRequest = {0}", CurrUser.NIK).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp.Where(w => w.userRequest == CurrUser.NIK).ToList();

            return Json(item, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult EditQuantityRealization(SCM_Sparepart_Request_Detail item)
        {
            var data = dbsp.SCM_Sparepart_Request_Detail.FirstOrDefault(x => x.Id == item.Id);
            data.Qty_Realization = item.Qty_Realization;

            var update = dbsp.SaveChanges();
            if (update == 1)
            {
                return Json(new
                {
                    status = "1",
                    msg = "Update Success"
                });
            }
            else
            {
                return Json(new
                {
                    status = "0",
                    msg = "Item Not Update"
                });
            }
        }
        [HttpPost]
        public ActionResult RemoveList(string itemid)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            try
            {
                SCM_Sparepart_Request_Temp sCM_Sparepart_Request_Temp = dbsp.SCM_Sparepart_Request_Temp.Find(itemid, CurrUser.NIK);
                dbsp.SCM_Sparepart_Request_Temp.Remove(sCM_Sparepart_Request_Temp);
                var del = dbsp.SaveChanges();
                if (del == 1)
                {
                    return Json(new
                    {
                        status = "1",
                        msg = "Item Deleted"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "0",
                        msg = "failed"
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "2",
                    msg = "failed",
                    itemid = itemid,
                    userNIK = CurrUser.NIK
                });
            }

        }

        // GET: SCM/Sparepart/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SCM/Sparepart/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult RequestData(DateTime dateFrom, DateTime dateTo, Byte status)
        {

            //DateTime parseStartDate = DateTime.Parse(result[0]);
            //

            //DateTime parseEndDate = DateTime.Parse(result[0]);
            //

            var fromDate = Convert.ToDateTime(dateFrom);
            var startDate = fromDate.ToString("yyyy-MM-dd");

            var toDate = Convert.ToDateTime(dateTo);
            var endDate = toDate.ToString("yyyy-MM-dd");

            //var spl = db.V_AXItemMaster.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var spl = from sp in dbsp.V_SCM_Sparepart_Request select sp;
            if (status == 0)
            {
                if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                {
                    spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                }
                else
                {
                    spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.userRequest == currUser);
                }

            }
            else
            {
                if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                {
                    spl = spl.Where(w => w.Status == status && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                }
                else
                {
                    spl = spl.Where(w => w.Status == status && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.userRequest == currUser);
                }
            }
            var result = spl.ToList();


            var CheckedAction = "";

            //ViewBag.dater = spl;
            //return Json(new
            //{
            //    status = status,
            //    from = startDate,
            //    to = endDate,
            //    result = spl
            //});

            Session["startDate"] = startDate;
            Session["endDate"] = endDate;
            Session["status"] = Convert.ToString(status);

            ViewBag.RequestList = result;
            ViewBag.status = status;
            ViewBag.dateStart = startDate;
            ViewBag.dateEnd = endDate;
            ViewBag.autopick = "true";
            ViewBag.CheckedAction = CheckedAction;
            return View();
        }
        public ActionResult RequestData()
        {
            string dateFrom = Session["startDate"] as string;
            string dateTo = Session["endDate"] as string;
            string statusFilter = Session["status"] as string;

            var spl = from sp in dbsp.V_SCM_Sparepart_Request select sp;
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            Console.WriteLine(Session["startDate"]);

            if (!string.IsNullOrEmpty(dateFrom))
            {
               
                var fromDate = Convert.ToDateTime(dateFrom);
                var startDate = fromDate.ToString("yyyy-MM-dd");

                var toDate = Convert.ToDateTime(dateTo);
                var endDate = toDate.ToString("yyyy-MM-dd");

                int status = Convert.ToInt32(statusFilter);
                if (status == 0)
                {
                    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                    }
                    else
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.userRequest == currUser);
                    }

                }
                else
                {
                    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                    {
                        spl = spl.Where(w => w.Status == status && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                    }
                    else
                    {
                        spl = spl.Where(w => w.Status == status && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.userRequest == currUser);
                    }
                }


            }

            else
            {
                if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                {
                    Console.WriteLine('1');
                    spl = spl.Where(w => w.Status == 1).OrderByDescending(o => o.Create_Time);
                }
                else
                {
                    Console.WriteLine('2');
                    spl = spl.Where(w => w.Status == 1 && w.userRequest == currUser).OrderByDescending(o => o.Create_Time);
                }

            }

            var result = spl.ToList();

            ViewBag.RequestList = result;
            ViewBag.status = 1;
            ViewBag.dateStart = null;
            ViewBag.dateEnd = null;
            ViewBag.autopick = "false";
            ViewBag.StartDate = dateFrom;
            ViewBag.endDate = dateTo;
            ViewBag.statusFilter = statusFilter;

            return View();
        }

        [HttpPost]
        public ActionResult GetRequestData(string dateFrom, string dateTo, int status)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            TempData["statusFilter"] = status;

            //string dateFrom = "2024-11-01 12:00:00";
            //string dateTo = "2024-11-14 12:00:00";
            //string FilterStatus = "0";

            //string dateFrom = Session["dateFrom"] as string;
            //string dateTo = Session["dateTo"] as string;
            //string FilterStatus = Session["status"] as string;
            Debug.WriteLine(dateFrom);
            var sql = from sp in dbsp.V_SCM_Sparepart_Request select sp;
            int sessionStatus;
            if (!string.IsNullOrEmpty(dateFrom))
            {
                TempData["statusFilter"] = status;
                DateTime fromDate = Convert.ToDateTime(dateFrom);
                var StartDate = fromDate.ToString("yyyy-mm-dd");
                DateTime toDate = Convert.ToDateTime(dateTo);
                var EndDate = toDate.ToString("yyyy-mm-dd");
                //int status = Convert.ToInt32(FilterStatus);
                Debug.WriteLine("a");
                if (status == 0)
                {
                    Debug.WriteLine("aa");
                    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                    {
                        Debug.WriteLine("aaa");
                        sql = sql.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                    }
                    else
                    {
                        Debug.WriteLine("aab");
                        sql = sql.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.userRequest == currUser);
                    }
                    
                } else
                {
                    Debug.WriteLine("ab");
                    if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                    {
                        Debug.WriteLine("aba");
                        sql = sql.Where(w => w.Status == status && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                    }
                    else
                    {
                        Debug.WriteLine("abb");
                        sql = sql.Where(w => w.Status == status && (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.userRequest == currUser);
                    }
                }
                sessionStatus = status;
            }
            else
            {
                TempData["statusFilter"] = 0;
                Debug.WriteLine("b");
                if (CurrUser.RoleName == "AdminSparepart" || CurrUser.RoleName == "Administrator" || CurrUser.RoleName == "WarehouseSparepart")
                {
                    Debug.WriteLine("ba");
                    sql = sql.Where(w => w.Status == 1).OrderByDescending(o => o.Create_Time);
                    sessionStatus = 1;
                }
                else
                {

                    Debug.WriteLine("bb");
                    sql = sql.Where(w => w.Status == 1 && w.userRequest == currUser).OrderByDescending(o => o.Create_Time);
                    sessionStatus = 1;
                }
            }
            var result = sql.OrderByDescending(o=>o.Create_Time).ToList();

            List<Tbl_V_SCM_Sparepart_Request> reqList = new List<Tbl_V_SCM_Sparepart_Request>();
            foreach (var item in result)
            {
                var actDetail = Url.Action("DetailRequest", "Sparepart", new { area = "SCM", RequestNo = item.RequestNo });
                var urlDetail = "<a href="+ actDetail+" class=\"btn btn-info btn-sm\" data-toggle=\"tooltip\" data-placement=\"right\" title=\"view Detail\"><i class=\"fa fa-eye\"></i> </a>";

                var action = Url.Action("ReturRequest", "Sparepart", new { area = "SCM", RequestNo = item.RequestNo });
                var urlAction = "<a href=" + action + " class=\"btn btn-warning btn-sm\" data-toggle=\"tooltip\" data-placement=\"right\" title=\"Retur Item\"><i class=\"fa fa-undo\"></i> </a>";

                string btnAction;
                if (item.Status == 4 && item.IsReturn == 0)
                {
                    btnAction = urlDetail + " " + urlAction;
                } else
                {
                    btnAction = urlDetail;
                }

                string sta = "";
                string badge = "";
                //string countdown = "";
                if (item.Status == 1)
                {
                    sta = "open";
                    badge = "primary";
                    //countdown = "";
                }
                else if (item.Status == 2)
                {
                    sta = "Preparing";
                    badge = "warning";
                    //countdown = "";
                }
                else if (item.Status == 3)
                {
                    sta = "Ready";
                    badge = "info";
                    //countdown = "";
                    //countdown = Html.Raw("Please Close your request before <strong>") + tbl.CloseTimeDueDate.ToString("dd - MMMM - yyyy") + Html.Raw("</strong>");
                }
                else if (item.Status == 10)
                {
                    sta = "Cancelled";
                    badge = "dark";
                    //countdown = "";
                }
                else
                {
                    sta = "Close";
                    badge = "secondary";
                    //countdown = "";
                }

                reqList.Add(
                    new Tbl_V_SCM_Sparepart_Request
                    {
                        RequestNo = item.RequestNo,
                        RequestBy = item.Name,
                        Remark = item.Remark,
                        Section = item.CostName,
                        RequestTime = item.Create_Time.ToString("dd MMM yyyy"),
                        Status = "<h5 ><span class=\"badge badge-"+ badge +"\"> " + sta + "</span></h5>",
                        Action = btnAction,

                    });
            }
            //List<SCM_Sparepart_Request_Temp> sCM_Sparepart_Request_Temps = dbsp.SCM_Sparepart_Request_Temp.ToList();
            //List<V_SCM_Sparepart_Master_List> V_SCM_Sparepart_Master_List = dbsp.V_SCM_Sparepart_Master_List.ToList();

            //var item = (
            //    from ai in sCM_Sparepart_Request_Temps
            //    join al in V_SCM_Sparepart_Master_List on ai.ITEMID equals al.ITEMID
            //    where (ai.userRequest == CurrUser.NIK)
            //    select new SCM_Sparepart_Request_Temp_Views
            //    {
            //        ITEMID = ai.ITEMID,
            //        ProductName = al.ProductName,
            //        Quantity = ai.quantity,
            //        UserRequest = ai.userRequest
            //    }).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp_Views.SqlQuery("SELECT a.ITEMID, a.userRequest , a.quantity, b.ProductName from SCM_Sparepart_Request_Temp a LEFT JOIN V_AXItemMaster b ON b.ITEMID = a.ITEMID WHERE a.userRequest = {0}", CurrUser.NIK).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp.Where(w => w.userRequest == CurrUser.NIK).ToList();
            Debug.WriteLine(reqList);
            return Json(new { reqList = reqList, status = sessionStatus, role = CurrUser.RoleName }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteItemRequest(int Id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            try
            {
                var data = dbsp.SCM_Sparepart_Request_Detail.Where(w => w.Id == Id).FirstOrDefault();
                dbsp.SCM_Sparepart_Request_Detail.Remove(data);
                var del = dbsp.SaveChanges();
                if (del == 1)
                {
                    return Json(new
                    {
                        status = "1",
                        msg = "Item Deleted"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "0",
                        msg = "failed"
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "2",
                    msg = "failed"
                });
            }

        }
        [HttpPost]
        public ActionResult PrepareRequest(string[] requestNo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            foreach (string ITEMID in requestNo)
            {
                SCM_Sparepart_Request_Header item = new SCM_Sparepart_Request_Header();
                item.RequestNo = ITEMID;
                dbsp.SCM_Sparepart_Request_Header.Attach(item);
                item.Status = 2; //Change a filed's value
                item.PrepareTime = DateTime.Now;
            }

            //return Json(requestNo, JsonRequestBehavior.AllowGet);
            var save = dbsp.SaveChanges();

            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Item prepared",
                    displayData = "prepared"
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed Prepare Item",
                    displayData = "Opened"
                });
            }
        }

        public ActionResult PrinterSetting()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var printerList = dbsp.SCM_Sparepart_Printer.ToList();
            var qrCodeList = dbsp.SCM_Sparepart_QR_Result.ToList();

            
            ViewBag.Printers = printerList;
            ViewBag.QrCodesResult = qrCodeList;

            return View();
        }


        [HttpPost]
        public JsonResult CreatePrinter(SCM_Sparepart_Printer printer)
        {
            try
            {
                if (printer == null)
                {
                    return Json(new { success = false, message = "Invalid data received." });
                }
                dbsp.SCM_Sparepart_Printer.Add(printer);
                dbsp.SaveChanges();
                return Json(new { success = true, message = "Printer added successfully!" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditPrinter(SCM_Sparepart_Printer printer)
        {
            try
            {
                if (printer == null)
                {
                    return Json(new { success = false, message = "Invalid data received." });
                }
                dbsp.Entry(printer).State = EntityState.Modified;

                dbsp.SaveChanges();
                return Json(new { success = true, message = "Printer updated successfully!" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeletePrinter(int id)
        {
            try
            {
               var printerToDelete = dbsp.SCM_Sparepart_Printer.Find(id);

                if (printerToDelete == null)
                {
                    return Json(new { success = false, message = "Printer not found." });
                }
                dbsp.SCM_Sparepart_Printer.Remove(printerToDelete);

                dbsp.SaveChanges();

                return Json(new { success = true, message = "Printer deleted successfully!" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


        [HttpPost]
        public JsonResult ChangePrinter(int id)
        {
            try {
                var allPrinters = dbsp.SCM_Sparepart_Printer.ToList();

                if (!allPrinters.Any())
                {
                    return Json(new { success = false, message = "Tidak ada data printer di database." });
                }

                foreach (var printer in allPrinters)
                {
                    printer.printer_status = (printer.id == id);
                }

                dbsp.SaveChanges();

                return Json(new { success = true, message = "Changed Printer successfully!" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CheckingPrinter(CheckingRequest request) {
          
            if (!string.IsNullOrWhiteSpace(request.shared_printer))
            {
                var printerPath = request.shared_printer;
                try
                {
                    // Query WMI tetap sama, hanya menggunakan variabel baru
                    string query = $"SELECT * FROM Win32_Printer WHERE Name = '{printerPath.Replace(@"\", @"\\")}'";
                    
                    using (var searcher = new ManagementObjectSearcher(query))
                    {
                        var printers = searcher.Get();
                        System.Diagnostics.Debug.WriteLine("ini dia 2: " + printers);
                        if (printers.Count == 0)
                        {
                            return Json(new { success = false, message = $"Shared printer '{printerPath}' tidak ditemukan atau tidak dapat diakses." });
                        }

                        foreach (ManagementObject printer in printers)
                        {
                            bool isOffline = Convert.ToBoolean(printer["WorkOffline"]);
                            int status = Convert.ToInt32(printer["PrinterStatus"]);

                            string deviceId = printer["DeviceID"]?.ToString();
                            string status2 = printer["Status"]?.ToString();
                            uint printerState = (uint)printer["PrinterState"];


                            System.Diagnostics.Debug.WriteLine($"  -> DeviceID: {deviceId}, Status: {status}, State: {printerState}");

                            if (isOffline || status == 7) 
                            {
                                return Json(new { success = false, message = $"Printer '{printerPath}' sedang offline." });
                            }
                        }
                        return Json(new { success = true, message = $"Printer '{printerPath}' online dan siap digunakan." });

                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Gagal memeriksa shared printer. Alasan: {ex.Message}"});
                }
            }
            else
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ip))
                {
                    return Json(new { success = false, message = "Request body must include a valid 'IP' and 'Port'." });
                }

                try
                {
                    using (var client = new TcpClient())
                    {
                        var result = client.BeginConnect(request.ip, request.port, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                        if (!success)
                        {
                            throw new Exception("Connection timed out after 3 seconds.");
                        }

                        client.EndConnect(result);
                        return Json(new { success = true, message = $"Printer di {request.ip}:{request.port} online." });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Printer offline or unreachable. Reason: {ex.Message}" });
                }
            }
        }

        private static readonly Random random = new Random();
        public bool ExecutePrintMultiple(List<int> listId)
        {
            // Pastikan ada ID yang diberikan untuk dicetak
            if (listId == null || !listId.Any())
            {
                System.Diagnostics.Debug.WriteLine("ExecutePrintMultiple failed: No IDs provided to print.");
                return false;
            }

            try
            {
                var currUserNik = ((ClaimsIdentity)User.Identity).GetUserId();
                var currUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUserNik);
                if (currUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("ExecutePrintMultiple failed: Current user not found.");
                    return false;
                }

                var printer = dbsp.SCM_Sparepart_Printer.FirstOrDefault(w => w.printer_status == true);
                if (printer == null)
                {
                    System.Diagnostics.Debug.WriteLine("ExecutePrintMultiple failed: No active printer configured.");
                    return false;
                }

                // Ambil semua data label dalam satu query untuk efisiensi
                var qrResults = dbsp.SCM_Sparepart_QR_Result.Where(r => listId.Contains(r.id)).ToList();

                // Pastikan semua ID yang diminta berhasil ditemukan
                if (qrResults.Count != listId.Count)
                {
                    System.Diagnostics.Debug.WriteLine("ExecutePrintMultiple failed: One or more label IDs not found in the database.");
                    return false;
                }

                var zplBuilder = new System.Text.StringBuilder();
                var printTime = DateTime.Now;
                string twoWordsName = string.Join(" ", currUser.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));


                foreach (var qrResult in qrResults)
                {
                    // Generate ZPL untuk setiap label dan gabungkan
                    string singleZpl = GenerateZpl(qrResult.item_number, qrResult.item_name, qrResult.QRcode, twoWordsName, printTime);
                    zplBuilder.Append(singleZpl);
                }

                string combinedZplData = zplBuilder.ToString();


                System.Diagnostics.Debug.WriteLine("disini contoh datanya");
                System.Diagnostics.Debug.WriteLine(combinedZplData);

                if (string.IsNullOrWhiteSpace(combinedZplData))
                {
                    System.Diagnostics.Debug.WriteLine("ExecutePrintMultiple failed: ZPL data is empty after generation.");
                    return false;
                }

                bool isPrintSuccess = false;

                // 3. Kirim satu perintah cetak gabungan ke printer
                if (!string.IsNullOrWhiteSpace(printer.shared_printer))
                {
                    if (!CheckPrinterSharedConnection(printer.shared_printer)) return false;

                    isPrintSuccess = SendingFileShared(combinedZplData, printer.shared_printer);
                }
                else if (!string.IsNullOrWhiteSpace(printer.printer_ip) && printer.port > 0)
                {
                    if (!CheckPrinterConnection(printer.printer_ip, printer.port)) return false;
                    if (printer.port == 515)
                    {
                        string queueName = "Ip1";
                        isPrintSuccess = SendingFileViaLpr(combinedZplData, printer.printer_ip, queueName);
                    }
                    else
                    {
                        isPrintSuccess = SendingFileToIp(combinedZplData, printer.printer_ip, printer.port);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ExecutePrintMultiple failed: Invalid printer configuration.");
                    return false;
                }
                if (!isPrintSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("ExecutePrintMultiple failed: Sending data to printer failed.");
                    return false;
                }
                foreach (var label in qrResults)
                {
                    label.updated_at = printTime; 
                }
                dbsp.SaveChanges(); 
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecutePrintMultiple encountered an exception: {ex}");
                return false;
            }
        }

        [HttpPost]
        public JsonResult SparepartPrintQr(string ids)
        {
            var list_id = ids.Split(',').Select(int.Parse).ToList();
            if (list_id == null || !list_id.Any())
            {
                return Json(new { success = false, message = "No IDs were provided." });
            }

            try
            {
                var currUserNik = ((ClaimsIdentity)User.Identity).GetUserId();
                var currUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUserNik);
                if (currUser == null)
                {
                    return Json(new { success = false, message = "Current user not found." });
                }

                var requestDetailsList = dbsp.V_SCM_Sparepart_Request_Detail
                                             .Where(w => list_id.Contains(w.Id))
                                             .ToList();

                if (!requestDetailsList.Any())
                {
                    return Json(new { success = false, message = "None of the specified Request Detail IDs were found." });
                }

                var existingQrs = dbsp.SCM_Sparepart_QR_Result
                                        .Where(qr => list_id.Contains(qr.from_id))
                                        .ToList();

                var newQrList = new List<SCM_Sparepart_QR_Result>();
                var idsToPrint = new List<int>();
                DateTime now = DateTime.Now;
                string yearPart = now.ToString("yy");
                string dayPart = now.ToString("dd");
                string monthPart;
                switch (now.Month)
                {
                    case 10:
                        monthPart = "A";
                        break;
                    case 11:
                        monthPart = "B";
                        break;
                    case 12:
                        monthPart = "C";
                        break;
                    default:
                        monthPart = now.Month.ToString();
                        break;
                }

                string dateCode = yearPart+dayPart+monthPart;

                foreach (var detail in requestDetailsList)
                {
                    var qrsForThisDetail = existingQrs.Where(qr => qr.from_id == detail.Id).ToList();
                    int existingCount = qrsForThisDetail.Count;
                    idsToPrint.AddRange(qrsForThisDetail.Select(qr => qr.id));

                    int requiredQty = detail.Qty_Realization;
                    int qtyToGenerate = requiredQty - existingCount;

                    if (qtyToGenerate > 0)
                    {
                        string qrPrefix = $"{detail.ITEMID}-{dateCode}-";

                        var lastQrForPrefix = dbsp.SCM_Sparepart_QR_Result
                            .Where(qr => qr.QRcode.StartsWith(qrPrefix))
                            .OrderByDescending(qr => qr.id)
                            .FirstOrDefault();

                        int lastSequence = 0;
                        if (lastQrForPrefix != null)
                        {
                            string lastCode = lastQrForPrefix.QRcode;
                            if (lastCode.Length > qrPrefix.Length)
                            {
                                string hexSuffix = lastCode.Substring(qrPrefix.Length);
                                lastSequence = int.Parse(hexSuffix, System.Globalization.NumberStyles.HexNumber);
                            }
                        }

                        for (int i = 0; i < qtyToGenerate; i++)
                        {
                            int newSequence = lastSequence + i + 1;
                            string uniqueCode = newSequence.ToString("X3");

                            var qrResult = new SCM_Sparepart_QR_Result
                            {
                                item_number = detail.ITEMID,
                                item_name = detail.ProductName,
                                QRcode = $"{qrPrefix}{uniqueCode}", 
                                from_id = detail.Id,
                                created_at = DateTime.Now,
                                updated_at = DateTime.Now
                            };

                            newQrList.Add(qrResult);
                        }
                    }
                }

                int newItemsCount = 0;
                if (newQrList.Any())
                {
                    newItemsCount = newQrList.Count;
                    dbsp.SCM_Sparepart_QR_Result.AddRange(newQrList);
                    dbsp.SaveChanges();

                    idsToPrint.AddRange(newQrList.Select(item => item.id));
                }

                if (idsToPrint.Any())
                {
                    var distinctIdsToPrint = idsToPrint.Distinct().ToList();
                    ExecutePrintMultiple(distinctIdsToPrint);

                    string message = $"{newItemsCount} new label(s) were generated. A total of {distinctIdsToPrint.Count} label(s) will be processed.";
                    return Json(new { success = true, message = message, print = distinctIdsToPrint });
                }
                else
                {
                    return Json(new { success = true, message = "All required labels already exist. No new labels were generated." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        private bool SendingFileViaLpr(string zplData, string ipAddress, string queueName)
        {
            try
            {
                byte[] data = System.Text.Encoding.ASCII.GetBytes(zplData);
                int jobId = new Random().Next(1, 1000);
                string jobName = $"job{jobId}.tmp";

                using (var client = new System.Net.Sockets.TcpClient())
                {
                    client.Connect(ipAddress, 515);
                    using (var stream = client.GetStream())
                    {
                        byte[] command = System.Text.Encoding.ASCII.GetBytes($"\x02{queueName}\n");
                        stream.Write(command, 0, command.Length);
                        if (stream.ReadByte() != 0) 
                        {
                            System.Diagnostics.Debug.WriteLine("LPR Error: Printer did not acknowledge job request.");
                            return false;
                        }

                        string controlFile = $"Hlocalhost\nP{jobName}\n";
                        byte[] controlFileBytes = System.Text.Encoding.ASCII.GetBytes(controlFile);
                        command = System.Text.Encoding.ASCII.GetBytes($"\x02{controlFileBytes.Length} cfA{jobId}localhost\n");
                        stream.Write(command, 0, command.Length);
                        if (stream.ReadByte() != 0)
                        {
                            System.Diagnostics.Debug.WriteLine("LPR Error: Printer did not acknowledge control file request.");
                            return false;
                        }
                        stream.Write(controlFileBytes, 0, controlFileBytes.Length);
                        stream.WriteByte(0); 
                        if (stream.ReadByte() != 0)
                        {
                            System.Diagnostics.Debug.WriteLine("LPR Error: Printer did not acknowledge control file data.");
                            return false;
                        }


                        command = System.Text.Encoding.ASCII.GetBytes($"\x03{data.Length} dfA{jobId}localhost\n");
                        stream.Write(command, 0, command.Length);
                        if (stream.ReadByte() != 0)
                        {
                            System.Diagnostics.Debug.WriteLine("LPR Error: Printer did not acknowledge data file request.");
                            return false;
                        }
                        stream.Write(data, 0, data.Length);
                        stream.WriteByte(0);
                        if (stream.ReadByte() != 0)
                        {
                            System.Diagnostics.Debug.WriteLine("LPR Error: Printer did not acknowledge data file content.");
                            return false;
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine("Successfully sent data via LPR protocol.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LPR printing failed: {ex}");
                return false;
            }
        }

        [HttpPost]
        public JsonResult RePrint(int id)
        {
            try
            {
                var currUserNik = ((ClaimsIdentity)User.Identity).GetUserId();
                var currUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUserNik);

                if (currUser == null)
                {
                    return Json(new { success = false, message = "Current user not found." });
                }
                var qrResult = dbsp.SCM_Sparepart_QR_Result.Find(id);
                if (qrResult == null)
                {
                    return Json(new { success = false, message = "Label data not found." });
                }

                var printer = dbsp.SCM_Sparepart_Printer.FirstOrDefault(w => w.printer_status == true);
                if (printer == null)
                {
                    return Json(new { success = false, message = "No active printer configured." });
                }

                string fullName = currUser.Name;
                string[] words = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string twoWordsName = string.Join(" ", words.Take(2));

                string zplData = GenerateZpl(qrResult.item_number, qrResult.item_name, qrResult.QRcode, twoWordsName, DateTime.Now);
                bool isPrintSuccess = false;

                if (!string.IsNullOrWhiteSpace(printer.shared_printer))
                {
                    if (!CheckPrinterSharedConnection(printer.shared_printer))
                    {
                        return Json(new { success = false, message = "Shared printer is not reachable." });
                    }
                    isPrintSuccess = SendingFileShared(zplData, printer.shared_printer);
                }
                else if (!string.IsNullOrWhiteSpace(printer.printer_ip) && printer.port > 0)
                {
                    if (!CheckPrinterConnection(printer.printer_ip, printer.port))
                    {
                        return Json(new { success = false, message = "IP printer is not reachable." });
                    }
                    if (printer.port == 515)
                    {
                        string queueName = "Ip1";
                        isPrintSuccess = SendingFileViaLpr(zplData, printer.printer_ip, queueName);
                    }
                    else
                    {
                        isPrintSuccess = SendingFileToIp(zplData, printer.printer_ip, printer.port);
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Printer configuration is invalid (no shared path or IP/Port)." });
                }

                if (!isPrintSuccess)
                {
                    return Json(new { success = false, message = "Failed to send data to printer. Please check printer status." });
                }

                qrResult.updated_at = DateTime.Now;
                dbsp.SaveChanges();

                return Json(new { success = true, message = "Label successfully reprinted." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


        private string GenerateZpl(string itemNo, string itemName, string qrCode, string printedBy, DateTime printedDate)
        {
            string formattedDate = printedDate.ToString("dd/MM/yyyy HH:mm:ss");
            return $@"^^XA
              ^CI28
              ^PW800
              ^FO45,30^A0N,50,50^FDSparepart Card^FS
              ^FO435,30^A0N,25,25^FDPrinted By : {printedBy}^FS
              ^FO435,60^A0N,25,25^FDDate         : {formattedDate}^FS
              ^FO35,90^GB700,3,3^FS
              ^FO165,110^A0N,33,33^FD{qrCode}^FS
              ^FO35,160^BQN,2,7^FDQA,{qrCode}^FS
              ^FO265,160^A0N,25,25^FDItem No:^FS
              ^FO265,190^A0N,25,25^FD{itemNo}^FS
              ^FO265,222^A0N,25,25^FDItem Pn:^FS
              ^FO265,255^A0N,25,25^FD{itemName}^FS
              ^FO265,290^A0N,25,25^FD0^FS
              ^FO285,285^GB30,30,3^FS
              ^FO325,290^A0N,25,25^FD1^FS
              ^FO345,285^GB30,30,3^FS
              ^FO385,290^A0N,25,25^FD2^FS
              ^FO405,285^GB30,30,3^FS
              ^FO445,290^A0N,25,25^FD3^FS
              ^FO465,285^GB30,30,3^FS
              ^FO505,290^A0N,25,25^FD4^FS
              ^FO525,285^GB30,30,3^FS
              ^XZ";
        }

        private bool SendingFileShared(string zplData, string sharedPrinterPath)
        {
            try
            {
                RawPrinterHelper.SendStringToPrinter(sharedPrinterPath, zplData);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to print to Shared Path {sharedPrinterPath}. Error: {ex.Message}");
                return false;
            }
        }

        private bool SendingFileToIp(string zplData, string ipAddress, int port)
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    if (!client.ConnectAsync(ipAddress, port).Wait(3000))
                    {
                      
                        return false;
                    }

                    using (var stream = client.GetStream())
                    {
                        byte[] dataToSend = System.Text.Encoding.UTF8.GetBytes(zplData);
                        stream.Write(dataToSend, 0, dataToSend.Length);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to print to IP {ipAddress}:{port}. Error: {ex.Message}");
                return false;
            }
        }

        private bool CheckPrinterSharedConnection(string shared_printer)
        {
            try {
                var printerPath = shared_printer;
                    string query = $"SELECT * FROM Win32_Printer WHERE Name = '{printerPath.Replace(@"\", @"\\")}'";
                    using (var searcher = new ManagementObjectSearcher(query))
                    {
                        var printers = searcher.Get();
                        if (printers.Count == 0)
                        {
                            return false;
                        }

                        foreach (ManagementObject printer in printers)
                        {
                            bool isOffline = Convert.ToBoolean(printer["WorkOffline"]);
                            int status = Convert.ToInt32(printer["PrinterStatus"]);

                            if (isOffline || status == 7)
                            {
                                return false;
                            }
                        }
                        return true;

                    }  
            } catch
            {
                return false;
            }
        }
        private bool CheckPrinterConnection(string ip, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(ip, port);
                    return true; 
                }
            }
            catch
            {
                return false; 
            }
        }




        public ActionResult ReadyItem(int Id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var DetailItem = dbsp.SCM_Sparepart_Request_Detail.Where(w => w.Id == Id).FirstOrDefault();

            DetailItem.IsReady = 1;
            DetailItem.Qty_Realization = DetailItem.Quantity;

            var save = dbsp.SaveChanges();

            if (save > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = "Item Ready",
                    displayData = "Ready"
                });
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Failed Ready Item",
                    displayData = "prepared"
                });
            }
            //try
            //{
            //    SCM_Sparepart_Request_Temp sCM_Sparepart_Request_Temp = db.SCM_Sparepart_Request_Temp.Find(itemid, CurrUser.NIK);
            //    db.SCM_Sparepart_Request_Temp.Remove(sCM_Sparepart_Request_Temp);
            //    var del = db.SaveChanges();
            //    if (del == 1)
            //    {
            //        return Json(new
            //        {
            //            status = "1",
            //            msg = "Item Deleted"
            //        });
            //    }
            //    else
            //    {
            //        return Json(new
            //        {
            //            status = "0",
            //            msg = "failed"
            //        });
            //    }
            //}
            //catch
            //{
            //    return Json(new
            //    {
            //        status = "2",
            //        msg = "failed",
            //        itemid = itemid,
            //        userNIK = CurrUser.NIK
            //    });
            //}

        }

        public ActionResult PostScanItem(string ITEMID, string RequestNo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var QDetailItem = dbsp.SCM_Sparepart_Request_Detail.Where(w => w.ITEMID == ITEMID && w.RequestNo == RequestNo);
            int countItem = QDetailItem.Count();

            if (countItem > 0)
            {
                var DetailItem = QDetailItem.FirstOrDefault();
                DetailItem.IsReady = 1;
                DetailItem.Qty_Realization = DetailItem.Quantity;
                var save = dbsp.SaveChanges();
                if (save > 0)
                {
                    return Json(new
                    {
                        status = 1,
                        msg = "Item Ready"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "<div class='alert alert-warning mt-1 p-1' role='alert'> Item <strong>" + ITEMID + "</strong> Already Scan</div>"
                    });
                }
            }
            else
            {
                return Json(new
                {
                    status = 2,
                    msg = "<div class='alert alert-danger mt-1 p-1' role='alert'> Item <strong>" + ITEMID + "</strong> Not Found</div>"
                });
            }




            //try
            //{
            //    SCM_Sparepart_Request_Temp sCM_Sparepart_Request_Temp = db.SCM_Sparepart_Request_Temp.Find(itemid, CurrUser.NIK);
            //    db.SCM_Sparepart_Request_Temp.Remove(sCM_Sparepart_Request_Temp);
            //    var del = db.SaveChanges();
            //    if (del == 1)
            //    {
            //        return Json(new
            //        {
            //            status = "1",
            //            msg = "Item Deleted"
            //        });
            //    }
            //    else
            //    {
            //        return Json(new
            //        {
            //            status = "0",
            //            msg = "failed"
            //        });
            //    }
            //}
            //catch
            //{
            //    return Json(new
            //    {
            //        status = "2",
            //        msg = "failed",
            //        itemid = itemid,
            //        userNIK = CurrUser.NIK
            //    });
            //}

        }
        [HttpPost]
        public ActionResult ConfirmReadyRequest(string requestNo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var EmailAddress = CurrUser.Email;

            // get stock for each item //
            int noStock = 0;
            var StockMinus = new ArrayList();
            var RequestDtl = dbsp.SCM_Sparepart_Request_Detail.Where(w => w.RequestNo == requestNo).ToList();
            foreach (var dtl in RequestDtl)
            {
                var DtlItemID = dtl.ITEMID;
                var Qstock = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ITEMID == DtlItemID).FirstOrDefault();
                if (Qstock.Stock < dtl.Qty_Realization)
                {
                    noStock++;
                    StockMinus.Add(dtl.ITEMID);
                }
            }
            //-- get stock for each item --//

            //if (noStock == 0)
            //{
                var Request = dbsp.SCM_Sparepart_Request_Header.Where(w => w.RequestNo == requestNo).FirstOrDefault();
                DateTime today = DateTime.Now;
                Request.Status = 3;
                Request.ReadyTime = today;
                Request.ReadyBy = CurrUser.NIK;
                Request.CloseTimeDueDate = today.AddDays(3);

                var save = dbsp.SaveChanges();

                if (save > 0)
                {
                    if (EmailAddress != null)
                    {
                        string FilePath = Path.Combine(Server.MapPath("~/Emails/SCM/Sparepart/"), "Test.html");
                        StreamReader str = new StreamReader(FilePath);
                        string MailText = str.ReadToEnd();
                        str.Close();

                        var act = Url.Action("DetailRequest", "Sparepart", new { area = "SCM", RequestNo = requestNo });
                        //Repalce [newusername] = signup user name   
                        MailText = MailText.Replace("##UserName##", requestNo);
                        MailText = MailText.Replace("##Dept##", "Information Technology");
                        MailText = MailText.Replace("##Url##", act);

                        var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "SPAREPART NOTIFICATION");
                        var receiverEmail = new MailAddress(EmailAddress, "Receiver");
                        var password = "100%NGKbusi!";
                        var sub = "[no-reply] Sparepart Apps";
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
                        using (var mess = new MailMessage(senderEmail, receiverEmail)
                        {
                            Subject = sub,
                            Body = body,
                            IsBodyHtml = true
                        })
                        {
                            smtp.Send(mess);
                        }
                    }
                    return Json(new
                    {
                        status = '1',
                        msg = "Request ready to pickup"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = '0',
                        msg = "Failed to Ready pickup request"
                    });
                }
            //}
            //else
            //{
            //    return Json(new
            //    {
            //        status = '0',
            //        msg = "Not Enough Stock For ITEM  " + StockMinus
            //    });
            //}
        }

        [HttpPost]
        public ActionResult CloseRequest(string[] requestNo)
        {
            //string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            //SqlConnection con = new SqlConnection(cnnString);

            //SqlCommand cmd = new SqlCommand("sp_SCM_Sparepart_Request_Close", con);
            //cmd.CommandType = CommandType.StoredProcedure;


            //cmd.Parameters.AddWithValue("@requestNo", requestNo);

            //con.Open();
            ////SqlDataReader i = cmd.ExecuteReader();
            //con.Close();
            //var join = string.Join(",", requestNo);

            //return Json(new
            //{
            //    status = requestNo,
            //    msg = join
            //});


            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var updateDataHeader = dbsp.SCM_Sparepart_Request_Header.Where(w => requestNo.Contains(w.RequestNo)).ToList();
            foreach (var data in updateDataHeader)
            {
                data.Status = 4;
                data.CloseTime = DateTime.UtcNow.Date;
            }

            var updateHeader = dbsp.SaveChanges();

            var getDataTotal = dbsp.SCM_Sparepart_Request_Detail.Where(w => requestNo.Contains(w.RequestNo)).GroupBy(g => g.ITEMID).Select(s => new { QtyRealization = s.Sum(i => i.Qty_Realization), ITEMID = s.Key }).ToList();

            //db.SaveChanges();
            foreach (var ReqId in getDataTotal)
            {
                var item = dbsp.SCM_Sparepart_Master_List.Where(w => w.ITEMID == ReqId.ITEMID).FirstOrDefault();
                dbsp.SCM_Sparepart_Master_List.Attach(item);
                item.Quantity = (item.Quantity - ReqId.QtyRealization);
                //Change a filed's value
            }

            var updateStock = dbsp.SaveChanges();

            if (updateHeader > 0 && updateStock > 0)
            {
                return Json(new { status = 1, msg = "Request Closed", displayData = "Closed" });
            }
            else if (updateHeader > 0)
            {
                return Json(new { status = 0, msg = "Request Close, But Stock Failed to Update", displayData = "Ready" });
            }
            else
            {
                return Json(new { status = 0, msg = "Failed Close Request", displayData = "Ready" });
            }

            //if (save > 0)
            //{

            //    return Json(new
            //    {
            //        status = '1',
            //        msg = "Request Closed"
            //    });
            //}
            //else
            //{
            //    return Json(new
            //    {
            //        status = '0',
            //        msg = "Failed Closed Request"
            //    });
            //}
        }

        public ActionResult CancelRequest(string requestNo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var updateDataHeader = dbsp.SCM_Sparepart_Request_Header.Where(w => w.RequestNo == requestNo).ToList();
            foreach (var data in updateDataHeader)
            {
                data.Status = 10;
                data.CancelTime = DateTime.UtcNow.Date;
                data.IsCancel = 1;
                data.CancelBy = CurrUser.NIK;
            }

            var updateHeader = dbsp.SaveChanges();

            var getDetail = dbsp.SCM_Sparepart_Request_Detail.Where(w => w.RequestNo == requestNo).ToList();

            foreach (var item in getDetail)
            {

                item.IsCancel = 1;
                //Change a filed's value
            }

            var updateDetail = dbsp.SaveChanges();

            if (updateHeader > 0 && updateDetail > 0)
            {
                return Json(new { status = 1, msg = "Request Cancel" });
            }
            else
            {
                return Json(new { status = 0, msg = "Failed Cancel Request" });
            }
        }
        [HttpGet]
        public ActionResult ReturRequest(string RequestNo)
        {
            var data = (from c in dbsp.SCM_Sparepart_Request_Header
                        join cn in dbsp.V_Users_Active on c.UserRequest equals cn.NIK
                        where (c.RequestNo == RequestNo)
                        select new ItemDetailDTO { RequestNo = c.RequestNo, Create_Time = c.Create_Time, Status = c.Status, Remark = c.Remark, UserRequest = cn.Name, CloseTimeDueDate = c.CloseTimeDueDate, CostName = c.CostName }).FirstOrDefault();
            ViewBag.header = data;
            ViewBag.RequestNo = RequestNo;
            var countNotReady = dbsp.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo && w.IsReady == 0);
            ViewBag.NotReady = countNotReady;
            //var header = new List<SCM_Sparepart_Request_Header>();
            //foreach ( var tdata in results)
            //{
            //    var sp = new SCM_Sparepart_Request_Header();
            //    sp.RequestNo = tdata.RequestNo;

            //    header.Add(sp);
            //}


            //return Json(new
            //{
            //    status = RequestNo,
            //    msg = results,
            //    spl =  spl
            //}, JsonRequestBehavior.AllowGet);

            return View();
        }
        public ActionResult ItemListRetur(string RequestNo)
        {
            var spl = dbsp.V_SCM_Sparepart_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();
            var CountRow = dbsp.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo);

            List<V_SCM_Sparepart_ItemList> actions = new List<V_SCM_Sparepart_ItemList>();

            var countNotReady = dbsp.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo && w.IsReady == 0);

            foreach (var Item in spl)
            {
                var Tools = "";

                Tools = "<a href=\"#EditQuantityModal\" data-toggle=\"modal\" title=\"Edit\" id=\"EditQtyItem\" data-itemid=\"" + Item.ITEMID + "\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning EditQtyItem\"><i class=\"fa fa-pencil\"></i></a> <a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";

                if (Item.IsReady == 0)
                {
                    Tools = "<a href=\"#\" title=\"ready\" id=\"procItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-primary procItem\"><i class=\"fa fa-check\"></i></a>";
                }
                else
                {
                    Tools = "<a href=\"#EditQuantityRealizationModal\" data-toggle=\"modal\" title=\"Insert Qty Retur\" id=\"editQty\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning procItem\"><i class=\"fa fa-edit btnEdit\"></i></a>";
                }

                actions.Add(
                    new V_SCM_Sparepart_ItemList
                    {
                        ITEMID = Item.ITEMID,
                        ProductName = Item.ProductName,
                        Quantity = Item.Quantity,
                        Qty_Realization = Item.Qty_Realization,
                        Tools = Tools,
                        Qty_Retur = "<input type=\"hidden\" class=\"form-control w-50 p-3\" name=\"ITEMID[]\" value=\"" + Item.ITEMID + "\"> <input type=\"number\" onkeyup=\"if (value > " + Item.Qty_Realization + ") value=0;\" min =\"0\" max=\"" + Item.Qty_Realization + "\" class=\"form-control w-50 p-3\" name=\"QtyRetur[" + Item.ITEMID + "]\" value=\"" + Item.Qty_Retur + "\">",
                        ReturNotes = "<input type =\"text\"  class=\"form-control\" name=\"ReturnNotes[" + Item.ITEMID + "]\" value=\"" + Item.ReturNotes + "\">"
                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
                notReady = countNotReady
            }, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        public ActionResult SaveReturRequest(string RequestNo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var spl = dbsp.SCM_Sparepart_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();

            List<SCM_Sparepart_Return_Detail> ReturList = new List<SCM_Sparepart_Return_Detail>();
            var returQty = new ArrayList();
            int insert = 0;
            foreach (var item in spl)
            {
                //var sp = new SCM_Sparepart_Request_Header();
                //sp.RequestNo = tdata.RequestNo;

                ////    header.Add(sp);

                int QtyRetur = int.Parse(Request["QTYRetur[" + item.ITEMID + "]"]);
                if (QtyRetur > 0)
                {
                    dbsr.SCM_Sparepart_Return_Detail.Add(new SCM_Sparepart_Return_Detail
                    {
                        RequestNo = RequestNo,
                        Quantity = int.Parse(Request["QTYRetur[" + item.ITEMID + "]"]),
                        ReturnNotes = Request["ReturnNotes[" + item.ITEMID + "]"],
                        ITEMID = item.ITEMID,
                        Create_By = CurrUser.NIK,
                        Create_Time = DateTime.Now,
                        Status = "open"
                    }); 

                    int i = dbsr.SaveChanges();
                    if (i > 0)
                    {
                        insert++;
                    }
                }

            }


            //string ReturnNo =  DateTime.Now.ToString("yyyyMMddHHmmss");

            //SCM_Sparepart_Return_Header header = new SCM_Sparepart_Return_Header();
            //header.ReturnNo = ReturnNo;
            //header.RequestNo = RequestNo;
            //header.CostName = CurrUser.CostName;
            //header.DivisionName = CurrUser.DivisionName;
            //header.SectionName = CurrUser.SectionName;
            //header.SubSectionName = CurrUser.SubSectionName;
            //header.Status = 1;
            //header.UserReturn = CurrUser.NIK;
            //header.Create_Time = DateTime.Now;

            //var headerList = dbsr.SCM_Sparepart_Return_Header.Add(header);
            //var saveHeader = dbsr.SaveChanges();

            //if (saveHeader >= 0)
            //{
            //    var detailRequest = dbsp.V_SCM_Sparepart_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();

            //    SCM_Sparepart_Return_Detail detail = new SCM_Sparepart_Return_Detail();
            //    foreach (var item in detailRequest)
            //    {
            //        detail.ITEMID = item.ITEMID;
            //        detail.Quantity = Qty_Retur[0];
            //        detail.Qty_Realization = 
            //    }
            //}
            if (insert > 0)
            {
                //update request header
                var query = dbsp.SCM_Sparepart_Request_Header.Where(w => w.RequestNo == RequestNo).FirstOrDefault();
                DateTime today = DateTime.Now;
                query.IsReturn = 1;

                var save = dbsp.SaveChanges();

                return Json(new
                {
                    status = 1,
                    rows = returQty,
                    //totalNotFiltered = Qty_Retur,
                    //total = ReturNotes
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    rows = returQty,
                    //totalNotFiltered = Qty_Retur,
                    //total = ReturNotes
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult CreateRetur()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            ViewBag.SparepartList = spl;

            return View();
        }
        [Authorize(Roles = "AdminSparepart, Administrator, WarehouseSparepart,GroupLeader")]
        public ActionResult Report()
        {
            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling");
            var query = spl.GroupBy(row => new { row.ProCateName, row.ProductCategory }).Select(group => new { ProCateName = group.Key.ProCateName, ProductCategory = group.Key.ProductCategory }).ToList();
            var section = db.Users_Section_AX.ToList();

            List<V_SCM_Sparepart_Master_List> par = new List<V_SCM_Sparepart_Master_List>();

            foreach (var data in query)
            {
                par.Add(new V_SCM_Sparepart_Master_List
                {
                    ProCateName = data.ProCateName,
                    ProductCategory = data.ProductCategory
                });
            }

            ViewBag.Section = section;
            ViewBag.ItemGroup = par;
            ViewBag.RackList = dbsp.SCM_Sparepart_Rack.Where(w => w.IsDelete == 0).ToList();
            ViewBag.NavHide = true;
            return View();
        }
        [Authorize(Roles = "Administrator, WarehouseSparepart")]
        public ActionResult ReportD365()
        {
            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling");
            var query = spl.GroupBy(row => new { row.ProCateName, row.ProductCategory }).Select(group => new { ProCateName = group.Key.ProCateName, ProductCategory = group.Key.ProductCategory }).ToList();
            var section = db.Users_Section_AX.ToList();

            List<V_SCM_Sparepart_Master_List> par = new List<V_SCM_Sparepart_Master_List>();

            foreach (var data in query)
            {
                par.Add(new V_SCM_Sparepart_Master_List
                {
                    ProCateName = data.ProCateName,
                    ProductCategory = data.ProductCategory
                });
            }

            ViewBag.Section = section;
            ViewBag.ItemGroup = par;
            ViewBag.RackList = dbsp.SCM_Sparepart_Rack.Where(w => w.IsDelete == 0).ToList();
            ViewBag.NavHide = false;
            return View();
        }

        [Authorize(Roles = "AdminSparepart, Administrator, WarehouseSparepart, GroupLeader")]
        [HttpPost]
        public ActionResult GenerateReport(SCM_Sparepart_Report smodel)
        {
            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            List<SCM_Sparepart_Report> k3list = new List<SCM_Sparepart_Report>();

            SqlCommand cmd = new SqlCommand("sp_SCM_Sparepart_Generate_Report", con);
            cmd.CommandType = CommandType.StoredProcedure;

            var reportTitle = "";
            var StrDateStart = Convert.ToDateTime(smodel.dateFrom).ToString("dd MMM yyyy");
            var StrDateTo = Convert.ToDateTime(smodel.dateTo).ToString("dd MMM yyyy");

            // date from 
            if (smodel.dateFrom.HasValue)
            {

                cmd.Parameters.AddWithValue("@dateFrom", smodel.dateFrom);
            }
            else
            {
                DateTime todayDate = DateTime.UtcNow.Date.AddDays(-7);
                cmd.Parameters.AddWithValue("@dateFrom", todayDate);
            }
            // date to
            if (smodel.dateTo.HasValue)
            {
                cmd.Parameters.AddWithValue("@dateTo", smodel.dateTo);
            }
            else
            {
                DateTime date2 = DateTime.UtcNow.Date;
                cmd.Parameters.AddWithValue("@dateTo", date2);
            }

            cmd.Parameters.AddWithValue("@ItemGroup", smodel.ItemGroup);
            cmd.Parameters.AddWithValue("@ProductCategory", smodel.ProductCategory);
            cmd.Parameters.AddWithValue("@COSTNAME", smodel.COSTNAME);
            cmd.Parameters.AddWithValue("@Status", smodel.Status);
            cmd.Parameters.AddWithValue("@MaintenanceType", smodel.MaintenanceType);

            SqlDataAdapter sd = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            con.Open();
            sd.Fill(dt);
            con.Close();

            if (smodel.dateFrom.HasValue && smodel.dateTo.HasValue)
            {
                reportTitle = "report periode " + StrDateStart + " - " + StrDateTo;

            }
            else
            {
                reportTitle = "report periode last 30 days";
            }


            foreach (DataRow dr in dt.Rows)
            {
                var ct = Convert.ToDateTime(dr["Create_Time"]);
                var createTime = ct.ToString("yyyy-MM-dd HH:mm");
                // get status
                var status = "";
                if (Convert.ToString(dr["Status"]) == "1")
                {
                    status = "Open";
                } else if (Convert.ToString(dr["Status"]) == "2")
                {
                    status = "Prepare";
                } else if (Convert.ToString(dr["Status"]) == "3")
                {
                    status = "Ready";
                } else if (Convert.ToString(dr["Status"]) == "4")
                {
                    status = "Close";
                }
                else if (Convert.ToString(dr["Status"]) == "10")
                {
                    status = "Cancel";
                }

                string getSeconds = "";

                if (Convert.ToInt32(dr["TotalSeconds"]) > 0)
                {
                    getSeconds = Helpers.Convertion.ConvertSecondsToDHMS(Convert.ToInt32(dr["TotalSeconds"]));
                }
                else
                {
                    getSeconds = "item not ready";
                }

                k3list.Add(
                    new SCM_Sparepart_Report
                    {
                        ITEMID = Convert.ToString(dr["ITEMID"]),
                        Quantity = Convert.ToInt32(dr["Qty_Realization"]),
                        ProductName = Convert.ToString(dr["ProductName"]),
                        ItemGroup = Convert.ToString(dr["ItemGroup"]),
                        ProCateName = Convert.ToString(dr["ProCateName"]),
                        Create_Date = Convert.ToDateTime(createTime).ToString("M/d/yyyy"),
                        PrepareDate = Convert.ToDateTime(createTime).ToString("M/d/yyyy"),
                        Create_Time = Convert.ToDateTime(createTime).ToString("HH:mm"),
                        COSTNAME = Convert.ToString(dr["COSTNAME"]),
                        Status = status,
                        NameUserRequest = Convert.ToString(dr["Name"]),
                        ReadyByName = Convert.ToString(dr["ReadyByName"]),
                        MaintenanceType = Convert.ToString(dr["MaintenanceType"]),
                        TotalSeconds = Convert.ToInt32(dr["TotalSeconds"]),
                        Duration = getSeconds,
                        RackBoxName = Convert.ToString(dr["RackBoxName"]),
                        StockAvailable = Convert.ToInt32(dr["Stock"]),
                        QtyNegative = Convert.ToInt32(MakeNegative(Convert.ToInt32(dr["Qty_Realization"]))),
                        Site = "BNGK"

                    });
            }

            return Json(new { rows = k3list, title = reportTitle }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GenerateReport365(SCM_Sparepart_Report smodel)
        {
            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            List<SCM_Sparepart_Report> k3list = new List<SCM_Sparepart_Report>();

            SqlCommand cmd = new SqlCommand("sp_SCM_Sparepart_Generate_Report365", con);
            cmd.CommandType = CommandType.StoredProcedure;

            var reportTitle = "";
            var StrDateStart = Convert.ToDateTime(smodel.dateFrom).ToString("dd MMM yyyy");
            var StrDateTo = Convert.ToDateTime(smodel.dateTo).ToString("dd MMM yyyy");

            // date from 
            if (smodel.dateFrom.HasValue)
            {

                cmd.Parameters.AddWithValue("@dateFrom", smodel.dateFrom);
            }
            else
            {
                DateTime todayDate = DateTime.UtcNow.Date.AddDays(-7);
                cmd.Parameters.AddWithValue("@dateFrom", todayDate);
            }
            // date to
            if (smodel.dateTo.HasValue)
            {
                cmd.Parameters.AddWithValue("@dateTo", smodel.dateTo);
            }
            else
            {
                DateTime date2 = DateTime.UtcNow.Date;
                cmd.Parameters.AddWithValue("@dateTo", date2);
            }

            cmd.Parameters.AddWithValue("@ItemGroup", smodel.ItemGroup);
            cmd.Parameters.AddWithValue("@ProductCategory", smodel.ProductCategory);
            cmd.Parameters.AddWithValue("@COSTNAME", smodel.COSTNAME);
            cmd.Parameters.AddWithValue("@Status", smodel.Status);
            cmd.Parameters.AddWithValue("@MaintenanceType", smodel.MaintenanceType);

            SqlDataAdapter sd = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            con.Open();
            sd.Fill(dt);
            con.Close();

            if (smodel.dateFrom.HasValue && smodel.dateTo.HasValue)
            {
                reportTitle = "report periode " + StrDateStart + " - " + StrDateTo;

            }
            else
            {
                reportTitle = "report periode last 30 days";
            }


            foreach (DataRow dr in dt.Rows)
            {
                var ct = Convert.ToDateTime(dr["Create_Time"]);
                var pt = Convert.ToDateTime(dr["PrepareTime"]);
                var createTime = ct.ToString("yyyy-MM-dd HH:mm");
                var PrepareTime = pt.ToString("yyyy-MM-dd HH:mm");
                // get status
                var status = "";
                if (Convert.ToString(dr["Status"]) == "1")
                {
                    status = "Open";
                }
                else if (Convert.ToString(dr["Status"]) == "2")
                {
                    status = "Prepare";
                }
                else if (Convert.ToString(dr["Status"]) == "3")
                {
                    status = "Ready";
                }
                else if (Convert.ToString(dr["Status"]) == "4")
                {
                    status = "Close";
                }
                else if (Convert.ToString(dr["Status"]) == "10")
                {
                    status = "Cancel";
                }

                string getSeconds = "";

                if (Convert.ToInt32(dr["TotalSeconds"]) > 0)
                {
                    getSeconds = Helpers.Convertion.ConvertSecondsToDHMS(Convert.ToInt32(dr["TotalSeconds"]));
                }
                else
                {
                    getSeconds = "item not ready";
                }

                k3list.Add(
                    new SCM_Sparepart_Report
                    {
                        ITEMID = Convert.ToString(dr["ITEMID"]),
                        Quantity = Convert.ToInt32(dr["Qty_Realization"]),
                        ProductName = Convert.ToString(dr["ProductName"]),
                        ItemGroup = Convert.ToString(dr["ItemGroup"]),
                        ProCateName = Convert.ToString(dr["ProCateName"]),
                        Create_Date = Convert.ToDateTime(createTime).ToString("M/d/yyyy"),
                        PrepareDate = Convert.ToDateTime(PrepareTime).ToString("M/d/yyyy"),
                        Create_Time = Convert.ToDateTime(createTime).ToString("HH:mm"),
                        COSTNAME = Convert.ToString(dr["COSTNAME"]),
                        Status = status,
                        NameUserRequest = Convert.ToString(dr["Name"]),
                        ReadyByName = Convert.ToString(dr["ReadyByName"]),
                        MaintenanceType = Convert.ToString(dr["MaintenanceType"]),
                        TotalSeconds = Convert.ToInt32(dr["TotalSeconds"]),
                        Duration = getSeconds,
                        RackBoxName = Convert.ToString(dr["RackBoxName"]),
                        StockAvailable = Convert.ToInt32(dr["Stock"]),
                        QtyNegative = Convert.ToInt32(MakeNegative(Convert.ToInt32(dr["Qty_Realization"]))),
                        Site = "BNGK"

                    });
            }

            return Json(new { rows = k3list, title = reportTitle }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult exportExcelReport(SCM_Sparepart_Report smodel)
        {
            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            List<SCM_Sparepart_Report> k3list = new List<SCM_Sparepart_Report>();

            SqlCommand cmd = new SqlCommand("sp_SCM_Sparepart_Generate_Report365", con);
            cmd.CommandType = CommandType.StoredProcedure;

            var reportTitle = "";
            var StrDateStart = Convert.ToDateTime(smodel.dateFrom).ToString("dd MMM yyyy");
            var StrDateTo = Convert.ToDateTime(smodel.dateTo).ToString("dd MMM yyyy");

            // date from 
            if (smodel.dateFrom.HasValue)
            {

                cmd.Parameters.AddWithValue("@dateFrom", smodel.dateFrom);
            }
            else
            {
                DateTime todayDate = DateTime.UtcNow.Date.AddDays(-7);
                cmd.Parameters.AddWithValue("@dateFrom", todayDate);
            }
            // date to
            if (smodel.dateTo.HasValue)
            {
                cmd.Parameters.AddWithValue("@dateTo", smodel.dateTo);
            }
            else
            {
                DateTime date2 = DateTime.UtcNow.Date;
                cmd.Parameters.AddWithValue("@dateTo", date2);
            }

            cmd.Parameters.AddWithValue("@ItemGroup", smodel.ItemGroup);
            cmd.Parameters.AddWithValue("@ProductCategory", smodel.ProductCategory);
            cmd.Parameters.AddWithValue("@COSTNAME", smodel.COSTNAME);
            cmd.Parameters.AddWithValue("@Status", smodel.Status);
            cmd.Parameters.AddWithValue("@MaintenanceType", smodel.MaintenanceType);

            SqlDataAdapter sd = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            con.Open();
            sd.Fill(dt);
            con.Close();

            if (smodel.dateFrom.HasValue && smodel.dateTo.HasValue)
            {
                reportTitle = "report periode " + StrDateStart + " - " + StrDateTo;

            }
            else
            {
                reportTitle = "report periode last 30 days";
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Products");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "Prepare Date";
                worksheet.Cell(currentRow, 2).Value = "ITEMID";
                worksheet.Cell(currentRow, 3).Value = "Quantity";
                worksheet.Cell(currentRow, 4).Value = "Site";
                worksheet.Cell(currentRow, 5).Value = "Item Group";
                worksheet.Cell(currentRow, 6).Value = "Product Name";
                worksheet.Cell(currentRow, 7).Value = "Section";

                foreach (DataRow dr in dt.Rows)
                {
                    var ct = Convert.ToDateTime(dr["Create_Time"]);
                    var createTime = ct.ToString("yyyy-MM-dd HH:mm");
                    var pt = Convert.ToDateTime(dr["PrepareTime"]);
                    var prepareTime = pt.ToString("yyyy-MM-dd HH:mm");

                    // get status
                    var status = "";
                    if (Convert.ToString(dr["Status"]) == "1")
                    {
                        status = "Open";
                    }
                    else if (Convert.ToString(dr["Status"]) == "2")
                    {
                        status = "Prepare";
                    }
                    else if (Convert.ToString(dr["Status"]) == "3")
                    {
                        status = "Ready";
                    }
                    else if (Convert.ToString(dr["Status"]) == "4")
                    {
                        status = "Close";
                    }
                    else if (Convert.ToString(dr["Status"]) == "10")
                    {
                        status = "Cancel";
                    }

                    string getSeconds = "";

                    if (Convert.ToInt32(dr["TotalSeconds"]) > 0)
                    {
                        getSeconds = Helpers.Convertion.ConvertSecondsToDHMS(Convert.ToInt32(dr["TotalSeconds"]));
                    }
                    else
                    {
                        getSeconds = "item not ready";
                    }

                    currentRow++;
                    var itemGroup = "";
                    if (Convert.ToString(dr["ItemGroup"]) == "MachineP")
                    {
                        itemGroup = "MP";
                    } else
                    {
                        itemGroup = "TL";
                    }
                    worksheet.Cell(currentRow, 1).Value = Convert.ToDateTime(prepareTime).ToString("M/d/yyyy");
                    worksheet.Cell(currentRow, 2).Value = Convert.ToString(dr["ITEMID"]);
                    worksheet.Cell(currentRow, 3).Value = Convert.ToInt32(MakeNegative(Convert.ToInt32(dr["Qty_Realization"])));
                    worksheet.Cell(currentRow, 4).Value = "BNGK";
                    worksheet.Cell(currentRow, 5).Value = itemGroup;
                    worksheet.Cell(currentRow, 6).Value = Convert.ToString(dr["ProductName"]);
                    worksheet.Cell(currentRow, 7).Value = Convert.ToString(dr["COSTNAME"]);

                    k3list.Add(
                        new SCM_Sparepart_Report
                        {
                            ITEMID = Convert.ToString(dr["ITEMID"]),
                            Quantity = Convert.ToInt32(dr["Qty_Realization"]),
                            ProductName = Convert.ToString(dr["ProductName"]),
                            ItemGroup = Convert.ToString(dr["ItemGroup"]),
                            ProCateName = Convert.ToString(dr["ProCateName"]),
                            Create_Date = Convert.ToDateTime(createTime).ToString("M/d/yyyy"),
                            Create_Time = Convert.ToDateTime(createTime).ToString("HH:mm"),
                            COSTNAME = Convert.ToString(dr["COSTNAME"]),
                            Status = status,
                            NameUserRequest = Convert.ToString(dr["Name"]),
                            ReadyByName = Convert.ToString(dr["ReadyByName"]),
                            MaintenanceType = Convert.ToString(dr["MaintenanceType"]),
                            TotalSeconds = Convert.ToInt32(dr["TotalSeconds"]),
                            Duration = getSeconds,
                            RackBoxName = Convert.ToString(dr["RackBoxName"]),
                            StockAvailable = Convert.ToInt32(dr["Stock"]),
                            QtyNegative = Convert.ToInt32(MakeNegative(Convert.ToInt32(dr["Qty_Realization"]))),
                            Site = "BNGK"

                        });
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Report Sparepart to D365.xlsx");
                }
            }

            
        }

        [Authorize(Roles = "Administrator, WarehouseSparepart")]
        public ActionResult StockIn()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            ViewBag.SparepartList = spl;

            return View();
        }
        public class Tbl_SCM_D365ImporForm_ProductReceipt
        {
            public int No { get; set; }
            public string ProductReceipt { get; set; }
            public string Site { get; set; }
            public string Warehouse { get; set; }
            public string InternalProductReceipt { get; set; }
            public int TotalItem { get; set; }
            public string ItemGroup { get; set; }
            public DateTime? DateReceived { get; set; }
            public string ReceivedBy { get; set; }
        }
        [HttpPost]
        public ActionResult PackingSlipList()
        {
            ////var query = dbax.SCM_D365ImporForm_ProductReceipt.GroupBy(g => g.ProductReceipt).ToList();
            //var spl = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ItemGroup == "MachineP");
            //var CountRow = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ItemGroup == "MachineP").Count();

            //var results = dbax.SCM_D365ImporForm_ProductReceipt
            //                .Where(Q => Q.ItemGroup == "MachineP")
            //                .GroupBy(x => new { x.ProductReceipt, x.Warehouse, x.ItemGroup, x.Site }).Select(x =>
            //                new 
            //                {
            //                    ProductReceipt = x.Key,

            //                }).ToList();
            //var countResults = results.Count();

            string[] itemGroup = { "MachineP", "Tooling" };
            string s = "2023-06-06";
            DateTime dt = DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);


            var query = (from t in dbax.SCM_D365ImporForm_ProductReceipt.Where(w => itemGroup.Contains(w.ItemGroup) && w.ConfirmReceivedStatus == 0 && w.Site == "ITS" && w.Date > dt)
                         group t by new { t.ProductReceipt, t.Site }
             into grp
                         select new
                         {
                             ProductReceipt = grp.Key.ProductReceipt,
                             Site = grp.Key.Site,
                             TotalItem = grp.Count()
                         }).ToList();
            var countResults = query.Count();

            List<Tbl_SCM_D365ImporForm_ProductReceipt> actions = new List<Tbl_SCM_D365ImporForm_ProductReceipt>();
            var no = 0;
            foreach (var Item in query)
            {
                no++;
                var UrlAction = Url.Action("PackingSlipDetail", "Sparepart", new { area = "SCM", ProductReceipt = Item.ProductReceipt });
                actions.Add(
                    new Tbl_SCM_D365ImporForm_ProductReceipt
                    {
                        No = no,
                        ProductReceipt = "<a href='" + UrlAction + "' data-row-id='" + Item.ProductReceipt + "' class='tooltip-link' data-id='" + Item.ProductReceipt + "' >" + Item.ProductReceipt + "</a>",
                        Site = Item.Site,
                        TotalItem = Item.TotalItem
                    });
            }

            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                total = countResults,
            }, JsonRequestBehavior.AllowGet);

        }
        [HttpGet]
        public ActionResult PackingSlipItemList(string ProductReceipt)
        {
            var spl = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt).ToList();
            var countList = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt).Count();
            //cek if any status 0 (not yet confirm)
            var countNotConfirm = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt && w.ConfirmReceivedStatus == 0).Count();
            return Json(new
            {
                rows = spl,
                //totalNotFiltered = query,
                total = countList,
            }, JsonRequestBehavior.AllowGet);


        }
        [HttpGet]
        public ActionResult PackingSlipDetail(string ProductReceipt)
        {
            var spl = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt).ToList();
            var countList = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt).Count();
            //cek if any status 0 (not yet confirm)
            var countNotConfirm = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt && w.ConfirmReceivedStatus == 0).Count();

            ViewBag.ItemList = spl;
            ViewBag.ProductReceipt = ProductReceipt;
            var statusConfirm = 0;
            if (countNotConfirm > 0)
            {
                statusConfirm = 0;
            }
            else
            {
                statusConfirm = 1;
            }
            ViewBag.statusConfirmReceived = statusConfirm;

            return View();

        }
        [HttpPost]
        public ActionResult ConfirmReceivedStockIn(string ProductReceipt)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var spl = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == ProductReceipt).ToList();
            foreach (var data in spl)
            {
                // set status confirm received = 1 on table SCM_D365ImporForm_ProductReceipt 
                data.ConfirmReceivedStatus = 1;

                // insert all item to table SCM_sparepart_product_received
                SCM_Sparepart_Product_Received item = new SCM_Sparepart_Product_Received();

                item.ProductReceipt = ProductReceipt;
                item.QuantityReceived = Convert.ToInt32(data.ReceivedQuantity);
                item.ReceivedBy = currUser;
                item.ITEMID = data.Item;
                item.DateReceived = DateTime.Now;
                item.Site = data.Site;
                item.ItemGroup = data.ItemGroup;

                dbsp.SCM_Sparepart_Product_Received.Add(item);
                var save = dbsp.SaveChanges();

            }

            var update = dbax.SaveChanges();

            if (update > 0)
            {
                return Json(new { status = '1', msg = "Update Success" });
            }
            else
            {
                return Json(new { status = '0', msg = "Update Failed" });
            }
        }
        [HttpPost]
        public ActionResult PackingSlipListReceived()
        {
            ////var query = dbax.SCM_D365ImporForm_ProductReceipt.GroupBy(g => g.ProductReceipt).ToList();
            //var spl = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ItemGroup == "MachineP");
            //var CountRow = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ItemGroup == "MachineP").Count();
            string[] itemGroup = { "MachineP", "Tooling" };
            var result = dbsp.SCM_Sparepart_Product_Received.GroupBy(item => item.ProductReceipt)
                 .Select(grouping => grouping.FirstOrDefault())
                 .Where(w => itemGroup.Contains(w.ItemGroup))
                 .OrderByDescending(item => item.DateReceived)
                 .ToList();

            var query = (from t in dbsp.SCM_Sparepart_Product_Received.Where(w => itemGroup.Contains(w.ItemGroup))
                         group t by new { t.ProductReceipt, t.ItemGroup, t.Site, t.DateReceived }
             into grp
                         select new
                         {
                             ProductReceipt = grp.Key.ProductReceipt,
                             ItemGroup = grp.Key.ItemGroup,
                             Site = grp.Key.Site,
                             DateReceived = grp.Key.DateReceived,
                             TotalItem = grp.Count()
                         }).ToList();
            var countResults = query.Count();

            List<Tbl_SCM_D365ImporForm_ProductReceipt> actions = new List<Tbl_SCM_D365ImporForm_ProductReceipt>();
            var no = 0;
            foreach (var Item in result)
            {

                no++;
                var UrlAction = Url.Action("PackingSlipDetail", "Sparepart", new { area = "SCM", ProductReceipt = Item.ProductReceipt });
                actions.Add(
                    new Tbl_SCM_D365ImporForm_ProductReceipt
                    {
                        No = no,
                        ProductReceipt = "<a href='" + UrlAction + "'  >" + Item.ProductReceipt + "</a>",
                        ItemGroup = Item.ItemGroup,
                        Site = Item.Site,
                        DateReceived = Item.DateReceived
                    });
            }

            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                result = result,
                total = countResults,
            }, JsonRequestBehavior.AllowGet);

        }
        [HttpPost]
        public ActionResult GetDetailItem(string itemid)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            var itemDetail = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ITEMID == itemid).FirstOrDefault();


            return Json(itemDetail, JsonRequestBehavior.AllowGet);
        }
        private class RackTableParms
        {
            public int No { get; set; }
            public Int64 RackId { get; set; }
            public string RackName { get; set; }
            public string RackLocation { get; set; }
            public string edtiButton { get; set; }
        }

        [HttpPost]
        public ActionResult RackList()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var query = dbsp.SCM_Sparepart_Rack.Where(w => w.IsDelete == 0).OrderByDescending(x => x.CreateTime).ToList();
            var CountRow = query.Count();

            //int i = 0;

            List<RackTableParms> par = new List<RackTableParms>();
            //var query2 =
            //   (from a in db.SCM_Sparepart_Rack
            //    select new { r = i++, RackId = a.RackId, RackName = a.RackName, RackLocation = a.RackLocation, Action = "", IsDelete = a.IsDelete }).Where(w => w.IsDelete == "0").ToList() ;

            int no = 0;
            foreach (var Item in query)
            {
                no++;
                par.Add(new RackTableParms

                {
                    No = no,
                    RackId = Item.RackId,
                    RackName = Item.RackName,
                    RackLocation = Item.RackLocation,
                    edtiButton = "<a href='#EditRackModal' data-toggle='modal' data-id='" + Item.RackId + "' class='btn btn-warning btn-sm' id='BtnEditRack'><i class='fa fa-edit'></i></a> &nbsp; <a href='#' data-toggle='modal' data-id='" + Item.RackId + "' class='btn btn-danger btn-sm' id='BtnDeleteRack'><i class='fa fa-trash'></i></a> "
                });
            }



            //}

            //var ReqList = db.SCM_Sparepart_Request_Temp_Views.SqlQuery("SELECT a.ITEMID, a.userRequest , a.quantity, b.ProductName from SCM_Sparepart_Request_Temp a LEFT JOIN V_AXItemMaster b ON b.ITEMID = a.ITEMID WHERE a.userRequest = {0}", CurrUser.NIK).ToList();

            //var ReqList = db.SCM_Sparepart_Request_Temp.Where(w => w.userRequest == CurrUser.NIK).ToList();

            return Json(new
            {
                rows = par,
                totalNotFiltered = CountRow,
                total = CountRow
            }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddRack(SCM_Sparepart_Rack smodel)
        {

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            SCM_Sparepart_Rack par = new SCM_Sparepart_Rack();

            par.RackName = smodel.RackName;
            par.RackLocation = smodel.RackLocation;
            par.IsDelete = 0;
            par.CreateTime = DateTime.Now;

            dbsp.SCM_Sparepart_Rack.Add(par);

            var ins = dbsp.SaveChanges();
            if (ins == 1)
            {
                return Json(new
                {
                    status = "1",
                    msg = "Save Success"
                });
            }
            else
            {
                return Json(new
                {
                    status = "0",
                    msg = "failed Save Form"
                });
            }


        }

        [HttpGet]
        public ActionResult EditRack(byte RackId)
        {
            var query = dbsp.SCM_Sparepart_Rack.Where(w => w.RackId == RackId).OrderByDescending(x => x.CreateTime).FirstOrDefault();
            return Json(new { item = query }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult EditRack(byte RackId, string RackName, string RackLocation)
        {
            var query = dbsp.SCM_Sparepart_Rack.Where(w => w.RackId == RackId).ToList();
            foreach (var data in query)
            {
                data.RackName = RackName;
                data.RackLocation = RackLocation;
            }

            var update = dbsp.SaveChanges();

            if (update > 0)
            {
                return Json(new { status = '1', msg = "Update Success" });
            }
            else
            {
                return Json(new { status = '0', msg = "Update Failed" });
            }
        }

        [HttpPost]
        public ActionResult RemoveRack(byte RackId)
        {
            var query = dbsp.SCM_Sparepart_Rack.Where(w => w.RackId == RackId).FirstOrDefault();


            var delete = dbsp.SCM_Sparepart_Rack.Remove(query);
            var result = dbsp.SaveChanges();
            if (result > 0)
            {
                return Json(new { status = '1', msg = "Delete Success" });
            }
            else
            {
                return Json(new { status = '0', msg = "Delete Failed" });
            }
        }

        [HttpPost]
        public ActionResult StockInAddItemTemp(SCM_Sparepart_Stock_In_Temp smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            dbsp.SCM_Sparepart_Stock_In_Temp.Add(smodel);
            var ins = dbsp.SaveChanges();
            if (ins > 0)
            {
                return Json(new { status = 1, msg = "Item Inserted" });
            }
            else
            {
                return Json(new { status = 0, msg = "Failed Insert Item" });
            }
        }

        [HttpPost]
        public ActionResult StockInItemTemp()
        {
            //var spl = db.V_SCM_Sparepart_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();
            //var CountRow = db.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo);

            //var id = 1;
            var query =
               (from a in dbsp.SCM_Sparepart_Stock_In_Temp
                join b in dbsp.V_SCM_Sparepart_Master_List on a.ITEMID equals b.ITEMID
                select new { itemID = a.ITEMID, ProductName = b.ProductName, ProductCategory = b.ProductCategory, ItemGroup = b.ItemGroup, QtyIn = a.QtyIn, Action = "" }).ToList();

            var CountRow = query.Count();
            // var countNotReady = db.V_SCM_Sparepart_Request_Detail.Count(w => w.RequestNo == RequestNo && w.IsReady == 0);

            //foreach (var Item in spl)
            //{
            //    var Tools = "";
            //    if (Item.IsReady == 0)
            //    {
            //        Tools = "<a href=\"#\" title=\"ready\" id=\"procItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-success procItem\"><i class=\"fa fa-check\"></i></a>";
            //    }
            //    else
            //    {
            //        Tools = "<a href=\"#EditQuantityModal\" data-toggle=\"modal\" title=\"Decrease Qty\" id=\"editQty\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning procItem\"><i class=\"fa fa-pencil btnEdit\"></i></a>";
            //    }

            //    actions.Add(
            //        new V_SCM_Sparepart_ItemList
            //        {
            //            ITEMID = Item.ITEMID,
            //            ProductName = Item.ProductName,
            //            Quantity = Item.Quantity,
            //            Qty_Realization = Item.Qty_Realization,
            //            Tools = Tools
            //        });

            //}

            return Json(new
            {
                rows = query,
                totalNotFiltered = CountRow,
                total = CountRow
            }, JsonRequestBehavior.AllowGet);
        }


        // POST: SCM/Sparepart/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        
        public void  EmailTest()
        {
            string FilePath = Path.Combine(Server.MapPath("~/Emails/SCM/Sparepart/"), "Test.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            //Repalce [newusername] = signup user name   
            MailText = MailText.Replace("##UserName##", "863.09.22");
            MailText = MailText.Replace("##Dept##", "Information Technology");
            MailText = MailText.Replace("##Url##", "https://portal.ngkbusi.com");

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Sparepart Apps");
            var receiverEmail = new MailAddress("ikhsan.sholihin@ngkbusi.com", "Receiver");
            var password = "100%NGKbusi!";
            var sub = "[no-reply] Sparepart Apps";
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
            using (var mess = new MailMessage(senderEmail, receiverEmail)
            {
                Subject = sub,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(mess);
            }
            //return Json(new
            //{
            //    status = '1',
            //    msg = "Request ready to pickup"
            //});
        }
        [Authorize(Roles = "Administrator, WarehouseSparepart")]
        public ActionResult AdjustmentSparepart()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            ViewBag.SparepartList = spl;

            var ItemListAdjust = dbsp.SCM_Sparepart_Adjusment_Stock
            .GroupBy(x => x.ITEMID)
            .Select(g => g.Key)
            .ToList();

            ViewBag.ItemListAdjust = ItemListAdjust;

            return View();
        }
        [HttpPost]
        public JsonResult GetAdjusmentData(string SelITEMID, DateTime? dateFrom, DateTime? dateTo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //var sql =dbsp.SCM_Sparepart_Adjusment_Stock.ToList();
            var sql = from p in dbsp.SCM_Sparepart_Adjusment_Stock select p;
            if (SelITEMID != "all")
            {
                sql = from p in sql
                      where SelITEMID.Contains(p.ITEMID)
                      select p;
            }

            // Cek apakah dateFrom dan dateTo tidak kosong/null, lalu filter berdasarkan tanggal
            if (dateFrom.HasValue && dateTo.HasValue)
            {
                sql = from p in sql
                      where p.DateAdjustmet >= dateFrom.Value && p.DateAdjustmet <= dateTo.Value
                      select p;
            }

            // Order by DateAdjustmet descending
            sql = sql.OrderByDescending(p => p.DateAdjustmet);

            var CountRow = sql.Count();

            List<Tbl_SCM_Sparepart_Adjusment_Stock> adjustmentList = new List<Tbl_SCM_Sparepart_Adjusment_Stock>();

            foreach (var Item in sql)
            {
                adjustmentList.Add(
                    new Tbl_SCM_Sparepart_Adjusment_Stock
                    {
                        ITEMID = Item.ITEMID,
                        QtyAdjustment = Item.QtyAdjustment,
                        DateAdjustment = Item.DateAdjustmet?.ToString("dd MMMM yyyy"),
                        RemarkAdjustment = Item.RemarkAdjustment
                    });
            }
            var jsonResult = Json(new { rows = adjustmentList, totalNotFiltered = CountRow, total = CountRow, dateFrom = dateFrom, dateTo = dateTo, itemselected = SelITEMID }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public static void SendEmail()
        {
            try
            {
                MailMessage mailMessage = new MailMessage();
                MailAddress fromAddress = new MailAddress("ngkportal-notification@ngkbusi.com");
                mailMessage.From = fromAddress;
                mailMessage.To.Add("ikhsan.sholihin@ngkbusi.com");
                mailMessage.Body = "This is Testing Email Without Configured SMTP Server";
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = " Testing Email";
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "localhost";
                smtpClient.Send(mailMessage);
            }
            catch (Exception)
            {
            }
        }
        public ActionResult AdjustmentRequest()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var _currUser = ((ClaimsIdentity)User.Identity);
            string divisionName = _currUser.FindFirstValue("deptName");

            // Ambil list NIK admin dari table SCM_Sparepart_User_CostName
            //var adminNiks = dbsp.SCM_Sparepart_User_Management
            //    .Where(w => (w.Role == "Admin" || w.Role == "Developer") && w.IsActive == 1)
            //    .Select(w => w.userNIK)
            //    .ToList();

            //// Redirect jika user bukan Admin (NIK tidak ada di adminNiks) ATAU bukan dari divisi Maintenance
            //if (!(adminNiks.Contains(currUser) || string.Equals(divisionName, "Maintenance", StringComparison.OrdinalIgnoreCase)))
            //{
            //    return RedirectToAction("Index");
            //}

            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            ViewBag.SparepartList = spl;

            var ItemListAdjust = dbsp.SCM_Sparepart_Master_List

            .GroupBy(x => x.ITEMID)
            .Select(g => g.Key)
            .ToList();

            ViewBag.ItemListAdjust = ItemListAdjust;

            return View();
        }
        public ActionResult CreateAdjustmentRequest()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var currUserName = ((ClaimsIdentity)User.Identity).GetUserName();

            ViewBag.CurrUser = currUser;
            ViewBag.CurrUserName = currUserName;
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();


            var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            ViewBag.SparepartList = spl;

            var ItemListAdjust = dbsp.SCM_Sparepart_Master_List
            .GroupBy(x => x.ITEMID)
            .Select(g => g.Key)
            .ToList();

            ViewBag.ItemListAdjust = ItemListAdjust;

            return View();
        }

        [HttpPost]
        public JsonResult SaveAdjustmentItem()
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                string divisionName = _currUser.FindFirstValue("deptName");
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

                // Ambil list NIK admin dari table SCM_Sparepart_User_CostName
                var adminNiks = dbsp.SCM_Sparepart_User_Management
                    .Where(w => (w.Role == "Admin" || w.Role == "Developer") && w.IsActive == 1)
                    .Select(w => w.userNIK)
                    .ToList();

                // Redirect jika user bukan Admin (NIK tidak ada di adminNiks) ATAU bukan dari divisi Maintenance
                if (!(adminNiks.Contains(currUser) || string.Equals(divisionName, "Maintenance", StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unauthorized user"
                    });
                }

                //if (currUser != "792.02.18")
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "Unauthorized user"
                //    });
                //}

                //if (!string.Equals(divisionName, "Maintenance", StringComparison.OrdinalIgnoreCase))
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "Only Maintenance division is allowed"
                //    });
                //}

                var itemId = Request["ItemID"];
                var qtyInput = Convert.ToInt32(Request["Qty"]);

                var existingItem = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                    .FirstOrDefault(x =>
                        x.Creator_NIK == currUser &&
                        x.ItemID == itemId &&
                        x.AdjustmentReqID == null
                    );

                if (existingItem != null)
                {
                    int beforeQty = existingItem.Qty;
                    existingItem.Qty += qtyInput;

                    dbsp.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = $"This item already exists. Previous total: {beforeQty}, current total: {existingItem.Qty}"
                    });
                }
                else
                {
                    var item = new SCM_Sparepart_Adjustment_Request_Detail
                    {
                        Creator_NIK = currUser,
                        ItemID = itemId,
                        Qty = qtyInput,
                        AdjustmentReqID = null,
                        isApproval = false
                    };

                    dbsp.SCM_Sparepart_Adjustment_Request_Detail.Add(item);
                    dbsp.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "Item added successfully"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message
                });
            }
        }


        [HttpPost]
        public JsonResult SaveAdjustmentItemExcel(List<AdjustmentItemDto> items)
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                string divisionName = _currUser.FindFirstValue("deptName");
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

                // Ambil list NIK admin dari table SCM_Sparepart_User_CostName
                var adminNiks = dbsp.SCM_Sparepart_User_Management
                    .Where(w => (w.Role == "Admin" || w.Role == "Developer") && w.IsActive == 1)
                    .Select(w => w.userNIK)
                    .ToList();

                // Redirect jika user bukan Admin (NIK tidak ada di adminNiks) ATAU bukan dari divisi Maintenance
                if (!(adminNiks.Contains(currUser) || string.Equals(divisionName, "Maintenance", StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unauthorized user"
                    });
                }

                //if (currUser != "792.02.18")
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "Unauthorized user"
                //    });
                //}

                //if (!string.Equals(divisionName, "Maintenance", StringComparison.OrdinalIgnoreCase))
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "Only Maintenance division is allowed"
                //    });
                //}

                // Ambil semua ITEMID valid
                var validItemIds = dbsp.SCM_Sparepart_Master_List
                    .Select(x => x.ITEMID)
                    .ToList();

                var invalidItems = new List<string>();

                foreach (var dto in items)
                {
                    var itemId = dto.ItemName?.Trim();

                    if (string.IsNullOrEmpty(itemId) || !validItemIds.Contains(itemId))
                    {
                        invalidItems.Add(itemId);
                        continue;
                    }

                    var existingItem = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                        .FirstOrDefault(x =>
                            x.Creator_NIK == currUser &&
                            x.ItemID == itemId &&
                            x.AdjustmentReqID == null
                        );

                    if (existingItem != null)
                    {
                        existingItem.Qty += dto.Qty;
                    }
                    else
                    {
                        dbsp.SCM_Sparepart_Adjustment_Request_Detail.Add(
                            new SCM_Sparepart_Adjustment_Request_Detail
                            {
                                Creator_NIK = currUser,
                                ItemID = itemId,
                                Qty = dto.Qty,
                                AdjustmentReqID = null,
                                isApproval = false
                            });
                    }
                }

                dbsp.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Items processed",
                    invalidItems
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult DeleteAdjustmentItem(int id)
        {
            try
            {
                var item = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                    .FirstOrDefault(x => x.ID == id);

                if (item == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Item not found"
                    });
                }

                dbsp.SCM_Sparepart_Adjustment_Request_Detail.Remove(item);
                dbsp.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Item deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message
                });
            }
        }

        [HttpGet]
        public JsonResult GetAdjustmentItem(string requestId)
        {
            try
            {
                // 1. Gunakan FirstOrDefault agar tidak error jika requestId null atau tidak ketemu
                var headerData = dbsp.SCM_Sparepart_Adjustment_Request_Header
                    .FirstOrDefault(w => w.AdjustmentReqID == requestId);

                // 2. Jika memang tidak ada, kembalikan data kosong (bukan error)
                if (headerData == null)
                {
                    return Json(new
                    {
                        success = true, // Tetap true agar AJAX tidak masuk ke fungsi .fail()
                        data = new List<object>(),
                        header = new List<object>(),
                        approvalnames = new List<object>()
                    }, JsonRequestBehavior.AllowGet);
                }

                // 3. Jika ada, ambil detailnya
                var items = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                    .Where(w => w.AdjustmentReqID == requestId)
                    .ToList();

                // 4. Ambil Nama Approval
                var nilsToFind = new List<string> { headerData.Creator_NIK, headerData.Approver_NIK };
                nilsToFind = nilsToFind.Where(x => !string.IsNullOrEmpty(x)).ToList();

                var approvalNames = db.V_Users_Active
                    .Where(w => nilsToFind.Contains(w.NIK))
                    .Select(s => new { s.NIK, s.Name })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = items,
                    header = new List<object> { headerData }, // Dibungkus list agar JS tetap bisa pakai [0]
                    approvalnames = approvalNames
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                // Jika terjadi error teknis lainnya, kirim data kosong saja
                return Json(new { success = true, data = new List<object>(), header = new List<object>() }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public JsonResult GetAdjustmentItemByUser()
        {
            try
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

                var items = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                    .Where(h => h.Creator_NIK == currUser && h.isApproval == false)
                    .ToList();

                return Json(new { success = true, data = items }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private string GenerateAdjustmentReqID()
        {
            var now = DateTime.Now;
            string prefix = $"ADJREQ-{now:yyMM}";

            var lastId = dbsp.SCM_Sparepart_Adjustment_Request_Header
                .Where(x => x.AdjustmentReqID.StartsWith(prefix))
                .OrderByDescending(x => x.AdjustmentReqID)
                .Select(x => x.AdjustmentReqID)
                .FirstOrDefault();

            int seq = 1;
            if (lastId != null)
            {
                seq = int.Parse(lastId.Substring(lastId.Length - 3)) + 1;
            }

            return $"{prefix}{seq:000}";
        }

        [HttpPost]
        public JsonResult SaveAdjustmentRequest()
        {
            try
            {
                var identity = (ClaimsIdentity)User.Identity;
                string currUser = identity.GetUserId();
                string deptName = identity.FindFirstValue("DeptName");

                // 1️⃣ Generate Request ID
                string adjustmentReqId = GenerateAdjustmentReqID();

                string approverNik = null;

                if (currUser == "792.02.18")
                {
                    approverNik = "701.12.14";
                }
                else
                {
                    // Jika user lain, cari berdasarkan Department dan PositionID
                    approverNik = db.V_Users_Active
                        .Where(x =>
                            x.DeptName == deptName &&
                            (x.PositionID == "VI-A" ||
                             x.PositionID == "VI-A1" ||
                             x.PositionID == "VI-B" ||
                             x.PositionID == "VI-B1")) // Duplikasi kondisi VI-B & VI-B1 sudah dirapikan
                        .Select(x => x.NIK)
                        .FirstOrDefault();
                }

                if (approverNik == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Approver not found"
                    });
                }

                // 3️⃣ Insert HEADER
                var header = new SCM_Sparepart_Adjustment_Request_Header
                {
                    AdjustmentReqID = adjustmentReqId,
                    Date_Created = DateTime.Now,
                    Remark = Request["Remark"],
                    Creator_NIK = currUser,
                    Creator_Status = false,
                    Approver_NIK = approverNik,
                    Approver_Status = false,
                    isApproved = 0
                };

                dbsp.SCM_Sparepart_Adjustment_Request_Header.Add(header);

                // 4️⃣ Update DETAIL (belum submit)
                var details = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                    .Where(x => x.Creator_NIK == currUser && x.AdjustmentReqID == null)
                    .ToList();

                if (!details.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "No adjustment item found"
                    });
                }

                foreach (var item in details)
                {
                    item.AdjustmentReqID = adjustmentReqId;
                    item.isApproval = true;
                }

                dbsp.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Adjustment request submitted successfully",
                    adjustmentReqId
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message
                });
            }
        }
        [HttpGet]
        public JsonResult GetAdjustmentRequest()
        {
            try
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
           
                var request = dbsp.SCM_Sparepart_Adjustment_Request_Header
                    .ToList();
                
                return Json(new
                {
                    success = true,
                    data = request
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ApprovalAdjustmentRequest(string requestId, string actionType, string remark = "")
        {
            using (var dbContextTransaction = dbsp.Database.BeginTransaction())
            {
                try
                {
                    var request = dbsp.SCM_Sparepart_Adjustment_Request_Header
                        .FirstOrDefault(w => w.AdjustmentReqID == requestId);

                    if (request == null)
                    {
                        return Json(new { success = false, message = "Data tidak ditemukan." });
                    }

                    if (actionType == "SIGN_CREATOR")
                    {
                        request.Creator_Status = true;
                        request.Date_Created = DateTime.Now;
                        request.Creator_Date_Signed = DateTime.Now;
                        request.Creator_NIK = Request["Creator_NIK"];
                    }
                    else if (actionType == "SIGN_APPROVER")
                    {
                        request.Approver_Status = true;
                        request.isApproved = 1;
                        request.Approver_Date_Signed = DateTime.Now;
                        request.Approver_NIK = Request["Approver_NIK"];

                        var details = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                            .Where(w => w.AdjustmentReqID == requestId)
                            .ToList();

                        foreach (var item in details)
                        {
                            dbsp.SCM_Sparepart_Adjusment_Stock.Add(
                                new SCM_Sparepart_Adjusment_Stock
                                {
                                    ITEMID = item.ItemID,
                                    QtyAdjustment = item.Qty,
                                    DateAdjustmet = DateTime.Now,
                                    RemarkAdjustment = request.Remark,
                                    AdjustmentBy = request.Approver_NIK,
                                }
                            );
                        }
                    }
                    else if (actionType == "REJECT_APPROVER")
                    {
                        request.Approver_Status = false;
                        request.isApproved = 2;
                        request.Approver_Date_Signed = DateTime.Now;
                        request.Approver_NIK = Request["Approver_NIK"];
                        request.Approval_Message = Request["Approval_Message"];
                    }

                    // ⬇⬇⬇ INI YANG PENTING
                    dbsp.SaveChanges();
                    dbContextTransaction.Commit();

                    // email boleh gagal, data tetap commit
                    try
                    {
                        ApprovalEmail(requestId, actionType);
                    }
                    catch { }

                    return Json(new
                    {
                        success = true,
                        message = "Status & Stock Updated!",
                        data = request
                    });
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();

                    // Mengambil pesan paling dalam (paling spesifik)
                    var innerException = ex;
                    while (innerException.InnerException != null)
                    {
                        innerException = innerException.InnerException;
                    }

                    var errorDetail = innerException.Message;

                    // Opsional: Jika menggunakan Entity Framework, biasanya ada tambahan detail di 'UpdateException'
                    return Json(new { success = false, message = "Database Error: " + errorDetail });
                }
            }
        }

        [HttpGet]
        public JsonResult GetRackLocationById(string ITEMID)
        {
            try
            {
                var data = (
                    from rack in dbsp.SCM_Sparepart_Rack_Master
                    join box in dbsp.SCM_Sparepart_Cupboard_RackBox
                        on rack.RackBoxID equals box.RackBoxID
                    where rack.ITEMID == ITEMID
                    select new
                    {
                        rack.ID,
                        rack.ITEMID,
                        rack.RackBoxID,
                        RackBoxName = box.RackBoxName
                    }
                )
                .OrderBy(x => x.RackBoxName)
                .ToList();

                return Json(new
                {
                    success = true,
                    data = data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult SaveRackLocation(string ITEMID, string RackID)
        {
            try
            {
                if (string.IsNullOrEmpty(ITEMID) || string.IsNullOrEmpty(RackID))
                {
                    return Json(new
                    {
                        success = false,
                        message = "ITEMID or RackID is required"
                    });
                }

                var rack = new SCM_Sparepart_Rack_Master
                {
                    ITEMID = ITEMID,
                    RackBoxID = RackID
                };

                dbsp.SCM_Sparepart_Rack_Master.Add(rack);
                dbsp.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Rack location saved successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult DeleteRackLocation(int id)
        {
            try
            {
                var rack = dbsp.SCM_Sparepart_Rack_Master
                    .FirstOrDefault(x => x.ID == id);

                if (rack == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Rack location not found"
                    });
                }

                dbsp.SCM_Sparepart_Rack_Master.Remove(rack);
                dbsp.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Rack location deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        public bool ApprovalEmail(string requestId, string actionType)
        {
            try
            {
                // === TEMPLATE ===
                string filePath = Path.Combine(
                    Server.MapPath("~/Emails/SCM/Sparepart/"),
                    "StockAdjustmentApproval.html"
                );
                string mailText = System.IO.File.ReadAllText(filePath);

                // === HEADER ===
                var header = dbsp.SCM_Sparepart_Adjustment_Request_Header
                    .FirstOrDefault(x => x.AdjustmentReqID == requestId);
                if (header == null) throw new Exception("Header not found");

                var items = dbsp.SCM_Sparepart_Adjustment_Request_Detail
                    .Where(x => x.AdjustmentReqID == requestId)
                    .ToList();

                // === CONTEXT ===
                string receiverNIK, statusText, statusColor, messageText, subject;

                switch (actionType)
                {
                    case "SIGN_CREATOR":
                        receiverNIK = header.Approver_NIK;
                        statusText = "WAITING APPROVAL";
                        statusColor = "#f59e0b";
                        messageText = "A new approval request has been submitted";
                        subject = "[Approval Needed] Sparepart Stock Adjustment";
                        break;

                    case "SIGN_APPROVER":
                        receiverNIK = header.Creator_NIK;
                        statusText = "APPROVED";
                        statusColor = "#16a34a";
                        messageText = "Your stock adjustment request has been approved";
                        subject = "[Approved] Sparepart Stock Adjustment";
                        break;

                    case "REJECT_APPROVER":
                        receiverNIK = header.Creator_NIK;
                        statusText = "REJECTED";
                        statusColor = "#dc2626";
                        messageText = "Your stock adjustment request has been rejected";
                        subject = "[Rejected] Sparepart Stock Adjustment";
                        break;

                    default:
                        throw new Exception("Invalid actionType");
                }

                var receiver = db.V_Users_Active
                    .FirstOrDefault(w => w.NIK == receiverNIK);

                if (receiver == null || string.IsNullOrEmpty(receiver.Email))
                    throw new Exception("Receiver email not found");
                string rejectSectionHtml = "";

                if (!string.IsNullOrWhiteSpace(header.Approval_Message))
                {
                    rejectSectionHtml = $@"
        <div style='margin-top:16px;margin-bottom:16px;border:1px solid #fca5a5;background:#fef2f2;padding:12px;border-radius:6px;'>
            <p style='margin:0 0 4px 0;font-weight:bold;color:#b91c1c;'>
                Rejection Message
            </p>
            <p style='margin:0;color:#dc2626;'>
                {header.Approval_Message}
            </p>
        </div>";
                }

                // === REPLACE ===
                mailText = mailText
                    .Replace("##ReceiverName##", receiver.Name)
                    .Replace("##Status##", statusText)
                    .Replace("##color##", statusColor)
                    .Replace("##Message##", messageText)
                    .Replace("##RequestID##", header.AdjustmentReqID)
                    .Replace("##RequestDate##", header.Date_Created.ToString("dd MMM yyyy"))
                    .Replace("##Remark##", header.Remark ?? "-")
                    .Replace("##ApproveLink##", $"https://portal.ngkbusi.com/NGKBusi/SCM/Sparepart/AdjustmentRequest?requestId={requestId}")
                    .Replace("##RejectSection##", rejectSectionHtml);

                // === ITEMS ===
                var rows = new StringBuilder();
                int no = 1;
                foreach (var i in items)
                {
                    rows.Append($@"
<tr>
<td>{no++}</td>
<td>{i.ItemID}</td>
<td align='center'>{i.Qty}</td>
</tr>");
                }

                mailText = mailText.Replace("##ItemRows##", rows.ToString());

                // === SEND ===
                var sender = new MailAddress("ngkportal-notification@ngkbusi.com", "Sparepart Apps");
                var receiverMail = new MailAddress(receiver.Email, receiver.Name);

                using (var smtp = new SmtpClient("mail.ngkbusi.com", 587))
                {
                    smtp.EnableSsl = false;
                    smtp.Credentials = new NetworkCredential(sender.Address, "100%NGKbusi!");
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var msg = new MailMessage(sender, receiverMail)
                    {
                        Subject = subject,
                        Body = mailText,
                        IsBodyHtml = true
                    })
                    {
                        smtp.Send(msg);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[EMAIL SUCCESS] {requestId}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMAIL FAILED] {requestId} | {ex}");
                return false;
            }
        }

        [HttpGet]
        public ActionResult DownloadAdjustmentTemplate()
        {
            string filePath = Path.Combine(
                Server.MapPath("~/Files/SCM/Sparepart/"),
                "Sparepart Adjustment Form.xlsx"
            );
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("File not found");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            string fileName = "Sparepart Adjustment Form.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            ); 
        }

        [HttpPost]
        public JsonResult UploadItemXls()
        {
            try
            {
                HttpFileCollectionBase files = Request.Files;
                if (files.Count == 0)
                    return Json(new { success = false, message = "No file uploaded" });

                var file = files[0];

                string tempFolder = Server.MapPath("~/Files/SCM/Sparepart/Temp/");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                string filePath = Path.Combine(
                    tempFolder,
                    Guid.NewGuid() + Path.GetExtension(file.FileName)
                );

                file.SaveAs(filePath);

                var validItemIds = dbsp.SCM_Sparepart_Master_List
                    .Select(x => x.ITEMID)
                    .ToList();

                var result = new List<object>();
                var invalidItems = new List<object>();

                using (var workbook = new ClosedXML.Excel.XLWorkbook(filePath))
                {
                    var sheet = workbook.Worksheet(1);

                    int startRow = 2;
                    int colItemId = 2; 
                    int colQty = 3;    

                    int lastRow = sheet.LastRowUsed().RowNumber();

                    for (int row = startRow; row <= lastRow; row++)
                    {
                        string itemId = sheet.Cell(row, colItemId).GetValue<string>().Trim();
                        string qtyText = sheet.Cell(row, colQty).GetValue<string>().Trim();

                        if (string.IsNullOrEmpty(itemId))
                            continue;

                        int qty;
                        int.TryParse(qtyText, out qty);

                        if (!validItemIds.Contains(itemId))
                        {
                            invalidItems.Add(new
                            {
                                Row = row,
                                ItemID = itemId,
                                Qty = qty
                            });
                            continue;
                        }

                        result.Add(new
                        {
                            ItemName = itemId,
                            Qty = qty
                        });
                    }
                }

                System.IO.File.Delete(filePath);

                return Json(new
                {
                    success = true,
                    data = result,         
                    invalidItems = invalidItems 
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


    }
}
