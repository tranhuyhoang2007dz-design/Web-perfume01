// ================= BANNER SLIDER LOGIC (ULTRA-SMOOTH) =================
let currentIndex = 0;
let isTransitioning = false; // Cờ khóa chống spam click nút
let slideInterval = null;

function changeSlide(direction) {
    let slides = document.querySelectorAll('.my-slide');
    if (slides.length === 0 || isTransitioning) return;

    // Khóa bấm nút trong thời gian animation CSS đang chạy (1.2 giây)
    isTransitioning = true;

    // Bỏ class active ở slide hiện tại
    slides[currentIndex].classList.remove('active');

    // Tính toán index slide tiếp theo
    currentIndex += direction;
    if (currentIndex >= slides.length) {
        currentIndex = 0;
    }
    if (currentIndex < 0) {
        currentIndex = slides.length - 1;
    }

    // Thêm class active cho slide mới
    slides[currentIndex].classList.add('active');

    // Mở lại khả năng click sau khi hoàn tất hiệu ứng mượt
    setTimeout(function () {
        isTransitioning = false;
    }, 1200);

    // Reset lại thời gian tự động chuyển slide khi người dùng bấm thủ công
    resetAutoPlay();
}

// Hàm khởi chạy tự động chuyển slide (5 giây / slide)
function startAutoPlay() {
    stopAutoPlay();
    slideInterval = setInterval(function () {
        changeSlide(1);
    }, 5000);
}

// Hàm dừng tự động chuyển slide
function stopAutoPlay() {
    if (slideInterval) {
        clearInterval(slideInterval);
        slideInterval = null;
    }
}

// Hàm reset đếm thời gian
function resetAutoPlay() {
    stopAutoPlay();
    startAutoPlay();
}

// Khởi tạo các sự kiện khi trang tải xong
document.addEventListener('DOMContentLoaded', function () {
    let slides = document.querySelectorAll('.my-slide');

    if (slides.length > 0) {
        // Bắt đầu tự động trượt
        startAutoPlay();

        // Tạm dừng khi rê chuột vào banner, tiếp tục khi rê chuột ra ngoài
        let heroSection = document.querySelector('.hero-section');
        if (heroSection) {
            heroSection.addEventListener('mouseenter', stopAutoPlay);
            heroSection.addEventListener('mouseleave', startAutoPlay);
        }
    }
});