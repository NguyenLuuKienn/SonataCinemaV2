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
            CapNhatTrangThaiLichChieu();

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



        private void CapNhatTrangThaiLichChieu()
        {
            var lichChieus = db.LichChieux.Include(l => l.Phim).ToList();
            DateTime now = DateTime.Now;

            foreach (var lc in lichChieus)
            {
                DateTime thoiDiemBatDau = lc.NgayChieu.Date + lc.GioChieu;
                DateTime thoiDiemKetThuc = thoiDiemBatDau.AddMinutes(lc.Phim.ThoiLuong ?? 0);

                if (now > thoiDiemKetThuc && lc.TrangThai != "Đã chiếu")
                {
                    lc.TrangThai = "Đã chiếu";
                    db.Entry(lc).State = EntityState.Modified;
                }
                else if (now >= thoiDiemBatDau && now <= thoiDiemKetThuc && lc.TrangThai != "Đang chiếu")
                {
                    lc.TrangThai = "Đang chiếu";
                    db.Entry(lc).State = EntityState.Modified;
                }
                else if (now < thoiDiemBatDau && lc.TrangThai != "Chưa chiếu")
                {
                    lc.TrangThai = "Chưa chiếu";
                    db.Entry(lc).State = EntityState.Modified;
                }
            }

            db.SaveChanges();
        }

        [HttpGet]
        public JsonResult GetLichChieuTrongNgay(int phongId, DateTime ngay)
        {
            try
            {
                // Lấy danh sách lịch chiếu trong ngày của phòng
                var lichChieuTrongNgay = db.LichChieux
                    .Where(lc => lc.ID_Phong == phongId &&
                                DbFunctions.TruncateTime(lc.NgayChieu) == DbFunctions.TruncateTime(ngay))
                    .Select(lc => new
                    {
                        gioChieu = lc.GioChieu,
                        tenPhim = lc.Phim.TenPhim,
                        thoiLuong = lc.Phim.ThoiLuong ?? 0
                    })
                    .ToList();

                // Tạo danh sách tất cả các khung giờ từ 8h đến 22h, mỗi 30 phút
                var tatCaKhungGio = new List<TimeSpan>();
                var gioHienTai = new TimeSpan(8, 0, 0); // Bắt đầu từ 8:00
                var gioKetThuc = new TimeSpan(22, 0, 0); // Kết thúc lúc 22:00

                while (gioHienTai <= gioKetThuc)
                {
                    tatCaKhungGio.Add(gioHienTai);
                    gioHienTai = gioHienTai.Add(TimeSpan.FromMinutes(30));
                }

                // Đánh dấu các khung giờ không khả dụng (đã có phim chiếu + 15 phút buffer)
                var khungGioKhongKhaDung = new List<TimeSpan>();
                foreach (var lichChieu in lichChieuTrongNgay)
                {
                    var batDau = lichChieu.gioChieu;
                    var ketThuc = batDau.Add(TimeSpan.FromMinutes(lichChieu.thoiLuong + 15)); // Thêm 15 phút buffer

                    // Đánh dấu tất cả các khung giờ nằm trong khoảng thời gian chiếu + buffer
                    khungGioKhongKhaDung.AddRange(
                        tatCaKhungGio.Where(kg =>
                            kg >= batDau.Add(TimeSpan.FromMinutes(-15)) && // Thêm 15 phút buffer trước
                            kg <= ketThuc
                        )
                    );
                }

                // Lọc ra các khung giờ còn khả dụng
                var khungGioKhaDung = tatCaKhungGio
                    .Except(khungGioKhongKhaDung)
                    .Select(kg => kg.ToString(@"hh\:mm"))
                    .ToList();

                return Json(new
                {
                    success = true,
                    lichChieuHienTai = lichChieuTrongNgay.Select(l => new
                    {
                        gioChieu = l.gioChieu.ToString(@"hh\:mm"),
                        tenPhim = l.tenPhim,
                        thoiLuong = l.thoiLuong,
                        ketThuc = l.gioChieu.Add(TimeSpan.FromMinutes(l.thoiLuong)).ToString(@"hh\:mm")
                    }),
                    khungGioKhaDung = khungGioKhaDung
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetThongTinGhe(int lichChieuId)
        {
            try
            {
                // Lấy thông tin lịch chiếu
                var lichChieu = db.LichChieux
                    .Include(lc => lc.Phim)
                    .Include(lc => lc.PhongChieu)
                    .FirstOrDefault(lc => lc.ID_LichChieu == lichChieuId);

                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu" }, JsonRequestBehavior.AllowGet);
                }

                // Lấy danh sách ghế đã đặt
                var ghesDaDat = db.Ves
                    .Where(v => v.ID_LichChieu == lichChieuId)
                    .Select(v => v.ChoNgoi)
                    .ToList();

                // Lấy tất cả ghế của phòng
                var danhSachGhe = db.Ghes
                    .Where(g => g.ID_Phong == lichChieu.ID_Phong)
                    .OrderBy(g => g.TenGhe)
                    .Select(g => new
                    {
                        tenGhe = g.TenGhe,
                        trangThai = ghesDaDat.Contains(g.TenGhe) ? "Đã đặt" : "Trống"
                    })
                    .ToList();

                var thongTinLichChieu = new
                {
                    tenPhim = lichChieu.Phim.TenPhim,
                    tenPhong = lichChieu.PhongChieu.TenPhong,
                    ngayChieu = lichChieu.NgayChieu.ToString("dd/MM/yyyy"),
                    gioChieu = lichChieu.GioChieu.ToString(@"hh\:mm")
                };

                return Json(new
                {
                    success = true,
                    thongTinLichChieu = thongTinLichChieu,
                    danhSachGhe = danhSachGhe
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}