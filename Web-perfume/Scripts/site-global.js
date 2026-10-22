/**
 * FILE: site-global.js
 * MỤC ĐÍCH: Chứa các hàm JavaScript dùng chung trên toàn bộ website.
 *
 * CÁC HÀM TRONG FILE NÀY:
 *   - showToast(message, type)  : Hiển thị thông báo nhỏ góc phải màn hình
 *   - quickAddToCart(variantId, quantity) : Thêm sản phẩm vào giỏ hàng (AJAX)
 *   - toggleWishlist(btn, productId)     : Bật/tắt nút yêu thích
 *   - subscribeNewsletter(e)             : Xử lý form đăng ký email
 *
 * GHI CHÚ: File này được nhúng vào _Layout.cshtml (xuất hiện trên mọi trang).
 *          Biến SITE_URLS được khai báo trong _Layout.cshtml để truyền URL từ Razor sang JS.
 */


/* ==========================================================================
   HÀM: showToast
   MỤC ĐÍCH: Tạo và hiển thị một hộp thông báo nhỏ (toast) ở góc phải màn hình.
   THAM SỐ:
     - message (string): Nội dung cần hiển thị
     - type (string): Loại thông báo, 'info' (mặc định) hoặc 'error' (lỗi)
   ========================================================================== */
function showToast(message, type) {
    // Nếu không truyền type thì mặc định là 'info'
    type = type || 'info';

    // Lấy phần tử container chứa các toast (đã có trong _Layout.cshtml)
    var container = document.getElementById('toast-container');

    // Tạo một div mới làm toast
    var toast = document.createElement('div');
    toast.className = 'luxury-toast';

    // Chọn icon phù hợp theo loại thông báo
    var icon = '<i class="bi bi-check-circle-fill text-warning"></i>';
    if (type === 'error') {
        icon = '<i class="bi bi-exclamation-triangle-fill text-danger"></i>';
    }

    // Điền nội dung HTML vào toast
    toast.innerHTML = icon + ' <span>' + message + '</span>';

    // Thêm toast vào container
    container.appendChild(toast);

    // Sau 50ms thì cho toast hiện ra (có hiệu ứng fade-in nhờ CSS transition)
    setTimeout(function () {
        toast.classList.add('show');
    }, 50);

    // Sau 3.5 giây thì ẩn toast đi, rồi xóa khỏi DOM sau 400ms nữa
    setTimeout(function () {
        toast.classList.remove('show');
        setTimeout(function () {
            toast.remove();
        }, 400);
    }, 3500);
}


/* ==========================================================================
   HÀM: quickAddToCart
   MỤC ĐÍCH: Gửi yêu cầu AJAX lên server để thêm sản phẩm vào giỏ hàng.
             Không tải lại trang, chỉ cập nhật số lượng trên badge giỏ hàng.
   THAM SỐ:
     - variantId (number): ID của biến thể sản phẩm (dung tích cụ thể)
     - quantity (number): Số lượng muốn thêm
   YÊU CẦU: Biến SITE_URLS.cartAddUrl phải được khai báo trong _Layout.cshtml
   ========================================================================== */
function quickAddToCart(variantId, quantity) {
    // Gửi POST request lên controller CartController, action AddToCart
    $.ajax({
        url: SITE_URLS.cartAddUrl,   // URL lấy từ biến được khai báo trong Layout
        type: 'POST',
        data: { variantId: variantId, quantity: quantity },

        // Khi server phản hồi thành công
        success: function (res) {
            if (res && res.success) {
                // Cập nhật số lượng trên badge giỏ hàng ở header
                $('#cartBadgeCount').text(res.cartCount);
                showToast(res.message || 'Đã thêm sản phẩm vào giỏ hàng thành công!');
            } else {
                // Server trả về nhưng có lỗi nghiệp vụ (ví dụ: hết hàng)
                showToast(res ? res.message : 'Có lỗi xảy ra, vui lòng thử lại', 'error');
            }
        },

        // Khi không kết nối được đến server
        error: function () {
            showToast('Không thể kết nối đến máy chủ', 'error');
        }
    });
}


/* ==========================================================================
   HÀM: toggleWishlist
   MỤC ĐÍCH: Bật/tắt nút trái tim (yêu thích) trên card sản phẩm.
             (Chức năng này là demo UI, chưa lưu vào database)
   THAM SỐ:
     - btn (HTMLElement): Nút được bấm (dùng để thay đổi class CSS)
     - productId (number): ID sản phẩm (để dùng sau khi có backend thật)
   ========================================================================== */
function toggleWishlist(btn, productId) {
    // Dùng jQuery để thao tác class trên nút
    $(btn).toggleClass('active');

    if ($(btn).hasClass('active')) {
        // Đang bật yêu thích: đổi icon rỗng → tim đầy
        $(btn).find('i').removeClass('bi-heart').addClass('bi-heart-fill');
        showToast('Đã thêm vào danh sách yêu thích!');
    } else {
        // Đang tắt yêu thích: đổi icon tim đầy → rỗng
        $(btn).find('i').removeClass('bi-heart-fill').addClass('bi-heart');
        showToast('Đã bỏ khỏi danh sách yêu thích.');
    }
}


/* ==========================================================================
   HÀM: subscribeNewsletter
   MỤC ĐÍCH: Xử lý sự kiện submit form đăng ký email ở footer.
             (Chức năng này là demo UI, chưa gửi email thật)
   THAM SỐ:
     - e (Event): Đối tượng sự kiện submit
   ========================================================================== */
function subscribeNewsletter(e) {
    // Ngăn form tải lại trang (hành vi mặc định của form)
    e.preventDefault();

    // Hiển thị thông báo cảm ơn
    showToast('Cảm ơn bạn đã đăng ký nhận tin từ MONA PERFUME!');

    // Xóa dữ liệu trong form sau khi đăng ký
    e.target.reset();
}
