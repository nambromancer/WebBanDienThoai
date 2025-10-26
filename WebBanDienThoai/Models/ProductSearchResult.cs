using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDienThoai.Models
{
    public class ProductSearchResult
    {
        public string Name { get; set; }
        public string Price { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryUrl { get; set; }
        public string DetailUrl { get; set; }
    }
}