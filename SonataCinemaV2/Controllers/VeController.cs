using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SonataCinemaV2.Models;
using SonataCinemaV2.Quyen;
using SonataCinemaV2.Helper;

namespace SonataCinemaV2.Controllers
{
    [AuthorizeRoles]
    public class VeController : Controller
    {
      CinemaV3Entities db = new CinemaV3Entities();
        // GET: Ve
        public ActionResult DanhSachVePartial()
        {
            var ves = db.Ves
                .Include(v => v.KhachHang)
                .Include(v => v.LichChieu.Phim)
                .Include(v => v.LichChieu.PhongChieu)
                .Include(v => v.ThanhToan)
                .ToList();
            return PartialView(ves);
        }

        [HttpGet]
        public JsonResult GetTicketDetails(int id)
        {
            try
            {
                var ve = db.Ves
                    .Include(v => v.KhachHang)
                    .Include(v => v.LichChieu.Phim)
                    .Include(v => v.LichChieu.PhongChieu)
                    .Include(v => v.ThanhToan)
                    .FirstOrDefault(v => v.ID_Ve == id);

                if (ve == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vé!" }, JsonRequestBehavior.AllowGet);
                }

                // Lấy thông tin combo riêng
                var comboInfo = db.ComboOrders
                    .Include(co => co.Combo)
                    .Where(co => co.ID_ThanhToan == ve.ID_ThanhToan)
                    .Select(co => new
                    {
                        TenCombo = co.Combo.TenCombo,
                        SoLuong = co.SoLuong,
                        Gia = co.GiaTien
                    })
                    .ToList();

                // Debug log
                System.Diagnostics.Debug.WriteLine($"Số lượng combo: {comboInfo.Count}");
                foreach (var combo in comboInfo)
                {
                    System.Diagnostics.Debug.WriteLine($"Combo: {combo.TenCombo}, SL: {combo.SoLuong}, Giá: {combo.Gia}");
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ve.ID_Ve,
                        TongTienGoc = ve.ThanhToan?.TongTienGoc ?? 0,
                        ve.DiemThuong,
                        ve.NgayDat,
                        ve.TrangThai,
                        ve.ChoNgoi,
                        KhachHang = new { ve.KhachHang.TenKhachHang },
                        Combos = comboInfo,
                        LichChieu = new
                        {
                            ve.LichChieu.NgayChieu,
                            GioChieu = ve.LichChieu.GioChieu.ToString(@"hh\:mm"),
                            Phim = new { ve.LichChieu.Phim.TenPhim },
                            PhongChieu = new { ve.LichChieu.PhongChieu.TenPhong }
                        }
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetTicketDetails: {ex.Message}");
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> CancelTicket(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var ve = db.Ves
                        .Include(v => v.LichChieu.Phim)
                        .Include(v => v.ThanhToan)
                        .Include(v => v.KhachHang)
                        .FirstOrDefault(v => v.ID_Ve == id);

                    if (ve == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy vé!" });
                    }

                    // Kiểm tra thời gian
                    var now = DateTime.Now;
                    var showTime = ve.LichChieu.NgayChieu.Date + ve.LichChieu.GioChieu;

                    if (showTime < now)
                    {
                        return Json(new { success = false, message = "Không thể huỷ vé cho phim đã chiếu!" });
                    }

                    var timeUntilShow = showTime - now;
                    if (timeUntilShow.TotalMinutes < 30)
                    {
                        return Json(new { success = false, message = "Chỉ có thể huỷ vé trước giờ chiếu 30 phút!" });
                    }

                    // Lưu thông tin để gửi email
                    var email = ve.KhachHang.Email;
                    var customerName = ve.KhachHang.TenKhachHang;
                    var movieName = ve.LichChieu.Phim.TenPhim;
                    var showTimeStr = $"{ve.LichChieu.NgayChieu:dd/MM/yyyy} {ve.LichChieu.GioChieu:hh\\:mm}";
                    var seats = ve.ChoNgoi;
                    decimal refundAmount = ve.ThanhToan.TongTienGoc * 0.8m;

                    // Chỉ cập nhật trạng thái vé thành "Đã huỷ", giữ nguyên thông tin ghế
                    ve.TrangThai = "Đã huỷ";

                    db.SaveChanges();
                    transaction.Commit();

                    // Gửi email xác nhận huỷ vé
                    await EmailHelper.SendCancellationEmail(
                        email,
                        customerName,
                        movieName,
                        showTimeStr,
                        seats,
                        refundAmount
                    );

                    return Json(new 
                    {
                        success = true,
                        message = $"Huỷ vé thành công! Email xác nhận đã được gửi. Bạn sẽ được hoàn lại {refundAmount:N0} VNĐ"
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Error in CancelTicket: {ex.Message}");
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }

    }
}