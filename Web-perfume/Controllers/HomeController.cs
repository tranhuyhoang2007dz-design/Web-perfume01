using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Web_perfume.Models;

namespace Web_perfume.Controllers
{
    public class HomeController : Controller
    {
        private PerfumeShopDBEntities db = new PerfumeShopDBEntities();

        public ActionResult Index()
        {
            var model = new HomeViewModel();

            try
            {
                // 1. Categories
                model.Categories = db.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryID)
                    .ToList();

                // 2. Featured Brands (active brands with products)
                model.FeaturedBrands = db.Brands
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.BrandName)
                    .Take(8)
                    .ToList();

                // Helper to map DB product to ProductItemViewModel
                Func<Product, ProductItemViewModel> mapToItem = p =>
                {
                    var defaultVariant = p.ProductVariants.FirstOrDefault(v => v.IsActive) 
                                         ?? p.ProductVariants.FirstOrDefault();
                    var primaryImg = p.ProductImages.FirstOrDefault(img => img.IsPrimary)?.ImageURL 
                                     ?? p.ProductImages.FirstOrDefault()?.ImageURL;

                    var activeVariants = p.ProductVariants.Where(v => v.IsActive).ToList();
                    decimal minPrice = activeVariants.Any() ? activeVariants.Min(v => v.Price) : 0;
                    decimal? minSalePrice = activeVariants.Where(v => v.SalePrice.HasValue && v.SalePrice.Value > 0)
                                                          .Select(v => v.SalePrice)
                                                          .DefaultIfEmpty(null)
                                                          .Min();

                    var reviews = p.ProductReviews.Where(r => r.IsApproved).ToList();
                    double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 5.0;

                    return new ProductItemViewModel
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Slug = p.Slug,
                        BrandID = p.BrandID,
                        BrandName = p.Brand != null ? p.Brand.BrandName : "",
                        CategoryName = p.Category != null ? p.Category.CategoryName : "",
                        Gender = p.Gender,
                        Concentration = p.Concentration,
                        PrimaryImageUrl = primaryImg,
                        DefaultVariantID = defaultVariant != null ? defaultVariant.VariantID : 0,
                        DefaultVolumeML = defaultVariant != null ? defaultVariant.VolumeML : 0,
                        TotalStock = activeVariants.Sum(v => v.StockQuantity),
                        MinPrice = minPrice,
                        MinSalePrice = minSalePrice,
                        AverageRating = avgRating,
                        ReviewCount = reviews.Count,
                        IsFeatured = p.IsFeatured
                    };
                };

                // 3. Featured Products
                var featuredQuery = db.Products
                    .Where(p => p.IsActive && p.IsFeatured)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToList();

                if (!featuredQuery.Any())
                {
                    featuredQuery = db.Products
                        .Where(p => p.IsActive)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(8)
                        .ToList();
                }
                model.FeaturedProducts = featuredQuery.Select(mapToItem).ToList();

                // 4. New Arrivals
                model.NewArrivals = db.Products
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToList()
                    .Select(mapToItem)
                    .ToList();

                // 5. Best Sellers (Take top products with variants)
                model.BestSellers = db.Products
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.ProductID)
                    .Take(4)
                    .ToList()
                    .Select(mapToItem)
                    .ToList();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                model.Categories = new List<Category>();
                model.FeaturedBrands = new List<Brand>();
                model.FeaturedProducts = new List<ProductItemViewModel>();
                model.NewArrivals = new List<ProductItemViewModel>();
                model.BestSellers = new List<ProductItemViewModel>();
            }

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Title = "Về Chúng Tôi";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Liên Hệ";
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
