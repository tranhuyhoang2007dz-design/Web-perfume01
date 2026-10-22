/**
 * FILE: product-detail.js
 * MỤC ĐÍCH: Chứa các hàm JavaScript dùng riêng cho trang Chi Tiết Sản Phẩm.
 *
 * CÁC HÀM TRONG FILE NÀY:
 *   - switchGalleryImage(el, src)  : Đổi ảnh lớn khi click vào ảnh nhỏ
 *   - selectVariant(btn)           : Chọn dung tích sản phẩm (30ml, 50ml...)
 *   - changeQuantity(delta)        : Tăng/giảm số lượng mua
 *   - onAddToCartClick()           : Xử lý click nút "Thêm vào giỏ hàng"
 *   - onBuyNowClick()              : Xử lý click nút "Mua ngay"
 *   - submitProductReview(e)       : Gửi đánh giá sản phẩm (AJAX)
 *
 * GHI CHÚ: File này được nhúng trong @section scripts của Detail.cshtml.
 *          Biến DETAIL_CONFIG và SITE_URLS được khai báo trong Detail.cshtml
 *          để truyền dữ liệu từ Razor (C#) sang JavaScript.
 *
 *   DETAIL_CONFIG = {
 *       currentVariantId: <ID biến thể đang chọn>   (số nguyên từ database)
 *   }
 *
 *   SITE_URLS = {
 *       cartAddUrl:    <URL action CartController.AddToCart>,
 *       checkoutUrl:   <URL trang checkout>,
 *       addReviewUrl:  <URL action ProductsController.AddReview>
 *   }
 */


/* ==========================================================================
   HÀM: switchGalleryImage
   MỤC ĐÍCH: Khi người dùng click vào ảnh nhỏ (thumbnail),
             thì cập nhật ảnh lớn ở trung tâm gallery.
   THAM SỐ:
     - el (HTMLElement): Phần tử thumbnail được click
     - src (string): Đường dẫn ảnh cần hiển thị ở khung lớn
   ========================================================================== */
function switchGalleryImage(el, src) {
    // Bỏ class 'active' khỏi tất cả thumbnail
    document.querySelectorAll('.gallery-thumb-item').forEach(function (thumb) {
        thumb.classList.remove('active');
    });

    // Gán class 'active' cho thumbnail vừa click (để CSS highlight nó)
    el.classList.add('active');

    // Cập nhật src của ảnh lớn
    document.getElementById('mainProductImage').src = src;
}


/* ==========================================================================
   HÀM: selectVariant
   MỤC ĐÍCH: Khi người dùng chọn dung tích khác (30ml → 100ml),
             cập nhật: giá hiển thị, trạng thái kho, mã SKU.
   THAM SỐ:
     - btn (HTMLElement): Nút dung tích được click
                          (có các data-* attribute chứa thông tin biến thể)
   ========================================================================== */
function selectVariant(btn) {
    // Bỏ active ở tất cả các nút dung tích
    document.querySelectorAll('.variant-btn').forEach(function (b) {
        b.classList.remove('active');
    });

    // Đánh dấu active nút vừa chọn
    btn.classList.add('active');

    // Đọc dữ liệu từ data-* attribute của nút (được gán trong HTML từ Razor)
    var variantId = parseInt(btn.getAttribute('data-variant-id'));
    var volume    = btn.getAttribute('data-volume');    // Dung tích (ml)
    var price     = parseFloat(btn.getAttribute('data-price'));    // Giá gốc
    var salePrice = parseFloat(btn.getAttribute('data-sale'));     // Giá khuyến mãi (0 nếu không có)
    var stock     = parseInt(btn.getAttribute('data-stock'));      // Số lượng tồn kho
    var sku       = btn.getAttribute('data-sku');                  // Mã sản phẩm

    // Lưu lại biến thể đang chọn (dùng khi click "Thêm vào giỏ")
    DETAIL_CONFIG.currentVariantId = variantId;

    // Cập nhật dung tích và mã SKU hiển thị
    document.getElementById('selectedVolumeText').textContent = volume + 'ml';
    document.getElementById('displaySku').textContent = sku;

    // Cập nhật giá: nếu có giá khuyến mãi và giá KM nhỏ hơn giá gốc
    if (salePrice > 0 && salePrice < price) {
        // Có khuyến mãi: hiển thị giá KM to, giá gốc gạch ngang, badge "GIẢM GIÁ"
        document.getElementById('displayCurrentPrice').textContent = salePrice.toLocaleString('vi-VN') + ' ₫';
        document.getElementById('displayOriginalPrice').textContent = price.toLocaleString('vi-VN') + ' ₫';
        document.getElementById('displayOriginalPrice').classList.remove('d-none');
        document.getElementById('displayDiscountBadge').classList.remove('d-none');
    } else {
        // Không có khuyến mãi: chỉ hiển thị giá gốc
        document.getElementById('displayCurrentPrice').textContent = price.toLocaleString('vi-VN') + ' ₫';
        document.getElementById('displayOriginalPrice').classList.add('d-none');
        document.getElementById('displayDiscountBadge').classList.add('d-none');
    }

    // Cập nhật trạng thái tồn kho và nút Thêm vào giỏ
    var stockText  = document.getElementById('stockStatusText');
    var btnAddCart = document.getElementById('btnAddToCart');

    if (stock > 0) {
        // Còn hàng
        stockText.textContent = 'Còn hàng (' + stock + ' sản phẩm có sẵn)';
        stockText.className   = 'text-success fw-bold ms-1';
        btnAddCart.disabled   = false;
    } else {
        // Hết hàng
        stockText.textContent = 'Tạm hết hàng';
        stockText.className   = 'text-danger fw-bold ms-1';
        btnAddCart.disabled   = true;
    }
}


/* ==========================================================================
   HÀM: changeQuantity
   MỤC ĐÍCH: Tăng hoặc giảm số lượng mua trong ô input số lượng.
   THAM SỐ:
     - delta (number): +1 để tăng, -1 để giảm
   ========================================================================== */
function changeQuantity(delta) {
    var input = document.getElementById('purchaseQuantity');

    // Đọc giá trị hiện tại, nếu không hợp lệ thì mặc định là 1
    var currentValue = parseInt(input.value) || 1;

    // Cộng thêm delta (tăng hoặc giảm), giữ trong khoảng [1, 99]
    var newValue = currentValue + delta;
    if (newValue < 1)  { newValue = 1; }
    if (newValue > 99) { newValue = 99; }

    input.value = newValue;
}


/* ==========================================================================
   HÀM: onAddToCartClick
   MỤC ĐÍCH: Đọc số lượng từ input rồi gọi quickAddToCart (trong site-global.js)
             để gửi yêu cầu thêm vào giỏ hàng.
   ========================================================================== */
function onAddToCartClick() {
    // Đọc số lượng từ ô input, mặc định là 1 nếu không hợp lệ
    var quantity = parseInt(document.getElementById('purchaseQuantity').value) || 1;

    // Gọi hàm quickAddToCart từ site-global.js
    quickAddToCart(DETAIL_CONFIG.currentVariantId, quantity);
}


/* ==========================================================================
   HÀM: onBuyNowClick
   MỤC ĐÍCH: Thêm sản phẩm vào giỏ rồi chuyển thẳng đến trang thanh toán.
   ========================================================================== */
function onBuyNowClick() {
    var quantity = parseInt(document.getElementById('purchaseQuantity').value) || 1;

    // Gửi AJAX để thêm vào giỏ hàng
    $.post(
        SITE_URLS.cartAddUrl,
        { variantId: DETAIL_CONFIG.currentVariantId, quantity: quantity },
        function (res) {
            if (res && res.success) {
                // Thêm thành công → chuyển sang trang thanh toán
                window.location.href = SITE_URLS.checkoutUrl;
            } else {
                showToast(res ? res.message : 'Có lỗi xảy ra', 'error');
            }
        }
    );
}


/* ==========================================================================
   HÀM: submitProductReview
   MỤC ĐÍCH: Gửi đánh giá sản phẩm bằng AJAX, không tải lại trang.
   THAM SỐ:
     - e (Event): Sự kiện submit của form đánh giá
   ========================================================================== */
function submitProductReview(e) {
    // Ngăn form tự submit (tải lại trang)
    e.preventDefault();

    var form = $(e.target);

    // Gửi POST request chứa dữ liệu form (rating + comment + productId)
    $.post(SITE_URLS.addReviewUrl, form.serialize(), function (res) {
        if (res && res.success) {
            showToast(res.message);
            // Tải lại trang sau 1.2 giây để hiển thị đánh giá mới
            setTimeout(function () {
                location.reload();
            }, 1200);
        } else {
            showToast(res ? res.message : 'Vui lòng thử lại', 'error');
        }
    });
}
