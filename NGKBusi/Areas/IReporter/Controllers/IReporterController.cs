using Newtonsoft.Json;
using NGKBusi.Areas.IReporter.Models;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Windows.Markup;

namespace NGKBusi.Areas.IReporter.Controllers
{
    public class IReporterController : Controller
    {
        IReporterConnection dbIReporter = new IReporterConnection();

        // GET: IReporter/IReporter
        public ActionResult Index()
        {

            ViewBag.NavHide = true;

            return View();
        }

        public JsonResult getIreporterData(string iRepName, DateTime iStartDate, DateTime iEndDate, string iSheetName = "")
        {
            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString());
            con.Open();

            SqlCommand cmd = new SqlCommand("sp_iReporterReport", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@reportName", iRepName);
            cmd.Parameters.AddWithValue("@sheetName", iSheetName);
            cmd.Parameters.AddWithValue("@startDate", iStartDate);
            cmd.Parameters.AddWithValue("@endDate", iEndDate);
            cmd.CommandTimeout = 30000;
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
            var result = JsonConvert.SerializeObject(dt,
                       new JsonSerializerSettings
                       {
                           ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                       });
            var fResult = Json(result, JsonRequestBehavior.AllowGet);
            fResult.MaxJsonLength = Int32.MaxValue;

            return fResult;
        }
        //public async Task<dynamic> getIreporterData(string iRepName, string iStartDate, string iEndDate, string iSheetName = "")
        //{
        //    //set up client
        //    HttpClient client = new HttpClient();
        //    string _url = "http://192.168.1.248:3000/data/warehouse?form=" + iRepName + "&start=" + iStartDate + "&end=" + iEndDate;
        //    var response = await client.GetAsync(_url);
        //    var result = await response.Content.ReadAsStringAsync();
        //    dynamic jsonresult = JsonConvert.DeserializeObject(result);
        //    var fResult = Json(JsonConvert.SerializeObject(jsonresult["data"]), JsonRequestBehavior.AllowGet);
        //    fResult.MaxJsonLength = Int32.MaxValue;

        //    return fResult;
        //}
    }
}