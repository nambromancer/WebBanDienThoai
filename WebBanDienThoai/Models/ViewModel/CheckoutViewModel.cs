using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.Models.ViewModel
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public CustomerInfoViewModel CustomerInfo { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DeliveryAddress { get; set; }

        public bool UseCustomerAddress { get; set; }
        public bool SaveAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức giao hàng")]
        public string DeliveryMethod { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        public decimal TotalAmount { get; set; }
        public bool IsBuyNow { get; set; }
    }

    public class CustomerInfoViewModel
    {
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }
}