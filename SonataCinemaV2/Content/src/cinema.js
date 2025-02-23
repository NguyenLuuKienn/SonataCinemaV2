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
        $.ajax({
            url: this.action,
            type: this.method,
            data: $(this).serialize(),
            success: function (response) {
                if (response.success) {
                    if (response.redirectUrl) {
                        window.location.href = response.redirectUrl;
                    } else {
                        window.location.href = '/';
                    }
                } else {
                    if (typeof response === 'string') {
                        $('#modalRegister').html(response);
                    } else {
                        alert('Đăng ký thất bại. Vui lòng thử lại.');
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Error:', error);
                alert('Có lỗi xảy ra trong quá trình đăng ký.');
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