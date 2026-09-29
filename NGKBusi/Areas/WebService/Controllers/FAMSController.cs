using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.WebService.Controllers
{
    public class FAMSController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        public ActionResult Index()
        {
            ViewBag.AssetList = db.V_Asset.ToList();

            return View();
        }

        // GET: WebService/FAMS
        [HttpGet]
        public JsonResult getAssetData()
        {
            var id = Request.QueryString["id"];
            if(id == "All")
            {
                var asset = db.V_Asset.ToList();
                return Json(asset, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var asset = db.V_Asset.Where(w => w.AssetID == id).ToList();
                return Json(asset, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpGet]
        public JsonResult getAssetLocation()
        {
            var assetLocation = db.FA_Asset_Location.ToList();

            return Json(assetLocation, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult setAssetAudit(String AssetNo, String Descr, String Section, String Location, String NIK)
        {
            var sect = Section == "Choose a section" ? null : Section;
            var loc = Location == "Choose a location" ? null : Location;
            var assetAudit = db.FA_Asset_Audit.Where(w => w.Asset_No == AssetNo && w.Period == DateTime.Now.Year).FirstOrDefault();
            var asset = db.V_Asset.Where(w => w.AssetID == AssetNo).ToList();

            if (assetAudit != null)
            {
                assetAudit.Descr = Descr;
                assetAudit.Section = sect;
                assetAudit.Location = loc;
                assetAudit.User_By = NIK;
                assetAudit.Date_At = DateTime.Now;
            }
            else
            {
                db.FA_Asset_Audit.Add(new FA_Asset_Audit
                {
                    Period = DateTime.Now.Year,
                    Asset_No = AssetNo,
                    Descr = Descr,
                    Section = sect,
                    Location = loc,
                    User_By = NIK,
                    Date_At = DateTime.Now

                });
            }
            db.SaveChanges();
            return Json(asset, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult getLogin(String NIK,String Password)
        {
            var user = db.V_Users_Active.Where(w => w.NIK == NIK && w.Password == Password).Select(s => new { s.NIK,s.Name });

            return Json(user, JsonRequestBehavior.AllowGet);
        }
    }
}