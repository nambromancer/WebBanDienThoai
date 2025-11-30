using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;
using PagedList;

namespace WebBanDienThoai.Areas.Admin.Controllers
{
    public class SpecNameWithOrderDTO
    {
        public string SpecName { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class SpecificationCategoriesController : BaseAdminController
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        // GET: Admin/SpecificationCategories
        public ActionResult Index(string search, int? categoryFilter, int? page)
        {
            var specCategories = db.SpecificationCategories.Include(s => s.Category).AsQueryable();

            // Lọc theo danh mục sản phẩm
            if (categoryFilter.HasValue)
            {
                specCategories = specCategories.Where(sc => sc.CategoryID == categoryFilter.Value);
                ViewBag.CategoryFilter = categoryFilter.Value;
            }

            // Tìm kiếm theo tên
            if (!string.IsNullOrWhiteSpace(search))
            {
                specCategories = specCategories.Where(sc => sc.SpecCategoryName.Contains(search) || sc.Category.CategoryName.Contains(search));
                ViewBag.SearchQuery = search;
            }

            // Tạo SelectList cho dropdown lọc danh mục
            ViewBag.Categories = new SelectList(db.Categories, "CategoryID", "CategoryName");

            // Sắp xếp
            var sortedCategories = specCategories
                .OrderBy(sc => sc.Category.CategoryName)
                .ThenBy(sc => sc.DisplayOrder ?? int.MaxValue)
                .ThenBy(sc => sc.SpecCategoryName);

            // Phân trang
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(sortedCategories.ToPagedList(pageNumber, pageSize));
        }

        // Giữ nguyên các action method khác...
        // GET: Admin/SpecificationCategories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SpecificationCategory specificationCategory = db.SpecificationCategories
                .Include(s => s.Category)
                .Include(s => s.ProductSpecifications)
                .FirstOrDefault(s => s.SpecCategoryID == id);

            if (specificationCategory == null)
            {
                return HttpNotFound();
            }
            return View(specificationCategory);
        }

        // GET: Admin/SpecificationCategories/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            return View(new SpecificationCategoryWithNamesVM());
        }

        // POST: Admin/SpecificationCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SpecificationCategoryWithNamesVM viewModel, string specNamesJson)
        {
            if (ModelState.IsValid)
            {
                var specCategory = viewModel.SpecificationCategory;

                if (db.SpecificationCategories.Any(sc =>
                    sc.CategoryID == specCategory.CategoryID &&
                    sc.SpecCategoryName.Equals(specCategory.SpecCategoryName, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("SpecificationCategory.SpecCategoryName", "Tên danh mục thông số đã tồn tại trong danh mục sản phẩm này!");
                    ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", specCategory.CategoryID);
                    return View(viewModel);
                }

                db.SpecificationCategories.Add(specCategory);
                db.SaveChanges();

                if (!string.IsNullOrWhiteSpace(specNamesJson))
                {
                    try
                    {
                        var specNamesWithOrder = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SpecNameWithOrderDTO>>(specNamesJson);

                        if (specNamesWithOrder != null && specNamesWithOrder.Any())
                        {
                            int addedCount = 0;
                            foreach (var item in specNamesWithOrder.Where(s => !string.IsNullOrWhiteSpace(s.SpecName)))
                            {
                                var productSpec = new ProductSpecification
                                {
                                    ProductID = 0,
                                    SpecCategoryID = specCategory.SpecCategoryID,
                                    SpecName = item.SpecName.Trim(),
                                    SpecValue = string.Empty,
                                    DisplayOrder = item.DisplayOrder
                                };
                                db.ProductSpecifications.Add(productSpec);
                                addedCount++;
                            }
                            db.SaveChanges();
                            TempData["SuccessMessage"] = $"Thêm danh mục thông số thành công! Đã tạo {addedCount} tên thông số mẫu.";
                        }
                    }
                    catch (Newtonsoft.Json.JsonException ex)
                    {
                        TempData["ErrorMessage"] = "Có lỗi khi lưu tên thông số: " + ex.Message;
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "Thêm danh mục thông số thành công!";
                }

                return RedirectToAction("Index");
            }

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", viewModel.SpecificationCategory.CategoryID);
            return View(viewModel);
        }

        // GET: Admin/SpecificationCategories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SpecificationCategory specificationCategory = db.SpecificationCategories.Find(id);
            if (specificationCategory == null)
            {
                return HttpNotFound();
            }

            var viewModel = new SpecificationCategoryWithNamesVM
            {
                SpecificationCategory = specificationCategory,
                SpecNames = db.ProductSpecifications
                    .Where(ps => ps.SpecCategoryID == id && ps.ProductID == 0)
                    .OrderBy(ps => ps.DisplayOrder ?? int.MaxValue)
                    .ThenBy(ps => ps.SpecName)
                    .Select(ps => ps.SpecName)
                    .ToList()
            };

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", specificationCategory.CategoryID);
            return View(viewModel);
        }

        // POST: Admin/SpecificationCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SpecificationCategoryWithNamesVM viewModel, string specNamesJson)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var specCategory = viewModel.SpecificationCategory;

                    if (db.SpecificationCategories.Any(sc =>
                        sc.SpecCategoryID != specCategory.SpecCategoryID &&
                        sc.CategoryID == specCategory.CategoryID &&
                        sc.SpecCategoryName.Equals(specCategory.SpecCategoryName, StringComparison.OrdinalIgnoreCase)))
                    {
                        ModelState.AddModelError("SpecificationCategory.SpecCategoryName", "Tên danh mục thông số đã tồn tại trong danh mục sản phẩm này!");
                        ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", specCategory.CategoryID);
                        viewModel.SpecNames = db.ProductSpecifications
                            .Where(ps => ps.SpecCategoryID == specCategory.SpecCategoryID && ps.ProductID == 0)
                            .OrderBy(ps => ps.DisplayOrder ?? int.MaxValue)
                            .ThenBy(ps => ps.SpecName)
                            .Select(ps => ps.SpecName)
                            .ToList();
                        return View(viewModel);
                    }

                    db.Entry(specCategory).State = EntityState.Modified;
                    db.SaveChanges();

                    if (!string.IsNullOrWhiteSpace(specNamesJson))
                    {
                        try
                        {
                            var specNamesWithOrder = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SpecNameWithOrderDTO>>(specNamesJson);

                            if (specNamesWithOrder != null && specNamesWithOrder.Any())
                            {
                                var validItems = specNamesWithOrder.Where(s => !string.IsNullOrWhiteSpace(s.SpecName)).ToList();

                                if (validItems.Any())
                                {
                                    var oldTemplates = db.ProductSpecifications
                                        .Where(ps => ps.SpecCategoryID == specCategory.SpecCategoryID && ps.ProductID == 0)
                                        .ToList();

                                    if (oldTemplates.Any())
                                    {
                                        db.ProductSpecifications.RemoveRange(oldTemplates);
                                        db.SaveChanges();
                                    }

                                    int addedCount = 0;
                                    foreach (var item in validItems)
                                    {
                                        var productSpec = new ProductSpecification
                                        {
                                            ProductID = 0,
                                            SpecCategoryID = specCategory.SpecCategoryID,
                                            SpecName = item.SpecName.Trim(),
                                            SpecValue = string.Empty,
                                            DisplayOrder = item.DisplayOrder
                                        };
                                        db.ProductSpecifications.Add(productSpec);
                                        addedCount++;
                                    }

                                    db.SaveChanges();
                                    TempData["SuccessMessage"] = $"Cập nhật danh mục thông số thành công! Đã lưu {addedCount} tên thông số.";
                                }
                                else
                                {
                                    TempData["SuccessMessage"] = "Cập nhật danh mục thông số thành công!";
                                }
                            }
                        }
                        catch (Newtonsoft.Json.JsonException)
                        {
                            TempData["ErrorMessage"] = "Có lỗi khi parse JSON spec names.";
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Cập nhật danh mục thông số thành công!";
                    }

                    return RedirectToAction("Index");
                }

                ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", viewModel.SpecificationCategory.CategoryID);
                viewModel.SpecNames = db.ProductSpecifications
                    .Where(ps => ps.SpecCategoryID == viewModel.SpecificationCategory.SpecCategoryID && ps.ProductID == 0)
                    .OrderBy(ps => ps.DisplayOrder ?? int.MaxValue)
                    .ThenBy(ps => ps.SpecName)
                    .Select(ps => ps.SpecName)
                    .ToList();

                return View(viewModel);
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                TempData["ErrorMessage"] = $"Lỗi validation: {string.Join("; ", ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors).Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))}";
                ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", viewModel.SpecificationCategory.CategoryID);
                viewModel.SpecNames = db.ProductSpecifications
                    .Where(ps => ps.SpecCategoryID == viewModel.SpecificationCategory.SpecCategoryID && ps.ProductID == 0)
                    .OrderBy(ps => ps.DisplayOrder ?? int.MaxValue)
                    .ThenBy(ps => ps.SpecName)
                    .Select(ps => ps.SpecName)
                    .ToList();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", viewModel.SpecificationCategory.CategoryID);
                viewModel.SpecNames = db.ProductSpecifications
                    .Where(ps => ps.SpecCategoryID == viewModel.SpecificationCategory.SpecCategoryID && ps.ProductID == 0)
                    .OrderBy(ps => ps.DisplayOrder ?? int.MaxValue)
                    .ThenBy(ps => ps.SpecName)
                    .Select(ps => ps.SpecName)
                    .ToList();
                return View(viewModel);
            }
        }

        // GET: Admin/SpecificationCategories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SpecificationCategory specificationCategory = db.SpecificationCategories
                .Include(s => s.Category)
                .Include(s => s.ProductSpecifications)
                .FirstOrDefault(s => s.SpecCategoryID == id);

            if (specificationCategory == null)
            {
                return HttpNotFound();
            }

            return View(specificationCategory);
        }

        // POST: Admin/SpecificationCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            SpecificationCategory specificationCategory = db.SpecificationCategories.Find(id);

            var usedSpecs = db.ProductSpecifications
                .Where(ps => ps.SpecCategoryID == id && ps.ProductID != 0)
                .ToList();

            if (usedSpecs.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa! Danh mục '{specificationCategory.SpecCategoryName}' đang có {usedSpecs.Count} thông số được sử dụng.";
                return RedirectToAction("Index");
            }

            var templates = db.ProductSpecifications
                .Where(ps => ps.SpecCategoryID == id && ps.ProductID == 0)
                .ToList();
            db.ProductSpecifications.RemoveRange(templates);

            db.SpecificationCategories.Remove(specificationCategory);
            db.SaveChanges();
            TempData["SuccessMessage"] = "Xóa danh mục thông số thành công!";
            return RedirectToAction("Index");
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