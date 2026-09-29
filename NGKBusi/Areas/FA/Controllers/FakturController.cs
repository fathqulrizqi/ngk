using ClosedXML.Excel;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.AX.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;

namespace NGKBusi.Areas.FA.Controllers
{
    public class FakturController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        D365Connection dbD365 = new D365Connection();

        [Authorize]
        public ActionResult Index()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 71
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            ViewBag.NavHide = true;

            //var minusTwoMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-2);
            //var custCodeList = db.V_AXCustInvoice.Where(w => w.INVOICEDATE >= minusTwoMonth && w.CURRENCYCODE == "IDR").OrderByDescending(o => o.INVOICEDATE).Select(s => s.INVOICEACCOUNT);
            //ViewBag.Customer = db.V_AXCustomerList.Where(w => w.CURRENCY == "IDR" && custCodeList.Contains(w.ACCOUNTNUM)).OrderBy(o => o.ACCOUNTNUM).ToList();
            //ViewBag.Invoice = db.V_AXCustInvoice.Where(w => w.INVOICEDATE >= minusTwoMonth && w.CURRENCYCODE == "IDR").OrderByDescending(o => o.INVOICEDATE).ToList();
            //ViewBag.InvoiceAddress = db.V_AXCustInvoiceAddress.Where(w => w.LOCATIONNAME != null).ToList();
            var minusTwoMonth = int.Parse(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-2).ToString("yyyyMMdd"));
            ViewBag.Customer = dbD365.AX_Sales_BI.Where(w => w.Currency == "IDR" && w.InvoiceDateYYYYMMDD > minusTwoMonth).OrderBy(o => o.Customer_code).Select(s => new d365Customer { Customer_code = s.Customer_code, Customer_name = s.Customer_name }).Distinct();
            //ViewBag.Invoice = dbD365.AX_Sales_BI.Where(w => w.Currency == "IDR" && w.InvoiceDateYYYYMMDD > minusTwoMonth).OrderByDescending(o => o.InvoiceDateYYYYMMDD).Select(s => new d365Invoice { Customer_code = s.Customer_code, Invoice = s.Tax_Invoice_Id.Substring(s.Tax_Invoice_Id.Length - 15), Invoice_Address = s.Invoice_Address }).Distinct();
            ViewBag.Invoice = dbD365.AX_Sales_BI.Where(w => w.Currency == "IDR" && w.InvoiceDateYYYYMMDD > minusTwoMonth).OrderByDescending(o => o.InvoiceDateYYYYMMDD).Select(s => new d365Invoice { Customer_code = s.Customer_code, Invoice = s.Invoice, Invoice_Address = s.Invoice_Address }).Distinct();
            ViewBag.InvoiceAddress = dbD365.AX_Sales_BI.Where(w => w.Currency == "IDR" && w.InvoiceDateYYYYMMDD > minusTwoMonth).OrderByDescending(o => o.InvoiceDateYYYYMMDD).Select(s => new d365InvoiceAddress { Customer_code = s.Customer_code, NPWP = s.NPWP, Invoice_Address = s.Invoice_Address }).Distinct();


            return View();
        }

        //[Authorize]
        [HttpPost]
        public JsonResult generateQrInvoice(List<string> invoices)
        {
            if (invoices == null || !invoices.Any())
            {
                return Json(new { success = true, data = new List<string>() }, JsonRequestBehavior.AllowGet);
            }
            var resultsList = new List<string>();
            var systemIdentification = "DELVI";

            var allInvoiceData = dbD365.AX_Sales_BI
                                       .Where(w => invoices.Contains(w.Invoice))
                                       .ToList();

            foreach (var singleInvoice in invoices)
            {
                var data = allInvoiceData.Where(w => w.Invoice == singleInvoice).ToList();

                if (!data.Any())
                {
                    continue;
                }

                var invoiceDate = data[0].InvoiceDateYYYYMMDD.ToString();
                string formattedInvoiceDate; 
                if (DateTime.TryParseExact(
                    invoiceDate,
                     "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime tanggal))
                {
                    formattedInvoiceDate = tanggal.ToString("dd/MM/yyyy");
                }
                else
                {
                    continue;
                }

                var invoiceNumber = data[0].Invoice;
                var sellingPrice = Convert.ToInt32(data.Sum(x => x.Net_amount));
                var taxbase = Math.Round(sellingPrice * 0.9166666666666667);
                var vatAmount = Math.Round(taxbase * 0.12);
                var withHoldingTax = 0;
                var totalAmount = Math.Round(sellingPrice + vatAmount);

                var result = systemIdentification + "|" + formattedInvoiceDate + "|" + invoiceNumber + "|" + sellingPrice + "|" + taxbase + "|" + vatAmount + "|" + withHoldingTax + "|" + totalAmount;

                resultsList.Add(result);
            }

            return Json(new { success = true, data = resultsList }, JsonRequestBehavior.AllowGet);
        }
        public class d365Customer
        {
            public string Customer_code { get; set; }
            public string Customer_name { get; set; }
        }
        public class d365Invoice
        {
            public string Customer_code { get; set; }
            public string Invoice { get; set; }
            public string Invoice_Address { get; set; }
        }
        public class d365InvoiceAddress
        {
            public string Customer_code { get; set; }
            public string NPWP { get; set; }
            public string Invoice_Address { get; set; }
        }
        public class TaxInvoiceBulk
        {
            [XmlElement(Order = 1)]
            public string TIN { get; set; }
            [XmlArray(Order = 2)]
            public List<TaxInvoice> ListOfTaxInvoice = new List<TaxInvoice>();
        }
        public class GoodService
        {
            [XmlElement(Order = 1)]
            public string Opt { get; set; }
            [XmlElement(Order = 2)]
            public string Code { get; set; }
            [XmlElement(Order = 3)]
            public string Name { get; set; }
            [XmlElement(Order = 4)]
            public string Unit { get; set; }
            [XmlElement(Order = 5)]
            public double Price { get; set; }
            [XmlElement(Order = 6)]
            public int Qty { get; set; }
            [XmlElement(Order = 7)]
            public double TotalDiscount { get; set; }
            [XmlElement(Order = 8)]
            public double TaxBase { get; set; }
            [XmlElement(Order = 9)]
            public double OtherTaxBase { get; set; }
            [XmlElement(Order = 10)]
            public double VATRate { get; set; }
            [XmlElement(Order = 11)]
            public double VAT { get; set; }
            [XmlElement(Order = 12)]
            public double STLGRate { get; set; }
            [XmlElement(Order = 13)]
            public double STLG { get; set; }
        }
        public class TaxInvoice
        {
            [XmlElement(Order = 1)]
            public string TaxInvoiceDate { get; set; }
            [XmlElement(Order = 2)]
            public string TaxInvoiceOpt { get; set; }
            [XmlElement(Order = 3)]
            public string TrxCode { get; set; }
            [XmlElement(Order = 4)]
            public string AddInfo { get; set; }
            [XmlElement(Order = 5)]
            public string CustomDoc { get; set; }
            [XmlElement(Order = 6)]
            public string CustomDocMonthYear { get; set; }
            [XmlElement(Order = 7)]
            public string RefDesc { get; set; }
            [XmlElement(Order = 8)]
            public string FacilityStamp { get; set; }
            [XmlElement(Order = 9)]
            public string SellerIDTKU { get; set; }
            [XmlElement(Order = 10)]
            public string BuyerTin { get; set; }
            [XmlElement(Order = 11)]
            public string BuyerDocument { get; set; }
            [XmlElement(Order = 12)]
            public string BuyerCountry { get; set; }
            [XmlElement(Order = 13)]
            public string BuyerDocumentNumber { get; set; }
            [XmlElement(Order = 14)]
            public string BuyerName { get; set; }
            [XmlElement(Order = 15)]
            public string BuyerAdress { get; set; }
            [XmlElement(Order = 16)]
            public string BuyerEmail { get; set; }
            [XmlElement(Order = 17)]
            public string BuyerIDTKU { get; set; }

            [XmlArray(Order = 18)]
            public List<GoodService> ListOfGoodService = new List<GoodService>();
        }

        public ActionResult FakturCSV()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var invoice = Request.Form.GetValues("iInvoice[]");
            var npwp = Request.Form.GetValues("iRegistrationNumber[]");
            var revision = Request.Form.GetValues("iRevision[]");
            var dokumenCode = Request.Form.GetValues("iDokumenCode[]");
            var customer = Request["iCustomer"].Split('|');
            var address = Request.Form.GetValues("iAddress[]");

            var fileName = "";
            foreach (var inv in invoice)
            {
                fileName += inv.ToString().Substring(inv.Length - 9).TrimStart('0') + ",";
            }

            string csv = "FK,KD_JENIS_TRANSAKSI,FG_PENGGANTI,NOMOR_FAKTUR,MASA_PAJAK,TAHUN_PAJAK,TANGGAL_FAKTUR,NPWP, NAMA ,ALAMAT_LENGKAP,JUMLAH_DPP,JUMLAH_PPN,JUMLAH_PPNBM,ID_KETERANGAN_TAMBAHAN,FG_UANG_MUKA,UANG_MUKA_DPP,UANG_MUKA_PPN,UANG_MUKA_PPNBM,REFERENSI,KODE_DOKUMEN_PENDUKUNG";
            csv += "\n" + "LT,NPWP,NAMA,JALAN,BLOK,NOMOR,RT,RW, KECAMATAN ,KELURAHAN,KABUPATEN,PROPINSI,KODE_POS,NOMOR_TELEPON";
            csv += "\n" + "OF,KODE_OBJEK,NAMA,HARGA_SATUAN,JUMLAH_BARANG,HARGA_TOTAL,DISKON,DPP, PPN ,TARIF_PPNBM,PPNBM";
            for (var i = 0; i <= invoice.Count() - 1; i++)
            {
                var addressData = address[i].Split('|');
                var currInvoice = invoice[i].ToString();
                var currCustCode = customer[0].ToString();
                var invoiceData = db.V_AXCustInvoice.Where(w => w.INVOICEACCOUNT == currCustCode && w.LEDGERVOUCHER == currInvoice).FirstOrDefault();
                csv += "\n" + "FK," + invoiceData.PURCHASEORDER.Substring(0, 2) + "," + revision[i].ToString() + "," + invoiceData.PURCHASEORDER.Substring(4, 3) + invoiceData.PURCHASEORDER.Substring(8, 2) + invoiceData.INVOICEID.ToString() + "," + invoiceData.INVOICEDATE.Month.ToString() + "," + invoiceData.INVOICEDATE.Year.ToString() + "," + invoiceData.INVOICEDATE.ToString("dd/MM/yyyy") + "," + npwp[i].ToString().Replace(".", "").Replace("-", "") + ",\"" + customer[1].ToString() + "\",\"" + addressData[1].ToString() + "\"," + invoiceData.SALESBALANCE.ToString("#.##") + "," + Math.Floor(invoiceData.SALESBALANCE * (decimal)0.11).ToString("#.##") + ",0," + (invoiceData.PURCHASEORDER.Substring(0, 2) == "07" ? "18" : "") + ",0,,,," + invoice[i].ToString() + "," + dokumenCode[i].ToString();

                var invoiceTrans = db.V_AXCustInvoiceTrans.Where(w => w.INVOICEID == invoiceData.INVOICEID).ToList();
                foreach (var data in invoiceTrans)
                {
                    var dataItem = invoiceData.PURCHASEORDER.Substring(0, 2) == "07" ? "Busi " + data.ITEMID + " (HS : 8511.10.20)" : data.ITEMID + data.EXTERNALITEMID;

                    csv += "\n" + "OF,," + dataItem + "," + data.SALESPRICE.ToString("#.##") + "," + data.QTY.ToString("#.##") + "," + (data.QTY * data.SALESPRICE).ToString("#.##") + "," + (data.DISCAMOUNT.ToString("#.##").Length == 0 ? "0" : (data.DISCAMOUNT * data.QTY).ToString("#.##")) + "," + data.LINEAMOUNT.ToString("#.##") + "," + (data.LINEAMOUNT * (decimal)0.11).ToString("#.##") + ",0,0";
                }
            }

            return File(new System.Text.UTF8Encoding().GetBytes(csv), "application/vnd.ms-excel", fileName.TrimEnd(',') + ".csv");
        }

        public ActionResult FakturCSVD365()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var invoice = Request.Form.GetValues("iInvoice[]");
            var npwp = Request.Form.GetValues("iRegistrationNumber[]");
            var revision = Request.Form.GetValues("iRevision[]");
            var dokumenCode = Request.Form.GetValues("iDokumenCode[]");
            var customer = Request["iCustomer"].Split('|');
            var address = Request.Form.GetValues("iAddress[]");

            var fileName = "";
            foreach (var inv in invoice)
            {
                fileName += inv.ToString().Substring(inv.Length - 9).TrimStart('0') + ",";
            }

            string csv = "FK,KD_JENIS_TRANSAKSI,FG_PENGGANTI,NOMOR_FAKTUR,MASA_PAJAK,TAHUN_PAJAK,TANGGAL_FAKTUR,NPWP, NAMA ,ALAMAT_LENGKAP,JUMLAH_DPP,JUMLAH_PPN,JUMLAH_PPNBM,ID_KETERANGAN_TAMBAHAN,FG_UANG_MUKA,UANG_MUKA_DPP,UANG_MUKA_PPN,UANG_MUKA_PPNBM,REFERENSI,KODE_DOKUMEN_PENDUKUNG";
            csv += "\n" + "LT,NPWP,NAMA,JALAN,BLOK,NOMOR,RT,RW, KECAMATAN ,KELURAHAN,KABUPATEN,PROPINSI,KODE_POS,NOMOR_TELEPON";
            csv += "\n" + "OF,KODE_OBJEK,NAMA,HARGA_SATUAN,JUMLAH_BARANG,HARGA_TOTAL,DISKON,DPP, PPN ,TARIF_PPNBM,PPNBM";
            for (var i = 0; i <= invoice.Count() - 1; i++)
            {
                var addressData = address[i];
                var currInvoice = invoice[i].ToString();
                var currCustCode = customer[0].ToString();
                var invoiceData = dbD365.AX_Sales_BI.Where(w => w.Customer_code == currCustCode && w.Invoice == currInvoice).FirstOrDefault();
                var invoiceDataNetSum = dbD365.AX_Sales_BI.Where(w => w.Customer_code == currCustCode && w.Invoice == currInvoice).Sum(s => s.Net_amount);
                var invoiceDataNetTax = dbD365.AX_Sales_BI.Where(w => w.Customer_code == currCustCode && w.Invoice == currInvoice).Sum(s => s.Tax);
                csv += "\n" + "FK," + invoiceData.Tax_Invoice_Id.Substring(0, 2) + "," + revision[i].ToString() + "," + invoiceData.Tax_Invoice_Id.Substring(4, 3) + invoiceData.Tax_Invoice_Id.Substring(8, 2) + invoiceData.Tax_Invoice_Id.Substring(invoiceData.Tax_Invoice_Id.Length - 8) + "," + invoiceData.InvoiceDateYYYYMM.ToString().Substring(4, 2) + "," + invoiceData.InvoiceDateYYYYMM.ToString().Substring(0, 4) + "," + DateTime.ParseExact(invoiceData.InvoiceDateYYYYMMDD.ToString(), "yyyyMMdd", null).ToString("dd/MM/yyyy") + "," + npwp[i].ToString().Replace(".", "").Replace("-", "") + ",\"" + customer[1].ToString() + "\",\"" + addressData.ToString() + "\"," + Math.Floor(invoiceDataNetSum).ToString("#.##") + "," + (invoiceData.Tax_Invoice_Id.Substring(0, 2) == "07" ? Math.Floor(invoiceDataNetSum * (decimal)0.11).ToString("#.##") : Math.Floor(invoiceDataNetTax).ToString("#.##")) + ",0," + (invoiceData.Tax_Invoice_Id.Substring(0, 2) == "07" ? "18" : "") + ",0,,,," + invoice[i].ToString() + "," + dokumenCode[i].ToString();

                var invoiceTrans = dbD365.AX_Sales_BI.Where(w => w.Invoice == invoiceData.Invoice)
                    .GroupBy(g => new { g.Item_code, g.External_Item_Id, g.Price_by_Local_currency })
                    .Select(s => new
                    {
                        Item_code = s.Key.Item_code,
                        External_Item_Id = s.Key.External_Item_Id,
                        Price_by_Local_currency = s.Average(ss => ss.Price_by_Local_currency),
                        Quantity = s.Sum(ss => ss.Quantity),
                        Amount_by_Local_currency = s.Sum(ss => ss.Amount_by_Local_currency),
                        Line_Disc = s.Sum(ss => ss.Line_Disc),
                        Net_amount = s.Sum(ss => ss.Net_amount),
                        Tax = s.Sum(ss => ss.Tax)
                    })
                    .ToList();

                foreach (var data in invoiceTrans)
                {
                    var dataItem = invoiceData.Tax_Invoice_Id.Substring(0, 2) == "07" ? "Busi " + data.Item_code + " (HS : 8511.10.20)" : data.Item_code + data.External_Item_Id;

                    csv += "\n" + "OF,," + dataItem + "," + data.Price_by_Local_currency.ToString("#.##") + "," + data.Quantity.ToString("#.##") + "," + (data.Amount_by_Local_currency).ToString("#.##") + "," + (data.Line_Disc.ToString("#.##").Length == 0 ? "0" : (data.Line_Disc).ToString("#.##")) + "," + data.Net_amount.ToString("#.##") + "," + (invoiceData.Tax_Invoice_Id.Substring(0, 2) == "07" ? (data.Net_amount * (decimal)0.11).ToString("#.##") : (data.Tax).ToString("#.##")) + ",0,0";
                }
            }

            return File(new System.Text.UTF8Encoding().GetBytes(csv), "application/vnd.ms-excel", fileName.TrimEnd(',') + ".csv");
        }
        public ActionResult FakturXML()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var invoice = Request.Form.GetValues("iInvoice[]");
            var npwp = Request.Form.GetValues("iRegistrationNumber[]");
            var revision = Request.Form.GetValues("iRevision[]");
            var dokumenCode = Request.Form.GetValues("iDokumenCode[]");
            var customer = Request["iCustomer"].Split('|');
            var address = Request.Form.GetValues("iAddress[]");
            var newTaxInvoiceBulk = new TaxInvoiceBulk();

            //TIN = Niterra NPWP
            newTaxInvoiceBulk.TIN = "0010006443055000";

            var fileName = "";
            foreach (var inv in invoice)
            {
                fileName += inv.ToString().Substring(inv.Length - 9).TrimStart('0') + ",";
            }

            for (var i = 0; i <= invoice.Count() - 1; i++)
            {
                var addressData = address[i];
                var currInvoice = invoice[i].ToString();
                var currCustCode = customer[0].ToString();
                var invoiceData = dbD365.AX_Sales_BI.Where(w => w.Customer_code == currCustCode && w.Invoice == currInvoice).FirstOrDefault();
                var invoiceDataNetSum = dbD365.AX_Sales_BI.Where(w => w.Customer_code == currCustCode && w.Invoice == currInvoice).Sum(s => s.Net_amount);
                var invoiceDataNetTax = dbD365.AX_Sales_BI.Where(w => w.Customer_code == currCustCode && w.Invoice == currInvoice).Sum(s => s.Tax);

                var invoiceTrans = dbD365.AX_Sales_BI.Where(w => w.Invoice == invoiceData.Invoice)
                    .GroupBy(g => new { g.Item_code, g.External_Item_Id, g.Price_by_Local_currency,g.Product_category_name })
                    .Select(s => new
                    {
                        Item_code = s.Key.Item_code,
                        External_Item_Id = s.Key.External_Item_Id,
                        Product_category_name = s.Key.Product_category_name,
                        Price_by_Local_currency = s.Average(ss => ss.Price_by_Local_currency),
                        Quantity = s.Sum(ss => ss.Quantity),
                        Amount_by_Local_currency = s.Sum(ss => ss.Amount_by_Local_currency),
                        Line_Disc = s.Sum(ss => ss.Line_Disc),
                        Net_amount = s.Sum(ss => ss.Net_amount),
                        Tax = s.Sum(ss => ss.Tax)
                    })
                    .ToList();

                var currCustomer = dbD365.AX_Customer_List.Where(w => w.Account == currCustCode).FirstOrDefault();
                var currTaxInvoice = new TaxInvoice()
                {
                    TaxInvoiceDate = DateTime.ParseExact(invoiceData.InvoiceDateYYYYMMDD.ToString(), "yyyyMMdd", null).ToString("yyyy-MM-dd"),
                    TaxInvoiceOpt = "Normal",
                    TrxCode = invoiceData.Customer_search_name.IndexOf("Batam") != -1 ? "07":"04",
                    AddInfo = (invoiceData.Customer_search_name.IndexOf("Batam") != -1 ? "TD.00518" : ""),
                    CustomDoc = dokumenCode[i].ToString(),
                    CustomDocMonthYear = invoiceData.Customer_search_name.IndexOf("Batam") != -1 ? DateTime.ParseExact(invoiceData.InvoiceDateYYYYMMDD.ToString(), "yyyyMMdd", null).ToString("yyyy-MM-dd") : "",
                    RefDesc = currInvoice,
                    FacilityStamp = (invoiceData.Customer_search_name.IndexOf("Batam") != -1 ? "TD.01118" : ""),
                    SellerIDTKU = "0010006443055000000000",
                    BuyerTin = currCustomer.NPWP16,
                    BuyerDocument = "TIN",
                    BuyerCountry = invoiceData.Country,
                    BuyerDocumentNumber = "",
                    BuyerName = invoiceData.Customer_name,
                    BuyerAdress = invoiceData.Invoice_Address,
                    BuyerEmail = "",
                    BuyerIDTKU = currCustomer.NITKU
                };
                foreach (var data in invoiceTrans)
                {
                    var dataItem = invoiceData.Customer_search_name.IndexOf("Batam") != -1 ? (data.Product_category_name == "2W Platform" ? "Rantai " + data.Item_code + " (HS : 7315.11.10)" : (data.Product_category_name == "Ignition Coil" ? "Koil Pengapian " + data.Item_code + " (HS : 8511.30.49)" : "Busi " + data.Item_code + " (HS : 8511.10.20)")) : data.Item_code + data.External_Item_Id;

                    currTaxInvoice.ListOfGoodService.Add(new GoodService()
                    {
                        Opt = "A",
                        Code = "000000",
                        Name = dataItem,
                        Unit = "UM.0021",
                        Price = double.Parse(data.Price_by_Local_currency.ToString()),
                        Qty = data.Quantity,
                        TotalDiscount = double.Parse(data.Line_Disc.ToString()),
                        TaxBase = double.Parse(Math.Round(data.Net_amount).ToString()),
                        OtherTaxBase = double.Parse(Math.Round((decimal)(0.9166666666666667) * data.Net_amount).ToString()),
                        VATRate = 12,
                        VAT = double.Parse(Math.Round(Math.Round((decimal)(0.9166666666666667) * data.Net_amount) * (decimal)0.12).ToString()),
                        STLGRate = 0,
                        STLG = 0
                    });
                }
                newTaxInvoiceBulk.ListOfTaxInvoice.Add(currTaxInvoice);
            }
            var mem = new MemoryStream();
            var ser = new XmlSerializer(typeof(TaxInvoiceBulk));
            ser.Serialize(mem, newTaxInvoiceBulk);
            var utf8 = new UTF8Encoding();

            return File(new System.Text.UTF8Encoding().GetBytes(utf8.GetString(mem.GetBuffer(), 0, (int)mem.Length)), "text/xml", fileName.TrimEnd(',') + ".xml");
        }


        public string getSalesBIData()
        {
            string fname;
            string filePath = "";
            HttpFileCollectionBase files = Request.Files;
            for (int z = 0; z < files.Count; z++)
            {
                HttpPostedFileBase file = files[z];

                if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                {
                    string[] testfiles = file.FileName.Split(new char[] { '\\' });
                    fname = testfiles[testfiles.Length - 1];
                }
                else
                {
                    fname = file.FileName;
                }

                string folderPath = Server.MapPath("~/Files/AX/");

                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                filePath = Path.Combine(folderPath, fname);
                file.SaveAs(filePath);
            }

            //Open the Excel file using ClosedXML.
            using (XLWorkbook workBook = new XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                IXLWorksheet workSheet = workBook.Worksheet(1);

                //Create a new DataTable.
                DataTable dt = new DataTable();

                //Loop through the Worksheet rows.
                bool firstRow = true;
                foreach (IXLRow row in workSheet.Rows())
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
                            //var CheckRecID = row.Cell(39).GetString();
                            //bool checkSameRecid = dt.AsEnumerable().Any(a => a.Field<string>("Rec id") == row.Cell(39).GetString());
                            //if (checkSameRecid)
                            //{
                            //    continue;
                            //}
                            //Add rows to DataTable.
                            dt.Rows.Add();
                            int i = 0;
                            foreach (IXLCell cell in row.Cells(1, dt.Columns.Count))
                            {
                                if (i < dt.Columns.Count)
                                {
                                    dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                                }

                                i++;
                            }
                        }
                    }
                }
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                var data = dt.Rows.OfType<DataRow>()
                .Select(row => dt.Columns.OfType<DataColumn>()
                    .ToDictionary(col => col.ColumnName, c => row[c]));
                var dtData = dt.AsEnumerable();
                var dtDate = dtData.Select(s => Convert.ToInt32(s["InvoiceDateYYYYMMDD"])).Distinct().ToList();
                var truncateData = dbD365.AX_Sales_BI.Where(w => dtDate.Contains(w.InvoiceDateYYYYMMDD)).ToList();
                dbD365.AX_Sales_BI.RemoveRange(truncateData);
                dbD365.SaveChanges();

                return System.Text.Json.JsonSerializer.Serialize(data);
            }

        }

        [HttpPost]
        public string UploadSalesBI()
        {
            var Company = Request["uploadData[Company]"];
            var InvoiceDateYYYYMM = int.Parse(Request["uploadData[InvoiceDateYYYYMM]"]);
            var InvoiceDateYYYYMMDD = int.Parse(Request["uploadData[InvoiceDateYYYYMMDD]"]);
            var Due_date = int.Parse(Request["uploadData[Due date]"]);
            var SO_Create_Date_YYYYMMDD = int.Parse(Request["uploadData[SO Create Date YYYYMMDD]"]);
            var Request_receipt_date = int.Parse(Request["uploadData[Request receipt date]"]);
            var Invoice = Request["uploadData[Invoice]"];
            var SO_No = Request["uploadData[SO No.]"];
            var Customer_code = Request["uploadData[Customer code]"];
            var Customer_name = Request["uploadData[Customer name]"];
            var Customer_search_name = Request["uploadData[Customer search name]"];
            var Item_code = Request["uploadData[Item code]"];
            var Item_name = Request["uploadData[Item name]"];
            var Division = Request["uploadData[Division]"];
            var Division_name = Request["uploadData[Division name]"];
            var Product_Category = Request["uploadData[Product Category]"];
            var Product_category_name = Request["uploadData[Product category name]"];
            var Country = Request["uploadData[Country]"];
            var Area = Request["uploadData[Area]"];
            var Sales_person = Request["uploadData[Sales person]"];
            var Sales_person_name = Request["uploadData[Sales person name]"];
            var Business_type = int.Parse(Request["uploadData[Business type]"]);
            var Business_type_name = Request["uploadData[Business type name]"];
            var Design = Request["uploadData[Design]"];
            var Brand = Request["uploadData[Brand]"];
            var Product_cate_major = Request["uploadData[Product cate major]"];
            var Product_cate_major_name = Request["uploadData[Product cate major name]"];
            var Product_cate_middle = Request["uploadData[Product cate middle]"];
            var Product_cate_middle_name = Request["uploadData[Product cate middle name]"];
            var Product_cate_minor = Request["uploadData[Product cate minor]"];
            var Product_cate_minor_name = Request["uploadData[Product cate minor Name]"];
            var Packaging_country = Request["uploadData[Packaging country]"];
            var Quantity = int.Parse(Request["uploadData[Quantity]"]);
            var Currency = Request["uploadData[Currency]"];
            var Price_by_IV_currency = decimal.Parse(Request["uploadData[Price by IV currency]"]);
            var Amount_by_IV_currency = decimal.Parse(Request["uploadData[Amount by IV currency]"]);
            var Price_by_Local_currency = decimal.Parse(Request["uploadData[Price by Local currency]"]);
            var Amount_by_Local_currency = decimal.Parse(Request["uploadData[Amount by Local currency]"]);
            var Rec_id = long.Parse(Request["uploadData[Rec id]"]);
            var Customer_reference = Request["uploadData[Customer reference]"];
            var Description_for_sales_report = Request["uploadData[Description for sales report]"];
            var Item_Group = Request["uploadData[Item Group]"];
            var Tax_Percent = decimal.Parse(Request["uploadData[Tax %]"]);
            var Tax = decimal.Parse(Request["uploadData[Tax]"]);
            var Net_amount = decimal.Parse(Request["uploadData[Net amount]"]);
            var Tax_Invoice_Id = Request["uploadData[Tax Invoice Id]"];
            var NPWP = Request["uploadData[NPWP]"];
            var Line_Disc = decimal.Parse(Request["uploadData[Line Disc]"]);
            var Invoice_Address = Request["uploadData[Invoice Address]"];
            var External_Item_Id = Request["uploadData[External Item Id]"];

            var checkRecIDExist = dbD365.AX_Sales_BI.Where(w => w.Rec_id == Rec_id).FirstOrDefault();
            if(checkRecIDExist != null)
            {
                goto skipInsert;
            }

            dbD365.AX_Sales_BI.Add(new AX_Sales_BI
            {
                Company = Company,
                InvoiceDateYYYYMM = InvoiceDateYYYYMM,
                InvoiceDateYYYYMMDD = InvoiceDateYYYYMMDD,
                Due_date = Due_date,
                SO_Create_Date_YYYYMMDD = SO_Create_Date_YYYYMMDD,
                Request_receipt_date = Request_receipt_date,
                Invoice = Invoice,
                SO_No = SO_No,
                Customer_code = Customer_code,
                Customer_name = Customer_name,
                Customer_search_name = Customer_search_name,
                Item_code = Item_code,
                Item_name = Item_name,
                Division = Division,
                Division_name = Division_name,
                Product_Category = Product_Category,
                Product_category_name = Product_category_name,
                Country = Country,
                Area = Area,
                Sales_person = Sales_person,
                Sales_person_name = Sales_person_name,
                Business_type = Business_type,
                Business_type_name = Business_type_name,
                Design = Design,
                Brand = Brand,
                Product_cate_major = Product_cate_major,
                Product_cate_major_name = Product_cate_major_name,
                Product_cate_middle = Product_cate_middle,
                Product_cate_middle_name = Product_cate_middle_name,
                Product_cate_minor = Product_cate_minor,
                Product_cate_minor_name = Product_cate_minor_name,
                Packaging_country = Packaging_country,
                Quantity = Quantity,
                Currency = Currency,
                Price_by_IV_currency = Price_by_IV_currency,
                Amount_by_IV_currency = Amount_by_IV_currency,
                Price_by_Local_currency = Price_by_Local_currency,
                Amount_by_Local_currency = Amount_by_Local_currency,
                Rec_id = Rec_id,
                Customer_reference = Customer_reference,
                Description_for_sales_report = Description_for_sales_report,
                Item_Group = Item_Group,
                Tax_Percent = Tax_Percent,
                Tax = Tax,
                Net_amount = Net_amount,
                Tax_Invoice_Id = Tax_Invoice_Id,
                NPWP = NPWP,
                Line_Disc = Line_Disc,
                Invoice_Address = Invoice_Address,
                External_Item_Id = External_Item_Id,
            });

            dbD365.SaveChanges();

        skipInsert:
            return Item_code;
        }
    }
}