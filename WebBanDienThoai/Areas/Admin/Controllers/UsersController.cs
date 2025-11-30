using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.Security;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;
using PagedList;

namespace WebBanDienThoai.Areas.Admin.Controllers
{
    public class UsersController : BaseAdminController
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        // GET: Admin/Users
        public ActionResult Index(string searchTerm, string roleFilter, string sortOrder, int? page)
        {
            var model = new SearchUserVM();
            var users = db.Users.Include(u => u.Customers).AsQueryable();

            // Tìm kiếm theo số điện thoại
            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(u => u.PhoneNumber.Contains(searchTerm));
                model.SearchTerm = searchTerm;
            }

            // Lọc theo role
            if (!string.IsNullOrEmpty(roleFilter))
            {
                users = users.Where(u => u.UserRole == roleFilter);
                model.RoleFilter = roleFilter;
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "phone_desc":
                    users = users.OrderByDescending(u => u.PhoneNumber);
                    break;
                case "role_asc":
                    users = users.OrderBy(u => u.UserRole);
                    break;
                case "role_desc":
                    users = users.OrderByDescending(u => u.UserRole);
                    break;
                case "date_asc":
                    users = users.OrderBy(u => u.CreatedDate);
                    break;
                case "date_desc":
                    users = users.OrderByDescending(u => u.CreatedDate);
                    break;
                default:
                    users = users.OrderBy(u => u.PhoneNumber);
                    break;
            }
            model.SortOrder = sortOrder;

            int pageNumber = page ?? 1;
            int pageSize = 10;
            model.Users = users.ToPagedList(pageNumber, pageSize);

            // Tạo SelectList cho dropdown role
            ViewBag.RoleList = new SelectList(new[]
            {
                new { Value = "", Text = "-- Tất cả vai trò --" },
                new { Value = "0", Text = "Admin" },
                new { Value = "1", Text = "Khách hàng" }
            }, "Value", "Text");

            return View(model);
        }

        // GET: Admin/Users/Details/5
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users
                .Include(u => u.Customers)
                .FirstOrDefault(u => u.PhoneNumber == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        // GET: Admin/Users/Create
        public ActionResult Create()
        {
            ViewBag.RoleList = new SelectList(new[]
            {
                new { Value = "0", Text = "Admin" },
                new { Value = "1", Text = "Khách hàng" }
            }, "Value", "Text");

            return View();
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PhoneNumber,Password,UserRole")] User user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra số điện thoại đã tồn tại chưa
                var existingUser = db.Users.Find(user.PhoneNumber);
                if (existingUser != null)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được đăng ký.");
                    ViewBag.RoleList = new SelectList(new[]
                    {
                        new { Value = "0", Text = "Admin" },
                        new { Value = "1", Text = "Khách hàng" }
                    }, "Value", "Text", user.UserRole);
                    return View(user);
                }

                // Kiểm tra UserRole
                if (string.IsNullOrEmpty(user.UserRole))
                {
                    ModelState.AddModelError("UserRole", "Vui lòng chọn vai trò.");
                    ViewBag.RoleList = new SelectList(new[]
                    {
                        new { Value = "0", Text = "Admin" },
                        new { Value = "1", Text = "Khách hàng" }
                    }, "Value", "Text");
                    return View(user);
                }

                // Mã hóa mật khẩu
                user.Password = FormsAuthentication.HashPasswordForStoringInConfigFile(user.Password, "MD5");
                user.CreatedDate = DateTime.Now;

                db.Users.Add(user);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm tài khoản thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.RoleList = new SelectList(new[]
            {
                new { Value = "0", Text = "Admin" },
                new { Value = "1", Text = "Khách hàng" }
            }, "Value", "Text", user.UserRole);
            return View(user);
        }

        // GET: Admin/Users/Edit/5
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.RoleList = new SelectList(new[]
            {
                new { Value = "0", Text = "Admin" },
                new { Value = "1", Text = "Khách hàng" }
            }, "Value", "Text", user.UserRole);

            return View(user);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PhoneNumber,UserRole")] User user, string NewPassword)
        {
            if (ModelState.IsValid)
            {
                var existingUser = db.Users.Find(user.PhoneNumber);
                if (existingUser == null)
                {
                    return HttpNotFound();
                }

                // Cập nhật role
                existingUser.UserRole = user.UserRole;

                // Cập nhật mật khẩu nếu có
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    existingUser.Password = FormsAuthentication.HashPasswordForStoringInConfigFile(NewPassword, "MD5");
                }

                db.Entry(existingUser).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật tài khoản thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.RoleList = new SelectList(new[]
            {
                new { Value = "0", Text = "Admin" },
                new { Value = "1", Text = "Khách hàng" }
            }, "Value", "Text", user.UserRole);
            return View(user);
        }

        // GET: Admin/Users/Delete/5
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users
                .Include(u => u.Customers)
                .FirstOrDefault(u => u.PhoneNumber == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            User user = db.Users
                .Include(u => u.Customers)
                .FirstOrDefault(u => u.PhoneNumber == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra có khách hàng liên kết không
            if (user.Customers != null && user.Customers.Any())
            {
                TempData["ErrorMessage"] = string.Format(
                    "Không thể xóa! Tài khoản này có {0} khách hàng liên kết. Vui lòng xóa hoặc chuyển khách hàng trước.",
                    user.Customers.Count);
                return RedirectToAction("Index");
            }

            db.Users.Remove(user);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Xóa tài khoản thành công!";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}