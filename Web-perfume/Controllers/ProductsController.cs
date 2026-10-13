using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Web_perfume.Models;

namespace Web_perfume.Controllers
{
    public class ProductsController : Controller
    {
        private PerfumeShopDBEntities db = new PerfumeShopDBEntities();

        // ==========================================
        // 1. PRODUCT LISTING & FILTERING
        // ==========================================
        public ActionResult Index(
            string keyword, 
            int? brandId, 
            int? categoryId, 
            string gender, 
            string concentration, 
            decimal? minPrice, 
            decimal? maxPrice, 
            string sort, 
            int page = 1)
        {
            int pageSize = 9;
            var query = db.Products.Where(p => p.IsActive);

            // 1. Search Keyword (Name or Brand Name)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p => p.ProductName.Contains(keyword) || 
                                         p.Brand.BrandName.Contains(keyword) ||
                                         p.Description.Contains(keyword));
            }

            // 2. Filter by Brand
            if (brandId.HasValue && brandId.Value > 0)
            {
                query = query.Where(p => p.BrandID == brandId.Value);
            }

            // 3. Filter by Category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            // 4. Filter by Gender
            if (!string.IsNullOrWhiteSpace(gender))
            {
                query = query.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase));
            }

            // 5. Filter by Concentration
            if (!string.IsNullOrWhiteSpace(concentration))
            {
                query = query.Where(p => p.Concentration.Equals(concentration, StringComparison.OrdinalIgnoreCase));
            }

            // Map DB entity to unified ProductItemViewModel
            Func<Product, ProductItemViewModel> mapToItem = p =>
            {
                var defaultVariant = p.ProductVariants.FirstOrDefault(v => v.IsActive) 
                                     ?? p.ProductVariants.FirstOrDefault();
                var primaryImg = p.ProductImages.FirstOrDefault(img => img.IsPrimary)?.ImageURL 
                                 ?? p.ProductImages.FirstOrDefault()?.ImageURL;

                var activeVariants = p.ProductVariants.Where(v => v.IsActive).ToList();
                decimal pMinPrice = activeVariants.Any() ? activeVariants.Min(v => v.Price) : 0;
                decimal? pMinSalePrice = activeVariants.Where(v => v.SalePrice.HasValue && v.SalePrice.Value > 0)
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
                    MinPrice = pMinPrice,
                    MinSalePrice = pMinSalePrice,
                    AverageRating = avgRating,
                    ReviewCount = reviews.Count,
                    IsFeatured = p.IsFeatured
                };
            };

            var allItems = query.ToList().Select(mapToItem).AsQueryable();

            // 6. Filter by Price Range
            if (minPrice.HasValue)
            {
                allItems = allItems.Where(p => p.DisplayPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                allItems = allItems.Where(p => p.DisplayPrice <= maxPrice.Value);
            }

            // 7. Sort
            switch (sort)
            {
                case "price_asc":
                    allItems = allItems.OrderBy(p => p.DisplayPrice);
                    break;
                case "price_desc":
                    allItems = allItems.OrderByDescending(p => p.DisplayPrice);
                    break;
                case "name_asc":
                    allItems = allItems.OrderBy(p => p.ProductName);
                    break;
                case "sale":
                    allItems = allItems.OrderByDescending(p => p.HasDiscount).ThenByDescending(p => p.DiscountPercentage);
                    break;
                case "newest":
                default:
                    allItems = allItems.OrderByDescending(p => p.ProductID);
                    break;
            }

            int totalCount = allItems.Count();
            var paginatedItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new ProductListViewModel
            {
                Products = paginatedItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Keyword = keyword,
                BrandId = brandId,
                CategoryId = categoryId,
                Gender = gender,
                Concentration = concentration,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sort,
                AvailableBrands = db.Brands.Where(b => b.IsActive).OrderBy(b => b.BrandName).ToList(),
                AvailableCategories = db.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToList(),
                AvailableGenders = db.Products.Where(p => p.Gender != null).Select(p => p.Gender).Distinct().ToList(),
                AvailableConcentrations = db.Products.Where(p => p.Concentration != null).Select(p => p.Concentration).Distinct().ToList()
            };

            if (brandId.HasValue)
            {
                var curBrand = db.Brands.Find(brandId.Value);
                if (curBrand != null) model.CurrentBrandName = curBrand.BrandName;
            }
            if (categoryId.HasValue)
            {
                var curCat = db.Categories.Find(categoryId.Value);
                if (curCat != null) model.CurrentCategoryName = curCat.CategoryName;
            }

            return View(model);
        }

        // ==========================================
        // 2. PRODUCT DETAIL PAGE
        // ==========================================
        public ActionResult Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index");
            }

            Product product = null;

            // Try find by Slug or ID
            if (int.TryParse(id, out int productId))
            {
                product = db.Products.FirstOrDefault(p => p.ProductID == productId && p.IsActive);
            }
            if (product == null)
            {
                product = db.Products.FirstOrDefault(p => p.Slug == id && p.IsActive);
            }

            if (product == null)
            {
                return HttpNotFound();
            }

            var variants = product.ProductVariants.Where(v => v.IsActive).OrderBy(v => v.VolumeML).ToList();
            var images = product.ProductImages.OrderByDescending(img => img.IsPrimary).ToList();
            var reviews = product.ProductReviews.Where(r => r.IsApproved).OrderByDescending(r => r.CreatedAt).ToList();

            // Related Products (same brand or category)
            var relatedDb = db.Products
                .Where(p => p.IsActive && p.ProductID != product.ProductID && (p.BrandID == product.BrandID || p.CategoryID == product.CategoryID))
                .Take(4)
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
                    BrandName = p.Brand != null ? p.Brand.BrandName : "",
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

            var model = new ProductDetailViewModel
            {
                Product = product,
                Variants = variants,
                Images = images,
                Reviews = reviews,
                RelatedProducts = relatedDb.Select(mapToItem).ToList(),
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 5.0,
                ReviewCount = reviews.Count,
                SelectedVariant = variants.FirstOrDefault()
            };

            return View(model);
        }

        // ==========================================
        // 3. ADD REVIEW ACTION
        // ==========================================
        [HttpPost]
        public ActionResult AddReview(int productId, int rating, string comment)
        {
            var user = Helpers.AuthHelper.GetUser(Session);
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để gửi đánh giá." });
            }

            var review = new ProductReview
            {
                ProductID = productId,
                UserID = user.UserID,
                Rating = Math.Max(1, Math.Min(5, rating)),
                Comment = comment,
                IsApproved = true, // Auto-approve for university demo convenience
                CreatedAt = DateTime.Now
            };

            db.ProductReviews.Add(review);
            db.SaveChanges();

            return Json(new { success = true, message = "Đánh giá của bạn đã được gửi thành công!" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

