using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;

namespace WebBanDienThoai.Areas.Admin.Controllers
{
    public class OrdersController : BaseAdminController
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        // GET: Admin/Orders
        public ActionResult Index(SearchOrderVM searchModel)
        {
            var orders = db.Orders
                .Include("Customer")
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .AsQueryable();

            // Tìm kiếm theo OrderID
            if (searchModel.OrderID.HasValue)
            {
                orders = orders.Where(o => o.OrderID == searchModel.OrderID.Value);
            }

            // Tìm kiếm theo tên khách hàng
            if (!string.IsNullOrWhiteSpace(searchModel.CustomerName))
            {
                orders = orders.Where(o => o.Customer.CustomerName.Contains(searchModel.CustomerName));
            }

            // Tìm kiếm theo số điện thoại
            if (!string.IsNullOrWhiteSpace(searchModel.PhoneNumber))
            {
                orders = orders.Where(o => o.Customer.PhoneNumber.Contains(searchModel.PhoneNumber));
            }

            // Lọc theo trạng thái đơn hàng
            if (!string.IsNullOrWhiteSpace(searchModel.OrderStatus))
            {
                orders = orders.Where(o => o.OrderStatus == searchModel.OrderStatus);
            }

            // Lọc theo khoảng thời gian
            if (searchModel.FromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= searchModel.FromDate.Value);
            }

            if (searchModel.ToDate.HasValue)
            {
                var toDate = searchModel.ToDate.Value.AddDays(1);
                orders = orders.Where(o => o.OrderDate < toDate);
            }

            // Sắp xếp mặc định: mới nhất trước
            var result = orders.OrderByDescending(o => o.OrderDate).ToList();

            ViewBag.SearchModel = searchModel;

            // Truyền danh sách trạng thái cho dropdown
            ViewBag.OrderStatuses = new SelectList(new[]
            {
                "Chờ xử lý",
                "Đang xử lý",
                "Đang giao hàng",
                "Đã giao hàng",
                "Đã hủy"
            });

            return View(result);
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Orders
                .Include("Customer")
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .Include("OrderDetails.Product.ProductImages")
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: Admin/Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Orders
                .Include("Customer")
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // ✅ KIỂM TRA: Chỉ không cho chỉnh sửa đơn hàng "Đã hủy" và "Đã giao hàng"
            if (order.OrderStatus == "Đã hủy")
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa đơn hàng đã hủy!";
                return RedirectToAction("Details", new { id = order.OrderID });
            }

            if (order.OrderStatus == "Đã giao hàng")
            {
                TempData["ErrorMessage"] = "Không thể chỉnh sửa đơn hàng đã giao!";
                return RedirectToAction("Details", new { id = order.OrderID });
            }

            // ✅ Xây dựng danh sách trạng thái có thể chuyển
            var availableStatuses = new List<string> { order.OrderStatus };

            if (order.OrderStatus == "Chờ xử lý")
            {
                availableStatuses.Add("Đang xử lý");
                availableStatuses.Add("Đã hủy");
            }
            else if (order.OrderStatus == "Đang xử lý")
            {
                availableStatuses.Add("Đang giao hàng");
                availableStatuses.Add("Đã hủy");
            }
            else if (order.OrderStatus == "Đang giao hàng")
            {
                availableStatuses.Add("Đã giao hàng");
                availableStatuses.Add("Đã hủy");
            }

            ViewBag.OrderStatuses = new SelectList(availableStatuses.Distinct(), order.OrderStatus);

            // Danh sách trạng thái thanh toán (luôn cho phép chỉnh sửa với tất cả trạng thái trừ Đã hủy)
            ViewBag.PaymentStatuses = new SelectList(new[]
            {
                "Chưa thanh toán",
                "Đã thanh toán",
                "Hoàn tiền"
            }, order.PaymentStatus);

            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = db.Orders.Find(order.OrderID);
                    if (existingOrder == null)
                    {
                        return HttpNotFound();
                    }

                    // ✅ KIỂM TRA: Không cho phép chỉnh sửa đơn hàng đã hủy hoặc đã giao
                    if (existingOrder.OrderStatus == "Đã hủy")
                    {
                        TempData["ErrorMessage"] = "Không thể chỉnh sửa đơn hàng đã hủy!";
                        return RedirectToAction("Details", new { id = order.OrderID });
                    }

                    if (existingOrder.OrderStatus == "Đã giao hàng")
                    {
                        TempData["ErrorMessage"] = "Không thể chỉnh sửa đơn hàng đã giao!";
                        return RedirectToAction("Details", new { id = order.OrderID });
                    }

                    // ✅ VALIDATE: Kiểm tra chuyển trạng thái hợp lệ
                    if (existingOrder.OrderStatus == "Chờ xử lý")
                    {
                        if (order.OrderStatus != "Chờ xử lý" &&
                            order.OrderStatus != "Đang xử lý" &&
                            order.OrderStatus != "Đã hủy")
                        {
                            TempData["ErrorMessage"] = "Trạng thái không hợp lệ!";
                            return RedirectToAction("Edit", new { id = order.OrderID });
                        }
                    }
                    else if (existingOrder.OrderStatus == "Đang xử lý")
                    {
                        if (order.OrderStatus != "Đang xử lý" &&
                            order.OrderStatus != "Đang giao hàng" &&
                            order.OrderStatus != "Đã hủy")
                        {
                            TempData["ErrorMessage"] = "Trạng thái không hợp lệ!";
                            return RedirectToAction("Edit", new { id = order.OrderID });
                        }
                    }
                    else if (existingOrder.OrderStatus == "Đang giao hàng")
                    {
                        if (order.OrderStatus != "Đang giao hàng" &&
                            order.OrderStatus != "Đã giao hàng" &&
                            order.OrderStatus != "Đã hủy")
                        {
                            TempData["ErrorMessage"] = "Trạng thái không hợp lệ!";
                            return RedirectToAction("Edit", new { id = order.OrderID });
                        }
                    }

                    // Cập nhật các trường được phép chỉnh sửa
                    existingOrder.OrderStatus = order.OrderStatus;
                    existingOrder.PaymentStatus = order.PaymentStatus;
                    existingOrder.DeliveryMethod = order.DeliveryMethod;
                    existingOrder.PaymentMethod = order.PaymentMethod;
                    existingOrder.AddressDelivery = order.AddressDelivery;

                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật đơn hàng thành công!";
                    return RedirectToAction("Details", new { id = order.OrderID });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            // Reload data if validation fails
            var orderData = db.Orders
                .Include("Customer")
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.OrderID == order.OrderID);

            var availableStatuses = new List<string> { orderData.OrderStatus };

            if (orderData.OrderStatus == "Chờ xử lý")
            {
                availableStatuses.Add("Đang xử lý");
                availableStatuses.Add("Đã hủy");
            }
            else if (orderData.OrderStatus == "Đang xử lý")
            {
                availableStatuses.Add("Đang giao hàng");
                availableStatuses.Add("Đã hủy");
            }
            else if (orderData.OrderStatus == "Đang giao hàng")
            {
                availableStatuses.Add("Đã giao hàng");
                availableStatuses.Add("Đã hủy");
            }

            ViewBag.OrderStatuses = new SelectList(availableStatuses.Distinct(), order.OrderStatus);

            ViewBag.PaymentStatuses = new SelectList(new[]
            {
                "Chưa thanh toán",
                "Đã thanh toán",
                "Hoàn tiền"
            }, order.PaymentStatus);

            return View(orderData);
        }

        // GET: Admin/Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.Orders
                .Include("Customer")
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // ✅ KIỂM TRA: Chỉ cho phép xóa đơn hàng đã hủy
            if (order.OrderStatus != "Đã hủy")
            {
                TempData["ErrorMessage"] = "Chỉ có thể xóa đơn hàng đã hủy!";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var order = db.Orders
                    .Include("OrderDetails")
                    .FirstOrDefault(o => o.OrderID == id);

                if (order == null)
                {
                    return HttpNotFound();
                }

                // ✅ KIỂM TRA: Chỉ cho phép xóa đơn hàng đã hủy
                if (order.OrderStatus != "Đã hủy")
                {
                    TempData["ErrorMessage"] = "Chỉ có thể xóa đơn hàng đã hủy!";
                    return RedirectToAction("Index");
                }

                // Xóa OrderDetails trước
                if (order.OrderDetails != null)
                {
                    db.OrderDetails.RemoveRange(order.OrderDetails);
                }

                // Xóa Order
                db.Orders.Remove(order);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Xóa đơn hàng thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        public JsonResult UpdateStatus(int orderId, string status)
        {
            try
            {
                var order = db.Orders.Find(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // ✅ KIỂM TRA: Không cho phép thay đổi trạng thái của đơn đã hủy, đã giao
                if (order.OrderStatus == "Đã hủy")
                {
                    return Json(new { success = false, message = "Không thể thay đổi trạng thái đơn hàng đã hủy" });
                }

                if (order.OrderStatus == "Đã giao hàng")
                {
                    return Json(new { success = false, message = "Không thể thay đổi trạng thái đơn hàng đã giao" });
                }

                // Validate status transitions
                var validStatuses = new[] { "Chờ xử lý", "Đang xử lý", "Đang giao hàng", "Đã giao hàng", "Đã hủy" };
                if (!validStatuses.Contains(status))
                {
                    return Json(new { success = false, message = "Trạng thái không hợp lệ" });
                }

                // ✅ VALIDATE: Kiểm tra chuyển trạng thái hợp lệ
                if (order.OrderStatus == "Chờ xử lý")
                {
                    if (status != "Đang xử lý" && status != "Đã hủy")
                    {
                        return Json(new { success = false, message = "Chỉ có thể chuyển sang 'Đang xử lý' hoặc 'Đã hủy'" });
                    }
                }
                else if (order.OrderStatus == "Đang xử lý")
                {
                    if (status != "Đang giao hàng" && status != "Đã hủy")
                    {
                        return Json(new { success = false, message = "Chỉ có thể chuyển sang 'Đang giao hàng' hoặc 'Đã hủy'" });
                    }
                }
                else if (order.OrderStatus == "Đang giao hàng")
                {
                    if (status != "Đã giao hàng" && status != "Đã hủy")
                    {
                        return Json(new { success = false, message = "Chỉ có thể chuyển sang 'Đã giao hàng' hoặc 'Đã hủy'" });
                    }
                }

                order.OrderStatus = status;

                // Tự động cập nhật PaymentStatus khi giao hàng thành công
                if (status == "Đã giao hàng" && order.PaymentMethod == "COD")
                {
                    order.PaymentStatus = "Đã thanh toán";
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: Admin/Orders/Statistics
        public ActionResult Statistics()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisYear = new DateTime(today.Year, 1, 1);

            // ✅ Đảm bảo không có giá trị null bằng cách ép kiểu rõ ràng
            ViewBag.TodayOrders = db.Orders.Count(o => DbFunctions.TruncateTime(o.OrderDate) == today);
            ViewBag.TodayRevenue = (decimal)(db.Orders
                .Where(o => DbFunctions.TruncateTime(o.OrderDate) == today && o.OrderStatus == "Đã giao hàng")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0);

            ViewBag.MonthOrders = db.Orders.Count(o => o.OrderDate >= thisMonth);
            ViewBag.MonthRevenue = (decimal)(db.Orders
                .Where(o => o.OrderDate >= thisMonth && o.OrderStatus == "Đã giao hàng")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0);

            ViewBag.YearOrders = db.Orders.Count(o => o.OrderDate >= thisYear);
            ViewBag.YearRevenue = (decimal)(db.Orders
                .Where(o => o.OrderDate >= thisYear && o.OrderStatus == "Đã giao hàng")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0);

            ViewBag.PendingOrders = db.Orders.Count(o => o.OrderStatus == "Chờ xử lý");
            ViewBag.ProcessingOrders = db.Orders.Count(o => o.OrderStatus == "Đang xử lý");
            ViewBag.ShippingOrders = db.Orders.Count(o => o.OrderStatus == "Đang giao hàng");
            ViewBag.CompletedOrders = db.Orders.Count(o => o.OrderStatus == "Đã giao hàng");
            ViewBag.CancelledOrders = db.Orders.Count(o => o.OrderStatus == "Đã hủy");

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
}