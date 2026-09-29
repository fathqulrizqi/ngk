using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using NGKBusi.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Identity;
using System;
using System.Diagnostics;

namespace NGKBusi.SignalR
{
    [Authorize]
    public class NotificationHub : Hub
    {
        DefaultConnection db = new DefaultConnection();
        // Menyimpan connectionId untuk setiap pengguna yang login
        private static readonly Dictionary<string, string> UserConnections = new Dictionary<string, string>();

        //
        // Summary:
        //     Gets the user security information for the current HTTP request.
        //
        // Returns:
        //     The user security information for the current HTTP request.

        // Menyimpan koneksi user
        public override Task OnConnected()
        {
            //var user = System.Web.HttpContext.Current.User; // Mendapatkan user dari HttpContext;

            //var userId = ((ClaimsIdentity)user.Identity).GetUserId();
            //if (!string.IsNullOrEmpty(userId))
            //{
            //    Groups.Add(Context.ConnectionId, userId);
            //}

            // PERBAIKAN: Gunakan 'Context.User', JANGAN 'HttpContext.Current'
            // Context.User aman digunakan di thread SignalR
            string userId = Context.User.Identity.GetUserId();
            // LOG INI AKAN MUNCUL DI OUTPUT WINDOW VISUAL STUDIO SAAT ANDA REFRESH PAGE
            System.Diagnostics.Debug.WriteLine($"---> [HUB] User Connected: {userId} (Group Created)");

            Groups.Add(Context.ConnectionId, userId);
            
            return base.OnConnected();
        }

        // Menghapus koneksi user saat disconnect
        public override Task OnDisconnected(bool stopCalled)
        {
            //var user = System.Web.HttpContext.Current.User; // Mendapatkan user dari HttpContext;
            //var userId = ((ClaimsIdentity)user.Identity).GetUserId();
            //if (!string.IsNullOrEmpty(userId))
            //{
            //    Groups.Remove(Context.ConnectionId, userId);
            //}
            //Console.WriteLine(userId);

            string userId = Context.User.Identity.GetUserId();
            string connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                // Hapus dari group (Meskipun SignalR otomatis membersihkan, ini good practice)
                Groups.Remove(connectionId, userId);
            }
            return base.OnDisconnected(stopCalled);
        }

        public void SendNotification(string userId, string message)
        {
            
            //if (UserConnections.ContainsKey(userId))
            //{
            //    Clients.Client(UserConnections[userId]).receiveNotification(message);
            //}

            Clients.Group(userId).receiveNotification(message);
        }

        // Tambahan: Fungsi broadcastNotif yang dipakai script JS sebelumnya
        public void BroadcastNotif(string senderName, string message, string url)
        {
            // Ini opsional, hanya jika client JS memanggil hub langsung
            Clients.All.broadcastNotif(senderName, message, url);
        }
    }
}