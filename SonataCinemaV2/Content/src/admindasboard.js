// JavaScript to handle active state change
const links = document.querySelectorAll('.nav-link');
        
links.forEach(link => {
  link.addEventListener('click', function() {
    // Remove active class from all links
    links.forEach(l => l.classList.remove('active'));
    
    // Add active class to the clicked link
    this.classList.add('active');
  });
});


document.addEventListener("DOMContentLoaded", function () {
    const manageButtons = document.querySelectorAll(".nav-link");
    const homeContent = document.querySelector(".home-content");
    const employeeManagement = document.querySelector(".employee-management");
    const movieManagement = document.querySelector(".movie-management");
    const screeningRoomManagement = document.querySelector(".screening-room-management");
    const customerManagement = document.querySelector(".customer-management");
    const showtimeManagement = document.querySelector(".showtime-management");

    manageButtons.forEach(button => {
        button.addEventListener("click", function (event) {
            event.preventDefault(); // Ngăn không cho link dẫn đến trang khác

            // Ẩn tất cả nội dung
            homeContent.classList.add("d-none");
            employeeManagement.classList.add("d-none");
            movieManagement.classList.add("d-none");
            screeningRoomManagement.classList.add("d-none");
            customerManagement.classList.add("d-none");
            showtimeManagement.classList.add("d-none");

            // Hiển thị nội dung tương ứng
            if (this.textContent.trim() === "Quản lý Nhân viên") {
                employeeManagement.classList.remove("d-none");
            } else if (this.textContent.trim() === "Quản lý Phim") {
                movieManagement.classList.remove("d-none");
            } else if (this.textContent.trim() === "Quản lý Phòng Chiếu") {
                screeningRoomManagement.classList.remove("d-none");
            } else if (this.textContent.trim() === "Quản lý Khách Hàng") {
                customerManagement.classList.remove("d-none");
            } else if (this.textContent.trim() === "Quản lý Lịch Chiếu") {
                showtimeManagement.classList.remove("d-none");
            } else if (this.textContent.trim() === "Trang chủ") {
                homeContent.classList.remove("d-none");
            }
        });
    });
});


function loadFile(event) {
    var output = document.getElementById('output');
    output.src = URL.createObjectURL(event.target.files[0]);
    output.style.display = 'block'; // Hiển thị ảnh khi người dùng chọn
    output.style.background = 'none'; // No background
}

function themLichChieu() {
    const data = {
        maPhim: $('#maPhim').val(),
        maPhong: $('#maPhong').val(),
        tuNgay: $('#tuNgay').val(),
        denNgay: $('#denNgay').val(),
        gioChieu: $('#gioChieu').val()
    };

    $.ajax({
        url: '/LichChieu/ThemLichChieu',
        type: 'POST',
        data: data,
        success: function (response) {
            if (response.success) {
                alert('Thêm lịch chiếu thành công');
                $('#themLichChieuModal').modal('hide');
                loadLichChieu();
            } else {
                alert('Lỗi: ' + response.message);
            }
        },
        error: function () {
            alert('Có lỗi xảy ra');
        }
    });
}

//function xoaLichChieu(maLichChieu) {
//    if (confirm('Bạn có chắc muốn xóa lịch chiếu này?')) {
//        $.ajax({
//            url: '/LichChieu/XoaLichChieu',
//            type: 'POST',
//            data: { maLichChieu: maLichChieu },
//            success: function (response) {
//                if (response.success) {
//                    alert('Xóa lịch chiếu thành công');
//                    loadLichChieu();
//                } else {
//                    alert('Lỗi: ' + response.message);
//                }
//            },
//            error: function () {
//                alert('Có lỗi xảy ra');
//            }
//        });
//    }
//}

function loadLichChieu() {
    $.get('/LichChieu/ReloadDanhSach', function (data) {
        $('.showtime-management').html(data);
    });
}

