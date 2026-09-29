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
    public class AssemblyQrSparkPlugController : Controller
    {

        DefaultConnection db = new DefaultConnection();
        AssemblyQrSparkPlugConnection dbq = new AssemblyQrSparkPlugConnection();
        // GET: Production/AssemblyQrSparkPlug
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();


            var TypeList = dbq.Production_Assembly_QrSparkPlug_Master_Type.ToList();
            var HistoryList = dbq.Production_Assembly_QrSparkPlug_History.ToList();

            ViewBag.TypeList = TypeList;
            ViewBag.HistoryList = HistoryList;
            
            return View();
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

            var dataType = dbq.Production_Assembly_QrSparkPlug_Master_Type.Where(w => w.id == request.id_tipe).First();

            var data = new Production_Assembly_QrSparkPlug_History();
            var lot = request.lot.ToUpper();

            data.Part_No = dataType.PN;
            data.Part_Name = dataType.tipe;
            data.Lot_No = lot;
            data.IRD_No = IRDGenerator(lot);
            data.Inspector = request.inspector.ToUpper();
            data.Qty = request.qty;
            data.Tgl_Inspection = request.tgl_inspection;
            data.created_at = DateTime.Now;

            data.QR_Header = $"{data.Part_No}|1202402|{data.Qty}|{data.Lot_No}";

            dbq.Production_Assembly_QrSparkPlug_History.Add(data);
            dbq.SaveChanges();

            int lastSequence = 0;

            var lastDetail = dbq.Production_Assembly_QrSparkPlug_History_Detail
                .Where(d => d.QR_Header == data.QR_Header)
                .OrderByDescending(d => d.Sequence_No) 
                .FirstOrDefault();

            if (lastDetail != null && !string.IsNullOrEmpty(lastDetail.Sequence_No))
            {
                int.TryParse(lastDetail.Sequence_No, out lastSequence);
            }
            

            int pages = request.page_count > 0 ? request.page_count : 1;
            int totalDetailCount = pages * 16;

            for (int i = 1; i <= totalDetailCount; i++)
            {
                var detail = new Production_Assembly_QrSparkPlug_History_Detail();

                detail.History_Id = data.id;

                detail.QR_Header = data.QR_Header;

                int currentSeqValue = lastSequence + i;

                string sequence = currentSeqValue.ToString("D3");

                detail.Sequence_No = sequence;

                detail.QR_String = $"{data.QR_Header}|{sequence}";

                 detail.created_at = DateTime.Now;

                dbq.Production_Assembly_QrSparkPlug_History_Detail.Add(detail);
            }

            dbq.SaveChanges();

            return RedirectToAction("ViewPrint", new { id = data.id });
        }


        public ActionResult ViewPrint(int id)
        {
            var data = dbq.Production_Assembly_QrSparkPlug_History.FirstOrDefault(w => w.id == id);

            if (data == null)
            {
                return HttpNotFound("The requested record does not exist.");
            }

            var details = dbq.Production_Assembly_QrSparkPlug_History_Detail
                             .Where(d => d.History_Id == id)
                             .OrderBy(d => d.Sequence_No)
                             .ToList();

            ViewBag.Data = data;
            ViewBag.Details = details; 

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateTipe(Production_Assembly_QrSparkPlug_Master_Type masterType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    masterType.created_at = DateTime.Now;
                    dbq.Production_Assembly_QrSparkPlug_Master_Type.Add(masterType);
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
        public JsonResult UpdateTipe(Production_Assembly_QrSparkPlug_Master_Type masterType)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingType = dbq.Production_Assembly_QrSparkPlug_Master_Type.Find(masterType.id);
                    if (existingType == null)
                    {
                        return Json(new { success = false, message = "Data tipe tidak ditemukan." });
                    }

                    existingType.tipe = masterType.tipe;
                    existingType.PN = masterType.PN;
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
                var masterType = dbq.Production_Assembly_QrSparkPlug_Master_Type.Find(id);
                if (masterType == null)
                {
                    return Json(new { success = false, message = "Data tidak ditemukan." });
                }

                dbq.Production_Assembly_QrSparkPlug_Master_Type.Remove(masterType);
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