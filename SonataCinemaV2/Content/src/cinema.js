// Giữ nguyên các function chọn phim, rạp, ngày và giờ
function chooseMovie(movie) {
    console.log("Movie chosen: ", movie);
    document.getElementById('selectedMovie').innerText = movie;
    document.getElementById('selectedMovieText').innerText = movie;
    document.getElementById('cinemaDropdown').disabled = false;
}

function chooseCinema(cinema) {
    document.getElementById('selectedCinema').innerText = cinema;
    document.getElementById('selectedCinemaText').innerText = cinema;
    document.getElementById('dayDropdown').disabled = false;
}

function chooseDay(day) {
    document.getElementById('selectedDay').innerText = day;
    document.getElementById('selectedDayText').innerText = day;
    document.getElementById('showtimeDropdown').disabled = false;
}

function chooseShowtime(showtime) {
    document.getElementById('selectedShowtime').innerText = showtime;
    document.getElementById('selectedShowtimeText').innerText = showtime;
}

// Giữ nguyên các function hiển thị/ẩn phim
function showMoreMovies() {
    var extraMovies = document.getElementById("extraMovies");
    var extraMovies2 = document.getElementById("extraMovies2");
    var viewMoreBtn = document.getElementById("viewMoreBtn");
    var collapseBtn = document.getElementById("collapseBtn");

    extraMovies.style.display = "flex";
    extraMovies2.style.display = "flex";
    viewMoreBtn.style.display = "none";
    collapseBtn.style.display = "inline-block";
}

function collapseMovies() {
    var extraMovies = document.getElementById("extraMovies");
    var extraMovies2 = document.getElementById("extraMovies2");
    var viewMoreBtn = document.getElementById("viewMoreBtn");
    var collapseBtn = document.getElementById("collapseBtn");

    extraMovies.style.display = "none";
    extraMovies2.style.display = "none";
    viewMoreBtn.style.display = "inline-block";
    collapseBtn.style.display = "none";
}

// Xử lý filter và sort
$(document).ready(function () {
    // Xử lý lọc phim
    $(document).on('click', '.category-links .sub-link', function (e) {
        e.preventDefault();
        $('.category-links .sub-link').removeClass('active');
        $(this).addClass('active');

        const filter = $(this).data('filter');
        console.log('Filter:', filter);

        if (filter === 'all') {
            $('.movie-card').fadeIn();
        } else {
            $('.movie-card').each(function () {
                const status = $(this).data('status');
                if (status === filter) {
                    $(this).fadeIn();
                } else {
                    $(this).fadeOut();
                }
            });
        }
    });

    // Xử lý sắp xếp phim
    $(document).on('change', '.form-select', function () {
        const sortBy = $(this).val();
        const $moviesGrid = $('.movies-grid');
        const $movies = $('.movie-card').toArray();

        switch (sortBy) {
            case 'name':
                $movies.sort((a, b) => {
                    const titleA = $(a).find('.movie-title').text().toLowerCase();
                    const titleB = $(b).find('.movie-title').text().toLowerCase();
                    return titleA.localeCompare(titleB);
                });
                break;

            case 'rating':
                $movies.sort((a, b) => {
                    const ratingA = parseFloat($(a).data('rating'));
                    const ratingB = parseFloat($(b).data('rating'));
                    return ratingB - ratingA;
                });
                break;

            case 'newest':
                $movies.sort((a, b) => {
                    const idA = parseInt($(a).data('id'));
                    const idB = parseInt($(b).data('id'));
                    return idB - idA;
                });
                break;
        }

        if (sortBy) {
            $moviesGrid.fadeOut(300, function () {
                $moviesGrid.empty();
                $movies.forEach(movie => $moviesGrid.append(movie));
                $moviesGrid.fadeIn(300);
            });
        }
    });
});

// Xử lý modal login/register
$(document).ready(function () {
    // Show login modal
    $(document).on('click', '#btnShowLogin', function () {
        $('#blurOverlay').show();
        $('#modalLogin').show();
    });

    // Hide modals when clicking overlay
    $(document).on('click', '#blurOverlay', function () {
        $('#blurOverlay').hide();
        $('#modalLogin').hide();
        $('#modalRegister').hide();
    });

    // Switch to register form
    $(document).on('click', '#btnSwitchToRegister', function () {
        $('#modalLogin').hide();
        $('#modalRegister').show();
    });

    // Switch to login form
    $(document).on('click', '#btnSwitchToLogin', function () {
        $('#modalRegister').hide();
        $('#modalLogin').show();
    });

    // Handle login form submission
    $(document).on('submit', '#loginForm', function (e) {
        e.preventDefault();
        $.ajax({
            url: this.action,
            type: this.method,
            data: $(this).serialize(),
            success: function (response) {
                if (response.success) {
                    // Thêm kiểm tra
                    if (response.redirectUrl) {
                        window.location.href = response.redirectUrl;
                    } else {
                        window.location.href = '/'; // URL mặc định
                    }
                } else {
                    // Xử lý khi response là partial view
                    if (typeof response === 'string') {
                        $('#loginPartialContainer').html(response);
                    } else {
                        alert('Đăng nhập thất bại. Vui lòng thử lại.');
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Error:', error);
                alert('Có lỗi xảy ra trong quá trình đăng nhập.');
            }
        });
    });

    // Handle register form submission
    $(document).on('submit', '#registerForm', function (e) {
        e.preventDefault();
        
        // Reset validation messages
        $('.text-danger').empty();
        
        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: $(this).serialize(),
            success: function(response) {
                console.log('Server Response:', response);
                
                if (response.success) {
                    Swal.fire({
                        title: 'Đăng Ký Thành Công!',
                        text: 'Chào mừng bạn đến với Sonata Cinema',
                        icon: 'success',
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#df9a2c'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            window.location.href = response.redirectUrl;
                        }
                    });
                } else {
                    if (response.errors) {
                        Object.keys(response.errors).forEach(function(key) {
                            $(`[data-valmsg-for="${key}"]`).text(response.errors[key]);
                        });
                    }
                    
                    Swal.fire({
                        title: 'Lỗi!',
                        text: 'Vui lòng kiểm tra lại thông tin đăng ký',
                        icon: 'error',
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#df9a2c'
                    });
                }
            },
            error: function(xhr, status, error) {
                console.error('AJAX Error:', error);
                console.error('Response Text:', xhr.responseText);
                
                Swal.fire({
                    title: 'Lỗi!',
                    text: 'Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại.',
                    icon: 'error',
                    confirmButtonText: 'OK',
                    confirmButtonColor: '#df9a2c'
                });
            }
        });
    });
    });

// xem mật khẩu
function togglePassword() {
    const passwordInput = document.getElementById('passwordInput');
    const toggleIcon = document.getElementById('togglePassword');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.classList.remove('fa-eye');
        toggleIcon.classList.add('fa-eye-slash');
    } else {
        passwordInput.type = 'password';
        toggleIcon.classList.remove('fa-eye-slash');
        toggleIcon.classList.add('fa-eye');
    }
}
$(document).on('submit', '#forgotPasswordForm', function (e) {
    e.preventDefault();
    console.log('Submitting forgot password form...'); // Debug log

    var form = $(this);
    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            console.log('Form submission response:', response); // Debug log
            if (response.success) {
                Swal.fire({
                    title: 'Thành công!',
                    text: 'Vui lòng kiểm tra email của bạn để nhận hướng dẫn đặt lại mật khẩu.',
                    icon: 'success',
                    confirmButtonText: 'Đóng',
                    confirmButtonColor: '#df9a2c'
                }).then((result) => {
                    $('#loginModal').modal('hide');
                });
            } else {
                $('#loginModal .modal-body').html(response);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error submitting form:', error); // Debug log
            Swal.fire({
                title: 'Lỗi!',
                text: 'Đã có lỗi xảy ra. Vui lòng thử lại sau.',
                icon: 'error',
                confirmButtonText: 'Đóng',
                confirmButtonColor: '#df9a2c'
            });
        }
    });
});


function showLoginForm() {
    $.ajax({
        url: '/Account/LoginPartial',
        type: 'GET',
        success: function (result) {
            $('#loginModal .modal-body').html(result);
            $('#loginModal .modal-title').text('Đăng nhập');
            // Khởi tạo lại các event handlers
            initializeLoginValidation();
        }
    });
}
$(document).ready(function () {
    // Xử lý click nút quên mật khẩu
    $(document).on('click', '#btnForgotPassword', function (e) {
        e.preventDefault();
        console.log('Forgot password button clicked');

        // Ẩn modal login
        $('#modalLogin').hide();

        // Hiển thị overlay và modal quên mật khẩu
        $('#blurOverlay').show();

        // Load form quên mật khẩu
        $.ajax({
            url: '/Account/ForgotPasswordPartial',
            type: 'GET',
            success: function (result) {
                $('#forgotPasswordPartialContainer').html(result);
                $('#modalForgotPassword').show();
            },
            error: function (xhr, status, error) {
                console.error('Error loading forgot password form:', error);
            }
        });
    });


    // Xử lý nút quay lại đăng nhập
    $(document).on('click', '#btnBackToLogin', function (e) {
        e.preventDefault();
        $('#modalForgotPassword').hide();
        $('#modalLogin').show();
    });
});
