document.addEventListener('DOMContentLoaded', function () {
    // Lưu thông tin đã chọn
    let selectedBooking = {
        movie: '',
        date: '',
        time: '',
        room: ''
    };

    // Xử lý chọn phim
    $('#quickMovieSelect').change(function () {
        selectedBooking.movie = $(this).val();
        if (selectedBooking.movie) {
            $('#quickDateSelect').prop('disabled', false);
        } else {
            resetSelections('movie');
        }
        checkEnableButton();
    });

    // Xử lý chọn ngày
    $('#quickDateSelect').change(function () {
        selectedBooking.date = $(this).val();
        if (selectedBooking.date) {
            $('#quickTimeSelect').prop('disabled', false);
        } else {
            resetSelections('date');
        }
        checkEnableButton();
    });

    // Xử lý chọn giờ
    $('#quickTimeSelect').change(function () {
        selectedBooking.time = $(this).val();
        if (selectedBooking.time) {
            $('#quickRoomSelect').prop('disabled', false);
        } else {
            resetSelections('time');
        }
        checkEnableButton();
    });

    // Xử lý chọn phòng
    $('#quickRoomSelect').change(function () {
        selectedBooking.room = $(this).val();
        checkEnableButton();
    });

    // Xử lý nút đặt vé
    $('#quickBookBtn').click(function () {
        // Chuyển đến trang BookingTicket và tự động điền thông tin
        window.location.href = '/Booking/BookingTicket';

        // Lưu thông tin vào localStorage để sử dụng ở trang BookingTicket
        localStorage.setItem('quickBooking', JSON.stringify(selectedBooking));
    });

    // Reset các lựa chọn
    function resetSelections(from) {
        switch (from) {
            case 'movie':
                $('#quickDateSelect').prop('disabled', true).val('');
                $('#quickTimeSelect').prop('disabled', true).val('');
                $('#quickRoomSelect').prop('disabled', true).val('');
                selectedBooking.date = '';
                selectedBooking.time = '';
                selectedBooking.room = '';
                break;
            case 'date':
                $('#quickTimeSelect').prop('disabled', true).val('');
                $('#quickRoomSelect').prop('disabled', true).val('');
                selectedBooking.time = '';
                selectedBooking.room = '';
                break;
            case 'time':
                $('#quickRoomSelect').prop('disabled', true).val('');
                selectedBooking.room = '';
                break;
        }
    }

    // Kiểm tra để enable/disable nút đặt vé
    function checkEnableButton() {
        const isComplete = selectedBooking.movie &&
            selectedBooking.date &&
            selectedBooking.time &&
            selectedBooking.room;
        $('#quickBookBtn').prop('disabled', !isComplete);
    }
});