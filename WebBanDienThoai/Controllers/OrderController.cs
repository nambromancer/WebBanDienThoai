using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDienThoai.Models;
using WebBanDienThoai.Models.ViewModel;

namespace WebBanDienThoai.Controllers
{
    public class OrderController : Controller
    {
        private WebBanDienThoaiDBEntities db = new WebBanDienThoaiDBEntities();

        public ActionResult Checkout()
        {
            if (Session["UserPhone"] == null)
            {
                TempData["LoginRequired"] = "Vui lòng đăng nhập để tiếp tục đặt hàng.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Order") });
            }

            var cart = GetCart();
            if (cart == null || !cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Cart", "Home");
            }

            var userPhone = Session["UserPhone"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == userPhone);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Cart", "Home");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                CustomerInfo = new CustomerInfoViewModel
                {
                    CustomerID = customer.CustomerID,
                    CustomerName = customer.CustomerName,
                    PhoneNumber = customer.PhoneNumber,
                    Email = customer.CustomerEmail,
                    Address = customer.CustomerAddress
                },
                DeliveryMethod = "Giao hàng COD",
                PaymentMethod = "COD",
                TotalAmount = cart.Sum(c => c.TotalPrice)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            if (Session["UserPhone"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục." });
            }

            var cart = GetCart();
            if (cart == null || !cart.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống." });
            }

            var userPhone = Session["UserPhone"].ToString();
            var customer = db.Customers.FirstOrDefault(c => c.PhoneNumber == userPhone);

            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng." });
            }

            try
            {
                // Lưu địa chỉ vào thông tin khách hàng nếu chọn
                if (string.IsNullOrEmpty(customer.CustomerAddress) && model.SaveAddress)
                {
                    customer.CustomerAddress = model.DeliveryAddress;
                    db.SaveChanges();
                }

                // Tính tổng tiền với phí vận chuyển
                decimal subtotal = cart.Sum(c => c.TotalPrice);
                decimal shippingFee = 0;

                // Nếu chọn "Giao hàng nhanh" thì cộng 30.000đ
                if (model.DeliveryMethod == "Giao hàng nhanh")
                {
                    shippingFee = 30000;
                }

                decimal totalAmount = subtotal + shippingFee;

                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    OrderStatus = "Chờ xử lý",
                    PaymentStatus = "Chưa thanh toán",
                    PaymentMethod = model.PaymentMethod,
                    DeliveryMethod = model.DeliveryMethod,
                    AddressDelivery = model.DeliveryAddress
                };

                db.Orders.Add(order);
                db.SaveChanges();

                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        TotalPrice = item.TotalPrice
                    };
                    db.OrderDetails.Add(orderDetail);
                }
                db.SaveChanges();

                Session["Cart"] = null;

                return Json(new
                {
                    success = true,
                    message = "Đặt hàng thành công!",
                    orderId = order.OrderID
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public ActionResult BuyNow(int productId, int quantity = 1)
        {
            if (Session["UserPhone"] == null)
            {
                TempData["LoginRequired"] = "Vui lòng đăng nhập để mua hàng.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("BuyNow", "Order", new { productId, quantity }) });
            }

            var product = db.Products
                .Include("ProductImages")
                .FirstOrDefault(p => p.ProductID == productId);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            // ✅ THÊM SẢN PHẨM VÀO GIỎ HÀNG TRƯỚC KHI CHECKOUT
            var cart = GetCart();
            var firstImage = product.ProductImages != null && product.ProductImages.Any()
                ? product.ProductImages.OrderBy(i => i.DisplayOrder.HasValue ? i.DisplayOrder.Value : int.MaxValue).First().ImageURL
                : product.ProductImage;

            // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
            var existingItem = cart.FirstOrDefault(c => c.ProductID == productId);
            if (existingItem != null)
            {
                // Nếu đã có, tăng số lượng
                existingItem.Quantity += quantity;
            }
            else
            {
                // Nếu chưa có, thêm mới
                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    ProductImage = firstImage,
                    Price = product.ProductPrice,
                    Quantity = quantity
                });
            }

            // Lưu giỏ hàng vào Session
            SaveCart(cart);

            // Chuyển đến trang Checkout (giỏ hàng đã có sản phẩm)
            return RedirectToAction("Checkout");
        }

        public ActionResult OrderConfirmed(int orderId)
        {
            var order = db.Orders
                .Include("OrderDetails")
                .Include("OrderDetails.Product")
                .Include("Customer")
                .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
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
}