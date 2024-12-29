using SonataCinemaV2.Models;
using SonataCinemaV2.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace SonataCinemaV2.Controllers
{
    public class QuickBookingController : Controller
    {
        CinemaV3Entities db = new CinemaV3Entities();

        public ActionResult Index()
        {
            var model = new QuickBookingViewModel
            {
                // Lấy danh sách phim đang chiếu
                Phims = db.Phims
                    .Select(p => p.TenPhim)
                    .ToList(),
            };
            return PartialView("_QuickBooking", model);
        }

        [HttpGet]
        public JsonResult GetNgayChieu(string tenPhim)
        {
            var dates = db.LichChieux
                .Where(lc => lc.Phim.TenPhim == tenPhim)
                .Select(lc => lc.NgayChieu)
                .Distinct()
                .ToList();

            // Format sau khi đã lấy dữ liệu
            var ngayChieu = dates.Select(d => d.ToString("dd/MM/yyyy")).ToList();

            return Json(ngayChieu, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetGioChieu(string tenPhim, string ngayChieu)
        {
            // Chuyển ngayChieu (string) thành DateTime
            DateTime parsedNgayChieu = DateTime.ParseExact(ngayChieu, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var gioChieu = db.LichChieux
                .Where(gc => gc.Phim.TenPhim == tenPhim && DbFunctions.TruncateTime(gc.NgayChieu) == parsedNgayChieu.Date)
                .Select(gc => gc.GioChieu)
                .ToList()
                .Select(g => g
                .ToString(@"hh\:mm")).ToList();

            return Json(gioChieu, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetPhongChieu(string tenPhim, string ngayChieu, string gioChieu)
        {
            DateTime parsedNgayChieu = DateTime.ParseExact(ngayChieu, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string formattedNgayChieu = parsedNgayChieu.ToString("dd-MM-yyyy");
            TimeSpan parsedGioChieu = TimeSpan.Parse(gioChieu);
            var phongChieu = db.LichChieux
                .Where(pc => pc.Phim.TenPhim == tenPhim && DbFunctions.TruncateTime(pc.NgayChieu)== parsedNgayChieu && pc.GioChieu == parsedGioChieu)
                .Select(pc => new
                {
                    TenPhong = pc.PhongChieu.TenPhong,
                    ID_PhongChieu = pc.ID_Phong,
                    IDLichChieu = pc.ID_LichChieu,
                    ngayChieu = formattedNgayChieu
                }).ToList();

            System.Diagnostics.Debug.WriteLine($"GetPhongChieu response: {new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(phongChieu)}");

            return Json(phongChieu, JsonRequestBehavior.AllowGet);
        }
    }
}