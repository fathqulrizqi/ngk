using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HPM.Models;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Net.Sockets;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;


namespace NGKBusi.Areas.SCM.Controllers
{
    public class HPMQRCodeGeneratorController : Controller
    {
        HPMQRGeneratorConnection dbh = new HPMQRGeneratorConnection();
        DefaultConnection db = new DefaultConnection();
        // GET: SCM/HPMQRCodeGenerator
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();


            var itemList = dbh.SCM_HPMQRGenerator_Items
                  .OrderByDescending(item => item.id)
                  .ToList();
            ViewBag.items = itemList;
            return View();
        }


        public ActionResult readTxt(HttpPostedFileBase file)
        {

            var statusMessages = new List<string>();

            if (file != null && file.ContentLength > 0)
            {
                if (Path.GetExtension(file.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string uploadFolderPath = Server.MapPath("~/Files/HPM/Inventory/");

                        if (!Directory.Exists(uploadFolderPath))
                        {
                            Directory.CreateDirectory(uploadFolderPath);
                        }

                        string originalFileName = Path.GetFileName(file.FileName);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                        string savedFilePath = Path.Combine(uploadFolderPath, uniqueFileName);

                        file.SaveAs(savedFilePath);

                        using (var reader = new StreamReader(savedFilePath))
                        {
                            string line;
                            int lineNumber = 0;

                            var data_item = new SCM_HPMQRGenerator_Items();

                            data_item.filename = file.FileName;
                            data_item.path = savedFilePath;

                            var item = dbh.SCM_HPMQRGenerator_Items.Add(data_item);
                            dbh.SaveChanges();

                            if (item != null)
                            {
                                while ((line = reader.ReadLine()) != null)
                                {

                                    lineNumber++;
                                    if (string.IsNullOrWhiteSpace(line)) continue;

                                    string[] parts = line.Split(new[] { '\t' });
                                    string isiParts = string.Join(", ", parts);


                                    string SafeSubstring(string s, int startIndex, int? length = null)
                                    {
                                        if (string.IsNullOrEmpty(s) || startIndex > s.Length) return "";
                                        if (length.HasValue)
                                        {
                                            if (startIndex + length.Value > s.Length) return s.Substring(startIndex);
                                            return s.Substring(startIndex, length.Value);
                                        }
                                        return s.Substring(startIndex);
                                    }

                                    string[] sample = parts[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                    if (sample.Length >= 16)
                                    {

                                        var data = new SCM_HPMQRGenerator_Item_Detail();

                                        data.item_id = item.id;

                                        // From index 2 + index 3 
                                        data.from = string.Join(" ", sample[2], sample[3]);

                                        // To: index 6 + index 4
                                        data.to = string.Join(" ", sample[6], sample[4]);

                                        // supply_address: index 7
                                        data.supply_address = sample[7];

                                        // M/S ID: index 8 
                                        data.ms_id = sample[8];

                                        // Inventory Category: index 9
                                        data.inventory_category = sample[9];

                                        // mid_coloum_1: index 10 + index 11
                                        data.mid_coloum_1 = string.Join(" ", sample[10], sample[11]);

                                        // mid_coloum_2: hapus index 0-13 + hapus 8 index terakhir
                                        int midColumnCount = sample.Length - 14 - 8;
                                        data.mid_coloum_2 = midColumnCount > 0 ? string.Join(" ", sample.Skip(14).Take(midColumnCount)) : "";

                                        // P/S Code: index -8 + index -7
                                        data.ps_code = string.Join(" ", sample[sample.Length - 8], sample[sample.Length - 7]);

                                        // order_class: index -6
                                        data.order_class = sample[sample.Length - 6];

                                        // production_seq_no: index -5 (12 char dari depan)
                                        data.production_seq_no = SafeSubstring(sample[sample.Length - 5], 0, 12);

                                        // KD Lot No: index -5 (3 char dari belakang) + index -4 (14 char dari depan)
                                        string kdLotSource1 = sample[sample.Length - 5];
                                        string kdLotPart1 = SafeSubstring(kdLotSource1, kdLotSource1.Length - 3);
                                        string kdLotPart2 = SafeSubstring(sample[sample.Length - 4], 0, 14);
                                        data.kd_lot_no = string.Join(" ", kdLotPart1, kdLotPart2);

                                        // barcode_No: index 0 (kecuali 2 digit pertama) + index 1 (5 char dari depan) + index -4 (6 char sebelum 4 digit terakhir)
                                        string barcodeSource3 = sample[sample.Length - 4];
                                        string barcodePart1 = SafeSubstring(sample[0], 2);
                                        string barcodePart2 = SafeSubstring(sample[1], 0, 5);
                                        string barcodePart3 = SafeSubstring(barcodeSource3, barcodeSource3.Length - 11, 6);
                                        data.barcode_no = barcodePart1 + barcodePart2 + barcodePart3;

                                        // ship: index -4 (3 char sebelum 4 digit terakhir)
                                        string shipSource = sample[sample.Length - 4];
                                        string ship = SafeSubstring(shipSource, shipSource.Length - 8, 3);
                                        data.ship = ship.TrimStart('0');

                                        // date: index -4 (4 char terakhir)
                                        string dateSource = sample[sample.Length - 4];
                                        data.date = SafeSubstring(dateSource, dateSource.Length - 5);

                                        // time: index -3 
                                        data.time = sample[sample.Length - 3];

                                        // hns: index -2 (di properti model namanya hns)
                                        data.hns = sample[sample.Length - 2];

                                        // --- PROSES PENYIMPANAN ---
                                        dbh.SCM_HPMQRGenerator_Item_Detail.Add(data);

                                    }
                                    else
                                    {
                                        statusMessages.Add($"Gagal: Format di baris {lineNumber} : {string.Join(", ", sample)}");
                                    }
                                }
                            }

                        }
                        dbh.SaveChanges();
                        statusMessages.Add($"Selesai");
                    }
                    catch (Exception ex)
                    {
                        statusMessages.Add($"Terjadi kesalahan saat memproses file: {ex.Message}");
                    }
                }
                else
                {
                    statusMessages.Add("Gagal: Harap unggah file dengan format .txt");
                }
            }
            else
            {
                statusMessages.Add("Gagal: Tidak ada file yang dipilih atau file kosong.");
            }


            TempData["StatusMessages"] = statusMessages;

            return RedirectToAction("Index");
        }

        [HttpGet]


        public ActionResult DetailTxt(int id)
        {
            // 1. Ambil data original dari database
            var detailItems = dbh.SCM_HPMQRGenerator_Item_Detail
                                 .Where(w => w.item_id == id)
                                 .ToList();
            var processedData = detailItems.Select(item => {
                var formParts = item.from?.Split(new[] { ' ' }, 2) ?? new string[0];
                var toParts = item.to?.Split(new[] { ' ' }, 2) ?? new string[0];
                var typeNGK = dbh.AX_CustExternalItem
    .Where(w => w.EXTERNALITEMID == item.mid_coloum_1 || w.EXTERNALITEMTXT == item.mid_coloum_1)
    .FirstOrDefault();
                return new LabelDetailViewModel
                {
                    item_id = item.item_id,
                    supply_address = item.supply_address,
                    ms_id = item.ms_id,
                    inventory_category = item.inventory_category,
                    mid_coloum_1 = item.mid_coloum_1,
                    mid_coloum_2 = item.mid_coloum_2,
                    ps_code = item.ps_code,
                    order_class = item.order_class,
                    production_seq_no = item.production_seq_no,
                    kd_lot_no = item.kd_lot_no,
                    barcode_no = item.barcode_no,
                    ship = item.ship,
                    date = item.date,
                    time = item.time,
                    hns = item.hns,
                    from_1 = formParts.Length > 0 ? formParts[0] : "",
                    from_2 = formParts.Length > 1 ? formParts[1] : "", 

                    to_1 = toParts.Length > 0 ? toParts[0] : "",
                    to_2 = toParts.Length > 1 ? toParts[1] : "",

                    qr_code = item.mid_coloum_2 + "|" + (typeNGK?.ITEMID ?? "null") + "|"+item.mid_coloum_1+"|"+item.ship
                };
            }).ToList();


            // 3. Lakukan grouping pada data yang SUDAH diproses
            var groupedData = processedData
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / 4)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            ViewBag.detail = groupedData;
            return View();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbh.Dispose();
            }
            base.Dispose(disposing);
        }
        public ActionResult DownloadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "File path cannot be empty.");
            }

            var fileRecord = dbh.SCM_HPMQRGenerator_Items.FirstOrDefault(f => f.path == filePath);

            if (fileRecord == null || !System.IO.File.Exists(filePath))
            {
                return HttpNotFound("File not found or you do not have permission to access it.");
            }

            string allowedFolderPath = Path.GetFullPath(Server.MapPath("~/Files/HPM/Inventory/"));
            string requestedFullPath = Path.GetFullPath(filePath);

            if (!requestedFullPath.StartsWith(allowedFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "Access to the file path is denied.");
            }


            byte[] fileBytes = System.IO.File.ReadAllBytes(requestedFullPath);

            string contentType = "text/plain";

            string downloadFileName = fileRecord.filename;

            return File(fileBytes, contentType, downloadFileName);
        }
    }

}