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
                DateTime thoiDiemBatDau = ngayChieu.Date + gioChieu;
                DateTime thoiDiemKetThuc = thoiDiemBatDau.AddMinutes(thoiLuong);

                var lichChieuTrongNgay = db.LichChieux
                    .Where(lc => lc.ID_Phong == phongId && DbFunctions.TruncateTime(lc.NgayChieu) == ngayChieu.Date)
                    .ToList();

                foreach (var lichChieu in lichChieuTrongNgay)
                {
                    DateTime batDauHienCo = lichChieu.NgayChieu.Date + lichChieu.GioChieu;
                    DateTime ketThucHienCo = batDauHienCo.AddMinutes(lichChieu.Phim.ThoiLuong ?? 0);

                    if ((thoiDiemBatDau < ketThucHienCo && thoiDiemKetThuc > batDauHienCo))
                    {
                        return true;
                    }
                }

                return false; 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi trong KiemTraTrungLichChieu: {ex.Message}");
                throw;
            }
        }

        [AdminOnlyAuthorize]
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

                    for (DateTime ngay = lichChieuMoi.TuNgay; ngay <= lichChieuMoi.DenNgay; ngay = ngay.AddDays(1))
                    {
                        DateTime thoiGianChieu = ngay.Date + gioChieu;


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
                var currentUser = db.NhanViens.FirstOrDefault(u => u.TenNhanVien == User.Identity.Name);
                if (currentUser == null || currentUser.QuyenHan != "Admin")
                {
                    return Json(new { success = false, message = "Bạn không có quyền đổi trạng thái lịch chiếu!" });
                }
                var lichChieu = db.LichChieux.Find(id);
                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu!" });
                }

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

                var tatCaKhungGio = new List<TimeSpan>();
                var gioHienTai = new TimeSpan(8, 0, 0);
                var gioKetThuc = new TimeSpan(22, 0, 0);

                while (gioHienTai <= gioKetThuc)
                {
                    tatCaKhungGio.Add(gioHienTai);
                    gioHienTai = gioHienTai.Add(TimeSpan.FromMinutes(30));
                }

                var khungGioKhongKhaDung = new List<TimeSpan>();
                foreach (var lichChieu in lichChieuTrongNgay)
                {
                    var batDau = lichChieu.gioChieu;
                    var ketThuc = batDau.Add(TimeSpan.FromMinutes(lichChieu.thoiLuong + 15));

                    khungGioKhongKhaDung.AddRange(
                        tatCaKhungGio.Where(kg =>
                            kg >= batDau.Add(TimeSpan.FromMinutes(-15)) && kg <= ketThuc
                        )
                    );
                }

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
                var lichChieu = db.LichChieux
                    .Include(lc => lc.Phim)
                    .Include(lc => lc.PhongChieu)
                    .FirstOrDefault(lc => lc.ID_LichChieu == lichChieuId);

                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch chiếu" }, JsonRequestBehavior.AllowGet);
                }

                var vesDaDat = db.Ves
                    .Include(v => v.KhachHang)
                    .Where(v => v.ID_LichChieu == lichChieuId)
                    .ToList();

                var ghesDangGiu = db.Ghe_TrangThai
                    .Include(gt => gt.Ghe)
                    .Where(gt => gt.ID_LichChieu == lichChieuId && gt.ThoiGianGiu > DateTime.Now)
                    .ToList();

                var danhSachGhe = db.Ghes
                    .Where(g => g.ID_Phong == lichChieu.ID_Phong)
                    .ToList()
                    .Select(g => new
                    {
                        tenGhe = g.TenGhe,
                        trangThai = vesDaDat.Any(v => v.ChoNgoi == g.TenGhe) ? "Đã đặt" :
                                   ghesDangGiu.Any(gt => gt.ID_Ghe == g.ID_Ghe) ? "Đang giữ" : "Trống",
                        nguoiDat = vesDaDat.FirstOrDefault(v => v.ChoNgoi == g.TenGhe)?.KhachHang.TenKhachHang ?? ""
                    })
                    .OrderBy(g => g.tenGhe)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Số ghế đã đặt: {vesDaDat.Count}");
                System.Diagnostics.Debug.WriteLine($"Số ghế đang giữ: {ghesDangGiu.Count}");
                System.Diagnostics.Debug.WriteLine($"Tổng số ghế: {danhSachGhe.Count}");

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
                System.Diagnostics.Debug.WriteLine($"Error in GetThongTinGhe: {ex.Message}");
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}