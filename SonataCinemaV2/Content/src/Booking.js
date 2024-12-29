$(document).ready(function () {
    let selectedSeats = [];
    let allLichChieu = [];
    const ticketPrice = 50000;

    const movieSelect = $("#movieSelect");
    const dateSelect = $("#dateSelect");
    const roomSelect = $("#roomSelect");
    const timeSelect = $("#timeSelect");

    const nextStepButton = $("#next-step");
    const seatSelectionContainer = $("#seatSelectionContainer");
    const bookingForm = $("#booking-form");

    const selectedMovie = $("#selected-movie");
    const selectedDate = $("#selected-date");
    const selectedRoom = $("#selected-room");
    const selectedTime = $("#selected-time");

    // Tạo hàm chung để lấy sơ đồ ghế
    function loadSeats(roomId, lichChieuId) {
        console.log("Loading seats for roomId:", roomId, "lichChieuId:", lichChieuId);

        if (!roomId || !lichChieuId) {
            console.error("Missing required parameters:", { roomId, lichChieuId });
            return Promise.reject("Missing parameters");
        }

        return $.ajax({
            url: '/Booking/GetSeats',
            type: 'GET',
            data: {
                room: parseInt(roomId), // Đảm bảo chuyển đổi sang số
                lichChieuId: parseInt(lichChieuId)
            }
        })
            .then(function (response) {
                console.log("Seats loaded successfully");
                $("#seat-selection").html(response);
                return true;
            })
            .fail(function (xhr, status, error) {
                console.error("Error loading seats:", {
                    status: xhr.status,
                    statusText: xhr.statusText,
                    error: error,
                    response: xhr.responseText
                });
                return false;
            });
    }

    // Xử lý QuickBooking data
    console.log("Checking QuickBooking data");
    const quickBookingData = localStorage.getItem('quickBooking');

    if (quickBookingData) {
        try {
            const booking = JSON.parse(quickBookingData);
            console.log("QuickBooking data:", booking);

            // Điền thông tin vào form
            movieSelect.val(booking.movie);
            dateSelect
                .prop('disabled', false)
                .html(`<option value="${booking.date}">${booking.date}</option>`)
                .val(booking.date);
            timeSelect
                .prop('disabled', false)
                .html(`<option value="${booking.time}">${booking.time}</option>`)
                .val(booking.time);
            roomSelect
                .prop('disabled', false)
                .html(`<option value="${booking.roomId}">${booking.room}</option>`)
                .val(booking.roomId);

            // Cập nhật thông tin vé
            selectedMovie.text(booking.movie);
            selectedDate.text(booking.date);
            selectedTime.text(booking.time);
            selectedRoom.text(booking.room);

            // Thiết lập IDLichChieu
            $('#IDLichChieu').val(booking.lichChieuId);

            // Enable nút tiếp tục ngay lập tức
            nextStepButton.prop('disabled', false);

            // Gọi API lấy sơ đồ ghế
            if (booking.lichChieuId && booking.roomId) {
                console.log("Loading seats with roomId:", booking.roomId);
                loadSeats(booking.roomId, booking.lichChieuId)
                    .then(function (success) {
                        if (success) {
                            nextStepButton.prop('disabled', false).trigger('click');
                        } else {
                            alert('Không thể tải sơ đồ ghế. Vui lòng thử lại.');
                        }
                    });
            }

            // Xóa dữ liệu sau khi sử dụng
            localStorage.removeItem('quickBooking');
        } catch (error) {
            console.error("Error processing QuickBooking data:", error);
        }
    }

    // Kiểm tra nút Tiếp Tục
    function checkContinueButton() {
        const isFormComplete =
            movieSelect.val() &&
            dateSelect.val() &&
            timeSelect.val() &&
            roomSelect.val();

        if (isFormComplete) {
            selectedMovie.text(movieSelect.val() || '-');
            selectedDate.text(dateSelect.val() || '-');
            selectedTime.text(timeSelect.val() || '-');
            selectedRoom.text(roomSelect.val() || '-');
        }

        nextStepButton.prop("disabled", !isFormComplete);

        if (!isFormComplete) {
            $("#ticket-count").text("0");
            $("#selected-seats").text("-");
            $("#total-price").text("0 VNĐ");
        }
    }

    // Hàm gọi API
    async function fetchData(url) {
        try {
            console.log("Calling API:", url);
            const response = await fetch(url);

            if (!response.ok) {
                throw new Error(await response.text() || "Server error");
            }

            const data = await response.text();
            console.log("API response received");
            return data;
        } catch (error) {
            console.error("API Error:", error);
            alert("Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại sau.");
            return null;
        }
    }

    // Tải tất cả mã lịch chiếu
    $.ajax({
        url: '/Booking/GetAllLichChieu',
        type: 'GET',
        success: function (data) {
            $('#lichChieuContainer').html(data);
        },
        error: function () {
            console.error("Error loading lich chieu");
        }
    });

    // Lựa chọn phim
    movieSelect.change(async function () {
        const selectedMovieValue = $(this).val();
        if (!selectedMovieValue) return;

        const dates = await fetchData(`/Booking/GetDates?movie=${encodeURIComponent(selectedMovieValue)}`);
        if (dates) {
            dateSelect
                .html('<option value="" hidden>Chọn Ngày</option>' + dates)
                .prop("disabled", false);
        }

        selectedMovie.text(selectedMovieValue);
        selectedDate.text('-');
        selectedRoom.text('-');
        selectedTime.text('-');
        $('#IDLichChieu').val("");

        checkContinueButton();
    });

    // Lựa chọn ngày
    dateSelect.change(async function () {
        const selectedMovieValue = movieSelect.val();
        const selectedDateValue = $(this).val();
        if (!selectedMovieValue || !selectedDateValue) return;

        const times = await fetchData(
            `/Booking/GetTimes?movie=${encodeURIComponent(selectedMovieValue)}&date=${encodeURIComponent(selectedDateValue)}`
        );
        if (times) {
            timeSelect
                .html('<option value="" hidden>Chọn Giờ</option>' + times)
                .prop("disabled", false);
        }

        selectedDate.text(selectedDateValue);
        selectedRoom.text('-');
        selectedTime.text('-');
        $('#IDLichChieu').val("");

        checkContinueButton();
    });

    // Lựa chọn giờ
    timeSelect.change(async function () {
        const selectedMovieValue = movieSelect.val();
        const selectedDateValue = dateSelect.val();
        const selectedTimeValue = $(this).val();
        if (!selectedMovieValue || !selectedDateValue || !selectedTimeValue) return;

        const rooms = await fetchData(
            `/Booking/GetRooms?movie=${encodeURIComponent(selectedMovieValue)}&date=${encodeURIComponent(selectedDateValue)}&time=${encodeURIComponent(selectedTimeValue)}`
        );
        if (rooms) {
            roomSelect
                .html('<option value="" hidden>Chọn Phòng</option>' + rooms)
                .prop("disabled", false);
        }

        selectedTime.text(selectedTimeValue);
        selectedRoom.text('-');
        $('#IDLichChieu').val("");

        checkContinueButton();
    });

    // Lựa chọn phòng
    roomSelect.change(async function () {
        const selectedOption = $(this).find(':selected');
        const selectedMovieValue = movieSelect.val();
        const selectedDateValue = dateSelect.val();
        const selectedTimeValue = timeSelect.val();
        const selectedRoomValue = $(this).val();

        if (!selectedMovieValue || !selectedDateValue || !selectedTimeValue || !selectedRoomValue) {
            return;
        }

        console.log("Selected values:", {
            movie: selectedMovieValue,
            date: selectedDateValue,
            time: selectedTimeValue,
            room: selectedRoomValue
        });

        const maLichChieu = $('#lichChieuContainer .lich-chieu-item').filter(function () {
            const matchMovie = $(this).data('ten-phim').trim() === selectedMovieValue.trim();
            const matchDate = $(this).data('ngay').trim() === selectedDateValue.trim();
            const matchTime = $(this).data('gio-chieu').trim() === selectedTimeValue.trim();
            const matchRoom = $(this).data('ma-phong').toString() === selectedRoomValue;

            console.log("Comparing:", {
                movie: { data: $(this).data('ten-phim'), selected: selectedMovieValue },
                date: { data: $(this).data('ngay'), selected: selectedDateValue },
                time: { data: $(this).data('gio-chieu'), selected: selectedTimeValue },
                room: { data: $(this).data('ma-phong'), selected: selectedRoomValue }
            });

            return matchMovie && matchDate && matchTime && matchRoom;
        }).data('ma-lich-chieu');

        if (maLichChieu) {
            $("#IDLichChieu").val(maLichChieu);
            console.log("Mã lịch chiếu:", maLichChieu);

            const seats = await fetchData(`/Booking/GetSeats?room=${selectedRoomValue}&lichChieuId=${maLichChieu}`);
            if (seats) {
                $("#seat-selection").html(seats);
            }
        } else {
            console.error("Không tìm thấy mã lịch chiếu với các giá trị:", {
                movie: selectedMovieValue,
                date: selectedDateValue,
                time: selectedTimeValue,
                room: selectedRoomValue
            });
            alert("Không tìm thấy lịch chiếu phù hợp.");
        }

        selectedRoom.text(selectedRoomValue);
        checkContinueButton();
    });

    // Điều hướng các bước
    nextStepButton.click(function () {
        bookingForm.hide();
        seatSelectionContainer.show();
    });

    $("#back").click(function () {
        seatSelectionContainer.hide();
        bookingForm.show();
    });

    // Xử lý chọn ghế
    $(document).on("click", ".seat:not(.occupied)", function () {
        const seatId = $(this).data('id');
        const seatName = $(this).data('name');

        if ($(this).hasClass("selected")) {
            $(this).removeClass("selected");
            selectedSeats = selectedSeats.filter(seat => seat.id !== seatId);
        } else {
            $(this).addClass("selected");
            selectedSeats.push({ id: seatId, name: seatName });
        }

        updateTicketInfo();
        $("#confirm-booking").prop("disabled", selectedSeats.length === 0);
    });

    // Cập nhật thông tin vé
    function updateTicketInfo() {
        $("#selected-seats").text(selectedSeats.map(seat => seat.name).join(", "));
        $("#ticket-count").text(selectedSeats.length);
        $("#total-price").text((selectedSeats.length * ticketPrice).toLocaleString() + " VNĐ");
    }

    // Xác nhận đặt vé
    $("#confirm-booking").click(function (e) {
        e.preventDefault();

        const idLichChieu = $("#IDLichChieu").val();
        const idKhachHang = $("#maKH").val();
        const tongTien = selectedSeats.length * ticketPrice;

        const formData = {
            IDLichChieu: parseInt(idLichChieu),
            IDKhachHang: idKhachHang,
            TongTien: tongTien,
            TenPhim: selectedMovie.text(),
            TenPhong: selectedRoom.text(),
            Ngay: selectedDate.text(),
            GioChieu: selectedTime.text(),
            ChonGhe: selectedSeats.map(seat => ({
                IDGhe: seat.id,
                TenGhe: seat.name
            }))
        };

        $.ajax({
            url: '/Booking/ConfirmBooking',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function (response) {
                if (response.redirectUrl) {
                    window.location.href = response.redirectUrl;
                } else if (response.error) {
                    alert(response.error);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error:', error);
                alert('Có lỗi xảy ra khi xử lý đặt vé!');
            }
        });
    });
});