document.addEventListener('DOMContentLoaded', function () {
    // Lưu thông tin đã chọn
    let selectedBooking = {
        movie: '',
        date: '',
        time: '',
        room: '',
        lichChieuId: ''
    };

    // Xử lý chọn phim
    $('#quickMovieSelect').change(function () {
        const movie = $(this).val();
        selectedBooking.movie = movie;
        resetSelections('movie');

        if (movie) {
            // Load danh sách ngày chiếu
            $.getJSON('/QuickBooking/GetNgayChieu', { tenPhim: movie }, function (dates) {
                const dateSelect = $('#quickDateSelect');
                dateSelect.html('<option value="">Chọn Ngày</option>');
                dates.forEach(date => {
                    dateSelect.append(`<option value="${date}">${date}</option>`);
                });
                dateSelect.prop('disabled', false);
            });
        }
    });

    // Xử lý chọn ngày
    $('#quickDateSelect').change(function () {
        const date = $(this).val();
        selectedBooking.date = date;
        resetSelections('date');

        if (date) {
            // Load danh sách giờ chiếu
            $.getJSON('/QuickBooking/GetGioChieu', {
                tenPhim: selectedBooking.movie,
                ngayChieu: date
            }, function (times) {
                const timeSelect = $('#quickTimeSelect');
                timeSelect.html('<option value="">Chọn Giờ</option>');
                times.forEach(time => {
                    timeSelect.append(`<option value="${time}">${time}</option>`);
                });
                timeSelect.prop('disabled', false);
            });
        }
    });

    // Xử lý chọn giờ
    $('#quickTimeSelect').change(function () {
        const time = $(this).val();
        selectedBooking.time = time;
        resetSelections('time');

        if (time) {
            $.getJSON('/QuickBooking/GetPhongChieu', {
                tenPhim: selectedBooking.movie,
                ngayChieu: selectedBooking.date,
                gioChieu: time
            }, function (rooms) {
                const roomSelect = $('#quickRoomSelect');
                roomSelect.html('<option value="">Chọn Phòng</option>');
                rooms.forEach(room => {
                    roomSelect.append(`
                    <option value="${room.ID_PhongChieu}" 
                            data-room-id="${room.ID_PhongChieu}"
                            data-room-name="${room.TenPhong}"
                            data-lichchieu="${room.IDLichChieu}">
                        ${room.TenPhong}
                    </option>
                `);
                });
                roomSelect.prop('disabled', false);
            });
        }
    });

    // Xử lý chọn phòng
    $('#quickRoomSelect').change(function () {
        const selectedOption = $(this).find(':selected');
        selectedBooking = {
            ...selectedBooking,
            roomId: parseInt(selectedOption.data('room-id')),     // ID phòng
            room: selectedOption.data('room-name'),               // Tên phòng
            lichChieuId: parseInt(selectedOption.data('lichchieu'))
        };
        checkEnableButton();
    });

    // Khi render options cho phòng chiếu
    function renderRoomOptions(data) {
        let options = '<option value="" hidden>Chọn Phòng</option>';
        data.forEach(room => {
            options += `<option value="${room.TenPhong}" data-lichchieu="${room.IDLichChieu}">${room.TenPhong}</option>`;
        });
        $('#quickRoomSelect').html(options).prop('disabled', false);
    }

    // Xử lý nút đặt vé
    $('#quickBookingBtn, #quickBookBtn').click(function () {
        console.log("Booking button clicked");
        console.log("Current selectedBooking:", selectedBooking);

        if (!selectedBooking.lichChieuId) {
            console.error("lichChieuId is missing!");
            alert('Không tìm thấy mã lịch chiếu. Vui lòng thử lại.');
            return;
        }

        try {
            // Lưu vào localStorage
            const bookingData = JSON.stringify(selectedBooking);
            console.log("Saving to localStorage:", bookingData);
            localStorage.setItem('quickBooking', bookingData);

            // Verify saved data
            const savedData = localStorage.getItem('quickBooking');
            const parsedData = JSON.parse(savedData);
            console.log("Verified saved data:", parsedData);

            // Chuyển trang
            window.location.href = '/Booking/BookingTicket';
        } catch (error) {
            console.error("Error saving booking data:", error);
            alert('Có lỗi xảy ra khi lưu thông tin đặt vé.');
        }
    });


    // Kiểm tra để enable/disable nút đặt vé
    function checkEnableButton() {
        const isComplete = selectedBooking.movie &&
            selectedBooking.date &&
            selectedBooking.time &&
            selectedBooking.room &&
            selectedBooking.lichChieuId; // Thêm điều kiện kiểm tra lichChieuId

        console.log("Form completion check:", {
            movie: selectedBooking.movie,
            date: selectedBooking.date,
            time: selectedBooking.time,
            room: selectedBooking.room,
            lichChieuId: selectedBooking.lichChieuId
        });

        // Enable/disable cả hai nút nếu có
        $('#quickBookBtn, #quickBookingBtn').prop('disabled', !isComplete);
    }

    // Reset các lựa chọn
    function resetSelections(from) {
        console.log("Resetting selections from:", from);

        switch (from) {
            case 'movie':
                $('#quickDateSelect').prop('disabled', true).val('');
                $('#quickTimeSelect').prop('disabled', true).val('');
                $('#quickRoomSelect').prop('disabled', true).val('');
                selectedBooking.date = '';
                selectedBooking.time = '';
                selectedBooking.room = '';
                selectedBooking.lichChieuId = '';
                break;
            case 'date':
                $('#quickTimeSelect').prop('disabled', true).val('');
                $('#quickRoomSelect').prop('disabled', true).val('');
                selectedBooking.time = '';
                selectedBooking.room = '';
                selectedBooking.lichChieuId = '';
                break;
            case 'time':
                $('#quickRoomSelect').prop('disabled', true).val('');
                selectedBooking.room = '';
                selectedBooking.lichChieuId = '';
                break;
        }

        console.log("After reset - selectedBooking:", selectedBooking);
        checkEnableButton();
    }
});