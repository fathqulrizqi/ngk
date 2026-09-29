using ClosedXML.Excel;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using NGK_AX.Models;
using NGKBusi.Areas.Production.Models;
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
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.Production.Controllers
{
    public class PlugCupQrGeneratorController : Controller
    {
        // GET: Production/PlugCup
        DefaultConnection db = new DefaultConnection();
        PlugCupQrGeneratorConnection dbq = new PlugCupQrGeneratorConnection();
        // GET: Production/AssemblyQrSparkPlug
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var TypeList = dbq.Production_PlugCup_QrGenerator_Master_Type.ToList();
            var HistoryList = dbq.Production_PlugCup_QrGenerator_History.ToList();

            ViewBag.TypeList = TypeList;
            ViewBag.HistoryList = HistoryList;

            return View();
        }

        public JsonResult getCustomerList()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var distinctCustomers = dbq.Production_PlugCup_QrGenerator_Master_Type
                .Select(x => x.customer)   
                .Distinct()               
                .ToList();                 

            return Json(new { success = true, data= distinctCustomers, message = "get customer success" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTypeList(string custName)
        {
            try
            {
                var typeList = dbq.Production_PlugCup_QrGenerator_Master_Type
                                  .Where(x => x.customer == custName)
                                  .ToList();

                return Json(new { success = true, data = typeList, message = "get type success" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult DetailHistory(int id)
        {
            return RedirectToAction("ViewPrint", new { id = id });
        }

        private string IRDGenerator(string lot)
        {

            var dayref = new Dictionary<char, string>
            {
                { 'A', "01" }, { 'B', "02" }, { 'C', "03" }, { 'E', "04" },
                { 'F', "05" }, { 'G', "06" }, { 'H', "07" }, { 'J', "08" },
                { 'K', "09" }, { 'L', "10" }, { '1', "11" }, { '2', "12" },
                { '3', "13" }, { '4', "14" }, { '5', "15" }, { '6', "16" },
                { '7', "17" }, { '8', "18" }, { '9', "19" }, { '0', "20" },
                { 'M', "21" }, { 'N', "22" }, { 'P', "23" }, { 'R', "24" },
                { 'S', "25" }, { 'T', "26" }, { 'U', "27" }, { 'V', "28" },
                { 'W', "29" }, { 'X', "30" }, { 'Y', "31" }
            };


            var monthref = new Dictionary<char, string>
            {
                { '1', "01" }, { '2', "02" }, { '3', "03" }, { '4', "04" },
                { '5', "05" }, { '6', "06" }, { '7', "07" }, { '8', "08" },
                { '9', "09" }, { 'X', "10" }, { 'Y', "11" }, { 'Z', "12" }

            };


            char yearChar = lot[1];
            char monthChar = lot[2];
            char dayChar = lot[3];


            string day = dayref[dayChar];
            string month = monthref[monthChar];
            string year = "2" + yearChar;

            return day + month + year;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult createHistory(RequestHistory request)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            string Name = string.Join(" ", CurrUser.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(1));

            var dataType = dbq.Production_PlugCup_QrGenerator_Master_Type.Where(w => w.id == request.id_tipe).First();

            var lastRunningNumberStr = dbq.Production_PlugCup_QrGenerator_History
                                         .OrderByDescending(x => x.created_at)
                                         .Select(x => x.Running_Number)
                                         .FirstOrDefault();

            int currentNum = int.TryParse(lastRunningNumberStr, out int parsed) ? parsed : -1;
            int nextNum = (currentNum + 1) % 1000;
            string newRunningNumber = nextNum.ToString("D3");
            string todayDate = DateTime.Now.ToString("yyyyMMdd");

            var data = new Production_PlugCup_QrGenerator_History();

            var lot = request.lot.ToUpper();
            data.Tipe = dataType.tipe;
            data.Part_No = dataType.part_no;
            data.Part_Name = dataType.part_name;
            data.Gimz_Code = dataType.gimz_code;
            data.Product_name = dataType.product_name;
            data.Lot_No = lot;
            data.IRD_No = IRDGenerator(lot);
            data.Inspector = request.inspector.ToUpper();
            data.Qty = request.qty;
            data.Customer = dataType.customer;
            data.Tgl_Inspection = request.tgl_inspection;
            data.created_at = DateTime.Now;
            data.Customer_Type = dataType.type_customer;
            data.Operator_Packing = request.operator_packing;

            data.Front_QR = !string.IsNullOrEmpty(dataType.customer_code)
                    ? $"{data.Part_No}|{dataType.customer_code}|{data.Qty}|{data.Lot_No}"
                    : null;

            data.Running_Number = newRunningNumber;
            data.QR_Header = $"P{todayDate}{newRunningNumber}";

            dbq.Production_PlugCup_QrGenerator_History.Add(data);
            dbq.SaveChanges();

            int pages = request.page_count > 0 ? request.page_count : 1;
            int totalDetailCount = pages * (dataType.label_per_page ?? 0);

            for (int i = 1; i <= totalDetailCount; i++)
            {
                string sequence = i.ToString("D3");
                string headerSequence = $"{data.QR_Header}{sequence}";

                char[] qrChars = new string(' ', 252).ToCharArray();

                Action<int, string> InsertAt = (pos, val) =>
                {
                    if (string.IsNullOrEmpty(val)) return;

                    val = val.Replace("\r", "").Replace("\n", "").Replace("\t", " ");

                    int zeroBasedIndex = pos - 1;
                    for (int j = 0; j < val.Length && (zeroBasedIndex + j) < 252; j++)
                    {
                        qrChars[zeroBasedIndex + j] = val[j];
                    }
                };

                InsertAt(1, headerSequence);           
                InsertAt(50, data.Inspector);          
                InsertAt(75, data.Product_name);
                InsertAt(154, data.Part_No?.Trim());
                InsertAt(189, data.Gimz_Code);         
                InsertAt(207, data.Lot_No);            
                InsertAt(217, data.Qty.ToString("D6")); 
                InsertAt(240, data.Qty.ToString("D7")); 

                string finalQrString = new string(qrChars);

                var detail = new Production_PlugCup_QrGenerator_History_Detail
                {
                    History_Id = data.id,
                    QR_Header = data.QR_Header,
                    Sequence_No = sequence,
                    QR_String = finalQrString,
                    created_at = DateTime.Now
                };

                dbq.Production_PlugCup_QrGenerator_History_Detail.Add(detail);
            }

            dbq.SaveChanges();

            return RedirectToAction("ViewPrint", new { id = data.id });
        }

        public ActionResult ViewPrint(int id)
        {
            var data = dbq.Production_PlugCup_QrGenerator_History.FirstOrDefault(w => w.id == id);

            if (data == null)
            {
                return HttpNotFound("The requested record does not exist.");
            }

            var details = dbq.Production_PlugCup_QrGenerator_History_Detail
                             .Where(d => d.History_Id == id)
                             .OrderBy(d => d.Sequence_No)
                             .ToList();

            ViewBag.Data = data;
            ViewBag.Details = details;


            return View();
        }
        public ActionResult Yamaha_OEM(int id)
        {
            var data = dbq.Production_PlugCup_QrGenerator_History.FirstOrDefault(w => w.id == id);

            if (data == null)
            {
                return HttpNotFound("The requested record does not exist.");
            }

            var details = dbq.Production_PlugCup_QrGenerator_History_Detail
                             .Where(d => d.History_Id == id)
                             .OrderBy(d => d.Sequence_No)
                             .ToList();

            ViewBag.Data = data;
            ViewBag.Details = details;


            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateTipe(Production_PlugCup_QrGenerator_Master_Type masterType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    masterType.created_at = DateTime.Now;
                    dbq.Production_PlugCup_QrGenerator_Master_Type.Add(masterType);
                    dbq.SaveChanges();
                    return Json(new { success = true, message = "Tipe berhasil ditambahkan!" });
                }

                string errorMessages = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
                return Json(new { success = false, message = "Data tidak valid: " + errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan server: " + ex.Message });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateTipe(Production_PlugCup_QrGenerator_Master_Type masterType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingType = dbq.Production_PlugCup_QrGenerator_Master_Type.Find(masterType.id);
                    

                    if (existingType == null)
                    {
                        return Json(new { success = false, message = "Data tipe tidak ditemukan." });
                    }

                    existingType.tipe = masterType.tipe;
                    existingType.part_no = masterType.part_no;
                    existingType.part_name = masterType.part_name;
                    existingType.gimz_code = masterType.gimz_code;
                    existingType.product_name  = masterType.product_name;
                    existingType.customer = masterType.customer;
                    existingType.customer_code = masterType.customer_code;
                    existingType.type_customer = masterType.type_customer;
                    existingType.label_per_page = masterType.label_per_page;
                    existingType.updated_at = DateTime.Now;
                    dbq.Entry(existingType).State = EntityState.Modified;
                    dbq.SaveChanges();

                    return Json(new { success = true, message = "Tipe berhasil diperbarui!" });
                }

                string errorMessages = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return Json(new { success = false, message = "Data tidak valid: " + errorMessages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan server: " + ex.Message });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteTipe(int id)
        {
            try
            {
                var masterType = dbq.Production_PlugCup_QrGenerator_Master_Type.Find(id);
                if (masterType == null)
                {
                    return Json(new { success = false, message = "Data tidak ditemukan." });
                }

                dbq.Production_PlugCup_QrGenerator_Master_Type.Remove(masterType);
                dbq.SaveChanges();
                return Json(new { success = true, message = "Tipe berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan server: " + ex.Message });
            }
        }

        

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbq.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}