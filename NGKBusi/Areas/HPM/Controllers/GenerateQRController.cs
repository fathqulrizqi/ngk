using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using NGKBusi.Areas.HPM.Models;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.HPM.Controllers
{
    public class GenerateQRController : Controller
    {
        GenerateQRConnection dbg = new GenerateQRConnection();

        public ActionResult Index()
        {
            var daftarItems = dbg.HPM_GenerateQR_Items.ToList();
            return View(daftarItems);
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


                        statusMessages.Add(savedFilePath);

                        file.SaveAs(savedFilePath);

                        using (var reader = new StreamReader(savedFilePath))
                        {
                            string line;
                            int lineNumber = 0;

                            var data_item = new HPM_GenerateQR_Items();

                            data_item.filename = file.FileName;
                            data_item.path = savedFilePath;

                            var item = dbg.HPM_GenerateQR_Items.Add(data_item);
                            dbg.SaveChanges();

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

                                    if (sample.Length >= 16 )
                                    {

                                        var data = new HPM_GenerateQR_Item_Detail();

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
                                        dbg.HPM_GenerateQR_Item_Detail.Add(data);
                                        
                                    }
                                    else
                                    {
                                        statusMessages.Add($"Gagal: Format di baris {lineNumber} : {string.Join(", ", sample)}");
                                    }
                                }
                            }
                        
                        }
                        dbg.SaveChanges();
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

        public ActionResult detailTxt()
        {
            //var daftarItems = dbg.HPM_GenerateQR_Item_Detail.Where(w => w.item_id == id).ToList();
            return View();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbg.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}