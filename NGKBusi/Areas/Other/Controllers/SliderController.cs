using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNet.Identity;

namespace NGKBusi.Areas.Other.Controllers
{
    public class SliderController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        // GET: Other/Slider
        public ActionResult Index()
        {
            ViewBag.MainSlider = db.Sliders.Where(w => w.Category == "Main").ToList();
            return View();
        }

        [HttpPost, ValidateInput(false)]
        [Authorize]
        public ActionResult Add(HttpPostedFileBase iSliderImage)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var expiredDateString = Request["iExpiredDate"].Split('-');
            var expiredDate = new DateTime(Int32.Parse(expiredDateString[2]), Int32.Parse(expiredDateString[1]), Int32.Parse(expiredDateString[0]));

            var cont = Request["iContent"];
            var path = "";
            var fileName = "";
            if (iSliderImage != null && iSliderImage.ContentLength > 0)
            {
                // extract only the filename
                fileName = Path.GetRandomFileName().Replace(".", "") + Path.GetExtension(iSliderImage.FileName);
                // store the file inside ~/App_Data/uploads folder
                path = Path.Combine(Server.MapPath("~/Images/Slider/Main"), fileName);
                iSliderImage.SaveAs(path);
            }

            db.Sliders.Add(new Sliders
            {
                Category = "Main",
                Path = "/NGKBusi/Images/Slider/Main/" + fileName,
                Content = cont,
                Is_Visible = true,
                Expired_Date = expiredDate,
                User_NIK = currUser.GetUserId(),
                Date_at = DateTime.Now
            });

            db.SaveChanges();
            return RedirectToAction("Index", "Slider", new { area = "Other" });
        }

        [HttpPost]
        public ActionResult Delete(int dataID, string path)
        {
            var iID = dataID;
            var fName = path.Split('/').ToArray();
            var fileName = fName[fName.Length - 1];

            string fullPath = Path.Combine(Server.MapPath("~/Images/Slider/Main"), fileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            var currSlider = db.Sliders.Where(x => x.ID == iID).FirstOrDefault();
            db.Sliders.Remove(currSlider);
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult SetVisible(int id)
        {
            var iID = id;

            var currSlider = db.Sliders.Where(x => x.ID == iID).FirstOrDefault();
            currSlider.Is_Visible = !currSlider.Is_Visible;
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost, ValidateInput(false)]
        [Authorize]
        public ActionResult Edit(HttpPostedFileBase iSliderImage)
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Int32.Parse(Request["iCurrID"]);
            var expiredDateString = Request["iExpiredDate"].Split('-');
            var expiredDate = new DateTime(Int32.Parse(expiredDateString[2]), Int32.Parse(expiredDateString[1]), Int32.Parse(expiredDateString[0]));

            var cont = Request["iContent"];
            var path = "";
            var fileName = "";

            Sliders editSlider = db.Sliders.Where(w => w.ID == currID).FirstOrDefault();

            if (iSliderImage != null && iSliderImage.ContentLength > 0)
            {

                string fullPath = Server.MapPath(editSlider.Path);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
                // extract only the filename
                fileName = Path.GetRandomFileName().Replace(".", "") + Path.GetExtension(iSliderImage.FileName);
                // store the file inside ~/App_Data/uploads folder
                path = Path.Combine(Server.MapPath("~/Images/Slider/Main"), fileName);
                iSliderImage.SaveAs(path);
                editSlider.Path = "/NGKBusi/Images/Slider/Main/" + fileName;
            }

            editSlider.Content = cont;
            editSlider.Expired_Date = expiredDate;
            editSlider.User_NIK = currUser.GetUserId();
            editSlider.Date_at = DateTime.Now;
            db.SaveChanges();

            return RedirectToAction("Index", "Slider", new { area = "Other" });
        }

        public ActionResult Content(int id)
        {
            var iID = id;

            ViewBag.Content = db.Sliders.Where(x => x.ID == iID).FirstOrDefault();

            return View();
        }
    }
}