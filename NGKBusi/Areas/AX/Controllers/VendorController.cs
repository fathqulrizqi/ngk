using Microsoft.AspNet.Identity;
using NGKBusi.Areas.AX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.AX.Controllers
{
    public class VendorController : Controller
    {
        VendorConnection dbV = new VendorConnection();
        // GET: AX/Vendor
        public ActionResult List()
        {
            var currAccNumber = Request["AccNumber"];

            ViewBag.CurrData = dbV.AX_Vendor_List.Where(w => w.AccountNum == currAccNumber).OrderByDescending(o => o.AccountNum).FirstOrDefault();
            ViewBag.CurrDataList = dbV.AX_Vendor_List.OrderByDescending(o => o.AccountNum).ToList();

            return View();
        }

        public ActionResult Add()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var currAccountNum = Request["iCurrAccNumber"];
            var accountNum = Request["iAccountNum"];
            var currVendorGroup = Request["iVendorGroup"];
            var currVendorName = Request["iVendorName"];
            var currVendorSearchName = Request["iVendorSearchName"];
            var currIsActive = Request["iIsActive"] != null ? bool.Parse(Request["iIsActive"]) : false;
            var checkData = dbV.AX_Vendor_List.Where(w => w.AccountNum == accountNum && w.AccountNum != currAccountNum).FirstOrDefault();
            if (checkData != null)
            {
                TempData["errMSG"] = "Account Number '" + accountNum + "' Already Exist!";
                return RedirectToAction("List", "Vendor", new { area = "AX", addNew = "addNew" });
            }

            var newData = new AX_Vendor_List();
            newData.AccountNum = accountNum;
            newData.VendGroup = currVendorGroup;
            newData.Name = currVendorName;
            newData.SearchName = currVendorSearchName;
            newData.IsActive = currIsActive;
            newData.Created_By = currUserID;
            newData.Created_At = DateTime.Now;
            dbV.AX_Vendor_List.Add(newData);
            dbV.SaveChanges();

            return RedirectToAction("List", "Vendor", new { area = "AX"});
        }
        public ActionResult Edit()
        {
            var currAccountNum = Request["iCurrAccNumber"];
            var accountNum = Request["iAccountNum"];
            var currVendorGroup = Request["iVendorGroup"];
            var currVendorName = Request["iVendorName"];
            var currVendorSearchName = Request["iVendorSearchName"];
            var currIsActive = Request["iIsActive"] != null? bool.Parse(Request["iIsActive"]) : false;
            var checkData = dbV.AX_Vendor_List.Where(w => w.AccountNum == accountNum && w.AccountNum != currAccountNum).FirstOrDefault();
            if (checkData != null)
            {
                TempData["errMSG"] = "Account Number '" + accountNum + "' Already Exist!";
                return RedirectToAction("List", "Vendor", new { area = "AX", addNew = "addNew" });
            }
            var editData = dbV.AX_Vendor_List.Where(w => w.AccountNum == currAccountNum).FirstOrDefault();

            editData.AccountNum = accountNum;
            editData.VendGroup = currVendorGroup;
            editData.Name = currVendorName;
            editData.SearchName = currVendorSearchName;
            editData.IsActive = currIsActive;
            dbV.SaveChanges();

            return RedirectToAction("List", "Vendor", new { area = "AX" });
        }
    }
}