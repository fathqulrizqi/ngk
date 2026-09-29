using NGKBusi.Models;
using NGK_AX.Models;
using NGKBusi.Areas.SCM.Models;
using System;
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
using System.Collections;
using NGKBusi.Helpers;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.Marketing.Models;
using System.Data.Entity.Validation;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using Rotativa;
using System.Diagnostics;

namespace NGKBusi.Areas.Marketing.Controllers
{
    public class MatPromController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        MatPromConnection dbmp = new MatPromConnection();
        // GET: Marketing/MatProm
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public FileResult ExportPDF(string ExportData)
        {
            using (MemoryStream stream = new System.IO.MemoryStream())
            {
                StringReader reader = new StringReader(ExportData);
                Document PdfFile = new Document(PageSize.A4);
                PdfWriter writer = PdfWriter.GetInstance(PdfFile, stream);
                PdfFile.Open();
                XMLWorkerHelper.GetInstance().ParseXHtml(writer, PdfFile, reader);
                PdfFile.Close();
                return File(stream.ToArray(), "application/pdf", "ExportData.pdf");
            }
        }
        public ActionResult GeneratePdf(string RequestNo)
        {
            var data = dbmp.V_Marketing_MatProm_Request.Where(w => w.RequestNo == RequestNo).FirstOrDefault();

            if (data.FormType == "distributor")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                    join am in dbmp.Marketing_MatProm_AddressMaster on rp.Recipient equals am.ID.ToString()
                                    where (rp.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = am.AddressTitle,
                                        RecipientAddress = am.AddressDetail
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "bengkel-points")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient.Where(w => w.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = rp.Recipient,
                                        RecipientAddress = ""
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "community")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient.Where(w => w.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = rp.Recipient,
                                        RecipientAddress = ""
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "internal")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                    join ax in dbmp.Users_Section_AX on rp.Recipient equals ax.COSTNAME.ToString()
                                    where (rp.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = ax.COSTNAME,
                                        RecipientAddress = ""
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "SalesMarketing")
            {

                if (data.useTemplate == 1)
                {
                    var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                        join th in dbmp.Marketing_MatProm_Template_Header on rp.Recipient equals th.ID.ToString()
                                        where (rp.RequestNo == RequestNo)
                                        select new Tbl_Marketing_MatProm_Recipient
                                        {
                                            RecipientName = th.TemplateName,
                                            RecipientAddress = ""
                                        }).ToList();
                    ViewBag.Recipient = QryRecipient;
                }
                else
                {
                    var QryRecipientNoTemplate = (from rp in dbmp.Marketing_MatProm_Request_Recipient

                                                  where (rp.RequestNo == RequestNo)
                                                  select new Tbl_Marketing_MatProm_Recipient
                                                  {
                                                      RecipientName = rp.Recipient,
                                                      RecipientAddress = ""
                                                  }).ToList();
                    ViewBag.Recipient = QryRecipientNoTemplate;
                }
                // additionalRecipient khusus form type salesMarketing
                var QryadditionalRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                              join am in dbmp.Marketing_MatProm_AddressMaster on rp.Recipient equals am.ID.ToString()
                                              where (rp.RequestNo == RequestNo)
                                              select new Tbl_Marketing_MatProm_Recipient
                                              {
                                                  RecipientName = am.AddressTitle,
                                                  RecipientAddress = am.AddressDetail
                                              }).ToList();
                ViewBag.additionalRecipient = QryadditionalRecipient;
            }
            ViewBag.header = data;
            // kondisi jika status signed ( waiting assignment) maka ambil data assignment user dari 
            if (data.Status == "Signed")
            {
                // ambil data assignment user dari table request_approval
                var approval = dbmp.Marketing_MatProm_Request_Approval.Where(w => w.RequestNo == RequestNo).ToList();
                ViewBag.approval = approval;
            }
            ViewBag.currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            ViewBag.RequestNo = RequestNo;
            var countNotReady = dbmp.V_Marketing_MatProm_Request_Detail.Count(w => w.RequestNo == RequestNo);
            ViewBag.NotReady = countNotReady;

            // ------------------------------------ detail item request -------------------------------------------- //

            var spl = dbmp.V_Marketing_MatProm_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();
            var request = dbmp.V_Marketing_MatProm_Request.Where(w => w.RequestNo == RequestNo).FirstOrDefault();

            List<Tbl_Marketing_MatProm_ItemList_Request> actions = new List<Tbl_Marketing_MatProm_ItemList_Request>();

            int No = 0;
            foreach (var Item in spl)
            {
                No++;
                var Tools = "";


                actions.Add(
                    new Tbl_Marketing_MatProm_ItemList_Request
                    {
                        Id = No,
                        ITEMID = Item.ITEMID,
                        ProductName = Item.ProductName,
                        Weight = Item.Weight,
                        Quantity = Item.Quantity,
                        Qty_Realization = Item.Qty_Realization,
                        RequestNotes = Item.RequestNotes,
                        IsChangeQty = Item.IsChangeQty,
                        Tools = Tools
                    });
            }

            ViewBag.itemDetail = actions;
            return new ViewAsPdf("GeneratePdf");
        }
        public ActionResult DataMatProm()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string q = "select ITEMID,  ProductName, ItemDescription, ItemGroup, ProductCategory, ProCateName, SectionType, Section from[dbo].[V_AXItemMaster] where ItemGroup like '%Tooling%' OR ItemGroup LIke '%MachineP%'";
            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(q, conn);
            var Sparepart = new List<Tbl_Marketing_MatProm_ItemMaster>();
            using (conn)
            {
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var sp = new Tbl_Marketing_MatProm_ItemMaster();
                    sp.ITEMID = rdr["ITEMID"].ToString();
                    sp.ProductName = rdr["ProductName"].ToString();
                    sp.ItemDescription = rdr["ItemDescription"].ToString();
                    sp.ItemGroup = rdr["ItemGroup"].ToString();
                    sp.ProductCategory = rdr["ProductCategory"].ToString();
                    sp.ProductCategory = rdr["ProductCategory"].ToString();

                    Sparepart.Add(sp);
                }
            }
            string[] itemGroup = { "" };

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => itemGroup.Contains(w.ItemGroup)).ToList();
            ViewBag.SparepartList = spl;


            var alertQtyMin = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => itemGroup.Contains(w.ItemGroup)).ToList();
            var countQtyMin = alertQtyMin.Count();
            ViewBag.alertQtyMin = alertQtyMin;
            ViewBag.countQtyMin = countQtyMin;
            return View();
        }
        [HttpGet]
        public ActionResult EditMasterItem(string ITEMID)
        {
            var spl = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == ITEMID).FirstOrDefault();
            ViewBag.DetailMatprom = spl;


            return View();
        }
        [HttpGet]
        public ActionResult AddExternalItem()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddExternalItem(Marketing_MatProm_ItemMaster smodel)
        {
            // get current session user
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            // get today date time
            DateTime today = DateTime.Now;

            //cek available itemid
            int countItemId = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == smodel.ITEMID).ToList().Count();

            // cek if itemid available
            if (countItemId > 0)
            {
                // if itemid available
                return Json(new
                {
                    status = "0",
                    msg = "Available Item ID"
                });
            }
            else
            {
                // add value for createby and createtime
                smodel.CreateBy = CurrUser.NIK;
                smodel.CreateTime = today;
                smodel.IsActive = 1;
                smodel.FirstStock = Convert.ToInt32(smodel.FirstStock);
                smodel.IsExternalItem = 1;
                smodel.ProductCategory = "S11010";
                smodel.ProCateName = "Spark Plug";
                smodel.Section = "Marketing";
                smodel.IsExternalItem = 1;


                try
                {
                    //insert to table
                    dbmp.Marketing_MatProm_ItemMaster.Add(smodel);
                    var ins = dbmp.SaveChanges();
                    if (ins == 1)
                    {
                        // if success insert
                        return Json(new
                        {
                            status = "1",
                            msg = "Item Inserted"
                        });
                    }
                    else
                    {
                        // if failed insert
                        return Json(new
                        {
                            status = "0",
                            msg = "Failed Insert",
                            smodel = smodel
                        }); ;
                    }

                }
                catch (Exception e)
                {
                    // if failed insert
                    return Json(new
                    {
                        status = "0",
                        msg = e.Message
                    });
                }
            }

        }
        [HttpPost]
        public JsonResult GetItemMasterList()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            string[] itemGroup = { "MachineP", "Tooling" };

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dbmp.V_Marketing_MatProm_ItemMaster.ToList();
            var CountRow = dbmp.V_Marketing_MatProm_ItemMaster.Count();
            List<Tbl_Marketing_MatProm_ItemMaster> actions = new List<Tbl_Marketing_MatProm_ItemMaster>();

            foreach (var Item in spl)
            {
                var Tools = "";

                var UrlAction = Url.Action("EditMasterItem", "MatProm", new { area = "Marketing", ITEMID = Item.ITEMID });

                var editButton = "";
                editButton = "<a href=\"" + UrlAction + "\" title=\"edit item\" class=\"btn btn-warning\"><i class=\"fa fa-edit\"></i></a>";

                actions.Add(
                    new Tbl_Marketing_MatProm_ItemMaster
                    {
                        ITEMID = Item.ITEMID,
                        ProductName = Item.ProductName,
                        ItemGroup = Item.ItemGroup,
                        ProCateName = Item.ProCateName,
                        Stock = Item.Stock,
                        Section = Item.Section,
                        Image = Tools,
                        Weight = Item.Weight,
                        Status = Item.IsActive == 1 ? "Active" : "Not Active",
                        EditButton = editButton
                    });
            }

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        public ActionResult DataAddress()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dbmp.Marketing_MatProm_AddressMaster.ToList();
            ViewBag.SparepartList = spl;


            var alertQtyMin = dbmp.Marketing_MatProm_AddressMaster.ToList();
            var countQtyMin = alertQtyMin.Count();
            ViewBag.alertQtyMin = alertQtyMin;
            ViewBag.countQtyMin = countQtyMin;
            return View();
        }
        [HttpPost]
        public JsonResult GetAddressList()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            string[] itemGroup = { "MachineP", "Tooling" };

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dbmp.Marketing_MatProm_AddressMaster.ToList();
            var CountRow = dbmp.Marketing_MatProm_AddressMaster.Count();
            List<Tbl_Marketing_MatProm_AddressMaster> actions = new List<Tbl_Marketing_MatProm_AddressMaster>();

            foreach (var Item in spl)
            {
                //var Tools = "";

                var UrlAction = Url.Action("EditMasterAddress", "MatProm", new { area = "Marketing" });

                var editButton = "";

                editButton = "<a href=\"" + UrlAction + "\" title=\"edit item\" class=\"btn btn-warning\"><i class=\"fa fa-edit\"></i></a>";

                actions.Add(
                    new Tbl_Marketing_MatProm_AddressMaster
                    {
                        AddressCode = Item.AddressCode,
                        AddressTitle = Item.AddressTitle,
                        AddressDetail = Item.AddressDetail,
                        AddressCity = Item.AddressCity,
                        AddressPerson = Item.AddressPerson,
                        Phone = Item.Phone,
                        //IsActive = activation,
                        BtnEdit = editButton
                    });
            }

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        public ActionResult CreateRequest()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var section = db.Users_Section_AX.Where(w => w.SECTIONTYPE != "").ToList();
            var Address = dbmp.Marketing_MatProm_AddressMaster.ToList();
            var Template = dbmp.Marketing_MatProm_Template_Header.Where(w => w.IsActive == 1).ToList();

            string[] itemGroup = { "PromAct", "PromSupply" };
            var products = dbmp.V_Marketing_MatProm_ItemMaster.Where(w=>w.IsActive==1).ToList(); // Ganti dengan entitas dan properti yang sesuai      
            //ViewBag.ProductList = new SelectList(products, "ITEMID", "ProductName");

            ViewBag.ProductList = products;
            ViewBag.CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            ViewBag.UserList = db.V_Users_Active.Where(w => w.Status == "Permanent").ToList();
            ViewBag.sectionCreator = CurrUser.CostName;
            ViewBag.section = section;
            ViewBag.Address = Address;
            ViewBag.Template = Template;
            ViewBag.NavHide = true;

            return View();

        }
        [HttpPost]
        public ActionResult CreateRequest(Marketing_MatProm_Request_Header smodel, string[] txtCommunityName, bool chkManualEvent = false)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            var usage = "";
            var DistributorID = "";
            var Event = "";
            String CommunityName;
            var CostName = "";
            var Template = "";
            // variable use template digunakan untuk membedakan form sales marketing yang menggunakan template atau tidak,
            // jika menggunakan template makan recipient akan join ke table template, jika tidak menggunakan template langsung tampilkan recipientnya
            byte useTemplate;

            // define form type
            var FormType = Request.Form.Get("formType");
            if (FormType == "distributor")
            {
                if (Request.Form.Get("tDistributor") == "")
                {
                    return Json(new
                    {
                        status = 3,
                        msg = "Empty Recipient"
                    });

                }
                usage = Request.Form.Get("tUsageDistributor");
                DistributorID = Request.Form.Get("tDistributor");
                Event = "";
                CommunityName = "";
                CostName = "";
                Template = "";
                useTemplate = 0;
            }
            else if (FormType == "bengkel-points")
            {
                if (Request.Form.Get("tBPevent") == "")
                {
                    return Json(new
                    {
                        status = 3,
                        msg = "Please Select event"
                    });
                }
                usage = Request.Form.Get("tUsageBP");
                DistributorID = "";
                Event = Request.Form.Get("tBPEvent");
                CommunityName = "";
                CostName = "";
                Template = "";
                useTemplate = 0;
            }
            else if (FormType == "community")
            {
                if (txtCommunityName == null || txtCommunityName.Length == 0 || string.Join(",", txtCommunityName) == "")
                {
                    return Json(new
                    {
                        status = 3,
                        msg = "Empty Community"
                    });
                }
                usage = Request.Form.Get("tCommunityUsage");
                DistributorID = "";
                Event = "";
                CommunityName = string.Join(",", txtCommunityName);
                CostName = "";
                Template = "";
                useTemplate = 0;
            }
            else if (FormType == "internal")
            {
                if (Request.Form.Get("tInternalSection") == "")
                {
                    return Json(new
                    {
                        status = 3,
                        msg = "Empty Department"
                    });
                }
                usage = Request.Form.Get("tInternalUsage");
                DistributorID = "";
                Event = "";
                CommunityName = "";
                CostName = Request.Form.Get("tInternalSection");
                Template = "";
                useTemplate = 0;
            }
            else if (FormType == "SalesMarketing")
            {
                // jika template tidak dipilih dan nama event kosong
                if (Request.Form.Get("tSMEventTemplate") == "0" && Request.Form.Get("tNameEvent") == "")
                {
                    // tampilkan alert
                    return Json(new
                    {
                        status = 3,
                        msg = "Empty Event Template"
                    });
                }
                // jika distributor tidak dipilih
                else if (Request.Form.Get("tDistributorSalesMarketing") == "")
                {
                    return Json(new
                    {
                        status = 3,
                        msg = "Empty Recipient"
                    });

                }
                // jika menggunakan template dan event template tidak dipilih
                else if (chkManualEvent == false && Request.Form.Get("tSMEventTemplate") == "0")
                {
                    // if use manual not checked but empty selected event
                    return Json(new
                    {
                        status = 3,
                        msg = "Empty Event Template"
                    });
                }
                //jika menggunakan manual event dan nama event tidak di isi
                else if (chkManualEvent == true && Request.Form.Get("tNameEvent") == "")
                {
                    // if use manual checked but empty event name
                    return Json(new
                    {
                        status = 3,
                        msg = "Empty Event Template"
                    });
                }

                //jika menggunakan template
                if (chkManualEvent == false)
                {
                    useTemplate = 1;
                    Template = Request.Form.Get("tSMEventTemplate");
                    int TemplateID = int.Parse(Request.Form.Get("tSMEventTemplate"));
                    // dapatkan value template name dari event id yang dipilih
                    var qEvent = dbmp.Marketing_MatProm_Template_Header.Where(w => w.ID == TemplateID).FirstOrDefault();
                    Event = qEvent.TemplateName;


                }
                // jika menggunakan manual event
                else
                {
                    useTemplate = 0;
                    Template = "";
                    Event = Request.Form.Get("tNameEvent");
                }

                usage = Request.Form.Get("tSMEventUsage");
                DistributorID = Request.Form.Get("tDistributorSalesMarketing");
                CommunityName = "";
                CostName = "";

            }
            else
            {
                usage = "";
                DistributorID = "";
                Event = "";
                CommunityName = "";
                CostName = "";
                Template = "";
                useTemplate = 0;
            }

            var spl = from sp in dbmp.Marketing_MatProm_Request_Temp select sp;
            if (FormType == "SalesMarketing")
            {
                int TemplateID = Convert.ToInt32(Request.Form.Get("tSMEventTemplate"));
                spl = spl.Where(w => w.formType == FormType && w.userRequest == CurrUser.NIK && w.TemplateID == TemplateID);
            }
            else
            {
                spl = spl.Where(w => w.formType == FormType && w.userRequest == CurrUser.NIK);
            }

            var stockMin = new ArrayList();
            int sMin = 0;
            int q = 0;
            foreach (var dt in spl)
            {
                var itemid = dt.ITEMID;
                var QStock = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == itemid).FirstOrDefault();
                int Stock = QStock.Stock;

                if (Stock < dt.quantity)
                {
                    sMin++;
                    stockMin.Add(dt.ITEMID);
                }
                if (dt.quantity == 0)
                {
                    q++;
                }
            }

            if (sMin == 0)
            {
                SqlCommand cmd = new SqlCommand("sp_Marketing_MatProm_Request_Create", con);
                cmd.CommandType = CommandType.StoredProcedure;

                //cek item temp
                var countList = dbmp.Marketing_MatProm_Request_Temp.Where(me => me.formType == FormType && me.userRequest == CurrUser.NIK).ToList();
                var count = countList.Count();
                if (count > 0 && q == 0)
                {
                    //return Json(new
                    //{
                    //    status = 0,
                    //    msg = "Empty Quantity Found in Item List Request",
                    //    minstock = sMin,
                    //    nulQTY = q,
                    //    itemidminus = DistributorID,
                    //    count = count,
                    //    countlist = countList
                    //}); ;
                    var Class = Request.Form.Get("tClass");
                    if (Class == null)
                    {
                        Class = "";
                    }

                    cmd.Parameters.AddWithValue("@userRequest", CurrUser.NIK);
                    cmd.Parameters.AddWithValue("@DistributorID", DistributorID);
                    cmd.Parameters.AddWithValue("@Event", Event);
                    cmd.Parameters.AddWithValue("@CommunityName", CommunityName);
                    cmd.Parameters.AddWithValue("@CostName", CostName);
                    cmd.Parameters.AddWithValue("@Template", Template);
                    cmd.Parameters.AddWithValue("@Usage", usage);
                    cmd.Parameters.AddWithValue("@FormType", FormType);
                    cmd.Parameters.AddWithValue("@UseTemplate", useTemplate);

                    con.Open();
                    int i = cmd.ExecuteNonQuery();
                    con.Close();

                    if (i > 0)
                    {
                        return Json(new
                        {
                            status = 1,
                            msg = "Request Send",
                            minstock = spl,
                            itemidminus = stockMin,
                            q = q,
                            count = count,
                            formType = FormType,
                            communityLength = txtCommunityName.Length,
                            CommunityName = CommunityName
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
                else if (q > 0)
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "Empty Quantity Found in Item List Request",
                        minstock = sMin,
                        nulQTY = q,
                        itemidminus = stockMin
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = 0,
                        msg = "Empty Item Request ",
                        minstock = sMin,
                        itemidminus = stockMin
                    });
                }
            }
            else if (q > 0)
            {
                return Json(new
                {
                    status = 2,
                    msg = "Empty Quantity Found in Item List Request",
                    minstock = sMin,
                    itemidminus = stockMin,
                    eventt = Template
                });
            }
            else
            {
                return Json(new
                {
                    status = 2,
                    msg = "Plese Check Stock for this Item " + stockMin,
                    minstock = sMin,
                    itemidminus = stockMin,
                    eventt = Template
                });
            }
        }
        [HttpGet]
        public ActionResult GetProducts(string term)
        {
            string[] itemGroup = { "PromAct", "PromSupply" };
            var items = db.V_AXItemMaster.Where(w => itemGroup.Contains(w.ItemGroup)).ToList();

            // Ubah data menjadi format JSON
            var jsonData = items.Select(item => new
            {
                Value = item.ITEMID,
                Text = item.ProductName
            });

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetProductInfo(string ITEMID)
        {
            var product = db.V_AXItemMaster.Where(w => w.ITEMID == ITEMID).FirstOrDefault();
            return Json(product, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddRequestList(Marketing_MatProm_Request_Temp smodel)
        {
            try
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
                var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

                var data = dbmp.Marketing_MatProm_Request_Temp.FirstOrDefault(x => x.ITEMID == smodel.ITEMID && x.userRequest == CurrUser.NIK && x.TemplateID == smodel.TemplateID);
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
                    dbmp.Marketing_MatProm_Request_Temp.Add(smodel);
                    var ins = dbmp.SaveChanges();
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
            catch (Exception e)
            {
                return Json(new
                {
                    status = "0",
                    msg = e.Message
                });
            }
        }
        [HttpPost]
        public ActionResult RequestList(string formType)
        {

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            //List<Marketing_MatProm_Request_Temp> Marketing_MatProm_Request_Temps = dbmp.Marketing_MatProm_Request_Temp.ToList();
            List<V_Marketing_MatProm_ItemMaster> V_Marketing_MatProm_ItemMaster = dbmp.V_Marketing_MatProm_ItemMaster.ToList();

            //if use template and no item found in request_temp then insert all item base on selected template            
            if (formType == "SalesMarketing")
            {
                //get template ID
                var TemplateID = Request.Form.Get("TemplateID");
                int TmpltID = Convert.ToInt32(TemplateID);
                if (!string.IsNullOrEmpty(TemplateID))
                {
                    // count item by templateID on table request temp
                    var CountItemTempByTemplate = dbmp.Marketing_MatProm_Request_Temp.Where(w => w.userRequest == CurrUser.NIK && w.formType == formType && w.TemplateID == TmpltID).ToList().Count();
                    if (CountItemTempByTemplate < 1)
                    {
                        // insert template item to request temp
                        var templateItem = (from th in dbmp.Marketing_MatProm_Template_Header
                                            join td in dbmp.Marketing_MatProm_Template_Detail on th.ID equals td.TemplateID
                                            where (th.ID == TmpltID)
                                            select new Marketing_MatProm_Template_Views
                                            {
                                                ITEMID = td.ITEMID,
                                                TemplateName = th.TemplateName,
                                                TemplateID = th.ID
                                            }).ToList();
                        foreach (var Item in templateItem)
                        {
                            Marketing_MatProm_Request_Temp itemRequest = new Marketing_MatProm_Request_Temp();
                            itemRequest.ITEMID = Item.ITEMID;
                            itemRequest.userRequest = CurrUser.NIK;
                            itemRequest.formType = formType;
                            itemRequest.TemplateID = Convert.ToInt32(TemplateID);

                            dbmp.Marketing_MatProm_Request_Temp.Add(itemRequest);
                        }
                        dbmp.SaveChanges();
                    }
                }

            }
            else if (formType == "community")
            {
                var countItem = dbmp.Marketing_MatProm_Request_Temp.Where(w => w.userRequest == CurrUser.NIK && w.formType == formType).Count();

                if (countItem < 1)
                {

                    var templateItemCom = (from th in dbmp.Marketing_MatProm_Template_Header
                                           join td in dbmp.Marketing_MatProm_Template_Detail on th.ID equals td.TemplateID
                                           where (th.TemplateName == "community")
                                           select new Marketing_MatProm_Template_Views
                                           {
                                               ITEMID = td.ITEMID,
                                               TemplateName = th.TemplateName,
                                               TemplateID = th.ID
                                           }).ToList();
                    foreach (var Items in templateItemCom)
                    {
                        Marketing_MatProm_Request_Temp itemComRequest = new Marketing_MatProm_Request_Temp();
                        itemComRequest.ITEMID = Items.ITEMID;
                        itemComRequest.userRequest = CurrUser.NIK;
                        itemComRequest.formType = formType;
                        itemComRequest.TemplateID = Items.TemplateID;

                        dbmp.Marketing_MatProm_Request_Temp.Add(itemComRequest);
                    }
                    dbmp.SaveChanges();

                }
            }

            var TemplateID2 = Request.Form.Get("TemplateID");
            int TmpltID2 = Convert.ToInt32(TemplateID2);
            var spl2 = from mp in dbmp.Marketing_MatProm_Request_Temp select mp;
            if (formType == "SalesMarketing")
            {
                int TemplateID = Convert.ToInt32(Request.Form.Get("TemplateID"));
                spl2 = spl2.Where(w => w.userRequest == CurrUser.NIK && w.formType == formType && w.TemplateID == TemplateID);
            }
            else
            {
                spl2 = spl2.Where(w => w.formType == formType && w.userRequest == currUser);
            }



            var ListTemp = spl2.ToList();

            //var countItemspl2 = spl2.Count();
            //return Json(new
            //{
            //    status = ListTemp,
            //    msg = currUser,
            //    count = countItemspl2
            //});

            List<Tbl_Marketing_MatProm_Request_Temp_Views> ItemRequestList = new List<Tbl_Marketing_MatProm_Request_Temp_Views>();
            foreach (var itemTemp in ListTemp)
            {
                // get product name 
                var checkItemMaster = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == itemTemp.ITEMID).Count();
                var qItemMaster = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == itemTemp.ITEMID).FirstOrDefault();
                string ProductName = "";
                if (checkItemMaster == 0)
                {
                    ProductName = "No Item Name";
                }
                else
                {
                    ProductName = qItemMaster.ProductName;
                }
                ItemRequestList.Add(
                    new Tbl_Marketing_MatProm_Request_Temp_Views
                    {
                        ITEMID = itemTemp.ITEMID,
                        ProductName = ProductName,
                        Quantity = itemTemp.quantity,
                        Notes = itemTemp.Notes,
                        TemplateID = itemTemp.TemplateID.ToString()
                    }); ;
            }

            return Json(ItemRequestList, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult EditQuantityItem(string itemid, int templateID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            //List<Marketing_MatProm_Request_Temp> marketing_MatProm_Request_Temp = .ToList();
            //List<V_Marketing_MatProm_ItemMaster> v_Marketing_MatProm_ItemMaster = dbmp.V_Marketing_MatProm_ItemMaster.ToList();

            var item = (
                from ai in dbmp.Marketing_MatProm_Request_Temp
                join al in dbmp.V_Marketing_MatProm_ItemMaster on ai.ITEMID equals al.ITEMID
                where (ai.userRequest == CurrUser.NIK && ai.ITEMID == itemid && ai.TemplateID == templateID)
                select new Tbl_Marketing_MatProm_Request_Temp_Views
                {
                    ITEMID = ai.ITEMID,
                    ProductName = al.ProductName,
                    Quantity = ai.quantity,
                    UserRequest = ai.userRequest,
                    Stock = al.Stock,
                    TemplateID = ai.TemplateID.ToString()
                }).FirstOrDefault();

            //var item = (
            //    from al in v_Marketing_MatProm_ItemMaster 
            //    where (al.ITEMID == itemid)
            //    select new Tbl_Marketing_MatProm_Request_Temp_Views
            //    {
            //        ITEMID = al.ITEMID,
            //        ProductName = al.ProductName,
            //        Quantity = 0,
            //        UserRequest = "",
            //        Stock = al.Stock,
            //        TemplateID = ""
            //    }).ToList();

            return Json(item, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult EditQuantityItem()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            int Qty = int.Parse(Request.Form.Get("Quantity"));
            var ITEMID = Request.Form.Get("tEditITEMID");
            int TemplateID = Convert.ToInt32(Request.Form.Get("TemplateID"));

            var data = dbmp.Marketing_MatProm_Request_Temp.Where(x => x.ITEMID == ITEMID && x.TemplateID == TemplateID && x.userRequest == CurrUser.NIK).FirstOrDefault();
            data.quantity = Qty;

            var update = dbmp.SaveChanges();
            //var update = 1;
            if (update == 1)
            {
                return Json(new
                {
                    status = "1",
                    msg = "Update Quntity Success",
                    qty = Qty,
                    ITEMID = ITEMID,
                    TemplateID = TemplateID,
                    data = data
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
        [HttpPost]
        public ActionResult RemoveList(string itemid)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            try
            {
                var marketing_MatProm_Request_Temp = dbmp.Marketing_MatProm_Request_Temp.Where(w => w.ITEMID == itemid && w.userRequest == CurrUser.NIK).FirstOrDefault();
                dbmp.Marketing_MatProm_Request_Temp.Remove(marketing_MatProm_Request_Temp);
                var del = dbmp.SaveChanges();
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
        public ActionResult RequestData()
        {
            string dateFrom = Session["startDate"] as string;
            string dateTo = Session["endDate"] as string;
            string statusFilter = Session["status"] as string;

            var spl = from sp in dbmp.V_Marketing_MatProm_Request select sp;
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            //var getData = 0;
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
                    // jika login menggunakan akun marketing verifikator, manager marketing dan user Ihsan makan tampilkan semua data
                    if (CurrUser.NIK == "871.01.24" || CurrUser.NIK == "814.01.19" || CurrUser.NIK == "863.09.22" || CurrUser.NIK == "810.11.18" || CurrUser.NIK == "861.08.22")
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                        //getData = 1;
                    }
                    else if(CurrUser.PositionName == "SENIOR MANAGER")
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.DivisionName == CurrUser.DivisionName);
                        //getData = 2;
                    }
                    else
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.CostName == CurrUser.CostName);
                        //getData = 3;
                    }

                }
                else
                {
                    // jika login menggunakan akun marketing verifikator, manager marketing dan user Ihsan makan tampilkan semua data
                    if (CurrUser.NIK == "871.01.24" || CurrUser.NIK == "814.01.19" || CurrUser.NIK == "863.09.22" || CurrUser.NIK == "810.11.18" || CurrUser.NIK == "861.08.22")
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate));
                        //getData = 4;
                    }
                    else if (CurrUser.PositionName == "SENIOR MANAGER")
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.DivisionName == CurrUser.DivisionName);
                        //getData = 5;
                    }
                    else
                    {
                        spl = spl.Where(w => (DbFunctions.TruncateTime(w.Create_Time) >= fromDate && DbFunctions.TruncateTime(w.Create_Time) <= toDate) && w.CostName == CurrUser.CostName);
                        //getData = 6;
                    }

                }
            }
            else
            { 
                if (CurrUser.NIK == "871.01.24" || CurrUser.NIK == "814.01.19" || CurrUser.NIK == "863.09.22" || CurrUser.NIK == "810.11.18" || CurrUser.NIK == "861.08.22")
                {
                    spl = spl.OrderByDescending(o => o.Create_Time);
                    //getData = 7;
                }
                else if (CurrUser.PositionName == "SENIOR MANAGER")
                {
                    spl = spl.Where(w => w.DivisionName == CurrUser.DivisionName);
                    //getData = 8;
                }
                else
                {
                    spl = spl.Where(w => w.CostName == CurrUser.CostName).OrderByDescending(o => o.Create_Time);
                    //getData = 9;
                }


            }

            var result = spl.ToList();
            //ViewBag.RequestList = result;
            //ViewBag.status = "";
            //ViewBag.dateStart = null;
            //ViewBag.dateEnd = null;
            //ViewBag.autopick = "false";
            //ViewBag.StartDate = dateFrom;
            //ViewBag.endDate = dateTo;
            //ViewBag.statusFilter = statusFilter;

            return View();
        }
        [HttpPost]
        public ActionResult GetRequestData(string dateFrom, string dateTo, string status, string RequestNo)
        {
            //string dateFrom = Session["startDate"] as string;
            //string dateTo = Session["endDate"] as string;
            //string statusFilter = Session["status"] as string;

            var spl = dbmp.V_Marketing_MatProm_Request.AsQueryable();
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();            

            if (!string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo)) {
                var fromDate = Convert.ToDateTime(dateFrom);
                var startDate = fromDate.ToString("yyyy-MM-dd");

                var toDate = Convert.ToDateTime(dateTo);
                var endDate = toDate.ToString("yyyy-MM-dd");

                spl = spl.Where(x => (DbFunctions.TruncateTime(x.Create_Time) >= fromDate && DbFunctions.TruncateTime(x.Create_Time) <= toDate));
            }
            else
            {
                DateTime today = DateTime.Today;
                DateTime last30Days = today.AddDays(-30);
                spl = spl.Where(x => (DbFunctions.TruncateTime(x.Create_Time) >= last30Days && DbFunctions.TruncateTime(x.Create_Time) <= today));
            }

            if (status.ToLower() != "all")
            {
                spl = spl.Where(x => x.Status == status);
            }

            if (!string.IsNullOrEmpty(RequestNo))
            {
                spl = spl.Where(x => x.RequestNo == RequestNo);
            }
            var getIF = 0;
            if (currUser == "871.01.24" || currUser == "814.01.19" || currUser == "863.09.22" || currUser == "810.11.18" || currUser == "861.08.22")
            {
                getIF = 1;
                spl = spl.Where(x => 1 == 1);
            }
            else if(CurrUser.PositionName == "SENIOR MANAGER" || CurrUser.PositionName == "DEPUTY GENERAL MANAGER" || CurrUser.PositionName == "DEPUTY GENERAL MANAGER ACTING" || CurrUser.PositionID == "8B" || CurrUser.PositionID == "7B")
            {
                getIF = 2;
                spl = spl.Where(x => x.DivisionName == CurrUser.DivisionName);
            }
            else
            {
                getIF = 3;
                spl = spl.Where(x => x.DeptName == CurrUser.DeptName);

            }


            var result = spl.OrderByDescending(o=>o.Create_Time).ToList();
            List<Tbl_V_Marketing_MatProm_Request> reqList = new List<Tbl_V_Marketing_MatProm_Request>();
            foreach (var rs in result)
            {
                string sta = "";
                string badge = "";

                if (rs.Status == "WaitingSign")
                {
                    sta = "Not Signed";
                    badge = "secondary";
                    //countdown = "";
                }
                else if (rs.Status == "Signed")
                {
                    sta = "Signed";
                    badge = "warning";
                    //countdown = "";
                }
                else if (rs.Status == "Acknowledgment")
                {
                    sta = "Acknowledgment";
                    badge = "info";
                    //countdown = "";
                    //countdown = Html.Raw("Please Close your request before <strong>") + tbl.CloseTimeDueDate.ToString("dd - MMMM - yyyy") + Html.Raw("</strong>");
                }
                else if (rs.Status == "Approved")
                {
                    sta = "Approved";
                    badge = "primary";
                    //countdown = "";
                }
                else if (rs.Status == "Verified")
                {
                    sta = "Verified";
                    badge = "success";
                    //countdown = "";
                }
                else if (rs.Status == "Cancelled")
                {
                    sta = "Cancelled";
                    badge = "danger";
                    //countdown = "";
                } else
                {
                    sta = "Not Found";
                    badge = "dark";
                }

                var  btnAction = "";
                var urlAction = Url.Action("DetailRequest", "MatProm", new { area = "Marketing", RequestNo = rs.RequestNo });
                btnAction = "<a href=" + urlAction + " class=\"btn btn-info btn - sm\" data-toggle=\"tooltip\" data-placement=\"right\" title=\"view Detail\"><i class=\"fa fa-eye\"></i> </a>";
                reqList.Add(
                    new Tbl_V_Marketing_MatProm_Request
                    {
                        RequestNo = rs.RequestNo,
                        Name = rs.Name,
                        FormType = rs.FormType,
                        CostName = rs.CostName,
                        RequestTime = rs.Create_Time.ToString("dd MMM yyyy"),
                        Status = "<h5 ><span class=\"badge badge-" + badge + "\"> " + sta + "</span></h5>",
                        Action = btnAction,

                    });
            }
            //ViewBag.RequestList = result;
            //ViewBag.status = "";
            //ViewBag.dateStart = null;
            //ViewBag.dateEnd = null;
            //ViewBag.autopick = "false";
            //ViewBag.StartDate = dateFrom;
            //ViewBag.endDate = dateTo;
            //ViewBag.statusFilter = status;

            return Json(new { reqList = reqList, role = CurrUser.RoleName, dateFrom = dateFrom, dateTo = dateTo, status = status, getIF =getIF, posName = CurrUser.PositionName }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ItemListRequest(string RequestNo, string Status)
        {
            var spl = dbmp.V_Marketing_MatProm_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();
            var CountRow = dbmp.V_Marketing_MatProm_Request_Detail.Count(w => w.RequestNo == RequestNo);

            List<V_SCM_Sparepart_ItemList> actions = new List<V_SCM_Sparepart_ItemList>();


            foreach (var Item in spl)
            {
                var Tools = "";
                if (Status == "1")
                {
                    Tools = "<a href=\"#EditQuantityModal\" data-toggle=\"modal\" title=\"Edit\" id=\"EditQtyItem\" data-itemid=\"" + Item.ITEMID + "\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning EditQtyItem\"><i class=\"fa fa-pencil\"></i></a> <a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";
                }
                else
                {

                    Tools = "<a href=\"#\" title=\"ready\" id=\"procItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-primary procItem\"><i class=\"fa fa-check\"></i></a> <a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";

                }

                actions.Add(
                    new V_SCM_Sparepart_ItemList
                    {
                        ITEMID = Item.ITEMID,
                        ProductName = Item.ProductName,
                        Quantity = Item.Quantity,
                        Qty_Realization = Item.Qty_Realization,
                        Tools = Tools
                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
                status = 1
            }, JsonRequestBehavior.AllowGet);
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

            // Other field you may need from the Product entity
        }
        [Authorize]
        [HttpGet]
        // GET: SCM/Sparepart/Details/5
        public ActionResult DetailRequest(string RequestNo)
        {

            var data = dbmp.V_Marketing_MatProm_Request.Where(w => w.RequestNo == RequestNo).FirstOrDefault();

            if (data.FormType == "distributor")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                    join am in dbmp.Marketing_MatProm_AddressMaster on rp.Recipient equals am.ID.ToString()
                                    where (rp.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = am.AddressTitle,
                                        RecipientAddress = am.AddressDetail
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "bengkel-points")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient.Where(w => w.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = rp.Recipient,
                                        RecipientAddress = ""
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "community")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient.Where(w => w.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = rp.Recipient,
                                        RecipientAddress = ""
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "internal")
            {
                var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                    join ax in dbmp.Users_Section_AX on rp.Recipient equals ax.COSTNAME.ToString()
                                    where (rp.RequestNo == RequestNo)
                                    select new Tbl_Marketing_MatProm_Recipient
                                    {
                                        RecipientName = ax.COSTNAME,
                                        RecipientAddress = ""
                                    }).ToList();
                ViewBag.Recipient = QryRecipient;
            }
            else if (data.FormType == "SalesMarketing")
            {
                // jika menggunakan template
                if (data.useTemplate == 1)
                {
                    var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                        join th in dbmp.Marketing_MatProm_Template_Header on rp.Recipient equals th.ID.ToString()
                                        where (rp.RequestNo == RequestNo)
                                        select new Tbl_Marketing_MatProm_Recipient
                                        {
                                            RecipientName = th.TemplateName,
                                            RecipientAddress = ""
                                        }).ToList();
                    ViewBag.Recipient = QryRecipient;
                }
                // jika tidak menggunakan template
                else
                {
                    var QryRecipientNoTemplate = (from rp in dbmp.Marketing_MatProm_Request_Header

                                                  where (rp.RequestNo == RequestNo)
                                                  select new Tbl_Marketing_MatProm_Recipient
                                                  {
                                                      RecipientName = rp.DistributorID,
                                                      RecipientAddress = ""
                                                  }).ToList();
                    ViewBag.Recipient = QryRecipientNoTemplate;
                }
                // additionalRecipient khusus form type salesMarketing
                var QryadditionalRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                              join am in dbmp.Marketing_MatProm_AddressMaster on rp.Recipient equals am.ID.ToString()
                                              where (rp.RequestNo == RequestNo)
                                              select new Tbl_Marketing_MatProm_Recipient
                                              {
                                                  RecipientName = am.AddressTitle,
                                                  RecipientAddress = am.AddressDetail
                                              }).ToList();
                ViewBag.additionalRecipient = QryadditionalRecipient;
            }
            ViewBag.header = data;
            // kondisi jika status signed ( waiting assignment) maka ambil data assignment user dari 
            if (data.Status == "Signed")
            {
                // ambil data assignment user dari table request_approval
                var approval = dbmp.Marketing_MatProm_Request_Approval.Where(w => w.RequestNo == RequestNo).ToList();
                ViewBag.approval = approval;
            }
            ViewBag.currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            ViewBag.RequestNo = RequestNo;
            var countNotReady = dbmp.V_Marketing_MatProm_Request_Detail.Count(w => w.RequestNo == RequestNo);
            ViewBag.NotReady = countNotReady;
            //ViewBag.NavHide = true;

            return View();
        }
        [HttpPost]
        public ActionResult DetailItemListRequest(string RequestNo, string Status)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var spl = dbmp.V_Marketing_MatProm_Request_Detail.Where(w => w.RequestNo == RequestNo).ToList();
            var CountRow = dbmp.V_Marketing_MatProm_Request_Detail.Count(w => w.RequestNo == RequestNo);
            var request = dbmp.V_Marketing_MatProm_Request.Where(w => w.RequestNo == RequestNo).FirstOrDefault();

            List<Tbl_Marketing_MatProm_ItemList_Request> actions = new List<Tbl_Marketing_MatProm_ItemList_Request>();

            var countNotReady = dbmp.V_Marketing_MatProm_Request_Detail.Count(w => w.RequestNo == RequestNo && w.IsReady == 0);

            foreach (var Item in spl)
            {
                var Tools = "";
                if (Status == "WaitingSign" && request.UserRequest == ((ClaimsIdentity)User.Identity).GetUserId())
                {
                    Tools = "<a href=\"#EditQuantityModal\" data-toggle=\"modal\" title=\"Edit\" id=\"EditQtyItem\" data-itemid=\"" + Item.ITEMID + "\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning EditQtyItem\"><i class=\"fa fa-pencil\"></i></a> <a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";

                }

                if (Status == "Approved" && request.VerifiedBy == ((ClaimsIdentity)User.Identity).GetUserId())
                {
                    if (Item.IsReady == 0)
                    {
                        Tools = "<a href=\"#\" title=\"ready\" id=\"procItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-primary procItem\"><i class=\"fa fa-check\"></i></a> <a href=\"#EditQuantityModal\" data-toggle=\"modal\" title=\"Edit\" id=\"EditQtyItem\" data-itemid=\"" + Item.ITEMID + "\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-warning EditQtyItem\"><i class=\"fa fa-pencil\"></i></a> <a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";
                    }
                    else
                    {

                        Tools = "<a href=\"#\" title=\"Cancel\" id=\"CancelReadyItem\"  data-id=\"" + Item.Id + "\" class=\"btn-sm btn-danger CancelReadyItem\"><i class=\"fa fa-times\"></i></a>";
                    }
                }
                else
                {

                }

                actions.Add(
                    new Tbl_Marketing_MatProm_ItemList_Request
                    {
                        ITEMID = Item.ITEMID,
                        ProductName = Item.ProductName,
                        Weight = (Item.Weight / 1000) * Item.Quantity,
                        Quantity = Item.Quantity,
                        Qty_Realization = Item.Qty_Realization,
                        RequestNotes = Item.RequestNotes,
                        IsChangeQty = Item.IsChangeQty,
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
        [HttpPost]
        public ActionResult ApprovalProcess(string requestNo, string ProcessAction)
        {
            // get user identity
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var EmailAddress = CurrUser.Email;

            string Status = "";
            string TaskName = "";
            string TaskForUser = "";

            var EmailSendTo = new ArrayList();

            // get request header data by request number
            var Request = dbmp.V_Marketing_MatProm_Request.Where(w => w.RequestNo == requestNo).FirstOrDefault();
            DateTime today = DateTime.Now;
            var userCreator = Request.UserRequest;
            var userCreatorName = Request.UserRequestName;

            // set condition if this approval process for sign by user creator (signed step)
            if (ProcessAction == "Sign")
            {
                // cek apakah akcnowledgment statusnya bernilai 2 (kalau 2 sebelumya di reject oleh manager marketing)
                int acknowdgmentStatus = Request.AcknowledgeStatus;
                if (acknowdgmentStatus == 2)
                {
                    Request.ApprovedStatus = 0;
                    // set taskname for task list table
                    TaskName = "Approval MatProm Request";
                    // set request header status
                    Status = "Acknowledgment";

                }
                else
                {

                    TaskForUser = "";

                    Request.AcknowledgeStatus = 0;
                    //Request.AcknowledgeBy = TaskForUser;
                    // set taskname for task list table
                    TaskName = "Acknowledgement MatProm Request";
                    // set request header status
                    Status = "Signed";

                }

                // get section of user creator
                var qGetuser = db.V_Users_Active.Where(w => w.NIK == userCreator).FirstOrDefault();
                var signedSection = qGetuser.CostName;
                // get user next level (manager or ast. mngr) for acknowledgment step base on signedSection
                var qAkcnowledgeUser = db.Master_Organization.Where(w => w.OrganizationName == signedSection).ToList();
                List<Marketing_MatProm_Request_Approval> userApproval = new List<Marketing_MatProm_Request_Approval>();
                var newTasks = new List<Task_Approval_List>();
                foreach (var ListUser in qAkcnowledgeUser)
                {
                    dbmp.Marketing_MatProm_Request_Approval.Add(
                        new Marketing_MatProm_Request_Approval
                        {
                            RequestNo = requestNo,
                            ApprovalUser = ListUser.OrganizationUser,
                            ApprovalType = "Acknowledgment",
                            Status = 0
                        });

                    EmailSendTo.Add(ListUser.OrganizationUser);

                    // Collect global approval task for each acknowledging manager
                    newTasks.Add(new Task_Approval_List
                    {
                        NIK = ListUser.OrganizationUser,
                        Menu_Id = 121,
                        Module = "Material Promotion",
                        Referal_id = requestNo,
                        DetailUrl = "~/Marketing/MatProm/DetailRequest?RequestNo=" + requestNo,
                        TaskType = "Acknowledge",
                        ApprovalDeadline = today.AddDays(1),
                        TaskTitle = "Acknowledgement MatProm Request",
                        DocumentID = null
                    });
                }
                
                if (newTasks.Any())
                {
                    ApprovalHelper.SaveApprovalList(newTasks);
                }

                // define the fields that must be updated in the request header table
                Request.SignTime = today;
                Request.SignBy = CurrUser.NIK;
                Request.Status = Status;

            }
            // set condition if this approval process for acknowledment by user manager (acknowledgment step)
            if (ProcessAction == "Acknowledgment")
            {
                // set request header status
                Status = "Acknowledgment";
                // set taskname for task list table
                TaskName = "Approval MatProm Request";

                // get marketing manager user for next step (approval)
                var signedSection = "MARKETING";
                var qApprovalUser = db.Master_Organization.Where(w => w.OrganizationName == signedSection && w.userLevel == 1).FirstOrDefault();
                TaskForUser = qApprovalUser.OrganizationUser;

                // define the fields that must be updated in the request header table
                Request.ApprovedBy = TaskForUser;
                Request.AcknowledgeTime = today;
                Request.AcknowledgeBy = CurrUser.NIK;
                Request.AcknowledgeStatus = 2;
                Request.ApprovedStatus = 0;
                Request.Status = Status;

                EmailSendTo.Add(TaskForUser);

            }
            // set condition if this approval process for approval by marketing manager (approval step)
            if (ProcessAction == "Approve")
            {
                // set condition if Ischangeqty = 1 (any qty change before)
                if (Request.IsChangeQty == 1)
                {
                    // update all quantity request for each item that have condition isChangeQty  = 1
                    var itemChangeQty = dbmp.Marketing_MatProm_Request_Detail.Where(w => w.IsChangeQty == 1 && w.RequestNo == requestNo).ToList();
                    foreach (var itemChange in itemChangeQty)
                    {
                        // set quantity request = quantity update temp
                        itemChange.Quantity = itemChange.Qty_Change_Temp;
                        // update field isChangeQty to 0
                        itemChange.IsChangeQty = 0;
                    }
                }
                // set condition if isChangeQty = 0 (no change Qty before)
                else
                {

                }
                // set request header status
                Status = "Approved";
                // set taskname for task list table
                TaskName = "Verified MatProm Request";
                // get verified user for next step (verified)
                TaskForUser = "871.01.24";

                // define the fields that must be updated in the request header table
                Request.VerifiedBy = TaskForUser;
                Request.ApprovedTime = today;
                Request.ApprovedBy = CurrUser.NIK;
                Request.ApprovedStatus = 2;
                Request.VerifiedStatus = 0;
                Request.Status = Status;
                Request.IsChangeQty = 0;

                // update IsChangeQty on request detail to be 0 value
                var reqDetail = dbmp.Marketing_MatProm_Request_Detail.Where(w => w.RequestNo == Request.RequestNo).ToList();
                reqDetail.ForEach(w => w.IsChangeQty = 0);

                EmailSendTo.Add(TaskForUser);

            }


            var save = dbmp.SaveChanges();
            // jika proses berhasil
            if (save > 0)
            {
                // Complete global approval task for the group/phase
                string completeTaskType = "Sign";
                if (ProcessAction == "Acknowledgment") completeTaskType = "Acknowledge";
                else if (ProcessAction == "Approve") completeTaskType = "Approval";

                ApprovalHelper.CompleteApprovalTaskForGroup(121, requestNo, completeTaskType);

                // disable tasklist user pada data previous task
                int countTask = db.Task_List.Where(w => w.ModuleID == requestNo && w.TaskForUser == CurrUser.NIK && w.ModuleParameter == "RequestNo").Count();
                if (countTask > 0)
                {
                    var taskDisable = db.Task_List.Where(w => w.ModuleID == requestNo && w.TaskForUser == CurrUser.NIK && w.ModuleParameter == "RequestNo").FirstOrDefault();
                    taskDisable.IsActive = 0;
                    db.SaveChanges();
                }

                // send email
                SendEmail(requestNo, EmailSendTo, Status, userCreatorName);

                // insert task list for next step user
                Task_List task = new Task_List();
                task.TaskName = TaskName;
                task.TaskForUser = TaskForUser;
                task.ModuleArea = "Marketing";
                task.ModuleController = "MatProm";
                task.Module = "DetailRequest";
                task.IsActive = 1;
                task.ModuleID = requestNo;
                task.ModuleParameter = "RequestNo";

                db.Task_List.Add(task);
                db.SaveChanges();

                // Save global approval task for next step user
                if (!string.IsNullOrEmpty(TaskForUser))
                {
                    string tType = TaskName.Contains("Acknowledge") ? "Acknowledge" :
                                   TaskName.Contains("Verify") ? "Verify" : "Approval";
                    ApprovalHelper.SaveSingleApproval(
                        nik: TaskForUser,
                        menuId: 121,
                        module: "Material Promotion",
                        referralId: requestNo,
                        deadline: today.AddDays(1),
                        taskTitle: TaskName,
                        documentId: null,
                        detailUrl: "~/Marketing/MatProm/DetailRequest?RequestNo=" + requestNo,
                        taskType: tType
                    );
                }
                return Json(new
                {
                    status = '1',
                    msg = "Request Signed",
                    //countTask = countTask,
                    emailSendTo = EmailSendTo
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to Sign"
                });
            }

        }

        [HttpPost]
        public ActionResult RejectProcess(string requestNo, string ProcessAction, string RejectNotes)
        {
            // get user identity
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var EmailAddress = CurrUser.Email;

            string Status = "";
            string TaskName = "";
            string TaskForUser = "";

            // get data request header by request Number
            var Request = dbmp.Marketing_MatProm_Request_Header.Where(w => w.RequestNo == requestNo).FirstOrDefault();
            // declare today datetime
            DateTime today = DateTime.Now;
            // get user session actor
            var userCreator = Request.UserRequest;

            // set condition if this reject process for sign by manager (acknowledgment step)
            if (ProcessAction == "Acknowledgment")
            {
                Status = "WaitingSign";
                TaskName = "Waiting ToSign";
                // get approval user
                var signedSection = "MARKETING";
                var qApprovalUser = db.Master_Organization.Where(w => w.OrganizationName == signedSection).FirstOrDefault();
                TaskForUser = qApprovalUser.OrganizationUser;

                // define request header update field
                Request.SignBy = TaskForUser;
                Request.IsReject = 1;
                Request.Status = Status;
                Request.AcknowledgeTime = today;
                Request.AcknowledgeStatus = 1;
                Request.AcknowledgeNotes = RejectNotes;

                // define for insert process task
                Marketing_MatProm_Process_Task proc = new Marketing_MatProm_Process_Task();
                proc.AssignedBy = CurrUser.NIK;
                proc.AssignedTime = today;
                proc.RequestNo = requestNo;
                proc.IsReject = 1;
                proc.Notes = RejectNotes;
                proc.TaskDescription = "Reject Acknowledgment";

                dbmp.Marketing_MatProm_Process_Task.Add(proc);
                //dbmp.SaveChanges();
            }
            // // set condition if this reject process for sign by marketing manager (approval step)
            if (ProcessAction == "Approve")
            {
                // get isChangeQty Condition
                var isChangeQty = Request.IsChangeQty;

                //condition if isChangeQty = 1 (any quantity change previously by marketing checker)
                if (isChangeQty == 1)
                {
                    // update isChangeQty value to 0, and update Qty_Change_temp value to 0
                    var ItemListRequest = dbmp.Marketing_MatProm_Request_Detail.Where(w => w.RequestNo == requestNo).ToList();
                    foreach (var itemReq in ItemListRequest)
                    {
                        itemReq.Qty_Change_Temp = 0;
                        itemReq.IsChangeQty = 0;
                    }

                    // go to next step (verified)
                    Status = "Approved";
                    TaskName = "Verified MatProm Request";
                    // get verified user
                    TaskForUser = "871.01.24";

                    // define request header update field
                    Request.VerifiedBy = TaskForUser;
                    Request.ApprovedTime = today;
                    Request.ApprovedBy = CurrUser.NIK;
                    Request.ApprovedStatus = 2;
                    Request.VerifiedStatus = 0;
                    Request.Status = Status;
                    Request.IsChangeQty = 0;

                    // update IsChangeQty on request detail to be 0 value
                    var reqDetail = dbmp.Marketing_MatProm_Request_Detail.Where(w => w.RequestNo == Request.RequestNo).ToList();
                    reqDetail.ForEach(w => w.IsChangeQty = 0);

                    //dbmp.SaveChanges();
                }
                else
                {
                    /* back to acknowledgment */
                    Status = "WaitingSign";
                    TaskName = "Acknowledgment Request";

                    // define request header update field
                    Request.ApprovedTime = today;
                    Request.ApprovedBy = CurrUser.NIK;
                    Request.ApprovedStatus = 1;
                    Request.AcknowledgeStatus = 2;
                    Request.Status = Status;
                    Request.ApprovedNotes = RejectNotes;

                    // define for insert process task
                    Marketing_MatProm_Process_Task proc = new Marketing_MatProm_Process_Task();
                    proc.AssignedBy = CurrUser.NIK;
                    proc.AssignedTime = today;
                    proc.RequestNo = requestNo;
                    proc.IsReject = 1;
                    proc.Notes = RejectNotes;
                    proc.TaskDescription = "Reject Approval";

                    //dbmp.Marketing_MatProm_Process_Task.Add(proc);
                }
            }
            // set condition if this reject process for verified by marketing checker (verified step)
            if (ProcessAction == "Verified")
            {
                /* back to acknowledgment */
                Status = "Acknowledgment";
                TaskName = "Approval Request";

                // define request header update field
                Request.VerifiedTime = today;
                Request.VerifiedBy = CurrUser.NIK;
                Request.VerifiedStatus = 1;
                Request.ApprovedStatus = 0;
                Request.Status = Status;
                Request.VerifiedNotes = RejectNotes;

                // define for insert process task
                Marketing_MatProm_Process_Task proc = new Marketing_MatProm_Process_Task();
                proc.AssignedBy = CurrUser.NIK;
                proc.AssignedTime = today;
                proc.RequestNo = requestNo;
                proc.IsReject = 1;
                proc.Notes = RejectNotes;
                proc.TaskDescription = "Reject Verified";

                dbmp.Marketing_MatProm_Process_Task.Add(proc);
                //dbmp.SaveChanges();
            }


            var save = dbmp.SaveChanges();

            if (save > 0)
            {
                // Complete global approval task for the group/phase
                string completeTaskType = "Approval";
                if (ProcessAction == "Acknowledgment") completeTaskType = "Acknowledge";
                else if (ProcessAction == "Verified") completeTaskType = "Verify";

                ApprovalHelper.CompleteApprovalTaskForGroup(121, requestNo, completeTaskType);

                //disable tasklist user previous task
                int countTask = db.Task_List.Where(w => w.ModuleID == requestNo && w.TaskForUser == CurrUser.NIK && w.ModuleParameter == "RequestNo").Count();
                if (countTask > 0)
                {
                    var taskDisable = db.Task_List.Where(w => w.ModuleID == requestNo && w.TaskForUser == CurrUser.NIK && w.ModuleParameter == "RequestNo").FirstOrDefault();
                    taskDisable.IsActive = 0;
                    //db.SaveChanges();
                }

                // insert task list for next step user
                Task_List task = new Task_List();
                task.TaskName = TaskName;
                task.TaskForUser = TaskForUser;
                task.ModuleArea = "Marketing";
                task.ModuleController = "MatProm";
                task.Module = "DetailRequest";
                task.IsActive = 1;
                task.ModuleID = requestNo;
                task.ModuleParameter = "RequestNo";

                db.Task_List.Add(task);
                db.SaveChanges();

                // Save global approval task for next step user
                if (!string.IsNullOrEmpty(TaskForUser))
                {
                    string tType = TaskName.Contains("Acknowledge") ? "Acknowledge" :
                                   TaskName.Contains("Verify") ? "Verify" : "Approval";
                    ApprovalHelper.SaveSingleApproval(
                        nik: TaskForUser,
                        menuId: 121,
                        module: "Material Promotion",
                        referralId: requestNo,
                        deadline: today.AddDays(1),
                        taskTitle: TaskName,
                        documentId: null,
                        detailUrl: "~/Marketing/MatProm/DetailRequest?RequestNo=" + requestNo,
                        taskType: tType
                    );
                }
                return Json(new
                {
                    status = '1',
                    msg = "Request Signed",
                    save = save
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to Sign",
                    save = save
                });
            }

        }

        public ActionResult TemplateData()
        {
            var spl = dbmp.Marketing_MatProm_Template_Header.Where(w => w.IsActive == 1).ToList();
            ViewBag.Template = spl;
            return View();
        }
        [HttpPost]
        public ActionResult UpdateTemplate(Marketing_MatProm_Template_Header smodel)
        {
            var spl = dbmp.Marketing_MatProm_Template_Header.Where(w => w.ID == smodel.ID).FirstOrDefault();
            spl.TemplateName = smodel.TemplateName;
            spl.IsActive = smodel.IsActive;

            int save = dbmp.SaveChanges();
            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "update success",
                    save = save,
   
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to update",
                    save = save
                });
            }
        }
        [HttpPost]
        public ActionResult DetailTemplateData(string templateID)
        {

            var spl = dbmp.Marketing_MatProm_Template_Detail.Where(w => w.TemplateID.ToString() == templateID).ToList();
            var CountRow = dbmp.Marketing_MatProm_Template_Detail.Count(w => w.TemplateID.ToString() == templateID);

            List<Tbl_Marketing_MatProm_Template_Detail> actions = new List<Tbl_Marketing_MatProm_Template_Detail>();

            int no = 0;
            foreach (var Item in spl)
            {
                no++;
                var Tools = "";

                var getItem = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == Item.ITEMID).FirstOrDefault();

                Tools = "<a href=\"#\" title=\"Delete\" id=\"DeleteItem\"  data-id=\"" + Item.ID + "\" class=\"btn-sm btn-danger DeleteItem\"><i class=\"fa fa-trash\"></i></a>";

                actions.Add(
                    new Tbl_Marketing_MatProm_Template_Detail()
                    {
                        ITEMID = Item.ITEMID,
                        ItemName = getItem.ProductName,
                        Tools = Tools,
                        No = no
                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
                status = 1
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult EditQuantityItemOpen(int DetailId)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            List<Marketing_MatProm_Request_Detail> marketing_MatProm_Request_Detail = dbmp.Marketing_MatProm_Request_Detail.ToList();
            List<V_Marketing_MatProm_ItemMaster> v_Marketing_MatProm_ItemMaster = dbmp.V_Marketing_MatProm_ItemMaster.ToList();

            var item = (
                from ai in marketing_MatProm_Request_Detail
                join al in v_Marketing_MatProm_ItemMaster on ai.ITEMID equals al.ITEMID
                where (ai.Id == DetailId)
                select new Tbl_Marketing_Matprom_Request
                {
                    ITEMID = ai.ITEMID,
                    ProductName = al.ProductName,
                    Quantity = ai.Quantity,
                    Stock = al.Stock,
                    Id = ai.Id
                }).ToList();

            return Json(item, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult EditQuantityItemOpen(Marketing_MatProm_Request_Detail smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            string showConfirmChangeButton;

            // get data detail item
            var data = dbmp.Marketing_MatProm_Request_Detail.FirstOrDefault(x => x.Id == smodel.Id);
            // get data header
            var dataHeader = dbmp.Marketing_MatProm_Request_Header.Where(w => w.RequestNo == data.RequestNo).FirstOrDefault();

            if(dataHeader.Status == "WaitingSign")
            {
                data.Quantity = smodel.Qty_Change_Temp;
                showConfirmChangeButton = "0";
            } else
            {
                if (smodel.Qty_Change_Temp < data.Quantity)
                {
                    // jika qty nya dikurangi
                    data.Quantity = smodel.Qty_Change_Temp;
                    showConfirmChangeButton = "0";
                }
                else
                {
                    // jika qty nya ditambahkan
                    data.IsChangeQty = 1;
                    data.Qty_Change_Temp = smodel.Qty_Change_Temp;
                    dataHeader.IsChangeQty = 1;
                    showConfirmChangeButton = "1";
                }
            }
            // cek apakah quantity updatenya dikurangi atau di tambahkan
            

            var update = dbmp.SaveChanges();
            if (update >= 1)
            {
                return Json(new
                {
                    status = "1",
                    msg = "Update Quntity Success",
                    showButton = showConfirmChangeButton
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
        [HttpPost]
        public ActionResult ReadyItem(int Id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var DetailItem = dbmp.Marketing_MatProm_Request_Detail.Where(w => w.Id == Id).FirstOrDefault();

            var Qstock = dbmp.V_Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == DetailItem.ITEMID).FirstOrDefault();
            if (Qstock.Stock < DetailItem.Quantity)
            {
                return Json(new
                {
                    status = 0,
                    msg = "item not enought stock",
                    displayData = "prepared"
                });

            }
            else
            {
                DetailItem.IsReady = 1;
                DetailItem.Qty_Realization = DetailItem.Quantity;

                var save = dbmp.SaveChanges();

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
            }

        }
        [HttpPost]
        public ActionResult ConfirmChangeQty(string requestNo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            string Status = "";
            //string TaskName = "";
            //string TaskForUser = "";

            var ConfirmChangeNotes = Request.Form.Get("ChangeNotes");

            var Req = dbmp.Marketing_MatProm_Request_Header.Where(w => w.RequestNo == requestNo).FirstOrDefault();
            DateTime today = DateTime.Now;

            /* back to acknowledgment */
            Status = "Acknowledgment";
            //string TaskName = "Approval Request";

            // define request header update field
            Req.VerifiedTime = today;
            Req.VerifiedBy = CurrUser.NIK;
            Req.VerifiedStatus = 3;
            Req.VerifiedNotes = ConfirmChangeNotes;
            Req.ApprovedStatus = 0;
            Req.Status = Status;

            //define for insert process task

            Marketing_MatProm_Process_Task proc = new Marketing_MatProm_Process_Task();
            proc.AssignedBy = CurrUser.NIK;
            proc.AssignedTime = today;
            proc.RequestNo = requestNo;
            proc.IsReject = 0;
            proc.Notes = ConfirmChangeNotes;
            proc.TaskDescription = "Confirm Change Quantity";

            dbmp.Marketing_MatProm_Process_Task.Add(proc);

            var save = dbmp.SaveChanges();

            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Confirm Change Has Been Send"
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to Confirm Change"
                });
            }

        }

        [HttpPost]
        public ActionResult ConfirmVerified(string requestNo, string sendTo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var EmailAddress = CurrUser.Email;

            var Request = dbmp.Marketing_MatProm_Request_Header.Where(w => w.RequestNo == requestNo).FirstOrDefault();
            DateTime today = DateTime.Now;
            Request.Status = "Verified";
            Request.VerifiedTime = today;
            Request.VerifiedBy = CurrUser.NIK;
            Request.VerifiedStatus = 2;

            //Request.CloseTimeDueDate = today.AddDays(3);

            var save = dbmp.SaveChanges();

            if (save > 0)
            {
                // Complete global verification task for the group/phase
                ApprovalHelper.CompleteApprovalTaskForGroup(121, requestNo, "Approval");
                //if (EmailAddress != null)
                //{
                //    string FilePath = Path.Combine(Server.MapPath("~/Emails/SCM/Sparepart/"), "Test.html");
                //    StreamReader str = new StreamReader(FilePath);
                //    string MailText = str.ReadToEnd();
                //    str.Close();

                //    var act = Url.Action("DetailRequest", "Sparepart", new { area = "SCM", RequestNo = requestNo });
                //    //Repalce [newusername] = signup user name   
                //    MailText = MailText.Replace("##UserName##", requestNo);
                //    MailText = MailText.Replace("##Dept##", "Information Technology");
                //    MailText = MailText.Replace("##Url##", act);

                //    var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Sparepart Apps");
                //    var receiverEmail = new MailAddress(EmailAddress, "Receiver");
                //    var password = "100%NGKbusi!";
                //    var sub = "[no-reply] Sparepart Apps";
                //    var body = MailText;
                //    var smtp = new SmtpClient
                //    {
                //        Host = "ngkbusi.com",
                //        Port = 587,
                //        EnableSsl = false,
                //        DeliveryMethod = SmtpDeliveryMethod.Network,
                //        UseDefaultCredentials = false,

                //        Credentials = new NetworkCredential(senderEmail.Address, password)
                //    };
                //    using (var mess = new MailMessage(senderEmail, receiverEmail)
                //    {
                //        Subject = sub,
                //        Body = body,
                //        IsBodyHtml = true
                //    })
                //    {
                //        smtp.Send(mess);
                //    }
                //}
                return Json(new
                {
                    status = '1',
                    msg = "Request Ready To Pickup"
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to  verivied request"
                });
            }

        }

        [HttpPost]
        public ActionResult DeleteItemRequest(int Id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            try
            {
                var data = dbmp.Marketing_MatProm_Request_Detail.Where(w => w.Id == Id).FirstOrDefault();
                dbmp.Marketing_MatProm_Request_Detail.Remove(data);
                var del = dbmp.SaveChanges();
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

        public ActionResult CancelRequest(string requestNo)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var updateDataHeader = dbmp.Marketing_MatProm_Request_Header.Where(w => w.RequestNo == requestNo).ToList();
            foreach (var data in updateDataHeader)
            {
                data.Status = "Cancelled";
                data.CancelTime = DateTime.UtcNow.Date;
                data.IsCancel = 1;
                data.CancelBy = CurrUser.NIK;
            }

            var updateHeader = dbmp.SaveChanges();

            var getDetail = dbmp.Marketing_MatProm_Request_Detail.Where(w => w.RequestNo == requestNo).ToList();

            foreach (var item in getDetail)
            {

                item.IsCancel = 1;
                //Change a filed's value
            }

            var updateDetail = dbmp.SaveChanges();

            if (updateHeader > 0 && updateDetail > 0)
            {
                // Cancel all pending global approval tasks for this request
                ApprovalHelper.CancelApprovalTasks(121, requestNo);

                return Json(new { status = 1, msg = "Request Cancel" });
            }
            else
            {
                return Json(new { status = 0, msg = "Failed Cancel Request" });
            }
        }

        public void SendEmail(string requestNo, ArrayList emailSendTo, string Status, string requestByName)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();


            string FilePath = Path.Combine(Server.MapPath("~/Emails/Marketing/MatProm/"), "notif.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();

            var act = Url.Action("DetailRequest", "MatProm", new { area = "Marketing", RequestNo = requestNo }, this.Request.Url.Scheme);
            //Repalce [newusername] = signup user name      
            MailText = MailText.Replace("##UserName##", requestNo);
            MailText = MailText.Replace("##Url##", act);
            MailText = MailText.Replace("##Status##", Status);
            MailText = MailText.Replace("##requestByName##", requestByName);

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "MatProm Notification");
            //var receiverEmail = new MailAddress(EmailAddress, "Receiver");
            var password = "100%NGKbusi!";
            var sub = "MatProm Apps Notification";
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

                foreach (string dataEmail in emailSendTo)
                {
                    if (dataEmail.Length > 0)
                    {
                        var CurrUser = db.V_Users_Active.Where(w => w.NIK == dataEmail).First();
                        var EmailAddress = CurrUser.Email;

                        mess.To.Add(new MailAddress(EmailAddress));

                    }

                }
                mess.Bcc.Add(new MailAddress("ikhsan.sholihin@ngkbusi.com"));
                smtp.Send(mess);
            }


        }
        [Authorize]
        public ActionResult Report()
        {
            //var spl = dbsp.V_SCM_Sparepart_Master_List.Where(w => w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling");
            //var query = spl.GroupBy(row => new { row.ProCateName, row.ProductCategory }).Select(group => new { ProCateName = group.Key.ProCateName, ProductCategory = group.Key.ProductCategory }).ToList();
            //var section = db.Users_Section_AX.ToList();

            //List<V_SCM_Sparepart_Master_List> par = new List<V_SCM_Sparepart_Master_List>();

            //foreach (var data in query)
            //{
            //    par.Add(new V_SCM_Sparepart_Master_List
            //    {
            //        ProCateName = data.ProCateName,
            //        ProductCategory = data.ProductCategory
            //    });
            //}

            //ViewBag.Section = section;
            //ViewBag.ItemGroup = par;
            //ViewBag.RackList = dbsp.SCM_Sparepart_Rack.Where(w => w.IsDelete == 0).ToList();

            return View();
        }

        [HttpPost]
        public ActionResult UpdateActivation(string ITEMID, string action)
        {
            var item = dbmp.Marketing_MatProm_ItemMaster.Where(w => w.ITEMID == ITEMID).FirstOrDefault();

            if (action == "Deactivate")
            {
                item.IsActive = 0;
            }
            else
            {               
                item.IsActive = 1;
            }
            int i = dbmp.SaveChanges();
            if (i > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = "Update Success"
                });
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Update Failed"
                });
            }
        }


        public ActionResult GenerateReport()
        {
            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            List<Tbl_Marketing_MatProm_Report> k3list = new List<Tbl_Marketing_MatProm_Report>();

            SqlCommand cmd = new SqlCommand("sp_Marketing_MatProm_Generate_Report", con);
            cmd.CommandType = CommandType.StoredProcedure;

            var reportTitle = "";
            //var StrDateStart = Convert.ToDateTime(smodel.dateFrom).ToString("dd MMM yyyy");
            //var StrDateTo = Convert.ToDateTime(smodel.dateTo).ToString("dd MMM yyyy");
            var dateFrom = Request.Form.Get("dateFrom");
            var dateTo = Request.Form.Get("dateTo");
            var status = Request.Form.Get("status");

            // date from 
            if (dateFrom != null || dateFrom != "")
            {

                cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
            }
            else
            {
                DateTime todayDate = DateTime.UtcNow.Date.AddMonths(-1);
                cmd.Parameters.AddWithValue("@dateFrom", todayDate);
            }
            // date to
            if (dateTo != null || dateTo != "")
            {
                cmd.Parameters.AddWithValue("@dateTo", dateTo);
            }
            else
            {
                DateTime date2 = DateTime.UtcNow.Date;
                cmd.Parameters.AddWithValue("@dateTo", date2);
            }

            cmd.Parameters.AddWithValue("@Status", status);
            //cmd.Parameters.AddWithValue("@ProductCategory", smodel.ProductCategory);
            //cmd.Parameters.AddWithValue("@COSTNAME", smodel.COSTNAME);
            //cmd.Parameters.AddWithValue("@Status", smodel.Status);
            //cmd.Parameters.AddWithValue("@MaintenanceType", smodel.MaintenanceType);

            SqlDataAdapter sd = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            con.Open();
            sd.Fill(dt);
            con.Close();

            //if (smodel.dateFrom.HasValue && smodel.dateTo.HasValue)
            //{
            //    reportTitle = "report periode " + StrDateStart + " - " + StrDateTo;

            //}
            //else
            //{
            //    reportTitle = "report periode last 30 days";
            //}

            foreach (DataRow dr in dt.Rows)
            {
                string RequestNo = Convert.ToString(dr["RequestNo"]);
                // get formtype
                var formType = Convert.ToString(dr["FormType"]);
                // define recipient
                var Recipient = "";
                if (formType == "distributor")
                {
                    var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                        join am in dbmp.Marketing_MatProm_AddressMaster on rp.Recipient equals am.ID.ToString()
                                        where (rp.RequestNo == RequestNo)
                                        select new Tbl_Marketing_MatProm_Recipient
                                        {
                                            RecipientName = am.AddressTitle
                                        }).ToList();
                    Recipient = string.Join(",", QryRecipient.Select(x => x.RecipientName));

                }
                else if (formType == "bengkel-points")
                {
                    var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient.Where(w => w.RequestNo == RequestNo)
                                        select new Tbl_Marketing_MatProm_Recipient
                                        {
                                            RecipientName = rp.Recipient
                                        }).ToList();
                    Recipient = string.Join(",", QryRecipient.Select(x => x.RecipientName));
                }
                else if (formType == "community")
                {
                    var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient.Where(w => w.RequestNo == RequestNo)
                                        select new Tbl_Marketing_MatProm_Recipient
                                        {
                                            RecipientName = rp.Recipient
                                        }).ToList();
                    Recipient = string.Join(",", QryRecipient.Select(x => x.RecipientName));
                }
                else if (formType == "internal")
                {
                    var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                        join ax in dbmp.Users_Section_AX on rp.Recipient equals ax.COSTNAME.ToString()
                                        where (rp.RequestNo == RequestNo)
                                        select new Tbl_Marketing_MatProm_Recipient
                                        {
                                            RecipientName = ax.COSTNAME
                                        }).ToList();
                    Recipient = string.Join(",", QryRecipient.Select(x => x.RecipientName));
                }
                else if (formType == "SalesMarketing")
                {
                    // additionalRecipient khusus form type salesMarketing
                   var QryRecipient = (from rp in dbmp.Marketing_MatProm_Request_Recipient
                                                 join am in dbmp.Marketing_MatProm_AddressMaster on rp.Recipient equals am.ID.ToString()
                                                 where (rp.RequestNo == RequestNo)
                                                 select new Tbl_Marketing_MatProm_Recipient
                                                 {
                                                     RecipientName = am.AddressTitle
                                                 }).ToList();
                    Recipient = string.Join(",", QryRecipient.Select(x => x.RecipientName));
                }

                

                DateTime date = Convert.ToDateTime(dr["Date"]);
                //DateTime? VerifiedDatetime;
                var VerifiedTime = "";
                if (dr["VerifiedTime"] is DBNull)
                {
                    VerifiedTime = "";
                }
                else
                {
                    VerifiedTime = Convert.ToDateTime(dr["VerifiedTime"]).ToString("dd MMMM yyyy");
                }


                k3list.Add(
                    new Tbl_Marketing_MatProm_Report
                    {
                        ITEMID = Convert.ToString(dr["ITEMID"]),
                        Quantity = Convert.ToInt32(dr["Qty_Realization"]),
                        ProductName = Convert.ToString(dr["ProductName"]),
                        Date = date.ToString("dd MMMM yyyy"),
                        Month = Convert.ToString(dr["Month"]),
                        Year = Convert.ToString(dr["Year"]),
                        VerifiedTime = VerifiedTime,
                        Usage = Convert.ToString(dr["Usage"]),
                        Description = Convert.ToString(dr["RequestNotes"]),
                        Recipient = Recipient,
                        RequestNo = RequestNo

                    });
            }

            return Json(new { rows = k3list, title = reportTitle }, JsonRequestBehavior.AllowGet);
        }
    }
}