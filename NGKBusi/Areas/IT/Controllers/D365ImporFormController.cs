using Newtonsoft.Json;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.IT.Controllers
{

    public class D365ImporFormController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        // GET: IT/D365ImporForm
        public ActionResult Index()
        {
            return View();
        }
        private bool FileExists(string remotePath, WebClient client)
        {
            try
            {
                client.DownloadData(remotePath);
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        private void DeleteFile(string remotePath, WebClient client)
        {
            client.UploadString(remotePath, WebRequestMethods.Ftp.DeleteFile, "");
        }

        public class DeliveryData
        {
            public List<DeliveryItem> data { get; set; }
        }
        public class DeliveryItem
        {
            public string SlipNo { get; set; }
            public string PartNo { get; set; }
            public int DeliveryQty { get; set; }
            public string DeliveryShipDate { get; set; }
            public string CustomerCode { get; set; }
        }
        [HttpPost]
        public async Task<ActionResult> uploadStockManagementV2(HttpPostedFileBase fileExcel)
        {
            HttpPostedFileBase file = Request.Files["fileExcel"];

            //string filePath = Server.MapPath("~/Files/Sales/EDI/Download/");
            //string fileName = "sales_suzuki";
            //filePath = filePath + fileName + ".xls";

            //fileOESSuzuki.SaveAs(filePath);



            if (file != null && file.ContentLength > 0)
            {
                // Ganti dengan alamat IP dan kredensial server FTP Anda
                string ftpServerIP = "ftp://192.168.1.248:21/Sales/EDI/Download";
                string ftpUsername = "it_admin";
                string ftpPassword = "N6K4dm1n!";

                string fileName = "STO V2.xlsx.xlsx";
                string remotePath = ftpServerIP + "/" + fileName;
                //int statusUpload;
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                    try
                    {
                        // Cek apakah file sudah ada di server FTP
                        if (FileExists(remotePath, client))
                        {
                            // Jika file sudah ada, hapus file lama
                            DeleteFile(remotePath, client);
                        }

                        // Baca konten file ke dalam buffer
                        byte[] fileBytes;
                        using (Stream inputStream = file.InputStream)
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                inputStream.CopyTo(memoryStream);
                                fileBytes = memoryStream.ToArray();
                            }
                        }

                        // Unggah file baru ke server FTP
                        client.UploadData(remotePath, fileBytes);
                        //statusUpload = 1;
                    }
                    catch (Exception)
                    {
                        //statusUpload = 0;
                    }
                }
            }

            db.Database.ExecuteSqlCommand("Truncate table D365_StockManagement_V2");
            db.SaveChanges();
            using (var client = new HttpClient())
            {
                // Ganti URL dengan URL web service yang sesuai
                string url = "http://192.168.1.248:8081/EDIwebservice/impor_xls.php";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true, MustRevalidate = true };
                // Mengirim permintaan GET ke web service
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    db.Database.ExecuteSqlCommand("EXEC	sp_D365_ImportStockManagement");
                   
                    return Json(new
                    {
                        status = 1

                    }, JsonRequestBehavior.AllowGet); ;
                }
                else
                {
                    return View("Error");
                }
            }

        }
    }
}