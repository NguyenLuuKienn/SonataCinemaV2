using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;

namespace SonataCinema.Controllers
{

    public class TicketNowController : Controller
    {
        // GET: TicketNow
        CinemaV3Entities db = new CinemaV3Entities();
        public ActionResult TicketNowPartial()
        {
            var phim = db.Phims.Select(p => p.TenPhim).ToList();
            var phong = db.PhongChieux.Select(pc => pc.TenPhong).ToList();
            var ngay = db.LichChieux.Select(lc => lc.NgayChieu).ToList().Select(d => d.ToString("dd-MM-yyyy")).ToList();
            var gio = db.LichChieux.Select(lc => lc.GioChieu).ToList().Select(t => t.ToString(@"hh\:mm")).ToList();
            return PartialView("TicketNowPartial");
        }

        [HttpGet]
        public ActionResult GetNgayChieu(string phim)
        {
            var ngayChieu = db.LichChieux
                .Where(lc => lc.Phim.TenPhim == phim && lc.NgayChieu >= DateTime.Today)
                .Select(lc => lc.NgayChieu)
                .Distinct()
                .OrderBy(d => d)
                .Select(d => d.ToString("dd-MM-yyyy"))
                .ToList();
            return PartialView("");
        }

        [HttpGet]
        public ActionResult GetGioChieu(string phim, string ngayChieu)
        {
            DateTime ngay;
            if (!DateTime.TryParseExact(ngayChieu, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out ngay))
            {
                return new HttpStatusCodeResult(400, "Ngày không hợp lệ");
            }
            var gioChieu = db.LichChieux
                .Where(lc => lc.Phim.TenPhim == phim &&
                             DbFunctions.TruncateTime(lc.NgayChieu) == ngay.Date)
                .Select(lc => lc.GioChieu)
                .Distinct()
                .ToList();
            return PartialView("_TimePartial", "Booking");
        }

        [HttpGet]
        public ActionResult GetPhongChieu(string phim, string ngayChieu, string gioChieu)
        {
            DateTime selectedDate;
            if (!DateTime.TryParseExact(ngayChieu, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out selectedDate))
            {
                return new HttpStatusCodeResult(400, "Ngày không hợp lệ");
            }
            var timeParts = gioChieu.Split(':');
            var timeSpan = new TimeSpan(int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);
            var phongChieu = db.LichChieux
                    .Where(lc => lc.Phim.TenPhim == phim &&
                                DbFunctions.TruncateTime(lc.NgayChieu) == selectedDate.Date &&
                                lc.GioChieu.Hours == timeSpan.Hours &&
                                lc.GioChieu.Minutes == timeSpan.Minutes)
                    .Select(lc => new
                    {
                        MaPhong = lc.ID_Phong,
                        TenPhong = lc.PhongChieu.TenPhong
                    })
                    .Distinct()
                    .ToList();
            return PartialView("");
        }
    }
}
