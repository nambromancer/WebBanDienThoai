using System;
using System.Linq;
using System.Web.Mvc;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Controllers
{
    public class SearchController : Controller
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        public ActionResult Index(string q)
        {
            var productsQuery = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Where(p => p.ProductID != 0);

            // Tìm kiếm theo từng từ khóa trong tên sản phẩm
            if (!string.IsNullOrWhiteSpace(q))
            {
                var terms = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var t in terms)
                {
                    var term = t.Trim();
                    if (term.Length == 0) continue;
                    productsQuery = productsQuery.Where(p => p.ProductName != null && p.ProductName.Contains(term));
                }
            }

            var products = productsQuery.OrderByDescending(p => p.ProductID).ToList();

            ViewBag.Query = q ?? "";
            ViewBag.Total = products.Count;

            return View(products);
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