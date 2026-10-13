using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Web_perfume.Models
{
    // ==========================================
    // VIEW MODELS FOR HOMEPAGE
    // ==========================================
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<Brand> FeaturedBrands { get; set; }
        public List<ProductItemViewModel> FeaturedProducts { get; set; }
        public List<ProductItemViewModel> NewArrivals { get; set; }
        public List<ProductItemViewModel> BestSellers { get; set; }
    }

    // ==========================================
    // UNIFIED PRODUCT CARD VIEW MODEL
    // ==========================================
    public class ProductItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Slug { get; set; }
        public string BrandName { get; set; }
        public int BrandID { get; set; }
        public string CategoryName { get; set; }
        public string Gender { get; set; }
        public string Concentration { get; set; }
        public string PrimaryImageUrl { get; set; }
        public decimal MinPrice { get; set; }
        public decimal? MinSalePrice { get; set; }
        public int DefaultVariantID { get; set; }
        public int DefaultVolumeML { get; set; }
        public int TotalStock { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsFeatured { get; set; }

        public bool HasDiscount => MinSalePrice.HasValue && MinSalePrice.Value < MinPrice;
        public int DiscountPercentage => HasDiscount ? (int)Math.Round((MinPrice - MinSalePrice.Value) / MinPrice * 100) : 0;
        public decimal DisplayPrice => HasDiscount ? MinSalePrice.Value : MinPrice;
    }

    // ==========================================
    // VIEW MODEL FOR PRODUCT LISTING & FILTERING
    // ==========================================
    public class ProductListViewModel
    {
        public List<ProductItemViewModel> Products { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // Filter parameters
        public string Keyword { get; set; }
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public string Gender { get; set; }
        public string Concentration { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } // "price_asc", "price_desc", "name_asc", "newest"

        // Filter collections for sidebar UI
        public List<Brand> AvailableBrands { get; set; }
        public List<Category> AvailableCategories { get; set; }
        public List<string> AvailableGenders { get; set; }
        public List<string> AvailableConcentrations { get; set; }

        public string CurrentBrandName { get; set; }
        public string CurrentCategoryName { get; set; }
    }

    // ==========================================
    // VIEW MODEL FOR PRODUCT DETAIL
    // ==========================================
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<ProductVariant> Variants { get; set; }
        public List<ProductImage> Images { get; set; }
        public List<ProductReview> Reviews { get; set; }
        public List<ProductItemViewModel> RelatedProducts { get; set; }

        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public ProductVariant SelectedVariant { get; set; }
    }

    // ==========================================
    // VIEW MODELS FOR CART & CHECKOUT
    // ==========================================
    public class CartItemViewModel
    {
        public int CartItemID { get; set; }
        public int VariantID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Slug { get; set; }
        public string BrandName { get; set; }
        public string ImageUrl { get; set; }
        public int VolumeML { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }

        public decimal UnitPrice => (SalePrice.HasValue && SalePrice.Value > 0) ? SalePrice.Value : Price;
        public decimal SubTotal => UnitPrice * Quantity;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal SubTotal => Items.Sum(i => i.SubTotal);
        public string CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee => SubTotal >= 1000000 ? 0 : 30000;
        public decimal TotalAmount => Math.Max(0, SubTotal - DiscountAmount + ShippingFee);
        public int TotalItems => Items.Sum(i => i.Quantity);
    }

    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên người nhận")]
        public string ReceiverName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string ReceiverPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string ShippingAddress { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Ghi chú đơn hàng")]
        public string Note { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer

        public string CouponCode { get; set; }
    }

    // ==========================================
    // VIEW MODELS FOR AUTHENTICATION
    // ==========================================
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }
    }

    public class UserProfileViewModel
    {
        public User User { get; set; }
        public List<Order> Orders { get; set; }
    }

    // ==========================================
    // VIEW MODELS FOR ADMIN DASHBOARD
    // ==========================================
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalBrands { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrdersCount { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<ProductItemViewModel> LowStockProducts { get; set; }
    }
}

