using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDienThoai.Models.ViewModel
{
    public class SearchProductVM
    {
        public string SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? CategoryFilter { get; set; }
        public string SortOrder { get; set; }
        public IPagedList<Product> Products { get; set; }
    }
}