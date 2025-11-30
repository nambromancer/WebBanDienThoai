using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace WebBanDienThoai.Areas.Admin.Controllers
{
    public class ProductsController : BaseAdminController
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        private string DownloadImageToContent(string url)
        {
            var uri = new Uri(url);
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif" };
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!allowed.Contains(ext, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ chấp nhận PNG, JPG, JPEG, GIF.");

            var folderVirtual = "~/Content/images/";
            var folderPhysical = Server.MapPath(folderVirtual);
            Directory.CreateDirectory(folderPhysical);

            var filename = Guid.NewGuid().ToString("N") + ext;
            var physicalPath = Path.Combine(folderPhysical, filename);

            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/5.0");
                client.DownloadFile(uri, physicalPath);
            }

            return folderVirtual + filename;
        }

        // GET: Admin/Products
        public ActionResult Index(string searchTerm, decimal? minPrice, decimal? maxPrice, int? categoryFilter, string sortOrder, int? page)
        {
            var model = new SearchProductVM();
            var products = db.Products.Where(p => p.ProductID != 0).AsQueryable();

            // Tìm kiếm theo tên, mô tả, danh mục
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p =>
                        p.ProductName.Contains(searchTerm) ||
                        p.ProductDescription.Contains(searchTerm) ||
                        p.Category.CategoryName.Contains(searchTerm));
                model.SearchTerm = searchTerm;
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice >= minPrice.Value);
                model.MinPrice = minPrice;
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.ProductPrice <= maxPrice.Value);
                model.MaxPrice = maxPrice;
            }

            // Lọc theo danh mục
            if (categoryFilter.HasValue)
            {
                products = products.Where(p => p.CategoryID == categoryFilter.Value);
                model.CategoryFilter = categoryFilter;
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "name_asc":
                    products = products.OrderBy(p => p.ProductName);
                    break;
                case "name_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                case "price_asc":
                    products = products.OrderBy(p => p.ProductPrice);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.ProductPrice);
                    break;
                default:
                    products = products.OrderBy(p => p.ProductName);
                    break;
            }
            model.SortOrder = sortOrder;

            int pageNumber = page ?? 1;
            int pageSize = 10;
            model.Products = products.ToPagedList(pageNumber, pageSize);

            // Tạo SelectList cho dropdown danh mục
            ViewBag.Categories = new SelectList(db.Categories, "CategoryID", "CategoryName");

            return View(model);
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            var model = new ProductWithSpecificationsVM();
            return View(model);
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductWithSpecificationsVM model)
        {
            if (ModelState.IsValid)
            {
                var product = model.Product;

                // ✅ FIX: Cho phép ProductDescription null hoặc empty cho phụ kiện
                if (string.IsNullOrWhiteSpace(product.ProductDescription))
                {
                    product.ProductDescription = string.Empty;
                }

                // Thêm product vào database
                db.Products.Add(product);
                db.SaveChanges();

                // ✅ FIX: Xử lý specifications cho phụ kiện (không cần category)
                if (model.SpecificationCategories != null)
                {
                    foreach (var specCat in model.SpecificationCategories)
                    {
                        if (specCat.Specifications != null)
                        {
                            // Lọc các spec có dữ liệu hợp lệ
                            var validSpecs = specCat.Specifications
                                .Where(s => !string.IsNullOrWhiteSpace(s.SpecName) || !string.IsNullOrWhiteSpace(s.SpecValue))
                                .ToList();

                            if (validSpecs.Any())
                            {
                                int categoryIdToUse = specCat.SpecCategoryID;

                                // Chỉ tạo default category khi SpecCategoryID = 0
                                if (categoryIdToUse == 0)
                                {
                                    try
                                    {
                                        categoryIdToUse = GetOrCreateDefaultSpecCategory(product.CategoryID);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Error creating default spec category: " + ex.Message);
                                        TempData["WarningMessage"] = "Không thể tạo danh mục thông số mặc định. Vui lòng kiểm tra lại.";
                                        continue; // Bỏ qua nhóm spec này
                                    }
                                }

                                // Lưu từng specification
                                foreach (var spec in validSpecs)
                                {
                                    var productSpec = new ProductSpecification
                                    {
                                        ProductID = product.ProductID,
                                        SpecCategoryID = categoryIdToUse,
                                        SpecName = !string.IsNullOrWhiteSpace(spec.SpecName) ? spec.SpecName.Trim() : string.Empty,
                                        SpecValue = !string.IsNullOrWhiteSpace(spec.SpecValue) ? spec.SpecValue.Replace("\r\n", "\n").Trim() : string.Empty,
                                        DisplayOrder = spec.DisplayOrder
                                    };
                                    db.ProductSpecifications.Add(productSpec);
                                }
                            }
                        }
                    }

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error saving specifications: " + ex.Message);
                        TempData["WarningMessage"] = "Lưu thông số kỹ thuật không thành công: " + ex.Message;
                    }
                }

                // Lưu product images
                if (model.ProductImages != null && model.ProductImages.Any())
                {
                    int displayOrder = 1;
                    bool hasMainImage = false;

                    foreach (var img in model.ProductImages)
                    {
                        if (!string.IsNullOrWhiteSpace(img.ImageURL))
                        {
                            try
                            {
                                var savedImagePath = DownloadImageToContent(img.ImageURL);

                                var productImage = new ProductImage
                                {
                                    ProductID = product.ProductID,
                                    ImageURL = savedImagePath,
                                    DisplayOrder = img.DisplayOrder.HasValue ? img.DisplayOrder.Value : displayOrder++,
                                    IsMainImage = img.IsMainImage
                                };

                                db.ProductImages.Add(productImage);

                                // Cập nhật ProductImage field với ảnh chính để backward compatibility
                                if (img.IsMainImage && !hasMainImage)
                                {
                                    product.ProductImage = savedImagePath;
                                    hasMainImage = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Error downloading image: " + ex.Message);
                            }
                        }
                    }

                    // Nếu không có ảnh nào được đánh dấu là main, dùng ảnh đầu tiên
                    if (!hasMainImage && model.ProductImages.Any())
                    {
                        var firstImage = db.ProductImages
                            .Where(pi => pi.ProductID == product.ProductID)
                            .OrderBy(pi => pi.DisplayOrder)
                            .FirstOrDefault();

                        if (firstImage != null)
                        {
                            product.ProductImage = firstImage.ImageURL;
                        }
                    }

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error saving images: " + ex.Message);
                        TempData["WarningMessage"] = "Lưu hình ảnh không thành công: " + ex.Message;
                    }
                }

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", model.Product.CategoryID);

            // Reload specification categories if validation fails
            if (model.SpecificationCategories == null || !model.SpecificationCategories.Any())
            {
                model.SpecificationCategories = new List<SpecificationCategoryInputVM>();
                var specCategories = db.SpecificationCategories
                    .OrderBy(sc => sc.DisplayOrder.HasValue ? sc.DisplayOrder.Value : int.MaxValue)
                    .ThenBy(sc => sc.SpecCategoryName)
                    .ToList();

                foreach (var specCat in specCategories)
                {
                    model.SpecificationCategories.Add(new SpecificationCategoryInputVM
                    {
                        SpecCategoryID = specCat.SpecCategoryID,
                        SpecCategoryName = specCat.SpecCategoryName,
                        SpecCategoryIcon = specCat.SpecCategoryIcon,
                        DisplayOrder = specCat.DisplayOrder,
                        Specifications = new List<SpecificationInputVM>()
                    });
                }
            }

            return View(model);
        }

        // GET: Admin/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);

            var model = new ProductWithSpecificationsVM
            {
                Product = product
            };

            var specCategories = db.SpecificationCategories
                .OrderBy(sc => sc.DisplayOrder.HasValue ? sc.DisplayOrder.Value : int.MaxValue)
                .ThenBy(sc => sc.SpecCategoryName)
                .ToList();

            var existingSpecs = db.ProductSpecifications
                .Where(ps => ps.ProductID == id.Value)
                .ToList();

            foreach (var specCat in specCategories)
            {
                var specCatVM = new SpecificationCategoryInputVM
                {
                    SpecCategoryID = specCat.SpecCategoryID,
                    SpecCategoryName = specCat.SpecCategoryName,
                    SpecCategoryIcon = specCat.SpecCategoryIcon,
                    DisplayOrder = specCat.DisplayOrder,
                    Specifications = new List<SpecificationInputVM>()
                };

                var categorySpecs = existingSpecs
                    .Where(es => es.SpecCategoryID == specCat.SpecCategoryID)
                    .OrderBy(es => es.DisplayOrder.HasValue ? es.DisplayOrder.Value : int.MaxValue)
                    .ToList();

                foreach (var spec in categorySpecs)
                {
                    specCatVM.Specifications.Add(new SpecificationInputVM
                    {
                        SpecID = spec.SpecID,
                        SpecName = spec.SpecName,
                        SpecValue = spec.SpecValue,
                        DisplayOrder = spec.DisplayOrder
                    });
                }

                model.SpecificationCategories.Add(specCatVM);
            }

            // Load existing product images
            var existingImages = db.ProductImages
                .Where(pi => pi.ProductID == id.Value)
                .OrderBy(pi => pi.DisplayOrder.HasValue ? pi.DisplayOrder.Value : int.MaxValue)
                .ToList();

            foreach (var img in existingImages)
            {
                model.ProductImages.Add(new ProductImageInputVM
                {
                    ImageID = img.ImageID,
                    ImageURL = img.ImageURL,
                    DisplayOrder = img.DisplayOrder,
                    IsMainImage = img.IsMainImage.HasValue ? img.IsMainImage.Value : false
                });
            }

            return View(model);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductWithSpecificationsVM model)
        {
            if (ModelState.IsValid)
            {
                var product = model.Product;

                // ✅ FIX: Cho phép ProductDescription null/empty
                if (string.IsNullOrWhiteSpace(product.ProductDescription))
                {
                    product.ProductDescription = string.Empty;
                }

                db.Entry(product).State = EntityState.Modified;

                // Xóa tất cả specifications cũ
                var oldSpecs = db.ProductSpecifications.Where(ps => ps.ProductID == product.ProductID).ToList();
                db.ProductSpecifications.RemoveRange(oldSpecs);

                // ✅ Thêm specifications mới với error handling
                if (model.SpecificationCategories != null)
                {
                    foreach (var specCat in model.SpecificationCategories)
                    {
                        if (specCat.Specifications != null)
                        {
                            var validSpecs = specCat.Specifications
                                .Where(s => !string.IsNullOrWhiteSpace(s.SpecName) || !string.IsNullOrWhiteSpace(s.SpecValue))
                                .ToList();

                            if (validSpecs.Any())
                            {
                                int categoryIdToUse = specCat.SpecCategoryID;

                                if (categoryIdToUse == 0)
                                {
                                    try
                                    {
                                        categoryIdToUse = GetOrCreateDefaultSpecCategory(product.CategoryID);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Error creating default spec category: " + ex.Message);
                                        continue;
                                    }
                                }

                                foreach (var spec in validSpecs)
                                {
                                    var productSpec = new ProductSpecification
                                    {
                                        ProductID = product.ProductID,
                                        SpecCategoryID = categoryIdToUse,
                                        SpecName = !string.IsNullOrWhiteSpace(spec.SpecName) ? spec.SpecName.Trim() : string.Empty,
                                        SpecValue = !string.IsNullOrWhiteSpace(spec.SpecValue) ? spec.SpecValue.Replace("\r\n", "\n").Trim() : string.Empty,
                                        DisplayOrder = spec.DisplayOrder
                                    };
                                    db.ProductSpecifications.Add(productSpec);
                                }
                            }
                        }
                    }
                }

                // Xóa tất cả product images cũ
                var oldImages = db.ProductImages.Where(pi => pi.ProductID == product.ProductID).ToList();
                db.ProductImages.RemoveRange(oldImages);

                // Thêm product images mới
                if (model.ProductImages != null && model.ProductImages.Any())
                {
                    int displayOrder = 1;
                    bool hasMainImage = false;

                    foreach (var img in model.ProductImages)
                    {
                        if (!string.IsNullOrWhiteSpace(img.ImageURL))
                        {
                            try
                            {
                                string imagePath = img.ImageURL;

                                // Chỉ download nếu là URL mới (không phải path cũ)
                                if (Uri.TryCreate(img.ImageURL, UriKind.Absolute, out var uri) &&
                                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                                {
                                    imagePath = DownloadImageToContent(img.ImageURL);
                                }

                                var productImage = new ProductImage
                                {
                                    ProductID = product.ProductID,
                                    ImageURL = imagePath,
                                    DisplayOrder = img.DisplayOrder.HasValue ? img.DisplayOrder.Value : displayOrder++,
                                    IsMainImage = img.IsMainImage
                                };

                                db.ProductImages.Add(productImage);

                                // Cập nhật ProductImage field với ảnh chính
                                if (img.IsMainImage && !hasMainImage)
                                {
                                    product.ProductImage = imagePath;
                                    hasMainImage = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Error processing image: " + ex.Message);
                            }
                        }
                    }

                    // Nếu không có ảnh chính, dùng ảnh đầu tiên
                    if (!hasMainImage)
                    {
                        var firstImage = db.ProductImages
                            .Where(pi => pi.ProductID == product.ProductID)
                            .OrderBy(pi => pi.DisplayOrder)
                            .FirstOrDefault();

                        if (firstImage != null)
                        {
                            product.ProductImage = firstImage.ImageURL;
                        }
                    }
                }

                try
                {
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error saving changes: " + ex.Message);
                    TempData["ErrorMessage"] = "Lỗi khi lưu: " + ex.Message;
                }

                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", model.Product.CategoryID);

            // Reload specification categories if validation fails
            if (model.SpecificationCategories == null || !model.SpecificationCategories.Any())
            {
                model.SpecificationCategories = new List<SpecificationCategoryInputVM>();
                var specCategories = db.SpecificationCategories
                    .OrderBy(sc => sc.DisplayOrder.HasValue ? sc.DisplayOrder.Value : int.MaxValue)
                    .ThenBy(sc => sc.SpecCategoryName)
                    .ToList();

                foreach (var specCat in specCategories)
                {
                    model.SpecificationCategories.Add(new SpecificationCategoryInputVM
                    {
                        SpecCategoryID = specCat.SpecCategoryID,
                        SpecCategoryName = specCat.SpecCategoryName,
                        SpecCategoryIcon = specCat.SpecCategoryIcon,
                        DisplayOrder = specCat.DisplayOrder,
                        Specifications = new List<SpecificationInputVM>()
                    });
                }
            }

            return View(model);
        }

        // GET: Admin/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);

            // Kiểm tra có OrderDetail nào đang dùng product này không
            if (product.OrderDetails != null && product.OrderDetails.Any())
            {
                TempData["ErrorMessage"] = string.Format("Không thể xóa! Sản phẩm '{0}' đang có trong {1} đơn hàng.", product.ProductName, product.OrderDetails.Count);
                return RedirectToAction("Index");
            }

            db.Products.Remove(product);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult GetSpecificationCategoriesByCategory(int categoryId)
        {
            try
            {
                var specCategories = db.SpecificationCategories
                    .Where(sc => sc.CategoryID == categoryId)
                    .OrderBy(sc => sc.DisplayOrder.HasValue ? sc.DisplayOrder.Value : int.MaxValue)
                    .ThenBy(sc => sc.SpecCategoryName)
                    .Select(sc => new
                    {
                        specCategoryID = sc.SpecCategoryID,
                        specCategoryName = sc.SpecCategoryName,
                        specCategoryIcon = sc.SpecCategoryIcon,
                        displayOrder = sc.DisplayOrder,
                        templateSpecs = sc.ProductSpecifications
                            .Where(ps => ps.ProductID == 0)
                            .OrderBy(ps => ps.DisplayOrder.HasValue ? ps.DisplayOrder.Value : int.MaxValue)
                            .ThenBy(ps => ps.SpecName)
                            .Select(ps => new
                            {
                                specName = ps.SpecName,
                                displayOrder = ps.DisplayOrder
                            })
                            .ToList()
                    })
                    .ToList();

                return Json(new { success = true, data = specCategories }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ✅ Cải thiện hàm GetOrCreateDefaultSpecCategory với error handling
        private int GetOrCreateDefaultSpecCategory(int categoryId)
        {
            try
            {
                var defaultSpecCat = db.SpecificationCategories
                    .FirstOrDefault(sc => sc.CategoryID == categoryId && sc.SpecCategoryName == "Thông số kỹ thuật");

                if (defaultSpecCat == null)
                {
                    defaultSpecCat = new SpecificationCategory
                    {
                        CategoryID = categoryId,
                        SpecCategoryName = "Thông số kỹ thuật",
                        SpecCategoryIcon = "fas fa-info-circle",
                        DisplayOrder = 1
                    };
                    db.SpecificationCategories.Add(defaultSpecCat);
                    db.SaveChanges();
                }

                return defaultSpecCat.SpecCategoryID;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetOrCreateDefaultSpecCategory: " + ex.Message);
                throw new Exception("Không thể tạo danh mục thông số mặc định: " + ex.Message, ex);
            }
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