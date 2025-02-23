using SonataCinemaV2.Quyen;
using SonataCinemaV2.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using SonataCinemaV2.Models;

namespace SonataCinema.Controllers
{
    [AuthorizeRoles]
    public class LichChieuController : Controller
    {
        private CinemaV3Entities db = new CinemaV3Entities();

        // Lấy danh sách lịch chiếu
        public ActionResult DanhSachLichChieuPartial()
        {
            ViewBag.DanhSachPhim = db.Phims.Where(p => p.TrangThai == "Đang chiếu").ToList();
            ViewBag.DanhSachPhong = db.PhongChieux.ToList();

            var lichChieus = db.LichChieux
                .Include(l => l.Phim)
                .Include(l => l.PhongChieu)
                .ToList();

            return PartialView("DanhSachLichChieuPartial", lichChieus);
        }
        private bool KiemTraTrungLichChieu(DateTime ngayChieu, TimeSpan gioChieu, int phongId, int thoiLuong)
        {
            try
            {
                // Tính toán thời gian bắt đầu và kết thúc của lịch chiếu mới
                DateTime thoiDiemBatDau = ngayChieu.Date + gioChieu;
                DateTime thoiDiemKetThuc = thoiDiemBatDau.AddMinutes(thoiLuong);

                // Lấy tất cả lịch chiếu trong ngày của phòng đó
                var lichChieuTrongNgay = db.LichChieux
                    .Where(lc => lc.ID_Phong == phongId && DbFunctions.TruncateTime(lc.NgayChieu) == ngayChieu.Date)
                    .ToList();

                foreach (var lichChieu in lichChieuTrongNgay)
                {
                    DateTime batDauHienCo = lichChieu.NgayChieu.Date + lichChieu.GioChieu;
                    DateTime ketThucHienCo = batDauHienCo.AddMinutes(lichChieu.Phim.ThoiLuong ?? 0);

                    // 🔥 Cải thiện kiểm tra trùng lịch bằng cách kiểm tra khoảng thời gian chồng lấn
                    if ((thoiDiemBatDau < ketThucHienCo && thoiDiemKetThuc > batDauHienCo))
                    {
                        return true; // Phim mới bị trùng lịch
                    }
                }

                return false; // Không có lịch nào trùng
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi trong KiemTraTrungLichChieu: {ex.Message}");
                throw;
            }
        }

        [AdminOnlyAuthorize]
        // Thêm lịch chiếu
        [HttpPost]
        public JsonResult ThemLichChieu(LichChieuMoi lichChieuMoi)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    TimeSpan gioChieu = TimeSpan.Parse(lichChieuMoi.GioChieu);
                    var lichChieuMoiList = new List<LichChieu>();

                    var phimm = db.Phims.Find(lichChieuMoi.IDPhim);
                    int thoiLuong = phimm?.ThoiLuong ?? 0; 

                    // Kiểm tra trùng lịch cho tất cả các ngày
                    for (DateTime ngay = lichChieuMoi.TuNgay; ngay <= lichChieuMoi.DenNgay; ngay = ngay.AddDays(1))
                    {
                        DateTime thoiGianChieu = ngay.Date + gioChieu;


                        // Kiểm tra trùng lịch
                        if (KiemTraTrungLichChieu(ngay, gioChieu, lichChieuMoi.IDPhong, thoiLuong))
                        {
                            var phim = db.Phims.Find(lichChieuMoi.IDPhim);
                            var phong = db.PhongChieux.Find(lichChieuMoi.IDPhong);
                            return Json(new
                            {
                                success = false,
                                message = $"Lịch chiếu bị trùng! Phòng {phong.TenPhong} đã có lịch chiếu phim khác vào thời điểm {gioChieu.ToString(@"hh\:mm")}, vui lòng chọn khung giờ khác."
                            });
                        }

                        // Nếu không trùng, thêm vào danh sách chờ
                        lichChieuMoiList.Add(new LichChieu
                        {
                            ID_Phim = lichChieuMoi.IDPhim,
                            ID_Phong = lichChieuMoi.IDPhong,
                            NgayChieu = ngay,
                            GioChieu = thoiGianChieu.TimeOfDay,
                            GiaVe = lichChieuMoi.GiaVe,
                            TrangThai = "Đang chiếu"
                        });
                    }

                    // Nếu không có lịch nào trùng, thêm tất cả vào database
                    if (lichChieuMoiList.Any())
                    {
                        db.LichChieux.AddRange(lichChieuMoiList);
                        db.SaveChanges();
                        return Json(new
                        {
                            success = true,
                            message = $"Đã thêm thành công {lichChieuMoiList.Count} lịch chiếu!"
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không có lịch chiếu nào được thêm!" });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
        }

        [AdminOnlyAuthorize]
        [HttpPost]
        public ActionResult XoaLichChieu(int ID_LichChieu)
        {
            try
            {
                var lichChieu = db.LichChieux.Find(ID_LichChieu);
                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Lịch chiếu không tồn tại!" });
                }

                // Kiểm tra xem có vé nào đã được đặt cho lịch chiếu này không
                var coVeDat = db.Ves.Any(v => v.ID_LichChieu == ID_LichChieu);
                if (coVeDat)
                {
                    return Json(new { success = false, message = "Không thể xóa lịch chiếu đã có người đặt vé!" });
                }

                db.LichChieux.Remove(lichChieu);
                db.SaveChanges();
                return Json(new { success = true, message = "Xóa lịch chiếu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [AdminOnlyAuthorize]
        [HttpPost]
        public JsonResult SuaLichChieu(LichChieu lichChieu)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var lcc = db.LichChieux.Find(lichChieu.ID_LichChieu);
                    if (lcc == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy lịch chiếu!" });
                    }

                    // Cập nhật thông tin
                    lcc.ID_Phong = lichChieu.ID_Phong;
                    lcc.NgayChieu = lichChieu.NgayChieu;
                    lcc.GioChieu = lichChieu.GioChieu;

                    db.SaveChanges();
                    return Json(new { success = true, message = "Cập nhật lịch chiếu thành công!" });
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [AdminOnlyAuthorize]
        [HttpPost]
        public JsonResult ToggleTrangThaiLichChieu(int id)
        {
            try
            {
                var lichChieu = db.LichChieux.Find(id);
                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu!" });
                }

                // Đổi trạng thái
                if (lichChieu.TrangThai == "Đang chiếu")
                {
                    lichChieu.TrangThai = "Ngừng chiếu";
                }
                else
                {
                    lichChieu.TrangThai = "Đang chiếu";
                }

                db.Entry(lichChieu).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Đã chuyển trạng thái thành {lichChieu.TrangThai}",
                    newStatus = lichChieu.TrangThai
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ToggleTrangThaiLichChieu: {ex.Message}");
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // lấy thông tin lịch 
        [HttpGet]
        public JsonResult GetLichChieuById(int id)
        {
            try
            {
                var lichChieu = db.LichChieux.Find(id);
                if (lichChieu == null)
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu" });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ID_LichChieu = lichChieu.ID_LichChieu,
                        ID_Phim = lichChieu.ID_Phim,
                        ID_Phong = lichChieu.ID_Phong,
                        NgayChieu = lichChieu.NgayChieu.ToString("dd/MM/yyyy"),
                        GioChieu = lichChieu.GioChieu.ToString(@"hh\:mm")
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}