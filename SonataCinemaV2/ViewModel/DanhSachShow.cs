using SonataCinemaV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class DanhSachShow
    {
        public IEnumerable<Phim> Phim { get; set; }
        public IEnumerable<NhanVien> nhanViens { get; set; }
        public IEnumerable<PhongChieu> phongChieu { get; set; }
        public IEnumerable<KhachHang> khachHang { get; set; }
    }
}