
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








