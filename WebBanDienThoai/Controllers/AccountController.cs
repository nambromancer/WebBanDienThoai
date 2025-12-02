using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using PagedList;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;

namespace WebBanDienThoai.Controllers
{
    public class AccountController : Controller
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        public ActionResult MyAccount()
        {
            if (Session["UserPhone"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("MyAccount") });
            }

            return RedirectToAction("UserProfile");
        }

        public ActionResult UserProfile()
        {
            if (Session["UserPhone"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("UserProfile") });
            }

            var userPhone = Session["UserPhone"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == userPhone);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            var model = new ProfileViewModel
            {
                CustomerID = customer.CustomerID,
                CustomerName = customer.CustomerName,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.CustomerEmail,
                Address = customer.CustomerAddress,
                DateOfBirth = customer.DateOfBirth,
                Gender = customer.Gender
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(ProfileViewModel model)
        {
            if (Session["UserPhone"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var userPhone = Session["UserPhone"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == userPhone);

            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng." });
            }

            try
            {
                customer.CustomerEmail = model.Email;
                customer.CustomerAddress = model.Address;
                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật thông tin thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (Session["UserPhone"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var userPhone = Session["UserPhone"].ToString();
            var user = db.Users.Find(userPhone);

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            string hashedOldPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(model.OldPassword, "MD5");
            if (user.Password != hashedOldPassword)
            {
                return Json(new { success = false, message = "Mật khẩu cũ không đúng." });
            }

            try
            {
                user.Password = FormsAuthentication.HashPasswordForStoringInConfigFile(model.NewPassword, "MD5");
                db.SaveChanges();

                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public ActionResult PurchaseHistory(int? page)
        {
            if (Session["UserPhone"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("PurchaseHistory") });
            }

            var userPhone = Session["UserPhone"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == userPhone);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            int pageSize = 2; // 2 đơn hàng mỗi trang
            int pageNumber = (page ?? 1);

            var orders = db.Orders
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .Include("OrderDetails.Product.ProductImages")
                .Where(o => o.CustomerID == customer.CustomerID)
                .OrderByDescending(o => o.OrderDate)
                .ToPagedList(pageNumber, pageSize);

            return View(orders);
        }

        public ActionResult OrderDetail(int orderId)
        {
            if (Session["UserPhone"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("OrderDetail", new { orderId = orderId }) });
            }

            var userPhone = Session["UserPhone"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == userPhone);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            var order = db.Orders
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .Include("OrderDetails.Product.ProductImages")
                .Include("Customer")
                .FirstOrDefault(o => o.OrderID == orderId && o.CustomerID == customer.CustomerID);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("PurchaseHistory");
            }

            return View(order);
        }

        [HttpPost]
        public ActionResult CancelOrder(int orderId)
        {
            if (Session["UserPhone"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var userPhone = Session["UserPhone"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == userPhone);

            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng." });
            }

            var order = db.Orders.Find(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            // ✅ Kiểm tra quyền sở hữu đơn hàng
            if (order.CustomerID != customer.CustomerID)
            {
                return Json(new { success = false, message = "Bạn không có quyền hủy đơn hàng này." });
            }

            // ✅ Chỉ cho phép hủy đơn đang chờ xử lý hoặc đang xử lý (TIẾNG VIỆT)
            if (order.OrderStatus != "Chờ xử lý" && order.OrderStatus != "Đang xử lý")
            {
                return Json(new { success = false, message = "Không thể hủy đơn hàng này. Chỉ có thể hủy đơn hàng đang chờ xử lý hoặc đang xử lý." });
            }

            try
            {
                // ✅ Cập nhật trạng thái thành "Đã hủy" (TIẾNG VIỆT)
                order.OrderStatus = "Đã hủy";
                db.SaveChanges();

                return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = db.Users.Find(model.PhoneNumber);
            if (existingUser != null)
            {
                ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được đăng ký.");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                var existingCustomer = db.Customers.FirstOrDefault(c => c.CustomerEmail == model.Email);
                if (existingCustomer != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(model);
                }
            }

            try
            {
                var user = new User
                {
                    PhoneNumber = model.PhoneNumber,
                    Password = FormsAuthentication.HashPasswordForStoringInConfigFile(model.Password, "MD5"),
                    UserRole = "1",
                    CreatedDate = DateTime.Now
                };
                db.Users.Add(user);
                db.SaveChanges();

                var customer = new Customer
                {
                    CustomerName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    CustomerEmail = model.Email,
                    DateOfBirth = model.BirthDate,
                    Gender = model.Gender
                };
                db.Customers.Add(customer);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string hashedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(model.Password, "MD5");
            var user = db.Users.FirstOrDefault(u =>
                u.PhoneNumber == model.PhoneNumber &&
                u.Password == hashedPassword);

            if (user == null)
            {
                ModelState.AddModelError("", "Số điện thoại hoặc mật khẩu không đúng.");
                return View(model);
            }

            var tempCart = Session["Cart"];

            Session["UserPhone"] = user.PhoneNumber;
            Session["UserRole"] = user.UserRole;

            if (tempCart != null)
            {
                Session["Cart"] = tempCart;
            }

            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == user.PhoneNumber);
            if (customer != null && !string.IsNullOrEmpty(customer.CustomerName))
            {
                Session["UserName"] = customer.CustomerName;
            }
            else
            {
                Session["UserName"] = user.PhoneNumber;
            }

            FormsAuthentication.SetAuthCookie(user.PhoneNumber, model.RememberMe);

            if (user.UserRole == "0")
            {
                TempData["SuccessMessage"] = "Chào mừng Admin!";
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            else
            {
                TempData["SuccessMessage"] = "Đăng nhập thành công!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = db.Customers.FirstOrDefault(c =>
                c.PhoneNumber == model.PhoneNumber &&
                c.CustomerEmail == model.Email);

            if (customer == null)
            {
                ModelState.AddModelError("", "Số điện thoại và email không khớp với thông tin trong hệ thống.");
                return View(model);
            }

            var user = db.Users.Find(model.PhoneNumber);
            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(model);
            }

            Session["ResetPasswordPhone"] = model.PhoneNumber;
            TempData["SuccessMessage"] = "Xác thực thành công! Vui lòng nhập mật khẩu mới.";
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public ActionResult ResetPassword()
        {
            if (Session["ResetPasswordPhone"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                PhoneNumber = Session["ResetPasswordPhone"].ToString()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var phoneNumber = Session["ResetPasswordPhone"]?.ToString();
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("ForgotPassword");
            }

            var user = db.Users.Find(phoneNumber);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Tài khoản không tồn tại.";
                return RedirectToAction("ForgotPassword");
            }

            user.Password = FormsAuthentication.HashPasswordForStoringInConfigFile(model.NewPassword, "MD5");
            db.SaveChanges();

            Session.Remove("ResetPasswordPhone");

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
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