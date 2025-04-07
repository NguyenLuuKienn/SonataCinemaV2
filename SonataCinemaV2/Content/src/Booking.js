$(document).ready(function () {
    let selectedSeats = [];
    let allLichChieu = [];
    let currentTicketPrice = 0;
    let selectedCombos = [];
    let comboTotalPrice = 0;

    const movieSelect = $("#movieSelect");
    const dateSelect = $("#dateSelect");
    const roomSelect = $("#roomSelect");
    const timeSelect = $("#timeSelect");

    const nextStepButton = $("#next-step");
    const seatSelectionContainer = $("#seatSelectionContainer");
    const comboSelectionContainer = $("#comboSelectionContainer");
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

    function loadCombos() {
        $.ajax({
            url: '/Booking/GetCombos',
            type: 'GET',
            success: function (response) {
                $('#combo-list').html(response);
            },
            error: function (xhr, status, error) {
                console.error('Lỗi khi tải danh sách combo:', error);
                Swal.fire('Lỗi', 'Không thể tải danh sách combo', 'error');
            }
        });
    }
    // Xử lý sự kiện khi click nút +/- combo
    $(document).on('click', '.btn-quantity', function () {
        const button = $(this);
        const comboItem = button.closest('.combo-item');
        const input = comboItem.find('input');
        const currentValue = parseInt(input.val());

        if (button.hasClass('plus') && currentValue < 10) {
            input.val(currentValue + 1);
            comboItem.find('.minus').prop('disabled', false);
            if (currentValue + 1 === 10) {
                button.prop('disabled', true);
            }
        } else if (button.hasClass('minus') && currentValue > 0) {
            input.val(currentValue - 1);
            comboItem.find('.plus').prop('disabled', false);
            if (currentValue - 1 === 0) {
                button.prop('disabled', true);
            }
        }

        updateComboSummary();
    });

    // Cập nhật thông tin combo đã chọn
    function updateComboSummary() {
        selectedCombos = [];
        comboTotalPrice = 0;

        $('.combo-item').each(function () {
            const quantity = parseInt($(this).find('input').val());
            if (quantity > 0) {
                const comboId = $(this).data('id');
                const comboName = $(this).find('.combo-name').text();
                const comboPrice = parseInt($(this).find('.combo-price').text().replace(/[^\d]/g, ''));

                selectedCombos.push({
                    id: comboId,
                    name: comboName,
                    quantity: quantity,
                    price: comboPrice
                });

                comboTotalPrice += quantity * comboPrice;
            }
        });

        // Cập nhật hiển thị combo đã chọn
        const selectedCombosList = $('#selected-combos-list');
        selectedCombosList.empty();

        if (selectedCombos.length > 0) {
            selectedCombos.forEach(combo => {
                selectedCombosList.append(`
                <div class="selected-combo-item">
                    <span>${combo.name} x ${combo.quantity}</span>
                    <span>${new Intl.NumberFormat('vi-VN', {
                    style: 'currency',
                    currency: 'VND'
                }).format(combo.price * combo.quantity)}</span>
                </div>
            `);
            });

            $('#selected-combo-summary').text(
                selectedCombos.map(c => `${c.name} x${c.quantity}`).join(', ')
            );
        } else {
            $('#selected-combo-summary').text('-');
        }

        // Cập nhật tổng tiền combo
        $('#combo-total-price, #combo-total').text(
            new Intl.NumberFormat('vi-VN', {
                style: 'currency',
                currency: 'VND'
            }).format(comboTotalPrice)
        );

        updateTotalPrice();
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
            if (booking.lichChieuId) {
                updateTicketPrice(booking.lichChieuId);
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
            alert("Phim chưa có lịch chiếu.");
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

            updateTicketPrice(maLichChieu);
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
    // Nút quay lại từ chọn ghế về form đặt vé
    $("#back-to-booking").click(function () {
        seatSelectionContainer.hide();
        bookingForm.show();
    });

    // Nút tiếp tục từ chọn ghế đến chọn combo
    $("#next-to-combo").click(function () {
        if (selectedSeats.length === 0) {
            Swal.fire('Thông báo', 'Vui lòng chọn ít nhất một ghế', 'warning');
            return;
        }
        seatSelectionContainer.hide();
        comboSelectionContainer.show();
        loadCombos();
    });

    // Nút quay lại từ chọn combo về chọn ghế
    $("#back-to-seats").click(function () {
        comboSelectionContainer.hide();
        seatSelectionContainer.show();
    });
    $("#back").click(function () {
        seatSelectionContainer.hide();
        bookingForm.show();
    });

    // Xử lý chọn ghế
    $(document).on("click", ".seat:not(.occupied):not(.holding)", function () {
        const seatId = $(this).data('id');
        const seatName = $(this).data('name');

        if ($(this).hasClass("selected")) {
            $(this).removeClass("selected");
            selectedSeats = selectedSeats.filter(seat => seat.id !== seatId);
        } else {
            $(this).addClass("selected");
            selectedSeats.push({ id: seatId, name: seatName });
        }
        $("#selected-seats").text(selectedSeats.map(seat => seat.name).join(", "));
        $("#ticket-count").text(selectedSeats.length);

        updateTicketInfo();
        $("#next-to-combo").prop("disabled", selectedSeats.length === 0);
    });
    // lấy giá vé
    function updateTicketPrice(lichChieuId) {
        $.ajax({
            url: '/Booking/GetTicketPrice',
            type: 'GET',
            data: { lichChieuId: lichChieuId },
            success: function (price) {
                currentTicketPrice = price;
                console.log('Giá vé:', currentTicketPrice); // Debug
                updateTotalPrice();
            },
            error: function (xhr, status, error) {
                console.error('Lỗi khi lấy giá vé:', error);
                currentTicketPrice = 0;
            }
        });
    }
    // Hàm cập nhật tổng tiền
    function updateTotalPrice() {
        const ticketTotal = selectedSeats.length * currentTicketPrice;
        const totalPrice = ticketTotal + comboTotalPrice;

        // Cập nhật hiển thị tổng tiền vé
        $('#ticket-total').text(
            new Intl.NumberFormat('vi-VN', {
                style: 'currency',
                currency: 'VND'
            }).format(ticketTotal)
        );
        // Cập nhật tổng tiền combo
        $('#combo-total').text(
            new Intl.NumberFormat('vi-VN', {
                style: 'currency',
                currency: 'VND'
            }).format(comboTotalPrice)
        );
        // Cập nhật tổng tiền cuối cùng
        $('#total-price').text(
            new Intl.NumberFormat('vi-VN', {
                style: 'currency',
                currency: 'VND'
            }).format(totalPrice)
        );
    }


    // Cập nhật thông tin vé
    function updateTicketInfo() {
        $("#selected-seats").text(selectedSeats.map(seat => seat.name).join(", "));
        $("#ticket-count").text(selectedSeats.length);
        const totalPrice = selectedSeats.length * currentTicketPrice;
        $("#total-price").text(new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(totalPrice));
    }

    // Xác nhận đặt vé
    // Thêm debug logs
    $("#bookingForm").on("submit", function (e) {
        e.preventDefault();
        console.log("Form submitted");

        const idLichChieu = $("#IDLichChieu").val();
        const idKhachHang = $("#maKH").val();
        const tongTienVe = selectedSeats.length * currentTicketPrice;

        console.log("Form data:", {
            idLichChieu,
            idKhachHang,
            selectedSeats,
            selectedCombos,
            tongTienVe,
            comboTotalPrice
        });

        // Tạo form data
        const formData = {
            IDLichChieu: parseInt(idLichChieu),
            IDKhachHang: idKhachHang.trim(),
            GiaVe: currentTicketPrice,
            TongTienVe: tongTienVe,
            TongTienCombo: comboTotalPrice,
            TongTien: tongTienVe + comboTotalPrice,
            ChonGhe: selectedSeats.map(seat => ({
                IDGhe: seat.id,
                TenGhe: seat.name
            })),
            Combos: selectedCombos.map(combo => ({
                IDCombo: combo.id,
                TenCombo: combo.name,
                SoLuong: combo.quantity,
                Gia: combo.price
            }))
        };


        console.log("Sending formData:", formData);

        // Gọi API giữ ghế và xử lý đặt vé
        $.ajax({
            url: '/Booking/HoldSelectedSeats',
            type: 'POST',
            data: {
                lichChieuId: parseInt(idLichChieu),
                selectedSeatIds: selectedSeats.map(seat => seat.id)
            },
            success: function (holdResponse) {
                console.log("HoldSelectedSeats response:", holdResponse);
                if (holdResponse.success) {
                    $.ajax({
                        url: '/Booking/ConfirmBooking',
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify(formData),
                        success: function (response) {
                            console.log("ConfirmBooking response:", response);
                            if (response.success && response.redirectUrl) {
                                window.location.href = response.redirectUrl;
                            } else {
                                releaseHeldSeats(idLichChieu, selectedSeats.map(seat => seat.id));
                                Swal.fire('Lỗi!', response.error || 'Có lỗi xảy ra khi xử lý đặt vé', 'error');
                            }
                        },
                        error: function (xhr, status, error) {
                            console.error("Booking error:", { xhr, status, error });
                            releaseHeldSeats(idLichChieu, selectedSeats.map(seat => seat.id));
                            Swal.fire('Lỗi!', 'Có lỗi xảy ra khi xử lý đặt vé!', 'error');
                        }
                    });
                } else {
                    Swal.fire('Lỗi!', 'Không thể giữ ghế: ' + (holdResponse.message || 'Có lỗi xảy ra'), 'error');
                }
            },
            error: function (xhr, status, error) {
                console.error("Hold seats error:", { xhr, status, error });
                Swal.fire('Lỗi!', 'Có lỗi xảy ra khi giữ ghế', 'error');
            }
        });
    });


    // Thêm hàm giải phóng ghế
    function releaseHeldSeats(lichChieuId, seatIds) {
        $.ajax({
            url: '/Booking/ReleaseSeat',
            type: 'POST',
            data: {
                lichChieuId: lichChieuId,
                gheIds: seatIds
            },
            error: function (xhr, status, error) {
                console.error('Error releasing seats:', error);
            }
        });
    }

    // Thêm hàm refresh ghế định kỳ
    function refreshSeats() {
        const roomId = roomSelect.val();
        const lichChieuId = $("#IDLichChieu").val();

        if (roomId && lichChieuId) {
            loadSeats(roomId, lichChieuId);
        }
    }

    // Refresh ghế mỗi 30 giây
    setInterval(refreshSeats, 30000);
});