using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class BookingTicket
    {
        public int? IdVe { get; set; }
        public IEnumerable<string> Phims { get; set; }
        public IEnumerable<string> PhongChieus { get; set; } 
        public IEnumerable<string> Ngays { get; set; } 
        public IEnumerable<string> GioChieux { get; set; } 
        public int SoLuongVe { get; set; }
        public decimal TongTien { get; set; }
        public List<string> GheDaChon { get; set; }
    }
}