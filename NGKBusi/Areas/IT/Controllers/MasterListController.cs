using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace NGKBusi.Areas.IT.Controllers
{
    public class MasterListController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: IT/MasterList
        [Authorize]
        public ActionResult Index()
        {
            ViewBag.Users = db.V_Users_Active.ToList();
            ViewBag.MasterList = db.IT_Master_List_Data.ToList();
            return View();
        }

        [Authorize]
        public ActionResult Form()
        {
            ViewBag.Users = db.V_Users_Active.ToList();
            ViewBag.MasterList = db.IT_Master_List_Data.ToList();
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult insertList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            db.IT_Master_List_Data.Add(new IT_Master_List_Data
            {
                NIK = Request["iNIK"],
                Asset_No = Request["iAssetNo"],
                brand = Request["iBrand"],
                model = Request["iModel"],
                computerName = Request["iComputerName"],
                processor = Request["iProcessor"],
                MSOffice = Request["iMSOffice"],
                MSOffice_User = Request["iMSOfficeUser"],
                RAM = Request["iRAM"],
                HDD = Request["iHDD"],
                OS = Request["iOS"],
                IP = Request["iIP"],
                Mac_Address = Request["iAnydesk"],
                Anydesk = Request["iMacAddress"],
                purchase = Int32.Parse(Request["iPurchase"]),
                month = Int32.Parse(Request["iMonth"]),
                type = Request["iType"],
                is_used = Request["iUsed"] == "1"?true:false,
                created_at = DateTime.Now,
                created_by = currUser.GetUserId(),
            });
            db.SaveChanges();

            return RedirectToAction("Index", "MasterList", new { area = "IT" });
        }
        [HttpPost]
        [Authorize]
        public ActionResult editList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Int32.Parse(Request["iDataID"]);
            var currData = db.IT_Master_List_Data.Where(x => x.id == currID).FirstOrDefault();

            currData.NIK = Request["iNIK"];
            currData.Asset_No = Request["iAssetNo"];
            currData.brand = Request["iBrand"];
            currData.model = Request["iModel"];
            currData.computerName = Request["iComputerName"];
            currData.processor = Request["iProcessor"];
            currData.MSOffice = Request["iMSOffice"];
            currData.MSOffice_User = Request["iMSOfficeUser"];
            currData.RAM = Request["iRAM"];
            currData.HDD = Request["iHDD"];
            currData.OS = Request["iOS"];
            currData.IP = Request["iIP"];
            currData.Mac_Address = Request["iAnydesk"];
            currData.Anydesk = Request["iMacAddress"];
            currData.purchase = Int32.Parse(Request["iPurchase"]);
            currData.month = Int32.Parse(Request["iMonth"]);
            currData.type = Request["iType"];
            currData.is_used = Request["iUsed"] == "1" ? true : false;
            currData.updated_at = DateTime.Now;
            currData.updated_by = currUser.GetUserId();
            db.SaveChanges();

            return RedirectToAction("Index", "MasterList", new { area = "IT" });
        }
        [HttpPost]
        [Authorize]
        public ActionResult deleteList()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Int32.Parse(Request["currDataID"]);
            var currData = db.IT_Master_List_Data.Where(x => x.id == currID).FirstOrDefault();
            db.IT_Master_List_Data.Remove(currData);
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }
    }
}