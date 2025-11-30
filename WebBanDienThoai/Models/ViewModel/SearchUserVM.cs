using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDienThoai.Models.ViewModel
{
    public class SearchUserVM
    {
        public string SearchTerm { get; set; }
        public string RoleFilter { get; set; }
        public string SortOrder { get; set; }
        public IPagedList<User> Users { get; set; }
    }
}