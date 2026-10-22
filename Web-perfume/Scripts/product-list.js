/**
 * FILE: product-list.js
 * MỤC ĐÍCH: Chứa JavaScript dùng cho trang Danh Sách Sản Phẩm (Products/Index).
 *
 * CÁC HÀM TRONG FILE NÀY:
 *   - applySort(sortValue) : Cập nhật lựa chọn sắp xếp và tự động submit form lọc
 *
 * GHI CHÚ: File này được nhúng trong @section scripts của Products/Index.cshtml.
 *          Nó thao tác với form có id="filterForm" và input có id="sortInput".
 */


/* ==========================================================================
   HÀM: applySort
   MỤC ĐÍCH: Khi người dùng thay đổi dropdown "Sắp xếp theo",
             cập nhật giá trị hidden input rồi submit form để lọc lại.
   THAM SỐ:
     - sortValue (string): Giá trị sắp xếp được chọn
                           Ví dụ: 'newest', 'price_asc', 'price_desc', 'name_asc', 'sale'
   CÁCH HOẠT ĐỘNG:
     1. Tìm hidden input có id="sortInput" trong form
     2. Gán giá trị sortValue vào đó
     3. Submit form → Controller nhận và xử lý theo sort mới
   ========================================================================== */
function applySort(sortValue) {
    // Cập nhật giá trị của hidden input (để gửi lên controller cùng form filter)
    document.getElementById('sortInput').value = sortValue;

    // Submit form để load lại trang với bộ lọc và sắp xếp mới
    document.getElementById('filterForm').submit();
}
