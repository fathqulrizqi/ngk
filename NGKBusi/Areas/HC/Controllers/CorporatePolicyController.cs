using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.HC.Controllers
{
    [Authorize]
    public class CorporatePolicyController : Controller
    {
        private CorporatePolicyConnection cpDb = new CorporatePolicyConnection();
        private DefaultConnection db = new DefaultConnection();

        private bool CheckIsHrdAdmin(string currUser)
        {
            if (string.IsNullOrEmpty(currUser)) return false;
            return currUser == "863.09.22" || currUser == "592.02.10";
        }

        // GET: HC/CorporatePolicy
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            bool isHrdAdmin = CheckIsHrdAdmin(currUser);
            
            var policies = cpDb.CorporatePolicies.OrderByDescending(o => o.CreatedAt).ToList();

            ViewBag.IsHrdAdmin = isHrdAdmin;
            ViewBag.Policies = policies;
            ViewBag.Nik = currUser;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadPolicy(string PolicyName, HttpPostedFileBase file)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            bool isHrdAdmin = CheckIsHrdAdmin(currUser);

            if (!isHrdAdmin)
            {
                return Json(new { status = 0, msg = "Unauthorized." });
            }

            if (file == null || file.ContentLength == 0 || string.IsNullOrEmpty(PolicyName))
            {
                return Json(new { status = 0, msg = "Policy Name and PDF file are required." });
            }

            try
            {
                string ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".pdf")
                {
                    return Json(new { status = 0, msg = "Only PDF files are allowed." });
                }

                string targetFolder = Server.MapPath("~/Files/HC/CorporatePolicy/");
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                // Avoid collision by appending timestamp
                string rawName = Path.GetFileNameWithoutExtension(file.FileName);
                string uniqueFileName = $"{rawName}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                string targetPath = Path.Combine(targetFolder, uniqueFileName);

                file.SaveAs(targetPath);

                var policy = new CorporatePolicyModel
                {
                    PolicyName = PolicyName,
                    FileName = uniqueFileName,
                    IsActive = true,
                    CreatedBy = currUser,
                    CreatedAt = DateTime.Now
                };

                cpDb.CorporatePolicies.Add(policy);
                cpDb.SaveChanges();

                return Json(new { status = 1, msg = "Policy successfully uploaded." });
            }
            catch (Exception ex)
            {
                return Json(new { status = 0, msg = "Upload failed: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult MoveToArchive(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            bool isHrdAdmin = CheckIsHrdAdmin(currUser);

            if (!isHrdAdmin)
            {
                return Json(new { status = 0, msg = "Unauthorized." });
            }

            try
            {
                var policy = cpDb.CorporatePolicies.FirstOrDefault(p => p.ID == id);
                if (policy == null)
                {
                    return Json(new { status = 0, msg = "Policy not found." });
                }

                // Paths
                string sourceFolder = Server.MapPath("~/Files/HC/CorporatePolicy/");
                string archiveFolder = Server.MapPath("~/Files/HC/CorporatePolicy/archive/");
                
                if (!Directory.Exists(archiveFolder))
                {
                    Directory.CreateDirectory(archiveFolder);
                }

                string sourcePath = Path.Combine(sourceFolder, policy.FileName);
                string destPath = Path.Combine(archiveFolder, policy.FileName);

                // Move file physically
                if (System.IO.File.Exists(sourcePath))
                {
                    if (System.IO.File.Exists(destPath))
                    {
                        System.IO.File.Delete(destPath);
                    }
                    System.IO.File.Move(sourcePath, destPath);
                }

                policy.IsActive = false;
                policy.UpdatedBy = currUser;
                policy.UpdatedAt = DateTime.Now;

                cpDb.SaveChanges();

                return Json(new { status = 1, msg = "Policy moved to archive." });
            }
            catch (Exception ex)
            {
                return Json(new { status = 0, msg = "Archiving failed: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult RestoreFromArchive(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            bool isHrdAdmin = CheckIsHrdAdmin(currUser);

            if (!isHrdAdmin)
            {
                return Json(new { status = 0, msg = "Unauthorized." });
            }

            try
            {
                var policy = cpDb.CorporatePolicies.FirstOrDefault(p => p.ID == id);
                if (policy == null)
                {
                    return Json(new { status = 0, msg = "Policy not found." });
                }

                // Paths
                string sourceFolder = Server.MapPath("~/Files/HC/CorporatePolicy/");
                string archiveFolder = Server.MapPath("~/Files/HC/CorporatePolicy/archive/");

                string sourcePath = Path.Combine(archiveFolder, policy.FileName);
                string destPath = Path.Combine(sourceFolder, policy.FileName);

                // Move file physically back to main folder
                if (System.IO.File.Exists(sourcePath))
                {
                    if (System.IO.File.Exists(destPath))
                    {
                        System.IO.File.Delete(destPath);
                    }
                    System.IO.File.Move(sourcePath, destPath);
                }

                policy.IsActive = true;
                policy.UpdatedBy = currUser;
                policy.UpdatedAt = DateTime.Now;

                cpDb.SaveChanges();

                return Json(new { status = 1, msg = "Policy restored to active." });
            }
            catch (Exception ex)
            {
                return Json(new { status = 0, msg = "Restoring failed: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeletePolicy(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            bool isHrdAdmin = CheckIsHrdAdmin(currUser);

            if (!isHrdAdmin)
            {
                return Json(new { status = 0, msg = "Unauthorized." });
            }

            try
            {
                var policy = cpDb.CorporatePolicies.FirstOrDefault(p => p.ID == id);
                if (policy == null)
                {
                    return Json(new { status = 0, msg = "Policy not found." });
                }

                string folder = policy.IsActive ? "~/Files/HC/CorporatePolicy/" : "~/Files/HC/CorporatePolicy/archive/";
                string filePath = Path.Combine(Server.MapPath(folder), policy.FileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                cpDb.CorporatePolicies.Remove(policy);
                cpDb.SaveChanges();

                return Json(new { status = 1, msg = "Policy deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = 0, msg = "Deletion failed: " + ex.Message });
            }
        }
    }
}
