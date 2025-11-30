using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDienThoai.Models;
using System.Diagnostics;

namespace WebBanDienThoai.Controllers
{
    public class HomeController : Controller
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        public ActionResult Index()
        {
            // Lấy 4 categories đầu tiên
            var categories = db.Categories
                .OrderBy(c => c.CategoryID)
                .Take(4)
                .ToList();

            // Debug: Log categories
            Debug.WriteLine("=== Categories ===");
            foreach (var cat in categories)
            {
                Debug.WriteLine($"CategoryID: {cat.CategoryID}, Name: {cat.CategoryName}");
            }

            // Tạo dictionary để lưu sản phẩm theo category
            var productsByCategory = new Dictionary<int, List<Product>>();
            var allSuggestedProducts = new List<Product>();

            foreach (var category in categories)
            {
                var products = db.Products
                    .Include("Category")
                    .Include("ProductImages")
                    .Where(p => p.CategoryID == category.CategoryID && p.ProductID != 0)
                    .OrderByDescending(p => p.ProductID)
                    .Take(15)
                    .ToList();

                // Debug: Log products per category
                Debug.WriteLine($"=== Products in {category.CategoryName} (CategoryID: {category.CategoryID}) ===");
                Debug.WriteLine($"Count: {products.Count}");
                foreach (var p in products.Take(3))
                {
                    Debug.WriteLine($"  - ProductID: {p.ProductID}, Name: {p.ProductName}, CategoryID: {p.CategoryID}");
                }

                productsByCategory[category.CategoryID] = products;
                allSuggestedProducts.AddRange(products);
            }

            // Debug: Log productsByCategory structure
            Debug.WriteLine("=== ProductsByCategory Dictionary ===");
            foreach (var kvp in productsByCategory)
            {
                Debug.WriteLine($"CategoryID {kvp.Key}: {kvp.Value.Count} products");
            }

            // Lấy tên các sản phẩm để hiển thị ở phần "Mọi người cũng tìm kiếm"
            var searchTerms = db.Products
                .Where(p => p.ProductID != 0 && !string.IsNullOrEmpty(p.ProductName))
                .OrderByDescending(p => p.ProductID)
                .Take(45)
                .ToList()
                .Select(p => new SearchTermViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName
                })
                .ToList();

            ViewBag.Categories = categories;
            ViewBag.ProductsByCategory = productsByCategory;
            ViewBag.SuggestedProducts = allSuggestedProducts.Distinct().Take(60).ToList();
            ViewBag.SearchTerms = searchTerms;

            return View();
        }

        public ActionResult Cart()
        {
            ViewBag.Message = "";
            return View();
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

    // ViewModel cho search terms
    public class SearchTermViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
    }
}