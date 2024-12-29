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
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
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
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập Lại Mật Khẩu")]
        public string NhapLaiMatKhau { get; set; }
    }
}