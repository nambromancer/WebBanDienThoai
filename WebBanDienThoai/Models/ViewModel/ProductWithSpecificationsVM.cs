using PagedList.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebBanDienThoai.Models;

namespace WebBanDienThoai.Models.ViewModel
{
    public class ProductWithSpecificationsVM
    {
        public Product Product { get; set; }
        public List<SpecificationCategoryInputVM> SpecificationCategories { get; set; }
        public List<ProductImageInputVM> ProductImages { get; set; }

        public ProductWithSpecificationsVM()
        {
            Product = new Product();
            SpecificationCategories = new List<SpecificationCategoryInputVM>();
            ProductImages = new List<ProductImageInputVM>();
        }
    }

    public class SpecificationCategoryInputVM
    {
        public int SpecCategoryID { get; set; }
        public string SpecCategoryName { get; set; }
        public string SpecCategoryIcon { get; set; }
        public int? DisplayOrder { get; set; }
        public List<SpecificationInputVM> Specifications { get; set; }

        public SpecificationCategoryInputVM()
        {
            Specifications = new List<SpecificationInputVM>();
        }
    }

    public class SpecificationInputVM
    {
        public int? SpecID { get; set; }
        public string SpecName { get; set; }
        public string SpecValue { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class ProductImageInputVM
    {
        public int? ImageID { get; set; }
        public string ImageURL { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsMainImage { get; set; }
    }
}