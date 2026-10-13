using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Web_perfume.Models;

namespace Web_perfume.Controllers
{
    public class BrandsController : Controller
    {
        private PerfumeShopDBEntities db = new PerfumeShopDBEntities();

        // GET: /brands
        public ActionResult Index()
        {
            var brands = db.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandName)
                .ToList();

            return View(brands);
        }

        // GET: /brands/detail/{id}
        public ActionResult Detail(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            var brand = db.Brands.FirstOrDefault(b => b.BrandID == id.Value && b.IsActive);
            if (brand == null)
            {
                return HttpNotFound();
            }

            // Products of this Brand
            var productsDb = db.Products
                .Where(p => p.BrandID == brand.BrandID && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            Func<Product, ProductItemViewModel> mapToItem = p =>
            {
                var defaultVariant = p.ProductVariants.FirstOrDefault(v => v.IsActive) ?? p.ProductVariants.FirstOrDefault();
                var primaryImg = p.ProductImages.FirstOrDefault(img => img.IsPrimary)?.ImageURL ?? p.ProductImages.FirstOrDefault()?.ImageURL;
                var activeVariants = p.ProductVariants.Where(v => v.IsActive).ToList();

                return new ProductItemViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    BrandID = p.BrandID,
                    BrandName = brand.BrandName,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "",
                    Gender = p.Gender,
                    Concentration = p.Concentration,
                    PrimaryImageUrl = primaryImg,
                    DefaultVariantID = defaultVariant != null ? defaultVariant.VariantID : 0,
                    DefaultVolumeML = defaultVariant != null ? defaultVariant.VolumeML : 0,
                    TotalStock = activeVariants.Sum(v => v.StockQuantity),
                    MinPrice = activeVariants.Any() ? activeVariants.Min(v => v.Price) : 0,
                    MinSalePrice = activeVariants.Where(v => v.SalePrice.HasValue && v.SalePrice.Value > 0).Select(v => v.SalePrice).DefaultIfEmpty(null).Min(),
                    AverageRating = 5.0,
                    ReviewCount = 0,
                    IsFeatured = p.IsFeatured
                };
            };

            ViewBag.Brand = brand;
            var productList = productsDb.Select(mapToItem).ToList();

            return View(productList);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

