using System;
using System.Linq;
using System.Web.Mvc;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Controllers
{
    public class ProductsController : Controller
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        // Hiển thị sản phẩm theo danh mục, thương hiệu và sắp xếp
        public ActionResult Index(int? categoryId, string brand, string sort)
        {
            var productsQuery = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Where(p => p.ProductID != 0);

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

            if (!string.IsNullOrWhiteSpace(brand))
            {
                productsQuery = productsQuery.Where(p => p.Brand == brand);
                ViewBag.SelectedBrand = brand;
            }

            var products = productsQuery.ToList();

            // Sắp xếp theo % giảm giá, ngày tạo, giá
            switch (sort)
            {
                case "discount_desc":
                    products = products
                        .OrderByDescending(p => p.OriginalPrice.HasValue && p.OriginalPrice > p.ProductPrice
                            ? (1 - (p.ProductPrice / p.OriginalPrice.Value))
                            : 0)
                        .ToList();
                    break;

                case "discount_asc":
                    products = products
                        .OrderBy(p => p.OriginalPrice.HasValue && p.OriginalPrice > p.ProductPrice
                            ? (1 - (p.ProductPrice / p.OriginalPrice.Value))
                            : 0)
                        .ToList();
                    break;

                case "new_asc":
                    products = products.OrderBy(p => p.ProductID).ToList();
                    break;

                case "new_desc":
                    products = products.OrderByDescending(p => p.ProductID).ToList();
                    break;

                case "price_asc":
                    products = products.OrderBy(p => p.ProductPrice).ToList();
                    break;

                case "price_desc":
                    products = products.OrderByDescending(p => p.ProductPrice).ToList();
                    break;

                default:
                    products = products.OrderByDescending(p => p.ProductID).ToList();
                    break;
            }

            ViewBag.SortOrder = sort;

            return View(products);
        }

        public ActionResult ProductDetail(int? id)
        {
            if (id == null || id == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var product = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Include("ProductSpecifications.SpecificationCategory")
                .FirstOrDefault(p => p.ProductID == id.Value && p.ProductID != 0);

            if (product == null)
            {
                return HttpNotFound();
            }

            var similarProducts = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Where(p => p.CategoryID == product.CategoryID && p.ProductID != id.Value && p.ProductID != 0)
                .OrderByDescending(p => p.ProductID)
                .Take(10)
                .ToList();

            var bestSellers = db.Products
                .Include("Category")
                .Include("ProductImages")
                .Where(p => p.ProductID != id.Value && p.ProductID != 0)
                .OrderBy(p => Guid.NewGuid())
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