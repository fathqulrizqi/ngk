using Microsoft.AspNet.Identity;
using NGK_AX.Models;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.SCM.Controllers
{
    public class FinishGoodsClaimController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        FinishGoodsClaimConnection dbfg = new FinishGoodsClaimConnection();

        // GET: SCM/FinishGoodsClaim
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetClaimByBN(string bn)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var dataBN = dbfg.V_Search_Claim_By_BN
                        .Where(w => w.FScanBarcodeID.Contains(bn))
                        .ToList();

            //var jsonResult = Json(new { rows = itemList, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            var jsonResult = Json(new { user = currUser, data = dataBN });
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;


        }

        [HttpPost]
        public JsonResult GetClaimByLotNoAndType(string lotNo, string type)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            string cnnString = System.Configuration.ConfigurationManager.ConnectionStrings["BarcodeConnection"].ConnectionString;
            SqlConnection con = new SqlConnection(cnnString);

            SqlCommand cmd = new SqlCommand("sp_Search_Claim_By_LotNo_Or_Type", con);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@LotNo", lotNo);
            cmd.Parameters.AddWithValue("@Type", type);

            SqlDataAdapter sd = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            con.Open();
            sd.Fill(dt);
            con.Close();

            List<Tbl_ClaimByLotNoAndType> dataClaim = new List<Tbl_ClaimByLotNoAndType>();

            foreach (DataRow dr in dt.Rows)
            {
                var ct = Convert.ToDateTime(dr["FEntryDate"]);
                var cr = Convert.ToDateTime(dr["FHistorySysDateReceived"]);
                var entryDate = ct.ToString("dd MM yyyy");
                var FHistorySysDateReceived = cr.ToString("dd MM yyyy");

                dataClaim.Add(
                    new Tbl_ClaimByLotNoAndType
                    {
                        FPM = Convert.ToString(dr["FPM"]),
                        Qty = Convert.ToInt32(dr["Qty"]),
                        FEntryDate = entryDate,
                        FHistorySysDateReceived = FHistorySysDateReceived,
                        ThirdpartyName = Convert.ToString(dr["ThirdpartyName"]),
                        ThirdpartyAddress = Convert.ToString(dr["ThirdpartyAddress"]),
                        LotNo = Convert.ToString(dr["LotNo"])
                    });
            }


            //var jsonResult = Json(new { rows = itemList, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            var jsonResult = Json(new { user = currUser, data = dataClaim });
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;


        }

        [HttpPost]
        public JsonResult GetRepackingData(string lotNo, string type)
        {
            var currUser = ((ClaimsIdentity)User.Identity).GetUserId();
            var CurrUser = db.V_Users_Active.Where(w => w.NIK == currUser).FirstOrDefault();

            var dataRepacking = dbfg.V_Search_Claim_Repacking
                                .Where(w => w.FWillRepackIntoType.Contains(type) && w.FLot.Contains(lotNo))
                                .GroupBy(w => new { w.FICNo, w.FLot, w.FWillRepackIntoType, w.FType, w.FRequestDate })
                                .Select(g => new
                                {
                                    FICNo = g.Key.FICNo,
                                    totalQty = g.Sum(x => x.FQty),
                                    FLot = g.Key.FLot,
                                    FWillRepackIntoType = g.Key.FWillRepackIntoType,
                                    FType = g.Key.FType,
                                    FRequestDate = g.Key.FRequestDate
                                })
                                .ToList();

            //var jsonResult = Json(new { rows = itemList, totalNotFiltered = CountRow, total = CountRow }, JsonRequestBehavior.AllowGet);
            var jsonResult = Json(new { user = currUser, data = dataRepacking });
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;


        }

    }
}