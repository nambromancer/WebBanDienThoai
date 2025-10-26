using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Controllers
{
    public class SearchController : Controller
    {
        // GET: /Search?q=iphone 17
        public ActionResult Index(string q)
        {
            var all = GetCatalog();
            IEnumerable<ProductSearchResult> items = all;

            if (!string.IsNullOrWhiteSpace(q))
            {
                items = all.Where(p => p.Name.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            var list = items.ToList();
            ViewBag.Query = q;
            ViewBag.Total = list.Count;
            return View(list);
        }

        // Demo catalog: lấy ảnh/giá từ các view hiện có
        private List<ProductSearchResult> GetCatalog()
        {
            return new List<ProductSearchResult>
            {
                new ProductSearchResult
                {
                    Name = "Iphone 17 256GB",
                    Price = "39.990.990₫",
                    ImageUrl = Url.Content("~/Content/img/ip1.jpg"),
                    CategoryUrl = Url.Action("Phone","Category"),
                    DetailUrl = Url.Action("ProductDetail","Products")
                },
                new ProductSearchResult
                {
                    Name = "Apple Watch Ultra 2",
                    Price = "18.990.000₫",
                    ImageUrl = Url.Content("~/Content/img/apple-watch-ultra-lte-49mm-vien-titanium-day-ocean-tb-600x600.jpg"),
                    CategoryUrl = Url.Action("Smartwatch","Category"),
                    DetailUrl = Url.Action("Smartwatch","Category")
                },
                new ProductSearchResult
                {
                    Name = "Laptop Asus Vivobook 14",
                    Price = "12.990.000₫",
                    ImageUrl = Url.Content("~/Content/img/laptop_asus_vivobook_go_14_e1404fa-eb482w_-_2.png"),
                    CategoryUrl = Url.Action("Laptop","Category"),
                    DetailUrl = Url.Action("Laptop","Category")
                },
                new ProductSearchResult
                {
                    Name = "Sạc Ugreen Robot Uno CD359 15550",
                    Price = "299.000₫",
                    ImageUrl = Url.Content("~/Content/img/adapter-sac-type-c-pd-gan-30w-ugreen-robot-uno-cd359-15550-thumb-638943079923012712-600x600.jpg"),
                    CategoryUrl = Url.Action("Accessories","Category"),
                    DetailUrl = Url.Action("Accessories","Category")
                }
            };
        }
    }
}