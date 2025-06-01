using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class Register
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [Display(Name = "Họ và Tên")]
        public string TenKhachHang { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đủ 10 số")]
        [Display(Name = "Số Điện Thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Bạn cần chọn giới tính")]
        [Display(Name = "Giới Tính")]
        public string GioiTinh { get; set; }

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        [Display(Name = "Ngày Sinh")]
        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật Khẩu")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z]).{8,}$",
        ErrorMessage = "Mật khẩu phải chứa ít nhất một chữ hoa và một chữ thường")]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập Lại Mật Khẩu")]
        public string NhapLaiMatKhau { get; set; }

        // Thêm các thuộc tính cho xác thực email
        [Display(Name = "Mã xác thực")]
        public string MaXacThuc { get; set; }

        public bool IsVerifying { get; set; } = false;
    }
}