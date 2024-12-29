using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class BookingViewModel
    {
        public int? IDVe { get; set; }
        public IEnumerable<string> Phims { get; set; }
        public IEnumerable<string> PhongChieus { get; set; }
        public IEnumerable<string> Ngays { get; set; }
        public IEnumerable<string> GioChieux { get; set; }
        public int SoLuongVe { get; set; }
        public decimal TongTien { get; set; }
        public string ChoNgoi { get; set; }
        public int? SelectedPhong { get; set; }
        public int IDKhachHang { get; set; }
        public int IDLichChieu { get; set; }
        public decimal GiaVe { get; set; }
        public int RewardPoints { get; set; }
        public TimeSpan GioChieu { get; set; }
        public DateTime NgayChieu { get; set; }
        public List<GheViewModel> DanhSachGhe { get; set; }
        public string TenPhim { get; set; }
        public string TenPhong { get; set; }

    }
}
