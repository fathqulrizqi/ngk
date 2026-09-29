using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using NGKBusi.Models;
using System.Data.Entity.SqlServer;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using System.Globalization;

namespace NGKBusi.Areas.PE.Controllers
{
    public class NumberingSystemController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: PE/NumberingSystem
        [Authorize]
        public ActionResult Index()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId();
            var coll = (from usr in db.Users.DefaultIfEmpty()
                        from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                        where usr.NIK == currUserID && rol.menuID == 5
                        select new { usr, rol })
                                .AsEnumerable().Select(s => s.usr);
            if (coll.FirstOrDefault() == null)
            {
                return View("UnAuthorized");
            }

            ViewBag.CheckCode = db.PE_NumberingSystem_DocCode.Where(z => z.docid == 3 || z.docid == 4 || z.docid == 5).ToList();
            ViewBag.DLInit = db.PE_NumberingSystem_DocCode.Where(z => z.docid == 1).ToList();
            ViewBag.DLLine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "A").ToList();
            ViewBag.DMInit = db.PE_NumberingSystem_DocCode.Where(z => z.docid == 2).ToList();
            ViewBag.DMLine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "A").ToList();
            ViewBag.DMMachine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "B").ToList();
            ViewBag.DMProcess = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "D").ToList();
            ViewBag.MHInit = db.PE_NumberingSystem_DocCode.Where(z => z.docid == 4).ToList();
            ViewBag.MHProducts = db.PE_NumberingSystem_Products.ToList();
            ViewBag.MHLine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "A").ToList();
            ViewBag.MHPartName = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "E").ToList();
            ViewBag.JDMInit = db.PE_NumberingSystem_DocCode.Where(z => z.docid == 3).ToList();
            ViewBag.JDMLine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "A").ToList();
            ViewBag.JDMMachine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "B").ToList();
            ViewBag.JDMDrawing = db.PE_NumberingSystem_Products.ToList();
            ViewBag.JDMProcess = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "C2").ToList();
            ViewBag.TCLine  = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "A").ToList();
            ViewBag.TCMachine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "B").ToList();
            ViewBag.TCTools = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "C2").ToList();
            ViewBag.TNLine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "A").ToList();
            ViewBag.TNMachine = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "B").ToList();
            ViewBag.TNTools = db.PE_NumberingSystem_Appendix.Where(z => z.appcode == "C1").ToList();
            ViewBag.DocList = db.PE_NumberingSystem_DocList.ToList();
            ViewBag.DocList_Sub = db.PE_NumberingSystem_DocList_Sub.ToList();
            ViewBag.NumberingList = db.PE_NumberingSystem_NumberingList.Where(z => z.ParentDoc == null).OrderByDescending(z => z.IssuedDate).ToList();
            return View();
        }
        
        [HttpPost]
        public ActionResult NSDelete()
        {
            var currID = Int32.Parse(Request["parentID"]);
            var currDoc = Request["parentDoc"];
            db.PE_NumberingSystem_NumberingList.RemoveRange(db.PE_NumberingSystem_NumberingList.Where(x => x.id == currID || x.ParentDoc == currDoc));
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult NSAddChild()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currParentDoc = Request["iParentDoc"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var lastDocNumber = db.PE_NumberingSystem_NumberingList.Where(x => x.DocNumber.StartsWith(currParentDoc) && x.DocNumber.Length == currParentDoc.Length + 1).OrderByDescending(o => o.id).FirstOrDefault();
            double temp;
            //'@' == Char Before 'A';
            var lastResult = (lastDocNumber == null || Double.TryParse(lastDocNumber.DocNumber.Substring(currParentDoc.Length - 1 ), out temp) ? "@" : (lastDocNumber.DocNumber.Substring(currParentDoc.Length).Length==0?"@":lastDocNumber.DocNumber.Substring(currParentDoc.Length)));
            
            char lastChar = Convert.ToChar(lastResult);
            lastChar++;
            var childNumber = currParentDoc + lastChar;
            
            db.PE_NumberingSystem_NumberingList.Add(new PE_NumberingSystem_NumberingList
            {
                DocNumber = childNumber,
                ParentDoc = currParentDoc,
                Remark = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();
            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        [HttpPost]
        public ActionResult NSEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currParentDoc = Request["iParentDoc"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var updatedNS = db.PE_NumberingSystem_NumberingList.Where(x => x.DocNumber == currParentDoc).FirstOrDefault();
            updatedNS.Remark = currRemark;
            db.SaveChanges();
            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        [HttpPost]
        [Authorize]
        public ActionResult NewDPPLine()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currInit = Request["iInit"];
            var currLine = Request["iLine"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var currMonth = DateTime.Now.ToString("MM");
            var currYear = DateTime.Now.ToString("yy");
            var lastSeqData = db.PE_NumberingSystem_NumberingList.Where(x => SqlFunctions.PatIndex(currInit + currLine + "-%"+ currYear + "-%",x.DocNumber) > 0 && x.ParentDoc == null).OrderByDescending(o => o.id).FirstOrDefault();
            var lastSeq = "";
            if (lastSeqData != null)
            {
                lastSeq = "000" + (Int32.Parse(lastSeqData.DocNumber.Substring(lastSeqData.DocNumber.Length -3)) + 1);
            }
            else
            {
                lastSeq = "001";
            }
            var docNumber = currInit + currLine + "-" + currMonth + currYear + "-" + lastSeq.Substring(lastSeq.Length - 3);

            db.PE_NumberingSystem_NumberingList.Add(new PE_NumberingSystem_NumberingList
            {
                DocNumber = docNumber,
                Remark = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        public ActionResult NewDPPMachine()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currInit = Request["iInit"];
            var currLine = Request["iLine"];
            var currMachine = Request["iMachine"];
            var currProcess = Request["iProcess"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var lastSeqData = db.PE_NumberingSystem_NumberingList.Where(x => SqlFunctions.PatIndex(currInit + "-" + currLine + "%-%", x.DocNumber) > 0 && x.ParentDoc == null).OrderByDescending(o => o.id).FirstOrDefault();
            var lastSeq = "";
            if (lastSeqData != null)
            {
                lastSeq = "000" + (Int32.Parse(lastSeqData.DocNumber.Substring(lastSeqData.DocNumber.Length - 3)) + 1);
            }
            else
            {
                lastSeq = "001";
            }
            var docNumber = currInit + "-" + currLine + currMachine + "-" + currProcess + "-" + lastSeq.Substring(lastSeq.Length - 3);

            db.PE_NumberingSystem_NumberingList.Add(new PE_NumberingSystem_NumberingList
            {
                DocNumber = docNumber,
                Remark = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        public ActionResult NewJDM()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currInit = Request["iInit"];
            var currLine = Request["iLine"];
            var currMachine = Request["iMachine"];
            var currDrawing = Request["iDrawing"];
            var currProcess = Request["iProcess"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var lastSeqData = db.PE_NumberingSystem_NumberingList.Where(x => SqlFunctions.PatIndex( currInit + currLine + currMachine + "-%", x.DocNumber) > 0 && x.ParentDoc == null).OrderByDescending(o => o.id).FirstOrDefault();
            var lastSeq = "";
            if (lastSeqData != null)
            {
                lastSeq = "00" + (Int32.Parse(lastSeqData.DocNumber.Substring(lastSeqData.DocNumber.Length - 2)) + 1);
            }
            else
            {
                lastSeq = "01";
            }
            var docNumber = currInit + currLine + currMachine + "-" + currDrawing + currProcess + lastSeq.Substring(lastSeq.Length - 2);
            
            db.PE_NumberingSystem_NumberingList.Add(new PE_NumberingSystem_NumberingList
            {
                DocNumber = docNumber,
                Remark = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        public ActionResult NewMatHandling()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currInit = Request["iInit"];
            var currProduct = Request["iProduct"];
            var currLine = Request["iLine"];
            var currPart = Request["iPart"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var lastSeqData = db.PE_NumberingSystem_NumberingList.Where(x => SqlFunctions.PatIndex(currInit + currProduct + currLine + "-%", x.DocNumber) > 0 && x.ParentDoc == null).OrderByDescending(o => o.id).FirstOrDefault();
            var lastSeq = "";
            if (lastSeqData != null)
            {
                lastSeq = "000" + (Int32.Parse(lastSeqData.DocNumber.Substring(lastSeqData.DocNumber.Length - 3)) + 1);
            }
            else
            {
                lastSeq = "001";
            }
            var docNumber = currInit + currProduct + currLine + "-" + currPart + lastSeq.Substring(lastSeq.Length - 3);
            
            db.PE_NumberingSystem_NumberingList.Add(new PE_NumberingSystem_NumberingList
            {
                DocNumber = docNumber,
                Remark = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        public ActionResult NewTC()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currInit = "T";
            var currLine = Request["iLine"];
            var currMachine = Request["iMachine"];
            var currConsume = 1;
            var currTool = Request["iTool"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var lastSeqData = db.PE_NumberingSystem_NumberingList.Where(x => SqlFunctions.PatIndex(currInit + "%" + currMachine + "-%", x.DocNumber) > 0 && x.ParentDoc == null).OrderByDescending(o => o.id).FirstOrDefault();
            var lastSeq = "";
            if (lastSeqData != null)
            {
                lastSeq = "00" + (Int32.Parse(lastSeqData.DocNumber.Substring(lastSeqData.DocNumber.Length - 2)) + 1);
            }
            else
            {
                lastSeq = "01";
            }
            var docNumber = currInit + currLine + currMachine + "-" + currConsume + currTool + lastSeq.Substring(lastSeq.Length - 2);

            db.PE_NumberingSystem_NumberingList.Add(new PE_NumberingSystem_NumberingList
            {
                DocNumber = docNumber,
                Remark = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        public ActionResult NewTN()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currInit = "T";
            var currLine = Request["iLine"];
            var currMachine = Request["iMachine"];
            var currConsume = 0;
            var currTool = Request["iTool"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var lastSeqData = db.PE_NumberingSystem_NumberingList.Where(x => SqlFunctions.PatIndex(currInit + "%" + currMachine + "-%", x.DocNumber) > 0 && x.ParentDoc == null).OrderByDescending(o => o.id).FirstOrDefault();
            var lastSeq = "";
            if (lastSeqData != null)
            {
                lastSeq = "00" + (Int32.Parse(lastSeqData.DocNumber.Substring(lastSeqData.DocNumber.Length - 2)) + 1);
            }
            else
            {
                lastSeq = "01";
            }
            var docNumber = currInit + currLine + currMachine + "-" + currConsume + currTool + lastSeq.Substring(lastSeq.Length - 2);

            db.PE_NumberingSystem_NumberingList.Add(new PE_NumberingSystem_NumberingList
            {
                DocNumber = docNumber,
                Remark = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("Index", "NumberingSystem", new { area = "PE" });
        }

        [Authorize]
        public ActionResult Appendix()
        {
            ViewBag.AppendixList = db.PE_NumberingSystem_Appendix.OrderBy(x => x.appcode).ThenBy(x=>x.code).ToList();
            return View();
        }
        
        [HttpPost]
        public ActionResult addAppendix()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currAppCode = Request["iAppCode"];
            var currCode = Request["iCode"];
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            db.PE_NumberingSystem_Appendix.Add(new PE_NumberingSystem_Appendix
            {
                appcode = currAppCode.ToUpper(),
                code = currCode.ToUpper(),
                name = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("Appendix", "NumberingSystem", new { area = "PE" });
        }

        [HttpPost]
        public Int32 checkAppendix()
        {
            var currID = (Request["iID"] == null ? 0 : Int32.Parse(Request["iID"]));
            var currAppCode = Request["iAppCode"].ToUpper();
            var currCode = Request["iCode"].ToUpper();
            var CheckData = db.PE_NumberingSystem_Appendix.Where(x => x.id != currID && x.appcode == currAppCode && x.code == currCode).ToList();

            return CheckData.Count;
        }

        [HttpPost]
        public ActionResult editAppendix()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currID = Int32.Parse(Request["iID"]);
            var currAppCode = Request["iAppCode"].ToUpper();
            var currCode = Request["iCode"].ToUpper();
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var UpdatedData = db.PE_NumberingSystem_Appendix.Where(x => x.id == currID).FirstOrDefault();
                UpdatedData.appcode = currAppCode.ToUpper();
                UpdatedData.code = currCode.ToUpper();
                UpdatedData.name = currRemark;
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult deleteAppendix()
        {
            var currID = Int32.Parse(Request["iID"]);
            var DeletedData = db.PE_NumberingSystem_Appendix.Where(x => x.id == currID).FirstOrDefault();
            db.PE_NumberingSystem_Appendix.Remove(DeletedData);
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }


        [Authorize]
        public ActionResult DocInit()
        {
            ViewBag.DocList = db.PE_NumberingSystem_DocList.ToList();
            ViewBag.DocInitList = db.PE_NumberingSystem_DocCode.Include(b => b.PE_NumberingSystem_DocList).OrderBy(x => x.docid).ThenBy(x => x.code).ToList();
            return View();
        }

        [HttpPost]
        public ActionResult AddDocInit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currDocID = Int32.Parse(Request["iDocID"]);
            var currCode = Request["iCode"].ToUpper();
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            db.PE_NumberingSystem_DocCode.Add(new PE_NumberingSystem_DocCode
            {
                docid = currDocID,
                code = currCode.ToUpper(),
                name = currRemark,
                IssuedBy = currUser.GetUserId(),
                IssuedDate = DateTime.Now,
                IssuedIP = currUser.FindFirst("ipAddress").Value,
                IssuedPC = currUser.FindFirst("pcName").Value
            });
            db.SaveChanges();

            return RedirectToAction("DocInit", "NumberingSystem", new { area = "PE" });
        }
        
        [HttpPost]
        public Int32 checkDocInit()
        {
            var currID = (Request["iID"] == null ? 0 : Int32.Parse(Request["iID"]));
            var currDocID = Int32.Parse(Request["iDocID"]);
            var currCode = Request["iCode"].ToUpper();
            var checkData = db.PE_NumberingSystem_DocCode.Where(x => x.id != currID && x.docid == currDocID && x.code == currCode).ToList();

            return checkData.Count;
        }

        [HttpPost]
        public ActionResult deleteDocInit()
        {
            var currID = Int32.Parse(Request["iID"]);
            var deletedData = db.PE_NumberingSystem_DocCode.Where(x => x.id == currID).FirstOrDefault();
            db.PE_NumberingSystem_DocCode.Remove(deletedData);
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }

        [HttpPost]
        public ActionResult editDocInit()
        {
            var currID = Int32.Parse(Request["iID"]);
            var currDocID = Int32.Parse(Request["iDocID"]);
            var currCode = Request["iCode"].ToUpper();
            var currRemark = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Request["iRemark"]);
            var editedData = db.PE_NumberingSystem_DocCode.Where(x => x.id == currID).FirstOrDefault();
            editedData.docid = currDocID;
            editedData.code = currCode;
            editedData.name = currRemark;
            db.SaveChanges();

            return Content(Boolean.TrueString);
        }
    }

}