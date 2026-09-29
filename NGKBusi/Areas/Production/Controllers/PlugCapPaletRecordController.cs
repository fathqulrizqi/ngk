
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using NGKBusi.Areas.Production.Models;
using NGKBusi.Models;
using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using System.Windows.Documents;
namespace NGKBusi.Areas.Production.Controllers
{

    public class PlugCapPaletRecordController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        PlugCapPalletConnection dbp = new PlugCapPalletConnection();
        // GET: Production/PlugCapPaletRecord
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var FGTagList = dbp.Production_PlugCap_Tag_FG
                   .OrderByDescending(x => x.created_at)
                   .ToList();

            ViewBag.FGTagList = FGTagList;
            return View();
        }

        [HttpPost]
        public JsonResult inputFGTag(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                {
                    return Json(new { success = false, message = "Data scan kosong." }, JsonRequestBehavior.AllowGet);
                }

                string[] dataArray = data.Split(' ');

                if (dataArray.Length < 1)
                {
                    return Json(new { success = false, message = "Format QR Kosong/Salah." }, JsonRequestBehavior.AllowGet);
                }

                string palletNo = dataArray[0];

                var existingData = dbp.Production_PlugCap_Tag_FG.FirstOrDefault(x => x.pallet_no == palletNo);

                if (existingData != null)
                {
                    string existingUrl = Url.Action("Detail", "PlugCapPaletRecord", new { id = existingData.id });

                    return Json(new
                    {
                        success = true, 
                        message = "Pallet sudah ada. Membuka data...",
                        redirectUrl = existingUrl,
                        data = existingData
                    }, JsonRequestBehavior.AllowGet);
                }

                if (dataArray.Length < 6)
                {
                    return Json(new { success = false, message = "Format QR Salah/Tidak Lengkap untuk data baru." }, JsonRequestBehavior.AllowGet);
                }

                string partNo = dataArray[1];

                string qtyString = dataArray[dataArray.Length - 1];

                if (!int.TryParse(qtyString, out int qty))
                {
                    if (!int.TryParse(dataArray[dataArray.Length - 2], out qty))
                    {
                        return Json(new { success = false, message = "Format Qty salah (harus angka)." }, JsonRequestBehavior.AllowGet);
                    }
                }


                string combinedTypeLot = "";
                for (int i = 2; i <= dataArray.Length - 3; i++)
                {
                    combinedTypeLot += dataArray[i] + " ";
                }

                combinedTypeLot = combinedTypeLot.Trim();

                string type = "";
                string lot = "";

                if (combinedTypeLot.Length > 4)
                {
                    lot = combinedTypeLot.Substring(combinedTypeLot.Length - 4);

                    type = combinedTypeLot.Substring(0, combinedTypeLot.Length - 4).Trim();
                }
                else
                {
                    lot = combinedTypeLot;
                    type = "-";
                }

                var dataInput = new Production_PlugCap_Tag_FG();
                dataInput.pallet_no = palletNo;
                dataInput.part_no = partNo;
                dataInput.type = type;
                dataInput.lot_no = lot;
                dataInput.qty = qty;
                dataInput.created_at = DateTime.Now;
                dataInput.updated_at = DateTime.Now;

                dbp.Production_PlugCap_Tag_FG.Add(dataInput);
                dbp.SaveChanges();

                string urlBaru = Url.Action("Detail", "PlugCapPaletRecord", new { id = dataInput.id });

                return Json(new
                {
                    success = true,
                    message = "Data Baru Berhasil Disimpan",
                    redirectUrl = urlBaru,
                    data = dataInput
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "System Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult Detail(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var fgTag = dbp.Production_PlugCap_Tag_FG
                   .FirstOrDefault(f => f.id == id);

            if (fgTag == null)
            {
                return HttpNotFound();
            }

            var partList = dbp.Production_PlugCap_Tag_Part
                              .Where(p => p.fg_id == id)
                              .ToList();

            ViewBag.FGTag = fgTag;
            ViewBag.PartTagList = partList;
            var PartTagList = dbp.Production_PlugCap_Tag_Part.Where(w => w.fg_id == id).ToList();

            return View();
        }

        [HttpPost]
        public JsonResult inputPartTag(List<List<string>> dataArray,int id)
        {
            // 1. Validasi Input
            if (dataArray == null || dataArray.Count == 0)
            {
                return Json(new { success = false, message = "Data is Empty" });
            }

            var listDataInput = new List<Production_PlugCap_Tag_Part>();

            try
            {
                
                for (int i = 0; i < dataArray.Count; i++)
                {
                    var row = dataArray[i]; 
                    if (row.Count < 5) continue;

                    var dataInput = new Production_PlugCap_Tag_Part();

                    dataInput.fg_id = id;

                    dataInput.pallet_no = row[0];
                    dataInput.part_no = row[1];

                    dataInput.type = row[2];

                    dataInput.lot_prod = row[3];
                    string dateString = row[0].Substring(1, 8);

                    DateTime tgl_repacking = DateTime.ParseExact(dateString, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

                    dataInput.tgl_repacking = tgl_repacking;
                    dataInput.BN = row[0].Substring(1); 


                    if (int.TryParse(row[4], out int parsedQty))
                    {
                        dataInput.qty = parsedQty;
                    }
                    else
                    {
                        dataInput.qty = 0;
                    }

                    dataInput.created_at = DateTime.Now;
                    dataInput.updated_at = DateTime.Now;
                    listDataInput.Add(dataInput);
                }

                if (listDataInput.Count > 0)
                {
                    dbp.Production_PlugCap_Tag_Part.AddRange(listDataInput);

                    dbp.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "Scan Berhasil. " + listDataInput.Count + " data tersimpan."
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Gagal memproses data (Format salah/kosong)." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error System: " + ex.Message });
            }
        }

        [HttpPost] 
        public JsonResult deleteFGTag(int id)
        {
            using (var transaction = dbp.Database.BeginTransaction()) // Opsional: Pakai transaksi biar aman
            {
                try
                {
                    var dataToDelete = dbp.Production_PlugCap_Tag_FG.FirstOrDefault(x => x.id == id);

                    if (dataToDelete == null)
                    {
                        return Json(new { success = false, message = "Data tidak ditemukan atau sudah dihapus." }, JsonRequestBehavior.AllowGet);
                    }

                    var relatedParts = dbp.Production_PlugCap_Tag_Part.Where(x => x.fg_id == id).ToList();

                    if (relatedParts.Any())
                    {
                        dbp.Production_PlugCap_Tag_Part.RemoveRange(relatedParts);
                    }

                    dbp.Production_PlugCap_Tag_FG.Remove(dataToDelete);

                    dbp.SaveChanges();
                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = "Pallet dan isinya berhasil dihapus.",
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Gagal menghapus: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost] 
        public JsonResult deletePartTag(int id)
        {
            try
            {
                var dataToDelete = dbp.Production_PlugCap_Tag_Part.FirstOrDefault(x => x.id == id);

                if (dataToDelete == null)
                {
                    return Json(new { success = false, message = "Data Part tidak ditemukan atau sudah dihapus." });
                }

                dbp.Production_PlugCap_Tag_Part.Remove(dataToDelete);

                dbp.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Part berhasil dihapus."
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error System: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetBatchPalletDetails(List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Tidak ada item yang dipilih." });
                }

                // 1. Ambil Semua Header FG yang sesuai ID
                var fgTags = dbp.Production_PlugCap_Tag_FG
                                     .Where(x => ids.Contains(x.id.Value))
                                     .ToList();

                // 2. Ambil Semua Part yang FG ID-nya ada di list
                var allParts = dbp.Production_PlugCap_Tag_Part
                                       .Where(x => ids.Contains(x.fg_id))
                                       .ToList();

                // 3. Gabungkan Data (Grouping)
                // Kita bentuk struktur data: [{ header: {}, details: [] }, { header: {}, details: [] }]
                var resultData = fgTags.Select(fg => new
                {
                    header = fg,
                    details = allParts.Where(p => p.fg_id == fg.id).OrderBy(p => p.created_at).ToList()
                }).ToList();

                return Json(new { success = true, data = resultData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

}