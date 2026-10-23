using System;
using System.Linq;
using System.Web.Mvc;
using Web_perfume.Helpers;
using Web_perfume.Models;

namespace Web_perfume.Controllers
{
    public class CheckoutController : Controller
    {
        private PerfumeShopDBEntities db = new PerfumeShopDBEntities();

        // GET: /checkout
        public ActionResult Index()
        {
            var cart = CartHelper.GetCart(Session, db);
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Apply coupon if stored in session
            string couponCode = Session["APPLIED_COUPON"] as string;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = db.Coupons.FirstOrDefault(c => c.Code == couponCode && c.IsActive && c.ExpiryDate >= DateTime.Now);
                if (coupon != null && cart.SubTotal >= coupon.MinOrderAmount)
                {
                    cart.CouponCode = coupon.Code;
                    cart.DiscountAmount = coupon.IsPercent 
                        ? (cart.SubTotal * coupon.DiscountValue) / 100 
                        : coupon.DiscountValue;
                }
            }

            var model = new CheckoutViewModel
            {
                Cart = cart,
                CouponCode = cart.CouponCode,
                PaymentMethod = "COD"
            };

            // Pre-fill user information if logged in
            var currentUser = AuthHelper.GetUser(Session);
            if (currentUser != null)
            {
                var userDb = db.Users.Find(currentUser.UserID);
                if (userDb != null)
                {
                    model.ReceiverName = userDb.FullName;
                    model.ReceiverPhone = userDb.Phone;
                    model.ShippingAddress = userDb.Address;
                    model.Email = userDb.Email;
                }
            }

            return View(model);
        }

        // POST: /checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CheckoutViewModel model)
        {
            var cart = CartHelper.GetCart(Session, db);
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Recalculate coupon
            string couponCode = Session["APPLIED_COUPON"] as string;
            Coupon coupon = null;
            if (!string.IsNullOrEmpty(couponCode))
            {
                coupon = db.Coupons.FirstOrDefault(c => c.Code == couponCode && c.IsActive && c.ExpiryDate >= DateTime.Now);
                if (coupon != null && cart.SubTotal >= coupon.MinOrderAmount)
                {
                    cart.CouponCode = coupon.Code;
                    cart.DiscountAmount = coupon.IsPercent 
                        ? (cart.SubTotal * coupon.DiscountValue) / 100 
                        : coupon.DiscountValue;
                }
            }
            model.Cart = cart;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Determine UserID
                var currentUser = AuthHelper.GetUser(Session);
                int userId = 0;

                if (currentUser != null)
                {
                    userId = currentUser.UserID;
                }
                else
                {
                    // Check if a user with this email already exists or use default customer
                    var existingUser = db.Users.FirstOrDefault(u => u.Email == model.Email);
                    if (existingUser != null)
                    {
                        userId = existingUser.UserID;
                    }
                    else
                    {
                        // Create customer record for guest checkout
                        var guestUser = new User
                        {
                            RoleID = 3, // Customer role
                            FullName = model.ReceiverName,
                            Email = !string.IsNullOrWhiteSpace(model.Email) ? model.Email : ("guest_" + DateTime.Now.Ticks + "@perfumeshop.vn"),
                            PasswordHash = "guest123",
                            Phone = model.ReceiverPhone,
                            Address = model.ShippingAddress,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        db.Users.Add(guestUser);
                        db.SaveChanges();
                        userId = guestUser.UserID;
                    }
                }

                // Create Order
                string orderCode = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var order = new Order
                {
                    OrderCode = orderCode,
                    UserID = userId,
                    CouponID = coupon != null ? (int?)coupon.CouponID : null,
                    ReceiverName = model.ReceiverName,
                    ReceiverPhone = model.ReceiverPhone,
                    ShippingAddress = model.ShippingAddress,
                    SubTotal = cart.SubTotal,
                    DiscountAmount = cart.DiscountAmount,
                    ShippingFee = cart.ShippingFee,
                    TotalAmount = cart.TotalAmount,
                    OrderStatus = "Pending",
                    PaymentMethod = model.PaymentMethod ?? "COD",
                    PaymentStatus = "Unpaid",
                    OrderDate = DateTime.Now
                };

                db.Orders.Add(order);
                db.SaveChanges();

                // Create OrderItems & update stock
                foreach (var item in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderID = order.OrderID,
                        VariantID = item.VariantID,
                        ProductNameSnapshot = item.ProductName,
                        SKU = item.SKU,
                        VolumeML = item.VolumeML,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    db.OrderItems.Add(orderItem);

                    // Deduct stock
                    var variant = db.ProductVariants.Find(item.VariantID);
                    if (variant != null)
                    {
                        variant.StockQuantity = Math.Max(0, variant.StockQuantity - item.Quantity);
                    }
                }

                // Increment coupon used count if used
                if (coupon != null)
                {
                    coupon.UsedCount += 1;
                }

                db.SaveChanges();

                // Clear cart and coupon
                CartHelper.ClearCart(Session, db);
                Session.Remove("APPLIED_COUPON");

                // KIỂM TRA PHƯƠNG THỨC THANH TOÁN: Nếu là chuyển khoản online / VietQR
                if (order.PaymentMethod == "BankTransfer" || order.PaymentMethod == "VietQR" || order.PaymentMethod == "Online")
                {
                    return RedirectToAction("PaymentQR", new { orderCode = order.OrderCode });
                }

                // Nếu là COD hoặc phương thức khác thì về trang đặt hàng thành công thông thường
                return RedirectToAction("OrderSuccess", new { id = order.OrderID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Không thể tạo đơn hàng: " + ex.Message);
                return View(model);
            }
        }

        // GET: /checkout/ordersuccess/{id}
        public ActionResult OrderSuccess(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = db.Orders.Find(id.Value);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        // GET: Checkout/PaymentQR
        public ActionResult PaymentQR(string orderCode)
        {
            var order = db.Orders.FirstOrDefault(o => o.OrderCode == orderCode);
            if (order == null)
            {
                return HttpNotFound("Không tìm thấy đơn hàng!");
            }

            string bankId = "VietinBank"; // Thay bằng mã ngân hàng của bạn
            string accountNo = "102876116407"; // Thay bằng STK của bạn
            string accountName = Uri.EscapeDataString("TRAN HUY HOANG"); // Tên chủ TK
            string addInfo = Uri.EscapeDataString(order.OrderCode);
            long amount = Convert.ToInt64(order.TotalAmount);

            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={amount}&addInfo={addInfo}&accountName={accountName}";

            ViewBag.QrUrl = qrUrl;
            return View(order);
        }
    }
}

