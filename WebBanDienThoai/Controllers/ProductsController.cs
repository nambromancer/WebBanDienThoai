using System;
using System.Linq;
using System.Web.Mvc;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Controllers
{
    public class ProductsController : Controller
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        // GET: Products
        // Hiển thị sản phẩm theo categoryId, brand, và sort
        public ActionResult Index(int? categoryId, string brand, string sort)
        {
            var productsQuery = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Where(p => p.ProductID != 0); // Ẩn dummy product

            // Filter by category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryID == categoryId.Value);
                var category = db.Categories.Find(categoryId.Value);
                if (category != null)
                {
                    ViewBag.CategoryName = category.CategoryName;
                    ViewBag.CurrentCategoryId = categoryId.Value;
                }
            }

            // Filter by brand
            if (!string.IsNullOrWhiteSpace(brand))
            {
                productsQuery = productsQuery.Where(p => p.Brand == brand);
                ViewBag.SelectedBrand = brand;
            }

            // Lấy danh sách rồi sắp xếp trong bộ nhớ cho các phép toán không dịch được sang SQL (ví dụ % giảm giá)
            var products = productsQuery.ToList();

            switch (sort)
            {
                case "discount_desc":
                    // % giảm giá cao -> thấp
                    products = products
                        .OrderByDescending(p => p.OriginalPrice.HasValue && p.OriginalPrice > p.ProductPrice
                            ? (1 - (p.ProductPrice / p.OriginalPrice.Value))
                            : 0)
                        .ToList();
                    break;

                case "discount_asc":
                    // % giảm giá thấp -> cao
                    products = products
                        .OrderBy(p => p.OriginalPrice.HasValue && p.OriginalPrice > p.ProductPrice
                            ? (1 - (p.ProductPrice / p.OriginalPrice.Value))
                            : 0)
                        .ToList();
                    break;

                case "new_asc":
                    // Cũ -> mới (nếu có CreatedDate hãy dùng CreatedDate thay ProductID)
                    products = products.OrderBy(p => p.ProductID).ToList();
                    break;

                case "new_desc":
                    // Mới -> cũ
                    products = products.OrderByDescending(p => p.ProductID).ToList();
                    break;

                case "price_asc":
                    products = products.OrderBy(p => p.ProductPrice).ToList();
                    break;

                case "price_desc":
                    products = products.OrderByDescending(p => p.ProductPrice).ToList();
                    break;

                default:
                    // Mặc định: mới nhất
                    products = products.OrderByDescending(p => p.ProductID).ToList();
                    break;
            }

            ViewBag.SortOrder = sort;

            return View(products);
        }

        // GET: Products/ProductDetail/5
        public ActionResult ProductDetail(int? id)
        {
            if (id == null || id == 0) // Không cho xem dummy product
            {
                return RedirectToAction("Index", "Home");
            }

            var product = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Include("ProductSpecifications.SpecificationCategory")
                .FirstOrDefault(p => p.ProductID == id.Value && p.ProductID != 0); // Ẩn dummy product

            if (product == null)
            {
                return HttpNotFound();
            }

            // Lấy 10 sản phẩm tương tự (cùng category, loại trừ sản phẩm hiện tại)
            var similarProducts = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Where(p => p.CategoryID == product.CategoryID && p.ProductID != id.Value && p.ProductID != 0)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            // Lấy 10 sản phẩm bán chạy (random từ tất cả categories, loại trừ sản phẩm hiện tại)
            var bestSellers = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Where(p => p.ProductID != id.Value && p.ProductID != 0)
                .OrderBy(p => Guid.NewGuid()) // Random order
                .Take(10)
                .ToList();

            ViewBag.SimilarProducts = similarProducts;
            ViewBag.BestSellers = bestSellers;

            return View(product);
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