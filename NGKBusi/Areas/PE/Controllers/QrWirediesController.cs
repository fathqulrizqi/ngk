using com.itextpdf.text.pdf;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.PE.Models;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Management;
using System.Net.Sockets;
using System.Security.Claims;
using System.Web.Mvc;
using System.Windows.Documents;

namespace NGKBusi.Areas.PE.Controllers
{
    public class QrWirediesController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        WirediesConnection dbwd = new WirediesConnection();
        // GET: PE/QrWiredies
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var printerList = dbwd.PE_Wiredies_Printer.ToList();
            var qrCodeList = dbwd.PE_Wiredies_QR_Result.ToList();



            ViewBag.Printers = printerList;
            ViewBag.QrCodesResult = qrCodeList;
            return View();
        }




        [HttpPost]
        public JsonResult CreatePrinter(PE_Wiredies_Printer printer)
        {
            try
            {
                if (printer == null)
                {
                    return Json(new { success = false, message = "Invalid data received." });
                }
                dbwd.PE_Wiredies_Printer.Add(printer);
                dbwd.SaveChanges();
                return Json(new { success = true, message = "Printer added successfully!" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditPrinter(PE_Wiredies_Printer printer)
        {
            try
            {
                if (printer == null)
                {
                    return Json(new { success = false, message = "Invalid data received." });
                }
                dbwd.Entry(printer).State = EntityState.Modified;

                dbwd.SaveChanges();
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
                var printerToDelete = dbwd.PE_Wiredies_Printer.Find(id);

                if (printerToDelete == null)
                {
                    return Json(new { success = false, message = "Printer not found." });
                }
                dbwd.PE_Wiredies_Printer.Remove(printerToDelete);

                dbwd.SaveChanges();

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
            try
            {
                var allPrinters = dbwd.PE_Wiredies_Printer.ToList();

                if (!allPrinters.Any())
                {
                    return Json(new { success = false, message = "Tidak ada data printer di database." });
                }

                foreach (var printer in allPrinters)
                {
                    printer.printer_status = (printer.id == id);
                }

                dbwd.SaveChanges();

                return Json(new { success = true, message = "Changed Printer successfully!" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
            public class CheckingRequest
            {
                public string ip { get; set; }
                public int port { get; set; }
                public string shared_printer { get; set; }
            }

        [HttpPost]
        public JsonResult CheckingPrinter(CheckingRequest request)
        {

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
                    return Json(new { success = false, message = $"Gagal memeriksa shared printer. Alasan: {ex.Message}" });
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

        public class LabelData
        {
            public string tanggal { get; set; }
            public string type { get; set; }
            public string category { get; set; }
            public string gimzcode { get; set; }
            public string qty { get; set; } 
        }

        private string GenerateZpl(string Type, string Category,string GimzCode,string Quantity, string qrCode, string printedBy, string printedDate)
        {
            return $@"^^XA
              ^CI28
              ^PW800
              ^FO50,30^A0N,37,37^FDWiredies Card^FS
              ^FO430,22^A0N,25,25^FDPrinted By : {printedBy}^FS
              ^FO430,52^A0N,25,25^FDDate         : {printedDate}^FS
              ^FO50,80^GB780,3,3^FS
              ^FO58,115^BQN,4,7^FDQA,{qrCode}^FS
              ^FO280,125^A0N,29,29^FDType^FS
              ^FO410,125^A0N,29,29^FD: {Type}^FS
              ^FO280,185^A0N,29,29^FDCategory^FS
              ^FO410,185^A0N,29,29^FD: {Category}^FS
              ^FO280,245^A0N,29,29^FDGimzCode^FS
              ^FO410,245^A0N,29,29^FD: {GimzCode}^FS
              ^FO280,305^A0N,29,29^FDQuantity^FS
              ^FO410,305^A0N,29,29^FD: {Quantity}^FS
              ^XZ";
        }

        private bool CheckPrinterSharedConnection(string shared_printer)
        {
            try
            {
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
            }
            catch
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
        public JsonResult PrintLabel(List<LabelData> ListLabel)
        {
            if (ListLabel == null || !ListLabel.Any())
            {
                return Json(new { success = false, message = "Printing Failed: A minimum of 1 label is required" });
            }
            var currUserNik = ((ClaimsIdentity)User.Identity).GetUserId();
            var currUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUserNik);
            if (currUser == null)
            {
                return Json(new { success = false, message = "Printing Failed: You Must Login First" });
            }
            var printer = dbwd.PE_Wiredies_Printer.FirstOrDefault(w => w.printer_status == true);
            if (printer == null)
            {
                return Json(new { success = false, message = "ExecutePrintMultiple failed: No active printer configured." });
            }

            try
            {
                var zplBuilder = new System.Text.StringBuilder();

                var newResultList = new List<PE_Wiredies_QR_Result>();
                var printTime = DateTime.Now;
                string formattedDate = printTime.ToString("yyyy/MM/dd");
                string twoWordsName = string.Join(" ", currUser.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2));
                var allInputQrCodes = ListLabel.Select(Label =>
                    $@"{formattedDate}-{Label.type}-{Label.category}-{Label.gimzcode}-{Label.qty}"
                ).Distinct().ToList();

                var existingCodesInDb = dbwd.PE_Wiredies_QR_Result
                                            .Where(r => allInputQrCodes.Contains(r.QRcode))
                                            .Select(r => r.QRcode)
                                            .ToHashSet();
                var codesToProcess = new HashSet<string>();
                foreach (var Label in ListLabel)
                {
                    string QrResult = $@"{formattedDate}-{Label.type}-{Label.category}-{Label.gimzcode}-{Label.qty}";

                    // (Logika ZPL tetap di sini jika Anda ingin TETAP MENCETAK label duplikat)
                    string singleZpl = GenerateZpl(Label.type, Label.category, Label.gimzcode, Label.qty, QrResult, twoWordsName, formattedDate);
                    zplBuilder.Append(singleZpl);

                    int labelQty;
                    if (!int.TryParse(Label.qty, out labelQty))
                    {
                        return Json(new { success = false, message = $"Invalid quantity format for GIMZ Code: {Label.gimzcode}" });
                    }

                    // --- LOGIKA PENYIMPANAN YANG DIPERBAIKI ---

                    // Cek apakah sudah ada di DB ATAU sudah diproses di loop ini
                    if (existingCodesInDb.Contains(QrResult) || codesToProcess.Contains(QrResult))
                    {
                        continue; // Lewati (skip) penyimpanan data yang sudah ada / duplikat
                    }

                    // Jika lolos cek di atas, ini adalah data baru
                    var Result = new PE_Wiredies_QR_Result
                    {
                        Date = formattedDate,
                        Type = Label.type,
                        Category = Label.category,
                        GimzCode = Label.gimzcode,
                        Qty = labelQty, // Anda sudah benar mem-parsing ini
                        QRcode = QrResult,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    newResultList.Add(Result);
                    codesToProcess.Add(QrResult); // Tandai QrResult ini sudah diproses
                }

                string combinedZplData = zplBuilder.ToString();

                int newItemsCount = 0;
                if (newResultList.Any())
                {
                    newItemsCount = newResultList.Count;
                    dbwd.PE_Wiredies_QR_Result.AddRange(newResultList);
                    dbwd.SaveChanges(); // SEKARANG AMAN dari error duplikat
                }
                if (string.IsNullOrWhiteSpace(combinedZplData))
                {
                    return Json(new { success = false, message = "ExecutePrintMultiple failed: ZPL data is empty after generation." });
                }

                bool isPrintSuccess = false;
                if (!string.IsNullOrWhiteSpace(printer.printer_ip) && printer.port > 0)
                {
                    if (!CheckPrinterConnection(printer.printer_ip, printer.port)) return Json(new { success = false, message = "ExecutePrintMultiple failed: Invalid printer connection." });

                    if (printer.port == 515)
                    {
                        string queueName = "Ip1";
                        isPrintSuccess = SendingFileViaLpr(combinedZplData, printer.printer_ip, queueName);
                    }
                    else
                    {
                        isPrintSuccess = SendingFileToIp(combinedZplData, printer.printer_ip, printer.port);
                    }
                }else if (!string.IsNullOrWhiteSpace(printer.shared_printer))
                {
                    if (!CheckPrinterSharedConnection(printer.shared_printer)) return Json(new { success = false, message = "ExecutePrintMultiple failed: Invalid printer connection." });

                    isPrintSuccess = SendingFileShared(combinedZplData, printer.shared_printer);
                }else
                {
                    return Json(new { success = false, message = "ExecutePrintMultiple failed: Invalid printer configuration." });
                }
                if (!isPrintSuccess)
                {
                    return Json(new { success = false, message = "ExecutePrintMultiple failed: Sending data to printer failed." });
                }


                return Json(new { success = true, message = $"{ListLabel.Count} label(s) sent to printer." });
            }
            catch (System.IO.IOException ioEx)
            {
                return Json(new { success = false, message = $"Printer offline or unreachable. Reason: {ioEx.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An unexpected error occurred. Reason: {ex.Message}" });
            }
        }

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
                var qrResult = dbwd.PE_Wiredies_QR_Result.Find(id);
                if (qrResult == null)
                {
                    return Json(new { success = false, message = "Label data not found." });
                }

                var printer = dbwd.PE_Wiredies_Printer.FirstOrDefault(w => w.printer_status == true);
                if (printer == null)
                {
                    return Json(new { success = false, message = "No active printer configured." });
                }

                string fullName = currUser.Name;
                string[] words = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string twoWordsName = string.Join(" ", words.Take(2));

                string zplData = GenerateZpl(qrResult.Type, qrResult.Category, qrResult.GimzCode, $@"{ qrResult.Qty}", qrResult.QRcode, twoWordsName, qrResult.Date);

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
                dbwd.SaveChanges();

                return Json(new { success = true, message = "Label successfully reprinted." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


    }
}