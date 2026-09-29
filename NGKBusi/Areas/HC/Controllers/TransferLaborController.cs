using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace NGKBusi.Areas.HC.Controllers
{
    public class TransferLaborController : Controller
    {
        DefaultConnection db = new DefaultConnection();

        // GET: HC/TransferLabor
        [Authorize]
        public ActionResult RequestLabor()
        {
            string[] excludeSection = { "SERIKAT PEKERJA", "SC Reward", "CLINIC", "KOPERASI", "BOD IDL", "BOD ADM", "FACTORY OFFICE" };
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.Division = db.V_Users_Active.Where(w => !excludeSection.Contains(w.DivisionName)).OrderBy(o => o.DivisionName).Select(s => s.DivisionName).Distinct();

            return View();
        }
        

        public ActionResult RequestLaborAdd()
        {
            var dtStart = (Request["iReqStartDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iReqStartDate"] + " " + (Request["iReqStartTime"].Length == 0 ? "00:00" : Request["iReqStartTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var dtEnd = (Request["iReqEndDate"].Length == 0 ? (DateTime?)null : DateTime.ParseExact(Request["iReqEndDate"] + " " + (Request["iReqEndTime"].Length == 0 ? "00:00" : Request["iReqEndTime"]), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture));
            var currUser = (ClaimsIdentity)User.Identity;

            var newData = db.HC_TransferLabor_RequestionSheet.Add(new HC_TransferLabor_RequestionSheet
            {
                Req_Number = this.getSequence("TL"),
                Qty = int.Parse(Request["iReqQty"]),
                Section_Code = Request["iSectionFrom"].Split('|')[0],
                Section_Name = Request["iSectionFrom"].Split('|')[1],
                Division_Name = Request["iDivision"],
                JobStart = dtStart,
                JobEnd = dtEnd,
                Section_To_Code = Request["iSectionTo"].Split('|')[0],
                Section_To_Name = Request["iSectionTo"].Split('|')[1],
                Job = Request["iJob"],
                JobType = Request["iJobType"],
                JobDesc = Request["iJobDesc"],
                Created_By = currUser.GetUserId(),
                Created_At = DateTime.Now,
                Approval = 1,
                Approval_Sub = 0
            });
            db.SaveChanges();


            return RedirectToAction("RequestLabor", "TransferLabor", new { area = "HC" });
        }
        public ActionResult RequestProposal()
        {

            ViewBag.NavHide = true;
            ViewBag.RequestList = db.HC_TransferLabor_RequestionSheet.ToList();

            string[] excludeSection = { "SERIKAT PEKERJA", "SC Reward", "CLINIC", "KOPERASI", "BOD IDL", "BOD ADM", "FACTORY OFFICE" };
            ViewBag.Section = db.AX_Section.ToList();
            ViewBag.Division = db.V_Users_Active.Where(w => !excludeSection.Contains(w.DivisionName)).OrderBy(o => o.DivisionName).Select(s => s.DivisionName).Distinct();


            return View();
        }

        public String ApprovalStatus(int type)
        {
            var stat = "Submitted";
            switch (type)
            {
                case 1:
                    stat = "Submitted";
                    break;
                case 2:
                    stat = "Reviewed";
                    break;
                case 3:
                    stat = "Approved";
                    break;
                default:
                    stat = "Done";
                    break;
            }



            return stat;
        }
        public String getSequence(string type)
        {
            var lastSeq = "";
            var seqHeader = type + "-" + DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM");

            var latestSequence = db.HC_TransferLabor_RequestionSheet.Where(w => w.Req_Number.Substring(0, seqHeader.Length) == seqHeader).OrderByDescending(o => o.ID).Select(s => s.Req_Number.Substring(s.Req_Number.Length - 4, 4)).FirstOrDefault();
            lastSeq = latestSequence != null ? "0000" + (Int32.Parse(latestSequence) + 1) : "0001";
            lastSeq = seqHeader + lastSeq.Substring(lastSeq.Length - 4, 4);

            return lastSeq;
        }        
    }
}