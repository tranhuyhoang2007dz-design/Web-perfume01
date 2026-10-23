using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Web_perfume.Helpers;
using Web_perfume.Models;

namespace Web_perfume.Controllers
{
    public class AdminController : Controller
    {
        private PerfumeShopDBEntities db = new PerfumeShopDBEntities();

        // Security check filter helper
        private bool CheckAdminAccess()
        {
            var user = AuthHelper.GetUser(Session);
            return user != null && user.IsAdmin;
        }

        // ==========================================
        // 1. DASHBOARD
        // ==========================================
        public ActionResult Index()
        {
            if (!CheckAdminAccess())
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Admin") });
            }

            var model = new AdminDashboardViewModel
            {
                TotalProducts = db.Products.Count(),
                TotalBrands = db.Brands.Count(),
                TotalOrders = db.Orders.Count(),
                TotalUsers = db.Users.Count(),
                TotalRevenue = db.Orders.Any() ? db.Orders.Sum(o => o.TotalAmount) : 0,
                PendingOrdersCount = db.Orders.Count(o => o.OrderStatus == "Pending"),
                RecentOrders = db.Orders.OrderByDescending(o => o.OrderDate).Take(6).ToList()
            };

            return View(model);
        }

        // ==========================================
        // 2. PRODUCT MANAGEMENT (CRUD)
        // ==========================================
        public ActionResult Products()
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var products = db.Products
                .OrderByDescending(p => p.ProductID)
                .ToList();

            return View(products);
        }

        // GET: /admin/createproduct
        public ActionResult CreateProduct()
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            ViewBag.BrandID = new SelectList(db.Brands.Where(b => b.IsActive), "BrandID", "BrandName");
            ViewBag.CategoryID = new SelectList(db.Categories.Where(c => c.IsActive), "CategoryID", "CategoryName");
            return View();
        }

        // POST: /admin/createproduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(Product product, int volumeML, decimal price, decimal? salePrice, int stockQuantity, HttpPostedFileBase imageFile)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.Now;
                product.Slug = GenerateSlug(product.ProductName);
                db.Products.Add(product);

                var imageUrl = SaveProductImage(imageFile) ?? "/Content/images/placeholder-perfume.svg";
                db.ProductImages.Add(new ProductImage
                {
                    ProductID = product.ProductID,
                    ImageURL = imageUrl,
                    IsPrimary = true
                });
                db.SaveChanges();

                // Add initial Variant
                var variant = new ProductVariant
                {
                    ProductID = product.ProductID,
                    VolumeML = volumeML > 0 ? volumeML : 100,
                    SKU = "PRD-" + product.ProductID + "-" + (volumeML > 0 ? volumeML : 100),
                    Price = price > 0 ? price : 1000000,
                    SalePrice = salePrice,
                    StockQuantity = stockQuantity >= 0 ? stockQuantity : 10,
                    IsActive = true
                };
                db.ProductVariants.Add(variant);

                // Add primary image if provided
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    db.ProductImages.Add(new ProductImage
                    {
                        ProductID = product.ProductID,
                        ImageURL = imageUrl.Trim(),
                        IsPrimary = true
                    });
                }
                else
                {
                    db.ProductImages.Add(new ProductImage
                    {
                        ProductID = product.ProductID,
                        ImageURL = "/Content/images/placeholder-perfume.svg",
                        IsPrimary = true
                    });
                }

                db.SaveChanges();
                TempData["AdminSuccess"] = "Thêm sản phẩm mới thành công!";
                return RedirectToAction("Products");
            }

            ViewBag.BrandID = new SelectList(db.Brands.Where(b => b.IsActive), "BrandID", "BrandName", product.BrandID);
            ViewBag.CategoryID = new SelectList(db.Categories.Where(c => c.IsActive), "CategoryID", "CategoryName", product.CategoryID);
            return View(product);
        }

        // GET: /admin/editproduct/{id}
        public ActionResult EditProduct(int? id)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");
            if (!id.HasValue) return RedirectToAction("Products");

            var product = db.Products.Find(id.Value);
            if (product == null) return HttpNotFound();

            LoadEditProductData(product);
            return View(product);
        }

        // POST: /admin/editproduct/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product product, HttpPostedFileBase imageFile,
            int? primaryImageId, int[] deleteImageIds, bool setNewAsPrimary = false)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var imageError = ValidateImage(imageFile);
            if (imageError != null)
                ModelState.AddModelError("", imageError);

            if (ModelState.IsValid)
            {
                var existing = db.Products.Find(product.ProductID);
                if (existing != null)
                {
                    existing.ProductName = product.ProductName;
                    existing.BrandID = product.BrandID;
                    existing.CategoryID = product.CategoryID;
                    existing.Gender = product.Gender;
                    existing.Concentration = product.Concentration;
                    existing.Description = product.Description;
                    existing.IsFeatured = product.IsFeatured;
                    existing.IsActive = product.IsActive;

                    // 1. Xóa các ảnh được chọn
                    if (deleteImageIds != null && deleteImageIds.Length > 0)
                    {
                        var toDelete = existing.ProductImages
                            .Where(i => deleteImageIds.Contains(i.ImageID))
                            .ToList();
                        foreach (var img in toDelete)
                        {
                            DeleteImageFile(img.ImageURL);
                            db.ProductImages.Remove(img);
                        }
                    }

                    // Ảnh còn lại sau khi xóa
                    var remaining = existing.ProductImages
                        .Where(i => deleteImageIds == null || !deleteImageIds.Contains(i.ImageID))
                        .ToList();

                    // 2. Đổi ảnh chính theo lựa chọn
                    if (primaryImageId.HasValue && remaining.Any(i => i.ImageID == primaryImageId.Value))
                    {
                        foreach (var img in remaining)
                            img.IsPrimary = (img.ImageID == primaryImageId.Value);
                    }

                    // 3. Thêm ảnh mới
                    var newUrl = SaveProductImage(imageFile);
                    if (newUrl != null)
                    {
                        bool makePrimary = setNewAsPrimary || !remaining.Any();
                        if (makePrimary)
                        {
                            foreach (var img in remaining) img.IsPrimary = false;
                        }
                        db.ProductImages.Add(new ProductImage
                        {
                            ProductID = existing.ProductID,
                            ImageURL = newUrl,
                            IsPrimary = makePrimary
                        });
                    }
                    else if (remaining.Any() && !remaining.Any(i => i.IsPrimary))
                    {
                        // Đảm bảo luôn có một ảnh chính
                        remaining.First().IsPrimary = true;
                    }

                    db.SaveChanges();
                    TempData["AdminSuccess"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Products");
                }
            }

            LoadEditProductData(product);
            return View(product);
        }

        private void LoadEditProductData(Product product)
        {
            ViewBag.BrandID = new SelectList(db.Brands.Where(b => b.IsActive), "BrandID", "BrandName", product.BrandID);
            ViewBag.CategoryID = new SelectList(db.Categories.Where(c => c.IsActive), "CategoryID", "CategoryName", product.CategoryID);
            ViewBag.Images = db.ProductImages
                .Where(i => i.ProductID == product.ProductID)
                .OrderByDescending(i => i.IsPrimary)
                .ToList();
        }

        // Chỉ xóa file do hệ thống upload, không đụng tới placeholder hay ảnh ngoài
        private void DeleteImageFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            if (!imageUrl.StartsWith("/Content/images/products/", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                var path = Server.MapPath("~" + imageUrl);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { /* bỏ qua lỗi xóa file */ }
        }

        // POST: /admin/deleteproduct/{id}
        [HttpPost]
        public ActionResult DeleteProduct(int id)
        {
            if (!CheckAdminAccess()) return Json(new { success = false, message = "Chưa đăng nhập quyền Admin" });

            var product = db.Products.Find(id);
            if (product != null)
            {
                // Soft delete by setting IsActive = false
                product.IsActive = !product.IsActive;
                db.SaveChanges();
                return Json(new { success = true, isActive = product.IsActive });
            }
            return Json(new { success = false });
        }

        // ==========================================
        // 3. BRAND MANAGEMENT (CRUD)
        // ==========================================
        public ActionResult Brands()
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var brands = db.Brands.OrderBy(b => b.BrandName).ToList();
            return View(brands);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBrand(Brand brand)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                brand.IsActive = true;
                db.Brands.Add(brand);
                db.SaveChanges();
                TempData["AdminSuccess"] = "Thêm thương hiệu " + brand.BrandName + " thành công!";
            }
            return RedirectToAction("Brands");
        }

        // GET: /admin/editbrand/{id}
        public ActionResult EditBrand(int? id)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");
            if (!id.HasValue) return RedirectToAction("Brands");

            var brand = db.Brands.Find(id.Value);
            if (brand == null) return HttpNotFound();

            ViewBag.ProductCount = db.Products.Count(p => p.BrandID == brand.BrandID);
            return View(brand);
        }

        // POST: /admin/editbrand/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBrand(Brand brand)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var name = (brand.BrandName ?? "").Trim();
            if (name.Length == 0)
                ModelState.AddModelError("", "Vui lòng nhập tên thương hiệu.");
            else if (db.Brands.Any(b => b.BrandName == name && b.BrandID != brand.BrandID))
                ModelState.AddModelError("", "Tên thương hiệu \"" + name + "\" đã tồn tại.");

            if (ModelState.IsValid)
            {
                var existing = db.Brands.Find(brand.BrandID);
                if (existing == null) return HttpNotFound();

                existing.BrandName = name;
                existing.Country = brand.Country;
                existing.Description = brand.Description;
                existing.IsActive = brand.IsActive;
                db.SaveChanges();

                TempData["AdminSuccess"] = "Cập nhật thương hiệu " + name + " thành công!";
                return RedirectToAction("Brands");
            }

            ViewBag.ProductCount = db.Products.Count(p => p.BrandID == brand.BrandID);
            return View(brand);
        }

        // POST: /admin/deletebrand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBrand(int id)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var brand = db.Brands.Find(id);
            if (brand == null) return HttpNotFound();

            var productCount = db.Products.Count(p => p.BrandID == id);
            if (productCount > 0)
            {
                TempData["AdminError"] = "Không thể xóa vì thương hiệu còn " + productCount
                    + " sản phẩm. Hãy chuyển sản phẩm sang thương hiệu khác hoặc chọn Tạm ẩn.";
                return RedirectToAction("EditBrand", new { id = id });
            }

            var brandName = brand.BrandName;
            db.Brands.Remove(brand);
            db.SaveChanges();

            TempData["AdminSuccess"] = "Đã xóa thương hiệu " + brandName + ".";
            return RedirectToAction("Brands");
        }

        // ==========================================
        // 4. CATEGORY MANAGEMENT (CRUD)
        // ==========================================
        public ActionResult Categories()
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var categories = db.Categories.OrderBy(c => c.CategoryID).ToList();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory(Category category)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                category.Slug = GenerateSlug(category.CategoryName);
                category.IsActive = true;
                db.Categories.Add(category);
                db.SaveChanges();
                TempData["AdminSuccess"] = "Thêm danh mục mới thành công!";
            }
            return RedirectToAction("Categories");
        }

        // POST: /admin/editcategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(Category category, bool IsActive = false)
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var name = (category.CategoryName ?? "").Trim();
            if (name.Length == 0)
            {
                TempData["AdminError"] = "Vui lòng nhập tên danh mục.";
                return RedirectToAction("Categories");
            }
            if (db.Categories.Any(c => c.CategoryName == name && c.CategoryID != category.CategoryID))
            {
                TempData["AdminError"] = "Tên danh mục \"" + name + "\" đã tồn tại.";
                return RedirectToAction("Categories");
            }

            var existing = db.Categories.Find(category.CategoryID);
            if (existing == null) return HttpNotFound();

            existing.CategoryName = name;
            existing.Slug = GenerateSlug(name);
            existing.Description = category.Description;
            existing.IsActive = IsActive;
            db.SaveChanges();

            TempData["AdminSuccess"] = "Cập nhật danh mục " + name + " thành công!";
            return RedirectToAction("Categories");
        }

        // POST: /admin/togglecategory
        [HttpPost]
        public ActionResult ToggleCategory(int id)
        {
            if (!CheckAdminAccess()) return Json(new { success = false, message = "Chưa đăng nhập quyền Admin" });

            var category = db.Categories.Find(id);
            if (category == null) return Json(new { success = false });

            category.IsActive = !category.IsActive;
            db.SaveChanges();
            return Json(new { success = true, isActive = category.IsActive });
        }

        // GET: /admin/getcategoryproducts?id=1
        [HttpGet]
        public ActionResult GetCategoryProducts(int id)
        {
            if (!CheckAdminAccess())
                return Json(new { success = false, message = "Không có quyền" }, JsonRequestBehavior.AllowGet);

            var products = db.Products
                .Where(p => p.CategoryID == id)
                .OrderByDescending(p => p.ProductID)
                .ToList()
                .Select(p => new
                {
                    id = p.ProductID,
                    name = p.ProductName,
                    brand = p.Brand != null ? p.Brand.BrandName : "",
                    gender = p.Gender,
                    isActive = p.IsActive,
                    image = p.ProductImages.FirstOrDefault(i => i.IsPrimary)?.ImageURL
                            ?? p.ProductImages.FirstOrDefault()?.ImageURL
                            ?? "/Content/images/placeholder-perfume.svg"
                })
                .ToList();

            return Json(new { success = true, products = products }, JsonRequestBehavior.AllowGet);
        }

        // ==========================================
        // 5. ORDERS MANAGEMENT
        // ==========================================
        public ActionResult Orders()
        {
            if (!CheckAdminAccess()) return RedirectToAction("Login", "Account");

            var orders = db.Orders.OrderByDescending(o => o.OrderDate).ToList();
            return View(orders);
        }

        [HttpPost]
        public ActionResult UpdateOrderStatus(int orderId, string status)
        {
            if (!CheckAdminAccess()) return Json(new { success = false, message = "Không có quyền" });

            var order = db.Orders.Find(orderId);
            if (order != null)
            {
                order.OrderStatus = status;
                if (status == "Delivered")
                {
                    order.PaymentStatus = "Paid";
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Helper slug generator
        private string GenerateSlug(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return Guid.NewGuid().ToString();
            string str = phrase.ToLower().Trim();
            // Simple Vietnamese character conversion
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[áàảãạâấầẩẫậăắằẳẵặ]", "a");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[éèẻẽẹêếềểễệ]", "e");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[íìỉĩị]", "i");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[óòỏõọôốồổỗộơớờởỡợ]", "o");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[úùủũụưứừửữự]", "u");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[ýỳỷỹỵ]", "y");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[đ]", "d");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "-").Trim('-');
            return str;
        }


        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const int MaxImageBytes = 3 * 1024 * 1024;

        private string ValidateImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
                return "Chỉ chấp nhận ảnh định dạng JPG, PNG, WEBP hoặc GIF.";
            if (!file.ContentType.StartsWith("image/"))
                return "Tệp được chọn không phải là ảnh.";
            if (file.ContentLength > MaxImageBytes)
                return "Ảnh không được vượt quá 3MB.";
            return null;
        }

        private string SaveProductImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0) return null;

            var folder = Server.MapPath("~/Content/images/products");
            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
            file.SaveAs(Path.Combine(folder, fileName));

            return Url.Content("~/Content/images/products/" + fileName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

