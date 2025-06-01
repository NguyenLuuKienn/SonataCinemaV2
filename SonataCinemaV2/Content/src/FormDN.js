
document.getElementById('customLoginBtn').addEventListener('click', function () {
    document.getElementById('customLoginModal').style.display = 'flex'; 
});


document.querySelector('.custom-close-modal').addEventListener('click', function () {
    document.getElementById('customLoginModal').style.display = 'none';
});



window.addEventListener('click', function (event) {
    var modal = document.getElementById('customLoginModal');
    if (event.target == modal) {
        modal.style.display = 'none'; 
    }
});



$(document).ready(function () {
    $("#showRegisterForm").click(function () {
        $("#formContainer").load('@Url.Action("DangKy", "Account")'); 
    });

$("#showLoginForm").click(function () {
    $("#formContainer").load('@Url.Action("DangNhap", "Account")'); 
    });
});
function(response) {
    if (response.success) {
        window.location.href = response.redirectUrl;
    } else if (response.requireVerification) {
        // Chuyển đến trang xác thực
        Swal.fire({
            icon: 'info',
            title: 'Cần xác thực email',
            text: response.message,
            showConfirmButton: true,
            confirmButtonText: 'Xác thực ngay'
        }).then(function () {
            window.location.href = response.redirectUrl;
        });
    } else {
        // Hiển thị lỗi đăng nhập
        Swal.fire({
            icon: 'error',
            title: 'Đăng nhập thất bại',
            text: response.message
        });
    }
}

// Add shake animation CSS
$('<style>')
    .prop('type', 'text/css')
    .html(`
            .shake {
                animation: shake 0.6s ease-in-out;
            }
            @keyframes shake {
                0%, 100% { transform: translateX(0); }
                25% { transform: translateX(-5px); }
                75% { transform: translateX(5px); }
            }
        `)
    .appendTo('head');






