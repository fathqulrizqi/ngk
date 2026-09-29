using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HC.Models;
using NGKBusi.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Controllers
{
    public class NotificationController : Controller
    {
        LegalPermitConnection dblp = new LegalPermitConnection();
        // GET: Notification
        public ActionResult PushNotification(string reqNo, List<string> user)
        {
            // Mengambil context hub untuk mengirimkan pesan
            var _hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            //// Mengirim notifikasi ke semua klien yang terhubung
            //hubContext.Clients.All.receiveNotification("Pesanan baru telah diterima!");

            //return Content("Notifikasi telah dikirim.");
            //int orderId = 234324;
            //var users = dbsp.SCM_Sparepart_User_Management.Where(w => w.Role == "Admin" || w.Role == "Developer").ToList();
            foreach (var usr in user)
            {
                var userId = usr;
                var hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                hubContext.Clients.Group(userId).receiveNotification($"{reqNo}");
            }



            return Content("Notifikasi dikirim ke user");
        }

        // 1. Ambil List Notifikasi (Untuk Dropdown)
        public ActionResult GetNotifications()
        {
            var currUser = User.Identity.GetUserId();
            var list = dblp.HC_LegalPermit_Notification
                           .Where(x => x.RecipientNIK == currUser)
                           .OrderByDescending(x => x.CreatedDate)
                           .Take(10) // Ambil 10 terakhir
                           .ToList();

            // Hitung jumlah yang belum dibaca
            var unreadCount = dblp.HC_LegalPermit_Notification
                                  .Count(x => x.RecipientNIK == currUser && x.IsRead == 0);

            return Json(new { data = list, count = unreadCount }, JsonRequestBehavior.AllowGet);
        }

        // 2. Tandai Sudah Dibaca (Saat diklik)
        [HttpPost]
        public ActionResult MarkAsRead(int id)
        {
            var notif = dblp.HC_LegalPermit_Notification.Find(id);
            if (notif != null)
            {
                notif.IsRead = 1;
                dblp.SaveChanges();
            }
            return Json(new { success = true });
        }
    }
}