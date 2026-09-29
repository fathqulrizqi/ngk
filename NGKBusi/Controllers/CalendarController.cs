using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using System.Security.Claims;

namespace NGKBusi.Controllers
{
    public class CalendarController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        // GET: Calendar
        public ActionResult Index()
        {
            var currUserId = User.Identity.GetUserId();
            ViewBag.allowedUpdate = false;
            if (Request.IsAuthenticated) { 
                ViewBag.allowedUpdate = db.Users_Menus_Roles.Where(x => x.userNIK == currUserId && x.menuID == 7).Select(x => x.allowUpdate).SingleOrDefault();
            }
            return View();
        }

        public ActionResult Data()
        {
            Response.ContentType = "text/xml";
            ViewBag.events = db.Events.ToList();
            return View();
        }

        public ActionResult Save(FormCollection actionValues)
        {
            String action_type = actionValues["!nativeeditor_status"];
            Int64 source_id = Int64.Parse(actionValues["id"]);
            Int64 target_id = source_id;
            
            try
            {
                switch (action_type)
                {
                    case "inserted":
                        db.Events.Add(new Events
                        {
                            start_date = DateTime.Parse(actionValues["start_date"]),
                            end_date = DateTime.Parse(actionValues["end_date"]),
                            text = actionValues["text"],
                            type = actionValues["type"]
                        });
                        db.SaveChanges();
                        source_id = db.Events.Max(item => item.id);
                        break;
                    case "deleted":
                        var DeletedData = db.Events.FirstOrDefault(ev => ev.id == source_id);
                        db.Events.Remove(DeletedData);
                        db.SaveChanges();
                        break;
                    default: // "updated"
                        var updatedData = db.Events.FirstOrDefault(ev => ev.id == source_id);
                        updatedData.start_date = DateTime.Parse(actionValues["start_date"]);
                        updatedData.end_date = DateTime.Parse(actionValues["end_date"]);
                        updatedData.text = actionValues["text"];
                        updatedData.type = actionValues["type"];
                        db.SaveChanges();
                        break;
                }
                target_id = source_id;
            }
            catch
            {
                action_type = "error";
            }

            ViewBag.action_type = action_type;
            ViewBag.source_id = source_id;
            ViewBag.target_id = target_id;
            Response.ContentType = "text/xml";
            return View();
        }
    }
}