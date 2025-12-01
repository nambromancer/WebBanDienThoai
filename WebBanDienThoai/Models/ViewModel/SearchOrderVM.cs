using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebBanDienThoai.Models.ViewModel
{
    public class SearchOrderVM
    {
        [Display(Name = "Mã đơn hàng")]
        public int? OrderID { get; set; }

        [Display(Name = "Tên khách hàng")]
        [StringLength(100)]
        public string CustomerName { get; set; }

        [Display(Name = "Số điện thoại")]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [Display(Name = "Trạng thái đơn hàng")]
        public string OrderStatus { get; set; }

        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }
    }
}