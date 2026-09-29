using Microsoft.AspNet.Identity;
using NGKBusi.Areas.PE.Models;
using NGKBusi.Areas.Quality.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using NGKBusi.Helpers;

namespace NGKBusi.Areas.PE.Controllers
{
    public class BMSController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        BMSConnection dbb = new BMSConnection();

        // ==========================================
        //  Manage Setting
        // ==========================================
        public ActionResult OtherSetting()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }

            return View();
        }

        public JsonResult getAccess()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return Json(new { success = false, message = "Unauthorized access" }, JsonRequestBehavior.AllowGet);
            }

            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var data = dbb.PE_BMS_Access.Where(a => a.deleted_at == null).ToList();
                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed get data Access: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public JsonResult CreateAccess(PE_BMS_Access dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var isNikExist = dbb.PE_BMS_Access.Any(a => a.NIK == dto.NIK);
                if (isNikExist)
                {
                    return Json(new { success = false, message = $"Validasi gagal: NIK '{dto.NIK}' sudah terdaftar dalam sistem." });
                }

                dto.created_at = DateTime.Now;
                dbb.PE_BMS_Access.Add(dto);
                dbb.SaveChanges();

                return Json(new { success = true, message = "Access Mapping berhasil ditambahkan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal create: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateAccess(int id, PE_BMS_Access dto)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbb.PE_BMS_Access.Find(id);

                if (item == null) return Json(new { success = false, message = "Data Access tidak ditemukan." });

                var isNikExist = dbb.PE_BMS_Access.Any(a => a.NIK == dto.NIK && a.access_id != id);
                if (isNikExist)
                {
                    return Json(new { success = false, message = $"Validasi gagal: NIK '{dto.NIK}' sudah digunakan oleh user lain." });
                }

                item.NIK = dto.NIK;
                item.access = dto.access;
                item.name = dto.name;
                item.updated_at = DateTime.Now;

                dbb.SaveChanges();
                return Json(new { success = true, message = "Access Mapping berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal update: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteAccess(int id)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik && w.access == "Administrator").FirstOrDefault();

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbb.PE_BMS_Access.Find(id);
                if (item == null) return Json(new { success = false, message = "Data Access tidak ditemukan." });

                dbb.PE_BMS_Access.Remove(item);
                dbb.SaveChanges();

                return Json(new { success = true, message = "Access Mapping berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal delete: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetToolingCategory()
        {
            try
            {
                var data = dbb.PE_BMS_Tooling_Category.Where(a => a.deleted_at == null)
                              .Select(s => new {
                                  category_id = s.category_id,
                                  category_name = s.category_name
                              })
                              .ToList();

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to retrieve Tooling Category data: " + errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CreateToolingCategory(PE_BMS_Tooling_Category dto)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.category_name))
                    return Json(new { success = false, message = "Invalid input data." });

                dbb.PE_BMS_Tooling_Category.Add(dto);
                dbb.SaveChanges();

                return Json(new { success = true, message = "Tooling Category added successfully." });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to create Tooling Category: " + errorMessage });
            }
        }

        [HttpPost]
        public JsonResult UpdateToolingCategory(int id, PE_BMS_Tooling_Category dto)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbb.PE_BMS_Tooling_Category.FirstOrDefault(x => x.category_id == id);
                if (item == null) return Json(new { success = false, message = "Tooling Category data not found." });

                item.category_name = dto.category_name;

                dbb.SaveChanges();

                return Json(new { success = true, message = "Tooling Category updated successfully." });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to update: " + errorMessage });
            }
        }

        [HttpPost]
        public JsonResult DeleteToolingCategory(int id)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbb.PE_BMS_Tooling_Category.FirstOrDefault(x => x.category_id == id && x.deleted_at == null);
                if (item == null) return Json(new { success = false, message = "Tooling Category data not found or already deleted." });

                var hasSpecs = dbb.PE_BMS_Tooling_Spec.Any(x => x.category_id == id && x.deleted_at == null);
                if (hasSpecs) return Json(new { success = false, message = "Cannot delete category because it is still in use by active Tooling Specs." });

                item.deleted_at = DateTime.Now;

                dbb.SaveChanges();

                return Json(new { success = true, message = "Tooling Category deleted successfully." });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to delete: " + errorMessage });
            }
        }

        [HttpGet]
        public JsonResult GetToolingSpec()
        {
            try
            {
                var data = dbb.PE_BMS_Tooling_Spec
                              .Where(w => w.deleted_at == null)
                              .Select(s => new {
                                  spec_id = s.spec_id,
                                  category_id = s.category_id,
                                  category_name = s.ToolingCategory != null ? s.ToolingCategory.category_name : "-",
                                  spec_name = s.spec_name,
                                  min_stock = s.min_stock,
                                  created_at = s.created_at,
                                  updated_at = s.updated_at
                              })
                              .ToList();

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to retrieve Tooling Spec data: " + errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CreateToolingSpec(PE_BMS_Tooling_Spec dto)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }); 

            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    if (dto == null) return Json(new { success = false, message = "Invalid input data." });

                    dto.created_at = DateTime.Now;

                    dbb.PE_BMS_Tooling_Spec.Add(dto);
                    dbb.SaveChanges();

                    var newStock = new PE_BMS_Stock_Management
                    {
                        spec_id = dto.spec_id, 
                        quantity = 0,
                        created_at = DateTime.Now
                    };
                    dbb.PE_BMS_Stock_Management.Add(newStock);

                    var newStockProduction = new PE_BMS_Stock_In_Production_Area
                    {
                        spec_id = dto.spec_id,
                        quantity = 0,
                        created_at = DateTime.Now
                    };
                    dbb.PE_BMS_Stock_In_Production_Area.Add(newStockProduction);

                    dbb.SaveChanges();

                    transaction.Commit();

                    return Json(new { success = true, message = "Tooling Spec and Stock (Qty: 0) added successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    string errorMessage = ex.Message;
                    if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                    return Json(new { success = false, message = "Failed to create tooling Spec: " + errorMessage });
                }
            }
        }

        [HttpPost]
        public JsonResult UpdateToolingSpec(int id, PE_BMS_Tooling_Spec dto)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                var item = dbb.PE_BMS_Tooling_Spec.FirstOrDefault(x => x.spec_id == id && x.deleted_at == null);
                if (item == null) return Json(new { success = false, message = "Tooling Spec data not found." });

                item.category_id = dto.category_id;
                item.spec_name = dto.spec_name;
                item.min_stock = dto.min_stock;
                item.updated_at = DateTime.Now;

                dbb.SaveChanges();

                return Json(new { success = true, message = "Tooling Spec updated successfully." });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to update: " + errorMessage });
            }
        }

        [HttpPost]
        public JsonResult DeleteToolingSpec(int id)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    var item = dbb.PE_BMS_Tooling_Spec.FirstOrDefault(x => x.spec_id == id && x.deleted_at == null);
                    if (item == null) return Json(new { success = false, message = "Tooling Spec data not found or already deleted." });

                    var stockItem = dbb.PE_BMS_Stock_Management.FirstOrDefault(x => x.spec_id == id && x.deleted_at == null);

                    if (stockItem != null && stockItem.quantity > 0)
                    {
                        return Json(new { success = false, message = "Cannot delete Spec because physical stock is still greater than 0." });
                    }

                    item.deleted_at = DateTime.Now;

                    if (stockItem != null)
                    {
                        stockItem.deleted_at = DateTime.Now;
                    }

                    var stockItemProduction = dbb.PE_BMS_Stock_In_Production_Area.FirstOrDefault(x => x.spec_id == id && x.deleted_at == null);

                    if (stockItemProduction != null && stockItemProduction.quantity > 0)
                    {
                        return Json(new { success = false, message = "Cannot delete Spec because physical stock is still greater than 0." });
                    }

                    item.deleted_at = DateTime.Now;

                    if (stockItemProduction != null)
                    {
                        stockItemProduction.deleted_at = DateTime.Now;
                    }

                    dbb.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Tooling Spec deleted successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string errorMessage = ex.Message;
                    if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                    return Json(new { success = false, message = "Failed to delete: " + errorMessage });
                }
            }
        }

        public ActionResult ManageStockWorkshop()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401");
            }

            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }

            return View();
        }

        [HttpGet]
        public JsonResult GetSpecsDropdown(bool isStaging = false)
        {
            try
            {
                var query = dbb.PE_BMS_Tooling_Spec
                    .Where(w => w.deleted_at == null && w.ToolingCategory != null);

                if (!isStaging)
                {
                    query = query.Where(w => !w.spec_name.ToUpper().Contains("NEW"));
                }

                var data = query
                    .Select(s => new {
                        id = s.spec_id,
                        name = s.spec_name
                    })
                    .ToList() 
                    .OrderByDescending(s => s.name.ToUpper().Contains("NEW")) 
                    .ThenBy(s => s.name) 
                    .ToList();

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetStockChartData()
        {
            try
            {
                var specsQuery = dbb.PE_BMS_Tooling_Spec
                    .Where(w => w.deleted_at == null)
                    .Select(s => new {
                        SpecId = s.spec_id,
                        Label = s.spec_name,
                        CategoryName = s.ToolingCategory != null ? s.ToolingCategory.category_name : "",
                        MinStock = s.min_stock,

                        CurrentStock = dbb.PE_BMS_Stock_Management
                            .Where(stock => stock.spec_id == s.spec_id && stock.deleted_at == null)
                            .Select(stock => stock.quantity)
                            .FirstOrDefault() ?? 0,

                        ProductionStock = dbb.PE_BMS_Stock_In_Production_Area
                             .Where(stock => stock.spec_id == s.spec_id && stock.deleted_at == null)
                             .Select(stock => stock.quantity)
                             .FirstOrDefault() ?? 0
                    })
                    .ToList();

                // =======================================================
                // CHART 1 & 2: HILANGKAN YANG SPEC_NAME (LABEL) ADA KATA "NEW"
                // =======================================================
                var specs = specsQuery
                    .Where(x => string.IsNullOrEmpty(x.Label) || !x.Label.ToUpper().Contains("NEW"))
                    .ToList();

                var labels = specs.Select(x => x.Label).ToArray();
                var currentStock = specs.Select(x => x.CurrentStock).ToArray();
                var productionStock = specs.Select(x => x.ProductionStock).ToArray();
                var minStock = specs.Select(x => x.MinStock).ToArray();

                // =======================================================
                // CHART 3 (STAGING): LOGIKA TERBARU
                // =======================================================

                var allLogs = dbb.PE_BMS_Log_Transactions
                    .Where(log => log.deleted_at == null)
                    .Select(log => new { log.spec_id, log.tooling_code, log.transaction_type, log.created_at })
                    .ToList();

                var activeStagingLogs = allLogs
                    .GroupBy(log => log.tooling_code)
                    .Select(g => g.OrderByDescending(x => x.created_at).FirstOrDefault())
                    .Where(log => log != null && log.transaction_type != null && log.transaction_type.ToUpper() == "STAGING")
                    .ToList();

                var chart3Data = specsQuery
                    .GroupBy(x =>
                    {
                        if (string.IsNullOrEmpty(x.CategoryName)) return "Uncategorized";

                        return System.Text.RegularExpressions.Regex.Replace(
                            x.CategoryName, @"\bNEW\b", "",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    })
                    .Select(g =>
                    {
                        var newSpecIds = g.Where(s => !string.IsNullOrEmpty(s.Label) && s.Label.ToUpper().Contains("NEW"))
                                          .Select(s => s.SpecId).ToList();

                        var repairSpecIds = g.Where(s => string.IsNullOrEmpty(s.Label) || !s.Label.ToUpper().Contains("NEW"))
                                             .Select(s => s.SpecId).ToList();

                        return new
                        {
                            BaseCategory = g.Key,
                            
                            NewStagingQty = activeStagingLogs.Count(log => newSpecIds.Contains(log.spec_id)),

                            RepairStagingQty = activeStagingLogs.Count(log => repairSpecIds.Contains(log.spec_id))
                        };
                    })
                    .ToList();

                var stagingLabels = chart3Data.Select(x => x.BaseCategory).ToArray();
                var newStagingStock = chart3Data.Select(x => x.NewStagingQty).ToArray();
                var repairStagingStock = chart3Data.Select(x => x.RepairStagingQty).ToArray();

                // =======================================================
                // RETURN HASIL KE VIEW
                // =======================================================
                return Json(new
                {
                    success = true,
                    labels = labels,
                    currentStock = currentStock,
                    productionStock = productionStock,
                    minStock = minStock,

                    stagingLabels = stagingLabels,
                    newStagingStock = newStagingStock,
                    repairStagingStock = repairStagingStock

                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetTransactionLogs()
        {
            try
            {
                var logs = dbb.PE_BMS_Log_Transactions
                    .OrderByDescending(o => o.created_at)
                    .Take(50)
                    .Select(s => new {
                        created_at = s.created_at,
                        delete_at = s.deleted_at,
                        transaction_type = s.transaction_type,
                        tooling_code = s.tooling_code,
                        item_no = s.item_no,
                        spec_name = s.ToolingSpec != null ? s.ToolingSpec.spec_name : "-",

                        category_name = (s.ToolingSpec != null && s.ToolingSpec.ToolingCategory != null)
                                        ? s.ToolingSpec.ToolingCategory.category_name
                                        : "-"
                    })
                    .ToList();

                return Json(new { success = true, data = logs }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult ProcessTransaction(int spec_id, string tooling_code, string item_no, string transaction_type)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik);

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

            if (transaction_type != "STOCK" && transaction_type != "DISPOSE" && transaction_type != "STAGING")
                return Json(new { success = false, message = "Invalid transaction type." });

            bool isAlreadyDisposed = dbb.PE_BMS_Tooling_Scrap.Any(x => x.tooling_code == tooling_code && x.deleted_at == null);
            if (isAlreadyDisposed)
            {
                return Json(new
                {
                    success = false,
                    message = $"Failed: Tooling '{tooling_code}' is already in the Scrap (Dispose) list. It cannot be processed further."
                });
            }

            var lastLog = dbb.PE_BMS_Log_Transactions
                             .Where(x => x.tooling_code == tooling_code && x.deleted_at == null)
                             .OrderByDescending(x => x.created_at)
                             .FirstOrDefault();

            if (lastLog == null)
            {
                if (transaction_type != "STAGING")
                {
                    return Json(new { success = false, message = $"Failed: Tooling '{tooling_code}' has no transaction history. It must be processed to the STAGING area first." });
                }
            }
            else
            {
                // --- LOGIKA PERUBAHAN SPEC ---
                if (transaction_type == "STAGING" || transaction_type == "DISPOSE")
                {
                    spec_id = (int)lastLog.spec_id;
                }
                else if (transaction_type == "STOCK")
                {
                    if (lastLog.transaction_type != "STAGING")
                    {
                        spec_id = (int)lastLog.spec_id;
                    }
                }

                // --- Validasi menuju STOCK ---
                if (transaction_type == "STOCK")
                {
                    if (lastLog.transaction_type == "STOCK")
                        return Json(new { success = false, message = $"Failed: Tooling '{tooling_code}' is already in STOCK." });

                    if (lastLog.transaction_type != "STAGING")
                        return Json(new { success = false, message = $"Failed: Tooling '{tooling_code}' must be in STAGING before it can be moved to STOCK." });
                }

                // --- Validasi menuju STAGING ---
                if (transaction_type == "STAGING")
                {
                    if (lastLog.transaction_type == "STAGING")
                        return Json(new { success = false, message = $"Failed: Tooling '{tooling_code}' is already in the STAGING queue." });

                    if (lastLog.transaction_type != "STOCK")
                        return Json(new { success = false, message = $"Failed: Tooling '{tooling_code}' can only enter STAGING if it comes from STOCK." });
                }

                // --- Validasi menuju DISPOSE ---
                // DISPOSE diperbolehkan dari STAGING maupun STOCK.
            }

            // 5. Eksekusi Database
            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    // Catat log transaksi baru
                    var newLog = new PE_BMS_Log_Transaction
                    {
                        spec_id = spec_id,
                        tooling_code = tooling_code,
                        item_no = item_no,
                        transaction_type = transaction_type,
                        created_at = DateTime.Now
                    };
                    dbb.PE_BMS_Log_Transactions.Add(newLog);
                    dbb.SaveChanges();

                    // Jika DISPOSE, tambahkan data ke tabel Scrap
                    if (transaction_type == "DISPOSE")
                    {
                        // Hitung seberapa sering tooling ini pernah masuk ke STOCK sebagai repair_count
                        int stockCount = dbb.PE_BMS_Log_Transactions
                                            .Count(x => x.tooling_code == tooling_code
                                                     && x.transaction_type == "STOCK"
                                                     && x.deleted_at == null);

                        int calculatedRepairCount = stockCount > 0 ? (stockCount - 1) : 0;

                        var newScrap = new PE_BMS_Tooling_Scrap
                        {
                            spec_id = spec_id, // Disini spec_id sudah dijamin memakai spec terakhir yang benar
                            tooling_code = tooling_code,
                            item_no = item_no,
                            repair_count = calculatedRepairCount,
                            last_usage_lifetime = 0,
                            created_at = DateTime.Now
                        };

                        dbb.PE_BMS_Tooling_Scrap.Add(newScrap);
                    }

                    // Manajemen Stok Fisik Utama berdasarkan spec_id yang aktif saat ini
                    var stockData = dbb.PE_BMS_Stock_Management.FirstOrDefault(x => x.spec_id == spec_id && x.deleted_at == null);

                    if (stockData == null)
                    {
                        stockData = new PE_BMS_Stock_Management
                        {
                            spec_id = spec_id,
                            quantity = (transaction_type == "STOCK") ? 1 : 0,
                            created_at = DateTime.Now
                        };
                        dbb.PE_BMS_Stock_Management.Add(stockData);
                    }
                    else
                    {
                        int currentQty = stockData.quantity ?? 0;

                        if (transaction_type == "STOCK")
                        {
                            // Tambah stok fisik spesifikasi yang baru
                            stockData.quantity = currentQty + 1;
                        }
                        else if (transaction_type == "DISPOSE" || transaction_type == "STAGING")
                        {
                            // Kurangi stok fisik spesifikasi lama
                            if (lastLog != null && lastLog.transaction_type == "STOCK")
                            {
                                if (currentQty < 1)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = $"Failed: Insufficient physical stock to withdraw. (Current: {currentQty})" });
                                }
                                stockData.quantity = currentQty - 1;
                            }
                        }

                        stockData.updated_at = DateTime.Now;
                    }

                    dbb.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = $"Transaction {transaction_type} processed successfully." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Exception deepestEx = ex;
                    while (deepestEx.InnerException != null)
                    {
                        deepestEx = deepestEx.InnerException;
                    }

                    return Json(new { success = false, message = "DB Error: " + deepestEx.Message });
                }
            }
        }

        [HttpGet]
        public JsonResult GetLowStockAlerts()
        {
            try
            {
                var specs = dbb.PE_BMS_Tooling_Spec
                    .Where(w => w.deleted_at == null &&
                                w.ToolingCategory != null)
                    .Select(s => new {
                        spec_id = s.spec_id,
                        spec_name = s.spec_name,
                        category_name = s.ToolingCategory.category_name,
                        min_stock = s.min_stock,

                        current_stock_workshop = dbb.PE_BMS_Stock_Management
                                                    .Where(stock => stock.spec_id == s.spec_id && stock.deleted_at == null)
                                                    .Select(stock => stock.quantity)
                                                    .FirstOrDefault() ?? 0,

                        current_stock_production = dbb.PE_BMS_Stock_In_Production_Area
                                                      .Where(stock => stock.spec_id == s.spec_id && stock.deleted_at == null)
                                                      .Select(stock => stock.quantity)
                                                      .FirstOrDefault() ?? 0
                    })
                    .ToList();

                var workshopLowStock = specs
                    .Where(x => x.current_stock_workshop < x.min_stock)
                    .Select(x => new {
                        x.spec_id,
                        x.spec_name,
                        x.category_name,
                        x.min_stock,
                        current_stock = x.current_stock_workshop,
                        shortage = x.min_stock - x.current_stock_workshop
                    })
                    .OrderByDescending(o => o.shortage)
                    .ToList();

                var productionLowStock = specs
                    .Where(x => x.current_stock_production < x.min_stock)
                    .Select(x => new {
                        x.spec_id,
                        x.spec_name,
                        x.category_name,
                        x.min_stock,
                        current_stock = x.current_stock_production,
                        shortage = x.min_stock - x.current_stock_production
                    })
                    .OrderByDescending(o => o.shortage)
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        workshopAlerts = workshopLowStock,
                        productionAlerts = productionLowStock
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetStockKPIs()
        {
            try
            {
                var logs = dbb.PE_BMS_Log_Transactions
                    .Where(x => x.deleted_at == null)
                    .Select(x => new { x.tooling_code, x.transaction_type, x.created_at })
                    .ToList();

                var latestTrans = logs.GroupBy(x => x.tooling_code)
                                      .Select(g => g.OrderByDescending(o => o.created_at).FirstOrDefault())
                                      .ToList();

                int inStock = latestTrans.Count(x => x != null && x.transaction_type == "STOCK");
                int staging = latestTrans.Count(x => x != null && x.transaction_type == "STAGING");

                var scrapData = dbb.PE_BMS_Tooling_Scrap.Where(x => x.deleted_at == null).ToList();
                var now = DateTime.Now;

                int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime startOfWeek = now.AddDays(-1 * diff).Date;

                int totalDispose = scrapData.Count;

                int disposeThisMonth = scrapData.Count(x =>
                    x.created_at.Month == now.Month &&
                    x.created_at.Year == now.Year);

                int disposeThisWeek = scrapData.Count(x =>
                    x.created_at >= startOfWeek);

                return Json(new
                {
                    success = true,
                    inStock = inStock,
                    staging = staging,
                    totalDispose = totalDispose,
                    disposeThisMonth = disposeThisMonth,
                    disposeThisWeek = disposeThisWeek
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetKPICategoryDetails(string type)
        {
            try
            {
                var specs = dbb.PE_BMS_Tooling_Spec.Include(x => x.ToolingCategory).ToList();

                // Diubah untuk menampung 'Stock' atau 'Staging'
                if (type == "Stock" || type == "Staging")
                {
                    var logs = dbb.PE_BMS_Log_Transactions
                        .Where(x => x.deleted_at == null)
                        .ToList();

                    var latestTrans = logs.GroupBy(x => x.tooling_code)
                                          .Select(g => g.OrderByDescending(o => o.created_at).FirstOrDefault())
                                          .ToList();

                    var targetType = "";
                    if (type == "Stock") targetType = "STOCK";
                    else if (type == "Staging") targetType = "STAGING";

                    var filtered = latestTrans.Where(x => x != null && x.transaction_type == targetType).ToList();

                    // Hitung repair berdasarkan seberapa sering masuk ke STOCK
                    var inCounts = logs.Where(x => x.transaction_type == "STOCK")
                                       .GroupBy(x => x.tooling_code)
                                       .ToDictionary(g => g.Key, g => g.Count());

                    var result = filtered.Select(x => {
                        var spec = specs.FirstOrDefault(s => s.spec_id == x.spec_id);

                        int totalStockIns = inCounts.ContainsKey(x.tooling_code) ? inCounts[x.tooling_code] : 0;
                        int repairCount = totalStockIns > 0 ? (totalStockIns - 1) : 0;

                        return new
                        {
                            category_name = spec?.ToolingCategory?.category_name ?? "-",
                            spec_name = spec?.spec_name ?? "-",
                            tooling_code = x.tooling_code,
                            repair_count = repairCount,
                            last_update = x.created_at
                        };
                    })
                    .OrderByDescending(x => x.last_update)
                    .ToList();

                    return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var scraps = dbb.PE_BMS_Tooling_Scrap.Where(x => x.deleted_at == null).ToList();
                    var now = DateTime.Now;

                    if (type == "MonthDispose")
                    {
                        scraps = scraps.Where(x => x.created_at.Month == now.Month && x.created_at.Year == now.Year).ToList();
                    }
                    else if (type == "WeekDispose")
                    {
                        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                        DateTime startOfWeek = now.AddDays(-1 * diff).Date;
                        scraps = scraps.Where(x => x.created_at >= startOfWeek).ToList();
                    }

                    var result = scraps.Select(x => {
                        var spec = specs.FirstOrDefault(s => s.spec_id == x.spec_id);
                        return new
                        {
                            category_name = spec?.ToolingCategory?.category_name ?? "-",
                            spec_name = spec?.spec_name ?? "-",
                            tooling_code = x.tooling_code,
                            repair_count = x.repair_count,
                            last_update = x.created_at
                        };
                    })
                    .OrderByDescending(x => x.last_update)
                    .ToList();

                    return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CancelDispose(string tooling_code)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");

            if (userAccess == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Access Denied: Only Administrators are allowed to cancel a dispose transaction."
                });
            }

            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    var scrap = dbb.PE_BMS_Tooling_Scrap.FirstOrDefault(x => x.tooling_code == tooling_code && x.deleted_at == null);
                    if (scrap == null) return Json(new { success = false, message = "Tooling is not in disposed state." });

                    var disposeLog = dbb.PE_BMS_Log_Transactions
                        .Where(x => x.tooling_code == tooling_code && x.transaction_type == "DISPOSE" && x.deleted_at == null)
                        .OrderByDescending(x => x.created_at)
                        .FirstOrDefault();

                    if (disposeLog == null) return Json(new { success = false, message = "Dispose transaction log not found." });

                    var previousLog = dbb.PE_BMS_Log_Transactions
                        .Where(x => x.tooling_code == tooling_code
                                 && x.log_transaction_id != disposeLog.log_transaction_id
                                 && x.deleted_at == null)
                        .OrderByDescending(x => x.created_at)
                        .FirstOrDefault();

                    scrap.deleted_at = DateTime.Now;
                    disposeLog.deleted_at = DateTime.Now;

                    if (previousLog != null)
                    {
                        if (previousLog.transaction_type == "STOCK")
                        {
                            var stockData = dbb.PE_BMS_Stock_Management.FirstOrDefault(x => x.spec_id == scrap.spec_id && x.deleted_at == null);
                            if (stockData != null)
                            {
                                stockData.quantity = (stockData.quantity ?? 0) + 1;
                                stockData.updated_at = DateTime.Now;
                            }
                        }
                        
                    }

                    dbb.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = $"Berhasil membatalkan Dispose untuk Tooling '{tooling_code}'." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    Exception deepestEx = ex;
                    while (deepestEx.InnerException != null)
                    {
                        deepestEx = deepestEx.InnerException;
                    }

                    return Json(new { success = false, message = "DB Error: " + deepestEx.Message });
                }
            }
        }

        // ==========================================
        //  Manage Inventory List
        // ==========================================

        [HttpGet]
        public JsonResult GetInventory()
        {
            try
            {
                var data = dbb.PE_BMS_Inventory_list
                              .Where(a => a.deleted_at == null)
                              .Select(s => new {
                                  inventory_id = s.inventory_id,
                                  url_image = s.url_image,
                                  category = s.category,
                                  name = s.name,
                                  Qty = s.Qty,
                                  created_at = s.created_at,
                                  updated_at = s.updated_at
                              })
                              .ToList();

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to retrieve Inventory data: " + errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetCategories()
        {
            try
            {
                var categories = dbb.PE_BMS_Inventory_list
                    .Where(x => x.deleted_at == null && x.category != null && x.category != "")
                    .Select(x => x.category)
                    .Distinct()
                    .ToList();

                return Json(new { success = true, data = categories }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CreateInventory(PE_BMS_Inventory_list dto, HttpPostedFileBase image_file)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.name))
                    return Json(new { success = false, message = "Nama Inventory wajib diisi." });

                // Proses Upload File
                if (image_file != null && image_file.ContentLength > 0)
                {
                    // Buat folder jika belum ada
                    string folderPath = Server.MapPath("~/Files/PE/BMS/Inventory/");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // Generate nama file unik agar tidak tertimpa
                    string fileName = DateTime.Now.ToString("yyyyMMddHHmmss_") + Path.GetFileName(image_file.FileName);
                    string savePath = Path.Combine(folderPath, fileName);

                    image_file.SaveAs(savePath);
                    dto.url_image = "/Files/PE/BMS/Inventory/" + fileName; // Path yang disimpan ke DB
                }

                dto.created_at = DateTime.Now;
                dbb.PE_BMS_Inventory_list.Add(dto);
                dbb.SaveChanges();

                return Json(new { success = true, message = "Inventory berhasil ditambahkan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal menyimpan: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateInventory(int id, PE_BMS_Inventory_list dto, HttpPostedFileBase image_file)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

            try
            {
                var item = dbb.PE_BMS_Inventory_list.FirstOrDefault(x => x.inventory_id == id && x.deleted_at == null);
                if (item == null) return Json(new { success = false, message = "Data tidak ditemukan." });

                // Proses Upload File jika ada file baru
                if (image_file != null && image_file.ContentLength > 0)
                {
                    string folderPath = Server.MapPath("~/Files/PE/BMS/Inventory/");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string fileName = DateTime.Now.ToString("yyyyMMddHHmmss_") + Path.GetFileName(image_file.FileName);
                    string savePath = Path.Combine(folderPath, fileName);

                    image_file.SaveAs(savePath);
                    item.url_image = "/Files/PE/BMS/Inventory/" + fileName; // Update dengan URL gambar baru
                }
                // Jika tidak upload file baru, URL lama tetap dipertahankan

                item.category = dto.category;
                item.name = dto.name;
                item.Qty = dto.Qty;
                item.updated_at = DateTime.Now;

                dbb.SaveChanges();

                return Json(new { success = true, message = "Inventory berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal mengupdate: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteInventory(int id)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");
            if (userAccess == null) return Json(new { success = false, message = "Forbidden" }, JsonRequestBehavior.AllowGet);

            try
            {
                // Soft Delete process
                var item = dbb.PE_BMS_Inventory_list.FirstOrDefault(x => x.inventory_id == id && x.deleted_at == null);
                if (item == null) return Json(new { success = false, message = "Inventory data not found or already deleted." });

                item.deleted_at = DateTime.Now;

                dbb.SaveChanges();

                return Json(new { success = true, message = "Inventory deleted successfully." });
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null) errorMessage += " | " + ex.InnerException.Message;
                return Json(new { success = false, message = "Failed to delete Inventory: " + errorMessage });
            }
        }

        [HttpGet]
        public ActionResult ManageInventory()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null)
            {
                return View("V401"); 
            }

            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }


            return View();

        }

        [HttpPost]
        public JsonResult SubmitInventoryUsage(InventoryUsageDTO data)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik);

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

            if (string.IsNullOrEmpty(data.project_name))
                return Json(new { success = false, message = "Nama project/pekerjaan wajib diisi." });

            if (data.details == null || data.details.Count == 0)
                return Json(new { success = false, message = "Komponen bahan/material wajib ditambahkan minimal 1." });

            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    // 1. Simpan Header Pekerjaan dengan Class/Tabel Baru
                    var header = new PE_BMS_Inventory_Usage_Header
                    {
                        project_name = data.project_name,
                        remarks = data.remarks,
                        created_by_nik = userNik,
                        created_by_name = userProfile != null ? userProfile.Name : userNik,
                        created_at = DateTime.Now
                    };
                    dbb.PE_BMS_Inventory_Usage_Header.Add(header);
                    dbb.SaveChanges(); // Menghasilkan header_id

                    // 2. Looping Komponen Detail
                    foreach (var d in data.details)
                    {
                        if (d.qty_used <= 0)
                            throw new Exception("Quantity material tidak boleh 0 atau minus.");

                        var item = dbb.PE_BMS_Inventory_list.FirstOrDefault(x => x.inventory_id == d.inventory_id && x.deleted_at == null);

                        if (item == null) throw new Exception($"Salah satu barang tidak ditemukan.");
                        if (item.Qty < d.qty_used) throw new Exception($"Stok '{item.name}' tidak mencukupi! Sisa stok {item.Qty}.");

                        // Kurangi Stok Master
                        item.Qty -= d.qty_used;
                        item.updated_at = DateTime.Now;

                        // Simpan Detail Penggunaan dengan Class/Tabel Baru
                        var detail = new PE_BMS_Inventory_Usage_Detail
                        {
                            header_id = header.header_id,
                            inventory_id = d.inventory_id,
                            qty_used = d.qty_used
                        };
                        dbb.PE_BMS_Inventory_Usage_Detail.Add(detail);
                    }

                    dbb.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Pekerjaan berhasil disubmit. Stok material telah dikurangi." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        // ==========================================
        //  EDIT / UPDATE Project & Adjust Stock
        // ==========================================
        [HttpPost]
        public JsonResult UpdateInventoryUsage(int header_id, InventoryUsageDTO data)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

            if (string.IsNullOrEmpty(data.project_name))
                return Json(new { success = false, message = "Nama project/pekerjaan wajib diisi." });

            if (data.details == null || data.details.Count == 0)
                return Json(new { success = false, message = "Komponen bahan/material wajib ditambahkan minimal 1." });

            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    // 1. Cari Header Lama
                    var header = dbb.PE_BMS_Inventory_Usage_Header.FirstOrDefault(x => x.header_id == header_id);
                    if (header == null) throw new Exception("Data Pekerjaan tidak ditemukan.");

                    // Update informasi teks header
                    header.project_name = data.project_name;
                    header.remarks = data.remarks;

                    // 2. KEMBALIKAN (REVERT) STOK LAMA
                    // Ambil detail lama, lalu kembalikan qty-nya ke master inventory
                    var oldDetails = dbb.PE_BMS_Inventory_Usage_Detail.Where(x => x.header_id == header_id).ToList();
                    foreach (var old in oldDetails)
                    {
                        var itemMaster = dbb.PE_BMS_Inventory_list.FirstOrDefault(x => x.inventory_id == old.inventory_id && x.deleted_at == null);
                        if (itemMaster != null)
                        {
                            itemMaster.Qty += old.qty_used; // Balikin stoknya
                        }
                    }

                    // 3. HAPUS DETAIL LAMA (Akan diganti dengan list material yang baru di-submit)
                    dbb.PE_BMS_Inventory_Usage_Detail.RemoveRange(oldDetails);
                    dbb.SaveChanges(); // Save point sementara di dalam transaksi

                    // 4. POTONG ULANG DENGAN STOK BARU (APPLY NEW)
                    foreach (var d in data.details)
                    {
                        if (d.qty_used <= 0) throw new Exception("Quantity material tidak boleh 0 atau minus.");

                        var itemMaster = dbb.PE_BMS_Inventory_list.FirstOrDefault(x => x.inventory_id == d.inventory_id && x.deleted_at == null);
                        if (itemMaster == null) throw new Exception("Salah satu barang tidak ditemukan di master inventory.");

                        // Cek apakah stok master (setelah direvert tadi) mencukupi untuk request yang baru
                        if (itemMaster.Qty < d.qty_used)
                            throw new Exception($"Stok '{itemMaster.name}' tidak mencukupi! Sisa stok saat ini (setelah kalkulasi ulang) adalah {itemMaster.Qty}.");

                        // Kurangi stok master
                        itemMaster.Qty -= d.qty_used;
                        itemMaster.updated_at = DateTime.Now;

                        // Insert detail baru
                        var newDetail = new PE_BMS_Inventory_Usage_Detail
                        {
                            header_id = header_id,
                            inventory_id = d.inventory_id,
                            qty_used = d.qty_used
                        };
                        dbb.PE_BMS_Inventory_Usage_Detail.Add(newDetail);
                    }

                    // 5. Commit Transaksi
                    dbb.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Data Pekerjaan berhasil diperbarui dan stok telah disesuaikan." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }


        [HttpPost]
        public JsonResult DeleteInventoryUsage(int header_id)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik && w.access == "Administrator");

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    var header = dbb.PE_BMS_Inventory_Usage_Header.FirstOrDefault(x => x.header_id == header_id);
                    if (header == null) throw new Exception("Data Pekerjaan tidak ditemukan.");

                    var details = dbb.PE_BMS_Inventory_Usage_Detail.Where(x => x.header_id == header_id).ToList();
                    foreach (var d in details)
                    {
                        var itemMaster = dbb.PE_BMS_Inventory_list.FirstOrDefault(x => x.inventory_id == d.inventory_id && x.deleted_at == null);
                        if (itemMaster != null)
                        {
                            itemMaster.Qty += d.qty_used; 
                            itemMaster.updated_at = DateTime.Now;
                        }
                    }

                    dbb.PE_BMS_Inventory_Usage_Detail.RemoveRange(details);
                    dbb.PE_BMS_Inventory_Usage_Header.Remove(header);     

                    dbb.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Data Pekerjaan berhasil dibatalkan dan seluruh stok material telah dikembalikan." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpGet]
        public ActionResult InventoryUsage()
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik );

            if (userAccess == null) return View("V403");

            return View();
        }

        // ==========================================
        //  READ: Manage Iventory Usage (List Page)
        // ==========================================
        [HttpGet]
        public JsonResult GetInventoryUsageList()
        {
            try
            {
                // Tarik data header sekaligus Sub-Query untuk menarik detail material-nya
                var data = dbb.PE_BMS_Inventory_Usage_Header
                              .OrderByDescending(x => x.created_at)
                              .Select(x => new {
                                  header_id = x.header_id,
                                  project_name = x.project_name,
                                  remarks = x.remarks,
                                  created_by_name = x.created_by_name,
                                  created_at = x.created_at,

                                  // Join manual ke tabel detail dan master inventory
                                  details = dbb.PE_BMS_Inventory_Usage_Detail
                                               .Where(d => d.header_id == x.header_id)
                                               .Join(dbb.PE_BMS_Inventory_list,
                                                     d => d.inventory_id,
                                                     i => i.inventory_id,
                                                     (d, i) => new {
                                                         item_name = i.name,
                                                         qty = d.qty_used
                                                     }).ToList()
                              }).ToList();

                // Format hasil untuk frontend
                var formattedData = data.Select(x => new {
                    x.header_id,
                    x.project_name,
                    x.remarks,
                    x.created_by_name,
                    created_at = x.created_at.ToString("dd-MM-yyyy HH:mm"),
                    materials = x.details 
                });

                return Json(new { success = true, data = formattedData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetInventoryUsageDetails(int header_id)
        {
            try
            {
                var details = (from d in dbb.PE_BMS_Inventory_Usage_Detail
                               join i in dbb.PE_BMS_Inventory_list on d.inventory_id equals i.inventory_id
                               where d.header_id == header_id
                               select new
                               {
                                   detail_id = d.detail_id,
                                   inventory_id = d.inventory_id,
                                   item_name = i.name,
                                   category = i.category,
                                   qty_used = d.qty_used
                               }).ToList();

                return Json(new { success = true, data = details }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetAllInventoryListForDropdown()
        {
            try
            {
                var data = dbb.PE_BMS_Inventory_list
                              .Where(a => a.deleted_at == null)
                              .Select(s => new {
                                  id = s.inventory_id,
                                  text = s.name + " (" + s.category + ")",
                                  max_qty = s.Qty 
                              })
                              .ToList();

                return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==========================================
        //  Manage Workshop Request
        // ==========================================

        [HttpGet]
        public ActionResult WorkshopRequest()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();


            if (userProfile == null)
            {
                return View("V401"); // Unauthorized
            }



            // Optional: Kirim nama user ke View jika ingin ditampilkan di UI
            ViewBag.UserName = userProfile.Name;
            return View();
        }

        [HttpGet]
        public JsonResult GetMyRequestHistory()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();

            try
            {
                var history = dbb.PE_BMS_Workshop_Request_Form
                                 .Where(w => w.request_by_nik == userNik)
                                 .OrderByDescending(o => o.create_at)
                                 .Select(s => new {
                                     form_id = s.form_id,
                                     department = s.department,
                                     request = s.request,
                                     request_type = s.request_type,
                                     request_qty = s.request_qty,
                                     due_date = s.due_date,
                                     status = s.status,
                                     create_at = s.create_at
                                 }).ToList();

                // Format tanggal agar mudah dibaca di frontend
                var formattedData = history.Select(x => new
                {
                    x.form_id,
                    x.department,
                    x.request,
                    x.request_type,
                    x.request_qty,
                    x.status,
                    due_date = x.due_date.ToString("yyyy-MM-dd"),
                    create_at = x.create_at.ToString("yyyy-MM-dd HH:mm")
                });

                return Json(new { success = true, data = formattedData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SubmitWorkshopRequest(PE_BMS_Workshop_Request_Form dto,
                                        HttpPostedFileBase file_request_image,
                                        HttpPostedFileBase file_reference_image)

        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser.GetUserId();
            var userProfile = db.V_Users_Active.Where(w => w.NIK == userNik).FirstOrDefault();

            if (userProfile == null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                string folderPath = Server.MapPath("~/Files/PE/BMS/WorkshopRequests/");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                if (file_request_image != null && file_request_image.ContentLength > 0)
                {
                    string fileName = $"{timestamp}_REQ_{Path.GetFileName(file_request_image.FileName)}";
                    file_request_image.SaveAs(Path.Combine(folderPath, fileName));
                    dto.request_image = "/Files/PE/BMS/WorkshopRequests/" + fileName;
                }

                if (file_reference_image != null && file_reference_image.ContentLength > 0)
                {
                    string fileName = $"{timestamp}_REF_{Path.GetFileName(file_reference_image.FileName)}";
                    file_reference_image.SaveAs(Path.Combine(folderPath, fileName));
                    dto.reference_image = "/Files/PE/BMS/WorkshopRequests/" + fileName;
                }

               
                dto.status = "Pending";
                dto.create_at = DateTime.Now;
                dto.update_at = DateTime.Now;
                dto.request_by_nik = userNik;
                dto.request_by_name = userProfile.Name;

                dbb.PE_BMS_Workshop_Request_Form.Add(dto);
                dbb.SaveChanges();

                string referalId = dto.form_id.ToString();

                var listNik = dbb.PE_BMS_Access.ToList();
                var approvalList = new List<Task_Approval_List>();

                foreach (var access in listNik)
                {
                    approvalList.Add(new Task_Approval_List
                    {
                        NIK = access.NIK, 
                        Menu_Id = 1059,
                        Module = "PE Workshop Request",
                        Referal_id = referalId,
                        TaskType = "Accept",
                        ApprovalDeadline = DateTime.Now.AddDays(1),
                        DetailUrl = $"~/PE/BMS/DataWorkshopRequest?id={referalId}",
                        TaskTitle = $"Workshop Request : {dto.request} ({referalId})", 
                        DocumentID = null
                    });
                }

                Helpers.ApprovalHelper.SaveApprovalList(approvalList);

                return Json(new { success = true, message = "Workshop request berhasil disubmit." });
            }
            catch (Exception ex)
            {
                Exception deepestEx = ex;
                while (deepestEx.InnerException != null)
                {
                    deepestEx = deepestEx.InnerException;
                }

                return Json(new { success = false, message = "DB Error: " + deepestEx.Message });
            }
        }

        // ==========================================
        //  WORKSHOP POV (Workshop Dashboard)
        // ==========================================

        [HttpGet]
        public ActionResult DataWorkshopRequest()
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();

            var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
            if (userProfile == null) return View("V401");

            var userAccess = dbb.PE_BMS_Access.Where(w => w.NIK == userNik).FirstOrDefault();
            if (userAccess == null)
            {
                return View("V403");
            }

            ViewBag.NavHide = true;
            return View();
        }

        [HttpGet]
        public ActionResult InventoryList()
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();

            var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);
            if (userProfile == null) return View("V401");


            return View();
        }

        [HttpGet]
        public JsonResult GetAllWorkshopRequests()
        {
            try
            {
                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();
                var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik );

                if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

                var data = dbb.PE_BMS_Workshop_Request_Form
                              .OrderByDescending(o => o.create_at)
                              .Select(s => new {
                                  form_id = s.form_id,
                                  department = s.department,
                                  request = s.request,
                                  request_type = s.request_type,
                                  request_desc = s.request_desc,
                                  request_qty = s.request_qty,
                                  due_date = s.due_date,
                                  status = s.status,
                                  create_at = s.create_at,
                                  request_by_name = s.request_by_name,
                                  request_image = s.request_image,
                                  reference_image = s.reference_image
                              }).ToList();

                var formattedData = data.Select(x => new
                {
                    x.form_id,
                    x.department,
                    x.request,
                    x.request_type,
                    x.request_desc,
                    x.request_qty,
                    x.status,
                    x.request_by_name,
                    x.request_image,
                    x.reference_image,
                    due_date = x.due_date.ToString("yyyy-MM-dd"),
                    create_at = x.create_at.ToString("yyyy-MM-dd HH:mm")
                });

                return Json(new { success = true, data = formattedData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult AcceptWorkshopRequest(int form_id)
        {
            try
            {

                var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
                string userNik = currUser?.GetUserId();
                var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik);

                if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

                var request = dbb.PE_BMS_Workshop_Request_Form.FirstOrDefault(x => x.form_id == form_id);
                if (request == null) return Json(new { success = false, message = "Data tidak ditemukan." });

                if (request.status != "Pending")
                    return Json(new { success = false, message = "Hanya request berstatus Pending yang dapat di-accept." });

                request.status = "On Progress";
                request.update_at = DateTime.Now;

                dbb.SaveChanges();

                var menuId = 1059;
                var referalId = form_id.ToString();
                var taskType = "Accept";

                Helpers.ApprovalHelper.CompleteApprovalTaskForGroup(menuId, referalId, taskType);

                var listNik = dbb.PE_BMS_Access.ToList();
                var approvalList = new List<Task_Approval_List>();

                foreach (var access in listNik)
                {
                    approvalList.Add(new Task_Approval_List
                    {
                        NIK = access.NIK,
                        Menu_Id = menuId,
                        Module = "PE Workshop Request",
                        Referal_id = referalId,
                        TaskType = "Execution",
                        ApprovalDeadline = request.due_date,
                        DetailUrl = $"~/PE/BMS/DataWorkshopRequest?id={referalId}",
                        TaskTitle = $"Workshop Execution : {request.request} ({referalId})",
                        DocumentID = null
                    });
                }

                Helpers.ApprovalHelper.SaveApprovalList(approvalList);


                return Json(new { success = true, message = "Request berhasil diterima dan masuk ke tahap On Progress." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Gagal memproses: " + ex.Message });
            }
        }

        public JsonResult GetSectionList()
        {
            var sectionList = db.Users_Section_AX
                                .Select(x => x.COSTNAME)
                                .Distinct()
                                .ToList();

            return Json(sectionList, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult FinishWorkshopRequest(int form_id, HttpPostedFileBase finished_image, List<InventoryDetailDTO> details)
        {
            var currUser = (System.Security.Claims.ClaimsIdentity)User.Identity;
            string userNik = currUser?.GetUserId();
            var userProfile = db.V_Users_Active.FirstOrDefault(w => w.NIK == userNik);

            if (userProfile == null) return Json(new { success = false, message = "Unauthorized" });


            var userAccess = dbb.PE_BMS_Access.FirstOrDefault(w => w.NIK == userNik);

            if (userAccess == null) return Json(new { success = false, message = "Forbidden" });

            using (var transaction = dbb.Database.BeginTransaction())
            {
                try
                {
                    var req = dbb.PE_BMS_Workshop_Request_Form.FirstOrDefault(x => x.form_id == form_id);
                    if (req == null) throw new Exception("Data Request tidak ditemukan.");
                    if (req.status != "On Progress") throw new Exception("Hanya request On Progress yang bisa diselesaikan.");
                    if (finished_image == null || finished_image.ContentLength == 0) throw new Exception("Foto hasil jadi wajib dilampirkan.");

                    string folderPath = Server.MapPath("~/Files/PE/BMS/WorkshopFinished/");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}_DONE_{Path.GetFileName(finished_image.FileName)}";
                    finished_image.SaveAs(Path.Combine(folderPath, fileName));

                    req.finished_image = "/Files/PE/BMS/WorkshopFinished/" + fileName;
                    req.status = "Done";
                    req.update_at = DateTime.Now;

                    if (details != null && details.Count > 0)
                    {
                        var header = new PE_BMS_Inventory_Usage_Header
                        {
                            project_name = $"Workshop Req #{req.form_id} - {req.request}",
                            remarks = "Auto-generated from Workshop Done Process",
                            created_by_nik = req.request_by_nik,
                            created_by_name = req.request_by_name,
                            created_at = DateTime.Now
                        };
                        dbb.PE_BMS_Inventory_Usage_Header.Add(header);
                        dbb.SaveChanges(); 

                        foreach (var d in details)
                        {
                            if (d.qty_used <= 0) throw new Exception("Quantity material tidak boleh 0 atau minus.");

                            var item = dbb.PE_BMS_Inventory_list.FirstOrDefault(x => x.inventory_id == d.inventory_id && x.deleted_at == null);
                            if (item == null) throw new Exception("Salah satu material tidak ditemukan di inventory.");
                            if (item.Qty < d.qty_used) throw new Exception($"Stok '{item.name}' tidak mencukupi! Sisa stok {item.Qty}.");

                            item.Qty -= d.qty_used; 
                            item.updated_at = DateTime.Now;

                            var usageDetail = new PE_BMS_Inventory_Usage_Detail
                            {
                                header_id = header.header_id,
                                inventory_id = d.inventory_id,
                                qty_used = d.qty_used
                            };
                            dbb.PE_BMS_Inventory_Usage_Detail.Add(usageDetail);
                        }
                    }

                    dbb.SaveChanges();
                    transaction.Commit();

                    var menuId = 1059;
                    var referalId = form_id.ToString();
                    var taskType = "Execution";

                    Helpers.ApprovalHelper.CompleteApprovalTaskForGroup(menuId, referalId, taskType);

                    return Json(new { success = true, message = "Pekerjaan berhasil diselesaikan." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Exception deepestEx = ex;
                    while (deepestEx.InnerException != null) deepestEx = deepestEx.InnerException;
                    return Json(new { success = false, message = deepestEx.Message });
                }
            }
        }
    }
}