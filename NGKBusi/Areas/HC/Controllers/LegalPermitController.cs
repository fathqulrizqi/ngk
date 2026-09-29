using AngleSharp.Network.Default;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Elmah;
using iTextSharp.xmp;
using Microsoft.AspNet.Identity;


//using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Areas.IT.Controllers;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Areas.Marketing.Models;
using NGKBusi.Models;
using NGKBusi.SignalR;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Services.Description;

namespace NGKBusi.Areas.HC.Controllers
{
    public class LegalPermitController : Controller
    {
        LegalPermitConnection dblp = new LegalPermitConnection();
        ReminderConnection dbr = new ReminderConnection();
        DefaultConnection db = new DefaultConnection();

        List<string> userAdmin = new List<string>() { "632.11.12", "831.03.19", "629.01.13", "876.06.24", "592.02.10" };

        // GET: HC/LegalPermit
        public ActionResult Index()
        {
            return View();
        }
        public class ListEmail
        {
            public string Email { get; set; }
        }
        public ActionResult Flow()
        {
            var spl = dblp.HC_LegalPermit_Flow.ToList();
            ViewBag.Flow = spl;
            return View();
        }
        private void SendRealtimeNotification(int agreementId, string source, string message, string senderName)
        {
            // 1. Siapkan Variable & Context
            var currUser = User.Identity.GetUserId(); // ID User yang sedang login (Pengirim)
            var recipients = new List<string>();      // List NIK Penerima
            string linkUrl = "";

            // Kita butuh akses ke tabel User Login (AspNetUsers) untuk menerjemahkan NIK ke GUID
            var dbIdentity = new ApplicationDbContext();

            // 2. Tentukan URL & Penerima berdasarkan Source
            if (source.ToLower() == "review")
            {
                // Ambil Data Review Request
                var data = dblp.HC_LegalPermit_ReviewAgreementRequest.Find(agreementId);

                if (data != null)
                {
                    // A. Tambahkan Creator (Jika yang komen bukan creator)
                    if (!string.IsNullOrEmpty(data.CreateBy) && data.CreateBy != currUser && data.Status == "Approved")
                    {
                        recipients.Add(data.CreateBy);
                    }

                    // B. Tambahkan Semua Admin (Kecuali yang komen jika dia admin)
                    var admins = dblp.HC_LegalPermit_UserAdmin.Select(x => x.UserNIK).ToList();
                    foreach (var adm in admins)
                    {
                        // Pastikan admin bukan diri sendiri dan belum ada di list recipients
                        if (adm != currUser && !recipients.Contains(adm))
                        {
                            recipients.Add(adm);
                        }
                    }

                    // Set URL ke halaman ProcessReview
                    linkUrl = Url.Action("ProcessReview", "LegalPermit", new { id = agreementId, area = "HC" });
                }
            }
            else if (source.ToLower() == "drafting")
            {
                // Ambil Data Review Request
                var data = dblp.HC_LegalPermit_DraftAgreementRequest.Find(agreementId);

                if (data != null)
                {
                    // A. Tambahkan Creator (Jika yang komen bukan creator)
                    if (!string.IsNullOrEmpty(data.CreatedBy) && data.CreatedBy != currUser && (data.Status == "Approved" || data.Status == "Revision"))
                    {
                        recipients.Add(data.CreatedBy);
                    }

                    // B. Tambahkan Semua Admin (Kecuali yang komen jika dia admin)
                    var admins = dblp.HC_LegalPermit_UserAdmin.Select(x => x.UserNIK).ToList();
                    foreach (var adm in admins)
                    {
                        // Pastikan admin bukan diri sendiri dan belum ada di list recipients
                        if (adm != currUser && !recipients.Contains(adm))
                        {
                            recipients.Add(adm);
                        }
                    }

                    // Set URL ke halaman ProcessReview
                    linkUrl = Url.Action("ProcessRequest", "LegalPermit", new { id = agreementId, area = "HC" });
                }
            }
            // else if (source == "Drafting") { ... logic drafting ... }

            // 3. Siapkan Hub Context SignalR
            var hubContext = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            // 4. Loop kirim notifikasi ke setiap penerima
            foreach (var targetNik in recipients)
            {
                try
                {
                    // A. SIMPAN KE DATABASE (History Notifikasi)
                    // Kita tetap simpan NIK di tabel history sesuai request bisnis
                    var notif = new HC_LegalPermit_Notification
                    {
                        RecipientNIK = targetNik,
                        senderNIK = senderName, // Pastikan nama property di Model sesuai (SenderName atau SenderNIK)
                        Message = message.Length > 40 ? message.Substring(0, 40) + "..." : message,
                        LinkUrl = linkUrl,
                        IsRead = 0, // Atau 0 jika tipe data int
                        CreatedDate = DateTime.Now
                    };

                    dblp.HC_LegalPermit_Notification.Add(notif);

                    var targetUser = db.V_Users_Active
                                 .FirstOrDefault(u => u.NIK == targetNik);                   

                    hubContext.Clients.Group(targetUser.NIK).broadcastNotif(senderName, notif.Message, notif.LinkUrl);
                    
                }
                catch (Exception ex)
                {
                    // Optional: Log error jika gagal kirim ke satu user, agar loop tidak berhenti
                    System.Diagnostics.Debug.WriteLine("Gagal kirim notif ke " + targetNik + ": " + ex.Message);
                }
            }

            // 5. Simpan perubahan ke Database Bisnis
            dblp.SaveChanges();

            // Dispose dbIdentity karena kita membuatnya manual di dalam function
            dbIdentity.Dispose();
        }

        private string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return "-";
            return System.Security.SecurityElement.Escape(value);
        }
        
        private string ToIndonesianWords(decimal amount)
        {
            if (amount == 0) return "nol rupiah";

            long rupiah = (long)Math.Floor(amount);
            int sen = (int)Math.Round((amount - rupiah) * 100);

            string words = NumberToWordsId(rupiah).Trim();
            words = string.IsNullOrWhiteSpace(words) ? "nol" : words;
            words = words + " rupiah";

            if (sen > 0)
            {
                words += " dan " + NumberToWordsId(sen) + " sen";
            }

            // Capitalize first letter
            return char.ToUpper(words[0]) + words.Substring(1);
        }

        // Helper: konversi ke kata-kata bahasa Inggris (mendukung s/d trillion)
        private string ToEnglishWords(decimal amount)
        {
            if (amount == 0) return "Zero rupiah";

            long dollars = (long)Math.Floor(amount);
            int cents = (int)Math.Round((amount - dollars) * 100);

            string words = NumberToWordsEn(dollars).Trim();
            words = string.IsNullOrWhiteSpace(words) ? "Zero" : words;
            words = words + " rupiah";

            if (cents > 0)
            {
                words += " and " + NumberToWordsEn(cents) + " cents";
            }

            // Capitalize first letter
            return char.ToUpper(words[0]) + words.Substring(1);
        }

        // Indonesian number to words (supports up to quintillions if needed)
        private string NumberToWordsId(long number)
        {
            if (number == 0) return "nol";

            if (number < 0) return "minus " + NumberToWordsId(Math.Abs(number));

            string[] units = { "", "satu", "dua", "tiga", "empat", "lima", "enam", "tujuh", "delapan", "sembilan", "sepuluh", "sebelas" };

            string words = "";

            if (number < 12)
            {
                words = units[number];
            }
            else if (number < 20)
            {
                words = NumberToWordsId(number - 10) + " belas";
            }
            else if (number < 100)
            {
                words = NumberToWordsId(number / 10) + " puluh" + (number % 10 > 0 ? " " + NumberToWordsId(number % 10) : "");
            }
            else if (number < 200)
            {
                words = "seratus" + (number - 100 > 0 ? " " + NumberToWordsId(number - 100) : "");
            }
            else if (number < 1000)
            {
                words = NumberToWordsId(number / 100) + " ratus" + (number % 100 > 0 ? " " + NumberToWordsId(number % 100) : "");
            }
            else if (number < 2000)
            {
                words = "seribu" + (number - 1000 > 0 ? " " + NumberToWordsId(number - 1000) : "");
            }
            else if (number < 1000000)
            {
                words = NumberToWordsId(number / 1000) + " ribu" + (number % 1000 > 0 ? " " + NumberToWordsId(number % 1000) : "");
            }
            else if (number < 1000000000)
            {
                words = NumberToWordsId(number / 1000000) + " juta" + (number % 1000000 > 0 ? " " + NumberToWordsId(number % 1000000) : "");
            }
            else if (number < 1000000000000)
            {
                words = NumberToWordsId(number / 1000000000) + " miliar" + (number % 1000000000 > 0 ? " " + NumberToWordsId(number % 1000000000) : "");
            }
            else if (number < 1000000000000000)
            {
                words = NumberToWordsId(number / 1000000000000) + " triliun" + (number % 1000000000000 > 0 ? " " + NumberToWordsId(number % 1000000000000) : "");
            }
            else
            {
                words = number.ToString(); // fallback for extremely large numbers
            }

            return words;
        }

        // English number to words (supports up to trillions here)
        private string NumberToWordsEn(long number)
        {
            if (number == 0) return "zero";

            if (number < 0) return "minus " + NumberToWordsEn(Math.Abs(number));

            var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
            var tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

            string words = "";

            if (number < 20)
                words = unitsMap[number];
            else if (number < 100)
                words = tensMap[number / 10] + (number % 10 > 0 ? "-" + unitsMap[number % 10] : "");
            else if (number < 1000)
                words = NumberToWordsEn(number / 100) + " hundred" + (number % 100 > 0 ? " " + NumberToWordsEn(number % 100) : "");
            else if (number < 1000000)
                words = NumberToWordsEn(number / 1000) + " thousand" + (number % 1000 > 0 ? " " + NumberToWordsEn(number % 1000) : "");
            else if (number < 1000000000)
                words = NumberToWordsEn(number / 1000000) + " million" + (number % 1000000 > 0 ? " " + NumberToWordsEn(number % 1000000) : "");
            else if (number < 1000000000000)
                words = NumberToWordsEn(number / 1000000000) + " billion" + (number % 1000000000 > 0 ? " " + NumberToWordsEn(number % 1000000000) : "");
            else
                words = number.ToString(); // fallback

            return words;
        }
        [HttpPost]
        public ActionResult AddFlow(string FlowName)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            HC_LegalPermit_Flow flow = new HC_LegalPermit_Flow();
            flow.FlowName = FlowName;
            flow.IsActive = "1";
            flow.CreateBy = currUser;
            flow.CreateTime = DateTime.Now;

            dblp.HC_LegalPermit_Flow.Add(flow);
            int i = dblp.SaveChanges();
            if (i > 0)
            {
                return Json(new { status = 1, msg = "Add Flow Success" });
            }
            else
            {
                return Json(new { status = 1, msg = "Failed to Add Flow" });
            }
        }
        [HttpPost]
        public ActionResult UpdateFlow(string FlowName, int ID)
        {
            var spl = dblp.HC_LegalPermit_Flow.Where(w => w.ID == ID).FirstOrDefault();
            spl.FlowName = FlowName;

            int save = dblp.SaveChanges();
            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "update success",
                    save = save
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to update",
                    save = save,
                    id = ID,
                    name = FlowName
                });
            }
        }
        [HttpPost]
        public ActionResult FormAddStep(int ID)
        {
            var flow = dblp.HC_LegalPermit_Flow.Where(w => w.ID == ID).FirstOrDefault();
            var listUser = db.V_Users_Active.ToList();

            ViewBag.flow = flow;
            ViewBag.listUser = listUser;
            return PartialView();
        }
        [HttpPost]
        public ActionResult AddStep(HC_LegalPermit_Flow_Detail smodel)
        {
            //get count step
            int count = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.Flow_ID == smodel.Flow_ID).ToList().Count();

            HC_LegalPermit_Flow_Detail flowDetail = new HC_LegalPermit_Flow_Detail();
            flowDetail.StepName = smodel.StepName;
            flowDetail.Description = smodel.Description;
            flowDetail.Estimation_Time = smodel.Estimation_Time;
            flowDetail.PIC = smodel.PIC;
            flowDetail.Flow_ID = smodel.Flow_ID;
            flowDetail.Requirement_Document = smodel.Requirement_Document;
            flowDetail.StepNumber = count + 1;
            dblp.HC_LegalPermit_Flow_Detail.Add(flowDetail);

            int save = dblp.SaveChanges();

            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                    save = save
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to save data",
                    save = save
                });
            }

        }
        public ActionResult DetailTemplateData(string FlowID)
        {

            var spl = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.Flow_ID.ToString() == FlowID && w.IsDelete == 0).OrderBy(x => x.StepNumber).ToList();
            var CountRow = dblp.HC_LegalPermit_Flow_Detail.Count(w => w.Flow_ID.ToString() == FlowID);

            List<Tbl_HC_LegalPermit_Flow_Detail> actions = new List<Tbl_HC_LegalPermit_Flow_Detail>();

            int no = 0;
            foreach (var Item in spl)
            {
                no++;
                var Tools = "";

                if (Item.StepNumber == 1)
                {
                    Tools += "<div class='btn-group'>";
                    Tools += "<button class='btn btn-danger' id='btndeleteStep' data-id='" + Item.ID + "'>";
                    Tools += "<i class='fas fa-trash'></i>";
                    Tools += "</button>";
                    Tools += "<button class='btn btn-primary ' id='btnDown' data-id='" + Item.ID + "' >";
                    Tools += "<i class='fas fa-caret-down'></i>";
                    Tools += "</button></div>";
                }
                else if (Item.StepNumber == CountRow)
                {
                    Tools += "<div class='btn-group'>";
                    Tools += "<button class='btn btn-danger' id='btndeleteStep' data-id='" + Item.ID + "'>";
                    Tools += "<i class='fas fa-trash'></i>";
                    Tools += "</button>";
                    Tools += "<button class='btn btn-info' id='btnUp' data-id='" + Item.ID + "'>";
                    Tools += "<i class='fas fa-caret-up'></i>";
                    Tools += "</button></div>";
                }
                else
                {
                    Tools += "<div class='btn-group'>";
                    Tools += "<button class='btn btn-danger' id='btndeleteStep' data-id='" + Item.ID + "'>";
                    Tools += "<i class='fas fa-trash'></i>";
                    Tools += "</button>";
                    Tools += "<button class='btn btn-info' id='btnUp' data-id='" + Item.ID + "'>";
                    Tools += "<i class='fas fa-caret-up'></i>";
                    Tools += "</button>";
                    Tools += "<button class='btn btn-primary' id='btnDown' data-id='" + Item.ID + "'>";
                    Tools += "<i class='fas fa-caret-down'></i>";
                    Tools += "</button></div>";
                }
                //var getPICInfo = db.V_Users_Active.Where(w => w.NIK == Item.PIC).FirstOrDefault();

                //Tools = "<a href=\"#\" title=\"Up\" id=\"btnUp\"  data-id=\"" + Item.ID + "\" class=\"btn-sm btn-danger UpStep\"><i class=\"fa fa-trash\"></i></a>";

                actions.Add(
                    new Tbl_HC_LegalPermit_Flow_Detail()
                    {
                        PIC = Item.PIC,
                        StepName = Item.StepName,
                        StepNumber = Item.StepNumber.ToString(),
                        Description = Item.Description,
                        EstimationTime = Item.Estimation_Time.ToString(),
                        Requirement_Document = Item.Requirement_Document,
                        No = no,
                        Button = Tools

                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
                status = 1
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult StepDown(int ID)
        {
            //turunkan step numbernya
            var spl = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.ID == ID).FirstOrDefault();
            int newStepNumber = spl.StepNumber = spl.StepNumber + 1;
            spl.StepNumber = newStepNumber;
            // naikkan step number dibawahnya
            var spl2 = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.Flow_ID == spl.Flow_ID && w.StepNumber == newStepNumber).FirstOrDefault();
            spl2.StepNumber = spl2.StepNumber - 1;
            int save = dblp.SaveChanges();
            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                    save = save
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to save data",
                    save = save
                });
            }

        }
        [HttpPost]
        public ActionResult StepUp(int ID)
        {
            //turunkan step numbernya
            var spl = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.ID == ID).FirstOrDefault();
            int newStepNumber = spl.StepNumber = spl.StepNumber - 1;
            spl.StepNumber = newStepNumber;
            // naikkan step number dibawahnya
            var spl2 = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.Flow_ID == spl.Flow_ID && w.StepNumber == newStepNumber).FirstOrDefault();
            spl2.StepNumber = spl2.StepNumber + 1;
            int save = dblp.SaveChanges();
            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                    save = save
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to save data",
                    save = save
                });
            }

        }
        [HttpPost]
        public ActionResult DeleteStep(int ID)
        {
            //turunkan step numbernya
            var spl = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.ID == ID).FirstOrDefault();
            spl.IsDelete = 1;
            int save = dblp.SaveChanges();
            if (save > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                    save = save
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed to save data",
                    save = save
                });
            }

        }

        /* ----------------------- Request controller ---------------------------------- */
        public ActionResult RequestList()
        {
            return View();

        }

        public JsonResult GetRequestList()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dblp.HC_LegalPermit_Request.OrderByDescending(o => o.ID).ToList();
            var CountRow = dblp.HC_LegalPermit_Request.Count();
            List<Tbl_HC_LegalPermit_Request> actions = new List<Tbl_HC_LegalPermit_Request>();

            foreach (var Item in spl)
            {
                //var Tools = "";

                var UrlAction = Url.Action("RequestDetail", "LegalPermit", new { area = "HC", ID = Item.ID.ToString() });

                var editButton = "";

                editButton = "<a href=\"" + UrlAction + "\" title=\"Detail\" >" + Item.ID + "</i></a>";

                actions.Add(
                    new Tbl_HC_LegalPermit_Request
                    {
                        RequestNo = editButton,
                        RequestDate = Item.CreateTime.ToString("MMMM dd, yyyy"),
                        SupplierName = Item.SupplierName,
                        ProjectName = Item.ProjectName,
                        FinalPrice = Item.FinalPrice.ToString(),
                        PlanStartDate = Item.PlanStartDate.ToString("MMMM dd, yyyy"),
                        //IsActive = activation,
                        PlanEndDate = Item.PlanEndDate.ToString("MMMM dd, yyyy"),
                    }); ;
            }

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult RequestForm()
        {
            return View();

        }
        [HttpPost]
        public ActionResult SubmitRequest(HC_LegalPermit_Request smodel, string breakdowncost, HttpPostedFileBase LegalDocument)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            HC_LegalPermit_Request request = new HC_LegalPermit_Request();
            request.RefQuotationNo = smodel.RefQuotationNo;
            request.SupplierName = smodel.SupplierName;
            request.PresidentDirector = smodel.PresidentDirector;
            request.Address = smodel.Address;
            request.TelpNo = smodel.TelpNo;
            request.FaxNo = smodel.FaxNo;
            request.ProjectName = smodel.ProjectName;
            request.Price = decimal.Parse(smodel.Price.ToString().Replace(".", ""));
            request.Discount = decimal.Parse(smodel.Discount.ToString().Replace(".", ""));
            request.FinalPrice = decimal.Parse(smodel.FinalPrice.ToString().Replace(".", ""));
            request.TermPayment = smodel.TermPayment;
            request.LeadTime = smodel.LeadTime;
            request.Penalty = int.Parse(smodel.Penalty.ToString().Replace(".", ""));
            request.BankAccountDetail = smodel.BankAccountDetail;
            request.Warranty = smodel.Warranty;
            request.PlanStartDate = smodel.PlanStartDate;
            request.PlanEndDate = smodel.PlanEndDate;
            request.CreateBy = CurrUser.NIK;
            request.CreateTime = DateTime.Now;

            if (LegalDocument.ContentLength > 0)
            {
                string extension = Path.GetExtension(LegalDocument.FileName);
                string filePath = Server.MapPath("~/Files/HC/LegalPermit/");
                string fileName = LegalDocument.FileName;
                filePath = filePath + fileName;

                LegalDocument.SaveAs(filePath);

                request.LegalDocument = fileName;
            }
            else
            {
                request.LegalDocument = "";
            }

            dblp.HC_LegalPermit_Request.Add(request);

            int r = dblp.SaveChanges();

            int ReqID = request.ID;

            int p = 0;
            if (r > 0)
            {
                // insert progress from prosedur template
                var prosedur = dblp.HC_LegalPermit_Flow_Detail.Where(w => w.Flow_ID == "1").ToList();
                foreach (var prs in prosedur)
                {
                    HC_LegalPermit_Request_Progress itemPrg = new HC_LegalPermit_Request_Progress
                    {
                        StepID = prs.ID,
                        ReqID = ReqID,
                        StepNumber = prs.StepNumber,
                        StepName = prs.StepName,
                        PIC = prs.PIC,
                        Status = 0,
                        Estimation_Time = prs.Estimation_Time
                    };
                    dblp.HC_LegalPermit_Request_Progress.Add(itemPrg);
                    int ip = dblp.SaveChanges();
                    if (ip > 0)
                    {
                        p++;
                    }
                }

            }

            // Deserialisasi ProdukList JSON menjadi List<ProdukModel>
            int s = 0;
            var detailBreakdown = JsonConvert.DeserializeObject<List<HC_LegalPermit_Request_BreakdownCost>>(breakdowncost);
            foreach (var brkdown in detailBreakdown)
            {

                HC_LegalPermit_Request_BreakdownCost item = new HC_LegalPermit_Request_BreakdownCost
                {
                    ItemName = brkdown.ItemName,
                    Price = decimal.Parse(brkdown.Price.ToString().Replace(".", "")),
                    ReqID = ReqID
                };

                dblp.HC_LegalPermit_Request_BreakdownCost.Add(item);
                int i = dblp.SaveChanges();
                if (i > 0)
                {
                    s++;
                }
            }

            if (s > 0 && r > 0 && p > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                    data = request,
                    breakdownCost = detailBreakdown,
                    detailBreakdown = detailBreakdown
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Failed Save Request",
                    data = request,
                    breakdownCost = detailBreakdown
                });
            }
        }

        public ActionResult RequestDetail(int ID)
        {
            var information = dblp.HC_LegalPermit_Request.Where(w => w.ID == ID).FirstOrDefault();
            var progress = dblp.HC_LegalPermit_Request_Progress.Where(w => w.ReqID == ID).ToList().OrderBy(o => o.StepNumber);
            var breakdownCost = dblp.HC_LegalPermit_Request_BreakdownCost.Where(w => w.ReqID == ID).ToList();

            ViewBag.information = information;
            ViewBag.progress = progress;
            ViewBag.breakdownCost = breakdownCost;
            return View();

        }

        public ActionResult ShowPdf(string fileName)
        {
            string filePath = Server.MapPath("~/Files/HC/LegalPermit/" + fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("File Not Found");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf");
        }
        public ActionResult Recap()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();


            ViewBag.subSection = CurrUser.SubSectionName;
            ViewBag.NavHide = true;
            ViewBag.userAdmin = userAdmin;
            ViewBag.User = CurrUser;
            return View();
        }

        public JsonResult GetRecapAgreementData()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            // read flags sent from ajaxRequest
            var showValidOnlyStr = Request.Form["showValidOnly"] ?? Request["showValidOnly"] ?? "0";
            int.TryParse(showValidOnlyStr, out int showValidOnlyInt);
            bool showValidOnly = showValidOnlyInt == 1;

            string status = "";
            string badge = "";

            string[] userAdmin = { "632.11.12", "831.03.19", "629.01.13", "876.06.24", "592.02.10" };

            Expression<Func<HC_LegalPermit_Recap_Agreement, bool>> filter = null;
            if (userAdmin.Contains(currUser))
            {
                filter = b => 1 == 1;
            }
            else
            {
                var idList = dblp.HC_LegalPermit_Share_User
                    .Where(a => a.NIK == currUser && a.LegalType == "Agreement")
                    .Select(a => a.Legal_ID);
                filter = b => idList.Contains(b.ID);
            }

            // base query: apply access filter first
            var query = dblp.HC_LegalPermit_Recap_Agreement.Where(filter);

            // valid-only filter: PeriodeEnd in the future
            if (showValidOnly)
            {
                query = query.Where(x => x.PeriodeEnd >= DateTime.Now);
            }
            else
            {
                query = query.Where(x => x.PeriodeEnd < DateTime.Now);
            }

            var spl = query.OrderByDescending(o => o.ID).ToList();

            var CountRow = spl.Count();
            List<Tbl_HC_LegalPermit_Recap_Agreement> actions = new List<Tbl_HC_LegalPermit_Recap_Agreement>();
            int No = 0;
            foreach (var Item in spl)
            {
                No++;
                var UrlAction = Url.Action("AgreementDetail", "LegalPermit", new { area = "HC", ID = Item.ID.ToString(), Legal = "Agreement" });
                var UrlAttachment = Url.Action("ViewPDF", "LegalPermit", new { area = "HC", fileName = Item.Attachment });

                // define status
                if (Item.PeriodeEnd <= DateTime.Now)
                {
                    status = "Invalid";
                    badge = "danger";
                }
                else
                {
                    status = "Valid";
                    badge = "success";
                }

                var shareButton = "<a href=\"#\" title=\"Share to User\" class=\"btn btn-sm btn-primary\" id=\"shareToUser\" data-id=\"" + Item.ID + "\" data-legal=\"Agreement\"><i class=\"fa fa-share-alt\"></i> </a>";
                var renewalButton = "";
                if (Item.IsRenewal == 1)
                {
                    var qRenewal = dblp.HC_LegalPermit_Recap_Agreement.Where(w => w.ID == Item.RenewalRefID).FirstOrDefault();
                    renewalButton = "<a href=\"" + UrlAction + "\" class=\"link-offset-2 link-offset-3-hover link-underline link-underline-opacity-0 link-underline-opacity-75-hover\">" + (qRenewal != null ? qRenewal.AgreementNo : "N/A") + "</a>";
                }
                else
                {
                    renewalButton = "<a href =\"#\" title=\"Renewal Document\" class=\"btn btn-sm btn-warning\" id=\"btnRenewal\" data-id=\"" + Item.ID + "\" data-legal=\"Agreement\"><i class=\"fa fa-refresh\"></i> </a>";
                }

                var bNote = "<span class=\"badge badge-pill badge-" + badge + "\">" + status + "</span>";

                actions.Add(
                    new Tbl_HC_LegalPermit_Recap_Agreement
                    {
                        Name = Item.AgreementName,
                        No = No,
                        SecondParty = Item.SecondParty,
                        AgreementNo = Item.AgreementNo,
                        DocumentID = "<a href=\"" + UrlAction + "\" class=\"link-offset-2 link-offset-3-hover link-underline link-underline-opacity-0 link-underline-opacity-75-hover\">" + Item.DocumentID + "</a>",
                        Attachment = "<a href=\"" + UrlAttachment + "\" class=\"link-offset-2 link-offset-3-hover link-underline link-underline-opacity-0 link-underline-opacity-75-hover\" title=\"Share to user\" target=\"_blank\">" + Item.Attachment + "</a>",
                        AgreementType = Item.AgreementType,
                        Note = bNote,
                        PeriodeStart = Item.PeriodeStart.ToString("dd MMM, yyyy"),
                        PeriodeEnd = Item.PeriodeEnd.ToString("dd MMM, yyyy"),
                        btnAlert = "<span>" + shareButton + " " + renewalButton + "</span>"
                    });
            }

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        public JsonResult GetRecapPermitData()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            // read flags sent from ajaxRequest
            var showValidOnlyStr = Request.Form["showPermitValidOnly"] ?? Request["showPermitValidOnly"] ?? "0";
            int.TryParse(showValidOnlyStr, out int showValidOnlyInt);
            bool showValidOnly = showValidOnlyInt == 1;

            string[] userAdmin = { "632.11.12", "831.03.19", "629.01.13", "876.06.24", "592.02.10" };

            Expression<Func<HC_LegalPermit_Recap_Permit, bool>> filter = null;
            if (userAdmin.Contains(currUser))
            {
                filter = b => 1 == 1;
            }
            else
            {
                //get share user
                var idList = dblp.HC_LegalPermit_Share_User
                    .Where(a => a.NIK == currUser && a.LegalType == "Permit")
                    .Select(a => a.Legal_ID);
                filter = b => idList.Contains(b.ID);
            }

            // base query: apply access filter first
            var query = dblp.HC_LegalPermit_Recap_Permit.Where(filter);
            // valid-only filter: Expired in the future
            if (showValidOnly)
            {
                query = query.Where(x => x.Expired >= DateTime.Now);
            }
            else
            {
                query = query.Where(x => x.Expired < DateTime.Now);
            }

            var spl = query.OrderByDescending(o => o.ID).ToList();

            var CountRow = spl.Count();
            List<Tbl_HC_LegalPermit_Recap_Permit> actions = new List<Tbl_HC_LegalPermit_Recap_Permit>();
            int No = 0;
            foreach (var Item in spl)
            {
                No++;

                string status = "";
                string badge = "";
                // get PIC Name
                var PIC = db.V_Users_Active.Where(w => w.NIK == Item.PIC).FirstOrDefault();

                // date expired condition
                string expired = "";
                if (Item.Expired == null)
                {
                    expired = "empty date";
                }
                else
                {
                    expired = Item.Expired?.ToString("dd MMM yyyy");
                    //var Tools = "";
                }
                // define status & badge
                if (Item.Expired == null)
                {
                    status = "Not Found End Date";
                    badge = "secondary";
                }
                else if (Item.Expired <= DateTime.Now)
                {
                    status = "Expired";
                    badge = "danger";

                }
                else
                {
                    status = "Valid";
                    badge = "success";
                }

                var UrlAction = Url.Action("AgreementDetail", "LegalPermit", new { area = "HC", ID = Item.ID.ToString(), Legal = "Permit" });
                var UrlAttachment = Url.Action("ViewPDF", "LegalPermit", new { area = "HC", fileName = Item.Attachment });
                //var editButton = "";
                var alertButton = "<a href=\"#\" title=\"Share to user\" class=\"btn btn-sm btn-primary\" id=\"shareToUser\" data-id=\"" + Item.ID + "\" data-legal=\"Permit\"><i class=\"fa fa-share-alt\"></i></a>";
                var bNote = "<span class=\"badge badge-pill badge-" + badge + "\">" + status + "</span>";
                var renewalButton = "";
                if (Item.IsRenewal == 1)
                {
                    var qRenewal = dblp.HC_LegalPermit_Recap_Permit.Where(w => w.ID == Item.RenewalRefID).FirstOrDefault();
                    renewalButton = "<a href=\"" + UrlAction + "\" class=\"link-offset-2 link-offset-3-hover link-underline link-underline-opacity-0 link-underline-opacity-75-hover\">" + (qRenewal != null ? qRenewal.Number : "N/A") + "</a>";
                }
                else
                {
                    renewalButton = "<a href =\"#\" title=\"Renewal Document\" class=\"btn btn-sm btn-warning\" id=\"btnRenewal\" data-id=\"" + Item.ID + "\" data-legal=\"Permit\"><i class=\"fa fa-refresh\"></i> </a>";
                }
                actions.Add(
                    new Tbl_HC_LegalPermit_Recap_Permit
                    {
                        Permit = Item.Permit,
                        No = No,
                        Number = Item.Number,
                        DocumentID = "<a href=\"" + UrlAction + "\" class=\"link-offset-2 link-offset-3-hover link-underline link-underline-opacity-0 link-underline-opacity-75-hover\">" + Item.DocumentID + "</a>",
                        SectionHandling = Item.SectionHandling,
                        Goverment = Item.Goverment,
                        PIC = PIC != null ? PIC.Name : (!string.IsNullOrEmpty(Item.PIC) ? Item.PIC : "N/A"),
                        Status = bNote,
                        Expired = expired,
                        Attachment = "<a href=\"" + UrlAttachment + "\" class=\"link-offset-2 link-offset-3-hover link-underline link-underline-opacity-0 link-underline-opacity-75-hover\" title=\"Share to user\" target=\"_blank\" >" + Item.Attachment + "</a>",
                        btnAlert = "<span>" + alertButton + " " + renewalButton + "</span>"
                    }); ;
            }

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult formAddAgreement(string ID)
        {
            ViewBag.ID = ID;
            var ListEmail = db.V_Users_Active.Where(w => w.Email != null).GroupBy(g => g.Email).Select(s => new ListEmail { Email = s.Key }).ToList();
            var ListVendor = db.V_AXVendorList.ToList();
            var ListUser = db.V_Users_Active.Where(w => w.SectionName == "GA" && w.Status == "Permanent" && w.PositionName != "OPERATOR").ToList();
            ViewBag.ListEmail = ListEmail;
            ViewBag.ListVendor = ListVendor;
            ViewBag.ListUser = ListUser;
            if (ID == "1")
            {
                var section = db.Users_Section_AX.ToList();
                ViewBag.Section = section;

            }

            return View();
        }
        [HttpPost]
        public ActionResult AddAgreement(HC_LegalPermit_Recap_Agreement smodelAgreement, HC_LegalPermit_Recap_Permit smodelPermit, IT_Reminder smodelReminder, string PeriodeStart, string PeriodeEnd, string[] selReminderUser)
        {
            int i;
            int insertReminder;

            // get formID
            var idForm = Request.Form.Get("IDForm");

            HttpPostedFileBase uploadFile = Request.Files["FileAttachment"];
            // get file attachment
            string filePath = "";
            string fileName = "";
            if (uploadFile != null && uploadFile.ContentLength > 0)
            {
                fileName = uploadFile.FileName;
                string extension = Path.GetExtension(fileName);
                filePath = Server.MapPath("~/Files/HC/LegalPermit/");
                filePath = filePath + fileName;
            }
            else
            {
                fileName = "not found";
                filePath = "not found";
            }

            /* insert reminder */
            IT_Reminder reminder = new IT_Reminder();
            reminder.ReminderTitle = idForm == "0" ? smodelAgreement.AgreementName : smodelPermit.Permit;
            reminder.Module = idForm == "0" ? "Agreement" : "Permit";
            reminder.Type = "internal";
            reminder.Thirdparty = idForm == "0" ? smodelAgreement.SecondParty : smodelPermit.Goverment;
            reminder.DueDate = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            reminder.Description = idForm == "0" ? smodelAgreement.AgreementName : smodelPermit.Permit;
            reminder.NotifStart = Convert.ToInt32(Request.Form.Get("NotifStart")) * -1;
            reminder.NotifTime = Request.Form.Get("NotifTime");
            reminder.IntervalRepetReminderType = "OneTime"; // no repeat reminder
            reminder.IntervalRepeatReminderNumber = 0;
            reminder.IntervalRepeatNotifType = Request.Form.Get("IntervalRepeatNotifType");
            reminder.IntervalRepeatNotifNumber = smodelReminder.IntervalRepeatNotifNumber;
            reminder.CreateTime = DateTime.Now;
            reminder.CreateBy = ((ClaimsIdentity)User.Identity).GetUserId();
            reminder.Attachment = fileName;
            reminder.IsActive = 1;

            dbr.IT_Reminder.Add(reminder);
            insertReminder = dbr.SaveChanges();
            int ReminderID = reminder.ID;


            if (idForm == "0")
            {
                HC_LegalPermit_Recap_Agreement data = new HC_LegalPermit_Recap_Agreement();
                data.AgreementName = smodelAgreement.AgreementName;
                data.AgreementNo = smodelAgreement.AgreementNo;
                data.SecondParty = smodelAgreement.SecondParty;
                data.PeriodeStart = DateTime.ParseExact(PeriodeStart, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                data.PeriodeEnd = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                data.AgreementType = smodelAgreement.AgreementType;
                data.ReminderID = ReminderID;
                data.CreatedAt = DateTime.Now;

                // Generate automatic Document ID (Format: AGRYYYY-XXXX)
                var currentYear = DateTime.Now.Year;
                var currentYearStr = currentYear.ToString();
                var prefix = "AGR" + currentYearStr + "-";
                
                var existingDocIDs = dblp.HC_LegalPermit_Recap_Agreement
                    .Where(w => w.DocumentID != null && w.DocumentID.StartsWith(prefix))
                    .Select(s => s.DocumentID)
                    .ToList();

                int nextSeq = 1;
                if (existingDocIDs.Any())
                {
                    var maxSeq = existingDocIDs
                        .Select(id => {
                            var parts = id.Split('-');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int seq))
                            {
                                return seq;
                            }
                            return 0;
                        })
                        .Max();
                    nextSeq = maxSeq + 1;
                }
                data.DocumentID = prefix + nextSeq.ToString("D4");

                if (uploadFile != null && uploadFile.ContentLength > 0)
                {
                    data.Attachment = fileName;
                    uploadFile.SaveAs(filePath);
                }

                dblp.HC_LegalPermit_Recap_Agreement.Add(data);
                i = dblp.SaveChanges();
                if (i > 0)
                {

                    if (insertReminder > 0)
                    {
                        // insert recipient user
                        foreach (var user in selReminderUser)
                        {
                            //var userInfo = db.V_Users_Active.Where(w => w.NIK == user).FirstOrDefault();
                            IT_Reminder_User userList = new IT_Reminder_User();
                            userList.ReminderID = reminder.ID;
                            userList.SendToUser = user;
                            userList.SendToUserEmail = user;
                            userList.IsActive = 1;

                            dbr.IT_Reminder_User.Add(userList);
                            int ins_user = dbr.SaveChanges();
                        }

                        int notifTimeInt = 9;
                        int.TryParse(reminder.NotifTime, out notifTimeInt);
                        IT_Reminder_Task newTask = new IT_Reminder_Task();
                        newTask.ReminderID = reminder.ID;
                        newTask.ReminderDate = reminder.DueDate.AddDays(reminder.NotifStart).Date;
                        newTask.ReminderTime = notifTimeInt;
                        newTask.ReminderDueDate = reminder.DueDate;

                        dbr.IT_Reminder_Task.Add(newTask);
                        int ins_task = dbr.SaveChanges();
                    }
                }
                else
                {

                    return Json(new
                    {
                        status = '0',
                        msg = "Failed to save Agreement",
                        //data = smodel
                    });
                }
            }
            else if (idForm == "1")
            {
                HC_LegalPermit_Recap_Permit data = new HC_LegalPermit_Recap_Permit();
                data.Permit = smodelPermit.Permit;
                data.Number = smodelPermit.Number;
                data.SectionHandling = smodelPermit.SectionHandling;
                data.Goverment = smodelPermit.Goverment;
                data.Expired = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                data.PIC = smodelPermit.PIC;
                data.Status = "Valid";
                data.ReminderID = ReminderID;
                data.CreatedAt = DateTime.Now;

                // Generate automatic Document ID (Format: PMTYYYY-XXXX)
                var currentYear = DateTime.Now.Year;
                var currentYearStr = currentYear.ToString();
                var prefix = "PMT" + currentYearStr + "-";
                
                var existingDocIDs = dblp.HC_LegalPermit_Recap_Permit
                    .Where(w => w.DocumentID != null && w.DocumentID.StartsWith(prefix))
                    .Select(s => s.DocumentID)
                    .ToList();

                int nextSeq = 1;
                if (existingDocIDs.Any())
                {
                    var maxSeq = existingDocIDs
                        .Select(id => {
                            var parts = id.Split('-');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int seq))
                            {
                                return seq;
                            }
                            return 0;
                        })
                        .Max();
                    nextSeq = maxSeq + 1;
                }
                data.DocumentID = prefix + nextSeq.ToString("D4");

                //HttpPostedFileBase uploadFile = Request.Files["FileAttachment"];
                //// get file attachment
                //string filePath = "";
                //string fileName = "";
                if (uploadFile != null && uploadFile.ContentLength > 0)
                {
                    //fileName = uploadFile.FileName;
                    //string extension = Path.GetExtension(fileName);
                    //filePath = Server.MapPath("~/Files/HC/LegalPermit/");
                    //filePath = filePath + fileName;

                    data.Attachment = fileName;
                    uploadFile.SaveAs(filePath);
                }
                //else
                //{
                //    fileName = "not found";
                //    filePath = "not found";
                //}
                dblp.HC_LegalPermit_Recap_Permit.Add(data);
                i = dblp.SaveChanges();
                if (i > 0)
                {
                    ///* insert reminder */
                    //IT_Reminder reminder = new IT_Reminder();
                    //reminder.ReminderTitle = smodelPermit.Permit;
                    //reminder.Module = "Permit";
                    //reminder.Type = "internal";
                    //reminder.Thirdparty = smodelPermit.Goverment;
                    //reminder.DueDate = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    //reminder.Description = smodelPermit.Permit;
                    //reminder.NotifStart = Convert.ToInt32(Request.Form.Get("NotifStart")) * -1;
                    //reminder.NotifTime = Request.Form.Get("NotifTime");
                    //reminder.IntervalRepetReminderType = "OneTime"; // no repeat reminder
                    //reminder.IntervalRepeatReminderNumber = 0;
                    //reminder.IntervalRepeatNotifType = Request.Form.Get("IntervalRepeatNotifType");
                    //reminder.IntervalRepeatNotifNumber = smodelReminder.IntervalRepeatNotifNumber;
                    //reminder.CreateTime = DateTime.Now;
                    //reminder.CreateBy = ((ClaimsIdentity)User.Identity).GetUserId();
                    //reminder.Attachment = fileName;
                    //reminder.IsActive = 1;

                    //dbr.IT_Reminder.Add(reminder);
                    //insertReminder = dbr.SaveChanges();
                    if (insertReminder > 0)
                    {
                        // insert recipient user
                        foreach (var user in selReminderUser)
                        {
                            //var userInfo = db.V_Users_Active.Where(w => w.NIK == user).FirstOrDefault();
                            IT_Reminder_User userList = new IT_Reminder_User();
                            userList.ReminderID = reminder.ID;
                            userList.SendToUser = user;
                            userList.SendToUserEmail = user;
                            userList.IsActive = 1;

                            dbr.IT_Reminder_User.Add(userList);
                            int ins_user = dbr.SaveChanges();
                        }

                        int notifTimeInt = 9;
                        int.TryParse(reminder.NotifTime, out notifTimeInt);
                        IT_Reminder_Task newTask = new IT_Reminder_Task();
                        newTask.ReminderID = reminder.ID;
                        newTask.ReminderDate = reminder.DueDate.AddDays(reminder.NotifStart).Date;
                        newTask.ReminderTime = notifTimeInt;
                        newTask.ReminderDueDate = reminder.DueDate;

                        dbr.IT_Reminder_Task.Add(newTask);
                        int ins_task = dbr.SaveChanges();
                    }
                }
                else
                {

                    return Json(new
                    {
                        status = '0',
                        msg = "Failed to save Agreement",
                        //data = smodel
                    });
                }
            }
            else
            {
                i = 0;
            }

            if (i > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                    //data = smodel,
                    //fileName = fileName,
                    //filepath = filePath
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Save Failed",
                    //data = smodel
                });
            }

        }
        [HttpPost]
        public ActionResult UpdateAgreement(HC_LegalPermit_Recap_Agreement smodelAgreement, HC_LegalPermit_Recap_Permit smodelPermit, IT_Reminder smodelReminder, string PeriodeStart, string PeriodeEnd, string[] selReminderUser)
        {
            int i;

            /* step 1 - upload file */
            HttpPostedFileBase uploadFile = Request.Files["FileAttachment"];
            // get file attachment
            string filePath = "";
            string fileName = "";
            if (uploadFile != null && uploadFile.ContentLength > 0)
            {
                fileName = uploadFile.FileName;
                string extension = Path.GetExtension(fileName);
                filePath = Server.MapPath("~/Files/HC/LegalPermit/");
                filePath = filePath + fileName;
            }
            else
            {
                fileName = "not found";
                filePath = "not found";
            }

            // get formID
            var idForm = Request.Form.Get("IDForm");
            if (idForm == "Agreement")
            {
                /* update agreement process */
                var id = smodelAgreement.ID;
                if (id == 0)
                {
                    int.TryParse(Request.Form.Get("ID"), out id);
                }
                var dataAgreement = dblp.HC_LegalPermit_Recap_Agreement.Where(w => w.ID == id).FirstOrDefault();
                if (dataAgreement == null)
                {
                    return Json(new
                    {
                        status = '0',
                        msg = "Agreement not found"
                    });
                }
                dataAgreement.AgreementName = smodelAgreement.AgreementName;
                dataAgreement.AgreementNo = smodelAgreement.AgreementNo;
                dataAgreement.SecondParty = smodelAgreement.SecondParty;
                dataAgreement.PeriodeStart = DateTime.ParseExact(PeriodeStart, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                dataAgreement.PeriodeEnd = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                dataAgreement.AgreementType = smodelAgreement.AgreementType;
                if (uploadFile != null && uploadFile.ContentLength > 0)
                {
                    dataAgreement.Attachment = fileName;
                    uploadFile.SaveAs(filePath);
                }

                i = dblp.SaveChanges();
                if (i >= 0)
                {
                    //update reminder header information
                    var reminder = dbr.IT_Reminder.Where(w => w.ID == dataAgreement.ReminderID).FirstOrDefault();
                    if (reminder != null)
                    {
                        reminder.ReminderTitle = dataAgreement.AgreementName;
                        reminder.Thirdparty = dataAgreement.SecondParty;
                        reminder.Description = dataAgreement.AgreementName;
                        reminder.DueDate = dataAgreement.PeriodeEnd;

                        if (uploadFile != null && uploadFile.ContentLength > 0)
                        {
                            reminder.Attachment = fileName;
                        }

                        dbr.SaveChanges();

                        // update reminder task safely using helper
                        ReminderController.RescheduleReminderTask(reminder.ID, dbr);
                    }
                    i = 1; // force success response even if i was 0 (no database changes detected by EF)
                }
                else
                {

                    return Json(new
                    {
                        status = '0',
                        msg = "Failed to save Agreement",
                        //data = smodel
                    });
                }
            }
            else if (idForm == "Permit")
            {
                var id = smodelPermit.ID;
                if (id == 0)
                {
                    int.TryParse(Request.Form.Get("ID"), out id);
                }
                var dataPermit = dblp.HC_LegalPermit_Recap_Permit.Where(w => w.ID == id).FirstOrDefault();
                if (dataPermit == null)
                {
                    return Json(new
                    {
                        status = '0',
                        msg = "Document not found"
                    });
                }
                dataPermit.Permit = smodelPermit.Permit;
                dataPermit.Number = smodelPermit.Number;
                dataPermit.SectionHandling = smodelPermit.SectionHandling;
                dataPermit.Goverment = smodelPermit.Goverment;
                dataPermit.Expired = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                dataPermit.PIC = smodelPermit.PIC;
                if (uploadFile != null && uploadFile.ContentLength > 0)
                {
                    dataPermit.Attachment = fileName;
                    uploadFile.SaveAs(filePath);
                }
                i = dblp.SaveChanges();
                if (i >= 0)
                {
                    /////update reminder header information
                    var reminder = dbr.IT_Reminder.Where(w => w.ID == dataPermit.ReminderID).FirstOrDefault();
                    if (reminder != null)
                    {
                        reminder.ReminderTitle = dataPermit.Permit;
                        reminder.Thirdparty = dataPermit.Goverment;
                        reminder.Description = dataPermit.Permit;
                        reminder.DueDate = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                        if (uploadFile != null && uploadFile.ContentLength > 0)
                        {
                            reminder.Attachment = fileName;
                        }

                        dbr.SaveChanges();

                        // update reminder task safely using helper
                        ReminderController.RescheduleReminderTask(reminder.ID, dbr);
                    }
                    i = 1; // force success response even if i was 0 (no database changes detected by EF)
                }
                else
                {

                    return Json(new
                    {
                        status = '0',
                        msg = "Failed to save Document",
                    });
                }
            }
            else
            {
                i = 0;
            }

            if (i > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Save Failed",
                    //data = smodel
                });
            }

        }
        [HttpPost]
        public ActionResult UpdateReminder(IT_Reminder smodel, string[] selReminderUser)
        {
            int u = 0;
            var ReminderData = dbr.IT_Reminder.Where(w => w.ID == smodel.ID).FirstOrDefault();
            if (ReminderData == null)
            {
                return Json(new { status = 0, msg = "Reminder not found" });
            }

            ReminderData.NotifStart = smodel.NotifStart * -1;
            ReminderData.NotifTime = smodel.NotifTime;
            ReminderData.IntervalRepeatNotifNumber = smodel.IntervalRepeatNotifNumber;
            ReminderData.IntervalRepeatNotifType = smodel.IntervalRepeatNotifType;
            u = dbr.SaveChanges();

            // update table IT_Reminder_Task safely using helper
            ReminderController.RescheduleReminderTask(smodel.ID, dbr);

            // delete & insert reminder user
            var ReminderUser = dbr.IT_Reminder_User.Where(w => w.ReminderID == smodel.ID).ToList();
            dbr.IT_Reminder_User.RemoveRange(ReminderUser);
            int r = 0;
            if (selReminderUser != null)
            {
                foreach (var user in selReminderUser)
                {
                    IT_Reminder_User userList = new IT_Reminder_User();
                    userList.ReminderID = smodel.ID;
                    userList.SendToUser = user;
                    userList.SendToUserEmail = user;
                    userList.IsActive = 1;

                    dbr.IT_Reminder_User.Add(userList);
                    int ins_user = dbr.SaveChanges();
                    r++;
                }
            }

            if (u > 0 || r > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = "Save success",
                });
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Save Failed",
                });
            }

        }
        [HttpGet]
        public ActionResult formRenewal(int DocumentID, string LegalType)
        {
            var qReminder = from p in dbr.IT_Reminder select p;
            var qReminderUser = from q in dbr.IT_Reminder_User select q;
            if (LegalType == "Agreement")
            {
                var qAgreement = dblp.HC_LegalPermit_Recap_Agreement.Where(w => w.ID == DocumentID).FirstOrDefault();

                qReminder = from p in qReminder where p.ID == qAgreement.ReminderID select p;
                qReminderUser = from q in qReminderUser where q.ReminderID == qAgreement.ReminderID select q;

                ViewBag.agreement = qAgreement;
                ViewBag.PrevAgreementID = DocumentID;
                ViewBag.PrevAgreementNo = qAgreement.AgreementNo;
            }
            else
            {
                var section = db.Users_Section_AX.ToList();
                var qPermit = dblp.HC_LegalPermit_Recap_Permit.Where(w => w.ID == DocumentID).FirstOrDefault();
                var qPICUser = db.V_Users_Active.Where(w => w.NIK == qPermit.PIC).ToList();
                qReminder = from p in qReminder where p.ID == qPermit.ReminderID select p;
                qReminderUser = from q in qReminderUser where q.ReminderID == qPermit.ReminderID select q;

                ViewBag.permit = qPermit;
                ViewBag.Section = section;
                ViewBag.PIC = qPICUser;
                ViewBag.PrevAgreementID = DocumentID;
                ViewBag.PrevAgreementNo = qPermit.Number;
            }

            var ListEmail = db.V_Users_Active.Where(w => w.Email != null).GroupBy(g => g.Email).Select(s => new ListEmail { Email = s.Key }).ToList();
            var ListVendor = db.V_AXVendorList.ToList();
            var ListUser = db.V_Users_Active.Where(w => w.SectionName == "GA" && w.Status == "Permanent" && w.PositionName != "OPERATOR").ToList();


            ViewBag.ListEmail = ListEmail;
            ViewBag.ListVendor = ListVendor;
            ViewBag.ListUser = ListUser;
            ViewBag.Reminder = qReminder.FirstOrDefault();
            ViewBag.ReminderUser = qReminderUser.ToList();


            ViewBag.LegalType = LegalType;

            return View();
        }
        [HttpPost]
        public ActionResult AddRenewal(HC_LegalPermit_Recap_Agreement smodelAgreement, HC_LegalPermit_Recap_Permit smodelPermit, IT_Reminder smodelReminder, string PeriodeStart, string PeriodeEnd, string[] selReminderUser, int PrevAgreementID, string PrevAgreementNo)
        {
            int i;
            int insertReminder;

            HttpPostedFileBase uploadFile = Request.Files["FileAttachment"];
            // get file attachment
            string filePath = "";
            string fileName = "";
            if (uploadFile != null && uploadFile.ContentLength > 0)
            {
                fileName = uploadFile.FileName;
                string extension = Path.GetExtension(fileName);
                filePath = Server.MapPath("~/Files/HC/LegalPermit/");
                filePath = filePath + fileName;
            }
            else
            {
                fileName = "not found";
                filePath = "not found";
            }

            // get formID
            var idForm = Request.Form.Get("IDForm");

            /* insert reminder */
            IT_Reminder reminder = new IT_Reminder();
            reminder.ReminderTitle = idForm == "Agreement" ? smodelAgreement.AgreementName : smodelPermit.Permit;
            reminder.Module = idForm == "Agreement" ? "Agreement" : "Permit";
            reminder.Type = "internal";
            reminder.Thirdparty = idForm == "Agreement" ? smodelAgreement.SecondParty : smodelPermit.Goverment;
            reminder.DueDate = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            reminder.Description = idForm == "Agreement" ? smodelAgreement.AgreementName : smodelPermit.Permit;
            reminder.NotifStart = Convert.ToInt32(Request.Form.Get("NotifStart")) * -1;
            reminder.NotifTime = Request.Form.Get("NotifTime");
            reminder.IntervalRepetReminderType = "OneTime"; // no repeat reminder
            reminder.IntervalRepeatReminderNumber = 0;
            reminder.IntervalRepeatNotifType = Request.Form.Get("IntervalRepeatNotifType");
            reminder.IntervalRepeatNotifNumber = smodelReminder.IntervalRepeatNotifNumber;
            reminder.CreateTime = DateTime.Now;
            reminder.CreateBy = ((ClaimsIdentity)User.Identity).GetUserId();
            reminder.Attachment = fileName;
            reminder.IsActive = 1;

            dbr.IT_Reminder.Add(reminder);
            insertReminder = dbr.SaveChanges();
            int ReminderID = reminder.ID;
            if (idForm == "Agreement")
            {
                /* insert agreement process */
                HC_LegalPermit_Recap_Agreement data = new HC_LegalPermit_Recap_Agreement();
                data.AgreementName = smodelAgreement.AgreementName;
                data.AgreementNo = smodelAgreement.AgreementNo;
                data.SecondParty = smodelAgreement.SecondParty;
                data.PeriodeStart = DateTime.ParseExact(PeriodeStart, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                data.PeriodeEnd = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                data.AgreementType = smodelAgreement.AgreementType;
                data.ReminderID = ReminderID;
                data.PrevAgreementID = PrevAgreementID;
                data.PrevAgreementNo = PrevAgreementNo;
                data.CreatedAt = DateTime.Now;

                // Generate automatic Document ID (Format: AGRYYYY-XXXX)
                var currentYear = DateTime.Now.Year;
                var currentYearStr = currentYear.ToString();
                var prefix = "AGR" + currentYearStr + "-";
                
                var existingDocIDs = dblp.HC_LegalPermit_Recap_Agreement
                    .Where(w => w.DocumentID != null && w.DocumentID.StartsWith(prefix))
                    .Select(s => s.DocumentID)
                    .ToList();

                int nextSeq = 1;
                if (existingDocIDs.Any())
                {
                    var maxSeq = existingDocIDs
                        .Select(id => {
                            var parts = id.Split('-');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int seq))
                            {
                                return seq;
                            }
                            return 0;
                        })
                        .Max();
                    nextSeq = maxSeq + 1;
                }
                data.DocumentID = prefix + nextSeq.ToString("D4");

                if (uploadFile != null && uploadFile.ContentLength > 0)
                {
                    data.Attachment = fileName;
                    uploadFile.SaveAs(filePath);
                }

                dblp.HC_LegalPermit_Recap_Agreement.Add(data);
                i = dblp.SaveChanges();

                if (i > 0)
                {
                    reminder.ModuleReferalID = data.ID;
                    reminder.ReferalDocumentNo = data.AgreementNo;
                    dbr.SaveChanges();
                }

                // update prev agreement status is Renewal & deactivate old reminder
                var qPrevAgreement = dblp.HC_LegalPermit_Recap_Agreement.Where(w => w.ID == PrevAgreementID).FirstOrDefault();
                if (qPrevAgreement != null)
                {
                    qPrevAgreement.IsRenewal = 1;
                    qPrevAgreement.RenewalRefID = data.ID;
                    dblp.SaveChanges();

                    if (qPrevAgreement.ReminderID > 0)
                    {
                        var oldReminder = dbr.IT_Reminder.FirstOrDefault(w => w.ID == qPrevAgreement.ReminderID);
                        if (oldReminder != null)
                        {
                            oldReminder.IsActive = 0;
                            var oldPendingTasks = dbr.IT_Reminder_Task.Where(w => w.ReminderID == oldReminder.ID && w.IsSend == 0).ToList();
                            foreach (var opt in oldPendingTasks)
                            {
                                opt.IsSend = 1;
                            }
                            dbr.SaveChanges();
                        }
                    }
                }

                // insert share user
                var shareUserList = dblp.HC_LegalPermit_Share_User.Where(w => w.Legal_ID == PrevAgreementID && w.LegalType == "Agreement" && w.IsDelete == 0).ToList();
                foreach (var shareUser in shareUserList)
                {
                    HC_LegalPermit_Share_User shr = new HC_LegalPermit_Share_User();
                    shr.NIK = shareUser.NIK;
                    shr.IsDelete = shareUser.IsDelete;
                    shr.CreateBy = shareUser.CreateBy;
                    shr.CreateTime = shareUser.CreateTime;
                    shr.Legal_ID = data.ID;
                    shr.LegalType = "Agreement";
                    dblp.HC_LegalPermit_Share_User.Add(shr);
                }
                dblp.SaveChanges();

                if (i > 0)
                {
                    // insert reminder user
                    if (insertReminder > 0)
                    {
                        // insert recipient user
                        foreach (var user in selReminderUser)
                        {
                            //var userInfo = db.V_Users_Active.Where(w => w.NIK == user).FirstOrDefault();
                            IT_Reminder_User userList = new IT_Reminder_User();
                            userList.ReminderID = reminder.ID;
                            userList.SendToUser = user;
                            userList.SendToUserEmail = user;
                            userList.IsActive = 1;

                            dbr.IT_Reminder_User.Add(userList);
                            int ins_user = dbr.SaveChanges();
                        }
                        // insert reminder task
                        int notifTimeInt = 9;
                        int.TryParse(reminder.NotifTime, out notifTimeInt);
                        IT_Reminder_Task newTask = new IT_Reminder_Task();
                        newTask.ReminderID = reminder.ID;
                        newTask.ReminderDate = reminder.DueDate.AddDays(reminder.NotifStart).Date;
                        newTask.ReminderTime = notifTimeInt;
                        newTask.ReminderDueDate = reminder.DueDate;

                        dbr.IT_Reminder_Task.Add(newTask);
                        int ins_task = dbr.SaveChanges();
                    }
                }
                else
                {

                    return Json(new
                    {
                        status = 0,
                        msg = "Failed to save Agreement",
                        //data = smodel
                    });
                }
            }
            else if (idForm == "Permit")
            {
                HC_LegalPermit_Recap_Permit data = new HC_LegalPermit_Recap_Permit();
                data.Permit = smodelPermit.Permit;
                data.Number = smodelPermit.Number;
                data.SectionHandling = smodelPermit.SectionHandling;
                data.Goverment = smodelPermit.Goverment;
                data.Expired = DateTime.ParseExact(PeriodeEnd, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                data.PIC = smodelPermit.PIC;
                data.Status = "Valid";
                data.ReminderID = ReminderID;
                data.PrevPermitID = PrevAgreementID;
                data.PrevPermitNo = PrevAgreementNo;
                data.CreatedAt = DateTime.Now;

                // Generate automatic Document ID (Format: PMTYYYY-XXXX)
                var currentYear = DateTime.Now.Year;
                var currentYearStr = currentYear.ToString();
                var prefix = "PMT" + currentYearStr + "-";
                
                var existingDocIDs = dblp.HC_LegalPermit_Recap_Permit
                    .Where(w => w.DocumentID != null && w.DocumentID.StartsWith(prefix))
                    .Select(s => s.DocumentID)
                    .ToList();

                int nextSeq = 1;
                if (existingDocIDs.Any())
                {
                    var maxSeq = existingDocIDs
                        .Select(id => {
                            var parts = id.Split('-');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int seq))
                            {
                                return seq;
                            }
                            return 0;
                        })
                        .Max();
                    nextSeq = maxSeq + 1;
                }
                data.DocumentID = prefix + nextSeq.ToString("D4");

                if (uploadFile != null && uploadFile.ContentLength > 0)
                {
                    data.Attachment = fileName;
                    uploadFile.SaveAs(filePath);
                }
                dblp.HC_LegalPermit_Recap_Permit.Add(data);
                i = dblp.SaveChanges();

                if (i > 0)
                {
                    reminder.ModuleReferalID = data.ID;
                    reminder.ReferalDocumentNo = data.Number;
                    dbr.SaveChanges();
                }

                // update prev agreement status is Renewal & deactivate old reminder
                var qPrevPermit = dblp.HC_LegalPermit_Recap_Permit.Where(w => w.ID == PrevAgreementID).FirstOrDefault();
                if (qPrevPermit != null)
                {
                    qPrevPermit.IsRenewal = 1;
                    qPrevPermit.RenewalRefID = data.ID;
                    dblp.SaveChanges();

                    if (qPrevPermit.ReminderID > 0)
                    {
                        var oldReminder = dbr.IT_Reminder.FirstOrDefault(w => w.ID == qPrevPermit.ReminderID);
                        if (oldReminder != null)
                        {
                            oldReminder.IsActive = 0;
                            var oldPendingTasks = dbr.IT_Reminder_Task.Where(w => w.ReminderID == oldReminder.ID && w.IsSend == 0).ToList();
                            foreach (var opt in oldPendingTasks)
                            {
                                opt.IsSend = 1;
                            }
                            dbr.SaveChanges();
                        }
                    }
                }

                // insert share user
                var shareUserList = dblp.HC_LegalPermit_Share_User.Where(w => w.Legal_ID == PrevAgreementID && w.LegalType == "Permit" && w.IsDelete == 0).ToList();
                foreach (var shareUser in shareUserList)
                {
                    HC_LegalPermit_Share_User shr = new HC_LegalPermit_Share_User();
                    shr.NIK = shareUser.NIK;
                    shr.IsDelete = shareUser.IsDelete;
                    shr.CreateBy = shareUser.CreateBy;
                    shr.CreateTime = shareUser.CreateTime;
                    shr.Legal_ID = data.ID;
                    shr.LegalType = "Permit";
                    dblp.HC_LegalPermit_Share_User.Add(shr);
                }
                dblp.SaveChanges();

                if (i > 0)
                {
                    if (insertReminder > 0)
                    {
                        // insert recipient user
                        foreach (var user in selReminderUser)
                        {
                            //var userInfo = db.V_Users_Active.Where(w => w.NIK == user).FirstOrDefault();
                            IT_Reminder_User userList = new IT_Reminder_User();
                            userList.ReminderID = reminder.ID;
                            userList.SendToUser = user;
                            userList.SendToUserEmail = user;
                            userList.IsActive = 1;

                            dbr.IT_Reminder_User.Add(userList);
                            int ins_user = dbr.SaveChanges();
                        }

                        int notifTimeInt = 9;
                        int.TryParse(reminder.NotifTime, out notifTimeInt);
                        IT_Reminder_Task newTask = new IT_Reminder_Task();
                        newTask.ReminderID = reminder.ID;
                        newTask.ReminderDate = reminder.DueDate.AddDays(reminder.NotifStart).Date;
                        newTask.ReminderTime = notifTimeInt;
                        newTask.ReminderDueDate = reminder.DueDate;

                        dbr.IT_Reminder_Task.Add(newTask);
                        int ins_task = dbr.SaveChanges();
                    }
                }
                else
                {

                    return Json(new
                    {
                        status = 0,
                        msg = "Failed to save Agreement",
                    });
                }
            }
            else
            {
                i = 0;
            }

            if (i > 0)
            {
                return Json(new
                {
                    status = 1,
                    msg = "Save success"
                });
            }
            else
            {
                return Json(new
                {
                    status = 0,
                    msg = "Save Failed"
                });
            }

        }
        public ActionResult formShareToUser(string ID, string legalType)
        {
            var user = db.V_Users_Active.ToList();
            ViewBag.user = user;
            ViewBag.Legal_ID = ID;
            ViewBag.LegalType = legalType;

            return PartialView();
        }
        [HttpPost]
        public ActionResult AddShareToUser(HC_LegalPermit_Share_User smodel, string[] UserNIK)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            foreach (var usr in UserNIK)
            {
                HC_LegalPermit_Share_User data = new HC_LegalPermit_Share_User();
                data.NIK = usr;
                data.IsDelete = 0;
                data.Legal_ID = smodel.Legal_ID;
                data.LegalType = smodel.LegalType;
                data.CreateBy = currUser;
                data.CreateTime = DateTime.Now;
                dblp.HC_LegalPermit_Share_User.Add(data);

            }
            int s = dblp.SaveChanges();
            if (s > 0)
            {
                return Json(new
                {
                    status = '1',
                    msg = "Save success",
                    //data = smodel,
                    //fileName = fileName,
                    //filepath = filePath
                });
            }
            else
            {
                return Json(new
                {
                    status = '0',
                    msg = "Save Failed",
                    //data = smodel
                });
            }
        }
        public JsonResult GetShareUser(int id, string legalType)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            //var spl = db.V_SCM_Sparepart_Master_List.Where(w =>  w.CostName == CurrUser.CostName && w.ItemGroup == "MachineP" || w.ItemGroup == "Tooling").ToList();
            var spl = dblp.HC_LegalPermit_Share_User.Where(w => w.Legal_ID == id && w.LegalType == legalType).OrderByDescending(o => o.ID).ToList();
            var CountRow = dblp.HC_LegalPermit_Share_User.Where(w => w.Legal_ID == id && w.LegalType == legalType).Count();
            List<Tbl_HC_LegalPermit_Share_User> actions = new List<Tbl_HC_LegalPermit_Share_User>();
            int No = 0;
            foreach (var Item in spl)
            {
                No++;

                // get user detail information
                var qUser = db.V_Users_Active.Where(w => w.NIK == Item.NIK).FirstOrDefault();
                actions.Add(
                    new Tbl_HC_LegalPermit_Share_User
                    {
                        NIK = Item.NIK,
                        No = No,
                        Name = qUser.Name,
                        Section = qUser.SectionName,
                        Action = "<a href=\"#\" title=\"delete user\" class=\"btn btn-sm btn-danger\" id=\"deleteShareUser\" data-id=\"" + Item.ID + "\" \"><i class=\"fa fa-trash\"></i></a>"
                    }); ;
            }

            var jsonResult = Json(new { rows = actions, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        public ActionResult AgreementDetail(int ID, string Legal)
        {
            var qReminder = from p in dbr.IT_Reminder select p;
            var qReminderUser = from q in dbr.IT_Reminder_User select q;
            if (Legal == "Agreement")
            {
                var qAgreement = dblp.HC_LegalPermit_Recap_Agreement.Where(w => w.ID == ID).FirstOrDefault();

                qReminder = from p in qReminder where p.ID == qAgreement.ReminderID select p;
                qReminderUser = from q in qReminderUser where q.ReminderID == qAgreement.ReminderID select q;

                ViewBag.agreement = qAgreement;
            }
            else
            {
                var section = db.Users_Section_AX.ToList();
                var qPermit = dblp.HC_LegalPermit_Recap_Permit.Where(w => w.ID == ID).FirstOrDefault();
                var qPICUser = db.V_Users_Active.Where(w => w.NIK == qPermit.PIC).ToList();
                qReminder = from p in qReminder where p.ID == qPermit.ReminderID select p;
                qReminderUser = from q in qReminderUser where q.ReminderID == qPermit.ReminderID select q;

                ViewBag.permit = qPermit;
                ViewBag.Section = section;
                ViewBag.PIC = qPICUser;
            }

            var ListEmail = db.V_Users_Active.Where(w => w.Email != null).GroupBy(g => g.Email).Select(s => new ListEmail { Email = s.Key }).ToList();
            var ListNewEmail = dbr.IT_Reminder_User.GroupBy(g => g.SendToUser).Select(s => new ListEmail { Email = s.Key }).ToList();
            var ListVendor = db.V_AXVendorList.ToList();
            var ListUser = db.V_Users_Active.Where(w => w.SectionName == "GA" && w.Status == "Permanent" && w.PositionName != "OPERATOR").ToList();


            ViewBag.ListEmail = ListEmail.Union(ListNewEmail).GroupBy(x => x.Email).Select(y => new ListEmail { Email = y.Key }).ToList(); ;
            ViewBag.ListVendor = ListVendor;
            ViewBag.ListUser = ListUser;
            ViewBag.Reminder = qReminder.FirstOrDefault();
            ViewBag.ReminderUser = qReminderUser.ToList();

            ViewBag.LegalType = Legal;

            return View();
        }
        [HttpGet]
        public ActionResult ViewPDF(string fileName)
        {
            string filePath = "~/Files/HC/LegalPermit/" + fileName;
            Response.AddHeader("Content-Disposition", "inline; filename=" + fileName);

            return File(filePath, "application/pdf");
        }

        /* ----- CHAPTER 2 REQUEST DARAFTING AGREEMENT ----- */

        public void GenerateApprovalFlow(string draftingId, decimal finalPrice)
        {
            // [DEBUG] 2. Cek apakah masuk fungsi
            Debug.WriteLine($"[DEBUG] --> Masuk GenerateApprovalFlow. ID={draftingId}, Price={finalPrice}");
            // 1. Ambil Master Approval yang Aktif
            var masterSteps = db.Approval_Master
                                .Where(m => m.minAmount <= finalPrice && m.Menu_Id == 1029)
                                .OrderBy(m => m.Levels)
                                .ToList();

            // [DEBUG] 3. Cek hasil query master
            Debug.WriteLine($"[DEBUG] --> Master Step Count: {masterSteps.Count}");
            if (masterSteps.Count == 0) return;

            // 2. Insert ke Approval List (Transaksi)
            int loopIndex = 0;
            foreach (var step in masterSteps)
            {
                string actionLabel = !string.IsNullOrEmpty(step.Title) ? step.Title : "Approval";
                //var minAmountExist = masterSteps.Where(w=>w.minAmount > 0).FirstOrDefault();
                var trans = new Approval_List
                {
                    Reveral_ID = draftingId,
                    Reveral_ID_Sub = null,
                    Menu_Id = 1029,
                    Document_Id = 1,
                    User_NIK = step.User_NIK,
                    Dept_Code = step.Dept_Code,
                    Dept_Name = step.Dept_Name,
                    Title = step.Title,
                    Levels = step.Levels,
                    Levels_Sub = step.Levels_Sub,
                    Is_Skip = false,
                    IsEmailOnly = step.IsEmailOnly,
                };
                db.Approval_List.Add(trans);
                loopIndex++;
            }

            int id = int.Parse(draftingId);
            var doc = dblp.HC_LegalPermit_DraftAgreementRequest.Find(id);

            if (doc != null)
            {
                var firstStep = masterSteps.First();

                // Ambil label aksi (misal: "Review", "Check", "Approval")
                string currentAction = !string.IsNullOrEmpty(firstStep.Title) ? firstStep.Title : "Approval";

                // FORMAT: "Waiting [Action] - [Title]"
                // Contoh: "Waiting Review - Staff Legal" atau "Waiting Approval - Director"
                doc.Status = "Submitted";
            }

            // 4. Catat History
            // Parameter: ID, Actor, Action, Comment
            //LogHistory(draftingId, "System", "Submitted", "Workflow Generated based on amount: " + finalPrice);

            db.SaveChanges();
        }

        public void LogHistory(string draftingId, string actorNik, string actorName, string action, int approval_level, int approval_sub, string note)
        {
            try
            {
                var history = new Approval_History
                {
                    // Pastikan tipe data DraftingID sesuai dengan di database (int atau string)
                    Reveral_ID = draftingId,
                    Menu_Id = 1029,
                    Menu_Name = "Drafting Agreement Request",
                    Document_Id = 1,
                    Document_Name = "Form Request",
                    Reveral_ID_Sub = null,  
                    Title = action,
                    Approval = approval_level,
                    Approval_Sub = approval_sub,
                    Note = note,
                    IsReject = false,
                    IsRevise = false,
                    Status = action,
                    Created_At = DateTime.Now,
                    Created_By_ID = actorNik,
                    Created_By_Name = actorName
                };

                db.Approval_History.Add(history);
                db.SaveChanges();

            }
            catch (Exception ex)
            {
                // Error handling agar log error tidak mematikan flow utama
                System.Diagnostics.Debug.WriteLine("Failed to write history: " + ex.Message);
            }
        }

        [Authorize]
        public ActionResult DraftingRequest()
        {
            // 1. Ambil Username yang sedang login
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            // 2. Tentukan siapa saja Admin-nya (Bisa hardcode atau ambil dari tabel Config/Roles)
            var adminUsers = dblp.HC_LegalPermit_UserAdmin
                            .Where(w => w.IsDeleted == 0)
                            .Select(w => w.UserNIK)
                            .ToList();

            // Cek apakah user saat ini adalah admin
            bool isAdmin = adminUsers.Contains(currUser.ToLower());

            IQueryable<HC_LegalPermit_DraftAgreementRequest> query = dblp.HC_LegalPermit_DraftAgreementRequest;
            if (isAdmin)
            {
                // ADMIN: Melihat SEMUA data
                query = query.Where(x => x.Status != "Draft");
            }
            else
            {
                // USER BIASA: Melihat data buatan sendiri ATAU data yang melibatkan dia sebagai reviewer (yang bukan Draft)
                query = query.Where(x => x.CreatedBy == currUser || 
                                         (x.Status != "Draft" && dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer
                                                                    .Any(r => r.DraftingReqID == x.ID && r.SendToNIK == currUser)));
            }

            // Ambil data, urutkan dari yang terbaru
            var list = query.OrderByDescending(x => x.RequestDate).ToList();

            ViewBag.List = list;
            ViewBag.IsAdmin = isAdmin;

            return View();

        }
        [Authorize]
        public ActionResult DraftingForm()
        {
            var ListVendor = db.V_AXVendorList.ToList();

            var excludedPositions = new[] { "OPERATOR", "KOPERASI", "" };
            var ListUser = db.V_Users_Active
                .Where(w => !excludedPositions.Contains(w.PositionName))
                .ToList();

            ViewBag.ListVendor = ListVendor;
            ViewBag.ListUser = ListUser;
            return View();
        }

        [HttpPost]
        public ActionResult SaveDrafting(HC_LegalPermit_DraftAgreementRequest model)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            try
            {
                // 1. Validasi Server Side (Double Check)
                if (model == null)
                    return Json(new { success = false, message = "Data kosong/corrupt" });

                if (string.IsNullOrEmpty(model.ProjectDetails))
                    return Json(new { success = false, message = "Detail Project Kosong" });

                // 2. Simpan ke Database (Contoh menggunakan EF Core)                              
                var entity = new HC_LegalPermit_DraftAgreementRequest
                {
                    RequestDate = model.RequestDate,
                    RefQuotationNo = model.RefQuotationNo,
                    SupplierName = model.SupplierName,
                    PresidentDirector = model.PresidentDirector,
                    Address = model.Address,
                    TelpNo = model.TelpNo,
                    EmailAddress = model.EmailAddress,
                    ProjectName = model.ProjectName,
                    // HTML disimpan ke database
                    ProjectDetails = model.ProjectDetails,

                    Price = model.Price,
                    Discount = model.Discount,
                    FinalPrice = model.FinalPrice,
                    TermOfPayment = model.TermOfPayment,
                    BankAccount = model.BankAccount,
                    LeadTimeWorks = model.LeadTimeWorks,
                    Warranty = model.Warranty,
                    PenaltyClause = model.PenaltyClause,

                    PlanStartDate = model.PlanStartDate,
                    PlanEndDate = model.PlanEndDate,

                    CreatedAt = DateTime.Now,
                    CreatedBy = currUser,

                    IsDeleted = 0,

                    Status = model.Status,
                    CurrApprovalLevel = model.Status == "Submitted" ? 1 : 0
                };

                dblp.HC_LegalPermit_DraftAgreementRequest.Add(entity);
                dblp.SaveChanges();

                // 3. Simpan Reviewers (Looping List)
                if (model.Reviewers != null && model.Reviewers.Count > 0)
                {
                    foreach (var nikReviewer in model.Reviewers)
                    {
                        
                        var reviewerMap = new HC_LegalPermit_DraftAgreementRequest_Reviewer();
                        reviewerMap.DraftingReqID = entity.ID; // Ambil ID yang baru
                        reviewerMap.SendToNIK = nikReviewer;

                        // Cari info user untuk simpan Email 
                        var userInfo = db.V_Users_Active.FirstOrDefault(u => u.NIK == nikReviewer);
                        reviewerMap.SendToEmail = userInfo?.Email;

                        dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer.Add(reviewerMap);
                    }
                    dblp.SaveChanges();
                }

                if (model.Status == "Submitted")
                {
                    Console.WriteLine("Status is Submitted, generating approval flow...");
                    SendEmail(entity.CreatedBy, entity, "Submitted", entity.ID);

                    try
                    {
                       

                        GenerateApprovalFlow(entity.ID.ToString(), model.FinalPrice);

                    }
                    catch (Exception genEx)
                    {
                        if (genEx.InnerException != null)
                            Debug.WriteLine($"[ERROR] Inner: {genEx.InnerException.Message}");

                        // Opsional: Lempar error agar muncul di SweetAlert browser
                        throw new Exception("Flow Error: " + genEx.Message);
                    }
                }


                return Json(new { success = true, message = "Data saved successfully", id = entity.ID });
            }
            catch (Exception ex)
            {
                // Mengambil pesan error terdalam (Inner Exception)
                var errorMessage = ex.Message;

                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner: " + ex.InnerException.Message;

                    // error SQL di level lebih dalam lagi
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMessage += " | Deep: " + ex.InnerException.InnerException.Message;
                    }
                }

                // Kembalikan pesan detail ke JSON agar bisa dibaca di SweetAlert
                return Json(new { success = false, message = errorMessage });
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult EditDraftingForm(int ID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            // Ambil data berdasarkan ID
            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == ID);

            //if (data == null)
            //{
            //    return NotFound(); 
            //}

            if (!data.CreatedBy.Equals(currUser, StringComparison.OrdinalIgnoreCase))
            {
                // OPSI A: Redirect kembali ke Index dengan pesan error (Lebih User Friendly)
                TempData["ErrorMessage"] = "Akses Ditolak: Anda hanya bisa mengedit data yang Anda buat sendiri.";
                return RedirectToAction("DraftingRequest");

                // OPSI B: Tampilkan halaman Error 403 (Lebih Strict)
                // return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            if (data.Status != "Draft")
            {
                // OPSI A: Redirect kembali ke Index dengan pesan error (Lebih User Friendly)
                TempData["ErrorMessage"] = "Akses Ditolak: Anda hanya bisa mengedit data dengan status draft.";
                return RedirectToAction("DraftingRequest");

                // OPSI B: Tampilkan halaman Error 403 (Lebih Strict)
                // return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            var excludedPositions = new[] { "OPERATOR", "KOPERASI", "" };
            var ListUser = db.V_Users_Active
                .Where(w => !excludedPositions.Contains(w.PositionName))
                .ToList();

            // Ambil Reviewer yang SUDAH DIPILIH untuk dokumen ini
            var selectedReviewers = dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer
                                    .Where(r => r.DraftingReqID == ID)
                                    .Select(r => r.SendToNIK)
                                    .ToList(); 

            // Kirim ke View via ViewBag atau Model
            ViewBag.SelectedReviewers = selectedReviewers;
            ViewBag.Data = data;
            ViewBag.ListUser = ListUser;

            return View();

        }

        [HttpPost]
        public ActionResult UpdateDrafting(HC_LegalPermit_DraftAgreementRequest model)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            try
            {

                // Cari data lama di database
                var entity = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == model.ID);

                if (entity == null)
                {
                    return Json(new { success = false, message = "Data tidak ditemukan di database." });
                }

                // Update field satu per satu
                entity.RequestDate = model.RequestDate;
                entity.RefQuotationNo = model.RefQuotationNo;
                entity.SupplierName = model.SupplierName;
                entity.PresidentDirector = model.PresidentDirector;
                entity.Address = model.Address;
                entity.TelpNo = model.TelpNo;
                entity.EmailAddress = model.EmailAddress;
                entity.ProjectName = model.ProjectName;
                entity.ProjectDetails = model.ProjectDetails; // Update HTML TinyMCE

                entity.Price = model.Price;
                entity.Discount = model.Discount;
                entity.FinalPrice = model.FinalPrice;

                entity.TermOfPayment = model.TermOfPayment;
                entity.BankAccount = model.BankAccount;
                entity.LeadTimeWorks = model.LeadTimeWorks;
                entity.Warranty = model.Warranty;
                entity.PenaltyClause = model.PenaltyClause;
                entity.PlanStartDate = model.PlanStartDate;
                entity.PlanEndDate = model.PlanEndDate;

                entity.UpdatedAt = DateTime.Now;
                entity.UpdatedBy = currUser;

                entity.Status = model.Status;
                entity.CurrApprovalLevel = model.Status == "Submitted" ? 1 : 0;


                // 3. UPDATE REVIEWERS (Delete All -> Insert New)
                // A. Hapus Reviewer Lama
                var oldReviewers = dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer.Where(x => x.DraftingReqID == model.ID);
                dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer.RemoveRange(oldReviewers);

                // B. Insert Reviewer Baru (Jika ada yang dipilih)
                if (model.Reviewers != null && model.Reviewers.Count > 0)
                {
                    foreach (var nik in model.Reviewers)
                    {
                        var userInfo = db.V_Users_Active.FirstOrDefault(u => u.NIK == nik);
                        
                        var newMap = new HC_LegalPermit_DraftAgreementRequest_Reviewer
                        {
                            DraftingReqID = model.ID,
                            SendToNIK = nik,
                            SendToEmail = userInfo?.Email
                        };
                        dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer.Add(newMap);
                    }
                }

                if (model.Status == "Submitted")
                {
                    //SendEmail(entity.CreatedBy, entity, "Submitted", entity.ID);

                    try
                    {

                        GenerateApprovalFlow(entity.ID.ToString(), model.FinalPrice);

                    }
                    catch (Exception genEx)
                    {
                        if (genEx.InnerException != null)
                            Debug.WriteLine($"[ERROR] Inner: {genEx.InnerException.Message}");

                        // Opsional: Lempar error agar muncul di SweetAlert browser
                        throw new Exception("Flow Error: " + genEx.Message);
                    }
                }

                dblp.SaveChanges();

                return Json(new { success = true, message = "Data berhasil diperbarui!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult ProcessRequest(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);
            //if (data == null) return NotFound();

            // 1. Ambil Approval List (Struktur Flow)
            var rawFlow = (from a in db.Approval_List
                           where a.Reveral_ID == id.ToString() && a.Menu_Id == 1029
                           join u in db.V_Users_Active on a.User_NIK equals u.NIK into gj
                           from usr in gj.DefaultIfEmpty() // left join
                           orderby a.Levels
                           select new
                           {
                               a.id,
                               a.Reveral_ID,
                               a.Menu_Id,
                               a.User_NIK,
                               a.Title,
                               a.Dept_Name,
                               a.Levels,
                               // fallback to NIK when name not found
                               UserName = (usr != null && !string.IsNullOrEmpty(usr.Name)) ? usr.Name : a.User_NIK
                           }).ToList();

            // 2. Ambil History (Log Aktifitas)
            var historyLogs = db.Approval_History
                                  .Where(x => x.Reveral_ID == id.ToString())
                                  .ToList();

            // 3. GABUNGKAN DATA (Mapping ke ViewModel)
            var timelineList = new List<ApprovalTimelineViewModel>();


            foreach (var step in rawFlow)
            {
                // Cari log history terakhir milik User NIK ini
                // Kita pakai OrderByDescending ActionDate untuk ambil aksi terakhir (jika ada revisi berulang)
                var userLog = historyLogs.Where(h => h.Created_By_ID == step.User_NIK)
                                         .OrderByDescending(h => h.Created_At)
                                         .FirstOrDefault();

                timelineList.Add(new ApprovalTimelineViewModel
                {
                    Title = step.Title,
                    Dept_Name = step.Dept_Name,
                    User_Name = step.UserName,
                    Levels = step.Levels,
                    //Status = step.,     // Status saat ini (Pending/Approved/Rejected)
                    IsCurrent = (data.CurrApprovalLevel == step.Levels && data.Status.Contains("Waiting")), // Logic IsCurrent Manual

                    // Isi data dari History (Jika log ditemukan)
                    ActionDate = userLog?.Created_At,
                    Comments = userLog?.Note
                });
            }

            // Ubah string ini manual untuk mengetes tampilan Admin 1 atau 2
            var adminUsersLevel = dblp.HC_LegalPermit_UserAdmin.Where(x => x.UserNIK == currUser).FirstOrDefault();
            if (adminUsersLevel == null)
            {
                // Redirect kembali ke Index dengan pesan error (Lebih User Friendly)
                TempData["ErrorMessage"] = "Akses Ditolak: Anda tidak terdaftar sebagai admin aplikasi.";
                return RedirectToAction("DraftingRequest");

            }

            // GET Reviewers
            var mapReviewers = dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer
                                   .Where(map => map.DraftingReqID == id)
                                   .ToList();

            var listNik = mapReviewers.Select(x => x.SendToNIK).ToList();

            var userDetails = new List<V_Users_Active>();

            if (listNik.Count > 0)
            {
                userDetails = db.V_Users_Active
                                .Where(usr => listNik.Contains(usr.NIK))
                                .ToList();
            }

            var reviewersList = new List<dynamic>();

            var query = from map in mapReviewers
                        join usr in userDetails on map.SendToNIK equals usr.NIK
                        select new { usr.Name, usr.DeptName, usr.NIK };

            // Konversi ke ExpandoObject agar bisa dibaca di View
            foreach (var item in query)
            {
                dynamic reviewObj = new ExpandoObject();
                reviewObj.Name = item.Name;
                reviewObj.Department = item.DeptName;
                reviewObj.NIK = item.NIK;

                reviewersList.Add(reviewObj);
            }

            // 6. Logic IsMyTurn
            bool isMyTurn = false;
            var currentStepObj = rawFlow.FirstOrDefault(x => x.Levels == data.CurrApprovalLevel);
            if (currentStepObj != null)
            {
                if (currentStepObj.User_NIK == currUser) isMyTurn = true;
            }

            ViewBag.CurrentRole = adminUsersLevel.UserLevel;
            ViewBag.CurrentUser = currUser ?? "Current Admin";
            ViewBag.Data = data;
            ViewBag.Reviewers = reviewersList;
            ViewBag.CurrentLevel = data.CurrApprovalLevel;
            ViewBag.IsMyTurn = isMyTurn;
            ViewBag.ApprovalList = timelineList;
            //ViewBag.ApprovalList = approvalFlow;

            return View();
        }

        [HttpGet]
        [Authorize]
        public ActionResult ViewRequest(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            // Cek apakah NIK user login ada di tabel HC_LegalPermit_UserAdmin
            bool isAdmin = dblp.HC_LegalPermit_UserAdmin.Any(x => x.UserNIK == currUser);

            // Jika dia Admin, langsung lempar ke ProcessRequest
            if (isAdmin)
            {
                return RedirectToAction("ProcessRequest", new { id = id });
            }


            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);

            if (data.Status == "Submitted" || data.Status == "Review" || data.Status == "Returned")
            {
                TempData["ErrorMessage"] = "Anda hanya bisa melihat halaman ini ketika status sudah Approved / Dalam Proses Revisi";
                return RedirectToAction("DraftingRequest");
            }
            //if (data == null) return NotFound();

            // PROTEKSI LEVEL DATA: Cek apakah user login adalah Pembuat Data?
            bool isCreator = data.CreatedBy.Equals(currUser, StringComparison.OrdinalIgnoreCase);

            // 1. Ambil Approval List (Struktur Flow)
            var rawFlow = (from a in db.Approval_List
                           where a.Reveral_ID == id.ToString() && a.Menu_Id == 1029
                           join u in db.V_Users_Active on a.User_NIK equals u.NIK into gj
                           from usr in gj.DefaultIfEmpty() // left join
                           orderby a.Levels
                           select new
                           {
                               a.id,
                               a.Reveral_ID,
                               a.Menu_Id,
                               a.User_NIK,
                               a.Title,
                               a.Dept_Name,
                               a.Levels,
                               // fallback to NIK when name not found
                               UserName = (usr != null && !string.IsNullOrEmpty(usr.Name)) ? usr.Name : a.User_NIK
                           }).ToList();

            // 2. Ambil History (Log Aktifitas)
            var historyLogs = db.Approval_History
                                  .Where(x => x.Reveral_ID == id.ToString())
                                  .ToList();

            // 3. GABUNGKAN DATA (Mapping ke ViewModel)
            var timelineList = new List<ApprovalTimelineViewModel>();


            foreach (var step in rawFlow)
            {
                // Cari log history terakhir milik User NIK ini
                // Kita pakai OrderByDescending ActionDate untuk ambil aksi terakhir (jika ada revisi berulang)
                var userLog = historyLogs.Where(h => h.Created_By_ID == step.User_NIK)
                                         .OrderByDescending(h => h.Created_At)
                                         .FirstOrDefault();

                timelineList.Add(new ApprovalTimelineViewModel
                {
                    Title = step.Title,
                    Dept_Name = step.Dept_Name,
                    User_Name = step.UserName,
                    Levels = step.Levels,
                    //Status = step.,     // Status saat ini (Pending/Approved/Rejected)
                    IsCurrent = (data.CurrApprovalLevel == step.Levels && data.Status.Contains("Waiting")), // Logic IsCurrent Manual

                    // Isi data dari History (Jika log ditemukan)
                    ActionDate = userLog?.Created_At,
                    Comments = userLog?.Note
                });
            }

            // New: cek apakah user adalah reviewer untuk drafting ini
            bool isReviewer = dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer
                                   .Any(r => r.DraftingReqID == id && r.SendToNIK == currUser);

            // Hanya deny akses jika bukan creator dan bukan reviewer
            if (!isCreator && !isReviewer)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke dokumen ini.";
                return RedirectToAction("DraftingRequest");
            }

            ViewBag.Data = data;
            ViewBag.ApprovalList = timelineList;
            ViewBag.CurrentLevel = data.CurrApprovalLevel;

            return View();

        }

        [HttpPost]
        public ActionResult UploadAgreement(int id, HttpPostedFileBase file) // Ganti IFormFile jadi HttpPostedFileBase
        {
            // Cek NULL dan Extension
            if (file == null || file.ContentLength == 0 || Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                return Json(new { success = false, message = "Wajib upload file PDF." });
            }

            try
            {

                var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);

                // Setup Folder
                string folderRelative = "~/Files/HC/LegalPermit/Drafting/";

                // 2. Tentukan Path Fisik (Untuk Save File)
                string serverFolder = Server.MapPath(folderRelative);

                if (!Directory.Exists(serverFolder)) Directory.CreateDirectory(serverFolder);

                // 3. Simpan File
                string fileName = id + "_Agreement_" + DateTime.Now.Ticks + ".pdf";
                string fullSavePath = Path.Combine(serverFolder, fileName);

                file.SaveAs(fullSavePath);

                data.AgreementFilePath = fileName;
                dblp.SaveChanges();

                string publicUrl = Url.Content(folderRelative + fileName);

                return Json(new { success = true, message = "File berhasil diupload.", path = publicUrl });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SendToReview(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);
            var currLevelApproval = data.CurrApprovalLevel;

            if (string.IsNullOrEmpty(data.AgreementFilePath))
                return Json(new { success = false, message = "Harap upload file terlebih dahulu." });

            data.Status = "Review";

            var nextStep = db.Approval_List
                                     .Where(x => x.Menu_Id == 1029 && x.Reveral_ID == id.ToString()
                                              && x.Levels > data.CurrApprovalLevel)
                                     .OrderBy(x => x.Levels)
                                     .FirstOrDefault();

            if (nextStep != null)
            {
                // --- MASIH ADA PROSES LANJUTAN ---
                // Update Level di Header ke Level si Next Step
                data.CurrApprovalLevel = nextStep.Levels;
                data.Status = "Waiting Approval - " + nextStep.Title;
            }
            else
            {
                // --- FINISH (Tidak ada level lebih tinggi) ---
                // doc.CurrentLevel biarkan di level terakhir atau set angka tinggi (misal 999)
                data.Status = "Approved";
            }

            dblp.SaveChanges();

            // get approval list data
            var approvalList = db.Approval_List.Where(w => w.Menu_Id == 1029 && w.Levels == currLevelApproval).FirstOrDefault();

            //insert to approval history
            var entity = new Approval_History
            {
                Menu_Id = 1029,
                Menu_Name = "Drafting Agreement",
                Document_Id = 1,
                Document_Name = null,
                Reveral_ID = id.ToString(),
                Reveral_ID_Sub = null,
                Title = approvalList.Title,
                Header = null,
                Label = null,
                Note = null,
                Approval = 2,
                Approval_Sub = 0,
                IsRevise = false,
                Status = "Drafting Uploaded",
                Created_At = DateTime.Now,
                Created_By_ID = currUser,
                Created_By_Name = CurrUser.Name

            };

            db.Approval_History.Add(entity);
            db.SaveChanges();

            // --- KIRIM EMAIL ---
            //Task.Run(() => {

            SendEmail(data.CreatedBy, data, "Review", data.ID);
            //});

            return Json(new { success = true });

        }

        [HttpPost]
        public ActionResult SubmitDraftingApproval(int draftingId, string action, string comment)
        {
            try
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
                var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

                // 1. Ambil Header Dokumen
                var doc = dblp.HC_LegalPermit_DraftAgreementRequest.Find(draftingId);
                if (doc == null) return Json(new { success = false, message = "Dokumen tidak ditemukan." });

                int currApprovalLevel = doc.CurrApprovalLevel;

                // 2. Ambil Step di List yang sesuai dengan Level Dokumen saat ini
                var currentListStep = db.Approval_List
                                        .FirstOrDefault(x => x.Reveral_ID == draftingId.ToString()
                                                          && x.Levels == doc.CurrApprovalLevel && x.Menu_Id == 1029);

                if (currentListStep == null)
                    return Json(new { success = false, message = "Data step approval tidak ditemukan." });

                // Validasi Safety: Pastikan yang approve adalah orang yang benar (Double check server side)
                if (currentListStep.User_NIK != currUser)
                    return Json(new { success = false, message = $"Anda tidak memiliki akses untuk approval ini.{currentListStep.User_NIK}" });


                if (action == "Approve")
                {
                    // A. Update Status di Tabel List (Transaksi perorangan)
                    //currentListStep.Status = "Approved";
                    //currentListStep.ApprovalDate = DateTime.Now;
                    //currentListStep.Comments = comment;

                    // B. Cek Next Step (Level Berikutnya)
                    
                    var nextStep = db.Approval_List
                                     .Where(x => x.Menu_Id == 1029 && x.Reveral_ID == draftingId.ToString()
                                              && x.Levels > doc.CurrApprovalLevel)
                                     .OrderBy(x => x.Levels)
                                     .FirstOrDefault();

                    if (nextStep != null)
                    {
                        // --- MASIH ADA PROSES LANJUTAN ---
                        // Update Level di Header ke Level si Next Step
                        doc.CurrApprovalLevel = nextStep.Levels;
                        doc.Status = "Waiting Approval - " + nextStep.Title;
                    }
                    else
                    {
                        // --- FINISH (Tidak ada level lebih tinggi) ---
                        // doc.CurrentLevel biarkan di level terakhir atau set angka tinggi (misal 999)
                        doc.Status = "Approved";
                    }

                    // Log History
                    LogHistory(doc.ID.ToString(), currUser, CurrUser.Name, "Approved", currApprovalLevel, 0, comment);
                    SendEmail(doc.CreatedBy, doc, "Approved", doc.ID);
                }
                else if (action == "Reject")
                {
                    // Update List Step ini jadi Rejected
                    //currentListStep.Status = "Rejected";
                    //currentListStep.ApprovalDate = DateTime.Now;
                    //currentListStep.Comments = comment;

                    // Update Header (Kembali ke Creator)
                    // Opsional: CurrentLevel mau direset ke 0 atau tetap di level ini? 
                    // Biasanya tetap di level ini agar creator tau nyangkut dimana, atau reset ke 0 jika harus ulang dari awal.
                    // Disini saya biarkan levelnya, tapi statusnya Returned.
                    doc.Status = "Returned";
                    doc.CurrApprovalLevel = 1;
                    doc.ReturnToLevel = currApprovalLevel;

                    LogHistory(doc.ID.ToString(), currUser, CurrUser.Name, "Rejected", currApprovalLevel, 0, comment);
                }

                // Simpan Perubahan di KEDUA Context (Header & List)
                dblp.SaveChanges(); // Simpan Header
                db.SaveChanges();   // Simpan List Status

                return Json(new { success = true, message = "Proses berhasil disimpan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // 3. Admin 2: Approve
        [HttpPost]
        public ActionResult ApproveRequest(int id)
        {
            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);
            data.Status = "Approved";
            dblp.SaveChanges();

            SendEmail(data.CreatedBy, data, data.Status, data.ID);

            return Json(new { success = true });

        }

        public ActionResult RejectRequest(int id)
        {

            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);
            data.Status = "Returned"; // Balik ke Admin 1
            dblp.SaveChanges();
            return Json(new { success = true });

        }

        [HttpPost]
        public ActionResult UserApproveAgreement(int id)
        {

            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);

            if (data == null)
                return Json(new { success = false, message = "Data tidak ditemukan." });

            // Validasi: Hanya bisa diapprove jika statusnya 'Approved' (dari Admin)
            if (data.Status == "Approved")
            {
                data.Status = "Completed"; // Status Final
                dblp.SaveChanges();

                SendEmail(data.CreatedBy, data, "Completed", data.ID);

                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Dokumen belum disetujui Admin atau sudah selesai." });
            }

        }
        [Authorize]
        [HttpPost]
        public ActionResult RequestRevision(int id, string reason)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);

            if (data == null)
                return Json(new { success = false, message = "Data tidak ditemukan." });

            // Validasi: Hanya bisa revisi jika status 'Approved' (sebelum difinalisasi)
            if (data.Status == "Approved")
            {
                data.Status = "Revision"; // Kembalikan status ke Returned agar bisa diupload ulang oleh Admin L1

                // Opsi: Simpan alasan revisi ke tabel komentar secara otomatis
                // (Contoh logic, sesuaikan dengan struktur tabel comment Anda)

                var comment = new HC_LegalPermit_DraftAgreementRequest_Comment
                {
                    DraftingRequestId = id,
                    Content = "REVISION REQUESTED: " + reason,
                    CreatedBy = CurrUser.Name,
                    FullName = CurrUser.Name,
                    CreatedTime = DateTime.Now,
                    Source = "Drafting"
                };
                dblp.HC_LegalPermit_DraftAgreementRequest_Comment.Add(comment);


                dblp.SaveChanges();
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Status dokumen tidak valid untuk revisi." });
            }

        }

        [HttpPost]
        public ActionResult SubmitRevision(int id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);
            if (data == null) return Json(new { success = false, message = "Data not found" });
            var currStatus = data.Status;

            var currentListStep = db.Approval_List
                                    .FirstOrDefault(x => x.Menu_Id == 1029 && x.Reveral_ID == id.ToString()
                                                      && x.Levels == data.CurrApprovalLevel);
            if (currentListStep.User_NIK != currUser) return Json(new { success = false, message = "Akses ditolak." });
            int currApprovalLevel = data.CurrApprovalLevel;

            LogHistory(data.ID.ToString(), currUser, CurrUser.Name, "Revised", currApprovalLevel, 0, "");

            // Validasi: Hanya bisa submit revisi jika statusnya memang diminta revisi
            if (currStatus == "Revision")
            {

                data.Status = "Approved";
                data.CurrApprovalLevel = data.ReturnToLevel; // Kembalikan ke level sebelum revisi
                data.ReturnToLevel = 0; // Reset ReturnToLevel setelah digunakan

                dblp.SaveChanges();

                SendEmail(data.CreatedBy, data, "Approved", data.ID);

                return Json(new { success = true });
            }
            else if (currStatus == "Returned")
            {
                data.Status = "Review";
                data.CurrApprovalLevel = data.ReturnToLevel;

                

                SendEmail(data.CreatedBy, data, "Review", id);

                dblp.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Status invalid for revision submission." });

        }

        [HttpGet]
        public ActionResult DownloadWordDraft(int id, string templateType)
        {
            try
            {
                // 1. Ambil Data dari Database
                var data = dblp.HC_LegalPermit_DraftAgreementRequest.FirstOrDefault(x => x.ID == id);
                if (data == null) return Content("Data tidak ditemukan di database.");

                // 2. Load Template
                string templatePath = "";

                if (templateType == "customer")
                {
                    templatePath = Server.MapPath("~/Files/HC/LegalPermit/DraftingTemplate/Template_Agreement_Customer.docx");
                } else if (templateType == "purchase")
                {
                    templatePath = Server.MapPath("~/Files/HC/LegalPermit/DraftingTemplate/Template_Agreement_Purchase.docx");
                } else if (templateType == "service")
                {
                    templatePath = Server.MapPath("~/Files/HC/LegalPermit/DraftingTemplate/Template_Agreement_Service.docx");
                } else if (templateType == "distributor")
                {
                    templatePath = Server.MapPath("~/Files/HC/LegalPermit/DraftingTemplate/Template_Agreement_Distributor.docx");
                } else if (templateType == "project")
                {
                    templatePath = Server.MapPath("~/Files/HC/LegalPermit/DraftingTemplate/Template_Agreement_Project.docx");
                }
                else
                {
                    return Content("Tipe template tidak valid.");
                }
                //string templatePath = Server.MapPath("~/Files/HC/LegalPermit/DraftingTemplate/Template_Agreement.docx");
                if (!System.IO.File.Exists(templatePath))
                    return Content("File Template tidak ditemukan di folder App_Data.");

                // 3. Proses Manipulasi File di Memory
                using (var mem = new MemoryStream())
                {
                    // Copy template fisik ke MemoryStream agar file asli tidak terkunci
                    using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(mem);
                    }

                    // 4. BUKA DOKUMEN MENGGUNAKAN OPENXML
                    using (var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(mem, true))
                    {
                        // === TEKNIK BRUTE FORCE XML REPLACE ===
                        // Kita baca seluruh isi dokumen (body) sebagai 1 string XML panjang
                        string docText = null;
                        using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
                        {
                            docText = sr.ReadToEnd();
                        }

                        // Lakukan Replace pada String XML tersebut.
                        // Ini akan mengabaikan tag pemisah Word (<w:t>, <w:r>, dll)

                        // --- DATA MAPPING ---
                        docText = docText.Replace("{{RefQuotationNo}}", EscapeXml(data.RefQuotationNo));
                        docText = docText.Replace("{{SupplierName}}", EscapeXml(data.SupplierName));
                        docText = docText.Replace("{{Address}}", EscapeXml(data.Address));
                        docText = docText.Replace("{{ProjectName}}", EscapeXml(data.ProjectName));
                        docText = docText.Replace("{{PresidentDirector}}", EscapeXml(data.PresidentDirector));

                        // FinalPrice -> formatted currency + Indonesian text + English text
                        decimal finalPrice = data.FinalPrice;
                        // Currency formatted as "Rp. 1.000.000"
                        string finalPriceCurrency = "Rp. " + finalPrice.ToString("N0", new CultureInfo("id-ID"));
                        // Text versions
                        string finalPriceTextId = ToIndonesianWords(finalPrice);
                        string finalPriceTextEn = ToEnglishWords(finalPrice);

                        docText = docText.Replace("{{FinalPriceCurrency}}", EscapeXml(finalPriceCurrency));
                        docText = docText.Replace("{{FinalPriceInWords_ID}}", EscapeXml(finalPriceTextId));
                        docText = docText.Replace("{{FinalPriceInWords_EN}}", EscapeXml(finalPriceTextEn));

                        // Format Tanggal (dd MMMM yyyy = 20 Januari 2026)
                        // Pastikan handle null value agar tidak error
                        string reqDate = data.RequestDate.HasValue ? data.RequestDate.Value.ToString("dd MMMM yyyy") : "-";

                        docText = docText.Replace("{{RequestDate}}", EscapeXml(reqDate));

                        string startDate = data.PlanStartDate.HasValue ? data.PlanStartDate.Value.ToString("dd MMMM yyyy") : "-";
                        docText = docText.Replace("{{PlanStartDate}}", EscapeXml(startDate));
                        string endDate = data.PlanEndDate.HasValue ? data.PlanEndDate.Value.ToString("dd MMMM yyyy") : "-";

                        docText = docText.Replace("{{PlanEndDate}}", EscapeXml(endDate));
                        docText = docText.Replace("{{Warranty}}", EscapeXml(data.Warranty));
                        docText = docText.Replace("{{EmailAddress}}", EscapeXml(data.EmailAddress));

                        // 5. SIMPAN KEMBALI XML KE DOKUMEN
                        // Kita timpa isi XML lama dengan XML baru yang sudah di-replace
                        using (StreamWriter sw = new StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create)))
                        {
                            sw.Write(docText);
                        }

                        // Save Changes ke struktur OpenXML
                        wordDoc.MainDocumentPart.Document.Save();
                    }

                    // 6. Return File ke Browser
                    mem.Position = 0; // Reset posisi stream ke awal
                    string downloadName = $"Draft_{data.ProjectName}_{DateTime.Now:yyyyMMdd}.docx";

                    return File(mem.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", downloadName);
                }
            }
            catch (Exception ex)
            {
                // Tampilkan error jelas jika terjadi crash
                return Content("Terjadi Error: " + ex.Message + " | Stack: " + ex.StackTrace);
            }
        }

        [HttpGet]
        public ActionResult GetComments(int agreementId, string source)
        {
            var comments = dblp.HC_LegalPermit_DraftAgreementRequest_Comment
                .Where(x => x.DraftingRequestId == agreementId && x.Source == source)
                .Include(x => x.Attachments)
                .OrderBy(x => x.CreatedTime)
                .ToList();

            var result = comments.Select(c => new {
                id = c.ID,
                parent = c.ParentId,
                created = c.CreatedTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                content = c.Content,
                fullname = c.CreatedBy,
                profile_picture_url = "https://viima-app.s3.amazonaws.com/media/public/defaults/user-icon.png",
                created_by_current_user = (c.CreatedBy == User.Identity.Name),

                // Mapping Attachment DB -> JSON Viima
                attachments = c.Attachments.Select(a => new {
                    id = a.ID,
                    url = a.FileUrl,
                    name = a.FileName,
                    mime_type = a.MimeType
                }).ToList()
            });

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult PostComment(int agreementId, CommentViewModel commentJson, string source)
        {
            try
            {
                //// 1. Validasi
                //bool hasContent = !string.IsNullOrEmpty(commentJson.content);
                //// Cek apakah attachments terisi (tidak null dan count > 0)
                //bool hasAttachment = commentJson.attachments != null && commentJson.attachments.Count > 0;

                //if (!hasContent && !hasAttachment)
                //{
                //    return Json(new { success = false, message = "Konten atau file kosong." });
                //}

                // 2. Simpan Header Komentar
                var newComment = new HC_LegalPermit_DraftAgreementRequest_Comment
                {
                    DraftingRequestId = agreementId,
                    ParentId = commentJson.parent,
                    Content = commentJson.content ?? "Attachment Sent", // Isi default jika teks kosong
                    Source = source,
                    CreatedBy = User.Identity.Name,
                    FullName = User.Identity.Name,
                    CreatedTime = DateTime.Now,
                    CreatedByAdmin = User.IsInRole("AdminLevel1") || User.IsInRole("AdminLevel2"),
                    UpvoteCount = 0
                };

                dblp.HC_LegalPermit_DraftAgreementRequest_Comment.Add(newComment);
                dblp.SaveChanges(); // ID terbentuk disini

                // 3. Simpan Attachment ke Database
                //if (hasAttachment)
                //{
                if (commentJson.attachments != null && commentJson.attachments.Count > 0)
                {
                    foreach (var item in commentJson.attachments)
                    {
                        // DEBUG: Pastikan URL tidak null
                        if (!string.IsNullOrEmpty(item.url))
                        {
                            var newAttachment = new HC_LegalPermit_DraftAgreementRequest_Comment_Attachment
                            {
                                CommentId = newComment.ID,
                                FileUrl = item.url,       // Ambil dari ViewModel (huruf kecil)
                                FileName = item.name,     // Ambil dari ViewModel (huruf kecil)
                                MimeType = item.mime_type
                            };
                            dblp.HC_LegalPermit_DraftAgreementRequest_Comment_Attachment.Add(newAttachment);
                        }
                    }
                    dblp.SaveChanges();
                }

                SendRealtimeNotification(agreementId, source, newComment.Content, User.Identity.Name);
                // 4. Return JSON
                return Json(new
                {
                    id = newComment.ID,
                    parent = newComment.ParentId,
                    content = newComment.Content,
                    created = newComment.CreatedTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    fullname = newComment.FullName,
                    profile_picture_url = "https://viima-app.s3.amazonaws.com/media/public/defaults/user-icon.png",
                    created_by_current_user = true,
                    upvote_count = 0,
                    user_has_upvoted = false,
                    // Kembalikan data attachment agar muncul di layar
                    attachments = commentJson.attachments ?? new List<AttachmentViewModel>()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UploadCommentAttachment(HttpPostedFileBase file)
        {
            try
            {
                // 1. Validasi: Di MVC 5 gunakan 'ContentLength', bukan 'Length'
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new { success = false, message = "File kosong atau tidak terbaca." });
                }

                // 2. Tentukan Folder Penyimpanan (Server Path)
                // Kita simpan di folder khusus 'Comments' agar rapi
                string relativePath = "~/Files/HC/LegalPermit/DraftingComment/";
                string serverPath = Server.MapPath(relativePath);

                // Buat folder jika belum ada
                if (!Directory.Exists(serverPath))
                {
                    Directory.CreateDirectory(serverPath);
                }

                // 3. Generate Nama Unik
                // Path.GetFileName penting untuk keamanan (menghapus path client jika ada)
                string uniqueName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string savePath = Path.Combine(serverPath, uniqueName);

                // 4. Simpan ke Server (MVC 5 menggunakan SaveAs)
                file.SaveAs(savePath);

                // 5. Generate URL Publik (Web Path)
                // PENTING: Jangan kirim 'serverPath' (C:\...) ke browser!
                // Gunakan Url.Content untuk mengubah '~/' menjadi URL relatif ('/Files/...')
                string publicUrl = Url.Content(relativePath + uniqueName);

                return Json(new
                {
                    success = true,
                    url = publicUrl,         // Hasil: /Files/HC/LegalPermit/Comments/uuid_namafile.jpg
                    name = file.FileName,    // Nama Asli untuk ditampilkan
                    mime_type = file.ContentType
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error Upload: " + ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult TriggerDailyReminder(string token)
        {
            // Cek Secret Token
            string expectedToken = "Niterra-Reminder-Secret-2026-XyZ!";
            if (token != expectedToken)
            {
                return new HttpStatusCodeResult(403, "Forbidden: Invalid Token");
            }

            try
            {
                // 2. Logic Batas Waktu H-4
                DateTime targetDate = DateTime.Today.AddDays(-4);
                DateTime today = DateTime.Today;

                // 3. Query Dokumen (Belum selesai & Umur >= 4 Hari)
                var stuckDocs = dblp.HC_LegalPermit_DraftAgreementRequest
                                    .Where(x => x.CreatedAt <= targetDate
                                             && x.Status.Contains("Waiting"))
                                    .ToList();
                var stuckDocCount = stuckDocs.Count;

                int emailSentCount = 0;

                // --- KITA BUAT LOG STRING UNTUK TAMPIL DI LAYAR ---
                string debugLog = $"<h3>Laporan Reminder Harian</h3>";
                debugLog += $"<b>Menemukan {stuckDocs.Count} dokumen yang berumur >= 4 hari.</b><br/><hr/>";


                foreach (var doc in stuckDocs)
                {
                    debugLog += $"<b>Memproses Dokumen ID: {doc.ID} ({doc.ProjectName})</b><br/>";
                    // 4. Cegah Spam (Sudah dikirim hari ini?)
                    if (doc.LastReminderDate != null && doc.LastReminderDate.Value.Date == today)
                    {
                        debugLog += $"- <span style='color:orange'>SKIPPED:</span> Hari ini sudah dikirim reminder pada {doc.LastReminderDate}.<br/><br/>";
                        continue; // Skip
                    }

                    // 5. Cari pemegang bola (Approver saat ini)
                    var currentStep = db.Approval_List
                                          .FirstOrDefault(x => x.Reveral_ID == doc.ID.ToString() && x.Menu_Id == 1029
                                                            && x.Levels == doc.CurrApprovalLevel);

                    if (currentStep == null)
                    {
                        debugLog += $"- <span style='color:red'>SKIPPED (ERROR):</span> Data step approval tidak ditemukan untuk Level {doc.CurrApprovalLevel}.<br/><br/>";
                        continue;
                    }

                    if (string.IsNullOrEmpty(currentStep.User_NIK))
                    {
                        debugLog += $"- <span style='color:red'>SKIPPED (ERROR):</span> User_NIK kosong di tabel Approval_List.<br/><br/>";
                        continue;
                    }

                    if (currentStep != null && !string.IsNullOrEmpty(currentStep.User_NIK))
                    {
                        var user = db.V_Users_Active.FirstOrDefault(u => u.NIK == currentStep.User_NIK);
                        if (user == null)
                        {
                            debugLog += $"- <span style='color:red'>SKIPPED (ERROR):</span> User dengan NIK {currentStep.User_NIK} tidak ditemukan di V_Users_Active.<br/><br/>";
                            continue;
                        }

                        if (string.IsNullOrEmpty(user.Email))
                        {
                            debugLog += $"- <span style='color:red'>SKIPPED (ERROR):</span> User {user.Name} tidak memiliki alamat Email di database.<br/><br/>";
                            continue;
                        }
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            debugLog += $"- Mencoba mengirim email ke {user.Email}... ";
                            string smtpError = "";
                            // 6. Kirim Email (Panggil fungsi kirim email Anda)
                            bool isSent = SendEmailReminder("ikhsan.sholihin@ngkbusi.com", user.Name, doc.ProjectName, doc.ID, out smtpError);

                            if (isSent)
                            {
                                doc.LastReminderDate = DateTime.Now;
                                emailSentCount++;
                                debugLog += $"<span style='color:green'><b>SUKSES TERKIRIM!</b></span><br/><br/>";
                            } else
                            {
                                debugLog += $"<span style='color:red'><b>GAGAL KIRIM (SMTP ERROR)</b></span>. Cek konfigurasi email/port.<br/><br/>";
                                debugLog += $"<span style='color:red'><i>Detail Error: {smtpError}</i></span><br/><br/>";
                            }
                        }
                    }
                }

                dblp.SaveChanges();

                debugLog += $"<hr/><h4>Kesimpulan: {emailSentCount} reminder berhasil dikirim.</h4>";

                // Return text sederhana agar kita tahu hasilnya saat di-test
                //return Content($"SUCCESS: {emailSentCount} reminder emails sent. Count {stuckDocCount} item need to be remind");
                return Content(debugLog , "text/html");
            }
            catch (Exception ex)
            {
                // Jangan tampilkan detail error ke publik, cukup log saja
                System.Diagnostics.Debug.WriteLine("Reminder Error: " + ex.Message);
                //return new HttpStatusCodeResult(500, "Internal Server Error");
                return Content($"<h3>CRITICAL ERROR:</h3><p>{ex.Message}</p>", "text/html");
            }
        }

        // Fungsi SendEmailReminder tetap sama seperti contoh sebelumnya
        private bool SendEmailReminder(string toEmail, string toName, string projectName, int docId, out string errorMessage)
        {
            errorMessage = "";

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Legal App Notification");
            var password = "100%NGKbusi!";


            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, password)
            };

            try
            {
                using (var mess = new MailMessage())
                {
                    mess.From = senderEmail;
                    mess.Subject = $"[URGENT] Reminder: Approval Dokumen {projectName}";
                    mess.IsBodyHtml = true;
                    mess.Body = $@"
                                        <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                                            <h3 style='color: #d9534f;'>Peringatan Batas Waktu Approval</h3>
                                            <p>Halo <b>{toName}</b>,</p>
                                            <p>Terdapat dokumen yang <b>telah melewati batas SLA (lebih dari 4 hari)</b> dan saat ini sedang menunggu aksi Anda.</p>
                                            <p><b>Nama Project:</b> {projectName}</p>
                                            <br/>
                                            <a href='https://portal.ngkbusi.com/NGKBusi/HC/LegalPermit/ProcessRequest/{docId}' 
                                               style='background-color: #2b5797; color: white; padding: 10px 20px; text-decoration: none; border-radius: 3px; display: inline-block;'>
                                               Klik Disini Untuk Memproses
                                            </a>
                                            <br/><br/>
                                            <p style='font-size: 12px; color: #777;'>Email ini dikirim secara otomatis oleh sistem karena dokumen belum diproses. Reminder ini akan dikirim setiap hari hingga dokumen diselesaikan.</p>
                                        </div>";
                    mess.To.Add(new MailAddress("ikhsan.sholihin@ngkbusi.com"));

                    //// Jangan ubah variable 'baseTemplate' asli, tapi buat string baru 'finalBody'
                    //string finalBody = baseTemplate.Replace("{{RECEIVER_NAME}}", user.Name ?? "User");

                    //mess.Body = finalBody;

                    // Kirim Email Individual
                    smtp.Send(mess);
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner: " + ex.InnerException.Message;
                }
                return false;
            }

        }

        public void SendEmail(string toName, dynamic dataRequest, string actionType, int ID)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            var listNikReviewer = dblp.HC_LegalPermit_DraftAgreementRequest_Reviewer
                        .Where(r => r.DraftingReqID == ID)
                        .Select(x => x.SendToNIK)
                        .ToList();

            string FilePath = Path.Combine(Server.MapPath("~/Emails/HC/LegalPermit/"), "Info.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();

            string subject = "";
            string headerMsg = "";

            string domain = Request.Url.Scheme + "://" + Request.Url.Authority;
            string linkUrl = "";

            List<V_Users_Active> userEmailTo = new List<V_Users_Active>();

            if (actionType == "Submitted")
            {
                subject = "[New Request] Agreement - " + dataRequest.ProjectName;
                headerMsg = "New Drafting Request has been created";
                // FIX: Use local variable to avoid dynamic in expression tree
                string createdByNIK = dataRequest.CreatedBy;
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                        .Where(x => x.UserLevel == 1)
                       .Select(x => x.UserNIK)
                       .ToList();
                
                // Include both the request creator and all level-2 admins as recipients
                userEmailTo = db.V_Users_Active
                                .Where(x => x.NIK == createdByNIK || listNikAdmin.Contains(x.NIK) || listNikReviewer.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ViewRequest", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            else if (actionType == "Review" )
            {
                int currApprovalLevel = 2;
                try
                {
                    // dataRequest is dynamic; copy value to a local int to avoid expression-tree/dynamic issues
                    if (dataRequest != null)
                        currApprovalLevel = Convert.ToInt32(dataRequest.CurrApprovalLevel);
                }
                catch
                {
                    currApprovalLevel = 2; // fallback
                }

                subject = "[ACTION] Review Agreement - " + dataRequest.ProjectName;
                headerMsg = "Mohon review request agreement berikut.";
                var listNikAdmin = db.Approval_List
                        .Where(x => x.Menu_Id == 1029 && x.Levels == currApprovalLevel)
                        .Select(x => x.User_NIK)
                        .Distinct()
                        .Where(nik => !string.IsNullOrEmpty(nik))
                        .ToList();

                userEmailTo = db.V_Users_Active
                                    .Where(x => listNikAdmin.Contains(x.NIK))
                                    .ToList();
                linkUrl = domain + Url.Action("ViewRequest", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            else if (actionType == "Approved")
            {
                subject = "[APPROVED] Agreement - " + dataRequest.ProjectName;
                headerMsg = "Request telah disetujui.";

                int currApprovalLevel = Convert.ToInt32(dataRequest.CurrApprovalLevel);
                // FIX: Use local variable to avoid dynamic in expression tree
                string createdByNIK = dataRequest.CreatedBy;
                var listNikAdmin = db.Approval_List
                        .Where(x => x.Menu_Id == 1029 && x.Levels == currApprovalLevel)
                        .Select(x => x.User_NIK)
                        .Distinct()
                        .Where(nik => !string.IsNullOrEmpty(nik))
                        .ToList();                

                // Include both the request creator and all level-2 admins as recipients
                userEmailTo = db.V_Users_Active
                                .Where(x => listNikAdmin.Contains(x.NIK) || listNikReviewer.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ViewRequest", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            else if (actionType == "Completed")
            {
                subject = "[Completed] Agreement - " + dataRequest.ProjectName;
                headerMsg = "Drafting Request telah complete.";
                // FIX: Use local variable to avoid dynamic in expression tree
                string createdByNIK = dataRequest.CreatedBy;
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                       .Where(x => x.UserLevel == 1)
                       .Select(x => x.UserNIK)
                       .ToList();
                // Include both listNikAdmin
                userEmailTo = db.V_Users_Active
                                .Where(x => x.NIK == createdByNIK || listNikAdmin.Contains(x.NIK) || listNikReviewer.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ViewRequest", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            else if (actionType == "Returned")
            {
                subject = "[REVISION] Agreement - " + dataRequest.ProjectName;
                headerMsg = "Request dikembalikan untuk revisi.";
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                       .Where(x => x.UserLevel == 1)
                       .Select(x => x.UserNIK)
                       .ToList();
                userEmailTo = db.V_Users_Active
                                .Where(x => listNikAdmin.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ProcessRequest", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            else if (actionType == "Revision")
            {
                subject = "[REVISION] Agreement - " + dataRequest.ProjectName;
                headerMsg = "Request dikembalikan untuk revisi.";
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                       .Where(x => x.UserLevel == 1)
                       .Select(x => x.UserNIK)
                       .ToList();
                userEmailTo = db.V_Users_Active
                                .Where(x => listNikAdmin.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ProcessRequest", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }


            MailText = MailText.Replace("{{MESSAGE_HEADER}}", headerMsg);
            MailText = MailText.Replace("{{PROJECT_NAME}}", dataRequest.ProjectName);
            MailText = MailText.Replace("{{SUPPLIER_NAME}}", dataRequest.SupplierName);
            MailText = MailText.Replace("{{REQUEST_DATE}}", dataRequest.CreatedAt.ToString("dd MMM yyyy"));
            MailText = MailText.Replace("{{STATUS_RAW}}", actionType);
            MailText = MailText.Replace("{{STATUS_DISPLAY}}", actionType.ToUpper());
            MailText = MailText.Replace("{{LINK_URL}}", linkUrl);

            // Simpan template dasar yang sudah terisi data umum
            string baseTemplate = MailText;

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Legal App Notification");
            var password = "100%NGKbusi!";

            //var sub = "Legal Apps Notification";
            //var body = MailText;

            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, password)
            };
            // --- 2. LOOPING PENGIRIMAN EMAIL ---
            // kirim satu per satu agar Nama Penerima (RECEIVER_NAME) bisa berubah sesuai user
            foreach (var user in userEmailTo)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        using (var mess = new MailMessage())
                        {
                            mess.From = senderEmail;
                            mess.Subject = subject;
                            mess.IsBodyHtml = true;
                            mess.To.Add(new MailAddress(user.Email));

                            // Jangan ubah variable 'baseTemplate' asli, tapi buat string baru 'finalBody'
                            string finalBody = baseTemplate.Replace("{{RECEIVER_NAME}}", user.Name ?? "User");

                            mess.Body = finalBody;

                            // Kirim Email Individual
                            smtp.Send(mess);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error jika pengiriman ke satu user gagal, agar loop ke user lain tetap jalan
                        System.Diagnostics.Debug.WriteLine($"Gagal kirim ke {user.Email}: {ex.Message}");
                    }
                }
            }
        }

        /* ----- CHAPTER 3 REQUEST REVIEW AGREEMENT ----- */
        private int GetUserLevel(string nik)
        {
            var admin = dblp.HC_LegalPermit_UserAdmin.FirstOrDefault(x => x.UserNIK == nik);
            return admin != null ? admin.UserLevel : 0;
        }
        public ActionResult ReviewAgreementRequest()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            int userLevel = GetUserLevel(currUser);
            ViewBag.UserLevel = userLevel;

            // Query requests from dblp and materialize to avoid cross-context join
            IQueryable<HC_LegalPermit_ReviewAgreementRequest> query = dblp.HC_LegalPermit_ReviewAgreementRequest;
            if (userLevel == 0)
            {
                query = query.Where(x => x.CreateBy == currUser);
            }

            var requests = query.OrderByDescending(x => x.CreateTime).ToList();

            // Get only needed user records from other context
            var nikList = requests
                .Select(r => r.CreateBy)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();

            var users = new List<V_Users_Active>();
            if (nikList.Count > 0)
            {
                users = db.V_Users_Active
                          .Where(u => nikList.Contains(u.NIK))
                          .ToList();
            }

            // Flatten into DTO so view can read properties directly
            var dtoList = requests.Select(r =>
            {
                var u = users.FirstOrDefault(x => x.NIK == r.CreateBy);
                return new Tbl_LegalPermit_ReviewAgreementRequest
                {
                    ID = r.ID,
                    ProjectName = r.ProjectName,
                    CompanyName = r.CompanyName,
                    ReviewDocument = r.ReviewDocument,
                    OriginalReviewDocumentName = r.OriginalReviewDocumentName,
                    QuotationDocumentName = r.QuotationDocumentName,
                    SignerInfoDocumentName = r.SignerInfoDocumentName,
                    CreateTime = r.CreateTime,
                    CreateBy = r.CreateBy,
                    CreatorName = (u != null && !string.IsNullOrEmpty(u.Name)) ? u.Name : r.CreateBy,
                    Status = r.Status
                };
            })
            .OrderByDescending(x => x.CreateTime)
            .ToList();

            // 3. Masukkan ke ViewBag
            ViewBag.List = dtoList;
            return View();
        }

        // ==========================================
        // 2. HALAMAN PROSES (DETAIL & ACTION)
        // ==========================================
        public ActionResult ProcessReview(int id)
        {
            var currNIK = ((ClaimsIdentity)User.Identity).GetUserId();
            int userLevel = GetUserLevel(currNIK);

            var data = dblp.HC_LegalPermit_ReviewAgreementRequest.FirstOrDefault(x => x.ID == id);
            if (data == null) return HttpNotFound();

            // Proteksi: User biasa tidak boleh akses punya orang lain
            if (userLevel == 0 && data.CreateBy != currNIK)
                return RedirectToAction("ReviewAgreementRequest");

            // Jika User Level 0 DAN Status belum Approved (Completed), tolak akses.
            if (userLevel == 0 && data.Status != "Approved")
            {
                // Kirim pesan error ke halaman Index
                TempData["ErrorMsg"] = "Dokumen masih dalam proses Review. Anda baru bisa melihat detail setelah status COMPLETED.";
                return RedirectToAction("Index");
            }

            ViewBag.UserLevel = userLevel;

            // Logic Display Status (Biar View bersih)
            string displayStatus = data.Status;
            string badgeColor = "secondary";

            if (data.Status == "Pending")
            {
                displayStatus = (userLevel > 0) ? "OPEN" : "SUBMITTED";
                badgeColor = (userLevel > 0) ? "info" : "primary";
            }
            else if (data.Status == "WaitingApproval")
            {
                displayStatus = "WAITING APPROVAL";
                badgeColor = "warning";
            }
            else if (data.Status == "Approved")
            {
                displayStatus = "COMPLETED";
                badgeColor = "success";
            }
            else if (data.Status == "ReturnedL1")
            {
                displayStatus = "RETURNED TO ADMIN";
                badgeColor = "danger";
            }

            ViewBag.DisplayStatus = displayStatus;
            ViewBag.BadgeColor = badgeColor;
            ViewBag.data = data;

            return View();
        }

        // ==========================================
        // 3. SUBMIT REQUEST REVIEW AGREEMENT
        // ==========================================
        [HttpPost]
        public ActionResult SubmitReviewRequest(string projectName, string companyName)
        {
            try
            {
                var currUser = ((ClaimsIdentity)User.Identity).GetUserId(); // Ambil NIK User Login

                // 1. TANGKAP FILE DARI REQUEST
                // Menggunakan Request.Files karena FormData mengirimnya sebagai multipart
                var fileWord = Request.Files["fileWord"];
                var fileQuot = Request.Files["fileQuot"];
                var fileSign = Request.Files["fileSign"];

                // 2. VALIDASI INPUT
                if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(companyName))
                    return Json(new { success = false, message = "Nama Project dan Company wajib diisi." });

                // Validasi Ketersediaan File: hanya Draft (Word) yang wajib
                if (fileWord == null || fileWord.ContentLength == 0)
                    return Json(new { success = false, message = "Draft Agreement (Word) wajib diupload." });

                // Validasi Ekstensi (Security Check)
                var extWord = Path.GetExtension(fileWord.FileName).ToLower();
                if (extWord != ".doc" && extWord != ".docx")
                    return Json(new { success = false, message = "Draft Agreement harus format Word (.doc/.docx)." });

                // Quotation & Signer optional: validate extension only if provided
                string extQuot = "";
                if (fileQuot != null && fileQuot.ContentLength > 0)
                {
                    extQuot = Path.GetExtension(fileQuot.FileName).ToLower();
                    if (extQuot != ".pdf")
                        return Json(new { success = false, message = "Quotation harus format PDF." });
                }

                string extSign = "";
                if (fileSign != null && fileSign.ContentLength > 0)
                {
                    extSign = Path.GetExtension(fileSign.FileName).ToLower();
                    // Allow multiple types as originally intended
                    var allowedSignExts = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
                    if (!allowedSignExts.Contains(extSign))
                        return Json(new { success = false, message = "Signer Info memiliki format yang tidak diperbolehkan." });
                }

                // 3. SIAPKAN PENYIMPANAN
                string folderRelative = "~/Files/HC/LegalPermit/Review/";
                string serverFolder = Server.MapPath(folderRelative);
                if (!Directory.Exists(serverFolder)) Directory.CreateDirectory(serverFolder);

                // Generate Timestamp unik agar nama file rapi
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string cleanProjectName = projectName.Replace(" ", "_").Replace("/", "-"); // Bersihkan karakter aneh

                // 4. PROSES SAVE FILE KE SERVER

                // A. Draft Agreement (wajib)
                string nameWord = $"DRAFT_{timeStamp}_{cleanProjectName}{extWord}";
                fileWord.SaveAs(Path.Combine(serverFolder, nameWord));

                // B. Quotation (optional)
                string nameQuot = "";
                if (fileQuot != null && fileQuot.ContentLength > 0)
                {
                    nameQuot = $"QUOT_{timeStamp}_{cleanProjectName}{extQuot}";
                    fileQuot.SaveAs(Path.Combine(serverFolder, nameQuot));
                }

                // C. Signer Info (optional)
                string nameSign = "";
                if (fileSign != null && fileSign.ContentLength > 0)
                {
                    nameSign = $"SIGN_{timeStamp}_{cleanProjectName}{extSign}";
                    fileSign.SaveAs(Path.Combine(serverFolder, nameSign));
                }

                // 5. SIMPAN KE DATABASE
                var newRequest = new HC_LegalPermit_ReviewAgreementRequest
                {
                    ProjectName = projectName,
                    CompanyName = companyName,

                    // Simpan Nama File Unik (Yang ada di server)
                    ReviewDocument = nameWord,
                    QuotationDocumentName = nameQuot,
                    SignerInfoDocumentName = nameSign,

                    // Simpan Nama File Asli (Opsional, untuk display ke user jika perlu)
                    OriginalReviewDocumentName = fileWord.FileName,

                    Status = "Pending",
                    CreateTime = DateTime.Now,
                    CreateBy = currUser
                };

                dblp.HC_LegalPermit_ReviewAgreementRequest.Add(newRequest);
                dblp.SaveChanges();

                // 6. KIRIM EMAIL NOTIFIKASI
                try
                {
                    SendEmailReviewRequest(newRequest.CreateBy, newRequest, "Submitted");
                }
                catch (Exception emailEx)
                {
                    // Jangan gagalkan proses submit hanya karena email error, cukup log saja
                    System.Diagnostics.Debug.WriteLine("Email Error: " + emailEx.Message);
                }

                return Json(new { success = true, message = "Request berhasil disubmit!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ==========================================
        // 4. ADMIN 1: UPLOAD REVIEW DOCUMENT
        // ==========================================
        [HttpPost]
        public ActionResult UploadReviewResult(int id, HttpPostedFileBase fileReview)
        {
            try
            {
                var currNIK = ((ClaimsIdentity)User.Identity).GetUserId();
                int userLevel = GetUserLevel(currNIK);

                // Validasi Hanya Admin 1 (atau level diatasnya jika boleh)
                if (userLevel < 1) return Json(new { success = false, message = "Unauthorized" });

                if (fileReview == null || fileReview.ContentLength == 0)
                    return Json(new { success = false, message = "File wajib diupload." });

                var ext = Path.GetExtension(fileReview.FileName).ToLower();
                if (ext != ".doc" && ext != ".docx")
                    return Json(new { success = false, message = "Hanya file Word (.docx) yang diperbolehkan." });

                var data = dblp.HC_LegalPermit_ReviewAgreementRequest.FirstOrDefault(x => x.ID == id);
                if (data == null) return Json(new { success = false, message = "Data not found" });

                // Save File
                string folder = Server.MapPath("~/Files/HC/LegalPermit/ReviewResult/");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string uniqueName = "REV_" + Guid.NewGuid().ToString() + ext;
                fileReview.SaveAs(Path.Combine(folder, uniqueName));

                // Update DB
                data.AfterReviewDocument = uniqueName;
                data.OriginalAfterReviewDocument = fileReview.FileName;
                data.ReviewedBy = currNIK;
                data.ReviewedTime = DateTime.Now;
                data.Status = "WaitingApproval"; // Status berubah jadi Waiting Approval

                dblp.SaveChanges();

                SendEmailReviewRequest(data.CreateBy, data, "WaitingApproval");

                return Json(new { success = true, message = "Dokumen review berhasil diupload. Status: Waiting Approval L2." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // 5. ADMIN 2: APPROVE / RETURN
        // ==========================================
        [HttpPost]
        public ActionResult ActionL2(int id, string type)
        {
            var currNIK = ((ClaimsIdentity)User.Identity).GetUserId();
            if (GetUserLevel(currNIK) != 2) return Json(new { success = false, message = "Hanya Admin Level 2." });

            var data = dblp.HC_LegalPermit_ReviewAgreementRequest.FirstOrDefault(x => x.ID == id);

            if (type == "Approve")
            {
                data.Status = "Approved";
                data.ApprovedBy = currNIK;
                data.ApprovedTime = DateTime.Now;
            }
            else // Return to Admin 1
            {
                data.Status = "ReturnedL1"; // Kembalikan ke Admin 1 untuk revisi review
                // File review lama tidak dihapus, tapi status memungkinkan Admin 1 upload ulang
            }
            SendEmailReviewRequest(data.CreateBy, data, data.Status);

            dblp.SaveChanges();
            return Json(new { success = true, message = "Proses Berhasil." });
        }
        public void SendEmailReviewRequest(string toName, dynamic dataRequest, string actionType)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();

            string FilePath = Path.Combine(Server.MapPath("~/Emails/HC/LegalPermit/"), "InfoReviewRequest.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();

            string subject = "";
            string headerMsg = "";

            string domain = Request.Url.Scheme + "://" + Request.Url.Authority;
            string linkUrl = "";

            List<V_Users_Active> userEmailTo = new List<V_Users_Active>();

            if (actionType == "Submitted")
            {
                subject = "[New Request] Review Agreement - " + dataRequest.ProjectName;
                headerMsg = "New Review Agreement Request has been created";
                // FIX: Use local variable to avoid dynamic in expression tree
                string createdByNIK = dataRequest.CreateBy;
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                        .Where(x => x.UserLevel == 1)
                       .Select(x => x.UserNIK)
                       .ToList();
                // Include both the request creator and all level-2 admins as recipients
                userEmailTo = db.V_Users_Active
                                .Where(x => x.NIK == createdByNIK || listNikAdmin.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ProcessReview", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            else if (actionType == "WaitingApproval")
            {
                subject = "[ACTION] Review Agreement Request - " + dataRequest.ProjectName;
                headerMsg = "Mohon review request agreement berikut.";
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                       .Where(x => x.UserLevel == 2 && x.IsDeleted == 0)
                       .Select(x => x.UserNIK)
                       .ToList();

                userEmailTo = db.V_Users_Active
                                    .Where(x => listNikAdmin.Contains(x.NIK))
                                    .ToList();
                linkUrl = domain + Url.Action("ProcessReview", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            else if (actionType == "Approved")
            {
                subject = "[APPROVED] Review Agreement - " + dataRequest.ProjectName;
                headerMsg = "Review Request Anda telah disetujui.";
                // FIX: Use local variable to avoid dynamic in expression tree
                string createdByNIK = dataRequest.CreateBy;
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                       .Select(x => x.UserNIK)
                       .ToList();
                // Include both the request creator and all level-2 admins as recipients
                userEmailTo = db.V_Users_Active
                                .Where(x => x.NIK == createdByNIK || listNikAdmin.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ProcessReview", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            //else if (actionType == "Completed")
            //{
            //    subject = "[Completed] Agreement - " + dataRequest.ProjectName;
            //    headerMsg = "Drafting Request telah complete.";
            //    // FIX: Use local variable to avoid dynamic in expression tree
            //    string createdByNIK = dataRequest.CreatedBy;
            //    var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
            //           .Where(x => x.UserLevel == 1)
            //           .Select(x => x.UserNIK)
            //           .ToList();
            //    // Include both listNikAdmin
            //    userEmailTo = db.V_Users_Active
            //                    .Where(x => x.NIK == createdByNIK || listNikAdmin.Contains(x.NIK))
            //                    .ToList();
            //    linkUrl = domain + Url.Action("ProcessReview", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            //}
            else if (actionType == "ReturnedL1")
            {
                subject = "[REVISION] Agreement - " + dataRequest.ProjectName;
                headerMsg = "Request dikembalikan untuk revisi.";
                var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
                       .Where(x => x.UserLevel == 1)
                       .Select(x => x.UserNIK)
                       .ToList();
                userEmailTo = db.V_Users_Active
                                .Where(x => listNikAdmin.Contains(x.NIK))
                                .ToList();
                linkUrl = domain + Url.Action("ProcessReview", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            }
            //else if (actionType == "Revision")
            //{
            //    subject = "[REVISION] Agreement - " + dataRequest.ProjectName;
            //    headerMsg = "Request dikembalikan untuk revisi.";
            //    var listNikAdmin = dblp.HC_LegalPermit_UserAdmin
            //           .Where(x => x.UserLevel == 1)
            //           .Select(x => x.UserNIK)
            //           .ToList();
            //    userEmailTo = db.V_Users_Active
            //                    .Where(x => listNikAdmin.Contains(x.NIK))
            //                    .ToList();
            //    linkUrl = domain + Url.Action("ProcessReview", "LegalPermit", new { id = dataRequest.ID, area = "HC" });
            //}


            MailText = MailText.Replace("{{MESSAGE_HEADER}}", headerMsg);
            MailText = MailText.Replace("{{PROJECT_NAME}}", dataRequest.ProjectName);
            MailText = MailText.Replace("{{SUPPLIER_NAME}}", dataRequest.CompanyName);
            MailText = MailText.Replace("{{REQUEST_DATE}}", dataRequest.CreateTime.ToString("dd MMM yyyy"));
            MailText = MailText.Replace("{{STATUS_RAW}}", actionType);
            MailText = MailText.Replace("{{STATUS_DISPLAY}}", actionType.ToUpper());
            MailText = MailText.Replace("{{LINK_URL}}", linkUrl);

            // Simpan template dasar yang sudah terisi data umum
            string baseTemplate = MailText;

            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "Legal App Notification");
            var password = "100%NGKbusi!";

            //var sub = "Legal Apps Notification";
            //var body = MailText;

            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, password)
            };
            // --- 2. LOOPING PENGIRIMAN EMAIL ---
            foreach (var user in userEmailTo)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        using (var mess = new MailMessage())
                        {
                            mess.From = senderEmail;
                            mess.Subject = subject;
                            mess.IsBodyHtml = true;
                            mess.To.Add(new MailAddress(user.Email));

                            string finalBody = baseTemplate.Replace("{{RECEIVER_NAME}}", user.Name ?? "User");

                            mess.Body = finalBody;

                            // Kirim Email Individual
                            smtp.Send(mess);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error jika pengiriman ke satu user gagal, agar loop ke user lain tetap jalan
                        System.Diagnostics.Debug.WriteLine($"Gagal kirim ke {user.Email}: {ex.Message}");
                    }
                }
            }
        }

    }
}