using System.Collections.Generic;
using System.Linq;
using Web_perfume.Models;

namespace Web_perfume.Helpers
{
    /*
     * CLASS: ProductMapper
     * MỤC ĐÍCH: Chứa hàm chuyển đổi (map) từ đối tượng Product (Entity từ database)
     *           sang ProductItemViewModel (dữ liệu để hiển thị trên giao diện).
     *
     * LÝ DO CẦN CLASS NÀY:
     *   - Trước đây, hàm mapToItem được viết lặp lại trong HomeController VÀ ProductsController.
     *   - Nếu cần sửa logic (ví dụ: cách tính giá, cách lấy ảnh), phải sửa ở 2 nơi → dễ quên.
     *   - Giải pháp: tách ra 1 class dùng chung → chỉ cần sửa 1 chỗ.
     *
     * CÁCH DÙNG trong Controller:
     *   using Web_perfume.Helpers;
     *   var viewModel = ProductMapper.MapToViewModel(product);
     *   var list = listProducts.Select(p => ProductMapper.MapToViewModel(p)).ToList();
     */
    public static class ProductMapper
    {
        /* ======================================================================
         * PHƯƠNG THỨC: MapToViewModel
         * MỤC ĐÍCH: Chuyển một đối tượng Product từ database sang ProductItemViewModel
         *           để dùng trong View (hiển thị card sản phẩm, danh sách, v.v.)
         *
         * THAM SỐ:
         *   - product (Product): Đối tượng sản phẩm từ Entity Framework (database)
         *
         * TRẢ VỀ:
         *   - ProductItemViewModel: Đối tượng chứa thông tin cần thiết để render View
         *
         * CÁC BƯỚC XỬ LÝ:
         *   1. Tìm biến thể (variant) mặc định để hiển thị (ưu tiên variant đang active)
         *   2. Tìm ảnh chính của sản phẩm (ưu tiên ảnh được đánh dấu IsPrimary)
         *   3. Tính giá thấp nhất từ các biến thể đang bán
         *   4. Tính giá khuyến mãi thấp nhất (nếu có)
         *   5. Tính điểm đánh giá trung bình từ các review đã duyệt
         *   6. Gộp tất cả vào ProductItemViewModel và trả về
         * ====================================================================== */
        public static ProductItemViewModel MapToViewModel(Product product)
        {
            // BƯỚC 1: Lấy biến thể mặc định
            // Ưu tiên biến thể đang active (IsActive = true)
            // Nếu không có biến thể nào active, lấy biến thể đầu tiên (dù inactive)
            var defaultVariant = product.ProductVariants.FirstOrDefault(v => v.IsActive)
                                 ?? product.ProductVariants.FirstOrDefault();

            // BƯỚC 2: Lấy URL ảnh chính
            // Ưu tiên ảnh được đánh dấu IsPrimary = true
            // Nếu không có ảnh chính, lấy ảnh đầu tiên trong danh sách
            var primaryImageUrl = product.ProductImages.FirstOrDefault(img => img.IsPrimary)?.ImageURL
                                  ?? product.ProductImages.FirstOrDefault()?.ImageURL;

            // BƯỚC 3: Tính giá thấp nhất từ các biến thể đang active
            // Lấy tất cả biến thể active để tính toán giá và tồn kho
            var activeVariants = product.ProductVariants.Where(v => v.IsActive).ToList();
            decimal minPrice = activeVariants.Any() ? activeVariants.Min(v => v.Price) : 0;

            // BƯỚC 4: Tìm giá khuyến mãi thấp nhất (nếu có)
            // Chỉ lấy những variant có SalePrice > 0 (tức là đang giảm giá)
            var saleVariants = activeVariants.Where(v => v.SalePrice.HasValue && v.SalePrice.Value > 0)
                                             .Select(v => v.SalePrice)
                                             .ToList();
            decimal? minSalePrice = saleVariants.Any() ? saleVariants.Min() : (decimal?)null;

            // BƯỚC 5: Tính điểm đánh giá trung bình từ các review đã được duyệt (IsApproved = true)
            var approvedReviews = product.ProductReviews.Where(r => r.IsApproved).ToList();
            double averageRating = approvedReviews.Any()
                ? approvedReviews.Average(r => r.Rating)
                : 5.0; // Mặc định 5 sao nếu chưa có ai đánh giá

            // BƯỚC 6: Tạo và trả về ViewModel
            return new ProductItemViewModel
            {
                ProductID        = product.ProductID,
                ProductName      = product.ProductName,
                Slug             = product.Slug,
                BrandID          = product.BrandID,
                BrandName        = product.Brand != null ? product.Brand.BrandName : "",
                CategoryName     = product.Category != null ? product.Category.CategoryName : "",
                Gender           = product.Gender,
                Concentration    = product.Concentration,
                PrimaryImageUrl  = primaryImageUrl,
                DefaultVariantID = defaultVariant != null ? defaultVariant.VariantID : 0,
                DefaultVolumeML  = defaultVariant != null ? defaultVariant.VolumeML : 0,
                TotalStock       = activeVariants.Sum(v => v.StockQuantity),
                MinPrice         = minPrice,
                MinSalePrice     = minSalePrice,
                AverageRating    = averageRating,
                ReviewCount      = approvedReviews.Count,
                IsFeatured       = product.IsFeatured
            };
        }

        /* ======================================================================
         * PHƯƠNG THỨC: MapListToViewModel
         * MỤC ĐÍCH: Chuyển một danh sách Product sang danh sách ProductItemViewModel.
         *           Là phiên bản tiện ích, giúp tránh viết .Select(p => ...).ToList() nhiều lần.
         *
         * THAM SỐ:
         *   - products: Danh sách sản phẩm từ database (IEnumerable có thể là List, IQueryable...)
         *
         * TRẢ VỀ:
         *   - List<ProductItemViewModel>: Danh sách ViewModel để truyền vào View
         * ====================================================================== */
        public static List<ProductItemViewModel> MapListToViewModel(IEnumerable<Product> products)
        {
            return products.Select(p => MapToViewModel(p)).ToList();
        }
    }
}
