using NGKBusi.Models;
using NGKBusi.Areas.Other.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.IO;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using System.Globalization;
using NGKBusi.Areas.IT.Controllers;


namespace NGKBusi.Areas.Other.Controllers
{
    public class NWRController : Controller
    {

        DefaultConnection db = new DefaultConnection();
        NWRConnection dbm = new NWRConnection();

        public ActionResult Index()
        {
            ViewBag.NavHide = true;
            var currUserId = User.Identity.GetUserId();
            var currUserName = User.Identity.GetUserName();

            var _currUser = (ClaimsIdentity)User.Identity;
            string secName = _currUser.FindFirstValue("secName");

            var hcUsersList = db.V_Users_Active
                                .Where(u => u.SectionName == "GA")
                                .Select(u => u.NIK)
                                .ToList();

            ViewBag.HCUsersList = hcUsersList; 

            var user = db.V_Users_Active.FirstOrDefault(u => u.NIK == currUserId);

            var nowReal = DateTime.Now;
            var now = nowReal.Date;
            string currentTime = nowReal.ToString("yyyy-MM-ddTHH:mm:ss");
            string timeX = nowReal.ToString("HH:mm");
            string dateX = nowReal.ToString("yyyy-MM-dd");

            ViewBag.now = now;
            ViewBag.timeX = timeX;
            ViewBag.dateX = dateX;
            ViewBag.allowedUpdate = false;
            ViewBag.currUsr = currUserId;
            ViewBag.currUsrNIK = user?.NIK ?? "";
            ViewBag.currUsrName = user?.Name ?? "unknown";

            ViewBag.IsAllowedConfig = secName == "GA" || hcUsersList.Contains(currUserId);

            return View();
        }

        public ActionResult NWRConfiguration()
        {
            ViewBag.NavHide = true;
            var currUserId = User.Identity.GetUserId();
            var currUserName = User.Identity.GetUserName();

            var _currUser = (ClaimsIdentity)User.Identity;
            string secName = _currUser.FindFirstValue("secName");

            var hcUsersList = db.V_Users_Active
                                .Where(u => u.SectionName == "GA")
                                .Select(u => u.NIK)
                                .ToList();

            ViewBag.HCUsersList = hcUsersList;

            var user = db.V_Users_Active.FirstOrDefault(u => u.NIK == currUserId);

            var nowReal = DateTime.Now;
            var now = nowReal.Date;
            string currentTime = nowReal.ToString("yyyy-MM-ddTHH:mm:ss");
            string timeX = nowReal.ToString("HH:mm");
            string dateX = nowReal.ToString("yyyy-MM-dd");

            ViewBag.now = now;
            ViewBag.timeX = timeX;
            ViewBag.dateX = dateX;
            ViewBag.allowedUpdate = false;
            ViewBag.currUsr = currUserId;
            ViewBag.currUsrNIK = user?.NIK ?? "";
            ViewBag.currUsrName = user?.Name ?? "unknown";

            ViewBag.IsAllowedConfig = secName == "GA" || hcUsersList.Contains(currUserId);

            return View();
        }

        [HttpGet]
        public JsonResult GetRoomCat()
        {
            var categories = dbm.OTH_NWR_Master_RoomCat
                .Where(x => x.isActive == true)
                .OrderBy(x => x.sequence2) 
                .Select(x => new
                {
                    x.ID,
                    x.RoomCat,
                    x.isMaintenance,
                    x.sequence,
                    x.sequence2,
                    x.isActive
                }).ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllRoomCat()
        {
            var categories = dbm.OTH_NWR_Master_RoomCat
                .OrderBy(x => x.sequence2)
                .Select(x => new
                {
                    x.ID,
                    x.RoomCat,
                    x.isMaintenance,
                    x.sequence,
                    x.sequence2,
                    x.isActive
                }).ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveMasterRoomCat(int ID, string RoomCat, bool isActive, bool isMaintenance)
        {
            try
            {
                if (ID == 0)
                {
                    // PROSES INSERT: Cari nilai max sequence1 & sequence2, lalu +1 otomatis.
                    int nextSeq1 = 1;
                    int nextSeq2 = 1;
                    if (dbm.OTH_NWR_Master_RoomCat.Any())
                    {
                        nextSeq1 = dbm.OTH_NWR_Master_RoomCat.Max(c => c.sequence) + 1;
                        nextSeq2 = dbm.OTH_NWR_Master_RoomCat.Max(c => c.sequence2) + 1;
                    }

                    var newCat = new OTH_NWR_Master_RoomCat
                    {
                        RoomCat = RoomCat,
                        sequence = nextSeq1,
                        sequence2 = nextSeq2, // Di set urutan terakhir secara otomatis
                        isActive = isActive,
                        isMaintenance = isMaintenance
                    };
                    dbm.OTH_NWR_Master_RoomCat.Add(newCat);
                }
                else
                {
                    // PROSES UPDATE
                    var existingCat = dbm.OTH_NWR_Master_RoomCat.FirstOrDefault(c => c.ID == ID);
                    if (existingCat != null)
                    {
                        existingCat.RoomCat = RoomCat;
                        existingCat.isActive = isActive;
                        existingCat.isMaintenance = isMaintenance;
                    }
                    else
                    {
                        return Json(new { errorCode = 102, message = "Data Kategori tidak ditemukan." });
                    }
                }

                dbm.SaveChanges();
                return Json(new { errorCode = 101, message = "Kategori berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { errorCode = 500, message = "Terjadi kesalahan server: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ReorderRoomCat(List<int> orderedIds)
        {
            try
            {
                if (orderedIds != null && orderedIds.Count > 0)
                {
                    int currentSeq2 = 1;
                    foreach (var id in orderedIds)
                    {
                        // Mencari kategori berdasarkan ID yang dikirim oleh Javascript Sortable
                        var cat = dbm.OTH_NWR_Master_RoomCat.FirstOrDefault(c => c.ID == id);
                        if (cat != null)
                        {
                            cat.sequence2 = currentSeq2;
                            currentSeq2++;
                        }
                    }
                    dbm.SaveChanges();
                }
                return Json(new { errorCode = 101, message = "Urutan Icon berhasil diperbarui." });
            }
            catch (Exception ex)
            {
                return Json(new { errorCode = 500, message = ex.Message });
            }
        }

        [HttpPost]
        // Parameter diubah menjadi 'sequence' sesuai permintaan Anda (Delete by Sequence)
        public JsonResult DeleteRoomCat(int sequence)
        {
            try
            {
                // VALIDASI: Cek relasi menggunakan 'sequence' (IDRoomCat)
                bool isUsedByRoom = dbm.OTH_NWR_Master_Rooms.Any(r => r.IDRoomCat == sequence);

                if (isUsedByRoom)
                {
                    return Json(new { errorCode = 102, message = "Gagal Dihapus! Kategori ini sedang digunakan oleh satu atau beberapa ruangan." });
                }

                // Cari kategori berdasarkan sequence
                var catToFind = dbm.OTH_NWR_Master_RoomCat.FirstOrDefault(c => c.sequence == sequence);

                if (catToFind != null)
                {
                    dbm.OTH_NWR_Master_RoomCat.Remove(catToFind);
                    dbm.SaveChanges();
                    return Json(new { errorCode = 101, message = "Kategori berhasil dihapus." });
                }

                return Json(new { errorCode = 102, message = "Kategori tidak ditemukan." });
            }
            catch (Exception ex)
            {
                return Json(new { errorCode = 500, message = "Terjadi kesalahan server: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetRoomDetails(int sequenceId)
        {
            // Menggunakan sequenceId untuk relasi yang benar sesuai database
            var rooms = (from room in dbm.OTH_NWR_Master_Rooms
                         join cat in dbm.OTH_NWR_Master_RoomCat on room.IDRoomCat equals cat.sequence
                         where room.IDRoomCat == sequenceId && room.IsActive == true
                         select new
                         {
                             room.ID,
                             room.RoomTitle,
                             room.Image,
                             room.IDRoomCat,
                             room.ExtensionNumber,
                             CategoryName = cat.RoomCat,
                             cat.isMaintenance,
                             cat.sequence,
                             prop = dbm.OTH_NWR_Rooms_Properties
                                 .Where(p => p.RoomID == room.ID)
                                 .Select(prop => new
                                 {
                                     prop.ID,
                                     prop.RoomID,
                                     prop.PropsName,
                                     prop.Quantity
                                 }).ToList()
                         }).ToList();

            return Json(rooms, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetSchedule(int roomId)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            List<Tbl_Events> scheduleList = new List<Tbl_Events>();

            var bookings = dbm.OTH_NWR_Bookings.Where(b => b.Status == 1 && b.RoomID == roomId).ToList();

            foreach (var booking in bookings)
            {
                var color = booking.UserNIK == currUser ? "#DA6F22" : "#007582";

                scheduleList.Add(new Tbl_Events
                {
                    id = Convert.ToInt32(booking.ID),
                    title = booking.Subject,
                    start = booking.Day.ToString("yyyy-MM-dd") + "T" + booking.StartTime,
                    end = booking.Day.ToString("yyyy-MM-dd") + "T" + booking.EndTime,
                    color = color,
                    roomid = booking.RoomID,
                    attendance = Convert.ToInt32(booking.Attendance),
                    link = booking.Link,
                    linkmeet = booking.LinkMeet
                });
            }

            return Json(scheduleList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetScheduleByEventId(int? idBook = null)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var currUserName = ((ClaimsIdentity)User.Identity).GetUserName();

            List<Tbl_Events> scheduleList = new List<Tbl_Events>();

            var bookings = dbm.OTH_NWR_Bookings.Where(b => b.Status == 1 && b.ID == idBook).ToList();


            foreach (var booking in bookings)
            {
                var color = booking.UserNIK == currUser ? "#DA6F22" : "#007582";
                var room = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(m => m.ID == booking.RoomID);
                var roomTitle = room != null ? room.RoomTitle : "Unknown Room";
                var userData = db.V_Users_Active.FirstOrDefault(w => w.NIK == booking.UserNIK);
                var name = userData != null ? userData.Name : "Unknown Name";

                scheduleList.Add(new Tbl_Events
                {
                    id = Convert.ToInt32(booking.ID),
                    subject = booking.Subject,
                    start = booking.Day.ToString("yyyy-MM-dd") + "T" + booking.StartTime,
                    end = booking.Day.ToString("yyyy-MM-dd") + "T" + booking.EndTime,
                    linkmeet = booking.LinkMeet,
                    roomid = booking.RoomID,
                    user = booking.UserNIK,
                    attendance = Convert.ToInt32(booking.Attendance),
                    username = name,
                    roomtitle = roomTitle,
                    timestamps = booking.Timestamps?.ToString("yyyy-MM-dd HH:mm:ss"),
                    color = color,
                    link = booking.Link
                });
            }


            return Json(scheduleList, JsonRequestBehavior.AllowGet);
        }

        private bool InRange(DateTime newStart, DateTime newEnd, DateTime existingStart, DateTime existingEnd)
        {
            return newStart < existingEnd && newEnd > existingStart;
        }

        [HttpPost]
        public JsonResult AddSchedule(/*int? idBook = null*/)
        {

            var now = DateTime.Now;
            var currentTime = DateTime.Now.AddDays(-1);
            string timeX = now.ToString("HH:mm");
            string dateX = now.ToString("yyyy-MM-dd");


            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var usernik = Request["UserNIK"];
            var title = Request["RoomTitle"];
            var day = DateTime.Parse(Request["Day"]);
            var startTime = Request["StartTime"];
            var endTime = Request["EndTime"];
            var roomId = int.Parse(Request["RoomId"]);
            var subject = Request["Subject"];
            var linkMeet = Request["LinkMeet"];
            var link = Request["Link"];
            var attendance = int.Parse(Request["Attendance"]);

            linkMeet = string.IsNullOrWhiteSpace(linkMeet) ? null : linkMeet;
            link = string.IsNullOrWhiteSpace(link) ? null : link;


            int status = 0;
            string msg = "";
            int errorCode = 0;


            var timeFormat = new System.Text.RegularExpressions.Regex(@"^(0[0-9]|1[0-9]|2[0-3]|[0-9]):[0-5][0-9]$");

            if (!timeFormat.IsMatch(startTime))
            {
                msg = "Start time must be in HH:mm format (e.g., 12:00).";
                return Json(new { status = status, message = msg, errorCode = errorCode }, JsonRequestBehavior.AllowGet);
            }

            if (!timeFormat.IsMatch(endTime))
            {
                msg = "End time must be in HH:mm format (e.g., 12:00).";
                return Json(new { status = status, message = msg, errorCode = errorCode }, JsonRequestBehavior.AllowGet);
            }


            DateTime newStartTime = DateTime.Parse($"{day:yyyy-MM-dd} {startTime}");
            DateTime newEndTime = DateTime.Parse($"{day:yyyy-MM-dd} {endTime}");


            if (!DateTime.TryParse(day.ToString("yyyy-MM-dd") + " " + startTime, out newStartTime) ||
                !DateTime.TryParse(day.ToString("yyyy-MM-dd") + " " + endTime, out newEndTime))
            {
                msg = "Invalid date or time format.";
                return Json(new { status = status, message = msg, errorCode = errorCode }, JsonRequestBehavior.AllowGet);
            }

            if (newEndTime < newStartTime)
            {
                msg = "End time cannot be less than start time.";
                return Json(new { status = 0, message = msg, errorCode = 999 }, JsonRequestBehavior.AllowGet);
            }

            if (currentTime > day)
            {
                msg = "Date time cannot be less than today";
                return Json(new { status = status, message = msg, errorCode = errorCode }, JsonRequestBehavior.AllowGet);
            }

            DateTime startOfDay = day.Date.AddHours(7);
            DateTime endOfDay = day.Date.AddHours(21);

            if (newStartTime < startOfDay || newEndTime > endOfDay)
            {
                msg = "The booking time must be between 07:00 and 21:00.";
                return Json(new { status = 0, message = msg, errorCode = errorCode }, JsonRequestBehavior.AllowGet);
            }


            var existingBookings = dbm.OTH_NWR_Bookings
                .Where(b => b.RoomID == roomId &&
                            b.Day == day &&
                            //b.ID != idBook &&
                            b.Status == 1)
                .ToList();

            var conflictingEvents = new List<OTH_NWR_Bookings>();

            foreach (var booking in existingBookings)
            {
                DateTime existingStartTime = DateTime.Parse($"{booking.Day:yyyy-MM-dd} {booking.StartTime}");
                DateTime existingEndTime = DateTime.Parse($"{booking.Day:yyyy-MM-dd} {booking.EndTime}");

                bool isOverlap = newStartTime < existingEndTime && newEndTime > existingStartTime;
                if (isOverlap)
                {
                    conflictingEvents.Add(booking);
                }
            }

            if (conflictingEvents.Count > 0)
            {
                // Menyusun pesan konflik
                var conflictMessages = new List<string>();
                foreach (var conflictingEvent in conflictingEvents)
                {
                    var room = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(m => m.ID == conflictingEvent.RoomID);
                    var roomTitle = room != null ? room.RoomTitle : "Unknown Room";
                    var userData = db.V_Users_Active.FirstOrDefault(w => w.NIK == conflictingEvent.UserNIK);
                    var name = userData != null ? userData.Name : "Unknown Name";

                    conflictMessages.Add($"'{conflictingEvent.Subject}' in '{roomTitle}' ({conflictingEvent.StartTime} to {conflictingEvent.EndTime})");
                }

                msg = conflictingEvents.Count == 1
                    ? $"Schedule conflict detected with {conflictMessages.First()}. Please choose a different time."
                    : $"Multiple schedule conflicts detected:<br>{string.Join("<br>", conflictMessages)}";

                status = 0;
                errorCode = conflictingEvents.Count == 1 ? 101 : 102;

                return Json(new
                {
                    status = status,
                    message = msg,
                    errorCode = errorCode,
                    conflict = conflictingEvents.Select(ce => new
                    {
                        ce.ID,
                        ce.UserNIK,
                        RoomID = ce.RoomID,
                        Subject = ce.Subject,
                        LinkMeet = ce.LinkMeet,
                        Attendance = ce.Attendance,
                        Day = ce.Day.ToString("yyyy-MM-dd"),
                        StartTime = ce.StartTime,
                        EndTime = ce.EndTime
                    }).ToList()
                }, JsonRequestBehavior.AllowGet);
            }

            var roomR = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(m => m.RoomTitle == title);
            var roomId2 = roomR != null ? roomR.ID : 0;

            int finalRoomId = (roomId > 0) ? roomId : roomId2;

            var user = db.V_Users_Active.FirstOrDefault(u => u.Name == usernik);
            var userNIK = user != null ? user.NIK : null;

            OTH_NWR_Bookings schedule = new OTH_NWR_Bookings
            {
                UserNIK = CurrUser?.NIK ?? usernik,
                RoomID = finalRoomId,
                Subject = subject,
                LinkMeet = linkMeet,
                Attendance = attendance,
                Link = link,
                Day = day,
                StartTime = startTime,
                EndTime = endTime,
                Status = 1,
                Timestamps = DateTime.Now
            };

            dbm.OTH_NWR_Bookings.Add(schedule);
            int ins = dbm.SaveChanges();

            if (ins > 0)
            {
                msg = "Workspace Reservation Added Successfully";
                status = 1;
            }
            else
            {
                msg = "Failed Save Schedule";
                status = 0;
            }

            return Json(new { status = status, message = msg }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateSchedule(int idBook)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            var currentTime = DateTime.Now.AddDays(-1);


            var usernik = Request["UserNIK"];
            var day = DateTime.Parse(Request["DayUpdate"]);
            var startTime = Request["StartTimeUpdate"];
            var endTime = Request["EndTimeUpdate"];
            var roomId = int.Parse(Request["RoomIdUpdate"]);
            var subject = Request["SubjectUpdate"];
            var linkmeet = Request["LinkMeetUpdate"];
            var link = Request["LinkUpdate"];
            var attendance = int.Parse(Request["AttendanceUpdate"]);

            linkmeet = string.IsNullOrWhiteSpace(linkmeet) ? null : linkmeet;
            link = string.IsNullOrWhiteSpace(link) ? null : link;

            int status = 0;
            string msg = "";
            int errorCode = 0;

            DateTime newStartTime;
            DateTime newEndTime;

            if (!DateTime.TryParse(day.ToString("yyyy-MM-dd") + " " + startTime, out newStartTime) ||
                !DateTime.TryParse(day.ToString("yyyy-MM-dd") + " " + endTime, out newEndTime))
            {
                msg = "Invalid date or time format.";
                return Json(new { status = status, message = msg, errorCode = 991 }, JsonRequestBehavior.AllowGet);
            }

            DateTime startOfDay = day.Date.AddHours(7); ;
            DateTime endOfDay = day.Date.AddHours(21);

            if (newStartTime < startOfDay || newEndTime > endOfDay)
            {
                msg = "The booking time must be between 07:00 and 21:00.";
                return Json(new { status = 0, message = msg, errorCode = 992 }, JsonRequestBehavior.AllowGet);
            }

            if (currentTime > day)
            {
                msg = "Data cannot be updated because it is outdated";
                return Json(new { status = status, message = msg, errorCode = errorCode }, JsonRequestBehavior.AllowGet);
            }

            if (newEndTime < newStartTime)
            {
                msg = "End time cannot be less than start time.";
                return Json(new { status = 0, message = msg, errorCode = 993 }, JsonRequestBehavior.AllowGet);
            }


            var schedule = dbm.OTH_NWR_Bookings.First(b => b.ID == idBook);
            var user = db.V_Users_Active.FirstOrDefault(u => u.Name == usernik);
            var userNIK = user != null ? user.NIK : null;

            schedule.UserNIK = CurrUser?.NIK ?? userNIK;
            schedule.RoomID = roomId;
            schedule.Subject = subject;
            schedule.LinkMeet = linkmeet;
            schedule.Attendance = attendance;
            schedule.Day = day;
            schedule.Link = link;
            schedule.StartTime = startTime;
            schedule.EndTime = endTime;
            schedule.Status = 1;
            schedule.Timestamps = DateTime.Now;

            var existingBookings = dbm.OTH_NWR_Bookings
                .Where(b => b.RoomID == roomId &&
                            b.Day == day &&
                            b.ID != idBook &&
                            b.Status == 1)
                .ToList();

            var conflictingEvents = new List<OTH_NWR_Bookings>();

            foreach (var booking in existingBookings)
            {
                if (!DateTime.TryParse($"{booking.Day:yyyy-MM-dd} {booking.StartTime}", out var existingStartTime) ||
                    !DateTime.TryParse($"{booking.Day:yyyy-MM-dd} {booking.EndTime}", out var existingEndTime))
                    continue;

                if (InRange(newStartTime, newEndTime, existingStartTime, existingEndTime))
                {
                    conflictingEvents.Add(booking);
                }
            }

            if (conflictingEvents.Count > 0)
            {
                status = 0;

                if (conflictingEvents.Count == 1)
                {
                    var conflictingEvent = conflictingEvents.First();
                    var room = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(m => m.ID == conflictingEvent.RoomID);
                    var roomTitle = room != null ? room.RoomTitle : "Unknown Room";
                    var userData = db.V_Users_Active.FirstOrDefault(w => w.NIK == conflictingEvent.UserNIK);
                    var name = userData != null ? userData.Name : "Unknown Name";

                    msg = $"Conflict detected with the following existing reservation:<br><br>" +
                          $"<strong>'{conflictingEvent.Subject}'</strong> in <strong>{roomTitle}</strong> ({conflictingEvent.StartTime} to {conflictingEvent.EndTime})<br>";

                    errorCode = 101; // Kode untuk satu konflik

                    return Json(new
                    {
                        status = status,
                        message = msg,
                        errorCode = errorCode,
                        conflict = new
                        {
                            conflictingEvent.ID,
                            conflictingEvent.UserNIK,
                            RoomID = conflictingEvent.RoomID,
                            Subject = conflictingEvent.Subject,
                            StartTime = conflictingEvent.StartTime,
                            EndTime = conflictingEvent.EndTime,
                            Day = conflictingEvent.Day.ToString("yyyy-MM-dd")
                        }
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    msg = "Multiple conflicts detected with the following existing reservations:<br><br>";
                    errorCode = 102;

                    foreach (var conflictingEvent in conflictingEvents)
                    {
                        var room = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(m => m.ID == conflictingEvent.RoomID);
                        var roomTitle = room != null ? room.RoomTitle : "Unknown Room";
                        var userData = db.V_Users_Active.FirstOrDefault(w => w.NIK == conflictingEvent.UserNIK);
                        var name = userData != null ? userData.Name : "Unknown Name";

                        msg += $"<strong>'{conflictingEvent.Subject}'</strong> in <strong>{roomTitle}</strong> ({conflictingEvent.StartTime} to {conflictingEvent.EndTime})<br>";
                    }

                    return Json(new
                    {
                        status = status,
                        message = msg,
                        errorCode = errorCode,
                        conflict = conflictingEvents.Select(ce => new
                        {
                            ce.ID,
                            ce.UserNIK,
                            RoomID = ce.RoomID,
                            Subject = ce.Subject,
                            StartTime = ce.StartTime,
                            EndTime = ce.EndTime,
                            Day = ce.Day.ToString("yyyy-MM-dd")
                        }).ToList()
                    }, JsonRequestBehavior.AllowGet);
                }
            }

            int updateSchedule = dbm.SaveChanges();

            if (updateSchedule > 0)
            {
                msg = "Workspace Reservation Updated Successfully";
                status = 1;
            }
            else
            {
                msg = "Failed Update Workspace";
                status = 0;
            }

            return Json(new { status = status, message = msg, roomId = roomId, errorCode = errorCode }, JsonRequestBehavior.AllowGet);
        }


        private string ExtractTime(string dateTimeString)
        {
            DateTime dateTime;
            if (DateTime.TryParse(dateTimeString, out dateTime))
            {
                return dateTime.ToString("HH:mm");
            }
            else
            {
                throw new Exception("Invalid Time Format.");
            }
        }

        [HttpPost]
        public JsonResult SubmitRequest(OTH_NWR_Requests requestModel)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();

            var idConflict = Int32.Parse(Request["idConflict"]);
            var NIKConflict = Request["NIKConflict"];
            var dateConflict = Request["dateConflict"];
            var startConflict = Request["startConflict"];
            var endConflict = Request["endConflict"];
            string startTime = ExtractTime(startConflict);
            string endTime = ExtractTime(endConflict);
            var roomConflict = Request["roomConflict"];
            var subjectConflict = Request["subjectConflict"];
            var attendanceConflict = Int32.Parse(Request["attendanceConflict"]);

            var NIKRequest = Request["NIKRequest"];
            var roomRequest = Request["roomRequest"];
            var dateRequest = Request["dateRequest"];
            var startRequest = Request["startRequest"];
            var endRequest = Request["endRequest"];
            var messageRequest = Request["messageRequest"];
            var attendanceRequest = Int32.Parse(Request["attendanceRequest"]);

            var existingRequests = dbm.OTH_NWR_Requests
         .Where(r => r.NIKRequest == NIKRequest && r.idConflict == idConflict && r.statusR == 2)
         .ToList();


            if (existingRequests.Any())
            {
                return Json(new { status = 0, message = "You have a pending request in this event, cannot submit another until it is resolved." }, JsonRequestBehavior.AllowGet);
            }

            var parseStart = DateTime.Parse(startRequest);
            var parseEnd = DateTime.Parse(endRequest);

            if (parseEnd < parseStart)
            {
                return Json(new { status = 0, message = "end time cannot be less than start time." }, JsonRequestBehavior.AllowGet);
            }

            var timeFormat = new System.Text.RegularExpressions.Regex(@"^(0[0-9]|1[0-9]|2[0-3]|[0-9]):[0-5][0-9]$");

            if (!timeFormat.IsMatch(startRequest))
            {
                return Json(new { status = 0, message = "Start time must be in HH:mm format (e.g., 12:00)." }, JsonRequestBehavior.AllowGet);
            }

            if (!timeFormat.IsMatch(endRequest))
            {
                return Json(new { status = 0, message = "End time must be in HH:mm format (e.g., 12:00)." }, JsonRequestBehavior.AllowGet);
            }

            int status = 0;
            string msg = "";

            OTH_NWR_Requests request = new OTH_NWR_Requests();
            request.idConflict = idConflict;
            request.NIKConflict = NIKConflict;
            request.dateConflict = dateConflict;
            request.startConflict = startTime;
            request.endConflict = endTime;
            request.roomConflict = roomConflict;
            request.attendanceConflict = attendanceConflict;
            request.subjectConflict = subjectConflict;
            request.NIKRequest = NIKRequest;
            request.roomRequest = roomRequest;
            request.dateRequest = dateRequest;
            request.startRequest = startRequest;
            request.endRequest = endRequest;
            request.messageRequest = messageRequest;
            request.attendanceRequest = attendanceRequest;
            request.timestamps = DateTime.Now;
            request.statusR = 2;
            dbm.OTH_NWR_Requests.Add(request);
            int ins = dbm.SaveChanges();

            if (ins > 0)
            {
                msg = "Your workspace request has been submitted.";
                status = 1;
                int idRequest = request.id;

                var emailConflict = db.Users.FirstOrDefault(w => w.NIK == NIKConflict);
                var emailRequest = db.Users.FirstOrDefault(w => w.NIK == NIKRequest);

                if (string.IsNullOrEmpty(emailConflict.Email))
                {
                    return Json(new { status = 0, message = "Your email is missing. Cannot send Request." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(emailRequest.Email))
                {
                    return Json(new { status = 0, message = "Email of recipient user is missing. Cannot send Request." }, JsonRequestBehavior.AllowGet);
                }

                try
                {
                    SendEmail(idRequest);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                }

                return Json(new { status = status, message = msg }, JsonRequestBehavior.AllowGet);
            }

            else
            {
                msg = "Failed to Make Request";
                status = 0;
            }

            return Json(new { status = status, message = msg }, JsonRequestBehavior.AllowGet);
        }

        public void SendEmail(int idRequest)
        {
            string FilePath = Path.Combine(Server.MapPath("~/Emails/Other/NWR/"), "notif.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();

            var requests = dbm.OTH_NWR_Requests.FirstOrDefault(w => w.id == idRequest);

            var emailConflict = db.Users.FirstOrDefault(w => w.NIK == requests.NIKConflict);
            var emailRequest = db.V_Users_Active.FirstOrDefault(w => w.NIK == requests.NIKRequest);


            MailText = MailText.Replace("##RecipientName##", emailConflict?.Name ?? "Unknown User");
            MailText = MailText.Replace("##ExistingUser##", emailConflict?.Email ?? "Unknown User");
            MailText = MailText.Replace("##ExistingDate##", requests.dateConflict);
            MailText = MailText.Replace("##ExistingTimeStart##", requests.startConflict);
            MailText = MailText.Replace("##ExistingTimeEnd##", requests.endConflict);
            MailText = MailText.Replace("##ExistingRoom##", requests.roomConflict);
            MailText = MailText.Replace("##ExistingSubject##", requests.subjectConflict);
            MailText = MailText.Replace("##ExistingAttendance##", requests.attendanceConflict.ToString());

            MailText = MailText.Replace("##UserRequest##", emailRequest?.Email ?? "Unknown Requester");
            MailText = MailText.Replace("##RequestDate##", requests.dateRequest);
            MailText = MailText.Replace("##RequestTimeStart##", requests.startRequest);
            MailText = MailText.Replace("##RequestTimeEnd##", requests.endRequest);
            MailText = MailText.Replace("##RequestRoom##", requests.roomRequest);
            MailText = MailText.Replace("##RequestMessage##", requests.messageRequest);
            MailText = MailText.Replace("##RequestAttendance##", requests.attendanceRequest.ToString());

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "NWR Request");
            var receiverEmail = new MailAddress(emailConflict.Email, "Receiver");
            //var receiverEmail = new MailAddress("adisti.putri@ngkbusi.com", "Receiver");
            var ccEmail = new MailAddress(emailRequest.Email, "CC Recipient");
            var password = "100%NGKbusi!";
            var sub = "NWR Conflict Request";
            var body = MailText;
            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, password)
            };

            using (var mess = new MailMessage(senderEmail, receiverEmail)
            {
                Subject = sub,
                Body = body,
                IsBodyHtml = true
            })
            {
                mess.CC.Add(ccEmail);
                smtp.Send(mess);
            }
        }

        [HttpPost]
        public JsonResult GetRequest()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var requests = dbm.OTH_NWR_Requests
                .Where(r => r.statusR == 2 && r.NIKConflict == currUser)
                .Select(req => new {
                    req.id,
                    req.idConflict,
                    req.NIKConflict,
                    req.dateConflict,
                    req.startConflict,
                    req.endConflict,
                    req.roomConflict,
                    req.subjectConflict,
                    req.NIKRequest,
                    req.roomRequest,
                    req.dateRequest,
                    req.startRequest,
                    req.endRequest,
                    req.messageRequest,
                    req.statusR,
                    req.timestamps
                })
                .ToList();

            return Json(new { errorCode = 101, request = requests }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetRequestById(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var requests = dbm.OTH_NWR_Requests
                .Where(r => r.statusR == 2 && r.NIKConflict == currUser && r.id == id)
                .Select(req => new {
                    req.id,
                    req.idConflict,
                    req.NIKConflict,
                    req.dateConflict,
                    req.startConflict,
                    req.endConflict,
                    req.roomConflict,
                    req.subjectConflict,
                    req.NIKRequest,
                    req.roomRequest,
                    req.dateRequest,
                    req.startRequest,
                    req.endRequest,
                    req.messageRequest,
                    req.statusR,
                    req.timestamps
                })
                .ToList();

            return Json(new { errorCode = 101, request = requests }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteSchedule(int idBook)
        {
            var booking = dbm.OTH_NWR_Bookings.FirstOrDefault(b => b.ID == idBook);

            var currentTime = DateTime.Now.AddDays(-1);
            if (currentTime > booking.Day)
            {
                return Json(new { success = false, message = "Workspace cannot be deleted because it is outdated." });
            }

            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            if (booking.UserNIK != currUser)
            {
                return Json(new { success = false, message = "You have no right to delete this schedule." });
            }

            dbm.OTH_NWR_Bookings.Remove(booking);
            dbm.SaveChanges();

            return Json(new { success = true, message = "Workspace Deleted Successfully" });
        }

        //string FilePath = Path.Combine(Server.MapPath("~/Emails/IT/Reminder/"), "notif.html");


        //MailAddress to = new MailAddress("postmaster@eranet.id");
        //MailAddress from = new MailAddress("admin@eranet.id");
        //MailMessage message = new MailMessage(from, to);
        //message.Subject = "NWR Request";
        //    message.Body = "NWR Body";

        //    var password = "100%NGKbusi!";
        //var sub = "Portal Reminder";
        //var body = "bodyhtml";
        //var smtp = new SmtpClient
        //{
        //    Host = "mx-relay-1.eradata.id",
        //    Port = 2126,
        //    EnableSsl = false,
        //    DeliveryMethod = SmtpDeliveryMethod.Network,
        //    UseDefaultCredentials = false,
        //};

        //    try
        //    {
        //        smtp.Send(message);

        //    } catch (Exception ex)
        //    {

        //    }

        // MASTER
        [HttpPost]
        public JsonResult GetMasterRooms()
        {
            var rooms = dbm.OTH_NWR_Master_Rooms
                .Select(r => new {
                    r.ID,
                    r.RoomTitle,
                    r.IDRoomCat,
                    r.ExtensionNumber,
                    r.Image,
                    r.IsActive
                })
                .ToList();

            return Json(new { errorCode = 101, masterRooms = rooms }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetRoomCategories()
        {
            var categories = dbm.OTH_NWR_Master_Rooms_Category
                .Select(c => new {
                    c.ID,
                    c.Category
                })
                .ToList();

            return Json(new { errorCode = 101, roomCategories = categories }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetRoomProperties(int roomId)
        {
            var properties = dbm.OTH_NWR_Rooms_Properties
                .Where(p => p.RoomID == roomId)
                .Select(p => new {
                    p.ID,
                    p.RoomID,
                    p.PropsName,
                    p.Quantity
                })
                .ToList();

            return Json(new { errorCode = 101, roomProperties = properties }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult SaveMasterRoom(int id, string roomTitle, int idRoomCat, int extensionNumber, bool isActive, HttpPostedFileBase roomImageFile)
        {
            string fileName = null;

            if (roomImageFile != null && roomImageFile.ContentLength > 0)
            {
                string extension = System.IO.Path.GetExtension(roomImageFile.FileName);
                fileName = roomTitle.Replace(" ", "_").ToLower() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;

                string targetFolder = Server.MapPath("~/Content/Images/Room/");

                if (!System.IO.Directory.Exists(targetFolder))
                {
                    System.IO.Directory.CreateDirectory(targetFolder);
                }

                string fullPath = System.IO.Path.Combine(targetFolder, fileName);
                roomImageFile.SaveAs(fullPath);
            }

            if (id == 0)
            {
                var newRoom = new OTH_NWR_Master_Rooms
                {
                    RoomTitle = roomTitle,
                    IDRoomCat = idRoomCat,
                    ExtensionNumber = extensionNumber,
                    Image = fileName, 
                    IsActive = isActive
                };
                dbm.OTH_NWR_Master_Rooms.Add(newRoom);
            }
            else
            {
                var existingRoom = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(r => r.ID == id);
                if (existingRoom != null)
                {
                    existingRoom.RoomTitle = roomTitle;
                    existingRoom.IDRoomCat = idRoomCat;
                    existingRoom.ExtensionNumber = extensionNumber;
                    existingRoom.IsActive = isActive;

                    if (fileName != null)
                    {
                        existingRoom.Image = fileName;
                    }
                }
            }

            dbm.SaveChanges();
            return Json(new { errorCode = 101, message = "Data ruangan dan foto berhasil disimpan" });
        }

        [HttpPost]
        public JsonResult SaveRoomProperty(int id, int roomId, string propsName, int quantity)
        {
            if (id == 0)
            {
                // Insert Baru
                var newProp = new OTH_NWR_Rooms_Properties
                {
                    RoomID = roomId,
                    PropsName = propsName,
                    Quantity = quantity
                };
                dbm.OTH_NWR_Rooms_Properties.Add(newProp);
            }
            else
            {
                // Update Lama
                var existingProp = dbm.OTH_NWR_Rooms_Properties.FirstOrDefault(p => p.ID == id);
                if (existingProp != null)
                {
                    existingProp.PropsName = propsName;
                    existingProp.Quantity = quantity;
                }
            }

            dbm.SaveChanges();
            return Json(new { errorCode = 101, message = "Properti berhasil disimpan" });
        }
        [HttpPost]
        public JsonResult ToggleRoomStatus(int id)
        {
            var room = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(r => r.ID == id);
            if (room != null)
            {
                room.IsActive = !room.IsActive; 
                dbm.SaveChanges();
            }

            return Json(new { errorCode = 101, message = "Status ruangan berhasil diubah" });
        }

        [HttpPost]
        public JsonResult DeleteRoomProperty(int id)
        {
            var prop = dbm.OTH_NWR_Rooms_Properties.FirstOrDefault(p => p.ID == id);
            if (prop != null)
            {
                dbm.OTH_NWR_Rooms_Properties.Remove(prop);
                dbm.SaveChanges();
            }

            return Json(new { errorCode = 101, message = "Properti berhasil dihapus" });
        }

        [HttpPost]
        public JsonResult DeleteRoom(int id)
        {
            var relatedProperties = dbm.OTH_NWR_Rooms_Properties.Where(p => p.RoomID == id).ToList();
            if (relatedProperties.Any())
            {
                dbm.OTH_NWR_Rooms_Properties.RemoveRange(relatedProperties);
            }

            var room = dbm.OTH_NWR_Master_Rooms.FirstOrDefault(r => r.ID == id);
            if (room != null)
            {
                dbm.OTH_NWR_Master_Rooms.Remove(room);
                dbm.SaveChanges(); 

                return Json(new { errorCode = 101, message = "Ruangan beserta seluruh properti di dalamnya berhasil dihapus permanen." });
            }

            return Json(new { errorCode = 102, message = "Gagal! Data ruangan tidak ditemukan atau sudah dihapus oleh pengguna lain." });
        }
    }
}