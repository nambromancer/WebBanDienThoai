using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Controllers
{
    public class AccountController : Controller
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        // GET: /Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
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

        // GET: /Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Obsolete]
        public ActionResult Login(LoginViewModel model)
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

            Session["UserPhone"] = user.PhoneNumber;
            Session["UserRole"] = user.UserRole;

            // Try to get customer display name (if a Customer record exists for this phone)
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == user.PhoneNumber);
            if (customer != null && !string.IsNullOrEmpty(customer.CustomerName))
            {
                Session["UserName"] = customer.CustomerName;
            }
            else
            {
                // Fallback to phone number when no friendly name available
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
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra customer có tồn tại với SĐT và Email khớp không
            var customer = db.Customers.FirstOrDefault(c =>
                c.PhoneNumber == model.PhoneNumber &&
                c.CustomerEmail == model.Email);

            if (customer == null)
            {
                ModelState.AddModelError("", "Số điện thoại và email không khớp với thông tin trong hệ thống.");
                return View(model);
            }

            // Kiểm tra user tồn tại
            var user = db.Users.Find(model.PhoneNumber);
            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return View(model);
            }

            // Lưu thông tin vào Session để xác thực ở bước tiếp theo
            Session["ResetPasswordPhone"] = model.PhoneNumber;
            TempData["SuccessMessage"] = "Xác thực thành công! Vui lòng nhập mật khẩu mới.";
            return RedirectToAction("ResetPassword");
        }

        // GET: /Account/ResetPassword
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

        // POST: /Account/ResetPassword
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

            // Cập nhật mật khẩu mới
            user.Password = FormsAuthentication.HashPasswordForStoringInConfigFile(model.NewPassword, "MD5");
            db.SaveChanges();

            // Xóa session
            Session.Remove("ResetPasswordPhone");

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
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