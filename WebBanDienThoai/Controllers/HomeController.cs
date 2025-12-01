using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;
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

            Debug.WriteLine("=== Categories ===");
            foreach (var cat in categories)
            {
                Debug.WriteLine($"CategoryID: {cat.CategoryID}, Name: {cat.CategoryName}");
            }

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

                Debug.WriteLine($"=== Products in {category.CategoryName} (CategoryID: {category.CategoryID}) ===");
                Debug.WriteLine($"Count: {products.Count}");
                foreach (var p in products.Take(3))
                {
                    Debug.WriteLine($"  - ProductID: {p.ProductID}, Name: {p.ProductName}, CategoryID: {p.CategoryID}");
                }

                productsByCategory[category.CategoryID] = products;
                allSuggestedProducts.AddRange(products);
            }

            Debug.WriteLine("=== ProductsByCategory Dictionary ===");
            foreach (var kvp in productsByCategory)
            {
                Debug.WriteLine($"CategoryID {kvp.Key}: {kvp.Value.Count} products");
            }

            // Lấy tên sản phẩm cho "Mọi người cũng tìm kiếm"
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
            var cart = GetCart();

            if (cart != null && cart.Any())
            {
                var similarProducts = db.Products
                    .Include("Category")
                    .Include("ProductImages")
                    .Where(p => p.ProductID != 0)
                    .OrderByDescending(p => p.ProductID)
                    .Take(10)
                    .ToList();

                var bestSellers = db.Products
                    .Include("Category")
                    .Include("ProductImages")
                    .Where(p => p.ProductID != 0)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(10)
                    .ToList();

                ViewBag.SimilarProducts = similarProducts;
                ViewBag.BestSellers = bestSellers;
            }

            return View(cart);
        }

        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity = 1)
        {
            var product = db.Products
                .Include("ProductImages")
                .FirstOrDefault(p => p.ProductID == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductID == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var firstImage = product.ProductImages != null && product.ProductImages.Any()
                    ? product.ProductImages.OrderBy(i => i.DisplayOrder ?? int.MaxValue).First().ImageURL
                    : product.ProductImage;

                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ProductImage = firstImage,
                    Price = product.ProductPrice,
                    Quantity = quantity
                });
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                message = "Đã thêm vào giỏ hàng",
                cartCount = cart.Sum(c => c.Quantity)
            });
        }

        [HttpPost]
        public JsonResult UpdateCartQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductID == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCart(cart);
            }

            return Json(new
            {
                success = true,
                cartCount = cart.Sum(c => c.Quantity),
                itemTotal = item != null ? item.TotalPrice : 0,
                cartTotal = cart.Sum(c => c.TotalPrice)
            });
        }

        [HttpPost]
        public JsonResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductID == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return Json(new
            {
                success = true,
                cartCount = cart.Sum(c => c.Quantity),
                cartTotal = cart.Sum(c => c.TotalPrice)
            });
        }

        [HttpPost]
        public JsonResult ClearCart()
        {
            Session["Cart"] = new List<CartItem>();

            return Json(new
            {
                success = true,
                message = "Đã xóa tất cả sản phẩm trong giỏ hàng"
            });
        }

        [HttpGet]
        public JsonResult GetCartCount()
        {
            var cart = GetCart();
            return Json(new
            {
                cartCount = cart.Sum(c => c.Quantity)
            }, JsonRequestBehavior.AllowGet);
        }

        private List<CartItem> GetCart()
        {
            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session["Cart"] = cart;
            }
            return cart;
        }

        private void SaveCart(List<CartItem> cart)
        {
            Session["Cart"] = cart;
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

    public class SearchTermViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
    }
}