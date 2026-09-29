using Microsoft.AspNet.Identity;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.IT.Controllers
{
    [Authorize]
    public class ITPolicyController : Controller
    {
        ITPolicyConnection dbit = new ITPolicyConnection();
        DefaultConnection db = new DefaultConnection();
        // GET: IT/ITPolicy
        public ActionResult Index()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var spl = dbit.IT_ITPolicy_Document.ToList();
            ViewBag.document = spl;
            ViewBag.nik = CurrUser.NIK;
            return View();
        }
        public ActionResult AllPolicy()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var spl = dbit.IT_Regulation_Document.Where(w=>w.IsActive ==1).ToList();
            ViewBag.document = spl;
            ViewBag.nik = CurrUser.NIK;
            return View();
        }

        [HttpPost]
        public JsonResult UploadITPolicyDocument(HttpPostedFileBase fileTaxDocumentation, string Year)
        {
            HttpPostedFileBase file = Request.Files["fileTaxDocumentation"];

            if (fileTaxDocumentation.ContentLength > 0)
            {
                string extension = Path.GetExtension(fileTaxDocumentation.FileName);
                string filePath = Server.MapPath("~/Files/IT/ITPolicy/");
                string fileName = fileTaxDocumentation.FileName;
                filePath = filePath + fileName;

                fileTaxDocumentation.SaveAs(filePath);

                IT_ITPolicy_Document header = new IT_ITPolicy_Document();
                header.Year = Year;
                header.DocumentName = fileName;

                dbit.IT_ITPolicy_Document.Add(header);

                int i = dbit.SaveChanges();

                if (i > 0)
                {
                    return
                     Json(new
                     {
                         status = 1,
                         fileName = fileName
                     }, JsonRequestBehavior.AllowGet);
                } else
                {
                    return Json(new
                    {
                        status = 0,
                    }, JsonRequestBehavior.AllowGet);
                }
                

                
            }
            else
            {
                return Json(new
                {
                    status = 0,
                }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public ActionResult DeleteDocument(int Id)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).First();
            try
            {
                var data = dbit.IT_ITPolicy_Document.Where(w => w.ID == Id).OrderByDescending(t=>t.Year).FirstOrDefault();

                string filePath = Server.MapPath("~/Files/IT/ITPolicy/");
                string fileName = data.DocumentName;
                filePath = filePath + fileName;

                dbit.IT_ITPolicy_Document.Remove(data);
                var del = dbit.SaveChanges();
                if (del == 1)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    return Json(new
                    {
                        status = "1",
                        msg = "Item Deleted"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "0",
                        msg = "failed"
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "2",
                    msg = "failed"
                });
            }

        }

        [HttpPost]
        public JsonResult GetPDF(int fileId)
        {
            byte[] bytes;
            string fileName, contentType;
            string constr = ConfigurationManager.ConnectionStrings["ConString"].ConnectionString;
            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = "SELECT Name, Data, ContentType FROM tblFiles WHERE Id=@Id";
                    cmd.Parameters.AddWithValue("@Id", fileId);
                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        sdr.Read();
                        bytes = (byte[])sdr["Data"];
                        contentType = sdr["ContentType"].ToString();
                        fileName = sdr["Name"].ToString();
                    }
                    con.Close();
                }
            }
            JsonResult jsonResult = Json(new { FileName = fileName, ContentType = contentType, Data = bytes });
            jsonResult.MaxJsonLength = int.MaxValue;

            return jsonResult;
        }



        public ActionResult isms()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var spl = dbit.IT_ISMS_Document.ToList();
            ViewBag.document = spl;
            ViewBag.nik = CurrUser.NIK;
            return View();
        }
        [HttpPost]
        public JsonResult UploadITISMS(HttpPostedFileBase fileTaxDocumentation, string Year)
        {
            HttpPostedFileBase file = Request.Files["fileTaxDocumentation"];

            if (fileTaxDocumentation.ContentLength > 0)
            {
                string extension = Path.GetExtension(fileTaxDocumentation.FileName);
                string filePath = Server.MapPath("~/Files/IT/ITISMS/");
                string fileName = fileTaxDocumentation.FileName;
                filePath = filePath + fileName;

                fileTaxDocumentation.SaveAs(filePath);

                IT_ISMS_Document header = new IT_ISMS_Document();
                header.Year = Year;
                header.DocumentName = fileName;

                dbit.IT_ISMS_Document.Add(header);

                int i = dbit.SaveChanges();

                if (i > 0)
                {
                    return
                     Json(new
                     {
                         status = 1,
                         fileName = fileName
                     }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        status = 0,
                    }, JsonRequestBehavior.AllowGet);
                }



            }
            else
            {
                return Json(new
                {
                    status = 0,
                }, JsonRequestBehavior.AllowGet);
            }

        }
        public ActionResult csirt()
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();
            var spl = dbit.IT_CSIRT_Document.ToList();
            ViewBag.document = spl;
            ViewBag.nik = CurrUser.NIK;
            return View();
        }
        [HttpPost]
        public JsonResult UploadITCSIRT(HttpPostedFileBase fileTaxDocumentation, string Year)
        {
            HttpPostedFileBase file = Request.Files["fileTaxDocumentation"];

            if (fileTaxDocumentation.ContentLength > 0)
            {
                string extension = Path.GetExtension(fileTaxDocumentation.FileName);
                string filePath = Server.MapPath("~/Files/IT/CSIRT/");
                string fileName = fileTaxDocumentation.FileName;
                filePath = filePath + fileName;

                fileTaxDocumentation.SaveAs(filePath);

                IT_CSIRT_Document header = new IT_CSIRT_Document();
                header.Year = Year;
                header.DocumentName = fileName;

                dbit.IT_CSIRT_Document.Add(header);

                int i = dbit.SaveChanges();

                if (i > 0)
                {
                    return
                     Json(new
                     {
                         status = 1,
                         fileName = fileName
                     }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        status = 0,
                    }, JsonRequestBehavior.AllowGet);
                }



            }
            else
            {
                return Json(new
                {
                    status = 0,
                }, JsonRequestBehavior.AllowGet);
            }

        }

    }
}