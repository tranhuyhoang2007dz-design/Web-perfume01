let currentIndex = 0;

function changeSlide(direction) {
    let slides = document.querySelectorAll('.my-slide');
    if (slides.length === 0) return;

    slides[currentIndex].classList.remove('active');
    currentIndex += direction;

    if (currentIndex >= slides.length) {
        currentIndex = 0;
    }
    if (currentIndex < 0) {
        currentIndex = slides.length - 1;
    }

    slides[currentIndex].classList.add('active');
}

// Tự động chuyển slide sau mỗi 4 giây
setInterval(function () {
    changeSlide(1);
}, 4000);