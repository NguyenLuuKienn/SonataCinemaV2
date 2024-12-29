using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SonataCinemaV2.ViewModel
{
    public class QuickBookingViewModel
    {
        public List<string> Phims { get; set; }
        public IEnumerable<string> PhongChieus { get; set; }
        public DateTime Ngays { get; set; }
        public DateTime GioChieux { get; set; }
    }
}