using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanDienThoai.Models.ViewModel
{
    public class SpecificationCategoryWithNamesVM
    {
        public SpecificationCategory SpecificationCategory { get; set; }

        // Danh sách tên thông số (SpecName) để tạo trước
        public List<string> SpecNames { get; set; }

        public SpecificationCategoryWithNamesVM()
        {
            SpecificationCategory = new SpecificationCategory();
            SpecNames = new List<string>();
        }
    }
}