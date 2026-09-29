using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NGKBusi.Models;

namespace NGKBusi.Areas.SCM.Controllers
{
    public class BarcodeController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: SCM/Barcode
        public ActionResult Index()
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["BarcodeConnection"].ConnectionString;
            string q = "SELECT t1.SectionID,t2.[Name] SectionName,t1.RackNo,t1.PM,Sum(t1.QtyReal) QtyReal FROM [NGK_BARCODE].[dbo].[T_OutFG_DebitCredit] t1 left Join [NGK_INVENTORY].[dbo].[T_SECTION] t2 on t1.SectionID = t2.SectionID where t1.SectionID in (14,26) and t1.[RackNo] <> '' and t1.[RackNo] is not null and t1.QtyReal > 0 group by t1.SectionID,t2.[Name],t1.RackNo,t1.PM order by t1.SectionID,t1.RackNo,t1.PM";
            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(q, conn);
            var Barcode = new List<BarcodeRackFG>();
            using (conn)
            {
                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while(rdr.Read())
                {
                    var bar = new BarcodeRackFG();
                    bar.SectionID = rdr["SectionID"].ToString();
                    bar.SectionName = rdr["SectionName"].ToString();
                    bar.RackNo = rdr["RackNo"].ToString();
                    bar.PartNo = rdr["PM"].ToString();
                    bar.QTY = rdr["QtyReal"].ToString();

                    Barcode.Add(bar);
                }
            }
            ViewBag.BarcodeList = Barcode;
            return View();
        }
    }
}