using System;
using System.Linq;
using System.Web.Mvc;
using Web_perfume.Helpers;
using Web_perfume.Models;

namespace Web_perfume.Controllers
{
    public class CartController : Controller
    {
        private PerfumeShopDBEntities db = new PerfumeShopDBEntities();

        // GET: /cart
        public ActionResult Index()
        {
            var cart = CartHelper.GetCart(Session, db);

            // Handle coupon in session if stored
            string couponCode = Session["APPLIED_COUPON"] as string;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = db.Coupons.FirstOrDefault(c => c.Code == couponCode && c.IsActive && c.ExpiryDate >= DateTime.Now);
                if (coupon != null && cart.SubTotal >= coupon.MinOrderAmount)
                {
                    cart.CouponCode = coupon.Code;
                    if (coupon.IsPercent)
                    {
                        cart.DiscountAmount = (cart.SubTotal * coupon.DiscountValue) / 100;
                    }
                    else
                    {
                        cart.DiscountAmount = coupon.DiscountValue;
                    }
                }
                else
                {
                    Session.Remove("APPLIED_COUPON");
                }
            }

            return View(cart);
        }

        // POST: /cart/addtocart
        [HttpPost]
        public ActionResult AddToCart(int variantId, int quantity = 1)
        {
            try
            {
                var variant = db.ProductVariants.Find(variantId);
                if (variant == null || !variant.IsActive)
                {
                    return Json(new { success = false, message = "Sản phẩm hoặc dung tích này không tồn tại." });
                }

                if (variant.StockQuantity < quantity)
                {
                    return Json(new { success = false, message = "Số lượng trong kho không đủ để đáp ứng." });
                }

                CartHelper.AddItem(Session, db, variantId, quantity);
                var cart = CartHelper.GetCart(Session, db);

                return Json(new
                {
                    success = true,
                    message = "Đã thêm " + variant.Product.ProductName + " (" + variant.VolumeML + "ml) vào giỏ hàng!",
                    cartCount = cart.TotalItems
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /cart/updatequantity
        [HttpPost]
        public ActionResult UpdateQuantity(int variantId, int quantity)
        {
            try
            {
                CartHelper.UpdateQuantity(Session, db, variantId, quantity);
                var cart = CartHelper.GetCart(Session, db);

                return Json(new
                {
                    success = true,
                    cartCount = cart.TotalItems,
                    subTotal = cart.SubTotal.ToString("N0") + " ₫",
                    totalAmount = cart.TotalAmount.ToString("N0") + " ₫"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /cart/removeitem
        [HttpPost]
        public ActionResult RemoveItem(int variantId)
        {
            try
            {
                CartHelper.RemoveItem(Session, db, variantId);
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // POST: /cart/applycoupon
        [HttpPost]
        public ActionResult ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                Session.Remove("APPLIED_COUPON");
                TempData["CouponError"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction("Index");
            }

            couponCode = couponCode.Trim().ToUpper();
            var coupon = db.Coupons.FirstOrDefault(c => c.Code == couponCode && c.IsActive && c.ExpiryDate >= DateTime.Now);

            if (coupon == null)
            {
                Session.Remove("APPLIED_COUPON");
                TempData["CouponError"] = "Mã giảm giá không tồn tại hoặc đã hết hạn.";
                return RedirectToAction("Index");
            }

            var cart = CartHelper.GetCart(Session, db);
            if (cart.SubTotal < coupon.MinOrderAmount)
            {
                Session.Remove("APPLIED_COUPON");
                TempData["CouponError"] = "Đơn hàng tối thiểu " + coupon.MinOrderAmount.ToString("N0") + "₫ để dùng mã này.";
                return RedirectToAction("Index");
            }

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                Session.Remove("APPLIED_COUPON");
                TempData["CouponError"] = "Mã giảm giá này đã hết lượt sử dụng.";
                return RedirectToAction("Index");
            }

            Session["APPLIED_COUPON"] = coupon.Code;
            TempData["CouponSuccess"] = "Áp dụng mã giảm giá thành công!";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

