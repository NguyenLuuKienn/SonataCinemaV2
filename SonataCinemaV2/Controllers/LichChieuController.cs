using SonataCinema.Quyen;
using SonataCinemaV2.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using SonataCinemaV2.Models;

namespace SonataCinema.Controllers
{
    [AdminAuthorize("Admin")]
    public class LichChieuController : Controller
    {
        private CinemaV3Entities db = new CinemaV3Entities();

        // Lấy danh sách lịch chiếu
        public ActionResult DanhSachLichChieuPartial()
        {
            ViewBag.DanhSachPhim = db.Phims.ToList();
            ViewBag.DanhSachPhong = db.PhongChieux.ToList();

            var lichChieus = db.LichChieux
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .ToList();

            return PartialView("DanhSachLichChieuPartial", lichChieus);
        }

        // Thêm lịch chiếu
        [HttpPost]
        public ActionResult ThemLichChieu(LichChieuMoi lichChieuMoi)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    TimeSpan gioChieu = TimeSpan.Parse(lichChieuMoi.GioChieu);

                    for (DateTime ngay = lichChieuMoi.TuNgay; ngay <= lichChieuMoi.DenNgay; ngay = ngay.AddDays(1))
                    {
                        DateTime thoiGianChieu = ngay.Date + gioChieu;

                        // Kiểm tra trùng lịch
                        bool coTrungLich = db.LichChieux.Any(lc =>
                            lc.ID_Phong == lichChieuMoi.IDPhong &&
                            DbFunctions.TruncateTime(lc.NgayChieu) == ngay.Date &&
                            lc.GioChieu == thoiGianChieu.TimeOfDay);

                        if (!coTrungLich)
                        {
                            var lichChieu = new LichChieu
                            {
                                ID_Phim = lichChieuMoi.IDPhim,
                                ID_Phong = lichChieuMoi.IDPhong,
                                NgayChieu = ngay,
                                GioChieu = thoiGianChieu.TimeOfDay
                            };
                            db.LichChieux.Add(lichChieu);
                        }
                    }

                    db.SaveChanges();
                    TempData["Message"] = "Thêm lịch chiếu thành công!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Dữ liệu không hợp lệ! Vui lòng kiểm tra lại.";
            }

            return RedirectToAction("IndexAdmin", "Admin");
        }

        // Xóa lịch chiếu
        [HttpPost]
        public ActionResult XoaLichChieu(int ID_LichChieu)
        {
            try
            {
                var lichChieu = db.LichChieux.Find(ID_LichChieu);
                if (lichChieu == null)
                {
                    TempData["Error"] = "Lịch chiếu không tồn tại!";
                    return RedirectToAction("IndexAdmin", "Admin");
                }

                db.LichChieux.Remove(lichChieu);
                db.SaveChanges();
                TempData["Message"] = "Xóa lịch chiếu thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("IndexAdmin", "Admin");
        }

        // Sửa lịch chiếu
        [HttpPost]
        public ActionResult SuaLichChieu(LichChieu lichChieu)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    var lichChieuCu = db.LichChieux.Find(lichChieu.ID_LichChieu);
                    var lcc = db.LichChieux.Find(lichChieu.ID_LichChieu);
                    if (lcc == null)
                    {
                        TempData["Error"] = "Không tìm thấy lịch chiếu!";
                        return RedirectToAction("IndexAdmin", "Admin");
                    }

                    // Cập nhật thông tin
                    lcc.ID_LichChieu = lichChieu.ID_LichChieu;
                    lcc.ID_Phong = lichChieu.ID_Phong;
                    lcc.NgayChieu = lichChieu.NgayChieu;
                    lcc.GioChieu = lichChieu.GioChieu;

                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật lịch chiếu thành công!";
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage);
                    TempData["Error"] = "Dữ liệu không hợp lệ: " + string.Join(", ", errors);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                System.Diagnostics.Debug.WriteLine($"Error in Edit: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return RedirectToAction("IndexAdmin", "Admin");
        }
        // Thêm action này để lấy thông tin lịch chiếu dạng JSON
        [HttpGet]
        public JsonResult GetLichChieuById(int id)
        {
            try
            {
                var lichChieu = db.LichChieux
                    .Include(l => l.Phim)
                    .Include(l => l.PhongChieu)
                    .FirstOrDefault(l => l.ID_LichChieu == id);

                if (lichChieu != null)
                {
                    // Debug
                    System.Diagnostics.Debug.WriteLine($"Time before format: {lichChieu.GioChieu}");

                    var result = new
                    {
                        MaLichChieu = lichChieu.ID_LichChieu,
                        MaPhim = lichChieu.ID_Phim,
                        MaPhong = lichChieu.ID_Phong,
                        Ngay = lichChieu.NgayChieu.ToString("yyyy-MM-dd"),
                        ThoiGianChieu = $"{lichChieu.GioChieu.Hours:00}:{lichChieu.GioChieu.Minutes:00}", // Đảm bảo dùng HH cho 24h
                        TenPhim = lichChieu.Phim.TenPhim,
                        TenPhong = lichChieu.PhongChieu.TenPhong
                    };

                    // Debug
                    System.Diagnostics.Debug.WriteLine($"Time after format: {result.ThoiGianChieu}");

                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}