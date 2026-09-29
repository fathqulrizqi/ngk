using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Security.Claims;
using Microsoft.AspNet.Identity;

namespace NGKBusi.Controllers
{
    public class HomeController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        public ActionResult Index()
        {
            ViewBag.Title = "Niterra Portal";
            ViewBag.MainSlider = db.Sliders.Where(w => w.Category == "Main" && w.Expired_Date >= DateTime.Now).ToList();

            if (User.Identity.IsAuthenticated)
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
                var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
                var FavoriteMenu = db.V_User_Menu_Favorites.Where(w => w.UserName == CurrUser.NIK).ToList();
                var TaskList = db.Task_Approval_Lists.Include(w => w.Menu).Where(w => w.NIK == currUser && !w.IsCompleteTask).OrderBy(w => w.ApprovalDeadline).ToList();
                ViewBag.FavoriteMenu = FavoriteMenu;
                ViewBag.TaskList = TaskList;
            }
            else
            {
                
                ViewBag.FavoriteMenu = "";
                ViewBag.TaskList = null;
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        
        public ActionResult mainMenu()
        {
            DefaultConnection db = new DefaultConnection();
            ViewBag.Title = "Niterra Portal";

            RecursiveMenu currModel = new RecursiveMenu { parentID = null, itemCount = 0, Menus = db.Menus.Include(c => c.Users_Menus_Roles).Where(x => x.id != 1 && x.isActive == true).OrderBy(x => x.sequence).ToList() };

            if (User.Identity.IsAuthenticated)
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
                var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
                var FavoriteMenu = db.V_User_Menu_Favorites.Where(w => w.UserName == CurrUser.NIK).ToList();
                ViewData["FavoriteMenu"] = FavoriteMenu;
            }
            else
            {
                ViewData["FavoriteMenu"] = "";
            }

            return PartialView("_MenuPartialNew", currModel);
        }



        public ActionResult AddFavoriteMenu(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            int data = db.Users_Menus_Favorites.Where(w => w.IdMenu == id && w.UserName == CurrUser.NIK).Count();
            if (data > 0)
            {
                return Json(new { status = 0 });
            }
            else
            {
                Users_Menus_Favorites Fav = new Users_Menus_Favorites();

                Fav.IdMenu = id;
                Fav.UserName = CurrUser.NIK;
                Fav.CreateTime = DateTime.Now;

                db.Users_Menus_Favorites.Add(Fav);

                var save = db.SaveChanges();
                if (save > 0)
                {
                    return Json(new { status = 1 });
                }
                else
                {
                    return Json(new { status = 0 });
                }
                
            }
        }

        public ActionResult RemoveFavoriteMenu(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var data = db.Users_Menus_Favorites.Where(w => w.IdMenu == id && w.UserName == CurrUser.NIK).ToList();
            db.Users_Menus_Favorites.RemoveRange(data);

            var save = db.SaveChanges();
            if (save > 0)
            {
                return Json(new { status = 1 });
            }
            else
            {
                return Json(new { status = 0 });
            }


        }
        [HttpPost]
        public ActionResult SetSessionData(string Navbar, string value)
        {
            Session["Navbar"] = value;
            return Json(new { success = true, value = value });
        }

    }
}