using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using NGKBusi.Areas.Production.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;

namespace NGKBusi.Areas.Production.Controllers
{
    public class PlugCapTagFGGeneratorController : Controller
    {
        // Inisialisasi koneksi database
        DefaultConnection db = new DefaultConnection();
        PlugCapTagFGGeneratorConnection dbp = new PlugCapTagFGGeneratorConnection();
        PlugCupQrGeneratorConnection dbq = new PlugCupQrGeneratorConnection();

        // GET: Production/PlugCapTagFGGenerator
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);

            // Menampilkan riwayat Tag FG yang sudah di-generate
            var FGTagList = dbp.Production_PlugCap_TagFG_Generator_Tag_FG
                               .OrderByDescending(x => x.created_at)
                               .ToList();

            ViewBag.FGTagList = FGTagList;
            return View();
        }

        public ActionResult Detail(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);

            var fgTag = dbp.Production_PlugCap_TagFG_Generator_Tag_FG
                           .FirstOrDefault(f => f.id == id);

            if (fgTag == null)
            {
                return HttpNotFound();
            }

            var partList = dbp.Production_PlugCap_TagFG_Generator_Tag_Part
                              .Where(p => p.fg_id == id)
                              .ToList();

            ViewBag.FGTag = fgTag;
            ViewBag.PartTagList = partList;

            return View();
        }

        [HttpPost]
        public JsonResult GenerateTagFG(List<Production_PlugCap_TagFG_Generator_Tag_Part> scannedParts)
        {
            if (scannedParts == null || !scannedParts.Any())
            {
                return Json(new { success = false, message = "Data Part kosong. Silakan scan part terlebih dahulu." }, JsonRequestBehavior.AllowGet);
            }

            using (var transaction = dbp.Database.BeginTransaction())
            {
                try
                {
                    var firstPart = scannedParts.First();
                    int totalQty = scannedParts.Sum(p => p.qty);

                    var validPart = scannedParts.FirstOrDefault(p => !string.IsNullOrEmpty(p.BN) && p.BN.Length >= 3);
                    if (validPart == null)
                    {
                        return Json(new { success = false, message = "Format BN pada part tidak valid." }, JsonRequestBehavior.AllowGet);
                    }

                    string basePrefix = validPart.BN.Substring(0, validPart.BN.Length - 3);
                    int maxSequence = -1;

                    var historyList = dbq.Production_PlugCup_QrGenerator_History_Detail
                                         .Where(x => x.QR_Header.StartsWith(basePrefix) || basePrefix.StartsWith(x.QR_Header))
                                         .Select(x => new { x.QR_Header, x.Sequence_No })
                                         .ToList();

                    foreach (var history in historyList)
                    {
                        string fullBn = (history.QR_Header + history.Sequence_No).Trim();

                        if (fullBn.StartsWith(basePrefix) && fullBn.Length >= 3)
                        {
                            string last3Digits = fullBn.Substring(fullBn.Length - 3);
                            if (int.TryParse(last3Digits, out int currentNum))
                            {
                                if (currentNum > maxSequence) maxSequence = currentNum;
                            }
                        }
                    }

                    var fgList = dbp.Production_PlugCap_TagFG_Generator_Tag_FG
                                    .Where(x => x.BN.StartsWith(basePrefix))
                                    .Select(x => x.BN)
                                    .ToList();

                    foreach (var bn in fgList)
                    {
                        if (!string.IsNullOrEmpty(bn) && bn.Length >= 3)
                        {
                            string last3Digits = bn.Substring(bn.Length - 3);
                            if (int.TryParse(last3Digits, out int currentNum))
                            {
                                if (currentNum > maxSequence) maxSequence = currentNum;
                            }
                        }
                    }

                    foreach (var part in scannedParts)
                    {
                        if (!string.IsNullOrEmpty(part.BN) && part.BN.StartsWith(basePrefix) && part.BN.Length >= 3)
                        {
                            string last3Digits = part.BN.Substring(part.BN.Length - 3);
                            if (int.TryParse(last3Digits, out int currentNum))
                            {
                                if (currentNum > maxSequence) maxSequence = currentNum;
                            }
                        }
                    }

                    string autoGeneratedBN = "";
                    if (maxSequence != -1)
                    {
                        int nextNum = maxSequence + 1;
                        string newRunningNumber = nextNum.ToString("D3");
                        autoGeneratedBN = basePrefix + newRunningNumber;
                    }
                    else
                    {
                        return Json(new { success = false, message = "System Error: Gagal men-generate BN baru." }, JsonRequestBehavior.AllowGet);
                    }

                    char[] qrChars = new string(' ', 252).ToCharArray();
                    Action<int, string> InsertAt = (pos, val) =>
                    {
                        if (string.IsNullOrEmpty(val)) return;
                        int zeroBasedIndex = pos - 1;
                        for (int j = 0; j < val.Length && (zeroBasedIndex + j) < 252; j++)
                        {
                            qrChars[zeroBasedIndex + j] = val[j];
                        }
                    };

                    InsertAt(1, autoGeneratedBN);
                    InsertAt(50, firstPart.inspector);
                    InsertAt(75, firstPart.product_name);
                    InsertAt(154, firstPart.part_no);
                    InsertAt(189, firstPart.gimzcode);
                    InsertAt(207, firstPart.lot);
                    InsertAt(217, totalQty.ToString("D6"));
                    InsertAt(240, totalQty.ToString("D7"));

                    string finalQrString = new string(qrChars);

                    var dataFG = new Production_PlugCap_TagFG_Generator_Tag_FG
                    {
                        BN = autoGeneratedBN,
                        inspector = firstPart.inspector,
                        part_name = firstPart.part_name,
                        product_name = firstPart.product_name,
                        part_no = firstPart.part_no,
                        gimzcode = firstPart.gimzcode,
                        lot = firstPart.lot,
                        qty = totalQty,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    dbp.Production_PlugCap_TagFG_Generator_Tag_FG.Add(dataFG);
                    dbp.SaveChanges();

                    foreach (var part in scannedParts)
                    {
                        part.fg_id = dataFG.id.Value;
                        part.created_at = DateTime.Now;
                        part.updated_at = DateTime.Now;
                    }

                    dbp.Production_PlugCap_TagFG_Generator_Tag_Part.AddRange(scannedParts);
                    dbp.SaveChanges();

                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = $"Berhasil generate Tag FG dengan BN: {autoGeneratedBN} (Dari BN terbesar).",
                        redirectUrl = Url.Action("Detail", "PlugCapTagFGGenerator", new { id = dataFG.id })
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "System Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public JsonResult AddMultiplePartsToExistingFG(int fgId, List<Production_PlugCap_TagFG_Generator_Tag_Part> newParts)
        {
            // Cek apakah data list kosong
            if (newParts == null || !newParts.Any())
            {
                return Json(new { success = false, message = "Data Part kosong atau tidak valid." }, JsonRequestBehavior.AllowGet);
            }

            using (var transaction = dbp.Database.BeginTransaction())
            {
                try
                {
                    // 1. Ambil data FG Tag yang menjadi target penambahan
                    var fgTag = dbp.Production_PlugCap_TagFG_Generator_Tag_FG.FirstOrDefault(x => x.id == fgId);
                    if (fgTag == null)
                    {
                        return Json(new { success = false, message = "FG Tag tidak ditemukan dalam database." }, JsonRequestBehavior.AllowGet);
                    }

                    int totalAddedQty = 0;

                    // Lakukan looping untuk semua part yang dikirim dari Frontend
                    foreach (var part in newParts)
                    {
                        // 2. Pemeriksaan Konsistensi Gimzcode & Lot
                        // Backend juga tetap memvalidasi keamanan datanya
                        if (fgTag.gimzcode.Trim() != part.gimzcode.Trim())
                        {
                            return Json(new { success = false, message = $"Gimzcode tidak cocok! Harusnya {fgTag.gimzcode} tapi ada yang discan {part.gimzcode}." }, JsonRequestBehavior.AllowGet);
                        }

                        if (fgTag.lot.Trim() != part.lot.Trim())
                        {
                            return Json(new { success = false, message = $"Lot tidak cocok! Harusnya {fgTag.lot} tapi ada yang discan {part.lot}." }, JsonRequestBehavior.AllowGet);
                        }

                        bool isDuplicate = dbp.Production_PlugCap_TagFG_Generator_Tag_Part.Any(x => x.BN == part.BN);
                        if (isDuplicate)
                        {
                            return Json(new { success = false, message = $"Part dengan Sequence (BN) {part.BN} sudah pernah di-scan sebelumnya!" }, JsonRequestBehavior.AllowGet);
                        }

                        part.fg_id = fgTag.id.Value;
                        part.created_at = DateTime.Now;
                        part.updated_at = DateTime.Now;

                        dbp.Production_PlugCap_TagFG_Generator_Tag_Part.Add(part);

                        totalAddedQty += part.qty;
                    }

                    fgTag.qty += totalAddedQty;
                    fgTag.updated_at = DateTime.Now;

                    dbp.SaveChanges();
                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = $"{newParts.Count} Part sukses ditambahkan! Qty FG Tag bertambah {totalAddedQty}."
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "System Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public JsonResult deleteFGTag(int id)
        {
            using (var transaction = dbp.Database.BeginTransaction())
            {
                try
                {
                    var dataToDelete = dbp.Production_PlugCap_TagFG_Generator_Tag_FG.FirstOrDefault(x => x.id == id);

                    if (dataToDelete == null)
                    {
                        return Json(new { success = false, message = "Data tidak ditemukan atau sudah dihapus." }, JsonRequestBehavior.AllowGet);
                    }

                    dbp.Production_PlugCap_TagFG_Generator_Tag_FG.Remove(dataToDelete);

                    dbp.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Tag FG beserta daftar part-nya berhasil dihapus." }, JsonRequestBehavior.AllowGet);
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Data sudah dihapus atau diubah oleh pengguna lain sebelumnya." }, JsonRequestBehavior.AllowGet);
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
            using (var transaction = dbp.Database.BeginTransaction())
            {
                try
                {
                    var partToDelete = dbp.Production_PlugCap_TagFG_Generator_Tag_Part.FirstOrDefault(x => x.id == id);

                    if (partToDelete == null)
                    {
                        return Json(new { success = false, message = "Data Part tidak ditemukan atau sudah dihapus." });
                    }

                    var fgHeader = dbp.Production_PlugCap_TagFG_Generator_Tag_FG.FirstOrDefault(x => x.id == partToDelete.fg_id);

                    if (fgHeader != null)
                    {
                        fgHeader.qty -= partToDelete.qty;
                        fgHeader.updated_at = DateTime.Now;
                    }

                    dbp.Production_PlugCap_TagFG_Generator_Tag_Part.Remove(partToDelete);

                    dbp.SaveChanges();
                    transaction.Commit();

                    return Json(new
                    {
                        success = true,
                        message = "Part berhasil dihapus dan quantity Tag FG telah diperbarui."
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Error System: " + ex.Message });
                }
            }
        }


        [HttpPost]
        public JsonResult GetBatchFGDetails(List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "Tidak ada item yang dipilih." });
                }

                var fgTags = dbp.Production_PlugCap_TagFG_Generator_Tag_FG
                                     .Where(x => ids.Contains(x.id.Value))
                                     .ToList();

                

                return Json(new { success = true, data = fgTags });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}