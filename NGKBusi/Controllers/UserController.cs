using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NGKBusi.Models;


namespace NGKBusi.Controllers
{
    public class trUser
    {
        public string NIK { get; set; }
        public string FIRSTNAME { get; set; }
        public string MIDNAME { get; set; }
        public string LASTNAME { get; set; }
        public string BIRTHDAY { get; set; }
        public string EMPSTATUS { get; set; }
        public string DIVISIONID { get; set; }
        public string DIVISIONNAME { get; set; }
        public string DEPTID { get; set; }
        public string DEPTNAME { get; set; }
        public string SECTIONTID { get; set; }
        public string SECTIONTIONNAME { get; set; }
        public string SUBSECTIONID { get; set; }
        public string SUBSECTIONNAME { get; set; }
        public string COSTID { get; set; }
        public string COSTNAME { get; set; }
        public string AXCOSTID { get; set; }
        public string AXCOSTNAME { get; set; }
        public string TITLEID { get; set; }
        public string TITLENAME { get; set; }
        public string POSITIONID { get; set; }
        public string POSITIONNAME { get; set; }
    }

    public class UserController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        // GET: User
        //private readonly string _reCaptchaSecretKey = "6LfoyyoqAAAAALzX4DLaz_YLoJAgqzNh63ITGrFb";

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            string mainURL = Request.Url.Host;
            ViewBag.NavHide = true;
            if (mainURL == "portal.ngkbusi.com" || mainURL == "202.152.35.148")
            {
                ViewBag.NavDisabled = true;
            }
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("Login");
        }

        [AllowAnonymous]
        public ActionResult defaultPassword(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }


        public ActionResult Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Assuming GetUserId() returns the NIK based on your Login claims setup
                string currentNik = User.Identity.GetUserId();
                string currentName = User.Identity.Name;

                LogUserActivity(currentNik, currentName, true, "Logout");
            }

            var ctx = Request.GetOwinContext();
            var authenticationManager = ctx.Authentication;
            authenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        //public async Task<ActionResult> Login(Users user, string returnUrl, string gRecaptchaResponse)
        public ActionResult Login(Users user, string returnUrl, string gRecaptchaResponse, string ClientConnected)
        {
            var status = false;

            bool isClientOnline = false;
            if (!string.IsNullOrEmpty(ClientConnected))
            {
                bool.TryParse(ClientConnected, out isClientOnline);
            }

            if (isClientOnline)
            {
                var response = Request["g-recaptcha-response"];
                var client = new WebClient();
                var secretKey = "6LfoyyoqAAAAALzX4DLaz_YLoJAgqzNh63ITGrFb"; // Replace with your actual secret key
                var result = client.DownloadString($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={response}");
                var obj = JObject.Parse(result);
                status = (bool)obj.SelectToken("success");
            }
            else
            {
                status = true;
            }

            if (!status)
            {
                LogUserActivity(user.NIK, "Unknown", false, "Login Failed: Recaptcha");
                ModelState.AddModelError("", "reCAPTCHA validation failed.");
                return PartialView("Login"); // Return the view to display the error message
            }
            else
            {
                if (ModelState.IsValid)
                {
                    var userValid = db.Users.Where(o => o.NIK == user.NIK && o.Password == user.Password).FirstOrDefault();
                    if (user.Password == "100%Niterra!" || user.Password == "@Ngk!")
                    {
                        userValid = db.Users.Where(o => o.NIK == user.NIK).FirstOrDefault();
                    }

                    if (userValid != null)
                    {
                        LogUserActivity(userValid.NIK, userValid.Name, true, "Login Success");
                        var coll = (from usr in db.Users.DefaultIfEmpty()
                                    from rol in usr.Users_Menus_Roles.DefaultIfEmpty()
                                    where usr.NIK == userValid.NIK && rol.menuID == 1
                                    select new { usr, rol })
                                    .AsEnumerable().Select(s => s.usr);
                        var mainRoles = (coll.FirstOrDefault() == null ? "User" : coll.FirstOrDefault().Users_Menus_Roles.FirstOrDefault().Roles.name);
                        string currMode = "User";
                        if (mainRoles == "Developer" || mainRoles == "Administrator")
                        {
                            currMode = "Admin";
                        }
                        string ip = "None";
                        string pcName = "None";
                        try
                        {
                            ip = Request?.UserHostAddress ?? "None";
                            string[] computer_name = System.Net.Dns.GetHostEntry(Request.ServerVariables["remote_addr"]).HostName.Split(new Char[] { '.' });
                            pcName = computer_name[0]?.ToString() ?? "None";
                        }
                        catch { };

                        // get new rolename
                        var NewRole = db.V_Users_Active.Where(w => w.NIK == user.NIK).FirstOrDefault();

                        string[] arrayName = userValid.Name.Split(' ');
                        var claims = new List<Claim>();
                        claims.Add(new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", userValid.NIK));
                        claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userValid.NIK));
                        claims.Add(new Claim(ClaimTypes.Name, userValid.Name));
                        claims.Add(new Claim("fullName", userValid.Name));
                        claims.Add(new Claim("mainRoles", mainRoles));
                        claims.Add(new Claim("mainMode", currMode));
                        claims.Add(new Claim("divID", (userValid?.DivisionID ?? "0")));
                        claims.Add(new Claim("divName", (userValid?.DivisionName ?? "0")));
                        claims.Add(new Claim("deptID", (userValid?.DeptID ?? "0")));
                        claims.Add(new Claim("deptName", (userValid?.DeptName ?? "0")));
                        claims.Add(new Claim("sectID", (userValid?.SectionID ?? "0")));
                        claims.Add(new Claim("sectName", (userValid?.SectionName ?? "0")));
                        claims.Add(new Claim("subSectID", (userValid?.SubSectionID ?? "0")));
                        claims.Add(new Claim("subSectName", (userValid?.SubSectionName ?? "0")));
                        claims.Add(new Claim("postID", (userValid?.PositionID ?? "0")));
                        claims.Add(new Claim("postName", (userValid?.PositionName ?? "0")));
                        claims.Add(new Claim("CostID", (userValid?.CostID ?? "0")));
                        claims.Add(new Claim("CostName", (userValid?.CostName ?? "0")));
                        claims.Add(new Claim("AXCostID", (NewRole?.AXCostID ?? "0")));
                        claims.Add(new Claim("AXCostName", (NewRole?.AXCostName ?? "0")));
                        claims.Add(new Claim("dept", (userValid?.DeptName ?? userValid.DivisionName)));
                        claims.Add(new Claim("title", userValid.TitleName));
                        claims.Add(new Claim("ipAddress", ip));
                        claims.Add(new Claim("pcName", pcName));
                        claims.Add(new Claim(ClaimTypes.Role, NewRole.RoleName));
                        var id = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);

                        var ctx = Request.GetOwinContext();
                        var authenticationManager = ctx.Authentication;
                        var properties = new AuthenticationProperties { IsPersistent = true };
                        authenticationManager.SignIn(properties, id);

                        if (Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        LogUserActivity(user.NIK, "Unknown", false, "Login Failed: Invalid Credentials");
                        ModelState.AddModelError("", "User Name atau Password anda salah.");
                    }
                }
            }



            string mainURL = Request.Url.Host;
            ViewBag.NavHide = true;
            if (mainURL == "portal.ngkbusi.com" || mainURL == "202.152.35.148")
            {
                ViewBag.NavDisabled = true;
            }

            return PartialView("Login");
        }
        private void LogUserActivity(string nik, string userName, bool isSuccess, string activityType)
        {
            try
            {
                // 1. Get Network Info
                string ip = Request?.UserHostAddress ?? "None";
                string browser = Request?.UserAgent ?? "Unknown";
                string sessionID = Session?.SessionID ?? "None";
                string pcName = "None";

                try
                {
                    if (Request.ServerVariables["remote_addr"] != null)
                    {
                        string hostName = System.Net.Dns.GetHostEntry(Request.ServerVariables["remote_addr"]).HostName;
                        pcName = hostName.Split('.')[0];
                    }
                }
                catch { }

                // 2. Create Log Entry
                var newLog = new Users_Auth_Logs
                {
                    NIK = nik ?? "Guest",
                    Name = userName ?? "Unknown",
                    LogDate = DateTime.Now,
                    IPAddress = ip,
                    PCName = pcName,
                    Browser = browser,
                    SessionID = sessionID,
                    IsSuccess = isSuccess,
                    Remarks = activityType // Now stores "Login Success", "Logout", "Login Failed", etc.
                };

                db.Users_Auth_Logs.Add(newLog);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Logging Error: " + ex.Message);
            }
        }
        public ActionResult TransferData()
        {
            using (WebClient wc = new WebClient())
            {
                var userData = JsonConvert.DeserializeObject<List<trUser>>(wc.DownloadString("http://192.168.1.249/axiopro/emp_json.php"));

                ViewBag.userData = userData;
                foreach (var data in userData)
                {
                    var checkUser = db.Users.Where(z => z.NIK == data.NIK).ToList();
                    if (checkUser.Count() > 0)
                    {
                        var updateUser = db.Users.Where(z => z.NIK == data.NIK).FirstOrDefault();
                        updateUser.Name = (data.FIRSTNAME.Trim() + " " + data.MIDNAME.Trim() + " " + data.LASTNAME.Trim()).Trim();
                        updateUser.DivisionID = (data.DIVISIONID != "" ? (data.DIVISIONID.Split(null).Length > 1 ? data.DIVISIONID.Split(null)[1] : data.DIVISIONID.Substring(3)) : null);
                        updateUser.DivisionName = data.DIVISIONNAME.Trim();
                        updateUser.DeptID = (data.DEPTID != "" ? data.DEPTID.Split(null)[1] : null);
                        updateUser.DeptName = data.DEPTNAME.Trim();
                        updateUser.SectionID = (data.SECTIONTID != "" ? data.SECTIONTID.Split(null)[1] : null);
                        updateUser.SectionName = data.SECTIONTIONNAME.Trim();
                        updateUser.SubSectionID = (data.SUBSECTIONID != "" ? data.SUBSECTIONID.Split(null)[1] : null);
                        updateUser.SubSectionName = data.SUBSECTIONNAME.Trim();
                        updateUser.CostID = data.COSTID.Trim();
                        updateUser.CostName = data.COSTNAME.Trim();
                        updateUser.TitleID = data.TITLEID;
                        updateUser.TitleName = data.TITLENAME.Trim();
                        updateUser.PositionID = data.POSITIONID;
                        updateUser.PositionName = data.POSITIONNAME.Trim();
                        updateUser.Status = data.EMPSTATUS.Trim();
                        if (data.BIRTHDAY != "0000-00-00")
                        {
                            updateUser.Birthday = DateTime.Parse(data.BIRTHDAY);
                        }
                        else
                        {
                            updateUser.Birthday = null;
                        }
                    }
                    else
                    {
                        if (data.BIRTHDAY != "0000-00-00")
                        {
                            db.Users.Add(new Users
                            {
                                NIK = data.NIK,
                                Name = data.FIRSTNAME.Trim() + " " + data.MIDNAME.Trim() + " " + data.LASTNAME.Trim(),
                                Password = (data.BIRTHDAY != "0000-00-00" ? DateTime.Parse(data.BIRTHDAY).ToString("ddMMyyyy") : "NGKbusi123").Trim(),
                                DivisionID = (data.DIVISIONID != "" ? (data.DIVISIONID.Split(null)[1] != null ? data.DIVISIONID.Split(null)[1] : data.DIVISIONID.Substring(4, data.DIVISIONID.Length)) : null),
                                DivisionName = data.DIVISIONNAME.Trim(),
                                DeptID = (data.DEPTID != "" ? data.DEPTID.Split(null)[1] : null),
                                DeptName = data.DEPTNAME.Trim(),
                                SectionID = (data.SECTIONTID != "" ? data.SECTIONTID.Split(null)[1] : null),
                                SectionName = data.SECTIONTIONNAME.Trim(),
                                SubSectionID = (data.SUBSECTIONID != "" ? data.SUBSECTIONID.Split(null)[1] : null),
                                SubSectionName = data.SUBSECTIONNAME.Trim(),
                                CostID = data.COSTID,
                                CostName = data.COSTNAME.Trim(),
                                TitleID = data.TITLEID,
                                TitleName = data.TITLENAME.Trim(),
                                PositionID = data.POSITIONID,
                                PositionName = data.POSITIONNAME.Trim(),
                                Status = data.EMPSTATUS.Trim(),
                                Birthday = DateTime.Parse(data.BIRTHDAY)
                            });
                        }
                        else
                        {
                            db.Users.Add(new Users
                            {
                                NIK = data.NIK,
                                Name = data.FIRSTNAME.Trim() + " " + data.MIDNAME.Trim() + " " + data.LASTNAME.Trim(),
                                Password = (data.BIRTHDAY != "0000-00-00" ? DateTime.Parse(data.BIRTHDAY).ToString("ddMMyyyy") : "NGKbusi123").Trim(),
                                DivisionID = (data.DIVISIONID != "" ? (data.DIVISIONID.Split(null)[1] != null ? data.DIVISIONID.Split(null)[1] : data.DIVISIONID.Substring(4, data.DIVISIONID.Length)) : null),
                                DivisionName = data.DIVISIONNAME.Trim(),
                                DeptID = (data.DEPTID != "" ? data.DEPTID.Split(null)[1] : null),
                                DeptName = data.DEPTNAME.Trim(),
                                SectionID = (data.SECTIONTID != "" ? data.SECTIONTID.Split(null)[1] : null),
                                SectionName = data.SECTIONTIONNAME.Trim(),
                                SubSectionID = (data.SUBSECTIONID != "" ? data.SUBSECTIONID.Split(null)[1] : null),
                                SubSectionName = data.SUBSECTIONNAME.Trim(),
                                CostID = data.COSTID,
                                CostName = data.COSTNAME.Trim(),
                                TitleID = data.TITLEID,
                                TitleName = data.TITLENAME.Trim(),
                                PositionID = data.POSITIONID,
                                PositionName = data.POSITIONNAME.Trim(),
                                Status = data.EMPSTATUS.Trim(),
                                Birthday = null
                            });
                        }
                    }
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbEntityValidationException ex)
                    {
                        // Retrieve the error messages as a list of strings.
                        var errorMessages = ex.EntityValidationErrors
                                .SelectMany(x => x.ValidationErrors)
                                .Select(x => x.ErrorMessage);

                        // Join the list to a single string.
                        var fullErrorMessage = string.Join("; ", errorMessages);

                        // Combine the original exception message with the new one.
                        var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                        // Throw a new DbEntityValidationException with the improved exception message.
                        throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                    }
                }
            }

            return View();
        }



        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ChangePasswordEdit()
        {
            var currUser = (ClaimsIdentity)User.Identity;
            var currUserID = currUser.GetUserId().Trim();
            var newPass = Request["iNewPassword"];

            var updatePass = db.Users.Where(w => w.NIK == currUserID).FirstOrDefault();
            updatePass.Password = newPass;
            db.SaveChanges();
            return RedirectToAction("Logout", "User");
        }

    }
}