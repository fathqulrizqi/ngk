using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGKBusi.Areas.WebService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace NGKBusi.Areas.WebService.Controllers
{
    public class TADAController : Controller
    {

        BengkelPointsConnection dBP = new BengkelPointsConnection();
        public int currentPage = 0;
        public int maxPage = 0;
        // GET: WebService/TADA
        public JsonResult GetMemberList(DateTime? startDate = (DateTime?)null, DateTime? endDate = (DateTime?)null, int page = 1, int perPage = 1000)
        {
            var responseString = "";
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes("9Z1rmDcfutDsE37q5uFNfZG9E" + ":" + "vRlkclV61mCpXLClhI3w2XnmnQESO7GPIBIetavJzbFadjl5nU");
            string toReturn = System.Convert.ToBase64String(bytes);
            var request = (HttpWebRequest)WebRequest.Create("https://api.gift.id/v1/m-integration/insight/membership/list");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers.Add("Authorization", "Basic " + toReturn);
            var sDate = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
            var eDate = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string jsonData = new JavaScriptSerializer().Serialize(new
                {
                    page = page,
                    perPage = perPage,
                    customerRegisteredStart = sDate,
                    customerRegisteredEnd = eDate
                });

                streamWriter.Write(jsonData);
                streamWriter.Flush();
                streamWriter.Close();
                using (var response1 = request.GetResponse())
                {
                    using (var reader = new StreamReader(response1.GetResponseStream()))
                    {
                        responseString = reader.ReadToEnd();
                    }
                }
            }
            var jsonResult = Json(responseString, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;

            //var jsonResultData = JObject.Parse(responseString);
            //maxPage = jsonResultData["data"].Count();

            //currentPage = 1;
            //JObject testArr = new JObject();
            //var test = testArr.ToList();

            //while(maxPage > 0)
            //{
            //    var jsonResultGetData = GetMemberListData(DateTime.Parse(sDate), DateTime.Parse(eDate), currentPage, maxPage);
            //    testArr.Add(jsonResultGetData["data"]);
            //}


            //return Json(test, JsonRequestBehavior.AllowGet);
            return jsonResult;
        }
        [HttpPost]
        public JsonResult UploadMemberData()
        {


            Tada_BengkelPoints_MemberList data = JsonConvert.DeserializeObject<Tada_BengkelPoints_MemberList>(Request["uploadData"]);
            var checkData = dBP.Tada_BengkelPoints_MemberList.Where(w => w.id == data.id).FirstOrDefault();
            if (checkData != null)
            {
                checkData.cardNo = data.cardNo;
                checkData.programName = data.programName;
                checkData.name = data.name;
                checkData.phone = data.phone;
                checkData.email = data.email;
                checkData.sex = data.sex;
                checkData.age = data.age;
                checkData.city = data.city;
                checkData.address = data.address;
                checkData.additionaldatastring = data.additionaldatastring;
                checkData.balance = data.balance;
                checkData.convertedBalance = data.convertedBalance;
                checkData.countTransaction = data.countTransaction;
                checkData.averageSpending = data.averageSpending;
                checkData.activationStoreLocation = data.activationStoreLocation;
                checkData.totalTransaction = data.totalTransaction;
                checkData.totalSpending = data.totalSpending;
                checkData.totalBalanceEarning = data.totalBalanceEarning;
                checkData.totalWalletEarning = data.totalWalletEarning;
                checkData.totalBalanceRedeem = data.totalBalanceRedeem;
                checkData.totalWalletRedeem = data.totalWalletRedeem;
                checkData.cardLevel = data.cardLevel;
                checkData.cardStatus = data.cardStatus;
                checkData.subscriptionStartDate = data.subscriptionStartDate;
                checkData.subscriptionExpiredDate = data.subscriptionExpiredDate;
                checkData.revenueGeneratedFromReferral = data.revenueGeneratedFromReferral;
                checkData.advocateName = data.advocateName;
                checkData.totalShares = data.totalShares;
                checkData.retentionRiskValue = data.retentionRiskValue;
                checkData.sherlockScore = data.sherlockScore;
            }
            else
            {
                dBP.Tada_BengkelPoints_MemberList.Add(new Tada_BengkelPoints_MemberList
                {
                    id = data.id,
                    cardNo = data.cardNo,
                    programName = data.programName,
                    name = data.name,
                    phone = data.phone,
                    email = data.email,
                    sex = data.sex,
                    age = data.age,
                    city = data.city,
                    address = data.address,
                    additionaldatastring = data.additionaldatastring,
                    balance = data.balance,
                    convertedBalance = data.convertedBalance,
                    countTransaction = data.countTransaction,
                    averageSpending = data.averageSpending,
                    activationStoreLocation = data.activationStoreLocation,
                    totalTransaction = data.totalTransaction,
                    totalSpending = data.totalSpending,
                    totalBalanceEarning = data.totalBalanceEarning,
                    totalWalletEarning = data.totalWalletEarning,
                    totalBalanceRedeem = data.totalBalanceRedeem,
                    totalWalletRedeem = data.totalWalletRedeem,
                    cardLevel = data.cardLevel,
                    cardStatus = data.cardStatus,
                    subscriptionStartDate = data.subscriptionStartDate,
                    subscriptionExpiredDate = data.subscriptionExpiredDate,
                    revenueGeneratedFromReferral = data.revenueGeneratedFromReferral,
                    advocateName = data.advocateName,
                    totalShares = data.totalShares,
                    retentionRiskValue = data.retentionRiskValue,
                    sherlockScore = data.sherlockScore,
                });
            }
            dBP.SaveChanges();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JObject GetMemberListData(DateTime? startDate = (DateTime?)null, DateTime? endDate = (DateTime?)null, int pageSize = 1, int dataCount = 1)
        {

            var responseString = "";
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes("9Z1rmDcfutDsE37q5uFNfZG9E" + ":" + "vRlkclV61mCpXLClhI3w2XnmnQESO7GPIBIetavJzbFadjl5nU");
            string toReturn = System.Convert.ToBase64String(bytes);
            var request = (HttpWebRequest)WebRequest.Create("https://api.gift.id/v1/m-integration/insight/membership/list");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers.Add("Authorization", "Basic " + toReturn);
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string jsonData = new JavaScriptSerializer().Serialize(new
                {
                    page = pageSize,
                    perPage = 1000,
                    customerActivationStart = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd"),
                    customerActivationEnd = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd")
                });

                streamWriter.Write(jsonData);
                streamWriter.Flush();
                streamWriter.Close();
                using (var response1 = request.GetResponse())
                {
                    using (var reader = new StreamReader(response1.GetResponseStream()))
                    {
                        responseString = reader.ReadToEnd();
                    }
                }
            }
            var jsonResultData = JObject.Parse(responseString);
            maxPage = jsonResultData["data"].Count();
            currentPage++;
            return jsonResultData;
        }

        public JsonResult GetTransactionList(DateTime? startDate = (DateTime?)null, DateTime? endDate = (DateTime?)null)
        {
            var responseString = "";
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes("9Z1rmDcfutDsE37q5uFNfZG9E" + ":" + "vRlkclV61mCpXLClhI3w2XnmnQESO7GPIBIetavJzbFadjl5nU");
            string toReturn = System.Convert.ToBase64String(bytes);
            var request = (HttpWebRequest)WebRequest.Create("https://api.gift.id/v1/m-integration/insight/transaction_list");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "*/*";
            request.Headers.Add("Authorization", "Basic " + toReturn);
            var sDate = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
            var eDate = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string jsonData = new JavaScriptSerializer().Serialize(new
                {
                    page = 1,
                    perPage = 100,
                    periodStart = sDate,
                    periodEnd = eDate
                });

                streamWriter.Write(jsonData);
                streamWriter.Flush();
                streamWriter.Close();
                using (var response1 = request.GetResponse())
                {
                    using (var reader = new StreamReader(response1.GetResponseStream()))
                    {
                        responseString = reader.ReadToEnd();
                    }
                }
            }
            var jsonResult = Json(responseString, JsonRequestBehavior.AllowGet);
            var jsonResultData = JObject.Parse(responseString);
            maxPage = (int)Math.Ceiling((double)int.Parse(jsonResultData["totalItems"].ToString()) / 100);
            currentPage = 1;
            string[] testArr = { };
            var test = testArr.ToList();

            for (var i = 1; i <= maxPage; i++)
            {
                var jsonResultGetData = GetTransactionListData(DateTime.Parse(sDate), DateTime.Parse(eDate), currentPage);
                test.Add(jsonResultGetData["data"].ToString());
            }
            jsonResult.MaxJsonLength = int.MaxValue;

            //return Json(jsonResultData["totalItems"].ToString(), JsonRequestBehavior.AllowGet);
            //return jsonResult;

            return Json(test, JsonRequestBehavior.AllowGet);
        }


        public JObject GetTransactionListData(DateTime? startDate = (DateTime?)null, DateTime? endDate = (DateTime?)null, int pageSize = 1)
        {

            var responseString = "";
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes("9Z1rmDcfutDsE37q5uFNfZG9E" + ":" + "vRlkclV61mCpXLClhI3w2XnmnQESO7GPIBIetavJzbFadjl5nU");
            string toReturn = System.Convert.ToBase64String(bytes);
            var request = (HttpWebRequest)WebRequest.Create("https://api.gift.id/v1/m-integration/insight/transaction_list");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "*/*";
            request.Headers.Add("Authorization", "Basic " + toReturn);
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string jsonData = new JavaScriptSerializer().Serialize(new
                {
                    page = pageSize,
                    perPage = 100,
                    periodStart = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd"),
                    periodEnd = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd")
                });

                streamWriter.Write(jsonData);
                streamWriter.Flush();
                streamWriter.Close();
                using (var response1 = request.GetResponse())
                {
                    using (var reader = new StreamReader(response1.GetResponseStream()))
                    {
                        responseString = reader.ReadToEnd();
                    }
                }
            }
            var jsonResultData = JObject.Parse(responseString);
            currentPage++;
            return jsonResultData;
        }

        public JsonResult GetTransactionsList(DateTime? startDate = (DateTime?)null, DateTime? endDate = (DateTime?)null)
        {
            var responseString = "";
            byte[] bytes = Encoding.GetEncoding(28591).GetBytes("9Z1rmDcfutDsE37q5uFNfZG9E" + ":" + "vRlkclV61mCpXLClhI3w2XnmnQESO7GPIBIetavJzbFadjl5nU");
            string toReturn = System.Convert.ToBase64String(bytes);
            var sDate = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
            var eDate = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
            var request = (HttpWebRequest)WebRequest.Create("https://api.gift.id/v1/m-integration/transactions?periodStart=" + sDate + "&periodEnd=" + eDate);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Accept = "*/*";
            request.Headers.Add("Authorization", "Basic " + toReturn);
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string jsonData = new JavaScriptSerializer().Serialize(new
                {
                    //    //page = 1,
                    //    //perPage = 100,
                    //    periodStart = sDate,
                    //    periodEnd = eDate
                });

                streamWriter.Write(jsonData);
                streamWriter.Flush();
                streamWriter.Close();
                using (var response1 = request.GetResponse())
                {
                    using (var reader = new StreamReader(response1.GetResponseStream()))
                    {
                        responseString = reader.ReadToEnd();
                    }
                }
            }
            var jsonResult = Json(responseString, JsonRequestBehavior.AllowGet);
            //var jsonResultData = JObject.Parse(responseString);
            //maxPage = (int)Math.Ceiling((double)int.Parse(jsonResultData["totalItems"].ToString()) / 100);
            //currentPage = 1;
            //string[] testArr = { };
            //var test = testArr.ToList();

            //for (var i = 1; i <= maxPage; i++)
            //{
            //    var jsonResultGetData = GetTransactionListData(DateTime.Parse(sDate), DateTime.Parse(eDate), currentPage);
            //    test.Add(jsonResultGetData["data"].ToString());
            //}
            //jsonResult.MaxJsonLength = int.MaxValue;

            //return Json(jsonResultData["totalItems"].ToString(), JsonRequestBehavior.AllowGet);
            //return jsonResult;

            return jsonResult;
        }


        public ActionResult Index()
        {
            return View();
        }
        public ActionResult MemberList()
        {
            return View();
        }
        public ActionResult TransactionList()
        {
            return View();
        }
        public ActionResult SubmissionList()
        {
            return View();
        }
        public ActionResult EmailTest()
        {
            string emailSender = ConfigurationManager.AppSettings["emailsender"].ToString();
            string emailSenderPassword = ConfigurationManager.AppSettings["emailPassword"].ToString();
            string emailSenderHost = ConfigurationManager.AppSettings["smtpserver"].ToString();
            int emailSenderPort = Convert.ToInt16(ConfigurationManager.AppSettings["portnumber"]);
            Boolean emailIsSSL = Convert.ToBoolean(ConfigurationManager.AppSettings["IsSSL"]);


            //Fetching Email Body Text from EmailTemplate File.  
            string FilePath = Path.Combine(Server.MapPath("~/Emails/Shared/"), "Test.html");
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();
            str.Close();

            //Repalce [newusername] = signup user name   
            MailText = MailText.Replace("##UserName##", "User Test");
            MailText = MailText.Replace("##Dept##", "Information Technology");


            string subject = "Notif Test NGK";

            //Base class for sending email  
            MailMessage _mailmsg = new MailMessage();

            //Make TRUE because our body text is html  
            _mailmsg.IsBodyHtml = true;

            //Set From Email ID  
            _mailmsg.From = new MailAddress(emailSender);

            //Set To Email ID  
            _mailmsg.To.Add("azis.abdillah@ngkbusi.com");

            //Set Subject  
            _mailmsg.Subject = subject;

            //Set Body Text of Email   
            _mailmsg.Body = MailText;


            //Now set your SMTP   
            SmtpClient _smtp = new SmtpClient();

            //Set HOST server SMTP detail  
            _smtp.Host = emailSenderHost;

            //Set PORT number of SMTP  
            _smtp.Port = emailSenderPort;

            //Set SSL --> True / False  
            _smtp.EnableSsl = emailIsSSL;

            //Set Sender UserEmailID, Password  
            NetworkCredential _network = new NetworkCredential(emailSender, emailSenderPassword);
            _smtp.Credentials = _network;

            //Send Method will send your MailMessage create above.  
            _smtp.Send(_mailmsg);

            return Content(MailText);
        }

    }
}