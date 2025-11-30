using System.Web.Mvc;
using WebBanDienThoai.Filters;

namespace WebBanDienThoai.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class BaseAdminController : Controller
    {
        protected string GetCurrentUserPhone()
        {
            return Session["UserPhone"]?.ToString();
        }

        protected string GetCurrentUserRole()
        {
            return Session["UserRole"]?.ToString();
        }
    }
}