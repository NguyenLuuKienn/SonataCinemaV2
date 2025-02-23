using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class LichChieuMoi
    {
        public int IDPhim { get; set; }
        public int IDPhong { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public string GioChieu { get; set; }
        public decimal GiaVe { get; set; }
        public string TrangThai { get; set; }
    }
}