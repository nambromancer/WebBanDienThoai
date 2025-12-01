using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;
using PagedList;

namespace WebBanDienThoai.Areas.Admin.Controllers
{
    public class CustomersController : BaseAdminController
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        // GET: Admin/Customers
        public ActionResult Index(string searchTerm, string genderFilter, string sortOrder, int? page)
        {
            var model = new SearchCustomerVM();
            var customers = db.Customers.AsQueryable();

            // Tìm kiếm theo tên, email, số điện thoại
            if (!string.IsNullOrEmpty(searchTerm))
            {
                customers = customers.Where(c =>
                    c.CustomerName.Contains(searchTerm) ||
                    c.CustomerEmail.Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm));
                model.SearchTerm = searchTerm;
            }

            // Lọc theo giới tính
            if (!string.IsNullOrEmpty(genderFilter))
            {
                customers = customers.Where(c => c.Gender == genderFilter);
                model.GenderFilter = genderFilter;
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "name_desc":
                    customers = customers.OrderByDescending(c => c.CustomerName);
                    break;
                case "email_asc":
                    customers = customers.OrderBy(c => c.CustomerEmail);
                    break;
                case "email_desc":
                    customers = customers.OrderByDescending(c => c.CustomerEmail);
                    break;
                case "date_asc":
                    customers = customers.OrderBy(c => c.DateOfBirth);
                    break;
                case "date_desc":
                    customers = customers.OrderByDescending(c => c.DateOfBirth);
                    break;
                default:
                    customers = customers.OrderBy(c => c.CustomerName);
                    break;
            }
            model.SortOrder = sortOrder;

            int pageNumber = page ?? 1;
            int pageSize = 10;
            model.Customers = customers.ToPagedList(pageNumber, pageSize);

            // Tạo SelectList cho dropdown giới tính
            ViewBag.GenderList = new SelectList(new[]
            {
                new { Value = "", Text = "-- Tất cả giới tính --" },
                new { Value = "Nam", Text = "Nam" },
                new { Value = "Nữ", Text = "Nữ" },
                new { Value = "Khác", Text = "Khác" }
            }, "Value", "Text");

            return View(model);
        }

        // GET: Admin/Customers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Customer customer = db.Customers
                .Include(c => c.Orders)
                .Include(c => c.User)
                .FirstOrDefault(c => c.CustomerID == id);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View(customer);
        }

        // GET: Admin/Customers/Create
        public ActionResult Create()
        {
            ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" });
            return View();
        }

        // POST: Admin/Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CustomerID,CustomerName,PhoneNumber,CustomerEmail,CustomerAddress,DateOfBirth,Gender")] Customer customer)
        {
            // ✅ VALIDATION SỐ ĐIỆN THOẠI - ĐỒNG BỘ VỚI CUSTOMER REGISTRATION
            if (!Regex.IsMatch(customer.PhoneNumber, @"^(0[3|5|7|8|9])+([0-9]{8})$"))
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại phải bắt đầu bằng 03, 05, 07, 08, 09 và có 10 chữ số.");
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa
                var existingCustomer = db.Customers.FirstOrDefault(c => c.CustomerEmail == customer.CustomerEmail);
                if (existingCustomer != null)
                {
                    ModelState.AddModelError("CustomerEmail", "Email này đã được sử dụng.");
                    ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" });
                    return View(customer);
                }

                // Kiểm tra số điện thoại đã tồn tại chưa
                var existingPhone = db.Customers.FirstOrDefault(c => c.PhoneNumber == customer.PhoneNumber);
                if (existingPhone != null)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng.");
                    ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" });
                    return View(customer);
                }

                db.Customers.Add(customer);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" });
            return View(customer);
        }

        // GET: Admin/Customers/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Customer customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }

            ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" }, customer.Gender);
            return View(customer);
        }

        // POST: Admin/Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CustomerID,CustomerName,PhoneNumber,CustomerEmail,CustomerAddress,DateOfBirth,Gender")] Customer customer)
        {
            // ✅ VALIDATION SỐ ĐIỆN THOẠI - ĐỒNG BỘ VỚI CUSTOMER REGISTRATION
            if (!Regex.IsMatch(customer.PhoneNumber, @"^(0[3|5|7|8|9])+([0-9]{8})$"))
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại phải bắt đầu bằng 03, 05, 07, 08, 09 và có 10 chữ số.");
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa (trừ chính khách hàng này)
                var existingCustomer = db.Customers
                    .FirstOrDefault(c => c.CustomerEmail == customer.CustomerEmail && c.CustomerID != customer.CustomerID);
                if (existingCustomer != null)
                {
                    ModelState.AddModelError("CustomerEmail", "Email này đã được sử dụng bởi khách hàng khác.");
                    ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" }, customer.Gender);
                    return View(customer);
                }

                // Kiểm tra số điện thoại đã tồn tại chưa (trừ chính khách hàng này)
                var existingPhone = db.Customers
                    .FirstOrDefault(c => c.PhoneNumber == customer.PhoneNumber && c.CustomerID != customer.CustomerID);
                if (existingPhone != null)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng bởi khách hàng khác.");
                    ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" }, customer.Gender);
                    return View(customer);
                }

                db.Entry(customer).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.GenderList = new SelectList(new[] { "Nam", "Nữ", "Khác" }, customer.Gender);
            return View(customer);
        }

        // GET: Admin/Customers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Customer customer = db.Customers
                .Include(c => c.Orders)
                .FirstOrDefault(c => c.CustomerID == id);

            if (customer == null)
            {
                return HttpNotFound();
            }

            return View(customer);
        }

        // POST: Admin/Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Customer customer = db.Customers
                .Include(c => c.Orders)
                .FirstOrDefault(c => c.CustomerID == id);

            // Kiểm tra có đơn hàng nào không
            if (customer.Orders != null && customer.Orders.Any())
            {
                TempData["ErrorMessage"] = string.Format(
                    "Không thể xóa! Khách hàng '{0}' đã có {1} đơn hàng. Vui lòng xóa các đơn hàng trước.",
                    customer.CustomerName,
                    customer.Orders.Count);
                return RedirectToAction("Index");
            }

            // Xóa User liên kết nếu có
            if (customer.User != null)
            {
                db.Users.Remove(customer.User);
            }

            db.Customers.Remove(customer);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
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