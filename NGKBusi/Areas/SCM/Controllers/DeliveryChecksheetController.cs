using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;

namespace NGKBusi.Areas.SCM.Controllers
{
    [Authorize]
    public class DeliveryChecksheetController : Controller
    {
        private DefaultConnection db = new DefaultConnection();
        private WHFGConnection whfg = new WHFGConnection();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Avoid infinite redirect loop when rendering AccessDenied action
            string actionName = filterContext.ActionDescriptor.ActionName;
            if (actionName.Equals("AccessDenied", StringComparison.OrdinalIgnoreCase))
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            if (User.Identity.IsAuthenticated)
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
                
                // Only allow access to users registered and active in WHFG_Checksheet_Users
                bool hasAccess = whfg.ChecksheetUsers.Any(u => u.NIK == currUser && u.IsActive);
                if (!hasAccess)
                {
                    filterContext.Result = RedirectToAction("AccessDenied");
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }

        // GET: SCM/DeliveryChecksheet/AccessDenied
        public ActionResult AccessDenied()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.UserNIK = ((ClaimsIdentity)User.Identity).GetUserId();
            }
            else
            {
                ViewBag.UserNIK = "Not Logged In";
            }
            return View();
        }

        // GET: SCM/DeliveryChecksheet
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var userDetail = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);
            if (userDetail == null)
            {
                return HttpNotFound("User active record not found.");
            }

            bool isAdmin = whfg.ChecksheetUsers.Any(u => u.NIK == currUser && u.IsActive && u.IsAdmin);

            List<WHFG_VehicleChecksheet_Header> logs;
            if (isAdmin)
            {
                logs = whfg.Headers
                    .Include(h => h.Vehicle)
                    .Include(h => h.Destination)
                    .OrderByDescending(h => h.CreatedDate)
                    .ToList();
            }
            else
            {
                logs = whfg.Headers
                    .Include(h => h.Vehicle)
                    .Include(h => h.Destination)
                    .Where(h => h.CreatedBy == currUser)
                    .OrderByDescending(h => h.CreatedDate)
                    .ToList();
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserNIK = currUser;
            ViewBag.CurrentDate = DateTime.Today;

            return View(logs);
        }

        // GET: SCM/DeliveryChecksheet/Create
        public ActionResult Create()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var userDetail = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);
            if (userDetail == null)
            {
                return HttpNotFound("User active record not found.");
            }

            ViewBag.Vehicles = whfg.Vehicles.Where(v => v.IsActive).OrderBy(v => v.LicensePlate).ToList();
            ViewBag.Destinations = whfg.Destinations.Where(d => d.IsActive).OrderBy(d => d.DestinationName).ToList();
            ViewBag.Items = whfg.Items.Where(i => i.IsActive).OrderBy(i => i.ItemNumber).ToList();
            ViewBag.DriverName = userDetail.Name;
            ViewBag.DriverNIK = userDetail.NIK;
            ViewBag.CheckDate = DateTime.Today;

            return View();
        }

        // POST: SCM/DeliveryChecksheet/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int vehicleId, int destinationId, string remarks)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var userDetail = db.V_Users_Active.FirstOrDefault(w => w.NIK == currUser);
            if (userDetail == null)
            {
                return HttpNotFound("User active record not found.");
            }

            var items = whfg.Items.Where(i => i.IsActive).ToList();

            // Validate that every check item is submitted
            foreach (var item in items)
            {
                string val = Request.Form["result_" + item.ItemID];
                if (string.IsNullOrEmpty(val))
                {
                    ModelState.AddModelError("", "Semua item pemeriksaan harus diisi.");
                    break;
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Vehicles = whfg.Vehicles.Where(v => v.IsActive).OrderBy(v => v.LicensePlate).ToList();
                ViewBag.Destinations = whfg.Destinations.Where(d => d.IsActive).OrderBy(d => d.DestinationName).ToList();
                ViewBag.Items = whfg.Items.Where(i => i.IsActive).OrderBy(i => i.ItemNumber).ToList();
                ViewBag.DriverName = userDetail.Name;
                ViewBag.DriverNIK = userDetail.NIK;
                ViewBag.CheckDate = DateTime.Today;
                return View();
            }

            using (var transaction = whfg.Database.BeginTransaction())
            {
                try
                {
                    var header = new WHFG_VehicleChecksheet_Header
                    {
                        Supplier = "PT. NITERRA MOBILITY INDONESIA",
                        DriverName = userDetail.Name,
                        DriverNIK = userDetail.NIK,
                        VehicleID = vehicleId,
                        DestinationID = destinationId,
                        CheckDate = DateTime.Today,
                        Remarks = remarks,
                        CreatedBy = currUser,
                        CreatedDate = DateTime.Now
                    };

                    whfg.Headers.Add(header);
                    whfg.SaveChanges();

                    foreach (var item in items)
                    {
                        string val = Request.Form["result_" + item.ItemID];
                        var detail = new WHFG_VehicleChecksheet_Detail
                        {
                            HeaderID = header.HeaderID,
                            ItemID = item.ItemID,
                            ResultValue = val
                        };
                        whfg.Details.Add(detail);
                    }

                    whfg.SaveChanges();
                    transaction.Commit();
                    TempData["Success"] = "Checksheet kendaraan operasional berhasil disimpan.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan data: " + ex.Message);
                }
            }

            ViewBag.Vehicles = whfg.Vehicles.Where(v => v.IsActive).OrderBy(v => v.LicensePlate).ToList();
            ViewBag.Destinations = whfg.Destinations.Where(d => d.IsActive).OrderBy(d => d.DestinationName).ToList();
            ViewBag.Items = whfg.Items.Where(i => i.IsActive).OrderBy(i => i.ItemNumber).ToList();
            ViewBag.DriverName = userDetail.Name;
            ViewBag.DriverNIK = userDetail.NIK;
            ViewBag.CheckDate = DateTime.Today;
            return View();
        }

        // GET: SCM/DeliveryChecksheet/Edit/5
        public ActionResult Edit(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var header = whfg.Headers.FirstOrDefault(h => h.HeaderID == id);
            if (header == null)
            {
                return HttpNotFound("Checksheet tidak ditemukan.");
            }

            // Validation: only owner same-day edit
            if (header.CreatedBy != currUser || header.CheckDate != DateTime.Today)
            {
                TempData["Error"] = "Anda tidak diizinkan mengubah checksheet ini atau checksheet sudah melewati hari pengisian.";
                return RedirectToAction("Index");
            }

            ViewBag.Vehicles = whfg.Vehicles.Where(v => v.IsActive).OrderBy(v => v.LicensePlate).ToList();
            ViewBag.Destinations = whfg.Destinations.Where(d => d.IsActive).OrderBy(d => d.DestinationName).ToList();
            ViewBag.Items = whfg.Items.Where(i => i.IsActive).OrderBy(i => i.ItemNumber).ToList();
            ViewBag.Header = header;
            ViewBag.Details = whfg.Details.Where(d => d.HeaderID == id).ToDictionary(d => d.ItemID, d => d.ResultValue);

            return View();
        }

        // POST: SCM/DeliveryChecksheet/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, int vehicleId, int destinationId, string remarks)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var header = whfg.Headers.FirstOrDefault(h => h.HeaderID == id);
            if (header == null)
            {
                return HttpNotFound("Checksheet tidak ditemukan.");
            }

            // Validation: only owner same-day edit
            if (header.CreatedBy != currUser || header.CheckDate != DateTime.Today)
            {
                TempData["Error"] = "Anda tidak diizinkan mengubah checksheet ini atau checksheet sudah melewati hari pengisian.";
                return RedirectToAction("Index");
            }

            var items = whfg.Items.Where(i => i.IsActive).ToList();

            // Validate that every check item is submitted
            foreach (var item in items)
            {
                string val = Request.Form["result_" + item.ItemID];
                if (string.IsNullOrEmpty(val))
                {
                    ModelState.AddModelError("", "Semua item pemeriksaan harus diisi.");
                    break;
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Vehicles = whfg.Vehicles.Where(v => v.IsActive).OrderBy(v => v.LicensePlate).ToList();
                ViewBag.Destinations = whfg.Destinations.Where(d => d.IsActive).OrderBy(d => d.DestinationName).ToList();
                ViewBag.Items = whfg.Items.Where(i => i.IsActive).OrderBy(i => i.ItemNumber).ToList();
                ViewBag.Header = header;
                ViewBag.Details = whfg.Details.Where(d => d.HeaderID == id).ToDictionary(d => d.ItemID, d => d.ResultValue);
                return View();
            }

            using (var transaction = whfg.Database.BeginTransaction())
            {
                try
                {
                    header.VehicleID = vehicleId;
                    header.DestinationID = destinationId;
                    header.Remarks = remarks;
                    header.UpdatedBy = currUser;
                    header.UpdatedDate = DateTime.Now;

                    // Clear old details
                    var oldDetails = whfg.Details.Where(d => d.HeaderID == id);
                    whfg.Details.RemoveRange(oldDetails);

                    // Insert new details
                    foreach (var item in items)
                    {
                        string val = Request.Form["result_" + item.ItemID];
                        var detail = new WHFG_VehicleChecksheet_Detail
                        {
                            HeaderID = header.HeaderID,
                            ItemID = item.ItemID,
                            ResultValue = val
                        };
                        whfg.Details.Add(detail);
                    }

                    whfg.SaveChanges();
                    transaction.Commit();
                    TempData["Success"] = "Checksheet kendaraan operasional berhasil diubah.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan perubahan: " + ex.Message);
                }
            }

            ViewBag.Vehicles = whfg.Vehicles.Where(v => v.IsActive).OrderBy(v => v.LicensePlate).ToList();
            ViewBag.Destinations = whfg.Destinations.Where(d => d.IsActive).OrderBy(d => d.DestinationName).ToList();
            ViewBag.Items = whfg.Items.Where(i => i.IsActive).OrderBy(i => i.ItemNumber).ToList();
            ViewBag.Header = header;
            ViewBag.Details = whfg.Details.Where(d => d.HeaderID == id).ToDictionary(d => d.ItemID, d => d.ResultValue);
            return View();
        }

        // POST: SCM/DeliveryChecksheet/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var header = whfg.Headers.FirstOrDefault(h => h.HeaderID == id);
            if (header == null)
            {
                return HttpNotFound("Checksheet tidak ditemukan.");
            }

            // Validation: only owner same-day delete
            if (header.CreatedBy != currUser || header.CheckDate != DateTime.Today)
            {
                TempData["Error"] = "Anda tidak diizinkan menghapus checksheet ini atau checksheet sudah melewati hari pengisian.";
                return RedirectToAction("Index");
            }

            try
            {
                whfg.Headers.Remove(header);
                whfg.SaveChanges();
                TempData["Success"] = "Checksheet kendaraan operasional berhasil dihapus.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Terjadi kesalahan saat menghapus data: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: SCM/DeliveryChecksheet/Detail/5
        public ActionResult Detail(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var header = whfg.Headers
                .Include(h => h.Vehicle)
                .Include(h => h.Destination)
                .FirstOrDefault(h => h.HeaderID == id);

            if (header == null)
            {
                return HttpNotFound("Checksheet tidak ditemukan.");
            }

            bool isAdmin = whfg.ChecksheetUsers.Any(u => u.NIK == currUser && u.IsActive && u.IsAdmin);

            // Access check: only admins or the creator
            if (!isAdmin && header.CreatedBy != currUser)
            {
                TempData["Error"] = "Anda tidak diizinkan melihat checksheet ini.";
                return RedirectToAction("Index");
            }

            ViewBag.Header = header;
            ViewBag.Items = whfg.Items.Where(i => i.IsActive).OrderBy(i => i.ItemNumber).ToList();
            ViewBag.Details = whfg.Details.Where(d => d.HeaderID == id).ToDictionary(d => d.ItemID, d => d.ResultValue);

            return View();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                whfg.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}