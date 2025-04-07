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

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ve.ID_Ve,
                        ve.Gia,
                        ve.DiemThuong,
                        ve.NgayDat,
                        ve.TrangThai,
                        ve.ChoNgoi,
                        KhachHang = new { ve.KhachHang.TenKhachHang },
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
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> CancelTicket(int id)
        {
            try
            {
                var ve = db.Ves.Find(id);
                if (ve == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vé!" });
                }

                // Lấy thông tin lịch chiếu
                var lichChieu = db.LichChieux
                                  .Include(lc => lc.Phim)
                                  .FirstOrDefault(lc => lc.ID_LichChieu == ve.ID_LichChieu);

                if (lichChieu == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin lịch chiếu!" });
                }

                // Lấy thời gian hiện tại
                var now = DateTime.Now;

                // Tính thời gian chiếu phim (kết hợp ngày chiếu và giờ chiếu)
                var showTime = lichChieu.NgayChieu.Date + lichChieu.GioChieu;

                // Kiểm tra xem phim đã chiếu chưa
                if (showTime < now)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể huỷ vé cho phim đã chiếu!"
                    });
                }

                // Tính thời gian còn lại đến lúc chiếu
                var timeUntilShow = showTime - now;

                // Kiểm tra xem còn ít nhất 30 phút trước khi chiếu
                if (timeUntilShow.TotalMinutes < 30)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Chỉ có thể huỷ vé trước giờ chiếu 30 phút!"
                    });
                }

                // Tiếp tục xử lý huỷ vé nếu thoả mãn điều kiện
                ve.TrangThai = "Đã huỷ";
                db.SaveChanges();

                // Lấy thông tin khách hàng
                var khachHang = db.KhachHangs.Find(ve.ID_KhachHang);
                if (khachHang == null || string.IsNullOrEmpty(khachHang.Email))
                {
                    throw new Exception("Không tìm thấy thông tin khách hàng hoặc email không hợp lệ.");
                }

                var email = khachHang.Email;
                var customerName = khachHang.TenKhachHang;
                var movieName = lichChieu.Phim.TenPhim;
                var formattedDate = lichChieu.NgayChieu.ToString("dd/MM/yyyy");
                var formattedTime = lichChieu.GioChieu.ToString(@"hh\:mm");
                var showTimeStr = $"{formattedDate} {formattedTime}";

                // Lấy danh sách ghế đã đặt
                var seats = string.Join(", ", ve.ChoNgoi);
                if (string.IsNullOrEmpty(seats))
                {
                    throw new Exception("Không tìm thấy danh sách ghế đã đặt.");
                }

                // Tính tiền hoàn trả (80% giá vé)
                decimal refundAmount = ve.Gia * 0.8m;

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
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

    }
}