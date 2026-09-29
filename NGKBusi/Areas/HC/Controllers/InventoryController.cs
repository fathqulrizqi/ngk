using ClosedXML.Excel;
using Microsoft.AspNet.Identity;
using NGKBusi.Areas.HC.Models;
using NGKBusi.Areas.PE.Models;
using NGKBusi.Areas.SCM.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;


namespace NGKBusi.Areas.HC.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        GAConnection dbm = new GAConnection();
        public ActionResult Inventory()
        {
            // 1. Mengambil data user login
            var _currUser = (ClaimsIdentity)User.Identity;
            string userNik = _currUser.GetUserId();

            // Mengambil NIK, Name, dan Role dari session
            string costID = _currUser.FindFirstValue("AXCostID");
            string costName = _currUser.FindFirstValue("AXCostName");
            string divisionName = _currUser.FindFirstValue("divName");
            string deptName = _currUser.FindFirstValue("CostName");
            string sectionName = _currUser.FindFirstValue("sectName");
            string username = _currUser.FindFirstValue("fullName");

            var userCostNames = dbm.HC_GA_Inventory_UserDept
        .Where(u => u.NIK == userNik)
        .Join(
            dbm.HC_GA_Inventory_Dept,
            ud => ud.Dept_ID,
            d => d.ID,
            (ud, d) => d.Cost_Name
        )
        .ToList();

            ViewBag.UserCostNames = userCostNames;

            var userCostName = dbm.HC_GA_Inventory_UserDept
    .Where(u => u.NIK == userNik)
    .Join(
        dbm.HC_GA_Inventory_Dept,
        ud => ud.Dept_ID,
        d => d.ID,
        (ud, d) => new { d.ID, d.Cost_Name }
    )
    .ToList();

            var selectedDept = userCostName.FirstOrDefault(); // ambil dept pertama

            if (selectedDept != null)
            {
                // 🔍 Ambil semua user di dept yang sama
                var deptUsers = dbm.HC_GA_Inventory_UserDept
                    .Where(u => u.Dept_ID == selectedDept.ID)
                    .Select(u => new { u.Name, u.Role })
                    .ToList();

                // 🧩 Looping semua role dan assign ke ViewBag sesuai role-nya
                foreach (var user in deptUsers)
                {
                    switch (user.Role?.ToUpper())
                    {
                        case "ADMIN":
                            ViewBag.PreparedName = user.Name;
                            break;
                        case "CHECKER":
                            ViewBag.CheckerName = user.Name;
                            break;
                        case "APPROVER":
                            ViewBag.ApproverName = user.Name;
                            break;
                    }
                }
            }

            // Menentukan tahun fiskal (FY)
            string FY = "FY1" + (DateTime.Now.Month >= 4
                                ? (int.Parse(DateTime.Now.ToString("yy")) + 1).ToString()
                                : DateTime.Now.ToString("yy"));

            // 2. Mencocokkan user ke database HC_GA_Inventory_UserDept untuk mengambil semua Dept_ID
            var userDepts = dbm.HC_GA_Inventory_UserDept
                .Where(x => x.NIK == userNik)
                .Select(x => x.Dept_ID)
                .ToList();

            // 3. Mengambil AX_Cost_ID dari tabel HC_GA_Inventory_Dept
            // dengan mencocokkan Dept_ID yang dimiliki user
            var axCostIDs = dbm.HC_GA_Inventory_Dept
          .Where(d => userDepts.Contains(d.ID))
          .Select(d => d.AX_Cost_ID.Trim().ToUpper())
          .ToList();
            System.Diagnostics.Debug.WriteLine($"LOGIN USER ID: {userNik}");
            System.Diagnostics.Debug.WriteLine($"CLAIM NIK: {_currUser.FindFirstValue("NIK")}");

            // 4. Mengambil budget list dengan mencocokkan AX_Cost_ID ke Section_To_Code
            //ViewBag.Budget = db.V_FA_BudgetSystem_BEX_BEL
            //    .Where(w => w.Period_FY == FY
            //                && w.COA_Code == "7691000"
            //                && axCostIDs.Contains(w.Section_To_Code)
            //                && w.Final_Version == 1
            //                && w.Latest == 1)
            //    .ToList();
            // 4. Ambil budget list, khusus ALF/AN ambil dua COA (7691000 & 7781101)
            var coaCodes = new List<string> { "7691000", "7515409" }; // default

            if (axCostIDs.Contains("B5100") || axCostIDs.Contains("B5101") || axCostIDs.Contains("B4410") || axCostIDs.Contains("B4130") || axCostIDs.Contains("B4430") || axCostIDs.Contains("B4440"))
            {
                coaCodes.Add("7781101");
                coaCodes.Add("7515409");
            }

            if (axCostIDs.Contains("B4110") || axCostIDs.Contains("B4120"))
            {
                coaCodes.Add("7711102");
            }

            if (axCostIDs.Contains("B5130") && userNik.Contains("845.01.20"))
            {
                axCostIDs.Add("B5710");
            }

            foreach (var id in axCostIDs)
            {
                System.Diagnostics.Debug.WriteLine($"AX COST ID: '{id}'");
            }

            var sampleBudget = db.V_FA_BudgetSystem_BEX_BEL
                .Where(w => w.Section_To_Code == "B4110" && w.Period_FY == FY)
                .FirstOrDefault();
            ViewBag.sampleBudget = sampleBudget;
            System.Diagnostics.Debug.WriteLine($"Budget Section_To_Code: '{sampleBudget}'");

            // B2220 = HUMAN RESOURCE, B2210 = GENERAL AFFAIR
            bool isHRorGA = axCostIDs.Contains("B2220") || axCostIDs.Contains("B2210") ||
                            userDepts.Contains(90) || userDepts.Contains(91) ||
                            userDepts.Contains(114) || userDepts.Contains(115);

            var allowedCoa = new List<string> { "7691000", "7781101", "7515409", "7711102" };
            var allowedCoaGA = new List<string> { "7691000", "7781101", "7711102", "7641103", "7515409", "8291203" };

            System.Diagnostics.Debug.WriteLine("=== AX COST IDS ===");
            foreach (var id in axCostIDs)
            {
                System.Diagnostics.Debug.WriteLine(id);
            }

            if (isHRorGA)
            {
                // HR & GA → Bisa buka SEMUA dept (tidak ada filter Section_To_Code)
                ViewBag.Budget = db.V_FA_BudgetSystem_BEX_BEL
                    .Where(w => w.Period_FY == FY
                                && allowedCoaGA.Contains(w.COA_Code)
                                && w.Final_Version == 1
                                && w.Latest == 1)
                    .ToList();
            }
            else
            {
                // User biasa → Filter budget strictly berdasarkan dept/section mereka
                ViewBag.Budget = db.V_FA_BudgetSystem_BEX_BEL
                    .Where(w => w.Period_FY == FY
                                && allowedCoa.Contains(w.COA_Code)
                                && (axCostIDs.Contains(w.Section_To_Code.Trim().ToUpper())
                                    || axCostIDs.Contains(w.Section_From_Code.Trim().ToUpper()))
                                && w.Final_Version == 1
                                && w.Latest == 1)
                    .ToList();
            }
            var nikDebug = dbm.HC_GA_Inventory_UserDept
    .Select(x => x.NIK)
    .ToList();

            System.Diagnostics.Debug.WriteLine("=== ALL USERDEPT NIK ===");
            foreach (var n in nikDebug)
            {
                System.Diagnostics.Debug.WriteLine($"'{n}'");
            }

            // Mengatur data untuk ViewBag
            ViewBag.DivName = divisionName;
            ViewBag.SecName = sectionName;
            ViewBag.CostID = costID;
            ViewBag.DeptName = deptName;
            ViewBag.UserName = username;

            ViewBag.CategoryList = dbm.HC_GA_Inventory_Category.ToList();
            ViewBag.SubcategoryList = dbm.HC_GA_Inventory_Subcategory.ToList();
            ViewBag.ItemList = dbm.HC_GA_Inventory_Item_List.ToList();
            ViewBag.NavHide = true;
            ViewBag.UserNik = userNik;

            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);

            if (currentUser != null)
            {
                ViewBag.UserName = currentUser.Name;
            }
            else
            {
                ViewBag.UserName = "";
            }

            return View();
        }

        public ActionResult _CreateOrder()
        {
            ViewBag.NavHide = true;
            ViewBag.CategoryList = dbm.HC_GA_Inventory_Category.ToList();
            string userNik = User.Identity.GetUserId();

            ViewBag.UserNik = userNik;

            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);

            if (currentUser != null)
            {
                ViewBag.UserName = currentUser.Name;
                ViewBag.DeptName = currentUser.CostName;
            }
            else
            {
                ViewBag.UserName = "";
                ViewBag.DeptName = "";
            }
            return View();
        }
        public ActionResult _DetailOrder()
        {
            var _currUser = ((ClaimsIdentity)User.Identity);
            string userNik = _currUser.GetUserId();
            string deptName = _currUser.FindFirstValue("deptName");
            string username = _currUser.FindFirstValue("fullName");

            ViewBag.UserName = username;
            ViewBag.UserNik = userNik;

            return View();
        }
        public ActionResult _ListOrder()
        {
            ViewBag.CategoryList = dbm.HC_GA_Inventory_Category.ToList();

            return View();
        }
        public ActionResult _NoAccess()
        {
            ViewBag.NavHide = false;
            var _currUser = ((ClaimsIdentity)User.Identity);
            string deptName = _currUser.FindFirstValue("CostName");
            ViewBag.DeptName = deptName;


            return View();
        }

        public ActionResult _ItemListTable()
        {
            ViewBag.NavHide = true;

            string userNik = User.Identity.GetUserId();
            string username = User.Identity.GetUserName();
            ViewBag.UserNik = userNik;
            var _currUser = ((ClaimsIdentity)User.Identity);
            string deptName = _currUser.FindFirstValue("CostName");
            ViewBag.DeptName = deptName;
            // ambil data user aktif
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
            if (currentUser == null)
                return RedirectToAction("_NoAccess");

            // daftar departemen yang boleh akses
            var allowedDepartments = new List<string>
    {
        "GENERAL AFFAIR",
        "HUMAN RESOURCE",
        "KOPERASI",
    };

            // kalau bukan departemen yang diizinkan → redirect
            if (!allowedDepartments.Contains(currentUser.CostName?.ToUpper()))
                return PartialView("~/Areas/HC/Views/Inventory/Partial/_NoAccess.cshtml");


            // kalau lolos, lanjut isi data view
            ViewBag.UserName = currentUser.Name;
            ViewBag.DeptName = currentUser.CostName;

            ViewBag.CategoryList = dbm.HC_GA_Inventory_Category.ToList();
            ViewBag.SubcategoryList = dbm.HC_GA_Inventory_Subcategory.ToList();

            return View();
        }

        public ActionResult _ItemRequest()
        {
            ViewBag.NavHide = true;
            string userNik = User.Identity.GetUserId();
            var username = User.Identity.GetUserName();
            ViewBag.Now = DateTime.Now.ToString("dd-MM-yyyy");


            ViewBag.UserNik = userNik;
            ViewBag.username = username;
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);

            if (currentUser != null)
            {
                ViewBag.UserName = currentUser.Name;
                ViewBag.DeptName = currentUser.CostName;
            }
            else
            {
                ViewBag.UserName = "";
                ViewBag.DeptName = "";
            }
            ViewBag.CategoryList = dbm.HC_GA_Inventory_Category.ToList();

            return View();
        }

        public ActionResult _ItemRequest2()
        {
            ViewBag.NavHide = false;
            var _currUser = ((ClaimsIdentity)User.Identity);
            string deptName = _currUser.FindFirstValue("deptName");

            var allowedDept = new List<string> { "HRD & GA", "KOPERASI" };

            if (!allowedDept.Contains(deptName))
            {
                ViewBag.DeptName = deptName;
                ViewBag.NavHide = false;
                return PartialView("~/Areas/HC/Views/Inventory/Partial/_NoAccess.cshtml");
            }

            string userNik = User.Identity.GetUserId();
            var username = User.Identity.GetUserName();
            ViewBag.Now = DateTime.Now.ToString("dd-MM-yyyy");

            ViewBag.UserNik = userNik;
            ViewBag.username = username;

            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);

            if (currentUser != null)
            {
                ViewBag.UserName = currentUser.Name;
                ViewBag.DeptName = currentUser.CostName;
            }
            else
            {
                ViewBag.UserName = "";
                ViewBag.DeptName = "";
            }

            ViewBag.CategoryList = dbm.HC_GA_Inventory_Category.ToList();

            // === BUDGET LOGIC - GA FULL ACCESS ===
            string FY = "FY1" + (DateTime.Now.Month >= 4
                                ? (int.Parse(DateTime.Now.ToString("yy")) + 1).ToString()
                                : DateTime.Now.ToString("yy"));

            var allowedCoaGA = new List<string> { "7691000", "7781101", "7711102", "7641103", "7515409", "8291203" };

            ViewBag.Budget = db.V_FA_BudgetSystem_BEX_BEL
                .Where(w => w.Period_FY == FY
                            && allowedCoaGA.Contains(w.COA_Code)
                            && w.Final_Version == 1
                            && w.Latest == 1)
                .ToList();
            // ================================

            return PartialView("_ItemRequest2");
        }

        public ActionResult _Sample()
        {
            return View();
        }
        public JsonResult GetItemDatas()
        {
            // Step 1: Get all "Approved" requests and their cart numbers.
            var approvedRequests = (from req in dbm.HC_GA_Inventory_Requests
                                    where req.Status == "Approved"
                                    select req).ToList();

            var approvedCartItems = new List<dynamic>();

            // Step 2: Iterate through each request to find the corresponding cart items.
            foreach (var request in approvedRequests)
            {
                var cartNos = request.Cart_No.Split(',').ToList();
                var carts = from cart in dbm.HC_GA_Inventory_Carts
                            where cartNos.Contains(cart.Cart_No)
                            select new
                            {
                                cart.Item_Id,
                                cart.Qty_Request,
                                request.Request_No
                            };
                approvedCartItems.AddRange(carts);
            }

            // Step 3: Group the results by Item_Id to calculate total order quantity.
            var groupedApprovedRequests = approvedCartItems.GroupBy(r => r.Item_Id)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalQty = g.Sum(x => x.Qty_Request),
                    RequestNos = string.Join(", ", g.Select(x => x.Request_No).Distinct())
                });

            // Step 4: Join the item list with the calculated order quantities.
            var rawData = (from item in dbm.HC_GA_Inventory_Item_List
                           join category in dbm.HC_GA_Inventory_Category
                           on item.Category_Id equals category.ID into categoryGroup
                           from cat in categoryGroup.DefaultIfEmpty()
                           join subcategory in dbm.HC_GA_Inventory_Subcategory
                           on item.Subcategory_Id equals subcategory.ID into subcategoryGroup
                           from subcat in subcategoryGroup.DefaultIfEmpty()
                           select new
                           {
                               item.ID,
                               item.Item_Name,
                               item.Location,
                               item.Category_Id,
                               item.Subcategory_Id,
                               CategoryName = cat != null ? cat.Category_Name : "undefined",
                               SubcategoryName = subcat != null ? subcat.Subcategory_Name : "undefined",
                               item.Description,
                               item.Qty,
                               item.Price,
                               item.Ref_Code,
                               item.Unit,
                               item.Last_Price_Update,
                               item.Image
                           }).AsEnumerable()
                           .Select(item => new
                           {
                               item.ID,
                               item.Item_Name,
                               item.Location,
                               item.Category_Id,
                               item.Subcategory_Id,
                               item.CategoryName,
                               item.SubcategoryName,
                               item.Description,
                               item.Qty,
                               item.Price,
                               item.Ref_Code,
                               item.Unit,
                               lastPriceUpdate = item.Last_Price_Update,
                               item.Image,

                               Qty_Order = groupedApprovedRequests.ContainsKey(item.ID) ? groupedApprovedRequests[item.ID].TotalQty : 0,
                               Qty_Preview = (groupedApprovedRequests.ContainsKey(item.ID) ? groupedApprovedRequests[item.ID].TotalQty : 0) - item.Qty,
                               Request_No = groupedApprovedRequests.ContainsKey(item.ID) ? groupedApprovedRequests[item.ID].RequestNos : null
                           })
                           .ToList();

            var lastPriceUpdate = rawData.FirstOrDefault()?.lastPriceUpdate;

            return Json(new { datas = rawData, lastPriceUpdate = lastPriceUpdate }, JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetSubcategoryDatas()
        {
            // Ambil data subkategori dan join dengan kategori untuk mendapatkan nama kategori
            var rawSubcategoryData = (from subcat in dbm.HC_GA_Inventory_Subcategory
                                      join cat in dbm.HC_GA_Inventory_Category
                                      on subcat.Category_Id equals cat.ID into categoryGroup
                                      from category in categoryGroup.DefaultIfEmpty() // Gunakan DefaultIfEmpty untuk LEFT JOIN
                                      select new
                                      {
                                          subcat.ID,
                                          subcat.Subcategory_Name,
                                          subcat.Category_Id,
                                          subcat.Subcategory_Image, // Ambil properti gambar
                                          CategoryName = category != null ? category.Category_Name : "Undefined Category" // Ambil nama kategori
                                      })
                                      .ToList() // Tarik data ke memori
                                      .Select(s => new // Kemudian terapkan null-coalescing di memori
                                      {
                                          s.ID,
                                          s.Subcategory_Name,
                                          s.Category_Id,
                                          s.CategoryName,
                                          Subcategory_Image = s.Subcategory_Image ?? "" // Pastikan ini tidak null
                                      })
                                      .ToList();

            return Json(rawSubcategoryData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemByCategory()
        {
            var result = dbm.HC_GA_Inventory_Category
                .Select(cat => new
                {
                    CategoryId = cat.ID,
                    CategoryName = cat.Category_Name,
                    Items = dbm.HC_GA_Inventory_Item_List
                        .Where(item => item.Category_Id == cat.ID)
                        .Select(item => new
                        {
                            item.ID,
                            item.Item_Name,
                            item.Location,
                            item.Description,
                            item.Qty,
                            item.Unit,
                            item.Image
                        }).ToList()
                }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllInventoryData()
        {
            var data = dbm.HC_GA_Inventory_Category
                .Select(cat => new
                {
                    cat.ID,
                    CategoryName = cat.Category_Name,
                    Subcategories = dbm.HC_GA_Inventory_Subcategory
                        .Where(sc => sc.Category_Id == cat.ID)
                        .Select(sc => new
                        {
                            sc.ID,
                            SubcategoryName = sc.Subcategory_Name
                        })
                        .ToList()
                })
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetItemBySubcategory(int subcategoryId)
        {
            // Melakukan query ke tabel HC_GA_Inventory_Items
            var items = dbm.HC_GA_Inventory_Item_List
                .Where(x => x.Subcategory_Id == subcategoryId)
                .Select(x => new
                {
                    x.ID,
                    Item_Name = x.Item_Name != null ? x.Item_Name.ToUpper() : null,
                    x.Location,
                    x.Description,
                    x.Qty,
                    x.Unit,
                    x.Category_Id,
                    x.Subcategory_Id,
                    x.Image,
                    x.Ref_Code,
                    // x.User_NIK,
                    // x.Timestamps
                })
                .ToList();

            return Json(items, JsonRequestBehavior.AllowGet);
        }


        public JsonResult GetItemById(int id)
        {
            var item = dbm.HC_GA_Inventory_Item_List
                .Where(x => x.ID == id)
                .Select(x => new
                {
                    x.ID,
                    Item_Name = x.Item_Name != null ? x.Item_Name.ToUpper() : null,
                    x.Location,
                    x.Description,
                    x.Qty,
                    x.Unit,
                    x.Category_Id,
                    x.Price,
                    x.Subcategory_Id,
                    x.Image
                })
                .FirstOrDefault();

            return Json(item, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCartbyUser()
        {
            var currUserNik = User.Identity.GetUserId();

            var carts = (from cart in dbm.HC_GA_Inventory_Carts
                         join item in dbm.HC_GA_Inventory_Item_List on cart.Item_Id equals item.ID
                         where cart.User_NIK == currUserNik && cart.Status == 0
                         select new
                         {
                             cart.ID,
                             cart.Cart_No,
                             cart.Item_Name,
                             cart.Item_Id,
                             item.Image,
                             Qty = cart.Qty_Request + " " + cart.Unit_Request,
                             cart.Budget_No,
                             cart.Budget_Desc,
                             cart.Qty_Request,
                             requestNo = cart.Request_No,
                             cart.Status,
                             Budget = cart.Budget_No + " | " + cart.Budget_Desc,
                             cart.Notes,
                             cart.Section_To_Name,
                             cart.Section_To_Code,
                             item.Price,
                             item.Category_Id,
                             Subcategory_Id = item.Subcategory_Id
                         }).ToList();

            if (!carts.Any())
            {
                return Json(new { carts, budgets = new List<object>(), totalFY2 = 0 }, JsonRequestBehavior.AllowGet);
            }

            var distinctSections = carts.Select(c => new { c.Section_To_Code, c.Section_To_Name }).Distinct().ToList();
            var budgetNos = carts.Select(c => c.Budget_No).Distinct().ToList();

            string FY = "FY1" + (DateTime.Now.Month >= 4
                ? (int.Parse(DateTime.Now.ToString("yy")) + 1).ToString()
                : DateTime.Now.ToString("yy"));

            var sectionCodesInCart = distinctSections.Select(s => s.Section_To_Code).ToList();


            var budgetData = db.FA_BudgetSystem_BEX
                .Where(x => sectionCodesInCart.Contains(x.Section_To_Code) &&
                            budgetNos.Contains(x.Budget_No) &&
                            x.Final_Version == 1 &&
                            x.Period_FY == FY)
                .ToList();


            var purchasedUsages = dbm.HC_GA_Inventory_Carts
        .Where(cart => budgetNos.Contains(cart.Budget_No) && cart.Status != 0)
        .GroupBy(cart => cart.Budget_No)
        .Select(g => new
        {
            Budget_No = g.Key,
            TotalUsage = g.Sum(c => (c.Price ?? 0) * (c.Qty_Acc ?? c.Qty_Request))
        }).ToDictionary(k => k.Budget_No, v => v.TotalUsage);
            
            var budgetsResult = budgetData
                .GroupBy(x => new { x.Budget_No, x.Section_To_Code })
                .Select(g => new
                {
                    budgetNo = g.Key.Budget_No,
                    sectionCode = g.Key.Section_To_Code,
                    budgetDesc = g.FirstOrDefault()?.Description,
                    sectionToName = g.FirstOrDefault()?.Section_To_Name,
                    totalFY2 = g.Sum(x => (decimal?)x.TotalFY2) ?? 0,
                    purchasedUsage = purchasedUsages.ContainsKey(g.Key.Budget_No) ? purchasedUsages[g.Key.Budget_No] : 0
                }).ToList();

            var workflowParticipants = dbm.HC_GA_Inventory_Dept
                .Where(d => sectionCodesInCart.Contains(d.AX_Cost_ID))
                .Select(d => new
                {
                    SectionCode = d.AX_Cost_ID,
                    Participants = dbm.HC_GA_Inventory_UserDept
                        .Where(u => u.Dept_ID == d.ID)
                        .Select(u => new { u.NIK, u.Name, u.Role })
                        .ToList()
                }).ToList();

            var primarySection = distinctSections.FirstOrDefault()?.Section_To_Code;
            var primaryWorkflow = workflowParticipants.FirstOrDefault(w => w.SectionCode == primarySection)?.Participants;

            var requestNo = carts.FirstOrDefault(c => !string.IsNullOrEmpty(c.requestNo))?.requestNo;
            var firstCategoryId = carts.FirstOrDefault()?.Category_Id;
            string categoryName = "";

            if (firstCategoryId != null)
            {
                categoryName = dbm.HC_GA_Inventory_Category
                                  .Where(c => c.ID == firstCategoryId)
                                  .Select(c => c.Category_Name)
                                  .FirstOrDefault() ?? "";
            }

            return Json(new
            {
                carts,
                requestNo,
                category = categoryName,
                budgets = budgetsResult,
                workflow = workflowParticipants,
                checker = primaryWorkflow?.FirstOrDefault(u => u.Role == "CHECKER"),
                approver = primaryWorkflow?.FirstOrDefault(u => u.Role == "APPROVER"),
                admin = primaryWorkflow?.FirstOrDefault(u => u.Role == "ADMIN")
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetHistoryByDept(List<string> costNames)
        {

            if (costNames == null || costNames.Count == 0)
            {
                return Json(new { success = true, data = new List<object>() }, JsonRequestBehavior.AllowGet);
            }

            var approvalMap = dbm.HC_GA_Inventory_Requests_Approval
                .GroupBy(a => a.Request_No)
                .Select(g => g.OrderByDescending(x => x.ID).FirstOrDefault())
                .ToDictionary(
                    x => x.Request_No,
                    x => new
                    {
                        x.Prepared_NIK,
                        x.Prepared_Name,
                        x.Prepared_Date,
                        x.Checked_NIK,
                        x.Checked_Name,
                        x.Checked_Date,
                        x.Approved_NIK,
                        x.Approved_Name,
                        x.Approved_Date
                    });

            var allCarts = dbm.HC_GA_Inventory_Carts.ToList();

            var userCostCodes = dbm.HC_GA_Inventory_Dept
                .Where(d => costNames.Contains(d.Cost_Name))
                .ToList()
                .Where(d => !string.IsNullOrWhiteSpace(d.AX_Cost_ID))
                .Select(d => d.AX_Cost_ID)
                .ToList();

            var requestDeptCodes = allCarts
                .Where(c => !string.IsNullOrEmpty(c.Request_No) && !string.IsNullOrEmpty(c.Section_To_Code))
                .GroupBy(c => c.Request_No)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(c => c.Section_To_Code).Distinct().ToList());

            var matchingRequestNos = requestDeptCodes
                .Where(kv => kv.Value.Any(code => userCostCodes.Contains(code)))
                .Select(kv => kv.Key)
                .ToList();

            var requests = dbm.HC_GA_Inventory_Requests
    .Where(r => costNames.Contains(r.Dept_Request) || matchingRequestNos.Contains(r.Request_No))
    .AsEnumerable()
    .Select(r =>
    {
        var cartNos = r.Cart_No?.Split(',') ?? new string[0];

        var relatedCarts = allCarts
            .Where(c => cartNos.Contains(c.Cart_No))
            .Select(c => new
            {
                c.Cart_No,
                c.isReady,
                Pick_Date = c.Pick_Date != null
                    ? c.Pick_Date.Value.ToString("yyyy-MM-dd")
                    : null
            })
            .ToList();

        return new
        {
            r.Request_No,
            r.Status,
            r.Subject,
            r.Receive_Number,
            r.Invoice_Number,
            r.User_NIK,
            r.User_Name,
            Timestamps = r.Timestamps.ToString("yyyy-MM-dd HH:mm"),
            r.Reason_Message,
            r.Dept_Request,
            Due_Date = r.Due_Date.ToString("yyyy-MM-dd"),
            Cart_Nos = cartNos,

            // 🔥 cart data
            Carts = relatedCarts,

            // approval
            Prepared_NIK = approvalMap.ContainsKey(r.Request_No) ? approvalMap[r.Request_No].Prepared_NIK : "",
            Approved_NIK = approvalMap.ContainsKey(r.Request_No) ? approvalMap[r.Request_No].Approved_NIK : "",
            Checked_NIK = approvalMap.ContainsKey(r.Request_No) ? approvalMap[r.Request_No].Checked_NIK : ""
        };
    })
    .ToList();

            return Json(requests, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetUnbCostCenters()
        {
            var data = db.V_Users_Active
                .Where(x => x.AXCostName != null)
                .Select(x => new
                {
                    x.AXCostID,
                    x.AXCostName
                })
                .Distinct()
                .OrderBy(x => x.AXCostName)
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetRequestHistoryDetails(string requestNo)
        {
            if (string.IsNullOrEmpty(requestNo))
                return Json(null, JsonRequestBehavior.AllowGet);

            var request = dbm.HC_GA_Inventory_Requests
                .FirstOrDefault(r => r.Request_No == requestNo);

            if (request == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            var approval = dbm.HC_GA_Inventory_Requests_Approval
                .FirstOrDefault(ra => ra.Request_No == requestNo);

            // Split Cart_No
            var cartNos = request.Cart_No?.Split(',') ?? new string[0];

            // Ambil item list + section
            var items = (from cart in dbm.HC_GA_Inventory_Carts
                         join item in dbm.HC_GA_Inventory_Item_List
                         on cart.Item_Id equals item.ID
                         where cartNos.Contains(cart.Cart_No)
                         select new
                         {
                             cart.ID,
                             cart.Cart_No,
                             cart.Item_Id,
                             Item_Name = cart.Item_Name,

                             Qty = cart.Qty_Request + " " + cart.Unit_Request,

                             cart.Qty_Request,
                             cart.Qty_Acc,
                             cart.Unit_Request,

                             Price = request.Status == "Complete"
                                        ? cart.Price
                                        : cart.Price,

                             Budget = cart.Budget_No == "UNB"
                                        ? cart.Budget_No + " | " + cart.Section_To_Name
                                        : cart.Budget_No + " | " + cart.Budget_Desc,

                             cart.Budget_No,
                             cart.Budget_Desc,
                             cart.Notes,
                             cart.Section_To_Code,
                             cart.Section_To_Name,
                             cart.Section_Name,
                             cart.isReady
                         }).ToList();

            // ========== NEW SECTION: Ambil Dept & Role Users ==========

            // Ambil item pertama (hindari FirstOrDefault berulang)
            var firstItem = items.FirstOrDefault();
            var sectionToCode = firstItem?.Section_To_Code;
            var sectionToName = firstItem?.Section_To_Name;

            var sectionName = firstItem?.Section_Name;

            HC_GA_Inventory_Dept dept = null;

            if (sectionName == "GA") // Sesuaikan kode/nama GA Anda
            {
                // Cari Dept GA di database (ID 10)
                dept = dbm.HC_GA_Inventory_Dept.FirstOrDefault(d => d.ID == 10);
                sectionToCode = "B2210";
            }
            // Validasi tambahan kalau firstItem null
            if (firstItem != null)
            {
                // Khusus UNB → pakai Cost_Name
                if (sectionToCode == "UNB")
                {
                    dept = dbm.HC_GA_Inventory_Dept
                        .FirstOrDefault(d => d.AX_Cost_Name == sectionToName);
                }
                else
                {
                    dept = dbm.HC_GA_Inventory_Dept
                        .FirstOrDefault(d => d.AX_Cost_ID == sectionToCode);
                }
            }

            object checker = null, approverObj = null, admin = null;
            string userNik = request.User_NIK;

            if (dept != null)
            {
                var adminData = dbm.HC_GA_Inventory_UserDept
                    .Where(u => u.Dept_ID == dept.ID && u.NIK == userNik && u.Role == "ADMIN")
                    .Select(u => new { u.NIK, u.Name, u.Route }) 
                    .FirstOrDefault();

                if (adminData != null)
                {
                    admin = new { NIK = adminData.NIK, Name = adminData.Name };

                    checker = dbm.HC_GA_Inventory_UserDept
                        .Where(u => u.Dept_ID == dept.ID && u.Role == "CHECKER" && u.Route == adminData.Route)
                        .Select(u => new { u.NIK, u.Name })
                        .FirstOrDefault();

                    approverObj = dbm.HC_GA_Inventory_UserDept
                        .Where(u => u.Dept_ID == dept.ID && u.Role == "APPROVER" && u.Route == adminData.Route)
                        .Select(u => new { u.NIK, u.Name })
                        .FirstOrDefault();
                }
            }

            var distinctSections = items
                .Select(x => new { x.Section_To_Code, x.Section_To_Name })
                .Distinct()
                .ToList();

            var budgetNos = items
                .Select(x => x.Budget_No)
                .Distinct()
                .ToList();

            // FY logic
            string FY = "FY1" + (DateTime.Now.Month >= 4
                ? (int.Parse(DateTime.Now.ToString("yy")) + 1).ToString()
                : DateTime.Now.ToString("yy"));

            var sectionCodes = distinctSections
                .Select(s => s.Section_To_Code)
                .ToList();

            var budgetData = db.FA_BudgetSystem_BEX
                .Where(x =>
                    sectionCodes.Contains(x.Section_To_Code) &&
                    budgetNos.Contains(x.Budget_No) &&
                    x.Final_Version == 1 &&
                    x.Period_FY == FY
                )
                .ToList();

            // =========================================================================
            // NEW: Hitung Purchased Usage menggunakan filter yang sama seperti di Cart
            // =========================================================================
            var purchasedUsages = (from c in dbm.HC_GA_Inventory_Carts
                                   join r in dbm.HC_GA_Inventory_Requests on c.Request_No equals r.Request_No
                                   where budgetNos.Contains(c.Budget_No)
                                      && r.Status != "Pending"
                                      && !r.Status.Contains("Waiting")
                                      && !r.Status.Contains("Reject")
                                      && !r.Status.Contains("Return")
                                   group c by c.Budget_No into g
                                   select new
                                   {
                                       Budget_No = g.Key,
                                       TotalUsage = g.Sum(x => (x.Price ?? 0) * (x.Qty_Acc ?? x.Qty_Request))
                                   }).ToDictionary(k => k.Budget_No, v => v.TotalUsage);
            // =========================================================================

            var budgetsResult = budgetData
                .GroupBy(x => new { x.Budget_No, x.Section_To_Code })
                .Select(g => new
                {
                    BudgetNo = g.Key.Budget_No,
                    SectionCode = g.Key.Section_To_Code,
                    BudgetDesc = g.FirstOrDefault()?.Description,
                    SectionToName = g.FirstOrDefault()?.Section_To_Name,
                    TotalFY2 = g.Sum(x => (decimal?)x.TotalFY2) ?? 0,
                    // Tambahkan properti PurchasedUsage di sini
                    PurchasedUsage = purchasedUsages.ContainsKey(g.Key.Budget_No) ? purchasedUsages[g.Key.Budget_No] : 0
                })
                .ToList();

            var totalRequestByBudget = items
                .GroupBy(i => i.Budget_No)
                .Select(g => new
                {
                    BudgetNo = g.Key,
                    TotalRequest = g.Sum(x => x.Qty_Request * (x.Price))
                })
                .ToList();
           

            // RETURN JSON
            return Json(new
            {
                request.Request_No,
                Invoice_No = request.Invoice_Number,
                request.Subject,
                request.Due_Date,
                request.Status,
                request.Receive_Number,
                RejectMessage = request.Reason_Message,
                Budgets = budgetsResult,
                BudgetSummary = totalRequestByBudget,


                // backend approval yang sudah ada
                PrepareBy = approval?.Prepared_Name,
                PrepareNik = approval?.Prepared_NIK,
                PrepareDate = approval?.Prepared_Date,
                CheckBy = approval?.Checked_Name,
                CheckDate = approval?.Checked_Date,
                ApproveBy = approval?.Approved_Name,
                ApproveDate = approval?.Approved_Date,

                // NEW: mapping roles
                Checker = checker,
                Approver = approverObj,
                Admin = admin,

                Items = items
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAllHistory()
        {
            var allCarts = dbm.HC_GA_Inventory_Carts.ToList();
            var itemList = dbm.HC_GA_Inventory_Item_List.ToList();

            // 🔥 Ambil data approval
            var approvals = dbm.HC_GA_Inventory_Requests_Approval.ToList();

            var requests = dbm.HC_GA_Inventory_Requests
                .Where(r =>
                       r.Status != "Rejected by Checker"
                         && r.Status != "Rejected by Approver")
                .AsEnumerable()
                .Select(r =>
                {
                    var cartNos = r.Cart_No?.Split(',') ?? new string[0];

                    var relatedCarts = allCarts
                        .Where(c => cartNos.Contains(c.Cart_No))
                        .Select(cart =>
                        {
                            var item = itemList.FirstOrDefault(i => i.ID == cart.Item_Id);
                            int itemStock = item?.Qty ?? 0;

                            return new
                            {
                                cart.Cart_No,
                                cart.Item_Id,
                                cart.Item_Name,
                                cart.Unit_Request,
                                Qty_Request = cart.Qty_Request,
                                Qty_Acc = cart.Qty_Acc ?? 0,
                                Stock_Qty = itemStock,
                                Notes = cart.Notes ?? "-",
                                cart.isReady,
                                cart.Pick_Date,
                                cart.Section_To_Name
                            };
                        }).ToList();

                    // 🔥 Get Approval Date
                    var approval = approvals.FirstOrDefault(a => a.Request_No == r.Request_No);
                    string approvedDate = approval?.Approved_Date?.ToString("yyyy-MM-dd HH:mm:ss");

                    // Cek need to be order
                    //string status = r.Status;
                    //if (relatedCarts.Any(c => c.Stock_Qty < c.Qty_Request))
                    //{
                    //    status = "Need to Be Order";
                    //}

                    return new
                    {
                        r.Request_No,
                        Status = r.Status,
                        r.Receive_Number,
                        r.Subject,
                        r.User_NIK,
                        r.User_Name,
                        r.Dept_Name,
                        r.Dept_Request,
                        r.Timestamps,
                        r.Invoice_Number,
                        Due_Date = r.Due_Date.ToString("yyyy-MM-dd"),
                        Approved_Date = approvedDate,
                        Cart_Nos = relatedCarts
                    };
                })
                .ToList();

            return Json(requests, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCartByCartNos(List<string> cartNos)
        {
            if (cartNos == null || !cartNos.Any())
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var requestsWithCartNos = dbm.HC_GA_Inventory_Requests
                .Where(r => r.Cart_No != null)
                .ToList()
                .Where(r => r.Cart_No.Split(',').Select(c => c.Trim()).Any(c => cartNos.Contains(c)))
                .ToList();

            var requestInfoMap = new Dictionary<string, (string requestNo, string rejectMessage, string receiveNumber, string invoiceNumber)>();

            foreach (var request in requestsWithCartNos)
            {
                var individualCartNos = request.Cart_No.Split(',').Select(c => c.Trim());
                foreach (var cartNo in individualCartNos)
                {
                    if (!requestInfoMap.ContainsKey(cartNo))
                    {
                        requestInfoMap[cartNo] = (request.Request_No, request.Reason_Message, request.Receive_Number, request.Invoice_Number);
                    }
                }
            }

            var carts = (from cart in dbm.HC_GA_Inventory_Carts
                         join item in dbm.HC_GA_Inventory_Item_List on cart.Item_Id equals item.ID
                         where cartNos.Contains(cart.Cart_No)
                         select new
                         {
                             cart.ID,
                             cart.Cart_No,
                             cart.Item_Id,
                             cart.Item_Name,
                             cart.Unit_Request,
                             cart.Timestamps,
                             cart.Qty_Request,
                             cart.Qty_Acc,
                             cart.Notes,
                             cart.User_Name,
                             cart.Dept_Name,
                             cart.Budget_No,
                             cart.Budget_Desc,
                             cart.isReady,
                             cart.Pick_Date,
                             cart.Price,
                             item.Qty,
                             item.Subcategory_Id
                         }).ToList();

            var cartIds = carts.Select(x => x.ID).ToList();
            var priceLogs = dbm.HC_GA_Inventory_Price_Logs 
                .Where(x => cartIds.Contains(x.Cart_ID))
                .GroupBy(x => x.Cart_ID)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.Update_Date).FirstOrDefault()
                );

            var totalRequestedPerItem = carts
                .GroupBy(c => c.Item_Id)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty_Request));

            var result = carts.Select(x =>
            {
                requestInfoMap.TryGetValue(x.Cart_No, out var reqInfo);

                priceLogs.TryGetValue(x.ID, out var log);

                string budgetInfo = x.Budget_No == "UNB"
                    ? (x.Budget_No ?? "-")
                    : $"{(x.Budget_No ?? "-")} | {(x.Budget_Desc ?? "-")}";

                string budgetAja = x.Budget_No == "UNB"
                    ? "UNB | UNB"
                    : $"{(x.Budget_No ?? "-")} | {(x.Budget_Desc ?? "-")}";

                int stock = x.Qty;
                int totalReq = totalRequestedPerItem.ContainsKey(x.Item_Id) ? totalRequestedPerItem[x.Item_Id] : 0;
                int preview = stock - totalReq;

                return new
                {
                    x.ID,
                    x.Cart_No,
                    Request_No = reqInfo.requestNo ?? "-",
                    Receive_Number = reqInfo.receiveNumber,
                    Invoice_Number = reqInfo.invoiceNumber,
                    x.Item_Id,
                    x.Item_Name,
                    x.Qty_Request,
                    Qty_Acc = x.Qty_Acc ?? 0,
                    Qty_Stock = stock,
                    Qty_Preview = preview,
                    x.Unit_Request,
                    x.Subcategory_Id,
                    Budget = budgetInfo,
                    BudgetAja = budgetAja,
                    Notes = x.Notes ?? "-",
                    Price = x.Price,
                    Old_Price = log?.Old_Price ?? null, // <-- Kirim data harga lama ke frontend (null jika tidak ada perubahan)
                    x.User_Name,
                    x.Dept_Name,
                    Reject_Message = reqInfo.rejectMessage ?? "-",
                    isReady = x.isReady,
                    Pick_Date = x.Pick_Date
                };
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveCategory(string Category_Name)
        {
            string userNik = User.Identity.GetUserId();
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
            var dept = currentUser?.CostName?.ToUpper() ?? "";

            if (dept != "HUMAN RESOURCE" && dept != "GENERAL AFFAIR")
            {
                return Json(new { success = false, message = "You do not have permission to Add Category." });
            }

            if (string.IsNullOrWhiteSpace(Category_Name))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            try
            {
                // Buat objek kategori baru
                HC_GA_Inventory_Category newCategory = new HC_GA_Inventory_Category
                {
                    Category_Name = Category_Name,
                    // Tambahkan properti lain jika ada, misalnya:
                    // Timestamps = DateTime.Now
                };

                // Simpan ke database
                dbm.HC_GA_Inventory_Category.Add(newCategory);
                int addedCategory = dbm.SaveChanges();

                if (addedCategory > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Category added successfully.",
                        categoryId = newCategory.ID // Mengembalikan ID yang baru dibuat
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to save category to database."
                    });
                }
            }
            catch (Exception ex)
            {
                // Tangani kesalahan tak terduga
                return Json(new
                {
                    success = false,
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public JsonResult UpdateCategory(int id, string Category_Name)
        {
            string userNik = User.Identity.GetUserId();
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
            var dept = currentUser?.CostName?.ToUpper() ?? "";

            if (dept != "HUMAN RESOURCE" && dept != "GENERAL AFFAIR")
            {
                return Json(new { success = false, message = "You do not have permission to Add Category." });
            }

            if (string.IsNullOrWhiteSpace(Category_Name))
            {
                return Json(new { success = false, message = "Category name cannot be empty." });
            }

            try
            {
                // 2. Cari kategori di database
                var data = dbm.HC_GA_Inventory_Category.FirstOrDefault(w => w.ID == id);
                if (data == null)
                {
                    return Json(new { success = false, message = "Category not found." });
                }

                // 3. Perbarui properti
                data.Category_Name = Category_Name;

                // 4. Simpan perubahan
                dbm.SaveChanges();

                return Json(new { success = true, message = "Category updated successfully." });
            }
            catch (Exception ex)
            {
                // 5. Tangani kesalahan
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveCart()
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            string costID = _currUser.FindFirstValue("AXCostID");
            string DeptName = _currUser.FindFirstValue("AXCostName");

            // Ambil data dari Request
            var userNik = Request["User_NIK"];
            var userName = Request["User_Name"];
            var budgetNo = Request["Budget_No"]?.Trim();
            var budgetDesc = Request["Budget_Desc"];
            var notes = Request["Notes"];

            string sectionToCode = "";
            string sectionToName = "";

            var userRoles = dbm.HC_GA_Inventory_UserDept
                .Where(x => x.NIK == userNik)
                .Select(x => x.Role)
                .ToList();

            if (!userRoles.Contains("ADMIN"))
            {
                return Json(new { success = false, message = "Oops! You are not allowed to add items to cart." }, JsonRequestBehavior.AllowGet);
            }

            int itemId;
            if (!Int32.TryParse(Request["Item_Id"], out itemId) || itemId <= 0)
            {
                return Json(new { success = false, message = "Item ID is not valid." }, JsonRequestBehavior.AllowGet);
            }

            int qtyReq;
            if (!Int32.TryParse(Request["Qty_Request"], out qtyReq) || qtyReq <= 0)
            {
                return Json(new { success = false, message = "Please enter a valid quantity." }, JsonRequestBehavior.AllowGet);
            }

            if (budgetNo == "UNB")
            {
                // Kondisi KHUSUS GENERAL AFFAIR
                if (DeptName.Equals("GENERAL AFFAIR", StringComparison.OrdinalIgnoreCase))
                {
                    sectionToCode = Request["Section_To_Code"]; // Ambil code dari request FE
                    sectionToName = Request["Section_To_Name"]?.Trim();

                    if (string.IsNullOrEmpty(sectionToName))
                    {
                        return Json(new { success = false, message = "GA User: Please select a Destination Department." }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    // Untuk departemen lain, kunci ke departemen mereka sendiri
                    sectionToCode = costID; // Menggunakan AXCostID user login
                    sectionToName = DeptName;
                }
            }
            else
            {
                // JIKA BUDGET RESMI (Bukan UNB), CARI DI MASTER BUDGET SYSTEM (Ini yang sebelumnya hilang)
                var budgetInfo = db.V_FA_BudgetSystem_BEX_BEL
                    .Where(w => w.Budget_No.Trim() == budgetNo)
                    .Select(s => new { s.Section_To_Code, s.Section_To_Name })
                    .FirstOrDefault();

                if (budgetInfo != null)
                {
                    sectionToCode = budgetInfo.Section_To_Code;
                    sectionToName = budgetInfo.Section_To_Name;
                }
                else
                {
                    return Json(new { success = false, message = "Budget Number not found in Budget System." }, JsonRequestBehavior.AllowGet);
                }
            }
            var itemDetail = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(w => w.ID == itemId);
            if (itemDetail == null)
            {
                return Json(new { success = false, message = "Item not found in inventory list." }, JsonRequestBehavior.AllowGet);
            }

            var existingCartItem = dbm.HC_GA_Inventory_Carts
                                 .FirstOrDefault(c => c.User_NIK == userNik && c.Status == 0);

            if (existingCartItem != null)
            {
                // 2. Ambil detail item yang sudah ada di cart untuk melihat kategorinya
                var existingItemDetail = dbm.HC_GA_Inventory_Item_List
                                            .FirstOrDefault(i => i.ID == existingCartItem.Item_Id);

                // 3. Bandingkan kategori. (Ubah .Category sesuai nama property di model HC_GA_Inventory_Item_List kamu)
                if (existingItemDetail != null && existingItemDetail.Category_Id != itemDetail.Category_Id)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Kamu tidak bisa menambahkan item dengan kategori yang berbeda ke dalam satu keranjang."
                    }, JsonRequestBehavior.AllowGet);
                }
            }

            var now = DateTime.Now;
            var prefix = $"CART-{now:yyMM}";

            var lastCartNumber = dbm.HC_GA_Inventory_Carts
                .Where(x => x.Cart_No.StartsWith(prefix))
                .OrderByDescending(x => x.Cart_No)
                .Select(x => x.Cart_No)
                .FirstOrDefault();

            int sequence = 1;
            if (!string.IsNullOrEmpty(lastCartNumber))
            {
                var lastSeqStr = lastCartNumber.Substring(prefix.Length);
                if (int.TryParse(lastSeqStr, out var lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }
            var finalCartNumber = $"{prefix}{sequence:D3}";

            // 6. Simpan ke Database
            try
            {
                HC_GA_Inventory_Carts cartData = new HC_GA_Inventory_Carts
                {
                    Cart_No = finalCartNumber,
                    Item_Id = itemId,
                    Item_Name = itemDetail.Item_Name,
                    Qty_Request = qtyReq,
                    Unit_Request = itemDetail.Unit,
                    Budget_No = budgetNo,
                    Budget_Desc = budgetDesc,
                    User_NIK = userNik,
                    User_Name = userName,
                    Dept_Name = DeptName,
                    Section_Name = Request["Section_Name"],
                    Division_Name = Request["Division_Name"],
                    Status = 0,
                    Notes = notes,
                    Section_To_Name = sectionToName,
                    Section_To_Code = sectionToCode,
                    Timestamps = now,
                    Price = itemDetail.Price 
                };

                dbm.HC_GA_Inventory_Carts.Add(cartData);
                dbm.SaveChanges();

                return Json(new { success = true, message = "Item added to cart successfully." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Internal Server Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult UpdateCart(int id)
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            string costID = _currUser.FindFirstValue("AXCostID");
            string DeptName = _currUser.FindFirstValue("AXCostName");
            var userNik = Request["User_NIK"];

            // 1. Validasi Role User (Hanya ADMIN yang boleh)
            var userRoles = dbm.HC_GA_Inventory_UserDept
                .Where(x => x.NIK == userNik)
                .Select(x => x.Role)
                .ToList();

            if (!userRoles.Contains("ADMIN"))
            {
                return Json(new { success = false, message = "Oops! You are not allowed to update cart." }, JsonRequestBehavior.AllowGet);
            }

            // 2. Ambil data eksisting dari database
            var data = dbm.HC_GA_Inventory_Carts.FirstOrDefault(w => w.ID == id);
            if (data == null)
            {
                return Json(new { success = false, message = "Cart item not found." }, JsonRequestBehavior.AllowGet);
            }

            // 3. Validasi Input Dasar
            int itemId;
            if (!Int32.TryParse(Request["Item_Id"], out itemId))
            {
                return Json(new { success = false, message = "Invalid Item ID." }, JsonRequestBehavior.AllowGet);
            }

            int qtyReq;
            if (!Int32.TryParse(Request["Qty_Request"], out qtyReq) || qtyReq <= 0)
            {
                return Json(new { success = false, message = "Please enter a valid quantity." }, JsonRequestBehavior.AllowGet);
            }

            // 4. Logika Penentuan Section Tujuan (Mapping Section_To)
            var budgetNo = Request["Budget_No"]?.Trim();
            string sectionToCode = "";
            string sectionToName = "";

            if (budgetNo == "UNB")
            {
                // Kondisi KHUSUS GENERAL AFFAIR
                if (DeptName.Equals("GENERAL AFFAIR", StringComparison.OrdinalIgnoreCase))
                {
                    sectionToCode = "UNB";
                    sectionToName = Request["Section_To_Name"].Trim();

                    if (string.IsNullOrEmpty(sectionToCode))
                    {
                        return Json(new { success = false, message = "GA User: Please select a Destination Department." }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    // Untuk departemen lain, kunci ke departemen mereka sendiri
                    sectionToCode = "UNB";
                    sectionToName = DeptName;
                }
            }
            else
            {
                // Jika Budget Resmi (Bukan UNB), cari di Master Budget System
                var budgetInfo = db.V_FA_BudgetSystem_BEX_BEL
                    .Where(w => w.Budget_No.Trim() == budgetNo)
                    .Select(s => new { s.Section_To_Code, s.Section_To_Name })
                    .FirstOrDefault();

                if (budgetInfo != null)
                {
                    sectionToCode = budgetInfo.Section_To_Code;
                    sectionToName = budgetInfo.Section_To_Name;
                }
                else
                {
                    return Json(new { success = false, message = "Budget Number not found in Budget System." }, JsonRequestBehavior.AllowGet);
                }
            }

            // 5. Update data objek
            try
            {
                // Ambil detail item terbaru (takutnya ID item diganti saat edit)
                var itemDetail = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(w => w.ID == itemId);
                if (itemDetail != null)
                {
                    data.Item_Id = itemId;
                    data.Item_Name = itemDetail.Item_Name;
                    data.Unit_Request = itemDetail.Unit;
                }

                data.Qty_Request = qtyReq;
                data.Budget_No = budgetNo;
                data.Budget_Desc = Request["Budget_Desc"];
                data.Section_To_Code = sectionToCode;
                data.Section_To_Name = sectionToName;
                data.Notes = Request["Notes"];
                data.Timestamps = DateTime.Now;

                dbm.SaveChanges();

                return Json(new { success = true, message = "Cart Updated Successfully" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult DeleteCart(int id)
        {
            var data = dbm.HC_GA_Inventory_Carts.FirstOrDefault(b => b.ID == id);

            dbm.HC_GA_Inventory_Carts.Remove(data);
            dbm.SaveChanges();

            return Json(new { success = true, message = "Cart Deleted Successfully" });
        }

        [HttpPost]
        public JsonResult SaveRequest(
            string subject,
            string requestNo,
            DateTime dueDate,
            string userNik,
            string deptName,
            string userName,
            string deptReq
        )
        {
            //var _currUser = (ClaimsIdentity)User.Identity;
            //string departmentName = _currUser.FindFirstValue("deptName");

            //deptName = deptName.Replace("&amp;", "&");

            //if (!string.Equals(departmentName, "HRD & GA", StringComparison.OrdinalIgnoreCase))
            //{
            //    int pendingVoucherCount = dbm.HC_GA_Inventory_Requests
            //        .Count(r => r.Dept_Request == deptName
            //                 && r.Receive_Number == null);

            //    if (pendingVoucherCount >= 2)
            //    {
            //        return Json(new
            //        {
            //            success = false,
            //            message = "You already have 2 requests without e-voucher. Please generate previous e-voucher before making another request."
            //        });
            //    }
            //}

            string costName;

            if (!string.IsNullOrEmpty(deptReq))
            {
                deptReq = WebUtility.HtmlDecode(deptReq);
            }

            if (!string.IsNullOrEmpty(deptName))
            {
                deptName = WebUtility.HtmlDecode(deptName);
            }

            if (deptReq == "UNB")
            {
                costName = deptName;
            }
            else
            {
                var department = dbm.HC_GA_Inventory_Dept
                    .FirstOrDefault(d => d.Cost_Name == deptReq);

                if (department == null)
                    return Json(new { success = false, message = "Invalid Department Request (deptReq)." });

                costName = department.Cost_Name;
            }

            // 2. Ambil Cart User
            var carts = dbm.HC_GA_Inventory_Carts
                .Where(c => c.User_NIK == userNik && c.Status == 0)
                .ToList();

            if (!carts.Any())
                return Json(new { success = false, message = "No items in the cart." });

            // 3. Tentukan RequestNo (pakai lama atau generate baru)
            string finalRequestNo = requestNo;

            if (string.IsNullOrWhiteSpace(finalRequestNo))
            {
                var now = DateTime.Now;
                var prefix = now.ToString("yyMMdd");

                var last = dbm.HC_GA_Inventory_Requests
                    .Where(r => r.Request_No.StartsWith("REQ-" + prefix))
                    .OrderByDescending(r => r.Request_No)
                    .FirstOrDefault();

                int seq = 1;

                if (last != null &&
                    int.TryParse(last.Request_No.Substring(10), out var lastSeq))
                {
                    seq = lastSeq + 1;
                }

                finalRequestNo = $"REQ-{prefix}{seq:D4}";
            }

            // 4. Join Cart_No
            var joinedCartNo = string.Join(",", carts.Select(c => c.Cart_No));

            // 5. Cek apakah request sudah ada → UPDATE
            var existing = dbm.HC_GA_Inventory_Requests
                .FirstOrDefault(r => r.Request_No == finalRequestNo);

            bool isNew = (existing == null);

            if (isNew)
            {
                // INSERT BARU
                existing = new HC_GA_Inventory_Requests
                {
                    Request_No = finalRequestNo,
                    Cart_No = joinedCartNo,
                    Subject = subject,
                    Due_Date = dueDate,
                    User_NIK = userNik,
                    User_Name = userName,
                    Dept_Name = deptName,
                    Dept_Request = deptName,
                    Status = "Pending",
                    Timestamps = DateTime.Now
                };

                dbm.HC_GA_Inventory_Requests.Add(existing);
            }
            else
            {
                // UPDATE EXISTING
                existing.Cart_No = joinedCartNo;
                existing.Status = "Pending";
                existing.Subject = subject;
                existing.Due_Date = dueDate;
                existing.Dept_Request = costName;
                existing.Timestamps = DateTime.Now;
            }

            // 6. Update Cart Assign ke Request
            foreach (var cart in carts)
            {
                cart.Status = 1;
                cart.Request_No = finalRequestNo;
            }

            // 7. Insert Approval kalau request baru
            if (isNew)
            {
                var approval = new HC_GA_Inventory_Requests_Approval
                {
                    Request_No = finalRequestNo
                };

                dbm.HC_GA_Inventory_Requests_Approval.Add(approval);
            }

            // 8. Save DB
            dbm.SaveChanges();

            // 9. Kirim Email
            try
            {
                //SendEmail(finalRequestNo, "pending");
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = true,
                    message = "Request saved, but failed to send email: " + ex.Message,
                    requestNo = finalRequestNo
                });
            }

            return Json(new
            {
                success = true,
                message = "Request saved successfully.",
                requestNo = finalRequestNo
            });
        }

        [HttpPost]
        public JsonResult DeleteRequest(string requestNo)
        {
            try
            {
                if (string.IsNullOrEmpty(requestNo))
                    return Json(new { success = false, message = "Invalid request number." });

                // Hapus data CART
                var carts = dbm.HC_GA_Inventory_Carts
                                .Where(c => c.Request_No == requestNo)
                                .ToList();
                if (carts.Any())
                    dbm.HC_GA_Inventory_Carts.RemoveRange(carts);

                // Hapus data APPROVAL (opsional: samakan nama tabel approval kamu)
                var approval = dbm.HC_GA_Inventory_Requests_Approval
                                  .Where(a => a.Request_No == requestNo)
                                  .ToList();
                if (approval.Any())
                    dbm.HC_GA_Inventory_Requests_Approval.RemoveRange(approval);

                // Hapus REQUEST-nya
                var request = dbm.HC_GA_Inventory_Requests
                                 .FirstOrDefault(r => r.Request_No == requestNo);
                if (request != null)
                    dbm.HC_GA_Inventory_Requests.Remove(request);

                dbm.SaveChanges();

                return Json(new { success = true, message = "Request deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public JsonResult GetItemsByRequest()
        {
            var datas = (from request in dbm.HC_GA_Inventory_Requests
                         join cart in dbm.HC_GA_Inventory_Carts
                         on request.Cart_No equals cart.Cart_No
                         select new
                         {
                             request.ID,
                             request.Request_No,
                             request.Cart_No,
                             cart.Item_Id,
                             cart.Item_Name,
                             cart.Qty_Request,
                             cart.Unit_Request,
                             cart.Budget_No,
                             cart.Budget_Desc,
                             cart.Notes
                         }).ToList();

            return Json(new { datas }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveItem(HC_GA_Inventory_Item_List model)
        {
            // Ambil user yang sedang login
            string userNik = User.Identity.GetUserId();
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);

            // Validasi akses: hanya GA yang boleh
            if (currentUser == null ||
                !(string.Equals(currentUser.CostName, "GENERAL AFFAIR", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(currentUser.CostName, "HUMAN RESOURCE", StringComparison.OrdinalIgnoreCase)))
            {
                return Json(new { success = false, message = "You're not allowed." });
            }
            // Validasi input dasar
            if (string.IsNullOrWhiteSpace(model.Item_Name) || model.Qty <= 0 || string.IsNullOrWhiteSpace(model.Unit) || model.Category_Id <= 0 || model.Subcategory_Id <= 0)
            {
                return Json(new { success = false, message = "Mohon lengkapi semua data wajib." });
            }

            try
            {
                // 1. Ambil data gambar dari request
                string finalImageValue = model.Image; // Asumsi 'model.Image' berisi URL gambar dari input teks

                // 2. Cek apakah ada file yang diupload (Request.Files)
                HttpPostedFileBase uploadedFile = Request.Files["Image"];
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    // Ambil nama file dan simpan di folder
                    string fileName = Path.GetFileName(uploadedFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/Files/HC/Inventory/"), fileName);
                    uploadedFile.SaveAs(path);
                    finalImageValue = fileName; // Gunakan nama file lokal
                }

                // 3. Buat objek Item List baru
                HC_GA_Inventory_Item_List data = new HC_GA_Inventory_Item_List
                {
                    Item_Name = model.Item_Name?.Trim().ToUpper(),
                    Location = model.Location,
                    Qty = model.Qty,
                    Unit = model.Unit,
                    Price = model.Price,
                    Ref_Code = model.Ref_Code,
                    Category_Id = model.Category_Id,
                    Subcategory_Id = model.Subcategory_Id,
                    Image = finalImageValue,
                    User_NIK = model.User_NIK,
                    Timestamps = DateTime.Now,
                };

                // 4. Tambahkan ke database dan simpan perubahan
                dbm.HC_GA_Inventory_Item_List.Add(data);
                dbm.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Item Added Successfully",
                    itemId = data.ID
                });
            }
            catch (Exception ex)
            {
                // Tangani error jika terjadi masalah
                return Json(new
                {
                    success = false,
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public JsonResult SaveSubcategory(string Subcategory_Name, int Category_Id, string Subcategory_Image)
        {
            string userNik = User.Identity.GetUserId();
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
            var dept = currentUser?.CostName?.ToUpper() ?? "";

            if (dept != "HUMAN RESOURCE" && dept != "GENERAL AFFAIR")
            {
                return Json(new { success = false, message = "You do not have permission to Add Subcategory." });
            }

            if (string.IsNullOrWhiteSpace(Subcategory_Name))
            {
                return Json(new { success = false, message = "Subcategory name is required." });
            }

            if (Category_Id <= 0)
            {
                return Json(new { success = false, message = "A category must be selected." });
            }

            try
            {
                string finalFileName = Subcategory_Image; // Default ke URL jika tidak ada file

                // 2. Tangani file gambar yang diupload
                HttpPostedFileBase uploadedFile = Request.Files["file"];
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    // Ambil nama file dan simpan di folder
                    string fileName = Path.GetFileName(uploadedFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/Files/HC/Inventory/"), fileName);
                    uploadedFile.SaveAs(path);
                    finalFileName = fileName; // Gunakan nama file lokal
                }

                // 3. Buat objek Subcategory baru dengan data gambar
                HC_GA_Inventory_Subcategory newSubcategory = new HC_GA_Inventory_Subcategory
                {
                    Subcategory_Name = Subcategory_Name,
                    Category_Id = Category_Id,
                    Subcategory_Image = finalFileName // Simpan nama file atau URL
                };

                // 4. Tambahkan ke database dan simpan perubahan
                dbm.HC_GA_Inventory_Subcategory.Add(newSubcategory);
                dbm.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Subcategory added successfully.",
                    subcategoryId = newSubcategory.ID
                });
            }
            catch (Exception ex)
            {
                // 5. Tangani error jika terjadi masalah
                return Json(new
                {
                    success = false,
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public JsonResult UpdateItem(HC_GA_Inventory_Item_List model)
        {
            if (model == null || model.ID <= 0)
                return Json(new { success = false, message = "Invalid request." });

            try
            {
                var data = dbm.HC_GA_Inventory_Item_List
                              .FirstOrDefault(x => x.ID == model.ID);

                if (data == null)
                    return Json(new { success = false, message = "Item not found." });

                bool isChanged = false;

                if (data.Qty != model.Qty)
                {
                    data.Qty = model.Qty;
                    data.Last_Price_Update = DateTime.Now;
                    isChanged = true;
                }

                if (data.Item_Name != model.Item_Name)
                {
                    data.Item_Name = model.Item_Name?.Trim().ToUpper();
                    isChanged = true;
                }

                if (data.Location != model.Location)
                {
                    data.Location = model.Location;
                    isChanged = true;
                }

                if (data.Unit != model.Unit)
                {
                    data.Unit = model.Unit;
                    isChanged = true;
                }

                if (data.Price != model.Price)
                {
                    data.Price = model.Price;
                    isChanged = true;
                }

                if (data.Ref_Code != model.Ref_Code)
                {
                    data.Ref_Code = model.Ref_Code;
                    isChanged = true;
                }

                if (data.Category_Id != model.Category_Id)
                {
                    data.Category_Id = model.Category_Id;
                    isChanged = true;
                }

                if (data.Subcategory_Id != model.Subcategory_Id)
                {
                    data.Subcategory_Id = model.Subcategory_Id;
                    isChanged = true;
                }

                data.User_NIK = model.User_NIK;
                data.Timestamps = DateTime.Now;

                if (Request.Files.Count > 0)
                {
                    var file = Request.Files["Image"];

                    if (file != null && file.ContentLength > 0)
                    {
                        // delete old image (if local file)
                        if (!string.IsNullOrEmpty(data.Image) && !data.Image.StartsWith("http"))
                        {
                            var oldPath = Server.MapPath("~/Files/HC/Inventory/" + data.Image);
                            if (System.IO.File.Exists(oldPath))
                                System.IO.File.Delete(oldPath);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string savePath = Server.MapPath("~/Files/HC/Inventory/" + fileName);

                        file.SaveAs(savePath);
                        data.Image = fileName;

                        isChanged = true;
                    }
                }
                else if (!string.IsNullOrEmpty(model.Image))
                {
                    data.Image = model.Image;
                    isChanged = true;
                }

                if (!isChanged)
                    return Json(new { success = false, message = "No changes detected." });

                dbm.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Item updated successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                });
            }
        }

        //public ActionResult DownloadItemSummary()
        //{
        //    var pcrData = dbm.Set<V_PE_PCR_Summary>()
        //                     .OrderByDescending(x => x.Head_ID)
        //                     .ToList();

        //    using (var workbook = new XLWorkbook())
        //    {
        //        var ws = workbook.Worksheets.Add("Summary");

        //        ws.Cell(1, 1).Value = "No";
        //        ws.Cell(1, 2).Value = "Head_ID";
        //        ws.Cell(1, 3).Value = "Date_Issued";
        //        ws.Cell(1, 4).Value = "Document_No";
        //        ws.Cell(1, 5).Value = "Title";
        //        ws.Cell(1, 6).Value = "Dept";
        //        ws.Cell(1, 7).Value = "Originator";
        //        ws.Cell(1, 8).Value = "Plan_Date";
        //        ws.Cell(1, 9).Value = "Finish_Date";
        //        ws.Cell(1, 10).Value = "Rank";
        //        ws.Cell(1, 11).Value = "Status";
        //        ws.Cell(1, 12).Value = "Progress_Percentage";
        //        ws.Cell(1, 13).Value = "Remarks";
        //        ws.Cell(1, 14).Value = "Duration_Days";
        //        ws.Cell(1, 14).Value = "CostImpact";

        //        var headerRange = ws.Range("A1:N1");
        //        headerRange.Style.Font.Bold = true;
        //        headerRange.Style.Font.FontSize = 14;
        //        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
        //        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        //        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        //        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        //        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        //        ws.Row(1).Height = 25;

        //        int row = 2;
        //        int no = 1;

        //        foreach (var d in pcrData)
        //        {
        //            ws.Cell(row, 1).Value = no++;
        //            ws.Cell(row, 2).Value = d.Head_ID;
        //            ws.Cell(row, 3).Value = d.Date_Issued;
        //            ws.Cell(row, 4).Value = d.Document_No;
        //            ws.Cell(row, 5).Value = d.Title;
        //            ws.Cell(row, 6).Value = d.Dept;
        //            ws.Cell(row, 7).Value = d.Originator;
        //            ws.Cell(row, 8).Value = d.Plan_Date;
        //            ws.Cell(row, 9).Value = d.Finish_Date;
        //            ws.Cell(row, 10).Value = d.Rank;
        //            ws.Cell(row, 11).Value = d.Status;
        //            ws.Cell(row, 12).Value = d.Progress_Percentage;
        //            ws.Cell(row, 13).Value = d.Remarks;
        //            ws.Cell(row, 14).Value = d.Duration_Days;
        //            ws.Cell(row, 14).Value = d.CostImpact;
        //            row++;
        //        }

        //        ws.Columns().AdjustToContents();

        //        using (var stream = new MemoryStream())
        //        {
        //            workbook.SaveAs(stream);
        //            stream.Position = 0;

        //            return File(
        //                stream.ToArray(),
        //                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //                $"PCR_SUMMARY_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        //            );
        //        }
        //    }
        //}

        public ActionResult DownloadItemSummary(string ids)
        {
            if (string.IsNullOrEmpty(ids)) return RedirectToAction("Index");

            var requestNos = ids.Split(',').ToList();

            var data = dbm.V_HC_GA_ItemSummary
                          .Where(x => requestNos.Contains(x.Request_No))
                          .OrderBy(x => x.Request_No)
                          .ThenBy(x => x.Item_Name)
                          .ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Summary");

                // --- HEADER TABEL UTAMA ---
                ws.Cell(1, 1).Value = "Date";
                ws.Cell(1, 2).Value = "Ref Code";
                ws.Cell(1, 3).Value = "Item Name";
                ws.Cell(1, 4).Value = "Quantity";
                ws.Cell(1, 5).Value = "Price";
                ws.Cell(1, 6).Value = "Department";
                ws.Cell(1, 7).Value = "Total";

                var mainHeaderRange = ws.Range(1, 1, 1, 7);
                mainHeaderRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                mainHeaderRange.Style.Font.FontColor = XLColor.Black;
                mainHeaderRange.Style.Font.Bold = true;
                mainHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var d in data)
                {
                    ws.Cell(row, 1).Value = d.Date_Issued.ToString("yyyy-MM-dd");
                    ws.Cell(row, 2).Value = d.Ref_Code;
                    ws.Cell(row, 3).Value = d.Item_Name;
                    ws.Cell(row, 4).Value = d.Quantity;
                    ws.Cell(row, 5).Value = d.Price ?? 0;
                    ws.Cell(row, 6).Value = d.Department;
                    ws.Cell(row, 7).Value = (d.Price ?? 0) * d.Quantity;
                    row++;
                }

                // --- SUMMARY PER ITEM ---
                var summary = data.GroupBy(x => x.Item_Name)
                                  .Select(g => new
                                  {
                                      ItemName = g.Key,
                                      TotalQty = g.Sum(x => x.Quantity),
                                      TotalPrice = g.Sum(x => (x.Price ?? 0) * x.Quantity),
                                      Ref_Code = g.FirstOrDefault()?.Ref_Code ?? ""
                                  }).ToList();

                row += 2; // space before summary
                ws.Cell(row, 1).Value = "Ref Code";
                ws.Cell(row, 2).Value = "Item Name";
                ws.Cell(row, 3).Value = "Total Quantity";
                ws.Cell(row, 4).Value = "Total Price";

                var summaryHeaderRange = ws.Range(row, 1, row, 4);
                summaryHeaderRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                summaryHeaderRange.Style.Font.FontColor = XLColor.Black;
                summaryHeaderRange.Style.Font.Bold = true;
                summaryHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                row++;
                foreach (var s in summary)
                {
                    ws.Cell(row, 1).Value = s.Ref_Code;
                    ws.Cell(row, 2).Value = s.ItemName;
                    ws.Cell(row, 3).Value = s.TotalQty;
                    ws.Cell(row, 4).Value = s.TotalPrice;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"ItemSummary_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    );
                }
            }
        }


        [HttpPost]
        public JsonResult UpdateSubcategory(string Subcategory_Name, int Category_Id, string Subcategory_Image)
        {
            string userNik = User.Identity.GetUserId();
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
            var dept = currentUser?.CostName?.ToUpper() ?? "";

            if (dept != "HUMAN RESOURCE" && dept != "GENERAL AFFAIR")
            {
                return Json(new { success = false, message = "You do not have permission to Update Subcategory." });
            }

            int id = int.Parse(Request["id"]);

            try
            {
                // Validasi input dasar
                if (string.IsNullOrWhiteSpace(Subcategory_Name) || Category_Id <= 0)
                {
                    return Json(new { success = false, message = "Subcategory name and category are required." });
                }

                // Cari subkategori di database menggunakan ID yang sudah diparsing
                var data = dbm.HC_GA_Inventory_Subcategory.FirstOrDefault(w => w.ID == id);
                if (data == null)
                {
                    return Json(new { success = false, message = "Subcategory not found." });
                }

                string finalImageValue = Request["Subcategory_Image"];

                // Tangani unggahan file baru
                HttpPostedFileBase uploadedFile = Request.Files["Subcategory_Image"];
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    // Hapus file lama jika ada dan itu adalah file lokal
                    if (!string.IsNullOrEmpty(data.Subcategory_Image) && !data.Subcategory_Image.StartsWith("http"))
                    {
                        var oldPath = Server.MapPath("~/Files/HC/Inventory/" + data.Subcategory_Image);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Simpan file baru
                    var fileName = Path.GetFileName(uploadedFile.FileName);
                    var savePath = Server.MapPath("~/Files/HC/Inventory/" + fileName);
                    uploadedFile.SaveAs(savePath);
                    finalImageValue = fileName;
                }

                // Perbarui properti
                data.Subcategory_Name = Subcategory_Name;
                data.Category_Id = Category_Id;
                data.Subcategory_Image = finalImageValue;

                dbm.SaveChanges();

                return Json(new { success = true, message = "Subcategory updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpPost]
        public JsonResult DeleteItem(int id)
        {
            try
            {
                // Ambil user login
                string userNik = User.Identity.GetUserId();
                var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
                var dept = currentUser?.CostName?.ToUpper() ?? "";

                if (dept != "HUMAN RESOURCE" && dept != "GENERAL AFFAIR")
                {
                    return Json(new { success = false, message = "You do not have permission to delete items." });
                }

                var data = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(b => b.ID == id);

                if (data == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                // Hapus file gambar jika ada, tetapi hanya jika itu bukan URL eksternal
                if (!string.IsNullOrEmpty(data.Image) && !data.Image.StartsWith("http"))
                {
                    var relativePath = "~/Files/HC/Inventory/" + data.Image;
                    var fullPath = Server.MapPath(relativePath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                // Hapus data dari database
                dbm.HC_GA_Inventory_Item_List.Remove(data);
                dbm.SaveChanges();

                return Json(new { success = true, message = "Item successfully deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpPost]
        public JsonResult DeleteCategory(int id)
        {
            string userNik = User.Identity.GetUserId();
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
            var dept = currentUser?.CostName?.ToUpper() ?? "";

            if (dept != "HUMAN RESOURCE" && dept != "GENERAL AFFAIR")
            {
                return Json(new { success = false, message = "You do not have permission to delete items." });
            }

            var data = dbm.HC_GA_Inventory_Category.FirstOrDefault(b => b.ID == id);

            dbm.HC_GA_Inventory_Category.Remove(data);
            dbm.SaveChanges();

            return Json(new { success = true, message = "Category Deleted Successfully" });
        }

        [HttpPost]
        public JsonResult DeleteSubcategory(int id)
        {
            string userNik = User.Identity.GetUserId();
            var currentUser = db.Users.FirstOrDefault(u => u.NIK == userNik);
            var dept = currentUser?.CostName?.ToUpper() ?? "";

            if (dept != "HUMAN RESOURCE" && dept != "GENERAL AFFAIR")
            {
                return Json(new { success = false, message = "You do not have permission to delete subcategory." });
            }

            var data = dbm.HC_GA_Inventory_Subcategory.FirstOrDefault(b => b.ID == id);

            dbm.HC_GA_Inventory_Subcategory.Remove(data);
            dbm.SaveChanges();

            return Json(new { success = true, message = "Subcategory Deleted Successfully" });
        }

        public class RequestConfirmationDto
        {
            public string Request_No { get; set; }
            public string Cart_No { get; set; }
            public int Item_Id { get; set; }
            public int Qty_Request { get; set; }
            public int Qty_Acc { get; set; }
            public string Status { get; set; }
            public string User_NIK { get; set; }
            public string User_NIK_Acc { get; set; }
            public string Message { get; set; }
        }

        [HttpPost]
        public JsonResult SaveRequestConfirmation(List<RequestConfirmationDto> confirmations)
        {
            if (confirmations == null || !confirmations.Any())
                return Json(new { success = false, message = "No items to process." });

            foreach (var dto in confirmations)
            {
                var cartNo = dto.Cart_No;
                var requestNo = dto.Request_No;

                // === Jika Request_No kosong, cari dari tabel request berdasarkan Cart_No ===
                if (string.IsNullOrWhiteSpace(requestNo))
                {
                    var reqHeader = dbm.HC_GA_Inventory_Requests
                        .FirstOrDefault(r => r.Cart_No.Contains(cartNo));
                    if (reqHeader != null)
                        requestNo = reqHeader.Request_No;
                }

                // === Ambil cart dan item terkait ===
                var cart = dbm.HC_GA_Inventory_Carts
                    .FirstOrDefault(c => c.Cart_No == cartNo && c.Item_Id == dto.Item_Id);

                var itemObj = dbm.HC_GA_Inventory_Item_List
                    .FirstOrDefault(i => i.ID == dto.Item_Id);

                // === Kalau cart tidak ditemukan, skip ===
                if (cart == null) continue;

                // === Update data cart ===
                cart.Qty_Acc = (dto.Status == "Rejected") ? 0 : dto.Qty_Acc;
                cart.User_NIK = dto.User_NIK;
                cart.Notes = (dto.Status == "Rejected") ? dto.Message : cart.Notes;
                cart.Timestamps = DateTime.Now;

                // === Update stok di Item_List jika status Complete ===
                if (dto.Status == "Complete" && itemObj != null)
                {
                    itemObj.Qty = cart.Qty_Acc ?? 0;
                    if (itemObj.Qty < 0)
                        itemObj.Qty = 0;
                }

                // === Update status di tabel Request utama ===
                var request = dbm.HC_GA_Inventory_Requests
                    .FirstOrDefault(r => r.Request_No == requestNo);

                if (request != null)
                {
                    if (dto.Status != "Order Complete")
                        request.Status = dto.Status;

                    if (dto.Status == "Rejected")
                        request.Reason_Message = dto.Message;
                }
            }

            int saved = dbm.SaveChanges();
            return Json(new { success = saved > 0 });
        }

        //[HttpPost]
        //public JsonResult SaveBulkRequestConfirmation(BulkRequestConfirmationModel model)
        //{
        //    if (model == null || model.Items == null || !model.Items.Any())
        //    {
        //        return Json(new { success = false, message = "No items to process." }, JsonRequestBehavior.AllowGet);
        //    }

        //    string errorMessage = null;
        //    var finalRequestStatus = new Dictionary<string, string>();
        //    var itemIds = model.Items.Select(x => x.Item_Id).Distinct().ToList();

        //    var itemListMap = dbm.HC_GA_Inventory_Item_List
        //        .Where(x => itemIds.Contains(x.ID))
        //        .ToDictionary(x => x.ID);

        //    var requestNos = model.Items.Select(x => x.Request_No).Distinct().ToList();

        //    var allCarts = dbm.HC_GA_Inventory_Carts
        //        .Where(c => requestNos.Contains(c.Request_No))
        //        .ToList();

        //    using (var transaction = dbm.Database.BeginTransaction())
        //    {
        //        try
        //        {
        //            var updatedRequests = new HashSet<string>();
        //            var newStatusSummary = new Dictionary<string, string>();

        //            // === 1. Update item carts ===
        //            foreach (var item in model.Items)
        //            {
        //                var itemObj = itemListMap.ContainsKey(item.Item_Id)
        //                    ? itemListMap[item.Item_Id]
        //                    : null;

        //                var cartRows = allCarts
        //                    .Where(c =>
        //                        c.Request_No == item.Request_No &&
        //                        c.Cart_No == item.Cart_No &&
        //                        c.Item_Id == item.Item_Id
        //                    )
        //                    .ToList();

        //                foreach (var cart in cartRows)
        //                {
        //                    cart.Qty_Acc = item.Qty_Request;

        //                    if (item.Status == "Ready to Pick")
        //                    {
        //                        cart.isReady = true;
        //                        //cart.Price = itemObj?.Price ?? 0;
        //                    }
        //                    else if (item.Status == "Update Pick Up")
        //                    {
        //                        cart.Pick_Date = DateTime.Now;
        //                        //cart.Price = itemObj?.Price ?? 0;
        //                    }
        //                    // Tambahkan "Generated" sebagai antisipasi jika Frontend diubah
        //                    else if (item.Status == "Complete" || item.Status == "Generated")
        //                    {
        //                        //decimal currentPrice = itemObj?.Price ?? 0;

        //                        //if (currentPrice <= 0)
        //                        //{
        //                        //    throw new Exception($"Cannot complete request. Item '{itemObj?.Item_Name}' has 0 price.");
        //                        //}

        //                        //cart.Price = currentPrice;
        //                        cart.isReady = true;
        //                        cart.Pick_Date = DateTime.Now;
        //                    }
        //                }

        //                // Update stock kalau statusnya Complete atau Generated
        //                if ((item.Status == "Complete" || item.Status == "Generated") && itemObj != null)
        //                {
        //                    itemObj.Qty -= item.Qty_Request;
        //                    if (itemObj.Qty < 0) itemObj.Qty = 0;
        //                }

        //                updatedRequests.Add(item.Request_No);
        //                newStatusSummary[item.Request_No] = "Complete";
        //            }

        //            dbm.SaveChanges();

        //            // === 2. Update status request ===
        //            foreach (var requestNo in updatedRequests)
        //            {
        //                var cartsPerRequest = allCarts
        //                    .GroupBy(x => x.Request_No)
        //                    .ToDictionary(
        //                        g => g.Key,
        //                        g => new {
        //                            Total = g.Count(),
        //                            Processed = g.Count(x => x.isReady == true && x.Pick_Date != null)
        //                        });
        //                var totalItems = cartsPerRequest[requestNo].Total;
        //                var processedItems = cartsPerRequest[requestNo].Processed;

        //                bool isAllProcessed = (totalItems == processedItems);

        //                var request = dbm.HC_GA_Inventory_Requests
        //                    .FirstOrDefault(r => r.Request_No == requestNo);

        //                if (request == null) continue;

        //                var incomingStatus = newStatusSummary[requestNo];

        //                if (incomingStatus == "Complete"
        //                    || incomingStatus == "Generated"
        //                    || incomingStatus == "Ready to Pick"
        //                    || incomingStatus == "Update Pick Up")
        //                {
        //                    if (isAllProcessed)
        //                    {
        //                        if (string.IsNullOrEmpty(request.Invoice_Number))
        //                        {
        //                            var invoiceResult = GenerateInvoiceNumber(request.Request_No);
        //                            dynamic data = invoiceResult.Data;
        //                            if (data.success == false)
        //                                throw new Exception("Invoice generation failed: " + data.message);

        //                            // 👇 LOGIKA UTAMA: Set status menjadi "Generated" karena Invoice baru saja dibuat
        //                            request.Status = "Generated";
        //                        }
        //                        else
        //                        {
        //                            // Jika invoice sudah ada sebelumnya, tetapkan ke "Complete" (opsional)
        //                            request.Status = "Complete";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        request.Status = "Ready to Pick";
        //                    }
        //                }
        //                else
        //                {
        //                    request.Status = incomingStatus;
        //                }

        //                if (incomingStatus == "Rejected")
        //                {
        //                    request.Reason_Message = model.Items
        //                        .FirstOrDefault(i => i.Request_No == requestNo)?.Message;
        //                }

        //                finalRequestStatus[requestNo] = request.Status;
        //            }

        //            dbm.SaveChanges();
        //            transaction.Commit();

        //            // === 3. Send Email Notifications ===
        //            // Looping melalui tiap request yang statusnya berubah, lalu kirim emailnya.
        //            foreach (var reqStatus in finalRequestStatus)
        //            {
        //                try
        //                {
        //                    //SendEmailStatusUpdate(reqStatus.Key, reqStatus.Value);
        //                }
        //                catch (Exception emailEx)
        //                {
        //                    System.Diagnostics.Debug.WriteLine($"Failed to send email for {reqStatus.Key}: {emailEx.Message}");
        //                }
        //            }

        //            return Json(new
        //            {
        //                success = true,
        //                message = $"{model.Items.Count} items updated successfully across {updatedRequests.Count} requests."
        //            }, JsonRequestBehavior.AllowGet);
        //        }
        //        catch (Exception ex)
        //        {
        //            errorMessage = ex.ToString();

        //            try
        //            {
        //                transaction.Rollback();
        //            }
        //            catch (Exception rbEx)
        //            {
        //                errorMessage += " | Rollback failed: " + rbEx.Message;
        //            }
        //        }
        //    }

        //    return Json(new
        //    {
        //        success = false,
        //        message = "An error occurred: " + errorMessage
        //    }, JsonRequestBehavior.AllowGet);
        //}
        public class UpdateBulkStatusModel
        {
            public List<string> RequestNos { get; set; }
            public string Status { get; set; }
        }

        [HttpPost]
        public JsonResult SaveBulkRequestConfirmation(BulkRequestConfirmationModel model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return Json(new { success = false, message = "No items to process." }, JsonRequestBehavior.AllowGet);
            }

            string errorMessage = null;
            var finalRequestStatus = new Dictionary<string, string>();

            var itemIds = model.Items.Select(x => x.Item_Id).Distinct().ToList();

            var itemListMap = dbm.HC_GA_Inventory_Item_List
                .Where(x => itemIds.Contains(x.ID))
                .ToDictionary(x => x.ID);

            var requestNos = model.Items.Select(x => x.Request_No).Distinct().ToList();

            var allCarts = dbm.HC_GA_Inventory_Carts
                .Where(c => requestNos.Contains(c.Request_No))
                .ToList();

            using (var transaction = dbm.Database.BeginTransaction())
            {
                try
                {
                    var updatedRequests = new HashSet<string>();
                    var newStatusSummary = new Dictionary<string, string>();

                    foreach (var item in model.Items)
                    {
                        var itemObj = itemListMap.ContainsKey(item.Item_Id)
                            ? itemListMap[item.Item_Id]
                            : null;

                        var cartRows = allCarts
                            .Where(c =>
                                c.Request_No == item.Request_No &&
                                c.Cart_No == item.Cart_No &&
                                c.Item_Id == item.Item_Id
                            )
                            .ToList();

                        foreach (var cart in cartRows)
                        {
                            cart.Qty_Acc = item.Qty_Request;

                            if (item.Status == "Ready to Pick")
                            {
                                cart.isReady = true;
                            }
                            else if (item.Status == "Update Pick Up")
                            {
                                cart.Pick_Date = DateTime.Now;
                            }
                            else if (item.Status == "Complete")
                            {
                                cart.isReady = true;
                                cart.Pick_Date = DateTime.Now;
                            }
                        }

                        //if (item.Status == "Complete" && itemObj != null)
                        //{
                        //    itemObj.Qty -= item.Qty_Request;
                        //    if (itemObj.Qty < 0) itemObj.Qty = 0;
                        //}

                        updatedRequests.Add(item.Request_No);
                        newStatusSummary[item.Request_No] = item.Status;
                    }

                    dbm.SaveChanges();

                    foreach (var requestNo in updatedRequests)
                    {
                        var cartsPerRequest = allCarts
                            .GroupBy(x => x.Request_No)
                            .ToDictionary(
                                g => g.Key,
                                g => new {
                                    Total = g.Count(),
                                    Processed = g.Count(x => x.isReady == true && x.Pick_Date != null)
                                });
                        var totalItems = cartsPerRequest[requestNo].Total;
                        var processedItems = cartsPerRequest[requestNo].Processed;

                        bool isAllProcessed = (totalItems == processedItems);

                        var request = dbm.HC_GA_Inventory_Requests
                            .FirstOrDefault(r => r.Request_No == requestNo);

                        if (request == null) continue;

                        var incomingStatus = newStatusSummary[requestNo];

                        if (incomingStatus == "Complete"
                            || incomingStatus == "Ready to Pick"
                            || incomingStatus == "Update Pick Up")
                        {
                            if (isAllProcessed)
                            {
                                if (string.IsNullOrEmpty(request.Invoice_Number))
                                {
                                    var invoiceResult = GenerateInvoiceNumber(request.Request_No);
                                    dynamic data = invoiceResult.Data;
                                    if (data.success == false)
                                        throw new Exception("Invoice generation failed: " + data.message);
                                }

                                request.Status = incomingStatus;
                            }
                            else
                            {
                                request.Status = "Ready to Pick";
                            }
                        }
                        else
                        {
                            request.Status = incomingStatus;
                        }

                        if (incomingStatus == "Rejected")
                        {
                            request.Reason_Message = model.Items
                                .FirstOrDefault(i => i.Request_No == requestNo)?.Message;
                        }

                        finalRequestStatus[requestNo] = request.Status;
                    }

                    dbm.SaveChanges();
                    transaction.Commit();

                    foreach (var reqStatus in finalRequestStatus)
                    {
                        var reqNo = reqStatus.Key;
                        var dbStatus = reqStatus.Value;

                        if (dbStatus == "Upload File")
                        {
                            continue;
                        }

                        try
                        {
                            SendEmailStatusUpdate(reqNo, dbStatus);
                        }
                        catch (Exception emailEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to send email for {reqNo}: {emailEx.Message}");
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        message = $"{model.Items.Count} items updated successfully across {updatedRequests.Count} requests."
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.ToString();

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rbEx)
                    {
                        errorMessage += " | Rollback failed: " + rbEx.Message;
                    }
                }
            }

            return Json(new
            {
                success = false,
                message = "An error occurred: " + errorMessage
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult GenerateMergeInvoiceNumber(List<string> requestNos)
        {
            try
            {
                if (requestNos == null || !requestNos.Any())
                    return Json(new { success = false, message = "No requests selected." });

                string year = DateTime.Now.ToString("yy");
                int month = DateTime.Now.Month;
                string monthRoman = ToRoman(month); 

                string prefix = $"INV{year}/KOPKAR/{monthRoman}/";

                var lastInvoice = dbm.HC_GA_Inventory_Requests
                    .Where(r => r.Invoice_Number.StartsWith(prefix))
                    .OrderByDescending(r => r.Invoice_Number)
                    .Select(r => r.Invoice_Number)
                    .FirstOrDefault();

                int nextIndex = 1;
                if (!string.IsNullOrEmpty(lastInvoice))
                {
                    var parts = lastInvoice.Split('/');
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int lastIndex))
                        nextIndex = lastIndex + 1;
                }

                string newInvoice = $"{prefix}{nextIndex:D3}";
                DateTime currentDate = DateTime.Now;
                var requestsToUpdate = dbm.HC_GA_Inventory_Requests
                    .Where(r => requestNos.Contains(r.Request_No))
                    .ToList();

                if (!requestsToUpdate.Any())
                    return Json(new { success = false, message = "Requests not found." });

                foreach (var req in requestsToUpdate)
                {
                    req.Invoice_Number = newInvoice;
                    req.Invoice_Date = currentDate;
                    req.Timestamps = currentDate;
                }

                dbm.SaveChanges();

                return Json(new
                {
                    success = true,
                    invoiceNumber = newInvoice,
                    invoiceDate = currentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    mergedCount = requestsToUpdate.Count 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult UpdateRequestStatusOnly(string requestNo, string status, string message = "")
        {
            if (string.IsNullOrEmpty(requestNo))
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid Request No"
                }, JsonRequestBehavior.AllowGet);
            }

            // Update hanya tabel utama
            var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == requestNo);
            if (request == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Request not found"
                }, JsonRequestBehavior.AllowGet);
            }

            request.Status = status;

            int ins = dbm.SaveChanges();

            if (ins > 0)
            {
                try
                {
                    SendEmailStatusUpdate(requestNo, status);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: " + ex.Message);
                    return Json(new
                    {
                        success = true,
                        message = status == "Rejected"
                                   ? "Request rejected successfully"
                                   : "Request status updated successfully (email failed to send)"
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    message = "Request updated and email sent successfully.",
                    requestNo
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to update request status"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        string GetEmailByNik(string nik)
        {
            if (string.IsNullOrEmpty(nik)) return "";

            return db.V_Users_Active
                          .Where(x => x.NIK == nik)
                          .Select(x => x.Email)
                          .FirstOrDefault();

        }

        string GetNameByNik(string nik)
        {
            if (string.IsNullOrEmpty(nik)) return "";
            return db.V_Users_Active
                .Where(x => x.NIK == nik)
                .Select(x => x.Name)
                .FirstOrDefault();
        }

        public void SendEmail(string idRequest, string action, string senderNik)
        {
            string filePath = Path.Combine(Server.MapPath("~/Emails/HC/Inventory/"), "notif.html");
            string mailText = System.IO.File.ReadAllText(filePath);

            var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(w => w.Request_No == idRequest);
            if (request == null) return;

            // === REQUESTER ===
            var user = db.V_Users_Active.FirstOrDefault(w => w.NIK == request.User_NIK);
            string username = user?.Name ?? request.User_Name;

            // ==========================================================
            // LOGIC TO DETERMINE DEPARTMENT ID
            // ==========================================================
            int? targetDeptId = null;

            // CHECK: If the user performing the action is 629.01.13, force Dept ID to 91
            if (senderNik == "629.01.13")
            {
                targetDeptId = 91;
            }
            else
            {
                // IF NOT, use the existing logic (based on Cart -> Section_To)
                var firstCart = dbm.HC_GA_Inventory_Carts
                    .FirstOrDefault(c => c.Request_No == idRequest);

                string sectionTo = firstCart?.Section_To_Code;

                if (!string.IsNullOrEmpty(sectionTo))
                {
                    var dept = dbm.HC_GA_Inventory_Dept
                        .FirstOrDefault(d => d.AX_Cost_ID == sectionTo);

                    if (dept != null)
                    {
                        targetDeptId = dept.ID;
                    }
                }
            }

            dynamic checker = null, approver = null, admin = null;

            // Query Checker/Approver/Admin based on the determined targetDeptId
            if (targetDeptId != null)
            {
                checker = dbm.HC_GA_Inventory_UserDept
                    .Where(u => u.Dept_ID == targetDeptId && u.Role == "CHECKER")
                    .Select(u => new { u.NIK, u.Name })
                    .FirstOrDefault();

                approver = dbm.HC_GA_Inventory_UserDept
                    .Where(u => u.Dept_ID == targetDeptId && u.Role == "APPROVER")
                    .Select(u => new { u.NIK, u.Name })
                    .FirstOrDefault();

                admin = dbm.HC_GA_Inventory_UserDept
                    .Where(u => u.Dept_ID == targetDeptId && u.Role == "ADMIN")
                    .Select(u => new { u.NIK, u.Name })
                    .FirstOrDefault();
            }

            // === PREPARE EMAIL INFOS ===
            string checkerMail = GetEmailByNik(checker?.NIK);
            string checkerName = GetNameByNik(checker?.NIK);

            string approverMail = GetEmailByNik(approver?.NIK);
            string approverName = GetNameByNik(approver?.NIK);

            string receivermail = "";
            string receiverName = "";
            string ccmail = "";
            string headMessage = "";

            // ==================================================
            //               EMAIL ROUTING
            // ==================================================
            if (action == "pending")
            {
                headMessage = "We would like to inform you that an inventory request has been submitted and requires your approval.";
                receivermail = user?.Email;
                receiverName = username;
            }
            else if (action == "prepare")
            {
                headMessage = "An inventory request needs your approval.";
                receivermail = checkerMail;
                receiverName = checkerName;

                ccmail = GetEmailByNik(admin?.NIK);
            }
            else if (action == "check")
            {
                headMessage = "An inventory request needs your approval.";
                receivermail = approverMail;
                receiverName = approverName;

                ccmail = GetEmailByNik(admin?.NIK);
            }
            else if (action == "approve")
            {
                headMessage = "Your request has been approved and submitted to the GA Division.";

                // === FIXED RECEIVER LIST ===
                receivermail = string.Join(";", new[]
                {
            "silvya.findari@niterragroup.com",
            "laifa.fathira@ngkbusi.com",
            "tuti.rusdiman@niterragroup.com"
        });

                receiverName = "All GA Team";
                ccmail = GetEmailByNik(admin?.NIK);
            }
            else if (action == "return")
            {
                headMessage = "Your request has been returned. Please review and resubmit.";

                receivermail = user?.Email;
                receiverName = username;

                ccmail = GetEmailByNik(admin?.NIK);
            }
            else if (action == "reject")
            {
                string reasonMessage = request.Reason_Message ?? "No reason provided.";

                headMessage = $"Your request has been rejected.Reason: {reasonMessage}";

                receivermail = user?.Email;
                receiverName = username;

                ccmail = GetEmailByNik(admin?.NIK);
            }

            // === FALLBACK RECEIVER ===
            if (string.IsNullOrEmpty(receivermail))
            {
                receivermail = !string.IsNullOrEmpty(ccmail)
                    ? ccmail
                    : "silvya.findari@niterragroup.com";
            }

            string linkMessage = $"https://portal.ngkbusi.com/NGKBusi/HC/Inventory/Inventory/{request.Request_No}";

            // === TEMPLATE REPLACEMENT ===
            mailText = mailText.Replace("##RequestNo##", request.Request_No);
            mailText = mailText.Replace("##ReceiverName##", receiverName);
            mailText = mailText.Replace("##Subject##", request.Subject);
            mailText = mailText.Replace("##DueDate##", request.Due_Date.ToString("dd MMM yyyy"));
            mailText = mailText.Replace("##UserNik##", username);
            mailText = mailText.Replace("##HeadMessage##", headMessage);
            mailText = mailText.Replace("##Link##", linkMessage);

            // === CART TABLE ROWS ===
            var cartNos = request.Cart_No.Split(',');
            var cartItems = dbm.HC_GA_Inventory_Carts
                .Where(c => cartNos.Contains(c.Cart_No))
                .ToList();

            string cartRowsHtml = "";
            int no = 1;

            foreach (var item in cartItems)
            {
                cartRowsHtml += $@"
        <tr>
            <td>{no}</td>
            <td>{item.Item_Name}</td>
            <td>{item.Qty_Request} {item.Unit_Request}</td>
            <td>{item.Budget_No} | {item.Budget_Desc}</td>
            <td>{item.Notes}</td>
        </tr>";
                no++;
            }

            mailText = mailText.Replace("##RepeatCartRows##", cartRowsHtml);

            // === SMTP ===
            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "GA Inventory Request");

            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, "100%NGKbusi!")
            };

            using (var message = new MailMessage()
            {
                From = senderEmail,
                Subject = $"[GA Inventory Request] {request.Request_No} | {request.Status}",
                Body = mailText,
                IsBodyHtml = true
            })
            {
                // multiple receivers
                foreach (var mail in receivermail.Split(';'))
                {
                    if (!string.IsNullOrWhiteSpace(mail))
                        message.To.Add(mail.Trim());
                }

                if (!string.IsNullOrEmpty(ccmail))
                    message.CC.Add(ccmail);

                smtp.Send(message);
            }
        }


        public void SendEmailStatusUpdate(string requestNo, string status)
        {
            string FilePath = Path.Combine(Server.MapPath("~/Emails/HC/Inventory/"), "status.html");
            string MailText = System.IO.File.ReadAllText(FilePath);

            var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(w => w.Request_No == requestNo);
            if (request == null) return;

            var user = db.Users.FirstOrDefault(u => u.NIK == request.User_NIK);
            string userName = user?.Name ?? "User";

            // === Replace basic placeholders ===
            MailText = MailText.Replace("##RequestNo##", request.Request_No);
            MailText = MailText.Replace("##Subject##", request.Subject);
            MailText = MailText.Replace("##DueDate##", request.Due_Date.ToString("dd MMM yyyy"));
            MailText = MailText.Replace("##UserNik##", userName);
            string displayStatus = status == "Update Pick Up" ? "Picked Up" : status;
            MailText = MailText.Replace("##Status##", displayStatus);

            // === Status note (use classic switch-case for compatibility) ===
            string statusNote;
            switch (status)
            {
                case "Ready to Pick":
                    statusNote = "Your items are ready to be picked up. Please collect it in KOPERASI.";
                    break;
                case "Update Pick Up":
                    statusNote = "Your items have been picked up.";
                    break;
                case "Complete":
                    statusNote = "Your request has been marked as complete. Thank you for using the GA Office Supply system.";
                    break;
                case "E-Voucher Issued":
                    statusNote = "Your invoice (e-voucher) has been successfully generated. Please proceed according to the provided details.";
                    break;
                case "On Order":
                    statusNote = "GA Team is currently ordering your item(s). Kindly wait until the stock becomes available.";
                    break;
                case "Rejected":
                    statusNote = "Unfortunately, your request has been rejected. Please check the message or contact GA for more info.";
                    break;
                default:
                    statusNote = "Please contact GA department for more information regarding your request status.";
                    break;
            }
            MailText = MailText.Replace("##StatusNote##", statusNote);

            // === Item list ===
            var cartNos = (request.Cart_No ?? "").Split(',').ToList();
            var allItems = GetCartByCartNosData(cartNos);

            var cartItems = new List<dynamic>();

            if (status == "Ready to Pick")
            {
                cartItems = allItems
                            .Where(x => x.isReady == true)
                            .ToList();
            }
            else if (status == "Complete")
            {
                cartItems = allItems
                            .Where(x => x.isReady == true && x.Pick_Date != null)
                            .ToList();
            }
            else
            {
                cartItems = allItems
                            .Where(x => x.isReady == true)
                            .ToList();
            }
            int no = 1;
            string cartRowsHtml = "";

            foreach (var item in cartItems)
            {
                string pickDateText = item.Pick_Date != null
    ? $"Picked Up: {Convert.ToDateTime(item.Pick_Date).ToString("dd MMM yyyy")}"
    : "Ready to Pick";


                cartRowsHtml += $@"
        <tr>
            <td>{no}</td>
            <td>{item.Item_Name}</td>
            <td>{item.Qty_Request} {item.Unit_Request}</td>
            <td>{item.Budget}</td>
            <td>{item.Notes}</td>
            <td>{pickDateText}</td>
        </tr>";
                no++;
            }
            MailText = MailText.Replace("##RepeatCartRows##", cartRowsHtml);

            // === Email routing ===
            // --- Email sender ---
            var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "GA Inventory Request");

            // --- Email list ---
            string requesterEmail = GetEmailByNik(request.User_NIK) ?? "no-reply@ngkbusi.com";

            // List penerima utama dan CC (lebih fleksibel)
            var toList = new List<string>();
            var ccList = new List<string>();

            // --- ROUTING LOGIC ---
            switch (status)
            {
                case "Complete":
                    // To user (creator)
                    toList.Add(requesterEmail);

                    // Extra GA recipients
                    toList.Add("silvya.findari@niterragroup.com");
                    toList.Add("laifa.fathira@niterragroup.com");
                    break;

                case "Waiting for Approval":
                case "Pending":
                case "Pending Approval":
                    // Kirim ke GA approver
                    //toList.Add("adisti.putri@ngkbusi.com");

                    // CC creator agar tahu requestnya pending
                    ccList.Add(requesterEmail);
                    break;

                default:
                    // Default → kirim ke user
                    toList.Add(requesterEmail);
                    break;
            }

            // Optional → CC GA fixed
            //ccList.Add("adisti.putri@niterragroup.com");

            // --- Prepare SMTP ---
            var smtp = new SmtpClient
            {
                Host = "ngkbusi.com",
                Port = 587,
                EnableSsl = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, "100%NGKbusi!")
            };

            // --- Send email ---
            using (var mail = new MailMessage())
            {
                mail.From = senderEmail;
                mail.Subject = $"[GA Inventory Confirmation] {requestNo} | {status}";
                mail.Body = MailText;
                mail.IsBodyHtml = true;

                foreach (var to in toList.Distinct())
                    mail.To.Add(new MailAddress(to));

                foreach (var cc in ccList.Distinct())
                    mail.CC.Add(new MailAddress(cc));

                smtp.Send(mail);
            }

        }

        private List<dynamic> GetCartByCartNosData(List<string> cartNos)
        {
            var result = (from cart in dbm.HC_GA_Inventory_Carts
                          join item in dbm.HC_GA_Inventory_Item_List
                              on cart.Item_Id equals item.ID
                          join confirmGrouped in
                              (from conf in dbm.HC_GA_Inventory_Request_Confirm
                               group conf by conf.Cart_No into g
                               select g.OrderByDescending(x => x.ID).FirstOrDefault())
                          on cart.Cart_No equals confirmGrouped.Cart_No into confirmJoin
                          from conf in confirmJoin.DefaultIfEmpty()
                          where cartNos.Contains(cart.Cart_No)
                          select new
                          {
                              cart.Cart_No,
                              cart.Item_Name,
                              cart.Unit_Request,
                              Qty_Request = cart.Qty_Request,
                              Budget = cart.Budget_No + " | " + cart.Budget_Desc,
                              Qty_Acc = conf != null ? conf.Qty_Acc : 0,
                              cart.Notes,
                              User_NIK_Acc = conf != null ? conf.User_NIK_Acc : null,
                              User_NIK_Acc_Name = conf != null ? conf.User_NIK_Acc_Name : null,
                              Manager_Name = conf.Manager_Name,
                              Date_Prepare = conf.Date_Prepare,
                              Date_Approve = conf.Date_Approve,
                              cart.isReady,
                              cart.Pick_Date
                          }).ToList<dynamic>(); // ← biar bisa dynamic access di email

            return result;
        }

        [HttpPost]
        public JsonResult UploadImage(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png" };
                    if (!allowedExt.Contains(ext))
                        return Json(new { success = false, message = "Only JPG and PNG allowed." });

                    var fileName = Guid.NewGuid().ToString() + ext;
                    var savePath = System.Web.Hosting.HostingEnvironment.MapPath("~/NGKBusi/Files/HC/Inventory");

                    //var savePath = @"C:\inetpub\wwwroot\ngk-portal\NGKBusi\Files\HC\Inventory";

                    if (!Directory.Exists(savePath))
                        Directory.CreateDirectory(savePath);

                    var fullPath = Path.Combine(savePath, fileName);
                    file.SaveAs(fullPath);

                    return Json(new { success = true, fileName = fileName });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            return Json(new { success = false, message = "No file uploaded" });
        }

        // ITEM ORDER CONTROLLER
        public JsonResult GetRequestItems()
        {
            var allRequests = dbm.HC_GA_Inventory_Requests
                .Where(r => r.Status == "Approved")
                .ToList();

            var allCarts = dbm.HC_GA_Inventory_Carts.ToList();
            var allItems = dbm.HC_GA_Inventory_Item_List.ToList();
            var confirms = dbm.HC_GA_Inventory_Request_Confirm.ToList();

            // Hitung qty berdasarkan status
            //var onPrepareQty = confirms.Where(c => c.Status == "On Prepare")
            //    .GroupBy(c => c.Item_Id)
            //    .ToDictionary(g => g.Key, g => g.Sum(c => c.Qty_Acc));

            //var readyQty = confirms.Where(c => c.Status == "Ready to Pick")
            //    .GroupBy(c => c.Item_Id)
            //    .ToDictionary(g => g.Key, g => g.Sum(c => c.Qty_Acc));

            //var onOrderQty = confirms.Where(c => c.Status == "On Order")
            //    .GroupBy(c => c.Item_Id)
            //    .ToDictionary(g => g.Key, g => g.Sum(c => c.Qty_Acc));

            //var waitingQty = confirms.Where(c => c.Status == "Waiting for Approval")
            //    .GroupBy(c => c.Item_Id)
            //    .ToDictionary(g => g.Key, g => g.Sum(c => c.Qty_Acc));

            //var approvedQty = confirms.Where(c => c.Status == "Approved")
            //    .GroupBy(c => c.Item_Id)
            //    .ToDictionary(g => g.Key, g => g.Sum(c => c.Qty_Acc));

            var result = new List<object>();

            foreach (var request in allRequests)
            {
                var cartNos = (request.Cart_No ?? "")
                    .Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                foreach (var cartNo in cartNos)
                {
                    var cart = allCarts.FirstOrDefault(c => c.Cart_No == cartNo);
                    if (cart == null) continue;

                    var item = allItems.FirstOrDefault(i => i.ID == cart.Item_Id);
                    if (item == null) continue;

                    var itemId = cart.Item_Id;
                    var itemQty = item.Qty;
                    var previewQty = itemQty - cart.Qty_Request;

                    // Hanya tampilkan jika stok kurang dari permintaan
                    if (itemQty >= cart.Qty_Request) continue;

                    result.Add(new
                    {
                        request.Request_No,
                        request.Subject,
                        request.Status,
                        request.Timestamps,
                        Due_Date = request.Due_Date.ToString("yyyy-MM-dd"),

                        cart.Cart_No,
                        cart.Item_Id,
                        cart.Item_Name,
                        cart.Qty_Request,
                        Qty = cart.Qty_Request + " " + cart.Unit_Request,
                        Budget = cart.Budget_No + " | " + cart.Budget_Desc,
                        cart.Unit_Request,
                        cart.User_Name,
                        cart.User_NIK,
                        cart.Dept_Name,
                        cart.Notes,
                        cart.isReady,

                        Qty_Stock = itemQty + " " + item.Unit,
                        Qty_Preview = previewQty + " " + item.Unit,
                        Qty_Preview_Int = previewQty
                    });
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetStampStatus(string requestNo)
        {
            var request = dbm.HC_GA_Inventory_Requests_Approval.FirstOrDefault(r => r.Request_No == requestNo);

            if (request == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                PrepareBy = request.Prepared_Name,
                PrepareDate = request.Prepared_Date,
                CheckedBy = request.Checked_Name,
                CheckedDate = request.Checked_Date,
                ApproveBy = request.Approved_Name,
                ApproveDate = request.Approved_Date,
            }, JsonRequestBehavior.AllowGet);
        }


        //[HttpGet]
        //public JsonResult GetStampStatusOld(string requestNo)
        //{
        //    var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == requestNo);

        //    if (request == null)
        //        return Json(null, JsonRequestBehavior.AllowGet);

        //    return Json(new
        //    {
        //        PrepareBy = request.Prepared_Name,
        //        PrepareDate = request.Prepared_Date,
        //        CheckedBy = request.Checked_Name, 
        //        CheckedDate = request.Checked_Date, 
        //        ApproveBy = request.Approved_Name,
        //        ApproveDate = request.Approved_Date,
        //    }, JsonRequestBehavior.AllowGet);
        //}
        [HttpGet]
        public JsonResult GetStampStatusOrder(string orderNo)
        {
            var request = dbm.HC_GA_Inventory_Order.FirstOrDefault(r => r.Order_No == orderNo);

            if (request == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                PrepareBy = request.Prepare_Name,
                PrepareDate = request.Prepare_Date,
                CheckBy = request.Check_Name,
                CheckDate = request.Check_Date,
                ApproveBy = request.Approve_Name,
                ApproveDate = request.Approve_Date,
                DispatchBy = request.Dispatcher_Name,
                DispatchDate = request.Dispatcher_Date,
                ReceiveBy = request.Receiver_Name,
                ReceiveDate = request.Receiver_Date,
                RejectMessage = request.Reject_Message,
                Status = request.Status
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveStampStatus()
        {
            var requestNo = Request["Request_No"];
            var action = Request["Action"];
            var userNik = Request["User_NIK"];
            var userName = Request["User_Name"];

            var userRoles = dbm.HC_GA_Inventory_UserDept
                                    .Where(x => x.NIK == userNik)
                                    .Select(x => x.Role)
                                    .ToList();

            var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == requestNo);

            if (request == null)
            {
                return Json(new { success = false, message = "Request not found" });
            }

            // CEK / BUAT APPROVAL ROW
            var approval = dbm.HC_GA_Inventory_Requests_Approval
                                  .FirstOrDefault(r => r.Request_No == requestNo);

            if (approval == null)
            {
                approval = new HC_GA_Inventory_Requests_Approval();
                approval.Request_No = requestNo;
                dbm.HC_GA_Inventory_Requests_Approval.Add(approval);
            }

            // ============================================================
            // PROCESS ACTION
            // ============================================================
            string emailStatus = ""; // <-- Untuk SendEmail
            string msg = "";         // Optional

            if (action == "prepare")
            {
                if (!userRoles.Contains("ADMIN"))
                    return Json(new { success = false, message = "You are not authorized to prepare this request." });

                approval.Prepared_NIK = userNik;
                approval.Prepared_Name = userName;
                approval.Prepared_Date = DateTime.Now;

                request.Status = "Waiting for Check";

                emailStatus = "prepare";
                msg = "Prepared completed.";
            }
            else if (action == "check")
            {
                if (!userRoles.Contains("CHECKER"))
                    return Json(new { success = false, message = "You are not authorized to check this request." });

                if (string.IsNullOrEmpty(approval.Prepared_NIK))
                    return Json(new { success = false, message = "Request must be prepared before checking." });

                approval.Checked_NIK = userNik;
                approval.Checked_Name = userName;
                approval.Checked_Date = DateTime.Now;

                request.Status = "Waiting for Approval";

                emailStatus = "check";
                msg = "Check completed.";
            }
            else if (action == "approve")
            {
                if (!userRoles.Contains("APPROVER"))
                    return Json(new { success = false, message = "You are not authorized to approve this request." });

                if (string.IsNullOrEmpty(approval.Checked_NIK))
                    return Json(new { success = false, message = "Request must be checked before approval." });

                approval.Approved_NIK = userNik;
                approval.Approved_Name = userName;
                approval.Approved_Date = DateTime.Now;

                request.Status = "Approved";

                var relatedCarts = dbm.HC_GA_Inventory_Carts
                                      .Where(c => c.Request_No == requestNo)
                                      .ToList();

                foreach (var cart in relatedCarts)
                {
                    var item = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(i => i.ID == cart.Item_Id);
                    if (item != null)
                    {
                        cart.Price = item.Price; 
                    }
                }

                emailStatus = "approve";
                msg = "Approval completed.";
            }
            else if (action == "return")
            {
                var reason = Request["Reason"];

                approval.Prepared_NIK = null;
                approval.Prepared_Name = null;
                approval.Prepared_Date = null;

                approval.Checked_NIK = null;
                approval.Checked_Name = null;
                approval.Checked_Date = null;

                approval.Approved_NIK = null;
                approval.Approved_Name = null;
                approval.Approved_Date = null;

                request.Status = "Return";
                request.Reason_Message = reason;

                var relatedCarts = dbm.HC_GA_Inventory_Carts
                                      .Where(c => c.Request_No == requestNo)
                                      .ToList();

                foreach (var cart in relatedCarts)
                {
                    cart.Status = 0;
                }

                emailStatus = "return";
                msg = "Request has been returned.";
            }
            else if (action == "reject")
            {
                var reason = Request["Reason"];
                var newStatus = Request["NewStatus"];

                if (newStatus == "Rejected by Checker")
                {
                    approval.Prepared_NIK = null;
                    approval.Prepared_Name = null;
                    approval.Prepared_Date = null;
                }
                else if (newStatus == "Rejected by Approver")
                {
                    approval.Prepared_NIK = null;
                    approval.Prepared_Name = null;
                    approval.Prepared_Date = null;

                    approval.Checked_NIK = null;
                    approval.Checked_Name = null;
                    approval.Checked_Date = null;
                }

                request.Status = newStatus;
                request.Reason_Message = reason;

                emailStatus = "reject";
                msg = "Request has been rejected.";
            }
            else
            {
                return Json(new { success = false, message = "Invalid action." });
            }

            dbm.SaveChanges();

            try
            {
                var _currUser = ((ClaimsIdentity)User.Identity);
                string currentUserNik = _currUser.GetUserId() ?? "";

                SendEmail(requestNo, emailStatus, currentUserNik);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = true,
                    message = msg + " But email failed: " + ex.Message
                });
            }

            return Json(new { success = true, message = msg + " Email sent." });
        }
        [HttpPost]
        public JsonResult SaveStampStatusOrder()
        {
            var orderNo = Request["Order_No"];
            var action = Request["Action"];
            var userNik = Request["User_NIK"];
            var userName = Request["User_Name"];
            var attn = Request["Attn"];
            var subject = Request["Subject"];
            var rejectMessage = Request["Reject_Message"];
            var rejectBy = Request["Reject_By"];

            try
            {
                if (string.IsNullOrEmpty(orderNo) || string.IsNullOrEmpty(action))
                {
                    return Json(new { success = false, message = "Invalid input." });
                }

                var orders = dbm.HC_GA_Inventory_Order
                    .Where(x => x.Order_No == orderNo)
                    .ToList();

                if (!orders.Any())
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                // Logic sesuai action
                if (action == "prepare")
                {
                    foreach (var order in orders)
                    {
                        order.Prepare_NIK = userNik;
                        order.Prepare_Name = userName;
                        order.Prepare_Date = DateTime.Now;
                        order.Attn = attn;
                        order.Subject = subject;
                        order.Status = "Pending Check";
                    }

                    // Ambil semua request no unik dari order
                    var requestNos = orders
                        .Select(x => x.Request_No)
                        .Distinct()
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    foreach (var reqNo in requestNos)
                    {
                        var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == reqNo);
                        if (request != null)
                        {
                            request.Status = "On Order";
                        }

                        var confirm = dbm.HC_GA_Inventory_Request_Confirm.FirstOrDefault(c => c.Request_No == reqNo);
                        if (confirm != null)
                        {
                            confirm.Status = "On Order";
                        }
                    }
                }
                else if (action == "check")
                {
                    if (orders.Any(o => string.IsNullOrEmpty(o.Prepare_NIK) || string.IsNullOrEmpty(o.Prepare_Name) || o.Prepare_Date == null))
                    {
                        return Json(new { success = false, message = "Please prepare the order first." });
                    }

                    foreach (var order in orders)
                    {
                        order.Check_NIK = userNik;
                        order.Check_Name = userName;
                        order.Check_Date = DateTime.Now;
                        order.Status = "Pending Approve";
                    }
                }
                else if (action == "approve")
                {
                    if (orders.Any(o => string.IsNullOrEmpty(o.Check_NIK) || string.IsNullOrEmpty(o.Check_Name) || o.Check_Date == null))
                    {
                        return Json(new { success = false, message = "Please check the order first." });
                    }

                    foreach (var order in orders)
                    {
                        order.Approve_NIK = userNik;
                        order.Approve_Name = userName;
                        order.Approve_Date = DateTime.Now;
                        order.Status = "Order Approved";
                    }
                }
                else if (action == "reject")
                {
                    foreach (var order in orders)
                    {
                        if (rejectBy == "check")
                        {
                            order.Status = "Rejected by Checker";
                            order.Reject_Message = rejectMessage;
                            order.Check_NIK = userNik;
                            order.Check_Name = userName;
                            order.Check_Date = DateTime.Now;
                        }
                        else if (rejectBy == "approve")
                        {
                            order.Status = "Rejected by Approver";
                            order.Reject_Message = rejectMessage;
                            order.Approve_NIK = userNik;
                            order.Approve_Name = userName;
                            order.Approve_Date = DateTime.Now;
                        }
                        else
                        {
                            order.Status = "Rejected";
                        }
                    }

                    // Tetap update juga status di tabel request & confirm
                    var requestNos = orders
                        .Select(x => x.Request_No)
                        .Distinct()
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    foreach (var reqNo in requestNos)
                    {
                        var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == reqNo);
                        if (request != null)
                        {
                            request.Status = "Rejected";
                            request.Reason_Message = rejectMessage;
                        }

                        var confirm = dbm.HC_GA_Inventory_Request_Confirm.FirstOrDefault(c => c.Request_No == reqNo);
                        if (confirm != null)
                        {
                            confirm.Status = "Rejected";
                            confirm.Message = rejectMessage;
                        }
                    }
                }

                else
                {
                    return Json(new { success = false, message = "Invalid action." });
                }

                dbm.SaveChanges();
                SendEmailOrder(orderNo, action);

                return Json(new { success = true, message = $"Successfully {action}ed." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public JsonResult SaveOrderHeader(string orderNo, string attn, string subject)
        {
            if (string.IsNullOrEmpty(orderNo))
            {
                return Json(new { success = false, message = "Order number is required." });
            }

            var orders = dbm.HC_GA_Inventory_Order
                            .Where(x => x.Order_No == orderNo)
                            .ToList();

            if (!orders.Any())
            {
                return Json(new { success = false, message = "Order not found." });
            }

            // Simpan Attn dan Subject ke semua item dalam order tersebut
            foreach (var order in orders)
            {
                order.Attn = attn;
                order.Subject = subject;
            }

            dbm.SaveChanges();

            return Json(new { success = true, message = "Header saved successfully." });
        }

        [HttpGet]
        public JsonResult GetOrderDetailsByOrderNo(string orderNo)
        {
            if (string.IsNullOrEmpty(orderNo))
            {
                return Json(new { success = false, message = "Order No is required." }, JsonRequestBehavior.AllowGet);
            }

            var itemsRaw = dbm.HC_GA_Inventory_Order
                  .Where(x => x.Order_No == orderNo)
                  .ToList();

            var items = itemsRaw.Select(x => new
            {
                x.ID,
                x.Order_No,
                x.Type,
                x.Request_No,
                x.Cart_No,
                x.Attn,
                x.Subject,
                x.Item_Order_Id,
                x.Item_Order_Name,
                x.Item_Order_Qty,
                x.Item_Order_Unit,
                x.Requestor_NIK,
                x.Requestor_Name,
                x.Requestor_Dept,
                Timestamps = x.Timestamps.ToString("yyyy-MM-dd"),
                x.Status,
                x.Qty_Kop,
                x.Qty_GA,
                x.Dispatcher_NIK,
                x.Dispatcher_Name,
                x.Dispatcher_Date,
                x.Receiver_NIK,
                x.Receiver_Name,
                x.Receiver_Date,
            }).ToList();

            return Json(new { success = true, data = items }, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public ActionResult CreateOrder(List<HC_GA_Inventory_Order> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "Cart is empty.";
                return RedirectToAction("Cart");
            }

            string orderNo = GenerateOrderNumber();

            // Collect all unique request numbers to update.
            List<string> requestNumbersToUpdate = new List<string>();

            foreach (var item in cartItems)
            {
                // Add the new order item to the database.
                dbm.HC_GA_Inventory_Order.Add(new HC_GA_Inventory_Order
                {
                    Order_No = orderNo,
                    Type = item.Type,
                    Status = "Pending Prepare", // Set initial status
                    Request_No = item.Request_No,
                    Cart_No = item.Cart_No,
                    Item_Order_Id = item.Item_Order_Id,
                    Item_Order_Name = item.Item_Order_Name,
                    Item_Order_Qty = item.Item_Order_Qty,
                    Item_Order_Unit = item.Item_Order_Unit,
                    Requestor_NIK = item.Requestor_NIK,
                    Requestor_Name = item.Requestor_Name,
                    Requestor_Dept = item.Requestor_Dept,
                    Timestamps = DateTime.Now
                });

                // Split the comma-separated string and add individual request numbers to the list.
                if (!string.IsNullOrEmpty(item.Request_No))
                {
                    var requestNos = item.Request_No.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(s => s.Trim())
                                                   .ToList();
                    requestNumbersToUpdate.AddRange(requestNos);
                }
            }

            dbm.SaveChanges(); // Save the new order items first.

            // Update the status for all unique request numbers.
            var uniqueRequestNos = requestNumbersToUpdate.Distinct().ToList();

            foreach (var reqNo in uniqueRequestNos)
            {
                // Find and update status in HC_GA_Inventory_Requests table.
                var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == reqNo);
                if (request != null)
                {
                    request.Status = "On Order";
                }

                // Find and update status in HC_GA_Inventory_Request_Confirm table.
                var confirms = dbm.HC_GA_Inventory_Request_Confirm.Where(c => c.Request_No == reqNo).ToList();
                foreach (var confirm in confirms)
                {
                    confirm.Status = "On Order";
                }
            }

            dbm.SaveChanges(); // Save all the status updates.

            return Json(new { success = true, orderNo = orderNo });
        }
        private string GenerateOrderNumber()
        {
            var today = DateTime.Today;
            string prefix = "ODR-" + today.ToString("yyMMdd");

            // Cari order terakhir dengan format ODR-250722-xxx
            var lastOrder = dbm.HC_GA_Inventory_Order
                .Where(o => o.Order_No.StartsWith(prefix))
                .OrderByDescending(o => o.Order_No)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastOrder != null)
            {
                var parts = lastOrder.Order_No.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNo))
                {
                    nextNumber = lastNo + 1;
                }
            }

            return $"{prefix}-{nextNumber.ToString("D3")}";
        }

        [HttpGet]
        public JsonResult GetAllOrderDetails()
        {
            try
            {
                var list = dbm.HC_GA_Inventory_Order
                    .Select(x => new
                    {
                        x.ID,
                        x.Order_No,
                        x.Type,
                        x.Request_No,
                        x.Cart_No,
                        x.Item_Order_Id,
                        x.Item_Order_Name,
                        x.Item_Order_Qty,
                        x.Item_Order_Unit,
                        x.Status,
                        x.Requestor_NIK,
                        x.Requestor_Name,
                        x.Requestor_Dept,
                        Timestamps = x.Timestamps
                    })
                    .ToList();

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult SaveQtyKop(string orderNo, List<QtyInput> items)
        {
            if (items == null || !items.Any())
            {
                return Json(new { success = false, message = "No data received." });
            }

            if (string.IsNullOrEmpty(orderNo))
            {
                return Json(new { success = false, message = "Order number is required." });
            }

            try
            {
                var itemIds = items.Select(i => i.Item_Order_Id).ToList();

                var matchedItems = dbm.HC_GA_Inventory_Order
                                         .Where(x => itemIds.Contains(x.Item_Order_Id) && x.Order_No == orderNo)
                                         .ToList();

                if (!matchedItems.Any())
                {
                    return Json(new { success = false, message = "No matching items found in database." });
                }

                var groupedInput = items
                     .GroupBy(i => i.Item_Order_Id)
                     .ToDictionary(g => g.Key, g => g.Last());

                List<string> requestNumbersToUpdate = new List<string>();

                foreach (var dbItem in matchedItems)
                {
                    if (groupedInput.TryGetValue(dbItem.Item_Order_Id, out var input))
                    {
                        dbItem.Qty_Kop = input.Qty_Kop;
                        dbItem.Dispatcher_NIK = input.Dispatcher_NIK;
                        dbItem.Dispatcher_Name = input.Dispatcher_Name;
                        dbItem.Dispatcher_Date = DateTime.Now;
                        dbItem.Status = "Dispatcher Confirmed";

                        if (!string.IsNullOrEmpty(dbItem.Request_No))
                        {
                            var requestNos = dbItem.Request_No.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                             .Select(s => s.Trim())
                                                             .ToList();
                            requestNumbersToUpdate.AddRange(requestNos);
                        }
                    }
                }

                dbm.SaveChanges();

                var uniqueRequestNos = requestNumbersToUpdate.Distinct().ToList();

                SendEmailOrder2(orderNo, "dispatch");

                return Json(new { success = true, message = "Qty_Kop and status updated successfully." });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "-";
                return Json(new { success = false, message = "Error: " + ex.Message + " | Inner: " + inner });
            }
        }

        [HttpPost]
        public ActionResult SaveQtyGA(string orderNo, List<QtyGAInput> items)
        {
            if (items == null || !items.Any())
            {
                return Json(new { success = false, message = "No data received." });
            }

            if (string.IsNullOrEmpty(orderNo))
            {
                return Json(new { success = false, message = "Order number is required." });
            }

            try
            {
                var itemIds = items.Select(i => i.Item_Order_Id).ToList();

                var matchedItems = dbm.HC_GA_Inventory_Order
                                         .Where(x => itemIds.Contains(x.Item_Order_Id) && x.Order_No == orderNo)
                                         .ToList();

                if (!matchedItems.Any())
                {
                    return Json(new { success = false, message = "No matching items found in database." });
                }

                var groupedInput = items
                     .GroupBy(i => i.Item_Order_Id)
                     .ToDictionary(g => g.Key, g => new
                     {
                         TotalQtyGA = g.Sum(x => x.Qty_GA),
                         LastInput = g.Last()
                     });

                // Step 1: Collect all unique Request_No from the order items
                List<string> requestNumbersToUpdate = new List<string>();

                foreach (var dbItem in matchedItems)
                {
                    if (groupedInput.TryGetValue(dbItem.Item_Order_Id, out var inputGroup))
                    {
                        // Update data in HC_GA_Inventory_Order table
                        dbItem.Qty_GA = inputGroup.TotalQtyGA;
                        dbItem.Receiver_NIK = inputGroup.LastInput.Receiver_NIK;
                        dbItem.Receiver_Name = inputGroup.LastInput.Receiver_Name;
                        dbItem.Receiver_Date = DateTime.Now;
                        dbItem.Status = "Order Complete"; // This is correct, as it's for the individual order item

                        // Update stock in HC_GA_Inventory_Item_List
                        var stockItem = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(x => x.ID == dbItem.Item_Order_Id);
                        if (stockItem != null)
                        {
                            stockItem.Qty = (stockItem.Qty) + inputGroup.TotalQtyGA;
                        }

                        // Add Request_No to the list for status update
                        if (!string.IsNullOrEmpty(dbItem.Request_No))
                        {
                            var requestNos = dbItem.Request_No.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                             .Select(s => s.Trim())
                                                             .ToList();
                            requestNumbersToUpdate.AddRange(requestNos);
                        }
                    }
                }

                dbm.SaveChanges(); // Save changes to Order and Item_List tables.

                // Step 2: Update the status for all unique requests
                var uniqueRequestNos = requestNumbersToUpdate.Distinct().ToList();

                foreach (var reqNo in uniqueRequestNos)
                {
                    var requestToUpdate = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == reqNo);

                    if (requestToUpdate != null)
                    {
                        // REVISION: The status of the main request will now be set to "Order Complete."
                        requestToUpdate.Status = "Order Complete";
                    }
                }

                // Step 3: Save changes to the Requests table
                dbm.SaveChanges();

                SendEmailOrder2(orderNo, "complete");

                return Json(new { success = true, message = "Qty_GA saved and stock and request status updated." });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "-";
                return Json(new { success = false, message = "Error: " + ex.Message + " | Inner: " + inner });
            }
        }

        public bool SendEmailOrder(string orderNo, string action)
        {
            try
            {
                // ... (kode untuk membaca file, mengganti placeholder, dll.)
                string FilePath = Path.Combine(Server.MapPath("~/Emails/HC/Inventory/"), "order.html");
                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                // ... (rest of your existing code to populate MailText)
                var order = dbm.HC_GA_Inventory_Order
                    .Where(c => c.Order_No == orderNo)
                    .OrderByDescending(c => c.ID)
                    .FirstOrDefault();
                string actionLink = $"https://portal.ngkbusi.com//NGKBusi/HC/Inventory/_DetailOrder?orderNo={order.Order_No}";
                MailText = MailText.Replace("##ActionLink##", actionLink);

                MailText = MailText.Replace("##OrderNo##", order.Order_No);
                MailText = MailText.Replace("##Subject##", order.Subject);
                MailText = MailText.Replace("##Attn##", order.Attn);
                MailText = MailText.Replace("##PrepareNIK##", order.Requestor_NIK);
                MailText = MailText.Replace("##PrepareName##", order.Requestor_Name);
                MailText = MailText.Replace("##Status##", action);

                string messageHtml = "";
                if ((order.Status == "Rejected by Checker" || order.Status == "Rejected by Approver") && !string.IsNullOrWhiteSpace(order.Reject_Message))
                {
                    messageHtml = $"<p style='color:red;'>Message: {order.Reject_Message}</p>";
                }
                MailText = MailText.Replace("##Message##", messageHtml);

                string statusNote = "";
                string subjectStatus = "";

                switch (action)
                {
                    case "prepare":
                        statusNote = "We would like to inform you that a new inventory order has been submitted and requires your preparation.";
                        subjectStatus = "Waiting for Check";
                        break;
                    case "check":
                        statusNote = "We would like to inform you that a new inventory order has been submitted and requires your review as a checker.";
                        subjectStatus = "Waiting for Approval";
                        break;
                    case "approve":
                        statusNote = "We would like to inform you that a new inventory order has been submitted and requires your approval.";
                        subjectStatus = "Waiting for Dispatch to Prepare";
                        break;
                    case "reject":
                        statusNote = "Unfortunately, your request has been rejected. Please check the provided message or contact GA for more details.";
                        subjectStatus = "Order Rejected";
                        break;
                    default:
                        statusNote = "Please contact the GA department for more information regarding the status of your request.";
                        subjectStatus = action;
                        break;
                }

                MailText = MailText.Replace("##HeadMessage##", statusNote);

                var orders = dbm.HC_GA_Inventory_Order
                  .Where(c => c.Order_No == orderNo)
                  .OrderBy(c => c.ID)
                  .ToList();

                int totalQty = orders.Sum(x => x.Item_Order_Qty);
                MailText = MailText.Replace("##TotalQty##", totalQty.ToString());

                int no = 1;
                string cartRowsHtml = "";
                foreach (var item in orders)
                {
                    cartRowsHtml += $@"
        <tr>
            <td>{no}</td>
            <td>{item.Item_Order_Name}</td>
            <td>{item.Type}</td>
            <td style=""text-align:center;"">{item.Item_Order_Qty}</td>
            <td>{item.Item_Order_Unit}</td>
        </tr>";
                    no++;
                }

                MailText = MailText.Replace("##RepeatCartRows##", cartRowsHtml);

                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "GA Inventory Request");
                MailAddress receiverEmail;

                if (action == "Waiting for Approval")
                {
                    receiverEmail = new MailAddress("adisti.putri@ngkbusi.com");
                }
                else
                {
                    receiverEmail = new MailAddress("adisti.putri@niterragroup.com");
                }

                var ccEmail = new MailAddress("adisti.putri@niterragroup.com");

                var smtp = new SmtpClient
                {
                    Host = "ngkbusi.com",
                    Port = 587,
                    EnableSsl = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, "100%NGKbusi!")
                };

                using (var message = new MailMessage(senderEmail, receiverEmail)
                {
                    Subject = $"[GA Inventory Order Confirmation] {orderNo} | {subjectStatus}",
                    Body = MailText,
                    IsBodyHtml = true
                })
                {
                    message.CC.Add(ccEmail);
                    smtp.Send(message);
                }
                return true;
            }
            catch (Exception)
            {
                // Log the exception for debugging purposes
                // Console.WriteLine(ex.ToString());
                return false;
            }
        }
        public bool SendEmailOrder2(string orderNo, string action)
        {
            try
            {
                // Path to the new email template
                string FilePath = Path.Combine(Server.MapPath("~/Emails/HC/Inventory/"), "order2.html");
                StreamReader str = new StreamReader(FilePath);
                string MailText = str.ReadToEnd();
                str.Close();

                // Get a list of all items for the given order number
                var orders = dbm.HC_GA_Inventory_Order
                    .Where(c => c.Order_No == orderNo)
                    .OrderBy(c => c.ID)
                    .ToList();

                // If no orders are found, stop the process
                if (!orders.Any())
                {
                    return false;
                }

                var firstOrder = orders.FirstOrDefault();
                string actionLink = $"https://portal.ngkbusi.com/NGKBusi/HC/Inventory/_DetailOrder?orderNo={firstOrder.Order_No}";
                MailText = MailText.Replace("##ActionLink##", actionLink);

                // Replace the common placeholders
                MailText = MailText.Replace("##OrderNo##", firstOrder.Order_No);
                MailText = MailText.Replace("##Subject##", firstOrder.Subject);
                MailText = MailText.Replace("##Attn##", firstOrder.Attn);
                MailText = MailText.Replace("##PrepareNIK##", firstOrder.Requestor_NIK);
                MailText = MailText.Replace("##PrepareName##", firstOrder.Requestor_Name);

                string headMessage = "";
                string subjectStatus = "";
                MailAddress receiverEmail;

                // Logic based on the action parameter
                switch (action)
                {
                    case "dispatch":
                        headMessage = "The requested items have been prepared and dispatched by the GA team. They are ready to be received and confirmed.";
                        subjectStatus = "Waiting for Receive Confirmation";
                        receiverEmail = new MailAddress("adisti.putri@ngkbusi.com");
                        break;
                    case "complete":
                        headMessage = "Your inventory order has been successfully completed. You can now view the final details.";
                        subjectStatus = "Order Complete";
                        receiverEmail = new MailAddress("adisti.putri@ngkbusi.com");
                        break;
                    default:
                        return false;
                }

                MailText = MailText.Replace("##HeadMessage##", headMessage);

                // Build the table rows with Qty_Kop and Qty_GA
                string cartRowsHtml = "";
                int no = 1;
                int totalQtyRequest = 0;
                int totalQtyPrepare = 0;
                int totalQtyReceive = 0;

                foreach (var item in orders)
                {
                    cartRowsHtml += $@"
            <tr>
                <td>{no}</td>
                <td>{item.Item_Order_Name}</td>
                <td>{item.Type}</td>
                <td style=""text-align:center;"">{item.Item_Order_Qty}</td>
                <td>{item.Item_Order_Unit}</td>
                <td style=""text-align:center;"">{item.Qty_Kop ?? 0}</td>
                <td style=""text-align:center;"">{item.Qty_GA ?? 0}</td>
            </tr>";
                    no++;
                    totalQtyRequest += item.Item_Order_Qty;
                    totalQtyPrepare += item.Qty_Kop ?? 0;
                    totalQtyReceive += item.Qty_GA ?? 0;
                }

                MailText = MailText.Replace("##RepeatCartRows##", cartRowsHtml);
                MailText = MailText.Replace("##TotalQtyRequest##", totalQtyRequest.ToString());
                MailText = MailText.Replace("##TotalQtyPrepare##", totalQtyPrepare.ToString());
                MailText = MailText.Replace("##TotalQtyReceive##", totalQtyReceive.ToString());
                MailText = MailText.Replace("##Message##", "");

                var senderEmail = new MailAddress("ngkportal-notification@ngkbusi.com", "GA Inventory Request");
                var ccEmail = new MailAddress("adisti.putri@niterragroup.com");

                var smtp = new SmtpClient
                {
                    Host = "ngkbusi.com",
                    Port = 587,
                    EnableSsl = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, "100%NGKbusi!")
                };

                using (var message = new MailMessage(senderEmail, receiverEmail)
                {
                    Subject = $"[GA Inventory Order Confirmation] {orderNo} | {subjectStatus}",
                    Body = MailText,
                    IsBodyHtml = true
                })
                {
                    message.CC.Add(ccEmail);
                    smtp.Send(message);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [HttpPost]
        public JsonResult UploadPriceXls()
        {
            try
            {
                string fileName;
                string filePath = "";
                HttpFileCollectionBase files = Request.Files;

                if (files.Count == 0)
                {
                    return Json(new { success = false, message = "Tidak ada file yang diterima oleh server." }, JsonRequestBehavior.AllowGet);
                }

                for (int i = 0; i < files.Count; i++)
                {
                    HttpPostedFileBase file = files[i];
                    fileName = Path.GetFileName(file.FileName);

                    string tempFolder = Server.MapPath("~/Files/HC/Inventory/Temp/");
                    if (!Directory.Exists(tempFolder))
                        Directory.CreateDirectory(tempFolder);

                    filePath = Path.Combine(tempFolder, fileName);
                    file.SaveAs(filePath);

                    string ext = Path.GetExtension(fileName).ToLower();
                    if (ext == ".xls")
                    {
                        // [Proses konversi NPOI .xls ke .xlsx tetap sama]
                        string xlsxPath = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(fileName) + ".xlsx");

                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            var hssfWorkbook = new NPOI.HSSF.UserModel.HSSFWorkbook(fileStream);
                            var xssfWorkbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
                            for (int s = 0; s < hssfWorkbook.NumberOfSheets; s++)
                            {
                                var sheet = hssfWorkbook.GetSheetAt(s);
                                var newSheet = xssfWorkbook.CreateSheet(sheet.SheetName);

                                for (int r = 0; r <= sheet.LastRowNum; r++)
                                {
                                    var row = sheet.GetRow(r);
                                    if (row == null) continue;
                                    var newRow = newSheet.CreateRow(r);

                                    for (int c = 0; c < row.LastCellNum; c++)
                                    {
                                        var cell = row.GetCell(c);
                                        if (cell == null) continue;
                                        var newCell = newRow.CreateCell(c);
                                        switch (cell.CellType)
                                        {
                                            case NPOI.SS.UserModel.CellType.Numeric:
                                                newCell.SetCellValue(cell.NumericCellValue);
                                                break;
                                            case NPOI.SS.UserModel.CellType.Boolean:
                                                newCell.SetCellValue(cell.BooleanCellValue);
                                                break;
                                            default:
                                                newCell.SetCellValue(cell.ToString());
                                                break;
                                        }
                                    }
                                }
                            }

                            using (var fsOut = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write))
                            {
                                xssfWorkbook.Write(fsOut);
                            }
                        }

                        System.IO.File.Delete(filePath);
                        filePath = xlsxPath;
                    }
                }

                int updatedCount = 0;
                int notFoundCount = 0;
                var debugList = new List<object>();

                using (var workBook = new ClosedXML.Excel.XLWorkbook(filePath))
                {
                    var sheet = workBook.Worksheet(1);
                    int startRow = 3;
                    int colKode = 1;  // Kolom A
                    int colHarga = 7; // Kolom G

                    int totalRows = sheet.LastRowUsed().RowNumber();
                    System.Diagnostics.Debug.WriteLine($"=== Mulai Membaca Excel: Total {totalRows} Baris ===");

                    for (int row = startRow; row <= totalRows; row++)
                    {
                        var cellKode = sheet.Cell(row, colKode);
                        var cellHarga = sheet.Cell(row, colHarga);

                        // 🔍 Mengambil data mentah (raw text) untuk debugging
                        string rawKode = cellKode.GetValue<string>()?.Trim() ?? "";
                        string rawHarga = cellHarga.GetValue<string>()?.Trim() ?? "";
                        string tipeDataHarga = cellHarga.DataType.ToString();

                        // Log langsung ke Output Window Visual Studio
                        System.Diagnostics.Debug.WriteLine($"[Baris {row}] Membaca -> Kode: '{rawKode}' | Harga Asli: '{rawHarga}' (Tipe: {tipeDataHarga})");

                        if (string.IsNullOrEmpty(rawKode))
                        {
                            System.Diagnostics.Debug.WriteLine($"[Baris {row}] Skip: Kolom Kode Kosong.");
                            continue;
                        }

                        decimal harga = 0;
                        bool isParsed = false;

                        if (cellHarga.DataType == ClosedXML.Excel.XLDataType.Number)
                        {
                            harga = cellHarga.GetValue<decimal>();
                            isParsed = true;
                        }
                        else
                        {
                            string cleanHarga = rawHarga.Replace("Rp", "")
                                                         .Replace(".", "")
                                                         .Replace(",", ".")
                                                         .Trim();

                            isParsed = decimal.TryParse(cleanHarga,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out harga);
                        }

                        if (!isParsed)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Baris {row}] ❌ Gagal Parsing Harga: '{rawHarga}'");
                            debugList.Add(new
                            {
                                Row = row,
                                KolomKode = rawKode,
                                HargaDiExcel = rawHarga,
                                TipeData = tipeDataHarga,
                                HargaHasilParses = "Gagal",
                                Status = "Wrong Price Format"
                            });
                            continue;
                        }

                        harga = Math.Round(harga, 0);
                        var item = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(x => x.Ref_Code == rawKode);

                        if (item != null)
                        {
                            item.Price = harga;
                            item.Last_Price_Update = DateTime.Now;
                            dbm.Entry(item).State = System.Data.Entity.EntityState.Modified;
                            updatedCount++;

                            debugList.Add(new
                            {
                                Row = row,
                                KolomKode = rawKode,
                                HargaDiExcel = rawHarga,
                                TipeData = tipeDataHarga,
                                HargaHasilParses = harga.ToString(),
                                Status = "Updated"
                            });
                        }
                        else
                        {
                            notFoundCount++;
                            debugList.Add(new
                            {
                                Row = row,
                                KolomKode = rawKode,
                                HargaDiExcel = rawHarga,
                                TipeData = tipeDataHarga,
                                HargaHasilParses = harga.ToString(),
                                Status = "Kode Tidak Terdaftar di DB"
                            });
                        }
                    }
                    dbm.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"=== Selesai Memproses Excel. Updated: {updatedCount}");
                }

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                return Json(new
                {
                    success = true,
                    message = $"Update selesai. {updatedCount} item diperbarui, {notFoundCount} tidak ditemukan.",
                    debugData = debugList.Take(50) 
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ CRITICAL ERROR: " + ex.Message);
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult UpdateItemPrice(string refCode, decimal price)
        {
            try
            {
                // 1️⃣ Validasi basic input
                if (string.IsNullOrWhiteSpace(refCode))
                    return Json(new { success = false, message = "Item code is required." }, JsonRequestBehavior.AllowGet);

                // 2️⃣ Normalisasi input biar gak gagal cuma karena spasi atau huruf kecil
                refCode = refCode.Trim().ToUpper();

                // 3️⃣ Cari item
                var item = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(x => x.Ref_Code.ToUpper() == refCode);
                if (item == null)
                    return Json(new { success = false, message = $"Item '{refCode}' not found in database." }, JsonRequestBehavior.AllowGet);

                // 4️⃣ Update harga
                item.Price = price;
                dbm.Entry(item).State = System.Data.Entity.EntityState.Modified;
                dbm.SaveChanges();

                // 5️⃣ Return result
                return Json(new
                {
                    success = true,
                    message = $"Price for '{refCode}' updated successfully to {price:N2}"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GenerateInvoiceNumber(string requestNo)
        {
            try
            {
                if (string.IsNullOrEmpty(requestNo))
                    return Json(new { success = false, message = "Request number is required." });

                // 1️⃣ Generate nomor baru
                string year = DateTime.Now.ToString("yy");
                int month = DateTime.Now.Month;
                string monthRoman = ToRoman(month);

                string prefix = $"INV{year}/KOPKAR/{monthRoman}/";

                var lastInvoice = dbm.HC_GA_Inventory_Requests
                    .Where(r => r.Invoice_Number.StartsWith(prefix))
                    .OrderByDescending(r => r.Invoice_Number)
                    .Select(r => r.Invoice_Number)
                    .FirstOrDefault();

                int nextIndex = 1;
                if (!string.IsNullOrEmpty(lastInvoice))
                {
                    var parts = lastInvoice.Split('/');
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int lastIndex))
                        nextIndex = lastIndex + 1;
                }

                string newInvoice = $"{prefix}{nextIndex:D3}";

                // 2️⃣ Update ke tabel request
                var request = dbm.HC_GA_Inventory_Requests.FirstOrDefault(r => r.Request_No == requestNo);
                if (request == null)
                    return Json(new { success = false, message = "Request not found." });

                request.Invoice_Number = newInvoice;
                request.Invoice_Date = DateTime.Now;
                request.Timestamps = DateTime.Now;

                dbm.SaveChanges();

                // 3️⃣ Return ke frontend
                return Json(new
                {
                    success = true,
                    invoiceNumber = newInvoice,
                    invoiceDate = request.Invoice_Date?.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        private string ToRoman(int number)
        {
            string[] roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
            return number >= 1 && number <= 12 ? roman[number - 1] : number.ToString();
        }

        [HttpGet]
        public ActionResult PreviewRequest(string requestNos)
        {
            // Cek apakah parameter kosong
            if (string.IsNullOrEmpty(requestNos))
                return Content("Request No not found");

            // --- 1. Ambil dan Pisahkan Banyak Request Header ---
            var requestList = requestNos.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(x => x.Trim())
                                        .ToList();

            var headers = dbm.HC_GA_Inventory_Requests
                .Where(x => requestList.Contains(x.Request_No))
                .ToList();

            if (!headers.Any())
                return Content("Requests not found");

            // --- 2. Ambil Cart dari SEMUA Request yang ditemukan ---
            var cartNos = headers
                .Where(h => !string.IsNullOrEmpty(h.Cart_No))
                .SelectMany(h => h.Cart_No.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(c => c.Trim())
                .Distinct()
                .ToList();

            // Gabungkan semua item dari cart tersebut
            var items = dbm.HC_GA_Inventory_Carts
                .Where(c => cartNos.Contains(c.Cart_No))
                .Select(c => new
                {
                    Section = c.Section_To_Name,
                    ItemName = c.Item_Name,
                    Qty = c.Qty_Acc ?? 0,
                    Price = c.Price ?? 0,
                    Budget = c.Budget_No == "UNB"
                        ? c.Budget_No
                        : c.Budget_No + " | " + c.Budget_Desc
                })
                .ToList()
                .GroupBy(x => x.Section)
                .ToList();

            var sectionname = items.FirstOrDefault()?.Key ?? "-";

            // ==========================================
            // FIX SUBJECT LOGIC HERE
            // ==========================================
            // Mengambil subjek dari header pertama sebagai representasi
            var firstHeader = headers.FirstOrDefault();
            string rawSubject = firstHeader?.Subject ?? "CATEGORY NOT FOUND";
            string cleanSubject = rawSubject;

            if (rawSubject.IndexOf("Stationary", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawSubject.IndexOf("Stationery", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cleanSubject = "Stationery";
            }
            else if (rawSubject.IndexOf("Refreshment", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cleanSubject = "Refreshment";
            }

            // --- 3. Generate Item Rows HTML ---
            string sectionBlocks = "";

            foreach (var sectionGroup in items)
            {
                decimal sectionTotal = 0;
                int no = 1;
                string itemRows = "";

                foreach (var item in sectionGroup)
                {
                    decimal total = item.Price * item.Qty;
                    sectionTotal += total;

                    itemRows += $@"
            <tr class='border-b text-xs'>
                <td class='p-2'>{no++}</td>
                <td class='p-2'>{item.ItemName}</td>
                <td class='p-2'>{item.Budget}</td>
                <td class='p-2 text-center'>{item.Qty}</td>
                <td class='p-2 text-right'>{item.Price:N0}</td>
                <td class='p-2 text-right'>{total:N0}</td>
            </tr>";
                }

                itemRows += $@"
        <tr class='bg-gray-100 font-semibold text-xs'>
            <td colspan='5' class='p-2 text-right'>Subtotal</td>
            <td class='p-2 text-right text-teal-700'>{sectionTotal:N0}</td>
        </tr>";

                sectionBlocks += $@"
        <div class='mb-6' style='page-break-inside: avoid;'>
            <h5 class='text-md font-bold text-gray-800 mb-2 flex items-center gap-2'>
                <i class='fas fa-shopping-cart text-teal-600'></i>
                {sectionGroup.Key}
            </h5>
            <div class='rounded-xl border border-gray-200 overflow-hidden'>
                <table class='w-full'>
                    <thead>
                        <tr class='bg-gradient-to-r from-teal-600 to-teal-700 text-white text-xs'>
                            <th class='p-2 text-left w-16'>No.</th>
                            <th class='p-2 text-left'>Item Description</th>
                            <th class='p-2 text-left'>Budget No</th>
                            <th class='p-2 text-center w-24'>Qty</th>
                            <th class='p-2 text-right w-24'>Price</th>
                            <th class='p-2 text-right w-28'>Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        {itemRows}
                    </tbody>
                </table>
            </div>
        </div>";
            }

            // --- 4. Hitung Summary Keseluruhan ---
            decimal subtotal = items.SelectMany(g => g).Sum(x => x.Price * x.Qty);
            int totalItems = items.SelectMany(g => g).Sum(x => x.Qty);
            string amountInWords = NumberToWordsHelper.NumberToWordsEN((long)subtotal) + " Rupiah";

            // --- 5. Load file Invoice.cshtml sebagai string ---
            string templatePath = Server.MapPath("~/Areas/HC/Views/Inventory/Partial/Invoice.cshtml");
            string html = System.IO.File.ReadAllText(templatePath);

            // --- 6. Map placeholder ---
            // Menggabungkan semua nomor request dan nomor penerimaan (jika ada)
            string joinedRequestNos = string.Join(", ", headers.Select(h => h.Request_No).Distinct());
            string joinedReceiveNos = string.Join(", ", headers.Where(h => !string.IsNullOrEmpty(h.Receive_Number)).Select(h => h.Receive_Number).Distinct());
            if (string.IsNullOrEmpty(joinedReceiveNos)) joinedReceiveNos = "-";

            html = html.Replace("##InvoiceNo##", joinedRequestNos);
            html = html.Replace("##ReceiveNumber##", joinedReceiveNos);
            html = html.Replace("##Subject##", cleanSubject);
            html = html.Replace("##Title##", "DETAIL");

            // Ambil tanggal dari request pertama
            string invoiceDateStr = firstHeader?.Invoice_Date?.ToString("dd MMM yyyy") ?? "-";
            html = html.Replace("##InvoiceDate##", invoiceDateStr);

            string dueDateStr = firstHeader?.Invoice_Date?.AddDays(7).ToString("dd MMM yyyy") ?? "-";
            html = html.Replace("##DueDate##", dueDateStr);

            html = html.Replace("##Subtotal##", subtotal.ToString("N0"));
            html = html.Replace("##TotalItems##", totalItems.ToString());
            html = html.Replace("##AmountInWords##", amountInWords);
            html = html.Replace("##SECTION_BLOCKS##", sectionBlocks);
            html = html.Replace("##CostName##", sectionname);

            // --- 7. Return html sebagai preview ---
            return Content(html, "text/html");
        }

        public static class NumberToWordsHelper
        {
            private static readonly string[] Units =
            {
        "Zero", "One", "Two", "Three", "Four", "Five", "Six",
        "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve",
        "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
        "Eighteen", "Nineteen"
    };

            private static readonly string[] Tens =
            {
        "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty",
        "Sixty", "Seventy", "Eighty", "Ninety"
    };

            public static string NumberToWordsEN(long number)
            {
                if (number == 0)
                    return "Zero";

                if (number < 0)
                    return "Minus " + NumberToWordsEN(Math.Abs(number));

                string words = "";

                if ((number / 1_000_000_000) > 0)
                {
                    words += NumberToWordsEN(number / 1_000_000_000) + " Billion ";
                    number %= 1_000_000_000;
                }

                if ((number / 1_000_000) > 0)
                {
                    words += NumberToWordsEN(number / 1_000_000) + " Million ";
                    number %= 1_000_000;
                }

                if ((number / 1_000) > 0)
                {
                    words += NumberToWordsEN(number / 1_000) + " Thousand ";
                    number %= 1_000;
                }

                if ((number / 100) > 0)
                {
                    words += NumberToWordsEN(number / 100) + " Hundred ";
                    number %= 100;
                }

                if (number > 0)
                {
                    if (number < 20)
                        words += Units[number];
                    else
                    {
                        words += Tens[number / 10];
                        if ((number % 10) > 0)
                            words += "-" + Units[number % 10];
                    }
                }

                return words.Trim();
            }
        }

        [HttpGet]
        public ActionResult PreviewInvoice(string invoiceNo)
        {
            if (string.IsNullOrEmpty(invoiceNo))
                return Content("Invoice No is required");

            invoiceNo = HttpUtility.UrlDecode(invoiceNo);

            string safeInvoiceNo = invoiceNo.Replace("/", "_");
            string folderPath = Server.MapPath("~/Files/HC/Inventory/Invoice/");

            if (Directory.Exists(folderPath))
            {
                // TAMBAHKAN SearchOption.AllDirectories di sini 👇
                var matchedFilePath = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f)
                        .ToLower()
                        .Contains(safeInvoiceNo.ToLower())
                    );

                if (matchedFilePath != null)
                {
                    string contentType = MimeMapping.GetMimeMapping(matchedFilePath);
                    return File(matchedFilePath, contentType);
                }
            }
            // ==========================================
            // 2. FALLBACK: GENERATE HTML PREVIEW
            // ==========================================
            var headers = dbm.HC_GA_Inventory_Requests
                .Where(x => x.Invoice_Number == invoiceNo)
                .ToList();

            if (headers == null || headers.Count == 0)
                return Content("Invoice not found");

            var mainHeader = headers.FirstOrDefault();

            var allCartNos = headers
                .SelectMany(h => (h.Cart_No ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(c => c.Trim())
                .Distinct()
                .ToList();

            var rawItems = dbm.HC_GA_Inventory_Carts
                .Where(c => allCartNos.Contains(c.Cart_No))
                .Select(c => new
                {
                    Section = c.Section_To_Name,
                    Qty = c.Qty_Acc ?? 0,
                    Price = c.Price ?? 0
                })
                .ToList();

            var sectionname = rawItems.Select(x => x.Section).FirstOrDefault() ?? "-";

            decimal subtotal = rawItems.Sum(x => x.Price * x.Qty);
            int totalItems = rawItems.Sum(x => x.Qty);

            string amountInWords = NumberToWordsHelper.NumberToWordsEN((long)subtotal) + " Rupiah";
            var reqList = headers
                .Select(h => h.Request_No)
                .Distinct()
                .ToList();

            string joinedReqs = string.Join(", ", reqList);

            // ==========================================
            // FIX SUBJECT LOGIC HERE
            // ==========================================
            string rawSubject = mainHeader.Subject ?? "CATEGORY NOT FOUND";
            string cleanSubject = rawSubject;

            // Option 1: If your data says "Purchase Stationary for FA" but you just want "Stationery"
            if (rawSubject.IndexOf("Stationary", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawSubject.IndexOf("Stationery", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cleanSubject = "Stationery";
            }
            else if (rawSubject.IndexOf("Refreshment", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cleanSubject = "Refreshment";
            }

            // B. Buat HTML-nya dengan styling besar untuk Subject
            string referToHtml = $@"
<div class='mb-6 mt-4 text-left pr-2'>
<p class='text-sm text-gray-600 italic'>
    Refer to Req: <span class='font-semibold text-gray-800'>{joinedReqs}</span>
</p>
</div>";

            string templatePath = Server.MapPath("~/Areas/HC/Views/Inventory/Partial/Invoice.cshtml");
            string html = System.IO.File.ReadAllText(templatePath);

            html = html.Replace("##Title##", "INVOICE");
            html = html.Replace("##InvoiceNo##", mainHeader.Invoice_Number ?? "-");
            html = html.Replace("##ReceiveNumber##", mainHeader.Receive_Number ?? "-");

            // We can still replace the raw subject if your template uses ##Subject## somewhere else
            html = html.Replace("##Subject##", cleanSubject);

            string invoiceDateStr = mainHeader.Invoice_Date?.ToString("dd MMM yyyy") ?? "-";
            html = html.Replace("##InvoiceDate##", invoiceDateStr);


            string dueDateStr = mainHeader.Invoice_Date?.AddDays(7).ToString("dd MMM yyyy") ?? "-";
            html = html.Replace("##DueDate##", dueDateStr);

            html = html.Replace("##Subtotal##", subtotal.ToString("N0"));
            html = html.Replace("##TotalItems##", totalItems.ToString());
            html = html.Replace("##AmountInWords##", amountInWords);
            html = html.Replace("##CostName##", sectionname);

            // Inject the new HTML block
            html = html.Replace("##SECTION_BLOCKS##", referToHtml);

            return Content(html, "text/html");
        }

        [HttpGet]
        public JsonResult GetInvoiceParams(string requestNo)
        {
            var req = dbm.HC_GA_Inventory_Requests
                         .FirstOrDefault(x => x.Request_No == requestNo);

            if (req == null)
                return Json(null, JsonRequestBehavior.AllowGet);

            // ===== Invoice Date & Due Date =====
            DateTime? invDate = req.Invoice_Date;
            string dueDate = invDate?.AddDays(7).ToString("MM/dd/yyyy");

            // ===== Third Party (temporary) =====
            string thirdParty = "D0136";

            // ===== Description =====
            string yy = DateTime.Now.ToString("yy");
            string mm = DateTime.Now.ToString("MM");
            string subject = req.Subject ?? "";
            string description = $"{yy}{mm}_{subject}";

            // ===== Prepare calculation vars =====
            decimal totalAmount = 0;
            var detailsList = new List<string>();

            // ===== Parse Cart_No string =====
            var cartNos = (req.Cart_No ?? "")
                          .Split(',')
                          .Select(x => x.Trim())
                          .Where(x => x != "")
                          .ToList();

            foreach (var cartNo in cartNos)
            {
                var cartRows = dbm.HC_GA_Inventory_Carts
                                  .Where(c => c.Cart_No == cartNo)
                                  .ToList();

                foreach (var ci in cartRows)
                {
                    int qty = ci.Qty_Acc ?? ci.Qty_Request;

                    var item = dbm.HC_GA_Inventory_Item_List
                                   .FirstOrDefault(i => i.ID == ci.Item_Id);

                    if (item == null) continue;

                    decimal lineAmount = qty * item.Price;
                    totalAmount += lineAmount;

                    // COA
                    string coa = ci.Budget_No == "UNB"
                        ? "UNB|UNB"
                        : db.V_FA_BudgetSystem_BEX_BEL
                            .Where(b => b.Budget_No == ci.Budget_No && b.Latest == 1)
                            .Select(b => b.COA_Code + "|" + b.COA_Name)
                            .FirstOrDefault() ?? "";
                    string bgtDesc = string.IsNullOrEmpty(ci.Budget_Desc) ? "UNB" : ci.Budget_Desc;
                    string budgetFull = $"{ci.Budget_No}|{bgtDesc}";

                    string coaFull = string.IsNullOrEmpty(coa) ? "UNB|Unbudgeted" : coa;
                    string sectionToName = ci.Section_To_Name?.Trim();

                    string axCostId = db.V_Users_Active
                        .Where(w => w.AXCostName == sectionToName)
                        .Select(w => w.AXCostID)
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(axCostId))
                    {
                        axCostId = ci.Section_To_Code;
                    }

                    string sectionTo = axCostId + "|" + sectionToName;

                    string detailStr = $"{lineAmount}||{budgetFull}||{description}||{coa}||{sectionTo}";

                    detailsList.Add(detailStr);
                }
            }

            string detailsFormatted = string.Join("#", detailsList);



            return Json(new
            {
                invoiceNumber = req.Invoice_Number,
                invoiceDate = invDate?.ToString("MM/dd/yyyy"),
                dueDate,
                thirdParty,
                description,
                amount = totalAmount,
                paymentType = "DirectPayment",
                details = detailsFormatted
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InventoryAdjustment()
        {
            var _currUser = ((ClaimsIdentity)User.Identity);
            string userNik = _currUser.GetUserId();
            string deptName = _currUser.FindFirstValue("deptName");
            string username = _currUser.FindFirstValue("fullName");

            var allowedDept = new List<string> { "HRD & GA", "KOPERASI" };

            if (!allowedDept.Contains(deptName))
            {
                ViewBag.DeptName = deptName;
                ViewBag.NavHide = false;
                return PartialView("~/Areas/HC/Views/Inventory/Partial/_NoAccess.cshtml");
            }

            ViewBag.UserName = username;
            ViewBag.UserNik = userNik;

            return View();
        }

        public ActionResult CreateInventoryAdjustment()
        {
            var _currUser = ((ClaimsIdentity)User.Identity);
            string userNik = _currUser.GetUserId();
            string deptName = _currUser.FindFirstValue("deptName");
            string username = _currUser.FindFirstValue("fullName");

            var allowedDept = new List<string> { "HRD & GA", "KOPERASI" };

            if (!allowedDept.Contains(deptName))
            {
                ViewBag.DeptName = deptName;
                ViewBag.NavHide = false;
                return PartialView("~/Areas/HC/Views/Inventory/Partial/_NoAccess.cshtml");
            }

            ViewBag.UserName = username;
            ViewBag.UserNik = userNik;

            var requestList = dbm.HC_GA_Inventory_Requests
    .Where(w =>
        string.IsNullOrEmpty(w.Invoice_Number) &&
        string.IsNullOrEmpty(w.Receive_Number)
    )
    .Select(x => x.Request_No)
    .Distinct()
    .ToList();

            ViewBag.RequestList = requestList;


            return View();
        }

        [HttpGet]
        public ActionResult GetRequestSubject(string requestNo)
        {
            if (string.IsNullOrEmpty(requestNo))
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var subject = dbm.HC_GA_Inventory_Requests
                .Where(x => x.Request_No == requestNo)
                .Select(x => x.Subject)
                .FirstOrDefault();

            return Json(new { success = true, subject = subject }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetItemsByRequestNo(string requestNo)
        {
            if (string.IsNullOrEmpty(requestNo))
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var items = dbm.HC_GA_Inventory_Carts
            .Where(c => c.Request_No == requestNo)
            .Select(c => new
            {
                ItemId = c.Item_Id,
                ItemName = c.Item_Name
            })
            .Distinct()
            .ToList();

            return Json(new { success = true, data = items }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetItemQuantity(string requestNo, int itemId)
        {
            try
            {
                if (string.IsNullOrEmpty(requestNo) || itemId <= 0)
                    return Json(new { success = false, message = "Invalid request" }, JsonRequestBehavior.AllowGet);

                var cartItem = dbm.HC_GA_Inventory_Carts
                    .Where(c => c.Request_No == requestNo && c.Item_Id == itemId)
                    .Select(c => new { c.Qty_Request })
                    .FirstOrDefault();

                if (cartItem == null)
                    return Json(new { success = false, message = "Item not found" }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, qty = cartItem.Qty_Request }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log error
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveAdjustment()
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                var currUser = _currUser.GetUserId();
                var datetime = DateTime.Now;

                int ItemId = Convert.ToInt32(Request["ItemID"]);
                string RequestNo = Request["Request_No"];
                int qtyAfter = Convert.ToInt32(Request["Qty_After"]);
                int qtyBefore = Convert.ToInt32(Request["Qty_Before"]);

                // Buat adjustment baru
                var item = new HC_GA_Inventory_Adjustment_Detail
                {
                    Creator_NIK = currUser,
                    Created_Date = datetime,
                    Request_No = RequestNo,
                    Item_ID = ItemId,
                    Qty_After = qtyAfter,
                    Qty_Before = qtyBefore,
                };

                dbm.HC_GA_Inventory_Adjustment_Detail.Add(item);

                // Update Qty_Request di cart
                var cart = dbm.HC_GA_Inventory_Carts
                    .FirstOrDefault(c => c.Request_No == RequestNo && c.Item_Id == ItemId);

                if (cart != null)
                {
                    cart.Qty_Request = qtyAfter;
                }

                dbm.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Item added successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message
                });
            }
        }

        [HttpGet]
        public JsonResult GetRecentAdjustments()
        {
            try
            {
                var _currUser = (ClaimsIdentity)User.Identity;
                var currUser = _currUser.GetUserId();

                var rawData = (from adj in dbm.HC_GA_Inventory_Adjustment_Detail
                               join item in dbm.HC_GA_Inventory_Item_List
                                   on adj.Item_ID equals item.ID

                               join req in dbm.HC_GA_Inventory_Requests
                                   on adj.Request_No equals req.Request_No

                               where adj.Creator_NIK == currUser
                               orderby adj.Created_Date descending
                               select new
                               {
                                   adj.ID,
                                   adj.Request_No,
                                   ItemName = item.Item_Name,
                                   adj.Item_ID,
                                   Subject = req.Subject,
                                   adj.Qty_Before,
                                   adj.Qty_After,
                                   adj.Created_Date,
                                   UserName = req.User_Name,
                               })
                               .Take(3)
                               .ToList();

                var formattedData = rawData.Select(x => new
                {
                    x.ID,
                    x.Request_No,
                    x.ItemName,
                    x.Item_ID,

                    Subject = !string.IsNullOrEmpty(x.Subject) ? x.Subject : "-",
                    x.UserName,
                    x.Qty_Before,
                    x.Qty_After,

                    DateStr = x.Created_Date.HasValue
                              ? x.Created_Date.Value.ToString("dd MMM, HH:mm")
                              : "-"
                });

                return Json(new { success = true, data = formattedData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult DownloadAdjustmentTemplate()
        {
            string filePath = Path.Combine(
                Server.MapPath("~/Files/HC/Inventory/"),
                "Inventory Adjustment.xlsx"
            );
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("File not found");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            string fileName = "Inventory Adjustment.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        [HttpPost]
        public JsonResult UploadItemXls()
        {
            try
            {
                if (Request.Files.Count == 0)
                    return Json(new { success = false, message = "No file uploaded" });

                var file = Request.Files[0];

                // RequestNo -> Subject
                var requestMap = dbm.HC_GA_Inventory_Requests
                    .ToDictionary(
                        x => x.Request_No.Trim(),
                        x => x.Subject
                    );

                // ItemName -> ItemId
                var itemMap = dbm.HC_GA_Inventory_Item_List
                    .ToDictionary(
                        x => x.Item_Name.Trim(),
                        x => x.ID
                    );

                string tempFolder = Server.MapPath("~/Files/HC/Inventory/Temp/");
                Directory.CreateDirectory(tempFolder);

                string filePath = Path.Combine(
                    tempFolder,
                    Guid.NewGuid() + Path.GetExtension(file.FileName)
                );

                file.SaveAs(filePath);

                var validItems = new List<object>();
                var invalidItems = new List<object>();

                using (var wb = new ClosedXML.Excel.XLWorkbook(filePath))
                {
                    var ws = wb.Worksheet(1);
                    var lastRow = ws.LastRowUsed().RowNumber();

                    for (int row = 2; row <= lastRow; row++)
                    {
                        string requestNo = ws.Cell(row, 2).GetString().Trim();
                        string itemName = ws.Cell(row, 3).GetString().Trim();
                        string qtyText = ws.Cell(row, 4).GetString().Trim();

                        if (string.IsNullOrEmpty(requestNo) || string.IsNullOrEmpty(itemName))
                            continue;

                        if (!int.TryParse(qtyText, out int qtyAfter) || qtyAfter <= 0)
                        {
                            invalidItems.Add(new
                            {
                                Row = row,
                                RequestNo = requestNo,
                                ItemName = itemName,
                                Reason = "Invalid quantity"
                            });
                            continue;
                        }

                        if (!requestMap.ContainsKey(requestNo))
                        {
                            invalidItems.Add(new
                            {
                                Row = row,
                                RequestNo = requestNo,
                                ItemName = itemName,
                                Reason = "Request No not found"
                            });
                            continue;
                        }

                        if (!itemMap.ContainsKey(itemName))
                        {
                            invalidItems.Add(new
                            {
                                Row = row,
                                RequestNo = requestNo,
                                ItemName = itemName,
                                Reason = "Item not found"
                            });
                            continue;
                        }

                        int itemId = itemMap[itemName];

                        int qtyBefore = dbm.HC_GA_Inventory_Carts
                            .Where(x =>
                                x.Request_No == requestNo &&
                                x.Item_Id == itemId
                            )
                            .Select(x => (int?)x.Qty_Request)
                            .FirstOrDefault() ?? 0;

                        validItems.Add(new
                        {
                            RequestNo = requestNo,
                            RequestSubject = requestMap[requestNo],
                            ItemId = itemId,
                            ItemName = itemName,
                            Qty_Before = qtyBefore,
                            Qty_After = qtyAfter
                        });
                    }
                }

                System.IO.File.Delete(filePath);

                return Json(new
                {
                    success = true,
                    data = validItems,
                    invalidItems
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult SaveAdjustmentItemExcel(List<HC_GA_Inventory_Adjustment_Detail> items)
        {
            try
            {
                if (items == null || !items.Any())
                    return Json(new { success = false, message = "No data received" });

                var identity = (ClaimsIdentity)User.Identity;
                string currUser = identity.GetUserId();

                var validItemIds = dbm.HC_GA_Inventory_Item_List
                    .Select(x => x.ID)
                    .ToHashSet();

                var validRequestNos = dbm.HC_GA_Inventory_Requests
                    .Select(x => x.Request_No)
                    .ToHashSet();

                var invalidItems = new List<object>();

                foreach (var dto in items)
                {
                    if (!validRequestNos.Contains(dto.Request_No))
                    {
                        invalidItems.Add(new
                        {
                            dto.Request_No,
                            dto.Item_ID,
                            Reason = "Invalid Request No"
                        });
                        continue;
                    }

                    if (!validItemIds.Contains(dto.Item_ID))
                    {
                        invalidItems.Add(new
                        {
                            dto.Request_No,
                            dto.Item_ID,
                            Reason = "Invalid Item"
                        });
                        continue;
                    }

                    var existingItem = dbm.HC_GA_Inventory_Adjustment_Detail
                        .FirstOrDefault(x =>
                            x.Creator_NIK == currUser &&
                            x.Request_No == dto.Request_No &&
                            x.Item_ID == dto.Item_ID
                        );

                    if (existingItem != null)
                    {
                        existingItem.Qty_After += dto.Qty_After;
                    }
                    else
                    {
                        dbm.HC_GA_Inventory_Adjustment_Detail.Add(
                            new HC_GA_Inventory_Adjustment_Detail
                            {
                                Creator_NIK = currUser,
                                Request_No = dto.Request_No,
                                Item_ID = dto.Item_ID,
                                Qty_Before = dto.Qty_Before,
                                Qty_After = dto.Qty_After,
                                Created_Date = DateTime.Now
                            });
                    }
                }

                dbm.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Excel items saved successfully",
                    invalidItems
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message
                });
            }
        }

        [HttpGet]
        public JsonResult GetAdjustment()
        {
            try
            {
                var data = (
                    from d in dbm.HC_GA_Inventory_Adjustment_Detail
                    join r in dbm.HC_GA_Inventory_Requests
                        on d.Request_No equals r.Request_No
                    join i in dbm.HC_GA_Inventory_Item_List
                        on d.Item_ID equals i.ID
                    select new
                    {
                        d.Created_Date,
                        d.Request_No,
                        r.Subject,
                        i.Item_Name,
                        d.Qty_Before,
                        d.Qty_After,
                        d.Creator_NIK
                    }
                ).ToList();

                var users = db.V_Users_Active
                .Select(x => new { x.NIK, x.Name })
                .ToList();

                var result = data
                .Select(d => new
                {
                    Date_Issued = d.Created_Date,
                    Request_No = d.Request_No,
                    Subject = d.Subject,
                    Item_Name = d.Item_Name,
                    Qty_Before = d.Qty_Before,
                    Qty_After = d.Qty_After,
                    Creator = users
                        .FirstOrDefault(u => u.NIK == d.Creator_NIK)?.Name
                        ?? d.Creator_NIK
                })
                .OrderByDescending(x => x.Date_Issued)
                .ToList();

                return Json(new
                {
                    success = true,
                    data = result
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GetInvoiceParamsBulk(List<string> requestNos)
        {
            try
            {
                if (requestNos == null || !requestNos.Any())
                {
                    return Json(new { success = false, message = "No request numbers provided" });
                }

                // 1. Ambil Semua Items (Carts) berdasarkan List Request No
                var allItems = dbm.HC_GA_Inventory_Carts
                                 .Where(x => requestNos.Contains(x.Request_No))
                                 .ToList();

                // 2. Ambil Semua Header berdasarkan List Request No
                var allHeaders = dbm.HC_GA_Inventory_Requests
                                   .Where(x => requestNos.Contains(x.Request_No))
                                   .ToList();

                // --- SECTION INVOICE NUMBER GENERATION (TETAP SAMA) ---
                string yearInv = DateTime.Now.ToString("yy");
                int monthInv = DateTime.Now.Month;
                string monthRoman = ToRoman(monthInv);
                string prefix = $"INV{yearInv}/KOPKAR/{monthRoman}/";

                var lastInvoice = dbm.HC_GA_Inventory_Requests
                    .Where(r => r.Invoice_Number.StartsWith(prefix))
                    .OrderByDescending(r => r.Invoice_Number)
                    .Select(r => r.Invoice_Number)
                    .FirstOrDefault();

                int nextIndex = 1;

                if (!string.IsNullOrEmpty(lastInvoice))
                {
                    var parts = lastInvoice.Split('/');
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int lastIndex))
                    {
                        nextIndex = lastIndex + 1;
                    }
                }

                string singleInvoiceNumber = $"{prefix}{nextIndex:D3}";
                DateTime singleInvoiceDate = DateTime.Now;

                // TERAPKAN NOMOR YANG SAMA KE SEMUA HEADER REQUEST
                foreach (var header in allHeaders)
                {
                    if (header.Invoice_Number != singleInvoiceNumber)
                    {
                        header.Invoice_Number = singleInvoiceNumber;
                        header.Invoice_Date = singleInvoiceDate;
                    }
                }
                // --------------------------------------------------------

                // 3. Persiapkan Data Budget & COA (Mapping)
                var budgetNosList = allItems
                    .Select(x => x.Budget_No)
                    .Where(x => !string.IsNullOrEmpty(x) && x != "UNB")
                    .Distinct()
                    .ToList();

                var budgetMap = db.V_FA_BudgetSystem_BEX_BEL
                    .Where(b => budgetNosList.Contains(b.Budget_No) && b.Latest == 1)
                    .Select(b => new { b.Budget_No, COA = b.COA_Code + "|" + b.COA_Name })
                    .ToDictionary(k => k.Budget_No, v => v.COA);

                // --- BARU: LOOKUP SECTION CODE UNTUK YANG "UNB" ---
                // Ambil daftar Section_To_Name dari item yang kodenya "UNB" atau kosong
                var unbSectionNames = allItems
                    .Where(x => string.IsNullOrWhiteSpace(x.Section_To_Code) || x.Section_To_Code.Trim().ToUpper() == "UNB")
                    .Select(x => x.Section_To_Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                // Tarik data dari V.Users.Active (Sesuaikan nama Context/View dengan sistemmu)
                // Kita simpan ke Dictionary: Key = Nama Section, Value = Cost ID
                var sectionLookup = new Dictionary<string, string>();
                if (unbSectionNames.Any())
                {
                    // Asumsi: View ada di context 'db' (atau ganti dengan context yang sesuai)
                    sectionLookup = db.V_Users_Active // <-- SESUAIKAN NAMA VIEW/TABLE-NYA
                        .Where(u => unbSectionNames.Contains(u.AXCostName) && !string.IsNullOrEmpty(u.AXCostID))
                        .Select(u => new { u.AXCostName, u.AXCostID })
                        .Distinct() // Antisipasi data kembar
                        .ToDictionary(k => k.AXCostName, v => v.AXCostID);
                }
                // ----------------------------------------------------

                // 4. Inisialisasi Dictionary Hasil
                var resultDictionary = new Dictionary<string, object>();

                foreach (var reqNo in requestNos)
                {
                    var header = allHeaders.FirstOrDefault(x => x.Request_No == reqNo);
                    var items = allItems.Where(x => x.Request_No == reqNo).ToList();

                    if (header == null) continue;

                    DateTime refDate = header.Invoice_Date.HasValue ? header.Invoice_Date.Value : header.Due_Date;
                    string yy = refDate.ToString("yy");
                    string mm = refDate.ToString("MM");
                    string monthName = refDate.ToString("MMMM");
                    string yyyy = refDate.ToString("yyyy");

                    string rawSubject = header.Subject ?? "Stationery";
                    string autoDescription = $"{yy}{mm}_Purchase of {rawSubject} period {monthName} {yyyy}_Koperasi";

                    var detailsMapped = items.Select(item =>
                    {
                        string bgtNo = string.IsNullOrEmpty(item.Budget_No) ? "UNB" : item.Budget_No;

                        string coaValue;
                        if (budgetMap.ContainsKey(bgtNo))
                        {
                            coaValue = budgetMap[bgtNo];
                        }
                        else
                        {
                            coaValue = "7691000|Office supply & Stationary Expense";
                        }

                        decimal safeQty = item.Qty_Acc.HasValue ? (decimal)item.Qty_Acc.Value : (decimal)(item.Qty_Request);
                        decimal safePrice = item.Price ?? 0;

                        // --- LOGIC SECTION MAPPING ---
                        string secCode = item.Section_To_Code;
                        string secName = item.Section_To_Name;

                        // Jika kodenya kosong atau "UNB"
                        if (string.IsNullOrWhiteSpace(secCode) || secCode.Trim().ToUpper() == "UNB")
                        {
                            // Coba cari Cost ID berdasarkan namanya
                            if (!string.IsNullOrWhiteSpace(secName) && sectionLookup.ContainsKey(secName))
                            {
                                secCode = sectionLookup[secName]; // Ambil dari V.Users.Active
                            }
                            else
                            {
                                // Fallback terakhir jika nama tidak ditemukan di V.Users.Active
                                secCode = "B2300";
                            }
                        }

                        // Fallback untuk nama
                        if (string.IsNullOrWhiteSpace(secName))
                        {
                            secName = "Accounting & Finance";
                        }

                        string finalSection = secCode + "|" + secName;
                        // -----------------------------

                        return new
                        {
                            invoiceNumber = header.Invoice_Number,
                            ItemName = item.Item_Name,
                            Qty = safeQty,
                            Price = safePrice,
                            LineTotal = safeQty * safePrice,
                            Budget_No = bgtNo + "|" + (string.IsNullOrWhiteSpace(item.Budget_Desc) ? "Unbudgeted" : item.Budget_Desc),
                            COA = coaValue,
                            SectionTo = finalSection // Pakai hasil mapping baru
                        };
                    }).ToList();

                    string fmtInvoiceDate = header.Invoice_Date.HasValue
                        ? header.Invoice_Date.Value.ToString("MM/dd/yyyy")
                        : "";
                    string fmtDueDate = header.Due_Date.ToString("MM/dd/yyyy");

                    resultDictionary.Add(reqNo, new
                    {
                        invoiceNumber = header.Invoice_Number ?? "",
                        invoiceDate = fmtInvoiceDate,
                        dueDate = fmtDueDate,
                        description = autoDescription,
                        thirdParty = "D0136",
                        amount = detailsMapped.Sum(x => x.LineTotal),
                        details = detailsMapped
                    });
                }

                return Json(resultDictionary);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateItemDetail(int id)
        {
            try
            {
                var cart = dbm.HC_GA_Inventory_Carts.FirstOrDefault(c => c.ID == id);
                if (cart == null)
                {
                    return Json(new { success = false, message = "Cart data not found." });
                }

                int newItemId;
                int.TryParse(Request["Item_Id"], out newItemId);

                int newQty;
                int.TryParse(Request["Qty_Acc"], out newQty);

                var masterItem = dbm.HC_GA_Inventory_Item_List.FirstOrDefault(i => i.ID == newItemId);

                if (masterItem == null)
                {
                    return Json(new { success = false, message = "Master item not found." });
                }

                cart.Item_Id = newItemId;
                cart.Item_Name = masterItem.Item_Name;
                cart.Unit_Request = masterItem.Unit;

                if (newQty > 0)
                {
                    cart.Qty_Acc = newQty;
                }

                string budgetNo = Request["Budget_No"];
                if (!string.IsNullOrEmpty(budgetNo))
                {
                    cart.Budget_No = budgetNo;
                    string budgetDesc = Request["Budget_Desc"];
                    if (!string.IsNullOrEmpty(budgetDesc))
                        cart.Budget_Desc = budgetDesc;

                    var budgetData = db.V_FA_BudgetSystem_BEX_BEL
                        .FirstOrDefault(b => b.Budget_No == budgetNo);
                    if (budgetData != null)
                    {
                        cart.Section_To_Name = budgetData.Section_To_Name ?? "";
                    }
                }

                cart.Timestamps = DateTime.Now;

                dbm.SaveChanges();

                return Json(new { success = true, message = "Data Updated!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetPriceLogs(string requestNo)
        {
            var dbLogs = (from log in dbm.HC_GA_Inventory_Price_Logs
                          join cart in dbm.HC_GA_Inventory_Carts on log.Cart_ID equals cart.ID
                          where log.Request_No == requestNo
                          orderby log.Update_Date descending
                          select new
                          {
                              log.ID,
                              log.Cart_ID,
                              cart.Item_Id, 
                              cart.Item_Name,
                              log.Old_Price,
                              log.New_Price,
                              log.Update_Date,
                              log.Updated_By
                          })
                          .AsEnumerable()
                          .ToList();

            var logs = dbLogs.Select(x => {
                decimal targetRevertPrice = x.Old_Price;

                if (targetRevertPrice == 0)
                {
                    var relatedLogWithPrice = dbLogs
                        .Where(l => l.Item_Id == x.Item_Id && (l.Old_Price > 0 || l.New_Price > 0))
                        .OrderByDescending(l => l.Update_Date)
                        .FirstOrDefault();

                    if (relatedLogWithPrice != null)
                    {
                        targetRevertPrice = relatedLogWithPrice.Old_Price > 0
                            ? relatedLogWithPrice.Old_Price
                            : relatedLogWithPrice.New_Price;
                    }
                    else
                    {
                        targetRevertPrice = x.New_Price;
                    }
                }

                return new
                {
                    x.ID,
                    x.Cart_ID,
                    x.Item_Name,
                    x.Old_Price,
                    x.New_Price,
                    Target_Revert_Price = targetRevertPrice,
                    Update_Date = x.Update_Date.ToString("dd MMM yyyy HH:mm:ss"),
                    x.Updated_By
                };
            }).ToList();

            return Json(logs, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult RollbackPrice(int cartId, decimal targetPrice)
        {
            try
            {
                var cart = dbm.HC_GA_Inventory_Carts.FirstOrDefault(x => x.ID == cartId);
                if (cart == null)
                    return Json(new { success = false, message = "Cart item tidak ditemukan." });

                decimal currentPrice = cart.Price ?? 0;
                if (currentPrice == targetPrice)
                    return Json(new { success = false, message = "Harga saat ini sudah sesuai dengan harga tujuan rollback." });

                cart.Price = targetPrice;

                var request = dbm.HC_GA_Inventory_Requests.ToList()
                    .FirstOrDefault(r => r.Cart_No != null && r.Cart_No.Split(',').Select(c => c.Trim()).Contains(cart.Cart_No));
                string reqNo = request?.Request_No ?? "-";

                var rollbackLog = new HC_GA_Inventory_Price_Logs 
                {
                    Request_No = reqNo,
                    Cart_ID = cartId,
                    Item_Id = cart.Item_Id,
                    Old_Price = currentPrice,
                    New_Price = targetPrice,
                    Update_Date = DateTime.Now,
                    Updated_By = "SYSTEM_ROLLBACK"
                };

                dbm.HC_GA_Inventory_Price_Logs.Add(rollbackLog);
                dbm.SaveChanges();

                return Json(new { success = true, message = "Harga berhasil dikembalikan!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RefreshRequestPrices(string requestNo)
        {
            var _currUser = (ClaimsIdentity)User.Identity;
            string userNik = _currUser.GetUserId();

            if (string.IsNullOrEmpty(requestNo))
            {
                return Json(new { success = false, message = "Nomor Request tidak valid." });
            }

            try
            {
                var cartsToUpdate = dbm.HC_GA_Inventory_Carts
                    .Where(c => c.Request_No == requestNo)
                    .ToList();

                if (!cartsToUpdate.Any())
                {
                    return Json(new { success = false, message = "Data cart tidak ditemukan untuk Request tersebut." });
                }

                int updatedCount = 0;

                using (var transaction = dbm.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var cart in cartsToUpdate)
                        {
                            var masterItem = dbm.HC_GA_Inventory_Item_List
                                .FirstOrDefault(i => i.ID == cart.Item_Id);

                            decimal currentCartPrice = cart.Price ?? 0m;
                            decimal masterItemPrice = masterItem != null ? masterItem.Price : 0m;

                            if (masterItem != null && masterItemPrice != currentCartPrice)
                            {
                                var log = new HC_GA_Inventory_Price_Logs
                                {
                                    Cart_ID = cart.ID,
                                    Request_No = requestNo,
                                    Item_Id = cart.Item_Id,
                                    Old_Price = currentCartPrice,
                                    New_Price = masterItemPrice,  
                                    Update_Date = DateTime.Now,
                                    Updated_By = userNik
                                };
                                dbm.HC_GA_Inventory_Price_Logs.Add(log);

                                cart.Price = masterItemPrice;

                                updatedCount++;
                            }
                        }

                        if (updatedCount > 0)
                        {
                            dbm.SaveChanges();
                            transaction.Commit();
                            return Json(new { success = true, message = $"{updatedCount} item(s) berhasil diperbarui ke harga terbaru." });
                        }
                        else
                        {
                            transaction.Rollback();
                            return Json(new { success = true, message = "Semua item sudah menggunakan harga terbaru." });
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Terjadi kesalahan: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetPriceByItemId(int itemId)
        {
            // Ambil harga dari Master Item List berdasarkan ID
            decimal price = dbm.HC_GA_Inventory_Item_List
                .Where(x => x.ID == itemId)
                .Select(x => x.Price)
                .FirstOrDefault(); // Default 0 jika null

            return Json(new { price = price }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ReUploadFile(string invoice_no, HttpPostedFileBase file)
        {
            try
            {
                // 1. The 'invoice_no' parameter actually holds the 'Receive_Number' sent from the frontend.
                // We look up the original request based on this Receive_Number.
                var requestData = dbm.HC_GA_Inventory_Requests
                                     .FirstOrDefault(r => r.Invoice_Number == invoice_no);

                if (requestData == null)
                {
                    return Json(new { success = false, message = "Data request tidak ditemukan berdasarkan Receive Number tersebut." });
                }

                // 2. Extract the actual Invoice Number from the found record.
                string actualInvoiceNumber = requestData.Invoice_Number;

                // Ensure the invoice number exists before trying to manipulate it
                if (string.IsNullOrEmpty(actualInvoiceNumber))
                {
                    return Json(new { success = false, message = "Invoice Number belum digenerate untuk request ini." });
                }

                if (file != null && file.ContentLength > 0)
                {
                    // 3. Clean the actual invoice number to use as a valid folder/file name
                    string safeInvoiceNo = actualInvoiceNumber.Replace("/", "_").Replace("\\", "_");

                    var uploadDir = Server.MapPath($"~/Files/HC/Inventory/Invoice/{safeInvoiceNo}");

                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    string fileExtension = Path.GetExtension(file.FileName);
                    string newFileName = $"{safeInvoiceNo}{fileExtension}";

                    var filePath = Path.Combine(uploadDir, newFileName);
                    file.SaveAs(filePath);

                    string relativeUrl = $"/Files/HC/Inventory/Invoice/{safeInvoiceNo}/{newFileName}";

                    return Json(new
                    {
                        success = true,
                        message = "File uploaded successfully.",
                        fileName = newFileName,
                        filePath = relativeUrl
                    });
                }

                return Json(new { success = false, message = "File is empty or not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
