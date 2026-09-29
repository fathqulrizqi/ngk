using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using NGKBusi.Areas.IT.Models;
using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;

namespace NGKBusi.Areas.IT.Controllers
{
    public class MasterMenusController : Controller
    {
        DefaultConnection db = new DefaultConnection();
        ITConnection dbm = new ITConnection();

        // GET: IT/MasterMenus
        public ActionResult Index()
        {
            var _currUser = (ClaimsIdentity)User.Identity;

            string userNik = _currUser.GetUserId();
            string deptName = _currUser.FindFirstValue("deptName");
            string allowedDept = "INFORMATION TECHNOLOGY";
            if (deptName != allowedDept)
            {
                ViewBag.DeptName = deptName;
                ViewBag.NavHide = false;
                return PartialView("~/Areas/IT/Views/MasterMenus/_NoAccess.cshtml");
            }

            ViewBag.UserNik = userNik;

            ViewBag.UserList = db.V_Users_Active.OrderBy(w => w.NIK).ToList();
            ViewBag.UserRoleList = db.Roles.OrderBy(w => w.id).ToList();


            return View();
        }

        public ActionResult _NoAccess()
        {

            ViewBag.NavHide = false;

            return View();
        }

        public ActionResult _Modal()
        {
            var _currUser = (ClaimsIdentity)User.Identity;

            string userNik = _currUser.GetUserId();
            ViewBag.UserNik = userNik;

            return View();
        }

        public ActionResult _ModalUser(int id)
        {
            var _currUser = (ClaimsIdentity)User.Identity;

            string userNik = _currUser.GetUserId();
            ViewBag.UserNik = userNik;

            return View();
        }

        [HttpGet]
        public JsonResult GetUserPermissions(int id)
        {
            var userPermissions = db.Users_Menus_Roles
                                    .Where(umr => umr.menuID == id)
                                    .Include(umr => umr.Users) 
                                    .ToList();

            // Asumsikan model Users memiliki properti Name
            var result = userPermissions.Select(umr => new {
                userNIK = umr.userNIK,
                Users = new { name = umr.Users.Name },
                menuID = umr.menuID,
                allowInsert = umr.allowInsert,
                allowUpdate = umr.allowUpdate,
                allowDelete = umr.allowDelete
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMenus()
        {
            var menus = db.Menus.ToList();

            var tree = menus
                .Where(m => m.parentID == null)
                .OrderBy(m => m.sequence)
                .Select(m => MapMenu(m, menus))
                .ToList();

            return Json(tree, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMenuById(int id)
        {
            try
            {
                var menu = db.Menus.FirstOrDefault(m => m.id == id);

                if (menu == null)
                {
                    return Json(new { success = false, message = "Menu not found" }, JsonRequestBehavior.AllowGet);
                }

                // Map menu ke object yang akan dikirim ke client
                var menuData = new
                {
                    Id = menu.id,
                    Title = menu.name,
                    Icon = menu.icon,
                    Controller = menu.controller,
                    Action = menu.action,
                    Area = menu.area,
                    Redirect = menu.redirect,
                    ParentId = menu.parentID,
                    Sequence = menu.sequence,
                    IsActive = menu.isActive,
                    IsGlobal = menu.isGlobal,
                    IsShow = menu.isShow,
                    CreatedBy = menu.CreatedBy,
                    CreatedDate = menu.CreatedDate,
                    UpdatedBy = menu.UpdatedBy,
                    UpdatedDate = menu.UpdatedDate
                };

                return Json(menuData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log error jika diperlukan
                return Json(new { success = false, message = "Error retrieving menu: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        //[HttpGet]
        //public JsonResult GetMenuRoles(int id)
        //{
        //    try
        //    {
        //        var menuDetails = db.Menus
        //                            .FirstOrDefault(m => m.id == id);

        //        var userPermissions = db.Users_Menus_Roles
        //                                .Where(umr => umr.menuID == id)
        //                                .ToList();

        //        var result = new
        //        {
        //            menuDetails = new
        //            {
        //                id = menuDetails?.id,
        //                name = menuDetails?.name,
        //                icon = menuDetails?.icon,
        //                controller = menuDetails?.controller,
        //                action = menuDetails?.action,
        //                area = menuDetails?.area,
        //                redirect = menuDetails?.redirect,
        //            },
        //            userPermissions = userPermissions.Select(umr => new
        //            {
        //                userNIK = umr.userNIK,
        //                name = umr.Users?.Name,
        //                roleId = umr.Roles?.id,
        //                roleName = umr.Roles?.name,
        //                allowInsert = umr.allowInsert,
        //                allowUpdate = umr.allowUpdate,
        //                allowDelete = umr.allowDelete
        //            }).ToList()
        //        };

        //        return Json(result, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { error = "Failed to retrieve data: " + ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        [HttpGet]
        public JsonResult GetMenuRoles(int id)
        {
            try
            {
                // Find menu details
                var menuDetails = db.Menus.FirstOrDefault(m => m.id == id);

                // Retrieve user permissions for the given menu.
                // Eager load the related Users and Roles tables to avoid N+1 query issues.
                var userPermissions = db.Users_Menus_Roles
                                        .Include(umr => umr.Users)
                                        .Include(umr => umr.Roles) // Make sure this is still valid for your model
                                        .Where(umr => umr.menuID == id)
                                        .ToList();

                // Get all roles from the database to map IDs to names
                var allRoles = db.Roles.ToList();

                var result = new
                {
                    menuDetails = new
                    {
                        id = menuDetails?.id,
                        name = menuDetails?.name,
                        icon = menuDetails?.icon,
                        controller = menuDetails?.controller,
                        action = menuDetails?.action,
                        area = menuDetails?.area,
                        redirect = menuDetails?.redirect,
                    },
                    userPermissions = userPermissions.Select(umr =>
                    {
                        // Check if the roleIds string is valid and not empty
                        string roleNames = "";
                        if (!string.IsNullOrEmpty(umr.roleIds))
                        {
                            // Split the string "1,2,3" into an array of strings ["1", "2", "3"]
                            var roleIdArray = umr.roleIds.Split(',');

                            // Map each ID to its corresponding role name
                            var selectedRoleNames = allRoles
                                .Where(role => roleIdArray.Contains(role.id.ToString()))
                                .Select(role => role.name)
                                .ToList();

                            // Join the role names into a comma-separated string
                            roleNames = string.Join(", ", selectedRoleNames);
                        }

                        return new
                        {
                            userNIK = umr.userNIK,
                            name = umr.Users?.Name,
                            roleIds = umr.roleIds,
                            roleNames = roleNames, 
                            roleName = umr.Roles?.name, 
                            allowInsert = umr.allowInsert,
                            allowUpdate = umr.allowUpdate,
                            allowDelete = umr.allowDelete,
                            allowView = umr.allowView,
                        };
                    }).ToList()
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Failed to retrieve data: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private MenuDto MapMenu(Menus menu, List<Menus> allMenus)
        {
            return new MenuDto
            {
                Id = menu.id,
                Title = menu.name,
                //Url = menu.url, // Properti ini tidak ada di model Menus Anda
                Icon = menu.icon,
                Sequence = menu.sequence,

                // Map properti status dari model database
                isActive = menu.isActive,
                isGlobal = menu.isGlobal,
                isShow = menu.isShow,

                Children = allMenus
                    .Where(m => m.parentID == menu.id)
                    .OrderBy(m => m.sequence)
                    .Select(m => MapMenu(m, allMenus))
                    .ToList()
            };
        }

        [HttpPost]
        public JsonResult SaveOrder(List<MenuOrderDto> items)
        {
            try
            {
                if (items == null || !items.Any())
                    return Json(new { success = false, message = "No data received" });

                foreach (var item in items)
                {
                    var menu = db.Menus.FirstOrDefault(m => m.id == item.Id);
                    if (menu != null)
                    {
                        menu.parentID = item.ParentId;
                        menu.sequence = item.Order;
                        menu.UpdatedDate = DateTime.Now;
                        menu.UpdatedPC = Environment.MachineName;
                    }
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Menu order updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult AddUserRole(int menuId, string userNik, string roleIds, bool allowInsert, bool allowView, bool allowUpdate, bool allowDelete)
        {
            if (string.IsNullOrWhiteSpace(userNik))
            {
                return Json(new { success = false, message = "User NIK is required." });
            }

            try
            {
                // Cek apakah role untuk user + menu + roleId sudah ada
                var existingRole = db.Users_Menus_Roles
                    .FirstOrDefault(umr => umr.menuID == menuId && umr.userNIK == userNik);

                if (existingRole != null)
                {
                    return Json(new { success = false, message = "User already has this role for the menu." });
                }

                var newRole = new Users_Menus_Roles
                {
                    menuID = menuId,
                    userNIK = userNik,
                    roleIds = roleIds,
                    allowView = allowView,
                    allowInsert = allowInsert,
                    allowUpdate = allowUpdate,
                    allowDelete = allowDelete,
                    CreatedDate = DateTime.Now,
                    CreatedPC = Request.UserHostAddress ?? Environment.MachineName, // fallback
                    isActive = true
                };

                db.Users_Menus_Roles.Add(newRole);

                if (db.SaveChanges() > 0)
                {
                    return Json(new { success = true, message = "User role added successfully." });
                }
                return Json(new { success = false, message = "Failed to add user role." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Internal server error. Please contact admin." });
            }
        }

        [HttpPost]
        public JsonResult SaveMenu(Menus menu)
        {
            try
            {
                if (menu == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid menu data"
                    }, JsonRequestBehavior.AllowGet);
                }

                // Optional: validasi nama menu supaya tidak duplikat
                var existing = db.Menus.FirstOrDefault(m => m.name == menu.name);
                if (existing != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Menu name already exists"
                    }, JsonRequestBehavior.AllowGet);
                }

                // Isi field yang dibutuhkan
                menu.CreatedDate = DateTime.Now;
                menu.UpdatedDate = DateTime.Now;
                menu.CreatedPC = Environment.MachineName;
                menu.UpdatedPC = Environment.MachineName;

                db.Menus.Add(menu);
                int saveResult = db.SaveChanges();

                if (saveResult > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Menu saved successfully"
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to save menu"
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult UpdateMenu(Menus menu)
        {
            try
            {
                var existingMenu = db.Menus.FirstOrDefault(m => m.id == menu.id);

                if (existingMenu == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Menu item not found"
                    }, JsonRequestBehavior.AllowGet);
                }

                existingMenu.name = menu.name;
                existingMenu.controller = menu.controller;
                existingMenu.action = menu.action;
                existingMenu.area = menu.area;
                existingMenu.redirect = menu.redirect;
                existingMenu.parentID = menu.parentID;
                existingMenu.icon = menu.icon;
                existingMenu.isActive = menu.isActive;
                existingMenu.isGlobal = menu.isGlobal;
                existingMenu.isShow = menu.isShow;

                existingMenu.UpdatedDate = DateTime.Now;
                existingMenu.UpdatedPC = Environment.MachineName;

                db.Entry(existingMenu).State = System.Data.Entity.EntityState.Modified;

                // Save the changes
                int saveResult = db.SaveChanges();

                if (saveResult > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Menu updated successfully"
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to update menu"
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpPost]
        public JsonResult UpdateUserRole(int menuId, string roleIds, string userNik, bool allowInsert, bool allowView, bool allowUpdate, bool allowDelete)
        {
            try
            {
                // Cari peran pengguna yang ada berdasarkan menuId dan userNik
                var existingRole = db.Users_Menus_Roles
                    .FirstOrDefault(umr => umr.menuID == menuId && umr.userNIK == userNik);

                if (existingRole == null)
                {
                    return Json(new { success = false, message = "User role not found" });
                }

                existingRole.roleIds = roleIds;
                existingRole.allowView = allowView;
                existingRole.allowInsert = allowInsert;
                existingRole.allowUpdate = allowUpdate;
                existingRole.allowDelete = allowDelete;

                existingRole.UpdatedDate = DateTime.Now;
                existingRole.UpdatedPC = Environment.MachineName;
                // existingRole.UpdatedBy = User.Identity.GetUserId(); // Dapatkan dari session

                db.Entry(existingRole).State = System.Data.Entity.EntityState.Modified;
                int saveResult = db.SaveChanges();

                if (saveResult > 0)
                {
                    return Json(new { success = true, message = "User role updated successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update user role" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteMenu(int id)
        {
            try
            {
                var menuToDelete = db.Menus.FirstOrDefault(m => m.id == id);

                if (menuToDelete == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Menu item not found"
                    }, JsonRequestBehavior.AllowGet);
                }

                // HARD DELETE - Hapus permanen dari database
                db.Menus.Remove(menuToDelete);

                int saveResult = db.SaveChanges();

                if (saveResult > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Menu deleted permanently"
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to delete menu"
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteUserRole(int menuId, string userNik)
        {
            try
            {
                // Cari peran pengguna yang ada berdasarkan menuId dan userNik
                var existingRole = db.Users_Menus_Roles
                    .FirstOrDefault(umr => umr.menuID == menuId && umr.userNIK == userNik);

                if (existingRole == null)
                {
                    return Json(new { success = false, message = "User role not found." });
                }

                // Hapus entitas dari database
                db.Users_Menus_Roles.Remove(existingRole);
                int saveResult = db.SaveChanges();

                if (saveResult > 0)
                {
                    return Json(new { success = true, message = "User role deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete user role." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

    }
}
