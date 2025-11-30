using System.Web;
using System.Web.Mvc;

namespace WebBanDienThoai.Filters
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Kiểm tra xem user đã đăng nhập chưa
            if (httpContext.Session["UserPhone"] == null)
            {
                return false;
            }

            // Kiểm tra role có phải Admin (0) không
            var userRole = httpContext.Session["UserRole"]?.ToString();
            return userRole == "0";
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Nếu chưa đăng nhập
            if (filterContext.HttpContext.Session["UserPhone"] == null)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            else
            {
                // Đã đăng nhập nhưng không phải Admin
                filterContext.Controller.TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang quản trị.";
                filterContext.Result = new RedirectResult("~/");
            }
        }
    }
}