using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class LichChieuViewModel
    {
        public int IDLichChieu { get; set; }
        public string TenPhim { get; set; }
        public string Ngay { get; set; }
        public int IDPhong { get; set; }
        public string GioChieu { get; set; }
    }
}