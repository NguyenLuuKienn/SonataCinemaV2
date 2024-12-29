using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class PhimMoi
    {
        public int IDPhim { get; set; }
        [Required(ErrorMessage = "Tên phim không được để trống")]
        public string TenPhim { get; set; }

        [Required(ErrorMessage = "Thể loại không được để trống")]
        public string TheLoai { get; set; }

        [Required(ErrorMessage = "Thời lượng không được để trống")]
        public int ThoiLuong { get; set; }

        public string TrangThai { get; set; }

        public byte NoiBat { get; set; }

        [NotMapped]
        public HttpPostedFileBase Poster { get; set; }

        [NotMapped]
        public HttpPostedFileBase Banner { get; set; }

        public string TenPoster { get; set; }
        public string TenBanner { get; set; }

        public string MoTa { get; set; }
        public string Trailer { get; set; }
    }
}