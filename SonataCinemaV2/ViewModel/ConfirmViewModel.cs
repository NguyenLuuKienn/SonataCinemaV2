using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class ConfirmViewModel
    {
        [Required]
        public int IDLichChieu { get; set; }
        public int IDKhachHang { get; set; }
        public decimal GiaVe {  get; set; }
        public decimal TongTien { get; set; }
        public decimal TongTienCombo { get; set; }
        public decimal TongTienVe { get; set; }
        public string TenPhim { get; set; }
        public string TenPhong { get; set; }
        public string Ngay { get; set; }
        public string GioChieu { get; set; }
        public List<GheViewModel> ChonGhe { get; set; }
        public List<ComboOrderViewModel> Combos { get; set; }
        public ConfirmViewModel()
        {
            ChonGhe = new List<GheViewModel>();
        }
    }
}

