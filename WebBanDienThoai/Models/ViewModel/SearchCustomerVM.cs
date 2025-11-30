using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDienThoai.Models.ViewModel
{
    public class SearchCustomerVM
    {
        public string SearchTerm { get; set; }
        public string GenderFilter { get; set; }
        public string SortOrder { get; set; }
        public IPagedList<Customer> Customers { get; set; }
    }
}