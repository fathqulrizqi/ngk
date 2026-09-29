using Microsoft.AspNet.Identity;
using NGK_AX.Models;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.SCM.Controllers
{
    public class MITtransferBNGKController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        MITtransferBNGKConnection dbmit = new MITtransferBNGKConnection();
        NGK_AXConnection dbax = new NGK_AXConnection();
        SparepartConnection dbsp = new SparepartConnection();

        private string ConvertDateTimeToString(DateTime dateTime, string format)
        {
            // Mengonversi DateTime ke string dengan format tertentu
           
                return dateTime.ToString(format);
            
            
        }

        // GET: SCM/MITtransferBNGK
        public ActionResult Index()
        {
            ViewBag.NavHide = true;
            return View();
        }

        public ActionResult MITList()
        {

            var spl = dbmit.V_D365_MIT.ToList();
            //var CountRow = dbmit.V_D365_MIT.Where(w=>w.PurchaseType == "Purchase order").ToList();

            List<Tbl_V_D365_MIT> actions = new List<Tbl_V_D365_MIT>();
            var no = 0;
            foreach (var Item in spl)
            {
                string formattedDate = ConvertDateTimeToString(Item.Date, "dd/MM/yyyy");
                no++;
                var UrlAction = Url.Action("ProcessTransfer", "MITtransferBNGK", new { area = "SCM" });
                actions.Add(
                    new Tbl_V_D365_MIT
                    {
                        No = no,
                        ProductReceipt = "<a href=\"#ProductModal\" class=\"SeeProductList\" title=\"see detail item\" data-id=\"" + Item.ProductReceipt + "\">" + Item.ProductReceipt + "</a>",
                        Periode = Item.Periode,
                        InvoiceAccount = Item.InvoiceAccount,
                        VendorName = Item.VendorName,
                        Date = formattedDate,
                        Button = "<a title=\"edit item\" class=\"btn btn-success btnReceive\" href=\"#ReceiveModal\" data-toggle=\"modal\" data-ProductReceipt=\"" + Item.ProductReceipt + "\" data-Periode=\"" + Item.Periode + "\" data-InvoiceAccount=\"" + Item.InvoiceAccount + "\" data-VendorName=\"" + Item.VendorName + "\" data-Date=\"" + Item.Date + "\"><i class=\"fa fa-edit\"></i> Confirm Arrival</a>"
                    }); 
            }

            // get data Arrival not complete dari table T_AXInvoiceTransfer
            //var qArrivalNotComplete = dbmit.T_AXInvoiceTransfer.Where(w => w.ArriveStatus == 0).ToList();
            //foreach (var item in qArrivalNotComplete)
            //{
            //    no++;


            //    // det field Date dari table V_D365_MIT
            //    var q_D365_MIT = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == item.Invoice).FirstOrDefault();
            //    string formattedDate = "";
            //    if (q_D365_MIT == null)
            //    {
            //        formattedDate = "not found";
            //    } else
            //    {
            //        formattedDate = ConvertDateTimeToString(q_D365_MIT.Date, "dd/MM/yyyy");
            //    }

            //    var UrlAction = Url.Action("ProcessTransfer", "MITtransferBNGK", new { area = "SCM" });
            //    //actions.Add(
            //    //    new Tbl_V_D365_MIT
            //    //    {
            //    //        No = no,
            //    //        ProductReceipt = "<a href=\"#ProductModal\" class=\"SeeProductList\" title=\"see detail item\" data-id=\"" + item.Invoice + "\">" + item.Invoice + "</a>",
            //    //        Periode = item.PeriodEDI,
            //    //        InvoiceAccount = item.InvoiceAccount,
            //    //        VendorName = item.PurchName,
            //    //        Date = formattedDate,
            //    //        Button = "<a title=\"edit item\" class=\"btn btn-success btnReceive\" href=\"#ReceiveModal\" data-toggle=\"modal\" data-ProductReceipt=\"" + item.Invoice + "\" data-Periode=\"" + item.PeriodEDI + "\" data-InvoiceAccount=\"" + item.InvoiceAccount + "\" data-VendorName=\"" + item.PurchName + "\" data-Date=\"" + item.DatePhysical + "\"><i class=\"fa fa-edit\"></i> Confirm Arrival</a>"
            //    //    }); 
            //}
            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                data = spl,
                total = spl.Count(),
            }, JsonRequestBehavior.AllowGet); 
        }

        public ActionResult MITListArrived()
        {

            var spl = dbmit.T_AXInvoiceTransfer.Where(w => w.Status == "0" && w.ArriveStatus == 1).ToList();
            var CountRow = dbmit.T_AXInvoiceTransfer.ToList();

            List<Tbl_MIT_Received> actions = new List<Tbl_MIT_Received>();
            var no = 0;
            foreach (var Item in spl)
            {
                var formattedDate = Convert.ToDateTime(Item.DatePhysical).ToString("MM/dd/yyyy"); ;
                no++;
                var UrlAction = Url.Action("ProcessTransfer", "MITtransferBNGK", new { area = "SCM" });
                var arriveByName = "";
                // get received by name
                if (Item.ArriveBy != null)
                {
                    // dapatka informasi nama penerima barang dari table user
                    var qUser = db.V_Users_Active.Where(w => w.NIK == Item.ArriveBy).FirstOrDefault();
                    arriveByName = qUser.Name;
                }
                else
                {
                    arriveByName = "Not Found";
                }
                actions.Add(
                    new Tbl_MIT_Received
                    {
                        No = no,
                        ProductReceipt = "<a href=\"#ProductModal\" class=\"SeeProductList\" title=\"see detail item\" data-id=\"" + Item.Invoice + "\" data-target=\"#ProductModal\" data-toggle=\"modal\">" + Item.Invoice + "</a>",
                        Periode = Item.PeriodEDI,
                        InvoiceAccount = Item.InvoiceAccount,
                        VendorName = Item.PurchName,
                        Date = formattedDate,
                        ReceivedBy = arriveByName,
                        Button = "<a title=\"Receive Document\" class=\"btn btn-primary btnReceiveDocument\" style=\"color:white\"  data-Invoice=\"" + Item.Invoice + "\" data-Periode=\"" + Item.PeriodEDI + "\" data-InvoiceAccount=\"" + Item.InvoiceAccount + "\" data-VendorName=\"" + Item.PurchName + "\" data-Date=\"" + Item.DatePhysical + "\"><i class=\"fa fa-receipt\" style=\"color:white\"></i> Receive Document</a>  <a title=\"Return Process\" class=\"btn btn-danger btnReturnProcess\" style=\"color:white\"  data-Invoice=\"" + Item.Invoice + "\" data-Periode=\"" + Item.PeriodEDI + "\" data-InvoiceAccount=\"" + Item.InvoiceAccount + "\" data-VendorName=\"" + Item.PurchName + "\" data-Date=\"" + Item.DatePhysical + "\"><i class=\"fa fa-undo\" style=\"color:white\"></i> Return Process</a>"
                    }); ;
            }

            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult MITListReceived()
        {

            var spl = dbmit.T_AXInvoiceTransfer.Where(w => w.Status == "1" && w.ArriveBy != null && w.JournalTransferBy == null && w.PostingJournalBy == null).ToList();
            var CountRow = dbmit.T_AXInvoiceTransfer.ToList();

            List<Tbl_MIT_Received> actions = new List<Tbl_MIT_Received>();
            var no = 0;
            foreach (var Item in spl)
            {
                var formattedDate = Convert.ToDateTime(Item.DatePhysical).ToString("MM/dd/yyyy"); ;
                no++;
                var UrlAction = Url.Action("ProcessTransfer", "MITtransferBNGK", new { area = "SCM" });
                var receiveByName = "";
                // get received by name
                if (Item.ReceiveDocumentBy != null)
                {
                    var qUser = db.V_Users_Active.Where(w => w.NIK == Item.ReceiveDocumentBy).FirstOrDefault();
                    receiveByName = qUser.Name;
                }
                else
                {
                    receiveByName = "Not Found";
                }
                actions.Add(
                    new Tbl_MIT_Received
                    {
                        No = no,
                        ProductReceipt = "<a href=\"#ProductModal\" class=\"SeeProductList\" title=\"see product list\" data-id=\"" + Item.Invoice + "\"  data-target=\"#ProductModal\" data-toggle=\"modal\">" + Item.Invoice + "</a>",
                        Periode = Item.PeriodEDI,
                        InvoiceAccount = Item.InvoiceAccount,
                        VendorName = Item.PurchName,
                        Date = formattedDate,
                        ReceivedBy = receiveByName,
                        Button = "<a title=\"Input Jurnal Number\" class=\"btn btn-warning btnJournalTransfer\"  href=\"#JournalTransferModal\" data-toggle=\"modal\" data-ProductReceipt=\"" + Item.Invoice + "\" data-Periode=\"" + Item.PeriodEDI + "\" data-InvoiceAccount=\"" + Item.InvoiceAccount + "\" data-VendorName=\"" + Item.PurchName + "\" data-Date=\"" + Item.DatePhysical + "\"><i class=\"fa fa-random\"></i> Journal Transfer</a>"
                    }); ;
            }

            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult MITListJournalTransfer()
        {

            var spl = dbmit.T_AXInvoiceTransfer.Where(w => w.Status == "1" && w.IsJournal == 1 && w.PostingJournalBy == null).ToList();
            var CountRow = dbmit.T_AXInvoiceTransfer.Where(w => w.Status == "1" && w.IsJournal == 1).ToList();

            List<Tbl_MIT_Received> actions = new List<Tbl_MIT_Received>();
            var no = 0;
            foreach (var Item in spl)
            {
                var formattedDate = Convert.ToDateTime(Item.DatePhysical).ToString("MM/dd/yyyy");
                no++;
                var UrlAction = Url.Action("ProcessTransfer", "MITtransferBNGK", new { area = "SCM" });
                var receiveByName = "";
                // get received by name
                if (Item.ReceiveDocumentBy != null)
                {
                    var qUser = db.V_Users_Active.Where(w => w.NIK == Item.ReceiveDocumentBy).FirstOrDefault();
                    receiveByName = qUser.Name;
                }
                else
                {
                    receiveByName = "Not Found";
                }
                var JournalTransferByName = "";
                var JournalTransferTime = "";
                if (Item.JournalTransferBy != null)
                {
                    var qUser = db.V_Users_Active.Where(w => w.NIK == Item.JournalTransferBy).FirstOrDefault();
                    JournalTransferByName = qUser.Name;
                    JournalTransferTime = Convert.ToDateTime(Item.JournalTransferTime).ToString("MM/dd/yyyy");
                } else
                {
                    JournalTransferByName = "not found";
                    JournalTransferTime = "not found";
                }
                actions.Add(
                    new Tbl_MIT_Received
                    {
                        No = no,
                        ProductReceipt = Item.Invoice,
                        Periode = Item.PeriodEDI,
                        InvoiceAccount = Item.InvoiceAccount,
                        VendorName = Item.PurchName,
                        Date = formattedDate,
                        ReceivedBy = receiveByName,
                        JournalTransferNumber = Item.JournalNumber,
                        JournalTransferBy = JournalTransferByName,
                        JournalTransferTime = JournalTransferTime,
                        Button = "<a title=\"Receive Document\" class=\"btn btn-info btnPostingJournal\" style=\"color:white\"  data-Invoice=\"" + Item.Invoice + "\" data-Periode=\"" + Item.PeriodEDI + "\" data-InvoiceAccount=\"" + Item.InvoiceAccount + "\" data-VendorName=\"" + Item.PurchName + "\" data-Date=\"" + Item.DatePhysical + "\"><i class=\"fa fa-check-square\" style=\"color:white\"></i> Posting Journal</a> "
                    });
            }

            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult MITListPostedJournal()
        {

            var spl = dbmit.T_AXInvoiceTransfer.Where(w => w.Status == "1" && w.IsJournal == 1 && w.IsPostingJournal == 1).ToList();
            var CountRow = dbmit.T_AXInvoiceTransfer.Where(w => w.Status == "1" && w.IsJournal == 1 && w.IsPostingJournal == 1).ToList();

            List<Tbl_MIT_Received> actions = new List<Tbl_MIT_Received>();
            var no = 0;
            foreach (var Item in spl)
            {
                var formattedDate = Convert.ToDateTime(Item.DatePhysical).ToString("MM/dd/yyyy");
                no++;
                var UrlAction = Url.Action("ProcessTransfer", "MITtransferBNGK", new { area = "SCM" });
                var receiveByName = "";
                // get received by name
                if (Item.ReceiveDocumentBy != null)
                {
                    var qUser = db.V_Users_Active.Where(w => w.NIK == Item.ReceiveDocumentBy).FirstOrDefault();
                    receiveByName = qUser.Name;
                }
                else
                {
                    receiveByName = "Not Found";
                }
                var JournalTransferByName = "";
                var JournalTransferTime = "";
                if (Item.JournalTransferBy != null)
                {
                    var qUser = db.V_Users_Active.Where(w => w.NIK == Item.JournalTransferBy).FirstOrDefault();
                    JournalTransferByName = qUser.Name;
                    JournalTransferTime = Convert.ToDateTime(Item.JournalTransferTime).ToString("MM/dd/yyyy");
                }
                else
                {
                    JournalTransferByName = "not found";
                    JournalTransferTime = "not found";
                }
                var PostingJournalByName = "";
                var PostingJournalTime = "";
                if(Item.PostingJournalBy != null)
                {
                    var qUser = db.V_Users_Active.Where(w => w.NIK == Item.PostingJournalBy).FirstOrDefault();
                    PostingJournalByName = qUser.Name;
                    PostingJournalTime = Convert.ToDateTime(Item.JournalTransferTime).ToString("MM/dd/yyyy");
                } else
                {
                    PostingJournalByName = "not found";
                    PostingJournalTime = "not found";
                }
                actions.Add(
                    new Tbl_MIT_Received
                    {
                        No = no,
                        ProductReceipt = Item.Invoice,
                        Periode = Item.PeriodEDI,
                        InvoiceAccount = Item.InvoiceAccount,
                        VendorName = Item.PurchName,
                        Date = formattedDate,
                        ReceivedBy = receiveByName,
                        JournalTransferNumber = Item.JournalNumber,
                        JournalTransferBy = JournalTransferByName,
                        JournalTransferTime = JournalTransferTime,
                        PostingJournalBy = PostingJournalByName,
                        PostingJournalTime = PostingJournalTime
                    });
            }

            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ProductList(string InvoiceId)
        {
            var spl = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == InvoiceId).ToList();
            var CountRow = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == InvoiceId).ToList();

            List<Tbl_ProductList> actions = new List<Tbl_ProductList>();
            var no = 0;
            foreach (var Item in spl)
            {
                no++;
                actions.Add(
                    new Tbl_ProductList
                    {
                        No = no,
                        ITEMID = Item.Item,
                        ProductName = Item.ProductName,
                        ReceivedQuantity = Item.ReceivedQuantity
                    });
            }

            return Json(new
            {
                rows = actions,
                //totalNotFiltered = query,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ConfirmArrival(T_AXInvoiceTransfer smodel, string DateReceive, string confirmArrivalType)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            // send email notification to Purchasing Admin
            var EmailSendTo = new ArrayList();

            // cek apakah invoice id available di table T_AXInvoiceTransfer
            int qCheckInvoice = dbmit.T_AXInvoiceTransfer.Where(w => w.Invoice == smodel.Invoice).Count();
           
            try
            {

                if (confirmArrivalType == "complete")
                {
                    smodel.ArriveStatus = 1;
                }
                else
                {
                    smodel.ArriveStatus = 0;
                }

                var a = 0;
                var ins = 0;
                if (qCheckInvoice > 0)
                {
                    // update T_AXInvoiceTransfer data only
                    var itemInvoice = dbmit.T_AXInvoiceTransfer.Where(w => w.Invoice == smodel.Invoice).FirstOrDefault();
                    itemInvoice.ArriveBy = CurrUser.NIK;
                    itemInvoice.ArriveTime = Convert.ToDateTime(DateReceive);
                    itemInvoice.ArriveStatus = smodel.ArriveStatus;
                    a = 1;
                     ins = dbmit.SaveChanges();
                }
                else
                {
                    smodel.ArriveBy = CurrUser.NIK;
                    smodel.ArriveTime = Convert.ToDateTime(DateReceive);

                    smodel.Status = "0";
                    smodel.DatePhysical = Convert.ToDateTime(DateReceive);

                    a = 2;
                    // insert data ke T_AXInvoiceTransfer
                    dbmit.T_AXInvoiceTransfer.Add(smodel);
                    ins = dbmit.SaveChanges();
                }                           

                // jika berhasil save dan confirm type complete, input stok ke portal untuk yang machineP dan tooling
                if (ins > 0 && confirmArrivalType == "complete")
                {
                    // insert to sparepart received if itemgroup in (tooling,machinep)
                    var qItemArrival = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == smodel.Invoice).ToList();
                    foreach(var itemArrival in qItemArrival)
                    {
                        SCM_Sparepart_Product_Received item = new SCM_Sparepart_Product_Received();

                        item.ProductReceipt = itemArrival.ProductReceipt;
                        item.QuantityReceived = Convert.ToInt32(itemArrival.ReceivedQuantity);
                        item.ReceivedBy = currUser;
                        item.ITEMID = itemArrival.Item;
                        item.DateReceived = Convert.ToDateTime(DateReceive);
                        item.Site = itemArrival.Site;
                        item.ItemGroup = itemArrival.ItemGroup;

                        if (item.ItemGroup == "MachineP" || item.ItemGroup == "Tooling")
                        {
                            dbsp.SCM_Sparepart_Product_Received.Add(item);
                            dbsp.SaveChanges();

                            // set confirmReceivedStatus kolom jadi 1 pada table dbax.SCM_D365ImporForm_ProductReceipt
                            var qItemReceived = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == smodel.Invoice && w.Item == item.ITEMID).FirstOrDefault();
                            qItemReceived.ConfirmReceivedStatus = 1;
                            dbax.SaveChanges();
                        }
                    }

                    
                    EmailSendTo.Add("ms.santari@niterragroup.com");

                    SendEmail(smodel.Invoice, EmailSendTo, smodel.JournalNumber);

                    return Json(new
                    {
                        status = "1",
                        msg = "Item Arrival Success, item stock updated",
                        a = a,
                        ins = ins
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "1",
                        msg = "Item Arrival Success",
                        a=a,
                        ins = ins
                    });
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
        public ActionResult ConfirmReceiveDocument(T_AXInvoiceTransfer smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            try
            {
                var qInvoiceTransfer = dbmit.T_AXInvoiceTransfer.Where(w => w.Invoice == smodel.Invoice).FirstOrDefault();
                qInvoiceTransfer.ReceiveDocumentBy = CurrUser.NIK;
                qInvoiceTransfer.ReceiveDocumentTime = DateTime.Now;
                qInvoiceTransfer.Status = "1";

                var ins = dbmit.SaveChanges();
                if (ins == 1)
                {
                    
                    return Json(new
                    {
                        status = "1",
                        msg = "Document receive success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "0",
                        msg = "Failed proccess receive document process, please contact the Administrator of Portal"
                    });
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
        public ActionResult ProcessJournalNumber(T_AXInvoiceTransfer smodel, bool IsFlexnet = false)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            try
            {
                var qInvoiceTransfer = dbmit.T_AXInvoiceTransfer.Where(w => w.Invoice == smodel.Invoice).FirstOrDefault();

                qInvoiceTransfer.JournalTransferTime = DateTime.Now;
                qInvoiceTransfer.JournalTransferBy = CurrUser.NIK;
                qInvoiceTransfer.IsJournal = 1;

                if (IsFlexnet)
                {
                    // langsung posting
                    qInvoiceTransfer.PostingJournalTime = DateTime.Now;
                    qInvoiceTransfer.PostingJournalBy = CurrUser.NIK;
                    qInvoiceTransfer.IsPostingJournal = 1;
                    qInvoiceTransfer.IsFlexnetItem = 1;
                    qInvoiceTransfer.JournalNumber = "flexnet";


                }
                else
                {
                    qInvoiceTransfer.JournalNumber = smodel.JournalNumber;
                    qInvoiceTransfer.IsFlexnetItem = 0;
                }

                var ins = dbmit.SaveChanges();
                if (ins == 1)
                {
                    // send email notification
                    // 1. get item group salah satu item dari table [dbo].[SCM_D365ImporForm_ProductReceipt]
                    var EmailSendTo = new ArrayList();
                    var qItemGroup = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == smodel.Invoice).FirstOrDefault();
                    if (qItemGroup.ItemGroup == "MachineP" || qItemGroup.ItemGroup == "Tooling")
                    {
                        EmailSendTo.Add("juni.efendi@ngkbusi.com");
                        EmailSendTo.Add("avia.ferdian@ngkbusi.com");
                        //EmailSendTo.Add("ihsan.shalihin.mail@gmail.com");
                    }
                    else if (qItemGroup.ItemGroup == "FG-3rd-T" || qItemGroup.ItemGroup == "FG-NGK-T")
                    {
                        EmailSendTo.Add("bayu.setyo@ngkbusi.com");
                        //EmailSendTo.Add("ikhsan.sholihin@ngkbusi.com");
                    }
                    else
                    {
                        EmailSendTo.Add("muhammad.khosyi@niterragroup.com");
                        EmailSendTo.Add("muhammad.rizal@niterragroup.com");
                        //EmailSendTo.Add("ihsansha71hin@gmail.com");
                    }
                    var msg = "";
                    if (IsFlexnet)
                    {
                        //tidak perlu kirim email
                        msg = "Journal and Posting Process save successfully";
                    }
                    else
                    {
                        SendEmail(smodel.Invoice, EmailSendTo, smodel.JournalNumber);
                        msg = "Journal Number save successfully";
                    }

                    return Json(new
                    {
                        status = "1",
                        msg = msg
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "0",
                        msg = "Failed to save Journal Number, please contact the Administrator of portal application"
                    });
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
        public ActionResult ProcessPostingJournal(T_AXInvoiceTransfer smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            try
            {
                var qInvoiceTransfer = dbmit.T_AXInvoiceTransfer.Where(w => w.Invoice == smodel.Invoice).FirstOrDefault();
                qInvoiceTransfer.PostingJournalTime = DateTime.Now;
                qInvoiceTransfer.PostingJournalBy = CurrUser.NIK;
                qInvoiceTransfer.IsPostingJournal = 1;


                var ins = dbmit.SaveChanges();
                if (ins == 1)
                {
                    
                    return Json(new
                    {
                        status = "1",
                        msg = "Journal Posted"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "0",
                        msg = "Failed to Posting Journal, please contact the Administrator of portal application"
                    });
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
        public ActionResult ProcessReturn(T_AXInvoiceTransfer smodel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            try
            {
                // update status arrival
                var remove = dbmit.T_AXInvoiceTransfer.Where(w => w.Invoice == smodel.Invoice).First();
                dbmit.T_AXInvoiceTransfer.Remove(remove);

                // remove from table scm_product_receive
                var SparepartReceive = dbsp.SCM_Sparepart_Product_Received.Where(w => w.ProductReceipt == smodel.Invoice).ToList();
                foreach(var itemdelete in SparepartReceive)
                {
                    dbsp.SCM_Sparepart_Product_Received.Remove(itemdelete);
                }
                dbsp.SaveChanges();

                var AXProductReceive = dbax.SCM_D365ImporForm_ProductReceipt.Where(w => w.ProductReceipt == smodel.Invoice).ToList();
                foreach(var item in AXProductReceive)
                {
                    item.ConfirmReceivedStatus = 0;
                }
                dbax.SaveChanges();

                var ins = dbmit.SaveChanges();
                if (ins == 1)
                {
                    return Json(new
                    {
                        status = "1",
                        msg = "Cancel Received Success"
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
            catch (Exception e)
            {
                return Json(new
                {
                    status = "0",
                    msg = e.Message
                });
            }
        }
        public void SendEmail(string requestNo, ArrayList emailSendTo, string JournalNumber)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();


            string FilePath = Path.Combine(Server.MapPath("~/Emails/SCM/MIT/"), "notif.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();

            //var act = Url.Action("DetailRequest", "MatProm", new { area = "Marketing", RequestNo = requestNo }, this.Request.Url.Scheme);
            //Repalce [newusername] = signup user name      
            MailText = MailText.Replace("##Invoice##", requestNo);
            MailText = MailText.Replace("##JournalNumber##", JournalNumber);
            //MailText = MailText.Replace("##Status##", Status);
            //MailText = MailText.Replace("##requestByName##", requestByName);

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "MIT Portal App");
            //var receiverEmail = new MailAddress(EmailAddress, "Receiver");
            var password = "100%NGKbusi!";
            var sub = "MIT System Notification";
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
                        //var CurrUser = db.V_Users_Active.Where(w => w.NIK == dataEmail).First();
                        //var EmailAddress = CurrUser.Email;

                        mess.To.Add(new MailAddress(dataEmail));

                    }

                }
                mess.Bcc.Add(new MailAddress("ikhsan.sholihin@ngkbusi.com"));
                smtp.Send(mess);
            }


        }
        //public ActionResult CancelReceived(T_AXInvoiceTransfer smodel)
        //{
        //    var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
        //    var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

        //    try
        //    {

        //        var remove = dbmit.T_AXInvoiceTransfer.Where(w => w.Invoice == smodel.Invoice).First();
        //        dbmit.T_AXInvoiceTransfer.Remove(remove);
        //        var ins = dbmit.SaveChanges();
        //        if (ins == 1)
        //        {
        //            return Json(new
        //            {
        //                status = "1",
        //                msg = "Cancel Received Success"
        //            });
        //        }
        //        else
        //        {
        //            return Json(new
        //            {
        //                status = "0",
        //                msg = "failed"
        //            });
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new
        //        {
        //            status = "0",
        //            msg = e.Message
        //        });
        //    }
        //}
    }
}